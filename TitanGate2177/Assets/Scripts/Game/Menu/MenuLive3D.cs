// The main menu's live 3D backdrop: the Kestrel in orbit, Mars and Phobos, drifting asteroids and the galaxy sky,
// drawn in real time into a render texture that MenuScene shows behind the menu. The player can drag to turn the
// camera round the ship and scroll (or pinch) to zoom; when left alone it slowly circles the ship, like the old
// looping video. Everything comes from Art/Blender/menu_live.py (meshes, baked maps, sky panorama, layout JSON) and
// is lit by the menu's own shaders, so scene lights never affect it. If anything is missing, Create returns null
// and the menu keeps the video backdrop.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TitanGate.Game.Menu
{
    public class MenuLive3D : MonoBehaviour
    {
        [Serializable]
        class Rock { public int mesh; public float[] pos, axis, drift; public float scale, spin; }

        [Serializable]
        class Layout
        {
            public float[] target, shift, sunDir, sunColor, fillDir, fillColor, bounceDir, bounceColor;
            public float[] marsPos, marsRot, phobosPos, band, engines, engineAxis, navRed, navGreen, strobe;
            public float yaw, pitch, dist, hfov, sunPower, marsRadius, phobosRadius;
            public Rock[] rocks;
        }

        const string Dir = "MenuLive/";
        const int Layer = 30;                                  // a layer nothing else uses; other cameras never draw it
        static readonly Vector3 Origin = new Vector3(0, -6000, 0);

        public RenderTexture Target { get; private set; }
        public bool Reduced;                                    // Reduce Motion: no automatic turning or rocking

        Layout _l;
        Camera _cam;
        Transform _root, _ship, _sky, _mars;

        /// <summary>For playtests: where Mars and the ship sit on screen (viewport 0..1, z = distance).</summary>
        public string Probe() => _cam == null ? "no camera" :
            $"mars {_cam.WorldToViewportPoint(_mars.position)} ship {_cam.WorldToViewportPoint(_ship.position)} yaw {_yaw:F1} pitch {_pitch:F1} dist {_dist:F1} camLocal {_cam.transform.localPosition} marsLocal {_mars.localPosition}";
        readonly List<(Transform t, Vector3 basePos, Vector3 axis, float spin, Vector3 drift)> _rocks = new List<(Transform, Vector3, Vector3, float, Vector3)>();
        readonly List<(Transform t, Material m, Color c, float size)> _engineGlows = new List<(Transform, Material, Color, float)>();
        readonly List<Transform> _billboards = new List<Transform>();
        Material _navR, _navG, _strobe, _engineHalo;
        Vector3 _target;
        float _yaw, _pitch, _dist, _yawVel, _pitchVel, _zoomTo, _idle = 0f, _t;
        float _yaw0, _pitch0, _dist0;
        readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();

        public const float MinDist = 17f, MaxDist = 95f;

        /// <summary>Build the scene. Returns null (and cleans up) if a file or shader is missing.</summary>
        public static MenuLive3D Create(int width, int height)
        {
            var go = new GameObject("MenuLive3D");
            DontDestroyOnLoad(go);
            var live = go.AddComponent<MenuLive3D>();
            try
            {
                if (!live.Build()) { Destroy(go); return null; }
                live.Resize(width, height);
                return live;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Live menu scene failed, using the video: " + e.Message);
                Destroy(go);
                return null;
            }
        }

        // ------------------------------------------------------------------ loading

        static Vector3 V(float[] a, int i = 0) => new Vector3(a[i], a[i + 1], a[i + 2]);
        static Color C(float[] a, float k = 1f) => new Color(a[0] * k, a[1] * k, a[2] * k, 1);

        Mesh LoadMesh(string name, bool tangents)
        {
            var ta = Resources.Load<TextAsset>(Dir + name + ".mlm");
            if (ta == null) throw new FileNotFoundException(name + ".mlm");
            using (var r = new BinaryReader(new MemoryStream(ta.bytes)))
            {
                if (new string(r.ReadChars(4)) != "MLM1") throw new InvalidDataException(name);
                int nv = r.ReadInt32(), ni = r.ReadInt32();
                var p = new Vector3[nv]; var n = new Vector3[nv]; var uv = new Vector2[nv]; var idx = new int[ni];
                for (int i = 0; i < nv; i++) p[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                for (int i = 0; i < nv; i++) n[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                for (int i = 0; i < nv; i++) uv[i] = new Vector2(r.ReadSingle(), r.ReadSingle());
                for (int i = 0; i < ni; i++) idx[i] = r.ReadInt32();
                var m = new Mesh { name = name, indexFormat = nv > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
                m.vertices = p; m.normals = n; m.uv = uv; m.triangles = idx;
                if (tangents) m.RecalculateTangents();
                m.RecalculateBounds(); m.UploadMeshData(true);
                _owned.Add(m);
                return m;
            }
        }

        Texture2D LoadTex(string name, bool mips = true, bool linear = false)
        {
            var ta = Resources.Load<TextAsset>(Dir + name + ".jpg");
            if (ta == null) throw new FileNotFoundException(name + ".jpg");
            var t = new Texture2D(2, 2, TextureFormat.RGB24, mips, linear) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 6 };
            t.LoadImage(ta.bytes, true);
            _owned.Add(t);
            return t;
        }

        Material Mat(string shader)
        {
            var s = Resources.Load<Shader>(Dir + shader);
            if (s == null || !s.isSupported) throw new InvalidOperationException("shader " + shader);
            var m = new Material(s); _owned.Add(m); return m;
        }

        Transform Obj(string name, Mesh mesh, Material mat, Transform parent)
        {
            var go = new GameObject(name) { layer = Layer };
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return go.transform;
        }

        Mesh _quad;
        Transform Glow(string name, Vector3 local, Color c, float size, Transform parent, float falloff = 2.2f, float core = 0.6f)
        {
            var m = Mat("MenuLiveGlow"); m.SetColor("_Color", c); m.SetFloat("_Falloff", falloff); m.SetFloat("_Core", core);
            var t = Obj(name, _quad, m, parent);
            t.localPosition = local; t.localScale = Vector3.one * size;
            _billboards.Add(t);
            return t;
        }

        bool Build()
        {
            var json = Resources.Load<TextAsset>(Dir + "menu_live");
            if (json == null) return false;
            _l = JsonUtility.FromJson<Layout>(json.text);
            _root = transform; _root.position = Origin;

            _quad = new Mesh { name = "glowQuad" };
            _quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            _quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            _quad.triangles = new[] { 0, 2, 1, 0, 3, 2 }; _quad.RecalculateBounds(); _owned.Add(_quad);

            // lights, shared by every live shader
            Shader.SetGlobalVector("_MLSunDir", V(_l.sunDir).normalized);
            Shader.SetGlobalColor("_MLSunCol", C(_l.sunColor, 1.7f));
            Shader.SetGlobalVector("_MLFillDir", V(_l.fillDir).normalized);
            Shader.SetGlobalColor("_MLFillCol", C(_l.fillColor, 0.3f));
            Shader.SetGlobalVector("_MLBounceDir", V(_l.bounceDir).normalized);
            Shader.SetGlobalColor("_MLBounceCol", C(_l.bounceColor, 0.38f));
            Shader.SetGlobalColor("_MLAmb", new Color(0.07f, 0.075f, 0.11f));
            Shader.SetGlobalColor("_MLRim", new Color(0.32f, 0.36f, 0.6f));

            // sky: a sphere that follows the camera, drawn first
            var sphere = LoadMesh("sphere", false);
            var skyM = Mat("MenuLiveSky");
            var skyTex = LoadTex("sky", false); skyTex.wrapModeU = TextureWrapMode.Repeat; skyTex.filterMode = FilterMode.Bilinear;
            skyM.mainTexture = skyTex; skyM.SetVector("_BandDir", V(_l.band));
            _sky = Obj("Sky", sphere, skyM, _root); _sky.localScale = Vector3.one * 4000f;

            // Mars and Phobos
            var marsM = Mat("MenuLivePlanet"); marsM.mainTexture = LoadTex("mars_albedo"); marsM.SetFloat("_Gain", 0.62f);
            var mars = Obj("Mars", sphere, marsM, _root); _mars = mars;
            mars.localPosition = V(_l.marsPos); mars.localScale = Vector3.one * _l.marsRadius;
            mars.localRotation = new Quaternion(_l.marsRot[0], _l.marsRot[1], _l.marsRot[2], _l.marsRot[3]);
            if (_l.phobosRadius > 0)
            {
                var phM = Mat("MenuLivePlanet"); phM.mainTexture = LoadTex("phobos_albedo"); phM.SetColor("_Atm", Color.clear); phM.SetFloat("_Gain", 0.55f);
                var ph = Obj("Phobos", sphere, phM, _root);
                ph.localPosition = V(_l.phobosPos); ph.localScale = new Vector3(1.25f, 0.95f, 1f) * _l.phobosRadius;
            }

            // the Kestrel
            var shipM = Mat("MenuLiveLit");
            shipM.mainTexture = LoadTex("ship_albedo");
            shipM.SetTexture("_EmitTex", LoadTex("ship_emit"));
            shipM.SetTexture("_NormTex", LoadTex("ship_normal", true, true));
            _ship = Obj("Kestrel", LoadMesh("ship", true), shipM, _root);

            // engines: a hot core and a tapering plume of glows behind each nozzle, and one wide halo
            var axis = V(_l.engineAxis).normalized;
            for (int e = 0; e + 2 < _l.engines.Length; e += 3)
            {
                var p = V(_l.engines, e);
                AddEngineGlow(p, new Color(0.85f, 0.95f, 1f, 1f), 2.1f, 3.2f, 0.6f);
                // the plume: soft overlapping glows with no hot centres, so it reads as one jet, not a row of dots
                for (int k = 1; k <= 7; k++)
                    AddEngineGlow(p + axis * (0.55f * k), new Color(0.3f, 0.58f, 1f, 0.34f - k * 0.04f), 2.4f - k * 0.18f, 1.5f, 0f);
            }
            var halo = Glow("EngineHalo", V(_l.engines, 0) * 0.5f + V(_l.engines, 9) * 0.5f + axis * 1.5f, new Color(0.25f, 0.5f, 1f, 0.35f), 12f, _ship, 2.6f);
            _engineHalo = halo.GetComponent<MeshRenderer>().sharedMaterial;
            _navR = Glow("NavRed", V(_l.navRed), new Color(1f, 0.12f, 0.06f, 1f), 1.6f, _ship).GetComponent<MeshRenderer>().sharedMaterial;
            _navG = Glow("NavGreen", V(_l.navGreen), new Color(0.15f, 1f, 0.3f, 1f), 1.6f, _ship).GetComponent<MeshRenderer>().sharedMaterial;
            _strobe = Glow("Strobe", V(_l.strobe), new Color(1f, 0.97f, 0.92f, 1f), 2.6f, _ship).GetComponent<MeshRenderer>().sharedMaterial;

            // asteroids
            var rockMeshes = new Mesh[3]; var rockMats = new Material[3];
            for (int i = 0; i < 3; i++)
            {
                rockMeshes[i] = LoadMesh("rock" + i, false);
                rockMats[i] = Mat("MenuLiveLit"); rockMats[i].mainTexture = LoadTex("rock" + i + "_albedo");
                rockMats[i].SetFloat("_UseNorm", 0); rockMats[i].SetFloat("_Spec", 0.12f); rockMats[i].SetFloat("_Gloss", 18);
                rockMats[i].SetColor("_Tint", new Color(1.25f, 1.15f, 1.1f));
            }
            if (_l.rocks != null)
                foreach (var r in _l.rocks)
                {
                    var t = Obj("Asteroid", rockMeshes[Mathf.Clamp(r.mesh, 0, 2)], rockMats[Mathf.Clamp(r.mesh, 0, 2)], _root);
                    t.localPosition = V(r.pos); t.localScale = Vector3.one * r.scale; t.localRotation = UnityEngine.Random.rotationUniform;
                    _rocks.Add((t, V(r.pos), V(r.axis).normalized, r.spin, V(r.drift)));
                }

            // camera
            var cgo = new GameObject("MenuLiveCamera");
            cgo.transform.SetParent(_root, false);
            _cam = cgo.AddComponent<Camera>();
            _cam.clearFlags = CameraClearFlags.SolidColor; _cam.backgroundColor = Color.black;
            _cam.cullingMask = 1 << Layer; _cam.nearClipPlane = 0.5f; _cam.farClipPlane = 4500f;
            _cam.allowHDR = false; _cam.allowMSAA = true; _cam.depth = -50; _cam.useOcclusionCulling = false;
            _target = V(_l.target);
            _yaw0 = _yaw = _l.yaw; _pitch0 = _pitch = _l.pitch; _dist0 = _dist = _zoomTo = _l.dist;
            Resize(1920, 1080); ApplyCamera();

            // Mars large on the right of the opening view (as in the video), Phobos keeping its place beside it
            var oldMars = mars.localPosition;
            mars.position = _cam.ViewportToWorldPoint(new Vector3(1.04f, 0.44f, 1550f));
            var phobosT = _root.Find("Phobos");
            if (phobosT != null) phobosT.localPosition += mars.localPosition - oldMars;
            // the sun from the upper left and a little in front, so the hull facing the opening camera is not in shadow
            var toCam = (_cam.transform.position - _ship.position).normalized;
            Shader.SetGlobalVector("_MLSunDir", (V(_l.sunDir).normalized + toCam * 0.75f).normalized);
            return true;
        }

        void AddEngineGlow(Vector3 local, Color c, float size, float falloff, float core)
        {
            var t = Glow("Engine", local, c, size, _ship, falloff, core);
            _engineGlows.Add((t, t.GetComponent<MeshRenderer>().sharedMaterial, c, size));
        }

        // ------------------------------------------------------------------ input

        /// <summary>Drag by a screen delta in pixels (x turns round the ship, y tilts).</summary>
        public void Drag(Vector2 delta)
        {
            _idle = 0; _dragging = true; _dragFrame = Time.frameCount;
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.008f);
            _yaw -= delta.x * 0.22f;
            _pitch = Mathf.Clamp(_pitch + delta.y * 0.18f, -55f, 70f);
            _yawVel = Mathf.Clamp(Mathf.Lerp(_yawVel, delta.x * 0.22f / dt, 0.5f), -240f, 240f);
            _pitchVel = Mathf.Clamp(Mathf.Lerp(_pitchVel, delta.y * 0.18f / dt, 0.5f), -160f, 160f);
        }

        public void EndDrag() { _idle = 0; _dragging = false; }
        bool _dragging; int _dragFrame; float _idlePhase;

        /// <summary>Zoom by wheel notches (positive = out).</summary>
        public void Zoom(float notches)
        {
            _idle = 0;
            _zoomTo = Mathf.Clamp(_zoomTo * Mathf.Pow(1.12f, notches), MinDist, MaxDist);
        }

        public void Resize(int w, int h)
        {
            w = Mathf.Clamp(w, 64, 2560); h = Mathf.Clamp(h, 64, 1600);
            if (Target != null && Target.width == w && Target.height == h) return;
            if (Target != null) { _cam.targetTexture = null; Target.Release(); Destroy(Target); }
            int aa = MenuScene.Quality == MenuQuality.Low ? 1 : MenuScene.Quality == MenuQuality.Medium ? 2 : 4;
            Target = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "MenuLiveRT", antiAliasing = aa };
            Target.Create();
            _cam.targetTexture = Target;
        }

        // ------------------------------------------------------------------ per frame

        void LateUpdate()
        {
            if (_cam == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            _t += dt; _idle += dt;

            // inertia after a drag, then a slow automatic turn round the ship; tilt and zoom drift home when idle
            if (_dragging)
            {
                // holding still keeps the ship still: the fling speed fades while no movement arrives
                if (Time.frameCount != _dragFrame) { float hold = Mathf.Exp(-dt * 14f); _yawVel *= hold; _pitchVel *= hold; }
                _idle = 0;
            }
            else
            {
                _yaw -= _yawVel * dt; _pitch = Mathf.Clamp(_pitch + _pitchVel * dt, -55f, 70f);
                float damp = Mathf.Exp(-dt * 3.2f); _yawVel *= damp; _pitchVel *= damp;
            }
            if (!Reduced && _idle > 4f)
            {
                // left alone, the camera eases back onto the video's path: a slow sway round the ship with a gentle push in
                _idlePhase += dt;
                float k = Mathf.Clamp01((_idle - 4f) / 6f) * dt * 0.6f;
                float swayYaw = _yaw0 + 9f * Mathf.Sin(_idlePhase * Mathf.PI * 2f / 32f);
                float swayPitch = _pitch0 + 3f * Mathf.Sin(_idlePhase * Mathf.PI * 2f / 32f + 1.1f);
                float swayDist = _dist0 * (1f - 0.22f * (0.5f - 0.5f * Mathf.Cos(_idlePhase * Mathf.PI * 2f / 32f)));
                _yaw = Mathf.LerpAngle(_yaw, swayYaw, k); _pitch = Mathf.Lerp(_pitch, swayPitch, k); _zoomTo = Mathf.Lerp(_zoomTo, swayDist, k);
            }
            _dist = Mathf.Lerp(_dist, _zoomTo, 1f - Mathf.Exp(-dt * 6f));
            ApplyCamera();

            // the ship rocks very gently; engines flicker; navigation lights blink like the video
            ShipFx(dt);
        }

        void ApplyCamera()
        {
            float yr = _yaw * Mathf.Deg2Rad, pr = _pitch * Mathf.Deg2Rad;
            var pos = _target + _dist * new Vector3(Mathf.Cos(pr) * Mathf.Cos(yr), Mathf.Sin(pr), Mathf.Cos(pr) * Mathf.Sin(yr));
            _cam.transform.localPosition = pos;
            _cam.transform.localRotation = Quaternion.LookRotation(_target - pos, Vector3.up);
            _sky.position = _cam.transform.position;

            // the lens: the same horizontal field and off-centre shift as the Blender camera (the ship sits right of centre)
            float aspect = Target != null ? (float)Target.width / Target.height : 16f / 9f;
            // 16:9 and wider keep the 16:9 height (wider screens see more to the sides); narrower ones keep the width
            float tanH = Mathf.Tan(_l.hfov * 0.5f * Mathf.Deg2Rad);
            float vfov = 2f * Mathf.Atan(tanH / Mathf.Min(aspect, 16f / 9f)) * Mathf.Rad2Deg;
            var proj = Matrix4x4.Perspective(vfov, aspect, _cam.nearClipPlane, _cam.farClipPlane);
            float shiftK = Mathf.Lerp(1f, 0.55f, Mathf.InverseLerp(_dist0, MinDist, _dist));   // close up, centre the ship a little more
            proj.m02 = 2f * _l.shift[0] * shiftK;
            proj.m12 = 2f * _l.shift[1] * aspect;
            _cam.projectionMatrix = proj;
        }

        void ShipFx(float dt)
        {
            if (!Reduced)
                _ship.localRotation = Quaternion.Euler(1.5f * Mathf.Sin(_t * 0.39f), 0, 3f * Mathf.Sin(_t * 0.31f + 0.4f));
            _ship.localPosition = new Vector3(0, Reduced ? 0 : 0.35f * Mathf.Sin(_t * 0.78f + 0.7f), 0);
            for (int i = 0; i < _engineGlows.Count; i++)
            {
                var (t, m, c, size) = _engineGlows[i];
                float f = 1f + 0.12f * Mathf.Sin(_t * 41f + i * 2.1f) + 0.06f * Mathf.Sin(_t * 103f + i);
                t.localScale = Vector3.one * size * f;
            }
            float blink = Mathf.Repeat(_t, 2f) < 0.83f ? 1f : 0.03f;
            float sp = Mathf.Repeat(_t, 4f);
            float strobe = (sp < 0.12f || (sp > 0.3f && sp < 0.42f)) ? 1f : 0f;
            _navR.SetColor("_Color", new Color(1f, 0.12f, 0.06f, blink));
            _navG.SetColor("_Color", new Color(0.15f, 1f, 0.3f, blink));
            _strobe.SetColor("_Color", new Color(1f, 0.97f, 0.92f, strobe));
            _engineHalo.SetColor("_Color", new Color(0.25f, 0.5f, 1f, 0.33f + 0.04f * Mathf.Sin(_t * 23f)));

            // glows face the camera
            var camRot = _cam.transform.rotation;
            foreach (var b in _billboards) b.rotation = camRot;

            // asteroids tumble and drift slowly back and forth
            for (int i = 0; i < _rocks.Count; i++)
            {
                var (t, bp, ax, spin, drift) = _rocks[i];
                if (!Reduced) t.Rotate(ax, spin * dt, Space.World);
                t.localPosition = bp + drift * 14f * Mathf.Sin(_t * 0.045f + i * 1.7f);
            }

            // other cameras must never draw this layer
            foreach (var c in Camera.allCameras) if (c != _cam) c.cullingMask &= ~(1 << Layer);
        }

        void OnDestroy()
        {
            if (Target != null) { if (_cam != null) _cam.targetTexture = null; Target.Release(); Destroy(Target); }
            foreach (var o in _owned) if (o != null) Destroy(o);
        }
    }
}
