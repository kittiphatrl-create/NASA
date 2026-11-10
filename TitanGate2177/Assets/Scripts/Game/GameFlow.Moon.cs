// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System.Collections;
using TitanGate.Core;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>
    /// Route: Earth (falls short, lands at the Moon's south pole). No air to use: oxygen comes from
    /// splitting water ice. Fix and aim the dish at Earth, send the signal, survive the long shadow, get rescued.
    /// </summary>
    public partial class GameFlow
    {
        bool _cableTaken, _moonPodOpen, _dishKitOn;

        void BuildMoonOutside()
        {
            Backdrop(Art.MoonSky(), Art.MoonRidge(0), Art.MoonRidge(1), Art.MoonGround(), 6f);
            var o = AddOutsideRoom("room.surface.moon");
            // Earth hangs low over the horizon here: the clue for aiming the dish
            var earth = _world.Quad(o, "earth", Art.Earth(), new Vector3(o.centerX + 19f, 3.0f, 45f), Quaternion.identity, WorldBuilder.OrderBackdrop + 3);
            earth.transform.localScale = Vector3.one * 1.0f;
            _world.Spot(o, "moon_solar", "spot.moon_solar", Art.SolarArray(false, false), -1.6f, 0, 2.2f, 0.4f);
            _world.Spot(o, "moon_dish", "spot.moon_dish", Art.Dish(false), 1.4f, 0, 1.9f, -1.0f);
            _world.Spot(o, "moon_crater", "spot.moon_crater", Art.CraterSite(false), 3.4f, 0.01f, 0.7f, -1.2f, WorldBuilder.OrderProp - 3);
            _world.Spot(o, "moon_pod", "spot.moon_pod", Art.CargoPod(false), 0.0f, 0, -0.4f, -1.3f, WorldBuilder.OrderPropFront);
        }

        IEnumerator MoonIntro()
        {
            yield return Say("orbi", "c2_land_1.moon");
            yield return Card("moon_south");
            yield return Say("orbi", "c2_land_2.moon");
            yield return Card("apollo");
            yield return Say("orbi", "c2_land_3.moon");
        }

        IEnumerator MoonOutsideFirst()
        {
            yield return Say("orbi", "c2_surface_first.moon");
            yield return Card("earth_sky");
        }

        void MoonObjectives(System.Action<bool, string> line)
        {
            var s = _sim.State;
            line(_inv.Has("item_antenna_kit") || _dishKitOn || s.flags.Contains("antenna_ready"), _loc.T("obj.moon.parts"));
            line(s.flags.Contains("antenna_ready"), _loc.T("obj.moon.antenna"));
            line(s.fuel >= _rc.survival.fuelTarget, _loc.T("obj.moon.signal", s.fuel, _rc.survival.fuelTarget));
        }

        string MoonHint()
        {
            var s = _sim.State;
            if (s.flags.Contains("antenna_ready")) return !s.flags.Contains("drill_ready") && s.water <= 40 ? "moon_drill" : null;
            if (_dishKitOn || _inv.Has("item_antenna_kit")) return "moon_aim";
            if (_inv.Has("item_feedhorn") && _inv.Has("item_cable")) return "moon_combine";
            return "moon_antenna";
        }

        void MoonRefresh(SurvivalState s)
        {
            var w = _world;
            w.Find("moon_solar")?.SetSprite(Art.SolarArray(false, s.efficiency < 0.75f));
            w.Find("moon_dish")?.SetSprite(Art.Dish(s.flags.Contains("antenna_ready")));
            w.Find("moon_crater")?.SetSprite(Art.CraterSite(s.flags.Contains("drill_ready")));
            w.Find("moon_pod")?.SetSprite(Art.CargoPod(_moonPodOpen));
            w.Find("s_cabinet")?.SetSprite(Art.Cabinet(false));
            w.Find("s_nav")?.SetSprite(Art.NavComputer(s.flags.Contains("antenna_ready")));
        }

        IEnumerator MoonInteract(Hotspot h)
        {
            switch (h.id)
            {
                case "s_cabinet":
                    if (_cableTaken) return Say("you", "moon_cabinet_empty");
                    _cableTaken = true;
                    return TakeOnce("item_cable", "moon_cable_take");
                case "s_nav": return ActionMenu(h, "look.moon_comms", new[] { "transmit" });
                case "moon_solar": return Seq(Card("solar"), ActionMenu(h, "look.moon_solar", new[] { "clean_panels", "build_berm" }));
                case "moon_pod": return MoonPod();
                case "moon_dish": return MoonDish();
                case "moon_crater": return MoonCrater(h);
            }
            return null;
        }

        IEnumerator MoonPod()
        {
            if (_moonPodOpen) { yield return Say("you", "moon_pod_empty"); yield break; }
            bool ok = false;
            yield return Keypad("keypad.moon_pod", _cfg.puzzles.moonPodCode, "moon_pod_wrong", r => ok = r);
            if (!ok) yield break;
            _moonPodOpen = true;
            yield return TakeOnce("item_feedhorn", "moon_pod_open", Pose.Kneel0, Pose.Kneel1);
        }

        IEnumerator MoonDish()
        {
            if (_sim.State.flags.Contains("antenna_ready")) { yield return Say("you", "moon_dish_done"); yield break; }
            if (!_dishKitOn)
            {
                if (!_inv.Has("item_antenna_kit"))
                {
                    yield return Say("you", _inv.Has("item_feedhorn") && _inv.Has("item_cable") ? "moon_dish_combine" : "moon_dish_broken");
                    yield break;
                }
                yield return _player.Act(Pose.Reach, Pose.Work0, _cfg.presentation.actionSeconds);
                _inv.Remove("item_antenna_kit");
                _dishKitOn = true;
                _sfx.Play("good");
                yield return Say("orbi", "moon_dish_kit_on");
            }
            string pick = null;
            yield return Choice("moon_aim.title", "moon_aim.body", new[] { "moon_aim.up", "moon_aim.horizon", "moon_aim.away" }, p => pick = p);
            if (pick == null) yield break;
            yield return _player.Act(Pose.Reach, Pose.Work1, 0.8f);
            if (pick == "moon_aim.horizon")
            {
                _sim.SetFlag("antenna_ready");
                _sfx.Play("good");
                RefreshAll();
                yield return Say("orbi", "moon_aim_ok");
            }
            else
            {
                _sfx.Play("bad");
                yield return Say("orbi", "moon_aim_wrong." + pick.Substring(9));
            }
        }

        IEnumerator MoonCrater(Hotspot h)
        {
            if (_sim.State.flags.Contains("drill_ready")) { yield return ActionMenu(h, "look.moon_crater", new[] { "drill_ice" }); yield break; }
            yield return Card("moon_ice");
            string pick = null;
            yield return Choice("moon_site.title", "moon_site.body", new[] { "moon_site.peak", "moon_site.crater", "moon_site.plain" }, p => pick = p);
            if (pick == null) yield break;
            if (pick == "moon_site.crater")
            {
                yield return _player.Act(Pose.Kneel0, Pose.Kneel1, _cfg.presentation.actionSeconds);
                _sim.SetFlag("drill_ready");
                _sfx.Play("good");
                RefreshAll();
                yield return Say("orbi", "moon_site_ok");
            }
            else
            {
                _sfx.Play("bad");
                yield return Say("orbi", "moon_site_wrong." + pick.Substring(10));
            }
        }
    }
}
