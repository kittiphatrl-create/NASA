using NUnit.Framework;
using TitanGate.Core;

namespace TitanGate.Tests
{
    public class DailyScheduleTests
    {
        [Test] public void FailedWorkPreservesTimeReservedForStory()
        {
            var day = new DailySchedule();
            Assert.That(day.TrySpend(10, 4), Is.True);
            Assert.That(day.TrySpend(3, 4), Is.False);
            Assert.That(day.Hour, Is.EqualTo(16));
            Assert.That(day.TrySpend(4), Is.True);
        }
        [Test] public void NightBoundaryAndResetAreExact()
        {
            var day = new DailySchedule();
            Assert.That(day.TrySpend(16), Is.True);
            Assert.That(day.Hour, Is.EqualTo(22));
            Assert.That(day.TrySpend(.5f), Is.False);
            day.Reset(); Assert.That(day.Hour, Is.EqualTo(6));
        }
        [Test] public void InvalidDurationsCannotCreateTime()
        {
            var day = new DailySchedule();
            Assert.That(day.TrySpend(-1), Is.False);
            Assert.That(day.TrySpend(float.NaN), Is.False);
            Assert.That(day.TrySpend(float.PositiveInfinity), Is.False);
            Assert.That(day.TrySpend(1, -1), Is.False);
            Assert.That(day.Remaining, Is.EqualTo(16));
        }
    }
}
