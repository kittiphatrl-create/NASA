# LAST 72 HOURS — Art & Sound Slots (for the art team)

Put PNG files into **`Naja/Assets/Resources/Art/`** with the exact names below.
The game picks them up automatically on the next Play. No code change and no scene setup needed.
If a file is missing, the game keeps the gray box, so you can add art one piece at a time.

## Rules

- Format: **PNG with transparency**. Lower-case file names, exactly as listed.
- Unity imports PNGs as *Sprite* by default in this project. Leave it that way.
- Sizes below are the on-screen size at 1920×1080. Draw at **1× or 2×** that size and keep the same aspect ratio.
  Objects keep their aspect ratio (no stretching), so a slightly different ratio is fine.
- The object's name/status text stays as a small strip along the bottom edge of the object. Leave the bottom ~20% of the picture calm.
- Do **not** bake red/green "broken/fixed" colours into object art. The game tints the object with its status colour.
- The room background is drawn behind everything. The astronaut walks on the floor in the bottom ~20% of the room, so keep that area clear.

## Rooms (full background, 2.5D)

The game is now 2.5D (like Kathy Rain / Broken Sword): the room is a box seen from the front, the floor is visible,
and the astronaut walks anywhere on it and gets smaller further back. Draw each room as a **front view with perspective**:
back wall in the middle, side walls angled in, floor at the bottom. The graybox in `Naja/Captures/8x_*.png` shows exactly
where the floor, walls and doors are — trace over it. Size **1901 × 795**.

`room_quarters.png`, `room_command.png`, `room_lifesupport.png`, `room_engineering.png`,
`room_cabin.png`, `room_commsroom.png`, `room_crawlspace.png`, `room_partsstorage.png`

Keep in the picture: corridor doors on the side walls (left/right), the ▲ hatch area on the back wall, and for inner rooms
the ▼ BACK exit at the front edge of the floor. Inner rooms have a lower ceiling.

## Objects (drawn on top of the room, in front of / behind the astronaut)

Size = the front face on screen at 1920×1080. **Wall** objects hang on the back wall. **Floor** objects stand in the room;
draw them as a 3/4 view (front + a bit of the top), the player can walk behind them. **Decor** = props with no
interaction (they still block walking).

| Room | File | Kind | Size (px) |
|---|---|---|---|
| Quarters | `obj_bed.png` | Floor | 475 × 79 |
| Quarters | `obj_food.png` | Wall | 168 × 127 |
| Quarters | `obj_locker.png` | Wall | 155 × 162 |
| Quarters | `deco_table.png` | Decor | 241 × 62 |
| Quarters | `deco_stool.png` | Decor | 87 × 49 |
| Command | `obj_console.png` | Floor | 543 × 92 |
| Command | `obj_nav.png` | Wall | 259 × 114 |
| Command | `obj_security.png` | Wall | 259 × 114 |
| Command | `deco_chair.png` | Decor | 120 × 69 |
| LifeSupport | `obj_scrubber.png` | Floor | 513 × 190 |
| LifeSupport | `obj_emergency.png` | Wall | 262 × 92 |
| LifeSupport | `deco_tanks.png` | Decor | 144 × 143 |
| Engineering | `obj_pdu.png` | Wall | 211 × 138 |
| Engineering | `obj_cable.png` | Wall | 211 × 46 |
| Engineering | `obj_propline.png` | Floor | 736 × 56 |
| Engineering | `obj_jump.png` | Floor | 346 × 149 |
| Cabin | `obj_recorder.png` | Wall | 192 × 37 |
| Cabin | `obj_viewport.png` | Wall | 255 × 66 |
| Cabin | `obj_desk.png` | Floor | 445 × 79 |
| Cabin | `deco_sofa.png` | Decor | 495 × 82 |
| CommsRoom | `obj_comms.png` | Wall | 342 × 116 |
| CommsRoom | `obj_signal.png` | Floor | 365 × 98 |
| CommsRoom | `deco_chair2.png` | Decor | 118 × 62 |
| Crawlspace | `obj_maint.png` | Wall | 164 × 142 |
| Crawlspace | `obj_duct.png` | Wall | 310 × 47 |
| Crawlspace | `deco_pipes.png` | Decor | 376 × 42 |
| PartsStorage | `obj_storage.png` | Wall | 194 × 142 |
| PartsStorage | `obj_rack.png` | Floor | 452 × 157 |
| PartsStorage | `deco_crate1.png` | Decor | 201 × 69 |
| PartsStorage | `deco_crate2.png` | Decor | 140 × 58 |

## Close-up views (optional, shown when the player clicks an object)

`closeup_<id>.png`, for example `closeup_scrubber.png` or `closeup_desk.png` (same ids as the `obj_` files).
Shown in the left part of the close-up window at about **640 × 560**. Without a file the game shows a gray device.

## Doors

| File | Use | Size (px) |
|---|---|---|
| `door_hatch.png` | ▲ hatch on the back wall that leads into an inner room | about 160 × 145 |

Side-wall doors and the inner-room exit are drawn by the game (they are slanted); paint them into the room background.

## Inventory items (icons)

Square, about **128 × 128** (shown at ~55 px).

`item_multitool.png`, `item_accesscard.png`, `item_sparefilter.png`, `item_fuse.png`,
`item_repaircable.png`, `item_sealantkit.png`, `item_navchip.png`

## Astronaut

| File | Notes |
|---|---|
| `astronaut.png` | Facing **right**, feet touching the bottom edge. Shown about 160 px tall at the front of the room and about half that at the back wall. The game flips it when walking left, scales it with depth and adds a walk bob. One static frame is enough for now (a back view for walking into the room can come later). |

## Mouse cursors (optional)

The game draws these itself. Add a file only to replace the built-in shape.
Size **32 × 32**; the click point is the centre.

| File | Meaning |
|---|---|
| `cursor_look.png` | inspect an object |
| `cursor_use.png` | the selected item works here |
| `cursor_cantuse.png` | the selected item does not work here |
| `cursor_doorleft.png` / `cursor_doorright.png` | walk through a door |
| `cursor_locked.png` | locked door |

## Sounds

Sounds live in `Naja/Assets/Resources/Audio/` (Kenney CC0). To replace one, overwrite the file and keep the **same name**.
`.ogg` or `.wav` both work if the name (without extension) stays the same.

| Name | Plays when |
|---|---|
| `ambient` | engine hum loop, whole game |
| `ui_click`, `ui_select` | button click, item select |
| `pickup` | item received |
| `ui_error`, `warn` | wrong item / warning |
| `door_open`, `door_close`, `bump` | doors, walking into the locked door |
| `step` | footsteps |
| `impact` | micrometeoroid hit |
| `work`, `success`, `fail` | repair started / mini-game won / lost |
| `tick` | hint arrow appears after being stuck |
| `jump`, `landing` | Jump ending / Mars ending (`fail` for losing endings) |

The critical alarm is generated in code (no file).
