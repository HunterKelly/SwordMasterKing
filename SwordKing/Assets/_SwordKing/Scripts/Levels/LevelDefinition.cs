using System;
using UnityEngine;

namespace SwordKing
{
    [CreateAssetMenu(menuName = "SwordKing/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Serializable]
        public struct EnemySpawn
        {
            public int id, zone;
            public string title;
            public Vector3 position;
            [Range(0, 2)] public int attackStyle;
            public bool boss;
            public EnemySpawn(int id, int zone, string title, Vector3 position, int style, bool boss = false)
            { this.id=id; this.zone=zone; this.title=title; this.position=position; attackStyle=style; this.boss=boss; }
        }
        [Header("Level identity")]
        public bool iceWorld;
        public string displayName = "THE BROKEN GATE";
        public string bossName = "THE GATEKEEPER";
        public string saveKey = "BrokenGate.Level1.Save.v1";
        [Header("Ice World")]
        public int placementSeed = 2026;
        [Range(1, 24)] public int randomEnemyCount = 12;
        [Range(0, 1)] public float snowFootstepVolume = .16f;
        [Range(0, 600)] public float snowfallRate = 180;
        public EnemyCombatSettings enemyCombat = new EnemyCombatSettings();
        public CombatFeedbackSettings combatFeedback = new CombatFeedbackSettings();
        public Vector3 arrival = new Vector3(0, .15f, -5);
        // v1 saves address exactly seven stable encounter IDs. Positions and styles are editable.
        public EnemySpawn[] encounters = DefaultEncounters();
        public static EnemySpawn[] DefaultEncounters() => new[]
        {
            new EnemySpawn(0,0,"Road Warden",new Vector3(0,.1f,23),0),
            new EnemySpawn(1,0,"Ash Swordsman",new Vector3(-4,.1f,31),2),
            new EnemySpawn(2,0,"Courtyard Reaper",new Vector3(4,.1f,34),1),
            new EnemySpawn(3,1,"Gatehouse Reaper",new Vector3(-3,.1f,69),1),
            new EnemySpawn(4,1,"Iron Sentinel",new Vector3(4,.1f,76),0),
            new EnemySpawn(5,1,"Captain of the Watch",new Vector3(-2,.1f,80),2),
            new EnemySpawn(6,2,"THE GATEKEEPER",new Vector3(0,.1f,116),2,true)
        };
        public static void ValidateEncounters(EnemySpawn[] spawns)
        {
            if (spawns == null || spawns.Length != 7) throw new InvalidOperationException("Level 1 requires seven encounters for v1 saves.");
            for (int i=0; i<spawns.Length; i++)
                if (spawns[i].id != i || spawns[i].zone != (i<3?0:i<6?1:2) || spawns[i].boss != (i==6))
                    throw new InvalidOperationException("Keep Level 1 IDs ordered 0–6, zones 0/0/0/1/1/1/2, and the boss at ID 6.");
        }
    }
}
