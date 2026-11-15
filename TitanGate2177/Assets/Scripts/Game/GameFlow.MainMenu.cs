// LAST DOCK main menu: layout, focus and input, intro sequence, transitions and menu audio.
// Components live in Scripts/Game/Menu; all sizes and colours come from MenuTokens.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using TitanGate.Game.Menu;
using T = TitanGate.Game.Menu.MenuTokens;
using Lang = TitanGate.Core.Lang;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        static bool s_mmIntroDone;
        static AudioSource s_mmWind, s_mmMusic;
        VisualElement _mmHost;   // the live menu screen; null once the player has left the menu
        string _mmFocusId;
        MenuInput _mmInput;

        static Texture2D MmPng(string name)
        {
            var ta = Resources.Load<TextAsset>("Sprites/KeyArt/" + name + ".png");
            if (ta == null) return null;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, true) { filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp, name = name };
            t.LoadImage(ta.bytes);
            return t;
        }

        /// <summary>Pixel bounds of the texels at or above an alpha threshold (texture space, y up).</summary>
        static RectInt MmAlphaBounds(Texture2D t, float threshold)
        {
            var px = t.GetPixels32(); byte th = (byte)(threshold * 255);
            int x0 = t.width, y0 = t.height, x1 = -1, y1 = -1;
            for (int y = 0; y < t.height; y++)
                for (int x = 0; x < t.width; x++)
                    if (px[y * t.width + x].a >= th) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
            return x1 < 0 ? new RectInt(0, 0, t.width, t.height) : new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        }

        /// <summary>Outer radius of the ring art (fraction of its half-size) and the clamp's anchor pixel, from menu_ring.json.</summary>
        static (float ringOuter, Vector2 clampAnchor, Vector2 clampLight, Vector2 clampTip) MmRingInfo()
        {
            var ta = Resources.Load<TextAsset>("Sprites/KeyArt/menu_ring");
            if (ta == null) return (0.97f, Vector2.zero, Vector2.zero, Vector2.zero);
            try { return MenuPointsJson.ParseRing(ta.text); } catch (Exception e) { Debug.LogWarning("menu_ring.json: " + e.Message); return (0.97f, Vector2.zero, Vector2.zero, Vector2.zero); }
        }

        /// <summary>Load menu_scene_NAME.json and its layer images. Null if the scene was not rendered.</summary>
        static SceneSpec MmSceneSpec(string name)
        {
            var ta = Resources.Load<TextAsset>("Sprites/KeyArt/menu_scene_" + name);
            if (ta == null) return null;
            SceneSpec spec;
            try { spec = MenuPointsJson.ParseScene(ta.text); } catch (Exception e) { Debug.LogWarning("menu_scene_" + name + ".json: " + e.Message); return null; }
            foreach (var l in spec.Layers) l.Tex = MmArt(l.File);
            spec.Layers.RemoveAll(l => l.Tex == null);
            return spec.Layers.Count > 0 ? spec : null;
        }

        /// <summary>The first key art (one background and one astronaut layer), used if the scene renders are missing.</summary>
        static SceneSpec MmLegacySpec()
        {
            var bg = LdKeyArt("menu_bg") ?? LdKeyArt("title_bg");
            if (bg == null) return null;
            var spec = new SceneSpec { Name = "legacy", Aspect = (float)bg.width / Mathf.Max(1, bg.height) };
            spec.Layers.Add(new SceneLayer { Name = "bg", Depth = 0.25f, Tex = bg });
            var cadet = MmPng("menu_cadet") ?? MmPng("title_cadet");
            if (cadet != null) spec.Layers.Add(new SceneLayer { Name = "cadet", Role = "cadet", Depth = 0.5f, Tex = cadet });
            var pts = MmPoints();
            if (pts != null)
            {
                spec.Sun = new ScenePoint { P = pts.sun, Layer = "bg", Glow = pts.sunGlow };
                for (int i = 0; i < pts.beacons.Count; i++) spec.Beacons.Add(new ScenePoint { P = pts.beacons[i], Layer = "bg", Color = i < pts.beaconColors.Count ? pts.beaconColors[i] : new Color(1f, 0.55f, 0.25f) });
                spec.Airlock = pts.airlock;
            }
            return spec;
        }

        /// <summary>Decode a key-art image stored as TextAsset bytes (file name like "menu_mars_sky.jpg").</summary>
        static Texture2D MmArt(string file)
        {
            var ta = Resources.Load<TextAsset>("Sprites/KeyArt/" + file);
            if (ta == null) { Debug.LogWarning("Menu art missing: " + file); return null; }
            bool png = file.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
            var t = new Texture2D(2, 2, png ? TextureFormat.RGBA32 : TextureFormat.RGB24, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = file };
            t.LoadImage(ta.bytes, true);   // no CPU copy kept: the menu never reads these pixels back
            return t;
        }

        static MenuPoints MmPoints()
        {
            var ta = Resources.Load<TextAsset>("Sprites/KeyArt/menu_points");
            if (ta == null) return null;
            try { return MenuPointsJson.Parse(ta.text); } catch (Exception e) { Debug.LogWarning("menu_points.json: " + e.Message); return null; }
        }

        string MmAgo(DateTime utc)
        {
            var d = DateTime.UtcNow - utc;
            if (d.TotalMinutes < 2) return _loc.T("mm.ago.now");
            if (d.TotalHours < 1) return _loc.T("mm.ago.min", (int)d.TotalMinutes);
            if (d.TotalDays < 1) return _loc.T("mm.ago.hour", (int)d.TotalHours);
            return _loc.T("mm.ago.day", (int)d.TotalDays);
        }

        /// <summary>Build the main menu. Returns false only if no key art exists (the old menu is used then).</summary>
        bool BuildMainMenuV2()
        {
            bool hasSaveNow = LdHasSave();
            // the background follows the story: aboard the Kestrel before a run, the Mars base once a run is saved
            var spec = MmSceneSpec(hasSaveNow ? "mars" : "ship") ?? MmLegacySpec();
            if (spec == null) return false;
            var logoTex = MmPng("menu_logo");
            LdMusicEnsure();
            _mmInput ??= new MenuInput();
            bool reduced = Settings.ReducedMotion;
            bool playIntro = !s_mmIntroDone;
            s_mmIntroDone = true;
            bool hasSave = LdHasSave();

            _ui.ShowScreen(screen =>
            {
                screen.style.alignItems = Align.Stretch; screen.style.justifyContent = Justify.FlexStart; screen.style.backgroundColor = T.BgDeep;
                var host = T.Fill(); host.pickingMode = PickingMode.Position; screen.Add(host);   // schedules live on host, so they stop when the screen changes

                // ---- scene
                var scene = new MenuScene(spec);
                scene.PushTo = spec.Airlock;
                host.Add(scene.Root);

                // ---- logo lockup, top-left in the safe area
                var col = new VisualElement { pickingMode = PickingMode.Ignore, name = "mm-column" };
                col.style.position = Position.Absolute; col.style.left = Length.Percent(T.Safe * 100); col.style.top = Length.Percent(15);
                col.style.transformOrigin = new TransformOrigin(0, 0);
                host.Add(col);
                // logo lockup
                var logoMask = new VisualElement { pickingMode = PickingMode.Ignore }; logoMask.style.overflow = Overflow.Hidden;
                float logoH = T.TitleSize * 1.05f, logoW;
                VisualElement logo;
                if (logoTex != null)
                {
                    // The Blender lockup carries glow padding: trim to the solid letters so the logo lines up with the column.
                    var ink = MmAlphaBounds(logoTex, 0.5f);
                    float scale = 840f / Mathf.Max(1, ink.width);                  // letters about 840 px wide at 1080p: clear of the ship window frame
                    const float glowPad = 18f;
                    logoW = ink.width * scale + glowPad * 2; logoH = ink.height * scale + glowPad * 2;
                    logo = new Image { image = logoTex, scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
                    logo.style.position = Position.Absolute;
                    logo.style.width = logoTex.width * scale; logo.style.height = logoTex.height * scale;
                    logo.style.left = glowPad - ink.xMin * scale; logo.style.top = glowPad - (logoTex.height - ink.yMax) * scale;
                }
                else
                {
                    // text fallback: two words with an explicit, even gap (the font's space is too narrow next to its swashes)
                    var row = new VisualElement { pickingMode = PickingMode.Ignore }; row.style.flexDirection = FlexDirection.Row;
                    foreach (var word in new[] { "LAST", "DOCK" })
                    {
                        var l = T.Text(word, MenuFont.Display, T.TitleSize * 0.8f, T.TextPrimary, false);
                        l.style.whiteSpace = WhiteSpace.NoWrap; l.style.letterSpacing = T.DisplayTracking;
                        l.style.textShadow = new TextShadow { offset = new Vector2(0, 2), blurRadius = 22, color = T.WithAlpha(T.AccentHot, 0.45f) };
                        if (word == "DOCK") l.style.marginLeft = T.TitleSize * 0.8f * 0.32f;
                        row.Add(l);
                    }
                    logo = row; logoW = 980; logoH = T.TitleSize * 0.95f;
                }
                if (logoTex == null) { logo.style.position = Position.Absolute; logo.style.left = 0; logo.style.top = 0; logo.style.width = logoW; logo.style.height = logoH; }
                logoMask.style.height = logoH; logoMask.style.width = logoW; logoMask.style.marginLeft = logoTex != null ? -18 : -6; logoMask.style.marginTop = logoTex != null ? -18 : 0;
                logoMask.Add(logo);
                var sheen = new VisualElement { pickingMode = PickingMode.Ignore }; sheen.style.position = Position.Absolute; sheen.style.top = -logoH * 0.2f;
                sheen.style.height = logoH * 1.4f; sheen.style.width = logoW * 0.22f; sheen.style.backgroundImage = new StyleBackground(MenuFx.Radial());
                sheen.style.unityBackgroundImageTintColor = new Color(1f, 0.93f, 0.8f, 0.32f); sheen.style.opacity = 0; logoMask.Add(sheen);
                col.Add(logoMask);
                var rule = new VisualElement { pickingMode = PickingMode.Ignore }; rule.style.height = 2; rule.style.width = 0; rule.style.marginTop = T.S1;
                rule.style.backgroundColor = T.WithAlpha(T.Accent, 0.85f); col.Add(rule);

                // ---- the docking ring menu (sits under the logo layer)
                var (ringOuter, clampAnchor, clampLight, clampTip) = MmRingInfo();
                var dial = new DockDial(MmPng("menu_ring"), MmPng("menu_clamp"), ringOuter, clampAnchor, clampLight, clampTip);
                MenuAudio.Init(gameObject);
                dial.Ticked += () => MenuAudio.Tick(dial.Speed);
                dial.Docked += _ => MenuAudio.Play("lock", 0.65f);
                host.Insert(host.IndexOf(col), dial);
                var tooltip = new MenuTooltip();
                var (saveDay, saveAt) = LdSaveInfo();
                int days = TitanGate.Core.LastDock.StationConfig.Game().days;
                string contSub = !hasSave ? _loc.T("mm.continue.none")
                    : saveDay > 0 && saveAt.HasValue ? _loc.T("mm.continue.day", saveDay, days, MmAgo(saveAt.Value)) : _loc.T("mm.continue.saved");
                DialItem cont = null, start = null;
                void AddCont() => cont = dial.AddItem("continue", _loc.T("mm.continue"), contSub);
                void AddStart() => start = dial.AddItem("start", _loc.T(hasSave ? "mm.new" : "mm.start"), _loc.T("mm.start.sub"));
                if (hasSave) { AddCont(); AddStart(); } else { AddStart(); AddCont(); }
                if (!hasSave) { cont.SetInteractable(false); cont.Tooltip = _loc.T("mm.continue.tip"); }
                DialItem Entry(string id, string key, Action act) { var it = dial.AddItem(id, _loc.T(key), null); it.OnSubmit = act; return it; }
                Entry("log", "mm.log", () => MmOpenSub(ShowHowTo));
                Entry("codex", "mm.codex", () => MmOpenSub(ShowCardBook));
                Entry("sources", "mm.sources", () => MmOpenSub(ShowSources));
                Entry("settings", "mm.settings", () => MmOpenSub(ShowSettings));
                Entry("credits", "mm.credits", () => MmOpenSub(ShowCredits));
                var quit = Entry("quit", "mm.quit", () => { _sfx.Play("close"); Application.Quit(); });
                void Fit()
                {
                    float w = host.resolvedStyle.width, h = host.resolvedStyle.height;
                    dial.Layout(w, h);
                    float s = Mathf.Min(1f, h / 1080f); col.style.scale = new Scale(new Vector3(s, s, 1));
                }
                host.RegisterCallback<GeometryChangedEvent>(_ => Fit());

                // ---- top-right chips
                var chips = new VisualElement { pickingMode = PickingMode.Ignore }; chips.style.position = Position.Absolute; chips.style.flexDirection = FlexDirection.Row;
                chips.style.top = Length.Percent(T.Safe * 100 * 1.6f); chips.style.right = Length.Percent(T.Safe * 100); host.Add(chips);
                var langChip = new MenuChip("lang", "globe", _loc.Current == Lang.TH ? "TH" : "EN") { Tooltip = _loc.T("mm.lang.tip") };
                langChip.OnSubmit = () => { _sfx.PlayVaried("click"); ToggleLang(); };
                var soundChip = new MenuChip("sound", MmMuted ? "mute" : "sound", "") { Tooltip = _loc.T(MmMuted ? "mm.muted.tip" : "mm.sound.tip") };
                soundChip.OnSubmit = () =>
                {
                    MmMuted = !MmMuted; MmApplyListener();
                    soundChip.Set(MmMuted ? "mute" : "sound", ""); soundChip.Tooltip = _loc.T(MmMuted ? "mm.muted.tip" : "mm.sound.tip");
                    _sfx.PlayVaried("click");
                };
                var fxChip = new MenuChip("fx", "sparkle", _loc.T("mm.fx." + (int)MenuScene.Quality)) { Tooltip = _loc.T("mm.fx.tip") };
                fxChip.OnSubmit = () => { MenuScene.Quality = (MenuQuality)(((int)MenuScene.Quality + 2) % 3); fxChip.Set("sparkle", _loc.T("mm.fx." + (int)MenuScene.Quality)); _sfx.PlayVaried("click"); };
                chips.Add(langChip); chips.Add(soundChip); chips.Add(fxChip);
                var chipItems = new List<IMenuItem> { langChip, soundChip, fxChip };

                // ---- footer: version on the left, input prompts on the right
                var footer = new VisualElement { pickingMode = PickingMode.Ignore }; footer.style.position = Position.Absolute; footer.style.flexDirection = FlexDirection.Row;
                footer.style.justifyContent = Justify.SpaceBetween; footer.style.alignItems = Align.Center;
                footer.style.left = Length.Percent(T.Safe * 100); footer.style.right = Length.Percent(T.Safe * 100); footer.style.bottom = Length.Percent(T.Safe * 100 * 0.9f);
                var ver = T.Text(_loc.T("mm.footer", Application.version, Application.companyName), MenuFont.Ui, T.FooterSize, T.TextSecondary); T.Shadow(ver);
                footer.Add(ver);
                var prompts = new InputPrompts(k => _loc.T(k)); footer.Add(prompts); prompts.Show(_mmInput.Device);
                host.Add(footer);

                var tipLayer = T.Fill(); tipLayer.Add(tooltip); host.Add(tipLayer);
                var debug = T.Text("", MenuFont.Ui, 14, new Color(0.6f, 1f, 0.7f), false); debug.style.position = Position.Absolute; debug.style.left = 12; debug.style.top = 10;
                debug.style.backgroundColor = new Color(0, 0, 0, 0.6f); debug.style.paddingLeft = debug.style.paddingRight = 8; debug.style.display = DisplayStyle.None; host.Add(debug);
                host.Add(scene.BlackCover);

                // ---- focus model: the ring (up/down turns it) and the chips (right arrow reaches them)
                bool inChips = false, launching = false; int chipFocus = 0;
                int sel = dial.Items.FindIndex(i => i.Id == _mmFocusId && i.Interactable);
                if (sel < 0) sel = dial.Items.FindIndex(i => i.Interactable);
                int savedChip = chipItems.FindIndex(c => c.Id == _mmFocusId);
                if (savedChip >= 0) { inChips = true; chipFocus = savedChip; }
                dial.Select(sel, false);
                void ApplyFocus()
                {
                    bool fv = _mmInput.FocusVisible;
                    for (int i = 0; i < dial.Items.Count; i++) dial.Items[i].SetFocus(!inChips && i == dial.Selected, fv);
                    for (int i = 0; i < chipItems.Count; i++) chipItems[i].SetFocus(inChips && i == chipFocus, fv);
                    _mmFocusId = inChips ? chipItems[chipFocus].Id : dial.Items[dial.Selected].Id;
                    if (fv && inChips) tooltip.ShowFor(chipItems[chipFocus].Element, chipItems[chipFocus].Tooltip, null);
                    else if (fv) tooltip.Hide();
                }
                void DialStep(int dir) { inChips = false; dial.Step(dir); ApplyFocus(); }   // the ring's ratchet and lock sounds come from its own events
                void Activate(IMenuItem it)
                {
                    if (launching) return;
                    if (!it.Interactable)
                    {
                        _sfx.PlayVaried("bad", 0.25f);
                        tooltip.ShowFor(it.Element, it.Tooltip, null, 0);
                        return;
                    }
                    if (it is DialItem di && dial.Items.IndexOf(di) != dial.Selected)
                    {
                        // clicking an item that is not docked: turn the ring to it, then run it once it locks in place
                        inChips = false; dial.Select(dial.Items.IndexOf(di)); ApplyFocus();
                        di.schedule.Execute(() => { if (!launching && !_ui.ModalOpen) Activate(di); }).StartingIn(Settings.ReducedMotion ? 0 : 300);
                        return;
                    }
                    it.SetPressed(true); it.Element.schedule.Execute(() => it.SetPressed(false)).StartingIn((long)(T.Fast * 1000));
                    if (it is DialItem) { MenuAudio.Play("chime", 0.6f, 0.01f); dial.Punch(); }
                    it.Submit();
                }
                // hovering turns the ring, but only while the real mouse moves: items sliding under a still cursor never chain-rotate
                float hoverGate = 0;
                void HoverTurn(DialItem di)
                {
                    int idx = dial.Items.IndexOf(di);
                    if (launching || _ui.ModalOpen || dial.Reveal < 0.99f || !di.Interactable || idx == dial.Selected) return;   // not during the intro
                    if (!_mmInput.MouseMovedRecently || Time.unscaledTime < hoverGate || dial.Speed > 1.2f || Mathf.Abs(dial.SlotOf(idx)) > 3.2f) return;   // let the ring settle before it follows the pointer again
                    hoverGate = Time.unscaledTime + 0.1f;
                    inChips = false; dial.Select(idx); ApplyFocus();
                }
                foreach (var it in dial.Items)
                {
                    var di = it;
                    it.PointerEntered = x => { x.SetHover(true); tooltip.ShowFor(x.Element, x.Tooltip, x.TooltipKey); HoverTurn(di); };
                    it.PointerLeft = x => { x.SetHover(false); tooltip.Hide(); };
                    it.Clicked = x => Activate(x);
                    it.RegisterCallback<PointerMoveEvent>(_ => HoverTurn(di));
                }
                foreach (var c in chipItems)
                    if (c is MenuItemBase b)
                    {
                        b.PointerEntered = x => { x.SetHover(true); tooltip.ShowFor(x.Element, x.Tooltip, x.TooltipKey); };
                        b.PointerLeft = x => { x.SetHover(false); tooltip.Hide(); };
                        b.Clicked = x => Activate(x);
                    }
                float wheelGate = 0;
                host.RegisterCallback<WheelEvent>(e =>
                {
                    if (launching || _ui.ModalOpen || Time.unscaledTime < wheelGate || Mathf.Abs(e.delta.y) < 0.01f) return;
                    wheelGate = Time.unscaledTime + 0.12f;   // one step per notch, even on smooth-scrolling wheels
                    DialStep(e.delta.y > 0 ? 1 : -1);
                });

                // ---- primary actions
                start.OnSubmit = () =>
                {
                    if (hasSave) { MmConfirmNew(() => Launch(() => { Settings.ClearCheckpoint(); LdClearSave(); _ui.HideScreen(); StartCoroutine(StartChapter1()); })); return; }
                    Launch(() => { Settings.ClearCheckpoint(); LdClearSave(); _ui.HideScreen(); StartCoroutine(StartChapter1()); });
                };
                cont.OnSubmit = () => Launch(() => { _ui.HideScreen(); if (!LdContinue()) ContinueGame(); });

                // ---- intro and departure timelines
                var stagger = new List<VisualElement> { langChip, soundChip, fxChip, footer };
                float introT = playIntro ? 0f : 99f, departT = -1f, last = Time.unscaledTime, t0 = Time.unscaledTime;
                Action departAction = null;
                bool stingPlayed = !playIntro, lockPlayed = !playIntro;
                void Launch(Action act)
                {
                    if (launching) return;
                    launching = true; departT = 0; departAction = act; tooltip.Hide();
                    _sfx.PlayVaried("console", 0.9f); _sfx.Play("whoosh", 0.5f);
                }
                void ApplyIntro(float t)
                {
                    if (reduced)
                    {
                        float a = Mathf.Clamp01(t / T.Slow);
                        scene.Black = 1 - a; scene.Push = 0; dial.Spin = 0; dial.Reveal = a; dial.style.opacity = a;
                        logoMask.style.width = logoW; rule.style.width = 560; sheen.style.opacity = 0; logoMask.style.opacity = a;
                        foreach (var e in stagger) { e.style.opacity = a; e.style.translate = new Translate(0, 0); }
                        return;
                    }
                    scene.Black = 1 - T.Ease(t / 0.8f);
                    scene.Push = 0.1f * (1 - T.Ease(t / 1.6f));
                    float lw = T.Ease((t - 0.6f) / 0.8f);
                    logoMask.style.width = logoW * lw; logoMask.style.opacity = 1;
                    rule.style.width = 560 * T.Ease((t - 1.0f) / 0.6f);
                    float sh = (t - 1.4f) / 0.9f;
                    sheen.style.opacity = sh > 0 && sh < 1 ? Mathf.Sin(sh * Mathf.PI) : 0;
                    sheen.style.left = Mathf.Lerp(-logoW * 0.25f, logoW * 1.05f, Mathf.Clamp01(sh));
                    // the ring swings in and locks: about 70 degrees of spin that settles as the items appear
                    float spin = T.Ease((t - 0.3f) / 1.5f);
                    dial.Spin = 70f * (1 - spin); dial.style.opacity = T.Ease((t - 0.3f) / 0.6f);
                    dial.Reveal = T.Ease((t - 1.3f) / 0.9f);
                    for (int i = 0; i < stagger.Count; i++)
                    {
                        float k = T.Ease((t - 2.0f - i * T.Stagger) / T.Base);
                        stagger[i].style.opacity = k; stagger[i].style.translate = new Translate(0, 24 * (1 - k));
                    }
                    if (!stingPlayed && t > 1.3f) { stingPlayed = true; _sfx.PlayVaried("card", 0.45f, 0.01f); }
                    if (!lockPlayed && t > 1.75f) { lockPlayed = true; MenuAudio.Play("lock", 0.75f); dial.Punch(); }
                }
                ApplyIntro(introT);

                // ---- audio: wind bed + distant hum
                MmWindStart(); _sfx.Loop("hum", true); MmApplyListener();
                _mmHost = host;
                var sceneTrack = Resources.Load<AudioClip>("Audio/Music/Menu/menu_" + spec.Name);   // e.g. menu_ship, menu_mars
                host.RegisterCallback<DetachFromPanelEvent>(_ =>
                {
                    MmWindStop(); MenuAudio.StopAll(); if (_sfx != null) _sfx.Loop("hum", false);
                    if (_chapterMusic != null) _chapterMusic.ContextGain = 1f;
                    if (_mmHost == host) { _mmHost = null; if (this != null && isActiveAndEnabled) StartCoroutine(MmMusicFadeOut()); }
                });

                bool modalWasOpen = false, debugOn = false; float fps = 60, prevSpin = 0;
                _mmInput.DeviceChanged -= MmOnDevice; MmOnDevice = d => { prompts.Show(d); ApplyFocus(); };
                _mmInput.DeviceChanged += MmOnDevice;
                host.schedule.Execute(() => { Fit(); ApplyFocus(); }).StartingIn(30);

                host.schedule.Execute(() =>
                {
                    if (host.panel == null) return;
                    float now = Time.unscaledTime, dt = Mathf.Clamp(now - last, 0, 0.1f); last = now;
                    var inp = _mmInput.Poll(dt);
                    bool modal = _ui.ModalOpen;
                    scene.DimTarget = modal ? 1 : 0;

                    if (introT < 3.6f)
                    {
                        introT += dt;
                        if (inp.any && introT > 0.15f) introT = 3.6f;   // any input skips the intro
                        ApplyIntro(introT);
                        inp = default;
                    }
                    else if (departT >= 0)
                    {
                        departT += dt;
                        float dur = reduced ? 0.4f : 1.25f;
                        float k = Mathf.Clamp01(departT / dur);
                        float ui = 1 - T.Ease(departT / 0.3f);
                        col.style.opacity = ui; chips.style.opacity = footer.style.opacity = ui; dial.style.opacity = 1 - T.Ease(departT / 0.5f);
                        if (!reduced) { scene.Push = 0.24f * T.Ease(k); dial.Spin = -45f * T.Ease(departT / 0.5f); }
                        scene.Black = reduced ? k : T.Ease((departT - 0.6f) / 0.65f);
                        if (departT >= dur && departAction != null)
                        {
                            var a = departAction; departAction = null; a();
                            return;   // the menu is detached; do not apply its departing mix to the new chapter
                        }
                        inp = default;
                    }
                    else if (modal)
                    {
                        // keyboard Esc is handled by GameFlow.Update; gamepad B closes sub-menus here
                        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) _ui.CloseModal();
                    }
                    else if (!modalWasOpen)
                    {
                        if (inp.dy != 0)
                        {
                            if (inChips) { inChips = false; _sfx.PlayVaried("hover"); ApplyFocus(); }
                            else DialStep(inp.dy);
                        }
                        else if (inp.dx != 0)
                        {
                            if (!inChips && inp.dx > 0) { inChips = true; chipFocus = 0; _sfx.PlayVaried("hover"); ApplyFocus(); }
                            else if (inChips)
                            {
                                int nf = chipFocus + inp.dx;
                                if (nf < 0) inChips = false; else chipFocus = Mathf.Min(nf, chipItems.Count - 1);
                                _sfx.PlayVaried("hover"); ApplyFocus();
                            }
                        }
                        if (inp.submit) Activate(inChips ? chipItems[chipFocus] : dial.Items[dial.Selected]);
                        if (inp.back)
                        {
                            if (inChips) { inChips = false; ApplyFocus(); }
                            else { int q = dial.Items.IndexOf(quit); if (dial.Selected != q) { dial.Select(q); _sfx.PlayVaried("hover"); ApplyFocus(); } }
                        }
                    }
                    modalWasOpen = modal;

                    dial.RichFx = MenuScene.Quality != MenuQuality.Low && !reduced;
                    float spinBefore = dial.Spin;
                    dial.Tick(dt, reduced);
                    MenuAudio.Servo(Mathf.Max(dial.Speed, Mathf.Abs(spinBefore - prevSpin) / Mathf.Max(dt, 0.001f) / 30f), dt);
                    prevSpin = spinBefore;
                    scene.Tick(dt, now - t0, inp.look, reduced);
                    float musicVol = Settings.Master * Settings.Ambience * 0.7f * (modal ? 0.5f : 1f) * (departT >= 0 ? 1 - Mathf.Clamp01(departT / 1.25f) * 0.6f : 1f);   // −6 dB under sub-menus
                    if (UserScoreActive)
                    {
                        // Keep the supplied opening score continuous into the intro, with the new menu's ducking.
                        _chapterMusic.ContextGain = (modal ? .5f : 1f) * (departT >= 0 ? 1f - Mathf.Clamp01(departT / 1.25f) * .6f : 1f);
                        if (s_mmMusic != null && s_mmMusic.isPlaying) s_mmMusic.Stop();
                        foreach (var layer in new[] { _musCalm, _musTension, _musStorm })
                            if (layer != null) layer.volume = 0f;
                    }
                    else if (sceneTrack != null)
                    {
                        MmMusicPlay(sceneTrack);
                        s_mmMusic.volume = Mathf.MoveTowards(s_mmMusic.volume, musicVol * 1.1f, dt * 0.35f);   // slow swell in
                        if (_musCalm != null) _musCalm.volume = Mathf.MoveTowards(_musCalm.volume, 0f, dt * 0.5f);
                    }
                    else
                    {
                        if (s_mmMusic != null && s_mmMusic.isPlaying) s_mmMusic.volume = Mathf.MoveTowards(s_mmMusic.volume, 0f, dt * 0.5f);
                        if (_musCalm != null)
                        {
                            _musCalm.volume = Mathf.MoveTowards(_musCalm.volume, musicVol, dt * 0.5f);
                            if (!_musCalm.isPlaying) _musCalm.Play();
                        }
                    }
                    if (s_mmWind != null) s_mmWind.volume = Mathf.MoveTowards(s_mmWind.volume, Settings.Master * Settings.Ambience * 0.55f, dt * 0.3f);

                    var kb = Keyboard.current;
                    if (kb != null && kb.f3Key.wasPressedThisFrame) { debugOn = !debugOn; debug.style.display = debugOn ? DisplayStyle.Flex : DisplayStyle.None; }
                    if (debugOn)
                    {
                        fps = Mathf.Lerp(fps, 1f / Mathf.Max(0.001f, Time.unscaledDeltaTime), 0.1f);
                        debug.text = $"FPS {fps:0}   input {_mmInput.Device}   focus {_mmFocusId}   fx {MenuScene.Quality}   reduce-motion {reduced}   {Screen.width}x{Screen.height}";
                    }
                }).Every(16);
            });
            return true;
        }

        Action<MenuDevice> MmOnDevice;

        /// <summary>Open a sub-menu (an existing modal) with a slide-in from the left; the scene dims behind it.</summary>
        void MmOpenSub(Action show)
        {
            _sfx.PlayVaried("click");
            show();
            if (!_ui.ModalOpen || Settings.ReducedMotion) return;
            var root = _ui.Root; if (root == null || root.childCount == 0) return;
            var layer = root[root.childCount - 1]; if (layer.childCount == 0) return;
            var panel = layer[0];
            panel.style.opacity = 0; panel.style.translate = new Translate(-64, 0);
            T.Transition(panel, T.Base, "opacity", "translate");
            panel.schedule.Execute(() => { panel.style.opacity = 1; panel.style.translate = new Translate(0, 0); }).StartingIn(16);
        }

        void MmConfirmNew(Action yes)
        {
            _ui.ShowModal(p =>
            {
                p.style.width = 720;
                var title = T.Text(_loc.T("mm.new.confirm.title"), MenuFont.Menu, T.ButtonSize, T.TextPrimary); p.Add(title);
                var body = T.Text(_loc.T("mm.new.confirm.body"), MenuFont.Ui, T.TaglineSize - 4, T.TextSecondary); body.style.marginTop = T.S1; p.Add(body);
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginTop = T.S3;
                var ok = _ui.B(_loc.T("mm.new.confirm.yes"), () => { _ui.CloseModal(); yes(); }, 26, true);
                var no = _ui.B(_loc.T("mm.cancel"), _ui.CloseModal, 26); no.style.marginLeft = T.S2;
                row.Add(ok); row.Add(no); p.Add(row);
            }, false);
        }

        // ------------------------------------------------------------------ menu audio

        static bool MmMuted { get => PlayerPrefs.GetInt("muted", 0) == 1; set { PlayerPrefs.SetInt("muted", value ? 1 : 0); PlayerPrefs.Save(); } }
        static bool s_appFocused = true;

        static void MmApplyListener() => AudioListener.volume = MmMuted || !s_appFocused ? 0f : 1f;

        void OnApplicationFocus(bool focus) { s_appFocused = focus; MmApplyListener(); }

        /// <summary>A procedural Martian wind loop (brown noise with slow gusts). Placeholder until a recorded bed is licensed.</summary>
        void MmWindStart()
        {
            if (s_mmWind == null)
            {
                const int rate = 22050; const float len = 14f;
                int n = (int)(rate * len), fade = rate;
                var data = new float[n + fade];
                var rng = new System.Random(77); float b = 0, lp = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    float w = (float)rng.NextDouble() * 2 - 1;
                    b = Mathf.Clamp(b + w * 0.02f, -1, 1) * 0.998f;          // brown noise
                    lp += (b - lp) * 0.08f;                                   // soften the top end
                    float t = i / (float)rate;
                    float gust = 0.55f + 0.3f * Mathf.Sin(t * 0.45f) + 0.15f * Mathf.Sin(t * 1.3f + 1.7f);
                    data[i] = lp * 2.4f * gust;
                }
                var loop = new float[n];
                for (int i = 0; i < n; i++) loop[i] = data[i];
                for (int i = 0; i < fade; i++) { float k = i / (float)fade; loop[i] = data[i] * k + data[n + i] * (1 - k); }   // seamless loop
                var clip = AudioClip.Create("menu_wind", n, 1, rate, false); clip.SetData(loop, 0);
                s_mmWind = gameObject.AddComponent<AudioSource>();
                s_mmWind.clip = clip; s_mmWind.loop = true; s_mmWind.playOnAwake = false; s_mmWind.volume = 0;
            }
            if (!s_mmWind.isPlaying) s_mmWind.Play();
        }

        void MmWindStop() { if (s_mmWind != null) s_mmWind.Stop(); }

        /// <summary>Start the scene's menu track (a no-op if it is already playing, so a menu rebuild never restarts it).</summary>
        void MmMusicPlay(AudioClip clip)
        {
            if (s_mmMusic == null)
            {
                s_mmMusic = gameObject.AddComponent<AudioSource>();
                s_mmMusic.loop = true; s_mmMusic.playOnAwake = false; s_mmMusic.volume = 0;
            }
            if (s_mmMusic.clip != clip) { s_mmMusic.clip = clip; s_mmMusic.volume = 0; s_mmMusic.Play(); }
            else if (!s_mmMusic.isPlaying) s_mmMusic.Play();
        }

        /// <summary>After the player leaves the menu, fade the menu track out over about 1.5 s (unless the menu comes back).</summary>
        System.Collections.IEnumerator MmMusicFadeOut()
        {
            yield return null;   // a rebuild (language, settings) re-attaches a new menu in the same frame
            while (s_mmMusic != null && s_mmMusic.isPlaying && _mmHost == null)
            {
                s_mmMusic.volume = Mathf.MoveTowards(s_mmMusic.volume, 0f, Time.unscaledDeltaTime * 0.6f);
                if (s_mmMusic.volume <= 0.001f) { s_mmMusic.Stop(); s_mmMusic.clip = null; yield break; }
                yield return null;
            }
        }
    }
}
