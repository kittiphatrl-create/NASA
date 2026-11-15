// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>Something the player can click. The astronaut walks to standX, then the flow handles id.</summary>
    public class Hotspot : MonoBehaviour
    {
        public string id;
        public string nameKey;
        public int room;
        public float standX;
        public bool interactable = true;
        /// <summary>For doors: the room this door leads to (-1 = not a door).</summary>
        public int leadsTo = -1;

        SpriteRenderer _sr, _glow;
        BoxCollider _col;

        public SpriteRenderer Renderer => _sr;

        public void Init(Sprite s, int order)
        {
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = order;
            var g = new GameObject("glow");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = new Vector3(0, 0, -0.01f);
            _glow = g.AddComponent<SpriteRenderer>();
            _glow.sortingOrder = order + 1;
            _glow.enabled = false;
            _col = gameObject.AddComponent<BoxCollider>();
            SetSprite(s);
        }

        public void SetSprite(Sprite s)
        {
            _sr.sprite = s;
            _glow.sprite = s != null ? Art.GlowOf(s, s.texture.GetInstanceID().ToString()) : null;
            if (s != null)
            {
                var b = s.bounds;
                _col.center = new Vector3(b.center.x, b.center.y, 0);
                _col.size = new Vector3(Mathf.Max(b.size.x, 0.6f), Mathf.Max(b.size.y, 0.6f), 0.4f);
            }
        }

        public void SetVisible(bool on)
        {
            _sr.enabled = on;
            _col.enabled = on && interactable;
            if (!on) _glow.enabled = false;
        }

        public void SetInteractable(bool on)
        {
            interactable = on;
            _col.enabled = on && _sr.enabled;
            if (!on) _glow.enabled = false;
        }

        public void SetHighlight(bool on, float alpha = 1f)
        {
            _glow.enabled = on && _sr.enabled && interactable;
            _glow.color = new Color(1, 1, 1, alpha);
        }

        /// <summary>Top centre in world space, for the hover label.</summary>
        public Vector3 LabelAnchor
        {
            get
            {
                if (_sr.sprite == null) return transform.position;
                var b = _sr.bounds;
                return new Vector3(b.center.x, b.max.y + 0.15f, b.center.z);
            }
        }
    }

    public enum RoomKind { Ship, Hab, Outside }

    public class Room
    {
        public int index;
        public string nameKey;
        public RoomKind kind;
        public float centerX;
        public Transform root;
        public readonly List<Hotspot> hotspots = new List<Hotspot>();
        public SpriteRenderer alarmLight;
    }

    /// <summary>Builds the 2.5D rooms: a cross-section box (back wall, floor, ceiling, side walls, hull cut) plus props.</summary>
    public class WorldBuilder
    {
        public const float RoomW = 10f, RoomH = 6f, ZFront = -1.5f, ZBack = 2f;
        public const int OrderBackdrop = -300, OrderWall = -100, OrderProp = -20, OrderPropFront = -5, OrderPlayer = 10, OrderFx = 30, OrderHull = 60;

        readonly Transform _root;
        readonly float _spacing;
        public readonly List<Room> rooms = new List<Room>();
        public readonly List<Hotspot> all = new List<Hotspot>();

        public WorldBuilder(Transform root, float spacing)
        {
            _root = root;
            _spacing = spacing;
        }

        public Room AddRoom(string nameKey, RoomKind kind, Sprite backWall)
        {
            var r = new Room { index = rooms.Count, nameKey = nameKey, kind = kind, centerX = rooms.Count * _spacing };
            var go = new GameObject("Room_" + nameKey);
            go.transform.SetParent(_root, false);
            r.root = go.transform;
            rooms.Add(r);
            float cx = r.centerX;

            if (kind == RoomKind.Outside)
            {
                return r;
            }

            bool mars = kind == RoomKind.Hab;
            var wall = Quad(r, "back", backWall, new Vector3(cx, 0, ZBack), Quaternion.identity, OrderWall);
            wall.color = mars ? new Color(.84f, .80f, .75f) : new Color(.82f, .86f, .94f);
            var bounce = Quad(r, "overhead-light-bounce", Art.SoftLight(),
                new Vector3(cx, 4.35f, ZBack - .015f), Quaternion.identity, OrderWall + 1);
            bounce.transform.localScale = new Vector3(8.5f, 3.2f, 1f);
            bounce.color = mars ? new Color(1f, .79f, .60f, .21f) : new Color(.72f, .86f, 1f, .19f);
            Quad(r, "floor", Art.Floor(mars), new Vector3(cx, 0, (ZFront + ZBack) / 2f), Quaternion.Euler(90, 0, 0), OrderWall + 2);
            Quad(r, "ceil", Art.Ceiling(), new Vector3(cx, RoomH, (ZFront + ZBack) / 2f), Quaternion.Euler(90, 0, 0), OrderWall + 2);
            Quad(r, "sideL", Art.SideWall(), new Vector3(cx - RoomW / 2f, 0, (ZFront + ZBack) / 2f), Quaternion.Euler(0, 90, 0), OrderWall + 1);
            Quad(r, "sideR", Art.SideWall(), new Vector3(cx + RoomW / 2f, 0, (ZFront + ZBack) / 2f), Quaternion.Euler(0, -90, 0), OrderWall + 1);

            // the cut hull around the cross-section, face-on at the front
            Quad(r, "hullB", Art.HullStrip(184, 10), new Vector3(cx - 5.75f, -0.62f, ZFront), Quaternion.identity, OrderHull);
            Quad(r, "hullT", Art.HullStrip(184, 10), new Vector3(cx - 5.75f, RoomH, ZFront), Quaternion.identity, OrderHull);
            Quad(r, "hullL", Art.HullStrip(10, 110), new Vector3(cx - 5.75f, -0.62f, ZFront), Quaternion.identity, OrderHull);
            Quad(r, "hullR", Art.HullStrip(10, 110), new Vector3(cx + 5.12f, -0.62f, ZFront), Quaternion.identity, OrderHull);

            // red alarm wash, off until something goes wrong
            var alarm = Quad(r, "alarm", WhitePixel(), new Vector3(cx, RoomH / 2f, ZBack - 0.05f), Quaternion.identity, OrderProp + 14);
            alarm.transform.localScale = new Vector3(RoomW * 16f, RoomH * 16f, 1);
            alarm.color = new Color(1f, 0.1f, 0.15f, 0f);
            r.alarmLight = alarm;
            return r;
        }

        static Sprite _white;
        public static Sprite WhitePixel()
        {
            if (_white != null) return _white;
            var c = new PixelCanvas(1, 1);
            c.Set(0, 0, new Color32(255, 255, 255, 255));
            _white = c.ToSprite(new Vector2(0.5f, 0.5f));
            return _white;
        }

        public SpriteRenderer Quad(Room r, string name, Sprite s, Vector3 pos, Quaternion rot, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(r != null ? r.root : _root, false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Decoration with no interaction. x is relative to the room centre.</summary>
        public SpriteRenderer Deco(Room r, Sprite s, float x, float y, float z, int order = OrderProp)
        {
            return Quad(r, "deco", s, new Vector3(r.centerX + x, y, z), Quaternion.identity, order);
        }

        /// <summary>Add a hotspot. x is relative to the room centre; standX defaults to just left of it.</summary>
        public Hotspot Spot(Room r, string id, string nameKey, Sprite s, float x, float y, float z, float? standOffset = null, int order = OrderProp)
        {
            var go = new GameObject("HS_" + id);
            go.transform.SetParent(r.root, false);
            go.transform.position = new Vector3(r.centerX + x, y, z);
            var h = go.AddComponent<Hotspot>();
            h.id = id;
            h.nameKey = nameKey;
            h.room = r.index;
            float off = standOffset ?? -0.9f;
            h.standX = Mathf.Clamp(r.centerX + x + off, r.centerX - RoomW / 2f + 0.6f, r.centerX + RoomW / 2f - 0.6f);
            h.Init(s, order);
            r.hotspots.Add(h);
            all.Add(h);
            return h;
        }

        /// <summary>A door to another room, at the left (-1) or right (+1) end of the back wall.</summary>
        public Hotspot DoorTo(Room r, int side, int target, string nameKey, bool airlock = false)
        {
            float x = side * (RoomW / 2f - 0.9f);
            var h = Spot(r, $"door_{r.index}_{target}", nameKey, Art.Door(false, airlock), x, 0, ZBack - 0.1f, 0f);
            h.leadsTo = target;
            return h;
        }

        public Hotspot Find(string id) => all.Find(h => h.id == id);
    }
}
