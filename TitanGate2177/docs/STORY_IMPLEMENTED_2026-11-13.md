# Mentor story and mission interactions — 13 November 2026

The user approved a missing-mentor story and game-first delivery. The website is optional: required controls, science clues, knowledge cards and archive remain available in the game. No web backend, account, network requirement or compulsory external browsing was added.

## Playable sequence

The opening now introduces Earth in 2177, the fictional jump corridors, Kestrel and the junior astronaut with Commander Arun. Arun is present in the cockpit, gives the morning assignment, can be approached for advice and supplies the facts shown during tutorial work. The existing quest order still teaches food delivery, water recycling, plants and radio use before resting.

After the debris strike Arun explains the manual launch release. The player collects supplies, then sees Arun stay at the controls while the escape pod separates. His last transmission ends without confirming his death. The small pod lands and docks with the existing Mars emergency habitat. The seven-day rescue ending now includes a broken transmission with his call sign and saved coordinates; this is an epilogue clue, not a newly implemented search chapter.

The original Kestrel and Mars maps were already different. The large ship remains a working ship with multiple rooms; the landed pod is one small room connected by a tube to the habitat. The new docking sequence makes this distinction explicit.

## Interaction behavior

- Saved the reachable pre-seat position and restored it before the scramble. Verification checks floor clearance and direct movement before pathfinding can conceal the sofa regression.
- Added original 64×80 action sprites: side-seated, watering can with drops, repair tool, kneeling plant work, pet play, feeding, lifting and carrying. The player uses seated poses on the sofa and at the evening pod gathering; humanoid residents also sit at gathering and Aya/Jules show work poses during routines.
- Short action coroutines clear the walking target and release control on completion. Lock ownership follows the coroutine rather than a separate timer, preventing a one-frame overlap from cancelling the next action. Pause uses scaled action time; switching worlds invalidates old completion callbacks.
- Objects carried on the ship are positioned at hand level; the carrying pose replaces the idle body, with gait frames while moving. Emergency pickup and stowing have a short lifting pose. These are procedural sprite animations, not imported skeletal animation clips.
- Nearby pet prompts use the same nearest-target selection as E, so the icon does not promise a different interaction. The portrait reflects the selected pet on the ship, correcting the old hard-coded dog fallback.
- Pet menu: Play, Wait Here, Come With Me, Close. Waiting holds position with a resting pose, following resumes the trail, and play uses a kneeling pose, a small pet hop and a heart reaction. These are optional cosmetic interactions: no supply, work-hour or morale charge/bonus. The ship emergency restores following.
- The original diary mode deliberately retained a busy flag. Walkable-base pet selection now clears that flag so the new action guards do not lock movement after landing.
- Gameplay HUD and interaction prompts are hidden during base-to-stage cinematics and restored by the ordinary gameplay refresh afterwards.

## Visitor identities and existing rules

- Tik: mint slime courier, rounded elastic silhouette and internal highlights.
- Pip: smaller pink gel courier with a pack and waving gesture.
- Mu: aquatic visitor in a tracked water vessel, animated bubbles and visible aquatic body. Its accepted visit lasts one night; it receives its existing ration and provides its existing help before leaving. Departure is logged separately from hunger.
- Nok: salvaged bioservice android with a wheeled base, antenna and living repair core. Scan/choice text explains why that core still needs the existing air, water and nutrient ration. Its costs were not silently replaced with invented robot power rules.
- Zee: adult human traveller with a pink expedition outfit and distinct hair silhouette. Aya/Jules retain their established roles and resident routines.

Visitor identifiers, scan gating, established admission choices and the previous Pip friendship event remain compatible. Slime motion, water-vessel bubbles and robot mechanisms differ instead of reusing the same humanoid with different colors. There are still established antagonistic events; this pass does not claim to have rewritten every possible visitor outcome as harmless.

## Verification

Verification outputs are collected in `production/qa/evidence/story-2026-11-13/` after the final runs. The source includes an opt-in `-autoshot -storyshowcase` route for pet commands, action completion, cinematic camera restoration and the distinct pod/habitat floor treatments. The actual ship route covers tutorial work, the seated sofa transition, scramble and landing. Temporary-guest EditMode cases verify both one-night departure and permanent residency.

- Latest Windows build: **Succeeded, 0 errors**, [open player](../Builds/Story/TitanGate2177.exe). Keep its adjacent runtime/data files together.
- EditMode: **67/67 passed**, [result XML](../production/qa/evidence/story-2026-11-13/polish-tests.xml).
- Final story/action showcase: **23 checks passed**, no failed checks or runtime errors. Includes all eight action poses releasing control and both pet commands. [Runtime log](../production/qa/evidence/story-2026-11-13/story/autoshot-log.txt).
- Final ship route: **32 checks passed**, no failed checks or runtime errors. The player returns to walkable floor after the sofa/impact transition and reaches the playable Mars base with five supplies. [Runtime log](../production/qa/evidence/story-2026-11-13/ship/autoshot-log.txt).
- Final UX route at 1920×1080: **13 checks passed**, no failed checks or runtime errors. [Runtime log](../production/qa/evidence/story-2026-11-13/ux/autoshot-log.txt).
- Base route: **50 checks passed**, no failed checks or runtime errors. This run admits several visitors and ends in **FailedAir on day 4**, not a successful rescue. It ran before the last ship-only feeding/lifting visual refinements. The core/base rules were unchanged by those refinements. [Runtime log](../production/qa/evidence/story-2026-11-13/base/autoshot-log.txt).
- [51-second automated preview with actual game audio](../production/qa/evidence/story-2026-11-13/story-and-actions.mp4): pet menu, action poses, Earth/jump opening, mentor, manual release, pod separation and docking. About 12 captured frames per second, encoded to a 30 fps MP4; this is not a game-performance measurement. One coalesced screenshot request holds the preceding real frame. The video decodes without errors. [Capture metrics](../production/qa/evidence/story-2026-11-13/story-and-actions.json).
- [Actual seated sofa capture](../production/qa/evidence/story-2026-11-13/ship/s03b_seated_on_sofa.png), [watering during the tutorial](../production/qa/evidence/story-2026-11-13/ship/action_06_water.png), [Arun and cadet](../production/qa/evidence/story-2026-11-13/story/05_mentor.png), [Mu directional frames](../production/qa/evidence/story-2026-11-13/story/frames_mu.png). Other visitor sheets are alongside these.
- [Build hashes and exact scenario results](../production/qa/evidence/story-2026-11-13/verification.json). Whitespace diff check passed. No third-party dependencies were added.

The first showcase caught the one-frame action-completion race; an initial base run also caught the retained diary busy flag. Both were fixed and their failing runs are not counted as passing final verification. No hand-operated full playtest or complete performance profile is claimed.

## Practical limits

This delivers the approved opening as in-engine illustrated pixel scenes with subtitles and motion. It does not add voice acting, a new chapter to rescue Arun, a website, arbitrary sitting on every decorative surface or a professional redraw of every animation direction. The existing map navigation and quest rules remain authoritative. The new alien physiology, jump network and android repair core are fictional story elements, not real NASA findings.

No commit or push is performed by this implementation pass.
