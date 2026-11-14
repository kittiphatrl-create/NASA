using UnityEngine;

namespace TitanGate.Game
{
    public static partial class Art
    {
        public static Sprite RepairBackplate(string type) => Cached($"repair-surface-{HabitatTheme}-{type}", () =>
        {
            var c = new PixelCanvas(550, 310); c.Fill(HabitatInk);
            HBPanel(c, 1, 1, 548, 308, HabitatShade);
            HBPanel(c, 10, 12, 530, 286, PixelCanvas.Hex("#283748"));
            // Offset light bands and an inset service tray give depth without hiding controls.
            c.Rect(14, 16, 522, 6, HabitatInk); c.Rect(14, 290, 522, 4, HabitatMetal);
            c.HLine(15, 534, 287, PixelCanvas.Hex("#40556c"));
            for (int x = 24; x < 530; x += 125) { HBBolt(c, x, 301); HBBolt(c, x, 7); }
            for (int x = 32; x < 145; x += 12) { c.Rect(x, 275, 7, 3, HabitatInk); c.HLine(x, x + 5, 278, HabitatMetal); }
            c.Rect(444, 272, 74, 12, HabitatInk);
            for (int x = 449; x < 514; x += 10) c.Rect(x, 275, 5, 5, x < 470 ? HabitatCyan : HabitatShade);
            c.Rect(183, 291, 184, 9, HabitatInk); c.Rect(188, 293, 174, 4, HabitatAmber);
            for (int i = 0; i < 13; i++) { int x = 31 + i * 39 % 490, y = 28 + i * 47 % 230; c.HLine(x, x + 4, y, PixelCanvas.Hex("#334456")); }
            if (type == "wires")
            { HBPanel(c, 76, 34, 61, 225, HabitatMetal); HBPanel(c, 413, 34, 61, 225, HabitatMetal); }
            if (type == "tune" || type == "crank")
            { c.Rect(202, 41, 146, 12, HabitatInk); c.HLine(207, 341, 49, HabitatMetal); }
            return c.ToSprite(new Vector2(.5f, .5f), 1);
        });
    }
}
