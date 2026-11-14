// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do 8-direction movement, running, carrying and using in every direction, and doors that slide open
//           make the ship and the base feel like real places?
// Date: 2026-11-15
using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>
    /// Feel of control: the cadet faces 8 directions (5 rendered in Blender, the left side mirrored), runs, and
    /// carries or uses things facing the way they look. Doorways get sliding doors that open as you come near.
    /// The pet system is off (the user cut it).
    /// </summary>
    public partial class GameFlow
    {
        /// <summary>The user cut pets: no pet pick, no pet on the ship, no pet in the morning jobs.</summary>
        public static bool NoPets = true;

        /// <summary>Morning jobs without the pet: report from the radio, then rest on the sofa.</summary>
        static readonly SStep[] ShipStepsNoPet =
        {
            new SStep { target = "radio", textKey = "ship.q.radio", factKey = "ship.fact.radio" },
            new SStep { target = "sofa", textKey = "ship.q.sofa" },
        };

        SStep[] SSteps => NoPets ? ShipStepsNoPet : ShipSteps;

        // ------------------------------------------------------------------ 8 directions

        int _bDir5;      // 0 S, 1 SE, 2 E, 3 NE, 4 N
        bool _bFlip8;    // west side: mirror the east sprite

        void BUpdateDir8()
        {
            var m = _bLastMove;
            if (m.sqrMagnitude < 0.01f) return;
            int k = Mathf.RoundToInt(Mathf.Atan2(m.y, m.x) * Mathf.Rad2Deg / 45f);
            switch (k)
            {
                case 0: _bDir5 = 2; _bFlip8 = false; break;
                case 1: _bDir5 = 3; _bFlip8 = false; break;
                case 2: _bDir5 = 4; _bFlip8 = false; break;
                case 3: _bDir5 = 3; _bFlip8 = true; break;
                case 4: case -4: _bDir5 = 2; _bFlip8 = true; break;
                case -3: _bDir5 = 1; _bFlip8 = true; break;
                case -2: _bDir5 = 0; _bFlip8 = false; break;
                case -1: _bDir5 = 1; _bFlip8 = false; break;
            }
        }

        /// <summary>Walk, run, idle and blink in 8 directions; falls back to the code-drawn cadet outside (suit).</summary>
        void BCadetSprite(int frame, bool outside)
        {
            BUpdateDir8();
            Sprite s = null;
            if (!outside)
            {
                string key = frame == -1 ? "idle" : frame == -2 ? "blink" : (_bRun && _bMoving ? "r" : "w") + (((frame % 8) + 8) % 8);
                s = Art.Cadet8(_bGather ? 0 : _bDir5, key);
            }
            if (s != null) { _bPlayer.sprite = s; _bPlayer.flipX = !_bGather && _bFlip8; return; }
            _bPlayer.sprite = Art.Cadet(_bGather ? 0 : _bFacing, frame, outside, _bRun && _bMoving);
            _bPlayer.flipX = _bFacing == 2 && !_bRight;
        }

        /// <summary>Carry and use poses facing the way the cadet looks (called after the code-drawn action pose).</summary>
        void BCadetActionSprite(string pose)
        {
            if (_bPlayer == null || BIsOutside(_bPos)) return;
            BUpdateDir8();
            string key = pose == "carry" ? (_bMoving ? "c" + ((int)_bGaitPhase % 8) : "ci") : "u" + ((int)(_bAnim * 6) % 4);
            var s = Art.Cadet8(_bDir5, key);
            if (s == null) return;
            _bPlayer.sprite = s; _bPlayer.flipX = _bFlip8;
        }

        // ------------------------------------------------------------------ automatic doors

        class BDoorGroup
        {
            public RectInt rect;
            public bool horizontal;   // a door in a horizontal wall (you walk through it up or down)
            public SpriteRenderer sr;
            public float open, lastSound = -10f;
            public bool wantOpen;
            public Vector2 center;
        }

        readonly List<Vector2Int> _bDoorTiles = new List<Vector2Int>();
        readonly List<BDoorGroup> _bDoorGroups = new List<BDoorGroup>();

        /// <summary>Group the doorway tiles cut by LDoor and put a sliding door on each (ship and LAST DOCK base).</summary>
        void BBuildDoors()
        {
            _bDoorGroups.Clear();
            if (_bMode == 0) return;   // the old Mars base keeps its own airlock door
            var left = new HashSet<Vector2Int>(_bDoorTiles);
            while (left.Count > 0)
            {
                var e = left.GetEnumerator(); e.MoveNext(); var start = e.Current;
                var group = new List<Vector2Int>(); var q = new Queue<Vector2Int>(); q.Enqueue(start); left.Remove(start);
                while (q.Count > 0)
                {
                    var c = q.Dequeue(); group.Add(c);
                    foreach (var d in new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down })
                        if (left.Remove(c + d)) q.Enqueue(c + d);
                }
                int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
                foreach (var c in group) { x0 = Mathf.Min(x0, c.x); y0 = Mathf.Min(y0, c.y); x1 = Mathf.Max(x1, c.x); y1 = Mathf.Max(y1, c.y); }
                bool horiz = x1 > x0 || y1 == y0 && LMakesWallAt(x0 - 1, y0) && LMakesWallAt(x1 + 1, y0);
                var g = new BDoorGroup { rect = new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1), horizontal = horiz };
                g.center = new Vector2(x0 + g.rect.width * .5f, y0 + g.rect.height * .5f);
                var sprite = Art.Door(horiz, false, 0);
                if (sprite == null) continue;
                g.sr = BSprite("door", sprite, new Vector2(x0, y0), horiz ? SortY(y0) + 2 : -29990);
                g.sr.transform.localScale = horiz ? new Vector3(g.rect.width / 2f, 1, 1) : new Vector3(1, g.rect.height / 2f, 1);
                _bDoorGroups.Add(g);
            }
        }

        bool BDoorLocked(BDoorGroup g)
        {
            if (_bMode == 1) return ShipVaultDoorLocked(g.rect);
            if (_bMode != 2 || _ld == null) return false;
            for (int x = g.rect.xMin; x < g.rect.xMax; x++)
                for (int y = g.rect.yMin; y < g.rect.yMax; y++)
                    if (LdDoorTileLocked(new Vector2Int(x, y))) return true;
            return false;
        }

        /// <summary>Doors slide open when the cadet comes near and close behind them, with a hiss.</summary>
        void BDoorsUpdate(float dt)
        {
            foreach (var g in _bDoorGroups)
            {
                bool locked = BDoorLocked(g), shut = locked || LdDoorAlwaysShut(g.rect);
                for (int x = g.rect.xMin; x < g.rect.xMax; x++)
                    for (int y = g.rect.yMin; y < g.rect.yMax; y++) _bSolid[x, y] = shut;   // a locked door blocks the way
                float reach = g.horizontal ? Mathf.Max(Mathf.Abs(_bPos.x - g.center.x) - g.rect.width * .5f, 0) + Mathf.Abs(_bPos.y - g.center.y) * .8f
                                           : Mathf.Max(Mathf.Abs(_bPos.y - g.center.y) - g.rect.height * .5f, 0) + Mathf.Abs(_bPos.x - g.center.x) * .8f;
                bool want = !shut && reach < (g.wantOpen ? 2.4f : 1.7f);   // opens at 1.7, closes past 2.4: no flicker at the edge
                if (want != g.wantOpen)
                {
                    g.wantOpen = want;
                    float vol = Mathf.Clamp01(1.2f - Vector2.Distance(_bPos, g.center) / 8f);
                    if (vol > 0.05f && Time.time - g.lastSound > 0.6f) { g.lastSound = Time.time; _sfx.Play(want ? "door_open" : "door_close", vol * 0.7f); }
                }
                g.open = Mathf.MoveTowards(g.open, want ? 1f : 0f, dt * 4.5f);
                var s = Art.Door(g.horizontal, locked, Mathf.RoundToInt(g.open * 3));
                if (s != null && g.sr.sprite != s) g.sr.sprite = s;
            }
            LdPolishUpdate(dt);   // grade, music, particles (GameFlow.Polish.cs)
        }
    }
}
