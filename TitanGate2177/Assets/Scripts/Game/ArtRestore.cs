// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do junk piles, dust, dead machines and a jammed door make the old base read as abandoned at a glance?
// Date: 2026-11-14
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>Abandoned-base props (y = 0 is the bottom row). Original procedural art.</summary>
    public static partial class Art
    {
        /// <summary>A heap of crates, panels and cables; variant 4 = searched (a few scraps left).</summary>
        public static Sprite RJunk(int v) => Prop("rjunk" + v, 30, 22, c =>
        {
            var rng = new System.Random(v * 37 + 11);
            if (v == 4) { c.Rect(4, 0, 10, 3, Pal.Steel0); c.Rect(16, 0, 8, 2, Pal.RustD); c.Line(6, 3, 12, 5, Pal.Red); return; }
            c.Rect(1, 0, 13, 9, PixelCanvas.Hex("#6b5a44")); c.Frame(1, 0, 13, 9, PixelCanvas.Hex("#4c3f30")); c.Line(2, 1, 12, 7, PixelCanvas.Hex("#4c3f30"));
            c.Rect(12, 0, 15, 6, Pal.Steel0); c.HLine(12, 26, 5, Pal.Steel);
            c.Rect(6, 8, 12, 7, Pal.Steel); c.Frame(6, 8, 12, 7, Pal.Steel0); c.Rect(8, 10, 4, 3, Pal.Navy1);
            for (int i = 0; i < 6; i++) c.Set(18 + rng.Next(10), 6 + rng.Next(8), i % 2 == 0 ? Pal.Red : Pal.Blue);
            c.Line(16, 9, 26, 14, Pal.Red); c.Line(17, 9, 27, 13, Pal.Blue);
            c.Speckle(PixelCanvas.Hex("#8a6f52"), 0.12f, v + 3);
        });

        /// <summary>Dust and broken bits on the floor (flat, walk-through).</summary>
        public static Sprite RDebris(int v) => Cached("rdebris" + v, () =>
        {
            var c = new PixelCanvas(40, 22);
            var rng = new System.Random(v * 53 + 7);
            c.Ellipse(20, 11, 18, 9, PixelCanvas.Hex("#7d5a44"));
            c.Ellipse(20, 11, 13, 6, PixelCanvas.Hex("#94694d"));
            for (int i = 0; i < 26; i++) c.Set(4 + rng.Next(32), 3 + rng.Next(16), rng.NextDouble() < 0.5 ? PixelCanvas.Hex("#5f4535") : PixelCanvas.Hex("#b38463"));
            c.Rect(8 + v * 3, 8, 5, 2, Pal.Steel); c.Rect(24, 12 - v, 4, 3, Pal.Steel0); c.Line(14, 6, 22, 9, Pal.Grey);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        /// <summary>The base generator: dead (dark, cut cables) or running (green lights).</summary>
        public static Sprite RGenerator(bool on) => Prop("rgen" + on, 34, 30, c =>
        {
            c.RoundRect(1, 0, 32, 26, Pal.Steel0); c.Rect(3, 2, 28, 20, Pal.Steel);
            for (int i = 0; i < 6; i++) c.HLine(5, 28, 5 + i * 3, Pal.Steel0);
            c.Rect(5, 22, 8, 5, Pal.Navy1); c.Rect(6, 23, 6, 3, on ? Pal.Green : Pal.RedD);
            c.Disc(25, 24, 3, on ? Pal.Amber : Pal.Steel0);
            if (!on) { c.Line(30, 4, 33, 1, Pal.Red); c.Line(29, 6, 33, 9, Pal.Blue); }
            else for (int i = 0; i < 3; i++) c.Set(16 + i * 3, 24, Pal.Green);
        });

        public static Sprite RBreaker(bool on) => Prop("rbrk" + on, 18, 22, c =>
        {
            c.RoundRect(1, 0, 16, 21, Pal.Steel0); c.Rect(3, 2, 12, 17, Pal.Navy1);
            for (int i = 0; i < 4; i++) { c.Rect(4, 4 + i * 4, 4, 2, Pal.SteelL); c.Rect(on ? 6 : 4, 4 + i * 4, 2, 2, on ? Pal.Green : Pal.Red); }
            c.Rect(10, 14, 4, 4, on ? Pal.Green : Pal.Amber);
        });

        public static Sprite RHeater(bool on) => Prop("rheat" + on, 20, 20, c =>
        {
            c.RoundRect(1, 0, 18, 18, Pal.Steel0);
            for (int i = 0; i < 4; i++) c.HLine(3, 16, 3 + i * 4, on ? Pal.OrangeL : Pal.Steel);
            c.Rect(14, 15, 3, 2, on ? Pal.Green : Pal.RedD);
        });

        /// <summary>A storage door stuck half open, warning stripes on it.</summary>
        public static Sprite RJammedDoor() => Prop("rjam", 34, 24, c =>
        {
            c.Rect(0, 0, 34, 22, Pal.Steel0); c.Rect(2, 2, 30, 18, Pal.Steel);
            for (int i = 0; i < 6; i++) c.Line(2 + i * 6, 2, 6 + i * 6, 8, Pal.Amber);
            c.Rect(15, 2, 3, 18, Pal.Ink); c.Line(16, 20, 18, 23, Pal.Steel0);
        });

        /// <summary>ORBI's face for chat bubbles: the round companion drone, eye lit cyan.</summary>
        public static Sprite ROrbiFace() => Cached("rorbi-face", () =>
        {
            var c = new PixelCanvas(24, 24);
            c.Disc(12, 12, 11, Pal.SteelL); c.Disc(12, 12, 9, Pal.White);
            c.Disc(12, 13, 5, Pal.Navy1); c.Disc(12, 13, 3, Pal.Cyan); c.Set(13, 15, Pal.White);
            c.Rect(5, 4, 14, 2, Pal.CyanD);
            c.Outline(Pal.Ink);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });
    }

    public static partial class Art
    {
        /// <summary>
        /// The Mars sky from the viewing spot, 480x270. Night: deep blue-black, stars, Phobos (bigger, lumpy),
        /// Deimos (a tiny dot), Earth as a bright blue star, dust glow on the horizon, the base silhouette with
        /// lit windows; optional faint green aurora. Dusk: the real blue Martian sunset.
        /// </summary>
        public static Sprite RSky(bool night, bool aurora, int seed) => Cached($"rsky_{night}_{aurora}_{seed % 4}", () =>
        {
            const int W = 480, H = 270;
            var c = new PixelCanvas(W, H);
            var rng = new System.Random(seed * 131 + 9);
            for (int y = 0; y < H; y++)
            {
                float t = y / (float)H;   // 0 = horizon, 1 = top
                Color32 col = night
                    ? Mix(PixelCanvas.Hex("#3a2a3a"), Mix(PixelCanvas.Hex("#0c1230"), PixelCanvas.Hex("#03040c"), (t - 0.25f) / 0.75f), t / 0.25f)
                    : Mix(PixelCanvas.Hex("#6f86b8"), Mix(PixelCanvas.Hex("#b98a63"), PixelCanvas.Hex("#4d3a3f"), (t - 0.35f) / 0.65f), t / 0.35f);
                c.HLine(0, W - 1, y, col);
            }
            if (night)
            {
                for (int i = 0; i < 520; i++)
                {
                    int x = rng.Next(W), y = 40 + rng.Next(H - 40);
                    byte b = (byte)(140 + rng.Next(115));
                    c.Set(x, y, new Color32(b, b, (byte)Mathf.Min(255, b + 20), 255));
                    if (rng.NextDouble() < 0.04) { c.Set(x + 1, y, new Color32(b, b, b, 160)); c.Set(x, y + 1, new Color32(b, b, b, 160)); }
                }
                if (aurora)
                    for (int x = 0; x < W; x++)
                    {
                        int top = 120 + Mathf.RoundToInt(Mathf.Sin(x * 0.03f + seed) * 18 + Mathf.Sin(x * 0.011f) * 25);
                        for (int y = 40; y < top; y++) { float a = (1 - (y - 40f) / (top - 40f)) * 0.35f; c.Set(x, y, Mix(c.Get(x, y), PixelCanvas.Hex("#5be38a"), a)); }
                    }
                // Phobos: lumpy, lit from one side; Deimos: a small bright dot; Earth: a blue star
                c.Ellipse(330, 200, 11, 8, PixelCanvas.Hex("#8c7a6e")); c.Ellipse(333, 202, 7, 5, PixelCanvas.Hex("#b6a596")); c.Set(326, 198, PixelCanvas.Hex("#5e5049"));
                c.Disc(140, 215, 2, PixelCanvas.Hex("#d8cfc4"));
                c.Disc(70, 160, 2, PixelCanvas.Hex("#7fb7ff")); c.Set(70, 163, PixelCanvas.Hex("#3d7bea")); c.Set(73, 160, PixelCanvas.Hex("#3d7bea")); c.Set(67, 160, PixelCanvas.Hex("#3d7bea")); c.Set(70, 157, PixelCanvas.Hex("#3d7bea"));
            }
            else
            {
                // blue glow round the setting Sun
                const int R = 95, sx = 250, sy = 50;
                for (int y = Mathf.Max(0, sy - R); y < Mathf.Min(H, sy + R); y++)
                    for (int x = sx - R; x < sx + R; x++)
                    {
                        float d = Mathf.Sqrt((x - sx) * (x - sx) + (y - sy) * (y - sy)) / R;
                        if (d >= 1 || x < 0 || x >= W) continue;
                        c.Set(x, y, Mix(c.Get(x, y), PixelCanvas.Hex("#cfe0ff"), (1 - d) * (1 - d) * 0.85f));
                    }
                c.Disc(sx, sy, 4, PixelCanvas.Hex("#ffffff"));
            }
            // ground and base silhouette with lit windows
            for (int x = 0; x < W; x++)
            {
                int g = 40 + Mathf.RoundToInt(Mathf.Sin(x * 0.02f + seed) * 6 + Mathf.Sin(x * 0.07f) * 3);
                c.VLine(x, 0, g, night ? PixelCanvas.Hex("#160c10") : PixelCanvas.Hex("#3a2320"));
            }
            c.Rect(170, 40, 120, 26, night ? PixelCanvas.Hex("#0b0b14") : PixelCanvas.Hex("#2a1c1c"));
            c.Disc(230, 66, 22, night ? PixelCanvas.Hex("#0b0b14") : PixelCanvas.Hex("#2a1c1c"));
            for (int i = 0; i < 5; i++) c.Rect(182 + i * 22, 52, 8, 5, night ? PixelCanvas.Hex("#ffcf6e") : PixelCanvas.Hex("#6e4b3a"));
            c.Line(300, 60, 320, 90, night ? PixelCanvas.Hex("#0b0b14") : PixelCanvas.Hex("#2a1c1c")); c.Disc(322, 92, 6, night ? PixelCanvas.Hex("#0b0b14") : PixelCanvas.Hex("#2a1c1c"));
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });
    }

    public static partial class Art
    {
        /// <summary>A five-pointed star for the end screen (gold when earned, dark when not).</summary>
        public static Sprite RStar(bool on) => Cached("rstar" + on, () =>
        {
            const int S = 48; var c = new PixelCanvas(S, S);
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++) { float a = Mathf.PI / 2 + i * Mathf.PI / 5; float r = i % 2 == 0 ? 23 : 10; pts[i] = new Vector2(24 + Mathf.Cos(a) * r, 23 + Mathf.Sin(a) * r); }
            Color32 fill = on ? Pal.Gold : PixelCanvas.Hex("#2b3245"), hi = on ? PixelCanvas.Hex("#fff0a8") : PixelCanvas.Hex("#3a4258");
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    bool inside = false; var p = new Vector2(x + 0.5f, y + 0.5f);
                    for (int i = 0, j = 9; i < 10; j = i++)
                        if ((pts[i].y > p.y) != (pts[j].y > p.y) && p.x < (pts[j].x - pts[i].x) * (p.y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x) inside = !inside;
                    if (inside) c.Set(x, y, y > 26 && x < 26 ? hi : fill);
                }
            c.Outline(Pal.Ink);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        /// <summary>The rescue lander: white capsule, gold band, legs; flame under it when burning.</summary>
        public static Sprite RRescueShip(bool flame) => Cached("rship" + flame, () =>
        {
            var c = new PixelCanvas(40, 52);
            if (flame) { c.Ellipse(20, 6, 6, 7, Pal.Amber); c.Ellipse(20, 8, 3, 5, PixelCanvas.Hex("#fff3c4")); }
            c.Line(6, 14, 11, 22, Pal.Steel); c.Line(34, 14, 29, 22, Pal.Steel); c.HLine(4, 8, 14, Pal.Steel0); c.HLine(32, 36, 14, Pal.Steel0);
            c.RoundRect(10, 16, 20, 26, Pal.White); c.Rect(10, 24, 20, 3, Pal.Gold);
            c.Ellipse(20, 44, 9, 6, Pal.White); c.Disc(20, 34, 4, Pal.CyanD); c.Disc(19, 35, 2, Pal.CyanL);
            c.Rect(18, 49, 4, 3, Pal.Red);
            c.Outline(Pal.Ink);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });
    }

    public static partial class Art
    {
        /// <summary>A thought bubble with a mood mark: heart (happy), sweat drop (nervous), snowflake-ish shiver lines (cold), Zz (tired), dots (creepy).</summary>
        public static Sprite RMood(string kind) => Cached("rmood" + kind, () =>
        {
            var c = new PixelCanvas(32, 32);
            c.Disc(16, 18, 13, Pal.White); c.Disc(6, 5, 3, Pal.White); c.Disc(3, 1, 1, Pal.White);
            switch (kind)
            {
                case "happy": c.Disc(12, 21, 4, Pal.Red); c.Disc(20, 21, 4, Pal.Red); for (int y = 0; y < 8; y++) c.HLine(9 + y, 23 - y, 19 - y, Pal.Red); break;
                case "nervous": c.Disc(16, 14, 5, Pal.Blue); for (int y = 0; y < 7; y++) c.HLine(16 - y / 3, 16 + y / 3, 25 - y, Pal.Blue); c.Set(14, 15, Pal.CyanL); break;
                case "tired": c.HLine(9, 15, 25, Pal.Navy3); c.Line(15, 25, 9, 19, Pal.Navy3); c.HLine(9, 15, 19, Pal.Navy3); c.HLine(17, 22, 15, Pal.Navy3); c.Line(22, 15, 17, 10, Pal.Navy3); c.HLine(17, 22, 10, Pal.Navy3); break;
                case "creepy": c.Disc(10, 18, 2, Pal.RedD); c.Disc(16, 18, 2, Pal.RedD); c.Disc(22, 18, 2, Pal.RedD); break;
                default: for (int i = 0; i < 3; i++) { c.VLine(10 + i * 6, 11, 25, Pal.CyanD); c.Set(9 + i * 6, 14 + i * 3, Pal.CyanD); c.Set(11 + i * 6, 20 - i * 2, Pal.CyanD); } break;
            }
            c.Outline(Pal.Ink);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });
    }

    /// <summary>Short names used by GameFlow.Restore.cs.</summary>
    public static class ArtRestore
    {
        public static Sprite Junk(int v) => Art.RJunk(v);
        public static Sprite Debris(int v) => Art.RDebris(v);
        public static Sprite Generator(bool on) => Art.RGenerator(on);
        public static Sprite Breaker(bool on) => Art.RBreaker(on);
        public static Sprite Heater(bool on) => Art.RHeater(on);
        public static Sprite JammedDoor() => Art.RJammedDoor();
        public static Sprite OrbiFace() => Art.ROrbiFace();
    }
}
