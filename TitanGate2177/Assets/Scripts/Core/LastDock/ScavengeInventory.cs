// LAST DOCK — the 60-second scavenge: carry up to 3, the lifeboat holds 8, only deposited items reach the station.
using System;
using System.Collections.Generic;

namespace TitanGate.Core.LastDock
{
    /// <summary>
    /// What can ride in the lifeboat. The first four fill the bars; the rest are special items found on the ship:
    /// Solar (portable panel, +1 power a day), RadioKit (radio repair in 1 day), RadMeter (no radiation when exploring),
    /// FrozenFood (3 safe meals), Flashlight (works through a power dip), Medkit (crew health +1).
    /// </summary>
    public enum Res { Food, Oxygen, Battery, Parts, Solar, RadioKit, RadMeter, FrozenFood, Flashlight, Medkit }

    public class ScavengeInventory
    {
        public const int CarryMax = 3, PodMax = 8;
        readonly List<Res> _carried = new List<Res>();
        readonly List<Res> _pod = new List<Res>();

        public IReadOnlyList<Res> Carried => _carried;
        public IReadOnlyList<Res> Pod => _pod;
        public int PodSpace => PodMax - _pod.Count;

        /// <summary>Pick something up. False when the hands are full.</summary>
        public bool Pick(Res r)
        {
            if (_carried.Count >= CarryMax) return false;
            _carried.Add(r);
            return true;
        }

        /// <summary>Put what you carry into the lifeboat, as much as fits. Returns how many went in.</summary>
        public int Deposit()
        {
            int n = 0;
            while (_carried.Count > 0 && _pod.Count < PodMax) { _pod.Add(_carried[0]); _carried.RemoveAt(0); n++; }
            return n;
        }

        /// <summary>Only what is in the lifeboat reaches the station; what is still in your hands is lost.</summary>
        public Dictionary<Res, int> Delivered()
        {
            var d = new Dictionary<Res, int>();
            foreach (Res r in Enum.GetValues(typeof(Res))) d[r] = 0;
            foreach (var r in _pod) d[r]++;
            return d;
        }
    }
}
