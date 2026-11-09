// LAST 72 HOURS — 2.5D room stage (Broken Sword / Kathy Rain style walkable floor).
// Logical floor space: x = 0..1 across the room, d = 0..1 depth (0 = front edge near the camera, 1 = back wall).
// Everything here is pure layout/math; GameUI draws it and the Astronaut walks on it.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Last72
{
    /// Perspective of one room. All values are normalised inside the room view (0..1, origin bottom-left).
    public class RoomShape
    {
        public float FloorFront = 0.04f;  // screen y of the front edge of the floor
        public float FloorBack = 0.40f;   // screen y where the floor meets the back wall
        public float Inset = 0.16f;       // how far the back corners move in (perspective)
        public float FarScale = 0.55f;    // character / object scale at the back wall
        public float WallTop = 0.92f;     // top of the back wall (low = cramped)
        public bool Inner;                // inner room: exit at the front edge (walk towards the camera)

        public float ScreenY(float d) { return Mathf.Lerp(FloorFront, FloorBack, d); }
        public float Left(float d) { return Mathf.Lerp(0f, Inset, d); }
        public float Right(float d) { return Mathf.Lerp(1f, 1f - Inset, d); }
        public float ScreenX(float x, float d) { return Mathf.Lerp(Left(d), Right(d), x); }
        public float Scale(float d) { return Mathf.Lerp(1f, FarScale, d); }
        public Vector2 Project(float x, float d) { return new Vector2(ScreenX(x, d), ScreenY(d)); }

        /// Screen point -> floor point (clamped onto the floor).
        public Vector2 Unproject(Vector2 s)
        {
            float d = Mathf.Clamp01(Mathf.InverseLerp(FloorFront, FloorBack, s.y));
            float x = Mathf.Clamp01(Mathf.InverseLerp(Left(d), Right(d), s.x));
            return new Vector2(x, d);
        }
    }

    public enum SpotKind { Wall, Floor, Decor, DoorLeft, DoorRight, Hatch, Exit }

    /// Where an object stands in the room. X/W in floor units, D = front edge depth, Dep = footprint depth,
    /// H = height (room-view heights at scale 1), Lift = raised off the floor (e.g. a wall screen).
    public class Spot
    {
        public string Id;      // hotspot id, or decor id (deco_*) for non-interactive props
        public string Name;    // decor label (hotspots use the model name)
        public SpotKind Kind;
        public float X, D, W, H, Dep, Lift;

        public bool Blocks { get { return Kind == SpotKind.Floor || Kind == SpotKind.Decor; } }

        /// Where the astronaut stands to use it.
        public Vector2 Stand
        {
            get
            {
                switch (Kind)
                {
                    case SpotKind.DoorLeft: return new Vector2(0.0f, 0.35f);
                    case SpotKind.DoorRight: return new Vector2(1.0f, 0.35f);
                    case SpotKind.Hatch: return new Vector2(X, 1f);
                    case SpotKind.Exit: return new Vector2(X, 0f);
                    case SpotKind.Wall: return new Vector2(X, 0.95f);
                    default: return new Vector2(X, Mathf.Max(0.02f, D - 0.07f));
                }
            }
        }

        /// Where the astronaut appears when arriving through this door.
        public Vector2 Arrive
        {
            get
            {
                switch (Kind)
                {
                    case SpotKind.DoorLeft: return new Vector2(0.06f, 0.35f);
                    case SpotKind.DoorRight: return new Vector2(0.94f, 0.35f);
                    case SpotKind.Hatch: return new Vector2(X, 0.86f);
                    case SpotKind.Exit: return new Vector2(X, 0.10f);
                    default: return Stand;
                }
            }
        }

        /// Distance from a floor point to this object (0 = touching / inside reach box).
        public float DistanceTo(Vector2 p)
        {
            float x0, x1, d0, d1;
            switch (Kind)
            {
                case SpotKind.DoorLeft: x0 = 0; x1 = 0.03f; d0 = 0.18f; d1 = 0.52f; break;
                case SpotKind.DoorRight: x0 = 0.97f; x1 = 1; d0 = 0.18f; d1 = 0.52f; break;
                case SpotKind.Hatch: x0 = X - W / 2; x1 = X + W / 2; d0 = 0.97f; d1 = 1; break;
                case SpotKind.Exit: x0 = X - W / 2; x1 = X + W / 2; d0 = 0; d1 = 0.03f; break;
                case SpotKind.Wall: x0 = X - W / 2; x1 = X + W / 2; d0 = 0.97f; d1 = 1; break;
                default: x0 = X - W / 2; x1 = X + W / 2; d0 = D; d1 = D + Dep; break;
            }
            float dx = Mathf.Max(0, Mathf.Max(x0 - p.x, p.x - x1));
            float dd = Mathf.Max(0, Mathf.Max(d0 - p.y, p.y - d1));
            return Mathf.Sqrt(dx * dx + dd * dd);
        }
    }

    public static class Stage25D
    {
        static Dictionary<Room, RoomShape> shapes;
        static Dictionary<Room, List<Spot>> spots;

        public static RoomShape Shape(Room r) { Init(); return shapes[r]; }
        public static List<Spot> Spots(Room r) { Init(); return spots[r]; }
        public static Spot Find(Room r, string id) { return Spots(r).Find(s => s.Id == id); }

        static Spot S(string id, SpotKind k, float x, float d, float w, float h, float dep = 0, float lift = 0, string name = null)
        { return new Spot { Id = id, Kind = k, X = x, D = d, W = w, H = h, Dep = dep, Lift = lift, Name = name }; }

        static void Init()
        {
            if (shapes != null) return;
            var deep = new RoomShape();
            var medium = new RoomShape { FloorBack = 0.28f, Inset = 0.07f, FarScale = 0.72f };
            shapes = new Dictionary<Room, RoomShape>
            {
                { Room.Quarters, deep },
                { Room.Command, deep },
                { Room.LifeSupport, medium },   // long corridor-like room: shallow floor
                { Room.Engineering, new RoomShape { FloorBack = 0.38f, Inset = 0.13f, FarScale = 0.58f } },
                { Room.Cabin, new RoomShape { FloorBack = 0.44f, Inset = 0.22f, FarScale = 0.52f, WallTop = 0.84f, Inner = true } },
                { Room.CommsRoom, new RoomShape { FloorBack = 0.42f, Inset = 0.20f, FarScale = 0.54f, WallTop = 0.84f, Inner = true } },
                { Room.Crawlspace, new RoomShape { FloorBack = 0.30f, Inset = 0.26f, FarScale = 0.66f, WallTop = 0.74f, Inner = true } },
                { Room.PartsStorage, new RoomShape { FloorBack = 0.42f, Inset = 0.20f, FarScale = 0.54f, WallTop = 0.84f, Inner = true } },
            };
            spots = new Dictionary<Room, List<Spot>>
            {
                { Room.Quarters, new List<Spot> {
                    S("bed", SpotKind.Floor, 0.20f, 0.52f, 0.30f, 0.13f, 0.30f),
                    S("food", SpotKind.Wall, 0.47f, 1, 0.13f, 0.29f),
                    S("locker", SpotKind.Wall, 0.66f, 1, 0.12f, 0.37f),
                    S("hatch_Cabin", SpotKind.Hatch, 0.86f, 1, 0.12f, 0.33f),
                    S("deco_table", SpotKind.Decor, 0.56f, 0.30f, 0.14f, 0.09f, 0.12f, 0, "Table"),
                    S("deco_stool", SpotKind.Decor, 0.70f, 0.26f, 0.05f, 0.07f, 0.05f, 0, "Stool"),
                    S("door_Quarters_Command", SpotKind.DoorRight, 0, 0, 0, 0.42f) } },
                { Room.Command, new List<Spot> {
                    S("console", SpotKind.Floor, 0.50f, 0.50f, 0.34f, 0.15f, 0.16f),
                    S("nav", SpotKind.Wall, 0.17f, 1, 0.20f, 0.26f),
                    S("security", SpotKind.Wall, 0.83f, 1, 0.20f, 0.26f),
                    S("hatch_CommsRoom", SpotKind.Hatch, 0.50f, 1, 0.13f, 0.33f),
                    S("deco_chair", SpotKind.Decor, 0.50f, 0.30f, 0.07f, 0.10f, 0.06f, 0, "Pilot Seat"),
                    S("door_Command_Quarters", SpotKind.DoorLeft, 0, 0, 0, 0.42f),
                    S("door_Command_LifeSupport", SpotKind.DoorRight, 0, 0, 0, 0.42f) } },
                { Room.LifeSupport, new List<Spot> {
                    S("scrubber", SpotKind.Floor, 0.44f, 0.72f, 0.30f, 0.30f, 0.20f),
                    S("emergency", SpotKind.Wall, 0.80f, 1, 0.16f, 0.16f, 0, 0.10f),
                    S("hatch_Crawlspace", SpotKind.Hatch, 0.14f, 1, 0.11f, 0.30f),
                    S("deco_tanks", SpotKind.Decor, 0.70f, 0.36f, 0.08f, 0.20f, 0.10f, 0, "O2 Tanks"),
                    S("door_LifeSupport_Command", SpotKind.DoorLeft, 0, 0, 0, 0.42f),
                    S("door_LifeSupport_Engineering", SpotKind.DoorRight, 0, 0, 0, 0.42f) } },
                { Room.Engineering, new List<Spot> {
                    S("pdu", SpotKind.Wall, 0.14f, 1, 0.15f, 0.30f),
                    S("cable", SpotKind.Wall, 0.36f, 1, 0.15f, 0.10f, 0, 0.14f),
                    S("propline", SpotKind.Floor, 0.40f, 0.30f, 0.42f, 0.08f, 0.08f),
                    S("jump", SpotKind.Floor, 0.68f, 0.66f, 0.22f, 0.26f, 0.16f),
                    S("hatch_PartsStorage", SpotKind.Hatch, 0.88f, 1, 0.11f, 0.32f),
                    S("door_Engineering_LifeSupport", SpotKind.DoorLeft, 0, 0, 0, 0.42f) } },
                { Room.Cabin, new List<Spot> {
                    S("recorder", SpotKind.Wall, 0.26f, 1, 0.18f, 0.09f, 0, 0.12f),
                    S("viewport", SpotKind.Wall, 0.66f, 1, 0.24f, 0.16f, 0, 0.12f),
                    S("desk", SpotKind.Floor, 0.66f, 0.50f, 0.30f, 0.13f, 0.18f),
                    S("deco_sofa", SpotKind.Decor, 0.24f, 0.30f, 0.30f, 0.12f, 0.16f, 0, "Sofa"),
                    S("exit_Cabin", SpotKind.Exit, 0.50f, 0, 0.24f, 0) } },
                { Room.CommsRoom, new List<Spot> {
                    S("comms", SpotKind.Wall, 0.32f, 1, 0.30f, 0.27f),
                    S("signal", SpotKind.Floor, 0.70f, 0.50f, 0.24f, 0.16f, 0.14f),
                    S("deco_chair2", SpotKind.Decor, 0.42f, 0.28f, 0.07f, 0.09f, 0.06f, 0, "Chair"),
                    S("exit_CommsRoom", SpotKind.Exit, 0.50f, 0, 0.24f, 0) } },
                { Room.Crawlspace, new List<Spot> {
                    S("maint", SpotKind.Wall, 0.30f, 1, 0.18f, 0.27f),
                    S("duct", SpotKind.Wall, 0.70f, 1, 0.34f, 0.09f, 0, 0.10f),
                    S("deco_pipes", SpotKind.Decor, 0.62f, 0.34f, 0.24f, 0.06f, 0.06f, 0, "Pipes"),
                    S("exit_Crawlspace", SpotKind.Exit, 0.50f, 0, 0.24f, 0) } },
                { Room.PartsStorage, new List<Spot> {
                    S("storage", SpotKind.Wall, 0.28f, 1, 0.17f, 0.33f),
                    S("rack", SpotKind.Floor, 0.68f, 0.52f, 0.30f, 0.26f, 0.12f),
                    S("deco_crate1", SpotKind.Decor, 0.24f, 0.30f, 0.12f, 0.10f, 0.10f, 0, "Crate"),
                    S("deco_crate2", SpotKind.Decor, 0.37f, 0.20f, 0.08f, 0.08f, 0.08f, 0, "Crate"),
                    S("exit_PartsStorage", SpotKind.Exit, 0.50f, 0, 0.24f, 0) } },
            };
        }

        /// Screen rect (room-view normalised) of an object's front face.
        public static Rect FaceRect(RoomShape sh, Spot s)
        {
            float d = s.Kind == SpotKind.Wall || s.Kind == SpotKind.Hatch ? 1f : s.D;
            float sc = sh.Scale(d);
            float x0 = sh.ScreenX(s.X - s.W / 2, d), x1 = sh.ScreenX(s.X + s.W / 2, d);
            float y0 = sh.ScreenY(d) + s.Lift * sc;
            return new Rect(x0, y0, x1 - x0, s.H * sc);
        }

        /// Sort key: larger = further away (drawn first).
        public static float SortDepth(Spot s)
        {
            if (s.Kind == SpotKind.Wall || s.Kind == SpotKind.Hatch) return 1.01f;
            if (s.Kind == SpotKind.DoorLeft || s.Kind == SpotKind.DoorRight) return 1.02f;
            if (s.Kind == SpotKind.Exit) return -1f;
            return s.D;
        }
    }

    /// Four-corner polygon for walls, floor and furniture tops (uGUI Image can only do rectangles).
    public class UIQuad : MaskableGraphic, ICanvasRaycastFilter
    {
        /// Clicks count only inside the polygon, not the whole (full-stretch) rect.
        public bool IsRaycastLocationValid(Vector2 sp, Camera cam)
        {
            Vector2 lp;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, cam, out lp)) return false;
            var r = rectTransform.rect;
            var p = new Vector2((lp.x - r.xMin) / r.width, (lp.y - r.yMin) / r.height);
            bool inside = false;
            for (int i = 0, j = 3; i < 4; j = i++)
            {
                var a = Corners[i]; var b = Corners[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        public Vector2[] Corners = new Vector2[4]; // normalised inside the parent rect: BL, BR, TR, TL

        public void Set(Vector2 a, Vector2 b, Vector2 c, Vector2 d) { Corners = new[] { a, b, c, d }; SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            var col = (Color32)color;
            for (int i = 0; i < 4; i++)
                vh.AddVert(new Vector3(r.xMin + Corners[i].x * r.width, r.yMin + Corners[i].y * r.height), col, Vector2.zero);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);
        }

        public static UIQuad Make(Transform parent, string name, Color c, Vector2 a, Vector2 b, Vector2 cc, Vector2 d)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var q = go.AddComponent<UIQuad>();
            q.color = c; q.raycastTarget = false;
            q.Set(a, b, cc, d);
            return q;
        }
    }

    /// Grid A* on the floor so click-to-walk goes around furniture.
    public class FloorPath
    {
        const int NX = 48, ND = 24;
        readonly bool[,] blocked = new bool[NX, ND];
        public const float RadiusX = 0.025f, RadiusD = 0.035f;
        readonly List<Spot> obstacles = new List<Spot>();

        public void Build(List<Spot> spots)
        {
            obstacles.Clear();
            foreach (var s in spots) if (s.Blocks) obstacles.Add(s);
            for (int i = 0; i < NX; i++)
                for (int j = 0; j < ND; j++)
                    blocked[i, j] = !IsFree(Cell(i, j));
        }

        static Vector2 Cell(int i, int j) { return new Vector2((i + 0.5f) / NX, (j + 0.5f) / ND); }
        static int CI(float x) { return Mathf.Clamp((int)(x * NX), 0, NX - 1); }
        static int CJ(float d) { return Mathf.Clamp((int)(d * ND), 0, ND - 1); }

        /// True if the astronaut can stand here (inside the floor and not inside furniture).
        public bool IsFree(Vector2 p)
        {
            if (p.x < 0 || p.x > 1 || p.y < 0 || p.y > 1) return false;
            foreach (var s in obstacles)
                if (p.x > s.X - s.W / 2 - RadiusX && p.x < s.X + s.W / 2 + RadiusX && p.y > s.D - RadiusD && p.y < s.D + s.Dep + RadiusD)
                    return false;
            return true;
        }

        /// Nearest free floor point to p (used when the click/target is inside furniture).
        public Vector2 NearestFree(Vector2 p)
        {
            if (IsFree(p)) return p;
            Vector2 best = p; float bd = 999;
            for (int i = 0; i < NX; i++)
                for (int j = 0; j < ND; j++)
                {
                    if (blocked[i, j]) continue;
                    float dd = (Cell(i, j) - p).sqrMagnitude;
                    if (dd < bd) { bd = dd; best = Cell(i, j); }
                }
            return best;
        }

        bool LineFree(Vector2 a, Vector2 b)
        {
            int n = Mathf.CeilToInt((b - a).magnitude / 0.01f);
            for (int k = 1; k <= n; k++) if (!IsFree(Vector2.Lerp(a, b, k / (float)n))) return false;
            return true;
        }

        /// Waypoints from a to b (b itself last). Straight line when nothing is in the way.
        public List<Vector2> Find(Vector2 a, Vector2 b)
        {
            b = NearestFree(b);
            var result = new List<Vector2>();
            if (LineFree(a, b)) { result.Add(b); return result; }
            int si = CI(a.x), sj = CJ(a.y), ti = CI(b.x), tj = CJ(b.y);
            var g = new float[NX, ND]; var from = new int[NX, ND]; var closed = new bool[NX, ND];
            for (int i = 0; i < NX; i++) for (int j = 0; j < ND; j++) { g[i, j] = float.MaxValue; from[i, j] = -1; }
            g[si, sj] = 0;
            var open = new List<int> { si * ND + sj };
            while (open.Count > 0)
            {
                int bi = 0; float bf = float.MaxValue;
                for (int k = 0; k < open.Count; k++)
                {
                    int ci = open[k] / ND, cj = open[k] % ND;
                    float f = g[ci, cj] + Mathf.Abs(ci - ti) + Mathf.Abs(cj - tj) * 2f;
                    if (f < bf) { bf = f; bi = k; }
                }
                int cur = open[bi]; open.RemoveAt(bi);
                int x = cur / ND, y = cur % ND;
                if (closed[x, y]) continue;
                closed[x, y] = true;
                if (x == ti && y == tj) break;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= NX || ny >= ND || closed[nx, ny]) continue;
                        if (blocked[nx, ny] && !(nx == ti && ny == tj)) continue;
                        float step = Mathf.Sqrt(dx * dx + dy * dy * 4f); // depth cells are "longer"
                        if (g[x, y] + step < g[nx, ny]) { g[nx, ny] = g[x, y] + step; from[nx, ny] = cur; open.Add(nx * ND + ny); }
                    }
            }
            if (from[ti, tj] < 0 && !(si == ti && sj == tj)) { result.Add(b); return result; } // no path: walk straight, collision stops us
            var cells = new List<Vector2>();
            for (int c = ti * ND + tj; c >= 0 && c != si * ND + sj; c = from[c / ND, c % ND]) cells.Add(Cell(c / ND, c % ND));
            cells.Reverse();
            // string-pull: keep only turning points that are needed
            var pos = a;
            for (int k = 0; k < cells.Count; k++)
            {
                if (k + 1 < cells.Count && LineFree(pos, cells[k + 1])) continue;
                result.Add(cells[k]); pos = cells[k];
            }
            if (result.Count == 0 || result[result.Count - 1] != b) result.Add(b);
            return result;
        }
    }
}
