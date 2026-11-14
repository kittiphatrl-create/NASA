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
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
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
            return Cached("blender-station-" + name, () =>
            {
                var ta = Resources.Load<TextAsset>("Sprites/Station/" + name);
                if (ta == null) return null;
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                t.LoadImage(ta.bytes);
                return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, 0), 44, 0, SpriteMeshType.FullRect);
            });
        }

        /// <summary>LAST DOCK walls and floors from Art/Blender/station_tiles.blend (64 px per tile). Set while the station map is built.</summary>
        public static bool UseStationTiles;

        static Sprite LoadTile(string name)
            => Cached("blender-tile-" + name, () =>
            {
                var ta = Resources.Load<TextAsset>("Sprites/Tiles/" + name);
                if (ta == null) return null;
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                t.LoadImage(ta.bytes);
                return Sprite.Create(t, new Rect(0, 0, t.width, t.height), Vector2.zero, 64, 0, SpriteMeshType.FullRect);
            });

        /// <summary>
        /// The cadet in 5 rendered directions (0 S, 1 SE, 2 E, 3 NE, 4 N; the game mirrors the west side).
        /// key: idle, blink, w0-7 walk, r0-7 run, c0-7 carry walking, ci carry standing, u0-3 use.
        /// </summary>
        public static Sprite Cadet8(int dir5, string key)
        {
            if (!UseBlenderCadet) return null;
            string name = $"d{Mathf.Clamp(dir5, 0, 4)}_{key}.png";
            return Cached("blender-c8-" + name, () =>
            {
                var ta = Resources.Load<TextAsset>("Sprites/Cadet8/" + name);
                if (ta == null) return null;
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                t.LoadImage(ta.bytes);
                return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, 0), 44, 0, SpriteMeshType.FullRect);
            });
        }

        /// <summary>Sliding door: horizontal (in a horizontal wall, 2 tiles wide, 2 tall) or vertical (top view, 1 x 2); frame 0 shut to 3 open.</summary>
        public static Sprite Door(bool horizontal, bool locked, int frame)
            => LoadTile($"door_{(horizontal ? "h" : "v")}_{(locked ? "lock" : "ok")}_{Mathf.Clamp(frame, 0, 3)}.png");

        /// <summary>Floor kinds 0 core/storage, 1 life support, 3 tube and doors, 4 comms, 5 thermal, 6 dock; variant 2 scuff, 4 hatch.</summary>
        static Sprite StationFloor(int kind, int variant)
        {
            if (kind == 2) return null;   // Mars ground keeps its own art
            int k = kind == 1 || kind == 3 || kind == 4 || kind == 5 || kind == 6 ? kind : 0;
            int v = variant == 4 ? 4 : variant == 2 || variant == 6 ? 2 : k == 6 && variant == 8 ? 8 : 0;   // dock: hazard stripes on 1 tile in 9
            return LoadTile($"floor_k{k}_v{v}.png");
        }

        static Sprite StationWall(bool face, bool hull, int variant)
            => LoadTile(face ? $"wall_face_h{(hull ? 1 : 0)}_v{((variant % 4) + 4) % 4}.png" : $"wall_top_h{(hull ? 1 : 0)}.png");

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
