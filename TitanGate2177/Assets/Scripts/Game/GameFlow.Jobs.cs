// PROTOTYPE - NOT FOR PRODUCTION
// Question: Is fixing a problem with your hands (fetch the part, carry it there, solve the panel) more exciting than picking a card?
// Date: 2026-11-13
using System;
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// Repair jobs in the walkable base (Among Us-style tasks): a broken thing waits at its place, the job panel
    /// says what it needs, the player fetches the part from the toolbox in the storage room (Tab shows the bag),
    /// carries it there and solves a small hands-on panel. What the repair costs is not shown: it depends on how
    /// well and how fast the panel was solved, plus a little chance (wear and tear), and it shows up on the gauges.
    /// Guests can be called over to help (they walk to the place); engineers can do a whole job for you.
    /// </summary>
    public partial class GameFlow
    {
        sealed class JobWay
        {
            public string item, option, puzzle, puzzle2, guest, flag, en, th;
        }

        sealed class JobDef
        {
            public string id, spot, en, th, bodyEn, bodyTh, factEn, factTh;
            public JobWay[] ways;
        }

        const int BagSlots = 3;
        readonly List<string> _bag = new List<string>();
        readonly Dictionary<string, int> _toolbox = new Dictionary<string, int>();
        static readonly string[] SimItems = { "spare_filter", "data_chip", "shield_kit", "med_kit" };
        static readonly string[] ToolboxOrder = { "fuse", "tape", "cable", "wrench", "sack", "spare_filter", "data_chip", "shield_kit", "med_kit" };

        // a guest called over to help, where they are going, and who already stands there
        readonly Dictionary<string, Vector2> _crewCall = new Dictionary<string, Vector2>();
        readonly HashSet<string> _crewHere = new HashSet<string>();
        JobDef _jobDelegate; JobWay _jobDelegateWay; string _jobDelegateGuest; float _jobDelegateT;
        int _jobsFixed, _jobMistakes;

        string TT(string en, string th) => CrewText(en, th);

        void JobReset()
        {
            _bag.Clear(); _toolbox.Clear();
            _toolbox["fuse"] = 2; _toolbox["tape"] = 2; _toolbox["cable"] = 1; _toolbox["wrench"] = 1; _toolbox["sack"] = 1;
            _crewCall.Clear(); _crewHere.Clear();
            _jobDelegate = null; _jobDelegateWay = null; _jobDelegateGuest = null;
            _jobsFixed = 0; _jobMistakes = 0;
            RadReset();
        }

        // ------------------------------------------------------------------ items

        static bool IsSimItem(string id) => Array.IndexOf(SimItems, id) >= 0;

        string ItemName(string id)
        {
            switch (id)
            {
                case "fuse": return TT("Fuse", "ฟิวส์");
                case "tape": return TT("Patch tape", "เทปซ่อมรอยรั่ว");
                case "cable": return TT("Cable", "สายไฟ");
                case "wrench": return TT("Wrench", "ประแจ");
                case "sack": return TT("Empty sacks", "กระสอบเปล่า");
                case "water_bag": return TT("Water bag", "ถุงน้ำ");
                case "spare_filter": return TT("Spare air filter", "ไส้กรองอากาศสำรอง");
                case "data_chip": return TT("Data chip", "ชิปข้อมูล");
                case "shield_kit": return TT("Shield panels", "แผ่นกันรังสี");
                case "med_kit": return TT("First aid kit", "กล่องปฐมพยาบาล");
                default: return id;
            }
        }

        string ItemInfo(string id)
        {
            switch (id)
            {
                case "fuse": return TT("Replaces a burnt fuse in the power room.", "ใช้แทนฟิวส์ที่ขาดในห้องไฟฟ้า");
                case "tape": return TT("Seals a crack so air stops leaking.", "ปิดรอยร้าวไม่ให้อากาศรั่ว");
                case "cable": return TT("Joins broken wires (the dish outside).", "ต่อสายไฟที่ขาด (จานสัญญาณข้างนอก)");
                case "wrench": return TT("Tightens pipes and bolts. You keep it.", "ขันท่อและน็อต ใช้แล้วไม่หมด");
                case "sack": return TT("Fill with Mars soil to block radiation.", "ใส่ดินดาวอังคารไว้กันรังสี");
                case "water_bag": return TT("Water for washing or a radiation wall.", "น้ำสำหรับล้างของหรือทำกำแพงกันรังสี");
                case "spare_filter": return TT("A clean filter for the air machine.", "ไส้กรองใหม่สำหรับเครื่องฟอกอากาศ");
                case "data_chip": return TT("Holds the rescue codes for the dish.", "เก็บรหัสขอความช่วยเหลือสำหรับจานสัญญาณ");
                case "shield_kit": return TT("Ready-made panels for the storm shelter.", "แผ่นสำเร็จรูปสำหรับที่หลบพายุ");
                case "med_kit": return TT("Use it from the bag: health +1.", "กดใช้จากกระเป๋า: สุขภาพ +1");
                default: return "";
            }
        }

        int StoreCount(string id)
        {
            if (IsSimItem(id)) return _life != null && _life.State.items.Contains(id) && !_bag.Contains(id) ? 1 : 0;
            return _toolbox.TryGetValue(id, out var n) ? n : 0;
        }

        bool BagHas(string id) => _bag.Contains(id);

        bool BagTake(string id)
        {
            if (_bag.Count >= BagSlots) { _sfx.Play("bad"); _ui.Toast(TT("Your bag is full (3 things). Put something back first.", "กระเป๋าเต็มแล้ว (ถือได้ 3 ชิ้น) เก็บของคืนก่อน"), GameUI.CAmber); return false; }
            if (id == "water_bag")
            {
                if (_life.State.stock.water < 1) { _sfx.Play("bad"); _ui.Toast(TT("No water left to fill a bag.", "น้ำไม่พอจะใส่ถุงแล้ว"), GameUI.CRed); return false; }
            }
            else if (StoreCount(id) <= 0) return false;
            else if (!IsSimItem(id)) _toolbox[id]--;
            _bag.Add(id);
            _sfx.Play("card");
            return true;
        }

        void BagReturn(string id)
        {
            if (!_bag.Remove(id)) return;
            if (!IsSimItem(id) && id != "water_bag") _toolbox[id] = (_toolbox.TryGetValue(id, out var n) ? n : 0) + 1;
            _sfx.Play("click");
        }

        /// <summary>A used part leaves the bag. Sim items are removed by the sim when the option consumes them.</summary>
        void BagUse(string id)
        {
            if (id == null || id == "wrench") return;
            _bag.Remove(id);
        }

        /// <summary>Salvage outside sometimes turns up spare parts for the toolbox.</summary>
        void ToolboxAdd(string id) => _toolbox[id] = (_toolbox.TryGetValue(id, out var n) ? n : 0) + 1;

        // ------------------------------------------------------------------ jobs

        static Dictionary<string, JobDef> _jobs;

        static JobWay W(string item, string option, string puzzle, string en, string th, string puzzle2 = null, string guest = null, string flag = null)
            => new JobWay { item = item, option = option, puzzle = puzzle, puzzle2 = puzzle2, guest = guest, flag = flag, en = en, th = th };

        static Dictionary<string, JobDef> Jobs
        {
            get
            {
                if (_jobs != null) return _jobs;
                _jobs = new Dictionary<string, JobDef>();
                void Add(JobDef d) => _jobs[d.id] = d;
                Add(new JobDef
                {
                    id = "filter", spot = "air",
                    en = "The air filter is clogged", th = "ไส้กรองอากาศอุดตัน",
                    bodyEn = "Mars dust from the landing clogged the air machine. Fix it, or breathing gets hard tonight.",
                    bodyTh = "ฝุ่นดาวอังคารจากตอนลงจอดอุดเครื่องฟอกอากาศ ถ้าไม่แก้ คืนนี้จะหายใจลำบาก",
                    factEn = "Mars dust is finer than flour and sticks to everything, so NASA designs filters that can be cleaned or swapped by hand.",
                    factTh = "ฝุ่นดาวอังคารละเอียดกว่าแป้งและเกาะทุกอย่าง NASA จึงออกแบบไส้กรองให้ถอดล้างหรือเปลี่ยนเองได้",
                    ways = new[]
                    {
                        W("spare_filter", "swap", "screws", "Swap in the spare filter", "เปลี่ยนเป็นไส้กรองสำรอง"),
                        W("water_bag", "wash", "scrub", "Wash the old filter", "ล้างไส้กรองเก่าด้วยน้ำ"),
                    },
                });
                Add(new JobDef
                {
                    id = "recycler", spot = "recycler",
                    en = "The water recycler is leaking", th = "เครื่องรีไซเคิลน้ำรั่ว",
                    bodyEn = "Pipes inside the recycler came loose. Water we should drink again is running away.",
                    bodyTh = "ท่อในเครื่องรีไซเคิลน้ำหลุด น้ำที่ควรนำกลับมาดื่มได้อีกกำลังไหลทิ้ง",
                    factEn = "On the International Space Station the water system recovers about 98% of water, even from sweat and urine.",
                    factTh = "บนสถานีอวกาศนานาชาติ ระบบน้ำนำน้ำกลับมาใช้ได้ประมาณ 98% รวมทั้งเหงื่อและปัสสาวะ",
                    ways = new[]
                    {
                        W("wrench", "repair", "pipes", "Reconnect the pipes", "ต่อท่อใหม่ให้ถูก"),
                        W(null, "tik_repair", null, "Ask Tik to fix it", "ให้ติ๊กซ่อมให้", guest: "tik"),
                    },
                });
                Add(new JobDef
                {
                    id = "storm_warning", spot = "shelter",
                    en = "A solar storm is coming", th = "พายุสุริยะกำลังมา",
                    bodyEn = "ORBI saw a flare on the Sun. Build a wall round the storm bunk in storage before the fast particles arrive.",
                    bodyTh = "ORBI เห็นเปลวสุริยะ ต้องสร้างกำแพงรอบที่นอนหลบพายุในคลัง ก่อนอนุภาคพลังงานสูงจะมาถึง",
                    factEn = "Water is full of hydrogen, which stops fast particles well. NASA plans storm shelters walled with water and supplies.",
                    factTh = "น้ำมีไฮโดรเจนมาก จึงกันอนุภาคพลังงานสูงได้ดี NASA วางแผนให้ห้องหลบพายุล้อมด้วยน้ำและเสบียง",
                    ways = new[]
                    {
                        W("water_bag", "water_wall", "shield", "Stack water bags round the bunk", "เรียงถุงน้ำรอบที่นอน"),
                        W("shield_kit", "shield_kit", "shield", "Fit the shield panels", "ติดแผ่นกันรังสี"),
                    },
                });
                Add(new JobDef
                {
                    id = "storm_last", spot = "shelter",
                    en = "The storm is almost here", th = "พายุใกล้มาถึงแล้ว",
                    bodyEn = "The shelter is still open. Fill sacks with Mars soil and pile them round the bunk.",
                    bodyTh = "ที่หลบพายุยังไม่พร้อม ตักดินดาวอังคารใส่กระสอบแล้วกองรอบที่นอน",
                    factEn = "About a metre of Mars soil (regolith) blocks a lot of radiation. Some plans bury the base under it.",
                    factTh = "ดินดาวอังคาร (เรโกลิธ) หนาราวหนึ่งเมตรกันรังสีได้มาก บางแผนจึงฝังฐานไว้ใต้ดิน",
                    ways = new[] { W("sack", "sandbags", "shield", "Pile soil sacks round the bunk", "กองกระสอบดินรอบที่นอน") },
                });
                Add(new JobDef
                {
                    id = "storm_power", spot = "panel",
                    en = "Power surge from the storm", th = "ไฟกระชากจากพายุ",
                    bodyEn = "The storm can make the power surge. Switch off what we can live without, keep what keeps us alive.",
                    bodyTh = "พายุทำให้ไฟกระชากได้ ปิดเครื่องที่ไม่จำเป็น แต่ต้องเปิดเครื่องที่ช่วยชีวิตไว้",
                    factEn = "Real spacecraft have a 'safe mode': when something goes wrong they switch off everything except what they need to survive.",
                    factTh = "ยานอวกาศจริงมี 'โหมดปลอดภัย' เมื่อมีปัญหาจะปิดทุกอย่าง เหลือแค่ระบบที่จำเป็นต่อการอยู่รอด",
                    ways = new[] { W(null, "safe_mode", "switches", "Put the base in safe mode", "เข้าโหมดปลอดภัย") },
                });
                foreach (var id in new[] { "beacon", "beacon_last" })
                    Add(new JobDef
                    {
                        id = id, spot = "beacon",
                        en = "The rescue dish is broken", th = "จานส่งสัญญาณเสีย",
                        bodyEn = "The dish outside has a cut cable and points the wrong way. Without it nobody hears us.",
                        bodyTh = "จานส่งสัญญาณข้างนอกสายไฟขาดและหันผิดทิศ ถ้าไม่ซ่อมจะไม่มีใครได้ยินเรา",
                        factEn = "A radio signal from Mars takes 3 to 22 minutes to reach Earth, depending on where the two planets are.",
                        factTh = "สัญญาณวิทยุจากดาวอังคารใช้เวลา 3–22 นาทีกว่าจะถึงโลก ขึ้นกับว่าดาวทั้งสองอยู่ห่างกันแค่ไหน",
                        ways = new[]
                        {
                            W("data_chip", "chip", "tune", "Load the data chip, aim the dish", "ใส่ชิปข้อมูล แล้วหันจาน"),
                            W("cable", "climb", "wires", "Climb up, join the cable, aim the dish", "ปีนขึ้นไปต่อสายไฟ แล้วหันจาน", puzzle2: "tune"),
                            W(null, "nok_part", "tune", "Fit Nok's spare part, aim the dish", "ใส่ชิ้นส่วนของน็อก แล้วหันจาน", flag: "nok_helped"),
                            W(null, "tik_fix", null, "Ask Tik to fix it", "ให้ติ๊กซ่อมให้", guest: "tik"),
                        },
                    });
                foreach (var id in new[] { "coords", "coords_last" })
                    Add(new JobDef
                    {
                        id = id, spot = "radio",
                        en = "Send our position", th = "ส่งพิกัดให้ทีมกู้ภัย",
                        bodyEn = "The dish works. Find the satellite's radio channel as it passes over, then send where we are.",
                        bodyTh = "จานพร้อมแล้ว หาช่องวิทยุของดาวเทียมตอนที่บินผ่าน แล้วส่งตำแหน่งของเรา",
                        factEn = "Rovers on Mars send most of their data through orbiters such as Mars Reconnaissance Orbiter, which pass over a few times a day.",
                        factTh = "ยานบนพื้นดาวอังคารส่งข้อมูลส่วนใหญ่ผ่านยานโคจร เช่น Mars Reconnaissance Orbiter ที่บินผ่านวันละไม่กี่ครั้ง",
                        ways = new[]
                        {
                            W(null, "send", "tune", "Tune the radio and send", "ปรับคลื่นแล้วส่ง"),
                            W(null, "crank", "crank", "No power? Crank the radio by hand", "ไฟไม่พอ? หมุนไดนาโมด้วยมือ"),
                        },
                    });
                Add(new JobDef
                {
                    id = "em_power", spot = "breaker",
                    en = "Short circuit!", th = "ไฟฟ้าลัดวงจร!",
                    bodyEn = "A fuse burnt out and the power room locked itself. Fit a new fuse and join each wire to its own colour.",
                    bodyTh = "ฟิวส์ขาดและห้องไฟฟ้าล็อกตัวเอง ใส่ฟิวส์ใหม่แล้วต่อสายไฟให้ตรงสี",
                    factEn = "Astronauts train to repair electrics themselves, because help from Earth is far away.",
                    factTh = "นักบินอวกาศฝึกซ่อมระบบไฟเอง เพราะความช่วยเหลือจากโลกอยู่ไกลมาก",
                    ways = new[]
                    {
                        W("fuse", null, "wires", "Fit the fuse, join the wires", "ใส่ฟิวส์ ต่อสายไฟ"),
                        W(null, null, null, "Ask an engineer to fix it", "ให้ช่างซ่อมให้", guest: "engineer"),
                        W(null, null, "wires", "No fuse? Bridge it with bare wire (risky)", "ไม่มีฟิวส์? ต่อสายตรงแทน (เสี่ยง)"),
                    },
                });
                Add(new JobDef
                {
                    id = "em_leak", spot = "leak",
                    en = "Air leak!", th = "อากาศรั่ว!",
                    bodyEn = "A crack in the hub wall is hissing. Tape over every crack, then bring the pressure back up.",
                    bodyTh = "ผนังโถงกลางมีรอยร้าว อากาศกำลังรั่ว ติดเทปปิดทุกรอย แล้วเติมความดันกลับขึ้นมา",
                    factEn = "On the space station, astronauts once hunted a tiny leak by watching which way floating tea leaves drifted.",
                    factTh = "บนสถานีอวกาศ นักบินอวกาศเคยตามหารูรั่วเล็ก ๆ โดยดูว่าใบชาที่ลอยอยู่ไหลไปทางไหน",
                    ways = new[]
                    {
                        W("tape", null, "leak", "Tape the cracks, refill the air", "ติดเทป เติมอากาศ"),
                        W(null, null, null, "Ask an engineer to fix it", "ให้ช่างซ่อมให้", guest: "engineer"),
                        W(null, null, "leak", "No tape? Stuff a spare shirt in the crack (risky)", "ไม่มีเทป? ยัดเสื้อสำรองอุดรอยร้าว (เสี่ยง)"),
                    },
                });
                return _jobs;
            }
        }

        /// <summary>The repair job waiting at a spot right now: today's card, or an emergency.</summary>
        JobDef JobAt(string spotId)
        {
            if (_bMode != 0 || _life == null) return null;
            if (_emType == "power" && spotId == "breaker") return Jobs["em_power"];
            if (_emType == "leak" && spotId == "leak") return Jobs["em_leak"];
            if (_lbView == LbView.Card && _lbCard != null && string.IsNullOrEmpty(_lbCard.visitor) && Jobs.TryGetValue(_lbCard.id, out var d) && d.spot == spotId) return d;
            return null;
        }

        JobDef CardJob => _bMode == 0 && _lbView == LbView.Card && _lbCard != null && Jobs.TryGetValue(_lbCard.id, out var d) ? d : null;

        string WayGuest(JobWay w)
        {
            if (w.guest == null || _life == null) return null;
            if (w.guest == "engineer") { foreach (var g in _life.State.guests) if (g == "rex" || g == "jules" || g == "tik") return g; return null; }
            return _life.State.guests.Contains(w.guest) ? w.guest : null;
        }

        LifeOption WayOption(JobDef d, JobWay w) => d.id.StartsWith("em_") || w.option == null ? null : _lbCard.options.Find(o => o.id == w.option);

        /// <summary>Can this way be used now? reason = what is missing (never what it costs).</summary>
        bool WayReady(JobDef d, JobWay w, out string reason)
        {
            reason = null;
            if (w.guest != null)
            {
                if (WayGuest(w) == null) { reason = w.guest == "engineer" ? TT("Needs an engineer guest (Rex, Jules or Tik).", "ต้องมีช่างพักอยู่ในฐาน (เร็กซ์ จูลส์ หรือติ๊ก)") : TT("Needs " + LbName(w.guest) + " staying here.", "ต้องมี" + LbName(w.guest) + "พักอยู่ในฐาน"); return false; }
                if (_jobDelegate != null) { reason = TT("Someone is already on it.", "มีคนกำลังซ่อมอยู่แล้ว"); return false; }
            }
            if (w.flag != null && !_life.State.flags.Contains(w.flag)) { reason = TT("You do not have this part.", "ยังไม่มีชิ้นส่วนนี้"); return false; }
            if (w.item != null && !BagHas(w.item))
            {
                reason = w.item == "water_bag" ? TT("Fetch a water bag from the water shelf in the pod.", "ไปเอาถุงน้ำที่ชั้นวางน้ำในยานชูชีพ")
                    : StoreCount(w.item) > 0 ? TT("Fetch it from the toolbox in storage.", "ไปหยิบที่ตู้เครื่องมือในคลัง")
                    : TT("None left. Search outside for parts.", "หมดแล้ว ลองหาของข้างนอก");
                return false;
            }
            var opt = WayOption(d, w);
            if (opt != null && !_life.CanChoose(opt, out _)) { reason = TT("Not possible with what the base has now.", "ตอนนี้ฐานมีของไม่พอสำหรับวิธีนี้"); return false; }
            if (w.option != null && !d.id.StartsWith("em_") && opt == null) return false;
            return true;
        }

        /// <summary>
        /// The job panel: what broke, a NASA fact, and the ways to fix it with what each needs. No costs are shown;
        /// friends nearby can be called over to help.
        /// </summary>
        void JobShow(JobDef d)
        {
            _bOpen = !d.id.StartsWith("em_");
            _bPath = null; _bGoal = null;
            var spot = BSpotById(d.spot);
            _ui.ShowScreen(root =>
            {
                var box = new VisualElement(); box.style.width = 1280;
                box.style.backgroundColor = new Color(0.04f, 0.05f, 0.1f, 0.97f); GameUI.Border(box, d.id.StartsWith("em_") ? GameUI.CRed : GameUI.CAmber, 3, 16);
                box.style.paddingLeft = box.style.paddingRight = 34; box.style.paddingTop = 20; box.style.paddingBottom = 20;
                var head = new VisualElement(); head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center;
                var face = d.id.StartsWith("em_") ? Art.JobItem(d.ways[0].item) : spot?.sr != null ? spot.sr.sprite : null;
                if (face != null)
                {
                    var sp = face; float k = Mathf.Min(150f / sp.rect.height, 190f / sp.rect.width);
                    var pic = new VisualElement(); pic.style.width = sp.rect.width * k; pic.style.height = sp.rect.height * k; pic.style.backgroundImage = new StyleBackground(sp); pic.style.marginRight = 24; pic.style.flexShrink = 0;
                    head.Add(pic);
                }
                var col = new VisualElement(); col.style.flexShrink = 1;
                col.Add(_ui.L(TT("REPAIR JOB", "งานซ่อม"), 19, true, GameUI.CDim));
                col.Add(_ui.L(TT(d.en, d.th), 38, true, d.id.StartsWith("em_") ? GameUI.CRed : GameUI.CAmber));
                col.Add(_ui.L(TT(d.bodyEn, d.bodyTh), 23));
                head.Add(col); box.Add(head);

                var fact = new VisualElement(); fact.style.marginTop = 12; fact.style.paddingLeft = fact.style.paddingRight = 14; fact.style.paddingTop = fact.style.paddingBottom = 8;
                fact.style.backgroundColor = new Color(0.03f, 0.12f, 0.16f, 0.95f); GameUI.Border(fact, GameUI.CCyanD, 2, 10);
                fact.Add(_ui.L(TT("NASA FACT  ", "ความรู้จาก NASA  ") + TT(d.factEn, d.factTh), 19, false, GameUI.CCyan));
                box.Add(fact);

                if (_jobDelegate == d)
                    box.Add(_ui.L(TT(LbName(_jobDelegateGuest) + " is working on it. You can do something else.", LbName(_jobDelegateGuest) + " กำลังซ่อมอยู่ ไปทำอย่างอื่นก่อนได้"), 22, true, GameUI.CGreen));

                // ---- ways to fix it
                box.Add(_ui.L(TT("How will you fix it?", "จะซ่อมยังไง?"), 22, true, GameUI.CAmber));
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap; row.style.marginTop = 6;
                var group = new List<VisualElement>();
                foreach (var w in d.ways)
                {
                    var way = w;
                    if (way.flag != null && !_life.State.flags.Contains(way.flag)) continue;
                    if (way.option != null && !d.id.StartsWith("em_") && _lbCard.options.Find(o => o.id == way.option) == null) continue;
                    bool ok = WayReady(d, way, out var why);
                    var hint = new VisualElement(); hint.style.flexDirection = FlexDirection.Row; hint.style.alignItems = Align.Center; hint.style.flexWrap = Wrap.Wrap;
                    if (way.item != null)
                    {
                        var ic = new Image { sprite = Art.JobItem(way.item), scaleMode = ScaleMode.ScaleToFit }; ic.style.width = 40; ic.style.height = 40; hint.Add(ic);
                        hint.Add(_ui.L(" " + ItemName(way.item) + (BagHas(way.item) ? TT(" · in your bag", " · อยู่ในกระเป๋า") : ""), 17, true, BagHas(way.item) ? GameUI.CGreen : GameUI.CDim));
                    }
                    else if (way.guest != null) hint.Add(_ui.L(TT("They walk over and do it while you do something else.", "เขาจะเดินมาซ่อมให้ ระหว่างนั้นคุณไปทำอย่างอื่นได้"), 16, false, GameUI.CDim));
                    else hint.Add(_ui.L(TT("No part needed.", "ไม่ต้องใช้อุปกรณ์"), 16, false, GameUI.CDim));
                    var card = BCard(TT(way.en, way.th), hint, () => JobStart(d, way), ok, null, group, 380, way.guest != null ? GameUI.CGreen : GameUI.CCyanD, 21);
                    card.style.minHeight = 116; card.style.marginLeft = 0; card.style.marginRight = 12; card.style.marginTop = 6;
                    if (!ok && why != null) { var l = _ui.L(why, 16, false, GameUI.CRed); l.style.marginTop = 6; card.Add(l); }
                    row.Add(card);
                }
                box.Add(row);

                // ---- friends who can come and help
                var helpers = new VisualElement(); helpers.style.flexDirection = FlexDirection.Row; helpers.style.flexWrap = Wrap.Wrap; helpers.style.alignItems = Align.Center; helpers.style.marginTop = 12;
                if (_life.State.guests.Count > 0)
                {
                    helpers.Add(_ui.L(TT("Friends:", "เพื่อน:"), 20, true, GameUI.CText));
                    foreach (var g in _life.State.guests)
                    {
                        string id = g;
                        if (_crewHere.Contains(id) && _crewCall.TryGetValue(id, out var at) && spot != null && Vector2.Distance(at, spot.stand) < 2.5f)
                            helpers.Add(_ui.L("  " + TT(LbName(id) + " is here to help", LbName(id) + " มาช่วยอยู่ตรงนี้แล้ว"), 19, true, GameUI.CGreen));
                        else if (_crewCall.ContainsKey(id))
                            helpers.Add(_ui.L("  " + TT(LbName(id) + " is on the way", LbName(id) + " กำลังเดินมา"), 19, false, GameUI.CCyan));
                        else if (spot != null)
                        {
                            var b = _ui.B(TT("Call " + LbName(id), "เรียก" + LbName(id) + "มาช่วย"), () => { CrewCall(id, spot.stand); JobShow(d); }, 19);
                            helpers.Add(b);
                        }
                    }
                    helpers.Add(_ui.L(TT("  A helper standing here makes the panel easier.", "  มีเพื่อนยืนช่วยอยู่ข้าง ๆ งานจะง่ายขึ้น"), 16, false, GameUI.CDim));
                }
                box.Add(helpers);

                var foot = new VisualElement(); foot.style.flexDirection = FlexDirection.Row; foot.style.marginTop = 16;
                foot.Add(_ui.B(TT("Not now", "ไว้ก่อน"), () => { _bOpen = false; _ui.HideScreen(); }, 22, true));
                if (!d.id.StartsWith("em_"))
                {
                    var last = _lbCard.options[_lbCard.options.Count - 1];
                    var giveUp = _ui.B(TT("Leave it broken (risky)", "ไม่ซ่อม ยอมเสี่ยง"), () => { _bOpen = true; _ui.HideScreen(); JobGiveUp(last); }, 20);
                    giveUp.style.marginLeft = 16; giveUp.SetEnabled(_life.CanChoose(last, out _));
                    foot.Add(giveUp);
                }
                foot.Add(_ui.L(TT("   Tab: your bag", "   Tab: เปิดกระเป๋า"), 18, false, GameUI.CDim));
                box.Add(foot);
                root.Add(box);
            });
        }

        void JobGiveUp(LifeOption last)
        {
            var done = _life.Choose(_lbCard.id, last.id);
            if (done == null) { _bOpen = false; return; }
            if (_lbCard.id == "recycler") _life.State.flags.Add("recycler_broken");
            if (Jobs.TryGetValue(_lbCard.id, out var jd)) DayNote(TT("Left broken: ", "ปล่อยไว้ไม่ซ่อม: ") + TT(jd.en, jd.th));
            JobRelease();
            _sfx.Play("bad");
            _ui.Toast(TT("You left it broken.", "ปล่อยไว้ไม่ซ่อม"), GameUI.CAmber);
            LbNextCard();
        }

        void JobStart(JobDef d, JobWay w)
        {
            _ui.HideScreen();
            string guest = WayGuest(w);
            if (w.guest != null && guest != null)
            {
                _jobDelegate = d; _jobDelegateWay = w; _jobDelegateGuest = guest; _jobDelegateT = 0;
                var spot = BSpotById(d.spot);
                CrewCall(guest, spot != null ? spot.stand : _bPos);
                _bOpen = false;
                _ui.Toast(TT(LbName(guest) + " will fix it. Carry on with your day.", LbName(guest) + " จะซ่อมให้ ไปทำอย่างอื่นได้เลย"), GameUI.CGreen);
                return;
            }
            int helpers = JobHelpers(d.spot);
            if (BagHas("shield_kit") && w.item == "shield_kit") helpers += 2;
            StartCoroutine(Act("repair", 0.45f, () =>
                PzOpen(w.puzzle, helpers, d, (m, s) =>
                {
                    if (w.puzzle2 == null) { JobFinish(d, w, m, s, helpers); return; }
                    PzOpen(w.puzzle2, helpers, d, (m2, s2) => JobFinish(d, w, m + m2, s + s2, helpers), () => JobCancel(d));
                }, () => JobCancel(d))));
        }

        void JobCancel(JobDef d)
        {
            _bOpen = false;
            _ui.HideScreen();
            _ui.Toast(TT("You stepped back. The job still waits.", "ถอยออกมาก่อน งานยังรออยู่"), GameUI.CDim);
        }

        int JobHelpers(string spotId)
        {
            var spot = BSpotById(spotId);
            if (spot == null) return 0;
            int n = 0;
            foreach (var g in _crewHere) if (_crewCall.TryGetValue(g, out var at) && Vector2.Distance(at, spot.stand) < 2.5f) n++;
            return n;
        }

        /// <summary>
        /// The job is done. Its card answer (or the emergency) resolves, then the hidden cost: more mistakes and a
        /// slower fix cost more, a helper halves it, and there is always a little wear and tear. Nothing is
        /// announced except that it works again; the gauges show what it took.
        /// </summary>
        void JobFinish(JobDef d, JobWay w, int mistakes, float secs, int helpers)
        {
            _ui.HideScreen();
            bool em = d.id.StartsWith("em_");
            BagUse(w.item);
            _jobsFixed++; _jobMistakes += mistakes;
            foreach (var g in new List<string>(_crewHere)) if (JobHelpers(d.spot) > 0) _bFriend[g] = Friend(g) + 1;
            JobHiddenCost(d, w, mistakes, secs, helpers);
            DayNote(TT("Fixed: ", "ซ่อมแล้ว: ") + TT(d.en, d.th) + (helpers > 0 ? TT(" (with a friend)", " (มีเพื่อนช่วย)") : ""));
            _sfx.Play("good");
            string done = d.id == "em_power" || d.id == "storm_power" ? TT("The power hums again.", "ไฟกลับมาทำงานแล้ว")
                : d.id == "em_leak" ? TT("The hissing stopped. The air holds.", "เสียงฟู่หยุดแล้ว อากาศไม่รั่วแล้ว")
                : d.id.StartsWith("storm") ? TT("The shelter is ready.", "ที่หลบพายุพร้อมแล้ว")
                : TT("Fixed! It works again.", "ซ่อมเสร็จ! กลับมาใช้ได้แล้ว");
            _ui.Toast(done, GameUI.CGreen);
            if (mistakes > 0) _ui.Toast(TT("ORBI: a few sparks on the way. Keep an eye on the gauges.", "ORBI: มีพลาดไปบ้าง ลองดูมาตรวัดนะ"), GameUI.CAmber);
            JobRelease();
            if (em) { EmEnd(); _bOpen = false; return; }
            var opt = WayOption(d, w);
            if (opt == null || _life.Choose(_lbCard.id, opt.id) == null) { _bOpen = false; return; }
            _life.State.flags.Remove("recycler_broken");
            if (d.id.StartsWith("beacon") && w.option == "climb") RadAdd(1.5f, TT("Working outside on the dish", "ปีนซ่อมจานข้างนอก"));
            LbNextCard();
        }

        void JobHiddenCost(JobDef d, JobWay w, int mistakes, float secs, int helpers)
        {
            var rng = new System.Random(Environment.TickCount ^ (_life.State.day * 977));
            string puzzle = w.puzzle ?? "";
            string res = puzzle == "leak" || puzzle == "screws" ? "o2" : puzzle == "pipes" || puzzle == "scrub" ? "water" : puzzle == "shield" ? "o2" : "power";
            float loss = mistakes * 0.5f;
            if (rng.NextDouble() < (helpers > 0 ? 0.2 : 0.45)) loss += 0.5f;            // wear and tear
            if (secs > 60) loss += 0.5f;                                                 // a slow fix wastes more
            if (helpers > 0) loss *= 0.5f;
            if (mistakes == 0 && secs < 25 && rng.NextDouble() < 0.35) loss -= 0.5f;    // a clean, quick fix saves a little
            if (d.id.StartsWith("em_") && w.item == null && w.guest == null) loss += 1f;  // a bare-hands patch without the right part never holds well
            loss = Mathf.Round(loss * 2) / 2f;
            var st = _life.State.stock;
            float have = res == "o2" ? st.o2 : res == "water" ? st.water : st.power;
            float take = Mathf.Clamp(loss, -1f, Mathf.Max(0, have - (res == "o2" ? _life.O2Tonight : 0)));
            if (Mathf.Abs(take) > 0.01f)
                _life.ApplyTask(res == "o2" ? new Stock { o2 = -take } : res == "water" ? new Stock { water = -take } : new Stock { power = -take });
            float hours = Mathf.Clamp(0.5f + secs / 40f, 0.5f, 2f) * (helpers > 0 ? 0.7f : 1f);
            _life.State.schedule.TrySpend(Mathf.Min(hours, Mathf.Max(0, LateHour - 0.2f - _life.State.schedule.Hour)));
            if (_playtest) Log($"job {d.id} way {w.option ?? w.puzzle} mistakes {mistakes} secs {secs:0} helpers {helpers} hidden {res} {-take:+0.#;-0.#;0}");
        }

        void JobRelease()
        {
            foreach (var g in _crewHere) _crewCall.Remove(g);
            _crewHere.Clear();
            _crewCall.Clear();
            _jobDelegate = null; _jobDelegateWay = null; _jobDelegateGuest = null;
        }

        void CrewCall(string id, Vector2 to)
        {
            int n = _crewCall.Count;
            _crewCall[id] = to + new Vector2(n % 2 == 0 ? 1.1f : -1.1f, 0.1f);
            _crewHere.Remove(id);
            _sfx.Play("blip");
            _ui.Toast(TT(LbName(id) + " is coming to help.", LbName(id) + " กำลังเดินมาช่วย"), GameUI.CCyan);
        }

        /// <summary>Every frame in the base: a delegated job finishes after its helper worked there for about an hour.</summary>
        void JobUpdate(float dt)
        {
            if (_bMode != 0 || _life == null) return;
            if (_jobDelegate == null) return;
            bool em = _jobDelegate.id.StartsWith("em_");
            bool stillThere = em ? _emType != null && Jobs.TryGetValue("em_" + _emType, out var ej) && ej == _jobDelegate : CardJob == _jobDelegate;
            if (!stillThere || !_life.State.guests.Contains(_jobDelegateGuest)) { JobRelease(); return; }
            if (!_crewHere.Contains(_jobDelegateGuest) || _ui.Blocking || _bGather) return;
            _jobDelegateT += dt;
            if (_jobDelegateT < SecondsPerHour * 0.8f) return;
            var d = _jobDelegate; var w = _jobDelegateWay; string who = _jobDelegateGuest;
            _bFriend[who] = Friend(who) + 1;
            if (em)
            {
                if (d.id == "em_power") _life.ApplyTask(new Stock { power = 0.5f });
                _ui.Toast(_loc.T("base.em.guest_fixed", LbName(who)), GameUI.CGreen);
                JobRelease(); EmEnd(); return;
            }
            var opt = WayOption(d, w) ?? WayOption(d, d.ways[0]);
            if (opt == null || !_life.CanChoose(opt, out _)) { JobRelease(); return; }
            _bOpen = true;
            _life.Choose(_lbCard.id, opt.id);
            _life.State.schedule.TrySpend(0.5f);
            _ui.Toast(TT(LbName(who) + " fixed it for you!", LbName(who) + " ซ่อมเสร็จให้แล้ว!"), GameUI.CGreen);
            _sfx.Play("good");
            JobRelease();
            LbNextCard();
        }

        // ------------------------------------------------------------------ toolbox and bag

        /// <summary>The toolbox in the storage room: take parts into the bag (3 slots) or put them back.</summary>
        void JobShowToolbox()
        {
            _ui.ShowScreen(root =>
            {
                var box = new VisualElement(); box.style.width = 1300;
                box.style.backgroundColor = new Color(0.05f, 0.06f, 0.1f, 0.97f); GameUI.Border(box, GameUI.CAmber, 3, 16);
                box.style.paddingLeft = box.style.paddingRight = 30; box.style.paddingTop = 18; box.style.paddingBottom = 18;
                box.Add(_ui.L(TT("Toolbox", "ตู้เครื่องมือ"), 36, true, GameUI.CAmber));
                box.Add(_ui.L(TT("Take what today's jobs need. You can carry 3 things.", "หยิบของที่งานวันนี้ต้องใช้ ถือได้ 3 ชิ้น"), 21, false, GameUI.CDim));
                var grid = new VisualElement(); grid.style.flexDirection = FlexDirection.Row; grid.style.flexWrap = Wrap.Wrap; grid.style.marginTop = 10;
                foreach (var id in ToolboxOrder)
                {
                    int n = StoreCount(id);
                    if (n <= 0 && IsSimItem(id)) continue;
                    grid.Add(ItemTile(id, n > 0 ? "×" + n : TT("none left", "หมดแล้ว"), n > 0 && _bag.Count < BagSlots ? TT("Take", "หยิบ") : null, () => { BagTake(id); JobShowToolbox(); }));
                }
                box.Add(grid);
                box.Add(BagRow(true, JobShowToolbox));
                var close = _ui.B(TT("Done", "เสร็จแล้ว"), () => _ui.HideScreen(), 24, true); close.style.alignSelf = Align.FlexStart; close.style.marginLeft = 0; close.style.marginTop = 14;
                box.Add(close);
                root.Add(box);
            });
        }

        VisualElement ItemTile(string id, string count, string action, Action act)
        {
            var t = new VisualElement(); t.style.width = 236; t.style.marginRight = 10; t.style.marginTop = 10; t.style.alignItems = Align.Center;
            t.style.paddingTop = 8; t.style.paddingBottom = 10; t.style.paddingLeft = t.style.paddingRight = 8;
            t.style.backgroundColor = new Color(0.09f, 0.11f, 0.2f, 0.96f); GameUI.Border(t, GameUI.CCyanD, 2, 10);
            var ic = new Image { sprite = Art.JobItem(id), scaleMode = ScaleMode.ScaleToFit }; ic.style.width = 72; ic.style.height = 72; t.Add(ic);
            t.Add(_ui.L(ItemName(id), 20, true, GameUI.CText));
            var info = _ui.L(ItemInfo(id), 15, false, GameUI.CDim); info.style.whiteSpace = WhiteSpace.Normal; info.style.unityTextAlign = TextAnchor.MiddleCenter; t.Add(info);
            if (count != null) t.Add(_ui.L(count, 18, true, GameUI.CAmber));
            if (action != null) { var b = _ui.B(action, act, 18); b.style.marginTop = 4; t.Add(b); }
            return t;
        }

        VisualElement BagRow(bool canReturn, Action redraw)
        {
            var wrap = new VisualElement(); wrap.style.marginTop = 14;
            wrap.Add(_ui.L(TT("Your bag", "กระเป๋าของคุณ") + $"  {_bag.Count}/{BagSlots}", 22, true, GameUI.CCyan));
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row;
            for (int i = 0; i < BagSlots; i++)
            {
                if (i < _bag.Count)
                {
                    string id = _bag[i];
                    string action = canReturn ? TT("Put back", "เก็บคืน") : id == "med_kit" ? TT("Use", "ใช้") : null;
                    row.Add(ItemTile(id, null, action, () =>
                    {
                        if (canReturn) BagReturn(id);
                        else if (id == "med_kit") UseMedKit();
                        redraw();
                    }));
                }
                else
                {
                    var e = new VisualElement(); e.style.width = 236; e.style.height = 120; e.style.marginRight = 10; e.style.marginTop = 10;
                    e.style.backgroundColor = new Color(0.05f, 0.06f, 0.1f, 0.8f); GameUI.Border(e, new Color(0.25f, 0.28f, 0.36f), 2, 10);
                    e.style.alignItems = Align.Center; e.style.justifyContent = Justify.Center;
                    e.Add(_ui.L(TT("empty", "ว่าง"), 18, false, GameUI.CDim));
                    row.Add(e);
                }
            }
            wrap.Add(row);
            return wrap;
        }

        void UseMedKit()
        {
            if (_life.State.health >= LC.health) { _ui.Toast(TT("You feel fine. Keep it for later.", "ตอนนี้ยังสบายดี เก็บไว้ใช้ทีหลัง"), GameUI.CDim); return; }
            _life.State.items.Remove("med_kit");
            _bag.Remove("med_kit");
            _life.ApplyHealth(1);
            _sfx.Play("good");
            _ui.Toast(TT("Bandaged and rested. Health +1.", "ทำแผลและพักแล้ว สุขภาพ +1"), GameUI.CGreen);
        }

        bool _bagOpen;

        /// <summary>Tab: the bag, exact stock numbers, radiation dose, friends and the jobs still waiting.</summary>
        void JobShowBag()
        {
            _bagOpen = true;
            _ui.ShowScreen(root =>
            {
                var box = new VisualElement(); box.style.width = 1400;
                box.style.backgroundColor = new Color(0.04f, 0.05f, 0.1f, 0.97f); GameUI.Border(box, GameUI.CCyan, 3, 16);
                box.style.paddingLeft = box.style.paddingRight = 30; box.style.paddingTop = 18; box.style.paddingBottom = 18;
                var top = new VisualElement(); top.style.flexDirection = FlexDirection.Row; top.style.justifyContent = Justify.SpaceBetween;
                top.Add(_ui.L(TT("Bag and status", "กระเป๋าและสถานะ"), 34, true, GameUI.CCyan));
                top.Add(_ui.L(TT("Tab / Esc to close", "กด Tab หรือ Esc เพื่อปิด"), 18, false, GameUI.CDim));
                box.Add(top);
                box.Add(BagRow(false, JobShowBag));

                var cols = new VisualElement(); cols.style.flexDirection = FlexDirection.Row; cols.style.marginTop = 14;
                // exact numbers
                var left = new VisualElement(); left.style.width = 430; left.style.marginRight = 20;
                left.Add(_ui.L(TT("Supplies (exact)", "เสบียง (ตัวเลขจริง)"), 21, true, GameUI.CAmber));
                var st = _life.State;
                foreach (var kind in GaugeKinds)
                {
                    var r = new VisualElement(); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginTop = 4;
                    r.Add(Sticker(kind, 30));
                    r.Add(_ui.L("  " + _loc.T("life.res." + kind) + "  " + LbNum(StockOf(kind)), 21));
                    left.Add(r);
                }
                left.Add(_ui.L(TT("Tonight the base needs", "คืนนี้ฐานต้องใช้") + $": O2 {LbNum(_life.O2Tonight)} · " + _loc.T("life.res.power") + " " + LbNum(LC.lifeSupportPower), 18, false, GameUI.CDim));
                left.Add(_ui.L(TT("Health", "สุขภาพ") + $" {st.health}/{LC.health} · " + TT("Morale", "กำลังใจ") + $" {st.morale}", 20, true, GameUI.CText));
                cols.Add(left);
                // radiation
                var mid = new VisualElement(); mid.style.width = 430; mid.style.marginRight = 20;
                mid.Add(_ui.L(TT("Radiation dose", "รังสีสะสม"), 21, true, GameUI.CAmber));
                mid.Add(RadBar(380));
                mid.Add(_ui.L(RadLevelText(), 19, true, RadColor()));
                var rf = _ui.L(TT("Curiosity's RAD instrument measured about 0.67 mSv a day on the Mars surface: thin air and no global magnetic field let more radiation through than on Earth.",
                    "เครื่อง RAD ของยาน Curiosity วัดได้ราว 0.67 มิลลิซีเวิร์ตต่อวันบนพื้นดาวอังคาร เพราะบรรยากาศบางและไม่มีสนามแม่เหล็กหุ้มทั้งดาว รังสีจึงผ่านเข้ามามากกว่าบนโลก"), 16, false, GameUI.CDim);
                rf.style.whiteSpace = WhiteSpace.Normal; mid.Add(rf);
                cols.Add(mid);
                // friends and jobs
                var right = new VisualElement(); right.style.flexGrow = 1;
                right.Add(_ui.L(TT("Friends", "เพื่อนในฐาน"), 21, true, GameUI.CAmber));
                if (st.guests.Count == 0) right.Add(_ui.L(TT("Nobody else yet.", "ยังไม่มีใคร"), 18, false, GameUI.CDim));
                foreach (var g in st.guests) right.Add(BondRow(g));
                right.Add(_ui.L(TT("Waiting jobs", "งานที่รออยู่"), 21, true, GameUI.CAmber));
                var cj = CardJob;
                if (cj != null) right.Add(_ui.L("• " + TT(cj.en, cj.th) + " → " + _loc.T("base.place." + cj.spot), 18));
                if (_emType == "power") right.Add(_ui.L("• " + TT(Jobs["em_power"].en, Jobs["em_power"].th) + TT(" (needs a fuse)", " (ต้องใช้ฟิวส์)"), 18, true, GameUI.CRed));
                if (_emType == "leak") right.Add(_ui.L("• " + TT(Jobs["em_leak"].en, Jobs["em_leak"].th) + TT(" (needs tape)", " (ต้องใช้เทป)"), 18, true, GameUI.CRed));
                if (cj == null && _emType != "power" && _emType != "leak") right.Add(_ui.L(TT("Nothing broken right now.", "ตอนนี้ไม่มีอะไรเสีย"), 18, false, GameUI.CDim));
                cols.Add(right);
                box.Add(cols);
                root.Add(box);
            });
        }

        VisualElement BondRow(string g)
        {
            var r = new VisualElement(); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginTop = 2;
            r.Add(_ui.L(LbName(g) + "  ", 19, true, GameUI.CText));
            r.Add(BondHearts(g, 22));
            return r;
        }

        /// <summary>Friendship as five hearts: full = fond, empty = upset.</summary>
        VisualElement BondHearts(string g, float size)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore }; row.style.flexDirection = FlexDirection.Row;
            int f = Mathf.Clamp(Friend(g) + 2, 0, 5);
            for (int i = 0; i < 5; i++)
            {
                var h = new VisualElement { pickingMode = PickingMode.Ignore }; h.style.width = size; h.style.height = size * 0.9f; h.style.marginRight = 2;
                h.style.backgroundImage = new StyleBackground(Art.Heart());
                h.style.unityBackgroundImageTintColor = i < f ? Color.white : new Color(0.25f, 0.25f, 0.3f);
                row.Add(h);
            }
            return row;
        }

        /// <summary>Small strip of what you carry, bottom left of the screen.</summary>
        VisualElement JobBagStrip()
        {
            var s = new VisualElement { pickingMode = PickingMode.Ignore };
            s.style.position = Position.Absolute; s.style.left = 24; s.style.bottom = 24; s.style.flexDirection = FlexDirection.Row; s.style.alignItems = Align.Center;
            s.style.backgroundColor = GameUI.CPanel; GameUI.Border(s, GameUI.CCyanD, 1, 8);
            s.style.paddingLeft = s.style.paddingRight = 10; s.style.paddingTop = s.style.paddingBottom = 6;
            s.Add(_ui.L(TT("Bag [Tab]", "กระเป๋า [Tab]"), 17, true, GameUI.CDim));
            for (int i = 0; i < BagSlots; i++)
            {
                var slot = new VisualElement { pickingMode = PickingMode.Ignore }; slot.style.width = 46; slot.style.height = 46; slot.style.marginLeft = 8;
                slot.style.backgroundColor = new Color(0.04f, 0.05f, 0.09f); GameUI.Border(slot, new Color(0.25f, 0.3f, 0.4f), 1, 6);
                if (i < _bag.Count) { slot.style.backgroundImage = new StyleBackground(Art.JobItem(_bag[i])); }
                s.Add(slot);
            }
            return s;
        }
    }
}
