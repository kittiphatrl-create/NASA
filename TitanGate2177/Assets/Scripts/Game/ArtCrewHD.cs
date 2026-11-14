using UnityEngine;

namespace TitanGate.Game
{
    public static partial class Art
    {
        public static Sprite TitleReadabilityFade() => Cached("title-fade6", () =>
        {
            var c = new PixelCanvas(64, 8);
            for (int x = 0; x < 64; x++) for (int y = 0; y < 8; y++) c.Set(x, y, new Color(.025f, .045f, .08f, Mathf.Pow(1 - x / 63f, .4f) * .86f));
            var texture = c.ToTexture(); texture.filterMode = FilterMode.Bilinear;
            return Sprite.Create(texture, new Rect(0, 0, 64, 8), new Vector2(.5f, .5f), 16);
        });
        // 64x80 authored pixels, not a resized 32px frame. PPU preserves the original world height.
        public static Sprite CrewHD(string id, int dir, int frame, bool suit, bool run, bool wave)
            => Cached($"crew-hd6-{id}-{dir}-{frame}-{suit}-{run}-{wave}", () =>
        {
            var c = new PixelCanvas(64, 80);
            Color32 ink = PixelCanvas.Hex("#293347"), cream = PixelCanvas.Hex("#fff1d2");
            bool human = id == "cadet" || id == "aya" || id == "jules" || id == "mentor" || id == "zee" || id == "official" || id == "kira" || id == "rex" || id == "vela";
            bool side = dir == 2, back = dir == 1, blink = frame == -2 && id != "mimic";
            var skin = human ? (id == "aya" ? PixelCanvas.Hex("#bc836e") : id == "jules" ? PixelCanvas.Hex("#e4b795") : id == "rex" ? PixelCanvas.Hex("#c98e6a") : id == "vela" ? PixelCanvas.Hex("#e8b896") : id == "kira" ? PixelCanvas.Hex("#f6d2b8") : PixelCanvas.Hex("#edbb93"))
                : id == "bizz" ? PixelCanvas.Hex("#ffd23f") : id == "mu" ? MuC : id == "ree" ? ReeC : id == "zee" ? ZeeC : id == "nok" ? NokC : TikC;
            var cloth = suit ? cream : id == "official" ? PixelCanvas.Hex("#2d3550") : id == "kira" ? PixelCanvas.Hex("#f4a6c8") : id == "rex" ? PixelCanvas.Hex("#36404f") : id == "vela" ? PixelCanvas.Hex("#7a3e6a") : id == "mentor" ? PixelCanvas.Hex("#537da1") : id == "zee" ? PixelCanvas.Hex("#d284b9") : id == "cadet" ? PixelCanvas.Hex("#e5a365") : id == "aya" ? PixelCanvas.Hex("#90c9b7") : id == "jules" ? PixelCanvas.Hex("#b7a9d9") : (Color32)Color.Lerp(skin, cream, .18f);
            Color32 dark = Color.Lerp(cloth, ink, .34f), light = Color.Lerp(cloth, cream, .35f);
            var hair = id == "official" ? PixelCanvas.Hex("#d7d6d0") : id == "kira" ? PixelCanvas.Hex("#ff8fc0") : id == "rex" ? PixelCanvas.Hex("#1f2230") : id == "vela" ? PixelCanvas.Hex("#c8873a") : id == "mentor" ? PixelCanvas.Hex("#b7b8b1") : id == "zee" ? PixelCanvas.Hex("#66466e") : id == "aya" ? PixelCanvas.Hex("#514052") : id == "jules" ? PixelCanvas.Hex("#90664f") : PixelCanvas.Hex("#594334");
            int f = Mathf.Max(0, frame) % 8;
            float phase = f * Mathf.PI / 4;
            int stride = frame < 0 ? 0 : Mathf.RoundToInt(Mathf.Sin(phase) * (run ? 9 : 6));
            int bob = frame < 0 ? 0 : (f == 1 || f == 5 ? -1 : f == 3 || f == 7 ? 1 : 0);
            int flight = run && frame >= 0 && (f == 2 || f == 6) ? 2 : 0;
            void Segment(int ax, int ay, int bx, int by, int thickness, Color32 color)
            {
                for (int dx = -thickness / 2; dx <= thickness / 2; dx++) c.Line(ax + dx, ay, bx + dx, by, color);
            }
            void Leg(int hip, int swing, bool far)
            {
                int lift = frame < 0 ? 0 : Mathf.Max(0, swing) / 2;
                int foot = hip + (side ? swing : 0), knee = hip + (side ? swing / 3 : 0);
                var shade = far ? dark : cloth;
                Segment(hip, 29 + bob + flight, knee, 16 + lift + flight, 6, shade);
                Segment(knee, 16 + lift + flight, foot, 5 + lift + flight, 5, shade);
                c.RoundRect(foot - 3, 2 + lift + flight, side ? 11 : 8, 6, ink);
                c.HLine(foot - 2, foot + (side ? 6 : 3), 3 + lift + flight, cream);
            }
            Leg(side ? 30 : 27, stride, true);
            Leg(side ? 34 : 38, -stride, false);
            int leftHand = 31 + bob + flight + stride / 3, rightHand = wave ? 59 : 31 + bob + flight - stride / 3;
            Segment(side ? 28 : 20, 43 + bob + flight, side ? 27 : 18, leftHand, 6, dark);
            c.RoundRect((side ? 27 : 18) - 3, leftHand - 3, 7, 6, suit ? cream : skin);
            int tx = side ? 24 : 23, tw = side ? 17 : 21;
            c.RoundRect(tx, 26 + bob + flight, tw, 23, cloth);
            c.Rect(tx, 28 + bob + flight, 3, 17, dark); c.HLine(tx + 3, tx + tw - 3, 47 + bob + flight, light);
            c.Rect(tx, 26 + bob + flight, tw, 3, ink); c.Rect(tx + tw / 2 - 2, 27 + bob + flight, 4, 2, Pal.Gold);
            if (back)
            {
                c.RoundRect(25, 31 + bob + flight, 18, 19, ink); c.RoundRect(27, 33 + bob + flight, 14, 15, dark);
                c.Rect(29, 39 + bob + flight, 10, 3, cream); c.Rect(32, 45 + bob + flight, 4, 2, Pal.Cyan);
            }
            else { c.RoundRect(tx + 5, 38 + bob + flight, 8, 7, cream); c.Rect(tx + 6, 40 + bob + flight, 3, 3, Pal.CyanD); }
            Segment(side ? 36 : 44, 43 + bob + flight, side ? 38 + stride / 2 : 47, rightHand, 6, cloth);
            c.RoundRect((side ? 38 + stride / 2 : 47) - 3, rightHand - 3, 7, 6, suit ? cream : skin);
            int hx = side ? 23 : 18, hy = 46 + bob + flight, hw = side ? 28 : 31;
            c.RoundRect(hx, hy, hw, 28, suit ? cream : skin);
            c.Rect(hx, hy + 4, 3, 18, suit ? Pal.Grey : Color.Lerp(skin, ink, .17f));
            if (human && !suit)
            {
                c.RoundRect(hx, hy + 22, hw, 8, hair);
                c.Rect(hx, hy + 10, side ? 9 : 4, 15, hair);
                c.Rect(hx + 4, hy + 21, side ? 17 : 19, 3, hair);
                if (id == "aya" || id == "zee") { c.RoundRect(hx - 4, hy + 3, 7, 22, hair); c.Rect(hx - 2, hy + 15, 4, 2, Pal.Gold); }
                if (id == "mentor" && !back) { c.Rect(hx + 5, hy + 3, hw - 10, 4, hair); c.Rect(hx + 5, hy + 1, 4, 3, hair); }
                if (id == "kira")
                {
                    // twin tails with star clips, a fringe, and a bow on the jacket
                    c.RoundRect(hx - 7, hy + 4, 8, 20, hair); c.RoundRect(hx + hw - 1, hy + 4, 8, 20, hair);
                    c.Disc(hx - 2, hy + 23, 2, Pal.Gold); c.Disc(hx + hw + 2, hy + 23, 2, Pal.Gold);
                    if (!back) c.Rect(hx + 3, hy + 17, hw - 6, 5, hair);
                    if (!back) { c.Rect(tx + tw / 2 - 4, 44 + bob + flight, 8, 3, Pal.Red); }
                }
                if (id == "rex")
                {
                    // swept spiky hair and a jacket collar
                    for (int i = 0; i < 5; i++) c.Rect(hx + 2 + i * 6, hy + 26 + (i % 2), 4, 4, hair);
                    if (!back) c.Rect(hx + 3, hy + 18, hw - 6, 4, hair);
                    c.Rect(tx - 1, 44 + bob + flight, tw + 2, 4, PixelCanvas.Hex("#22293a"));
                    if (!back && !side) { c.Rect(hx + 5, hy + 12, 9, 4, ink); c.Rect(hx + 18, hy + 12, 9, 4, ink); c.HLine(hx + 14, hx + 18, hy + 14, ink); }
                }
                if (id == "vela")
                {
                    // long wavy hair over the shoulders and a gold scarf
                    c.RoundRect(hx - 5, hy - 6, 8, 30, hair); c.RoundRect(hx + hw - 3, hy - 6, 8, 30, hair);
                    if (!back) c.Rect(hx + 3, hy + 19, hw - 10, 4, hair);
                    c.Rect(tx + 2, 45 + bob + flight, tw - 4, 3, Pal.Gold);
                }
                if (id == "official" && !back) { c.Rect(tx + tw / 2 - 1, 30 + bob + flight, 3, 16, Pal.RedD); c.Rect(tx + 3, 40 + bob + flight, 3, 3, Pal.Gold); }
                if (back) c.RoundRect(hx + 1, hy + 1, hw - 2, 26, hair);
            }
            if (suit && !back) c.RoundRect(hx + 4, hy + 6, hw - 7, 15, Pal.BlueD);
            if (!back)
            {
                int eye = side ? hx + 17 : hx + 7;
                c.RoundRect(eye, hy + 11, 4, blink ? 1 : 6, ink);
                if (!blink) c.Set(eye, hy + 16, cream);
                if (!side) { c.RoundRect(eye + 14, hy + 11, 4, blink ? 1 : 6, ink); if (!blink) c.Set(eye + 14, hy + 16, cream); }
                c.Rect(eye - 2, hy + 8, 4, 2, PetPink);
                if (side) { c.Rect(hx + hw - 1, hy + 10, 3, 5, suit ? cream : skin); c.HLine(eye + 4, eye + 7, hy + 6, ink); }
                else { c.Rect(eye + 15, hy + 8, 4, 2, PetPink); c.HLine(hx + 13, hx + 17, hy + 5, ink); if (wave) c.Rect(hx + 14, hy + 3, 3, 2, PetPink); }
            }
            if (!human)
            {
                if (id == "tik" || id == "mimic" || id == "pip") { c.VLine(hx + 7, hy + 29, hy + 33, skin); c.Disc(hx + 7, hy + 34, 2, Pal.Gold); }
                if (id == "mu") { c.RoundRect(43, 23, 14, 16, Pal.RustD); c.HLine(44, 55, 35, Pal.Gold); }
                if (id == "zee" && !back) c.RoundRect(hx + 14, hy + 20, 3, 4, ink);
                if (id == "nok") c.Rect(side ? 36 : 46, 36, 5, 7, cream);
                if (id == "ree") { c.RoundRect(42, 28, 15, 12, Pal.Ice); c.VLine(48, 30, 39, Pal.GreenD); c.HLine(45, 52, 36, Pal.Green); }
                if (id == "bizz")
                {
                    // a round bee-like courier: black stripes, see-through wings, two antennae with pollen balls
                    for (int y = 30; y < 48; y += 6) c.Rect(tx, y + bob + flight, tw, 3, ink);
                    c.Ellipse(hx - 6, hy + 14, 7, 10, new Color32(220, 240, 255, 200)); c.Ellipse(hx + hw + 6, hy + 14, 7, 10, new Color32(220, 240, 255, 200));
                    c.Line(hx + 8, hy + 27, hx + 4, hy + 34, ink); c.Line(hx + hw - 8, hy + 27, hx + hw - 4, hy + 34, ink);
                    c.Disc(hx + 4, hy + 35, 2, Pal.Gold); c.Disc(hx + hw - 4, hy + 35, 2, Pal.Gold);
                }
                if (id == "shade") { c.RoundRect(hx - 1, hy + 19, hw + 2, 11, PixelCanvas.Hex("#8075a9")); c.Rect(hx, hy + 5, 4, 18, PixelCanvas.Hex("#8075a9")); }
            }
            c.Outline(ink);
            return c.ToSprite(new Vector2(.5f, 0), 44);
        });

        public static Sprite ConditionFace(int mood) => Cached("condition-face6-" + mood, () =>
        {
            var c = new PixelCanvas(32, 32);
            var fill = mood == 0 ? PixelCanvas.Hex("#a9dfb1") : mood == 1 ? Pal.Gold : mood == 2 ? PixelCanvas.Hex("#f0b478") : PetPink;
            c.Disc(16, 16, 14, fill); c.Disc(12, 20, 10, Color.Lerp(fill, Pal.White, .12f));
            c.Rect(10, 17, 3, 4, Pal.Ink); c.Rect(21, 17, 3, 4, Pal.Ink);
            c.HLine(12, 21, mood == 0 ? 8 : 10, Pal.Ink);
            c.Set(11, mood == 0 ? 10 : 8, Pal.Ink); c.Set(22, mood == 0 ? 10 : 8, Pal.Ink);
            c.Outline(Pal.Ink); return c.ToSprite(new Vector2(.5f, .5f));
        });

        public static Sprite SprintIcon() => Cached("sprint-icon6", () =>
        {
            var c = new PixelCanvas(20, 20); c.Disc(12, 16, 3, Pal.CyanL);
            c.Line(10, 12, 7, 7, Pal.Cyan); c.Line(11, 12, 8, 7, Pal.Cyan);
            c.Line(8, 8, 13, 5, Pal.Cyan); c.Line(13, 5, 11, 1, Pal.Cyan);
            c.Line(7, 7, 4, 3, Pal.Cyan); c.HLine(1, 4, 3, Pal.Cyan);
            c.Line(10, 12, 15, 9, Pal.Cyan); c.HLine(15, 18, 9, Pal.Cyan);
            c.Line(9, 11, 5, 10, Pal.Cyan); c.VLine(5, 10, 13, Pal.Cyan);
            return c.ToSprite(new Vector2(.5f, .5f));
        });
    }
}
