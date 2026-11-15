// Generated overlay textures for the menu: soft radial glow, the left readability scrim,
// vignette and film grain. Gradients are dithered so 8-bit alpha never shows banding.
using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Game.Menu
{
    public static class MenuFx
    {
        static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        static Texture2D Make(string key, int w, int h, System.Func<float, float, System.Random, Color> px, FilterMode filter = FilterMode.Bilinear, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            if (Cache.TryGetValue(key, out var t) && t != null) return t;
            var rng = new System.Random(key.GetHashCode());
            t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = filter, wrapMode = wrap, name = "menu-fx-" + key };
            var c = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    c[y * w + x] = px((x + 0.5f) / w, 1f - (y + 0.5f) / h, rng);   // v = 0 at the top
            t.SetPixels32(c); t.Apply(false, true);
            Cache[key] = t;
            return t;
        }

        static float Dither(System.Random r) => ((float)r.NextDouble() - 0.5f) * (1.5f / 255f);
        static float Smooth(float a, float b, float x) { x = Mathf.Clamp01((x - a) / (b - a)); return x * x * (3 - 2 * x); }

        /// <summary>White radial falloff, for glows (tint it).</summary>
        public static Texture2D Radial() => Make("radial", 128, 128, (u, v, r) =>
        {
            float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f;
            float a = Mathf.Pow(Mathf.Clamp01(1 - d), 2.2f) + Dither(r);
            return new Color(1, 1, 1, Mathf.Clamp01(a));
        });

        /// <summary>Small soft dot for dust motes.</summary>
        public static Texture2D Mote() => Make("mote", 32, 32, (u, v, r) =>
        {
            float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f;
            return new Color(1, 1, 1, Mathf.Clamp01(Mathf.Pow(Mathf.Clamp01(1 - d), 1.6f)));
        });

        /// <summary>
        /// Left readability scrim: a long eased linear falloff plus a large radial pool behind the menu column,
        /// combined as screen-space "over" so there is never a visible edge. Drawn at 640x360 and stretched.
        /// </summary>
        public static Texture2D Scrim(Color deep) => Make("scrim", 640, 360, (u, v, r) =>
        {
            float lin = 0.92f * (1f - Smooth(0.16f, 0.60f, u));
            float d = new Vector2((u - 0.2f) / 0.62f, (v - 0.56f) / 0.85f).magnitude;
            float rad = 0.38f * (1f - Smooth(0f, 1f, d));
            float a = 1f - (1f - lin) * (1f - rad) + Dither(r);
            return new Color(deep.r, deep.g, deep.b, Mathf.Clamp01(a));
        });

        /// <summary>Plain gold ring with ticks, used only if the Blender ring render is missing.</summary>
        public static Texture2D DockRingFallback() => Make("ring", 512, 512, (u, v, r) =>
        {
            var p = new Vector2(u - 0.5f, v - 0.5f) * 2f; float d = p.magnitude;
            float band = Smooth(0.895f, 0.905f, d) * (1f - Smooth(0.965f, 0.975f, d));
            float inner = Smooth(0.86f, 0.865f, d) * (1f - Smooth(0.872f, 0.877f, d)) * 0.7f;
            float ang = Mathf.Atan2(p.y, p.x) / (Mathf.PI * 2f) * 72f;
            float tick = (1f - Smooth(0.04f, 0.09f, Mathf.Abs(ang - Mathf.Round(ang)))) * Smooth(0.82f, 0.83f, d) * (1f - Smooth(0.855f, 0.86f, d));
            float a = Mathf.Clamp01(band + inner + tick * 0.8f);
            var gold = Color.Lerp(new Color(0.95f, 0.66f, 0.3f), new Color(1f, 0.9f, 0.7f), Smooth(0.9f, 0.97f, d));
            return new Color(gold.r, gold.g, gold.b, a);
        });

        /// <summary>Soft fade along the bottom edge so footer text keeps 4.5:1 over bright ground.</summary>
        public static Texture2D BottomFade(Color deep) => Make("bottom", 8, 256, (u, v, r) =>
        {
            float a = 0.9f * Smooth(0f, 1f, v) + Dither(r);
            return new Color(deep.r, deep.g, deep.b, Mathf.Clamp01(a));
        });

        public static Texture2D Vignette() => Make("vignette", 320, 180, (u, v, r) =>
        {
            float d = new Vector2((u - 0.5f) * 1.25f, (v - 0.5f)).magnitude * 1.6f;
            float a = 0.5f * Smooth(0.55f, 1.25f, d) + Dither(r);
            return new Color(0.03f, 0.02f, 0.03f, Mathf.Clamp01(a));
        });

        /// <summary>Tileable monochrome grain; the menu shifts it every frame at about 24 fps.</summary>
        public static Texture2D Grain() => Make("grain", 256, 256, (u, v, r) =>
        {
            float g = (float)r.NextDouble();
            return new Color(g, g, g, 1);
        }, FilterMode.Point, TextureWrapMode.Repeat);

        /// <summary>Soft white bar for the logo shimmer sweep (transparent at both ends).</summary>
        public static Texture2D Sheen() => Make("sheen", 128, 8, (u, v, r) =>
        {
            float a = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 3f) * 0.55f;
            return new Color(1f, 0.96f, 0.86f, a);
        });
    }
}
