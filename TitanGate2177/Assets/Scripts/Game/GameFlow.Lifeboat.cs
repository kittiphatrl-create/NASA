// PROTOTYPE - NOT FOR PRODUCTION
// Question: Is Chapter 2 more fun as a read-and-choose survival diary (visitors, a pet, shared rations)
//           than as a walk-and-click outpost?
// Date: 2026-11-11
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// Card-based Chapter 2 prototype: a survival diary in the lifeboat. Pick one pet, then each day read the
    /// day's situations and choose, decide who eats and drinks, and read what the night did.
    /// All rules live in LifeboatSim; this file only draws its state and passes the clicks on.
    /// </summary>
    public partial class GameFlow
    {
        enum LbView { Pick, Card, Result, Ration, Night, End }

        LifeboatSim _life;
        LbView _lbView;
        LifeCard _lbCard;
        LifeOption _lbPicked;
        List<LifeLog> _lbNight;
        int _lbNightDay;
        Stock _lbNightUsed;
        List<string> _lbStowed;
        readonly HashSet<string> _lbFed = new HashSet<string>();
        bool _lbMaker;
        /// <summary>The pet pick is shown before the 60-second scramble, not inside the diary.</summary>
        bool _lbPickForScramble;
        string _lbScramblePet, _lbFixedPet;

        /// <summary>Verification of the old walk-and-click chapter: keep the landing leading to the outpost.</summary>
        public static bool ForceOutpost;

        LifeboatConfig LC => _cfg.lifeboat;
        public LifeboatSim Life => _life;
        /// <summary>The landing leads into the survival diary (one room, read and choose).</summary>
        bool UseLifeboat => LC.replacesOutpost && !ForceOutpost;

        /// <summary>
        /// Start the diary. stowed = supplies from the 60-second scramble (null = the demo kit);
        /// pet = the pet picked before the scramble (null = show the pick screen first).
        /// </summary>
        void StartLifeboat(List<string> stowed = null, string pet = null)
        {
            _lbStowed = stowed ?? new List<string>(LC.demoStowed);
            _lbFixedPet = pet;
            _lbPickForScramble = false;
            _life = null;
            _chapter = 3;   // no world input: the whole mode is read-and-choose
            _busy = true;
            Time.timeScale = 1f;
            if (WalkBase)
            {
                // walk the week around a top-down base; the cards wait at places in it
                StartBase();
                _lbView = LbView.Pick;
                if (pet != null && LC.GetPet(pet) != null) LbPickPet(pet);
                else LbShow();
                return;
            }
            ClearWorld();
            BuildShip(true);
            BuildMarsOutside();
            _player.gameObject.SetActive(true);
            _orbi.gameObject.SetActive(true);
            _player.SetSuit(false);
            LbPlaceStage();
            _ui.SetHudVisible(false);
            _ui.SetBaseButton(false);
            _ui.CloseModal();
            _sfx.Loop("hum", true);
            // one room: the cabin stays visible on the left, the diary sits on the right
            _ui.FadeTo(0.35f, 0.4f);
            _lbView = LbView.Pick;
            if (pet != null && LC.GetPet(pet) != null) LbPickPet(pet);
            else LbShow();
        }

        /// <summary>Pet pick before the 60-second scramble. Returns when one is chosen.</summary>
        IEnumerator LbPickBeforeScramble()
        {
            _lbPickForScramble = true;
            _lbScramblePet = null;
            _life = null;
            _lbView = LbView.Pick;
            LbShow();
            while (_lbPickForScramble) yield return null;
        }

        void LbScan(string scan)
        {
            var odd = _life.Scan(_lbCard.id, scan);
            if (odd == null) return;
            _sfx.Play(odd.Value ? "bad" : "good");
            LbShow();
        }

        void LbPickPet(string pet)
        {
            _life = new LifeboatSim(LC, pet, LifeboatSim.Bonus(LC, _lbStowed), OutpostSim.SupplyFlags(_cfg.chapter1, _lbStowed));
            // walkable base: a different set of visitors every run, never two knock days in a row
            if (WalkBase) _life.RandomizeVisitors(SeedOverride >= 0 ? SeedOverride : System.Environment.TickCount);
            LbNextCard();
            if (WalkBase) _busy = false;
        }

        void LbNextCard()
        {
            _bOpen = false;
            _lbCard = _life.NextCard();
            _lbPicked = null;
            if (_life.State.outcome != LifeOutcome.Ongoing) _lbView = LbView.End;
            else if (_lbCard != null) _lbView = LbView.Card;
            else
            {
                // default plan: everyone eats if the stock allows it, guests give way first
                _lbFed.Clear();
                var plan = _life.Mouths();
                while (!_life.CanFeed(plan)) plan.RemoveAt(plan.Count - 1);
                foreach (var who in plan) _lbFed.Add(who);
                _lbMaker = false;
                _lbView = LbView.Ration;
            }
            LbShow();
        }

        void LbChoose(LifeOption o)
        {
            var done = _life.Choose(_lbCard.id, o.id);
            if (done == null) return;
            if (_bRoot != null && _loc.Has($"life.card.{_lbCard.id}.{o.id}")) DayNote(_loc.T($"life.card.{_lbCard.id}.title") + " → " + _loc.T($"life.card.{_lbCard.id}.{o.id}"));
            _lbPicked = done;
            _sfx.Play(string.IsNullOrEmpty(done.admits) ? "good" : "open");
            _lbView = LbView.Result;
            LbShow();
        }

        void LbSleep()
        {
            if (_playtest) Log("LbSleep fed=" + _lbFed.Count + " mouths=" + _life.Mouths().Count + " from: " + System.Environment.StackTrace);
            var s = _life.State.stock;
            var before = new Stock { o2 = s.o2, water = s.water, food = s.food };
            var need = _life.RationNeed(_lbFed);
            bool run = _lbMaker && _life.CanRunMaker;
            float o2 = _life.O2TonightWith(run);
            float o2Have = before.o2 + (run ? LC.o2Maker.o2 : 0);
            int day = _life.State.day;
            var night = _life.EndDay(_lbFed, run);
            if (_bRoot != null && night != null) { BFriendsAfterNight(_lbFed); BAfterNight(night); RadNight(day); }
            if (night == null) return;
            _lbNight = night;
            _lbNightDay = day;
            _lbNightUsed = new Stock { water = need.water, food = need.food, o2 = Mathf.Min(o2, o2Have) };
            _sfx.Play(night.Exists(e => e.kind == LifeLogKind.Theft || e.kind == LifeLogKind.NoPower || e.kind == LifeLogKind.Hungry || e.kind == LifeLogKind.NightHit) ? "bad" : "good");
            _lbView = LbView.Night;
            LbShow();
        }

        // ------------------------------------------------------------------ text helpers

        string LbName(string who)
        {
            if (who == LifeboatSim.Me) return _loc.T("life.crew.me");
            if (who == LifeboatSim.Pet) return _loc.T($"pet.{_life.State.pet}.name");
            return _loc.Has($"visitor.{who}.name") ? _loc.T($"visitor.{who}.name") : _loc.T($"pet.{who}.name");
        }

        static string LbNum(float v) => Mathf.Approximately(v, Mathf.Round(v)) ? Mathf.RoundToInt(v).ToString() : v.ToString("0.#");

        string LbDelta(Stock d)
        {
            var sb = new StringBuilder();
            void Part(string key, float v)
            {
                if (v == 0) return;
                if (sb.Length > 0) sb.Append("   ");
                sb.Append(_loc.T(key)).Append(v > 0 ? " +" : " ").Append(LbNum(v));
            }
            Part("life.res.o2", d.o2); Part("life.res.water", d.water); Part("life.res.food", d.food); Part("life.res.power", d.power);
            return sb.Length == 0 ? _loc.T("life.no_cost") : sb.ToString();
        }

        string LbCost(LifeOption o)
        {
            string cost = LbDelta(o.delta);
            if (o.health == 0) return cost;
            string hp = _loc.T("life.res.health_name") + (o.health > 0 ? " +" : " ") + o.health;
            return o.delta.IsZero ? hp : cost + "   " + hp;
        }

        string LbItems()
        {
            var names = new List<string>();
            var sorted = new List<string>(_life != null ? (IEnumerable<string>)_life.State.items : OutpostSim.SupplyFlags(_cfg.chapter1, _lbStowed));
            sorted.Sort(string.CompareOrdinal);
            foreach (var i in sorted) names.Add(_loc.T("life.item." + i));
            return names.Count == 0 ? _loc.T("life.items.none") : string.Join(", ", names);
        }

        // ------------------------------------------------------------------ screen

        void LbShow()
        {
            if (BInterceptShow()) return;
            if (_bRoot != null && _lbView == LbView.Card && _lbCard != null && !string.IsNullOrEmpty(_lbCard.visitor)) { BShowPeep(); return; }
            if (_bRoot != null && _lbView == LbView.Ration) { BShowMeal(); return; }
            _ui.ShowScreen(root =>
            {
                var box = new VisualElement();
                bool wide = _lbView == LbView.Pick;
                if (!wide) { root.style.alignItems = Align.FlexEnd; box.style.marginRight = 70; }
                box.style.width = wide ? 1560 : 1220;
                box.style.backgroundColor = new Color(0.03f, 0.05f, 0.12f, 0.95f);
                var edge = _lbView == LbView.End && LbFailed ? GameUI.CRed
                    : _lbView == LbView.End && _life.State.outcome == LifeOutcome.Stranded ? GameUI.CAmber
                    : _lbView == LbView.Card && !string.IsNullOrEmpty(_lbCard.visitor) ? GameUI.CAmber : GameUI.CCyan;
                GameUI.Border(box, edge, 3, 14);
                box.style.paddingLeft = box.style.paddingRight = 40; box.style.paddingTop = 22; box.style.paddingBottom = 22;
                if (_life != null) LbHeader(box);
                switch (_lbView)
                {
                    case LbView.Pick: LbBuildPick(box); break;
                    case LbView.Card: LbBuildCard(box); break;
                    case LbView.Result: LbBuildResult(box); break;
                    case LbView.Ration: LbBuildRation(box); break;
                    case LbView.Night: LbBuildNight(box); break;
                    case LbView.End: LbBuildEnd(box); break;
                }
                root.Add(box);
            });
            LbStage();
        }

        // ------------------------------------------------------------------ the room (left of the diary)

        const float LbCamShift = 2.5f, LbHatchX = -4.1f, LbPlayerX = -2.6f, LbPetX = -3.3f;
        static readonly float[] LbGuestX = { -1.6f, -0.9f, -0.2f, 0.5f };
        SpriteRenderer _lbPetSr, _lbHatchSr;
        readonly List<SpriteRenderer> _lbGuestSr = new List<SpriteRenderer>();

        /// <summary>Camera and astronaut for the one-room view: the room sits left of the diary panel.</summary>
        void LbPlaceStage()
        {
            _room = RQuarters;
            float cx = _world.rooms[RQuarters].centerX;
            _rig.SnapTo(cx + LbCamShift);
            _player.Place(cx + LbPlayerX);
            _orbi.Snap();
            _lbPetSr = _lbHatchSr = null;
            _lbGuestSr.Clear();
        }

        SpriteRenderer LbSprite(SpriteRenderer sr, Sprite s, float x, float z, int order)
        {
            var room = _world.rooms[RQuarters];
            if (sr == null) sr = _world.Deco(room, s, x, 0f, z, order);
            sr.sprite = s;
            sr.transform.position = new Vector3(room.centerX + x, 0f, z);
            sr.sortingOrder = order;
            sr.color = Color.white;
            sr.gameObject.SetActive(true);
            return sr;
        }

        static void LbHide(SpriteRenderer sr) { if (sr != null) sr.gameObject.SetActive(false); }

        /// <summary>Draw who is in the room now: the pet, guests aboard, and whoever is knocking at the hatch.</summary>
        void LbStage()
        {
            if (_bRoot != null || _world == null || _world.rooms.Count <= RQuarters || _lbPickForScramble || _life == null) return;
            var st = _life.State;
            if (st.pet != null) { _lbPetSr = LbSprite(_lbPetSr, Art.Pet(st.pet), LbPetX, -0.3f, WorldBuilder.OrderPlayer + 1); _lbPetSr.flipX = false; }
            else LbHide(_lbPetSr);

            var inside = new List<string>(st.guests);
            if (st.outcome == LifeOutcome.FailedMonster && st.monsterLetIn != null) inside.Add(st.monsterLetIn);
            for (int i = 0; i < Mathf.Max(inside.Count, _lbGuestSr.Count); i++)
            {
                if (i >= _lbGuestSr.Count) _lbGuestSr.Add(null);
                if (i >= inside.Count || i >= LbGuestX.Length) { LbHide(_lbGuestSr[i]); continue; }
                _lbGuestSr[i] = LbSprite(_lbGuestSr[i], Art.Visitor(inside[i]), LbGuestX[i], 0.7f, WorldBuilder.OrderPlayer - 2);
                _lbGuestSr[i].flipX = true;   // facing the astronaut
                if (inside[i] == st.monsterLetIn) _lbGuestSr[i].color = new Color(0.75f, 0.6f, 0.7f);
            }

            // the visitor stands in the hatch while its card is open, and leaves when it is let in or turned away
            string knock = null;
            if ((_lbView == LbView.Card || _lbView == LbView.Result) && _lbCard != null && !string.IsNullOrEmpty(_lbCard.visitor)
                && !st.guests.Contains(_lbCard.visitor) && st.monsterLetIn != _lbCard.visitor)
                knock = _lbView == LbView.Card || (_lbPicked != null && _lbPicked.admits != _lbCard.visitor && _lbPicked.id != "refuse") ? _lbCard.visitor : null;
            if (knock != null) _lbHatchSr = LbSprite(_lbHatchSr, Art.Visitor(knock), LbHatchX, 1.4f, WorldBuilder.OrderProp + 2);
            else LbHide(_lbHatchSr);
            _player.Face(_world.rooms[RQuarters].centerX + (knock != null ? LbHatchX : LbGuestX[0]));
        }

        bool LbFailed => _life.State.outcome == LifeOutcome.FailedAir || _life.State.outcome == LifeOutcome.FailedHealth || _life.State.outcome == LifeOutcome.FailedMonster;

        Button LbLangButton() => _ui.B(_loc.Current == Lang.TH ? "English" : "ภาษาไทย", () => { ToggleLang(); LbShow(); }, 22);

        Label LbText(VisualElement parent, string text, int size = 24, bool bold = false, Color? color = null)
        {
            var l = _ui.L(text, size, bold, color);
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.flexShrink = 0;
            parent.Add(l);
            return l;
        }

        /// <summary>Day, the four stocks, health, who is aboard, and how long the supplies last.</summary>
        void LbHeader(VisualElement box)
        {
            var st = _life.State;
            var top = new VisualElement(); top.style.flexDirection = FlexDirection.Row; top.style.alignItems = Align.Center; top.style.justifyContent = Justify.SpaceBetween;
            top.style.flexShrink = 0;
            top.Add(_ui.L(_loc.T(_bRoot != null ? "base.hud.day" : "life.title_day", st.day, LC.daysTotal), 26, true, GameUI.CAmber));
            top.Add(LbLangButton());
            box.Add(top);

            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.flexWrap = Wrap.Wrap; row.style.flexShrink = 0;
            void Chip(string label, string value, Color c)
            {
                var chip = new VisualElement(); chip.style.flexDirection = FlexDirection.Row; chip.style.alignItems = Align.Center;
                chip.style.backgroundColor = GameUI.CPanel2; GameUI.Border(chip, c, 2, 8);
                chip.style.paddingLeft = chip.style.paddingRight = 14; chip.style.paddingTop = chip.style.paddingBottom = 4;
                chip.style.marginRight = 10; chip.style.marginTop = 4; chip.style.marginBottom = 4;
                var name = _ui.L(label, 22, false, GameUI.CDim); name.style.marginRight = 10;
                chip.Add(name);
                chip.Add(_ui.L(value, 28, true, c));
                row.Add(chip);
            }
            Color Level(float v) => v <= 1 ? GameUI.CRed : v <= 3 ? GameUI.CAmber : GameUI.CText;
            if (_bRoot != null) { }   // gauges get their own row below (they were squeezed beside the health text)
            else
            {
                Chip(_loc.T("life.res.o2"), LbNum(st.stock.o2), Level(st.stock.o2));
                Chip(_loc.T("life.res.water"), LbNum(st.stock.water), Level(st.stock.water));
                Chip(_loc.T("life.res.food"), LbNum(st.stock.food), Level(st.stock.food));
                Chip(_loc.T("life.res.power"), LbNum(st.stock.power), st.stock.power <= 0 ? GameUI.CRed : GameUI.CText);
            }
            row.Add(_ui.L(_loc.T("life.res.health", st.health, LC.health), 24, true, st.health < LC.health ? GameUI.CRed : GameUI.CGreen));
            box.Add(row);
            if (_bRoot != null) { var gauges = BGaugeRow(0.8f); gauges.style.marginTop = 4; box.Add(gauges); }

            var crew = new List<string>();
            foreach (var who in _life.Mouths())
                crew.Add(who == LifeboatSim.Pet ? $"{LbName(who)} ({_loc.T("trait." + _life.PetTrait)})" : LbName(who));
            LbText(box, _loc.T("life.crew", string.Join(", ", crew)) + "   ·   " + _loc.T("life.items", LbItems()), 21, false, GameUI.CDim);
            if (st.outcome == LifeOutcome.Ongoing)
            {
                int left = _life.DaysOfSupplies();
                int needed = LC.daysTotal - st.day + 1;
                LbText(box, _loc.T("life.days_left", left, LC.daysTotal) + "  " + _loc.T("life.tonight", LbNum(_life.O2Tonight)), 21, true, left >= needed ? GameUI.CGreen : GameUI.CAmber);
            }
            var line = new VisualElement(); line.style.height = 2; line.style.backgroundColor = GameUI.CCyanD; line.style.marginTop = 8; line.style.marginBottom = 10;
            box.Add(line);
        }

        void LbBuildPick(VisualElement box)
        {
            var top = new VisualElement(); top.style.flexDirection = FlexDirection.Row; top.style.alignItems = Align.Center; top.style.justifyContent = Justify.SpaceBetween;
            top.Add(_ui.L(_loc.T("life.pick.title"), 40, true, GameUI.CCyan));
            top.Add(LbLangButton());
            box.Add(top);
            LbText(box, _loc.T(_lbPickForScramble ? "life.pick.body_scramble" : "life.pick.body"), 25);
            if (!_lbPickForScramble) LbText(box, _loc.T("life.pick.kit", LbItems()) + "   ·   " + _loc.T("life.unit_note"), 21, false, GameUI.CDim);
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginTop = 14; row.style.justifyContent = Justify.SpaceBetween;
            foreach (var p in LC.pets)
            {
                string id = p.id;
                var col = new VisualElement(); col.style.width = 470; col.style.backgroundColor = GameUI.CPanel2;
                GameUI.Border(col, GameUI.CCyanD, 2, 10);
                col.style.paddingLeft = col.style.paddingRight = 18; col.style.paddingTop = 14; col.style.paddingBottom = 12;
                col.Add(_ui.L(_loc.T($"pet.{id}.name"), 34, true, GameUI.CAmber));
                col.Add(_ui.L(_loc.T("trait." + p.trait), 24, true, GameUI.CCyan));
                var pic = new VisualElement();
                pic.style.width = Art.PetW(id) * 6; pic.style.height = Art.PetH(id) * 6;
                pic.style.backgroundImage = new StyleBackground(Art.Pet(id));
                pic.style.alignSelf = Align.Center; pic.style.marginTop = 4; pic.style.marginBottom = 4;
                col.Add(pic);
                var desc = LbText(col, _loc.T($"pet.{id}.desc"), 22);
                desc.style.minHeight = 100;
                var b = _ui.B(_loc.T("life.pick.choose", _loc.T($"pet.{id}.name")), () =>
                {
                    if (!_lbPickForScramble) { LbPickPet(id); return; }
                    _lbScramblePet = id;
                    _lbPickForScramble = false;
                    _ui.HideScreen();
                }, 26, true);
                b.style.marginLeft = b.style.marginRight = 0;
                col.Add(b);
                row.Add(col);
            }
            box.Add(row);
            var note = LbText(box, _loc.T("life.fiction"), 20, false, GameUI.CDim);
            note.style.marginTop = 12;
        }

        void LbBuildCard(VisualElement box)
        {
            var card = _lbCard;
            _life.CardProgress(out int done, out int total);
            bool visitor = !string.IsNullOrEmpty(card.visitor);
            LbText(box, (visitor ? _loc.T("life.visitor_header") + "   ·   " : "") + _loc.T("life.card_count", done + 1, total), 21, true, visitor ? GameUI.CAmber : GameUI.CDim);
            var cardHead = box;
            if (visitor)
            {
                // look through the peephole: the visitor stands outside the airlock
                var peepRow = new VisualElement(); peepRow.style.flexDirection = FlexDirection.Row; peepRow.style.alignItems = Align.Center; peepRow.style.flexShrink = 0;
                peepRow.Add(BPeephole(card.visitor));
                cardHead = new VisualElement(); cardHead.style.flexGrow = 1; cardHead.style.flexShrink = 1; cardHead.style.marginLeft = 22;
                peepRow.Add(cardHead);
                box.Add(peepRow);
            }
            _ui.Title(cardHead, _loc.T($"life.card.{card.id}.title"));
            LbText(cardHead, _loc.T($"life.card.{card.id}.body"), visitor ? 23 : 25);
            if (card.clues > 0)
            {
                var head = LbText(box, _loc.T("life.clues"), 21, true, GameUI.CAmber);
                head.style.marginTop = 4;
                for (int i = 1; i <= card.clues; i++) LbText(box, "·  " + _loc.T($"life.card.{card.id}.clue{i}"), 21);
            }

            if (visitor) LbBuildScans(box, card);

            // the pet's opinion: the trait decides what kind of help it is
            if (_life.State.pet != null)
            {
                var hint = new VisualElement();
                hint.style.backgroundColor = new Color(0.05f, 0.16f, 0.2f, 0.9f); GameUI.Border(hint, GameUI.CCyanD, 2, 8);
                hint.style.paddingLeft = hint.style.paddingRight = 16; hint.style.paddingTop = 8; hint.style.paddingBottom = 8;
                hint.style.marginTop = 8; hint.style.marginBottom = 6; hint.style.flexShrink = 0;
                string key = _life.HintKey(card);
                string pet = LbName(LifeboatSim.Pet);
                if (key == null) LbText(hint, _loc.T("life.hint_hungry", pet), 22, false, GameUI.CDim);
                else
                {
                    LbText(hint, _loc.T("life.hint_from", pet, _loc.T("trait." + _life.PetTrait)), 21, true, GameUI.CCyan);
                    LbText(hint, _loc.T(key), 23);
                }
                box.Add(hint);
            }

            if (_bRoot != null) { BBuildOptionCards(box, card); return; }
            foreach (var o in card.options)
            {
                var opt = o;
                bool can = _life.CanChoose(opt, out var why);
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.flexShrink = 0;
                var b = _ui.B(_loc.T($"life.card.{card.id}.{opt.id}"), () => LbChoose(opt), 25, false);
                b.style.width = 720; b.style.unityTextAlign = TextAnchor.MiddleLeft; b.style.marginLeft = 0;
                b.SetEnabled(can);
                row.Add(b);
                var col = new VisualElement(); col.style.flexShrink = 1; col.style.marginLeft = 10;
                string cost = LbCost(opt);
                if (!string.IsNullOrEmpty(opt.admits))
                {
                    var v = LC.GetVisitor(opt.admits);
                    cost = LbDelta(new Stock { o2 = -v.perDay.o2, water = -v.perDay.water, food = -v.perDay.food }) + "  / " + _loc.T("life.per_day");
                }
                col.Add(_ui.L(cost, 22, true, GameUI.CText));
                if (!can) col.Add(_ui.L(_loc.T(why), 20, false, GameUI.CRed));
                row.Add(col);
                box.Add(row);
            }
            if (visitor) { var note = LbText(box, _loc.T("life.fiction"), 19, false, GameUI.CDim); note.style.marginTop = 8; }
        }

        /// <summary>Scanners at the hatch: each reading teaches what makes something alive.</summary>
        void LbBuildScans(VisualElement box, LifeCard card)
        {
            int left = _life.ScansLeft(card);
            var panel = new VisualElement();
            panel.style.backgroundColor = new Color(0.12f, 0.08f, 0.02f, 0.85f); GameUI.Border(panel, GameUI.CAmber, 2, 8);
            panel.style.paddingLeft = panel.style.paddingRight = 14; panel.style.paddingTop = 6; panel.style.paddingBottom = 6;
            panel.style.marginTop = 6; panel.style.flexShrink = 0;
            LbText(panel, _loc.T("life.scan.title", left), 21, true, GameUI.CAmber);
            if (_life.PetTrait == LC.extraScanTrait && _life.State.petFed) LbText(panel, _loc.T("life.scan.extra", LbName(LifeboatSim.Pet)), 19, false, GameUI.CCyan);
            if (left > 0)
            {
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap; row.style.flexShrink = 0;
                foreach (var sc in LC.scans)
                {
                    string id = sc;
                    var b = _ui.B(_loc.T($"life.scan.{id}.name"), () => LbScan(id), 21, false);
                    b.style.marginLeft = 0; b.style.marginRight = 10; b.style.marginTop = b.style.marginBottom = 3;
                    b.SetEnabled(_life.CanScan(card, id));
                    row.Add(b);
                }
                panel.Add(row);
            }
            foreach (var sc in _life.ScansDone(card))
            {
                bool odd = _life.IsOdd(card.visitor, sc);
                LbText(panel, _loc.T($"life.scan.{sc}.name") + ": " + _loc.T($"life.scan.{card.visitor}.{sc}") + "   " + _loc.T(odd ? "life.scan.odd" : "life.scan.normal"), 21, true, odd ? GameUI.CRed : GameUI.CGreen);
                LbText(panel, _loc.T($"life.scan.{sc}.how"), 17, false, GameUI.CDim);
            }
            if (left == 0 && _life.ScansDone(card).Count > 0) LbText(panel, _loc.T("life.scan.none_left"), 18, false, GameUI.CDim);
            box.Add(panel);
        }

        void LbBuildResult(VisualElement box)
        {
            var card = _lbCard;
            LbText(box, _loc.T("life.result_header"), 21, true, GameUI.CDim);
            _ui.Title(box, _loc.T($"life.card.{card.id}.title"));
            LbText(box, _loc.T("life.you_chose", _loc.T($"life.card.{card.id}.{_lbPicked.id}")), 23, true, GameUI.CAmber);
            var body = LbText(box, _loc.T($"life.card.{card.id}.{_lbPicked.id}.result"), 26);
            body.style.marginTop = 8; body.style.marginBottom = 8;
            if (_bRoot != null) { var hint = BOptionHint(_lbPicked, out bool perDay); box.Add(BDeltaIcons(hint, perDay, _lbPicked.health)); }
            else if (!_lbPicked.delta.IsZero || _lbPicked.health != 0) LbText(box, LbCost(_lbPicked), 26, true, GameUI.CCyan);
            var b = _ui.B(_loc.T("life.continue"), LbNextCard, 28, true);
            b.style.alignSelf = Align.FlexStart; b.style.marginLeft = 0; b.style.marginTop = 14;
            box.Add(b);
        }

        void LbBuildRation(VisualElement box)
        {
            var st = _life.State;
            _ui.Title(box, _loc.T("life.ration.title"));
            LbText(box, _loc.T("life.ration.body"), 23);
            foreach (var who in _life.Mouths())
            {
                string id = who;
                bool fed = _lbFed.Contains(id);
                var ration = _life.RationOf(id);
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 6;
                var name = _ui.L(LbName(id), 27, true, GameUI.CAmber); name.style.width = 300;
                row.Add(name);
                var b = _ui.B(_loc.T(fed ? "life.ration.fed" : "life.ration.skipped"), () =>
                {
                    if (!_lbFed.Remove(id)) _lbFed.Add(id);
                    LbShow();
                }, 24, fed);
                b.name = "ration-" + id;
                b.style.width = 260;
                row.Add(b);
                var cost = _ui.L(_loc.T("life.ration.cost", LbNum(ration.water), LbNum(ration.food)), 22, false, fed ? GameUI.CText : GameUI.CDim);
                cost.style.width = 240; cost.style.marginLeft = 10;
                row.Add(cost);
                if (!fed)
                    row.Add(_ui.L(_loc.T(id == LifeboatSim.Me ? "life.ration.skip_me" : id == LifeboatSim.Pet ? "life.ration.skip_pet" : "life.ration.skip_guest"), 21, false, GameUI.CRed));
                box.Add(row);
            }
            bool makerCan = _life.CanRunMaker;
            if (!makerCan) _lbMaker = false;
            if (LC.o2Maker.o2 > 0)
            {
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginTop = 14;
                var name = _ui.L(_loc.T("life.maker.name"), 24, true, GameUI.CCyan); name.style.width = 300;
                row.Add(name);
                var b = _ui.B(_loc.T(_lbMaker ? "life.maker.on" : "life.maker.off"), () => { _lbMaker = !_lbMaker; LbShow(); }, 24, _lbMaker);
                b.name = "ration-maker";
                b.style.width = 260;
                b.SetEnabled(makerCan);
                row.Add(b);
                var cost = _ui.L(_loc.T("life.maker.cost", LbNum(LC.o2Maker.power), LbNum(LC.o2Maker.o2)), 22, false, _lbMaker ? GameUI.CText : GameUI.CDim);
                cost.style.marginLeft = 10; cost.style.width = 240;
                row.Add(cost);
                if (!makerCan) row.Add(_ui.L(_loc.T("life.maker.no_power"), 21, false, GameUI.CRed));
                box.Add(row);
                LbText(box, _loc.T("life.maker.fact"), 19, false, GameUI.CDim);
            }
            var need = _life.RationNeed(_lbFed);
            bool run = _lbMaker && makerCan;
            float o2 = _life.O2TonightWith(run);
            float o2Have = st.stock.o2 + (run ? LC.o2Maker.o2 : 0);
            bool can = _life.CanFeed(_lbFed);
            var total = LbText(box, _loc.T("life.ration.total", LbNum(need.water), LbNum(st.stock.water), LbNum(need.food), LbNum(st.stock.food), LbNum(o2), LbNum(o2Have)), 24, true, can ? GameUI.CCyan : GameUI.CRed);
            total.style.marginTop = 12;
            if (st.flags.Contains("filter_clogged")) LbText(box, _loc.T("life.ration.o2_clog"), 21, false, GameUI.CAmber);
            if (!_life.PowerTonightWith(run)) LbText(box, _loc.T("life.ration.o2_power"), 21, false, GameUI.CAmber);
            if (o2 > o2Have) LbText(box, _loc.T("life.ration.o2_short"), 23, true, GameUI.CRed);
            if (!can) LbText(box, _loc.T("life.ration.cant"), 22, true, GameUI.CRed);
            var sleep = _ui.B(_loc.T("life.ration.confirm"), LbSleep, 28, true);
            sleep.style.alignSelf = Align.FlexStart; sleep.style.marginLeft = 0; sleep.style.marginTop = 12;
            sleep.SetEnabled(can);
            box.Add(sleep);
        }

        string LbNightLine(LifeLog e)
        {
            string pet = _life.State.pet != null ? _loc.T($"pet.{_life.State.pet}.name") : "";
            switch (e.kind)
            {
                case LifeLogKind.Hungry: return _loc.T("life.night.Hungry");
                case LifeLogKind.PetHungry: return _loc.T("life.night.PetHungry", pet);
                case LifeLogKind.NoPower: return _loc.T("life.night.NoPower");
                case LifeLogKind.Gift: return _loc.T("life.night.Gift." + e.id);
                case LifeLogKind.Theft:
                    return _loc.T("life.night.Theft", LbName(e.id), LbNum(e.value))
                        + (e.detail != null ? " " + _loc.T("life.night.Theft.item", _loc.T("life.item." + e.detail)) : "");
                case LifeLogKind.TheftStopped: return _loc.T("life.night.TheftStopped." + e.detail, LbName(e.id), pet);
                case LifeLogKind.GuestDeparted: return _loc.T("life.night.GuestDeparted", LbName(e.id));
                case LifeLogKind.GuestLeftHungry: return _loc.T("life.night.GuestLeftHungry", LbName(e.id));
                case LifeLogKind.MakerRan: return _loc.T("life.night.MakerRan", LbNum(e.value));
                case LifeLogKind.NightSafe: return _loc.T($"life.night.event.{e.id}.safe");
                case LifeLogKind.NightHit: return _loc.T($"life.night.event.{e.id}.hit");
                default: return null;
            }
        }

        static Color LbNightColor(LifeLogKind k) =>
            k == LifeLogKind.Gift || k == LifeLogKind.TheftStopped || k == LifeLogKind.NightSafe || k == LifeLogKind.MakerRan ? GameUI.CGreen
            : k == LifeLogKind.GuestLeftHungry || k == LifeLogKind.PetHungry ? GameUI.CAmber : GameUI.CRed;

        void LbBuildNight(VisualElement box)
        {
            _ui.Title(box, _loc.T("life.night.title", _lbNightDay));
            LbText(box, _loc.T("life.night.used", LbNum(_lbNightUsed.water), LbNum(_lbNightUsed.food), LbNum(_lbNightUsed.o2)), 23, false, GameUI.CDim);
            int shown = 0;
            foreach (var e in _lbNight)
            {
                string line = LbNightLine(e);
                if (line == null) continue;
                var l = LbText(box, line, 26, false, LbNightColor(e.kind));
                l.style.marginTop = 8;
                shown++;
            }
            if (shown == 0) { var l = LbText(box, _loc.T("life.night.calm"), 26, false, GameUI.CGreen); l.style.marginTop = 8; }
            var b = _ui.B(_loc.T("life.continue"), LbNextCard, 28, true);
            b.style.alignSelf = Align.FlexStart; b.style.marginLeft = 0; b.style.marginTop = 16;
            box.Add(b);
        }

        void LbBuildEnd(VisualElement box)
        {
            var st = _life.State;
            bool alive = st.outcome == LifeOutcome.Survived;
            if (st.outcome == LifeOutcome.Rescued || st.outcome == LifeOutcome.Stranded)
            {
                bool won = st.outcome == LifeOutcome.Rescued;
                LbText(box, _loc.T($"life.end.{st.outcome}.title"), 44, true, won ? GameUI.CCyan : GameUI.CAmber);
                LbText(box, won ? _loc.T("life.end.Rescued.body") : _loc.T("life.end.stranded." + _life.StrandedReason), 25);
                var stars = _life.Stars();
                int count = 0;
                for (int i = 0; i < stars.Length; i++)
                {
                    if (stars[i]) count++;
                    LbText(box, _loc.T(stars[i] ? "life.star.got" : "life.star.missed") + "  " + _loc.T("life.star." + (i + 1)), 23, stars[i], stars[i] ? GameUI.CAmber : GameUI.CDim);
                }
                if (st.guests.Count > 0)
                {
                    var names = new List<string>();
                    foreach (var g in st.guests) names.Add(LbName(g));
                    LbText(box, _loc.T("life.end.with", string.Join(", ", names)), 21, false, GameUI.CGreen);
                }
                if (won && _shotDir == null) Settings.RecordStars("lifeboat", count);
            }
            else if (alive)
            {
                LbText(box, _loc.T("life.end.Survived.title", st.day), 36, true, GameUI.CCyan);
                int left = _life.DaysOfSupplies();
                int needed = LC.daysTotal - st.day;
                LbText(box, left >= needed ? _loc.T("life.end.on_track", LC.daysTotal, left) : _loc.T("life.end.short", LC.daysTotal, left, needed), 26, true, left >= needed ? GameUI.CGreen : GameUI.CAmber);
            }
            else
            {
                LbText(box, _loc.T("life.end.failed.title"), 44, true, GameUI.CRed);
                LbText(box, _loc.T("life.end." + st.outcome, st.monsterLetIn != null ? LbName(st.monsterLetIn) : ""), 24);
                if (st.outcome == LifeOutcome.FailedMonster)
                {
                    var used = st.log.FindAll(e => e.kind == LifeLogKind.Scan && e.id == st.monsterLetIn);
                    var names = new List<string>();
                    foreach (var e in used) names.Add(_loc.T($"life.scan.{e.detail}.name") + " (" + _loc.T(e.value > 0 ? "life.scan.odd" : "life.scan.normal") + ")");
                    LbText(box, names.Count == 0 ? _loc.T("life.end.FailedMonster.noscan") : _loc.T("life.end.FailedMonster.scans", string.Join(", ", names)), 21, true, GameUI.CAmber);
                }
            }

            var head = LbText(box, _loc.T("life.end.choices"), 22, true, GameUI.CAmber);
            head.style.marginTop = 10;
            var cols = new VisualElement(); cols.style.flexDirection = FlexDirection.Row; cols.style.alignItems = Align.FlexStart;
            var colA = new VisualElement(); colA.style.width = 560; colA.style.marginRight = 20;
            var colB = new VisualElement(); colB.style.width = 560;
            cols.Add(colA); cols.Add(colB);
            var choices = st.log.FindAll(e => e.kind == LifeLogKind.Choice);
            int half = (choices.Count + 1) / 2;
            for (int i = 0; i < choices.Count; i++)
            {
                var e = choices[i];
                LbText(i < half ? colA : colB, _loc.T("life.end.choice_row", e.day, _loc.T($"life.card.{e.id}.title"), _loc.T($"life.card.{e.id}.{e.detail}")), 18);
            }
            box.Add(cols);
            foreach (var e in st.log)
            {
                if (e.kind != LifeLogKind.Theft && e.kind != LifeLogKind.TheftStopped && e.kind != LifeLogKind.NightHit && e.kind != LifeLogKind.NightSafe
                    && e.kind != LifeLogKind.Hungry && e.kind != LifeLogKind.GuestLeftHungry && e.kind != LifeLogKind.NoPower) continue;
                LbText(box, _loc.T("life.end.event_row", e.day, LbNightLine(e)), 19, false, LbNightColor(e.kind));
            }
            if (alive && LC.LastDay < LC.daysTotal)
            {
                var next = LbText(box, _loc.T("life.end.next", LC.LastDay + 1, LC.daysTotal), 21, false, GameUI.CDim);
                next.style.marginTop = 12;
            }
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginTop = 14;
            var again = _ui.B(_loc.T("life.end.again"), () => StartLifeboat(_lbStowed, _lbFixedPet), 28, true);
            again.style.marginLeft = 0;
            row.Add(again);
            row.Add(_ui.B(_loc.T("menu.sources"), ShowSources, 24));
            row.Add(_ui.B(_loc.T("end.title"), RestartToTitle, 26));
            box.Add(row);
        }
    }
}
