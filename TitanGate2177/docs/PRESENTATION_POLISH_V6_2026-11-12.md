# Titan Gate 2177 — presentation pass V6

Built and checked locally on 12 November 2026. This extends the existing uncommitted game; no reset, commit, push, branch deletion or scene replacement was performed.

## Open and review

- Windows player: [TitanGate2177.exe](../Builds/Polish/TitanGate2177.exe). Keep its adjacent data/runtime folders together.
- [Menu, settings, dusk/night, conversation and pause preview with actual game audio](../production/qa/evidence/polish-v6-2026-11-12/menu-and-night-preview.mp4), about 21 seconds.
- [Automated base playthrough with actual game audio](../production/qa/evidence/polish-v6-2026-11-12/base-walkthrough.mp4), about 2 minutes 42 seconds. This run admits several visitors and **ends in oxygen failure on day 4**. It is an ending-to-ending verification run, not evidence of a successful seven-day rescue or a manual human playtest.
- [English title](../production/qa/evidence/polish-v6-2026-11-12/ux/03_title_en.png), [night](../production/qa/evidence/polish-v6-2026-11-12/ux/06_night_hud.png), [pause](../production/qa/evidence/polish-v6-2026-11-12/ux/08b_pause_th.png), [Aya conversation](../production/qa/evidence/polish-v6-2026-11-12/ux/07_crew_conversation.png).
- Frame sheets: [cadet](../production/qa/evidence/polish-v6-2026-11-12/ux/frames_cadet.png), [Aya](../production/qa/evidence/polish-v6-2026-11-12/ux/frames_aya.png), [Jules](../production/qa/evidence/polish-v6-2026-11-12/ux/frames_jules.png), [Pip](../production/qa/evidence/polish-v6-2026-11-12/ux/frames_pip.png), [dog](../production/qa/evidence/polish-v6-2026-11-12/ux/frames_comet.png), [cat](../production/qa/evidence/polish-v6-2026-11-12/ux/frames_pixel.png), [rabbit](../production/qa/evidence/polish-v6-2026-11-12/ux/frames_luna.png). All other visitor sheets are alongside these.

The videos use Unity-rendered screenshots and their recorded timestamps, plus the actual AudioListener mix. No desktop, microphone or private-window capture is involved. Frame captures are approximately 12 fps, encoded to a compatible 30 fps MP4; this is **not** a measurement of game FPS. The final preview contains 240 captured frames with no coalesced requests. Audio measurements and video durations are in the adjacent JSON files. Automated pauses and rapid choices make these verification videos different from normal player pacing.

## Resulting behavior

- Title: removed the bottom-right mission-caption box and the oversized outer menu card, increased the foreground ship and moved Mars behind it, retained the original key art, strengthened the amber primary action and grouped secondary actions. A subdued left-side fade protects title legibility.
- Click the ship to test its engines; click ORBI to make it react. These controls also support keyboard focus. Corrected ORBI's hit area after comparing it with the rendered robot. New-mission launch has a short reaction and guards against repeated activation.
- Shared button feedback adds hover/focus lift, press response and UI sound. Pause and settings use the same spacecraft-panel treatment. Reduced-motion preference suppresses movement where applied.
- Replaced the status headline, numerical morale label and heart row with an expression badge, morale fill and runner/stamina fill. Resource cards retain icon, color, name and fill. Percentages still normalize to the highest reserve seen in that run; Tab exposes real stock units. They are not physical tank-pressure readings.
- Cadet, human and alien frames are authored at **64×80 source pixels**, rather than simply enlarging the old art. The world height remains controlled by 44 pixels per unit. Side profiles have one visible eye and a nose; back views show hair/backpack; eight gait phases use narrower hip alignment and bent-knee passing poses.
- Player gait advances with actual movement distance, including a coherent whole-body flight offset when running. Standing against a wall does not drain sprint stamina. Sprint exhausts, waits before recovery and requires recovery to 30 before it can resume; it resets for a new day. The ship and base both expose stamina. Sprint unit coverage is independent of visual preference.
- Existing Aya/Jules residents retain walk-to-work routines and nearby E conversations. Dogs, cats and rabbits retain distinct directional sheets, contact shadows and their existing animation pipeline. This pass does not replace every pet with 64px source art.
- Added friendly **Pip**: wave/trade/refuse on day 1; a wave sets a persistent friendship flag that can unlock a day-4 return with supplies. This supplies one small recurring event chain rather than claiming a complete new narrative campaign.
- Added seven real, downloaded CC0 Kenney interface clips for hover, click, opening, closing, confirmation, engine interaction and drone reaction. Existing generated sounds remain as fallbacks and for unrelated effects. Volumes for frequent UI feedback are reduced.
- Retained the previous pass's action-based workday, reserved story time, Thai text shaping, practical light pools, contact shadows, mats and greenery. See [previous pass](UX_POLISH_2026-11-12.md) for their exact rules.

## Research, observations and rights

| Reference | What was studied | Applied here |
|---|---|---|
| [AdamCYounis — Pixel Art Class: 8 Directional Walk Part 1](https://www.youtube.com/watch?v=aios4kDJZsc) | Directly sampled around 20:47 (walk/run differences), 27:33 (front leg) and 40:57 (front arm). This was selective viewing, not the whole tutorial. | Separate limb construction, directional silhouettes and coherent stride/flight timing; the sprites here are newly authored. |
| [Neverknights artist tutorial — pixel-art walk cycle](https://neverknights.de/en/tutorials/pixel-art-walk-cycle/) | Contact, passing and intermediate poses across an eight-frame cycle. | Narrower stance, segmented knees and controlled displacement. |
| [EA official Sims 4 player guide](https://cdn-assets-ts4.pulse.ea.com/Guide/TheSims4_PlayersGuide_ENGLISH.pdf) | Portrait/emotion communication and needs hierarchy. | A face communicating condition beside separate mood/stamina bars. No Sims graphics were imported. |
| [Eastward official media](https://eastwardgame.com/media/) · [gameplay preview](https://www.youtube.com/watch?v=hOlZjUSRqiY) | Previously sampled at 02:25 and 07:18: warm practical lights, overlap, compact corner HUD and clear silhouettes. | Existing layered room treatment and lighting. Not newly watched in full during V6. |
| [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds) | Downloaded official 1.0 pack, checked its included CC0 license. | Seven bundled .ogg clips. [Original license](Kenney-Interface-Sounds-LICENSE.txt); [source and SHA256 manifest](kenney-ui-audio-provenance.json). |

The earlier lighting URLs supplied by the user remain references, not imported assets. Several were previously unavailable; no claim is made to have watched their contents. No paid, ripped or unlicensed third-party sprites, UI layouts or game audio were imported. The new art and panels are project-specific implementations informed by these references.

## Verification and practical limits

- Windows build: **Succeeded, 0 errors**, using the saved project scene in Unity 6000.3.25f1. The user's open scene was not overwritten.
- EditMode: **65/65 passed**, including workday boundaries, time/resource atomicity, sprint exhaustion/recovery and minimal-loot seven-day rescue for every pet. Result: [XML](../production/qa/evidence/polish-v6-2026-11-12/polish-tests.xml).
- Actual base player run: **48 passed checks, 0 failed checks, 0 runtime errors**, reached the day-4 oxygen-failure ending. Covers walking, work, first-person visitor inspection, infrared effect, resident conversation and evening gathering.
- Actual ship player run: **29 passed checks, 0 failed checks, 0 runtime errors**, reached impact, timed scramble and landing with five supplies.
- UX showcase: **13 passed checks, 0 failed checks, 0 runtime errors**. Covers morning/dusk/night, real menu settings action, rejected work atomicity, E-interaction handler, NPC movement, pause freeze/resume, authored sprite width, audio inclusion, title reactions and rendered ORBI hit area. See the [runtime log](../production/qa/evidence/polish-v6-2026-11-12/ux/autoshot-log.txt).
- Default 1280×720 gameplay and 1920×1080 UX were inspected. Other aspect ratios, text scaling, all keyboard/controller paths and a sustained performance profile have not been certified.
- The provided relevant skills were already installed; the [existing audit](skill-audit-2026-11-12.json) records 11 present, with no duplicate installation.

This is a substantive polish pass, not a complete cinematic/map redraw or a claim of final animation quality. Repeated guest choices, how well players understand dwindling air, readability of icon-only condition, and subjective gait feel still need a fresh human playtest. The recorded failure demonstrates the resource pressure of the chosen admissions; it does not prove that admitting every guest is balanced or that a new player will understand the consequences soon enough.
