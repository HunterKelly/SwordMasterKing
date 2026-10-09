using System;

namespace SwordKing
{
    public enum PlayerAttackKind { Slash, Overhead, Thrust, JumpingOverhead, Spin, ChargedOverhead }
    public static class PlayerAttackModel
    {
        public const float MaxChargeSeconds = 2f;
        public const float SpinChargeThreshold = .25f;
        public static PlayerAttackKind ChargedKind(bool overhead, bool sprint, float heldSeconds)
            => !overhead && !sprint && heldSeconds >= SpinChargeThreshold ? PlayerAttackKind.Spin
             : overhead && !sprint && heldSeconds >= SpinChargeThreshold ? PlayerAttackKind.ChargedOverhead : Kind(overhead,sprint);
        public static float ChargeSeconds(PlayerAttackKind kind) => (kind == PlayerAttackKind.Spin || kind == PlayerAttackKind.Overhead || kind == PlayerAttackKind.ChargedOverhead) ? .6f : MaxChargeSeconds;
        public static float ChargeFraction(float heldSeconds, PlayerAttackKind kind = PlayerAttackKind.Slash)
            => Math.Max(0, Math.Min(1, heldSeconds / ChargeSeconds(kind)));
        public static PlayerAttackKind Kind(bool overhead, bool sprint)
            => overhead ? (sprint ? PlayerAttackKind.JumpingOverhead : PlayerAttackKind.Overhead)
                        : (sprint ? PlayerAttackKind.Thrust : PlayerAttackKind.Slash);
        public static float DamageMultiplier(PlayerAttackKind kind, float heldSeconds, float fullChargeMultiplier)
        {
            float style = kind == PlayerAttackKind.JumpingOverhead ? 3.5f : kind == PlayerAttackKind.ChargedOverhead ? 1.8f : kind == PlayerAttackKind.Overhead ? 1.25f : kind == PlayerAttackKind.Thrust ? 1.15f : 1;
            return style * (1 + (Math.Max(1, fullChargeMultiplier) - 1) * ChargeFraction(heldSeconds,kind));
        }
    }
}
