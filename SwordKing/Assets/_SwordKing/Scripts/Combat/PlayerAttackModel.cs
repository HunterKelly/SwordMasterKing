using System;

namespace SwordKing
{
    public enum PlayerAttackKind { Slash, Overhead, Thrust, JumpingOverhead, Spin }
    public static class PlayerAttackModel
    {
        public const float MaxChargeSeconds = 2f;
        public const float SpinChargeThreshold = .25f;
        public static PlayerAttackKind ChargedKind(bool overhead, bool sprint, float heldSeconds)
            => !overhead && !sprint && heldSeconds >= SpinChargeThreshold ? PlayerAttackKind.Spin : Kind(overhead,sprint);
        public static float ChargeFraction(float heldSeconds) => Math.Max(0, Math.Min(1, heldSeconds / MaxChargeSeconds));
        public static PlayerAttackKind Kind(bool overhead, bool sprint)
            => overhead ? (sprint ? PlayerAttackKind.JumpingOverhead : PlayerAttackKind.Overhead)
                        : (sprint ? PlayerAttackKind.Thrust : PlayerAttackKind.Slash);
        public static float DamageMultiplier(PlayerAttackKind kind, float heldSeconds, float fullChargeMultiplier)
        {
            float style = kind == PlayerAttackKind.JumpingOverhead ? 1.65f : kind == PlayerAttackKind.Overhead ? 1.25f : kind == PlayerAttackKind.Thrust ? 1.15f : 1;
            return style * (1 + (Math.Max(1, fullChargeMultiplier) - 1) * ChargeFraction(heldSeconds));
        }
    }
    public sealed class ShiftGesture
    {
        bool held;
        float started;
        public bool SprintHeld { get; private set; }
        public bool Tick(bool pressed, float now, float threshold)
        {
            if(pressed && !held) started=now;
            bool roll=held && !pressed && now-started < threshold;
            SprintHeld=pressed && now-started >= threshold;
            held=pressed; return roll;
        }
        public void Clear() { held=false; SprintHeld=false; }
    }
}
