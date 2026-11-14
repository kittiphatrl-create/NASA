# LAST DOCK on Mars: work plan from the user's request of 2026-11-15

This plan merges three things:
- the LAST DOCK spec (turn-based station, 6 incidents),
- the Mars management design (`MANAGEMENT_GAME_HANDOFF_2026-11-14.md`),
- the user's latest instructions.

Every point the user made is listed below with how it will be handled.

## Agreed direction

- The lifeboat **lands on Mars** next to an abandoned base. The "station" from LAST DOCK becomes that **Mars base**: the background is Mars ground, not space, and there is an outside area to explore.
- Keep the LAST DOCK rules: turn-based days, each action costs time, incidents with at least 2 fixes and a NASA fact.
- Bring back from the Mars design: going outside to explore, radiation, solar storms, the rover, and unlabelled food you gamble on.

## The user's points, item by item

| # | What the user asked | How it will be handled | Phase |
|---|---|---|---|
| 1 | Character controls are off: carrying only faces forward | Blender renders carry poses in every direction | 1 |
| 2 | Walk diagonally at 45° with matching animation | 8 directions: render 5 (S, SE, E, NE, N) and mirror the rest | 1 |
| 3 | Running animation | New run frames, bigger stride, body leaning forward | 1 |
| 4 | Interaction animations for objects | "work/use" pose in every direction, faces the object | 1 |
| 5 | Remove the pet system completely | Turn off pet pick, pet following and pet menu in every flow that is still played | 1 |
| 6 | Doors that open and close automatically, with sound | Door sprites from Blender; open when the player is near; door sound | 1 |
| 7 | Remove room signs once doors exist | No more HUB/STORE/COMMS signs | 1 |
| 8 | The ship lands on Mars, so the background should be Mars | Base map uses Mars ground and rocks around the base | 1 |
| 9 | Add 5 rooms; 2 of them hold strong items (portable solar: +1 power a day; radio repair kit: "repair signal" quest takes 1 day instead of 3) | 5 new rooms on the ship, where the 60-second grab happens. 2 locked rooms hold the strong items | 2 |
| 10 | Only one key card, found at random; it opens only one room | The key card spawns at a random spot on the ship each run. Using it on one locked room spends it. The player has to choose | 2 |
| 11 | Random items to grab in the 60 seconds: radiation meter, frozen food box, repel gun/flashlight, medicine, others | Each run places a random set of items in the rooms. Each item has a real effect at the base (see table below) | 2 |
| 12 | Every room has detail and a reason to exist | Each room gets a purpose, props that fit it, and its own floor | 2 |
| 13 | Find a layout skill | No layout skill in the claude.ai catalog. Used the local `unity-environment-level-design` skill (routes, doors, landmarks, clearance) | done |
| 14 | Make the player feel pressure and challenge | See "Pressure" below | 3 |
| 15 | Can old systems come back? Where are going outside and the rover? | See "Old systems in the code" below. Outside exploration and the rover are rebuilt as base actions | 3 |
| 16 | Study 60 Seconds! Reatomized, Fallout (Shelter) and No Time to Relax | See "What we borrow" below (mechanics only) | 2–3 |
| 17 | Fix the UI/UX to match the references | The Pinterest pins are UI motion (card selection with gold frames), line icon sets and loading animations. The images show dark ornate panels and an elegant title menu. Restyle panels, cards and icons. Note: `GameUI.cs` belongs to the other editor, so add new styles in a separate file | 4 |
| 18 | Use skill.md files that fit; sculpt in Blender like clay | Skills: `blender-mcp-3d-modeling`, `pixel-art-game-pipeline`, `unity-vfx-audio-game-feel`. Model with sculpt mode (Clay Strips/Grab/Smooth on a remeshed base), not by stacking primitives | 4 |
| 19 | VFX, effects, sound, music, global volume, shaders, skybox | Built-in renderer, so no URP Volume: add a post-processing script (bloom, vignette, grading), shaders (light flicker, heat shimmer, dust), menu skybox, music per situation | 4 |
| 20 | Read everything carefully before acting | This document | done |

## Item effects (phase 2)

| Item | Where | Effect at the base |
|---|---|---|
| Portable solar panel | Locked room A | Power +1 every day |
| Radio repair kit | Locked room B | "Repair signal" quest takes 1 day instead of 3 |
| Key card | Random spot | Opens one locked room, then it is spent |
| Radiation meter | Random | Shows the high-radiation spots before you go outside |
| Frozen food box | Random | Safe food, no chance of bad food |
| Repel gun / flashlight | Random | Drives off intruders on dark nights, or trade with a stranger |
| Medicine | Random | Crew health +1 |
| O₂ / battery / food / parts | Spread around | Fill the bars directly (already exists) |

## What we borrow (mechanics only)

- **60 Seconds! Reatomized:** grab items under time pressure; daily journal events with choices that depend on the items you have; rationing; going outside with risk.
- **Fallout Shelter:** each room has a job; incidents start in a specific room and get worse if ignored; rushing a job pays more but risks failure.
- **No Time to Relax:** a day's time budget split between necessary jobs and resting; obvious trade-offs.

## Pressure (phase 3)

- Every day the power left is shown against how much the base must use. A countdown to the rescue ship.
- Things build up when ignored: an incident left one day spreads to the next room.
- Warnings before danger: ORBI warns one day before a solar storm; sounds at the door on dark nights.
- Scarce resources: not enough parts to fix everything, so the player must choose.
- Sound and picture follow the situation: red flashing light, heartbeat when O₂ is low, power cuts that dim the screen.

## Old systems in the code (can they come back?)

| System | File | Status | Bring back? |
|---|---|---|---|
| Walking the Mars base, outside area, airlock and suit | `GameFlow.Base.cs`, `GameFlow.Outside.cs` | Turned off (walk-only) | Yes, as the base's outside area |
| Radiation dose outside + shelter | `GameFlow.Radiation.cs` | Turned off | Yes, works with the radiation meter |
| Emergencies with repair parts | `GameFlow.Emergency.cs` | Turned off | Partly; LAST DOCK incidents already do this job |
| Splitting food at dinner | `GameFlow.Meal.cs` | Turned off | Yes, for the food gamble |
| ORBI chat warnings on the left | `GameFlow.Orbi.cs` | Usable | Yes, for warnings and pressure |
| Old rover (salvage spot only) | `GameFlow.Outside.cs` | No real rover system | Build new: parts, charging, upgrades |
| Repair puzzles | `GameFlow.Puzzles.cs` | Usable | Yes, for the signal and rover quests |
| Rescue ship cutscene + star summary | `GameFlow.RestoreEnd.cs` | Usable | Yes |
| Pets, visitors, peephole | several files | User said to cut | No |

## Phase order

1. **Feel of control:** 8 directions, running, carrying and using in every direction, no pets, automatic doors with sound, no signs, Mars background.
2. **The 60 seconds:** 5 new rooms, 2 locked rooms, random key card, random items with real effects.
3. **The Mars base:** going outside, radiation, storms, rover, food gamble, pressure.
4. **Look and sound:** UI/UX per the references, clay-sculpted Blender models, VFX, music, post-processing, shaders, title skybox.
