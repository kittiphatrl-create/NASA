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
"$B" -b "$A/mars_keyart_sunset.blend" --python "$S/layers_mars.py" -- only=prep 2>&1 | grep -iE "error|traceback" || true   # adds the foreground rocks
"$B" -b "$A/mars_keyart_menu.blend" --python "$S/ship_setup.py" 2>&1 | grep -iE "error|traceback|SAVED" || true
# v2 premium clay, Cycles (OptiX GPU if present): mars_keyart_sunset_v2.blend -> renders/menu/scenes/ (the no-save menu uses the space video, so the ship scene is not re-rendered)
"$B" -b "$A/mars_keyart_sunset.blend" --python "$S/mars_v2.py" 2>&1 | grep -iE "error|traceback|SAVED" || true
"$B" -b "$A/mars_keyart_sunset_v2.blend" --python "$S/mars_v2_anim.py" 2>&1 | grep -iE "error|traceback|FLAG loop|SAVED" || true   # airlock door, flag, dust devil
"$B" -b "$A/mars_keyart_sunset_v2.blend" --python "$S/layers_mars_v2.py" -- s_c=192 rect=982,270,1468,999 2>&1 | grep -iE "error|traceback|LAYER|CADET RECT|LOOP|TIMES" || true
"$B" -b "$A/mars_keyart_sunset_v2.blend" --python "$S/loops_mars_v2.py" 2>&1 | grep -iE "error|traceback|LOOP|DOOR|TIMES" || true
"$B" -b --python "$S/png9.py" -- "$M"/scenes/menu_mars_*.png 2>&1 | grep -E "PNG9" || true   # lossless zlib-9
sh "$S/copy_mars.sh"
