// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does restoring an abandoned base room by room, with action-timed days, make a better 20-minute game
//           than surviving seven fixed days? (docs/superpowers/specs/2026-11-14-base-restoration-design.md)
// Date: 2026-11-14
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// The abandoned-base chapter in the walkable base: dark rooms, debris, jammed doors and dead machines.
    /// RestoreSim owns the rules (rooms, tasks, shown hours, needs, weather, relay passes); this partial builds the
    /// extra spots, gates the doors, darkens rooms by their state, opens a task panel with the hours shown first,
    /// runs the puzzle, then applies the result. The clock moves only by actions in this mode.
    /// </summary>
    public partial class GameFlow
    {
        RestoreSim _restore;
        /// <summary>Tools you keep (broom, crowbar) and found parts (fuse, cable, filter), carried on the belt.</summary>
        readonly Dictionary<string, int> _belt = new Dictionary<string, int>();
        /// <summary>Set by the classic screenshot/bot routes that still test the 7-day base.</summary>
        public static bool ForceClassicBase;

        bool RestoreMode => WalkBase && !ForceClassicBase && _cfg.restore != null && _cfg.restore.rooms.Count > 0;

        // restore room id → layout room key, and the door tiles that open with the room
        static readonly Dictionary<string, string> RoomKey = new Dictionary<string, string>
        {
            ["lounge"] = "base.room.pod", ["power"] = "base.room.power", ["greenhouse"] = "base.room.greenhouse",
            ["storage"] = "base.room.storage", ["quarters"] = "base.room.quarters", ["comms"] = "base.room.comms",
        };
        static readonly Dictionary<string, Vector2Int[]> RoomDoors = new Dictionary<string, Vector2Int[]>
        {
            ["greenhouse"] = new[] { new Vector2Int(20, 17), new Vector2Int(21, 17) },
            ["quarters"] = new[] { new Vector2Int(29, 11), new Vector2Int(29, 12) },
            ["comms"] = new[] { new Vector2Int(29, 2), new Vector2Int(29, 3) },
            ["storage"] = new[] { new Vector2Int(18, 6), new Vector2Int(19, 6) },
        };

        void RestoreReset()
        {
            _restore = null;
            _belt.Clear();
            _rDark.Clear(); _rDebris.Clear(); _rHeadlamp = null; _rLastState.Clear();
        }

        /// <summary>Called from LbPickPet once the run's seed is known.</summary>
        void RestoreInit(int seed)
        {
            _restore = new RestoreSim(_cfg.restore, seed);
            _belt.Clear();
            _rNight = false; _rSafeMode = false; _rFlareCalled = false; _rFlareDone = false; _rAurora = false; _rPickup = false;
            _ui.Toast(TT("ORBI moved into its backup chip before the ship broke up. It is in your pocket: plug it into the lounge console.", "ORBI ย้ายตัวเองลงชิปสำรองก่อนยานแตก ชิปอยู่ในกระเป๋าเรา เอาไปเสียบที่เครื่องในห้องนั่งเล่น"), GameUI.CCyan);
            DayNote(TT("We docked with an old base. Dark, cold and dusty. ORBI is still on the chip.", "เชื่อมกับฐานเก่าแล้ว มืด หนาว ฝุ่นเต็มไปหมด ORBI ยังอยู่ในชิป"));
        }

        /// <summary>The 7-day base run gets only visitor cards and no day-7 win in restore mode.</summary>
        LifeboatConfig RestoreLifeboatConfig()
        {
            var c = JsonUtility.FromJson<LifeboatConfig>(JsonUtility.ToJson(LC));
            c.cards = c.cards.FindAll(x => !string.IsNullOrEmpty(x.visitor));
            c.daysTotal = 99; c.sliceDays = 0;
            c.nights = new List<LifeNightDef>();   // storms come with the daily weather here, not on a fixed night
            return c;
        }

        // ------------------------------------------------------------------ belt (tools and found parts)

        bool BeltHas(string id) => _belt.TryGetValue(id, out var n) && n > 0;
        void BeltAdd(string id) { _belt[id] = (_belt.TryGetValue(id, out var n) ? n : 0) + 1; }
        void BeltUse(string id) { if (BeltHas(id)) _belt[id]--; }

        List<string> RestoreBag()
        {
            var l = new List<string>(_bag);
            foreach (var kv in _belt) for (int i = 0; i < kv.Value; i++) l.Add(kv.Key);
            return l;
        }

        // ------------------------------------------------------------------ props

        readonly Dictionary<string, SpriteRenderer> _rDebris = new Dictionary<string, SpriteRenderer>();

        /// <summary>Extra spots for restore tasks (called at the end of BBuildProps).</summary>
        void RestoreBuildProps()
        {
            if (!RestoreMode) return;
            void P(string id, string en, string th, Sprite sp, float x, float y, Vector2 stand, int w = 1) => BProp(id, TT(en, th), sp, x, y, stand, w, w > 0 ? 1 : 0);
            // lounge: ORBI's console between the shelves, junk piles to search around the base
            P("orbi", "ORBI console", "เครื่องของ ORBI", Art.Console("orbi", 20, 26, Pal.Cyan), 5.3f, 15.12f, new Vector2(5.3f, 14.4f));
            P("junk_lounge", "Junk pile", "กองของ", ArtRestore.Junk(0), 2.9f, 9.5f, new Vector2(3.6f, 9.6f), 0);
            P("junk_hub", "Junk pile", "กองของ", ArtRestore.Junk(1), 25.6f, 8.6f, new Vector2(25.6f, 9.4f), 0);
            P("junk_power", "Junk pile", "กองของ", ArtRestore.Junk(2), 28.0f, 3.2f, new Vector2(27.3f, 3.2f), 0);
            P("junk_airlock", "Junk pile", "กองของ", ArtRestore.Junk(3), 30.4f, 18.6f, new Vector2(29.6f, 18.8f), 0);
            // power room: generator and breaker box
            // on the bottom wall: the room's door is at x 25-26 on the top wall and must stay clear
            P("generator", "Generator", "เครื่องปั่นไฟ", ArtRestore.Generator(false), 26.4f, 1.12f, new Vector2(26.4f, 2.1f), 2);
            P("breaker_box", "Breaker box", "ตู้เบรกเกอร์", ArtRestore.Breaker(false), 23.4f, 3.2f, new Vector2(24.2f, 3.2f));
            // greenhouse lamp panel, storage door, MOXIE panel, quarters heater and bunks, radio power
            P("grow_lamps", "Grow lamp panel", "แผงไฟปลูกผัก", Art.Console("lamp", 18, 22, Pal.Green), 16.8f, 22.12f, new Vector2(17.4f, 21.4f));
            P("storage_door", "Jammed door", "ประตูที่ติด", ArtRestore.JammedDoor(), 19.0f, 6.0f, new Vector2(19.0f, 7.3f), 0);
            P("air_panel", "Oxygen maker panel", "แผงเครื่องผลิตออกซิเจน", Art.Console("moxp", 18, 22, Pal.Cyan), 19.8f, 16.12f, new Vector2(19.8f, 15.4f));
            P("heater", "Heater", "ฮีตเตอร์", ArtRestore.Heater(false), 35.4f, 13.12f, new Vector2(34.6f, 13.0f));
            P("bunks", "Bunks", "เตียงนอน", Art.Bed(), 32.0f, 14.4f, new Vector2(32.0f, 13.4f), 3);
            P("radio_power", "Radio power", "ไฟเข้าวิทยุ", Art.Console("rpw", 18, 22, Pal.Amber), 33.2f, 5.12f, new Vector2(33.2f, 4.4f));
            P("stargaze", "Viewing spot", "จุดดูดาว", Art.Telescope(), 33.6f, 24.4f, new Vector2(33.0f, 23.8f));
            // dust and debris piles: walk-through, hidden when swept
            foreach (var (id, x, y) in new[] { ("debris_power", 24.6f, 3.6f), ("debris_greenhouse", 21.0f, 19.0f), ("debris_storage", 19.4f, 3.6f),
                                               ("debris_air", 18.6f, 13.6f), ("debris_quarters", 33.6f, 11.6f), ("debris_comms", 33.6f, 2.6f) })
            {
                var sp = BProp(id, TT("Dust and debris", "ฝุ่นและเศษของ"), ArtRestore.Debris(id.Length & 3), x, y, new Vector2(x, y - 0.6f), 0, 0, SortY(y) - 40);
                _rDebris[id] = sp.sr;
            }
        }

        // ------------------------------------------------------------------ per frame: doors, darkness, clock sync

        readonly Dictionary<string, SpriteRenderer> _rDark = new Dictionary<string, SpriteRenderer>();
        readonly Dictionary<string, RoomState> _rLastState = new Dictionary<string, RoomState>();
        SpriteRenderer _rHeadlamp;

        /// <summary>Restore mode: a visitor only knocks once the day's actions have reached late morning.</summary>
        bool VisitorWaiting(LifeCard c) => c != null && !string.IsNullOrEmpty(c.visitor) && RestoreMode && _life != null && _life.State.schedule.Hour < VisitorHour;

        void RestoreUpdate(float dt)
        {
            if (!RestoreMode || _restore == null || _bRoot == null || _bMode != 0) return;
            RestoreSync();
            if (_lbView == LbView.Card && _lbCard != null && !string.IsNullOrEmpty(_lbCard.visitor) && !VisitorWaiting(_lbCard)
                && !_bAnnounced.Contains(_lbCard.id) && !_bOpen && !_ui.Blocking) BOnEvent();
            RestoreFlareUpdate();
            // rooms: doors, darkness and the moment a room comes back to life
            foreach (var r in _cfg.restore.rooms)
            {
                var st = _restore.StateOf(r.id);
                if (RoomDoors.TryGetValue(r.id, out var doors))
                {
                    bool shut = st == RoomState.Locked || r.id == "storage" && !_restore.IsDone("s_pry");
                    foreach (var d in doors) _bSolid[d.x, d.y] = shut;
                }
                if (_rLastState.TryGetValue(r.id, out var was) && was != st) RestoreRoomChanged(r.id, was, st);
                _rLastState[r.id] = st;
            }
            RestoreDarkness(dt);
        }

        /// <summary>Both clocks agree: the restore sim drives the hour, the old schedule follows (and the reverse for old actions).</summary>
        void RestoreSync()
        {
            var sc = _life?.State.schedule;
            if (sc == null) return;
            float d = _restore.Hour - sc.Hour;
            if (d > 0.001f) sc.TrySpend(Mathf.Min(d, sc.Remaining));
            else if (d < -0.001f) _restore.Spend(-d);
        }

        void RestoreDarkness(float dt)
        {
            // one dark veil per room; rooms on the generator (hub, tube, airlock) follow the power room
            foreach (var (rect, key) in _bRooms)
            {
                string id = null;
                foreach (var kv in RoomKey) if (kv.Value == key) id = kv.Key;
                var st = id != null ? _restore.StateOf(id) : _restore.StateOf("power") == RoomState.Restored ? RoomState.Restored : RoomState.Dark;
                if (!_rDark.TryGetValue(key, out var veil) || veil == null)
                {
                    veil = BSprite("room-dark", WorldBuilder.WhitePixel(), new Vector2(rect.x + rect.width / 2f, rect.y + rect.height / 2f), 29975);
                    veil.transform.localScale = new Vector3((rect.width + 0.8f) * 16, (rect.height + 1.6f) * 16, 1);
                    veil.color = new Color(0.01f, 0.01f, 0.03f, 0.8f);
                    _rDark[key] = veil;
                }
                float target = st == RoomState.Restored ? 0f : st == RoomState.Unpowered ? 0.55f + Mathf.Abs(Mathf.Sin(_bAnim * 7f + rect.x)) * 0.12f
                    : st == RoomState.Locked ? 0.86f : 0.72f;
                // a broken tube light flickers now and then in dark rooms
                if (st == RoomState.Dark && Mathf.Sin(_bAnim * 1.3f + rect.y * 3f) > 0.985f) target = 0.35f;
                var c = veil.color; c.a = Mathf.MoveTowards(c.a, target, dt * (target < c.a ? 1.2f : 3f)); veil.color = c;
            }
            // the cadet's headlamp in dark places
            if (_rHeadlamp == null)
            {
                _rHeadlamp = BSprite("headlamp", Art.SoftLight(), _bPos, 29985);
                if (_bGlowMaterial != null) _rHeadlamp.sharedMaterial = _bGlowMaterial;
                _rHeadlamp.transform.localScale = Vector3.one * 3.2f;
            }
            string room = BRoomAt(_bPos);
            bool dark = false;
            if (room != null && _rDark.TryGetValue(room, out var v) && v != null) dark = v.color.a > 0.3f;
            _rHeadlamp.transform.position = new Vector3(_bPos.x, _bPos.y + 0.5f, 0);
            _rHeadlamp.color = new Color(1f, 0.92f, 0.75f, Mathf.MoveTowards(_rHeadlamp.color.a, dark ? 0.55f : 0f, Time.deltaTime * 2f));
        }

        void RestoreRoomChanged(string room, RoomState was, RoomState now)
        {
            if (now == RoomState.Restored && was != RoomState.Unpowered)
            {
                _sfx.Play("good"); _sShake = 0.15f;
                _ui.Toast(TT("Room restored: ", "ฟื้นห้องสำเร็จ: ") + RoomName(room) + TT(" · base ", " · ฐาน ") + Mathf.RoundToInt(_restore.Percent) + "%", GameUI.CGreen);
                DayNote(TT("Restored: ", "ฟื้นห้อง: ") + RoomName(room));
                OrbiSay(RoomRestoredLine(room), GameUI.CGreen);
                // what the room was hiding
                if (room == "storage")
                {
                    _life.ApplyTask(new Stock { food = 3 });
                    _ui.Toast(TT("Sealed ration packs on the storage shelves: food +3. Space food is made to last for years.", "เจอเสบียงแบบซีลบนชั้นห้องเก็บของ: อาหาร +3 อาหารอวกาศถูกทำมาให้เก็บได้หลายปี"), GameUI.CGreen);
                }
                if (room == "greenhouse") { _bSeeds += 3; _ui.Toast(TT("A seed packet in the greenhouse drawer: seeds +3. Plant them in the beds.", "เจอซองเมล็ดในลิ้นชักเรือนกระจก: เมล็ด +3 เอาไปปลูกในแปลง"), GameUI.CGreen); }
            }
            else if (now == RoomState.Restored) _ui.Toast(TT("Power is back in ", "ไฟกลับมาแล้วที่") + RoomName(room), GameUI.CGreen);
            else if (now == RoomState.Dark && was == RoomState.Locked) _ui.Toast(TT("You can get into ", "เข้าไปได้แล้ว: ") + RoomName(room), GameUI.CCyan);
            BRefreshProps();
        }

        string RoomName(string id)
        {
            switch (id)
            {
                case "lounge": return TT("the lounge", "ห้องนั่งเล่น");
                case "power": return TT("the power room", "ห้องไฟฟ้า");
                case "greenhouse": return TT("the greenhouse", "เรือนกระจก");
                case "storage": return TT("the storage room", "ห้องเก็บของ");
                case "air": return TT("the air machine", "เครื่องผลิตอากาศ");
                case "quarters": return TT("the quarters", "ห้องพัก");
                case "comms": return TT("the radio room", "ห้องวิทยุ");
                default: return id;
            }
        }

        string RoomRestoredLine(string room)
        {
            switch (room)
            {
                case "power": return TT("The generator runs! Lights and doors in the hub work again. Pick which room to bring back next.", "เครื่องปั่นไฟทำงานแล้ว! ไฟและประตูในโถงกลับมาแล้ว เลือกเองเลยว่าจะฟื้นห้องไหนต่อ");
                case "greenhouse": return TT("Grow lamps on. Red and blue light, like NASA's Veggie garden. You can plant now.", "ไฟปลูกผักติดแล้ว แสงแดงกับน้ำเงินเหมือนสวน Veggie ของ NASA ปลูกผักได้แล้ว");
                case "storage": return TT("Water recycler running and the toolbox is open. The space station recovers about 98% of its water too.", "เครื่องรีไซเคิลน้ำทำงานแล้ว ตู้เครื่องมือเปิดได้ สถานีอวกาศก็นำน้ำกลับมาใช้ได้ราว 98%");
                case "air": return TT("The oxygen maker is back. It splits oxygen out of Martian CO2, like NASA's MOXIE did.", "เครื่องผลิตออกซิเจนกลับมาแล้ว แยกออกซิเจนจาก CO2 ของดาวอังคาร เหมือน MOXIE ของ NASA");
                case "quarters": return TT("Heater on and beds ready. Real rest at night now.", "ฮีตเตอร์ติด เตียงพร้อม กลางคืนได้พักเต็มที่แล้ว");
                case "comms": return TT("The radio works! Now wait for the relay orbiter to pass overhead and send the call.", "วิทยุใช้ได้แล้ว! รอยานโคจรถ่ายทอดผ่านเหนือฐาน แล้วส่งคำขอความช่วยเหลือ");
                default: return TT("Another part of the base is alive again.", "ฐานกลับมามีชีวิตอีกส่วนแล้ว");
            }
        }

        // ------------------------------------------------------------------ using spots

        RestoreTaskDef TaskDef(string id) => _cfg.restore.tasks.Find(t => t.id == id);

        /// <summary>The open restore task at this spot (reset tasks sit on the room's breaker or main machine).</summary>
        string TaskAtSpot(string spotId)
        {
            if (!RestoreMode || _restore == null) return null;
            foreach (var id in _restore.OpenTasks())
            {
                var t = TaskDef(id);
                string spot = t != null ? t.spot : ResetSpot(id.Substring("reset_".Length));
                if (spot == spotId) return id;
            }
            return null;
        }

        static string ResetSpot(string room) => room == "greenhouse" ? "grow_lamps" : room == "storage" ? "recycler" : room == "air" ? "air_panel" : room == "quarters" ? "heater" : room == "comms" ? "radio_power" : "breaker_box";

        /// <summary>Restore routing for a used spot. True = handled.</summary>
        bool RestoreUse(BSpot s)
        {
            if (!RestoreMode || _restore == null || _bMode != 0) return false;
            string task = TaskAtSpot(s.id);
            if (task != null) { RestoreTaskShow(task); return true; }
            if (s.id == "orbi") { if (_restore.IsDone("chip")) OrbiTerminalShow(); return true; }
            if (RestoreUseLife(s)) return true;
            if (s.id.StartsWith("debris_") || s.id.StartsWith("junk_")) { _ui.Toast(TT("Nothing more here.", "ตรงนี้ไม่มีอะไรแล้ว"), GameUI.CDim); return true; }
            // machines in rooms that are not back yet
            string need = s.id.StartsWith("plot") ? "greenhouse" : s.id == "air" ? "air" : s.id == "toolbox" || s.id == "recycler" ? "storage" : s.id == "radio" ? "comms" : null;
            if (need != null && _restore.StateOf(need) != RoomState.Restored)
            {
                _sfx.Play("bad");
                _ui.Toast(TT("It does not work yet. Restore ", "ยังใช้ไม่ได้ ต้องฟื้น") + RoomName(need) + TT(" first.", "ก่อน"), GameUI.CAmber);
                return true;
            }
            if (s.id == "radio" && _restore.StateOf("comms") == RoomState.Restored) { RestoreRadioShow(); return true; }
            return false;
        }

        string TaskTitle(string id)
        {
            switch (id)
            {
                case "chip": return TT("Plug ORBI's chip into the console", "เสียบชิป ORBI เข้าเครื่อง");
                case "find_lounge": case "find_hub": case "find_power": case "find_airlock": return TT("Search the junk pile", "ค้นกองของ");
                case "p_wire": return TT("Rewire the generator", "ต่อวงจรเครื่องปั่นไฟ");
                case "p_breaker": return TT("Reset the breaker box", "รีเซ็ตตู้เบรกเกอร์");
                case "g_lamps": return TT("Fix the grow lamps", "ซ่อมไฟปลูกผัก");
                case "g_beds": return TT("Turn the soil in the grow beds", "พรวนดินในแปลงปลูก");
                case "s_pry": return TT("Pry open the jammed door", "งัดประตูที่ติดอยู่");
                case "s_pipes": return TT("Reconnect the water recycler pipes", "ต่อท่อเครื่องรีไซเคิลน้ำ");
                case "a_filter": return TT("Change the clogged air filter", "เปลี่ยนไส้กรองอากาศที่ตัน");
                case "a_moxie": return TT("Tune the oxygen maker", "ปรับจูนเครื่องผลิตออกซิเจน");
                case "q_heater": return TT("Wire up the heater", "ต่อสายฮีตเตอร์");
                case "q_beds": return TT("Make the bunks", "จัดเตียงให้นอนได้");
                case "c_power": return TT("Power up the radio", "ต่อไฟเข้าวิทยุ");
                case "c_dish": return TT("Aim the dish at the relay orbit", "ปรับจานให้ชี้ไปทางวงโคจรยานถ่ายทอด");
            }
            if (id.StartsWith("reset_")) return TT("Reset the tripped power: ", "รีเซ็ตไฟที่ตัด: ") + RoomName(id.Substring(6));
            var t = TaskDef(id);
            if (t != null && t.kind == "sweep") return TT("Sweep away the dust and debris", "กวาดฝุ่นและเศษของ");
            return id;
        }

        string TaskFact(RestoreTaskDef t)
        {
            switch (t?.kind)
            {
                case "sweep": return TT("Mars dust is finer than flour and gets into everything. Dust on solar panels ended NASA's InSight mission in 2022.", "ฝุ่นดาวอังคารละเอียดกว่าแป้ง เข้าไปได้ทุกซอก ฝุ่นที่เกาะแผงโซลาร์ทำให้ยาน InSight ของ NASA จบภารกิจในปี 2022");
                case "search": return TT("Astronauts reuse everything: on the space station, old packaging and parts get a second job.", "นักบินอวกาศใช้ของซ้ำทุกอย่าง บนสถานีอวกาศ ของเก่าและชิ้นส่วนเหลือใช้ถูกเอามาใช้งานใหม่");
                case "chip": return TT("Spacecraft computers keep a backup copy of their software so a crew can restart them.", "คอมพิวเตอร์ในยานอวกาศเก็บสำเนาซอฟต์แวร์สำรองไว้ เผื่อลูกเรือต้องเริ่มระบบใหม่");
                case "pry": return TT("Lunar and Mars tools are built for gloved hands: big handles, simple levers.", "เครื่องมือสำหรับดวงจันทร์และดาวอังคารออกแบบให้ใช้กับถุงมือหนา ด้ามใหญ่ คันโยกง่าย ๆ");
                default: return TT("Astronauts train to repair systems themselves: help from Earth is minutes to hours away.", "นักบินอวกาศฝึกซ่อมระบบเอง เพราะความช่วยเหลือจากโลกต้องรอเป็นนาทีถึงหลายชั่วโมง");
            }
        }

        static string Hrs(float h) => (Mathf.Approximately(h, Mathf.Round(h)) ? Mathf.RoundToInt(h).ToString() : h.ToString("0.#"));

        /// <summary>Task panel: what, what it needs, and the hours it takes, shown before you start.</summary>
        void RestoreTaskShow(string id)
        {
            var t = TaskDef(id);
            var bag = RestoreBag();
            bool can = _restore.CanDo(id, bag, out var why);
            float hours = _restore.HoursFor(id);
            _ui.ShowScreen(root =>
            {
                var box = new VisualElement(); box.style.width = 1100;
                box.style.backgroundColor = new Color(0.05f, 0.06f, 0.1f, 0.97f); GameUI.Border(box, GameUI.CAmber, 3, 16);
                box.style.paddingLeft = box.style.paddingRight = 30; box.style.paddingTop = 18; box.style.paddingBottom = 18;
                box.Add(_ui.L(TaskTitle(id), 34, true, GameUI.CAmber));
                box.Add(_ui.L(TT("Takes about ", "ใช้เวลาราว ") + Hrs(hours) + TT(" h", " ชม.") + TT($" · now {(int)_restore.Hour:00}:{(int)(_restore.Hour % 1 * 60):00}, {Hrs(_restore.HoursLeft)} h left today", $" · ตอนนี้ {(int)_restore.Hour:00}:{(int)(_restore.Hour % 1 * 60):00} วันนี้เหลือ {Hrs(_restore.HoursLeft)} ชม."), 24, true, GameUI.CCyan));
                if (t != null && !string.IsNullOrEmpty(t.needItem))
                    box.Add(_ui.L(TT("Needs: ", "ต้องใช้: ") + ItemNameR(t.needItem) + (t.consume ? TT(" (used up)", " (ใช้แล้วหมด)") : TT(" (you keep it)", " (ใช้ได้เรื่อย ๆ)")), 21, false, bag.Contains(t.needItem) ? GameUI.CGreen : GameUI.CRed));
                if (t != null && !string.IsNullOrEmpty(t.gives)) box.Add(_ui.L(TT("Might turn up something useful.", "อาจเจอของที่ใช้ได้"), 20, false, GameUI.CText));
                if (!can) box.Add(_ui.L(WhyText(why, t), 21, true, GameUI.CRed));
                box.Add(_ui.L(TaskFact(t), 16, false, GameUI.CDim));
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginTop = 14;
                var go = _ui.B(TT("Start", "เริ่มทำ") + " (" + Hrs(hours) + TT(" h)", " ชม.)"), () => RestoreStart(id), 24, true);
                go.SetEnabled(can); row.Add(go);
                var back = _ui.B(TT("Not now", "ไว้ก่อน"), () => _ui.HideScreen(), 22); back.style.marginLeft = 14; row.Add(back);
                box.Add(row);
                root.Add(box);
            });
        }

        string ItemNameR(string id) => id == "broom" ? TT("Broom", "ไม้กวาด") : id == "crowbar" ? TT("Crowbar", "ชะแลง") : ItemName(id);

        string WhyText(string key, RestoreTaskDef t)
        {
            switch (key)
            {
                case "restore.need.item": return TT("You do not have the ", "ยังไม่มี") + ItemNameR(t?.needItem) + TT(" yet. Search the junk piles around the base.", " ลองค้นกองของรอบ ๆ ฐาน");
                case "restore.need.time": return TT("Not enough time left today. Rest and do it tomorrow.", "วันนี้เหลือเวลาไม่พอ พักก่อนแล้วทำพรุ่งนี้");
                case "restore.need.room": return TT("This room is still shut. Get the generator running first.", "ห้องนี้ยังเข้าไม่ได้ ต้องเปิดเครื่องปั่นไฟก่อน");
                default: return TT("Already done.", "ทำไปแล้ว");
            }
        }

        void RestoreStart(string id)
        {
            _ui.HideScreen();
            var t = TaskDef(id);
            string puzzle = t != null ? t.puzzle : "wires";
            if (string.IsNullOrEmpty(puzzle)) { StartCoroutine(Act("repair", 1.0f, () => RestoreFinish(id, 0))); return; }
            var d = new JobDef { id = "restore_" + id, spot = t != null ? t.spot : ResetSpot(id.Substring(6)), en = TaskTitle(id), th = TaskTitle(id), factEn = TaskFact(t), factTh = TaskFact(t), ways = new JobWay[0] };
            int helpers = JobHelpers(d.spot);
            StartCoroutine(Act(t != null && t.kind == "sweep" ? "repair" : "repair", 0.45f, () =>
                PzOpen(puzzle, helpers, d, (m, s) => RestoreFinish(id, m), () => _ui.Toast(TT("You stepped back. It still waits.", "ถอยออกมาก่อน งานยังรออยู่"), GameUI.CDim))));
        }

        void RestoreFinish(string id, int mistakes)
        {
            _ui.HideScreen();
            var t = TaskDef(id);
            float before = _restore.Hour;
            if (!_restore.Do(id, RestoreBag(), mistakes)) { _ui.Toast(TT("Not enough time left today.", "วันนี้เหลือเวลาไม่พอแล้ว"), GameUI.CRed); return; }
            RestoreSync();
            if (t != null && t.consume && !string.IsNullOrEmpty(t.needItem)) { if (BeltHas(t.needItem)) BeltUse(t.needItem); else BagUse(t.needItem); }
            if (t != null && !string.IsNullOrEmpty(t.gives))
                foreach (var g in t.gives.Split(','))
                {
                    BeltAdd(g);
                    _ui.Toast(TT("Found: ", "เจอ: ") + ItemNameR(g), GameUI.CGreen);
                }
            if (t != null && t.kind == "sweep" && _rDebris.TryGetValue(t.spot, out var deb) && deb != null) deb.enabled = false;
            if (t != null && t.kind == "search") { var junk = BSpotById(t.spot); if (junk?.sr != null) junk.sr.sprite = ArtRestore.Junk(4); }
            if (id == "chip") OrbiBoot();
            float spent = _restore.Hour - before;
            _sfx.Play("good");
            _ui.Toast(TaskTitle(id) + TT(" · done · ", " · เสร็จ · ") + Hrs(spent) + TT(" h", " ชม.") + (mistakes > 0 ? TT(" (fixing mistakes took longer)", " (แก้จุดที่พลาดเลยนานขึ้น)") : ""), GameUI.CCyan);
            BRefreshProps();
        }

        /// <summary>Hover label: the hours a task at this spot takes.</summary>
        string RestoreHint(string spotId)
        {
            string task = TaskAtSpot(spotId);
            return task == null ? null : " · " + TaskTitle(task) + " · " + Hrs(_restore.HoursFor(task)) + TT(" h", " ชม.");
        }

        /// <summary>Spots with an open restore task get a "!".</summary>
        IEnumerable<string> RestoreMarkSpots()
        {
            if (!RestoreMode || _restore == null) yield break;
            foreach (var id in _restore.OpenTasks())
            {
                var t = TaskDef(id);
                yield return t != null ? t.spot : ResetSpot(id.Substring(6));
            }
        }
    }
}
