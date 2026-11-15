# MARS menu scene (save exists): layered parallax renders + cadet idle sprite sheet + manifest.
# blender -b Art/Blender/mars_keyart_sunset.blend --python Art/Blender/menu_pipeline/layers_mars.py [-- only=cadet|layers] [preview]
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
WORK = os.path.join(OUT, "_work_mars")
os.makedirs(WORK, exist_ok=True)
D = bpy.data
assert D.filepath.lower().endswith("mars_keyart_sunset.blend"), D.filepath
sc = bpy.context.scene
cam = sc.camera
W, H = 2560, 1440
SW, SH = 1920, 1080                     # cadet sheet frames at 0.75 of the canvas
SAMPLES = int(opts.get("samples", 96))
times = {}
for _n in idle.RIG:                      # a previous run baked the loop as keyframes; pose by script instead
    D.objects[_n].animation_data_clear()
sc.frame_set(1)


def reset_visibility():
    for o in sc.objects:
        if o.type in ("MESH", "CURVE"):
            o.visible_camera, o.is_holdout = True, False
            o.visible_shadow = o.name not in [x.name for x in D.collections["menu_fg"].objects]
    sc.render.film_transparent = False

# ------------------------------------------------------------------ foreground rocks (near camera, bottom-left / bottom)
fg = D.collections.get("menu_fg") or D.collections.new("menu_fg")
if fg.name not in [c.name for c in sc.collection.children]:
    sc.collection.children.link(fg)
for o in list(fg.objects):
    D.objects.remove(o, do_unlink=True)
fr = [cam.matrix_world @ c for c in cam.data.view_frame(scene=sc)]
tr, br, bl, tl = fr
cpos = cam.matrix_world.translation.copy()


def ray(u, v):
    p = tl + (tr - tl) * u + (bl - tl) * v
    return (p - cpos).normalized()


dg = bpy.context.evaluated_depsgraph_get()


def ground_z(x, y):
    o = Vector((x, y, 60.0))
    for _ in range(10):
        hit, loc, nor, idx, ob, mt = sc.ray_cast(dg, o, Vector((0, 0, -1)))
        if not hit:
            return 0.0
        if ob.name == "ground":
            return loc.z
        o = loc + Vector((0, 0, -0.01))
    return 0.0


FG_ROCKS = [("rock35", 0.05, 0.95, 4.4, 0.62), ("rock20", 0.20, 0.97, 3.7, 1.15), ("rock25", 0.53, 0.99, 4.6, 0.42)]
# darker, barely rim-lit copy of the rock material: silhouettes that frame the corner
fgm = D.materials.get("rock_fg")
if fgm is None:   # dark warm silhouette with a faint top lift: frames the corner, no blue rim
    fgm = D.materials.new("rock_fg")
    if not fgm.node_tree:
        fgm.use_nodes = True
    nt = fgm.node_tree
    nt.nodes.clear()
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(geo.outputs["Normal"], sep.inputs[0])
    mr = nt.nodes.new("ShaderNodeMapRange")
    mr.interpolation_type = "SMOOTHSTEP"
    nt.links.new(sep.outputs[2], mr.inputs[0])
    mr.inputs[1].default_value, mr.inputs[2].default_value = 0.2, 0.95
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    nt.links.new(mr.outputs[0], mix.inputs[0])
    mix.inputs[6].default_value = (0.020, 0.010, 0.008, 1)
    mix.inputs[7].default_value = (0.075, 0.036, 0.022, 1)
    em = nt.nodes.new("ShaderNodeEmission")
    nt.links.new(mix.outputs[2], em.inputs["Color"])
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(em.outputs[0], out.inputs["Surface"])
for i, (src, u, v, dist, scl) in enumerate(FG_ROCKS):
    s = D.objects[src]
    o = s.copy()
    o.data = s.data.copy()
    fg.objects.link(o)
    o.name = f"fg_rock{i}"
    d = ray(u, v)
    d.z = 0
    p = cpos + d.normalized() * dist
    o.location = Vector((p.x, p.y, ground_z(p.x, p.y) - 0.15))
    o.scale = s.scale * scl
    o.rotation_euler.z += 0.7 * i
    o.hide_render = False
    o.data.materials.clear()
    o.data.materials.append(fgm)
bpy.context.view_layer.update()

# ------------------------------------------------------------------ layer membership
CADET = [o for o in D.collections["cadet"].all_objects]
FG = list(fg.objects)
MOUNT = [D.objects["ridge"]]
BASE = [o for o in sc.objects if o.type in ("MESH", "CURVE") and o not in CADET and o not in FG and o not in MOUNT and not o.hide_render]
ORDER = {"mountains": MOUNT, "base": BASE, "cadet": CADET, "foreground": FG}
RANK = ["mountains", "base", "cadet", "foreground"]


def setup(layer, cadet_shadow=False):
    """layer objects visible, farther layers as holdout, nearer layers invisible to camera (and casting no shadow)."""
    sc.render.film_transparent = layer != "sky"
    sc.eevee.taa_render_samples = SAMPLES
    li = RANK.index(layer) if layer in RANK else -1
    for name, objs in ORDER.items():
        r = RANK.index(name)
        for o in objs:
            if layer == "sky":
                o.visible_camera, o.is_holdout = False, False
            elif r == li:
                o.visible_camera, o.is_holdout = True, False
            elif r < li:
                o.visible_camera, o.is_holdout = True, True
            else:
                o.visible_camera, o.is_holdout = False, False
            o.visible_shadow = name not in ("foreground",) and not (name == "cadet" and not (layer == "cadet" or cadet_shadow))
    if cadet_shadow:
        for o in CADET:
            o.visible_camera, o.visible_shadow = False, True


def lights_and_points():
    """normalized points for the manifest (top-left origin)."""
    def scr(p):
        v = world_to_camera_view(sc, cam, Vector(p))
        return round(v.x, 4), round(1 - v.y, 4)
    sdir = Vector(sc["menu_sun_disc_dir"])
    srad = sc["menu_sun_radius_rad"]
    sx, sy = scr(cpos + sdir * 1000)
    perp = sdir.cross(Vector((0, 0, 1))).normalized()
    ex, ey = scr(cpos + (sdir * math.cos(srad) + perp * math.sin(srad)) * 1000)
    r_h = math.hypot((ex - sx) * W, (ey - sy) * H) / H
    g = math.radians(4.0)
    gx, gy = scr(cpos + (sdir * math.cos(g) + perp * math.sin(g)) * 1000)
    glow_h = math.hypot((gx - sx) * W, (gy - sy) * H) / H
    # Earth: evening star ~3 deg above and ~2.5 deg left of the sun
    az = math.atan2(sdir.y, sdir.x) + math.radians(2.5)
    el = math.asin(sdir.z) + math.radians(3.0)
    edir = Vector((math.cos(el) * math.cos(az), math.cos(el) * math.sin(az), math.sin(el)))
    earth = scr(cpos + edir * 1000)
    cols = {"window": "#FF9A4A", "door": "#FFB066", "beacon": "#FF3A24", "pod": "#FF5A3C"}
    beacons, airlock = [], None
    for name, pos, kind in sc["menu_light_markers"]:
        p = Vector(pos)
        x, y = scr(p)
        dvec = p - cpos
        o = cpos.copy()
        visible = False
        for _ in range(16):
            hit, loc, nor, idx, ob, mt = sc.ray_cast(dg, o, dvec.normalized())
            if not hit:
                break
            if ob in CADET or ob in FG:
                o = loc + dvec.normalized() * 0.01
                continue
            visible = (loc - cpos).length >= dvec.length - 0.35
            break
        if visible and 0 <= x <= 1 and 0 <= y <= 1:
            beacons.append({"x": x, "y": y, "color": cols[kind], "layer": "base"})
            if kind == "door" and airlock is None:
                airlock = {"x": x, "y": y}
    # horizon (far edge of the plain in front of the ridge)
    props = {o.name for o in sc.objects if o.name not in ("ground", "ridge")}
    horizon = []
    for i in range(21):
        u = min(max(i / 20, 0.0005), 0.9995)
        v = 0.25
        while v < 0.95:
            o = cpos.copy(); dd = ray(u, v); hit_ground = False
            for _ in range(16):
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
    return {"sun": {"x": sx, "y": sy, "radius": round(r_h, 4), "glow_radius": round(glow_h, 4), "layer": "sky"},
            "earth": {"x": earth[0], "y": earth[1], "layer": "sky"},
            "beacons": beacons, "airlock": airlock, "horizon": horizon}


def cadet_bbox_px(w, h):
    """union of the cadet's projected bbox over all idle frames, in pixels at (w, h)"""
    xs, ys = [], []
    for f in range(idle.N_FRAMES):
        idle.pose(f)
        dgl = bpy.context.evaluated_depsgraph_get()
        for o in CADET:
            if o.type != "MESH" or o.hide_render:
                continue
            me = o.evaluated_get(dgl).to_mesh()
            for vtx in list(me.vertices)[::3]:
                v = world_to_camera_view(sc, cam, o.matrix_world @ vtx.co)
                xs.append(v.x * w); ys.append((1 - v.y) * h)
            o.evaluated_get(dgl).to_mesh_clear()
    idle.rest()
    return min(xs), min(ys), max(xs), max(ys)


manifest_layers = []
only = opts.get("only")
# ================================================================== static layers at 2560x1440
if only in (None, "layers"):
    for layer in ("sky", "mountains", "base", "foreground"):
        setup(layer)
        p = os.path.join(WORK, f"{layer}.png")
        times[layer] = round(K.render(sc, p, W, H), 1)
        print("RENDERED", layer, times[layer])
    pts = lights_and_points()
    # sky: add Earth (a small bluish evening star) then JPG
    sky = K.load(os.path.join(WORK, "sky.png"))
    ex, ey = pts["earth"]["x"] * W, pts["earth"]["y"] * H
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    d = np.hypot(xx + 0.5 - ex, yy + 0.5 - ey)
    core = np.clip(3.1 - d, 0, 1)[..., None]
    halo = (0.55 * np.exp(-(d / 5.0) ** 2 / 2))[..., None]
    sky[..., :3] = sky[..., :3] * (1 - halo) + np.array([0.56, 0.71, 0.94], np.float32) * halo
    sky[..., :3] = sky[..., :3] * (1 - core) + np.array([0.90, 0.95, 1.0], np.float32) * core
    K.save(sky[..., :3], os.path.join(OUT, "menu_mars_sky.jpg"), "JPEG", 90)
    manifest_layers.append({"name": "sky", "file": "menu_mars_sky.jpg", "depth": 0.0, "rect": [0, 0, 1, 1]})
    for layer, depth in (("mountains", 0.12), ("base", 0.3), ("foreground", 0.9)):
        px = K.load(os.path.join(WORK, f"{layer}.png"))
        bb = K.alpha_bbox(px)
        fn = f"menu_mars_{'fg' if layer == 'foreground' else layer}.png"
        K.save(px[bb[1]:bb[3], bb[0]:bb[2]], os.path.join(OUT, fn))
        manifest_layers.append({"name": layer, "file": fn, "depth": depth, "rect": K.norm_rect(bb, W, H)})
        print("LAYER", layer, bb)
    json.dump({"layers": manifest_layers, "points": pts}, open(os.path.join(WORK, "static.json"), "w"), indent=1)

# ================================================================== cadet idle sheet at 0.75 scale, with its ground shadow
if only in (None, "cadet"):
    st = json.load(open(os.path.join(WORK, "static.json")))
    x0, y0, x1, y1 = cadet_bbox_px(SW, SH)
    # shadow extent: full 0.75 frame of base with/without the cadet's shadow (rest pose)
    setup("base")
    K.render(sc, os.path.join(WORK, "s_base_full.png"), SW, SH)
    setup("base", cadet_shadow=True)
    K.render(sc, os.path.join(WORK, "s_base_sh_full.png"), SW, SH)
    shl = K.shadow_layer(K.load(os.path.join(WORK, "s_base_full.png")), K.load(os.path.join(WORK, "s_base_sh_full.png")))
    sb = K.alpha_bbox(shl, thr=0.03, margin=0)
    print("cadet bbox", (round(x0), round(y0), round(x1), round(y1)), "shadow bbox", sb)
    M = 10
    rx0 = int(max(0, math.floor(min(x0, sb[0] if sb else x0)) - M)); ry0 = int(max(0, math.floor(y0) - M))
    rx1 = int(min(SW, math.ceil(max(x1, sb[2] if sb else x1)) + M)); ry1 = int(min(SH, math.ceil(max(y1, sb[3] if sb else y1)) + M + 12))
    rect = (rx0, ry0, rx1, ry1)
    fw, fh = rx1 - rx0, ry1 - ry0
    print("CADET RECT 0.75", rect, fw, fh)
    setup("base")
    K.render(sc, os.path.join(WORK, "c_base.png"), SW, SH, rect)
    base_c = K.load(os.path.join(WORK, "c_base.png"))
    assert base_c.shape[:2] == (fh, fw), base_c.shape
    # contact shadow under the boots (rest pose), in rect pixels
    feet = []
    dgl = bpy.context.evaluated_depsgraph_get()
    for nme in ("SoleL", "SoleR", "BootL", "BootR"):
        o = D.objects[nme]
        for vtx in o.evaluated_get(dgl).to_mesh().vertices:
            v = world_to_camera_view(sc, cam, o.matrix_world @ vtx.co)
            feet.append((v.x * SW - rx0, (1 - v.y) * SH - ry0))
    fxc = sum(p[0] for p in feet) / len(feet); fyb = max(p[1] for p in feet)
    fwid = max(p[0] for p in feet) - min(p[0] for p in feet)
    yy, xx = np.mgrid[0:fh, 0:fw].astype(np.float32)
    e = ((xx - fxc) / (fwid * 0.62)) ** 2 + ((yy - fyb - 4) / (fwid * 0.62 * 0.16)) ** 2
    contact = 0.40 * np.exp(-2.2 * e)
    contact = np.where(contact < 0.004, 0.0, contact)
    frames = []
    t0 = time.time()
    for f in range(idle.N_FRAMES):
        idle.pose(f)
        setup("cadet")
        sc.eevee.taa_render_samples = 64
        K.render(sc, os.path.join(WORK, f"c_{f:02d}.png"), SW, SH, rect)
        setup("base", cadet_shadow=True)
        sc.eevee.taa_render_samples = 64
        K.render(sc, os.path.join(WORK, f"cs_{f:02d}.png"), SW, SH, rect)
        cad = K.load(os.path.join(WORK, f"c_{f:02d}.png"))
        sh = K.shadow_layer(base_c, K.load(os.path.join(WORK, f"cs_{f:02d}.png")))
        # contact shadow joins the cast shadow
        sa, ca = sh[..., 3], contact * (base_c[..., 3] > 0.5)
        col = (sh[..., :3] * sa[..., None] + np.array([0.13, 0.07, 0.07]) * ca[..., None] * (1 - sa[..., None]))
        a = sa + ca * (1 - sa)
        sh = np.concatenate([col / np.maximum(a[..., None], 1e-6), a[..., None]], 2)
        out = np.zeros_like(cad)
        K.over(out, sh, 0, 0)
        K.over(out, cad, 0, 0)
        frames.append(out)
    idle.rest()
    times["cadet_32_frames"] = round(time.time() - t0, 1)
    sheet = np.zeros((fh * 4, fw * 8, 4), np.float32)
    for i, fr_ in enumerate(frames):
        r_, c_ = divmod(i, 8)
        sheet[r_ * fh:(r_ + 1) * fh, c_ * fw:(c_ + 1) * fw] = fr_
    K.save(sheet, os.path.join(OUT, "menu_mars_cadet.png"))

    d_loop = float(np.abs(frames[-1] - frames[0]).mean() * 255)
    d_step = float(np.mean([np.abs(frames[i + 1] - frames[i]).mean() for i in range(31)]) * 255)
    print("LOOP diff 31->0", round(d_loop, 3), "mean step", round(d_step, 3))
    cad_rect = [round(rx0 / SW, 5), round(ry0 / SH, 5), round(rx1 / SW, 5), round(ry1 / SH, 5)]
    # astronaut bbox in canvas coords (rest pose)
    bx = cadet_bbox_px(1, 1) if False else None
    layers = st["layers"]
    layers.insert(3, {"name": "cadet", "file": "menu_mars_cadet.png", "depth": 0.5, "rect": cad_rect, "role": "cadet",
                      "sheet": {"cols": 8, "rows": 4, "frames": 32, "fps": 8}})
    idle.rest()
    xs, ys = [], []
    dgl = bpy.context.evaluated_depsgraph_get()
    for o in CADET:
        if o.type == "MESH" and not o.hide_render:
            for vtx in list(o.evaluated_get(dgl).to_mesh().vertices)[::2]:
                v = world_to_camera_view(sc, cam, o.matrix_world @ vtx.co)
                xs.append(v.x); ys.append(1 - v.y)
    pts = st["points"]
    pts["astronaut"] = {"x0": round(min(xs), 4), "y0": round(min(ys), 4), "x1": round(max(xs), 4), "y1": round(max(ys), 4)}
    manifest = {"scene": "mars", "canvas": [W, H], "layers": layers,
                "points": {k: pts[k] for k in ("sun", "earth", "beacons", "airlock", "astronaut", "horizon")}}
    json.dump(manifest, open(os.path.join(OUT, "menu_scene_mars.json"), "w", encoding="utf-8"), indent=1)
    # flat preview: all layers + cadet frame 0, and a contact sheet of the idle frames over the background
    canvas = np.zeros((H, W, 4), np.float32)
    bgonly = None
    for L in layers:
        x0n, y0n, x1n, y1n = L["rect"]
        px0, py0, px1, py1 = round(x0n * W), round(y0n * H), round(x1n * W), round(y1n * H)
        img = K.load(os.path.join(OUT, L["file"]))
        if L["name"] == "cadet":
            bgonly = canvas.copy()
            img = K.resize(frames[0], px1 - px0, py1 - py0)
        if img.shape[:2] != (py1 - py0, px1 - px0):
            img = K.resize(img, px1 - px0, py1 - py0)
        K.over(canvas, img, px0, py0)
    K.save(canvas[..., :3], os.path.join(OUT, "preview_mars.png"))
    bgc = bgonly[round(cad_rect[1] * H):round(cad_rect[3] * H), round(cad_rect[0] * W):round(cad_rect[2] * W)]
    bgc = K.resize(bgc, fw, fh)
    contact_img = np.zeros((fh * 4, fw * 8, 4), np.float32)
    for i, fr_ in enumerate(frames):
        r_, c_ = divmod(i, 8)
        tile = bgc.copy()
        K.over(tile, fr_, 0, 0)
        contact_img[r_ * fh:(r_ + 1) * fh, c_ * fw:(c_ + 1) * fw] = tile
    contact_img = K.resize(contact_img, contact_img.shape[1] // 2, contact_img.shape[0] // 2)
    K.save(contact_img[..., :3], os.path.join(OUT, "contact_mars.png"))
    idle.keyframe_all(sc)
    for _f in os.listdir(WORK):               # per-frame intermediates are not kept
        if _f.endswith(".png") and _f.startswith(('c_', 'cs_', 's_base', 'c_base')):
            os.remove(os.path.join(WORK, _f))
reset_visibility()
bpy.ops.wm.save_mainfile()
print("MARS TIMES", times)
