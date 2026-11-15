using UnityEngine;

namespace TitanGate.Game
{
    // Original, point-filtered pixel art. Shared silhouettes, feet pivots and palette for the whole crew.
    public static partial class Art
    {
        public static Sprite QuietSpace() => Cached("title-space-v4", () =>
        {
            var c = new PixelCanvas(320, 180);
            for (int y = 0; y < 180; y++) for (int x = 0; x < 320; x++)
            {
                float haze = Mathf.Exp(-Mathf.Pow((y - 110 + x * .12f) / 48, 2));
                c.Set(x, y, Color.Lerp(PixelCanvas.Hex("#080f20"), PixelCanvas.Hex("#233a51"), haze * .65f));
            }
            var rng = new System.Random(2177);
            for (int i = 0; i < 85; i++)
            {
                int x = rng.Next(320), y = rng.Next(180);
                c.Set(x, y, i % 4 == 0 ? Pal.Ice : PixelCanvas.Hex("#60798e"));
                if (i % 17 == 0) { c.HLine(x - 1, x + 1, y, Pal.Ice); c.VLine(x, y - 1, y + 1, Pal.Ice); }
            }
            return c.ToSprite(new Vector2(.5f, .5f));
        });

        public static Sprite TitleMars() => Cached("title-mars-v4", () =>
        {
            var c = new PixelCanvas(160, 160);
            for (int y = 0; y < 160; y++) for (int x = 0; x < 160; x++)
            {
                float nx = (x - 80) / 74f, ny = (y - 80) / 74f, r = nx * nx + ny * ny;
                if (r > 1) continue;
                float z = Mathf.Sqrt(1 - r);
                float light = Mathf.Clamp01(-nx * .65f + ny * .30f + z * .62f);
                float terrain = Mathf.Sin(x * .17f + Mathf.Sin(y * .09f) * 4) * Mathf.Sin(y * .23f + x * .06f);
                var color = Color.Lerp(PixelCanvas.Hex("#512d42"), PixelCanvas.Hex("#f1bb86"), light * .85f + terrain * .07f);
                if (ny > .77f && light > .5f) color = Color.Lerp(color, Pal.White, .65f);
                c.Set(x, y, color);
            }
            return c.ToSprite(new Vector2(.5f, .5f));
        });
        public static Sprite Crew(string id, int dir, int frame, bool suit = false, bool run = false, bool wave = false)
            => ModelCrew(id, dir, frame, suit, run, wave)
                ?? (id == "tik" || id == "pip" || id == "mu" || id == "nok"
                    ? VisitorForm(id, dir, frame, wave) : CrewHD(id, dir, frame, suit, run, wave));

        static Sprite LegacyCrew(string id, int dir, int frame, bool suit, bool run, bool wave) => Cached($"crew-v3-{id}-{dir}-{frame}-{suit}-{run}-{wave}", () =>
        {
            var c = new PixelCanvas(32, 40);
            bool human = id == "cadet" || id == "aya" || id == "jules";
            Color32 ink = PixelCanvas.Hex("#29283f"), cream = PixelCanvas.Hex("#fff0ce");
            Color32 skin = id == "aya" ? PixelCanvas.Hex("#b97962") : id == "jules" ? PixelCanvas.Hex("#e9b18c") : Pal.Skin;
            Color32 coat = id == "cadet" ? PixelCanvas.Hex("#eea15f") : id == "aya" ? PixelCanvas.Hex("#8fd6c0") : id == "jules" ? PixelCanvas.Hex("#bea8e9") : id == "ree" ? ReeC : id == "nok" ? NokC : id == "mu" ? MuC : id == "zee" ? ZeeC : TikC;
            if (suit) coat = cream;
            Color32 shade = Color.Lerp(coat, ink, .3f);
            int step = frame < 0 ? 0 : new[] { 0, 1, 2, 0, -1, -2 }[frame % 6];
            int bob = frame >= 0 && frame % 3 == 1 ? 1 : 0;
            int lift = run ? 3 : 2;
            // Sturdy boots and short legs; big, readable head.
            c.RoundRect(9, 1 + (step > 0 ? lift : 0), 6, 9, ink);
            c.RoundRect(18, 1 + (step < 0 ? lift : 0), 6, 9, ink);
            c.Rect(10, 5 + (step > 0 ? lift : 0), 4, 5, shade);
            c.Rect(19, 5 + (step < 0 ? lift : 0), 4, 5, coat);
            c.HLine(10, 13, 2 + (step > 0 ? lift : 0), cream);
            c.HLine(19, 22, 2 + (step < 0 ? lift : 0), cream);
            c.RoundRect(8, 10 + bob, 17, 13, coat);
            c.Rect(8, 11 + bob, 3, 10, shade);
            c.HLine(11, 23, 21 + bob, cream);
            c.Rect(9, 10 + bob, 15, 3, ink);
            c.Rect(15, 11 + bob, 4, 2, Pal.Gold);
            int arm = wave ? 7 : step * 2;
            c.RoundRect(4, 12 + bob + Mathf.Max(0, -arm), 5, 9, shade);
            c.RoundRect(24, 12 + bob + Mathf.Max(0, arm), 5, 9, coat);
            c.Rect(5, 11 + bob + Mathf.Max(0, -arm), 4, 3, human && !suit ? skin : cream);
            c.Rect(25, 11 + bob + Mathf.Max(0, arm), 4, 3, human && !suit ? skin : cream);
            if (dir == 1)
            {
                c.RoundRect(11, 13 + bob, 12, 11, ink);
                c.RoundRect(12, 15 + bob, 10, 8, shade);
                c.Rect(14, 18 + bob, 6, 2, cream); c.Set(18, 21 + bob, Pal.Cyan);
            }
            else { c.Rect(12, 16 + bob, 5, 4, cream); c.Set(20, 19 + bob, Pal.Cyan); }
            Color32 head = human ? skin : coat;
            c.RoundRect(6, 23 + bob, 22, 15, suit ? cream : head);
            c.Rect(6, 24 + bob, 3, 11, suit ? Pal.Grey : Color.Lerp(head, ink, .18f));
            if (human && !suit)
            {
                var hair = id == "aya" ? PixelCanvas.Hex("#473047") : id == "jules" ? PixelCanvas.Hex("#a76c53") : Pal.Hair;
                c.RoundRect(6, 33 + bob, 22, 6, hair);
                c.Rect(6, 27 + bob, 3, 9, hair);
                c.Rect(23, 30 + bob, 5, 6, hair);
                if (id == "aya") { c.RoundRect(25, 25 + bob, 5, 10, hair); c.Rect(26, 31 + bob, 3, 2, Pal.Gold); }
                if (dir == 1) c.RoundRect(7, 25 + bob, 20, 12, hair);
            }
            if (dir != 1)
            {
                if (suit) c.RoundRect(9, 26 + bob, 16, 8, Pal.BlueD);
                bool blink = frame == -2 && id != "mimic";
                int ex = dir == 2 ? 17 : 11;
                c.Rect(ex, 28 + bob, 3, blink ? 1 : 4, ink);
                if (dir != 2) c.Rect(ex + 8, 28 + bob, 3, blink ? 1 : 4, ink);
                if (!blink) { c.Set(ex, 31 + bob, cream); if (dir != 2) c.Set(ex + 8, 31 + bob, cream); }
                c.Rect(ex - 1, 26 + bob, 3, 1, PetPink);
                c.Rect(Mathf.Min(26, ex + 8), 26 + bob, 3, 1, PetPink);
                c.HLine(16, 18, 25 + bob, ink);
            }
            if (!human && id != "shade") { c.Set(9, 38, coat); c.Set(24, 38, coat); }
            if (id == "shade") { c.RoundRect(7, 24, 20, 14, Pal.Navy1); c.Rect(10, 27, 14, 5, ink); c.Set(12, 29, Pal.Ice); }
            if (id == "ree") { c.Rect(21, 9, 9, 8, Pal.Ice); c.VLine(25, 11, 16, Pal.GreenD); c.HLine(23, 27, 14, Pal.Green); }
            if (id == "nok") c.Rect(25, 14, 3, 5, Pal.RedD);
            if (id == "mu") { c.RoundRect(22, 7, 9, 10, Pal.RustD); c.HLine(23, 29, 14, Pal.Gold); }
            if (id == "zee" && dir != 1) { c.Rect(16, 33 + bob, 3, 3, ink); c.Set(16, 35 + bob, cream); }
            if (dir == 2)
            {
                var side = new PixelCanvas(32, 40);
                for (int y = 0; y < 40; y++) for (int x = 0; x < 32; x++)
                {
                    var pixel = c.Get(x, y);
                    if (pixel.a > 0) side.Set(16 + Mathf.RoundToInt((x - 16) * .73f), y, pixel);
                }
                c = side;
            }
            c.Outline(ink);
            return c.ToSprite(new Vector2(.5f, 0), 22);
        });

        public static Sprite CrewPortrait(int mood) => Cached("crew-portrait-hd6-" + mood, () =>
        {
            var c = new PixelCanvas(48, 48); c.Fill(PixelCanvas.Hex("#213c4a"));
            var sp = Crew("cadet", 0, -1);
            for (int y = 39; y < 80; y++) for (int x = 10; x < 56; x++)
            {
                var pixel = sp.texture.GetPixel(x, y);
                if (pixel.a > 0) c.Set(x - 9, y - 36, pixel);
            }
            return c.ToSprite(new Vector2(.5f, .5f));
        });

        public static Sprite PetFrame(string id, int frame, int dir)
        {
            string key = frame == -1 ? "idle" : frame == -2 ? "blink" : "w" + (Mathf.Max(0, frame) % 8);
            return ModelPet(id, $"d{Mathf.Clamp(dir, 0, 2)}_{key}") ?? DrawPetFrame(id, frame, dir);
        }

        static Sprite DrawPetFrame(string id, int frame, int dir) => Cached($"pet-v3-{id}-{frame}-{dir}", () =>
        {
            var c = new PixelCanvas(26, 32);
            Color32 fur = id == "comet" ? PixelCanvas.Hex("#dc9b66") : id == "pixel" ? PetGrey : Pal.White;
            Color32 dark = Color.Lerp(fur, Pal.Ink, .3f);
            int f = Mathf.Max(0, frame), step = frame < 0 ? 0 : f % 2;
            c.RoundRect(5, 5, 16, 10, fur);
            c.Rect(6, 1 + step, 4, 6, dark); c.Rect(17, 2 - step, 4, 6, dark);
            c.RoundRect(11, 10, 13, 11, fur);
            if (id == "luna") { c.Rect(12, 19, 3, 10, fur); c.Rect(20, 19, 3, 10, fur); c.Set(13, 22, PetPink); }
            else { c.Rect(11, 17, 3, 6, dark); c.Rect(21, 17, 3, 6, dark); }
            c.Rect(2, 10 + f % 3, 4, 4, fur);
            c.Rect(12, 10, 11, 2, Pal.CyanD); c.Set(18, 11, Pal.Gold);
            if (dir != 1)
            {
                c.Rect(14, 16, 2, frame == -2 ? 1 : 3, Pal.Ink); c.Rect(21, 16, 2, frame == -2 ? 1 : 3, Pal.Ink);
                c.Set(14, 18, Pal.White); c.Set(21, 18, Pal.White);
                c.RoundRect(16, 12, 6, 4, Pal.White); c.Rect(18, 14, 2, 1, Pal.Ink);
            }
            if (dir != 2)
            {
                c = new PixelCanvas(26, 32);
                c.RoundRect(6, 3, 14, 13, fur);
                c.Rect(6, 1 + step, 4, 5, dark); c.Rect(16, 2 - step, 4, 5, dark);
                c.RoundRect(4, 10, 18, 12, fur);
                c.Rect(4, id == "luna" ? 20 : 15, 3, id == "luna" ? 11 : 6, dark); c.Rect(19, id == "luna" ? 20 : 15, 3, id == "luna" ? 11 : 6, dark);
                if (id == "luna") { c.VLine(5, 23, 29, PetPink); c.VLine(20, 23, 29, PetPink); }
                c.HLine(7, 18, 10, Pal.CyanD);
                if (dir == 0)
                {
                    c.Rect(8, 15, 2, frame == -2 ? 1 : 3, Pal.Ink); c.Rect(17, 15, 2, frame == -2 ? 1 : 3, Pal.Ink);
                    c.RoundRect(10, 11, 7, 4, Pal.White); c.Rect(12, 14, 3, 1, Pal.Ink);
                }
                else c.Rect(11 + f % 2, 7, 4, 3, Pal.White);
            }
            c.Outline(Pal.Ink); return c.ToSprite(new Vector2(.5f, 0), 20);
        });

        public static Sprite TalkBubble() => Cached("talk-bubble-v3", () =>
        {
            var c = new PixelCanvas(20, 17);
            c.RoundRect(1, 4, 18, 12, Pal.White); c.Rect(5, 1, 3, 4, Pal.White);
            c.Rect(5, 9, 2, 2, Pal.Navy1); c.Rect(9, 9, 2, 2, Pal.Navy1); c.Rect(13, 9, 2, 2, Pal.Navy1);
            c.Outline(Pal.Ink); return c.ToSprite();
        });

        public static Sprite HabitatMat(bool green) => Cached("habitat-mat" + green, () =>
        {
            var c = new PixelCanvas(64, 32);
            var baseC = PixelCanvas.Hex(green ? "#385e60" : "#6d5267");
            c.RoundRect(1, 1, 62, 30, baseC);
            c.Frame(3, 3, 58, 26, PixelCanvas.Hex(green ? "#80aba0" : "#b58d94"));
            for (int i = 7; i < 59; i += 4) c.VLine(i, 5, 26, Color.Lerp(baseC, Pal.White, .06f));
            return c.ToSprite(new Vector2(.5f, .5f));
        });

        public static Sprite HabitatPlant() => Cached("habitat-plant", () =>
        {
            var c = new PixelCanvas(20, 32);
            c.RoundRect(4, 1, 13, 10, PixelCanvas.Hex("#dba07c"));
            c.Rect(4, 2, 3, 8, PixelCanvas.Hex("#996c70")); c.HLine(4, 16, 9, Pal.White);
            c.VLine(10, 10, 26, Pal.GreenD);
            for (int i = 0; i < 3; i++)
            {
                c.Ellipse(6, 14 + i * 5, 4, 2, PixelCanvas.Hex("#7bb08b"));
                c.Ellipse(14, 17 + i * 5, 4, 2, PixelCanvas.Hex("#abd697"));
            }
            c.Outline(Pal.Ink); return c.ToSprite();
        });
    }
}
