// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-11
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TitanGate.Core;

namespace TitanGate.Tests
{
    /// <summary>Small outpost configs so each test owns its numbers.</summary>
    static class Outposts
    {
        public static OutpostConfig Basic()
        {
            return new OutpostConfig
            {
                hoursTotal = 168, landingClock = 6, maxValue = 100,
                start = new ResourceDelta { air = 70, power = 50, shelter = 0, supplies = 60, energy = 80 },
                airUsePerHour = 1.4f, lifeSupportAirPerHour = 1f,
                systems = new List<SystemDef>
                {
                    new SystemDef { id = "life_support", powerPerHour = 2.5f, onAtStart = true },
                    new SystemDef { id = "greenhouse", powerPerHour = 2f },
                    new SystemDef { id = "beacon", powerPerHour = 1f },
                    new SystemDef { id = "shelter", powerPerHour = 2f },
                },
                solarPerHour = 5.5f, daylightFrom = 7, daylightTo = 19, dustPerDay = 0.12f, minEfficiency = 0.4f,
                suppliesUsePerHour = 0.45f, energyAwakePerHour = -1.4f, energySleepPerHour = 7f,
                lowEnergy = 20, lowEnergyExtraHours = 1, collapseSleepHours = 6, suppliesZeroFailHours = 24,
                warnAir = 25, radPerHour = 0.029f, radPerOutsideHour = 0.05f, radSafe = 50, radLimit = 150,
                spe = new SpeDef { hour = 84, durationHours = 6, warningHour = 28, dose = 80, energyLoss = 40, airLoss = 35, powerLoss = 40, unpoweredShelterFactor = 0.5f },
                relayClocks = new[] { 10, 22 }, rescuePassEveryHours = 6, greenhouseHoursToReady = 24,
                nightEventClock = 2, randomEventDays = new int[0], encounterClock = 8, encounterDays = new int[0],
                actions = new List<OutpostAction>
                {
                    new OutpostAction { id = "moxie", hours = 2, delta = new ResourceDelta { power = -6, air = 14 } },
                    new OutpostAction { id = "eat", hours = 1, delta = new ResourceDelta { supplies = -6, energy = 25 } },
                    new OutpostAction { id = "walk", hours = 3, delta = new ResourceDelta { energy = -12 }, outside = true },
                    new OutpostAction { id = "stow", hours = 1, setsFlag = "panels_stowed", solFlag = true },
                    new OutpostAction { id = "shelter_kit", hours = 1, delta = new ResourceDelta { shelter = 40 }, requiresFlag = "shield_kit", consumesFlag = true },
                    new OutpostAction { id = "send_coords", hours = 1, needsSystem = "beacon" },
                    new OutpostAction { id = "ask_earth", hours = 1 },
                    new OutpostAction { id = "sci", hours = 1, science = true, minDay = 3, setsFlag = "sci" },
                },
            };
        }
    }

    public class OutpostClockTests
    {
        [Test]
        public void Clock_StartsAtDawnOfDay1_AndDayChangesAt0600()
        {
            var sim = new OutpostSim(Outposts.Basic(), 1);
            Assert.AreEqual(6, sim.Clock);
            Assert.AreEqual(1, sim.Day);
            sim.State.power = 100; sim.State.supplies = 100;
            sim.Advance(23, true);
            Assert.AreEqual(5, sim.Clock);
            Assert.AreEqual(1, sim.Day);
            sim.Advance(1, true);
            Assert.AreEqual(2, sim.Day);
            Assert.AreEqual(144, sim.HoursLeft);
        }

        [Test]
        public void Rescue_IsCheckedAtHour168_PreparedWins_UnpreparedIsStranded()
        {
            var c = Outposts.Basic();
            c.start.supplies = 100; c.suppliesUsePerHour = 0.1f; c.airUsePerHour = 1f;
            var ready = new OutpostSim(c, 1);
            ready.SetSystem("beacon", true);
            ready.State.power = 100;
            Assert.IsNotNull(ready.Do("send_coords"));
            for (int i = 0; i < 200 && ready.State.outcome == Outcome.Ongoing; i++) { ready.State.power = 100; ready.State.energy = 80; ready.Advance(1); }
            Assert.AreEqual(Outcome.Rescued, ready.State.outcome);
            Assert.AreEqual(168, ready.State.hour);

            var lazy = new OutpostSim(c, 1);
            for (int i = 0; i < 200 && lazy.State.outcome == Outcome.Ongoing; i++) { lazy.State.power = 100; lazy.State.energy = 80; lazy.Advance(1); }
            Assert.AreEqual(Outcome.Stranded, lazy.State.outcome, "alive but no coordinates: stranded, not a timeout game over");
        }
    }

    public class OutpostSystemTests
    {
        [Test]
        public void LifeSupport_SlowsAirLoss_AndCostsPower()
        {
            var sim = new OutpostSim(Outposts.Basic(), 1);
            sim.State.power = 90;
            float air = sim.State.air;
            sim.Advance(1);                       // 06:00-07:00, no sun yet
            Assert.AreEqual(air - 1.4f + 1f, sim.State.air, 1e-3);
            Assert.AreEqual(90 - 2.5f, sim.State.power, 1e-3);
            sim.SetSystem("life_support", false);
            air = sim.State.air;
            sim.Advance(1);
            Assert.AreEqual(air - 1.4f, sim.State.air, 1e-3);
        }

        [Test]
        public void Solar_OnlyInDaylight_ScaledByDust()
        {
            var c = Outposts.Basic();
            c.systems.ForEach(s => s.onAtStart = false);
            var sim = new OutpostSim(c, 1);
            float p = sim.State.power;
            sim.Advance(1);                       // 06:00 -> no sun
            Assert.AreEqual(p, sim.State.power, 1e-3);
            sim.State.efficiency = 0.5f;
            sim.Advance(1);                       // 07:00 -> sun
            Assert.AreEqual(p + 5.5f * 0.5f, sim.State.power, 1e-3);
        }

        [Test]
        public void NotEnoughPower_SwitchesASystemOff_AndLogsIt()
        {
            var sim = new OutpostSim(Outposts.Basic(), 1);
            sim.State.power = 1f;
            var r = sim.Advance(1);
            Assert.IsFalse(sim.IsOn("life_support"));
            Assert.IsTrue(r.events.Exists(e => e.kind == LogKind.PowerOut && e.id == "life_support"));
        }

        [Test]
        public void PowerNeededUntilSunrise_CountsSwitchedOnSystems()
        {
            var sim = new OutpostSim(Outposts.Basic(), 1);
            sim.SetSystem("beacon", true);
            Assert.AreEqual((2.5f + 1f) * 1, sim.PowerNeededUntilSunrise, 1e-3, "06:00 -> sun at 07:00");
        }
    }

    public class OutpostActionTests
    {
        [Test]
        public void Action_AppliesCosts_ThenItsHoursPass()
        {
            var sim = new OutpostSim(Outposts.Basic(), 1);
            sim.State.air = 50;
            var r = sim.Do("moxie");
            Assert.AreEqual(2, r.hours);
            Assert.AreEqual(2, sim.State.hour);
            Assert.AreEqual(50 + 14 - 2 * 0.4f, sim.State.air, 1e-3);
        }

        [Test]
        public void LowEnergy_CostsAnExtraHour_AndZeroEnergyCollapses()
        {
            var sim = new OutpostSim(Outposts.Basic(), 1);
            sim.State.energy = 15;
            Assert.AreEqual(4, sim.HoursFor(sim.Config.GetAction("walk")));
            sim.State.energy = 5;
            var r = sim.Do("walk");
            Assert.AreEqual(1, sim.State.collapses);
            Assert.IsTrue(r.stoppedEarly);
            Assert.IsTrue(r.events.Exists(e => e.kind == LogKind.Collapse));
        }

        [Test]
        public void Outside_AddsDose_AndMinDayGatesScience()
        {
            var sim = new OutpostSim(Outposts.Basic(), 1);
            sim.Do("walk");
            Assert.AreEqual(3 * 0.05f + 3 * 0.029f, sim.State.dose, 1e-3);
            Assert.IsFalse(sim.CanDo("sci", out var why));
            Assert.AreEqual("reason.later", why);
        }

        [Test]
        public void ItemAction_UsesUpItsFlag()
        {
            var sim = new OutpostSim(Outposts.Basic(), 1, null, new[] { "shield_kit" });
            Assert.IsNotNull(sim.Do("shelter_kit"));
            Assert.AreEqual(40, sim.State.shelter, 1e-3);
            Assert.IsFalse(sim.CanDo("shelter_kit", out var why));
            Assert.AreEqual("reason.need.shield_kit", why);
        }

        [Test]
        public void SendCoords_NeedsTheBeaconOn_AndIsAcknowledgedOnARescuePass()
        {
            var sim = new OutpostSim(Outposts.Basic(), 1);
            Assert.IsFalse(sim.CanDo("send_coords", out var why));
            Assert.AreEqual("reason.system_off.beacon", why);
            sim.SetSystem("beacon", true);
            sim.Do("send_coords");
            Assert.AreEqual(1, sim.State.coords);
            sim.Advance(6 - sim.State.hour % 6);
            Assert.AreEqual(2, sim.State.coords);
        }
    }

    public class OutpostEventTests
    {
        [Test]
        public void Sleep_RestoresEnergy_ButALowAirAlarmWakesYou()
        {
            var sim = new OutpostSim(Outposts.Basic(), 1);
            sim.State.energy = 10;
            sim.Advance(4, true);
            Assert.AreEqual(10 + 28, sim.State.energy, 1e-3);
            sim.SetSystem("life_support", false);
            sim.State.air = 27;
            var r = sim.Advance(8, true);
            Assert.IsTrue(r.stoppedEarly);
            Assert.IsTrue(r.events.Exists(e => e.kind == LogKind.WakeAlarm));
            Assert.AreEqual(Outcome.Ongoing, sim.State.outcome);
        }

        [Test]
        public void NightEvent_PreventedBySolFlag_ElseHurts()
        {
            var c = Outposts.Basic();
            c.nightEvents = new List<NightEventDef> { new NightEventDef { id = "dust_devil", preventFlag = "panels_stowed", efficiencyLoss = 0.25f } };
            c.randomEventDays = new[] { 1, 2 };
            var sim = new OutpostSim(c, 1);
            sim.State.supplies = 100; sim.State.power = 100;
            sim.Do("stow");
            sim.Advance(sim.HoursToMorning, true);
            Assert.AreEqual(1f - 0.12f, sim.State.efficiency, 1e-3, "prevented: only the daily dust");
            sim.State.power = 100;
            sim.Advance(24, true);
            Assert.AreEqual(1f - 0.12f - 0.25f - 0.12f, sim.State.efficiency, 1e-3);
            Assert.IsTrue(sim.State.log.Exists(e => e.kind == LogKind.Night && !e.ok));
        }

        [Test]
        public void SolarParticleEvent_HitsOnce_ShelterAndPowerDecideHowHard()
        {
            OutpostSim Run(float shelter, bool powered)
            {
                var c = Outposts.Basic();
                c.airUsePerHour = 0; c.suppliesUsePerHour = 0; c.energyAwakePerHour = 0; c.radPerHour = 0;
                c.systems.ForEach(s => s.powerPerHour = 0);
                var sim = new OutpostSim(c, 1);
                sim.State.shelter = shelter;
                sim.SetSystem("shelter", powered);
                sim.Advance(90);
                return sim;
            }
            var none = Run(0, false);
            var half = Run(100, false);
            var full = Run(100, true);
            Assert.AreEqual(80, none.State.dose, 1e-3);
            Assert.AreEqual(40, half.State.dose, 1e-3, "an unpowered shelter protects half as well");
            Assert.AreEqual(0, full.State.dose, 1e-3);
            Assert.AreEqual(1, none.State.log.FindAll(e => e.kind == LogKind.Spe).Count);
        }

        [Test]
        public void EarthMessages_RideTheRelayPasses()
        {
            var sim = new OutpostSim(Outposts.Basic(), 1);
            sim.State.power = 100; sim.State.supplies = 100;
            int wait = sim.HoursUntilReply();
            sim.Do("ask_earth");
            Assert.IsFalse(sim.CanDo("ask_earth", out _), "one question at a time");
            sim.Advance(wait - 2, true);
            Assert.IsFalse(sim.State.messages.Find(m => m.id == "earth_hint").delivered);
            sim.State.power = 100;
            sim.Advance(2, true);
            Assert.IsTrue(sim.State.messages.Find(m => m.id == "earth_hint").delivered);
        }

        [Test]
        public void SpeWarning_ArrivesAtItsHour()
        {
            var sim = new OutpostSim(Outposts.Basic(), 1);
            sim.State.supplies = 100;
            for (int i = 0; i < 28; i++) { sim.State.power = 100; sim.Advance(1, true); }
            Assert.IsTrue(sim.State.messages.Find(m => m.id == "earth_spe_warning").delivered);
        }

        [Test]
        public void NoSupplies_For24Hours_FailsTheMission()
        {
            var sim = new OutpostSim(Outposts.Basic(), 1);
            sim.State.supplies = 0;
            for (int i = 0; i < 30 && sim.State.outcome == Outcome.Ongoing; i++) { sim.State.air = 80; sim.State.power = 80; sim.State.energy = 80; sim.Advance(1); }
            Assert.AreEqual(Outcome.FailedSupplies, sim.State.outcome);
        }

        [Test]
        public void Encounters_DrawnOnFreeMornings_OptionNeedsItsSupply()
        {
            var c = Outposts.Basic();
            c.encounterDays = new[] { 2, 3 };
            c.dilemmas = new List<DilemmaDef>
            {
                new DilemmaDef { id = "fixed3", day = 3, options = new List<DilemmaOption> { new DilemmaOption { id = "x" }, new DilemmaOption { id = "y" } } },
                new DilemmaDef { id = "cut", random = true, options = new List<DilemmaOption>
                    { new DilemmaOption { id = "medkit", needFlag = "med_kit" }, new DilemmaOption { id = "ignore", delta = new ResourceDelta { energy = -20 } } } },
                new DilemmaDef { id = "other", random = true, options = new List<DilemmaOption> { new DilemmaOption { id = "ok" }, new DilemmaOption { id = "no" } } },
            };
            var sim = new OutpostSim(c, 1);
            sim.State.supplies = 100;
            Assert.IsNull(sim.DueDilemma(), "day 1 has no event");
            sim.State.power = 100; sim.Advance(24, true);
            var d = sim.DueDilemma();
            Assert.IsTrue(d.random);
            if (d.id == "cut")
            {
                Assert.IsFalse(sim.CanChoose(d.options[0], out var why));
                Assert.AreEqual("dilemma.need.med_kit", why);
            }
            sim.Choose(d.id, d.options[d.options.Count - 1].id);
            sim.State.power = 100; sim.Advance(24, true);
            Assert.AreEqual("fixed3", sim.DueDilemma().id, "a fixed dilemma takes its morning");
            Assert.IsNull(sim.State.encounterToday);
        }

        [Test]
        public void CarryOver_CabinAirAndSupplies()
        {
            var c = new Chapter1Config { carryO2Pivot = 50, carryO2Factor = 0.3f };
            c.supplies.Add(new SupplyDef { id = "o2", delta = new ResourceDelta { air = 12 } });
            c.supplies.Add(new SupplyDef { id = "kit", flag = "med_kit" });
            Assert.AreEqual(15, OutpostSim.CarryOver(c, 100, null).air, 1e-3);
            Assert.AreEqual(-12, OutpostSim.CarryOver(c, 10, null).air, 1e-3);
            Assert.AreEqual(12, OutpostSim.CarryOver(c, 50, new[] { "o2", "kit" }).air, 1e-3);
            CollectionAssert.AreEqual(new[] { "med_kit" }, OutpostSim.SupplyFlags(c, new[] { "o2", "kit" }));
        }
    }

    public class PuzzleTests
    {
        static PuzzleConfig P() => new PuzzleConfig
        {
            lockerCode = "7", podCode = "39", fuelGaugePercent = 18,
            navOptions = new List<NavOption>
            {
                new NavOption { id = "titan", fuelNeeded = 60, route = "" },
                new NavOption { id = "earth", fuelNeeded = 35, route = "" },
                new NavOption { id = "mars", fuelNeeded = 15, route = "mars" },
            },
            edlOrder = new List<string> { "edl_entry", "edl_chute", "edl_shield", "edl_rockets" },
            recipes = new List<Recipe> { new Recipe { a = "item_plate", b = "item_sealant", result = "item_hull_patch" } },
        };

        [Test]
        public void Code_TrimmedExactMatch()
        {
            Assert.IsTrue(Puzzles.CheckCode(" 39 ", "39"));
            Assert.IsFalse(Puzzles.CheckCode("3", "39"));
            Assert.IsFalse(Puzzles.CheckCode("", "39"));
        }

        [Test]
        public void Nav_OnlyMarsIsReachable()
        {
            var p = P();
            Assert.IsTrue(Puzzles.CanReach(p, "mars"));
            Assert.IsFalse(Puzzles.CanReach(p, "earth"));
            Assert.IsFalse(Puzzles.CanReach(p, "titan"));
            Assert.AreEqual("mars", Puzzles.RouteFor(p, "mars"));
            Assert.IsNull(Puzzles.RouteFor(p, "titan"));
            Assert.IsNull(Puzzles.RouteFor(p, "pluto"));
        }

        [Test]
        public void Edl_FindsFirstWrongStep()
        {
            var p = P();
            Assert.AreEqual(-1, Puzzles.FirstWrongEdlStep(new List<string> { "edl_entry", "edl_chute" }, p));
            Assert.AreEqual(1, Puzzles.FirstWrongEdlStep(new List<string> { "edl_entry", "edl_rockets" }, p));
            Assert.IsTrue(Puzzles.EdlComplete(p.edlOrder, p));
        }

        [Test]
        public void Inventory_CombinesByRecipeEitherOrder()
        {
            var p = P();
            var inv = new Inventory();
            inv.Add("item_sealant");
            inv.Add("item_plate");
            Assert.AreEqual("item_hull_patch", inv.Combine("item_sealant", "item_plate", p.recipes));
            Assert.IsTrue(inv.Has("item_hull_patch"));
            Assert.IsNull(inv.Combine("item_hull_patch", "item_plate", p.recipes));
        }

        [Test]
        public void Codex_UnlocksOnce()
        {
            var c = new Codex();
            int fired = 0;
            c.Unlocked += _ => fired++;
            Assert.IsTrue(c.Unlock("sol"));
            Assert.IsFalse(c.Unlock("sol"));
            Assert.AreEqual(1, fired);
        }
    }

    /// <summary>Checks on the shipped config and strings (Config/Data: reads the real data on purpose).</summary>
    public class ShippedDataTests
    {
        static string Root => Path.Combine(UnityEngine.Application.dataPath, "Resources", "Data");
        static GameConfig Shipped() => GameConfig.FromJson(File.ReadAllText(Path.Combine(Root, "config.json")));
        static Localization Strings() => Localization.FromJson(File.ReadAllText(Path.Combine(Root, "strings.json")));

        /// <summary>A sensible player (mirrors tools/balance_sim.py).</summary>
        static OutpostSim Play(GameConfig g, int seed, float cabin, string[] items, int pick, bool heed, bool incidentsOk)
        {
            var c = g.outpost;
            var flags = new List<string>(OutpostSim.SupplyFlags(g.chapter1, items)) { "catalyst_installed", "drill_ready", "has_hull_patch" };
            var sim = new OutpostSim(c, seed, OutpostSim.CarryOver(g.chapter1, cabin, items), flags);
            int lastDay = 0, guard = 0;
            while (sim.State.outcome == Outcome.Ongoing && guard++ < 2000)
            {
                if (sim.Day != lastDay && sim.Clock >= 6)
                {
                    lastDay = sim.Day;
                    var d = sim.DueDilemma();
                    if (d != null) sim.Choose(d.id, Pick(sim, d, pick).id);
                    if (sim.State.outcome != Outcome.Ongoing) break;
                }
                int clock = sim.Clock;
                if (clock >= 22 || clock < 6)
                {
                    sim.SetSystem("greenhouse", false);
                    bool lowAir = sim.State.air <= c.warnAir;
                    if (!lowAir || !sim.CanDo("moxie", out _)) { sim.Advance(sim.HoursToMorning, true); continue; }
                }
                var a = Policy(sim, c, heed);
                if (a != null)
                {
                    sim.Do(a);
                    var inc = sim.DueIncident();
                    if (inc != null) sim.ResolveIncident(inc, incidentsOk);
                }
                else sim.Advance(1);
            }
            return sim;
        }

        static DilemmaOption Pick(OutpostSim sim, DilemmaDef d, int pick)
        {
            var ok = d.options.FindAll(o => sim.CanChoose(o, out _));
            if (!d.random) { var o = d.options[System.Math.Min(pick, d.options.Count - 1)]; return ok.Contains(o) ? o : ok[0]; }
            DilemmaOption best = null; float bestV = float.MinValue;
            foreach (var o in ok)
            {
                float v = o.delta.air + o.delta.power + o.delta.supplies + o.delta.shelter + 0.5f * o.delta.energy - 6 * o.hours - 5 * o.dose;
                if (v > bestV) { bestV = v; best = o; }
            }
            return best;
        }

        static string Policy(OutpostSim sim, OutpostConfig c, bool heed)
        {
            var s = sim.State; int clock = sim.Clock;
            sim.SetSystem("greenhouse", s.flags.Contains("greenhouse_started") && !s.flags.Contains("greenhouse_ready")
                                        && clock >= c.daylightFrom && clock < c.daylightTo && s.power > 30);
            sim.SetSystem("beacon", s.coords == 1 || (s.flags.Contains("beacon_repaired") && s.coords == 0));
            sim.SetSystem("shelter", c.spe.hour - 2 <= s.hour && s.hour <= c.spe.hour && s.power > 10);
            sim.SetSystem("life_support", true);
            var tonight = sim.TonightEvent();
            var wants = new List<string>();
            float nightPower = (24 + 6 - 19) * c.GetSystem("life_support").powerPerHour;
            if (s.air < 40 || (clock >= 18 && s.air < 55)) wants.Add("moxie");
            if (clock >= 17 && s.power < nightPower && s.efficiency < 0.85f) wants.Add("clean_panels");
            if (s.supplies < 35) wants.AddRange(new[] { "drill_ice", "sabatier", "recycle" });
            if (s.energy < 35) wants.Add("eat");
            if (heed && tonight != null && clock >= 14)
            {
                if (tonight.preventFlag == "panels_stowed") wants.Add("stow_panels");
                if (tonight.preventFlag == "seals_inspected") wants.Add("inspect_seals");
                if (tonight.preventMinShelter > s.shelter) wants.AddRange(new[] { "shelter_kit", "shelter_water", "shelter_regolith" });
            }
            if (s.efficiency < 0.7f) wants.Add("clean_panels");
            if (heed && s.shelter < 75 && s.hour < c.spe.hour) wants.AddRange(new[] { "shelter_kit", s.supplies > 45 ? "shelter_water" : "shelter_regolith", "shelter_regolith" });
            wants.AddRange(new[] { "repair_beacon_chip", "repair_beacon", "send_coords", "setup_greenhouse_seeds", "setup_greenhouse" });
            foreach (var a in c.actions) if (a.science) wants.Add(a.id);
            if (s.air < 75) wants.Add("moxie");
            if (s.supplies < 70) wants.AddRange(new[] { "drill_ice", "sabatier", "recycle" });
            foreach (var w in wants)
            {
                if (!sim.CanDo(w, out _)) continue;
                var d = c.GetAction(w).delta;
                if (d.power < 0 && s.power + d.power < 15) continue;
                return w;
            }
            return null;
        }

        static readonly string[] Strong = { "sup_o2", "sup_food", "sup_battery", "sup_shield", "sup_cell" };

        /// <summary>A prepared player is rescued with no loot and the worst cabin air, and loot really helps.</summary>
        [Test]
        public void Outpost_PreparedPlayerIsRescued_MinimalAndStrongLoot()
        {
            var g = Shipped();
            foreach (var items in new[] { new string[0], Strong })
                for (int pick = 0; pick < 2; pick++)
                {
                    int rescued = 0; string loss = null;
                    for (int seed = 0; seed < 40; seed++)
                    {
                        var sim = Play(g, seed, g.chapter1.cabinFloor, items, pick, true, false);
                        if (sim.State.outcome == Outcome.Rescued) rescued++;
                        else loss = $"seed {seed} {sim.State.outcome} hour {sim.State.hour} air {sim.State.air:0} power {sim.State.power:0} supplies {sim.State.supplies:0}";
                    }
                    Assert.GreaterOrEqual(rescued, 38, $"loot {items.Length} pick {pick}: {rescued}/40 e.g. {loss}");
                }
        }

        /// <summary>Ignoring the forecasts and the shelter must cost something real.</summary>
        [Test]
        public void Outpost_IgnoringForecasts_HurtsHealthAndSometimesFails()
        {
            var g = Shipped();
            int healthy = 0, failed = 0;
            for (int seed = 0; seed < 40; seed++)
            {
                var sim = Play(g, seed, 50, new string[0], 0, false, false);
                if (sim.State.outcome != Outcome.Rescued) failed++;
                else if (sim.Stars()[1]) healthy++;
            }
            Assert.AreEqual(0, healthy, "no healthy star without preparing for the solar event");
            Assert.Greater(failed, 0, "ignoring forecasts should sometimes fail");
        }

        [Test]
        public void Outpost_DataIsConsistent()
        {
            var g = Shipped();
            var c = g.outpost;
            Assert.AreEqual(168, c.hoursTotal, "rescue after 7 days");
            Assert.AreEqual(60f, g.chapter1.scrambleSeconds, "the opening scramble stays 60 real seconds");
            Assert.Greater(g.chapter1.supplies.Count, g.chapter1.stowSlots);
            Assert.AreEqual(4, c.systems.Count);
            foreach (var a in c.actions)
            {
                Assert.Greater(a.hours, 0, a.id);
                if (!string.IsNullOrEmpty(a.card)) Assert.IsTrue(g.cards.Exists(x => x.id == a.card), a.id + " card " + a.card);
            }
            foreach (var d in c.dilemmas)
            {
                Assert.GreaterOrEqual(d.options.Count, 2, d.id);
                Assert.IsTrue(d.options.Exists(o => string.IsNullOrEmpty(o.needFlag)), d.id + " must have an answer that needs no item");
            }
            Assert.AreEqual(1, g.routes.Count);
            Assert.AreEqual("mars", g.routes[0].id);
        }

        [Test]
        public void Strings_CoverEverything()
        {
            var g = Shipped();
            var loc = Strings();
            var keys = new List<string>();
            foreach (var card in g.cards) { keys.Add($"card.{card.id}.title"); keys.Add($"card.{card.id}.body"); }
            var c = g.outpost;
            foreach (var s in c.systems) { keys.Add($"system.{s.id}"); keys.Add($"system.{s.id}.desc"); }
            foreach (var a in c.actions)
            {
                keys.Add($"action.{a.id}"); keys.Add($"done.{a.id}");
                if (!string.IsNullOrEmpty(a.requiresFlag)) keys.Add($"reason.need.{a.requiresFlag}");
                if (!string.IsNullOrEmpty(a.setsFlag) && !a.science) keys.Add($"reason.done.{a.setsFlag}");
                if (!string.IsNullOrEmpty(a.needsSystem)) keys.Add($"reason.system_off.{a.needsSystem}");
                if (a.science) { keys.Add($"sci.{a.id}.ask"); keys.Add($"debrief.sci.{a.id}"); }
            }
            foreach (var e in c.nightEvents) foreach (var suf in new[] { "name", "forecast", "hit", "saved", "prevent" }) keys.Add($"event.{e.id}.{suf}");
            foreach (var i in c.incidents) { keys.Add($"incident.{i.id}.alarm"); keys.Add($"incident.{i.id}.fixed"); keys.Add($"incident.{i.id}.missed"); keys.Add($"debrief.lesson.{i.id}"); }
            foreach (var e in c.nightEvents) keys.Add($"debrief.lesson.{e.id}");
            foreach (var d in c.dilemmas)
            {
                keys.Add($"dilemma.{d.id}.title"); keys.Add($"dilemma.{d.id}.body");
                foreach (var o in d.options)
                {
                    keys.Add($"dilemma.{d.id}.{o.id}"); keys.Add($"dilemma.{d.id}.{o.id}.result");
                    if (!string.IsNullOrEmpty(o.needFlag)) keys.Add($"dilemma.need.{o.needFlag}");
                }
            }
            foreach (var sup in g.chapter1.supplies) { keys.Add($"sup.{sup.id}.name"); keys.Add($"sup.{sup.id}.desc"); }
            foreach (var n in g.puzzles.navOptions) { keys.Add("nav." + n.id); keys.Add("nav.confirm." + n.id); }
            foreach (var st in g.routes[0].edl.order) { keys.Add("edl." + st); keys.Add("edl.wrong.mars." + st); }
            foreach (var k in new[] { "reason.no_power", "reason.no_supplies", "reason.no_energy", "reason.no_time", "reason.later",
                         "reason.done.science", "reason.shelter_full", "reason.not_hungry", "reason.coords_sent", "reason.waiting_earth",
                         "end.rescued.title", "end.stranded.title", "end.failed.title", "end.FailedAir", "end.FailedSupplies",
                         "msg.earth_spe_warning", "msg.earth_hint", "sources.title" })
                keys.Add(k);
            foreach (var k in keys) Assert.IsTrue(loc.Has(k), "missing key: " + k);
        }

        [Test]
        public void Strings_BothLanguages_AndMatchingArguments()
        {
            var rx = new System.Text.RegularExpressions.Regex(@"\{\d+\}");
            foreach (var e in Strings().All)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.en), "missing EN: " + e.k);
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.th), "missing TH: " + e.k);
                var en = new List<string>(); foreach (System.Text.RegularExpressions.Match m in rx.Matches(e.en)) en.Add(m.Value);
                var th = new List<string>(); foreach (System.Text.RegularExpressions.Match m in rx.Matches(e.th)) th.Add(m.Value);
                en.Sort(); th.Sort();
                CollectionAssert.AreEqual(en, th, e.k);
            }
        }
    }
}
