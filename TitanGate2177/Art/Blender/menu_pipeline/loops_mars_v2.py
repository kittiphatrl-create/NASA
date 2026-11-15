# Renders the Mars menu's extra animated layers and merges them into menu_scene_mars.json:
#   dustdevil (24 f, 8 fps, loop) | flag (24 f, 12 fps, loop) | door (16 f, 16 fps, play once, frame 0 == base layer)
# blender -b Art/Blender/mars_keyart_sunset_v2.blend --python Art/Blender/menu_pipeline/loops_mars_v2.py [-- only=dustdevil,flag,door]
import bpy, sys, os, math, json, time
import numpy as np
from mathutils import Vector, Matrix
from bpy_extras.object_utils import world_to_camera_view
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import scenekit as K
import clay as C
import idle

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
only = set(opts.get("only", "dustdevil,flag,door").split(","))
dev = C.cycles_setup(sc, samples=128)
print("DEVICE", dev)
for _n in idle.RIG:
    D.objects[_n].animation_data_clear()
sc.frame_set(1)
LOOPS = list(D.collections["menu_loops"].all_objects)
DEVIL = [D.objects["v2_dustdevil"]]
FLAG = [o for o in LOOPS if o.name.startswith("v2_flag")]
CADET = list(D.collections["cadet"].all_objects)
FG = list(D.collections["menu_fg"].objects)
MOUNT = [D.objects["ridge"]]
GROUND = D.objects["ground"]
REND = [o for o in sc.objects if o.type in ("MESH", "CURVE") and not o.hide_render and o not in LOOPS]
BASE = [o for o in REND if o not in CADET and o not in FG and o not in MOUNT]
times, stats = {}, {}


def reset_all():
    for o in REND + LOOPS:
        o.visible_camera, o.is_holdout, o.is_shadow_catcher = True, False, False
        o.visible_shadow = o.name not in ("cad_helmet", "v2_dustdevil") and o not in FG


def setup_loop(objs):
    """only `objs` visible; the terrain is a holdout so dunes in front still cut them; everything else invisible."""
    reset_all()
    sc.render.film_transparent = True
    for o in REND + LOOPS:
        o.visible_camera = False
    for o in objs:
        o.visible_camera = True
    GROUND.visible_camera, GROUND.is_holdout = True, True


def setup_base():
    """exactly the base-layer pass of layers_mars_v2.py"""
    reset_all()
    sc.render.film_transparent = True
    for o in LOOPS:
        o.visible_camera, o.visible_shadow = False, False
    for o in MOUNT:
        o.is_holdout = True
    for o in CADET + FG:
        o.visible_camera, o.visible_shadow = False, False


def scr(p):
    v = world_to_camera_view(sc, cam, Vector(p))
    return v.x, 1 - v.y


def proj_bbox(objs, w, h):
    xs, ys = [], []
    dg = bpy.context.evaluated_depsgraph_get()
    for o in objs:
        me = o.evaluated_get(dg).to_mesh()
        for v in me.vertices:
            x, y = scr(o.matrix_world @ v.co)
            xs.append(x * w); ys.append(y * h)
        o.evaluated_get(dg).to_mesh_clear()
    return min(xs), min(ys), max(xs), max(ys)


def clamp_rect(r, w, h, m):
    return (int(max(0, math.floor(r[0]) - m)), int(max(0, math.floor(r[1]) - m)), int(min(w, math.ceil(r[2]) + m)), int(min(h, math.ceil(r[3]) + m)))


def sheet(frames, cols, path):
    fh, fw = frames[0].shape[:2]
    rows = (len(frames) + cols - 1) // cols
    sh = np.zeros((fh * rows, fw * cols, 4), np.float32)
    for i, f in enumerate(frames):
        r_, c_ = divmod(i, cols)
        sh[r_ * fh:(r_ + 1) * fh, c_ * fw:(c_ + 1) * fw] = f
    K.save(sh, path)
    return rows


def loop_stats(frames, name):
    seam = float(np.abs(frames[-1] - frames[0]).mean() * 255)
    step = float(np.mean([np.abs(frames[i + 1] - frames[i]).mean() for i in range(len(frames) - 1)]) * 255)
    stats[name] = (round(seam, 3), round(step, 3))
    print("LOOP", name, "seam", round(seam, 3), "mean step", round(step, 3), flush=True)


results = {}
sc.cycles.use_adaptive_sampling = False

# ---------------------------------------------------------------- dust devil (0.75 scale)
if "dustdevil" in only:
    mat = D.objects["v2_dustdevil"].active_material
    ph = mat.node_tree.nodes["phase"]
    DW_, DH_ = 1280, 720                               # 0.5 of the canvas: a soft distant volume loses nothing
    setup_loop(DEVIL)
    sc.cycles.samples = 48
    rects = []
    for p_ in (0.0, 0.5):
        ph.outputs[0].default_value = p_
        K.render(sc, os.path.join(WORK, "dd_probe.png"), DW_, DH_)
        rects.append(K.alpha_bbox(K.load(os.path.join(WORK, "dd_probe.png")), thr=0.03, margin=0))
    rr = (min(r[0] for r in rects), min(r[1] for r in rects), max(r[2] for r in rects), max(r[3] for r in rects))
    rect = clamp_rect(rr, DW_, DH_, 8)
    sc.cycles.samples = int(opts.get("s_dd", 160))
    frames = []
    t0 = time.time()
    for k in range(24):
        ph.outputs[0].default_value = k / 24.0
        K.render(sc, os.path.join(WORK, f"dd_{k:02d}.png"), DW_, DH_, rect)
        frames.append(K.load(os.path.join(WORK, f"dd_{k:02d}.png")))
    ph.outputs[0].default_value = 0.0
    times["dustdevil"] = round(time.time() - t0, 1)
    rows = sheet(frames, 8, os.path.join(OUT, "menu_mars_dustdevil.png"))
    loop_stats(frames, "dustdevil")
    results["dustdevil"] = {"rect": [rect[0] / DW_, rect[1] / DH_, rect[2] / DW_, rect[3] / DH_], "rows": rows, "n": 24, "fps": 8, "frames": frames}

# ---------------------------------------------------------------- flag (0.75 scale)
if "flag" in only:
    flag = D.objects["v2_flag"]
    keys = flag.data.shape_keys.key_blocks

    def set_key(k):
        for kb in keys[1:]:
            kb.value = 0.0
        keys[k + 1].value = 1.0
    rr = None
    for k in range(24):
        set_key(k)
        b = proj_bbox(FLAG, SW, SH)
        rr = b if rr is None else (min(rr[0], b[0]), min(rr[1], b[1]), max(rr[2], b[2]), max(rr[3], b[3]))
    rect = clamp_rect(rr, SW, SH, 10)
    setup_loop(FLAG)
    sc.cycles.samples = int(opts.get("s_fl", 192))
    frames = []
    t0 = time.time()
    for k in range(24):
        set_key(k)
        K.render(sc, os.path.join(WORK, f"fl_{k:02d}.png"), SW, SH, rect)
        frames.append(K.load(os.path.join(WORK, f"fl_{k:02d}.png")))
    set_key(0)
    times["flag"] = round(time.time() - t0, 1)
    rows = sheet(frames, 8, os.path.join(OUT, "menu_mars_flag.png"))
    loop_stats(frames, "flag")
    results["flag"] = {"rect": [rect[0] / SW, rect[1] / SH, rect[2] / SW, rect[3] / SH], "rows": rows, "n": 24, "fps": 12, "frames": frames}

# ---------------------------------------------------------------- airlock door (full scale, frame 0 == base layer)
info = sc["v2_door"]
if "door" in only:
    leaves = info["leaves"]
    slide = Vector(info["slide"])
    DW = info["width"]
    glow = D.objects["v2_airlock_inner"].active_material.node_tree.nodes["door_glow"]
    area = D.objects["v2_airlock_area"].data
    closed = {n: Matrix([leaves[n]["m"][i * 4:(i + 1) * 4] for i in range(4)]) for n in leaves.keys()}

    def door_state(t):
        e = t * t * (3 - 2 * t)
        for n in leaves.keys():
            side = leaves[n]["side"]
            D.objects[n].matrix_world = Matrix.Translation(slide * side * (DW / 2) * e) @ closed[n]
        lt = min(1.0, t / 0.4)
        lt = lt * lt * (3 - 2 * lt)
        glow.outputs[0].default_value = 0.8 * lt          # warm, readable interior (not blown out)
        area.energy = 170.0 * lt                           # spills out onto the sand
    door_objs = list(D.collections["v2_airlock"].all_objects)
    pb = proj_bbox([o for o in door_objs if o.type == "MESH"], W, H)
    sx, sy = scr(info["spill"])
    rr = (min(pb[0], sx * W - 90), pb[1], max(pb[2], sx * W + 90), max(pb[3], sy * H + 60))
    rect = clamp_rect(rr, W, H, 24)
    setup_base()
    sc.cycles.use_adaptive_sampling = True
    sc.cycles.samples = 320                          # identical to the base layer pass
    frames = []
    t0 = time.time()
    for k in range(16):
        door_state(k / 15.0)
        K.render(sc, os.path.join(WORK, f"door_{k:02d}.png"), W, H, rect)
        frames.append(K.load(os.path.join(WORK, f"door_{k:02d}.png")))
    door_state(0.0)
    times["door"] = round(time.time() - t0, 1)
    # feather the patch edge so it melts into the base layer; compare frame 0 to the base layer pixels
    fh, fw = frames[0].shape[:2]
    yy, xx = np.mgrid[0:fh, 0:fw].astype(np.float32)
    edge = np.minimum(np.minimum(xx + 0.5, fw - xx - 0.5), np.minimum(yy + 0.5, fh - yy - 0.5))
    feather = np.clip(edge / 10.0, 0, 1)
    base_png = K.load(os.path.join(WORK, "base.png"))
    b0 = base_png[rect[1]:rect[3], rect[0]:rect[2]]
    stats["door_frame0_vs_base"] = round(float(np.abs(frames[0][..., :3] - b0[..., :3])[b0[..., 3] > 0.5].mean() * 255), 3)
    print("DOOR frame0 vs base mean abs diff (0-255):", stats["door_frame0_vs_base"], flush=True)
    for f in frames:
        f[..., 3] *= feather
    rows = sheet(frames, 8, os.path.join(OUT, "menu_mars_door.png"))
    results["door"] = {"rect": [rect[0] / W, rect[1] / H, rect[2] / W, rect[3] / H], "rows": rows, "n": 16, "fps": 16, "frames": frames}

# ---------------------------------------------------------------- manifest merge + preview + contact sheet
man = json.load(open(os.path.join(OUT, "menu_scene_mars.json"), encoding="utf-8"))
L = [l for l in man["layers"] if l["name"] not in ("dustdevil", "flag", "door")]
prev = {l["name"]: l for l in man["layers"]}
def entry(name, depth, role):
    if name in results:
        r = results[name]
        return {"name": name, "file": f"menu_mars_{name}.png", "depth": depth, "rect": [round(v, 5) for v in r["rect"]], "role": role,
                "sheet": {"cols": 8, "rows": r["rows"], "frames": r["n"], "fps": r["fps"]}}
    return prev.get(name)
order = []
for l in L:
    order.append(l)
    if l["name"] == "mountains" and entry("dustdevil", 0.2, "loop"):
        order.append(entry("dustdevil", 0.2, "loop"))
    if l["name"] == "base":
        for nm in ("door", "flag"):
            e = entry(nm, 0.3, "door" if nm == "door" else "loop")
            if e:
                order.append(e)
man["layers"] = order
dc = scr(info["center"])
ds = scr(info["spill"])
man["points"]["airlock"] = {"x": round(dc[0], 4), "y": round(dc[1], 4)}
man["points"]["door_light"] = {"x": round(ds[0], 4), "y": round(ds[1], 4)}
json.dump(man, open(os.path.join(OUT, "menu_scene_mars.json"), "w", encoding="utf-8"), indent=1)
canvas = np.zeros((H, W, 4), np.float32)
for l in man["layers"]:
    x0n, y0n, x1n, y1n = l["rect"]
    px0, py0, px1, py1 = round(x0n * W), round(y0n * H), round(x1n * W), round(y1n * H)
    img = K.load(os.path.join(OUT, l["file"]))
    if "sheet" in l:
        cols, rows = l["sheet"]["cols"], l["sheet"]["rows"]
        fh_, fw_ = img.shape[0] // rows, img.shape[1] // cols
        img = img[0:fh_, 0:fw_]
    if img.shape[:2] != (py1 - py0, px1 - px0):
        img = K.resize(img, px1 - px0, py1 - py0)
    K.over(canvas, img, px0, py0)
K.save(canvas[..., :3], os.path.join(OUT, "preview_mars.png"))
if results:
    tiles = []
    for name in ("dustdevil", "flag", "door"):
        if name in results:
            fr = results[name]["frames"]
            th = 180
            row = []
            for f in fr[::2] if len(fr) > 8 else fr:
                bg = np.zeros_like(f)
                bg[..., :3] = 0.35
                bg[..., 3] = 1
                K.over(bg, f, 0, 0)
                row.append(K.resize(bg, max(1, int(f.shape[1] * th / f.shape[0])), th))
            tiles.append(np.concatenate(row, 1))
    wmax = max(t.shape[1] for t in tiles)
    tiles = [np.concatenate([t, np.zeros((t.shape[0], wmax - t.shape[1], 4), np.float32)], 1) for t in tiles]
    K.save(np.concatenate(tiles, 0)[..., :3], os.path.join(OUT, "contact_mars_loops.png"))
for _f in os.listdir(WORK):
    if _f.startswith(("dd_", "fl_", "door_")) and _f.endswith(".png"):
        os.remove(os.path.join(WORK, _f))
reset_all()
sc.render.film_transparent = False
bpy.ops.wm.save_mainfile()
print("LOOPS TIMES", times, "STATS", stats)
