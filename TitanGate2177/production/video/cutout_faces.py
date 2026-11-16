"""Removes the background from each member photo and crops head + shoulders.

python production/video/cutout_faces.py   (needs rembg, scipy, Pillow)
faces/<key>.webp  ->  faces/<key>_cut.png (transparent, square-ish, person centred)
Only the largest foreground region is kept, so name labels drawn on the photos are dropped.
"""
import pathlib
import numpy as np
from PIL import Image
from rembg import remove, new_session
from scipy import ndimage

HERE = pathlib.Path(__file__).resolve().parent / "faces"
session = new_session("birefnet-portrait")

for src in sorted(HERE.glob("*.webp")):
    im = Image.open(src).convert("RGB")
    cut = remove(im, session=session, post_process_mask=True)
    a = np.array(cut.getchannel("A"))
    lab, n = ndimage.label(a > 40)
    if n > 1:
        sizes = ndimage.sum(np.ones_like(a), lab, range(1, n + 1))
        keep = 1 + int(np.argmax(sizes))
        a = np.where(lab == keep, a, 0).astype(np.uint8)
        cut.putalpha(Image.fromarray(a))
    ys, xs = np.nonzero(a > 40)
    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    w = x1 - x0
    h = min(y1 - y0, int(w * 1.15))
    pad = int(w * 0.06)
    box = (max(0, x0 - pad), max(0, y0 - pad), min(im.width, x1 + pad), min(im.height, y0 + h))
    out = cut.crop(box)
    out.save(HERE / f"{src.stem}_cut.png")
    print(src.stem, out.size)
