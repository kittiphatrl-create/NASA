// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System;
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// All screen UI, built in code with UI Toolkit. Text uses the Advanced text generator
    /// (HarfBuzz shaping) so Thai vowels and tone marks sit in the right place.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public static readonly Color CPanel = new Color(0.04f, 0.06f, 0.13f, 0.92f);
        public static readonly Color CPanel2 = new Color(0.09f, 0.13f, 0.25f, 0.96f);
        public static readonly Color CCyan = new Color(0.26f, 0.89f, 1f);
        public static readonly Color CCyanD = new Color(0.10f, 0.55f, 0.69f);
        public static readonly Color CText = new Color(0.92f, 0.95f, 1f);
        public static readonly Color CDim = new Color(0.62f, 0.68f, 0.82f);
        public static readonly Color CAmber = new Color(1f, 0.75f, 0.25f);
        public static readonly Color CRed = new Color(1f, 0.33f, 0.38f);
        public static readonly Color CGreen = new Color(0.36f, 0.89f, 0.54f);

        UIDocument _doc;
        Localization _loc;
        Font _font, _fontBold;
        public Sfx sfx;

        VisualElement _incident, _incidentFill, _cine;
        Label _caption;
        Button _skip;
        public event Action SkipClicked;

        /// <summary>Cutscene mode: letterbox bars, subtitle and a skip button; the HUD is hidden.</summary>
        public void SetCinematic(bool on)
        {
            _cine.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            if (on) { _cine.BringToFront(); _fader.BringToFront(); _skip.text = _loc.T("ui.skip"); }
            _caption.text = "";
        }

        public void SetCaption(string text)
        {
            _caption.text = text ?? "";
            _caption.style.opacity = 0;
            _caption.schedule.Execute(() => _caption.style.opacity = 1).StartingIn(60);
        }
        Label _incidentLabel;
        VisualElement _root, _hud, _topLeft, _topRight, _resRow, _forecast, _invBar, _hover, _dialog, _dialogBlocker, _modalLayer, _toastBox, _fader, _flash, _screen;
        Label _objective, _chapterLabel, _hoverLabel, _dialogSpeaker, _dialogText, _dialogMore, _forecastLabel, _invHint;
        Button _btnCards, _btnHint, _btnLang;
        Image _dialogPortrait;

        // dialogue
        struct Line { public string speaker, key; public object[] args; public Action done; }
        readonly Queue<Line> _lines = new Queue<Line>();
        Line _current;
        bool _lineOpen;
        float _lineShownAt;

        // modal
        Action<VisualElement> _modalBuild;
        Action _modalClosed;
        bool _modalClosable;

        public event Action CardsClicked, HintClicked, LangClicked;
        public event Action<string> ItemClicked;

        public bool DialogOpen => _lineOpen;
        public bool ModalOpen => _modalBuild != null;
        public bool ScreenOpen => _screen != null && _screen.style.display == DisplayStyle.Flex;
        public bool Blocking => DialogOpen || ModalOpen || ScreenOpen;
        public VisualElement Root => _root;
        public IPanel Panel => _root?.panel;

        public void Init(Localization loc)
        {
            _loc = loc;
            _font = Resources.Load<Font>("Fonts/ChakraPetch-Regular");
            _fontBold = Resources.Load<Font>("Fonts/ChakraPetch-Bold");

            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            ps.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/Runtime");
            ps.clearColor = false;
            _doc = gameObject.AddComponent<UIDocument>();
            _doc.panelSettings = ps;
            Build();
        }

        // ------------------------------------------------------------------ helpers

        public void Txt(VisualElement e, bool bold = false)
        {
            e.style.unityFontDefinition = FontDefinition.FromFont(bold ? _fontBold : _font);
            e.style.unityTextGenerator = TextGeneratorType.Advanced;
            e.style.color = CText;
        }

        public Label L(string text, int size = 26, bool bold = false, Color? color = null)
        {
            var l = new Label(text);
            Txt(l, bold);
            l.style.fontSize = Mathf.Round(size * Settings.TextScale);
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginTop = l.style.marginBottom = 0;
            l.style.paddingTop = 2; l.style.paddingBottom = 2;
            if (color.HasValue) l.style.color = color.Value;
            l.pickingMode = PickingMode.Ignore;
            return l;
        }

        readonly List<(string text, Button b, Action act)> _pressable = new List<(string, Button, Action)>();

        /// <summary>Verification hook: press the newest enabled button whose label contains text.</summary>
        public bool Press(string text)
        {
            for (int i = _pressable.Count - 1; i >= 0; i--)
            {
                var (t, b, act) = _pressable[i];
                if (b.panel == null || !b.enabledInHierarchy || t == null || !t.Contains(text)) continue;
                act?.Invoke();
                return true;
            }
            return false;
        }

        public Button B(string text, Action onClick, int size = 26, bool primary = false)
        {
            if (_pressable.Count > 400) _pressable.RemoveRange(0, 200);
            var b = new Button(() => { sfx?.Play("click"); onClick?.Invoke(); }) { text = text };
            b.RegisterCallback<MouseEnterEvent>(_ => { if (b.enabledSelf) sfx?.Play("hover"); });
            Txt(b, true);
            b.style.fontSize = Mathf.Round(size * Settings.TextScale);
            b.style.whiteSpace = WhiteSpace.Normal;
            b.style.backgroundColor = primary ? new Color(0.10f, 0.45f, 0.58f) : CPanel2;
            b.style.color = CText;
            Border(b, primary ? CCyan : CCyanD, 2, 6);
            b.style.paddingLeft = b.style.paddingRight = 18;
            b.style.paddingTop = b.style.paddingBottom = 8;
            b.style.marginTop = b.style.marginBottom = 5;
            b.style.marginLeft = b.style.marginRight = 5;
            b.RegisterCallback<MouseEnterEvent>(_ => { if (b.enabledSelf) b.style.backgroundColor = new Color(0.14f, 0.32f, 0.5f); });
            b.RegisterCallback<MouseLeaveEvent>(_ => b.style.backgroundColor = primary ? new Color(0.10f, 0.45f, 0.58f) : CPanel2);
            _pressable.Add((text, b, () => { sfx?.Play("click"); onClick?.Invoke(); }));
            return b;
        }

        public static void Border(VisualElement e, Color c, float w, float r)
        {
            e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = c;
            e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = w;
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r;
        }

        static void Pad(VisualElement e, float p)
        {
            e.style.paddingTop = e.style.paddingBottom = e.style.paddingLeft = e.style.paddingRight = p;
        }

        static VisualElement Abs(VisualElement e)
        {
            e.style.position = Position.Absolute;
            return e;
        }

        static VisualElement Full(VisualElement e)
        {
            Abs(e);
            e.style.left = e.style.top = e.style.right = e.style.bottom = 0;
            return e;
        }

        // ------------------------------------------------------------------ build

        void Build()
        {
            _root = _doc.rootVisualElement;
            _root.Clear();
            _root.pickingMode = PickingMode.Ignore;
            Txt(_root);

            _hud = Full(new VisualElement());
            _hud.pickingMode = PickingMode.Ignore;
            _root.Add(_hud);

            // top-left: chapter + objective
            _topLeft = Abs(new VisualElement());
            _topLeft.style.left = 20; _topLeft.style.top = 16; _topLeft.style.width = 560;
            _topLeft.style.backgroundColor = CPanel; Border(_topLeft, CCyanD, 2, 8); Pad(_topLeft, 14);
            _topLeft.pickingMode = PickingMode.Ignore;
            _chapterLabel = L("", 22, true, CCyan);
            _objective = L("", 24);
            _topLeft.Add(_chapterLabel);
            _topLeft.Add(_objective);
            _hud.Add(_topLeft);

            // top-right: buttons
            _topRight = Abs(new VisualElement());
            _topRight.style.right = 16; _topRight.style.top = 16;
            _topRight.style.flexDirection = FlexDirection.Row;
            _btnCards = B("", () => CardsClicked?.Invoke(), 24);
            _btnHint = B("", () => HintClicked?.Invoke(), 24);
            _btnLang = B("", () => LangClicked?.Invoke(), 24, true);
            _topRight.Add(_btnCards); _topRight.Add(_btnHint); _topRight.Add(_btnLang);
            _hud.Add(_topRight);

            // resources row (Mars only)
            _resRow = Abs(new VisualElement());
            _resRow.style.left = 600; _resRow.style.right = 560; _resRow.style.top = 16;
            _resRow.style.flexDirection = FlexDirection.Row; _resRow.style.flexWrap = Wrap.Wrap;
            _resRow.style.justifyContent = Justify.Center;
            _resRow.pickingMode = PickingMode.Ignore;
            _hud.Add(_resRow);

            _forecast = Abs(new VisualElement());
            _forecast.style.right = 16; _forecast.style.top = 92; _forecast.style.width = 520;
            _forecast.style.backgroundColor = CPanel; Border(_forecast, CAmber, 2, 8); Pad(_forecast, 12);
            _forecast.pickingMode = PickingMode.Ignore;
            _forecastLabel = L("", 22);
            _forecast.Add(_forecastLabel);
            _hud.Add(_forecast);

            // inventory bar
            _invBar = Abs(new VisualElement());
            _invBar.style.left = 0; _invBar.style.right = 0; _invBar.style.bottom = 14;
            _invBar.style.flexDirection = FlexDirection.Row; _invBar.style.justifyContent = Justify.Center;
            _invBar.pickingMode = PickingMode.Ignore;
            _hud.Add(_invBar);
            _invHint = L("", 20, false, CDim);
            Abs(_invHint); _invHint.style.bottom = 104; _invHint.style.left = 0; _invHint.style.right = 0;
            _invHint.style.unityTextAlign = TextAnchor.MiddleCenter;
            _hud.Add(_invHint);

            // hover label
            _hover = Abs(new VisualElement());
            _hover.style.backgroundColor = CPanel; Border(_hover, CCyan, 2, 6);
            _hover.style.paddingLeft = _hover.style.paddingRight = 10; _hover.style.paddingTop = _hover.style.paddingBottom = 2;
            _hover.pickingMode = PickingMode.Ignore;
            _hoverLabel = L("", 24, true, CCyan);
            _hover.Add(_hoverLabel);
            _hover.style.display = DisplayStyle.None;
            _root.Add(_hover);

            // dialogue
            _dialogBlocker = Full(new VisualElement());
            _dialogBlocker.style.display = DisplayStyle.None;
            _dialogBlocker.RegisterCallback<PointerDownEvent>(_ => Advance());
            _root.Add(_dialogBlocker);
            _dialog = Abs(new VisualElement());
            _dialog.style.left = 260; _dialog.style.right = 260; _dialog.style.bottom = 130;
            _dialog.style.backgroundColor = CPanel; Border(_dialog, CCyan, 3, 10); Pad(_dialog, 18);
            _dialog.style.flexDirection = FlexDirection.Row;
            _dialog.pickingMode = PickingMode.Ignore;
            _dialogPortrait = new Image { scaleMode = ScaleMode.ScaleToFit };
            _dialogPortrait.style.width = 96; _dialogPortrait.style.height = 96; _dialogPortrait.style.marginRight = 18;
            _dialogPortrait.pickingMode = PickingMode.Ignore;
            _dialog.Add(_dialogPortrait);
            var col = new VisualElement { pickingMode = PickingMode.Ignore };
            col.style.flexGrow = 1; col.style.flexShrink = 1;
            _dialogSpeaker = L("", 24, true, CCyan);
            _dialogText = L("", 28);
            _dialogMore = L("", 18, false, CDim);
            _dialogMore.style.unityTextAlign = TextAnchor.LowerRight;
            col.Add(_dialogSpeaker); col.Add(_dialogText); col.Add(_dialogMore);
            _dialog.Add(col);
            _dialog.style.display = DisplayStyle.None;
            _dialogBlocker.Add(_dialog);

            // modal
            _modalLayer = Full(new VisualElement());
            _modalLayer.style.backgroundColor = new Color(0, 0, 0, 0.55f);
            _modalLayer.style.alignItems = Align.Center; _modalLayer.style.justifyContent = Justify.Center;
            _modalLayer.style.display = DisplayStyle.None;
            _root.Add(_modalLayer);

            // full screens (title / end)
            _screen = Full(new VisualElement());
            _screen.style.display = DisplayStyle.None;
            _screen.style.alignItems = Align.Center; _screen.style.justifyContent = Justify.Center;
            _root.Add(_screen);

            // real-time emergency banner
            _incident = Abs(new VisualElement());
            _incident.style.left = 380; _incident.style.right = 380; _incident.style.top = 150;
            _incident.style.backgroundColor = new Color(0.35f, 0.04f, 0.08f, 0.95f); Border(_incident, CRed, 3, 10); Pad(_incident, 12);
            _incident.pickingMode = PickingMode.Ignore;
            _incidentLabel = L("", 26, true, CText);
            _incidentLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _incident.Add(_incidentLabel);
            var itrack = new VisualElement { pickingMode = PickingMode.Ignore };
            itrack.style.height = 12; itrack.style.marginTop = 6; itrack.style.backgroundColor = new Color(0.1f, 0.02f, 0.04f);
            _incidentFill = new VisualElement { pickingMode = PickingMode.Ignore };
            _incidentFill.style.height = 12; _incidentFill.style.backgroundColor = CRed;
            itrack.Add(_incidentFill);
            _incident.Add(itrack);
            _incident.style.display = DisplayStyle.None;
            _root.Add(_incident);

            // toast
            _toastBox = Abs(new VisualElement());
            _toastBox.style.top = 280; _toastBox.style.left = 0; _toastBox.style.right = 0;
            _toastBox.style.alignItems = Align.Center;
            _toastBox.pickingMode = PickingMode.Ignore;
            _root.Add(_toastBox);

            // flash + fader
            _flash = Full(new VisualElement());
            _flash.pickingMode = PickingMode.Ignore;
            _flash.style.backgroundColor = new Color(1, 0, 0, 0);
            _root.Add(_flash);
            _fader = Full(new VisualElement());
            _fader.pickingMode = PickingMode.Ignore;
            _fader.style.backgroundColor = new Color(0, 0, 0, 0);
            _fader.style.alignItems = Align.Center; _fader.style.justifyContent = Justify.Center;
            _root.Add(_fader);

            // cinematic layer: letterbox bars, subtitle, skip
            _cine = Full(new VisualElement());
            _cine.pickingMode = PickingMode.Ignore;
            var barTop = Abs(new VisualElement()); barTop.style.left = barTop.style.right = barTop.style.top = 0; barTop.style.height = 110;
            barTop.style.backgroundColor = Color.black; barTop.pickingMode = PickingMode.Ignore;
            var barBot = Abs(new VisualElement()); barBot.style.left = barBot.style.right = barBot.style.bottom = 0; barBot.style.height = 150;
            barBot.style.backgroundColor = Color.black; barBot.pickingMode = PickingMode.Ignore;
            barBot.style.alignItems = Align.Center; barBot.style.justifyContent = Justify.Center;
            _caption = L("", 34, true, CText);
            _caption.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("opacity") };
            _caption.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(400, TimeUnit.Millisecond) };
            _caption.style.unityTextAlign = TextAnchor.MiddleCenter; _caption.style.maxWidth = 1500;
            barBot.Add(_caption);
            _cine.Add(barTop); _cine.Add(barBot);
            _skip = B("", () => SkipClicked?.Invoke(), 22);
            Abs(_skip); _skip.style.right = 30; _skip.style.bottom = 40;
            _cine.Add(_skip);
            _cine.style.display = DisplayStyle.None;
            _root.Add(_cine);

            // title / end screens sit above the fade so they stay readable over a dark scene
            _screen.BringToFront();
            SetHudVisible(false);
            RefreshButtons(0, 0);
        }

        // ------------------------------------------------------------------ HUD

        public void SetHudVisible(bool on) => _hud.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;

        public void RefreshButtons(int cards, int total)
        {
            _btnCards.text = _loc.T("ui.cards", cards, total);
            _btnHint.text = _loc.T("ui.hint");
            _btnLang.text = _loc.Current == Lang.TH ? "TH | en" : "th | EN";
        }

        public void SetObjective(string chapter, string text)
        {
            _chapterLabel.text = chapter;
            _objective.text = text;
        }

        /// <summary>Chapter 1 after the impact: one flashing cabin-oxygen meter.</summary>
        public void SetCabin(float o2, bool show)
        {
            _resRow.Clear();
            if (!show) { _resRow.style.display = DisplayStyle.None; _forecast.style.display = DisplayStyle.None; return; }
            _resRow.style.display = DisplayStyle.Flex;
            _resRow.Add(Bar("hud.cabin_o2", Mathf.RoundToInt(o2), 100, new Color(0.45f, 0.85f, 1f), 30, 260));
        }

        /// <summary>Show the survival bars; pass null to hide them (ship chapter).</summary>
        public void SetResources(SurvivalState s, SurvivalConfig c, string progressKey = "hud.fuel")
        {
            _resRow.Clear();
            if (s == null) { _resRow.style.display = DisplayStyle.None; _forecast.style.display = DisplayStyle.None; return; }
            _resRow.style.display = DisplayStyle.Flex;

            var solBox = Chip();
            solBox.Add(L(_loc.T("hud.sol", s.sol, c.solsTotal), 24, true, CCyan));
            var pips = new VisualElement { pickingMode = PickingMode.Ignore };
            pips.style.flexDirection = FlexDirection.Row; pips.style.marginLeft = 8; pips.style.alignItems = Align.Center;
            for (int i = 0; i < c.actionsPerSol; i++)
            {
                var p = new VisualElement { pickingMode = PickingMode.Ignore };
                p.style.width = 14; p.style.height = 22; p.style.marginLeft = 3;
                p.style.backgroundColor = i < s.ap ? CCyan : new Color(0.2f, 0.25f, 0.4f);
                pips.Add(p);
            }
            solBox.Add(pips);
            solBox.Add(L(" " + _loc.T("hud.ap", s.ap), 20, false, CDim));
            _resRow.Add(solBox);

            _resRow.Add(Bar("hud.o2", s.o2, c.maxValue, new Color(0.45f, 0.85f, 1f), c.warnThreshold));
            _resRow.Add(Bar("hud.water", s.water, c.maxValue, new Color(0.3f, 0.55f, 1f), c.warnThreshold));
            _resRow.Add(Bar("hud.power", s.power, c.maxValue, CAmber, c.warnThreshold));
            _resRow.Add(Bar("hud.food", s.food, c.maxValue, CGreen, c.warnThreshold));
            _resRow.Add(Bar("hud.stamina", s.stamina, c.maxValue, new Color(1f, 0.55f, 0.75f), c.lowStaminaThreshold));
            _resRow.Add(Bar(progressKey, s.fuel, c.fuelTarget, new Color(1f, 0.6f, 0.2f), -1));
            var rad = Chip();
            bool radHigh = s.dose > c.radSafe;
            rad.Add(L(_loc.T("hud.dose", s.dose.ToString("0.0")), 20, radHigh,
                radHigh ? CRed : s.dose > c.radSafe * 0.6f ? CAmber : new Color(0.75f, 1f, 0.55f)));
            _resRow.Add(rad);
            var eff = Chip();
            eff.Add(L(_loc.T("hud.solar", Mathf.RoundToInt(s.efficiency * 100)), 20, false, s.efficiency < 0.7f ? CAmber : CDim));
            _resRow.Add(eff);
        }

        VisualElement Chip()
        {
            var v = new VisualElement { pickingMode = PickingMode.Ignore };
            v.style.flexDirection = FlexDirection.Row; v.style.alignItems = Align.Center;
            v.style.backgroundColor = CPanel; Border(v, CCyanD, 2, 6);
            v.style.paddingLeft = v.style.paddingRight = 10; v.style.paddingTop = v.style.paddingBottom = 4;
            v.style.marginRight = 6; v.style.marginBottom = 6;
            return v;
        }

        VisualElement Bar(string key, int v, int max, Color col, int warn, int width = 90)
        {
            var chip = Chip();
            bool low = warn >= 0 && v <= warn;
            chip.Add(L(_loc.T(key), 20, true, low ? CRed : CText));
            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.style.width = width; track.style.height = 14; track.style.marginLeft = 8; track.style.marginRight = 6;
            track.style.backgroundColor = new Color(0.12f, 0.15f, 0.28f);
            var fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.style.height = 14; fill.style.width = Length.Percent(Mathf.Clamp01(v / (float)max) * 100f);
            fill.style.backgroundColor = low ? CRed : col;
            track.Add(fill);
            chip.Add(track);
            chip.Add(L(v.ToString(), 20, true, low ? CRed : CText));
            if (low)
            {
                Border(chip, CRed, 2, 6);
                // danger you can see: low bars blink
                chip.schedule.Execute(() => chip.style.opacity = chip.resolvedStyle.opacity > 0.75f ? 0.4f : 1f).Every(320);
            }
            return chip;
        }

        /// <summary>Show the emergency banner; frac is the time left (1 to 0). Null text hides it.</summary>
        public void SetIncident(string text, float frac)
        {
            if (string.IsNullOrEmpty(text)) { _incident.style.display = DisplayStyle.None; return; }
            _incident.style.display = DisplayStyle.Flex;
            _incidentLabel.text = text;
            _incidentFill.style.width = Length.Percent(Mathf.Clamp01(frac) * 100f);
            _incident.style.opacity = (Time.unscaledTime * 3f % 1f) < 0.5f ? 1f : 0.82f;
        }

        public void SetForecast(string text, bool danger)
        {
            if (string.IsNullOrEmpty(text)) { _forecast.style.display = DisplayStyle.None; return; }
            _forecast.style.display = DisplayStyle.Flex;
            Border(_forecast, danger ? CRed : CAmber, 2, 8);
            _forecastLabel.text = text;
        }

        public void SetInventory(IReadOnlyList<string> items, string selected, Func<string, string> nameOf)
        {
            _invBar.Clear();
            int slots = Mathf.Max(6, items.Count);
            for (int i = 0; i < slots; i++)
            {
                var slot = new VisualElement();
                slot.style.width = 84; slot.style.height = 84; slot.style.marginLeft = slot.style.marginRight = 5;
                slot.style.backgroundColor = CPanel;
                slot.style.alignItems = Align.Center; slot.style.justifyContent = Justify.Center;
                bool has = i < items.Count;
                bool sel = has && items[i] == selected;
                Border(slot, sel ? CAmber : CCyanD, sel ? 4 : 2, 8);
                if (has)
                {
                    string id = items[i];
                    var img = new Image { image = Art.Icon(id), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                    img.style.width = 64; img.style.height = 64;
                    slot.Add(img);
                    slot.RegisterCallback<PointerDownEvent>(e => { e.StopPropagation(); ItemClicked?.Invoke(id); });
                    slot.RegisterCallback<MouseEnterEvent>(_ => _invHint.text = nameOf(id));
                    slot.RegisterCallback<MouseLeaveEvent>(_ => _invHint.text = selected != null ? _loc.T("ui.using", nameOf(selected)) : "");
                }
                else slot.pickingMode = PickingMode.Ignore;
                _invBar.Add(slot);
            }
            _invHint.text = selected != null ? _loc.T("ui.using", nameOf(selected)) : "";
        }

        public void SetHover(string text, Vector2 panelPos)
        {
            if (string.IsNullOrEmpty(text)) { _hover.style.display = DisplayStyle.None; return; }
            _hover.style.display = DisplayStyle.Flex;
            _hoverLabel.text = text;
            _hover.style.left = panelPos.x - 120;
            _hover.style.top = panelPos.y - 56;
        }

        // ------------------------------------------------------------------ dialogue

        /// <summary>Queue a line. Speaker: "orbi", "you" or "sys". Text is re-translated on language change.</summary>
        public void Say(string speaker, string key, Action done = null, params object[] args)
        {
            _lines.Enqueue(new Line { speaker = speaker, key = key, args = args, done = done });
            if (!_lineOpen) Next();
        }

        void Next()
        {
            if (_lines.Count == 0)
            {
                _lineOpen = false;
                _dialogBlocker.style.display = DisplayStyle.None;
                _dialog.style.display = DisplayStyle.None;
                return;
            }
            _current = _lines.Dequeue();
            _lineOpen = true;
            _lineShownAt = Time.unscaledTime;
            _dialogBlocker.style.display = DisplayStyle.Flex;
            _dialog.style.display = DisplayStyle.Flex;
            RenderLine();
            sfx?.Play("blip");
        }

        void RenderLine()
        {
            string sp = _current.speaker;
            _dialogSpeaker.text = _loc.T("speaker." + sp);
            _fullLine = _loc.T(_current.key, _current.args);
            _typed = Settings.TextSpeed <= 0 ? _fullLine.Length : 0;
            _dialogText.text = SafeCut(_fullLine, (int)_typed);
            _dialogMore.text = _loc.T("ui.continue");
            _dialogPortrait.image = sp == "orbi" ? Art.Orbi(0).texture : (sp == "you" ? Art.Astronaut(false, Pose.Idle0).texture : null);
            _dialogPortrait.style.display = _dialogPortrait.image != null ? DisplayStyle.Flex : DisplayStyle.None;
            Border(_dialog, sp == "sys" ? CAmber : CCyan, 3, 10);
        }

        string _fullLine = "";
        float _typed;

        /// <summary>Cut text without separating Thai vowels/tone marks from their base letter.</summary>
        static string SafeCut(string s, int n)
        {
            if (n >= s.Length) return s;
            while (n < s.Length && IsThaiMark(s[n])) n++;
            return s.Substring(0, n);
        }

        static bool IsThaiMark(char c) => c == 'ั' || (c >= 'ิ' && c <= 'ฺ') || (c >= '็' && c <= '๎');

        /// <summary>True while the current line is still being typed out.</summary>
        public bool Typing => _lineOpen && _typed < _fullLine.Length;

        public void Advance()
        {
            if (!_lineOpen || Time.unscaledTime - _lineShownAt < 0.15f) return;
            if (Typing) { _typed = _fullLine.Length; _dialogText.text = _fullLine; return; }
            var done = _current.done;
            Next();
            done?.Invoke();
        }

        // ------------------------------------------------------------------ modal

        /// <summary>Open a modal. build() is called again on language change, so it must read state, not cache text.</summary>
        public void ShowModal(Action<VisualElement> build, bool closable = true, Action onClosed = null)
        {
            _modalBuild = build;
            _modalClosed = onClosed;
            _modalClosable = closable;
            _modalLayer.style.display = DisplayStyle.Flex;
            _modalLayer.BringToFront();
            sfx?.Play("open");
            RebuildModal();
        }

        public void RebuildModal()
        {
            if (_modalBuild == null) return;
            _modalLayer.Clear();
            var panel = new VisualElement();
            panel.style.backgroundColor = CPanel; Border(panel, CCyan, 3, 12); Pad(panel, 26);
            panel.style.minWidth = 640; panel.style.maxWidth = 1180;
            _modalLayer.Add(panel);
            _modalBuild(panel);
            if (_modalClosable)
            {
                var close = B(_loc.T("ui.close"), CloseModal, 24);
                close.style.alignSelf = Align.FlexEnd;
                close.style.marginTop = 14;
                panel.Add(close);
            }
        }

        public void CloseModal()
        {
            if (_modalBuild == null) return;
            _modalBuild = null;
            sfx?.Play("close");
            _modalLayer.Clear();
            _modalLayer.style.display = DisplayStyle.None;
            var c = _modalClosed;
            _modalClosed = null;
            c?.Invoke();
        }

        public Label Title(VisualElement parent, string text)
        {
            var t = L(text, 34, true, CCyan);
            t.style.marginBottom = 10;
            parent.Add(t);
            return t;
        }

        // ------------------------------------------------------------------ screens

        public void ShowScreen(Action<VisualElement> build)
        {
            _screen.Clear();
            _screen.style.display = DisplayStyle.Flex;
            // each screen starts centred and above the fade (a cutscene brings the fader forward)
            _screen.style.alignItems = Align.Center;
            _screen.style.justifyContent = Justify.Center;
            _screen.BringToFront();
            build(_screen);
        }

        public void HideScreen()
        {
            _screen.Clear();
            _screen.style.display = DisplayStyle.None;
        }

        // ------------------------------------------------------------------ fx

        public void Toast(string text, Color? border = null)
        {
            var t = new VisualElement { pickingMode = PickingMode.Ignore };
            t.style.backgroundColor = CPanel2; Border(t, border ?? CAmber, 3, 10);
            t.style.paddingLeft = t.style.paddingRight = 22; t.style.paddingTop = t.style.paddingBottom = 10;
            t.style.marginBottom = 8;
            t.Add(L(text, 26, true, border ?? CAmber));
            _toastBox.Add(t);
            t.schedule.Execute(() => t.RemoveFromHierarchy()).StartingIn(2800);
        }

        float _fadeTarget, _fadeVal, _fadeSpeed = 2f, _flashVal;
        Label _fadeText;

        public void FadeTo(float alpha, float seconds)
        {
            _fadeTarget = alpha;
            _fadeSpeed = seconds <= 0 ? 999f : 1f / seconds;
            if (seconds <= 0) { _fadeVal = alpha; ApplyFade(); }
        }

        public bool FadeDone => Mathf.Approximately(_fadeVal, _fadeTarget);

        public void FadeText(string text, int size = 44)
        {
            _fader.Clear();
            if (string.IsNullOrEmpty(text)) return;
            _fadeText = L(text, size, true, CText);
            _fadeText.style.unityTextAlign = TextAnchor.MiddleCenter;
            _fadeText.style.maxWidth = 1400;
            _fader.Add(_fadeText);
        }

        public void Flash(Color c) { _flash.style.backgroundColor = c; _flashVal = c.a; }

        void ApplyFade()
        {
            _fader.style.backgroundColor = new Color(0, 0, 0, _fadeVal);
            _fader.style.opacity = 1;
            if (_fadeText != null) _fadeText.style.opacity = Mathf.Clamp01(_fadeVal * 1.5f);
        }

        void Update()
        {
            if (Typing)
            {
                _typed = Mathf.Min(_fullLine.Length, _typed + Settings.TextSpeed * Time.unscaledDeltaTime);
                _dialogText.text = SafeCut(_fullLine, (int)_typed);
            }
            if (!Mathf.Approximately(_fadeVal, _fadeTarget))
            {
                _fadeVal = Mathf.MoveTowards(_fadeVal, _fadeTarget, _fadeSpeed * Time.deltaTime);
                ApplyFade();
            }
            if (_flashVal > 0)
            {
                _flashVal = Mathf.Max(0, _flashVal - Time.deltaTime * 0.8f);
                var c = _flash.resolvedStyle.backgroundColor;
                _flash.style.backgroundColor = new Color(c.r, c.g, c.b, _flashVal);
            }
        }

        /// <summary>Re-render everything that shows text, after a language switch.</summary>
        public void RefreshText()
        {
            if (_lineOpen) RenderLine();
            RebuildModal();
        }

        /// <summary>Is the pointer over an interactive UI element? screenPos is in screen pixels (y up).</summary>
        public bool PointerOverUI(Vector2 screenPos)
        {
            if (Panel == null) return false;
            var p = RuntimePanelUtils.ScreenToPanel(Panel, new Vector2(screenPos.x, Screen.height - screenPos.y));
            var picked = Panel.Pick(p);
            return picked != null && picked != _root && _root.Contains(picked);
        }

        public Vector2 WorldToPanel(Vector3 world, Camera cam) => RuntimePanelUtils.CameraTransformWorldToPanel(Panel, world, cam);
    }
}
