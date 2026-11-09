// LAST 72 HOURS — context-sensitive mouse cursor (classic point-and-click "smart cursor").
// Magnifier = inspect, crosshair = use selected item here, red X = item won't work here,
// arrow = door, padlock = locked. Shapes are drawn in code; art slot "cursor_<kind>" overrides.
using System.Collections.Generic;
using UnityEngine;

namespace Last72
{
    public enum CursorKind { Default, Look, Use, CantUse, DoorLeft, DoorRight, Locked, DoorUp, DoorDown }

    public static class ContextCursor
    {
        public static CursorKind Current { get; private set; }
        static readonly Dictionary<CursorKind, Texture2D> tex = new Dictionary<CursorKind, Texture2D>();
        static readonly Dictionary<CursorKind, Vector2> hot = new Dictionary<CursorKind, Vector2>();
        const int N = 32;

        public static void Set(CursorKind k)
        {
            if (k == Current) return;
            Current = k;
            if (k == CursorKind.Default) { Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); return; }
            Texture2D t;
            if (!tex.TryGetValue(k, out t)) { t = Build(k); tex[k] = t; }
            Cursor.SetCursor(t, hot[k], CursorMode.Auto);
        }

        public static void Reset() { Current = CursorKind.Default; Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); }

        static Texture2D Build(CursorKind k)
        {
            var custom = ArtSlots.GetReadable("cursor_" + k.ToString().ToLowerInvariant());
            if (custom != null) { hot[k] = new Vector2(custom.width / 2f, custom.height / 2f); return custom; }

            var m = new bool[N, N]; // [x, y] with y = 0 at TOP (cursor space)
            Color fill = Color.white;
            switch (k)
            {
                case CursorKind.Look:
                    Ring(m, 12, 12, 9, 2.2f);
                    Line(m, 18, 18, 28, 28, 2.4f);
                    hot[k] = new Vector2(12, 12); fill = new Color(1f, 0.9f, 0.5f); break;
                case CursorKind.Use:
                    Ring(m, 16, 16, 10, 1.8f);
                    Line(m, 16, 2, 16, 10, 1.4f); Line(m, 16, 22, 16, 30, 1.4f);
                    Line(m, 2, 16, 10, 16, 1.4f); Line(m, 22, 16, 30, 16, 1.4f);
                    Disc(m, 16, 16, 2.2f);
                    hot[k] = new Vector2(16, 16); fill = new Color(0.45f, 1f, 0.55f); break;
                case CursorKind.CantUse:
                    Ring(m, 16, 16, 12, 2f);
                    Line(m, 8, 8, 24, 24, 2.2f);
                    hot[k] = new Vector2(16, 16); fill = new Color(1f, 0.35f, 0.3f); break;
                case CursorKind.DoorLeft:
                case CursorKind.DoorRight:
                    for (int y = 6; y <= 26; y++)
                        for (int x = 4; x <= 28; x++)
                        {
                            int dy = Mathf.Abs(y - 16);
                            bool head = x >= 14 && x <= 28 - dy;          // triangle tip at x=28
                            bool shaft = x < 16 && dy <= 4;
                            if (head || shaft) m[k == CursorKind.DoorRight ? x : N - 1 - x, y] = true;
                        }
                    hot[k] = new Vector2(16, 16); fill = new Color(0.55f, 0.85f, 1f); break;
                case CursorKind.DoorUp:   // enter the hatch in the back wall
                case CursorKind.DoorDown: // step back out of an inner room
                    for (int y = 4; y <= 28; y++)
                        for (int x = 6; x <= 26; x++)
                        {
                            int dx = Mathf.Abs(x - 16);
                            bool head = y <= 18 && y >= 4 + dx;          // triangle tip at y=4 (top)
                            bool shaft = y > 16 && dx <= 4;
                            if (head || shaft) m[x, k == CursorKind.DoorUp ? y : N - 1 - y] = true;
                        }
                    hot[k] = new Vector2(16, 16); fill = new Color(0.45f, 1f, 0.85f); break;
                case CursorKind.Locked:
                    for (int y = 14; y <= 28; y++) for (int x = 7; x <= 25; x++) m[x, y] = true;   // body
                    Ring(m, 16, 13, 6.5f, 1.8f);                                                // shackle
                    for (int y = 19; y <= 23; y++) m[16, y] = false;                             // keyhole
                    hot[k] = new Vector2(16, 16); fill = new Color(1f, 0.45f, 0.35f); break;
            }
            return Paint(m, fill);
        }

        static void Disc(bool[,] m, float cx, float cy, float r)
        {
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) m[x, y] = true;
        }

        static void Ring(bool[,] m, float cx, float cy, float r, float w)
        {
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (Mathf.Abs(d - r) <= w) m[x, y] = true;
                }
        }

        static void Line(bool[,] m, float x0, float y0, float x1, float y1, float w)
        {
            var a = new Vector2(x0, y0); var b = new Vector2(x1, y1);
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    var p = new Vector2(x, y);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / (b - a).sqrMagnitude);
                    if ((a + t * (b - a) - p).magnitude <= w) m[x, y] = true;
                }
        }

        /// Fill shape, add 1px black outline, flip to texture space (y up).
        static Texture2D Paint(bool[,] m, Color fill)
        {
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    Color c = Color.clear;
                    if (m[x, y]) c = fill;
                    else
                    {
                        for (int dy = -1; dy <= 1 && c.a == 0; dy++)
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx >= 0 && ny >= 0 && nx < N && ny < N && m[nx, ny]) { c = new Color(0, 0, 0, 0.9f); break; }
                            }
                    }
                    px[(N - 1 - y) * N + x] = c;
                }
            t.SetPixels(px);
            t.Apply();
            return t;
        }
    }
}
