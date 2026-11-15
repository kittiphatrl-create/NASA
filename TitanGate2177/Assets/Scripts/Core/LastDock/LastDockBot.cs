// LAST DOCK — simple scripted players for tests and auto-play: they only use actions a player could press.
using System.Collections.Generic;

namespace TitanGate.Core.LastDock
{
    /// <summary>Balanced = the radio route; BatteryFirst = the beacon route (power-hungry, few work units); Neglect = ignores everything.</summary>
    public enum BotPlan { Balanced, BatteryFirst, Neglect }

    public static class LastDockBot
    {
        /// <summary>The next action this plan would press. Always returns an action that Can() accepts.</summary>
        public static string Choose(StationSimulation s, BotPlan plan)
        {
            foreach (var a in Wishes(s, plan))
                if (s.Can(a, out _)) return a;
            return "end_day";
        }

        /// <summary>The power plan a sensible manager wants tonight: life support first, extras only if the battery allows.</summary>
        static bool[] WantPower(StationSimulation s, BotPlan plan)
        {
            var want = new bool[s.On.Length];
            want[(int)Sys.Scrubber] = true;
            want[(int)Sys.O2Gen] = s.Water > 0;
            want[(int)Sys.Heater] = true;
            want[(int)Sys.Beacon] = s.BeaconBuilt && s.Signal < s.C.signalNeeded;
            want[(int)Sys.Recycler] = s.Water <= 6;
            int sendsLeft = plan == BotPlan.Balanced ? s.C.signalNeeded - s.Signal : 0;
            int floor = s.Day >= s.C.days ? s.C.dockPower : 1 + (s.Day >= s.C.days - 1 ? sendsLeft : 0);
            int Draw()
            {
                int d = s.C.baseLoad + (s.BackupCircuit ? 1 : 0) + (s.StormToday && s.ShelterReady ? 1 : 0);
                foreach (Sys x in System.Enum.GetValues(typeof(Sys))) if (want[(int)x]) d += s.DrawOf(x);
                return d;
            }
            foreach (var x in new[] { Sys.Recycler, Sys.Heater, Sys.Beacon, Sys.O2Gen })
            {
                if (s.Battery + s.SolarToday - Draw() >= floor) break;
                want[(int)x] = false;
            }
            return want;
        }

        static IEnumerable<string> Wishes(StationSimulation s, BotPlan plan)
        {
            if (s.DinnerPending) { s.MealPlan = s.AutoMeals(); yield return "dinner"; yield break; }
            if (s.RaiderPending)
            {
                if (s.Food >= 5) yield return "raid_food";
                if (s.Battery >= s.C.dockPower + 3) yield return "raid_battery";
                yield return "raid_refuse";
                yield break;
            }
            if (s.PowerDipPending)
            {
                yield return "dip_flashlight";
                if (s.Battery >= 4) yield return "dip_battery";
                yield return "dip_shed";
                yield return "dip_cancel";
                yield break;
            }
            if (plan != BotPlan.Neglect)
            {
                // the power plan first: switching is free
                var want = WantPower(s, plan);
                foreach (Sys x in System.Enum.GetValues(typeof(Sys)))
                    if (want[(int)x] != s.On[(int)x]) yield return (want[(int)x] ? "power_on_" : "power_off_") + StationSimulation.Key(x);
                switch (s.ActiveFault)
                {
                    case Fault.Leak:
                        yield return "repair_leak";
                        if (s.LeakRoom == Room.Storage && s.FindsLeft == 0) yield return "detach_storage";
                        yield return "seal_room";
                        break;
                    case Fault.Scrubber:
                        yield return "repair_scrubber"; yield return "absorber"; yield return "cannibalize"; yield return "shelter";
                        break;
                    case Fault.Thermal:
                        yield return "repair_thermal"; yield return "shed_load"; yield return "cannibalize";
                        break;
                    case Fault.Fan:
                        yield return "repair_fan"; yield return "backup_fan";
                        break;
                    case Fault.O2Gen:
                        yield return "repair_o2gen"; yield return "cannibalize"; yield return "low_activity";
                        break;
                    case Fault.Recycler:
                        if (s.Parts >= 2) yield return "repair_recycler";
                        break;
                }
                if (s.Co2 >= s.C.co2High) { yield return "absorber"; yield return "shelter"; }
                if (s.Sealed.Contains(Room.Comms) && s.Signal < s.C.signalNeeded || s.Sealed.Count > 0 && s.Parts >= 3) yield return "repair_leak";
                if (s.Health < s.C.health) yield return "use_medkit";
                if (s.StormToday) yield return "prepare_shelter";
                // keep machines out of the danger zone (service is cheaper than repair)
                Machine? worst = null;
                foreach (Machine m in System.Enum.GetValues(typeof(Machine)))
                    if (s.Running(m) && s.IsOk(m) && (worst == null || s.Cond[(int)m] < s.Cond[(int)worst.Value])) worst = m;
                if (worst != null && s.Cond[(int)worst.Value] < 50) yield return "service_" + StationSimulation.Key(worst.Value);
                if (s.Water <= 2) yield return "ice";
                if (s.Food <= 2) yield return "open_mystery";
                if (s.SolarToday <= 3 && s.Dust >= 30) yield return "clean_panels";
            }
            int sendsLeft = s.C.signalNeeded - s.Signal;
            if (plan == BotPlan.BatteryFirst && !s.BeaconBuilt)
            {
                yield return "build_beacon";
                if (!s.StormToday && s.RoverParts < s.C.beaconParts && !(s.HasRadMeter && s.IsHot(1))) yield return "explore_1";
            }
            bool late = s.Day >= s.C.days - sendsLeft;
            if (plan != BotPlan.BatteryFirst || late && !s.BeaconBuilt || s.Day >= s.C.days - 1 && s.Signal < s.C.signalNeeded)
            {
                yield return "repair_radio";
                yield return "radio_antenna";
                int reserve = s.C.dockPower;
                if (s.Battery - 1 >= reserve || late) yield return "send";
            }
            if (plan != BotPlan.Neglect)
            {
                if (s.Water <= 4) yield return "ice";
                yield return "search";
                if (s.Dust >= 40) yield return "clean_panels";
            }
            yield return "end_day";
        }

        /// <summary>Play a whole run; returns the finished simulation.</summary>
        public static StationSimulation Play(StationConfig c, Dictionary<Res, int> delivered, int seed, BotPlan plan, int maxActions = 400)
        {
            var s = new StationSimulation(c, delivered, seed);
            for (int i = 0; i < maxActions && s.Outcome == Outcome.Ongoing; i++) s.Do(Choose(s, plan));
            return s;
        }
    }
}
