// LAST DOCK — picks incidents from the written set, fairly and repeatably (seeded).
using System;
using System.Collections.Generic;

namespace TitanGate.Core.LastDock
{
    /// <summary>
    /// Rules: at most one major fault at a time; never the same incident twice in a row; a cooldown per kind;
    /// only systems that are working now can break; detached or sealed rooms are never the source; an incident
    /// only starts when the player still has a Work Unit left to answer it; day 1 is quiet.
    /// One incident is planned per day from day 2, at a seeded moment of the day.
    /// </summary>
    public class IncidentDirector
    {
        readonly int _seed;
        readonly StationConfig _c;
        int _plannedStep = -1, _plannedDay = -1;
        public Fault Last = Fault.None;
        Dictionary<Fault, int> _lastDay = new Dictionary<Fault, int>();
        /// <summary>Previews run with a silent director: nothing is rolled.</summary>
        public bool Silent;

        public IncidentDirector(int seed, StationConfig c) { _seed = seed; _c = c; }

        public IncidentDirector Clone()
        {
            var d = (IncidentDirector)MemberwiseClone();
            d._lastDay = new Dictionary<Fault, int>(_lastDay);
            return d;
        }

        public void NewDay(StationSimulation s)
        {
            var dayRng = new Random(_seed * 9973 + s.Day * 131);
            _plannedDay = s.Day;
            _plannedStep = s.Day >= 2 ? dayRng.Next(0, Math.Max(1, _c.workUnits - 1)) : -1;
        }

        bool Cooling(Fault f, int day) => _lastDay.TryGetValue(f, out var d) && day - d < _c.cooldownDays;

        /// <summary>After an action (or at day start, step 0): start today's planned incident if it is fair to.</summary>
        public void MaybeTrigger(StationSimulation s, int actionsToday)
        {
            if (Silent || s.Outcome != Outcome.Ongoing) return;
            if (_plannedDay != s.Day) NewDay(s);
            if (actionsToday != _plannedStep) return;
            _plannedStep = -1;
            if (s.ActiveFault != Fault.None) return;     // never two at once
            if (s.WorkUnits < 1) return;                  // always leave a move to answer with
            var options = new List<(Fault f, Room? room)>();
            void Add(Fault f, bool can, Room? room = null) { if (can && f != Last && !Cooling(f, s.Day)) options.Add((f, room)); }
            Add(Fault.Scrubber, s.ScrubberOk);
            Add(Fault.Thermal, s.ThermalOk);
            Add(Fault.Fan, s.FanOk && s.ScrubberOk);
            Add(Fault.O2Gen, s.O2GenOk && s.ScrubberOk);
            foreach (var r in new[] { Room.Storage, Room.Thermal, Room.Comms })
                if (s.Usable(r)) { Add(Fault.Leak, true, r); }
            if (options.Count == 0) return;
            var dayRng = new Random(_seed * 31 + s.Day * 7 + actionsToday);
            var pick = options[dayRng.Next(options.Count)];
            Last = pick.f; _lastDay[pick.f] = s.Day;
            s.ForceFault(pick.f, pick.room);
        }

        /// <summary>Incident E: does the power dip during this transmission?</summary>
        public bool RollPowerDip(StationSimulation s)
        {
            if (Silent || s.Day < 2 || Cooling(Fault.PowerDip, s.Day) || s.ActiveFault != Fault.None) return false;
            var r = new Random(_seed * 17 + s.Day * 3 + s.Signal);
            if (r.NextDouble() >= _c.powerDipChance) return false;
            _lastDay[Fault.PowerDip] = s.Day;   // the dip has its own cooldown; it does not reset the no-repeat rule for faults
            return true;
        }
    }
}
