// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does a living title screen (parallax layers from Blender, drifting Mars dust, a title that builds itself,
//           icon tiles, a cadet you can poke) make the first ten seconds exciting?
// Date: 2026-11-15
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Lang = TitanGate.Core.Lang;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        static Texture2D _ldCadetLayer;

        static Texture2D LdCadetLayer()
        {
            if (_ldCadetLayer != null) return _ldCadetLayer;
            var ta = Resources.Load<TextAsset>("Sprites/KeyArt/title_cadet.png");
            if (ta == null) return null;
            _ldCadetLayer = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            _ldCadetLayer.LoadImage(ta.bytes);
            return _ldCadetLayer;
        }

        static VisualElement LdFill()
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute; e.style.left = 0; e.style.right = 0; e.style.top = 0; e.style.bottom = 0;
            return e;
        }

        /// <summary>LAST DOCK title screen. Returns false when the art is missing (the old menu is used then).</summary>
        bool LdBuildTitle()
        {
            var bgTex = LdKeyArt("title_bg"); var cadetTex = LdCadetLayer();
            if (bgTex == null || cadetTex == null) return false;
            LdMusicEnsure();
            _ui.ShowScreen(root =>
            {
                root.style.alignItems = Align.FlexStart; root.style.justifyContent = Justify.Center; root.style.backgroundColor = Color.black;
                // ---- picture layers
                var bg = LdFill(); bg.style.backgroundImage = new StyleBackground(bgTex); bg.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover); root.Add(bg);
                var sun = new VisualElement { pickingMode = PickingMode.Ignore }; sun.style.position = Position.Absolute; sun.style.width = 260; sun.style.height = 260;
                sun.style.left = Length.Percent(27.2f); sun.style.top = Length.Percent(30f); sun.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
                sun.style.backgroundImage = new StyleBackground(Art.SoftLight()); sun.style.unityBackgroundImageTintColor = new Color(1f, 0.9f, 0.7f, 0.55f); root.Add(sun);
                var dust = LdFill(); root.Add(dust);
                var rnd = new System.Random(5); var motes = new List<(VisualElement e, float x, float y, float sp, float ph)>();
                for (int i = 0; i < 46; i++)
                {
                    var m = new VisualElement { pickingMode = PickingMode.Ignore }; float sz = 2 + (float)rnd.NextDouble() * 5;
                    m.style.position = Position.Absolute; m.style.width = sz; m.style.height = sz; GameUI.Border(m, Color.clear, 0, sz);
                    m.style.backgroundColor = new Color(1f, 0.72f, 0.5f, 0.25f + (float)rnd.NextDouble() * 0.35f);
                    dust.Add(m); motes.Add((m, (float)rnd.NextDouble(), (float)rnd.NextDouble(), 0.01f + (float)rnd.NextDouble() * 0.03f, (float)rnd.NextDouble() * 6f));
                }
                var cadet = LdFill(); cadet.style.backgroundImage = new StyleBackground(cadetTex); cadet.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                var shadow = new VisualElement { pickingMode = PickingMode.Ignore }; shadow.style.position = Position.Absolute; shadow.style.left = Length.Percent(47); shadow.style.top = Length.Percent(88);
                shadow.style.width = Length.Percent(10); shadow.style.height = Length.Percent(4); shadow.style.backgroundColor = new Color(0.2f, 0.07f, 0.03f, 0.45f); GameUI.Border(shadow, Color.clear, 0, 60);
                root.Add(shadow); root.Add(cadet);
                var shade = LdFill(); shade.style.width = Length.Percent(58); shade.style.right = StyleKeyword.Auto;
                shade.style.backgroundImage = new StyleBackground(Art.TitleReadabilityFade()); root.Add(shade);
                // ---- title, built letter by letter
                var col = new VisualElement(); col.style.marginLeft = 90; col.style.width = 640;
                var titleRow = new VisualElement(); titleRow.style.flexDirection = FlexDirection.Row;
                string title = "LAST DOCK"; int li = 0;
                foreach (char ch in title)
                {
                    var l = _ui.L(ch == ' ' ? " " : ch.ToString(), 104, true, GameUI.CText); LdFont(l);
                    l.style.unityTextOutlineWidth = 0.6f; l.style.unityTextOutlineColor = new Color(0, 0, 0, 0.6f);
                    titleRow.Add(l); LdAnimateIn(l, 2 + li++);
                }
                col.Add(titleRow);
                var gold = new VisualElement(); gold.style.height = 4; gold.style.width = 0; gold.style.backgroundColor = LdGold; gold.style.marginTop = 2; col.Add(gold);
                var tag = new VisualElement(); tag.style.flexDirection = FlexDirection.Row; tag.style.alignItems = Align.Center; tag.style.marginTop = 10; tag.style.marginBottom = 26;
                tag.Add(LdIconImage("storm", 22, LdGold));
                var y = _ui.L("  MARS · 2177", 28, true, LdGold); LdFont(y); tag.Add(y);
                var sub = _ui.L("   " + TT("7 days. One base. Get home.", "7 วัน ฐานเดียว กลับบ้านให้ได้"), 24, false, GameUI.CText); tag.Add(sub);
                col.Add(tag); LdAnimateIn(tag, 12);
                // ---- the two big cards
                VisualElement Big(string icon, string label, string hint, Action act, bool primary, bool enabled)
                {
                    var c = new VisualElement(); c.style.flexDirection = FlexDirection.Row; c.style.alignItems = Align.Center; c.style.height = 84; c.style.marginBottom = 10;
                    c.style.paddingLeft = 18; c.style.paddingRight = 22;
                    var baseCol = primary ? new Color(0.95f, 0.62f, 0.18f, 0.95f) : new Color(0.06f, 0.07f, 0.11f, 0.85f);
                    c.style.backgroundColor = baseCol; GameUI.Border(c, primary ? LdGold : LdGoldDim, 2, 12);
                    var ic = new VisualElement(); ic.style.width = 54; ic.style.height = 54; ic.style.alignItems = Align.Center; ic.style.justifyContent = Justify.Center;
                    ic.style.backgroundColor = new Color(0, 0, 0, primary ? 0.25f : 0.35f); GameUI.Border(ic, Color.clear, 0, 27);
                    ic.Add(LdIconImage(icon, 32, primary ? Color.white : LdGold)); c.Add(ic);
                    var txt = new VisualElement(); txt.style.marginLeft = 16;
                    var t1 = _ui.L(label, 32, true, primary ? GameUI.Ink : GameUI.CText); LdFont(t1); txt.Add(t1);
                    txt.Add(_ui.L(hint, 18, false, primary ? new Color(0.2f, 0.12f, 0.05f) : GameUI.CDim));
                    c.Add(txt);
                    c.SetEnabled(enabled);
                    if (enabled)
                    {
                        c.RegisterCallback<MouseEnterEvent>(_ => { c.style.scale = new Scale(new Vector3(1.03f, 1.03f, 1)); c.style.translate = new Translate(8, 0); _sfx.Play("hover"); });
                        c.RegisterCallback<MouseLeaveEvent>(_ => { c.style.scale = new Scale(Vector3.one); c.style.translate = new Translate(0, 0); });
                        c.RegisterCallback<ClickEvent>(_ => { if (!_menuLaunching) { _sfx.Play("click"); act(); } });
                    }
                    else c.style.opacity = 0.45f;
                    col.Add(c); LdAnimateIn(c, 14 + col.childCount);
                    return c;
                }
                Big("play", TT("START MISSION", "เริ่มภารกิจ"), TT("New game · about 15 minutes", "เกมใหม่ · ประมาณ 15 นาที"),
                    () => MenuDepart(() => { Settings.ClearCheckpoint(); _ui.HideScreen(); StartCoroutine(StartChapter1()); }), true, true);
                Big("continue", TT("CONTINUE", "เล่นต่อ"), Settings.HasSave ? TT("Back to your last save", "กลับไปที่เซฟล่าสุด") : TT("No save yet", "ยังไม่มีเซฟ"),
                    () => { _ui.HideScreen(); ContinueGame(); }, false, Settings.HasSave);
                // ---- icon tiles with a label that appears on hover
                var tiles = new VisualElement(); tiles.style.flexDirection = FlexDirection.Row; tiles.style.marginTop = 8;
                var tip = _ui.L("", 20, true, LdGold); tip.style.marginTop = 8; tip.style.height = 26; LdFont(tip);
                void Tile(string icon, string label, Action act)
                {
                    var t = new VisualElement(); t.style.width = 70; t.style.height = 70; t.style.marginRight = 10; t.style.alignItems = Align.Center; t.style.justifyContent = Justify.Center;
                    t.style.backgroundColor = new Color(0.06f, 0.07f, 0.11f, 0.85f); GameUI.Border(t, LdGoldDim, 2, 14);
                    var img = LdIconImage(icon, 34, GameUI.CText); t.Add(img);
                    t.RegisterCallback<MouseEnterEvent>(_ => { tip.text = label; img.tintColor = LdGold; GameUI.Border(t, LdGold, 2, 14); t.style.translate = new Translate(0, -4); _sfx.Play("hover"); });
                    t.RegisterCallback<MouseLeaveEvent>(_ => { tip.text = ""; img.tintColor = GameUI.CText; GameUI.Border(t, LdGoldDim, 2, 14); t.style.translate = new Translate(0, 0); });
                    t.RegisterCallback<ClickEvent>(_ => { if (!_menuLaunching) { _sfx.Play("click"); act(); } });
                    tiles.Add(t); LdAnimateIn(t, 20 + tiles.childCount);
                }
                Tile("help", TT("How to play", "วิธีเล่น"), ShowHowTo);
                Tile("gear", TT("Settings", "ตั้งค่า"), ShowSettings);
                Tile("book", TT("Science cards", "การ์ดความรู้"), ShowCardBook);
                Tile("source", TT("NASA sources", "แหล่งข้อมูล NASA"), ShowSources);
                Tile("star", TT("Credits", "เครดิต"), ShowCredits);
                Tile("exit", TT("Quit", "ออกจากเกม"), () => Application.Quit());
                Tile("lang", _loc.Current == Lang.TH ? "English" : "ภาษาไทย", ToggleLang);
                col.Add(tiles); col.Add(tip);
                root.Add(col);
                // ---- poke the cadet
                var hit = new VisualElement(); hit.style.position = Position.Absolute; hit.style.left = Length.Percent(46); hit.style.top = Length.Percent(30); hit.style.width = Length.Percent(12); hit.style.height = Length.Percent(60);
                float hop = -10f; string[] says = { TT("Ready, Cadet?", "พร้อมไหม นักบิน?"), TT("Mars is cold: −60 °C at night!", "ดาวอังคารหนาว: กลางคืน −60 °C!"), TT("Let's get home together.", "กลับบ้านไปด้วยกันนะ") };
                var bubble = _ui.L("", 24, true, GameUI.Ink); bubble.style.position = Position.Absolute; bubble.style.left = Length.Percent(52); bubble.style.top = Length.Percent(24);
                bubble.style.backgroundColor = GameUI.Paper; bubble.style.paddingLeft = bubble.style.paddingRight = 14; bubble.style.paddingTop = bubble.style.paddingBottom = 6; GameUI.Border(bubble, LdGold, 2, 12);
                bubble.style.opacity = 0; LdFont(bubble);
                int sayIx = 0;
                hit.RegisterCallback<ClickEvent>(_ => { hop = 0f; bubble.text = says[sayIx++ % says.Length]; bubble.style.opacity = 1; _sfx.Play("pickup"); });
                root.Add(hit); root.Add(bubble);
                // ---- animate: slow drift, mouse parallax, dust, sun pulse, the gold line, the hop
                float t0 = Time.unscaledTime;
                root.schedule.Execute(() =>
                {
                    if (root.panel == null) return;
                    float t = Time.unscaledTime - t0;
                    var mp = UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero;
                    float mx = Settings.ReducedMotion ? 0 : (mp.x / Mathf.Max(1, Screen.width) - 0.5f), my = Settings.ReducedMotion ? 0 : (mp.y / Mathf.Max(1, Screen.height) - 0.5f);
                    float drift = Settings.ReducedMotion ? 0 : Mathf.Sin(t * 0.08f) * 0.6f;
                    bg.style.scale = new Scale(new Vector3(1.08f + Mathf.Sin(t * 0.05f) * 0.01f, 1.08f + Mathf.Sin(t * 0.05f) * 0.01f, 1));
                    bg.style.translate = new Translate(Length.Percent(-mx * 1.2f + drift), Length.Percent(my * 0.8f));
                    hop += Time.unscaledDeltaTime * 4f;
                    float jump = hop < 1f ? Mathf.Sin(hop * Mathf.PI) * 2.2f : 0f;
                    float breathe = Settings.ReducedMotion ? 0 : Mathf.Sin(t * 1.6f) * 0.25f;
                    cadet.style.scale = new Scale(new Vector3(1.04f, 1.04f + breathe * 0.004f, 1));
                    cadet.style.translate = new Translate(Length.Percent(-mx * 3f + drift * 1.4f), Length.Percent(my * 1.6f - jump));
                    shadow.style.translate = new Translate(Length.Percent(-mx * 30f + drift * 14f), 0);
                    sun.style.opacity = 0.75f + Mathf.Sin(t * 0.9f) * 0.2f;
                    gold.style.width = Mathf.Min(560, Mathf.Max(0, (t - 0.6f) * 900));
                    if (hop > 2.5f) bubble.style.opacity = Mathf.Max(0, bubble.resolvedStyle.opacity - Time.unscaledDeltaTime);
                    float w = root.resolvedStyle.width, h = root.resolvedStyle.height;
                    for (int i = 0; i < motes.Count; i++)
                    {
                        var (e, x0, y0, sp, ph) = motes[i];
                        float x = Mathf.Repeat(x0 - t * sp, 1f), yy = y0 + Mathf.Sin(t * 0.7f + ph) * 0.01f;
                        e.style.left = x * w - mx * 60f; e.style.top = yy * h;
                    }
                    float vol = Settings.Master * Settings.Ambience * 0.7f;
                    if (_musCalm != null) _musCalm.volume = Mathf.MoveTowards(_musCalm.volume, vol, Time.unscaledDeltaTime * 0.5f);
                }).Every(16);
            });
            return true;
        }
    }
}
