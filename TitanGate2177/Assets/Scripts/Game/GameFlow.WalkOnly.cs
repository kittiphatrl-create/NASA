// PROTOTYPE - NOT FOR PRODUCTION
// Question: After landing, is it better to just walk the Mars base freely while the next management design is made?
// Date: 2026-11-14
using System.Collections.Generic;
using TitanGate.Core;

namespace TitanGate.Game
{
    /// <summary>
    /// Walk-only Mars base: after the landing the map stays, but every system on top of it is off — no restore
    /// tasks or puzzles, no quests or cards, no clock, no emergencies or blackouts, no suit timer or radiation,
    /// no ORBI warnings and no HUD panels. The cadet and the pet walk, doors open, that is all. The ship
    /// chapter before the landing is unchanged. Set WalkOnlyBase = false to bring the restore chapter back.
    /// </summary>
    public partial class GameFlow
    {
        public static bool WalkOnlyBase = true;

        bool MarsWalkOnly => WalkOnlyBase && _bMode == 0 && WalkBase && !ForceClassicBase;

        /// <summary>A run with no cards and no fixed nights, so nothing happens by itself.</summary>
        LifeboatConfig WalkOnlyLifeboatConfig()
        {
            var c = UnityEngine.JsonUtility.FromJson<LifeboatConfig>(UnityEngine.JsonUtility.ToJson(LC));
            c.cards = new List<LifeCard>();
            c.nights = new List<LifeNightDef>();
            c.daysTotal = 99; c.sliceDays = 0;
            return c;
        }

        /// <summary>Only doors and the pet react in walk-only mode.</summary>
        bool WalkOnlyUse(BSpot s)
        {
            if (!MarsWalkOnly) return false;
            if (s.id == "hatch" || s.id == "hatch_out") { BUseDoor(); return true; }
            if (s.id == "pet") { PetMenu(); return true; }
            return true;   // everything else is scenery
        }
    }
}
