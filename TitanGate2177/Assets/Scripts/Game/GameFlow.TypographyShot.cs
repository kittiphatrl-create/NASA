using System.Collections;
using System.IO;
using System.Linq;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        IEnumerator AutoTypography()
        {
            _loc.Current = Lang.TH;
            yield return new WaitForSeconds(2);
            ShowMainMenu(); yield return Shot("01_title_th");
            var regular = Resources.Load<Font>("Fonts/TitanGateUI-Regular");
            var bold = Resources.Load<Font>("Fonts/TitanGateUI-Bold");
            const string sample = "ที่นี่ เชื้อเพลิง น้ำ ผู้กู้ภัย ฟื้นฟู ตั้งค่า สำเร็จ ABC xyz 0123456789";
            Check(regular != null && bold != null && sample.Where(c => !char.IsWhiteSpace(c)).All(c => regular.HasCharacter(c) && bold.HasCharacter(c)), "both bundled font weights cover Thai, marks, Latin and digits");
            const string clusters = "น้ำ ที่ อุณหภูมิ CO₂";
            string display = GameUI.Thai(clusters);
            Check(display == "\ue255า \ue218 อุณหภูมิ CO₂", "water and stacked vowels use shaped clusters");
            Check(display.Where(c => c >= '\ue000' && c <= '\uf8ff').All(c => regular.HasCharacter(c) && bold.HasCharacter(c)),
                "both loaded faces contain the shaped glyphs");
            _ui.ShowModal(p => {
                p.style.width = 1100;
                p.Add(_ui.L("ที่นี่ · เชื้อเพลิง · น้ำ · ผู้กู้ภัย", 42, true));
                p.Add(_ui.L("ฟื้นฟูฐานรกร้าง เก็บเครื่องมือ แล้วกลับมาพักด้วยกัน", 28));
                p.Add(_ui.L("Restore the habitat · Oxygen 75% · 06:00", 28));
                p.Add(_ui.B("ตั้งค่า / Settings", () => { }));
            });
            yield return Shot("02_thai_marks_and_latin"); _ui.CloseModal();
            _loc.Current = Lang.EN; BuildMainMenu(); yield return Shot("03_title_en");
            _loc.Current = Lang.TH;
            StartLifeboat(null, "comet"); yield return new WaitForSeconds(1);
            string[] types = { "wires", "screws", "scrub", "pipes", "shield", "switches", "tune", "leak", "crank" };
            foreach (var type in types)
            {
                int callbacks = 0;
                PzOpen(type, 0, new JobDef { id = type, en = "Habitat maintenance", th = "บำรุงรักษาฐาน", factEn = "", factTh = "" },
                    (_, __) => { callbacks++; _ui.HideScreen(); }, () => _ui.HideScreen());
                yield return Shot(type + "_working");
                _pzSolve?.Invoke(); PzComplete(); // Repeated completion must not award twice.
                _ui.Press("ถอยออกมา");
                Check(_pzRoot != null && callbacks == 0, type + " cannot discard a completed result through the back button");
                yield return new WaitForSeconds(.1f); yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(_shotDir, type + "_complete.png"));
                Check(!_pzBoard.Query<Label>().ToList().Any(l => l.text == "DONE" || l.text == "เสร็จแล้ว"), type + " has no success text over its mechanism");
                Check(_pzLamp.style.backgroundColor.value == GameUI.CGreen && _pzProgress.style.opacity.value == 1, type + " signals completion with lamp and rail");
                while (_pzRoot != null) yield return null;
                Check(callbacks == 1 && !_ui.ScreenOpen, type + " completes once and returns control");
            }
            // UI-only staging: no balance, collision, visitor or story rules are changed.
            ShowMainMenu();
            var settingsTile = _ui.Root.Q<VisualElement>("title-tile-gear");
            if (settingsTile != null)
            {
                using var click = ClickEvent.GetPooled();
                click.target = settingsTile;
                settingsTile.SendEvent(click);
            }
            else _ui.Press(_loc.T("menu.settings"));
            Check(_ui.ModalOpen, "settings opens using the menu callback");
            yield return Shot("ui_01_settings"); _ui.CloseModal();
            StartLifeboat(null, "comet"); yield return new WaitForSeconds(1.5f);
            yield return Shot("ui_02_hud");
            var bounds = _bHud.worldBound; var viewport = _ui.Root.worldBound;
            Check(bounds.xMin >= 0 && bounds.xMax <= viewport.xMax, "day and mission HUD remains within viewport");
            OrbiSay(TT("Water is running low. Check the recycler.", "น้ำใกล้หมดแล้ว ลองตรวจเครื่องรีไซเคิลน้ำ"), GameUI.CAmber);
            yield return Shot("ui_03_orbi_notice");
            _ui.Say("orbi", "c2_base_intro"); yield return new WaitForSeconds(1);
            Check(_ui.DialogOpen, "dialogue opens with background shade");
            yield return Shot("ui_04_dialogue");
            _ui.Advance();
            Check(_ui.DialogOpen && !_ui.Typing, "first advance finishes typing without closing the line");
            _ui.Advance();
            Check(!_ui.DialogOpen, "second advance closes the line");
            yield return CheckDialoguePresentation();
            var beforeCard = _lbCard;
            _lbCard = LC.cards.First(c => !string.IsNullOrEmpty(c.visitor));
            BShowPeep(); yield return Shot("ui_05_peephole");
            Check(_ui.ScreenOpen, "visitor view opens with existing actions");
            _ui.HideScreen(); _lbCard = beforeCard;
            OpenPause(); yield return new WaitForSecondsRealtime(.3f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(_shotDir, "ui_06_pause.png"));
            Check(_ui.ModalOpen && Time.timeScale == 0, "pause still freezes simulation");
            _ui.CloseModal(); Check(Time.timeScale == 1, "resume restores simulation time");
            Log($"done errors={_errors} checksFailed={_checksFailed}"); _autoLog?.Close(); _autoLog = null;
            yield return new WaitForSeconds(.5f); Application.Quit();
        }

        IEnumerator CheckDialoguePresentation()
        {
            int speed = Settings.TextSpeed;
            float size = Settings.TextScale;
            bool reduced = Settings.ReducedMotion;
            var language = _loc.Current;
            try
            {
                Settings.TextScale = 1.3f;
                Settings.TextSpeed = 70;
                Settings.ReducedMotion = true;
                _ui.Say("mentor", "c2_base_intro");
                yield return new WaitForSecondsRealtime(.6f);
                var portrait = _ui.Root.Q<Image>("dialogue-portrait");
                var resting = portrait.style.translate.value;
                Check(_ui.Typing && portrait.image != null &&
                      resting.x.value == 0 && resting.y.value == 0,
                    "portrait is displayed and stays still with Reduced Motion");
                _ui.Advance();
                yield return Shot("ui_04_mentor_th_large");
                _ui.Advance();

                _loc.Current = Lang.EN;
                Settings.TextSpeed = 0;
                _ui.Say("you", "c1_bed_look");
                yield return Shot("ui_04_player_en_large");
                Check(!_ui.Typing && portrait.image != null && portrait.layout.x > _ui.Root.layout.width * .5f,
                    "player portrait uses the right side and instant text");
                _ui.Advance();

                _loc.Current = Lang.TH;
                _ui.Say("sys", "c2_base_intro");
                yield return Shot("ui_04_system_th_large");
                Check(portrait.image == null && portrait.style.display.value == DisplayStyle.None,
                    "dialogue without a portrait gives the text the full frame");
                _ui.Advance();
            }
            finally
            {
                Settings.TextSpeed = speed;
                Settings.TextScale = size;
                Settings.ReducedMotion = reduced;
                _loc.Current = language;
            }
        }

        IEnumerator AutoRestFlow()
        {
            ForceOutpost = false; ForceDiary = false; _loc.Current = Lang.EN;
            yield return new WaitForSeconds(1);
            StartLifeboat(null, "comet"); yield return new WaitForSeconds(1);
            var dock = _ui.Root.Q<VisualElement>("notice-dock"); dock.Clear();
            _ui.Toast("A sealed seed packet! One more seed for the greenhouse.", GameUI.CGreen);
            _ui.Toast("It is getting dark. Dinner is ready in the pod.", GameUI.CAmber);
            OrbiSay("You are thirsty! Drink at the water shelf in the lounge.", GameUI.CAmber);
            yield return new WaitForSeconds(.5f);
            Check(dock.childCount == 3, "ordinary and ORBI notices share one dock");
            bool separated = true;
            for (int i = 0; i < dock.childCount; i++) for (int j = i + 1; j < dock.childCount; j++)
                separated &= !dock[i].worldBound.Overlaps(dock[j].worldBound);
            Check(separated, "all three notification bounds are non-overlapping");
            Check(dock.worldBound.yMax < _bHudSpeed.worldBound.yMin, "notifications remain above pause controls");
            yield return Shot("01_notices_stacked");
            for (int test = 0; test < 2; test++)
            {
                if (test == 1)
                {
                    var snap = _restore.Snapshot(); snap.done = _cfg.restore.tasks.Select(t => t.id).ToArray();
                    snap.unpowered = new string[0]; _restore.Load(snap); yield return new WaitForSeconds(.5f);
                }
                _restore.Spend(Mathf.Max(0, 20 - _restore.Hour)); RestoreSync();
                // Stage an evening only after its visitor cards have been resolved.
                foreach (var card in _life.OpenCards().ToArray()) _life.State.answered.Add(card.id);
                _lbView = LbView.Ration;
                yield return BGather();
                foreach (var who in _life.Mouths()) MealFeed(who, true);
                int day = _life.State.day;
                Check(_ui.Press("Finish dinner and go to bed"), "finish-dinner button starts sleep: " + test);
                // A second click must not charge or advance a second night.
                LbSleep();
                float timeout = 0; while ((_sleepTransition || _lbView != LbView.Night) && timeout < 8) { timeout += Time.deltaTime; yield return null; }
                Check(_lbView == LbView.Night && _life.State.day == day + 1, "exactly one overnight transition: " + test);
                Check(_bSleeping, "cadet is visibly sleeping on a bed: " + test);
                yield return Shot("02_sleep_" + test);
                Check(Vector2.Distance(_sleepBed, test == 0 ? BSpotById("sleep_cot").pos : BSpotById("bunks").pos) < .01f, "correct accessible bed selected: " + test);
                LbNextCard(); yield return new WaitForSeconds(.7f);
                Check(BFree(_bPos) && Vector2.Distance(_bPos, _wakeStand) < .1f, "wake beside bed on free floor: " + test);
                Check(_bPath == null && _bGoal == null && !_bGather && !_bSleeping && !ActionLocked, "wake clears seating, walking and action locks: " + test);
                var at = _bPos; yield return new WaitForSeconds(.7f);
                Check(Vector2.Distance(at, _bPos) < .01f, "no uncommanded walking after wake: " + test);
                yield return Shot("03_morning_" + test);
            }
            Log($"done errors={_errors} checksFailed={_checksFailed}"); _autoLog?.Close(); _autoLog = null;
            Application.Quit();
        }

    }
}
