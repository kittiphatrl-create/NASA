# Operational UI refresh

A restrained presentation pass based on the supplied UI-system / UX-redesign notes and warning/day-sticker references. Existing room layout, gameplay actions, save format and balance remain unchanged. No reference-game artwork was imported.

## Changes

- Shared console headers use the existing outlined pictograms and the existing Titan Gate font pipeline. Power rows use compact ON/OFF switches; explanations remain in tooltips. Night forecasts use icon/number chips. Machine percentages retain their labels and condition bars; amber now starts at the requested presentation threshold of 30%, independently of simulation break chances.
- Rescue progress uses four labelled stages. Action cards have larger icons, a lock for unavailable actions, compact costs, and no empty "no change" line. Room panels reuse the existing teal frame and scroll at large text sizes. Existing room grouping remains in place.
- Evacuation instructions have a tilted orange/red warning heading, hazard trim, capacity pictograms and one primary start action. The running countdown separates seconds, three carry slots and eight pod slots; late ticks and pickup-slot feedback are added. There is no new flashing screen effect.
- The in-game M shortcut has an outlined book icon; the day/work badge is separate from the warning timer. HUD objects are removed with the existing play overlay on world cleanup.
- UiMotion provides unscaled, cancellable feedback with reduced-motion handling. Existing animated panel/card entrances now use it. Sound loops pause when game time is paused and resume afterwards; explicit loop-stop requests cancel paused loops as well.
- Added synthesised tick, tick_hi, stamp, slot, stinger, toggle_on, toggle_off cues through the existing SFX/settings route. No downloaded audio was added. Dialogue blips stay muted.

## Verification

- Unity MCP and engine APIs only; no desktop input automation.
- All 147 EditMode tests passed.
- Six focused checks passed: loop pause, time pause, loop resume, reduced-motion pop, reduced-motion panel visibility, and free switches preserving work units. One earlier QA attempt used a reflection property where the simulation exposes a field; that QA harness was corrected before the accepted run.
- Eight console views captured in EN at a 1920x1080 UI root and TH at 130% text in a constrained 1280x720 UI root. The latter is a constrained logical viewport inside a 1920x1080 GameView capture, not a native 720p player capture. ScrollView "viewport=0" entries in the text log refer to an unrelated hidden scroll selected by the query, so those metrics are not layout-pass assertions. Screenshots are the layout evidence.
- Editor was reopened with D3D11 after an earlier D3D12 device crash. The crash is not counted as a passing test run.

Screenshots and focused results: `production/qa/evidence/operational-ui/`. Original before-views were supplied in the task. The full gameplay and Windows-build results are recorded below after completion.

## Intentionally restrained scope

The main menu, pause deck, maps, room order and game rules were retained. This does not implement every elaborate animation proposed in the master prompt: no full-screen boss-style interstitial, rotating countdown ring, Airlock grid replacement or narrated clips were introduced. The warning panel and timer adopt the reference language through our own typography, pictograms, angled heading and hazard trim. No claim is made that every prompt checklist item was implemented.

## Captured comparisons

| Screen | Supplied before | Current after |
|---|---|---|
| board | [before](../production/qa/evidence/operational-ui/before-board.png) | [after](../production/qa/evidence/operational-ui/en-1920-board.png) |
| life | [before](../production/qa/evidence/operational-ui/before-life.png) | [after](../production/qa/evidence/operational-ui/en-1920-life.png) |
| thermal | [before](../production/qa/evidence/operational-ui/before-thermal.png) | [after](../production/qa/evidence/operational-ui/en-1920-thermal.png) |
| storage | [before](../production/qa/evidence/operational-ui/before-storage.png) | [after](../production/qa/evidence/operational-ui/en-1920-storage.png) |
| communications | [before](../production/qa/evidence/operational-ui/before-communications.png) | [after](../production/qa/evidence/operational-ui/en-1920-communications.png) |
| airlock | [before](../production/qa/evidence/operational-ui/before-airlock.png) | [after](../production/qa/evidence/operational-ui/en-1920-airlock.png) |
| dock | [before](../production/qa/evidence/operational-ui/before-dock.png) | [after](../production/qa/evidence/operational-ui/en-1920-dock.png) |
| evacuation | [before](../production/qa/evidence/operational-ui/before-evacuation.png) | [after](../production/qa/evidence/operational-ui/evacuation-rules.png) |

Dinner had no distinct supplied before-view; its after-view is included with the other console captures.

## Final gameplay run

The full automated seed-12 flow reached rescue on day 7: `done errors=0 checksFailed=0`. It includes the real scramble, room actions and save restoration, with accelerated time and the existing hand-minigame QA bypass. Player preferences were restored after testing.

## Build

Final Windows build: `Succeeded errors=0`, refreshed 23:23 Bangkok time on 15 November 2026, including the last shortcut-caption cleanup. Output: `Builds/Win/TitanGate2177.exe`. The build folder remains local/ignored; source and review evidence are committed.
