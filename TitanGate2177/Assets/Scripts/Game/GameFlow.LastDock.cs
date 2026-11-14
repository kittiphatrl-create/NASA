// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does a short turn-based station week (read the panel, pick a fix, see the cost first) teach real
//           life-support science to 7-12 year olds better than a real-time scramble?
// Date: 2026-11-15
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TitanGate.Core.LastDock;
using DockRoom = TitanGate.Core.LastDock.Room;
using Res = TitanGate.Core.LastDock.Res;
using Fault = TitanGate.Core.LastDock.Fault;
using Outcome = TitanGate.Core.LastDock.Outcome;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// LAST DOCK: after the 60-second scramble the lifeboat docks at an abandoned station. Seven days, three Work Units
    /// a day. Walking and reading panels are free; only actions cost time. The rules live in Core/LastDock
    /// (StationSimulation, IncidentDirector); this file is only the presentation: map mode 2, consoles, panels, HUD.
    /// </summary>
    public partial class GameFlow
    {
        /// <summary>The scramble leads into LAST DOCK instead of the Mars base.</summary>
        public static bool LastDockMode = true;

        StationSimulation _ld;
        Dictionary<Res, int> _ldKit;
        ScavengeInventory _ldInv;
        int _ldSeed, _ldLogMark;
        bool _ldEnded;
        VisualElement _ldHud;
        Label _ldHudTop, _ldHudAlert, _ldHudRoom;
        readonly List<(int day, Fault f, string room)> _ldIncidents = new List<(int, Fault, string)>();
        readonly Dictionary<DockRoom, SpriteRenderer> _ldShade = new Dictionary<DockRoom, SpriteRenderer>();
        Fault _ldSeenFault;

        // ------------------------------------------------------------------ words

        string LdRes(Res r) => r switch
        {
            Res.Food => TT("Food", "อาหาร"), Res.Oxygen => TT("Oxygen", "ออกซิเจน"),
            Res.Battery => TT("Battery", "แบตเตอรี่"), Res.Parts => TT("Parts", "อะไหล่"),
            Res.Solar => TT("Portable solar panel", "แผงโซลาร์พกพา"), Res.RadioKit => TT("Radio repair kit", "ชุดซ่อมวิทยุ"),
            Res.RadMeter => TT("Radiation meter", "เครื่องวัดรังสี"), Res.FrozenFood => TT("Frozen food box", "กล่องอาหารแช่แข็ง"),
            Res.Flashlight => TT("Flashlight", "ไฟฉาย"), _ => TT("Medicine", "ยารักษา"),
        };

        static Res LdResOfSupply(string id) => id switch
        {
            "sup_food" => Res.Food, "sup_o2" => Res.Oxygen, "sup_battery" => Res.Battery, "sup_solar" => Res.Solar, "sup_radiokit" => Res.RadioKit,
            "sup_radmeter" => Res.RadMeter, "sup_frozen" => Res.FrozenFood, "sup_flashlight" => Res.Flashlight, "sup_medkit" => Res.Medkit, _ => Res.Parts,
        };
        static string LdSupplyOf(Res r) => r == Res.Food ? "sup_food" : r == Res.Oxygen ? "sup_o2" : r == Res.Battery ? "sup_battery" : "sup_parts";
        string LdSupName(string id) => LdRes(LdResOfSupply(id));

        string LdRoomName(DockRoom r) => r switch
        {
            DockRoom.Core => TT("Core", "แกนกลาง"), DockRoom.LifeSupport => TT("Life Support", "ระบบช่วยชีวิต"),
            DockRoom.Thermal => TT("Thermal Control", "ควบคุมความร้อน"), DockRoom.Storage => TT("Storage", "ห้องเก็บของ"), _ => TT("Communications", "ห้องสื่อสาร"),
        };

        string LdLevel(string level) => level == "critical" ? TT("CRITICAL", "วิกฤต") : level == "high" ? TT("HIGH", "สูง") : TT("normal", "ปกติ");
        Color LdLevelColor(string level) => level == "critical" ? GameUI.CRed : level == "high" ? GameUI.CAmber : GameUI.CGreen;

        (string name, string effect) LdAction(string a)
        {
            switch (a)
            {
                case "send": return (TT("Send location", "ส่งพิกัด"), TT("Signal +1. Uses 1 battery. Makes heat if cooling is broken.", "สัญญาณ +1 ใช้แบต 1 ถ้าระบบระบายความร้อนเสียจะร้อนขึ้น"));
                case "search": return (TT("Search the lockers", "ค้นตู้เก็บของ"), TT($"Find one spare thing ({_ld.FindsLeft} left).", $"เจอของสำรอง 1 ชิ้น (เหลือ {_ld.FindsLeft} ครั้ง)"));
                case "repair_scrubber": return (TT("Repair the CO₂ scrubber", "ซ่อมเครื่องกรอง CO₂"), TT("Uses 1 part. CO₂ goes down again.", "ใช้อะไหล่ 1 CO₂ จะลดลงอีกครั้ง"));
                case "absorber": return (TT("Use a CO₂ absorber canister", "ใช้กระป๋องดูดซับ CO₂"), TT("CO₂ −3 now. The scrubber is still broken.", "CO₂ −3 ทันที แต่เครื่องกรองยังเสียอยู่"));
                case "shelter": return (TT("Rest in the core", "พักในแกนกลาง"), TT("Breathe calmly: CO₂ does not rise this step.", "หายใจช้า ๆ CO₂ ไม่เพิ่มในช่วงนี้"));
                case "low_activity": return (TT("Move slowly", "เคลื่อนไหวน้อย ๆ"), TT("Use less oxygen: a stopped O₂ generator costs no O₂ this step.", "ใช้ออกซิเจนน้อยลง ช่วงนี้ไม่เสีย O₂ จากเครื่องผลิตที่หยุด"));
                case "repair_thermal": return (TT("Repair the coolant loop", "ซ่อมท่อระบายความร้อน"), TT("Uses 1 part. Heat goes down.", "ใช้อะไหล่ 1 ความร้อนลดลง"));
                case "shed_load": return (TT("Switch off spare machines", "ปิดเครื่องที่ไม่จำเป็น"), TT("Heat −2 now and cools faster. No sending for the rest of today.", "ความร้อน −2 และเย็นเร็วขึ้น แต่วันนี้ส่งสัญญาณไม่ได้"));
                case "repair_leak": return (TT("Patch the hole", "อุดรูรั่ว"), TT("Uses 2 parts. The leak stops and closed rooms open.", "ใช้อะไหล่ 2 รอยรั่วหยุด ห้องที่ปิดไว้กลับมาใช้ได้"));
                case "seal_room": return (TT("Close the hatch", "ปิดประตูกั้นห้อง"), TT("The leak stops, but the room is closed until patched.", "รอยรั่วหยุด แต่ห้องนั้นใช้ไม่ได้จนกว่าจะอุด"));
                case "detach_storage": return (TT("Detach the storage module", "ปลดโมดูลห้องเก็บของ"), TT("The leak stops for good. Storage is lost forever.", "รอยรั่วหยุดถาวร แต่เสียห้องเก็บของไปตลอด"));
                case "repair_fan": return (TT("Repair the air fan", "ซ่อมพัดลมหมุนเวียนอากาศ"), TT("Uses 1 part. Air moves again.", "ใช้อะไหล่ 1 อากาศหมุนเวียนอีกครั้ง"));
                case "backup_fan": return (TT("Switch on the backup fan", "เปิดพัดลมสำรอง"), TT("Air moves again. Costs 1 more battery every night.", "อากาศหมุนเวียนได้ แต่ใช้แบตเพิ่ม 1 ทุกคืน"));
                case "repair_o2gen": return (TT("Repair the O₂ generator", "ซ่อมเครื่องผลิต O₂"), TT("Uses 1 part. Oxygen stops dropping.", "ใช้อะไหล่ 1 ออกซิเจนหยุดลด"));
                case "cannibalize": return (TT("Take a part from the transmitter", "ถอดอะไหล่จากเครื่องส่ง"), TT("Parts +1, but Signal −1.", "อะไหล่ +1 แต่สัญญาณ −1"));
                case "dip_battery": return (TT("Use the battery", "ใช้แบตเตอรี่"), TT("Battery −1. The signal goes out.", "แบต −1 สัญญาณส่งออกไปได้"));
                case "dip_shed": return (TT("Pause air and cooling for a moment", "หยุดระบบอากาศกับความเย็นชั่วครู่"), TT("The signal goes out. CO₂ +1 and Heat +1.", "สัญญาณส่งออกได้ CO₂ +1 และความร้อน +1"));
                case "dip_cancel": return (TT("Cancel the send", "ยกเลิกการส่ง"), TT("Battery comes back. No signal this time.", "ได้แบตคืน แต่ครั้งนี้ไม่มีสัญญาณ"));
                case "repair_radio": return (TT("Work on the radio (quest 1)", "ซ่อมวิทยุ (เควส 1)"), _ld.HasRadioKit ? TT("With the repair kit it is fixed in one go.", "มีชุดซ่อมวิทยุ ซ่อมเสร็จในครั้งเดียว")
                    : TT($"One session a day; {_ld.C.radioRepairDays} sessions needed ({_ld.RadioRepair} done). The first one uses 1 part.", $"ซ่อมได้วันละครั้ง ต้องซ่อม {_ld.C.radioRepairDays} ครั้ง (ทำแล้ว {_ld.RadioRepair}) ครั้งแรกใช้อะไหล่ 1"));
                case "explore": return (TT("Go outside: search the crash site", "ออกไปข้างนอก: ค้นจุดที่ยานตก"), _ld.HasRadMeter ? TT("Once a day. Find one supply. Your radiation meter shows the hot spots, so no radiation.", "วันละครั้ง เจอของ 1 อย่าง มีเครื่องวัดรังสีจึงเลี่ยงจุดรังสีได้")
                    : TT($"Once a day. Find one supply — or walk into a radiation hot spot ({Mathf.RoundToInt(_ld.C.exploreRadiation * 100)}% chance: crew −1).", $"วันละครั้ง เจอของ 1 อย่าง หรือเดินเข้าจุดรังสีสูง (โอกาส {Mathf.RoundToInt(_ld.C.exploreRadiation * 100)}%: ลูกเรือ −1)"));
                case "use_medkit": return (TT("Use medicine", "ใช้ยารักษา"), TT($"Crew health +1. Medicine left: {_ld.Medkits}.", $"สุขภาพลูกเรือ +1 ยาเหลือ {_ld.Medkits}"));
                case "explore_0": case "explore_1": case "explore_2":
                {
                    int i = a[8] - '0';
                    return (TT("Search: ", "ค้น: ") + LdSiteName(i), TT($"May find: {LdSiteFinds(i)}", $"อาจเจอ: {LdSiteFinds(i)}"));
                }
                case "prepare_shelter": return (TT("Prepare the storm shelter", "เตรียมห้องหลบรังสี"), TT("Tonight the shelter uses 1 battery and keeps the radiation out.", "คืนนี้ห้องหลบรังสีใช้แบต 1 และกันรังสีไว้"));
                case "open_mystery": return (TT("Open an unlabelled food box", "เปิดกล่องอาหารไม่มีฉลาก"), TT($"Usually 1–2 meals, but it can be spoiled (upset stomach: −1 work unit tomorrow). Boxes: {_ld.MysteryFood}.", $"ส่วนใหญ่ได้ 1–2 มื้อ แต่อาจเสีย (ท้องเสีย: พรุ่งนี้เวลาทำงาน −1) เหลือ {_ld.MysteryFood} กล่อง"));
                case "assemble_rover": return (TT("Assemble the rover", "ประกอบรถสำรวจ"), TT("Uses 1 part. The rover can then search far away once a day.", "ใช้อะไหล่ 1 แล้วรถออกไปหาของไกล ๆ ได้วันละครั้ง"));
                case "rover_solar": return (TT("Move the solar panel onto the rover", "ย้ายแผงโซลาร์ไปติดรถ"), TT("Trips cost no base power, but the base loses its +1 power a day.", "รถออกไปไม่กินไฟฐาน แต่ฐานไม่ได้ไฟ +1 ต่อวันอีก"));
                case "send_rover": return (TT("Send the rover out", "ส่งรถออกไปสำรวจ"), (_ld.RoverSolar ? TT("Solar rover: no charge needed. ", "รถโซลาร์: ไม่ต้องชาร์จ ") : TT($"Charge: {_ld.C.roverCharge} battery. ", $"ชาร์จ: แบต {_ld.C.roverCharge} ")) + TT("Brings back about 3 supplies. It may break down, or strangers may follow it home.", "ได้ของกลับมาราว 3 อย่าง อาจเสียกลางทาง หรือมีคนแปลกหน้าตามกลับมา"));
                case "repair_rover": return (TT("Repair the rover", "ซ่อมรถสำรวจ"), TT("Uses 1 part.", "ใช้อะไหล่ 1"));
                case "raid_food": return (TT("Give them 2 food", "ให้อาหาร 2"), TT("They leave in peace.", "พวกเขาจากไปอย่างสงบ"));
                case "raid_battery": return (TT("Give them 2 battery", "ให้แบต 2"), TT("They leave in peace.", "พวกเขาจากไปอย่างสงบ"));
                case "raid_refuse": return (TT("Say no", "ปฏิเสธ"), _ld.HasFlashlight ? TT("They will come back tonight — your flashlight will scare them off.", "คืนนี้พวกเขาจะกลับมา แต่ไฟฉายจะไล่ไปได้") : TT("They will come back tonight and take 2 food.", "คืนนี้พวกเขาจะกลับมาเอาอาหาร 2"));
                case "dip_flashlight": return (TT("Work by flashlight", "ใช้ไฟฉายทำงานต่อ"), TT("Turn the cabin lights off and send by flashlight: no extra cost.", "ปิดไฟห้องแล้วใช้ไฟฉายส่งต่อ: ไม่เสียอะไรเพิ่ม"));
                case "end_day": return (TT("Dinner and sleep", "กินข้าวแล้วนอน"), TT("Pass the rest of today. Broken things keep getting worse.", "ข้ามเวลาที่เหลือของวัน ของที่เสียจะแย่ลงต่อ"));
            }
            return (a, "");
        }

        struct LdIncident { public string title, symptom, diagnosis, ignore, fact; public DockRoom room; public string[] actions; }

        LdIncident LdIncidentOf(Fault f)
        {
            switch (f)
            {
                case Fault.Scrubber: return new LdIncident { room = DockRoom.LifeSupport, actions = new[] { "repair_scrubber", "absorber", "shelter", "cannibalize" },
                    title = TT("CO₂ is rising", "CO₂ กำลังสูงขึ้น"),
                    symptom = TT("Headache and sleepy. The CO₂ meter climbs, but the O₂ meter is fine.", "ปวดหัว ง่วง มาตรวัด CO₂ สูงขึ้น แต่ O₂ ยังปกติ"),
                    diagnosis = TT("The CO₂ scrubber stopped. We have oxygen; the gas we breathe OUT is not being removed.", "เครื่องกรอง CO₂ หยุด ออกซิเจนยังมี แต่ก๊าซที่เราหายใจออกไม่ถูกกรองออก"),
                    ignore = TT("CO₂ rises every action. At CRITICAL the crew gets hurt.", "CO₂ เพิ่มทุกการกระทำ ถึงขั้นวิกฤตลูกเรือจะบาดเจ็บ"),
                    fact = TT("The ISS removes CO₂ from the air with molecular sieves. Earlier spacecraft like Apollo soaked it up with lithium hydroxide canisters.", "ISS กำจัด CO₂ ออกจากอากาศด้วยตะแกรงโมเลกุล ส่วนยานรุ่นก่อนอย่างอะพอลโลใช้กระป๋องลิเทียมไฮดรอกไซด์ดูดซับไว้") };
                case Fault.Thermal: return new LdIncident { room = DockRoom.Thermal, actions = new[] { "repair_thermal", "shed_load", "cannibalize" },
                    title = TT("Equipment is overheating", "อุปกรณ์ร้อนเกินไป"),
                    symptom = TT("Hot air and a warning light. When HEAT is high the transmitter switches itself off.", "อากาศร้อน ไฟเตือนติด ถ้าความร้อนสูง เครื่องส่งจะตัดตัวเอง"),
                    diagnosis = TT("The coolant loop that carries heat to the radiators stopped.", "ท่อน้ำยาที่พาความร้อนไปยังแผงระบายความร้อนหยุดทำงาน"),
                    ignore = TT("Heat rises every action (faster while sending). Too hot drains the batteries.", "ความร้อนเพิ่มทุกการกระทำ (เร็วขึ้นตอนส่งสัญญาณ) ร้อนมากจะดูดแบตหมด"),
                    fact = TT("The ISS pumps liquid ammonia through loops to big radiators outside, which radiate the heat away into space.", "ISS สูบแอมโมเนียเหลวผ่านท่อไปยังแผงระบายความร้อนขนาดใหญ่ด้านนอก ซึ่งแผ่ความร้อนออกสู่อวกาศ") };
                case Fault.Leak:
                {
                    var room = _ld.LeakRoom ?? DockRoom.Storage;
                    var acts = room == DockRoom.Storage ? new[] { "repair_leak", "seal_room", "detach_storage" } : new[] { "repair_leak", "seal_room" };
                    return new LdIncident { room = room, actions = acts,
                        title = TT($"Air leak in {LdRoomName(room)}", $"อากาศรั่วใน{LdRoomName(room)}"),
                        symptom = TT($"A hiss, and the pressure in {LdRoomName(room)} is dropping.", $"มีเสียงฟู่ และความดันใน{LdRoomName(room)}กำลังลดลง"),
                        diagnosis = TT("A tiny hole. Air escapes into space and takes our oxygen with it.", "มีรูเล็ก ๆ อากาศรั่วออกสู่อวกาศ พาออกซิเจนของเราไปด้วย"),
                        ignore = TT("Oxygen drops every action. When it is empty the crew gets hurt.", "ออกซิเจนลดทุกการกระทำ ถ้าหมดลูกเรือจะบาดเจ็บ"),
                        fact = TT("In 2018 the ISS crew traced a leak to a hole about 2 mm wide in a docked Soyuz and plugged it with epoxy on gauze.", "ปี 2018 ลูกเรือ ISS ตามหารอยรั่วจนพบรูกว้างราว 2 มม. ในยานโซยุซที่เทียบท่าอยู่ แล้วอุดด้วยกาวอีพ็อกซีบนผ้าก๊อซ") };
                }
                case Fault.Fan: return new LdIncident { room = DockRoom.LifeSupport, actions = new[] { "repair_fan", "backup_fan", "shelter" },
                    title = TT("The air is not moving", "อากาศไม่หมุนเวียน"),
                    symptom = TT("The air feels still. CO₂ rises near the crew even though the scrubber runs.", "อากาศนิ่ง CO₂ รอบตัวลูกเรือสูงขึ้น ทั้งที่เครื่องกรองยังทำงาน"),
                    diagnosis = TT("The air circulation fan stopped. In microgravity air does not move by itself.", "พัดลมหมุนเวียนอากาศหยุด ในสภาพไร้น้ำหนัก อากาศไม่เคลื่อนที่เอง"),
                    ignore = TT("CO₂ rises every action. At CRITICAL the crew gets hurt.", "CO₂ เพิ่มทุกการกระทำ ถึงขั้นวิกฤตลูกเรือจะบาดเจ็บ"),
                    fact = TT("Air hardly mixes by itself in microgravity, so pockets of breathed-out CO₂ can form. ISS sleep stations aim a fan at the astronaut's face.", "ในสภาพไร้น้ำหนัก อากาศแทบไม่ผสมกันเอง จึงอาจมี CO₂ ที่หายใจออกค้างเป็นกลุ่ม ห้องนอนบน ISS จึงมีพัดลมเป่าที่หน้านักบินอวกาศ") };
                case Fault.O2Gen: return new LdIncident { room = DockRoom.LifeSupport, actions = new[] { "repair_o2gen", "low_activity", "cannibalize" },
                    title = TT("The oxygen generator stopped", "เครื่องผลิตออกซิเจนหยุด"),
                    symptom = TT("The O₂ meter drops every action. CO₂ is normal.", "มาตรวัด O₂ ลดทุกการกระทำ แต่ CO₂ ปกติ"),
                    diagnosis = TT("The machine that splits the station's water into oxygen stopped. The scrubber still works.", "เครื่องที่แยกน้ำของสถานีเป็นออกซิเจนหยุด ส่วนเครื่องกรอง CO₂ ยังทำงาน"),
                    ignore = TT("Oxygen tanks drop every action. When empty the crew gets hurt.", "ถังออกซิเจนลดทุกการกระทำ ถ้าหมดลูกเรือจะบาดเจ็บ"),
                    fact = TT("The ISS makes oxygen by electrolysis: electricity splits water (H₂O) into oxygen and hydrogen.", "ISS ผลิตออกซิเจนด้วยการแยกน้ำด้วยไฟฟ้า (อิเล็กโทรลิซิส) แยก H₂O เป็นออกซิเจนกับไฮโดรเจน") };
                default: return new LdIncident { room = DockRoom.Comms, actions = new[] { "dip_battery", "dip_shed", "dip_cancel" },
                    title = TT("Power dip while sending!", "ไฟตกระหว่างส่งสัญญาณ!"),
                    symptom = TT("The lights flicker as the transmitter pulls power.", "ไฟกะพริบตอนเครื่องส่งดึงไฟ"),
                    diagnosis = TT("The transmitter needs more power than the line can give right now.", "เครื่องส่งต้องการไฟมากกว่าที่สายส่งให้ได้ตอนนี้"),
                    ignore = TT("Choose now: the send is waiting.", "ต้องเลือกตอนนี้ การส่งรออยู่"),
                    fact = TT("The ISS circles Earth every 90 minutes and spends about 30 of them in shadow; batteries charged by the solar arrays keep it running.", "ISS โคจรรอบโลกทุก 90 นาที และอยู่ในเงาโลกราว 30 นาที แบตเตอรี่ที่ชาร์จจากแผงโซลาร์เซลล์จ่ายไฟให้ตลอด") };
            }
        }

        // ------------------------------------------------------------------ start

        /// <summary>Begin the station week with what the lifeboat delivered.</summary>
        void StartLastDock(Dictionary<Res, int> kit, int seed)
        {
            _ldKit = kit ?? new Dictionary<Res, int> { [Res.Food] = 2, [Res.Oxygen] = 2, [Res.Battery] = 2, [Res.Parts] = 2 };
            _ldSeed = seed;
            _ld = new StationSimulation(new StationConfig(), _ldKit, seed);
            _ldEnded = false; _ldLogMark = 0; _ldIncidents.Clear(); _ldSeenFault = Fault.None;
            _chapter = 3;
            _busy = false;
            if (!_playFast) Time.timeScale = 1f;
            _ui.HideScreen();
            BBegin(2, new Vector2(12f, 11.5f));
            LdBuildHud();
            _ui.FadeTo(0f, 0.6f);
            LdIntro();
        }

        void LdIntro()
        {
            var c = new StationConfig();
            _ui.ShowModal(p =>
            {
                LdHeader(p, "LAST DOCK");
                p.Add(LdPara(TT("The lifeboat landed on Mars, docked to an old, empty base. A rescue ship can reach us in 7 days.",
                    "ยานชูชีพลงจอดบนดาวอังคาร เทียบกับฐานเก่าที่ไม่มีใคร ยานกู้ภัยจะมาถึงใน 7 วัน")));
                p.Add(LdPara(TT($"To be picked up: ① send our location {c.signalNeeded} times from Communications, ② keep {c.dockPower} battery to open the docking port on day 7, ③ keep the crew safe.",
                    $"เพื่อให้ยานกู้ภัยรับเราได้: ① ส่งพิกัด {c.signalNeeded} ครั้งจากห้องสื่อสาร ② เก็บแบตไว้ {c.dockPower} เพื่อเปิดช่องเทียบท่าในวันที่ 7 ③ ดูแลลูกเรือให้ปลอดภัย"), GameUI.CAmber));
                p.Add(LdPara(TT($"Each day has {c.workUnits} work units. Every action costs 1. Walking and reading panels are free — take your time.",
                    $"แต่ละวันมีเวลาทำงาน {c.workUnits} ช่วง ทุกการกระทำใช้ 1 ช่วง เดินและอ่านแผงฟรี ไม่ต้องรีบ")));
                var kit = string.Join("   ", new[] { Res.Food, Res.Oxygen, Res.Battery, Res.Parts }.Select(r => LdRes(r) + " +" + (_ldKit.TryGetValue(r, out var n) ? n : 0)));
                p.Add(LdPara(TT("From the lifeboat: ", "ขนมาจากยานชูชีพ: ") + kit, GameUI.CGreen));
                var extras = new[] { Res.Solar, Res.RadioKit, Res.RadMeter, Res.FrozenFood, Res.Flashlight, Res.Medkit }.Where(r => _ldKit.TryGetValue(r, out var n) && n > 0).Select(LdRes).ToList();
                p.Add(LdPara(extras.Count == 0 ? TT("No special items came along.", "ไม่มีของพิเศษติดมา") : TT("Special items: ", "ของพิเศษ: ") + string.Join(", ", extras), extras.Count == 0 ? GameUI.CDim : GameUI.CAmber));
                p.Add(LdPara(TT("Quest 1: repair the radio in Communications (one session a day). Then send our location.", "เควส 1: ซ่อมวิทยุที่ห้องสื่อสาร (ซ่อมได้วันละครั้ง) แล้วค่อยส่งพิกัด"), GameUI.CCyan));
                p.Add(LdPara(TT($"Already in the base: Food {c.baseFood}, Oxygen {c.baseOxygen}, Battery {c.baseBattery}, Parts {c.baseParts}, 1 CO₂ absorber.",
                    $"ของที่ฐานมีอยู่แล้ว: อาหาร {c.baseFood} ออกซิเจน {c.baseOxygen} แบต {c.baseBattery} อะไหล่ {c.baseParts} กระป๋องดูดซับ CO₂ 1"), GameUI.CDim));
                p.Add(LdPara(TT("Walk with WASD, press E at a console. Things will break: read the panel, then pick a fix.",
                    "เดินด้วย WASD กด E ที่คอนโซล ของจะเสีย: อ่านแผงก่อน แล้วค่อยเลือกวิธีแก้"), GameUI.CCyan));
            });
        }

        Label LdPara(string text, Color? c = null, int size = 24)
        {
            var l = _ui.L(text, size, false, c ?? GameUI.CText);
            l.style.whiteSpace = WhiteSpace.Normal; l.style.marginBottom = 8; l.style.maxWidth = 1080;
            return l;
        }

        // ------------------------------------------------------------------ map (mode 2)

        void LdLayout()
        {
            Art.UseStationTiles = true;   // Blender walls and floors for this map
            _bW = 56; _bH = 26;   // the base on the left, Mars outside on the right
            _bKind = new int[_bW, _bH];
            for (int x = 0; x < _bW; x++) for (int y = 0; y < _bH; y++) _bKind[x, y] = KGround;   // the base sits on Mars
            LRoom(2, 9, 8, 15, 6, "ld.dock");
            LRoom(10, 11, 14, 12, 3, "ld.tube");
            LRoom(16, 7, 28, 16, 0, "ld.core");
            LRoom(16, 18, 25, 23, 1, "ld.life");
            LRoom(30, 9, 36, 15, 5, "ld.thermal");
            LRoom(16, 1, 21, 5, 0, "ld.storage");
            LRoom(23, 1, 28, 5, 4, "ld.comms");
            foreach (int y in new[] { 11, 12 }) { LDoor(9, y, 6); LDoor(15, y); LDoor(29, y, 5); }
            foreach (int x in new[] { 20, 21 }) LDoor(x, 17, 1);
            foreach (int x in new[] { 18, 19 }) LDoor(x, 6, 0);
            foreach (int x in new[] { 25, 26 }) LDoor(x, 6, 4);
            LdLayoutOutside();   // airlock and the Mars surface (GameFlow.MarsOutside.cs)
            _bHatch = new Vector2(3, 3);   // unused here (the airlock has its own sliding door), kept off the map's rooms
        }

        /// <summary>Wall sign for each station module (only letters the sign font has).</summary>
        static void LdSign(string key, ref string code, ref Color32 color)
        {
            switch (key)
            {
                case "ld.core": code = "CORE"; color = Art.HabitatAmber; break;
                case "ld.dock": code = "PORT"; color = Art.HabitatAmber; break;
                case "ld.life": code = "AIR"; color = Art.HabitatCyan; break;
                case "ld.thermal": code = "HEAT"; color = PixelCanvas.Hex("#e58a4a"); break;
                case "ld.storage": code = "STORE"; break;
                case "ld.comms": code = "COMMS"; color = Art.HabitatCyan; break;
            }
        }

        static DockRoom? LdRoomOfKey(string key) => key switch
        {
            "ld.dock" => DockRoom.Core, "ld.tube" => DockRoom.Core, "ld.core" => DockRoom.Core, "ld.life" => DockRoom.LifeSupport,
            "ld.thermal" => DockRoom.Thermal, "ld.storage" => DockRoom.Storage, "ld.comms" => DockRoom.Comms, _ => (DockRoom?)null,
        };

        static readonly Dictionary<DockRoom, (Vector2Int a, Vector2Int b)[]> LdDoors = new Dictionary<DockRoom, (Vector2Int, Vector2Int)[]>
        {
            [DockRoom.LifeSupport] = new[] { (new Vector2Int(20, 17), new Vector2Int(20, 15)), (new Vector2Int(21, 17), new Vector2Int(21, 15)) },
            [DockRoom.Thermal] = new[] { (new Vector2Int(29, 11), new Vector2Int(27, 11)), (new Vector2Int(29, 12), new Vector2Int(27, 12)) },
            [DockRoom.Storage] = new[] { (new Vector2Int(18, 6), new Vector2Int(18, 8)), (new Vector2Int(19, 6), new Vector2Int(19, 8)) },
            [DockRoom.Comms] = new[] { (new Vector2Int(25, 6), new Vector2Int(25, 8)), (new Vector2Int(26, 6), new Vector2Int(26, 8)) },
        };

        /// <summary>A doorway tile of a closed (sealed or detached) module: its sliding door stays shut with a red light.</summary>
        bool LdDoorTileLocked(Vector2Int t)
        {
            foreach (var kv in LdDoors)
                if (!_ld.Usable(kv.Key))
                    foreach (var (door, _) in kv.Value) if (door == t) return true;
            return false;
        }

        static readonly Dictionary<DockRoom, RectInt> LdRects = new Dictionary<DockRoom, RectInt>
        {
            [DockRoom.LifeSupport] = new RectInt(16, 18, 10, 6), [DockRoom.Thermal] = new RectInt(30, 9, 7, 7),
            [DockRoom.Storage] = new RectInt(16, 1, 6, 5), [DockRoom.Comms] = new RectInt(23, 1, 6, 5),
        };

        static Sprite _ldPixel;
        static Sprite LdPixel()
        {
            if (_ldPixel == null)
            {
                var t = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                t.SetPixel(0, 0, Color.white); t.Apply();
                _ldPixel = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0, 0), 1f);
            }
            return _ldPixel;
        }

        void LdBuildProps()
        {
            // ---- Docking port (x 2-8, y 9-15): the lifeboat we came in, and the port console
            BDeco(Art.LifeboatPod(false, 0, ScavengeInventory.PodMax), 5.0f, 12.4f, 4);
            BProp("ld_dock", TT("Docking port and airlock", "ช่องเทียบท่าและประตูออกข้างนอก"), Art.Station("dock", false) ?? Art.Console("lddock", 22, 20, Pal.Amber), 5.0f, 9.2f, new Vector2(5.0f, 10.2f), 1, 1);
            BLamp(new Vector2(5.5f, 12f), new Color(1f, 0.8f, 0.5f), 6f, 0.5f);
            BLamp(new Vector2(12.5f, 12f), new Color(0.7f, 0.85f, 1f), 3f, 0.35f);

            // ---- Core (x 16-28, y 7-16): station board, bunks for the night, seats
            // the board sits on the wall between the Life Support and airlock doors (it used to cover a door frame)
            BProp("ld_board", TT("Station board", "แผงสถานะสถานี"), Art.Station("board", false) ?? Art.MissionBoard(false), 24.5f, 17.05f, new Vector2(24.5f, 16.3f), order: SortY(17) + 1);
            // the crew table: dinner ends the day; the two chairs face each other across it
            BProp("ld_table", TT("Dinner table (end the day)", "โต๊ะกินข้าว (จบวัน)"), Art.Table(), 22.0f, 11.2f, new Vector2(22.0f, 10.2f), 3);
            BProp("deco", null, Art.PilotSeat(), 19.6f, 11.0f, Vector2.zero, 1);
            BProp("deco", null, Art.PilotSeat(), 24.4f, 11.0f, Vector2.zero, 1).sr.flipX = true;
            BLamp(new Vector2(22f, 12f), new Color(1f, 0.85f, 0.6f), 8f, 0.5f);

            // ---- Life Support (x 16-25, y 18-23): scrubber, O₂ generator, fan
            BProp("ld_scrubber", TT("CO₂ scrubber", "เครื่องกรอง CO₂"), Art.Station("scrubber", false) ?? Art.Recycler(), 17.6f, 23.12f, new Vector2(17.8f, 22.2f), 2, 1);
            BProp("ld_o2gen", TT("O₂ generator", "เครื่องผลิต O₂"), Art.Station("o2gen", false) ?? Art.Moxie(true), 21.0f, 23.12f, new Vector2(21.0f, 22.2f), 2, 1);
            BProp("ld_fan", TT("Air fan", "พัดลมหมุนเวียนอากาศ"), Art.Station("fan", false) ?? Art.Console("ldfan", 22, 22, Pal.Cyan), 24.4f, 23.12f, new Vector2(24.2f, 22.2f), 1, 1);
            BLamp(new Vector2(21f, 21f), new Color(0.6f, 1f, 0.7f), 6f, 0.45f);

            // ---- Thermal control (x 30-36, y 9-15): coolant pump console and tanks
            BProp("ld_thermal", TT("Coolant loop", "ระบบท่อระบายความร้อน"), Art.Station("thermal", false) ?? Art.Console("ldthermal", 26, 30, Pal.Amber), 33.0f, 15.12f, new Vector2(33.0f, 14.3f), 2, 1);
            BDeco(Art.FuelTank(0.7f, false), 35.6f, 9.4f, 1);
            BDeco(Art.FuelTank(0.5f, false), 31.0f, 9.4f, 1);
            BLamp(new Vector2(33f, 12f), new Color(1f, 0.6f, 0.35f), 5f, 0.45f);

            // ---- Storage (x 16-21, y 1-5)
            BProp("ld_lockers", TT("Spare lockers", "ตู้เก็บของสำรอง"), Art.Station("lockers", false) ?? Art.Shelf(true), 17.4f, 5.12f, new Vector2(17.6f, 4.3f), 2, 1);
            BDeco(Art.Locker(false), 21.1f, 5.12f, 1);
            BDeco(Art.Crates(), 20.0f, 1.4f, 2);
            BLamp(new Vector2(19f, 3f), new Color(0.75f, 0.85f, 1f), 5f, 0.4f);

            // ---- Communications (x 23-28, y 1-5)
            BProp("ld_radio", TT("Transmitter", "เครื่องส่งสัญญาณ"), Art.Station("radio", false) ?? Art.Console("ldradio", 26, 30, Pal.Cyan), 27.4f, 5.12f, new Vector2(27.2f, 4.3f), 2, 1);
            BDeco(Art.NavComputer(true), 24.0f, 5.12f, 1);
            BLamp(new Vector2(25.5f, 3f), new Color(0.4f, 0.9f, 1f), 5f, 0.45f);

            // a shade over each module, coloured by what is wrong there
            _ldShade.Clear();
            foreach (var kv in LdRects)
            {
                var sr = BSprite("ld_shade", LdPixel(), new Vector2(kv.Value.x, kv.Value.y - 1), 30000);
                sr.transform.localScale = new Vector3(kv.Value.width, kv.Value.height + 2, 1);
                sr.color = Color.clear;
                _ldShade[kv.Key] = sr;
            }
            LdBuildOutside();
        }

        DockRoom LdRoomOfSpot(string id) => id switch
        {
            "ld_scrubber" => DockRoom.LifeSupport, "ld_o2gen" => DockRoom.LifeSupport, "ld_fan" => DockRoom.LifeSupport,
            "ld_thermal" => DockRoom.Thermal, "ld_lockers" => DockRoom.Storage, "ld_radio" => DockRoom.Comms, _ => DockRoom.Core,
        };

        string LdSpotOfRoom(DockRoom r) => r switch
        {
            DockRoom.LifeSupport => _ld != null && !_ld.FanOk ? "ld_fan" : _ld != null && !_ld.O2GenOk ? "ld_o2gen" : "ld_scrubber",
            DockRoom.Thermal => "ld_thermal", DockRoom.Storage => "ld_lockers", DockRoom.Comms => "ld_radio", _ => "ld_board",
        };

        IEnumerable<(string id, bool urgent)> LdMarks()
        {
            if (_ld == null || _ld.Outcome != Outcome.Ongoing) yield break;
            var f = _ld.ActiveFault;
            if (f != Fault.None)
            {
                var inc = LdIncidentOf(f);
                if (_ld.Usable(inc.room)) yield return (LdSpotOfRoom(inc.room), true);
                yield return ("ld_board", true);
            }
            else if (_ld.Signal < _ld.C.signalNeeded && _ld.Can("send", out _)) yield return ("ld_radio", false);
        }

        // ------------------------------------------------------------------ per frame

        void LdUpdate(float dt)
        {
            if (_ld == null) return;
            // close hatches of sealed or detached modules; open them again when patched
            foreach (var kv in LdDoors)
            {
                bool shut = !_ld.Usable(kv.Key);
                foreach (var (door, _) in kv.Value) _bSolid[door.x, door.y] = shut;
            }
            float pulse = 0.5f + 0.5f * Mathf.Sin(_bAnim * (Settings.ReducedMotion ? 0 : 4));
            foreach (var kv in _ldShade)
            {
                Color c = Color.clear;
                if (_ld.Detached.Contains(kv.Key)) c = new Color(0.02f, 0.02f, 0.05f, 0.85f);
                else if (_ld.Sealed.Contains(kv.Key)) c = new Color(0.05f, 0.05f, 0.12f, 0.65f);
                else if (_ld.LeakRoom == kv.Key) c = new Color(0.9f, 0.1f, 0.1f, 0.12f + 0.12f * pulse);
                else if (kv.Key == DockRoom.Thermal && _ld.Heat > 0) c = new Color(1f, 0.45f, 0.1f, Mathf.Min(0.35f, 0.07f * _ld.Heat) * (0.7f + 0.3f * pulse));
                else if (kv.Key == DockRoom.LifeSupport && (!_ld.ScrubberOk || !_ld.FanOk || !_ld.O2GenOk)) c = new Color(1f, 0.75f, 0.1f, 0.1f + 0.08f * pulse);
                kv.Value.color = Color.Lerp(kv.Value.color, c, dt * 5f);
            }
            // Blender consoles show their state: fault light and red screen when broken
            foreach (var (spot, art, bad) in new[] {
                ("ld_scrubber", "scrubber", !_ld.ScrubberOk), ("ld_o2gen", "o2gen", !_ld.O2GenOk), ("ld_fan", "fan", !_ld.FanOk && !_ld.BackupCircuit),
                ("ld_thermal", "thermal", !_ld.ThermalOk || _ld.HeatLevel != "normal"), ("ld_radio", "radio", _ld.TransmitterTripped || !_ld.Usable(DockRoom.Comms)),
                ("ld_board", "board", _ld.ActiveFault != Fault.None), ("ld_dock", "dock", _ld.Battery < _ld.C.dockPower), ("ld_lockers", "lockers", _ld.FindsLeft == 0) })
            {
                var sp = BSpotById(spot); var sprite = Art.Station(art, bad);
                if (sp != null && sprite != null && sp.sr.sprite != sprite) sp.sr.sprite = sprite;
            }
            _sfx.Loop("siren", _ld.ActiveFault != Fault.None && _ld.Outcome == Outcome.Ongoing && !_ui.ModalOpen);
            // a new incident: alarm, flash, and it goes in the incident log
            var f = _ld.ActiveFault;
            if (f != _ldSeenFault)
            {
                if (f != Fault.None && f != Fault.PowerDip)
                {
                    var inc = LdIncidentOf(f);
                    _ldIncidents.Add((_ld.Day, f, LdRoomName(inc.room)));
                    _sfx.Play("alarm"); _ui.Flash(new Color(1f, 0.2f, 0.1f, 0.35f));
                    _ui.Toast("! " + inc.title + " — " + TT($"go to {LdRoomName(inc.room)} or the station board", $"ไปที่{LdRoomName(inc.room)} หรือแผงสถานะสถานี"), GameUI.CRed);
                }
                _ldSeenFault = f;
            }
            LdOutsideUpdate(dt);
        }

        // ------------------------------------------------------------------ HUD

        void LdBuildHud()
        {
            LdEndBase();
            _ldHud = new VisualElement { pickingMode = PickingMode.Ignore };
            _ldHud.style.position = Position.Absolute; _ldHud.style.left = 16; _ldHud.style.right = 16; _ldHud.style.top = 12;
            _ldHud.style.alignItems = Align.Center;
            _ldHudTop = _ui.L("", 24, true, GameUI.CText);
            _ldHudTop.pickingMode = PickingMode.Ignore;
            _ldHudTop.style.backgroundColor = new Color(0.04f, 0.05f, 0.09f, 0.88f);
            _ldHudTop.style.paddingLeft = _ldHudTop.style.paddingRight = 18; _ldHudTop.style.paddingTop = _ldHudTop.style.paddingBottom = 6;
            GameUI.Border(_ldHudTop, new Color(1f, .92f, .76f, .5f), 2, 10);
            _ldHudTop.enableRichText = true;
            _ldHud.Add(_ldHudTop);
            _ldHudTop.style.display = DisplayStyle.None;   // replaced by the chip row below
            _ldHud.Add(LdChipRow());
            _ldHudAlert = _ui.L("", 22, true, GameUI.CRed);
            _ldHudAlert.pickingMode = PickingMode.Ignore;
            _ldHudAlert.style.marginTop = 6; _ldHudAlert.style.backgroundColor = new Color(0.25f, 0.03f, 0.05f, 0.9f);
            _ldHudAlert.style.paddingLeft = _ldHudAlert.style.paddingRight = 14; _ldHudAlert.style.paddingTop = _ldHudAlert.style.paddingBottom = 4;
            _ldHud.Add(_ldHudAlert);
            _ldHudRoom = _ui.L("", 20, false, GameUI.CDim);
            _ldHudRoom.pickingMode = PickingMode.Ignore;
            _ldHudRoom.style.marginTop = 4;
            _ldHud.Add(_ldHudRoom);
            _ui.Root.Add(_ldHud);
        }

        /// <summary>Remove the station HUD (called when the top-down map is torn down).</summary>
        void LdEndBase()
        {
            if (_ldHud != null) { _ldHud.RemoveFromHierarchy(); _ldHud = null; }
            if (_bMode == 2) _sfx.Loop("siren", false);
            Art.UseStationTiles = false;
            LdPolishReset();
        }

        static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        void LdHud()
        {
            if (_bHud != null) _bHud.style.display = DisplayStyle.None;
            if (_bHudPortrait != null) _bHudPortrait.style.display = DisplayStyle.None;
            if (_ldHud == null || _ld == null) return;
            _ldHud.style.display = _ui.ScreenOpen ? DisplayStyle.None : DisplayStyle.Flex;
            var s = _ld;
            string wu = new string('●', Mathf.Max(0, s.WorkUnits)) + new string('○', Mathf.Max(0, s.C.workUnits - s.WorkUnits));
            string Num(string name, int v, int warn) => $"{name} <color={Hex(v <= warn ? GameUI.CRed : GameUI.CText)}>{v}</color>";
            string hearts = new string('♥', Mathf.Max(0, s.Health)) + new string('·', Mathf.Max(0, s.C.health - s.Health));
            _ldHudTop.text = string.Join("   ", new[]
            {
                $"<color={Hex(GameUI.CAmber)}>{TT("DAY", "วันที่")} {s.Day}/{s.C.days}</color>",
                TT("Time", "เวลา") + $" <color={Hex(GameUI.CCyan)}>{wu}</color>",
                Num(LdRes(Res.Food), s.Food, 1), Num("O₂", s.Oxygen, 0), Num(LdRes(Res.Battery), s.Battery, s.C.dockPower), Num(LdRes(Res.Parts), s.Parts, -1),
                $"CO₂ <color={Hex(LdLevelColor(s.Co2Level))}>{LdLevel(s.Co2Level)}</color>",
                $"{TT("Heat", "ความร้อน")} <color={Hex(LdLevelColor(s.HeatLevel))}>{LdLevel(s.HeatLevel)}</color>",
                $"{TT("Signal", "สัญญาณ")} <color={Hex(s.Signal >= s.C.signalNeeded ? GameUI.CGreen : GameUI.CCyan)}>{s.Signal}/{s.C.signalNeeded}</color>",
                $"<color={Hex(s.Health <= 1 ? GameUI.CRed : GameUI.CGreen)}>{hearts}</color>",
                $"<color={Hex(GameUI.CAmber)}>{TT("Rescue in", "ยานกู้ภัยมาใน")} {s.C.days - s.Day + 1} {TT("days", "วัน")}</color>",
            });
            var f = s.ActiveFault;
            LdChip("day", $"{s.Day}/{s.C.days}", GameUI.CAmber);
            LdChip("time", wu, GameUI.CCyan);
            LdChip("food", s.Food.ToString() + (s.MysteryFood > 0 ? $" +{s.MysteryFood}?" : ""), s.Food <= 1 ? GameUI.CRed : GameUI.CText, s.Food == 0);
            LdChip("o2", s.Oxygen.ToString(), s.Oxygen <= 0 ? GameUI.CRed : GameUI.CText, s.Oxygen <= 0 && (s.LeakRoom != null || !s.O2GenOk));
            LdChip("battery", s.Battery.ToString(), s.Battery < s.C.dockPower ? GameUI.CRed : GameUI.CText, s.Battery <= 0);
            LdChip("parts", s.Parts.ToString(), s.Parts == 0 ? GameUI.CAmber : GameUI.CText);
            LdChip("co2", LdLevel(s.Co2Level), LdLevelColor(s.Co2Level), s.Co2Level == "critical");
            LdChip("heat", LdLevel(s.HeatLevel), LdLevelColor(s.HeatLevel), s.HeatLevel == "critical");
            LdChip("signal", s.RadioFixed ? $"{s.Signal}/{s.C.signalNeeded}" : TT($"radio {s.RadioRepair}/{s.C.radioRepairDays}", $"วิทยุ {s.RadioRepair}/{s.C.radioRepairDays}"), s.Signal >= s.C.signalNeeded ? GameUI.CGreen : s.RadioFixed ? GameUI.CCyan : GameUI.CAmber);
            LdChip("heart", hearts, s.Health <= 1 ? GameUI.CRed : GameUI.CGreen, s.Health <= 1);
            LdChip("rescue", TT($"{s.C.days - s.Day + 1} days", $"อีก {s.C.days - s.Day + 1} วัน"), GameUI.CAmber);
            var al = s.Outcome == Outcome.Ongoing ? LdAlert() : null;
            _ldHudAlert.style.display = al != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (al != null) { _ldHudAlert.text = al.Value.text; _ldHudAlert.style.color = al.Value.color; }
            var room = BRoomAt(_bPos);
            var r = room == null ? null : LdRoomOfKey(room);
            _ldHudRoom.text = (r == null ? "" : "▸ " + (room == "ld.dock" ? TT("Docking port", "ช่องเทียบท่า") : LdRoomName(r.Value)) + "   ") + TT("WASD walk · E use · reading is free", "WASD เดิน · E ใช้ · อ่านฟรี");
        }

        // ------------------------------------------------------------------ consoles

        void LdUse(BSpot s)
        {
            if (_ld == null || _ldEnded) return;
            _sfx.Play("console", 0.6f);
            if (_ld.PowerDipPending) { LdShowDip(); return; }
            if (_ld.RaiderPending) { LdShowRaid(); return; }
            if (s.id.StartsWith("site_") || s.id == "rover_pad") { LdShowOutsidePanel(s.id); return; }
            if (s.id == "ld_airlock") { LdShowAirlock(); return; }
            if (s.id == "ld_table") { LdShowPanel(DockRoom.Core, "ld_table"); return; }
            var room = LdRoomOfSpot(s.id);
            if (!_ld.Usable(room)) { _ui.Toast(TT("This module is closed off. Patch it from the station board.", "โมดูลนี้ถูกปิดอยู่ อุดรอยรั่วได้จากแผงสถานะสถานี"), GameUI.CDim); return; }
            LdShowPanel(room, s.id);
        }

        /// <summary>Which actions a console offers. Only actions that can apply now are shown (with the reason if blocked).</summary>
        IEnumerable<string> LdConsoleActions(DockRoom room, string spot)
        {
            if (spot == "ld_table") return new[] { "open_mystery", "end_day" }.Where(a => _ld.Can(a, out var w) || w != "not_needed");
            var f = _ld.ActiveFault;
            var list = new List<string>();
            if (f != Fault.None)
            {
                var inc = LdIncidentOf(f);
                if (spot == "ld_board" || spot == "ld_dock" || inc.room == room) list.AddRange(inc.actions);
            }
            switch (room)
            {
                case DockRoom.LifeSupport: list.AddRange(new[] { "repair_scrubber", "absorber", "repair_fan", "backup_fan", "repair_o2gen" }); break;
                case DockRoom.Thermal: list.AddRange(new[] { "repair_thermal", "shed_load" }); break;
                case DockRoom.Storage: list.Add("search"); break;
                case DockRoom.Comms: list.Add("repair_radio"); list.Add("send"); break;
                case DockRoom.Core:
                    if (_ld.Sealed.Count > 0) list.Add("repair_leak");
                    list.AddRange(new[] { "prepare_shelter", "open_mystery", "use_medkit", "shelter", "end_day" }); break;
            }
            return list.Distinct().Where(a => _ld.Can(a, out var why) || why != "not_needed" && why != "unknown" && why != "no_medkit" && !(a == "repair_radio" && why == "done"));
        }

        VisualElement LdStatusBlock(DockRoom room)
        {
            var box = new VisualElement(); box.style.marginBottom = 10;
            void Line(string label, bool ok, string okText = null, string badText = null)
                => box.Add(LdPara("• " + label + ": " + (ok ? okText ?? TT("working", "ทำงานปกติ") : badText ?? TT("BROKEN", "เสีย")), ok ? GameUI.CGreen : GameUI.CRed, 22));
            var s = _ld;
            switch (room)
            {
                case DockRoom.LifeSupport:
                    Line(TT("CO₂ scrubber", "เครื่องกรอง CO₂"), s.ScrubberOk);
                    Line(TT("Air fan", "พัดลมหมุนเวียน"), s.FanOk || s.BackupCircuit, s.BackupCircuit ? TT("backup circuit on (+1 battery a night)", "ใช้วงจรสำรอง (+แบต 1 ต่อคืน)") : null);
                    Line(TT("O₂ generator (splits station water)", "เครื่องผลิต O₂ (แยกน้ำของสถานี)"), s.O2GenOk);
                    box.Add(LdPara($"CO₂: {LdLevel(s.Co2Level)}   ·   O₂ {TT("tanks", "ถัง")}: {s.Oxygen}   ·   {TT("absorber canisters", "กระป๋องดูดซับ")}: {s.Absorbers}", GameUI.CText, 22));
                    break;
                case DockRoom.Thermal:
                    Line(TT("Coolant loop to radiators", "ท่อน้ำยาไปแผงระบายความร้อน"), s.ThermalOk);
                    box.Add(LdPara($"{TT("Heat", "ความร้อน")}: {LdLevel(s.HeatLevel)}" + (s.TransmitterTripped ? TT("  · transmitter switched off (too hot)", "  · เครื่องส่งตัดเอง (ร้อนเกิน)") : "") + (s.LoadShedToday ? TT("  · spare machines off today", "  · ปิดเครื่องที่ไม่จำเป็นแล้ววันนี้") : ""), LdLevelColor(s.HeatLevel), 22));
                    break;
                case DockRoom.Storage:
                    box.Add(LdPara(TT($"Lockers left to search: {s.FindsLeft}", $"ตู้ที่ยังค้นได้: {s.FindsLeft}"), GameUI.CText, 22));
                    break;
                case DockRoom.Comms:
                    box.Add(LdPara(s.RadioFixed ? TT("Radio: working", "วิทยุ: ใช้งานได้") : TT($"Radio: broken — repair {s.RadioRepair}/{s.C.radioRepairDays}" + (s.HasRadioKit ? " (repair kit: 1 session)" : ""), $"วิทยุ: เสีย — ซ่อมแล้ว {s.RadioRepair}/{s.C.radioRepairDays}" + (s.HasRadioKit ? " (มีชุดซ่อม: ครั้งเดียวเสร็จ)" : "")), s.RadioFixed ? GameUI.CGreen : GameUI.CAmber, 22));
                    box.Add(LdPara(TT($"Location sent: {s.Signal} of {s.C.signalNeeded}", $"ส่งพิกัดแล้ว: {s.Signal} จาก {s.C.signalNeeded}"), s.Signal >= s.C.signalNeeded ? GameUI.CGreen : GameUI.CCyan, 22));
                    if (s.TransmitterTripped) box.Add(LdPara(TT("Too hot: the transmitter switched itself off. Cool the station first.", "ร้อนเกิน: เครื่องส่งตัดเอง ต้องลดความร้อนก่อน"), GameUI.CRed, 22));
                    break;
                default:
                    Line(TT("CO₂ scrubber", "เครื่องกรอง CO₂"), s.ScrubberOk);
                    Line(TT("Air fan", "พัดลม"), s.FanOk || s.BackupCircuit);
                    Line(TT("O₂ generator", "เครื่องผลิต O₂"), s.O2GenOk);
                    Line(TT("Coolant loop", "ท่อระบายความร้อน"), s.ThermalOk);
                    foreach (DockRoom r in new[] { DockRoom.LifeSupport, DockRoom.Thermal, DockRoom.Storage, DockRoom.Comms })
                        if (!s.Usable(r) || s.LeakRoom == r)
                            box.Add(LdPara("• " + LdRoomName(r) + ": " + (s.Detached.Contains(r) ? TT("detached", "ปลดออกแล้ว") : s.Sealed.Contains(r) ? TT("hatch closed", "ปิดประตูกั้นแล้ว") : TT("LEAKING", "รั่ว!")), GameUI.CRed, 22));
                    box.Add(LdPara(TT($"Docking port needs {s.C.dockPower} battery on day {s.C.days}. Battery now: {s.Battery}.", $"ช่องเทียบท่าต้องใช้แบต {s.C.dockPower} ในวันที่ {s.C.days} ตอนนี้มีแบต {s.Battery}"), s.Battery >= s.C.dockPower ? GameUI.CDim : GameUI.CAmber, 22));
                    box.Add(LdPara(TT($"Each night: eat 1 food, solar +{s.C.solarPerDay} battery, station uses {s.C.baseLoad}" + (s.BackupCircuit ? " (+1 backup fan)" : "") + ".",
                        $"ทุกคืน: กินอาหาร 1 โซลาร์เซลล์ให้แบต +{s.C.solarPerDay} สถานีใช้ {s.C.baseLoad}" + (s.BackupCircuit ? " (+1 พัดลมสำรอง)" : "")), GameUI.CDim, 20));
                    break;
            }
            return box;
        }

        VisualElement LdIncidentBlock(Fault f)
        {
            var inc = LdIncidentOf(f);
            var box = new VisualElement();
            box.style.backgroundColor = new Color(0.25f, 0.04f, 0.06f, 0.7f); GameUI.Border(box, GameUI.CRed, 2, 10);
            box.style.paddingLeft = box.style.paddingRight = 14; box.style.paddingTop = box.style.paddingBottom = 8; box.style.marginBottom = 10;
            box.Add(LdPara("! " + inc.title, GameUI.CRed, 28));
            box.Add(LdPara(TT("What we notice: ", "อาการ: ") + inc.symptom, GameUI.CText, 22));
            box.Add(LdPara(TT("Why: ", "สาเหตุ: ") + inc.diagnosis, GameUI.CText, 22));
            box.Add(LdPara(TT("If we wait: ", "ถ้าปล่อยไว้: ") + inc.ignore, GameUI.CAmber, 22));
            return box;
        }

        string LdPreviewText(string action)
        {
            if (action.StartsWith("explore") || action == "send_rover" || action == "open_mystery") return TT("the result is random", "ผลเป็นแบบสุ่ม");
            var p = _ld.Preview(action);
            if (!p.ok) return "";
            var parts = new List<string>();
            void N(string name, int d) { if (d != 0) parts.Add(name + " " + (d > 0 ? "+" : "−") + Mathf.Abs(d)); }
            N(LdRes(Res.Food), p.food); N("O₂", p.oxygen); N(LdRes(Res.Battery), p.battery); N(LdRes(Res.Parts), p.parts);
            N(TT("Absorber", "กระป๋องดูดซับ"), p.absorbers); N("CO₂", p.co2); N(TT("Heat", "ความร้อน"), p.heat); N(TT("Signal", "สัญญาณ"), p.signal); N(TT("Crew", "ลูกเรือ"), p.health);
            return parts.Count == 0 ? TT("no change in numbers", "ตัวเลขไม่เปลี่ยน") : string.Join(" · ", parts);
        }

        string LdBlockedText(string why) => why switch
        {
            "no_parts" => TT("not enough parts", "อะไหล่ไม่พอ"), "no_battery" => TT("no battery", "แบตไม่พอ"), "no_absorber" => TT("no canisters left", "ไม่มีกระป๋องเหลือ"),
            "too_hot" => TT("too hot — cool the station first", "ร้อนเกินไป ต้องลดความร้อนก่อน"), "load_shed" => TT("spare machines are off today", "วันนี้ปิดเครื่องไปแล้ว"),
            "room_closed" => TT("module closed", "โมดูลถูกปิด"), "empty" => TT("nothing left", "ไม่เหลืออะไรแล้ว"), "done" => TT("already done", "ทำครบแล้ว"),
            "no_time" => TT("no work units left today", "วันนี้หมดเวลาทำงานแล้ว"), "radio_broken" => TT("the radio must be repaired first (quest 1)", "ต้องซ่อมวิทยุก่อน (เควส 1)"),
            "rover_parts" => TT("not all rover parts found yet", "ยังหาชิ้นส่วนรถไม่ครบ"), "rover_broken" => TT("the rover is broken", "รถเสียอยู่"), "no_food" => TT("not enough food", "อาหารไม่พอ"),
            "raid_pending" => TT("answer the strangers first", "ต้องตอบคนแปลกหน้าก่อน"), "once_a_day" => TT("already done today", "วันนี้ทำไปแล้ว"), "no_medkit" => TT("no medicine", "ไม่มียา"), "no_flashlight" => TT("no flashlight", "ไม่มีไฟฉาย"), _ => why,
        };

        void LdShowPanel(DockRoom room, string spot)
        {
            _ui.ShowModal(p =>
            {
                string title = spot == "ld_dock" ? TT("Docking port", "ช่องเทียบท่า") : spot == "ld_table" ? TT("Dinner table", "โต๊ะกินข้าว") : spot == "ld_board" ? TT("Station board", "แผงสถานะสถานี") : LdRoomName(room);
                LdHeader(p, title);
                p.Add(LdPara(TT($"Day {_ld.Day} of {_ld.C.days} · work units left: {_ld.WorkUnits}", $"วันที่ {_ld.Day} จาก {_ld.C.days} · เวลาทำงานเหลือ: {_ld.WorkUnits}"), GameUI.CCyan, 22));
                var f = _ld.ActiveFault;
                if (f != Fault.None) { var inc = LdIncidentOf(f); if (spot == "ld_board" || spot == "ld_dock" || inc.room == room) p.Add(LdIncidentBlock(f)); }
                p.Add(LdStatusBlock(room));
                foreach (var a in LdConsoleActions(room, spot)) p.Add(LdActionRow(a));
                if (spot == "ld_board" && _ldIncidents.Count > 0) p.Add(_ui.B(TT("Incident log", "บันทึกเหตุการณ์"), LdShowIncidentLog, 22));
            });
        }

        VisualElement LdActionRow(string a) => LdCard(a);   // styled card (GameFlow.LdStyle.cs)

        void LdShowDip()
        {
            _ui.ShowModal(p =>
            {
                LdHeader(p, LdIncidentOf(Fault.PowerDip).title);
                p.Add(LdIncidentBlock(Fault.PowerDip));
                foreach (var a in new[] { "dip_flashlight", "dip_battery", "dip_shed", "dip_cancel" }) if (a != "dip_flashlight" || _ld.HasFlashlight) p.Add(LdActionRow(a));
                p.Add(LdPara("NASA: " + LdIncidentOf(Fault.PowerDip).fact, GameUI.CCyan, 20));
            }, false);
        }

        void LdShowIncidentLog()
        {
            _ui.ShowModal(p =>
            {
                LdHeader(p, TT("Incident log", "บันทึกเหตุการณ์"));
                foreach (var (day, f, room) in _ldIncidents)
                {
                    var inc = LdIncidentOf(f);
                    p.Add(LdPara(TT($"Day {day} · {room}: ", $"วันที่ {day} · {room}: ") + inc.title, GameUI.CAmber, 22));
                    p.Add(LdPara("NASA: " + inc.fact, GameUI.CCyan, 20));
                }
            });
        }

        // ------------------------------------------------------------------ acting

        /// <summary>Take an action through the simulation, then show what happened.</summary>
        void LdDo(string a)
        {
            if (_ld == null || _ldEnded) return;
            int day = _ld.Day;
            var before = _ld.ActiveFault;
            int logMark = _ld.Log.Count;
            if ((a.StartsWith("explore_") || a == "send_rover") && _ld.Can(a, out _)) { StartCoroutine(LdExpedition(a)); return; }   // off-screen trip (GameFlow.DayFlow.cs)
            _ui.CloseModal();
            var r = _ld.Do(a);
            if (!r.ok) { _sfx.Play("bad"); _ui.Toast(LdBlockedText(r.reason), GameUI.CRed); return; }
            _sfx.Play(a.StartsWith("repair") ? "good" : "card");
            if (a.StartsWith("repair") || a == "assemble_rover") _sfx.Play("clank");
            if (a == "prepare_shelter") _sfx.Play("shield");
            if (a.StartsWith("repair") || a == "assemble_rover") LdSparks();
            StartCoroutine(Act(a.StartsWith("repair") || a == "search" ? "lift" : "lift", .45f, null));
            foreach (var n in r.notes) { var t = LdNote(n); if (t != null) _ui.Toast(t, n == "power_dip" ? GameUI.CRed : GameUI.CGreen); }
            // the solved incident teaches its fact
            if (before != Fault.None && before != Fault.PowerDip && _ld.ActiveFault != before)
                _ui.Toast("NASA: " + LdIncidentOf(before).fact, GameUI.CCyan);
            LdAfterAction(day, logMark);
        }

        string LdNote(string n)
        {
            if (n.StartsWith("found_") && n != "found_rover_part" && n != "found_mystery")
            {
                string k = n.Substring(6);
                string what = k == "absorber" ? TT("a CO₂ absorber canister", "กระป๋องดูดซับ CO₂") : k == "food" ? LdRes(Res.Food) : k == "battery" ? LdRes(Res.Battery) : k == "oxygen" ? TT("an oxygen tank", "ถังออกซิเจน") : LdRes(Res.Parts);
                return TT("Found: ", "เจอ: ") + what + " +1";
            }
            switch (n)
            {
                case "signal_sent": return TT($"Location sent! ({_ld.Signal}/{_ld.C.signalNeeded})", $"ส่งพิกัดแล้ว! ({_ld.Signal}/{_ld.C.signalNeeded})");
                case "signal_cancelled": return TT("Send cancelled. Battery saved.", "ยกเลิกการส่ง เก็บแบตไว้");
                case "power_dip": return TT("Power dip! Choose what to do.", "ไฟตก! เลือกว่าจะทำอย่างไร");
                case "absorber_used": return TT("Canister in: CO₂ drops.", "ใส่กระป๋องแล้ว CO₂ ลดลง");
                case "sheltered": return TT("Resting calmly in the core.", "พักหายใจช้า ๆ ในแกนกลาง");
                case "low_activity": return TT("Moving slowly to save oxygen.", "เคลื่อนไหวช้า ๆ เพื่อประหยัดออกซิเจน");
                case "load_shed": return TT("Spare machines off. It's cooling down.", "ปิดเครื่องที่ไม่จำเป็นแล้ว กำลังเย็นลง");
                case "backup_fan": return TT("Backup fan on. Air is moving.", "เปิดพัดลมสำรองแล้ว อากาศหมุนเวียน");
                case "cannibalized": return TT("Took a part from the transmitter.", "ถอดอะไหล่จากเครื่องส่งแล้ว");
                case "detached_storage": return TT("Storage module detached. The leak is gone.", "ปลดห้องเก็บของแล้ว รอยรั่วหายไป");
                case "rest_interrupted": return TT("Woken by an alarm!", "ตื่นเพราะเสียงเตือน!");
                case "radio_progress": return TT($"Radio repair: {_ld.RadioRepair}/{_ld.C.radioRepairDays}. Come back tomorrow.", $"ซ่อมวิทยุ: {_ld.RadioRepair}/{_ld.C.radioRepairDays} พรุ่งนี้มาซ่อมต่อ");
                case "radio_fixed": return TT("The radio works! Now send our location.", "วิทยุใช้ได้แล้ว! ส่งพิกัดได้เลย");
                case "explore_radiation": return TT("A radiation hot spot! The crew is hurt.", "เจอจุดรังสีสูง! ลูกเรือบาดเจ็บ");
                case "healed": return TT("Medicine taken: crew health +1.", "กินยาแล้ว สุขภาพลูกเรือ +1");
                case "found_rover_part": return TT($"Found a rover part! ({_ld.RoverParts}/{_ld.C.roverPartsNeeded})", $"เจอชิ้นส่วนรถ! ({_ld.RoverParts}/{_ld.C.roverPartsNeeded})");
                case "found_mystery": return TT("Found 2 food boxes with no label.", "เจอกล่องอาหารไม่มีฉลาก 2 กล่อง");
                case "shelter_ready": return TT("The storm shelter is ready for tonight.", "ห้องหลบรังสีพร้อมสำหรับคืนนี้");
                case "food_spoiled": return TT("Yuck, it was spoiled! Upset stomach tomorrow.", "แย่แล้ว อาหารเสีย! พรุ่งนี้ท้องเสีย");
                case "food_great": return TT("Great food! +2 meals.", "อาหารดีมาก! +2 มื้อ");
                case "food_ok": return TT("Normal food: +1 meal.", "อาหารปกติ: +1 มื้อ");
                case "rover_built": return TT("The rover is ready!", "รถสำรวจพร้อมแล้ว!");
                case "rover_solar": return TT("The solar panel now rides on the rover.", "ย้ายแผงโซลาร์ไปติดรถแล้ว");
                case "rover_back": return TT("The rover came back with supplies.", "รถกลับมาพร้อมของ");
                case "rover_broke": return TT("The rover broke down on the way back.", "รถเสียระหว่างทางกลับ");
                case "rover_fixed": return TT("The rover is fixed.", "ซ่อมรถเสร็จแล้ว");
                case "raiders": return TT("Strangers followed the rover home!", "มีคนแปลกหน้าตามรถกลับมา!");
                case "raid_paid": return TT("The strangers took the supplies and left.", "คนแปลกหน้าเอาของแล้วจากไป");
                case "raid_refused": return TT("You said no. Watch out tonight...", "คุณปฏิเสธ ระวังคืนนี้ให้ดี...");
            }
            if (n.StartsWith("fixed_")) return TT("Fixed!", "ซ่อมเสร็จ!");
            if (n.StartsWith("sealed_")) return TT("Hatch closed. The leak is contained.", "ปิดประตูกั้นแล้ว รอยรั่วถูกกั้นไว้");
            return null;
        }

        string LdEventText(string e)
        {
            if (e.StartsWith("fault_")) { var k = e.Split('_')[1]; if (System.Enum.TryParse<Fault>(k, out var f)) return "! " + LdIncidentOf(f).title; }
            if (e.StartsWith("transmitter_tripped")) return TT("The transmitter overheated and switched off.", "เครื่องส่งร้อนเกินและตัดเอง");
            switch (e)
            {
                case "signal_sent": return TT("Location sent.", "ส่งพิกัดแล้ว");
                case "co2_critical": return TT("CO₂ became critical — the crew got hurt.", "CO₂ ถึงขั้นวิกฤต ลูกเรือบาดเจ็บ");
                case "no_oxygen_leak": return TT("Oxygen ran out while air leaked — the crew got hurt.", "ออกซิเจนหมดเพราะอากาศรั่ว ลูกเรือบาดเจ็บ");
                case "no_oxygen_o2gen_down": return TT("Oxygen ran out with the generator stopped — the crew got hurt.", "ออกซิเจนหมดเพราะเครื่องผลิตหยุด ลูกเรือบาดเจ็บ");
                case "heat_shutdown": return TT("Too hot: batteries drained.", "ร้อนเกิน แบตเตอรี่ถูกดูดไป");
                case "hungry": return TT("No food left — the crew got weaker.", "อาหารหมด ลูกเรืออ่อนแรง");
                case "no_power": return TT("The battery ran flat at night — the crew got cold and weak.", "แบตหมดตอนกลางคืน ลูกเรือหนาวและอ่อนแรง");
                case "tired": return TT("High CO₂ made everyone tired: one less work unit tomorrow.", "CO₂ สูงทำให้ทุกคนเหนื่อย พรุ่งนี้เวลาทำงานลดลง 1");
                case "sealed": return TT("A hatch was closed to stop a leak.", "ปิดประตูกั้นเพื่อหยุดรอยรั่ว");
                case "detached_storage": return TT("The storage module was detached.", "ปลดโมดูลห้องเก็บของออก");
                case "cannibalized": return TT("A part was taken from the transmitter.", "ถอดอะไหล่จากเครื่องส่ง");
                case "power_dip": return TT("The power dipped during a send.", "ไฟตกระหว่างส่งสัญญาณ");
                case "radiation": return TT("Radiation outside hurt the crew.", "รังสีข้างนอกทำให้ลูกเรือบาดเจ็บ");
                case "radio_fixed": return TT("The radio was repaired.", "ซ่อมวิทยุสำเร็จ");
                case "healed": return TT("Medicine helped the crew.", "ยาช่วยลูกเรือ");
                case "storm_radiation": return TT("The solar storm's radiation got into the base — the crew was hurt.", "รังสีจากพายุสุริยะเข้ามาในฐาน ลูกเรือบาดเจ็บ");
                case "shelter_on": return TT("The storm shelter kept everyone safe (1 battery).", "ห้องหลบรังสีช่วยทุกคนไว้ (ใช้แบต 1)");
                case "raiders_stole": return TT("The strangers came back in the night and took food. (story)", "คนแปลกหน้ากลับมาตอนกลางคืนและเอาอาหารไป (เรื่องแต่ง)");
                case "raiders_scared": return TT("Your flashlight scared the strangers away. (story)", "ไฟฉายไล่คนแปลกหน้าไปได้ (เรื่องแต่ง)");
                case "dark_warning": return TT("The base went dark. Something scratched at the door... (story)", "ฐานมืดสนิท มีอะไรบางอย่างขูดประตู... (เรื่องแต่ง)");
                case "intruder_stole": return TT("Another dark night: something got in and took supplies. (story)", "มืดอีกคืน มีบางอย่างเข้ามาเอาของไป (เรื่องแต่ง)");
                case "intruder_scared": return TT("Your flashlight kept the thing at the door away. (story)", "ไฟฉายไล่สิ่งที่อยู่หน้าประตูไปได้ (เรื่องแต่ง)");
                case "food_poisoning": return TT("Bad food made the crew sick.", "อาหารเสียทำให้ลูกเรือป่วย");
                case "sick": return TT("Upset stomach: one less work unit tomorrow.", "ท้องเสีย: พรุ่งนี้เวลาทำงานลดลง 1");
                case "rover_built": return TT("The rover was assembled.", "ประกอบรถสำรวจแล้ว");
                case "rover_broke": return TT("The rover broke down.", "รถสำรวจเสีย");
                case "signal_missing": return TT("The rescue ship arrived, but it never got our location.", "ยานกู้ภัยมาถึง แต่ไม่ได้รับพิกัดของเรา");
                case "dock_power_missing": return TT("The rescue ship arrived, but there was not enough battery to open the docking port.", "ยานกู้ภัยมาถึง แต่แบตไม่พอเปิดช่องเทียบท่า");
            }
            if (e.StartsWith("fixed_")) return TT("Something was repaired.", "ซ่อมอุปกรณ์สำเร็จ");
            return null;
        }

        void LdNightReport(int day, int logMark)
        {
            var lines = new List<string>();
            for (int i = logMark; i < _ld.Log.Count; i++) { var t = LdEventText(_ld.Log[i]); if (t != null && !lines.Contains(t)) lines.Add(t); }
            _sfx.Play("card");
            _ui.ShowModal(p =>
            {
                LdHeader(p, TT($"Night {day}", $"คืนที่ {day}"));
                p.Add(LdPara(TT($"Everyone ate 1 food. Solar panels gave +{_ld.C.solarPerDay} battery; the station used {_ld.C.baseLoad}" + (_ld.BackupCircuit ? " (+1 for the backup fan)." : "."),
                    $"ทุกคนกินอาหาร 1 โซลาร์เซลล์ให้แบต +{_ld.C.solarPerDay} สถานีใช้ไป {_ld.C.baseLoad}" + (_ld.BackupCircuit ? " (+1 พัดลมสำรอง)" : "")), GameUI.CText, 22));
                foreach (var l in lines) p.Add(LdPara("• " + l, GameUI.CDim, 22));
                p.Add(LdPara(TT($"Morning of day {_ld.Day}. {_ld.C.days - _ld.Day} more night(s) until rescue. Work units today: {_ld.WorkUnits}.",
                    $"เช้าวันที่ {_ld.Day} อีก {_ld.C.days - _ld.Day} คืนยานกู้ภัยจะมา วันนี้มีเวลาทำงาน {_ld.WorkUnits} ช่วง"), GameUI.CAmber, 24));
                if (_ld.ActiveFault != Fault.None) p.Add(LdPara("! " + LdIncidentOf(_ld.ActiveFault).title, GameUI.CRed, 24));
            });
        }

        // ------------------------------------------------------------------ end

        /// <summary>The short cause chain for the lose screen: the incident that started it, then what followed.</summary>
        List<string> LdCauseChain()
        {
            var chain = new List<string>();
            int lostAt = _ld.Log.LastIndexOf("lost");
            int start = 0;
            for (int i = lostAt - 1; i >= 0; i--)
                if (_ld.Log[i].StartsWith("fault_")) { start = i; break; }
            for (int i = start; i < lostAt; i++)
            {
                var e = _ld.Log[i];
                if (e.StartsWith("fixed_") || e == "signal_sent" || e == "tired" && chain.Count > 3) continue;
                var t = LdEventText(e);
                if (t != null && (chain.Count == 0 || chain[chain.Count - 1] != t)) chain.Add(t);
            }
            if (chain.Count == 0) chain.Add(TT("The crew ran out of strength.", "ลูกเรือหมดแรง"));
            if (chain.Count > 5) chain.RemoveRange(1, chain.Count - 5);
            return chain;
        }

        string LdTip()
        {
            var log = _ld.Log;
            if (log.Contains("signal_missing")) return TT("Tip: start the radio repair on day 1 (one session a day), then send early. The radio repair kit saves two days.", "เคล็ดลับ: เริ่มซ่อมวิทยุตั้งแต่วันแรก (ซ่อมได้วันละครั้ง) แล้วรีบส่งพิกัด ชุดซ่อมวิทยุช่วยประหยัด 2 วัน");
            if (log.Contains("dock_power_missing")) return TT("Tip: keep 2 battery for the docking port. Search the lockers for spare batteries.", "เคล็ดลับ: เก็บแบต 2 ไว้เปิดช่องเทียบท่า และค้นตู้หาแบตสำรอง");
            if (log.Contains("co2_critical")) return TT("Tip: when CO₂ rises, fix the scrubber or fan first, or use an absorber canister.", "เคล็ดลับ: ถ้า CO₂ สูง ให้ซ่อมเครื่องกรองหรือพัดลมก่อน หรือใช้กระป๋องดูดซับ");
            if (log.Exists(e => e.StartsWith("no_oxygen"))) return TT("Tip: a leak empties the oxygen tanks fast. Close the hatch, then patch it.", "เคล็ดลับ: รอยรั่วทำให้ออกซิเจนหมดเร็ว ปิดประตูกั้นก่อน แล้วค่อยอุด");
            return TT("Tip: read the station board each morning. Every problem has more than one fix.", "เคล็ดลับ: อ่านแผงสถานะทุกเช้า ทุกปัญหามีทางแก้มากกว่าหนึ่งทาง");
        }

        IEnumerator LdEnd()
        {
            _ldEnded = true;
            _sfx.Loop("siren", false);
            bool won = _ld.Outcome == Outcome.Rescued;
            _sfx.Play(won ? "good" : "bad");
            LdStinger(won ? "victory" : "transmission");
            yield return new WaitForSeconds(0.8f);
            _ui.ShowScreen(root =>
            {
                root.Add(GameUI.Backdrop(new Color(0.02f, 0.03f, 0.07f, 0.96f)));
                var box = new VisualElement(); box.style.alignItems = Align.Center; box.style.maxWidth = 1300;
                var head = _ui.L(won ? TT("RESCUED! The ship docked at LAST DOCK.", "รอดแล้ว! ยานกู้ภัยเทียบท่าที่ LAST DOCK") : TT("The station could not hold on this time.", "ครั้งนี้สถานีไปต่อไม่ไหว"), 50, true, won ? GameUI.CAmber : GameUI.CRed);
                head.style.whiteSpace = WhiteSpace.Normal; head.style.unityTextAlign = TextAnchor.MiddleCenter;
                box.Add(head);
                if (won)
                {
                    box.Add(LdPara(TT($"You kept the crew safe for {_ld.C.days} days, sent the location {_ld.Signal} times and opened the docking port with {_ld.Battery} battery.",
                        $"คุณดูแลลูกเรือได้ {_ld.C.days} วัน ส่งพิกัด {_ld.Signal} ครั้ง และเปิดช่องเทียบท่าด้วยแบต {_ld.Battery}"), GameUI.CText, 28));
                    box.Add(LdPara(TT($"Problems solved: {_ldIncidents.Count}. Crew health: {_ld.Health}/{_ld.C.health}.", $"ปัญหาที่แก้ได้: {_ldIncidents.Count} ครั้ง สุขภาพลูกเรือ: {_ld.Health}/{_ld.C.health}"), GameUI.CGreen, 26));
                }
                else
                {
                    box.Add(LdPara(TT("What happened:", "เกิดอะไรขึ้น:"), GameUI.CDim, 26));
                    var chain = LdCauseChain();
                    for (int i = 0; i < chain.Count; i++) box.Add(LdPara((i == 0 ? "" : "↓ ") + chain[i], i == chain.Count - 1 ? GameUI.CRed : GameUI.CText, 26));
                    box.Add(LdPara(LdTip(), GameUI.CCyan, 24));
                }
                if (_ldIncidents.Count > 0)
                {
                    box.Add(LdPara(TT("What you learned:", "สิ่งที่ได้เรียนรู้:"), GameUI.CDim, 22));
                    foreach (var f in _ldIncidents.Select(i => i.f).Distinct().Take(3)) box.Add(LdPara("NASA: " + LdIncidentOf(f).fact, GameUI.CCyan, 20));
                }
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginTop = 24;
                row.Add(_ui.B(TT("Restart the week (same supplies)", "เริ่มสัปดาห์ใหม่ (ของเดิม)"), () => StartLastDock(_ldKit, _ldSeed + 1), 26, true));
                row.Add(_ui.B(_loc.T("end.title"), RestartToTitle, 24));
                box.Add(row);
                root.Add(box);
            });
        }

        // ------------------------------------------------------------------ scramble link

        /// <summary>Where the LAST DOCK supplies lie on the ship: four of each kind, spread over the rooms.</summary>
        static readonly (string id, float x, float y)[] LdShipSupplies =
        {
            ("sup_food", 28.4f, 16.8f), ("sup_food", 33.0f, 15.0f), ("sup_food", 22.0f, 15.2f), ("sup_food", 14.0f, 11.0f),
            ("sup_o2", 21.5f, 4.6f), ("sup_o2", 24.5f, 6.6f), ("sup_o2", 40.0f, 8.0f), ("sup_o2", 12.0f, 15.2f),
            ("sup_battery", 5.5f, 6.0f), ("sup_battery", 5.0f, 14.5f), ("sup_battery", 40.5f, 11.0f), ("sup_battery", 24.0f, 11.8f),
            ("sup_parts", 11.5f, 4.5f), ("sup_parts", 13.5f, 6.6f), ("sup_parts", 10.6f, 18.0f), ("sup_parts", 14.8f, 14.6f),
        };

        /// <summary>The rules, read before the clock starts (the clock waits while a panel is open).</summary>
        void LdScrambleRules()
        {
            _ui.ShowModal(p =>
            {
                LdHeader(p, TT("60 SECONDS TO THE LIFEBOAT", "60 วินาที ขนของขึ้นยานชูชีพ"));
                p.Add(LdPara(TT("The ship is breaking up. Grab supplies and carry them to the lifeboat in the bay.", "ยานกำลังแตก หยิบเสบียงแล้วขนไปที่ยานชูชีพในห้องจอด")));
                p.Add(LdPara(TT($"• Carry up to {ScavengeInventory.CarryMax} things at a time (E to pick up).", $"• ถือได้ครั้งละ {ScavengeInventory.CarryMax} ชิ้น (กด E เพื่อหยิบ)")));
                p.Add(LdPara(TT($"• The lifeboat holds {ScavengeInventory.PodMax}. Only what is IN the lifeboat comes with you.", $"• ยานชูชีพจุได้ {ScavengeInventory.PodMax} ชิ้น เฉพาะของที่อยู่ในยานเท่านั้นที่ไปกับเรา")));
                p.Add(LdPara(TT("• When time runs out the lifeboat launches; things in your hands are lost.", "• หมดเวลายานชูชีพจะออกทันที ของที่ยังถืออยู่จะหายไป")));
                p.Add(LdPara(TT("• Two rooms by the cockpit are locked. ONE key card is hidden on the ship — it opens only one of them.", "• ห้องข้างห้องบังคับยาน 2 ห้องล็อกอยู่ มีคีย์การ์ดใบเดียวซ่อนบนยาน เปิดได้แค่ห้องเดียว"), GameUI.CAmber));
                p.Add(LdPara(TT("• Special items (medicine, radiation meter, flashlight, frozen food) are somewhere different every time.", "• ของพิเศษ (ยา เครื่องวัดรังสี ไฟฉาย อาหารแช่แข็ง) อยู่คนละที่ทุกรอบ"), GameUI.CCyan));
                p.Add(LdPara(TT("Food keeps the crew going, Oxygen refills the tanks, Battery powers the radio and docking port, Parts fix machines.", "อาหารให้ลูกเรือมีแรง ออกซิเจนเติมถัง แบตเตอรี่ใช้ส่งสัญญาณและเปิดช่องเทียบท่า อะไหล่ใช้ซ่อมเครื่อง"), GameUI.CCyan));
                p.Add(LdPara(TT("The station you will reach has a little of everything, so any mix can work.", "สถานีที่จะไปมีของอยู่บ้างทุกอย่าง จึงเลือกแบบไหนก็รอดได้"), GameUI.CDim, 20));
                var go = _ui.B(TT("Start!", "เริ่ม!"), _ui.CloseModal, 30, true); go.style.marginTop = 14; go.style.alignSelf = Align.Center;
                p.Add(go);
            }, false);
        }

        IEnumerator LdAfterScramble()
        {
            var kit = _ldInv.Delivered();
            yield return Fade(1f, 0.6f);
            _ui.FadeText(TT("The lifeboat falls through the thin red sky of Mars... and lands beside an old, empty base.", "ยานชูชีพตกผ่านฟ้าสีแดงบาง ๆ ของดาวอังคาร... แล้วลงจอดข้างฐานเก่าที่ไม่มีใคร"), 40);
            yield return new WaitForSeconds(_playtest ? 1.5f : 3f);
            _ui.FadeText(null);
            StartLastDock(kit, SeedOverride >= 0 ? SeedOverride : System.Environment.TickCount);
        }

        // scramble bookkeeping goes through the tested ScavengeInventory
        const int LdPodMax = ScavengeInventory.PodMax;
        void LdNewInventory() => _ldInv = new ScavengeInventory();
        bool LdPick(string supplyId) => _ldInv.Pick(LdResOfSupply(supplyId));
        int LdDeposit() => _ldInv.Deposit();

        // ------------------------------------------------------------------ auto play (-autoshot -play)

        BotPlan LdPlanFromArgs()
        {
            var args = System.Environment.GetCommandLineArgs();
            return _playDumb ? BotPlan.Neglect : System.Array.IndexOf(args, "-battery") >= 0 ? BotPlan.BatteryFirst : BotPlan.Balanced;
        }

        /// <summary>-autoshot -play -lastdock: straight into the station with a kit from -kit F,O,B,P (default 2,2,2,2).</summary>
        IEnumerator PlayLastDockDirect()
        {
            var args = System.Environment.GetCommandLineArgs();
            var kit = new Dictionary<Res, int> { [Res.Food] = 2, [Res.Oxygen] = 2, [Res.Battery] = 2, [Res.Parts] = 2 };
            int ki = System.Array.IndexOf(args, "-kit");
            if (ki >= 0 && ki + 1 < args.Length)
            {
                var n = args[ki + 1].Split(',');
                if (n.Length == 4) { kit[Res.Food] = int.Parse(n[0]); kit[Res.Oxygen] = int.Parse(n[1]); kit[Res.Battery] = int.Parse(n[2]); kit[Res.Parts] = int.Parse(n[3]); }
            }
            StartLastDock(kit, SeedOverride);
            yield return new WaitForSeconds(1.2f);
            yield return PlayLastDock(LdPlanFromArgs());
        }

        /// <summary>After the scramble in a full -play run: wait for the station, then play it.</summary>
        IEnumerator PlayLastDockAfterScramble()
        {
            float t = 0;
            while ((_ld == null || _bMode != 2) && t < 60) { t += Time.deltaTime; yield return null; }
            PNote($"lifeboat to station took {t:0}s; delivered {string.Join(",", _ldInv.Delivered().Select(kv => kv.Key + "=" + kv.Value))}");
            Check(_ld != null && _bMode == 2, "station started after the scramble");
            if (_ld == null) yield break;
            yield return new WaitForSeconds(1f);
            yield return PlayLastDock(LdPlanFromArgs());
        }

        IEnumerator PlayLastDock(BotPlan plan)
        {
            PNote($"LAST DOCK start, plan {plan}, kit F{_ld.Food} O{_ld.Oxygen} B{_ld.Battery} P{_ld.Parts}");
            yield return Shot("ld01_intro");
            _ui.CloseModal();
            var walkTo = BSpotById("ld_lockers"); if (walkTo != null) { BGoTo(walkTo.stand, null); yield return new WaitForSeconds(0.6f); yield return Shot("ld02_walk"); }
            var outSite = BSpotById("site_0"); if (outSite != null) { BGoTo(outSite.stand, null); float tw = 0; while (_bPath != null && tw < 12f) { tw += Time.deltaTime; yield return null; } yield return Shot("ld04_outside"); }
            float start = _playClock;
            int shots = 0, guard = 0; bool faultShot = false;
            while (_ld.Outcome == Outcome.Ongoing && guard++ < 120)
            {
                while (_ui.ModalOpen && !_ld.PowerDipPending && !_ld.RaiderPending) { if (shots < 12 && _ld.Day >= 2) { shots++; yield return Shot($"ld{10 + shots:00}_modal"); } _ui.CloseModal(); yield return new WaitForSeconds(0.2f); }
                string a = LastDockBot.Choose(_ld, plan);
                if (!a.StartsWith("dip_") && !a.StartsWith("raid_"))
                {
                    string spot = a == "end_day" || a == "open_mystery" ? "ld_table" : a.StartsWith("explore_") || a.Contains("rover") ? "ld_airlock"
                        : a == "use_medkit" || a == "prepare_shelter" || a == "open_mystery" ? "ld_board" : a == "send" || a == "cannibalize" || a == "repair_radio" ? "ld_radio" : a == "search" ? "ld_lockers" : a == "shed_load" || a == "repair_thermal" ? "ld_thermal"
                        : a == "shelter" || a == "repair_leak" || a == "seal_room" || a == "detach_storage" ? "ld_board"
                        : a == "repair_fan" || a == "backup_fan" ? "ld_fan" : a == "repair_o2gen" || a == "low_activity" ? "ld_o2gen" : "ld_scrubber";
                    if (spot != "ld_board" && !_ld.Usable(LdRoomOfSpot(spot))) spot = "ld_board";
                    yield return AutoWalkUse(spot);
                    if (spot == "ld_airlock" && !_ldShotAirlock) { _ldShotAirlock = true; yield return new WaitForSeconds(0.5f); yield return Shot("ld07_airlock"); }
                    yield return new WaitForSeconds(0.3f);
                    if (_ld.ActiveFault != Fault.None && shots < 12) { shots++; yield return Shot($"ld{10 + shots:00}_panel"); }
                }
                PNote($"day {_ld.Day} wu {_ld.WorkUnits}: {a}  [F{_ld.Food} O{_ld.Oxygen} B{_ld.Battery} P{_ld.Parts} CO2 {_ld.Co2} H{_ld.Heat} S{_ld.Signal} HP{_ld.Health} fault {_ld.ActiveFault}]");
                LdDo(a);
                yield return new WaitForSeconds(0.4f);
                { float ws = 0; while ((_ldSeq || _ui.ScreenOpen && !_ldEnded) && ws < 20f) { ws += Time.deltaTime; yield return null; } }
                if (!faultShot && _ld.ActiveFault != Fault.None && _ld.ActiveFault != Fault.PowerDip) { faultShot = true; _ui.CloseModal(); var fs = BSpotById(LdSpotOfRoom(LdIncidentOf(_ld.ActiveFault).room)); if (fs != null) { yield return AutoWalkUse(fs.id); _ui.CloseModal(); yield return new WaitForSeconds(0.5f); } yield return Shot("ld03_fault"); }
            }
            PNote($"LAST DOCK {_ld.Outcome} on day {_ld.Day} in {_playClock - start:0}s; log: {string.Join(",", _ld.Log)}");
            Check(_ld.Outcome != Outcome.Ongoing, "LAST DOCK run finished");
            yield return new WaitForSeconds(1.5f);
            yield return Shot("ld90_end");
            // Restart clears everything
            var kit = _ldKit;
            StartLastDock(kit, _ldSeed);
            yield return new WaitForSeconds(0.8f);
            Check(_ld.Day == 1 && _ld.Outcome == Outcome.Ongoing && _ld.Log.Count == 0 && _ldIncidents.Count == 0 && !_ldEnded, "restart clears the run");
            yield return Shot("ld91_restart");
        }
    }
}
