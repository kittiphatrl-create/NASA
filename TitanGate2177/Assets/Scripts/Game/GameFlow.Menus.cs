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
            _chapter = 0;
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
            _ui.ShowScreen(root =>
            {
                root.style.alignItems = Align.FlexStart;
                root.style.justifyContent = Justify.Center;
                var col = new VisualElement();
                col.style.marginLeft = 110;
                col.style.width = 700;
                col.style.backgroundColor = new Color(0.02f, 0.03f, 0.09f, 0.72f);
                GameUI.Border(col, new Color(0.26f, 0.89f, 1f, 0.6f), 2, 16);
                col.style.paddingLeft = col.style.paddingRight = 44; col.style.paddingTop = 34; col.style.paddingBottom = 30;

                var logo = _ui.L("TITAN GATE", 92, true, GameUI.CCyan);
                logo.style.marginBottom = -18;
                col.Add(logo);
                var year = _ui.L("2177", 64, true, GameUI.CAmber);
                col.Add(year);
                var sub = _ui.L(_loc.T("title.subtitle"), 30, true);
                sub.style.marginBottom = 18;
                col.Add(sub);

                Button Big(string key, Action a, bool primary = false, bool enabled = true, string extra = null)
                {
                    var b = _ui.B(_loc.T(key) + (extra != null ? "  " + extra : ""), a, 30, primary);
                    b.style.unityTextAlign = TextAnchor.MiddleLeft;
                    b.style.height = 62; b.style.marginLeft = 0; b.style.marginRight = 0;
                    b.SetEnabled(enabled);
                    col.Add(b);
                    return b;
                }

                var start = Big("menu.new", () => { Settings.ClearCheckpoint(); _ui.HideScreen(); StartCoroutine(StartChapter1()); }, true);
                start.name = "btn-start";
                Big("menu.continue", () => { _ui.HideScreen(); ContinueGame(); }, false, Settings.HasSave,
                    Settings.HasSave ? "(" + _loc.T("route." + Settings.SaveRoute + ".name") + ")" : null);
                Big("menu.cards", ShowCardBook, false, true, $"{CardsEverCount()}/{_cfg.cards.Count}");
                Big("menu.howto", ShowHowTo);
                Big("menu.settings", ShowSettings);
                Big("menu.sources", ShowSources);
                Big("menu.credits", ShowCredits);
                Big("menu.quit", () => { Application.Quit(); });
                var best = _ui.L(_loc.T("menu.stars", Settings.BestStars("mars")), 20, false, GameUI.CAmber);
                best.style.marginTop = 8;
                col.Add(best);

                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginTop = 14; row.style.alignItems = Align.Center;
                row.Add(_ui.B(_loc.Current == Lang.TH ? "English" : "ภาษาไทย", ToggleLang, 22));
                row.Add(_ui.L(_loc.T("menu.version"), 18, false, GameUI.CDim));
                col.Add(row);
                root.Add(col);
            });
        }

        void ContinueGame()
        {
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
            if (_busy || InCutscene || _chapter < 1 || _chapter > 2) return;
            Time.timeScale = 0f;
            _ui.ShowModal(p =>
            {
                p.style.width = 640;
                p.style.alignItems = Align.Stretch;
                _ui.Title(p, _loc.T("pause.title"));
                p.Add(_ui.B(_loc.T("pause.resume"), () => _ui.CloseModal(), 28, true));
                p.Add(_ui.B(_loc.T("menu.cards"), () => { _ui.CloseModal(); ShowCardBook(); }, 26));
                p.Add(_ui.B(_loc.T("menu.howto"), () => { _ui.CloseModal(); ShowHowTo(); }, 26));
                p.Add(_ui.B(_loc.T("menu.settings"), () => { _ui.CloseModal(); ShowSettings(); }, 26));
                p.Add(_ui.B(_loc.T("pause.restart"), () => { Time.timeScale = 1f; Restart(); }, 26));
                p.Add(_ui.B(_loc.T("pause.menu"), () => { Time.timeScale = 1f; RestartToTitle(); }, 26));
            }, true, () => Time.timeScale = 1f);
        }

        // ------------------------------------------------------------------ settings

        void ShowSettings()
        {
            bool paused = _chapter >= 1 && _chapter <= 2;
            if (paused) Time.timeScale = 0f;
            _ui.ShowModal(p =>
            {
                p.style.width = 900;
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
                for (int i = 1; i <= 4; i++) Item($"sources.observed.{i}");
                Head("sources.principle", GameUI.CCyan);
                for (int i = 1; i <= 5; i++) Item($"sources.principle.{i}");
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
