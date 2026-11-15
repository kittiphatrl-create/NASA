// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do a 60-Seconds-style trip outside (pick where, the screen cuts, you walk back in with what you found)
//           and a dinner-then-"DAY 2" title card make each day feel like a story, told mostly with icons?
// Date: 2026-11-15
using System.Collections;
using System.Collections.Generic;
using TitanGate.Core.LastDock;
using UnityEngine;
using UnityEngine.UIElements;
using Fault = TitanGate.Core.LastDock.Fault;
using Outcome = TitanGate.Core.LastDock.Outcome;

namespace TitanGate.Game
{
    /// <summary>
    /// LAST DOCK day flow: the airlock (choose a site or send the rover; the trip happens off screen and the cadet walks
    /// back in to a results card of icons), and the end of the day (dinner at the table, a black DAY N card that sums
    /// up the night in icons). Rules stay in StationSimulation.
    /// </summary>
    public partial class GameFlow
    {
        /// <summary>True while a cut-scene sequence of the day flow runs (the playtest bot waits on it).</summary>
        bool _ldSeq, _ldShotDay, _ldShotTrip, _ldShotAirlock;
        static Font _ldChakra;

        /// <summary>Chakra Petch (Thai and Latin) for titles, chips and numbers.</summary>
        void LdFont(TextElement t)
        {
            if (_ldChakra == null) _ldChakra = Resources.Load<Font>("Fonts/ChakraPetch-Bold");
            if (_ldChakra == null) return;
            t.style.unityFontDefinition = FontDefinition.FromFont(_ldChakra);
            t.style.unityTextGenerator = TextGeneratorType.Advanced;
        }

        // ------------------------------------------------------------------ Blender key art (Art/Blender/mars_keyart.blend)

        static readonly Dictionary<string, Texture2D> LdArt = new Dictionary<string, Texture2D>();

        static Texture2D LdKeyArt(string name)
        {
            if (LdArt.TryGetValue(name, out var t) && t != null) return t;
            var ta = Resources.Load<TextAsset>("Sprites/KeyArt/" + name + ".jpg");
            if (ta == null) return null;
            t = new Texture2D(2, 2, TextureFormat.RGB24, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            t.LoadImage(ta.bytes);
            LdArt[name] = t;
            return t;
        }

        /// <summary>A full-screen picture behind a screen (cover-fit), with a soft dark edge for text on the left.</summary>
        VisualElement LdArtBackdrop(string name, float leftShade = 0.75f)
        {
            var host = new VisualElement { pickingMode = PickingMode.Ignore };
            host.style.position = Position.Absolute; host.style.left = 0; host.style.right = 0; host.style.top = 0; host.style.bottom = 0;
            var tex = LdKeyArt(name);
            if (tex != null)
            {
                host.style.backgroundImage = new StyleBackground(tex);
                host.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            }
            var shade = new VisualElement { pickingMode = PickingMode.Ignore };
            shade.style.position = Position.Absolute; shade.style.left = 0; shade.style.top = 0; shade.style.bottom = 0; shade.style.width = Length.Percent(60);
            shade.style.backgroundImage = new StyleBackground(Art.TitleReadabilityFade());
            shade.style.opacity = leftShade;
            host.Add(shade);
            return host;
        }

        /// <summary>A story beat: the picture fades in with one line of text, then fades out.</summary>
        IEnumerator LdStoryPanel(string art, string text, float seconds)
        {
            bool done = false;
            _ui.ShowScreen(root =>
            {
                root.Add(LdArtBackdrop(art, 0f));
                var bar = new VisualElement(); bar.style.position = Position.Absolute; bar.style.left = 0; bar.style.right = 0; bar.style.bottom = 0; bar.style.height = 170;
                bar.style.backgroundColor = new Color(0, 0, 0, 0.6f); bar.style.justifyContent = Justify.Center; bar.style.alignItems = Align.Center;
                var l = _ui.L(text, 34, true, GameUI.CText); l.style.whiteSpace = WhiteSpace.Normal; l.style.maxWidth = 1400; l.style.unityTextAlign = TextAnchor.MiddleCenter; LdFont(l);
                bar.Add(l); root.Add(bar);
                LdAnimateIn(l, 1);
                var skip = _ui.B(TT("Next", "ต่อไป"), () => done = true, 22); skip.style.position = Position.Absolute; skip.style.right = 40; skip.style.bottom = 190; root.Add(skip);
            });
            yield return Fade(0f, 0.6f);
            float t = 0; while (!done && t < seconds) { t += Time.deltaTime; yield return null; }
            yield return Fade(1f, 0.5f);
            _ui.HideScreen();
        }

        // ------------------------------------------------------------------ icons for numbers

        /// <summary>A row of icon + number chips for a change (green gain, red loss). Empty changes are skipped.</summary>
        VisualElement LdDeltaRow(ActionResult r, int size = 22)
        {
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap; row.style.alignItems = Align.Center;
            void Add(string icon, int d, bool goodWhenUp = true)
            {
                if (d == 0) return;
                bool good = d > 0 == goodWhenUp;
                var c = good ? GameUI.CGreen : GameUI.CRed;
                var chip = new VisualElement(); chip.style.flexDirection = FlexDirection.Row; chip.style.alignItems = Align.Center;
                chip.style.marginRight = 10; chip.style.marginTop = 2;
                chip.Add(LdIconImage(icon, size, c));
                var l = _ui.L((d > 0 ? "+" : "−") + Mathf.Abs(d), size - 2, true, c); l.style.marginLeft = 3; LdFont(l); chip.Add(l);
                row.Add(chip);
            }
            Add("food", r.food); Add("o2", r.oxygen); Add("battery", r.battery); Add("parts", r.parts); Add("can", r.absorbers);
            Add("co2", r.co2, false); Add("heat", r.heat, false); Add("signal", r.signal); Add("heart", r.health);
            if (row.childCount == 0) row.Add(_ui.L(TT("no change", "ไม่เปลี่ยน"), size - 4, false, GameUI.CDim));
            return row;
        }

        /// <summary>A big icon tile for the results and day cards.</summary>
        VisualElement LdTile(string icon, string big, string small, Color color)
        {
            var t = new VisualElement(); t.style.width = 150; t.style.paddingTop = 10; t.style.paddingBottom = 10; t.style.marginLeft = 6; t.style.marginRight = 6; t.style.marginTop = 6;
            t.style.alignItems = Align.Center; t.style.backgroundColor = new Color(0.07f, 0.08f, 0.12f, 0.95f); GameUI.Border(t, color * new Color(1, 1, 1, 0.6f), 2, 12);
            t.Add(LdIconImage(icon, 44, color));
            var b = _ui.L(big, 30, true, color); LdFont(b); t.Add(b);
            var s = _ui.L(small, 17, false, GameUI.CDim); s.style.whiteSpace = WhiteSpace.Normal; s.style.unityTextAlign = TextAnchor.MiddleCenter; t.Add(s);
            LdAnimateIn(t, ++_ldCardIndex);
            return t;
        }

        VisualElement LdTileRow() { var r = new VisualElement(); r.style.flexDirection = FlexDirection.Row; r.style.flexWrap = Wrap.Wrap; r.style.justifyContent = Justify.Center; return r; }

        // ------------------------------------------------------------------ the airlock

        void LdShowAirlock()
        {
            _ui.ShowModal(p =>
            {
                LdHeader(p, TT("Airlock — go outside", "ประตูออกข้างนอก"));
                var info = new VisualElement(); info.style.flexDirection = FlexDirection.Row; info.style.alignItems = Align.Center; info.style.marginBottom = 4;
                info.Add(LdIconImage("time", 22, GameUI.CCyan));
                info.Add(LdPara(TT(" One trip a day. Pick where to go:", " ออกได้วันละครั้ง เลือกที่จะไป:"), GameUI.CCyan, 22));
                p.Add(info);
                if (_ld.StormToday) p.Add(LdPara("! " + TT("SOLAR STORM: every site is a radiation hot spot today.", "พายุสุริยะ: วันนี้ทุกจุดรังสีสูง"), GameUI.CRed, 22));
                for (int i = 0; i < StationSimulation.Sites; i++) p.Add(LdSiteCard(i));
                var rover = new List<string>();
                foreach (var a in new[] { "assemble_rover", "rover_solar", "send_rover", "repair_rover" })
                    if (_ld.Can(a, out var why) || why != "not_needed" && why != "rover_parts") rover.Add(a);
                var head = new VisualElement(); head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center; head.style.marginTop = 10;
                head.Add(LdIconImage("rover", 26, LdGold));
                head.Add(LdPara(_ld.RoverBuilt ? TT(" Rover", " รถสำรวจ") : TT($" Rover parts {_ld.RoverParts}/{_ld.C.roverPartsNeeded} (old rover wreck)", $" ชิ้นส่วนรถ {_ld.RoverParts}/{_ld.C.roverPartsNeeded} (ซากรถสำรวจเก่า)"), LdGold, 22));
                p.Add(head);
                foreach (var a in rover) p.Add(LdActionRow(a));
            });
        }

        string LdSiteIcon(int i) => i == 0 ? "parts" : i == 1 ? "rover" : "food";

        VisualElement LdSiteCard(int i)
        {
            string a = "explore_" + i;
            var card = LdCard(a);
            // radiation badge on the card: what the meter says, or a question mark without one
            bool known = _ld.HasRadMeter || _ld.StormToday;
            bool hot = _ld.IsHot(i);
            var badge = new VisualElement(); badge.style.alignItems = Align.Center; badge.style.marginLeft = 8; badge.style.minWidth = 76;
            badge.Add(LdIconImage("storm", 26, !known ? GameUI.CAmber : hot ? GameUI.CRed : GameUI.CGreen));
            var l = _ui.L(!known ? "?" : hot ? TT("HOT", "อันตราย") : TT("safe", "ปลอดภัย"), 17, true, !known ? GameUI.CAmber : hot ? GameUI.CRed : GameUI.CGreen); LdFont(l);
            badge.Add(l);
            card.Insert(card.childCount - 1, badge);
            return card;
        }

        /// <summary>The trip outside happens off screen: walk to the door, the screen cuts to black, then the cadet walks back in.</summary>
        IEnumerator LdExpedition(string a)
        {
            _ldSeq = true; _busy = true;
            _ui.CloseModal();
            bool rover = a == "send_rover";
            int day = _ld.Day, logMark = _ld.Log.Count;
            var door = new Vector2(31.2f, 20.6f);
            BGoTo(door, null);
            float t = 0; while (_bPath != null && t < 4f) { t += Time.deltaTime; yield return null; }
            _sfx.Play("door_open");
            yield return Fade(1f, 0.5f);
            _ui.FadeText(rover ? TT("The rover rolls out across the red dust...", "รถสำรวจแล่นออกไปบนฝุ่นสีแดง...")
                               : TT("You step out onto Mars...", "คุณก้าวออกไปบนดาวอังคาร...") + "\n" + LdSiteName(a.StartsWith("explore_") ? a[8] - '0' : 0), 40);
            _sfx.Play(rover ? "rover" : "whoosh");
            yield return new WaitForSeconds(_playtest ? 0.6f : 1.6f);
            var r = _ld.Do(a);
            if (r.ok) LdRecord(a);
            _bPos = door; _bLastMove = Vector2.left; _bPath = null; _bGoal = null;
            _ui.FadeText(null);
            _sfx.Play("door_close");
            yield return Fade(0f, 0.5f);
            BGoTo(new Vector2(29f, 20.6f), null);
            t = 0; while (_bPath != null && t < 3f) { t += Time.deltaTime; yield return null; }
            _busy = false;
            if (!r.ok) { _ldSeq = false; _ui.Toast(LdBlockedText(r.reason), GameUI.CRed); yield break; }
            bool closed = false;
            _ui.ShowModal(p =>
            {
                LdHeader(p, rover ? TT("The rover is back!", "รถสำรวจกลับมาแล้ว!") : TT("Back inside!", "กลับเข้ามาแล้ว!"));
                var row = LdTileRow();
                void Tile(string icon, int d, string name) { if (d != 0) row.Add(LdTile(icon, (d > 0 ? "+" : "−") + Mathf.Abs(d), name, d > 0 ? GameUI.CGreen : GameUI.CRed)); }
                Tile("parts", r.parts, LdRes(Res.Parts)); Tile("battery", r.battery, LdRes(Res.Battery)); Tile("o2", r.oxygen, "O₂"); Tile("food", r.food, LdRes(Res.Food));
                if (r.notes.Contains("found_rover_part")) row.Add(LdTile("rover", "+1", TT($"rover part ({_ld.RoverParts}/{_ld.C.roverPartsNeeded})", $"ชิ้นส่วนรถ ({_ld.RoverParts}/{_ld.C.roverPartsNeeded})"), LdGold));
                if (r.notes.Contains("found_mystery") || rover) row.Add(LdTile("food", "?", TT("unlabelled food", "อาหารไม่มีฉลาก"), GameUI.CAmber));
                if (r.notes.Contains("explore_radiation")) row.Add(LdTile("storm", "−1", TT("radiation hot spot!", "โดนรังสีสูง!"), GameUI.CRed));
                if (r.notes.Contains("rover_broke")) row.Add(LdTile("rover", "✕", TT("rover broke down", "รถเสีย"), GameUI.CRed));
                if (row.childCount == 0) row.Add(LdTile("walk", "0", TT("nothing found", "ไม่เจออะไร"), GameUI.CDim));
                p.Add(row);
                if (r.notes.Contains("explore_radiation")) p.Add(LdPara("NASA: " + LdStormFact, GameUI.CCyan, 19));
            }, true, () => closed = true);
            _sfx.Play(r.notes.Contains("explore_radiation") ? "bad" : "good");
            if (_playtest) { yield return new WaitForSeconds(0.6f); if (!_ldShotTrip) { _ldShotTrip = true; yield return Shot("ld06_trip"); } _ui.CloseModal(); }
            while (!closed) yield return null;
            _ldSeq = false;
            LdAfterAction(day, logMark);
        }

        /// <summary>What follows any action: events that need an answer, the end, or the night.</summary>
        void LdAfterAction(int day, int logMark)
        {
            if (_ld.PowerDipPending) { LdShowDip(); return; }
            if (_ld.RaiderPending) { LdShowRaid(); return; }
            if (_ld.Outcome != Outcome.Ongoing) { StartCoroutine(LdEnd()); return; }
            if (_ld.DinnerPending || _ld.Day != day) StartCoroutine(LdDinnerAndDay(day, logMark));
        }

        // ------------------------------------------------------------------ dinner, then the DAY card

        IEnumerator LdDinnerAndDay(int nightOf, int logMark)
        {
            _ldSeq = true; _busy = true;
            _ui.CloseModal();
            // dinner at the table: sit on the left chair and eat
            var seat = new Vector2(20.6f, 10.3f);   // in front of the table (the chairs and table block their tiles)
            BGoTo(seat, null);
            float t = 0; while (_bPath != null && t < 5f) { t += Time.deltaTime; yield return null; }
            _bLastMove = Vector2.right;
            _ui.Toast(TT("Dinner time.", "ได้เวลาอาหารเย็น"), GameUI.CAmber);
            yield return Act("lift", _playtest ? 0.5f : 1.0f, null);
            int foodBefore = _ld.Food;
            if (_ld.DinnerPending)
            {
                yield return LdDinnerScreen();
                LdRecord("dinner:" + string.Join(",", System.Array.ConvertAll(_ld.MealPlan, m => ((int)m).ToString())));
                _ld.Do("dinner");
                if (_ld.Outcome != Outcome.Ongoing) { _busy = false; _ldSeq = false; StartCoroutine(LdEnd()); yield break; }
                LdWriteSave();
            }
            yield return Fade(1f, 0.7f);
            var lines = new List<(string icon, string text, Color c)>();
            for (int i = logMark; i < _ld.Log.Count; i++)
            {
                string e = _ld.Log[i]; var txt = LdEventText(e);
                if (txt == null || lines.Exists(x => x.text == txt)) continue;
                string icon = e.Contains("storm") || e.Contains("radiation") || e.Contains("shelter") ? "storm" : e.Contains("food") || e.Contains("hungry") || e.Contains("raid") ? "food"
                    : e.Contains("power") || e.Contains("dark") || e.Contains("intruder") ? "battery" : e.Contains("tired") || e.Contains("sick") ? "heart" : e.StartsWith("fault") ? "parts" : "signal";
                bool bad = e.Contains("stole") || e.Contains("hungry") || e.Contains("no_power") || e.Contains("radiation") || e.Contains("sick") || e.Contains("tired") || e.StartsWith("fault") || e.Contains("dark");
                lines.Add((icon, txt, bad ? GameUI.CRed : GameUI.CGreen));
            }
            bool go = false;
            _ui.ShowScreen(root =>
            {
                root.Add(GameUI.Backdrop(new Color(0f, 0f, 0f, 1f)));
                var box = new VisualElement(); box.style.alignItems = Align.Center; box.style.maxWidth = 1300;
                _ldCardIndex = 0;
                var night = _ui.L(TT($"Night {nightOf}", $"คืนที่ {nightOf}"), 26, false, GameUI.CDim); LdFont(night); box.Add(night);
                var big = _ui.L(TT($"DAY {_ld.Day}", $"วันที่ {_ld.Day}"), 96, true, LdGold); LdFont(big); box.Add(big); LdAnimateIn(big, 0);
                var row = LdTileRow();
                row.Add(LdTile("food", "−" + Mathf.Max(0, foodBefore - _ld.Food), TT("meals eaten", "มื้อที่กิน"), GameUI.CAmber));
                int net = _ld.SolarToday - _ld.C.baseLoad - (_ld.BackupCircuit ? 1 : 0);
                row.Add(LdTile("battery", (net >= 0 ? "+" : "−") + Mathf.Abs(net), TT("solar − base", "โซลาร์ − ฐานใช้"), net >= 0 ? GameUI.CGreen : GameUI.CRed));
                row.Add(LdTile("time", _ld.WorkUnits.ToString(), TT("work units today", "เวลาทำงานวันนี้"), GameUI.CCyan));
                row.Add(LdTile("rescue", (_ld.C.days - _ld.Day + 1).ToString(), TT("days to rescue", "วันจนยานกู้ภัยมา"), LdGold));
                box.Add(row);
                foreach (var (icon, text, c) in lines)
                {
                    var r = new VisualElement(); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginTop = 6;
                    r.Add(LdIconImage(icon, 24, c)); var l = LdPara(" " + text, c, 21); l.style.marginBottom = 0; r.Add(l);
                    box.Add(r); LdAnimateIn(r, ++_ldCardIndex);
                }
                if (_ld.StormTomorrow) box.Add(LdPara("! " + TT("ORBI: solar storm TOMORROW.", "ORBI: พรุ่งนี้พายุสุริยะ"), GameUI.CAmber, 24));
                var btn = _ui.B(TT("Start the day", "เริ่มวันใหม่"), () => go = true, 28, true); btn.style.marginTop = 22; box.Add(btn);
                root.Add(box);
            });
            _sfx.Play("card");
            if (_playtest && !_ldShotDay) { _ldShotDay = true; yield return Shot("ld05_day"); }
            float wait = 0;
            while (!go && !(_playtest && (wait += Time.deltaTime) > 1.2f)) yield return null;
            _ui.HideScreen();
            _bPos = seat + new Vector2(0, -0.8f);
            yield return Fade(0f, 0.6f);
            _busy = false; _ldSeq = false;
            if (_ld.ActiveFault != Fault.None) _ui.Toast("! " + LdIncidentOf(_ld.ActiveFault).title, GameUI.CRed);
        }
    }
}
