// Reads menu_points.json (written by the Blender render script) into MenuPoints.
// A tiny JSON reader is used instead of JsonUtility so points may be written as {x,y} or [x,y].
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace TitanGate.Game.Menu
{
    public static class MenuPointsJson
    {
        public static MenuPoints Parse(string json)
        {
            var root = new Reader(json).Value() as Dictionary<string, object>;
            if (root == null) throw new FormatException("not an object");
            var p = new MenuPoints();
            if (TryVec(Find(root, "sun", "sun_center", "sunCenter"), out var sun)) p.sun = sun;
            if (Find(root, "sun") is Dictionary<string, object> sd)
            {
                if (TryNum(Find(sd, "radius", "r"), out var sr0)) p.sunRadius = sr0;
                if (TryNum(Find(sd, "glow_radius", "glow"), out var sg)) p.sunGlow = sg;
            }
            if (Find(root, "rover_scale_hint") is Dictionary<string, object> rh && TryNum(Find(rh, "length_at_horizon", "width"), out var rw)) p.roverWidth = rw;
            if (Find(root, "rover_sprite") is Dictionary<string, object> rs && TryNum(Find(rs, "ground_y"), out var rg)) p.roverGround = rg;
            if (TryNum(Find(root, "sunRadius", "sun_radius"), out var sr)) p.sunRadius = sr;
            if (TryVec(Find(root, "airlock", "door", "airlock_door"), out var al)) p.airlock = al;
            if (Find(root, "beacons", "lights", "windows", "beacon_lights") is List<object> bl)
                foreach (var b in bl)
                    if (TryVec(b, out var v))
                    {
                        p.beacons.Add(v);
                        var c = new Color(1f, 0.55f, 0.25f);
                        if (b is Dictionary<string, object> bd && Find(bd, "color") is string hex) ColorUtility.TryParseHtmlString(hex, out c);
                        p.beaconColors.Add(c);
                    }
            if (Find(root, "horizon", "horizon_line") is List<object> hl)
                foreach (var h in hl) if (TryVec(h, out var v)) p.horizon.Add(v);
            p.horizon.Sort((a, b) => a.x.CompareTo(b.x));
            if (Find(root, "astronaut", "cadet", "astronaut_bbox") is Dictionary<string, object> ad)
            {
                if (TryNum(Find(ad, "x", "x0", "min_x", "left"), out var x0) && TryNum(Find(ad, "y", "y0", "min_y", "top"), out var y0))
                {
                    float x1 = TryNum(Find(ad, "x1", "max_x", "right"), out var xr) ? xr : x0 + (TryNum(Find(ad, "w", "width"), out var w) ? w : 0.1f);
                    float y1 = TryNum(Find(ad, "y1", "max_y", "bottom"), out var yb) ? yb : y0 + (TryNum(Find(ad, "h", "height"), out var hh) ? hh : 0.4f);
                    p.cadet = Rect.MinMaxRect(x0, y0, x1, y1);
                }
            }
            return p;
        }

        /// <summary>menu_scene_*.json: layered background with depths, sprite sheets and points.</summary>
        public static SceneSpec ParseScene(string json)
        {
            var root = new Reader(json).Value() as Dictionary<string, object>;
            if (root == null) throw new FormatException("not an object");
            var spec = new SceneSpec();
            if (Find(root, "scene", "name") is string nm) spec.Name = nm;
            if (Find(root, "canvas") is List<object> cv && cv.Count >= 2 && TryNum(cv[0], out var cw) && TryNum(cv[1], out var ch) && ch > 0) spec.Aspect = cw / ch;
            if (Find(root, "layers") is List<object> ls)
                foreach (var o in ls)
                {
                    if (!(o is Dictionary<string, object> d)) continue;
                    var l = new SceneLayer { Name = Find(d, "name") as string ?? "", File = Find(d, "file") as string, Role = Find(d, "role") as string, Video = Find(d, "video") as string };
                    if (TryNum(Find(d, "depth"), out var dep)) l.Depth = dep;
                    if (Find(d, "rect") is List<object> r && r.Count >= 4 && TryNum(r[0], out var x0) && TryNum(r[1], out var y0) && TryNum(r[2], out var x1) && TryNum(r[3], out var y1))
                        l.Rect = Rect.MinMaxRect(x0, y0, x1, y1);
                    if (Find(d, "sheet") is Dictionary<string, object> sh)
                    {
                        if (TryNum(Find(sh, "cols"), out var c)) l.Cols = Mathf.Max(1, (int)c);
                        if (TryNum(Find(sh, "rows"), out var rr)) l.Rows = Mathf.Max(1, (int)rr);
                        if (TryNum(Find(sh, "frames"), out var f)) l.Frames = Mathf.Max(1, (int)f);
                        if (TryNum(Find(sh, "fps"), out var fps)) l.Fps = fps;
                    }
                    if (!string.IsNullOrEmpty(l.File)) spec.Layers.Add(l);
                }
            if (Find(root, "points") is Dictionary<string, object> pts)
            {
                spec.Sun = Point(Find(pts, "sun"));
                spec.Earth = Point(Find(pts, "earth"));
                if (Find(pts, "beacons", "lights") is List<object> bl) foreach (var b in bl) { var p = Point(b); if (p != null) spec.Beacons.Add(p); }
                if (TryVec(Find(pts, "airlock", "focus"), out var al)) spec.Airlock = al;
                spec.DoorLight = Point(Find(pts, "door_light", "doorLight"));
                if (Find(pts, "window") is Dictionary<string, object> w && TryVec(w, out var wc))
                {
                    TryNum(Find(w, "r", "radius"), out var wr);
                    spec.Window = new Vector3(wc.x, wc.y, wr);
                    if (!TryVec(Find(pts, "airlock", "focus"), out _)) spec.Airlock = wc;   // the ship pushes in toward the window
                }
            }
            return spec;
        }

        static ScenePoint Point(object o)
        {
            if (!TryVec(o, out var v)) return null;
            var p = new ScenePoint { P = v };
            if (o is Dictionary<string, object> d)
            {
                p.Layer = Find(d, "layer") as string;
                if (Find(d, "color") is string hex && ColorUtility.TryParseHtmlString(hex, out var c)) p.Color = c;
                if (TryNum(Find(d, "radius", "r"), out var r)) p.Radius = r;
                if (TryNum(Find(d, "glow_radius", "glow"), out var g)) p.Glow = g;
            }
            return p;
        }

        /// <summary>menu_ring.json: ring outer radius as a fraction of the image half-size, and the clamp's anchor pixel.</summary>
        public static (float outer, Vector2 anchor, Vector2 light, Vector2 tip) ParseRing(string json)
        {
            var root = new Reader(json).Value() as Dictionary<string, object>;
            if (root == null) throw new FormatException("not an object");
            float size = 0;
            if (TryNum(Find(root, "size", "ring_size", "width", "ring_px"), out var sz)) size = sz;
            if (Find(root, "ring") is Dictionary<string, object> rd)
            {
                if (TryNum(Find(rd, "size", "width", "px"), out var rs)) size = rs;
                root = Merge(root, rd);
            }
            float outer = 0.97f;
            if (TryNum(Find(root, "outer_radius_frac", "outer_frac", "outer_radius", "outer", "band_outer", "radius_outer"), out var o))
                outer = o > 1.5f && size > 0 ? o / (size * 0.5f) : o;
            var anchor = Vector2.zero;
            object c = Find(root, "clamp_anchor", "anchor");
            if (c == null && Find(root, "clamp") is Dictionary<string, object> cd) c = Find(cd, "anchor", "anchor_px", "rim");
            if (TryVec(c, out var a)) anchor = a;
            Vector2 light = Vector2.zero, tip = Vector2.zero;
            if (Find(root, "clamp") is Dictionary<string, object> cl)
            {
                if (TryVec(Find(cl, "indicator_px", "light_px", "indicator"), out var lv)) light = lv;
                if (TryVec(Find(cl, "tip_px", "tip"), out var tv)) tip = tv;
            }
            return (Mathf.Clamp(outer, 0.5f, 1f), anchor, light, tip);
        }

        static Dictionary<string, object> Merge(Dictionary<string, object> a, Dictionary<string, object> b)
        {
            var m = new Dictionary<string, object>(a);
            foreach (var kv in b) m[kv.Key] = kv.Value;
            return m;
        }

        static object Find(Dictionary<string, object> d, params string[] keys)
        {
            foreach (var k in keys) if (d.TryGetValue(k, out var v)) return v;
            return null;
        }

        static bool TryNum(object o, out float f)
        {
            f = 0;
            if (o is double d) { f = (float)d; return true; }
            return false;
        }

        static bool TryVec(object o, out Vector2 v)
        {
            v = default;
            if (o is List<object> l && l.Count >= 2 && TryNum(l[0], out var x) && TryNum(l[1], out var y)) { v = new Vector2(x, y); return true; }
            if (o is Dictionary<string, object> d)
            {
                if (TryNum(Find(d, "x", "u"), out var dx) && TryNum(Find(d, "y", "v"), out var dy)) { v = new Vector2(dx, dy); return true; }
                if (TryVec(Find(d, "pos", "position", "center", "screen"), out v)) return true;
            }
            return false;
        }

        sealed class Reader
        {
            readonly string s; int i;
            public Reader(string s) { this.s = s; }
            void Ws() { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
            public object Value()
            {
                Ws();
                if (i >= s.Length) return null;
                char c = s[i];
                if (c == '{') { i++; var d = new Dictionary<string, object>(); Ws(); if (s[i] == '}') { i++; return d; }
                    while (true) { Ws(); var k = (string)Value(); Ws(); i++; d[k] = Value(); Ws(); if (s[i] == ',') { i++; continue; } i++; return d; } }
                if (c == '[') { i++; var l = new List<object>(); Ws(); if (s[i] == ']') { i++; return l; }
                    while (true) { l.Add(Value()); Ws(); if (s[i] == ',') { i++; continue; } i++; return l; } }
                if (c == '"')
                {
                    i++; var sb = new StringBuilder();
                    while (s[i] != '"') { if (s[i] == '\\') { i++; char e = s[i]; sb.Append(e == 'n' ? '\n' : e == 't' ? '\t' : e == 'u' ? (char)Convert.ToInt32(s.Substring(i + 1, 4), 16) : e); if (e == 'u') i += 4; } else sb.Append(s[i]); i++; }
                    i++; return sb.ToString();
                }
                if (s.Substring(i).StartsWith("true")) { i += 4; return true; }
                if (s.Substring(i).StartsWith("false")) { i += 5; return false; }
                if (s.Substring(i).StartsWith("null")) { i += 4; return null; }
                int st = i; while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
                return double.Parse(s.Substring(st, i - st), CultureInfo.InvariantCulture);
            }
        }
    }
}
