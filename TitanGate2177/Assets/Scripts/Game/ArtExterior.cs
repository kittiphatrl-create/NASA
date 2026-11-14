// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>Exterior art for the title screen and cutscenes: the ship, its flames, planets, effects.</summary>
    public static partial class Art
    {
        static Sprite Centered(string key, int w, int h, System.Action<PixelCanvas> draw, bool outline = true, float ppu = PixelCanvas.PPU) => Cached(key, () =>
        {
            var c = new PixelCanvas(w, h);
            draw(c);
            if (outline) { c.Shade(); c.Outline(Pal.Ink); }
            return c.ToSprite(new Vector2(0.5f, 0.5f), ppu);
        });

        /// <summary>The Kestrel seen from the side, nose to the right. shield = heat shield under the belly.</summary>
        public static Sprite ShipExterior(bool shield, bool legs, bool damaged) => Centered($"shipx2{shield}{legs}{damaged}", 120, 52, c =>
        {
            var hull = PixelCanvas.Hex("#e9edf4"); var hullD = PixelCanvas.Hex("#b9c1d2"); var hullDD = PixelCanvas.Hex("#8a93a8");
            // engine nacelles above and below the tail
            c.RoundRect(6, 36, 34, 9, hullD); c.RoundRect(6, 7, 34, 9, hullD);
            c.Rect(6, 38, 30, 2, hullDD); c.Rect(6, 9, 30, 2, hullDD);
            c.Rect(2, 37, 6, 7, Pal.Steel0); c.Rect(2, 8, 6, 7, Pal.Steel0);
            c.Rect(36, 39, 6, 3, Pal.Orange); c.Rect(36, 10, 6, 3, Pal.Orange);
            // main fuselage: tapered toward the nose
            for (int x = 10; x < 104; x++)
            {
                int half = x < 90 ? 12 : 12 - (x - 90) * 12 / 14;
                c.VLine(x, 26 - half, 26 + half, hull);
                c.Set(x, 26 - half, hullDD); c.Set(x, 26 + half, hullDD);
                c.VLine(x, 26 - half + 1, 26 - half + 3, hullD);
            }
            // nose cone and cockpit canopy
            for (int x = 104; x < 117; x++) c.VLine(x, 26 - (117 - x) / 3, 26 + (117 - x) / 3, x > 112 ? Pal.Grey : hull);
            c.RoundRect(88, 27, 16, 7, Pal.Ink); c.RoundRect(89, 28, 14, 5, Pal.BlueD); c.Rect(92, 31, 7, 1, Pal.CyanL); c.Set(100, 30, Pal.CyanL);
            // engine block at the tail
            c.Rect(4, 18, 10, 16, Pal.Steel); c.Rect(2, 20, 4, 12, Pal.Steel0); c.Rect(1, 22, 2, 8, Pal.Ink);
            // stripes, windows, panel lines, emblem
            c.Rect(14, 21, 78, 2, Pal.Orange); c.Rect(14, 23, 78, 1, Pal.OrangeD);
            for (int x = 22; x < 86; x += 7) { c.Rect(x, 26, 3, 3, Pal.Navy2); c.Set(x + 1, 28, Pal.CyanL); }
            for (int x = 30; x < 90; x += 18) c.VLine(x, 16, 36, hullD);
            c.Disc(60, 33, 3, Pal.BlueD); c.Disc(60, 33, 2, Pal.White); c.Set(60, 33, Pal.Red);
            // radiator fins and antenna on the back
            for (int i = 0; i < 4; i++) c.Rect(44 + i * 6, 39, 4, 5, Pal.Steel);
            c.VLine(70, 38, 46, Pal.Steel); c.Disc(70, 47, 2, Pal.Red);
            // cargo bay doors underneath
            c.Rect(40, 14, 30, 3, Pal.Steel0); c.HLine(40, 69, 15, Pal.SteelL);
            if (shield) { c.Rect(16, 2, 92, 5, PixelCanvas.Hex("#5b4636")); c.Rect(16, 2, 92, 2, PixelCanvas.Hex("#3d2e24")); }
            if (legs)
            {
                c.Line(20, 14, 4, 2, Pal.SteelL); c.Line(21, 14, 5, 2, Pal.Steel); c.Rect(0, 0, 6, 3, Pal.Steel0);
                c.Line(20, 38, 4, 50, Pal.SteelL); c.Line(21, 38, 5, 50, Pal.Steel); c.Rect(0, 49, 6, 3, Pal.Steel0);
            }
            if (damaged)
            {
                c.Disc(62, 26, 5, Pal.Ink); c.Disc(62, 26, 3, Pal.RedD); c.Disc(61, 27, 1, Pal.Orange);
                c.Line(54, 31, 70, 20, Pal.Ink); c.Line(56, 19, 68, 33, Pal.Ink);
                c.Rect(64, 30, 3, 2, Pal.Amber);
            }
        });

        /// <summary>Engine flame pointing left (rotate with the ship). frame 0/1 flicker.</summary>
        public static Sprite Flame(int frame, bool big) => Centered($"flame{frame}{big}", big ? 48 : 30, 16, c =>
        {
            int len = big ? (frame == 0 ? 44 : 38) : (frame == 0 ? 26 : 22);
            for (int i = 0; i < len; i++)
            {
                float k = i / (float)len;
                int half = Mathf.RoundToInt(Mathf.Lerp(6, 0, k * k));
                var col = k < 0.2f ? Pal.White : k < 0.45f ? PixelCanvas.Hex("#fff0b0") : k < 0.75f ? Pal.Amber : Pal.Orange;
                int x = c.W - 1 - i;
                c.VLine(x, 8 - half, 7 + half, col);
            }
        }, outline: false);

        public static Sprite Parachute() => Centered("chute", 72, 56, c =>
        {
            for (int x = 0; x < 72; x++)
            {
                float dx = (x - 36) / 36f;
                int top = 55 - Mathf.RoundToInt(dx * dx * 14);
                for (int y = top - 12; y <= top; y++)
                    c.Set(x, y, ((x / 8) % 2 == 0) ? Pal.White : Pal.Orange);
            }
            for (int i = 0; i <= 6; i++) c.Line(2 + i * 11, 30 + (i == 0 || i == 6 ? 2 : 0), 36, 0, Pal.Grey);
        });

        public static Sprite Plasma(int frame) => Centered("plasma" + frame, 64, 28, c =>
        {
            for (int y = 0; y < 28; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x - 32) / 32f, dy = (y - 22) / 18f;
                    float d = dx * dx + dy * dy;
                    if (d < 1f && y < 24 && ((x + y + frame) % 3 != 0 || d < 0.4f))
                        c.Set(x, y, d < 0.25f ? Pal.White : d < 0.5f ? PixelCanvas.Hex("#fff0b0") : d < 0.75f ? Pal.Amber : Pal.Orange);
                }
        }, outline: false);

        public static Sprite Debris() => Centered("debris", 14, 12, c =>
        {
            c.RoundRect(2, 2, 10, 8, Pal.Steel); c.Rect(3, 6, 4, 3, Pal.SteelL); c.Set(9, 3, Pal.Steel0); c.Set(5, 3, Pal.Steel0);
        });

        public static Sprite Streak() => Centered("streak", 64, 4, c =>
        {
            for (int x = 0; x < 64; x++) { var a = (byte)(x * 4); c.Set(x, 1, new Color32(255, 230, 180, a)); c.Set(x, 2, new Color32(255, 200, 120, (byte)(a / 2))); }
        }, outline: false);

        public static Sprite Puff() => Centered("puff", 10, 10, c => { c.Disc(5, 5, 4, PixelCanvas.Hex("#c9c3bb")); c.Disc(4, 6, 2, Pal.White); }, outline: false);

        public static Sprite Jupiter() => Centered("jupiter", 96, 96, c =>
        {
            c.Disc(48, 48, 46, PixelCanvas.Hex("#d8b48a"));
            Color32[] bands = { PixelCanvas.Hex("#b98b62"), PixelCanvas.Hex("#e8d3b0"), PixelCanvas.Hex("#a8744c"), PixelCanvas.Hex("#d9c19c") };
            for (int y = 2; y < 94; y++)
            {
                var col = bands[(y / 7) % bands.Length];
                for (int x = 0; x < 96; x++) if (c.Get(x, y).a > 0) c.Set(x, y, col);
            }
            c.Ellipse(62, 34, 7, 4, PixelCanvas.Hex("#b2452e"));
            for (int y = 0; y < 96; y++) for (int x = 0; x < 30; x++) if (c.Get(x, y).a > 0 && x < 8 + Mathf.Abs(y - 48) / 3) c.Set(x, y, PixelCanvas.Hex("#6b4a32"));
        }, outline: false, ppu: 8f);

        public static Sprite MoonDisc() => Centered("moondisc", 80, 80, c =>
        {
            c.Disc(40, 40, 38, PixelCanvas.Hex("#a4a7b0"));
            var rng = new System.Random(5);
            for (int i = 0; i < 18; i++) { int x = rng.Next(12, 68), y = rng.Next(12, 68); if (c.Get(x, y).a > 0) c.Disc(x, y, rng.Next(2, 6), PixelCanvas.Hex("#80838d")); }
            for (int y = 0; y < 80; y++) for (int x = 0; x < 80; x++) if (c.Get(x, y).a > 0 && x > 56 + Mathf.Abs(y - 40) / 4) c.Set(x, y, PixelCanvas.Hex("#5d606b"));
        }, outline: false, ppu: 8f);

        public static Sprite TitanDisc() => Centered("titandisc", 40, 40, c =>
        {
            c.Disc(20, 20, 18, PixelCanvas.Hex("#d99a45"));
            c.Disc(20, 20, 15, PixelCanvas.Hex("#c7843a"));
            for (int y = 0; y < 40; y++) for (int x = 0; x < 40; x++) if (c.Get(x, y).a > 0 && x < 9 + Mathf.Abs(y - 20) / 3) c.Set(x, y, PixelCanvas.Hex("#7c4a1e"));
        }, outline: false, ppu: 8f);

        public static Sprite RescueShip() => Centered("rescue", 80, 30, c =>
        {
            c.RoundRect(10, 8, 56, 14, Pal.White); c.Rect(10, 8, 56, 3, Pal.Grey);
            for (int i = 0; i < 12; i++) c.VLine(66 + i, 9 + i / 3, 20 - i / 3, Pal.White);
            c.Rect(12, 14, 52, 2, Pal.Blue); c.Rect(70, 15, 5, 3, Pal.CyanL);
            c.Rect(2, 10, 8, 10, Pal.Steel0); c.Rect(30, 22, 20, 6, Pal.BlueD); c.Rect(30, 2, 20, 6, Pal.BlueD);
        });

        /// <summary>The warp vortex: spiral arms (rotate the transform to animate).</summary>
        public static Sprite Vortex() => Centered("vortex", 96, 96, c =>
        {
            for (int arm = 0; arm < 4; arm++)
                for (float t = 0; t < 1f; t += 0.004f)
                {
                    float r = t * 44f, a = t * 9f + arm * Mathf.PI / 2f;
                    int x = 48 + Mathf.RoundToInt(Mathf.Cos(a) * r), y = 48 + Mathf.RoundToInt(Mathf.Sin(a) * r);
                    var col = t < 0.25f ? Pal.White : t < 0.6f ? Pal.CyanL : Pal.Cyan;
                    c.Set(x, y, col); c.Set(x + 1, y, col);
                }
            c.Disc(48, 48, 5, Pal.White);
        }, outline: false, ppu: 8f);

        public static Sprite Nebula() => Cached("nebula", () =>
        {
            var c = new PixelCanvas(240, 135);
            c.Fill(Pal.Space);
            var rng = new System.Random(77);
            Color32[] cols = { PixelCanvas.Hex("#1b1446"), PixelCanvas.Hex("#2a1650"), PixelCanvas.Hex("#123050"), PixelCanvas.Hex("#3a1846") };
            for (int i = 0; i < 70; i++)
            {
                int x = rng.Next(240), y = rng.Next(135), r = rng.Next(6, 22);
                var col = cols[rng.Next(cols.Length)];
                for (int yy = -r; yy <= r; yy++)
                    for (int xx = -r; xx <= r; xx++)
                        if (xx * xx + yy * yy <= r * r && ((x + xx + y + yy) % 2 == 0 || xx * xx + yy * yy < r * r / 3)) c.Set(x + xx, y + yy, col);
            }
            for (int i = 0; i < 260; i++) c.Set(rng.Next(240), rng.Next(135), rng.NextDouble() < 0.15 ? Pal.CyanL : (rng.NextDouble() < 0.1 ? Pal.Amber : Pal.Grey));
            for (int i = 0; i < 18; i++) { int x = rng.Next(240), y = rng.Next(135); c.Set(x, y, Pal.White); c.Set(x + 1, y, Pal.Grey); c.Set(x - 1, y, Pal.Grey); c.Set(x, y + 1, Pal.Grey); c.Set(x, y - 1, Pal.Grey); }
            return c.ToSprite(new Vector2(0.5f, 0.5f), 4f);
        });

        public static Sprite Sparkle() => Centered("sparkle", 7, 7, c =>
        {
            c.Set(3, 3, Pal.White); c.Set(2, 3, Pal.CyanL); c.Set(4, 3, Pal.CyanL); c.Set(3, 2, Pal.CyanL); c.Set(3, 4, Pal.CyanL);
            c.Set(1, 3, Pal.CyanD); c.Set(5, 3, Pal.CyanD); c.Set(3, 1, Pal.CyanD); c.Set(3, 5, Pal.CyanD);
        }, outline: false);

        public static Sprite SkyGradient(string key, string top, string bottom) => Cached("grad" + key, () =>
        {
            var c = new PixelCanvas(8, 64);
            for (int y = 0; y < 64; y++) c.HLine(0, 7, y, Color32.Lerp(PixelCanvas.Hex(bottom), PixelCanvas.Hex(top), y / 63f));
            return c.ToSprite(new Vector2(0.5f, 0.5f), 2f);
        });

        static readonly string[] StarRows =
        {
            ".....#.....",
            "....###....",
            "....###....",
            "###########",
            ".#########.",
            "..#######..",
            "..#######..",
            ".####.####.",
            ".###...###.",
            "##.......##",
        };

        /// <summary>A debrief star for UI Toolkit: gold when earned, dark when not.</summary>
        public static Texture2D StarIcon(bool on) => Cached("star" + on, () =>
        {
            var c = new PixelCanvas(13, 12);
            var fill = on ? Pal.Gold : new Color32(40, 50, 80, 255);
            for (int r = 0; r < StarRows.Length; r++)
                for (int x = 0; x < StarRows[r].Length; x++)
                    if (StarRows[r][x] == '#') c.Set(x + 1, 10 - r, fill);
            if (on) { c.Set(6, 8, Pal.White); c.Set(5, 7, Pal.White); }
            c.Outline(on ? Pal.GoldD : new Color32(90, 110, 160, 255));
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        }).texture;
    }
}
