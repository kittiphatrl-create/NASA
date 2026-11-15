#!/bin/sh
# Copies the layered MARS menu scene into Unity as TextAsset bytes (+ plain JSON manifest). Mars only.
S="$(cd "$(dirname "$0")" && pwd)"
M="$S/../renders/menu/scenes"
K="$S/../../../Assets/Resources/Sprites/KeyArt"
for f in menu_mars_sky.jpg menu_mars_mountains.png menu_mars_dustdevil.png menu_mars_base.png menu_mars_door.png menu_mars_flag.png \
         menu_mars_cadet.png menu_mars_fg.png; do
  [ -f "$M/$f" ] && cp -f "$M/$f" "$K/$f.bytes"
done
cp -f "$M/menu_scene_mars.json" "$K/menu_scene_mars.json"
echo "copied mars scene to $K"
