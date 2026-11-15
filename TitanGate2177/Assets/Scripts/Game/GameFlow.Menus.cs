// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System;
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>Main menu over the animated key art, pause menu, settings, how to play, credits, card book.</summary>
    public partial class GameFlow
    {
        /// <summary>Set before a reload to start straight from the Chapter 2 checkpoint.</summary>
        public static bool ContinueFromSave;

        // ------------------------------------------------------------------ main menu

        void ShowMainMenu()
        {
            _chapter = 0; _menuLaunching = false;
            Time.timeScale = 1f;
            LdScore(StageMusicPlayer.Cue.Opening);
            ClearWorld();
            _player.gameObject.SetActive(false);
            _orbi.gameObject.SetActive(false);
            _ui.SetHudVisible(false);
            _sfx.Loop("hum", false);
            BuildTitleStage();
            BuildMainMenu();
            _ui.FadeTo(1f, 0f);
            _ui.FadeTo(0f, 1.2f);
        }

        void BuildMainMenu()
        {
            if (LastDockMode && LdBuildTitle()) return;   // the living LAST DOCK title (GameFlow.LdTitle.cs)
            _ui.ShowScreen(root =>
            {
                root.style.alignItems = Align.FlexStart; root.style.justifyContent = Justify.Center;
                if (LastDockMode && LdKeyArt("title_keyart") != null) root.Add(LdArtBackdrop("title_keyart", 0.95f));   // Blender key art: the clay cadet at the Mars base
                else { var shade = new VisualElement { pickingMode = PickingMode.Ignore }; shade.style.position = Position.Absolute; shade.style.left = 0; shade.style.top = 0; shade.style.bottom = 0; shade.style.width = Length.Percent(55); shade.style.backgroundImage = new StyleBackground(Art.TitleReadabilityFade()); root.Add(shade); }
                var col = new VisualElement(); col.style.marginLeft = 95; col.style.width = 690;
                col.style.paddingLeft = col.style.paddingRight = 34; col.style.paddingTop = col.style.paddingBottom = 26;
                col.style.backgroundColor = Color.clear;
                GameUI.Border(col, Color.clear, 0, 0);
                var logo = _ui.L(LastDockMode ? "LAST DOCK" : "TITAN GATE", 84, true, GameUI.CText); logo.style.marginTop = 8; if (LastDockMode) LdFont(logo); else _ui.DisplayFont(logo); col.Add(logo);
                var year = _ui.L(LastDockMode ? "MARS · 2177" : "2 1 7 7", 32, true, GameUI.CAmber); year.style.marginBottom = 8; if (LastDockMode) LdFont(year); else _ui.DisplayFont(year); col.Add(year);
                col.Add(_ui.L(_loc.T("title.subtitle"), 25));
                var rule = new VisualElement(); rule.style.height = 3; rule.style.width = 90; rule.style.backgroundColor = GameUI.CAmber; rule.style.marginTop = 12; rule.style.marginBottom = 25; col.Add(rule);
                Button Main(string key, Action action, bool primary = false, bool enabled = true)
                {
                    var button = _ui.B(_loc.T(key), () => { if (!_menuLaunching) action(); }, 28, primary); button.style.height = 66;
                    button.style.marginLeft = button.style.marginRight = 0; button.style.unityTextAlign = TextAnchor.MiddleLeft;
                    StyleFlightButton(button, primary); button.SetEnabled(enabled); col.Add(button); return button;
                }
                var start = Main("menu.new", () => MenuDepart(() => { Settings.ClearCheckpoint(); _ui.HideScreen(); StartCoroutine(StartChapter1()); }), true); start.name = "btn-start";
                Main("menu.continue", () => { _ui.HideScreen(); ContinueGame(); }, false, Settings.HasSave);
                if (!LastDockMode) Main("menu.lifeboat", () => { _ui.HideScreen(); StartLifeboat(); });
                var tools = new VisualElement(); tools.style.flexDirection = FlexDirection.Row; tools.style.flexWrap = Wrap.Wrap; tools.style.marginTop = 16;
                void Small(string key, Action action)
                {
                    var button = _ui.B(_loc.T(key), () => { if (!_menuLaunching) action(); }, 22); button.style.width = 295; button.style.minHeight = 48; StyleFlightButton(button); tools.Add(button);
                }
                Small("menu.howto", ShowHowTo); Small("menu.settings", ShowSettings);
                Small("menu.cards", ShowCardBook); Small("menu.sources", ShowSources);
                Small("menu.credits", ShowCredits); Small("menu.quit", () => Application.Quit());
                col.Add(tools);
                var footer = new VisualElement(); footer.style.flexDirection = FlexDirection.Row; footer.style.alignItems = Align.Center; footer.style.marginTop = 14;
                footer.Add(_ui.B(_loc.Current == Lang.TH ? "English" : "ภาษาไทย", ToggleLang, 20));
                col.Add(footer);
                root.Add(col);
                if (!LastDockMode) AddTitleInteractions(root);
                start.Focus();
            });
        }

        void ContinueGame()
        {
            if (LastDockMode && LdContinue()) return;   // LAST DOCK: replay the saved run (GameFlow.Dinner.cs)
            // an older save written at the end of the scramble: go on into LAST DOCK, not the retired diary/outpost
            if (LastDockMode && Settings.SaveRoute == "lifeboat") { LdContinueFromStowed(Settings.SaveItems); return; }
            if (Settings.SaveRoute == "lifeboat") { _lbContinueDay = HasDaySave; StartLifeboat(Settings.SaveItems, Settings.SavePet); return; }
            _cabinO2 = Settings.SaveCabin;
            _stowed.Clear();
            _stowed.AddRange(Settings.SaveItems);
            foreach (var c in new[] { "titan", "debris", "whipple", "deltav" }) _codex.Unlock(c);
            _player.gameObject.SetActive(true);
            _orbi.gameObject.SetActive(true);
            _ui.SetHudVisible(true);
            StartCoroutine(StartChapter2(Settings.SaveRoute));
        }

        public void RestartFromCheckpoint()
        {
            ContinueFromSave = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ------------------------------------------------------------------ pause

        void OpenPause()
        {
            if (InCutscene || _ui.Blocking || (_bRoot == null && (_busy || _chapter < 1 || _chapter > 2))) return;
            OpenPauseDeck(PausePage.Menu);
        }

        void ShowSettings() => OpenPauseDeck(PausePage.Settings);
        void ShowHowTo() => OpenPauseDeck(PausePage.Help);

        /// <summary>
        /// Where the science comes from, in three kinds the player can tell apart: measured by NASA,
        /// a NASA scientific principle, or made up for the game (event sizes, penalties, the 7-day rescue).
        /// </summary>
        void ShowSources()
        {
            _ui.ShowModal(p =>
            {
                p.style.width = 1240;
                _ui.Title(p, _loc.T("sources.title"));
                var scroll = new ScrollView(ScrollViewMode.Vertical);
                scroll.style.maxHeight = 680;
                void Head(string key, Color c) { var l = _ui.L(_loc.T(key), 26, true, c); l.style.marginTop = 10; scroll.Add(l); }
                void Item(string key) { var l = _ui.L("· " + _loc.T(key), 21, false, GameUI.CText); l.style.whiteSpace = WhiteSpace.Normal; l.style.marginTop = 3; scroll.Add(l); }
                Head("sources.observed", GameUI.CGreen);
                for (int i = 1; i <= 5; i++) Item($"sources.observed.{i}");
                Head("sources.principle", GameUI.CCyan);
                for (int i = 1; i <= 6; i++) Item($"sources.principle.{i}");
                Head("sources.simulated", GameUI.CAmber);
                for (int i = 1; i <= 3; i++) Item($"sources.simulated.{i}");
                p.Add(scroll);
            });
        }

        void ShowCredits()
        {
            _ui.ShowModal(p =>
            {
                p.style.width = 980;
                _ui.Title(p, _loc.T("menu.credits"));
                for (int i = 1; i <= 5; i++)
                {
                    p.Add(_ui.L(_loc.T($"credits.{i}.title"), 24, true, GameUI.CAmber));
                    var b = _ui.L(_loc.T($"credits.{i}.body"), 23); b.style.marginBottom = 10;
                    p.Add(b);
                }
            });
        }

        // ------------------------------------------------------------------ card book (kept between runs)

        HashSet<string> CardsEver()
        {
            var set = new HashSet<string>(Settings.CardsEver.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            foreach (var c in _codex.Cards) set.Add(c);
            return set;
        }

        int CardsEverCount() => CardsEver().Count;

        void RememberCard(string id)
        {
            var set = CardsEver();
            set.Add(id);
            Settings.CardsEver = string.Join(",", set);
        }

        void ShowCardBook() => OpenPauseDeck(PausePage.Cards);
    }
}
