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
            var p = new VisualElement(); p.style.position = Position.Absolute; p.style.right = 24; p.style.top = 20; p.style.width = 206;
            p.style.backgroundColor = GameUI.CPanel; GameUI.Border(p, GameUI.CCyanD, 1, 4);
            p.style.paddingLeft = p.style.paddingRight = 12; p.style.paddingTop = p.style.paddingBottom = 10;
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
            var portrait = new Image { sprite = Art.CrewPortrait(BMood()), scaleMode = ScaleMode.ScaleToFit }; portrait.style.width = 94; portrait.style.height = 94; row.Add(portrait);
            var face = new Image { sprite = Art.ConditionFace(BMood()), scaleMode = ScaleMode.ScaleToFit }; face.style.width = 55; face.style.height = 55;
            face.tooltip = CrewText("Physical condition", "สภาพร่างกาย"); row.Add(face); p.Add(row);
            void Need(string icon, float fraction, Color color, string tooltip)
            {
                var line = new VisualElement(); line.style.flexDirection = FlexDirection.Row; line.style.alignItems = Align.Center; line.style.marginTop = 8; line.tooltip = tooltip;
                var glyph = new Image { sprite = icon == "☺" ? Art.ConditionFace(_life.State.morale >= 65 ? 0 : _life.State.morale >= 30 ? 1 : 2) : Art.SprintIcon() }; glyph.style.width = 24; glyph.style.height = 24; glyph.style.marginRight = 6; line.Add(glyph);
                var bar = new VisualElement(); bar.style.width = 112; bar.style.height = 9; bar.style.backgroundColor = GameUI.CPanel2; Round(bar, 3);
                var fill = new VisualElement(); fill.style.width = Length.Percent(Mathf.Clamp01(fraction) * 100); fill.style.height = 9; fill.style.backgroundColor = color; Round(fill, 3); bar.Add(fill); line.Add(bar);
                if (_bShowNumbers) line.Add(_ui.L(Mathf.RoundToInt(fraction * 100).ToString(), 16)); p.Add(line);
            }
            Need("☺", _life.State.morale / 100f, GameUI.CAmber, CrewText("Morale", "กำลังใจ"));
            Need(">>", _sprint.Value / 100f, _sprint.Exhausted ? GameUI.CAmber : GameUI.CCyan, CrewText("Stamina · hold Shift to run; walk to recover", "แรงกาย · กด Shift วิ่ง เดินเพื่อฟื้นแรง"));
            if (_life.State.pet != null)
            {
                var pet = new VisualElement(); pet.style.flexDirection = FlexDirection.Row; pet.style.alignItems = Align.Center; pet.style.marginTop = 9;
                var sprite = new Image { sprite = Art.Pet(_life.State.pet), scaleMode = ScaleMode.ScaleToFit }; sprite.style.width = 42; sprite.style.height = 42; pet.Add(sprite);
                pet.Add(_ui.L(LbName(LifeboatSim.Pet), 18, false, _life.State.petFed ? GameUI.CDim : GameUI.CAmber)); p.Add(pet);
            }
            return p;
        }

        // ------------------------------------------------------------------ hints instead of numbers

        /// <summary>Sticker icons with an up or down arrow for each stock a choice changes. No amounts.</summary>
        VisualElement BDeltaIcons(Stock d, bool perDay = false, int health = 0)
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
            if (perDay) row.Add(_ui.L(_loc.T("base.card.every_day"), 16, true, GameUI.CAmber));
            if (row.childCount == 0) row.Add(_ui.L(_loc.T("base.card.free"), 17, false, GameUI.CDim));
            return row;
        }

        /// <summary>
        /// A choice card: lifts and grows slightly on hover or keyboard focus (about 150 ms), glows, and plays one
        /// hover sound; a click locks all cards in its group so the choice is sent once.
        /// </summary>
        VisualElement BCard(string title, VisualElement hints, Action pick, bool enabled, string whyKey, List<VisualElement> group, float width = 300, Color? accent = null, int titleSize = 23)
        {
            var c = new VisualElement();
            var edge = accent ?? GameUI.CCyanD;
            c.style.width = width; c.style.minHeight = 150; c.style.flexShrink = 0;
            c.style.marginLeft = c.style.marginRight = 8;
            c.style.paddingLeft = c.style.paddingRight = 14; c.style.paddingTop = 12; c.style.paddingBottom = 12;
            c.style.backgroundColor = enabled ? new Color(0.08f, 0.12f, 0.24f, 0.97f) : new Color(0.08f, 0.08f, 0.1f, 0.9f);
            GameUI.Border(c, enabled ? edge : new Color(0.3f, 0.3f, 0.35f), 3, 12);
            c.style.transitionProperty = new List<StylePropertyName> { "scale", "translate", "border-top-color", "background-color" };
            c.style.transitionDuration = new List<TimeValue> { new TimeValue(150, TimeUnit.Millisecond) };
            c.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseOut) };
            var t = _ui.L(title, titleSize, true, enabled ? GameUI.CText : GameUI.CDim);
            t.style.marginBottom = 8; t.style.whiteSpace = WhiteSpace.Normal;
            c.Add(t);
            if (hints != null) c.Add(hints);
            if (!enabled && whyKey != null) { var w = _ui.L(_loc.T(whyKey), 17, false, GameUI.CRed); w.style.marginTop = 6; c.Add(w); }
            group?.Add(c);
            if (!enabled) return c;
            c.focusable = true;
            bool locked = false;
            void Lift(bool on)
            {
                if (Settings.ReducedMotion) { GameUI.Border(c, on ? GameUI.CAmber : edge, 2, 12); return; }
                if (locked) return;
                c.style.scale = new Scale(on ? new Vector3(1.06f, 1.06f, 1) : Vector3.one);
                c.style.translate = new Translate(0, on ? -12 : 0);
                c.style.backgroundColor = on ? new Color(0.12f, 0.2f, 0.36f, 0.98f) : new Color(0.08f, 0.12f, 0.24f, 0.97f);
                GameUI.Border(c, on ? GameUI.CCyan : edge, 3, 12);
                if (on) _sfx.Play("hover", 0.7f);
            }
            void Pick()
            {
                if (locked) return;
                if (group != null) foreach (var g in group) g.SetEnabled(false);
                locked = true;
                c.style.scale = new Scale(new Vector3(0.97f, 0.97f, 1));
                _sfx.Play("click");
                c.schedule.Execute(() => pick?.Invoke()).StartingIn(120);
            }
            c.RegisterCallback<PointerEnterEvent>(_ => Lift(true));
            c.RegisterCallback<PointerLeaveEvent>(_ => Lift(false));
            c.RegisterCallback<FocusInEvent>(_ => Lift(true));
            c.RegisterCallback<FocusOutEvent>(_ => Lift(false));
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
            _ui.ShowScreen(root =>
            {
                root.style.alignItems = Align.FlexStart; root.style.justifyContent = Justify.FlexStart;
                var bg = Abs(0, 0, 1920, 1080); bg.style.backgroundColor = new Color(0.01f, 0.01f, 0.03f, 1f);
                root.Add(bg);

                // ---- the lens
                const float L = 660, LX = 630, LY = 40;
                var lens = Abs(LX, LY, L, L); lens.style.overflow = Overflow.Hidden; Round(lens, L / 2);
                var sky = Abs(0, 0, L, L * 0.62f); sky.style.backgroundColor = new Color(0.82f, 0.56f, 0.4f); lens.Add(sky);
                var haze = Abs(0, L * 0.42f, L, L * 0.2f); haze.style.backgroundColor = new Color(0.9f, 0.66f, 0.5f); lens.Add(haze);
                var ridge = Abs(-40, L * 0.44f, L + 80, L * 0.22f);
                ridge.style.backgroundImage = new StyleBackground(Art.MarsRidge(1)); ridge.style.opacity = 0.9f; lens.Add(ridge);
                var ground = Abs(0, L * 0.6f, L, L * 0.4f); ground.style.backgroundColor = new Color(0.6f, 0.3f, 0.17f); lens.Add(ground);
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
                var glass = Abs(0, 0, L, L); glass.style.backgroundColor = new Color(0.6f, 0.8f, 1f, 0.05f); lens.Add(glass);
                var sweep = Abs(0, -20, L, 16); sweep.style.backgroundColor = new Color(1f, 0.15f, 0.12f, 0.85f); sweep.style.display = DisplayStyle.None; lens.Add(sweep);
                var sweepGlow = Abs(0, -40, L, 50); sweepGlow.style.backgroundColor = new Color(1f, 0.2f, 0.1f, 0.18f); sweepGlow.style.display = DisplayStyle.None; lens.Add(sweepGlow);
                root.Add(lens);
                var mask = Abs(LX - 6, LY - 6, L + 12, L + 12); mask.style.backgroundImage = new StyleBackground(Art.LensMask()); root.Add(mask);
                var rim = Abs(LX - 14, LY - 14, L + 28, L + 28); rim.style.borderTopWidth = rim.style.borderBottomWidth = rim.style.borderLeftWidth = rim.style.borderRightWidth = 16;
                rim.style.borderTopColor = rim.style.borderBottomColor = rim.style.borderLeftColor = rim.style.borderRightColor = new Color(0.08f, 0.09f, 0.13f);
                Round(rim, (L + 28) / 2); root.Add(rim);

                // breathing and a little sway
                float t0 = Time.realtimeSinceStartup;
                who.schedule.Execute(() =>
                {
                    float t = Time.realtimeSinceStartup - t0;
                    var animated = Art.Crew(visitor, 0, t % 4 > 3.8f ? -2 : -1);
                    who.style.backgroundImage = new StyleBackground(animated);
                    heat.style.backgroundImage = new StyleBackground(animated);
                    float b = 1 + Mathf.Sin(t * 2.2f) * 0.012f;
                    var tr = new Translate(Mathf.Sin(t * 0.7f) * 6f, (1 - b) * ph * 0.5f);
                    who.style.scale = new Scale(new Vector3(1, b, 1)); who.style.translate = tr;
                    heat.style.scale = new Scale(new Vector3(1, b, 1)); heat.style.translate = tr;
                }).Every(40);

                // ---- left: what you found
                var log = new VisualElement(); log.style.position = Position.Absolute; log.style.left = 34; log.style.top = 34; log.style.width = 500; log.style.bottom = 34;
                log.style.backgroundColor = new Color(0.03f, 0.06f, 0.1f, 0.92f); GameUI.Border(log, GameUI.CCyanD, 2, 12);
                log.style.paddingLeft = log.style.paddingRight = 20; log.style.paddingTop = 14;
                log.Add(_ui.L(_loc.T("base.peep.header"), 20, true, GameUI.CAmber));
                var title = _ui.L(_loc.T($"life.card.{card.id}.title"), 32, true, GameUI.CCyan); title.style.marginTop = 4; log.Add(title);
                var body = _ui.L(_loc.T($"life.card.{card.id}.body"), 21); body.style.marginTop = 6; log.Add(body);
                var ev = _ui.L(_loc.T("base.peep.evidence"), 21, true, GameUI.CAmber); ev.style.marginTop = 14; log.Add(ev);
                for (int i = 1; i <= Mathf.Min(_peepTalk, card.clues); i++)
                {
                    var q = _ui.L("“" + _loc.T("base.peep.q" + i) + "”", 18, false, GameUI.CDim); q.style.marginTop = 6; log.Add(q);
                    log.Add(_ui.L("→ " + _loc.T($"life.card.{card.id}.clue{i}"), 20));
                }
                foreach (var sc in _life.ScansDone(card))
                {
                    bool odd = _life.IsOdd(visitor, sc);
                    var head = _ui.L(_loc.T($"life.scan.{sc}.name") + " · " + _loc.T(odd ? "base.peep.unusual" : "base.peep.expected"), 20, true, odd ? GameUI.CRed : GameUI.CGreen);
                    head.style.marginTop = 8; log.Add(head);
                    log.Add(_ui.L(_loc.T($"life.scan.{visitor}.{sc}"), 19));
                    log.Add(_ui.L(_loc.T("base.peep.limit." + sc), 16, false, GameUI.CDim));
                }
                if (_peepTalk == 0 && _life.ScansDone(card).Count == 0) { var none = _ui.L(_loc.T("base.peep.nothing_yet"), 19, false, GameUI.CDim); none.style.marginTop = 4; log.Add(none); }
                if (_life.State.pet != null)
                {
                    string key = _life.HintKey(card);
                    var pet = _ui.L(key == null ? _loc.T("life.hint_hungry", LbName(LifeboatSim.Pet)) : LbName(LifeboatSim.Pet) + ": " + _loc.T(key), 18, false, GameUI.CCyan);
                    pet.style.marginTop = 12; log.Add(pet);
                }
                var fic = _ui.L(_loc.T("life.fiction"), 15, false, GameUI.CDim); fic.style.position = Position.Absolute; fic.style.bottom = 10; fic.style.left = 20; fic.style.right = 20; log.Add(fic);
                root.Add(log);

                // ---- below the lens: ways to check
                var checks = new VisualElement(); checks.style.position = Position.Absolute; checks.style.left = 548; checks.style.width = 840; checks.style.top = LY + L + 26;
                checks.style.flexDirection = FlexDirection.Row; checks.style.justifyContent = Justify.Center;
                var group = new List<VisualElement>();
                int left = _life.ScansLeft(card);
                bool canTalk = _peepTalk < card.clues;
                checks.Add(BCard(_loc.T("base.peep.talk"), _ui.L(_loc.T(canTalk ? "base.peep.talk_hint" : "base.peep.talk_done"), 16, false, GameUI.CDim),
                    () => { _peepTalk++; _sfx.Play("blip"); BShowPeep(); }, canTalk, null, group, 190, GameUI.CGreen, 20));
                foreach (var sc in LC.scans)
                {
                    string id = sc;
                    bool can = _life.CanScan(card, id);
                    var hint = _ui.L(_loc.T(left > 0 ? "base.peep.scans_left" : "base.peep.no_scans", left), 16, false, GameUI.CDim);
                    checks.Add(BCard(_loc.T($"base.peep.scan.{id}"), hint, () => BRunScan(id, who, heat, sweep, sweepGlow, ph), can, null, group, 190, GameUI.CAmber, 20));
                }
                root.Add(checks);

                // ---- right: what to do
                var acts = new VisualElement(); acts.style.position = Position.Absolute; acts.style.right = 30; acts.style.top = 40; acts.style.width = 480;
                acts.Add(_ui.L(_loc.T("base.peep.decide"), 22, true, GameUI.CAmber));
                var dgroup = new List<VisualElement>();
                foreach (var o in card.options)
                {
                    var opt = o;
                    bool can = _life.CanChoose(opt, out var why);
                    var d = BOptionHint(opt, out bool perDay);
                    var cc = BCard(_loc.T($"life.card.{card.id}.{opt.id}"), BDeltaIcons(d, perDay, opt.health), () => { _peepCard = null; LbChoose(opt); }, can, can ? null : why, dgroup, 500,
                        opt.id == "refuse" ? GameUI.CRed : !string.IsNullOrEmpty(opt.admits) ? GameUI.CGreen : GameUI.CCyanD);
                    cc.style.width = 460; cc.style.minHeight = 110; cc.style.marginTop = 10; cc.style.marginLeft = 0;
                    acts.Add(cc);
                }
                var back = _ui.B(_loc.T("base.peep.back"), () => { _bOpen = false; _ui.HideScreen(); }, 22);
                back.style.marginTop = 18; back.style.alignSelf = Align.FlexStart; back.style.marginLeft = 0;
                acts.Add(back);
                root.Add(acts);

                // play the scan that was just chosen
                if (_peepFx != null) { string fx = _peepFx; _peepFx = null; BScanFx(fx, root, who, heat, sweep, sweepGlow, ph, thermalOdd); }
            });
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
