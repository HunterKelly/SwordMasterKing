using System;
using UnityEngine;

namespace SwordKing
{
    [Serializable]
    public sealed class EnemyCombatSettings
    {
        [Header("Pursuit")]
        [Min(.1f)] public float moveSpeed = 3.4f;
        [Min(.1f)] public float bossMoveSpeed = 3.2f;
        [Min(1)] public float detectionRange = 17f;
        [Min(1)] public float bossDetectionRange = 24f;
        [Min(0)] public float strafeSpeed = 1.4f;
        [Header("Attack timings (seconds)")]
        [Min(.25f)] public float windup = .62f;
        [Min(.25f)] public float bossWindup = .72f;
        [Min(.25f)] public float bossPhaseTwoWindup = .5f;
        [Min(.08f)] public float strikeDuration = .12f;
        [Min(.45f)] public float recovery = .85f;
        [Min(.45f)] public float bossRecovery = .95f;
        [Min(.45f)] public float bossPhaseTwoRecovery = .7f;
        [Min(.5f)] public float groupAttackSpacing = .8f;
        [Header("Special attack reactions (regular enemies)")]
        [Min(0)] public float thrustPushDistance = 1.5f;
        [Min(.05f)] public float thrustPushDuration = .2f;
        [Min(0)] public float landingPopHeight = .35f;
        public float ChaseSpeed(bool boss) => Mathf.Max(.1f, boss ? bossMoveSpeed : moveSpeed);
        public float Windup(bool boss, bool phaseTwo) => Mathf.Max(.25f, boss ? (phaseTwo ? bossPhaseTwoWindup : bossWindup) : windup);
        public float Recovery(bool boss, bool phaseTwo) => Mathf.Max(.45f, boss ? (phaseTwo ? bossPhaseTwoRecovery : bossRecovery) : recovery);
    }
}
