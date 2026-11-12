# TITAN GATE 2177 — Junior Astronaut Mission Trainer

2.5D pixel-art point-and-click mission trainer (TH/EN) for NASA Space Apps 2026. Unity **6000.3.25f1**,
built-in renderer, UI Toolkit.

ปี 2177 นักบินฝึกหัดกับ AI ชื่อ ORBI — ยาน Kestrel ชนขยะอวกาศ มีเวลา 60 วินาทีเก็บของก่อนลงจอดฉุกเฉินบนดาวอังคาร
แล้วต้องรักษายานที่ลงจอดแล้วให้เป็นฐานที่อยู่รอดได้ 7 วัน (168 ชั่วโมง) จนยานกู้ภัยมาถึง

## Play

- Editor: open `Assets/Scenes/Main.unity` → Play.
- Windows build: menu **Titan Gate → Build Windows** → `Builds/Win/TitanGate2177.exe`.
- Browser build: needs Unity's **Web Build Support** module (not installed yet on the build PC).

**Controls:** click to walk / use · hold **Space** = show everything usable · **B** = Base Control (power) ·
**H** = ORBI hint · **C** = knowledge cards · click an item then another item = combine · right click / **Esc** = drop / close / pause.

## How the outpost works (Chapter 2)

- One clock: **hours since landing (0–168)**. Landing is Day 1 06:00; days change at 06:00; rescue is checked at hour 168.
- Every action costs hours. HUD: **Air · Power · Shelter** (big), **Supplies · Energy** (small), radiation dose, solar output.
- **Base Control** switches four powered systems (life support, greenhouse lamps, beacon, shelter support). Solar
  charges 07:00–19:00, so power for the night has to be saved.
- **Solar storm** at hour 84 (Day 4, 18:00). Earth's warning arrives at hour 28 on a relay pass. Shelter readiness ×
  power decides how much hits.
- **Earth link:** messages ride relay passes at 10:00 and 22:00 (reply after two passes) plus a 14-minute light delay.
- **Rescue:** repair the beacon, switch it on, send coordinates; the rescue ship acknowledges on a 6-hourly pass.
  Hour 168: coordinates acknowledged → Rescued; alive without → Stranded; air or supplies gone earlier → Failed.
- The opening **60-second scramble** (5 of 10 supplies) carries into the outpost; no supply is required to win.

## Where things are

| Path | What |
|---|---|
| `Assets/Resources/Data/config.json` | **Every tuning number** (`outpost` section: systems, actions, events, storm, dilemmas) |
| `Assets/Resources/Data/strings.json` | All text, EN + TH — generated from `tools/strings_src.py` |
| `Assets/Scripts/Core/` | Pure C# rules: `OutpostSim` (hour clock, power, storm, messages, rescue), `Puzzles`, `Localization` |
| `Assets/Scripts/Game/` | Unity layer: pixel art in code, world, UI, Chapter 1 (`Ch1`, `Scramble`), Chapter 2 (`Ch2`, `Mars`) |
| `Assets/Tests/EditMode/` | 29 EditMode tests (clock, power, storm, relay messages, rescue, encounters, a sensible-player run over 40 seeds with minimal and strong loot, string coverage) |
| `docs/` | Brief, roadmap, educator guides (EN/TH) |
| `production/qa/evidence/` | Screenshots from the automated pass |

Edit text: change `tools/strings_src.py`, then `python tools/strings_src.py Assets/Resources/Data/strings.json`.

## Verify

- Tests (headless): `Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml`
- Screenshot pass: `Builds/Win/TitanGate2177.exe -autoshot` → plays Chapter 1 and the 7-day outpost through the real
  click handlers, checks every step (PASS/FAIL lines), saves every screen and `autoshot-log.txt` into
  `Builds/Win/Captures/mars/`, then quits.
- Balance: `python tools/balance_sim.py` mirrors the outpost rules for quick tuning (C# tests are the authority).

## Science and data

In-game **Science & Data Sources** separates three kinds of content: values **observed by NASA** (e.g. Curiosity RAD
surface dose), **NASA science principles** (DONKI space weather, ECLSS, plant growth, radio delay), and **game
simulation** (the 7-day rescue, storm size, penalties, 0–100 bars). No number is presented as live data.

## Thai text

UI Toolkit's **Advanced** text generator with **Chakra Petch** (SIL OFL, `docs/ChakraPetch-OFL.txt`). Unity does not
stack a tone mark above an upper vowel, so `GameUI.Thai()` raises those marks with a rich-text offset for every string.

All art is drawn procedurally in code; no third-party game art is used.
