// LAST DOCK — rules tests (spec: scavenge counts, incident options, free reading, one action = one step,
// CO₂ and O₂ independent, detached rooms never targeted, restart clears, at least two winning plans).
using System.Collections.Generic;
using NUnit.Framework;
using TitanGate.Core.LastDock;

namespace TitanGate.Tests
{
    public class LastDockTests
    {
        static Dictionary<Res, int> Kit(int food = 2, int o2 = 2, int bat = 2, int parts = 2) =>
            new Dictionary<Res, int> { [Res.Food] = food, [Res.Oxygen] = o2, [Res.Battery] = bat, [Res.Parts] = parts };

        static StationSimulation New(int seed = 1, Dictionary<Res, int> kit = null) => new StationSimulation(new StationConfig(), kit ?? Kit(), seed);

        // ---------------------------------------------------------------- scavenge

        [Test]
        public void Scavenge_CarryThree_PodEight_OnlyDepositedCount()
        {
            var inv = new ScavengeInventory();
            Assert.IsTrue(inv.Pick(Res.Food)); Assert.IsTrue(inv.Pick(Res.Food)); Assert.IsTrue(inv.Pick(Res.Battery));
            Assert.IsFalse(inv.Pick(Res.Parts), "hands hold three");
            Assert.AreEqual(3, inv.Deposit());
            inv.Pick(Res.Parts); inv.Pick(Res.Parts); inv.Pick(Res.Oxygen); inv.Deposit();
            inv.Pick(Res.Oxygen); inv.Pick(Res.Oxygen); inv.Pick(Res.Oxygen);
            Assert.AreEqual(2, inv.Deposit(), "lifeboat holds eight");
            inv.Pick(Res.Food);
            var d = inv.Delivered();
            Assert.AreEqual(2, d[Res.Food], "food still in hand is lost");
            Assert.AreEqual(3, d[Res.Oxygen]);
            Assert.AreEqual(1, d[Res.Battery]);
            Assert.AreEqual(2, d[Res.Parts]);
            Assert.AreEqual(0, inv.PodSpace);
        }

        [Test]
        public void Station_AddsDeliveredOnTopOfMinimumStock()
        {
            var c = new StationConfig();
            var empty = new StationSimulation(c, null, 1);
            Assert.AreEqual(c.baseFood, empty.Food);
            var s = New(1, Kit(3, 1, 2, 0));
            Assert.AreEqual(c.baseFood + 3, s.Food);
            Assert.AreEqual(c.baseOxygen + 1, s.Oxygen);
            Assert.AreEqual(c.baseBattery + 2, s.Battery);
            Assert.AreEqual(c.baseParts, s.Parts);
        }

        // ---------------------------------------------------------------- time

        [Test]
        public void Reading_And_Preview_DoNotMoveTime()
        {
            var s = New();
            s.ForceFault(Fault.Scrubber);
            int wu = s.WorkUnits, co2 = s.Co2, parts = s.Parts, day = s.Day;
            var p = s.Preview("repair_scrubber");
            _ = s.Co2Level; _ = s.HeatLevel; _ = s.ActiveFault; _ = s.Can("send", out _);
            Assert.IsTrue(p.ok);
            Assert.AreEqual(-1, p.parts); Assert.AreEqual(-1, p.wu);
            Assert.AreEqual(wu, s.WorkUnits); Assert.AreEqual(co2, s.Co2); Assert.AreEqual(parts, s.Parts); Assert.AreEqual(day, s.Day);
            Assert.IsFalse(s.ScrubberOk, "preview did not repair");
        }

        [Test]
        public void Preview_MatchesRealResult()
        {
            foreach (var a in new[] { "search", "shelter", "repair_scrubber", "absorber" })
            {
                var s = New(5);
                s.ForceFault(Fault.Scrubber);
                var p = s.Preview(a);
                var r = s.Do(a);
                Assert.AreEqual(p.co2, r.co2, a); Assert.AreEqual(p.parts, r.parts, a); Assert.AreEqual(p.wu, r.wu, a);
                Assert.AreEqual(p.food, r.food, a); Assert.AreEqual(p.absorbers, r.absorbers, a);
            }
        }

        [Test]
        public void OneAction_IsOneStep()
        {
            var s = New();
            s.ForceFault(Fault.Scrubber);
            s.Do("search");
            Assert.AreEqual(1, s.Co2, "one step of a broken scrubber");
            Assert.AreEqual(2, s.WorkUnits);
            s.Do("search");
            Assert.AreEqual(2, s.Co2);
        }

        [Test]
        public void ClosingAndReopening_PanelChangesNothing()
        {
            // the UI only reads state and calls Preview; doing that many times must leave the run untouched
            var s = New(3);
            s.ForceFault(Fault.Leak, Room.Comms);
            var before = (s.Oxygen, s.WorkUnits, s.LeakRoom, s.Day, s.Log.Count);
            for (int i = 0; i < 5; i++) { s.Preview("seal_room"); s.Preview("repair_leak"); _ = s.ActiveFault; }
            Assert.AreEqual(before, (s.Oxygen, s.WorkUnits, s.LeakRoom, s.Day, s.Log.Count));
        }

        [Test]
        public void Night_ComesAfterLastWorkUnit()
        {
            var s = New();
            int food = s.Food;
            s.Do("search"); s.Do("search"); s.Do("search");
            Assert.AreEqual(2, s.Day);
            Assert.AreEqual(food - 1, s.Food, "ate one at night");
            Assert.AreEqual(3, s.WorkUnits);
        }

        // ---------------------------------------------------------------- air

        [Test]
        public void Co2_And_O2_AreIndependent()
        {
            var a = New();
            a.ForceFault(Fault.Scrubber);
            int o2 = a.Oxygen;
            a.Do("shelter"); a.Do("search");
            Assert.AreEqual(o2, a.Oxygen, "broken scrubber: CO2 rises, oxygen stays");
            Assert.Greater(a.Co2, 0);

            var b = New();
            b.ForceFault(Fault.O2Gen);
            o2 = b.Oxygen;
            b.Do("search");
            Assert.AreEqual(o2 - 1, b.Oxygen, "broken O2 generator: oxygen falls");
            Assert.AreEqual(0, b.Co2, "scrubber still works: CO2 stays low");
        }

        [Test]
        public void Fan_Failure_RaisesCo2_UntilBackupOrRepair()
        {
            var s = New();
            s.ForceFault(Fault.Fan);
            s.Do("search");
            Assert.AreEqual(1, s.Co2);
            s.Do("backup_fan");
            Assert.AreEqual(0, s.Co2, "backup circuit moves air again");
        }

        // ---------------------------------------------------------------- incidents have working options

        static readonly Dictionary<Fault, string[]> Options = new Dictionary<Fault, string[]>
        {
            [Fault.Scrubber] = new[] { "repair_scrubber", "absorber", "shelter" },
            [Fault.Thermal] = new[] { "repair_thermal", "shed_load" },
            [Fault.Leak] = new[] { "repair_leak", "seal_room" },
            [Fault.Fan] = new[] { "repair_fan", "backup_fan" },
            [Fault.O2Gen] = new[] { "repair_o2gen", "low_activity" },
        };

        [Test]
        public void EveryIncident_HasAtLeastTwoWorkingOptions()
        {
            foreach (var kv in Options)
            {
                Assert.GreaterOrEqual(kv.Value.Length, 2);
                foreach (var a in kv.Value)
                {
                    var s = New(2, Kit(parts: 2));
                    s.ForceFault(kv.Key, Room.Comms);
                    Assert.IsTrue(s.Can(a, out var why), kv.Key + " " + a + " " + why);
                    var r = s.Do(a);
                    Assert.IsTrue(r.ok, kv.Key + " " + a);
                }
            }
        }

        [Test]
        public void Repairs_EndTheFault()
        {
            foreach (var f in new[] { Fault.Scrubber, Fault.Thermal, Fault.Leak, Fault.Fan, Fault.O2Gen })
            {
                var s = New(2, Kit(parts: 2));
                s.ForceFault(f, Room.Storage);
                s.Do(f == Fault.Leak ? "repair_leak" : "repair_" + (f == Fault.O2Gen ? "o2gen" : f.ToString().ToLower()));
                Assert.AreEqual(Fault.None, s.ActiveFault, f.ToString());
            }
        }

        [Test]
        public void NoParts_StillHasAWayOut()
        {
            var s = New(2, Kit(parts: 0));
            s.Parts = 0;
            s.Signal = 1;
            s.ForceFault(Fault.Scrubber);
            Assert.IsFalse(s.Can("repair_scrubber", out _));
            Assert.IsTrue(s.Can("cannibalize", out _));
            s.Do("cannibalize");
            Assert.AreEqual(1, s.Parts);
            Assert.AreEqual(0, s.Signal, "known cost: one signal step");
            Assert.IsTrue(s.Can("repair_scrubber", out _));
        }

        [Test]
        public void HeatTripsTransmitter_ShedLoadCoolsIt()
        {
            var s = New();
            s.ForceFault(Fault.Thermal);
            s.Do("search"); s.Do("search"); s.Do("search");
            Assert.IsTrue(s.TransmitterTripped);
            Assert.IsFalse(s.Can("send", out var why)); Assert.AreEqual("too_hot", why);
            s.Do("shed_load");
            Assert.Less(s.Heat, s.C.heatTrip);
        }

        [Test]
        public void PowerDip_HasThreeAnswers()
        {
            foreach (var a in new[] { "dip_battery", "dip_shed", "dip_cancel" })
            {
                var s = New();
                s.Battery = 5;
                s.PowerDipPending = true;
                Assert.IsFalse(s.Can("search", out _), "the dip must be answered first");
                Assert.IsTrue(s.Can(a, out var why), a + " " + why);
                s.Do(a);
                Assert.IsFalse(s.PowerDipPending);
            }
        }

        [Test]
        public void SealedRoom_CannotBeUsed_UntilRepaired()
        {
            var s = New(1, Kit(parts: 2));
            s.ForceFault(Fault.Leak, Room.Comms);
            s.Do("seal_room");
            Assert.IsFalse(s.Usable(Room.Comms));
            Assert.IsFalse(s.Can("send", out var why)); Assert.AreEqual("room_closed", why);
            s.Do("repair_leak");
            Assert.IsTrue(s.Usable(Room.Comms));
        }

        [Test]
        public void DetachedRoom_StaysInactive_AndIsNeverTargeted()
        {
            for (int seed = 1; seed <= 40; seed++)
            {
                var s = New(seed, Kit(5, 5, 5, 5));
                s.ForceFault(Fault.Leak, Room.Storage);
                s.Do("detach_storage");
                Assert.AreEqual(0, s.FindsLeft);
                Assert.IsFalse(s.Can("search", out _));
                for (int i = 0; i < 60 && s.Outcome == Outcome.Ongoing; i++)
                {
                    s.Do(LastDockBot.Choose(s, BotPlan.Balanced));
                    Assert.AreNotEqual(Room.Storage, s.LeakRoom, "seed " + seed);
                    Assert.IsTrue(s.Detached.Contains(Room.Storage));
                }
            }
        }

        // ---------------------------------------------------------------- director fairness

        [Test]
        public void Director_IsSeeded_OneAtATime_NoRepeats()
        {
            for (int seed = 1; seed <= 30; seed++)
            {
                var a = LastDockBot.Play(new StationConfig(), Kit(), seed, BotPlan.Balanced);
                var b = LastDockBot.Play(new StationConfig(), Kit(), seed, BotPlan.Balanced);
                CollectionAssert.AreEqual(a.Log, b.Log, "same seed, same run");
                string last = null;
                foreach (var e in a.Log)
                {
                    if (!e.StartsWith("fault_")) continue;
                    string kind = e.Split('_')[1];
                    Assert.AreNotEqual(last, kind, "seed " + seed + " repeated " + kind);
                    last = kind;
                }
            }
        }

        [Test]
        public void DayOne_IsQuiet()
        {
            for (int seed = 1; seed <= 30; seed++)
            {
                var s = New(seed);
                while (s.Day == 1)
                {
                    Assert.IsFalse(s.Log.Exists(e => e.StartsWith("fault_")), "seed " + seed);
                    s.Do("search");
                }
            }
        }

        [Test]
        public void IncidentsStart_OnlyWithAMoveLeft()
        {
            for (int seed = 1; seed <= 30; seed++)
            {
                var s = new StationSimulation(new StationConfig(), Kit(), seed);
                for (int i = 0; i < 200 && s.Outcome == Outcome.Ongoing; i++)
                {
                    bool hadFault = s.ActiveFault != Fault.None;
                    s.Do(LastDockBot.Choose(s, BotPlan.Balanced));
                    if (!hadFault && s.ActiveFault != Fault.None && s.ActiveFault != Fault.PowerDip)
                        Assert.GreaterOrEqual(s.WorkUnits, 1, "seed " + seed);
                }
            }
        }

        // ---------------------------------------------------------------- whole runs

        [Test]
        public void TwoDifferentPlans_CanWin()
        {
            int balanced = 0, battery = 0;
            for (int seed = 1; seed <= 20; seed++)
            {
                if (LastDockBot.Play(new StationConfig(), Kit(), seed, BotPlan.Balanced).Outcome == Outcome.Rescued) balanced++;
                if (LastDockBot.Play(new StationConfig(), Kit(), seed, BotPlan.BatteryFirst).Outcome == Outcome.Rescued) battery++;
            }
            Assert.GreaterOrEqual(balanced, 12, "balanced plan wins most seeds (phase 3 target: ~70%, the game should feel tense)");
            Assert.GreaterOrEqual(battery, 12, "battery-first plan wins most seeds");
        }

        /// <summary>Balance readout (always passes): wins per plan and kit over 50 seeds, plus the usual cause of loss.</summary>
        [Test]
        public void BalanceReport()
        {
            var sb = new System.Text.StringBuilder();
            var kits = new Dictionary<string, Dictionary<Res, int>>
            {
                ["even"] = Kit(), ["lowParts"] = Kit(3, 2, 3, 0), ["batteryHeavy"] = Kit(1, 1, 5, 1), ["noFood"] = Kit(0, 3, 3, 2), ["empty"] = Kit(0, 0, 0, 0), ["foodAir"] = Kit(3, 5, 0, 0),
            };
            foreach (var k in kits)
                foreach (BotPlan p in System.Enum.GetValues(typeof(BotPlan)))
                {
                    int wins = 0; var causes = new Dictionary<string, int>();
                    for (int seed = 1; seed <= 50; seed++)
                    {
                        var s = LastDockBot.Play(new StationConfig(), k.Value, seed, p);
                        if (s.Outcome == Outcome.Rescued) wins++;
                        else { string c = s.Log.Count > 1 ? s.Log[s.Log.Count - 2] : "?"; causes[c] = causes.TryGetValue(c, out var n) ? n + 1 : 1; }
                    }
                    sb.Append(k.Key).Append('/').Append(p).Append(": ").Append(wins).Append("/50 ");
                    foreach (var c in causes) sb.Append(c.Key).Append('=').Append(c.Value).Append(' ');
                    sb.AppendLine();
                }
            Assert.Pass(sb.ToString());
        }

        [Test]
        public void LowParts_StillWinnable()
        {
            int wins = 0;
            for (int seed = 1; seed <= 20; seed++)
                if (LastDockBot.Play(new StationConfig(), Kit(3, 2, 3, 0), seed, BotPlan.Balanced).Outcome == Outcome.Rescued) wins++;
            Assert.GreaterOrEqual(wins, 10);
        }

        [Test]
        public void Neglect_Loses_WithACauseChain()
        {
            int losses = 0;
            for (int seed = 1; seed <= 20; seed++)
            {
                var s = LastDockBot.Play(new StationConfig(), Kit(), seed, BotPlan.Neglect);
                if (s.Outcome == Outcome.Lost) { losses++; Assert.Contains("lost", s.Log); }
            }
            Assert.GreaterOrEqual(losses, 15, "ignoring faults should usually lose");
        }

        [Test]
        public void Restart_ClearsEverything()
        {
            var a = LastDockBot.Play(new StationConfig(), Kit(), 4, BotPlan.Neglect);
            var b = new StationSimulation(new StationConfig(), Kit(), 4);
            var fresh = new StationSimulation(new StationConfig(), Kit(), 4);
            Assert.AreNotEqual(Outcome.Ongoing, a.Outcome);
            Assert.AreEqual((fresh.Day, fresh.Food, fresh.Oxygen, fresh.Battery, fresh.Parts, fresh.Co2, fresh.Heat, fresh.Signal, fresh.Health),
                            (b.Day, b.Food, b.Oxygen, b.Battery, b.Parts, b.Co2, b.Heat, b.Signal, b.Health));
            Assert.AreEqual(0, b.Log.Count);
            Assert.AreEqual(Outcome.Ongoing, b.Outcome);
        }

        // ---------------------------------------------------------------- special items from the ship (phase 2)

        static Dictionary<Res, int> With(Res item) { var k = Kit(); k[item] = 1; return k; }

        [Test]
        public void Radio_NeedsThreeDays_OrOneWithTheKit()
        {
            var s = New(1);
            s.Director.Silent = true;
            Assert.IsFalse(s.Can("send", out var why)); Assert.AreEqual("radio_broken", why);
            s.Do("repair_radio");
            Assert.IsFalse(s.Can("repair_radio", out why), "one session a day"); Assert.AreEqual("once_a_day", why);
            s.Do("end_day"); s.Do("repair_radio"); s.Do("end_day"); s.Do("repair_radio");
            Assert.IsTrue(s.RadioFixed); Assert.AreEqual(3, s.Day);
            var k = New(1, With(Res.RadioKit));
            k.Do("repair_radio");
            Assert.IsTrue(k.RadioFixed, "the kit fixes it in one session");
            Assert.AreEqual(1, k.Day);
        }

        [Test]
        public void PortableSolar_AddsOnePowerEachNight()
        {
            var a = New(1); var b = New(1, With(Res.Solar));
            a.Director.Silent = b.Director.Silent = true;
            a.Do("end_day"); b.Do("end_day");
            Assert.AreEqual(a.Battery + 1, b.Battery);
        }

        [Test]
        public void FrozenFood_AddsSafeMeals_Medkit_Heals()
        {
            Assert.AreEqual(New(1).Food + new StationConfig().frozenMeals, New(1, With(Res.FrozenFood)).Food);
            var s = New(1, With(Res.Medkit));
            Assert.IsFalse(s.Can("use_medkit", out _), "full health");
            s.Health = 2;
            s.Do("use_medkit");
            Assert.AreEqual(3, s.Health); Assert.AreEqual(0, s.Medkits);
        }

        [Test]
        public void Flashlight_IsAFreeAnswerToAPowerDip()
        {
            var s = New(1, With(Res.Flashlight));
            s.PowerDipPending = true; int bat = s.Battery;
            Assert.IsTrue(s.Can("dip_flashlight", out _));
            s.Do("dip_flashlight");
            Assert.AreEqual(1, s.Signal); Assert.AreEqual(bat + 1, s.Battery, "the send's battery comes back");
            var t = New(1); t.PowerDipPending = true;
            Assert.IsFalse(t.Can("dip_flashlight", out _));
        }

        [Test]
        public void OneSiteIsHotEachDay_AMeterShowsWhich()
        {
            for (int seed = 1; seed <= 40; seed++)
            {
                var a = New(seed); a.Director.Silent = true;
                if (a.StormToday) continue;
                int hot = a.HotSite, safe = (hot + 1) % StationSimulation.Sites;
                a.Do("explore_" + hot);
                Assert.AreEqual(2, a.Health, "the hot spot hurts");
                Assert.IsFalse(a.Can("explore_" + safe, out _), "once a day");
                var b = New(seed, With(Res.RadMeter)); b.Director.Silent = true;
                b.Do("explore_" + safe);
                Assert.AreEqual(3, b.Health, "a meter lets you choose the safe site");
            }
        }

        [Test]
        public void SolarStorm_IsWarnedTheDayBefore_ShelterCostsPower()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var s = New(seed); s.Director.Silent = true;
                Assert.GreaterOrEqual(s.StormDay, s.C.stormEarliest);
                while (!s.StormTomorrow) s.Do("end_day");
                Assert.IsFalse(s.StormToday);
                s.Do("end_day");
                Assert.IsTrue(s.StormToday);
                var t = s.Clone(); t.Director.Silent = true;
                for (int i = 0; i < 3; i++) Assert.IsTrue(t.IsHot(i), "every site is hot in a storm");
                int hp = s.Health, bat = s.Battery;
                t.Do("end_day");
                Assert.AreEqual(hp - 1, t.Health, "no shelter: radiation gets in");
                s.Do("prepare_shelter"); s.Do("end_day");
                Assert.AreEqual(hp, s.Health, "the powered shelter protects");
                Assert.Contains("shelter_on", s.Log);
            }
        }

        [Test]
        public void Rover_NeedsThreeParts_AssembleCharge_AndSolarUpgrade()
        {
            var s = New(3, With(Res.Solar)); s.Director.Silent = true;
            Assert.IsFalse(s.Can("assemble_rover", out var why)); Assert.AreEqual("rover_parts", why);
            s.RoverParts = 3;
            s.Do("assemble_rover");
            Assert.IsTrue(s.RoverBuilt);
            int bat = s.Battery;
            var c = s.Clone(); c.Director.Silent = true; c.Do("send_rover");
            Assert.LessOrEqual(c.Battery, bat - 1, "a trip costs a big charge");
            s.Do("rover_solar");
            Assert.IsFalse(s.HasSolar, "the panel moved from the base to the rover");
            Assert.AreEqual(s.C.solarPerDay, s.SolarToday);
            int b2 = s.Battery; s.Do("send_rover");
            Assert.GreaterOrEqual(s.Battery, b2, "a solar rover charges itself");
            Assert.IsFalse(s.Can("send_rover", out why));
        }

        [Test]
        public void MysteryFood_IsAGamble_MedicineFixesASickDay()
        {
            int spoiled = 0, good = 0;
            for (int seed = 1; seed <= 60; seed++)
            {
                var s = New(seed); s.Director.Silent = true; s.MysteryFood = 1;
                var r = s.Do("open_mystery");
                if (s.Sick) spoiled++; else good++;
            }
            Assert.Greater(spoiled, 3); Assert.Greater(good, 30);
            var t = New(1, With(Res.Medkit)); t.Sick = true;
            Assert.IsTrue(t.Can("use_medkit", out _));
            t.Do("use_medkit");
            Assert.IsFalse(t.Sick);
        }

        [Test]
        public void Raiders_MustBeAnswered_FlashlightScaresThem()
        {
            var s = New(1); s.RaiderPending = true;
            Assert.IsFalse(s.Can("search", out var why)); Assert.AreEqual("raid_pending", why);
            int food = s.Food;
            s.Do("raid_refuse");
            s.Director.Silent = true; s.Do("end_day");
            Assert.Contains("raiders_stole", s.Log);
            var f = New(1, With(Res.Flashlight)); f.Director.Silent = true; f.RaiderPending = true;
            f.Do("raid_refuse"); f.Do("end_day");
            Assert.Contains("raiders_scared", f.Log);
        }

        [Test]
        public void TwoDarkNights_BringAnIntruder_WithAWarningFirst()
        {
            var s = New(1); s.Director.Silent = true; s.Battery = 0;
            s.Do("end_day");
            Assert.Contains("dark_warning", s.Log);
            s.Battery = 0; s.Do("end_day");
            Assert.Contains("intruder_stole", s.Log);
        }

        [Test]
        public void SpecialItems_TakeALifeboatSlot()
        {
            var inv = new ScavengeInventory();
            inv.Pick(Res.Solar); inv.Pick(Res.Medkit); inv.Deposit();
            Assert.AreEqual(ScavengeInventory.PodMax - 2, inv.PodSpace);
            Assert.AreEqual(1, inv.Delivered()[Res.Solar]);
        }

        [Test]
        public void Win_NeedsSignalAndDockPower()
        {
            var s = New(1, Kit(5, 5, 5, 5));
            s.Director.Silent = true;
            s.Signal = 3; s.Battery = 1; s.Day = 7;
            s.Do("end_day");
            Assert.AreEqual(Outcome.Lost, s.Outcome);
            Assert.Contains("dock_power_missing", s.Log);
            var t = New(1, Kit(5, 5, 5, 5));
            t.Director.Silent = true;
            t.Signal = 3; t.Battery = 4; t.Day = 7;
            t.Do("end_day");
            Assert.AreEqual(Outcome.Rescued, t.Outcome);
        }
    }
}
