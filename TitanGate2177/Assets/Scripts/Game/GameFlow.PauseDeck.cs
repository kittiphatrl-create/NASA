using System;
using System.Linq;
using TitanGate.Game.Menu;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        enum PausePage { Menu, Settings, Help, Cards }

        bool _pauseDeckOpen, _pauseHasMenu;
        float _pauseTimeScale;
        PausePage _pausePage, _pauseReturn;
        int _pauseCardGroup = -1;
        string _pauseOpenCard, _pauseFocus;
        float _pauseCardScroll;
        ScrollView _pauseGrid;

        void OpenPauseDeck(PausePage page)
        {
            if (_pauseDeckOpen) return;
            _pauseDeckOpen = true;
            _pauseHasMenu = page == PausePage.Menu;
            _pauseTimeScale = Time.timeScale;
            if (_pauseHasMenu) Time.timeScale = 0f;
            _pausePage = _pauseReturn = page;
            _pauseFocus = _pauseOpenCard = null;
            _pauseCardGroup = -1;
            _pauseCardScroll = 0;
            _ui.ShowModal(BuildPauseDeck, false, ClosePauseDeck, customPanel: true);
        }

        void ClosePauseDeck()
        {
            _pauseDeckOpen = false;
            _pauseGrid = null;
            if (_pauseHasMenu) Time.timeScale = _pauseTimeScale;
        }

        void PauseDeckBack()
        {
            if (_pauseOpenCard != null)
            {
                _pauseFocus = "pause-card-" + _pauseOpenCard;
                _pauseOpenCard = null;
                _ui.RebuildModal();
            }
            else if (_pausePage != PausePage.Menu && _pauseHasMenu)
            {
                _pausePage = PausePage.Menu;
                _pauseFocus = "pause-" + _pauseReturn;
                _ui.RebuildModal();
            }
            else _ui.CloseModal();
        }

        void PauseDeckGo(PausePage page)
        {
            _pauseReturn = _pausePage = page;
            _pauseFocus = null;
            _ui.RebuildModal();
        }

        static VisualElement PauseRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            return row;
        }

        static void PausePadding(VisualElement element, int x, int y)
        {
            element.style.paddingLeft = element.style.paddingRight = x;
            element.style.paddingTop = element.style.paddingBottom = y;
        }

        Label PauseLabel(string text, int size, bool bold = true) => _ui.L(text, size, bold, FieldBook.Ink);

        void BuildPauseDeck(VisualElement shell)
        {
            shell.name = "pause-deck-" + _pausePage;
            bool menu = _pausePage == PausePage.Menu;
            shell.style.width = menu ? 1090 : _pausePage == PausePage.Settings ? 1115 : 1460;
            shell.style.maxWidth = Length.Percent(94);
            shell.style.maxHeight = Length.Percent(94);
            PausePadding(shell, 47, 30);
            shell.Add(GameUI.Plate(FieldBook.Teal, FieldBook.Ink, 37, false, 9));
            var rim = GameUI.Plate(FieldBook.Teal, FieldBook.Rim, 29, false, 4);
            rim.style.left = rim.style.right = rim.style.top = rim.style.bottom = 10;
            shell.Add(rim);
            foreach (bool top in new[] { true, false })
            foreach (bool left in new[] { true, false })
            {
                var screw = new VisualElement { pickingMode = PickingMode.Ignore };
                screw.style.position = Position.Absolute;
                if (top) screw.style.top = 26; else screw.style.bottom = 26;
                if (left) screw.style.left = 34; else screw.style.right = 34;
                screw.style.width = screw.style.height = 23;
                screw.style.backgroundColor = FieldBook.Rim;
                GameUI.Border(screw, FieldBook.Ink, 4, 12);
                var slit = new VisualElement { pickingMode = PickingMode.Ignore };
                slit.style.height = 2; slit.style.marginTop = 6;
                slit.style.marginLeft = slit.style.marginRight = 3;
                slit.style.backgroundColor = FieldBook.Ink;
                screw.Add(slit);
                shell.Add(screw);
            }

            if (!menu)
            {
                string header = _pausePage == PausePage.Cards ? TT("Knowledge cards", "การ์ดความรู้")
                    : _loc.T(_pausePage == PausePage.Settings ? "menu.settings" : "menu.howto");
                var heading = PauseRow();
                heading.style.marginBottom = 18;
                heading.style.minHeight = 76;
                if (_pausePage == PausePage.Cards) heading.Add(PauseIcon("cards", 48));
                PausePadding(heading, 21, 9);
                heading.style.backgroundColor = FieldBook.Card;
                GameUI.Border(heading, FieldBook.Ink, 4, 14);
                var title = PauseLabel(header, 42);
                title.style.flexGrow = 1;
                title.style.unityTextAlign = TextAnchor.MiddleCenter;
                heading.Add(title);
                if (_pausePage == PausePage.Cards)
                {
                    var have = CardsEver();
                    heading.Add(PauseLabel($"{_cfg.cards.Count(c => have.Contains(c.id))}/{_cfg.cards.Count}", 39));
                }
                if (_pausePage == PausePage.Settings)
                    heading.Add(PauseButton("←", PauseDeckBack, "pause-top-back", size: 32));
                shell.Add(heading);
            }

            var spread = new VisualElement();
            spread.style.flexShrink = 1;
            spread.style.minHeight = 0;
            spread.style.flexDirection = FlexDirection.Row;
            if (_pausePage == PausePage.Settings) BuildPauseSettings(PausePaper(spread), shell);
            else if (_pausePage == PausePage.Menu) BuildPauseMenu(spread);
            else if (_pausePage == PausePage.Help) BuildPauseHelp(spread);
            else BuildPauseCards(spread);
            shell.Add(spread);

            var footer = PauseRow();
            footer.style.flexShrink = 0;
            footer.style.marginTop = 12;
            foreach (bool left in new[] { true, false })
            {
                if (!left)
                {
                    var back = PauseButton(menu ? _loc.T("pause.resume") : TT("Back", "กลับ"),
                        PauseDeckBack, "pause-back", size: 29);
                    back.style.marginLeft = back.style.marginRight = 16;
                    back.Insert(0, PauseKey("Esc"));
                    footer.Add(back);
                }
                var rail = new VisualElement { pickingMode = PickingMode.Ignore };
                rail.style.height = 12; rail.style.flexGrow = 1;
                rail.style.backgroundColor = FieldBook.Ink;
                GameUI.Border(rail, FieldBook.Rim, 2, 8);
                footer.Add(rail);
            }
            shell.Add(footer);
            shell.schedule.Execute(() =>
            {
                var first = shell.Q<VisualElement>(_pauseFocus ?? (menu ? "pause-resume" :
                    _pausePage == PausePage.Settings ? "pause-master" :
                    _pausePage == PausePage.Cards ? "pause-group-" + _pauseCardGroup : "pause-back"));
                (first ?? shell.Q<Button>("pause-back"))?.Focus();
            });
        }

        static VisualElement PausePaper(VisualElement spread, float basis = 100)
        {
            var page = new VisualElement();
            page.style.flexBasis = Length.Percent(basis);
            page.style.flexGrow = 1;
            page.style.flexShrink = 1;
            page.style.minWidth = page.style.minHeight = 0;
            PausePadding(page, 25, 25);
            page.Add(GameUI.Plate(FieldBook.Paper, FieldBook.Ink, 26, false, 7));
            var inner = GameUI.Plate(FieldBook.Card, FieldBook.Tan, 19, false, 2);
            inner.style.left = inner.style.top = inner.style.right = inner.style.bottom = 11;
            page.Add(inner);
            spread.Add(page);
            return page;
        }

        static void PauseHinges(VisualElement spread, float position)
        {
            var spine = new VisualElement { pickingMode = PickingMode.Ignore };
            spine.style.position = Position.Absolute;
            spine.style.left = Length.Percent(position);
            spine.style.marginLeft = -22;
            spine.style.top = Length.Percent(16);
            spine.style.bottom = Length.Percent(16);
            spine.style.width = 44;
            spine.style.justifyContent = Justify.SpaceBetween;
            for (int i = 0; i < 3; i++)
            {
                var hinge = new VisualElement { pickingMode = PickingMode.Ignore };
                hinge.style.height = 33;
                hinge.Add(GameUI.Plate(FieldBook.Paper, FieldBook.Ink, 8, false, 4));
                spine.Add(hinge);
            }
            spread.Add(spine);
        }

        Label PauseKey(string key)
        {
            var label = PauseLabel(key, 22);
            label.pickingMode = PickingMode.Ignore;
            PausePadding(label, 8, 4);
            label.style.marginRight = 9;
            label.style.backgroundColor = FieldBook.Amber;
            GameUI.Border(label, FieldBook.Ink, 2, 6);
            return label;
        }

        VisualElement PauseIcon(string id, int size)
        {
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.style.width = icon.style.height = size;
            icon.style.flexShrink = 0;
            if (FieldBookIcons.Has(id))
            {
                icon.generateVisualContent += ctx => FieldBookIcons.Draw(ctx.painter2D, id, size);
                return icon;
            }
            if (id == "type")
            {
                // Amber "Aa" with an ink outline, matching the other cartoon icons.
                Label Glyph(Color color, float dx, float dy)
                {
                    var text = _ui.L("Aa", size * 3 / 5, true, color);
                    text.pickingMode = PickingMode.Ignore;
                    text.style.position = Position.Absolute;
                    text.style.left = dx; text.style.top = dy;
                    text.style.right = -dx; text.style.bottom = -dy;
                    text.style.fontSize = size * .58f;
                    text.style.whiteSpace = WhiteSpace.NoWrap;
                    text.style.unityTextAlign = TextAnchor.MiddleCenter;
                    return text;
                }
                float o = Mathf.Max(1.5f, size / 28f);
                foreach (var (dx, dy) in new[] { (-o, 0f), (o, 0f), (0f, -o), (0f, o), (o, o), (o * 1.8f, o * 1.8f) })
                    icon.Add(Glyph(FieldBook.Ink, dx, dy));
                icon.Add(Glyph(FieldBook.Amber, 0, 0));
                return icon;
            }
            if (id is "pause" or "wave" or "motion" or "full" or "cards" or "ship" or "box" or "hand" or "day" or "clock" or "restart" or "mars"
                or "pad" or "planet" or "titan" or "door" or "sparkle")
            {
                icon.generateVisualContent += ctx => DrawPauseIcon(ctx.painter2D, id, size);
                return icon;
            }
            var texture = MenuIcons.Get(id);
            var shadow = new Image { image = texture, tintColor = FieldBook.Ink, pickingMode = PickingMode.Ignore };
            shadow.style.position = Position.Absolute;
            shadow.style.width = shadow.style.height = size;
            shadow.style.left = shadow.style.top = 2;
            icon.Add(shadow);
            var glyph = new Image { image = texture, tintColor = FieldBook.Ink, pickingMode = PickingMode.Ignore };
            glyph.style.width = glyph.style.height = size;
            icon.Add(glyph);
            return icon;
        }

        Button PauseButton(string text, Action action, string name = null, string icon = null, int size = 28,
            bool selected = false, bool scroller = false)
        {
            var button = new Button(() => { _sfx.Play("click"); action(); }) { name = name };
            button.style.flexDirection = FlexDirection.Row;
            button.style.alignItems = Align.Center;
            button.style.justifyContent = icon == null ? Justify.Center : Justify.FlexStart;
            button.style.minHeight = scroller ? 70 : 51;
            button.style.flexShrink = 0;
            button.style.marginTop = button.style.marginBottom = 3;
            button.style.marginLeft = button.style.marginRight = 0;
            PausePadding(button, scroller ? 18 : 13, 5);
            var marker = new VisualElement { pickingMode = PickingMode.Ignore };
            if (scroller)
            {
                marker.style.width = 21;
                marker.style.height = 24;
                marker.style.flexShrink = 0;
                marker.generateVisualContent += ctx =>
                {
                    var p = ctx.painter2D;
                    p.fillColor = FieldBook.Ink;
                    p.BeginPath(); p.MoveTo(new Vector2(3, 2)); p.LineTo(new Vector2(17, 12)); p.LineTo(new Vector2(3, 22));
                    p.ClosePath(); p.Fill();
                };
                button.Add(marker);
            }
            if (icon != null)
            {
                var graphic = PauseIcon(icon, scroller ? 48 : 37);
                graphic.style.marginRight = 12;
                button.Add(graphic);
            }
            var copy = PauseLabel(text, size);
            copy.style.flexShrink = 1;
            var copyClip = new VisualElement { pickingMode = PickingMode.Ignore };
            copyClip.style.overflow = Overflow.Hidden;
            copyClip.style.flexShrink = 1;
            var trail = PauseLabel(text, size);
            trail.style.position = Position.Absolute;
            trail.style.left = trail.style.top = 0;
            trail.style.opacity = 0;
            if (scroller) copyClip.Add(trail);
            copyClip.Add(copy);
            button.Add(copyClip);
            bool hovered = false, focused = false, wasActive = false;
            float start = 0;
            IVisualElementScheduledItem motion = null;
            void Paint()
            {
                // The panel can appear below a stationary pointer when going back.
                // Only pointer movement should replace a restored keyboard focus.
                bool active = focused || (!scroller && hovered);
                button.style.backgroundColor = selected || (active && scroller) ? FieldBook.Amber
                    : active ? FieldBook.Paper : FieldBook.Card;
                button.style.backgroundImage = StyleKeyword.None;
                GameUI.Border(button, focused ? FieldBook.Teal : FieldBook.Ink, focused ? 4 : 3, 12);
                marker.style.opacity = active ? 1 : 0;
                if (scroller && active && !wasActive && !Settings.ReducedMotion)
                {
                    start = Time.unscaledTime;
                    motion?.Pause();
                    motion = button.schedule.Execute(() =>
                    {
                        float t = Settings.ReducedMotion ? 1 : Mathf.Clamp01((Time.unscaledTime - start) / .15f);
                        float travel = (1 - t) * (1 - t);
                        copy.style.translate = new Translate(0, 14 * travel);
                        trail.style.translate = new Translate(0, 20 * travel);
                        trail.style.opacity = .32f * travel;
                        if (t >= 1) motion.Pause();
                    }).Every(12);
                }
                wasActive = active;
            }
            button.RegisterCallback<PointerEnterEvent>(_ => { hovered = true; Paint(); });
            button.RegisterCallback<PointerLeaveEvent>(_ => { hovered = false; Paint(); });
            if (scroller) button.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (evt.deltaPosition.sqrMagnitude > 0) button.Focus();
            });
            button.RegisterCallback<FocusInEvent>(_ => { focused = true; Paint(); });
            button.RegisterCallback<FocusOutEvent>(_ => { focused = false; Paint(); });
            button.RegisterCallback<DetachFromPanelEvent>(_ => motion?.Pause());
            Paint();
            return button;
        }

        void BuildPauseMenu(VisualElement spread)
        {
            var titlePage = PausePaper(spread, 44);
            var actions = PausePaper(spread, 56);
            titlePage.style.justifyContent = Justify.Center;
            titlePage.style.alignItems = Align.Center;
            titlePage.Add(PauseIcon("pause", 142));
            var title = PauseLabel(TT("Paused", "พักเกม"), 60);
            title.style.marginTop = 18;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            PausePadding(title, 23, 10);
            GameUI.Border(title, FieldBook.Ink, 3, 13);
            titlePage.Add(title);
            var scroll = PauseScroll(actions);
            scroll.Add(PauseButton(_loc.T("pause.resume"), PauseDeckBack, "pause-resume", "play", 34, scroller: true));
            scroll.Add(PauseButton(_loc.T("menu.settings"), () => PauseDeckGo(PausePage.Settings), "pause-Settings", "gear", 34, scroller: true));
            scroll.Add(PauseButton(_loc.T("menu.howto"), () => PauseDeckGo(PausePage.Help), "pause-Help", "pad", 34, scroller: true));
            scroll.Add(PauseButton(TT("Knowledge cards", "การ์ดความรู้"), () => PauseDeckGo(PausePage.Cards), "pause-Cards", "cards", 34, scroller: true));
            var separator = new VisualElement();
            separator.style.height = 3; separator.style.flexShrink = 0;
            separator.style.marginTop = separator.style.marginBottom = 11;
            separator.style.backgroundColor = FieldBook.Ink;
            scroll.Add(separator);
            scroll.Add(PauseButton(TT("Restart", "เริ่มใหม่"), () => { _ui.CloseModal(); Restart(); }, "pause-restart", "restart", 30, scroller: true));
            scroll.Add(PauseButton(TT("Main menu", "เมนูหลัก"), () => { _ui.CloseModal(); RestartToTitle(); }, "pause-title", "door", 30, scroller: true));
            scroll.style.height = Mathf.Min(730, 484 + 75 * (Settings.TextScale - 1));
            PauseHinges(spread, 44);
        }

        static ScrollView PauseScroll(VisualElement parent)
        {
            var view = new ScrollView(ScrollViewMode.Vertical);
            view.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            view.verticalScrollerVisibility = ScrollerVisibility.Auto;
            view.style.flexShrink = 1;
            view.contentContainer.style.paddingRight = 4;
            var scrollbar = view.verticalScroller;
            scrollbar.style.width = 14;
            scrollbar.style.marginLeft = 5;
            scrollbar.style.backgroundColor = FieldBook.Paper;
            scrollbar.lowButton.style.display = scrollbar.highButton.style.display = DisplayStyle.None;
            var thumb = scrollbar.slider.Q<VisualElement>("unity-dragger");
            if (thumb != null)
            {
                thumb.style.backgroundColor = FieldBook.Teal;
                GameUI.Border(thumb, FieldBook.Ink, 2, 6);
            }
            parent.Add(view);
            return view;
        }
    }
}
