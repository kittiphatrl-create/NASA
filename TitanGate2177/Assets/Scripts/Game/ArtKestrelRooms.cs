using UnityEngine;

namespace TitanGate.Game
{
    public enum KestrelRoomLook { Deck, Lab, Quarters, Galley, Cockpit }

    public static partial class Art
    {
        public static Sprite KestrelDamageDisplay(Sprite source, System.Predicate<Color> isEmissive)
            => Cached($"kestrel/damage-display/{source.GetInstanceID()}", () =>
            {
                var rect = source.textureRect;
                int w = Mathf.RoundToInt(rect.width), h = Mathf.RoundToInt(rect.height);
                var pixels = source.texture.GetPixels(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), w, h);
                var mask = new PixelCanvas(w, h);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var pixel = pixels[y * w + x];
                    if (!isEmissive(pixel)) continue;
                    float value = Mathf.Lerp(.58f, 1f, Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b)));
                    mask.Set(x, y, new Color(value, value, value, pixel.a));
                }
                var sprite = mask.ToSprite(source.pivot / source.rect.size, source.pixelsPerUnit);
                sprite.texture.filterMode = source.texture.filterMode;
                return sprite;
            });

        public static Sprite KestrelHullLight(bool lens) => Cached($"kestrel/hull-light/{lens}", () =>
        {
            var art = new PixelCanvas(48, 74);
            if (lens)
            {
                art.RoundRect(24, 15, 14, 44, Color.white);
                art.Rect(33, 19, 3, 36, new Color32(255, 255, 255, 180));
            }
            else
            {
                art.RoundRect(1, 2, 45, 70, KInk);
                art.RoundRect(4, 6, 38, 62, KSteel);
                art.RoundRect(7, 9, 34, 56, KLight);
                art.RoundRect(17, 12, 23, 50, KShadow);
                art.Disc(11, 15, 3, KInk);
                art.Disc(11, 59, 3, KInk);
            }
            var sprite = art.ToSprite(new Vector2(.5f, .5f), 132f);
            sprite.texture.filterMode = FilterMode.Bilinear;
            return sprite;
        });

        public static Sprite KestrelRoomFloor(KestrelRoomLook look, int kind, int variant)
        {
            if (look == KestrelRoomLook.Deck) return KestrelFloor(kind, variant);
            int pattern = look switch
            {
                KestrelRoomLook.Quarters => variant,
                KestrelRoomLook.Cockpit => variant == 4 ? 1 : variant % 3 == 2 ? 2 : 0,
                KestrelRoomLook.Lab => variant == 4 ? 2 : variant % 3 == 1 ? 1 : 0,
                _ => variant % 3
            };
            return Cached($"kestrel/room-floor/{look}/{pattern}", () =>
            {
                var c = new PixelCanvas(64, 64);
                switch (look)
                {
                    case KestrelRoomLook.Cockpit:
                        c.Fill(PixelCanvas.Hex("#4f6581"));
                        c.Rect(2, 2, 61, 61, PixelCanvas.Hex("#a3bed9"));
                        c.HLine(2, 62, 62, PixelCanvas.Hex("#d1e4f7"));
                        c.VLine(2, 2, 61, PixelCanvas.Hex("#c2d9ed"));
                        c.HLine(3, 62, 2, KSteel);
                        foreach (int x in new[] { 6, 55 })
                        foreach (int y in new[] { 6, 54 }) KBolt(c, x, y);
                        if (pattern == 1) KVent(c, 17, 15, 30, 34);
                        break;
                    case KestrelRoomLook.Galley:
                        var cream = PixelCanvas.Hex("#dec09d");
                        var terracotta = PixelCanvas.Hex("#ad644f");
                        var glaze = PixelCanvas.Hex("#b8a294");
                        cream = Color.Lerp(cream, glaze, pattern * .10f);
                        terracotta = Color.Lerp(terracotta, glaze, pattern * .10f);
                        c.Fill(cream);
                        c.Rect(0, 0, 32, 32, terracotta);
                        c.Rect(32, 32, 32, 32, terracotta);
                        break;
                    case KestrelRoomLook.Quarters:
                        var purple = PixelCanvas.Hex("#45415e");
                        c.Fill(purple);
                        var rng = new System.Random(2177 + pattern);
                        for (int i = 0; i < 290; i++)
                            c.Rect(rng.Next(64), rng.Next(64), rng.Next(1, 4), 2,
                                i % 3 == 0 ? PixelCanvas.Hex("#55506d") : PixelCanvas.Hex("#39364f"));
                        break;
                    case KestrelRoomLook.Lab:
                        var green = pattern == 1 ? PixelCanvas.Hex("#3b4d44") : PixelCanvas.Hex("#344a46");
                        c.Fill(PixelCanvas.Hex("#182d2d"));
                        c.Rect(2, 2, 60, 60, green);
                        c.HLine(2, 61, 61, PixelCanvas.Hex("#50645b"));
                        c.VLine(2, 2, 61, PixelCanvas.Hex("#415951"));
                        KBolt(c, 5, 5);
                        KBolt(c, 55, 55);
                        if (pattern == 2) KVent(c, 22, 22, 20, 20);
                        break;
                }
                return c.ToSprite(Vector2.zero, 64);
            });
        }

        public static Sprite KestrelRoomWall(KestrelRoomLook look, bool face, bool hull, int variant, int deckSides)
        {
            if (look == KestrelRoomLook.Cockpit) return KestrelCockpitWall(face, deckSides);
            if (look == KestrelRoomLook.Deck || (!face && !hull))
                return KestrelWall(face, hull, variant, deckSides);
            return Cached($"kestrel/room-wall/{look}/{face}/{deckSides}", () =>
            {
                var c = new PixelCanvas(64, face ? 128 : 64);
                var edge = PixelCanvas.Hex("#e2d9b9");
                var seam = PixelCanvas.Hex("#74858d");
                if (!face)
                {
                    c.Fill(seam);
                    c.Rect(4, 3, 57, 58, edge);
                    c.HLine(5, 60, 60, KRim);
                }
                else
                {
                    c.Rect(0, 7, 64, 107, KInk);
                    c.Rect(0, 11, 64, 97, look == KestrelRoomLook.Lab ? PixelCanvas.Hex("#9eafa5") : PixelCanvas.Hex("#aab7b5"));
                    c.Rect(0, 9, 64, 9, KShadow);
                    c.Rect(0, 38, 64, 5, PixelCanvas.Hex("#ae8749"));
                    c.HLine(0, 63, 65, seam);
                    c.VLine(30, 19, 105, seam);
                    c.VLine(63, 19, 105, seam);
                    c.Rect(0, 91, 64, 23, edge);
                    c.HLine(0, 63, 113, KRim);
                }
                return c.ToSprite(Vector2.zero, 64);
            });
        }

        static Sprite KestrelCockpitWall(bool face, int deckSides) => Cached($"kestrel/cockpit-wall/{face}/{deckSides}", () =>
        {
            var c = new PixelCanvas(64, face ? 128 : 64);
            if (face)
            {
                KPanel(c, 0, 14, 64, 103, KLight);
                c.Rect(0, 100, 64, 16, KRim);
                c.HLine(0, 63, 115, Pal.White);
                c.HLine(0, 63, 92, KSteel);
                c.HLine(0, 63, 61, KSteel);
                c.VLine(59, 22, 95, KSteel);
                for (int i = 0; i < KCable.Length; i++)
                {
                    int y = 22 + i * 4;
                    c.Rect(0, y - 1, 64, 4, KInk);
                    c.HLine(0, 63, y, KCable[i]);
                }
            }
            else
            {
                c.Fill(KInk);
                c.Rect(6, 6, 52, 52, KShadow);
                if ((deckSides & 1) != 0) KestrelCockpitRim(c, 0);
                if ((deckSides & 2) != 0) KestrelCockpitRim(c, 44);
                if ((deckSides & 4) != 0)
                {
                    c.Rect(0, 50, 64, 14, KLight);
                    c.HLine(0, 63, 62, KRim);
                }
            }
            return c.ToSprite(Vector2.zero, 64);
        });

        public static Sprite KestrelCaptainDoor(bool locked, int frame) => Cached($"kestrel/captain-door/{locked}/{frame}", () =>
        {
            const int width = 96, height = 192;
            var art = new PixelCanvas(width, height);
            var pearl = PixelCanvas.Hex("#e7edf1");
            var navy = PixelCanvas.Hex("#243348");
            var lamp = locked ? Pal.Red : Pal.Green;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (x < 4 || x >= 92) continue;
                bool pocket = y < 18 || y >= 174;
                int travel = frame * 26;
                int leaf = y < height / 2 ? y + travel : height - 1 - y + travel;
                bool closedLeaf = leaf < 95 && x >= 17 && x < 79;
                int edge = Mathf.Min(x - 4, 91 - x);
                if (pocket || edge < 13)
                {
                    float shine = .82f - .055f * Mathf.Abs(edge - 5);
                    art.Set(x, y, Color.Lerp(navy, pearl, Mathf.Clamp01(shine)));
                    continue;
                }
                if (!closedLeaf) continue;
                float sheen = .32f + .18f * Mathf.Cos((x - 30f) * .048f);
                var panel = Color.Lerp(navy, KSteel, sheen);
                if (leaf >= 91 || x < 20 || x > 75) panel = KLight;
                if (leaf >= 57 && leaf <= 69 && x >= 26 && x <= 69)
                    panel = Color.Lerp(KScreen, KCyan, .18f + (69 - leaf) * .035f);
                art.Set(x, y, panel);
            }
            art.Rect(33, 178, 30, 9, KInk);
            art.Rect(37, 181, 22, 3, lamp);
            var sprite = art.ToSprite(Vector2.zero, width);
            sprite.texture.filterMode = FilterMode.Bilinear;
            return sprite;
        });

        public static Sprite KestrelObservationGlass() => Cached("kestrel/observation-glass", () =>
        {
            const int size = 128;
            var pane = new PixelCanvas(size, size);
            for (int x = 0; x < size; x++)
            {
                int edge = Mathf.Min(x, size - 1 - x);
                var frame = Color.Lerp(KInk, KSteel, 1f - Mathf.Abs(edge - 5f) / 8f);
                float reflection = Mathf.Exp(-Mathf.Pow((x - 22f) / 5f, 2f));
                Color32 glass = new Color(.22f, .54f, .71f, .16f + reflection * .20f);
                Color32 column = edge < 10 ? (Color32)frame : glass;
                pane.VLine(x, 0, size - 1, column);
            }
            // Narrow seals break up the run of glass while leaving the space backdrop visible.
            pane.Rect(10, 0, size - 20, 3, KShadow);
            pane.Rect(10, 3, size - 20, 2, KSteel);
            return pane.ToSprite(Vector2.zero, size);
        });

        static void KestrelCockpitRim(PixelCanvas c, int x)
        {
            c.Rect(x, 0, 20, 64, KLight);
            c.VLine(x + 2, 0, 63, KRim);
            c.Rect(x + 5, 0, 11, 64, KInk);
            for (int i = 0; i < KCable.Length; i++)
                c.VLine(x + 6 + i * 4, 0, 63, KCable[i]);
        }

        public static Sprite KestrelCockpitScreens() => Cached("kestrel/cockpit-screen-bank", () =>
        {
            const int width = 46, height = 154;
            var c = new PixelCanvas(width, height);
            for (int panel = 0; panel < 3; panel++)
            {
                for (int x = 0; x < width; x++)
                {
                    int bottom = panel * 44 + x / 3;
                    c.VLine(x, bottom, bottom + 44, KInk);
                    if (x < 2 || x >= 44) continue;
                    c.VLine(x, bottom + 3, bottom + 40, x < 7 ? KRim : KLight);
                    if (x < 10 || x > 40) continue;
                    c.VLine(x, bottom + 7, bottom + 36, KScreen);
                    for (int line = 0; line < 5; line++)
                        if (x < 26 + (line * 7 + panel * 3) % 15)
                            c.Set(x, bottom + 12 + line * 5, line == 4 ? KCyan : KSteel);
                }
            }
            return c.ToSprite(new Vector2(.5f, 0), 48);
        });

        public static Sprite KestrelBunk() => Cached("kestrel/quarters/bunk", () =>
        {
            var c = new PixelCanvas(112, 48);
            KPanel(c, 2, 4, 108, 40, KSteel);
            KPanel(c, 7, 10, 99, 34, PixelCanvas.Hex("#ccd2e4"));
            KPanel(c, 12, 15, 24, 24, PixelCanvas.Hex("#e6e9f5"));
            KPanel(c, 37, 12, 67, 29, PixelCanvas.Hex("#425680"));
            c.Rect(40, 14, 61, 7, PixelCanvas.Hex("#26385f"));
            c.VLine(42, 19, 38, PixelCanvas.Hex("#7691bb"));
            c.Rect(1, 2, 5, 45, KInk);
            c.VLine(3, 4, 43, KLight);
            c.Rect(101, 1, 5, 8, KInk);
            return c.ToSprite(new Vector2(.5f, 0), 40);
        });

        public static Sprite KestrelGalleySofa() => Cached("kestrel/galley/sofa", () =>
        {
            var c = new PixelCanvas(112, 54);
            var blue = PixelCanvas.Hex("#315d9b");
            KPanel(c, 8, 21, 96, 32, blue);
            KPanel(c, 11, 5, 90, 27, PixelCanvas.Hex("#234477"));
            KPanel(c, 13, 9, 42, 21, blue);
            KPanel(c, 57, 9, 42, 21, blue);
            foreach (int x in new[] { 3, 97 }) KPanel(c, x, 4, 12, 33, blue);
            c.HLine(13, 98, 48, PixelCanvas.Hex("#739bd2"));
            return c.ToSprite(new Vector2(.5f, 0), 40);
        });

        public static Sprite KestrelGalleyFridge() => Cached("kestrel/galley/fridge", () =>
        {
            var c = new PixelCanvas(52, 82);
            var enamel = PixelCanvas.Hex("#d7e0e6");
            KPanel(c, 2, 2, 48, 79, enamel);
            KPanel(c, 6, 5, 39, 40, enamel);
            KPanel(c, 6, 45, 39, 33, enamel);
            c.Rect(36, 22, 4, 13, KSteel);
            c.Rect(36, 53, 4, 11, KSteel);
            c.Rect(12, 64, 17, 6, PixelCanvas.Hex("#6baa8f"));
            c.HLine(15, 25, 67, KRim);
            return c.ToSprite(new Vector2(.5f, 0), 36);
        });

        public static Sprite KestrelGalleyRack() => Cached("kestrel/galley/rack", () =>
        {
            var c = new PixelCanvas(60, 82);
            KPanel(c, 1, 0, 58, 81, KSteel);
            KPanel(c, 6, 15, 48, 61, KScreen);
            var jars = new[] { KAmber, KCyan, PixelCanvas.Hex("#b36764"), PixelCanvas.Hex("#8bb879") };
            for (int row = 0; row < 2; row++)
            {
                int y = 39 + row * 22;
                c.HLine(8, 51, y - 1, KLight);
                for (int i = 0; i < jars.Length; i++)
                {
                    int x = 11 + i * 10;
                    c.Rect(x, y, 7, 11 + (i % 2) * 2, jars[(i + row) % jars.Length]);
                    c.HLine(x, x + 6, y + 8, KRim);
                }
            }
            c.Rect(11, 20, 37, 10, KInk);
            c.HLine(13, 46, 25, KLight);
            return c.ToSprite(new Vector2(.5f, 0), 40);
        });

        public static Sprite KestrelLabPlanter(int variant) => Cached($"kestrel/lab/planter/{variant}", () =>
        {
            var c = new PixelCanvas(84, 52);
            var rng = new System.Random(2177 + variant);
            KPanel(c, 0, 1, 84, 38, KSteel);
            c.Rect(5, 8, 74, 26, PixelCanvas.Hex("#49362e"));
            for (int x = 10; x < 76; x += 12)
            for (int i = 0; i < 3; i++)
            {
                int y = 15 + i * 9 + rng.Next(4);
                int offset = rng.Next(-3, 4);
                c.Ellipse(x + offset, y, 8, 7, PixelCanvas.Hex("#193d30"));
                c.Ellipse(x + offset, y + 3, 6, 6, i == 2 ? PixelCanvas.Hex("#64ae6c") : PixelCanvas.Hex("#488954"));
                c.Line(x + offset - 3, y + 4, x + offset + 1, y + 6, PixelCanvas.Hex("#97ca80"));
            }
            c.Rect(2, 3, 80, 4, KShadow);
            return c.ToSprite(new Vector2(.5f, 0), 40);
        });
    }
}
