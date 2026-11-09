// LAST 72 HOURS — core game logic.
// Pure C# (no UnityEngine) so it can be unit-simulated outside Unity.
using System;
using System.Collections.Generic;

namespace Last72
{
    public enum Room { Quarters, Command, LifeSupport, Engineering, Cabin, CommsRoom, Crawlspace, PartsStorage }

    public enum Item { Multitool, AccessCard, SpareFilter, Fuse, RepairCable, SealantKit, NavChip }

    public enum Ending { None, Jump, Mars, FailTime, FailOxygen, FailPower }

    public enum AlertLevel { Info, Warning, Critical, Good }

    public class Alert
    {
        public string Text;
        public AlertLevel Level;
        public int MinutesLeft;
        public bool Banner; // show as big banner
    }

    /// <summary>An action the player can take from an interaction panel.</summary>
    public class GameAction
    {
        public string Id;
        public string Title;
        public string Summary;      // what happens (shown in confirm dialog)
        public int BaseMinutes;     // 0 = free, no confirm
        public bool IsWork;         // affected by hunger/fatigue penalties
        public string Blocker;      // null = available, else reason it is disabled
        public Item? Consumes;      // item removed on completion
        public bool FromItem;       // triggered by using a selected item
        public Action<GameModel> Effect;
    }

    public class Hotspot
    {
        public string Id;
        public Room Room;
        public string Name;
        // Normalised rect inside the room view (0..1, origin bottom-left)
        public float X, Y, W, H;
        public Room? DoorTo; // non-null = door
    }

    public enum ObjectiveState { NotStarted, InProgress, Completed, Optional }

    public class GameModel
    {
        // ---------------- tunables ----------------
        public const int StartMinutes = 72 * 60;
        public const float O2DrainBroken = 1.5f, O2DrainFixed = 0.2f;
        public const float PwrDrainBroken = 0.6f, PwrDrainFixed = 0.15f;
        public const float LeakPerHour = 0.5f;
        public const float HungerPerHour = 1.4f;
        public const float FatiguePerHour = 1.5f;
        public const int StartRations = 4;

        // ---------------- state ----------------
        public int MinutesLeft;
        public float Oxygen, Power, Propellant, Hunger, Fatigue;
        public int Rations;
        public Room CurrentRoom;
        public readonly List<Item> Inventory = new List<Item>();
        public Item? Selected;
        public readonly List<Alert> Alerts = new List<Alert>();
        public Ending Ending = Ending.None;
        public int ActionsTaken;

        // progress flags
        public bool LockerOpened, CabinetOpened, StorageOpened, CommsOpened;
        public bool OxygenFixed, PowerFixed, LeakSealed, CableSpliced, ReserveTransferred;
        public bool NavRecovered, NavCalibrated, JumpStabilized, FinalPrepped;
        public bool EngLocked, EngOverridden, EngWarned;
        public int LogsRead;

        readonly HashSet<string> firedEvents = new HashSet<string>();

        public event Action Changed;

        public static readonly List<Hotspot> Hotspots = BuildHotspots();

        public GameModel() { Reset(); }

        public void Reset()
        {
            MinutesLeft = StartMinutes;
            Oxygen = 72; Power = 58; Propellant = 42; Hunger = 30; Fatigue = 35;
            Rations = StartRations;
            CurrentRoom = Room.Quarters;
            Inventory.Clear(); Selected = null; Alerts.Clear();
            Ending = Ending.None; ActionsTaken = 0;
            LockerOpened = CabinetOpened = StorageOpened = CommsOpened = false;
            OxygenFixed = PowerFixed = LeakSealed = CableSpliced = ReserveTransferred = false;
            NavRecovered = NavCalibrated = JumpStabilized = FinalPrepped = false;
            EngLocked = EngOverridden = EngWarned = false;
            LogsRead = 0;
            firedEvents.Clear();
            Push("ASTRA-01: Debris impact detected. Senior crew lost. You are in command now.", AlertLevel.Critical, true);
            Push("ท่อเชื้อเพลิงรั่ว — Propellant กำลังลดลง. ระบบ O2 เสีย — ออกซิเจนลดเร็ว.", AlertLevel.Warning);
            Push("เริ่มจากตู้ Locker ข้างเตียง: เก็บ Multitool และ Access Card.", AlertLevel.Info);
            Notify();
        }

        // ---------------- helpers ----------------
        public bool IsOver { get { return Ending != Ending.None; } }
        public bool NavReady { get { return NavRecovered && NavCalibrated; } }
        public bool Has(Item i) { return Inventory.Contains(i); }
        void Notify() { if (Changed != null) Changed(); }

        public void Push(string text, AlertLevel level, bool banner = false)
        {
            Alerts.Add(new Alert { Text = text, Level = level, MinutesLeft = MinutesLeft, Banner = banner });
            if (Alerts.Count > 60) Alerts.RemoveAt(0);
        }

        public static string FormatTime(int minutes)
        {
            if (minutes < 0) minutes = 0;
            return string.Format("{0:00}:{1:00}:00", minutes / 60, minutes % 60);
        }

        public static string FormatCost(int minutes)
        {
            if (minutes < 60) return minutes + " นาที";
            int h = minutes / 60, m = minutes % 60;
            return m == 0 ? h + " ชั่วโมง" : h + " ชม. " + m + " นาที";
        }

        public static string ItemName(Item i)
        {
            switch (i)
            {
                case Item.Multitool: return "Multitool";
                case Item.AccessCard: return "Access Card";
                case Item.SpareFilter: return "Spare Filter";
                case Item.Fuse: return "Fuse";
                case Item.RepairCable: return "Repair Cable";
                case Item.SealantKit: return "Sealant Kit";
                case Item.NavChip: return "Nav Data Chip";
            }
            return i.ToString();
        }

        public static string RoomName(Room r)
        {
            switch (r)
            {
                case Room.Quarters: return "CREW QUARTERS";
                case Room.Command: return "COMMAND DECK";
                case Room.LifeSupport: return "LIFE SUPPORT";
                case Room.Engineering: return "ENGINEERING";
                case Room.Cabin: return "CAPTAIN'S CABIN";
                case Room.CommsRoom: return "COMMS ROOM";
                case Room.Crawlspace: return "SERVICE CRAWLSPACE";
                case Room.PartsStorage: return "PARTS STORAGE";
            }
            return r.ToString();
        }

        /// Inner rooms sit behind a hatch in the back wall of a main room.
        public static bool IsInner(Room r) { return r >= Room.Cabin; }

        /// Main corridor room that an inner room belongs to (main rooms return themselves).
        public static Room ParentOf(Room r)
        {
            switch (r)
            {
                case Room.Cabin: return Room.Quarters;
                case Room.CommsRoom: return Room.Command;
                case Room.Crawlspace: return Room.LifeSupport;
                case Room.PartsStorage: return Room.Engineering;
                default: return r;
            }
        }

        /// <summary>Penalty multiplier for work actions.</summary>
        public float WorkMultiplier
        {
            get
            {
                float m = 1f;
                if (Fatigue >= 80) m *= 1.5f;
                if (Hunger >= 80) m *= 1.25f;
                return m;
            }
        }

        public int CostOf(GameAction a)
        {
            if (a.BaseMinutes <= 0) return 0;
            if (!a.IsWork) return a.BaseMinutes;
            int c = (int)Math.Round(a.BaseMinutes * WorkMultiplier);
            return (c + 14) / 15 * 15; // round up to 15 min
        }

        // ---------------- navigation (free) ----------------
        public string TryEnter(Room r)
        {
            if (IsOver) return "Game over";
            if (ParentOf(r) == Room.Engineering && EngLocked)
                return "ENGINEERING LOCKED — ใช้ Access Card ที่ Security Terminal (Command Deck) เพื่อ Override";
            CurrentRoom = r;
            Notify();
            return null;
        }

        public void ClearSelection() { Selected = null; }

        public void SelectItem(Item? i)
        {
            Selected = (Selected == i) ? null : i;
            Notify();
        }

        // ---------------- inspect (free) ----------------
        public string Describe(string hotspotId)
        {
            switch (hotspotId)
            {
                case "bed": return "เตียงนอนแบบแขวนผนัง. นอน 4 ชั่วโมง ลด Fatigue 50.\nถ้า Fatigue ถึง 100 คุณจะสลบ 6 ชั่วโมงโดยไม่ได้เลือก.";
                case "food": return "ตู้อาหาร — เหลือ Ration " + Rations + " ชุด.\nกิน 15 นาที ลด Hunger 35.";
                case "locker": return LockerOpened ? "Locker ว่างแล้ว." : "Locker ส่วนตัวของ Cmdr. Arin. น่าจะมีอุปกรณ์อยู่ข้างใน.";
                case "recorder": return "เครื่องบันทึกเสียงของลูกเรืออาวุโส. มี 3 บันทึก.";
                case "console": return ObjectivesText();
                case "nav":
                    if (!NavRecovered) return "Navigation Terminal: DATA CORRUPTED.\nต้องใช้ Nav Data Chip (เลือกไอเทมแล้วคลิกที่นี่).";
                    if (!NavCalibrated) return "Navigation Data recovered. ต้อง Calibrate (ต้องมีไฟจาก PDU ที่ซ่อมแล้ว).";
                    return "Navigation: READY. เส้นทาง Saturn Jump Zone และ Mars ARES-7 พร้อมคำนวณ.";
                case "security":
                    if (EngOverridden) return "Security: Engineering override ACTIVE. ห้องเครื่องเปิดถาวร.";
                    if (EngLocked) return "Security: ENGINEERING LOCKDOWN ACTIVE.\nใช้ Access Card ที่นี่เพื่อ Override (1 ชั่วโมง).\nต้องใส่รหัส 4 หลัก — อยู่ในบันทึกเสียงของวิศวกร Mali.";
                    return "Security Terminal. ระบบรักษาความปลอดภัยเสียหาย — อาจล็อกห้องเองได้.\nใช้ Access Card เพื่อ Override ล่วงหน้าได้ (ต้องใช้รหัส 4 หลักจากบันทึกเสียงของวิศวกร Mali).";
                case "comms":
                    if (CommsOpened) return "Communications: สัญญาณจาก Earth ขาดหาย. ช่องเก็บชิปว่างแล้ว.";
                    return "Communications Panel. ฝาครอบบุบติดแน่น — มองเห็น Nav Data Chip อยู่ข้างใน.\nต้องใช้ Multitool งัดเปิด.";
                case "scrubber":
                    return OxygenFixed ? "Oxygen Scrubber: NOMINAL. O2 ลด " + O2DrainFixed + "/ชม."
                        : "Oxygen Scrubber: FILTER CLOGGED. O2 ลด " + O2DrainBroken + "/ชม.!\nต้องใช้ Spare Filter.";
                case "maint":
                    return CabinetOpened ? "Maintenance Cabinet ว่างแล้ว." : "Maintenance Cabinet ล็อกด้วยสกรู. ต้องใช้ Multitool.";
                case "emergency":
                    return string.Format("O2 LEVEL: {0:0.0}%\nCO₂ SCRUBBING: {1}\nDrain: {2}/ชม.\nคาดว่าจะหมดใน ~{3:0} ชั่วโมง.\n\nดูความรู้เพิ่มใน MISSION HANDBOOK > Life Support",
                        Oxygen, OxygenFixed ? "ONLINE" : "OFFLINE", OxygenFixed ? O2DrainFixed : O2DrainBroken,
                        Oxygen / (OxygenFixed ? O2DrainFixed : O2DrainBroken));
                case "pdu":
                    return PowerFixed ? "Power Distribution Unit: ONLINE."
                        : "Power Distribution Unit: FUSE BLOWN. Power ลด " + PwrDrainBroken + "/ชม.\nต้องใช้ Fuse (และต้องมี Multitool เปิดแผง).";
                case "cable":
                    return CableSpliced ? "Cable Junction: ซ่อมแล้ว. Jump Drive รับไฟได้."
                        : "Damaged Cable Junction: สายไฟหลักไป Jump Drive ขาด.\nต้องใช้ Repair Cable.";
                case "propline":
                    if (!LeakSealed) return string.Format("Propellant Line: LEAKING! สูญเสีย {0}/ชม.\nPropellant: {1:0.0}\nต้องใช้ Sealant Kit.", LeakPerHour, Propellant);
                    if (!ReserveTransferred) return string.Format("Propellant Line: SEALED. Propellant {0:0.0}\nถังสำรองยังมีเชื้อเพลิง +30 — ต้องมีไฟ PDU เพื่อปั๊ม.", Propellant);
                    return string.Format("Propellant Line: SEALED. ถังสำรองถูกโอนแล้ว. Propellant {0:0.0}", Propellant);
                case "jump":
                    return JumpStabilized ? "Jump Drive: STABILIZED. พร้อม Jump."
                        : "Jump Drive Console: UNSTABLE.\nต้องการ: PDU online + Cable Junction ซ่อมแล้ว.\nStabilize ใช้ Power 10.";
                case "storage":
                    return StorageOpened ? "Storage Cabinet ว่างแล้ว." : "Storage Cabinet ของฝ่ายวิศวกรรม.";
                case "desk": return "โต๊ะของ Cmdr. Arin. มีรูปถ่ายครอบครัวกับโน้ตลายมือ:\n\"ถ้าเกิดเหตุ — หยุดการรั่วก่อน แล้วค่อยฟื้นไฟ. อากาศยังพอให้คิดได้.\"";
                case "viewport": return "หน้าต่างกลม. วงแหวนดาวเสาร์ยังเป็นจุดเล็กๆ ไกลออกไป.\nเศษซากของยานลอยผ่านช้าๆ — นี่คือสิ่งที่ชนเรา.";
                case "signal": return "Signal Log: ข้อความสุดท้ายจาก Mission Control\n\"Jump Window เปิดถึง T-00:00. ถ้าเชื้อเพลิงไม่พอ ให้ถอยไป ARES-7 บนดาวอังคาร.\"";
                case "duct": return OxygenFixed ? "ท่อลมเงียบแล้ว. อากาศไหลปกติ." : "ท่อลมมีเสียงหวีดเบาๆ. อากาศข้างในหนาและอุ่น — Scrubber กรองไม่ทันแล้ว.";
                case "rack": return "ชั้นอะไหล่ส่วนใหญ่พังจากแรงกระแทก. ที่เหลือใช้ได้อยู่ใน Storage Cabinet.";
            }
            return "";
        }

        // ---------------- actions list (free to open) ----------------
        public List<GameAction> ActionsFor(string id)
        {
            var list = new List<GameAction>();
            switch (id)
            {
                case "bed":
                    list.Add(new GameAction { Id = "sleep", Title = "SLEEP", BaseMinutes = 240,
                        Summary = "Fatigue -50 (Hunger, O2, Power ยังลดตามเวลา)",
                        Blocker = Fatigue < 15 ? "ยังไม่ง่วง (Fatigue < 15)" : null,
                        Effect = m => { m.Fatigue = Math.Max(0, m.Fatigue - 50); m.Push("นอนหลับ 4 ชั่วโมง. Fatigue ลดลง.", AlertLevel.Good); } });
                    break;
                case "food":
                    list.Add(new GameAction { Id = "eat", Title = "EAT RATION", BaseMinutes = 15,
                        Summary = "Hunger -35, Ration -1",
                        Blocker = Rations <= 0 ? "Ration หมดแล้ว" : null,
                        Effect = m => { m.Hunger = Math.Max(0, m.Hunger - 35); m.Rations--; m.Push("กินอาหาร. Hunger ลดลง. เหลือ Ration " + m.Rations, AlertLevel.Good); } });
                    break;
                case "locker":
                    if (!LockerOpened)
                        list.Add(new GameAction { Id = "locker", Title = "COLLECT ITEMS", BaseMinutes = 0,
                            Effect = m => { m.LockerOpened = true; m.Give(Item.Multitool); m.Give(Item.AccessCard); } });
                    break;
                case "recorder":
                    for (int i = 0; i < CrewLogs.Length; i++)
                    {
                        int idx = i;
                        list.Add(new GameAction { Id = "log" + i, Title = "PLAY LOG " + (i + 1), BaseMinutes = 0,
                            Effect = m => { m.LogsRead = Math.Max(m.LogsRead, idx + 1); } });
                    }
                    break;
                case "console":
                    list.Add(new GameAction { Id = "prep", Title = "FINAL PREPARATION", BaseMinutes = 120, IsWork = true,
                        Summary = "ล็อกพิกัด ตรวจระบบทั้งหมด ปลดล็อกการเลือกเส้นทาง",
                        Blocker = FinalPrepped ? "เตรียมพร้อมแล้ว" : (!NavReady ? "ต้อง Navigation READY ก่อน (Recover + Calibrate)" : null),
                        Effect = m => { m.FinalPrepped = true; m.Push("Final Preparation เสร็จ. เลือกเส้นทางที่ Main Console.", AlertLevel.Good, true); } });
                    list.Add(new GameAction { Id = "jumpgo", Title = "ATTEMPT JUMP → SATURN", BaseMinutes = 60,
                        Summary = "เปิด Jump Drive เดินทางสู่ดาวเคราะห์ดวงใหม่ (จบเกม)",
                        Blocker = JumpBlocker(),
                        Effect = m => m.Finish(Ending.Jump) });
                    list.Add(new GameAction { Id = "marsgo", Title = "MARS FALLBACK → ARES-7", BaseMinutes = 60,
                        Summary = "ยกเลิกภารกิจเดิม ลงจอดฉุกเฉินที่สถานี ARES-7 (จบเกม)",
                        Blocker = MarsBlocker(),
                        Effect = m => m.Finish(Ending.Mars) });
                    break;
                case "nav":
                    if (NavRecovered && !NavCalibrated)
                        list.Add(new GameAction { Id = "calibrate", Title = "CALIBRATE NAVIGATION", BaseMinutes = 180, IsWork = true,
                            Summary = "Navigation READY, Power -5",
                            Blocker = !PowerFixed ? "ต้องซ่อม Power Distribution Unit ก่อน" : null,
                            Effect = m => { m.NavCalibrated = true; m.Power -= 5; m.Push("Navigation calibrated — NAV READY.", AlertLevel.Good); } });
                    break;
                case "propline":
                    if (LeakSealed && !ReserveTransferred)
                        list.Add(new GameAction { Id = "transfer", Title = "TRANSFER RESERVE TANK", BaseMinutes = 120, IsWork = true,
                            Summary = "Propellant +30, Power -4",
                            Blocker = !PowerFixed ? "ปั๊มต้องใช้ไฟ — ซ่อม PDU ก่อน" : null,
                            Effect = m => { m.ReserveTransferred = true; m.Propellant += 30; m.Power -= 4; m.Push("โอนเชื้อเพลิงสำรองแล้ว. Propellant +30.", AlertLevel.Good); } });
                    break;
                case "jump":
                    if (!JumpStabilized)
                        list.Add(new GameAction { Id = "stabilize", Title = "STABILIZE JUMP DRIVE", BaseMinutes = 300, IsWork = true,
                            Summary = "Jump Drive พร้อมใช้, Power -10",
                            Blocker = !PowerFixed ? "ต้องซ่อม PDU ก่อน" : (!CableSpliced ? "ต้องซ่อม Cable Junction ก่อน" : null),
                            Effect = m => { m.JumpStabilized = true; m.Power -= 10; m.Push("Jump Drive STABILIZED.", AlertLevel.Good, true); } });
                    break;
                case "storage":
                    if (!StorageOpened)
                        list.Add(new GameAction { Id = "storage", Title = "COLLECT ITEMS", BaseMinutes = 0,
                            Effect = m => { m.StorageOpened = true; m.Give(Item.Fuse); m.Give(Item.RepairCable); m.Give(Item.SealantKit); } });
                    break;
            }
            return list;
        }

        /// <summary>Text hint of which item a hotspot needs (shown in panel).</summary>
        public string ItemHint(string id)
        {
            switch (id)
            {
                case "comms": return CommsOpened ? null : "Multitool";
                case "nav": return NavRecovered ? null : "Nav Data Chip";
                case "security": return EngOverridden ? null : "Access Card";
                case "scrubber": return OxygenFixed ? null : "Spare Filter";
                case "maint": return CabinetOpened ? null : "Multitool";
                case "pdu": return PowerFixed ? null : "Fuse";
                case "cable": return CableSpliced ? null : "Repair Cable";
                case "propline": return LeakSealed ? null : "Sealant Kit";
            }
            return null;
        }

        public class UseResult { public string Error; public GameAction Action; }

        /// <summary>Selected item used on hotspot. Returns action to confirm, or an error. Never consumes on error.</summary>
        public UseResult UseItem(string id, Item item)
        {
            var r = new UseResult();
            GameAction a = null;
            switch (id)
            {
                case "comms":
                    if (item == Item.Multitool && !CommsOpened)
                        a = new GameAction { Id = "comms", Title = "PRY OPEN COMMS PANEL", BaseMinutes = 0,
                            Effect = m => { m.CommsOpened = true; m.Give(Item.NavChip); } };
                    break;
                case "maint":
                    if (item == Item.Multitool && !CabinetOpened)
                        a = new GameAction { Id = "maint", Title = "UNSCREW CABINET", BaseMinutes = 0,
                            Effect = m => { m.CabinetOpened = true; m.Give(Item.SpareFilter); } };
                    break;
                case "nav":
                    if (item == Item.NavChip && !NavRecovered)
                        a = new GameAction { Id = "recover", Title = "RECOVER NAVIGATION DATA", BaseMinutes = 120, IsWork = true,
                            Summary = "Navigation data restored (ใช้ Nav Data Chip)", Consumes = Item.NavChip,
                            Effect = m => { m.NavRecovered = true; m.Push("Navigation data recovered. ต่อไป: Calibrate.", AlertLevel.Good); } };
                    break;
                case "security":
                    if (item == Item.AccessCard && !EngOverridden)
                        a = new GameAction { Id = "override", Title = "OVERRIDE ENGINEERING LOCKDOWN", BaseMinutes = 60,
                            Summary = "Engineering เปิดถาวร, Power -2",
                            Effect = m => { m.EngOverridden = true; m.EngLocked = false; m.Power -= 2; m.Push("SECURITY OVERRIDE — Engineering unlocked.", AlertLevel.Good, true); } };
                    break;
                case "scrubber":
                    if (item == Item.SpareFilter && !OxygenFixed)
                        a = new GameAction { Id = "o2", Title = "REPAIR OXYGEN SCRUBBER", BaseMinutes = 240, IsWork = true,
                            Summary = "O2 +10, O2 drain " + O2DrainBroken + " → " + O2DrainFixed + "/ชม.", Consumes = Item.SpareFilter,
                            Effect = m => { m.OxygenFixed = true; m.Oxygen += 10; m.Push("Oxygen Scrubber ONLINE.", AlertLevel.Good, true); } };
                    break;
                case "pdu":
                    if (item == Item.Fuse && !PowerFixed)
                    {
                        if (!Has(Item.Multitool)) { r.Error = "ต้องมี Multitool เพื่อเปิดแผง PDU ก่อน"; return r; }
                        a = new GameAction { Id = "power", Title = "RESTORE POWER DISTRIBUTION", BaseMinutes = 240, IsWork = true,
                            Summary = "Power +15, Power drain " + PwrDrainBroken + " → " + PwrDrainFixed + "/ชม.", Consumes = Item.Fuse,
                            Effect = m => { m.PowerFixed = true; m.Power += 15; m.Push("Power Distribution ONLINE.", AlertLevel.Good, true); } };
                    }
                    break;
                case "cable":
                    if (item == Item.RepairCable && !CableSpliced)
                        a = new GameAction { Id = "cable", Title = "SPLICE CABLE JUNCTION", BaseMinutes = 120, IsWork = true,
                            Summary = "เชื่อมสายไฟหลักไป Jump Drive", Consumes = Item.RepairCable,
                            Effect = m => { m.CableSpliced = true; m.Push("Cable Junction repaired.", AlertLevel.Good); } };
                    break;
                case "propline":
                    if (item == Item.SealantKit && !LeakSealed)
                        a = new GameAction { Id = "seal", Title = "SEAL PROPELLANT LEAK", BaseMinutes = 180, IsWork = true,
                            Summary = "หยุดการรั่วของเชื้อเพลิง", Consumes = Item.SealantKit,
                            Effect = m => { m.LeakSealed = true; m.Push("Propellant leak SEALED.", AlertLevel.Good, true); } };
                    break;
            }
            if (a == null) r.Error = ItemName(item) + " ใช้กับสิ่งนี้ไม่ได้.";
            else { a.FromItem = true; r.Action = a; }
            return r;
        }

        void Give(Item i)
        {
            if (!Inventory.Contains(i)) Inventory.Add(i);
            Push("ได้รับ: " + ItemName(i), AlertLevel.Good);
        }

        // ---------------- perform ----------------
        /// <summary>Run an action. Time passes first; effect applies only if still alive.</summary>
        public bool Perform(GameAction a)
        {
            if (IsOver || a == null) return false;
            if (a.Blocker != null) { Push(a.Blocker, AlertLevel.Warning); Notify(); return false; }
            if (a.Consumes.HasValue && !Has(a.Consumes.Value)) return false; // stale action, item already used
            int cost = CostOf(a);
            if (cost > 0)
            {
                ActionsTaken++;
                bool sleeping = a.Id == "sleep";
                Advance(cost, sleeping);
                if (IsOver) { Notify(); return true; }
            }
            if (a.Consumes.HasValue) { Inventory.Remove(a.Consumes.Value); if (Selected == a.Consumes) Selected = null; }
            if (a.Effect != null) a.Effect(this);
            Clamp();
            if (!IsOver) CheckFailure();
            if (!IsOver && cost > 0) CheckCollapse();
            Notify();
            return true;
        }

        /// Failed repair mini-game: time is lost, nothing is repaired.
        public void ApplyMistake(int minutes, string what)
        {
            if (IsOver) return;
            Push("ซ่อมพลาด: " + what + " — เสียเวลา " + FormatCost(minutes), AlertLevel.Warning);
            Advance(minutes, false);
            if (!IsOver) CheckCollapse();
            Notify();
        }

        /// Perfect mini-game: small resource bonus.
        public void ApplyPerfectBonus(string actionId)
        {
            if (IsOver) return;
            string txt = null;
            switch (actionId)
            {
                case "o2": Oxygen += 3; txt = "O2 +3"; break;
                case "power": Power += 3; txt = "Power +3"; break;
                case "cable": Power += 2; txt = "Power +2"; break;
                case "seal": Propellant += 2; txt = "Propellant +2"; break;
                case "calibrate": Power += 2; txt = "Power +2"; break;
                case "stabilize": Power += 3; txt = "Power +3"; break;
            }
            Clamp();
            if (txt != null) Push("PERFECT REPAIR! " + txt, AlertLevel.Good);
            Notify();
        }

        void Clamp()
        {
            Oxygen = Math.Min(100, Math.Max(0, Oxygen));
            Power = Math.Min(100, Math.Max(0, Power));
            Propellant = Math.Min(100, Math.Max(0, Propellant));
            Hunger = Math.Min(100, Math.Max(0, Hunger));
            Fatigue = Math.Min(100, Math.Max(0, Fatigue));
        }

        /// <summary>Time passes in 15-minute ticks so events and failures fire at the right moment.</summary>
        void Advance(int minutes, bool sleeping)
        {
            int remaining = minutes;
            while (remaining > 0 && !IsOver)
            {
                int step = Math.Min(15, remaining);
                remaining -= step;
                float h = step / 60f;
                int before = MinutesLeft;
                MinutesLeft -= step;
                Oxygen -= (OxygenFixed ? O2DrainFixed : O2DrainBroken) * h;
                Power -= (PowerFixed ? PwrDrainFixed : PwrDrainBroken) * h;
                if (!LeakSealed) Propellant -= LeakPerHour * h;
                Hunger += HungerPerHour * h;
                if (!sleeping) Fatigue += FatiguePerHour * h;
                Clamp();
                RunEvents(before, MinutesLeft);
                CheckFailure();
            }
        }

        void CheckFailure()
        {
            if (IsOver) return;
            if (Oxygen <= 0) Finish(Ending.FailOxygen);
            else if (Power <= 0) Finish(Ending.FailPower);
            else if (MinutesLeft <= 0) Finish(Ending.FailTime);
        }

        void CheckCollapse()
        {
            if (Fatigue >= 100)
            {
                Push("คุณหมดแรงและสลบไป 6 ชั่วโมง!", AlertLevel.Critical, true);
                Advance(360, true);
                if (!IsOver) { Fatigue = 45; Push("ฟื้นขึ้นมาแล้ว. อย่าปล่อยให้ Fatigue ถึง 100 อีก.", AlertLevel.Warning); }
            }
        }

        bool Crossed(int before, int after, int hoursLeft) { return before > hoursLeft * 60 && after <= hoursLeft * 60; }

        bool Once(string key) { return firedEvents.Add(key); }

        void RunEvents(int before, int after)
        {
            if (Crossed(before, after, 66) && Once("comms"))
                Push("COMMS: สัญญาณจาก Mission Control ขาดหาย. คุณต้องตัดสินใจเอง.", AlertLevel.Info);
            if (Crossed(before, after, 60) && Once("engwarn") && !EngOverridden)
            {
                EngWarned = true;
                Push("SECURITY ALERT — ENGINEERING LOCKDOWN IN 04:00:00. เก็บของใน Engineering ให้ครบ หรือ Override ล่วงหน้าที่ Security Terminal.", AlertLevel.Critical, true);
            }
            if (Crossed(before, after, 58) && Once("engwarn2") && !EngOverridden)
                Push("SECURITY: Engineering lockdown in 02:00:00", AlertLevel.Warning);
            if (Crossed(before, after, 56) && Once("englock") && !EngOverridden)
            {
                EngLocked = true;
                if (ParentOf(CurrentRoom) == Room.Engineering) CurrentRoom = Room.Command;
                Push("ENGINEERING LOCKDOWN ACTIVE. ใช้ Access Card ที่ Security Terminal เพื่อ Override (1 ชม.).", AlertLevel.Critical, true);
            }
            if (Crossed(before, after, 48) && Once("meteor"))
            {
                if (!LeakSealed) { Propellant -= 5; Push("MICROMETEOROID STRIKE! ท่อเชื้อเพลิงที่ยังรั่วแตกเพิ่ม — Propellant -5.", AlertLevel.Critical, true); }
                else Push("MICROMETEOROID STRIKE! ท่อที่ซีลแล้วรับแรงกระแทกไว้ได้.", AlertLevel.Warning, true);
            }
            if (Crossed(before, after, 40) && Once("navwarn"))
                Push(NavReady ? "NAV: Jump window stable." : "NAVIGATION WARNING: หน้าต่างเส้นทางกำลังเลื่อน — ต้อง Recover + Calibrate Navigation ให้ทัน.", AlertLevel.Warning, !NavReady);
            if (Crossed(before, after, 30) && Once("solar"))
            {
                if (!PowerFixed) { Power -= 6; Push("SOLAR PARTICLE EVENT! ระบบไฟที่ยังเสียโดนกระแทก — Power -6.", AlertLevel.Critical, true); }
                else Push("SOLAR PARTICLE EVENT — PDU ที่ซ่อมแล้วตัดวงจรป้องกันไว้ได้.", AlertLevel.Warning, true);
            }
            if (Crossed(before, after, 24) && Once("final"))
                Push("FINAL FLIGHT PROTOCOL: เหลือ 24 ชม. ทำ Final Preparation แล้วเลือกเส้นทางที่ Main Console.", AlertLevel.Critical, true);
            if (Crossed(before, after, 12) && Once("emergency"))
            {
                if (EngLocked) { EngLocked = false; EngOverridden = true; }
                Push("EMERGENCY FINAL ACCESS: ทุกห้องปลดล็อก. เหลือ 12 ชั่วโมงสุดท้าย!", AlertLevel.Critical, true);
            }
            // low-resource alerts (once each)
            if (Oxygen < 25 && Once("lowo2")) Push("LOW OXYGEN (<25%)! ซ่อม Oxygen Scrubber ด่วน.", AlertLevel.Critical, true);
            if (Power < 25 && Once("lowpwr")) Push("LOW POWER (<25%)! ซ่อม PDU ด่วน.", AlertLevel.Critical, true);
            if (Hunger >= 75 && Once("hungry" + Rations)) Push("HUNGER สูง — ถ้าถึง 80 การซ่อมจะช้าลง 25%.", AlertLevel.Warning);
            if (Fatigue >= 75 && Once("tired" + (int)(MinutesLeft / 600))) Push("FATIGUE สูง — ถ้าถึง 80 งานช้าลง 50%, ถึง 100 จะสลบ.", AlertLevel.Warning);
        }

        // ---------------- endings ----------------
        public string JumpBlocker()
        {
            var r = new List<string>();
            if (!FinalPrepped) r.Add("Final Preparation");
            if (!JumpStabilized) r.Add("Jump Drive Stabilized");
            if (!NavReady) r.Add("Navigation READY");
            if (Power < 35) r.Add(string.Format("Power ≥ 35 (now {0:0})", Power));
            if (Propellant < 65) r.Add(string.Format("Propellant ≥ 65 (now {0:0})", Propellant));
            if (MinutesLeft < 60) r.Add("เวลา ≥ 1 ชม.");
            return r.Count == 0 ? null : "ต้องการ: " + string.Join(", ", r.ToArray());
        }

        public string MarsBlocker()
        {
            var r = new List<string>();
            if (!FinalPrepped) r.Add("Final Preparation");
            if (!NavReady) r.Add("Navigation READY");
            if (Power < 20) r.Add(string.Format("Power ≥ 20 (now {0:0})", Power));
            if (Propellant < 30) r.Add(string.Format("Propellant ≥ 30 (now {0:0})", Propellant));
            if (MinutesLeft < 60) r.Add("เวลา ≥ 1 ชม.");
            return r.Count == 0 ? null : "ต้องการ: " + string.Join(", ", r.ToArray());
        }

        public void Finish(Ending e)
        {
            if (IsOver) return;
            Ending = e;
        }

        public bool IsSuccess { get { return Ending == Ending.Jump || Ending == Ending.Mars; } }

        /// <summary>Mission grade 1..3 stars for replay value.</summary>
        public int Stars
        {
            get
            {
                if (!IsSuccess) return 0;
                int s = Ending == Ending.Jump ? 2 : 1;
                if (MinutesLeft >= 30 * 60) s++;
                return s;
            }
        }

        public ObjectiveState Obj(bool done, bool started) { return done ? ObjectiveState.Completed : (started ? ObjectiveState.InProgress : ObjectiveState.NotStarted); }

        public string ObjectivesText()
        {
            Func<ObjectiveState, string> s = st => st == ObjectiveState.Completed ? "<color=#5AD975>[COMPLETED]</color>  " : st == ObjectiveState.InProgress ? "<color=#FFBF40>[IN PROGRESS]</color>" : "<color=#94A3B8>[NOT STARTED]</color>";
            var lines = new List<string>();
            lines.Add("MISSION OBJECTIVES");
            lines.Add(s(Obj(OxygenFixed, Has(Item.SpareFilter))) + " 1. Repair Oxygen Scrubber");
            lines.Add(s(Obj(PowerFixed, Has(Item.Fuse))) + " 2. Restore Power Distribution");
            lines.Add(s(Obj(LeakSealed, Has(Item.SealantKit))) + " 3. Seal Propellant Leak");
            lines.Add(s(Obj(NavRecovered, Has(Item.NavChip))) + " 4. Recover Navigation Data");
            lines.Add(s(Obj(NavCalibrated, NavRecovered)) + " 5. Calibrate Navigation");
            lines.Add(s(Obj(JumpStabilized, CableSpliced)) + " 6. Stabilize Jump Drive (THE JUMP only)");
            lines.Add(s(Obj(false, FinalPrepped)) + " 7. Final Route Selection");
            lines.Add("");
            lines.Add(string.Format("Optional: Splice Cable {0} | Reserve Tank {1}", CableSpliced ? "✓" : "–", ReserveTransferred ? "✓" : "–"));
            lines.Add("");
            lines.Add("THE JUMP: " + (JumpBlocker() ?? "READY"));
            lines.Add("MARS: " + (MarsBlocker() ?? "READY"));
            return string.Join("\n", lines.ToArray());
        }

        /// One-line "what to do next" tracker shown under the HUD.
        public string NextHint()
        {
            if (IsOver) return "";
            if (Fatigue >= 75) return "พักผ่อน: นอนที่ Bunk Bed (Crew Quarters) ก่อนสลบ";
            if (Hunger >= 75 && Rations > 0) return "หิวมาก: กินอาหารที่ Food Storage (Crew Quarters)";
            if (!LockerOpened) return "เปิด Crew Locker เก็บ Multitool + Access Card";
            bool needEng = !StorageOpened || !LeakSealed || !PowerFixed;
            if (needEng && EngLocked) return LogsRead >= 2 ? "Engineering ถูกล็อก: ใช้ Access Card ที่ Security Terminal (Command Deck) แล้วใส่รหัส" : "Engineering ถูกล็อก: หารหัส Override ในบันทึกเสียง (Captain's Cabin หลัง Crew Quarters) แล้วไป Security Terminal";
            if (!StorageOpened) return "ไป Parts Storage (ประตูหลัง Engineering): เก็บของใน Storage Cabinet ก่อนห้องจะล็อก!";
            if (!LeakSealed) return "Engineering: ใช้ Sealant Kit กับ Propellant Line — เชื้อเพลิงกำลังรั่ว";
            if (!PowerFixed) return "Engineering: ใช้ Fuse กับ Power Distribution Unit";
            if (!OxygenFixed) return CabinetOpened ? "Life Support: ใช้ Spare Filter กับ Oxygen Scrubber" : "Service Crawlspace (ประตูหลัง Life Support): ใช้ Multitool เปิด Maintenance Cabinet";
            if (!NavRecovered) return CommsOpened ? "Command Deck: ใช้ Nav Data Chip กับ Navigation Terminal" : "Comms Room (ประตูหลัง Command Deck): ใช้ Multitool งัด Comms Panel เอา Nav Data Chip";
            if (!NavCalibrated) return "Command Deck: Calibrate Navigation";
            if (!ReserveTransferred) return "Engineering: Transfer Reserve Tank (+30 Propellant) ที่ Propellant Line";
            if (!CableSpliced && Propellant >= 65) return "สำหรับ THE JUMP: ใช้ Repair Cable กับ Cable Junction (Engineering)";
            if (!JumpStabilized && CableSpliced && Propellant >= 65) return "สำหรับ THE JUMP: Stabilize Jump Drive (Engineering)";
            if (!FinalPrepped) return "Main Console (Command Deck): Final Preparation";
            return "Main Console: เลือกเส้นทาง — THE JUMP หรือ MARS FALLBACK";
        }

        /// Hotspot id that NextHint() points at (for the on-screen guide marker). Mirrors NextHint order.
        public string NextTargetId()
        {
            if (IsOver) return null;
            if (Fatigue >= 75) return "bed";
            if (Hunger >= 75 && Rations > 0) return "food";
            if (!LockerOpened) return "locker";
            bool needEng = !StorageOpened || !LeakSealed || !PowerFixed;
            if (needEng && EngLocked) return LogsRead >= 2 ? "security" : "recorder";
            if (!StorageOpened) return "storage";
            if (!LeakSealed) return "propline";
            if (!PowerFixed) return "pdu";
            if (!OxygenFixed) return CabinetOpened ? "scrubber" : "maint";
            if (!NavRecovered) return CommsOpened ? "nav" : "comms";
            if (!NavCalibrated) return "nav";
            if (!ReserveTransferred) return "propline";
            if (!CableSpliced && Propellant >= 65) return "cable";
            if (!JumpStabilized && CableSpliced && Propellant >= 65) return "jump";
            return "console";
        }

        public string EndingTitle()
        {
            switch (Ending)
            {
                case Ending.Jump: return "ENDING A — THE JUMP";
                case Ending.Mars: return "ENDING B — MARS FALLBACK";
                case Ending.FailTime: return "MISSION FAILED — TIME EXPIRED";
                case Ending.FailOxygen: return "MISSION FAILED — OXYGEN DEPLETED";
                case Ending.FailPower: return "MISSION FAILED — TOTAL POWER LOSS";
            }
            return "";
        }

        public string EndingText()
        {
            switch (Ending)
            {
                case Ending.Jump:
                    return "Jump Drive ส่องสว่างเหนือวงแหวนดาวเสาร์. ASTRA-01 พุ่งเข้าสู่เส้นทาง Jump มุ่งหน้าสู่ดาวเคราะห์ดวงใหม่.\nนักบินฝึกหัดคนหนึ่งพาความหวังของมนุษยชาติไปต่อได้สำเร็จ.";
                case Ending.Mars:
                    return "คุณยกเลิกเส้นทางเดิมและนำ ASTRA-01 ลงจอดฉุกเฉินใกล้สถานี ARES-7 บนดาวอังคาร.\nภารกิจเปลี่ยนไป แต่คุณรอด — และข้อมูลของยานจะช่วยภารกิจถัดไป. (Successful Ending)";
                case Ending.FailTime:
                    return "หน้าต่างการเดินทางปิดลง. ยานลอยเคว้งในอวกาศโดยไม่มีเส้นทางกลับ.";
                case Ending.FailOxygen:
                    return "Oxygen Scrubber ไม่ได้รับการซ่อมทัน. CO₂ สะสมจนระบบช่วยชีวิตล่ม.\nTip: ซ่อม Oxygen ก่อนสิ่งอื่น.";
                case Ending.FailPower:
                    return "ระบบไฟฟ้าดับทั้งยาน. ไม่มีไฟสำหรับระบบช่วยชีวิตและนำทาง.\nTip: ซ่อม PDU ให้เร็ว และระวังงานที่กินไฟ.";
            }
            return "";
        }

        public static readonly string[] CrewLogs = new string[]
        {
            "LOG 1 — Cmdr. Arin Phrommin\n\"ถ้าเธอได้ยินบันทึกนี้ แปลว่าฉันไปไม่ถึงห้องควบคุม. Multitool กับ Access Card อยู่ใน Locker ของฉัน.\nสิ่งแรก: ออกซิเจน. Filter สำรองอยู่ใน Maintenance Cabinet ที่ Life Support.\"",
            "LOG 2 — Eng. Mali Kongsak\n\"ระบบ Security เสียหลังชน — มันจะล็อก Engineering เองในอีกไม่กี่สิบชั่วโมง.\nFuse, สายไฟ, Sealant อยู่ใน Storage Cabinet ห้องเครื่อง. ซีลท่อเชื้อเพลิงให้เร็ว — ทุกชั่วโมงที่รั่วคือโอกาส Jump ที่หายไป.\nถ้าห้องเครื่องล็อก รหัส Override คือ 4-7-1-9. จำไว้นะ.\"",
            "LOG 3 — Cmdr. Arin Phrommin\n\"Jump ต้องใช้ Propellant อย่างน้อย 65. ถังสำรองมีอีก 30 แต่ต้องมีไฟปั๊ม.\nถ้าไม่ทัน... ARES-7 บนดาวอังคารคือทางรอด. ไม่มีใครโทษเธอหรอก. แค่กลับไปให้ได้.\""
        };

        // ---------------- layout ----------------
        static string ShortRoom(Room r)
        {
            switch (r)
            {
                case Room.Quarters: return "CREW\nQUARTERS";
                case Room.Command: return "COMMAND\nDECK";
                case Room.LifeSupport: return "LIFE\nSUPPORT";
                case Room.Engineering: return "ENGINE\nROOM";
                case Room.Cabin: return "CAPTAIN'S\nCABIN";
                case Room.CommsRoom: return "COMMS\nROOM";
                case Room.Crawlspace: return "SERVICE\nCRAWLSPACE";
                default: return "PARTS\nSTORAGE";
            }
        }

        /// Hatch in the back wall of a main room, leading to its inner room (pre-remap coords).
        static void AddHatch(List<Hotspot> l, Room from, Room to, float x)
        {
            l.Add(new Hotspot
            {
                Id = "hatch_" + to, Room = from, DoorTo = to,
                Name = "▲\n" + ShortRoom(to),
                X = x, Y = 0.28f, W = 0.10f, H = 0.42f
            });
        }

        /// Way back out of an inner room (left edge, like the corridor doors).
        static void AddExit(List<Hotspot> l, Room from)
        {
            var to = ParentOf(from);
            l.Add(new Hotspot
            {
                Id = "exit_" + from, Room = from, DoorTo = to,
                Name = "▼ BACK\n" + ShortRoom(to),
                X = 0.004f, Y = 0.07f, W = 0.064f, H = 0.56f
            });
        }

        static void AddDoor(List<Hotspot> l, Room from, Room to, bool left)
        {
            l.Add(new Hotspot
            {
                Id = "door_" + from + "_" + to, Room = from, DoorTo = to,
                Name = (left ? "◀\n" : "▶\n") + ShortRoom(to),
                X = left ? 0.004f : 0.932f, Y = 0.07f, W = 0.064f, H = 0.56f
            });
        }

        static List<Hotspot> BuildHotspots()
        {
            var l = new List<Hotspot>();
            // Crew Quarters
            l.Add(new Hotspot { Id = "bed", Room = Room.Quarters, Name = "Bunk Bed", X = 0.05f, Y = 0.28f, W = 0.30f, H = 0.18f });
            l.Add(new Hotspot { Id = "food", Room = Room.Quarters, Name = "Food Storage", X = 0.40f, Y = 0.28f, W = 0.14f, H = 0.40f });
            l.Add(new Hotspot { Id = "locker", Room = Room.Quarters, Name = "Crew Locker", X = 0.58f, Y = 0.28f, W = 0.12f, H = 0.52f });
            AddHatch(l, Room.Quarters, Room.Cabin, 0.79f);
            // Command Deck
            l.Add(new Hotspot { Id = "console", Room = Room.Command, Name = "Main Console", X = 0.41f, Y = 0.28f, W = 0.28f, H = 0.22f });
            AddHatch(l, Room.Command, Room.CommsRoom, 0.29f);
            l.Add(new Hotspot { Id = "nav", Room = Room.Command, Name = "Navigation Terminal", X = 0.05f, Y = 0.28f, W = 0.22f, H = 0.34f });
            l.Add(new Hotspot { Id = "security", Room = Room.Command, Name = "Security Terminal", X = 0.73f, Y = 0.28f, W = 0.22f, H = 0.34f });

            // Life Support
            l.Add(new Hotspot { Id = "scrubber", Room = Room.LifeSupport, Name = "Oxygen Scrubber", X = 0.30f, Y = 0.28f, W = 0.36f, H = 0.50f });
            AddHatch(l, Room.LifeSupport, Room.Crawlspace, 0.10f);
            l.Add(new Hotspot { Id = "emergency", Room = Room.LifeSupport, Name = "Emergency Panel", X = 0.74f, Y = 0.45f, W = 0.20f, H = 0.22f });
            // Engineering
            l.Add(new Hotspot { Id = "pdu", Room = Room.Engineering, Name = "Power Distribution Unit", X = 0.04f, Y = 0.28f, W = 0.18f, H = 0.46f });
            l.Add(new Hotspot { Id = "cable", Room = Room.Engineering, Name = "Cable Junction", X = 0.25f, Y = 0.62f, W = 0.18f, H = 0.14f });
            l.Add(new Hotspot { Id = "propline", Room = Room.Engineering, Name = "Propellant Line", X = 0.25f, Y = 0.28f, W = 0.30f, H = 0.16f });
            l.Add(new Hotspot { Id = "jump", Room = Room.Engineering, Name = "Jump Drive Console", X = 0.58f, Y = 0.28f, W = 0.22f, H = 0.40f });
            AddHatch(l, Room.Engineering, Room.PartsStorage, 0.85f);
            // Captain's Cabin (behind Crew Quarters)
            l.Add(new Hotspot { Id = "recorder", Room = Room.Cabin, Name = "Voice Recorder", X = 0.12f, Y = 0.42f, W = 0.22f, H = 0.14f });
            l.Add(new Hotspot { Id = "desk", Room = Room.Cabin, Name = "Captain's Desk", X = 0.42f, Y = 0.28f, W = 0.26f, H = 0.20f });
            l.Add(new Hotspot { Id = "viewport", Room = Room.Cabin, Name = "Viewport", X = 0.76f, Y = 0.36f, W = 0.20f, H = 0.26f });
            // Comms Room (behind Command Deck)
            l.Add(new Hotspot { Id = "comms", Room = Room.CommsRoom, Name = "Comms Panel", X = 0.12f, Y = 0.28f, W = 0.34f, H = 0.40f });
            l.Add(new Hotspot { Id = "signal", Room = Room.CommsRoom, Name = "Signal Log", X = 0.58f, Y = 0.36f, W = 0.30f, H = 0.24f });
            // Service Crawlspace (behind Life Support)
            l.Add(new Hotspot { Id = "maint", Room = Room.Crawlspace, Name = "Maintenance Cabinet", X = 0.14f, Y = 0.28f, W = 0.22f, H = 0.42f });
            l.Add(new Hotspot { Id = "duct", Room = Room.Crawlspace, Name = "Air Duct", X = 0.50f, Y = 0.52f, W = 0.40f, H = 0.18f });
            // Parts Storage (behind Engineering)
            l.Add(new Hotspot { Id = "storage", Room = Room.PartsStorage, Name = "Storage Cabinet", X = 0.14f, Y = 0.28f, W = 0.20f, H = 0.46f });
            l.Add(new Hotspot { Id = "rack", Room = Room.PartsStorage, Name = "Spare Parts Rack", X = 0.46f, Y = 0.28f, W = 0.40f, H = 0.30f });
            // Objects: keep clear of the edge doors and sit on the lowered floor line
            foreach (var h in l)
            {
                h.X = 0.085f + h.X * 0.83f;
                h.W = h.W * 0.83f;
                h.Y = h.Y - 0.08f;
            }
            // Doors: linear corridor  Quarters <-> Command <-> Life Support <-> Engineering
            AddDoor(l, Room.Quarters, Room.Command, false);
            AddDoor(l, Room.Command, Room.Quarters, true);
            AddDoor(l, Room.Command, Room.LifeSupport, false);
            AddDoor(l, Room.LifeSupport, Room.Command, true);
            AddDoor(l, Room.LifeSupport, Room.Engineering, false);
            AddDoor(l, Room.Engineering, Room.LifeSupport, true);
            AddExit(l, Room.Cabin); AddExit(l, Room.CommsRoom); AddExit(l, Room.Crawlspace); AddExit(l, Room.PartsStorage);
            return l;
        }
    }
}
