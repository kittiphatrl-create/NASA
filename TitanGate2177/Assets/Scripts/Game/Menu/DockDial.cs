// The docking-ring menu: a metal ring sits off the left edge; menu items ride on an arc just
// outside it. Choosing an item turns the whole ring so that item docks at the fixed clamp
// (3 o'clock). The list wraps around, so items leaving one end come back at the other.
// The ring moves on a slightly under-damped spring (it overshoots a touch and settles like a
// heavy mechanism); docking fires a lock flash, a clamp "punch", sparks and a sheen on the label.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using T = TitanGate.Game.Menu.MenuTokens;

namespace TitanGate.Game.Menu
{
    /// <summary>One label on the ring. Only the docked item shows its sub-label.</summary>
    public class DialItem : MenuItemBase
    {
        public readonly Label Label, Sub;
        readonly Image _lock;
        readonly VisualElement _row, _swash, _sheen;
        float _weight;            // 0 far from the clamp .. 1 docked
        float _dockT = -1f;       // time since this item docked (drives the slide-in and the sheen)

        public DialItem(string id, string label, string sub) : base(id)
        {
            style.position = Position.Absolute;
            style.transformOrigin = new TransformOrigin(0, Length.Percent(50));
            _row = new VisualElement { pickingMode = PickingMode.Ignore };
            _row.style.flexDirection = FlexDirection.Row; _row.style.alignItems = Align.Center;
            _lock = new Image { image = MenuIcons.Get("lock"), pickingMode = PickingMode.Ignore };
            _lock.style.width = _lock.style.height = 22; _lock.style.marginRight = T.S1; _lock.style.display = DisplayStyle.None;
            _row.Add(_lock);
            Label = T.Text(label, MenuFont.Menu, T.ButtonSize, T.TextPrimary);
            Label.style.whiteSpace = WhiteSpace.NoWrap; T.Shadow(Label, 10, 0.85f);
            _row.Add(Label);
            _sheen = new VisualElement { pickingMode = PickingMode.Ignore };
            _sheen.style.position = Position.Absolute; _sheen.style.top = -14; _sheen.style.bottom = -14; _sheen.style.width = 90;
            _sheen.style.backgroundImage = new StyleBackground(MenuFx.Radial());
            _sheen.style.unityBackgroundImageTintColor = new Color(1f, 0.95f, 0.82f, 0.45f); _sheen.style.opacity = 0;
            _row.Add(_sheen);
            Add(_row);
            _swash = new VisualElement { pickingMode = PickingMode.Ignore };
            _swash.style.height = 2; _swash.style.marginTop = 2; _swash.style.width = Length.Percent(100);
            _swash.style.backgroundColor = T.AccentHot; _swash.style.opacity = 0; T.Transition(_swash, T.Fast, "opacity");
            Add(_swash);
            Sub = T.Text(sub ?? "", MenuFont.Ui, T.SubSize, new Color(0.93f, 0.8f, 0.6f));
            Sub.style.whiteSpace = WhiteSpace.NoWrap; Sub.style.marginTop = 2; Sub.style.opacity = 0; T.Shadow(Sub, 8, 0.9f);
            Add(Sub);
        }

        public void SetWeight(float w) { if (Mathf.Abs(w - _weight) < 0.004f) return; _weight = w; Refresh(); }

        /// <summary>Docked: the label slides in a few pixels and a light sweeps across it.</summary>
        public void PlayDock(bool reduced) { _dockT = reduced ? 10f : 0f; }

        public void Animate(float dt)
        {
            if (_dockT < 0 || _dockT > 1.2f) { _row.style.translate = new Translate(0, 0); _sheen.style.opacity = 0; return; }
            _dockT += dt;
            float slide = 1f - T.Ease(_dockT / 0.35f);
            _row.style.translate = new Translate(-14f * slide, 0);
            float w = Label.resolvedStyle.width; if (float.IsNaN(w) || w <= 0) w = 200;
            float k = Mathf.Clamp01((_dockT - 0.08f) / 0.6f);
            _sheen.style.left = Mathf.Lerp(-90f, w + 10f, k);
            _sheen.style.opacity = k > 0 && k < 1 ? Mathf.Sin(k * Mathf.PI) : 0;
            Sub.style.translate = new Translate(0, 8f * (1f - T.Ease((_dockT - 0.1f) / 0.35f)));
            float bloom = Mathf.Sin(Mathf.Clamp01(_dockT / 0.7f) * Mathf.PI);
            Label.style.textShadow = new TextShadow { offset = new Vector2(0, 2), blurRadius = 22 + 26 * bloom, color = new Color(0.75f, 0.4f, 0.08f, 0.9f + 0.1f * bloom) };
        }

        protected override void Refresh()
        {
            _lock.style.display = Interactable ? DisplayStyle.None : DisplayStyle.Flex;
            var c = Color.Lerp(new Color(0.88f, 0.8f, 0.68f), new Color(1f, 0.83f, 0.5f), _weight);
            if (hover && Interactable) c = Color.Lerp(c, T.AccentHot, 0.6f);
            Label.style.color = c;
            _lock.tintColor = c;
            Label.style.textShadow = new TextShadow { offset = new Vector2(0, 2), blurRadius = 10 + 12 * _weight, color = Color.Lerp(new Color(0, 0, 0, 0.85f), new Color(0.55f, 0.28f, 0.05f, 0.9f), _weight) };
            _swash.style.opacity = focusVisible && Interactable ? 1 : 0;
        }
    }

    public class DockDial : VisualElement
    {
        public readonly List<DialItem> Items = new List<DialItem>();
        public int Selected { get; private set; }
        public event Action<int> SelectionChanged;
        /// <summary>Fires each time an item passes the clamp while the ring turns (ratchet sound).</summary>
        public event Action Ticked;
        /// <summary>Fires once when the ring comes to rest with an item in the clamp.</summary>
        public event Action<int> Docked;
        /// <summary>Degrees between neighbouring items on the ring.</summary>
        public float StepDeg = 10f;
        /// <summary>The ring turns 30° per item (gear ratio 3:1) so a latch always docks in the clamp.</summary>
        public float RingStepDeg = 30f;
        /// <summary>Extra ring spin used by the intro and the departure (degrees).</summary>
        public float Spin;
        /// <summary>0..1 visibility of the items (the intro staggers them by distance from the clamp).</summary>
        public float Reveal = 1f;
        /// <summary>Sparks and the moving glints (off at Low effects quality).</summary>
        public bool RichFx = true;
        /// <summary>Ring speed in items per second (drives the servo sound and the halo).</summary>
        public float Speed => Mathf.Abs(_v);
        public bool Settled => Mathf.Abs(_s - _target) < 0.02f && Mathf.Abs(_v) < 0.15f;

        readonly Image _ring, _clamp;
        readonly VisualElement _halo, _flash, _light, _sparkLayer, _shock;
        readonly float _ringOuter, _ringPx;
        readonly Vector2 _clampAnchor, _clampSize, _clampLight, _clampTip;
        float _s, _v, _target;                 // ring position / velocity / target, in items (unwrapped)
        float _cx, _cy, _r, _ir, _u = 1f, _clampK = 1f;
        int _lastPassed = int.MinValue; float _dockedFor = float.NaN;
        float _flashT = 9f, _punchT = 9f, _time, _haloPulse, _grind, _shockT = 9f, _shockPower = 1f;
        readonly List<(VisualElement e, Vector2 p, Vector2 v, float life, float age)> _sparks = new List<(VisualElement, Vector2, Vector2, float, float)>();
        readonly System.Random _rng = new System.Random(3);

        public DockDial(Texture2D ring, Texture2D clamp, float ringOuter, Vector2 clampAnchorPx, Vector2 clampLightPx, Vector2 clampTipPx)
        {
            pickingMode = PickingMode.Ignore;
            T.Fill(this);
            _ringOuter = ringOuter > 0.1f ? ringOuter : 0.97f;
            _halo = Glow(new Color(1f, 0.62f, 0.25f, 0.22f)); Add(_halo);
            _ring = new Image { image = ring != null ? ring : MenuFx.DockRingFallback(), scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
            _ringPx = ring != null ? ring.width : 0f;
            _ring.style.position = Position.Absolute;
            Add(_ring);
            _flash = Glow(new Color(1f, 0.85f, 0.55f, 0.9f)); _flash.style.opacity = 0; Add(_flash);
            _clamp = new Image { image = clamp, scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
            _clamp.style.position = Position.Absolute; _clamp.style.display = clamp != null ? DisplayStyle.Flex : DisplayStyle.None;
            _clampSize = clamp != null ? new Vector2(clamp.width, clamp.height) : Vector2.zero;
            _clampAnchor = clamp != null ? (clampAnchorPx == Vector2.zero ? new Vector2(clamp.width * 0.27f, clamp.height * 0.5f) : clampAnchorPx) : Vector2.zero;
            _clampLight = clamp != null ? (clampLightPx == Vector2.zero ? new Vector2(clamp.width * 0.52f, clamp.height * 0.5f) : clampLightPx) : Vector2.zero;
            _clampTip = clamp != null ? (clampTipPx == Vector2.zero ? new Vector2(clamp.width * 0.81f, clamp.height * 0.5f) : clampTipPx) : Vector2.zero;
            Add(_clamp);
            _light = Glow(new Color(1f, 0.6f, 0.15f, 1f)); _light.style.opacity = 0.4f; Add(_light);
            _shock = new VisualElement { pickingMode = PickingMode.Ignore }; _shock.style.position = Position.Absolute;
            T.Round(_shock, 9999); T.Stroke(_shock, new Color(1f, 0.82f, 0.48f, 1f), 2.5f); _shock.style.opacity = 0;
            Add(_shock);
            _sparkLayer = T.Fill(); Add(_sparkLayer);
        }

        static VisualElement Glow(Color tint)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute;
            e.style.backgroundImage = new StyleBackground(MenuFx.Radial());
            e.style.unityBackgroundImageTintColor = tint;
            return e;
        }

        static void Place(VisualElement e, float cx, float cy, float size)
        {
            e.style.width = e.style.height = size; e.style.left = cx - size / 2; e.style.top = cy - size / 2;
        }

        public DialItem AddItem(string id, string label, string sub)
        {
            var it = new DialItem(id, label, sub);
            Items.Add(it); Insert(IndexOf(_sparkLayer), it);
            return it;
        }

        static float Wrap(float x, float n) { x %= n; if (x < -n / 2f) x += n; if (x >= n / 2f) x -= n; return x; }
        int Index(float unwrapped) { int n = Items.Count; return ((Mathf.RoundToInt(unwrapped) % n) + n) % n; }

        /// <summary>Select an item, turning the shorter way round.</summary>
        public void Select(int i, bool animate = true)
        {
            if (Items.Count == 0) return;
            int n = Items.Count;
            float delta = Wrap(i - Index(_target), n);
            _target += delta;
            if (!animate) { _s = _target; _v = 0; _dockedFor = _target; _lastPassed = Mathf.RoundToInt(_s); }
            if (Selected != i) { Selected = i; SelectionChanged?.Invoke(i); }
        }

        /// <summary>Move to the next interactable item in a direction (+1 = down the ring), wrapping.</summary>
        public void Step(int dir)
        {
            int n = Items.Count, i = Selected;
            for (int k = 0; k < n; k++) { i = (i + dir + n) % n; if (Items[i].Interactable) break; }
            Select(i);
        }

        /// <summary>The clamp grips: a short punch and a flash (used when an item is chosen).</summary>
        public void Punch() { _punchT = 0; _flashT = 0; _haloPulse = 1f; _shockT = 0; _shockPower = 1.4f; Burst(26, 1.35f); }

        /// <summary>Place the ring for the panel size: scaled from a 1080-high layout, anchored to the left safe area.</summary>
        public void Layout(float w, float h)
        {
            if (w <= 0 || h <= 0) return;
            _u = h / 1080f;
            _r = 340f * _u;                       // ring outer radius
            _ir = 440f * _u;                      // where the labels start: clear of the clamp tip
            float apexX = w * T.Safe + 240f * _u; // the docked label's left edge
            _cx = apexX - _ir; _cy = h * 0.62f;
            float size = 2f * _r / _ringOuter;
            Place(_ring, _cx, _cy, size);
            Place(_halo, _cx, _cy, size * 1.25f);
            Place(_flash, _cx + _r, _cy, 320f * _u);
            if (_clampSize.x > 0)
            {
                // the clamp is authored at the ring's scale; without a ring render, size it to about 96 px at 1080p
                _clampK = _ringPx > 0 ? size / _ringPx : 96f * _u / _clampSize.x;
                _clamp.style.width = _clampSize.x * _clampK; _clamp.style.height = _clampSize.y * _clampK;
                _clamp.style.left = _cx + _r - _clampAnchor.x * _clampK; _clamp.style.top = _cy - _clampAnchor.y * _clampK;
                Place(_light, _clamp.style.left.value.value + _clampLight.x * _clampK, _cy - _clampAnchor.y * _clampK + _clampLight.y * _clampK, 46f * _u);
            }
            else _light.style.display = DisplayStyle.None;
        }

        Vector2 TipPos() => _clampSize.x > 0
            ? new Vector2(_cx + _r + (_clampTip.x - _clampAnchor.x) * _clampK, _cy + (_clampTip.y - _clampAnchor.y) * _clampK)
            : new Vector2(_cx + _r + 20f * _u, _cy);

        /// <summary>Gold sparks fanning out of the clamp. power scales size and speed.</summary>
        void Burst(int count, float power = 1f, Vector2? from = null, float spread = 2.2f, float baseAngle = 0f)
        {
            if (!RichFx || _sparks.Count > 90) return;
            var origin = from ?? TipPos();
            for (int i = 0; i < count; i++)
            {
                var e = new VisualElement { pickingMode = PickingMode.Ignore }; e.style.position = Position.Absolute;
                float sz = (5f + (float)_rng.NextDouble() * 9f) * _u * power; e.style.width = e.style.height = sz;
                e.style.translate = new Translate(-sz / 2, -sz / 2);
                e.style.backgroundImage = new StyleBackground(MenuFx.Radial());   // soft core reads as a hot ember
                e.style.unityBackgroundImageTintColor = Color.Lerp(new Color(1f, 0.62f, 0.2f), new Color(1f, 0.96f, 0.82f), (float)_rng.NextDouble());
                _sparkLayer.Add(e);
                float ang = baseAngle + ((float)_rng.NextDouble() - 0.5f) * spread;
                float sp = (160f + (float)_rng.NextDouble() * 320f) * _u * power;
                _sparks.Add((e, origin, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * sp, 0.55f + (float)_rng.NextDouble() * 0.5f, 0f));
            }
        }

        public void Tick(float dt, bool reduced)
        {
            if (Items.Count == 0) return;
            _time += dt;
            // heavy mechanism: a spring with a little overshoot (snaps straight to the target with Reduce Motion)
            if (reduced) { _s = _target; _v = 0; }
            else
            {
                const float k = 150f, c = 15.5f;   // damping ratio about 0.63
                for (int i = 0; i < 4; i++)
                {
                    float h = dt / 4f;
                    _v += (k * (_target - _s) - c * _v) * h;
                    _s += _v * h;
                }
                if (Mathf.Abs(_s - _target) < 0.0005f && Mathf.Abs(_v) < 0.01f) { _s = _target; _v = 0; }
            }
            int passed = Mathf.RoundToInt(_s);
            if (_lastPassed == int.MinValue) _lastPassed = passed;
            if (passed != _lastPassed) { _lastPassed = passed; Ticked?.Invoke(); }
            if (Settled && !Mathf.Approximately(_dockedFor, _target))
            {
                _dockedFor = _target;
                _flashT = 0; _punchT = 0.06f; _haloPulse = 0.7f; _shockT = 0; _shockPower = 1f;
                Burst(16, 1.1f);
                Items[Selected].PlayDock(reduced);
                Docked?.Invoke(Selected);
            }

            float sway = reduced ? 0 : Mathf.Sin(_time * 0.6f) * 0.35f;   // the ring never sits perfectly dead
            _ring.style.rotate = new Rotate(new Angle(-_s * RingStepDeg + Spin + sway, AngleUnit.Degree));
            _haloPulse = Mathf.MoveTowards(_haloPulse, 0f, dt * 1.6f);
            _halo.style.unityBackgroundImageTintColor = new Color(1f, 0.62f, 0.25f, 0.2f + Mathf.Clamp01(Speed / 5f) * 0.18f + _haloPulse * 0.3f);
            if (!reduced && Speed > 1.5f)
            {
                // metal on metal: the faster the ring turns, the more sparks the clamp throws (up or down, against the motion)
                _grind += dt * Speed * 3.2f;
                while (_grind >= 1f)
                {
                    _grind -= 1f;
                    var rim = new Vector2(_cx + _r, _cy);
                    Burst(1, 0.7f, rim, 0.9f, _v > 0 ? -1.2f : 1.2f);
                }
            }

            // lock flash at the clamp, clamp punch, indicator light
            _flashT += dt;
            _flash.style.opacity = _flashT < 0.6f ? Mathf.Pow(1f - _flashT / 0.6f, 1.6f) : 0f;
            _shockT += dt;
            if (_shockT < 0.55f && RichFx)   // off with Reduce Motion and at Low quality
            {
                float k = _shockT / 0.55f, e = 1f - Mathf.Pow(1f - k, 3f);
                float d = Mathf.Lerp(30f, 260f * _shockPower, e) * _u;
                var c = new Vector2(_cx + _r, _cy);
                _shock.style.width = _shock.style.height = d; _shock.style.left = c.x - d / 2; _shock.style.top = c.y - d / 2;
                _shock.style.opacity = (1f - k) * 0.85f;
                float bw = Mathf.Lerp(4f, 1f, k) * _u;
                _shock.style.borderTopWidth = _shock.style.borderBottomWidth = _shock.style.borderLeftWidth = _shock.style.borderRightWidth = bw;
            }
            else _shock.style.opacity = 0;
            _punchT += dt;
            float punch = _punchT < 0.22f ? Mathf.Sin(_punchT / 0.22f * Mathf.PI) : 0f;
            _clamp.style.translate = new Translate(-7f * _u * punch, 0);
            float idle = 0.35f + 0.15f * Mathf.Sin(_time * 2.2f);
            _light.style.opacity = Mathf.Max(idle, _flash.style.opacity.value) + (Settled ? 0.15f : 0f);
            _light.style.translate = new Translate(-7f * _u * punch, 0);

            for (int i = _sparks.Count - 1; i >= 0; i--)
            {
                var (e, p, v, life, age) = _sparks[i];
                age += dt; v.y += 520f * _u * dt; v *= 1f - 1.8f * dt; p += v * dt;
                if (age >= life) { e.RemoveFromHierarchy(); _sparks.RemoveAt(i); continue; }
                e.style.left = p.x; e.style.top = p.y; e.style.opacity = 1f - age / life;
                _sparks[i] = (e, p, v, life, age);
            }

            int n = Items.Count;
            // the docked item's sub-label needs room: items below it move a little further round the ring
            var dockedItem = Items[Selected];
            float subOpen = string.IsNullOrEmpty(dockedItem.Sub.text) ? 0f : dockedItem.Sub.resolvedStyle.opacity;
            for (int i = 0; i < n; i++)
            {
                var it = Items[i];
                float slot = Wrap(i - _s, n);
                slot += Mathf.Clamp01(slot) * subOpen * 0.6f;
                float a = slot * StepDeg * Mathf.Deg2Rad, ad = Mathf.Abs(slot);
                float x = _cx + _ir * Mathf.Cos(a), y = _cy + _ir * Mathf.Sin(a);
                float labelH = it.Label.resolvedStyle.height; if (float.IsNaN(labelH) || labelH <= 0) labelH = 44f;
                float sc = Mathf.Max(0.68f, 1.1f - 0.16f * Mathf.Min(ad, 1f) - 0.07f * Mathf.Max(ad - 1f, 0f)) * _u;   // the docked item reads larger
                it.style.left = x; it.style.top = y - labelH * 0.5f * sc;
                it.style.scale = new Scale(new Vector3(sc, sc, 1));
                float reveal = Mathf.Clamp01(Reveal * 4f - ad * 0.7f);   // the intro brings items in from the clamp outward
                float fade = Mathf.Pow(Mathf.Clamp01(1f - ad / 3.6f), 0.8f) * reveal;
                it.style.opacity = fade * (it.Interactable ? 1f : 0.45f);
                it.pickingMode = fade > 0.15f ? PickingMode.Position : PickingMode.Ignore;
                it.SetWeight(Mathf.Clamp01(1f - ad));
                it.Animate(dt);
                bool docked = i == Selected && Settled;
                it.Sub.style.opacity = Mathf.MoveTowards(it.Sub.resolvedStyle.opacity, docked ? 1f : 0f, dt * 5f);
                it.Sub.style.display = string.IsNullOrEmpty(it.Sub.text) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        /// <summary>Signed position of an item relative to the clamp, in items (0 = docked).</summary>
        public float SlotOf(int i) => Wrap(i - _s, Items.Count);
    }
}
