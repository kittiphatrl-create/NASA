// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-11
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// Chapter 2: seven days (168 h) at the Mars outpost on the landed Kestrel. Actions cost hours,
    /// Base Control splits the power between systems, the solar storm tests the shelter, Earth answers
    /// through relay passes, and the rescue ship checks in at hour 168.
    /// </summary>
    public partial class GameFlow
    {
        OutpostSim _sim;
        IncidentDef _incident;
        float _incidentLeft;
        readonly HashSet<int> _visited = new HashSet<int>();
        SpriteRenderer _sky;
        bool _radWarned, _baseTutorialShown;

        OutpostConfig OC => _cfg.outpost;

        IEnumerator StartChapter2(string route)
        {
            _route = "mars";
            _chapter = 2;
            _busy = true;
            _incident = null;
            _radWarned = false;
            _sealantTaken = _plateTaken = _podOpen = false;
            _inv.Remove("item_patchkit");
            int seed = SeedOverride >= 0 ? SeedOverride : System.Environment.TickCount & 0x7fffffff;
            var bonus = OutpostSim.CarryOver(_cfg.chapter1, _cabinO2, _stowed);
            Settings.SaveCheckpoint("mars", _cabinO2, _stowed);
            _sfx.Loop("hum", true);
            _sim = new OutpostSim(OC, seed, bonus, OutpostSim.SupplyFlags(_cfg.chapter1, _stowed));
            ClearWorld();
            BuildShip(true);
            BuildMarsOutside();
            _player.SetSuit(false);
            _visited.Clear();
            _visited.Add(RQuarters);
            PlaceInRoom(RQuarters, 1.2f);
            _ui.SetBaseButton(true);
            RefreshAll();
            _ui.FadeText(_loc.T("ch2.intro_card"));
            yield return Fade(1f, 0f);
            yield return Wait(2.4f);
            _ui.FadeText(null);
            yield return Fade(0f, 0.9f);
            ShowCarryReport(bonus);
            yield return WaitModal();
            yield return MarsIntro();
            yield return Say("orbi", "c2_rad_intro.mars");
            yield return Card("rad_dose");
            yield return Say("orbi", "c2_base_intro");
            _baseTutorialShown = true;
            BaseModal();
            yield return WaitModal();
            yield return ScienceNews();
            _busy = false;
            RefreshAll();
        }

        void ShowCarryReport(ResourceDelta bonus)
        {
            _ui.ShowModal(p =>
            {
                p.style.width = 980;
                _ui.Title(p, _loc.T("carry.title"));
                int air = Mathf.RoundToInt(bonus.air);
                p.Add(_ui.L(_loc.T("carry.air", Mathf.RoundToInt(_cabinO2), (air >= 0 ? "+" : "") + air), 26, true, air >= 0 ? GameUI.CGreen : GameUI.CRed));
                if (_stowed.Count == 0) p.Add(_ui.L(_loc.T("carry.stowed_none"), 24, false, GameUI.CAmber));
                else
                {
                    p.Add(_ui.L(_loc.T("carry.stowed", _stowed.Count, StowSlots), 24, true, GameUI.CGreen));
                    foreach (var id in _stowed) p.Add(_ui.L("· " + _loc.T($"sup.{id}.name") + " — " + _loc.T($"sup.{id}.desc"), 22, false, GameUI.CText));
                }
                p.Add(_ui.L(_loc.T("carry.plan", OC.hoursTotal), 24, false, GameUI.CDim));
            });
        }

        // ------------------------------------------------------------------ per-frame pressure

        void TickChapter2()
        {
            if (_sim == null) return;
            var s = _sim.State;
            bool danger = s.outcome == Outcome.Ongoing && (s.air <= OC.warnAir || s.supplies <= OC.warnSupplies || s.power <= OC.warnPower);
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

            // weather you can see: the sky darkens as the solar storm gets close, and at night
            if (_sky != null)
            {
                float storm = s.speDone ? 0 : Mathf.Clamp01(1f - _sim.HoursToSpe / (float)OC.spe.hour);
                float night = _sim.IsDaylight ? 0f : 0.55f;
                _sky.color = Color.Lerp(Color.white, new Color(0.35f, 0.32f, 0.4f), Mathf.Max(storm * 0.6f, night));
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
            if (_sim.State.outcome != Outcome.Ongoing) StartCoroutine(EndRun());
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
        }

        // ------------------------------------------------------------------ objectives, HUD, forecast

        void Ch2Refresh()
        {
            if (_sim == null) return;
            var s = _sim.State;
            var sb = new StringBuilder();
            void Line(bool done, string text) => sb.Append(done ? "[OK] " : "[  ] ").Append(text).Append('\n');
            Line(s.flags.Contains("beacon_repaired"), _loc.T("obj.beacon"));
            Line(s.coords == 2, _loc.T(s.coords == 1 ? "obj.coords_waiting" : "obj.coords"));
            Line(s.speDone || s.shelter >= 70, s.speDone ? _loc.T("obj.shelter_done") : _loc.T("obj.shelter", Mathf.RoundToInt(s.shelter)));
            Line(s.flags.Contains("greenhouse_ready"), _loc.T("obj.greenhouse", Mathf.Min(s.greenhouseHours, OC.greenhouseHoursToReady), OC.greenhouseHoursToReady));
            Line(_sim.ScienceDone >= _sim.ScienceTotal, _loc.T("obj.science", _sim.ScienceDone, _sim.ScienceTotal));
            Line(false, _loc.T("obj.survive", OC.hoursTotal / 24));
            _ui.SetObjective(_loc.T("ch2.title", _sim.Day), sb.ToString().TrimEnd());
            _ui.SetOutpost(_sim);
            _ui.SetForecast(ForecastText(out bool forecastDanger), forecastDanger);

            var w = _world;
            if (w == null || w.Find("s_board") == null) return;
            var ev = _sim.TonightEvent();
            w.Find("s_board").SetSprite(Art.MissionBoard(ev != null && !_sim.IsPrevented(ev)));
            w.Find("s_o2gen").SetSprite(Art.Moxie(false));
            w.Find("s_gauge").SetSprite(Art.Gauge(Mathf.Max(5, Mathf.RoundToInt(s.power)), false));
            MarsRefresh(s);
            bool danger = s.air <= OC.warnAir || s.power <= 0;
            float low = Mathf.Clamp01(1f - s.power / (OC.warnPower + 25f));
            foreach (var r in w.rooms)
            {
                if (r.alarmLight == null) continue;
                // red wash in danger; otherwise the lights dim as the batteries run low
                r.alarmLight.color = danger || _incident != null ? new Color(1f, 0.1f, 0.15f, 0.14f)
                                   : new Color(0.02f, 0.03f, 0.1f, r.kind == RoomKind.Outside ? 0f : 0.32f * low);
            }
        }

        string ForecastText(out bool danger)
        {
            danger = false;
            var s = _sim.State;
            var sb = new StringBuilder();
            var ev = _sim.TonightEvent();
            if (ev != null)
            {
                bool ok = _sim.IsPrevented(ev);
                sb.Append(_loc.T("forecast.tonight", _loc.T($"event.{ev.id}.name"))).Append('\n')
                  .Append(_loc.T("forecast.prepare", _loc.T($"event.{ev.id}.prevent"))).Append(' ')
                  .Append(_loc.T(ok ? "forecast.ready" : "forecast.not_ready")).Append('\n');
            }
            else sb.Append(_loc.T("forecast.calm")).Append('\n');
            if (!s.speDone)
            {
                int h = _sim.HoursToSpe;
                bool warned = s.messages.Exists(m => m.id == "earth_spe_warning" && m.delivered);
                if (warned)
                {
                    sb.Append(_loc.T("forecast.spe", h, Mathf.RoundToInt(_sim.Protection * 100))).Append('\n');
                    if (h <= 24 && _sim.Protection < 0.7f) danger = true;
                }
            }
            float need = _sim.PowerNeededUntilSunrise;
            if (need > s.power)
            {
                sb.Append(_loc.T("forecast.power_short", Mathf.RoundToInt(need), Mathf.RoundToInt(s.power))).Append('\n');
                if (!_sim.IsDaylight) danger = true;
            }
            sb.Append(_loc.T("forecast.relay", NextRelayClock().ToString("00"), OC.commDelayMinutes));
            return sb.ToString();
        }

        int NextRelayClock()
        {
            for (int h = 1; h <= 24; h++)
            {
                int c = _sim.ClockAt(_sim.State.hour + h);
                if (System.Array.IndexOf(OC.relayClocks, c) >= 0) return c;
            }
            return OC.relayClocks[0];
        }

        string Ch2HintId()
        {
            var s = _sim.State;
            if (_incident != null) return "c2_incident";
            if (s.air <= OC.warnAir + 10) return "out_low_air";
            if (s.supplies <= OC.warnSupplies + 10) return "out_low_supplies";
            if (_sim.PowerNeededUntilSunrise > s.power || s.power <= OC.warnPower) return "out_low_power";
            string r = MarsHint();
            if (r != null) return r;
            var ev = _sim.TonightEvent();
            if (ev != null && !_sim.IsPrevented(ev) && _sim.Clock >= 12) return "c2_prepare";
            if (!s.speDone && s.shelter < 70) return "out_shelter";
            if (s.flags.Contains("beacon_repaired") && s.coords == 0) return "out_coords";
            if (!s.flags.Contains("greenhouse_started")) return "out_greenhouse";
            if (ScienceOpenNow().Count > 0) return "c2_science";
            return "out_wait";
        }

        // ------------------------------------------------------------------ interactions

        IEnumerator Ch2EnterRoom(int room)
        {
            if (!_visited.Add(room)) yield break;
            if (room == ROutside) yield return MarsOutsideFirst();
        }

        IEnumerator Ch2Interact(Hotspot h)
        {
            if (_incident != null && h.id == _incident.hotspot) { yield return FixIncident(h); yield break; }
            var routeHandler = MarsInteract(h);
            if (routeHandler != null) { yield return routeHandler; yield break; }

            switch (h.id)
            {
                case "s_bed": yield return BedModal(); break;
                case "s_console": BaseModal(); yield return WaitModal(); break;
                case "s_board":
                    yield return _player.Act(Pose.Work0, Pose.Work1, 0.6f);
                    yield return Say("orbi", "c2_board_1");
                    yield return MorningForecast(true);
                    break;
                case "s_airlock": yield return GoRoom(ROutside); break;
                default:
                    if (OC.actions.Exists(a => a.hotspot == h.id)) yield return ActionMenu(h);
                    else yield return Say("you", $"look2.{h.id}");
                    break;
            }
        }

        void OnBase()
        {
            if (_chapter != 2 || _sim == null || _busy || _ui.Blocking) return;
            BaseModal();
        }

        // ------------------------------------------------------------------ actions

        /// <summary>Every action this hotspot offers (from config). Item shortcuts only show when you have the item.</summary>
        IEnumerator ActionMenu(Hotspot h, System.Action<string> onDone = null)
        {
            string chosen = null;
            var list = OC.actions.FindAll(a => a.hotspot == h.id
                && (!a.science || a.minDay <= _sim.Day)
                && (!a.consumesFlag || _sim.State.flags.Contains(a.requiresFlag))
                && (a.id != "repair_beacon_chip" || _sim.State.flags.Contains("data_chip"))
                && (a.id != "setup_greenhouse_seeds" || _sim.State.flags.Contains("seed_pack")));
            _ui.ShowModal(p =>
            {
                p.style.width = 1080;
                _ui.Title(p, _loc.T(h.nameKey));
                var look = _ui.L(_loc.T("look.out." + h.id), 23, false, GameUI.CDim); look.style.marginBottom = 8; look.style.whiteSpace = WhiteSpace.Normal;
                p.Add(look);
                foreach (var def in list)
                {
                    string id = def.id;
                    bool can = _sim.CanDo(id, out var reason);
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
                    var b = _ui.B(_loc.T("action." + id), () => { chosen = id; _ui.CloseModal(); }, 25, can);
                    b.style.width = 470;
                    b.SetEnabled(can);
                    if (def.science && can) b.style.color = new Color(0.75f, 1f, 0.55f);
                    row.Add(b);
                    var info = new VisualElement(); info.style.flexShrink = 1;
                    string txt = DeltaText(def.delta, _sim.HoursFor(def));
                    if (def.outside) txt += "  " + _loc.T("hud.outside");
                    info.Add(_ui.L(txt, 21, true, GameUI.CText));
                    if (!can) info.Add(_ui.L(_loc.T(reason), 20, false, GameUI.CRed));
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
                case "clean_panels": case "stow_panels": case "sci_weather": yield return _player.Act(Pose.Reach, Pose.Work0, _cfg.presentation.actionSeconds * 1.3f); break;
                case "drill_ice": case "sci_core": case "shelter_regolith":
                    yield return _player.Act(Pose.Kneel0, Pose.Kneel1, _cfg.presentation.actionSeconds * 1.5f); break;
                case "setup_greenhouse": case "setup_greenhouse_seeds": yield return _player.Act(Pose.Reach, Pose.Work0, _cfg.presentation.actionSeconds); break;
                default: yield return _player.Act(Pose.Work0, Pose.Work1, _cfg.presentation.actionSeconds); break;
            }
            var def = OC.GetAction(id);
            int hours = _sim.HoursFor(def);
            var r = _sim.Do(id);
            if (r == null) { _sfx.Play("bad"); yield break; }
            _sfx.Play("good");
            _ui.Toast(_loc.T("done." + id) + "   " + DeltaText(def.delta, hours), GameUI.CGreen);
            if (id == "send_coords") Note("c2_coords_sent", _sim.HoursUntilNextRescuePass());
            if (id == "ask_earth") Note("c2_asked_earth", _sim.HoursUntilReply(), OC.commDelayMinutes);
            RefreshAll();
            if (!string.IsNullOrEmpty(def.card)) yield return Card(def.card);
            yield return RunEvents(r);
            yield return AfterAction();
        }

        IEnumerator AfterAction()
        {
            RefreshAll();
            var s = _sim.State;
            if (s.outcome != Outcome.Ongoing) { yield return EndRun(); yield break; }
            var due = _sim.DueIncident();
            if (due != null && _incident == null) StartIncident(due);
            yield return RadCheck();
            if (_sim.IsNight) Note("out_bedtime");
            else if (s.energy < OC.lowEnergy) Note("c2_tired");
        }

        /// <summary>Report what a stretch of time did: Earth messages, the solar storm, power cuts, rescue pings.</summary>
        IEnumerator RunEvents(AdvanceResult r, bool fromSleep = false)
        {
            if (r == null) yield break;
            foreach (var e in r.events)
            {
                switch (e.kind)
                {
                    case LogKind.Message:
                        _sfx.Play("blip");
                        if (e.id == "earth_spe_warning")
                        {
                            yield return Say("sys", "msg.earth_spe_warning", OC.commDelayMinutes, _sim.DayAt(OC.spe.hour), _sim.ClockAt(OC.spe.hour).ToString("00"));
                            yield return Card("comm_delay");
                        }
                        else
                        {
                            yield return Say("sys", "msg.earth_hint", OC.commDelayMinutes);
                            var id = Ch2HintId();
                            yield return Say("orbi", $"hint.{id}.2");
                        }
                        break;
                    case LogKind.Spe:
                        yield return SpeSequence(e);
                        break;
                    case LogKind.Rescue:
                        _sfx.Play("good");
                        yield return Say("sys", "msg.coords_ack");
                        break;
                    case LogKind.PowerOut:
                        if (!fromSleep) { _sfx.Play("bad"); _ui.Toast(_loc.T("out_power_cut", _loc.T("system." + e.id)), GameUI.CRed); }
                        break;
                    case LogKind.Collapse:
                        yield return Say("orbi", "c2_collapse");
                        break;
                    case LogKind.WakeAlarm:
                        _sfx.Play("alarm");
                        yield return Say("orbi", e.id == "critical_air" ? "out_wake_critical" : "out_wake_alarm");
                        break;
                }
            }
            RefreshAll();
        }

        IEnumerator SpeSequence(LogEntry e)
        {
            _sfx.Play("alarm");
            _sfx.Play("rumble");
            _rig.Shake(1.2f, 0.12f);
            _ui.Flash(new Color(1f, 0.85f, 0.4f, 0.45f));
            yield return Say("sys", "spe.arrives");
            yield return Say("orbi", e.ok ? "spe.protected" : "spe.hit", e.value.ToString("0"), Mathf.RoundToInt(_sim.State.shelter));
            yield return Card("sep_2017");
            RefreshAll();
        }

        // ------------------------------------------------------------------ base control (power budget)

        void BaseModal()
        {
            _ui.ShowModal(p =>
            {
                var s = _sim.State;
                p.style.width = 1240;
                _ui.Title(p, _loc.T("base.title"));
                var intro = _ui.L(_loc.T("base.intro"), 22, false, GameUI.CDim); intro.style.whiteSpace = WhiteSpace.Normal; intro.style.marginBottom = 6;
                p.Add(intro);
                float solar = _sim.IsDaylight ? OC.solarPerHour * s.efficiency : 0f;
                p.Add(_ui.L(_loc.T("base.power_line", Mathf.RoundToInt(s.power), _sim.PowerDrawPerHour.ToString("0.0"), solar.ToString("0.0")), 23, true, GameUI.CAmber));
                float need = _sim.PowerNeededUntilSunrise;
                p.Add(_ui.L(_loc.T("base.night_line", Mathf.RoundToInt(need)), 22, need > s.power, need > s.power ? GameUI.CRed : GameUI.CGreen));
                foreach (var sys in OC.systems)
                {
                    string id = sys.id;
                    bool on = _sim.IsOn(id);
                    var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 4;
                    var b = _ui.B((on ? "■ " : "□ ") + _loc.T("system." + id) + "  " + _loc.T(on ? "base.on" : "base.off"), () =>
                    {
                        _sim.SetSystem(id, !_sim.IsOn(id));
                        _sfx.Play(_sim.IsOn(id) ? "open" : "close");
                        RefreshAll();
                        _ui.RebuildModal();
                    }, 24, on);
                    b.style.width = 420;
                    row.Add(b);
                    var d = _ui.L(_loc.T("system." + id + ".desc") + "  " + _loc.T("base.draw", sys.powerPerHour.ToString("0.0")), 20, false, GameUI.CText);
                    d.style.flexShrink = 1; d.style.whiteSpace = WhiteSpace.Normal;
                    row.Add(d);
                    p.Add(row);
                }
                var waitRow = new VisualElement(); waitRow.style.flexDirection = FlexDirection.Row; waitRow.style.marginTop = 10;
                waitRow.Add(_ui.B(_loc.T("base.wait1"), () => { _ui.CloseModal(); StartCoroutine(WaitHours(1)); }, 23));
                waitRow.Add(_ui.B(_loc.T("base.wait_event"), () => { _ui.CloseModal(); StartCoroutine(WaitUntilSomething()); }, 23));
                waitRow.Add(_ui.B(_loc.T("menu.sources"), () => { _ui.CloseModal(); ShowSources(); }, 21));
                p.Add(waitRow);
            });
        }

        IEnumerator WaitHours(int hours)
        {
            if (_busy || _sim.State.outcome != Outcome.Ongoing) yield break;
            _busy = true;
            var r = _sim.Advance(hours);
            yield return RunEvents(r);
            yield return AfterAction();
            _busy = false;
        }

        /// <summary>Let time pass until something happens: an Earth message, the storm, a power cut, nightfall.</summary>
        IEnumerator WaitUntilSomething()
        {
            if (_busy || _sim.State.outcome != Outcome.Ongoing) yield break;
            _busy = true;
            _ui.FadeTo(0.4f, 0.2f);
            int waited = 0;
            AdvanceResult last = null;
            while (waited < 24 && _sim.State.outcome == Outcome.Ongoing)
            {
                last = _sim.Advance(1);
                waited++;
                RefreshAll();
                yield return new WaitForSeconds(0.08f);
                if (last.events.Count > 0 || _sim.Clock == 22 || _sim.State.air <= OC.warnAir) break;
            }
            _ui.FadeTo(0f, 0.2f);
            Note("out_waited", waited);
            yield return RunEvents(last);
            yield return AfterAction();
            _busy = false;
        }

        // ------------------------------------------------------------------ sleep / night

        IEnumerator BedModal()
        {
            int choice = 0;
            string action = null;
            _ui.ShowModal(p =>
            {
                var s = _sim.State;
                p.style.width = 1080;
                _ui.Title(p, _loc.T("bed.title_time", _sim.Day, _sim.Clock.ToString("00")));
                var ev = _sim.TonightEvent();
                if (ev != null)
                {
                    bool ok = _sim.IsPrevented(ev);
                    p.Add(_ui.L(_loc.T("forecast.tonight", _loc.T($"event.{ev.id}.name")) + "  " + _loc.T(ok ? "forecast.ready" : "forecast.not_ready"), 24, true, ok ? GameUI.CGreen : GameUI.CAmber));
                }
                float need = _sim.PowerNeededUntilSunrise;
                p.Add(_ui.L(_loc.T("base.night_line", Mathf.RoundToInt(need)), 22, need > s.power, need > s.power ? GameUI.CRed : GameUI.CDim));
                if (_incident != null) p.Add(_ui.L(_loc.T("bed.incident"), 24, true, GameUI.CRed));
                string airWarn = _sim.SleepAirWarning;
                if (airWarn != null)
                {
                    var aw = _ui.L(_loc.T(airWarn, Mathf.RoundToInt(s.air), Mathf.RoundToInt(OC.critAir)), 24, true, airWarn == "bed.air_critical" ? GameUI.CRed : GameUI.CAmber);
                    aw.style.whiteSpace = WhiteSpace.Normal;
                    p.Add(aw);
                }
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginTop = 8; row.style.flexWrap = Wrap.Wrap;
                // a full night's sleep from 20:00; earlier in the day only a nap
                if (_sim.IsNight || _sim.Clock >= 20)
                    row.Add(_ui.B(_loc.T("bed.sleep_morning", _sim.HoursToMorning), () => { choice = 1; _ui.CloseModal(); }, 26, true));
                else row.Add(_ui.L(_loc.T("bed.too_early"), 22, false, GameUI.CDim));
                row.Add(_ui.B(_loc.T("bed.nap"), () => { choice = 2; _ui.CloseModal(); }, 24));
                p.Add(row);
                // the shelter is built around the bunk: water wall and radiation blanket
                foreach (var def in OC.actions.FindAll(a => a.hotspot == "s_bed" && (!a.consumesFlag || s.flags.Contains(a.requiresFlag))))
                {
                    string id = def.id;
                    bool can = _sim.CanDo(id, out var reason);
                    var r2 = new VisualElement(); r2.style.flexDirection = FlexDirection.Row; r2.style.alignItems = Align.Center;
                    var b = _ui.B(_loc.T("action." + id), () => { action = id; _ui.CloseModal(); }, 24, can);
                    b.style.width = 470; b.SetEnabled(can);
                    r2.Add(b);
                    r2.Add(_ui.L(DeltaText(def.delta, _sim.HoursFor(def)) + (can ? "" : "   " + _loc.T(reason)), 20, true, can ? GameUI.CText : GameUI.CRed));
                    p.Add(r2);
                }
            });
            yield return WaitModal();
            if (action != null) { yield return DoAction(action); yield break; }
            if (choice == 1) yield return SleepRoutine(_sim.HoursToMorning);
            else if (choice == 2) yield return SleepRoutine(2);
        }

        IEnumerator SleepRoutine(int hours, bool collapsed = false)
        {
            if (_incident != null) IncidentMissed();
            if (_sim.State.outcome != Outcome.Ongoing) yield break;
            var bed = _world.Find("s_bed");
            if (!collapsed && _room == RQuarters)
            {
                _player.Lie(true);
                _player.transform.position = new Vector3(bed.transform.position.x + 1.2f, 0.75f, 1.0f);
            }
            int dayBefore = _sim.Day;
            yield return Fade(1f, 0.9f);
            var r = _sim.Advance(hours, true);
            _player.Lie(false);
            _player.SetSuit(false);
            PlaceInRoom(RQuarters, 1.2f);
            RefreshAll();
            if (_sim.State.outcome != Outcome.Ongoing) { yield return EndRun(); yield break; }
            _ui.FadeText(_loc.T("night.wake", _sim.Day, _sim.Clock.ToString("00")));
            yield return Wait(1.0f);
            _ui.FadeText(null);
            yield return Fade(0f, 0.7f);
            ShowNightReport(r);
            yield return WaitModal();
            yield return RunEvents(r, true);
            if (_sim.State.outcome != Outcome.Ongoing) { yield return EndRun(); yield break; }
            yield return RadCheck();
            if (_sim.Day != dayBefore) yield return Morning();
        }

        IEnumerator Morning()
        {
            yield return MorningForecast(false);
            yield return ScienceNews();
            yield return MorningDilemma();
        }

        void ShowNightReport(AdvanceResult r)
        {
            _ui.ShowModal(p =>
            {
                var s = _sim.State;
                p.style.width = 1040;
                _ui.Title(p, _loc.T("report.title_hours", r.hours));
                p.Add(_ui.L(_loc.T("report.change", Signed(Mathf.RoundToInt(s.air - r.air0)), Signed(Mathf.RoundToInt(s.power - r.power0)),
                    Signed(Mathf.RoundToInt(s.supplies - r.supplies0)), Signed(Mathf.RoundToInt(s.energy - r.energy0))), 23));
                bool any = false;
                foreach (var e in r.events)
                {
                    if (e.kind == LogKind.Night)
                    {
                        any = true;
                        var l = _ui.L(_loc.T($"event.{e.id}." + (e.ok ? "saved" : "hit")), 25, true, e.ok ? GameUI.CGreen : GameUI.CRed);
                        l.style.marginTop = 6; l.style.whiteSpace = WhiteSpace.Normal;
                        p.Add(l);
                    }
                    else if (e.kind == LogKind.PowerOut)
                    {
                        any = true;
                        p.Add(_ui.L(_loc.T("out_power_cut", _loc.T("system." + e.id)), 23, true, GameUI.CRed));
                    }
                }
                if (!any) p.Add(_ui.L(_loc.T("report.calm"), 23, false, GameUI.CGreen));
                p.Add(_ui.L(_loc.T("report.dose", (s.dose - r.dose0).ToString("0.0"), s.dose.ToString("0.0")), 21, false, GameUI.CDim));
                p.Add(_ui.L(_loc.T("report.rescue_in", _sim.HoursLeft), 22, true, GameUI.CAmber));
            });
        }

        IEnumerator MorningForecast(bool always)
        {
            var ev = _sim.TonightEvent();
            if (ev != null) yield return Say("orbi", $"event.{ev.id}.forecast");
            else if (always) Note("forecast.orbi_calm");
            var s = _sim.State;
            bool warned = s.messages.Exists(m => m.id == "earth_spe_warning" && m.delivered);
            if (warned && !s.speDone && _sim.HoursToSpe <= 48)
                yield return Say("orbi", "c2_spe_soon", _sim.HoursToSpe, Mathf.RoundToInt(_sim.Protection * 100));
        }

        // ------------------------------------------------------------------ science, dilemmas, radiation

        List<OutpostAction> ScienceOpenNow()
        {
            var st = _sim.State;
            return OC.actions.FindAll(a => a.science && a.minDay <= _sim.Day && !st.flags.Contains(a.setsFlag)
                && (string.IsNullOrEmpty(a.requiresFlag) || st.flags.Contains(a.requiresFlag)));
        }

        IEnumerator ScienceNews()
        {
            foreach (var a in OC.actions)
                if (a.science && a.minDay == _sim.Day && !_sim.State.flags.Contains(a.setsFlag))
                {
                    _sfx.Play("blip");
                    Note("c2_sci_new", _loc.T($"sci.{a.id}.ask"));
                }
            yield return null;
        }

        IEnumerator MorningDilemma()
        {
            var d = _sim.DueDilemma();
            if (d == null) yield break;
            string pick = null;
            _sfx.Play("blip");
            _ui.ShowModal(p =>
            {
                p.style.width = 1100;
                p.Add(_ui.L(_loc.T(d.random ? "dilemma.header_event" : "dilemma.header", _sim.Day), 22, true, d.random ? GameUI.CCyan : GameUI.CAmber));
                _ui.Title(p, _loc.T($"dilemma.{d.id}.title"));
                var body = _ui.L(_loc.T($"dilemma.{d.id}.body"), 24); body.style.marginBottom = 8; body.style.whiteSpace = WhiteSpace.Normal;
                p.Add(body);
                foreach (var o in d.options)
                {
                    string id = o.id;
                    bool can = _sim.CanChoose(o, out var why);
                    var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
                    var b = _ui.B(_loc.T($"dilemma.{d.id}.{id}"), () => { pick = id; _ui.CloseModal(); }, 25, false);
                    b.style.width = 560; b.SetEnabled(can);
                    row.Add(b);
                    string info = DeltaText(o.delta, o.hours);
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
            var r = _sim.Choose(d.id, pick);
            RefreshAll();
            yield return Say("orbi", $"dilemma.{d.id}.{pick}.result");
            var chosen = d.options.Find(o => o.id == pick);
            if (!string.IsNullOrEmpty(chosen.card)) yield return Card(chosen.card);
            yield return RunEvents(r);
            if (_sim.State.outcome != Outcome.Ongoing) yield return EndRun();
        }

        IEnumerator RadCheck()
        {
            if (_radWarned || _sim.State.dose <= OC.radSafe) yield break;
            _radWarned = true;
            _sfx.Play("bad");
            yield return Say("orbi", "c2_rad_warn", Mathf.RoundToInt(OC.radSafe));
        }

        // ------------------------------------------------------------------ the end

        IEnumerator EndRun()
        {
            _incident = null;
            _sfx.Loop("siren", false); _sfx.Loop("heart", false);
            var o = _sim.State.outcome;
            if (o == Outcome.Rescued)
            {
                yield return Say("sys", "msg.rescue_arrives");
                yield return Fade(1f, 0.8f);
                _chapter = 3;
                yield return PlayCutscene("win_mars");
            }
            else
            {
                _sfx.Play("bad");
                yield return Fade(0.9f, 1f);
            }
            ShowEnd(o);
        }

        static readonly Dictionary<string, int> RouteChapter = new Dictionary<string, int> { ["mars"] = 2 };

        void ShowEnd(Outcome o)
        {
            _chapter = 3;
            _sfx.Loop("siren", false); _sfx.Loop("heart", false);
            _ui.SetIncident(null, 0);
            _ui.SetHudVisible(false);
            _ui.SetBaseButton(false);
            _ui.CloseModal();
            _ui.FadeTo(0.92f, 0.3f);
            bool won = o == Outcome.Rescued;
            bool alive = o == Outcome.Rescued || o == Outcome.Stranded;
            bool[] stars = _sim != null ? _sim.Stars() : new bool[3];
            int starCount = 0;
            foreach (var b in stars) if (b) starCount++;
            bool newBest = won && _shotDir == null && Settings.RecordStars("mars", starCount);
            string kind = won ? "rescued" : alive ? "stranded" : "failed";
            void Build()
            {
                _ui.ShowScreen(root =>
                {
                    var box = new VisualElement();
                    box.style.alignItems = Align.Center; box.style.maxWidth = 1640;
                    GameUI.PanelFrame(box, won || alive ? GameUI.CAmber : GameUI.CRed);
                    box.style.paddingLeft = box.style.paddingRight = 44; box.style.paddingTop = box.style.paddingBottom = 24;
                    box.Add(_ui.L(_loc.T($"end.{kind}.title"), 52, true, won ? GameUI.CCyan : alive ? GameUI.CAmber : GameUI.CRed));
                    var b = _ui.L(_loc.T(won ? "end.rescued.body" : alive ? "end.stranded." + (_sim?.StrandedReason ?? "body") : "end." + o, OC.rescuePassEveryHours), 26); b.style.unityTextAlign = TextAnchor.MiddleCenter; b.style.marginTop = 6;
                    b.style.whiteSpace = WhiteSpace.Normal;
                    box.Add(b);
                    if (_sim != null) box.Add(BuildDebrief(won, stars, newBest));
                    var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginTop = 14;
                    if (!won && Settings.HasSave) row.Add(_ui.B(_loc.T("end.retry_landing"), RestartFromCheckpoint, 28, true));
                    row.Add(_ui.B(_loc.T(won ? "end.again" : "end.restart"), Restart, 28, won));
                    row.Add(_ui.B(_loc.T("menu.sources"), ShowSources, 24));
                    row.Add(_ui.B(_loc.T("end.title"), RestartToTitle, 26));
                    row.Add(_ui.B(_loc.Current == Lang.TH ? "English" : "ภาษาไทย", () => { ToggleLang(); Build(); }, 24));
                    box.Add(row);
                    root.Add(box);
                });
            }
            Build();
        }

        /// <summary>The mission debrief: three stars, the numbers, and what to remember next time.</summary>
        VisualElement BuildDebrief(bool won, bool[] stars, bool newBest)
        {
            var st = _sim.State;
            var cols = new VisualElement();
            cols.style.flexDirection = FlexDirection.Row; cols.style.marginTop = 14; cols.style.alignItems = Align.FlexStart;
            cols.style.backgroundColor = new Color(0.06f, 0.09f, 0.2f, 0.9f);
            GameUI.Border(cols, GameUI.CCyanD, 2, 10);
            cols.style.paddingLeft = cols.style.paddingRight = 22; cols.style.paddingTop = cols.style.paddingBottom = 14;

            var left = new VisualElement(); left.style.width = 640; left.style.marginRight = 26;
            left.Add(_ui.L(_loc.T("debrief.title"), 28, true, GameUI.CAmber));
            string[] starText =
            {
                _loc.T("debrief.star.rescued"),
                _loc.T("debrief.star.health", Mathf.RoundToInt(OC.radSafe)),
                _loc.T("debrief.star.tasks", _sim.ScienceTotal),
            };
            for (int i = 0; i < 3; i++)
            {
                var r = new VisualElement(); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginTop = 4;
                var img = new Image { image = Art.StarIcon(stars[i]), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                img.style.width = 40; img.style.height = 38; img.style.marginRight = 10;
                r.Add(img);
                var l = _ui.L(starText[i], 22, stars[i], stars[i] ? GameUI.CText : GameUI.CDim); l.style.flexShrink = 1; l.style.whiteSpace = WhiteSpace.Normal;
                r.Add(l);
                left.Add(r);
            }
            int nights = 0, nightsOk = 0, inc = 0, incOk = 0;
            LogEntry spe = null;
            foreach (var e in st.log)
            {
                if (e.kind == LogKind.Night) { nights++; if (e.ok) nightsOk++; }
                else if (e.kind == LogKind.Incident) { inc++; if (e.ok) incOk++; }
                else if (e.kind == LogKind.Spe) spe = e;
            }
            void Stat(string text, bool bad = false) { var l = _ui.L(text, 21, false, bad ? GameUI.CRed : GameUI.CText); l.style.marginTop = 2; left.Add(l); }
            var gap = new VisualElement(); gap.style.height = 8; left.Add(gap);
            Stat(_loc.T("debrief.stat.hours", st.hour, OC.hoursTotal));
            Stat(_loc.T("debrief.stat.nights", nightsOk, nights), nightsOk < nights);
            Stat(_loc.T("debrief.stat.incidents", incOk, inc), incOk < inc);
            if (spe != null) Stat(_loc.T("debrief.stat.spe", spe.value.ToString("0")), !spe.ok);
            Stat(_loc.T("debrief.stat.dose", st.dose.ToString("0.0"), Mathf.RoundToInt(OC.radLimit)), st.dose > OC.radSafe);
            Stat(_loc.T("debrief.stat.science", _sim.ScienceDone, _sim.ScienceTotal), _sim.ScienceDone < _sim.ScienceTotal);
            Stat(_loc.T(st.flags.Contains("greenhouse_ready") ? "debrief.stat.greenhouse_yes" : "debrief.stat.greenhouse_no"), !st.flags.Contains("greenhouse_ready"));
            int routeCards = 0, routeFound = 0;
            foreach (var card in _cfg.cards) { routeCards++; if (_codex.Has(card.id)) routeFound++; }
            Stat(_loc.T("debrief.stat.cards", routeFound, routeCards));
            var best = _ui.L(_loc.T("debrief.best", Settings.BestStars("mars") + "/3") + (newBest ? "   " + _loc.T("debrief.new_best") : ""), 22, true, GameUI.CAmber);
            best.style.marginTop = 8;
            left.Add(best);
            cols.Add(left);

            var right = new VisualElement(); right.style.width = 800;
            right.Add(_ui.L(_loc.T("debrief.lessons"), 28, true, GameUI.CCyan));
            int shown = 0;
            void Lesson(string text, Color c)
            {
                var l = _ui.L(text, 21, false, c); l.style.marginTop = 6; l.style.whiteSpace = WhiteSpace.Normal; right.Add(l);
            }
            if (spe != null && !spe.ok) { Lesson(_loc.T("debrief.lesson.spe"), GameUI.CText); shown++; }
            if (st.coords != 2) { Lesson(_loc.T("debrief.lesson.coords"), GameUI.CText); shown++; }
            foreach (var e in st.log)
            {
                if (e.ok || shown >= 5 || (e.kind != LogKind.Night && e.kind != LogKind.Incident)) continue;
                shown++;
                string head = e.kind == LogKind.Incident ? _loc.T("debrief.day", _sim.DayAt(e.hour)) : _loc.T("debrief.night", _sim.NightAt(e.hour)) + " · " + _loc.T($"event.{e.id}.name");
                Lesson(head + ": " + _loc.T($"debrief.lesson.{e.id}"), GameUI.CText);
            }
            if (shown == 0 && won) Lesson(_loc.T("debrief.perfect"), GameUI.CGreen);
            var picked = st.log.FindAll(e => e.kind == LogKind.Choice);
            if (picked.Count > 0) Lesson(_loc.T("debrief.choices", string.Join(", ", picked.ConvertAll(e => _loc.T($"dilemma.{e.id}.{e.option}")))), GameUI.CText);
            var missed = OC.actions.FindAll(a => a.science && !st.flags.Contains(a.setsFlag));
            if (missed.Count > 0) Lesson(_loc.T("debrief.sci_missed", string.Join(", ", missed.ConvertAll(a => _loc.T($"debrief.sci.{a.id}")))), GameUI.CAmber);
            Lesson(_loc.T("debrief.data_note"), GameUI.CDim);
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
            foreach (var r in new[] { f, n })
            {
                r.drawMode = SpriteDrawMode.Tiled;
                r.size = new Vector2(r.sprite.rect.width / r.sprite.pixelsPerUnit * 3f, r.sprite.rect.height / r.sprite.pixelsPerUnit);
            }
            var g = w.Quad(null, "ground", ground, new Vector3(mid, -0.02f, 14f), Quaternion.Euler(90, 0, 0), WorldBuilder.OrderBackdrop + 6);
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

        /// <summary>Take an item from a container once, with a pose and a short note.</summary>
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
            foreach (var f in new[] { "catalyst_installed", "drill_ready", "has_hull_patch" }) _sim.SetFlag(f);
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
