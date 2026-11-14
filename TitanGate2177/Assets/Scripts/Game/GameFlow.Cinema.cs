// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do short cinematic shots (Blender renders, slow camera moves, one short line each) tell the story
//           better than the old side-view puppet cutscenes?
// Date: 2026-11-15
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// LAST DOCK cutscenes: each scene is a list of shots. A shot is one Blender render (Resources/Sprites/KeyArt) with a
    /// slow push or pan, letterbox bars, a short line, and optional shake, flash and sound. Space/click skips the scene.
    /// </summary>
    public partial class GameFlow
    {
        struct CinShot
        {
            public string art, en, th, sfx;
            public float seconds, zoomFrom, zoomTo, panX, shake;
            public bool flash;
        }

        static CinShot Cut(string art, float seconds, string en, string th, float zoomFrom = 1.04f, float zoomTo = 1.14f, float panX = 0f, string sfx = null, float shake = 0, bool flash = false)
            => new CinShot { art = art, seconds = seconds, en = en, th = th, zoomFrom = zoomFrom, zoomTo = zoomTo, panX = panX, sfx = sfx, shake = shake, flash = flash };

        IEnumerator LdCinema(params CinShot[] shots)
        {
            InCutscene = true;
            bool skip = false;
            VisualElement pic = null, flash = null; Label line = null; VisualElement lineBox = null;
            _ui.ShowScreen(root =>
            {
                root.style.backgroundColor = Color.black;
                pic = new VisualElement { pickingMode = PickingMode.Ignore };
                pic.style.position = Position.Absolute; pic.style.left = 0; pic.style.right = 0; pic.style.top = 0; pic.style.bottom = 0;
                pic.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                root.Add(pic);
                foreach (var top in new[] { true, false })
                {
                    var bar = new VisualElement { pickingMode = PickingMode.Ignore }; bar.style.position = Position.Absolute; bar.style.left = 0; bar.style.right = 0; bar.style.height = Length.Percent(11);
                    bar.style.backgroundColor = Color.black; if (top) bar.style.top = 0; else bar.style.bottom = 0; root.Add(bar);
                }
                lineBox = new VisualElement { pickingMode = PickingMode.Ignore }; lineBox.style.position = Position.Absolute; lineBox.style.left = 0; lineBox.style.right = 0; lineBox.style.bottom = Length.Percent(13);
                lineBox.style.alignItems = Align.Center;
                line = _ui.L("", 38, true, GameUI.CText); line.pickingMode = PickingMode.Ignore; line.style.unityTextAlign = TextAnchor.MiddleCenter; line.style.maxWidth = 1400; line.style.whiteSpace = WhiteSpace.Normal;
                line.style.unityTextOutlineWidth = 1.2f; line.style.unityTextOutlineColor = new Color(0, 0, 0, 0.9f); LdFont(line);
                lineBox.Add(line); root.Add(lineBox);
                flash = new VisualElement { pickingMode = PickingMode.Ignore }; flash.style.position = Position.Absolute; flash.style.left = 0; flash.style.right = 0; flash.style.top = 0; flash.style.bottom = 0;
                flash.style.backgroundColor = new Color(1f, 0.95f, 0.85f, 0f); root.Add(flash);
                var skipBtn = _ui.B(TT("Skip  ▸▸", "ข้าม  ▸▸"), () => skip = true, 20); skipBtn.style.position = Position.Absolute; skipBtn.style.right = 36; skipBtn.style.top = Length.Percent(3); root.Add(skipBtn);
            });
            _ui.FadeTo(0f, 0.4f);
            foreach (var s in shots)
            {
                if (skip) break;
                var tex = LdKeyArt(s.art);
                if (tex != null) pic.style.backgroundImage = new StyleBackground(tex);
                line.text = TT(s.en, s.th); line.style.opacity = 0;
                if (!string.IsNullOrEmpty(s.sfx)) _sfx.Play(s.sfx);
                if (s.flash) flash.style.backgroundColor = new Color(1f, 0.95f, 0.85f, 0.95f);
                float t = 0, dur = _playtest ? Mathf.Min(1f, s.seconds) : s.seconds;
                while (t < dur && !skip)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.SmoothStep(0, 1, t / dur);
                    float z = Mathf.Lerp(s.zoomFrom, s.zoomTo, k);
                    float sx = 0, sy = 0;
                    if (s.shake > 0 && !Settings.ReducedMotion) { float a = s.shake * (1 - k * 0.7f); sx = (Mathf.PerlinNoise(t * 20, 0) - .5f) * 2 * a; sy = (Mathf.PerlinNoise(0, t * 20) - .5f) * 2 * a; }
                    pic.style.scale = new Scale(new Vector3(z, z, 1));
                    pic.style.translate = new Translate(Length.Percent(s.panX * (k - 0.5f) * 10 + sx), Length.Percent(sy));
                    line.style.opacity = Mathf.Clamp01(t / 0.5f) * Mathf.Clamp01((dur - t) / 0.4f);
                    var fc = flash.resolvedStyle.backgroundColor; if (fc.a > 0) { fc.a = Mathf.Max(0, fc.a - Time.unscaledDeltaTime * 1.6f); flash.style.backgroundColor = fc; }
                    var kb = UnityEngine.InputSystem.Keyboard.current;
                    if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) skip = true;
                    yield return null;
                }
            }
            yield return Fade(1f, 0.35f);
            _ui.HideScreen();
            InCutscene = false;
        }

        // ------------------------------------------------------------------ the scenes

        IEnumerator LdCinIntro() => LdCinema(
            Cut("s01_cruise", 4.5f, "2177. The ship Kestrel is three days from Mars.", "ปี 2177 ยาน Kestrel อีกสามวันจะถึงดาวอังคาร", 1.02f, 1.12f, 0.3f, "whoosh"),
            Cut("s02_nose", 4f, "Mars. Our new home — if we get there.", "ดาวอังคาร บ้านใหม่ของเรา ถ้าเราไปถึง", 1.0f, 1.18f),
            Cut("title_keyart", 3.5f, "You are the youngest cadet on board.", "คุณคือนักบินฝึกหัดที่อายุน้อยที่สุดบนยาน", 1.12f, 1.04f, -0.3f));

        IEnumerator LdCinImpact() => LdCinema(
            Cut("s03_debris", 3f, "Debris! A cloud of rock, right on our path.", "ขยะอวกาศ! ก้อนหินจำนวนมากพุ่งมาทางเรา", 1.0f, 1.25f, 0.4f, "alarm", 0.4f),
            Cut("s04_impact", 2.5f, "IMPACT.", "โดนชน!", 1.2f, 1.06f, 0f, "thud", 2.2f, true),
            Cut("s04_impact", 2.5f, "The hull is breached. Abandon ship!", "ตัวยานรั่ว สละยาน!", 1.06f, 1.12f, 0.2f, "rumble", 0.8f));

        IEnumerator LdCinLaunch() => LdCinema(
            Cut("s05_launch", 4f, "The lifeboat breaks free.", "ยานชูชีพหลุดออกมาได้", 1.05f, 1.2f, -0.3f, "whoosh", 0.5f),
            Cut("cut_landing", 4f, "Mars rushes up to meet us...", "พื้นดาวอังคารพุ่งเข้ามาหา...", 1.18f, 1.02f, 0f, "thud", 0.9f),
            Cut("title_keyart", 4f, "An old, empty base. Rescue can reach us in 7 days.", "ฐานเก่าที่ไม่มีใคร ยานกู้ภัยจะมาถึงใน 7 วัน", 1.0f, 1.1f, 0.3f));

        IEnumerator LdCinWin() => LdCinema(
            Cut("cut_rescue", 4f, "A light crosses the red sky.", "แสงหนึ่งพาดผ่านฟ้าสีแดง", 1.0f, 1.12f, 0.2f, "whoosh"),
            Cut("cut_rescue", 4f, "They heard us. We're going home.", "พวกเขาได้ยินเรา เราได้กลับบ้านแล้ว", 1.12f, 1.22f, -0.2f));

        IEnumerator LdCinLose() => LdCinema(
            Cut("cut_dusk", 4f, "Night falls over the quiet base.", "ความมืดปกคลุมฐานที่เงียบสงัด", 1.0f, 1.1f, 0.2f),
            Cut("title_keyart", 3.5f, "Every mission teaches something. Try again.", "ทุกภารกิจสอนอะไรเราเสมอ ลองอีกครั้ง", 1.1f, 1.02f, -0.2f));
    }
}
