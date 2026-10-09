using NUnit.Framework;

namespace SwordKing.Tests
{
    public sealed class AttackInputBufferTests
    {
        [Test]
        public void EarlyPressWaitsForCooldownAndFiresOnlyOnce()
        {
            var input=new AttackInputBuffer(); input.Press(1);
            Assert.That(input.Consume(1.05f,false),Is.False);
            Assert.That(input.Consume(1.10f,true),Is.True);
            Assert.That(input.Consume(1.11f,true),Is.False);
        }
        [Test]
        public void OldAndCancelledPressesDoNotAttackLater()
        {
            var input=new AttackInputBuffer(); input.Press(1);
            Assert.That(input.Consume(1.16f,true),Is.False);
            input.Press(2); input.Clear();
            Assert.That(input.Consume(2.01f,true),Is.False);
        }
        [Test]
        public void RepeatedPressesDoNotCreateAnAttackBacklog()
        {
            var input=new AttackInputBuffer(); input.Press(1); input.Press(1.05f);
            Assert.That(input.Consume(1.17f,true),Is.True);
            Assert.That(input.Consume(1.18f,true),Is.False);
        }
        [Test]
        public void DefaultFeedbackDoesNotFreezeTheAttackClock()
        {
            Assert.That(new CombatFeedbackSettings().hitStop,Is.False);
        }
    }
}
