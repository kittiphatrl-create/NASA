using System;
using System.Linq;
using TitanGate.Core.LastDock;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        static string PausePercent(float value) => $"{Mathf.RoundToInt(value * 100)}%";

        void BuildPauseSettings(VisualElement page, VisualElement shell)
        {
            var rows = PauseScroll(page);
            rows.style.height = 710;
            var tip = PauseLabel("", 20);
            tip.pickingMode = PickingMode.Ignore;
            tip.style.position = Position.Absolute;
            tip.style.display = DisplayStyle.None;
            tip.style.maxWidth = 220;
            tip.style.backgroundColor = FieldBook.Paper;
            PausePadding(tip, 8, 3);
            GameUI.Border(tip, FieldBook.Ink, 2, 6);
            shell.Add(tip);
            VisualElement hoveredRow = null, focusedRow = null;
            void Hint()
            {
                var current = hoveredRow ?? focusedRow;
                tip.style.display = current == null ? DisplayStyle.None : DisplayStyle.Flex;
                if (current == null) return;
                // Sit just right of the row's icon, straddling the row's top edge (as in the approved
                // settings mock), so the slider and its value stay visible beside the label.
                var pos = shell.WorldToLocal(current.worldBound.position);
                tip.text = GameUI.Thai((string)current.userData);
                tip.style.left = pos.x + 78;
                tip.style.top = Mathf.Max(0, pos.y - 22);
                tip.BringToFront();
            }
            rows.verticalScroller.valueChanged += _ => Hint();

            VisualElement Row(string icon, string label)
            {
                var row = PauseRow();
                row.userData = label;
                row.style.minHeight = 82;
                row.style.marginBottom = 6;
                row.style.flexShrink = 0;
                PausePadding(row, 19, 6);
                GameUI.Border(row, FieldBook.Ink, 2, 14);
                var sign = PauseIcon(icon, 59);
                sign.pickingMode = PickingMode.Position;
                sign.tooltip = label;
                row.Add(sign);
                var controls = PauseRow();
                controls.style.marginLeft = 32;
                controls.style.flexGrow = 1;
                controls.style.minWidth = 0;
                sign.RegisterCallback<PointerEnterEvent>(_ => { hoveredRow = row; Hint(); });
                sign.RegisterCallback<PointerLeaveEvent>(_ => { if (hoveredRow == row) hoveredRow = null; Hint(); });
                row.RegisterCallback<FocusInEvent>(_ => { focusedRow = row; Hint(); });
                row.RegisterCallback<FocusOutEvent>(evt =>
                {
                    if (focusedRow == row && !(evt.relatedTarget is VisualElement next && row.Contains(next))) focusedRow = null;
                    Hint();
                });
                row.RegisterCallback<GeometryChangedEvent>(_ =>
                {
                    if ((hoveredRow ?? focusedRow) == row) Hint();
                });
                row.Add(controls);
                rows.Add(row);
                return controls;
            }

            void Volume(string id, string icon, string key, float current, Action<float> change)
            {
                string label = _loc.T(key);
                var controls = Row(icon, label);
                var slot = new VisualElement();
                slot.style.height = 46;
                slot.style.flexGrow = 1;
                slot.style.minWidth = 90;
                var rail = new VisualElement { pickingMode = PickingMode.Ignore };
                rail.style.position = Position.Absolute;
                rail.style.left = rail.style.right = 18;
                rail.style.top = 14;
                rail.style.height = 20;
                rail.style.backgroundColor = FieldBook.Paper;
                GameUI.Border(rail, FieldBook.Ink, 3, 10);
                rail.style.overflow = Overflow.Hidden;
                var fill = new VisualElement { pickingMode = PickingMode.Ignore };
                fill.style.height = Length.Percent(100);
                fill.style.backgroundColor = FieldBook.Amber;
                rail.Add(fill);
                slot.Add(rail);
                var range = new VisualElement { pickingMode = PickingMode.Ignore };
                range.style.position = Position.Absolute;
                range.style.left = range.style.right = 18;
                range.style.height = 46;
                var thumb = new VisualElement { pickingMode = PickingMode.Ignore };
                thumb.style.position = Position.Absolute;
                thumb.style.top = 4;
                thumb.style.width = thumb.style.height = 36;
                thumb.style.marginLeft = -18;
                thumb.style.backgroundColor = FieldBook.Card;
                GameUI.Border(thumb, FieldBook.Ink, 4, 10);
                range.Add(thumb); slot.Add(range);

                var slider = new Slider(0, 1) { name = id, value = current, tooltip = label };
                slider.style.position = Position.Absolute;
                slider.style.left = slider.style.right = slider.style.top = slider.style.bottom = 0;
                slider.style.marginLeft = slider.style.marginRight = 0;
                slider.style.opacity = 0;
                var dragger = slider.Q<VisualElement>("unity-dragger");
                if (dragger != null) dragger.style.width = 36;
                slot.Add(slider);
                var value = PauseLabel(PausePercent(current), 31);
                value.style.minWidth = 106;
                value.style.marginLeft = 18;
                value.style.unityTextAlign = TextAnchor.MiddleRight;
                controls.Add(slot); controls.Add(value);
                void Paint(float amount)
                {
                    fill.style.width = thumb.style.left = Length.Percent(amount * 100);
                    value.text = PausePercent(amount);
                }
                slider.RegisterValueChangedCallback(evt =>
                {
                    float amount = Mathf.Round(evt.newValue * 100) / 100;
                    slider.SetValueWithoutNotify(amount);
                    change(amount);
                    Paint(amount);
                });
                slider.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode != KeyCode.LeftArrow && evt.keyCode != KeyCode.RightArrow) return;
                    slider.value = Mathf.Clamp01(slider.value + (evt.keyCode == KeyCode.RightArrow ? .01f : -.01f));
                    evt.StopImmediatePropagation();
                }, TrickleDown.TrickleDown);
                slider.RegisterCallback<FocusInEvent>(_ => GameUI.Border(thumb, FieldBook.Teal, 5, 10));
                slider.RegisterCallback<FocusOutEvent>(_ => GameUI.Border(thumb, FieldBook.Ink, 4, 10));
                Paint(current);
            }
            Volume("pause-master", "sound", "set.master", Settings.Master, x => Settings.Master = x);
            Volume("pause-effects", "sparkle", "set.effects", Settings.Effects, x => Settings.Effects = x);
            Volume("pause-ambience", "wave", "set.ambience", Settings.Ambience, x => Settings.Ambience = x);

            void Segments<T>(string id, string icon, string label, T[] values, T current, Func<int, string> caption, Action<T> set)
            {
                var controls = Row(icon, label);
                for (int i = 0; i < values.Length; i++)
                {
                    var choice = values[i];
                    string name = id + i;
                    var button = PauseButton(caption(i), () =>
                    {
                        _pauseFocus = name;
                        set(choice);
                        _ui.RebuildModal();
                    }, name, size: 25, selected: Equals(choice, current));
                    button.tooltip = label;
                    button.style.flexBasis = 0; button.style.flexGrow = 1;
                    button.style.marginRight = 4;
                    controls.Add(button);
                }
            }
            var sizes = new[] { 1f, 1.15f, 1.3f };
            Segments("pause-size-", "type", _loc.T("set.textsize"), sizes, Settings.TextScale, i => PausePercent(sizes[i]),
                x => { Settings.TextScale = x; RefreshAll(); });
            var speeds = new[] { 35, 70, 140, 0 };
            var speedKeys = new[] { "set.speed.slow", "set.speed.normal", "set.speed.fast", "set.speed.instant" };
            Segments("pause-speed-", "gauge", _loc.T("set.textspeed"), speeds, Settings.TextSpeed, i => _loc.T(speedKeys[i]),
                x => Settings.TextSpeed = x);

            void Switch(string name, string icon, string key, Func<bool> get, Action<bool> set)
            {
                string label = _loc.T(key);
                var controls = Row(icon, label);
                var state = PauseLabel("", 28);
                state.style.flexGrow = 1;
                state.style.unityTextAlign = TextAnchor.MiddleRight;
                var button = new Button { name = name, tooltip = label };
                button.style.width = 126; button.style.height = 48;
                button.style.marginLeft = button.style.marginRight = 0;
                PausePadding(button, 4, 4);
                var knob = new VisualElement { pickingMode = PickingMode.Ignore };
                knob.style.width = knob.style.height = 34;
                knob.style.backgroundColor = FieldBook.Card;
                GameUI.Border(knob, FieldBook.Ink, 3, 18);
                button.Add(knob); controls.Add(button); controls.Add(state);
                bool focused = false;
                void Paint()
                {
                    bool on = get();
                    button.style.flexDirection = FlexDirection.Row;
                    button.style.justifyContent = on ? Justify.FlexEnd : Justify.FlexStart;
                    button.style.backgroundColor = on ? FieldBook.Amber : FieldBook.Teal;
                    button.style.backgroundImage = StyleKeyword.None;
                    GameUI.Border(button, focused ? FieldBook.Rim : FieldBook.Ink, focused ? 5 : 3, 26);
                    state.text = GameUI.Thai(_loc.T(on ? "set.on" : "set.off"));
                }
                button.clicked += () =>
                {
                    _sfx.Play("click");
                    set(!get()); Paint();
                };
                button.RegisterCallback<FocusInEvent>(_ => { focused = true; Paint(); });
                button.RegisterCallback<FocusOutEvent>(_ => { focused = false; Paint(); });
                if (key == "set.fullscreen") button.schedule.Execute(Paint).Every(120);
                Paint();
            }
            Switch("pause-motion", "motion", "set.motion", () => Settings.ReducedMotion, x => Settings.ReducedMotion = x);
            Switch("pause-fullscreen", "full", "set.fullscreen", () => Screen.fullScreen, x => Screen.fullScreen = x);

            var language = Row("globe", _loc.T("set.language"));
            void ChangeLanguage(string focus)
            {
                _pauseFocus = focus;
                ToggleLang();
            }
            language.Add(PauseButton("◀", () => ChangeLanguage("pause-language-prev"), "pause-language-prev", size: 23, selected: true));
            var code = PauseLabel(TT("English", "ไทย"), 30);
            code.style.flexGrow = 1;
            code.style.unityTextAlign = TextAnchor.MiddleCenter;
            language.Add(code);
            language.Add(PauseButton("▶", () => ChangeLanguage("pause-language-next"), "pause-language-next", size: 23, selected: true));
        }

        VisualElement PauseTile(VisualElement page)
        {
            var tile = new VisualElement();
            tile.style.marginBottom = 12;
            tile.style.flexShrink = 0;
            PausePadding(tile, 17, 13);
            GameUI.Border(tile, FieldBook.Ink, 2, 15);
            page.Add(tile);
            return tile;
        }

        void BuildPauseHelp(VisualElement spread)
        {
            bool station = LastDockMode && TopDownCh1;
            var keys = PauseScroll(PausePaper(spread, 45));
            var guide = PauseScroll(PausePaper(spread, 55));
            keys.style.height = guide.style.height = 630;
            void Control(string key, string name)
            {
                var row = PauseTile(keys);
                row.style.minHeight = 126;
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                var caps = PauseRow();
                caps.style.width = 200;
                caps.style.flexShrink = 1;
                caps.style.flexWrap = Wrap.Wrap;
                if (key == "WASD")
                {
                    var cluster = new VisualElement();
                    var top = PauseRow(); top.style.justifyContent = Justify.Center; top.Add(PauseKey("W")); cluster.Add(top);
                    var bottom = PauseRow();
                    foreach (string letter in new[] { "A", "S", "D" }) bottom.Add(PauseKey(letter));
                    cluster.Add(bottom); caps.Add(cluster);
                    caps.Add(PauseLabel("↑↓←→", 22));
                }
                else caps.Add(PauseKey(key));
                row.Add(caps); row.Add(PauseLabel(name, 33));
            }
            PauseHinges(spread, 45);

            if (!station)
            {
                for (int i = 1; i <= 11; i++)
                {
                    var section = PauseTile(i <= 5 ? keys : guide);
                    section.Add(PauseLabel(_loc.T($"howto.{i}.title"), 28));
                    section.Add(PauseLabel(_loc.T($"howto.{i}.body"), 23, false));
                }
                return;
            }
            Control("WASD", TT("Move", "เดิน"));
            Control("E", TT("Interact", "โต้ตอบ"));
            Control("M", TT("Field book", "สมุดภารกิจ"));
            Control("Esc", TT("Pause", "พักเกม"));

            var rules = _ld?.C ?? StationConfig.Game();
            VisualElement Section(string icon, string title, string text)
            {
                var card = PauseTile(guide);
                var head = PauseRow();
                var picture = PauseIcon(icon, 64);
                picture.style.marginRight = 18;
                head.Add(picture);
                var labels = new VisualElement(); labels.style.flexShrink = 1;
                labels.Add(PauseLabel(title, 38));
                labels.Add(PauseLabel(text, 26));
                head.Add(labels);
                card.Add(head);
                return card;
            }
            void Chips(VisualElement card, params (string icon, string label)[] contents)
            {
                var line = PauseRow();
                line.style.marginTop = 10;
                line.style.flexWrap = Wrap.Wrap;
                foreach (var data in contents)
                {
                    var chip = PauseRow();
                    chip.style.marginRight = chip.style.marginBottom = 7;
                    PausePadding(chip, 8, 3);
                    GameUI.Border(chip, FieldBook.Ink, 2, 9);
                    chip.Add(PauseIcon(data.icon, 32));
                    chip.Add(PauseLabel(data.label, 22));
                    line.Add(chip);
                }
                card.Add(line);
            }
            Chips(Section("ship", "Kestrel", TT("Load the lifeboat", "ขนเสบียงขึ้นยาน")),
                ("clock", TT($"{_cfg.chapter1.scrambleSeconds:0} sec", $"{_cfg.chapter1.scrambleSeconds:0} วิ")),
                ("hand", TT($"Carry {ScavengeInventory.CarryMax}", $"ถือ {ScavengeInventory.CarryMax}")),
                ("box", TT($"Pod holds {ScavengeInventory.PodMax}", $"ยานจุ {ScavengeInventory.PodMax}")));
            Chips(Section("radio", "Last Dock", TT($"Send {rules.signalNeeded} signals", $"ส่งพิกัด {rules.signalNeeded} ครั้ง")),
                ("battery", TT($"Reserve {rules.dockPower} power", $"สำรองแบต {rules.dockPower}")),
                ("day", TT($"Day {rules.days}", $"วันที่ {rules.days}")));
            Section("clock", TT($"{rules.workUnits} work slots/day", $"ทำงาน {rules.workUnits} ช่วง/วัน"),
                TT("Walk, read and switch machines free.", "เดิน อ่าน เปิดปิดเครื่อง ฟรี"));
        }

        void BuildPauseCards(VisualElement spread)
        {
            var have = CardsEver();
            var groups = _cfg.cards.Select(card => card.chapter).Distinct().ToArray();
            if (!groups.Contains(_pauseCardGroup)) _pauseCardGroup = groups.Length > 0 ? groups[0] : -1;
            var groupsView = PauseScroll(PausePaper(spread, 29));
            var page = PausePaper(spread, 71);
            var scroll = PauseScroll(page);
            scroll.style.height = groupsView.style.height = 610;
            PauseHinges(spread, 29);
            foreach (int chapter in groups)
            {
                string group = chapter switch
                {
                    1 => TT("Space", "อวกาศ"),
                    2 => TT("Mars", "ดาวอังคาร"),
                    3 => TT("Moon", "ดวงจันทร์"),
                    4 => TT("Titan", "ไททัน"),
                    _ => _loc.T("codex.group." + chapter)
                };
                string icon = chapter switch { 1 => "planet", 2 => "mars", 3 => "moon", 4 => "titan", _ => "codex" };
                string name = "pause-group-" + chapter;
                var button = PauseButton(group, () =>
                {
                    _pauseCardGroup = chapter;
                    _pauseOpenCard = null;
                    _pauseCardScroll = 0;
                    _pauseFocus = name;
                    _ui.RebuildModal();
                }, name, icon, 28, _pauseCardGroup == chapter);
                button.style.minHeight = 99;
                button.style.marginBottom = 12;
                groupsView.Add(button);
            }
            if (_pauseOpenCard != null && have.Contains(_pauseOpenCard))
            {
                var article = PauseTile(scroll);
                article.Add(PauseLabel(_loc.T($"card.{_pauseOpenCard}.title"), 38));
                article.Add(PauseLabel(_loc.T($"card.{_pauseOpenCard}.body"), 28, false));
                return;
            }
            var grid = PauseRow();
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.alignItems = Align.Stretch;
            grid.style.justifyContent = Justify.FlexStart;
            scroll.Add(grid);
            int column = 0;
            foreach (var card in _cfg.cards.Where(c => c.chapter == _pauseCardGroup))
            {
                var tile = new VisualElement();
                PausePadding(tile, 7, 7);
                tile.style.width = Length.Percent(24);
                tile.style.minHeight = 260;
                tile.style.marginRight = Length.Percent(column++ % 4 == 3 ? 0 : 1.2f);
                tile.style.marginBottom = 13;
                tile.style.alignItems = Align.Stretch;
                var border = GameUI.Plate(FieldBook.Paper, FieldBook.Ink, 19, false, 6);
                tile.Add(border);
                if (have.Contains(card.id))
                {
                    var button = PauseButton(_loc.T($"card.{card.id}.title"), () =>
                    {
                        _pauseOpenCard = card.id;
                        _pauseCardScroll = scroll.scrollOffset.y;
                        _pauseFocus = "pause-back";
                        _ui.RebuildModal();
                    }, "pause-card-" + card.id, size: 24);
                    button.style.flexGrow = 1; button.style.flexShrink = 1;
                    tile.Add(button);
                }
                else
                {
                    var inner = GameUI.Plate(FieldBook.Card, FieldBook.Tan, 15, false, 5);
                    inner.style.left = inner.style.top = inner.style.right = inner.style.bottom = 13;
                    tile.Add(inner);
                    tile.style.justifyContent = Justify.Center; tile.style.alignItems = Align.Center;
                    tile.Add(PauseIcon("lock", 75));
                    tile.RegisterCallback<PointerEnterEvent>(_ => GameUI.Border(tile, FieldBook.Amber, 4, 13));
                    tile.RegisterCallback<PointerLeaveEvent>(_ => GameUI.Border(tile, Color.clear, 0, 13));
                }
                grid.Add(tile);
            }
            if (!_cfg.cards.Any(c => c.chapter == _pauseCardGroup && have.Contains(c.id)))
            {
                var empty = PauseLabel(TT("No cards found", "ยังไม่พบการ์ด"), 32);
                empty.style.marginTop = 12;
                empty.style.unityTextAlign = TextAnchor.MiddleCenter;
                scroll.Add(empty);
            }
            _pauseGrid = scroll;
            scroll.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (scroll == _pauseGrid && _pauseCardScroll > 0)
                    scroll.scrollOffset = new Vector2(0, _pauseCardScroll);
            });
        }

        static void DrawPauseIcon(Painter2D p, string id, int size)
        {
            float px = size / 32f;
            Vector2 At(float x, float y) => new Vector2(x * px, y * px);
            void Shape(Color fill, params Vector2[] points)
            {
                p.BeginPath(); p.MoveTo(points[0]);
                for (int i = 1; i < points.Length; i++) p.LineTo(points[i]);
                p.ClosePath(); p.fillColor = fill; p.strokeColor = FieldBook.Ink;
                p.lineWidth = 1.75f * px; p.Fill(); p.Stroke();
            }
            void Rect(float x, float y, float w, float h, Color fill) =>
                Shape(fill, At(x + 1, y), At(x + w - 1, y), At(x + w, y + 1), At(x + w, y + h - 1),
                    At(x + w - 1, y + h), At(x + 1, y + h), At(x, y + h - 1), At(x, y + 1));
            void Line(float x, float y, float u, float v, float width = 2)
            {
                p.BeginPath(); p.MoveTo(At(x, y)); p.LineTo(At(u, v));
                p.strokeColor = FieldBook.Ink; p.lineWidth = width * px; p.Stroke();
            }
            void Disc(float x, float y, float r, Color fill)
            {
                p.BeginPath(); p.Arc(At(x, y), r * px, Angle.Degrees(0), Angle.Degrees(360));
                p.fillColor = fill; p.strokeColor = FieldBook.Ink; p.lineWidth = 2 * px;
                p.Fill(); p.Stroke();
            }
            var amber = FieldBook.Amber;
            switch (id)
            {
                case "pause": Rect(6, 4, 7, 24, amber); Rect(19, 4, 7, 24, amber); break;
                case "cards": Rect(4, 9, 15, 19, amber); Rect(9, 6, 15, 19, amber); Rect(14, 3, 15, 19, amber); break;
                case "wave":
                    for (int i = 0; i < 5; i++)
                    {
                        int h = 5 + (2 - Mathf.Abs(2 - i)) * 6;
                        Rect(4 + i * 5, 16 - h / 2f, 2, h, amber);
                    }
                    break;
                case "motion": Line(12, 8, 28, 8, 3); Line(7, 16, 24, 16, 3); Line(3, 24, 20, 24, 3); break;
                case "full":
                    Line(4, 12, 4, 4); Line(4, 4, 12, 4); Line(20, 4, 28, 4); Line(28, 4, 28, 12);
                    Line(28, 20, 28, 28); Line(28, 28, 20, 28); Line(12, 28, 4, 28); Line(4, 28, 4, 20);
                    break;
                case "ship":   // the Kestrel lifeboat: flat hull, cockpit dome, two legs
                    Line(9, 22, 7, 27); Line(23, 22, 25, 27);
                    Shape(amber, At(10, 13), At(12, 7), At(20, 7), At(22, 13));
                    Shape(amber, At(1, 17), At(5, 13), At(27, 13), At(31, 17), At(27, 22), At(5, 22));
                    Rect(13, 9, 6, 3, FieldBook.Card); break;
                case "pad":    // game controller
                    Shape(amber, At(4, 12), At(9, 8), At(23, 8), At(28, 12), At(30, 22), At(26, 25),
                        At(21, 20), At(11, 20), At(6, 25), At(2, 22));
                    Line(10, 11, 10, 17, 2.5f); Line(7, 14, 13, 14, 2.5f);
                    Disc(21, 12.5f, 1f, FieldBook.Ink); Disc(24.5f, 15.5f, 1f, FieldBook.Ink); break;
                case "door":   // main menu: a door with a knob
                    Rect(8, 3, 16, 25, amber); Rect(11, 6, 10, 9, FieldBook.Card);
                    Disc(20, 19, .9f, FieldBook.Ink); Line(4, 29, 28, 29, 2.5f); break;
                case "sparkle": // one big four-point star and a small one
                    Shape(amber, At(12, 4), At(14.5f, 14.5f), At(24, 17), At(14.5f, 19.5f), At(12, 30),
                        At(9.5f, 19.5f), At(1, 17), At(9.5f, 14.5f));
                    Shape(amber, At(25, 2), At(26.3f, 5.7f), At(30, 7), At(26.3f, 8.3f), At(25, 12),
                        At(23.7f, 8.3f), At(20, 7), At(23.7f, 5.7f)); break;
                case "planet": // ringed planet: back of the ring, planet, then the front of the ring
                {
                    void Ring(float from, float to)
                    {
                        const float tilt = -.42f;
                        p.BeginPath();
                        for (int i = 0; i <= 24; i++)
                        {
                            float t = Mathf.Lerp(from, to, i / 24f);
                            float x = 15 * Mathf.Cos(t), y = 4.6f * Mathf.Sin(t);
                            var pt = At(16 + x * Mathf.Cos(tilt) - y * Mathf.Sin(tilt), 16 + x * Mathf.Sin(tilt) + y * Mathf.Cos(tilt));
                            if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
                        }
                        p.strokeColor = FieldBook.Ink; p.lineWidth = 2.6f * px; p.Stroke();
                    }
                    Ring(Mathf.PI, 2 * Mathf.PI);
                    Disc(16, 16, 9, amber);
                    Ring(0, Mathf.PI);
                    break;
                }
                case "titan":  // banded moon
                    Disc(16, 16, 12, amber);
                    Line(5.1f, 11, 26.9f, 11, 1.6f); Line(4, 17, 28, 17, 1.6f); Line(5.6f, 22, 26.4f, 22, 1.6f); break;
                case "box": Rect(4, 6, 24, 20, amber); Line(4, 12, 28, 12); Line(7, 24, 24, 13); break;
                case "hand":
                    Shape(amber, At(11, 28), At(5, 16), At(8, 14), At(12, 19), At(12, 7), At(15, 4),
                        At(18, 5), At(18, 13), At(22, 8), At(26, 10), At(26, 21), At(22, 28));
                    break;
                case "day": Rect(5, 7, 22, 21, amber); Line(5, 13, 27, 13); Line(10, 4, 10, 10); Line(22, 4, 22, 10); Rect(13, 17, 6, 5, FieldBook.Ink); break;
                case "clock": Disc(16, 16, 12, amber); Line(16, 9, 16, 17); Line(16, 17, 22, 17); break;
                case "restart":
                    p.BeginPath(); p.Arc(At(16, 16), 10 * px, Angle.Degrees(35), Angle.Degrees(326));
                    p.strokeColor = FieldBook.Ink; p.lineWidth = 3.4f * px; p.Stroke();
                    Shape(amber, At(25, 4), At(27, 13), At(18, 12)); break;
                case "mars":
                    Disc(16, 16, 12, amber); Disc(12, 11, 2, FieldBook.Tan);
                    Disc(20, 19, 3, FieldBook.Tan); break;
            }
        }
    }
}
