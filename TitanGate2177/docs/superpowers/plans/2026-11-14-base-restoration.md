# Base Restoration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the post-crash Mars base into an abandoned base the player restores room by room (action-timed days, personal needs, ORBI chip with chat warnings, weather, solar-storm blackouts, a pretty night) until a distress signal is sent during a relay pass.

**Architecture:** A new pure C# rules class `RestoreSim` (Core, EditMode-tested) owns rooms, tasks, action time, personal needs, weather, blackout, relay window and the win. The existing `LifeboatSim` keeps base stock, guests/visitors, meals and nights, but in restore mode it only gets visitor cards and no day-7 win. New `GameFlow.Restore*.cs` partials wire it into the walkable base and reuse jobs, puzzles, peephole, meal, ORBI and pace code.

**Tech Stack:** Unity 6000.3.25f1 (built-in renderer), C#, UI Toolkit, procedural pixel art (`PixelCanvas`), NUnit EditMode tests, JsonUtility config.

**Spec:** `docs/superpowers/specs/2026-11-14-base-restoration-design.md`

## Global Constraints

- Whole run ~20 min, never more than 30 min (spec §2, §6.1).
- Clock moves only by actions; walking, talking, ORBI, bag, peephole are free; every action shows its hours before it is done; day budget 06:00–20:00 = 14 h; ~10–14 actions a day; restoring everything needs ~3–4 days (spec §6.1b).
- Power room first (forced), then any order among greenhouse / storage+water / air (MOXIE) / quarters; comms room opens after 3 of those 4; comms is always last (spec §5.2, §6.2).
- Win = signal sent during a relay pass → one more short morning → goodbye → pickup → end screen. Lose = base O₂ 0 or health 0 → lose screen with reason + "retry this morning" + "new game" (spec §5.3, §5.6, §6.8).
- HUD shows only the player's hunger, thirst, fatigue; base numbers only via ORBI; ORBI danger warnings as a left-side Messenger-style bubble with ORBI's picture (spec §5.11, §5.12, §6.5).
- NASA accuracy: Mars is never hot outside (Gale ~0 °C day, ~−70 °C night); "hot day" = base cooling loop failure; solar storms cut power and add radiation, never fire; relay pass 1–2 per sol ~8 min real (shortened in game); blue Mars sunset; visible aurora seen by Perseverance 2024 (spec §6.6–6.9b).
- Borrow mechanics only; no art, names or content from other games.
- TH/EN text via `TT(en, th)` or `tools/jobs_strings.py` → `python tools/strings_src.py Assets/Resources/Data/strings.json`.
- No `git commit`/push/merge without the user's OK: "checkpoint" steps below = compile + tests green, then stop for a commit only if the user allowed it.

## Review Focus

- Player starts a timed action with too little time left in the day → action refused with "not enough time today", never half-applied. (Task 1 test `Do_RefusesWhenDayTooShort`)
- Solar storm blackout while a puzzle panel is open → panel closes cleanly, room shows unpowered, reset task appears. (Task 7 test `Blackout_UnpowersRestoredRoomsAndAddsReset`)
- Relay window missed → next window later, signal not sent, game continues. (Task 2 test `Send_OutsideWindow_Fails_NextWindowLater`)
- Continue from a nightly save mid-restoration → rooms/tasks/needs restored. (Task 4 test `Snapshot_RoundTrips`)
- Player never asks ORBI → still warned before losing. (Task 6 test `Warnings_ListDangerBeforeLoss`)

---

### Task 1: RestoreSim core — rooms, tasks, action time, needs

**Files:**
- Create: `Assets/Scripts/Core/RestoreSim.cs` (+ `.meta`)
- Test: `Assets/Tests/EditMode/RestoreSimTests.cs` (+ `.meta`)

**Interfaces:**
- Produces:
  - `[Serializable] class RestoreTaskDef { string id, room, needItem, puzzle, kind; float hours; }` (`kind`: `"sweep"|"repair"|"wire"|"chip"|"send"`)
  - `[Serializable] class RestoreRoomDef { string id; bool first; bool last; }`
  - `[Serializable] class RestoreConfig { List<RestoreRoomDef> rooms; List<RestoreTaskDef> tasks; int roomsForComms = 3; float startHour = 6, endHour = 20; float hungerPerHour, thirstPerHour, fatiguePerWorkHour, hotThirstMult; float needDamageAt = 0; List<string> weatherPool; float relayHour1, relayHour2, relayLengthHours; }`
  - `enum RoomState { Locked, Dark, Restored, Unpowered }`
  - `class RestoreSim(RestoreConfig c, int seed)` with: `int Day; float Hour; float HoursLeft; float Food, Water, Rest` (personal meters 0–100, 100 = fine); `RoomState StateOf(string room)`; `bool IsDone(string task)`; `float Percent` (done non-chip tasks / all); `bool CanDo(string task, IReadOnlyCollection<string> bag, out string reasonKey)`; `float HoursFor(string task, int mistakes = 0)`; `bool Do(string task, int mistakes = 0)`; `bool Spend(float hours, float work = 0)`; `void Eat()`, `void Drink()`, `void Nap(float hours)`, `void Sleep()` (Day++, Hour = startHour, Rest = 100); `IEnumerable<string> OpenTasks()`.
  - reason keys: `"restore.need.time"`, `"restore.need.item"`, `"restore.need.room"` (room still locked), `"restore.need.power"` (power room not restored), `"restore.need.rooms"` (comms before 3 rooms).

- [ ] **Step 1: Write failing tests** in `RestoreSimTests` with a local `Cfg()` (power: `chip`, `power_wire`; greenhouse: `gh_sweep`, `gh_lamps`; storage: `st_pry`, `st_pipes`; air: `air_filter`; quarters: `q_heater`; comms: `c_dish`, `c_send`):
  - `Start_OnlyPowerRoomOpen`: `StateOf("greenhouse") == Locked`, `StateOf("power") == Dark`.
  - `PowerRestored_OpensOtherRoomsNotComms`: do `chip`, `power_wire` → greenhouse `Dark`, comms `Locked`.
  - `CommsOpensAfterThreeRooms`.
  - `Do_SpendsShownHours`: `HoursFor("gh_sweep") == 0.5f`; after `Do`, `Hour == 6.5f`.
  - `Do_RefusesWhenDayTooShort`: spend to 19.8, `CanDo("power_wire")` false with `"restore.need.time"`, state unchanged.
  - `Mistakes_AddHalfHourEach_CappedAtOneHour`: `HoursFor(t, 3) == base + 1f`.
  - `Needs_DropWithTime_FasterWhenHot`; `Eat/Drink` reset to 100; `Sleep` → `Day == 2`, `Hour == 6`.
  - `Percent_CountsDoneTasks`.
- [ ] **Step 2: Run** `Unity -batchmode -runTests -testPlatform EditMode -testFilter RestoreSimTests` (or MCP `run_tests`). Expected: FAIL (type missing).
- [ ] **Step 3: Implement `RestoreSim`** to the interface. A room is `Restored` when all its tasks are done; `Locked` until its gate (power restored; comms: `roomsForComms` of the four free rooms restored).
- [ ] **Step 4: Run tests** → PASS (all 67 old tests still pass).
- [ ] **Step 5: Checkpoint.**

### Task 2: RestoreSim weather, relay window, win/lose

**Files:** Modify `Assets/Scripts/Core/RestoreSim.cs`; Test `RestoreSimTests.cs`

**Interfaces:**
- Produces: `string Weather` (`"normal"|"cold"|"dust"|"hot_inside"|"flare"`), picked per day from `weatherPool` by seed, never `"flare"` on day 1; `bool RelayOpen` (Hour in `[relayHourN, relayHourN + relayLengthHours)`); `float NextRelayHour`; `bool SendSignal()` (needs comms restored except `c_send`, and `RelayOpen`); `bool Signalled`; `bool CoolingBroken` (true on `hot_inside` until task `fix_cooling` done); `float ThirstMult`.
- [ ] **Step 1: Failing tests:** `Weather_DeterministicPerSeed`, `Weather_NoFlareOnDayOne`, `Send_InsideWindow_Succeeds`, `Send_OutsideWindow_Fails_NextWindowLater`, `HotInside_ThirstFaster_UntilCoolingFixed`.
- [ ] **Step 2: Run → FAIL.** **Step 3: Implement.** **Step 4: Run → PASS.** **Step 5: Checkpoint.**

### Task 3: Blackout and snapshot

**Files:** Modify `RestoreSim.cs`; Test `RestoreSimTests.cs`

**Interfaces:**
- Produces: `void Blackout()` → every `Restored` room except `power` becomes `Unpowered` and gets an open task `reset_<room>` (kind `wire`, 0.5 h); `Do("reset_<room>")` → `Restored`; `RestoreSnapshot Snapshot()` / `void Load(RestoreSnapshot s)` (`[Serializable]`: day, hour, meters, done tasks, unpowered rooms, signalled).
- [ ] **Step 1: Failing tests:** `Blackout_UnpowersRestoredRoomsAndAddsReset`, `ResetTask_RepowersRoom`, `Snapshot_RoundTrips`. **Steps 2–5** as above.

### Task 4: Config data and restore-mode switch

**Files:**
- Modify: `Assets/Scripts/Core/GameConfig.cs:258` (add `public RestoreConfig restore = new RestoreConfig();`)
- Modify: `Assets/Resources/Data/config.json` (new `"restore"` section)
- Test: `RestoreSimTests.ShippedConfig_IsPlayable` (reads `Assets/Resources/Data/config.json` like `LifeboatSimTests` does)

**Data (my numbers, tune in Task 12):** rooms `power`(first) / `greenhouse` / `storage` / `air` / `quarters` / `comms`(last); tasks per room 2–3 using puzzles `wires|scrub|pipes|screws|switches|tune|leak|crank|shield`; hours 0.5–2; items `broom` (lounge), `crowbar` (hub debris), `fuse`, `cable`, `tape`, `spare_filter`; `hungerPerHour 4`, `thirstPerHour 5`, `fatiguePerWorkHour 6`, `hotThirstMult 1.8`; `weatherPool ["normal","normal","cold","dust","hot_inside","flare"]`; relay `9.5`, `16.5`, length `0.75`; `lifeboat.daysTotal` unchanged for diary mode, restore mode forces no day-7 win (Task 5).
- [ ] **Step 1: Failing test** `ShippedConfig_IsPlayable`: every task's room exists; every needItem is findable (a `find_*` task or toolbox); sum of all task hours ≥ 30 and ≤ 56 (3–4 days of 14 h).
- [ ] **Steps 2–5.**

### Task 5: Restore mode in the walkable base (clock, tasks, routing)

**Files:**
- Create: `Assets/Scripts/Game/GameFlow.Restore.cs`
- Modify: `GameFlow.Lifeboat.cs` (`LbPickPet`: create `_restore = new RestoreSim(_cfg.restore, _lbSeed)` when `RestoreMode`; give `LifeboatSim` a config copy whose `cards` are visitor cards only and `daysTotal = 99`)
- Modify: `GameFlow.Outside.cs` (`BClock`: no real-time ticking when `RestoreMode`; 20:00 reached only by actions or "rest" choice)
- Modify: `GameFlow.Base.cs` (`BUseResolved`: route spots that own restore tasks to `RestoreTaskShow`; hover label shows `· X h`)
- Modify: `GameFlow.Jobs.cs` (`JobHiddenCost` → in restore mode spend `RestoreSim.HoursFor(task, mistakes)` instead of secs-based hours)

**Interfaces:**
- Consumes: Task 1–3 `RestoreSim`.
- Produces: `bool RestoreMode => _cfg.restore.rooms.Count > 0 && WalkBase`; `RestoreSim _restore`; `void RestoreTaskShow(string taskId)` (job-style panel: what, needs item, **hours shown**, start → puzzle → `_restore.Do`); `string TaskAtSpot(string spotId)`; `void RestoreSync()` (LifeboatSim schedule follows `_restore.Hour`).
- Gate existing spots by room state: plots need `greenhouse` Restored, MOXIE (`air`) needs `air`, toolbox needs `storage`, bed sleep needs `quarters`; water shelf in the lounge works from the start (drink = 0.25 base water). Ungated spots in a Dark room show "too dark, restore power first".
- [ ] **Step 1:** Extend `GameFlow.BaseShot.cs` with a restore route check list: chip → power → greenhouse sweep; assert `_restore.StateOf("greenhouse") == Dark` after power; hour advanced exactly by shown hours.
- [ ] **Step 2:** Run base autoshot (`-autoshot -base`) → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Run → checks pass, `done errors=0`.
- [ ] **Step 5: Checkpoint.**

### Task 6: ORBI chip, terminal, chat warnings, needs HUD, symptoms

**Files:**
- Create: `Assets/Scripts/Game/GameFlow.Orbi.cs`
- Modify: `GameFlow.Base.cs` (`BBuildHud`/HUD refresh: hide base gauges in restore mode, show `NeedsBar()`), `GameFlow.ShipTop.cs` (scramble end: ORBI "moves into the chip" line + chip in hands), `GameFlow.Layout.cs` (ORBI terminal prop `orbi` in the lounge console)
- Modify: `RestoreSim.cs` + test: `List<string> Warnings(Stock base, float o2Tonight, float powerTonight)` keys `restore.warn.o2|power|water|food|food_me|water_me|rest_me|flare`

**Interfaces:** Produces `void OrbiChat(string text, Color tone)` (stack of up to 3 left bubbles with ORBI face, auto-fade 6 s), `void OrbiTerminalShow()` (base O₂/water/food/power, temps, radiation, `Percent`, next relay), `VisualElement NeedsBar()`.
- [ ] **Step 1: Failing test** `Warnings_ListDangerBeforeLoss` (O₂ < tonight → `restore.warn.o2`).
- [ ] **Steps 2–4**; base autoshot shot `r06_orbi_chat` + `r06_terminal` viewed.
- [ ] Symptoms in `ActionPresentation`: pant (O₂ warning) / sweat (`CoolingBroken`) / shiver (`cold`) / slow walk (`Rest < 25`).
- [ ] **Step 5: Checkpoint.**

### Task 7: Abandoned look, restored look, blackout

**Files:**
- Create: `Assets/Scripts/Game/ArtRestore.cs` (debris pile, cut wire with spark frames, dusty overlay, emergency red lamp, chip, broom, crowbar icons)
- Modify: `GameFlow.Restore.cs` (per-room dark overlay alpha by `RoomState`; flickering lamps; debris spots removed when swept; restored-room light fade-in + toast + ORBI line)
- Modify: `GameFlow.Emergency.cs` (flare: warn via `OrbiChat`; prepared = `switches` task done today + player in lounge; else `_restore.Blackout()`)

- [ ] **Step 1:** Base autoshot shots: `r07_dark_start`, `r07_room_restored`, `r07_blackout`; check `Blackout_UnpowersRestoredRoomsAndAddsReset` path in game (rooms dark again, reset "!" shown).
- [ ] **Steps 2–4**, review shots by eye. **Step 5: Checkpoint.**

### Task 8: Weather of the day

**Files:** Modify `GameFlow.Restore.cs`, `GameFlow.Base.cs` (`BLight`, dust fog)
- Morning: ORBI chat line with today's weather and real-range temps (day ~0 °C / night ~−70 °C, colder on `cold`).
- `cold`: heater uses +0.5 power tonight; `dust`: solar −1 tonight, outside fog; `hot_inside`: `fix_cooling` task appears in the air room (pipes puzzle, 1 h).
- [ ] Shots per weather via `-seed`; **Checkpoint.**

### Task 9: Comms room, relay pass, ending, lose screen

**Files:** Modify `GameFlow.Restore.cs`; reuse `PzTune`, `BCutscene`, end screen code in `GameFlow.Lifeboat.cs` (`LbView.End`)
- Comms tasks: `c_power` (wires), `c_dish` (tune); then `send` only when `RelayOpen` (ORBI says next pass time; HUD countdown while open).
- Win: `Signalled` → that night sofa celebration → next morning free walk (no tasks) → ORBI "ship is here" → walk to hatch → goodbye line → pickup cutscene (reuse landing cutscene reversed or a new short one) → end screen (days, rooms, friends) + "play again".
- Lose: O₂ 0 or health 0 → lose screen with reason + "retry this morning" (nightly save, extend `DaySave` with `RestoreSnapshot`) + "new game".
- [ ] Bot reaches win and lose; **Checkpoint.**

### Task 10: Night

**Files:** Create `Assets/Scripts/Game/ArtNight.cs` (star field, Phobos/Deimos sprites, Earth dot, green aurora band); modify `GameFlow.Base.cs` `BLight` (blue sunset tint at dusk, night sky layer outside, window light pools), `GameFlow.Restore.cs` (after dinner: night free walk until the player sleeps at sofa/bed or 22:00; aurora on the night after a flare; stargazing spot outside → ORBI fact; suit air drains 1.5× at night).
- [ ] Shots `r10_dusk_blue`, `r10_night_sky`, `r10_aurora`; **Checkpoint.**

### Task 11: Ship intro to ~3 minutes

**Files:** Modify `GameFlow.ShipTop.cs` (fewer morning jobs: keep 2 of the current list), `GameFlow.Story.cs` (shorter lines). Verify with `-autoshot -ship -play` log: intro + morning + scramble ≤ 180 s real time.
- [ ] **Checkpoint.**

### Task 12: Bots, balance, strings, docs

**Files:** Modify `GameFlow.PlayShot.cs` (restore brain: careful = plans the day by hours left, drinks/eats, prepares for flares, sends at relay; careless = random tasks, ignores warnings and flares); `config.json` numbers; `tools/jobs_strings.py` keys; `docs/redesign-v2.md` round 11; `docs/educator-guide*.md` play time.
- [ ] Run both bots on seeds 11–15 (one after another, not in parallel). Target: careful signals in ≥4/5 within ~20 min real time at 1x-equivalent; careless loses 5/5.
- [ ] All EditMode tests pass; base autoshot `checksFailed=0`.
- [ ] **Checkpoint**, then ask the user to commit.
