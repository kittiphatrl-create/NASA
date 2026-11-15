// LAST DOCK — picks incidents from the written set, fairly and repeatably (seeded).
using System;
using System.Collections.Generic;

namespace TitanGate.Core.LastDock
{
    /// <summary>
    /// Rules: at most one major fault at a time; never the same incident twice in a row; a cooldown per kind;
    /// only running machines that work can break, and only when worn (BreakChance); otherwise a rare hull leak;
    /// detached or sealed rooms are never the source; an incident
    /// only starts when the player still has a Work Unit left to answer it; day 1 is quiet.
    /// One incident is planned per day from day 2, at dawn or after the first action (never while the crew
    /// eats and sleeps), so it is always part of the day the player is playing.
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
            // step 0 = at dawn; the game announces a dawn fault only after the player starts the day, never on the
            // night card, so it does not look like it came with the end of the previous day
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
            var dayRng = new Random(_seed * 31 + s.Day * 7 + actionsToday);
            // management core: the running machine in the worst shape may break — the worse its condition, the likelier.
            // Servicing machines before they get bad keeps this quiet: a risk the player can manage, not luck.
            Machine? worst = null;
            foreach (Machine m in Enum.GetValues(typeof(Machine)))
            {
                var f = StationSimulation.FaultOf(m);
                if (!s.Running(m) || !s.IsOk(m) || f == Last || Cooling(f, s.Day)) continue;
                if (worst == null || s.Cond[(int)m] < s.Cond[(int)worst.Value]) worst = m;
            }
            double roll = dayRng.NextDouble();
            if (worst != null && roll < BreakChance(s.Cond[(int)worst.Value]))
            {
                var f = StationSimulation.FaultOf(worst.Value);
                Last = f; _lastDay[f] = s.Day;
                s.ForceFault(f);
                return;
            }
            // nothing gave way: a micrometeorite can still hole the hull now and then
            if (Last == Fault.Leak || Cooling(Fault.Leak, s.Day) || dayRng.NextDouble() >= _c.leakChance) return;
            var rooms = new List<Room>();
            foreach (var r in new[] { Room.Storage, Room.Thermal, Room.Comms }) if (s.Usable(r)) rooms.Add(r);
            if (rooms.Count == 0) return;
            Last = Fault.Leak; _lastDay[Fault.Leak] = s.Day;
            s.ForceFault(Fault.Leak, rooms[dayRng.Next(rooms.Count)]);
        }

        /// <summary>Chance a machine in this condition breaks at today's check.</summary>
        public static double BreakChance(int cond) => cond >= 60 ? 0 : cond >= 40 ? 0.25 : cond >= 20 ? 0.55 : 0.9;

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
