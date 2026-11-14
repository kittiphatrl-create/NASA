// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do pictures (gauges, faces, a peephole view, tactile cards) make survival choices more exciting than numbers?
// Date: 2026-11-12
using System;
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// Visual layer of the walkable base: resource gauges with sticker icons (top left), the cadet portrait
    /// (top right), choice cards that lift on hover and show only up/down hints instead of numbers, and the
    /// full-screen first-person peephole where visitors are inspected (infrared, breath, door microphone, talk).
    /// Rules stay in LifeboatSim; this file only draws its state.
    /// </summary>
    public partial class GameFlow
    {
        static readonly string[] GaugeKinds = { "o2", "water", "food", "power" };
        readonly Dictionary<string, float> _bMax = new Dictionary<string, float>();
        bool _bShowNumbers;

        float StockOf(string kind)
        {
            var s = _life.State.stock;
            return kind == "o2" ? s.o2 : kind == "water" ? s.water : kind == "food" ? s.food : s.power;
        }

        float GaugeMax(string kind)
        {
            float v = StockOf(kind);
            if (!_bMax.TryGetValue(kind, out var m) || v > m) { m = Mathf.Max(v, 6f); _bMax[kind] = m; }
            return m;
        }

        static Color GaugeColor(string kind) =>
            kind == "o2" ? new Color(0.35f, 0.9f, 1f) : kind == "water" ? new Color(0.25f, 0.5f, 0.95f) :
            kind == "food" ? new Color(1f, 0.72f, 0.25f) : new Color(1f, 0.88f, 0.3f);

        static void Round(VisualElement e, float r) { e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r; }

        static VisualElement Abs(float left, float top, float w, float h)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute; e.style.left = left; e.style.top = top; e.style.width = w; e.style.height = h;
            return e;
        }

        VisualElement Sticker(string kind, float size)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.width = size; e.style.height = size; e.style.flexShrink = 0;
            e.style.backgroundImage = new StyleBackground(Art.Sticker(kind));
            return e;
        }

        // ------------------------------------------------------------------ gauges

        /// <summary>
        /// One gauge per resource: oxygen is a gas bottle with bubbles, water a tank with a wavy surface, food a stack
        /// of cans, power a battery with charge segments. Level only; Tab shows the exact numbers.
        /// </summary>
        VisualElement BGaugeRow(float scale = 1f)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.style.flexDirection = FlexDirection.Row;
            foreach (var kind in GaugeKinds)
            {
                float fraction = Mathf.Clamp01(StockOf(kind) / GaugeMax(kind));
                var card = new VisualElement { pickingMode = PickingMode.Ignore };
                card.style.width = 122 * scale; card.style.marginRight = 8; card.style.paddingLeft = card.style.paddingRight = 10;
                card.style.paddingTop = card.style.paddingBottom = 8; card.style.backgroundColor = GameUI.CPanel2;
                GameUI.Border(card, fraction < .25f ? GameUI.CRed : GameUI.CCyanD, 1, 3);
                card.style.borderTopWidth = 3; card.style.borderTopColor = GaugeColor(kind); card.style.borderBottomWidth = 3;
                var head = new VisualElement(); head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center;
                head.Add(Sticker(kind, 25 * scale));
                var title = _ui.L(_loc.T("life.res." + kind), 18, true); title.style.marginLeft = 5; head.Add(title); card.Add(head);
                card.Add(_ui.L(_bShowNumbers ? LbNum(StockOf(kind)) : Mathf.RoundToInt(fraction * 100) + "%", 27, true, fraction < .25f ? GameUI.CRed : GaugeColor(kind)));
                var track = new VisualElement(); track.style.height = 8; track.style.backgroundColor = new Color(.025f,.04f,.08f); Round(track, 4);
                var fill = new VisualElement(); fill.style.height = 8; fill.style.width = Length.Percent(fraction * 100); fill.style.backgroundColor = GaugeColor(kind); Round(fill, 4); track.Add(fill); card.Add(track);
                var status = _ui.L(fraction < .25f ? CrewText("LOW", "ใกล้หมด") : CrewText("RESERVE", "สำรอง"), 14, false, fraction < .25f ? GameUI.CRed : GameUI.CDim);
                card.Add(status); row.Add(card);
            }
            return row;
        }

        /// <summary>0 well, 1 tired or hungry, 2 sick, 3 critical — from health and whether everyone could eat.</summary>
        int BMood()
        {
            var st = _life.State;
            if (st.health <= 1) return 3;
            if (st.health < LC.health) return 2;
            if (st.stock.food < 1 || st.stock.water < 1 || st.stock.o2 < 2) return 1;
            return 0;
        }

        VisualElement BPortraitPanel()
        {
            bool ship = _bMode == 1;
            var p = new VisualElement { pickingMode = PickingMode.Ignore };
            p.style.position = Position.Absolute; p.style.left = 24; p.style.bottom = 24; p.style.width = 480;
            p.Add(GameUI.Plate(GameUI.Paper, GameUI.Ink, 21, true, 5));
            p.style.paddingLeft = p.style.paddingRight = 16; p.style.paddingTop = p.style.paddingBottom = 8;
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
            var identity = new VisualElement { pickingMode = PickingMode.Ignore };
            identity.style.width = 65; identity.style.marginRight = 7; identity.style.alignItems = Align.Center;
            var portrait = new Image { sprite = Art.CrewPortrait(ship ? 0 : BMood()), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            portrait.style.width = 64; portrait.style.height = 64;
            portrait.style.backgroundColor = GameUI.CPanel2;
            identity.Add(portrait);
            identity.Add(_ui.L(TT("Luke", "ลุค"), 17, true, GameUI.Ink));
            row.Add(identity); p.Add(row);
            var meters = new VisualElement { pickingMode = PickingMode.Ignore };
            meters.style.flexGrow = 1;
            row.Add(meters);
            void Need(Sprite icon, string label, float fraction, Color color, string value)
            {
                var line = new VisualElement { pickingMode = PickingMode.Ignore };
                line.style.flexDirection = FlexDirection.Row; line.style.alignItems = Align.Center;
                var glyph = new Image { sprite = icon, pickingMode = PickingMode.Ignore };
                glyph.style.width = glyph.style.height = 22; glyph.style.marginRight = 4;
                line.Add(glyph);
                var caption = _ui.L(label, 16, true, GameUI.Ink); caption.style.width = 82; line.Add(caption);
                var bar = new VisualElement { pickingMode = PickingMode.Ignore };
                bar.style.flexGrow = 1; bar.style.height = 10; bar.style.backgroundColor = GameUI.CPanel2;
                var fill = new VisualElement { pickingMode = PickingMode.Ignore };
                fill.style.width = Length.Percent(Mathf.Clamp01(fraction) * 100); fill.style.height = 10; fill.style.backgroundColor = color;
                bar.Add(fill); line.Add(bar);
                var amount = _ui.L(value, 16, true, GameUI.Ink);
                amount.style.width = 62; amount.style.unityTextAlign = TextAnchor.MiddleRight;
                line.Add(amount);
                for (int i = 1; i < 5; i++)
                {
                    var tick = Abs(0, 0, 2, 10);
                    tick.style.left = Length.Percent(i * 20);
                    tick.style.backgroundColor = GameUI.Ink;
                    bar.Add(tick);
                }
                meters.Add(line);
            }
            if (ship)
            {
                // Chapter 1 has no needs simulation. Keep the same layout without inventing values.
                Need(Art.Sticker("food"), TT("Fullness", "ความอิ่ม"), 0, GameUI.CAmber, "—");
                Need(Art.Sticker("water"), TT("Water", "น้ำ"), 0, GameUI.CCyan, "—");
                Need(Art.Sticker("power"), TT("Energy", "พลังงาน"), 0, GameUI.CGreen, "—");
                meters.tooltip = TT("Needs tracking starts after landing. SHIFT uses the live stamina bar.", "เริ่มติดตามความอิ่ม น้ำ และพลังงานหลังลงจอด · SHIFT ใช้แรงวิ่งจริง");
            }
            else if (RestoreMode && _restore != null)
            {
                Need(Art.Sticker("food"), TT("Fullness", "ความอิ่ม"), _restore.Food / 100f, GameUI.CAmber, $"{_restore.Food:0}%");
                Need(Art.Sticker("water"), TT("Water", "น้ำ"), _restore.Water / 100f, GameUI.CCyan, $"{_restore.Water:0}%");
                Need(Art.Sticker("power"), TT("Energy", "พลังงาน"), _restore.Rest / 100f, GameUI.CGreen, $"{_restore.Rest:0}%");
            }
            else
                Need(Art.ConditionFace(BMood()), TT("Morale", "กำลังใจ"), _life.State.morale / 100f, GameUI.CAmber, $"{_life.State.morale:0}%");
            Need(Art.SprintIcon(), TT("Stamina", "แรงวิ่ง"), _sprint.Value / 100f, GameUI.CCyan, $"{_sprint.Value:0}%");
            Need(Art.RadIcon(), TT("Rad", "รังสี"), RadFrac, _dose < DosePerSick ? new Color(.74f, .58f, .98f) : RadColor(), $"{_dose:0.#}");
            string hudPet = ship ? _sPet : _life.State.pet;
            if (hudPet != null)
            {
                var pet = new VisualElement { pickingMode = PickingMode.Ignore };
                pet.style.alignItems = Align.Center;
                pet.style.marginLeft = 8; pet.style.paddingLeft = 7; pet.style.paddingRight = 3;
                pet.style.borderLeftWidth = 2; pet.style.borderLeftColor = GameUI.Ink;
                var sprite = new Image { sprite = Art.Pet(hudPet), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                sprite.style.width = sprite.style.height = 26; pet.Add(sprite);
                pet.Add(_ui.L(ship ? _loc.T($"pet.{hudPet}.name") : LbName(LifeboatSim.Pet), 15, true, GameUI.Ink));
                row.Add(pet);
            }
            return p;
        }

        // ------------------------------------------------------------------ hints instead of numbers

        /// <summary>Sticker icons with an up or down arrow for each stock a choice changes. No amounts.</summary>
        VisualElement BDeltaIcons(Stock d, bool perDay = false, int health = 0, bool choice = false)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.flexWrap = Wrap.Wrap;
            void One(string kind, float v)
            {
                if (Mathf.Approximately(v, 0)) return;
                var chip = new VisualElement { pickingMode = PickingMode.Ignore };
                chip.style.flexDirection = FlexDirection.Row; chip.style.alignItems = Align.Center; chip.style.marginRight = 8;
                chip.Add(Sticker(kind, 30));
                string arrow = v > 0 ? (Mathf.Abs(v) >= 2 ? "▲▲" : "▲") : (Mathf.Abs(v) >= 2 ? "▼▼" : "▼");
                chip.Add(_ui.L(arrow, 18, true, v > 0 ? GameUI.CGreen : GameUI.CRed));
                row.Add(chip);
            }
            One("o2", d.o2); One("water", d.water); One("food", d.food); One("power", d.power);
            if (health != 0)
            {
                var h = new VisualElement { pickingMode = PickingMode.Ignore }; h.style.width = 24; h.style.height = 22;
                h.style.backgroundImage = new StyleBackground(Art.Heart()); row.Add(h);
                row.Add(_ui.L(health > 0 ? "▲" : "▼", 18, true, health > 0 ? GameUI.CGreen : GameUI.CRed));
            }
            if (perDay) row.Add(_ui.L(_loc.T("base.card.every_day"), 16, true, choice ? GameUI.Paper : GameUI.CAmber));
            if (row.childCount == 0) row.Add(_ui.L(_loc.T("base.card.free"), 17, false, GameUI.CDim));
            if (choice)
            {
                row.style.alignSelf = Align.FlexStart;
                row.style.backgroundColor = GameUI.CPanel;
                row.style.paddingLeft = row.style.paddingRight = 8;
            }
            return row;
        }

        /// <summary>
        /// A choice card: lifts and grows slightly on hover or keyboard focus (about 150 ms), glows, and plays one
        /// hover sound; a click locks all cards in its group so the choice is sent once.
        /// </summary>
        VisualElement BCard(string title, VisualElement hints, Action pick, bool enabled, string whyKey, List<VisualElement> group, float width = 300, Color? accent = null, int titleSize = 23, Sprite icon = null, bool cut = false, bool paper = false)
        {
            var c = new VisualElement();
            var edge = accent ?? GameUI.CCyanD;
            c.style.width = width; c.style.minHeight = 150; c.style.flexShrink = 0;
            c.style.marginLeft = c.style.marginRight = 8;
            c.style.paddingLeft = c.style.paddingRight = 14; c.style.paddingTop = 12; c.style.paddingBottom = 12;
            c.style.backgroundColor = enabled ? GameUI.CPanel2 : GameUI.CPanel;
            VisualElement idle = null, selected = null;
            if (cut)
            {
                c.style.backgroundColor = Color.clear;
                c.style.paddingLeft = c.style.paddingRight = 24;
                idle = GameUI.Plate(paper ? GameUI.Paper : GameUI.CPanel2, enabled ? edge : GameUI.CDim, 22, true, 5);
                selected = GameUI.Plate(GameUI.CAmber, GameUI.Ink, 22, true, 5);
                selected.style.display = DisplayStyle.None;
                c.Add(idle); c.Add(selected);
                if (!enabled) c.style.opacity = .75f;
                if (paper && hints is Label hint) hint.style.color = GameUI.Ink;
            }
            else
            {
                GameUI.Border(c, enabled ? edge : GameUI.CCyanD, 3, 3);
                c.style.borderLeftWidth = 7;
            }
            c.style.transitionProperty = new List<StylePropertyName> { "scale", "translate", "border-top-color", "background-color" };
            c.style.transitionDuration = new List<TimeValue> { new TimeValue(150, TimeUnit.Millisecond) };
            c.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseOut) };
            var t = _ui.L(title, titleSize, true, paper ? GameUI.Ink : enabled ? GameUI.CText : GameUI.CDim);
            t.style.marginBottom = 8; t.style.whiteSpace = WhiteSpace.Normal;
            var heading = new VisualElement { pickingMode = PickingMode.Ignore };
            heading.style.flexDirection = paper ? FlexDirection.Column : FlexDirection.Row;
            if (paper) c.style.justifyContent = Justify.Center;
            heading.style.alignItems = Align.Center;
            if (icon != null)
            {
                var picture = new Image { sprite = icon, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                picture.style.width = picture.style.height = paper ? 56 : 36;
                picture.style.marginRight = 8; picture.style.flexShrink = 0;
                heading.Add(picture);
            }
            t.style.flexShrink = 1;
            heading.Add(t);
            c.Add(heading);
            if (hints != null)
            {
                if (paper) hints.style.alignSelf = Align.Center;
                c.Add(hints);
            }
            if (!enabled && whyKey != null) { var w = _ui.L(_loc.T(whyKey), 17, false, GameUI.CRed); w.style.marginTop = 6; c.Add(w); }
            group?.Add(c);
            if (!enabled) return c;
            c.focusable = true;
            bool locked = false;
            bool hovering = false, focused = false;
            void Paint()
            {
                if (locked) return;
                bool on = hovering || focused;
                if (cut)
                {
                    idle.style.display = on ? DisplayStyle.None : DisplayStyle.Flex;
                    selected.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                    t.text = GameUI.Thai(on && !paper ? "▸ " + title : title);
                    t.style.color = on || paper ? GameUI.Ink : GameUI.CText;
                    return;
                }
                c.style.backgroundColor = on ? new Color(.24f, .16f, .08f) : GameUI.CPanel2;
                GameUI.Border(c, on ? GameUI.CAmber : edge, 3, 3);
                c.style.borderLeftWidth = 7;
                t.text = GameUI.Thai(on ? "▸ " + title : title);
                t.style.color = on ? GameUI.CAmber : GameUI.CText;
            }
            void Pick()
            {
                if (locked) return;
                if (group != null) foreach (var g in group) g.SetEnabled(false);
                locked = true;
                if (!Settings.ReducedMotion) c.style.scale = new Scale(new Vector3(0.97f, 0.97f, 1));
                _sfx.Play("click");
                c.schedule.Execute(() => pick?.Invoke()).StartingIn(120);
            }
            c.RegisterCallback<PointerEnterEvent>(_ => { hovering = true; Paint(); if (!locked) _sfx.Play("hover", .7f); });
            c.RegisterCallback<PointerLeaveEvent>(_ => { hovering = false; Paint(); });
            c.RegisterCallback<FocusInEvent>(_ => { focused = true; Paint(); });
            c.RegisterCallback<FocusOutEvent>(_ => { focused = false; Paint(); });
            c.RegisterCallback<ClickEvent>(_ => Pick());
            c.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.Space) Pick(); });
            return c;
        }

        Stock BOptionHint(LifeOption o, out bool perDay)
        {
            perDay = false;
            if (!string.IsNullOrEmpty(o.admits) && LC.GetVisitor(o.admits) != null)
            {
                var v = LC.GetVisitor(o.admits);
                perDay = true;
                return new Stock { o2 = -v.perDay.o2, water = -v.perDay.water, food = -v.perDay.food };
            }
            return o.delta;
        }

        /// <summary>The day card's choices as a row of cards (non-visitor cards in the base).</summary>
        void BBuildOptionCards(VisualElement box, LifeCard card)
        {
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap;
            row.style.justifyContent = Justify.Center; row.style.marginTop = 14; row.style.flexShrink = 0;
            var group = new List<VisualElement>();
            foreach (var o in card.options)
            {
                var opt = o;
                bool can = _life.CanChoose(opt, out var why);
                var d = BOptionHint(opt, out bool perDay);
                row.Add(BCard(_loc.T($"life.card.{card.id}.{opt.id}"), BDeltaIcons(d, perDay, opt.health), () => LbChoose(opt), can, can ? null : why, group,
                    card.options.Count >= 4 ? 250 : 320));
            }
            box.Add(row);
        }

        // ------------------------------------------------------------------ first-person peephole

        string _peepFx;          // scan to animate on the next draw
        int _peepTalk;           // how many questions were asked of this visitor
        string _peepCard;

        Sprite _peepScenery;

        Sprite PeepholeScenery()
        {
            if (_peepScenery != null) return _peepScenery;
            var ridge = Art.MarsRidge(1).texture;
            var ground = Art.MarsGround().texture;
            const int size = 256;
            var canvas = new PixelCanvas(size, size);
            var sky = PixelCanvas.Hex("#d28b61");
            var haze = PixelCanvas.Hex("#efb48a");
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var point = new Vector2((x + .5f) / size * 2 - 1, (y + .5f) / size * 2 - 1);
                var uv = (point * (1 + .22f * point.sqrMagnitude) + Vector2.one) * .5f;
                float horizon = .42f;
                Color32 color = Color32.Lerp(haze, sky, Mathf.InverseLerp(horizon, .9f, uv.y));
                if (uv.y < horizon)
                    color = ground.GetPixel(Mathf.Clamp((int)(uv.x * ground.width), 0, ground.width - 1), Mathf.Clamp((int)(uv.y / horizon * ground.height), 0, ground.height - 1));
                else if (uv.y < horizon + .17f)
                {
                    var rock = ridge.GetPixel(Mathf.Clamp((int)(uv.x * ridge.width), 0, ridge.width - 1), (int)((uv.y - horizon) / .17f * (ridge.height - 1)));
                    color = Color32.Lerp(color, rock, rock.a);
                }
                canvas.Set(x, y, color);
            }
            _peepScenery = canvas.ToSprite();
            return _peepScenery;
        }

        /// <summary>
        /// Full-screen view through the airlock peephole: the visitor right outside at eye level, Mars behind, a dark
        /// lens ring. Left: what you found. Below the lens: ways to check (talk, infrared, breath, microphone).
        /// Right: what to do. The visitor's waiting clock is paused while this view is open.
        /// </summary>
        void BShowPeep()
        {
            var card = _lbCard;
            if (_peepCard != card.id) { _peepCard = card.id; _peepTalk = 0; }
            string visitor = card.visitor;
            _ui.ShowScreen(screen =>
            {
                var bg = GameUI.Backdrop(Color.black);
                screen.Add(bg);
                var root = new VisualElement { pickingMode = PickingMode.Ignore };
                root.style.width = 1920;
                root.style.height = 1080;
                root.style.flexShrink = 0;
                screen.Add(root);
                void Fit(float width, float height)
                {
                    float k = Mathf.Min(width / 1920f, height / 1080f);
                    root.style.scale = new Scale(new Vector3(k, k, 1));
                }
                if (screen.layout.width > 0 && screen.layout.height > 0) Fit(screen.layout.width, screen.layout.height);
                EventCallback<GeometryChangedEvent> resize = e => Fit(e.newRect.width, e.newRect.height);
                screen.RegisterCallback(resize);
                root.RegisterCallback<DetachFromPanelEvent>(_ => screen.UnregisterCallback(resize));

                // ---- the lens
                const float L = 660, LX = 630, LY = 40;
                var lens = Abs(LX, LY, L, L); lens.style.overflow = Overflow.Hidden; Round(lens, L / 2);
                lens.style.backgroundImage = new StyleBackground(PeepholeScenery());
                var sprite = Art.Visitor(visitor);
                float scale = L * 0.92f / sprite.rect.height;
                float pw = sprite.rect.width * scale, ph = sprite.rect.height * scale;
                var who = Abs((L - pw) / 2f, L * 0.2f, pw, ph);
                who.style.backgroundImage = new StyleBackground(sprite);
                lens.Add(who);
                bool thermalDone = false, thermalOdd = false;
                foreach (var sc in _life.ScansDone(card)) if (sc == "thermal") { thermalDone = true; thermalOdd = _life.IsOdd(visitor, sc); }
                var heat = Abs((L - pw) / 2f, L * 0.2f, pw, ph);
                heat.style.backgroundImage = new StyleBackground(sprite);
                heat.style.unityBackgroundImageTintColor = thermalOdd ? new Color(0.3f, 0.5f, 1f) : new Color(1f, 0.45f, 0.1f);
                heat.style.opacity = thermalDone ? 0.55f : 0f;
                lens.Add(heat);
                var mood = VisitorMood(visitor);
                var glass = Abs(0, 0, L, L); glass.style.backgroundColor = new Color(0.6f, 0.8f, 1f, 0.05f); lens.Add(glass);
                var sweep = Abs(0, -20, L, 16); sweep.style.backgroundColor = new Color(1f, 0.15f, 0.12f, 0.85f); sweep.style.display = DisplayStyle.None; lens.Add(sweep);
                var sweepGlow = Abs(0, -40, L, 50); sweepGlow.style.backgroundColor = new Color(1f, 0.2f, 0.1f, 0.18f); sweepGlow.style.display = DisplayStyle.None; lens.Add(sweepGlow);
                root.Add(lens);
                var mask = Abs(0, 0, L, L); mask.style.backgroundImage = new StyleBackground(Art.LensMask()); lens.Add(mask);
                var rim = Abs(LX - 29, LY - 29, L + 58, L + 58);
                GameUI.Border(rim, new Color(.28f, .22f, .17f), 21, (L + 58) / 2);
                root.Add(rim);
                var gasket = Abs(LX - 9, LY - 9, L + 18, L + 18);
                GameUI.Border(gasket, GameUI.Ink, 13, (L + 18) / 2);
                root.Add(gasket);
                const float centerX = LX + L / 2, centerY = LY + L / 2;
                for (int i = 0; i < 4; i++)
                {
                    float angle = (45 + i * 90) * Mathf.Deg2Rad;
                    var lamp = Abs(centerX + Mathf.Cos(angle) * 347 - 37, centerY + Mathf.Sin(angle) * 347 - 6, 74, 12);
                    lamp.style.rotate = new Rotate(new Angle(135 + i * 90));
                    lamp.style.backgroundColor = GameUI.CAmber;
                    GameUI.Border(lamp, GameUI.Ink, 2, 4);
                    root.Add(lamp);
                }

                // breathing and a little sway
                float t0 = Time.realtimeSinceStartup;
                who.schedule.Execute(() =>
                {
                    float t = Time.realtimeSinceStartup - t0;
                    var animated = Art.Crew(visitor, 0, t % 4 > 3.8f ? -2 : -1);
                    who.style.backgroundImage = new StyleBackground(animated);
                    heat.style.backgroundImage = new StyleBackground(animated);
                    if (Settings.ReducedMotion) return;
                    float b = 1 + Mathf.Sin(t * 2.2f) * 0.012f;
                    float shiver = mood.kind == "cold" || mood.kind == "nervous" ? Mathf.Sin(t * 40f) * 2.5f : 0f;
                    var tr = new Translate(Mathf.Sin(t * 0.7f) * 6f + shiver, (1 - b) * ph * 0.5f);
                    who.style.scale = new Scale(new Vector3(1, b, 1)); who.style.translate = tr;
                    heat.style.scale = new Scale(new Vector3(1, b, 1)); heat.style.translate = tr;
                }).Every(40);

                // ---- left: a short ID card (icons and words, not paragraphs)
                var logFrame = Abs(34, 100, 500, 880);
                logFrame.Add(GameUI.Plate(GameUI.Paper, GameUI.Ink, 29, true, 7));
                logFrame.style.paddingLeft = 40; logFrame.style.paddingRight = 35;
                logFrame.style.paddingTop = 20; logFrame.style.paddingBottom = 28;
                var log = new ScrollView();
                log.style.flexGrow = 1;
                log.style.paddingLeft = log.style.paddingRight = 0; log.style.paddingTop = 0;
                log.Add(_ui.PrintedLabel(_loc.T("base.peep.header"), 21, GameUI.CAmber, GameUI.Ink));
                var title = _ui.L(_loc.T($"life.card.{card.id}.title"), 36, true, GameUI.Ink); title.style.marginTop = 6; log.Add(title);
                // what you can see through the glass: their mood, in big words
                var moodChip = new VisualElement(); moodChip.style.marginTop = 10; moodChip.style.paddingLeft = moodChip.style.paddingRight = 14; moodChip.style.paddingTop = moodChip.style.paddingBottom = 8;
                moodChip.style.backgroundColor = new Color(mood.color.r * 0.22f, mood.color.g * 0.22f, mood.color.b * 0.22f, 0.95f); GameUI.Border(moodChip, mood.color, 2, 10);
                moodChip.Add(_ui.L(TT("LOOKS", "ท่าทาง"), 17, true, GameUI.CDim));
                moodChip.Add(_ui.L(mood.text, 26, true, mood.color));
                log.Add(moodChip);
                BPeepIdCard(log, card);
                // scanner results as two big tiles
                var tiles = new VisualElement(); tiles.style.flexDirection = FlexDirection.Row; tiles.style.marginTop = 12;
                foreach (var sc in LC.scans)
                {
                    bool done = System.Linq.Enumerable.Contains(_life.ScansDone(card), sc);
                    bool odd = done && _life.IsOdd(visitor, sc);
                    var t = new VisualElement(); t.style.flexGrow = 1; t.style.flexBasis = 0; t.style.marginRight = 10; t.style.paddingLeft = t.style.paddingRight = 10; t.style.paddingTop = t.style.paddingBottom = 8;
                    t.style.backgroundColor = !done ? new Color(0.1f, 0.11f, 0.16f) : odd ? new Color(0.3f, 0.04f, 0.06f) : new Color(0.04f, 0.22f, 0.12f);
                    GameUI.Border(t, !done ? new Color(0.3f, 0.32f, 0.4f) : odd ? GameUI.CRed : GameUI.CGreen, 2, 10);
                    t.Add(_ui.L(_loc.T($"base.peep.scan.{sc}"), 22, true, GameUI.CText));
                    t.Add(_ui.L(!done ? TT("Not scanned", "ยังไม่สแกน") : odd ? TT("Anomaly", "ผิดปกติ") : TT("Normal", "ปกติ"), 25, true, !done ? GameUI.CDim : odd ? GameUI.CRed : GameUI.CGreen));
                    tiles.Add(t);
                }
                log.Add(tiles);
                // what they said, one short line each
                int said = Mathf.Min(_peepTalk, card.clues);
                if (_peepTalk > 0)
                {
                    var hd = _ui.L(TT("What you heard", "สิ่งที่ได้ยิน"), 22, true, GameUI.Ink); hd.style.marginTop = 12; log.Add(hd);
                    for (int i = 1; i <= said; i++) { var q = _ui.L("• " + _loc.T($"life.card.{card.id}.clue{i}"), 20, false, GameUI.Ink); q.style.marginTop = 3; log.Add(q); }
                    var chat = PeepChat.TryGetValue(visitor, out var cl) ? cl : null;
                    int extra = chat == null ? 0 : Mathf.Min(_peepTalk - said, chat.Length / 2);
                    for (int i = 0; i < extra; i++) { var q = _ui.L("• " + TL(chat[i * 2 + 1]), 20, false, GameUI.Ink); q.style.marginTop = 3; log.Add(q); }
                }
                if (_life.State.pet != null)
                {
                    string key = _life.HintKey(card);
                    var pet = _ui.L(key == null ? _loc.T("life.hint_hungry", LbName(LifeboatSim.Pet)) : LbName(LifeboatSim.Pet) + ": " + _loc.T(key), 20, true, GameUI.Ink);
                    pet.style.marginTop = 12; log.Add(pet);
                }
                var fic = _ui.L(_loc.T("life.fiction"), 16, false, GameUI.Ink);
                fic.style.marginTop = 14; fic.style.marginBottom = 12; log.Add(fic);
                logFrame.Add(log);
                root.Add(logFrame);

                // ---- below the lens: talk as much as you like, scan as often as you like
                var checks = new VisualElement(); checks.style.position = Position.Absolute; checks.style.left = 548; checks.style.width = 840; checks.style.top = LY + L + 46;
                checks.style.flexDirection = FlexDirection.Row; checks.style.justifyContent = Justify.Center;
                checks.Add(BCard(_loc.T("base.peep.talk"), _ui.L(TT("Ask a question", "ถามคำถาม"), 16, false, GameUI.CDim),
                    () => { _peepTalk++; _sfx.Play("blip"); BPeepTalkVN(card); }, true, null, null, 240, GameUI.CGreen, 22, Art.TalkBubble(), cut: true, paper: true));
                foreach (var sc in LC.scans)
                {
                    string id = sc;
                    bool done = System.Linq.Enumerable.Contains(_life.ScansDone(card), id);
                    var hint = _ui.L(done ? TT("Scan again", "สแกนซ้ำได้") : TT("Free, no limit", "สแกนได้ไม่จำกัด"), 16, false, GameUI.CDim);
                    checks.Add(BCard(_loc.T($"base.peep.scan.{id}"), hint, () =>
                    {
                        if (done) { _peepFx = id; _sfx.Play(id == "thermal" ? "whoosh" : "blip"); BShowPeep(); }
                        else BRunScan(id, who, heat, sweep, sweepGlow, ph);
                    }, done || _life.CanScan(card, id), null, null, 240, GameUI.CAmber, 22,
                        id == "thermal" ? Art.JobItem("data_chip") : Art.Sticker("o2"), cut: true, paper: true));
                }
                root.Add(checks);

                // ---- right: what to do
                var acts = new ScrollView(); acts.style.position = Position.Absolute; acts.style.right = 30; acts.style.top = 100; acts.style.bottom = 40; acts.style.width = 480;
                acts.Add(_ui.PrintedLabel(_loc.T("base.peep.decide"), 26, GameUI.Paper, GameUI.Ink));
                if (IsTrader(visitor))
                {
                    var tg = new List<VisualElement>();
                    var tc = BCard(_loc.T("base.trade.open"), _ui.L(_loc.T("base.trade.open_hint"), 16, false, GameUI.Ink), BShowTrade, true, null, tg, 460, GameUI.CAmber, icon: Art.Sticker("power"), cut: true, paper: true);
                    tc.style.minHeight = 80; tc.style.marginTop = 10; tc.style.marginLeft = 0; tc.style.width = 460;
                    acts.Add(tc);
                }
                var dgroup = new List<VisualElement>();
                foreach (var o in card.options)
                {
                    var opt = o;
                    bool can = _life.CanChoose(opt, out var why);
                    var d = BOptionHint(opt, out bool perDay);
                    bool admits = !string.IsNullOrEmpty(opt.admits);
                    var choiceIcon = admits || opt.id == "refuse" ? Art.Door(admits, true) : Art.TalkBubble();
                    var cc = BCard(_loc.T($"life.card.{card.id}.{opt.id}"), BDeltaIcons(d, perDay, opt.health, choice: true), () => { _peepCard = null; LbChoose(opt); }, can, can ? null : why, dgroup, 460,
                        opt.id == "refuse" ? GameUI.CRed : GameUI.Paper, 25, choiceIcon, cut: true);
                    cc.style.width = 460; cc.style.minHeight = 110; cc.style.marginTop = 10; cc.style.marginLeft = 0;
                    acts.Add(cc);
                }
                var back = _ui.B(_loc.T("base.peep.back"), () => { _bOpen = false; _ui.HideScreen(); }, 22);
                back.style.marginTop = 18; back.style.alignSelf = Align.FlexStart; back.style.marginLeft = 0;
                _ui.CutButton(back, size: 22);
                acts.Add(back);
                root.Add(acts);

                // play the scan that was just chosen
                if (_peepFx != null) { string fx = _peepFx; _peepFx = null; BScanFx(fx, root, who, heat, sweep, sweepGlow, ph, thermalOdd); }
            });
        }

        /// <summary>
        /// ORBI's best guess from its (fictional) visitor database once a scanner has a reading: species, sex and
        /// temperament, each marked as an estimate. Unknown shapes come back as "not in the database".
        /// </summary>
        void BDossier(VisualElement log, string visitor)
        {
            var box = new VisualElement();
            box.style.marginTop = 10; box.style.paddingLeft = box.style.paddingRight = 10; box.style.paddingTop = 6; box.style.paddingBottom = 8;
            bool known = _loc.Has($"base.dossier.{visitor}.species");
            box.style.backgroundColor = known ? new Color(0.04f, 0.14f, 0.16f, 0.95f) : new Color(0.2f, 0.04f, 0.06f, 0.95f);
            GameUI.Border(box, known ? GameUI.CCyan : GameUI.CRed, 2, 8);
            box.Add(_ui.L(_loc.T("base.dossier.header"), 18, true, known ? GameUI.CCyan : GameUI.CRed));
            if (!known) box.Add(_ui.L(_loc.T("base.dossier.unknown"), 19, true, GameUI.CRed));
            else
            {
                box.Add(_ui.L(_loc.T("base.dossier.species") + "  " + _loc.T($"base.dossier.{visitor}.species"), 19));
                box.Add(_ui.L(_loc.T("base.dossier.sex") + "  " + _loc.T($"base.dossier.{visitor}.sex"), 19));
                box.Add(_ui.L(_loc.T("base.dossier.temper") + "  " + _loc.T($"base.dossier.{visitor}.temper"), 19));
                box.Add(_ui.L(_loc.T($"base.dossier.{visitor}.conf"), 16, false, GameUI.CDim));
            }
            log.Add(box);
        }

        /// <summary>What a visitor looks like through the glass: a mood word and its bubble (fiction; follows their data).</summary>
        (string kind, string text, Color color) VisitorMood(string visitor)
        {
            var v = LC.GetVisitor(visitor);
            if (v != null && v.monster) return ("creepy", TT("Smiles too long. Never blinks.", "ยิ้มค้างนานเกินไป ไม่กะพริบตาเลย"), GameUI.CRed);
            if (IsTrader(visitor)) return ("happy", TT("Cheerful, wants to trade", "ยิ้มแย้ม อยากค้าขาย"), GameUI.CGreen);
            if (v != null && v.steals) return ("nervous", TT("Nervous, looks away a lot", "ลุกลี้ลุกลน หลบตาบ่อย"), GameUI.CAmber);
            if (v != null && !v.giftPerDay.IsZero) return ("happy", TT("Friendly smile, waves at you", "ยิ้มเป็นมิตร โบกมือให้"), GameUI.CGreen);
            if (v != null && v.leavesAfterNight) return ("tired", TT("Exhausted, asks for one night", "เหนื่อยมาก ขอพักแค่คืนเดียว"), GameUI.CCyan);
            return ("cold", TT("Shivering and scared", "หนาวสั่น ดูกลัว ๆ"), GameUI.CCyan);
        }

        /// <summary>Species, sex and temper as three short chips (from ORBI's database once a scan has a reading).</summary>
        void BPeepIdCard(VisualElement log, LifeCard card)
        {
            string v = card.visitor;
            bool scanned = _life.ScansDone(card).Count > 0;
            bool known = _loc.Has($"base.dossier.{v}.species");
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap; row.style.marginTop = 8;
            void Chip(string label, string value, Color c)
            {
                var e = new VisualElement(); e.style.marginRight = 8; e.style.marginTop = 6; e.style.paddingLeft = e.style.paddingRight = 10; e.style.paddingTop = e.style.paddingBottom = 4;
                e.style.maxWidth = 450;
                e.style.backgroundColor = new Color(c.r * 0.25f, c.g * 0.25f, c.b * 0.25f, 0.95f); GameUI.Border(e, c, 2, 8);
                e.Add(_ui.L(label, 17, true, GameUI.CDim));
                e.Add(_ui.L(value, 23, true, c));
                row.Add(e);
            }
            if (!scanned)
            {
                Chip(TT("WHO", "เป็นใคร"), "?", GameUI.CDim); Chip(TT("SEX", "เพศ"), "?", GameUI.CDim); Chip(TT("TEMPER", "นิสัย"), "?", GameUI.CDim);
                log.Add(row);
                log.Add(_ui.L(TT("Scan to look them up in ORBI's database.", "สแกนเพื่อค้นข้อมูลจากฐานข้อมูลของ ORBI"), 16, false, GameUI.Ink));
                return;
            }
            if (!known) { Chip(TT("WHO", "เป็นใคร"), TT("not in database!", "ไม่มีในฐานข้อมูล!"), GameUI.CRed); log.Add(row); return; }
            Chip(TT("WHO", "เป็นใคร"), _loc.T($"base.dossier.{v}.species"), GameUI.CCyan);
            Chip(TT("SEX", "เพศ"), _loc.T($"base.dossier.{v}.sex"), GameUI.CCyan);
            Chip(TT("TEMPER", "นิสัย"), _loc.T($"base.dossier.{v}.temper"), GameUI.CAmber);
            log.Add(row);
            log.Add(_ui.L(_loc.T($"base.dossier.{v}.conf"), 16, false, GameUI.Ink));
        }

        /// <summary>The question and the answer, as a visual-novel exchange through the door; then back to the peephole.</summary>
        void BPeepTalkVN(LifeCard card)
        {
            int i = Mathf.Clamp(_peepTalk, 1, Mathf.Max(1, card.clues));
            var lines = new List<(string, string)> { (null, _loc.T("base.peep.q" + i)), (card.visitor, card.clues > 0 ? _loc.T($"life.card.{card.id}.clue{i}") : "...") };
            var chat = PeepChat.TryGetValue(card.visitor, out var cl) ? cl : null;
            int extra = _peepTalk - card.clues;
            if (extra > 0 && chat != null)
            {
                int k = (extra - 1) % (chat.Length / 2);
                lines = new List<(string, string)> { (null, TL(chat[k * 2])), (card.visitor, TL(chat[k * 2 + 1])) };
            }
            var replies = new List<(string, System.Action)> { (_loc.T("base.peep.back_to_lens"), () => BShowPeep()) };
            BShowVN(card.visitor, lines, replies);
        }

        void BRunScan(string id, VisualElement who, VisualElement heat, VisualElement sweep, VisualElement glow, float ph)
        {
            // the scan is spent in the simulation once; the redraw plays its effect
            var odd = _life.Scan(_lbCard.id, id);
            if (odd == null) return;
            _peepFx = id;
            _sfx.Play(id == "thermal" ? "whoosh" : "blip");
            BShowPeep();
        }

        void BScanFx(string id, VisualElement root, VisualElement who, VisualElement heat, VisualElement sweep, VisualElement glow, float ph, bool thermalOdd)
        {
            float t0 = Time.realtimeSinceStartup;
            if (id == "thermal")
            {
                sweep.style.display = DisplayStyle.Flex; glow.style.display = DisplayStyle.Flex;
                heat.style.opacity = 0;
                sweep.schedule.Execute(() =>
                {
                    float k = Mathf.Clamp01((Time.realtimeSinceStartup - t0) / 1.3f);
                    sweep.style.top = -20 + k * 700; glow.style.top = -40 + k * 700;
                    heat.style.opacity = k * 0.55f;
                    if (k >= 1) { sweep.style.display = DisplayStyle.None; glow.style.display = DisplayStyle.None; }
                }).Every(16).Until(() => Time.realtimeSinceStartup - t0 > 1.35f);
                return;
            }
            // breath and heartbeat: a pulse line under the lens that beats a few times
            var line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.style.position = Position.Absolute; line.style.left = 790; line.style.top = 600; line.style.width = 340; line.style.height = 70;
            line.style.flexDirection = FlexDirection.Row; line.style.alignItems = Align.Center;
            var bars = new List<VisualElement>();
            for (int i = 0; i < 24; i++)
            {
                var b = new VisualElement { pickingMode = PickingMode.Ignore };
                b.style.width = 10; b.style.height = 4; b.style.marginRight = 4; b.style.backgroundColor = id == "breath" ? GameUI.CCyan : GameUI.CRed;
                line.Add(b); bars.Add(b);
            }
            root.Add(line);
            int beats = 0;
            line.schedule.Execute(() =>
            {
                float t = Time.realtimeSinceStartup - t0;
                for (int i = 0; i < bars.Count; i++)
                {
                    float x = t * 3f - i * 0.18f;
                    float v = id == "breath" ? (Mathf.Sin(x * 1.3f) * 0.5f + 0.5f) * 40 : Mathf.Max(0, Mathf.Sin(x * 4f)) * Mathf.Max(0, Mathf.Sin(x * 4f)) * 60;
                    bars[i].style.height = 4 + v;
                }
                if (id == "heartbeat" && (int)(t / 0.6f) > beats) { beats = (int)(t / 0.6f); _sfx.Play("thud", 0.35f); }
            }).Every(30).Until(() => Time.realtimeSinceStartup - t0 > 2.4f);
            line.schedule.Execute(() => line.RemoveFromHierarchy()).StartingIn(2600);
        }
    }
}
