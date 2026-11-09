# LAST 72 HOURS — UI Layout Reference

Layout decisions for the graybox, based on patterns from shipped games in the same genres
(point-and-click adventure, side-view survival, spaceship management). We borrow *conventions*,
not art or content.

## Screen map (1920×1080)

```
┌──────────┬───────────┬──────────────┬────────────┬──────────────────────┐
│ SHIP MAP │ O2 / PWR  │  COUNTDOWN   │ HUNGER     │ OBJECTIVES HANDBOOK  │  top bar 10%
│ QTRS CMD │ PROPELLANT│   72:00:00   │ FATIGUE    │ LOG                  │
├──────────┴───────────┴──────────────┴────────────┴──────────────────────┤
│ NEXT ▸ what to do next …                                   ROOM NAME     │  tracker 4%
├─────────────────────────────────────────────────────────────────────────┤
│ ◀ door      [banner]                               [toasts: last 3]  ▶  │
│             objects / hotspots on the back wall                  door   │  room 74%
│ ── floor ───────── astronaut walks here ─────────────────────────────── │
│                    hover line: "Use Fuse with PDU"                        │
├─────────────────────────────────────────────────────────────────────────┤
│ INVENTORY [icon][icon][icon]…            SELECTED: Fuse  + hint          │  inventory 11%
└─────────────────────────────────────────────────────────────────────────┘
```

## What we took, and from where

| Pattern | Seen in | Why it fits us |
|---|---|---|
| Ship status top-left, traffic-light colours (green / orange / red) | FTL: Faster Than Light | Instant read of O2 / Power / Propellant at a glance |
| Clickable ship diagram showing which room is broken | FTL | Fast travel + "where is the problem" in one widget |
| Big clock as the centre of the HUD | 60 Seconds!, Papers, Please | Our core mechanic is time; it must be the loudest element |
| Survival needs always visible, compact | The Long Dark, Sheltered | Hunger/Fatigue affect work speed, player must see them |
| Side-view room, exits at the left/right edge | This War of Mine, Sheltered, Broken Sword | Walking to a door feels natural; doors stop overlapping objects |
| Inventory bar always visible at the bottom | Broken Sword, Sierra-style games | Only 7 items; hiding it (hover bars) hurts discoverability for first-time judges |
| Item follows the cursor when selected | Classic LucasArts / Sierra adventures | Clear "I'm holding X" state |
| Hover "sentence line" (Use X with Y) | Thimbleweed Park / SCUMM games | Tells the player what a click will do before it costs time |
| "NEXT ▸" objective tracker | Thimbleweed Park to-do list, modern adventures | Nobody gets stuck during a 5-minute demo |
| Alerts as short toasts + full LOG on demand | Most management sims | Frees the right 25% of the screen for the room |

## Rules

- The room is the hero: ≥ 70% of the screen.
- Nothing that floats over the room (banner, toasts, prompt, cursor item) may block clicks.
- Free actions never ask for confirmation; actions that cost time always show a confirm with the time cost.
- Colour meaning is fixed: green OK, orange warning, red broken or locked, blue current or selected.

## Interaction polish (beta)

| Feature | Borrowed from | Behaviour |
|---|---|---|
| Smart cursor | Classic point-and-click "smart cursor" (LucasArts / Sierra / modern adventures) | Magnifier = inspect, green crosshair = selected item works here, red "no" sign (circle with slash) = it does not, arrow = door, padlock = locked door |
| Double-click = arrive instantly | Modern point-and-click games (skip the walk) | Double-click an object, door or floor to skip walking. Walking is free either way |
| Esc chain | Most adventure games | Esc closes the open window → drops the held item → opens the pause menu |
| Pause menu | Standard | Resume, volume −/+, mute, hint arrow on/off, controls, restart (with confirm), quit |
| Hint arrow | Objective markers + "stuck" hint systems | Bouncing ▼ over the NEXT target. Always on until the locker is opened (tutorial); after that it appears only when the NEXT hint has not changed for 20 s. Points at the door towards another room when the target is elsewhere |

## Depth: inner rooms, close-ups, object markers

Reference board: `ref_depth_rooms.jpg` (official Steam screenshots and YouTube gameplay frames, for reference only, not for the game).

| Feature | Borrowed from | Behaviour |
|---|---|---|
| Inner rooms behind back-wall hatches | Thimbleweed Park back-wall doors, Rusty Lake Hotel "KITCHEN" door, Gemini Rue side rooms | Each main room has a teal ▲ hatch: Captain's Cabin (Quarters), Comms Room (Command), Service Crawlspace (Life Support), Parts Storage (Engineering). Inner rooms are smaller: low ceiling, pipes, fewer wall panels. Way out = ▼ BACK door on the left. Entering is free, like every room change. W/↑ or S/↓ or E uses the door |
| Content moved inward | — | Voice Recorder → Cabin, Comms Panel → Comms Room, Maintenance Cabinet → Crawlspace, Storage Cabinet → Parts Storage. Same puzzles and time costs; NEXT hints name the inner room |
| New free lore objects | Environmental storytelling in all of the above | Captain's Desk (strategy note), Viewport, Signal Log (Jump window / ARES-7), Air Duct, Spare Parts Rack. Inspect only, no time cost |
| Close-up view | Rusty Lake Hotel zoom-in, Machinarium machine close-ups | Clicking an object zooms the window out of the object (0.16 s). Left: the device up close (status screen FAULT / OK / NEEDS item, lights, item slot) or art `closeup_<id>.png`. Right: text + actions. ▼ BACK at the bottom centre |
| ? / ! / ✓ markers over objects | This War of Mine interaction icons | ? = not looked at yet, ! = broken or needs an item, ✓ = done |
| Hint arrow through inner rooms | — | Points at the hatch when the target is inside, or at ▼ BACK when the player is inside the wrong inner room |
| Ship map | FTL | Highlights the main room while the player is in one of its inner rooms |

Lockdown: if the player is in Parts Storage when Engineering locks, they are moved to the Command Deck (same as Engineering).

## 2.5D rooms (walk anywhere)

Reference board: `ref_walk_depth.jpg` (Broken Sword 5 near/far frames from YouTube + Steam screenshots of Kathy Rain,
Grim Fandango, Technobabylon, Deponia, Unavowed; reference only).

| Feature | Borrowed from | Behaviour |
|---|---|---|
| Walkable floor in perspective | Broken Sword 5, Kathy Rain, Grim Fandango | Each room is a box seen from the front. Floor = trapezoid; the astronaut walks anywhere with WASD / arrows (8 directions) or by clicking the floor |
| Depth scaling | same | Closer to the screen = bigger (×1.25 at the front edge), smaller at the back (×0.52–0.72 depending on the room). Back view (no visor) while walking into the room |
| Furniture you walk around | Kathy Rain, Deponia | Floor objects and props block walking. Click-to-walk uses grid A* around them. Draw order is sorted by depth, so the astronaut can stand behind a desk or in front of it |
| Walk into doors | classic adventure exits | Push A into the left-wall door, D into the right-wall door, W into a ▲ hatch, S out of an inner room (▼ BACK at the front edge). E still works. Locked Engineering door = bump |
| Room depth per room | — | Deep: Quarters, Command, Engineering. Shallow corridor: Life Support. Inner rooms: deep but narrow with a low ceiling; Crawlspace is the most cramped |
| Interactive vs decor | — | Interactive objects keep ?/!/✓ markers, smart cursor and close-ups. Decor (sofa, table, crates, tanks, pipes, seats) only blocks walking |

Code: `Assets/Scripts/UI/Stage25D.cs` (room shapes, object layout table, quad mesh, A* path), `Astronaut.cs` (2D walk),
`GameUI.cs` (draws the room, depth sort, door triggers). To move an object, edit its line in `Stage25D.Init()`:
`S(id, kind, x, depth, width, height, footprintDepth, lift)` — all in floor units.

Art and sound replacement: see `ART_SLOTS.md`.

## Repair mini-games

Every time-costing repair runs a 5–20 second mini-game. **Success** keeps the normal time cost from the
handoff table. **Fail** costs +30 minutes, then you can retry. **Perfect** gives a small resource bonus.

| Repair | Mini-game | Borrowed from |
|---|---|---|
| Oxygen Scrubber | Skill-check bar: stop the needle in the green zone twice | Dead by Daylight skill checks, Among Us "Calibrate Distributor" |
| Power Distribution / Cable Junction | Connect wires by colour (4 / 3 wires) | Among Us "Fix Wiring" |
| Seal Propellant Leak | Click leaks before they spray | Whack-a-mole reflex tasks |
| Calibrate Navigation | Tune FREQ / AMP to overlay the target signal | Among Us "Align Engine Output" style tuning |
| Stabilize Jump Drive | Repeat the flashing sequence (5 steps) | Simon memory game |
| Security Override | 4-digit code hidden in engineer Mali's voice log (4719) | Classic adventure "find the code" puzzle |

## Sound

Kenney CC0 packs ("Sci-Fi Sounds" and "Interface Sounds"), downloaded from the OpenGameArt mirror and
stored in `Assets/Resources/Audio` (license in `KENNEY_CC0_LICENSE.txt`). The alarm is generated in code.
Hooks: button click, item select, pickup, wrong item, door open/close, footsteps, lock bump,
critical alarm, meteor impact, repair work, success/fail, ambient engine loop, and per-ending stingers.
