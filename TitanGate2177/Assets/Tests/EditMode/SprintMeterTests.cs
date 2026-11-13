using NUnit.Framework;
using TitanGate.Core;

namespace TitanGate.Tests
{
    public class SprintMeterTests
    {
        [Test] public void ExhaustionRequiresRecoveryBeforeSprintingAgain()
        {
            var s = new SprintMeter(); s.Tick(5, true);
            Assert.That(s.Value, Is.EqualTo(0)); Assert.That(s.CanSprint, Is.False);
            s.Tick(1, false); Assert.That(s.CanSprint, Is.False);
            s.Tick(2, false); Assert.That(s.CanSprint, Is.True);
        }
        [Test] public void PauseAndInvalidTimeCannotChangeStamina()
        {
            var s = new SprintMeter(); s.Tick(0, true); s.Tick(float.NaN, true); s.Tick(-2, true);
            Assert.That(s.Value, Is.EqualTo(100));
        }
        [Test] public void RecoveryWaitsThenClampsToFull()
        {
            var s = new SprintMeter(); s.Tick(1, true); s.Tick(.5f, false);
            Assert.That(s.Value, Is.EqualTo(77).Within(.001));
            s.Tick(20, false); Assert.That(s.Value, Is.EqualTo(100));
        }
    }
}
