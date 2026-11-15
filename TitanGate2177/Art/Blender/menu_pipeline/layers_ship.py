# SHIP menu scene (no save): layered renders + cadet idle sprite sheet + manifest.
# blender -b Art/Blender/menu_ship.blend --python Art/Blender/menu_pipeline/layers_ship.py [-- only=layers|cadet]
import bpy, sys, os, math, json, time
import numpy as np
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import scenekit as K
import idle

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
opts = dict(a.split("=", 1) for a in argv if "=" in a)
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
OUT = os.path.join(ROOT, "Art", "Blender", "renders", "menu", "scenes")
WORK = os.path.join(OUT, "_work_ship")
os.makedirs(WORK, exist_ok=True)
D = bpy.data
assert D.filepath.lower().endswith("menu_ship.blend"), D.filepath
sc = D.scenes["Scene"]
ps = D.scenes["Planet"]
cam = sc.camera
W, H, SW, SH = 2560, 1440, 1920, 1080
times = {}
for _n in idle.RIG:
    D.objects[_n].animation_data_clear()
sc.frame_set(1)
CADET = list(D.collections["cadet"].all_objects)
FG = list(D.collections["ship_fg"].all_objects)
CABIN = [o for o in sc.objects if o.type == "MESH" and o not in CADET and o not in FG]
only = opts.get("only")


def setup(layer):
    sc.render.film_transparent = True
    for o in CABIN:
        o.visible_camera, o.is_holdout, o.visible_shadow = True, layer in ("cadet", "foreground"), True
    for o in CADET:
        o.visible_camera, o.is_holdout = layer == "cadet", layer == "foreground"
        o.visible_shadow = True
    for o in FG:
        o.visible_camera, o.is_holdout, o.visible_shadow = layer == "foreground", False, False


def scr(p):
    v = world_to_camera_view(sc, cam, Vector(p))
    return round(v.x, 4), round(1 - v.y, 4)


WX, WY, WZ, RW = sc["ship_window"]
wcx, wcy = scr((WX, WY, WZ))
ex, ey = scr((WX + RW, WY, WZ))
ex2, ey2 = scr((WX, WY, WZ + RW))
w_r = (math.hypot((ex - wcx) * W, (ey - wcy) * H) + math.hypot((ex2 - wcx) * W, (ey2 - wcy) * H)) / 2 / H
layers = []
if only in (None, "layers"):
    # ---------------------------------------------------------- 1 space + stars (procedural, deterministic)
    rng = np.random.default_rng(2177)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    g = (xx / W) * 0.6 + (yy / H) * 0.4
    space = np.zeros((H, W, 3), np.float32)
    space[:] = np.array([0.020, 0.024, 0.045])
    space += g[..., None] * np.array([0.012, 0.012, 0.030])
    band = np.exp(-(((yy / H - 0.35) - (xx / W - 0.5) * 0.55) / 0.16) ** 2)        # faint milky band
    space += (band * K.gblur(rng.random((H // 8, W // 8)).astype(np.float32), 2.0).repeat(8, 0).repeat(8, 1)[:H, :W] * 0.035)[..., None] * np.array([0.8, 0.85, 1.0])
    stars = np.zeros((H, W), np.float32)
    tints = np.zeros((H, W, 3), np.float32)
    for n, lo, hi, sz in ((2600, 0.10, 0.45, 0), (260, 0.35, 0.8, 1), (24, 0.8, 1.0, 2)):
        xs = rng.uniform(0, W, n); ys_ = rng.uniform(0, H, n); br = rng.uniform(lo, hi, n)
        tint = rng.choice(np.array([[1, 1, 1], [0.82, 0.9, 1.0], [1.0, 0.92, 0.8]], np.float32), n)
        for x, y, b, t in zip(xs, ys_, br, tint):
            xi, yi = int(x), int(y)
            r = 1 + sz
            x0, x1, y0, y1 = max(xi - r, 0), min(xi + r + 1, W), max(yi - r, 0), min(yi + r + 1, H)
            d2 = (xx[y0:y1, x0:x1] - x) ** 2 + (yy[y0:y1, x0:x1] - y) ** 2
            s = b * np.exp(-d2 / (2 * (0.45 + 0.35 * sz) ** 2))
            stars[y0:y1, x0:x1] = np.maximum(stars[y0:y1, x0:x1], s)
            tints[y0:y1, x0:x1] = np.where(s[..., None] >= stars[y0:y1, x0:x1][..., None] - 1e-6, t, tints[y0:y1, x0:x1])
    space = space * (1 - stars[..., None]) + tints * stars[..., None]
    K.save(space, os.path.join(OUT, "menu_ship_space.jpg"), "JPEG", 92)
    layers.append({"name": "space", "file": "menu_ship_space.jpg", "depth": 0.0, "rect": [0, 0, 1, 1]})
    # ---------------------------------------------------------- 2 planet (Mars from orbit) + thin blue limb halo
    t0 = time.time()
    K.render(ps, os.path.join(WORK, "planet.png"), W, H)
    times["planet"] = round(time.time() - t0, 1)
    pl = K.load(os.path.join(WORK, "planet.png"))
    PX, PY, RP = ps["planet"]
    Lp = Vector(sc["planet_light"])
    d = np.hypot(xx + 0.5 - PX, yy + 0.5 - PY)
    ang_lit = ((xx + 0.5 - PX) * Lp.x + -(yy + 0.5 - PY) * Lp.y) / np.maximum(d, 1) / math.hypot(Lp.x, Lp.y)
    side = np.clip((ang_lit + 0.25) / 0.9, 0, 1)
    halo = 0.75 * np.exp(-((d - RP) / (0.011 * RP)) ** 2) * side * (d > RP * 0.995)
    hc = K.to_srgb(np.array([0.30, 0.52, 1.0]))
    a0 = pl[..., 3]
    out_a = a0 + halo * (1 - a0)
    pl[..., :3] = (pl[..., :3] * a0[..., None] + hc * (halo * (1 - a0))[..., None]) / np.maximum(out_a[..., None], 1e-6)
    pl[..., 3] = out_a
    m = 0.10 * w_r * H + 40                                  # drift margin around the window
    bx0, by0 = int(max(0, (wcx * W) - w_r * H - m)), int(max(0, (wcy * H) - w_r * H - m))
    bx1, by1 = int(min(W, (wcx * W) + w_r * H + m)), int(min(H, (wcy * H) + w_r * H + m))
    sub = pl[by0:by1, bx0:bx1]
    bb = K.alpha_bbox(sub, margin=0)
    rect = (bx0 + bb[0], by0 + bb[1], bx0 + bb[2], by0 + bb[3])
    K.save(pl[rect[1]:rect[3], rect[0]:rect[2]], os.path.join(OUT, "menu_ship_planet.png"))
    layers.append({"name": "planet", "file": "menu_ship_planet.png", "depth": 0.04, "rect": K.norm_rect(rect, W, H), "role": "planet"})
    # ---------------------------------------------------------- 3 cabin (window hole transparent), 5 foreground
    setup("cabin")
    times["cabin"] = round(K.render(sc, os.path.join(WORK, "cabin.png"), W, H), 1)
    cabin = K.load(os.path.join(WORK, "cabin.png"))
    opening = np.hypot(xx + 0.5 - wcx * W, yy + 0.5 - wcy * H) < (w_r * H * (RW - 0.09) / RW)
    print("CABIN opening: max alpha", round(float(cabin[..., 3][opening].max()), 4), "mean", round(float(cabin[..., 3][opening].mean()), 5))
    K.save(cabin, os.path.join(OUT, "menu_ship_cabin.png"))
    layers.append({"name": "cabin", "file": "menu_ship_cabin.png", "depth": 0.35, "rect": [0, 0, 1, 1]})
    setup("foreground")
    times["foreground"] = round(K.render(sc, os.path.join(WORK, "fg.png"), W, H), 1)
    fgp = K.load(os.path.join(WORK, "fg.png"))
    bb = K.alpha_bbox(fgp)
    K.save(fgp[bb[1]:bb[3], bb[0]:bb[2]], os.path.join(OUT, "menu_ship_fg.png"))
    layers.append({"name": "foreground", "file": "menu_ship_fg.png", "depth": 0.9, "rect": K.norm_rect(bb, W, H)})
    json.dump({"layers": layers}, open(os.path.join(WORK, "static.json"), "w"), indent=1)
    print("SHIP LAYERS", times)

if only in (None, "cadet"):
    layers = json.load(open(os.path.join(WORK, "static.json")))["layers"]
    xs, ys = [], []
    for f in range(idle.N_FRAMES):
        idle.pose(f)
        dgl = bpy.context.evaluated_depsgraph_get()
        for o in CADET:
            if o.type == "MESH" and not o.hide_render:
                me = o.evaluated_get(dgl).to_mesh()
                for vtx in list(me.vertices)[::3]:
                    v = world_to_camera_view(sc, cam, o.matrix_world @ vtx.co)
                    xs.append(v.x); ys.append(1 - v.y)
    idle.rest()
    M = 10
    rect = (int(max(0, min(xs) * SW - M)), int(max(0, min(ys) * SH - M)), int(min(SW, max(xs) * SW + M)), int(min(SH, max(ys) * SH + M)))
    fw, fh = rect[2] - rect[0], rect[3] - rect[1]
    print("CADET RECT 0.75", rect, fw, fh)
    frames = []
    t0 = time.time()
    setup("cadet")
    sc.eevee.taa_render_samples = 64
    for f in range(idle.N_FRAMES):
        idle.pose(f)
        K.render(sc, os.path.join(WORK, f"c_{f:02d}.png"), SW, SH, rect)
        fr_ = K.load(os.path.join(WORK, f"c_{f:02d}.png"))
        assert fr_.shape[:2] == (fh, fw), fr_.shape
        frames.append(fr_)
    idle.rest()
    times["cadet_32_frames"] = round(time.time() - t0, 1)
    sheet = np.zeros((fh * 4, fw * 8, 4), np.float32)
    for i, fr_ in enumerate(frames):
        r_, c_ = divmod(i, 8)
        sheet[r_ * fh:(r_ + 1) * fh, c_ * fw:(c_ + 1) * fw] = fr_
    K.save(sheet, os.path.join(OUT, "menu_ship_cadet.png"))
    print("LOOP diff 31->0", round(float(np.abs(frames[-1] - frames[0]).mean() * 255), 3),
          "mean step", round(float(np.mean([np.abs(frames[i + 1] - frames[i]).mean() for i in range(31)]) * 255), 3))
    cad_rect = [round(rect[0] / SW, 5), round(rect[1] / SH, 5), round(rect[2] / SW, 5), round(rect[3] / SH, 5)]
    layers.insert(3, {"name": "cadet", "file": "menu_ship_cadet.png", "depth": 0.5, "rect": cad_rect, "role": "cadet",
                      "sheet": {"cols": 8, "rows": 4, "frames": 32, "fps": 8}})
    idle.rest()
    dgl = bpy.context.evaluated_depsgraph_get()
    ax, ay = [], []
    for o in CADET:
        if o.type == "MESH" and not o.hide_render:
            for vtx in list(o.evaluated_get(dgl).to_mesh().vertices)[::2]:
                v = world_to_camera_view(sc, cam, o.matrix_world @ vtx.co)
                ax.append(v.x); ay.append(1 - v.y)
    beacons = []
    for p, col in sc["ship_beacons"]:
        x, y = scr(p)
        if 0 <= x <= 1 and 0 <= y <= 1:
            beacons.append({"x": x, "y": y, "color": col, "layer": "cabin"})
    manifest = {"scene": "ship", "canvas": [W, H], "layers": layers,
                "points": {"window": {"x": wcx, "y": wcy, "r": round(w_r, 4)}, "beacons": beacons,
                           "astronaut": {"x0": round(min(ax), 4), "y0": round(min(ay), 4), "x1": round(max(ax), 4), "y1": round(max(ay), 4)}}}
    json.dump(manifest, open(os.path.join(OUT, "menu_scene_ship.json"), "w", encoding="utf-8"), indent=1)
    canvas = np.zeros((H, W, 4), np.float32)
    bgonly = None
    for L in layers:
        x0n, y0n, x1n, y1n = L["rect"]
        px0, py0, px1, py1 = round(x0n * W), round(y0n * H), round(x1n * W), round(y1n * H)
        img = K.load(os.path.join(OUT, L["file"]))
        if L["name"] == "cadet":
            bgonly = canvas.copy()
            img = frames[0]
        if img.shape[:2] != (py1 - py0, px1 - px0):
            img = K.resize(img, px1 - px0, py1 - py0)
        K.over(canvas, img, px0, py0)
    K.save(canvas[..., :3], os.path.join(OUT, "preview_ship.png"))
    bgc = K.resize(bgonly[round(cad_rect[1] * H):round(cad_rect[3] * H), round(cad_rect[0] * W):round(cad_rect[2] * W)], fw, fh)
    contact = np.zeros((fh * 4, fw * 8, 4), np.float32)
    for i, fr_ in enumerate(frames):
        r_, c_ = divmod(i, 8)
        tile = bgc.copy()
        K.over(tile, fr_, 0, 0)
        contact[r_ * fh:(r_ + 1) * fh, c_ * fw:(c_ + 1) * fw] = tile
    K.save(K.resize(contact, contact.shape[1] // 2, contact.shape[0] // 2)[..., :3], os.path.join(OUT, "contact_ship.png"))
    idle.keyframe_all(sc)
    for _f in os.listdir(WORK):               # per-frame intermediates are not kept
        if _f.endswith(".png") and _f.startswith(('c_',)):
            os.remove(os.path.join(WORK, _f))
for o in sc.objects:
    if o.type == "MESH":
        o.visible_camera, o.is_holdout, o.visible_shadow = True, False, o not in FG
bpy.ops.wm.save_mainfile()
print("SHIP TIMES", times)
