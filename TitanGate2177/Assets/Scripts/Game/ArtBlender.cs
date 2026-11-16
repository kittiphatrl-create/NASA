// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do sprites rendered from a Blender model (orthographic, cel-shaded, 64x80) read better than hand-coded pixels?
// Date: 2026-11-15
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>
    /// Sprites rendered in Blender (source: Art/Blender/cadet.blend). Each PNG is stored as .bytes so it loads with
    /// point filtering and no compression. Missing files fall back to the code-drawn sprites.
    /// </summary>
    public static partial class Art
    {
        public static bool UseBlenderCadet = true;

        /// <summary>The cadet in the flight suit: dir 0 front, 1 back, 2 side (faces right); frame -1 idle, -2 blink, 0-7 walk.</summary>
        static Sprite BlenderCadet(int dir, int frame)
        {
            string k = frame == -1 ? "idle" : frame == -2 ? "blink" : "f" + ((frame % 8) + 8) % 8;
            string name = $"cadet_d{Mathf.Clamp(dir, 0, 2)}_{k}.png";
            return Cached("blender-" + name, () =>
            {
                var ta = Resources.Load<TextAsset>("Sprites/Cadet/" + name);
                if (ta == null) return null;
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, true) { filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp, mipMapBias = -0.35f };
                t.LoadImage(ta.bytes);
                return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, 0), 44, 0, SpriteMeshType.FullRect);
            });
        }

        /// <summary>
        /// LAST DOCK station console rendered in Blender (Art/Blender/station_consoles.blend), ok or fault variant.
        /// id: scrubber, o2gen, fan, thermal, radio, board, dock, lockers. Null when the file is missing.
        /// </summary>
        public static Sprite Station(string id, bool bad)
        {
            if (!UseBlenderCadet) return null;
            string name = $"{id}_{(bad ? "bad" : "ok")}.png";
            return Cached($"blender-station-{UseLastDockLook}-{name}", () =>
            {
                var ta = StationArtAsset("Station", name, out bool isLastDock);
                if (ta == null) return null;
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, true)
                {
                    filterMode = FilterMode.Trilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 4,
                    mipMapBias = -0.35f
                };
                t.LoadImage(ta.bytes);
                float ppu = isLastDock ? 132f : 44f;
                Vector2 pivot = id.EndsWith("rug") ? new Vector2(.5f, .5f) : new Vector2(.5f, 0);
                return Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, ppu, 0, SpriteMeshType.FullRect);
            });
        }

        /// <summary>LAST DOCK walls and floors rendered in Blender. Set while the station map is built.</summary>
        public static bool UseStationTiles;
        public static bool UseLastDockLook;

        static TextAsset StationArtAsset(string folder, string name, out bool isLastDock)
        {
            var asset = UseLastDockLook ? Resources.Load<TextAsset>($"Sprites/LastDock/{folder}/{name}") : null;
            isLastDock = asset != null;
            return asset ?? Resources.Load<TextAsset>($"Sprites/{folder}/{name}");
        }

        static Sprite LoadTile(string name)
            => Cached($"blender-tile-{UseLastDockLook}-{name}", () =>
            {
                var ta = StationArtAsset("Tiles", name, out bool isLastDock);
                if (ta == null) return null;
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, true)
                {
                    filterMode = FilterMode.Trilinear,
                    wrapMode = name.StartsWith("floor_k2_") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                    anisoLevel = 4,
                    mipMapBias = -0.35f
                };
                t.LoadImage(ta.bytes);
                if (isLastDock && (name.StartsWith("floor_k5_") || name.StartsWith("floor_k3_") || name.StartsWith("floor_k6_") || name.Contains("_v4")))
                    SoftenHighFrequencyTile(t);
                float ppu = t.width / (name.StartsWith("door_h_") ? 2f : 1f);
                return Sprite.Create(t, new Rect(0, 0, t.width, t.height), Vector2.zero, ppu, 0, SpriteMeshType.FullRect);
            });

        internal static void SoftenHighFrequencyTile(Texture2D t, float blend = .42f)
        {
            int w = t.width, h = t.height;
            var src = t.GetPixels32();
            var dst = (Color32[])src.Clone();
            for (int y = 1; y < h - 1; y++)
            {
                int row = y * w;
                for (int x = 1; x < w - 1; x++)
                {
                    Color c = src[row + x];
                    if (c.a == 0) continue;
                    Color l = src[row + x - 1], r = src[row + x + 1], d = src[row - w + x], u = src[row + w + x];
                    Color avg = (l + r + d + u) * .25f;
                    float diff = Mathf.Abs(c.r - avg.r) + Mathf.Abs(c.g - avg.g) + Mathf.Abs(c.b - avg.b);
                    if (diff > .11f)
                    {
                        Color m = Color.Lerp(c, avg, blend);
                        m.a = c.a;
                        dst[row + x] = m;
                    }
                }
            }
            t.SetPixels32(dst);
            t.Apply(true, false);
        }

        public static void PrewarmCadetSprites(bool includeEva)
        {
            if (!UseBlenderCadet) return;
            string[] keys = { "idle", "blink", "w0", "w1", "w2", "w3", "w4", "w5", "w6", "w7", "r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7" };
            for (int d = 0; d <= 4; d++)
                foreach (var k in keys)
                {
                    Cadet8(d, k, false);
                    if (includeEva) Cadet8(d, k, true);
                }
        }

        /// <summary>
        /// The cadet in 5 rendered directions (0 S, 1 SE, 2 E, 3 NE, 4 N; the game mirrors the west side).
        /// key: idle, blink, w0-7 walk, r0-7 run, c0-7 carry walking, ci carry standing, u0-3 use.
        /// </summary>
        public static Sprite Cadet8(int dir5, string key, bool eva = false)
        {
            string bank = eva ? "CadetEva" : "Cadet8";
            return RenderedCharacter($"{bank}/d{Mathf.Clamp(dir5, 0, 4)}_{key}", new Vector2(.5f, 0), 44);
        }

        static Sprite RenderedCharacter(string name, Vector2 pivot, int pixelsPerUnit)
        {
            if (!UseBlenderCadet) return null;
            return Cached("model/" + name, () =>
            {
                var ta = Resources.Load<TextAsset>($"Sprites/{name}.png");
                if (ta == null) return null;
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, true) { filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp, mipMapBias = -0.35f };
                if (!t.LoadImage(ta.bytes))
                {
                    Object.Destroy(t);
                    return null;
                }
                GradeCharacterTexture(t);
                return Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
            });
        }

        static void GradeCharacterTexture(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a == 0) continue;
                Color c = pixels[i];
                float luma = c.grayscale;
                float light = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.18f, .83f, luma));
                float contrast = Mathf.Lerp(.86f, 1.10f, light);
                pixels[i] = new Color(Mathf.Clamp01(c.r * contrast), Mathf.Clamp01(c.g * contrast),
                    Mathf.Clamp01(c.b * contrast + .023f * (1f - light)), c.a);
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, false);
        }

        static Sprite ModelCrew(string id, int dir, int frame, bool eva, bool run, bool wave)
        {
            string key = frame == -1 ? "idle" : frame == -2 ? "blink" : (run ? "r" : "w") + (Mathf.Max(0, frame) % 8);
            if (id == "cadet" && (!wave || eva))
            {
                int direction = dir == 1 ? 4 : dir == 2 ? 2 : 0;
                return Cadet8(direction, key, eva);
            }
            if (eva) return null;
            return RenderedCharacter($"Models/{id}/d{Mathf.Clamp(dir, 0, 2)}_{(wave ? "wave" : key)}", new Vector2(.5f, 0), 44);
        }

        static Sprite ModelCrewAction(string id, string action, int phase, bool eva)
        {
            if (eva) return null;
            if (id == "cadet" && (action == "carry" || action == "lift"))
                return BlenderAction(action, phase);
            string key = action == "sit" ? action : action + ((phase % 4 + 4) % 4);
            return RenderedCharacter($"Models/{id}/{key}", new Vector2(.5f, 0), 44);
        }

        static Sprite ModelAstronaut(bool eva, Pose pose)
        {
            string key = pose switch
            {
                Pose.Idle0 or Pose.Idle1 => "idle",
                Pose.Walk0 or Pose.Walk1 or Pose.Walk2 or Pose.Walk3
                    => "w" + (2 * ((int)pose - (int)Pose.Walk0)),
                Pose.Work1 or Pose.Eat1 or Pose.Kneel1 => "u1",
                _ => "u0",
            };
            return Cadet8(2, key, eva);
        }

        static Sprite ModelPet(string id, string key)
            => RenderedCharacter($"Models/{id}/{key}", new Vector2(.5f, 0), 20);

        static Sprite ModelOrbi(int frame)
            => RenderedCharacter($"Models/orbi/{Mathf.Clamp(frame, 0, 2)}", new Vector2(.5f, .5f), 16);

        /// <summary>Sliding door: horizontal (in a horizontal wall, 2 tiles wide, 2 tall) or vertical (top view, 1 x 2); frame 0 shut to 3 open.</summary>
        public static Sprite Door(bool horizontal, bool locked, int frame)
            => LoadTile($"door_{(horizontal ? "h" : "v")}_{(locked ? "lock" : "ok")}_{Mathf.Clamp(frame, 0, 3)}.png");

        /// <summary>Floor kinds 0 core/storage, 1 life support, 2 Mars regolith, 3 tube and doors, 4 comms, 5 thermal, 6 dock; variant 2 scuff, 4 hatch.</summary>
        static Sprite StationFloor(int kind, int variant)
        {
            if (kind == 2) return UseLastDockLook ? LoadTile($"floor_k2_v{((variant % 4) + 4) % 4}.png") ?? LoadTile("floor_k2_v0.png") : null;
            int k = kind == 1 || kind == 3 || kind == 4 || kind == 5 || kind == 6 ? kind : 0;
            int v = variant == 4 ? 4 : variant == 2 || variant == 6 ? 2 : k == 6 && variant == 8 ? 8 : 0;   // dock: hazard stripes on 1 tile in 9
            return LoadTile($"floor_k{k}_v{v}.png");
        }

        static Sprite StationWall(bool face, bool hull, int variant, int deckSides = 12)
        {
            int h = hull ? 1 : 0;
            int v = ((variant % 4) + 4) % 4;
            int ds = deckSides & 15;
            if (face) return LoadTile($"wall_face_h{h}_v{v}_ds{ds | 8}.png") ?? LoadTile($"wall_face_h{h}_v{v}.png");
            return LoadTile($"wall_top_h{h}_ds{ds}.png") ?? LoadTile($"wall_top_h{h}.png");
        }

        /// <summary>Blender action poses for the cadet (front view, 4 phases); other actions keep the code-drawn sprite.</summary>
        static Sprite BlenderAction(string action, int phase)
        {
            if (action != "carry" && action != "lift") return null;
            string name = $"cadet_{action}_p{((phase % 4) + 4) % 4}.png";
            return Cached("blender-" + name, () =>
            {
                var ta = Resources.Load<TextAsset>("Sprites/Cadet/" + name);
                if (ta == null) return null;
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                t.LoadImage(ta.bytes);
                return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, 0), 44, 0, SpriteMeshType.FullRect);
            });
        }
    }
}
