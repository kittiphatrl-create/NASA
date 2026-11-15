# Main menu upgrade: changelog (2026-11-15)

The LAST DOCK main menu was rebuilt. The game's existing actions are kept: start game, continue, how to play, card book, NASA sources, settings, credits, quit and language.

## Where the code is

| Part | File |
|---|---|
| Design tokens: colour, type, 8 px spacing, motion, fonts | `Assets/Scripts/Game/Menu/MenuTokens.cs` |
| MenuButton, MenuListItem, MenuChip, MenuTooltip, InputPrompts | `Assets/Scripts/Game/Menu/MenuWidgets.cs` |
| Input from mouse, keyboard, gamepad and touch; last-used device; key repeat | `Assets/Scripts/Game/Menu/MenuInput.cs` |
| SceneBackground: art layers, parallax, idle, dust, beacons, rover, scrim, vignette, grain | `Assets/Scripts/Game/Menu/MenuScene.cs` |
| Generated textures (dithered gradients, grain, glows) | `Assets/Scripts/Game/Menu/MenuFx.cs` |
| 24-unit line icon set (anti-aliased distance fields) | `Assets/Scripts/Game/Menu/MenuIcons.cs` |
| Reader for the Blender point data | `Assets/Scripts/Game/Menu/MenuPointsJson.cs` |
| Layout, focus model, intro, departure, sub-menu transition, audio (TransitionManager) | `Assets/Scripts/Game/GameFlow.MainMenu.cs` |
| Entry point (`LdBuildTitle` now calls the new menu) | `Assets/Scripts/Game/GameFlow.LdTitle.cs` |
| Strings, EN and TH (`mm.*` keys) | `Assets/Resources/Data/strings.json` |

Small changes in other files:
- `Sfx.cs`: adds `PlayVaried` (±3 % pitch), and `Loop` is now null-safe.
- `GameFlow.Dinner.cs`: the save also stores the day reached and the time it was saved.

## The 8 problems from the review

1. **Hard seam in the left gradient.** The old scrim used `pow(x, 0.4)`, which drops steeply at its edge, and covered only 58 % of the width. The new scrim covers the full screen. It combines an eased linear falloff with a large radial pool behind the column, plus a soft bottom fade for the footer. All gradients are dithered (±0.75/255), so there is no banding.
2. **Seven icon-only buttons.** Replaced by a two-column list with an icon and a label on every row: Mission Log, Codex, NASA Sources, Settings, Credits and Quit. The top-right chips (language, sound, effects) show tooltips after 300 ms.
3. **Continue looked enabled.** It now has a real disabled state: 40 % opacity, a lock icon, no hover, skipped by keyboard and gamepad focus, a tooltip "Start a mission first" and a muted buzz on click. Start Mission is the default focus. When a save exists, Continue becomes primary, shows "Day N of 7 · Saved 2 h ago", and Start becomes "New Mission" with a confirmation step.
4. **Uneven title.** The logo is a single lockup with one explicit word gap, a thin accent rule, a mask-wipe reveal and one shimmer sweep.
5. **Tagline row.** Split into two levels: a location line ("☀ MARS · 2177", small tracked caps, secondary colour) and the tagline (28 px, primary colour, sentence case).
6. **Flat scene.** The key art was re-rendered in Blender 5.3 (EEVEE, AgX), in a copy of the scene, `Art/Blender/mars_keyart_menu.blend`; the original file is unchanged.
   - Camera: a low hero shot from behind, about 40° field of view, 1.4° dutch tilt. The astronaut sits on the right third.
   - Lighting: a warm golden-hour sun with a real disc and baked bloom, a warm rim light on the astronaut, cool teal shadow fill, and lit portholes and door lamps.
   - Atmosphere and grade: distance and height haze, so the mountains fade out; baked depth of field; a mild teal-shadow / warm-highlight grade.
   - The muddy blue shapes are now dark solar panels and a glass greenhouse. The antenna mast now reaches its beacon (it used to float and looked like a second sun).
   - In the menu, the layers get a 12 s breathing dolly, smoothed mouse or right-stick parallax (about ±2°, stronger on the astronaut layer), and dust in three depth bands.
   - The menu also adds blinking habitat lights at the positions Blender exported, a rover that crosses the open plain every ~40 s, a sun-glow pulse, a vignette and animated film grain (about 2.8 %).
   - Low / Medium / High effects quality (top-right chip): Low drops grain, near dust and the rover.
7. **Static character.** The character breathes, shifts weight on a 7 s cycle and, when the look pose exists, looks toward the base every 8–15 s. It has its own parallax layer.
8. **Missing standard elements.** Added a footer with version and copyright, input prompts that switch when the device changes (mouse, keyboard, gamepad, touch), an intro of 3 s or less that any input skips, a 1.25 s departure (UI fades, camera pushes toward the airlock, fade to black), a sub-menu slide-in with the scene dimmed behind it, music that drops 6 dB under sub-menus, a wind bed and a distant hum, and audio that mutes when the window loses focus.

## Fakes and limits (the stack has no live 3D menu scene)

- The background is pre-rendered key art, not a live 3D scene. So depth of field, bloom, fog, rim light and the sun glow are baked in Blender, and "camera motion" is 2D parallax and scale on separate art layers. The Built-in Render Pipeline has no post-processing volume, and UI Toolkit draws over the camera, so camera post effects could not reach the menu art.
- Chromatic aberration was left out. It would need a full-screen shader pass over UI Toolkit, and the brief asks for restraint.
- UI Toolkit transitions cannot use a custom cubic-bezier. CSS-style transitions use EaseOutCubic; script-driven animation (intro, departure) uses the exact token curve `cubic-bezier(0.22, 1, 0.36, 1)`.
- The sub-menu panels (Settings, Credits and the others) still use the existing modal style from `GameUI.cs`, which is the other editor's file. They now slide in with the scene dimmed behind them. Restyling them to the new tokens is left as a follow-up.
- The wind bed is procedural (filtered brown noise with gusts), standing in until a licensed recording is added.

## Assets

New, all in `Assets/Resources/`:

| Asset | Source | Licence |
|---|---|---|
| `Fonts/CinzelDecorative-Bold.ttf`, `Fonts/Cinzel-SemiBold.ttf`, `Fonts/Cinzel-Regular.ttf` | Google Fonts (Natanael Gama) | SIL OFL 1.1, `docs/Cinzel*-OFL.txt` |
| `Fonts/Taviraj-Medium.ttf`, `Fonts/Taviraj-SemiBold.ttf` | Google Fonts (Cadson Demak) | SIL OFL 1.1, `docs/Taviraj-OFL.txt` |
| `Sprites/KeyArt/menu_bg.jpg.bytes`, `menu_cadet.png.bytes`, `menu_cadet_look.png.bytes`, `menu_logo.png.bytes`, `menu_rover.png.bytes`, `menu_points.json` (1.3 MB total) | Rendered for this project from the team's own Blender scene | Project art |

The fonts are credited in the Credits screen (`credits.3.body`).

To re-render the key art, run `Art/Blender/menu_pipeline/run_all.sh` (about 65 s), then copy `Art/Blender/renders/menu/*` into `Assets/Resources/Sprites/KeyArt/` as `*.jpg.bytes` / `*.png.bytes`. `renders/menu/_work/` holds intermediate renders only.

Not created, with placeholders wired in:
- **Wind bed:** procedural (see above). For a recorded one, try Freesound "mars wind" or "desert wind loop" filtered to CC0, or Sonniss GDC bundles (royalty-free).
- **Music:** the menu reuses the CC0 "sector" track from the Dark Sci-Fi Audio Pack already in the project; it ducks under sub-menus.
- **UI sounds:** these reuse the existing Kenney (CC0) and synth sounds: hover tick, confirm "console" click, back "close", disabled "bad" at low volume, and the intro sting "card". All play with ±3 % pitch. A deeper airlock "thunk" for confirm would fit better (Kenney Sci-Fi Sounds `forceField_*` / `impactMetal_*`, or a Freesound CC0 "airlock").
- **The user's door sound (Pixabay):** its licence still needs checking before submission (as in the earlier handoff).

## Acceptance checklist (2026-11-15, Unity 6000.3 Editor)

Screenshots, contrast reports and the checker script are in `Captures/tmp/main-menu-2026-11-15/` (git-ignored). The views are EN/TH × save/no save × 1920×1080, 1280×720 and 2560×1080.

| Check | Result | How |
|---|---|---|
| No seam or banding in the background gradient | **Pass** | Full-screen dithered scrim; checked on all 12 screenshots |
| Every icon and button has a label or tooltip; all states exist | **Pass** | List rows have labels; chips have tooltips (300 ms, below the chip when near the right edge); default, hover, focus-visible, pressed, disabled and selected states |
| Continue disabled with no save; default focus on the primary action | **Pass** | Keyboard Down from Start goes to Mission Log (Continue skipped); with a save, Continue is primary and focused |
| Logo spacing even; hierarchy Title > Tagline > Location | **Pass** | Blender lockup with the swash pair opened, trimmed to the letters and aligned to the column |
| Fog, dust, bloom, depth of field, vignette, animated character, parallax | **Pass (with fakes)** | Fog, bloom and depth of field are baked in Blender (no live 3D menu); the rest is live in UI Toolkit |
| Intro plays once, can be skipped, respects Reduce Motion | **Pass** | About 3 s on the first visit; any input skips it; Reduce Motion gives a 0.45 s fade with no push, shimmer, drift or parallax |
| Mouse, keyboard, gamepad and touch; prompts switch automatically | **Pass (touch not tested on a device)** | Keyboard and gamepad were driven through Input System events (a virtual gamepad); prompts switch per device; rows are 48 px |
| TH and EN render without clipping | **Pass** | Thai uses Taviraj/Sarabun with extra padding; the column shrinks to fit on short screens (21:9) |
| ≥ 60 FPS at Medium; Low preset exists | **Partly checked** | F3 overlay: 59 FPS, capped by vSync at 60 in the Editor at 1920×1080 on High. Not yet measured on a mid-range laptop build |
| Text contrast ≥ 4.5:1 over the 3D background | **Pass** | All 12 views: lowest 6.21:1, measured around each on-screen label using the label positions from Unity |

Not covered here: the sub-menu panels keep their old modal style and have no keyboard or gamepad focus inside them (B or Esc closes them). Fix this together with `GameUI.cs`.

## Update: docking-ring menu (same day, after review)

The user asked to remove the location line, tagline, both cards and the two-column list, and to rebuild the menu in the logo's style. They chose concept C, a docking ring, with live gold text (not rendered words), no tagline, and sub-labels only on the selected item.

- **`Menu/DockDial.cs` (new).** A metal docking ring sits off the left edge, and the menu items ride on an arc just outside it.
  - Moving the selection turns the whole ring until that item docks at a fixed clamp at 3 o'clock. The list wraps around.
  - The docked item is larger and gold, with a warm glow. Only the docked item shows its sub-label ("Day 3 of 7 · Saved 2 h ago", "New game · about 15 minutes"); the items below move round the ring to make room for it.
  - Items fade and shrink with their distance from the clamp.
- **Input.**
  - Up and Down (keyboard, D-pad or stick) and the mouse wheel turn the ring.
  - Enter or A runs the docked item.
  - Clicking an item turns the ring to it, then runs it once it locks (about 0.3 s).
  - Right arrow reaches the top-right chips, and Left returns to the ring.
  - Esc or B turns the ring to Quit.
  - The disabled Continue (no save) is skipped and shows a lock icon.
- **Motion.**
  - Intro: the ring swings in about 70° and locks, with a latch "clank".
  - Departure: the ring turns away while the camera pushes in.
  - Reduce Motion turns the ring with no animation and keeps simple fades.
- **Art.** `menu_ring` and `menu_clamp` are rendered in Blender in the logo's metal (`Art/Blender/menu_pipeline/`). If they are missing, a generated gold ring is used instead.
- **Unchanged.** The logo lockup, chips, footer, input prompts, scene, audio and the F3 debug overlay.
- **Removed.** The old cards (`MenuButton`) and the list rows (`MenuListItem`) are no longer used by the menu. They stay in `MenuWidgets.cs` for reuse.
- **Ring art.** The ring has 12 latches and turns 30° per item (a 3:1 gear ratio against the 10° item spacing), so a latch always seats in the clamp. The clamp is drawn at the ring's scale, with its anchor on the rim at 3 o'clock.
- **QA (ring version):**
  - The 12 views are in `Captures/tmp/main-menu-2026-11-15/after/`; the card version is kept in `after_v1_cards/`.
  - Contrast of the docked item, the readable neighbours, the sub-label and the footer: lowest 6.21:1. Items near the ends of the arc fade below 50 % opacity on purpose; they reach full contrast when turned to the clamp.
  - Input, verified by trace: the mouse wheel steps the ring and skips the disabled Continue, the gamepad D-pad steps it, Right goes to the chips and Left comes back, and Esc turns the ring to Quit.
  - EditMode tests: 137/137.

### Ring follow-up: hover turns the ring, more sound, effects and motion

- **Hover turns the ring.** Pointing at any item turns the ring to it. It reacts only while the physical mouse is moving and the ring is nearly still, so items sliding under a resting cursor never chain-rotate. Disabled items show their tooltip and don't turn the ring.
- **Motion.**
  - The ring runs on a spring with damping ratio about 0.63: it overshoots a touch and settles like a heavy mechanism.
  - The ring never sits completely still: it sways ±0.35° at rest.
  - A docking label slides in 14 px; a light sweeps across it, its glow blooms once, and its sub-label rises in.
  - The clamp punches 7 px when an item locks or is chosen.
- **Effects** (`DockDial.cs`):
  - On lock: a flash at the clamp, a gold shockwave ring, an ember burst from the clamp tip and a halo pulse around the ring.
  - While turning: grinding sparks from the rim when the ring turns fast, and a halo that brightens with speed.
  - The amber indicator light on the clamp breathes and flares on lock.
  - Sparks and the shockwave are off at Low quality and with Reduce Motion.
- **Sound** (`Menu/MenuAudio.cs`, synthesised at start-up, no new files):
  - A ratchet tick for each item that passes the clamp, with pitch rising with speed.
  - A servo whir that follows the ring's speed.
  - A latch clunk with a metal ping when an item docks.
  - A warm two-note chime when an item is chosen.
  - The intro's lock uses the same clunk, with a clamp punch.
- **Checked.** Hover test (simulated mouse): one turn, then no drift with the cursor still. Reduce Motion: navigation works with no spring or sparks. EditMode 137/137. FX screenshots are in `qa_ring/`.

## Update: layered story backgrounds (ship / Mars)

The user approved a background that follows the story. With no save, the menu shows the ship scene, aboard the Kestrel in orbit before the accident. Once a run is saved, it shows the Mars base at a blue Martian sunset. The art is rendered in Blender as depth layers, with an idle loop for the astronaut.

- **Ship scene.** A dim cabin with a big round docking-port window (gold frame, echoing the menu ring). Mars is seen from orbit, with a terminator and a blue limb haze. The cadet stands on the right third.
  - Layers: space, planet, cabin (window cut out), cadet loop, foreground rail.
  - The planet drifts and turns very slowly in the window, as if the ship were in orbit. The cabin lights blink, and the dust floats slowly instead of blowing.
- **Mars scene.**
  - The sky follows the blue sunsets NASA photographed on Mars: a small sun with a cool blue aureole, and a rust and dusk sky.
  - Earth is a small blue evening star near the sun; it twinkles.
  - The orange lifeboat has landed beside the habitats, with a blinking beacon.
  - Layers: sky, mountains, base, cadet loop (with a ground shadow), dark foreground rocks.
- **Cadet idle.** 32 frames at 8 fps (a seamless 4 s loop): breathing, a weight shift and a small head turn. The next frame cross-fades in over an opaque current frame, so the body never turns see-through.
- **Engine** (`Menu/SceneSpec.cs`, `Menu/MenuScene.cs`, `MenuPointsJson.ParseScene`).
  - The background comes from `menu_scene_<ship|mars>.json`: layers back to front, each with a depth from 0 (far) to 1 (near) and a rect; optional sprite sheets; and points (sun, Earth, lights, window, airlock).
  - Each layer gets its own parallax, scaled by depth, and the camera push-in heads for the airlock (Mars) or the window (ship).
  - If a manifest is missing, the menu falls back to the earlier single key art.
  - Reduce Motion freezes the loops, the drift and the parallax.
- **Art budget.** Mars 5.65 MB and ship 7.35 MB of `.bytes`; only one scene is loaded at a time.
- **Re-rendering.** Run `Art/Blender/menu_pipeline/run_all.sh` (it now includes `layers_mars.py`, `ship_setup.py`, `layers_ship.py`, `idle.py` and `copy_scenes.sh`). New source files are `Art/Blender/menu_ship.blend` and `Art/Blender/mars_keyart_sunset.blend`.
- **Art choices.**
  - The sunlight on Mars comes from higher than the visible sun, to keep the shadows a sensible length.
  - The sun disc is stylised, larger than the true 2/3-of-Earth size, which would be about 11 px.
  - Earth is painted into the sky; Phobos is left out.
- **QA.**
  - 12 views (EN/TH × ship/Mars × 1920×1080, 1280×720 and 21:9): contrast lowest 6.45:1. Captures are in `Captures/tmp/main-menu-2026-11-15/scenes/`.
  - 60 FPS in both scenes in the Editor (vSync).
  - The logo was narrowed to 840 px so it clears the ship's window frame.
  - EditMode 137/137.

### Menu music per scene

- Each scene can have its own menu track, at `Resources/Audio/Music/Menu/menu_<scene>.mp3` (`menu_ship`, `menu_mars`). Without one, the menu falls back to the game's calm CC0 track ("sector"), as before.
- Behaviour:
  - The track swells in slowly and ducks 6 dB under sub-menus.
  - It keeps playing, without restarting, when the menu rebuilds (language change, closing Settings).
  - It fades out about 1.5 s after the player leaves the menu.
  - It is imported as a streaming Vorbis clip, so the whole file is never held in memory.
- **Licensing:** the ship track the user supplied, `menu_ship.mp3` (a commercial film score), is listed in this machine's `.git/info/exclude` only. It is **not committed**, so other clones play the CC0 fallback. Before any public build or submission, replace it with a licensed or royalty-free track. There is no Mars track yet, so Mars uses the fallback.

## Debug

Press **F3** on the main menu to show FPS, the active input device, the focus target, the effects quality and the reduce-motion flag.

## Update: space backdrop (evening of 15 November)

The user asked for the ship menu to drop the cadet and the porthole frame. It now shows open space: the Kestrel in orbit, a galaxy behind it and a large Mars on the right, with a camera that slowly zooms in on the ship. The rest of the menu UI is unchanged.

- **Backdrop.**
  - The new scene `menu_scene_space.json` has one layer: `menu_space.jpg` (the first frame) with the looping video `Resources/Cine/menu_space.mp4` drawn over it.
  - The video is 1920×1080 at 24 fps and loops seamlessly every 16 s. The camera swings about 8° round the ship and zooms in mid-loop (dolly plus 30→39 mm), then eases back out.
  - The ship's engines flicker. It shows a red and a green navigation light and a white double strobe.
  - The galaxy band has a warm core and dust lanes. Phobos sits near the limb of Mars.
- **Engine.**
  - `SceneLayer.Video` and the `"video"` key in the manifest add a `VideoPlayer` with a render texture over the still. The video fades in once its first frame is drawn, and it is stopped and freed when the menu closes.
  - Reduce Motion pauses the video.
  - Parallax, the departure push (toward `focus`), floating motes, the scrim, the vignette and the grain all work as before.
- **Which scene.** Before a run the menu shows the space scene, falling back to `ship`. Once a run is saved it shows `mars` (unchanged).
- **Re-rendering.** `blender -b --factory-startup --python Art/Blender/menu_space.py -- video` writes `Art/Blender/renders/menu/menu_space.mp4` and `menu_space_0001.jpg` (about 6 min). It reuses the cutscene asset kit and textures. Copy the video to `Assets/Resources/Cine/menu_space.mp4`, and the still to `Assets/Resources/Sprites/KeyArt/menu_space.jpg.bytes`.
- **Scroll sound.** The ring's ratchet tick is now a soft, glassy console tick, and the servo whir is about 8 dB quieter (`Menu/MenuAudio.cs`).
- **Music.** The menu has its own track, `Resources/Audio/UserScore/menu.mp3` (`StageMusicPlayer.Cue.Menu`), and the opening cutscene keeps `opening.mp3`. `mars.mp3` was replaced with the user's new Mars track and plays 3 dB lower, to match the others. See `docs/AUDIO_CREDITS.md`.

## Update: live 3D ship the player can turn and zoom

The user asked to make the menu ship interactive and to fill the empty left side. Before a run, the menu now draws the scene live in 3D. The looping video stays as the fallback.

- **Interaction.**
  - Press on empty space over the ship (right of the ring) and drag to turn the camera round it. A fling keeps turning briefly.
  - The mouse wheel over the ship zooms between 17 and 95 units. Over the ring, it still turns the ring.
  - Touch drag works the same way.
  - The footer says "Drag the ship to turn it · Scroll to zoom".
  - Clicks on menu items are never taken by the drag.
- **Idle.** After 4 s with no input, the camera eases back onto the video's path: a slow ±9° sway round the ship with a gentle push in, over 32 s. Reduce Motion stops the sway and the rocking.
- **Scene** (`Menu/MenuLive3D.cs`, a hidden camera on layer 30 rendering into a render texture that `MenuScene` shows as the `live3d` layer):
  - The Kestrel uses baked albedo, emission and normal maps. Its engine plumes are soft glows, and it has blinking red and green navigation lights and a strobe.
  - Mars, with a soft terminator and an atmosphere rim, is framed large on the right of the opening view. Phobos sits beside it.
  - Ten slowly tumbling asteroids sit mostly on the left.
  - The galaxy sky is a Blender panorama with a teal and violet nebula to the left. The stars are drawn in the shader, so they stay sharp at any zoom.
  - Lighting is computed in the menu's own shaders (`Resources/MenuLive/*.shader`), so scene lights cannot change it.
- **Assets.** `Art/Blender/menu_live.py` builds the same scene as the video and exports these files to `Art/Blender/renders/menu/live/`:
  - meshes in a small binary format (`.mlm`)
  - baked maps (`.jpg`)
  - `sky.jpg`, a 4096×2048 equirectangular panorama
  - `menu_live.json` (camera, lights, planets, engines, lights, asteroids)

  `Art/Blender/menu_pipeline/copy_live.sh` copies them into `Assets/Resources/MenuLive/`, about 6 MB in all.
- **Fallback.** If a file or shader is missing, `MenuLive3D.Create` returns null and the `live` layer plays `Cine/menu_space.mp4` instead.
- **Playtest.** `TitanGate2177.exe -autoshot -menulive` opens the menu and captures 5 views into `Captures/menulive/`: the default view, a drag, a zoom in, the far side and idle. It drives the drag and zoom through code and sends no desktop input. Result on 15 November: errors 0, and Mars at viewport (1.04, 0.44) in the opening view.
