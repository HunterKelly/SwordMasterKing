using System;

namespace SwordKing
{
    // Pure gameplay math: no Animator, animation events, or root motion.
    public static class SwingModel
    {
        public static float MaxRate(int speed) => 4f + 0.8f * speed;
        public static float RecoverySeconds(int recovery) => 1.1f / (1f + 0.12f * recovery);
        public static float MinDamage(int speed) => 3f + 0.9f * speed;
        public static float MaxDamage(int power, int speed) => 24f + 4f * power + 0.9f * speed;
        public static float Charge(float elapsed, int recovery)
            => Math.Max(0f, Math.Min(1f, elapsed / RecoverySeconds(recovery)));
        public static float Damage(float elapsed, int power, int recovery, int speed)
        {
            float charge = Charge(elapsed, recovery);
            // Convex recovery rewards deliberate pauses; minimum damage supports speed builds.
            return MinDamage(speed) + (MaxDamage(power, speed) - MinDamage(speed)) * charge * charge;
        }
    }
}
