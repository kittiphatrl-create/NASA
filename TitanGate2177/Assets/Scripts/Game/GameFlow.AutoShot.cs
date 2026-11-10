// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System.Collections;
using System.IO;
using TitanGate.Core;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>
    /// Verification mode: run the player with "-autoshot -route mars|moon|titan". It plays the route
    /// through the real click handlers and modal buttons, saves a screenshot of every screen to
    /// Captures/&lt;route&gt;/, logs every error and every check, then quits.
    /// </summary>
    public partial class GameFlow
    {
        bool _autoAdvance, _autoCloseModals;
        StreamWriter _autoLog;
        int _errors, _checksFailed;
        string _autoRoute = "mars", _shotDir;

        public static bool AutoShotRequested()
        {
            foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-autoshot") return true;
            return false;
        }

        void BeginAutoShotIfRequested()
        {
            if (!AutoShotRequested()) return;
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-route") _autoRoute = args[i + 1];
            _shotDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Captures", _autoRoute);
            Directory.CreateDirectory(_shotDir);
            _autoLog = new StreamWriter(Path.Combine(_shotDir, "autoshot-log.txt"), false);
            Application.logMessageReceived += (msg, st, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                {
                    _errors++;
                    _autoLog?.WriteLine($"[{type}] {msg}\n{st}");
                    _autoLog?.Flush();
                }
            };
            SeedOverride = 7;
            Application.runInBackground = true;
            StartCoroutine(AutoShot());
            StartCoroutine(AutoHelper());
        }

        void Log(string s) { _autoLog?.WriteLine(s); _autoLog?.Flush(); }

        void Check(bool ok, string what)
        {
            if (!ok) _checksFailed++;
            Log((ok ? "PASS " : "FAIL ") + what);
        }

        IEnumerator AutoHelper()
        {
            while (true)
            {
                if (_autoAdvance && _ui.DialogOpen) _ui.Advance();
                if (_autoCloseModals && _ui.ModalOpen) _ui.CloseModal();
                yield return new WaitForSeconds(0.2f);
            }
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForSeconds(0.35f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(_shotDir, name + ".png"));
            Log("shot " + name);
            yield return new WaitForSeconds(0.25f);
        }

        /// <summary>Advance dialogue (and close modals) until the game is idle.</summary>
        IEnumerator Settle()
        {
            _autoAdvance = true; _autoCloseModals = true;
            float t = 0;
            while ((_busy || _ui.DialogOpen || _ui.ModalOpen) && t < 25f) { t += Time.deltaTime; yield return null; }
            _autoAdvance = false; _autoCloseModals = false;
        }

        /// <summary>Advance dialogue until a modal that is not a knowledge card is open.</summary>
        IEnumerator UntilModal(float limit = 12f)
        {
            float t = 0;
            while (t < limit)
            {
                if (_ui.DialogOpen) _ui.Advance();
                if (_ui.ModalOpen)
                {
                    yield return new WaitForSeconds(0.25f);
                    if (_cardModalOpen) { _ui.CloseModal(); continue; }
                    yield break;
                }
                t += Time.deltaTime;
                yield return null;
            }
        }

        IEnumerator Click(string id, bool settle = true)
        {
            var h = _world.Find(id);
            if (h == null) { Check(false, "hotspot exists: " + id); yield break; }
            if (h.room != _room) { _player.SetSuit(_world.rooms[h.room].kind == RoomKind.Outside); PlaceInRoom(h.room, 0f); }
            float t = 0;
            while (_busy && t < 10) { t += Time.deltaTime; yield return null; }
            StartCoroutine(Interact(h));
            yield return null;
            if (settle) yield return Settle();
        }

        IEnumerator ClickToModal(string id)
        {
            yield return Click(id, false);
            yield return UntilModal();
        }

        void PressKeys(string code)
        {
            foreach (var ch in code) _ui.Press(ch.ToString());
            _ui.Press(_loc.T("keypad.ok"));
        }

        IEnumerator AutoShot()
        {
            _loc.Current = Lang.TH;
            yield return new WaitForSeconds(1.0f);
            string R = _autoRoute;
            if (R == "mars")
            {
                yield return new WaitForSeconds(1.2f);
                yield return Shot("00_menu_th");
                _ui.HideScreen();
                yield return Shot("00_keyart");
                BuildMainMenu();
                ShowSettings(); yield return Shot("00_settings_th"); _ui.CloseModal();
                ShowHowTo(); yield return Shot("00_howto_th"); _ui.CloseModal();
                ToggleLang(); yield return Shot("00_menu_en"); ToggleLang();
            }

            _ui.HideScreen();
            StartCoroutine(StartChapter1());
            if (R == "mars")
            {
                float w0 = 0; while (!InCutscene && w0 < 5) { w0 += Time.deltaTime; yield return null; }
                yield return new WaitForSeconds(2.0f); yield return Shot("c1_intro_a");
                yield return new WaitForSeconds(4.0f); yield return Shot("c1_intro_b");
            }
            yield return new WaitForSeconds(1.0f);
            yield return Settle();

            if (R == "mars")
                for (int i = 0; i < 5; i++) { PlaceInRoom(i, -1.5f); RefreshAll(); yield return Shot($"01_ship_room{i}"); }
            PlaceInRoom(RCockpit, -1.2f);

            yield return Click("s_scope");
            yield return Click("s_console", false);
            { float w1 = 0; while (!InCutscene && w1 < 10) { w1 += Time.deltaTime; if (_ui.DialogOpen) _ui.Advance(); if (_cardModalOpen) _ui.CloseModal(); yield return null; } }
            if (R == "mars") { yield return new WaitForSeconds(1.9f); yield return Shot("c2_impact"); }
            yield return Settle();
            Check(_impact, "impact after two shift tasks");
            float o2Before = _cabinO2;
            yield return new WaitForSeconds(3f);
            Check(_cabinO2 < o2Before, $"cabin O2 drains in real time ({o2Before:0} -> {_cabinO2:0})");
            PlaceInRoom(RFuel, -1f); RefreshAll();
            yield return Shot("02_leak_fuelbay");


            yield return ClickToModal("s_locker");
            PressKeys("3"); yield return new WaitForSeconds(0.2f);
            yield return Shot("03_keypad_wrong");
            PressKeys(_cfg.puzzles.lockerCode);
            yield return Settle();
            Check(_inv.Has("item_patchkit"), "locker code from the debris card opens the locker");
            yield return Click("s_breach");
            Check(_sealed, "breach sealed with the kit");
            Log($"cabin O2 when sealed: {_cabinO2:0}");
            yield return Click("s_gauge");

            yield return ClickToModal("s_nav");
            yield return Shot("04_nav");
            string dest = R == "moon" ? "earth" : R;
            _ui.Press(_loc.T("nav." + dest));
            yield return new WaitForSeconds(0.4f);
            yield return Shot("05_nav_confirm");
            _ui.Press(_loc.T("nav.go"));
            yield return Settle();
            Check(_route == R, "route chosen: " + _route);

            yield return Click("s_shelf");
            Check(_manualRead, "manual read for the route");
            yield return ClickToModal("s_seat");
            var edl = _cfg.GetRoute(R).edl;
            string bad = edl.distractors.Count > 0 ? edl.distractors[0] : edl.order[2];
            _ui.Press(_loc.T("edl." + bad));
            yield return new WaitForSeconds(0.3f);
            yield return Shot("06_edl_wrong");
            foreach (var st in edl.order) { _ui.Press(_loc.T("edl." + st)); yield return new WaitForSeconds(0.15f); }
            { float ws = 0; while (!_scramble && ws < 10) { ws += Time.deltaTime; if (_ui.DialogOpen) _ui.Advance(); yield return null; } }
            Check(_scramble, "60-second scramble started after the landing steps");
            yield return new WaitForSeconds(0.3f);
            PlaceInRoom(RQuarters, -1.5f); RefreshAll();
            yield return Shot("06b_scramble");
            float before = _scrambleLeft;
            foreach (var sid in new[] { "sup_food", "sup_medkit", "sup_o2" }) yield return Click("loose_" + sid);
            Check(_stowed.Count == 3, "stowed 3 supplies: " + string.Join(",", _stowed));
            Check(_scrambleLeft < before, $"scramble clock runs ({before:0} -> {_scrambleLeft:0})");
            _scrambleLeft = 0.1f;
            _autoAdvance = true;
            { float w2 = 0; while (!InCutscene && w2 < 10) { w2 += Time.deltaTime; yield return null; } }
            yield return new WaitForSeconds(1.8f); yield return Shot("07_land_a");
            yield return new WaitForSeconds(3.2f); yield return Shot("07_land_b");
            yield return new WaitForSeconds(3.0f); yield return Shot("07_land_c");
            yield return new WaitForSeconds(2.8f); yield return Shot("07_land_d");
            float t = 0;
            while (_chapter != 2 && t < 30) { t += Time.deltaTime; yield return null; }
            Check(_chapter == 2, "landed, chapter 2 started");
            t = 0;
            while (!_ui.ModalOpen && t < 10) { t += Time.deltaTime; yield return null; }
            _autoAdvance = false;
            yield return Shot("08_carry_report");
            Check(_sim.State.flags.Contains("med_kit"), "stowed first aid kit becomes a Chapter 2 flag");
            yield return Settle();
            yield return Shot("09_quarters_landed");

            switch (R)
            {
                case "mars":
                    yield return Click("s_toolbox");
                    yield return ClickToModal("s_cabinet");
                    _ui.Press(_loc.T("item.item_cat_cu")); yield return Settle();
                    yield return Click("s_reactor");
                    Check(!_sim.State.flags.Contains("catalyst_installed"), "wrong catalyst rejected");
                    yield return ClickToModal("s_cabinet");
                    _ui.Press(_loc.T("item.item_cat_ni")); yield return Settle();
                    yield return Click("s_reactor");
                    Check(_sim.State.flags.Contains("catalyst_installed"), "nickel catalyst installed");
                    yield return Click("s_airlock");
                    yield return Shot("10_outside");
                    yield return Click("mars_scrap");
                    OnItemClicked("item_plate"); OnItemClicked("item_sealant");
                    yield return Settle();
                    Check(_inv.Has("item_hull_patch"), "plate + sealant combine");
                    yield return ClickToModal("mars_pod");
                    PressKeys(_cfg.puzzles.podCode); yield return Settle();
                    yield return Click("mars_drill");
                    Check(_sim.State.flags.Contains("drill_ready"), "drill bit installed");
                    yield return ClickToModal("s_line");
                    yield return Shot("11_action_menu");
                    _ui.Press(_loc.T("action.repair_line")); yield return Settle();
                    Check(_sim.State.flags.Contains("line_repaired"), "fuel line repaired");
                    break;
                case "moon":
                    yield return Click("s_cabinet");
                    yield return Click("s_airlock");
                    yield return Shot("10_outside");
                    yield return ClickToModal("moon_pod");
                    PressKeys(_cfg.puzzles.moonPodCode); yield return Settle();
                    Check(_inv.Has("item_feedhorn"), "pod code 1969 opens the pod");
                    OnItemClicked("item_feedhorn"); OnItemClicked("item_cable");
                    yield return Settle();
                    Check(_inv.Has("item_antenna_kit"), "feed horn + cable combine");
                    yield return ClickToModal("moon_dish");
                    yield return Shot("11_aim_choice");
                    _ui.Press(_loc.T("moon_aim.up")); yield return Settle();
                    Check(!_sim.State.flags.Contains("antenna_ready"), "wrong aim rejected");
                    yield return ClickToModal("moon_dish");
                    _ui.Press(_loc.T("moon_aim.horizon")); yield return Settle();
                    Check(_sim.State.flags.Contains("antenna_ready"), "dish aimed at Earth on the horizon");
                    yield return ClickToModal("moon_crater");
                    _ui.Press(_loc.T("moon_site.crater")); yield return Settle();
                    Check(_sim.State.flags.Contains("drill_ready"), "drill set in the shadowed crater");
                    break;
                default:
                    yield return ClickToModal("s_airlock");
                    yield return Shot("10_suit_choice");
                    _ui.Press(_loc.T("titan_suit.pressure")); yield return Settle();
                    Check(!_titanSuitOk, "pressure suit rejected");
                    yield return ClickToModal("s_airlock");
                    _ui.Press(_loc.T("titan_suit.heated")); yield return Settle();
                    Check(_titanSuitOk && _room == ROutside, "heated suit + mask, outside");
                    yield return Shot("11_outside");
                    yield return ClickToModal("titan_gate");
                    PressKeys(_cfg.puzzles.titanGateCode); yield return Settle();
                    Check(_sim.State.flags.Contains("gate_found"), "gate code 179 unlocks the gate");
                    yield return Click("s_cabinet");
                    yield return Click("s_crates");
                    OnItemClicked("item_drillbit"); OnItemClicked("item_heater_coil");
                    yield return Settle();
                    Check(_inv.Has("item_heated_bit"), "drill bit + heater coil combine");
                    yield return Click("titan_drill");
                    Check(_sim.State.flags.Contains("drill_ready"), "heated bit installed");
                    break;
            }

            if (R == "moon")
            {
                yield return ClickToModal("moon_solar");
                yield return Shot("11b_berm_menu");
                _ui.Press(_loc.T("action.build_berm")); yield return Settle();
                Check(_sim.Shielded, "regolith berm built (radiation shield)");
            }
            Check(_codex.Has("rad_dose"), "radiation card shown after landing");

            for (int i = 0; i < 6; i++) { PlaceInRoom(i, -1.5f); _player.SetSuit(i == ROutside); RefreshAll(); yield return Shot($"12_landed_room{i}"); }

            PlaceInRoom(RCockpit, -1f); _player.SetSuit(false);
            var sci1 = _rc.actions.Find(a => a.science && a.minSol == 1);
            yield return ClickToModal("s_scope");
            yield return Shot("12b_science_menu");
            _ui.Press(_loc.T("action." + sci1.id)); yield return Settle();
            Check(_sim.State.flags.Contains(sci1.setsFlag) && _codex.Has(sci1.card), $"science task {sci1.id} done, card {sci1.card}");
            var later = _rc.actions.Find(a => a.science && a.minSol > 1);
            Check(!_sim.CanDo(later.id, out var laterWhy) && laterWhy == "reason.later", "later science task waits for its day");

            PlaceInRoom(RQuarters, 1.2f); _player.SetSuit(false);
            yield return ClickToModal("s_bed");
            yield return Shot("13_bed");
            _ui.Press(_loc.T("bed.sleep"));
            t = 0;
            _autoAdvance = true;
            while (!_ui.ModalOpen && t < 15) { t += Time.deltaTime; yield return null; }
            _autoAdvance = false;
            yield return Shot("14_night_report");
            _ui.CloseModal();
            yield return UntilModal();
            yield return Shot("14b_dilemma");
            var dil = _rc.dilemmas.Find(d => d.sol == 2);
            _ui.Press(_loc.T($"dilemma.{dil.id}.{dil.options[0].id}"));
            yield return Settle();
            Check(_sim.State.sol == 2, "sol 2 reached");
            Check(_sim.State.choices.TryGetValue(dil.id, out var chose) && chose == dil.options[0].id, "morning dilemma answered: " + dil.id);

            yield return ClickToModal("s_recycler"); _ui.Press(_loc.T("action.recycle")); yield return Settle();
            yield return ClickToModal("s_o2gen"); _ui.Press(_loc.T("action." + O2Action)); yield return Settle();
            Check(_incident != null, "daytime emergency started after 2 actions");
            if (_incident != null)
            {
                yield return new WaitForSeconds(1.5f);
                yield return Shot("15_incident");
                var spot = _incident.hotspot;
                yield return Click(spot);
                Check(_incident == null && _sim.State.incidentsDone.Count == 1, "emergency fixed by reaching " + spot);
            }

            OnHint();
            yield return new WaitForSeconds(0.4f);
            yield return Shot("16_hint");
            yield return Settle();
            OnCards();
            yield return Shot("17_codex");
            _ui.CloseModal();

            int keepO2 = _sim.State.o2;
            _sim.State.o2 = 18; RefreshAll();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("18_low_o2");
            _sim.State.o2 = keepO2; RefreshAll();

            DebugSolvePuzzles();
            _sim.State.fuel = _rc.survival.fuelTarget;
            bool eventShot = false;
            while (_sim.State.sol < _rc.survival.launchSol && _sim.State.outcome == SimOutcome.Ongoing)
            {
                var ss = _sim.State;
                ss.o2 = ss.water = ss.food = ss.power = ss.stamina = 80;
                _sim.Sleep();
                if (!eventShot && _sim.State.encounterToday != null)
                {
                    eventShot = true;
                    var ev = _rc.dilemmas.Find(d => d.id == _sim.State.encounterToday);
                    RefreshAll();
                    StartCoroutine(MorningDilemma());
                    yield return UntilModal();
                    yield return Shot("18b_event");
                    var opt = ev.options.Find(o => _sim.CanChoose(o, out _));
                    _ui.Press(_loc.T($"dilemma.{ev.id}.{opt.id}"));
                    yield return Settle();
                    Check(_sim.State.choices.ContainsKey(ev.id), "random event answered: " + ev.id);
                }
            }
            Check(eventShot, "a random event was drawn during the week");
            Check(_sim.State.sol == 7, "the stay lasts a full week (sol 7)");
            RefreshAll();
            Log($"sim sol={_sim.State.sol} outcome={_sim.State.outcome} o2={_sim.State.o2} water={_sim.State.water} food={_sim.State.food} power={_sim.State.power}");
            yield return ClickToModal("s_console");
            yield return Shot("19_goal_modal");
            _ui.Press(_loc.T($"goal.{R}.go"));
            t = 0;
            _autoAdvance = true;
            while (!InCutscene && t < 10) { t += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(1.8f); yield return Shot("19_win_a");
            yield return new WaitForSeconds(3.4f); yield return Shot("19_win_b");
            yield return new WaitForSeconds(3.2f); yield return Shot("19_win_c");
            t = 0;
            while (!_ui.ScreenOpen && t < 25) { t += Time.deltaTime; yield return null; }
            _autoAdvance = false;
            yield return new WaitForSeconds(0.8f);
            Check(_chapter == 3 && _sim.State.outcome == SimOutcome.Won, "route won");
            var stars = _sim.Stars();
            Check(stars[0] && !stars[2], "debrief stars: mission yes, science not all");
            Log($"debrief dose={_sim.State.dose:0.0} log={_sim.State.log.Count} stars={stars[0]},{stars[1]},{stars[2]} best={Settings.BestStars(R)}");
            yield return Shot("20_win");
            ShowEnd(false, SimOutcome.LostO2);
            yield return Shot("21_lose");

            Log($"done errors={_errors} checksFailed={_checksFailed} cards={_codex.Cards.Count}");
            _autoLog?.Close();
            _autoLog = null;
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }
    }
}
