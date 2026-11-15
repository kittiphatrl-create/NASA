using System.Collections.Generic;
using System.Linq;
using TitanGate.Core.LastDock;
using DockRoom = TitanGate.Core.LastDock.Room;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        FieldBook _fieldBook;
        VisualElement _bookHint;
        Label _playAlert;
        bool _fieldBookClosing;
        bool IsBookLevel => _bRoot != null && (_bMode == 1 || _bMode == 2 && _ld != null && !_ldEnded);
        bool CanOpenBook => IsBookLevel && !InCutscene && !_busy && !ActionLocked && !_ldSeq &&
            !_ui.Blocking && (_bMode != 1 || !_sDone && !_sSitting);

        bool UpdateFieldBook(Keyboard keys)
        {
            if (_fieldBook == null)
            {
                if (keys == null || !keys.mKey.wasPressedThisFrame || !CanOpenBook) return false;
                OpenFieldBook();
                return true;
            }

            if (!IsBookLevel || InCutscene || _ui.DialogOpen || _ui.ScreenOpen || _ui.ModalOpen)
            {
                EndFieldBook();
                return false;
            }

            if (keys != null)
            {
                if (keys.mKey.wasPressedThisFrame || keys.escapeKey.wasPressedThisFrame)
                    CloseFieldBook();
                else if (keys.rightArrowKey.wasPressedThisFrame)
                    _fieldBook.Select(_fieldBook.Requested + 1);
                else if (keys.leftArrowKey.wasPressedThisFrame)
                    _fieldBook.Select(_fieldBook.Requested - 1);
                if (keys.f12Key.wasPressedThisFrame) Capture("field-book");
            }
            if (!_fieldBook.Tick(Time.unscaledDeltaTime)) EndFieldBook();
            return true;
        }

        string BookWord(string key, params object[] args) => _loc.T("book." + key, args);

        string BookInteraction(BSpot spot)
        {
            string name = _loc.Has(spot.nameKey) ? _loc.T(spot.nameKey) : spot.nameKey;
            if (_bMode == 2)
            {
                if (_ld.PowerDipPending || _ld.RaiderPending) return BookWord("use.alert");
                if (spot.id == "ld_radio") return BookWord(_ld.RadioFixed ? "use.comms" : "use.repair");
                if (spot.id == "ld_table") return BookWord("use.dinner");
                return BookWord("use.open", name);
            }
            if (spot.id == "keycard" || _sScramble && spot.id.StartsWith("sup_"))
                return BookWord("use.take", name);
            if (_sScramble && spot.id == "lifeboat") return BookWord("use.stow");
            if (spot.id == _sTarget && spot.id == "radio") return BookWord("use.report");
            if (spot.id == _sTarget && spot.id == "sofa") return BookWord("use.rest");
            if (spot.id.StartsWith("vault_")) return BookWord("use.open", name);
            return BookWord("use.look", name);
        }

        void OpenFieldBook()
        {
            if (_fieldBook != null || !CanOpenBook) return;
            _ui.SetHover(null, Vector2.zero);
            _ui.ClearNotices();
            if (_bookHint != null) _bookHint.style.display = DisplayStyle.None;
            if (_playAlert != null) _playAlert.style.display = DisplayStyle.None;
            // The normal blocking UI pauses the evacuation timer. UpdateFieldBook consumes
            // the gameplay update while open, preserving that behavior and stopping movement.
            _fieldBookClosing = false;
            _fieldBook = new FieldBook(_ui, _bMode == 2 ? BookWord("station") : "KESTREL",
                new[] { BookWord("status"), BookWord("mission"), BookWord("items") },
                _loc.T("ui.close"), BuildBookHeader, PopulateBook, () => _sfx.Play("card", .55f),
                CloseFieldBook, TrackBookTurn);
            _sfx.Play("open");
        }

        void CloseFieldBook()
        {
            if (_fieldBook == null || _fieldBookClosing) return;
            _fieldBookClosing = true;
            _sfx.Play("close");
            _fieldBook.Close();
        }

        void EndFieldBook()
        {
            _fieldBook?.Dispose();
            _fieldBook = null;
            _fieldBookClosing = false;
        }

        void EndPlayOverlay()
        {
            EndFieldBook();
            _bookHint?.RemoveFromHierarchy();
            _playAlert?.RemoveFromHierarchy();
            _bookHint = null;
            _playAlert = null;
        }

        void RefreshPlayOverlay(string alert = null, Color? accent = null)
        {
            bool show = IsBookLevel && !InCutscene && !_ui.Blocking && _fieldBook == null;
            if (_bookHint == null)
            {
                var button = new Button(OpenFieldBook) { name = "open-field-book" };
                var label = _ui.CutButton(button, true, "M", 22, LdIcon("book"));
                label.name = "open-book-label";
                button.style.position = Position.Absolute;
                button.style.right = 24;
                button.style.bottom = 20;
                button.style.paddingLeft = button.style.paddingRight = 18;
                button.style.height = 52;
                _ui.Root.Add(button);
                _bookHint = button;
                _playAlert = _ui.L("", 23, true, FieldBook.Ink);
                _playAlert.style.position = Position.Absolute;
                _playAlert.style.top = 19;
                _playAlert.style.left = Length.Percent(22);
                _playAlert.style.right = Length.Percent(22);
                _playAlert.style.paddingTop = _playAlert.style.paddingBottom = 8;
                _playAlert.style.paddingLeft = _playAlert.style.paddingRight = 18;
                _playAlert.style.unityTextAlign = TextAnchor.MiddleCenter;
                _ui.Root.Add(_playAlert);
            }
            _bookHint.style.display = show && CanOpenBook ? DisplayStyle.Flex : DisplayStyle.None;
            _bookHint.Q<Label>("open-book-label").text = BookWord("open");
            _bookHint.tooltip = BookWord("open");
            _playAlert.style.display = show && !string.IsNullOrEmpty(alert) ? DisplayStyle.Flex : DisplayStyle.None;
            if (show && !string.IsNullOrEmpty(alert))
            {
                _playAlert.text = GameUI.Thai(alert);
                _playAlert.style.backgroundColor = FieldBook.Paper;
                GameUI.Border(_playAlert, accent ?? FieldBook.Red, 4, 10);
            }
        }

        Label BookText(string text, int size, bool bold = false, Color? color = null)
            => _ui.L(text, size, bold, color ?? FieldBook.Ink);

        static VisualElement BookRow()
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            return row;
        }

        static void AddBookCard(VisualElement page, VisualElement card, float height)
        {
            card.style.minHeight = height;
            card.style.marginBottom = 13;
            card.style.paddingLeft = card.style.paddingRight = 17;
            card.style.paddingTop = card.style.paddingBottom = 8;
            card.style.flexShrink = 0;
            page.Add(card);
        }

        void BuildBookHeader(VisualElement panel)
        {
            panel.style.alignItems = Align.Center;
            bool station = _bMode == 2;
            string title = station ? BookWord("day", _ld.Day, _ld.C.days) :
                _sScramble ? EscapeSummary() : BookWord("morning");
            panel.Add(BookText(title, 40, true));
            if (!station)
            {
                if (_sScramble) panel.Add(BookText(BookWord("ship.storage", _sHands.Count, HandSlots, _stowed.Count, StowSlots), 25, true));
                return;
            }
            var row = BookRow();
            row.Add(FieldBook.Icon("sun", 38));
            var slots = BookSegments(_ld.WorkUnits, _ld.C.workUnits, 29);
            slots.style.width = 260;
            slots.style.marginLeft = slots.style.marginRight = 10;
            // Available segments stay on the right of the elapsed work units.
            slots.style.flexDirection = FlexDirection.RowReverse;
            row.Add(slots);
            row.Add(FieldBook.Icon("moon", 38));
            panel.Add(row);
        }

        string EscapeSummary() => BookWord("ship.evac", Mathf.CeilToInt(Mathf.Max(0, _sLeft)));

        void PopulateBook(int tab, VisualElement left, VisualElement right)
        {
            if (tab == FieldBook.Items) BuildBookItems(left, right);
            else if (tab == FieldBook.Mission)
            {
                if (_bMode == 2) BuildStationMissions(left, right);
                else BuildShipMissions(left, right);
            }
            else if (_bMode == 2) BuildStationStatus(left, right);
            else BuildShipStatus(left, right);
        }

        void BookHeading(VisualElement parent, string text, string icon)
        {
            var row = BookRow();
            row.style.marginBottom = 13;
            var mark = FieldBook.Icon(icon, 49);
            mark.style.marginRight = 10;
            row.Add(mark);
            row.Add(BookText(text, 37, true));
            parent.Add(row);
        }

        static VisualElement BookSegments(int filled, int total, int height)
        {
            var row = BookRow();
            for (int i = 0; i < total; i++)
            {
                var segment = FieldBook.Well(i < filled ? FieldBook.Amber : FieldBook.Paper, 6);
                segment.style.flexGrow = 1;
                segment.style.flexBasis = 0;
                segment.style.height = height;
                segment.style.marginRight = 3;
                row.Add(segment);
            }
            return row;
        }

        static VisualElement BookMeter(float fraction, int height, Color fill)
        {
            var track = FieldBook.Well(FieldBook.Paper, 12);
            track.style.height = height;
            track.style.overflow = Overflow.Hidden;
            var level = new VisualElement { pickingMode = PickingMode.Ignore };
            level.style.backgroundColor = fill;
            level.style.width = Length.Percent(Mathf.Clamp01(fraction) * 100);
            level.style.flexGrow = 1;
            track.Add(level);
            return track;
        }

        void BookResource(VisualElement page, string name, string icon, int count, int max = 0, bool units = false)
        {
            var card = FieldBook.Well();
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;
            var glyph = FieldBook.Icon(icon, 75);
            glyph.style.marginRight = 14;
            card.Add(glyph);
            var middle = new VisualElement { pickingMode = PickingMode.Ignore };
            middle.style.flexGrow = 1;
            middle.Add(BookText(name, 35, true));
            if (max > 0 || units)
            {
                var meter = units ? BookSegments(Mathf.Min(count, 16), Mathf.Clamp(count, 6, 16), 26) :
                    BookMeter((float)count / max, 27, FieldBook.Amber);
                meter.style.marginTop = 5;
                middle.Add(meter);
            }
            card.Add(middle);
            var value = BookText(max > 0 ? $"{count}/{max}" : count.ToString(), 43, true);
            value.style.marginLeft = 17;
            card.Add(value);
            AddBookCard(page, card, 128);
        }

        void AddBookPortrait(VisualElement page, float height)
        {
            var card = FieldBook.Well();
            card.style.alignItems = Align.Center;
            var frame = FieldBook.Well(FieldBook.Green);
            frame.style.width = 150;
            frame.style.height = 146;
            frame.style.overflow = Overflow.Hidden;
            var portrait = new Image { sprite = Art.Crew("cadet", 0, -1), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            portrait.style.height = 221;
            portrait.style.flexShrink = 0;
            // Frame the face and shoulders of the real in-game cadet sprite.
            FieldBook.Place(portrait, -20, -7, 184, 221);
            frame.Add(portrait);
            card.Add(frame);
            AddBookCard(page, card, height);
        }

        void BuildStationStatus(VisualElement left, VisualElement right)
        {
            var s = _ld;
            BookResource(left, LdRes(Res.Food), "food", s.Food, units: true);
            BookResource(left, LdRes(Res.Oxygen), "o2", s.Oxygen, units: true);
            BookResource(left, LdRes(Res.Battery), "bolt", s.Battery, s.C.batteryCap);
            BookResource(left, BookWord("water"), "water", s.Water);
            AddBookPortrait(right, 195);
            BookCondition(right, BookWord("heat"), "heat", s.HeatLevel, s.Heat, s.C.heatTrip, s.C.heatCritical);
            BookCondition(right, "CO₂", "co2", s.Co2Level);
        }

        void BookCondition(VisualElement page, string title, string icon, string level, int amount = 0, int high = 0, int critical = 0)
        {
            var tint = level == "critical" ? FieldBook.Red : level == "high" ? FieldBook.Amber : FieldBook.Green;
            var card = FieldBook.Well();
            var row = BookRow();
            var mark = FieldBook.Icon(icon, 48);
            mark.style.marginRight = 10;
            row.Add(mark);
            var name = BookText(title, 35, true);
            name.style.flexGrow = 1;
            row.Add(name);
            var badge = FieldBook.Well(tint);
            badge.style.minWidth = 155;
            var text = BookText(LdLevel(level), 31, true);
            text.style.unityTextAlign = TextAnchor.MiddleCenter;
            badge.Add(text);
            row.Add(badge);
            card.Add(row);
            if (critical > high)
            {
                var gauge = BookRow();
                gauge.style.marginTop = 14;
                float end = critical + Mathf.Max(2, critical - high);
                gauge.style.height = 40;
                gauge.style.backgroundColor = FieldBook.Ink;
                gauge.style.overflow = Overflow.Hidden;
                GameUI.Border(gauge, FieldBook.Ink, 3, 13);
                foreach (var band in new[] { (high / end, FieldBook.Green), ((critical - high) / end, FieldBook.Amber), ((end - critical) / end, FieldBook.Red) })
                {
                    var zone = new VisualElement { pickingMode = PickingMode.Ignore };
                    zone.style.width = Length.Percent(band.Item1 * 100);
                    zone.style.height = 40;
                    zone.style.backgroundColor = band.Item2;
                    zone.style.borderRightWidth = 2;
                    zone.style.borderRightColor = FieldBook.Ink;
                    gauge.Add(zone);
                }
                var marker = FieldBook.Well(FieldBook.Paper, 14);
                marker.style.position = Position.Absolute;
                marker.style.left = Length.Percent(Mathf.Lerp(1, 94, Mathf.Clamp01(amount / end)));
                marker.style.width = marker.style.height = 27;
                gauge.Add(marker);
                card.Add(gauge);
            }
            AddBookCard(page, card, critical > high ? 174 : 125);
        }

        void BuildShipStatus(VisualElement left, VisualElement right)
        {
            AddBookPortrait(right, 240);
            if (_sScramble)
            {
                BookHeading(left, BookWord("ship.evac.title"), "time");
                BookResource(left, BookWord("ship.hands"), "bag", _sHands.Count, HandSlots);
                BookResource(left, BookWord("ship.pod"), "rescue", _stowed.Count, StowSlots);
            }
            else
            {
                BookHeading(left, BookWord("mission"), "radio");
                BookTask(left, ShipObjective(), "", _sTarget == null, 152);
            }
        }

        string ShipObjective() => _sTarget == null ? BookWord("done") :
            _loc.T(SSteps[_sStep].textKey, _sPet != null ? _loc.T($"pet.{_sPet}.name") : "");

        void BookTask(VisualElement page, string title, string description, bool done, float height = 100)
        {
            var card = FieldBook.Well(done ? new Color32(216, 229, 186, 255) : FieldBook.Card);
            var row = BookRow();
            var icon = FieldBook.Icon(done ? "check" : "marker", 31);
            icon.style.marginRight = 8;
            row.Add(icon);
            var text = BookText(title, 30, true);
            text.style.flexShrink = 1;
            row.Add(text);
            card.Add(row);
            if (!string.IsNullOrEmpty(description)) card.Add(BookText(description, 24, false, FieldBook.Muted));
            AddBookCard(page, card, height);
        }

        void BuildStationMissions(VisualElement left, VisualElement right)
        {
            var s = _ld;
            BookHeading(left, BookWord("radio", s.RadioRepair, s.C.radioRepairDays), "radio");
            left.Add(BookText(LdRoomName(DockRoom.Comms), 25, true, FieldBook.Muted));
            BookTask(left, BookWord("board"), BookWord("board.cost"), s.RadioRepair >= 1);
            BookTask(left, BookWord("power"), BookWord("power.cost"), s.RadioRepair >= 2);
            BookTask(left, BookWord("antenna"), BookWord("antenna.site"), s.RadioFixed);

            BookHeading(right, BookWord("rescue"), "rescue");
            BookTask(right, BookWord("signal", s.Signal, s.C.signalNeeded), LdRoomName(DockRoom.Comms), s.Signal >= s.C.signalNeeded);
            BookTask(right, BookWord("dock", s.C.days), BookWord("dock.reserve", s.C.dockPower), s.Outcome == Outcome.Rescued);
            BookTask(right, BookWord("beacon"), s.BeaconBuilt ? BookWord("beacon.power") :
                BookWord("beacon.parts", s.RoverParts, s.C.beaconParts), s.BeaconBuilt, 122);
        }

        void BuildShipMissions(VisualElement left, VisualElement right)
        {
            BookHeading(left, _sScramble ? BookWord("ship.evac.title") : BookWord("ship.report"), "radio");
            if (_sScramble)
            {
                BookTask(left, BookWord("ship.collect"), BookWord("ship.carry.limit", HandSlots), _stowed.Count >= StowSlots, 146);
                BookHeading(right, BookWord("ship.pod"), "rescue");
                BookTask(right, BookWord("ship.stow", _stowed.Count, StowSlots), "", _stowed.Count >= StowSlots, 146);
            }
            else
            {
                for (int i = 0; i < SSteps.Length; i++)
                {
                    var step = SSteps[i];
                    string title = _loc.T(step.textKey, _sPet != null ? _loc.T($"pet.{_sPet}.name") : "");
                    BookTask(i <= _sStep ? left : right, title, "", i < _sStep || _sDone, 109);
                }
            }
        }

        readonly struct BookItem
        {
            public readonly string Name, Icon, Description;
            public readonly int Count;
            public readonly Sprite Sprite;

            public BookItem(string name, int count, string icon, string description, Sprite sprite = null)
            {
                Name = name;
                Count = count;
                Icon = icon;
                Description = description;
                Sprite = sprite;
            }
        }

        static string BookSupplyIcon(Res r) => r switch
        {
            Res.Food => "food", Res.Oxygen => "o2", Res.Battery => "battery", Res.Parts => "gear",
            Res.Solar => "sun", Res.RadioKit => "radio", Res.RadMeter => "gauge", Res.FrozenFood => "food",
            Res.Flashlight => "flashlight", _ => "medkit",
        };

        BookItem BookSupply(string id, int count, string description = null)
        {
            Res r = LdResOfSupply(id);
            return new BookItem(LastDockMode ? LdRes(r) : _loc.T($"sup.{id}.name"), count, BookSupplyIcon(r),
                description ?? BookWord("item." + r.ToString().ToLowerInvariant()), Art.LooseSupply(id));
        }

        List<BookItem> StationBookItems()
        {
            var s = _ld;
            var items = new List<BookItem>();
            void Add(string id, int count)
            {
                if (count > 0) items.Add(BookSupply(id, count));
            }
            Add("sup_food", s.Food);
            Add("sup_o2", s.Oxygen);
            Add("sup_battery", s.Battery);
            Add("sup_parts", s.Parts);
            if (s.Water > 0) items.Add(new BookItem(BookWord("water"), s.Water, "water", BookWord("item.water")));
            if (s.Absorbers > 0) items.Add(new BookItem(BookWord("absorber"), s.Absorbers, "can", BookWord("item.absorber")));
            Add("sup_solar", s.HasSolar ? 1 : 0);
            Add("sup_radiokit", s.HasRadioKit && !s.RadioFixed ? 1 : 0);
            Add("sup_radmeter", s.HasRadMeter ? 1 : 0);
            Add("sup_flashlight", s.HasFlashlight ? 1 : 0);
            Add("sup_medkit", s.Medkits);
            if (s.MysteryFood > 0) items.Add(new BookItem(BookWord("mystery"), s.MysteryFood, "food", BookWord("item.mystery")));
            if (s.RoverParts > 0) items.Add(new BookItem(BookWord("rover.parts"), s.RoverParts, "rover", BookWord("item.rover")));
            return items;
        }

        void BuildBookItems(VisualElement left, VisualElement right)
        {
            bool station = _bMode == 2;
            BookHeading(left, BookWord(station ? "storage" : _sScramble ? "items" : "ship.hands"), "bag");
            var list = station ? StationBookItems() : _sHands.GroupBy(id => id).Select(g => BookSupply(g.Key, g.Count())).ToList();
            if (!station && _sCarry != null && !_sScramble)
                list.Add(new BookItem(_loc.T($"sup.{_sCarry}.name"), 1, "bag", ShipObjective(), Art.LooseSupply(_sCarry)));
            if (!station && _ldHasCard)
                list.Add(new BookItem(BookWord("keycard"), 1, "can", BookWord("item.keycard")));

            var details = new VisualElement { pickingMode = PickingMode.Ignore };
            var packed = new List<BookItem>();
            if (!station && _sScramble)
                packed.AddRange(_stowed.GroupBy(id => id).Select(g => BookSupply(g.Key, g.Count(), BookWord("item.stowed"))));
            right.Add(details);

            void Detail(BookItem? item)
            {
                details.Clear();
                if (item == null)
                {
                    details.Add(BookText(BookWord("empty"), 31, true, FieldBook.Muted));
                    return;
                }
                var value = item.Value;
                var panel = FieldBook.Well(FieldBook.Card);
                panel.style.width = 370;
                panel.style.height = 284;
                panel.style.alignSelf = Align.Center;
                panel.style.alignItems = Align.Center;
                panel.Add(BookItemImage(value, 214));
                details.Add(panel);
                var title = BookText(value.Name + $" ×{value.Count}", 33, true);
                title.style.marginTop = 16;
                title.style.unityTextAlign = TextAnchor.MiddleCenter;
                details.Add(title);
                var body = BookText(value.Description, 27, false, FieldBook.Muted);
                body.style.unityTextAlign = TextAnchor.MiddleCenter;
                details.Add(body);
            }

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            left.Add(scroll);
            Button selected = null;
            void Select(Button button, BookItem item)
            {
                if (selected != null) selected.style.backgroundColor = FieldBook.Card;
                selected = button;
                selected.style.backgroundColor = FieldBook.Amber;
                Detail(item);
            }
            void AddGrid(List<BookItem> items, int minSlots)
            {
                var grid = new VisualElement();
                grid.style.flexDirection = FlexDirection.Row;
                grid.style.flexWrap = Wrap.Wrap;
                scroll.Add(grid);
                for (int i = 0; i < Mathf.Max(minSlots, items.Count); i++)
                {
                    VisualElement slot;
                    if (i < items.Count)
                    {
                        var item = items[i];
                        Button button = null;
                        button = BookItemButton(item.Name, () => { _sfx.Play("click"); Select(button, item); });
                        button.style.backgroundColor = FieldBook.Card;
                        button.Add(BookItemImage(item, station ? 107 : 59));
                        var label = BookText($"×{item.Count}", station ? 34 : 24, true);
                        label.style.unityTextAlign = TextAnchor.MiddleCenter;
                        button.Add(label);
                        if (selected == null) Select(button, item);
                        slot = button;
                    }
                    else slot = FieldBook.Well(FieldBook.Paper);
                    slot.style.width = Length.Percent(station ? 31 : 46);
                    slot.style.height = station ? 204 : 110;
                    slot.style.marginRight = 8;
                    slot.style.marginBottom = 9;
                    grid.Add(slot);
                }
            }
            if (!station && _sScramble)
            {
                scroll.Add(BookText(BookWord("ship.hands") + $"  {_sHands.Count}/{HandSlots}", 27, true));
                if (list.Count == 0) scroll.Add(BookText(BookWord("empty"), 24, false, FieldBook.Muted));
                AddGrid(list, 0);
                scroll.Add(BookText(BookWord("ship.pod") + $"  {_stowed.Count}/{StowSlots}", 27, true));
                if (packed.Count == 0) scroll.Add(BookText(BookWord("empty"), 24, false, FieldBook.Muted));
                AddGrid(packed, 0);
            }
            else AddGrid(list, 6);
            if (selected == null) Detail(null);
        }

        Button BookItemButton(string name, System.Action select)
        {
            var button = new Button(select) { tooltip = name };
            button.style.justifyContent = Justify.Center;
            button.style.alignItems = Align.Center;
            GameUI.Border(button, FieldBook.Ink, 3, 13);
            button.RegisterCallback<FocusInEvent>(_ => GameUI.Border(button, FieldBook.Teal, 5, 13));
            button.RegisterCallback<FocusOutEvent>(_ => GameUI.Border(button, FieldBook.Ink, 3, 13));
            button.RegisterCallback<PointerEnterEvent>(_ => button.style.borderTopColor = FieldBook.Teal);
            button.RegisterCallback<PointerLeaveEvent>(_ => button.style.borderTopColor = FieldBook.Ink);
            return button;
        }

        static VisualElement BookItemImage(BookItem item, int size)
        {
            if (item.Sprite == null || FieldBookIcons.Has(item.Icon)) return FieldBook.Icon(item.Icon, size);
            var image = new Image { sprite = item.Sprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.style.width = image.style.height = size;
            image.style.flexShrink = 0;
            return image;
        }
    }
}
