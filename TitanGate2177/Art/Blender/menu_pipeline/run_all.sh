#!/bin/sh
# Re-renders the LAST DOCK main-menu key art from Art/Blender/mars_keyart.blend (about 65 s).
# setup_menu.py builds Art/Blender/mars_keyart_menu.blend from the original file (the original is never saved).
# ring.py adds the docking-ring menu art (menu_ring.png, menu_clamp.png, menu_ring.json).
# Then copy the results from Art/Blender/renders/menu/ into Assets/Resources/Sprites/KeyArt/ as *.jpg.bytes / *.png.bytes.
set -e
B="${BLENDER:-/c/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe}"
S="$(cd "$(dirname "$0")" && pwd)"
A="$(cd "$S/.." && pwd)"
M="$A/renders/menu"
"$B" -b "$A/mars_keyart.blend" --python "$S/setup_menu.py" 2>&1 | grep -iE "error|traceback|SAVED" || true
"$B" -b "$A/mars_keyart_menu.blend" --python "$S/extras.py" 2>&1 | grep -iE "error|traceback|EXTRAS TIMES|LOGO alpha" || true
"$B" -b "$A/mars_keyart_menu.blend" --python "$S/ring.py" 2>&1 | grep -iE "error|traceback|RING TIMES|CLAMP" || true
"$B" -b "$A/mars_keyart_menu.blend" --python "$S/points.py" -- "$M/menu_points.json" 2>&1 | grep -iE "error|traceback|WROTE" || true
"$B" -b "$A/mars_keyart_menu.blend" --python "$S/render_menu.py" -- bg bg_shadow cadet cadet_look full 2>&1 | grep -iE "error|traceback|TIMES" || true
"$B" -b --python "$S/post.py" -- "$M" "$M/menu_points.json" 2>&1 | grep -iE "error|traceback|JPG|JSON|CONTACT" || true
# Layered menu background scenes (renders/menu/scenes/): Mars blue sunset (save exists) and the Kestrel cabin (no save).
MENU_VARIANT=sunset "$B" -b "$A/mars_keyart.blend" --python "$S/setup_menu.py" 2>&1 | grep -iE "error|traceback|SAVED" || true
"$B" -b "$A/mars_keyart_sunset.blend" --python "$S/layers_mars.py" -- only=layers 2>&1 | grep -iE "error|traceback|LAYER|MARS TIMES" || true
"$B" -b "$A/mars_keyart_sunset.blend" --python "$S/layers_mars.py" -- only=cadet 2>&1 | grep -iE "error|traceback|CADET RECT|LOOP|MARS TIMES" || true
"$B" -b "$A/mars_keyart_menu.blend" --python "$S/ship_setup.py" 2>&1 | grep -iE "error|traceback|SAVED" || true
"$B" -b "$A/menu_ship.blend" --python "$S/layers_ship.py" -- only=layers 2>&1 | grep -iE "error|traceback|CABIN|SHIP LAYERS" || true
"$B" -b "$A/menu_ship.blend" --python "$S/layers_ship.py" -- only=cadet 2>&1 | grep -iE "error|traceback|CADET RECT|LOOP|SHIP TIMES" || true
sh "$S/copy_scenes.sh"
