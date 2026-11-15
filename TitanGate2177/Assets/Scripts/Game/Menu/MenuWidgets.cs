// Menu components: MenuButton (big primary/secondary card), MenuListItem (icon + label row),
// MenuChip (small top-right toggle), MenuTooltip and InputPrompts. Each interactive element
// has default, hover, focus-visible, pressed, disabled and selected states.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using T = TitanGate.Game.Menu.MenuTokens;

namespace TitanGate.Game.Menu
{
    public interface IMenuItem
    {
        string Id { get; }
        VisualElement Element { get; }
        bool Interactable { get; }
        string Tooltip { get; }
        string TooltipKey { get; }
        void SetHover(bool on);
        void SetFocus(bool on, bool visible);
        void SetPressed(bool on);
        void Submit();
        /// <summary>Optional explicit neighbours for left/right moves (ids).</summary>
        string Left { get; set; }
        string Right { get; set; }
    }

    /// <summary>Shared state machine and pointer wiring for every menu control.</summary>
    public abstract class MenuItemBase : VisualElement, IMenuItem
    {
        public string Id { get; }
        public VisualElement Element => this;
        public bool Interactable { get; private set; } = true;
        public string Tooltip { get; set; }
        public string TooltipKey { get; set; }
        public string Left { get; set; }
        public string Right { get; set; }
        public Action OnSubmit;
        public Action<IMenuItem> PointerEntered, PointerLeft, Clicked;

        protected bool hover, focused, focusVisible, pressed, selected;

        protected MenuItemBase(string id)
        {
            Id = id; name = "menu-" + id;
            focusable = false;   // the menu runs its own focus model (keyboard, gamepad, mouse-follow)
            RegisterCallback<PointerEnterEvent>(_ => PointerEntered?.Invoke(this));
            RegisterCallback<PointerLeaveEvent>(_ => { SetPressed(false); PointerLeft?.Invoke(this); });
            RegisterCallback<PointerDownEvent>(e => { if (e.button == 0 && Interactable) SetPressed(true); });
            RegisterCallback<PointerUpEvent>(e => { if (e.button == 0) SetPressed(false); });
            RegisterCallback<ClickEvent>(_ => Clicked?.Invoke(this));
        }

        public void SetInteractable(bool on) { Interactable = on; Refresh(); }
        public void SetSelected(bool on) { selected = on; Refresh(); }
        public void SetHover(bool on) { hover = on; Refresh(); }
        public void SetFocus(bool on, bool visible) { focused = on; focusVisible = on && visible; Refresh(); }
        public void SetPressed(bool on) { pressed = on && Interactable; Refresh(); }
        public void Submit() { if (Interactable) OnSubmit?.Invoke(); }
        protected abstract void Refresh();

        /// <summary>Active = hovered by mouse or focused (with or without a visible ring).</summary>
        protected bool Active => Interactable && (hover || focused);
    }

    /// <summary>Big 640x84 card: icon disc, label, sub-label. Primary = accent fill; Secondary = dark panel.</summary>
    public class MenuButton : MenuItemBase
    {
        readonly VisualElement _glow, _ring, _body, _disc;
        readonly Image _icon;
        readonly Label _label, _sub;
        string _iconId;
        bool _primary;

        public MenuButton(string id, string icon, string label, string sub, bool primary) : base(id)
        {
            _iconId = icon; _primary = primary;
            style.width = T.ButtonWidth; style.minHeight = T.ButtonHeight; style.marginBottom = T.S2;
            T.Transition(this, T.Fast, "scale", "opacity", "translate");

            _glow = T.Fill(); _glow.style.left = -40; _glow.style.right = -40; _glow.style.top = -34; _glow.style.bottom = -34;
            _glow.style.backgroundImage = new StyleBackground(MenuFx.Radial());
            _glow.style.opacity = 0; T.Transition(_glow, T.Base, "opacity");
            Add(_glow);

            _body = new VisualElement { pickingMode = PickingMode.Ignore };
            _body.style.flexGrow = 1; _body.style.flexDirection = FlexDirection.Row; _body.style.alignItems = Align.Center;
            _body.style.paddingLeft = T.S2 + 2; _body.style.paddingRight = T.S3; _body.style.paddingTop = _body.style.paddingBottom = T.S1;
            T.Round(_body, T.Radius); T.Stroke(_body, T.Border, T.BorderWidth);
            T.Transition(_body, T.Fast, "background-color", "border-top-color", "border-bottom-color", "border-left-color", "border-right-color");
            Add(_body);

            _disc = new VisualElement { pickingMode = PickingMode.Ignore };
            _disc.style.width = _disc.style.height = T.IconDisc; _disc.style.flexShrink = 0;
            _disc.style.alignItems = Align.Center; _disc.style.justifyContent = Justify.Center; T.Round(_disc, T.IconDisc / 2);
            _icon = new Image { pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit };
            _icon.style.width = _icon.style.height = T.Icon;
            T.Transition(_icon, T.Base, "translate");
            _disc.Add(_icon); _body.Add(_disc);

            var col = new VisualElement { pickingMode = PickingMode.Ignore };
            col.style.marginLeft = T.S2; col.style.flexShrink = 1; col.style.flexGrow = 1;
            _label = T.Text(label, MenuFont.Menu, T.ButtonSize, T.TextPrimary);
            _sub = T.Text(sub, MenuFont.Ui, T.SubSize, T.TextSecondary);
            col.Add(_label); col.Add(_sub); _body.Add(col);

            _ring = T.Fill(); _ring.style.left = _ring.style.right = _ring.style.top = _ring.style.bottom = -5;
            T.Round(_ring, T.Radius + 4); T.Stroke(_ring, T.AccentHot, T.FocusRing);
            _ring.style.opacity = 0; T.Transition(_ring, T.Fast, "opacity");
            Add(_ring);
            Refresh();
        }

        public void SetPrimary(bool primary) { _primary = primary; Refresh(); }
        public void SetSub(string sub) { _sub.text = sub; T.Apply(_sub, MenuFont.Ui, T.SubSize, _sub.resolvedStyle.color); Refresh(); }

        protected override void Refresh()
        {
            bool on = Active;
            bool primary = _primary && Interactable;
            Color fill = primary ? (on ? T.AccentHot : T.Accent) : (on ? T.PanelHover : T.Panel);
            _body.style.backgroundColor = fill;
            var edge = primary ? T.WithAlpha(T.AccentHot, on ? 1f : 0.7f) : (on ? T.WithAlpha(T.Accent, 0.85f) : T.Border);
            _body.style.borderTopColor = _body.style.borderBottomColor = _body.style.borderLeftColor = _body.style.borderRightColor = edge;
            _label.style.color = primary ? T.InkOnAccent : (on ? T.AccentHot : T.TextPrimary);
            _sub.style.color = primary ? T.InkOnAccentSoft : T.TextSecondary;
            _disc.style.backgroundColor = new Color(0, 0, 0, primary ? 0.18f : 0.35f);
            _icon.image = MenuIcons.Get(Interactable ? _iconId : "lock");
            _icon.tintColor = primary ? T.InkOnAccent : (on ? T.AccentHot : T.TextPrimary);
            _icon.style.translate = new Translate(on ? 3 : 0, 0);
            _ring.style.opacity = focusVisible && Interactable ? 1 : 0;
            _glow.style.unityBackgroundImageTintColor = T.WithAlpha(T.AccentHot, primary ? 0.55f : 0.35f);
            _glow.style.opacity = Interactable && (focusVisible || (primary && on)) ? 1 : 0;
            style.opacity = Interactable ? 1 : 0.4f;
            float s = !Interactable ? 1 : pressed ? T.PressScale : focusVisible ? T.FocusScale : 1;
            style.scale = new Scale(new Vector3(s, s, 1));
        }
    }

    /// <summary>Secondary menu row: 24 px line icon + label, 48 px tall (touch-safe).</summary>
    public class MenuListItem : MenuItemBase
    {
        readonly Image _icon; readonly Label _label; readonly VisualElement _ring;

        public MenuListItem(string id, string icon, string label) : base(id)
        {
            style.flexDirection = FlexDirection.Row; style.alignItems = Align.Center;
            style.height = T.ItemHeight; style.width = T.ItemWidth; style.paddingLeft = T.S2; style.paddingRight = T.S1;
            T.Transition(this, T.Fast, "scale");
            _icon = new Image { image = MenuIcons.Get(icon), pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit };
            _icon.style.width = _icon.style.height = T.Icon; _icon.style.flexShrink = 0;
            T.Transition(_icon, T.Base, "translate");
            Add(_icon);
            _label = T.Text(label, MenuFont.Menu, T.SecondarySize, T.TextPrimary);
            _label.style.marginLeft = T.S2; T.Shadow(_label, 8, 0.85f);
            Add(_label);
            _ring = T.Fill(); _ring.style.left = _ring.style.right = _ring.style.top = _ring.style.bottom = -2;
            T.Round(_ring, T.Radius); T.Stroke(_ring, T.AccentHot, T.FocusRing); _ring.style.opacity = 0; T.Transition(_ring, T.Fast, "opacity");
            Add(_ring);
            Refresh();
        }

        protected override void Refresh()
        {
            bool on = Active;
            _label.style.color = on || selected ? T.AccentHot : T.TextPrimary;
            _icon.tintColor = on || selected ? T.AccentHot : T.TextSecondary;
            _icon.style.translate = new Translate(on ? 4 : 0, 0);   // a small parallax nudge on the icon
            _ring.style.opacity = focusVisible ? 1 : 0;
            float s = pressed ? T.PressScale : focusVisible ? T.FocusScale : 1;
            style.scale = new Scale(new Vector3(s, s, 1));
            style.opacity = Interactable ? 1 : 0.4f;
        }
    }

    /// <summary>Small top-right toggle (language, sound, effects quality).</summary>
    public class MenuChip : MenuItemBase
    {
        readonly Image _icon; readonly Label _label; readonly VisualElement _ring;

        public MenuChip(string id, string icon, string label) : base(id)
        {
            style.flexDirection = FlexDirection.Row; style.alignItems = Align.Center; style.height = T.ItemHeight; style.minWidth = T.ItemHeight;
            style.paddingLeft = T.S2 - 2; style.paddingRight = T.S2; style.marginLeft = T.S1;
            T.Round(this, T.ItemHeight / 2); T.Stroke(this, T.Border, T.BorderWidth);
            T.Transition(this, T.Fast, "scale", "background-color");
            _icon = new Image { pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit }; _icon.style.width = _icon.style.height = 20; Add(_icon);
            _label = T.Text("", MenuFont.UiBold, T.ChipSize, T.TextPrimary); _label.style.marginLeft = T.S1; Add(_label);
            _ring = T.Fill(); _ring.style.left = _ring.style.right = _ring.style.top = _ring.style.bottom = -4;
            T.Round(_ring, T.ItemHeight / 2 + 4); T.Stroke(_ring, T.AccentHot, T.FocusRing); _ring.style.opacity = 0; T.Transition(_ring, T.Fast, "opacity");
            Add(_ring);
            Set(icon, label);
        }

        public void Set(string icon, string label)
        {
            _icon.image = MenuIcons.Get(icon);
            _label.text = label; T.Apply(_label, MenuFont.UiBold, T.ChipSize, T.TextPrimary);
            _label.style.display = string.IsNullOrEmpty(label) ? DisplayStyle.None : DisplayStyle.Flex;
            Refresh();
        }

        protected override void Refresh()
        {
            bool on = Active;
            style.backgroundColor = on ? T.PanelHover : T.Panel;
            _icon.tintColor = on || selected ? T.AccentHot : T.TextPrimary;
            if (_label != null) _label.style.color = on ? T.AccentHot : T.TextPrimary;
            _ring.style.opacity = focusVisible ? 1 : 0;
            float s = pressed ? T.PressScale : focusVisible ? T.FocusScale : 1;
            style.scale = new Scale(new Vector3(s, s, 1));
        }
    }

    /// <summary>One shared tooltip: label + keybind chip, shown after a short hover/focus delay.</summary>
    public class MenuTooltip : VisualElement
    {
        readonly Label _text, _key; readonly VisualElement _keyBox;
        IVisualElementScheduledItem _pending;

        public MenuTooltip()
        {
            pickingMode = PickingMode.Ignore; style.position = Position.Absolute;
            style.flexDirection = FlexDirection.Row; style.alignItems = Align.Center;
            style.paddingLeft = style.paddingRight = T.S2 - 4; style.paddingTop = style.paddingBottom = T.S1 - 2;
            style.backgroundColor = T.WithAlpha(T.BgDeep, 0.94f); T.Round(this, T.Radius - 2); T.Stroke(this, T.Border, T.BorderWidth);
            _text = T.Text("", MenuFont.Ui, T.FooterSize + 2, T.TextPrimary); Add(_text);
            _keyBox = InputPrompts.Keycap("", out _key); _keyBox.style.marginLeft = T.S1; Add(_keyBox);
            style.opacity = 0; T.Transition(this, T.Fast, "opacity", "translate");
        }

        public void ShowFor(VisualElement target, string text, string key, float delay = T.TooltipDelay)
        {
            Hide();
            if (string.IsNullOrEmpty(text) || target == null) return;
            _pending = schedule.Execute(() =>
            {
                _text.text = text; T.Apply(_text, MenuFont.Ui, T.FooterSize + 2, T.TextPrimary);
                _key.text = key ?? ""; _keyBox.style.display = string.IsNullOrEmpty(key) ? DisplayStyle.None : DisplayStyle.Flex;
                var parentEl = this.parent; if (parentEl == null || target.panel == null) return;
                var b = target.worldBound;
                var rightEdge = parentEl.WorldToLocal(new Vector2(b.xMax, b.yMax));
                if (rightEdge.x > parentEl.layout.width * 0.6f)
                {
                    // targets on the right (the chips): open below, right-aligned, so it never covers its neighbours
                    style.left = StyleKeyword.Auto; style.right = parentEl.layout.width - rightEdge.x; style.top = rightEdge.y + T.S1;
                }
                else
                {
                    var tl = parentEl.WorldToLocal(new Vector2(b.xMax + T.S2, b.center.y));
                    style.right = StyleKeyword.Auto; style.left = tl.x; style.top = tl.y - 18;
                }
                style.opacity = 1; style.translate = new Translate(0, 0);
            }).StartingIn((long)(delay * 1000));
        }

        public void Hide()
        {
            _pending?.Pause(); _pending = null;
            style.opacity = 0; style.translate = new Translate(-6, 0);
        }
    }

    /// <summary>Footer prompts that follow the last-used input device.</summary>
    public class InputPrompts : VisualElement
    {
        readonly Func<string, string> _t;

        public InputPrompts(Func<string, string> t) { _t = t; pickingMode = PickingMode.Ignore; style.flexDirection = FlexDirection.Row; style.alignItems = Align.Center; }

        public static VisualElement Keycap(string text, out Label label, bool round = false)
        {
            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.style.minWidth = 26; box.style.height = 26; box.style.paddingLeft = box.style.paddingRight = 6;
            box.style.alignItems = Align.Center; box.style.justifyContent = Justify.Center;
            box.style.backgroundColor = T.WithAlpha(T.TextPrimary, 0.08f);
            T.Round(box, round ? 13 : 5); T.Stroke(box, T.WithAlpha(T.TextSecondary, 0.6f), 1.5f);
            label = T.Text(text, MenuFont.UiBold, 14, T.TextPrimary, false);
            box.Add(label);
            return box;
        }

        void Pair(string[] keys, string action, bool round = false, string icon = null)
        {
            var grp = new VisualElement { pickingMode = PickingMode.Ignore }; grp.style.flexDirection = FlexDirection.Row; grp.style.alignItems = Align.Center; grp.style.marginLeft = T.S3;
            if (icon != null) { var im = new Image { image = MenuIcons.Get(icon), pickingMode = PickingMode.Ignore }; im.style.width = im.style.height = 20; im.tintColor = T.TextSecondary; grp.Add(im); }
            foreach (var k in keys) { var kc = Keycap(k, out _, round); kc.style.marginRight = 4; grp.Add(kc); }
            var l = T.Text(action, MenuFont.Ui, T.FooterSize, T.TextSecondary); l.style.marginLeft = 4; T.Shadow(l); grp.Add(l);
            Add(grp);
        }

        public void Show(MenuDevice d)
        {
            Clear();
            switch (d)
            {
                case MenuDevice.Keyboard:
                    Pair(new[] { "↑", "↓" }, _t("mm.p.navigate")); Pair(new[] { "Enter" }, _t("mm.p.select")); Pair(new[] { "Esc" }, _t("mm.p.back")); break;
                case MenuDevice.Gamepad:
                    Pair(new string[0], _t("mm.p.navigate"), false, "pad"); Pair(new[] { "A" }, _t("mm.p.select"), true); Pair(new[] { "B" }, _t("mm.p.back"), true); break;
                case MenuDevice.Touch:
                    Pair(new string[0], _t("mm.p.tap")); break;
                default:
                    Pair(new string[0], _t("mm.p.click"), false, "mouse"); Pair(new[] { "Esc" }, _t("mm.p.back")); break;
            }
        }
    }
}
