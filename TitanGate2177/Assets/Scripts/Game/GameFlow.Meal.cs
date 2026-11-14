// PROTOTYPE - NOT FOR PRODUCTION
// Question: Is sharing dinner more meaningful when you drag the meals onto the people sitting with you?
// Date: 2026-11-13
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// Evening in the pod, the walkable-base version of the ration screen: everyone sits round the console, the
    /// meals stand on a tray, and the player drags a meal onto each person (or right-clicks them for a menu).
    /// Status bars and faces show who is hungry; the sim's ration rules are unchanged (one meal = that person's
    /// food and water ration). Also the survival guide and visual-novel conversations.
    /// </summary>
    public partial class GameFlow
    {
        VisualElement _mealGhost, _mealRoot;
        int _mealDay = -1;
        readonly List<(VisualElement card, string who)> _mealCards = new List<(VisualElement, string)>();

        /// <summary>Meals still on the tray: how many more people the stock can feed, cheapest first.</summary>
        int MealsLeft()
        {
            var test = new HashSet<string>(_lbFed);
            int n = 0;
            foreach (var who in _life.Mouths())
            {
                if (test.Contains(who)) continue;
                test.Add(who);
                if (_life.CanFeed(test)) n++; else test.Remove(who);
            }
            return n;
        }

        VisualElement MealIcon(float size)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.width = size; e.style.height = size; e.style.flexDirection = FlexDirection.Row; e.style.alignItems = Align.FlexEnd;
            var can = Sticker("food", size * 0.62f); var bottle = Sticker("water", size * 0.5f);
            bottle.style.marginLeft = -size * 0.12f;
            e.Add(can); e.Add(bottle);
            return e;
        }

        Sprite MouthSprite(string who)
        {
            if (who == LifeboatSim.Me) return Art.Crew("cadet", 0, -1);
            if (who == LifeboatSim.Pet) return Art.Pet(_life.State.pet);
            return Art.Crew(who, 0, -1);
        }

        void MealFeed(string who, bool on)
        {
            if (on)
            {
                var test = new HashSet<string>(_lbFed) { who };
                if (!_life.CanFeed(test)) { _sfx.Play("bad"); _ui.Toast(_loc.T("base.meal.none_left"), GameUI.CRed); return; }
                _lbFed.Add(who); _sfx.Play("good");
            }
            else { _lbFed.Remove(who); _sfx.Play("click"); }
            LbShow();
        }

        readonly List<(VisualElement fig, string who)> _mealFigures = new List<(VisualElement, string)>();
        readonly List<string> _dayNotes = new List<string>();
        int _dayNotesDay = -1;

        /// <summary>One line for tonight's journal page (what happened today).</summary>
        void DayNote(string line)
        {
            if (_life == null || string.IsNullOrEmpty(line)) return;
            if (_dayNotesDay != _life.State.day) { _dayNotes.Clear(); _dayNotesDay = _life.State.day; }
            if (_dayNotes.Count < 7) _dayNotes.Add(line);
        }

        static readonly Color Ink = new Color(0.16f, 0.12f, 0.09f), InkDim = new Color(0.38f, 0.32f, 0.26f), InkRed = new Color(0.62f, 0.12f, 0.1f), InkGreen = new Color(0.12f, 0.42f, 0.2f);

        /// <summary>
        /// Dinner, 60 Seconds-style: a side view of the pod like a doll's house with everyone seated behind the
        /// table under the lamp, and the journal open on the right: today's notes, then who eats tonight.
        /// Drag a meal from the journal onto a person (in the scene or the list), click a box, or right-click.
        /// </summary>
        void BShowMeal()
        {
            if (_playtest) Log($"BShowMeal day {_life.State.day} mealDay {_mealDay} fed {_lbFed.Count}");
            // each evening starts with full plates on the tray: the player hands them out
            if (_mealDay != _life.State.day) { _mealDay = _life.State.day; _lbFed.Clear(); }
            _ui.ShowScreen(root =>
            {
                _mealRoot = root;
                _mealCards.Clear(); _mealFigures.Clear();
                root.style.alignItems = Align.FlexStart; root.style.justifyContent = Justify.FlexStart;
                var bg = Abs(0, 0, 1920, 1080); bg.style.backgroundColor = new Color(0.02f, 0.02f, 0.05f, 1f); root.Add(bg);

                // ---- the scene
                const float K = 3.6f, T = 130, X0 = 20;
                var back = Abs(X0, T, 320 * K, 180 * K); back.style.backgroundImage = new StyleBackground(Art.PodDiningBack()); root.Add(back);
                var mouths = _life.Mouths();
                int n = mouths.Count;
                for (int i = 0; i < n; i++)
                {
                    string who = mouths[i];
                    bool fed = _lbFed.Contains(who);
                    bool pet = who == LifeboatSim.Pet;
                    var sp = pet ? Art.Pet(_life.State.pet) : who == LifeboatSim.Me ? Art.Crew("cadet", 0, -1) : Art.Crew(who, 0, -1);
                    float h = pet ? 150 : 290, w = sp.rect.width * h / sp.rect.height;
                    float cx = X0 + 160 * K + (i - (n - 1) / 2f) * Mathf.Min(230, 900f / Mathf.Max(1, n));
                    float feet = T + (pet ? 512 : 520);
                    var fig = Abs(cx - w / 2, feet - h, w, h); fig.style.backgroundImage = new StyleBackground(sp);
                    if (!fed) fig.style.unityBackgroundImageTintColor = new Color(0.8f, 0.8f, 0.9f);
                    fig.pickingMode = PickingMode.Position;
                    string id = who;
                    fig.RegisterCallback<PointerDownEvent>(e => { if (e.button == 1) { BMealMenu(id, e.position); e.StopPropagation(); } else if (_lbFed.Contains(id)) MealFeed(id, false); });
                    root.Add(fig);
                    _mealFigures.Add((fig, who));
                    // a speech-bubble status over each head
                    var bub = new VisualElement { pickingMode = PickingMode.Ignore };
                    bub.style.position = Position.Absolute; bub.style.left = cx - 80; bub.style.width = 160; bub.style.top = feet - h - 70;
                    bub.style.alignItems = Align.Center; bub.style.paddingTop = bub.style.paddingBottom = 4;
                    bub.style.backgroundColor = new Color(0.97f, 0.94f, 0.86f, 0.95f); GameUI.Border(bub, fed ? new Color(0.2f, 0.6f, 0.3f) : new Color(0.75f, 0.3f, 0.2f), 2, 10);
                    bub.Add(_ui.L(LbName(who), 17, true, Ink));
                    var icons = new VisualElement { pickingMode = PickingMode.Ignore }; icons.style.flexDirection = FlexDirection.Row; icons.style.alignItems = Align.Center;
                    if (fed) icons.Add(_ui.L(TT("full", "อิ่มแล้ว"), 16, true, InkGreen));
                    else { var m = MealIcon(28); m.style.opacity = 0.55f; icons.Add(m); icons.Add(_ui.L(TT(" hungry", " หิว"), 16, true, InkRed)); }
                    bub.Add(icons);
                    root.Add(bub);
                }
                var table = Abs(X0, T + 120 * K, 320 * K, 60 * K); table.style.backgroundImage = new StyleBackground(Art.PodDiningTable()); root.Add(table);
                for (int i = 0; i < n; i++)
                    if (_lbFed.Contains(mouths[i]))
                    {
                        var plate = MealIcon(54); plate.style.position = Position.Absolute;
                        plate.style.left = _mealFigures[i].fig.resolvedStyle.left; // set below once laid out
                        float cx = X0 + 160 * K + (i - (n - 1) / 2f) * Mathf.Min(230, 900f / Mathf.Max(1, n));
                        plate.style.left = cx - 27; plate.style.top = T + 120 * K + 6 * K - 40;
                        root.Add(plate);
                    }
                var lamp = Abs(X0, T, 320 * K, 180 * K); lamp.pickingMode = PickingMode.Ignore; lamp.style.backgroundColor = new Color(1f, 0.8f, 0.5f, 0.05f); root.Add(lamp);
                float t0 = Time.realtimeSinceStartup;
                lamp.schedule.Execute(() => lamp.style.backgroundColor = new Color(1f, 0.8f, 0.5f, 0.04f + Mathf.PerlinNoise((Time.realtimeSinceStartup - t0) * 3f, 0.3f) * 0.05f)).Every(60);
                // supplies and radiation under the scene, so the meal choice is made with the stock in view
                var gauges = BGaugeRow(1.25f); gauges.style.position = Position.Absolute; gauges.style.left = 40; gauges.style.top = T + 180 * K + 24; root.Add(gauges);
                var hp = _ui.L(TT("Health ", "สุขภาพ ") + _life.State.health + "/" + LC.health + TT(" · Morale ", " · กำลังใจ ") + _life.State.morale, 24, true, GameUI.CText);
                hp.style.position = Position.Absolute; hp.style.left = 760; hp.style.top = T + 180 * K + 30; root.Add(hp);
                var rad = RadBar(300); rad.style.position = Position.Absolute; rad.style.left = 760; rad.style.top = T + 180 * K + 80; root.Add(rad);
                var cap = _ui.L(_loc.T("base.meal.title", _life.State.day), 34, true, GameUI.CAmber); cap.style.position = Position.Absolute; cap.style.left = 40; cap.style.top = 40; root.Add(cap);
                var help = _ui.L(_loc.T("base.meal.help"), 18, false, GameUI.CDim); help.style.position = Position.Absolute; help.style.left = 40; help.style.top = 88; help.style.width = 1100; root.Add(help);

                // ---- the journal
                var book = new VisualElement(); book.style.position = Position.Absolute; book.style.left = 1215; book.style.top = 40; book.style.width = 670; book.style.bottom = 40;
                book.style.backgroundColor = new Color(0.93f, 0.89f, 0.78f); GameUI.Border(book, new Color(0.42f, 0.3f, 0.18f), 6, 12);
                book.style.paddingLeft = 36; book.style.paddingRight = 26; book.style.paddingTop = 18; book.style.paddingBottom = 14;
                var spine = Abs(0, 0, 18, 2000); spine.style.backgroundColor = new Color(0.55f, 0.38f, 0.22f); book.Add(spine);
                var head = new VisualElement(); head.style.flexDirection = FlexDirection.Row; head.style.justifyContent = Justify.SpaceBetween; head.style.alignItems = Align.Center;
                head.Add(_ui.L(TT("Journal · day ", "บันทึก · วันที่ ") + _life.State.day, 30, true, Ink));
                head.Add(_ui.B(_loc.T("base.guide.open"), BShowGuide, 17));
                book.Add(head);
                if (_dayNotesDay != _life.State.day) { _dayNotes.Clear(); _dayNotesDay = _life.State.day; }
                if (_dayNotes.Count == 0) book.Add(_ui.L(TT("A quiet day at the base.", "วันนี้ที่ฐานเงียบสงบ"), 19, false, InkDim));
                foreach (var line in _dayNotes) { var l = _ui.L("– " + line, 18, false, Ink); l.style.marginTop = 2; book.Add(l); }
                var rule = new VisualElement(); rule.style.height = 2; rule.style.marginTop = 10; rule.style.marginBottom = 8; rule.style.backgroundColor = new Color(0.55f, 0.45f, 0.32f); book.Add(rule);
                book.Add(_ui.L(TT("Dinner tonight", "มื้อเย็นคืนนี้"), 24, true, Ink));
                var tray = new VisualElement(); tray.style.flexDirection = FlexDirection.Row; tray.style.alignItems = Align.Center; tray.style.flexWrap = Wrap.Wrap; tray.style.marginTop = 4; tray.style.minHeight = 64;
                int left = MealsLeft();
                tray.Add(_ui.L(_loc.T("base.meal.tray"), 18, true, InkDim));
                for (int i = 0; i < left; i++)
                {
                    var m = MealIcon(56);
                    m.pickingMode = PickingMode.Position; m.style.marginLeft = 8;
                    m.RegisterCallback<PointerDownEvent>(e => MealDragStart(e.position));
                    tray.Add(m);
                }
                if (left == 0) tray.Add(_ui.L(_loc.T("base.meal.tray_empty"), 17, false, InkRed));
                book.Add(tray);
                foreach (var who in mouths)
                {
                    string id = who; bool fed = _lbFed.Contains(who);
                    var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 6;
                    row.style.paddingLeft = row.style.paddingRight = 8; row.style.paddingTop = row.style.paddingBottom = 4;
                    row.style.backgroundColor = fed ? new Color(0.8f, 0.9f, 0.75f, 0.6f) : new Color(1f, 1f, 1f, 0.25f); Round(row, 8);
                    var spr = MouthSprite(who);
                    var pic = new VisualElement { pickingMode = PickingMode.Ignore }; float k = 56f / spr.rect.height; pic.style.width = spr.rect.width * k; pic.style.height = 56; pic.style.backgroundImage = new StyleBackground(spr); row.Add(pic);
                    var col = new VisualElement { pickingMode = PickingMode.Ignore }; col.style.marginLeft = 10; col.style.flexGrow = 1;
                    col.Add(_ui.L(LbName(who), 20, true, Ink));
                    string state = fed ? TT("eats tonight", "ได้กินคืนนี้") : who == LifeboatSim.Me ? TT("no food = health −1", "ไม่กิน = สุขภาพ −1")
                        : who == LifeboatSim.Pet ? TT("no food = it won't help tomorrow", "ไม่กิน = พรุ่งนี้ไม่ช่วยงาน") : TT("no food = leaves, may break things", "ไม่กิน = ออกจากฐาน อาจทำของพัง");
                    col.Add(_ui.L(state, 16, false, fed ? InkGreen : InkRed));
                    if (who != LifeboatSim.Me && who != LifeboatSim.Pet && _bFriend.ContainsKey(who)) col.Add(BondHearts(who, 16));
                    row.Add(col);
                    var box = new VisualElement(); box.style.width = 46; box.style.height = 46; box.style.backgroundColor = new Color(1, 1, 1, 0.6f); GameUI.Border(box, Ink, 3, 6);
                    box.style.alignItems = Align.Center; box.style.justifyContent = Justify.Center;
                    if (fed) box.Add(MealIcon(36));
                    box.RegisterCallback<ClickEvent>(_ => MealFeed(id, !_lbFed.Contains(id)));
                    row.Add(box);
                    row.RegisterCallback<PointerDownEvent>(e => { if (e.button == 1) { BMealMenu(id, e.position); e.StopPropagation(); } });
                    _mealCards.Add((row, who));
                    book.Add(row);
                }
                bool makerCan = _life.CanRunMaker; if (!makerCan) _lbMaker = false;
                if (LC.o2Maker.o2 > 0)
                {
                    var mk = _ui.B(_loc.T(_lbMaker ? "base.meal.maker_on" : "base.meal.maker_off"), () => { _lbMaker = !_lbMaker; LbShow(); }, 19, _lbMaker);
                    mk.SetEnabled(makerCan); mk.style.alignSelf = Align.FlexStart; mk.style.marginLeft = 0; mk.style.marginTop = 10;
                    book.Add(mk);
                    if (!makerCan) book.Add(_ui.L(_loc.T("life.maker.no_power"), 15, false, InkRed));
                }
                bool can = _life.CanFeed(_lbFed);
                bool run = _lbMaker && makerCan;
                if (_life.O2TonightWith(run) > _life.State.stock.o2 + (run ? LC.o2Maker.o2 : 0)) book.Add(_ui.L(_loc.T("life.ration.o2_short"), 19, true, InkRed));
                var sleep = _ui.B(_loc.T("base.meal.sleep"), LbSleep, 26, true);
                sleep.style.alignSelf = Align.FlexStart; sleep.style.marginLeft = 0; sleep.style.marginTop = 10; sleep.SetEnabled(can);
                book.Add(sleep);
                root.Add(book);

                root.RegisterCallback<PointerMoveEvent>(e => { if (_mealGhost != null) { _mealGhost.style.left = e.position.x - 30; _mealGhost.style.top = e.position.y - 30; } });
                root.RegisterCallback<PointerUpEvent>(e => MealDragEnd(e.position));
            });
            LbStage();
        }

        VisualElement BMealCard(string who)
        {
            bool fed = _lbFed.Contains(who);
            var c = new VisualElement();
            c.style.width = 250; c.style.marginRight = 12; c.style.marginTop = 6; c.style.alignItems = Align.Center;
            c.style.paddingTop = 8; c.style.paddingBottom = 10; c.style.paddingLeft = c.style.paddingRight = 10;
            c.style.backgroundColor = fed ? new Color(0.07f, 0.2f, 0.14f, 0.95f) : new Color(0.1f, 0.1f, 0.18f, 0.95f);
            GameUI.Border(c, fed ? GameUI.CGreen : GameUI.CCyanD, 2, 10);
            var sp = MouthSprite(who);
            var pic = new VisualElement { pickingMode = PickingMode.Ignore };
            float k = Mathf.Min(110f / sp.rect.height, 120f / sp.rect.width);
            pic.style.width = sp.rect.width * k; pic.style.height = sp.rect.height * k; pic.style.backgroundImage = new StyleBackground(sp);
            c.Add(pic);
            c.Add(_ui.L(LbName(who), 22, true, GameUI.CAmber));
            // simple status bars: full after a meal; low when skipped (and lower for the pet that went hungry)
            float hunger = fed ? 1f : who == LifeboatSim.Pet && !_life.State.petFed ? 0.15f : 0.4f;
            float thirst = fed ? 1f : 0.45f;
            float mood = Mathf.Clamp01(_life.State.morale / 100f) * (fed ? 1f : 0.8f);
            c.Add(MealBar("base.meal.full", hunger, new Color(1f, 0.72f, 0.25f)));
            c.Add(MealBar("base.meal.drink", thirst, new Color(0.35f, 0.6f, 1f)));
            c.Add(MealBar("base.meal.mood", mood, new Color(0.4f, 0.9f, 0.55f)));
            // rest: how well they slept last night (cold and dark when the power ran out) and how healthy they are
            bool cold = _lbNight != null && _lbNight.Exists(e => e.kind == LifeLogKind.NoPower);
            float rest = (who == LifeboatSim.Me ? _life.State.health / (float)LC.health : 0.85f) * (cold ? 0.5f : 1f);
            c.Add(MealBar("base.meal.rest", rest, new Color(0.75f, 0.6f, 1f)));
            var plate = new VisualElement { pickingMode = PickingMode.Ignore };
            plate.style.width = 120; plate.style.height = 54; plate.style.marginTop = 6; plate.style.alignItems = Align.Center; plate.style.justifyContent = Justify.Center;
            plate.style.backgroundColor = new Color(0.85f, 0.85f, 0.9f, 0.25f); Round(plate, 27);
            if (fed) plate.Add(MealIcon(48)); else plate.Add(_ui.L(_loc.T("base.meal.drop_here"), 15, false, GameUI.CDim));
            c.Add(plate);
            c.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button == 1) { BMealMenu(who, e.position); e.StopPropagation(); }
                else if (fed) MealFeed(who, false);
            });
            _mealCards.Add((c, who));
            return c;
        }

        VisualElement MealBar(string key, float v, Color col)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore }; row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 2;
            var l = _ui.L(_loc.T(key), 15, false, GameUI.CDim); l.style.width = 70; row.Add(l);
            var bar = new VisualElement { pickingMode = PickingMode.Ignore }; bar.style.width = 140; bar.style.height = 10; bar.style.backgroundColor = new Color(0.1f, 0.12f, 0.2f); Round(bar, 5);
            var fill = new VisualElement { pickingMode = PickingMode.Ignore }; fill.style.width = Length.Percent(v * 100); fill.style.height = 10; fill.style.backgroundColor = col; Round(fill, 5);
            bar.Add(fill); row.Add(bar);
            return row;
        }

        void MealDragStart(Vector2 at)
        {
            if (_mealRoot == null) return;
            _mealGhost = MealIcon(60);
            _mealGhost.style.position = Position.Absolute; _mealGhost.style.left = at.x - 30; _mealGhost.style.top = at.y - 30;
            _mealRoot.Add(_mealGhost);
            _sfx.Play("card");
        }

        void MealDragEnd(Vector2 at)
        {
            if (_mealGhost == null) return;
            _mealGhost.RemoveFromHierarchy(); _mealGhost = null;
            foreach (var (card, who) in _mealCards)
                if (card.worldBound.Contains(at)) { if (!_lbFed.Contains(who)) MealFeed(who, true); return; }
            foreach (var (fig, who) in _mealFigures)
                if (fig.worldBound.Contains(at)) { if (!_lbFed.Contains(who)) MealFeed(who, true); return; }
        }

        /// <summary>Right-click menu over a person: give the meal, or skip it tonight, with what each does.</summary>
        void BMealMenu(string who, Vector2 at)
        {
            var menu = new VisualElement();
            menu.style.position = Position.Absolute; menu.style.left = Mathf.Min(at.x, 1500); menu.style.top = Mathf.Min(at.y, 800); menu.style.width = 400;
            menu.style.backgroundColor = new Color(0.05f, 0.07f, 0.14f, 0.98f); GameUI.Border(menu, GameUI.CAmber, 2, 10);
            menu.style.paddingLeft = menu.style.paddingRight = 10; menu.style.paddingTop = menu.style.paddingBottom = 8;
            menu.Add(_ui.L(LbName(who), 22, true, GameUI.CAmber));
            var group = new List<VisualElement>();
            var give = BCard(_loc.T("base.meal.give"), _ui.L(_loc.T("base.meal.give_hint"), 16, false, GameUI.CGreen), () => { menu.RemoveFromHierarchy(); MealFeed(who, true); }, !_lbFed.Contains(who), null, group, 370, GameUI.CGreen, 20);
            give.style.minHeight = 70; give.style.marginLeft = 0; menu.Add(give);
            var skip = BCard(_loc.T("base.meal.skip"), _ui.L(_loc.T(who == LifeboatSim.Me ? "life.ration.skip_me" : who == LifeboatSim.Pet ? "life.ration.skip_pet" : "life.ration.skip_guest"), 16, false, GameUI.CRed),
                () => { menu.RemoveFromHierarchy(); MealFeed(who, false); }, _lbFed.Contains(who), null, group, 370, GameUI.CRed, 20);
            skip.style.minHeight = 70; skip.style.marginLeft = 0; skip.style.marginTop = 6; menu.Add(skip);
            var close = _ui.B(_loc.T("ui.close"), () => menu.RemoveFromHierarchy(), 18); close.style.alignSelf = Align.FlexEnd; menu.Add(close);
            _mealRoot?.Add(menu);
        }

        // ------------------------------------------------------------------ survival guide

        int _guidePage;

        /// <summary>The survival handbook: short illustrated pages that explain how the base keeps everyone alive.</summary>
        void BShowGuide()
        {
            _ui.ShowModal(p =>
            {
                p.style.width = 980;
                int n = 8;
                _guidePage = Mathf.Clamp(_guidePage, 0, n - 1);
                var head = new VisualElement(); head.style.flexDirection = FlexDirection.Row; head.style.justifyContent = Justify.SpaceBetween;
                head.Add(_ui.L(_loc.T("base.guide.title"), 30, true, GameUI.CAmber));
                head.Add(_ui.L($"{_guidePage + 1}/{n}", 22, true, GameUI.CDim));
                p.Add(head);
                string icon = new[] { "o2", "water", "food", "power", "o2", "food", "power", "water" }[_guidePage];
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 10;
                row.Add(Sticker(icon, 90));
                var col = new VisualElement(); col.style.marginLeft = 20; col.style.flexShrink = 1;
                col.Add(_ui.L(_loc.T($"base.guide.{_guidePage + 1}.title"), 26, true, GameUI.CCyan));
                col.Add(_ui.L(_loc.T($"base.guide.{_guidePage + 1}.body"), 22));
                row.Add(col); p.Add(row);
                var nav = new VisualElement(); nav.style.flexDirection = FlexDirection.Row; nav.style.marginTop = 16; nav.style.justifyContent = Justify.SpaceBetween;
                var prev = _ui.B("◀", () => { _guidePage--; _ui.RebuildModal(); }, 24); prev.SetEnabled(_guidePage > 0);
                var next = _ui.B("▶", () => { _guidePage++; _ui.RebuildModal(); }, 24); next.SetEnabled(_guidePage < n - 1);
                nav.Add(prev); nav.Add(_ui.B(_loc.T("ui.close"), () => _ui.CloseModal(), 22, true)); nav.Add(next);
                p.Add(nav);
            });
        }

        // ------------------------------------------------------------------ visual-novel conversation

        /// <summary>
        /// A conversation shown like a visual novel: the speaker large on the left, name plate, text box at the
        /// bottom, and reply choices. Each line is (speaker id or null for the cadet, text).
        /// </summary>
        void BShowVN(string id, List<(string who, string text)> lines, List<(string label, System.Action act)> replies, int index = 0)
        {
            _ui.ShowScreen(root =>
            {
                root.style.alignItems = Align.FlexStart; root.style.justifyContent = Justify.FlexEnd;
                var dim = Abs(0, 0, 1920, 1080); dim.style.backgroundColor = new Color(0.01f, 0.02f, 0.05f, 0.72f); root.Add(dim);
                var (who, text) = lines[Mathf.Clamp(index, 0, lines.Count - 1)];
                var sp = who == null ? Art.Crew("cadet", 0, -1) : Art.Crew(who, 0, -1, false, false, index == 0);
                float k = 640f / sp.rect.height;
                var pic = Abs(who == null ? 1180 : 160, 1080 - 640 - 250, sp.rect.width * k, 640); pic.style.backgroundImage = new StyleBackground(sp); root.Add(pic);
                var boxv = new VisualElement(); boxv.style.position = Position.Absolute; boxv.style.left = 80; boxv.style.right = 80; boxv.style.bottom = 40; boxv.style.minHeight = 230;
                boxv.style.backgroundColor = new Color(0.05f, 0.07f, 0.14f, 0.97f); GameUI.Border(boxv, GameUI.CCyan, 3, 16);
                boxv.style.paddingLeft = boxv.style.paddingRight = 34; boxv.style.paddingTop = 18; boxv.style.paddingBottom = 18;
                var plate = _ui.L(who == null ? _loc.T("life.crew.me") : LbName(who), 26, true, GameUI.CAmber);
                plate.style.position = Position.Absolute; plate.style.top = -24; plate.style.left = 30; plate.style.backgroundColor = new Color(0.05f, 0.07f, 0.14f, 1f);
                plate.style.paddingLeft = plate.style.paddingRight = 14; GameUI.Border(plate, GameUI.CAmber, 2, 8);
                boxv.Add(plate);
                if (who != null && _life != null && _life.State.guests.Contains(who))
                {
                    var hearts = BondHearts(who, 22); hearts.style.position = Position.Absolute; hearts.style.top = -18; hearts.style.right = 30; boxv.Add(hearts);
                }
                var body = _ui.L(text, 27); body.style.marginTop = 10; boxv.Add(body);
                bool last = index >= lines.Count - 1;
                var opts = new VisualElement(); opts.style.flexDirection = FlexDirection.Row; opts.style.flexWrap = Wrap.Wrap; opts.style.marginTop = 12;
                if (!last) opts.Add(_ui.B(_loc.T("life.continue"), () => BShowVN(id, lines, replies, index + 1), 22, true));
                else foreach (var (label, act) in replies) { var a = act; opts.Add(_ui.B(label, () => { _ui.HideScreen(); a?.Invoke(); }, 22, false)); }
                boxv.Add(opts);
                root.Add(boxv);
            });
        }
    }
}
