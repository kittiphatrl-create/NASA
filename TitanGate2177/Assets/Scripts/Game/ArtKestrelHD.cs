using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Game
{
    public static partial class Art
    {
        public static bool UseKestrelHD;
        static Dictionary<string, string> _shipHDMapping;
        static readonly Dictionary<string, Sprite> ShipHDSprites = new Dictionary<string, Sprite>();

        static Sprite KestrelHDReplacement(string key, Sprite original)
        {
            if (!UseKestrelHD || original == null) return original;
            if (_shipHDMapping == null)
            {
                _shipHDMapping = new Dictionary<string, string>();
                var manifest = Resources.Load<TextAsset>("Sprites/KestrelHD/mapping");
                if (manifest != null)
                    foreach (var line in manifest.text.Split('\n'))
                    {
                        var parts = line.Trim().Split('\t');
                        if (parts.Length == 2) _shipHDMapping[parts[0]] = parts[1];
                    }
            }
            if (!_shipHDMapping.TryGetValue(key, out var asset)) return original;
            return LoadKestrelHD(key, asset, original.pivot / original.rect.size,
                original.rect.width / original.pixelsPerUnit) ?? original;
        }

        public static Sprite KestrelHydroponicsRack(int variant) =>
            LoadKestrelHD("hydro/rack/" + variant, "hydro_rack_" + variant, new Vector2(.5f, .045f), 2.1f)
            ?? KestrelLabPlanter(variant);

        public static Sprite KestrelHydroponicsTanks() =>
            LoadKestrelHD("hydro/tanks", "hydro_tanks", new Vector2(.5f, .17f), 2.05f)
            ?? Shelf(false);

        static Sprite LoadKestrelHD(string key, string asset, Vector2 pivot, float worldWidth)
        {
            if (ShipHDSprites.TryGetValue(key, out var replacement)) return replacement;
            var data = Resources.Load<TextAsset>("Sprites/KestrelHD/" + asset + ".png");
            if (data == null) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (!texture.LoadImage(data.bytes))
            {
                Object.Destroy(texture);
                return null;
            }
            replacement = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot,
                texture.width / worldWidth, 0, SpriteMeshType.FullRect);
            replacement.name = "HD/" + key;
            ShipHDSprites[key] = replacement;
            return replacement;
        }

        public static void ClearKestrelHDCache()
        {
            foreach (var s in ShipHDSprites.Values)
            {
                if (s == null) continue;
                var tex = s.texture;
                if (Application.isPlaying)
                {
                    Object.Destroy(s);
                    if (tex != null) Object.Destroy(tex);
                }
                else
                {
                    Object.DestroyImmediate(s);
                    if (tex != null) Object.DestroyImmediate(tex);
                }
            }
            ShipHDSprites.Clear();
            _shipHDMapping = null;
        }
    }
}
