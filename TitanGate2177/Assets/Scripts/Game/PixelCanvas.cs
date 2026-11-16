// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>A tiny pixel canvas, y goes up (row 0 is the bottom), turned into point-filtered sprites.</summary>
    public class PixelCanvas
    {
        public readonly int W, H;
        readonly Color32[] _px;

        public PixelCanvas(int w, int h)
        {
            W = w; H = h;
            _px = new Color32[w * h];
        }

        public static Color32 Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c);
            return c;
        }

        public Color32 Get(int x, int y) => (x < 0 || y < 0 || x >= W || y >= H) ? new Color32(0, 0, 0, 0) : _px[y * W + x];

        public void Set(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            _px[y * W + x] = c;
        }

        public void Fill(Color32 c) { for (int i = 0; i < _px.Length; i++) _px[i] = c; }

        /// <summary>Fill rect from (x,y) bottom-left, w by h.</summary>
        public void Rect(int x, int y, int w, int h, Color32 c)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++) Set(i, j, c);
        }

        public void Frame(int x, int y, int w, int h, Color32 c)
        {
            for (int i = x; i < x + w; i++) { Set(i, y, c); Set(i, y + h - 1, c); }
            for (int j = y; j < y + h; j++) { Set(x, j, c); Set(x + w - 1, j, c); }
        }

        public void HLine(int x0, int x1, int y, Color32 c) { for (int i = x0; i <= x1; i++) Set(i, y, c); }
        public void VLine(int x, int y0, int y1, Color32 c) { for (int j = y0; j <= y1; j++) Set(x, j, c); }

        public void Line(int x0, int y0, int x1, int y1, Color32 c)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                Set(x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        public void Disc(int cx, int cy, int r, Color32 c)
        {
            for (int y = -r; y <= r; y++)
                for (int x = -r; x <= r; x++)
                    if (x * x + y * y <= r * r + r / 2) Set(cx + x, cy + y, c);
        }

        public void Ellipse(int cx, int cy, int rx, int ry, Color32 c)
        {
            for (int y = -ry; y <= ry; y++)
                for (int x = -rx; x <= rx; x++)
                    if ((x * x) / (float)(rx * rx) + (y * y) / (float)(ry * ry) <= 1f) Set(cx + x, cy + y, c);
        }

        /// <summary>Rounded rectangle (corners cut by one pixel).</summary>
        public void RoundRect(int x, int y, int w, int h, Color32 c)
        {
            Rect(x + 1, y, w - 2, h, c);
            Rect(x, y + 1, 1, h - 2, c);
            Rect(x + w - 1, y + 1, 1, h - 2, c);
        }

        /// <summary>Paint an ASCII map; '.' and ' ' are skipped. Row 0 of the array is the top.</summary>
        public void Ascii(int x, int y, string[] rows, System.Func<char, Color32?> pal)
        {
            for (int r = 0; r < rows.Length; r++)
            {
                int py = y + rows.Length - 1 - r;
                for (int i = 0; i < rows[r].Length; i++)
                {
                    char ch = rows[r][i];
                    if (ch == '.' || ch == ' ') continue;
                    var col = pal(ch);
                    if (col.HasValue) Set(x + i, py, col.Value);
                }
            }
        }

        /// <summary>Add a 1px outline around every opaque area.</summary>
        public void Outline(Color32 c)
        {
            var copy = (Color32[])_px.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (copy[y * W + x].a > 0) continue;
                    bool n = (x > 0 && copy[y * W + x - 1].a > 0) || (x < W - 1 && copy[y * W + x + 1].a > 0)
                          || (y > 0 && copy[(y - 1) * W + x].a > 0) || (y < H - 1 && copy[(y + 1) * W + x].a > 0);
                    if (n) _px[y * W + x] = c;
                }
        }

        /// <summary>A copy holding only the ring around this canvas' shape: the hover highlight.</summary>
        public PixelCanvas Glow(Color32 c)
        {
            var g = new PixelCanvas(W + 2, H + 2);
            for (int y = 0; y < H + 2; y++)
                for (int x = 0; x < W + 2; x++)
                {
                    if (Get(x - 1, y - 1).a > 0) continue;
                    bool n = Get(x - 2, y - 1).a > 0 || Get(x, y - 1).a > 0 || Get(x - 1, y - 2).a > 0 || Get(x - 1, y).a > 0;
                    if (n) g.Set(x, y, c);
                }
            return g;
        }

        /// <summary>
        /// Texture marks of c over opaque pixels, about p of the area (deterministic seed).
        /// Marks sit on a jittered grid and are 1-2 px dashes, so they read as placed by hand
        /// instead of white noise clumping into blotches.
        /// </summary>
        public void Speckle(Color32 c, float p, int seed, bool onlyOpaque = true)
        {
            if (p <= 0f) return;
            var rng = new System.Random(seed);
            int cell = Mathf.Max(2, Mathf.RoundToInt(Mathf.Sqrt(1.5f / p)));
            for (int gy = 0; gy < H; gy += cell)
                for (int gx = 0; gx < W; gx += cell)
                {
                    if (rng.NextDouble() > 0.85) continue;
                    int x = gx + rng.Next(cell), y = gy + rng.Next(cell);
                    int len = rng.NextDouble() < 0.5 ? 2 : 1;
                    for (int i = 0; i < len; i++)
                    {
                        int px = x + i;
                        if (px >= W || y >= H) continue;
                        if (!onlyOpaque || _px[y * W + px].a > 0) _px[y * W + px] = c;
                    }
                }
        }

        static readonly Color32 RimLight = new Color32(255, 248, 230, 255);
        static readonly Color32 RimShadow = new Color32(16, 14, 34, 255);

        /// <summary>
        /// One light for every prop: silhouette pixels facing up/left get a warm rim light, pixels
        /// facing down/right get shadow. Call before Outline. Inner details are untouched.
        /// </summary>
        public void Shade(float light = 0.22f, float dark = 0.28f)
        {
            var copy = (Color32[])_px.Clone();
            bool Op(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && copy[y * W + x].a > 0;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (!Op(x, y)) continue;
                    var c = copy[y * W + x];
                    bool top = !Op(x, y + 1), left = !Op(x - 1, y), bottom = !Op(x, y - 1), right = !Op(x + 1, y);
                    if (top || left) c = Color32.Lerp(c, RimLight, top ? light : light * 0.6f);
                    else if (bottom || right) c = Color32.Lerp(c, RimShadow, bottom ? dark : dark * 0.6f);
                    c.a = copy[y * W + x].a;
                    _px[y * W + x] = c;
                }
        }

        public void Blit(PixelCanvas src, int ox, int oy)
        {
            for (int y = 0; y < src.H; y++)
                for (int x = 0; x < src.W; x++)
                {
                    var c = src.Get(x, y);
                    if (c.a > 0) Set(ox + x, oy + y, c);
                }
        }

        public Texture2D ToTexture(TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            bool mip = W >= 16 && H >= 16;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, mip)
            {
                filterMode = mip ? (W >= 32 || H >= 32 ? FilterMode.Trilinear : FilterMode.Bilinear) : FilterMode.Point,
                wrapMode = wrap,
                anisoLevel = mip ? 4 : 1,
                mipMapBias = mip ? -0.35f : 0f,
                name = "px"
            };
            t.SetPixels32(_px);
            t.Apply(mip, false);
            return t;
        }

        public const float PPU = 16f;

        /// <summary>Sprite with pivot at bottom-centre (feet), the default for things standing on a floor.</summary>
        public Sprite ToSprite(Vector2? pivot = null, float ppu = PPU)
        {
            var t = ToTexture();
            return Sprite.Create(t, new UnityEngine.Rect(0, 0, W, H), pivot ?? new Vector2(0.5f, 0f), ppu, 0, SpriteMeshType.FullRect);
        }
    }
}
