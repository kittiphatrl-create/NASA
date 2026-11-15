// PROTOTYPE - NOT FOR PRODUCTION
// Question: Is the survival week more fun when the cadet walks around a top-down base and does each job?
// Date: 2026-11-12
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>Top-down three-quarter sprites for the walkable Mars base: floor tiles, walls, plots, markers, light.</summary>
    public static partial class Art
    {
        // one tile = 16 art pixels = 1 world unit

        /// <summary>Floor tile. kind: 0 = base metal, 1 = greenhouse, 2 = Mars ground outside.</summary>
        public static Sprite BaseFloor(int kind, int variant) => UseStationTiles && StationFloor(kind, variant) is Sprite st ? st : HabitatFloor(kind, variant);
        static Sprite LegacyBaseFloor(int kind, int variant) => Cached($"bfloor{kind}_{variant}", () =>
        {
            var c = new PixelCanvas(16, 16);
            if (kind == 2)
            {
                c.Fill(Pal.Rust);
                c.Speckle(Pal.RustL, 0.025f, 11 + variant * 7, false);
                c.Speckle(Pal.RustD, 0.035f, 23 + variant * 5, false);
                if (variant % 3 == 0) { c.Disc(5, 6, 1, Pal.RustD); c.Set(4, 7, Pal.RustL); }
                if (variant % 4 == 1) { c.HLine(9, 13, 11, Pal.RustD); }
            }
            else if (kind == 3)   // corridor: darker panels with a light strip down the middle
            {
                c.Fill(Pal.Navy1);
                c.Frame(0, 0, 16, 16, Pal.Navy0);
                c.Rect(0, 7, 16, 2, Pal.Navy2);
                if (variant % 2 == 0) c.Rect(6, 7, 4, 2, Pal.SteelL);
            }
            else if (kind == 4)   // carpet in crew rooms
            {
                c.Fill(PixelCanvas.Hex("#5a4a6e"));
                c.Speckle(PixelCanvas.Hex("#4c3e5e"), 0.18f, 3 + variant, false);
                c.Speckle(PixelCanvas.Hex("#6a5a80"), 0.08f, 9 + variant, false);
            }
            else if (kind == 5)   // engine grating
            {
                c.Fill(PixelCanvas.Hex("#2a2f40"));
                for (int i = 1; i < 16; i += 3) { c.HLine(0, 15, i, PixelCanvas.Hex("#3d4459")); c.VLine(i, 0, 15, PixelCanvas.Hex("#353b4f")); }
                if (variant % 4 == 0) c.Set(7, 7, Pal.Amber);
            }
            else if (kind == 6)   // lifeboat pod floor
            {
                c.Fill(PixelCanvas.Hex("#6e6656"));
                c.Frame(0, 0, 16, 16, PixelCanvas.Hex("#57503f"));
                c.HLine(2, 13, 5, PixelCanvas.Hex("#7d7563")); c.HLine(2, 13, 10, PixelCanvas.Hex("#7d7563"));
            }
            else if (kind == 7)   // galley checker
            {
                var a1 = PixelCanvas.Hex("#c9785e"); var a2 = PixelCanvas.Hex("#e8c9a6");
                for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) c.Set(x, y, ((x / 8) + (y / 8)) % 2 == 0 ? a1 : a2);
            }
            else if (kind == 8)   // bay: yellow hazard stripes at the edges
            {
                c.Fill(Pal.Steel0);
                c.Frame(0, 0, 16, 16, Pal.Navy2);
                if (variant % 3 == 0) for (int x = 0; x < 16; x++) if ((x / 2) % 2 == 0) c.VLine(x, 6, 9, Pal.AmberD);
            }
            else if (kind == 1)
            {
                c.Fill(PixelCanvas.Hex("#3c4a44"));
                c.Frame(0, 0, 16, 16, PixelCanvas.Hex("#2c3833"));
                c.HLine(1, 14, 8, PixelCanvas.Hex("#33403a"));
                c.Set(2, 2, Pal.SteelL); c.Set(13, 13, PixelCanvas.Hex("#556a60"));
            }
            else
            {
                c.Fill(Pal.Steel0);
                c.Frame(0, 0, 16, 16, Pal.Navy2);
                c.HLine(1, 14, 14, PixelCanvas.Hex("#566b7b"));
                c.Set(2, 2, Pal.Steel); c.Set(13, 2, Pal.Steel); c.Set(2, 13, Pal.Steel); c.Set(13, 13, Pal.Steel);
                if (variant % 5 == 2) { c.HLine(4, 11, 7, Pal.Navy2); c.HLine(4, 11, 9, Pal.Navy2); }
            }
            return c.ToSprite(new Vector2(0, 0));
        });

        /// <summary>
        /// Wall tile. face = the tile below is open, so the wall shows its front: 16 px of face under an 8 px cap
        /// (sprite 16x24, pivot bottom-left). Otherwise only the cap shows (16x16).
        /// </summary>
        public static Sprite BaseWall(bool face, bool outerHull, int variant = 0, int deckSides = 12) => UseStationTiles && StationWall(face, outerHull, variant, deckSides) is Sprite st ? st : HabitatWall(face, outerHull, variant);
        static Sprite LegacyBaseWall(bool face, bool outerHull) => Cached($"bwall{face}{outerHull}", () =>
        {
            var cap = outerHull ? PixelCanvas.Hex("#e0d5bf") : Pal.Navy3;
            var capD = outerHull ? PixelCanvas.Hex("#7d969c") : Pal.Navy2;
            if (!face)
            {
                var c = new PixelCanvas(16, 16);
                c.Fill(cap);
                c.Frame(0, 0, 16, 16, capD);
                return c.ToSprite(new Vector2(0, 0));
            }
            var w = new PixelCanvas(16, 32);
            // face: 24 px tall (1.5 tiles), a skirting at the bottom, a light seam and a stripe
            var faceC = outerHull ? PixelCanvas.Hex("#a4b5b2") : Pal.Navy1;
            var faceD = outerHull ? PixelCanvas.Hex("#6f7891") : Pal.Navy0;
            w.Rect(0, 0, 16, 24, faceC);
            w.HLine(0, 15, 0, Pal.Ink);
            w.Rect(0, 1, 16, 2, faceD);
            w.Rect(0, 9, 16, 2, outerHull ? Pal.AmberD : Pal.Navy2);
            w.Rect(0, 18, 16, 1, outerHull ? PixelCanvas.Hex("#b5bccc") : Pal.Navy2);
            w.Rect(0, 20, 16, 4, outerHull ? PixelCanvas.Hex("#a8b0c2") : PixelCanvas.Hex("#202a50"));
            w.VLine(0, 3, 23, outerHull ? PixelCanvas.Hex("#7d869d") : Pal.Navy0);
            // cap (top of the wall, its thickness)
            w.Rect(0, 24, 16, 8, cap);
            w.HLine(0, 15, 24, capD);
            w.HLine(0, 15, 31, capD);
            return w.ToSprite(new Vector2(0, 0));
        });

        /// <summary>Airlock door seen from inside the base, with a round peephole. 32x24, pivot bottom-left.</summary>
        public static Sprite PeepDoor(bool knocking) => Cached("peepdoor" + knocking, () =>
        {
            var c = new PixelCanvas(32, 24);
            c.Rect(0, 0, 32, 24, Pal.Navy3);
            c.Rect(2, 0, 28, 20, Pal.Steel);
            c.Rect(3, 1, 26, 18, Pal.Steel0);
            c.VLine(16, 1, 18, Pal.Navy1);
            c.Rect(3, 9, 26, 2, Pal.Amber);
            c.Disc(16, 13, 4, Pal.Ink);
            c.Disc(16, 13, 3, knocking ? Pal.Amber : Pal.SkyMarsD);
            c.Set(15, 14, Pal.White);
            c.Rect(5, 3, 3, 3, Pal.Green);
            c.Outline(Pal.Ink);
            return c.ToSprite(new Vector2(0, 0));
        });

        /// <summary>Greenhouse plot. stage 0 empty, 1 seed, 2 sprout, 3 ripe. 32x14, pivot bottom-centre.</summary>
        public static Sprite Plot(int stage, bool dry) => Prop($"plot{stage}{dry}", 32, 16, c =>
        {
            c.Rect(0, 0, 32, 10, Pal.Steel0);
            c.Rect(1, 1, 30, 8, dry ? PixelCanvas.Hex("#7a5236") : PixelCanvas.Hex("#4a3222"));
            c.Speckle(PixelCanvas.Hex("#5e3f2b"), 0.2f, 5);
            for (int i = 0; i < 3; i++)
            {
                int x = 6 + i * 10;
                if (stage == 1) c.Rect(x - 1, 5, 2, 1, PixelCanvas.Hex("#c8a36a"));
                if (stage == 2) { c.VLine(x, 5, 9, Pal.GreenD); c.Set(x - 1, 9, Pal.Green); c.Set(x + 1, 8, Pal.Green); }
                if (stage == 3)
                {
                    c.VLine(x, 5, 12, Pal.GreenD);
                    c.Disc(x, 12, 3, Pal.Green);
                    c.Disc(x - 2, 10, 2, Pal.GreenD);
                    c.Set(x + 1, 13, PixelCanvas.Hex("#c7f59c"));
                    c.Disc(x + 2, 6, 1, PixelCanvas.Hex("#d9b36b"));
                }
            }
        });

        /// <summary>Shelf on a wall, showing one supply as items you can count. kind: o2, water, food.</summary>
        public static Sprite SupplyShelf(string kind, int count) => Prop($"shelf_{kind}{count}", 34, 26, c =>
        {
            c.Rect(0, 0, 2, 26, Pal.Steel0); c.Rect(32, 0, 2, 26, Pal.Steel0);
            c.Rect(0, 0, 34, 2, Pal.Steel); c.Rect(0, 12, 34, 2, Pal.Steel); c.Rect(0, 24, 34, 2, Pal.Steel);
            for (int i = 0; i < Mathf.Min(count, 10); i++)
            {
                int row = i / 5, col = i % 5;
                int x = 3 + col * 6, y = 2 + row * 12;
                if (kind == "water") { c.Rect(x, y, 5, 8, Pal.Blue); c.Rect(x + 1, y + 8, 3, 2, Pal.BlueD); c.Set(x + 1, y + 5, Pal.Ice); }
                else if (kind == "food") { c.Rect(x, y, 5, 6, Pal.Amber); c.HLine(x, x + 4, y + 3, Pal.Red); c.HLine(x, x + 4, y + 6, Pal.Grey); }
                else { c.Rect(x + 1, y, 3, 9, Pal.Green); c.Rect(x + 2, y + 9, 1, 1, Pal.Grey); c.Set(x + 1, y + 6, Pal.White); }
            }
        });

        /// <summary>Battery bank with one bar per unit of power.</summary>
        public static Sprite BatteryBank(int bars) => Prop("battery" + bars, 30, 26, c =>
        {
            c.Rect(0, 0, 30, 26, Pal.Steel0);
            c.Rect(2, 2, 26, 22, Pal.Navy0);
            for (int i = 0; i < 6; i++)
                c.Rect(4 + (i % 3) * 8, 4 + (i / 3) * 10, 6, 8, i < bars ? Pal.Green : Pal.Navy2);
            c.Rect(12, 24, 6, 2, Pal.Amber);
        });

        /// <summary>Stack of water bags in the storm shelter (water is a good radiation shield).</summary>
        public static Sprite WaterWall(bool built) => Prop("waterwall" + built, 30, 18, c =>
        {
            if (!built) { c.Rect(0, 0, 30, 3, Pal.Steel0); c.Rect(4, 3, 8, 4, Pal.BlueD); return; }
            for (int r = 0; r < 3; r++)
                for (int i = 0; i < 4 - (r == 2 ? 1 : 0); i++)
                {
                    int x = i * 7 + (r % 2) * 3, y = r * 6;
                    c.RoundRect(x, y, 7, 6, Pal.Blue);
                    c.Set(x + 2, y + 4, Pal.Ice);
                }
        });

        /// <summary>A sign hanging on a wall, with a short icon-like label. 18x10.</summary>
        public static Sprite WallSign(Color32 col) => Prop("sign" + col.r + col.g + col.b, 18, 10, c =>
        {
            c.Rect(0, 0, 18, 10, Pal.Navy0);
            c.Rect(1, 1, 16, 8, col);
            c.HLine(3, 14, 5, Pal.Ink);
        });

        /// <summary>Bouncing "!" marker over the place where the next event waits.</summary>
        public static Sprite Marker(bool urgent) => Cached("marker" + urgent, () =>
        {
            var c = new PixelCanvas(9, 15);
            var col = urgent ? Pal.Red : Pal.Amber;
            c.RoundRect(0, 4, 9, 11, col);
            c.Rect(3, 7, 3, 6, Pal.Ink);
            c.Rect(3, 5, 3, 1, Pal.Ink);
            c.Set(4, 3, col); c.Set(3, 2, col); c.Set(5, 2, col); c.Set(4, 1, col); c.Set(4, 0, col);
            c.Outline(Pal.Ink);
            return c.ToSprite(new Vector2(0.5f, 0f));
        });

        /// <summary>Small arrow that circles the cadet and points at the next event. Points right.</summary>
        public static Sprite PointerArrow() => Cached("ptr", () =>
        {
            var c = new PixelCanvas(10, 9);
            for (int i = 0; i < 5; i++) c.VLine(i + 4, i, 8 - i, Pal.Amber);
            c.Rect(0, 3, 5, 3, Pal.Amber);
            c.Outline(Pal.Ink);
            return c.ToSprite(new Vector2(0.3f, 0.5f));
        });

        /// <summary>Soft round light. Tinted by the renderer colour.</summary>
        public static Sprite SoftLight() => Cached("softlight", () =>
        {
            const int n = 48;
            var c = new PixelCanvas(n, n);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2 - 1, dy = (y + 0.5f) / n * 2 - 1;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1 - d);
                    a = a * a;
                    c.Set(x, y, new Color32(255, 255, 255, (byte)(a * 255)));
                }
            var t = c.ToTexture();
            t.filterMode = FilterMode.Bilinear;
            return Sprite.Create(t, new UnityEngine.Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        });

        /// <summary>Little heart for petting the pet.</summary>
        public static Sprite Heart() => Cached("heart", () =>
        {
            var c = new PixelCanvas(7, 6);
            c.Rect(0, 3, 3, 2, Pal.Red); c.Rect(4, 3, 3, 2, Pal.Red); c.Rect(1, 1, 5, 2, Pal.Red); c.Set(3, 0, Pal.Red); c.Set(3, 3, Pal.Red);
            c.Set(1, 4, Pal.White);
            return c.ToSprite(new Vector2(0.5f, 0f));
        });

        /// <summary>Soft shadow under characters.</summary>
        public static Sprite Shadow() => Cached("shadow", () =>
        {
            var c = new PixelCanvas(14, 5);
            c.Ellipse(7, 2, 6, 2, new Color32(0, 0, 0, 90));
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });
        /// <summary>Detail on a wall face (16x16, pivot bottom-left): 0 vent, 1 pipes, 2 warning stripe, 3 light panel.</summary>
        public static Sprite WallDetail(int kind, bool hull) => UseStationTiles && StationWall(true, hull, 0) != null ? null : WallDetailDrawn(kind, hull);

        static Sprite WallDetailDrawn(int kind, bool hull) => Cached($"wdet{kind}{hull}", () =>
        {
            var c = new PixelCanvas(16, 16);
            var dark = hull ? PixelCanvas.Hex("#6f7891") : Pal.Navy0;
            switch (kind)
            {
                case 0: c.Rect(4, 4, 8, 7, dark); for (int y = 5; y < 11; y += 2) c.HLine(5, 10, y, hull ? Pal.SteelL : Pal.Navy2); break;
                case 1: c.Rect(0, 9, 16, 2, Pal.Steel); c.Rect(0, 3, 16, 2, Pal.Steel0); c.Rect(6, 2, 2, 10, Pal.SteelL); c.Set(6, 9, Pal.Amber); break;
                case 2: for (int x = 0; x < 16; x++) if ((x / 2) % 2 == 0) c.VLine(x, 2, 4, Pal.Amber); else c.VLine(x, 2, 4, Pal.Ink); break;
                default: c.Rect(5, 8, 6, 5, Pal.Navy1); c.Rect(6, 9, 4, 3, Pal.CyanL); break;
            }
            return c.ToSprite(new Vector2(0, 0));
        });

        public static Sprite Rock(int v) => Prop("rock" + v, 10 + v * 4, 6 + v * 2, c =>
        {
            int w = c.W, h = c.H;
            c.Ellipse(w / 2, h / 2, w / 2 - 1, h / 2 - 1, Pal.RustD);
            c.Ellipse(w / 2 - 1, h / 2 + 1, w / 2 - 3, h / 2 - 2, Pal.Rust);
            c.Set(w / 2 - 2, h - 3, Pal.RustL);
        });

        /// <summary>Tileable starfield for the space around the ship.</summary>
        public static Sprite SpaceTile() => Cached("spacetile", () =>
        {
            var c = new PixelCanvas(48, 48);
            c.Fill(PixelCanvas.Hex("#05060f"));
            var rng = new System.Random(4);
            for (int i = 0; i < 26; i++)
            {
                int x = rng.Next(48), y = rng.Next(48);
                c.Set(x, y, i % 7 == 0 ? Pal.CyanL : i % 5 == 0 ? Pal.Amber : Pal.Grey);
                if (i % 9 == 0) { c.Set(x + 1, y, Pal.Steel); c.Set(x - 1, y, Pal.Steel); c.Set(x, y + 1, Pal.Steel); c.Set(x, y - 1, Pal.Steel); }
            }
            var t = c.ToTexture(TextureWrapMode.Repeat);
            return Sprite.Create(t, new UnityEngine.Rect(0, 0, 48, 48), new Vector2(0, 0), PixelCanvas.PPU, 0, SpriteMeshType.FullRect);
        });

        /// <summary>Dark corners over the whole view (soft, bilinear).</summary>
        public static Sprite Vignette() => Cached("vignette", () =>
        {
            const int w = 64, h = 36;
            var c = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f) / w * 2 - 1, dy = (y + 0.5f) / h * 2 - 1;
                    float d = Mathf.Sqrt(dx * dx * 0.9f + dy * dy * 0.9f);
                    float a = Mathf.Clamp01((d - 0.72f) / 0.6f);
                    c.Set(x, y, new Color32(4, 4, 12, (byte)(a * a * 200)));
                }
            var t = c.ToTexture();
            t.filterMode = FilterMode.Bilinear;
            return Sprite.Create(t, new UnityEngine.Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        });

        /// <summary>Window in the ship's hull wall with stars and the planet below. 48x16, pivot bottom-left.</summary>
        public static Sprite ShipWindow() => Cached("shipwindow", () =>
        {
            var c = new PixelCanvas(48, 16);
            c.Rect(0, 0, 48, 16, PixelCanvas.Hex("#a4b5b2"));
            c.RoundRect(2, 2, 44, 12, Pal.Ink);
            c.RoundRect(3, 3, 42, 10, PixelCanvas.Hex("#070a1f"));
            c.Disc(36, 0, 9, Pal.Rust); c.Disc(35, 1, 8, Pal.RustL); c.Disc(38, -1, 6, Pal.Rust);
            int[] sx = { 6, 11, 17, 22, 28, 9, 25 }, sy = { 10, 6, 11, 8, 11, 4, 5 };
            for (int i = 0; i < sx.Length; i++) c.Set(sx[i], sy[i], i % 3 == 0 ? Pal.CyanL : Pal.White);
            c.VLine(16, 3, 12, Pal.Steel0); c.VLine(32, 3, 12, Pal.Steel0);
            return c.ToSprite(new Vector2(0, 0));
        });

        /// <summary>Lifeboat hatch in the bay wall. 48x24, pivot bottom-left.</summary>
        public static Sprite LifeboatHatch(bool open, int fill, int slots) => Cached($"lbhatch{open}{fill}_{slots}", () =>
        {
            var c = new PixelCanvas(48, 24);
            c.Rect(0, 0, 48, 24, PixelCanvas.Hex("#a4b5b2"));
            c.RoundRect(6, 0, 36, 22, Pal.Ink);
            c.RoundRect(8, 0, 32, 20, open ? PixelCanvas.Hex("#1b2140") : Pal.Orange);
            if (!open) { c.Rect(8, 9, 32, 2, Pal.OrangeD); c.Disc(24, 15, 3, Pal.Ink); c.Disc(24, 15, 2, Pal.CyanD); }
            else
                for (int i = 0; i < slots; i++)
                    c.Rect(10 + i * (28 / Mathf.Max(1, slots)), 3, Mathf.Max(2, 28 / Mathf.Max(1, slots) - 2), 4, i < fill ? Pal.Green : Pal.Navy2);
            for (int x = 0; x < 48; x++) if ((x / 3) % 2 == 0) c.Set(x, 23, Pal.Amber);
            c.Outline(Pal.Ink);
            return c.ToSprite(new Vector2(0, 0));
        });

        public static Sprite PetBowl(bool full) => Prop("bowl" + full, 14, 7, c =>
        {
            c.Ellipse(7, 3, 6, 3, Pal.Red);
            c.Ellipse(7, 4, 5, 2, full ? Pal.Amber : Pal.RedD);
            if (full) { c.Set(5, 5, Pal.OrangeD); c.Set(8, 5, Pal.OrangeD); }
        });

        public static Sprite Sofa() => Prop("sofa", 44, 20, c =>
        {
            c.Rect(0, 0, 44, 6, Pal.Navy2);
            c.RoundRect(0, 4, 44, 16, Pal.Blue);
            c.Rect(3, 4, 38, 7, Pal.BlueD);
            c.RoundRect(0, 4, 5, 12, Pal.BlueD); c.RoundRect(39, 4, 5, 12, Pal.BlueD);
            c.HLine(5, 38, 11, Pal.Blue);
            c.VLine(22, 5, 10, Pal.Blue);
        });

        /// <summary>Fire blocking a doorway after the impact.</summary>
        public static Sprite Fire(int frame) => Cached("fire" + frame, () =>
        {
            var c = new PixelCanvas(16, 20);
            int o = frame % 2;
            c.Ellipse(8, 5, 7, 5, Pal.RedD);
            c.Ellipse(8, 7 + o, 5, 6, Pal.Red);
            c.Ellipse(7 + o, 9, 3, 6, Pal.Orange);
            c.Ellipse(8, 9 + o, 2, 4, Pal.Amber);
            c.Set(5 - o, 16, Pal.Orange); c.Set(11 + o, 15, Pal.Amber); c.Set(8, 18 - o, Pal.Red);
            return c.ToSprite(new Vector2(0.5f, 0f));
        });
        /// <summary>Round porthole in a wall face showing the outside (Mars sky or space). 24x16, pivot bottom-left.</summary>
        public static Sprite Porthole(bool mars) => Cached("porthole" + mars, () =>
        {
            var c = new PixelCanvas(24, 16);
            c.Disc(12, 8, 7, Pal.Ink);
            c.Disc(12, 8, 6, PixelCanvas.Hex("#a4b5b2"));
            c.Disc(12, 8, 5, mars ? Pal.SkyMars : PixelCanvas.Hex("#070a1f"));
            if (mars) { c.Rect(7, 3, 11, 4, Pal.Rust); c.Set(9, 7, Pal.RustD); c.Set(14, 6, Pal.RustD); c.Disc(15, 11, 1, PixelCanvas.Hex("#f3e1c4")); }
            else { c.Set(10, 10, Pal.White); c.Set(14, 6, Pal.CyanL); }
            c.Set(10, 11, Pal.White);
            return c.ToSprite(new Vector2(0, 0));
        });

        public static Sprite Table() => HabitatTable();
        static Sprite LegacyTable() => Prop("table", 40, 20, c =>
        {
            c.Rect(4, 0, 3, 8, Pal.Steel0); c.Rect(33, 0, 3, 8, Pal.Steel0);
            c.RoundRect(0, 8, 40, 12, Pal.Steel);
            c.Rect(2, 10, 36, 8, Pal.SteelL);
            c.Rect(8, 13, 6, 3, Pal.Amber); c.Rect(25, 12, 4, 4, Pal.White); c.Set(30, 14, Pal.Red);
        });

        /// <summary>Painted floor arrow (points right). 12x8, pivot centre.</summary>
        public static Sprite FloorArrow() => Cached("floorarrow", () =>
        {
            var c = new PixelCanvas(12, 9);
            for (int i = 0; i < 5; i++) { c.HLine(i, i + 3, 4 + i, Pal.Amber); c.HLine(i, i + 3, 4 - i, Pal.Amber); }
            for (int i = 0; i < 5; i++) { c.HLine(i + 6, i + 9, 4 + i, Pal.Amber); c.HLine(i + 6, i + 9, 4 - i, Pal.Amber); }
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });
        // ------------------------------------------------------------------ cadet in four directions

        /// <summary>
        /// The cadet seen from the top-down camera. dir: 0 facing the camera, 1 back (walking away, backpack visible),
        /// 2 side (facing right; flip for left). frame: -1/-2 idle breaths, 0-5 walk or run cycle. Pivot at the feet.
        /// </summary>
        public static Sprite Cadet(int dir, int frame, bool suit, bool run) => !suit && UseBlenderCadet && BlenderCadet(dir, frame) is Sprite b ? b : Crew("cadet", dir, frame, suit, run);

        static Sprite LegacyCadet(int dir, int frame, bool suit, bool run) => Cached($"cadet{dir}_{frame}_{suit}_{run}", () =>
        {
            if (dir == 2)
            {
                // side view: reuse the side-view astronaut, lean forward and bounce when running
                var pose = frame < 0 ? (frame == -1 ? Pose.Idle0 : Pose.Idle1) : (Pose)((int)Pose.Walk0 + (frame % 4));
                var side = DrawAstronaut(suit, pose);
                if (!run || frame < 0) return side.ToSprite();
                var lean = new PixelCanvas(22, 28);
                for (int y = 0; y < 28; y++)
                    for (int x = 0; x < 22; x++)
                    {
                        int sx = y > 12 ? x - 1 : x;
                        int sy = (frame % 2 == 0) ? y + 1 : y;
                        if (sx >= 0 && sy < 28) lean.Set(x, y, side.Get(sx, sy));
                    }
                return lean.ToSprite();
            }
            var c = new PixelCanvas(22, 28);
            Color32 body = suit ? Pal.White : Pal.Orange, bodyD = suit ? Pal.Grey : Pal.OrangeD, bodyL = suit ? Pal.White : Pal.OrangeL;
            Color32 boot = suit ? Pal.Steel : Pal.Boot, hand = suit ? Pal.Grey : Pal.Skin;
            bool moving = frame >= 0;
            int lift = run ? 2 : 1;
            int lL = moving && (frame == 1 || frame == 2) ? lift : 0;
            int rL = moving && (frame == 4 || frame == 5) ? lift : 0;
            int dy = !moving ? (frame == -2 ? -1 : 0) : (run ? (frame % 3 == 0 ? 1 : 0) : (frame % 3 == 1 ? 0 : -1)) + (run ? 1 : 0);
            int swing = moving ? (frame < 3 ? 1 : -1) * (run ? 2 : 1) : 0;

            // legs
            c.Rect(8, 1 + lL, 3, 8 - lL, bodyD); c.Rect(8, 1 + lL, 3, 2, boot);
            c.Rect(12, 1 + rL, 3, 8 - rL, body); c.Rect(12, 1 + rL, 3, 2, boot);
            // arms swing opposite to the legs
            int la = Mathf.Max(0, swing), ra = Mathf.Max(0, -swing);
            c.Rect(5, 10 + dy + la, 2, 7 - Mathf.Abs(swing), bodyD); c.Rect(5, 9 + dy + la, 2, 1, hand);
            c.Rect(16, 10 + dy + ra, 2, 7 - Mathf.Abs(swing), bodyD); c.Rect(16, 9 + dy + ra, 2, 1, hand);
            // torso
            c.Rect(7, 9 + dy, 9, 8, body);
            c.Rect(7, 9 + dy, 9, 1, suit ? Pal.Steel : Pal.Boot);
            c.Rect(7, 10 + dy, 1, 7, bodyD); c.Rect(15, 10 + dy, 1, 6, bodyL);
            if (dir == 0)
            {
                c.Rect(10, 13 + dy, 3, 3, suit ? Pal.Steel : Pal.OrangeL);
                c.Set(9, 15 + dy, Pal.Blue); c.Set(13, 15 + dy, Pal.White);
            }
            else
            {
                // backpack with straps
                c.Rect(8, 10 + dy, 7, 7, Pal.Steel); c.Rect(8, 16 + dy, 7, 1, Pal.SteelL);
                c.Rect(9, 12 + dy, 5, 2, Pal.Steel0); c.Set(11, 14 + dy, Pal.Cyan);
            }
            // head
            if (suit)
            {
                c.RoundRect(7, 17 + dy, 9, 9, Pal.White);
                c.Rect(7, 18 + dy, 1, 6, Pal.Grey);
                if (dir == 0) { c.RoundRect(9, 19 + dy, 5, 4, Pal.Blue); c.Set(12, 21 + dy, Pal.CyanL); }
                else c.Rect(9, 20 + dy, 5, 2, Pal.Grey);
            }
            else if (dir == 0)
            {
                c.Rect(8, 17 + dy, 7, 7, Pal.Skin);
                c.Rect(8, 17 + dy, 1, 6, Pal.SkinD);
                c.Rect(8, 23 + dy, 7, 2, Pal.Hair); c.Rect(7, 20 + dy, 1, 4, Pal.Hair); c.Rect(15, 20 + dy, 1, 4, Pal.Hair);
                c.Rect(9, 22 + dy, 2, 1, Pal.Hair);
                bool blink = frame == -2;
                c.Set(10, 20 + dy, blink ? Pal.SkinD : Pal.Ink); c.Set(13, 20 + dy, blink ? Pal.SkinD : Pal.Ink);
                c.Rect(11, 18 + dy, 2, 1, Pal.SkinD);
            }
            else
            {
                c.Rect(8, 17 + dy, 7, 7, Pal.Hair);
                c.Rect(7, 19 + dy, 1, 4, Pal.Hair); c.Rect(15, 19 + dy, 1, 4, Pal.Hair);
                c.Rect(8, 24 + dy, 7, 1, Pal.Hair);
                c.Set(7, 18 + dy, Pal.Skin); c.Set(15, 18 + dy, Pal.Skin);
            }
            c.Shade(0.14f, 0.2f);
            c.Outline(Pal.Ink);
            return c.ToSprite();
        });

        // ------------------------------------------------------------------ HUD stickers and portraits

        /// <summary>Sticker icon for a resource: o2, water, food, power. 16x16.</summary>
        public static Sprite Sticker(string kind) => Cached("sticker_" + kind, () =>
        {
            var c = new PixelCanvas(16, 16);
            c.Disc(8, 8, 7, Pal.White);
            switch (kind)
            {
                case "o2": c.Disc(6, 7, 3, Pal.Cyan); c.Disc(11, 10, 2, Pal.Cyan); c.Disc(10, 4, 1, Pal.CyanD); c.Set(5, 8, Pal.White); break;
                case "water": c.Disc(8, 6, 3, Pal.Blue); c.Rect(7, 9, 3, 2, Pal.Blue); c.Set(8, 11, Pal.Blue); c.Set(7, 6, Pal.Ice); break;
                case "food": c.Rect(5, 4, 7, 8, Pal.Amber); c.Rect(5, 7, 7, 2, Pal.Red); c.HLine(5, 11, 11, Pal.Grey); break;
                default:
                    for (int i = 0; i < 5; i++) { c.Set(9 - i, 12 - i, Pal.Gold); c.Set(10 - i, 12 - i, Pal.Gold); c.Set(11 - i, 8 - i, Pal.Gold); c.Set(12 - i, 8 - i, Pal.Gold); }
                    c.HLine(5, 11, 7, Pal.Gold); break;
            }
            c.Outline(Pal.Ink);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        /// <summary>Cadet portrait. mood: 0 well, 1 tired or hungry, 2 sick, 3 critical. 24x24.</summary>
        public static Sprite Portrait(int mood) => Cached("portrait" + mood, () =>
        {
            var c = new PixelCanvas(24, 24);
            c.Fill(mood >= 3 ? Pal.RedD : mood == 2 ? PixelCanvas.Hex("#3f5a3a") : Pal.Navy2);
            var skin = mood >= 2 ? PixelCanvas.Hex("#d8c9a0") : Pal.Skin;
            c.Rect(4, 0, 16, 5, Pal.Orange);
            c.RoundRect(6, 4, 12, 14, skin);
            c.Rect(6, 15, 12, 4, Pal.Hair); c.Rect(5, 10, 2, 7, Pal.Hair); c.Rect(17, 10, 2, 7, Pal.Hair); c.Rect(8, 14, 4, 1, Pal.Hair);
            if (mood == 0) { c.Rect(9, 11, 1, 2, Pal.Ink); c.Rect(14, 11, 1, 2, Pal.Ink); }
            else if (mood == 1) { c.HLine(8, 10, 11, Pal.Ink); c.HLine(13, 15, 11, Pal.Ink); c.Rect(8, 10, 3, 1, Pal.SkinD); c.Rect(13, 10, 3, 1, Pal.SkinD); }
            else { c.Set(9, 12, Pal.Ink); c.Set(14, 12, Pal.Ink); c.Rect(8, 10, 3, 1, Pal.SkinD); c.Rect(13, 10, 3, 1, Pal.SkinD); }
            if (mood == 0) { c.HLine(10, 13, 7, Pal.Ink); c.Set(9, 8, Pal.Ink); c.Set(14, 8, Pal.Ink); }
            else if (mood == 1) c.HLine(10, 13, 7, Pal.Ink);
            else { c.HLine(10, 13, 8, Pal.Ink); c.Set(9, 7, Pal.Ink); c.Set(14, 7, Pal.Ink); }
            if (mood >= 2) { c.Set(16, 13, Pal.Cyan); c.Set(16, 12, Pal.Cyan); }
            c.Outline(Pal.Ink);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        /// <summary>Dark lens ring for the first-person peephole: clear middle, soft dark edge, a glint on the glass.</summary>
        public static Sprite LensMask() => Cached("lensmask", () =>
        {
            const int n = 256;
            var c = new PixelCanvas(n, n);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2 - 1, dy = (y + 0.5f) / n * 2 - 1;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.SmoothStep(0, 1, Mathf.Clamp01((d - 0.74f) / 0.22f));
                    float g = Mathf.Clamp01(1 - Mathf.Abs(d - 0.64f) / 0.03f) * Mathf.Clamp01((-dx + dy) * 1.4f - 0.6f);
                    if (g > 0.05f && a < 0.1f) c.Set(x, y, new Color32(255, 255, 255, (byte)(g * 70)));
                    else c.Set(x, y, new Color32(2, 3, 8, (byte)(a * 255)));
                }
            var t = c.ToTexture();
            t.filterMode = FilterMode.Bilinear;
            return Sprite.Create(t, new UnityEngine.Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 16f);
        });

        /// <summary>Lintel over a doorway in a horizontal wall, 16x40, pivot bottom-left (sits high, like a frame).</summary>
        public static Sprite DoorFrame(bool hull) => Cached("doorframe" + hull, () =>
        {
            var c = new PixelCanvas(16, 32);
            var post = hull ? PixelCanvas.Hex("#b5bccc") : Pal.Navy3;
            var dark = hull ? PixelCanvas.Hex("#6f7891") : Pal.Navy0;
            c.Rect(0, 24, 16, 8, post); c.HLine(0, 15, 24, dark); c.HLine(0, 15, 31, dark);
            for (int x = 0; x < 16; x++) if ((x / 2) % 2 == 0) c.Set(x, 26, Pal.Amber);
            return c.ToSprite(new Vector2(0, 0));
        });
        /// <summary>Side view of the small escape pod: blunt cone, heat shield, window, orange stripe; airbags when landing.</summary>
        public static Sprite PodSide(bool airbags) => Cached("podside" + airbags, () =>
        {
            var c = new PixelCanvas(40, 40);
            c.Rect(8, 8, 24, 4, PixelCanvas.Hex("#5b4636"));
            for (int y = 12; y < 34; y++) { int half = 12 - (y - 12) / 3; c.HLine(20 - half, 19 + half, y, y < 22 ? Pal.White : Pal.Grey); }
            c.HLine(9, 30, 18, Pal.Orange); c.HLine(10, 29, 19, Pal.OrangeD);
            c.Disc(20, 25, 3, Pal.Ink); c.Disc(20, 25, 2, Pal.CyanD); c.Set(19, 26, Pal.CyanL);
            c.Rect(18, 34, 4, 2, Pal.Steel); c.Rect(19, 36, 2, 2, Pal.Red);
            c.Rect(10, 6, 3, 2, Pal.Steel0); c.Rect(27, 6, 3, 2, Pal.Steel0);
            if (airbags)
            {
                c.Disc(9, 6, 5, PixelCanvas.Hex("#e8d9b0")); c.Disc(20, 4, 5, PixelCanvas.Hex("#f2e6c6")); c.Disc(31, 6, 5, PixelCanvas.Hex("#e8d9b0"));
                c.Set(8, 8, Pal.White); c.Set(19, 7, Pal.White); c.Set(30, 8, Pal.White);
            }
            c.Outline(Pal.Ink);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });
        // ------------------------------------------------------------------ cutscene sets

        /// <summary>Press hall at mission control: back wall, mission banner, screens, stage edge. 160x90.</summary>
        public static Sprite PressHall() => Cached("presshall", () =>
        {
            var c = new PixelCanvas(160, 90);
            for (int y = 0; y < 90; y++) c.HLine(0, 159, y, Color32.Lerp(PixelCanvas.Hex("#0e1630"), PixelCanvas.Hex("#22305c"), y / 90f));
            c.Rect(0, 0, 160, 22, PixelCanvas.Hex("#1a2140"));
            c.Rect(0, 20, 160, 3, Pal.Steel);
            // screens with Mars and the Kestrel
            c.Rect(14, 44, 34, 24, Pal.Ink); c.Rect(16, 46, 30, 20, PixelCanvas.Hex("#0a1024")); c.Disc(31, 56, 7, Pal.Rust); c.Disc(29, 58, 3, Pal.RustL);
            c.Rect(112, 44, 34, 24, Pal.Ink); c.Rect(114, 46, 30, 20, PixelCanvas.Hex("#0a1024"));
            c.Rect(118, 54, 22, 4, Pal.Grey); c.Rect(140, 55, 3, 2, Pal.Grey); c.Rect(115, 55, 3, 2, Pal.Orange);
            // banner
            c.Rect(52, 56, 56, 22, PixelCanvas.Hex("#16408a")); c.Frame(52, 56, 56, 22, Pal.Gold);
            c.Disc(80, 70, 6, Pal.White); c.Disc(80, 70, 4, PixelCanvas.Hex("#16408a")); c.Line(70, 64, 90, 76, Pal.Red);
            c.Rect(58, 59, 44, 2, Pal.White);
            // ceiling lights
            for (int i = 0; i < 8; i++) c.Rect(8 + i * 20, 86, 8, 2, Pal.CyanL);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        /// <summary>Speaker's podium with a microphone and badge. 30x34.</summary>
        public static Sprite Podium() => Prop("podium", 30, 34, c =>
        {
            c.Rect(3, 0, 24, 26, PixelCanvas.Hex("#3a2f4a"));
            c.Rect(0, 24, 30, 5, PixelCanvas.Hex("#4c3f61"));
            c.Disc(15, 13, 6, Pal.Gold); c.Disc(15, 13, 4, PixelCanvas.Hex("#16408a"));
            c.VLine(21, 29, 33, Pal.Steel); c.Rect(20, 32, 3, 2, Pal.Ink);
        });

        /// <summary>Row of audience heads (silhouettes) in front of the stage. 160x16.</summary>
        public static Sprite Audience() => Cached("audience", () =>
        {
            var c = new PixelCanvas(160, 18);
            var rng = new System.Random(3);
            for (int x = 2; x < 160; x += 9)
            {
                int h = 8 + rng.Next(4);
                c.Disc(x + 3, h, 4, PixelCanvas.Hex("#070a16")); c.Rect(x - 1, 0, 9, h - 2, PixelCanvas.Hex("#070a16"));
                if (rng.NextDouble() < 0.25) c.Rect(x + 5, h + 1, 3, 2, Pal.Steel);   // a camera
            }
            return c.ToSprite(new Vector2(0.5f, 0f));
        });

        /// <summary>Inside the Kestrel cockpit: big window with stars, consoles, warning lights. 160x90.</summary>
        public static Sprite CockpitInterior(bool alarm) => Cached("cockpitint" + alarm, () =>
        {
            var c = new PixelCanvas(160, 90);
            c.Fill(alarm ? PixelCanvas.Hex("#2a1220") : Pal.Navy1);
            c.Rect(20, 34, 120, 46, Pal.Ink);
            c.Rect(23, 37, 114, 40, PixelCanvas.Hex("#04060f"));
            var rng = new System.Random(9);
            for (int i = 0; i < 40; i++) c.Set(24 + rng.Next(112), 38 + rng.Next(38), i % 5 == 0 ? Pal.CyanL : Pal.Grey);
            c.Disc(118, 44, 9, Pal.Rust); c.Disc(115, 46, 5, Pal.RustL);
            c.VLine(60, 37, 76, Pal.Navy2); c.VLine(100, 37, 76, Pal.Navy2);
            c.Rect(0, 0, 160, 22, alarm ? PixelCanvas.Hex("#3a1a26") : Pal.Navy2);
            for (int i = 0; i < 6; i++)
            {
                int x = 10 + i * 25;
                c.Rect(x, 8, 18, 10, Pal.Ink); c.Rect(x + 1, 9, 16, 8, i % 2 == 0 ? Pal.CyanD : PixelCanvas.Hex("#1d3d2c"));
                c.Rect(x + 3, 11, 6, 2, alarm && i % 2 == 1 ? Pal.Red : Pal.Green);
            }
            for (int i = 0; i < 4; i++) c.Disc(14 + i * 44, 84, 2, alarm ? Pal.Red : Pal.Amber);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        /// <summary>One frame of an explosion fireball (0..5 grows then fades).</summary>
        public static Sprite Blast(int frame) => Cached("blast" + frame, () =>
        {
            var c = new PixelCanvas(48, 48);
            int r = 6 + frame * 4;
            var outer = frame < 4 ? Pal.Orange : Pal.Steel;
            c.Disc(24, 24, Mathf.Min(r, 23), outer);
            if (frame < 5) c.Disc(24, 24, Mathf.Max(2, r - 5), frame < 3 ? Pal.Amber : Pal.OrangeD);
            if (frame < 3) c.Disc(24, 24, Mathf.Max(1, r - 10), Pal.White);
            var rng = new System.Random(frame + 4);
            for (int i = 0; i < 10; i++) c.Set(rng.Next(48), rng.Next(48), Pal.Amber);
            return c.ToSprite(new Vector2(0.5f, 0.5f));
        });

        /// <summary>A Mars surface base seen from outside: domes, ribbed modules, greenhouse glow, solar panels, dish. 150x60.</summary>
        public static Sprite MarsBaseExterior() => Cached("marsbaseext", () =>
        {
            var c = new PixelCanvas(150, 60);
            // greenhouse dome (glows)
            c.Ellipse(30, 14, 22, 18, PixelCanvas.Hex("#9ad7c2")); c.Ellipse(30, 14, 19, 15, PixelCanvas.Hex("#3d7f66"));
            for (int x = 12; x < 50; x += 6) c.VLine(x, 2, 26, PixelCanvas.Hex("#7fbfa8"));
            c.Disc(24, 10, 3, Pal.Green); c.Disc(34, 8, 3, Pal.Green); c.Disc(40, 12, 2, Pal.GreenD);
            // main hub dome
            c.Ellipse(80, 16, 26, 20, PixelCanvas.Hex("#d8dbe2")); c.Ellipse(80, 16, 23, 17, PixelCanvas.Hex("#b9bfcc"));
            c.Rect(70, 6, 20, 4, PixelCanvas.Hex("#ffd479")); c.Rect(72, 7, 16, 2, PixelCanvas.Hex("#fff3c0"));
            c.HLine(56, 104, 4, Pal.Steel0);
            // ribbed cylinder module
            c.RoundRect(104, 2, 34, 18, PixelCanvas.Hex("#c9cdd8"));
            for (int x = 107; x < 136; x += 5) c.VLine(x, 3, 18, PixelCanvas.Hex("#9aa1b3"));
            c.Rect(112, 9, 6, 4, Pal.CyanD); c.Rect(124, 9, 6, 4, Pal.CyanD);
            // connecting tubes
            c.Rect(50, 4, 8, 6, PixelCanvas.Hex("#a3a9b8")); c.Rect(98, 4, 8, 6, PixelCanvas.Hex("#a3a9b8"));
            // solar panels and dish
            c.Ellipse(142, 44, 7, 4, Pal.Grey); c.VLine(142, 0, 40, Pal.Steel); c.Disc(142, 48, 2, Pal.Red);
            c.Rect(0, 0, 150, 2, Pal.RustD);
            return c.ToSprite(new Vector2(0.5f, 0f));
        });

        /// <summary>One segment of the flexible docking tube. 12x8.</summary>
        public static Sprite TubeSegment() => Prop("tubeseg", 12, 8, c =>
        {
            c.Rect(0, 0, 12, 8, PixelCanvas.Hex("#a3a9b8")); c.VLine(2, 0, 7, PixelCanvas.Hex("#7c8396")); c.VLine(8, 0, 7, PixelCanvas.Hex("#7c8396"));
            c.HLine(0, 11, 6, PixelCanvas.Hex("#c9cdd8"));
        });
    }
}
