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
            string[] types = { "wires", "screws", "scrub", "pipes", "shield", "switches", "tune", "leak", "crank" };
            foreach (var type in types)
            {
                int callbacks = 0;
                PzOpen(type, 0, new JobDef { en = "Habitat maintenance", th = "บำรุงรักษาฐาน", factEn = "", factTh = "" },
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
            Log($"done errors={_errors} checksFailed={_checksFailed}"); _autoLog?.Close(); _autoLog = null;
            yield return new WaitForSeconds(.5f); Application.Quit();
        }
    }
}
