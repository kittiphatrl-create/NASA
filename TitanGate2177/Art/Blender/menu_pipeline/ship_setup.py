# SHIP menu scene (no save): aboard the Kestrel in orbit. Builds Art/Blender/menu_ship.blend from mars_keyart_menu.blend
# (only the cadet and its materials are kept; the first save is a Save As, the menu file is not modified).
# blender -b Art/Blender/mars_keyart_menu.blend --python Art/Blender/menu_pipeline/ship_setup.py
import bpy, bmesh, sys, os, math
from mathutils import Vector, Euler
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from nb import NB, srgb
import metalkit as MK

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
SRC = os.path.join(ROOT, "Art", "Blender", "mars_keyart_menu.blend")
DST = os.path.join(ROOT, "Art", "Blender", "menu_ship.blend")
assert os.path.normcase(os.path.abspath(bpy.data.filepath)) == os.path.normcase(SRC), bpy.data.filepath
bpy.ops.wm.save_as_mainfile(filepath=DST)
D = bpy.data
rad = math.radians
sc = D.scenes["Scene"]
for s in list(D.scenes):
    if s != sc:
        D.scenes.remove(s)
bpy.context.window.scene = sc if bpy.context.window else None

# ------------------------------------------------------------------ strip everything but the cadet
keep = {o.name for o in D.collections["cadet"].all_objects} | {"KeyCam"}
for o in list(sc.objects):
    if o.name not in keep:
        D.objects.remove(o, do_unlink=True)
for c in list(D.collections):
    if c.name not in ("cadet",):
        D.collections.remove(c)
for blk in (D.meshes, D.lights, D.curves):
    for b in list(blk):
        if b.users == 0:
            blk.remove(b)

# ------------------------------------------------------------------ layout (camera looks +Y; floor z = 0)
LENS, PITCH, ROLL = 30.0, 4.0, -1.0
CAM = Vector((0.0, 0.0, 1.55))
TX = 18.0 / LENS                     # tan(half hfov)
TY = TX * 9 / 16
CADET_D, CADET_SX = 7.5, 0.665
WIN_D, WIN_SX, WIN_SY, WIN_SR = 10.2, 0.675, 0.43, 0.335      # window: wall distance, screen centre, radius (frac of height)
cad_x = CADET_D * (CADET_SX - 0.5) * 2 * TX
WX = WIN_D * (WIN_SX - 0.5) * 2 * TX
WZ = CAM.z + WIN_D * math.tan(rad(PITCH) + math.atan((0.5 - WIN_SY) * 2 * TY))
RW = WIN_D * math.tan(math.atan(WIN_SR * 2 * TY))
WY = WIN_D
print("SHIP window", round(WX, 2), round(WZ, 2), round(RW, 2), "cadet x", round(cad_x, 2))

cab = D.collections.new("cabin")
sc.collection.children.link(cab)
fgc = D.collections.new("ship_fg")
sc.collection.children.link(fgc)

cam = D.objects["KeyCam"]
cam.location = CAM
cam.rotation_mode = "XYZ"
cam.rotation_euler = Euler((rad(90 + PITCH), rad(ROLL), 0.0), "XYZ")
cam.data.lens = LENS
cam.data.sensor_width, cam.data.sensor_fit = 36.0, "HORIZONTAL"
cam.data.clip_start, cam.data.clip_end = 0.05, 200
cam.data.dof.use_dof = True
cam.data.dof.focus_object = D.objects["Body"]
cam.data.dof.aperture_fstop = 2.0
sc.camera = cam

root = D.objects["Root"]
root.location = Vector((cad_x, CADET_D, 0.0))
az = math.degrees(math.atan2(root.location.y - CAM.y, root.location.x - CAM.x))
root.rotation_euler = (0, 0, rad(az + 6.0 + 90.0))
bpy.context.view_layer.update()

# ------------------------------------------------------------------ materials (cadet toon look re-aimed for an interior)
look = D.node_groups["MenuLook"]
RIM_TO = Vector((0.45, 1.0, 0.55)).normalized()          # light pouring in through the window, ahead-right and above
for n in look.nodes:
    if n.bl_idname == "ShaderNodeVectorMath" and n.operation == "DOT_PRODUCT" and n.inputs[0].is_linked \
            and n.inputs[0].links[0].from_node.bl_idname == "ShaderNodeNewGeometry" and n.inputs[0].links[0].from_socket.name == "Normal":
        cx = n.inputs[1].links[0].from_node
        for i in range(3):
            cx.inputs[i].default_value = RIM_TO[i]
    if n.bl_idname == "ShaderNodeMix" and n.blend_type == "ADD" and not n.inputs[7].is_linked and n.inputs[7].default_value[0] > 1.5:
        n.inputs[7].default_value = (*srgb("#FFC296", 2.1), 1)      # warm Mars-shine rim
for m in D.materials:
    if m.node_tree:
        for n in m.node_tree.nodes:
            if n.type == "GROUP" and n.node_tree == look:
                n.inputs["Fog"].default_value = 0.0
                n.inputs["Sheen"].default_value = 0.0


def toon(name, hexcol, rim=0.12):
    m = D.materials["metal"].copy()
    m.name = name
    for n in m.node_tree.nodes:
        if n.type == "MIX" and n.blend_type == "MULTIPLY" and not n.inputs[6].is_linked:
            n.inputs[6].default_value = (*srgb(hexcol), 1)
        if n.type == "GROUP":
            n.inputs["Rim"].default_value, n.inputs["Fog"].default_value, n.inputs["Sheen"].default_value = rim, 0.0, 0.0
    return m


def emit(name, hexcol, strength):
    m = D.materials.new(name)
    if not m.node_tree:
        m.use_nodes = True
    m.node_tree.nodes.clear()
    B = NB(m.node_tree)
    e = B.node("ShaderNodeEmission")
    e.inputs["Color"].default_value = (*srgb(hexcol), 1)
    e.inputs["Strength"].default_value = strength
    m.node_tree.links.new(e.outputs[0], B.node("ShaderNodeOutputMaterial").inputs["Surface"])
    return m


m_wall = toon("cabin_wall", "#3A3E4A", 0.10)
m_wall2 = toon("cabin_rib", "#2A2D36", 0.18)
m_floor = toon("cabin_floor", "#2E2A2A", 0.05)
m_rail = toon("cabin_rail", "#6A6460", 0.45)
m_panel = toon("cabin_panel", "#22252C", 0.10)


def plane(name, size, loc, rot, mat, coll=cab):
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=0.5)
    bmesh.ops.scale(bm, vec=(size[0], size[1], 1), verts=bm.verts)
    o = MK.mesh_obj(coll, name, bm, mat)
    o.location, o.rotation_euler = loc, rot
    return o


# back wall with a round hole for the docking-port window
wall = MK.box(cab, "wall_back", (30.0, 0.12, 9.0), (2.0, WY + 0.06, 4.5), (0, 0, 0), m_wall)
bpy.ops.mesh.primitive_cylinder_add(vertices=256, radius=RW, depth=2.0, location=(WX, WY, WZ), rotation=(rad(90), 0, 0))
cutter = bpy.context.active_object
cutter.name = "win_cutter"
bo = wall.modifiers.new("hole", "BOOLEAN")
bo.operation, bo.object = "DIFFERENCE", cutter
try:
    bo.solver = "EXACT"
except Exception:
    pass
bpy.context.view_layer.objects.active = wall
for o in bpy.context.selected_objects:
    o.select_set(False)
wall.select_set(True)
bpy.ops.object.modifier_apply(modifier="hole")
D.objects.remove(cutter, do_unlink=True)
MK.box(cab, "floor", (30.0, 16.0, 0.2), (2.0, WY - 7.0, -0.1), (0, 0, 0), m_floor)
MK.box(cab, "ceiling", (30.0, 16.0, 0.2), (2.0, WY - 7.0, 9.1), (0, 0, 0), m_wall2)
MK.box(cab, "wall_left", (0.3, 16.0, 9.0), (-9.5, WY - 7.0, 4.5), (0, 0, 0), m_wall2)
for i, x in enumerate((-6.6, -3.4, 7.4)):                       # structural ribs
    MK.box(cab, f"rib{i}", (0.42, 0.30, 9.0), (x, WY - 0.12, 4.5), (0, 0, 0), m_wall2, bevel=0.05)
MK.box(cab, "kick", (30.0, 0.22, 0.45), (2.0, WY - 0.08, 0.225), (0, 0, 0), m_wall2, bevel=0.03)
MK.box(cab, "top_band", (30.0, 0.22, 0.5), (2.0, WY - 0.08, 6.7), (0, 0, 0), m_wall2, bevel=0.03)
# handrails
rail_x1 = WX - RW - 0.95
MK.box(cab, "rail_h", (rail_x1 + 8.5, 0.09, 0.09), ((rail_x1 - 8.5) / 2, WY - 0.38, 1.25), (0, 0, 0), m_rail, bevel=0.04)
for i, x in enumerate((-7.2, -4.6, -2.0)):
    MK.box(cab, f"rail_brk{i}", (0.08, 0.36, 0.08), (x, WY - 0.2, 1.25), (0, 0, 0), m_rail, bevel=0.02)
MK.box(cab, "rail_v", (0.09, 0.09, 3.2), (rail_x1 + 0.2, WY - 0.38, 2.75), (0, 0, 0), m_rail, bevel=0.04)
for z in (1.3, 4.2):
    MK.box(cab, f"rail_vbrk{z}", (0.08, 0.36, 0.08), (rail_x1 + 0.2, WY - 0.2, z), (0, 0, 0), m_rail, bevel=0.02)

# ------------------------------------------------------------------ gold docking-port window frame (ring language)
ROWS = ((1, 0, 0), (0, 0, 1), (0, -1, 0))            # world -> ring-local (ring axis = toward the camera)
fr_coll = D.collections.new("window_frame")
cab.children.link(fr_coll)
R0, R1, DEP = RW, RW * 1.20, 0.30
m_band = MK.metal("win_band", R0, R1, 0.30, 0.72, center=(WX, WY, WZ), axis_mat=ROWS, gain=0.47, light_mix=0.5)
m_bead = MK.metal("win_bead", R0 - 0.06, R0 + 0.02, 0.38, 0.78, center=(WX, WY, WZ), axis_mat=ROWS, gain=0.47, light_mix=0.5)
m_latch = MK.metal("win_latch", R0 + 0.05, R1 - 0.05, 0.36, 0.70, center=(WX, WY, WZ), axis_mat=ROWS, gain=0.50, light_mix=0.5)
m_bolt = MK.metal("win_bolt", 0.0, 100.0, 0.80, 0.80, center=(WX, WY, WZ), axis_mat=ROWS, gain=0.52, light_mix=0.45)
f = 0.06
prof = [(R0, -0.05), (R0, DEP - f)] + MK.fillet(R0 + f, DEP - f, f, math.pi, math.pi / 2)[1:] + MK.fillet(R1 - f, DEP - f, f, math.pi / 2, 0.0) + [(R1, -0.05)]
frame_pieces = [MK.lathe(fr_coll, "win_band", prof, m_band, segs=256)]
frame_pieces.append(MK.lathe(fr_coll, "win_bead", MK.fillet(R0 - 0.02, DEP * 0.55, 0.05, math.pi, 0.0, n=12), m_bead, segs=256))
RM = (R0 + R1) / 2
for k in range(12):
    a = 2 * math.pi * k / 12
    frame_pieces.append(MK.box(fr_coll, f"win_latch{k}", (0.30, 0.34, 0.16), (RM * math.cos(a), RM * math.sin(a), DEP + 0.04), (0, 0, a), m_latch, bevel=0.035))
    frame_pieces.append(MK.bolt(fr_coll, f"win_lbolt{k}", ((RM + 0.02) * math.cos(a), (RM + 0.02) * math.sin(a), DEP + 0.125), 0.045, m_bolt))
    b = a + math.pi / 12
    frame_pieces.append(MK.bolt(fr_coll, f"win_bolt{k}", (RM * math.cos(b), RM * math.sin(b), DEP), 0.05, m_bolt))
pivot = D.objects.new("window_frame_pivot", None)
fr_coll.objects.link(pivot)
pivot.location = (WX, WY - 0.02, WZ)
pivot.rotation_euler = (rad(90), 0, 0)
for o in frame_pieces:
    o.parent = pivot

# ------------------------------------------------------------------ interior lamps (beacons) and lights
BEACONS = []
lamp_warm = emit("ship_lamp_warm", "#FFB060", 4.0)
lamp_amber = emit("ship_lamp_amber", "#FF9A30", 5.0)
lamp_green = emit("ship_lamp_green", "#62E08A", 3.0)
o = MK.box(cab, "wall_lamp", (0.62, 0.10, 0.16), (-5.0, WY - 0.12, 2.55), (0, 0, 0), lamp_warm, bevel=0.04)
BEACONS.append((o.location.copy(), "#FFB060"))
for i, x in enumerate((-7.8, -5.0, -1.9, 5.6)):
    o = MK.box(cab, f"guide{i}", (0.30, 0.08, 0.07), (x, WY - 0.22, 0.30), (0, 0, 0), lamp_warm, bevel=0.02)
    BEACONS.append((o.location.copy(), "#FFB060"))
px_ = rail_x1 - 0.95
MK.box(cab, "status_panel", (0.70, 0.08, 0.95), (px_, WY - 0.08, 2.05), (0, 0, 0), m_panel, bevel=0.04)
for i, (dz, mat, col) in enumerate(((0.25, lamp_amber, "#FF9A30"), (0.0, lamp_green, "#62E08A"), (-0.25, lamp_amber, "#FF9A30"))):
    o = MK.bolt(cab, f"status_led{i}", (px_ + 0.18, WY - 0.14, 2.05 + dz), 0.05, mat, flat=0.6)
    o.rotation_euler = (rad(90), 0, 0)
    BEACONS.append((o.location.copy(), col))
sc["ship_beacons"] = [[list(p), c] for p, c in BEACONS]
sc["ship_window"] = [WX, WY - 0.02, WZ, RW]


def light(name, typ, loc, energy, color, size=0.5, rot=None, shadow=True):
    ld = D.lights.new(name, typ)
    ld.energy, ld.color = energy, color
    if typ in ("POINT", "SPOT"):
        ld.shadow_soft_size = size
    if typ == "AREA":
        ld.shape, ld.size = "DISK", size
    if typ == "SUN":
        ld.angle = rad(size)
    ld.use_shadow = shadow
    o = D.objects.new(name, ld)
    cab.objects.link(o)
    o.location = loc
    if rot is not None:
        o.rotation_mode = "QUATERNION"
        o.rotation_quaternion = rot
    return o


sun_dir = Vector((-0.30, -1.0, -0.42)).normalized()                    # sunlight entering through the window
light("window_sun", "SUN", (0, 0, 10), 2.6, (1.0, 0.94, 0.86), 1.0, sun_dir.to_track_quat("-Z", "Y"))
light("mars_shine", "AREA", (WX, WY + 0.6, WZ - 0.6), 900.0, (1.0, 0.55, 0.30), RW * 1.8, Vector((0, -1, 0.15)).to_track_quat("-Z", "Y"))
light("floor_warm_l", "POINT", (-5.2, WY - 1.0, 0.45), 70.0, (1.0, 0.58, 0.30), 0.4)
light("floor_warm_r", "POINT", (5.4, WY - 1.0, 0.45), 55.0, (1.0, 0.58, 0.30), 0.4)
light("ceiling_warm", "AREA", (0.5, 5.5, 8.8), 160.0, (1.0, 0.70, 0.45), 5.0, Vector((0, 0, -1)).to_track_quat("-Z", "Y"))
light("back_fill", "POINT", (-1.5, 1.0, 2.6), 45.0, (1.0, 0.66, 0.42), 1.0, shadow=False)

# ------------------------------------------------------------------ foreground: blurred handrail edge near the camera (bottom right)
rail_mat = toon("fg_rail", "#2C2A2A", 0.6)
MK.box(fgc, "fg_rail", (1.6, 0.09, 0.09), (1.15, 1.62, 1.245), (0, rad(-5), rad(6)), rail_mat, bevel=0.04)
MK.box(fgc, "fg_rail_brk", (0.07, 0.07, 0.50), (0.86, 1.66, 0.93), (0, 0, 0), rail_mat, bevel=0.025)

# ------------------------------------------------------------------ world: dark, faint cool ambient
w = D.worlds.new("ShipWorld")
if not w.node_tree:
    w.use_nodes = True
bg = next(n for n in w.node_tree.nodes if n.type == "BACKGROUND")
bg.inputs["Color"].default_value = (*srgb("#14161E"), 1)
bg.inputs["Strength"].default_value = 1.0
sc.world = w
sc.view_settings.exposure = 0.0
sc.render.film_transparent = True
sc.eevee.taa_render_samples = 96

# ------------------------------------------------------------------ PLANET scene: Mars from orbit in canvas pixel units (ortho)
W, H = 2560, 1440
ps = D.scenes.new("Planet")
ps.render.engine = "BLENDER_EEVEE"
ps.render.resolution_x, ps.render.resolution_y, ps.render.resolution_percentage = W, H, 100
ps.render.film_transparent = True
ps.render.filter_size = 1.3
ps.eevee.taa_render_samples = 64
ps.view_settings.view_transform = "AgX"
for lk in ("AgX - Medium High Contrast", "Medium High Contrast"):
    try:
        ps.view_settings.look = lk
        break
    except Exception:
        pass
ps.view_settings.exposure = -0.15
ps.world = D.worlds.new("PlanetWorld")
PX, PY, RP = 2330.0, 1530.0, 1080.0          # canvas px: centre below-right of the window, limb across its upper-left
ps["planet"] = [PX, PY, RP]
pm = D.materials.new("mars_globe")
if not pm.node_tree:
    pm.use_nodes = True
nt = pm.node_tree
nt.nodes.clear()
B = NB(nt)
tc = B.node("ShaderNodeTexCoord")
geo = B.node("ShaderNodeNewGeometry")
n1 = B.node("ShaderNodeTexNoise")
n1.inputs["Scale"].default_value, n1.inputs["Detail"].default_value, n1.inputs["Roughness"].default_value = 1.6, 5.0, 0.55
nt.links.new(tc.outputs["Object"], n1.inputs["Vector"])
n2 = B.node("ShaderNodeTexNoise")
n2.inputs["Scale"].default_value, n2.inputs["Detail"].default_value, n2.inputs["Roughness"].default_value = 7.0, 8.0, 0.6
nt.links.new(tc.outputs["Object"], n2.inputs["Vector"])
mixn = B.math("ADD", B.math("MULTIPLY", n1.outputs["Fac"], 0.72), B.math("MULTIPLY", n2.outputs["Fac"], 0.28))
alb = MK.ramp_node(B, mixn, [(0.0, "#2E160E"), (0.40, "#5A2A18"), (0.47, "#9A4422"), (0.56, "#C0703C"), (0.66, "#D89A64"), (1.0, "#E8B07E")])
pz = B.sep(tc.outputs["Object"])[2]
cap = B.maprange(pz, 0.86, 0.91, 0.0, 1.0, "SMOOTHSTEP")
alb = B.mix(cap, alb, srgb("#EEE6DC"))
L = Vector((-0.50, 0.50, -0.71)).normalized()          # sun behind the planet, upper left: lit crescent on the limb, terminator across the window
ndl = B.vmath("DOT_PRODUCT", geo.outputs["Normal"], B.vec(L))
day = B.maprange(ndl, -0.10, 0.38, 0.0, 1.0, "SMOOTHSTEP")
lit = B._scale_sock(alb, B.math("ADD", 0.03, B.math("MULTIPLY", day, 1.05)))
nv = B.sep(geo.outputs["Normal"])[2]                                     # view is +Z
limb = B.maprange(nv, 0.45, 0.0, 0.0, 1.0, "SMOOTHSTEP")
haze = B.math("MULTIPLY", limb, B.maprange(ndl, -0.15, 0.25, 0.0, 1.0, "SMOOTHSTEP"))
col = B.mix(B.math("MULTIPLY", haze, 0.55), lit, srgb("#6E9EE6"), "MIX")
em = B.node("ShaderNodeEmission")
B.set(em.inputs["Color"], col)
nt.links.new(em.outputs[0], B.node("ShaderNodeOutputMaterial").inputs["Surface"])
bm = bmesh.new()
bmesh.ops.create_uvsphere(bm, u_segments=192, v_segments=96, radius=1.0)
globe = MK.mesh_obj(ps.collection, "mars_globe", bm, pm, smooth=True)
globe.location = (PX, -PY, 0.0)
globe.scale = (RP, RP, RP)
globe.rotation_euler = (rad(-62), rad(18), rad(-24))     # north pole tipped toward the camera / upper right
pcd = D.cameras.new("PlanetCam")
pcd.type, pcd.ortho_scale = "ORTHO", float(W)
pcd.clip_start, pcd.clip_end = 1.0, 10000.0
pc = D.objects.new("PlanetCam", pcd)
ps.collection.objects.link(pc)
pc.location = (W / 2, -H / 2, 5000.0)
ps.camera = pc
sc["planet_light"] = list(L)

bpy.ops.wm.save_mainfile()
print("SAVED", D.filepath)
