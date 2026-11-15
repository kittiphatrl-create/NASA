#!/bin/sh
# Copy the live 3D menu exports (Art/Blender/menu_live.py) into Resources as TextAssets the menu loads at run time.
set -e
SRC="$(dirname "$0")/../renders/menu/live"
DST="$(dirname "$0")/../../../Assets/Resources/MenuLive"
mkdir -p "$DST"
for f in "$SRC"/*.mlm "$SRC"/*.jpg; do cp "$f" "$DST/$(basename "$f").bytes"; done
cp "$SRC/menu_live.json" "$DST/menu_live.json"
