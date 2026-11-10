// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System.Collections;
using TitanGate.Core;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>
    /// Route: Titan (the hardest). A long gravity-assist voyage, almost no sunlight (RTG power only),
    /// -179 C cold, thick nitrogen air. Pick the right suit, unlock and charge the warp gate, open it.
    /// </summary>
    public partial class GameFlow
    {
        bool _titanSuitOk, _coilTaken, _bitTaken;

        void BuildTitanOutside()
        {
            Backdrop(Art.TitanSky(0), Art.TitanRidge(0), Art.TitanRidge(1), Art.TitanGround());
            var o = AddOutsideRoom("room.surface.titan");
            _world.Spot(o, "titan_rtg", "spot.titan_rtg", Art.Rtg(true), -1.7f, 0, 1.4f, 0.9f);
            _world.Spot(o, "titan_drill", "spot.titan_drill", Art.DrillRig(false, false), 0.1f, 0, 0.6f, -0.9f, WorldBuilder.OrderPropFront);
            _world.Spot(o, "titan_gate", "spot.titan_gate", Art.Gate(0), 2.4f, 0, 2.6f, -1.6f);
            Particles(o, Art.Rain(0), 40, new Color(0.75f, 0.85f, 1f, 0.55f));
            foreach (var d in _dust) d.name = "rain";
        }

        IEnumerator TitanIntro()
        {
            yield return Say("orbi", "c2_land_1.titan");
            yield return Card("titan_cold");
            yield return Say("orbi", "c2_land_2.titan");
            yield return Card("titan_air");
            yield return Say("orbi", "c2_land_3.titan");
        }

        IEnumerator TitanOutsideFirst()
        {
            yield return Say("orbi", "c2_surface_first.titan");
            yield return Card("titan_lakes");
        }

        void TitanObjectives(System.Action<bool, string> line)
        {
            var s = _sim.State;
            line(_titanSuitOk, _loc.T("obj.titan.suit"));
            line(s.flags.Contains("gate_found"), _loc.T("obj.titan.gate"));
            line(s.fuel >= _rc.survival.fuelTarget, _loc.T("obj.titan.charge", s.fuel, _rc.survival.fuelTarget));
        }

        string TitanHint()
        {
            var s = _sim.State;
            if (!_titanSuitOk) return "titan_suit";
            if (!s.flags.Contains("gate_found")) return "titan_gate";
            if (!s.flags.Contains("drill_ready") && s.water <= 40) return "titan_drill";
            return null;
        }

        void TitanRefresh(SurvivalState s)
        {
            var w = _world;
            w.Find("titan_gate")?.SetSprite(Art.Gate(s.flags.Contains("gate_found") ? s.fuel : 0));
            w.Find("titan_rtg")?.SetSprite(Art.Rtg(!s.Has("rtg_checked") ? (Time.time % 1f) < 0.5f : true));
            w.Find("titan_drill")?.SetSprite(Art.DrillRig(s.flags.Contains("drill_ready"), false));
            w.Find("s_cabinet")?.SetSprite(Art.Cabinet(false));
        }

        IEnumerator TitanInteract(Hotspot h)
        {
            switch (h.id)
            {
                case "s_cabinet":
                    if (_coilTaken) return Say("you", "titan_cabinet_empty");
                    _coilTaken = true;
                    return TakeOnce("item_heater_coil", "titan_coil_take");
                case "s_crates":
                    if (_bitTaken) return Say("you", "titan_crates_empty");
                    _bitTaken = true;
                    return TakeOnce("item_drillbit", "titan_bit_take", Pose.Kneel0, Pose.Kneel1);
                case "titan_rtg": return Seq(Card("rtg"), ActionMenu(h, "look.titan_rtg", new[] { "check_rtg" }));
                case "titan_gate": return TitanGateSpot(h);
                case "titan_drill": return TitanDrill(h);
            }
            return null;
        }

        IEnumerator TitanSuitChoice()
        {
            string pick = null;
            yield return Choice("titan_suit.title", "titan_suit.body", new[] { "titan_suit.pressure", "titan_suit.heated", "titan_suit.none" }, p => pick = p);
            if (pick == null) yield break;
            if (pick == "titan_suit.heated")
            {
                _titanSuitOk = true;
                _sfx.Play("good");
                RefreshAll();
                yield return Say("orbi", "titan_suit_ok");
            }
            else
            {
                _sfx.Play("bad");
                yield return Say("orbi", "titan_suit_wrong." + pick.Substring(11));
            }
        }

        IEnumerator TitanGateSpot(Hotspot h)
        {
            if (!_sim.State.flags.Contains("gate_found"))
            {
                bool ok = false;
                yield return Keypad("keypad.titan_gate", _cfg.puzzles.titanGateCode, "titan_gate_wrong", r => ok = r);
                if (!ok) yield break;
                yield return _player.Act(Pose.Reach, Pose.Work0, 0.8f);
                _sim.SetFlag("gate_found");
                RefreshAll();
                yield return Say("orbi", "titan_gate_open");
                yield return Card("dragonfly");
            }
            yield return ActionMenu(h, "look.titan_gate", new[] { "charge_gate" });
        }

        IEnumerator TitanDrill(Hotspot h)
        {
            if (_sim.State.flags.Contains("drill_ready")) { yield return ActionMenu(h, "look.titan_drill", new[] { "drill_ice" }); yield break; }
            if (_inv.Has("item_heated_bit"))
            {
                yield return _player.Act(Pose.Work0, Pose.Work1, _cfg.presentation.actionSeconds);
                _inv.Remove("item_heated_bit");
                _sim.SetFlag("drill_ready");
                _sfx.Play("good");
                RefreshAll();
                yield return Say("orbi", "titan_drill_ready");
                yield break;
            }
            yield return Say("orbi", _inv.Has("item_drillbit") ? "titan_drill_cold" : "titan_drill_need");
        }
    }
}
