# MARS menu scene v2 (Cycles, premium clay): layered renders + cadet idle sprite sheet + manifest (same schema/file names as v1).
# blender -b Art/Blender/mars_keyart_sunset_v2.blend --python Art/Blender/menu_pipeline/layers_mars_v2.py [-- only=layers|cadet]
import bpy, sys, os, math, json, time
import numpy as np
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import scenekit as K
import idle
import clay as C

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
opts = dict(a.split("=", 1) for a in argv if "=" in a)
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
OUT = os.path.join(ROOT, "Art", "Blender", "renders", "menu", "scenes")
WORK = os.path.join(OUT, "_work_mars_v2")
os.makedirs(WORK, exist_ok=True)
D = bpy.data
assert D.filepath.lower().endswith("mars_keyart_sunset_v2.blend"), D.filepath
sc = bpy.context.scene
cam = sc.camera
W, H, SW, SH = 2560, 1440, 1920, 1080
SAMP = {"sky": 32, "mountains": int(opts.get("s_m", 160)), "base": int(opts.get("s_b", 320)), "foreground": int(opts.get("s_f", 192)),
        "cadet": int(opts.get("s_c", 320))}
dev = C.cycles_setup(sc, samples=64)
print("DEVICE", dev, "SAMPLES", SAMP)
for _n in idle.RIG:
    D.objects[_n].animation_data_clear()
sc.frame_set(1)
times = {}
CADET = list(D.collections["cadet"].all_objects)
FG = list(D.collections["menu_fg"].objects)
MOUNT = [D.objects["ridge"]]
GROUND = D.objects["ground"]
LOOPS = list(D.collections["menu_loops"].all_objects) if D.collections.get("menu_loops") else []   # own layers (loops_mars_v2.py)
REND = [o for o in sc.objects if o.type in ("MESH", "CURVE") and not o.hide_render and o not in LOOPS]
BASE = [o for o in REND if o not in CADET and o not in FG and o not in MOUNT]
fr = [cam.matrix_world @ c for c in cam.data.view_frame(scene=sc)]
tr, br, bl, tl = fr
cpos = cam.matrix_world.translation.copy()
dg = bpy.context.evaluated_depsgraph_get()


def ray(u, v):
    p = tl + (tr - tl) * u + (bl - tl) * v
    return (p - cpos).normalized()


def setup(layer):
    for o in LOOPS:
        o.visible_camera, o.visible_shadow = False, False
    sc.render.film_transparent = layer != "sky"
    sc.cycles.samples = SAMP.get(layer, 128)
    for o in REND:
        o.visible_camera, o.is_holdout, o.is_shadow_catcher = True, False, False
        o.visible_shadow = o.name != "cad_helmet" and o not in FG
    if layer == "sky":
        for o in REND:
            o.visible_camera = False
    elif layer == "mountains":
        for o in BASE + CADET + FG:
            o.visible_camera = False
    elif layer == "base":
        for o in MOUNT:
            o.is_holdout = True
        for o in CADET + FG:
            o.visible_camera, o.visible_shadow = False, False
    elif layer == "foreground":
        for o in MOUNT + BASE + CADET:
            o.is_holdout = True
    elif layer == "cadet":
        for o in MOUNT + BASE:
            o.is_holdout = True
        for o in FG:
            o.visible_camera = False
    elif layer == "base_sh":                       # base with ONLY the cadet's shadow added (cadet itself invisible)
        for o in MOUNT:
            o.is_holdout = True
        for o in CADET:
            o.visible_camera = False
            o.visible_shadow = o.name != "cad_helmet"
        for o in FG:
            o.visible_camera, o.visible_shadow = False, False


def reset():
    for o in LOOPS:
        o.visible_camera, o.visible_shadow = True, o.name != "v2_dustdevil"
    for o in REND:
        o.visible_camera, o.is_holdout, o.is_shadow_catcher = True, False, False
        o.visible_shadow = o.name != "cad_helmet" and o not in FG
    sc.render.film_transparent = False


def points():
    def scr(p):
        v = world_to_camera_view(sc, cam, Vector(p))
        return round(v.x, 4), round(1 - v.y, 4)
    sdir = Vector(sc["menu_sun_disc_dir"])
    srad = sc["menu_sun_radius_rad"]
    sx, sy = scr(cpos + sdir * 1000)
    perp = sdir.cross(Vector((0, 0, 1))).normalized()
    ex, ey = scr(cpos + (sdir * math.cos(srad) + perp * math.sin(srad)) * 1000)
    g = math.radians(4.0)
    gx, gy = scr(cpos + (sdir * math.cos(g) + perp * math.sin(g)) * 1000)
    az = math.atan2(sdir.y, sdir.x) + math.radians(2.5)
    el = math.asin(sdir.z) + math.radians(3.0)
    earth = scr(cpos + Vector((math.cos(el) * math.cos(az), math.cos(el) * math.sin(az), math.sin(el))) * 1000)
    cols = {"window": "#FF9A4A", "door": "#FFB066", "beacon": "#FF3A24", "pod": "#FF5A3C"}
    beacons, airlock = [], None
    for name, pos, kind in sc["menu_light_markers"]:
        p = Vector(pos)
        x, y = scr(p)
        dvec = p - cpos
        o = cpos.copy()
        visible = False
        for _ in range(24):
            hit, loc, nor, idx, ob, mt = sc.ray_cast(dg, o, dvec.normalized())
            if not hit:
                break
            if ob in CADET or ob in FG or ob.name.startswith(("v2_pebble", "pebble")):
                o = loc + dvec.normalized() * 0.01
                continue
            visible = (loc - cpos).length >= dvec.length - 0.35
            break
        if visible and 0 <= x <= 1 and 0 <= y <= 1:
            beacons.append({"x": x, "y": y, "color": cols[kind], "layer": "base"})
            if kind == "door" and airlock is None:
                airlock = {"x": x, "y": y}
    props = {o.name for o in sc.objects if o.name not in ("ground", "ridge")}
    horizon = []
    for i in range(21):
        u = min(max(i / 20, 0.0005), 0.9995)
        v = 0.25
        while v < 0.95:
            o = cpos.copy(); dd = ray(u, v); hit_ground = False
            for _ in range(24):
                hit, loc, nor, idx, ob, mt = sc.ray_cast(dg, o, dd)
                if not hit:
                    break
                if ob.name in props:
                    o = loc + dd * 0.01
                    continue
                hit_ground = ob.name == "ground"
                break
            if hit_ground:
                horizon.append([round(i / 20, 4), round(v, 4)])
                break
            v += 0.002
    return {"sun": {"x": sx, "y": sy, "radius": round(math.hypot((ex - sx) * W, (ey - sy) * H) / H, 4),
                    "glow_radius": round(math.hypot((gx - sx) * W, (gy - sy) * H) / H, 4), "layer": "sky"},
            "earth": {"x": earth[0], "y": earth[1], "layer": "sky"}, "beacons": beacons, "airlock": airlock, "horizon": horizon}


def cadet_px_bbox(w, h, frames=True):
    xs, ys = [], []
    for f in (range(idle.N_FRAMES) if frames else [0]):
        idle.pose(f)
        dgl = bpy.context.evaluated_depsgraph_get()
        for o in CADET:
            if o.type != "MESH" or o.hide_render:
                continue
            me = o.evaluated_get(dgl).to_mesh()
            for vtx in list(me.vertices)[::4]:
                v = world_to_camera_view(sc, cam, o.matrix_world @ vtx.co)
                xs.append(v.x * w); ys.append((1 - v.y) * h)
            o.evaluated_get(dgl).to_mesh_clear()
    idle.rest()
    return min(xs), min(ys), max(xs), max(ys)


only = opts.get("only")
if only in (None, "layers"):
    layers = []
    for layer in ("sky", "mountains", "base", "foreground"):
        setup(layer)
        times[layer] = round(K.render(sc, os.path.join(WORK, f"{layer}.png"), W, H), 1)
        print("RENDERED", layer, times[layer], flush=True)
    pts = points()
    sky = K.load(os.path.join(WORK, "sky.png"))
    ex, ey = pts["earth"]["x"] * W, pts["earth"]["y"] * H
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    d = np.hypot(xx + 0.5 - ex, yy + 0.5 - ey)
    core = np.clip(3.1 - d, 0, 1)[..., None]
    halo = (0.55 * np.exp(-(d / 5.0) ** 2 / 2))[..., None]
    sky[..., :3] = sky[..., :3] * (1 - halo) + np.array([0.56, 0.71, 0.94], np.float32) * halo
    sky[..., :3] = sky[..., :3] * (1 - core) + np.array([0.90, 0.95, 1.0], np.float32) * core
    K.save(sky[..., :3], os.path.join(OUT, "menu_mars_sky.jpg"), "JPEG", 90)
    layers.append({"name": "sky", "file": "menu_mars_sky.jpg", "depth": 0.0, "rect": [0, 0, 1, 1]})
    for layer, depth in (("mountains", 0.12), ("base", 0.3), ("foreground", 0.9)):
        px = K.load(os.path.join(WORK, f"{layer}.png"))
        bb = K.alpha_bbox(px)
        fn = f"menu_mars_{'fg' if layer == 'foreground' else layer}.png"
        K.save(px[bb[1]:bb[3], bb[0]:bb[2]], os.path.join(OUT, fn))
        layers.append({"name": layer, "file": fn, "depth": depth, "rect": K.norm_rect(bb, W, H)})
        print("LAYER", layer, bb)
    json.dump({"layers": layers, "points": pts}, open(os.path.join(WORK, "static.json"), "w"), indent=1)

if only in (None, "cadet"):
    st = json.load(open(os.path.join(WORK, "static.json")))
    x0, y0, x1, y1 = cadet_px_bbox(SW, SH)
    # shadow extent: cadet vertices projected along the key light onto the ground plane under the feet
    sun = D.objects["Sun"]
    ldir = (sun.matrix_world.to_3x3() @ Vector((0, 0, -1))).normalized()
    gz = D.objects["Root"].matrix_world.translation.z
    sxs, sys_ = [], []
    dgl = bpy.context.evaluated_depsgraph_get()
    for o in CADET:
        if o.type == "MESH" and not o.hide_render:
            for vtx in list(o.evaluated_get(dgl).to_mesh().vertices)[::6]:
                p = o.matrix_world @ vtx.co
                t = (gz - p.z) / ldir.z
                v = world_to_camera_view(sc, cam, p + ldir * t)
                sxs.append(v.x * SW); sys_.append((1 - v.y) * SH)
    sb = (min(sxs), min(sys_), max(sxs), max(sys_))
    M = 10
    rect = (int(max(0, math.floor(min(x0, sb[0])) - M)), int(max(0, math.floor(min(y0, sb[1])) - M)),
            int(min(SW, math.ceil(max(x1, sb[2])) + M)), int(min(SH, math.ceil(max(y1, sb[3])) + M)))
    if "rect" in opts:                                 # keep the previous rect when the new cadet + shadow fit inside it
        keep = tuple(int(v) for v in opts["rect"].split(","))
        if keep[0] <= rect[0] + M and keep[1] <= rect[1] + M and keep[2] >= rect[2] - M and keep[3] >= rect[3] - M:
            rect = keep
        else:
            print("RECT does not fit, using", rect)
    fw, fh = rect[2] - rect[0], rect[3] - rect[1]
    print("CADET RECT 0.75", rect, fw, fh, "shadow bbox", sb, flush=True)
    frames = []
    t0 = time.time()
    sc.cycles.use_adaptive_sampling = False        # identical sample counts in base / base_sh -> noise cancels in the ratio
    setup("base")
    sc.cycles.samples = SAMP["cadet"]
    K.render(sc, os.path.join(WORK, "c_base.png"), SW, SH, rect)
    base_c = K.load(os.path.join(WORK, "c_base.png"))
    nfr = 1 if opts.get("test") else idle.N_FRAMES
    for f in range(nfr):
        idle.pose(f)
        setup("cadet")
        K.render(sc, os.path.join(WORK, f"c_{f:02d}.png"), SW, SH, rect)
        setup("base_sh")
        sc.cycles.samples = SAMP["cadet"]
        K.render(sc, os.path.join(WORK, f"cs_{f:02d}.png"), SW, SH, rect)
        cad = K.load(os.path.join(WORK, f"c_{f:02d}.png"))
        assert cad.shape[:2] == (fh, fw), cad.shape
        sh = K.shadow_layer(base_c, K.load(os.path.join(WORK, f"cs_{f:02d}.png")))
        a = sh[..., 3]
        keep = K.gblur(a, 2.0) > 0.06                  # drop isolated specks, keep the contiguous cast shadow
        sh[..., 3] = np.where(keep, a, 0.0)
        fr_ = np.zeros_like(cad)
        K.over(fr_, sh, 0, 0)
        K.over(fr_, cad, 0, 0)
        frames.append(fr_)
        if opts.get("test"):
            tst = base_c.copy()
            K.over(tst, fr_, 0, 0)
            K.save(np.concatenate([fr_, tst], 1), os.path.join(WORK, "test_frame.png"))
            print("TEST shadow alpha max", round(float(sh[..., 3].max()), 3), "px>0.02", int((sh[..., 3] > 0.02).sum()), "of", fw * fh)
            raise SystemExit(0)
        if f % 8 == 0:
            print("frame", f, round(time.time() - t0, 1), flush=True)
    idle.rest()
    times["cadet_32_frames"] = round(time.time() - t0, 1)
    sheet = np.zeros((fh * 4, fw * 8, 4), np.float32)
    for i, fr_ in enumerate(frames):
        r_, c_ = divmod(i, 8)
        sheet[r_ * fh:(r_ + 1) * fh, c_ * fw:(c_ + 1) * fw] = fr_
    K.save(sheet, os.path.join(OUT, "menu_mars_cadet.png"))
    feet = slice(int(fh * 0.86), fh)
    print("LOOP diff 31->0", round(float(np.abs(frames[-1] - frames[0]).mean() * 255), 3),
          "mean step", round(float(np.mean([np.abs(frames[i + 1] - frames[i]).mean() for i in range(31)]) * 255), 3),
          "feet-region step (denoise flicker check)", round(float(np.mean([np.abs(frames[i + 1][feet] - frames[i][feet]).mean() for i in range(31)]) * 255), 3))
    cad_rect = [round(rect[0] / SW, 5), round(rect[1] / SH, 5), round(rect[2] / SW, 5), round(rect[3] / SH, 5)]
    layers = st["layers"]
    layers.insert(3, {"name": "cadet", "file": "menu_mars_cadet.png", "depth": 0.5, "rect": cad_rect, "role": "cadet",
                      "sheet": {"cols": 8, "rows": 4, "frames": 32, "fps": 8}})
    ax0, ay0, ax1, ay1 = cadet_px_bbox(1.0, 1.0, frames=False)
    pts = st["points"]
    pts["astronaut"] = {"x0": round(ax0, 4), "y0": round(ay0, 4), "x1": round(ax1, 4), "y1": round(ay1, 4)}
    manifest = {"scene": "mars", "canvas": [W, H], "layers": layers,
                "points": {k: pts[k] for k in ("sun", "earth", "beacons", "airlock", "astronaut", "horizon")}}
    json.dump(manifest, open(os.path.join(OUT, "menu_scene_mars.json"), "w", encoding="utf-8"), indent=1)
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
    K.save(canvas[..., :3], os.path.join(OUT, "preview_mars.png"))
    bgc = K.resize(bgonly[round(cad_rect[1] * H):round(cad_rect[3] * H), round(cad_rect[0] * W):round(cad_rect[2] * W)], fw, fh)
    contact = np.zeros((fh * 4, fw * 8, 4), np.float32)
    for i, fr_ in enumerate(frames):
        r_, c_ = divmod(i, 8)
        tile = bgc.copy()
        K.over(tile, fr_, 0, 0)
        contact[r_ * fh:(r_ + 1) * fh, c_ * fw:(c_ + 1) * fw] = tile
    K.save(K.resize(contact, contact.shape[1] // 2, contact.shape[0] // 2)[..., :3], os.path.join(OUT, "contact_mars.png"))
    for _f in os.listdir(WORK):
        if _f.startswith("c_") and _f.endswith(".png"):
            os.remove(os.path.join(WORK, _f))
    idle.keyframe_all(sc)
reset()
bpy.ops.wm.save_mainfile()
print("MARS V2 TIMES", times)
