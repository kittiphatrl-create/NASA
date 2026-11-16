using UnityEngine;

namespace TitanGate.Game
{
    public static partial class Art
    {
        public static int HabitatTheme { get; set; }
        public static Color32 HabitatInk => PixelCanvas.Hex("#101c2b");
        public static Color32 HabitatMetal => PixelCanvas.Hex(HabitatTheme == 1 ? "#b8bbb3" : HabitatTheme == 2 ? "#82776c" : "#66768f");
        public static Color32 HabitatShade => PixelCanvas.Hex(HabitatTheme == 1 ? "#697c86" : HabitatTheme == 2 ? "#45454a" : "#303e56");
        public static Color32 HabitatFloorColor => PixelCanvas.Hex(HabitatTheme == 1 ? "#637e8d" : HabitatTheme == 2 ? "#4b5056" : "#294669");
        public static Color32 HabitatAmber => PixelCanvas.Hex("#f7a72e");
        public static Color32 HabitatCyan => PixelCanvas.Hex("#63dce8");

        public static Sprite DockTubeLight() => Cached("dock-tube-light", () =>
        {
            var c = new PixelCanvas(96, 44);
            var ink = PixelCanvas.Hex("#1a1d24");
            var bracket = PixelCanvas.Hex("#3a3c42");
            var housing = PixelCanvas.Hex("#6d675f");
            var highlight = PixelCanvas.Hex("#9c9387");
            var recess = PixelCanvas.Hex("#2b2928");
            var warmRim = PixelCanvas.Hex("#f3b352");
            var warmCore = PixelCanvas.Hex("#ffebba");
            c.RoundRect(8, 10, 80, 26, ink);
            c.RoundRect(11, 13, 74, 20, bracket);
            c.Rect(14, 17, 68, 14, housing);
            c.Rect(16, 28, 64, 3, highlight);
            c.Rect(18, 12, 60, 13, recess);
            c.Rect(21, 14, 54, 9, warmRim);
            c.Rect(24, 16, 48, 5, warmCore);
            c.Rect(15, 21, 3, 4, ink);
            c.Rect(78, 21, 3, 4, ink);
            var s = c.ToSprite(new Vector2(.5f, .5f), 96);
            s.texture.filterMode = FilterMode.Bilinear;
            return s;
        });

        public static Sprite KestrelWallLight() => Cached("kestrel-wall-light", () =>
        {
            var c = new PixelCanvas(96, 40);
            var ink = PixelCanvas.Hex("#121c2c");
            var bracket = PixelCanvas.Hex("#364960");
            var housing = PixelCanvas.Hex("#7890a8");
            var highlight = PixelCanvas.Hex("#b8cde0");
            var recess = PixelCanvas.Hex("#1e2b3c");
            var warmRim = PixelCanvas.Hex("#f5be6b");
            var warmCore = PixelCanvas.Hex("#fff0c9");
            c.RoundRect(8, 9, 80, 24, ink);
            c.RoundRect(11, 12, 74, 18, bracket);
            c.Rect(14, 16, 68, 12, housing);
            c.Rect(16, 25, 64, 3, highlight);
            c.Rect(18, 11, 60, 12, recess);
            c.Rect(21, 13, 54, 8, warmRim);
            c.Rect(24, 15, 48, 4, warmCore);
            c.Rect(15, 19, 3, 4, ink);
            c.Rect(78, 19, 3, 4, ink);
            var s = c.ToSprite(new Vector2(.5f, .5f), 96);
            s.texture.filterMode = FilterMode.Bilinear;
            return s;
        });

        /// <summary>Soft directional light cone (pivot at top centre) for wall wash and floor falloff.</summary>
        public static Sprite WallLightCone() => Cached("wall-light-cone", () =>
        {
            const int w = 96, h = 112;
            var c = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
            {
                float t = 1f - (y + .5f) / h; // 0 at top source, 1 at bottom edge
                float halfSpan = Mathf.Lerp(.18f, .96f, Mathf.Pow(t, .82f));
                float vFade = Mathf.SmoothStep(0f, .12f, t) * Mathf.Pow(1f - t, 1.45f);
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs((x + .5f) / w * 2f - 1f) / halfSpan;
                    if (dx >= 1f) continue;
                    float hFade = Mathf.Pow(1f - dx * dx, 1.6f);
                    float a = Mathf.Clamp01(vFade * hFade);
                    c.Set(x, y, new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f)));
                }
            }
            var tex = c.ToTexture();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, 1f), 64f, 0, SpriteMeshType.FullRect);
        });

        public static Sprite DeckRouteMarker() => Cached("deck-route-marker", () =>
        {
            var c = new PixelCanvas(64, 64);
            c.RoundRect(7, 10, 50, 8, Color.Lerp(HabitatInk, HabitatShade, .25f));
            c.Rect(10, 12, 44, 4, Color.Lerp(HabitatAmber, HabitatShade, .18f));
            c.Rect(12, 14, 40, 2, Color.Lerp(Pal.Gold, HabitatAmber, .35f));
            var s = c.ToSprite(Vector2.zero, 64);
            s.texture.filterMode = FilterMode.Bilinear;
            return s;
        });

        static void HBPanel(PixelCanvas c, int x, int y, int w, int h, Color32 face)
        {
            c.Rect(x, y, w, h, HabitatInk);
            c.Rect(x + 2, y + 3, w - 4, h - 5, face);
            c.HLine(x + 3, x + w - 4, y + h - 3, Color.Lerp(face, Pal.Ice, .35f));
            c.VLine(x + 2, y + 4, y + h - 4, Color.Lerp(face, Pal.Ice, .15f));
            c.Rect(x + 3, y + 2, w - 6, 3, Color.Lerp(face, HabitatInk, .5f));
        }

        static void HBBolt(PixelCanvas c, int x, int y)
        {
            c.Rect(x - 2, y - 2, 5, 5, HabitatInk); c.Rect(x - 1, y, 3, 2, HabitatMetal); c.HLine(x - 1, x + 1, y, HabitatShade);
        }

        public static Sprite HabitatFloor(int kind, int variant) => Cached($"hab-floor-{HabitatTheme}-{kind}-{variant}", () =>
        {
            var c = new PixelCanvas(64, 64);
            if (kind == 2)
            {
                c.Fill(PixelCanvas.Hex("#a65232"));
                var rng = new System.Random(2177 + variant);
                for (int i = 0; i < 9; i++)
                { int x = rng.Next(5, 57), y = rng.Next(5, 57); c.HLine(x, x + rng.Next(3, 8), y, i % 2 == 0 ? PixelCanvas.Hex("#b9623b") : PixelCanvas.Hex("#94492e")); }
                if (variant % 3 == 0) { c.RoundRect(21, 29, 12, 6, Pal.RustD); c.HLine(23, 30, 34, Pal.RustL); }
            }
            else
            {
                var face = HabitatFloorColor;
                if (kind == 1) face = Color.Lerp(face, PixelCanvas.Hex("#547965"), .45f);
                if (kind == 4) face = Color.Lerp(face, PixelCanvas.Hex("#625f77"), .3f);
                if (kind == 5) face = Color.Lerp(face, HabitatInk, .35f);
                if (kind == 6 || kind == 7) face = Color.Lerp(face, PixelCanvas.Hex("#847969"), .55f);
                c.Fill(Color.Lerp(face, HabitatInk, .48f)); c.Rect(1, 1, 62, 62, face);
                c.HLine(2, 61, 62, Color.Lerp(face, Pal.Ice, .12f)); c.VLine(1, 2, 61, Color.Lerp(face, Pal.Ice, .06f));
                c.HLine(2, 62, 1, Color.Lerp(face, HabitatInk, .25f));
                // Low contrast clusters, not per-pixel noise or a vent on every tile.
                if (variant == 2 || variant == 6)
                { c.HLine(12, 28, 45, Color.Lerp(face, Pal.Ice, .1f)); c.HLine(38, 48, 17, Color.Lerp(face, HabitatInk, .18f)); }
                if (variant == 4)
                { c.Rect(18, 18, 28, 22, Color.Lerp(face, HabitatInk, .2f)); c.HLine(20, 43, 39, Color.Lerp(face, Pal.Ice, .22f)); c.Rect(28, 22, 9, 3, HabitatInk); }
                if (kind == 5)
                    for (int y = 9; y < 58; y += 8) { c.Rect(7, y, 50, 3, HabitatInk); c.HLine(8, 55, y + 3, HabitatShade); }
                if (kind == 3)
                { c.Rect(0, 27, 64, 10, HabitatShade); c.Rect(0, 30, 64, 3, HabitatMetal); if (variant % 3 == 0) c.Rect(18, 30, 28, 3, HabitatAmber); }
                if (kind == 8 && variant % 3 == 0)
                    for (int x = 4; x < 60; x += 14) c.Line(x, 5, x + 9, 14, HabitatAmber);
                c.Rect(6, 6, 3, 3, HabitatShade); c.Rect(55, 55, 3, 3, HabitatShade);
            }
            return c.ToSprite(Vector2.zero, 64);
        });

        // Floor-only contact shading: never changes wall or navigation geometry.
        public static Sprite HabitatContactShadow(int mask) => Cached("hab-contact-" + mask, () =>
        {
            var c = new PixelCanvas(64, 64);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float a = 0f;
                if ((mask & 1) != 0) a += .48f * Mathf.Pow(Mathf.Clamp01(1f - (63 - y) / 20f), 1.65f);
                if ((mask & 2) != 0) a += .36f * Mathf.Pow(Mathf.Clamp01(1f - x / 15f), 1.65f);
                if ((mask & 4) != 0) a += .36f * Mathf.Pow(Mathf.Clamp01(1f - (63 - x) / 15f), 1.65f);
                if ((mask & 8) != 0) a += .28f * Mathf.Pow(Mathf.Clamp01(1f - y / 13f), 1.65f);
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 142f);
                if (alpha > 0) c.Set(x, y, new Color32(8, 16, 32, alpha));
            }
            var s = c.ToSprite(Vector2.zero, 64);
            s.texture.filterMode = FilterMode.Bilinear;
            return s;
        });

        public static Sprite HabitatWall(bool face, bool hull, int variant) => Cached($"hab-wall-{HabitatTheme}-{face}-{hull}-{variant}", () =>
        {
            var c = new PixelCanvas(64, face ? 128 : 64);
            if (!face) { HBPanel(c, 0, 0, 64, 64, HabitatMetal); c.Rect(5, 6, 54, 6, HabitatShade); return c.ToSprite(Vector2.zero, 64); }
            HBPanel(c, 0, 0, 64, 98, HabitatShade);
            c.Rect(4, 5, 56, 9, HabitatInk); c.HLine(5, 58, 15, HabitatMetal);
            HBPanel(c, 6, 22, 52, 52, HabitatMetal);
            c.Rect(10, 25, 44, 3, HabitatShade); c.Rect(12, 61, 25, 3, HabitatShade);
            HBBolt(c, 12, 68); HBBolt(c, 51, 29);
            if (variant % 4 == 0)
            { c.Rect(15, 36, 34, 14, HabitatInk); c.Rect(19, 40, 26, 6, HabitatAmber); c.HLine(20, 43, 46, Pal.Gold); }
            else if (variant % 4 == 1)
                for (int y = 36; y < 54; y += 5) { c.Rect(17, y, 30, 3, HabitatInk); c.HLine(18, 44, y + 3, HabitatShade); }
            else { c.Rect(39, 34, 8, 21, HabitatShade); c.Rect(41, 43, 3, 8, HabitatInk); }
            // A few deliberately placed chips, never blanket random speckling.
            if (hull || variant % 3 == 0) { c.HLine(9, 15, 57, HabitatShade); c.Rect(48, 60, 4, 2, HabitatShade); }
            HBPanel(c, 0, 96, 64, 32, HabitatMetal);
            c.Rect(5, 101, 54, 5, HabitatShade); c.HLine(5, 58, 121, Color.Lerp(HabitatMetal, Pal.Ice, .3f));
            return c.ToSprite(Vector2.zero, 64);
        });

        public static Sprite HabitatConsole(string id, int w, int h, Color32 screen, bool alarm) => Cached($"hab-console-{HabitatTheme}-{id}-{w}-{h}-{alarm}", () =>
        {
            int W = w * 4, H = h * 4; var c = new PixelCanvas(W, H);
            HBPanel(c, 8, 0, W - 16, H / 2 + 4, HabitatShade);
            c.Rect(14, 6, 9, H / 3, HabitatInk); c.Rect(W - 23, 6, 9, H / 3, HabitatInk);
            HBPanel(c, 0, H / 2 - 8, W, H / 2 + 8, HabitatMetal);
            HBPanel(c, 9, H / 2 + 9, W - 18, H / 2 - 20, HabitatInk);
            var lit = alarm ? Pal.Red : screen;
            c.Rect(15, H / 2 + 16, W - 30, H / 2 - 33, PixelCanvas.Hex("#102e43"));
            c.HLine(17, W - 18, H - 19, Color.Lerp(lit, Pal.White, .35f));
            for (int y = H / 2 + 21; y < H - 25; y += 8) c.Rect(20, y, Mathf.Max(8, (W - 43) * (y % 3 + 1) / 3), 2, lit);
            c.Rect(11, H / 2 - 2, W - 22, 5, HabitatInk);
            for (int x = 15; x < W - 15; x += 13) c.Rect(x, H / 2 - 1, 5, 3, x % 2 == 0 ? HabitatAmber : lit);
            return c.ToSprite(new Vector2(.5f, 0), 64);
        });

        public static Sprite HabitatTable() => Cached($"hab-table-{HabitatTheme}", () =>
        {
            var c = new PixelCanvas(160, 80); c.Rect(16, 0, 12, 34, HabitatInk); c.Rect(132, 0, 12, 34, HabitatInk);
            c.Rect(18, 4, 4, 24, HabitatMetal); c.Rect(134, 4, 4, 24, HabitatMetal);
            HBPanel(c, 0, 28, 160, 52, HabitatShade); HBPanel(c, 6, 43, 148, 32, HabitatMetal);
            c.Rect(15, 50, 52, 18, PixelCanvas.Hex("#315761")); c.Rect(19, 53, 16, 10, HabitatInk);
            c.Line(36, 56, 53, 63, HabitatCyan); c.Rect(103, 51, 18, 15, Pal.Ice); c.HLine(107, 116, 61, HabitatShade);
            c.Rect(128, 54, 13, 10, HabitatAmber); HBBolt(c, 11, 68); HBBolt(c, 148, 48);
            return c.ToSprite(new Vector2(.5f, 0), 64);
        });

        public static Sprite HabitatLabel(string code, Color32 accent) => Cached($"hab-label-{HabitatTheme}-{code}", () =>
        {
            var c = new PixelCanvas(96, 28); HBPanel(c, 0, 0, 96, 28, HabitatShade); c.Rect(5, 5, 6, 18, accent);
            int x = 18;
            foreach (char ch in code)
            {
                string glyph = HBGlyph(ch);
                for (int y = 0; y < 5; y++) for (int col = 0; col < 3; col++) if (glyph[y * 3 + col] == '1') c.Rect(x + col * 2, 17 - y * 2, 2, 2, Pal.Ice);
                x += 9;
            }
            return c.ToSprite(new Vector2(.5f, 0), 48);
        });

        static string HBGlyph(char c)
        {
            switch (c) {
                case 'A': return "010101111101101"; case 'B': return "110101110101110"; case 'C': return "011100100100011";
                case 'D': return "110101101101110"; case 'E': return "111100110100111"; case 'H': return "101101111101101";
                case 'I': return "111010010010111"; case 'L': return "100100100100111"; case 'M': return "101111111101101";
                case 'O': return "010101101101010"; case 'P': return "110101110100100"; case 'R': return "110101110101101";
                case 'S': return "011100010001110"; case 'T': return "111010010010010"; case 'U': return "101101101101111";
                case 'W': return "101101111111101"; case '2': return "110001010100111"; default: return "000000000000000";
            }
        }
    }
}
