# Adds "Logo" and "Rover" scenes to mars_keyart_menu.blend, renders menu_logo.png and menu_rover.png.
# blender -b mars_keyart_menu.blend --python extras.py -- [ring=1] [only=logo|rover]
import bpy, sys, os, math, time
import numpy as np
from mathutils import Vector
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from nb import NB, srgb

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
opts = dict(a.split("=", 1) for a in argv if "=" in a)
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))   # project root
OUT = os.path.join(ROOT, r"Art\Blender\renders\menu")
FONT = os.path.join(ROOT, r"Assets\Resources\Fonts\CinzelDecorative-Bold.ttf")
D = bpy.data
assert bpy.data.filepath.lower().endswith("mars_keyart_menu.blend")
only = opts.get("only")
RING = opts.get("ring", "0") == "1"   # docking ring in the O: tried, competes with the O's own swash -> off
times = {}


def new_scene(name, w, h):
    old = D.scenes.get(name)
    if old:
        for o in list(old.collection.all_objects):
            D.objects.remove(o, do_unlink=True)
        D.scenes.remove(old)
    s = D.scenes.new(name)
    s.render.engine = "BLENDER_EEVEE"
    s.render.resolution_x, s.render.resolution_y = w, h
    s.render.resolution_percentage = 100
    s.render.film_transparent = True
    s.render.filter_size = 1.2
    s.eevee.taa_render_samples = 64
    s.view_settings.view_transform = "Standard"
    s.view_settings.look = "None"
    s.render.dither_intensity = 0.0
    s.render.use_compositing = False
    s.render.image_settings.file_format = "PNG"
    s.render.image_settings.color_mode = "RGBA"
    s.render.image_settings.color_depth = "8"
    w_ = D.worlds.new(name + "World")
    s.world = w_
    try:
        w_.use_nodes = True
    except Exception:
        pass
    bgn = next(n for n in w_.node_tree.nodes if n.type == "BACKGROUND")
    bgn.inputs["Color"].default_value = (0, 0, 0, 1)
    return s


def emission_mat(name, build):
    m = D.materials.new(name)
    try:
        m.use_nodes = True
    except Exception:
        pass
    nt = m.node_tree
    nt.nodes.clear()
    B = NB(nt)
    col = build(B)
    em = B.node("ShaderNodeEmission")
    B.set(em.inputs["Color"], col)
    out = B.node("ShaderNodeOutputMaterial")
    nt.links.new(em.outputs[0], out.inputs["Surface"])
    return m


def load_px(path):
    im = D.images.load(path, check_existing=False)
    w, h = im.size
    px = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)
    D.images.remove(im)
    return px


def save_px(px, path):
    h, w = px.shape[:2]
    im = D.images.new(os.path.basename(path), w, h, alpha=True)
    im.pixels[:] = np.clip(px, 0, 1).ravel()
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()
    D.images.remove(im)


def gblur(a, sigma):
    r = int(sigma * 3)
    k = np.exp(-0.5 * (np.arange(-r, r + 1) / sigma) ** 2)
    k /= k.sum()
    p = np.pad(a, r, mode="constant")
    p = np.apply_along_axis(lambda m: np.convolve(m, k, mode="valid"), 0, p)
    p = np.apply_along_axis(lambda m: np.convolve(m, k, mode="valid"), 1, p)
    return p


def bbox_of(obj, scene):
    vl = scene.view_layers[0]
    vl.update()
    dg = vl.depsgraph
    oe = obj.evaluated_get(dg)
    me = oe.to_mesh()
    vs = [obj.matrix_world @ v.co for v in me.vertices]
    oe.to_mesh_clear()
    return Vector([min(v[i] for v in vs) for i in range(3)]), Vector([max(v[i] for v in vs) for i in range(3)])


# =========================================================== LOGO
if only in (None, "logo"):
    LW, LH = 1600, 360
    s = new_scene("Logo", LW, LH)
    bpy.context.window.scene = s if bpy.context.window else None
    font = D.fonts.load(FONT, check_existing=True)
    SIZE = 1.0
    SPACING = 1.10

    def make_text(body, align="LEFT"):
        cu = D.curves.new("txt_" + body.replace(" ", "_"), "FONT")
        cu.body = body
        cu.font = font
        cu.size = SIZE
        cu.space_character = SPACING
        cu.space_word = 1.0
        cu.align_x = align
        cu.align_y = "BOTTOM_BASELINE"
        o = D.objects.new(cu.name, cu)
        s.collection.objects.link(o)
        return o

    # measure with flat (no bevel) left-aligned copies
    bpy.context.view_layer.update() if False else None
    meas = {}
    for b in ("LAST D", "LAST DO", "O", "LAST DOCK"):
        o = make_text(b)
        s.view_layers[0].update()
        mn, mx = bbox_of(o, s)
        meas[b] = (mn, mx)
        D.objects.remove(o, do_unlink=True)
    full_l = meas["LAST DOCK"]
    o_w = meas["O"][1].x - meas["O"][0].x
    o_cx_left = meas["LAST DO"][1].x - o_w / 2
    o_cy = (meas["O"][0].y + meas["O"][1].y) / 2
    cap_lo, cap_hi = full_l[0].y, full_l[1].y

    txt = make_text("LAST DOCK", "CENTER")
    cu = txt.data
    cu.body_format[0].kerning = 6          # the L's swash runs under the A; open that pair to match the others
    cu.extrude = 0.035
    cu.bevel_depth = 0.016
    cu.bevel_resolution = 4
    cu.offset = -0.010
    s.view_layers[0].update()
    mn, mx = bbox_of(txt, s)
    # left-aligned -> centred shift (use the flat measurement extents)
    shift = ((mn.x + mx.x) / 2) - ((full_l[0].x + full_l[1].x) / 2)
    o_cx = o_cx_left + shift
    print("LOGO bbox", tuple(round(v, 3) for v in mn), tuple(round(v, 3) for v in mx), "O centre", round(o_cx, 3), round(o_cy, 3), "O width", round(o_w, 3))

    y0, y1 = cap_lo, cap_hi

    def build_logo(B):
        tc = B.node("ShaderNodeTexCoord")
        geo = B.node("ShaderNodeNewGeometry")
        oy = B.sep(tc.outputs["Object"])[1]
        t = B.maprange(oy, y0, y1, 0.0, 1.0)
        rmp = B.node("ShaderNodeValToRGB")
        B.set(rmp.inputs[0], t)
        cr = rmp.color_ramp
        cr.interpolation = "EASE"
        stops = [(0.0, "#6E2C0E"), (0.16, "#A8501C"), (0.42, "#F0A030"), (0.70, "#F9D58E"), (1.0, "#FFF4E0")]
        cr.elements[0].position, cr.elements[0].color = stops[0][0], (*srgb(stops[0][1]), 1)
        cr.elements[1].position, cr.elements[1].color = stops[1][0], (*srgb(stops[1][1]), 1)
        for p, h in stops[2:]:
            e = cr.elements.new(p)
            e.color = (*srgb(h), 1)
        front_col = rmp.outputs["Color"]
        # faint polished sheen line across the face
        sheen = B.math("EXPONENT", B.math("MULTIPLY", B.math("POWER", B.math("DIVIDE", B.math("SUBTRACT", t, 0.60), 0.07), 2.0), -1.0))
        front_col = B.mix(B.math("MULTIPLY", sheen, 0.18), front_col, srgb("#FFFFFF"), "ADD")
        nx, ny, nz = B.sep(geo.outputs["Normal"])
        front = B.maprange(nz, 0.82, 0.97, 0.0, 1.0, "SMOOTHSTEP")
        up = B.math("ADD", ny, B.math("MULTIPLY", nx, 0.35))
        bev = B.node("ShaderNodeValToRGB")
        B.set(bev.inputs[0], B.maprange(up, -0.75, 0.75, 0.0, 1.0))
        br = bev.color_ramp
        br.interpolation = "EASE"
        br.elements[0].position, br.elements[0].color = 0.0, (*srgb("#3C1606"), 1)
        br.elements[1].position, br.elements[1].color = 1.0, (*srgb("#FFF8EC"), 1)
        e = br.elements.new(0.5)
        e.color = (*srgb("#C87A26"), 1)
        # bevel tone also follows the vertical gradient a little (copper low, cream high)
        bev_col = B.mix(0.35, bev.outputs["Color"], front_col, "MULTIPLY")
        bev_col = B.mix(0.55, bev_col, bev.outputs["Color"])
        return B.mix(front, bev_col, front_col)

    logo_mat = emission_mat("logo_metal", build_logo)
    txt.data.materials.append(logo_mat)

    if RING:
        # docking-port ring glyph in the counter of the O: thin ring + four small latches
        R = o_w * 0.205
        bpy.ops.mesh.primitive_torus_add(major_radius=R, minor_radius=o_w * 0.020, major_segments=64, minor_segments=10,
                                         location=(o_cx, o_cy, 0.03))
        ring = bpy.context.active_object
        ring.name = "dock_ring"
        ring.scale.z = 0.45
        for c in ring.users_collection:
            c.objects.unlink(ring)
        s.collection.objects.link(ring)
        ring.data.materials.append(logo_mat)
        for k in range(4):
            ang = math.radians(45 + 90 * k)
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(o_cx + math.cos(ang) * R, o_cy + math.sin(ang) * R, 0.03))
            lt = bpy.context.active_object
            lt.name = f"dock_latch{k}"
            lt.scale = (o_w * 0.075, o_w * 0.034, 0.02)
            lt.rotation_euler.z = ang
            for c in lt.users_collection:
                c.objects.unlink(lt)
            s.collection.objects.link(lt)
            lt.data.materials.append(logo_mat)
        bpy.ops.mesh.primitive_uv_sphere_add(radius=o_w * 0.03, location=(o_cx, o_cy, 0.03), segments=20, ring_count=10)
        dot = bpy.context.active_object
        dot.name = "dock_core"
        dot.scale.z = 0.5
        for c in dot.users_collection:
            c.objects.unlink(dot)
        s.collection.objects.link(dot)
        dot.data.materials.append(logo_mat)

    camd = D.cameras.new("LogoCam")
    camd.type = "ORTHO"
    width = mx.x - mn.x
    camd.ortho_scale = width / 0.84          # text fills 84 % of the width, rest is glow room
    camd.sensor_fit = "HORIZONTAL"
    cam = D.objects.new("LogoCam", camd)
    s.collection.objects.link(cam)
    cam.location = ((mn.x + mx.x) / 2, (y0 + y1) / 2, 10.0)
    cam.rotation_euler = (0, 0, 0)
    s.camera = cam
    print("LOGO text height fraction", round((y1 - y0) / (camd.ortho_scale * LH / LW), 3))
    raw = os.path.join(OUT, "logo_raw.png")
    s.render.filepath = raw
    t0 = time.time()
    bpy.ops.render.render(write_still=True, scene=s.name)
    times["logo"] = round(time.time() - t0, 1)
    px = load_px(raw)
    a = px[..., 3]
    glow = gblur(a, 9.0) * 0.55 + gblur(a, 22.0) * 0.45
    ga = np.clip(glow * 0.30, 0, 1)
    gcol = np.array(srgb("#F0A030"), dtype=np.float32)
    gcol = np.where(gcol <= 0.0031308, gcol * 12.92, 1.055 * np.power(gcol, 1 / 2.4) - 0.055)   # back to sRGB
    out_a = a + ga * (1 - a)
    out_pm = px[..., :3] * a[..., None] + gcol * ga[..., None] * (1 - a[..., None])
    out = np.concatenate([out_pm / np.maximum(out_a[..., None], 1e-6), out_a[..., None]], axis=2)
    out[..., :3] = np.where(out_a[..., None] > 0, out[..., :3], 0)
    save_px(out, os.path.join(OUT, "menu_logo.png"))
    ys, xs = np.nonzero(out_a[::-1] > 0.02)
    print("LOGO alpha bbox x", xs.min(), xs.max(), "y", ys.min(), ys.max())

# =========================================================== ROVER (side view silhouette)
if only in (None, "rover"):
    RW, RH, SS = 256, 128, 4
    s = new_scene("Rover", RW * SS, RH * SS)
    src = [o for o in D.collections["rover"].objects if o.type == "MESH"]
    sil = emission_mat("rover_silhouette", lambda B: (lambda geo: B.mix(
        B.maprange(B.math("ADD", B.sep(geo.outputs["Normal"])[2], B.math("MULTIPLY", B.sep(geo.outputs["Normal"])[0], 0.6)), 0.35, 0.95, 0.0, 1.0, "SMOOTHSTEP"),
        srgb("#2E1B16"), srgb("#9A5A38")))(B.node("ShaderNodeNewGeometry")))
    eye = emission_mat("rover_eye_amber", lambda B: B.rgb(tuple(c * 1.15 for c in srgb("#FFA040"))))
    dups = []
    for o in src:
        d = o.copy()
        d.data = o.data.copy()
        d.modifiers.clear() if False else None
        s.collection.objects.link(d)
        d.data.materials.clear()
        d.data.materials.append(eye if "eye" in o.name else sil)
        dups.append(d)
    s.view_layers[0].update()
    dg = s.view_layers[0].depsgraph
    mn = Vector((1e9,) * 3); mx = -mn
    eye_x = None
    for d in dups:
        de = d.evaluated_get(dg)
        me = de.to_mesh()
        for v in me.vertices:
            w = d.matrix_world @ v.co
            mn = Vector([min(a, b) for a, b in zip(mn, w)]); mx = Vector([max(a, b) for a, b in zip(mx, w)])
            if "eye" in d.name:
                eye_x = w.x
        de.to_mesh_clear()
    c = (mn + mx) / 2
    print("ROVER bbox", tuple(round(v, 2) for v in mn), tuple(round(v, 2) for v in mx), "eye x", eye_x)
    camd = D.cameras.new("RoverCam")
    camd.type = "ORTHO"
    L = mx.x - mn.x
    Hh = mx.z - mn.z
    camd.ortho_scale = max(L / 0.86, Hh / 0.80 * 2.0)
    cam = D.objects.new("RoverCam", camd)
    s.collection.objects.link(cam)
    faces_right = eye_x is not None and eye_x > c.x
    # look along +Y from -Y (front of rover = +X = screen right); if the eye is at -X, look from +Y instead
    if faces_right:
        cam.location = (c.x, mn.y - 20, c.z + camd.ortho_scale * 0.5 * (RH / RW) * 0.0)
        cam.rotation_euler = (math.radians(90), 0, 0)
    else:
        cam.location = (c.x, mx.y + 20, c.z)
        cam.rotation_euler = (math.radians(90), 0, math.radians(180))
    # sit the wheels near the bottom of the frame
    cam.location.z = mn.z + (camd.ortho_scale * RH / RW) * 0.5 - 0.06 * (camd.ortho_scale * RH / RW)
    s.camera = cam
    raw = os.path.join(OUT, "rover_raw.png")
    s.render.filepath = raw
    t0 = time.time()
    bpy.ops.render.render(write_still=True, scene=s.name)
    times["rover"] = round(time.time() - t0, 1)
    px = load_px(raw)
    h, w = px.shape[:2]
    pm = px.copy()
    pm[..., :3] *= pm[..., 3:4]
    pm = pm.reshape(h // SS, SS, w // SS, SS, 4).mean(axis=(1, 3))
    a = pm[..., 3:4]
    out = np.concatenate([pm[..., :3] / np.maximum(a, 1e-6), a], axis=2)
    out[..., :3] = np.where(a > 0, out[..., :3], 0)
    save_px(out, os.path.join(OUT, "menu_rover.png"))
    print("ROVER faces", "right" if faces_right else "right (camera flipped)")

bpy.context.window.scene = D.scenes["Scene"] if bpy.context.window else None
bpy.ops.wm.save_mainfile()
print("EXTRAS TIMES", times)
