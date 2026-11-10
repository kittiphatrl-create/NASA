# TITAN GATE 2177 — Junior Astronaut Mission Trainer

Engine: Unity 6000.3.25f1 · Goal: jam (NASA activity submission) *(inferred (strong))*

## In your words

> คือตอนนี้กำลังคิดเกมส่งกิจกรรม NASA โดยมีโจทย์คือ Build a Junior Astronaut Mission Trainer
> อยากได้เกมแนว 2.5D ภาพ pixel puzzle ทำภารกิจ และสามารถให้ความรู้เกี่ยวกับอวกาศได้
>
> References: Don't Escape: 4 Days to Survive (take: point-and-click, inventory, cross-room
> puzzles, action-based time, preparing before a big event · avoid: guess-only puzzles, large
> maps) · 60 Parsecs! (take: hunger, fatigue, rationing, forced decisions under scarcity ·
> avoid: luck-driven randomness, managing a big crew) · Oxygen Not Included (take: links between
> oxygen, water, power, food and life-support efficiency · avoid: gas/temperature/pipe
> simulation, detailed construction). Unity 6000.3.25f1.

1. Project: new project in its own folder under E:\!for WORK\UNITY · "full" = verify every
   system; don't re-verify a system that is fine unless it or a related system changed.
2. Setting: year 2177, one astronaut + one advising AI, en route to a warp gate at a moon of
   Saturn. Scene 1: the ship hits space debris, the fuel room is damaged, must land. Scene 2:
   landed, survive on Mars.
3. Resources: cut some so the game is balanced and not too hard.
4. Events: random allowed, but preventable or only minor damage — except a few with heavy damage.
5. Knowledge: knowledge cards are puzzle clues; no quiz.
6. Look: point and click, but the character is seen performing every action.
7. Fail: restart the whole game · bilingual TH/EN, fonts must not break, Thai vowels/tone marks
   must not sink; both readable and on-theme.
8. Scope: short game playable to the end, plus a plan for further development.

## Hypothesis

If the player must use real NASA knowledge cards as clues to solve puzzles and plan life
support, they will feel *they figured it out because they know real science* — we know it is
true if testers solve each main puzzle with at most one hint and can retell 3+ facts after play.
*(inferred (strong))*

## Look and references

2.5D side cross-section ("dollhouse"): pixel-art sprites (large crisp pixels) in a 3D scene,
fixed perspective camera one room at a time, multi-layer parallax backdrops (stars and Saturn in
scene 1; Mars ridges and dust in scene 2). Palette: deep navy = ship interior, cyan = screens /
AI / hotspots, rust orange = Mars, amber = warning, red = critical. Signature detail: the
astronaut walks to each clicked spot and plays the action (repair, type, eat, sleep, drill),
while the AI drone ORBI floats beside them with speech bubbles. Every figure drawn as pixel art,
no emoji, no plain circles. Reference games give mechanics only; none of their art or assets.

## First minute

Cockpit of the ship *Kestrel* → ORBI greets and teaches clicking (Space lights all hotspots) →
first routine task (star scan) gives the first knowledge card → the shift bar ticks down →
alarm → **debris impact**: screen shake, Fuel Bay turns red → the player sees the first lock
(a hull breach) before finding what seals it. *(guessed)*

## Challenge

Scene 1 is pure puzzle, no death (seal the breach → read the fuel gauge and pick a reachable
destination → order the Mars landing (EDL) steps from the manual; a wrong order gets ORBI's
explanation and a retry). Scene 2: 4 sols on Mars, 6 actions a sol; first real trouble on sol 2
(an event ORBI forecasts a sol ahead). Any vital resource at zero = loss, restart the whole game
(you said). Big global dust storm on the night of sol 3 is the event to prepare for. *(guessed)*

## Features

- [ ] Scene 1 ship: 3 rooms (Cockpit, Fuel Bay, Cargo), 3 cross-room puzzles
- [ ] Scene 2 Mars: 4 locations (Hab, Life Support, Fuel Bay, Surface), 4 sols
- [ ] 4 resources: O₂, Water, Power, Food + astronaut Stamina (hunger+fatigue merged) *(inferred (strong))*
- [ ] Links: MOXIE CO₂→O₂ (power) · water recycler (power) · solar panels (dust lowers output, clean them) · ice drilling → water · Sabatier → methane fuel · power at zero drains O₂
- [ ] Goal scene 2: fuel to 100 + fuel line repaired → launch → ending toward Titan Gate
- [ ] Action-based time; low stamina makes actions cost double; sleeping ends the sol
- [ ] Random events from a fixed deck, each forecast and preventable (dust storm, micro-leak, solar flare); one heavy event (global dust storm)
- [ ] Inventory bar, use items on the scene and combine items
- [ ] ~14 knowledge cards, real NASA facts, used as clues + a codex
- [ ] ORBI hint button: small nudge then bigger, never the answer outright
- [ ] TH/EN toggle any time · UI Toolkit Advanced Text (HarfBuzz shaping) · Chakra Petch (OFL)
- [ ] All tuning in `Assets/Resources/Data/config.json` · EditMode unit tests for survival sim, events, puzzles

## Your systems

- Point-and-click + inventory + cross-room puzzles — in the slice
- Action-based time — in the slice
- Linked life support (4 resources + stamina) — in the slice
- Random-but-preventable events — in the slice
- Knowledge cards as clues — in the slice
- AI helper ORBI + hints — in the slice
- Bilingual — in the slice
- Sound — thin stand-in (short synthesized SFX, no music) *(guessed)*

## Out of scope

- Scene 3 through the Titan warp gate: grow it
- Greenhouse (potatoes): add it as a feature
- Save/Load: add it as a feature
- Difficulty levels: change it after play
- Music: add it as a feature

## Core loop

- Read resources and ORBI's forecast → choose actions (make O₂, recycle water, clean panels, drill ice, make fuel, repair, eat) → linked values change
- Prepare for the forecast event → sleep (ends sol) → event resolves: prevented or damaged
- Fuel full + line repaired → launch = win · a vital at zero or the launch window missed = restart

## Build order

1. The slice: scene 1 → scene 2 → ending, start to finish.
2. Roadmap: `docs/roadmap.md`.

## Intent

*staged failure* (scene 1 cannot be lost; scene 2 restarts the game) — *you said*

## Change log

- **2026-11-10 · after first playtest (owner):** doors overlapped props; "every destination should lead
  somewhere different" (chose: separate full chapters); "room layout changed between chapters" (chose:
  one ship); "not pressured enough" (chose: events happening, real-time O₂ leak timer, danger you can
  see and hear, chapter 1 results carry over); "art still looks AI-made, fix later".
  Built: one 5-room ship for both chapters · three Chapter 2 routes (Mars / Moon / Titan) with their own
  machines, puzzles, events, landing procedure and ending · real-time cabin O₂ leak + optional supply bag
  carried into Chapter 2 · two real-time daytime emergencies per route · flashing low bars, siren and
  heartbeat, major-event countdown · 28 knowledge cards.

- **2026-11-10 · round 3 (owner):** asked for cutscenes, less annoying UX/UI sound, a good title image and a
  main menu with the key buttons. Built: 8 in-engine pixel cutscenes (intro, impact, landing ×3, ending ×3,
  skippable) · animated title key art (`docs/key-art.png`) · main menu (new, continue from landing checkpoint,
  card book kept between runs, how to play, settings, credits, quit) · pause menu (Esc) · settings (3 volumes,
  text size, dialogue speed, full screen, language) · soft synthesized UI sounds, typewriter dialogue, ship hum.

- **2026-11-10 · round 4 (owner):** chose the mission debrief and astronaut radiation/health; "the game ends in
  about 5 minutes". Built: Chapter 2 now lasts 7 days (big event on night 5, minor events re-drawn, a third
  daytime emergency on day 5, progress 14 per action) · radiation dose in mSv with real per-day values
  (Mars 0.7, Moon 1.4, Titan ~0), flare dose, EVA dose, a 150 mSv mission limit and a Moon regolith berm that
  halves it · 3 Mission Control science tasks per route (days 1/3/5, each with a card) · Veggie garden for
  food · 12 new cards (40 total) · end-of-mission debrief: 3 stars (complete, healthy, all science), stats,
  "what to remember" lessons for every missed night/emergency, best stars per route on the menu.
  Balance: `tools/balance_sim.py` and the EditMode policy test — a sensible player wins every route at the
  worst carry-over with every emergency missed; ignoring forecasts loses. Also fixed: end screens were dimmed
  under the fader and pushed left by the menu layout.

- **2026-11-10 · round 5 (owner: "do them all"):** morning dilemmas (3 per route: ORBI rest, plus two
  route trade-offs such as burning methane for power at the cost of oxygen), shown in the debrief; balance
  holds for either answer · how-to pages 9–11 (radiation, science, choices) · art consistency pass in code:
  one light direction on every prop (rim light/shadow), hand-placed texture instead of random noise, ground
  tiled at prop pixel size, smooth seamless skylines with rock strata, no skyline/ground edge on screen ·
  educator guide in English and Thai (`docs/educator-guide*.md`). Hand-drawn sprites stay on the roadmap.

- **2026-11-10 · round 6 (owner):** less text: cards now unlock with a toast and are read in the card book
  (only the first card of a session opens by itself), action confirmations and pickups are toasts, the
  morning forecast is one line (the HUD panel has the rest), science requests are toasts · random events
  like 60 Seconds: a deck of 8 per route (7 shared + 1 route card) drawn on free mornings, some options
  need earlier preparation (spare filter) · 4 new cards (44) · difficulty: more oxygen used at night,
  harsher unprevented Moon events, progress 15 per action; a sensible player wins 638/640 simulated runs,
  ignoring forecasts almost always loses · art: characters lit like the props, room lights dim as power
  runs low.

- **2026-11-10 · round 7 (owner chose "60-second scramble" for Chapter 1):** after the landing steps the
  cadet has 60 seconds to grab loose supplies thrown around by the impact; the landing lockers hold 5 of 9.
  Air, water, food, power, a power cell and a family photo give start bonuses; the first aid kit, spare
  filter and seed packets unlock better answers in Chapter 2 events. The old supply bag is gone; the
  checkpoint saves what was stowed.

## Keep
