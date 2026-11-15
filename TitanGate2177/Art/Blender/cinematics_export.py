# Copy the graded cutscene stills into the game as JPG bytes (system Python with Pillow).
#   python Art/Blender/cinematics_export.py
import glob, os
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "Art", "Blender", "renders", "cin", "final")
DST = os.path.join(ROOT, "Assets", "Resources", "Sprites", "KeyArt")

for p in sorted(glob.glob(os.path.join(SRC, "c*.png"))):
    name = os.path.splitext(os.path.basename(p))[0]
    im = Image.open(p).convert("RGB")
    out = os.path.join(DST, name + ".jpg.bytes")
    im.save(out, "JPEG", quality=88, optimize=True, progressive=False)
    print(name, im.size, os.path.getsize(out) // 1024, "KB")
