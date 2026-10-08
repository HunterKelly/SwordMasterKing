using NUnit.Framework;

namespace SwordKing.Tests
{
    public sealed class SwingModelTests
    {
        [TestCase(10, 2, 0)]
        [TestCase(4, 4, 4)]
        [TestCase(0, 2, 10)]
        public void RecoveryPreservesStyleDamageEndpoints(int power, int recovery, int speed)
        {
            Assert.That(SwingModel.Damage(0, power, recovery, speed), Is.EqualTo(SwingModel.MinDamage(speed)).Within(.0001f));
            float duration = SwingModel.RecoverySeconds(recovery);
            Assert.That(SwingModel.Damage(duration, power, recovery, speed), Is.EqualTo(SwingModel.MaxDamage(power, speed)).Within(.0001f));
            Assert.That(SwingModel.Damage(duration * 10, power, recovery, speed), Is.EqualTo(SwingModel.MaxDamage(power, speed)).Within(.0001f));
        }
        [Test]
        public void HalfRecoveryRewardsWaitingWithQuadraticDamage()
        {
            float minimum = SwingModel.MinDamage(4), maximum = SwingModel.MaxDamage(4, 4);
            float damage = SwingModel.Damage(SwingModel.RecoverySeconds(4) * .5f, 4, 4, 4);
            Assert.That(damage, Is.EqualTo(minimum + (maximum - minimum) * .25f).Within(.0001f));
        }
        [Test]
        public void HigherRecoveryReachesFullChargeSooner()
        {
            Assert.That(SwingModel.RecoverySeconds(10), Is.LessThan(SwingModel.RecoverySeconds(0)));
            Assert.That(SwingModel.Charge(-1, 4), Is.Zero);
            Assert.That(SwingModel.Charge(100, 4), Is.EqualTo(1));
        }
        [Test]
        public void SpeedBuildRetainsHigherRateAndMinimumDamage()
        {
            Assert.That(SwingModel.MaxRate(0), Is.EqualTo(4));
            Assert.That(SwingModel.MaxRate(10), Is.EqualTo(12));
            Assert.That(SwingModel.MinDamage(10), Is.GreaterThan(SwingModel.MinDamage(0)));
        }
        [Test]
        public void DefaultEncountersPreserveSaveIndices()
        {
            Assert.DoesNotThrow(() => LevelDefinition.ValidateEncounters(LevelDefinition.DefaultEncounters()));
            var invalid = LevelDefinition.DefaultEncounters();
            invalid[0].id = 6;
            Assert.Throws<System.InvalidOperationException>(() => LevelDefinition.ValidateEncounters(invalid));
        }
    }
}
