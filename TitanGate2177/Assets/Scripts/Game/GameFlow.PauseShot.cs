using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;
using Key = UnityEngine.InputSystem.Key;
using MouseButton = UnityEngine.InputSystem.LowLevel.MouseButton;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        static readonly bool PauseCheckRequested = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-pausecheck") >= 0;

        IEnumerator CheckPauseDeck(string stage, Keyboard keyboard)
        {
            var lang = _loc.Current;
            var mouse = InputSystem.AddDevice<Mouse>();
            float effects = Settings.Effects;
            float textScale = Settings.TextScale;
            string savedCards = Settings.CardsEver;
            bool reduced = Settings.ReducedMotion;
            string frames = stage == "station" ? Path.Combine(_shotDir, "pause-frames") : null;
            StreamWriter times = null;
            int frame = 0;
            float start = Time.unscaledTime;
            if (frames != null)
            {
                Directory.CreateDirectory(frames);
                times = new StreamWriter(Path.Combine(frames, "times.csv"));
            }
            IEnumerator Record(float seconds)
            {
                float until = Time.unscaledTime + seconds;
                do
                {
                    yield return new WaitForEndOfFrame();
                    if (times != null)
                    {
                        ScreenCapture.CaptureScreenshot(Path.Combine(frames, $"frame_{frame:00000}.png"));
                        times.WriteLine($"{frame},{(Time.unscaledTime - start).ToString("F5", CultureInfo.InvariantCulture)}");
                        frame++;
                    }
                    yield return new WaitForSecondsRealtime(.045f);
                } while (Time.unscaledTime < until);
            }
            void Point(Vector2 p, bool down)
            {
                var root = _ui.Root.worldBound;
                var position = new Vector2((p.x - root.x) / root.width * Screen.width,
                    (1 - (p.y - root.y) / root.height) * Screen.height);
                mouse.MakeCurrent();
                var state = new MouseState { position = position };
                if (down) state = state.WithButton(MouseButton.Left);
                InputSystem.QueueStateEvent(mouse, state);
            }
            IEnumerator Click(string name)
            {
                var element = _ui.Root.Q<VisualElement>(name);
                Check(element != null, "button exists: " + name);
                if (element == null) yield break;
                var p = element.worldBound.center;
                Point(p, false); yield return Record(.28f);
                Point(p, true); yield return Record(.10f);
                Point(p, false); yield return Record(.15f);
            }
            IEnumerator Still(string name)
            {
                yield return Record(.44f);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(_shotDir, stage + "-pause-" + name + ".png"));
                yield return new WaitForSecondsRealtime(.13f);
            }
            void Page(PausePage page) =>
                Check(_pauseDeckOpen && _pausePage == page && Time.timeScale == 0, stage + " paused on " + page);
            try
            {
                Settings.ReducedMotion = false;
                _loc.Current = stage == "station" ? Lang.TH : Lang.EN;
                _ui.RefreshText();
                yield return Still("menu");
                Page(PausePage.Menu);
                Settings.Effects = .7f;
                yield return Click("pause-Settings");
                Page(PausePage.Settings);
                var slider = _ui.Root.Q<Slider>("pause-effects");
                Check(slider != null && Mathf.Approximately(slider.value, .7f), "settings read saved effects");
                yield return Still("settings");
                if (slider != null)
                {
                    var bounds = slider.worldBound;
                    Vector2 Location(float amount) => new Vector2(Mathf.Lerp(bounds.xMin + 18, bounds.xMax - 18, amount), bounds.center.y);
                    Point(Location(.7f), true); yield return Record(.11f);
                    for (int i = 1; i <= 9; i++)
                    {
                        Point(Location(Mathf.Lerp(.7f, .55f, i / 9f)), true);
                        yield return Record(.06f);
                    }
                    Point(Location(.55f), false); yield return Record(.27f);
                    Check(Mathf.Abs(Settings.Effects - .55f) <= .04f, "drag updates effects while paused");
                    slider.Focus();
                    float current = Settings.Effects;
                    yield return BookKey(keyboard, Key.RightArrow);
                    Check(Settings.Effects > current, "focused slider accepts the arrow key");
                }
                yield return BookKey(keyboard, Key.Escape);
                Page(PausePage.Menu);
                yield return Record(.24f);
                Check(_ui.Root.focusController.focusedElement == _ui.Root.Q<Button>("pause-Settings"),
                    "back restores focus to Settings");
                yield return Click("pause-Help");
                Page(PausePage.Help);
                yield return Still("help");
                yield return Click("pause-back");
                Page(PausePage.Menu);
                yield return Click("pause-Cards");
                Page(PausePage.Cards);
                yield return Still("cards");
                var groups = _ui.Root.Query<Button>().ToList().FindAll(button => button.name?.StartsWith("pause-group-") == true);
                Check(groups.Count == _cfg.cards.Select(c => c.chapter).Distinct().Count(), "card groups match the catalog");
                if (groups.Count > 1)
                {
                    yield return Click(groups[1].name);
                    yield return Still("cards-mars");
                }
                yield return Click("pause-back");
                Page(PausePage.Menu);
                yield return Click("pause-resume");
                Check(!_pauseDeckOpen && !_ui.ModalOpen && Time.timeScale > 0, "resume restores gameplay");
                times?.Dispose();
                times = null;
                if (stage != "station") yield break;

                yield return BookKey(keyboard, Key.Escape);
                yield return Click("pause-Settings");
                yield return Click("pause-size-2");
                Check(Mathf.Approximately(Settings.TextScale, 1.3f), "text scale uses the existing 130% setting");
                yield return Still("settings-large");
                yield return Click("pause-motion");
                Check(Settings.ReducedMotion, "reduced motion is written to Settings");
                Settings.TextScale = textScale;
                yield return BookKey(keyboard, Key.Escape);

                var card = _cfg.cards.FirstOrDefault();
                if (card != null)
                {
                    Settings.CardsEver = card.id;
                    yield return Click("pause-Cards");
                    yield return Click("pause-card-" + card.id);
                    Check(_pauseOpenCard == card.id, "unlocked card opens the actual localized article");
                    yield return Still("card-detail");
                    yield return BookKey(keyboard, Key.Escape);
                    Check(_pausePage == PausePage.Cards && _pauseOpenCard == null && Time.timeScale == 0,
                        "back from article returns to the card collection");
                    yield return BookKey(keyboard, Key.Escape);
                }
                yield return BookKey(keyboard, Key.Escape);
                Check(!_pauseDeckOpen && Time.timeScale > 0, "Escape resumes after settings and card navigation");
                float gameplaySpeed = Time.timeScale;
                ShowCardBook();
                Check(Mathf.Approximately(Time.timeScale, gameplaySpeed), "direct card hotkey keeps its original time scale");
                yield return BookKey(keyboard, Key.Escape);
                Check(!_pauseDeckOpen, "direct card hotkey returns directly to the game");
            }
            finally
            {
                times?.Dispose();
                if (_pauseDeckOpen) _ui.CloseModal();
                if (mouse.added) InputSystem.RemoveDevice(mouse);
                Settings.Effects = effects;
                Settings.TextScale = textScale;
                Settings.CardsEver = savedCards;
                Settings.ReducedMotion = reduced;
                _loc.Current = lang;
            }
        }
    }
}
