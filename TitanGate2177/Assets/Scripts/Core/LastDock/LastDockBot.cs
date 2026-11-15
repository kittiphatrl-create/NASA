// LAST DOCK — simple scripted players for tests and auto-play: they only use actions a player could press.
using System.Collections.Generic;

namespace TitanGate.Core.LastDock
{
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
                if (plan == BotPlan.BatteryFirst) yield return "dip_shed";
                if (s.Battery >= 4) yield return "dip_battery";
                yield return "dip_shed";
                yield return "dip_cancel";
                yield break;
            }
            if (plan != BotPlan.Neglect)
            {
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
                }
                if (s.Co2 >= s.C.co2High) { yield return "absorber"; yield return "shelter"; }
                if (s.Sealed.Contains(Room.Comms) && s.Signal < s.C.signalNeeded || s.Sealed.Count > 0 && s.Parts >= 3) yield return "repair_leak";
            }
            if (plan != BotPlan.Neglect && s.Health < s.C.health) yield return "use_medkit";
            if (plan != BotPlan.Neglect && s.StormToday) yield return "prepare_shelter";
            if (plan != BotPlan.Neglect && s.Food <= 2) yield return "open_mystery";
            if (plan != BotPlan.Neglect)
            {
                yield return "assemble_rover";
                if (s.RoverBroken && s.Parts >= 2) yield return "repair_rover";
                if (s.RoverSolar || s.Battery >= s.C.dockPower + 4) yield return "send_rover";
            }
            yield return "repair_radio";   // quest 1: the radio must work before any send
            int sendsLeft = s.C.signalNeeded - s.Signal;
            int reserve = s.C.dockPower + (plan == BotPlan.BatteryFirst ? 1 : 0);
            bool late = s.Day >= s.C.days - sendsLeft;
            if (plan == BotPlan.BatteryFirst)
            {
                yield return "search";
                if (s.Battery - 1 >= reserve || late) yield return "send";
            }
            else
            {
                if (s.Battery - 1 >= reserve || late) yield return "send";
                yield return "search";
            }
            if (plan != BotPlan.Neglect && !s.StormToday && (s.HasRadMeter || s.Parts == 0 && s.Health == s.C.health))
            {
                // with a radiation meter the bot sees today's hot spot and picks a safe site; without one it guesses
                int want = s.RoverParts < s.C.roverPartsNeeded ? 1 : 0;
                if (s.HasRadMeter && s.IsHot(want)) want = s.IsHot(0) ? 2 : 0;
                yield return "explore_" + want;
            }
            yield return "end_day";
        }

        /// <summary>Play a whole run; returns the finished simulation.</summary>
        public static StationSimulation Play(StationConfig c, Dictionary<Res, int> delivered, int seed, BotPlan plan, int maxActions = 200)
        {
            var s = new StationSimulation(c, delivered, seed);
            for (int i = 0; i < maxActions && s.Outcome == Outcome.Ongoing; i++) s.Do(Choose(s, plan));
            return s;
        }
    }
}
