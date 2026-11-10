// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// Chapter 2, shared by every route: survival loop, sleep and night report, forecasts,
    /// real-time daytime emergencies, alarms, the goal console. The route files fill in the world.
    /// </summary>
    public partial class GameFlow
    {
        SurvivalSim _sim;
        GameConfig _rc;
        IncidentDef _incident;
        float _incidentLeft;
        readonly HashSet<int> _visited = new HashSet<int>();
        SpriteRenderer _sky;
        bool _radWarned;

        IEnumerator StartChapter2(string route)
        {
            _route = route;
            _chapter = 2;
            _busy = true;
            _incident = null;
            _radWarned = false;
            _rc = _cfg.ForRoute(route);
            _inv.Remove("item_patchkit");
            int seed = SeedOverride >= 0 ? SeedOverride : System.Environment.TickCount & 0x7fffffff;
            var bonus = SurvivalSim.CarryOver(_cfg.chapter1, _cabinO2, _stowed);
            Settings.SaveCheckpoint(route, _cabinO2, _stowed);
            _sfx.Loop("hum", true);
            _sim = new SurvivalSim(_rc, seed, bonus);
            foreach (var flag in SurvivalSim.SupplyFlags(_cfg.chapter1, _stowed)) _sim.SetFlag(flag);
            ClearWorld();
            BuildShip(true);
            switch (route)
            {
                case "mars": BuildMarsOutside(); break;
                case "moon": BuildMoonOutside(); break;
                default: BuildTitanOutside(); break;
            }
            _player.SetSuit(false);
            _visited.Clear();
            _visited.Add(RQuarters);
            PlaceInRoom(RQuarters, 1.2f);
            _sim.BeginFirstSol();
            RefreshAll();
            _ui.FadeText(_loc.T($"ch2.{route}.intro_card"));
            yield return Fade(1f, 0f);
            yield return Wait(2.4f);
            _ui.FadeText(null);
            yield return Fade(0f, 0.9f);
            ShowCarryReport(bonus);
            yield return WaitModal();
            switch (route)
            {
                case "mars": yield return MarsIntro(); break;
                case "moon": yield return MoonIntro(); break;
                default: yield return TitanIntro(); break;
            }
            yield return Say("orbi", "c2_rad_intro." + route);
            yield return Card("rad_dose");
            var major = _sim.MajorEvent();
            if (major != null)
                yield return Say("orbi", "c2_major_warning", _loc.T($"event.{major.id}.name"), major.night, _loc.T($"event.{major.id}.prevent"));
            yield return MorningForecast();
            yield return ScienceNews();
            yield return MorningDilemma();
            _busy = false;
            RefreshAll();
        }

        void ShowCarryReport(ResourceDelta bonus)
        {
            _ui.ShowModal(p =>
            {
                p.style.width = 980;
                _ui.Title(p, _loc.T("carry.title"));
                p.Add(_ui.L(_loc.T("carry.o2", Mathf.RoundToInt(_cabinO2), (bonus.o2 >= 0 ? "+" : "") + bonus.o2), 26, true, bonus.o2 >= 0 ? GameUI.CGreen : GameUI.CRed));
                if (_stowed.Count == 0) p.Add(_ui.L(_loc.T("carry.stowed_none"), 24, false, GameUI.CAmber));
                else
                {
                    p.Add(_ui.L(_loc.T("carry.stowed", _stowed.Count, StowSlots), 24, true, GameUI.CGreen));
                    foreach (var id in _stowed) p.Add(_ui.L("· " + _loc.T($"sup.{id}.name") + " — " + _loc.T($"sup.{id}.desc"), 22, false, GameUI.CText));
                }
                p.Add(_ui.L(_loc.T("carry.route." + _route), 24, false, GameUI.CDim));
            });
        }

        // ------------------------------------------------------------------ per-frame pressure

        void TickChapter2()
        {
            if (_sim == null) return;
            var s = _sim.State;
            var c = _rc.survival;
            bool danger = s.outcome == SimOutcome.Ongoing && (s.o2 <= c.warnThreshold || s.water <= c.warnThreshold || s.food <= c.warnThreshold || s.power <= 0);
            _sfx.Loop("heart", danger);
            _sfx.Loop("siren", _incident != null);

            if (_incident != null && !_ui.ScreenOpen)
            {
                _incidentLeft -= Time.deltaTime;
                var spot = _world.Find(_incident.hotspot);
                string where = spot != null ? _loc.T(_world.rooms[spot.room].nameKey) : "";
                _ui.SetIncident(_loc.T($"incident.{_incident.id}.alarm", Mathf.CeilToInt(Mathf.Max(0, _incidentLeft)), where), _incidentLeft / _incident.seconds);
                if (spot != null) spot.SetHighlight(true, 0.5f + 0.5f * Mathf.Sin(Time.time * 12));
                if (_incidentLeft <= 0) IncidentMissed();
            }
            else _ui.SetIncident(null, 0);

            // weather you can see: the outside darkens as the big event gets close
            if (_sky != null)
            {
                var major = _sim.MajorEvent();
                float k = major == null ? 0 : Mathf.Clamp01((s.sol - 1f) / Mathf.Max(1, major.night - 1));
                _sky.color = Color.Lerp(Color.white, new Color(0.55f, 0.5f, 0.5f), k * 0.7f);
            }
        }

        void StartIncident(IncidentDef i)
        {
            _incident = i;
            _incidentLeft = i.seconds;
            _sfx.Play("alarm");
            _rig.Shake(0.4f, 0.08f);
            _ui.Flash(new Color(1f, 0.1f, 0.1f, 0.35f));
        }

        void IncidentMissed()
        {
            var i = _incident;
            _incident = null;
            _sim.ResolveIncident(i, false);
            _sfx.Play("bad");
            _ui.Toast(_loc.T($"incident.{i.id}.missed") + "   " + DeltaText(i.penalty, 0), GameUI.CRed);
            RefreshAll();
            if (_sim.State.outcome != SimOutcome.Ongoing) StartCoroutine(GameOver(_sim.State.outcome));
        }

        IEnumerator FixIncident(Hotspot h)
        {
            var i = _incident;
            _incident = null;
            _ui.SetIncident(null, 0);
            yield return _player.Act(Pose.Work0, Pose.Work1, _cfg.presentation.actionSeconds);
            _sim.ResolveIncident(i, true);
            _sfx.Play("good");
            _ui.Toast(_loc.T($"incident.{i.id}.fixed"), GameUI.CGreen);
            yield return Say("orbi", $"incident.{i.id}.fixed");
        }

        // ------------------------------------------------------------------ objective & HUD

        void Ch2Refresh()
        {
            if (_sim == null) return;
            var s = _sim.State;
            var sb = new StringBuilder();
            void Line(bool done, string text) => sb.Append(done ? "[OK] " : "[  ] ").Append(text).Append('\n');
            switch (_route)
            {
                case "mars": MarsObjectives(Line); break;
                case "moon": MoonObjectives(Line); break;
                default: TitanObjectives(Line); break;
            }
            if (_sim.ScienceTotal > 0) Line(_sim.ScienceDone >= _sim.ScienceTotal, _loc.T("obj.science", _sim.ScienceDone, _sim.ScienceTotal));
            Line(_sim.LaunchWindowOpen, _loc.T($"obj.{_route}.wait", _rc.survival.launchSol));
            Line(false, _loc.T($"obj.{_route}.goal"));
            _ui.SetObjective(_loc.T($"ch2.{_route}.title", s.sol), sb.ToString().TrimEnd());
            _ui.SetResources(s, _rc.survival, $"route.{_route}.progress");

            var ev = _sim.TonightEvent();
            var major = _sim.MajorEvent();
            string majorLine = major != null && s.sol < major.night
                ? "\n" + _loc.T("forecast.major_in", _loc.T($"event.{major.id}.name"), major.night - s.sol + 1) : "";
            if (s.sol >= _rc.survival.solsTotal) _ui.SetForecast(_loc.T("forecast.last_sol." + _route), true);
            else if (ev == null) _ui.SetForecast(_loc.T("forecast.calm") + majorLine, false);
            else
            {
                bool ok = _sim.IsPrevented(ev);
                _ui.SetForecast(_loc.T("forecast.tonight", _loc.T($"event.{ev.id}.name")) + "\n" +
                                _loc.T("forecast.prepare", _loc.T($"event.{ev.id}.prevent")) + "\n" +
                                _loc.T(ok ? "forecast.ready" : "forecast.not_ready") + majorLine, ev.major && !ok);
            }

            var w = _world;
            if (w == null || w.Find("s_board") == null) return;
            w.Find("s_board").SetSprite(Art.MissionBoard(ev != null && !_sim.IsPrevented(ev)));
            w.Find("s_o2gen").SetSprite(_route == "mars" ? Art.Moxie(false) : Art.Electrolyzer(false));
            w.Find("s_gauge").SetSprite(Art.Gauge(Mathf.Max(5, s.fuel), false));
            switch (_route)
            {
                case "mars": MarsRefresh(s); break;
                case "moon": MoonRefresh(s); break;
                default: TitanRefresh(s); break;
            }
            bool danger = s.o2 <= _rc.survival.warnThreshold || s.power <= 0;
            // red wash in danger; otherwise the lights dim as the batteries run low
            float low = Mathf.Clamp01(1f - s.power / (float)Mathf.Max(1, _rc.survival.warnThreshold + 15));
            foreach (var r in w.rooms)
            {
                if (r.alarmLight == null) continue;
                r.alarmLight.color = danger || _incident != null ? new Color(1f, 0.1f, 0.15f, 0.14f)
                                   : new Color(0.02f, 0.03f, 0.1f, r.kind == RoomKind.Outside ? 0f : 0.32f * low);
            }
        }

        string Ch2HintId()
        {
            var s = _sim.State;
            var c = _rc.survival;
            if (_incident != null) return "c2_incident";
            if (s.o2 <= c.warnThreshold) return _route + "_low_o2";
            if (s.water <= c.warnThreshold) return _route + "_low_water";
            if (s.power <= c.warnThreshold) return _route + "_low_power";
            string r = _route == "mars" ? MarsHint() : _route == "moon" ? MoonHint() : TitanHint();
            if (r != null) return r;
            var ev = _sim.TonightEvent();
            if (ev != null && !_sim.IsPrevented(ev)) return "c2_prepare";
            if (ScienceOpenNow().Count > 0 && s.ap > 0) return "c2_science";
            if (s.fuel < c.fuelTarget) return _route + "_progress";
            if (!_sim.LaunchWindowOpen) return "c2_wait";
            return _route + "_goal";
        }

        // ------------------------------------------------------------------ interactions

        IEnumerator Ch2EnterRoom(int room)
        {
            if (!_visited.Add(room)) yield break;
            if (room == ROutside)
            {
                switch (_route)
                {
                    case "mars": yield return MarsOutsideFirst(); break;
                    case "moon": yield return MoonOutsideFirst(); break;
                    default: yield return TitanOutsideFirst(); break;
                }
            }
        }

        string O2Action => _route == "mars" ? "moxie" : "electrolyze";

        IEnumerator Ch2Interact(Hotspot h)
        {
            if (_incident != null && h.id == _incident.hotspot) { yield return FixIncident(h); yield break; }

            IEnumerator routeHandler = _route == "mars" ? MarsInteract(h) : _route == "moon" ? MoonInteract(h) : TitanInteract(h);
            if (routeHandler != null) { yield return routeHandler; yield break; }

            switch (h.id)
            {
                case "s_bed": yield return BedModal(); break;
                case "s_food": yield return ActionMenu(h, "look.s_food.greens", new[] { "eat", "grow_greens" }); break;
                case "s_board":
                    yield return _player.Act(Pose.Work0, Pose.Work1, 0.6f);
                    yield return Say("orbi", "c2_board_1");
                    yield return Card("radiation");
                    yield return MorningForecast();
                    {
                        var open = ScienceOpenNow();
                        if (open.Count == 0) yield return Say("orbi", "c2_sci_none");
                        else yield return Say("orbi", "c2_sci_open", string.Join("  /  ", open.ConvertAll(a => _loc.T($"sci.{a.id}.ask"))));
                    }
                    break;
                case "s_scope":
                    if (_rc.actions.Exists(a => a.science && a.hotspot == h.id && a.minSol <= _sim.State.sol))
                        yield return ActionMenu(h, "look.s_scope.sci", new string[0]);
                    else yield return Say("you", "look2.s_scope");
                    break;
                case "s_o2gen":
                    yield return Card(_route == "mars" ? "moxie" : "electrolysis");
                    yield return ActionMenu(h, "look.s_o2gen." + _route, new[] { O2Action });
                    break;
                case "s_recycler":
                    yield return Card("water_recycle");
                    yield return ActionMenu(h, "look.s_recycler", new[] { "recycle" });
                    break;
                case "s_seals":
                    var seals = _route == "moon" ? new[] { "inspect_seals", "close_shutters", "set_heaters" }
                              : _route == "titan" ? new[] { "inspect_seals", "insulate" } : new[] { "inspect_seals" };
                    yield return ActionMenu(h, "look.s_seals." + _route, seals);
                    break;
                case "s_console":
                    if (_sim.CanLaunch(out var reason)) yield return GoalModal();
                    else yield return Say("orbi", reason == "reason.need_fuel" ? "reason.need_fuel." + _route : reason, _rc.survival.fuelTarget, _rc.survival.launchSol);
                    break;
                case "s_airlock":
                    if (_route == "titan" && !_titanSuitOk) { yield return TitanSuitChoice(); if (!_titanSuitOk) break; }
                    yield return GoRoom(ROutside);
                    break;
                default:
                    yield return Say("you", $"look2.{h.id}");
                    break;
            }
        }

        // ------------------------------------------------------------------ actions

        IEnumerator ActionMenu(Hotspot h, string lookKey, string[] actions, System.Action<string> onDone = null)
        {
            string chosen = null;
            var list = new List<string>(actions);
            foreach (var sa in _rc.actions)
                if (sa.science && sa.hotspot == h.id && sa.minSol <= _sim.State.sol && !list.Contains(sa.id)) list.Add(sa.id);
            _ui.ShowModal(p =>
            {
                p.style.width = 1000;
                _ui.Title(p, _loc.T(h.nameKey));
                if (lookKey != null) { var l = _ui.L(_loc.T(lookKey), 24, false, GameUI.CDim); l.style.marginBottom = 10; p.Add(l); }
                foreach (var a in list)
                {
                    string id = a;
                    var def = _rc.GetAction(id);
                    if (def == null) continue;
                    bool can = _sim.CanDo(id, out var reason);
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
                    var b = _ui.B(_loc.T("action." + id), () => { chosen = id; _ui.CloseModal(); }, 26, can);
                    if (def.science && can) b.style.color = new Color(0.75f, 1f, 0.55f);
                    b.style.width = 440;
                    b.SetEnabled(can);
                    row.Add(b);
                    var info = new VisualElement(); info.style.flexShrink = 1;
                    info.Add(_ui.L(DeltaText(def.delta, _sim.ApCost(def)), 22, true, GameUI.CText));
                    if (!can) info.Add(_ui.L(_loc.T(reason), 22, false, GameUI.CRed));
                    row.Add(info);
                    p.Add(row);
                }
            });
            yield return WaitModal();
            if (chosen != null) yield return DoAction(chosen);
            onDone?.Invoke(chosen);
        }

        IEnumerator DoAction(string id)
        {
            switch (id)
            {
                case "eat": yield return _player.Act(Pose.Eat0, Pose.Eat1, _cfg.presentation.actionSeconds, "step"); break;
                case "clean_panels": case "stow_panels": case "check_rtg": yield return _player.Act(Pose.Reach, Pose.Work0, _cfg.presentation.actionSeconds * 1.3f); break;
                case "drill_ice": case "sci_core": case "sci_ice_core": case "sci_ice_sample": case "sci_dunes": case "build_berm":
                    yield return _player.Act(Pose.Kneel0, Pose.Kneel1, _cfg.presentation.actionSeconds * 1.5f); break;
                case "grow_greens": case "sci_seismo": case "sci_weather": yield return _player.Act(Pose.Reach, Pose.Work0, _cfg.presentation.actionSeconds); break;
                case "charge_gate": yield return _player.Act(Pose.Reach, Pose.Work1, _cfg.presentation.actionSeconds * 1.4f); break;
                default: yield return _player.Act(Pose.Work0, Pose.Work1, _cfg.presentation.actionSeconds); break;
            }
            var def = _rc.GetAction(id);
            int cost = _sim.ApCost(def);
            if (!_sim.Do(id, out var reason))
            {
                _sfx.Play("bad");
                yield return Say("orbi", reason);
                yield break;
            }
            _sfx.Play("good");
            _ui.Toast(_loc.T("action." + id) + "   " + DeltaText(def.delta, cost), GameUI.CGreen);
            RefreshAll();
            if (!string.IsNullOrEmpty(def.card)) yield return Card(def.card);
            yield return AfterAction();
        }

        IEnumerator AfterAction()
        {
            RefreshAll();
            var s = _sim.State;
            if (s.outcome != SimOutcome.Ongoing) { yield return GameOver(s.outcome); yield break; }
            yield return RadCheck();
            var due = _sim.DueIncident();
            if (due != null && _incident == null) StartIncident(due);
            if (s.stamina <= 0)
            {
                yield return Say("orbi", "c2_collapse");
                yield return SleepRoutine(true);
                yield break;
            }
            if (s.ap <= 0) Note(s.sol >= _rc.survival.launchSol && _sim.CanLaunch(out _) ? "c2_day_over_launch" : "c2_day_over");
            else if (s.stamina < _rc.survival.lowStaminaThreshold) Note("c2_tired");
        }

        // ------------------------------------------------------------------ sleep / night

        IEnumerator BedModal()
        {
            bool sleep = false;
            _ui.ShowModal(p =>
            {
                p.style.width = 980;
                _ui.Title(p, _loc.T("bed.title", _sim.State.sol));
                var ev = _sim.TonightEvent();
                if (_sim.State.sol >= _rc.survival.solsTotal) p.Add(_ui.L(_loc.T("bed.last_sol." + _route), 26, true, GameUI.CRed));
                else if (ev == null) p.Add(_ui.L(_loc.T("bed.calm"), 26));
                else
                {
                    bool ok = _sim.IsPrevented(ev);
                    p.Add(_ui.L(_loc.T("forecast.tonight", _loc.T($"event.{ev.id}.name")), 26, true, ev.major ? GameUI.CRed : GameUI.CAmber));
                    p.Add(_ui.L(_loc.T("forecast.prepare", _loc.T($"event.{ev.id}.prevent")), 24));
                    p.Add(_ui.L(_loc.T(ok ? "forecast.ready" : "forecast.not_ready"), 26, true, ok ? GameUI.CGreen : GameUI.CRed));
                }
                var c = _rc.survival;
                p.Add(_ui.L(_loc.T("bed.night_use", c.nightO2, c.nightWater, c.nightFood, c.habPowerLoad), 22, false, GameUI.CDim));
                if (_incident != null) p.Add(_ui.L(_loc.T("bed.incident"), 24, true, GameUI.CRed));
                if (_sim.State.ap > 0) p.Add(_ui.L(_loc.T("bed.ap_left", _sim.State.ap), 22, false, GameUI.CAmber));
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginTop = 10;
                row.Add(_ui.B(_loc.T("bed.sleep"), () => { sleep = true; _ui.CloseModal(); }, 28, true));
                row.Add(_ui.B(_loc.T("bed.not_yet"), () => _ui.CloseModal(), 28));
                p.Add(row);
            }, closable: false);
            yield return WaitModal();
            if (sleep) yield return SleepRoutine(false);
        }

        IEnumerator SleepRoutine(bool collapsed)
        {
            if (_incident != null) IncidentMissed();
            if (_sim.State.outcome != SimOutcome.Ongoing) yield break;
            var bed = _world.Find("s_bed");
            if (!collapsed && _room == RQuarters)
            {
                _player.Lie(true);
                _player.transform.position = new Vector3(bed.transform.position.x + 1.2f, 0.75f, 1.0f);
            }
            yield return Fade(1f, collapsed ? 0.4f : 0.9f);
            var r = _sim.Sleep(collapsed);
            _player.Lie(false);
            _player.SetSuit(false);
            PlaceInRoom(RQuarters, 1.2f);
            if (r.outcome != SimOutcome.Ongoing)
            {
                RefreshAll();
                yield return GameOver(r.outcome);
                yield break;
            }
            _ui.FadeText(_loc.T("night.text." + _route, r.sol + 1));
            yield return Wait(1.2f);
            _ui.FadeText(null);
            RefreshAll();
            yield return Fade(0f, 0.7f);
            ShowNightReport(r);
            yield return WaitModal();
            yield return RadCheck();
            if (r.sol + 1 == _rc.survival.launchSol) yield return Say("orbi", "c2_window_open." + _route);
            yield return MorningForecast();
            yield return ScienceNews();
            yield return MorningDilemma();
        }

        void ShowNightReport(NightReport r)
        {
            _ui.ShowModal(p =>
            {
                p.style.width = 980;
                _ui.Title(p, _loc.T("report.title", r.sol));
                p.Add(_ui.L(_loc.T("report.use", r.o2Used, r.waterUsed, r.foodUsed, r.habLoad), 24));
                if (r.collapsed) p.Add(_ui.L(_loc.T("report.collapsed"), 24, true, GameUI.CAmber));
                if (r.eventId != null)
                {
                    bool saved = r.eventPrevented;
                    var l = _ui.L(_loc.T($"event.{r.eventId}." + (saved ? "saved" : "hit")), 26, true, saved ? GameUI.CGreen : GameUI.CRed);
                    l.style.marginTop = 8; l.style.marginBottom = 8;
                    p.Add(l);
                    if (!saved) p.Add(_ui.L(DeltaText(r.eventPenalty, 0), 24, true, GameUI.CRed));
                }
                else p.Add(_ui.L(_loc.T("report.calm"), 24, false, GameUI.CGreen));
                if (r.powerOut) p.Add(_ui.L(_loc.T("report.power_out", _rc.survival.powerOutO2Penalty), 26, true, GameUI.CRed));
                p.Add(_ui.L(_loc.T("report.dose", r.dose.ToString("0.0"), _sim.State.dose.ToString("0.0")), 22, r.dose >= 10f,
                    r.dose >= 10f ? GameUI.CRed : GameUI.CDim));
                p.Add(_ui.L(_loc.T("report.morning." + _route, r.morningSolar, Mathf.RoundToInt(_sim.State.efficiency * 100)), 24, false, GameUI.CAmber));
            });
        }

        IEnumerator MorningForecast()
        {
            var s = _sim.State;
            var ev = _sim.TonightEvent();
            // one line at most: the forecast panel on the HUD carries the details
            if (s.sol >= _rc.survival.solsTotal) { yield return Say("orbi", "forecast.orbi_last." + _route); yield break; }
            if (ev == null) { Note("forecast.orbi_calm"); yield break; }
            yield return Say("orbi", $"event.{ev.id}.forecast");
            if (ev.id == "solar_flare") yield return Card("radiation");
            if (ev.dose > 0 && !string.IsNullOrEmpty(_rc.survival.shieldFlag) && !_sim.Shielded) yield return Say("orbi", "moon_berm_tip");
            if (ev.id == "global_storm") yield return Card("dust_storm");
            if (ev.id == "moon_dust") yield return Card("moon_dust");
            if (ev.id == "methane_rain") yield return Card("titan_lakes");
        }

        // ------------------------------------------------------------------ science & radiation

        /// <summary>Science tasks Mission Control has asked for that can be done now (not done, unlocked).</summary>
        List<ActionDef> ScienceOpenNow()
        {
            var st = _sim.State;
            return _rc.actions.FindAll(a => a.science && a.minSol <= st.sol && !st.flags.Contains(a.setsFlag)
                && (string.IsNullOrEmpty(a.requiresFlag) || st.flags.Contains(a.requiresFlag)));
        }

        /// <summary>ORBI relays any science request that opens today.</summary>
        IEnumerator ScienceNews()
        {
            foreach (var a in _rc.actions)
                if (a.science && a.minSol == _sim.State.sol && !_sim.State.flags.Contains(a.setsFlag))
                {
                    _sfx.Play("blip");
                    Note("c2_sci_new", _loc.T($"sci.{a.id}.ask"));
                    yield return null;
                }
        }

        /// <summary>Some mornings ORBI asks for a decision with a cost either way.</summary>
        IEnumerator MorningDilemma()
        {
            var d = _sim.DueDilemma();
            if (d == null) yield break;
            string pick = null;
            _sfx.Play("blip");
            _ui.ShowModal(p =>
            {
                p.style.width = 1100;
                p.Add(_ui.L(_loc.T(d.random ? "dilemma.header_event" : "dilemma.header", _sim.State.sol), 22, true, d.random ? GameUI.CCyan : GameUI.CAmber));
                _ui.Title(p, _loc.T($"dilemma.{d.id}.title"));
                var body = _ui.L(_loc.T($"dilemma.{d.id}.body"), 24); body.style.marginBottom = 4; body.style.whiteSpace = WhiteSpace.Normal;
                p.Add(body);
                if (!d.random)
                {
                    var hint = _ui.L(_loc.T("dilemma.no_right"), 21, false, GameUI.CDim); hint.style.marginBottom = 8;
                    p.Add(hint);
                }
                foreach (var o in d.options)
                {
                    string id = o.id;
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
                    bool can = _sim.CanChoose(o, out var why);
                    var b = _ui.B(_loc.T($"dilemma.{d.id}.{id}"), () => { pick = id; _ui.CloseModal(); }, 25, false);
                    b.style.width = 560;
                    b.SetEnabled(can);
                    row.Add(b);
                    string info = DeltaText(o.delta, o.ap);
                    if (o.dose > 0) info += "  " + _loc.T("hud.dose", o.dose.ToString("0.0"));
                    var col = new VisualElement(); col.style.flexShrink = 1;
                    col.Add(_ui.L(info, 22, true, GameUI.CText));
                    if (!can) col.Add(_ui.L(_loc.T(why), 20, false, GameUI.CRed));
                    row.Add(col);
                    p.Add(row);
                }
            }, closable: false);
            yield return WaitModal();
            if (pick == null)
            {
                var allowed = d.options.FindAll(o => _sim.CanChoose(o, out _));
                pick = allowed[_shotDir != null ? 0 : allowed.Count - 1].id;
            }
            _sim.Choose(d.id, pick);
            RefreshAll();
            yield return Say("orbi", $"dilemma.{d.id}.{pick}.result");
            var chosen = d.options.Find(o => o.id == pick);
            if (!string.IsNullOrEmpty(chosen.card)) yield return Card(chosen.card);
            if (_sim.State.outcome != SimOutcome.Ongoing) yield return GameOver(_sim.State.outcome);
        }

        IEnumerator RadCheck()
        {
            if (_radWarned || _sim.State.dose <= _rc.survival.radSafe) yield break;
            _radWarned = true;
            _sfx.Play("bad");
            yield return Say("orbi", "c2_rad_warn", Mathf.RoundToInt(_rc.survival.radSafe));
        }

        // ------------------------------------------------------------------ goal / end

        IEnumerator GoalModal()
        {
            bool go = false;
            _ui.ShowModal(p =>
            {
                _ui.Title(p, _loc.T($"goal.{_route}.title"));
                p.Add(_ui.L(_loc.T($"goal.{_route}.body"), 26));
                p.Add(_ui.B(_loc.T($"goal.{_route}.go"), () => { go = true; _ui.CloseModal(); }, 32, true));
            });
            yield return WaitModal();
            if (!go) yield break;
            _sim.Launch();
            yield return WinSequence();
        }

        IEnumerator WinSequence()
        {
            _incident = null;
            _sfx.Loop("siren", false); _sfx.Loop("heart", false);
            yield return _player.Act(Pose.Work0, Pose.Work1, 0.8f);
            yield return Fade(1f, 0.8f);
            _chapter = 3;
            yield return PlayCutscene("win_" + _route);
            ShowEnd(true, SimOutcome.Won);
        }

        IEnumerator GameOver(SimOutcome o)
        {
            _incident = null;
            _sfx.Play("bad");
            _sfx.Play("alarm");
            yield return Fade(0.9f, 1f);
            ShowEnd(false, o);
        }

        static readonly Dictionary<string, int> RouteChapter = new Dictionary<string, int> { ["mars"] = 2, ["moon"] = 3, ["titan"] = 4 };

        void ShowEnd(bool won, SimOutcome o)
        {
            _chapter = 3;
            _sfx.Loop("siren", false); _sfx.Loop("heart", false);
            _ui.SetIncident(null, 0);
            _ui.SetHudVisible(false);
            _ui.CloseModal();
            _ui.FadeTo(0.92f, 0.3f);
            bool[] stars = _sim != null ? _sim.Stars() : new bool[3];
            int starCount = 0;
            foreach (var b in stars) if (b) starCount++;
            bool newBest = won && _shotDir == null && Settings.RecordStars(_route, starCount);
            void Build()
            {
                _ui.ShowScreen(root =>
                {
                    var box = new VisualElement();
                    box.style.alignItems = Align.Center; box.style.maxWidth = 1640;
                    box.style.backgroundColor = new Color(0.03f, 0.05f, 0.12f, 0.95f);
                    GameUI.Border(box, won ? GameUI.CCyan : GameUI.CRed, 3, 14);
                    box.style.paddingLeft = box.style.paddingRight = 44; box.style.paddingTop = box.style.paddingBottom = 24;
                    box.Add(_ui.L(_loc.T(won ? $"end.win.{_route}.title" : "end.lose.title"), 52, true, won ? GameUI.CCyan : GameUI.CRed));
                    string body = won ? _loc.T($"end.win.{_route}.body") : _loc.T($"end.lose.{_route}.{o}");
                    var b = _ui.L(body, 26); b.style.unityTextAlign = TextAnchor.MiddleCenter; b.style.marginTop = 6;
                    box.Add(b);
                    if (_sim != null) box.Add(BuildDebrief(won, stars, starCount, newBest));
                    var tip = _ui.L(_loc.T(won ? "end.win.next" : "end.lose.tip"), 22, false, GameUI.CDim);
                    tip.style.unityTextAlign = TextAnchor.MiddleCenter; tip.style.marginTop = 8;
                    box.Add(tip);
                    var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginTop = 14;
                    if (!won && Settings.HasSave) row.Add(_ui.B(_loc.T("end.retry_landing"), RestartFromCheckpoint, 28, true));
                    row.Add(_ui.B(_loc.T(won ? "end.again" : "end.restart"), Restart, 28, won));
                    row.Add(_ui.B(_loc.T("end.title"), RestartToTitle, 26));
                    row.Add(_ui.B(_loc.Current == Lang.TH ? "English" : "ภาษาไทย", () => { ToggleLang(); Build(); }, 24));
                    box.Add(row);
                    root.Add(box);
                });
            }
            Build();
        }

        /// <summary>The mission debrief: three stars, the numbers, and what to remember next time.</summary>
        VisualElement BuildDebrief(bool won, bool[] stars, int starCount, bool newBest)
        {
            var st = _sim.State;
            var c = _rc.survival;
            var cols = new VisualElement();
            cols.style.flexDirection = FlexDirection.Row; cols.style.marginTop = 14; cols.style.alignItems = Align.FlexStart;
            cols.style.backgroundColor = new Color(0.06f, 0.09f, 0.2f, 0.9f);
            GameUI.Border(cols, GameUI.CCyanD, 2, 10);
            cols.style.paddingLeft = cols.style.paddingRight = 22; cols.style.paddingTop = cols.style.paddingBottom = 14;

            // left: stars and numbers
            var left = new VisualElement(); left.style.width = 640; left.style.marginRight = 26;
            left.Add(_ui.L(_loc.T("debrief.title"), 28, true, GameUI.CAmber));
            string[] starText =
            {
                _loc.T("debrief.star.done"),
                _loc.T("debrief.star.health", Mathf.RoundToInt(c.radSafe)),
                _loc.T("debrief.star.science", _sim.ScienceTotal),
            };
            for (int i = 0; i < 3; i++)
            {
                var r = new VisualElement(); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginTop = 4;
                var img = new Image { image = Art.StarIcon(stars[i]), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                img.style.width = 40; img.style.height = 38; img.style.marginRight = 10;
                r.Add(img);
                var l = _ui.L(starText[i], 22, stars[i], stars[i] ? GameUI.CText : GameUI.CDim); l.style.flexShrink = 1;
                r.Add(l);
                left.Add(r);
            }
            int nights = 0, nightsOk = 0, inc = 0, incOk = 0;
            foreach (var e in st.log)
            {
                if (e.choice) continue;
                if (e.incident) { inc++; if (e.ok) incOk++; }
                else { nights++; if (e.ok) nightsOk++; }
            }
            void Stat(string text, bool bad = false) { var l = _ui.L(text, 21, false, bad ? GameUI.CRed : GameUI.CText); l.style.marginTop = 2; left.Add(l); }
            var gap = new VisualElement(); gap.style.height = 8; left.Add(gap);
            Stat(_loc.T("debrief.stat.sols", st.sol, c.solsTotal));
            Stat(_loc.T("debrief.stat.nights", nightsOk, nights), nightsOk < nights);
            Stat(_loc.T("debrief.stat.incidents", incOk, inc), incOk < inc);
            Stat(_loc.T("debrief.stat.dose", st.dose.ToString("0.0"), Mathf.RoundToInt(c.radLimit)), st.dose > c.radSafe);
            Stat(_loc.T("debrief.stat.science", _sim.ScienceDone, _sim.ScienceTotal), _sim.ScienceDone < _sim.ScienceTotal);
            int ch = RouteChapter.TryGetValue(_route, out var cc) ? cc : 0;
            int routeCards = 0, routeFound = 0;
            foreach (var card in _cfg.cards) if (card.chapter == ch) { routeCards++; if (_codex.Has(card.id)) routeFound++; }
            Stat(_loc.T("debrief.stat.cards", routeFound, routeCards));
            var best = _ui.L(_loc.T("debrief.best", Settings.BestStars(_route) + "/3") + (newBest ? "   " + _loc.T("debrief.new_best") : ""), 22, true, GameUI.CAmber);
            best.style.marginTop = 8;
            left.Add(best);
            cols.Add(left);

            // right: lessons
            var right = new VisualElement(); right.style.width = 800;
            right.Add(_ui.L(_loc.T("debrief.lessons"), 28, true, GameUI.CCyan));
            int shown = 0;
            foreach (var e in st.log)
            {
                if (e.ok || e.choice || shown >= 5) continue;
                shown++;
                string head = e.incident ? _loc.T("debrief.day", e.sol) : _loc.T("debrief.night", e.sol) + " · " + _loc.T($"event.{e.id}.name");
                var l = _ui.L(head + ": " + _loc.T($"debrief.lesson.{e.id}"), 21, false, GameUI.CText);
                l.style.marginTop = 6; l.style.whiteSpace = WhiteSpace.Normal;
                right.Add(l);
            }
            if (shown == 0 && won)
            {
                var l = _ui.L(_loc.T("debrief.perfect"), 22, true, GameUI.CGreen); l.style.marginTop = 6; l.style.whiteSpace = WhiteSpace.Normal;
                right.Add(l);
            }
            var picked = st.log.FindAll(e => e.choice);
            if (picked.Count > 0)
            {
                var l = _ui.L(_loc.T("debrief.choices", string.Join(", ", picked.ConvertAll(e => _loc.T($"dilemma.{e.id}.{e.option}")))), 21, false, GameUI.CText);
                l.style.marginTop = 10; l.style.whiteSpace = WhiteSpace.Normal;
                right.Add(l);
            }
            var missed = _rc.actions.FindAll(a => a.science && !st.flags.Contains(a.setsFlag));
            if (missed.Count > 0)
            {
                var l = _ui.L(_loc.T("debrief.sci_missed", string.Join(", ", missed.ConvertAll(a => _loc.T($"debrief.sci.{a.id}")))), 21, false, GameUI.CAmber);
                l.style.marginTop = 10; l.style.whiteSpace = WhiteSpace.Normal;
                right.Add(l);
            }
            if (routeFound < routeCards)
            {
                var l = _ui.L(_loc.T("debrief.cards_left", routeCards - routeFound), 21, false, GameUI.CDim);
                l.style.marginTop = 10; l.style.whiteSpace = WhiteSpace.Normal;
                right.Add(l);
            }
            cols.Add(right);
            return cols;
        }

        // ------------------------------------------------------------------ shared outside helpers

        void Backdrop(Sprite sky, Sprite far, Sprite near, Sprite ground, float skyScale = 8f)
        {
            var w = _world;
            _sky = new GameObject("sky").AddComponent<SpriteRenderer>();
            _sky.sprite = sky; _sky.sortingOrder = WorldBuilder.OrderBackdrop;
            _sky.transform.SetParent(_rig.transform, false);
            _sky.transform.localPosition = new Vector3(0, 0, 120); _sky.transform.localScale = Vector3.one * skyScale;
            float mid = (w.rooms[0].centerX + w.rooms[w.rooms.Count - 1].centerX) / 2f;
            var f = w.Quad(null, "ridgeFar", far, new Vector3(mid, -6f, 70), Quaternion.identity, WorldBuilder.OrderBackdrop + 2);
            f.transform.localScale = Vector3.one * 2.8f;
            var n = w.Quad(null, "ridgeNear", near, new Vector3(mid, -1.5f, 26), Quaternion.identity, WorldBuilder.OrderBackdrop + 4);
            n.transform.localScale = Vector3.one * 1.4f;
            // the skylines are seamless, so tile them wide enough that no edge shows on any screen
            foreach (var r in new[] { f, n })
            {
                r.drawMode = SpriteDrawMode.Tiled;
                r.size = new Vector2(r.sprite.rect.width / r.sprite.pixelsPerUnit * 3f, r.sprite.rect.height / r.sprite.pixelsPerUnit);
            }
            var g = w.Quad(null, "ground", ground, new Vector3(mid, -0.02f, 14f), Quaternion.Euler(90, 0, 0), WorldBuilder.OrderBackdrop + 6);
            // tile the ground at native size so its pixels match the props (no 5x stretched pixels)
            g.drawMode = SpriteDrawMode.Tiled;
            g.size = new Vector2(ground.rect.width / ground.pixelsPerUnit * 15f, ground.rect.height / ground.pixelsPerUnit * 10f);
        }

        void Particles(Room room, Sprite s, int count, Color tint)
        {
            var rng = new System.Random(4);
            for (int i = 0; i < count; i++)
            {
                var d = _world.Quad(room, "p", s, new Vector3(room.centerX - 8 + (float)rng.NextDouble() * 16, 0.3f + (float)rng.NextDouble() * 5.5f, -1f + (float)rng.NextDouble() * 3), Quaternion.identity, WorldBuilder.OrderFx);
                d.color = tint;
                _dust.Add(d.transform);
            }
        }

        /// <summary>Take an item from a container once, with a pose and a line.</summary>
        IEnumerator TakeOnce(string item, string line, Pose a = Pose.Work0, Pose b = Pose.Work1)
        {
            yield return _player.Act(a, b, 0.6f);
            _inv.Add(item);
            _sfx.Play("good");
            Note(line);
        }

        // ------------------------------------------------------------------ debug hooks for verification

        public void DebugSolvePuzzles()
        {
            foreach (var f in new[] { "catalyst_installed", "drill_ready", "has_hull_patch", "antenna_ready", "gate_found" }) _sim.SetFlag(f);
            if (_route == "mars") _sim.Do("repair_line", out _);
            _titanSuitOk = true;
            RefreshAll();
        }

        public void DebugGoRoom(int room)
        {
            if (_world != null && room < _world.rooms.Count)
            {
                _player.SetSuit(_world.rooms[room].kind == RoomKind.Outside);
                PlaceInRoom(room, 0f);
                RefreshAll();
            }
        }
    }
}
