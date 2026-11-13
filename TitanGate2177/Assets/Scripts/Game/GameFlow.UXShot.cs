using System.Collections;
using System.IO;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        IEnumerator AutoUX()
        {
            _loc.Current = Lang.TH;
            yield return new WaitForSeconds(2);
            ShowMainMenu();
            yield return new WaitForSeconds(1.5f);
            yield return Shot("01_title_th");
            var screenPoint = Camera.main.WorldToScreenPoint(_titleDrone.bounds.center);
            var panelPoint = RuntimePanelUtils.ScreenToPanel(_ui.Panel, new Vector2(screenPoint.x, Screen.height - screenPoint.y));
            Check(_ui.Panel.Pick(panelPoint)?.name == "orbi-interactive", "ORBI's rendered centre hits its interactive button");
            TitleShipPoke(); TitleDronePoke();
            Check(_titleBoostUntil > Time.unscaledTime && _titleWaveUntil > Time.unscaledTime, "title ship and ORBI reactions are active");
            Check(Art.Crew("cadet", 2, 3).rect.width == 64, "live side-view sprite uses authored 64px art");
            Check(Resources.Load<AudioClip>("Audio/KenneyUI/click") != null, "licensed UI click clip is included in the player");
            Check(_ui.Press(_loc.T("menu.settings")), "title settings opens through real menu action");
            Check(_ui.ModalOpen, "settings modal visible");
            yield return Shot("02_settings_th");
            _ui.CloseModal();
            _loc.Current = Lang.EN; BuildMainMenu();
            yield return Shot("03_title_en");
            _loc.Current = Lang.TH;
            StartLifeboat(null, "comet");
            yield return new WaitForSeconds(1.5f);
            Check(_life.State.schedule.Hour == 6, "day begins at 06:00");
            yield return Shot("04_morning_hud");
            float air = _life.State.stock.o2;
            _life.State.schedule.TrySpend(13);
            yield return new WaitForSeconds(1.2f);
            yield return Shot("05_dusk_hud");
            float before = _life.State.schedule.UsedHours;
            Check(!BTimedTask(LC.taskSolar, 3), "optional work cannot consume reserved story time");
            Check(_life.State.stock.o2 == air && _life.State.schedule.UsedHours == before, "rejected work is atomic for time and resources");
            _life.State.schedule.TrySpend(3);
            // Let the rejected-work notice expire before the lighting reference capture.
            yield return new WaitForSeconds(3.2f);
            yield return Shot("06_night_hud");
            _life.State.schedule.Reset();
            _life.State.guests.Add("aya"); BRefreshProps();
            _bPos = (Vector2)_bGuests[0].transform.position + Vector2.left * .8f;
            yield return new WaitForSeconds(.2f);
            BUseNearest();
            Check(_ui.ModalOpen, "nearby crewmate opens conversation through the E interaction handler");
            yield return Shot("07_crew_conversation");
            _ui.CloseModal();
            Vector2 start = _bGuests[0].transform.position;
            yield return new WaitForSeconds(5);
            Check(Vector2.Distance(start, _bGuests[0].transform.position) > .2f, "admitted crewmate walks toward a work station");
            yield return Shot("08_crew_routine");
            OpenPause(); Check(Time.timeScale == 0 && _ui.ModalOpen, "pause freezes simulation");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(_shotDir, "08b_pause_th.png"));
            yield return new WaitForSecondsRealtime(.3f);
            Log("shot 08b_pause_th");
            _ui.CloseModal(); Check(Time.timeScale == 1, "resume restores simulation time");
            BSaveCadetSheet(Path.Combine(_shotDir, "09_cadet_frames.png"));
            foreach (var id in new[] { "cadet", "aya", "jules", "tik", "mu", "zee", "nok", "ree", "shade", "mimic", "pip", "comet", "pixel", "luna" })
                SaveActorAtlas(Path.Combine(_shotDir, "frames_" + id + ".png"), id);
            Log($"done errors={_errors} checksFailed={_checksFailed}");
            _autoLog?.Close(); _autoLog = null;
            yield return new WaitForSeconds(.5f); Application.Quit();
        }

        static void SaveActorAtlas(string path, string id)
        {
            const int w = 68, h = 84, z = 2;
            var sheet = new Texture2D(w * 10 * z, h * 8 * z, TextureFormat.RGBA32, false);
            var pixels = new Color32[sheet.width * sheet.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(24, 35, 49, 255);
            sheet.SetPixels32(pixels);
            bool pet = id == "comet" || id == "pixel" || id == "luna";
            for (int run = 0; run < 2; run++) for (int dir = 0; dir < 4; dir++) for (int f = -2; f < 8; f++)
            {
                var sprite = pet ? Art.PetFrame(id, f, dir == 3 ? 2 : dir) : Art.Crew(id, dir == 3 ? 2 : dir, f, false, run == 1);
                for (int y = 0; y < sprite.rect.height; y++) for (int x = 0; x < sprite.rect.width; x++)
                {
                    var color = sprite.texture.GetPixel(x, y); if (color.a < .1f) continue;
                    int px = dir == 3 ? (int)sprite.rect.width - 1 - x : x;
                    for (int dy = 0; dy < z; dy++) for (int dx = 0; dx < z; dx++)
                        sheet.SetPixel(((f + 2) * w + px) * z + dx, ((7 - (dir + run * 4)) * h + y) * z + dy, color);
                }
            }
            sheet.Apply(); File.WriteAllBytes(path, sheet.EncodeToPNG()); Destroy(sheet);
        }
    }
}
