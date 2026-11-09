// LAST 72 HOURS — graybox astronaut that walks anywhere on the 2.5D room floor (scales with depth).
// Pure visuals + movement; GameUI decides what happens on arrival.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Last72
{
    public class Astronaut : MonoBehaviour
    {
        public const float BaseScale = 1.25f;  // size at the front edge of the floor
        public const float SpeedX = 0.42f;     // floor widths per second
        public const float SpeedD = 0.55f;     // floor depths per second

        public Vector2 Pos = new Vector2(0.5f, 0.3f); // floor point: x across, y = depth d
        public float X { get { return Pos.x; } }
        public float D { get { return Pos.y; } }
        public Vector2 ManualMove;      // -1..1 per axis (x = A/D, y = W/S), set every frame by the controller
        public int Facing = 1;
        public bool FacingAway;         // walking into the room: show the back of the helmet
        public bool IsWalking { get; private set; }
        public bool HasTarget { get { return path.Count > 0; } }
        /// Input pushed against the edge of the floor this frame (door triggers use it).
        public Vector2 Push { get; private set; }

        public RoomShape Shape = new RoomShape();
        public Func<Vector2, bool> IsFree;

        RectTransform root, body, legL, legR, head, visor, glint, badge;
        readonly List<Vector2> path = new List<Vector2>();
        Action onArrive;
        float walkPhase, bumpUntil;

        public static Astronaut Build(RectTransform parent)
        {
            var go = new GameObject("Astronaut", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<Astronaut>();
            a.root = (RectTransform)go.transform;
            a.root.pivot = new Vector2(0.5f, 0f);
            a.root.sizeDelta = new Vector2(48, 120);

            Color suit = new Color(0.86f, 0.87f, 0.82f);
            Color dark = new Color(0.55f, 0.57f, 0.55f);
            a.legL = Part(a.root, "LegL", -9, 32, 13, 32, dark, new Vector2(0.5f, 1f));
            a.legR = Part(a.root, "LegR", 9, 32, 13, 32, dark, new Vector2(0.5f, 1f));
            a.body = Part(a.root, "Body", 0, 28, 40, 54, suit, new Vector2(0.5f, 0f));
            Part(a.body, "Pack", -26, 8, 12, 38, dark, new Vector2(0.5f, 0f));
            a.badge = Part(a.body, "Badge", 8, 30, 10, 8, new Color(1f, 0.55f, 0.2f), new Vector2(0.5f, 0f));
            a.head = Part(a.root, "Helmet", 0, 80, 36, 34, suit, new Vector2(0.5f, 0f));
            a.visor = Part(a.head, "Visor", 7, 9, 22, 15, new Color(0.18f, 0.48f, 0.80f), new Vector2(0.5f, 0f));
            a.glint = Part(a.head, "Glint", 12, 18, 5, 4, new Color(1, 1, 1, 0.8f), new Vector2(0.5f, 0f));
            // soft floor shadow (sells the depth)
            var sh = Part(a.root, "Shadow", 0, -4, 44, 8, new Color(0, 0, 0, 0.35f), new Vector2(0.5f, 0f));
            sh.SetAsFirstSibling();

            // friend's art (Resources/Art/astronaut.png, facing right, feet at the bottom edge) replaces the rect body
            var art = ArtSlots.Get("astronaut");
            if (art != null)
            {
                foreach (var img in go.GetComponentsInChildren<Image>()) if (img.name != "Shadow") img.enabled = false;
                float h = 130f, w = h * art.rect.width / art.rect.height;
                var pic = Part(a.root, "Sprite", 0, 0, w, h, Color.white, new Vector2(0.5f, 0f));
                pic.GetComponent<Image>().sprite = art;
            }
            a.Apply(Vector2.zero, 0);
            return a;
        }

        // Child rect positioned in pixels relative to parent's bottom-center.
        static RectTransform Part(RectTransform parent, string name, float x, float y, float w, float h, Color c, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = pivot;
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            var img = go.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return rt;
        }

        public void Place(Vector2 p)
        {
            Cancel();
            Pos = new Vector2(Mathf.Clamp01(p.x), Mathf.Clamp01(p.y));
            Apply(Vector2.zero, 0);
        }

        /// Follow waypoints, then invoke arrive (immediately if already there).
        public void WalkPath(List<Vector2> points, Action arrive)
        {
            path.Clear();
            if (points != null) path.AddRange(points);
            onArrive = arrive;
            if (path.Count == 0 || (path.Count == 1 && (path[0] - Pos).magnitude < 0.01f)) { path.Clear(); Arrive(); }
        }

        public void Cancel() { path.Clear(); onArrive = null; }

        public void Bump() { bumpUntil = Time.unscaledTime + 0.45f; }

        void Arrive()
        {
            var cb = onArrive;
            path.Clear(); onArrive = null;
            if (cb != null) cb();
        }

        bool Free(Vector2 p) { return IsFree == null || IsFree(p); }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            Vector2 move = Vector2.zero;
            Push = Vector2.zero;
            if (ManualMove.sqrMagnitude > 0.0001f)
            {
                Cancel(); // keyboard overrides click-walk
                var dir = Vector2.ClampMagnitude(ManualMove, 1f);
                move = new Vector2(dir.x * SpeedX, dir.y * SpeedD) * dt;
                var want = Pos + move;
                if (want.x < 0 || want.x > 1) Push = new Vector2(Mathf.Sign(dir.x), Push.y);
                if (want.y < 0 || want.y > 1) Push = new Vector2(Push.x, Mathf.Sign(dir.y));
            }
            else if (path.Count > 0)
            {
                var tgt = path[0];
                var delta = tgt - Pos;
                // normalise so depth and width feel the same speed
                var scaled = new Vector2(delta.x / SpeedX, delta.y / SpeedD);
                float timeLeft = scaled.magnitude;
                if (timeLeft <= dt)
                {
                    move = delta;
                    path.RemoveAt(0);
                }
                else move = delta * (dt / timeLeft);
            }
            var before = Pos;
            Vector2 next = new Vector2(Mathf.Clamp01(Pos.x + move.x), Mathf.Clamp01(Pos.y + move.y));
            if (Free(next)) Pos = next;
            else if (Free(new Vector2(next.x, Pos.y))) Pos = new Vector2(next.x, Pos.y);   // slide along furniture
            else if (Free(new Vector2(Pos.x, next.y))) Pos = new Vector2(Pos.x, next.y);
            else if (path.Count > 0) path.RemoveAt(0); // stuck on a waypoint: skip it
            var moved = Pos - before;
            if (path.Count == 0 && onArrive != null && ManualMove.sqrMagnitude < 0.0001f) { Arrive(); }
            Apply(moved, dt);
        }

        void Apply(Vector2 moved, float dt)
        {
            IsWalking = moved.sqrMagnitude > 1e-10f;
            if (IsWalking)
            {
                if (Mathf.Abs(moved.x) > 1e-5f) Facing = moved.x > 0 ? 1 : -1;
                if (Mathf.Abs(moved.y) / SpeedD > Mathf.Abs(moved.x) / SpeedX * 0.8f) FacingAway = moved.y > 0;
                else FacingAway = false;
                walkPhase += dt * 11f;
            }
            else walkPhase = Mathf.MoveTowards(walkPhase, Mathf.Round(walkPhase / Mathf.PI) * Mathf.PI, dt * 8f);

            float t = Time.unscaledTime;
            float sc = Shape.Scale(Pos.y) * BaseScale;
            float bob = IsWalking ? Mathf.Abs(Mathf.Sin(walkPhase)) * 6f * sc : 0f;
            float shake = 0;
            if (t < bumpUntil) shake = Mathf.Sin(t * 70f) * 7f * ((bumpUntil - t) / 0.45f);

            root.anchorMin = root.anchorMax = Shape.Project(Pos.x, Pos.y);
            root.anchoredPosition = new Vector2(shake, bob);
            root.localScale = new Vector3(Facing * sc, sc, 1);

            visor.gameObject.SetActive(!FacingAway);
            glint.gameObject.SetActive(!FacingAway);
            badge.gameObject.SetActive(!FacingAway);

            float swing = Mathf.Sin(walkPhase) * 28f;
            legL.localRotation = Quaternion.Euler(0, 0, swing);
            legR.localRotation = Quaternion.Euler(0, 0, -swing);
            float breathe = IsWalking ? 1f : 1f + Mathf.Sin(t * 2.2f) * 0.025f;
            body.localScale = new Vector3(1, breathe, 1);
            head.anchoredPosition = new Vector2(0, 80 + (breathe - 1) * 54f);
        }
    }

    /// Transparent click catcher: reports normalized x inside its rect.
    public class FloorClick : MonoBehaviour, IPointerClickHandler
    {
        public Action<Vector2> OnClickPoint; // normalised (0..1) point inside this rect

        public void OnPointerClick(PointerEventData e)
        {
            var rt = (RectTransform)transform;
            Vector2 lp;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out lp)) return;
            var n = new Vector2((lp.x - rt.rect.xMin) / rt.rect.width, (lp.y - rt.rect.yMin) / rt.rect.height);
            if (OnClickPoint != null) OnClickPoint(n);
        }
    }
}

namespace Last72
{
    /// Reports pointer enter/exit (for the hover "sentence line").
    public class HoverReport : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public System.Action<bool> OnHover;
        public void OnPointerEnter(PointerEventData e) { if (OnHover != null) OnHover(true); }
        public void OnPointerExit(PointerEventData e) { if (OnHover != null) OnHover(false); }
        void OnDisable() { if (OnHover != null) OnHover(false); }
    }
}
