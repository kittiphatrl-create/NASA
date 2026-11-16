// PROTOTYPE - NOT FOR PRODUCTION
// Question: Is the base easier to read (and more alive) when ORBI reports it and warns in chat bubbles,
//           instead of gauges always on screen?
// Date: 2026-11-14
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// ORBI in the abandoned base: it rode down on a chip, comes back when the chip is plugged into the lounge
    /// console, reports the base on its terminal, and warns about real dangers while the cadet can still move.
    /// </summary>
    public partial class GameFlow
    {
        readonly HashSet<string> _orbiWarned = new HashSet<string>();
        int _orbiWarnDay = -1;
        float _orbiWarnT;
        bool _rPickup;   // the signal was sent and a night has passed: the ship lands, say goodbye at the airlock

        bool OrbiOnline => _restore != null && _restore.IsDone("chip");

        // ------------------------------------------------------------------ spoken notices

        void OrbiSay(string text, Color tone)
        {
            if (_ui?.Root == null) return;
            _ui.NotifySpeech("orbi", text, tone);
            _sfx.Play("blip", 0.7f);
        }

        void OrbiBoot()
        {
            OrbiSay(TT("Rebooting... ORBI online! The chip held. This base has been empty a long time: power off, rooms dark, dust everywhere.",
                "กำลังเริ่มระบบ... ORBI กลับมาแล้ว! ชิปยังดีอยู่ ฐานนี้ร้างมานาน ไฟดับ ห้องมืด ฝุ่นเต็มไปหมด"), GameUI.CCyan);
            OrbiSay(TT("Start with the generator in the power room. Every job shows how many hours it takes. Talk to me here for the base status.",
                "เริ่มที่เครื่องปั่นไฟในห้องไฟฟ้าก่อน ทุกงานบอกว่าใช้กี่ชั่วโมง อยากรู้สถานะฐานมาคุยกับฉันที่นี่"), GameUI.CCyan);
        }

        // ------------------------------------------------------------------ warnings

        void OrbiUpdate(float dt)
        {
            if (!RestoreMode || _restore == null || _life == null || _bMode != 0 || _life.State.outcome != LifeOutcome.Ongoing) return;
            _orbiWarnT -= dt;
            if (_orbiWarnT > 0) return;
            _orbiWarnT = 1f;
            if (_orbiWarnDay != _restore.Day) { _orbiWarnDay = _restore.Day; _orbiWarned.Clear(); OrbiMorning(); }
            // needs at zero hurt: once a day each
            if (_restore.Water <= 0 && _orbiWarned.Add("hurt_water")) { _life.ApplyHealth(-1); _ui.Toast(TT("Dehydrated: health −1. Drink water!", "ขาดน้ำ: สุขภาพ −1 ไปดื่มน้ำเร็ว!"), GameUI.CRed); }
            if (_restore.Food <= 0 && _orbiWarned.Add("hurt_food")) { _life.ApplyHealth(-1); _ui.Toast(TT("Starving: health −1.", "หิวจนอ่อนแรง: สุขภาพ −1"), GameUI.CRed); }
            var ration = _life.RationNeed(_life.Mouths());
            bool flare = _restore.Weather == "flare" && !_rFlareDone;
            foreach (var key in _restore.Warnings(_life.State.stock, _life.O2Tonight, LC.lifeSupportPower - LC.solarPerDay, ration, flare))
            {
                bool personal = key.EndsWith("_me");
                if (!OrbiOnline && !personal) continue;   // without ORBI only your own body tells you
                if (!_orbiWarned.Add(key)) continue;
                OrbiSay(WarnText(key), key == "restore.warn.flare" ? GameUI.CAmber : GameUI.CRed);
                if (key == "restore.warn.o2") _sfx.Play("alarm");
            }
        }

        bool _rFlareDone;

        string WarnText(string key)
        {
            switch (key)
            {
                case "restore.warn.o2": return TT("Warning: not enough oxygen for everyone tonight! Run the oxygen maker, or fewer people inside.", "เตือน: ออกซิเจนไม่พอสำหรับทุกคนคืนนี้! เปิดเครื่องผลิตออกซิเจน หรือให้คนในฐานน้อยลง");
                case "restore.warn.power": return TT("Warning: the battery will not last the night for life support. Clean the solar panels, or switch things off.", "เตือน: แบตเตอรี่ไม่พอเลี้ยงระบบช่วยชีวิตทั้งคืน ไปปัดฝุ่นแผงโซลาร์ หรือปิดเครื่องที่ไม่จำเป็น");
                case "restore.warn.water": return TT("Not enough water for tonight's drinks. Drill ice outside or fix the water recycler.", "น้ำไม่พอสำหรับคืนนี้ ออกไปเจาะน้ำแข็ง หรือซ่อมเครื่องรีไซเคิลน้ำ");
                case "restore.warn.food": return TT("Not enough food for everyone tonight.", "อาหารไม่พอสำหรับทุกคนคืนนี้");
                case "restore.warn.flare": return TT("Space weather alert: a solar storm is coming today. Reset the breaker box to safe mode and be in the lounge when I call!", "แจ้งเตือนสภาพอวกาศ: วันนี้พายุสุริยะกำลังมา ไปตั้งตู้เบรกเกอร์เป็นโหมดปลอดภัย แล้วมาอยู่ห้องนั่งเล่นตอนฉันเรียก!");
                case "restore.warn.food_me": return TT("Your tummy is rumbling. Eat at dinner, or grab a snack from the food shelf.", "ท้องร้องแล้ว ไปกินมื้อเย็น หรือหยิบของกินที่ชั้นอาหาร");
                case "restore.warn.water_me": return TT("You are thirsty! Drink at the water shelf in the lounge.", "คอแห้งแล้ว! ไปดื่มน้ำที่ชั้นน้ำในห้องนั่งเล่น");
                case "restore.warn.rest_me": return TT("You are worn out. Nap on the sofa, or call it a day.", "เหนื่อยมากแล้ว ไปงีบที่โซฟา หรือพักวันนี้ก่อน");
                default: return key;
            }
        }

        /// <summary>Morning report: today's weather and what it means (real ranges from Curiosity's weather station).</summary>
        void OrbiMorning()
        {
            if (!OrbiOnline) return;
            string w = _restore.Weather;
            string line = w == "cold" ? TT("Cold snap: about −20°C at noon, −90°C tonight. The heater will eat more power.", "อากาศหนาวจัด: กลางวันราว −20°C คืนนี้ราว −90°C ฮีตเตอร์จะกินไฟมากขึ้น")
                : w == "dust" ? TT("Dust storm: the sky is brown and the solar panels get less light today. Nights stay a bit warmer.", "พายุฝุ่น: ฟ้าสีน้ำตาล แผงโซลาร์ได้แสงน้อยลงวันนี้ แต่กลางคืนจะอุ่นขึ้นนิดหน่อย")
                : w == "hot_inside" ? TT("Outside is a normal −5°C, but the base cooling loop failed: it is 31°C in here! You will get thirsty fast. Fix the cooling pipes.", "ข้างนอก −5°C ปกติ แต่ระบบระบายความร้อนของฐานเสีย ในนี้ 31°C! จะหิวน้ำเร็ว ไปซ่อมท่อระบายความร้อน")
                : w == "flare" ? TT("Space weather watch: the Sun is very active today. Expect a solar storm.", "เฝ้าระวังสภาพอวกาศ: ดวงอาทิตย์คึกมากวันนี้ เตรียมรับพายุสุริยะ")
                : TT("Clear sky: about 0°C at noon, −70°C tonight. A normal day on Mars.", "ฟ้าใส: กลางวันราว 0°C คืนนี้ราว −70°C วันปกติบนดาวอังคาร");
            OrbiSay(TT($"Good morning! Day {_restore.Day}. ", $"อรุณสวัสดิ์! วันที่ {_restore.Day} ") + line, GameUI.CCyan);
        }

        // ------------------------------------------------------------------ terminal

        void OrbiTerminalShow()
        {
            var st = _life.State;
            _ui.ShowScreen(root =>
            {
                var box = new VisualElement(); box.style.width = 1200;
                GameUI.PanelFrame(box, GameUI.CAmber);
                box.style.paddingLeft = box.style.paddingRight = 30; box.style.paddingTop = 18; box.style.paddingBottom = 18;
                var head = new VisualElement(); head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center;
                var face = new Image { sprite = ArtRestore.OrbiFace(), scaleMode = ScaleMode.ScaleToFit }; face.style.width = 72; face.style.height = 72; face.style.marginRight = 14;
                head.Add(face); head.Add(_ui.L(TT("ORBI · base status", "ORBI · สถานะฐาน"), 34, true, GameUI.CCyan));
                box.Add(head);
                var ration = _life.RationNeed(_life.Mouths());
                void Line(string label, float have, float need, string unit = "")
                {
                    bool bad = have < need;
                    box.Add(_ui.L($"{label}: {LbNum(have)}{unit}" + TT($"   (tonight needs {LbNum(need)})", $"   (คืนนี้ต้องใช้ {LbNum(need)})"), 24, true, bad ? GameUI.CRed : GameUI.CText));
                }
                Line("O₂", st.stock.o2, _life.O2Tonight);
                Line(TT("Water", "น้ำ"), st.stock.water, ration.water);
                Line(TT("Food", "อาหาร"), st.stock.food, ration.food);
                Line(TT("Power", "ไฟฟ้า"), st.stock.power, Mathf.Max(0, LC.lifeSupportPower - LC.solarPerDay));
                string inside = _restore.CoolingBroken ? "31°C" : _restore.StateOf("quarters") == RoomState.Restored ? "21°C" : "12°C";
                box.Add(_ui.L(TT("Inside: ", "ในฐาน: ") + inside + TT("  ·  Radiation: ", "  ·  รังสีสะสม: ") + RadLevelText(), 21, false, GameUI.CText));
                box.Add(_ui.L(TT("Base restored: ", "ฟื้นฐานแล้ว: ") + Mathf.RoundToInt(_restore.Percent) + "%", 26, true, GameUI.CAmber));
                var rooms = new List<string>();
                foreach (var r in _cfg.restore.rooms)
                {
                    if (r.start) continue;
                    var s = _restore.StateOf(r.id);
                    string mark = s == RoomState.Restored ? TT("done", "เสร็จ") : s == RoomState.Unpowered ? TT("no power", "ไฟดับ") : s == RoomState.Locked ? TT("shut", "ปิดอยู่") : TT("to fix", "รอซ่อม");
                    rooms.Add(RoomName(r.id) + " (" + mark + ")");
                }
                box.Add(_ui.L(string.Join("   ", rooms), 19, false, GameUI.CDim));
                if (_restore.StateOf("comms") == RoomState.Restored)
                    box.Add(_ui.L(TT("Next relay orbiter pass: ", "ยานโคจรถ่ายทอดผ่านครั้งถัดไป: ") + HourText(_restore.NextRelayHour), 22, true, GameUI.CCyan));
                box.Add(_ui.B(TT("Thanks, ORBI", "ขอบใจนะ ORBI"), () => _ui.HideScreen(), 22));
                root.Add(box);
            });
        }

        static string HourText(float h) { if (h >= 24) h -= 24; return $"{(int)h:00}:{(int)(h % 1 * 60):00}"; }

        // ------------------------------------------------------------------ needs on the HUD

        /// <summary>Walking slows down when exhausted or short of air.</summary>
        float RestoreSpeedMult => !RestoreMode || _restore == null ? 1f : (_restore.Rest < 25 ? 0.75f : 1f) * (_orbiWarned.Contains("restore.warn.o2") ? 0.85f : 1f);

        // ------------------------------------------------------------------ radio and the ending

        void RestoreRadioShow()
        {
            _ui.ShowScreen(root =>
            {
                var box = new VisualElement(); box.style.width = 1000;
                GameUI.PanelFrame(box, GameUI.CAmber);
                box.style.paddingLeft = box.style.paddingRight = 30; box.style.paddingTop = 18; box.style.paddingBottom = 18;
                box.Add(_ui.L(TT("Distress call", "ส่งคำขอความช่วยเหลือ"), 36, true, GameUI.CCyan));
                box.Add(_ui.L(TT("The radio cannot reach Earth directly. A relay orbiter passes overhead twice a day for a short time; the message rides on it to Earth.",
                    "วิทยุส่งตรงถึงโลกไม่ได้ ต้องรอยานโคจรถ่ายทอดที่ผ่านเหนือฐานวันละสองครั้ง ครั้งละไม่นาน ข้อความจะฝากไปกับยานนั้นถึงโลก"), 21));
                box.Add(_ui.L(TT("Real NASA rovers talk to orbiters like this: a pass lasts about 8 minutes, once or twice a day.", "ยานสำรวจจริงของ NASA ก็คุยกับยานโคจรแบบนี้ ผ่านแต่ละครั้งราว 8 นาที วันละ 1–2 ครั้ง"), 16, false, GameUI.CDim));
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginTop = 14;
                if (_restore.Signalled) box.Add(_ui.L(TT("Message sent. Help is on the way.", "ส่งข้อความแล้ว ความช่วยเหลือกำลังมา"), 24, true, GameUI.CGreen));
                else if (_restore.RelayOpen)
                {
                    box.Add(_ui.L(TT("The orbiter is overhead NOW!", "ยานโคจรอยู่เหนือฐานตอนนี้!"), 26, true, GameUI.CGreen));
                    row.Add(_ui.B(TT("Lock on and send", "จับสัญญาณแล้วส่ง"), () =>
                    {
                        _ui.HideScreen();
                        var d = new JobDef { id = "restore_send", spot = "radio", en = "Lock on to the relay orbiter", th = "จับสัญญาณยานโคจรถ่ายทอด", factEn = "Mars relay orbiters carry most rover data to Earth.", factTh = "ยานโคจรถ่ายทอดรอบดาวอังคารส่งข้อมูลส่วนใหญ่ของยานสำรวจไปยังโลก", ways = new JobWay[0] };
                        PzOpen("tune", 0, d, (m, s) => RestoreSend(), () => { });
                    }, 24, true));
                }
                else
                {
                    float next = _restore.NextRelayHour;
                    box.Add(_ui.L(TT("Next pass: ", "ยานจะผ่านครั้งถัดไป: ") + HourText(next) + (next >= 24 ? TT(" tomorrow", " พรุ่งนี้") : ""), 24, true, GameUI.CAmber));
                    if (next < 24 && next - _restore.Hour <= _restore.HoursLeft)
                        row.Add(_ui.B(TT("Wait by the radio until it passes", "รอที่วิทยุจนยานผ่าน") + $" ({Hrs(next - _restore.Hour)}" + TT(" h)", " ชม.)"), () => { _restore.Spend(next - _restore.Hour); RestoreSync(); RestoreRadioShow(); }, 22, true));
                }
                var back = _ui.B(TT("Close", "ปิด"), () => _ui.HideScreen(), 22); back.style.marginLeft = 14; row.Add(back);
                box.Add(row);
                root.Add(box);
            });
        }

        void RestoreSend()
        {
            _ui.HideScreen();
            if (!_restore.SendSignal()) { _ui.Toast(TT("The orbiter moved out of range. Try the next pass.", "ยานโคจรเลยระยะไปแล้ว ลองรอบถัดไป"), GameUI.CAmber); return; }
            RestoreSync();
            _sfx.Play("good"); _sShake = 0.2f;
            DayNote(TT("We sent the distress call! Earth heard us.", "ส่งคำขอความช่วยเหลือแล้ว! โลกได้ยินเรา"));
            OrbiSay(TT("Message received by Earth! The reply took minutes to cross the gap: a rescue ship lands tomorrow. You did it!",
                "โลกได้รับข้อความแล้ว! คำตอบใช้เวลาหลายนาทีกว่าจะข้ามมาถึง ยานกู้ภัยจะลงจอดพรุ่งนี้ ทำได้แล้ว!"), GameUI.CGreen);
        }

        /// <summary>After the night that follows the signal: say goodbye at the airlock to end the run.</summary>
        void RestoreNight()
        {
            if (!RestoreMode || _restore == null) return;
            RestoreWeatherNight();
            if (_lbFed.Contains(LifeboatSim.Me)) { _restore.Eat(); _restore.Drink(); }
            else { _life.ApplyHealth(-1); }
            _rNight = false; _rSafeMode = false; _rFlareCalled = false;
            bool signalled = _restore.Signalled;
            _restore.Sleep();
            if (signalled) _rPickup = true;
            _rFlareDone = false; _rAurora = false;
        }

        bool RestoreGoodbye(BSpot s)
        {
            if (!_rPickup || s.id != "hatch" && s.id != "hatch_out") return false;
            _ui.ShowScreen(root =>
            {
                var box = new VisualElement(); box.style.width = 900;
                GameUI.PanelFrame(box, GameUI.CGreen);
                box.style.paddingLeft = box.style.paddingRight = 30; box.style.paddingTop = 18; box.style.paddingBottom = 18;
                box.Add(_ui.L(TT("The rescue ship has landed", "ยานกู้ภัยลงจอดแล้ว"), 36, true, GameUI.CGreen));
                box.Add(_ui.L(TT("Take one last look at the base you brought back to life.", "มองฐานที่เราฟื้นขึ้นมาอีกครั้ง ก่อนบอกลา"), 22));
                box.Add(_ui.B(TT("Say goodbye and board", "บอกลาฐานแล้วขึ้นยาน"), () =>
                {
                    _ui.HideScreen();
                    RestoreRescueScene(() => { _life.State.outcome = LifeOutcome.Rescued; _lbView = LbView.End; LbShow(); });
                }, 26, true));
                box.Add(_ui.B(TT("Not yet", "อีกเดี๋ยว"), () => _ui.HideScreen(), 20));
                root.Add(box);
            });
            return true;
        }
    }
}
