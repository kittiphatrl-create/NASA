// LAST DOCK — station rules. Pure C# (no Unity) so it runs in EditMode tests.
// Turn-based: a day has a few Work Units; every action that costs time does its work, moves every uncontrolled
// fault one step, and updates the systems. Reading panels and previews never move time.
using System;
using System.Collections.Generic;

namespace TitanGate.Core.LastDock
{
    public enum Room { Core, LifeSupport, Thermal, Storage, Comms }
    /// <summary>Incidents A-F of the design: CO₂ scrubber, thermal loop, room leak, air fan, power dip, O₂ generator.</summary>
    public enum Fault { None, Scrubber, Thermal, Leak, Fan, PowerDip, O2Gen }
    public enum Outcome { Ongoing, Rescued, Lost }

    [Serializable]
    public class StationConfig
    {
        public int days = 7, workUnits = 3, health = 3;
        /// <summary>What the abandoned station still holds, so a poor scavenge can still be played.</summary>
        public int baseFood = 4, baseOxygen = 2, baseBattery = 5, baseParts = 2, baseAbsorbers = 1;
        public int signalNeeded = 3, dockPower = 2, solarPerDay = 1, baseLoad = 1;
        public int co2High = 2, co2Critical = 4, heatTrip = 3, heatCritical = 5;
        /// <summary>Spare things left in storage, found one per search, in this order.</summary>
        public List<string> storageFinds = new List<string> { "parts", "battery", "absorber", "food", "parts", "battery" };
        public int cooldownDays = 2;
        public float powerDipChance = 0.25f;
        /// <summary>Quest 1: the station radio needs a work session on this many different days (1 with the repair kit).</summary>
        public int radioRepairDays = 3;
        /// <summary>Chance that a trip outside walks through a radiation hot spot (none with a radiation meter).</summary>
        public float exploreRadiation = 0.3f;
        public int frozenMeals = 3;
        /// <summary>One solar storm per run, on a seeded day in this range; ORBI warns the day before.</summary>
        public int stormEarliest = 3, stormLatest = 6;
        public int roverPartsNeeded = 3, roverCharge = 2;
        public float roverBreakChance = 0.2f, raiderChance = 0.15f, mysterySpoilChance = 0.25f;
    }

    /// <summary>What an action would do (or did): the change in every number, and the messages.</summary>
    public class ActionResult
    {
        public bool ok;
        public string reason;
        public int food, oxygen, battery, parts, absorbers, co2, heat, signal, wu, health;
        public List<string> notes = new List<string>();
    }

    public class StationSimulation
    {
        public readonly StationConfig C;
        public int Day = 1, WorkUnits, Health;
        public int Food, Oxygen, Battery, Parts, Absorbers;
        public int Co2, Heat, Signal;
        public bool ScrubberOk = true, FanOk = true, O2GenOk = true, ThermalOk = true;
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
        /// <summary>Quest 1 progress: radio repair sessions done (one per day).</summary>
        public int RadioRepair;
        public bool RadioWorkedToday, ExploredToday;
        public bool HasSolar, HasRadioKit, HasRadMeter, HasFlashlight;
        public int Medkits;
        /// <summary>Phase 3 (Mars outside): storm, sites, rover, mystery food, raiders, dark nights.</summary>
        public int StormDay, MysteryFood, RoverParts, DarkNights;
        public bool ShelterReady, RoverBuilt, RoverSolar, RoverBroken, RoverToday, RaiderPending, RaidTonight, Sick;
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
        public int SolarToday => C.solarPerDay + (HasSolar ? 1 : 0);

        public StationSimulation(StationConfig c, Dictionary<Res, int> delivered, int seed)
        {
            C = c; _seed = seed;
            Food = c.baseFood; Oxygen = c.baseOxygen; Battery = c.baseBattery; Parts = c.baseParts; Absorbers = c.baseAbsorbers;
            if (delivered != null)
            {
                Food += Get(delivered, Res.Food); Oxygen += Get(delivered, Res.Oxygen);
                Battery += Get(delivered, Res.Battery); Parts += Get(delivered, Res.Parts);
                HasSolar = Get(delivered, Res.Solar) > 0; HasRadioKit = Get(delivered, Res.RadioKit) > 0;
                HasRadMeter = Get(delivered, Res.RadMeter) > 0; HasFlashlight = Get(delivered, Res.Flashlight) > 0;
                Food += Get(delivered, Res.FrozenFood) * c.frozenMeals;
                Medkits = Get(delivered, Res.Medkit);
            }
            Health = c.health; WorkUnits = c.workUnits;
            StormDay = new Random(seed * 977 + 5).Next(c.stormEarliest, c.stormLatest + 1);
            Director = new IncidentDirector(seed, c);
        }

        static int Get(Dictionary<Res, int> d, Res r) => d.TryGetValue(r, out var n) ? n : 0;

        // ------------------------------------------------------------------ reading (never moves time)

        public string Co2Level => Co2 >= C.co2Critical ? "critical" : Co2 >= C.co2High ? "high" : "normal";
        public string HeatLevel => Heat >= C.heatCritical ? "critical" : Heat >= C.heatTrip ? "high" : "normal";
        public bool TransmitterTripped => Heat >= C.heatTrip;
        public bool Usable(Room r) => !Sealed.Contains(r) && !Detached.Contains(r);
        public Fault ActiveFault => !ScrubberOk ? Fault.Scrubber : !ThermalOk ? Fault.Thermal : LeakRoom != null ? Fault.Leak : !FanOk ? Fault.Fan : !O2GenOk ? Fault.O2Gen : PowerDipPending ? Fault.PowerDip : Fault.None;
        public int FindsLeft => Detached.Contains(Room.Storage) ? 0 : C.storageFinds.Count - FindsUsed;

        public StationSimulation Clone()
        {
            var s = (StationSimulation)MemberwiseClone();
            s.Sealed = new HashSet<Room>(Sealed);
            s.Detached = new HashSet<Room>(Detached);
            s.Log = new List<string>(Log);
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

        // ------------------------------------------------------------------ acting

        /// <summary>Can this action be taken now? reason = a short key when not.</summary>
        public bool Can(string action, out string reason)
        {
            reason = null;
            if (Outcome != Outcome.Ongoing) { reason = "over"; return false; }
            bool dip = action.StartsWith("dip_"), raid = action.StartsWith("raid_");
            if (PowerDipPending != dip) { reason = dip ? "no_dip" : "dip_pending"; return false; }
            if (RaiderPending != raid) { reason = raid ? "no_raid" : "raid_pending"; return false; }
            if (!dip && !raid && action != "end_day" && WorkUnits <= 0) { reason = "no_time"; return false; }
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
                    if (RadioFixed) return Fail(out reason, "done");
                    if (!Usable(Room.Comms)) return Fail(out reason, "room_closed");
                    if (RadioWorkedToday) return Fail(out reason, "once_a_day");
                    if (RadioRepair == 0 && Parts < 1) return Fail(out reason, "no_parts");
                    return true;
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
                    if (f == "parts") Parts++; else if (f == "food") Food++; else if (f == "battery") Battery++; else if (f == "oxygen") Oxygen++; else if (f == "absorber") Absorbers++;
                    notes.Add("found_" + f);
                    break;
                case "repair_scrubber": Parts--; ScrubberOk = true; notes.Add("fixed_scrubber"); Log.Add("fixed_scrubber"); break;
                case "repair_fan": Parts--; FanOk = true; BackupCircuit = false; notes.Add("fixed_fan"); Log.Add("fixed_fan"); break;
                case "repair_o2gen": Parts--; O2GenOk = true; notes.Add("fixed_o2gen"); Log.Add("fixed_o2gen"); break;
                case "repair_thermal": Parts--; ThermalOk = true; notes.Add("fixed_thermal"); Log.Add("fixed_thermal"); break;
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
                    if (RadioRepair == 0) Parts--;
                    RadioRepair = HasRadioKit ? C.radioRepairDays : RadioRepair + 1;
                    RadioWorkedToday = true;
                    if (RadioFixed) { notes.Add("radio_fixed"); Log.Add("radio_fixed"); } else notes.Add("radio_progress");
                    break;
                case "explore_0": case "explore_1": case "explore_2":
                {
                    // a walk outside to one site; one site is a radiation hot spot each day (all of them in a storm)
                    int site = action[8] - '0';
                    ExploredToday = true;
                    var rng = new Random(_seed * 7919 + Day * 97 + _explores++);
                    if (IsHot(site)) { notes.Add("explore_radiation"); Hurt("radiation"); break; }
                    double roll = rng.NextDouble();
                    if (site == 0) { if (roll < 0.5) { Parts++; notes.Add("found_parts"); } else if (roll < 0.8) { Battery++; notes.Add("found_battery"); } else { Oxygen++; notes.Add("found_oxygen"); } }
                    else if (site == 1) { if (RoverParts < C.roverPartsNeeded && roll < 0.65) { RoverParts++; notes.Add("found_rover_part"); Log.Add("rover_part"); } else { Parts++; notes.Add("found_parts"); } }
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
                case "assemble_rover": Parts--; RoverBuilt = true; notes.Add("rover_built"); Log.Add("rover_built"); break;
                case "rover_solar": HasSolar = false; RoverSolar = true; notes.Add("rover_solar"); Log.Add("rover_solar"); break;
                case "send_rover":
                {
                    if (!RoverSolar) Battery -= C.roverCharge;
                    RoverToday = true;
                    var rng = new Random(_seed * 6151 + Day * 37);
                    Parts++; MysteryFood++;
                    if (rng.NextDouble() < 0.5) Battery++; else Oxygen++;
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
                    // resting still lets time pass: each spare Work Unit is a step, and a new incident stops the rest
                    step = false; int dayAtStart = Day;
                    while (WorkUnits > 0 && Outcome == Outcome.Ongoing)
                    {
                        var faultBefore = ActiveFault;
                        WorkUnits -= 1; _actionsToday++; Step();
                        if (Outcome != Outcome.Ongoing) break;
                        if (WorkUnits <= 0) { Night(); break; }
                        Director.MaybeTrigger(this, _actionsToday);
                        if (faultBefore == Fault.None && ActiveFault != Fault.None) { notes.Add("rest_interrupted"); break; }
                    }
                    if (WorkUnits <= 0 && Outcome == Outcome.Ongoing && Day == dayAtStart) Night();
                    break;
            }
            if (step) { WorkUnits -= 1; _actionsToday++; Step(); if (WorkUnits <= 0 && Outcome == Outcome.Ongoing) Night(); else if (Outcome == Outcome.Ongoing) Director.MaybeTrigger(this, _actionsToday); }
            var r = Diff(before); r.ok = true; r.notes.AddRange(notes);
            return r;
        }

        void FinishDipStep()
        {
            // the send already took its Work Unit; the power dip resolves inside the same step
            Step();
            if (WorkUnits <= 0 && Outcome == Outcome.Ongoing) Night();
        }

        /// <summary>One step of time: every fault that is not under control gets a little worse.</summary>
        void Step()
        {
            // CO₂: the scrubber removes it; air must also move through it (the fan)
            bool co2Rises = !ScrubberOk || !FanOk && !BackupCircuit;
            if (_stepShelter) co2Rises = false;
            Co2 = co2Rises ? Co2 + 1 : Math.Max(0, Co2 - 1);
            if (Co2 >= C.co2Critical) { Hurt("co2_critical"); }
            // heat: equipment makes heat; the loop carries it to the radiators
            bool heatRises = !ThermalOk && !LoadShedToday;
            Heat = heatRises ? Heat + 1 + (_stepSent ? 1 : 0) : Math.Max(0, Heat - 1 - (LoadShedToday ? 1 : 0));
            if (Heat >= C.heatTrip && !Log.Contains("transmitter_tripped@" + Day)) Log.Add("transmitter_tripped@" + Day);
            if (Heat >= C.heatCritical) { Battery = Math.Max(0, Battery - 1); Log.Add("heat_shutdown"); }
            // O₂: the generator splits the station's service water; without it, breathing draws on the tanks
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

        /// <summary>The night: food, power for life support, fatigue from CO₂; then the next day or the end.</summary>
        void Night()
        {
            if (Outcome != Outcome.Ongoing) return;
            if (Food > 0) Food -= 1; else Hurt("hungry");
            int net = SolarToday - C.baseLoad - (BackupCircuit ? 1 : 0);
            Battery += net;
            // solar storm night: the shelter needs power, or radiation gets through the roof
            if (StormToday) { if (ShelterReady) { Battery -= 1; Log.Add("shelter_on"); } else Hurt("storm_radiation"); }
            if (Battery < 0) { Battery = 0; Hurt("no_power"); }
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
            FatigueTomorrow = (Co2 >= C.co2High ? 1 : 0) + (Sick ? 1 : 0);
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

        // ------------------------------------------------------------------ diff helpers

        int[] Snap() => new[] { Food, Oxygen, Battery, Parts, Absorbers, Co2, Heat, Signal, WorkUnits, Health };

        ActionResult Diff(int[] b)
        {
            var a = Snap();
            return new ActionResult { food = a[0] - b[0], oxygen = a[1] - b[1], battery = a[2] - b[2], parts = a[3] - b[3], absorbers = a[4] - b[4], co2 = a[5] - b[5], heat = a[6] - b[6], signal = a[7] - b[7], wu = a[8] - b[8], health = a[9] - b[9] };
        }

        // test/director hooks
        internal void Break(Fault f, Room? room = null)
        {
            switch (f)
            {
                case Fault.Scrubber: ScrubberOk = false; break;
                case Fault.Thermal: ThermalOk = false; break;
                case Fault.Fan: FanOk = false; break;
                case Fault.O2Gen: O2GenOk = false; break;
                case Fault.Leak: LeakRoom = room ?? Room.Storage; break;
            }
            Log.Add("fault_" + f + (f == Fault.Leak ? "_" + LeakRoom : ""));
        }

        /// <summary>For tests and debugging: start a fault now (respects nothing).</summary>
        public void ForceFault(Fault f, Room? room = null) => Break(f, room);
    }
}
