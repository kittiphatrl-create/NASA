// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-11
using System;
using System.Collections.Generic;

namespace TitanGate.Core
{
    public enum Outcome { Ongoing, Rescued, Stranded, FailedAir, FailedSupplies }

    public enum LogKind { Night, Spe, Incident, Choice, Message, PowerOut, Collapse, WakeAlarm, Rescue }

    /// <summary>One thing that happened, for the HUD log, the morning report and the debrief.</summary>
    public class LogEntry
    {
        public int hour;
        public LogKind kind;
        public string id;
        public bool ok;
        public string option;
        public float value;
    }

    /// <summary>A message to or from Earth, carried by the relay orbiter passes.</summary>
    public class Message
    {
        public string id;
        public int queuedHour;
        public int passes;
        public bool delivered;
    }

    /// <summary>Everything the outpost tracks. Plain data so tests can inspect it.</summary>
    public class OutpostState
    {
        public float air, power, shelter, supplies, energy;
        /// <summary>Hours since landing. The single authoritative survival clock (0..168).</summary>
        public int hour;
        public float efficiency = 1f;
        public float dose;
        public int collapses, zeroSuppliesHours, greenhouseHours, actionsToday;
        /// <summary>0 = not sent, 1 = sent and waiting for a rescue-ship pass, 2 = acknowledged.</summary>
        public int coords;
        public bool speDone;
        public string encounterToday;
        public Outcome outcome = Outcome.Ongoing;
        public readonly HashSet<string> flags = new HashSet<string>();
        public readonly HashSet<string> solFlags = new HashSet<string>();
        public readonly Dictionary<string, bool> systems = new Dictionary<string, bool>();
        public readonly HashSet<string> incidentsDone = new HashSet<string>();
        public readonly HashSet<string> encountersUsed = new HashSet<string>();
        public readonly Dictionary<string, string> choices = new Dictionary<string, string>();
        public readonly List<LogEntry> log = new List<LogEntry>();
        public readonly List<Message> messages = new List<Message>();

        public bool Has(string flag) => flags.Contains(flag) || solFlags.Contains(flag);
    }

    /// <summary>What a stretch of time did, so the UI can report it.</summary>
    public class AdvanceResult
    {
        public readonly List<LogEntry> events = new List<LogEntry>();
        public int hours;
        /// <summary>Stopped before the requested hours (a low-air alarm woke the astronaut, or a collapse).</summary>
        public bool stoppedEarly;
        public float air0, power0, supplies0, energy0, dose0;
    }

    /// <summary>
    /// The 7-day Mars outpost on the landed Kestrel: hour-by-hour life support, a power budget split
    /// between switchable systems, shelter readiness for the solar particle event, relay-orbiter
    /// messages with Earth, and the rescue check at hour 168. No Unity types: every rule is testable.
    /// </summary>
    public class OutpostSim
    {
        readonly OutpostConfig _c;
        readonly Dictionary<int, string> _nights = new Dictionary<int, string>();
        readonly List<string> _deck = new List<string>();
        IncidentDef _pendingIncident;

        public OutpostState State { get; } = new OutpostState();
        public OutpostConfig Config => _c;

        public OutpostSim(OutpostConfig c, int seed, ResourceDelta bonus = null, IEnumerable<string> flags = null)
        {
            _c = c;
            var s = State;
            s.air = c.start.air; s.power = c.start.power; s.shelter = c.start.shelter; s.supplies = c.start.supplies; s.energy = c.start.energy;
            if (bonus != null) Apply(bonus);
            if (flags != null) foreach (var f in flags) s.flags.Add(f);
            foreach (var sys in c.systems) s.systems[sys.id] = sys.onAtStart;
            BuildNights(seed);
            foreach (var d in c.dilemmas) if (d.random) _deck.Add(d.id);
            var er = new Random(seed * 31 + 7);
            for (int i = _deck.Count - 1; i > 0; i--) { int j = er.Next(i + 1); (_deck[i], _deck[j]) = (_deck[j], _deck[i]); }
            s.messages.Add(new Message { id = "earth_spe_warning", queuedHour = -1 });
        }

        void BuildNights(int seed)
        {
            var deck = new List<string>();
            foreach (var e in _c.nightEvents) deck.Add(e.id);
            var rng = new Random(seed);
            int d = deck.Count;
            string last = null;
            foreach (var day in _c.randomEventDays)
            {
                if (deck.Count == 0) break;
                if (d >= deck.Count)
                {
                    for (int i = deck.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (deck[i], deck[j]) = (deck[j], deck[i]); }
                    d = 0;
                    if (deck.Count > 1 && deck[0] == last) (deck[0], deck[deck.Count - 1]) = (deck[deck.Count - 1], deck[0]);
                }
                _nights[day] = last = deck[d++];
            }
        }

        // ------------------------------------------------------------------ clock

        public int Clock => ClockAt(State.hour);
        public int Day => DayAt(State.hour);
        public int ClockAt(int h) => (_c.landingClock + h) % 24;
        public int DayAt(int h) => 1 + h / 24;
        public int HoursLeft => _c.hoursTotal - State.hour;
        public bool IsDaylight => Clock >= _c.daylightFrom && Clock < _c.daylightTo;
        public bool IsNight => Clock >= 22 || Clock < 6;
        /// <summary>Hours until the next 06:00 (a full night's sleep ends there).</summary>
        public int HoursToMorning => (24 + 6 - Clock) % 24 == 0 ? 24 : (24 + 6 - Clock) % 24;

        /// <summary>The night event scheduled for the night that ends day d, or null.</summary>
        public NightEventDef NightEventFor(int day) => _nights.TryGetValue(day, out var id) ? _c.GetNightEvent(id) : null;
        public NightEventDef TonightEvent() => NightEventFor(Day);

        public bool IsPrevented(NightEventDef e) =>
            (string.IsNullOrEmpty(e.preventFlag) || State.Has(e.preventFlag)) && State.shelter >= e.preventMinShelter;

        public int HoursToSpe => State.speDone ? -1 : _c.spe.hour - State.hour;

        /// <summary>How much the shelter would protect right now (0..1).</summary>
        public float Protection => State.shelter / 100f * (IsOn("shelter") ? 1f : _c.spe.unpoweredShelterFactor);

        // ------------------------------------------------------------------ systems

        public bool IsOn(string id) => State.systems.TryGetValue(id, out var on) && on;

        public void SetSystem(string id, bool on)
        {
            if (State.systems.ContainsKey(id)) State.systems[id] = on;
        }

        /// <summary>Power the systems that are on right now will draw per hour.</summary>
        public float PowerDrawPerHour
        {
            get
            {
                float p = 0;
                foreach (var s in _c.systems) if (IsOn(s.id)) p += s.powerPerHour;
                return p;
            }
        }

        /// <summary>Power the switched-on systems need from now until sunrise (07:00).</summary>
        public float PowerNeededUntilSunrise
        {
            get
            {
                // in daylight: the whole coming night; at night: the hours left until the sun is back
                int h = IsDaylight ? 24 - (_c.daylightTo - _c.daylightFrom) : (24 + _c.daylightFrom - Clock) % 24;
                return PowerDrawPerHour * h;
            }
        }

        // ------------------------------------------------------------------ time

        /// <summary>Let time pass hour by hour. Sleeping restores energy; a low-air alarm wakes the astronaut.</summary>
        public AdvanceResult Advance(int hours, bool sleeping = false)
        {
            var r = new AdvanceResult { air0 = State.air, power0 = State.power, supplies0 = State.supplies, energy0 = State.energy, dose0 = State.dose };
            float startAir = State.air;
            for (int i = 0; i < hours; i++)
            {
                if (State.outcome != Outcome.Ongoing) break;
                if (sleeping && State.air <= _c.warnAir && startAir > _c.warnAir)
                {
                    r.stoppedEarly = true;
                    Log(r, LogKind.WakeAlarm, "low_air", false);
                    break;
                }
                Tick(sleeping, r);
                r.hours++;
                if (!sleeping && State.energy <= 0 && State.outcome == Outcome.Ongoing)
                {
                    State.collapses++;
                    Log(r, LogKind.Collapse, "collapse", false);
                    var c = Advance(_c.collapseSleepHours, true);
                    r.events.AddRange(c.events);
                    r.hours += c.hours;
                    r.stoppedEarly = true;
                    break;
                }
            }
            return r;
        }

        void Tick(bool sleeping, AdvanceResult r)
        {
            var s = State;
            int clock = Clock;
            if (clock >= _c.daylightFrom && clock < _c.daylightTo) s.power += _c.solarPerHour * s.efficiency;
            foreach (var sys in _c.systems)
            {
                if (!IsOn(sys.id)) continue;
                if (s.power >= sys.powerPerHour) s.power -= sys.powerPerHour;
                else { s.systems[sys.id] = false; Log(r, LogKind.PowerOut, sys.id, false); }
            }
            s.air -= _c.airUsePerHour;
            if (IsOn("life_support")) s.air += _c.lifeSupportAirPerHour;
            s.supplies -= _c.suppliesUsePerHour;
            s.energy += sleeping ? _c.energySleepPerHour : _c.energyAwakePerHour - (s.supplies <= 0 ? 1f : 0f);
            if (s.flags.Contains("greenhouse_started") && IsOn("greenhouse"))
            {
                s.greenhouseHours++;
                if (s.greenhouseHours >= _c.greenhouseHoursToReady) s.flags.Add("greenhouse_ready");
            }
            s.dose += _c.radPerHour;
            ClampAll();
            s.zeroSuppliesHours = s.supplies <= 0 ? s.zeroSuppliesHours + 1 : 0;
            s.hour++;
            AfterHour(r);
        }

        void AfterHour(AdvanceResult r)
        {
            var s = State;
            if (s.air <= 0 && s.outcome == Outcome.Ongoing) s.outcome = Outcome.FailedAir;
            if (s.zeroSuppliesHours >= _c.suppliesZeroFailHours && s.outcome == Outcome.Ongoing) s.outcome = Outcome.FailedSupplies;
            if (s.outcome != Outcome.Ongoing) return;

            if (Clock == _c.nightEventClock)
            {
                var e = NightEventFor(Day);   // 02:00 still belongs to this day: days change at 06:00
                if (e != null)
                {
                    bool ok = IsPrevented(e);
                    var entry = Log(r, LogKind.Night, e.id, ok);
                    if (!ok)
                    {
                        Apply(e.penalty);
                        s.efficiency -= e.efficiencyLoss;
                        s.dose += e.dose;
                        entry.value = e.dose;
                        ClampAll();
                    }
                }
            }

            var spe = _c.spe;
            if (s.hour == spe.hour && !s.speDone)
            {
                s.speDone = true;
                float p = Protection;
                float k = 1f - p;
                s.dose += spe.dose * k;
                s.energy -= spe.energyLoss * k;
                s.air -= spe.airLoss * k;
                s.power -= spe.powerLoss * k;
                ClampAll();
                var entry = Log(r, LogKind.Spe, "spe", p >= 0.7f);
                entry.value = spe.dose * k;
                if (s.air <= 0) s.outcome = Outcome.FailedAir;
            }

            if (s.hour == spe.warningHour) DeliverMessage("earth_spe_warning", r);
            if (Array.IndexOf(_c.relayClocks, Clock) >= 0) RelayPass(r);

            if (s.hour % _c.rescuePassEveryHours == 0 && s.coords == 1 && IsOn("beacon"))
            {
                s.coords = 2;
                Log(r, LogKind.Rescue, "coords_ack", true);
            }

            if (s.hour % 24 == 0)
            {
                s.solFlags.Clear();
                s.actionsToday = 0;
                s.efficiency = Math.Max(_c.minEfficiency, s.efficiency - _c.dustPerDay);
                DrawEncounter();
            }

            if (s.hour >= _c.hoursTotal && s.outcome == Outcome.Ongoing)
                s.outcome = s.coords == 2 ? Outcome.Rescued : Outcome.Stranded;
        }

        // ------------------------------------------------------------------ messages (relay orbiter + light delay)

        void RelayPass(AdvanceResult r)
        {
            foreach (var m in State.messages)
            {
                if (m.delivered || m.queuedHour < 0) continue;
                m.passes++;
                if (m.passes >= 2) DeliverMessage(m, r);
            }
        }

        void DeliverMessage(string id, AdvanceResult r)
        {
            var m = State.messages.Find(x => x.id == id && !x.delivered);
            if (m != null) DeliverMessage(m, r);
        }

        void DeliverMessage(Message m, AdvanceResult r)
        {
            m.delivered = true;
            Log(r, LogKind.Message, m.id, true);
        }

        /// <summary>Hours until a message sent now gets an answer (next pass out, the pass after back).</summary>
        public int HoursUntilReply()
        {
            int found = 0;
            for (int h = 1; h <= 48; h++)
                if (Array.IndexOf(_c.relayClocks, ClockAt(State.hour + h)) >= 0 && ++found == 2) return h;
            return 24;
        }

        /// <summary>Hours until the rescue ship next passes overhead and can hear the beacon.</summary>
        public int HoursUntilNextRescuePass()
        {
            int every = Math.Max(1, _c.rescuePassEveryHours);
            int r = every - State.hour % every;
            return r == 0 ? every : r;
        }

        // ------------------------------------------------------------------ actions

        public int HoursFor(OutpostAction a) => a.hours + (State.energy < _c.lowEnergy ? _c.lowEnergyExtraHours : 0);

        /// <summary>Can the action be done now? reason is a localisation key when false.</summary>
        public bool CanDo(string id, out string reason)
        {
            reason = null;
            var a = _c.GetAction(id);
            var s = State;
            if (a == null) { reason = "reason.unknown"; return false; }
            if (s.outcome != Outcome.Ongoing) { reason = "reason.over"; return false; }
            if (a.minDay > Day) { reason = "reason.later"; return false; }
            if (!string.IsNullOrEmpty(a.requiresFlag) && !s.flags.Contains(a.requiresFlag)) { reason = "reason.need." + a.requiresFlag; return false; }
            if (!string.IsNullOrEmpty(a.setsFlag) && s.Has(a.setsFlag)) { reason = a.science ? "reason.done.science" : "reason.done." + a.setsFlag; return false; }
            if (!string.IsNullOrEmpty(a.needsSystem) && !IsOn(a.needsSystem)) { reason = "reason.system_off." + a.needsSystem; return false; }
            if (a.delta.power < 0 && s.power < -a.delta.power) { reason = "reason.no_power"; return false; }
            if (a.delta.supplies < 0 && s.supplies < -a.delta.supplies) { reason = "reason.no_supplies"; return false; }
            if (a.delta.energy <= 0 && s.energy <= 0) { reason = "reason.no_energy"; return false; }
            if (a.delta.shelter > 0 && s.shelter >= _c.maxValue) { reason = "reason.shelter_full"; return false; }
            if (a.delta.energy > 0 && s.energy >= _c.maxValue) { reason = "reason.not_hungry"; return false; }
            if (id == "send_coords" && s.coords != 0) { reason = "reason.coords_sent"; return false; }
            if (id == "ask_earth" && s.messages.Exists(m => m.id == "earth_hint" && !m.delivered)) { reason = "reason.waiting_earth"; return false; }
            if (s.hour + HoursFor(a) > _c.hoursTotal) { reason = "reason.no_time"; return false; }
            return true;
        }

        /// <summary>Do an action: its costs apply now, then its hours pass. Returns null when CanDo is false.</summary>
        public AdvanceResult Do(string id)
        {
            if (!CanDo(id, out _)) return null;
            var a = _c.GetAction(id);
            var s = State;
            int h = HoursFor(a);
            Apply(a.delta);
            if (a.cleansPanels) s.efficiency = 1f;
            if (!string.IsNullOrEmpty(a.setsFlag)) { if (a.solFlag) s.solFlags.Add(a.setsFlag); else s.flags.Add(a.setsFlag); }
            if (a.consumesFlag) s.flags.Remove(a.requiresFlag);
            if (a.outside) s.dose += _c.radPerOutsideHour * h;
            if (id == "send_coords") s.coords = 1;
            if (id == "ask_earth") { s.messages.RemoveAll(m => m.id == "earth_hint"); s.messages.Add(new Message { id = "earth_hint", queuedHour = s.hour }); }
            s.actionsToday++;
            if (_pendingIncident == null && _c.incidents != null)
                foreach (var inc in _c.incidents)
                    if (inc.day == Day && s.actionsToday >= inc.afterActions && !s.incidentsDone.Contains(inc.id)) { _pendingIncident = inc; break; }
            return Advance(h);
        }

        /// <summary>A daytime emergency triggered by the last action, or null.</summary>
        public IncidentDef DueIncident() => State.outcome == Outcome.Ongoing ? _pendingIncident : null;

        /// <summary>Close an incident. Missed ones apply their penalty.</summary>
        public void ResolveIncident(IncidentDef i, bool handled)
        {
            if (i == null || !State.incidentsDone.Add(i.id)) return;
            if (_pendingIncident == i) _pendingIncident = null;
            State.log.Add(new LogEntry { hour = State.hour, kind = LogKind.Incident, id = i.id, ok = handled });
            if (!handled) { Apply(i.penalty); if (State.air <= 0) State.outcome = Outcome.FailedAir; }
        }

        /// <summary>Set a story flag (from a solved puzzle).</summary>
        public void SetFlag(string flag) => State.flags.Add(flag);

        // ------------------------------------------------------------------ dilemmas and random events

        void DrawEncounter()
        {
            State.encounterToday = null;
            if (Array.IndexOf(_c.encounterDays, Day) < 0) return;
            if (_c.dilemmas.Exists(d => !d.random && d.day == Day)) return;
            foreach (var id in _deck)
            {
                var d = _c.dilemmas.Find(x => x.id == id);
                if (State.encountersUsed.Contains(id)) continue;
                if (!string.IsNullOrEmpty(d.requiresFlag) && !State.flags.Contains(d.requiresFlag)) continue;
                State.encounterToday = id;
                State.encountersUsed.Add(id);
                return;
            }
        }

        public IReadOnlyList<string> EncounterDeck => _deck;

        /// <summary>This morning's dilemma or random event that has not been answered yet, or null.</summary>
        public DilemmaDef DueDilemma()
        {
            if (State.outcome != Outcome.Ongoing) return null;
            foreach (var d in _c.dilemmas)
                if (!d.random && d.day == Day && !State.choices.ContainsKey(d.id)) return d;
            if (State.encounterToday != null && !State.choices.ContainsKey(State.encounterToday))
                return _c.dilemmas.Find(x => x.id == State.encounterToday);
            return null;
        }

        public bool CanChoose(DilemmaOption o, out string reason)
        {
            reason = null;
            if (!string.IsNullOrEmpty(o.needFlag) && !State.flags.Contains(o.needFlag)) { reason = "dilemma.need." + o.needFlag; return false; }
            if (o.delta.supplies < 0 && State.supplies < -o.delta.supplies) { reason = "reason.no_supplies"; return false; }
            if (o.delta.power < 0 && State.power < -o.delta.power) { reason = "reason.no_power"; return false; }
            return true;
        }

        public AdvanceResult Choose(string dilemmaId, string optionId)
        {
            var d = _c.dilemmas.Find(x => x.id == dilemmaId);
            var o = d?.options.Find(x => x.id == optionId);
            if (o == null || State.outcome != Outcome.Ongoing || State.choices.ContainsKey(d.id) || !CanChoose(o, out _)) return null;
            State.choices[d.id] = o.id;
            State.log.Add(new LogEntry { hour = State.hour, kind = LogKind.Choice, id = d.id, option = o.id, ok = true });
            Apply(o.delta);
            State.dose += o.dose;
            if (!string.IsNullOrEmpty(o.setsFlag)) { if (o.solFlag) State.solFlags.Add(o.setsFlag); else State.flags.Add(o.setsFlag); }
            if (State.air <= 0) State.outcome = Outcome.FailedAir;
            return o.hours > 0 ? Advance(o.hours) : new AdvanceResult();
        }

        // ------------------------------------------------------------------ results

        public int ScienceTotal => _c.actions.FindAll(a => a.science).Count;
        public int ScienceDone => _c.actions.FindAll(a => a.science && State.flags.Contains(a.setsFlag)).Count;

        /// <summary>Debrief stars: rescued; healthy (low dose, never collapsed); all science + greenhouse ready.</summary>
        public bool[] Stars()
        {
            bool won = State.outcome == Outcome.Rescued;
            return new[]
            {
                won,
                won && State.dose <= _c.radSafe && State.collapses == 0,
                won && ScienceDone >= ScienceTotal && State.flags.Contains("greenhouse_ready"),
            };
        }

        // ------------------------------------------------------------------ helpers

        LogEntry Log(AdvanceResult r, LogKind kind, string id, bool ok)
        {
            var e = new LogEntry { hour = State.hour, kind = kind, id = id, ok = ok };
            State.log.Add(e);
            r?.events.Add(e);
            return e;
        }

        void Apply(ResourceDelta d)
        {
            var s = State;
            s.air += d.air; s.power += d.power; s.shelter += d.shelter; s.supplies += d.supplies; s.energy += d.energy;
            ClampAll();
        }

        void ClampAll()
        {
            var s = State;
            float m = _c.maxValue;
            s.air = Clamp(s.air, m); s.power = Clamp(s.power, m); s.shelter = Clamp(s.shelter, m);
            s.supplies = Clamp(s.supplies, m); s.energy = Clamp(s.energy, m);
            s.efficiency = Math.Max(_c.minEfficiency, Math.Min(1f, s.efficiency));
        }

        static float Clamp(float v, float m) => v < 0 ? 0 : v > m ? m : v;

        /// <summary>Start bonus from Chapter 1: cabin oxygen left (as air) plus every supply stowed.</summary>
        public static ResourceDelta CarryOver(Chapter1Config c, float cabinO2, IEnumerable<string> stowed)
        {
            var d = new ResourceDelta { air = (float)Math.Round((cabinO2 - c.carryO2Pivot) * c.carryO2Factor) };
            if (stowed == null || c.supplies == null) return d;
            foreach (var id in stowed)
            {
                var s = c.supplies.Find(x => x.id == id);
                if (s == null) continue;
                d.air += s.delta.air; d.power += s.delta.power; d.shelter += s.delta.shelter;
                d.supplies += s.delta.supplies; d.energy += s.delta.energy;
            }
            return d;
        }

        /// <summary>Flags the stowed supplies give at the outpost (med_kit, spare_filter, ...).</summary>
        public static List<string> SupplyFlags(Chapter1Config c, IEnumerable<string> stowed)
        {
            var flags = new List<string>();
            if (stowed == null || c.supplies == null) return flags;
            foreach (var id in stowed)
            {
                var s = c.supplies.Find(x => x.id == id);
                if (s != null && !string.IsNullOrEmpty(s.flag)) flags.Add(s.flag);
            }
            return flags;
        }
    }
}
