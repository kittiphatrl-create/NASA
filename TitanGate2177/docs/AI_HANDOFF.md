# TITAN GATE 2177 — AI HANDOFF / อ่านไฟล์นี้ก่อนเริ่มงาน

ตรวจ source วันที่ 14 พฤศจิกายน 2026 (Asia/Bangkok). เอกสารนี้เป็นแผนที่ของระบบที่มีจริง ไม่ใช่คำสั่งให้สร้างใหม่ทุกอย่าง เอกสารเก่าเป็นประวัติ: README บางส่วนยังอธิบาย OutpostSim รุ่นเก่า ต้องตรวจ active flags และ GameFlow จริงก่อนแก้

## 1. ภาพรวมและสิ่งที่ผู้ใช้ต้องการ
เกมเอาชีวิตรอด/ฝึกนักบินอวกาศ pixel-art 2.5D ไทย/อังกฤษ ใน Unity 6000.3.25f1, built-in renderer, UI Toolkit, Input System. ภาพส่วนใหญ่สร้างจากโค้ด PixelCanvas/SpriteRenderer ไม่ใช่โมเดล 3D หรือ Animator clip สำเร็จรูป เป้าหมายคือภาพน่ารัก อ่านง่าย มีมิติจากผนัง การบัง เงาและแสง พร้อมเรียนรู้วิทยาศาสตร์ผ่านการเล่น

ทำเกมก่อน เว็บไซต์ยังไม่สร้างและไม่ใช่ข้อบังคับในการเล่น ข้อมูลที่จำเป็นต้องอ่านในเกมได้ เว็บภายหลังอาจเป็นคลังความรู้/คู่มือครู อย่าบังคับสลับไปหา clue นอกเกม

## 2. เนื้อเรื่องที่ตกลงแล้ว
ปี 2177 มนุษย์มีถิ่นฐานนอกโลกและเครือข่ายจัมป์ (เรื่องแต่ง) ผู้เล่นเป็นนักบินฝึกหัดบน Kestrel กับผู้การอรุณ, ORBI และสัตว์เลี้ยง อรุณสอนงานประจำเพื่อให้รู้จักยาน ก่อนพักบนโซฟา เศษอุกกาบาตชนยาน ระบบปล่อยยานกู้ชีพเสีย อรุณต้องอยู่เปิดด้วยมือ ผู้เล่นเก็บเสบียงแล้วหนีไปกับสัตว์และ ORBI ด้วยยานเล็ก สัญญาณอรุณขาดหาย **ยังไม่ยืนยันว่าเสียชีวิต**
ยานกู้ชีพหนึ่งห้องลงดาวอังคารและเชื่อมท่อกับฐานฉุกเฉินที่มีอยู่เดิม เริ่มเอาชีวิตรอด 7 วัน ซ่อมวิทยุและส่งพิกัดขอความช่วยเหลือ ตอนจบช่วยเหลือสำเร็จมีเบาะแสรหัสเรียกอรุณ ยังไม่มีบทตามหาอรุณที่เล่นได้
Kestrel เป็นยานใหญ่หลายห้อง, pod เป็นห้องเล็กหนึ่งห้อง, habitat เป็นฐานหลายโมดูล คนละแผนที่ อย่ารวมเป็นสถานที่เดียวกัน

## 3. เส้นทางเล่นและการควบคุม
- เมนู New mission → คัตซีน → เลือกสัตว์ → งานเช้าบนยาน → นั่งพัก → อุกกาบาตชน → เก็บของ 60 วินาที → อพยพ → ฐาน
- WASD/ลูกศรเดิน, Shift วิ่ง, คลิกเดิน/ใช้, E ใช้/คุยกับเป้าหมายใกล้, Tab ดูของ/รายละเอียด, Esc ปิด/พักเกม ตาม context
- ฐานล่าสุดมี P หยุดเวลาเพื่อวางแผน และ F เปลี่ยน 1x/2x; ต้องอ่าน PaceUpdate เพราะการหยุดเวลา/หน้าต่าง/QA มีข้อยกเว้น
- บนยานสอนหยิบอาหาร→ชามสัตว์, ตัวกรอง→เครื่องรีไซเคิล, น้ำ→ต้นไม้, วิทยุ, โซฟา. มือถือได้ 3 ชิ้น; ยานกู้ชีพจุ 5 จากเสบียง 10 แบบ
- อย่าสับสน timer จริงของ scramble/เหตุฉุกเฉิน กับเวลาทำงานต่อวัน และ OutpostSim แบบ 168 ชั่วโมงรุ่นเก่า

## 4. ระบบฐานปัจจุบัน
LifeboatSim เป็นเจ้าของกติกาเสบียง/แขก/กิน/คืน/ผลลัพธ์; GameFlow partials เชื่อมโลกเดินได้และ UI. ยังมี OutpostSim และเส้นทางเก่าอยู่เพื่อ compatibility/QA อย่าแก้ผิดระบบ
ทรัพยากรหลัก O2 น้ำ อาหาร ไฟ; สุขภาพและ morale แยกกัน; สตามินาวิ่งแยกจาก O2. HUD ใช้สี ไอคอน หลอด และหน้าอารมณ์ ค่าร้อยละบางชุด normalize จากยอดสำรองสูงสุดในรอบ ไม่ใช่ความดันถังจริง
DailySchedule มีช่วง 06:00–22:00. โค้ดล่าสุดเพิ่ม clock/pace และโครงการอิสระแล้ว ห้ามเชื่อเอกสารก่อนหน้าที่บอกว่าเดินไม่เสียเวลาเสมอ ต้องอ่าน Pace/Meal และจุด update ของ schedule ปัจจุบัน
มีต้นไม้ ปลูก/เก็บเกี่ยว, แผงสุริยะ, เจาะน้ำแข็ง, เครื่องผลิตอากาศ MOXIE, กิน/นอนรวมใน pod, project board เลือกโครงการได้สองครั้งต่อวัน, งานซ่อมใช้เครื่องมือ/อะไหล่และมินิเกม, สำรวจ/เก็บของนอกฐาน, รังสี, สภาพพายุ และเหตุฉุกเฉินไฟฟ้า/อากาศรั่ว/หลบพายุ. รายละเอียดและตัวเลขทั้งหมดดู config snapshot ด้านล่างและไฟล์เจ้าของระบบในสารบัญ
อาหารเย็นใช้ถาดอาหารลากไปให้แต่ละคนและเมนูรายคน; sim ยังตัดสินค่าอาหารและน้ำจริง มีบทสนทนาแบบ visual novel และคู่มือเอาชีวิตรอด
มี checkpoint และเซฟรายคืนด้วย PlayerPrefs (save_day); ต้องตรวจ version/schema และเงื่อนไข restore ใน Pace.cs ก่อนเปลี่ยน state. ไม่ใช่ cloud save
ผลลัพธ์มีรอด/ช่วยเหลือสำเร็จ/ติดค้าง/ล้มเหลวจากอากาศหรือสุขภาพ ตาม flags และวันของ sim. ตรวจ enum จริง; 7 วันไม่ได้แปลว่าชนะอัตโนมัติ

## 5. ตัวละคร สัตว์ และผู้มาเยือน
ผู้เล่น, ผู้การอรุณ, ORBI; สัตว์ dog Comet, cat Pixel, rabbit Luna มี trait ช่วยอ่านหลักฐาน. Pet menu: เล่น/รอ/ตาม/ปิด เป็น optional cosmetic; ไม่เสีย work hours หรือ supplies. ตอนอพยพเปิดตามกลับ เพื่อไม่ทิ้งสัตว์ไว้บนยาน
Aya นักพฤกษศาสตร์, Jules ช่าง; Tik สไลม์มินต์, Pip เจลชมพูจิ๋ว, Mu สิ่งมีชีวิตน้ำในถังล้อเลื่อน, Nok bioservice android ที่มีแกนชีวภาพ, Zee นักเดินทางมนุษย์ผู้ใหญ่. Source ล่าสุดมี visitor เพิ่มและการสุ่มแขกแล้ว ดูรายชื่อ config ด้านล่างและ RandomizeVisitors; รายการในเอกสารเก่าไม่ครบ
แขกมีค่าอยู่/ของช่วย/ผลข้างเคียงต่างกัน บางรายพักคืนเดียว (leavesAfterNight) แล้วออกโดย log GuestDeparted, บางรายอยู่ต่อหรืออาจก่อปัญหา. มี E สนทนาและเดินไปสถานีงาน; บางช่างช่วยเหตุฉุกเฉินได้
Pip มี wave/friendship flag และ return event. การตัดสินคนจากหน้าตาอย่างเดียวไม่ใช่หลักฐาน; สแกนอุณหภูมิ การหายใจ และจังหวะชีพจร/ระบบต้องอธิบายตามเผ่าพันธุ์. สิ่งมีชีวิตและสแกน alien เป็นเรื่องแต่ง

## 6. ภาพ เสียง UI และแอนิเมชัน
คนใช้ source 64×80 px, PPU 44; gait 8 จังหวะ หน้า/หลัง/ข้าง; สัตว์ใช้ sheet ขนาดต่างกัน. ArtActions มีนั่ง รดน้ำ ซ่อม ปลูก ให้อาหาร เล่น หยิบ ถือของ; เป็น sprite procedural ไม่ใช่ animation controller. งานรุ่นล่าสุดมี ArtJobs เพิ่ม ต้องตรวจด้วย
UI Toolkit, Chakra Petch, Thai Advanced Text Generator. ข้อความ source tools/strings_src.py และ story_strings.py → สร้าง strings.json. ห้ามแก้ generated strings.json อย่างเดียว
เมนู/พัก/ตั้งค่าใช้ theme ยาน, hover/focus/press animation, เสียง Kenney CC0 บางส่วนและ synthesized fallback. มี reduced motion. แสงเป็น built-in-compatible glow และ contact shadows ไม่ใช่ URP 2D
อ้างอิงภาพเก่าใช้ Eastward/เกมเอาชีวิตรอดเป็นแรงบันดาลใจ ไม่ได้ import sprite ของเกมเหล่านั้น. ใบอนุญาตเสียง/ฟอนต์อยู่ docs

## 7. บั๊กและสิ่งที่ต้องระวัง
- โซฟา: SImpact ย้ายผู้เล่นเข้า collider ตอนนั่ง ต้อง restore standingPosition ที่เดินถึงจริงก่อน scramble. ห้ามแก้ด้วยการ teleport ใน test จนซ่อนปัญหา
- Action lock ใช้ coroutine completion ไม่ใช่ timer แยกที่หมดก่อนหนึ่งเฟรม; เปลี่ยนโลกต้องยกเลิก callback ของโลกเก่า
- WalkBase ต้องปลด busy ของ diary หลังเลือกสัตว์/ลงฐาน ไม่เช่นนั้น input ถูกล็อก
- HUD/interaction prompt ต้องซ่อนตอนเล่น stage cinematic แล้วคืนเมื่อกลับมา
- Source ล่าสุดมีหลายงานจากเพื่อน/AI อื่น อย่า reset/clean/overwrite scene. ดู git status และ commit ก่อนเริ่ม. งานส่งต่อรอบนี้ไม่เปลี่ยน gameplay
- README/เอกสารสมัยก่อน/ผลทดสอบ 67 ตัวเป็นประวัติ ไม่ใช่คำยืนยันว่าฉบับล่าสุดผ่านแล้ว. Binaries ใน Builds อาจเก่ากว่า source
- ยังไม่มีเว็บไซต์, cloud save, voice acting, บทเล่นตามหาอรุณ หรือท่านั่งทุกวัตถุตกแต่ง. ภาพ/ความสนุก/บาลานซ์ยังต้อง human playtest

## 8. NASA: ใช้จริงแค่ไหน
**ใช้ข้อมูล/ความรู้ที่อ้าง NASA แบบ offline แล้ว แต่ไม่ใช่ live NASA API integration.** ค้น source ไม่พบ UnityWebRequest/HttpClient เรียก NASA. อย่าอ้างว่าเกมดึงข้อมูลสด
- RealFlares ใน GameFlow.Base.cs hard-code วัน/ระดับ/active region 6 เหตุการณ์จากที่ผู้พัฒนาอ้าง DONKI; เลือกหนึ่งเหตุการณ์ต่อรอบ. ยังต้องแนบ event IDs, query parameters, retrieved-at และ raw response เพื่อยืนยัน provenance ราย record ไม่ควรเรียกว่า dataset import ที่ตรวจครบแล้ว
- หน้า Science archive มี Curiosity RAD ~0.64 mSv/day และเกมปัด 0.7. NASA release อีกช่วงรายงาน 0.67 (Aug 2012–Jun 2013); ต้องกำกับเครื่องมือ ช่วงเวลา สถานที่และหน่วย ไม่สรุปเป็นค่าคงที่ทั่วดาวอังคาร
- MOXIE ใช้หลักผลิตออกซิเจนจาก CO2; ECLSS ใช้หลักควบคุมอากาศ/รีไซเคิลน้ำ; พืช/แสง/น้ำ และการสื่อสารเป็นความรู้ NASA. ค่าไฟ/อากาศที่ได้และเวลางานในเกมเป็น game balance ไม่ใช่สมการวิศวกรรม NASA
- วันอังคาร, landing sequence, radiation/storm และความรู้ใน cards ต้องแยก measured/reference fact ออกจาก approximation และ fiction
- จัมป์ เอเลี่ยน อรุณ Kestrel การกู้ภัย 7 วัน scanner ของเผ่าต่างดาว และค่าหลอด 0–100 เป็นเรื่องแต่ง/จำลอง
- ยังสรุปไม่ได้ว่าผ่านเกณฑ์ NASA Space Apps challenge โดยไม่เห็นโจทย์/ปี/เกณฑ์เฉพาะ. ถ้าบังคับ NASA datasets ควรเพิ่ม snapshot JSON/CSV ที่มี provenance, event ID/source URL และหลักฐานว่า records เปลี่ยนเหตุการณ์/ตัวเลือกในเกมอย่างไร. **ไม่จำเป็นต้องทำเว็บเพื่อใช้ NASA data**

แหล่ง NASA ที่ตรวจเว็บวันที่ส่งต่อ:
- DONKI: https://ccmc.gsfc.nasa.gov/tools/DONKI/
- API index: https://ccmc.gsfc.nasa.gov/tools/API/
- ECLSS: https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/
- RAD report: https://www.nasa.gov/news-release/nasa-rover-results-include-first-age-measurement-on-mars-and-help-for-human-exploration/

## 9. วิธีเริ่มงานของ AI ใหม่
เปิด Unity project โฟลเดอร์นี้ ไม่ใช่ F:/NASA/Naja. Scene Assets/Scenes/Main.unity. อ่าน active flags, LifeboatSim, GameFlow.Base/ShipTop/Layout และ partial ใหม่ก่อนแก้. อย่าเริ่มจาก README รุ่นเก่าเพียงไฟล์เดียว
Editor QA queue: Logs/polish-qa.request = tests / build / build-story เมื่อ Editor เปิด. ผล Logs/polish-tests.xml และ polish-build-result.txt. ตรวจว่า import source ล่าสุดจริงและ timestamp/hash ตรงก่อนเชื่อผล
Player QA flags: -autoshot -ship, -base, -uxshowcase, -storyshowcase; source ล่าสุดเพิ่ม -play (-fast เป็นตัวช่วยทดสอบ) เพื่อเล่นตั้งแต่ต้นพร้อม clock/emergencies. แต่ executable เก่าอาจยังไม่รองรับ flags ใหม่
หลักฐานก่อนหน้า: production/qa/evidence/story-2026-11-13/ และ docs/STORY_IMPLEMENTED_2026-11-13.md. ไฟล์ PLAYTEST_2026-11-14.md จะระบุผลรอบนี้และความใหม่ของ binary แยกจาก source

## 10. Snapshot ตัวเลข/ข้อมูลปัจจุบัน (อ่านค่าจาก config โดยตรง)

```json
{
  "chapter1": {
    "cabinO2Start": 100,
    "cabinDrainPerSec": 0.8,
    "cabinFloor": 10,
    "carryO2Pivot": 50,
    "carryO2Factor": 0.3,
    "scrambleSeconds": 60,
    "stowSlots": 5,
    "supplies": [
      {
        "id": "sup_o2",
        "delta": {
          "air": 12
        }
      },
      {
        "id": "sup_water",
        "delta": {
          "supplies": 8
        }
      },
      {
        "id": "sup_food",
        "delta": {
          "supplies": 10
        }
      },
      {
        "id": "sup_battery",
        "delta": {
          "power": 20
        }
      },
      {
        "id": "sup_photo",
        "delta": {
          "energy": 15
        }
      },
      {
        "id": "sup_medkit",
        "delta": {},
        "flag": "med_kit"
      },
      {
        "id": "sup_filter",
        "delta": {},
        "flag": "spare_filter"
      },
      {
        "id": "sup_seeds",
        "delta": {},
        "flag": "seed_pack"
      },
      {
        "id": "sup_cell",
        "delta": {},
        "flag": "data_chip"
      },
      {
        "id": "sup_shield",
        "delta": {},
        "flag": "shield_kit"
      }
    ]
  },
  "puzzles": {
    "lockerCode": "7",
    "podCode": "39",
    "fuelGaugePercent": 18,
    "navOptions": [
      {
        "id": "titan",
        "fuelNeeded": 60,
        "route": ""
      },
      {
        "id": "earth",
        "fuelNeeded": 35,
        "route": ""
      },
      {
        "id": "mars",
        "fuelNeeded": 15,
        "route": "mars"
      }
    ],
    "edlOrder": [
      "edl_entry",
      "edl_chute",
      "edl_shield",
      "edl_rockets"
    ],
    "catalysts": [
      "item_cat_fe",
      "item_cat_ni",
      "item_cat_cu"
    ],
    "catalystCorrect": "item_cat_ni",
    "recipes": [
      {
        "a": "item_plate",
        "b": "item_sealant",
        "result": "item_hull_patch"
      }
    ],
    "shiftTasksBeforeImpact": 2
  },
  "routes": [
    {
      "id": "mars",
      "edl": {
        "order": [
          "edl_entry",
          "edl_chute",
          "edl_shield",
          "edl_rockets"
        ],
        "distractors": []
      }
    }
  ],
  "cards": [
    {
      "id": "titan",
      "chapter": 1
    },
    {
      "id": "debris",
      "chapter": 1
    },
    {
      "id": "whipple",
      "chapter": 1
    },
    {
      "id": "deltav",
      "chapter": 1
    },
    {
      "id": "edl",
      "chapter": 2
    },
    {
      "id": "sol",
      "chapter": 2
    },
    {
      "id": "mars_air",
      "chapter": 2
    },
    {
      "id": "moxie",
      "chapter": 2
    },
    {
      "id": "water_recycle",
      "chapter": 2
    },
    {
      "id": "ice",
      "chapter": 2
    },
    {
      "id": "solar",
      "chapter": 2
    },
    {
      "id": "sabatier",
      "chapter": 2
    },
    {
      "id": "radiation",
      "chapter": 2
    },
    {
      "id": "dust_storm",
      "chapter": 2
    },
    {
      "id": "rad_dose",
      "chapter": 1
    },
    {
      "id": "veggie",
      "chapter": 1
    },
    {
      "id": "mars_sunset",
      "chapter": 2
    },
    {
      "id": "mars_lake",
      "chapter": 2
    },
    {
      "id": "mars_pressure",
      "chapter": 2
    },
    {
      "id": "regolith",
      "chapter": 2
    },
    {
      "id": "isolation",
      "chapter": 1
    },
    {
      "id": "co2_frost",
      "chapter": 2
    },
    {
      "id": "comm_delay",
      "chapter": 2
    },
    {
      "id": "sep_2017",
      "chapter": 2
    },
    {
      "id": "eclss",
      "chapter": 2
    }
  ],
  "presentation": {
    "walkSpeed": 3.6,
    "actionSeconds": 1.2,
    "roomSpacing": 16,
    "cameraMoveSeconds": 0.5,
    "textScale": 1.0
  },
  "outpost": {
    "hoursTotal": 168,
    "landingClock": 6,
    "maxValue": 100,
    "start": {
      "air": 70,
      "power": 50,
      "shelter": 0,
      "supplies": 55,
      "energy": 80
    },
    "airUsePerHour": 1.4,
    "lifeSupportAirPerHour": 1.0,
    "systems": [
      {
        "id": "life_support",
        "powerPerHour": 2.5,
        "onAtStart": true
      },
      {
        "id": "greenhouse",
        "powerPerHour": 2.0,
        "onAtStart": false
      },
      {
        "id": "beacon",
        "powerPerHour": 1.0,
        "onAtStart": false
      },
      {
        "id": "shelter",
        "powerPerHour": 2.0,
        "onAtStart": false
      }
    ],
    "solarPerHour": 5.5,
    "daylightFrom": 7,
    "daylightTo": 19,
    "dustPerDay": 0.12,
    "minEfficiency": 0.4,
    "suppliesUsePerHour": 0.45,
    "energyAwakePerHour": -1.4,
    "energySleepPerHour": 7.0,
    "lowEnergy": 20,
    "lowEnergyExtraHours": 1,
    "collapseSleepHours": 6,
    "suppliesZeroFailHours": 24,
    "warnAir": 25,
    "critAir": 10,
    "warnSupplies": 20,
    "warnPower": 15,
    "radPerHour": 0.029,
    "radPerOutsideHour": 0.05,
    "radSafe": 50,
    "radLimit": 150,
    "spe": {
      "hour": 84,
      "durationHours": 6,
      "dose": 80,
      "energyLoss": 40,
      "airLoss": 35,
      "unpoweredShelterFactor": 0.5,
      "warningHour": 28,
      "powerLoss": 40
    },
    "relayClocks": [
      10,
      22
    ],
    "rescuePassEveryHours": 6,
    "commDelayMinutes": 14,
    "greenhouseHoursToReady": 24,
    "nightEventClock": 2,
    "randomEventDays": [
      1,
      2,
      4,
      5,
      6
    ],
    "encounterClock": 8,
    "encounterDays": [
      2,
      3,
      4,
      5,
      6
    ],
    "actions": [
      {
        "id": "moxie",
        "hours": 2,
        "delta": {
          "power": -6,
          "air": 14
        },
        "hotspot": "s_o2gen",
        "card": "moxie"
      },
      {
        "id": "recycle",
        "hours": 2,
        "delta": {
          "power": -4,
          "supplies": 6
        },
        "hotspot": "s_recycler",
        "card": "water_recycle"
      },
      {
        "id": "drill_ice",
        "hours": 3,
        "delta": {
          "energy": -12,
          "supplies": 12
        },
        "requiresFlag": "drill_ready",
        "outside": true,
        "hotspot": "mars_drill",
        "card": "ice"
      },
      {
        "id": "sabatier",
        "hours": 2,
        "delta": {
          "power": -6,
          "supplies": 8
        },
        "requiresFlag": "catalyst_installed",
        "hotspot": "s_reactor",
        "card": "sabatier"
      },
      {
        "id": "eat",
        "hours": 1,
        "delta": {
          "supplies": -6,
          "energy": 25
        },
        "hotspot": "s_food"
      },
      {
        "id": "clean_panels",
        "hours": 2,
        "delta": {
          "energy": -8
        },
        "outside": true,
        "cleansPanels": true,
        "hotspot": "mars_solar",
        "card": "solar"
      },
      {
        "id": "stow_panels",
        "hours": 1,
        "delta": {
          "energy": -4
        },
        "setsFlag": "panels_stowed",
        "solFlag": true,
        "outside": true,
        "hotspot": "mars_solar"
      },
      {
        "id": "inspect_seals",
        "hours": 1,
        "delta": {
          "energy": -3
        },
        "setsFlag": "seals_inspected",
        "solFlag": true,
        "hotspot": "s_seals"
      },
      {
        "id": "shelter_water",
        "hours": 2,
        "delta": {
          "supplies": -10,
          "shelter": 25
        },
        "hotspot": "s_bed",
        "card": "radiation"
      },
      {
        "id": "shelter_regolith",
        "hours": 4,
        "delta": {
          "energy": -20,
          "shelter": 35
        },
        "outside": true,
        "hotspot": "mars_scrap",
        "card": "regolith"
      },
      {
        "id": "shelter_kit",
        "hours": 1,
        "delta": {
          "shelter": 40
        },
        "requiresFlag": "shield_kit",
        "consumesFlag": true,
        "hotspot": "s_bed"
      },
      {
        "id": "repair_beacon",
        "hours": 3,
        "delta": {
          "energy": -8
        },
        "requiresFlag": "has_hull_patch",
        "setsFlag": "beacon_repaired",
        "hotspot": "s_line"
      },
      {
        "id": "repair_beacon_chip",
        "hours": 1,
        "delta": {
          "energy": -2
        },
        "requiresFlag": "data_chip",
        "setsFlag": "beacon_repaired",
        "hotspot": "s_line"
      },
      {
        "id": "send_coords",
        "hours": 1,
        "delta": {},
        "requiresFlag": "beacon_repaired",
        "needsSystem": "beacon",
        "hotspot": "s_nav"
      },
      {
        "id": "ask_earth",
        "hours": 1,
        "delta": {},
        "hotspot": "s_nav"
      },
      {
        "id": "setup_greenhouse",
        "hours": 3,
        "delta": {
          "energy": -6
        },
        "setsFlag": "greenhouse_started",
        "hotspot": "s_food",
        "card": "veggie"
      },
      {
        "id": "setup_greenhouse_seeds",
        "hours": 1,
        "delta": {},
        "requiresFlag": "seed_pack",
        "setsFlag": "greenhouse_started",
        "hotspot": "s_food",
        "card": "veggie"
      },
      {
        "id": "sci_sunset",
        "hours": 1,
        "delta": {
          "energy": -3
        },
        "science": true,
        "minDay": 1,
        "setsFlag": "sci_sunset",
        "hotspot": "s_scope",
        "card": "mars_sunset"
      },
      {
        "id": "sci_core",
        "hours": 2,
        "delta": {
          "energy": -6
        },
        "science": true,
        "minDay": 3,
        "setsFlag": "sci_core",
        "requiresFlag": "drill_ready",
        "outside": true,
        "hotspot": "mars_drill",
        "card": "mars_lake"
      },
      {
        "id": "sci_weather",
        "hours": 1,
        "delta": {
          "energy": -3
        },
        "science": true,
        "minDay": 5,
        "setsFlag": "sci_weather",
        "outside": true,
        "hotspot": "mars_solar",
        "card": "mars_pressure"
      }
    ],
    "nightEvents": [
      {
        "id": "dust_devil",
        "preventFlag": "panels_stowed",
        "penalty": {},
        "efficiencyLoss": 0.3
      },
      {
        "id": "micro_leak",
        "preventFlag": "seals_inspected",
        "penalty": {
          "air": -15
        }
      },
      {
        "id": "minor_flare",
        "preventMinShelter": 30,
        "penalty": {
          "energy": -15
        },
        "dose": 8
      }
    ],
    "incidents": [
      {
        "id": "recycler_fire",
        "day": 2,
        "afterActions": 2,
        "seconds": 30,
        "hotspot": "s_recycler",
        "penalty": {
          "supplies": -10,
          "power": -6
        }
      },
      {
        "id": "hatch_leak",
        "day": 3,
        "afterActions": 3,
        "seconds": 30,
        "hotspot": "s_airlock",
        "penalty": {
          "air": -12
        }
      },
      {
        "id": "scrubber_clog",
        "day": 5,
        "afterActions": 2,
        "seconds": 30,
        "hotspot": "s_o2gen",
        "penalty": {
          "air": -12
        }
      }
    ],
    "dilemmas": [
      {
        "id": "mars_rover",
        "day": 6,
        "options": [
          {
            "id": "fetch",
            "hours": 2,
            "delta": {
              "power": 12,
              "energy": -12
            },
            "dose": 1.0
          },
          {
            "id": "stay",
            "hours": 0,
            "delta": {}
          }
        ]
      },
      {
        "id": "orbi_rest",
        "random": true,
        "options": [
          {
            "id": "rest",
            "hours": 0,
            "delta": {
              "power": 8
            },
            "setsFlag": "orbi_off",
            "solFlag": true
          },
          {
            "id": "stay_on",
            "hours": 0,
            "delta": {
              "power": -4
            }
          }
        ]
      },
      {
        "id": "supply_pod",
        "random": true,
        "options": [
          {
            "id": "fetch",
            "hours": 2,
            "delta": {
              "energy": -15,
              "supplies": 15
            },
            "dose": 0.5
          },
          {
            "id": "leave",
            "hours": 0,
            "delta": {}
          }
        ]
      },
      {
        "id": "family_message",
        "random": true,
        "options": [
          {
            "id": "watch",
            "hours": 1,
            "delta": {
              "energy": 20
            },
            "card": "isolation"
          },
          {
            "id": "later",
            "hours": 0,
            "delta": {}
          }
        ]
      },
      {
        "id": "greenhouse_leak",
        "random": true,
        "options": [
          {
            "id": "replant",
            "hours": 0,
            "delta": {},
            "needFlag": "seed_pack"
          },
          {
            "id": "patch",
            "hours": 1,
            "delta": {
              "energy": -8
            }
          },
          {
            "id": "let_freeze",
            "hours": 0,
            "delta": {
              "supplies": -8
            }
          }
        ]
      },
      {
        "id": "spare_parts",
        "random": true,
        "options": [
          {
            "id": "sort",
            "hours": 1,
            "delta": {
              "energy": -5
            },
            "setsFlag": "spare_filter"
          },
          {
            "id": "leave_box",
            "hours": 0,
            "delta": {}
          }
        ]
      },
      {
        "id": "filter_wear",
        "random": true,
        "options": [
          {
            "id": "swap",
            "hours": 0,
            "delta": {},
            "needFlag": "spare_filter"
          },
          {
            "id": "clean_by_hand",
            "hours": 1,
            "delta": {
              "energy": -10,
              "air": -6
            }
          }
        ]
      },
      {
        "id": "cut_hand",
        "random": true,
        "options": [
          {
            "id": "medkit",
            "hours": 0,
            "delta": {},
            "needFlag": "med_kit"
          },
          {
            "id": "bandage",
            "hours": 1,
            "delta": {}
          },
          {
            "id": "ignore",
            "hours": 0,
            "delta": {
              "energy": -20
            }
          }
        ]
      },
      {
        "id": "mars_frost",
        "random": true,
        "options": [
          {
            "id": "scrape",
            "hours": 1,
            "delta": {
              "energy": -10
            },
            "card": "co2_frost"
          },
          {
            "id": "wait",
            "hours": 0,
            "delta": {
              "power": -10
            },
            "card": "co2_frost"
          }
        ]
      }
    ]
  },
  "lifeboat": {
    "daysTotal": 7,
    "sliceDays": 0,
    "health": 3,
    "start": {
      "o2": 11,
      "water": 8,
      "food": 6,
      "power": 4
    },
    "crewPerDay": {
      "o2": 1,
      "water": 1,
      "food": 1,
      "power": 0
    },
    "lifeSupportPower": 1,
    "solarPerDay": 2,
    "o2PenaltyNoPower": 1,
    "o2PenaltyClogged": 1,
    "pets": [
      {
        "id": "comet",
        "trait": "brave",
        "foodPerDay": 1
      },
      {
        "id": "pixel",
        "trait": "smart",
        "foodPerDay": 1
      },
      {
        "id": "luna",
        "trait": "careful",
        "foodPerDay": 1
      }
    ],
    "visitors": [
      {
        "id": "tik",
        "perDay": {
          "o2": 1,
          "water": 1,
          "food": 1,
          "power": 0
        },
        "giftPerDay": {
          "o2": 0,
          "water": 1,
          "food": 0,
          "power": 1
        },
        "steals": false,
        "stealFood": 0,
        "monster": false,
        "failsScans": []
      },
      {
        "id": "mu",
        "perDay": {
          "o2": 1,
          "water": 1,
          "food": 1,
          "power": 0
        },
        "giftPerDay": {
          "o2": 0,
          "water": 0,
          "food": 0,
          "power": 0
        },
        "steals": true,
        "stealFood": 2,
        "monster": false,
        "failsScans": [],
        "leavesAfterNight": true
      },
      {
        "id": "zee",
        "perDay": {
          "o2": 0,
          "water": 0,
          "food": 0,
          "power": 0
        },
        "giftPerDay": {
          "o2": 0,
          "water": 0,
          "food": 0,
          "power": 0
        },
        "steals": false,
        "stealFood": 0,
        "monster": false,
        "failsScans": []
      },
      {
        "id": "nok",
        "perDay": {
          "o2": 1,
          "water": 1,
          "food": 1,
          "power": 0
        },
        "giftPerDay": {
          "o2": 0,
          "water": 0,
          "food": 0,
          "power": 0
        },
        "steals": false,
        "stealFood": 0,
        "monster": false,
        "failsScans": []
      },
      {
        "id": "shade",
        "perDay": {
          "o2": 1,
          "water": 1,
          "food": 1,
          "power": 0
        },
        "giftPerDay": {
          "o2": 0,
          "water": 0,
          "food": 0,
          "power": 0
        },
        "steals": false,
        "stealFood": 0,
        "monster": true,
        "failsScans": [
          "thermal",
          "breath"
        ]
      },
      {
        "id": "ree",
        "perDay": {
          "o2": 1,
          "water": 1,
          "food": 1,
          "power": 0
        },
        "giftPerDay": {
          "o2": 2,
          "water": 0,
          "food": 0,
          "power": 0
        },
        "steals": false,
        "stealFood": 0,
        "monster": false,
        "failsScans": []
      },
      {
        "id": "mimic",
        "perDay": {
          "o2": 1,
          "water": 1,
          "food": 1,
          "power": 0
        },
        "giftPerDay": {
          "o2": 0,
          "water": 0,
          "food": 0,
          "power": 0
        },
        "steals": false,
        "stealFood": 0,
        "monster": true,
        "failsScans": [
          "breath"
        ]
      },
      {
        "id": "aya",
        "perDay": {
          "o2": 1,
          "water": 1,
          "food": 1,
          "power": 0
        },
        "giftPerDay": {
          "o2": 0,
          "water": 0,
          "food": 0,
          "power": 0
        },
        "steals": false,
        "stealFood": 0,
        "monster": false,
        "failsScans": []
      },
      {
        "id": "jules",
        "perDay": {
          "o2": 1,
          "water": 1,
          "food": 1,
          "power": 0
        },
        "giftPerDay": {
          "o2": 0,
          "water": 0,
          "food": 0,
          "power": 0
        },
        "steals": false,
        "stealFood": 0,
        "monster": false,
        "failsScans": []
      },
      {
        "id": "pip",
        "perDay": {
          "o2": 0,
          "water": 0,
          "food": 0,
          "power": 0
        },
        "giftPerDay": {},
        "monster": false,
        "failsScans": []
      },
      {
        "id": "kira",
        "perDay": {
          "o2": 1,
          "water": 1,
          "food": 1,
          "power": 0
        },
        "giftPerDay": {
          "o2": 0,
          "water": 0,
          "food": 1,
          "power": 0
        },
        "steals": false,
        "stealFood": 0,
        "monster": false,
        "failsScans": []
      },
      {
        "id": "rex",
        "perDay": {
          "o2": 1,
          "water": 1,
          "food": 1,
          "power": 0
        },
        "giftPerDay": {
          "o2": 0,
          "water": 0,
          "food": 0,
          "power": 1
        },
        "steals": false,
        "stealFood": 0,
        "monster": false,
        "failsScans": []
      },
      {
        "id": "vela",
        "perDay": {
          "o2": 0,
          "water": 0,
          "food": 0,
          "power": 0
        },
        "giftPerDay": {
          "o2": 0,
          "water": 0,
          "food": 0,
          "power": 0
        },
        "steals": false,
        "stealFood": 0,
        "monster": false,
        "failsScans": []
      },
      {
        "id": "bizz",
        "perDay": {
          "o2": 1,
          "water": 0,
          "food": 1,
          "power": 0
        },
        "giftPerDay": {
          "o2": 0,
          "water": 0,
          "food": 1,
          "power": 0
        },
        "steals": false,
        "stealFood": 0,
        "monster": false,
        "failsScans": []
      }
    ],
    "cards": [
      {
        "id": "filter",
        "day": 1,
        "visitor": "",
        "clues": 0,
        "options": [
          {
            "id": "swap",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "needItem": "spare_filter",
            "consumesItem": true
          },
          {
            "id": "wash",
            "delta": {
              "o2": 0,
              "water": -1,
              "food": 0,
              "power": 0
            }
          },
          {
            "id": "ignore",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "setsFlag": "filter_clogged"
          }
        ]
      },
      {
        "id": "visitor_tik",
        "day": 1,
        "visitor": "tik",
        "clues": 3,
        "options": [
          {
            "id": "admit",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "admits": "tik"
          },
          {
            "id": "trade",
            "delta": {
              "o2": 0,
              "water": -1,
              "food": 0,
              "power": 2
            }
          },
          {
            "id": "refuse",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "crate",
        "day": 2,
        "visitor": "",
        "clues": 0,
        "options": [
          {
            "id": "walk",
            "delta": {
              "o2": -1,
              "water": 1,
              "food": 2,
              "power": 0
            }
          },
          {
            "id": "send_pet",
            "delta": {
              "o2": 0,
              "water": 1,
              "food": 2,
              "power": 0
            },
            "needTrait": "brave"
          },
          {
            "id": "leave",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "visitor_mu",
        "day": 2,
        "visitor": "mu",
        "clues": 3,
        "options": [
          {
            "id": "admit",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "admits": "mu"
          },
          {
            "id": "gift",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": -1,
              "power": 0
            },
            "setsFlag": "mu_fed"
          },
          {
            "id": "refuse",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "locker",
        "day": 2,
        "visitor": "",
        "clues": 0,
        "options": [
          {
            "id": "lock",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": -1
            },
            "setsFlag": "locker_locked"
          },
          {
            "id": "open",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "recycler",
        "day": 3,
        "visitor": "",
        "clues": 0,
        "options": [
          {
            "id": "repair",
            "delta": {
              "o2": 0,
              "water": 2,
              "food": 0,
              "power": -1
            }
          },
          {
            "id": "tik_repair",
            "delta": {
              "o2": 0,
              "water": 3,
              "food": 0,
              "power": 0
            },
            "needGuest": "tik"
          },
          {
            "id": "skip",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "storm_warning",
        "day": 3,
        "visitor": "",
        "clues": 0,
        "options": [
          {
            "id": "water_wall",
            "delta": {
              "o2": 0,
              "water": -2,
              "food": 0,
              "power": 0
            },
            "setsFlag": "shelter_ready"
          },
          {
            "id": "shield_kit",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "needItem": "shield_kit",
            "consumesItem": true,
            "setsFlag": "shelter_ready"
          },
          {
            "id": "later",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "visitor_zee",
        "day": 3,
        "visitor": "zee",
        "clues": 3,
        "options": [
          {
            "id": "buy_o2",
            "delta": {
              "o2": 2,
              "water": 0,
              "food": 0,
              "power": -2
            }
          },
          {
            "id": "buy_food",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 2,
              "power": -2
            }
          },
          {
            "id": "refuse",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "storm_last",
        "day": 4,
        "visitor": "",
        "clues": 0,
        "unlessFlag": "shelter_ready",
        "options": [
          {
            "id": "sandbags",
            "delta": {
              "o2": -1,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "setsFlag": "shelter_ready"
          },
          {
            "id": "send_pet",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "needTrait": "brave",
            "setsFlag": "shelter_ready"
          },
          {
            "id": "hide",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "storm_power",
        "day": 4,
        "visitor": "",
        "clues": 0,
        "options": [
          {
            "id": "safe_mode",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": -1
            },
            "setsFlag": "electronics_safe"
          },
          {
            "id": "leave_on",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "visitor_shade",
        "day": 4,
        "visitor": "shade",
        "clues": 3,
        "ifFlag": "",
        "unlessFlag": "",
        "ifGuest": "",
        "options": [
          {
            "id": "admit",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "admits": "shade"
          },
          {
            "id": "refuse",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "visitor_nok",
        "day": 5,
        "visitor": "nok",
        "clues": 3,
        "options": [
          {
            "id": "admit",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "admits": "nok",
            "setsFlag": "nok_helped"
          },
          {
            "id": "treat",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "needItem": "med_kit",
            "consumesItem": true,
            "setsFlag": "nok_helped"
          },
          {
            "id": "water",
            "delta": {
              "o2": 0,
              "water": -1,
              "food": 0,
              "power": 0
            },
            "setsFlag": "nok_helped"
          },
          {
            "id": "refuse",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "mu_returns",
        "day": 5,
        "visitor": "mu",
        "clues": 0,
        "ifFlag": "mu_fed",
        "options": [
          {
            "id": "accept",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 2,
              "power": 0
            }
          },
          {
            "id": "decline",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "far_crate",
        "day": 6,
        "visitor": "",
        "clues": 0,
        "options": [
          {
            "id": "walk",
            "delta": {
              "o2": -2,
              "water": 1,
              "food": 3,
              "power": 0
            }
          },
          {
            "id": "send_pet",
            "delta": {
              "o2": 0,
              "water": 1,
              "food": 3,
              "power": 0
            },
            "needTrait": "brave"
          },
          {
            "id": "leave",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "beacon",
        "day": 6,
        "visitor": "",
        "clues": 0,
        "options": [
          {
            "id": "nok_part",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 2,
              "power": 0
            },
            "needFlag": "nok_helped",
            "setsFlag": "beacon_fixed"
          },
          {
            "id": "tik_fix",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": -1
            },
            "needGuest": "tik",
            "setsFlag": "beacon_fixed"
          },
          {
            "id": "chip",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "needItem": "data_chip",
            "consumesItem": true,
            "setsFlag": "beacon_fixed"
          },
          {
            "id": "climb",
            "delta": {
              "o2": -1,
              "water": 0,
              "food": 0,
              "power": -2
            },
            "setsFlag": "beacon_fixed"
          },
          {
            "id": "wait",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "coords",
        "day": 6,
        "visitor": "",
        "clues": 0,
        "ifFlag": "beacon_fixed",
        "options": [
          {
            "id": "send",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": -1
            },
            "setsFlag": "coords_sent"
          },
          {
            "id": "crank",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "health": -1,
            "setsFlag": "coords_sent"
          },
          {
            "id": "wait",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "visitor_ree",
        "day": 6,
        "visitor": "ree",
        "clues": 3,
        "ifFlag": "",
        "unlessFlag": "",
        "ifGuest": "",
        "options": [
          {
            "id": "admit",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "admits": "ree"
          },
          {
            "id": "trade",
            "delta": {
              "o2": 0,
              "water": -1,
              "food": 1,
              "power": 0
            }
          },
          {
            "id": "refuse",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "beacon_last",
        "day": 7,
        "visitor": "",
        "clues": 0,
        "unlessFlag": "beacon_fixed",
        "options": [
          {
            "id": "nok_part",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 2,
              "power": 0
            },
            "needFlag": "nok_helped",
            "setsFlag": "beacon_fixed"
          },
          {
            "id": "tik_fix",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": -1
            },
            "needGuest": "tik",
            "setsFlag": "beacon_fixed"
          },
          {
            "id": "chip",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "needItem": "data_chip",
            "consumesItem": true,
            "setsFlag": "beacon_fixed"
          },
          {
            "id": "climb",
            "delta": {
              "o2": -1,
              "water": 0,
              "food": 0,
              "power": -2
            },
            "setsFlag": "beacon_fixed"
          },
          {
            "id": "give_up",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "coords_last",
        "day": 7,
        "visitor": "",
        "clues": 0,
        "ifFlag": "beacon_fixed",
        "unlessFlag": "coords_sent",
        "options": [
          {
            "id": "send",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": -1
            },
            "setsFlag": "coords_sent"
          },
          {
            "id": "crank",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "health": -1,
            "setsFlag": "coords_sent"
          },
          {
            "id": "skip",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "visitor_mimic",
        "day": 7,
        "visitor": "mimic",
        "clues": 3,
        "ifFlag": "",
        "unlessFlag": "",
        "ifGuest": "",
        "options": [
          {
            "id": "admit",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "admits": "mimic"
          },
          {
            "id": "refuse",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "log",
        "day": 7,
        "visitor": "",
        "clues": 0,
        "options": [
          {
            "id": "air",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          },
          {
            "id": "water",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          },
          {
            "id": "friends",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "visitor_aya",
        "day": 2,
        "visitor": "aya",
        "clues": 3,
        "options": [
          {
            "id": "admit",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "admits": "aya"
          },
          {
            "id": "refuse",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "visitor_jules",
        "day": 3,
        "visitor": "jules",
        "clues": 3,
        "options": [
          {
            "id": "admit",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "admits": "jules"
          },
          {
            "id": "refuse",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            }
          }
        ]
      },
      {
        "id": "visitor_pip",
        "day": 1,
        "visitor": "pip",
        "clues": 3,
        "options": [
          {
            "id": "wave",
            "delta": {},
            "setsFlag": "pip_friend"
          },
          {
            "id": "trade",
            "delta": {
              "water": -0.5,
              "power": 1
            }
          },
          {
            "id": "refuse",
            "delta": {}
          }
        ]
      },
      {
        "id": "pip_return",
        "day": 4,
        "visitor": "pip",
        "ifFlag": "pip_friend",
        "clues": 2,
        "options": [
          {
            "id": "accept",
            "delta": {
              "food": 1,
              "water": 1
            }
          },
          {
            "id": "refuse",
            "delta": {}
          }
        ]
      },
      {
        "id": "visitor_kira",
        "day": 3,
        "visitor": "kira",
        "clues": 3,
        "ifFlag": "",
        "unlessFlag": "",
        "ifGuest": "",
        "options": [
          {
            "id": "admit",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "needItem": "",
            "consumesItem": false,
            "needTrait": "",
            "admits": "kira",
            "setsFlag": "",
            "needFlag": "",
            "needGuest": "",
            "health": 0
          },
          {
            "id": "snack",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": -1,
              "power": 0
            },
            "needItem": "",
            "consumesItem": false,
            "needTrait": "",
            "admits": "",
            "setsFlag": "kira_friend",
            "needFlag": "",
            "needGuest": "",
            "health": 0
          },
          {
            "id": "refuse",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "needItem": "",
            "consumesItem": false,
            "needTrait": "",
            "admits": "",
            "setsFlag": "",
            "needFlag": "",
            "needGuest": "",
            "health": 0
          }
        ]
      },
      {
        "id": "visitor_rex",
        "day": 5,
        "visitor": "rex",
        "clues": 3,
        "ifFlag": "",
        "unlessFlag": "",
        "ifGuest": "",
        "options": [
          {
            "id": "admit",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "needItem": "",
            "consumesItem": false,
            "needTrait": "",
            "admits": "rex",
            "setsFlag": "",
            "needFlag": "",
            "needGuest": "",
            "health": 0
          },
          {
            "id": "swap",
            "delta": {
              "o2": 0,
              "water": -1,
              "food": 0,
              "power": 2
            },
            "needItem": "",
            "consumesItem": false,
            "needTrait": "",
            "admits": "",
            "setsFlag": "",
            "needFlag": "",
            "needGuest": "",
            "health": 0
          },
          {
            "id": "refuse",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "needItem": "",
            "consumesItem": false,
            "needTrait": "",
            "admits": "",
            "setsFlag": "",
            "needFlag": "",
            "needGuest": "",
            "health": 0
          }
        ]
      },
      {
        "id": "visitor_vela",
        "day": 2,
        "visitor": "vela",
        "clues": 3,
        "ifFlag": "",
        "unlessFlag": "",
        "ifGuest": "",
        "options": [
          {
            "id": "thanks",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "needItem": "",
            "consumesItem": false,
            "needTrait": "",
            "admits": "",
            "setsFlag": "",
            "needFlag": "",
            "needGuest": "",
            "health": 0
          },
          {
            "id": "gift",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": -1,
              "power": 0
            },
            "needItem": "",
            "consumesItem": false,
            "needTrait": "",
            "admits": "",
            "setsFlag": "vela_friend",
            "needFlag": "",
            "needGuest": "",
            "health": 1
          },
          {
            "id": "refuse",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "needItem": "",
            "consumesItem": false,
            "needTrait": "",
            "admits": "",
            "setsFlag": "",
            "needFlag": "",
            "needGuest": "",
            "health": 0
          }
        ]
      },
      {
        "id": "visitor_bizz",
        "day": 4,
        "visitor": "bizz",
        "clues": 3,
        "ifFlag": "",
        "unlessFlag": "",
        "ifGuest": "",
        "options": [
          {
            "id": "admit",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "needItem": "",
            "consumesItem": false,
            "needTrait": "",
            "admits": "bizz",
            "setsFlag": "",
            "needFlag": "",
            "needGuest": "",
            "health": 0
          },
          {
            "id": "honey",
            "delta": {
              "o2": 0,
              "water": -1,
              "food": 2,
              "power": 0
            },
            "needItem": "",
            "consumesItem": false,
            "needTrait": "",
            "admits": "",
            "setsFlag": "",
            "needFlag": "",
            "needGuest": "",
            "health": 0
          },
          {
            "id": "refuse",
            "delta": {
              "o2": 0,
              "water": 0,
              "food": 0,
              "power": 0
            },
            "needItem": "",
            "consumesItem": false,
            "needTrait": "",
            "admits": "",
            "setsFlag": "",
            "needFlag": "",
            "needGuest": "",
            "health": 0
          }
        ]
      }
    ],
    "supplies": [
      {
        "id": "sup_o2",
        "stock": {
          "o2": 3,
          "water": 0,
          "food": 0,
          "power": 0
        }
      },
      {
        "id": "sup_water",
        "stock": {
          "o2": 0,
          "water": 3,
          "food": 0,
          "power": 0
        }
      },
      {
        "id": "sup_food",
        "stock": {
          "o2": 0,
          "water": 0,
          "food": 3,
          "power": 0
        }
      },
      {
        "id": "sup_battery",
        "stock": {
          "o2": 0,
          "water": 0,
          "food": 0,
          "power": 3
        }
      }
    ],
    "demoStowed": [
      "sup_water",
      "sup_food",
      "sup_battery",
      "sup_filter",
      "sup_medkit"
    ],
    "nights": [
      {
        "id": "storm_crew",
        "day": 4,
        "safeFlag": "shelter_ready",
        "healthLoss": 1,
        "delta": {
          "o2": 0,
          "water": 0,
          "food": 0,
          "power": 0
        }
      },
      {
        "id": "storm_power",
        "day": 4,
        "safeFlag": "electronics_safe",
        "healthLoss": 0,
        "delta": {
          "o2": 0,
          "water": 0,
          "food": 0,
          "power": -2
        }
      }
    ],
    "beaconFlag": "beacon_fixed",
    "rescueFlag": "coords_sent",
    "kindFlag": "nok_helped",
    "o2Maker": {
      "o2": 2,
      "water": 0,
      "food": 0,
      "power": 1.5
    },
    "scans": [
      "thermal",
      "breath"
    ],
    "scansPerVisitor": 2,
    "extraScanTrait": "careful",
    "replacesOutpost": true
  }
}
```

## 11. สารบัญโค้ดปัจจุบันและคำอธิบายในไฟล์

- `Assets/Scripts/Core/DailySchedule.cs` — Action-based day, 06:00–22:00. Exploration and reading never spend time.

- `Assets/Scripts/Core/GameConfig.cs` — A change to the outpost values. Used by actions, events, supplies and dilemma answers.

- `Assets/Scripts/Core/LifeboatSim.cs` — The four things the lifeboat crew lives on. One unit is enough for one person for one day.

- `Assets/Scripts/Core/Localization.cs` — Two-language string table. Missing keys show as #key so they are easy to spot.

- `Assets/Scripts/Core/OutpostSim.cs` — One thing that happened, for the HUD log, the morning report and the debrief.

- `Assets/Scripts/Core/Puzzles.cs` — Items the astronaut carries. Order is kept so the bar does not reshuffle.

- `Assets/Scripts/Core/SprintMeter.cs` — Inspect implementation; no summary header.

- `Assets/Scripts/Editor/PolishQARunner.cs` — Opt-in local QA queue for an already-open Editor. No scene replacement or runtime injection.

- `Assets/Scripts/Editor/ProjectSetup.cs` — Creates the one scene the game needs (everything else is built in code) and registers it.

- `Assets/Scripts/Editor/TextShapingSetup.cs` — The game requests advanced shaping for Thai; enable the matching Unity 6 project setting.

- `Assets/Scripts/Game/Actors.cs` — The junior astronaut: walks to a spot and plays a visible action for everything they do.

- `Assets/Scripts/Game/Art.cs` — Palette. Navy = ship, cyan = screens/AI/hotspots, rust = Mars, amber = warning, red = critical.

- `Assets/Scripts/Game/ArtActions.cs` — Inspect implementation; no summary header.

- `Assets/Scripts/Game/ArtBase.cs` — Top-down three-quarter sprites for the walkable Mars base: floor tiles, walls, plots, markers, light.

- `Assets/Scripts/Game/ArtCrew.cs` — Inspect implementation; no summary header.

- `Assets/Scripts/Game/ArtCrewHD.cs` — Inspect implementation; no summary header.

- `Assets/Scripts/Game/ArtExterior.cs` — Exterior art for the title screen and cutscenes: the ship, its flames, planets, effects.

- `Assets/Scripts/Game/ArtJobs.cs` — Repair parts and carried things, 16x16 icons (y = 0 is the bottom row).

- `Assets/Scripts/Game/ArtLifeboat.cs` — Pets and visitors for the survival diary, drawn in code like the rest of the game (feet at y = 0). The monsters are drawn to pass a quick look; only a scan or a careful read of the clues gives them away.

- `Assets/Scripts/Game/ArtVisitorForms.cs` — Inspect implementation; no summary header.

- `Assets/Scripts/Game/GameFlow.Actions.cs` — Inspect implementation; no summary header.

- `Assets/Scripts/Game/GameFlow.AutoShot.cs` — Verification mode: run the player with "-autoshot -route mars|moon|titan". It plays the route through the real click handlers and modal buttons, saves a screenshot of every screen to Captures/&lt;route&gt;/, logs every error and every check, then quits.

- `Assets/Scripts/Game/GameFlow.Base.cs` — Walkable survival week. The same LifeboatSim rules and day cards, but each card waits at a place in a top-down Mars base (a knock at the airlock, a storm alarm at the power panel, the beacon outside). The cadet walks there and opens it. Between events there are hands-on jobs that trade one stock for another.

- `Assets/Scripts/Game/GameFlow.BaseShot.cs` — Screenshot pass for the walkable base (-autoshot -base): walks to every event through the real walking code.

- `Assets/Scripts/Game/GameFlow.Ch1.cs` — Chapter 1: the ship Kestrel. Morning shift, debris impact with a real-time oxygen leak, seal, pick any destination (each is its own Chapter 2), land with that world's procedure.

- `Assets/Scripts/Game/GameFlow.Ch2.cs` — Chapter 2: seven days (168 h) at the Mars outpost on the landed Kestrel. Actions cost hours, Base Control splits the power between systems, the solar storm tests the shelter, Earth answers through relay passes, and the rescue ship checks in at hour 168.

- `Assets/Scripts/Game/GameFlow.Crew.cs` — Inspect implementation; no summary header.

- `Assets/Scripts/Game/GameFlow.cs` — The director: title, chapter 1 (ship), chapter 2 (Mars), ending. Owns input, rooms and the shared systems (inventory, knowledge cards, hints, language). Chapter logic is in the partial files.

- `Assets/Scripts/Game/GameFlow.Cutscenes.cs` — In-engine pixel-art cutscenes: intro, debris impact, a landing per world, an ending per world. Each is a few short shots on a stage far from the ship; Space, Esc or the Skip button skips.

- `Assets/Scripts/Game/GameFlow.Emergency.cs` — Random daily emergencies in the walkable base (from day 2): a power fault that locks the power room until the breaker is reset, a solar storm alert (reach the storm shelter in time), or an air leak in the hub. ORBI announces each with a red alert. Engineers staying as guests (Rex, Jules) fix faults for free. Also: trading with travelling merchants, and guests who help or cause trouble.

- `Assets/Scripts/Game/GameFlow.Jobs.cs` — Repair jobs in the walkable base (Among Us-style tasks): a broken thing waits at its place, the job panel says what it needs, the player fetches the part from the toolbox in the storage room (Tab shows the bag), carries it there and solves a small hands-on panel. What the repair costs is not shown: it depends on how well and how fast the panel was solved, plus a little chance (wear and tear), and it shows up on the gauges. Guests can be called over to help (they walk to the place); engineers can do a whole job for you.

- `Assets/Scripts/Game/GameFlow.Layout.cs` — Room-by-room layouts for the two top-down maps (see docs/redesign-v2.md, "Layouts"). Ship Kestrel: engine room at the back, a long corridor down the middle with three rooms on each side, the cockpit at the nose. Mars: the landed lifeboat pod, a short tube, then the hub with modules around it. A room is a rectangle of floor; walls are made around every room; doors are floor tiles cut through walls.

- `Assets/Scripts/Game/GameFlow.Lifeboat.cs` — Card-based Chapter 2 prototype: a survival diary in the lifeboat. Pick one pet, then each day read the day's situations and choose, decide who eats and drinks, and read what the night did. All rules live in LifeboatSim; this file only draws its state and passes the clicks on.

- `Assets/Scripts/Game/GameFlow.LifeboatShot.cs` — Verification mode for the survival diary: run the player with "-autoshot -lifeboat". It plays the whole week three times through the real buttons (rescued with three stars, rescued the hard way, stranded), saves the screens to Captures/lifeboat/, logs every check, then quits.

- `Assets/Scripts/Game/GameFlow.Mars.cs` — The Mars surface around the landed Kestrel: MOXIE, solar panels, ice, the Sabatier reactor, the beacon line.

- `Assets/Scripts/Game/GameFlow.Meal.cs` — Evening in the pod, the walkable-base version of the ration screen: everyone sits round the console, the meals stand on a tray, and the player drags a meal onto each person (or right-clicks them for a menu). Status bars and faces show who is hungry; the sim's ration rules are unchanged (one meal = that person's food and water ration). Also the survival guide and visual-novel conversations.

- `Assets/Scripts/Game/GameFlow.MenuPolish.cs` — Inspect implementation; no summary header.

- `Assets/Scripts/Game/GameFlow.Menus.cs` — Main menu over the animated key art, pause menu, settings, how to play, credits, card book.

- `Assets/Scripts/Game/GameFlow.Outside.cs` — The walkable base's day clock (time passes while you walk; dinner from 18:00, the day ends at 20:00), salvage to search outside (ice, scrap, seeds, an old rover), and guest friendship (fed guests grow fond of you and help more; guests left hungry leave and may break something).

- `Assets/Scripts/Game/GameFlow.Pace.cs` — Pace and freedom in the walkable base (round 10, see docs/redesign-v2.md): - time control: P pauses (look around, open panels, plan), F switches 1x/2x, like Frostpunk or RimWorld; - the oxygen maker (MOXIE) runs whenever you walk up to it, once a day, for power; - the project board in the hub: pick up to two base projects a day, each with an open price and a lasting effect, solved hands-on like a repair; - the game saves every night, so a run can be continued from the next morning.

- `Assets/Scripts/Game/GameFlow.PlayShot.cs` — Playtest pass (-autoshot -play): a simple player brain plays the whole game with the day clock and random emergencies switched on, like a person would: intro, morning jobs, scramble, landing, then the week in the base (events, jobs, salvage, emergencies, dinner). It logs timings and stock per day and takes regular screenshots, so a human can review pacing and problems. It does not judge fun; it records what happened.

- `Assets/Scripts/Game/GameFlow.Puzzles.cs` — Hands-on repair panels in the style of Among Us tasks, each teaching one real idea: join wires by colour, unscrew and swap a filter, scrub dust off, turn pipe pieces until water flows, stack water bags round a bunk against radiation, switch to safe mode without turning off life support, tune a radio dish to a satellite, tape air leaks and refill the pressure, or crank a radio by hand. Each reports mistakes and time. A helper standing next to you makes the panel easier. Autoshot and playtest runs solve them by themselves.

- `Assets/Scripts/Game/GameFlow.Radiation.cs` — Radiation dose in game units (not real millisieverts): a little every night (cosmic rays reach the surface because Mars has thin air and no global magnetic field), more for every minute outside, and a lot when a solar storm hits someone who is not in the shelter. Every <see cref="DosePerSick"/> units the cadet loses one health (radiation sickness). The facts shown come from Curiosity's RAD instrument.

- `Assets/Scripts/Game/GameFlow.Scramble.cs` — The 60-second scramble (60 Seconds style): the impact threw supplies all over the ship. Before the landing, the cadet has one minute to grab what fits in the landing lockers. Every supply carries into Chapter 2 as a start bonus or as a flag that unlocks event answers.

- `Assets/Scripts/Game/GameFlow.Ship.cs` — The ship Kestrel: the same five rooms in both chapters. After landing the rooms keep their layout, gain damage, and the cargo airlock opens onto the world the player chose. Layout rule: every prop sits between x = -3.3 and +3.3 so nothing overlaps a door (doors at +-4.1).

- `Assets/Scripts/Game/GameFlow.ShipTop.cs` — Chapter 1 in the top-down view: the ship Kestrel. A morning of short quests that teach walking, carrying and using things (and where everything is), a rest on the sofa, the debris impact, then 60 real seconds to carry supplies to the lifeboat. The lifeboat lands on Mars and the walkable base week begins.

- `Assets/Scripts/Game/GameFlow.Story.cs` — Opening: Earth in 2177, then a press conference at mission control where the cadet is chosen and walks up beside Commander Arun, the jump to Mars, and the two of them in the Kestrel cockpit.

- `Assets/Scripts/Game/GameFlow.StoryShot.cs` — Inspect implementation; no summary header.

- `Assets/Scripts/Game/GameFlow.Talk.cs` — Conversation banks. Guests in the base: a greeting that depends on friendship, stories about themselves, a Mars fact from their own job, and replies for asking help or asking them to leave. Visitors at the peephole: extra answers after the clue questions (the monsters answer a little wrong). Lines are (English, Thai). Characters may be redrawn later; the lines describe personality, not looks.

- `Assets/Scripts/Game/GameFlow.UXShot.cs` — Inspect implementation; no summary header.

- `Assets/Scripts/Game/GameFlow.Visual.cs` — Visual layer of the walkable base: resource gauges with sticker icons (top left), the cadet portrait (top right), choice cards that lift on hover and show only up/down hints instead of numbers, and the full-screen first-person peephole where visitors are inspected (infrared, breath, door microphone, talk). Rules stay in LifeboatSim; this file only draws its state.

- `Assets/Scripts/Game/GameFlow.Walkthrough.cs` — Inspect implementation; no summary header.

- `Assets/Scripts/Game/GameUI.cs` — All screen UI, built in code with UI Toolkit. Text uses the Advanced text generator (HarfBuzz shaping) so Thai vowels and tone marks sit in the right place.

- `Assets/Scripts/Game/PixelCanvas.cs` — A tiny pixel canvas, y goes up (row 0 is the bottom), turned into point-filtered sprites.

- `Assets/Scripts/Game/Settings.cs` — Player settings, saved in PlayerPrefs. Values are 0..1 except textScale and textSpeed.

- `Assets/Scripts/Game/Sfx.cs` — Synthesized sound: soft sine/triangle tones with gentle envelopes (no harsh square waves), plus looping danger sounds and a quiet ship ambience. Volumes follow Settings.

- `Assets/Scripts/Game/WalkthroughAudioCapture.cs` — QA-only capture of the actual listener mix; never synthesizes or replaces audio.

- `Assets/Scripts/Game/World.cs` — Something the player can click. The astronaut walks to standX, then the flow handles id.

## 12. NASA/แหล่งข้อมูล/ความรู้ที่แสดงในเกม (ข้อความ source ณ วันส่งต่อ)

- `sources.title`: SCIENCE & DATA SOURCES / วิทยาศาสตร์และแหล่งข้อมูล

- `sources.observed`: OBSERVED BY NASA (measured) / NASA วัดได้จริง

- `sources.observed.1`: Mars surface radiation about 0.64 mSv per day: Curiosity rover's RAD instrument (Hassler et al., Science, 2014). The game rounds it to 0.7 per day. / รังสีบนพื้นดาวอังคารประมาณ 0.64 mSv ต่อวัน: เครื่องมือ RAD บนยาน Curiosity (Hassler และคณะ, Science, 2014) เกมปัดเป็น 0.7 ต่อวัน

- `sources.observed.2`: September 2017: a solar storm was detected at Mars by Curiosity RAD and the MAVEN orbiter. It inspired the day-4 storm. / กันยายน 2017: Curiosity RAD และยาน MAVEN ตรวจพบพายุสุริยะที่ดาวอังคาร เป็นแรงบันดาลใจของพายุวันที่ 4

- `sources.observed.3`: MOXIE made oxygen from Mars air on the Perseverance rover, 2021–2023. / MOXIE ผลิตออกซิเจนจากอากาศดาวอังคารบนยาน Perseverance ปี 2021–2023

- `sources.observed.4`: A Mars day (sol) lasts 24 h 39 min 35 s. / หนึ่งวันบนดาวอังคาร (sol) ยาว 24 ชม. 39 นาที 35 วินาที

- `sources.principle`: NASA SCIENCE PRINCIPLES / หลักวิทยาศาสตร์ของ NASA

- `sources.principle.1`: Space weather alerts like the storm warning come from NASA's DONKI space weather database (ccmc.gsfc.nasa.gov/tools/DONKI). / คำเตือนพายุแบบในเกมมาจากฐานข้อมูลสภาพอวกาศ DONKI ของ NASA (ccmc.gsfc.nasa.gov/tools/DONKI)

- `sources.principle.2`: Life support (ECLSS) removes CO₂, recycles water and makes oxygen (nasa.gov, ECLSS). / ระบบยังชีพ (ECLSS) กำจัด CO₂ รีไซเคิลน้ำ และผลิตออกซิเจน (nasa.gov, ECLSS)

- `sources.principle.3`: Plants in space need light, water and nutrients (nasa.gov, growing plants in space). / พืชในอวกาศต้องการแสง น้ำ และสารอาหาร (nasa.gov, การปลูกพืชในอวกาศ)

- `sources.principle.4`: Water and dense material like regolith block radiation; shelters cut the dose. / น้ำและวัสดุหนาแน่นอย่างดินรีโกลิทกันรังสีได้ ห้องหลบภัยช่วยลดรังสี

- `sources.principle.5`: Radio between Earth and Mars takes about 4–24 minutes each way, depending on where the planets are. Relay orbiters carry rover messages. / คลื่นวิทยุระหว่างโลกกับดาวอังคารใช้เวลาราว 4–24 นาทีต่อเที่ยว ขึ้นกับตำแหน่งดาว ยานโคจรถ่ายทอดส่งข้อความของยานสำรวจ

- `sources.simulated`: GAME SIMULATION (made up for play) / การจำลองในเกม (ตัวเลขสมมติ)

- `sources.simulated.1`: The 7-day rescue, the storm's size and timing, and every penalty are invented for the game. / การกู้ภัย 7 วัน ขนาดและเวลาของพายุ และค่าปรับทุกอย่างเป็นเรื่องที่แต่งขึ้นสำหรับเกม

- `sources.simulated.2`: Air, power, shelter, supplies and energy are 0–100 game bars, not real units. The 14-minute radio delay is one value picked from the real range. / อากาศ ไฟฟ้า ห้องหลบภัย เสบียง และพลังกาย เป็นแถบ 0–100 ในเกม ไม่ใช่หน่วยจริง ความล่าช้าของวิทยุ 14 นาทีเป็นค่าหนึ่งที่เลือกจากช่วงจริง

- `sources.simulated.3`: The game does not read live data: no number here is presented as a live NASA reading. / เกมไม่ได้ดึงข้อมูลสด ไม่มีตัวเลขไหนที่อ้างว่าเป็นค่าสดจาก NASA

- `ship.fact.food`: Fed! Space food comes in sealed pouches, so crumbs can't float into the machines. / ให้อาหารแล้ว! อาหารอวกาศบรรจุในถุงปิดสนิท เศษอาหารจะได้ไม่ลอยไปติดเครื่องจักร

- `ship.fact.water`: Filter fitted! The ISS water recycler turns about 98% of the crew's water, even sweat, back into drinking water. / ใส่ไส้กรองแล้ว! เครื่องรีไซเคิลน้ำบนสถานีอวกาศนานาชาติเปลี่ยนน้ำของลูกเรือราว 98% แม้แต่เหงื่อ กลับมาเป็นน้ำดื่มได้

- `ship.fact.plants`: Watered! On the ISS, NASA's Veggie garden grows lettuce under red and blue LED lamps. / รดน้ำแล้ว! บนสถานีอวกาศนานาชาติ สวน Veggie ของ NASA ปลูกผักกาดใต้ไฟ LED สีแดงกับน้ำเงิน

- `ship.fact.radio`: Report sent! Radio travels at the speed of light, but from Mars a message takes about 4 to 24 minutes to reach Earth. / ส่งรายงานแล้ว! คลื่นวิทยุเดินทางเร็วเท่าแสง แต่จากดาวอังคารข้อความต้องใช้เวลาราว 4 ถึง 24 นาทีกว่าจะถึงโลก

- `sources.observed.5`: Solar flares (NASA DONKI space weather database, archived values, not live): the base shows one real X-class flare each run, e.g. X9.0 on 2024-10-03 and X8.7 on 2024-05-14. / เปลวสุริยะ (ฐานข้อมูลสภาพอากาศอวกาศ DONKI ของ NASA, ข้อมูลย้อนหลัง ไม่ใช่ข้อมูลสด): ฐานแสดงเปลวสุริยะระดับ X ที่เกิดขึ้นจริง 1 ครั้งต่อรอบ เช่น X9.0 วันที่ 2024-10-03 และ X8.7 วันที่ 2024-05-14

- `sources.principle.6`: Base jobs: dust on solar panels (NASA InSight, mission ended 2022), water recycling on the ISS (about 98% recovered), lettuce under red and blue LEDs (NASA Veggie), radio delay Mars to Earth (about 4 to 24 minutes). / งานในฐาน: ฝุ่นบนแผงโซลาร์ (ยาน InSight ของ NASA จบภารกิจปี 2022), การรีไซเคิลน้ำบน ISS (นำกลับมาใช้ได้ราว 98%), ปลูกผักกาดใต้ไฟ LED แดงกับน้ำเงิน (NASA Veggie), เวลาหน่วงของวิทยุจากดาวอังคารถึงโลก (ราว 4 ถึง 24 นาที)

## 13. เอกสารประกอบที่ควรอ่าน
- [educator-guide.md](educator-guide.md)
- [educator-guide.th.md](educator-guide.th.md)
- [game-brief.md](game-brief.md)
- [IMPLEMENTATION_2026-11-13.md](IMPLEMENTATION_2026-11-13.md)
- [PRESENTATION_POLISH_V6_2026-11-12.md](PRESENTATION_POLISH_V6_2026-11-12.md)
- [redesign-v2.md](redesign-v2.md)
- [roadmap.md](roadmap.md)
- [STORY_IMPLEMENTED_2026-11-13.md](STORY_IMPLEMENTED_2026-11-13.md)
- [STORY_REVIEW_2026-11-13.md](STORY_REVIEW_2026-11-13.md)
- [UX_POLISH_2026-11-12.md](UX_POLISH_2026-11-12.md)

## 14. ข้อเท็จจริงเพิ่มเติมจาก source ล่าสุดที่สำคัญ
- GameFlow.Outside.cs: SecondsPerHour = 26 วินาที, DinnerHour = 18:00, LateHour = 20:00. มี real-time clock ขณะเดินเล่นตามเงื่อนไขใน OutsideUpdate; ต่างจากรุ่นเก่าที่เวลาเดินไม่ลดเลย. P/F และหน้าต่างมีผลต่อ clock ให้ตรวจ implementation ก่อน tuning
- GameFlow.Radiation.cs: dose เป็น **game units ไม่ใช่ mSv**; นอกฐานทุก 15 วินาทีเพิ่ม 1 หน่วย, ทุก 12 หน่วยเสีย 1 health; คืนปกติ +0.6, คืนวัน4ไม่มี shelter +4. ค่านี้ห้ามอ้างว่าเป็น NASA radiation equation
- Startup flags กับ default fields ใน C# อาจมีค่าที่ไม่ serialize ลง config.json. Snapshot config อย่างเดียวไม่ใช่ภาพรวมพฤติกรรมทั้งหมด
- Source revision ณ ตรวจ: 79f76b8 feat(base): free task order, pace controls, MOXIE, project board. README pointer ถูกเพิ่มโดยงานเอกสารรอบนี้เท่านั้น

## 15. NASA ที่ยืนยันกับหน้า NASA เพิ่มเติม
- MOXIE: https://www.nasa.gov/solar-system/nasas-oxygen-generating-experiment-moxie-completes-mars-mission/ — ยืนยันการผลิตออกซิเจนบน Mars; ไม่รับรองผลผลิตหรือราคาไฟในเกม
- X9.0 วันที่ 3 ต.ค. 2024: https://science.nasa.gov/blogs/solar-cycle-25/2024/10/03/sun-releases-strong-solar-flare-6/ — ยืนยันวันและระดับตรงกับ record ตัวอย่างใน RealFlares
- X8.7 วันที่ 14 พ.ค. 2024: https://svs.gsfc.nasa.gov/14593 — ยืนยันวันและระดับอีก record ตัวอย่าง. ยังไม่ได้ตรวจ active-region และ event ID ของทั้งหกแถวครบจาก DONKI API

## 16. งานถัดไปที่แนะนำ (ยังไม่ถือว่าพัฒนาแล้ว)
1. อ่าน PLAYTEST_2026-11-14.md และแก้ regression ที่พบก่อนเพิ่มระบบใหม่
2. ขอ URL/ชื่อ NASA challenge จริงเพื่อ map requirement กับ evidence ไม่สรุป compliance จากชื่อ NASA ในเมนู
3. เก็บ archived NASA records เป็นไฟล์มี provenance และ schema; ใช้ records ขับ event ขณะคงเกม offline/reproducible
4. ทำ human playtest เรื่องจังหวะวัน ความเข้าใจค่าใช้จ่าย การสแกน และความคุ้มค่ารับแขก. automated pass ไม่วัดความสนุก
5. ตรวจความครบของ save restoration กับงาน/สุ่มแขก/โครงการ/รังสี และตรวจ loop อาหารเย็น/เปิดแผง/พักเกม
6. รักษาภาษาไทย/อังกฤษ เส้นทาง skip cinematic กล้อง และ input lock เมื่อเพิ่ม content

## 17. ดัชนี content จากข้อมูลปัจจุบัน

### pets
- `comet`
- `pixel`
- `luna`

### visitors
- `tik`
- `mu` [one night]
- `zee`
- `nok`
- `shade`
- `ree`
- `mimic`
- `aya`
- `jules`
- `pip`
- `kira`
- `rex`
- `vela`
- `bizz`

### cards
- `filter` day 1 choices=swap, wash, ignore
- `visitor_tik` day 1 visitor=tik choices=admit, trade, refuse
- `crate` day 2 choices=walk, send_pet, leave
- `visitor_mu` day 2 visitor=mu choices=admit, gift, refuse
- `locker` day 2 choices=lock, open
- `recycler` day 3 choices=repair, tik_repair, skip
- `storm_warning` day 3 choices=water_wall, shield_kit, later
- `visitor_zee` day 3 visitor=zee choices=buy_o2, buy_food, refuse
- `storm_last` day 4 choices=sandbags, send_pet, hide
- `storm_power` day 4 choices=safe_mode, leave_on
- `visitor_shade` day 4 visitor=shade choices=admit, refuse
- `visitor_nok` day 5 visitor=nok choices=admit, treat, water, refuse
- `mu_returns` day 5 visitor=mu choices=accept, decline
- `far_crate` day 6 choices=walk, send_pet, leave
- `beacon` day 6 choices=nok_part, tik_fix, chip, climb, wait
- `coords` day 6 choices=send, crank, wait
- `visitor_ree` day 6 visitor=ree choices=admit, trade, refuse
- `beacon_last` day 7 choices=nok_part, tik_fix, chip, climb, give_up
- `coords_last` day 7 choices=send, crank, skip
- `visitor_mimic` day 7 visitor=mimic choices=admit, refuse
- `log` day 7 choices=air, water, friends
- `visitor_aya` day 2 visitor=aya choices=admit, refuse
- `visitor_jules` day 3 visitor=jules choices=admit, refuse
- `visitor_pip` day 1 visitor=pip choices=wave, trade, refuse
- `pip_return` day 4 visitor=pip choices=accept, refuse
- `visitor_kira` day 3 visitor=kira choices=admit, snack, refuse
- `visitor_rex` day 5 visitor=rex choices=admit, swap, refuse
- `visitor_vela` day 2 visitor=vela choices=thanks, gift, refuse
- `visitor_bizz` day 4 visitor=bizz choices=admit, honey, refuse

### nights
- `storm_crew` day 4
- `storm_power` day 4

### supplies
- `sup_o2`
- `sup_water`
- `sup_food`
- `sup_battery`

### ภาพรวมไฟล์ข้อความ
2082 localized entries ณ snapshot นี้. รายการความรู้และบทพูดจริงอยู่ใน generated strings.json และ source generator อย่าแปลใหม่โดยทิ้ง key เดิม

## 18. ไฟล์สรุปหลักฐานประกอบ
- [NASA audit](NASA_DATA_AUDIT_2026-11-14.md)
- [Fresh playtest](PLAYTEST_2026-11-14.md)
- [Source/build snapshot and hashes](handoff_snapshot_2026-11-14.json)

ข้อสังเกตจาก static review: dispatch ใน GameFlow.AutoShot.cs มี if(play) แยกจาก chain ของ story/ux/ship และ fallback ForceOutpost; ควรตรวจการ dispatch ของ -ship ก่อนใช้ผลอัตโนมัติเป็นหลักฐานของ route นั้น งานรอบนี้เล่นฉบับล่าสุดด้วย -play ซึ่งไม่ได้เข้าทาง fallback นี้ ข้อนี้ยังไม่ใช่บั๊กที่ reproduce ในเกมปกติ
