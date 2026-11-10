// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>Palette. Navy = ship, cyan = screens/AI/hotspots, rust = Mars, amber = warning, red = critical.</summary>
    public static class Pal
    {
        public static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        public static readonly Color32 Ink = PixelCanvas.Hex("#121624");
        public static readonly Color32 Space = PixelCanvas.Hex("#070a17");
        public static readonly Color32 Navy0 = PixelCanvas.Hex("#121a33");
        public static readonly Color32 Navy1 = PixelCanvas.Hex("#1b2547");
        public static readonly Color32 Navy2 = PixelCanvas.Hex("#25335e");
        public static readonly Color32 Navy3 = PixelCanvas.Hex("#33457c");
        public static readonly Color32 Steel0 = PixelCanvas.Hex("#3b4566");
        public static readonly Color32 Steel = PixelCanvas.Hex("#5d6a8e");
        public static readonly Color32 SteelL = PixelCanvas.Hex("#93a1c2");
        public static readonly Color32 White = PixelCanvas.Hex("#eef2f8");
        public static readonly Color32 Grey = PixelCanvas.Hex("#aab3c6");
        public static readonly Color32 Cyan = PixelCanvas.Hex("#43e3ff");
        public static readonly Color32 CyanD = PixelCanvas.Hex("#1a8db0");
        public static readonly Color32 CyanL = PixelCanvas.Hex("#c4f7ff");
        public static readonly Color32 Amber = PixelCanvas.Hex("#ffbf3f");
        public static readonly Color32 AmberD = PixelCanvas.Hex("#b87a1c");
        public static readonly Color32 Red = PixelCanvas.Hex("#ff4d5a");
        public static readonly Color32 RedD = PixelCanvas.Hex("#9c2236");
        public static readonly Color32 Green = PixelCanvas.Hex("#5be38a");
        public static readonly Color32 GreenD = PixelCanvas.Hex("#23864c");
        public static readonly Color32 Orange = PixelCanvas.Hex("#ec8434");
        public static readonly Color32 OrangeD = PixelCanvas.Hex("#b35722");
        public static readonly Color32 OrangeL = PixelCanvas.Hex("#ffae63");
        public static readonly Color32 Skin = PixelCanvas.Hex("#f1bf92");
        public static readonly Color32 SkinD = PixelCanvas.Hex("#c98d63");
        public static readonly Color32 Hair = PixelCanvas.Hex("#3d2a22");
        public static readonly Color32 Boot = PixelCanvas.Hex("#363b55");
        public static readonly Color32 Rust = PixelCanvas.Hex("#c2582d");
        public static readonly Color32 RustD = PixelCanvas.Hex("#7c3320");
        public static readonly Color32 RustDD = PixelCanvas.Hex("#4e1f17");
        public static readonly Color32 RustL = PixelCanvas.Hex("#e1895a");
        public static readonly Color32 Sand = PixelCanvas.Hex("#d9a46e");
        public static readonly Color32 SkyMars = PixelCanvas.Hex("#d6a27a");
        public static readonly Color32 SkyMarsD = PixelCanvas.Hex("#a96a4c");
        public static readonly Color32 Gold = PixelCanvas.Hex("#e6b84a");
        public static readonly Color32 GoldD = PixelCanvas.Hex("#9e7424");
        public static readonly Color32 Blue = PixelCanvas.Hex("#3d7bea");
        public static readonly Color32 BlueD = PixelCanvas.Hex("#22489a");
        public static readonly Color32 Ice = PixelCanvas.Hex("#d6f3ff");
    }

    public enum Pose { Idle0, Idle1, Walk0, Walk1, Walk2, Walk3, Work0, Work1, Eat0, Eat1, Reach, Kneel0, Kneel1 }

    /// <summary>Every sprite in the game, drawn in code as pixel art and cached.</summary>
    public static partial class Art
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        static Sprite Cached(string key, System.Func<Sprite> make)
        {
            if (!Cache.TryGetValue(key, out var s) || s == null) { s = make(); Cache[key] = s; }
            return s;
        }

        public static void ClearCache() => Cache.Clear();

        // ------------------------------------------------------------------ characters

        public static Sprite Astronaut(bool suit, Pose pose) => Cached($"astro_{suit}_{pose}", () => DrawAstronaut(suit, pose).ToSprite());

        public static PixelCanvas DrawAstronaut(bool suit, Pose pose)
        {
            var c = new PixelCanvas(22, 28);
            Color32 body = suit ? Pal.White : Pal.Orange;
            Color32 bodyD = suit ? Pal.Grey : Pal.OrangeD;
            Color32 bodyL = suit ? Pal.White : Pal.OrangeL;
            Color32 boot = suit ? Pal.Steel : Pal.Boot;
            Color32 hand = suit ? Pal.Grey : Pal.Skin;

            int dy = 0;
            bool kneel = pose == Pose.Kneel0 || pose == Pose.Kneel1;
            switch (pose)
            {
                case Pose.Idle1: case Pose.Walk0: case Pose.Walk2: dy = -1; break;
                case Pose.Kneel0: case Pose.Kneel1: dy = -3; break;
            }

            // legs: far leg (darker) then near leg
            if (kneel)
            {
                c.Rect(7, 1, 4, 2, bodyD); c.Rect(7, 3, 2, 3, bodyD); c.Rect(6, 1, 1, 2, boot);
                c.Rect(10, 4, 4, 2, body); c.Rect(12, 1, 2, 3, body); c.Rect(12, 1, 3, 1, boot);
            }
            else
            {
                int far = 7, near = 10, farLift = 0, nearLift = 0;
                switch (pose)
                {
                    case Pose.Walk0: far = 6; near = 11; break;
                    case Pose.Walk1: far = 8; near = 9; farLift = 1; break;
                    case Pose.Walk2: far = 11; near = 6; break;
                    case Pose.Walk3: far = 9; near = 8; nearLift = 1; break;
                }
                Leg(c, far, farLift, bodyD, boot);
                Leg(c, near, nearLift, body, boot);
            }

            // backpack (suit only)
            if (suit) { c.Rect(4, 10 + dy, 3, 8, Pal.Steel); c.Rect(4, 16 + dy, 3, 1, Pal.SteelL); c.Set(5, 13 + dy, Pal.Cyan); }

            // far arm
            c.Rect(6, 10 + dy, 2, 6, bodyD);

            // torso
            c.Rect(7, 9 + dy, 6, 8, body);
            c.Rect(7, 9 + dy, 1, 8, bodyD);
            c.Rect(12, 10 + dy, 1, 6, bodyL);
            c.Rect(7, 9 + dy, 6, 1, suit ? Pal.Steel : Pal.Boot);
            c.Set(11, 14 + dy, Pal.Blue); c.Set(10, 14 + dy, Pal.White);
            if (suit) c.Rect(8, 12 + dy, 4, 1, Pal.Orange);

            // head
            if (suit)
            {
                c.RoundRect(6, 16 + dy, 8, 8, Pal.White);
                c.Rect(6, 17 + dy, 1, 6, Pal.Grey);
                c.Rect(9, 18 + dy, 5, 4, Pal.Blue);
                c.Rect(9, 18 + dy, 5, 1, Pal.BlueD);
                c.Set(12, 21 + dy, Pal.CyanL); c.Set(13, 20 + dy, Pal.CyanL);
            }
            else
            {
                c.Rect(7, 17 + dy, 6, 6, Pal.Skin);
                c.Rect(7, 17 + dy, 1, 5, Pal.SkinD);
                c.Rect(7, 22 + dy, 6, 1, Pal.Hair);
                c.Rect(8, 23 + dy, 4, 1, Pal.Hair);
                c.Rect(7, 19 + dy, 2, 3, Pal.Hair);
                c.Set(11, 20 + dy, Pal.Ink);
                c.Set(12, 18 + dy, Pal.SkinD);
            }

            // near arm
            switch (pose)
            {
                case Pose.Work0:
                    c.Rect(10, 13 + dy, 2, 3, body); c.Rect(11, 13 + dy, 4, 2, body); c.Rect(15, 13 + dy, 1, 2, hand); break;
                case Pose.Work1:
                    c.Rect(10, 13 + dy, 2, 3, body); c.Rect(11, 12 + dy, 4, 2, body); c.Rect(15, 12 + dy, 1, 2, hand); break;
                case Pose.Eat0:
                case Pose.Eat1:
                    int hy = pose == Pose.Eat0 ? 18 : 17;
                    c.Rect(10, 12 + dy, 2, 3, body); c.Rect(11, 14 + dy, 2, hy - 14, body); c.Rect(11, hy + dy, 2, 1, hand);
                    if (!suit) c.Rect(13, hy + dy, 1, 1, Pal.Amber);
                    break;
                case Pose.Reach:
                    c.Rect(10, 14 + dy, 2, 9, body); c.Rect(10, 23 + dy, 2, 1, hand); break;
                case Pose.Kneel0:
                case Pose.Kneel1:
                    int ky = pose == Pose.Kneel0 ? 9 : 8;
                    c.Rect(10, ky + dy + 3, 2, 3, body); c.Rect(11, ky + dy + 2, 4, 2, body); c.Rect(15, ky + dy + 2, 1, 2, hand); break;
                default:
                    c.Rect(9, 10 + dy, 2, 6, body); c.Rect(9, 10 + dy, 1, 6, bodyD); c.Rect(9, 9 + dy, 2, 1, hand); break;
            }

            c.Shade(0.14f, 0.2f);
            c.Outline(Pal.Ink);
            return c;
        }

        static void Leg(PixelCanvas c, int x, int lift, Color32 col, Color32 boot)
        {
            c.Rect(x, 1 + lift, 2, 8 - lift, col);
            c.Rect(x, 1 + lift, 3, 2, boot);
        }

        public static Sprite Orbi(int frame) => Cached("orbi" + frame, () =>
        {
            var c = new PixelCanvas(18, 16);
            c.Rect(2, 7, 1, 4, Pal.Steel); c.Rect(15, 7, 1, 4, Pal.Steel);
            c.RoundRect(3, 4, 12, 9, Pal.White);
            c.Rect(3, 4, 12, 2, Pal.Grey);
            c.Rect(5, 7, 8, 4, Pal.Navy0);
            bool blink = frame == 2;
            if (blink) { c.Rect(6, 8, 2, 1, Pal.Cyan); c.Rect(10, 8, 2, 1, Pal.Cyan); }
            else { c.Rect(7, 8, 1, 2, Pal.Cyan); c.Rect(10, 8, 1, 2, Pal.Cyan); }
            c.VLine(9, 13, 14, Pal.Steel); c.Set(9, 15, frame == 1 ? Pal.CyanL : Pal.Cyan);
            if (frame == 1) c.Rect(7, 1, 4, 3, Pal.CyanL); else c.Rect(8, 1, 2, 3, Pal.Cyan);
            c.Shade(0.14f, 0.2f);
            c.Outline(Pal.Ink);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        // ------------------------------------------------------------------ room shell

        public const int RoomPxW = 160, RoomPxH = 96, RoomPxD = 56;

        /// <summary>Back wall of a room. windows = list of (x, y, w, h) holes in pixels.</summary>
        public static Sprite BackWall(string key, bool mars, params RectInt[] windows) => Cached("wall_" + key, () =>
        {
            var c = new PixelCanvas(RoomPxW, RoomPxH);
            Color32 baseC = mars ? PixelCanvas.Hex("#2c2f52") : Pal.Navy2;
            c.Fill(baseC);
            for (int y = 0; y < RoomPxH; y += 24) c.HLine(0, RoomPxW - 1, y, Pal.Navy1);
            for (int x = 0; x < RoomPxW; x += 32) c.VLine(x, 0, RoomPxH - 1, Pal.Navy1);
            for (int y = 0; y < RoomPxH; y += 24)
                for (int x = 0; x < RoomPxW; x += 32)
                {
                    c.Set(x + 2, y + 2, Pal.Navy3); c.Set(x + 29, y + 2, Pal.Navy3);
                    c.Set(x + 2, y + 21, Pal.Navy3); c.Set(x + 29, y + 21, Pal.Navy3);
                }
            c.Rect(0, 0, RoomPxW, 7, Pal.Navy1);
            for (int x = 0; x < RoomPxW; x += 8) { c.Rect(x, 0, 4, 2, Pal.AmberD); }
            c.Rect(0, RoomPxH - 6, RoomPxW, 2, Pal.CyanD);
            for (int y = 24; y < RoomPxH - 7; y += 24) c.HLine(0, RoomPxW - 1, y - 1, Pal.Navy3);
            foreach (var w in windows)
            {
                c.Rect(w.x - 3, w.y - 3, w.width + 6, w.height + 6, Pal.Steel);
                c.Frame(w.x - 3, w.y - 3, w.width + 6, w.height + 6, Pal.SteelL);
                c.Rect(w.x, w.y, w.width, w.height, Pal.Clear);
            }
            return c.ToSprite(new Vector2(0.5f, 0f));
        });

        public static Sprite Floor(bool mars) => Cached("floor" + mars, () =>
        {
            var c = new PixelCanvas(RoomPxW, RoomPxD);
            c.Fill(mars ? PixelCanvas.Hex("#2a2638") : PixelCanvas.Hex("#1d2440"));
            for (int x = 0; x < RoomPxW; x += 8) c.VLine(x, 0, RoomPxD - 1, Pal.Navy0);
            for (int y = 0; y < RoomPxD; y += 8) c.HLine(0, RoomPxW - 1, y, Pal.Navy0);
            for (int y = 4; y < RoomPxD; y += 8) for (int x = 4; x < RoomPxW; x += 8) c.Set(x, y, Pal.Steel0);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        public static Sprite Ceiling() => Cached("ceil", () =>
        {
            var c = new PixelCanvas(RoomPxW, RoomPxD);
            c.Fill(Pal.Navy1);
            for (int x = 10; x < RoomPxW; x += 40) c.Rect(x, 18, 20, 8, Pal.CyanD);
            for (int x = 10; x < RoomPxW; x += 40) c.Rect(x + 2, 20, 16, 4, Pal.CyanL);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        public static Sprite SideWall() => Cached("side", () =>
        {
            var c = new PixelCanvas(RoomPxD, RoomPxH);
            c.Fill(Pal.Navy1);
            for (int y = 0; y < RoomPxH; y += 24) c.HLine(0, RoomPxD - 1, y, Pal.Navy0);
            c.Rect(0, 0, RoomPxD, 7, Pal.Navy0);
            return c.ToSprite(new Vector2(0.5f, 0f));
        });

        /// <summary>The cut edge of the hull around a room, seen face-on.</summary>
        public static Sprite HullStrip(int w, int h) => Cached($"hull{w}x{h}", () =>
        {
            var c = new PixelCanvas(w, h);
            c.Fill(Pal.Steel0);
            c.Frame(0, 0, w, h, Pal.Ink);
            if (w > h) { for (int x = 2; x < w - 2; x += 6) c.Rect(x, h / 2 - 1, 3, 2, Pal.Steel); }
            else { for (int y = 2; y < h - 2; y += 6) c.Rect(w / 2 - 1, y, 2, 3, Pal.Steel); }
            return c.ToSprite(new Vector2(0f, 0f));
        });

        // ------------------------------------------------------------------ backdrops

        public static Sprite Starfield() => Cached("stars", () =>
        {
            var c = new PixelCanvas(320, 180);
            c.Fill(Pal.Space);
            var rng = new System.Random(7);
            for (int i = 0; i < 40; i++) c.Disc(rng.Next(320), rng.Next(180), rng.Next(6, 16), PixelCanvas.Hex(i % 2 == 0 ? "#0e1230" : "#140f2c"));
            for (int i = 0; i < 420; i++)
            {
                var col = rng.NextDouble() < 0.15 ? Pal.CyanL : (rng.NextDouble() < 0.1 ? Pal.Amber : Pal.Grey);
                c.Set(rng.Next(320), rng.Next(180), col);
            }
            for (int i = 0; i < 30; i++) { int x = rng.Next(320), y = rng.Next(180); c.Set(x, y, Pal.White); c.Set(x + 1, y, Pal.Grey); c.Set(x - 1, y, Pal.Grey); c.Set(x, y + 1, Pal.Grey); c.Set(x, y - 1, Pal.Grey); }
            return c.ToSprite(new Vector2(0.5f, 0.5f), 8f);
        });

        public static Sprite Saturn() => Cached("saturn", () =>
        {
            var c = new PixelCanvas(96, 48);
            c.Ellipse(48, 24, 46, 9, PixelCanvas.Hex("#8f7a5a"));
            c.Ellipse(48, 24, 40, 6, Pal.Clear);
            c.Disc(48, 24, 17, PixelCanvas.Hex("#d9b77a"));
            for (int y = 10; y < 40; y += 5) c.HLine(30, 66, y, PixelCanvas.Hex("#b8935a"));
            c.Ellipse(48, 24, 17, 17, Pal.Clear);
            c.Disc(48, 24, 17, PixelCanvas.Hex("#d9b77a"));
            for (int y = 9; y <= 39; y += 5) for (int x = 31; x <= 65; x++) if (c.Get(x, y).a > 0) c.Set(x, y, PixelCanvas.Hex("#c49a5e"));
            for (int y = 7; y <= 41; y++) for (int x = 31; x <= 46; x++) if (c.Get(x, y).a > 0 && (x + y) % 3 == 0) c.Set(x, y, PixelCanvas.Hex("#a9864f"));
            // ring front pass
            for (int x = 2; x < 94; x++)
                for (int y = 15; y < 24; y++)
                {
                    float dx = (x - 48) / 46f, dyy = (y - 24) / 9f;
                    float d = dx * dx + dyy * dyy;
                    float dx2 = (x - 48) / 40f, dy2 = (y - 24) / 6f;
                    if (d <= 1f && dx2 * dx2 + dy2 * dy2 > 1f) c.Set(x, y, PixelCanvas.Hex("#e8d3a3"));
                }
            c.Disc(78, 30, 2, PixelCanvas.Hex("#e0a352"));
            return c.ToSprite(new Vector2(0.5f, 0.5f), 8f);
        });

        public static Sprite MarsPlanet() => Cached("marsplanet", () =>
        {
            var c = new PixelCanvas(40, 40);
            c.Disc(20, 20, 18, Pal.Rust);
            for (int y = 2; y < 38; y++) for (int x = 2; x < 38; x++) if (c.Get(x, y).a > 0 && (x * 7 + y * 3) % 11 == 0) c.Set(x, y, Pal.RustD);
            c.Rect(14, 34, 12, 3, Pal.White);
            for (int y = 2; y < 38; y++) for (int x = 2; x < 20; x++) if (c.Get(x, y).a > 0 && x < 8 + (y % 3)) c.Set(x, y, Pal.RustDD);
            return c.ToSprite(new Vector2(0.5f, 0.5f), 8f);
        });

        public static Sprite MarsSky() => Cached("marssky", () =>
        {
            var c = new PixelCanvas(64, 64);
            for (int y = 0; y < 64; y++)
            {
                float t = y / 63f;
                var col = Color32.Lerp(Pal.SkyMars, Pal.SkyMarsD, t);
                c.HLine(0, 63, y, col);
            }
            c.Disc(46, 46, 3, PixelCanvas.Hex("#f6e7c8"));
            return c.ToSprite(new Vector2(0.5f, 0.5f), 2f);
        });

        public static Sprite MarsRidge(int layer) => Cached("ridge" + layer, () =>
        {
            int w = 480, h = layer == 0 ? 70 : 46;
            var c = new PixelCanvas(w, h);
            var rng = new System.Random(11 + layer);
            Color32 col = layer == 0 ? PixelCanvas.Hex("#9a5a40") : Pal.RustD;
            Color32 top = layer == 0 ? PixelCanvas.Hex("#b4704f") : Pal.Rust;
            Color32 strata = layer == 0 ? PixelCanvas.Hex("#86503a") : Pal.RustDD;
            for (int x = 0; x < w; x++)
            {
                int hh = (int)RidgeY(x, w, h, 11 + layer + rng.Next(0, 1), 0.3f, 0.92f);
                c.VLine(x, 0, hh, col);
                c.Set(x, hh, top);
                // sediment layers that follow the hill line: Mars rock was laid down by water and wind
                for (int k = 0; k < 6; k++) { int yb = hh - 4 - k * 6 - (k % 2); if (yb > 0) c.Set(x, yb, strata); }
            }
            return c.ToSprite(new Vector2(0.5f, 0f), 8f);
        });

        public static Sprite MarsGround() => Cached("marsground", () =>
        {
            var c = new PixelCanvas(320, 64);
            c.Fill(Pal.Rust);
            c.Speckle(Pal.RustD, 0.08f, 3);
            c.Speckle(Pal.RustL, 0.04f, 4);
            var rng = new System.Random(9);
            for (int i = 0; i < 40; i++) { int x = rng.Next(320), y = rng.Next(64); c.Rect(x, y, 3, 2, Pal.RustDD); c.Set(x + 1, y + 1, Pal.RustD); }
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        // ------------------------------------------------------------------ props

        static Sprite Prop(string key, int w, int h, System.Action<PixelCanvas> draw, bool outline = true) => Cached(key, () =>
        {
            var c = new PixelCanvas(w, h);
            draw(c);
            if (outline) { c.Shade(); c.Outline(Pal.Ink); }
            return c.ToSprite();
        });

        /// <summary>A smooth, seamless skyline (sum of sines; period = the sprite width).</summary>
        static float RidgeY(int x, int w, int h, int seed, float lo, float hi)
        {
            var r = new System.Random(seed);
            float p1 = (float)r.NextDouble() * 6.283f, p2 = (float)r.NextDouble() * 6.283f, p3 = (float)r.NextDouble() * 6.283f;
            float t = x / (float)w * Mathf.PI * 2f;
            float v = 0.5f * Mathf.Sin(t * 2 + p1) + 0.32f * Mathf.Sin(t * 5 + p2) + 0.18f * Mathf.Sin(t * 11 + p3);
            return Mathf.Lerp(h * lo, h * hi, v * 0.5f + 0.5f);
        }

        public static Sprite Door(bool open, bool airlock) => Prop($"door{open}{airlock}", 22, 40, c =>
        {
            c.Rect(1, 0, 20, 38, Pal.Steel0);
            c.Frame(1, 0, 20, 38, Pal.SteelL);
            if (open) c.Rect(4, 0, 14, 34, Pal.Navy0);
            else
            {
                c.Rect(4, 0, 14, 34, Pal.Steel);
                c.VLine(10, 0, 33, Pal.Steel0); c.VLine(11, 0, 33, Pal.SteelL);
                if (airlock) { c.Disc(11, 24, 4, Pal.Navy0); c.Disc(11, 24, 3, Pal.CyanD); c.Set(12, 25, Pal.CyanL); }
                for (int y = 2; y < 8; y += 2) c.HLine(4, 17, y, Pal.AmberD);
            }
            c.Rect(8, 35, 6, 2, airlock ? Pal.Amber : Pal.Green);
        });

        public static Sprite PilotSeat() => Prop("seat", 22, 24, c =>
        {
            c.Rect(3, 0, 4, 6, Pal.Steel); c.Rect(13, 0, 4, 6, Pal.Steel);
            c.Rect(2, 6, 18, 5, Pal.Navy3); c.Rect(2, 6, 18, 1, Pal.Navy1);
            c.Rect(2, 10, 5, 13, Pal.Navy3); c.Rect(3, 11, 3, 11, Pal.Blue);
            c.Rect(2, 19, 5, 4, Pal.Navy3);
        });

        public static Sprite Console(string key, int w, int h, Color32 screen, bool alarm = false) => Prop("console" + key + alarm, w, h, c =>
        {
            c.Rect(2, 0, w - 4, h / 2, Pal.Steel0);
            c.Rect(0, h / 2 - 2, w, 3, Pal.Steel);
            c.Rect(3, h / 2 + 1, w - 6, h / 2 - 2, Pal.Ink);
            c.Rect(4, h / 2 + 2, w - 8, h / 2 - 4, alarm ? Pal.RedD : PixelCanvas.Hex("#0b2a3a"));
            for (int y = h / 2 + 4; y < h - 3; y += 3) c.HLine(6, 6 + (y * 7) % (w - 14), y, alarm ? Pal.Red : screen);
            for (int x = 4; x < w - 4; x += 4) c.Set(x, h / 2 - 1, (x / 4) % 3 == 0 ? Pal.Amber : Pal.Green);
            c.Rect(5, 2, w - 10, 2, Pal.Steel);
        });

        public static Sprite Telescope() => Prop("scope", 22, 34, c =>
        {
            c.Rect(8, 0, 6, 3, Pal.Steel); c.Rect(10, 3, 2, 14, Pal.Steel0);
            c.Line(3, 18, 18, 30, Pal.SteelL); c.Line(3, 19, 18, 31, Pal.SteelL); c.Line(4, 19, 19, 31, Pal.Steel); c.Line(4, 18, 19, 30, Pal.Steel);
            c.Rect(16, 28, 5, 5, Pal.Steel); c.Rect(17, 29, 3, 3, Pal.Cyan);
            c.Rect(5, 12, 12, 6, Pal.Navy3); c.Rect(6, 13, 10, 4, PixelCanvas.Hex("#0b2a3a")); c.HLine(7, 13, 15, Pal.Cyan);
        });

        public static Sprite NavComputer(bool marsSet) => Prop("nav" + marsSet, 24, 30, c =>
        {
            c.Rect(4, 0, 16, 10, Pal.Steel0);
            c.Rect(1, 10, 22, 19, Pal.Steel);
            c.Rect(3, 12, 18, 15, PixelCanvas.Hex("#071a26"));
            c.Disc(12, 19, 2, Pal.Amber);
            for (int a = 0; a < 360; a += 12) { float r = a * Mathf.Deg2Rad; c.Set(12 + Mathf.RoundToInt(Mathf.Cos(r) * 6), 19 + Mathf.RoundToInt(Mathf.Sin(r) * 5), Pal.CyanD); }
            c.Set(18, 19, marsSet ? Pal.Rust : Pal.Cyan); c.Set(18, 20, marsSet ? Pal.Rust : Pal.Cyan);
            if (marsSet) c.Line(12, 19, 18, 20, Pal.Rust);
        });

        public static Sprite FuelTank(float level, bool alarm) => Prop($"tank{Mathf.RoundToInt(level * 10)}{alarm}", 24, 46, c =>
        {
            c.RoundRect(2, 2, 20, 42, Pal.Steel);
            c.Rect(3, 3, 3, 40, Pal.SteelL);
            c.Rect(17, 3, 3, 40, Pal.Steel0);
            c.Rect(9, 6, 6, 32, Pal.Ink);
            int lv = Mathf.RoundToInt(30 * level);
            c.Rect(10, 7, 4, lv, alarm ? Pal.Red : Pal.Green);
            c.Rect(6, 0, 12, 2, Pal.Steel0);
            c.Rect(2, 40, 20, 2, Pal.AmberD);
        });

        public static Sprite Gauge(int percent, bool alarm) => Prop($"gauge{percent}{alarm}", 20, 24, c =>
        {
            c.Rect(1, 6, 18, 17, Pal.Steel);
            c.Rect(3, 8, 14, 13, Pal.Ink);
            int bars = Mathf.Clamp(Mathf.RoundToInt(percent / 100f * 12), 1, 12);
            for (int i = 0; i < 12; i++) c.Rect(4 + i, 9, 1, 3 + i / 2, i < bars ? (alarm ? Pal.Red : Pal.Green) : Pal.Navy1);
            c.Rect(8, 0, 4, 6, Pal.Steel0);
        });

        public static Sprite Breach(bool sealed_, int frame) => Prop($"breach{sealed_}{frame}", 28, 26, c =>
        {
            if (sealed_)
            {
                c.Rect(4, 4, 20, 18, Pal.Steel);
                c.Frame(4, 4, 20, 18, Pal.SteelL);
                for (int x = 6; x < 22; x += 4) { c.Set(x, 6, Pal.Ink); c.Set(x, 19, Pal.Ink); }
                c.Rect(8, 11, 12, 4, Pal.Amber);
                return;
            }
            c.Disc(14, 13, 8, Pal.Ink);
            c.Line(14, 13, 2, 22, Pal.Ink); c.Line(14, 13, 25, 3, Pal.Ink); c.Line(14, 13, 26, 20, Pal.Ink); c.Line(14, 13, 3, 4, Pal.Ink);
            c.Disc(14, 13, 4, frame == 0 ? Pal.RedD : Pal.Red);
            c.Disc(14, 13, 2, Pal.Space);
            if (frame == 1) { c.Set(20, 18, Pal.Amber); c.Set(7, 8, Pal.Amber); c.Set(22, 9, Pal.White); }
            else { c.Set(19, 7, Pal.Amber); c.Set(8, 18, Pal.White); }
        }, outline: false);

        public static Sprite Locker(bool open) => Prop("locker" + open, 22, 40, c =>
        {
            c.Rect(1, 0, 20, 39, Pal.Steel);
            c.Rect(2, 1, 1, 37, Pal.SteelL);
            if (open) { c.Rect(3, 2, 16, 34, Pal.Navy0); c.Rect(5, 20, 12, 1, Pal.Steel); }
            else
            {
                c.Rect(3, 2, 16, 34, Pal.Steel0);
                for (int y = 30; y < 35; y += 2) c.HLine(5, 16, y, Pal.Steel);
                c.Rect(13, 14, 5, 7, Pal.Ink); c.Rect(14, 18, 3, 2, Pal.Green);
                c.Set(14, 16, Pal.Grey); c.Set(16, 16, Pal.Grey); c.Set(14, 15, Pal.Grey); c.Set(16, 15, Pal.Grey);
                c.Rect(4, 24, 8, 3, Pal.Red); c.Rect(5, 25, 6, 1, Pal.White);
            }
        });

        public static Sprite Shelf(bool withManual) => Prop("shelf" + withManual, 30, 34, c =>
        {
            c.Rect(1, 0, 2, 34, Pal.Steel); c.Rect(27, 0, 2, 34, Pal.Steel);
            for (int y = 0; y < 34; y += 11) c.Rect(1, y, 28, 2, Pal.SteelL);
            c.Rect(4, 2, 6, 8, Pal.Navy3); c.Rect(11, 2, 7, 6, Pal.Steel0); c.Rect(19, 2, 7, 8, Pal.OrangeD);
            c.Rect(4, 13, 3, 8, Pal.BlueD); c.Rect(8, 13, 3, 7, Pal.GreenD); c.Rect(22, 13, 4, 6, Pal.Steel);
            if (withManual) { c.Rect(13, 13, 7, 9, Pal.Amber); c.Rect(14, 15, 5, 1, Pal.Ink); c.Rect(14, 18, 5, 1, Pal.Ink); }
            c.Rect(5, 24, 18, 7, Pal.Navy3); c.Rect(6, 25, 16, 5, PixelCanvas.Hex("#0b2a3a"));
        });

        public static Sprite Crates() => Prop("crates", 34, 26, c =>
        {
            void Box(int x, int y, int w, int h, Color32 col, Color32 d)
            { c.Rect(x, y, w, h, col); c.Frame(x, y, w, h, d); c.Line(x, y, x + w - 1, y + h - 1, d); }
            Box(0, 0, 16, 14, Pal.OrangeD, PixelCanvas.Hex("#7a3a17"));
            Box(17, 0, 16, 12, Pal.Steel, Pal.Steel0);
            Box(5, 14, 14, 11, Pal.Steel0, Pal.Navy1);
        });

        public static Sprite Bed() => Prop("bed", 46, 18, c =>
        {
            c.Rect(0, 0, 46, 7, Pal.Steel0);
            c.Rect(1, 7, 44, 4, Pal.Navy3);
            c.Rect(2, 11, 9, 4, Pal.White);
            c.Rect(12, 9, 33, 4, Pal.BlueD);
            c.Rect(12, 12, 33, 1, Pal.Blue);
            c.Rect(0, 0, 2, 17, Pal.Steel);
        });

        public static Sprite FoodLocker() => Prop("food", 22, 32, c =>
        {
            c.Rect(1, 0, 20, 31, Pal.White);
            c.Rect(1, 0, 2, 31, Pal.Grey);
            c.HLine(3, 19, 15, Pal.Grey);
            c.Rect(15, 18, 2, 6, Pal.Steel); c.Rect(15, 7, 2, 6, Pal.Steel);
            c.Rect(5, 25, 8, 3, Pal.Green); c.Rect(6, 26, 6, 1, Pal.White);
        });

        public static Sprite Toolbox(bool hasSealant) => Prop("toolbox" + hasSealant, 20, 14, c =>
        {
            c.Rect(1, 0, 18, 9, Pal.Red);
            c.Rect(1, 0, 18, 2, Pal.RedD);
            c.Rect(7, 9, 6, 1, Pal.Steel); c.Rect(6, 9, 1, 3, Pal.Steel); c.Rect(13, 9, 1, 3, Pal.Steel); c.Rect(7, 11, 6, 1, Pal.Steel);
            if (hasSealant) { c.Rect(15, 8, 3, 5, Pal.Amber); c.Set(16, 13, Pal.White); }
        });

        public static Sprite MissionBoard(bool alert) => Prop("board" + alert, 32, 22, c =>
        {
            c.Rect(0, 0, 32, 22, Pal.Steel);
            c.Rect(2, 2, 28, 18, PixelCanvas.Hex("#0b2a3a"));
            c.Rect(4, 15, 10, 2, Pal.Cyan);
            for (int i = 0; i < 4; i++) c.Rect(4 + i * 6, 5, 4, 7, i == 2 && alert ? Pal.Amber : Pal.CyanD);
            if (alert) c.Rect(22, 15, 6, 2, Pal.Red);
        });

        public static Sprite Moxie(bool running) => Prop("moxie" + running, 28, 32, c =>
        {
            c.Rect(2, 0, 24, 4, Pal.Steel0);
            c.Rect(3, 4, 22, 20, Pal.Gold);
            c.Rect(3, 4, 3, 20, Pal.GoldD);
            for (int x = 8; x < 24; x += 3) c.VLine(x, 7, 20, Pal.GoldD);
            c.Rect(8, 24, 12, 6, Pal.Steel); c.Rect(10, 26, 8, 2, running ? Pal.Cyan : Pal.Navy1);
            c.Rect(0, 12, 3, 3, Pal.Steel); c.Rect(25, 16, 3, 3, Pal.Steel);
        });

        public static Sprite Recycler() => Prop("recycler", 26, 36, c =>
        {
            c.Rect(0, 0, 26, 4, Pal.Steel0);
            c.RoundRect(2, 4, 9, 28, Pal.Steel); c.Rect(4, 7, 5, 20, Pal.Blue); c.Rect(4, 7, 5, 8, Pal.BlueD);
            c.RoundRect(14, 4, 10, 24, Pal.Steel); c.Rect(16, 7, 6, 16, Pal.Blue);
            c.Rect(10, 26, 5, 2, Pal.SteelL); c.Rect(5, 32, 2, 3, Pal.SteelL);
            c.Set(19, 25, Pal.Green);
        });

        public static Sprite SealPanel() => Prop("sealpanel", 20, 24, c =>
        {
            c.Rect(0, 0, 20, 24, Pal.Steel);
            c.Rect(2, 2, 16, 20, Pal.Steel0);
            c.Disc(6, 15, 3, Pal.White); c.Line(6, 15, 8, 17, Pal.Red);
            c.Disc(14, 15, 3, Pal.White); c.Line(14, 15, 12, 17, Pal.Ink);
            c.Rect(4, 5, 12, 4, Pal.Ink); c.Rect(5, 6, 6, 2, Pal.Green);
        });

        public static Sprite Cabinet(bool withCatalysts) => Prop("cabinet" + withCatalysts, 24, 34, c =>
        {
            c.Rect(0, 0, 24, 33, Pal.Steel);
            c.Rect(2, 2, 20, 29, Pal.Navy0);
            for (int y = 2; y < 31; y += 10) c.Rect(2, y, 20, 1, Pal.SteelL);
            if (withCatalysts)
            {
                c.Rect(4, 13, 4, 6, Pal.Grey); c.Rect(4, 18, 4, 1, PixelCanvas.Hex("#8c6f63"));
                c.Rect(10, 13, 4, 6, Pal.Grey); c.Rect(10, 18, 4, 1, Pal.Green);
                c.Rect(16, 13, 4, 6, Pal.Grey); c.Rect(16, 18, 4, 1, PixelCanvas.Hex("#d27d3f"));
            }
            c.Rect(4, 24, 16, 5, Pal.Steel0); c.Rect(5, 26, 6, 1, Pal.Amber);
        });

        public static Sprite Sabatier(bool ready, bool running) => Prop($"sab{ready}{running}", 30, 42, c =>
        {
            c.Rect(2, 0, 26, 5, Pal.Steel0);
            c.RoundRect(4, 5, 22, 30, Pal.Steel);
            c.Rect(5, 6, 3, 28, Pal.SteelL);
            c.Rect(10, 14, 10, 14, Pal.Ink);
            c.Rect(11, 15, 8, 12, running ? Pal.Amber : (ready ? Pal.CyanD : Pal.Navy1));
            if (running) { c.Rect(13, 17, 4, 8, PixelCanvas.Hex("#fff0b0")); }
            c.Rect(8, 35, 14, 6, Pal.Steel0); c.Rect(10, 37, 10, 2, ready ? Pal.Green : Pal.Red);
            c.Rect(0, 24, 4, 3, Pal.SteelL); c.Rect(26, 10, 4, 3, Pal.SteelL);
        });

        public static Sprite FuelLine(bool fixed_, int frame) => Prop($"line{fixed_}{frame}", 70, 14, c =>
        {
            c.Rect(0, 4, 70, 6, Pal.Steel);
            c.Rect(0, 8, 70, 1, Pal.SteelL);
            for (int x = 6; x < 70; x += 16) c.Rect(x, 3, 3, 8, Pal.Steel0);
            if (frame < 0) { }
            else if (fixed_) { c.Rect(30, 2, 12, 10, Pal.Amber); c.Rect(31, 3, 10, 1, Pal.AmberD); }
            else
            {
                c.Line(32, 4, 36, 9, Pal.Ink); c.Line(36, 9, 39, 4, Pal.Ink);
                c.Set(35 + frame, 12, Pal.CyanL); c.Set(37 - frame, 13, Pal.Grey);
            }
        });

        public static Sprite SolarArray(bool stowed, bool dirty) => Prop($"solar{stowed}{dirty}", 64, 34, c =>
        {
            Color32 cell = dirty ? PixelCanvas.Hex("#5c3e3a") : Pal.BlueD;
            Color32 line = dirty ? Pal.RustD : Pal.Blue;
            for (int i = 0; i < 3; i++)
            {
                int x = 2 + i * 21;
                c.Rect(x + 8, 0, 2, stowed ? 6 : 16, Pal.Steel);
                if (stowed) { c.Rect(x, 6, 18, 3, cell); c.HLine(x, x + 17, 8, line); }
                else
                {
                    for (int k = 0; k < 14; k++) c.Rect(x + k / 2 - 3 + 3, 16 + k, 18, 1, cell);
                    for (int k = 0; k < 14; k += 3) c.HLine(x + k / 2, x + 17 + k / 2, 16 + k, line);
                    if (dirty) c.Speckle(Pal.RustL, 0.0f, 1);
                }
            }
            if (dirty && !stowed) for (int k = 0; k < 40; k++) c.Set(4 + (k * 13) % 58, 17 + (k * 7) % 13, Pal.Rust);
        });

        public static Sprite DrillRig(bool withBit, bool running) => Prop($"drill{withBit}{running}", 26, 38, c =>
        {
            c.Line(2, 0, 12, 34, Pal.Steel); c.Line(23, 0, 13, 34, Pal.Steel); c.Line(12, 0, 12, 34, Pal.Steel0);
            c.Rect(8, 30, 10, 6, Pal.Amber); c.Rect(9, 32, 8, 2, Pal.AmberD);
            c.Rect(12, 8, 2, 22, Pal.SteelL);
            if (withBit) { c.Rect(11, running ? 0 : 3, 4, 6, Pal.Grey); c.Set(12, running ? 0 : 3, Pal.White); }
            if (running) { c.Set(8, 1, Pal.Ice); c.Set(17, 2, Pal.Ice); c.Set(6, 3, Pal.Sand); }
        });

        public static Sprite IceSite() => Prop("icesite", 30, 6, c =>
        {
            c.Ellipse(15, 3, 14, 2, Pal.RustD);
            c.Ellipse(15, 3, 9, 1, Pal.Ice);
            c.Set(10, 4, Pal.White); c.Set(20, 3, Pal.White);
        }, outline: false);

        public static Sprite CargoPod(bool open) => Prop("pod" + open, 34, 24, c =>
        {
            c.RoundRect(1, 0, 32, 20, Pal.White);
            c.Rect(1, 0, 32, 4, Pal.Grey);
            c.Rect(4, 8, 26, 2, Pal.Orange);
            if (open) { c.Rect(5, 11, 24, 8, Pal.Navy0); }
            else { c.Rect(24, 11, 6, 6, Pal.Ink); c.Rect(25, 14, 4, 2, Pal.Amber); }
            c.Speckle(Pal.Sand, 0.05f, 2);
            c.Rect(6, 20, 4, 3, Pal.Steel); c.Rect(24, 20, 4, 3, Pal.Steel);
        });

        public static Sprite Scrap(bool present) => Prop("scrap" + present, 22, 10, c =>
        {
            c.Ellipse(11, 2, 10, 2, Pal.RustD);
            if (present)
            {
                c.Line(3, 2, 16, 7, Pal.SteelL); c.Line(3, 3, 16, 8, Pal.Grey); c.Line(4, 2, 17, 7, Pal.Steel);
                c.Line(4, 3, 17, 8, Pal.Grey); c.Set(10, 6, Pal.White);
            }
        });

        public static Sprite ShipHull() => Prop("shiphull", 48, 80, c =>
        {
            c.Rect(0, 0, 40, 80, Pal.Steel);
            c.Rect(0, 0, 40, 6, Pal.Steel0);
            for (int y = 10; y < 80; y += 14) c.HLine(0, 39, y, Pal.Steel0);
            c.Rect(36, 0, 4, 80, Pal.SteelL);
            c.Rect(40, 30, 6, 4, Pal.Steel0); c.Rect(40, 64, 8, 3, Pal.Steel0);
            c.Rect(14, 6, 18, 34, Pal.Steel0); c.Rect(16, 8, 14, 30, Pal.Navy3); c.Disc(23, 30, 3, Pal.CyanD);
            c.Rect(4, 50, 10, 6, Pal.Orange);
            // grime collects along the bottom; rivets sit on every panel seam (placed, not random)
            for (int x = 0; x < 36; x++) { int dh = 3 + (x * 7) % 4; for (int y = 6; y < 6 + dh; y++) c.Set(x, y, y == 5 + dh ? Pal.Steel : Pal.Steel0); }
            for (int y = 10; y < 80; y += 14) for (int x = 3; x < 36; x += 8) c.Set(x, y + 2, Pal.SteelL);
        });

        public static Sprite Electrolyzer(bool running) => Prop("electro" + running, 28, 32, c =>
        {
            c.Rect(2, 0, 24, 4, Pal.Steel0);
            c.RoundRect(3, 4, 10, 24, Pal.SteelL); c.Rect(5, 7, 6, 16, Pal.Blue); c.Rect(5, 7, 6, 5, Pal.BlueD);
            c.RoundRect(15, 4, 10, 24, Pal.SteelL); c.Rect(17, 7, 6, 16, Pal.White); c.Rect(17, 7, 6, 4, Pal.Grey);
            c.Rect(12, 20, 4, 3, Pal.Steel);
            for (int i = 0; i < 3; i++) c.Set(7 + i, 14 + i * 3, running ? Pal.CyanL : Pal.CyanD);
            c.Rect(8, 28, 12, 3, Pal.Steel0); c.Set(10, 29, Pal.Amber); c.Set(17, 29, running ? Pal.Green : Pal.Grey);
        });

        public static Sprite SupplyBag() => Prop("supplybag", 22, 16, c =>
        {
            c.RoundRect(1, 0, 20, 12, Pal.Orange); c.Rect(1, 0, 20, 3, Pal.OrangeD);
            c.Rect(8, 12, 6, 3, Pal.Boot); c.Rect(9, 4, 4, 5, Pal.White); c.Rect(10, 5, 2, 3, Pal.Red);
        });

        public static Sprite MoonSky() => Cached("moonsky", () =>
        {
            var c = new PixelCanvas(160, 90);
            c.Fill(Pal.Space);
            var rng = new System.Random(21);
            for (int i = 0; i < 180; i++) c.Set(rng.Next(160), rng.Next(90), rng.NextDouble() < 0.2 ? Pal.CyanL : Pal.Grey);
            return c.ToSprite(new Vector2(0.5f, 0.5f), 8f);
        });

        /// <summary>Earth as seen from the Moon: blue, white clouds, half lit.</summary>
        public static Sprite Earth() => Cached("earth", () =>
        {
            var c = new PixelCanvas(28, 28);
            c.Disc(14, 14, 12, Pal.Blue);
            c.Rect(7, 12, 7, 4, Pal.Green); c.Rect(16, 17, 6, 3, Pal.Green); c.Rect(15, 8, 4, 3, Pal.Green);
            c.Rect(5, 18, 9, 1, Pal.White); c.Rect(13, 6, 8, 1, Pal.White); c.Rect(18, 13, 5, 1, Pal.White);
            for (int y = 0; y < 28; y++) for (int x = 0; x < 28; x++) if (c.Get(x, y).a > 0 && x < 9 + (y % 2)) c.Set(x, y, Pal.BlueD);
            return c.ToSprite(new Vector2(0.5f, 0.5f), 8f);
        });

        public static Sprite MoonGround() => Cached("moonground", () =>
        {
            var c = new PixelCanvas(320, 64);
            c.Fill(PixelCanvas.Hex("#8c8f99"));
            c.Speckle(PixelCanvas.Hex("#6f727d"), 0.10f, 31);
            c.Speckle(PixelCanvas.Hex("#b5b8c2"), 0.04f, 32);
            var rng = new System.Random(33);
            for (int i = 0; i < 16; i++) { int x = rng.Next(320), y = rng.Next(64), r = rng.Next(3, 8); c.Ellipse(x, y, r + 1, r / 2 + 1, PixelCanvas.Hex("#b5b8c2")); c.Ellipse(x, y, r, r / 2, PixelCanvas.Hex("#5d606b")); }
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        public static Sprite MoonRidge(int layer) => Cached("moonridge" + layer, () =>
        {
            int w = 480, h = layer == 0 ? 60 : 40;
            var c = new PixelCanvas(w, h);
            var rng = new System.Random(41 + layer);
            Color32 col = layer == 0 ? PixelCanvas.Hex("#4b4e59") : PixelCanvas.Hex("#6d707b");
            Color32 top = layer == 0 ? PixelCanvas.Hex("#d9dbe2") : PixelCanvas.Hex("#a6a9b3");
            for (int x = 0; x < w; x++)
            {
                int hh = (int)RidgeY(x, w, h, 41 + layer + rng.Next(0, 1), 0.2f, 0.7f);
                int prev = (int)RidgeY(x - 1, w, h, 41 + layer, 0.2f, 0.7f);
                c.VLine(x, 0, hh, col);
                c.Set(x, hh, top);
                if (hh >= prev) { c.Set(x, hh - 1, top); c.Set(x, hh - 2, Color32.Lerp(col, top, 0.4f)); }
            }
            return c.ToSprite(new Vector2(0.5f, 0f), 8f);
        });

        public static Sprite Dish(bool fixed_) => Prop("dish" + fixed_, 40, 44, c =>
        {
            c.Rect(18, 0, 4, 22, Pal.Steel); c.Rect(12, 0, 16, 3, Pal.Steel0);
            if (fixed_)
            {
                c.Ellipse(20, 30, 17, 9, Pal.White); c.Ellipse(20, 31, 14, 6, Pal.Grey);
                c.Line(20, 31, 31, 41, Pal.SteelL); c.Rect(30, 40, 3, 3, Pal.Amber); c.Set(31, 43, Pal.Green);
            }
            else
            {
                c.Ellipse(14, 28, 13, 7, Pal.Grey); c.Ellipse(14, 29, 10, 4, Pal.Steel);
                c.Line(4, 22, 26, 34, Pal.Ink); c.Rect(22, 20, 3, 3, Pal.Red);
            }
        });

        public static Sprite CraterSite(bool ready) => Prop("crater" + ready, 44, 16, c =>
        {
            c.Ellipse(22, 7, 21, 6, PixelCanvas.Hex("#b5b8c2"));
            c.Ellipse(22, 6, 18, 4, PixelCanvas.Hex("#2b2d36"));
            c.Rect(16, 5, 6, 1, Pal.Ice); c.Set(27, 6, Pal.Ice); c.Set(12, 5, Pal.CyanL);
            if (ready) { c.Rect(21, 6, 2, 10, Pal.Steel); c.Rect(19, 13, 6, 3, Pal.Amber); }
        }, outline: false);

        public static Sprite TitanSky(float dark) => Cached("titansky" + Mathf.RoundToInt(dark * 10), () =>
        {
            var c = new PixelCanvas(64, 64);
            for (int y = 0; y < 64; y++)
            {
                var col = Color32.Lerp(PixelCanvas.Hex("#c98a3e"), PixelCanvas.Hex("#6b4520"), y / 63f);
                col = Color32.Lerp(col, PixelCanvas.Hex("#2a1a0c"), dark);
                c.HLine(0, 63, y, col);
            }
            c.Ellipse(44, 50, 9, 2, PixelCanvas.Hex("#e0b070"));
            c.Disc(44, 50, 4, PixelCanvas.Hex("#d6a560"));
            return c.ToSprite(new Vector2(0.5f, 0.5f), 2f);
        });

        public static Sprite TitanGround() => Cached("titanground", () =>
        {
            var c = new PixelCanvas(320, 64);
            c.Fill(PixelCanvas.Hex("#6e4a2a"));
            c.Speckle(PixelCanvas.Hex("#4f341d"), 0.09f, 51);
            c.Speckle(PixelCanvas.Hex("#c9c2b0"), 0.03f, 52);
            var rng = new System.Random(53);
            for (int i = 0; i < 30; i++) { int x = rng.Next(320), y = rng.Next(64); c.Rect(x, y, 3, 2, PixelCanvas.Hex("#d8d2c0")); c.Set(x + 1, y, Pal.White); }
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        public static Sprite TitanRidge(int layer) => Cached("titanridge" + layer, () =>
        {
            int w = 480, h = layer == 0 ? 60 : 40;
            var c = new PixelCanvas(w, h);
            var rng = new System.Random(61 + layer);
            Color32 col = layer == 0 ? PixelCanvas.Hex("#6b4a28") : PixelCanvas.Hex("#4e3219");
            for (int x = 0; x < w; x++)
            {
                int hh = (int)RidgeY(x, w, h, 61 + layer + rng.Next(0, 1), 0.25f, 0.85f);
                c.VLine(x, 0, hh, col);
                c.Set(x, hh, Color32.Lerp(col, PixelCanvas.Hex("#e0b070"), 0.35f));
                for (int k = 0; k < 4; k++) { int yb = hh - 5 - k * 7; if (yb > 0) c.Set(x, yb, Color32.Lerp(col, Pal.Ink, 0.25f)); }
            }
            if (layer == 1)
                for (int x = 120; x < 300; x++) { c.Set(x, 3, PixelCanvas.Hex("#1d1a24")); c.Set(x, 4, PixelCanvas.Hex("#2a2733")); c.Set(x, 2, PixelCanvas.Hex("#3b3646")); }
            return c.ToSprite(new Vector2(0.5f, 0f), 8f);
        });

        public static Sprite Rtg(bool glow) => Prop("rtg" + glow, 22, 28, c =>
        {
            c.Rect(4, 0, 14, 3, Pal.Steel0);
            c.Rect(6, 3, 10, 22, Pal.Steel);
            for (int y = 5; y < 24; y += 3) { c.Rect(2, y, 18, 1, Pal.SteelL); }
            c.Rect(9, 10, 4, 6, glow ? Pal.Amber : Pal.RedD);
            c.Rect(8, 25, 6, 2, Pal.Amber); c.Set(10, 27, Pal.Ink);
        });

        public static Sprite Gate(int charge) => Prop("gate" + charge, 64, 72, c =>
        {
            c.Rect(4, 0, 10, 8, Pal.Steel0); c.Rect(50, 0, 10, 8, Pal.Steel0);
            for (int a = 0; a < 360; a += 3)
            {
                float r = a * Mathf.Deg2Rad;
                int x = 32 + Mathf.RoundToInt(Mathf.Cos(r) * 27), y = 38 + Mathf.RoundToInt(Mathf.Sin(r) * 30);
                c.Rect(x - 2, y - 2, 4, 4, Pal.Steel);
            }
            int lit = Mathf.Clamp(charge, 0, 100) * 12 / 100;
            for (int i = 0; i < 12; i++)
            {
                float r = (90 - i * 30) * Mathf.Deg2Rad;
                int x = 32 + Mathf.RoundToInt(Mathf.Cos(r) * 27), y = 38 + Mathf.RoundToInt(Mathf.Sin(r) * 30);
                c.Rect(x - 1, y - 1, 3, 3, i < lit ? Pal.Cyan : Pal.Navy1);
            }
            if (charge >= 100) c.Ellipse(32, 38, 22, 25, PixelCanvas.Hex("#2be3ff"));
            c.Rect(26, 2, 12, 6, Pal.Steel); c.Rect(28, 4, 8, 2, Pal.Amber);
        });

        public static Sprite Rain(int frame) => Cached("rain" + frame, () =>
        {
            var c = new PixelCanvas(3, 8);
            c.VLine(1, 0, 6, PixelCanvas.Hex("#9fb0c8"));
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        public static Sprite Plant() => Prop("plant", 12, 14, c =>
        {
            c.Rect(3, 0, 6, 4, Pal.Steel);
            c.Line(6, 4, 3, 11, Pal.GreenD); c.Line(6, 4, 9, 12, Pal.Green); c.Line(6, 4, 6, 13, Pal.Green);
            c.Set(3, 11, Pal.Green); c.Set(9, 12, Pal.GreenD);
        });

        public static Sprite Spark(int frame) => Cached("spark" + frame, () =>
        {
            var c = new PixelCanvas(8, 8);
            if (frame == 0) { c.Set(4, 4, Pal.White); c.Set(3, 4, Pal.Amber); c.Set(5, 4, Pal.Amber); c.Set(4, 3, Pal.Amber); c.Set(4, 5, Pal.Amber); }
            else { c.Set(2, 2, Pal.Amber); c.Set(6, 6, Pal.Amber); c.Set(2, 6, Pal.White); c.Set(6, 2, Pal.White); }
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        public static Sprite Dust() => Cached("dust", () =>
        {
            var c = new PixelCanvas(4, 4);
            c.Rect(1, 1, 2, 2, Pal.Sand);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        public static Sprite GlowOf(Sprite s, string key) => Cached("glow_" + key, () =>
        {
            var tex = s.texture;
            var c = new PixelCanvas(tex.width, tex.height);
            var px = tex.GetPixels32();
            for (int y = 0; y < tex.height; y++) for (int x = 0; x < tex.width; x++) c.Set(x, y, px[y * tex.width + x]);
            var g = c.Glow(Pal.Cyan);
            var pivot = new Vector2((s.pivot.x + 1) / g.W, (s.pivot.y + 1) / g.H);
            return g.ToSprite(pivot, s.pixelsPerUnit);
        });

        // ------------------------------------------------------------------ item icons (16x16)

        /// <summary>A loose supply lying on the floor during the 60-second scramble.</summary>
        public static Sprite LooseSupply(string id) => Prop("loose_" + id, 16, 16, c => DrawSupply(c, id));

        /// <summary>16x16 drawing of a scramble supply (shared by the floor sprite and the UI icon).</summary>
        static void DrawSupply(PixelCanvas c, string id)
        {
            switch (id)
            {
                case "sup_o2": // oxygen bottle
                    c.Rect(5, 1, 6, 11, Pal.CyanD); c.Rect(6, 2, 4, 9, Pal.Cyan); c.Rect(6, 12, 4, 2, Pal.Steel); c.Rect(7, 14, 2, 2, Pal.SteelL);
                    c.Rect(6, 6, 4, 2, Pal.White); break;
                case "sup_water": // water jug
                    c.Rect(3, 1, 10, 10, Pal.BlueD); c.Rect(4, 2, 8, 7, Pal.Blue); c.Rect(6, 11, 4, 3, Pal.Steel); c.Rect(4, 9, 8, 1, Pal.Ice); break;
                case "sup_food": // ration box
                    c.Rect(2, 1, 12, 9, Pal.OrangeD); c.Rect(2, 8, 12, 2, Pal.Orange); c.Rect(6, 3, 4, 3, Pal.White); c.Rect(7, 2, 2, 5, Pal.Red); break;
                case "sup_battery": // battery pack
                    c.Rect(2, 1, 12, 9, Pal.Steel0); c.Rect(3, 2, 10, 7, Pal.Steel); c.Rect(5, 10, 2, 2, Pal.SteelL); c.Rect(9, 10, 2, 2, Pal.SteelL);
                    c.Rect(6, 4, 4, 3, Pal.Amber); break;
                case "sup_cell": // power cell for the goal machine
                    c.Rect(6, 1, 4, 12, Pal.Steel0); c.Rect(7, 2, 2, 10, Pal.Gold); c.Rect(5, 13, 6, 2, Pal.Steel); c.Set(7, 7, Pal.White); break;
                case "sup_photo": // family photo
                    c.Rect(3, 2, 10, 10, Pal.White); c.Rect(4, 3, 8, 7, Pal.BlueD); c.Rect(4, 3, 8, 2, Pal.Green);
                    c.Rect(6, 5, 2, 3, Pal.Skin); c.Rect(9, 5, 2, 2, Pal.Skin); break;
                case "sup_medkit": // first aid kit
                    c.Rect(2, 1, 12, 9, Pal.White); c.Rect(6, 2, 4, 7, Pal.Red); c.Rect(4, 4, 8, 3, Pal.Red); c.Rect(6, 10, 4, 1, Pal.Grey); break;
                case "sup_filter": // air filter cartridge
                    c.Rect(3, 1, 10, 11, Pal.Steel); for (int y = 2; y < 11; y += 2) c.HLine(4, 11, y, Pal.Grey); c.Rect(6, 12, 4, 2, Pal.Steel0); break;
                case "sup_seeds": // seed packets
                    c.Rect(2, 1, 7, 10, Pal.Green); c.Rect(7, 2, 7, 10, Pal.Amber); c.Set(4, 6, Pal.White); c.Set(10, 7, Pal.White); c.Set(11, 5, Pal.GreenD); break;
                default:
                    c.Rect(4, 4, 8, 8, Pal.Grey); break;
            }
        }

        public static Texture2D Icon(string itemId)
        {
            var s = Cached("icon_" + itemId, () =>
            {
                var c = new PixelCanvas(16, 16);
                switch (itemId)
                {
                    case "item_patchkit":
                        c.Rect(1, 3, 14, 10, Pal.White); c.Rect(1, 3, 14, 2, Pal.Grey); c.Rect(6, 5, 4, 6, Pal.Red); c.Rect(4, 7, 8, 2, Pal.Red); break;
                    case "item_manual":
                        c.Rect(3, 1, 10, 14, Pal.Amber); c.Rect(3, 1, 2, 14, Pal.AmberD); c.HLine(6, 11, 11, Pal.Ink); c.HLine(6, 11, 8, Pal.Ink); c.HLine(6, 9, 5, Pal.Ink); break;
                    case "item_drillbit":
                        c.Rect(6, 8, 4, 7, Pal.Steel); c.Line(7, 8, 7, 1, Pal.Grey); c.Line(8, 8, 8, 1, Pal.White); c.Line(6, 6, 9, 4, Pal.Grey); c.Line(6, 3, 9, 1, Pal.Grey); break;
                    case "item_cat_fe": Cartridge(c, PixelCanvas.Hex("#8c6f63"), "Fe"); break;
                    case "item_cat_ni": Cartridge(c, Pal.Green, "Ni"); break;
                    case "item_cat_cu": Cartridge(c, PixelCanvas.Hex("#d27d3f"), "Cu"); break;
                    case "item_plate":
                        c.Rect(2, 4, 12, 8, Pal.Steel); c.Rect(2, 10, 12, 2, Pal.SteelL); c.Set(4, 6, Pal.Ink); c.Set(11, 6, Pal.Ink); break;
                    case "item_sealant":
                        c.Rect(5, 1, 6, 11, Pal.Amber); c.Rect(6, 12, 4, 2, Pal.White); c.Rect(7, 14, 2, 2, Pal.White); c.Rect(6, 4, 4, 3, Pal.Ink); break;
                    case "item_hull_patch":
                        c.Rect(2, 4, 12, 8, Pal.Steel); c.Rect(2, 7, 12, 3, Pal.Amber); c.Set(4, 5, Pal.Ink); c.Set(11, 5, Pal.Ink); break;
                    case "item_feedhorn":
                        c.Rect(6, 1, 4, 5, Pal.Steel); c.Line(4, 6, 2, 14, Pal.SteelL); c.Line(11, 6, 13, 14, Pal.SteelL);
                        c.Rect(4, 6, 8, 2, Pal.Steel); c.HLine(2, 13, 14, Pal.Gold); break;
                    case "item_cable":
                        for (int i = 0; i < 12; i++) c.Set(2 + i, 8 + (int)(Mathf.Sin(i * 0.9f) * 3), Pal.Amber);
                        c.Rect(1, 6, 2, 4, Pal.Steel); c.Rect(13, 6, 2, 4, Pal.Steel); break;
                    case "item_antenna_kit":
                        c.Ellipse(8, 9, 6, 3, Pal.SteelL); c.Ellipse(8, 9, 4, 2, Pal.Steel); c.Line(8, 9, 8, 2, Pal.Amber); c.Rect(6, 1, 4, 2, Pal.Steel); break;
                    case "item_heater_coil":
                        for (int y = 3; y < 14; y += 2) c.HLine(4, 11, y, Pal.Red); c.VLine(4, 3, 13, Pal.RedD); c.VLine(11, 3, 13, Pal.RedD); break;
                    case "item_heated_bit":
                        c.Rect(6, 8, 4, 7, Pal.Steel); c.Line(7, 8, 7, 1, Pal.Red); c.Line(8, 8, 8, 1, Pal.Amber); c.Line(6, 5, 9, 3, Pal.Red); break;
                    default:
                        if (itemId.StartsWith("sup_")) DrawSupply(c, itemId);
                        else c.Rect(4, 4, 8, 8, Pal.Grey);
                        break;
                }
                c.Outline(Pal.Ink);
                return c.ToSprite(new Vector2(0.5f, 0.5f));
            });
            return s.texture;
        }

        static void Cartridge(PixelCanvas c, Color32 band, string label)
        {
            c.Rect(5, 1, 6, 13, Pal.Grey); c.Rect(5, 1, 2, 13, Pal.Steel); c.Rect(5, 10, 6, 3, band); c.Rect(6, 14, 4, 1, Pal.Steel);
        }
    }
}
