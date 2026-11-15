using System;
using System.Collections;
using System.Collections.Generic;
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
        static readonly bool BookCheckRequested = Array.IndexOf(Environment.GetCommandLineArgs(), "-bookcheck") >= 0;
        readonly List<(int from, int to)> _bookTurns = new List<(int, int)>();

        void TrackBookTurn(int from, int to)
        {
            if (BookCheckRequested) _bookTurns.Add((from, to));
        }

        static IEnumerator BookKey(Keyboard keyboard, Key key)
        {
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return new WaitForSecondsRealtime(.08f);
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
        }

        IEnumerator ClickBookTab(int tab)
        {
            var button = _ui.Root.Q<Button>("field-book-tab-" + tab);
            var pointer = InputSystem.AddDevice<Mouse>();
            try
            {
                var pos = button.worldBound.center;
                var panel = _ui.Root.worldBound;
                Vector2 screen = new Vector2(pos.x / panel.width * Screen.width, (1 - pos.y / panel.height) * Screen.height);
                pointer.MakeCurrent();
                InputSystem.QueueStateEvent(pointer, new MouseState { position = screen });
                yield return null;
                InputSystem.QueueStateEvent(pointer, new MouseState { position = screen }.WithButton(MouseButton.Left));
                yield return new WaitForSecondsRealtime(.09f);
                InputSystem.QueueStateEvent(pointer, new MouseState { position = screen });
                yield return new WaitForSecondsRealtime(.09f);
            }
            finally
            {
                if (pointer.added) InputSystem.RemoveDevice(pointer);
            }
        }

        IEnumerator WaitForBook()
        {
            float timeout = Time.unscaledTime + 2;
            while (_fieldBook != null && !_fieldBook.Settled && Time.unscaledTime < timeout)
                yield return null;
        }

        bool BookShows(string text) => _ui.Root.Q<VisualElement>("field-book")?.Query<Label>()
            .ToList().Any(l => l.text == GameUI.Thai(text)) == true;

        IEnumerator CheckEvacuationSupplies()
        {
            float timeout = Time.unscaledTime + 1;
            while (ActionLocked && Time.unscaledTime < timeout) yield return null;
            OpenFieldBook();
            yield return WaitForBook();
            if (_fieldBook != null)
            {
                _fieldBook.Select(FieldBook.Items);
                yield return WaitForBook();
                Check(BookShows(BookWord("ship.hands") + $"  {_sHands.Count}/{HandSlots}") &&
                      BookShows(BookWord("ship.pod") + $"  {_ldInv.Pod.Count}/{StowSlots}"),
                    "evacuation items match the cargo held and packed separately");
                yield return Shot("evacuation-cargo-th");
            }
            else Check(false, "evacuation book opens after collecting and loading supplies");
            CloseFieldBook();
            yield return new WaitForSecondsRealtime(.8f);
        }

        IEnumerator CheckBookAfterContinue()
        {
            OpenFieldBook();
            yield return WaitForBook();
            Check(BookShows(BookWord("day", _ld.Day, _ld.C.days)) && BookShows($"{_ld.Battery}/{_ld.C.batteryCap}"),
                "the reopened book shows the loaded day and battery");
            if (_fieldBook != null)
            {
                _fieldBook.Select(FieldBook.Mission);
                yield return WaitForBook();
                Check(BookShows(BookWord("radio", _ld.RadioRepair, _ld.C.radioRepairDays)) &&
                      BookShows(BookWord("signal", _ld.Signal, _ld.C.signalNeeded)),
                    "loaded book shows repairs and signals sent");
                yield return Shot("station-mission-after-continue");
            }
            CloseFieldBook();
            yield return new WaitForSecondsRealtime(.8f);
        }

        IEnumerator CheckFieldBook(string stage, bool station)
        {
            bool reduced = Settings.ReducedMotion;
            var language = _loc.Current;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                Settings.ReducedMotion = false;
                _loc.Current = Lang.TH;
                float clock = _sLeft;
                yield return BookKey(keyboard, Key.M);
                yield return WaitForBook();
                Check(_fieldBook?.Current == FieldBook.Status && _fieldBook.Settled, stage + " M opens to Status");
                if (_fieldBook == null) Log($"Book input state: eligible={CanOpenBook}, busy={_busy}, blocking={_ui.Blocking}, mode={_bMode}, active keyboard={Keyboard.current == keyboard}");
                if (_fieldBook == null) yield break;
                if (station)
                {
                    Check(BookShows(BookWord("day", _ld.Day, _ld.C.days)), "book shows the live day");
                    Check(BookShows($"{_ld.Battery}/{_ld.C.batteryCap}"), "book shows the live battery and capacity");
                    Check(!BookShows(BookWord("radio", _ld.RadioRepair, _ld.C.radioRepairDays)), "radio stays off Status");
                }
                Vector2 before = _bPos;
                yield return BookKey(keyboard, Key.D);
                Check(_bPos == before, "movement input cannot reach the scene while the book is open");
                yield return Shot(stage + "-status-th");
                if (stage == "evacuation") Check(Mathf.Abs(_sLeft - clock) < .35f, "reading the book pauses the evacuation clock");

                if (station)
                {
                    // Every ordered pair of tabs, with two real leaf turns for a skip.
                    foreach (int target in new[] { 1, 2, 1, 0, 2, 0 })
                    {
                        int from = _fieldBook.Current;
                        _bookTurns.Clear();
                        if (from == FieldBook.Status && target == FieldBook.Mission)
                        {
                            yield return ClickBookTab(target);
                            Check(_fieldBook.Requested == target, "mouse selects the Mission tab");
                            // Continue exercising the remaining routes even if a host has no working mouse.
                            _fieldBook.Select(target);
                        }
                        else _fieldBook.Select(target);
                        yield return WaitForBook();
                        var expected = new List<(int, int)>();
                        int direction = Math.Sign(target - from);
                        for (int page = from; page != target; page += direction)
                            expected.Add((page, page + direction));
                        Check(_fieldBook.Settled && _fieldBook.Current == target && _bookTurns.SequenceEqual(expected),
                            $"page turn {from} to {target}: {expected.Count} toward {(direction > 0 ? "left" : "right")}");
                        if (target == FieldBook.Mission && from == FieldBook.Status)
                        {
                            Check(BookShows(BookWord("radio", _ld.RadioRepair, _ld.C.radioRepairDays)), "mission shows live radio repairs");
                            Check(!BookShows(LdRes(TitanGate.Core.LastDock.Res.Food)), "mission has no food gauge");
                            yield return Shot(stage + "-mission-th");
                        }
                        if (target == FieldBook.Items && from == FieldBook.Mission)
                            yield return Shot(stage + "-items-th");
                    }
                    _fieldBook.Select(FieldBook.Items);
                    yield return null;
                    _fieldBook.Select(FieldBook.Mission);
                    yield return WaitForBook();
                    Check(_fieldBook.Current == FieldBook.Mission, "most recent tab request wins");
                    _fieldBook.Select(FieldBook.Items);
                    yield return null;
                }
                else
                {
                    yield return BookKey(keyboard, Key.RightArrow);
                    yield return WaitForBook();
                    Check(_fieldBook.Current == FieldBook.Mission, stage + " keyboard changes tab");
                    yield return Shot(stage + "-mission-th");
                }
                yield return BookKey(keyboard, Key.Escape);
                yield return new WaitForSecondsRealtime(.8f);
                Check(_fieldBook == null && !_ui.ScreenOpen, stage + " Escape closes book during play");

                _loc.Current = Lang.EN;
                Settings.ReducedMotion = true;
                OpenFieldBook();
                Check(_fieldBook?.Current == FieldBook.Status && _fieldBook.Settled, "reopen resets to Status with Reduced Motion");
                if (_fieldBook != null)
                {
                    yield return Shot(stage + "-status-en");
                    _fieldBook.Select(FieldBook.Items);
                    Check(_fieldBook.Current == FieldBook.Items && _fieldBook.Settled, "Reduced Motion switches immediately");
                    yield return Shot(stage + "-items-en");
                    yield return BookKey(keyboard, Key.M);
                    Check(_fieldBook == null, "M closes book");
                }
                yield return BookKey(keyboard, Key.Escape);
                Check(_ui.ModalOpen, "Escape opens Pause after the book closes");
                if (_pauseDeckOpen && stage != "evacuation" && PauseCheckRequested)
                {
                    yield return CheckPauseDeck(stage, keyboard);
                    yield break;
                }
                if (station)
                {
                    yield return new WaitForSecondsRealtime(.3f);
                    yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(_shotDir, stage + "-pause-en.png"));
                    yield return new WaitForSecondsRealtime(.2f);
                }
                float scale = _playFast ? 3f : 1f;
                _ui.CloseModal();
                Time.timeScale = scale;
            }
            finally
            {
                EndFieldBook();
                if (keyboard.added) InputSystem.RemoveDevice(keyboard);
                Settings.ReducedMotion = reduced;
                _loc.Current = language;
            }
        }
    }
}
