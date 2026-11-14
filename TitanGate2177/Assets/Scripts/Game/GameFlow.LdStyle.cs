// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do dark panels with thin gold frames, a line-icon set, chips in the HUD and action cards that slide in
//           make choices faster to read for 7-12 year olds (refs: ornate RPG panels, gold card picks, line icon sets)?
// Date: 2026-11-15
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Res = TitanGate.Core.LastDock.Res;

namespace TitanGate.Game
{
    /// <summary>LAST DOCK look: the icon set, HUD chips, panel headers and action cards. Owns no game rules.</summary>
    public partial class GameFlow
    {
        static readonly Color LdGold = new Color(0.95f, 0.78f, 0.42f), LdGoldDim = new Color(0.95f, 0.78f, 0.42f, 0.35f);
        static readonly Color LdCardBg = new Color(0.07f, 0.08f, 0.12f, 0.96f), LdCardHover = new Color(0.13f, 0.14f, 0.2f, 0.98f);

        // ------------------------------------------------------------------ icon set (16 px line icons)

        static readonly Dictionary<string, Sprite> LdIcons = new Dictionary<string, Sprite>();

        static Sprite LdIcon(string id)
        {
            if (LdIcons.TryGetValue(id, out var s) && s != null) return s;
            var c = new PixelCanvas(16, 16);
            Color32 w = new Color32(255, 246, 225, 255);
            switch (id)
            {
                case "food": c.HLine(2, 13, 7, w); c.Line(2, 7, 4, 3, w); c.Line(13, 7, 11, 3, w); c.HLine(4, 11, 3, w); c.Line(6, 9, 5, 12, w); c.Line(9, 9, 10, 12, w); break;
                case "o2": c.Frame(5, 1, 6, 11, w); c.HLine(6, 9, 12, w); c.Rect(7, 13, 2, 2, w); c.HLine(6, 9, 6, w); break;
                case "battery": c.Frame(1, 4, 13, 8, w); c.Rect(14, 6, 1, 4, w); c.Line(8, 10, 6, 7, w); c.HLine(6, 9, 7, w); c.Line(9, 7, 7, 5, w); break;
                case "parts": c.Line(3, 3, 11, 11, w); c.Line(4, 3, 12, 11, w); c.Frame(10, 10, 4, 4, w); c.Frame(1, 1, 4, 4, w); break;
                case "co2": c.Disc(5, 7, 3, w); c.Disc(10, 8, 4, w); c.Disc(5, 7, 2, Pal.Clear); c.Disc(10, 8, 3, Pal.Clear); c.HLine(2, 14, 4, w); break;
                case "heat": c.Frame(6, 5, 3, 9, w); c.Disc(7, 3, 2, w); c.Rect(7, 6, 1, 6, w); c.HLine(10, 12, 12, w); c.HLine(10, 12, 9, w); break;
                case "signal": c.VLine(8, 1, 9, w); c.Rect(7, 9, 3, 1, w); c.Line(4, 10, 6, 14, w); c.Line(12, 10, 10, 14, w); c.Line(4, 4, 5, 2, w); c.Line(12, 4, 11, 2, w); break;
                case "heart": c.Disc(5, 9, 3, w); c.Disc(10, 9, 3, w); for (int i = 0; i < 6; i++) c.HLine(2 + i, 13 - i, 8 - i, w); break;
                case "time": c.Disc(8, 8, 6, w); c.Disc(8, 8, 5, Pal.Clear); c.VLine(8, 8, 12, w); c.HLine(8, 11, 8, w); break;
                case "rescue": c.Line(8, 15, 3, 4, w); c.Line(8, 15, 13, 4, w); c.HLine(3, 13, 4, w); c.Line(6, 4, 4, 1, w); c.Line(10, 4, 12, 1, w); break;
                case "day": c.Frame(2, 2, 12, 11, w); c.HLine(2, 13, 10, w); c.Rect(5, 13, 1, 2, w); c.Rect(10, 13, 1, 2, w); c.Rect(5, 4, 2, 2, w); c.Rect(9, 4, 2, 2, w); break;
                case "storm": c.Disc(8, 8, 3, w); c.VLine(8, 0, 2, w); c.VLine(8, 13, 15, w); c.HLine(0, 2, 8, w); c.HLine(13, 15, 8, w); c.Line(2, 2, 4, 4, w); c.Line(13, 13, 11, 11, w); c.Line(13, 2, 11, 4, w); c.Line(2, 13, 4, 11, w); break;
                case "rover": c.Frame(2, 7, 12, 4, w); c.Disc(4, 4, 2, w); c.Disc(8, 4, 2, w); c.Disc(12, 4, 2, w); c.VLine(11, 11, 14, w); c.HLine(10, 13, 14, w); break;
                case "walk": c.Disc(8, 13, 2, w); c.Line(8, 11, 8, 6, w); c.Line(8, 6, 5, 1, w); c.Line(8, 6, 11, 1, w); c.Line(8, 10, 5, 7, w); c.Line(8, 10, 11, 7, w); break;
                case "medkit": c.Frame(2, 3, 12, 10, w); c.Rect(7, 5, 2, 6, w); c.Rect(5, 7, 6, 2, w); break;
                default: c.Frame(3, 3, 10, 10, w); break;
            }
            s = c.ToSprite(new Vector2(.5f, .5f), 16);
            s.texture.filterMode = FilterMode.Point;
            LdIcons[id] = s;
            return s;
        }

        static string LdActionIcon(string a)
            => a.StartsWith("repair") || a == "assemble_rover" || a == "cannibalize" ? "parts" : a.StartsWith("send") && a != "send_rover" || a.StartsWith("dip_") ? "signal"
             : a.StartsWith("explore") ? "walk" : a.Contains("rover") ? "rover" : a == "search" ? "parts" : a == "end_day" ? "time" : a == "use_medkit" ? "medkit"
             : a == "prepare_shelter" ? "storm" : a == "open_mystery" || a == "raid_food" ? "food" : a == "absorber" || a == "shelter" || a == "backup_fan" ? "co2"
             : a == "shed_load" ? "heat" : a == "raid_battery" ? "battery" : a == "low_activity" ? "o2" : "time";

        Image LdIconImage(string id, float size, Color tint)
        {
            var img = new Image { sprite = LdIcon(id), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            img.style.width = size; img.style.height = size; img.tintColor = tint; img.style.flexShrink = 0;
            return img;
        }

        // ------------------------------------------------------------------ HUD chips

        readonly Dictionary<string, (VisualElement chip, Label value, Image icon)> _ldChips = new Dictionary<string, (VisualElement, Label, Image)>();

        VisualElement LdChipRow()
        {
            _ldChips.Clear();
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap; row.style.justifyContent = Justify.Center;
            foreach (var id in new[] { "day", "time", "food", "o2", "battery", "parts", "co2", "heat", "signal", "heart", "rescue" })
            {
                var chip = new VisualElement { pickingMode = PickingMode.Ignore };
                chip.style.flexDirection = FlexDirection.Row; chip.style.alignItems = Align.Center;
                chip.style.backgroundColor = new Color(0.04f, 0.05f, 0.09f, 0.88f);
                GameUI.Border(chip, LdGoldDim, 1, 7);
                chip.style.paddingLeft = 8; chip.style.paddingRight = 10; chip.style.paddingTop = 3; chip.style.paddingBottom = 3;
                chip.style.marginLeft = 3; chip.style.marginRight = 3; chip.style.marginBottom = 4;
                var icon = LdIconImage(id, 22, GameUI.CText);
                var val = _ui.L("", 21, true, GameUI.CText); val.pickingMode = PickingMode.Ignore; val.style.marginLeft = 6;
                chip.Add(icon); chip.Add(val); row.Add(chip);
                _ldChips[id] = (chip, val, icon);
            }
            return row;
        }

        void LdChip(string id, string text, Color color, bool alarm = false)
        {
            if (!_ldChips.TryGetValue(id, out var c)) return;
            c.value.text = text; c.value.style.color = color; c.icon.tintColor = color;
            float pulse = alarm && !Settings.ReducedMotion ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f) : 0f;
            c.chip.style.backgroundColor = alarm ? Color.Lerp(new Color(0.25f, 0.03f, 0.05f, 0.92f), new Color(0.45f, 0.05f, 0.08f, 0.95f), pulse) : new Color(0.04f, 0.05f, 0.09f, 0.88f);
        }

        // ------------------------------------------------------------------ panel header and action cards

        Label LdHeader(VisualElement p, string title)
        {
            _ldCardIndex = 0;
            var head = new VisualElement(); head.style.marginBottom = 10;
            var t = _ui.L(title, 36, true, LdGold); _ui.DisplayFont(t);
            head.Add(t);
            var rule = new VisualElement(); rule.style.height = 2; rule.style.marginTop = 4; rule.style.backgroundColor = LdGoldDim;
            var gem = new VisualElement(); gem.style.width = 10; gem.style.height = 10; gem.style.backgroundColor = LdGold; gem.style.rotate = new Rotate(new Angle(45f));
            gem.style.position = Position.Absolute; gem.style.left = 0; gem.style.top = -4;
            rule.Add(gem); head.Add(rule);
            p.Add(head);
            LdAnimateIn(head, 0);
            return t;
        }

        int _ldCardIndex;

        VisualElement LdCard(string a)
        {
            bool can = _ld.Can(a, out var why);
            var (name, effect) = LdAction(a);
            var card = new VisualElement();
            card.style.flexDirection = FlexDirection.Row; card.style.alignItems = Align.Center;
            card.style.backgroundColor = LdCardBg; card.style.marginTop = 7;
            card.style.paddingLeft = 12; card.style.paddingRight = 14; card.style.paddingTop = 8; card.style.paddingBottom = 8;
            GameUI.Border(card, can ? LdGoldDim : new Color(1, 1, 1, 0.08f), 1, 9);
            card.style.borderLeftWidth = 5; card.style.borderLeftColor = can ? LdGold : new Color(0.5f, 0.5f, 0.55f, 0.6f);
            var iconBox = new VisualElement(); iconBox.style.width = 48; iconBox.style.height = 48; iconBox.style.alignItems = Align.Center; iconBox.style.justifyContent = Justify.Center;
            iconBox.style.backgroundColor = new Color(1, 1, 1, 0.05f); GameUI.Border(iconBox, LdGoldDim, 1, 24);
            iconBox.Add(LdIconImage(LdActionIcon(a), 30, can ? LdGold : GameUI.CDim));
            card.Add(iconBox);
            var mid = new VisualElement(); mid.style.marginLeft = 14; mid.style.flexGrow = 1; mid.style.flexShrink = 1;
            var title = _ui.L(name, 24, true, can ? GameUI.CText : GameUI.CDim); title.style.whiteSpace = WhiteSpace.Normal;
            mid.Add(title);
            mid.Add(LdPara(effect, can ? GameUI.CDim : new Color(0.6f, 0.6f, 0.62f), 19));
            mid.Add(LdPara(can ? "→ " + LdPreviewText(a) : TT("Can't: ", "ทำไม่ได้: ") + LdBlockedText(why), can ? GameUI.CGreen : GameUI.CRed, 18));
            card.Add(mid);
            // the cost on the right: one turn, the rest of the day, or nothing (answers to an event)
            bool free = a.StartsWith("dip_") || a.StartsWith("raid_");
            var cost = new VisualElement(); cost.style.alignItems = Align.Center; cost.style.marginLeft = 12; cost.style.minWidth = 70;
            cost.Add(LdIconImage("time", 26, free ? GameUI.CDim : GameUI.CCyan));
            cost.Add(_ui.L(free ? TT("free", "ฟรี") : a == "end_day" ? TT("all day", "ทั้งวัน") : TT("1 turn", "1 ช่วง"), 17, true, free ? GameUI.CDim : GameUI.CCyan));
            card.Add(cost);
            if (can)
            {
                card.RegisterCallback<MouseEnterEvent>(_ => { card.style.backgroundColor = LdCardHover; _sfx.Play("hover"); });
                card.RegisterCallback<MouseLeaveEvent>(_ => card.style.backgroundColor = LdCardBg);
                card.RegisterCallback<ClickEvent>(_ => { _sfx.Play("click"); LdDo(a); });
            }
            LdAnimateIn(card, ++_ldCardIndex);
            return card;
        }

        /// <summary>Slide and fade in, one after another (motion off with Reduced Motion).</summary>
        void LdAnimateIn(VisualElement e, int index)
        {
            if (Settings.ReducedMotion) return;
            e.style.opacity = 0; e.style.translate = new Translate(0, 14);
            e.style.transitionProperty = new List<StylePropertyName> { "opacity", "translate" };
            e.style.transitionDuration = new List<TimeValue> { new TimeValue(0.22f, TimeUnit.Second), new TimeValue(0.22f, TimeUnit.Second) };
            e.style.transitionDelay = new List<TimeValue> { new TimeValue(0.04f * index, TimeUnit.Second), new TimeValue(0.04f * index, TimeUnit.Second) };
            e.schedule.Execute(() => { e.style.opacity = 1; e.style.translate = new Translate(0, 0); }).StartingIn(16);
        }
    }
}
