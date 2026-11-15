using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    public partial class GameUI
    {
        const float PortraitBeat = .12f;
        static readonly string[] DialogueCues = { "โซฟา", "ห้องสื่อสาร", "ยานชูชีพ", "ช่องเทียบท่า", "sofa", "Communications", "lifeboat", "docking port" };
        readonly List<int> _dialogEnds = new List<int>();
        string _fullLine = "";
        int _shownClusters, _emphasisStart, _emphasisEnd, _portraitSide;
        float _nextCluster, _portraitBeatAt = float.NegativeInfinity;
        bool _portraitToRight;
        VisualElement _dialogFrame, _speakerPlate, _nextLine, _dialogHighlight;
        ScrollView _speech;

        public bool Typing => _lineOpen && _shownClusters < _dialogEnds.Count;

        void BuildDialogue()
        {
            _dialogBlocker = Full(new VisualElement { name = "dialogue-overlay" });
            _dialogBlocker.style.backgroundColor = DialogueShade;
            _dialogBlocker.style.display = DisplayStyle.None;
            _dialogBlocker.RegisterCallback<PointerDownEvent>(evt =>
            {
                for (var element = evt.target as VisualElement; element != null; element = element.parent)
                    if (element is Scroller) return;
                Advance();
            });
            _root.Add(_dialogBlocker);

            _dialog = Abs(new VisualElement { name = "dialogue-panel", pickingMode = PickingMode.Ignore });
            _dialog.style.bottom = 34;
            _dialog.style.minHeight = 207;
            _dialog.style.maxHeight = Length.Percent(48);
            _dialog.style.paddingLeft = _dialog.style.paddingRight = 62;
            _dialog.style.paddingTop = 33;
            _dialog.style.paddingBottom = 25;
            _dialogFrame = Full(new VisualElement { pickingMode = PickingMode.Ignore });
            _dialogFrame.generateVisualContent += DrawDialogueFrame;
            _dialog.Add(_dialogFrame);

            _dialogSpeaker = L("", 30, true, FieldBook.Ink);
            _dialogSpeaker.name = "dialogue-speaker";
            _dialogSpeaker.style.paddingLeft = _dialogSpeaker.style.paddingRight = 22;
            _dialogSpeaker.style.paddingTop = _dialogSpeaker.style.paddingBottom = 5;
            _dialogSpeaker.style.unityTextAlign = TextAnchor.MiddleCenter;
            _speakerPlate = Abs(new VisualElement { pickingMode = PickingMode.Ignore });
            _speakerPlate.style.top = -27;
            _speakerPlate.style.minWidth = 190;
            _speakerPlate.style.maxWidth = Length.Percent(84);
            _speakerPlate.Add(Plate(FieldBook.Amber, FieldBook.Ink, 15, false, 4));
            _speakerPlate.Add(_dialogSpeaker);

            _speech = new ScrollView(ScrollViewMode.Vertical) { name = "dialogue-scroll" };
            _speech.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _speech.verticalScrollerVisibility = ScrollerVisibility.Auto;
            _speech.style.flexGrow = 1;
            _speech.style.flexShrink = 1;
            _speech.style.marginTop = 16;
            _dialogHighlight = Abs(new VisualElement { pickingMode = PickingMode.Ignore });
            _dialogHighlight.style.backgroundColor = (Color)new Color32(255, 218, 151, 255);
            Border(_dialogHighlight, Color.clear, 0, 7);
            _speech.Add(_dialogHighlight);
            _dialogText = L("", 37, false, FieldBook.Ink);
            _dialogText.name = "dialogue-line";
            _dialogText.enableRichText = true;
            _dialogText.style.paddingTop = 4;
            _dialogText.style.paddingBottom = 8;
            _speech.Add(_dialogText);
            _speech.RegisterCallback<GeometryChangedEvent>(_ => PlaceDialogueHighlight());
            _dialog.Add(_speech);

            _nextLine = new VisualElement { name = "dialogue-continue", pickingMode = PickingMode.Ignore };
            _nextLine.style.flexDirection = FlexDirection.Row;
            _nextLine.style.alignItems = Align.Center;
            _nextLine.style.alignSelf = Align.FlexEnd;
            _nextLine.style.flexShrink = 0;
            _nextLine.style.marginTop = 4;
            _nextLine.Add(Hotkey("Enter"));
            _dialogMore = L("", 24, true, FieldBook.Ink);
            _nextLine.Add(_dialogMore);
            _dialog.Add(_nextLine);
            _dialog.Add(_speakerPlate);
            _dialog.style.display = DisplayStyle.None;
            _dialogBlocker.Add(_dialog);

            _dialogPortrait = new Image { name = "dialogue-portrait", scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            _dialogPortrait.style.position = Position.Absolute;
            _dialogPortrait.style.bottom = 21;
            _dialogPortrait.style.width = Length.Percent(30);
            _dialogPortrait.style.height = Length.Percent(55);
            _dialogBlocker.Add(_dialogPortrait);
        }

        void DrawDialogueFrame(MeshGenerationContext context)
        {
            float w = _dialogFrame.layout.width, h = _dialogFrame.layout.height;
            if (w < 80 || h < 70) return;
            var painter = context.painter2D;
            void Shape(float inset, float corner, Color fill, Color stroke, float weight)
            {
                painter.BeginPath();
                void Move(float x, float y) => painter.MoveTo(new Vector2(_portraitToRight ? w - x : x, y));
                void Line(float x, float y) => painter.LineTo(new Vector2(_portraitToRight ? w - x : x, y));
                Move(inset + corner, inset);
                Line(w - inset - corner, inset);
                Line(w - inset, inset + corner);
                Line(w - inset, h - inset - corner);
                Line(w - inset - corner, h - inset);
                Line(inset + corner, h - inset);
                Line(inset, h - inset - corner);
                if (_dialogPortrait.image != null)
                {
                    Line(inset, h * .50f);
                    Line(inset - 29, h * .35f);
                    Line(inset, h * .35f);
                }
                Line(inset, inset + corner);
                painter.ClosePath();
                painter.fillColor = fill;
                painter.strokeColor = stroke;
                painter.lineWidth = weight;
                if (fill.a > 0) painter.Fill();
                painter.Stroke();
            }
            Shape(4, 29, FieldBook.Teal, FieldBook.Ink, 8);
            Shape(12, 25, Color.clear, FieldBook.Rim, 3);
            Shape(20, 19, FieldBook.Paper, FieldBook.Ink, 4);
        }

        void PrepareDialogue(string speaker, string text)
        {
            _fullLine = text;
            _dialogEnds.Clear();
            var characters = StringInfo.GetTextElementEnumerator(text);
            while (characters.MoveNext())
            {
                int end = characters.ElementIndex + characters.GetTextElement().Length;
                // Older .NET tables separate Thai Sara Am and some combining marks.
                if (end < text.Length && ContinuesThaiCluster(text[end])) continue;
                _dialogEnds.Add(end);
            }
            _emphasisStart = _emphasisEnd = -1;
            foreach (string cue in DialogueCues)
            {
                int index = text.IndexOf(cue, StringComparison.OrdinalIgnoreCase);
                if (index < 0) continue;
                _emphasisStart = index;
                _emphasisEnd = index + cue.Length;
                break;
            }

            var portrait = speaker switch
            {
                "orbi" => Art.Orbi(0),
                "mentor" => Art.Crew("mentor", 0, -1),
                "you" => Art.Crew("cadet", 0, -1),
                _ => null
            };
            _dialogPortrait.image = portrait?.texture;
            if (portrait != null) portrait.texture.filterMode = FilterMode.Point;
            _portraitToRight = speaker == "you";
            _dialogPortrait.style.display = portrait == null ? DisplayStyle.None : DisplayStyle.Flex;
            _dialogPortrait.style.left = _portraitToRight ? StyleKeyword.Auto : new StyleLength(Length.Percent(-.7f));
            _dialogPortrait.style.right = _portraitToRight ? new StyleLength(Length.Percent(-.7f)) : StyleKeyword.Auto;
            _dialog.style.left = Length.Percent(portrait != null && !_portraitToRight ? 25 : 7);
            _dialog.style.right = Length.Percent(portrait != null && _portraitToRight ? 25 : 7);
            _speakerPlate.style.left = _portraitToRight ? StyleKeyword.Auto : new StyleLength(62);
            _speakerPlate.style.right = _portraitToRight ? new StyleLength(62) : StyleKeyword.Auto;
            _dialogFrame.MarkDirtyRepaint();
            _dialogMore.text = Thai(_loc.Current == Lang.TH ? "ต่อ" : "Next");
            _dialogText.style.fontSize = Mathf.Round(37 * Settings.TextScale);
            _dialogSpeaker.style.fontSize = Mathf.Round(30 * Settings.TextScale);
            _dialogMore.style.fontSize = Mathf.Round(24 * Settings.TextScale);
            _speech.scrollOffset = Vector2.zero;
            RestPortrait();
            _portraitSide = 1;
            _shownClusters = 0;
            _nextCluster = Time.unscaledTime + .16f;
            if (Settings.TextSpeed <= 0) FinishDialogue();
            else ShowDialogueText();
        }

        static bool ContinuesThaiCluster(char c)
            => c == '\u0e31' || c == '\u0e33' || (c >= '\u0e34' && c <= '\u0e3a') || (c >= '\u0e47' && c <= '\u0e4e');

        void ShowDialogueText()
        {
            int end = _shownClusters == 0 ? 0 : _dialogEnds[_shownClusters - 1];
            var display = new StringBuilder(_fullLine.Length + 60);

            void Run(int start, int stop, bool emphasis)
            {
                if (start == stop) return;
                int split = Mathf.Clamp(end, start, stop);
                if (emphasis) display.Append("<b>");
                if (start < split)
                {
                    if (emphasis) display.Append("<color=#774512>");
                    display.Append(Thai(_fullLine.Substring(start, split - start)));
                    if (emphasis) display.Append("</color>");
                }
                if (split < stop)
                    display.Append("<color=#101A1800>").Append(Thai(_fullLine.Substring(split, stop - split))).Append("</color>");
                if (emphasis) display.Append("</b>");
            }

            // Keep the same runs and weights during typing, including the transparent tail.
            if (_emphasisStart >= 0)
            {
                Run(0, _emphasisStart, false);
                Run(_emphasisStart, _emphasisEnd, true);
                Run(_emphasisEnd, _fullLine.Length, false);
            }
            else Run(0, _fullLine.Length, false);
            _dialogText.text = display.ToString();
            _nextLine.style.opacity = Typing ? 0f : 1f;
            PlaceDialogueHighlight();
        }

        void PlaceDialogueHighlight()
        {
            int end = _shownClusters == 0 ? 0 : _dialogEnds[_shownClusters - 1];
            _dialogHighlight.style.display = DisplayStyle.None;
            if (_emphasisStart < 0 || end < _emphasisEnd || _speech.layout.width <= 0) return;
            string before = _fullLine.Substring(0, _emphasisStart);
            if (before.Contains("\n")) return;
            string word = _fullLine.Substring(_emphasisStart, _emphasisEnd - _emphasisStart);
            float left = _dialogText.MeasureTextSize(Thai(before), 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            float width = _dialogText.MeasureTextSize("<b>" + Thai(word) + "</b>", 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            if (float.IsNaN(left) || float.IsNaN(width) || width <= 0 || left + width + 18 >= _speech.layout.width) return;
            _dialogHighlight.style.left = Mathf.Max(0, left - 3);
            _dialogHighlight.style.top = 4;
            _dialogHighlight.style.width = width + 7;
            _dialogHighlight.style.height = Mathf.Round(42 * Settings.TextScale);
            _dialogHighlight.style.display = DisplayStyle.Flex;
        }

        void FinishDialogue()
        {
            _shownClusters = _dialogEnds.Count;
            RestPortrait();
            ShowDialogueText();
        }

        void RestPortrait()
        {
            _portraitBeatAt = float.NegativeInfinity;
            if (_dialogPortrait != null) _dialogPortrait.style.translate = new Translate(0, 0);
        }

        void TickDialogue()
        {
            if (!Typing) return;
            int speed = Settings.TextSpeed;
            if (speed <= 0) { FinishDialogue(); return; }
            float now = Time.unscaledTime;
            float interval = 5.2f / speed;
            bool voiced = false, changed = false;
            int batch = 0;
            while (_shownClusters < _dialogEnds.Count && now >= _nextCluster && batch++ < 8)
            {
                int start = _shownClusters == 0 ? 0 : _dialogEnds[_shownClusters - 1];
                int end = _dialogEnds[_shownClusters++];
                char final = _fullLine[end - 1];
                if (!char.IsWhiteSpace(_fullLine[start]) && !char.IsPunctuation(_fullLine[start]))
                    voiced = true;
                float pause = (final == '.' || final == '!' || final == '?' || final == '\n') ? .09f : 0;
                _nextCluster += interval + pause;
                changed = true;
            }
            if (voiced)
            {
                sfx?.PlayDialogueKey();
                _portraitSide = -_portraitSide;
                _portraitBeatAt = now;
            }
            if (changed) ShowDialogueText();
            float age = now - _portraitBeatAt;
            if (!Typing || Settings.ReducedMotion || age >= PortraitBeat) { RestPortrait(); return; }
            float falloff = 1 - age / PortraitBeat;
            _dialogPortrait.style.translate = new Translate(_portraitSide * 3 * falloff,
                -2.5f * Mathf.Sin(Mathf.PI * age / PortraitBeat));
        }
    }
}
