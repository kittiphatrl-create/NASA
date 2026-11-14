// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do drinking, snacks, naps, a solar storm that cuts the power, daily weather and a pretty night make
//           the restored base feel like a place you live in, not a checklist?
// Date: 2026-11-14
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// Everyday life in the abandoned base (restore mode): drink and snack, nap, the solar storm of a flare day
    /// (prepare the breaker box and shelter in the lounge, or the power trips everywhere), weather costs at night,
    /// the cooling-loop repair on a hot day, and the night walk with a sky view outside.
    /// </summary>
    public partial class GameFlow
    {
        bool _rNight, _rSafeMode, _rFlareCalled, _rAurora;
        const float FlareCallHour = 12f, FlareHitHour = 13f;

        /// <summary>Everyday spots in restore mode. True = handled.</summary>
        bool RestoreUseLife(BSpot s)
        {
            var st = _life.State;
            switch (s.id)
            {
                case "shelf_water":
                    if (st.stock.water < 0.25f) { BBad("base.shelf", _loc.T("life.res.water"), "0"); return true; }
                    if (_restore.HoursLeft < 0.25f) return false;
                    _life.ApplyTask(new Stock { water = -0.25f });
                    _restore.Spend(0.25f); RestoreSync(); _restore.Drink();
                    _sfx.Play("good", 0.6f);
                    _ui.Toast(TT("You drink a cup of recycled water. Water −0.25 · 15 min", "ดื่มน้ำรีไซเคิลหนึ่งแก้ว น้ำ −0.25 · 15 นาที"), GameUI.CCyan);
                    return true;
                case "shelf_food":
                    if (st.stock.food < 0.5f || _restore.Food > 70) { _ui.Toast(TT("Not hungry enough for a snack (or no food).", "ยังไม่หิวพอจะกินขนม (หรืออาหารหมด)"), GameUI.CDim); return true; }
                    _life.ApplyTask(new Stock { food = -0.5f });
                    _restore.Spend(0.25f); RestoreSync();
                    _restore.Eat();
                    _ui.Toast(TT("A quick snack. Food −0.5 · 15 min", "กินของว่างนิดหน่อย อาหาร −0.5 · 15 นาที"), GameUI.CCyan);
                    return true;
                case "seats":
                    if (_rNight) { _rNight = false; LbSleep(); return true; }
                    if (s.id == BActiveSpot) return false;   // dinner when it is time
                    RestoreNapShow(1f);
                    return true;
                case "bunks":
                    if (_restore.StateOf("quarters") != RoomState.Restored) return false;
                    if (_rNight) { _rNight = false; LbSleep(); return true; }
                    RestoreNapShow(2f);
                    return true;
                case "panel":
                    if (!_restore.CoolingBroken) return false;
                    RestoreCoolingShow();
                    return true;
                case "breaker_box":
                    if (_restore.Weather != "flare" || _rFlareDone || _restore.StateOf("power") != RoomState.Restored) return false;
                    if (_rSafeMode) { _ui.Toast(TT("Safe mode is on. Now get to the lounge before the storm.", "เปิดโหมดปลอดภัยแล้ว ไปห้องนั่งเล่นก่อนพายุมา"), GameUI.CCyan); return true; }
                    var d = new JobDef { id = "restore_safe", spot = "breaker_box", en = "Storm safe mode", th = "โหมดปลอดภัยรับพายุ", factEn = "Spacecraft switch sensitive electronics off or to safe mode before a solar storm.", factTh = "ยานอวกาศจะปิดอุปกรณ์อิเล็กทรอนิกส์ที่ไวหรือเข้าโหมดปลอดภัยก่อนพายุสุริยะมา", ways = new JobWay[0] };
                    StartCoroutine(Act("repair", 0.4f, () => PzOpen("switches", JobHelpers("breaker_box"), d, (m, sec) =>
                    {
                        _ui.HideScreen(); _restore.Spend(0.5f, 0.5f); RestoreSync(); _rSafeMode = true;
                        _sfx.Play("good"); OrbiSay(TT("Safe mode on: the storm cannot fry the circuits now. Shelter in the lounge at 13:00!", "เปิดโหมดปลอดภัยแล้ว พายุจะทำวงจรเสียไม่ได้ ไปหลบที่ห้องนั่งเล่นก่อน 13:00!"), GameUI.CGreen);
                    }, () => { })));
                    return true;
                case "stargaze":
                    SkyShow();
                    return true;
            }
            return false;
        }

        void RestoreNapShow(float hours)
        {
            if (_restore.Hour + hours > _cfg.restore.endHour) { _ui.Toast(TT("Too late for a nap. Sleep tonight.", "ดึกเกินจะงีบแล้ว ไว้นอนคืนนี้"), GameUI.CDim); return; }
            StartCoroutine(Act("sit", 0.8f, () =>
            {
                if (!_restore.Nap(hours)) return;
                RestoreSync();
                _ui.Toast(TT($"You nap for {Hrs(hours)} h. Energy back up.", $"งีบไป {Hrs(hours)} ชม. แรงกลับมาแล้ว"), GameUI.CGreen);
            }));
        }

        void RestoreCoolingShow()
        {
            if (_restore.Hour + _cfg.restore.coolingHours > _cfg.restore.endHour) { _ui.Toast(TT("Not enough time today.", "วันนี้เวลาไม่พอแล้ว"), GameUI.CRed); return; }
            var d = new JobDef { id = "restore_cooling", spot = "panel", en = "Fix the cooling loop", th = "ซ่อมระบบระบายความร้อน", factEn = "The space station pumps coolant through loops to carry heat out to radiators, or the inside would overheat.", factTh = "สถานีอวกาศสูบของเหลวระบายความร้อนวนผ่านท่อไปยังแผงระบายความร้อน ไม่อย่างนั้นข้างในจะร้อนจัด", ways = new JobWay[0] };
            StartCoroutine(Act("repair", 0.4f, () => PzOpen("pipes", JobHelpers("panel"), d, (m, sec) =>
            {
                _ui.HideScreen();
                if (!_restore.FixCooling()) return;
                RestoreSync();
                _sfx.Play("good");
                OrbiSay(TT("Coolant flowing again. It will be comfy in here soon.", "ของเหลวระบายความร้อนไหลแล้ว เดี๋ยวในฐานก็เย็นสบาย"), GameUI.CGreen);
            }, () => { })));
        }

        // ------------------------------------------------------------------ the solar storm of a flare day

        void RestoreFlareUpdate()
        {
            if (_restore.Weather != "flare" || _rFlareDone || _life.State.outcome != LifeOutcome.Ongoing) return;
            if (!_rFlareCalled && _restore.Hour >= FlareCallHour)
            {
                _rFlareCalled = true; _sfx.Play("alarm"); _sShake = 0.3f;
                EmAlert(TT("Solar flare incoming", "เปลวสุริยะกำลังมา"), TT("Head to the lounge shelter.", "ไปยังที่หลบภัยในห้องนั่งเล่น"), radiation: true);
                OrbiSay(TT("X-rays from the flare just arrived at light speed. The fast particles follow soon: get to the lounge shelter NOW!", "รังสีเอกซ์จากเปลวสุริยะมาถึงแล้วด้วยความเร็วแสง อนุภาคพลังงานสูงจะตามมาเร็ว ๆ นี้ ไปหลบห้องนั่งเล่นเดี๋ยวนี้!"), GameUI.CRed);
            }
            if (_restore.Hour < FlareHitHour || _ui.Blocking) return;
            _rFlareDone = true; _rAurora = true;
            bool sheltered = BRoomAt(_bPos) == "base.room.pod";
            if (!_rSafeMode)
            {
                _restore.Blackout();
                _sfx.Play("bad"); _sShake = 0.5f;
                OrbiSay(TT("The storm tripped the power everywhere! Reset each room at its breaker or main machine (the ! marks).", "พายุทำไฟตัดทั้งฐาน! ไปรีเซ็ตไฟทีละห้องที่ตู้ไฟหรือเครื่องหลักของห้อง (ดูเครื่องหมาย !)"), GameUI.CRed);
                DayNote(TT("Solar storm: the power tripped across the base.", "พายุสุริยะ: ไฟตัดทั้งฐาน"));
            }
            else DayNote(TT("Solar storm: safe mode saved the circuits.", "พายุสุริยะ: โหมดปลอดภัยช่วยวงจรไว้ได้"));
            RadAdd(sheltered ? 0.5f : 13f, sheltered ? null : TT("caught by the solar storm outside the shelter", "โดนพายุสุริยะนอกห้องหลบ"));
            if (sheltered) OrbiSay(TT("The storm passed over us. The lounge walls and water tanks kept most particles out.", "พายุผ่านไปแล้ว ผนังห้องนั่งเล่นกับถังน้ำกันอนุภาคไว้ได้เกือบหมด"), GameUI.CGreen);
        }

        /// <summary>Night costs of the weather (called before sleeping).</summary>
        void RestoreWeatherNight()
        {
            // a working water recycler wins back most of the day's water (the space station recovers about 98%)
            if (_restore.StateOf("storage") == RoomState.Restored) { _life.ApplyTask(new Stock { water = 1.5f }); _ui.Toast(TT("The water recycler cleaned last night's water: water +1.5.", "เครื่องรีไซเคิลน้ำบำบัดน้ำเมื่อคืน: น้ำ +1.5"), GameUI.CCyan); }
            if (_restore.Weather == "cold") { _life.ApplyTask(new Stock { power = -Mathf.Min(0.5f, _life.State.stock.power) }); _ui.Toast(TT("The heaters worked hard in the cold: power −0.5.", "ฮีตเตอร์ทำงานหนักเพราะหนาวจัด: ไฟ −0.5"), GameUI.CAmber); }
            if (_restore.Weather == "dust") { _life.ApplyTask(new Stock { power = -Mathf.Min(1f, _life.State.stock.power) }); _ui.Toast(TT("Dusty sky: the solar panels made less power. Power −1.", "ฟ้าเต็มไปด้วยฝุ่น แผงโซลาร์ได้ไฟน้อยลง: ไฟ −1"), GameUI.CAmber); }
        }

        // ------------------------------------------------------------------ evening and night

        /// <summary>After dinner: a walk under the night sky before bed (sleep at the lounge seats or the bunks).</summary>
        void RestoreEvening()
        {
            _ui.HideScreen();
            _rNight = true; _bLateDone = true;
            BEndGather();
            _bOpen = false;
            OrbiSay(_rAurora ? TT("Look outside tonight: after a solar storm Mars can glow with a faint green aurora. Perseverance photographed one in 2024!", "คืนนี้ลองออกไปดูข้างนอก หลังพายุสุริยะ ดาวอังคารมีแสงออโรราสีเขียวจาง ๆ ได้ Perseverance เคยถ่ายไว้ในปี 2024!")
                : TT("Free time until bed. The stars are amazing tonight: try the viewing spot outside the airlock. Sleep at the seats or a bunk.", "เวลาว่างก่อนนอน คืนนี้ดาวสวยมาก ลองไปจุดดูดาวนอกประตูปรับแรงดัน แล้วไปนอนที่เก้าอี้หรือเตียง"), GameUI.CCyan);
        }

        void SkyShow()
        {
            bool night = _rNight || _restore.Hour >= 18.5f;
            _ui.ShowScreen(root =>
            {
                var box = new VisualElement(); box.style.alignItems = Align.Center;
                var pic = new Image { sprite = Art.RSky(night, night && _rAurora, _restore.Day), scaleMode = ScaleMode.ScaleToFit };
                pic.style.width = 1440; pic.style.height = 810;
                GameUI.Border(pic, GameUI.CCyan, 3, 12);
                box.Add(pic);
                box.Add(_ui.L(night
                    ? TT("Two moons: Phobos (big, crosses the sky twice a day) and little Deimos. The bright blue dot is Earth: everyone you know is there.", "ดวงจันทร์สองดวง: โฟบอส (ใหญ่ เคลื่อนข้ามฟ้าวันละสองรอบ) กับไดมอสดวงเล็ก จุดสีฟ้าสว่างนั่นคือโลก ทุกคนที่เรารู้จักอยู่ตรงนั้น")
                    : TT("Mars sunsets are blue: fine dust scatters blue light toward the setting Sun. Curiosity photographed one in 2015.", "พระอาทิตย์ตกบนดาวอังคารเป็นสีฟ้า เพราะฝุ่นละเอียดกระเจิงแสงสีฟ้าไปทางดวงอาทิตย์ที่กำลังตก Curiosity ถ่ายไว้ในปี 2015"), 22, false, GameUI.CText));
                box.Add(_ui.B(TT("Back inside", "กลับเข้าไปข้างใน"), () => _ui.HideScreen(), 22));
                root.Add(box);
            });
        }
    }
}
