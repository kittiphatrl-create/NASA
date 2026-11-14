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
        bool _autoAdvance, _autoCloseModals, _ch1Lifeboat;
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
            bool restFlow = System.Array.IndexOf(args, "-restflowcheck") >= 0;
            bool art = System.Array.IndexOf(args, "-artshowcase") >= 0;
            bool typography = System.Array.IndexOf(args, "-typecheck") >= 0;
            bool story = System.Array.IndexOf(args, "-storyshowcase") >= 0;
            bool ux = System.Array.IndexOf(args, "-uxshowcase") >= 0;
            bool lifeboat = System.Array.IndexOf(args, "-lifeboat") >= 0;   // the card-based Chapter 2 on its own
            bool walkBase = System.Array.IndexOf(args, "-base") >= 0;       // the walkable Mars base week
            bool play = System.Array.IndexOf(args, "-play") >= 0;           // a full playthrough with the real clock
            bool shipTop = System.Array.IndexOf(args, "-ship") >= 0;        // the top-down Chapter 1
            _ch1Lifeboat = System.Array.IndexOf(args, "-ch1lifeboat") >= 0; // Chapter 1, then the landing into the diary
            if (_ch1Lifeboat) ForceDiary = true;
            if (restFlow) _autoRoute = "restflow-" + Screen.width;
            else if (art) _autoRoute = "art";
            else if (typography) _autoRoute = "typography-" + Screen.width;
            else if (story) _autoRoute = "story";
            else if (ux) _autoRoute = "ux";
            else if (shipTop) _autoRoute = "ship-" + Screen.width;
            if (play) _autoRoute = "play";
            else if (walkBase) _autoRoute = "base";
            else if (lifeboat) _autoRoute = "lifeboat";
            else if (_ch1Lifeboat) _autoRoute = "ch1lifeboat";
            else ForceOutpost = true;   // the default pass checks the old walk-and-click outpost
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
            if (System.Array.IndexOf(args, "-recordwalkthrough") >= 0) StartCoroutine(RecordWalkthrough());
            // fixed seed for repeatable screenshots; -seed N picks another run (balance checks over several seeds)
            int seedArg = System.Array.IndexOf(args, "-seed");
            SeedOverride = seedArg >= 0 && seedArg + 1 < args.Length && int.TryParse(args[seedArg + 1], out var sv) ? sv : 7;
            Application.runInBackground = true;
            StartCoroutine(restFlow ? AutoRestFlow() : art ? AutoArt() : typography ? AutoTypography() : play ? AutoPlay() : story ? AutoStory() : ux ? AutoUX() : shipTop ? AutoShip() : walkBase ? AutoBase() : lifeboat ? AutoLifeboat() : AutoShot());
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
            string R = "mars"; // the outpost game has one destination
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
            string dest = "mars";
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
            { float ws = 0; while (!_scramble && ws < 10) {
                ws += Time.deltaTime;
                if (_lbPickForScramble && _ui.ScreenOpen)
                {
                    yield return Shot("06a_pet_pick");
                    Check(_ui.Press(_loc.T("life.pick.choose", _loc.T("pet.luna.name"))), "pet picked before the 60-second scramble");
                }
                if (_ui.DialogOpen) _ui.Advance();
                yield return null; } }
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
            if (_ch1Lifeboat) { yield return AutoCh1LifeboatTail(); yield break; }
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

            Check(_sim.State.hour == 0 && _sim.Day == 1 && _sim.Clock == 6, "survival starts Day 1 06:00, hour 0 of 168");
            Check(_sim.HoursLeft == 168, "rescue countdown is 168 h (7 days)");

            // the old Mars puzzles still work and feed the outpost
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
            int hourBefore = _sim.State.hour;
            yield return ClickToModal("s_line");
            yield return Shot("11_action_menu");
            _ui.Press(_loc.T("action.repair_beacon")); yield return Settle();
            Check(_sim.State.flags.Contains("beacon_repaired"), "beacon line repaired with the patch");
            Check(_sim.State.hour - hourBefore >= 3, $"repair takes in-game hours ({hourBefore} -> {_sim.State.hour})");

            // Base Control: the power budget
            PlaceInRoom(RCockpit, -1f); _player.SetSuit(false);
            OnBase();
            yield return new WaitForSeconds(0.3f);
            _sim.SetSystem("beacon", true); RefreshAll(); _ui.RebuildModal();
            yield return new WaitForSeconds(0.2f);
            yield return Shot("11b_base_control");
            _ui.CloseModal();
            Check(_sim.IsOn("beacon") && _sim.PowerDrawPerHour > 3f, "switching the beacon on adds to the power draw");
            yield return ClickToModal("s_nav");
            yield return Shot("11c_comms");
            _ui.Press(_loc.T("action.send_coords")); yield return Settle();
            Check(_sim.State.coords >= 1, "coordinates sent through the beacon");
            yield return ClickToModal("s_nav");
            _ui.Press(_loc.T("action.ask_earth")); yield return Settle();
            Check(_sim.State.messages.Exists(m => m.id == "earth_hint"), "question queued for Earth (relay pass + light delay)");
            yield return ClickToModal("s_scope");
            yield return Shot("12b_science_menu");
            _ui.Press(_loc.T("action.sci_sunset")); yield return Settle();
            Check(_sim.State.flags.Contains("sci_sunset") && _codex.Has("mars_sunset"), "science task done, card mars_sunset");
            PlaceInRoom(RQuarters, 1f);
            yield return ClickToModal("s_bed");
            yield return Shot("12c_bed_shelter");
            float shelter0 = _sim.State.shelter;
            _ui.Press(_loc.T("action.shelter_water")); yield return Settle();
            Check(_sim.State.shelter > shelter0, $"water wall raises shelter readiness ({shelter0:0} -> {_sim.State.shelter:0})");

            for (int i = 0; i < 6; i++) { PlaceInRoom(i, -1.5f); _player.SetSuit(i == ROutside); RefreshAll(); yield return Shot($"12_landed_room{i}"); }

            // night: sleep until morning, report, the morning event
            PlaceInRoom(RQuarters, 1.2f); _player.SetSuit(false);
            _sim.Advance((21 - _sim.Clock + 24) % 24); RefreshAll();   // evening: full sleep is offered from 20:00
            int dayBefore = _sim.Day;
            float bedAir = _sim.State.air;
            foreach (var lvl in new[] { OC.warnAir - 5, OC.critAir - 2 })
            {
                _sim.State.air = lvl; RefreshAll();
                string warn = _sim.SleepAirWarning;
                Check(warn != null && !_loc.T(warn).StartsWith("#"), "bunk warns before sleeping: " + warn);
                yield return ClickToModal("s_bed");
                yield return Shot(warn == "bed.air_critical" ? "13a_bed_air_critical" : "13a_bed_air_low");
                _ui.CloseModal();
                yield return new WaitForSeconds(0.5f);
            }
            _sim.State.air = bedAir; RefreshAll();
            yield return ClickToModal("s_bed");
            yield return Shot("13_bed");
            _ui.Press(_loc.T("bed.sleep_morning", _sim.HoursToMorning));
            t = 0;
            _autoAdvance = true;
            while (!_ui.ModalOpen && t < 15) { t += Time.deltaTime; yield return null; }
            _autoAdvance = false;
            yield return Shot("14_night_report");
            Check(_sim.Day == dayBefore + 1 && _sim.Clock == 6, $"slept until 06:00 of day {_sim.Day}");
            _ui.CloseModal();
            yield return UntilModal();
            if (_sim.DueDilemma() != null)
            {
                yield return Shot("14b_event");
                var dil = _sim.DueDilemma();
                var opt = dil.options.Find(o => _sim.CanChoose(o, out _));
                _ui.Press(_loc.T($"dilemma.{dil.id}.{opt.id}"));
            }
            yield return Settle();
            Check(_sim.State.choices.Count >= 1, "morning event answered");

            // daytime emergency after two actions on day 2
            yield return ClickToModal("s_recycler"); _ui.Press(_loc.T("action.recycle")); yield return Settle();
            yield return ClickToModal("s_o2gen"); _ui.Press(_loc.T("action.moxie")); yield return Settle();
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
            float keepAir = _sim.State.air;
            _sim.State.air = 18; RefreshAll();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("18_low_air");
            _sim.State.air = keepAir; RefreshAll();

            // fast-forward with a prepared base up to just before the solar storm
            DebugSolvePuzzles();
            void Topped() { var ss = _sim.State; ss.air = ss.supplies = ss.energy = 80; ss.power = 90; }
            while (_sim.State.hour < OC.spe.hour - 1 && _sim.State.outcome == Outcome.Ongoing) { Topped(); _sim.Advance(1); if (_sim.DueDilemma() is DilemmaDef dd) _sim.Choose(dd.id, dd.options.Find(o => _sim.CanChoose(o, out _)).id); }
            Check(_sim.State.messages.Exists(m => m.id == "earth_spe_warning" && m.delivered), "Earth's storm warning arrived on its relay pass");
            Check(_sim.State.messages.Exists(m => m.id == "earth_hint" && m.delivered), "Earth answered the question after the relay delay");
            _sim.State.shelter = 100; _sim.SetSystem("shelter", true); _sim.SetSystem("beacon", true);
            RefreshAll();
            PlaceInRoom(RQuarters, 0f);
            yield return Shot("15b_spe_forecast");
            StartCoroutine(WaitHours(1));
            t = 0; while (!_ui.DialogOpen && t < 5) { t += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(0.6f);
            yield return Shot("16b_spe");
            yield return Settle();
            Check(_sim.State.speDone && _sim.State.log.Exists(e => e.kind == LogKind.Spe && e.ok), "solar storm hit once; the ready shelter protected the crew");

            while (_sim.State.hour < OC.hoursTotal - 1 && _sim.State.outcome == Outcome.Ongoing) { Topped(); _sim.Advance(1); if (_sim.DueDilemma() is DilemmaDef dd) _sim.Choose(dd.id, dd.options.Find(o => _sim.CanChoose(o, out _)).id); }
            Topped();
            Log($"sim hour={_sim.State.hour} day={_sim.Day} coords={_sim.State.coords} air={_sim.State.air:0} power={_sim.State.power:0} dose={_sim.State.dose:0.0}");
            Check(_sim.State.coords == 2, "rescue ship acknowledged the coordinates");
            StartCoroutine(WaitHours(1));
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
            Check(_chapter == 3 && _sim.State.outcome == Outcome.Rescued && _sim.State.hour == 168, "rescued at the end of day 7 (hour 168)");
            var stars = _sim.Stars();
            Check(stars[0], "debrief: rescue star");
            Log($"debrief dose={_sim.State.dose:0.0} log={_sim.State.log.Count} stars={stars[0]},{stars[1]},{stars[2]}");
            yield return Shot("20_win");
            // the stranded ending names the first rescue step that was missing: show each one, in both languages
            Check(_sim.State.log.TrueForAll(e => e.kind != LogKind.Night || _sim.NightAt(e.hour) >= 1), "debrief numbers nights from 1");
            _sim.State.outcome = Outcome.Stranded;   // so the debrief stars match the screen being shown
            foreach (var why in new[] { "no_beacon", "not_sent", "not_heard" })
            {
                if (why == "no_beacon") _sim.State.flags.Remove("beacon_repaired"); else _sim.State.flags.Add("beacon_repaired");
                _sim.State.coords = why == "not_heard" ? 1 : 0;
                Check(_sim.StrandedReason == why && !_loc.T("end.stranded." + why).StartsWith("#"), "stranded ending explains: " + why);
                ShowEnd(Outcome.Stranded);
                yield return Shot("21_stranded_" + why + "_th");
                ToggleLang(); ShowEnd(Outcome.Stranded);
                yield return Shot("21_stranded_" + why + "_en");
                ToggleLang();
            }
            ShowEnd(Outcome.FailedAir);
            yield return Shot("21_lose");
            _ui.HideScreen();
            ShowSources();
            yield return new WaitForSeconds(0.3f);
            yield return Shot("22_sources");
            _ui.CloseModal();

            Log($"done errors={_errors} checksFailed={_checksFailed} cards={_codex.Cards.Count}");
            _autoLog?.Close();
            _autoLog = null;
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }
    }
}
