// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do a slower day, a pause/speed control, an air maker you run yourself and projects you pick make
//           the week feel like your own base to run (not a list of chores in a fixed order)?
// Date: 2026-11-14
using System;
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// Pace and freedom in the walkable base (round 10, see docs/redesign-v2.md):
    /// - time control: P pauses (look around, open panels, plan), F switches 1x/2x, like Frostpunk or RimWorld;
    /// - the oxygen maker (MOXIE) runs whenever you walk up to it, once a day, for power;
    /// - the project board in the hub: pick up to two base projects a day, each with an open price and a
    ///   lasting effect, solved hands-on like a repair;
    /// - the game saves every night, so a run can be continued from the next morning.
    /// </summary>
    public partial class GameFlow
    {
        // ------------------------------------------------------------------ time control

        bool _bPaused;
        float _bSpeed = 1f;
        VisualElement _bHudSpeed;
        Label _bPauseLabel;
        Label _bPauseCaption, _bSpeedCaption;
        Button _bPauseBtn, _bSpeedBtn;

        void PaceBuildHud()
        {
            PaceEndBase();
            _bPaused = false; _bSpeed = 1f;
            if (_bMode == 1)
            {
                _bHudSpeed = new VisualElement();
                _bHudSpeed.style.position = Position.Absolute; _bHudSpeed.style.right = 24; _bHudSpeed.style.bottom = 24;
                var pause = _ui.B(TT("Pause", "หยุดชั่วคราว"), OpenPause, 18);
                _ui.CutButton(pause, true, "ESC", 18);
                _bHudSpeed.Add(pause); _ui.Root.Add(_bHudSpeed);
                return;
            }
            if (_bMode != 0 || MarsWalkOnly) return;
            _bHudSpeed = new VisualElement();
            _bHudSpeed.style.position = Position.Absolute; _bHudSpeed.style.right = 24; _bHudSpeed.style.bottom = 24;
            _bHudSpeed.style.flexDirection = FlexDirection.Row;
            _bPauseBtn = _ui.B(TT("Pause", "หยุดเวลา"), () => { _bPaused = !_bPaused; _sfx.Play("blip"); PaceRefresh(); }, 18);
            _bSpeedBtn = _ui.B(TT("Speed ×1", "ความเร็ว ×1"), () => { _bSpeed = _bSpeed > 1.5f ? 1f : 2f; _sfx.Play("blip"); PaceRefresh(); }, 18);
            _bPauseCaption = _ui.CutButton(_bPauseBtn, true, "P", 18);
            _bSpeedCaption = _ui.CutButton(_bSpeedBtn, true, "F", 18);
            _bSpeedBtn.style.marginLeft = 8;
            if (RestoreMode) _bSpeedBtn.style.display = DisplayStyle.None;   // the clock only moves by actions here
            _bHudSpeed.Add(_bPauseBtn); _bHudSpeed.Add(_bSpeedBtn);
            _ui.Root.Add(_bHudSpeed);
            _bPauseLabel = _ui.L(TT("TIME PAUSED · look around and plan · P to carry on", "หยุดเวลาอยู่ · เดินดู วางแผนได้ · กด P เพื่อเล่นต่อ"), 26, true, GameUI.CAmber);
            _bPauseLabel.pickingMode = PickingMode.Ignore;
            _bPauseLabel.style.position = Position.Absolute; _bPauseLabel.style.top = 120; _bPauseLabel.style.alignSelf = Align.Center;
            _bPauseLabel.style.left = 0; _bPauseLabel.style.right = 0; _bPauseLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _bPauseLabel.style.display = DisplayStyle.None;
            _ui.Root.Add(_bPauseLabel);
        }

        void PaceEndBase()
        {
            _bHudSpeed?.RemoveFromHierarchy(); _bHudSpeed = null;
            _bPauseLabel?.RemoveFromHierarchy(); _bPauseLabel = null;
            if (_autoLog == null) Time.timeScale = 1f;
        }

        /// <summary>Every frame in the base: keys and the clock speed. Panels always run at normal speed.</summary>
        void PaceUpdate(Keyboard kb)
        {
            if (_bMode != 0 || _bHudSpeed == null) return;
            if (!_ui.Blocking && kb != null)
            {
                if (kb.pKey.wasPressedThisFrame) { _bPaused = !_bPaused; _sfx.Play("blip"); PaceRefresh(); }
                if (kb.fKey.wasPressedThisFrame && !RestoreMode) { _bSpeed = _bSpeed > 1.5f ? 1f : 2f; _sfx.Play("blip"); PaceRefresh(); }
            }
            bool panel = _ui.ScreenOpen || _ui.Blocking || _pzRoot != null || InCutscene;
            if (_autoLog == null) Time.timeScale = panel ? 1f : _bPaused ? 0f : _bSpeed;
            bool show = !panel && _life != null;
            _bHudSpeed.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            _bPauseLabel.style.display = show && _bPaused ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void PaceRefresh()
        {
            if (_bPauseBtn == null) return;
            _bPauseCaption.text = GameUI.Thai(_bPaused ? TT("Play", "เล่นต่อ") : TT("Pause", "หยุดเวลา"));
            _bSpeedCaption.text = TT("Speed ×", "ความเร็ว ×") + (_bSpeed > 1.5f ? "2" : "1");
        }

        // ------------------------------------------------------------------ the oxygen maker (MOXIE), any time of day

        void MoxieShow()
        {
            var st = _life.State;
            bool ranToday = _bDone.Contains("moxie");
            float pw = LC.o2Maker.power, o2 = LC.o2Maker.o2;
            bool nightPower = st.stock.power - pw + LC.solarPerDay >= LC.lifeSupportPower;
            _ui.ShowScreen(root =>
            {
                var box = new VisualElement(); box.style.width = 1000;
                GameUI.PanelFrame(box, GameUI.CAmber);
                box.style.paddingLeft = box.style.paddingRight = 30; box.style.paddingTop = 18; box.style.paddingBottom = 18;
                box.Add(_ui.L(TT("Oxygen maker", "เครื่องผลิตออกซิเจน"), 36, true, GameUI.CCyan));
                box.Add(_ui.L(TT($"It pulls in the thin Martian air (mostly carbon dioxide) and splits off oxygen. Uses {LbNum(pw)} power, makes {LbNum(o2)} O2. Once a day, takes about 1 hour.",
                    $"ดูดอากาศบาง ๆ ของดาวอังคาร (ส่วนใหญ่เป็นคาร์บอนไดออกไซด์) แล้วแยกออกซิเจนออกมา ใช้ไฟ {LbNum(pw)} ได้ O2 {LbNum(o2)} ทำได้วันละครั้ง ใช้เวลาราว 1 ชั่วโมง"), 22));
                box.Add(_ui.L(TT("NASA's MOXIE on the Perseverance rover made real oxygen on Mars from 2021 to 2023: about as much per hour as a small dog breathes.",
                    "เครื่อง MOXIE บนยาน Perseverance ของ NASA ผลิตออกซิเจนจริงบนดาวอังคารได้ระหว่างปี 2021–2023 ราวชั่วโมงละเท่ากับที่สุนัขตัวเล็กหายใจ"), 17, false, GameUI.CDim));
                box.Add(_ui.L(TT($"Battery now: {LbNum(st.stock.power)} · O2 now: {LbNum(st.stock.o2)} · O2 everyone breathes tonight: {LbNum(_life.O2Tonight)}",
                    $"ไฟตอนนี้: {LbNum(st.stock.power)} · O2 ตอนนี้: {LbNum(st.stock.o2)} · คืนนี้ทุกคนใช้ O2: {LbNum(_life.O2Tonight)}"), 21, true, GameUI.CText));
                string why = ranToday ? TT("Already ran today. The filters need to cool down.", "วันนี้เปิดไปแล้ว ไส้กรองต้องพักให้เย็นก่อน")
                    : !_life.CanRunMaker ? TT($"Not enough power (needs {LbNum(pw)}). Clean the solar panels or save power first.", $"ไฟไม่พอ (ต้องใช้ {LbNum(pw)}) ลองปัดฝุ่นแผงโซลาร์หรือประหยัดไฟก่อน")
                    : !_life.State.schedule.CanSpend(1f) || _life.State.schedule.Hour + 1f > LateHour ? TT("Not enough time left today.", "วันนี้เหลือเวลาไม่พอ") : null;
                if (why == null && !nightPower)
                    box.Add(_ui.L(TT("Careful: after this the battery may be too low for life support tonight.", "ระวัง: เปิดแล้วไฟอาจไม่พอเลี้ยงระบบช่วยชีวิตคืนนี้"), 20, true, GameUI.CAmber));
                if (why != null) box.Add(_ui.L(why, 20, true, GameUI.CRed));
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginTop = 14;
                var go = _ui.B(TT("Start the oxygen maker", "เปิดเครื่องผลิตออกซิเจน"), () =>
                {
                    _ui.HideScreen();
                    StartCoroutine(Act("repair", 1.2f, () =>
                    {
                        if (!_life.CanRunMaker || !_bDone.Add("moxie")) return;
                        _life.ApplyTask(new Stock { power = -pw, o2 = o2 }, 1f);
                        _sfx.Play("good");
                        _ui.Toast(TT($"The oxygen maker hums for an hour. O2 +{LbNum(o2)}, power −{LbNum(pw)}.", $"เครื่องผลิตออกซิเจนทำงานไป 1 ชั่วโมง O2 +{LbNum(o2)} ไฟ −{LbNum(pw)}"), GameUI.CGreen);
                        DayNote(TT("Ran the oxygen maker.", "เปิดเครื่องผลิตออกซิเจน"));
                        BRefreshProps();
                    }));
                }, 24, true);
                go.SetEnabled(why == null);
                row.Add(go);
                var back = _ui.B(TT("Not now", "ไว้ก่อน"), () => _ui.HideScreen(), 22); back.style.marginLeft = 14; row.Add(back);
                box.Add(row);
                root.Add(box);
            });
        }

        // ------------------------------------------------------------------ project board

        sealed class ProjectDef
        {
            public string id, flag, part, puzzle, en, th, bodyEn, bodyTh, factEn, factTh;
            public Stock cost = new Stock();
            public bool repeat;
        }

        const int ProjectsPerDay = 2;

        static readonly ProjectDef[] Projects =
        {
            new ProjectDef { id = "shelter_wall", flag = "shelter_ready", puzzle = "shield", cost = new Stock { water = 1 },
                en = "Water wall round the beds", th = "กำแพงน้ำรอบที่นอน",
                bodyEn = "Stack water bags round the storage shelter. Storm nights and solar storms hurt much less.",
                bodyTh = "เรียงถุงน้ำรอบที่หลบพายุในคลัง คืนพายุและพายุสุริยะจะโดนรังสีน้อยลงมาก",
                factEn = "Water is one of the best shields against space radiation, so NASA studies water walls for crew quarters.",
                factTh = "น้ำกันรังสีในอวกาศได้ดีมาก NASA จึงศึกษาการทำกำแพงน้ำรอบห้องนอนนักบินอวกาศ" },
            new ProjectDef { id = "battery_plus", flag = "battery_plus", part = "cable", puzzle = "wires",
                en = "Second battery string", th = "ต่อแบตเตอรี่ชุดที่สอง",
                bodyEn = "Wire the spare cells in. The battery keeps a little more sunlight every night (+0.5 power each morning).",
                bodyTh = "ต่อเซลล์แบตเตอรี่สำรองเข้าระบบ เก็บแสงแดดได้มากขึ้นทุกคืน (ไฟ +0.5 ทุกเช้า)",
                factEn = "NASA's Ingenuity helicopter charged its batteries from its own small solar panel every Martian day.",
                factTh = "เฮลิคอปเตอร์ Ingenuity ของ NASA ชาร์จแบตเตอรี่จากแผงโซลาร์เล็ก ๆ ของตัวเองทุกวันบนดาวอังคาร" },
            new ProjectDef { id = "grow_lamps", flag = "grow_lamps", puzzle = "switches", cost = new Stock { power = 0.5f },
                en = "Brighter grow lamps", th = "ไฟปลูกผักแรงขึ้น",
                bodyEn = "Tune the red and blue lamps over the plots. Plants grow faster every night.",
                bodyTh = "ปรับไฟสีแดงและน้ำเงินเหนือแปลงผัก ผักจะโตเร็วขึ้นทุกคืน",
                factEn = "NASA's Veggie garden grows lettuce under red and blue LEDs: plants use those colours the most.",
                factTh = "สวน Veggie ของ NASA ปลูกผักกาดใต้ไฟ LED สีแดงกับน้ำเงิน เพราะพืชใช้แสงสองสีนี้มากที่สุด" },
            new ProjectDef { id = "seal_hub", flag = "hub_sealed", part = "tape", puzzle = "leak",
                en = "Seal the hub seams", th = "อุดรอยต่อผนังโถงกลาง",
                bodyEn = "Tape every weak seam in the hub before it cracks. Air leaks become rare.",
                bodyTh = "ติดเทปรอยต่อที่อ่อนแอในโถงกลางก่อนจะร้าว อากาศจะรั่วน้อยลงมาก",
                factEn = "Space station crews check the hull seals often: a tiny leak is easier to stop before it grows.",
                factTh = "ลูกเรือสถานีอวกาศตรวจซีลผนังยานบ่อย ๆ เพราะรอยรั่วเล็ก ๆ อุดง่ายกว่าตอนที่มันใหญ่แล้ว" },
            new ProjectDef { id = "print_fuse", puzzle = "screws", cost = new Stock { power = 0.5f }, repeat = true,
                en = "Print a spare fuse", th = "พิมพ์ฟิวส์สำรอง",
                bodyEn = "Melt scrap into the little printer and make one fuse for the toolbox.",
                bodyTh = "หลอมเศษเหล็กใส่เครื่องพิมพ์เล็ก ๆ แล้วทำฟิวส์ 1 อันเก็บไว้ในตู้เครื่องมือ",
                factEn = "Astronauts on the space station have 3D-printed tools and spare parts instead of waiting for a rocket.",
                factTh = "นักบินอวกาศบนสถานีอวกาศเคยพิมพ์ 3 มิติเครื่องมือและอะไหล่เอง แทนที่จะรอจรวดส่งของ" },
        };

        int ProjectsToday() { int n = 0; foreach (var p in Projects) if (_bDone.Contains("proj_" + p.id)) n++; return n; }

        string ProjectBlock(ProjectDef p)
        {
            var st = _life.State;
            if (p.flag != null && st.flags.Contains(p.flag)) return TT("Done.", "ทำแล้ว");
            if (_bDone.Contains("proj_" + p.id)) return TT("Done today.", "วันนี้ทำแล้ว");
            if (ProjectsToday() >= ProjectsPerDay) return TT($"You already worked on {ProjectsPerDay} projects today.", $"วันนี้ทำโครงการไปแล้ว {ProjectsPerDay} อย่าง");
            if (p.part != null && !BagHas(p.part)) return StoreCount(p.part) > 0 ? TT("Bring the " + ItemName(p.part) + " from the toolbox in storage.", "ไปหยิบ" + ItemName(p.part) + "ที่ตู้เครื่องมือในคลังมาก่อน") : TT("No " + ItemName(p.part) + " left. Search outside.", ItemName(p.part) + "หมดแล้ว ลองหาข้างนอก");
            if (st.stock.water < p.cost.water || st.stock.power < p.cost.power || st.stock.o2 < p.cost.o2 || st.stock.food < p.cost.food) return TT("Not enough supplies.", "ของไม่พอ");
            _life.CardProgress(out int done, out int total);
            if (!st.schedule.CanSpend(1.5f, total - done) || st.schedule.Hour + 1.5f > LateHour) return TT("Not enough time today (keep time for today's jobs).", "วันนี้เหลือเวลาไม่พอ (ต้องเผื่อเวลาให้งานของวันนี้)");
            return null;
        }

        string CostText(ProjectDef p)
        {
            var parts = new List<string>();
            if (p.cost.water > 0) parts.Add(TT("water ", "น้ำ ") + LbNum(p.cost.water));
            if (p.cost.power > 0) parts.Add(TT("power ", "ไฟ ") + LbNum(p.cost.power));
            if (p.cost.o2 > 0) parts.Add("O2 " + LbNum(p.cost.o2));
            if (p.part != null) parts.Add(ItemName(p.part));
            parts.Add(TT("about 1-2 hours", "ราว 1–2 ชั่วโมง"));
            return TT("Uses: ", "ใช้: ") + string.Join(" · ", parts);
        }

        void ProjectsShow()
        {
            _ui.ShowScreen(root =>
            {
                var box = new VisualElement(); box.style.width = 1500;
                GameUI.PanelFrame(box, GameUI.CAmber);
                box.style.paddingLeft = box.style.paddingRight = 30; box.style.paddingTop = 18; box.style.paddingBottom = 18;
                box.Add(_ui.L(TT("Project board", "บอร์ดโครงการ"), 36, true, GameUI.CAmber));
                box.Add(_ui.L(TT($"Make the base yours. Pick up to {ProjectsPerDay} projects a day, in any order. Each one costs something now and helps for the rest of the week.",
                    $"ทำฐานให้เป็นแบบที่เราอยากได้ เลือกทำได้วันละไม่เกิน {ProjectsPerDay} โครงการ ทำอันไหนก่อนก็ได้ ทุกอย่างต้องจ่ายตอนนี้ แต่ช่วยไปจนจบสัปดาห์"), 21));
                box.Add(_ui.L(TT($"Mission: survive {LC.daysTotal} days. ", $"ภารกิจ: อยู่รอดให้ครบ {LC.daysTotal} วัน ") + _loc.T("base.toast.flare", _bFlare.date, _bFlare.cls), 17, false, GameUI.CDim));
                var grid = new VisualElement(); grid.style.flexDirection = FlexDirection.Row; grid.style.flexWrap = Wrap.Wrap; grid.style.marginTop = 10;
                foreach (var p in Projects)
                {
                    string block = ProjectBlock(p);
                    bool done = p.flag != null && _life.State.flags.Contains(p.flag);
                    var card = new VisualElement(); card.style.width = 700; card.style.marginRight = 14; card.style.marginBottom = 12;
                    card.style.paddingLeft = card.style.paddingRight = 16; card.style.paddingTop = 10; card.style.paddingBottom = 10;
                    card.style.backgroundColor = done ? new Color(0.08f, 0.2f, 0.12f, 0.9f) : new Color(0.1f, 0.12f, 0.18f, 0.95f);
                    GameUI.Border(card, done ? GameUI.CGreen : block == null ? GameUI.CAmber : GameUI.CPanel2, 2, 10);
                    card.Add(_ui.L(TT(p.en, p.th), 25, true, done ? GameUI.CGreen : GameUI.CText));
                    card.Add(_ui.L(TT(p.bodyEn, p.bodyTh), 18));
                    if (!done) card.Add(_ui.L(CostText(p), 18, true, GameUI.CAmber));
                    card.Add(_ui.L(TT(p.factEn, p.factTh), 15, false, GameUI.CCyan));
                    if (!done)
                    {
                        var b = _ui.B(block ?? TT("Start", "เริ่มทำ"), () => ProjectStart(p), 19, block == null);
                        b.SetEnabled(block == null); b.style.marginTop = 6;
                        card.Add(b);
                    }
                    grid.Add(card);
                }
                box.Add(grid);
                box.Add(_ui.B(TT("Close", "ปิด"), () => _ui.HideScreen(), 22));
                root.Add(box);
            });
        }

        void ProjectStart(ProjectDef p)
        {
            if (ProjectBlock(p) != null) return;
            _ui.HideScreen();
            var d = new JobDef { id = "proj_" + p.id, spot = "board", en = p.en, th = p.th, bodyEn = p.bodyEn, bodyTh = p.bodyTh, factEn = p.factEn, factTh = p.factTh, ways = new JobWay[0] };
            var w = new JobWay { puzzle = p.puzzle, item = p.part };
            int helpers = JobHelpers("board");
            StartCoroutine(Act("repair", 0.45f, () => PzOpen(p.puzzle, helpers, d, (m, s) => ProjectFinish(p, d, w, m, s, helpers),
                () => _ui.Toast(TT("You stepped back. The project can wait.", "ถอยออกมาก่อน โครงการรอได้"), GameUI.CDim))));
        }

        void ProjectFinish(ProjectDef p, JobDef d, JobWay w, int mistakes, float secs, int helpers)
        {
            _ui.HideScreen();
            if (!_bDone.Add("proj_" + p.id)) return;
            _life.ApplyTask(new Stock { water = -p.cost.water, power = -p.cost.power, o2 = -p.cost.o2, food = -p.cost.food });
            if (p.part != null) BagUse(p.part);
            if (p.flag != null) _life.State.flags.Add(p.flag);
            if (p.id == "print_fuse") ToolboxAdd("fuse");
            JobHiddenCost(d, w, mistakes, secs, helpers);   // like a repair: careless or slow work wastes a little more, and it takes time
            _sfx.Play("good");
            _ui.Toast(TT("Project finished: ", "โครงการเสร็จแล้ว: ") + TT(p.en, p.th), GameUI.CGreen);
            DayNote(TT("Project: ", "โครงการ: ") + TT(p.en, p.th));
            BRefreshProps();
        }

        // ------------------------------------------------------------------ nightly save

        [Serializable]
        sealed class DaySave
        {
            public string pet; public int seed, day, health, morale, seeds, doseHits; public bool petFed, wreck; public float o2, water, food, power, dose;
            public string[] stowed, guests, items, flags, answered, scanKeys, scanVals, boxIds, bag, friendIds;
            public int[] boxN, friendN, plot, plotReady;
            public string restore; public string[] beltIds; public int[] beltN;   // restore mode
        }

        const string DaySaveKey = "save_day";
        int _lbSeed;
        bool _lbContinueDay, _bSeedsRestored;

        /// <summary>After each night: remember the next morning, so Continue starts the run from there.</summary>
        void DaySaveWrite()
        {
            if (_life == null || _autoLog != null) return;
            if (_life.State.outcome != LifeOutcome.Ongoing)
            {
                // a lost run keeps the last morning so the lose screen can offer "retry from this morning"
                if (!LbFailed) { PlayerPrefs.DeleteKey(DaySaveKey); PlayerPrefs.Save(); }
                return;
            }
            var st = _life.State;
            var s = new DaySave
            {
                pet = st.pet, seed = _lbSeed, day = st.day, health = st.health, morale = st.morale, petFed = st.petFed,
                o2 = st.stock.o2, water = st.stock.water, food = st.stock.food, power = st.stock.power,
                stowed = _lbStowed.ToArray(), guests = st.guests.ToArray(), items = new List<string>(st.items).ToArray(),
                flags = new List<string>(st.flags).ToArray(), answered = new List<string>(st.answered).ToArray(),
                seeds = _bSeeds, wreck = _bWreckSearched, dose = _dose, doseHits = _doseHits,
                bag = _bag.ToArray(), plot = (int[])_bPlot.Clone(), plotReady = (int[])_bPlotReady.Clone(),
            };
            var sk = new List<string>(); var sv = new List<string>();
            foreach (var kv in st.scanned) { sk.Add(kv.Key); sv.Add(string.Join(",", kv.Value)); }
            s.scanKeys = sk.ToArray(); s.scanVals = sv.ToArray();
            var bi = new List<string>(); var bn = new List<int>();
            foreach (var kv in _toolbox) { bi.Add(kv.Key); bn.Add(kv.Value); }
            s.boxIds = bi.ToArray(); s.boxN = bn.ToArray();
            var fi = new List<string>(); var fn = new List<int>();
            foreach (var kv in _bFriend) { fi.Add(kv.Key); fn.Add(kv.Value); }
            s.friendIds = fi.ToArray(); s.friendN = fn.ToArray();
            if (_restore != null)
            {
                s.restore = JsonUtility.ToJson(_restore.Snapshot());
                var ri = new List<string>(); var rn = new List<int>();
                foreach (var kv in _belt) { ri.Add(kv.Key); rn.Add(kv.Value); }
                s.beltIds = ri.ToArray(); s.beltN = rn.ToArray();
            }
            PlayerPrefs.SetString(DaySaveKey, JsonUtility.ToJson(s));
            Settings.SaveCheckpoint("lifeboat", _cfg.chapter1.cabinO2Start, _lbStowed, st.pet);
            PlayerPrefs.Save();
        }

        static bool HasDaySave => PlayerPrefs.HasKey(DaySaveKey);
        static int DaySaveDay { get { try { return JsonUtility.FromJson<DaySave>(PlayerPrefs.GetString(DaySaveKey, "")).day; } catch { return 0; } } }

        /// <summary>Called right after the sim is made (before the first card): put the saved morning back.</summary>
        bool DaySaveRestore()
        {
            _bSeedsRestored = false;
            if (!_lbContinueDay) return false;
            _lbContinueDay = false;
            DaySave s;
            try { s = JsonUtility.FromJson<DaySave>(PlayerPrefs.GetString(DaySaveKey, "")); } catch { return false; }
            if (s == null || s.day < 2) return false;
            var st = _life.State;
            st.day = s.day; st.health = s.health; st.morale = s.morale; st.petFed = s.petFed;
            st.stock.o2 = s.o2; st.stock.water = s.water; st.stock.food = s.food; st.stock.power = s.power;
            st.guests.Clear(); st.guests.AddRange(s.guests ?? new string[0]);
            st.items.Clear(); foreach (var i in s.items ?? new string[0]) st.items.Add(i);
            st.flags.Clear(); foreach (var f in s.flags ?? new string[0]) st.flags.Add(f);
            st.answered.Clear(); foreach (var a in s.answered ?? new string[0]) st.answered.Add(a);
            st.scanned.Clear();
            if (s.scanKeys != null) for (int i = 0; i < s.scanKeys.Length; i++) st.scanned[s.scanKeys[i]] = new List<string>(s.scanVals[i].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            _bSeeds = s.seeds; _bSeedsRestored = true; _bWreckSearched = s.wreck; _lbSeed = s.seed;
            _dose = s.dose; _doseHits = s.doseHits;
            _bag.Clear(); if (s.bag != null) _bag.AddRange(s.bag);
            _toolbox.Clear(); if (s.boxIds != null) for (int i = 0; i < s.boxIds.Length; i++) _toolbox[s.boxIds[i]] = s.boxN[i];
            _bFriend.Clear(); if (s.friendIds != null) for (int i = 0; i < s.friendIds.Length; i++) _bFriend[s.friendIds[i]] = s.friendN[i];
            if (s.plot != null) for (int i = 0; i < 3 && i < s.plot.Length; i++) { _bPlot[i] = s.plot[i]; _bPlotReady[i] = s.plotReady[i]; }
            if (_restore != null && !string.IsNullOrEmpty(s.restore))
            {
                _restore.Load(JsonUtility.FromJson<RestoreSnapshot>(s.restore));
                _belt.Clear(); if (s.beltIds != null) for (int i = 0; i < s.beltIds.Length; i++) _belt[s.beltIds[i]] = s.beltN[i];
            }
            _ui.Toast(TT($"Welcome back: the morning of day {s.day}.", $"ยินดีต้อนรับกลับมา: เช้าวันที่ {s.day}"), GameUI.CCyan);
            return true;
        }
    }
}
