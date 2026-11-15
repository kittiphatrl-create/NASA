// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does the Kestrel feel like a lived-in ship when every screen and status LED actually gives off light?
// Date: 2026-11-15
using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>
    /// Screen and LED glow for the ship Kestrel. After the ship is built, every prop and wall sprite is scanned once for
    /// bright emissive pixels (green, cyan, blue and amber screens/LEDs); each lit area gets a soft coloured light over the
    /// dark veil, with a gentle per-light flicker. The light follows its source (it fades with walls you walk behind and
    /// switches off if the prop is hidden).
    /// </summary>
    public partial class GameFlow
    {
        class ScreenGlow
        {
            public SpriteRenderer src, glow;
            public Color color;
            public float strength, scale;
            public int faultGroup;
        }

        readonly List<ScreenGlow> _sGlows = new List<ScreenGlow>();
        readonly List<(SpriteRenderer source, SpriteRenderer mask)> _sFaultDisplays = new List<(SpriteRenderer, SpriteRenderer)>();
        static readonly Dictionary<Sprite, List<(Vector2 local, Color color, float size)>> GlowSpotCache =
            new Dictionary<Sprite, List<(Vector2, Color, float)>>();

        const int GlowCell = 4;          // texture pixels per detection cell
        const int GlowMaxLights = 320;   // keep the overlay cheap

        void SBuildScreenGlows()
        {
            _sGlows.Clear();
            _sFaultDisplays.Clear();
            if (_bRoot == null) return;
            var placed = new List<Vector2>();
            var sources = _bRoot.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in sources)
            {
                if (_sGlows.Count >= GlowMaxLights) break;
                if (sr == null || sr.sprite == null) continue;
                if (sr.sortingOrder <= -29000 || sr.sortingOrder >= 29000) continue;   // floors, space, overlays
                string spName = sr.sprite.name.ToLowerInvariant();
                string goName = sr.name.ToLowerInvariant();
                if (goName.StartsWith("hull-light")) continue; // placed fixtures have their own spill
                if (spName.Contains("loose") || goName == "can") continue;    // supplies you carry aren't machines
                if (spName.StartsWith("hd/hydro/")) continue; // foliage and nutrient tanks are not light sources
                if (spName.Contains("plant") || spName.Contains("planter") || goName.Contains("plant")) continue; // plants don't emit light
                if (spName.Contains("bed") || spName.Contains("bunk") || spName.Contains("cot") || goName.Contains("bed") || goName.Contains("bunk")) continue; // beds don't emit light
                var spots = GlowSpots(sr.sprite);
                if (spots == null || spots.Count == 0) continue;
                int faultGroup = -1;
                if (spName.Contains("/console/") || spName.Contains("screen-bank") ||
                    spName.Contains("recycler") || spName.Contains("sabatier") || spName.Contains("sabtruetrue"))
                {
                    var mask = BSprite("fault-display", Art.KestrelDamageDisplay(sr.sprite, IsEmissive),
                        sr.transform.position, sr.sortingOrder + 1);
                    mask.transform.SetParent(sr.transform, false);
                    mask.transform.localPosition = Vector3.zero;
                    mask.flipX = sr.flipX;
                    mask.flipY = sr.flipY;
                    mask.enabled = false;
                    faultGroup = _sFaultDisplays.Count;
                    _sFaultDisplays.Add((sr, mask));
                }
                foreach (var (local, color, size) in spots)
                {
                    Vector2 world = sr.transform.TransformPoint(new Vector3(sr.flipX ? -local.x : local.x, local.y, 0));
                    if (placed.Exists(p => (p - world).sqrMagnitude < 0.09f)) continue;   // merge lights closer than 0.3
                    placed.Add(world);
                    var g = BSprite("screen-glow", Art.SoftLight(), world, 30001);
                    if (_bGlowMaterial == null) { var shader = Resources.Load<Shader>("UI/HabitatGlow"); if (shader != null) _bGlowMaterial = new Material(shader); }
                    if (_bGlowMaterial != null) g.sharedMaterial = _bGlowMaterial;
                    if (goName != "door") BBoundRoomLight(g, sr.transform.position, includeWallFace: true);
                    float scale = Mathf.Clamp(size * 2.0f, .45f, 1.5f);
                    g.transform.localScale = Vector3.one * scale;
                    g.color = new Color(color.r, color.g, color.b, 0f);
                    _sGlows.Add(new ScreenGlow
                    {
                        src = sr, glow = g, color = color,
                        strength = Mathf.Lerp(.20f, .42f, Mathf.Clamp01(size)),
                        scale = scale,
                        faultGroup = faultGroup,
                    });
                    if (_sGlows.Count >= GlowMaxLights) break;
                }
            }
        }

        float SDisplayFaultPulse(int group) => SFailedLight(Mathf.Repeat(.24f + group * .41f, 1f),
            1.02f + (group % 4) * .15f);

        /// <summary>Keep the emissive pixels and their spill in sync while the electronics lose power.</summary>
        void SScreenGlows(float dt)
        {
            bool emergency = SPowerFault;
            for (int i = 0; i < _sFaultDisplays.Count; i++)
            {
                var (source, mask) = _sFaultDisplays[i];
                if (mask == null) continue;
                mask.enabled = emergency && source != null && source.enabled;
                if (!mask.enabled) continue;
                var red = Color.Lerp(new Color(.13f, .023f, .023f), EmergencyRed, SDisplayFaultPulse(i));
                red.a = source.color.a;
                mask.color = red;
            }
            foreach (var s in _sGlows)
            {
                if (s.glow == null) continue;
                bool visible = s.src != null && s.src.enabled && s.src.gameObject.activeInHierarchy;
                float srcA = s.src != null ? s.src.color.a : 0f;
                bool failed = emergency && s.faultGroup >= 0;
                float strength = failed ? .10f + SDisplayFaultPulse(s.faultGroup) * 2.0f : emergency ? .7f : 1f;
                float target = visible ? s.strength * strength * srcA : 0f;
                var c = failed ? EmergencyRed : s.color;
                c.a = Mathf.Lerp(s.glow.color.a, target, dt * (failed ? 12f : 8f));
                s.glow.color = c;
                s.glow.transform.localScale = Vector3.one * s.scale * (failed ? 1.9f : 1f);
            }
        }

        /// <summary>Find the lit areas of a sprite (local positions in sprite space, colour, rough size in units).</summary>
        static List<(Vector2 local, Color color, float size)> GlowSpots(Sprite sp)
        {
            if (GlowSpotCache.TryGetValue(sp, out var cached)) return cached;
            List<(Vector2, Color, float)> result = null;
            var tex = sp.texture;
            if (tex != null && tex.isReadable)
            {
                try { result = ScanGlow(sp, tex); }
                catch (System.Exception) { result = null; }
            }
            GlowSpotCache[sp] = result;
            return result;
        }

        static List<(Vector2, Color, float)> ScanGlow(Sprite sp, Texture2D tex)
        {
            var r = sp.textureRect;
            int x0 = Mathf.RoundToInt(r.x), y0 = Mathf.RoundToInt(r.y), w = Mathf.RoundToInt(r.width), h = Mathf.RoundToInt(r.height);
            if (w < 4 || h < 4) return null;
            var px = tex.GetPixels(x0, y0, w, h);
            int cw = (w + GlowCell - 1) / GlowCell, ch = (h + GlowCell - 1) / GlowCell;
            var cellCol = new Color[cw * ch];
            var cellHit = new bool[cw * ch];
            for (int cy = 0; cy < ch; cy++)
            for (int cx = 0; cx < cw; cx++)
            {
                int hits = 0; Color sum = Color.clear;
                for (int yy = cy * GlowCell; yy < Mathf.Min(h, cy * GlowCell + GlowCell); yy++)
                for (int xx = cx * GlowCell; xx < Mathf.Min(w, cx * GlowCell + GlowCell); xx++)
                {
                    var c = px[yy * w + xx];
                    if (!IsEmissive(c)) continue;
                    hits++; sum += c;
                }
                if (hits >= 3) { cellHit[cy * cw + cx] = true; cellCol[cy * cw + cx] = sum / hits; }
            }

            // flood-fill neighbouring lit cells into blobs
            var list = new List<(Vector2, Color, float)>();
            var seen = new bool[cw * ch];
            var q = new Queue<int>();
            float ppu = sp.pixelsPerUnit;
            for (int i = 0; i < cellHit.Length; i++)
            {
                if (!cellHit[i] || seen[i]) continue;
                int count = 0, minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
                float sx = 0, sy = 0; Color sum = Color.clear;
                q.Enqueue(i); seen[i] = true;
                while (q.Count > 0)
                {
                    int k = q.Dequeue(), kx = k % cw, ky = k / cw;
                    count++; sx += kx; sy += ky; sum += cellCol[k];
                    minX = Mathf.Min(minX, kx); maxX = Mathf.Max(maxX, kx); minY = Mathf.Min(minY, ky); maxY = Mathf.Max(maxY, ky);
                    if (kx > 0) Visit(k - 1); if (kx < cw - 1) Visit(k + 1);
                    if (ky > 0) Visit(k - cw); if (ky < ch - 1) Visit(k + cw);
                }
                if (count < 1) continue;
                Color col = sum / count; col.a = 1f;
                Color.RGBToHSV(col, out float hh, out float ss, out _);
                col = Color.HSVToRGB(hh, Mathf.Clamp01(ss * 1.1f), 1f);   // full-brightness light colour
                float cxp = (sx / count + 0.5f) * GlowCell, cyp = (sy / count + 0.5f) * GlowCell;
                var local = new Vector2((cxp - sp.pivot.x) / ppu, (cyp - sp.pivot.y) / ppu);
                float size = Mathf.Max(maxX - minX + 1, maxY - minY + 1) * GlowCell / ppu;
                list.Add((local, col, size));
            }
            return list.Count > 0 ? list : null;

            void Visit(int k) { if (cellHit[k] && !seen[k]) { seen[k] = true; q.Enqueue(k); } }
        }

        /// <summary>
        /// Screen and LED colours as they come out of the Blender renders: green and cyan screens are only mid-bright, so
        /// the bar is low, but blue (hue past 205°, the wall cable conduit and water cans) and orange/red (hull stripes,
        /// the galley floor) are left out.
        /// </summary>
        static bool IsEmissive(Color c)
        {
            if (c.a < 0.85f) return false;
            Color.RGBToHSV(c, out float hue, out float sat, out float val);
            float deg = hue * 360f;
            bool green = deg >= 85f && deg <= 165f && sat >= 0.32f && val >= 0.55f;
            bool cyan = deg > 165f && deg <= 205f && sat >= 0.32f && val >= 0.58f;
            bool amber = deg >= 36f && deg < 60f && sat >= 0.45f && val >= 0.78f;
            return green || cyan || amber;
        }
    }
}
