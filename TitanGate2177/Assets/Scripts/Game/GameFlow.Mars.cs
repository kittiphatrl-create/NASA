// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System.Collections;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>Route: Mars. MOXIE, solar panels, ice, Sabatier fuel, the great dust storm, launch to orbit.</summary>
    public partial class GameFlow
    {
        bool _sealantTaken, _plateTaken, _podOpen;

        void BuildMarsOutside()
        {
            Backdrop(Art.MarsSky(), Art.MarsRidge(0), Art.MarsRidge(1), Art.MarsGround());
            var o = AddOutsideRoom("room.surface.mars");
            _world.Spot(o, "mars_solar", "spot.mars_solar", Art.SolarArray(false, false), -1.4f, 0, 2.2f, 0.4f);
            _world.Deco(o, Art.IceSite(), 2.1f, 0.01f, 0.9f, WorldBuilder.OrderProp - 3);
            _world.Spot(o, "mars_drill", "spot.mars_drill", Art.DrillRig(false, false), 2.1f, 0, 1.0f, -0.9f);
            _world.Spot(o, "mars_pod", "spot.mars_pod", Art.CargoPod(false), 3.9f, 0, 1.9f, -1.3f);
            _world.Spot(o, "mars_scrap", "spot.mars_scrap", Art.Scrap(true), 0.6f, 0, -0.4f, -0.7f, WorldBuilder.OrderPropFront);
            Particles(o, Art.Dust(), 26, new Color(1, 1, 1, 0.5f));
        }

        IEnumerator MarsIntro()
        {
            yield return Say("orbi", "c2_land_1.mars");
            yield return Card("sol");
            yield return Say("orbi", "c2_land_2.mars");
            yield return Say("orbi", "c2_land_3.mars");
        }

        IEnumerator MarsOutsideFirst()
        {
            yield return Say("orbi", "c2_surface_first.mars");
            yield return Card("mars_air");
        }

        void MarsObjectives(System.Action<bool, string> line)
        {
            var s = _sim.State;
            line(s.flags.Contains("line_repaired"), _loc.T("obj.mars.line"));
            line(s.flags.Contains("catalyst_installed"), _loc.T("obj.mars.catalyst"));
            line(s.fuel >= _rc.survival.fuelTarget, _loc.T("obj.mars.fuel", s.fuel, _rc.survival.fuelTarget));
        }

        string MarsHint()
        {
            var s = _sim.State;
            if (!s.flags.Contains("line_repaired")) return _inv.Has("item_hull_patch") ? "c2_line_fix" : "c2_line";
            if (!s.flags.Contains("catalyst_installed")) return "c2_catalyst";
            return null;
        }

        void MarsRefresh(SurvivalState s)
        {
            var w = _world;
            bool stowed = s.Has(SurvivalSim.FlagPanelsStowed);
            w.Find("mars_solar")?.SetSprite(Art.SolarArray(stowed, s.efficiency < 0.75f));
            w.Find("s_reactor")?.SetSprite(Art.Sabatier(s.flags.Contains("catalyst_installed"), false));
            w.Find("s_line")?.SetSprite(Art.FuelLine(s.flags.Contains("line_repaired"), 0));
            w.Find("mars_pod")?.SetSprite(Art.CargoPod(_podOpen));
            w.Find("s_toolbox")?.SetSprite(Art.Toolbox(!_sealantTaken));
            w.Find("mars_scrap")?.SetSprite(Art.Scrap(!_plateTaken));
            w.Find("s_cabinet")?.SetSprite(Art.Cabinet(!s.flags.Contains("catalyst_installed")));
            w.Find("mars_drill")?.SetSprite(Art.DrillRig(s.flags.Contains("drill_ready"), false));
        }

        /// <summary>Mars-only hotspots. Null = not a Mars hotspot (the shared handler takes it).</summary>
        IEnumerator MarsInteract(Hotspot h)
        {
            var s = _sim.State;
            switch (h.id)
            {
                case "s_toolbox":
                    if (_sealantTaken) return Say("you", "c2_toolbox_look");
                    _sealantTaken = true;
                    return TakeOnce("item_sealant", "c2_toolbox_take", Pose.Kneel0, Pose.Kneel1);
                case "s_cabinet": return CabinetModal();
                case "s_reactor": return MarsReactor();
                case "s_line": return MarsLine();
                case "mars_solar": return Seq(Card("solar"), ActionMenu(h, "look.c2_solar", new[] { "clean_panels", "stow_panels" }));
                case "mars_scrap":
                    if (_plateTaken) return Say("you", "c2_scrap_look");
                    _plateTaken = true;
                    return TakeOnce("item_plate", "c2_scrap_take", Pose.Kneel0, Pose.Kneel1);
                case "mars_pod": return MarsPod();
                case "mars_drill": return MarsDrill(h);
            }
            return null;
        }

        static IEnumerator Seq(params IEnumerator[] parts)
        {
            foreach (var p in parts) yield return p;
        }

        IEnumerator MarsReactor()
        {
            var s = _sim.State;
            if (s.flags.Contains("catalyst_installed")) { yield return ActionMenu(_world.Find("s_reactor"), "look.c2_reactor", new[] { "sabatier" }); yield break; }
            string cat = _cfg.puzzles.catalysts.Find(c => _inv.Has(c));
            if (cat == null)
            {
                yield return Say("you", "c2_reactor_read");
                yield return Card("sabatier");
                yield return Say("orbi", "c2_reactor_need");
                yield break;
            }
            yield return _player.Act(Pose.Work0, Pose.Work1, _cfg.presentation.actionSeconds);
            if (cat == _cfg.puzzles.catalystCorrect)
            {
                _inv.Remove(cat);
                _sim.SetFlag("catalyst_installed");
                _sfx.Play("good");
                RefreshAll();
                yield return Say("orbi", "c2_catalyst_ok");
                yield return Card("sabatier");
            }
            else
            {
                _sfx.Play("bad");
                yield return Say("orbi", "c2_catalyst_wrong", NameOf(cat));
            }
        }

        IEnumerator MarsLine()
        {
            var s = _sim.State;
            if (s.flags.Contains("line_repaired")) { yield return Say("you", "c2_line_done"); yield break; }
            if (_inv.Has("item_hull_patch"))
            {
                _sim.SetFlag("has_hull_patch");
                bool did = false;
                yield return ActionMenu(_world.Find("s_line"), "look.c2_line_ready", new[] { "repair_line" }, a => did = a == "repair_line");
                if (did) _inv.Remove("item_hull_patch");
            }
            else if (_inv.Has("item_plate") && _inv.Has("item_sealant")) yield return Say("orbi", "c2_line_combine");
            else yield return Say("you", "c2_line_need");
        }

        IEnumerator MarsPod()
        {
            if (_podOpen) { yield return Say("you", "c2_pod_empty"); yield break; }
            bool ok = false;
            yield return Keypad("keypad.c2_pod", _cfg.puzzles.podCode, "c2_pod_wrong", r => ok = r);
            if (!ok) yield break;
            _podOpen = true;
            yield return TakeOnce("item_drillbit", "c2_pod_open", Pose.Kneel0, Pose.Kneel1);
        }

        IEnumerator MarsDrill(Hotspot h)
        {
            if (_sim.State.flags.Contains("drill_ready")) { yield return ActionMenu(h, "look.c2_drill", new[] { "drill_ice" }); yield break; }
            if (!_inv.Has("item_drillbit")) { yield return Say("you", "c2_drill_need"); yield break; }
            yield return _player.Act(Pose.Work0, Pose.Work1, _cfg.presentation.actionSeconds);
            _inv.Remove("item_drillbit");
            _sim.SetFlag("drill_ready");
            _sfx.Play("good");
            RefreshAll();
            yield return Say("orbi", "c2_drill_ready");
            yield return Card("ice");
        }

        IEnumerator CabinetModal()
        {
            if (_sim.State.flags.Contains("catalyst_installed")) { yield return Say("you", "c2_cabinet_done"); yield break; }
            string pick = null;
            _ui.ShowModal(p =>
            {
                p.style.width = 980;
                _ui.Title(p, _loc.T("cabinet.title"));
                p.Add(_ui.L(_loc.T("cabinet.body"), 24, false, GameUI.CDim));
                foreach (var c in _cfg.puzzles.catalysts)
                {
                    string id = c;
                    bool held = _inv.Has(id);
                    var b = _ui.B(_loc.T("item." + id) + (held ? "  " + _loc.T("cabinet.held") : ""), () => { pick = id; _ui.CloseModal(); }, 28);
                    b.SetEnabled(!held);
                    p.Add(b);
                }
            });
            yield return WaitModal();
            if (pick == null) yield break;
            foreach (var c in _cfg.puzzles.catalysts) _inv.Remove(c);
            yield return _player.Act(Pose.Work0, Pose.Work1, 0.5f);
            _inv.Add(pick);
            yield return Say("you", "c2_cabinet_take", NameOf(pick));
        }
    }
}
