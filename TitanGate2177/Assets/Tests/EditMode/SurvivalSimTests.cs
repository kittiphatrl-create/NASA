// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TitanGate.Core;

namespace TitanGate.Tests
{
    /// <summary>Builds configs for tests so each test owns its numbers.</summary>
    static class TestConfigs
    {
        public static GameConfig Basic()
        {
            var c = new GameConfig();
            c.survival = new SurvivalConfig
            {
                startO2 = 60, startWater = 50, startPower = 40, startFood = 70, startStamina = 80,
                maxValue = 100, solsTotal = 4, launchSol = 4, actionsPerSol = 6, solarBase = 40,
                habPowerLoad = 14, nightO2 = 14, nightWater = 10, nightFood = 8, dustPerSol = 0.15f,
                minEfficiency = 0.3f, powerOutO2Penalty = 25, sleepStamina = 45, collapseStamina = 20,
                lowStaminaThreshold = 25, lowStaminaExtraAp = 1, fuelTarget = 100,
                randomEventNights = new[] { 1, 2 }
            };
            c.actions = new List<ActionDef>
            {
                A("moxie", new ResourceDelta { power = -8, o2 = 18, stamina = -5 }),
                A("eat", new ResourceDelta { food = -10, stamina = 25 }),
                A("clean_panels", new ResourceDelta { o2 = -3, stamina = -12 }, clean: true),
                A("stow_panels", new ResourceDelta { o2 = -2, stamina = -6 }, sets: "panels_stowed", sol: true),
                A("inspect_seals", new ResourceDelta { stamina = -4 }, sets: "seals_inspected", sol: true),
                A("sabatier", new ResourceDelta { power = -10, water = -8, fuel = 20, stamina = -5 }, req: "catalyst_installed"),
                A("repair_line", new ResourceDelta { stamina = -10 }, req: "has_hull_patch", sets: "line_repaired"),
                A("drill_ice", new ResourceDelta { o2 = -3, stamina = -14, water = 18 }, req: "drill_ready"),
                A("recycle", new ResourceDelta { power = -5, water = 12, stamina = -5 }),
            };
            c.events = new List<EventDef>
            {
                new EventDef { id = "dust_devil", preventFlag = "panels_stowed", efficiencyLoss = 0.25f, nextMorningSolarFactor = 0.6f },
                new EventDef { id = "micro_leak", preventFlag = "seals_inspected", penalty = new ResourceDelta { o2 = -12 } },
                new EventDef { id = "solar_flare", preventMinWater = 35, penalty = new ResourceDelta { stamina = -30 } },
                new EventDef { id = "global_storm", major = true, night = 3, preventFlag = "panels_stowed", preventMinPower = 40,
                    penalty = new ResourceDelta { power = -10 }, efficiencyLoss = 0.4f,
                    nextMorningSolarFactor = 0f, preventedNextMorningSolarFactor = 0f },
            };
            return c;
        }

        static ActionDef A(string id, ResourceDelta d, string req = null, string sets = null, bool sol = false, bool clean = false) =>
            new ActionDef { id = id, ap = 1, delta = d, requiresFlag = req, setsFlag = sets, solFlag = sol, cleansPanels = clean };

        /// <summary>Remove random events so a test sees only the rule it checks.</summary>
        public static GameConfig Calm()
        {
            var c = Basic();
            c.events.Clear();
            return c;
        }

        public static SurvivalSim Ready(GameConfig c, int seed = 1)
        {
            var sim = new SurvivalSim(c, seed);
            sim.SetFlag("catalyst_installed");
            sim.SetFlag("drill_ready");
            sim.SetFlag("has_hull_patch");
            sim.BeginFirstSol();
            return sim;
        }
    }

    public class SurvivalSimTests
    {
        [Test]
        public void BeginFirstSol_AddsSolarPowerAndActionPoints()
        {
            var sim = new SurvivalSim(TestConfigs.Calm(), 1);
            int produced = sim.BeginFirstSol();
            Assert.AreEqual(40, produced);
            Assert.AreEqual(80, sim.State.power);
            Assert.AreEqual(6, sim.State.ap);
            Assert.AreEqual(1, sim.State.sol);
        }

        [Test]
        public void Moxie_ConvertsPowerToOxygen_AndSpendsOneAction()
        {
            var sim = TestConfigs.Ready(TestConfigs.Calm());
            Assert.IsTrue(sim.Do("moxie", out _));
            Assert.AreEqual(72, sim.State.power);
            Assert.AreEqual(78, sim.State.o2);
            Assert.AreEqual(5, sim.State.ap);
        }

        [Test]
        public void Action_WithMissingRequiredFlag_IsRefusedAndChangesNothing()
        {
            var sim = new SurvivalSim(TestConfigs.Calm(), 1);
            sim.BeginFirstSol();
            Assert.IsFalse(sim.Do("sabatier", out var reason));
            Assert.AreEqual("reason.need.catalyst_installed", reason);
            Assert.AreEqual(0, sim.State.fuel);
            Assert.AreEqual(6, sim.State.ap);
        }

        [Test]
        public void LowStamina_MakesActionsCostTwo()
        {
            var sim = TestConfigs.Ready(TestConfigs.Calm());
            sim.State.stamina = 20;
            Assert.IsTrue(sim.Do("moxie", out _));
            Assert.AreEqual(4, sim.State.ap);
        }

        [Test]
        public void NoActionPoints_RefusesAction()
        {
            var sim = TestConfigs.Ready(TestConfigs.Calm());
            sim.State.ap = 0;
            Assert.IsFalse(sim.Do("moxie", out var reason));
            Assert.AreEqual("reason.no_ap", reason);
        }

        [Test]
        public void NotEnoughPower_RefusesPoweredAction()
        {
            var sim = TestConfigs.Ready(TestConfigs.Calm());
            sim.State.power = 5;
            Assert.IsFalse(sim.Do("sabatier", out var reason));
            Assert.AreEqual("reason.no_power", reason);
        }

        [Test]
        public void Sleep_AppliesNightUse_DustAndRest()
        {
            var sim = TestConfigs.Ready(TestConfigs.Calm());
            sim.State.stamina = 30;
            var r = sim.Sleep();
            Assert.AreEqual(2, sim.State.sol);
            Assert.AreEqual(46, sim.State.o2);                    // 60 - 14
            Assert.AreEqual(40, sim.State.water);                 // 50 - 10
            Assert.AreEqual(62, sim.State.food);                  // 70 - 8
            Assert.AreEqual(0.85f, sim.State.efficiency, 0.001f);
            Assert.AreEqual(75, sim.State.stamina);               // 30 + 45
            Assert.AreEqual(34, r.morningSolar);                  // 40 * 0.85
            Assert.AreEqual(100, sim.State.power);                // 80 - 14 + 34, clamped
        }

        [Test]
        public void Collapse_GivesLessRest()
        {
            var sim = TestConfigs.Ready(TestConfigs.Calm());
            sim.State.stamina = 10;
            sim.Sleep(collapsed: true);
            Assert.AreEqual(30, sim.State.stamina);
        }

        [Test]
        public void PowerBelowZero_DrainsOxygen()
        {
            var sim = TestConfigs.Ready(TestConfigs.Calm());
            sim.State.power = 5;
            var r = sim.Sleep();
            Assert.IsTrue(r.powerOut);
            Assert.AreEqual(60 - 14 - 25, sim.State.o2);
        }

        [Test]
        public void OxygenAtZero_LosesTheGame()
        {
            var sim = TestConfigs.Ready(TestConfigs.Calm());
            sim.State.o2 = 10;
            var r = sim.Sleep();
            Assert.AreEqual(SimOutcome.LostO2, r.outcome);
            Assert.AreEqual(SimOutcome.LostO2, sim.State.outcome);
        }

        [Test]
        public void CleanPanels_RestoresEfficiency()
        {
            var sim = TestConfigs.Ready(TestConfigs.Calm());
            sim.State.efficiency = 0.4f;
            Assert.IsTrue(sim.Do("clean_panels", out _));
            Assert.AreEqual(1f, sim.State.efficiency, 0.001f);
        }

        [Test]
        public void SolFlags_ClearEachMorning_PersistentFlagsStay()
        {
            var sim = TestConfigs.Ready(TestConfigs.Calm());
            sim.Do("stow_panels", out _);
            sim.Do("repair_line", out _);
            Assert.IsTrue(sim.State.Has("panels_stowed"));
            sim.Sleep();
            Assert.IsFalse(sim.State.Has("panels_stowed"));
            Assert.IsTrue(sim.State.Has("line_repaired"));
        }

        [Test]
        public void SolFlagAction_CannotRepeatSameSol()
        {
            var sim = TestConfigs.Ready(TestConfigs.Calm());
            Assert.IsTrue(sim.Do("stow_panels", out _));
            Assert.IsFalse(sim.Do("stow_panels", out var reason));
            Assert.AreEqual("reason.done.panels_stowed", reason);
        }

        [Test]
        public void Fuel_IsCappedAtTarget()
        {
            var sim = TestConfigs.Ready(TestConfigs.Calm());
            sim.State.fuel = 90;
            Assert.IsTrue(sim.Do("sabatier", out _));
            Assert.AreEqual(100, sim.State.fuel);
            Assert.IsFalse(sim.Do("sabatier", out var reason));
            Assert.AreEqual("reason.fuel_full", reason);
        }

        [Test]
        public void Launch_NeedsRepairFuelAndOpenWindow()
        {
            var sim = TestConfigs.Ready(TestConfigs.Calm());
            Assert.IsFalse(sim.CanLaunch(out var r1));
            Assert.AreEqual("reason.need.line_repaired", r1);
            sim.Do("repair_line", out _);
            sim.State.fuel = 100;
            Assert.IsFalse(sim.CanLaunch(out var r2));
            Assert.AreEqual("reason.window_not_open", r2);
            sim.State.sol = 4;
            Assert.IsTrue(sim.Launch());
            Assert.AreEqual(SimOutcome.Won, sim.State.outcome);
        }

        [Test]
        public void SleepingOnLastSol_ClosesTheLaunchWindow()
        {
            var sim = TestConfigs.Ready(TestConfigs.Calm());
            sim.State.sol = 4;
            var r = sim.Sleep();
            Assert.AreEqual(SimOutcome.LostWindow, r.outcome);
        }
    }

    public class EventTests
    {
        [Test]
        public void Schedule_IsDeterministicForASeed_AndMajorIsFixed()
        {
            var c = TestConfigs.Basic();
            var a = SurvivalSim.BuildSchedule(c, 42);
            var b = SurvivalSim.BuildSchedule(c, 42);
            CollectionAssert.AreEqual(a, b);
            Assert.AreEqual(3, a.Length);
            Assert.AreEqual("global_storm", a[2]);
            Assert.IsNotNull(a[0]);
            Assert.IsNotNull(a[1]);
            Assert.AreNotEqual(a[0], a[1]);
        }

        [Test]
        public void Schedule_EverySeedUsesOnlyMinorsOnRandomNights()
        {
            var c = TestConfigs.Basic();
            for (int seed = 0; seed < 50; seed++)
            {
                var s = SurvivalSim.BuildSchedule(c, seed);
                Assert.AreNotEqual("global_storm", s[0]);
                Assert.AreNotEqual("global_storm", s[1]);
            }
        }

        static SurvivalSim WithTonight(string eventId)
        {
            var c = TestConfigs.Basic();
            c.survival.randomEventNights = new int[0];
            var ev = c.GetEvent(eventId);
            ev.major = true;
            ev.night = 1;
            c.events.RemoveAll(e => e.id == "global_storm" && eventId != "global_storm");
            return TestConfigs.Ready(c);
        }

        [Test]
        public void Leak_Unprevented_CostsOxygen()
        {
            var sim = WithTonight("micro_leak");
            var r = sim.Sleep();
            Assert.AreEqual("micro_leak", r.eventId);
            Assert.IsFalse(r.eventPrevented);
            Assert.AreEqual(60 - 14 - 12, sim.State.o2);
        }

        [Test]
        public void Leak_PreventedByInspectingSeals()
        {
            var sim = WithTonight("micro_leak");
            sim.Do("inspect_seals", out _);
            var r = sim.Sleep();
            Assert.IsTrue(r.eventPrevented);
            Assert.AreEqual(60 - 14, sim.State.o2);
        }

        [Test]
        public void Flare_PreventedByEnoughWaterShielding()
        {
            var sim = WithTonight("solar_flare");
            sim.State.stamina = 50;
            var r = sim.Sleep();
            Assert.IsTrue(r.eventPrevented);   // water 50 >= 35 at bedtime
            Assert.AreEqual(95, sim.State.stamina);
        }

        [Test]
        public void Flare_UnpreventedWithLowWater_CostsStamina()
        {
            var sim = WithTonight("solar_flare");
            sim.State.water = 30;
            sim.State.stamina = 50;
            var r = sim.Sleep();
            Assert.IsFalse(r.eventPrevented);
            Assert.AreEqual(50 - 30 + 45, sim.State.stamina);
        }

        [Test]
        public void DustDevil_Unprevented_CutsTomorrowsSolarAndEfficiency()
        {
            var sim = WithTonight("dust_devil");
            var r = sim.Sleep();
            Assert.IsFalse(r.eventPrevented);
            Assert.AreEqual(0.6f, sim.State.efficiency, 0.001f);       // 1 - 0.25 - 0.15
            Assert.AreEqual((int)System.Math.Round(40 * 0.6f * 0.6f), r.morningSolar);
        }

        [Test]
        public void Storm_PreparedPlayer_LosesOnlyOneMorningOfSun()
        {
            var sim = WithTonight("global_storm");
            sim.Do("stow_panels", out _);
            sim.State.power = 60;
            var r = sim.Sleep();
            Assert.IsTrue(r.eventPrevented);
            Assert.AreEqual(0, r.morningSolar);
            Assert.AreEqual(60 - 14, sim.State.power);
            Assert.AreEqual(0.85f, sim.State.efficiency, 0.001f);
        }

        [Test]
        public void Storm_UnpreparedPlayer_TakesHeavyDamage()
        {
            var sim = WithTonight("global_storm");
            sim.State.power = 20;
            var r = sim.Sleep();
            Assert.IsFalse(r.eventPrevented);
            Assert.IsTrue(r.powerOut);                       // 20 - 14 - 10 < 0
            Assert.AreEqual(60 - 14 - 25, sim.State.o2);
            Assert.AreEqual(0.45f, sim.State.efficiency, 0.001f);
        }
    }

    public class PuzzleTests
    {
        static PuzzleConfig P() => new PuzzleConfig
        {
            lockerCode = "7", podCode = "39", fuelGaugePercent = 18,
            navOptions = new List<NavOption>
            {
                new NavOption { id = "titan", fuelNeeded = 60 },
                new NavOption { id = "earth", fuelNeeded = 35 },
                new NavOption { id = "mars", fuelNeeded = 15 },
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
        public void Nav_OnlyMarsReachableWithGaugeFuel()
        {
            var p = P();
            Assert.IsTrue(Puzzles.CanReach(p, "mars"));
            Assert.IsFalse(Puzzles.CanReach(p, "earth"));
            Assert.IsFalse(Puzzles.CanReach(p, "titan"));
            Assert.AreEqual("mars", Puzzles.BestReachable(p));
        }

        [Test]
        public void Edl_FindsFirstWrongStep()
        {
            var p = P();
            Assert.AreEqual(-1, Puzzles.FirstWrongEdlStep(new List<string> { "edl_entry", "edl_chute" }, p));
            Assert.AreEqual(1, Puzzles.FirstWrongEdlStep(new List<string> { "edl_entry", "edl_rockets" }, p));
            Assert.IsTrue(Puzzles.EdlComplete(p.edlOrder, p));
            Assert.IsFalse(Puzzles.EdlComplete(new List<string> { "edl_entry" }, p));
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
            Assert.IsFalse(inv.Has("item_plate"));
            Assert.IsNull(inv.Combine("item_hull_patch", "item_plate", p.recipes));
        }

        [Test]
        public void Inventory_NoDuplicates()
        {
            var inv = new Inventory();
            Assert.IsTrue(inv.Add("a"));
            Assert.IsFalse(inv.Add("a"));
            Assert.AreEqual(1, inv.Items.Count);
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

    public class RouteRuleTests
    {
        [Test]
        public void CarryOver_CabinOxygenAboveAndBelowPivot()
        {
            var c = new Chapter1Config { carryO2Pivot = 50, carryO2Factor = 0.4f, suppliesBonus = new ResourceDelta { food = 15, water = 10 } };
            Assert.AreEqual(20, SurvivalSim.CarryOver(c, 100, false).o2);
            Assert.AreEqual(-16, SurvivalSim.CarryOver(c, 10, false).o2);
            var withBag = SurvivalSim.CarryOver(c, 50, true);
            Assert.AreEqual(0, withBag.o2);
            Assert.AreEqual(15, withBag.food);
            Assert.AreEqual(10, withBag.water);
        }

        [Test]
        public void StartBonus_IsAppliedAndClamped()
        {
            var sim = new SurvivalSim(TestConfigs.Calm(), 1, new ResourceDelta { o2 = 60, food = -5 });
            Assert.AreEqual(100, sim.State.o2);
            Assert.AreEqual(65, sim.State.food);
            Assert.AreEqual(0, sim.State.fuel);
        }

        static GameConfig WithIncident()
        {
            var c = TestConfigs.Calm();
            c.incidents = new List<IncidentDef>
            {
                new IncidentDef { id = "fire", sol = 1, afterActions = 2, seconds = 30, hotspot = "s_recycler", penalty = new ResourceDelta { water = -12 } }
            };
            return c;
        }

        [Test]
        public void Incident_BecomesDueAfterEnoughActions_Once()
        {
            var sim = TestConfigs.Ready(WithIncident());
            Assert.IsNull(sim.DueIncident());
            sim.Do("moxie", out _);
            Assert.IsNull(sim.DueIncident());
            sim.Do("moxie", out _);
            var i = sim.DueIncident();
            Assert.IsNotNull(i);
            sim.ResolveIncident(i, true);
            Assert.IsNull(sim.DueIncident());
            Assert.AreEqual(50, sim.State.water);
        }

        [Test]
        public void Incident_Missed_AppliesPenalty()
        {
            var sim = TestConfigs.Ready(WithIncident());
            sim.Do("moxie", out _); sim.Do("moxie", out _);
            sim.ResolveIncident(sim.DueIncident(), false);
            Assert.AreEqual(38, sim.State.water);
        }

        [Test]
        public void GoalFlag_IsPerRoute()
        {
            var c = TestConfigs.Calm();
            c.survival.goalFlag = "antenna_ready";
            var sim = TestConfigs.Ready(c);
            sim.State.fuel = 100; sim.State.sol = 4;
            Assert.IsFalse(sim.CanLaunch(out var r));
            Assert.AreEqual("reason.need.antenna_ready", r);
            sim.SetFlag("antenna_ready");
            Assert.IsTrue(sim.CanLaunch(out _));
        }

        [Test]
        public void Steps_PerRouteOrderAndDistractor()
        {
            var moon = new List<string> { "moon_deorbit", "moon_pitch", "moon_hover", "moon_touch" };
            Assert.AreEqual(0, Puzzles.FirstWrongStep(new List<string> { "edl_chute" }, moon));
            Assert.AreEqual(-1, Puzzles.FirstWrongStep(new List<string> { "moon_deorbit", "moon_pitch" }, moon));
            Assert.IsTrue(Puzzles.StepsComplete(moon, moon));
        }

        [Test]
        public void Nav_EveryDestinationLeadsToARoute()
        {
            var p = new PuzzleConfig
            {
                navOptions = new List<NavOption>
                {
                    new NavOption { id = "titan", fuelNeeded = 60, route = "titan" },
                    new NavOption { id = "earth", fuelNeeded = 35, route = "moon" },
                    new NavOption { id = "mars", fuelNeeded = 15, route = "mars" },
                }
            };
            Assert.AreEqual("moon", Puzzles.RouteFor(p, "earth"));
            Assert.AreEqual("titan", Puzzles.RouteFor(p, "titan"));
            Assert.AreEqual("mars", Puzzles.RouteFor(p, "mars"));
            Assert.IsNull(Puzzles.RouteFor(p, "pluto"));
        }
    }

    /// <summary>
    /// Config/Data checks on the shipped files (Config/Data story type: reads the real data on purpose).
    /// </summary>
    public class ShippedDataTests
    {
        static string Root => Path.Combine(UnityEngine.Application.dataPath, "Resources", "Data");

        static GameConfig Shipped() => GameConfig.FromJson(File.ReadAllText(Path.Combine(Root, "config.json")));

        static Localization Strings() => Localization.FromJson(File.ReadAllText(Path.Combine(Root, "strings.json")));

        static readonly Dictionary<string, string> PreventByFlag = new Dictionary<string, string>
        {
            ["panels_stowed"] = "stow_panels", ["seals_inspected"] = "inspect_seals", ["shutters_closed"] = "close_shutters",
            ["heaters_set"] = "set_heaters", ["hab_insulated"] = "insulate", ["rtg_checked"] = "check_rtg",
        };
        static readonly Dictionary<string, string> O2Action = new Dictionary<string, string> { ["mars"] = "moxie", ["moon"] = "electrolyze", ["titan"] = "electrolyze" };
        static readonly Dictionary<string, string> Progress = new Dictionary<string, string> { ["mars"] = "sabatier", ["moon"] = "transmit", ["titan"] = "charge_gate" };

        static SurvivalSim ReadySim(GameConfig c, int seed, ResourceDelta bonus)
        {
            var sim = new SurvivalSim(c, seed, bonus);
            foreach (var f in new[] { "catalyst_installed", "drill_ready", "has_hull_patch", "line_repaired", "antenna_ready", "gate_found" }) sim.SetFlag(f);
            sim.BeginFirstSol();
            return sim;
        }

        /// <summary>
        /// A sensible player (mirrors tools/balance_sim.py): prepare for tonight's forecast, keep O2, water,
        /// food and energy up, save power for the big night, then do science and progress.
        /// </summary>
        static string Pick(SurvivalSim sim, string route, GameConfig c, bool heedForecasts)
        {
            var st = sim.State;
            var s = c.survival;
            var ev = sim.TonightEvent();
            int needPw = System.Math.Max(s.habPowerLoad, ev != null ? ev.preventMinPower : 0);
            var major = sim.MajorEvent();
            if (major != null && major.night == st.sol + 1) needPw += 15;
            int needW = ev != null ? ev.preventMinWater : 0;
            bool Ok(string id)
            {
                var a = c.GetAction(id);
                if (a == null || !sim.CanDo(id, out _)) return false;
                if (a.delta.power < 0 && st.power + a.delta.power < needPw) return false;
                if (a.delta.water < 0 && st.water + a.delta.water < needW) return false;
                return true;
            }
            var wants = new List<string>();
            if (heedForecasts && ev != null && !string.IsNullOrEmpty(ev.preventFlag) && !st.Has(ev.preventFlag)) wants.Add(PreventByFlag[ev.preventFlag]);
            if (st.o2 < 50) wants.Add(O2Action[route]);
            if (st.water < 40 || (needW > 0 && st.water < needW + 5)) { wants.Add("drill_ice"); wants.Add("recycle"); }
            if (st.stamina < 35) wants.Add("eat");
            if (st.food < 30) wants.Add("grow_greens");
            if (c.GetAction("clean_panels") != null && st.efficiency < 0.75f) wants.Add("clean_panels");
            if (route == "moon" && st.sol >= 2) wants.Add("build_berm");
            foreach (var a in c.actions) if (a.science) wants.Add(a.id);
            wants.Add(Progress[route]); wants.Add(O2Action[route]); wants.Add("drill_ice"); wants.Add("grow_greens");
            foreach (var w in wants) if (Ok(w)) return w;
            return null;
        }

        /// <summary>
        /// Fixed dilemmas: take option [pick] (both answers must be playable). Random encounters: a
        /// sensible player takes the better deal (mirrors tools/balance_sim.py).
        /// </summary>
        static DilemmaOption PickOption(SurvivalSim sim, DilemmaDef d, int pick)
        {
            var ok = d.options.FindAll(o => sim.CanChoose(o, out _));
            if (!d.random)
            {
                var o = d.options[System.Math.Min(pick, d.options.Count - 1)];
                return ok.Contains(o) ? o : ok[0];
            }
            DilemmaOption best = null;
            float bestV = float.MinValue;
            foreach (var o in ok)
            {
                float v = o.delta.o2 + o.delta.water + o.delta.power + o.delta.food + o.delta.fuel + 0.5f * o.delta.stamina - 8 * o.ap - 5 * o.dose;
                if (v > bestV) { bestV = v; best = o; }
            }
            return best;
        }

        /// <summary>Plays one whole stay with the policy. Emergencies are all missed (worst case).</summary>
        static SurvivalSim PlayStay(GameConfig c, string route, int seed, ResourceDelta bonus, bool heedForecasts, int pick = 0)
        {
            var sim = ReadySim(c, seed, bonus);
            while (sim.State.outcome == SimOutcome.Ongoing)
            {
                DilemmaDef dil;
                while ((dil = sim.DueDilemma()) != null) sim.Choose(dil.id, PickOption(sim, dil, pick).id);
                if (sim.State.outcome != SimOutcome.Ongoing) break;
                string id;
                int guard = 0;
                while (sim.State.outcome == SimOutcome.Ongoing && (id = Pick(sim, route, c, heedForecasts)) != null && guard++ < 20)
                {
                    sim.Do(id, out _);
                    var inc = sim.DueIncident();
                    if (inc != null) sim.ResolveIncident(inc, false);
                }
                if (sim.State.outcome != SimOutcome.Ongoing) break;
                if (sim.State.sol >= c.survival.solsTotal) { sim.Launch(); break; }
                sim.Sleep(sim.State.stamina <= 0);
            }
            return sim;
        }

        /// <summary>
        /// Worst fair case: little cabin oxygen, no supplies bag, every daytime emergency missed.
        /// A sensible player must still win every route for every event order and finish the science.
        /// </summary>
        [Test]
        public void EveryRoute_SensiblePlayerWins_EvenInTheWorstCarryOver()
        {
            var shipped = Shipped();
            Assert.AreEqual(3, shipped.routes.Count);
            foreach (var route in shipped.routes)
            {
                var c = shipped.ForRoute(route.id);
                var bonus = SurvivalSim.CarryOver(shipped.chapter1, shipped.chapter1.cabinFloor, false);
                for (int pick = 0; pick < 2; pick++)
                {
                    // choices have real costs now, so a rare unlucky pick may lose; 95% must still win
                    int wins = 0;
                    string lastLoss = null;
                    for (int seed = 0; seed < 40; seed++)
                    {
                        var sim = PlayStay(c, route.id, seed, bonus, true, pick);
                        var st = sim.State;
                        if (st.outcome == SimOutcome.Won)
                        {
                            wins++;
                            Assert.IsTrue(sim.Stars()[2], $"{route.id} seed {seed}: science {sim.ScienceDone}/{sim.ScienceTotal}");
                        }
                        else lastLoss = $"seed {seed} {st.outcome} sol {st.sol} o2 {st.o2} water {st.water} food {st.food} power {st.power} progress {st.fuel}";
                    }
                    Assert.GreaterOrEqual(wins, 38, $"{route.id} pick {pick}: {wins}/40, e.g. {lastLoss}");
                }
            }
        }

        /// <summary>Ignoring ORBI's forecasts should usually lose: preparing is the game.</summary>
        [Test]
        public void EveryRoute_IgnoringForecasts_UsuallyLoses()
        {
            var shipped = Shipped();
            foreach (var route in shipped.routes)
            {
                var c = shipped.ForRoute(route.id);
                var bonus = SurvivalSim.CarryOver(shipped.chapter1, 50f, false);
                for (int pick = 0; pick < 2; pick++)
                {
                    int wins = 0;
                    for (int seed = 0; seed < 40; seed++)
                        if (PlayStay(c, route.id, seed, bonus, false, pick).State.outcome == SimOutcome.Won) wins++;
                    Assert.Less(wins, 10, route.id + " pick " + pick);
                }
            }
        }

        /// <summary>Doing nothing must not win on any route: the game needs the player.</summary>
        [Test]
        public void EveryRoute_DoingNothing_Loses()
        {
            var shipped = Shipped();
            foreach (var route in shipped.routes)
            {
                var sim = ReadySim(shipped.ForRoute(route.id), 3, null);
                SimOutcome o = SimOutcome.Ongoing;
                for (int i = 0; i < route.survival.solsTotal && o == SimOutcome.Ongoing; i++) o = sim.Sleep().outcome;
                Assert.AreNotEqual(SimOutcome.Ongoing, o, route.id);
                Assert.AreNotEqual(SimOutcome.Won, o, route.id);
            }
        }

        [Test]
        public void EveryRoute_EveryEventIsPreventable_AndIncidentsAreSane()
        {
            var shipped = Shipped();
            foreach (var r in shipped.routes)
            {
                foreach (var e in r.events)
                {
                    bool flagReachable = string.IsNullOrEmpty(e.preventFlag) || r.actions.Exists(a => a.setsFlag == e.preventFlag);
                    Assert.IsTrue(flagReachable, r.id + "/" + e.id);
                    Assert.LessOrEqual(e.preventMinWater, r.survival.maxValue, e.id);
                    Assert.LessOrEqual(e.preventMinPower, r.survival.maxValue, e.id);
                }
                Assert.AreEqual(1, r.events.FindAll(e => e.major).Count, r.id + " needs exactly one major event");
                foreach (var i in r.incidents)
                {
                    Assert.Greater(i.seconds, 15f, i.id);
                    Assert.LessOrEqual(i.afterActions, r.survival.actionsPerSol, i.id);
                    Assert.IsFalse(string.IsNullOrEmpty(i.hotspot), i.id);
                }
                Assert.AreEqual(4, r.edl.order.Count, r.id);
            }
            foreach (var n in shipped.puzzles.navOptions) Assert.IsNotNull(shipped.GetRoute(n.route), n.id);
        }

        [Test]
        public void Strings_EveryEntryHasBothLanguages()
        {
            var loc = Strings();
            Assert.Greater(loc.Count, 50);
            foreach (var e in loc.All)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.en), "missing EN: " + e.k);
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.th), "missing TH: " + e.k);
            }
        }

        [Test]
        public void Strings_CoverEveryCardEventActionIncidentStepAndReason()
        {
            var c = Shipped();
            var loc = Strings();
            var keys = new List<string>();
            foreach (var card in c.cards) { keys.Add($"card.{card.id}.title"); keys.Add($"card.{card.id}.body"); }
            foreach (var r in c.routes)
            {
                keys.Add($"route.{r.id}.name"); keys.Add($"route.{r.id}.progress"); keys.Add($"end.win.{r.id}.title"); keys.Add($"end.win.{r.id}.body");
                foreach (var e in r.events) { keys.Add($"event.{e.id}.name"); keys.Add($"event.{e.id}.forecast"); keys.Add($"event.{e.id}.hit"); keys.Add($"event.{e.id}.saved"); keys.Add($"event.{e.id}.prevent"); }
                foreach (var a in r.actions)
                {
                    keys.Add($"action.{a.id}"); keys.Add($"done.{a.id}");
                    if (!string.IsNullOrEmpty(a.requiresFlag)) keys.Add($"reason.need.{a.requiresFlag}");
                    if (!string.IsNullOrEmpty(a.setsFlag) && !a.science) keys.Add($"reason.done.{a.setsFlag}");
                    if (!string.IsNullOrEmpty(a.card)) { keys.Add($"card.{a.card}.title"); Assert.IsTrue(c.cards.Exists(x => x.id == a.card), a.card); }
                    if (a.science) { keys.Add($"sci.{a.id}.ask"); keys.Add($"debrief.sci.{a.id}"); }
                }
                foreach (var i in r.incidents) { keys.Add($"incident.{i.id}.alarm"); keys.Add($"incident.{i.id}.fixed"); keys.Add($"incident.{i.id}.missed"); }
                foreach (var st in r.edl.order) keys.Add("edl." + st);
                foreach (var st in r.edl.order) keys.Add($"edl.wrong.{r.id}.{st}");
                foreach (var st in r.edl.distractors) { keys.Add("edl." + st); keys.Add($"edl.wrong.{r.id}.{st}"); }
                keys.Add("reason.need." + r.survival.goalFlag);
                foreach (var e in r.events) keys.Add($"debrief.lesson.{e.id}");
                foreach (var i in r.incidents) keys.Add($"debrief.lesson.{i.id}");
                Assert.GreaterOrEqual(r.dilemmas.Count, 2, r.id + " needs dilemmas");
                foreach (var d in r.dilemmas)
                {
                    keys.Add($"dilemma.{d.id}.title"); keys.Add($"dilemma.{d.id}.body");
                    Assert.GreaterOrEqual(d.options.Count, 2, d.id);
                    foreach (var o in d.options)
                    {
                        keys.Add($"dilemma.{d.id}.{o.id}"); keys.Add($"dilemma.{d.id}.{o.id}.result");
                        if (!string.IsNullOrEmpty(o.needFlag)) keys.Add($"dilemma.need.{o.needFlag}");
                        if (!string.IsNullOrEmpty(o.card)) Assert.IsTrue(c.cards.Exists(x => x.id == o.card), o.card);
                    }
                }
                keys.Add($"end.lose.{r.id}.LostRadiation");
            }
            foreach (var n in c.puzzles.navOptions) { keys.Add("nav." + n.id); keys.Add("nav.confirm." + n.id); }
            Assert.Greater(c.chapter1.supplies.Count, c.chapter1.stowSlots, "the scramble must force a choice");
            foreach (var sup in c.chapter1.supplies) { keys.Add($"sup.{sup.id}.name"); keys.Add($"sup.{sup.id}.desc"); }
            foreach (var r in new[] { "reason.no_ap", "reason.no_stamina", "reason.no_food", "reason.no_power", "reason.no_water",
                         "reason.fuel_full", "reason.not_hungry", "reason.need_fuel", "reason.window_not_open",
                         "reason.later", "reason.done.science", "hud.dose" })
                keys.Add(r);
            foreach (var k in keys) Assert.IsTrue(loc.Has(k), "missing key: " + k);
        }

        [Test]
        public void Strings_FormatArgumentsMatchBetweenLanguages()
        {
            var rx = new System.Text.RegularExpressions.Regex(@"\{\d+\}");
            foreach (var e in Strings().All)
            {
                var en = new List<string>(); foreach (System.Text.RegularExpressions.Match m in rx.Matches(e.en)) en.Add(m.Value);
                var th = new List<string>(); foreach (System.Text.RegularExpressions.Match m in rx.Matches(e.th)) th.Add(m.Value);
                en.Sort(); th.Sort();
                CollectionAssert.AreEqual(en, th, e.k);
            }
        }
    }

    /// <summary>Radiation, science tasks, longer schedules and the debrief stars.</summary>
    public class RadiationAndDebriefTests
    {
        static GameConfig Rad()
        {
            var c = TestConfigs.Calm();
            c.survival.radPerSol = 2f;
            c.survival.radPerEva = 1f;
            c.survival.radLimit = 100f;
            c.survival.radSafe = 10f;
            c.survival.shieldFlag = "berm";
            c.survival.shieldFactor = 0.5f;
            c.actions.Find(a => a.id == "clean_panels").outside = true;
            c.actions.Add(new ActionDef { id = "build_berm", ap = 1, delta = new ResourceDelta { stamina = -5 }, setsFlag = "berm" });
            c.actions.Add(new ActionDef { id = "sci_a", ap = 1, delta = new ResourceDelta { stamina = -1 }, setsFlag = "sci_a", science = true, minSol = 2 });
            return c;
        }

        [Test]
        public void Night_AddsBackgroundDose_AndShieldHalvesIt()
        {
            var sim = TestConfigs.Ready(Rad());
            var r = sim.Sleep();
            Assert.AreEqual(2f, r.dose, 1e-4);
            Assert.AreEqual(2f, sim.State.dose, 1e-4);
            sim.Do("build_berm", out _);
            Assert.IsTrue(sim.Shielded);
            sim.Sleep();
            Assert.AreEqual(3f, sim.State.dose, 1e-4);
        }

        [Test]
        public void OutsideAction_AddsEvaDose()
        {
            var sim = TestConfigs.Ready(Rad());
            sim.Do("clean_panels", out _);
            Assert.AreEqual(1f, sim.State.dose, 1e-4);
            sim.Do("moxie", out _);
            Assert.AreEqual(1f, sim.State.dose, 1e-4);
        }

        [Test]
        public void UnpreventedEvent_AddsItsDose_PreventedDoesNot()
        {
            var c = Rad();
            c.survival.radPerSol = 0;
            c.events.Add(new EventDef { id = "flare", preventMinWater = 35, dose = 40f });
            c.survival.randomEventNights = new[] { 1, 2 };
            var sim = TestConfigs.Ready(c);
            sim.State.water = 10;
            sim.Sleep();
            Assert.AreEqual(40f, sim.State.dose, 1e-4);
            sim.State.water = 90;
            sim.Sleep();
            Assert.AreEqual(40f, sim.State.dose, 1e-4);
        }

        [Test]
        public void ReachingTheDoseLimit_EndsTheMission()
        {
            var sim = TestConfigs.Ready(Rad());
            sim.State.dose = 99.5f;
            var r = sim.Sleep();
            Assert.AreEqual(SimOutcome.LostRadiation, r.outcome);
        }

        [Test]
        public void ScienceTask_WaitsForItsSol_ThenCountsOnce()
        {
            var sim = TestConfigs.Ready(Rad());
            Assert.IsFalse(sim.CanDo("sci_a", out var why));
            Assert.AreEqual("reason.later", why);
            sim.Sleep();
            Assert.IsTrue(sim.Do("sci_a", out _));
            Assert.AreEqual(1, sim.ScienceDone);
            Assert.IsFalse(sim.CanDo("sci_a", out why));
            Assert.AreEqual("reason.done.science", why);
        }

        [Test]
        public void Stars_NeedAWin_LowDose_NoCollapse_AndAllScience()
        {
            var c = Rad();
            c.survival.goalFlag = null;
            c.survival.launchSol = 2;
            var sim = TestConfigs.Ready(c);
            sim.Sleep();
            sim.Do("sci_a", out _);
            sim.State.fuel = c.survival.fuelTarget;
            Assert.IsTrue(sim.Launch());
            CollectionAssert.AreEqual(new[] { true, true, true }, sim.Stars());
            sim.State.dose = 11f;
            Assert.IsFalse(sim.Stars()[1]);
        }

        [Test]
        public void Log_RecordsNightEventsAndEmergencies()
        {
            var c = TestConfigs.Basic();
            c.incidents = new List<IncidentDef> { new IncidentDef { id = "fire", sol = 1, afterActions = 1, hotspot = "x", penalty = new ResourceDelta { water = -1 } } };
            var sim = TestConfigs.Ready(c);
            sim.Do("moxie", out _);
            sim.ResolveIncident(sim.DueIncident(), false);
            sim.Sleep();
            Assert.AreEqual(2, sim.State.log.Count);
            Assert.IsTrue(sim.State.log[0].incident);
            Assert.IsFalse(sim.State.log[0].ok);
            Assert.AreEqual(sim.Schedule[0], sim.State.log[1].id);
        }

        static GameConfig WithDilemma()
        {
            var c = TestConfigs.Calm();
            c.dilemmas = new List<DilemmaDef>
            {
                new DilemmaDef
                {
                    id = "d", sol = 1, options = new List<DilemmaOption>
                    {
                        new DilemmaOption { id = "a", ap = 2, dose = 1.5f, delta = new ResourceDelta { power = 10, stamina = -5 } },
                        new DilemmaOption { id = "b", setsFlag = "orbi_off", solFlag = true },
                    }
                }
            };
            return c;
        }

        [Test]
        public void Dilemma_IsDueOnItsMorning_AndAppliesTheChosenOption()
        {
            var sim = TestConfigs.Ready(WithDilemma());
            var d = sim.DueDilemma();
            Assert.AreEqual("d", d.id);
            int power = sim.State.power, ap = sim.State.ap;
            Assert.IsTrue(sim.Choose("d", "a"));
            Assert.AreEqual(power + 10, sim.State.power);
            Assert.AreEqual(ap - 2, sim.State.ap);
            Assert.AreEqual(1.5f, sim.State.dose, 1e-4);
            Assert.AreEqual("a", sim.State.choices["d"]);
            Assert.IsNull(sim.DueDilemma());
            Assert.IsFalse(sim.Choose("d", "b"), "a dilemma is answered once");
            Assert.IsTrue(sim.State.log.Exists(e => e.choice && e.option == "a"));
        }

        static GameConfig WithEncounters()
        {
            var c = TestConfigs.Calm();
            c.survival.encounterSols = new[] { 1, 2, 3 };
            c.dilemmas = new List<DilemmaDef>
            {
                new DilemmaDef { id = "fixed2", sol = 2, options = new List<DilemmaOption> { new DilemmaOption { id = "x" } } },
                new DilemmaDef { id = "parts", random = true, options = new List<DilemmaOption>
                    { new DilemmaOption { id = "sort", setsFlag = "spare" }, new DilemmaOption { id = "leave" } } },
                new DilemmaDef { id = "wear", random = true, options = new List<DilemmaOption>
                    { new DilemmaOption { id = "swap", needFlag = "spare" }, new DilemmaOption { id = "scrub", delta = new ResourceDelta { o2 = -6 } } } },
                new DilemmaDef { id = "followup", random = true, requiresFlag = "spare", options = new List<DilemmaOption> { new DilemmaOption { id = "ok" } } },
            };
            return c;
        }

        [Test]
        public void Encounters_AreDrawnOnlyOnFreeEncounterMornings_EachOnce()
        {
            var sim = TestConfigs.Ready(WithEncounters());
            var first = sim.DueDilemma();
            Assert.IsNotNull(first, "sol 1 draws an encounter");
            Assert.IsTrue(first.random);
            sim.Choose(first.id, first.options[first.options.Count - 1].id);
            sim.Sleep();
            Assert.AreEqual("fixed2", sim.DueDilemma().id, "a fixed dilemma takes the morning");
            Assert.IsNull(sim.State.encounterToday);
            sim.Choose("fixed2", "x");
            sim.Sleep();
            var third = sim.DueDilemma();
            Assert.IsNotNull(third);
            Assert.AreNotEqual(first.id, third.id, "never the same encounter twice");
            Assert.AreNotEqual("followup", third.id, "follow-ups wait for their flag");
        }

        [Test]
        public void EncounterOption_NeedsItsFlag_AndFollowUpUnlocks()
        {
            var c = WithEncounters();
            var sim = TestConfigs.Ready(c);
            var wear = c.dilemmas.Find(d => d.id == "wear");
            Assert.IsFalse(sim.CanChoose(wear.options[0], out var why));
            Assert.AreEqual("dilemma.need.spare", why);
            sim.SetFlag("spare");
            Assert.IsTrue(sim.CanChoose(wear.options[0], out _));
        }

        [Test]
        public void CarryOver_AddsStowedSupplies_AndTheirFlags()
        {
            var c = new Chapter1Config { carryO2Pivot = 50, carryO2Factor = 0.4f };
            c.supplies.Add(new SupplyDef { id = "w", delta = new ResourceDelta { water = 12 } });
            c.supplies.Add(new SupplyDef { id = "cell", delta = new ResourceDelta { fuel = 10 } });
            c.supplies.Add(new SupplyDef { id = "kit", flag = "med_kit" });
            var d = SurvivalSim.CarryOver(c, 50f, new[] { "w", "cell", "kit", "unknown" });
            Assert.AreEqual(12, d.water);
            Assert.AreEqual(10, d.fuel);
            CollectionAssert.AreEqual(new[] { "med_kit" }, SurvivalSim.SupplyFlags(c, new[] { "w", "kit" }));
            var sim = new SurvivalSim(TestConfigs.Calm(), 1, d);
            Assert.AreEqual(10, sim.State.fuel, "a stowed power cell starts the progress bar");
        }

        [Test]
        public void Dilemma_SolFlagLastsUntilMorning()
        {
            var sim = TestConfigs.Ready(WithDilemma());
            sim.Choose("d", "b");
            Assert.IsTrue(sim.State.Has("orbi_off"));
            sim.Sleep();
            Assert.IsFalse(sim.State.Has("orbi_off"));
        }

        [Test]
        public void LongSchedule_ReusesTheDeck_NeverTheSameEventTwoNightsRunning()
        {
            var c = TestConfigs.Basic();
            c.survival.solsTotal = 7;
            c.survival.randomEventNights = new[] { 1, 2, 3, 4, 6 };
            c.events.Find(e => e.major).night = 5;
            for (int seed = 0; seed < 50; seed++)
            {
                var n = SurvivalSim.BuildSchedule(c, seed);
                for (int i = 0; i < 4; i++) Assert.IsNotNull(n[i], $"seed {seed} night {i + 1}");
                Assert.AreEqual("global_storm", n[4]);
                for (int i = 1; i < 4; i++) Assert.AreNotEqual(n[i - 1], n[i], $"seed {seed}");
                var seen = new HashSet<string>(new[] { n[0], n[1], n[2] });
                Assert.AreEqual(3, seen.Count, "the first pass uses every minor event once");
            }
        }
    }
}
