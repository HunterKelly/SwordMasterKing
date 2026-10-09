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
        [TestCase(PlayerAttackKind.Slash)]
        [TestCase(PlayerAttackKind.Overhead)]
        [TestCase(PlayerAttackKind.Thrust)]
        [TestCase(PlayerAttackKind.JumpingOverhead)]
        public void EveryAttackChargesMonotonicallyAndCapsAtTwoSeconds(PlayerAttackKind kind)
        {
            float tap=PlayerAttackModel.DamageMultiplier(kind,0,2);
            float half=PlayerAttackModel.DamageMultiplier(kind,1,2);
            float full=PlayerAttackModel.DamageMultiplier(kind,2,2);
            Assert.That(half,Is.GreaterThan(tap)); Assert.That(full,Is.GreaterThan(half));
            Assert.That(full,Is.EqualTo(tap*2).Within(.0001));
            Assert.That(PlayerAttackModel.DamageMultiplier(kind,20,2),Is.EqualTo(full));
        }
    }
}
