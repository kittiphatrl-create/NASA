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
            if (InCutscene || (_bRoot == null && (_busy || _chapter < 1 || _chapter > 2))) return;
            Time.timeScale = 0;
            _ui.ShowModal(p =>
            {
                p.style.width = 940; ThemePanel(p);
                _ui.Title(p, _loc.T("pause.title"));
                var body = new VisualElement(); body.style.flexDirection = FlexDirection.Row; body.style.marginTop = 15;
                var main = new VisualElement(); main.style.width = 490;
                void ButtonIn(VisualElement box, string key, Action action, bool primary = false)
                {
                    var button = _ui.B(_loc.T(key), action, 25, primary); StyleFlightButton(button, primary);
                    button.style.minHeight = primary ? 84 : 58; box.Add(button);
                }
                ButtonIn(main, "pause.resume", () => _ui.CloseModal(), true);
                ButtonIn(main, "menu.settings", () => { _ui.CloseModal(); ShowSettings(); });
                var aux = new VisualElement(); aux.style.flexGrow = 1; aux.style.marginLeft = 20;
                var badge = new Image { sprite = Art.ConditionFace(_life == null ? 0 : BMood()), scaleMode = ScaleMode.ScaleToFit }; badge.style.height = 80; aux.Add(badge);
                if (_life != null) aux.Add(_ui.L(BClockText(), 20, true, GameUI.CAmber));
                ButtonIn(aux, "menu.howto", () => { _ui.CloseModal(); ShowHowTo(); });
                ButtonIn(aux, "menu.cards", () => { _ui.CloseModal(); ShowCardBook(); });
                body.Add(main); body.Add(aux); p.Add(body);
                var footer = new VisualElement(); footer.style.flexDirection = FlexDirection.Row; footer.style.marginTop = 24;
                ButtonIn(footer, "pause.restart", () => { Time.timeScale = 1; Restart(); });
                ButtonIn(footer, "pause.menu", () => { Time.timeScale = 1; RestartToTitle(); }); p.Add(footer);
            }, true, () => Time.timeScale = 1);
        }

        // ------------------------------------------------------------------ settings

        void ShowSettings()
        {
            bool paused = _chapter >= 1 && _chapter <= 3;
            if (paused) Time.timeScale = 0f;
            _ui.ShowModal(p =>
            {
                p.style.width = 900; ThemePanel(p);
                _ui.Title(p, _loc.T("menu.settings"));
                void Row(string key, string value, Action minus, Action plus)
                {
                    var r = new VisualElement(); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginBottom = 4;
                    var name = _ui.L(_loc.T(key), 26); name.style.width = 380; r.Add(name);
                    var m = _ui.B("−", () => { minus(); _ui.RebuildModal(); }, 28); m.style.width = 70; r.Add(m);
                    var v = _ui.L(value, 26, true, GameUI.CCyan); v.style.width = 190; v.style.unityTextAlign = TextAnchor.MiddleCenter; r.Add(v);
                    var pl = _ui.B("+", () => { plus(); _ui.RebuildModal(); }, 28); pl.style.width = 70; r.Add(pl);
                    p.Add(r);
                }
                string Pct(float f) => Mathf.RoundToInt(f * 100) + "%";
                Row("set.master", Pct(Settings.Master), () => Settings.Master -= 0.1f, () => Settings.Master += 0.1f);
                Row("set.effects", Pct(Settings.Effects), () => Settings.Effects -= 0.1f, () => Settings.Effects += 0.1f);
                Row("set.ambience", Pct(Settings.Ambience), () => Settings.Ambience -= 0.1f, () => Settings.Ambience += 0.1f);
                float[] sizes = { 1f, 1.15f, 1.3f };
                int si = Mathf.Max(0, Array.FindIndex(sizes, x => Mathf.Approximately(x, Settings.TextScale)));
                Row("set.textsize", Pct(sizes[si]), () => { Settings.TextScale = sizes[Mathf.Max(0, si - 1)]; RefreshAll(); },
                    () => { Settings.TextScale = sizes[Mathf.Min(sizes.Length - 1, si + 1)]; RefreshAll(); });
                int[] speeds = { 35, 70, 140, 0 };
                string[] speedKeys = { "set.speed.slow", "set.speed.normal", "set.speed.fast", "set.speed.instant" };
                int sp = Mathf.Max(0, Array.IndexOf(speeds, Settings.TextSpeed));
                Row("set.textspeed", _loc.T(speedKeys[sp]), () => Settings.TextSpeed = speeds[Mathf.Max(0, sp - 1)], () => Settings.TextSpeed = speeds[Mathf.Min(speeds.Length - 1, sp + 1)]);
                Row("set.motion", _loc.T(Settings.ReducedMotion ? "set.on" : "set.off"), () => Settings.ReducedMotion = false, () => Settings.ReducedMotion = true);
                Row("set.fullscreen", _loc.T(Screen.fullScreen ? "set.on" : "set.off"), () => Screen.fullScreen = false, () => Screen.fullScreen = true);
                var lang = _ui.B(_loc.T("set.language") + ":  " + (_loc.Current == Lang.TH ? "ภาษาไทย" : "English"), () => { ToggleLang(); }, 26);
                lang.style.marginTop = 10;
                p.Add(lang);
                var note = _ui.L(_loc.T("set.note"), 20, false, GameUI.CDim); note.style.marginTop = 8;
                p.Add(note);
            }, true, () => { if (paused) Time.timeScale = 1f; if (_chapter == 0) BuildMainMenu(); });
        }

        // ------------------------------------------------------------------ how to play / credits

        void ShowHowTo()
        {
            _ui.ShowModal(p =>
            {
                p.style.width = 1120;
                _ui.Title(p, _loc.T("menu.howto"));
                var scroll = new ScrollView(ScrollViewMode.Vertical);
                scroll.style.maxHeight = 640;
                for (int i = 1; i <= 11; i++)
                {
                    var box = new VisualElement();
                    box.style.flexDirection = FlexDirection.Row; box.style.marginBottom = 10;
                    var n = _ui.L(i.ToString(), 30, true, GameUI.CAmber); n.style.width = 46;
                    box.Add(n);
                    var c = new VisualElement(); c.style.flexShrink = 1;
                    c.Add(_ui.L(_loc.T($"howto.{i}.title"), 26, true, GameUI.CCyan));
                    c.Add(_ui.L(_loc.T($"howto.{i}.body"), 23));
                    box.Add(c);
                    scroll.Add(box);
                }
                p.Add(scroll);
            });
        }

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

        void ShowCardBook()
        {
            var have = CardsEver();
            _ui.ShowModal(p =>
            {
                p.style.width = 1180;
                _ui.Title(p, _loc.T("ui.codex_title", have.Count, _cfg.cards.Count));
                var scroll = new ScrollView(ScrollViewMode.Vertical);
                scroll.style.height = 620;
                string lastGroup = null;
                foreach (var c in _cfg.cards)
                {
                    string group = "codex.group." + c.chapter;
                    if (group != lastGroup)
                    {
                        lastGroup = group;
                        var g = _ui.L(_loc.T(group), 24, true, GameUI.CAmber);
                        g.style.marginTop = 8; g.style.marginBottom = 4;
                        scroll.Add(g);
                    }
                    bool got = have.Contains(c.id);
                    var box = new VisualElement();
                    box.style.marginBottom = 10;
                    box.style.backgroundColor = GameUI.CPanel2;
                    GameUI.Border(box, got ? GameUI.CCyanD : new Color(0.25f, 0.28f, 0.4f), 2, 8);
                    box.style.paddingLeft = box.style.paddingRight = 14; box.style.paddingTop = box.style.paddingBottom = 8;
                    if (got)
                    {
                        box.Add(_ui.L(_loc.T($"card.{c.id}.title"), 26, true, GameUI.CCyan));
                        box.Add(_ui.L(_loc.T($"card.{c.id}.body"), 23));
                    }
                    else box.Add(_ui.L(_loc.T("ui.card_locked"), 24, false, GameUI.CDim));
                    scroll.Add(box);
                }
                p.Add(scroll);
            }, true, () => { if (_chapter == 0) BuildMainMenu(); });
        }
    }
}
