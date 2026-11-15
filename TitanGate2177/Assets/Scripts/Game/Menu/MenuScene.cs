// The menu's "3D" backdrop. The scene is pre-rendered in Blender as several depth layers
// (fog, rim light, sun glow and depth of field are baked). The UI adds camera drift, per-layer
// parallax, the astronaut's idle loop (a sprite sheet, cross-faded between frames), a slow
// orbital drift for the planet seen from the ship, drifting dust, blinking lights, a twinkling
// Earth, vignette and grain.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;
using T = TitanGate.Game.Menu.MenuTokens;

namespace TitanGate.Game.Menu
{
    public enum MenuQuality { Low = 0, Medium = 1, High = 2 }

    public class MenuScene
    {
        public static MenuQuality Quality
        {
            get => (MenuQuality)Mathf.Clamp(PlayerPrefs.GetInt("menu_fx", (int)MenuQuality.High), 0, 2);
            set { PlayerPrefs.SetInt("menu_fx", (int)value); PlayerPrefs.Save(); }
        }

        public readonly VisualElement Root;
        public readonly SceneSpec Spec;
        /// <summary>Black cover used by the intro and the departure (0..1).</summary>
        public float Black { set => _black.style.opacity = value; }
        /// <summary>Extra camera push-in (0 = rest) and the point it moves toward.</summary>
        public float Push; public Vector2 PushTo = new Vector2(0.5f, 0.5f);
        /// <summary>Dim behind sub-menus (0..1).</summary>
        public float DimTarget;
        /// <summary>The black cover must sit above the menu UI, so the menu adds it after its own layers.</summary>
        public VisualElement BlackCover => _black;

        class LayerView
        {
            public SceneLayer Def;
            public VisualElement Box;      // canvas-sized, carries the parallax transform
            public Image A, B;             // B fades in over A between sheet frames
            public int Frame;
            public VideoPlayer Vp;         // a looping video drawn over the still A once its first frame is ready
            public Image V;
            public RenderTexture Rt;
            public float VideoIn;
            public bool IsLive;            // V shows the live 3D scene (MenuLive3D) instead of a video
        }

        readonly List<LayerView> _layers = new List<LayerView>();
        readonly List<(VisualElement e, float ph, float speed)> _beacons = new List<(VisualElement, float, float)>();
        readonly VisualElement _farDust, _nearDust, _grain, _dim, _black;
        VisualElement _sun, _earth;
        readonly List<Mote> _motes = new List<Mote>();
        Vector2 _look; float _dim01, _grainT, _sheetT;
        readonly System.Random _rng = new System.Random(2177);
        MenuQuality _builtFor;

        struct Mote { public VisualElement e; public float x, y, speed, phase, depth, size; }

        public MenuScene(SceneSpec spec)
        {
            Spec = spec;
            Root = T.Fill(); Root.style.overflow = Overflow.Hidden; Root.style.backgroundColor = T.BgDeep;
            _farDust = T.Fill();
            _nearDust = T.Fill();
            bool farDustAdded = false;
            foreach (var def in spec.Layers)
            {
                if (def.Tex == null) continue;
                // far dust drifts in front of the far layers but behind the astronaut and the foreground
                if (!farDustAdded && def.Depth >= 0.45f) { Root.Add(_farDust); farDustAdded = true; }
                var v = new LayerView { Def = def, Box = new VisualElement { pickingMode = PickingMode.Ignore } };
                v.Box.style.position = Position.Absolute;
                v.A = Img(def); v.B = Img(def); v.B.style.opacity = 0;
                v.Box.Add(v.A); if (def.IsSheet) v.Box.Add(v.B);
                if (def.Role == "planet") { v.A.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50)); }
                if (def.IsSheet) SetFrame(v, 0);
                if (def.Role == "live3d") { if (!StartLive(v) && !string.IsNullOrEmpty(def.Video)) StartVideo(v); }
                else if (!string.IsNullOrEmpty(def.Video)) StartVideo(v);
                Root.Add(v.Box); _layers.Add(v);
            }
            if (!farDustAdded) Root.Add(_farDust);
            Root.Add(_nearDust);

            // sun glow pulse, a twinkling Earth and blinking lights ride on their own layers
            if (spec.Sun != null)
            {
                _sun = Glow(new Color(1f, 0.86f, 0.62f, 0.3f));
                _sun.style.width = _sun.style.height = Length.Percent(Mathf.Clamp(spec.Sun.Glow > 0 ? spec.Sun.Glow : 0.12f, 0.04f, 0.3f) * 1.6f * 100f / spec.Aspect);
                Attach(_sun, spec.Sun.P, spec.Sun.Layer);
            }
            if (spec.Earth != null)
            {
                _earth = Glow(new Color(0.62f, 0.8f, 1f, 0.9f)); _earth.style.width = _earth.style.height = 22;
                Attach(_earth, spec.Earth.P, spec.Earth.Layer);
            }
            foreach (var b in spec.Beacons)
            {
                var e = Glow(new Color(b.Color.r, b.Color.g, b.Color.b, 0.9f)); e.style.width = e.style.height = 26;
                Attach(e, b.P, b.Layer);
                _beacons.Add((e, (float)_rng.NextDouble() * 6f, 0.4f + (float)_rng.NextDouble() * 0.35f));
            }

            var scrim = T.Fill(); scrim.style.backgroundImage = new StyleBackground(MenuFx.Scrim(T.BgDeep)); Root.Add(scrim);
            var bottom = T.Fill(); bottom.style.top = Length.Percent(80); bottom.style.backgroundImage = new StyleBackground(MenuFx.BottomFade(T.BgDeep)); Root.Add(bottom);
            var vig = T.Fill(); vig.style.backgroundImage = new StyleBackground(MenuFx.Vignette()); Root.Add(vig);
            _grain = T.Fill(); _grain.style.backgroundImage = new StyleBackground(MenuFx.Grain());
            _grain.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            _grain.style.backgroundSize = new BackgroundSize(256, 256);
            _grain.style.opacity = 0.028f; Root.Add(_grain);
            _dim = T.Fill(); _dim.style.backgroundColor = new Color(T.BgDeep.r, T.BgDeep.g, T.BgDeep.b, 1); _dim.style.opacity = 0; Root.Add(_dim);
            _black = T.Fill(); _black.style.backgroundColor = Color.black; _black.style.opacity = 0;   // added last by the menu, above the UI

            Root.RegisterCallback<GeometryChangedEvent>(_ => Layout());
            Root.RegisterCallback<DetachFromPanelEvent>(_ => StopVideos());
            BuildDust(Quality);
        }

        /// <summary>Play the layer's clip into a render texture shown over the still (the still covers the first frames).</summary>
        void StartVideo(LayerView v)
        {
            var clip = Resources.Load<VideoClip>(v.Def.Video);
            if (clip == null) { Debug.LogWarning("Menu video missing: " + v.Def.Video); return; }
            var go = new GameObject("MenuVideo_" + v.Def.Name);
            UnityEngine.Object.DontDestroyOnLoad(go);
            var vp = go.AddComponent<VideoPlayer>();
            vp.playOnAwake = false; vp.isLooping = true; vp.skipOnDrop = true; vp.waitForFirstFrame = true;
            vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.renderMode = VideoRenderMode.RenderTexture;
            v.Rt = new RenderTexture((int)clip.width, (int)clip.height, 0) { name = "MenuVideoRT" };
            v.Rt.Create();
            vp.targetTexture = v.Rt; vp.clip = clip;
            vp.Play();
            v.Vp = vp;
            v.V = Img(v.Def); v.V.image = v.Rt; v.V.style.opacity = 0;
            v.Box.Add(v.V);
        }

        /// <summary>The live 3D backdrop the player can turn and zoom; null when the scene has none or it failed.</summary>
        public MenuLive3D Live { get; private set; }

        static Vector2Int LiveSize(float aspect)
        {
            int w = Mathf.Max(Screen.width, Mathf.RoundToInt(Screen.height * aspect));
            return new Vector2Int(w, Mathf.RoundToInt(w / aspect));
        }

        bool StartLive(LayerView v)
        {
            var size = LiveSize(Spec.Aspect);
            Live = MenuLive3D.Create(size.x, size.y);
            if (Live == null) return false;
            v.IsLive = true;
            v.V = Img(v.Def); v.V.image = Live.Target; v.V.style.opacity = 0;
            v.Box.Add(v.V);
            return true;
        }

        void StopVideos()
        {
            if (Live != null) { UnityEngine.Object.Destroy(Live.gameObject); Live = null; }
            foreach (var v in _layers)
            {
                if (v.Vp != null) { v.Vp.Stop(); UnityEngine.Object.Destroy(v.Vp.gameObject); v.Vp = null; }
                if (v.Rt != null) { v.Rt.Release(); UnityEngine.Object.Destroy(v.Rt); v.Rt = null; }
            }
        }

        static Image Img(SceneLayer def)
        {
            var im = new Image { image = def.Tex, scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
            im.style.position = Position.Absolute;
            im.style.left = Length.Percent(def.Rect.xMin * 100); im.style.top = Length.Percent(def.Rect.yMin * 100);
            im.style.width = Length.Percent(def.Rect.width * 100); im.style.height = Length.Percent(def.Rect.height * 100);
            return im;
        }

        static VisualElement Glow(Color tint)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute; e.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            e.style.backgroundImage = new StyleBackground(MenuFx.Radial());
            e.style.unityBackgroundImageTintColor = tint;
            return e;
        }

        void Attach(VisualElement e, Vector2 p, string layer)
        {
            var v = _layers.Find(l => l.Def.Name == layer) ?? (_layers.Count > 0 ? _layers[0] : null);
            if (v == null) return;
            e.style.left = Length.Percent(p.x * 100); e.style.top = Length.Percent(p.y * 100);
            v.Box.Add(e);
        }

        static void SetFrame(LayerView v, int f)
        {
            var d = v.Def; f %= d.Frames;
            float w = 1f / d.Cols, h = 1f / d.Rows;
            int c = f % d.Cols, r = f / d.Cols;
            v.A.uv = new Rect(c * w, 1f - (r + 1) * h, w, h);
            int n = (f + 1) % d.Frames; c = n % d.Cols; r = n / d.Cols;
            v.B.uv = new Rect(c * w, 1f - (r + 1) * h, w, h);
            v.Frame = f;
        }

        /// <summary>Cover-fit every layer box to the panel; on ultrawide take a little more from the sky than the ground.</summary>
        void Layout()
        {
            float w = Root.resolvedStyle.width, h = Root.resolvedStyle.height;
            if (w <= 0 || h <= 0) return;
            float aw = w, ah = w / Spec.Aspect;
            if (ah < h) { ah = h; aw = h * Spec.Aspect; }
            foreach (var v in _layers)
            {
                v.Box.style.width = aw; v.Box.style.height = ah; v.Box.style.left = (w - aw) / 2; v.Box.style.top = (h - ah) * 0.55f;
            }
        }

        void BuildDust(MenuQuality q)
        {
            _builtFor = q;
            foreach (var m in _motes) m.e.RemoveFromHierarchy();
            _motes.Clear();
            bool space = Spec.Name == "ship" || Spec.Name == "space" || Spec.Name == "live";   // in space: a few slow floating motes in the cabin light, not wind-blown dust
            int far = (q == MenuQuality.Low ? 16 : q == MenuQuality.Medium ? 30 : 40) / (space ? 2 : 1), near = q == MenuQuality.High ? 10 : q == MenuQuality.Medium ? 4 : 0;
            for (int i = 0; i < far + near; i++)
            {
                bool isNear = i >= far;
                float depth = isNear ? 1f : (float)_rng.NextDouble() * 0.6f;
                float size = isNear ? 10 + (float)_rng.NextDouble() * 14 : 2 + depth * 5;
                var e = new VisualElement { pickingMode = PickingMode.Ignore }; e.style.position = Position.Absolute; e.style.width = e.style.height = size;
                e.style.backgroundImage = new StyleBackground(MenuFx.Mote());
                e.style.unityBackgroundImageTintColor = space ? new Color(1f, 0.9f, 0.75f, isNear ? 0.1f : 0.18f + depth * 0.25f) : new Color(1f, 0.74f, 0.52f, isNear ? 0.16f : 0.22f + depth * 0.35f);
                (isNear ? _nearDust : _farDust).Add(e);
                float speed = (0.006f + depth * 0.022f + (isNear ? 0.02f : 0)) * (space ? 0.25f : 1f);
                _motes.Add(new Mote { e = e, x = (float)_rng.NextDouble(), y = (float)_rng.NextDouble(), speed = speed, phase = (float)_rng.NextDouble() * 6f, depth = depth, size = size });
            }
        }

        public void Tick(float dt, float t, Vector2 look, bool reduced)
        {
            var q = Quality;
            if (q != _builtFor) BuildDust(q);
            float w = Root.resolvedStyle.width, h = Root.resolvedStyle.height;
            if (w <= 0) return;

            // camera: slow breathing dolly + smoothed parallax (about ±2° of yaw) + push-in; near layers move more
            if (reduced) look = Vector2.zero;
            _look = Vector2.Lerp(_look, look, 1f - Mathf.Exp(-3.2f * dt));
            float breathe = reduced ? 0 : Mathf.Sin(t * Mathf.PI * 2f / 12f);
            var push = (PushTo - new Vector2(0.5f, 0.5f)) * Push * 100f;
            foreach (var v in _layers)
            {
                float d = v.Def.Depth;
                float s = 1.06f + 0.008f * breathe * (1f - d * 0.5f) + Push * (1f + d * 0.3f);
                v.Box.style.scale = new Scale(new Vector3(s, s, 1));
                float px = Mathf.Lerp(0.6f, 4.2f, d), py = Mathf.Lerp(0.3f, 1.9f, d);
                v.Box.style.translate = new Translate(Length.Percent(-_look.x * px - push.x * (0.9f + d * 0.3f) + breathe * 0.25f * (1f - d)), Length.Percent(_look.y * py - push.y * (0.9f + d * 0.3f)));
                if (v.Def.Role == "planet" && !reduced)
                {
                    // seen from orbit: the planet slides and turns very slowly in the window
                    v.A.style.rotate = new Rotate(new Angle(Mathf.Sin(t * Mathf.PI * 2f / 90f) * 1.6f, AngleUnit.Degree));
                    v.A.style.translate = new Translate(Length.Percent(Mathf.Sin(t * Mathf.PI * 2f / 70f) * 1.2f), Length.Percent(Mathf.Cos(t * Mathf.PI * 2f / 85f) * 0.8f));
                }
            }

            // video layers fade in over their still once the first frame has been drawn
            foreach (var v in _layers)
            {
                if (v.IsLive && Live != null)
                {
                    Live.Reduced = reduced;
                    var size = LiveSize(Spec.Aspect);
                    Live.Resize(size.x, size.y);
                    if (v.V.image != Live.Target) v.V.image = Live.Target;
                    v.VideoIn = Mathf.MoveTowards(v.VideoIn, 1f, dt / 0.5f);
                    v.V.style.opacity = v.VideoIn;
                    continue;
                }
                if (v.Vp == null || v.V == null) continue;
                if (v.Vp.isPlaying && v.Vp.frame > 1) v.VideoIn = Mathf.MoveTowards(v.VideoIn, 1f, dt / 0.4f);
                v.V.style.opacity = v.VideoIn;
                if (reduced && v.Vp.isPlaying) v.Vp.Pause(); else if (!reduced && !v.Vp.isPlaying && v.Vp.isPrepared) v.Vp.Play();
            }

            // idle loops: step the sheet at its fps and cross-fade the next frame in over an opaque current frame
            if (!reduced)
            {
                _sheetT += dt;
                foreach (var v in _layers)
                {
                    if (!v.Def.IsSheet) continue;
                    float fps = v.Def.Fps > 0 ? v.Def.Fps : 8f;
                    float pos = _sheetT * fps; int f = (int)pos % v.Def.Frames;
                    if (f != v.Frame) SetFrame(v, f);
                    v.B.style.opacity = pos - Mathf.Floor(pos);
                }
            }

            if (_sun != null) _sun.style.opacity = reduced ? 0.8f : 0.72f + Mathf.Sin(t * 0.8f) * 0.12f;
            if (_earth != null) _earth.style.opacity = reduced ? 0.85f : 0.7f + 0.3f * Mathf.PerlinNoise(t * 1.7f, 3.1f);   // atmospheric twinkle
            foreach (var (e, ph, sp) in _beacons)
            {
                float k = Mathf.Repeat(t * sp + ph, 1f);
                e.style.opacity = k < 0.12f ? Mathf.Sin(k / 0.12f * Mathf.PI) : 0.12f;
            }

            for (int i = 0; i < _motes.Count; i++)
            {
                var m = _motes[i];
                if (!reduced) { m.x = Mathf.Repeat(m.x - m.speed * dt, 1.05f); }
                float wob = reduced ? 0 : Mathf.Sin(t * 0.7f + m.phase) * 0.012f;
                float px = (m.x - 0.025f) * w - _look.x * (20f + m.depth * 70f), py = (m.y + wob) * h + _look.y * (10f + m.depth * 30f);
                m.e.style.left = px; m.e.style.top = py;
                _motes[i] = m;
            }

            _grain.style.display = q == MenuQuality.Low ? DisplayStyle.None : DisplayStyle.Flex;
            _grainT += dt;
            if (_grainT > 1f / 24f && !reduced)
            {
                _grainT = 0;
                _grain.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, _rng.Next(256));
                _grain.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top, _rng.Next(256));
            }

            _dim01 = Mathf.MoveTowards(_dim01, DimTarget, dt / T.Slow);
            _dim.style.opacity = _dim01 * 0.55f;
        }
    }
}
