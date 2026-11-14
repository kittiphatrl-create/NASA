# LAST DOCK: handoff notes (end of 2026-11-15)

Read this first. Details live in `docs/LAST_DOCK_MARS_PLAN_2026-11-15.md`, `docs/LAST_DOCK_SCIENCE_NOTES.md` and `docs/AUDIO_CREDITS.md`.

## The game now

1. **Title screen** (`GameFlow.LdTitle.cs`): Blender key art in two layers, the background and the cadet.
   - Mouse parallax, drifting Mars dust, a pulsing sun, and a title that builds letter by letter.
   - Two big cards: Start and Continue.
   - Icon tiles for How to play, Settings, Cards, Sources, Credits, Quit and Language. The label shows on hover.
   - Click the cadet and they hop and say a line.
2. **Cutscenes** (`GameFlow.Cinema.cs`): Blender stills with a slow push or pan, letterbox bars, one short line, and shake, flash or sound. Space skips.
   - Scenes: intro (`LdCinIntro`), debris impact (`LdCinImpact`), lifeboat launch and landing (`LdCinLaunch`), win (`LdCinWin`), lose (`LdCinLose`).
   - Images: `Assets/Resources/Sprites/KeyArt/*.jpg.bytes`.
   - Sources: `Art/Blender/story_space.blend` (space shots) and `Art/Blender/mars_keyart.blend` (Mars shots).
3. **Ship, morning** (`GameFlow.ShipTop.cs`): one job, the radio report, then the sofa. Debris hits, then the 60-second grab.
   - Carry 3 things; the lifeboat holds 8.
   - 5 extra rooms (`GameFlow.ShipRooms.cs`).
   - Key card at a random spot; it opens 1 of 2 vaults: portable solar panel or radio repair kit.
4. **Mars base, 7 turn-based days** (`GameFlow.LastDock.cs`, rules in `Core/LastDock/StationSimulation.cs`).
   - 3 work units a day and 6 incidents.
   - The radio quest takes 3 days.
   - Trips outside: airlock, cut to black, a results card (`GameFlow.DayFlow.cs`).
   - Rover, solar storm, mystery food, raiders.
   - Each day ends with dinner and then a "DAY N" card.
5. **Win**: signal 3/3 and 2 battery on day 7. **Lose**: shows the chain of causes.

## Look and sound

- Sprites come from Blender, made clay-style with remesh, displace and smooth modifiers. Sources are in `Art/Blender/`:
  - `cadet.blend`: 8-direction walk, run, carry and use poses.
  - `station_consoles.blend`: consoles and rover, ok/broken versions.
  - `station_tiles.blend`: walls, floors and doors.
- Each `.blend` has text blocks that re-render everything: `render8`, `render_all`, `tiles`, `decay`. Run them, then outline the renders and copy them into `Assets/Resources/Sprites/...` as `*.png.bytes` (see git history for the PIL snippet).
- Post grade shader: `Resources/Shaders/LastDockGrade.shader` with `LdPostFx.cs`.
- Music and sound are CC0 (Dark Sci-Fi Audio Pack, Kenney Sci-Fi/RPG/UI). The door sound came from the user. Credits are in `docs/AUDIO_CREDITS.md`.
- The dialogue blip is muted on purpose: the user asked for it.

## How to test

- EditMode tests: 130 in `Tests/EditMode/LastDockTests.cs`, all pass.
  - The Editor test runner stalls when Unity is not focused. Run the tests in batch on a copy of the project, or focus the Editor.
- Bot playthrough of a Windows build:
  - Full game: `Builds/Win/TitanGate2177.exe -autoshot -play -ship -fast -seed 12 [-battery|-dumb]`
  - Base only: same, with `-lastdock` in place of `-ship`
  - Logs and screenshots go to `Builds/Win/Captures/play/`. The log includes a reachability check: "every use spot reachable".
- Last run before this commit: build OK, 0 errors, full flow from the menu to the end screen.

## Still to do (user's wishes, not done yet)

- More text can become icons. Toasts, the ship HUD (`GameUI.cs`, the friend's file) and the night list still use sentences.
- The cadet in a spacesuit outside still uses the old code-drawn sprite.
- The impact cutscene reuses one render for 2 shots. A ship-interior shot with the cadet and the red alarm would make it stronger.
- Balance: a careful bot wins about 70% (intended to feel tense). Check with 2–3 real players and aim for a 15-minute run.
- Before submission, check the license of the user's door sound (Pixabay).
