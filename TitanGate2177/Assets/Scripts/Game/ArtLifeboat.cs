// PROTOTYPE - NOT FOR PRODUCTION
// Question: Is Chapter 2 more fun as a read-and-choose survival diary (visitors, a pet, shared rations)
//           than as a walk-and-click outpost?
// Date: 2026-11-12
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>
    /// Pets and visitors for the survival diary, drawn in code like the rest of the game (feet at y = 0).
    /// The monsters are drawn to pass a quick look; only a scan or a careful read of the clues gives them away.
    /// </summary>
    public static partial class Art
    {
        static readonly Color32 TikC = PixelCanvas.Hex("#8fb39a"), TikD = PixelCanvas.Hex("#4f7a63");
        static readonly Color32 MuC = PixelCanvas.Hex("#8a6fb5"), MuD = PixelCanvas.Hex("#57437f");
        static readonly Color32 ZeeC = PixelCanvas.Hex("#4fb3a8"), ZeeD = PixelCanvas.Hex("#2f7a72");
        static readonly Color32 NokC = PixelCanvas.Hex("#a8d98a"), NokD = PixelCanvas.Hex("#6d9e55");
        static readonly Color32 ReeC = PixelCanvas.Hex("#c9c46a"), ReeD = PixelCanvas.Hex("#8c8840");
        static readonly Color32 PetGrey = PixelCanvas.Hex("#8c93a6"), PetPink = PixelCanvas.Hex("#f2a6b4");

        /// <summary>The pet picked before the scramble: comet (dog), pixel (cat) or luna (rabbit).</summary>
        public static Sprite Pet(string id) => PetFrame(id, -1, 0);

        static Sprite LegacyPet(string id) => Prop("pet_" + id, PetW(id), PetH(id), c => DrawPet(c, id));

        public static int PetW(string id) => id == "comet" ? 18 : id == "pixel" ? 16 : 14;
        public static int PetH(string id) => id == "luna" ? 14 : 12;

        static void DrawPet(PixelCanvas c, string id)
        {
            switch (id)
            {
                case "comet": // dog, tail up
                    c.Rect(4, 0, 2, 3, Pal.OrangeD); c.Rect(11, 0, 2, 3, Pal.OrangeD);
                    c.Rect(3, 3, 11, 5, Pal.Orange);
                    c.Rect(12, 5, 5, 5, Pal.Orange); c.Rect(12, 9, 2, 3, Pal.OrangeD);
                    c.Set(15, 8, Pal.Ink); c.Set(17, 7, Pal.Ink);
                    c.Rect(1, 7, 2, 4, Pal.OrangeD);
                    c.HLine(12, 14, 5, Pal.Cyan);
                    break;
                case "pixel": // cat
                    c.Rect(4, 0, 2, 2, PetGrey); c.Rect(9, 0, 2, 2, PetGrey);
                    c.Rect(3, 2, 9, 4, PetGrey);
                    c.Rect(11, 4, 4, 5, PetGrey); c.Set(11, 9, PetGrey); c.Set(14, 9, PetGrey);
                    c.Set(12, 7, Pal.Green); c.Set(14, 7, Pal.Green);
                    c.VLine(1, 3, 9, PetGrey); c.Set(2, 9, PetGrey);
                    c.HLine(4, 10, 4, Pal.Steel);
                    break;
                default: // luna, rabbit
                    c.Rect(3, 0, 7, 6, Pal.White); c.Rect(1, 3, 2, 2, Pal.White);
                    c.Rect(8, 4, 5, 5, Pal.White);
                    c.Rect(8, 9, 2, 5, Pal.White); c.Rect(11, 9, 2, 5, Pal.White);
                    c.VLine(8, 10, 12, PetPink); c.VLine(12, 10, 12, PetPink);
                    c.Set(11, 6, Pal.RedD); c.Set(12, 5, PetPink);
                    break;
            }
        }

        /// <summary>A visitor at the hatch or a guest aboard.</summary>
        public static Sprite Visitor(string id) => Crew(id, 0, -1);

        static Sprite LegacyVisitor(string id) => Prop("visitor_" + id, VisitorW(id), VisitorH(id), c => DrawVisitor(c, id));

        static int VisitorW(string id) => id == "zee" ? 20 : id == "nok" ? 12 : id == "tik" || id == "mimic" ? 14 : 16;
        static int VisitorH(string id) => id == "mu" || id == "shade" ? 30 : id == "ree" ? 24 : id == "nok" ? 16 : id == "zee" ? 18 : 20;

        static void DrawVisitor(PixelCanvas c, string id)
        {
            switch (id)
            {
                case "tik":
                case "mimic": // the copy is the same shape: no blink glint, and a belt with no scratches
                {
                    bool copy = id == "mimic";
                    c.Rect(4, 0, 2, 4, TikD); c.Rect(8, 0, 2, 4, TikD);
                    c.Rect(3, 4, 8, 8, TikC);
                    c.Rect(1, 6, 2, 5, TikC); c.Rect(11, 6, 2, 5, TikC);
                    c.HLine(3, 10, 6, copy ? Pal.Gold : Pal.AmberD);
                    if (!copy) { c.Set(5, 6, Pal.Amber); c.Set(8, 6, Pal.Amber); }
                    c.Set(4, 5, Pal.Steel); c.Set(9, 5, Pal.SteelL);
                    c.Ellipse(7, 15, 4, 4, TikC);
                    c.Rect(4, 14, 2, 3, Pal.Ink); c.Rect(8, 14, 2, 3, Pal.Ink);
                    if (!copy) { c.Set(4, 16, Pal.White); c.Set(8, 16, Pal.White); }
                    break;
                }
                case "mu": // tall, with a full bag that clinks
                    c.Rect(5, 0, 2, 10, MuD); c.Rect(9, 0, 2, 10, MuD);
                    c.Rect(4, 10, 8, 10, MuC);
                    c.Rect(2, 12, 2, 7, MuC); c.Rect(12, 12, 2, 7, MuC);
                    c.Rect(11, 7, 5, 6, Pal.RustD); c.Set(13, 12, Pal.SteelL); c.Set(12, 9, Pal.Steel);
                    c.Ellipse(8, 24, 4, 5, MuC);
                    c.HLine(5, 6, 24, Pal.Amber); c.HLine(9, 10, 24, Pal.Amber);
                    break;
                case "zee": // round trader with a price sign
                    c.Ellipse(9, 8, 7, 7, ZeeC);
                    c.Rect(6, 0, 2, 2, ZeeD); c.Rect(11, 0, 2, 2, ZeeD);
                    c.Set(6, 10, Pal.Ink); c.Set(9, 11, Pal.Ink); c.Set(12, 10, Pal.Ink);
                    c.HLine(7, 11, 6, ZeeD);
                    c.VLine(18, 0, 13, Pal.Steel);
                    c.Rect(15, 12, 5, 6, Pal.White); c.Rect(16, 14, 2, 2, Pal.Amber); c.Set(18, 15, Pal.Ink);
                    break;
                case "nok": // small, one burned arm
                    c.Rect(3, 0, 2, 3, NokD); c.Rect(7, 0, 2, 3, NokD);
                    c.Rect(3, 3, 6, 5, NokC);
                    c.Rect(1, 4, 2, 3, NokC); c.Rect(9, 4, 2, 3, Pal.RedD);
                    c.Ellipse(6, 11, 4, 4, NokC);
                    c.Rect(4, 11, 1, 2, Pal.Ink); c.Rect(7, 11, 1, 2, Pal.Ink); c.Set(4, 12, Pal.White); c.Set(7, 12, Pal.White);
                    break;
                case "shade": // hooded cloak, dark visor that never fogs, dust settled on the shoulders
                    c.Rect(3, 0, 10, 22, Pal.Navy2);
                    c.Rect(2, 0, 1, 6, Pal.Navy1); c.Rect(13, 0, 1, 6, Pal.Navy1);
                    c.VLine(8, 0, 20, Pal.Navy1);
                    c.Ellipse(8, 25, 5, 5, Pal.Navy2);
                    c.Rect(5, 23, 6, 3, Pal.Ink);
                    c.Set(4, 21, Pal.Rust); c.Set(6, 22, Pal.RustL); c.Set(11, 21, Pal.Rust); c.Set(9, 22, Pal.Rust); c.Set(12, 20, Pal.RustL);
                    break;
                case "ree": // gardener with a clear box of plants
                    c.Rect(5, 0, 2, 5, ReeD); c.Rect(9, 0, 2, 5, ReeD);
                    c.Rect(4, 5, 8, 8, ReeC);
                    c.Ellipse(8, 17, 4, 4, ReeC);
                    c.Rect(6, 16, 1, 2, Pal.Ink); c.Rect(9, 16, 1, 2, Pal.Ink); c.Set(6, 17, Pal.White); c.Set(9, 17, Pal.White);
                    c.Rect(8, 7, 8, 5, Pal.Ice);
                    c.VLine(10, 8, 10, Pal.GreenD); c.VLine(13, 8, 9, Pal.GreenD);
                    c.Set(10, 11, Pal.Green); c.Set(9, 10, Pal.Green); c.Set(13, 10, Pal.Green); c.Set(14, 11, Pal.Green);
                    break;
                default:
                    c.Rect(3, 0, 8, 16, Pal.Grey);
                    break;
            }
        }
    }
}
