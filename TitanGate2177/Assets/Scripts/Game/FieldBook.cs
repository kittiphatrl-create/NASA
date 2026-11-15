using System;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>The two-page field book. The caller supplies live game data for each spread.</summary>
    sealed class FieldBook
    {
        public const int Status = 0, Mission = 1, Items = 2;
        const float Width = 1496, Height = 972;
        const float OpenSeconds = .76f, CloseSeconds = .72f;
        public static readonly Color Ink = new Color32(16, 26, 24, 255);
        public static readonly Color Teal = new Color32(57, 109, 100, 255);
        public static readonly Color Rim = new Color32(121, 171, 149, 255);
        public static readonly Color Paper = new Color32(255, 235, 202, 255);
        public static readonly Color Card = new Color32(255, 242, 218, 255);
        public static readonly Color Tan = new Color32(192, 154, 113, 255);
        public static readonly Color Amber = new Color32(255, 181, 60, 255);
        public static readonly Color Green = new Color32(112, 178, 130, 255);
        public static readonly Color Red = new Color32(229, 107, 64, 255);
        public static readonly Color Muted = new Color32(92, 76, 56, 255);

        readonly GameUI _ui;
        readonly VisualElement _overlay, _book, _faceClip, _face, _spread, _leaf, _covers;
        VisualElement _left, _right, _nextLeft, _nextRight;
        readonly Button[] _tabs = new Button[3];
        readonly VisualElement[] _glyphs = new VisualElement[3];
        readonly Action<int, VisualElement, VisualElement> _populate;
        readonly Action _pageSound, _close;
        readonly Action<int, int> _turned;
        readonly Focusable _priorFocus;
        float _fold, _turn = -1, _turnSeconds, _iconAge = 1;
        bool _closing, _swapped;
        int _current, _requested, _from, _next;
        int _highlighted;
        bool _skipping;
        public int Current => _current;
        public int Requested => _requested;
        public bool Settled => _fold == 1 && _turn < 0 && _current == _requested && !_closing;

        public FieldBook(GameUI ui, string title, string[] tabs, string closeText,
            Action<VisualElement> header, Action<int, VisualElement, VisualElement> populate,
            Action pageSound, Action close, Action<int, int> turned = null)
        {
            _ui = ui;
            _populate = populate;
            _pageSound = pageSound;
            _close = close;
            _turned = turned;
            _priorFocus = ui.Panel?.focusController?.focusedElement;
            _overlay = new VisualElement { name = "field-book", focusable = true, tabIndex = -1 };
            Place(_overlay, 0, 0);
            _overlay.style.right = _overlay.style.bottom = 0;
            _overlay.style.alignItems = Align.Center;
            _overlay.style.justifyContent = Justify.Center;
            _overlay.RegisterCallback<KeyDownEvent>(TrapTab, TrickleDown.TrickleDown);
            _book = new VisualElement { pickingMode = PickingMode.Ignore };
            _book.style.width = Width;
            _book.style.height = Height;
            _book.style.flexShrink = 0;
            _overlay.Add(_book);
            _faceClip = new VisualElement { pickingMode = PickingMode.Ignore };
            Place(_faceClip, 0, 0, Width, Height);
            _faceClip.style.overflow = Overflow.Hidden;
            _book.Add(_faceClip);
            _face = new VisualElement { pickingMode = PickingMode.Ignore };
            Place(_face, 0, 0, Width, Height);
            _faceClip.Add(_face);
            _face.Add(Drawing(0, 0, Width, Height, DrawCase));

            var titleWell = Well();
            Place(titleWell, 132, 43, 551, 78);
            var heading = Text(title, 45, true);
            heading.style.unityTextAlign = TextAnchor.MiddleCenter;
            titleWell.Add(heading);
            _face.Add(titleWell);
            var headerWell = Well();
            Place(headerWell, 724, 31, 612, 126);
            header(headerWell);
            _face.Add(headerWell);

            _spread = new VisualElement { pickingMode = PickingMode.Ignore };
            Place(_spread, 104, 204, 1288, 646);
            _face.Add(_spread);
            _spread.Add(Drawing(-17, 22, 1322, 611, DrawPageStack));
            _left = Page(0);
            _right = Page(644);
            _spread.Add(_left);
            _spread.Add(_right);
            _leaf = Drawing(0, 0, 1288, 646, DrawLeaf);
            _spread.Add(_leaf);
            _spread.Add(Drawing(610, 0, 68, 646, DrawHinges));

            for (int i = 0; i < _tabs.Length; i++)
            {
                int tab = i;
                var button = Button(tabs[i], () => Select(tab));
                button.name = "field-book-tab-" + i;
                button.style.flexDirection = FlexDirection.Row;
                Place(button, 122 + 218 * i, 136, 216, 73);
                var glyph = Icon(i == Status ? "gauge" : i == Mission ? "radio" : "bag", 53);
                glyph.style.marginRight = 9;
                button.Add(glyph);
                button.Add(Text(tabs[i], 33, true));
                _face.Add(button);
                _tabs[i] = button;
                _glyphs[i] = glyph;
            }

            var closeButton = Button(closeText, _close);
            Place(closeButton, 594, 865, 308, 74);
            closeButton.style.flexDirection = FlexDirection.Row;
            closeButton.Add(ui.Hotkey("M"));
            closeButton.Add(Text(closeText, 33, true));
            _face.Add(closeButton);

            _covers = Drawing(0, 0, Width, Height, DrawCovers);
            _book.Add(_covers);
            _populate(Status, _left, _right);
            Highlight(Status);
            ui.Root.Add(_overlay);
            _overlay.Focus();
            if (Settings.ReducedMotion) _fold = 1;
            Paint();
        }

        public void Close()
        {
            if (_overlay.panel == null) return;
            _closing = true;
            _overlay.Focus();
            if (Settings.ReducedMotion) _fold = 0;
        }

        public void Dispose()
        {
            _overlay.RemoveFromHierarchy();
            if (_priorFocus is VisualElement old && old.panel == _ui.Panel && old.enabledInHierarchy)
                old.Focus();
        }

        public void Select(int tab)
        {
            if (_closing || tab < 0 || tab >= _tabs.Length || _requested == tab) return;
            _requested = tab;
            if (Settings.ReducedMotion) Show(tab);
        }

        // A new target replaces the pending destination. Finish the leaf already in flight,
        // then travel from that spread to the most recent target.
        public bool Tick(float dt)
        {
            if (_closing)
            {
                _fold = Mathf.MoveTowards(_fold, 0, dt / CloseSeconds);
                if (_fold == 0) return false;
            }
            else if (_fold < 1)
                _fold = Mathf.MoveTowards(_fold, 1, dt / OpenSeconds);
            else if (_turn >= 0)
            {
                _turn = Mathf.Min(1, _turn + dt / _turnSeconds);
                if (_turn >= .5f && !_swapped)
                {
                    _swapped = true;
                    if (_next > _from) ReplaceLeft(_nextLeft);
                    else ReplaceRight(_nextRight);
                    _nextLeft = _nextRight = null;
                    _current = _next;
                }
                if (_turn >= 1)
                {
                    _turn = -1;
                    _turned?.Invoke(_from, _next);
                    if (_current == _requested)
                    {
                        _skipping = false;
                        Highlight(_current);
                    }
                }
            }
            else if (_current != _requested)
            {
                _from = _current;
                _next = _current + Math.Sign(_requested - _current);
                _skipping |= Mathf.Abs(_requested - _current) > 1;
                _turnSeconds = _skipping ? .48f : .60f;
                _swapped = false;
                _turn = 0;
                _nextLeft = Page(0);
                _nextRight = Page(644);
                _populate(_next, _nextLeft, _nextRight);
                if (_next > _from) ReplaceRight(_nextRight);
                else ReplaceLeft(_nextLeft);
                _pageSound();
            }

            _iconAge += dt;
            Paint();
            return true;
        }

        void Show(int tab)
        {
            var left = Page(0);
            var right = Page(644);
            _populate(tab, left, right);
            ReplaceLeft(left);
            ReplaceRight(right);
            Highlight(tab);
        }

        void Highlight(int tab)
        {
            _current = tab;
            _highlighted = tab;
            for (int i = 0; i < _tabs.Length; i++)
            {
                _tabs[i].style.backgroundColor = i == tab ? Amber : Paper;
                _glyphs[i].style.translate = new Translate(0, 0);
                _glyphs[i].style.rotate = new Rotate(0);
            }
            _iconAge = 0;
        }

        void ReplaceLeft(VisualElement page)
        {
            _left.RemoveFromHierarchy();
            _left = page;
            _spread.Insert(1, page);
        }

        void ReplaceRight(VisualElement page)
        {
            _right.RemoveFromHierarchy();
            _right = page;
            _spread.Insert(2, page);
        }

        void Paint()
        {
            float w = _ui.Root.layout.width, h = _ui.Root.layout.height;
            float size = float.IsNaN(w) || float.IsNaN(h) ? 1 :
                Mathf.Clamp(Mathf.Min((w - 36) / Width, (h - 34) / Height), .1f, 1);
            float enter = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, .27f, _fold));
            float fold = CoverAngle();
            float edge = Width / 2 * (1 + Mathf.Min(0, Mathf.Cos(fold)));
            float scale = size * Mathf.Lerp(.65f, 1, enter) * Mathf.Lerp(.92f, 1, fold / Mathf.PI);
            _overlay.style.backgroundColor = new Color(.015f, .025f, .035f, .67f * Mathf.SmoothStep(0, 1, _fold));
            _book.style.scale = new Scale(new Vector2(scale, scale));
            _book.style.opacity = enter;
            _book.style.translate = new Translate(-edge * .5f * scale,
                (Mathf.Lerp(78, 0, enter) + Mathf.Sin(fold) * 16) * scale);
            _book.style.rotate = new Rotate(new Angle(Mathf.Lerp(-5, 0, fold / Mathf.PI)));
            _faceClip.style.left = edge;
            _faceClip.style.width = Width - edge;
            _face.style.left = -edge;
            _covers.style.display = _fold < 1 ? DisplayStyle.Flex : DisplayStyle.None;
            _covers.MarkDirtyRepaint();
            _leaf.style.display = _turn >= 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (_turn >= 0) _leaf.MarkDirtyRepaint();
            if (!Settings.ReducedMotion && _iconAge < .65f)
            {
                float swing = Mathf.Sin(_iconAge * 24) * (1 - _iconAge / .65f);
                _glyphs[_highlighted].style.rotate = new Rotate(new Angle(swing * 13));
                _glyphs[_highlighted].style.translate = new Translate(0, -Mathf.Abs(swing) * 7);
            }
            else if (!Settings.ReducedMotion)
            {
                _glyphs[_highlighted].style.rotate = new Rotate(0);
                _glyphs[_highlighted].style.translate = new Translate(0, 0);
            }
        }

        void TrapTab(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Tab) return;
            var buttons = _overlay.Query<Button>().ToList();
            buttons.RemoveAll(b => !b.enabledInHierarchy || b.resolvedStyle.display == DisplayStyle.None);
            if (buttons.Count == 0) return;
            int index = buttons.IndexOf(_ui.Panel?.focusController?.focusedElement as Button);
            int step = evt.shiftKey ? -1 : 1;
            buttons[index < 0 ? (step > 0 ? 0 : buttons.Count - 1) : (index + step + buttons.Count) % buttons.Count].Focus();
            evt.StopImmediatePropagation();
        }

        public Label Text(string text, int size, bool bold = false, Color? color = null)
            => _ui.L(text, size, bold, color ?? Ink);

        public Button Button(string tooltip, Action action)
        {
            var b = new Button(action) { tooltip = tooltip };
            b.style.marginLeft = b.style.marginRight = b.style.marginTop = b.style.marginBottom = 0;
            b.style.paddingLeft = b.style.paddingRight = 8;
            b.style.alignItems = Align.Center;
            b.style.justifyContent = Justify.Center;
            b.style.backgroundColor = Paper;
            GameUI.Border(b, Ink, 4, 13);
            bool hover = false, focused = false;
            void Edge() => GameUI.Border(b, focused || hover ? Teal : Ink, 4, 13);
            b.RegisterCallback<PointerEnterEvent>(_ => { hover = true; Edge(); });
            b.RegisterCallback<PointerLeaveEvent>(_ => { hover = false; Edge(); });
            b.RegisterCallback<FocusInEvent>(_ => { focused = true; Edge(); });
            b.RegisterCallback<FocusOutEvent>(_ => { focused = false; Edge(); });
            return b;
        }

        public static VisualElement Well(Color? fill = null, float radius = 15)
        {
            var card = new VisualElement { pickingMode = PickingMode.Ignore };
            card.style.backgroundColor = fill ?? Card;
            GameUI.Border(card, Ink, 3, radius);
            card.style.justifyContent = Justify.Center;
            return card;
        }

        public static void Place(VisualElement e, float x, float y, float w = -1, float h = -1)
        {
            e.style.position = Position.Absolute;
            e.style.left = x;
            e.style.top = y;
            if (w >= 0) e.style.width = w;
            if (h >= 0) e.style.height = h;
        }

        static VisualElement Drawing(float x, float y, float w, float h, Action<Painter2D> draw)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            Place(element, x, y, w, h);
            element.generateVisualContent += context => draw(context.painter2D);
            return element;
        }

        static VisualElement Page(float x)
        {
            var page = new VisualElement { pickingMode = PickingMode.Ignore };
            Place(page, x, 0, 644, 646);
            page.style.paddingLeft = page.style.paddingRight = 37;
            page.style.paddingTop = page.style.paddingBottom = 34;
            page.Add(Drawing(0, 0, 644, 646, p =>
            {
                CutRect(p, 1, 1, 642, 642, 25, Paper, Ink, 6);
                CutRect(p, 17, 14, 610, 614, 17, Color.clear, Tan, 2);
            }));
            return page;
        }

        static void DrawCase(Painter2D p)
        {
            CutRect(p, 23, 40, 1456, 915, 47, Ink, Ink, 8);
            CutRect(p, 12, 19, 1468, 920, 42, Teal, Ink, 9);
            CutRect(p, 24, 30, 1444, 897, 37, Color.clear, Rim, 5);
            CutRect(p, 80, 176, 1336, 691, 26, new Color32(33, 65, 60, 255), Ink, 5);
            foreach (float x in new[] { 68f, Width - 68 })
            foreach (float y in new[] { 66f, 891f })
            {
                Disc(p, x, y, 16, Ink);
                Disc(p, x, y, 10, new Color32(162, 183, 176, 255));
                CutRect(p, x - 7, y - 1, 14, 2, 0, Teal, Color.clear, 0);
            }
            CutRect(p, 45, 136, 36, 86, 6, Amber, Ink, 5);
            CutRect(p, 20, 161, 86, 36, 6, Amber, Ink, 5);
            CutRect(p, 48, 157, 30, 42, 0, Amber, Color.clear, 0);
            foreach (float y in new[] { 152f, 219f })
            {
                Disc(p, 1430, y, 24, Ink);
                Disc(p, 1428, y - 3, 17, Amber);
                Disc(p, 1422, y - 10, 4, Paper);
            }
            CutRect(p, 168, 890, 370, 11, 4, Ink, Rim, 2);
            CutRect(p, 958, 890, 370, 11, 4, Ink, Rim, 2);
        }

        static void DrawPageStack(Painter2D p)
        {
            CutRect(p, 0, 0, 1322, 611, 26, Tan, Ink, 6);
            CutRect(p, 7, 9, 1308, 593, 24, Paper, Tan, 3);
        }

        static void DrawHinges(Painter2D p)
        {
            foreach (float y in new[] { 83f, 305f, 529f })
            {
                CutRect(p, 4, y + 4, 60, 43, 10, Tan, Ink, 5);
                CutRect(p, 5, y, 58, 34, 9, Paper, Ink, 4);
                CutRect(p, 14, y + 6, 39, 5, 2, Card, Color.clear, 0);
            }
        }

        float CoverAngle() => Mathf.PI * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.18f, 1, _fold));

        void DrawCovers(Painter2D p)
        {
            float angle = CoverAngle();
            if (angle >= Mathf.PI * .5f) return; // The inside of the lid is the live left page.
            float spine = Width / 2;
            float reach = Mathf.Max(5, Mathf.Cos(angle) * 730);
            float edge = spine + reach;
            float lift = Mathf.Sin(angle) * 86;
            p.fillColor = Teal;
            p.strokeColor = Ink;
            p.lineWidth = 8;
            p.BeginPath();
            p.MoveTo(new Vector2(spine, 27));
            p.LineTo(new Vector2(edge - reach * .035f, 26 - lift));
            p.LineTo(new Vector2(edge, 52 - lift));
            p.LineTo(new Vector2(edge, 916 - lift));
            p.LineTo(new Vector2(edge - reach * .035f, 939 - lift));
            p.LineTo(new Vector2(spine, 939));
            p.ClosePath();
            p.Fill();
            p.Stroke();
            if (reach > 100)
            {
                float x = (spine + edge) * .5f;
                CutRect(p, x - reach * .32f, 372 - lift * .5f, reach * .64f, 178, 14, Ink, Rim, 5);
                Disc(p, x, 461 - lift * .5f, Mathf.Min(34, reach * .13f), Amber);
                CutRect(p, spine + reach * .2f, 718 - lift * .5f, reach * .6f, 16, 5, Ink, Rim, 3);
            }
        }

        void DrawLeaf(Painter2D p)
        {
            float direction = _next > _from ? 1 : -1;
            float angle = Mathf.SmoothStep(0, 1, _turn) * Mathf.PI;
            float reach = Mathf.Cos(angle);
            float curl = Mathf.Sin(angle);
            float spine = 644;
            float edge = spine + direction * (reach * 635 - curl * 14);
            p.fillColor = Color.Lerp(Paper, Tan, curl * .57f);
            p.strokeColor = Ink;
            p.lineWidth = 4;
            p.BeginPath();
            p.MoveTo(new Vector2(spine, 6));
            p.BezierCurveTo(new Vector2(spine + direction * curl * 110, -curl * 32),
                new Vector2(edge - direction * reach * 95, curl * 9), new Vector2(edge, 15 + curl * 23));
            p.LineTo(new Vector2(edge, 626 - curl * 47));
            p.BezierCurveTo(new Vector2(edge - direction * reach * 95, 633),
                new Vector2(spine + direction * curl * 110, 646 - curl * 23), new Vector2(spine, 639));
            p.ClosePath();
            p.Fill();
            p.Stroke();
            p.strokeColor = Color.Lerp(Tan, Paper, .45f);
            p.lineWidth = 3;
            p.BeginPath();
            p.MoveTo(new Vector2(spine + direction * 8, 16));
            p.BezierCurveTo(new Vector2(spine + direction * curl * 95, 8),
                new Vector2(edge - direction * reach * 70, curl * 13 + 16), new Vector2(edge - direction * reach * 13, 24 + curl * 23));
            p.Stroke();
        }

        public static VisualElement Icon(string id, int size)
        {
            if (FieldBookIcons.Has(id))
            {
                var shape = new VisualElement { pickingMode = PickingMode.Ignore };
                shape.style.width = shape.style.height = size;
                shape.style.flexShrink = 0;
                shape.generateVisualContent += context => FieldBookIcons.Draw(context.painter2D, id, size);
                return shape;
            }
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.style.width = icon.style.height = size;
            icon.style.flexShrink = 0;
            var shadow = new Image { sprite = GameFlow.LdIcon(id), tintColor = Ink, pickingMode = PickingMode.Ignore };
            var glyph = new Image { sprite = shadow.sprite, tintColor = Amber, pickingMode = PickingMode.Ignore };
            Place(shadow, 2, 3, size, size);
            Place(glyph, 0, 0, size, size);
            icon.Add(shadow);
            icon.Add(glyph);
            return icon;
        }

        static void Disc(Painter2D p, float x, float y, float r, Color c)
        {
            p.fillColor = c;
            p.BeginPath();
            p.Arc(new Vector2(x, y), r, Angle.Degrees(0), Angle.Degrees(360));
            p.ClosePath();
            p.Fill();
        }

        static void CutRect(Painter2D p, float x, float y, float w, float h, float cut, Color fill, Color edge, float line)
        {
            p.fillColor = fill;
            p.strokeColor = edge;
            p.lineWidth = line;
            p.BeginPath();
            p.MoveTo(new Vector2(x + cut, y));
            p.LineTo(new Vector2(x + w - cut, y));
            p.LineTo(new Vector2(x + w, y + cut));
            p.LineTo(new Vector2(x + w, y + h - cut));
            p.LineTo(new Vector2(x + w - cut, y + h));
            p.LineTo(new Vector2(x + cut, y + h));
            p.LineTo(new Vector2(x, y + h - cut));
            p.LineTo(new Vector2(x, y + cut));
            p.ClosePath();
            if (fill.a > 0) p.Fill();
            if (line > 0) p.Stroke();
        }
    }
}
