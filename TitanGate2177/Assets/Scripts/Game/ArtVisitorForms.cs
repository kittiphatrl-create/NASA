using UnityEngine;

namespace TitanGate.Game
{
    public static partial class Art
    {
        public static Sprite VisitorForm(string id, int dir, int frame, bool wave) => Cached($"visitor-form-{id}-{dir}-{frame}-{wave}", () =>
        {
            var c = new PixelCanvas(64, 80); int f = Mathf.Max(0, frame), bounce = frame < 0 ? 0 : f % 4 < 2 ? 2 : 0;
            bool back = dir == 1, side = dir == 2;
            if (id == "tik" || id == "pip")
            {
                bool tiny = id == "pip"; int h = tiny ? 29 : 40, x = tiny ? 15 : 7, w = tiny ? 34 : 49;
                var gel = PixelCanvas.Hex(tiny ? "#f3b3d2" : "#8de0c5");
                c.RoundRect(x, 3 + bounce, w, h - bounce, gel); c.Disc(x + w / 2, h - 1, w / 3, gel);
                c.RoundRect(x + 5, 14, 6, h - 20, Pal.Ice); c.Rect(x + 8, 12, 3, 5, Pal.White);
                c.RoundRect(x + w - 12, 4, 13, 18, Pal.RustD); c.Rect(x + w - 10, 15, 9, 3, Pal.Gold);
                if (!back) { int eye = side ? x + w - 18 : x + 12; c.Rect(eye, 23, 4, 6, Pal.Ink); if (!side) c.Rect(eye + 16, 23, 4, 6, Pal.Ink); c.HLine(eye + 4, eye + 9, 18, Pal.Ink); }
                if (wave) { c.Line(x + w - 2, 19, 59, 36 + f % 2 * 4, gel); c.Disc(59, 36 + f % 2 * 4, 3, gel); }
                c.Disc(x + w / 2, h + 17, 3, Pal.Gold); c.VLine(x + w / 2, h + 8, h + 15, gel);
            }
            else if (id == "mu")
            {
                // Aquatic explorer: a water vessel on a tracked support, not humanoid legs.
                c.RoundRect(10, 2, 44, 10, Pal.Ink); c.Rect(14 + f % 3, 4, 5, 5, Pal.Grey); c.Rect(36 + f % 3, 4, 5, 5, Pal.Grey);
                c.RoundRect(13, 12, 38, 48, Pal.Grey); c.RoundRect(17, 17, 30, 38, Pal.BlueD); c.RoundRect(19, 19, 26, 29, Pal.CyanD);
                c.Disc(32, 36 + bounce, 10, PixelCanvas.Hex("#bcbded")); c.Disc(24, 30 + bounce, 6, PixelCanvas.Hex("#bcbded"));
                if (!back) { c.Rect(side ? 36 : 27, 38 + bounce, 3, 4, Pal.Ink); if (!side) c.Rect(36, 38 + bounce, 3, 4, Pal.Ink); }
                c.Frame(19, 57, 26, 5, Pal.Gold); c.Rect(5, 22, 7, 17, Pal.Grey); c.Rect(52, wave ? 35 : 22, 7, 17, Pal.Grey);
                for (int i = 0; i < 3; i++) c.Frame(20 + i * 8, 22 + (f * 4 + i * 9) % 30, 3, 3, Pal.Ice);
                c.VLine(21, 25, 48, Pal.White);
            }
            else
            {
                // Nok: salvaged service robot with antenna and a wheeled base.
                c.RoundRect(16, 2 + bounce, 32, 10, Pal.Ink); c.RoundRect(15, 13 + bounce, 34, 23, Pal.Rust);
                c.RoundRect(9, 37 + bounce, 45, 30, Pal.Grey); c.RoundRect(14, 41 + bounce, 35, 19, Pal.BlueD);
                if (!back) { c.Rect(side ? 38 : 21, 48 + bounce, 5, 6, Pal.Cyan); if (!side) c.Rect(37, 48 + bounce, 5, 6, Pal.Cyan); }
                c.Rect(20, 22 + bounce, 22, 4, Pal.Gold); c.VLine(45, 67 + bounce, 74, Pal.Grey); c.Disc(45, 75, 3, Pal.Gold);
                c.Line(15, 31, 6, 21 + bounce, Pal.Grey); c.Line(49, 31, 58, wave ? 47 : 21 + bounce, Pal.Grey);
            }
            c.Outline(Pal.Ink); return c.ToSprite(new Vector2(.5f, 0), 44);
        });

        public static Sprite StoryHabitat() => Cached("story-habitat", () =>
        {
            var c = new PixelCanvas(192, 80);
            for (int x = 18; x < 175; x += 51) { c.RoundRect(x, 12, 46, 47, Pal.Grey); c.Rect(x + 3, 17, 40, 7, Pal.Gold); c.RoundRect(x + 8, 31, 30, 19, Pal.BlueD); c.HLine(x + 11, x + 32, 46, Pal.Cyan); }
            c.Rect(4, 10, 184, 6, Pal.Ink); c.Rect(68, 19, 12, 24, Pal.Ice); c.Rect(118, 19, 12, 24, Pal.Ice);
            c.VLine(156, 55, 74, Pal.Grey); c.Line(145, 72, 169, 72, Pal.Ice); c.Disc(155, 74, 3, Pal.Cyan);
            c.Outline(Pal.Ink); return c.ToSprite(new Vector2(.5f, 0));
        });
    }
}
