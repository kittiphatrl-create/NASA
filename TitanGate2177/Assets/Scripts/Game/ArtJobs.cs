// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do clear item icons, a lived-in dinner scene and a rich Earth make the base week read at a glance?
// Date: 2026-11-13
using UnityEngine;

namespace TitanGate.Game
{
    public static partial class Art
    {
        static Color32 Mix(Color32 a, Color32 b, float t) => Color32.Lerp(a, b, Mathf.Clamp01(t));
        static Color32 A(Color32 c, byte a) => new Color32(c.r, c.g, c.b, a);

        /// <summary>Repair parts and carried things, 16x16 icons (y = 0 is the bottom row).</summary>
        public static Sprite JobItem(string id) => Cached("jobitem_" + id, () =>
        {
            var c = new PixelCanvas(16, 16);
            switch (id)
            {
                case "fuse":
                    c.Rect(3, 6, 10, 5, PixelCanvas.Hex("#cfe8ff")); c.Rect(5, 8, 6, 1, Pal.Amber);
                    c.Rect(1, 5, 3, 7, Pal.SteelL); c.Rect(12, 5, 3, 7, Pal.SteelL); c.VLine(2, 5, 11, Pal.Steel);
                    break;
                case "tape":
                    c.Disc(8, 8, 6, Pal.SteelL); c.Disc(8, 8, 3, Pal.Steel0); c.Disc(8, 8, 2, Pal.Clear);
                    c.Rect(10, 2, 6, 3, Pal.SteelL); c.HLine(2, 7, 12, Pal.White);
                    break;
                case "cable":
                    for (int i = 0; i < 12; i++) { float a = i * 0.6f; c.Set(8 + Mathf.RoundToInt(Mathf.Cos(a) * (2 + i * 0.4f)), 8 + Mathf.RoundToInt(Mathf.Sin(a) * (2 + i * 0.4f)), Pal.Red); }
                    c.Line(3, 3, 7, 7, Pal.Red); c.Rect(1, 1, 3, 3, Pal.Gold);
                    for (int i = 0; i < 10; i++) { float a = i * 0.6f + 3; c.Set(8 + Mathf.RoundToInt(Mathf.Cos(a) * (1.5f + i * 0.45f)), 8 + Mathf.RoundToInt(Mathf.Sin(a) * (1.5f + i * 0.45f)), Pal.Blue); }
                    break;
                case "wrench":
                    c.Line(3, 3, 11, 11, Pal.SteelL); c.Line(4, 3, 12, 11, Pal.SteelL); c.Line(3, 4, 11, 12, Pal.Steel);
                    c.Disc(12, 12, 3, Pal.SteelL); c.Rect(12, 12, 3, 3, Pal.Clear); c.Disc(3, 3, 2, Pal.Steel);
                    break;
                case "sack":
                    c.RoundRect(2, 1, 12, 11, PixelCanvas.Hex("#a07a4a")); c.Rect(5, 12, 6, 2, PixelCanvas.Hex("#8a6538")); c.HLine(4, 11, 13, PixelCanvas.Hex("#c7a26b"));
                    c.HLine(3, 12, 6, PixelCanvas.Hex("#8a6538")); c.Speckle(PixelCanvas.Hex("#b78d58"), 0.15f, 3);
                    break;
                case "water_bag":
                    c.RoundRect(2, 1, 12, 11, Pal.Blue); c.Rect(3, 2, 10, 5, PixelCanvas.Hex("#5aa0ff")); c.Rect(6, 12, 4, 3, Pal.SteelL); c.Set(4, 9, Pal.Ice); c.Set(4, 8, Pal.Ice);
                    break;
                case "spare_filter":
                    c.Rect(2, 2, 12, 12, Pal.SteelL); c.Rect(3, 3, 10, 10, Pal.White);
                    for (int i = 4; i < 13; i += 2) c.VLine(i, 3, 12, PixelCanvas.Hex("#c7d2e0"));
                    break;
                case "data_chip":
                    c.Rect(3, 3, 10, 10, Pal.GreenD); c.Rect(5, 5, 6, 6, Pal.Ink); c.Set(6, 6, Pal.Gold);
                    for (int i = 4; i < 13; i += 2) { c.Set(i, 2, Pal.Gold); c.Set(i, 13, Pal.Gold); c.Set(2, i, Pal.Gold); c.Set(13, i, Pal.Gold); }
                    break;
                case "shield_kit":
                    c.Rect(2, 2, 12, 12, Pal.Amber);
                    for (int i = 0; i < 12; i++) if ((i / 2) % 2 == 0) c.Line(2 + i, 2, 2, 2 + i, Pal.Ink);
                    c.Frame(2, 2, 12, 12, Pal.AmberD);
                    break;
                case "med_kit":
                    c.RoundRect(1, 2, 14, 11, Pal.White); c.Rect(6, 12, 4, 2, Pal.Grey); c.Rect(7, 4, 2, 7, Pal.Red); c.Rect(4, 6, 8, 3, Pal.Red);
                    break;
                default: c.Disc(8, 8, 5, Pal.Grey); break;
            }
            c.Outline(Pal.Ink);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        /// <summary>Radiation trefoil, 16x16.</summary>
        public static Sprite RadIcon() => Cached("radicon", () =>
        {
            var c = new PixelCanvas(16, 16);
            c.Disc(8, 8, 7, Pal.Amber);
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
            {
                float dx = x - 7.5f, dy = y - 7.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 90;
                bool blade = r > 2.2f && r < 6.2f && Mathf.Repeat(ang, 120) < 60;
                if (blade || r < 1.6f) c.Set(x, y, Pal.Ink);
            }
            c.Outline(Pal.Ink);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        static float Noise(int x, int y, int seed)
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177; h ^= h >> 16;
            return (h & 0xffff) / 65535f;
        }

        static float Smooth(float x, float y, float scale, int seed)
        {
            float fx = x / scale, fy = y / scale;
            int ix = Mathf.FloorToInt(fx), iy = Mathf.FloorToInt(fy);
            float tx = fx - ix, ty = fy - iy;
            tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
            float a = Mathf.Lerp(Noise(ix, iy, seed), Noise(ix + 1, iy, seed), tx);
            float b = Mathf.Lerp(Noise(ix, iy + 1, seed), Noise(ix + 1, iy + 1, seed), tx);
            return Mathf.Lerp(a, b, ty);
        }

        static float Fbm(float x, float y, int seed) => Smooth(x, y, 22, seed) * 0.55f + Smooth(x, y, 11, seed + 1) * 0.3f + Smooth(x, y, 5, seed + 2) * 0.15f;

        /// <summary>
        /// Earth in 2177 for the opening: oceans and continents on a lit sphere, swirling clouds, a thin blue
        /// atmosphere at the edge, and city lights on the night side. 128x128, 24 pixels per unit.
        /// </summary>
        public static Sprite EarthHD() => Cached("earth-hd2", () =>
        {
            const int S = 128; const float R = 54;
            var c = new PixelCanvas(S, S);
            var deep = PixelCanvas.Hex("#123a7a"); var sea = PixelCanvas.Hex("#2a6fd0"); var shallow = PixelCanvas.Hex("#4ba3e6");
            var green = PixelCanvas.Hex("#3f8f4a"); var forest = PixelCanvas.Hex("#2c6b3a"); var desert = PixelCanvas.Hex("#c9a76a"); var ice = PixelCanvas.Hex("#eef6ff");
            var cloud = PixelCanvas.Hex("#f4f8ff"); var night = PixelCanvas.Hex("#050a1c"); var city = PixelCanvas.Hex("#ffd77a");
            Vector3 sun = new Vector3(0.75f, 0.35f, 0.56f).normalized;
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = (x - S / 2f + 0.5f) / R, dy = (y - S / 2f + 0.5f) / R;
                float d2 = dx * dx + dy * dy;
                if (d2 > 1f)
                {
                    float rim = Mathf.Sqrt(d2);
                    if (rim < 1.09f)
                    {
                        var n0 = new Vector3(dx / rim, dy / rim, 0);
                        float lit = Mathf.Clamp01(Vector3.Dot(n0, sun) + 0.35f);
                        byte a = (byte)(Mathf.Clamp01(1 - (rim - 1) / 0.09f) * 170 * lit);
                        if (a > 8) c.Set(x, y, new Color32(120, 190, 255, a));
                    }
                    continue;
                }
                float dz = Mathf.Sqrt(1 - d2);
                var n = new Vector3(dx, dy, dz);
                // spherical coordinates so the map wraps round the ball
                float lon = Mathf.Atan2(dx, dz) * 40 + 70, lat = Mathf.Asin(dy) * 40 + 64;
                float land = Fbm(lon, lat, 11);
                float cl = Fbm(lon * 1.3f + 30, lat * 2.2f, 77);
                float polar = Mathf.Abs(dy);
                Color32 col;
                if (polar > 0.9f) col = ice;
                else if (land > 0.56f) col = land > 0.66f ? (polar < 0.35f ? desert : forest) : green;
                else col = land > 0.5f ? shallow : land > 0.38f ? sea : deep;
                float light = Vector3.Dot(n, sun);
                float shade = Mathf.Clamp01(light * 0.85f + 0.25f);
                Color32 lit2 = Mix(night, col, shade);
                if (cl > 0.6f) lit2 = Mix(lit2, Mix(night, cloud, Mathf.Clamp01(light + 0.3f)), Mathf.Clamp01((cl - 0.6f) * 4));
                if (light < -0.05f && land > 0.58f && Noise(x, y, 5) > 0.86f && cl < 0.62f) lit2 = city;
                // ocean glint
                var h = (sun + Vector3.forward).normalized;
                float spec = Mathf.Pow(Mathf.Max(0, Vector3.Dot(n, h)), 40);
                if (land <= 0.56f && cl < 0.6f) lit2 = Mix(lit2, Pal.White, spec * 0.8f);
                // dithered terminator: a pixel-art soft edge
                if (Mathf.Abs(light + 0.02f) < 0.05f && ((x + y) & 1) == 0) lit2 = Mix(lit2, night, 0.5f);
                c.Set(x, y, lit2);
            }
            return c.ToSprite(new Vector2(0.5f, 0.5f), 24f);
        });

        /// <summary>Orbital station over Earth: truss, two pairs of solar wings, white modules, a docking ring. 112x44.</summary>
        public static Sprite OrbitStation(int blink) => Cached("orbit-station" + blink, () =>
        {
            var c = new PixelCanvas(112, 44);
            var panel = PixelCanvas.Hex("#2a4f9c"); var panelL = PixelCanvas.Hex("#4c78d6"); var frame = PixelCanvas.Hex("#c9a648");
            void Wing(int x, int y)
            {
                c.Rect(x, y, 26, 12, panel);
                for (int i = 0; i < 26; i += 4) c.VLine(x + i, y, y + 11, panelL);
                c.HLine(x, x + 25, y + 6, panelL);
                c.Frame(x, y, 26, 12, frame);
            }
            c.Rect(4, 21, 104, 3, Pal.SteelL); for (int i = 6; i < 106; i += 5) c.Set(i, 22, Pal.Steel);   // main truss
            Wing(4, 26); Wing(4, 7); Wing(82, 26); Wing(82, 7);
            c.VLine(17, 18, 26, Pal.Steel); c.VLine(95, 18, 26, Pal.Steel);
            // modules
            c.RoundRect(34, 17, 44, 11, Pal.White); c.HLine(35, 76, 26, PixelCanvas.Hex("#d8dee8")); c.HLine(35, 76, 18, PixelCanvas.Hex("#aeb7c6"));
            for (int i = 38; i < 76; i += 9) c.VLine(i, 18, 27, PixelCanvas.Hex("#c4ccd8"));
            c.RoundRect(50, 28, 12, 10, Pal.White); c.Rect(52, 34, 8, 2, Pal.CyanD);
            c.RoundRect(50, 7, 12, 10, PixelCanvas.Hex("#e6e2d6")); c.Rect(53, 9, 6, 2, Pal.Amber);
            c.Disc(80, 22, 4, Pal.SteelL); c.Disc(80, 22, 2, Pal.Steel0);
            c.Rect(40, 21, 4, 3, Pal.CyanL); c.Rect(66, 21, 4, 3, Pal.CyanL);
            c.Set(30, 23, blink == 0 ? Pal.Red : Pal.RedD); c.Set(84, 23, blink == 0 ? Pal.Green : Pal.GreenD);
            c.Outline(new Color32(10, 14, 30, 255));
            return c.ToSprite(new Vector2(0.5f, 0.5f), 24f);
        });

        /// <summary>
        /// Evening inside the landed lifeboat pod, seen from the side like a doll's house: curved hull ribs, a
        /// porthole on the Mars night, shelves, a warm hanging lamp and a bench. 320x180. Characters stand on top,
        /// then <see cref="PodDiningTable"/> covers their legs so they look seated.
        /// </summary>
        public static Sprite PodDiningBack() => Cached("pod-dining-back2", () =>
        {
            var c = new PixelCanvas(320, 180);
            var wall = PixelCanvas.Hex("#1c2440"); var wallL = PixelCanvas.Hex("#27325a"); var rib = PixelCanvas.Hex("#121831"); var floor = PixelCanvas.Hex("#2b2230");
            c.Fill(wall);
            for (int y = 0; y < 180; y++) for (int x = 0; x < 320; x++)
            {
                // warm lamp pool in the middle, darker at the edges
                float dx = (x - 160) / 170f, dy = (y - 120) / 120f;
                float glow = Mathf.Clamp01(1 - (dx * dx + dy * dy));
                var col = Mix(wall, PixelCanvas.Hex("#5a4a52"), glow * 0.55f);
                if ((x / 4 + y / 4) % 2 == 0 && glow < 0.2f) col = Mix(col, rib, 0.25f);
                c.Set(x, y, col);
            }
            for (int i = 0; i < 9; i++) { int x = 12 + i * 37; c.Rect(x, 30, 4, 150, wallL); c.VLine(x + 4, 30, 179, rib); }
            // curved ceiling and floor
            for (int x = 0; x < 320; x++)
            {
                float k = (x - 160) / 160f;
                int top = 180 - Mathf.RoundToInt(18 + k * k * 22);
                for (int y = top; y < 180; y++) c.Set(x, y, PixelCanvas.Hex("#0d1226"));
                int bot = Mathf.RoundToInt(14 + k * k * 14);
                for (int y = 0; y < bot; y++) c.Set(x, y, floor);
            }
            // porthole: Mars night, stars, a small blue Earth
            int px = 252, py = 112;
            c.Disc(px, py, 26, Pal.SteelL); c.Disc(px, py, 22, PixelCanvas.Hex("#0b0f22"));
            for (int y = py - 22; y <= py + 22; y++) for (int x = px - 22; x <= px + 22; x++)
                if ((x - px) * (x - px) + (y - py) * (y - py) <= 22 * 22)
                {
                    if (y < py - 6 && Noise(x, y, 3) > 0.93f) c.Set(x, y, Pal.White);
                    if (y < py - 10 + Mathf.RoundToInt(Mathf.Sin(x * 0.2f) * 3)) continue;
                    if (y < py - 4) c.Set(x, y, PixelCanvas.Hex("#3a1c1c"));
                }
            c.Disc(px + 9, py + 12, 2, Pal.Blue); c.Set(px + 9, py + 13, Pal.White);
            for (int a = 0; a < 8; a++) { float t = a * Mathf.PI / 4; c.Set(px + Mathf.RoundToInt(Mathf.Cos(t) * 24), py + Mathf.RoundToInt(Mathf.Sin(t) * 24), Pal.Steel0); }
            // shelves with supplies on the left wall
            for (int s = 0; s < 2; s++)
            {
                int y = 104 + s * 30;
                c.Rect(26, y, 74, 3, Pal.Steel);
                for (int i = 0; i < 6; i++)
                {
                    int x = 30 + i * 12;
                    if (s == 0) { c.Rect(x, y + 3, 7, 10, Pal.Amber); c.Rect(x, y + 6, 7, 3, Pal.Red); }
                    else { c.Rect(x + 1, y + 3, 6, 13, Pal.Blue); c.Rect(x + 2, y + 16, 4, 2, Pal.SteelL); }
                }
            }
            // console with screens on the right of the porthole wall
            c.Rect(196, 70, 30, 26, Pal.Steel0); c.Rect(199, 80, 24, 12, PixelCanvas.Hex("#103a33")); c.HLine(201, 220, 86, Pal.Green); c.Set(204, 75, Pal.Amber); c.Set(210, 75, Pal.Red);
            // hanging lamp and its cone
            c.VLine(160, 150, 168, Pal.Steel); c.Rect(150, 144, 21, 6, Pal.Steel0); c.Rect(152, 142, 17, 2, PixelCanvas.Hex("#ffe2a8"));
            for (int y = 40; y < 142; y++)
            {
                int half = Mathf.RoundToInt((142 - y) * 0.75f) + 8;
                for (int x = 160 - half; x <= 160 + half; x++) { var o = c.Get(x, y); if (o.a > 0) c.Set(x, y, Mix(o, PixelCanvas.Hex("#ffd9a0"), 0.06f)); }
            }
            // bench behind the table
            c.Rect(40, 44, 240, 8, PixelCanvas.Hex("#4b3a55")); c.Rect(40, 52, 240, 22, PixelCanvas.Hex("#3a2c46"));
            for (int i = 0; i < 6; i++) c.VLine(60 + i * 40, 52, 73, PixelCanvas.Hex("#2e2338"));
            return c.ToSprite(new Vector2(0.5f, 0.5f), 16f);
        });

        /// <summary>The table in front of the bench: plates and cups on it, legs below. 320x60.</summary>
        public static Sprite PodDiningTable() => Cached("pod-dining-table3", () =>
        {
            var c = new PixelCanvas(320, 60);
            c.Rect(44, 0, 8, 34, Pal.Steel0); c.Rect(268, 0, 8, 34, Pal.Steel0);
            c.Rect(26, 8, 268, 26, PixelCanvas.Hex("#3d4456")); c.HLine(26, 293, 8, PixelCanvas.Hex("#2b3040"));   // front panel hides the legs
            for (int i = 0; i < 268; i += 34) c.VLine(26 + i, 9, 33, PixelCanvas.Hex("#353b4c"));
            c.Rect(20, 34, 280, 18, PixelCanvas.Hex("#6b7488")); c.Rect(20, 50, 280, 4, PixelCanvas.Hex("#8c96ab")); c.Rect(20, 34, 280, 3, PixelCanvas.Hex("#4b5366"));
            for (int i = 0; i < 280; i += 7) c.Set(20 + i, 44, PixelCanvas.Hex("#7d869b"));
            c.Rect(150, 52, 20, 6, Pal.Amber); c.Rect(152, 56, 16, 3, Pal.OrangeL);   // the shared pot in the middle
            return c.ToSprite(new Vector2(0.5f, 0f), 16f);
        });
    }
}
