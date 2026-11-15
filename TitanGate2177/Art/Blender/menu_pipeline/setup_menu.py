# Builds Art/Blender/mars_keyart_menu.blend from the friend's mars_keyart.blend.
# Run:  blender -b mars_keyart.blend --python setup_menu.py
# The source file is only read; the first save is a Save As to the menu copy.
import bpy, math, os, sys
from mathutils import Vector, Matrix, Euler
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from nb import NB, srgb

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))   # project root
SRC = os.path.join(ROOT, r"Art\Blender\mars_keyart.blend")
VARIANT = os.environ.get("MENU_VARIANT", "menu")      # "menu" (default key art) or "sunset" (save-exists menu scene)
SUNSET = VARIANT == "sunset"
DST = os.path.join(ROOT, r"Art\Blender\mars_keyart_sunset.blend" if SUNSET else r"Art\Blender\mars_keyart_menu.blend")
assert os.path.normcase(os.path.abspath(bpy.data.filepath)) == os.path.normcase(SRC), bpy.data.filepath
bpy.ops.wm.save_as_mainfile(filepath=DST)          # first save = Save As to the copy
assert os.path.normcase(bpy.data.filepath) == os.path.normcase(DST)

sc = bpy.context.scene
D = bpy.data
rad = math.radians

# ------------------------------------------------------------------ config
CAM_XY = (-4.2, -6.2)
CAM_H = 0.80                    # camera height above the cadet's ground level (low hero shot)
CADET_XY = (1.4, 6.2)           # cadet moved ~3 m forward onto flatter ground
CADET_SCREEN_X = 0.672          # right third
LENS = 50.0                     # 36 mm sensor -> 39.6 deg horizontal FOV
PITCH = 3.4                     # degrees up
ROLL = -1.4                     # dutch tilt
SUN_REL_AZ = 12.5               # sun disc: degrees right of view centre
SUN_DISC_ELEV = 9.4
LIGHT_ELEV = 17.0               # the key light sits a little higher than the disc (shorter shadows)
LIGHT_REL_AZ = 28.0             # ...and a little further right than the disc
RIM_REL_AZ, RIM_ELEV = 78.0, 24.0   # dedicated warm rim direction (right side, slightly behind)
CADET_FACE_LEFT = 8.0           # cadet faces away from camera, turned this many degrees to the left
FSTOP = 2.4

MOVES = {}                       # name -> (dx, dy, dz) applied to unparented objects
GROUPS = {
    "hab_a": ["hab_a", "hab_a_cap", "hab_a_cap.001", "hab_a_win"],
    "hab_c": ["hab_c", "hab_c_cap", "hab_c_cap.001", "hab_c_win"],
    "mast": ["Cylinder.004", "beacon"],
    "hab_b": ["hab_b", "hab_b_cap", "hab_b_cap.001", "hab_b_win"],
    "solar": ["solar0", "solar1", "solar2", "solar3", "Cylinder", "Cylinder.001", "Cylinder.002", "Cylinder.003"],
    "lifeboat": ["lifeboat", "porthole"],
    "dome": ["dome"],
    "rover": [o.name for o in D.collections["rover"].objects],
}
BASE_SHIFT = (5.0, 8.0)          # the whole base sits a little further back and right
GROUP_MOVES = {
    "hab_a": (0.0, 0.0, 0.0),
    "hab_c": (0.0, 0.0, 0.0),
    "mast": (0.0, 0.0, 0.0),
    "hab_b": (13.5, 12.0, 0.0),
    "dome": (5.5, 6.0, 0.0),
    "solar": (20.0, -1.0, 0.0),
    "lifeboat": (-13.0, 1.0, 0.0),
    "rover": (-14.0, 0.0, 0.0),
}

# palette (linear)
for _g in ("hab_a", "hab_b", "hab_c", "mast", "dome", "solar"):
    _m = GROUP_MOVES[_g]
    GROUP_MOVES[_g] = (_m[0] + BASE_SHIFT[0], _m[1] + BASE_SHIFT[1], 0.0)
HIDE = ["rock24", "rock34"]       # dark blobs at the cadet's feet / centre of the plain

HAZE_FAR = srgb("#6E4232")
HAZE_SUN = srgb("#E2A06A", 1.05)
FILL_COL = srgb("#2A4258")
RIM_COL = srgb("#FFB878", 2.7)
FOG_START, FOG_SCALE, FOG_MAX, HAZE_EXP = 12.0, 95.0, 0.86, 16.0
FILL_K = 0.26
CADET_SHADOW = 0.80
SHEEN_LO, SHEEN_HI, SHEEN_MIN, SHEEN_MAX = 0.84, 0.99, 0.50, 1.10
BOUNCE_COL = srgb("#7A3418")
BOUNCE_K = 0.35
CLOUD_W, CLOUD_H = 2.7, 2.6
CLOUD_ANG, CLOUD_R, CLOUD_Z = 15.0, 10.0, 10.0   # degrees left of view centre, metres out, blocker height
SUN_R_DEG = 0.62
BLUE_COL, BLUE_EXP, BLUE_K = (0.0, 0.0, 0.0), 300.0, 0.0       # blue forward-scatter in the haze (sunset only)
SUN_ENERGY, SUN_COLOR, EXPOSURE = 3.4, (1.0, 0.80, 0.58), -0.25
if SUNSET:
    # Mars blue sunset: small sun just above the ridge on the right, blue aureole, butterscotch -> dusky violet sky
    SUN_REL_AZ, SUN_DISC_ELEV = 14.5, 5.6
    LIGHT_REL_AZ, LIGHT_ELEV = 10.0, 27.0          # key light at the sun azimuth (cheated higher) -> compact cadet shadow toward camera
    RIM_REL_AZ, RIM_ELEV = 62.0, 18.0
    SUN_R_DEG = 0.62 * 2.0 / 3.0                   # ~2/3 of the Earth-like disc used in the day menu
    HAZE_FAR = srgb("#4E3A3C")
    HAZE_SUN = srgb("#A87A52", 1.0)
    FOG_MAX = 0.80
    HAZE_EXP = 10.0
    RIM_COL = srgb("#9CC4FF", 2.3)                 # cool blue rim from the aureole
    BOUNCE_COL = srgb("#8A4420")
    BOUNCE_K = 0.55                                # warm ground bounce
    BLUE_COL, BLUE_EXP, BLUE_K = srgb("#8FB4E6"), 260.0, 0.45
    SUN_ENERGY, SUN_COLOR, EXPOSURE = 2.15, (1.0, 0.80, 0.62), -0.72
    SHEEN_MAX = 0.95
    GROUP_MOVES["lifeboat"] = (15.1, 10.4, 0.0)    # the escape pod, landed beside the habitats
    SHEEN_MIN = 0.55


def dir_from(az, el):
    return Vector((math.cos(el) * math.cos(az), math.cos(el) * math.sin(az), math.sin(el)))


# ------------------------------------------------------------------ layout
def ground_z(x, y):
    dg = bpy.context.evaluated_depsgraph_get()
    o = Vector((x, y, 60.0))
    for _ in range(8):
        hit, loc, nor, idx, ob, mat = sc.ray_cast(dg, o, Vector((0, 0, -1)))
        if not hit:
            return 0.0
        if ob.name == "ground":
            return loc.z
        o = loc + Vector((0, 0, -0.01))
    return 0.0


def group_sink(objs):
    dg = bpy.context.evaluated_depsgraph_get()
    zmin, gmin = 1e9, 1e9
    for o in objs:
        if o.type == "MESH":
            oe = o.evaluated_get(dg)
            me = oe.to_mesh()
            if len(me.vertices):
                zmin = min(zmin, min((o.matrix_world @ v.co).z for v in me.vertices))
            oe.to_mesh_clear()
        t = o.matrix_world.translation
        gmin = min(gmin, ground_z(t.x, t.y))
    return zmin - gmin


bpy.context.view_layer.update()
for g, names in GROUPS.items():
    off = Vector(GROUP_MOVES.get(g, (0, 0, 0)))
    if off.length == 0:
        continue
    objs = [D.objects[n] for n in names if D.objects.get(n) and D.objects[n].parent is None]
    hidden = []
    for o in objs:                      # keep the group out of its own ground probes
        hidden.append((o, o.hide_viewport))
    sink0 = group_sink(objs)
    for o in objs:
        o.location += off
    bpy.context.view_layer.update()
    sink1 = group_sink(objs)
    for o in objs:
        o.location.z += sink0 - sink1
    bpy.context.view_layer.update()
    print("moved", g, "sink", round(sink0, 2), "->", round(group_sink(objs), 2))
for n, off in MOVES.items():
    D.objects[n].location += Vector(off)

root = D.objects["Root"]
root.location = Vector((CADET_XY[0], CADET_XY[1], ground_z(*CADET_XY) - 0.03))
CAM_POS = Vector((CAM_XY[0], CAM_XY[1], root.location.z + CAM_H))
print("cam ground", round(ground_z(*CAM_XY), 2), "cam z", round(CAM_POS.z, 2), "cadet ground", round(root.location.z, 2))
_cv = root.location - CAM_POS
CADET_AZ = math.degrees(math.atan2(_cv.y, _cv.x))
HFOV_T = 18.0 / LENS
YAW = (90.0 - CADET_AZ) - math.degrees(math.atan((CADET_SCREEN_X - 0.5) * 2 * HFOV_T))
CADET_ROT = CADET_AZ + CADET_FACE_LEFT + 90.0
root.rotation_euler[2] = rad(CADET_ROT)
print("cadet rot", round(CADET_ROT, 1), "yaw", round(YAW, 2))
for _n in HIDE:
    D.objects[_n].hide_render = True
D.objects["sun_disc"].hide_render = True          # the sun now lives in the sky shader, in the right place
D.objects["sun_disc"].hide_viewport = True
bc = D.objects["beacon"]
bc.scale = (0.5, 0.5, 0.5)
mast = D.objects["Cylinder.004"]
mast.scale = (mast.scale[0] * 2.2, mast.scale[1] * 2.2, mast.scale[2])
# the original mast stops ~2.5 m short of its beacon (that is why it read as a faint second sun): extend it to meet it
bpy.context.view_layer.update()
_mz0 = mast.location.z - mast.dimensions.z / 2
_top = bc.matrix_world.translation.z - 0.10
_len = _top - _mz0
mast.scale.z *= _len / mast.dimensions.z
mast.location.z = _mz0 + _len / 2
print("mast", round(_mz0, 2), "->", round(_top, 2))
bpy.context.view_layer.update()

VIEW_AZ = 90.0 - YAW
SUN_AZ = VIEW_AZ - SUN_REL_AZ
SUN_DISC = dir_from(rad(SUN_AZ), rad(SUN_DISC_ELEV))
SUN_LIGHT = dir_from(rad(VIEW_AZ - LIGHT_REL_AZ), rad(LIGHT_ELEV))
RIM_DIR = dir_from(rad(VIEW_AZ - RIM_REL_AZ), rad(RIM_ELEV))


# ------------------------------------------------------------------ camera
cam = D.objects["KeyCam"]
cam.location = CAM_POS
cam.rotation_mode = "XYZ"
cam.rotation_euler = Euler((rad(90 + PITCH), rad(ROLL), rad(-YAW)), "XYZ")
cam.data.lens = LENS
cam.data.sensor_width = 36.0
cam.data.sensor_fit = "HORIZONTAL"
cam.data.clip_start = 0.1
cam.data.clip_end = 1500.0
cam.data.dof.use_dof = True
cam.data.dof.focus_object = D.objects["Body"]
cam.data.dof.aperture_fstop = FSTOP
cam.data.dof.aperture_blades = 0

# the friend's rectangular windows sit partly inside the displaced hull (ragged top edge): pull them out a little
for _n in ("hab_a_win", "hab_b_win", "hab_c_win"):
    _o = D.objects[_n]
    _d = CAM_POS - _o.location
    _d.z = 0.0
    _o.location += _d.normalized() * 0.12

# ------------------------------------------------------------------ key light
sun = D.objects["Sun"]
sun.rotation_mode = "QUATERNION"
sun.rotation_quaternion = (-SUN_LIGHT).to_track_quat("-Z", "Y")
sun.data.energy = SUN_ENERGY
sun.data.color = SUN_COLOR
sun.data.angle = rad(3.0)
sun.data.use_shadow = True
try:
    sun.data.use_shadow_jitter = True
except Exception:
    pass

# ------------------------------------------------------------------ look group (fill, rim, aerial haze)
look = D.node_groups.new("MenuLook", "ShaderNodeTree")
for nm, st, dv in (("Color", "NodeSocketColor", None), ("Light", "NodeSocketFloat", 0.5), ("Rim", "NodeSocketFloat", 0.0), ("Fog", "NodeSocketFloat", 1.0), ("Sheen", "NodeSocketFloat", 0.0)):
    s = look.interface.new_socket(nm, in_out="INPUT", socket_type=st)
    if dv is not None:
        s.default_value = dv
look.interface.new_socket("Color", in_out="OUTPUT", socket_type="NodeSocketColor")
B = NB(look)
gi = B.node("NodeGroupInput")
go = B.node("NodeGroupOutput")
geo = B.node("ShaderNodeNewGeometry")
camd = B.node("ShaderNodeCameraData")
# cool sky fill in the shadow band
fill_mask = B.maprange(gi.outputs["Light"], 0.04, 0.32, 1.0, 0.0, "SMOOTHSTEP")
c1 = B.mix(B.math("MULTIPLY", fill_mask, FILL_K), gi.outputs["Color"], FILL_COL, "ADD")
# warm rim on the silhouette edge facing the sun
ndl = B.vmath("DOT_PRODUCT", geo.outputs["Normal"], B.vec(RIM_DIR))
ndl_m = B.maprange(ndl, 0.05, 0.55, 0.0, 1.0, "SMOOTHSTEP")
lw = B.node("ShaderNodeLayerWeight")
lw.inputs["Blend"].default_value = 0.5
edge = B.maprange(lw.outputs["Facing"], 0.22, 0.62, 0.0, 1.0, "SMOOTHSTEP")
rim = B.math("MULTIPLY", B.math("MULTIPLY", ndl_m, edge), gi.outputs["Rim"])
c2 = B.mix(rim, c1, RIM_COL, "ADD")
down = B.maprange(B.sep(geo.outputs["Normal"])[2], 0.15, -0.6, 0.0, 1.0, "SMOOTHSTEP")
bounce = B.math("MULTIPLY", B.math("MULTIPLY", down, fill_mask), BOUNCE_K)
c2 = B.mix(bounce, c2, BOUNCE_COL, "ADD")
# dust forward-scatter on the ground: brighter looking toward the sun, darker looking away (calms the left side)
vflat = B.vmath("NORMALIZE", B.vmath("MULTIPLY", B.vmath("SCALE", geo.outputs["Incoming"], scale=-1.0), (1.0, 1.0, 0.0)))
sflat = Vector((SUN_DISC.x, SUN_DISC.y, 0.0)).normalized()
fwd = B.maprange(B.vmath("DOT_PRODUCT", vflat, B.vec(sflat)), SHEEN_LO, SHEEN_HI, SHEEN_MIN, SHEEN_MAX, "SMOOTHSTEP")
sheen_f = B.math("ADD", 1.0, B.math("MULTIPLY", B.math("SUBTRACT", fwd, 1.0), gi.outputs["Sheen"]))
c2 = B._scale_sock(c2, sheen_f)
# aerial haze: distance based, a little thinner with height, brighter toward the sun
dd = B.math("MAXIMUM", B.math("SUBTRACT", camd.outputs["View Distance"], FOG_START), 0.0)
fd = B.math("SUBTRACT", 1.0, B.math("EXPONENT", B.math("MULTIPLY", dd, -1.0 / FOG_SCALE)))
hz = B.sep(geo.outputs["Position"])[2]
hm = B.maprange(hz, 0.0, 14.0, 1.0, 0.62)
f = B.math("MULTIPLY", B.math("MULTIPLY", fd, hm), B.math("MULTIPLY", gi.outputs["Fog"], FOG_MAX), clamp=True)
vdir = B.vmath("SCALE", geo.outputs["Incoming"], scale=-1.0)
sdot = B.math("POWER", B.math("MAXIMUM", B.vmath("DOT_PRODUCT", vdir, B.vec(SUN_DISC)), 0.0), HAZE_EXP)
fogc = B.mix(sdot, HAZE_FAR, HAZE_SUN)
if SUNSET:
    sblue = B.math("POWER", B.math("MAXIMUM", B.vmath("DOT_PRODUCT", vdir, B.vec(SUN_DISC)), 0.0), BLUE_EXP)
    fogc = B.mix(B.math("MULTIPLY", sblue, BLUE_K), fogc, BLUE_COL, "MIX")
c3 = B.mix(f, c2, fogc)
look.links.new(c3, go.inputs["Color"])

CADET_MATS = {"suit", "suitD", "skin", "hair", "cream", "boot", "belt", "ink", "patch"}
RIM = {m: 1.0 for m in CADET_MATS}
RIM.update({"module": 0.30, "lifeboat": 0.35, "roverBody.001": 0.3, "metal": 0.2, "glass": 0.2, "rock": 0.55})
if SUNSET:
    RIM.update({"rock": 0.15, "lifeboat": 0.5})


def tint(v, cool_amt=1.0):
    t = max(0.0, min(1.0, (v - 0.5) / 0.6))
    cool = tuple(1.0 + (c - 1.0) * cool_amt for c in (0.72, 0.88, 0.98))
    if cool_amt < 0:          # clay figure: rich, saturated warm shadows
        cool = (1.0, 0.80, 0.68)
    warm = (1.07, 0.97, 0.80)
    return tuple(v * (cool[i] + (warm[i] - cool[i]) * t) for i in range(3))


def retint_ramp(rn, w=0.032, shadow_mul=1.0, cool_amt=1.0):
    cr = rn.color_ramp
    src = [(e.position, e.color[0] * (shadow_mul if i == 0 else 1.0)) for i, e in enumerate(cr.elements)]
    if any(abs(e.color[0] - e.color[1]) > 1e-3 or abs(e.color[0] - e.color[2]) > 1e-3 for e in cr.elements):
        return False
    stops = [(src[0][0], tint(src[0][1], cool_amt))]
    for i in range(1, len(src)):
        p, v = src[i]
        stops.append((max(p - w, stops[-1][0] + 0.002), tint(src[i - 1][1], cool_amt)))
        stops.append((p + w, tint(v, cool_amt)))
    cr.interpolation = "LINEAR"
    while len(cr.elements) > 1:
        cr.elements.remove(cr.elements[-1])
    cr.elements[0].position = stops[0][0]
    cr.elements[0].color = (*stops[0][1], 1.0)
    for p, c in stops[1:]:
        e = cr.elements.new(p)
        e.color = (*c, 1.0)
    return True


patched = []
for mat in list(D.materials):
    nt = mat.node_tree
    if not nt:
        continue
    em = next((n for n in nt.nodes if n.type == "EMISSION"), None)
    s2r = next((n for n in nt.nodes if n.type == "SHADERTORGB"), None)
    if em is None or s2r is None:
        continue
    ln = next((l for l in nt.links if l.to_node == em and l.to_socket.name == "Color"), None)
    if ln is None:
        continue
    src = ln.from_socket
    g = nt.nodes.new("ShaderNodeGroup")
    g.node_tree = look
    g.location = (em.location.x - 200, em.location.y - 200)
    nt.links.new(src, g.inputs["Color"])
    nt.links.new(s2r.outputs["Color"], g.inputs["Light"])
    nt.links.new(g.outputs["Color"], em.inputs["Color"])
    g.inputs["Rim"].default_value = RIM.get(mat.name, 0.0)
    g.inputs["Sheen"].default_value = 1.0 if mat.name in ("sand", "sandD", "rock") else 0.0
    for l in nt.links:
        if l.from_node == s2r and l.to_node.type == "VALTORGB":
            if mat.name in CADET_MATS:
                retint_ramp(l.to_node, shadow_mul=CADET_SHADOW, cool_amt=-1.0)
            else:
                retint_ramp(l.to_node)
    patched.append(mat.name)
print("patched", patched)


def toon_base(nt, B):
    """Diffuse -> ShaderToRGB, returns the light socket."""
    dif = B.node("ShaderNodeBsdfDiffuse")
    s2r = B.node("ShaderNodeShaderToRGB")
    nt.links.new(dif.outputs[0], s2r.inputs[0])
    return s2r.outputs["Color"], dif


def finish(nt, B, color, light, rim=0.0, fog=1.0):
    g = B.node("ShaderNodeGroup")
    g.node_tree = look
    B.set(g.inputs["Color"], color)
    B.set(g.inputs["Light"], light)
    g.inputs["Rim"].default_value = rim
    g.inputs["Fog"].default_value = fog
    em = B.node("ShaderNodeEmission")
    nt.links.new(g.outputs["Color"], em.inputs["Color"])
    out = B.node("ShaderNodeOutputMaterial")
    nt.links.new(em.outputs[0], out.inputs["Surface"])


def new_mat(name):
    m = D.materials.new(name)
    try:
        m.use_nodes = True
    except Exception:
        pass
    m.node_tree.nodes.clear()
    return m, m.node_tree, NB(m.node_tree)


# ------------------------------------------------------------------ solar panels: dark cells, thin frame grid, sky sheen
m, nt, B = new_mat("solar_menu")
light, _ = toon_base(nt, B)
tc = B.node("ShaderNodeTexCoord")
gx, gy, gz = B.sep(tc.outputs["Generated"])


def grid_line(v, n, wdt):
    fr = B.math("FRACT", B.math("MULTIPLY", v, n))
    d = B.math("ABSOLUTE", B.math("SUBTRACT", fr, 0.5))
    return B.maprange(d, 0.5 - wdt * 1.6, 0.5 - wdt, 0.0, 1.0, "SMOOTHSTEP")


lines = B.math("MAXIMUM", grid_line(gx, 6.0, 0.035), grid_line(gy, 4.0, 0.05))
lw = B.node("ShaderNodeLayerWeight")
lw.inputs["Blend"].default_value = 0.35
sheen = B.maprange(lw.outputs["Fresnel"], 0.05, 0.9, 0.12, 1.0, "SMOOTHSTEP")
cell = B.mix(sheen, srgb("#0B1426"), srgb("#B9785A"))
col = B.mix(lines, cell, srgb("#8C8E96"))
lit = B.maprange(light, 0.05, 0.6, 0.7, 1.15)
col = B._scale_sock(col, lit)
finish(nt, B, col, light, rim=0.25)
for n in GROUPS["solar"]:
    o = D.objects[n]
    if o.name.startswith("solar"):
        o.data.materials.clear()
        o.data.materials.append(m)

# ------------------------------------------------------------------ dome: smoked glass greenhouse with ribs and a warm glow inside
m, nt, B = new_mat("dome_glass")
light, _ = toon_base(nt, B)
tc = B.node("ShaderNodeTexCoord")
ox, oy, oz = B.sep(tc.outputs["Generated"])
lw = B.node("ShaderNodeLayerWeight")
lw.inputs["Blend"].default_value = 0.45
refl = B.maprange(lw.outputs["Fresnel"], 0.1, 0.95, 0.0, 1.0, "SMOOTHSTEP")
glass = B.mix(refl, srgb("#1A1A20"), srgb("#B57A58"))
glow = B.maprange(oz, 0.62, 0.95, 1.0, 0.0, "SMOOTHSTEP")
glass = B.mix(B.math("MULTIPLY", glow, 0.55), glass, srgb("#FF9A48"), "ADD")
ang = B.math("ARCTAN2", B.math("SUBTRACT", oy, 0.5), B.math("SUBTRACT", ox, 0.5))
merid = grid_line(B.math("DIVIDE", ang, 2 * math.pi), 14.0, 0.03)
rings = grid_line(oz, 9.0, 0.04)
ribs = B.math("MAXIMUM", merid, rings)
rib_col = B._scale_sock(srgb("#CFC3AE"), B.maprange(light, 0.05, 0.6, 0.45, 1.1))
col = B.mix(ribs, glass, rib_col)
finish(nt, B, col, light, rim=0.3)
dome = D.objects["dome"]
dome.data.materials.clear()
dome.data.materials.append(m)
for md in dome.modifiers:
    if md.type == "DISPLACE":
        md.show_render = md.show_viewport = False

# ------------------------------------------------------------------ world: hazy Mars sky with a real sun, cool fill for lighting
w = D.worlds.new("MenuSky")
try:
    w.use_nodes = True
except Exception:
    pass
nt = w.node_tree
nt.nodes.clear()
B = NB(nt)
tc = B.node("ShaderNodeTexCoord")
d = B.vmath("NORMALIZE", tc.outputs["Generated"])
elev = B.sep(d)[2]
s = B.vmath("DOT_PRODUCT", d, B.vec(SUN_DISC))
s0 = B.math("MAXIMUM", s, 0.0)
haze = B.mix(B.math("POWER", s0, HAZE_EXP), HAZE_FAR, HAZE_SUN)
side = B.maprange(s, 0.72, 0.995, 0.0, 1.0, "SMOOTHSTEP")
e0 = B.maprange(elev, 0.0, 0.42, 0.0, 1.0)


def ramp(fac, stops):
    n = B.node("ShaderNodeValToRGB")
    B.set(n.inputs[0], fac)
    cr = n.color_ramp
    cr.interpolation = "EASE"
    cr.elements[0].position = stops[0][0]
    cr.elements[0].color = (*stops[0][1], 1)
    cr.elements[1].position = stops[1][0]
    cr.elements[1].color = (*stops[1][1], 1)
    for p, c in stops[2:]:
        e = cr.elements.new(p)
        e.color = (*c, 1)
    return n.outputs["Color"]


if SUNSET:   # butterscotch/rust near the sun's azimuth, dusky brown-violet overhead and to the left
    cool = ramp(e0, [(0.0, srgb("#5C4440")), (0.2, srgb("#423038")), (0.55, srgb("#261C2A")), (1.0, srgb("#100C18"))])
    warm = ramp(e0, [(0.0, srgb("#C49A6C")), (0.2, srgb("#8E6450")), (0.55, srgb("#4A3238")), (1.0, srgb("#1C1420"))])
else:
    cool = ramp(e0, [(0.0, srgb("#5A3428")), (0.2, srgb("#3E2626")), (0.55, srgb("#22191F")), (1.0, srgb("#0E0B12"))])
    warm = ramp(e0, [(0.0, srgb("#B07450")), (0.2, srgb("#7E4A3A")), (0.55, srgb("#4A2C2E")), (1.0, srgb("#1E1418"))])
upper = B.mix(side, cool, warm)
hb = B.maprange(elev, -0.004, 0.13, 0.0, 1.0, "SMOOTHSTEP")
sky = B.mix(hb, haze, upper)
SUN_R = rad(SUN_R_DEG)
GLOWS = ((90.0, 0.16, srgb("#FFA860")), (700.0, 0.45, srgb("#FFC088")), (6000.0, 2.0, srgb("#FFE2B8")))
if SUNSET:   # Mars: the sky right around the sun is BLUE (forward scattering by fine dust), a few degrees wide
    GLOWS = ((40.0, 0.08, srgb("#C89870")), (700.0, 0.42, srgb("#4F86D2")), (3000.0, 0.95, srgb("#86B4EE")), (16000.0, 2.4, srgb("#E6F2FF")))
for p, k, colr in GLOWS:
    sky = B.mix(B.math("MULTIPLY", B.math("POWER", s0, p), k), sky, colr, "ADD")
disc = B.maprange(s, math.cos(SUN_R * 1.10), math.cos(SUN_R * 0.93), 0.0, 1.0, "SMOOTHSTEP")
sun_rgb = B.rgb((0.94 * 24, 0.97 * 24, 1.0 * 24) if SUNSET else (1.0 * 28, 0.86 * 28, 0.66 * 28))
sky = B.mix(disc, sky, sun_rgb, "ADD")
bg_cam = B.node("ShaderNodeBackground")
B.set(bg_cam.inputs["Color"], sky)
bg_fill = B.node("ShaderNodeBackground")
bg_fill.inputs["Color"].default_value = (*FILL_COL, 1)
bg_fill.inputs["Strength"].default_value = 1.2
lp = B.node("ShaderNodeLightPath")
ms = B.node("ShaderNodeMixShader")
nt.links.new(lp.outputs["Is Camera Ray"], ms.inputs[0])
nt.links.new(bg_fill.outputs[0], ms.inputs[1])
nt.links.new(bg_cam.outputs[0], ms.inputs[2])
wo = B.node("ShaderNodeOutputWorld")
nt.links.new(ms.outputs[0], wo.inputs["Surface"])
sc.world = w

# ------------------------------------------------------------------ habitat lights: portholes, door lamps, path markers, spill
win_mat = D.materials["window"]
en = next(n for n in win_mat.node_tree.nodes if n.type == "EMISSION")
en.inputs["Strength"].default_value = 1.25
en.inputs["Color"].default_value = (1.0, 0.42, 0.12, 1.0)
lamp_mat = D.materials.new("lamp_warm")
try:
    lamp_mat.use_nodes = True
except Exception:
    pass
lnt = lamp_mat.node_tree
lnt.nodes.clear()
lb = NB(lnt)
le = lb.node("ShaderNodeEmission")
le.inputs["Color"].default_value = (1.0, 0.62, 0.30, 1)
le.inputs["Strength"].default_value = 3.5
lo = lb.node("ShaderNodeOutputMaterial")
lnt.links.new(le.outputs[0], lo.inputs["Surface"])

lights_col = D.collections.new("menu_lights")
D.collections["base"].children.link(lights_col)
LIGHT_MARKERS = []   # (name, world position, kind)


def add_mesh_obj(name, mesh_fn, loc, rot_q=None, mat=None, scale=None):
    bpy.ops.object.select_all(action="DESELECT")
    mesh_fn()
    o = bpy.context.active_object
    o.name = name
    for c in o.users_collection:
        c.objects.unlink(o)
    lights_col.objects.link(o)
    o.location = loc
    if rot_q is not None:
        o.rotation_mode = "QUATERNION"
        o.rotation_quaternion = rot_q
    if scale is not None:
        o.scale = scale
    if mat is not None:
        o.data.materials.append(mat)
    return o


bpy.context.view_layer.update()
dg = bpy.context.evaluated_depsgraph_get()


def hit_on(obj_names, target):
    """Ray from the camera toward target; return first hit on one of obj_names."""
    o = CAM_POS.copy()
    dvec = (target - o).normalized()
    for _ in range(12):
        hit, loc, nor, idx, ob, mt = sc.ray_cast(dg, o, dvec)
        if not hit:
            return None
        if ob.name in obj_names:
            return loc, nor
        o = loc + dvec * 0.01
    return None


def hab_axis(body, capA, capB):
    a = D.objects[capA].matrix_world.translation
    b = D.objects[capB].matrix_world.translation
    return a, b


HABS = {
    "hab_a": ("hab_a", "hab_a_cap", "hab_a_cap.001"),
    "hab_b": ("hab_b", "hab_b_cap", "hab_b_cap.001"),
    "hab_c": ("hab_c", "hab_c_cap", "hab_c_cap.001"),
}
PORTS = {"hab_a": [0.18, 0.80], "hab_b": [0.22, 0.75], "hab_c": [0.25]}
DOOR = {"hab_a": "hab_a_cap", "hab_c": "hab_c_cap", "hab_b": "hab_b_cap"}
port_count = 0
for hk, (body, ca, cb) in HABS.items():
    a, b = hab_axis(body, ca, cb)
    bc = D.objects[body].matrix_world.translation
    for t in PORTS.get(hk, []):
        p = a.lerp(b, t)
        target = Vector((p.x, p.y, bc.z + 0.15))
        r = hit_on({body, ca, cb}, target)
        if not r:
            print("no porthole hit", hk, t)
            continue
        loc, nor = r
        q = nor.to_track_quat("Z", "Y")
        port_count += 1
        o = add_mesh_obj(f"{hk}_port{port_count}", lambda: bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=0.19, depth=0.08), loc + nor * 0.01, q, win_mat)
        LIGHT_MARKERS.append((o.name, loc + nor * 0.05, "window"))
    # door lamp on the cap facing the camera side
    cap = D.objects[DOOR[hk]]
    cp = cap.matrix_world.translation
    target = Vector((cp.x, cp.y, cp.z + 0.9))
    r = hit_on({cap.name, body}, target)
    if r:
        loc, nor = r
        o = add_mesh_obj(f"{hk}_doorlamp", lambda: bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.09), loc + nor * 0.08, None, lamp_mat)
        LIGHT_MARKERS.append((o.name, o.location.copy(), "door"))
    # existing big window
    wo_ = D.objects.get(hk + "_win")
    if wo_:
        LIGHT_MARKERS.append((wo_.name, wo_.matrix_world.translation.copy(), "window"))
    # warm spill light in front of the module
    ld = D.lights.new(hk + "_spill", "POINT")
    ld.energy = 22.0
    ld.color = (1.0, 0.62, 0.32)
    ld.shadow_soft_size = 0.6
    ld.use_shadow = False
    lo_ = D.objects.new(hk + "_spill", ld)
    lights_col.objects.link(lo_)
    front = bc + (CAM_POS - bc).normalized() * 3.2
    lo_.location = Vector((front.x, front.y, ground_z(front.x, front.y) + 0.35))
    try:
        lo_.light_linking.receiver_collection = D.collections["terrain"]
    except Exception as e:
        print("light linking", e)

# antenna mast: darker so it reads against the bright haze (the beacon was floating like a second sun)
mm, mnt, MB = new_mat("mast_dark")
mlight, _ = toon_base(mnt, MB)
finish(mnt, MB, MB._scale_sock(srgb("#3A3436"), MB.maprange(mlight, 0.05, 0.6, 0.8, 1.3)), mlight, rim=0.6, fog=0.75)
mast.data.materials.clear()
mast.data.materials.append(mm)
# beacon: keep the red mast light, make it a little hotter for bloom
bm = D.materials["beacon"]
bn = next(n for n in bm.node_tree.nodes if n.type == "EMISSION")
bn.inputs["Strength"].default_value = 9.0
LIGHT_MARKERS.append(("beacon", D.objects["beacon"].matrix_world.translation.copy(), "beacon"))
if SUNSET:   # small blinking beacon on top of the landed escape pod
    lbm = D.materials.new("lifeboat_beacon")
    try:
        lbm.use_nodes = True
    except Exception:
        pass
    lbm.node_tree.nodes.clear()
    _lb = NB(lbm.node_tree)
    _le = _lb.node("ShaderNodeEmission")
    _le.inputs["Color"].default_value = (*srgb("#FF5A3C"), 1)
    _le.inputs["Strength"].default_value = 7.0
    lbm.node_tree.links.new(_le.outputs[0], _lb.node("ShaderNodeOutputMaterial").inputs["Surface"])
    _dg = bpy.context.evaluated_depsgraph_get()
    _lbo = D.objects["lifeboat"]
    _me = _lbo.evaluated_get(_dg).to_mesh()
    _vs = [_lbo.matrix_world @ v.co for v in _me.vertices]
    _top = max(_vs, key=lambda v: v.z)
    _cx = sum(v.x for v in _vs) / len(_vs)
    _cy = sum(v.y for v in _vs) / len(_vs)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.13, location=(_cx, _cy, _top.z + 0.08))
    _bo = bpy.context.active_object
    _bo.name = "lifeboat_beacon"
    for _c in _bo.users_collection:
        _c.objects.unlink(_bo)
    D.collections["lifeboat"].objects.link(_bo)
    _bo.data.materials.append(lbm)
    LIGHT_MARKERS.append(("lifeboat_beacon", _bo.location.copy(), "pod"))

sc["menu_light_markers"] = [[n, list(p), k] for n, p, k in LIGHT_MARKERS]
sc["menu_sun_disc_dir"] = list(SUN_DISC)
sc["menu_sun_radius_rad"] = SUN_R

# ------------------------------------------------------------------ render + colour
sc.render.engine = "BLENDER_EEVEE"
sc.render.resolution_x = 2560
sc.render.resolution_y = 1440
sc.render.resolution_percentage = 100
sc.render.film_transparent = False
sc.render.filter_size = 1.5
sc.eevee.taa_render_samples = 96
sc.eevee.use_shadows = True
sc.eevee.shadow_ray_count = 3
sc.eevee.shadow_step_count = 8
sc.eevee.use_bokeh_jittered = True
sc.eevee.bokeh_overblur = 4.0
sc.view_settings.view_transform = "AgX"
for lk in ("AgX - Medium High Contrast", "Medium High Contrast", "AgX - Punchy", "Punchy"):
    try:
        sc.view_settings.look = lk
        break
    except Exception:
        pass
print("look", sc.view_settings.look)
sc.view_settings.exposure = EXPOSURE
sc.view_settings.gamma = 1.0
sc.render.dither_intensity = 0.6

# compositor: bloom on sun and lamps, gentle teal/orange CDL grade
ct = D.node_groups.new("MenuComp", "CompositorNodeTree")
ct.interface.new_socket("Image", in_out="OUTPUT", socket_type="NodeSocketColor")
rl = ct.nodes.new("CompositorNodeRLayers")
rl.scene = sc
gl = ct.nodes.new("CompositorNodeGlare")
gl.name = "bloom"
for k, v in (("Type", "Bloom"), ("Quality", "High")):
    try:
        gl.inputs[k].default_value = v
    except Exception as e:
        print("glare", k, e)
gl.inputs["Threshold"].default_value = 1.1
gl.inputs["Smoothness"].default_value = 0.35
gl.inputs["Strength"].default_value = 0.45
gl.inputs["Size"].default_value = 0.8
gl.inputs["Saturation"].default_value = 1.0
gl.inputs["Tint"].default_value = (0.92, 0.97, 1.05, 1.0) if SUNSET else (1.0, 0.88, 0.72, 1.0)
ct.links.new(rl.outputs["Image"], gl.inputs["Image"])
cb = ct.nodes.new("CompositorNodeColorBalance")
cb.name = "grade"
for v in ("Offset/Power/Slope (ASC-CDL)", "Offset/Power/Slope", "ASC-CDL"):
    try:
        cb.inputs["Type"].default_value = v
        break
    except Exception:
        pass
print("cb type", cb.inputs["Type"].default_value)
cols = [i for i in cb.inputs if i.type == "RGBA"]
named = {}
for i in cb.inputs:
    if i.type == "RGBA":
        named.setdefault(i.name, i)
named["Power"].default_value = (1.015, 0.985, 0.94, 1.0)
named["Slope"].default_value = (1.035, 1.0, 0.955, 1.0)
ct.links.new(gl.outputs["Image"], cb.inputs["Image"])
hs = ct.nodes.new("CompositorNodeHueSat")
hs.name = "sat"
hs.inputs["Saturation"].default_value = 1.07
gout = ct.nodes.new("NodeGroupOutput")
ct.links.new(cb.outputs["Image"], hs.inputs["Image"])
ct.links.new(hs.outputs["Image"], gout.inputs[0])
sc.compositing_node_group = ct
sc.render.use_compositing = True

bpy.ops.wm.save_mainfile()
print("SAVED", bpy.data.filepath)
