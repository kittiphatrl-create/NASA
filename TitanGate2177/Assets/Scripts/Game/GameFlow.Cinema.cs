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
    /// LAST DOCK cutscenes: each scene is a list of shots. A shot is one Blender render (Resources/Sprites/KeyArt/c01..c13,
    /// built by Art/Blender/cinematics_build.py) with a slow push or pan, a cross-fade from the last shot, letterbox bars,
    /// a short line, and live light on top: drifting dust in the light, alarm or fire flicker, film grain and a vignette.
    /// Space/click skips the scene.
    /// </summary>
    public partial class GameFlow
    {
        enum CinFx { None, Dust, Embers, Alarm, Fire, Stars }

        struct CinShot
        {
            public string art, en, th, sfx;
            public float seconds, zoomFrom, zoomTo, panX, shake;
            public bool flash;
            public CinFx fx;
        }

        static CinShot Cut(string art, float seconds, string en, string th, float zoomFrom = 1.04f, float zoomTo = 1.14f, float panX = 0f,
                           string sfx = null, float shake = 0, bool flash = false, CinFx fx = CinFx.None)
            => new CinShot { art = art, seconds = seconds, en = en, th = th, zoomFrom = zoomFrom, zoomTo = zoomTo, panX = panX, sfx = sfx, shake = shake, flash = flash, fx = fx };

        static Texture2D _cinGrain, _cinVignette, _cinMote;

        /// <summary>Small generated textures for the overlays: tiling grain, a soft vignette, a round soft mote.</summary>
        static void LdCinTextures()
        {
            if (_cinGrain != null) return;
            var rng = new System.Random(2177);
            _cinGrain = new Texture2D(256, 256, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            var px = new Color32[256 * 256];
            for (int i = 0; i < px.Length; i++) { byte v = (byte)rng.Next(0, 256); px[i] = new Color32(v, v, v, (byte)rng.Next(0, 70)); }
            _cinGrain.SetPixels32(px); _cinGrain.Apply();
            _cinVignette = new Texture2D(128, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float dx = (x - 63.5f) / 63.5f, dy = (y - 63.5f) / 63.5f;
                    float a = Mathf.Clamp01((dx * dx + dy * dy - 0.35f) / 1.3f);
                    _cinVignette.SetPixel(x, y, new Color(0, 0, 0, a * a * 0.85f));
                }
            _cinVignette.Apply();
            _cinMote = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float d = Mathf.Sqrt((x - 15.5f) * (x - 15.5f) + (y - 15.5f) * (y - 15.5f)) / 15.5f;
                    _cinMote.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - d), 2.2f)));
                }
            _cinMote.Apply();
        }

        class CinMote { public VisualElement el; public Vector2 p, v; public float size, phase; }

        IEnumerator LdCinema(params CinShot[] shots)
        {
            InCutscene = true;
            LdCinTextures();
            bool skip = false;
            VisualElement picA = null, picB = null, flash = null, tint = null, grain = null, motesLayer = null; Label line = null;
            var motes = new List<CinMote>();
            _ui.ShowScreen(root =>
            {
                root.style.backgroundColor = Color.black;
                VisualElement Full()
                {
                    var e = new VisualElement { pickingMode = PickingMode.Ignore };
                    e.style.position = Position.Absolute; e.style.left = 0; e.style.right = 0; e.style.top = 0; e.style.bottom = 0;
                    return e;
                }
                picA = Full(); picA.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover); root.Add(picA);
                picB = Full(); picB.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover); picB.style.opacity = 0; root.Add(picB);
                motesLayer = Full(); motesLayer.style.overflow = Overflow.Hidden; root.Add(motesLayer);
                tint = Full(); tint.style.backgroundColor = Color.clear; root.Add(tint);
                var vig = Full(); vig.style.backgroundImage = new StyleBackground(_cinVignette); vig.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover); root.Add(vig);
                grain = Full(); grain.style.backgroundImage = new StyleBackground(_cinGrain);
                grain.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
                grain.style.backgroundSize = new BackgroundSize(new Length(256, LengthUnit.Pixel), new Length(256, LengthUnit.Pixel));
                grain.style.opacity = 0.35f; root.Add(grain);
                foreach (var top in new[] { true, false })
                {
                    var bar = new VisualElement { pickingMode = PickingMode.Ignore }; bar.style.position = Position.Absolute; bar.style.left = 0; bar.style.right = 0; bar.style.height = Length.Percent(11);
                    bar.style.backgroundColor = Color.black; if (top) bar.style.top = 0; else bar.style.bottom = 0; root.Add(bar);
                }
                var lineBox = new VisualElement { pickingMode = PickingMode.Ignore }; lineBox.style.position = Position.Absolute; lineBox.style.left = 0; lineBox.style.right = 0; lineBox.style.bottom = Length.Percent(13);
                lineBox.style.alignItems = Align.Center;
                line = _ui.L("", 38, true, GameUI.CText); line.pickingMode = PickingMode.Ignore; line.style.unityTextAlign = TextAnchor.MiddleCenter; line.style.maxWidth = 1400; line.style.whiteSpace = WhiteSpace.Normal;
                line.style.unityTextOutlineWidth = 1.2f; line.style.unityTextOutlineColor = new Color(0, 0, 0, 0.9f); LdFont(line);
                lineBox.Add(line); root.Add(lineBox);
                flash = Full(); flash.style.backgroundColor = new Color(1f, 0.95f, 0.85f, 0f); root.Add(flash);
                var skipBtn = _ui.B(TT("Skip  ▸▸", "ข้าม  ▸▸"), () => skip = true, 20); skipBtn.style.position = Position.Absolute; skipBtn.style.right = 36; skipBtn.style.top = Length.Percent(3); root.Add(skipBtn);
            });
            _ui.FadeTo(0f, 0.4f);
            var rnd = new System.Random(7);
            bool first = true;
            VisualElement cur = picA, prev = picB;
            foreach (var s in shots)
            {
                if (skip) break;
                // the new shot goes on the back layer and fades in over the old one
                (cur, prev) = (prev, cur);
                var tex = LdKeyArt(s.art);
                if (tex != null) cur.style.backgroundImage = new StyleBackground(tex);
                // picB sits above picA: fade picB in when it gets the new shot, or out when picA does
                if (first) { picA.style.opacity = 1; picB.style.opacity = cur == picB ? 1 : 0; }
                else { picA.style.opacity = 1; picB.style.opacity = cur == picB ? 0 : 1; }
                line.text = TT(s.en, s.th); line.style.opacity = 0;
                if (!string.IsNullOrEmpty(s.sfx)) _sfx.Play(s.sfx);
                if (s.flash) flash.style.backgroundColor = new Color(1f, 0.95f, 0.85f, 0.95f);
                LdCinMotes(motesLayer, motes, s.fx, rnd);
                float t = 0, dur = _playtest ? Mathf.Min(1f, s.seconds) : s.seconds;
                bool shotTaken = false;
                while (t < dur && !skip)
                {
                    float dt = Time.unscaledDeltaTime; t += dt;
                    float k = Mathf.SmoothStep(0, 1, t / dur);
                    float z = Mathf.Lerp(s.zoomFrom, s.zoomTo, k);
                    float sx = 0, sy = 0;
                    if (s.shake > 0 && !Settings.ReducedMotion) { float a = s.shake * (1 - k * 0.7f); sx = (Mathf.PerlinNoise(t * 20, 0) - .5f) * 2 * a; sy = (Mathf.PerlinNoise(0, t * 20) - .5f) * 2 * a; }
                    cur.style.scale = new Scale(new Vector3(z, z, 1));
                    cur.style.translate = new Translate(Length.Percent(s.panX * (k - 0.5f) * 10 + sx), Length.Percent(sy));
                    if (!first) { float f = Mathf.Clamp01(t / 0.45f); picB.style.opacity = cur == picB ? f : 1 - f; }
                    line.style.opacity = Mathf.Clamp01((t - 0.2f) / 0.5f) * Mathf.Clamp01((dur - t) / 0.4f);
                    var fc = flash.resolvedStyle.backgroundColor; if (fc.a > 0) { fc.a = Mathf.Max(0, fc.a - dt * 1.6f); flash.style.backgroundColor = fc; }
                    tint.style.backgroundColor = LdCinTint(s.fx, Time.unscaledTime);
                    grain.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, rnd.Next(0, 256));
                    grain.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top, rnd.Next(0, 256));
                    LdCinMoveMotes(motes, s.fx, dt, Time.unscaledTime);
                    var kb = UnityEngine.InputSystem.Keyboard.current;
                    if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) skip = true;
                    if (_playtest && !shotTaken && t > dur * 0.7f) { shotTaken = true; yield return Shot("cin_" + s.art); }
                    yield return null;
                }
                first = false;
            }
            yield return Fade(1f, 0.35f);
            _ui.HideScreen();
            InCutscene = false;
        }

        /// <summary>Colour wash on top of the still: a red alarm pulse, a warm fire flicker, or nothing.</summary>
        static Color LdCinTint(CinFx fx, float time)
        {
            if (Settings.ReducedMotion) return Color.clear;
            switch (fx)
            {
                case CinFx.Alarm: { float p = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(time * 5.2f), 3f); return new Color(0.9f, 0.05f, 0.02f, 0.18f * p); }
                case CinFx.Fire: { float n = Mathf.PerlinNoise(time * 7f, 0.3f); return new Color(1f, 0.45f, 0.1f, 0.03f + 0.09f * n); }
                default: return Color.clear;
            }
        }

        /// <summary>Fill the mote layer for this shot: dust in the light, sparks drifting up, or a few twinkling stars.</summary>
        void LdCinMotes(VisualElement layer, List<CinMote> motes, CinFx fx, System.Random rnd)
        {
            layer.Clear(); motes.Clear();
            int n = fx == CinFx.Dust ? 46 : fx == CinFx.Embers || fx == CinFx.Fire ? 34 : fx == CinFx.Stars ? 24 : 0;
            Color col = fx == CinFx.Dust ? new Color(1f, 0.86f, 0.66f) : fx == CinFx.Stars ? new Color(0.8f, 0.9f, 1f) : new Color(1f, 0.6f, 0.25f);
            for (int i = 0; i < n; i++)
            {
                var m = new CinMote { el = new VisualElement { pickingMode = PickingMode.Ignore } };
                m.size = fx == CinFx.Stars ? 3 + (float)rnd.NextDouble() * 4 : 3 + (float)rnd.NextDouble() * (fx == CinFx.Dust ? 9 : 6);
                m.p = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
                m.v = fx == CinFx.Dust ? new Vector2(0.006f + (float)rnd.NextDouble() * 0.012f, -0.004f + (float)rnd.NextDouble() * 0.008f)
                    : fx == CinFx.Stars ? Vector2.zero
                    : new Vector2(-0.01f + (float)rnd.NextDouble() * 0.02f, -0.04f - (float)rnd.NextDouble() * 0.06f);
                m.phase = (float)rnd.NextDouble() * 10f;
                m.el.style.position = Position.Absolute; m.el.style.width = m.size; m.el.style.height = m.size;
                m.el.style.backgroundImage = new StyleBackground(_cinMote);
                m.el.style.unityBackgroundImageTintColor = col;
                layer.Add(m.el); motes.Add(m);
            }
        }

        static void LdCinMoveMotes(List<CinMote> motes, CinFx fx, float dt, float time)
        {
            foreach (var m in motes)
            {
                if (!Settings.ReducedMotion) m.p += m.v * dt;
                m.p.x = Mathf.Repeat(m.p.x, 1f); m.p.y = Mathf.Repeat(m.p.y, 1f);
                float wob = fx == CinFx.Dust ? Mathf.Sin(time * 0.7f + m.phase) * 0.006f : 0f;
                m.el.style.left = Length.Percent((m.p.x + wob) * 100f); m.el.style.top = Length.Percent(m.p.y * 100f);
                float a = fx == CinFx.Stars ? 0.35f + 0.65f * Mathf.Pow(0.5f + 0.5f * Mathf.Sin(time * 2.3f + m.phase * 3f), 4f)
                        : fx == CinFx.Dust ? 0.25f + 0.35f * (0.5f + 0.5f * Mathf.Sin(time * 1.1f + m.phase))
                        : 0.5f + 0.5f * Mathf.PerlinNoise(time * 6f, m.phase);
                m.el.style.opacity = a;
            }
        }

        // ------------------------------------------------------------------ the scenes

        IEnumerator LdCinIntro() => LdCinema(
            Cut("c01_cruise", 4.5f, "2177. The ship Kestrel is three days from Mars.", "ปี 2177 ยาน Kestrel อีกสามวันจะถึงดาวอังคาร", 1.02f, 1.12f, 0.3f, "whoosh", fx: CinFx.Stars),
            Cut("c02_mars", 4f, "Mars. Our new home — if we get there.", "ดาวอังคาร บ้านใหม่ของเรา ถ้าเราไปถึง", 1.0f, 1.16f, -0.2f, fx: CinFx.Stars),
            Cut("c03_cadet", 4f, "You are the youngest cadet on board.", "คุณคือนักบินฝึกหัดที่อายุน้อยที่สุดบนยาน", 1.12f, 1.03f, -0.25f, fx: CinFx.Dust));

        IEnumerator LdCinImpact() => LdCinema(
            Cut("c04_debris", 3f, "Debris! A cloud of rock, right on our path.", "ขยะอวกาศ! ก้อนหินจำนวนมากพุ่งมาทางเรา", 1.0f, 1.22f, 0.4f, "alarm", 0.4f),
            Cut("c05_impact", 2.5f, "IMPACT.", "โดนชน!", 1.2f, 1.06f, 0f, "thud", 2.2f, true, CinFx.Fire),
            Cut("c06_alarm", 3f, "The hull is breached. Abandon ship!", "ตัวยานรั่ว สละยาน!", 1.04f, 1.14f, 0.15f, "rumble", 0.7f, fx: CinFx.Alarm));

        IEnumerator LdCinLaunch() => LdCinema(
            Cut("c07_launch", 3.5f, "The lifeboat breaks free.", "ยานชูชีพหลุดออกมาได้", 1.05f, 1.18f, -0.3f, "whoosh", 0.5f, fx: CinFx.Embers),
            Cut("c08_entry", 3.5f, "Mars rushes up to meet us...", "พื้นดาวอังคารพุ่งเข้ามาหา...", 1.16f, 1.02f, 0.2f, "rumble", 1.0f, fx: CinFx.Fire),
            Cut("c09_arrive", 4.5f, "An old, empty base. Rescue can reach us in 7 days.", "ฐานเก่าที่ไม่มีใคร ยานกู้ภัยจะมาถึงใน 7 วัน", 1.0f, 1.1f, 0.25f, "thud", 0.3f, fx: CinFx.Dust));

        IEnumerator LdCinWin() => LdCinema(
            Cut("c10_rescue", 4f, "A light drops through the red sky.", "แสงหนึ่งลอยลงมาจากฟ้าสีแดง", 1.0f, 1.12f, -0.2f, "whoosh", fx: CinFx.Dust),
            Cut("c11_home", 4.5f, "They heard us. We're going home.", "พวกเขาได้ยินเรา เราได้กลับบ้านแล้ว", 1.1f, 1.02f, 0.2f, fx: CinFx.Dust));

        IEnumerator LdCinLose() => LdCinema(
            Cut("c12_dusk", 4f, "Night falls over the quiet base.", "ความมืดปกคลุมฐานที่เงียบสงัด", 1.0f, 1.1f, 0.2f, fx: CinFx.Dust),
            Cut("c13_lone", 4f, "Every mission teaches something. Try again.", "ทุกภารกิจสอนอะไรเราเสมอ ลองอีกครั้ง", 1.1f, 1.02f, -0.2f, fx: CinFx.Stars));
    }
}
