using System.Collections.Generic;
using NUnit.Framework;
using TitanGate.Core;

namespace TitanGate.Tests
{
    public class TemporaryVisitorTests
    {
        [Test] public void FedTemporaryVisitorHelpsThenLeavesAfterOneNight()
        {
            var c = Lifeboats.Basic(); c.GetVisitor("helper").leavesAfterNight = true;
            var sim = new LifeboatSim(c, null); sim.Choose("door", "admit_helper");
            var events = sim.EndDay(Lifeboats.Everyone(sim));
            Assert.That(events.Exists(e => e.kind == LifeLogKind.Gift && e.id == "helper"));
            Assert.That(events.Exists(e => e.kind == LifeLogKind.GuestDeparted && e.id == "helper"));
            Assert.That(sim.State.guests, Is.Empty);
            Assert.That(events.Exists(e => e.kind == LifeLogKind.GuestLeftHungry), Is.False);
        }
        [Test] public void PermanentVisitorDoesNotDepartWithTemporaryGuests()
        {
            var c = Lifeboats.Basic(); var sim = new LifeboatSim(c, null);
            sim.Choose("door", "admit_helper"); var events = sim.EndDay(Lifeboats.Everyone(sim));
            Assert.That(sim.State.guests, Does.Contain("helper"));
            Assert.That(events.Exists(e => e.kind == LifeLogKind.GuestDeparted), Is.False);
        }
    }
}
