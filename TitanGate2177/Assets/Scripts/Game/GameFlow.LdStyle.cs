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
        static readonly Color LdCardBg = GameUI.CPanel2, LdCardHover = new Color32(52, 93, 82, 255);

        // ------------------------------------------------------------------ icon set (16 px line icons)

        static readonly Dictionary<string, Sprite> LdIcons = new Dictionary<string, Sprite>();

        internal static Sprite LdIcon(string id)
        {
            if (LdIcons.TryGetValue(id, out var s) && s != null) return s;
            var c = new PixelCanvas(16, 16);
            Color32 w = new Color32(255, 246, 225, 255);
            switch (id)
            {
                case "gauge": c.Disc(8, 8, 7, w); c.Disc(8, 8, 5, Pal.Clear); c.Line(8, 8, 11, 11, w); c.Disc(8, 8, 1, w); c.Set(4, 8, w); c.Set(8, 12, w); c.Set(8, 4, w); break;
                case "radio": c.Rect(3, 1, 10, 11, w); c.Rect(10, 11, 2, 5, w); c.Rect(5, 7, 6, 3, Pal.Clear); c.Rect(5, 3, 2, 2, Pal.Clear); c.Rect(9, 3, 2, 2, Pal.Clear); break;
                case "bag": c.Rect(1, 3, 14, 9, w); c.Frame(5, 11, 6, 4, w); c.HLine(2, 13, 7, Pal.Clear); c.Rect(6, 6, 4, 3, w); c.Set(8, 7, Pal.Clear); break;
                case "bolt": for (int y = 1; y < 8; y++) c.HLine(9 - y / 2, 9 + y / 2, y, w); for (int y = 7; y < 16; y++) c.HLine(3 + (y - 7) / 2, Mathf.Max(8, 14 - (y - 7)), y, w); break;
                case "moon": c.Disc(7, 8, 7, w); c.Disc(11, 11, 6, Pal.Clear); break;
                case "check": c.Line(2, 7, 6, 3, w); c.Line(2, 8, 6, 4, w); c.Line(6, 3, 13, 12, w); c.Line(6, 4, 13, 13, w); break;
                case "marker": c.Frame(3, 3, 10, 10, w); break;
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
                case "can": c.Frame(4, 2, 8, 12, w); c.HLine(5, 10, 11, w); c.HLine(5, 10, 5, w); c.Rect(6, 7, 4, 2, w); break;
                case "play": for (int i = 0; i < 6; i++) c.VLine(4 + i, 2 + i, 13 - i, w); c.Line(4, 2, 4, 13, w); break;
                case "continue": c.Line(3, 2, 3, 13, w); for (int i = 0; i < 5; i++) c.VLine(6 + i, 3 + i, 12 - i, w); c.Line(12, 2, 12, 13, w); break;
                case "help": c.Disc(8, 8, 6, w); c.Disc(8, 8, 5, Pal.Clear); c.HLine(6, 9, 11, w); c.VLine(10, 9, 11, w); c.HLine(8, 10, 8, w); c.VLine(8, 6, 8, w); c.Set(8, 4, w); break;
                case "gear": c.Disc(8, 8, 5, w); c.Disc(8, 8, 2, Pal.Clear); for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; c.Rect(8 + Mathf.RoundToInt(Mathf.Cos(a) * 6) - 1, 8 + Mathf.RoundToInt(Mathf.Sin(a) * 6) - 1, 2, 2, w); } break;
                case "book": c.Frame(2, 3, 6, 10, w); c.Frame(8, 3, 6, 10, w); c.HLine(3, 6, 9, w); c.HLine(3, 6, 7, w); c.HLine(9, 12, 9, w); c.HLine(9, 12, 7, w); break;
                case "source": c.Disc(8, 8, 2, w); for (int i = 0; i < 3; i++) { float a = i * Mathf.PI / 3; for (float k = -6; k <= 6; k += 0.5f) c.Set(8 + Mathf.RoundToInt(Mathf.Cos(a) * k), 8 + Mathf.RoundToInt(Mathf.Sin(a) * k * 0.45f), w); } break;
                case "star": c.Line(8, 15, 5, 5, w); c.Line(8, 15, 11, 5, w); c.Line(1, 10, 15, 10, w); c.Line(1, 10, 11, 5, w); c.Line(15, 10, 5, 5, w); break;
                case "exit": c.Frame(2, 2, 7, 12, w); c.HLine(7, 14, 8, w); c.Line(14, 8, 11, 5, w); c.Line(14, 8, 11, 11, w); break;
                case "lang": c.Disc(8, 8, 6, w); c.Disc(8, 8, 5, Pal.Clear); c.HLine(2, 14, 8, w); c.VLine(8, 2, 14, w); c.Line(5, 3, 5, 13, w); c.Line(11, 3, 11, 13, w); break;
                case "water": c.Disc(8, 5, 4, w); c.Disc(8, 5, 3, Pal.Clear); c.Line(4, 6, 8, 14, w); c.Line(12, 6, 8, 14, w); c.Set(6, 5, w); break;
                case "sun": c.Disc(8, 8, 3, w); c.Disc(8, 8, 2, Pal.Clear); c.VLine(8, 1, 3, w); c.VLine(8, 13, 15, w); c.HLine(1, 3, 8, w); c.HLine(13, 15, 8, w); c.Set(3, 3, w); c.Set(13, 13, w); c.Set(13, 3, w); c.Set(3, 13, w); break;
                case "plant": c.VLine(8, 1, 10, w); c.Line(8, 6, 4, 9, w); c.Line(4, 9, 3, 12, w); c.Line(8, 8, 12, 11, w); c.Line(12, 11, 13, 13, w); c.HLine(4, 12, 1, w); c.Line(4, 1, 5, 0, w); break;
                case "wrench": c.Line(3, 3, 11, 11, w); c.Line(4, 3, 12, 11, w); c.Frame(10, 10, 4, 4, w); c.Set(2, 4, w); c.Set(4, 2, w); break;
                default: c.Frame(3, 3, 10, 10, w); break;
            }
            s = c.ToSprite(new Vector2(.5f, .5f), 16);
            s.texture.filterMode = FilterMode.Point;
            LdIcons[id] = s;
            return s;
        }

        static string LdActionIcon(string a)
            => a.StartsWith("service_") ? "wrench" : a == "ice" ? "water" : a == "clean_panels" ? "sun" : a == "radio_antenna" || a == "build_beacon" ? "signal"
             : a.StartsWith("repair") || a == "assemble_rover" || a == "cannibalize" ? "parts" : a.StartsWith("send") && a != "send_rover" || a.StartsWith("dip_") ? "signal"
             : a.StartsWith("explore") ? "walk" : a.Contains("rover") ? "rover" : a == "search" ? "parts" : a == "end_day" ? "time" : a == "use_medkit" ? "medkit"
             : a == "prepare_shelter" ? "storm" : a == "open_mystery" || a == "raid_food" ? "food" : a == "absorber" ? "can" : a == "shelter" || a == "backup_fan" ? "co2"
             : a == "shed_load" ? "heat" : a == "raid_battery" ? "battery" : a == "low_activity" ? "o2" : "time";

        Image LdIconImage(string id, float size, Color tint)
        {
            var img = new Image { sprite = LdIcon(id), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            img.style.width = size; img.style.height = size; img.tintColor = tint; img.style.flexShrink = 0;
            return img;
        }

        // ------------------------------------------------------------------ panel header and action cards

        Label LdHeader(VisualElement p, string title)
        {
            _ldCardIndex=0; p.schedule.Execute(()=>{if(p.Q<ScrollView>()==null)OpPanel(p);}).StartingIn(1);
            var head=OpRow();head.style.marginBottom=12;head.Add(OpIcon("gear",46));
            var titleLabel=_ui.L(title,38,true,FieldBook.Paper);titleLabel.style.marginLeft=12;titleLabel.style.flexShrink=1;titleLabel.style.whiteSpace=WhiteSpace.Normal;head.Add(titleLabel);p.Add(head);return titleLabel;
        }

        int _ldCardIndex;

        /// <summary>Day and work units as two chips (instead of a sentence).</summary>
        VisualElement LdDayChips()
        {
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginBottom = 8;
            void C(string icon, string text, Color col)
            {
                var c = new VisualElement(); c.style.flexDirection = FlexDirection.Row; c.style.alignItems = Align.Center; c.style.marginRight = 10;
                c.Add(LdIconImage(icon, 22, col)); var l = _ui.L(" " + text, 22, true, col); LdFont(l); c.Add(l); row.Add(c);
            }
            C("day", $"{_ld.Day}/{_ld.C.days}", GameUI.CAmber);
            C("time", new string('●', Mathf.Max(0, _ld.WorkUnits)) + new string('○', Mathf.Max(0, _ld.C.workUnits - _ld.WorkUnits)), GameUI.CCyan);
            return row;
        }

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
            iconBox.Add(OpIcon(can ? LdActionIcon(a) : "lock", 48));
            card.Add(iconBox);
            var mid = new VisualElement(); mid.style.marginLeft = 14; mid.style.flexGrow = 1; mid.style.flexShrink = 1;
            var title = _ui.L(name, 24, true, can ? GameUI.CText : GameUI.CDim); title.style.whiteSpace = WhiteSpace.Normal; LdFont(title);
            mid.Add(title);
            var eff = LdPara(effect, can ? GameUI.CDim : new Color(0.6f, 0.6f, 0.62f), 18);
            bool random = a.StartsWith("explore") || a == "send_rover" || a == "open_mystery";
            bool service = a.StartsWith("service_");
            if (!random && !service) eff.style.display = DisplayStyle.None;
            if (can && service) { eff.text=GameUI.Thai(effect); mid.Add(eff); }
            if (can && service) eff.style.color = GameUI.CGreen;
            else if (can && !random) mid.Add(LdDeltaRow(_ld.Preview(a), 20));   // what changes, as icons
            else mid.Add(LdPara(can ? TT("Random find", "สุ่มของที่พบ") : LdBlockedText(why), can ? GameUI.CAmber : GameUI.CRed, 18));
            card.Add(mid);
            // the cost on the right: one turn, the rest of the day, or nothing (answers to an event)
            bool free = a.StartsWith("dip_") || a.StartsWith("raid_");
            var cost = new VisualElement(); cost.style.alignItems = Align.Center; cost.style.marginLeft = 12; cost.style.minWidth = 70;
            cost.Add(OpIcon("time",28));
            cost.Add(_ui.L(free ? TT("free", "ฟรี") : a == "end_day" ? TT("all day", "ทั้งวัน") : TT("1", "1"), 17, true, free ? GameUI.CDim : GameUI.CCyan));
            card.Add(cost);
            if (can)
            {
                card.RegisterCallback<MouseEnterEvent>(_ => { card.style.backgroundColor = LdCardHover; _sfx.Play("hover"); });
                card.RegisterCallback<MouseLeaveEvent>(_ => { card.style.backgroundColor = LdCardBg; });
                card.RegisterCallback<FocusInEvent>(_ => GameUI.Border(card,FieldBook.Amber,3,9));
                card.RegisterCallback<FocusOutEvent>(_ => GameUI.Border(card,FieldBook.Rim,1,9));
                GameUI.BindCard(card, () => { _sfx.Play("click"); LdDo(a); });
            }
            card.tooltip=effect;
            if(!can){card.focusable=true;card.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.Return)UiMotion.Shake(card);});}
            LdAnimateIn(card, ++_ldCardIndex);
            return card;
        }

        /// <summary>Slide and fade in, one after another (motion off with Reduced Motion).</summary>
        void LdAnimateIn(VisualElement e, int index)
        {
            UiMotion.FadeSlideIn(e,index);
        }
    }
}
