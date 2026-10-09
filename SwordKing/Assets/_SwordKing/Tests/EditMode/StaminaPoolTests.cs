using NUnit.Framework;

namespace SwordKing.Tests
{
    public sealed class StaminaPoolTests
    {
        [Test]
        public void SprintRefillStartsAfterPointTwoSeconds()
        {
            var pool=new StaminaPool(); pool.Drain(100);
            pool.Tick(.2f,true); Assert.That(pool.Current,Is.EqualTo(0));
            pool.Tick(.5f,true); Assert.That(pool.Current,Is.EqualTo(30).Within(.001));
        }
        [Test]
        public void SprintDrainIsGradualAndStopsAtZero()
        {
            var pool=new StaminaPool(); pool.Drain(6*.5f);
            Assert.That(pool.Current,Is.EqualTo(97));
            pool.Drain(1000); Assert.That(pool.Current,Is.EqualTo(0));
            pool.Tick(.2f,true); Assert.That(pool.Current,Is.EqualTo(0));
            pool.Tick(.1f,true); Assert.That(pool.Current,Is.EqualTo(6).Within(.001));
        }
        [Test]
        public void PauseStopsRefillAndResetRestoresStamina()
        {
            var pool=new StaminaPool(); pool.Drain(35);
            pool.Tick(10,false); Assert.That(pool.Current,Is.EqualTo(65));
            pool.Reset(); Assert.That(pool.Current,Is.EqualTo(100));
        }
        [Test]
        public void RefillCapsAtMaximumAndDoesNotDependOnFrameSize()
        {
            var one=new StaminaPool(); var many=new StaminaPool(); one.Drain(70); many.Drain(70);
            one.Tick(1,true); for(int i=0;i<100;i++) many.Tick(.01f,true);
            Assert.That(one.Current,Is.EqualTo(many.Current).Within(.001));
            one.Tick(10,true); Assert.That(one.Current,Is.EqualTo(100));
        }
    }
}
