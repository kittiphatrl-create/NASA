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
        const int DialogueTextSize = 44;
        const int SpeakerTextSize = 33;
        static readonly string[] DialogueCues = { "โซฟา", "ห้องสื่อสาร", "ยานชูชีพ", "ช่องเทียบท่า", "sofa", "Communications", "lifeboat", "docking port" };
        readonly List<int> _dialogEnds = new List<int>();
        readonly Dictionary<string, Texture2D> _dialogPortraits = new Dictionary<string, Texture2D>();
        string _fullLine = "";
        string _portraitSpeaker;
        int _shownClusters, _emphasisStart, _emphasisEnd, _speechPose;
        float _nextCluster, _portraitBeatAt = float.NegativeInfinity, _nextMouthPose, _timedTextInterval;
        bool _timedDialogue;
        int _voicedPose;
        Image _dialogPortrait;
        VisualElement _portraitStage, _portraitSpeech, _dialogFrame, _speakerPlate, _nextLine, _dialogHighlight, _speakerDetail;
        ScrollView _speech, _dialogueReplies;
        Button _firstDialogueReply;

        public bool Typing => _lineOpen && _shownClusters < _dialogEnds.Count;

        void BuildDialogue()
        {
            _dialogBlocker = Full(new VisualElement { name = "dialogue-overlay" });
            _dialogBlocker.style.backgroundColor = new Color(.025f, .037f, .065f, .72f);
            _dialogBlocker.style.display = DisplayStyle.None;
            _dialogBlocker.RegisterCallback<PointerDownEvent>(evt =>
            {
                for (var element = evt.target as VisualElement; element != null; element = element.parent)
                    if (element is Scroller || element is Button || element == _dialogueReplies) return;
                Advance();
            });
            _root.Add(_dialogBlocker);

            _portraitStage = Abs(new VisualElement { name = "dialogue-portrait-stage", pickingMode = PickingMode.Ignore });
            _dialogPortrait = new Image { name = "dialogue-portrait", scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            Full(_dialogPortrait);
            _portraitSpeech = Full(new VisualElement { name = "dialogue-portrait-speech", pickingMode = PickingMode.Ignore });
            _portraitSpeech.generateVisualContent += ctx =>
                DrawPortraitSpeech(ctx, _portraitSpeaker, _speechPose, _portraitSpeech.layout, _dialogPortrait.image);
            _portraitStage.Add(_dialogPortrait);
            _portraitStage.Add(_portraitSpeech);
            _dialogBlocker.Add(_portraitStage);
            _dialogBlocker.RegisterCallback<GeometryChangedEvent>(_ => LayoutDialogue());

            _dialog = Abs(new VisualElement { name = "dialogue-panel", pickingMode = PickingMode.Ignore });
            _dialog.style.bottom = Length.Percent(7.5f);
            _dialog.style.maxHeight = Length.Percent(51);
            _dialog.style.paddingTop = 51;
            _dialog.style.paddingBottom = 33;
            _dialogFrame = Full(new VisualElement { pickingMode = PickingMode.Ignore });
            _dialogFrame.generateVisualContent += ctx => DrawDialogueFrame(ctx, _dialogFrame.layout, _dialogPortrait.image != null);
            _dialog.Add(_dialogFrame);

            _dialogSpeaker = L("", SpeakerTextSize, true, FieldBook.Ink);
            _dialogSpeaker.name = "dialogue-speaker";
            _dialogSpeaker.style.paddingLeft = _dialogSpeaker.style.paddingRight = 26;
            _dialogSpeaker.style.paddingTop = _dialogSpeaker.style.paddingBottom = 7;
            _dialogSpeaker.style.unityTextAlign = TextAnchor.MiddleCenter;
            _speakerPlate = Abs(new VisualElement { pickingMode = PickingMode.Ignore });
            _speakerPlate.style.top = -30;
            _speakerPlate.style.minWidth = 132;
            _speakerPlate.style.flexShrink = 0;
            _speakerPlate.style.maxWidth = Length.Percent(84);
            _speakerPlate.Add(Plate(FieldBook.Amber, FieldBook.Ink, 15, false, 4));
            _speakerPlate.Add(_dialogSpeaker);

            _speech = new ScrollView(ScrollViewMode.Vertical) { name = "dialogue-scroll" };
            _speech.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _speech.verticalScrollerVisibility = ScrollerVisibility.Auto;
            _speech.style.flexGrow = 1;
            _speech.style.flexShrink = 1;
            _dialogHighlight = Abs(new VisualElement { pickingMode = PickingMode.Ignore });
            _dialogHighlight.style.backgroundColor = (Color)new Color32(255, 218, 151, 255);
            Border(_dialogHighlight, Color.clear, 0, 7);
            _speech.Add(_dialogHighlight);
            _dialogText = L("", DialogueTextSize, false, FieldBook.Ink);
            _dialogText.name = "dialogue-line";
            _dialogText.enableRichText = true;
            _dialogText.style.paddingTop = 4;
            _dialogText.style.paddingBottom = 8;
            _speech.Add(_dialogText);
            _speech.RegisterCallback<GeometryChangedEvent>(_ => PlaceDialogueHighlight());
            _dialog.Add(_speech);

            _dialogueReplies = new ScrollView(ScrollViewMode.Vertical) { name = "dialogue-replies" };
            _dialogueReplies.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _dialogueReplies.verticalScrollerVisibility = ScrollerVisibility.Auto;
            _dialogueReplies.style.flexShrink = 0;
            _dialogueReplies.style.marginTop = 7;
            _dialogueReplies.style.display = DisplayStyle.None;
            _dialog.Add(_dialogueReplies);

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
            _speakerDetail = Abs(new VisualElement { pickingMode = PickingMode.Ignore });
            _speakerDetail.style.top = -24;
            _speakerDetail.style.right = 68;
            _dialog.Add(_speakerDetail);
            _dialog.style.display = DisplayStyle.None;
            _dialogBlocker.Add(_dialog);
        }

        static void DrawDialogueFrame(MeshGenerationContext context, Rect bounds, bool hasPortrait, float scale = 1)
        {
            float w = bounds.width / scale, h = bounds.height / scale;
            if (w < 80 || h < 70) return;
            var painter = context.painter2D;
            void Shape(float inset, float corner, Color fill, Color stroke, float weight)
            {
                painter.BeginPath();
                void Move(float x, float y) => painter.MoveTo(new Vector2(x * scale, y * scale));
                void Line(float x, float y) => painter.LineTo(new Vector2(x * scale, y * scale));
                Move(inset + corner, inset);
                Line(w - inset - corner, inset);
                Line(w - inset, inset + corner);
                Line(w - inset, h - inset - corner);
                Line(w - inset - corner, h - inset);
                Line(inset + corner, h - inset);
                Line(inset, h - inset - corner);
                if (hasPortrait)
                {
                    float tail = Mathf.Max(63, h * .29f);
                    Line(inset, tail + 40 - inset * .55f);
                    Line(-30 + inset * 1.6f, tail + inset * .48f);
                    Line(inset, tail + inset * .48f);
                }
                Line(inset, inset + corner);
                painter.ClosePath();
                painter.fillColor = fill;
                painter.strokeColor = stroke;
                painter.lineWidth = weight * scale;
                if (fill.a > 0) painter.Fill();
                painter.Stroke();
            }
            Shape(5, 30, FieldBook.Teal, new Color32(22, 45, 43, 255), 9);
            Shape(12, 25, Color.clear, FieldBook.Rim, 3);
            Shape(20, 19, FieldBook.Paper, FieldBook.Ink, 4);
        }

        string DialogueSpeakerName(string speaker)
        {
            if (speaker == "orbi") return "ORBI";
            if (speaker == "official") return _loc.Current == Lang.TH ? "ศูนย์ควบคุมภารกิจ" : "Mission Control";
            string key = "speaker." + speaker;
            return _loc.Has(key) ? _loc.T(key) : _loc.T("visitor." + speaker + ".name");
        }

        void PrepareDialogueReplies()
        {
            _speakerDetail.Clear();
            if (_current.detail?.Invoke() is VisualElement detail) _speakerDetail.Add(detail);
            _dialogueReplies.Clear();
            _dialogueReplies.scrollOffset = Vector2.zero;
            _dialogueReplies.style.display = DisplayStyle.None;
            _firstDialogueReply = null;
            if (_current.replies == null) return;
            foreach (var (label, act) in _current.replies)
            {
                var button = B(label, () =>
                {
                    if (DialogOpen && !Typing) CompleteDialogue(act);
                }, 25, true);
                button.style.unityTextAlign = TextAnchor.MiddleLeft;
                button.style.minHeight = 50;
                _firstDialogueReply ??= button;
                _dialogueReplies.Add(button);
            }
        }

        internal VisualElement SpokenCallout(string speaker, string text, Color tone)
        {
            var row = new VisualElement { name = "spoken-callout", pickingMode = PickingMode.Ignore };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.FlexEnd;
            var portrait = new Image { image = DialoguePortrait(speaker), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            var portraitBox = new VisualElement { pickingMode = PickingMode.Ignore };
            portraitBox.style.width = 103;
            portraitBox.style.height = 114;
            portraitBox.style.flexShrink = 0;
            portraitBox.style.marginRight = 10;
            portraitBox.Add(Full(portrait));
            var speechFx = Full(new VisualElement { pickingMode = PickingMode.Ignore });
            portraitBox.Add(speechFx);
            row.Add(portraitBox);
            if (speaker is "orbi" or "mentor")
            {
                float started = Time.unscaledTime;
                float speakingTime = Mathf.Clamp(text.Length * .038f, .5f, 2.8f);
                speechFx.generateVisualContent += ctx =>
                {
                    float age = Time.unscaledTime - started;
                    int pose = !Settings.ReducedMotion && age < speakingTime ? (int)(age * 12) % 4 : 0;
                    DrawPortraitSpeech(ctx, speaker, pose, speechFx.layout, portrait.image);
                };
                speechFx.schedule.Execute(speechFx.MarkDirtyRepaint).Every(70)
                    .Until(() => Time.unscaledTime - started > speakingTime + .1f);
            }
            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.style.flexGrow = 1;
            box.style.flexShrink = 1;
            box.style.minHeight = 76;
            box.style.paddingTop = 20;
            box.style.paddingBottom = 11;
            box.style.paddingLeft = box.style.paddingRight = 18;
            var frame = Full(new VisualElement { pickingMode = PickingMode.Ignore });
            frame.generateVisualContent += ctx => DrawDialogueFrame(ctx, frame.layout, portrait.image != null, .38f);
            box.Add(frame);
            var plate = PrintedLabel(DialogueSpeakerName(speaker), 18, FieldBook.Amber, FieldBook.Ink);
            plate.style.position = Position.Absolute;
            plate.style.left = 16;
            plate.style.top = -12;
            box.Add(plate);
            box.Add(L(text, 23, false, tone == CRed ? new Color32(104, 35, 31, 255) : FieldBook.Ink));
            row.Add(box);
            return row;
        }

        internal void NotifySpeech(string speaker, string text, Color tone)
        {
            var notice = SpokenCallout(speaker, text, tone);
            notice.style.width = Length.Percent(100);
            PushNotice(notice, 9600);
            notice.schedule.Execute(() => notice.style.display = Blocking ? DisplayStyle.None : DisplayStyle.Flex).Every(100);
        }

        Texture2D DialoguePortrait(string speaker)
        {
            string asset = speaker switch
            {
                "mentor" => "mentor",
                "orbi" => "orbi",
                "you" => "cadet",
                _ => null
            };
            if (asset == null) return Art.ConversationBust(speaker).texture;
            if (_dialogPortraits.TryGetValue(asset, out var texture)) return texture;
            var data = Resources.Load<TextAsset>("Sprites/Dialogue/" + asset + ".png");
            if (data == null) return Art.ConversationBust(asset).texture;
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "Dialogue " + asset,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            if (!texture.LoadImage(data.bytes, true))
            {
                Destroy(texture);
                return Art.ConversationBust(asset).texture;
            }
            _dialogPortraits.Add(asset, texture);
            return texture;
        }

        void OnDestroy()
        {
            foreach (var portrait in _dialogPortraits.Values)
                Destroy(portrait);
        }

        void LayoutDialogue()
        {
            float width = _dialogBlocker.layout.width, height = _dialogBlocker.layout.height;
            if (!(width > 0 && height > 0)) return;

            var portrait = _dialogPortrait.image;
            bool compact = width < 1180;
            float inset = compact ? 40 : 68;
            _dialog.style.left = Length.Percent(portrait == null ? 7 : (compact ? 29 : 36));
            _dialog.style.right = Length.Percent(portrait == null ? 7 : 2.4f);
            _dialog.style.minHeight = Mathf.Min(compact ? 230 : 278, height * .46f);
            _dialog.style.paddingLeft = _dialog.style.paddingRight = inset;
            _speakerPlate.style.left = inset - 5;
            _speakerDetail.style.right = inset - 5;
            _dialogueReplies.style.maxHeight = Mathf.Min(214, height * .22f);
            if (portrait == null) return;

            bool hovering = _portraitSpeaker is "orbi" or "sys";
            float aspect = (float)portrait.width / portrait.height;
            float portraitWidth = Mathf.Min(height * (hovering ? .68f : .86f) * aspect,
                width * (compact ? .295f : .35f));
            _portraitStage.style.width = portraitWidth;
            _portraitStage.style.height = portraitWidth / aspect;
            _portraitStage.style.left = Mathf.Max(width * .012f, width * (compact ? .295f : .35f) - portraitWidth);
            _portraitStage.style.bottom = hovering ? Length.Percent(9) : new Length(0);
        }

        static void DrawPortraitSpeech(MeshGenerationContext context, string speaker, int pose, Rect bounds, Texture image)
        {
            if (pose == 0 || image == null || !(bounds.width > 0 && bounds.height > 0)) return;
            float zoom = Mathf.Min(bounds.width / image.width, bounds.height / image.height);
            float w = image.width * zoom, h = image.height * zoom;
            float left = (bounds.width - w) * .5f, top = (bounds.height - h) * .5f;
            var p = context.painter2D;
            void Pixels(float x, float y, float width, float height, Color32 color)
            {
                x += left;
                y += top;
                p.BeginPath();
                p.MoveTo(new Vector2(x, y));
                p.LineTo(new Vector2(x + width, y));
                p.LineTo(new Vector2(x + width, y + height));
                p.LineTo(new Vector2(x, y + height));
                p.ClosePath();
                p.fillColor = color;
                p.Fill();
            }

            if (speaker == "orbi")
            {
                float unit = w / 118;
                for (int i = 0; i < 5; i++)
                {
                    int bar = 2 + (i + pose * 2) % 4;
                    Pixels(w * .605f + (i - 2) * 4 * unit, h * .53f - bar * unit / 2,
                        2 * unit, bar * unit, new Color32(75, 231, 209, 255));
                }
                return;
            }

            if (speaker is not ("mentor" or "you")) return;
            bool mentor = speaker == "mentor";
            float step = w / (mentor ? 119 : 101);
            float center = w * (mentor ? .603f : .605f);
            float lip = h * (mentor ? .366f : .273f);
            Color32 upper = mentor ? new Color32(118, 67, 44, 255) : new Color32(140, 77, 56, 255);
            Color32 dark = mentor ? new Color32(68, 36, 28, 255) : new Color32(91, 44, 39, 255);
            Color32 lower = mentor ? new Color32(185, 121, 83, 255) : new Color32(186, 112, 79, 255);

            // Keep the corners attached to the original lip crease. Only the center of the lower lip drops.
            float halfWidth = (pose == 3 ? 3 : pose == 2 ? 5 : 4.5f) * step;
            float opening = (pose == 1 ? 1 : 2) * step;
            Pixels(center - halfWidth, lip, halfWidth * 2, step, upper);
            Pixels(center - halfWidth + step, lip + step, (halfWidth - step) * 2, opening, dark);
            Pixels(center - halfWidth + 2 * step, lip + opening + step, (halfWidth - 2 * step) * 2, step, lower);
            if (pose == 2)
                Pixels(center - 2 * step, lip + step, 4 * step, step * .7f,
                    mentor ? new Color32(216, 181, 149, 255) : new Color32(216, 159, 124, 255));
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

            _portraitSpeaker = speaker;
            _dialogPortrait.image = DialoguePortrait(speaker);
            _portraitStage.style.display = _dialogPortrait.image == null ? DisplayStyle.None : DisplayStyle.Flex;
            LayoutDialogue();
            _dialogFrame.MarkDirtyRepaint();
            _dialogMore.text = Thai(_loc.Current == Lang.TH ? "ต่อ" : "Next");
            _dialogText.style.fontSize = Mathf.Round(DialogueTextSize * Settings.TextScale);
            _dialogSpeaker.style.fontSize = Mathf.Round(SpeakerTextSize * Settings.TextScale);
            _dialogMore.style.fontSize = Mathf.Round(24 * Settings.TextScale);
            _speech.scrollOffset = Vector2.zero;
            RestPortrait();
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
            bool hasReplies = _firstDialogueReply != null;
            _nextLine.style.display = _timedDialogue || hasReplies ? DisplayStyle.None : DisplayStyle.Flex;
            _nextLine.style.opacity = Typing ? 0f : 1f;
            if (!Typing && hasReplies && _dialogueReplies.style.display == DisplayStyle.None)
            {
                _dialogueReplies.style.display = DisplayStyle.Flex;
                var first = _firstDialogueReply;
                first.schedule.Execute(() => { if (_firstDialogueReply == first && DialogOpen) first.Focus(); });
            }
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
            _dialogHighlight.style.height = Mathf.Round((DialogueTextSize + 5) * Settings.TextScale);
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
            _nextMouthPose = 0;
            _voicedPose = 0;
            SetPortraitSpeechPose(0);
        }

        void SetPortraitSpeechPose(int pose)
        {
            if (_speechPose == pose) return;
            _speechPose = pose;
            _portraitSpeech.MarkDirtyRepaint();
            bool guest = _portraitSpeaker is not ("mentor" or "you" or "orbi" or "sys");
            _portraitStage.style.translate = new Translate(0, guest && pose == 2 ? -3 : 0);
        }

        static int SpeechShape(string line, int start, int end)
        {
            char first = char.ToLowerInvariant(line[start]);
            if (first is 'm' or 'b' or 'p') return 0;
            for (int i = start; i < end; i++)
            {
                char c = char.ToLowerInvariant(line[i]);
                if (c is 'o' or 'u' or 'w' or 'โ' or 'อ' or 'ุ' or 'ู') return 3;
                if (c is 'a' or 'e' or 'ะ' or 'า' or 'ำ' or 'แ' or 'เ' or 'ั') return 2;
            }
            return 1;
        }

        void TickDialogue()
        {
            if (!Typing) return;
            int speed = Settings.TextSpeed;
            if (speed <= 0) { FinishDialogue(); return; }
            float now = Time.unscaledTime;
            float interval = 5.2f / speed;
            if (_timedDialogue) interval = Mathf.Min(interval, _timedTextInterval);
            bool voiced = false, changed = false;
            int shape = 0;
            int batch = 0;
            while (_shownClusters < _dialogEnds.Count && now >= _nextCluster && batch++ < 8)
            {
                int start = _shownClusters == 0 ? 0 : _dialogEnds[_shownClusters - 1];
                int end = _dialogEnds[_shownClusters++];
                char final = _fullLine[end - 1];
                if (char.IsLetterOrDigit(_fullLine[start]))
                {
                    voiced = true;
                    shape = SpeechShape(_fullLine, start, end);
                }
                float pause = (final == '.' || final == '!' || final == '?' || final == '\n') ? .09f : 0;
                _nextCluster += interval + pause;
                changed = true;
            }
            if (voiced)
            {
                if (!_timedDialogue) sfx?.PlayDialogueKey();
                _portraitBeatAt = now;
                if (now >= _nextMouthPose)
                {
                    _voicedPose = _portraitSpeaker is "mentor" or "you" ? shape : 1 + _shownClusters % 3;
                    _nextMouthPose = now + .09f;
                }
            }
            if (changed) ShowDialogueText();
            if (!Typing) { RestPortrait(); return; }
            bool speaking = _dialogPortrait.image != null && !Settings.ReducedMotion
                && now - _portraitBeatAt < Mathf.Max(.12f, interval * .86f);
            SetPortraitSpeechPose(speaking ? _voicedPose : 0);
        }
    }
}
