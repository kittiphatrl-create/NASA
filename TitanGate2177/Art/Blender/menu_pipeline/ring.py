# Docking-ring main menu art in the LAST DOCK logo metal (same ramps, bevel tones, Standard view, faint warm glow).
#   menu_ring.png  1600x1600  rotating docking ring (12-fold symmetric, lighting depends only on facing + radial normal)
#   menu_clamp.png  320x320   static clamp at 3 o'clock, authored at the ring's pixel scale (800 px per ring half-size)
#   menu_ring.json           radii / clamp anchor + tip pixels
# blender -b Art/Blender/mars_keyart_menu.blend --python Art/Blender/menu_pipeline/ring.py
import bpy, bmesh, sys, os, math, json, time
import numpy as np
from mathutils import Vector, Matrix
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from nb import NB, srgb

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
OUT = os.path.join(ROOT, "Art", "Blender", "renders", "menu")
os.makedirs(OUT, exist_ok=True)
D = bpy.data
assert D.filepath.lower().endswith("mars_keyart_menu.blend"), D.filepath
TAU = 2 * math.pi

# ---- geometry (units: ring half-size = 1.0 = 800 px)
SIZE = 1600
R_OUT, R_BAND_IN = 0.970, 0.905          # main band
H_BAND = 0.030
R_PLATE_IN, H_PLATE = 0.868, 0.012       # recessed tick plate between band and inner rim
R_BEAD, BEAD_R = 0.858, 0.0095           # inner thin rim (round bead)
N_LATCH, N_TICK, LONG_EVERY = 12, 72, 6
TICK_OUT, TICK_SHORT, TICK_LONG = 0.901, 0.011, 0.023
CLAMP_PX, CLAMP_CX = 320, 1.06           # clamp canvas: 0.4 units wide, centred at x=1.06, y=0

LOGO_FRONT = [(0.0, "#6E2C0E"), (0.16, "#A8501C"), (0.42, "#F0A030"), (0.70, "#F9D58E"), (1.0, "#FFF4E0")]
LOGO_BEVEL = [(0.0, "#3C1606"), (0.5, "#C87A26"), (1.0, "#FFF8EC")]


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
    s.view_settings.view_transform = "Standard"     # same as menu_logo
    s.view_settings.look = "None"
    s.render.dither_intensity = 0.0
    s.render.use_compositing = False
    st = s.render.image_settings
    st.file_format, st.color_mode, st.color_depth = "PNG", "RGBA", "8"
    st.compression = 90
    wd = D.worlds.new(name + "World")
    s.world = wd
    return s


def ramp_node(B, fac, stops):
    n = B.node("ShaderNodeValToRGB")
    B.set(n.inputs[0], fac)
    cr = n.color_ramp
    cr.interpolation = "EASE"
    cr.elements[0].position, cr.elements[0].color = stops[0][0], (*srgb(stops[0][1]), 1)
    cr.elements[1].position, cr.elements[1].color = stops[-1][0], (*srgb(stops[-1][1]), 1)
    for p, h in stops[1:-1]:
        e = cr.elements.new(p)
        e.color = (*srgb(h), 1)
    return n.outputs["Color"]


def metal(name, lo, hi, t_edge, t_center, axis="radial"):
    """Logo metal. Front faces: logo gradient across the part's own profile (radial or y); bevels: logo bevel ramp
    driven by facing (n.z) and the RADIAL normal component only -> no screen-space light direction."""
    m = D.materials.new(name)
    nt = m.node_tree if m.node_tree else None
    if nt is None:
        m.use_nodes = True
        nt = m.node_tree
    nt.nodes.clear()
    B = NB(nt)
    geo = B.node("ShaderNodeNewGeometry")
    px, py, pz = B.sep(geo.outputs["Position"])
    nx, ny, nz = B.sep(geo.outputs["Normal"])
    pxy = B.vmath("MULTIPLY", geo.outputs["Position"], (1.0, 1.0, 0.0))
    c = B.vmath("LENGTH", pxy) if axis == "radial" else py
    t = B.maprange(c, lo, hi, 0.0, 1.0)
    v = B.math("SUBTRACT", 1.0, B.math("ABSOLUTE", B.math("SUBTRACT", B.math("MULTIPLY", t, 2.0), 1.0)))
    vs = B.maprange(v, 0.0, 1.0, 0.0, 1.0, "SMOOTHSTEP")
    ft = B.math("ADD", t_edge, B.math("MULTIPLY", vs, t_center - t_edge))
    front_col = ramp_node(B, ft, LOGO_FRONT)
    nr = B.vmath("DOT_PRODUCT", B.vmath("NORMALIZE", pxy), geo.outputs["Normal"])
    front = B.maprange(nz, 0.82, 0.97, 0.0, 1.0, "SMOOTHSTEP")
    bu = B.math("ADD", B.math("ADD", 0.25, B.math("MULTIPLY", nz, 0.55)), B.math("MULTIPLY", nr, 0.25), clamp=True)
    bev = ramp_node(B, bu, LOGO_BEVEL)
    bev_col = B.mix(0.55, B.mix(0.35, bev, front_col, "MULTIPLY"), bev)      # same blend as the logo bevel
    col = B.mix(front, bev_col, front_col)
    em = B.node("ShaderNodeEmission")
    B.set(em.inputs["Color"], col)
    out = B.node("ShaderNodeOutputMaterial")
    nt.links.new(em.outputs[0], out.inputs["Surface"])
    return m


def flat_mat(name, build):
    m = D.materials.new(name)
    if not m.node_tree:
        m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    B = NB(nt)
    em = B.node("ShaderNodeEmission")
    B.set(em.inputs["Color"], build(B))
    out = B.node("ShaderNodeOutputMaterial")
    nt.links.new(em.outputs[0], out.inputs["Surface"])
    return m


def link(s, o):
    for c in list(o.users_collection):
        c.objects.unlink(o)
    s.collection.objects.link(o)
    return o


def mesh_obj(s, name, bm, mat, smooth=False):
    me = D.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = smooth
    me.materials.append(mat)
    o = D.objects.new(name, me)
    s.collection.objects.link(o)
    return o


def fillet(cx, cz, rad, a0, a1, n=8):
    return [(cx + rad * math.cos(a0 + (a1 - a0) * i / n), cz + rad * math.sin(a0 + (a1 - a0) * i / n)) for i in range(n + 1)]


def lathe(s, name, prof, mat, segs=720):
    bm = bmesh.new()
    vs = [bm.verts.new((r, 0.0, z)) for r, z in prof]
    es = [bm.edges.new((vs[i], vs[i + 1])) for i in range(len(vs) - 1)]
    bmesh.ops.spin(bm, geom=vs + es, cent=(0, 0, 0), axis=(0, 0, 1), angle=TAU, steps=segs, use_merge=True)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
    bm.normal_update()
    top = max(bm.faces, key=lambda f: f.calc_center_median().z)
    if top.normal.z < 0:
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
    return mesh_obj(s, name, bm, mat, smooth=True)


def box(s, name, size, loc, rot_z, mat, bevel=0.0, segs=5):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=size, verts=bm.verts)
    o = mesh_obj(s, name, bm, mat)
    o.location = loc
    o.rotation_euler = (0, 0, rot_z)
    if bevel > 0:
        md = o.modifiers.new("bev", "BEVEL")
        md.width, md.segments, md.limit_method = bevel, segs, "NONE"
    return o


def bolt(s, name, loc, r, mat):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=24, v_segments=12, radius=r)
    bmesh.ops.scale(bm, vec=(1, 1, 0.45), verts=bm.verts)
    o = mesh_obj(s, name, bm, mat, smooth=True)
    o.location = loc
    return o


def ortho_cam(s, scale, cx, cy):
    cd = D.cameras.new(s.name + "Cam")
    cd.type, cd.ortho_scale, cd.sensor_fit = "ORTHO", scale, "HORIZONTAL"
    cd.clip_start, cd.clip_end = 0.01, 100
    cam = D.objects.new(s.name + "Cam", cd)
    s.collection.objects.link(cam)
    cam.location = (cx, cy, 10.0)
    s.camera = cam
    return cam


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
    im.filepath_raw, im.file_format = path, "PNG"
    im.save()
    D.images.remove(im)


def gblur(a, sigma):
    r = int(sigma * 3)
    k = np.exp(-0.5 * (np.arange(-r, r + 1) / sigma) ** 2)
    k /= k.sum()
    p = np.pad(a, r)
    p = np.apply_along_axis(lambda m: np.convolve(m, k, mode="valid"), 0, p)
    return np.apply_along_axis(lambda m: np.convolve(m, k, mode="valid"), 1, p)


def to_srgb(c):
    c = np.asarray(c, np.float32)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def logo_glow(px, mask=None):
    """menu_logo glow: blur(9)*.55 + blur(22)*.45, 30 % alpha, #F0A030, under the art."""
    a = px[..., 3]
    ga = np.clip((gblur(a, 9.0) * 0.55 + gblur(a, 22.0) * 0.45) * 0.30, 0, 1)
    if mask is not None:
        ga *= mask
    gcol = to_srgb(srgb("#F0A030"))
    out_a = a + ga * (1 - a)
    pm = px[..., :3] * a[..., None] + gcol * ga[..., None] * (1 - a[..., None])
    out = np.concatenate([pm / np.maximum(out_a[..., None], 1e-6), out_a[..., None]], axis=2)
    out[..., :3] = np.where(out_a[..., None] > 0, out[..., :3], 0)
    return out


def render(s, path):
    s.render.filepath = path
    t0 = time.time()
    bpy.ops.render.render(write_still=True, scene=s.name)
    return round(time.time() - t0, 1)


times = {}
# =================================================================== materials (shared by ring + clamp)
m_band = metal("ring_band", R_BAND_IN, R_OUT, 0.32, 0.74)
m_plate = metal("ring_plate", R_PLATE_IN, R_BAND_IN, 0.19, 0.26)
m_bead = metal("ring_bead", R_BEAD - BEAD_R, R_BEAD + BEAD_R, 0.40, 0.80)
m_latch = metal("ring_latch", 0.9115, 0.9635, 0.36, 0.70)
m_bolt = metal("ring_bolt", 0.0, 1.0, 0.80, 0.80)
m_groove = flat_mat("ring_groove", lambda B: B.rgb(srgb("#3A1406")))

# =================================================================== RING
s = new_scene("Ring", SIZE, SIZE)
f = 0.009
band_prof = [(R_BAND_IN, -0.01), (R_BAND_IN, H_BAND - f)] + fillet(R_BAND_IN + f, H_BAND - f, f, math.pi, math.pi / 2)[1:] \
    + fillet(R_OUT - f, H_BAND - f, f, math.pi / 2, 0.0) + [(R_OUT, -0.01)]
lathe(s, "ring_band", band_prof, m_band)
fp = 0.004
plate_prof = [(R_PLATE_IN, -0.01), (R_PLATE_IN, H_PLATE - fp)] + fillet(R_PLATE_IN + fp, H_PLATE - fp, fp, math.pi, math.pi / 2)[1:] \
    + [(R_BAND_IN + 0.001, H_PLATE)]
lathe(s, "ring_plate", plate_prof, m_plate)
bead_prof = fillet(R_BEAD, H_PLATE, BEAD_R, math.pi, 0.0, n=16)
lathe(s, "ring_bead", bead_prof, m_bead)
R_MID = (R_BAND_IN + R_OUT) / 2
for k in range(N_LATCH):
    a = TAU * k / N_LATCH
    cx, cy = R_MID * math.cos(a), R_MID * math.sin(a)
    box(s, f"latch{k}", (0.050, 0.058, 0.030), (cx, cy, H_BAND + 0.008), a, m_latch, bevel=0.0068)
    bolt(s, f"latch_bolt{k}", ((R_MID + 0.004) * math.cos(a), (R_MID + 0.004) * math.sin(a), H_BAND + 0.0235), 0.0062, m_bolt)
    b = a + TAU / N_LATCH / 2
    bolt(s, f"band_bolt{k}", (R_MID * math.cos(b), R_MID * math.sin(b), H_BAND), 0.0066, m_bolt)
for k in range(N_TICK):
    a = TAU * k / N_TICK
    long_ = k % LONG_EVERY == 0
    ln, wd = (TICK_LONG, 0.0046) if long_ else (TICK_SHORT, 0.0030)
    rc = TICK_OUT - ln / 2
    box(s, f"tick{k}", (ln, wd, 0.004), (rc * math.cos(a), rc * math.sin(a), H_PLATE - 0.0014), a, m_groove)
ortho_cam(s, 2.0, 0.0, 0.0)
raw = os.path.join(OUT, "_work", "ring_raw.png")
os.makedirs(os.path.dirname(raw), exist_ok=True)
times["ring"] = render(s, raw)
px = load_px(raw)
yy, xx = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32)
rr = np.hypot(xx + 0.5 - SIZE / 2, yy + 0.5 - SIZE / 2) / (SIZE / 2)
mask = np.clip((0.995 - rr) / 0.02, 0, 1) * np.clip((rr - 0.78) / 0.04, 0, 1)   # glow fades before the canvas edge; centre stays empty
ring = logo_glow(px, mask)
ring[rr < 0.78] = 0.0
save_px(ring, os.path.join(OUT, "menu_ring.png"))
print("RING alpha>0 inside r<0.80:", int((ring[..., 3][rr < 0.80] > 0).sum()), "max alpha at canvas edge:", float(ring[..., 3][rr > 0.999].max()))

# =================================================================== CLAMP (static, 3 o'clock, same world units as the ring)
s = new_scene("Clamp", CLAMP_PX, CLAMP_PX)
m_clamp = metal("clamp_body", -0.075, 0.075, 0.34, 0.78, axis="y")
m_arm = metal("clamp_arm", -0.075, 0.075, 0.38, 0.72, axis="y")
ZC = H_BAND + 0.004                                          # sits just above the band face
box(s, "clamp_spine", (0.040, 0.150, 0.040), (0.996, 0.0, ZC), 0.0, m_clamp, bevel=0.007)
for sgn in (1, -1):
    box(s, f"clamp_arm{sgn}", (0.074, 0.026, 0.020), (0.951, sgn * 0.0575, ZC + 0.012), 0.0, m_arm, bevel=0.0065)
    box(s, f"clamp_lip{sgn}", (0.018, 0.034, 0.024), (0.921, sgn * 0.0535, ZC + 0.012), 0.0, m_arm, bevel=0.006)
    bolt(s, f"clamp_bolt{sgn}", (0.996, sgn * 0.052, ZC + 0.020), 0.0068, m_bolt)
# pointer: tapered blade ending in a tip pointing right (toward the selected item)
bm = bmesh.new()
pts = [(1.010, -0.046), (1.120, -0.030), (1.188, 0.0), (1.120, 0.030), (1.010, 0.046)]
vs = [bm.verts.new((x, y, 0.0)) for x, y in pts]
fc = bm.faces.new(vs)
ext = bmesh.ops.extrude_face_region(bm, geom=[fc])
for v in [e for e in ext["geom"] if isinstance(e, bmesh.types.BMVert)]:
    v.co.z = 0.034
bm.normal_update()
if max(bm.faces, key=lambda f: f.calc_center_median().z).normal.z < 0:
    bmesh.ops.reverse_faces(bm, faces=bm.faces)
blade = mesh_obj(s, "clamp_blade", bm, m_clamp)
blade.location.z = ZC - 0.017
md = blade.modifiers.new("bev", "BEVEL")
md.width, md.segments, md.limit_method = 0.008, 6, "NONE"
# amber indicator light in a dark bezel
LIGHT = (1.066, 0.0)
bez_mat = flat_mat("clamp_bezel", lambda B: B.rgb(srgb("#2A1206")))
bpy.ops.mesh.primitive_torus_add(major_radius=0.0125, minor_radius=0.0038, major_segments=32, minor_segments=8, location=(*LIGHT, ZC + 0.020))
bez = link(s, bpy.context.active_object)
bez.name = "clamp_bezel"
bez.data.materials.append(bez_mat)
lamp_mat = flat_mat("clamp_lamp", lambda B: B.mix(
    B.maprange(B.sep(B.node("ShaderNodeNewGeometry").outputs["Normal"])[2], 0.55, 0.98, 0.0, 1.0, "SMOOTHSTEP"),
    srgb("#FF8A28"), srgb("#FFE2B0")))
lamp = bolt(s, "clamp_lamp", (*LIGHT, ZC + 0.021), 0.0095, lamp_mat)
cam = ortho_cam(s, CLAMP_PX / 800.0, CLAMP_CX, 0.0)
raw = os.path.join(OUT, "_work", "clamp_raw.png")
times["clamp"] = render(s, raw)
cpx = load_px(raw)


def to_px(x, y):  # world -> clamp image pixel (top-left origin)
    return (round((x - (CLAMP_CX - 0.2)) * 800.0, 1), round((0.2 - y) * 800.0, 1))


# warm halo on the indicator (premultiplied add, then the logo glow over everything)
lx, ly = to_px(*LIGHT)
yy, xx = np.mgrid[0:CLAMP_PX, 0:CLAMP_PX].astype(np.float32)
d2 = (xx + 0.5 - lx) ** 2 + ((CLAMP_PX - 1 - yy) + 0.5 - ly) ** 2
halo = 0.55 * np.exp(-d2 / (2 * 7.0 ** 2))
amber = to_srgb(srgb("#FFA040"))
a0 = cpx[..., 3]
pm = cpx[..., :3] * a0[..., None] + amber * halo[..., None]
a1 = np.clip(a0 + halo * (1 - a0), 0, 1)
cpx = np.concatenate([np.clip(pm / np.maximum(a1[..., None], 1e-6), 0, 1), a1[..., None]], axis=2)
clamp = logo_glow(cpx)
save_px(clamp, os.path.join(OUT, "menu_clamp.png"))
ys_, xs_ = np.nonzero(clamp[::-1][..., 3] > 0.5)
dg = s.view_layers[0].depsgraph
s.view_layers[0].update()
tip_x = max((blade.matrix_world @ v.co).x for v in blade.evaluated_get(dg).to_mesh().vertices)
anchor = to_px(R_OUT, 0.0)
tip = to_px(tip_x, 0.0)
light_px = to_px(*LIGHT)
print("CLAMP opaque bbox x", xs_.min(), xs_.max(), "y", ys_.min(), ys_.max(), "anchor", anchor, "tip", tip, "light", light_px)

data = {
    "ring": {"file": "menu_ring.png", "width": SIZE, "height": SIZE, "center_px": [SIZE / 2, SIZE / 2],
             "units": "radii are fractions of the half-size (800 px)",
             "outer_radius": R_OUT, "band_outer": R_OUT, "band_inner": R_BAND_IN,
             "tick_outer": TICK_OUT, "tick_inner_short": round(TICK_OUT - TICK_SHORT, 4), "tick_inner_long": round(TICK_OUT - TICK_LONG, 4),
             "tick_radius": round(TICK_OUT - TICK_LONG / 2, 4), "tick_count": N_TICK, "long_tick_every": LONG_EVERY,
             "inner_rim_outer": round(R_BEAD + BEAD_R, 4), "inner_rim_inner": round(R_BEAD - BEAD_R, 4),
             "transparent_inside": round(R_BEAD - BEAD_R, 4),
             "latch_count": N_LATCH, "latch_angles_deg": [round(360 * k / N_LATCH, 1) for k in range(N_LATCH)],
             "angle_convention": "0 deg = 3 o'clock, counter-clockwise on screen; latches and long ticks share these angles (12-fold symmetric, step 30 deg)"},
    "clamp": {"file": "menu_clamp.png", "width": CLAMP_PX, "height": CLAMP_PX, "origin": "top-left pixels",
              "anchor_px": list(anchor), "tip_px": list(tip), "indicator_px": list(light_px),
              "anchor_note": "anchor = point on the ring's outer rim at 3 o'clock; the clamp is authored at the ring's scale, so place it with anchor_px on ring pixel (1576, 800) and scale both by the same factor",
              "ring_px_for_anchor": [SIZE / 2 + R_OUT * SIZE / 2, SIZE / 2]},
    "style": "LAST DOCK logo metal: cream #FFF4E0 -> gold #F0A030 -> copper, Standard view transform (as menu_logo), glow #F0A030 at 30 % (sigma 9/22 px)",
}
with open(os.path.join(OUT, "menu_ring.json"), "w", encoding="utf-8") as fjs:
    json.dump(data, fjs, indent=1)
bpy.ops.wm.save_mainfile()
print("RING TIMES", times)
