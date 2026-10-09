using NUnit.Framework;

namespace SwordKing.Tests
{
    public sealed class PlayerAttackModelTests
    {
        [Test]
        public void ShiftTapRollsOnceOnRelease()
        {
            var shift=new ShiftGesture();
            Assert.That(shift.Tick(true,0,.18f),Is.False);
            Assert.That(shift.SprintHeld,Is.False);
            Assert.That(shift.Tick(false,.1f,.18f),Is.True);
            Assert.That(shift.Tick(false,.11f,.18f),Is.False);
        }
        [Test]
        public void SprintHoldNeverRollsOnRelease()
        {
            var shift=new ShiftGesture(); shift.Tick(true,0,.18f);
            Assert.That(shift.Tick(true,.2f,.18f),Is.False);
            Assert.That(shift.SprintHeld,Is.True);
            Assert.That(shift.Tick(false,.5f,.18f),Is.False);
            Assert.That(shift.SprintHeld,Is.False);
        }
        [Test]
        public void ClearingShiftCancelsPendingTap()
        {
            var shift=new ShiftGesture(); shift.Tick(true,0,.18f); shift.Clear();
            Assert.That(shift.Tick(false,.1f,.18f),Is.False);
        }
        [TestCase(false,false,PlayerAttackKind.Slash)]
        [TestCase(true,false,PlayerAttackKind.Overhead)]
        [TestCase(false,true,PlayerAttackKind.Thrust)]
        [TestCase(true,true,PlayerAttackKind.JumpingOverhead)]
        public void MovementAndButtonSelectAttack(bool overhead,bool sprint,PlayerAttackKind expected)
        { Assert.That(PlayerAttackModel.Kind(overhead,sprint),Is.EqualTo(expected)); }
        [Test]
        public void WalkingChargeSpinsButQuickClicksAndSprintRetainTheirAttack()
        {
            Assert.That(PlayerAttackModel.ChargedKind(false,false,.1f),Is.EqualTo(PlayerAttackKind.Slash));
            Assert.That(PlayerAttackModel.ChargedKind(false,false,.25f),Is.EqualTo(PlayerAttackKind.Spin));
            Assert.That(PlayerAttackModel.ChargedKind(false,true,2),Is.EqualTo(PlayerAttackKind.Thrust));
            Assert.That(PlayerAttackModel.ChargedKind(true,false,2),Is.EqualTo(PlayerAttackKind.ChargedOverhead));
        }
        [Test]
        public void SpinChargesInPointSixSecondsAndHeavyVariantsHaveHigherBaseDamage()
        {
            Assert.That(PlayerAttackModel.ChargeSeconds(PlayerAttackKind.Spin),Is.EqualTo(.6f));
            Assert.That(PlayerAttackModel.ChargeSeconds(PlayerAttackKind.Overhead),Is.EqualTo(.6f));
            Assert.That(PlayerAttackModel.ChargeSeconds(PlayerAttackKind.ChargedOverhead),Is.EqualTo(.6f));
            Assert.That(PlayerAttackModel.ChargeFraction(.6f,PlayerAttackKind.ChargedOverhead),Is.EqualTo(1));
            Assert.That(PlayerAttackModel.ChargeSeconds(PlayerAttackKind.JumpingOverhead),Is.EqualTo(2));
            Assert.That(PlayerAttackModel.DamageMultiplier(PlayerAttackKind.ChargedOverhead,2,2),
                Is.GreaterThan(PlayerAttackModel.DamageMultiplier(PlayerAttackKind.Overhead,2,2)));
            Assert.That(PlayerAttackModel.DamageMultiplier(PlayerAttackKind.JumpingOverhead,0,2),Is.EqualTo(3.5f));
        }
        [Test]
        public void SustainedAttackHitsEachTargetOnceAndCanAcquireNewTargets()
        {
            var window=new HitOnceWindow(); window.Begin(1,.45f);
            Assert.That(window.TryHit(10,1),Is.True);
            Assert.That(window.TryHit(10,1.2f),Is.False);
            Assert.That(window.TryHit(20,1.3f),Is.True);
            Assert.That(window.TryHit(30,1.5f),Is.False);
            window.Begin(2,.45f);
            Assert.That(window.TryHit(10,2),Is.True);
            window.Cancel(); Assert.That(window.TryHit(20,2.1f),Is.False);
        }
        [TestCase(PlayerAttackKind.ChargedOverhead)]
        [TestCase(PlayerAttackKind.Spin)]
        [TestCase(PlayerAttackKind.Slash)]
        [TestCase(PlayerAttackKind.Overhead)]
        [TestCase(PlayerAttackKind.Thrust)]
        [TestCase(PlayerAttackKind.JumpingOverhead)]
        public void EveryAttackChargesMonotonicallyAndCapsAtItsChargeLimit(PlayerAttackKind kind)
        {
            float tap=PlayerAttackModel.DamageMultiplier(kind,0,2);
            float half=PlayerAttackModel.DamageMultiplier(kind,PlayerAttackModel.ChargeSeconds(kind)*.5f,2);
            float full=PlayerAttackModel.DamageMultiplier(kind,PlayerAttackModel.ChargeSeconds(kind),2);
            Assert.That(half,Is.GreaterThan(tap)); Assert.That(full,Is.GreaterThan(half));
            Assert.That(full,Is.EqualTo(tap*2).Within(.0001));
            Assert.That(PlayerAttackModel.DamageMultiplier(kind,20,2),Is.EqualTo(full));
        }
    }
}
