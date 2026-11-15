using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>Bold, outlined pictograms drawn in a 32-unit grid.</summary>
    static class FieldBookIcons
    {
        public static bool Has(string id) => id is "food" or "o2" or "battery" or "bolt" or "bag" or
            "radio" or "gauge" or "heat" or "co2" or "water" or "gear" or "can" or "flashlight" or "sun" or "moon";

        public static void Draw(Painter2D p, string id, int size)
        {
            float unit = size / 32f, offset = 0;
            Color fill = FieldBook.Amber;
            Vector2 P(float x, float y) => new Vector2((x + offset) * unit, (y + offset * 1.25f) * unit);
            void Move(float x, float y) => p.MoveTo(P(x, y));
            void Line(float x, float y) => p.LineTo(P(x, y));
            void Curve(float a, float b, float c, float d, float x, float y) => p.BezierCurveTo(P(a, b), P(c, d), P(x, y));
            void Start() { p.BeginPath(); p.fillColor = fill; p.strokeColor = FieldBook.Ink; p.lineWidth = 1.9f * unit; }
            void End() { p.ClosePath(); p.Fill(); p.Stroke(); }
            void Circle(float x, float y, float radius, Color color)
            {
                Start();
                p.fillColor = color;
                p.Arc(P(x, y), radius * unit, Angle.Degrees(0), Angle.Degrees(360));
                End();
            }
            void Box(float x, float y, float w, float h, float r)
            {
                Start();
                Move(x + r, y); Line(x + w - r, y); Curve(x + w, y, x + w, y, x + w, y + r);
                Line(x + w, y + h - r); Curve(x + w, y + h, x + w, y + h, x + w - r, y + h);
                Line(x + r, y + h); Curve(x, y + h, x, y + h, x, y + h - r);
                Line(x, y + r); Curve(x, y, x, y, x + r, y);
                End();
            }
            void Stroke(float x, float y, float toX, float toY)
            {
                Start();
                Move(x, y); Line(toX, toY);
                p.Stroke();
            }

            for (int pass = 0; pass < 2; pass++)
            {
                offset = pass == 0 ? 1.1f : 0;
                fill = pass == 0 ? FieldBook.Ink : FieldBook.Amber;
                Color cut = pass == 0 ? FieldBook.Ink : FieldBook.Paper;
                switch (id)
                {
                    case "food":
                        Start(); Move(4, 17); Curve(5, 12, 9, 13, 10, 14);
                        Curve(11, 9, 17, 9, 19, 13); Curve(22, 12, 25, 13, 26, 17); End();
                        Start(); Move(3, 17); Line(28, 17); Curve(27, 23, 24, 24, 21, 26);
                        Line(21, 28); Line(10, 28); Line(10, 26); Curve(6, 24, 4, 22, 3, 17); End();
                        for (int x = 10; x <= 21; x += 5)
                        {
                            Start(); Move(x, 9); Curve(x + 5, 5, x - 3, 6, x + 2, 2); p.Stroke();
                        }
                        break;
                    case "o2":
                        Box(10, 3, 12, 5, 1.5f); Box(7, 8, 18, 21, 5);
                        Circle(16, 19, 4.8f, cut);
                        break;
                    case "battery":
                        Box(12, 2, 8, 5, 1); Box(7, 6, 18, 22, 4);
                        Stroke(8, 11, 24, 11); Stroke(12, 18, 20, 18); Stroke(16, 14, 16, 22);
                        break;
                    case "bolt":
                        Start(); Move(18, 2); Line(4, 18); Line(14, 18); Line(11, 29);
                        Line(27, 12); Line(18, 12); Line(20, 2); End();
                        break;
                    case "bag":
                        Box(11, 4, 10, 8, 2); Box(3, 9, 26, 19, 4);
                        Start(); Move(3, 14); Curve(5, 23, 27, 23, 29, 14); p.Stroke();
                        Box(13, 18, 6, 5, 1);
                        break;
                    case "radio":
                        Box(21, 2, 4, 12, 1); Box(7, 8, 18, 20, 3);
                        fill = cut; Box(11, 12, 10, 5, 1);
                        Circle(12, 22, 1.3f, cut); Circle(20, 22, 1.3f, cut);
                        break;
                    case "gauge":
                        Circle(16, 16, 12, fill);
                        foreach (var tick in new[] { (8, 10, 10, 12), (16, 6, 16, 8), (22, 10, 20, 12), (7, 17, 9, 17), (23, 17, 25, 17) })
                            Stroke(tick.Item1, tick.Item2, tick.Item3, tick.Item4);
                        Stroke(16, 17, 21, 12); Circle(16, 17, 1, FieldBook.Ink);
                        break;
                    case "heat":
                        Start(); Move(10, 18); Line(10, 6); Curve(10, 1, 17, 1, 17, 6);
                        Line(17, 18); Curve(26, 29, 1, 32, 10, 18); End();
                        Stroke(13.5f, 11, 13.5f, 23); Stroke(21, 7, 25, 7); Stroke(21, 13, 25, 13);
                        break;
                    case "co2":
                        Start(); Move(6, 24); Curve(0, 23, 1, 15, 7, 14);
                        Curve(6, 6, 20, 3, 23, 12); Curve(33, 12, 32, 25, 25, 25);
                        Line(6, 25); End();
                        break;
                    case "water":
                        Start(); Move(16, 3); Curve(15, 9, 6, 15, 6, 22);
                        Curve(6, 32, 26, 32, 26, 22); Curve(26, 15, 17, 9, 16, 3); End();
                        if (pass == 1)
                        {
                            Start(); p.strokeColor = FieldBook.Paper;
                            Move(10, 22); Curve(10, 19, 12, 16, 14, 14); p.Stroke();
                        }
                        break;
                    case "gear":
                        Start();
                        for (int i = 0; i < 32; i++)
                        {
                            float radians = i * Mathf.PI / 16;
                            float radius = i % 4 < 2 ? 12.6f : 9.9f;
                            float x = 15.5f + Mathf.Cos(radians) * radius, y = 15.5f + Mathf.Sin(radians) * radius;
                            if (i == 0) Move(x, y); else Line(x, y);
                        }
                        End(); Circle(15.5f, 15.5f, 4.2f, cut);
                        break;
                    case "can":
                        Box(10, 3, 12, 7, 2); Box(6, 8, 20, 20, 4);
                        Stroke(7, 14, 25, 14); Stroke(7, 23, 25, 23);
                        break;
                    case "flashlight":
                        Start(); Move(3, 20); Line(15, 12); Line(17, 7); Line(22, 5); Line(28, 15);
                        Line(23, 18); Line(18, 18); Line(7, 27); End();
                        Stroke(17, 8, 23, 18); Stroke(25, 4, 28, 1); Stroke(28, 8, 31, 7);
                        break;
                    case "sun":
                        Circle(16, 16, 6, fill);
                        for (int i = 0; i < 8; i++)
                        {
                            float a = i * Mathf.PI / 4;
                            Stroke(16 + Mathf.Cos(a) * 9, 16 + Mathf.Sin(a) * 9, 16 + Mathf.Cos(a) * 13, 16 + Mathf.Sin(a) * 13);
                        }
                        break;
                    case "moon":
                        Start(); Move(18, 3); Curve(0, 3, 0, 28, 16, 28);
                        Curve(24, 28, 27, 23, 28, 18); Curve(16, 24, 10, 12, 18, 3); End();
                        break;
                }
            }
        }
    }
}
