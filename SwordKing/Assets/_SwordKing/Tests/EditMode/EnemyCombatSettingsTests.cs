using NUnit.Framework;

namespace SwordKing.Tests
{
    public sealed class EnemyCombatSettingsTests
    {
        [Test]
        public void DefaultsIncreasePursuitAndShortenTheAttackCycle()
        {
            var settings = new EnemyCombatSettings();
            Assert.That(settings.ChaseSpeed(false), Is.GreaterThan(2.25f));
            Assert.That(settings.ChaseSpeed(true), Is.GreaterThan(2.5f));
            Assert.That(settings.Windup(false, false), Is.LessThan(.9f));
            Assert.That(settings.Recovery(false, false), Is.LessThan(1.25f));
            Assert.That(settings.Windup(true, true), Is.LessThan(settings.Windup(true, false)));
            Assert.That(settings.Recovery(true, true), Is.LessThan(settings.Recovery(true, false)));
        }
        [Test]
        public void InvalidInspectorTimingsRetainReactionAndPunishWindows()
        {
            var settings = new EnemyCombatSettings { windup=-1, bossWindup=0, bossPhaseTwoWindup=-5, recovery=-1, bossRecovery=0, bossPhaseTwoRecovery=-2, moveSpeed=-1 };
            Assert.That(settings.Windup(false, false), Is.GreaterThanOrEqualTo(.25f));
            Assert.That(settings.Windup(true, false), Is.GreaterThanOrEqualTo(.25f));
            Assert.That(settings.Windup(true, true), Is.GreaterThanOrEqualTo(.25f));
            Assert.That(settings.Recovery(false, false), Is.GreaterThanOrEqualTo(.45f));
            Assert.That(settings.Recovery(true, true), Is.GreaterThanOrEqualTo(.45f));
            Assert.That(settings.ChaseSpeed(false), Is.GreaterThan(0));
        }
    }
}
