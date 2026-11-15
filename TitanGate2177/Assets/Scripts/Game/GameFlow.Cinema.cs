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
            bool impactScene = System.Array.Exists(shots, x => x.art == "i01_debris");
            foreach (var s in shots)
            {
                if (skip) break;
                LdScoreShot(s.art);
                bool redAlert = s.art == "i01_debris" || s.art == "i02_hit" || s.art == "i03_run";
                if (_chapterMusic != null) _chapterMusic.ContextGain = redAlert ? .55f : 1f;   // leave room for the crash and the alarm
                if (s.art == "i01_debris") { _sfx.CinematicAlarm(false); _sfx.Loop("ship_alarm", false); }   // quiet space until the rock hits
                if (s.art == "i03_run") _sfx.CinematicAlarm(true);
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
                _sfx.CinematicShot(s.art);
                if (!string.IsNullOrEmpty(s.sfx)) _sfx.Play(s.sfx);
                if (s.flash) flash.style.backgroundColor = new Color(1f, 0.95f, 0.85f, 0.95f);
                float t = 0, dur = _playtest ? Mathf.Min(1.2f, len + s.hold) : len + s.hold;
                bool shotTaken = false, touchdown = false, hit = false, alarmOn = false; float shown = -1f, kick = 0f;
                while (t < dur && !skip)
                {
                    float dt = Time.unscaledDeltaTime; t += dt;
                    // timed to the picture: the video clock once it runs, else our own
                    float vt = clip != null && _cineVp.isPlaying && _cineVp.time > 0 ? (float)_cineVp.time : t;
                    // the rock grazes the hull 2.0 s into i01; in i02 the jolt lands at 0.75 s, the red lights at 1.15 s
                    if (!hit && ((s.art == "i01_debris" && vt >= 2.0f) || (s.art == "i02_hit" && vt >= 0.72f)))
                    {
                        hit = true;
                        bool inside = s.art == "i02_hit";
                        _sfx.Impact(inside ? .9f : 1f);
                        kick = inside ? 1.6f : 1.2f;
                        flash.style.backgroundColor = inside ? new Color(1f, 0.95f, 0.85f, 0.95f) : new Color(1f, 0.6f, 0.25f, 0.55f);
                    }
                    if (!alarmOn && s.art == "i02_hit" && vt >= 1.12f) { alarmOn = true; _sfx.CinematicAlarm(true); }
                    // The landing impact belongs at touchdown, after the braking engines have run.
                    if (s.art == "l04_land" && !touchdown && t >= len * .65f)
                    {
                        touchdown = true;
                        _sfx.Play("thud", .7f);
                        _sfx.StopCinematicAudio();
                    }
                    // stay black until the new video has drawn its first frame, then a quick fade up
                    if (shown < 0 && (clip == null || _cineVp.time > 0.06 || t > 1.5f)) shown = t;
                    black.style.opacity = shown < 0 ? 1f : 1f - Mathf.Clamp01((t - shown) / 0.25f);
                    float sx = 0, sy = 0;
                    kick = Mathf.Max(0f, kick - dt * 1.6f);
                    if ((s.shake > 0 || kick > 0) && !Settings.ReducedMotion) { float a = s.shake * Mathf.Clamp01(1 - t / dur) + kick; sx = (Mathf.PerlinNoise(t * 20, 0) - .5f) * 2 * a; sy = (Mathf.PerlinNoise(0, t * 20) - .5f) * 2 * a; }
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
            _sfx.StopCinematicAudio();
            if (_chapterMusic != null) _chapterMusic.ContextGain = 1f;
            if (impactScene)
            {
                // the scramble keeps the ship's alarm at its normal level; the klaxon belongs to the cutscene
                _sfx.CinematicAlarm(false);
                _sfx.Loop("ship_alarm", true);
            }
            if (_cineVp != null) { _cineVp.Stop(); _cineVp.clip = null; }
            yield return Fade(1f, 0.3f);
            _ui.HideScreen();
            InCutscene = false;
        }

        // ------------------------------------------------------------------ the scenes

        /// <summary>The world in 2177, then you: the youngest trainee boards the Kestrel, and that night the first orders arrive.
        /// The game then starts the next morning in the cadet's quarters (ORBI's greeting, the radio report).</summary>
        IEnumerator LdCinIntro() => LdCinema(
            Cut("p01_earth", "The year 2177.", "ปี 2177", 0.8f, "whoosh"),
            Cut("p02_moon", "People have built towns on the Moon,", "มนุษย์สร้างเมืองบนดวงจันทร์", 0.3f),
            Cut("p03_mars", "bases on Mars,", "ตั้งฐานบนดาวอังคาร", 0.3f),
            Cut("p04_jupiter", "and ships that reach as far as Jupiter.", "และส่งยานไปไกลถึงดาวพฤหัส", 0.5f),
            Cut("p06_walk", "You are the youngest trainee pilot of your class. Today you join the ship Kestrel.", "คุณคือนักบินฝึกหัดที่อายุน้อยที่สุดในรุ่น วันนี้คุณย้ายขึ้นยาน Kestrel", 1.2f),
            Cut("p05_cabin", "That night your first job arrives: send the morning report by radio.", "คืนนั้นงานแรกมาถึง: พรุ่งนี้เช้าส่งรายงานทางวิทยุ", 1.2f));

        /// <summary>Plays while the cadet rests on the lounge sofa after the report. Afterwards the 60-second scramble starts.</summary>
        IEnumerator LdCinImpact() => LdCinema(
            Cut("i01_debris", "Space debris! One rock clips the hull.", "ขยะอวกาศ! ก้อนหนึ่งเฉียดชนตัวยาน", 0.4f, "whoosh"),
            Cut("i02_hit", "IMPACT!", "โดนชน!", 0.6f),
            Cut("i03_run", "The hull is leaking. 60 seconds: load supplies into the lifeboat!", "ยานรั่ว! มีเวลา 60 วินาที ขนเสบียงขึ้นยานชูชีพ", 0.8f, "rumble", 0.3f));

        /// <summary>After the scramble: away from the Kestrel, the real Mars landing steps, then the old base where the week begins.</summary>
        IEnumerator LdCinLaunch() => LdCinema(
            Cut("l01_away", "The lifeboat breaks free of the Kestrel.", "ยานชูชีพหลุดออกจากยาน Kestrel ได้ทัน", 0.3f, "whoosh"),
            Cut("l02_entry", "Entry: the heat shield glows at 1,300 °C.", "เข้าชั้นบรรยากาศ · โล่กันความร้อนร้อนถึง 1,300 °C", 0.6f, null, 0.5f),
            Cut("l03_chute", "The parachute opens and slows us down.", "ร่มชูชีพกาง · ชะลอความเร็วลง", 0.3f, "whoosh"),
            Cut("l04_land", "Braking rockets... touchdown!", "จรวดชะลอ · แตะพื้นแล้ว!", 0.4f, null, 0.3f),
            Cut("l05_base", "An old, empty base. Fix the radio, call for help, and hold on for 7 days.", "ฐานเก่าที่ไม่มีใคร · ซ่อมวิทยุ ส่งสัญญาณขอความช่วยเหลือ แล้วอยู่ให้รอด 7 วัน", 1.6f));

        IEnumerator LdCinWin() => LdCinema(
            Cut("w01_rescue", "A rescue ship! They heard your radio call.", "ยานกู้ภัยมาแล้ว! พวกเขาได้ยินสัญญาณวิทยุของคุณ", 0.8f, "whoosh"),
            Cut("w02_home", "You're going home.", "คุณได้กลับบ้านแล้ว", 1.2f, "rumble"));

        IEnumerator LdCinLose() => LdCinema(
            Cut("x01_dusk", "One by one, the base's systems go dark.", "ระบบของฐานดับลงทีละอย่าง", 0.8f),
            Cut("x02_stars", "Not this time. Every mission teaches something. Try again.", "ครั้งนี้ยังไม่สำเร็จ ทุกภารกิจสอนอะไรเราเสมอ ลองอีกครั้ง", 1.4f));
    }
}
