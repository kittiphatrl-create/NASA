// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System;
using System.Collections.Generic;

namespace TitanGate.Core
{
    /// <summary>Items the astronaut carries. Order is kept so the bar does not reshuffle.</summary>
    public class Inventory
    {
        readonly List<string> _items = new List<string>();
        public event Action Changed;

        public IReadOnlyList<string> Items => _items;
        public bool Has(string id) => _items.Contains(id);

        public bool Add(string id)
        {
            if (string.IsNullOrEmpty(id) || _items.Contains(id)) return false;
            _items.Add(id);
            Changed?.Invoke();
            return true;
        }

        public bool Remove(string id)
        {
            bool ok = _items.Remove(id);
            if (ok) Changed?.Invoke();
            return ok;
        }

        /// <summary>Combine two carried items by recipe. Returns the result id or null.</summary>
        public string Combine(string a, string b, IList<Recipe> recipes)
        {
            if (!Has(a) || !Has(b) || a == b) return null;
            var r = Puzzles.FindRecipe(a, b, recipes);
            if (r == null) return null;
            _items.Remove(a);
            _items.Remove(b);
            _items.Add(r.result);
            Changed?.Invoke();
            return r.result;
        }
    }

    /// <summary>Knowledge cards the player has unlocked.</summary>
    public class Codex
    {
        readonly List<string> _unlocked = new List<string>();
        public event Action<string> Unlocked;
        public IReadOnlyList<string> Cards => _unlocked;
        public bool Has(string id) => _unlocked.Contains(id);

        /// <summary>Unlock a card. Returns true only the first time.</summary>
        public bool Unlock(string id)
        {
            if (string.IsNullOrEmpty(id) || _unlocked.Contains(id)) return false;
            _unlocked.Add(id);
            Unlocked?.Invoke(id);
            return true;
        }
    }

    /// <summary>Pure puzzle rules. Every answer here is written on a knowledge card in the game.</summary>
    public static class Puzzles
    {
        public static bool CheckCode(string input, string expected) =>
            !string.IsNullOrEmpty(input) && input.Trim() == expected;

        /// <summary>Can the ship reach this destination with the fuel left?</summary>
        public static bool CanReach(PuzzleConfig p, string destinationId)
        {
            var o = p.navOptions.Find(n => n.id == destinationId);
            return o != null && o.fuelNeeded <= p.fuelGaugePercent;
        }

        /// <summary>The destination the puzzle wants: the farthest reachable one.</summary>
        public static string BestReachable(PuzzleConfig p)
        {
            NavOption best = null;
            foreach (var o in p.navOptions)
                if (o.fuelNeeded <= p.fuelGaugePercent && (best == null || o.fuelNeeded > best.fuelNeeded)) best = o;
            return best?.id;
        }

        /// <summary>Index of the first wrong step in the landing order, -1 if all correct so far.</summary>
        public static int FirstWrongEdlStep(IList<string> order, PuzzleConfig p)
        {
            for (int i = 0; i < order.Count; i++)
                if (i >= p.edlOrder.Count || order[i] != p.edlOrder[i]) return i;
            return -1;
        }

        public static bool EdlComplete(IList<string> order, PuzzleConfig p) =>
            order.Count == p.edlOrder.Count && FirstWrongEdlStep(order, p) == -1;

        /// <summary>First wrong step against any expected order (per-route landing), -1 if fine so far.</summary>
        public static int FirstWrongStep(IList<string> order, IList<string> expected)
        {
            for (int i = 0; i < order.Count; i++)
                if (i >= expected.Count || order[i] != expected[i]) return i;
            return -1;
        }

        public static bool StepsComplete(IList<string> order, IList<string> expected) =>
            order.Count == expected.Count && FirstWrongStep(order, expected) == -1;

        /// <summary>Where a navigation choice leads; null when it is out of reach (no route).</summary>
        public static string RouteFor(PuzzleConfig p, string destinationId)
        {
            var o = p.navOptions.Find(n => n.id == destinationId);
            return o == null || string.IsNullOrEmpty(o.route) ? null : o.route;
        }

        public static Recipe FindRecipe(string a, string b, IList<Recipe> recipes)
        {
            foreach (var r in recipes)
                if ((r.a == a && r.b == b) || (r.a == b && r.b == a)) return r;
            return null;
        }
    }
}
