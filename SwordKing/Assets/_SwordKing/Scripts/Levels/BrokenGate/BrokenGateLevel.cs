using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwordKing
{
    [DefaultExecutionOrder(100)]
    public partial class BrokenGateLevel : MonoBehaviour
    {
        enum ScreenState { Title, Playing, Paused, Shrine, Dead, Victory }
        [Serializable]
        public class SaveData
        {
            public int version=1, power=4, recovery=4, speed=4, shards, deaths;
            public bool checkpoint, cache, completed;
            public bool[] defeated=new bool[7];
            public float seconds;
        }
        const string SaveKey="BrokenGate.Level1.Save.v1";
        public PlayerController Player { get; private set; }
        public BrokenGateWorld World { get; private set; }
        public bool InputBlocked => screen!=ScreenState.Playing;
        readonly List<GateEnemy> enemies=new List<GateEnemy>();
        readonly Dictionary<string,AudioClip> sounds=new Dictionary<string,AudioClip>();
        readonly List<FloatingHit> hits=new List<FloatingHit>();
        class FloatingHit { public Vector3 p; public string text; public float until; }
        SaveData save=new SaveData();
        ScreenState screen=ScreenState.Title;
        AudioSource audioSource, wind;
        GateEnemy boss;
        bool bossActive, ready, hasSave, confirmNew;
        int flasks=2, chosenStyle=1;
        float nextEnemyAttack, messageUntil, hurtFlashUntil;
        string message="", interaction="", saveWarning="";
        float originalTimeScale;
        bool originalFog;
        Color originalFogColor, originalAmbient;
        float originalFogDensity;
        FogMode originalFogMode;

        public void Initialize(PlayerController player)
        {
            Player=player; originalTimeScale=Time.timeScale;
            originalFog=RenderSettings.fog; originalFogColor=RenderSettings.fogColor;
            originalFogDensity=RenderSettings.fogDensity; originalFogMode=RenderSettings.fogMode;
            originalAmbient=RenderSettings.ambientLight;
            World = player.AuthoredWorld;
            if (World == null)
            {
                var worldRoot = new GameObject("The Broken Gate - World");
                worldRoot.transform.SetParent(transform, false);
                World = worldRoot.AddComponent<BrokenGateWorld>();
                World.Build();
            }
            World.EnsureMaterials();
            var spawns = player.LevelDefinition != null ? player.LevelDefinition.encounters : LevelDefinition.DefaultEncounters();
            LevelDefinition.ValidateEncounters(spawns);
            foreach (var spawn in spawns)
            {
                var enemy = new GateEnemy(this, spawn.id, spawn.zone, spawn.title, spawn.position, spawn.attackStyle, spawn.boss);
                enemies.Add(enemy);
                if (spawn.boss) boss = enemy;
            }
            RenderSettings.fog=true; RenderSettings.fogColor=new Color(.20f,.28f,.31f);
            RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogDensity=.014f;
            RenderSettings.ambientLight=new Color(.45f,.52f,.57f);
            Player.PlayerCamera.backgroundColor=RenderSettings.fogColor;
            Player.PlayerCamera.farClipPlane=220;
            foreach(var light in GetComponentsInChildren<Light>()) if(light.type==LightType.Directional)
            { light.color=new Color(1,.85f,.66f); light.shadows=LightShadows.Soft; light.shadowStrength=.7f; }
            CreateAudio(); hasSave=PlayerPrefs.HasKey(SaveKey);
            Player.RestoreAt(Arrival); SetScreen(ScreenState.Title); ready=true;
        }
        Vector3 Arrival => Player.LevelDefinition != null ? Player.LevelDefinition.arrival : new Vector3(0, .15f, -5);

        void StartRun(bool load)
        {
            save=new SaveData();
            if(load)
            {
                try { save=JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey)); }
                catch(Exception) { save=null; }
                if(save==null || save.version!=1 || save.defeated==null || save.defeated.Length!=7)
                { saveWarning="Save could not be read. Start a new journey."; return; }
                save.power=Mathf.Clamp(save.power,0,10); save.recovery=Mathf.Clamp(save.recovery,0,10); save.speed=Mathf.Clamp(save.speed,0,10);
                save.shards=Mathf.Max(0,save.shards); save.deaths=Mathf.Max(0,save.deaths);
                if(float.IsNaN(save.seconds)||float.IsInfinity(save.seconds)||save.seconds<0) save.seconds=0;
            }
            else
            {
                if(chosenStyle==0) { save.power=10; save.recovery=2; save.speed=0; }
                if(chosenStyle==2) { save.power=0; save.recovery=2; save.speed=10; }
            }
            Player.power=save.power; Player.recovery=save.recovery; Player.speed=save.speed;
            ResetEncounters();
            Player.RestoreAt(save.checkpoint?World.Camp+Vector3.back*2:Arrival);
            flasks=2; confirmNew=false; Save(); SetScreen(save.completed?ScreenState.Victory:ScreenState.Playing);
            Notify(load?"Journey resumed at your last refuge.":"Find the road through the fortress.");
        }
        void ResetEncounters()
        {
            for(int i=0;i<enemies.Count;i++) enemies[i].Reset(save.defeated[i]);
            bossActive=false; nextEnemyAttack=Time.time+.8f; hits.Clear(); SyncGates();
            World.BossEntrance.SetActive(false); World.Cache.SetActive(!save.cache);
        }
        bool Cleared(int zone)
        {
            foreach(var enemy in enemies) if(enemy.Zone==zone && enemy.Alive) return false;
            return true;
        }
        void SyncGates()
        {
            World.CourtyardGate.SetActive(!Cleared(0)); World.GatehouseGate.SetActive(!Cleared(1));
            World.ExitGate.SetActive(boss.Alive);
            if(!boss.Alive) World.BossEntrance.SetActive(false);
        }
        void SetScreen(ScreenState value)
        {
            screen=value; Time.timeScale=value==ScreenState.Playing?1:0;
            Player.SetGameplayInput(value==ScreenState.Playing);
            if(wind!=null) wind.volume=value==ScreenState.Playing?.16f:.06f;
        }
        public void Pause() { if(ready && screen==ScreenState.Playing) SetScreen(ScreenState.Paused); }
        public void TogglePause()
        {
            if(screen==ScreenState.Playing) SetScreen(ScreenState.Paused);
            else if(screen==ScreenState.Paused || screen==ScreenState.Shrine) SetScreen(ScreenState.Playing);
        }
        void Update()
        {
            if(!ready || InputBlocked) return;
            save.seconds+=Time.deltaTime;
            var input = Player.InputFrame;
            bool interact = input.Interact, heal = input.Heal;
            Vector3 p=Player.PlayerTransform.position;
            if(p.y < -6) { Die("The ravine claims another traveler."); return; }
            if(heal && !Player.IsRolling && flasks>0 && Player.Heal(45)) { flasks--; PlaySound("heal"); Notify("Ember flask • health restored"); }
            if(p.z>103 && p.z<126 && boss.Alive && !bossActive)
            { bossActive=true; World.BossEntrance.SetActive(true); Notify("THE GATEKEEPER • Break his watch."); PlaySound("boss"); }
            foreach(var enemy in enemies)
            {
                enemy.Tick(Time.deltaTime);
                if(InputBlocked) return;
            }
            interaction="";
            if(!save.cache && Vector3.Distance(p,World.CachePosition)<2.2f)
            {
                interaction="E  Open the lost knight's coffer";
                if(interact)
                {
                    save.cache=true; save.shards+=2; World.Cache.SetActive(false); Save();
                    Notify("Lost knight's coffer • +2 upgrade embers"); PlaySound("reward");
                }
            }
            else if(Vector3.Distance(p,World.Camp)<3 && Cleared(1))
            {
                interaction="E  Rest at the shrine / improve your sword";
                if(interact && !Player.IsRolling)
                {
                    save.checkpoint=true; Player.Heal(100); flasks=2; Save();
                    PlaySound("reward"); SetScreen(ScreenState.Shrine);
                }
            }
            if(!boss.Alive && p.z>132)
            {
                save.completed=true; Save(); PlaySound("victory"); SetScreen(ScreenState.Victory);
            }
            hits.RemoveAll(h=>h.until<Time.time);
        }
        public bool CanEngage(GateEnemy enemy)
        {
            if(InputBlocked) return false;
            float z=Player.PlayerTransform.position.z;
            if(enemy.IsBoss) return bossActive;
            return enemy.Zone==0?z>16 && z<40:z>64 && z<86;
        }
        public bool TryClaimAttack(GateEnemy enemy)
        {
            if(Time.time<nextEnemyAttack) return false;
            // Space group attacks apart so tells remain readable.
            nextEnemyAttack=Time.time+(enemy.IsBoss?.5f:1.05f); return true;
        }
        public void ResolvePlayerAttack(float damage,float reach,float angle,float charge)
        {
            Vector3 p=Player.PlayerTransform.position;
            foreach(var enemy in enemies)
            {
                if(!enemy.Alive || !CanEngage(enemy)) continue;
                Vector3 delta=enemy.Root.position-p; float vertical=Mathf.Abs(delta.y); delta.y=0;
                if(delta.magnitude>reach+(enemy.IsBoss?.45f:0) || vertical>2 || Vector3.Angle(Player.PlayerTransform.forward,delta)>angle*.5f) continue;
                if(!HasClearStrike(p+Vector3.up*1.2f,enemy.Root.position+Vector3.up*1.2f,enemy)) continue;
                float dealt=enemy.ReceiveHit(damage,charge);
                hits.Add(new FloatingHit { p=enemy.Root.position+Vector3.up*(enemy.IsBoss?3.7f:2.3f), text=Mathf.RoundToInt(dealt).ToString(), until=Time.time+.6f });
                PlaySound(charge>=.8f?"heavy":"hit");
            }
        }
        bool HasClearStrike(Vector3 from,Vector3 to,GateEnemy target)
        {
            Vector3 delta=to-from;
            foreach(var hit in Physics.RaycastAll(from,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
            {
                Transform t=hit.collider.transform;
                if(t.IsChildOf(Player.PlayerTransform) || t.IsChildOf(target.Root)) continue;
                return false;
            }
            return true;
        }
        public void ResolveEnemyAttack(GateEnemy enemy,bool low,float reach,float damage)
        {
            if(InputBlocked) return;
            Vector3 center=enemy.Root.position+enemy.Root.forward*(reach*.5f)+Vector3.up*(low?.45f:1.65f);
            Vector3 extents=new Vector3(reach*.65f,low?.38f:1.1f,reach*.5f);
            // One volume, one damage event. Physical hurtboxes provide the jump/roll exclusions.
            Physics.SyncTransforms();
            foreach(var col in Physics.OverlapBox(center,extents,enemy.Root.rotation,~0,QueryTriggerInteraction.Collide))
            {
                var hurtbox=col.GetComponent<PlayerHurtbox>();
                if(hurtbox==null || hurtbox.Owner!=Player || hurtbox.IsLowerBody!=low) continue;
                Vector3 delta=Player.PlayerTransform.position-enemy.Root.position; delta.y=0;
                if(delta.magnitude>reach || Vector3.Angle(enemy.Root.forward,delta)>65) continue;
                if(!HasClearStrike(Player.PlayerTransform.position+Vector3.up*1.2f,enemy.Root.position+Vector3.up*1.2f,enemy)) continue;
                hurtbox.TryHit(damage); break;
            }
            PlaySound("swing");
        }
        public void EnemyDefeated(GateEnemy enemy)
        {
            if(save.defeated[enemy.Id]) return;
            save.defeated[enemy.Id]=true; save.shards+=enemy.IsBoss?3:1;
            SyncGates(); Save(); PlaySound("reward");
            if(enemy.IsBoss) Notify("THE GATEKEEPER HAS FALLEN • Cross the open gate.");
            else if(Cleared(enemy.Zone)) Notify("The seal is broken. The way is open.");
            else Notify("+1 upgrade ember");
        }
        public void OnPlayerHurt()
        {
            hurtFlashUntil=Time.time+.22f; PlaySound("hurt");
            if(Player.PlayerHealth<=0) Die("The fortress has not finished with you.");
        }
        void Die(string reason)
        {
            save.deaths++; message=reason; Save(); SetScreen(ScreenState.Dead);
        }
        void Respawn()
        {
            ResetEncounters(); flasks=2;
            Player.RestoreAt(save.checkpoint?World.Camp+Vector3.back*2:Arrival);
            SetScreen(ScreenState.Playing); Notify("Rise again. Your earned upgrades remain.");
        }
        void Notify(string text) { message=text; messageUntil=Time.time+4; }
        string Objective()
        {
            float z=Player.PlayerTransform.position.z;
            if(!Cleared(0)) return z<16?"Follow the road into the outer courtyard":"Defeat the courtyard watch to break the first seal";
            if(z<64) return "Cross the broken bridge • a lost coffer lies to the east";
            if(!Cleared(1)) return "Defeat the gatehouse watch • jump amber sweeps, roll red strikes";
            if(!save.checkpoint && z<101) return "Rest at the ember shrine before the final gate";
            return boss.Alive?"Defeat the Gatekeeper":"Cross the open gate";
        }
        void Upgrade(int stat)
        {
            if(save.shards<=0) return;
            if(stat==0 && Player.power<10) Player.power++;
            else if(stat==1 && Player.recovery<10) Player.recovery++;
            else if(stat==2 && Player.speed<10) Player.speed++;
            else return;
            save.shards--; Save(); PlaySound("reward");
        }
        void OnApplicationQuit() { if(ready && screen!=ScreenState.Title) Save(); }
        void OnDestroy()
        {
            Time.timeScale=originalTimeScale<=0?1:originalTimeScale;
            RenderSettings.fog=originalFog; RenderSettings.fogColor=originalFogColor;
            RenderSettings.fogDensity=originalFogDensity; RenderSettings.fogMode=originalFogMode; RenderSettings.ambientLight=originalAmbient;
            if(World!=null) World.Dispose();
            foreach(var clip in sounds.Values) Destroy(clip);
        }
    }
}
