using UnityEngine;

namespace TitanGate.Game
{
    public static partial class Art
    {
        public static Sprite CrewAction(string id, string action, int phase = 0, bool suit = false)
            => id == "cadet" && !suit && UseBlenderCadet && BlenderAction(action, phase) is Sprite blender ? blender : CrewActionDrawn(id, action, phase, suit);

        static Sprite CrewActionDrawn(string id, string action, int phase, bool suit)
            => Cached($"action-v1-{id}-{action}-{phase % 4}-{suit}", () =>
        {
            var c = new PixelCanvas(64, 80);
            var source = CrewHD(id, action == "sit" ? 2 : 0, -1, suit, false, false);
            bool sit = action == "sit", kneel = action == "plant" || action == "pet" || action == "feed";
            int drop = sit ? 12 : kneel ? 15 : 0;
            for (int y = 27; y < 80; y++) for (int x = 0; x < 64; x++)
            {
                if (y < 46 && (x < 23 || x > 43)) continue;
                var px = source.texture.GetPixel(x, y); if (px.a > 0) c.Set(x, y - drop, px);
            }
            var cloth = suit ? Pal.White : id == "mentor" ? PixelCanvas.Hex("#537da1") : id == "aya" ? PixelCanvas.Hex("#90c9b7") : id == "jules" ? PixelCanvas.Hex("#b7a9d9") : PixelCanvas.Hex("#e5a365");
            var skin = PixelCanvas.Hex("#edbb93");
            if (sit)
            {
                c.RoundRect(25, 16, 23, 7, cloth); c.Rect(42, 5, 7, 16, cloth);
                c.RoundRect(41, 2, 14, 6, Pal.Ink); c.HLine(43, 53, 3, Pal.White);
            }
            else if (kneel)
            {
                c.RoundRect(24, 3, 10, 15, cloth); c.RoundRect(33, 3, 15, 6, cloth);
                c.Rect(22, 1, 13, 4, Pal.Ink); c.Rect(43, 1, 9, 4, Pal.Ink);
            }
            else
            {
                c.Rect(25, 4, 8, 25, cloth); c.Rect(36, 4, 8, 25, cloth);
                c.RoundRect(23, 1, 11, 6, Pal.Ink); c.RoundRect(35, 1, 12, 6, Pal.Ink);
                if (action == "carry")
                {
                    var gait = CrewHD(id, 0, phase * 2, suit, false, false);
                    for (int y = 0; y < 27; y++) for (int x = 0; x < 64; x++) c.Set(x, y, gait.texture.GetPixel(x, y));
                }
            }
            int handY = 33 - drop + phase % 2 * 2;
            c.Line(24, 42 - drop, 18, handY, cloth); c.Line(25, 42 - drop, 19, handY, cloth);
            c.Line(42, 42 - drop, 48, handY, cloth); c.Line(41, 42 - drop, 47, handY, cloth);
            c.RoundRect(15, handY - 2, 7, 6, skin); c.RoundRect(45, handY - 2, 7, 6, skin);
            if (action == "water")
            {
                c.RoundRect(40, 22, 15, 11, Pal.Blue); c.Frame(38, 28, 7, 9, Pal.Ice);
                c.Line(53, 28, 60, 24, Pal.Ice); c.Rect(58, 20, 4, 4, Pal.Ice);
                for (int i = 0; i < 4; i++) c.Rect(56 - i * 3, 13 - (i + phase) % 4 * 3, 2, 3, Pal.Cyan);
            }
            if (action == "repair")
            {
                c.Line(47, 30, 51 + phase % 2 * 3, 44, Pal.Grey);
                c.Frame(48 + phase % 2 * 3, 43, 7, 6, Pal.Ice); c.Rect(50 + phase % 2 * 3, 46, 3, 4, new Color32(0,0,0,0));
                if (phase % 2 == 0) { c.HLine(54, 59, 37, Pal.Gold); c.VLine(57, 35, 40, Pal.Gold); }
            }
            if (action == "plant") { c.RoundRect(43, 8, 12, 9, Pal.RustD); c.VLine(48, 14, 24, Pal.Green); c.Disc(45, 20, 3, Pal.Green); c.Disc(51, 23, 3, Pal.Green); }
            if (action == "feed") { c.RoundRect(41, 3, 18, 6, Pal.Grey); c.Disc(47, 9, 2, Pal.Gold); c.Disc(52, 8 + phase % 2, 2, Pal.Rust); c.Rect(43, 16, 8, 8, Pal.Gold); }
            if (action == "pet") { c.Line(46, handY, 53, 13 + phase % 2 * 3, skin); c.Disc(53, 13 + phase % 2 * 3, 3, skin); }
            if (action == "carry" || action == "lift")
            {
                c.RoundRect(18, 22 + phase % 2, 34, 18, Pal.RustD); c.Frame(20, 24 + phase % 2, 30, 14, Pal.Gold);
                c.HLine(21, 49, 31 + phase % 2, Pal.Rust); c.Rect(16, 27, 7, 6, skin); c.Rect(47, 27, 7, 6, skin);
            }
            c.Outline(Pal.Ink); return c.ToSprite(new Vector2(.5f, 0), 44);
        });

        public static Sprite PetCommandIcon(string id) => Cached("pet-command-" + id, () =>
        {
            var c = new PixelCanvas(36, 36); c.RoundRect(1, 1, 34, 34, Pal.Ink); c.Frame(2, 2, 32, 32, Pal.Gold);
            var face = PetFrame(id, -1, 0);
            for (int y = 9; y < 32; y++) for (int x = 0; x < 26; x++)
            { var px = face.texture.GetPixel(x, y); if (px.a > 0) c.Set(x + 5, y - 3, px); }
            return c.ToSprite(new Vector2(.5f, 0), 24);
        });

        public static Sprite PetRest(string id) => Cached("pet-rest-" + id, () =>
        {
            var c = new PixelCanvas(26, 32); var face = PetFrame(id, -1, 0);
            for (int y = 10; y < 32; y++) for (int x = 0; x < 26; x++)
            { var px = face.texture.GetPixel(x, y); if (px.a > 0) c.Set(x, y - 2, px); }
            var fur = id == "comet" ? PixelCanvas.Hex("#dc9b66") : id == "pixel" ? PetGrey : Pal.White;
            c.RoundRect(5, 1, 17, 10, fur); c.Rect(6, 1, 5, 3, Pal.Ink); c.Rect(16, 1, 5, 3, Pal.Ink);
            c.HLine(9, 17, 8, Pal.CyanD); c.Outline(Pal.Ink); return c.ToSprite(new Vector2(.5f, 0), 20);
        });
    }
}
