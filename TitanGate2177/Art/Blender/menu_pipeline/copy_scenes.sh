#!/bin/sh
# Copies the layered menu scenes into Unity as TextAsset bytes (+ plain JSON manifests).
S="$(cd "$(dirname "$0")" && pwd)"
M="$S/../renders/menu/scenes"
K="$S/../../../Assets/Resources/Sprites/KeyArt"
for f in menu_mars_sky.jpg menu_mars_mountains.png menu_mars_base.png menu_mars_cadet.png menu_mars_fg.png          menu_ship_space.jpg menu_ship_planet.png menu_ship_cabin.png menu_ship_cadet.png menu_ship_fg.png; do
  cp -f "$M/$f" "$K/$f.bytes"
done
cp -f "$M/menu_scene_mars.json" "$K/menu_scene_mars.json"
cp -f "$M/menu_scene_ship.json" "$K/menu_scene_ship.json"
echo "copied scenes to $K"
