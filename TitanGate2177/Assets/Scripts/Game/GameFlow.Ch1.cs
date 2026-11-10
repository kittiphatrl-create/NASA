// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System.Collections;
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// Chapter 1: the ship Kestrel. Morning shift, debris impact with a real-time oxygen leak,
    /// seal, pick any destination (each is its own Chapter 2), land with that world's procedure.
    /// </summary>
    public partial class GameFlow
    {
        readonly HashSet<string> _tasks = new HashSet<string>();
        bool _impact, _sealed, _fuelChecked, _manualRead, _lockerOpen, _floorWarned;
        float _cabinO2 = 100f, _cabinUiTimer;
        string _route;   // null until a destination is chosen

        IEnumerator StartChapter1()
        {
            _chapter = 1;
            _route = null;
            _cabinO2 = _cfg.chapter1.cabinO2Start;
            _stowed.Clear();
            _scramble = false;
            _ui.HideScreen();
            ClearWorld();
            BuildShip(false);
            BuildSpaceBackdrop();
            _player.gameObject.SetActive(true);
            _orbi.gameObject.SetActive(true);
            _player.SetSuit(false);
            PlaceInRoom(RCockpit, -1.2f);
            _ui.SetHudVisible(true);
            RefreshAll();
            _busy = true;
            yield return Fade(1f, 0f);
            yield return PlayCutscene("intro");
            _sfx.Loop("hum", true);
            yield return Fade(0f, 0.8f);
            yield return Say("orbi", "c1_intro_1");
            yield return Say("orbi", "c1_intro_2");
            yield return Say("orbi", "c1_intro_3");
            _busy = false;
            RefreshAll();
        }

        void BuildSpaceBackdrop()
        {
            var w = _world;
            var stars = new GameObject("stars").AddComponent<SpriteRenderer>();
            stars.sprite = Art.Starfield(); stars.sortingOrder = WorldBuilder.OrderBackdrop;
            stars.transform.SetParent(_rig.transform, false);
            stars.transform.localPosition = new Vector3(0, 0, 110); stars.transform.localScale = Vector3.one * 6f;
            w.Quad(null, "saturn", Art.Saturn(), new Vector3(1.5f, 4.6f, 60), Quaternion.identity, WorldBuilder.OrderBackdrop + 5);
            var marsDot = w.Quad(null, "mars", Art.MarsPlanet(), new Vector3(w.rooms[1].centerX + 2.5f, 5.2f, 50), Quaternion.identity, WorldBuilder.OrderBackdrop + 5);
            marsDot.transform.localScale = Vector3.one * 0.6f;
        }

        // ------------------------------------------------------------------ pressure: the leak

        /// <summary>Per-frame danger: chapter 1 cabin leak, chapter 2 incidents and alarms.</summary>
        void TickPressure()
        {
            AnimateSparks();
            if (_chapter == 1 && _impact && !_sealed)
            {
                if (!_ui.DialogOpen && !_ui.ScreenOpen)
                    _cabinO2 = Mathf.Max(_cfg.chapter1.cabinFloor, _cabinO2 - _cfg.chapter1.cabinDrainPerSec * Time.deltaTime);
                _cabinUiTimer -= Time.deltaTime;
                if (_cabinUiTimer <= 0) { _cabinUiTimer = 0.25f; _ui.SetCabin(_cabinO2, true); }
                _sfx.Loop("siren", true);
                _sfx.Loop("heart", _cabinO2 < 30f);
                if (!_floorWarned && _cabinO2 <= _cfg.chapter1.cabinFloor + 0.01f && !_busy)
                {
                    _floorWarned = true;
                    _ui.Say("orbi", "c1_masks");
                }
                var br = _world?.Find("s_breach");
                if (br != null) br.SetSprite(Art.Breach(false, (int)(Time.time * 6) % 2));
                return;
            }
            if (_chapter == 1 && _scramble) { TickScramble(); return; }
            if (_chapter == 2) { TickChapter2(); return; }
            _sfx.Loop("siren", false);
            _sfx.Loop("heart", false);
        }

        // ------------------------------------------------------------------ objective

        string Ch1Objective()
        {
            if (_scramble) return "c1_scramble";
            if (!_impact) return "c1_shift";
            if (!_sealed) return "c1_breach";
            if (!_fuelChecked) return "c1_fuel";
            if (_route == null) return "c1_nav";
            if (!_manualRead) return "c1_manual";
            return "c1_edl";
        }

        void Ch1Refresh()
        {
            string obj = Ch1Objective();
            string text = obj == "c1_shift"
                ? _loc.T("obj.c1_shift", _tasks.Count, _cfg.puzzles.shiftTasksBeforeImpact)
                : _loc.T("obj." + obj, _route != null ? _loc.T("route." + _route + ".name") : "");
            if (_scramble) text = _loc.T("obj.c1_scramble", _stowed.Count, StowSlots);
            _ui.SetObjective(_loc.T("ch1.title"), text);
            _ui.SetCabin(_cabinO2, _impact && !_sealed);

            var w = _world;
            if (w == null || w.Find("s_gauge") == null) return;
            int pct = _impact ? _cfg.puzzles.fuelGaugePercent : 64;
            w.Find("s_gauge").SetSprite(Art.Gauge(pct, _impact));
            w.Find("s_tank").SetSprite(Art.FuelTank(pct / 100f, _impact && !_sealed));
            w.Find("s_console").SetSprite(Art.Console("main", 50, 26, Pal.Cyan, _impact && !_sealed));
            w.Find("s_nav").SetSprite(Art.NavComputer(_route != null));
            w.Find("s_locker").SetSprite(Art.Locker(_lockerOpen));
            w.Find("s_shelf").SetSprite(Art.Shelf(!_manualRead));
            var br = w.Find("s_breach");
            if (br != null)
            {
                br.SetVisible(_impact);
                br.SetSprite(Art.Breach(_sealed, 0));
                br.SetInteractable(_impact && !_sealed);
            }
            foreach (var r in w.rooms)
                if (r.alarmLight != null) r.alarmLight.color = new Color(1f, 0.1f, 0.15f, _impact && !_sealed ? 0.16f : 0f);
        }

        // ------------------------------------------------------------------ interactions

        IEnumerator Ch1Interact(Hotspot h)
        {
            if (h.id.StartsWith("loose_")) { yield return GrabSupply(h); yield break; }
            if (_scramble) { _ui.Toast(_loc.T("c1_scramble_hurry"), GameUI.CAmber); yield break; }
            switch (h.id)
            {
                case "s_scope":
                    if (!_tasks.Contains("scan") && !_impact)
                    {
                        yield return _player.Act(Pose.Work0, Pose.Work1, _cfg.presentation.actionSeconds);
                        _tasks.Add("scan");
                        yield return Say("you", "c1_scope_task");
                        yield return Card("titan");
                        yield return AfterShiftTask();
                    }
                    else yield return Say("you", "c1_scope_look");
                    break;

                case "s_console":
                    if (!_tasks.Contains("log") && !_impact)
                    {
                        yield return _player.Act(Pose.Work0, Pose.Work1, _cfg.presentation.actionSeconds);
                        _tasks.Add("log");
                        yield return Say("you", "c1_log_task");
                        yield return Card("debris");
                        yield return AfterShiftTask();
                    }
                    else if (_impact && !_sealed) yield return Say("orbi", "c1_console_alarm");
                    else yield return Say("you", "c1_console_look");
                    break;

                case "s_gauge":
                    if (!_impact)
                    {
                        if (!_tasks.Contains("fuel"))
                        {
                            yield return _player.Act(Pose.Reach, Pose.Work0, _cfg.presentation.actionSeconds);
                            _tasks.Add("fuel");
                            yield return Say("you", "c1_gauge_task");
                            yield return AfterShiftTask();
                        }
                        else yield return Say("you", "c1_gauge_task");
                    }
                    else if (!_sealed) yield return Say("orbi", "c1_gauge_leaking");
                    else if (!_fuelChecked)
                    {
                        yield return _player.Act(Pose.Reach, Pose.Work0, 0.8f);
                        _fuelChecked = true;
                        yield return Say("you", "c1_gauge_low", _cfg.puzzles.fuelGaugePercent);
                        yield return Say("orbi", "c1_gauge_orbi");
                        yield return Card("deltav");
                    }
                    else yield return Say("you", "c1_gauge_low", _cfg.puzzles.fuelGaugePercent);
                    break;

                case "s_tank": yield return Say("you", _impact ? "c1_tanks_after" : "c1_tanks_look"); break;

                case "s_breach":
                    if (_inv.Has("item_patchkit"))
                    {
                        _inv.Remove("item_patchkit");
                        yield return _player.Act(Pose.Reach, Pose.Work0, _cfg.presentation.actionSeconds * 1.5f);
                        _sealed = true;
                        _sfx.Play("good");
                        _sfx.Loop("siren", false); _sfx.Loop("heart", false);
                        RefreshAll();
                        yield return Say("orbi", "c1_sealed", Mathf.RoundToInt(_cabinO2));
                        yield return Card("whipple");
                    }
                    else yield return Say("you", "c1_breach_need");
                    break;

                case "s_locker":
                    if (!_impact) yield return Say("you", "c1_locker_pre");
                    else if (_lockerOpen) yield return Say("you", "c1_locker_empty");
                    else
                    {
                        bool ok = false;
                        yield return Keypad("keypad.c1_locker", _cfg.puzzles.lockerCode, "c1_locker_wrong", r => ok = r);
                        if (ok)
                        {
                            _lockerOpen = true;
                            yield return _player.Act(Pose.Work0, Pose.Work1, 0.5f);
                            _inv.Add("item_patchkit");
                            _sfx.Play("good");
                            yield return Say("you", "c1_locker_open");
                        }
                    }
                    break;

                case "s_shelf":
                    if (_route == null) yield return Say("you", _manualRead ? "c1_shelf_look" : "c1_shelf_choose_first");
                    else if (!_manualRead)
                    {
                        yield return _player.Act(Pose.Reach, Pose.Idle0, 0.6f);
                        _manualRead = true;
                        RefreshAll();
                        yield return Say("you", "c1_manual_read." + _route);
                        yield return Card(_route == "mars" ? "edl" : _route == "moon" ? "moon_landing" : "huygens");
                    }
                    else yield return Say("you", "c1_shelf_look");
                    break;

                case "s_crates": yield return Say("you", "c1_crates_look"); break;
                case "s_airlock": yield return Say("orbi", "c1_airlock_space"); break;
                case "s_bed": yield return Say("you", _impact ? "c1_bed_no" : "c1_bed_look"); break;
                case "s_food": yield return Say("you", "c1_food_look"); break;
                case "s_toolbox": yield return Say("you", "c1_toolbox_look"); break;
                case "s_board": yield return Say("you", "c1_board_look"); break;
                case "s_o2gen": yield return Say("orbi", _impact && !_sealed ? "c1_o2gen_leak" : "c1_o2gen_look"); break;
                case "s_recycler": yield return Say("you", "c1_recycler_look"); break;
                case "s_cabinet": yield return Say("you", "c1_cabinet_look"); break;
                case "s_seals": yield return Say("orbi", _impact && !_sealed ? "c1_seals_leak" : "c1_seals_look"); break;
                case "s_reactor": yield return Say("orbi", "c1_reactor_look"); break;
                case "s_line": yield return Say("you", "c1_line_look"); break;

                case "s_nav":
                    if (!_impact) yield return Say("you", "c1_nav_pre");
                    else if (!_sealed || !_fuelChecked) yield return Say("orbi", "c1_nav_need_fuel");
                    else if (_route == null) yield return NavModal();
                    else yield return Say("you", "c1_nav_done", _loc.T("route." + _route + ".name"));
                    break;

                case "s_seat":
                    if (_route == null) yield return Say("you", _impact ? "c1_seat_need_nav" : "c1_seat_pre");
                    else if (!_manualRead) yield return Say("orbi", "c1_seat_need_manual");
                    else yield return EdlModal();
                    break;
            }
        }

        IEnumerator AfterShiftTask()
        {
            RefreshAll();
            if (_impact || _tasks.Count < _cfg.puzzles.shiftTasksBeforeImpact) yield break;
            yield return Wait(0.4f);
            yield return ImpactSequence();
        }

        IEnumerator ImpactSequence()
        {
            _rig.Shake(0.5f, 0.1f);
            yield return Wait(0.3f);
            yield return Fade(1f, 0.3f);
            yield return PlayCutscene("impact");
            _sfx.Loop("hum", true);
            _impact = true;
            RefreshAll();
            yield return Fade(0f, 0.4f);
            _sfx.Play("thud");
            _rig.Shake(1.0f, 0.2f);
            _ui.Flash(new Color(1f, 0.15f, 0.2f, 0.5f));
            _sfx.Play("alarm");
            RefreshAll();
            yield return Say("sys", "c1_impact_sys");
            yield return Say("orbi", "c1_impact_1");
            yield return Card("debris");
            yield return Say("orbi", "c1_impact_2");
            yield return Say("orbi", "c1_impact_3");
            RefreshAll();
        }

        // ------------------------------------------------------------------ puzzles

        /// <summary>A number keypad. The answer is always written on a knowledge card.</summary>
        IEnumerator Keypad(string promptKey, string code, string wrongKey, System.Action<bool> result)
        {
            string entry = "";
            bool solved = false;
            Label display = null, status = null;
            void Render() { if (display != null) display.text = entry.Length > 0 ? entry : "_"; }
            _ui.ShowModal(p =>
            {
                p.style.width = 760;
                _ui.Title(p, _loc.T("keypad.title"));
                p.Add(_ui.L(_loc.T(promptKey), 26));
                display = _ui.L("_", 64, true, GameUI.CAmber);
                display.style.unityTextAlign = TextAnchor.MiddleCenter;
                display.style.backgroundColor = new Color(0.02f, 0.03f, 0.07f);
                display.style.marginTop = 10; display.style.marginBottom = 10;
                p.Add(display);
                status = _ui.L("", 24, false, GameUI.CRed);
                p.Add(status);
                var grid = new VisualElement();
                grid.style.flexDirection = FlexDirection.Row; grid.style.flexWrap = Wrap.Wrap; grid.style.justifyContent = Justify.Center;
                grid.style.width = 520; grid.style.alignSelf = Align.Center;
                foreach (var k in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "OK" })
                {
                    string key = k;
                    var b = _ui.B(k == "C" ? _loc.T("keypad.clear") : (k == "OK" ? _loc.T("keypad.ok") : k), () =>
                    {
                        if (key == "C") { entry = ""; status.text = ""; }
                        else if (key == "OK")
                        {
                            if (Puzzles.CheckCode(entry, code)) { solved = true; _sfx.Play("good"); _ui.CloseModal(); return; }
                            _sfx.Play("bad");
                            status.text = _loc.T(wrongKey);
                            entry = "";
                        }
                        else if (entry.Length < 4) { entry += key; _sfx.Play("click"); }
                        Render();
                    }, 34, key == "OK");
                    b.style.width = 150; b.style.height = 72;
                    grid.Add(b);
                }
                p.Add(grid);
                Render();
            });
            yield return WaitModal();
            result(solved);
        }

        /// <summary>Generic choice modal: returns the picked option id (or null when closed).</summary>
        IEnumerator Choice(string titleKey, string bodyKey, string[] options, System.Action<string> picked, params object[] bodyArgs)
        {
            string chosen = null;
            _ui.ShowModal(p =>
            {
                p.style.width = 1000;
                _ui.Title(p, _loc.T(titleKey));
                if (bodyKey != null) { var b = _ui.L(_loc.T(bodyKey, bodyArgs), 24, false, GameUI.CDim); b.style.marginBottom = 8; p.Add(b); }
                foreach (var o in options)
                {
                    string id = o;
                    p.Add(_ui.B(_loc.T(id), () => { chosen = id; _ui.CloseModal(); }, 26));
                }
            });
            yield return WaitModal();
            picked(chosen);
        }

        IEnumerator NavModal()
        {
            string chosen = null;
            _ui.ShowModal(p =>
            {
                p.style.width = 1000;
                _ui.Title(p, _loc.T("nav.title"));
                p.Add(_ui.L(_loc.T("nav.fuel", _cfg.puzzles.fuelGaugePercent), 30, true, GameUI.CAmber));
                p.Add(_ui.L(_loc.T("nav.body"), 24, false, GameUI.CDim));
                foreach (var o in _cfg.puzzles.navOptions)
                {
                    string id = o.id;
                    bool reach = o.fuelNeeded <= _cfg.puzzles.fuelGaugePercent;
                    var b = _ui.B(_loc.T("nav.option", _loc.T("nav." + id), o.fuelNeeded) + "   " + _loc.T(reach ? "nav.direct" : "nav.not_direct"),
                        () => { chosen = id; _ui.CloseModal(); }, 26, reach);
                    p.Add(b);
                }
            });
            yield return WaitModal();
            if (chosen == null) yield break;

            // every destination is allowed: show what it will cost, then confirm
            bool go = false;
            _ui.ShowModal(p =>
            {
                p.style.width = 1000;
                _ui.Title(p, _loc.T("nav." + chosen));
                p.Add(_ui.L(_loc.T("nav.confirm." + chosen), 26));
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginTop = 12;
                row.Add(_ui.B(_loc.T("nav.go"), () => { go = true; _ui.CloseModal(); }, 28, true));
                row.Add(_ui.B(_loc.T("nav.back"), () => _ui.CloseModal(), 26));
                p.Add(row);
            }, closable: false);
            yield return WaitModal();
            if (!go) { yield return NavModal(); yield break; }

            yield return _player.Act(Pose.Work0, Pose.Work1, 0.6f);
            _route = Puzzles.RouteFor(_cfg.puzzles, chosen);
            _sfx.Play("good");
            RefreshAll();
            yield return Say("orbi", "c1_nav_ok." + _route);
            if (_route == "titan") yield return Card("gravity_assist");
        }

        IEnumerator EdlModal()
        {
            var edl = _cfg.GetRoute(_route).edl;
            var order = new List<string>();
            var steps = new List<string>(edl.order);
            steps.AddRange(edl.distractors);
            steps.Sort((a, b) => (a.GetHashCode() & 0xff).CompareTo(b.GetHashCode() & 0xff));
            string wrong = null;
            bool done = false;
            _ui.ShowModal(p =>
            {
                p.style.width = 1160;
                _ui.Title(p, _loc.T("edl.title." + _route));
                p.Add(_ui.L(_loc.T("edl.body"), 24, false, GameUI.CDim));
                var slots = new VisualElement(); slots.style.flexDirection = FlexDirection.Row; slots.style.marginTop = 12; slots.style.marginBottom = 12;
                for (int i = 0; i < edl.order.Count; i++)
                {
                    var s = new VisualElement();
                    s.style.flexGrow = 1; s.style.flexBasis = 0; s.style.height = 96; s.style.marginRight = 8;
                    s.style.backgroundColor = new Color(0.02f, 0.03f, 0.07f);
                    GameUI.Border(s, i < order.Count ? GameUI.CGreen : GameUI.CCyanD, 2, 8);
                    s.style.justifyContent = Justify.Center; s.style.alignItems = Align.Center;
                    var lb = _ui.L((i + 1) + ". " + (i < order.Count ? _loc.T("edl." + order[i]) : "..."), 21, true, i < order.Count ? GameUI.CGreen : GameUI.CDim);
                    lb.style.unityTextAlign = TextAnchor.MiddleCenter;
                    s.Add(lb);
                    slots.Add(s);
                }
                p.Add(slots);
                if (wrong != null)
                {
                    var wl = _ui.L(_loc.T($"edl.wrong.{_route}.{wrong}"), 24, false, GameUI.CRed);
                    wl.style.marginBottom = 8;
                    p.Add(wl);
                }
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap;
                foreach (var st in steps)
                {
                    string step = st;
                    var b = _ui.B(_loc.T("edl." + step), () =>
                    {
                        order.Add(step);
                        if (Puzzles.FirstWrongStep(order, edl.order) >= 0) { wrong = step; order.Clear(); _sfx.Play("bad"); }
                        else { wrong = null; _sfx.Play("click"); }
                        if (Puzzles.StepsComplete(order, edl.order)) { done = true; _ui.CloseModal(); return; }
                        _ui.RebuildModal();
                    }, 24);
                    b.SetEnabled(!order.Contains(step));
                    b.style.width = 520;
                    row.Add(b);
                }
                p.Add(row);
            });
            yield return WaitModal();
            if (done) yield return BeginScramble();
        }

        IEnumerator LandingSequence()
        {
            _sfx.Play("good");
            yield return Say("orbi", "c1_edl_ok." + _route);
            yield return _player.Act(Pose.Work0, Pose.Work1, 0.8f);
            yield return Fade(1f, 0.8f);
            yield return PlayCutscene("land_" + _route);
            yield return StartChapter2(_route);
        }

        /// <summary>Debug/verification: jump straight to a route's Chapter 2.</summary>
        public void DebugSkipTo(string route)
        {
            StopAllCoroutines();
            _ui.CloseModal();
            _ui.HideScreen();
            _busy = false;
            foreach (var c in new[] { "titan", "debris", "whipple", "deltav" }) _codex.Unlock(c);
            StartCoroutine(StartChapter2(route));
        }
    }
}
