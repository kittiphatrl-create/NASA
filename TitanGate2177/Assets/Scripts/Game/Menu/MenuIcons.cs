// Single-weight line icons for the main menu, drawn on a 24-unit grid and rasterised once
// with anti-aliased distance fields, so they stay crisp at 24 px and at 4K.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Game.Menu
{
    public static class MenuIcons
    {
        const int Res = 96;            // texture size; displayed at 24-48 px
        const float Stroke = 1.6f;     // stroke width in grid units (of 24)

        static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        abstract class Prim { public abstract float Dist(Vector2 p); }
        sealed class Seg : Prim
        {
            readonly Vector2 a, b; public Seg(Vector2 a, Vector2 b) { this.a = a; this.b = b; }
            public override float Dist(Vector2 p) { var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-5f, ab.sqrMagnitude)); return (p - (a + ab * t)).magnitude; }
        }
        sealed class Arc : Prim
        {
            readonly Vector2 c; readonly float r, a0, a1; readonly Vector2 s;
            /// <summary>Angles in degrees, counter-clockwise from +x, y up. s squashes the circle into an ellipse.</summary>
            public Arc(Vector2 c, float r, float a0 = 0, float a1 = 360, Vector2? squash = null) { this.c = c; this.r = r; this.a0 = a0; this.a1 = a1; s = squash ?? Vector2.one; }
            public override float Dist(Vector2 p)
            {
                var q = new Vector2((p.x - c.x) / s.x, (p.y - c.y) / s.y);
                float ang = Mathf.Atan2(q.y, q.x) * Mathf.Rad2Deg; if (ang < a0) ang += 360; if (ang > a0 + 360) ang -= 360;
                float minS = Mathf.Min(s.x, s.y);
                if (a1 - a0 >= 359.9f || (ang >= a0 && ang <= a1)) return Mathf.Abs(q.magnitude - r) * minS;
                Vector2 P(float deg) => c + Vector2.Scale(new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad)) * r, s);
                return Mathf.Min((p - P(a0)).magnitude, (p - P(a1)).magnitude);
            }
        }
        sealed class Dot : Prim
        {
            readonly Vector2 c; readonly float r; public Dot(Vector2 c, float r) { this.c = c; this.r = r; }
            public override float Dist(Vector2 p) => Mathf.Max(0, (p - c).magnitude - r);
        }

        // Grid coordinates: x right, y DOWN (like SVG), 0..24. Converted when drawing.
        static Vector2 V(float x, float y) => new Vector2(x, y);
        static IEnumerable<Prim> Poly(bool closed, params float[] xy)
        {
            int n = xy.Length / 2;
            for (int i = 0; i < n - 1; i++) yield return new Seg(V(xy[i * 2], xy[i * 2 + 1]), V(xy[i * 2 + 2], xy[i * 2 + 3]));
            if (closed) yield return new Seg(V(xy[n * 2 - 2], xy[n * 2 - 1]), V(xy[0], xy[1]));
        }
        // Arcs in SVG space: angles measured with y down, so flip the sign to keep "counter-clockwise on screen".
        static Arc A(float cx, float cy, float r, float a0 = 0, float a1 = 360, float sx = 1, float sy = 1) => new Arc(V(cx, cy), r, a0, a1, new Vector2(sx, sy));

        static List<Prim> Shape(string id)
        {
            var p = new List<Prim>();
            switch (id)
            {
                case "play": p.AddRange(Poly(true, 8, 5, 19, 12, 8, 19)); break;
                case "resume": p.AddRange(Poly(true, 9, 5, 19, 12, 9, 19)); p.Add(new Seg(V(5, 5), V(5, 19))); break;
                case "log":   // a mission notebook
                    p.AddRange(Poly(true, 6, 3, 19, 3, 19, 21, 6, 21)); p.Add(new Seg(V(9, 3), V(9, 21)));
                    p.Add(new Seg(V(12, 8), V(16, 8))); p.Add(new Seg(V(12, 12), V(16, 12))); break;
                case "settings":
                    p.Add(A(12, 12, 3.2f));
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; p.Add(new Seg(V(12 + Mathf.Cos(a) * 6.2f, 12 + Mathf.Sin(a) * 6.2f), V(12 + Mathf.Cos(a) * 9f, 12 + Mathf.Sin(a) * 9f))); }
                    p.Add(A(12, 12, 6.2f)); break;
                case "codex":   // planet with a ring
                    p.Add(A(12, 12, 5.5f)); p.Add(A(12, 12, 10.5f, 0, 360, 1, 0.3f)); break;
                case "sources":   // dish antenna
                    p.Add(A(10, 12, 7, 45, 225)); p.Add(new Seg(V(5, 7), V(15, 17))); p.Add(new Seg(V(10, 12), V(17, 5)));
                    p.Add(new Dot(V(18, 4), 1.3f)); p.Add(new Seg(V(8, 19), V(13, 19))); break;
                case "credits":   // five-point star outline
                {
                    var pts = new List<float>();
                    for (int i = 0; i < 10; i++) { float r = i % 2 == 0 ? 9.5f : 4.2f; float a = -Mathf.PI / 2 + i * Mathf.PI / 5; pts.Add(12 + Mathf.Cos(a) * r); pts.Add(12.6f + Mathf.Sin(a) * r); }
                    p.AddRange(Poly(true, pts.ToArray())); break;
                }
                case "quit":
                    p.AddRange(Poly(false, 13, 4, 5, 4, 5, 20, 13, 20)); p.Add(new Seg(V(10, 12), V(21, 12)));
                    p.AddRange(Poly(false, 17, 8, 21, 12, 17, 16)); break;
                case "globe":
                    p.Add(A(12, 12, 9)); p.Add(A(12, 12, 9, 0, 360, 0.42f, 1)); p.Add(new Seg(V(3, 12), V(21, 12)));
                    p.Add(new Seg(V(5, 7.5f), V(19, 7.5f))); p.Add(new Seg(V(5, 16.5f), V(19, 16.5f))); break;
                case "sound":
                    p.AddRange(Poly(true, 4, 9, 8, 9, 13, 5, 13, 19, 8, 15, 4, 15)); p.Add(A(13, 12, 4, -50, 50)); p.Add(A(13, 12, 7.5f, -50, 50)); break;
                case "mute":
                    p.AddRange(Poly(true, 4, 9, 8, 9, 13, 5, 13, 19, 8, 15, 4, 15)); p.Add(new Seg(V(16, 9), V(21, 15))); p.Add(new Seg(V(21, 9), V(16, 15))); break;
                case "lock":
                    p.AddRange(Poly(true, 5, 11, 19, 11, 19, 21, 5, 21)); p.Add(A(12, 11, 4.5f, 180, 360)); p.Add(new Dot(V(12, 16), 1.2f)); break;
                case "sparkle":   // effects quality
                    p.Add(new Seg(V(12, 3), V(12, 21))); p.Add(new Seg(V(3, 12), V(21, 12)));
                    p.Add(new Seg(V(7, 7), V(17, 17))); p.Add(new Seg(V(17, 7), V(7, 17))); p.Add(A(12, 12, 3)); break;
                case "sun":
                    p.Add(A(12, 12, 4));
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; p.Add(new Seg(V(12 + Mathf.Cos(a) * 7, 12 + Mathf.Sin(a) * 7), V(12 + Mathf.Cos(a) * 9.5f, 12 + Mathf.Sin(a) * 9.5f))); }
                    break;
                case "mouse": p.Add(A(12, 12, 6, 0, 360, 1, 1.45f)); p.Add(new Seg(V(12, 4), V(12, 9))); break;
                case "pad":
                    p.Add(A(12, 13, 9, 0, 360, 1, 0.62f)); p.Add(new Seg(V(6, 13), V(10, 13))); p.Add(new Seg(V(8, 11), V(8, 15)));
                    p.Add(new Dot(V(15.5f, 12), 1.1f)); p.Add(new Dot(V(18, 14), 1.1f)); break;
                default: p.AddRange(Poly(true, 5, 5, 19, 5, 19, 19, 5, 19)); break;
            }
            return p;
        }

        /// <summary>A white icon with alpha; tint it with Image.tintColor or unityBackgroundImageTintColor.</summary>
        public static Texture2D Get(string id)
        {
            if (Cache.TryGetValue(id, out var tex) && tex != null) return tex;
            var prims = Shape(id);
            tex = new Texture2D(Res, Res, TextureFormat.RGBA32, true) { filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp, name = "menu-icon-" + id };
            var px = new Color32[Res * Res];
            float unit = Res / 24f, half = Stroke * 0.5f;
            for (int y = 0; y < Res; y++)
                for (int x = 0; x < Res; x++)
                {
                    // texture rows go bottom-up; the grid is top-down
                    var g = new Vector2((x + 0.5f) / unit, 24f - (y + 0.5f) / unit);
                    float d = float.MaxValue;
                    foreach (var pr in prims) d = Mathf.Min(d, pr.Dist(g));
                    float a = Mathf.Clamp01((half - d) * unit + 0.5f);
                    px[y * Res + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255));
                }
            tex.SetPixels32(px); tex.Apply(true, true);
            Cache[id] = tex;
            return tex;
        }
    }
}
