// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do short animated 3D shots (Blender, rendered to video) tell the story better than still pictures?
// Date: 2026-11-15
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

namespace TitanGate.Game
{
    /// <summary>
    /// LAST DOCK cutscenes: each scene is a list of animated shots. A shot is a short video
    /// (Resources/Cine/&lt;shot&gt;.mp4, 960x540, 24 fps) rendered by Art/Blender/pixel_cine.py, played through a
    /// VideoPlayer into a RenderTexture behind letterbox bars, with one short line and optional sound, shake and flash.
    /// The last frame holds for a moment so the line can be read. Space/click skips the scene.
    /// </summary>
    public partial class GameFlow
    {
        struct CinShot
        {
            public string art, en, th, sfx;
            public float hold, shake;
            public bool flash;
        }

        static CinShot Cut(string art, string en, string th, float hold = 0.6f, string sfx = null, float shake = 0, bool flash = false)
            => new CinShot { art = art, en = en, th = th, hold = hold, sfx = sfx, shake = shake, flash = flash };

        VideoPlayer _cineVp;
        RenderTexture _cineRt;

        void LdCineSetup()
        {
            if (_cineRt == null) { _cineRt = new RenderTexture(960, 540, 0) { filterMode = FilterMode.Bilinear }; _cineRt.Create(); }
            if (_cineVp == null)
            {
                var go = new GameObject("CutsceneVideo");
                _cineVp = go.AddComponent<VideoPlayer>();
                _cineVp.playOnAwake = false; _cineVp.isLooping = false; _cineVp.skipOnDrop = true;
                _cineVp.renderMode = VideoRenderMode.RenderTexture; _cineVp.audioOutputMode = VideoAudioOutputMode.None;
                _cineVp.waitForFirstFrame = true;
            }
            _cineVp.targetTexture = _cineRt;
            var prev = RenderTexture.active; RenderTexture.active = _cineRt; GL.Clear(true, true, Color.black); RenderTexture.active = prev;
        }

        IEnumerator LdCinema(params CinShot[] shots)
        {
            InCutscene = true;
            LdCineSetup();
            bool skip = false;
            VisualElement pic = null, flash = null, black = null; Label line = null;
            _ui.ShowScreen(root =>
            {
                root.style.backgroundColor = Color.black;
                VisualElement Full()
                {
                    var e = new VisualElement { pickingMode = PickingMode.Ignore };
                    e.style.position = Position.Absolute; e.style.left = 0; e.style.right = 0; e.style.top = 0; e.style.bottom = 0;
                    return e;
                }
                pic = Full(); pic.style.backgroundImage = new StyleBackground(UnityEngine.UIElements.Background.FromRenderTexture(_cineRt));
                pic.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover); root.Add(pic);
                black = Full(); black.style.backgroundColor = Color.black; root.Add(black);
                foreach (var top in new[] { true, false })
                {
                    var bar = new VisualElement { pickingMode = PickingMode.Ignore }; bar.style.position = Position.Absolute; bar.style.left = 0; bar.style.right = 0; bar.style.height = Length.Percent(8);
                    bar.style.backgroundColor = Color.black; if (top) bar.style.top = 0; else bar.style.bottom = 0; root.Add(bar);
                }
                var lineBox = new VisualElement { pickingMode = PickingMode.Ignore }; lineBox.style.position = Position.Absolute; lineBox.style.left = 0; lineBox.style.right = 0; lineBox.style.bottom = Length.Percent(10);
                lineBox.style.alignItems = Align.Center;
                line = _ui.L("", 38, true, GameUI.CText); line.pickingMode = PickingMode.Ignore; line.style.unityTextAlign = TextAnchor.MiddleCenter; line.style.maxWidth = 1400; line.style.whiteSpace = WhiteSpace.Normal;
                line.style.unityTextOutlineWidth = 1.4f; line.style.unityTextOutlineColor = new Color(0, 0, 0, 0.95f); LdFont(line);
                line.style.paddingLeft = 18; line.style.paddingRight = 18; line.style.paddingTop = 6; line.style.paddingBottom = 6;
                line.style.backgroundColor = new Color(0, 0, 0, 0.45f);
                lineBox.Add(line); root.Add(lineBox);
                flash = Full(); flash.style.backgroundColor = new Color(1f, 0.95f, 0.85f, 0f); root.Add(flash);
                var skipBtn = _ui.B(TT("Skip  ▸▸", "ข้าม  ▸▸"), () => skip = true, 20); skipBtn.style.position = Position.Absolute; skipBtn.style.right = 36; skipBtn.style.top = Length.Percent(2); root.Add(skipBtn);
            });
            _ui.FadeTo(0f, 0.3f);
            foreach (var s in shots)
            {
                if (skip) break;
                var clip = Resources.Load<VideoClip>("Cine/" + s.art);
                float len = 2f;
                if (clip != null)
                {
                    _cineVp.Stop();
                    var prevRt = RenderTexture.active; RenderTexture.active = _cineRt; GL.Clear(true, true, Color.black); RenderTexture.active = prevRt;
                    _cineVp.clip = clip; _cineVp.Prepare();
                    float wait = 0;
                    while (!_cineVp.isPrepared && wait < 4f) { wait += Time.unscaledDeltaTime; yield return null; }
                    _cineVp.Play();
                    len = (float)clip.length;
                }
                line.text = TT(s.en, s.th); line.style.opacity = 0;
                if (!string.IsNullOrEmpty(s.sfx)) _sfx.Play(s.sfx);
                if (s.flash) flash.style.backgroundColor = new Color(1f, 0.95f, 0.85f, 0.95f);
                float t = 0, dur = _playtest ? Mathf.Min(1.2f, len + s.hold) : len + s.hold;
                bool shotTaken = false; float shown = -1f;
                while (t < dur && !skip)
                {
                    float dt = Time.unscaledDeltaTime; t += dt;
                    // stay black until the new video has drawn its first frame, then a quick fade up
                    if (shown < 0 && (clip == null || _cineVp.time > 0.06 || t > 1.5f)) shown = t;
                    black.style.opacity = shown < 0 ? 1f : 1f - Mathf.Clamp01((t - shown) / 0.25f);
                    float sx = 0, sy = 0;
                    if (s.shake > 0 && !Settings.ReducedMotion) { float a = s.shake * Mathf.Clamp01(1 - t / dur); sx = (Mathf.PerlinNoise(t * 20, 0) - .5f) * 2 * a; sy = (Mathf.PerlinNoise(0, t * 20) - .5f) * 2 * a; }
                    pic.style.translate = new Translate(Length.Percent(sx), Length.Percent(sy));
                    line.style.opacity = Mathf.Clamp01((t - 0.2f) / 0.4f) * Mathf.Clamp01((dur - t) / 0.3f);
                    var fc = flash.resolvedStyle.backgroundColor; if (fc.a > 0) { fc.a = Mathf.Max(0, fc.a - dt * 1.8f); flash.style.backgroundColor = fc; }
                    var kb = UnityEngine.InputSystem.Keyboard.current;
                    if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) skip = true;
                    if (_playtest && !shotTaken && t > dur * 0.7f) { shotTaken = true; yield return Shot("cin_" + s.art); }
                    yield return null;
                }
                // fade to black before the next cut
                float o = 0;
                while (o < 0.2f && !skip) { o += Time.unscaledDeltaTime; black.style.opacity = Mathf.Clamp01(o / 0.2f); yield return null; }
            }
            if (_cineVp != null) { _cineVp.Stop(); _cineVp.clip = null; }
            yield return Fade(1f, 0.3f);
            _ui.HideScreen();
            InCutscene = false;
        }

        // ------------------------------------------------------------------ the scenes

        /// <summary>The world in 2177, then you: the youngest trainee, first day on the Kestrel.</summary>
        IEnumerator LdCinIntro() => LdCinema(
            Cut("p01_earth", "The year 2177.", "ปี 2177", 0.8f, "whoosh"),
            Cut("p02_moon", "People have built towns on the Moon,", "มนุษย์สร้างเมืองบนดวงจันทร์", 0.3f),
            Cut("p03_mars", "bases on Mars,", "ตั้งฐานบนดาวอังคาร", 0.3f),
            Cut("p04_jupiter", "and ships that reach as far as Jupiter.", "และส่งยานไปไกลถึงดาวพฤหัส", 0.5f),
            Cut("p05_cabin", "You are the youngest trainee pilot of your class.", "คุณคือนักบินฝึกหัดที่อายุน้อยที่สุดในรุ่น", 1.0f),
            Cut("p06_walk", "Today is your first day of work on the ship Kestrel.", "วันนี้คือวันแรกที่คุณทำงานบนยาน Kestrel", 1.0f));

        IEnumerator LdCinImpact() => LdCinema(
            Cut("i01_debris", "Space debris! Heading straight for us.", "ขยะอวกาศ! พุ่งตรงมาทางเรา", 0.3f, "alarm"),
            Cut("i02_hit", "IMPACT!", "โดนชน!", 0.4f, "thud", 1.2f, true),
            Cut("i03_run", "The hull is breached. Get to the lifeboat!", "ยานรั่ว! รีบไปที่ยานชูชีพ", 0.5f, "rumble", 0.3f));

        IEnumerator LdCinLaunch() => LdCinema(
            Cut("l01_away", "The lifeboat breaks free.", "ยานชูชีพหลุดออกมาได้", 0.3f, "whoosh"),
            Cut("l02_entry", "Entry: the heat shield glows at 1,300 °C.", "เข้าชั้นบรรยากาศ · โล่กันความร้อนร้อนถึง 1,300 °C", 0.6f, "rumble", 0.5f),
            Cut("l03_chute", "The parachute opens and slows us down.", "ร่มชูชีพกาง · ชะลอความเร็วลง", 0.3f),
            Cut("l04_land", "Braking rockets... touchdown!", "จรวดชะลอ · แตะพื้นแล้ว!", 0.4f, "thud", 0.3f),
            Cut("l05_base", "An old, empty base. Rescue can reach us in 7 days.", "ฐานเก่าที่ไม่มีใคร · ยานกู้ภัยจะมาถึงใน 7 วัน", 1.2f));

        IEnumerator LdCinWin() => LdCinema(
            Cut("w01_rescue", "The rescue ship! They heard us.", "ยานกู้ภัยมาแล้ว! พวกเขาได้ยินเรา", 0.8f, "whoosh"),
            Cut("w02_home", "We're going home.", "เรากำลังกลับบ้าน", 1.2f, "rumble"));

        IEnumerator LdCinLose() => LdCinema(
            Cut("x01_dusk", "Night falls over the quiet base.", "ความมืดปกคลุมฐานที่เงียบสงัด", 0.8f),
            Cut("x02_stars", "Every mission teaches something. Try again.", "ทุกภารกิจสอนอะไรเราเสมอ ลองอีกครั้ง", 1.2f));
    }
}
