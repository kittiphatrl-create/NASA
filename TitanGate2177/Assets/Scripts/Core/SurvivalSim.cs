// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System;
using System.Collections.Generic;

namespace TitanGate.Core
{
    public enum SimOutcome { Ongoing, Won, LostO2, LostWater, LostFood, LostWindow, LostRadiation }

    /// <summary>Everything the Mars chapter tracks. Plain data so tests can inspect it.</summary>
    public class SurvivalState
    {
        public int o2, water, power, food, stamina, fuel;
        public int sol;
        public int ap;
        public float efficiency = 1f;
        public float morningSolarFactor = 1f;
        public int lastSolarProduced;
        public readonly HashSet<string> flags = new HashSet<string>();
        public readonly HashSet<string> solFlags = new HashSet<string>();
        public SimOutcome outcome = SimOutcome.Ongoing;
        public readonly HashSet<string> incidentsDone = new HashSet<string>();
        /// <summary>Radiation dose so far, in mSv.</summary>
        public float dose;
        public int collapses;
        /// <summary>Every night event and daytime emergency, in order, for the debrief.</summary>
        public readonly List<LogEntry> log = new List<LogEntry>();
        /// <summary>Dilemma id to the option picked.</summary>
        public readonly Dictionary<string, string> choices = new Dictionary<string, string>();
        /// <summary>Random encounter drawn this morning (null on a quiet day).</summary>
        public string encounterToday;
        public readonly HashSet<string> encountersUsed = new HashSet<string>();

        public bool Has(string flag) => flags.Contains(flag) || solFlags.Contains(flag);
    }

    /// <summary>One night event or daytime emergency and whether the astronaut handled it.</summary>
    public class LogEntry
    {
        public int sol;
        public bool incident;
        /// <summary>A morning dilemma; option holds the answer picked.</summary>
        public bool choice;
        public string option;
        public string id;
        public bool ok;
    }

    /// <summary>What happened overnight, for the morning report.</summary>
    public class NightReport
    {
        public int sol;
        public int habLoad, o2Used, waterUsed, foodUsed;
        public string eventId;
        public bool eventPrevented;
        public ResourceDelta eventPenalty = new ResourceDelta();
        public bool powerOut;
        public bool collapsed;
        public int morningSolar;
        /// <summary>Radiation taken tonight (background + event), in mSv.</summary>
        public float dose;
        public SimOutcome outcome;
    }

    /// <summary>
    /// The Mars survival rules: action-based time, linked life support, forecast events.
    /// No Unity types, so every rule is unit-testable.
    /// </summary>
    public class SurvivalSim
    {
        public const string FlagPanelsStowed = "panels_stowed";

        readonly GameConfig _cfg;
        readonly SurvivalConfig _s;
        readonly string[] _schedule; // index = night - 1
        readonly List<string> _encounterDeck = new List<string>();

        public SurvivalState State { get; } = new SurvivalState();
        public GameConfig Config => _cfg;

        /// <summary>Build the sim. The seed only decides which minor events come on which night.</summary>
        public SurvivalSim(GameConfig cfg, int seed) : this(cfg, seed, null) { }

        /// <summary>startBonus carries Chapter 1 results (cabin oxygen left, supplies grabbed).</summary>
        public SurvivalSim(GameConfig cfg, int seed, ResourceDelta startBonus)
        {
            _cfg = cfg;
            _s = cfg.survival;
            _schedule = BuildSchedule(cfg, seed);
            if (cfg.dilemmas != null)
            {
                foreach (var d in cfg.dilemmas) if (d.random) _encounterDeck.Add(d.id);
                var er = new Random(seed * 31 + 7);
                for (int i = _encounterDeck.Count - 1; i > 0; i--)
                {
                    int j = er.Next(i + 1);
                    (_encounterDeck[i], _encounterDeck[j]) = (_encounterDeck[j], _encounterDeck[i]);
                }
            }
            State.o2 = _s.startO2;
            State.water = _s.startWater;
            State.power = _s.startPower;
            State.food = _s.startFood;
            State.stamina = _s.startStamina;
            State.sol = 0;
            if (startBonus != null) { int f = startBonus.fuel; Apply(startBonus); State.fuel = Math.Max(0, Math.Min(_s.fuelTarget, f)); }
        }

        /// <summary>Start bonus from Chapter 1: oxygen left in the cabin and the supplies bag.</summary>
        public static ResourceDelta CarryOver(Chapter1Config c, float cabinO2, bool supplies)
        {
            var d = new ResourceDelta { o2 = (int)Math.Round((cabinO2 - c.carryO2Pivot) * c.carryO2Factor) };
            if (supplies && c.suppliesBonus != null)
            {
                d.o2 += c.suppliesBonus.o2; d.water += c.suppliesBonus.water; d.power += c.suppliesBonus.power;
                d.food += c.suppliesBonus.food; d.stamina += c.suppliesBonus.stamina;
            }
            return d;
        }

        /// <summary>Start bonus from Chapter 1: cabin oxygen left plus every supply stowed in the scramble.</summary>
        public static ResourceDelta CarryOver(Chapter1Config c, float cabinO2, IEnumerable<string> stowed)
        {
            var d = CarryOver(c, cabinO2, false);
            if (stowed == null || c.supplies == null) return d;
            foreach (var id in stowed)
            {
                var s = c.supplies.Find(x => x.id == id);
                if (s == null) continue;
                d.o2 += s.delta.o2; d.water += s.delta.water; d.power += s.delta.power;
                d.food += s.delta.food; d.stamina += s.delta.stamina; d.fuel += s.delta.fuel;
            }
            return d;
        }

        /// <summary>Flags the stowed supplies give in Chapter 2 (med_kit, spare_filter, ...).</summary>
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

        /// <summary>Actions spent today (counts the extra cost when tired).</summary>
        public int ActionsUsedToday => _s.actionsPerSol - State.ap;

        /// <summary>A daytime emergency that is due now and has not fired yet, or null.</summary>
        public IncidentDef DueIncident()
        {
            if (State.outcome != SimOutcome.Ongoing || _cfg.incidents == null) return null;
            foreach (var i in _cfg.incidents)
                if (i.sol == State.sol && ActionsUsedToday >= i.afterActions && !State.incidentsDone.Contains(i.id)) return i;
            return null;
        }

        /// <summary>Close an incident. Unresolved ones apply their penalty.</summary>
        public void ResolveIncident(IncidentDef i, bool handled)
        {
            if (i == null || !State.incidentsDone.Add(i.id)) return;
            State.log.Add(new LogEntry { sol = State.sol, incident = true, id = i.id, ok = handled });
            if (!handled) ApplyPenalty(i.penalty);
        }

        /// <summary>The order random encounters will be offered in (for tests and tools).</summary>
        public IReadOnlyList<string> EncounterDeck => _encounterDeck;

        void DrawEncounter()
        {
            State.encounterToday = null;
            if (_cfg.dilemmas == null || Array.IndexOf(_s.encounterSols ?? new int[0], State.sol) < 0) return;
            if (_cfg.dilemmas.Exists(d => !d.random && d.sol == State.sol)) return;
            foreach (var id in _encounterDeck)
            {
                var d = _cfg.dilemmas.Find(x => x.id == id);
                if (State.encountersUsed.Contains(id)) continue;
                if (!string.IsNullOrEmpty(d.requiresFlag) && !State.flags.Contains(d.requiresFlag)) continue;
                State.encounterToday = id;
                State.encountersUsed.Add(id);
                return;
            }
        }

        /// <summary>This morning's dilemma or encounter that has not been answered yet, or null.</summary>
        public DilemmaDef DueDilemma()
        {
            if (State.outcome != SimOutcome.Ongoing || _cfg.dilemmas == null) return null;
            foreach (var d in _cfg.dilemmas)
                if (!d.random && d.sol == State.sol && !State.choices.ContainsKey(d.id)) return d;
            if (State.encounterToday != null && !State.choices.ContainsKey(State.encounterToday))
                return _cfg.dilemmas.Find(x => x.id == State.encounterToday);
            return null;
        }

        /// <summary>Can this answer be picked now? reason is a localisation key when false.</summary>
        public bool CanChoose(DilemmaOption o, out string reason)
        {
            reason = null;
            if (!string.IsNullOrEmpty(o.needFlag) && !State.flags.Contains(o.needFlag)) { reason = "dilemma.need." + o.needFlag; return false; }
            if (State.water < o.needWater) { reason = "reason.no_water"; return false; }
            if (State.power < o.needPower) { reason = "reason.no_power"; return false; }
            if (State.food < o.needFood) { reason = "reason.no_food"; return false; }
            if (State.stamina < o.needStamina) { reason = "reason.no_stamina"; return false; }
            if (o.ap > State.ap) { reason = "reason.no_ap"; return false; }
            return true;
        }

        /// <summary>Answer a dilemma. Time it costs is taken from today (never below zero).</summary>
        public bool Choose(string dilemmaId, string optionId)
        {
            var d = _cfg.dilemmas?.Find(x => x.id == dilemmaId);
            var o = d?.options.Find(x => x.id == optionId);
            if (o == null || State.outcome != SimOutcome.Ongoing || State.choices.ContainsKey(d.id) || !CanChoose(o, out _)) return false;
            State.choices[d.id] = o.id;
            State.log.Add(new LogEntry { sol = State.sol, choice = true, id = d.id, option = o.id, ok = true });
            State.ap = Math.Max(0, State.ap - o.ap);
            State.dose += o.dose;
            if (!string.IsNullOrEmpty(o.setsFlag)) { if (o.solFlag) State.solFlags.Add(o.setsFlag); else State.flags.Add(o.setsFlag); }
            ApplyPenalty(o.delta);
            return true;
        }

        /// <summary>Apply a loss (clamped) and check whether it ended the run.</summary>
        public void ApplyPenalty(ResourceDelta d)
        {
            Apply(d);
            CheckVitals();
        }

        /// <summary>Night-by-night event ids (null = calm night). Majors are fixed, minors shuffled.</summary>
        public static string[] BuildSchedule(GameConfig cfg, int seed)
        {
            var nights = new string[Math.Max(0, cfg.survival.solsTotal - 1)];
            foreach (var e in cfg.events)
                if (e.major && e.night >= 1 && e.night <= nights.Length) nights[e.night - 1] = e.id;

            var deck = new List<string>();
            foreach (var e in cfg.events) if (!e.major) deck.Add(e.id);
            var rng = new Random(seed);
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }
            // Longer stays draw the deck again (reshuffled), never the same event two nights running.
            int d = 0;
            string last = null;
            foreach (var n in cfg.survival.randomEventNights)
            {
                if (n < 1 || n > nights.Length || nights[n - 1] != null || deck.Count == 0) continue;
                if (d >= deck.Count)
                {
                    d = 0;
                    for (int i = deck.Count - 1; i > 0; i--)
                    {
                        int j = rng.Next(i + 1);
                        (deck[i], deck[j]) = (deck[j], deck[i]);
                    }
                    if (deck.Count > 1 && deck[0] == last) (deck[0], deck[deck.Count - 1]) = (deck[deck.Count - 1], deck[0]);
                }
                nights[n - 1] = last = deck[d++];
            }
            return nights;
        }

        public string[] Schedule => (string[])_schedule.Clone();

        /// <summary>Event that will hit tonight, or null.</summary>
        public EventDef TonightEvent()
        {
            int n = State.sol;
            if (n < 1 || n > _schedule.Length || _schedule[n - 1] == null) return null;
            return _cfg.GetEvent(_schedule[n - 1]);
        }

        /// <summary>The fixed major event, for ORBI's long-range forecast.</summary>
        public EventDef MajorEvent() => _cfg.events.Find(e => e.major);

        /// <summary>Begin the first sol (call once after landing).</summary>
        public int BeginFirstSol()
        {
            State.sol = 1;
            return StartMorning();
        }

        int StartMorning()
        {
            State.ap = _s.actionsPerSol;
            State.solFlags.Clear();
            DrawEncounter();
            int produced = (int)Math.Round(_s.solarBase * State.efficiency * State.morningSolarFactor);
            State.morningSolarFactor = 1f;
            State.power = Clamp(State.power + produced);
            State.lastSolarProduced = produced;
            return produced;
        }

        public int ApCost(ActionDef a)
        {
            int cost = a.ap;
            if (cost > 0 && State.stamina < _s.lowStaminaThreshold) cost += _s.lowStaminaExtraAp;
            return cost;
        }

        /// <summary>Can the action be done now? reason is a localisation key when false.</summary>
        public bool CanDo(string actionId, out string reason)
        {
            reason = null;
            var a = _cfg.GetAction(actionId);
            if (a == null) { reason = "reason.unknown"; return false; }
            if (State.outcome != SimOutcome.Ongoing) { reason = "reason.over"; return false; }
            if (a.minSol > State.sol) { reason = "reason.later"; return false; }
            if (!string.IsNullOrEmpty(a.requiresFlag) && !State.flags.Contains(a.requiresFlag))
            { reason = "reason.need." + a.requiresFlag; return false; }
            if (!string.IsNullOrEmpty(a.setsFlag) && State.Has(a.setsFlag))
            { reason = a.science ? "reason.done.science" : "reason.done." + a.setsFlag; return false; }
            if (ApCost(a) > State.ap) { reason = "reason.no_ap"; return false; }
            bool restores = a.delta.stamina > 0;
            if (!restores && State.stamina <= 0) { reason = "reason.no_stamina"; return false; }
            if (a.delta.food < 0 && State.food < -a.delta.food) { reason = "reason.no_food"; return false; }
            if (a.delta.power < 0 && State.power < -a.delta.power) { reason = "reason.no_power"; return false; }
            if (a.delta.water < 0 && State.water < -a.delta.water) { reason = "reason.no_water"; return false; }
            if (a.delta.fuel > 0 && State.fuel >= _s.fuelTarget) { reason = "reason.fuel_full"; return false; }
            if (a.delta.stamina > 0 && State.stamina >= _s.maxValue) { reason = "reason.not_hungry"; return false; }
            return true;
        }

        /// <summary>Do an action. Returns false (and changes nothing) when CanDo is false.</summary>
        public bool Do(string actionId, out string reason)
        {
            if (!CanDo(actionId, out reason)) return false;
            var a = _cfg.GetAction(actionId);
            State.ap -= ApCost(a);
            Apply(a.delta);
            State.fuel = Math.Min(_s.fuelTarget, State.fuel);
            if (a.cleansPanels) State.efficiency = 1f;
            if (a.outside) State.dose += _s.radPerEva;
            if (!string.IsNullOrEmpty(a.setsFlag))
            {
                if (a.solFlag) State.solFlags.Add(a.setsFlag);
                else State.flags.Add(a.setsFlag);
            }
            CheckVitals();
            return true;
        }

        /// <summary>Set a story flag (from a solved puzzle).</summary>
        public void SetFlag(string flag) => State.flags.Add(flag);

        public bool LaunchWindowOpen => State.sol >= _s.launchSol;

        public bool CanLaunch(out string reason)
        {
            reason = null;
            if (!string.IsNullOrEmpty(_s.goalFlag) && !State.flags.Contains(_s.goalFlag)) { reason = "reason.need." + _s.goalFlag; return false; }
            if (State.fuel < _s.fuelTarget) { reason = "reason.need_fuel"; return false; }
            if (!LaunchWindowOpen) { reason = "reason.window_not_open"; return false; }
            return true;
        }

        public bool Launch()
        {
            if (!CanLaunch(out _)) return false;
            State.outcome = SimOutcome.Won;
            return true;
        }

        /// <summary>
        /// End the sol: night use, tonight's event, power-out cascade, dust, then morning.
        /// collapsed = the astronaut ran out of time or energy and passed out (less rest).
        /// </summary>
        public NightReport Sleep(bool collapsed = false)
        {
            var r = new NightReport { sol = State.sol, collapsed = collapsed };
            if (collapsed) State.collapses++;
            if (State.outcome != SimOutcome.Ongoing) { r.outcome = State.outcome; return r; }

            if (State.sol >= _s.solsTotal)
            {
                State.outcome = SimOutcome.LostWindow;
                r.outcome = State.outcome;
                return r;
            }

            // Prevention is judged at bedtime, on what the player can see on the HUD.
            var ev = TonightEvent();
            bool prevented = ev != null && IsPrevented(ev);

            float shield = Shielded ? _s.shieldFactor : 1f;
            r.dose = _s.radPerSol * shield;
            r.habLoad = _s.habPowerLoad; r.o2Used = _s.nightO2; r.waterUsed = _s.nightWater; r.foodUsed = _s.nightFood;
            State.power -= _s.habPowerLoad;
            State.o2 -= _s.nightO2;
            State.water -= _s.nightWater;
            State.food -= _s.nightFood;

            if (ev != null)
            {
                r.eventId = ev.id;
                r.eventPrevented = prevented;
                State.log.Add(new LogEntry { sol = State.sol, id = ev.id, ok = prevented });
                if (!prevented) r.dose += ev.dose * shield;
                if (r.eventPrevented)
                {
                    State.morningSolarFactor = ev.preventedNextMorningSolarFactor;
                }
                else
                {
                    Apply(ev.penalty, clamp: false);
                    r.eventPenalty = ev.penalty;
                    State.efficiency -= ev.efficiencyLoss;
                    State.morningSolarFactor = ev.nextMorningSolarFactor;
                }
            }

            if (State.power < 0)
            {
                r.powerOut = true;
                State.o2 -= _s.powerOutO2Penalty;
                State.power = 0;
            }

            State.dose += r.dose;
            State.efficiency = Math.Max(_s.minEfficiency, State.efficiency - _s.dustPerSol);
            State.stamina += collapsed ? _s.collapseStamina : _s.sleepStamina;
            ClampAll();
            CheckVitals();
            r.outcome = State.outcome;
            if (State.outcome != SimOutcome.Ongoing) return r;

            State.sol++;
            r.morningSolar = StartMorning();
            return r;
        }

        /// <summary>Is the hab behind its radiation shield (e.g. the regolith berm)?</summary>
        public bool Shielded => !string.IsNullOrEmpty(_s.shieldFlag) && State.flags.Contains(_s.shieldFlag);

        /// <summary>Science tasks on this route, and how many are done.</summary>
        public int ScienceTotal => _cfg.actions.FindAll(a => a.science).Count;
        public int ScienceDone => _cfg.actions.FindAll(a => a.science && !string.IsNullOrEmpty(a.setsFlag) && State.flags.Contains(a.setsFlag)).Count;

        /// <summary>The three debrief stars: mission done, crew healthy, all science done.</summary>
        public bool[] Stars()
        {
            return new[]
            {
                State.outcome == SimOutcome.Won,
                State.outcome == SimOutcome.Won && State.dose <= _s.radSafe && State.collapses == 0,
                State.outcome == SimOutcome.Won && ScienceTotal > 0 && ScienceDone >= ScienceTotal,
            };
        }

        /// <summary>Would tonight's event be prevented if the sol ended now?</summary>
        public bool IsPrevented(EventDef ev)
        {
            if (!string.IsNullOrEmpty(ev.preventFlag) && !State.Has(ev.preventFlag)) return false;
            if (State.water < ev.preventMinWater) return false;
            if (State.power < ev.preventMinPower) return false;
            return true;
        }

        void Apply(ResourceDelta d, bool clamp = true)
        {
            State.o2 += d.o2;
            State.water += d.water;
            State.power += d.power;
            State.food += d.food;
            State.stamina += d.stamina;
            State.fuel += d.fuel;
            if (clamp) ClampAll();
        }

        void ClampAll()
        {
            State.o2 = Clamp(State.o2);
            State.water = Clamp(State.water);
            State.power = Clamp(State.power);
            State.food = Clamp(State.food);
            State.stamina = Clamp(State.stamina);
            State.fuel = Math.Max(0, Math.Min(_s.fuelTarget, State.fuel));
        }

        int Clamp(int v) => Math.Max(0, Math.Min(_s.maxValue, v));

        void CheckVitals()
        {
            if (State.outcome != SimOutcome.Ongoing) return;
            if (State.o2 <= 0) State.outcome = SimOutcome.LostO2;
            else if (State.water <= 0) State.outcome = SimOutcome.LostWater;
            else if (State.food <= 0) State.outcome = SimOutcome.LostFood;
            else if (_s.radLimit > 0 && State.dose >= _s.radLimit) State.outcome = SimOutcome.LostRadiation;
        }
    }
}
