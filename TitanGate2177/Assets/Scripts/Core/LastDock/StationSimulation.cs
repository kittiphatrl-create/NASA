// LAST DOCK — station rules. Pure C# (no Unity) so it runs in EditMode tests.
// Turn-based: a day has a few Work Units; every action that costs time does its work, moves every uncontrolled
// fault one step, and updates the systems. Reading panels, previews and the power plan never move time.
// Management core (docs/LASTDOCK_MANAGEMENT.md): a nightly power budget, machine condition that wears and can be
// serviced, a water-based production chain (O₂ maker, recycler, ice) and two rescue routes.
using System;
using System.Collections.Generic;

namespace TitanGate.Core.LastDock
{
    public enum Room { Core, LifeSupport, Thermal, Storage, Comms }
    /// <summary>Incidents of the design: CO₂ scrubber, thermal loop, room leak, air fan, power dip, O₂ maker, recycler.</summary>
    public enum Fault { None, Scrubber, Thermal, Leak, Fan, PowerDip, O2Gen, Recycler }
    public enum Outcome { Ongoing, Rescued, Lost }
    /// <summary>Machines that wear down (0–100 %).</summary>
    public enum Machine { Scrubber, O2Gen, Fan, Thermal, Recycler }
    /// <summary>Systems in the nightly power plan.</summary>
    public enum Sys { Scrubber, O2Gen, Recycler, Heater, Beacon }

    [Serializable]
    public class StationConfig
    {
        public int days = 7, workUnits = 3, health = 3;
        /// <summary>What the abandoned station still holds, so a poor scavenge can still be played.</summary>
        public int baseFood = 4, baseOxygen = 2, baseBattery = 6, baseParts = 2, baseAbsorbers = 1, baseWater = 8;
        public int signalNeeded = 3, dockPower = 2, baseLoad = 1;
        public int co2High = 2, co2Critical = 4, heatTrip = 3, heatCritical = 5;
        /// <summary>Spare things left in storage, found one per search, in this order.</summary>
        public List<string> storageFinds = new List<string> { "parts", "battery", "water", "absorber", "food", "parts" };
        public int cooldownDays = 2;
        public float powerDipChance = 0.25f;
        /// <summary>Radio route: board, power test, antenna (the repair kit does the first two).</summary>
        public int radioRepairDays = 3;
        /// <summary>Chance that a trip to a site walks through a radiation hot spot (none with a radiation meter).</summary>
        public float exploreRadiation = 0.3f;
        public int frozenMeals = 3;
        /// <summary>One solar storm per run, on a seeded day in this range; ORBI warns the day before.</summary>
        public int stormEarliest = 3, stormLatest = 6;
        public int roverPartsNeeded = 3, roverCharge = 2;
        public float roverBreakChance = 0.2f, raiderChance = 0.15f, mysterySpoilChance = 0.25f;
        /// <summary>People to feed each night (1 = the old single-mouth rule). With dinnerChoice the day ends in a dinner the
        /// player plans: who eats a meal, who eats an unlabelled box, who goes without.</summary>
        public int crew = 1;
        public bool dinnerChoice;
        /// <summary>Hunger: 1 = hungry (no effect), 2 = weak (one less work unit tomorrow), 3 = starving (the crew gets hurt).</summary>
        public int hungerWeak = 2, hungerStarve = 3;

        /// <summary>The settings the game plays with: just you, and a dinner to plan each night.</summary>
        public static StationConfig Game() => new StationConfig { crew = 1, dinnerChoice = true };   // you are alone: one person, one dinner to plan

        // ---- power budget
        public int batteryCap = 12, solarBase = 6, dustPerNight = 10, dustStorm = 35;
        /// <summary>Rover parts the beacon needs (the rover itself needs roverPartsNeeded).</summary>
        public int beaconParts = 2;
        public float stormSolar = 0.3f;
        public int drawScrubber = 1, drawO2Gen = 2, drawRecycler = 1, drawHeater = 1, drawBeacon = 1;
        // ---- machine condition
        public int condMin = 60, condMax = 90, wearPerNight = 7, wearHot = 6, serviceGain = 40, repairCond = 45;
        public float leakChance = 0.2f;
        // ---- production
        public int iceWater = 3;
    }

    /// <summary>What each person gets at dinner.</summary>
    public enum Meal { None = 0, Food = 1, Mystery = 2 }

    /// <summary>What an action would do (or did): the change in every number, and the messages.</summary>
    public class ActionResult
    {
        public bool ok;
        public string reason;
        public int food, oxygen, battery, parts, absorbers, co2, heat, signal, wu, health, water;
        public List<string> notes = new List<string>();
    }

    /// <summary>The morning board's look ahead for the current power plan.</summary>
    public struct Forecast
    {
        public int solar, draw, batteryTonight, waterPerNight, o2PerDay, foodPerNight;
        public int o2Days, waterDays, foodDays;
        public bool shortTonight;
    }

    public class StationSimulation
    {
        public readonly StationConfig C;
        public int Day = 1, WorkUnits, Health;
        public int Food, Oxygen, Battery, Parts, Absorbers, Water;
        public int Co2, Heat, Signal;
        public bool ScrubberOk = true, FanOk = true, O2GenOk = true, ThermalOk = true, RecyclerOk = true;
        public bool BackupCircuit, LoadShedToday;
        public Room? LeakRoom;
        public HashSet<Room> Sealed = new HashSet<Room>();
        public HashSet<Room> Detached = new HashSet<Room>();
        public int FindsUsed;
        public bool PowerDipPending;
        public Outcome Outcome = Outcome.Ongoing;
        public int FatigueTomorrow;
        /// <summary>Plain event keys in order, for the night log and the lose screen's cause chain.</summary>
        public List<string> Log = new List<string>();
        public IncidentDirector Director;
        /// <summary>Radio route progress: 0 none, 1 board, 2 power test, 3 antenna (= fixed).</summary>
        public int RadioRepair;
        public bool RadioWorkedToday, ExploredToday;
        public bool HasSolar, HasRadioKit, HasRadMeter, HasFlashlight;
        public int Medkits;
        /// <summary>Mars outside: storm, sites, rover, mystery food, raiders, dark nights.</summary>
        public int StormDay, MysteryFood, RoverParts, DarkNights;
        public bool ShelterReady, RoverBuilt, RoverSolar, RoverBroken, RoverToday, RaiderPending, RaidTonight, Sick;
        /// <summary>Dinner: hunger per person, and the plan the player sets before the "dinner" action.</summary>
        public int[] Hunger;
        public Meal[] MealPlan;
        public bool DinnerPending;
        /// <summary>Machine condition, 0–100 (index = Machine).</summary>
        public int[] Cond = new int[5];
        /// <summary>The power plan (index = Sys).</summary>
        public bool[] On = new bool[5];
        /// <summary>Dust on the solar panels, 0–90 %.</summary>
        public int Dust;
        public bool BeaconBuilt;
        bool _stepShelter, _stepLowActivity, _stepSent;
        int _actionsToday, _seed, _explores;
        public const int Sites = 3;   // 0 supply-drop wreck, 1 old rover wreck, 2 ridge cache
        public bool StormToday => Day == StormDay;
        public bool StormTomorrow => Day == StormDay - 1;
        /// <summary>Today's radiation hot spot outside (every site during a storm). A radiation meter shows it.</summary>
        public int HotSite => new Random(_seed * 131 + Day * 17).Next(Sites);
        public bool IsHot(int site) => StormToday || site == HotSite;

        public bool RadioFixed => RadioRepair >= C.radioRepairDays;
        public int RadioDaysNeeded => HasRadioKit ? 1 : C.radioRepairDays;
        /// <summary>Solar power the panels give tonight (dust and storm included).</summary>
        public int SolarToday => (int)Math.Round(C.solarBase * (1 - Dust / 100.0) * (StormToday ? C.stormSolar : 1)) + (HasSolar ? 1 : 0);

        public StationSimulation(StationConfig c, Dictionary<Res, int> delivered, int seed)
        {
            C = c; _seed = seed;
            Food = c.baseFood; Oxygen = c.baseOxygen; Battery = c.baseBattery; Parts = c.baseParts; Absorbers = c.baseAbsorbers; Water = c.baseWater;
            if (delivered != null)
            {
                Food += Get(delivered, Res.Food); Oxygen += Get(delivered, Res.Oxygen);
                Battery += Get(delivered, Res.Battery); Parts += Get(delivered, Res.Parts);
                HasSolar = Get(delivered, Res.Solar) > 0; HasRadioKit = Get(delivered, Res.RadioKit) > 0;
                HasRadMeter = Get(delivered, Res.RadMeter) > 0; HasFlashlight = Get(delivered, Res.Flashlight) > 0;
                Food += Get(delivered, Res.FrozenFood) * c.frozenMeals;
                Medkits = Get(delivered, Res.Medkit);
            }
            Battery = Math.Min(Battery, c.batteryCap);
            if (HasRadioKit) RadioRepair = Math.Max(RadioRepair, c.radioRepairDays - 1);   // board and power test done
            Health = c.health; WorkUnits = c.workUnits;
            Hunger = new int[Math.Max(1, c.crew)];
            StormDay = new Random(seed * 977 + 5).Next(c.stormEarliest, c.stormLatest + 1);
            var condRng = new Random(seed * 389 + 11);
            for (int i = 0; i < Cond.Length; i++) Cond[i] = condRng.Next(c.condMin, c.condMax + 1);
            On[(int)Sys.Scrubber] = On[(int)Sys.O2Gen] = On[(int)Sys.Heater] = true;   // what was running when the crew arrived
            Director = new IncidentDirector(seed, c);
        }

        static int Get(Dictionary<Res, int> d, Res r) => d.TryGetValue(r, out var n) ? n : 0;

        // ------------------------------------------------------------------ reading (never moves time)

        public string Co2Level => Co2 >= C.co2Critical ? "critical" : Co2 >= C.co2High ? "high" : "normal";
        public string HeatLevel => Heat >= C.heatCritical ? "critical" : Heat >= C.heatTrip ? "high" : "normal";
        public bool TransmitterTripped => Heat >= C.heatTrip;
        public bool Usable(Room r) => !Sealed.Contains(r) && !Detached.Contains(r);
        public Fault ActiveFault => !ScrubberOk ? Fault.Scrubber : !ThermalOk ? Fault.Thermal : LeakRoom != null ? Fault.Leak : !FanOk ? Fault.Fan : !O2GenOk ? Fault.O2Gen : !RecyclerOk ? Fault.Recycler : PowerDipPending ? Fault.PowerDip : Fault.None;
        public int FindsLeft => Detached.Contains(Room.Storage) ? 0 : C.storageFinds.Count - FindsUsed;

        public bool IsOk(Machine m) => m switch
        {
            Machine.Scrubber => ScrubberOk, Machine.O2Gen => O2GenOk, Machine.Fan => FanOk, Machine.Thermal => ThermalOk, _ => RecyclerOk,
        };
        /// <summary>Is this machine running (switched on, or always on), so it wears tonight?</summary>
        public bool Running(Machine m) => m switch
        {
            Machine.Scrubber => On[(int)Sys.Scrubber], Machine.O2Gen => On[(int)Sys.O2Gen], Machine.Recycler => On[(int)Sys.Recycler], _ => true,
        };
        public static Room RoomOf(Machine m) => m == Machine.Thermal ? Room.Thermal : Room.LifeSupport;
        public static string Key(Machine m) => m switch { Machine.Scrubber => "scrubber", Machine.O2Gen => "o2gen", Machine.Fan => "fan", Machine.Thermal => "thermal", _ => "recycler" };
        public static string Key(Sys s) => s switch { Sys.Scrubber => "scrubber", Sys.O2Gen => "o2gen", Sys.Recycler => "recycler", Sys.Heater => "heater", _ => "beacon" };

        public int DrawOf(Sys s) => s switch
        {
            Sys.Scrubber => C.drawScrubber, Sys.O2Gen => C.drawO2Gen, Sys.Recycler => C.drawRecycler, Sys.Heater => C.drawHeater, _ => C.drawBeacon,
        };

        /// <summary>Tonight's draw for the current plan (base load, switched-on systems, backup fan, storm shelter).</summary>
        public int DrawTonight()
        {
            int d = C.baseLoad + (BackupCircuit ? 1 : 0) + (StormToday && ShelterReady ? 1 : 0);
            foreach (Sys s in Enum.GetValues(typeof(Sys))) if (On[(int)s] && (s != Sys.Beacon || BeaconBuilt)) d += DrawOf(s);
            return d;
        }

        bool ScrubbingNow => On[(int)Sys.Scrubber] && ScrubberOk && (FanOk || BackupCircuit);
        bool MakingO2Now => On[(int)Sys.O2Gen] && O2GenOk && Water > 0;

        /// <summary>Look ahead for the current plan: tonight's battery and how long O₂, water and food last.</summary>
        public Forecast Look()
        {
            var f = new Forecast { solar = SolarToday, draw = DrawTonight() };
            f.batteryTonight = Math.Min(C.batteryCap, Battery + f.solar - f.draw);
            f.shortTonight = f.batteryTonight < 0;
            bool recycling = On[(int)Sys.Recycler] && RecyclerOk;
            f.waterPerNight = -1 + (recycling ? 1 : 0) - (MakingO2Now ? 1 : 0);
            f.o2PerDay = (MakingO2Now ? 1 : O2GenOk ? -1 : -C.workUnits) - (LeakRoom != null ? C.workUnits : 0);
            f.foodPerNight = -1;
            int Days(int stock, int per) => per >= 0 ? 99 : Math.Max(0, stock / -per);
            f.o2Days = Days(Oxygen, f.o2PerDay); f.waterDays = Days(Water, f.waterPerNight); f.foodDays = Days(Food, f.foodPerNight);
            return f;
        }

        public StationSimulation Clone()
        {
            var s = (StationSimulation)MemberwiseClone();
            s.Sealed = new HashSet<Room>(Sealed);
            s.Detached = new HashSet<Room>(Detached);
            s.Log = new List<string>(Log);
            s.Hunger = (int[])Hunger.Clone();
            if (MealPlan != null) s.MealPlan = (Meal[])MealPlan.Clone();
            s.Cond = (int[])Cond.Clone();
            s.On = (bool[])On.Clone();
            s.Director = Director.Clone();
            return s;
        }

        /// <summary>What an action would do, using the same rules as Do, on a copy; incidents are not rolled.</summary>
        public ActionResult Preview(string action)
        {
            var copy = Clone();
            copy.Director.Silent = true;
            return copy.Do(action);
        }

        static bool TryMachine(string key, out Machine m)
        {
            foreach (Machine x in Enum.GetValues(typeof(Machine))) if (Key(x) == key) { m = x; return true; }
            m = Machine.Scrubber; return false;
        }

        static bool TrySys(string key, out Sys s)
        {
            foreach (Sys x in Enum.GetValues(typeof(Sys))) if (Key(x) == key) { s = x; return true; }
            s = Sys.Scrubber; return false;
        }

        /// <summary>Free actions: switching power is planning, not work.</summary>
        public static bool IsFree(string action) => action.StartsWith("power_") || action.StartsWith("dip_") || action.StartsWith("raid_");

        // ------------------------------------------------------------------ acting

        /// <summary>Can this action be taken now? reason = a short key when not.</summary>
        public bool Can(string action, out string reason)
        {
            reason = null;
            if (Outcome != Outcome.Ongoing) { reason = "over"; return false; }
            if (DinnerPending != (action == "dinner")) { reason = DinnerPending ? "dinner_pending" : "not_needed"; return false; }
            if (action == "dinner") return true;
            bool dip = action.StartsWith("dip_"), raid = action.StartsWith("raid_");
            if (PowerDipPending != dip) { reason = dip ? "no_dip" : "dip_pending"; return false; }
            if (RaiderPending != raid) { reason = raid ? "no_raid" : "raid_pending"; return false; }
            if (action.StartsWith("power_on_") || action.StartsWith("power_off_"))
            {
                bool want = action.StartsWith("power_on_");
                if (!TrySys(action.Substring(want ? 9 : 10), out var sys)) return Fail(out reason, "unknown");
                if (sys == Sys.Beacon && !BeaconBuilt) return Fail(out reason, "no_beacon");
                return On[(int)sys] != want || Fail(out reason, "not_needed");
            }
            if (!dip && !raid && action != "end_day" && WorkUnits <= 0) { reason = "no_time"; return false; }
            if (action.StartsWith("service_"))
            {
                if (!TryMachine(action.Substring(8), out var m)) return Fail(out reason, "unknown");
                if (!IsOk(m)) return Fail(out reason, "broken");
                if (!Usable(RoomOf(m))) return Fail(out reason, "room_closed");
                return Cond[(int)m] <= 100 - 10 || Fail(out reason, "not_needed");
            }
            switch (action)
            {
                case "send":
                    if (!Usable(Room.Comms)) return Fail(out reason, "room_closed");
                    if (TransmitterTripped) return Fail(out reason, "too_hot");
                    if (!RadioFixed) return Fail(out reason, "radio_broken");
                    if (LoadShedToday) return Fail(out reason, "load_shed");
                    if (Battery < 1) return Fail(out reason, "no_battery");
                    if (Signal >= C.signalNeeded) return Fail(out reason, "done");
                    return true;
                case "search": return Usable(Room.Storage) && FindsLeft > 0 || Fail(out reason, Usable(Room.Storage) ? "empty" : "room_closed");
                case "repair_scrubber": return Need(!ScrubberOk, 1, out reason);
                case "repair_fan": return Need(!FanOk, 1, out reason);
                case "repair_o2gen": return Need(!O2GenOk, 1, out reason);
                case "repair_thermal": return Need(!ThermalOk, 1, out reason);
                case "repair_recycler": return Need(!RecyclerOk, 1, out reason);
                case "repair_leak": return Need(LeakRoom != null || Sealed.Count > 0, 2, out reason);
                case "absorber": return Absorbers > 0 || Fail(out reason, "no_absorber");
                // last resort so no repair is ever locked behind one item: strip a part from the comms rack
                case "cannibalize": return Parts == 0 && Usable(Room.Comms) && ActiveFault != Fault.None || Fail(out reason, "not_needed");
                case "shelter": case "low_activity": case "end_day": return true;
                case "shed_load": return !LoadShedToday || Fail(out reason, "done");
                case "backup_fan": return !FanOk && !BackupCircuit || Fail(out reason, "not_needed");
                case "seal_room": return LeakRoom != null || Fail(out reason, "not_needed");
                case "detach_storage": return LeakRoom == Room.Storage || Fail(out reason, "not_needed");
                case "dip_battery": return Battery >= 1 || Fail(out reason, "no_battery");
                case "dip_shed": case "dip_cancel": return true;
                case "dip_flashlight": return HasFlashlight || Fail(out reason, "no_flashlight");
                case "repair_radio":
                    // the first two radio steps happen in Communications: the board, then a power test
                    if (RadioRepair >= C.radioRepairDays - 1) return Fail(out reason, RadioFixed ? "done" : "antenna_next");
                    if (!Usable(Room.Comms)) return Fail(out reason, "room_closed");
                    if (RadioRepair == 0 && Parts < 1) return Fail(out reason, "no_parts");
                    if (RadioRepair == 1 && !ThermalOk) return Fail(out reason, "needs_cooling");
                    if (RadioRepair == 1 && Battery < 1) return Fail(out reason, "no_battery");
                    return true;
                case "radio_antenna":
                    if (RadioFixed) return Fail(out reason, "done");
                    if (RadioRepair < C.radioRepairDays - 1) return Fail(out reason, "radio_steps");
                    if (StormToday) return Fail(out reason, "storm");
                    return true;
                case "build_beacon":
                    if (BeaconBuilt) return Fail(out reason, "done");
                    if (RoverParts < C.beaconParts) return Fail(out reason, "rover_parts");
                    return Parts >= 1 || Fail(out reason, "no_parts");
                case "clean_panels":
                    if (StormToday) return Fail(out reason, "storm");
                    return Dust > 0 || Fail(out reason, "not_needed");
                case "ice": return !ExploredToday || Fail(out reason, "once_a_day");
                case "explore_0": case "explore_1": case "explore_2": return !ExploredToday || Fail(out reason, "once_a_day");
                case "prepare_shelter": return StormToday && !ShelterReady || Fail(out reason, StormToday ? "done" : "not_needed");
                case "open_mystery": return MysteryFood > 0 || Fail(out reason, "not_needed");
                case "assemble_rover":
                    if (RoverBuilt) return Fail(out reason, "not_needed");
                    if (RoverParts < C.roverPartsNeeded) return Fail(out reason, "rover_parts");
                    return Parts >= 1 || Fail(out reason, "no_parts");
                case "rover_solar": return RoverBuilt && HasSolar && !RoverSolar || Fail(out reason, "not_needed");
                case "send_rover":
                    if (!RoverBuilt) return Fail(out reason, "not_needed");
                    if (RoverBroken) return Fail(out reason, "rover_broken");
                    if (RoverToday) return Fail(out reason, "once_a_day");
                    return RoverSolar || Battery >= C.roverCharge || Fail(out reason, "no_battery");
                case "repair_rover": return Need(RoverBroken, 1, out reason);
                case "raid_food": return Food >= 2 || Fail(out reason, "no_food");
                case "raid_battery": return Battery >= 2 || Fail(out reason, "no_battery");
                case "raid_refuse": return true;
                case "use_medkit":
                    if (Medkits <= 0) return Fail(out reason, "no_medkit");
                    if (Health >= C.health && !Sick) return Fail(out reason, "not_needed");
                    return true;
            }
            reason = "unknown"; return false;
        }

        bool Need(bool broken, int parts, out string reason)
        {
            reason = null;
            if (!broken) { reason = "not_needed"; return false; }
            if (Parts < parts) { reason = "no_parts"; return false; }
            return true;
        }

        static bool Fail(out string reason, string why) { reason = why; return false; }

        /// <summary>Take an action. Time-costing actions do their work, then one step passes for every fault.</summary>
        public ActionResult Do(string action)
        {
            var before = Snap();
            if (!Can(action, out var why)) return new ActionResult { ok = false, reason = why };
            var notes = new List<string>();
            bool step = true;
            if (action.StartsWith("power_"))
            {
                bool want = action.StartsWith("power_on_");
                TrySys(action.Substring(want ? 9 : 10), out var sys);
                On[(int)sys] = want;
                notes.Add(action);
                var free = Diff(before); free.ok = true; free.notes.AddRange(notes);
                return free;
            }
            if (action.StartsWith("service_"))
            {
                TryMachine(action.Substring(8), out var m);
                Cond[(int)m] = Math.Min(100, Cond[(int)m] + C.serviceGain);
                notes.Add("serviced_" + Key(m)); Log.Add("serviced_" + Key(m));
            }
            switch (action)
            {
                case "send":
                    Battery -= 1;
                    _stepSent = true;
                    if (Director.RollPowerDip(this)) { PowerDipPending = true; step = false; WorkUnits -= 1; _actionsToday++; notes.Add("power_dip"); Log.Add("power_dip"); }
                    else { Signal += 1; notes.Add("signal_sent"); Log.Add("signal_sent"); }
                    break;
                case "search":
                    string f = C.storageFinds[FindsUsed++];
                    if (f == "parts") Parts++; else if (f == "food") Food++; else if (f == "battery") Battery = Math.Min(C.batteryCap, Battery + 1);
                    else if (f == "oxygen") Oxygen++; else if (f == "absorber") Absorbers++; else if (f == "water") Water += 2;
                    notes.Add("found_" + f);
                    break;
                case "repair_scrubber": Parts--; ScrubberOk = true; Cond[(int)Machine.Scrubber] = C.repairCond; notes.Add("fixed_scrubber"); Log.Add("fixed_scrubber"); break;
                case "repair_fan": Parts--; FanOk = true; BackupCircuit = false; Cond[(int)Machine.Fan] = C.repairCond; notes.Add("fixed_fan"); Log.Add("fixed_fan"); break;
                case "repair_o2gen": Parts--; O2GenOk = true; Cond[(int)Machine.O2Gen] = C.repairCond; notes.Add("fixed_o2gen"); Log.Add("fixed_o2gen"); break;
                case "repair_thermal": Parts--; ThermalOk = true; Cond[(int)Machine.Thermal] = C.repairCond; notes.Add("fixed_thermal"); Log.Add("fixed_thermal"); break;
                case "repair_recycler": Parts--; RecyclerOk = true; Cond[(int)Machine.Recycler] = C.repairCond; notes.Add("fixed_recycler"); Log.Add("fixed_recycler"); break;
                case "repair_leak": Parts -= 2; LeakRoom = null; Sealed.Clear(); notes.Add("fixed_leak"); Log.Add("fixed_leak"); break;
                case "absorber": Absorbers--; Co2 = Math.Max(0, Co2 - 3); notes.Add("absorber_used"); break;
                case "cannibalize": Parts++; if (Signal > 0) Signal--; notes.Add("cannibalized"); Log.Add("cannibalized"); break;
                case "shelter": _stepShelter = true; notes.Add("sheltered"); break;
                case "low_activity": _stepLowActivity = true; notes.Add("low_activity"); break;
                case "shed_load": LoadShedToday = true; Heat = Math.Max(0, Heat - 2); notes.Add("load_shed"); break;
                case "backup_fan": BackupCircuit = true; notes.Add("backup_fan"); break;
                case "seal_room": Sealed.Add(LeakRoom.Value); notes.Add("sealed_" + LeakRoom.Value); Log.Add("sealed"); LeakRoom = null; break;
                case "detach_storage": Detached.Add(Room.Storage); LeakRoom = null; Sealed.Remove(Room.Storage); notes.Add("detached_storage"); Log.Add("detached_storage"); break;
                case "dip_battery": PowerDipPending = false; Battery -= 1; Signal += 1; step = false; notes.Add("signal_sent"); Log.Add("signal_sent"); FinishDipStep(); break;
                case "dip_shed": PowerDipPending = false; Signal += 1; Heat += 1; Co2 += 1; step = false; notes.Add("signal_sent"); Log.Add("signal_sent"); FinishDipStep(); break;
                case "dip_cancel": PowerDipPending = false; Battery += 1; step = false; notes.Add("signal_cancelled"); FinishDipStep(); break;
                case "dip_flashlight": PowerDipPending = false; Battery += 1; Signal += 1; step = false; notes.Add("signal_sent"); Log.Add("signal_sent"); FinishDipStep(); break;
                case "repair_radio":
                    if (RadioRepair == 0) { Parts--; notes.Add("radio_board"); }
                    else { Battery -= 1; notes.Add("radio_tested"); }
                    RadioRepair++;
                    RadioWorkedToday = true;
                    Log.Add(RadioRepair == 1 ? "radio_board" : "radio_tested");
                    break;
                case "radio_antenna":
                    RadioRepair = C.radioRepairDays;
                    notes.Add("radio_fixed"); Log.Add("radio_fixed");
                    break;
                case "build_beacon":
                    Parts--; RoverParts -= C.beaconParts; BeaconBuilt = true; On[(int)Sys.Beacon] = true;
                    Signal++;   // its first ping goes out as soon as it is switched on
                    notes.Add("beacon_built"); Log.Add("beacon_built");
                    break;
                case "clean_panels": Dust = 0; notes.Add("panels_clean"); Log.Add("panels_clean"); break;
                case "ice":
                    ExploredToday = true;
                    if (StormToday) { notes.Add("explore_radiation"); Hurt("radiation"); break; }
                    Water += C.iceWater; notes.Add("found_ice"); Log.Add("ice");
                    break;
                case "explore_0": case "explore_1": case "explore_2":
                {
                    // a walk outside to one site; one site is a radiation hot spot each day (all of them in a storm)
                    int site = action[8] - '0';
                    ExploredToday = true;
                    var rng = new Random(_seed * 7919 + Day * 97 + _explores++);
                    if (IsHot(site)) { notes.Add("explore_radiation"); Hurt("radiation"); break; }
                    double roll = rng.NextDouble();
                    if (site == 0) { if (roll < 0.5) { Parts++; notes.Add("found_parts"); } else if (roll < 0.8) { Battery = Math.Min(C.batteryCap, Battery + 1); notes.Add("found_battery"); } else { Oxygen++; notes.Add("found_oxygen"); } }
                    else if (site == 1) { if (RoverParts < C.roverPartsNeeded) { RoverParts++; notes.Add("found_rover_part"); Log.Add("rover_part"); } else { Parts++; notes.Add("found_parts"); } }
                    else { MysteryFood += 2; notes.Add("found_mystery"); }
                    break;
                }
                case "prepare_shelter": ShelterReady = true; notes.Add("shelter_ready"); break;
                case "open_mystery":
                {
                    MysteryFood--;
                    var rng = new Random(_seed * 4243 + Day * 29 + MysteryFood);
                    double roll = rng.NextDouble();
                    if (roll < C.mysterySpoilChance) { Sick = true; notes.Add("food_spoiled"); Log.Add("food_poisoning"); }
                    else if (roll < C.mysterySpoilChance + 0.25) { Food += 2; notes.Add("food_great"); }
                    else { Food += 1; notes.Add("food_ok"); }
                    break;
                }
                case "assemble_rover": Parts--; RoverParts -= C.roverPartsNeeded; RoverBuilt = true; notes.Add("rover_built"); Log.Add("rover_built"); break;
                case "rover_solar": HasSolar = false; RoverSolar = true; notes.Add("rover_solar"); Log.Add("rover_solar"); break;
                case "send_rover":
                {
                    if (!RoverSolar) Battery -= C.roverCharge;
                    RoverToday = true;
                    var rng = new Random(_seed * 6151 + Day * 37);
                    Parts++; MysteryFood++; Water++;
                    if (rng.NextDouble() < 0.5) Battery = Math.Min(C.batteryCap, Battery + 1); else Oxygen++;
                    notes.Add("rover_back");
                    double ev = rng.NextDouble();
                    if (ev < C.roverBreakChance) { RoverBroken = true; notes.Add("rover_broke"); Log.Add("rover_broke"); }
                    else if (ev < C.roverBreakChance + C.raiderChance) { RaiderPending = true; notes.Add("raiders"); Log.Add("raiders"); }
                    break;
                }
                case "repair_rover": Parts--; RoverBroken = false; notes.Add("rover_fixed"); break;
                case "raid_food": RaiderPending = false; Food -= 2; step = false; notes.Add("raid_paid"); Log.Add("raid_paid"); break;
                case "raid_battery": RaiderPending = false; Battery -= 2; step = false; notes.Add("raid_paid"); Log.Add("raid_paid"); break;
                case "raid_refuse": RaiderPending = false; RaidTonight = true; step = false; notes.Add("raid_refused"); Log.Add("raid_refused"); break;
                case "use_medkit": Medkits--; if (Health < C.health) Health++; Sick = false; notes.Add("healed"); Log.Add("healed"); break;
                case "end_day":
                    // resting still lets time pass: each spare Work Unit is a step (broken things keep getting
                    // worse), but no new incident starts while the crew eats and sleeps
                    step = false; int dayAtStart = Day;
                    while (WorkUnits > 0 && Outcome == Outcome.Ongoing)
                    {
                        WorkUnits -= 1; _actionsToday++; Step();
                        if (Outcome != Outcome.Ongoing) break;
                        if (WorkUnits <= 0) { EndOfDay(); break; }
                    }
                    if (WorkUnits <= 0 && Outcome == Outcome.Ongoing && Day == dayAtStart && !DinnerPending) EndOfDay();
                    break;
            }
            if (action == "dinner") { DinnerPending = false; Night(); }
            if (step && action != "dinner") { WorkUnits -= 1; _actionsToday++; Step(); if (WorkUnits <= 0 && Outcome == Outcome.Ongoing) EndOfDay(); else if (Outcome == Outcome.Ongoing) Director.MaybeTrigger(this, _actionsToday); }
            var r = Diff(before); r.ok = true; r.notes.AddRange(notes);
            return r;
        }

        void FinishDipStep()
        {
            // the send already took its Work Unit; the power dip resolves inside the same step
            Step();
            if (WorkUnits <= 0 && Outcome == Outcome.Ongoing) EndOfDay();
        }

        /// <summary>One step of time: every system that is not under control gets a little worse.</summary>
        void Step()
        {
            // CO₂: the scrubber removes it when it is switched on, working, and the fan pushes air through it
            bool co2Rises = !ScrubbingNow;
            if (_stepShelter) co2Rises = false;
            Co2 = co2Rises ? Co2 + 1 : Math.Max(0, Co2 - 1);
            if (Co2 >= C.co2Critical) { Hurt("co2_critical"); }
            // heat: equipment makes heat; the loop carries it to the radiators
            bool heatRises = !ThermalOk && !LoadShedToday;
            Heat = heatRises ? Heat + 1 + (_stepSent ? 1 : 0) : Math.Max(0, Heat - 1 - (LoadShedToday ? 1 : 0));
            if (Heat >= C.heatTrip && !Log.Contains("transmitter_tripped@" + Day)) Log.Add("transmitter_tripped@" + Day);
            if (Heat >= C.heatCritical) { Battery = Math.Max(0, Battery - 1); Log.Add("heat_shutdown"); }
            // O₂: a BROKEN maker is an emergency (the tanks drain every step); switching it off or running it dry is a
            // planned choice that costs one tank a night (see Night)
            if (!O2GenOk && !_stepLowActivity) Breathe("o2gen_down");
            // a leaking room loses air until it is sealed or fixed
            if (LeakRoom != null) Breathe("leak");
            _stepShelter = _stepLowActivity = _stepSent = false;
            if (Health <= 0) Lose();
        }

        void Breathe(string why)
        {
            if (Oxygen > 0) Oxygen -= 1;
            else Hurt("no_oxygen_" + why);
        }

        void Hurt(string why)
        {
            Health -= 1; Log.Add(why);
            if (Health <= 0) Lose();
        }

        void Lose() { if (Outcome == Outcome.Ongoing) { Outcome = Outcome.Lost; Log.Add("lost"); } }

        /// <summary>The day's work is done: either straight to the night, or to a dinner the player plans first.</summary>
        void EndOfDay()
        {
            if (Outcome != Outcome.Ongoing) return;
            if (C.dinnerChoice) { DinnerPending = true; if (MealPlan == null) MealPlan = AutoMeals(); return; }
            Night();
        }

        /// <summary>A sensible default plan: feed the hungriest first while meals last, then unlabelled boxes.</summary>
        public Meal[] AutoMeals()
        {
            var plan = new Meal[Hunger.Length];
            // ration: spread the meals over the nights still to come; anyone about to go weak eats first
            int nightsLeft = Math.Max(1, C.days - Day + 1);
            int budget = Math.Min(Food, (Food + nightsLeft - 1) / nightsLeft);
            foreach (var h in Hunger) if (h >= C.hungerWeak - 1) budget = Math.Max(budget, 0);
            int urgent = 0; foreach (var h in Hunger) if (h >= C.hungerWeak - 1) urgent++;
            int food = Math.Min(Food, Math.Max(budget, urgent)), boxes = MysteryFood;
            var order = new List<int>(); for (int i = 0; i < Hunger.Length; i++) order.Add(i);
            order.Sort((a, b) => Hunger[b].CompareTo(Hunger[a]));
            foreach (var i in order)
            {
                if (food > 0) { plan[i] = Meal.Food; food--; }
                else if (boxes > 0 && Hunger[i] >= C.hungerWeak - 1) { plan[i] = Meal.Mystery; boxes--; }
            }
            return plan;
        }

        /// <summary>Is this plan possible with what is in stock?</summary>
        public bool PlanFits(Meal[] plan)
        {
            int f = 0, m = 0; foreach (var x in plan) { if (x == Meal.Food) f++; if (x == Meal.Mystery) m++; }
            return f <= Food && m <= MysteryFood;
        }

        /// <summary>Dinner: everyone in the plan eats; whoever goes without gets hungrier.</summary>
        void Eat()
        {
            if (C.crew <= 1 && !C.dinnerChoice) { if (Food > 0) Food -= 1; else Hurt("hungry"); return; }
            var plan = MealPlan != null && MealPlan.Length == Hunger.Length && PlanFits(MealPlan) ? MealPlan : AutoMeals();
            var rng = new Random(_seed * 7151 + Day * 13);
            for (int i = 0; i < Hunger.Length; i++)
            {
                switch (plan[i])
                {
                    case Meal.Food: Food--; Hunger[i] = 0; break;
                    case Meal.Mystery:
                        MysteryFood--; Hunger[i] = 0;
                        if (rng.NextDouble() < C.mysterySpoilChance) { Sick = true; Log.Add("food_poisoning"); }
                        break;
                    default:
                        Hunger[i]++;
                        if (Hunger[i] >= C.hungerStarve) { Hunger[i] = C.hungerStarve - 1; Hurt("hungry"); }
                        break;
                }
            }
            Log.Add("dinner");
            MealPlan = null;
        }

        /// <summary>The night: the power plan is paid, machines make water, oxygen and food, they wear; then the next day.</summary>
        void Night()
        {
            if (Outcome != Outcome.Ongoing) return;
            // ---- power: what cannot be paid switches itself off, least important first
            int solar = SolarToday;
            foreach (var s in new[] { Sys.Beacon, Sys.Recycler, Sys.Heater, Sys.O2Gen, Sys.Scrubber })
            {
                if (Battery + solar - DrawTonight() >= 0) break;
                if (On[(int)s] && (s != Sys.Beacon || BeaconBuilt)) { On[(int)s] = false; Log.Add("shed_" + Key(s)); }
            }
            Battery = Math.Min(C.batteryCap, Battery + solar - DrawTonight());
            // solar storm night: the shelter needs power (paid above), or radiation gets through the roof
            if (StormToday) { if (ShelterReady) Log.Add("shelter_on"); else Hurt("storm_radiation"); }
            if (Battery < 0) { Battery = 0; Hurt("no_power"); }

            // ---- water and food: recycle, drink, make oxygen, eat
            if (On[(int)Sys.Recycler] && RecyclerOk) { Water++; Log.Add("recycled"); }
            if (Water > 0) Water--; else Hurt("thirsty");
            if (MakingO2Now) { Water--; Oxygen++; }
            else if (O2GenOk) Breathe("o2gen_off");   // switched off or no water: the crew breathes from the tanks
            Eat();   // dinner: the meal plan (GameFlow.Dinner.cs)
            bool cold = !On[(int)Sys.Heater];
            if (cold) Log.Add("cold");
            // ---- the beacon sends on its own while it is powered
            if (BeaconBuilt && On[(int)Sys.Beacon]) { Signal++; Log.Add("beacon_signal"); }

            // ---- machines wear; one that reaches 0 % stops
            foreach (Machine m in Enum.GetValues(typeof(Machine)))
            {
                if (!Running(m) || !IsOk(m)) continue;
                int wear = C.wearPerNight + (m == Machine.Thermal && HeatLevel != "normal" ? C.wearHot : 0);
                Cond[(int)m] = Math.Max(0, Cond[(int)m] - wear);
                if (Cond[(int)m] == 0 && ActiveFault == Fault.None) Break(FaultOf(m));
            }
            Dust = Math.Min(90, Dust + (StormToday ? C.dustStorm : C.dustPerNight));

            // raiders you sent away come back in the dark (a flashlight scares them off) [fiction]
            if (RaidTonight) { if (HasFlashlight) Log.Add("raiders_scared"); else { Food = Math.Max(0, Food - 2); Log.Add("raiders_stole"); } }
            // two dark nights in a row (battery empty, lights off) and something gets in [fiction]
            DarkNights = Battery <= 0 ? DarkNights + 1 : 0;
            if (DarkNights == 1) Log.Add("dark_warning");
            if (DarkNights >= 2)
            {
                if (HasFlashlight) Log.Add("intruder_scared");
                else { if (Parts > 0) Parts--; else Food = Math.Max(0, Food - 1); Log.Add("intruder_stole"); }
            }
            bool weak = false; foreach (var h in Hunger) if (h >= C.hungerWeak) weak = true;
            if (weak && C.dinnerChoice) Log.Add("weak");
            FatigueTomorrow = (Co2 >= C.co2High ? 1 : 0) + (Sick ? 1 : 0) + (weak && C.dinnerChoice ? 1 : 0) + (cold ? 1 : 0);
            if (Co2 >= C.co2High) Log.Add("tired");
            if (Sick) Log.Add("sick");
            Sick = false;
            LoadShedToday = false; RadioWorkedToday = false; ExploredToday = false; ShelterReady = false; RoverToday = false; RaidTonight = false;
            if (Outcome != Outcome.Ongoing) return;
            if (Day >= C.days)
            {
                if (Signal >= C.signalNeeded && Battery >= C.dockPower) { Outcome = Outcome.Rescued; Log.Add("rescued"); }
                else { Log.Add(Signal < C.signalNeeded ? "signal_missing" : "dock_power_missing"); Lose(); }
                return;
            }
            Day++;
            WorkUnits = Math.Max(1, C.workUnits - FatigueTomorrow);
            _actionsToday = 0;
            Director.NewDay(this);
            Director.MaybeTrigger(this, 0);
        }

        public static Fault FaultOf(Machine m) => m switch
        {
            Machine.Scrubber => Fault.Scrubber, Machine.O2Gen => Fault.O2Gen, Machine.Fan => Fault.Fan, Machine.Thermal => Fault.Thermal, _ => Fault.Recycler,
        };

        // ------------------------------------------------------------------ diff helpers

        int[] Snap() => new[] { Food, Oxygen, Battery, Parts, Absorbers, Co2, Heat, Signal, WorkUnits, Health, Water };

        ActionResult Diff(int[] b)
        {
            var a = Snap();
            return new ActionResult { food = a[0] - b[0], oxygen = a[1] - b[1], battery = a[2] - b[2], parts = a[3] - b[3], absorbers = a[4] - b[4], co2 = a[5] - b[5], heat = a[6] - b[6], signal = a[7] - b[7], wu = a[8] - b[8], health = a[9] - b[9], water = a[10] - b[10] };
        }

        // test/director hooks
        internal void Break(Fault f, Room? room = null)
        {
            switch (f)
            {
                case Fault.Scrubber: ScrubberOk = false; Cond[(int)Machine.Scrubber] = Math.Min(Cond[(int)Machine.Scrubber], 15); break;
                case Fault.Thermal: ThermalOk = false; Cond[(int)Machine.Thermal] = Math.Min(Cond[(int)Machine.Thermal], 15); break;
                case Fault.Fan: FanOk = false; Cond[(int)Machine.Fan] = Math.Min(Cond[(int)Machine.Fan], 15); break;
                case Fault.O2Gen: O2GenOk = false; Cond[(int)Machine.O2Gen] = Math.Min(Cond[(int)Machine.O2Gen], 15); break;
                case Fault.Recycler: RecyclerOk = false; Cond[(int)Machine.Recycler] = Math.Min(Cond[(int)Machine.Recycler], 15); break;
                case Fault.Leak: LeakRoom = room ?? Room.Storage; break;
            }
            Log.Add("fault_" + f + (f == Fault.Leak ? "_" + LeakRoom : ""));
        }

        /// <summary>For tests and debugging: start a fault now (respects nothing).</summary>
        public void ForceFault(Fault f, Room? room = null) => Break(f, room);
    }
}
