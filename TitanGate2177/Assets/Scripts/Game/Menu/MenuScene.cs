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
        /// <summary>How far the push also slides its point toward the middle of the view (0 = it stays put and grows).</summary>
        public float PushCenter;
        /// <summary>Walking in: the airlock light comes on (DoorGlow), the camera passes the astronaut and the foreground
        /// (PassBy), the door's light spills toward the camera (Spill) and finally fills the view (Bloom). All 0..1.</summary>
        public float DoorGlow, PassBy, Spill, Bloom;
        public static readonly Color DoorLightColor = new Color(1f, 0.8f, 0.56f);
        /// <summary>True when the scene has an airlock door sequence (a play-once sheet with role "door").</summary>
        public bool HasDoor => _layers.Exists(l => l.Def.Role == "door" && l.Def.IsSheet);
        /// <summary>Open the airlock door once; it rests on its closed first frame until then.</summary>
        public void PlayDoor() { if (_doorT < 0) _doorT = 0; }
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
        }

        readonly List<LayerView> _layers = new List<LayerView>();
        readonly List<(VisualElement e, float ph, float speed)> _beacons = new List<(VisualElement, float, float)>();
        readonly VisualElement _farDust, _nearDust, _grain, _dim, _black;
        VisualElement _sun, _earth;
        readonly List<Mote> _motes = new List<Mote>();
        Vector2 _look; float _dim01, _grainT, _sheetT;
        readonly System.Random _rng = new System.Random(2177);
        MenuQuality _builtFor;
        // outdoor life: wind gusts carrying dust ribbons, slow sun rays, the odd meteor, and the airlock
        readonly bool _outdoor;
        VisualElement _gustBack, _gustFront, _rays, _rays2, _meteor, _doorGlow, _bloom, _scrim, _bottomFade, _vignette;
        float _gust, _gustX, _raysRot, _meteorT = -1, _nextMeteor, _mDur, _mDist, _doorT = -1;
        Vector2 _mP0, _mDir;

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
                if (!string.IsNullOrEmpty(def.Video)) StartVideo(v);
                Root.Add(v.Box); _layers.Add(v);
            }
            if (!farDustAdded) Root.Add(_farDust);
            Root.Add(_nearDust);

            _outdoor = spec.Name != "ship" && spec.Name != "space";
            if (_outdoor)
            {
                // wind-blown dust: a ribbon low over the plain behind the astronaut, a closer one in front
                _gustBack = Ribbon(0.57f, 0.17f); _farDust.Add(_gustBack);
                _gustFront = Ribbon(0.75f, 0.27f); _nearDust.Add(_gustFront);
                _nextMeteor = 7f + (float)_rng.NextDouble() * 6f;
            }

            // slow crepuscular rays behind the sun glow (pale blue: a Martian sunset)
            if (spec.Sun != null && _outdoor)
            {
                _rays = RaysEl(0.62f); Attach(_rays, spec.Sun.P, spec.Sun.Layer);
                _rays2 = RaysEl(0.4f); Attach(_rays2, spec.Sun.P, spec.Sun.Layer);
            }
            if (_outdoor && _layers.Count > 0)
            {
                _meteor = new VisualElement { pickingMode = PickingMode.Ignore };
                _meteor.style.position = Position.Absolute;
                _meteor.style.width = Length.Percent(9); _meteor.style.height = Length.Percent(0.45f);
                _meteor.style.backgroundImage = new StyleBackground(MenuFx.Streak());
                _meteor.style.unityBackgroundImageTintColor = new Color(0.86f, 0.93f, 1f, 1f);
                _meteor.style.transformOrigin = new TransformOrigin(Length.Percent(100), Length.Percent(50));
                _meteor.style.opacity = 0;
                (_layers.Find(l => l.Def.Name == "sky") ?? _layers[0]).Box.Add(_meteor);   // in the sky: hills and the base pass in front of it
            }

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

            // the airlock light, off until the player chooses Continue
            {
                var door = _layers.Find(l => l.Def.Role == "door");
                var at = spec.DoorLight != null ? spec.DoorLight.P : spec.Airlock;
                string layer = spec.DoorLight?.Layer ?? door?.Def.Name ?? "base";
                _doorGlow = Glow(new Color(1f, 0.9f, 0.74f, 1f));
                _doorGlow.style.width = Length.Percent(8); _doorGlow.style.height = Length.Percent(8 * spec.Aspect);
                _doorGlow.style.opacity = 0;
                Attach(_doorGlow, at, layer);
            }

            _scrim = T.Fill(); _scrim.style.backgroundImage = new StyleBackground(MenuFx.Scrim(T.BgDeep)); Root.Add(_scrim);
            _bottomFade = T.Fill(); _bottomFade.style.top = Length.Percent(80); _bottomFade.style.backgroundImage = new StyleBackground(MenuFx.BottomFade(T.BgDeep)); Root.Add(_bottomFade);
            _vignette = T.Fill(); _vignette.style.backgroundImage = new StyleBackground(MenuFx.Vignette()); Root.Add(_vignette);
            _grain = T.Fill(); _grain.style.backgroundImage = new StyleBackground(MenuFx.Grain());
            _grain.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            _grain.style.backgroundSize = new BackgroundSize(256, 256);
            _grain.style.opacity = 0.028f; Root.Add(_grain);
            _dim = T.Fill(); _dim.style.backgroundColor = new Color(T.BgDeep.r, T.BgDeep.g, T.BgDeep.b, 1); _dim.style.opacity = 0; Root.Add(_dim);
            _bloom = T.Fill(); _bloom.style.backgroundColor = DoorLightColor; _bloom.style.opacity = 0; Root.Add(_bloom);
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

        void StopVideos()
        {
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

        /// <summary>A full-width band of tiling wind-blown dust (scrolled by Tick).</summary>
        static VisualElement Ribbon(float top, float height)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute; e.style.left = 0; e.style.right = 0;
            e.style.top = Length.Percent(top * 100); e.style.height = Length.Percent(height * 100);
            e.style.backgroundImage = new StyleBackground(MenuFx.Gust());
            e.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.NoRepeat);
            e.style.unityBackgroundImageTintColor = new Color(1f, 0.83f, 0.66f, 1f);   // sunlit dust, paler than the ground
            e.style.opacity = 0;
            return e;
        }

        /// <summary>A square ray disc, sized as a fraction of the layer's width.</summary>
        VisualElement RaysEl(float size)
        {
            var e = Glow(new Color(0.82f, 0.9f, 1f, 1f));
            e.style.backgroundImage = new StyleBackground(MenuFx.Rays());
            e.style.width = Length.Percent(size * 100); e.style.height = Length.Percent(size * 100 * Spec.Aspect);
            e.style.opacity = 0;
            return e;
        }

        void FireMeteor()
        {
            _meteorT = 0;
            _mP0 = new Vector2(0.52f + (float)_rng.NextDouble() * 0.2f, 0.05f + (float)_rng.NextDouble() * 0.14f);   // upper sky, right of the logo
            float ang = (14f + (float)_rng.NextDouble() * 16f) * Mathf.Deg2Rad;                                    // falling to the right
            _mDir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * Spec.Aspect);                                     // box units: y is a fraction of the height
            _mDist = 0.12f + (float)_rng.NextDouble() * 0.08f;
            _mDur = 0.55f + (float)_rng.NextDouble() * 0.35f;
            _meteor.style.rotate = new Rotate(new Angle(ang * Mathf.Rad2Deg, AngleUnit.Degree));
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
            bool space = Spec.Name == "ship" || Spec.Name == "space";   // in space: a few slow floating motes in the cabin light, not wind-blown dust
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
                float s = 1.06f + 0.008f * breathe * (1f - d * 0.5f) + Push * (1f + d * 0.3f) + PassBy * Mathf.Max(0f, d - 0.35f) * 2.2f;
                v.Box.style.scale = new Scale(new Vector3(s, s, 1));
                if (d >= 0.45f) v.Box.style.opacity = 1f - PassBy;   // the camera walks past the astronaut and the near rocks
                float px = Mathf.Lerp(0.6f, 4.2f, d), py = Mathf.Lerp(0.3f, 1.9f, d);
                float pk = 0.9f + d * 0.3f + PushCenter;
                v.Box.style.translate = new Translate(Length.Percent(-_look.x * px - push.x * pk + breathe * 0.25f * (1f - d)), Length.Percent(_look.y * py - push.y * pk));
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
                    if (!v.Def.IsSheet || v.Def.Role == "door") continue;
                    float fps = v.Def.Fps > 0 ? v.Def.Fps : 8f;
                    float pos = _sheetT * fps; int f = (int)pos % v.Def.Frames;
                    if (f != v.Frame) SetFrame(v, f);
                    v.B.style.opacity = pos - Mathf.Floor(pos);
                }
            }

            // the airlock door: closed first frame at rest; once asked, it plays through and holds open
            if (_doorT >= 0 && !reduced) _doorT += dt;
            foreach (var v in _layers)
            {
                if (v.Def.Role != "door" || !v.Def.IsSheet) continue;
                float pos = Mathf.Max(0, _doorT) * (v.Def.Fps > 0 ? v.Def.Fps : 16f);
                int f = Mathf.Min((int)pos, v.Def.Frames - 1);
                if (f != v.Frame) SetFrame(v, f);
                v.B.style.opacity = _doorT > 0 && f < v.Def.Frames - 1 ? pos - Mathf.Floor(pos) : 0;
            }
            if (_doorGlow != null)
            {
                _doorGlow.style.opacity = Mathf.Clamp01(Mathf.Max(DoorGlow * (0.85f + (reduced ? 0 : 0.15f * Mathf.Sin(t * 9f))), Spill));
                float gs = 1f + DoorGlow * 0.5f + Spill * Spill * 16f;   // the spill swells from the doorway until it fills the view
                _doorGlow.style.scale = new Scale(new Vector3(gs, gs, 1));
            }
            _bloom.style.opacity = Bloom;
            _nearDust.style.opacity = 1f - PassBy;
            // the readability shading is for the menu text: it lifts as the camera walks in
            _scrim.style.opacity = _bottomFade.style.opacity = 1f - PassBy * 0.9f;
            _vignette.style.opacity = 1f - PassBy * 0.6f;

            // wind: gusts come and go; dust ribbons and motes speed up with them
            if (_outdoor)
            {
                float g = Mathf.Sin(t * Mathf.PI * 2f / 13f) * 0.275f + 0.275f + Mathf.PerlinNoise(t * 0.11f, 7.3f) * 0.55f;
                _gust = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 1f, g));
                bool on = q != MenuQuality.Low;
                _gustBack.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                _gustFront.style.display = q == MenuQuality.High ? DisplayStyle.Flex : DisplayStyle.None;
                if (!reduced) _gustX += dt * w * (0.03f + 0.11f * _gust);
                float tb = w * 0.9f, tf = w * 1.35f;
                _gustBack.style.backgroundSize = new BackgroundSize(new Length(tb), new Length(100, LengthUnit.Percent));
                _gustBack.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, new Length(Mathf.Repeat(-_gustX * 0.6f - _look.x * 30f, tb)));
                _gustBack.style.opacity = reduced ? 0.12f : 0.1f + 0.42f * _gust;
                _gustFront.style.backgroundSize = new BackgroundSize(new Length(tf), new Length(100, LengthUnit.Percent));
                _gustFront.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, new Length(Mathf.Repeat(-_gustX * 1.2f - _look.x * 90f, tf)));
                _gustFront.style.opacity = reduced ? 0.07f : 0.06f + 0.28f * _gust;
            }
            if (_rays != null)
            {
                bool on = q != MenuQuality.Low;
                _rays.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                _rays2.style.display = q == MenuQuality.High ? DisplayStyle.Flex : DisplayStyle.None;
                if (!reduced) _raysRot += dt * 1.2f;   // about five minutes a turn
                float breath = reduced ? 1f : 0.85f + 0.15f * Mathf.Sin(t * 0.45f);
                _rays.style.rotate = new Rotate(new Angle(_raysRot, AngleUnit.Degree));
                _rays.style.opacity = (q == MenuQuality.High ? 0.26f : 0.18f) * breath;
                _rays2.style.rotate = new Rotate(new Angle(-_raysRot * 0.6f + 17f, AngleUnit.Degree));
                _rays2.style.opacity = 0.16f * (reduced ? 1f : 0.85f + 0.15f * Mathf.Sin(t * 0.31f + 2f));
            }
            if (_meteor != null)
            {
                if (_meteorT < 0)
                {
                    if (t >= _nextMeteor)
                    {
                        if (q != MenuQuality.Low && !reduced) FireMeteor();
                        else _nextMeteor = t + 20f;
                    }
                }
                else
                {
                    _meteorT += dt;
                    float k = _meteorT / _mDur;
                    if (k >= 1f || reduced)
                    {
                        _meteorT = -1; _meteor.style.opacity = 0;
                        _nextMeteor = t + 20f + (float)_rng.NextDouble() * 20f;   // one every 20 to 40 seconds
                    }
                    else
                    {
                        float e = 1f - (1f - k) * (1f - k);
                        var p = _mP0 + _mDir * (_mDist * e);
                        _meteor.style.left = Length.Percent(p.x * 100f - 9f); _meteor.style.top = Length.Percent(p.y * 100f - 0.225f);
                        _meteor.style.scale = new Scale(new Vector3(0.35f + 0.65f * Mathf.Clamp01(k * 3f), 1, 1));
                        _meteor.style.opacity = k < 0.15f ? k / 0.15f : Mathf.Pow(1f - (k - 0.15f) / 0.85f, 1.5f);
                    }
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
                if (!reduced) { m.x = Mathf.Repeat(m.x - m.speed * (1f + 1.6f * _gust) * dt, 1.05f); }
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
