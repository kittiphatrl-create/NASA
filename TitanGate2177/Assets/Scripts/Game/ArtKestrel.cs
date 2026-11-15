using UnityEngine;

namespace TitanGate.Game
{
    public static partial class Art
    {
        static readonly Color32 KInk = PixelCanvas.Hex("#111b2d");
        static readonly Color32 KShadow = PixelCanvas.Hex("#27364d");
        static readonly Color32 KSteel = PixelCanvas.Hex("#617e99");
        static readonly Color32 KLight = PixelCanvas.Hex("#a9c2d6");
        static readonly Color32 KRim = PixelCanvas.Hex("#d0e2ed");
        static readonly Color32 KCyan = PixelCanvas.Hex("#76e3e8");
        static readonly Color32 KAmber = PixelCanvas.Hex("#f1c36f");
        static readonly Color32 KScreen = PixelCanvas.Hex("#122f45");
        static readonly Color32[] KCable = {
            PixelCanvas.Hex("#ba6974"), PixelCanvas.Hex("#4e9eae"), PixelCanvas.Hex("#d1a27b")
        };

        static void KPanel(PixelCanvas c, int x, int y, int w, int h, Color32 face)
        {
            c.Rect(x, y, w, h, KInk);
            c.Rect(x + 2, y + 3, w - 4, h - 5, face);
            c.HLine(x + 2, x + w - 3, y + h - 3, Color.Lerp(face, KRim, .55f));
            c.VLine(x + 2, y + 3, y + h - 3, Color.Lerp(face, KRim, .32f));
            c.HLine(x + 3, x + w - 3, y + 3, Color.Lerp(face, KInk, .45f));
        }

        static void KBolt(PixelCanvas c, int x, int y)
        {
            c.Rect(x, y, 3, 3, KShadow);
            c.Set(x + 1, y + 2, KLight);
        }

        static void KVent(PixelCanvas c, int x, int y, int w, int h)
        {
            KPanel(c, x, y, w, h, KSteel);
            for (int row = y + 5; row < y + h - 4; row += 4)
                c.HLine(x + 4, x + w - 5, row, KInk);
        }

        public static Sprite KestrelFloor(int kind, int variant) => Cached($"kestrel/floor/{kind}/{variant}", () =>
        {
            var c = new PixelCanvas(64, 64);
            var steel = kind switch
            {
                3 => PixelCanvas.Hex("#4c5b71"),
                5 => PixelCanvas.Hex("#52677c"),
                1 => PixelCanvas.Hex("#839faa"),
                7 => PixelCanvas.Hex("#8c9eaf"),
                _ => PixelCanvas.Hex("#879fb7"),
            };
            c.Fill(Color.Lerp(steel, KInk, .55f));
            c.Rect(2, 2, 61, 61, steel);
            c.HLine(2, 62, 62, Color.Lerp(steel, KRim, .57f));
            c.VLine(2, 3, 62, Color.Lerp(steel, KRim, .38f));
            c.HLine(3, 62, 2, Color.Lerp(steel, KInk, .36f));
            c.VLine(62, 3, 61, Color.Lerp(steel, KInk, .27f));
            foreach (int x in new[] { 6, 55 })
            foreach (int y in new[] { 6, 54 }) KBolt(c, x, y);
            if (variant == 4) KVent(c, 22, 21, 20, 21);
            else if (kind == 8 && (variant == 0 || variant == 6))
                for (int x = 15; x < 48; x += 9)
                {
                    c.Line(x, 11, x + 5, 17, KAmber);
                    c.Line(x + 1, 11, x + 6, 17, KRim);
                }
            else if (variant == 2)
                c.HLine(16, 27, 17, Color.Lerp(steel, KInk, .16f));
            return c.ToSprite(Vector2.zero, 64);
        });

        static void KCables(PixelCanvas c, int variant)
        {
            for (int i = 0; i < KCable.Length; i++)
            {
                int y = 4 + i * 4;
                c.Rect(0, y - 1, 64, 4, KInk);
                c.HLine(0, 63, y, KCable[i]);
                int x = (variant % 2 == 0 ? 11 : 49) + i * 4;
                if (x >= 63 || variant % 4 == 3) continue;
                c.Line(x, y, x + 4, y + 6, KInk);
                c.Rect(x + 3, y + 5, 4, 18 - y, KInk);
                c.Line(x + 1, y, x + 4, y + 5, KCable[i]);
                c.VLine(x + 4, y + 5, 20, KCable[i]);
            }
        }

        public static Sprite KestrelWall(bool face, bool hull, int variant, int deckSides)
            => Cached($"kestrel/wall/{face}/{hull}/{variant}/{deckSides}", () =>
            {
                var c = new PixelCanvas(64, face ? 128 : 64);
                if (!face)
                {
                    c.Fill(KInk);
                    var channel = hull ? PixelCanvas.Hex("#1b2739") : KShadow;
                    if ((deckSides & 3) != 0)
                    {
                        c.Rect(17, 0, 30, 64, channel);
                        c.VLine(17, 0, 63, KSteel);
                        c.VLine(46, 0, 63, KShadow);
                    }
                    else
                    {
                        c.Rect(0, 18, 64, 28, channel);
                        c.HLine(0, 63, 46, KSteel);
                        c.HLine(0, 63, 18, KShadow);
                    }
                    if ((deckSides & 1) != 0) { c.Rect(0, 0, 8, 64, KLight); c.VLine(2, 0, 63, KRim); }
                    if ((deckSides & 2) != 0) { c.Rect(56, 0, 8, 64, KLight); c.VLine(61, 0, 63, KRim); }
                    if ((deckSides & 4) != 0) { c.Rect(0, 55, 64, 9, KLight); c.HLine(0, 63, 61, KRim); }
                    if ((deckSides & 8) != 0) { c.Rect(0, 0, 64, 9, KLight); c.HLine(0, 63, 2, KRim); }
                    return c.ToSprite(Vector2.zero, 64);
                }
                c.Rect(0, 14, 64, 105, KInk);
                c.Rect(0, 16, 64, 16, KShadow);
                c.HLine(0, 63, 20, KLight);
                c.HLine(0, 63, 23, KSteel);
                c.Rect(0, 92, 64, 23, KSteel);
                c.Rect(0, 109, 64, 9, KLight);
                c.HLine(0, 63, 117, KRim);
                c.HLine(0, 63, 106, KShadow);
                KPanel(c, 4, 30, 56, 72, hull ? KSteel : KLight);
                KPanel(c, 9, 36, 46, 60, KSteel);
                if (variant % 4 == 0)
                    KVent(c, 15, 64, 27, 22);
                else
                {
                    var led = variant % 4 == 1 ? PixelCanvas.Hex("#9cd491") : variant % 4 == 2 ? KCyan : KAmber;
                    KPanel(c, 16, 68, 20, 11, KScreen);
                    c.Rect(21, 72, 10, 3, led);
                }
                KPanel(c, 39, 44, 12, 18, KShadow);
                c.Rect(43, 54, 4, 3, KRim);
                c.Set(44, 49, KCyan);
                for (int i = 0; i < 5; i++) c.Rect(12 + i * 7, 39, 4, 2, i == 3 ? KAmber : KLight);
                KBolt(c, 7, 97);
                KCables(c, variant);
                return c.ToSprite(Vector2.zero, 64);
            });

                public static Sprite KestrelDoor(bool horizontal, bool locked, int frame)
        {
            frame = Mathf.Clamp(frame, 0, 3);
            return Cached($"kestrel/door/{horizontal}/{locked}/{frame}", () =>
            {
                var c = new PixelCanvas(horizontal ? 128 : 64, 128);
                var lamp = locked ? PixelCanvas.Hex("#f57069") : PixelCanvas.Hex("#83db9c");
                var frameSteel = PixelCanvas.Hex("#53657f");
                var guide = PixelCanvas.Hex("#273348");

                if (horizontal)
                {
                    var aperture = new RectInt(17, 20, 94, 80);
                    int travel = 47 * frame / 3;
                    KDoorLeaf(c, aperture, 17 - travel, 20, 47, 80);
                    KDoorLeaf(c, aperture, 64 + travel, 20, 47, 80);

                    c.Rect(8, 100, 112, 21, frameSteel);
                    c.HLine(8, 119, 120, KLight);
                    c.HLine(8, 119, 102, KShadow);
                    c.Rect(13, 97, 102, 7, KInk);
                    c.Rect(56, 105, 16, 5, KInk);
                    c.Rect(59, 106, 10, 3, lamp);
                    KDoorJamb(c, 3, 17, 14, 86);
                    KDoorJamb(c, 111, 17, 14, 86);
                    c.HLine(17, 110, 18, guide);
                    c.HLine(17, 110, 19, KSteel);
                }
                else
                {
                    var aperture = new RectInt(10, 13, 44, 102);
                    int travel = 51 * frame / 3;
                    KDoorLeaf(c, aperture, 10, 13 - travel, 44, 51);
                    KDoorLeaf(c, aperture, 10, 64 + travel, 44, 51);
                    KDoorJamb(c, 1, 0, 9, 128);
                    KDoorJamb(c, 54, 0, 9, 128);
                    c.Rect(10, 115, 44, 13, guide);
                    c.HLine(10, 53, 126, KLight);
                    c.Rect(27, 118, 10, 4, lamp);
                    c.Rect(10, 0, 44, 13, guide);
                    c.HLine(10, 53, 11, KSteel);
                }

                return c.ToSprite(Vector2.zero, 64);
            });
        }

        static void KDoorLeaf(PixelCanvas door, RectInt aperture, int x, int y, int width, int height)
        {
            var leaf = new PixelCanvas(width, height);
            KPanel(leaf, 0, 0, width, height, PixelCanvas.Hex("#4e5f7c"));
            leaf.Frame(4, 5, width - 8, height - 11, PixelCanvas.Hex("#63728d"));
            leaf.Rect(12, height - 29, width - 24, 16, KShadow);
            leaf.Rect(14, height - 27, width - 28, 12, PixelCanvas.Hex("#549bc5"));
            leaf.HLine(15, width - 16, height - 16, PixelCanvas.Hex("#7cbbd9"));
            leaf.Rect(8, 13, width - 16, 5, PixelCanvas.Hex("#e8a04c"));
            leaf.HLine(8, width - 9, 17, KAmber);
            for (int row = Mathf.Max(y, aperture.yMin); row < Mathf.Min(y + height, aperture.yMax); row++)
                for (int col = Mathf.Max(x, aperture.xMin); col < Mathf.Min(x + width, aperture.xMax); col++)
                    door.Set(col, row, leaf.Get(col - x, row - y));
        }

        static void KDoorJamb(PixelCanvas c, int x, int y, int width, int height)
        {
            KPanel(c, x, y, width, height, KShadow);
            c.Rect(x + 3, y + 7, width - 6, height - 14, PixelCanvas.Hex("#e0ad71"));
            c.Rect(x + 4, y + 10, width - 8, height - 20, PixelCanvas.Hex("#fff1be"));
        }

public static Sprite KestrelSeat() => Cached("kestrel/seat", () =>
        {
            var c = new PixelCanvas(52, 58);
            var blue = PixelCanvas.Hex("#467fb7");
            KPanel(c, 7, 0, 10, 26, KSteel);
            KPanel(c, 35, 0, 10, 24, KSteel);
            KPanel(c, 4, 18, 46, 15, blue);
            KPanel(c, 4, 26, 16, 32, blue);
            c.VLine(8, 29, 53, PixelCanvas.Hex("#70a6db"));
            c.HLine(19, 45, 28, PixelCanvas.Hex("#70a6db"));
            return c.ToSprite(new Vector2(.5f, 0), 40);
        });

        public static Sprite KestrelConsole(int variant) => Cached($"kestrel/console/{variant}", () =>
        {
            var c = new PixelCanvas(80, 68);
            KPanel(c, 8, 0, 64, 22, KSteel);
            KPanel(c, 0, 14, 80, 53, KLight);
            KPanel(c, 6, 23, 68, 38, KShadow);
            c.Rect(11, 28, 58, 29, KScreen);
            if (variant == 0)
            {
                c.Disc(39, 42, 6, KAmber);
                for (int a = 0; a < 360; a += 12)
                {
                    float r = a * Mathf.Deg2Rad;
                    c.Rect(39 + Mathf.RoundToInt(Mathf.Cos(r) * 17), 42 + Mathf.RoundToInt(Mathf.Sin(r) * 11), 2, 2, KCyan);
                }
            }
            else
            {
                c.Rect(13, 52, 24, 2, KCyan);
                c.HLine(13, 65, 48, KSteel);
                for (int i = 0; i < 4; i++)
                {
                    c.Rect(14, 32 + i * 4, 12 + (i * 13 + variant * 3) % 32, 1, i == 1 ? KRim : KCyan);
                    c.Rect(58, 32 + i * 4, 6, 2, i == 2 ? KAmber : KSteel);
                }
            }
            for (int i = 0; i < 6; i++) c.Rect(10 + i * 10, 19, 4, 2, i == 1 ? KAmber : KCyan);
            KBolt(c, 3, 60); KBolt(c, 73, 60);
            return c.ToSprite(new Vector2(.5f, 0), 48);
        });

        public static Sprite KestrelLocker(bool open, bool galley = false) => Cached($"kestrel/locker/{open}/{galley}", () =>
        {
            var c = new PixelCanvas(48, 80);
            KPanel(c, 1, 0, 46, 80, KLight);
            KPanel(c, 5, 7, 38, 65, open ? KShadow : KSteel);
            if (open)
            {
                c.Rect(10, 13, 28, 48, KInk);
                c.HLine(10, 37, 36, KLight);
                c.Rect(15, 14, 15, 10, KAmber);
            }
            else
            {
                KPanel(c, 10, 52, 24, 12, KScreen);
                c.Rect(15, 56, 14, 3, galley ? KAmber : KCyan);
                KPanel(c, 32, 29, 7, 13, KShadow);
                c.Rect(16, 26, 10, 8, KRim);
                c.HLine(18, 24, 30, KSteel);
                c.HLine(18, 23, 28, KSteel);
            }
            KVent(c, 9, 7, 25, 13);
            return c.ToSprite(new Vector2(.5f, 0), 36);
        });

        public static Sprite LifeboatPod(bool open, int fill, int slots) => Cached($"lifeboat/pod/{open}/{fill}/{slots}", () =>
        {
            var c = new PixelCanvas(128, 94);
                        var orange = PixelCanvas.Hex("#de793b");
            var shade = PixelCanvas.Hex("#9b4a37");
            var gold = PixelCanvas.Hex("#ffc47a");
            var ivory = PixelCanvas.Hex("#ffe2aa");
            var glass = PixelCanvas.Hex("#204f6c");
            var glassLight = PixelCanvas.Hex("#357f9c");

            // Swept thruster pods stand out from the narrower pressure hull.
            for (int row = 7; row <= 54; row++)
            {
                int outer = row < 14 ? 9 - (row - 7) : 2 + (row - 14) * 17 / 40;
                int inner = 36 + (row - 7) * 13 / 47;
                c.HLine(outer, inner, row, KInk);
                c.HLine(127 - inner, 127 - outer, row, KInk);
                if (row < 10 || row > 51) continue;
                var panel = row < 18 ? KShadow : row < 29 ? shade : orange;
                c.HLine(outer + 3, inner - 2, row, panel);
                c.HLine(129 - inner, 124 - outer, row, panel);
                c.Set(outer + 3, row, gold);
                c.Set(124 - outer, row, shade);
            }
            foreach (int x in new[] { 12, 99 })
            {
                KPanel(c, x, 3, 17, 17, KSteel);
                c.Rect(x + 5, 5, 7, 4, KInk);
                c.HLine(x + 6, x + 10, 9, KCyan);
            }

            var profile = new[] { (5, 24), (9, 36), (21, 43), (40, 49), (54, 50),
                (65, 45), (76, 35), (86, 23), (92, 11) };
            for (int i = 1; i < profile.Length; i++)
            {
                var (bottom, lowerWidth) = profile[i - 1];
                var (top, upperWidth) = profile[i];
                for (int row = bottom; row <= top; row++)
                {
                    int w = Mathf.RoundToInt(Mathf.Lerp(lowerWidth, upperWidth, (row - bottom) / (float)(top - bottom)));
                    c.HLine(64 - w, 63 + w, row, KInk);
                    if (row < 8 || row > 89) continue;
                    var shell = row < 20 ? shade : row < 50 ? orange : gold;
                    c.HLine(67 - w, 60 + w, row, shell);
                    c.HLine(67 - w, 71 - w, row, row > 53 ? ivory : gold);
                    c.HLine(56 + w, 60 + w, row, shade);
                }
            }
            c.HLine(56, 71, 88, ivory);

            // Trapezoidal canopy with a continuous blue reflection across both panes.
            for (int row = 50; row < 75; row++)
            {
                int inset = row < 55 ? 55 - row : (row - 55) * 3 / 4;
                c.HLine(19 + inset, 108 - inset, row, KInk);
                if (row >= 53 && row <= 71)
                    c.HLine(22 + inset, 105 - inset, row, row < 62 ? glass : glassLight);
            }
            c.Line(31, 56, 42, 70, KCyan);
            c.Line(38, 55, 46, 66, KLight);
            c.HLine(46, 90, 70, KCyan);
            c.Rect(62, 52, 3, 21, KShadow);
            c.Rect(21, 43, 22, 5, KRim);
            c.Rect(85, 43, 22, 5, KRim);
            foreach (int x in new[] { 19, 93 })
            {
                KPanel(c, x, 24, 16, 13, KShadow);
                c.HLine(x + 4, x + 11, 29, KLight);
                c.Rect(x + 5, 32, 6, 2, KAmber);
            }

            c.RoundRect(41, 7, 46, 40, KInk);
            KPanel(c, 44, 10, 40, 34, open ? KShadow : KSteel);
            if (open)
            {
                c.Rect(49, 15, 30, 22, KInk);
                c.HLine(50, 77, 35, KCyan);
                for (int i = 0; i < slots; i++)
                    c.Rect(50 + i * 28 / slots, 20, Mathf.Max(1, 25 / slots), 4, i < fill ? KCyan : KShadow);
                KPanel(c, 46, 1, 36, 13, KSteel);
                c.HLine(50, 77, 7, KShadow);
            }
            else
            {
                KBolt(c, 48, 16);
                KBolt(c, 76, 16);
                c.VLine(63, 15, 40, KShadow);
                c.Rect(55, 27, 18, 5, KShadow);
                c.HLine(57, 70, 30, KLight);
                c.HLine(48, 79, 10, KRim);
            }
return c.ToSprite(new Vector2(.5f, 0), 32);
        });

        public static Sprite KestrelPickupOutline(Sprite item) => Cached("kestrel/pickup-outline/" + item.GetInstanceID(), () =>
        {
            int width = (int)item.rect.width, height = (int)item.rect.height;
            float rim = Mathf.Max(1f, item.pixelsPerUnit * .035f);
            int pad = Mathf.CeilToInt(item.pixelsPerUnit * .21f);
            var pixels = new PixelCanvas(width + pad * 2, height + pad * 2);
            var source = item.texture.GetPixels((int)item.rect.x, (int)item.rect.y, width, height);
            var distance = new float[pixels.W * pixels.H];
            for (int i = 0; i < distance.Length; i++) distance[i] = pad + 1f;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (source[y * width + x].a > .15f) distance[(y + pad) * pixels.W + x + pad] = 0f;

            // Measure outward from the silhouette in both directions so the glow follows its shape.
            void Visit(int x, int y, int nx, int ny, float step)
            {
                if (nx < 0 || ny < 0 || nx >= pixels.W || ny >= pixels.H) return;
                int i = y * pixels.W + x;
                distance[i] = Mathf.Min(distance[i], distance[ny * pixels.W + nx] + step);
            }
            for (int direction = 1; direction >= -1; direction -= 2)
            for (int row = 0; row < pixels.H; row++)
            for (int col = 0; col < pixels.W; col++)
            {
                int x = direction > 0 ? col : pixels.W - 1 - col;
                int y = direction > 0 ? row : pixels.H - 1 - row;
                Visit(x, y, x - direction, y, 1f);
                Visit(x, y, x, y - direction, 1f);
                Visit(x, y, x - 1, y - direction, 1.4142f);
                Visit(x, y, x + 1, y - direction, 1.4142f);
            }
            for (int y = 0; y < pixels.H; y++)
            for (int x = 0; x < pixels.W; x++)
            {
                float d = distance[y * pixels.W + x];
                if (d <= 0f || d >= pad) continue;
                float fade = 1f - Mathf.InverseLerp(rim, pad, d);
                pixels.Set(x, y, new Color(1f, 1f, 1f, fade * fade));
            }

            var pivot = new Vector2((item.pivot.x + pad) / pixels.W, (item.pivot.y + pad) / pixels.H);
            var sprite = pixels.ToSprite(pivot, item.pixelsPerUnit);
            sprite.texture.filterMode = FilterMode.Bilinear;
            return sprite;
        });

        public static Sprite KestrelSkySpeck(bool ice) => Cached(ice ? "kestrel/ice-flake" : "kestrel/twinkle", () =>
        {
            const int size = 32;
            var pixels = new PixelCanvas(size, size);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs((x + .5f) / size * 2f - 1f);
                float dy = Mathf.Abs((y + .5f) / size * 2f - 1f);
                float core = Mathf.Exp(-(dx * dx + dy * dy) * (ice ? 13f : 22f));
                float arms = ice ? 0f : Mathf.Max(
                    Mathf.Exp(-dx * dx * 170f) * Mathf.Pow(1f - dy, 3f),
                    Mathf.Exp(-dy * dy * 170f) * Mathf.Pow(1f - dx, 3f)) * .62f;
                pixels.Set(x, y, new Color(1f, 1f, 1f, Mathf.Max(core, arms)));
            }
            var sprite = pixels.ToSprite(new Vector2(.5f, .5f), ice ? 180f : 116f);
            sprite.texture.filterMode = FilterMode.Bilinear;
            return sprite;
        });

        public static Sprite KestrelSpace() => Cached("kestrel/space", () =>
        {
            const int width = 960, height = 600;
            var c = new PixelCanvas(width, height);
            var voidColor = PixelCanvas.Hex("#070a18");
            var cloud = PixelCanvas.Hex("#263b73");
            var dust = PixelCanvas.Hex("#573b70");
            var glow = PixelCanvas.Hex("#c47449");
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float sweep = 760 + 46 * Mathf.Sin((y - 160) * .015f) + (y - 300) * .11f;
                float band = Mathf.Clamp01(1 - Mathf.Abs(x - sweep) / 137);
                float cloudShape = Mathf.PerlinNoise(x * .015f + 3, y * .012f + 21);
                float grain = Mathf.PerlinNoise(x * .063f + 24, y * .060f + 13);
                float density = Mathf.Clamp01((cloudShape * .78f + grain * .22f - .30f) * band * 2.75f);
                float heat = Mathf.Clamp01((Mathf.PerlinNoise(x * .033f + 17, y * .027f + 2) - .57f) * 3.6f);
                var hue = Color.Lerp(Color.Lerp(cloud, dust, grain), glow, heat * heat);
                c.Set(x, y, Color.Lerp(voidColor, hue, density));
            }

            var rng = new System.Random(2177);
            for (int i = 0; i < 1920; i++)
            {
                int x = rng.Next(width), y = rng.Next(height);
                c.Set(x, y, i % 23 == 0 ? KRim : i % 17 == 0 ? KAmber : i % 6 == 0 ? KLight : KSteel);
                if (i % 137 != 0) continue;
                c.HLine(x - 3, x + 3, y, KSteel);
                c.VLine(x, y - 3, y + 3, KSteel);
                c.HLine(x - 1, x + 1, y, KRim);
                c.VLine(x, y - 1, y + 1, KRim);
                c.Set(x, y, Pal.White);
            }

            const int cx = 751, cy = 270, radius = 14;
            var ocean = PixelCanvas.Hex("#335985");
            var land = PixelCanvas.Hex("#7c8c67");
            var atmosphere = PixelCanvas.Hex("#78b8cf");
            c.Disc(cx, cy, radius, KShadow);
            for (int y = -radius; y <= radius; y++)
            for (int x = -radius; x <= radius; x++)
            {
                float r2 = (x * x + y * y) / (float)(radius * radius);
                if (r2 > 1) continue;
                float shade = Mathf.Clamp01(Mathf.Sqrt(1 - r2) * .8f + (y - x) / 36f);
                var face = Mathf.PerlinNoise(7 + x * .18f, 8 + y * .18f) > .56f ? land : ocean;
                if (Mathf.PerlinNoise(11 + x * .22f, 1 + y * .40f) > .7f) face = KRim;
                if (r2 > .89f) face = atmosphere;
                c.Set(cx + x, cy + y, Color.Lerp(voidColor, face, .25f + .75f * shade));
            }
            var sprite = c.ToSprite(Vector2.zero, 12);
            sprite.texture.filterMode = FilterMode.Bilinear;
            return sprite;
        });
    }
}
