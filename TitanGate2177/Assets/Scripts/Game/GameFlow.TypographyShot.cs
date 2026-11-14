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
            var regular = Resources.Load<Font>("Fonts/Sarabun-Regular");
            var bold = Resources.Load<Font>("Fonts/Sarabun-SemiBold");
            const string sample = "ที่นี่ เชื้อเพลิง น้ำ ผู้กู้ภัย ฟื้นฟู ตั้งค่า สำเร็จ ABC xyz 0123456789";
            Check(regular != null && bold != null && sample.Where(c => !char.IsWhiteSpace(c)).All(c => regular.HasCharacter(c) && bold.HasCharacter(c)), "both bundled font weights cover Thai, marks, Latin and digits");
            Check(GameUI.Thai(sample) == sample, "Thai combining sequences remain intact for shaping");
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
            Check(_ui.Press(_loc.T("menu.settings")), "settings opens using the menu callback");
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
            _autoAdvance = true; yield return new WaitForSeconds(1); _autoAdvance = false;
            while (_ui.DialogOpen) { _ui.Advance(); yield return new WaitForSeconds(.3f); }
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
    }
}
