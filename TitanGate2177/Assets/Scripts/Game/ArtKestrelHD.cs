using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Game
{
    public static partial class Art
    {
        public static bool UseKestrelHD;
        static Dictionary<string, string> _shipHDMapping;
        static readonly Dictionary<string, Sprite> ShipHDSprites = new Dictionary<string, Sprite>();

        static Sprite KestrelHDReplacement(string key, Sprite original)
        {
            if (!UseKestrelHD || original == null) return original;
            if (_shipHDMapping == null)
            {
                _shipHDMapping = new Dictionary<string, string>();
                var manifest = Resources.Load<TextAsset>("Sprites/KestrelHD/mapping");
                if (manifest != null)
                    foreach (var line in manifest.text.Split('\n'))
                    {
                        var parts = line.Trim().Split('\t');
                        if (parts.Length == 2) _shipHDMapping[parts[0]] = parts[1];
                    }
            }
            if (!_shipHDMapping.TryGetValue(key, out var asset))
            {
                if (key == "lockerFalse") asset = "locker_closed_48_80";
                else if (key == "lockerTrue") asset = "locker_open_48_80";
                else if (key == "navTrue" || key == "navFalse") asset = "console_0_80_68";
                else return original;
            }
            return LoadKestrelHD(key, asset, original.pivot / original.rect.size,
                original.rect.width / original.pixelsPerUnit) ?? original;
        }

        public static Sprite KestrelHydroponicsRack(int variant) =>
            LoadKestrelHD("hydro/rack/" + variant, "hydro_rack_" + variant, new Vector2(.5f, .045f), 2.1f)
            ?? KestrelLabPlanter(variant);

        public static Sprite KestrelHydroponicsTanks() =>
            LoadKestrelHD("hydro/tanks", "hydro_tanks", new Vector2(.5f, .17f), 2.05f)
            ?? Shelf(false);

        static Sprite LoadKestrelHD(string key, string asset, Vector2 pivot, float worldWidth)
        {
            if (ShipHDSprites.TryGetValue(key, out var replacement)) return replacement;
            var data = Resources.Load<TextAsset>("Sprites/KestrelHD/" + asset + ".png");
            if (data == null) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            texture.filterMode = FilterMode.Trilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.anisoLevel = 4;
            texture.mipMapBias = -0.35f;
            if (!texture.LoadImage(data.bytes))
            {
                Object.Destroy(texture);
                return null;
            }
            RefineKestrelHDTexture(texture, key, asset);
            replacement = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot,
                texture.width / worldWidth, 0, SpriteMeshType.FullRect);
            replacement.name = "HD/" + key;
            ShipHDSprites[key] = replacement;
            return replacement;
        }

        static void RefineKestrelHDTexture(Texture2D t, string key, string asset)
        {
            if (asset.StartsWith("floor_quarters_"))
            {
                var px = t.GetPixels32();
                for (int i = 0; i < px.Length; i++)
                {
                    Color c = px[i];
                    // Calm bright yellow horizontal grate slits so they do not flicker while walking.
                    if (c.r > .56f && c.g > .46f && c.b < .38f)
                        px[i] = Color.Lerp(c, new Color(.38f, .37f, .48f, c.a), .68f);
                }
                t.SetPixels32(px);
                SoftenHighFrequencyTile(t, .48f);
                return;
            }
            if (asset.StartsWith("floor_k5_") || asset.StartsWith("floor_k3_") || asset.StartsWith("floor_k8_")
                || asset.Contains("_v4_") || asset.StartsWith("floor_cockpit_1") || asset.StartsWith("floor_lab_2"))
            {
                var px = t.GetPixels32();
                for (int i = 0; i < px.Length; i++)
                {
                    Color c = px[i];
                    if (c.r > .62f && c.g > .42f && c.b < .34f)
                        px[i] = Color.Lerp(c, new Color(.44f, .46f, .52f, c.a), .52f);
                }
                t.SetPixels32(px);
                SoftenHighFrequencyTile(t, .46f);
                return;
            }
            if (asset == "bed_46_18") { DrawGroundedBed(t); return; }
            if (asset == "table_160_80") { DrawGroundedWorkbench(t); return; }
            if (asset.StartsWith("shelf_")) { DrawGroundedShelf(t, asset.Contains("manual")); return; }
            if (asset == "seat_52_58") { GroundPilotSeat(t); return; }
            if (asset == "sofa_112_54") { GroundGalleySofa(t); return; }
            if (asset.StartsWith("console_")) { GroundConsole(t); return; }
            if (asset == "screen_bank_46_154") { GroundScreenBank(t); return; }
            if (asset.StartsWith("locker_") || asset.StartsWith("fridge_") || asset.StartsWith("rack_galley_"))
            {
                CompactAndMountWallCabinet(t, asset);
                return;
            }
            if (asset.StartsWith("tank_")) { CompactAndCradleTank(t); return; }
            if (asset.StartsWith("battery_") || asset == "recycler_26_36" || asset == "crates_34_26")
            {
                AddEquipmentPlinth(t);
                return;
            }
            if (asset == "hydro_tanks") { GroundHydroTanks(t); return; }
            if (asset.StartsWith("hydro_rack_")) { GroundHydroRack(t); }
        }

        static void FillRect(Color32[] px, int w, int h, int x0, int y0, int rw, int rh, Color32 col)
        {
            int x1 = Mathf.Min(w, x0 + rw), y1 = Mathf.Min(h, y0 + rh);
            x0 = Mathf.Max(0, x0); y0 = Mathf.Max(0, y0);
            for (int y = y0; y < y1; y++)
            {
                int row = y * w;
                for (int x = x0; x < x1; x++) px[row + x] = col;
            }
        }

        static void SoftShadowBar(Color32[] px, int w, int h, int x0, int x1, int y0, int y1, byte maxA = 135)
        {
            int cx = (x0 + x1) / 2, rx = Mathf.Max(1, (x1 - x0) / 2);
            int cy = (y0 + y1) / 2, ry = Mathf.Max(1, (y1 - y0) / 2);
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(h - 1, y1); y++)
            {
                float ny = (y - cy) / (float)ry;
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(w - 1, x1); x++)
                {
                    float nx = Mathf.Max(0f, (Mathf.Abs(x - cx) - rx * .55f) / (rx * .45f));
                    float d = Mathf.Clamp01(1f - (nx * nx + ny * ny * .75f));
                    if (d <= 0f) continue;
                    byte a = (byte)Mathf.RoundToInt(d * maxA);
                    int idx = y * w + x;
                    if (px[idx].a < a) px[idx] = new Color32(8, 16, 30, a);
                }
            }
        }

        static void DrawGroundedBed(Texture2D t)
        {
            int w = t.width, h = t.height; // 138x54, opaqueX=[3..134]
            var px = new Color32[w * h];
            var ink = PixelCanvas.Hex("#111b2a");
            var frameD = PixelCanvas.Hex("#233246");
            var frameM = PixelCanvas.Hex("#475d76");
            var frameL = PixelCanvas.Hex("#7d95ae");
            var sheet = PixelCanvas.Hex("#dce5ef");
            var sheetShade = PixelCanvas.Hex("#a5b6c9");
            var blanket = PixelCanvas.Hex("#c86d3b");
            var blanketL = PixelCanvas.Hex("#e38b52");
            var blanketD = PixelCanvas.Hex("#8f4726");

            SoftShadowBar(px, w, h, 4, w - 5, 0, 7, 145);
            // 3/4 steel berth frame and deck legs
            FillRect(px, w, h, 3, 4, w - 6, 44, ink);
            FillRect(px, w, h, 5, 6, w - 10, 12, frameD);
            FillRect(px, w, h, 5, 15, w - 10, 3, frameM);
            // Recessed toe-kick under berth
            FillRect(px, w, h, 18, 4, w - 36, 6, PixelCanvas.Hex("#0d1522"));
            // Corner stanchion legs bolted to deck
            FillRect(px, w, h, 5, 3, 14, 13, frameM);
            FillRect(px, w, h, w - 19, 3, 14, 13, frameM);
            FillRect(px, w, h, 7, 13, 10, 2, frameL);
            FillRect(px, w, h, w - 17, 13, 10, 2, frameL);
            // Outer bed shell
            FillRect(px, w, h, 5, 18, w - 10, 29, frameM);
            FillRect(px, w, h, 6, 45, w - 12, 2, frameL);
            // Mattress and front 3/4 bevel
            FillRect(px, w, h, 9, 19, w - 18, 25, sheetShade);
            FillRect(px, w, h, 9, 23, w - 18, 20, sheet);
            // Headboard bolster on left
            FillRect(px, w, h, 6, 19, 7, 26, frameD);
            FillRect(px, w, h, 14, 21, 22, 20, ink);
            FillRect(px, w, h, 16, 22, 18, 18, PixelCanvas.Hex("#eef3f9"));
            FillRect(px, w, h, 16, 22, 18, 4, sheetShade);
            // Folded thermal blanket on right
            FillRect(px, w, h, 44, 18, w - 54, 26, ink);
            FillRect(px, w, h, 46, 19, w - 58, 5, blanketD);
            FillRect(px, w, h, 46, 24, w - 58, 19, blanket);
            FillRect(px, w, h, 46, 39, w - 58, 4, blanketL);
            FillRect(px, w, h, 46, 24, 5, 19, blanketL);
            t.SetPixels32(px);
            t.Apply(true, false);
        }

        static void DrawGroundedWorkbench(Texture2D t)
        {
            int w = t.width, h = t.height; // 480x240, opaqueX=[10..469]
            var px = new Color32[w * h];
            var ink = PixelCanvas.Hex("#111b2a");
            var legD = PixelCanvas.Hex("#1e2c3e");
            var legM = PixelCanvas.Hex("#3b4f66");
            var fascia = PixelCanvas.Hex("#4d647d");
            var topM = PixelCanvas.Hex("#7890a8");
            var topL = PixelCanvas.Hex("#adc2d6");
            var mat = PixelCanvas.Hex("#203647");
            var matLine = PixelCanvas.Hex("#4ca6b8");

            SoftShadowBar(px, w, h, 12, w - 13, 0, 18, 150);
            // Left and right heavy 3/4 stanchion legs + lower cross-beam
            FillRect(px, w, h, 22, 8, 42, 82, ink);
            FillRect(px, w, h, w - 64, 8, 42, 82, ink);
            FillRect(px, w, h, 26, 11, 34, 76, legD);
            FillRect(px, w, h, w - 60, 11, 34, 76, legD);
            FillRect(px, w, h, 30, 14, 12, 70, legM);
            FillRect(px, w, h, w - 56, 14, 12, 70, legM);
            // Lower tool stretcher bar
            FillRect(px, w, h, 58, 24, w - 116, 18, ink);
            FillRect(px, w, h, 62, 27, w - 124, 12, legD);
            FillRect(px, w, h, 62, 36, w - 124, 3, legM);
            // Tabletop 3/4 front fascia and top surface
            FillRect(px, w, h, 10, 74, w - 20, 142, ink);
            FillRect(px, w, h, 14, 78, w - 28, 22, legD);
            FillRect(px, w, h, 14, 94, w - 28, 6, fascia);
            FillRect(px, w, h, 14, 100, w - 28, 112, topM);
            FillRect(px, w, h, 16, 205, w - 32, 6, topL);
            FillRect(px, w, h, 16, 102, 5, 108, topL);
            // Anti-static diagnostic mat and clamped EVA tool tray on tabletop
            FillRect(px, w, h, 42, 116, 236, 76, ink);
            FillRect(px, w, h, 46, 120, 228, 68, mat);
            FillRect(px, w, h, 54, 128, 64, 42, ink);
            FillRect(px, w, h, 57, 131, 58, 36, PixelCanvas.Hex("#112230"));
            FillRect(px, w, h, 64, 146, 44, 4, matLine);
            FillRect(px, w, h, 134, 134, 126, 4, matLine);
            FillRect(px, w, h, 134, 152, 96, 4, matLine);
            // Right-side parts tray & amber calibrator
            FillRect(px, w, h, 306, 120, 126, 68, ink);
            FillRect(px, w, h, 310, 124, 118, 60, legD);
            FillRect(px, w, h, 320, 134, 46, 40, PixelCanvas.Hex("#df9b3b"));
            FillRect(px, w, h, 324, 164, 38, 6, PixelCanvas.Hex("#ffe09e"));
            FillRect(px, w, h, 376, 134, 40, 40, PixelCanvas.Hex("#58829e"));
            t.SetPixels32(px);
            t.Apply(true, false);
        }

        static void DrawGroundedShelf(Texture2D t, bool withManual)
        {
            int w = t.width, h = t.height; // 120x136, opaqueX=[6..113]
            var px = new Color32[w * h];
            var ink = PixelCanvas.Hex("#101a28");
            var back = PixelCanvas.Hex("#162334");
            var post = PixelCanvas.Hex("#4a617a");
            var postL = PixelCanvas.Hex("#829bb5");
            var shelf = PixelCanvas.Hex("#36495e");
            var rail = PixelCanvas.Hex("#95adc4");

            SoftShadowBar(px, w, h, 6, w - 7, 0, 8, 145);
            // Outer rack frame & floor plinth (x=6..113 so OpaqueSpanX is unchanged)
            FillRect(px, w, h, 6, 4, 108, 114, ink);
            FillRect(px, w, h, 9, 7, 102, 10, PixelCanvas.Hex("#1c2a3d"));
            FillRect(px, w, h, 9, 15, 102, 3, post);
            FillRect(px, w, h, 14, 18, 92, 95, back);
            // Left and right upright stanchions
            FillRect(px, w, h, 8, 6, 8, 110, post);
            FillRect(px, w, h, 104, 6, 8, 110, post);
            FillRect(px, w, h, 10, 8, 3, 106, postL);
            FillRect(px, w, h, 106, 8, 3, 106, postL);
            FillRect(px, w, h, 8, 112, 104, 4, postL);

            // Three horizontal shelves with front retaining rails and stowed containers
            int[] sy = { 18, 50, 82 };
            for (int i = 0; i < sy.Length; i++)
            {
                int y = sy[i];
                FillRect(px, w, h, 14, y, 92, 8, shelf);
                FillRect(px, w, h, 14, y + 6, 92, 3, rail);
                FillRect(px, w, h, 14, y - 2, 92, 2, ink);
            }
            // Bottom shelf: heavy supply crates
            FillRect(px, w, h, 20, 27, 36, 20, ink);
            FillRect(px, w, h, 22, 29, 32, 16, PixelCanvas.Hex("#3c5c78"));
            FillRect(px, w, h, 22, 42, 32, 3, PixelCanvas.Hex("#6e94b5"));
            FillRect(px, w, h, 62, 27, 38, 20, ink);
            FillRect(px, w, h, 64, 29, 34, 16, PixelCanvas.Hex("#b86b35"));
            FillRect(px, w, h, 64, 42, 34, 3, PixelCanvas.Hex("#e89c5e"));
            // Middle shelf: cylinders & manual/tool kit
            FillRect(px, w, h, 20, 59, 22, 20, ink);
            FillRect(px, w, h, 22, 61, 18, 16, PixelCanvas.Hex("#3d826b"));
            FillRect(px, w, h, 46, 59, 30, 20, ink);
            FillRect(px, w, h, 48, 61, 26, 16, withManual ? PixelCanvas.Hex("#d99b38") : PixelCanvas.Hex("#4d6882"));
            FillRect(px, w, h, 50, 73, 22, 3, withManual ? PixelCanvas.Hex("#ffe19c") : postL);
            FillRect(px, w, h, 80, 59, 20, 18, ink);
            FillRect(px, w, h, 82, 61, 16, 14, PixelCanvas.Hex("#587591"));
            // Top shelf: diagnostic case
            FillRect(px, w, h, 24, 91, 72, 18, ink);
            FillRect(px, w, h, 26, 93, 68, 14, PixelCanvas.Hex("#284358"));
            FillRect(px, w, h, 32, 98, 24, 4, PixelCanvas.Hex("#62d0dc"));
            t.SetPixels32(px);
            t.Apply(true, false);
        }

        static void GroundPilotSeat(Texture2D t)
        {
            int w = t.width, h = t.height; // 156x174, opaqueX=[18..137], opaqueY=[28..159]
            var px = t.GetPixels32();
            var ink = PixelCanvas.Hex("#111b2a");
            var dark = PixelCanvas.Hex("#1d2b3e");
            var steel = PixelCanvas.Hex("#496078");
            var rim = PixelCanvas.Hex("#8ba3bc");

            SoftShadowBar(px, w, h, 20, w - 21, 0, 10, 145);
            // Twin bolted deck rails + central hydraulic pedestal under the seat
            FillRect(px, w, h, 28, 4, 24, 28, ink);
            FillRect(px, w, h, w - 52, 4, 24, 28, ink);
            FillRect(px, w, h, 31, 6, 18, 24, steel);
            FillRect(px, w, h, w - 49, 6, 18, 24, steel);
            FillRect(px, w, h, 34, 8, 6, 20, rim);
            FillRect(px, w, h, w - 46, 8, 6, 20, rim);
            // Central column and under-seat chassis
            FillRect(px, w, h, 58, 8, 40, 24, ink);
            FillRect(px, w, h, 62, 10, 32, 20, dark);
            FillRect(px, w, h, 22, 22, w - 44, 10, ink);
            FillRect(px, w, h, 25, 24, w - 50, 6, dark);
            t.SetPixels32(px);
            t.Apply(true, false);
        }

        static void GroundGalleySofa(Texture2D t)
        {
            int w = t.width, h = t.height; // 336x162, opaqueX=[6..329]
            var px = t.GetPixels32();
            var ink = PixelCanvas.Hex("#101928");
            var plinth = PixelCanvas.Hex("#1b2a3d");
            var steel = PixelCanvas.Hex("#485f78");
            var rim = PixelCanvas.Hex("#7c96b0");

            SoftShadowBar(px, w, h, 8, w - 9, 0, 9, 150);
            // Recessed continuous base frame with 3 anchored deck skids
            FillRect(px, w, h, 10, 4, w - 20, 16, ink);
            FillRect(px, w, h, 14, 7, w - 28, 11, plinth);
            FillRect(px, w, h, 14, 16, w - 28, 3, steel);
            foreach (int sx in new[] { 16, w / 2 - 22, w - 60 })
            {
                FillRect(px, w, h, sx, 3, 44, 14, ink);
                FillRect(px, w, h, sx + 3, 5, 38, 10, steel);
                FillRect(px, w, h, sx + 6, 12, 32, 3, rim);
            }
            t.SetPixels32(px);
            t.Apply(true, false);
        }

        static void GroundConsole(Texture2D t)
        {
            int w = t.width, h = t.height; // 240x204, opaqueX=[11..228], opaqueY=[22..172]
            var px = t.GetPixels32();
            var ink = PixelCanvas.Hex("#101928");
            var dark = PixelCanvas.Hex("#1a2738");
            var steel = PixelCanvas.Hex("#435a73");
            var rim = PixelCanvas.Hex("#7c96b0");

            SoftShadowBar(px, w, h, 14, w - 15, 0, 8, 145);
            FillRect(px, w, h, 18, 3, w - 36, 23, ink);
            FillRect(px, w, h, 22, 6, w - 44, 18, dark);
            FillRect(px, w, h, 22, 21, w - 44, 3, steel);
            FillRect(px, w, h, 18, 3, 28, 8, steel);
            FillRect(px, w, h, w - 46, 3, 28, 8, steel);
            FillRect(px, w, h, 22, 8, 20, 2, rim);
            FillRect(px, w, h, w - 42, 8, 20, 2, rim);
            t.SetPixels32(px);
            t.Apply(true, false);
        }

        static void GroundScreenBank(Texture2D t)
        {
            int w = t.width, h = t.height; // 138x462, opaqueX=[12..130], opaqueY=[16..445]
            var px = t.GetPixels32();
            var ink = PixelCanvas.Hex("#101928");
            var dark = PixelCanvas.Hex("#1d2c3f");
            var steel = PixelCanvas.Hex("#4b637d");

            SoftShadowBar(px, w, h, 12, 130, 0, 8, 145);
            FillRect(px, w, h, 14, 3, 114, 18, ink);
            FillRect(px, w, h, 18, 6, 106, 12, dark);
            FillRect(px, w, h, 18, 16, 106, 3, steel);
            t.SetPixels32(px);
            t.Apply(true, false);
        }

        static void CompactAndMountWallCabinet(Texture2D t, string asset)
        {
            int w = t.width, h = t.height;
            var src = t.GetPixels32();
            int minX = w, maxX = -1, minY = h, maxY = -1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (src[y * w + x].a > 12)
                    {
                        if (x < minX) minX = x; if (x > maxX) maxX = x;
                        if (y < minY) minY = y; if (y > maxY) maxY = y;
                    }
            if (maxX < minX) return;

            var dst = new Color32[w * h];
            var ink = PixelCanvas.Hex("#101928");
            var plinth = PixelCanvas.Hex("#1a2738");
            var steel = PixelCanvas.Hex("#4d657e");
            var rim = PixelCanvas.Hex("#8ba3bc");

            int newBottom = 20;
            int newTop = Mathf.RoundToInt(h * 0.75f); // stays below horizontal wall top cap
            for (int y = newBottom; y <= newTop; y++)
            {
                float v = (y - newBottom) / (float)Mathf.Max(1, newTop - newBottom);
                int sy = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(minY, maxY, v)), minY, maxY);
                for (int x = minX; x <= maxX; x++)
                    dst[y * w + x] = src[sy * w + x];
            }

            if (asset == "locker_open_48_80")
            {
                // Replace the flat diagram inside the open locker with a recessed 3/4 EVA suit bay.
                int ix0 = minX + 12, ix1 = maxX - 12;
                int iy0 = newBottom + 8, iy1 = newTop - 10;
                FillRect(dst, w, h, ix0, iy0, ix1 - ix0, iy1 - iy0, ink);
                FillRect(dst, w, h, ix0 + 3, iy0 + 3, ix1 - ix0 - 6, iy1 - iy0 - 6, PixelCanvas.Hex("#152233"));
                // Upper helmet shelf
                int shelfY = iy1 - 36;
                FillRect(dst, w, h, ix0 + 3, shelfY, ix1 - ix0 - 6, 5, steel);
                FillRect(dst, w, h, ix0 + 3, shelfY + 3, ix1 - ix0 - 6, 2, rim);
                // Stowed EVA helmet on top shelf
                int cx = (ix0 + ix1) / 2;
                FillRect(dst, w, h, cx - 16, shelfY + 6, 32, 22, ink);
                FillRect(dst, w, h, cx - 13, shelfY + 8, 26, 18, PixelCanvas.Hex("#dce6f2"));
                FillRect(dst, w, h, cx - 9, shelfY + 11, 18, 10, PixelCanvas.Hex("#285c7d"));
                FillRect(dst, w, h, cx - 6, shelfY + 16, 10, 3, PixelCanvas.Hex("#7be0ec"));
                // Hanging orange flight suit below shelf
                int suitTop = shelfY - 4, suitH = suitTop - (iy0 + 16);
                FillRect(dst, w, h, cx - 24, iy0 + 16, 48, suitH, ink);
                FillRect(dst, w, h, cx - 21, iy0 + 18, 42, suitH - 4, PixelCanvas.Hex("#c96b33"));
                FillRect(dst, w, h, cx - 18, suitTop - 12, 36, 8, PixelCanvas.Hex("#f09b59"));
                FillRect(dst, w, h, cx - 11, iy0 + 32, 22, 20, PixelCanvas.Hex("#dce6f2"));
                FillRect(dst, w, h, cx - 7, iy0 + 38, 14, 6, PixelCanvas.Hex("#3a6e8c"));
                // Boot rack at bottom of locker
                FillRect(dst, w, h, ix0 + 6, iy0 + 4, ix1 - ix0 - 12, 10, plinth);
            }

            SoftShadowBar(dst, w, h, minX, maxX, 0, 8, 150);
            FillRect(dst, w, h, minX, 4, maxX - minX + 1, newBottom - 2, ink);
            FillRect(dst, w, h, minX + 4, 7, maxX - minX - 7, newBottom - 7, plinth);
            FillRect(dst, w, h, minX + 4, newBottom - 3, maxX - minX - 7, 3, steel);
            FillRect(dst, w, h, minX + 2, newTop - 2, maxX - minX - 3, 3, rim);
            t.SetPixels32(dst);
            t.Apply(true, false);
        }

        static void CompactAndCradleTank(Texture2D t)
        {
            int w = t.width, h = t.height; // 96x184, opaqueX=[6..89]
            var src = t.GetPixels32();
            int minX = 6, maxX = w - 7, minY = 9, maxY = h - 1;
            var dst = new Color32[w * h];
            var ink = PixelCanvas.Hex("#101928");
            var cradle = PixelCanvas.Hex("#243448");
            var steel = PixelCanvas.Hex("#556e87");

            int newBottom = 18, newTop = Mathf.RoundToInt(h * 0.78f);
            for (int y = newBottom; y <= newTop; y++)
            {
                float v = (y - newBottom) / (float)Mathf.Max(1, newTop - newBottom);
                int sy = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(minY, maxY, v)), minY, maxY);
                for (int x = minX; x <= maxX; x++)
                    dst[y * w + x] = src[sy * w + x];
            }
            SoftShadowBar(dst, w, h, minX, maxX, 0, 8, 150);
            FillRect(dst, w, h, minX + 4, 4, maxX - minX - 7, 16, ink);
            FillRect(dst, w, h, minX + 8, 7, maxX - minX - 15, 11, cradle);
            FillRect(dst, w, h, minX + 8, 16, maxX - minX - 15, 3, steel);
            t.SetPixels32(dst);
            t.Apply(true, false);
        }

        static void AddEquipmentPlinth(Texture2D t)
        {
            int w = t.width, h = t.height;
            var px = t.GetPixels32();
            int minX = w, maxX = -1, minY = h;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a > 12)
                    {
                        if (x < minX) minX = x; if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                    }
            if (maxX < minX) return;
            var ink = PixelCanvas.Hex("#101928");
            var plinth = PixelCanvas.Hex("#1d2b3e");
            var steel = PixelCanvas.Hex("#4c647d");
            SoftShadowBar(px, w, h, minX, maxX, 0, Mathf.Max(5, minY), 145);
            if (minY > 4)
            {
                FillRect(px, w, h, minX + 2, 3, maxX - minX - 3, minY + 2, ink);
                FillRect(px, w, h, minX + 5, 5, maxX - minX - 9, minY - 2, plinth);
                FillRect(px, w, h, minX + 5, minY, maxX - minX - 9, 2, steel);
            }
            t.SetPixels32(px);
            t.Apply(true, false);
        }

        static void GroundHydroTanks(Texture2D t)
        {
            int w = t.width, h = t.height; // 352x304, pivot y=0.17 (~52px), opaqueX=[36..315], opaqueY=[57..272]
            var px = t.GetPixels32();
            var ink = PixelCanvas.Hex("#101928");
            var plinth = PixelCanvas.Hex("#1d2d3d");
            var steel = PixelCanvas.Hex("#4f6880");
            SoftShadowBar(px, w, h, 36, 315, 38, 56, 145);
            FillRect(px, w, h, 40, 44, 272, 18, ink);
            FillRect(px, w, h, 44, 47, 264, 12, plinth);
            FillRect(px, w, h, 44, 57, 264, 3, steel);
            t.SetPixels32(px);
            t.Apply(true, false);
        }

        static void GroundHydroRack(Texture2D t)
        {
            int w = t.width, h = t.height; // 352x400, opaqueX=[24..327], opaqueY=[5..389]
            var px = t.GetPixels32();
            SoftShadowBar(px, w, h, 24, 327, 0, 14, 145);
            t.SetPixels32(px);
            t.Apply(true, false);
        }

        public static void ClearKestrelHDCache()
        {
            foreach (var s in ShipHDSprites.Values)
            {
                if (s == null) continue;
                var tex = s.texture;
                if (Application.isPlaying)
                {
                    Object.Destroy(s);
                    if (tex != null) Object.Destroy(tex);
                }
                else
                {
                    Object.DestroyImmediate(s);
                    if (tex != null) Object.DestroyImmediate(tex);
                }
            }
            ShipHDSprites.Clear();
            _shipHDMapping = null;
        }
    }
}
