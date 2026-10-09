using UnityEngine;

namespace SwordKing
{
    [CreateAssetMenu(menuName = "SwordKing/Player Settings")]
    public sealed class PlayerSettings : ScriptableObject
    {
        [Header("Starting sword stats (journey styles override these)")]
        [Range(0, 10)] public int power = 4, recovery = 4, speed = 4;
        [Header("Movement and combat")]
        [Min(.1f)] public float moveSpeed = 6f, reach = 2.8f;
        [Header("Strafe rotation (visuals only)")]
        [InspectorName("Strafe Turn Angle"), Range(0, 60)] public float strafeLeanAngle = 45f;
        [InspectorName("Strafe Turn Speed"), Min(1)] public float strafeLeanSpeed = 360f;
        [Range(30, 180)] public float attackAngle = 110f;
        [Header("Sprint and charged attacks")]
        [Min(1)] public float sprintMultiplier = 1.6f;
        [Range(.1f, .3f)] public float sprintHoldThreshold = .18f;
        [Min(1)] public float fullChargeDamageMultiplier = 2f;
        [Header("Jump and roll")]
        [Range(.1f, 1.5f)] public float jumpHeightFraction = .5f;
        [Min(.1f)] public float rollDuration = .45f, rollDistance = 3.6f;
        [Min(0)] public float rollRecovery = .1f;
        [Min(1)] public float gravityStrength = 20f;
    }
}
