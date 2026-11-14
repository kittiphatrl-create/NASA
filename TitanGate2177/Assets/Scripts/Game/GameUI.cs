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
        public static readonly Color CPanel = new Color(0.055f, 0.066f, 0.086f, 0.98f);
        public static readonly Color CPanel2 = new Color(0.11f, 0.13f, 0.17f, 1f);
        public static readonly Color CCyan = new Color(0.54f, 0.88f, 0.81f);
        public static readonly Color CCyanD = new Color(0.42f, 0.40f, 0.36f);
        public static readonly Color CText = new Color(1f, 0.95f, 0.84f);
        public static readonly Color CDim = new Color(0.77f, 0.77f, 0.75f);
        public static readonly Color CAmber = new Color(1f, 0.67f, 0.22f);
        public static readonly Color CRed = new Color(1f, 0.33f, 0.38f);
        public static readonly Color CGreen = new Color(0.36f, 0.89f, 0.54f);
        public static readonly Color Ink = new Color(0.09f, 0.075f, 0.065f);
        public static readonly Color Paper = new Color(1f, .92f, .76f);
        public static readonly Color DialogueShade = new Color(.012f, .016f, .027f, .82f);

        UIDocument _doc;
        Localization _loc;
        Font _font, _fontBold, _displayFont;
        public Sfx sfx;

        VisualElement _incident, _incidentFill, _cine;
        Label _caption;
        Button _skip;
        public event Action SkipClicked;

        /// <summary>Cutscene mode: letterbox bars, subtitle and a skip button; the HUD is hidden.</summary>
        public void SetCinematic(bool on)
        {
            _cine.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            if (on) { _cine.BringToFront(); _fader.BringToFront(); _skip.text = Thai(_loc.T("ui.skip")); }
            _caption.text = "";
        }

        public void SetCaption(string text)
        {
            _caption.text = Thai(text ?? "");
            _caption.style.opacity = 0;
            _caption.schedule.Execute(() => _caption.style.opacity = 1).StartingIn(60);
        }
        Label _incidentLabel;
        VisualElement _root, _hud, _topLeft, _topRight, _resRow, _forecast, _invBar, _hover, _dialog, _dialogBlocker, _modalLayer, _toastBox, _fader, _flash, _screen;
        Label _objective, _chapterLabel, _hoverLabel, _dialogSpeaker, _dialogText, _dialogMore, _forecastLabel, _invHint;
        Button _btnCards, _btnHint, _btnLang, _btnBase;
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

        public event Action CardsClicked, HintClicked, LangClicked, BaseClicked;
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
            _font = Resources.Load<Font>("Fonts/Sarabun-Regular");
            _fontBold = Resources.Load<Font>("Fonts/Sarabun-SemiBold");
            _displayFont = Resources.Load<Font>("Fonts/ChakraPetch-Bold");

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

        public void DisplayFont(Label label)
        {
            if (_loc.Current == Lang.EN && _displayFont != null)
                label.style.unityFontDefinition = FontDefinition.FromFont(_displayFont);
        }

        public static VisualElement Backdrop(Color color)
        {
            var shade = Full(new VisualElement { pickingMode = PickingMode.Ignore });
            shade.style.backgroundColor = color;
            return shade;
        }

        public static VisualElement Plate(Color fill, Color edge, float cut = 14, bool printed = false, float weight = 3)
        {
            var plate = Full(new VisualElement { pickingMode = PickingMode.Ignore });
            plate.generateVisualContent += ctx =>
            {
                float w = plate.contentRect.width, h = plate.contentRect.height;
                if (w < 8 || h < 8) return;
                float c = Mathf.Min(cut, h * .35f, w * .15f);
                var p = ctx.painter2D;
                p.fillColor = fill;
                p.strokeColor = edge;
                p.lineWidth = weight;
                p.BeginPath();
                float inset = weight / 2 + 1;
                p.MoveTo(new Vector2(c + inset, inset));
                p.LineTo(new Vector2(w - c - inset, inset));
                p.LineTo(new Vector2(w - inset, h * .5f));
                p.LineTo(new Vector2(w - c - inset, h - inset));
                p.LineTo(new Vector2(inset, h - inset));
                p.LineTo(new Vector2(c + inset, inset));
                p.ClosePath();
                p.Fill();
                p.Stroke();
                if (!printed || w < 52 || h < 26) return;
                p.fillColor = new Color(edge.r, edge.g, edge.b, .40f);
                for (int row = 0; row < 5; row++)
                for (int col = row; col < 10; col++)
                {
                    var center = new Vector2(w - c - 12 - col * 5, h - 10 - row * 5);
                    p.BeginPath();
                    p.Arc(center, 1.35f, Angle.Degrees(0), Angle.Degrees(360));
                    p.ClosePath();
                    p.Fill();
                }
            };
            return plate;
        }

        public static void PanelFrame(VisualElement panel, Color accent)
        {
            panel.style.backgroundColor = Color.clear;
            Border(panel, Color.clear, 0, 0);
            panel.Insert(0, Plate(CPanel, accent, 20));
        }

        public Label L(string text, int size = 26, bool bold = false, Color? color = null)
        {
            var l = new Label(Thai(text));
            Txt(l, bold);
            l.style.fontSize = Mathf.Round(size * Settings.TextScale);
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginTop = l.style.marginBottom = 0;
            l.style.paddingTop = 2; l.style.paddingBottom = 2;
            if (color.HasValue) l.style.color = color.Value;
            l.pickingMode = PickingMode.Ignore;
            return l;
        }

        public VisualElement PrintedLabel(string text, int size, Color fill, Color ink)
        {
            var tag = new VisualElement { pickingMode = PickingMode.Ignore };
            tag.Add(Plate(fill, Ink, 15, true, 5));
            var label = L(text, size, true, ink);
            DisplayFont(label);
            label.style.paddingLeft = label.style.paddingRight = 23;
            label.style.paddingTop = label.style.paddingBottom = 7;
            tag.Add(label);
            return tag;
        }

        public Label Hotkey(string key)
        {
            var cap = L(key, 18, true, Ink);
            cap.style.backgroundColor = CAmber;
            cap.style.unityTextAlign = TextAnchor.MiddleCenter;
            cap.style.paddingLeft = cap.style.paddingRight = 8;
            cap.style.marginRight = 8;
            Border(cap, Ink, 2, 4);
            return cap;
        }

        // Native button text remains native: translated/dynamic labels keep their original behaviour.
        static readonly Dictionary<int, Sprite> _buttonPaper = new Dictionary<int, Sprite>();
        static Sprite ButtonPaper(bool selected, bool pale)
        {
            int key = selected ? 2 : pale ? 1 : 0;
            if (_buttonPaper.TryGetValue(key, out var found)) return found;
            var canvas = new PixelCanvas(256, 64);
            Color32 fill = selected ? CAmber : pale ? Paper : CPanel2;
            Color32 edge = selected || pale ? Ink : Paper;
            for (int y = 0; y < 64; y++)
            {
                int left = Mathf.RoundToInt((63 - y) * .20f) + 2;
                int right = 253 - Mathf.RoundToInt(Mathf.Abs(y - 31.5f) * .30f);
                canvas.HLine(left, right, y, edge);
                if (y > 3 && y < 60) canvas.HLine(left + 3, right - 3, y, fill);
            }
            for (int y = 10; y < 28; y += 5) for (int x = 216; x < 240 - (y - 10); x += 5)
                canvas.Set(x, y, Color.Lerp(fill, edge, .32f));
            var sprite = canvas.ToSprite();
            _buttonPaper[key] = sprite;
            return sprite;
        }

        public Label CutButton(Button button, bool pale = false, string key = null, int size = 25, Sprite icon = null)
        {
            var idle = Plate(pale ? Paper : CPanel2, pale ? Ink : Paper, 18, true, 5);
            var active = Plate(CAmber, Ink, 18, true, 5);
            button.Insert(0, idle); button.Insert(1, active);
            var caption = L(button.text, size, true);
            caption.style.flexShrink = 1;
            button.tooltip = button.text;
            button.text = "";
            if (key != null || icon != null)
            {
                button.style.flexDirection = FlexDirection.Row;
                button.style.alignItems = Align.Center;
                if (key != null) button.Add(Hotkey(key));
                if (icon != null)
                {
                    var glyph = new Image { sprite = icon, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                    glyph.style.width = 32; glyph.style.height = 32;
                    glyph.style.marginRight = 10; glyph.style.flexShrink = 0;
                    button.Add(glyph);
                }
            }
            button.Add(caption);
            bool focus = false, hover = false;
            void Paint()
            {
                bool on = (focus || hover) && button.enabledInHierarchy;
                active.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                idle.style.display = on ? DisplayStyle.None : DisplayStyle.Flex;
                button.style.backgroundColor = Color.clear;
                button.style.backgroundImage = StyleKeyword.None;
                Border(button, Color.clear, 0, 0);
                caption.style.color = on || pale ? Ink : CText;
                button.style.opacity = button.enabledInHierarchy ? 1f : .6f;
            }
            button.RegisterCallback<MouseEnterEvent>(_ => { hover = true; Paint(); });
            button.RegisterCallback<MouseLeaveEvent>(_ => { hover = false; Paint(); });
            button.RegisterCallback<FocusInEvent>(_ => { focus = true; Paint(); });
            button.RegisterCallback<FocusOutEvent>(_ => { focus = false; Paint(); });
            Paint();
            return caption;
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
            var b = new Button(() => { sfx?.Play("click"); onClick?.Invoke(); }) { text = Thai(text) };
            b.RegisterCallback<MouseEnterEvent>(_ => { if (b.enabledSelf) sfx?.Play("hover"); });
            Txt(b, true);
            b.style.fontSize = Mathf.Round(size * Settings.TextScale);
            b.style.whiteSpace = WhiteSpace.Normal;
            bool hover = false, focus = false;
            void Paint()
            {
                bool active = hover || focus;
                b.style.backgroundColor = Color.clear;
                b.style.backgroundImage = new StyleBackground(ButtonPaper(active, primary));
                b.style.color = active || primary ? Ink : CText;
                Border(b, Color.clear, 0, 0);
                b.style.opacity = b.enabledInHierarchy ? 1f : .5f;
            }
            Paint();
            b.RegisterCallback<FocusInEvent>(_ => { focus = true; Paint(); });
            b.RegisterCallback<FocusOutEvent>(_ => { focus = false; Paint(); });
            b.style.paddingLeft = b.style.paddingRight = 28;
            b.style.paddingTop = b.style.paddingBottom = 8;
            b.style.marginTop = b.style.marginBottom = 5;
            b.style.marginLeft = b.style.marginRight = 5;
            b.RegisterCallback<MouseEnterEvent>(_ => { if (b.enabledInHierarchy) { hover = true; Paint(); } });
            b.RegisterCallback<MouseLeaveEvent>(_ => { hover = false; Paint(); });
            b.style.transitionProperty = new List<StylePropertyName> { "scale", "translate", "background-color" };
            b.style.transitionDuration = new List<TimeValue> { new TimeValue(130, TimeUnit.Millisecond) };
            b.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseOut) };
            void Lift(bool on)
            {
                if (!b.enabledSelf || Settings.ReducedMotion) return;
                b.style.scale = new Scale(on ? new Vector3(1.025f, 1.025f, 1) : Vector3.one);
                b.style.translate = new Translate(on ? 5 : 0, on ? -2 : 0);
            }
            b.RegisterCallback<MouseEnterEvent>(_ => Lift(true)); b.RegisterCallback<MouseLeaveEvent>(_ => Lift(false));
            b.RegisterCallback<FocusInEvent>(_ => Lift(true)); b.RegisterCallback<FocusOutEvent>(_ => Lift(false));
            b.RegisterCallback<PointerDownEvent>(_ => { if (!Settings.ReducedMotion && b.enabledSelf) b.style.scale = new Scale(new Vector3(.98f, .98f, 1)); });
            b.RegisterCallback<PointerUpEvent>(_ => Lift(true));
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
            _topLeft.Add(Plate(CPanel, CAmber)); Pad(_topLeft, 16);
            _topLeft.pickingMode = PickingMode.Ignore;
            _chapterLabel = L("", 22, true, CAmber);
            DisplayFont(_chapterLabel);
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
            _btnBase = B("", () => BaseClicked?.Invoke(), 24, true);
            _btnBase.style.display = DisplayStyle.None;
            _topRight.Add(_btnBase); _topRight.Add(_btnCards); _topRight.Add(_btnHint); _topRight.Add(_btnLang);
            _hud.Add(_topRight);

            // resources row (Mars only)
            _resRow = Abs(new VisualElement());
            _resRow.style.left = 600; _resRow.style.right = 760; _resRow.style.top = 16;
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
            _dialogBlocker.style.backgroundColor = DialogueShade;
            _dialogBlocker.style.display = DisplayStyle.None;
            _dialogBlocker.RegisterCallback<PointerDownEvent>(_ => Advance());
            _root.Add(_dialogBlocker);
            _dialog = Abs(new VisualElement());
            _dialog.style.left = Length.Percent(21); _dialog.style.right = Length.Percent(8); _dialog.style.bottom = 38;
            _dialog.style.maxHeight = Length.Percent(48);
            _dialog.Add(Plate(Paper, Ink, 30, printed: true, weight: 7)); Pad(_dialog, 30);
            _dialog.style.alignItems = Align.Center;
            _dialog.style.flexDirection = FlexDirection.Row;
            _dialog.pickingMode = PickingMode.Ignore;
            _dialogPortrait = new Image { scaleMode = ScaleMode.ScaleToFit };
            _dialogPortrait.style.position = Position.Absolute;
            _dialogPortrait.style.bottom = 32;
            _dialogPortrait.style.width = Length.Percent(24);
            _dialogPortrait.style.height = Length.Percent(44);
            _dialogPortrait.pickingMode = PickingMode.Ignore;
            _dialogBlocker.Add(_dialogPortrait);
            var col = new VisualElement { pickingMode = PickingMode.Ignore };
            col.style.flexGrow = 1; col.style.flexShrink = 1;
            _dialogSpeaker = L("", 25, true, Ink);
            _dialogSpeaker.style.backgroundColor = CAmber;
            _dialogSpeaker.style.paddingLeft = _dialogSpeaker.style.paddingRight = 12;
            DisplayFont(_dialogSpeaker);
            _dialogText = L("", 31, false, Ink);
            _dialogMore = L("", 19, true, Ink);
            _dialogMore.style.unityTextAlign = TextAnchor.LowerRight;
            var speech = new ScrollView();
            speech.style.flexShrink = 1;
            speech.style.maxHeight = 360;
            speech.Add(_dialogText);
            col.Add(_dialogSpeaker); col.Add(speech); col.Add(_dialogMore);
            _dialog.Add(col);
            _dialog.style.display = DisplayStyle.None;
            _dialogBlocker.Add(_dialog);

            // modal
            _modalLayer = Full(new VisualElement());
            _modalLayer.style.backgroundColor = new Color(0, 0, 0, 0.76f);
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
            _incident.Add(Plate(new Color(0.27f, 0.04f, 0.055f), CRed)); Pad(_incident, 16);
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
            _toastBox.style.bottom = 88; _toastBox.style.right = 24; _toastBox.style.maxWidth = 560;
            _toastBox.style.alignItems = Align.FlexEnd;
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
            _btnCards.text = Thai(_loc.T("ui.cards", cards, total));
            _btnHint.text = Thai(_loc.T("ui.hint"));
            _btnBase.text = Thai(_loc.T("ui.base"));
            _btnLang.text = _loc.Current == Lang.TH ? "TH | en" : "th | EN";
        }

        public void SetObjective(string chapter, string text)
        {
            _chapterLabel.text = Thai(chapter);
            _objective.text = Thai(text);
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
        /// <summary>Show/hide the Base Control button (Chapter 2 only).</summary>
        public void SetBaseButton(bool on) => _btnBase.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;

        /// <summary>
        /// The outpost HUD: the clock and rescue countdown, three big gauges (Air, Power, Shelter) and
        /// two small ones (Supplies, Energy), plus radiation and solar output. Null hides it.
        /// </summary>
        public void SetOutpost(OutpostSim sim)
        {
            _resRow.Clear();
            if (sim == null) { _resRow.style.display = DisplayStyle.None; _forecast.style.display = DisplayStyle.None; return; }
            _resRow.style.display = DisplayStyle.Flex;
            var s = sim.State;
            var c = sim.Config;
            var time = Chip();
            time.Add(L(_loc.T("hud.time", sim.Day, c.hoursTotal / 24, sim.Clock.ToString("00")), 24, true, CCyan));
            time.Add(L("   " + _loc.T("hud.rescue_in", sim.HoursLeft), 22, true, sim.HoursLeft <= 24 ? CGreen : CAmber));
            _resRow.Add(time);
            var brk = new VisualElement { pickingMode = PickingMode.Ignore }; brk.style.width = Length.Percent(100); brk.style.height = 0;
            _resRow.Add(brk);
            _resRow.Add(Bar("hud.air", Mathf.RoundToInt(s.air), 100, new Color(0.45f, 0.85f, 1f), (int)c.warnAir, 120));
            _resRow.Add(Bar("hud.power", Mathf.RoundToInt(s.power), 100, CAmber, (int)c.warnPower, 120));
            _resRow.Add(Bar("hud.shelter", Mathf.RoundToInt(s.shelter), 100, new Color(0.75f, 0.6f, 1f), -1, 120));
            var brk2 = new VisualElement { pickingMode = PickingMode.Ignore }; brk2.style.width = Length.Percent(100); brk2.style.height = 0;
            _resRow.Add(brk2);
            _resRow.Add(Bar("hud.supplies", Mathf.RoundToInt(s.supplies), 100, CGreen, (int)c.warnSupplies, 70));
            _resRow.Add(Bar("hud.energy", Mathf.RoundToInt(s.energy), 100, new Color(1f, 0.55f, 0.75f), (int)c.lowEnergy, 70));
            var rad = Chip();
            bool radHigh = s.dose > c.radSafe;
            rad.Add(L(_loc.T("hud.dose", s.dose.ToString("0.0")), 18, radHigh, radHigh ? CRed : new Color(0.75f, 1f, 0.55f)));
            _resRow.Add(rad);
            var sun = Chip();
            sun.Add(L(_loc.T("hud.solar", Mathf.RoundToInt(s.efficiency * 100)), 18, false, s.efficiency < 0.7f ? CAmber : CDim));
            _resRow.Add(sun);
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
            _incidentLabel.text = Thai(text);
            _incidentFill.style.width = Length.Percent(Mathf.Clamp01(frac) * 100f);
        }

        public void SetForecast(string text, bool danger)
        {
            if (string.IsNullOrEmpty(text)) { _forecast.style.display = DisplayStyle.None; return; }
            _forecast.style.display = DisplayStyle.Flex;
            Border(_forecast, danger ? CRed : CAmber, 2, 8);
            _forecastLabel.text = Thai(text);
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
                    slot.RegisterCallback<MouseEnterEvent>(_ => _invHint.text = Thai(nameOf(id)));
                    slot.RegisterCallback<MouseLeaveEvent>(_ => _invHint.text = selected != null ? _loc.T("ui.using", nameOf(selected)) : "");
                }
                else slot.pickingMode = PickingMode.Ignore;
                _invBar.Add(slot);
            }
            _invHint.text = Thai(selected != null ? _loc.T("ui.using", nameOf(selected)) : "");
        }

        public void SetHover(string text, Vector2 panelPos)
        {
            if (string.IsNullOrEmpty(text)) { _hover.style.display = DisplayStyle.None; return; }
            _hover.style.display = DisplayStyle.Flex;
            _hoverLabel.text = Thai(text);
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
            _dialogSpeaker.text = Thai(_loc.T("speaker." + sp));
            _fullLine = _loc.T(_current.key, _current.args);
            _typed = Settings.TextSpeed <= 0 ? _fullLine.Length : 0;
            _dialogText.text = Thai(SafeCut(_fullLine, (int)_typed));
            _dialogMore.text = Thai(_loc.T("ui.continue"));
            _dialogPortrait.image = sp == "orbi" ? Art.Orbi(0).texture : (sp == "mentor" ? Art.Crew("mentor", 0, -1).texture : sp == "you" ? Art.Crew("cadet", 0, -1).texture : null);
            _dialogPortrait.style.display = _dialogPortrait.image != null ? DisplayStyle.Flex : DisplayStyle.None;
            bool player = sp == "you";
            _dialog.style.left = Length.Percent(player ? 8 : 21);
            _dialog.style.right = Length.Percent(player ? 21 : 8);
            _dialogPortrait.style.left = player ? StyleKeyword.Auto : new StyleLength(Length.Percent(0));
            _dialogPortrait.style.right = player ? new StyleLength(Length.Percent(0)) : StyleKeyword.Auto;
            _dialogSpeaker.style.alignSelf = player ? Align.FlexEnd : Align.FlexStart;
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

        /// <summary>Keep combining sequences intact for Advanced Text Generator and the font's mark positioning.</summary>
        public static string Thai(string s) => s;

        static bool IsThaiMark(char c) => c == 'ั' || (c >= 'ิ' && c <= 'ฺ') || (c >= '็' && c <= '๎');

        /// <summary>True while the current line is still being typed out.</summary>
        public bool Typing => _lineOpen && _typed < _fullLine.Length;

        public void Advance()
        {
            if (!_lineOpen || Time.unscaledTime - _lineShownAt < 0.15f) return;
            if (Typing) { _typed = _fullLine.Length; _dialogText.text = Thai(_fullLine); return; }
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
            panel.style.backgroundColor = Color.clear; Border(panel, Color.clear, 0, 0); Pad(panel, 32);
            panel.Add(Plate(CPanel, Paper, 24, true, 5));
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
            var t = L(text, 36, true, CAmber);
            DisplayFont(t);
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
            t.Add(Plate(CPanel2, border ?? CAmber));
            t.style.paddingLeft = t.style.paddingRight = 22; t.style.paddingTop = t.style.paddingBottom = 10;
            t.style.marginBottom = 8; t.style.maxWidth = 560;
            while (_toastBox.childCount >= 2) _toastBox.RemoveAt(0);
            t.Add(L(text, 24, true, border ?? CAmber));
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
                _dialogText.text = Thai(SafeCut(_fullLine, (int)_typed));
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
