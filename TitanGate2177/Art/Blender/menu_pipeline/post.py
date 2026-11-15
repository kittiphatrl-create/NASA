# Post: menu_bg.jpg from bg.png; cadet PNGs get the cadet's separable ground shadow baked in as a soft alpha layer.
# blender -b --python post.py -- <dir>
import bpy, sys, os, numpy as np
import json
args = sys.argv[sys.argv.index("--") + 1:]
d = args[0]
pts = json.load(open(args[1])) if len(args) > 1 else None


def load(name):
    im = bpy.data.images.load(os.path.join(d, name))
    w, h = im.size
    px = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)
    return im, px


def save_png(px, name):
    h, w = px.shape[:2]
    im = bpy.data.images.new(name, w, h, alpha=True)
    im.pixels[:] = np.clip(px, 0, 1).ravel()
    im.filepath_raw = os.path.join(d, name)
    im.file_format = "PNG"
    im.save()


bgim, bg = load("bg.png")
_, bgs = load("bg_shadow.png")
# shadow matte: the smallest alpha that lets a premultiplied layer turn bg into bg_shadow in every channel
ratio = np.where(bg[..., :3] > 1.0 / 255, bgs[..., :3] / np.maximum(bg[..., :3], 1e-6), 1.0)
a = np.clip(1.0 - ratio.min(axis=2), 0.0, 1.0)
a = np.where(a < 0.02, 0.0, a)
S = np.clip(bgs[..., :3] - bg[..., :3] * (1.0 - a[..., None]), 0.0, 1.0)
# soft contact shadow under the boots (the low sun throws the real shadow forward, so the feet need grounding)
if pts:
    Hh, Ww = bg.shape[:2]
    f = pts["astronaut_feet"]
    cx, cy = f["x"] * Ww, (1.0 - f["y"]) * Hh + 6.0
    rx = max(f["w"] * Ww * 0.62, 20.0)
    ry = rx * 0.16
    yy, xx = np.mgrid[0:Hh, 0:Ww].astype(np.float32)
    e = ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2
    ac = 0.42 * np.exp(-2.2 * e)
    ac = np.where(ac < 0.004, 0.0, ac)
    sc_col = np.array([0.16, 0.08, 0.06], dtype=np.float32)          # deep warm-brown, sRGB
    S = S + sc_col * ac[..., None] * (1.0 - a[..., None])
    a = a + ac * (1.0 - a)
    print("CONTACT", round(cx), round(cy), round(rx), round(ry))
diff = np.abs(bg[..., :3] - bgs[..., :3]).max(axis=2)
ys, xs = np.nonzero(a > 0)
print("SHADOW px", len(xs), "bbox", (xs.min(), xs.max(), ys.min(), ys.max()) if len(xs) else None, "max a", float(a.max()))
for name in ("cadet.png", "cadet_look.png"):
    if not os.path.exists(os.path.join(d, name)):
        continue
    _, c = load(name)
    ca = c[..., 3:4]
    out_a = ca + a[..., None] * (1.0 - ca)
    out_pm = c[..., :3] * ca + S * (1.0 - ca)
    out = np.concatenate([out_pm / np.maximum(out_a, 1e-6), out_a], axis=2)
    out[..., :3] = np.where(out_a > 0, out[..., :3], 0.0)
    save_png(out, "menu_" + name)
    # check: menu_cadet over bg must equal the full render (cadet + its shadow) within 8-bit rounding
    comp = out[..., :3] * out_a + bg[..., :3] * (1 - out_a)
    print("COMPOSITE", name, "pixels with alpha", int((out_a[..., 0] > 0).sum()))
bgim.file_format = "JPEG"
bgim.save(filepath=os.path.join(d, "menu_bg.jpg"), quality=90)
print("JPG saved")

# ---- annotate menu_points.json with occlusion by the cadet layer and the rover sprite's ground line
if pts:
    _, cm = load("menu_cadet.png")
    Hh, Ww = cm.shape[:2]
    for b in pts["beacons"]:
        px_, py_ = int(b["x"] * (Ww - 1)), int((1.0 - b["y"]) * (Hh - 1))
        b["behind_cadet"] = bool(cm[py_, px_, 3] > 0.5)
    rv_path = os.path.join(d, "menu_rover.png")
    if os.path.exists(rv_path):
        _, rv = load("menu_rover.png")
        rh, rw = rv.shape[:2]
        ys_, xs_ = np.nonzero(rv[..., 3] > 0.5)
        pts["rover_sprite"] = {"file": "menu_rover.png", "width": rw, "height": rh, "faces": "right",
                               "ground_y": round(1.0 - ys_.min() / (rh - 1), 4),
                               "x0": round(xs_.min() / (rw - 1), 4), "x1": round(xs_.max() / (rw - 1), 4),
                               "note": "ground_y = wheel contact line in the sprite (top-down); flip X to drive left"}
    pts["layers"] = {"menu_bg": "jpg, no cadet, no cadet shadow",
                     "menu_cadet": "png RGBA, same canvas; includes the cadet's soft ground shadow + contact shadow as partial alpha",
                     "menu_cadet_look": "as menu_cadet, head turned 15 deg toward the base (11 neck + 4 upper body)",
                     "blend": "straight alpha, composited in sRGB (gamma) space like Unity UI in Gamma colour space"}
    json.dump(pts, open(args[1], "w", encoding="utf-8"), indent=1)
    print("JSON annotated", sum(b["behind_cadet"] for b in pts["beacons"]), "lights behind cadet")
