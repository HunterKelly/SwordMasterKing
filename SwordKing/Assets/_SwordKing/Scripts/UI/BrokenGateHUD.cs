using UnityEngine;

namespace SwordKing
{
    public partial class BrokenGateLevel
    {
        GUIStyle heading, subtitle, body, small, button, centered;
        readonly Color ink=new Color(.025f,.045f,.055f,.94f), gold=new Color(.94f,.72f,.38f), teal=new Color(.22f,.8f,.72f);
        void Styles()
        {
            if(heading!=null) return;
            heading=new GUIStyle(GUI.skin.label) { fontSize=42, fontStyle=FontStyle.Bold, alignment=TextAnchor.MiddleCenter };
            heading.normal.textColor=new Color(.96f,.91f,.8f);
            subtitle=new GUIStyle(GUI.skin.label) { fontSize=18, alignment=TextAnchor.MiddleCenter, wordWrap=true };
            subtitle.normal.textColor=gold;
            body=new GUIStyle(GUI.skin.label) { fontSize=18, wordWrap=true };
            body.normal.textColor=new Color(.90f,.93f,.92f);
            small=new GUIStyle(body) { fontSize=14 };
            centered=new GUIStyle(body) { alignment=TextAnchor.MiddleCenter };
            button=new GUIStyle(GUI.skin.button) { fontSize=18, padding=new RectOffset(14,14,10,10) };
        }
        void Panel(Rect rect,Color color)
        {
            Color old=GUI.color; GUI.color=color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=old;
        }
        void Bar(Rect rect,float value,Color color)
        {
            Panel(rect,new Color(.1f,.13f,.15f,.95f));
            Panel(new Rect(rect.x,rect.y,rect.width*Mathf.Clamp01(value),rect.height),color);
        }
        bool Button(float x,float y,float w,string text) => GUI.Button(new Rect(x,y,w,42),text,button);
        void OnGUI()
        {
            if(!ready) return;
            Styles();
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            Vector2 offset=new Vector2((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2);
            GUI.matrix=Matrix4x4.TRS(new Vector3(offset.x,offset.y,0),Quaternion.identity,Vector3.one*scale);
            if(screen==ScreenState.Playing)
            {
                Panel(new Rect(22,20,292,108),ink);
                GUI.Label(new Rect(38,29,255,24),LevelTitle,small);
                Bar(new Rect(38,60,255,13),Player.PlayerHealth/100,new Color(.78f,.24f,.23f));
                GUI.Label(new Rect(38,78,170,20),"HEALTH  "+Mathf.CeilToInt(Player.PlayerHealth),small);
                Bar(new Rect(38,103,255,7),Player.SwingCharge,gold);
                GUI.Label(new Rect(340,24,600,54),Objective(),subtitle);
                Panel(new Rect(22,605,310,92),ink);
                GUI.Label(new Rect(38,617,280,26),"Q  Ember flask  "+flasks+" / 2",body);
                GUI.Label(new Rect(38,650,270,24),save.shards+" upgrade embers",small);
                GUI.Label(new Rect(908,630,350,65),"WASD move  •  Mouse aim  •  Click attack\nSpace jump  •  Shift roll  •  Esc pause",small);
                GUI.Label(new Rect(634,348,24,24),"+",centered);
                if(Player.IsRolling) GUI.Label(new Rect(500,520,280,25),"INVINCIBLE",subtitle);
                else if(Player.LowerBodyProtected) GUI.Label(new Rect(500,520,280,25),"AIRBORNE",small);
                if(interaction.Length>0) { Panel(new Rect(350,568,580,42),ink); GUI.Label(new Rect(360,574,560,30),interaction,centered); }
                if(Time.time<messageUntil) GUI.Label(new Rect(260,95,760,45),message,centered);
                if(bossActive && boss.Alive)
                {
                    GUI.Label(new Rect(350,642,580,25),boss.Health<boss.MaxHealth*.5f?BossTitle+"  •  UNBOUND":BossTitle,subtitle);
                    Bar(new Rect(350,679,580,10),boss.Health/boss.MaxHealth,new Color(.75f,.24f,.19f));
                }
                foreach(var enemy in enemies)
                {
                    if(!enemy.Alive || !enemy.Engaged) continue;
                    Vector3 point=Player.PlayerCamera.WorldToScreenPoint(enemy.Root.position+Vector3.up*(enemy.IsBoss?3.6f:2.5f));
                    if(point.z<=0) continue;
                    float x=(point.x-offset.x)/scale, y=(Screen.height-point.y-offset.y)/scale;
                    if(!enemy.IsBoss) Bar(new Rect(x-45,y,90,5),enemy.Health/enemy.MaxHealth,new Color(.8f,.35f,.2f));
                    if(enemy.Telegraphing)
                    {
                        string tell=enemy.LowAttack?"SWEEP • JUMP / ROLL":"OVERHEAD • ROLL";
                        GUI.Label(new Rect(x-150,y-36,300,28),tell,subtitle);
                    }
                }
                foreach(var hit in hits)
                {
                    Vector3 point=Player.PlayerCamera.WorldToScreenPoint(hit.p+Vector3.up*(.6f-(hit.until-Time.time)));
                    if(point.z>0) GUI.Label(new Rect((point.x-offset.x)/scale-30,(Screen.height-point.y-offset.y)/scale-20,70,32),hit.text,subtitle);
                }
                if(Time.time<hurtFlashUntil)
                {
                    Color c=new Color(.8f,.1f,.06f,.28f);
                    Panel(new Rect(0,0,1280,8),c); Panel(new Rect(0,712,1280,8),c);
                    Panel(new Rect(0,0,8,720),c); Panel(new Rect(1272,0,8,720),c);
                }
            }
            else
            {
                Panel(new Rect(0,0,1280,720),new Color(.015f,.025f,.03f,.79f));
                Panel(new Rect(260,75,760,570),ink); Panel(new Rect(260,75,760,3),gold);
                if(screen==ScreenState.Title) TitleScreen();
                else if(screen==ScreenState.Paused)
                {
                    GUI.Label(new Rect(300,135,680,60),"JOURNEY PAUSED",heading);
                    GUI.Label(new Rect(340,214,600,60),"Click quickly for light cuts. Wait for a heavy strike.\nJump amber sweeps. Roll through red strikes.",centered);
                    if(Button(460,320,360,"Return to the road")) SetScreen(ScreenState.Playing);
                    if(Button(460,378,360,"Save and return to title")) { Save(); SetScreen(ScreenState.Title); }
                    GUI.Label(new Rect(340,450,600,70),"Power "+Player.power+"   /   Recovery "+Player.recovery+"   /   Speed "+Player.speed+"\nSpend upgrade embers at the shrine beyond the gatehouse.",centered);
                }
                else if(screen==ScreenState.Shrine) ShrineScreen();
                else if(screen==ScreenState.Dead)
                {
                    GUI.Label(new Rect(300,160,680,60),"YOU HAVE FALLEN",heading);
                    GUI.Label(new Rect(340,255,600,60),message,centered);
                    GUI.Label(new Rect(340,326,600,60),"Earned embers and defeated enemies are kept.\nSurviving enemies return to full health.",centered);
                    if(Button(460,435,360,"Rise at your last refuge")) Respawn();
                }
                else if(screen==ScreenState.Victory)
                {
                    GUI.Label(new Rect(300,133,680,60),"THE ROAD IS OPEN",heading);
                    GUI.Label(new Rect(340,212,600,60),IceWorld?"CHAPTER II  •  COMPLETE\nThe frozen watch has fallen.":"CHAPTER I  •  COMPLETE\nThe watch is broken. Beyond the gate, your journey begins.",subtitle);
                    GUI.Label(new Rect(340,315,600,110),"Time  "+Mathf.FloorToInt(save.seconds/60)+"m "+Mathf.FloorToInt(save.seconds%60)+"s\nDeaths  "+save.deaths+"\nLost knight's coffer  "+(save.cache?"Found":"Unclaimed"),centered);
                    if(!IceWorld && Button(460,423,360,"Continue to Level 2 - Ice World"))
                    {
                        SetScreen(ScreenState.Playing);
                        UnityEngine.SceneManagement.SceneManager.LoadScene("IceWorld");
                    }
                    if(Button(460,473,360,"Return to title")) SetScreen(ScreenState.Title);
                }
            }
            if(saveWarning.Length>0) GUI.Label(new Rect(260,678,760,32),saveWarning,centered);
            GUI.matrix=Matrix4x4.identity;
        }
        void TitleScreen()
        {
            GUI.Label(new Rect(300,109,680,64),LevelTitle,heading);
            GUI.Label(new Rect(340,179,600,36),IceWorld?"CHAPTER II  •  ICE WORLD":"CHAPTER I  •  THE MOUNTAIN FORTRESS",subtitle);
            GUI.Label(new Rect(340,239,600,60),IceWorld?"Snow falls over the frozen watch.\nClear the courtyard and defeat its Warden.":"A sealed road. A fallen watch.\nFight your way through, and open the gate.",centered);
            GUI.Label(new Rect(340,319,600,30),"Choose your starting sword style",centered);
            string[] titles={"Heavy","Balanced","Fast"};
            for(int i=0;i<3;i++)
            {
                GUI.backgroundColor=chosenStyle==i?teal:Color.white;
                if(Button(340+i*205,361,190,titles[i])) chosenStyle=i;
            }
            GUI.backgroundColor=Color.white;
            string desc=chosenStyle==0?"Power 10 / Recovery 2 / Speed 0 — decisive blows":chosenStyle==2?"Power 0 / Recovery 2 / Speed 10 — relentless cuts":"Power 4 / Recovery 4 / Speed 4 — adaptable rhythm";
            GUI.Label(new Rect(340,414,600,30),desc,small);
            if(hasSave)
            {
                if(Button(340,474,290,"Continue journey")) StartRun(true);
                if(Button(650,474,290,confirmNew?"Confirm new journey":"New journey"))
                { if(confirmNew) StartRun(false); else confirmNew=true; }
                if(confirmNew) GUI.Label(new Rect(340,528,600,35),"Starting again replaces this level's saved journey.",centered);
            }
            else if(Button(440,474,400,"Begin the journey")) StartRun(false);
            GUI.Label(new Rect(340,581,600,28),"WASD • Mouse • Click • Space jump • Shift roll • Q heal",small);
        }
        void ShrineScreen()
        {
            GUI.Label(new Rect(300,108,680,60),"EMBER SHRINE",heading);
            GUI.Label(new Rect(340,180,600,60),"Checkpoint saved • Health and flasks restored\n"+save.shards+" upgrade embers available",subtitle);
            string[] names={"Power","Recovery","Speed"}; int[] values={Player.power,Player.recovery,Player.speed};
            string[] descriptions={"Stronger fully recovered blows","Less waiting between strong attacks","Faster attack cap and stronger quick cuts"};
            for(int i=0;i<3;i++)
            {
                float y=270+i*84;
                GUI.Label(new Rect(340,y,365,26),names[i]+"   "+values[i]+" / 10",body);
                GUI.Label(new Rect(340,y+29,390,26),descriptions[i],small);
                GUI.enabled=save.shards>0 && values[i]<10;
                if(Button(770,y,170,"Improve • 1")) Upgrade(i);
                GUI.enabled=true;
            }
            if(Button(440,552,400,"Return to the road")) SetScreen(ScreenState.Playing);
        }
    }
}
