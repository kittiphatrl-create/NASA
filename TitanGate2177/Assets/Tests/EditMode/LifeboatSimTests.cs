// PROTOTYPE - NOT FOR PRODUCTION
// Question: Is Chapter 2 more fun as a read-and-choose survival diary (visitors, a pet, shared rations)
//           than as a walk-and-click outpost?
// Date: 2026-11-11
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TitanGate.Core;

namespace TitanGate.Tests
{
    /// <summary>A small lifeboat config so each test owns its numbers.</summary>
    static class Lifeboats
    {
        public const int StartO2 = 6, StartWater = 6, StartFood = 6, StartPower = 2, StealFood = 2;

        public static LifeboatConfig Basic()
        {
            return new LifeboatConfig
            {
                daysTotal = 7, sliceDays = 0, health = 3,
                start = new Stock { o2 = StartO2, water = StartWater, food = StartFood, power = StartPower },
                crewPerDay = new Stock { o2 = 1, water = 1, food = 1 },
                lifeSupportPower = 1, solarPerDay = 1, o2PenaltyNoPower = 1, o2PenaltyClogged = 1,
                pets = new List<PetDef>
                {
                    new PetDef { id = "dog", trait = "brave" },
                    new PetDef { id = "cat", trait = "smart" },
                    new PetDef { id = "rabbit", trait = "careful" },
                },
                visitors = new List<VisitorDef>
                {
                    new VisitorDef { id = "helper", perDay = new Stock { o2 = 1, water = 1, food = 1 }, giftPerDay = new Stock { power = 1 } },
                    new VisitorDef { id = "thief", perDay = new Stock { o2 = 1, water = 1, food = 1 }, steals = true, stealFood = StealFood },
                },
                cards = new List<LifeCard>
                {
                    new LifeCard
                    {
                        id = "door", day = 1, visitor = "helper",
                        options = new List<LifeOption>
                        {
                            new LifeOption { id = "admit_helper", admits = "helper" },
                            new LifeOption { id = "admit_thief", admits = "thief" },
                            new LifeOption { id = "refuse" },
                            new LifeOption { id = "use_item", needItem = "spare_filter", consumesItem = true },
                            new LifeOption { id = "send_pet", needTrait = "brave", delta = new Stock { food = 2 } },
                            new LifeOption { id = "pricey", delta = new Stock { water = -99 } },
                            new LifeOption { id = "clog", setsFlag = "filter_clogged" },
                            new LifeOption { id = "lock", setsFlag = "locker_locked", delta = new Stock { power = -1 } },
                        },
                    },
                },
            };
        }

        public static List<string> Everyone(LifeboatSim sim) => sim.Mouths();
    }

    public class LifeboatDayTests
    {
        [Test]
        public void Night_CadetUsesOneOfEach_PetEatsFood_SunPaysForLifeSupport()
        {
            var sim = new LifeboatSim(Lifeboats.Basic(), "cat");
            Assert.IsNull(sim.EndDay(Lifeboats.Everyone(sim)), "today's card must be answered before the night");
            Assert.IsNotNull(sim.Choose("door", "refuse"));
            var night = sim.EndDay(Lifeboats.Everyone(sim));
            Assert.IsNotNull(night);
            var s = sim.State;
            Assert.AreEqual(Lifeboats.StartO2 - 1, s.stock.o2);
            Assert.AreEqual(Lifeboats.StartWater - 1, s.stock.water);
            Assert.AreEqual(Lifeboats.StartFood - 2, s.stock.food, "cadet + pet");
            Assert.AreEqual(Lifeboats.StartPower, s.stock.power, "one unit of sun in, one unit of life support out");
            Assert.AreEqual(2, s.day);
            Assert.AreEqual(0, night.Count, "a quiet night");
        }

        [Test]
        public void Guest_BreathesEvenWhenNotFed_AndLeavesHungry()
        {
            var sim = new LifeboatSim(Lifeboats.Basic(), null);
            sim.Choose("door", "admit_helper");
            Assert.AreEqual(2, sim.O2Tonight, "air cannot be rationed");
            var night = sim.EndDay(new List<string> { LifeboatSim.Me });
            Assert.AreEqual(Lifeboats.StartO2 - 2, sim.State.stock.o2);
            Assert.AreEqual(Lifeboats.StartWater - 1, sim.State.stock.water, "the guest got no water");
            Assert.IsTrue(night.Exists(e => e.kind == LifeLogKind.GuestLeftHungry && e.id == "helper"));
            Assert.IsFalse(night.Exists(e => e.kind == LifeLogKind.Gift), "no share, no help");
            Assert.AreEqual(0, sim.State.guests.Count);
        }

        [Test]
        public void FedGuest_GivesItsGift_AndStays()
        {
            var sim = new LifeboatSim(Lifeboats.Basic(), null);
            sim.Choose("door", "admit_helper");
            var night = sim.EndDay(Lifeboats.Everyone(sim));
            Assert.IsTrue(night.Exists(e => e.kind == LifeLogKind.Gift && e.id == "helper"));
            Assert.AreEqual(Lifeboats.StartPower + 1, sim.State.stock.power);
            Assert.AreEqual(Lifeboats.StartFood - 2, sim.State.stock.food);
            CollectionAssert.Contains(sim.State.guests, "helper");
        }

        [Test]
        public void SkippingYourOwnRation_CostsHealth_AndThreeTimesFails()
        {
            var c = Lifeboats.Basic();
            c.cards.Clear();
            var sim = new LifeboatSim(c, null);
            var nobody = new List<string>();
            sim.EndDay(nobody);
            Assert.AreEqual(c.health - 1, sim.State.health);
            Assert.AreEqual(LifeOutcome.Ongoing, sim.State.outcome);
            sim.EndDay(nobody);
            sim.EndDay(nobody);
            Assert.AreEqual(LifeOutcome.FailedHealth, sim.State.outcome);
            Assert.IsNull(sim.EndDay(nobody), "nothing happens after the end");
        }

        [Test]
        public void HungryPet_GivesNoHint_UntilItIsFedAgain()
        {
            var c = Lifeboats.Basic();
            c.cards.Add(new LifeCard { id = "day2", day = 2, options = new List<LifeOption> { new LifeOption { id = "ok" }, new LifeOption { id = "send_pet", needTrait = "brave" } } });
            var sim = new LifeboatSim(c, "dog");
            Assert.AreEqual("life.card.door.hint.brave", sim.HintKey(sim.NextCard()));
            sim.Choose("door", "refuse");
            var night = sim.EndDay(new List<string> { LifeboatSim.Me });
            Assert.IsTrue(night.Exists(e => e.kind == LifeLogKind.PetHungry));
            var card = sim.NextCard();
            Assert.AreEqual("day2", card.id);
            Assert.IsNull(sim.HintKey(card));
            Assert.IsFalse(sim.CanChoose(card.options[1], out var reason));
            Assert.AreEqual("life.need.pet_fed", reason);
        }

        [Test]
        public void PlanThatCostsMoreThanTheStock_IsRefused()
        {
            var c = Lifeboats.Basic();
            c.cards.Clear();
            c.start.food = 1;
            var sim = new LifeboatSim(c, "cat");
            Assert.IsFalse(sim.CanFeed(sim.Mouths()), "one food cannot feed the cadet and the pet");
            Assert.IsNull(sim.EndDay(sim.Mouths()));
            Assert.IsTrue(sim.CanFeed(new List<string> { LifeboatSim.Me }));
        }

        [Test]
        public void LastDay_IsRescuedOnlyWithCoordinatesSent_AndAShortRunCanStopEarlier()
        {
            var c = Lifeboats.Basic();
            c.cards.Clear();
            c.start = new Stock { o2 = 20, water = 20, food = 20, power = 5 };
            var full = new LifeboatSim(c, null);
            for (int i = 0; i < 6; i++) full.EndDay(full.Mouths());
            Assert.AreEqual(LifeOutcome.Ongoing, full.State.outcome, "the start of day 7 is not the end of day 7");
            Assert.AreEqual(7, full.State.day);
            full.EndDay(full.Mouths());
            Assert.AreEqual(LifeOutcome.Stranded, full.State.outcome, "alive, but nobody was told where to land");
            Assert.AreEqual("no_beacon", full.StrandedReason);
            CollectionAssert.AreEqual(new[] { false, false, false }, full.Stars());

            var fixedOnly = new LifeboatSim(c, null);
            fixedOnly.State.flags.Add(c.beaconFlag);
            for (int i = 0; i < 7; i++) fixedOnly.EndDay(fixedOnly.Mouths());
            Assert.AreEqual(LifeOutcome.Stranded, fixedOnly.State.outcome);
            Assert.AreEqual("not_sent", fixedOnly.StrandedReason);

            var sent = new LifeboatSim(c, null);
            sent.State.flags.Add(c.beaconFlag); sent.State.flags.Add(c.rescueFlag);
            for (int i = 0; i < 7; i++) sent.EndDay(sent.Mouths());
            Assert.AreEqual(LifeOutcome.Rescued, sent.State.outcome);
            Assert.IsNull(sent.StrandedReason);
            CollectionAssert.AreEqual(new[] { true, true, false }, sent.Stars(), "rescued and healthy, but helped nobody");
            sent.State.flags.Add(c.kindFlag);
            CollectionAssert.AreEqual(new[] { true, true, true }, sent.Stars());

            c.sliceDays = 2;
            var slice = new LifeboatSim(c, null);
            slice.EndDay(slice.Mouths());
            Assert.AreEqual(LifeOutcome.Ongoing, slice.State.outcome);
            slice.EndDay(slice.Mouths());
            Assert.AreEqual(LifeOutcome.Survived, slice.State.outcome);
        }
    }

    public class LifeboatWeekTests
    {
        static LifeboatConfig TwoCards()
        {
            var c = Lifeboats.Basic();
            c.cards.Clear();
            c.cards.Add(new LifeCard
            {
                id = "first", day = 1,
                options = new List<LifeOption>
                {
                    new LifeOption { id = "fix", setsFlag = "fixed" },
                    new LifeOption { id = "admit", admits = "helper" },
                    new LifeOption { id = "none" },
                },
            });
            c.cards.Add(new LifeCard { id = "needs_fix", day = 1, ifFlag = "fixed", options = new List<LifeOption> { new LifeOption { id = "ok" } } });
            c.cards.Add(new LifeCard { id = "last_chance", day = 1, unlessFlag = "fixed", options = new List<LifeOption> { new LifeOption { id = "ok" } } });
            c.cards.Add(new LifeCard
            {
                id = "with_helper", day = 1, ifGuest = "helper",
                options = new List<LifeOption>
                {
                    new LifeOption { id = "ask", needGuest = "helper" },
                    new LifeOption { id = "flag", needFlag = "fixed" },
                    new LifeOption { id = "crank", health = -1 },
                },
            });
            return c;
        }

        [Test]
        public void Cards_AppearOnlyWhenTheirConditionHolds()
        {
            var a = new LifeboatSim(TwoCards(), null);
            a.Choose("first", "fix");
            Assert.AreEqual("needs_fix", a.NextCard().id, "an earlier answer opens a later card of the same day");
            a.CardProgress(out int done, out int total);
            Assert.AreEqual(1, done);
            Assert.AreEqual(2, total);
            a.Choose("needs_fix", "ok");
            Assert.IsNull(a.NextCard());

            var b = new LifeboatSim(TwoCards(), null);
            b.Choose("first", "none");
            Assert.AreEqual("last_chance", b.NextCard().id);

            var g = new LifeboatSim(TwoCards(), null);
            g.Choose("first", "admit");
            g.Choose("last_chance", "ok");
            Assert.AreEqual("with_helper", g.NextCard().id);
        }

        [Test]
        public void Options_CanNeedAFlagOrAGuest_AndCanCostHealthButNeverTheLastOfIt()
        {
            var c = TwoCards();
            var sim = new LifeboatSim(c, null);
            sim.Choose("first", "admit");
            sim.Choose("last_chance", "ok");
            var card = sim.NextCard();
            LifeOption O(string id) => card.options.Find(o => o.id == id);
            Assert.IsTrue(sim.CanChoose(O("ask"), out _));
            Assert.IsFalse(sim.CanChoose(O("flag"), out var why));
            Assert.AreEqual("life.need.flag.fixed", why);
            sim.State.health = 1;
            Assert.IsFalse(sim.CanChoose(O("crank"), out why));
            Assert.AreEqual("life.need.health", why);
            sim.State.health = 2;
            Assert.IsNotNull(sim.Choose("with_helper", "crank"));
            Assert.AreEqual(1, sim.State.health);

            var alone = new LifeboatSim(c, null);
            Assert.IsFalse(alone.CanChoose(O("ask"), out why));
            Assert.AreEqual("life.need.guest.helper", why);
        }

        [Test]
        public void StormNight_HurtsOnlyWhatWasNotPrepared_AndOnlyOnItsNight()
        {
            var c = Lifeboats.Basic();
            c.cards.Clear();
            c.start = new Stock { o2 = 20, water = 20, food = 20, power = 5 };
            c.nights.Add(new LifeNightDef { id = "storm_crew", day = 2, safeFlag = "shelter_ready", healthLoss = 1 });
            c.nights.Add(new LifeNightDef { id = "storm_power", day = 2, safeFlag = "electronics_safe", delta = new Stock { power = -2 } });

            var open = new LifeboatSim(c, null);
            var n1 = open.EndDay(open.Mouths());
            Assert.AreEqual(0, n1.Count, "the storm is tomorrow");
            var n2 = open.EndDay(open.Mouths());
            Assert.AreEqual(2, n2.FindAll(e => e.kind == LifeLogKind.NightHit).Count);
            Assert.AreEqual(c.health - 1, open.State.health);
            Assert.AreEqual(5 - 2, open.State.stock.power);
            var n3 = open.EndDay(open.Mouths());
            Assert.AreEqual(0, n3.Count, "it hits once");

            var ready = new LifeboatSim(c, null);
            ready.State.flags.Add("shelter_ready");
            ready.EndDay(ready.Mouths());
            var r2 = ready.EndDay(ready.Mouths());
            Assert.IsTrue(r2.Exists(e => e.kind == LifeLogKind.NightSafe && e.id == "storm_crew"));
            Assert.IsTrue(r2.Exists(e => e.kind == LifeLogKind.NightHit && e.id == "storm_power"));
            Assert.AreEqual(c.health, ready.State.health);
        }

        [Test]
        public void OxygenMaker_TurnsPowerIntoAir_WhenThereIsPowerForIt()
        {
            var c = Lifeboats.Basic();
            c.cards.Clear();
            c.o2Maker = new Stock { power = 2, o2 = 2 };
            var sim = new LifeboatSim(c, null);
            Assert.IsTrue(sim.CanRunMaker);
            var night = sim.EndDay(sim.Mouths(), true);
            Assert.IsTrue(night.Exists(e => e.kind == LifeLogKind.MakerRan));
            Assert.AreEqual(Lifeboats.StartO2 + 2 - 1, sim.State.stock.o2);
            Assert.AreEqual(Lifeboats.StartPower - 2, sim.State.stock.power, "maker -2, sun +1, life support -1");
            Assert.IsFalse(sim.CanRunMaker, "the battery is empty now");
            var second = sim.EndDay(sim.Mouths(), true);
            Assert.IsFalse(second.Exists(e => e.kind == LifeLogKind.MakerRan), "asking for it without power does nothing");
            Assert.AreEqual(Lifeboats.StartO2 + 2 - 2, sim.State.stock.o2);
        }
    }

    public class LifeboatAirTests
    {
        [Test]
        public void CloggedFilter_AndEmptyBattery_EachCostExtraO2()
        {
            var c = Lifeboats.Basic();
            var clogged = new LifeboatSim(c, null);
            clogged.Choose("door", "clog");
            Assert.AreEqual(2, clogged.O2Tonight);

            c.start.power = 0; c.solarPerDay = 0;
            var dark = new LifeboatSim(c, null);
            dark.Choose("door", "refuse");
            Assert.IsFalse(dark.PowerTonight);
            Assert.AreEqual(2, dark.O2Tonight);
            var night = dark.EndDay(dark.Mouths());
            Assert.IsTrue(night.Exists(e => e.kind == LifeLogKind.NoPower));
            Assert.AreEqual(Lifeboats.StartO2 - 2, dark.State.stock.o2);
            Assert.AreEqual(0, dark.State.stock.power, "power never goes below zero");
        }

        [Test]
        public void NotEnoughO2ForEveryoneInside_FailsTheRun()
        {
            var c = Lifeboats.Basic();
            c.start.o2 = 2;
            var sim = new LifeboatSim(c, null);
            sim.Choose("door", "admit_helper");
            sim.EndDay(sim.Mouths());
            Assert.AreEqual(LifeOutcome.Ongoing, sim.State.outcome, "night 1 used the last two units");
            sim.EndDay(sim.Mouths());
            Assert.AreEqual(LifeOutcome.FailedAir, sim.State.outcome);
        }

        [Test]
        public void LettingSomeoneIn_OrSpendingAir_IsRefused_WhenTonightsAirWouldNotCoverIt()
        {
            var c = Lifeboats.Basic();
            c.start.o2 = 1;
            var door = c.GetCard("door");
            door.options.Add(new LifeOption { id = "walk", delta = new Stock { o2 = -1, food = 2 } });
            var sim = new LifeboatSim(c, null);
            Assert.IsFalse(sim.CanChoose(door.options.Find(o => o.id == "admit_helper"), out var why), "one unit of air, two breathers");
            Assert.AreEqual("life.need.air", why);
            Assert.IsFalse(sim.CanChoose(door.options.Find(o => o.id == "walk"), out why), "the walk would leave nothing for the night");
            Assert.AreEqual("life.need.air", why);
            Assert.IsTrue(sim.CanChoose(door.options.Find(o => o.id == "refuse"), out _));
            sim.Choose("door", "refuse");
            sim.EndDay(sim.Mouths());
            Assert.AreEqual(LifeOutcome.Ongoing, sim.State.outcome, "exactly enough air is enough");
        }

        [Test]
        public void DaysOfSupplies_CountsTheWholeCrew()
        {
            var sim = new LifeboatSim(Lifeboats.Basic(), null);
            Assert.AreEqual(6, sim.DaysOfSupplies(), "alone: 6 of everything, 1 a day");
            sim.Choose("door", "admit_helper");
            Assert.AreEqual(3, sim.DaysOfSupplies(), "a guest halves it");
        }
    }

    public class LifeboatChoiceTests
    {
        [Test]
        public void Options_NeedTheirItem_TheirPetTrait_AndEnoughStock()
        {
            var c = Lifeboats.Basic();
            var door = c.GetCard("door");
            LifeOption O(string id) => door.options.Find(o => o.id == id);

            var bare = new LifeboatSim(c, "cat");
            Assert.IsFalse(bare.CanChoose(O("use_item"), out var why));
            Assert.AreEqual("life.need.item.spare_filter", why);
            Assert.IsFalse(bare.CanChoose(O("send_pet"), out why));
            Assert.AreEqual("life.need.trait.brave", why);
            Assert.IsFalse(bare.CanChoose(O("pricey"), out why));
            Assert.AreEqual("life.need.stock", why);
            Assert.IsNull(bare.Choose("door", "use_item"));
            Assert.IsTrue(bare.CanChoose(O("refuse"), out _), "there is always an answer that needs nothing");

            var kit = new LifeboatSim(c, "dog", null, new[] { "spare_filter" });
            Assert.IsNotNull(kit.Choose("door", "use_item"));
            CollectionAssert.DoesNotContain(kit.State.items, "spare_filter", "a one-shot item is used up");
            Assert.IsNull(kit.Choose("door", "refuse"), "a card is answered once");

            var brave = new LifeboatSim(c, "dog");
            Assert.IsNotNull(brave.Choose("door", "send_pet"));
            Assert.AreEqual(Lifeboats.StartFood + 2, brave.State.stock.food);
        }

        [Test]
        public void Thief_TakesFoodAndAnItem_ThenLeaves()
        {
            var sim = new LifeboatSim(Lifeboats.Basic(), "cat", null, new[] { "spare_filter", "med_kit" });
            sim.Choose("door", "admit_thief");
            var night = sim.EndDay(sim.Mouths());
            var theft = night.Find(e => e.kind == LifeLogKind.Theft);
            Assert.IsNotNull(theft);
            Assert.AreEqual(Lifeboats.StealFood, theft.value);
            Assert.AreEqual("med_kit", theft.detail, "items go in a fixed order so a run can be replayed");
            Assert.AreEqual(Lifeboats.StartFood - 3 - Lifeboats.StealFood, sim.State.stock.food, "cadet + pet + thief ate, then the theft");
            CollectionAssert.DoesNotContain(sim.State.items, "med_kit");
            CollectionAssert.Contains(sim.State.items, "spare_filter");
            Assert.AreEqual(0, sim.State.guests.Count);
        }

        [Test]
        public void Thief_IsStoppedByAFedBravePet_OrByTheLock()
        {
            var c = Lifeboats.Basic();
            c.cards.Add(new LifeCard { id = "evening", day = 1, options = new List<LifeOption> { new LifeOption { id = "lock", setsFlag = "locker_locked" }, new LifeOption { id = "open" } } });

            var guarded = new LifeboatSim(c, "dog", null, new[] { "med_kit" });
            guarded.Choose("door", "admit_thief");
            guarded.Choose("evening", "open");
            var n1 = guarded.EndDay(guarded.Mouths());
            Assert.IsTrue(n1.Exists(e => e.kind == LifeLogKind.TheftStopped && e.detail == "pet"));
            CollectionAssert.Contains(guarded.State.items, "med_kit");

            var hungryDog = new LifeboatSim(c, "dog", null, new[] { "med_kit" });
            hungryDog.Choose("door", "admit_thief");
            hungryDog.Choose("evening", "open");
            var n2 = hungryDog.EndDay(new List<string> { LifeboatSim.Me, "thief" });
            Assert.IsTrue(n2.Exists(e => e.kind == LifeLogKind.Theft), "a hungry guard dog does not guard");

            var locked = new LifeboatSim(c, "cat", null, new[] { "med_kit" });
            locked.Choose("door", "admit_thief");
            locked.Choose("evening", "lock");
            var n3 = locked.EndDay(locked.Mouths());
            Assert.IsTrue(n3.Exists(e => e.kind == LifeLogKind.TheftStopped && e.detail == "lock"));
            CollectionAssert.Contains(locked.State.items, "med_kit");
            CollectionAssert.DoesNotContain(locked.State.flags, "locker_locked", "the lock holds for one night");
        }

        [Test]
        public void ScrambleSupplies_BecomeStartingStock()
        {
            var c = Lifeboats.Basic();
            c.supplies.Add(new LifeSupplyDef { id = "sup_water", stock = new Stock { water = 3 } });
            var bonus = LifeboatSim.Bonus(c, new[] { "sup_water", "sup_unknown" });
            var sim = new LifeboatSim(c, null, bonus);
            Assert.AreEqual(Lifeboats.StartWater + 3, sim.State.stock.water);
        }
    }

    public class LifeboatVisitorTests
    {
        static LifeboatConfig WithMonster()
        {
            var c = Lifeboats.Basic();
            c.scans = new List<string> { "thermal", "breath", "heartbeat" };
            c.scansPerVisitor = 1;
            c.extraScanTrait = "careful";
            c.visitors.Add(new VisitorDef { id = "fake", perDay = new Stock { o2 = 1, water = 1, food = 1 }, monster = true, failsScans = new List<string> { "thermal", "breath" } });
            c.cards.Clear();
            c.cards.Add(new LifeCard
            {
                id = "knock", day = 1, visitor = "fake",
                options = new List<LifeOption> { new LifeOption { id = "admit", admits = "fake" }, new LifeOption { id = "refuse" } },
            });
            c.cards.Add(new LifeCard
            {
                id = "friend", day = 1, visitor = "helper",
                options = new List<LifeOption> { new LifeOption { id = "admit", admits = "helper" }, new LifeOption { id = "refuse" } },
            });
            return c;
        }

        [Test]
        public void LettingAMonsterIn_EndsTheRunAtOnce()
        {
            var sim = new LifeboatSim(WithMonster(), "cat");
            Assert.IsNotNull(sim.Choose("knock", "admit"));
            Assert.AreEqual(LifeOutcome.FailedMonster, sim.State.outcome);
            Assert.AreEqual("fake", sim.State.monsterLetIn);
            Assert.AreEqual(1, sim.State.day, "it ends before the night");
            CollectionAssert.DoesNotContain(sim.State.guests, "fake");
            Assert.IsNull(sim.NextCard());

            var careful = new LifeboatSim(WithMonster(), "cat");
            careful.Choose("knock", "refuse");
            Assert.AreEqual(LifeOutcome.Ongoing, careful.State.outcome);
            Assert.AreEqual("friend", careful.NextCard().id);
        }

        [Test]
        public void Scans_ReadWrongOnAMonster_NormalOnALivingVisitor_AndAreLimited()
        {
            var sim = new LifeboatSim(WithMonster(), "cat");
            var knock = sim.NextCard();
            Assert.AreEqual(1, sim.ScanBudget(knock));
            Assert.AreEqual(false, sim.Scan("knock", "heartbeat"), "a monster can pass one scanner");
            Assert.AreEqual(0, sim.ScansLeft(knock));
            Assert.IsNull(sim.Scan("knock", "breath"), "no scans left");
            Assert.IsNull(sim.Scan("friend", "breath"), "only the visitor at the hatch now can be scanned");
            sim.Choose("knock", "refuse");
            Assert.AreEqual(false, sim.Scan("friend", "breath"), "a living visitor reads normal");
            Assert.IsTrue(sim.State.log.Exists(e => e.kind == LifeLogKind.Scan && e.id == "helper" && e.detail == "breath"));
        }

        [Test]
        public void FedCarefulPet_GivesOneExtraScan()
        {
            var careful = new LifeboatSim(WithMonster(), "rabbit");
            var knock = careful.NextCard();
            Assert.AreEqual(2, careful.ScanBudget(knock));
            Assert.AreEqual(false, careful.Scan("knock", "heartbeat"));
            Assert.AreEqual(true, careful.Scan("knock", "breath"), "the second scan finds it");
            Assert.IsNull(careful.Scan("knock", "breath"), "the same scanner is not used twice");

            var hungry = new LifeboatSim(WithMonster(), "rabbit");
            hungry.State.petFed = false;
            Assert.AreEqual(1, hungry.ScanBudget(hungry.NextCard()), "a hungry pet does not help");
        }
    }

    /// <summary>Checks on the shipped lifeboat data (Config/Data: reads the real files on purpose).</summary>
    public class LifeboatShippedDataTests
    {
        static string Root => Path.Combine(UnityEngine.Application.dataPath, "Resources", "Data");
        static GameConfig Shipped() => GameConfig.FromJson(File.ReadAllText(Path.Combine(Root, "config.json")));
        static Localization Strings() => Localization.FromJson(File.ReadAllText(Path.Combine(Root, "strings.json")));

        [Test]
        public void Data_IsConsistent()
        {
            var g = Shipped();
            var c = g.lifeboat;
            Assert.AreEqual(3, c.pets.Count);
            Assert.Greater(c.cards.Count, 0);
            var ids = new HashSet<string>();
            foreach (var card in c.cards)
            {
                Assert.IsTrue(ids.Add(card.id), "duplicate card " + card.id);
                Assert.That(card.day, Is.InRange(1, c.daysTotal), card.id);
                Assert.GreaterOrEqual(card.options.Count, 2, card.id);
                if (!string.IsNullOrEmpty(card.visitor)) Assert.IsNotNull(c.GetVisitor(card.visitor), card.id);
                bool free = false;
                foreach (var o in card.options)
                {
                    if (!string.IsNullOrEmpty(o.admits)) Assert.IsNotNull(c.GetVisitor(o.admits), card.id + "." + o.id);
                    if (!string.IsNullOrEmpty(o.needTrait)) Assert.IsTrue(c.pets.Exists(p => p.trait == o.needTrait), card.id + "." + o.id);
                    if (!string.IsNullOrEmpty(o.needItem)) Assert.IsTrue(g.chapter1.supplies.Exists(s => s.flag == o.needItem), card.id + "." + o.id + " needs an item the scramble cannot give");
                    if (!string.IsNullOrEmpty(o.needGuest)) Assert.IsNotNull(c.GetVisitor(o.needGuest), card.id + "." + o.id);
                    if (string.IsNullOrEmpty(o.needItem) && string.IsNullOrEmpty(o.needTrait) && string.IsNullOrEmpty(o.admits)
                        && string.IsNullOrEmpty(o.needFlag) && string.IsNullOrEmpty(o.needGuest) && o.health >= 0 && o.delta.o2 >= 0 && o.delta.water >= 0 && o.delta.food >= 0 && o.delta.power >= 0) free = true;
                }
                Assert.IsTrue(free, card.id + ": every card needs one answer that asks for nothing");
            }
            for (int day = 1; day <= c.LastDay; day++) Assert.IsTrue(c.cards.Exists(x => x.day == day), "no card on day " + day);
            foreach (var id in c.demoStowed) Assert.IsTrue(g.chapter1.supplies.Exists(s => s.id == id), id);
            Assert.AreEqual(7, c.LastDay, "the shipped game plays the whole week");
            Assert.AreEqual(3, c.scans.Count);
            Assert.IsTrue(c.visitors.Exists(v => v.monster), "the week has monsters to find");
            foreach (var v in c.visitors)
            {
                foreach (var sc in v.failsScans) CollectionAssert.Contains(c.scans, sc, v.id);
                if (v.monster) Assert.GreaterOrEqual(v.failsScans.Count, 2, v.id + ": a monster must fail at least two scanners, so two scans always catch it");
                else Assert.AreEqual(0, v.failsScans.Count, v.id + ": a living visitor must read normal");
            }
            foreach (var x in c.cards)
                if (!string.IsNullOrEmpty(x.visitor) && c.GetVisitor(x.visitor).monster)
                    Assert.IsTrue(x.options.Exists(o => o.admits == x.visitor), x.id + ": the monster asks to come in");
            Assert.IsTrue(c.cards.Exists(x => x.options.Exists(o => o.setsFlag == c.beaconFlag)), "nothing repairs the beacon");
            Assert.IsTrue(c.cards.Exists(x => x.options.Exists(o => o.setsFlag == c.rescueFlag)), "nothing sends the coordinates");
            foreach (var n in c.nights) Assert.That(n.day, Is.InRange(1, c.daysTotal), n.id);
            foreach (var s in c.supplies) Assert.IsTrue(g.chapter1.supplies.Exists(x => x.id == s.id), s.id);
        }

        [Test]
        public void Strings_CoverEveryCardPetAndVisitor()
        {
            var g = Shipped();
            var c = g.lifeboat;
            var loc = Strings();
            var keys = new List<string>();
            foreach (var p in c.pets) { keys.Add($"pet.{p.id}.name"); keys.Add($"pet.{p.id}.desc"); keys.Add("trait." + p.trait); }
            foreach (var v in c.visitors)
            {
                keys.Add($"visitor.{v.id}.name");
                if (!v.giftPerDay.IsZero) keys.Add("life.night.Gift." + v.id);
            }
            foreach (var s in g.chapter1.supplies) if (!string.IsNullOrEmpty(s.flag)) keys.Add("life.item." + s.flag);
            foreach (var card in c.cards)
            {
                keys.Add($"life.card.{card.id}.title"); keys.Add($"life.card.{card.id}.body");
                for (int i = 1; i <= card.clues; i++) keys.Add($"life.card.{card.id}.clue{i}");
                foreach (var p in c.pets) keys.Add($"life.card.{card.id}.hint.{p.trait}");
                foreach (var o in card.options)
                {
                    keys.Add($"life.card.{card.id}.{o.id}"); keys.Add($"life.card.{card.id}.{o.id}.result");
                    if (!string.IsNullOrEmpty(o.needItem)) keys.Add("life.need.item." + o.needItem);
                    if (!string.IsNullOrEmpty(o.needTrait)) keys.Add("life.need.trait." + o.needTrait);
                }
            }
            keys.Add("life.need.air"); keys.Add("life.need.stock"); keys.Add("life.need.pet_fed"); keys.Add("life.need.health");
            foreach (var card in c.cards)
                foreach (var o in card.options)
                {
                    if (!string.IsNullOrEmpty(o.needFlag)) keys.Add("life.need.flag." + o.needFlag);
                    if (!string.IsNullOrEmpty(o.needGuest)) keys.Add("life.need.guest." + o.needGuest);
                }
            foreach (var n in c.nights) { keys.Add($"life.night.event.{n.id}.safe"); keys.Add($"life.night.event.{n.id}.hit"); }
            foreach (var sc in c.scans) { keys.Add($"life.scan.{sc}.name"); keys.Add($"life.scan.{sc}.how"); }
            foreach (var card in c.cards)
                if (!string.IsNullOrEmpty(card.visitor))
                    foreach (var sc in c.scans) keys.Add($"life.scan.{card.visitor}.{sc}");
            foreach (var sup in g.chapter1.supplies) keys.Add("life.sup." + sup.id);
            foreach (var k in new[] { "life.end.FailedMonster", "life.end.FailedMonster.scans", "life.end.FailedMonster.noscan", "life.scan.title", "life.scan.odd", "life.scan.normal", "life.pick.body_scramble", "route.lifeboat.name" }) keys.Add(k);
            foreach (var k in new[] { "life.end.Rescued.title", "life.end.Rescued.body", "life.end.Stranded.title", "life.end.stranded.no_beacon", "life.end.stranded.not_sent",
                "life.star.1", "life.star.2", "life.star.3", "life.maker.name", "life.maker.fact", "life.night.MakerRan" }) keys.Add(k);
            foreach (var kind in new[] { "Hungry", "PetHungry", "NoPower", "Theft", "Theft.item", "TheftStopped.pet", "TheftStopped.lock", "GuestLeftHungry" }) keys.Add("life.night." + kind);
            foreach (var o in new[] { LifeOutcome.FailedAir, LifeOutcome.FailedHealth }) keys.Add("life.end." + o);
            foreach (var k in keys) Assert.IsTrue(loc.Has(k), "missing string: " + k);
        }

        /// <summary>Everyone eats when the stock allows it; guests give way first, then the pet, the cadet last.</summary>
        static List<string> Plan(LifeboatSim sim)
        {
            var plan = sim.Mouths();
            while (!sim.CanFeed(plan)) plan.RemoveAt(plan.Count - 1);
            return plan;
        }

        /// <summary>A careful player's evening: run the oxygen maker only when the air will not last, and keep power for the beacon.</summary>
        static void CarefulNight(LifeboatSim sim)
        {
            var c = sim.Config;
            int left = c.LastDay - sim.State.day + 1;
            float keep = sim.State.flags.Contains(c.rescueFlag) ? 0 : 1;
            bool run = sim.CanRunMaker && sim.State.stock.o2 < sim.O2Tonight * left && sim.State.stock.power - c.o2Maker.power >= keep;
            Assert.IsNotNull(sim.EndDay(Plan(sim), run));
        }

        /// <summary>
        /// With nothing saved from the scramble, a player who fixes problems, trades instead of hosting, shelters from
        /// the storm and helps Nok is rescued with three stars, whichever pet they took. No answer here needs an item or a trait.
        /// </summary>
        [Test]
        public void MinimalLoot_CarefulPlayer_IsRescued_WithEveryPet()
        {
            var g = Shipped();
            var c = g.lifeboat;
            var plan = new Dictionary<string, string>
            {
                ["filter"] = "wash", ["visitor_tik"] = "trade", ["crate"] = "walk", ["visitor_mu"] = "refuse", ["locker"] = "open",
                ["recycler"] = "repair", ["storm_warning"] = "water_wall", ["visitor_zee"] = "buy_food", ["storm_power"] = "safe_mode",
                ["visitor_nok"] = "water", ["far_crate"] = "walk", ["beacon"] = "nok_part", ["coords"] = "send", ["log"] = "air",
                ["visitor_shade"] = "refuse", ["visitor_ree"] = "refuse", ["visitor_mimic"] = "refuse",
                ["visitor_aya"] = "refuse", ["visitor_jules"] = "refuse",
                ["visitor_pip"] = "refuse", ["pip_return"] = "refuse",
            };
            foreach (var pet in c.pets)
            {
                var sim = new LifeboatSim(c, pet.id);
                int guard = 0;
                while (sim.State.outcome == LifeOutcome.Ongoing && guard++ < 100)
                {
                    var card = sim.NextCard();
                    if (card == null) { CarefulNight(sim); continue; }
                    Assert.IsTrue(plan.ContainsKey(card.id), "unplanned card " + card.id);
                    Assert.IsNotNull(sim.Choose(card.id, plan[card.id]), $"pet={pet.id} day {sim.State.day}: {card.id} -> {plan[card.id]} was refused");
                }
                Assert.AreEqual(LifeOutcome.Rescued, sim.State.outcome, "pet=" + pet.id);
                Assert.AreEqual(7, sim.State.day);
                CollectionAssert.AreEqual(new[] { true, true, true }, sim.Stars(), "pet=" + pet.id);
            }
        }

        /// <summary>
        /// Random answers for a whole week, many times, with a fixed seed: no card is ever left without an answer,
        /// every run ends, all three endings are reachable, and the loot from the scramble helps.
        /// </summary>
        [Test]
        public void RandomWeeks_NeverDeadlock_ReachEveryEnding_AndLootHelps()
        {
            var g = Shipped();
            var c = g.lifeboat;
            const int Runs = 400;
            var seen = new HashSet<LifeOutcome>();
            var rescued = new Dictionary<string, int> { ["none"] = 0, ["demo"] = 0 };
            foreach (var pet in c.pets)
                foreach (var loot in new[] { "none", "demo" })
                {
                    var stowed = loot == "none" ? new List<string>() : c.demoStowed;
                    var rng = new System.Random(7);
                    for (int run = 0; run < Runs; run++)
                    {
                        var sim = new LifeboatSim(c, pet.id, LifeboatSim.Bonus(c, stowed), OutpostSim.SupplyFlags(g.chapter1, stowed));
                        int steps = 0;
                        while (sim.State.outcome == LifeOutcome.Ongoing)
                        {
                            Assert.Less(steps++, 200, "a run must end");
                            var card = sim.NextCard();
                            if (card == null) { Assert.IsNotNull(sim.EndDay(Plan(sim), rng.Next(2) == 0)); continue; }
                            var allowed = card.options.FindAll(o => sim.CanChoose(o, out _));
                            Assert.Greater(allowed.Count, 0, $"day {sim.State.day}: {card.id} has no answer left");
                            Assert.IsNotNull(sim.Choose(card.id, allowed[rng.Next(allowed.Count)].id));
                        }
                        seen.Add(sim.State.outcome);
                        Assert.AreNotEqual(LifeOutcome.Survived, sim.State.outcome, "the shipped week has real endings");
                        if (sim.State.outcome == LifeOutcome.Rescued) rescued[loot]++;
                        if (sim.State.outcome == LifeOutcome.Rescued || sim.State.outcome == LifeOutcome.Stranded) Assert.AreEqual(7, sim.State.day);
                    }
                }
            CollectionAssert.Contains(seen, LifeOutcome.Rescued);
            CollectionAssert.Contains(seen, LifeOutcome.Stranded);
            Assert.IsTrue(seen.Contains(LifeOutcome.FailedAir) || seen.Contains(LifeOutcome.FailedHealth), "a careless week can fail");
            Assert.Greater(rescued["demo"], rescued["none"] * 2, "strong loot should make rescue clearly more likely");
        }
    }
}
