// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does a steady clock, things to find outside and guests who grow fond of you make each day feel full?
// Date: 2026-11-13
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>
    /// The walkable base's day clock (time passes while you walk; dinner from 18:00, the day ends at 20:00),
    /// salvage to search outside (ice, scrap, seeds, an old rover), and guest friendship (fed guests grow fond
    /// of you and help more; guests left hungry leave and may break something).
    /// </summary>
    public partial class GameFlow
    {
        // about 6 real minutes from 06:00 to 20:00 (was 4): enough to run the base, not just rush it
        const float SecondsPerHour = 26f, DinnerHour = 18f, LateHour = 20f;
        bool _bLateDone, _bDinnerToast, _autoClock;

        /// <summary>Time passes while you are free to walk. At 20:00 whatever is still open is decided for you.</summary>
        void BClock(float dt)
        {
            if (MarsWalkOnly || _life == null || _life.State.outcome != LifeOutcome.Ongoing || _ui.Blocking || _bGather || _bOpen) return;
            if (_autoLog != null && !_autoClock) return;   // the screenshot pass drives the day itself
            if (_lbView != LbView.Card && _lbView != LbView.Ration) return;
            var sc = _life.State.schedule;
            // restore mode: the clock moves only when you act (every action shows its hours first)
            if (sc.Hour < LateHour && !RestoreMode) sc.TrySpend(Mathf.Min(dt / SecondsPerHour, LateHour - sc.Hour));
            if (_lbView == LbView.Ration && sc.Hour >= DinnerHour && !_bDinnerToast) { _bDinnerToast = true; _ui.Toast(_loc.T("base.clock.dinner"), GameUI.CAmber); }
            if (sc.Hour < LateHour - 0.001f) return;
            if (_lbView == LbView.Card && _lbCard != null && !_bKnockPending)
            {
                // too late for today's open event: it resolves with its last option
                _ui.Toast(_loc.T("base.clock.late_event"), GameUI.CRed);
                BTimeout();
            }
            else if (_lbView == LbView.Ration && !_bLateDone)
            {
                _bLateDone = true;
                _ui.Toast(_loc.T("base.clock.bedtime"), GameUI.CAmber);
                StartCoroutine(BGather());
            }
        }

        void BClockNewDay() { _bLateDone = false; _bDinnerToast = false; }

        // ------------------------------------------------------------------ salvage outside

        static readonly Vector2[] SalvageSpots =
        {
            new Vector2(32.5f, 25.4f), new Vector2(38.6f, 26.3f), new Vector2(44.2f, 24.2f),
            new Vector2(43.6f, 16.4f), new Vector2(39.4f, 9.6f), new Vector2(35.2f, 20.4f),
        };
        bool _bWreckSearched;

        void BBuildSalvage()
        {
            for (int i = 0; i < SalvageSpots.Length; i++)
            {
                var p = SalvageSpots[i];
                var sp = BProp("salvage" + i, "base.spot.salvage", Art.Scrap(true), p.x, p.y, new Vector2(p.x, p.y - 0.6f));
                sp.sr.sortingOrder = SortY(p.y);
                BLamp(p + new Vector2(0, 0.3f), new Color(1f, 0.85f, 0.5f), 1.4f, 0.4f);
            }
            BProp("wreck", "base.spot.wreck", Art.CargoPod(true), 41.6f, 3.2f, new Vector2(41.6f, 2.4f), 2, 1);
            _bWreckSearched = false;
        }

        /// <summary>Each morning about half the searched piles have new things blown in by the wind.</summary>
        void BRespawnSalvage()
        {
            var rng = new System.Random(_life.State.day * 13 + 5);
            for (int i = 0; i < SalvageSpots.Length; i++)
            {
                var s = BSpotById("salvage" + i);
                if (s == null) continue;
                if (!s.sr.enabled && rng.NextDouble() < 0.5) { s.sr.enabled = true; s.sr.sprite = Art.Scrap(true); }
            }
        }

        /// <summary>Search a pile or the old rover. Takes half an hour (or one hour for the rover).</summary>
        bool BUseOutside(BSpot s)
        {
            if (s.id == "wreck")
            {
                if (_bWreckSearched) { _ui.Toast(_loc.T("base.wreck.empty"), GameUI.CDim); return true; }
                if (!BTimedTask(new Stock { o2 = 2 }, 1)) return true;
                _bWreckSearched = true; _bSeeds += 2;
                _sfx.Play("good");
                _ui.Toast(_loc.T("base.wreck.found"), GameUI.CGreen);
                return true;
            }
            if (!s.id.StartsWith("salvage")) return false;
            if (!s.sr.enabled) return true;
            int i = s.id[7] - '0';
            var rng = new System.Random(_life.State.day * 101 + i * 17 + 3);
            double r = rng.NextDouble();
            string what; var d = new Stock();
            if (r < 0.35) { what = "ice"; d.water = 1; }
            else if (r < 0.6) { what = "scrap"; }
            else if (r < 0.75) { what = "seeds"; }
            else if (r < 0.85) { what = "can"; d.food = 1; }
            else what = "dust";
            if (!BTimedTask(d, 0.5f)) return true;
            if (what == "seeds") _bSeeds++;
            if (what == "scrap") { string part = rng.NextDouble() < 0.5 ? "fuse" : rng.NextDouble() < 0.5 ? "tape" : "cable"; ToolboxAdd(part); _ui.Toast(TT("Spare part for the toolbox: ", "ได้อะไหล่เก็บเข้าตู้เครื่องมือ: ") + ItemName(part), GameUI.CGreen); }
            s.sr.enabled = false;
            _sfx.Play(what == "dust" ? "bad" : "card");
            _ui.Toast(_loc.T("base.salvage." + what), what == "dust" ? GameUI.CDim : GameUI.CGreen);
            return true;
        }

        // ------------------------------------------------------------------ guest friendship

        readonly Dictionary<string, int> _bFriend = new Dictionary<string, int>();

        int Friend(string who) => _bFriend.TryGetValue(who, out var f) ? f : 0;

        /// <summary>
        /// After each night: guests who were fed grow fonder of you (and help more once they really like you);
        /// guests who were not fed lose patience. Very fond guests bring a bonus that fits who they are.
        /// </summary>
        void BFriendsAfterNight(ICollection<string> fed)
        {
            foreach (var g in new List<string>(_life.State.guests))
            {
                int f = Friend(g) + (fed.Contains(g) ? 1 : -2);
                _bFriend[g] = f;
                if (f >= 2)
                {
                    var bonus = g == "rex" || g == "jules" || g == "nok" ? new Stock { power = 1 }
                        : g == "kira" || g == "aya" || g == "ree" || g == "bizz" ? new Stock { food = 1 }
                        : new Stock { water = 1 };
                    _life.ApplyTask(bonus);
                    _ui.Toast(_loc.T("base.friend.help", LbName(g)), GameUI.CGreen);
                }
                else if (f <= -1 && _life.State.stock.food >= 1 && _life.State.guests.Contains(g))
                {
                    _life.ApplyTask(new Stock { food = -1 });
                    _ui.Toast(_loc.T("base.friend.grumpy", LbName(g)), GameUI.CRed);
                }
            }
        }

        void BFriendTalk(string who) { if (_bDone.Add("vn_" + who)) _bFriend[who] = Friend(who) + 1; }
    }
}
