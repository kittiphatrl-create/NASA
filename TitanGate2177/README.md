# TITAN GATE 2177 — Junior Astronaut Mission Trainer

2.5D pixel-art point-and-click mission trainer (TH/EN). Unity **6000.3.25f1**, built-in renderer, UI Toolkit.

ปี 2177 นักบินฝึกหัด 1 คนกับ AI ชื่อ ORBI — ยานชนขยะอวกาศ ต้องลงจอดดาวอังคาร แล้วเอาชีวิตรอดจนปล่อยยานกลับได้
ความรู้อวกาศของจริงจาก NASA คือเบาะแสของทุกปริศนา

## Play

- Editor: open `Assets/Scenes/Main.unity` → Play.
- Windows build: menu **Titan Gate → Build Windows** → `Builds/Win/TitanGate2177.exe`.

**Controls:** click to walk / use · hold **Space** = show everything usable · **H** = ORBI hint ·
**C** = knowledge cards · click an item then another item = combine · right click / **Esc** = drop item / close.

## Where things are

| Path | What |
|---|---|
| `Assets/Resources/Data/config.json` | **Every tuning number** (resources, actions, events, puzzle answers) |
| `Assets/Resources/Data/strings.json` | All text, EN + TH — generated from `tools/strings_src.py` |
| `Assets/Scripts/Core/` | Pure C# rules: `SurvivalSim`, `Puzzles`, `Localization` (unit-tested) |
| `Assets/Scripts/Game/` | Unity layer: pixel art drawn in code (`Art.cs`), world, UI, chapter flow |
| `Assets/Tests/EditMode/` | 44 EditMode tests (rules, events, incidents, carry-over, per-route winnability over 40 seeds, string coverage) |
| `docs/game-brief.md` · `docs/roadmap.md` | Brief and development plan |
| `production/qa/evidence/` | Screenshots of every screen from the automated pass |

Edit text: change `tools/strings_src.py`, then `python tools/strings_src.py Assets/Resources/Data/strings.json`.

## Verify

- Tests (headless): `Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml`
- Screenshot pass: `Builds/Win/TitanGate2177.exe -autoshot -route mars|moon|titan` → plays the route through the
  real click handlers and modal buttons, checks every puzzle (PASS/FAIL lines), saves every screen and
  `autoshot-log.txt` into `Builds/Win/Captures/<route>/`, then quits.
- Balance: `python tools/balance_sim.py` mirrors the survival rules for quick tuning (C# tests are the authority).

## Thai text

Thai uses UI Toolkit's **Advanced** text generator (HarfBuzz shaping, `style.unityTextGenerator = Advanced`) with
**Chakra Petch** (SIL OFL, `docs/ChakraPetch-OFL.txt`), so vowels and tone marks are positioned correctly.

All art is drawn procedurally in code; no third-party game art is used.
