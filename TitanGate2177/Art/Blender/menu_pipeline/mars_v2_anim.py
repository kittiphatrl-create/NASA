# Adds the animated Mars menu pieces to mars_keyart_sunset_v2.blend (run after mars_v2.py):
#   airlock porch + sliding door with interior light (closed state is part of the base layer),
#   mission flag (cloth sim, Mars gravity, seamless 24-frame loop stored as shape keys),
#   distant dust devil (procedural volume, periodic in one 'phase' value -> exact loop).
# blender -b Art/Blender/mars_keyart_sunset_v2.blend --python Art/Blender/menu_pipeline/mars_v2_anim.py
import bpy, bmesh, sys, os, math
import numpy as np
from mathutils import Vector, Matrix, Euler, noise
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from nb import NB, srgb
import clay as C
import sculpt as S

D = bpy.data
assert D.filepath.lower().endswith("mars_keyart_sunset_v2.blend"), D.filepath
sc = bpy.context.scene
rad = math.radians
cam = sc.camera
cpos = cam.matrix_world.translation.copy()
haze = D.node_groups.get("HazeV2")
dg = bpy.context.evaluated_depsgraph_get()
for nm in ("menu_loops", "v2_airlock"):
    c = D.collections.get(nm)
    if c:
        for o in list(c.objects):
            D.objects.remove(o, do_unlink=True)
        D.collections.remove(c)
loops = D.collections.new("menu_loops")
sc.collection.children.link(loops)
air = D.collections.new("v2_airlock")
D.collections["base"].children.link(air)


def ground_z(x, y):
    o = Vector((x, y, 60.0))
    for _ in range(12):
        hit, loc, nor, idx, ob, mt = sc.ray_cast(dg, o, Vector((0, 0, -1)))
        if not hit:
            return 0.0
        if ob.name == "ground":
            return loc.z
        o = loc + Vector((0, 0, -0.01))
    return 0.0


def world_bbox(o):
    me = o.evaluated_get(dg).to_mesh()
    vs = [o.matrix_world @ v.co for v in me.vertices]
    o.evaluated_get(dg).to_mesh_clear()
    return Vector([min(v[i] for v in vs) for i in range(3)]), Vector([max(v[i] for v in vs) for i in range(3)])


# =========================================================== AIRLOCK on hab_a (front, near the left end)
hmin, hmax = world_bbox(D.objects["hab_a"])
hx = hmin.x + 0.55
hy = hmin.y + 0.15                                  # front skin of the module (cylinder axis along X)
gz = ground_z(hx, hy - 0.6)
face = Vector((cpos.x - hx, cpos.y - hy, 0.0)).normalized()
yaw = math.atan2(face.y, face.x) + math.pi / 2       # porch local -Y faces the camera
R = Matrix.Rotation(yaw, 4, "Z")
PW, PD, PH = 2.1, 1.3, 2.35                          # porch (side walls hide the parted leaves)
DW, DH, DD = 0.98, 1.78, 0.62                        # doorway (width, height, pocket depth)
origin = Vector((hx, hy, gz - 0.12)) + face * 0.45
M = Matrix.Translation(origin) @ R


def porch_bm(bm):
    r = bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=(PW, PD, PH), verts=r["verts"])
    bmesh.ops.translate(bm, vec=(0, 0, PH / 2), verts=r["verts"])


porch = S.organic("v2_airlock_porch", porch_bm, air, D.materials["v2_module"], voxel=0.03, smooth=2)
cut = D.objects.new("cut", D.meshes.new("cut"))
air.objects.link(cut)
bm = bmesh.new()
r = bmesh.ops.create_cube(bm, size=1.0)
bmesh.ops.scale(bm, vec=(DW, DD * 2, DH), verts=r["verts"])
bmesh.ops.translate(bm, vec=(0, -PD / 2, 0.14 + DH / 2), verts=r["verts"])
bm.to_mesh(cut.data)
bm.free()
bo = porch.modifiers.new("door", "BOOLEAN")
bo.operation, bo.object = "DIFFERENCE", cut
try:
    bo.solver = "EXACT"
except Exception:
    pass
bv = porch.modifiers.new("bev", "BEVEL")
bv.width, bv.segments, bv.limit_method = 0.06, 4, "ANGLE"
S.bake(porch, coords=None)
D.objects.remove(cut, do_unlink=True)
for v in porch.data.vertices:                         # hand-made wobble
    v.co += v.normal * 0.01 * noise.noise(v.co * 2.5)
porch.matrix_world = M
# frame trim around the doorway (sculpted rounded tube, darker painted clay)
m_trim = C.clay("v2_airlock_trim", "#5A5E6A", rough=0.45, sss=0.05, size=0.3, dust=0.5, dust_hex="#7A3A1E", ao_dist=0.08, haze=haze)
tp = []
for (x, z) in ((-DW / 2, 0.14), (-DW / 2, 0.14 + DH - 0.22), (-DW / 2 + 0.22, 0.14 + DH), (DW / 2 - 0.22, 0.14 + DH), (DW / 2, 0.14 + DH - 0.22), (DW / 2, 0.14)):
    tp.append(Vector((x * 1.06, -PD / 2 - 0.02, z)))
trim = S.tube("v2_airlock_trim", S.catmull(tp, 10), lambda t, L: 0.045, air, m_trim, seg=10)
trim.matrix_world = M
# interior: warm panel at the back of the pocket, a little hand rail
m_in = D.materials.new("v2_airlock_interior")
if not m_in.node_tree:
    m_in.use_nodes = True
nt = m_in.node_tree
nt.nodes.clear()
B = NB(nt)
pr = B.node("ShaderNodeBsdfPrincipled")
pr.inputs["Base Color"].default_value = (*srgb("#C8B8A0"), 1)
pr.inputs["Roughness"].default_value = 0.6
pr.inputs["Emission Color"].default_value = (*srgb("#FF9A52"), 1)   # deep amber (AgX bleaches pale tints to white)
glow = B.node("ShaderNodeValue")
glow.name = "door_glow"
glow.outputs[0].default_value = 0.0
gx, gy, gz = B.sep(B.node("ShaderNodeTexCoord").outputs["Generated"])      # brighter overhead, darker edges -> reads as a lit corridor
grad = B.math("MULTIPLY", B.maprange(gz, 0.0, 1.0, 0.5, 1.0), B.maprange(B.math("ABSOLUTE", B.math("SUBTRACT", gx, 0.5)), 0.25, 0.5, 1.0, 0.6))
B.set(pr.inputs["Emission Strength"], B.math("MULTIPLY", glow.outputs[0], grad))
nt.links.new(pr.outputs[0], B.node("ShaderNodeOutputMaterial").inputs["Surface"])
inner = D.objects.new("v2_airlock_inner", D.meshes.new("v2_airlock_inner"))
air.objects.link(inner)
bm = bmesh.new()
r = bmesh.ops.create_cube(bm, size=1.0)
bmesh.ops.scale(bm, vec=(DW * 0.98, 0.05, DH * 0.98), verts=r["verts"])
bmesh.ops.translate(bm, vec=(0, -PD / 2 + DD - 0.04, 0.14 + DH / 2), verts=r["verts"])
bm.to_mesh(inner.data)
bm.free()
inner.data.materials.append(m_in)
inner.matrix_world = M
# door leaf: painted clay with a porthole and hazard stripes; slides left into the porch wall
def leaf_color(B, col):
    tc = B.node("ShaderNodeTexCoord").outputs["Object"]
    x, y, z = B.sep(tc)
    band = B.math("MULTIPLY", B.maprange(z, 0.30, 0.42, 1.0, 0.0), B.maprange(B.math("SINE", B.math("MULTIPLY", B.math("ADD", x, z), 22.0)), -0.1, 0.1, 0.0, 1.0))
    col = B.mix(band, col, srgb("#C8642A"))
    return col


m_leaf = C.clay("v2_airlock_door", "#B7B2A6", rough=0.5, sss=0.1, size=0.3, thumbs=0.6, tool=0.7, dust=0.5, dust_hex="#7A3A1E", ao_dist=0.06,
                haze=haze, extra_color=leaf_color, height_dust=(-0.2, 0.9, 0.6))
leaves = []
for side in (-1, 1):                                  # split door: two leaves part left / right into the porch walls
    lf = D.objects.new(f"v2_airlock_door{'L' if side < 0 else 'R'}", D.meshes.new("v2_airlock_door"))
    air.objects.link(lf)
    bm = bmesh.new()
    r = bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=(DW / 2 - 0.012, 0.08, DH - 0.02), verts=r["verts"])
    bmesh.ops.translate(bm, vec=(0, 0, DH / 2), verts=r["verts"])
    bm.to_mesh(lf.data)
    bm.free()
    lf.data.materials.append(m_leaf)
    b_ = lf.modifiers.new("bev", "BEVEL")
    b_.width, b_.segments = 0.04, 4
    lf.matrix_world = M @ Matrix.Translation((side * DW / 4, -PD / 2 + 0.10, 0.15))
    pw_ = D.objects.new(f"v2_airlock_port{side}", D.meshes.new("v2_airlock_port"))
    air.objects.link(pw_)
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=24, radius1=0.085, radius2=0.085, depth=0.03)
    bm.to_mesh(pw_.data)
    bm.free()
    pw_.data.materials.append(D.materials["window"])
    pw_.parent = lf
    pw_.matrix_parent_inverse = Matrix.Identity(4)
    pw_.location = (0.0, -0.045, DH * 0.72)
    pw_.rotation_euler = (rad(90), 0, 0)
    leaves.append((lf.name, side, [list(r_) for r_ in lf.matrix_world]))
# interior lights (off in the base layer)
la = D.lights.new("v2_airlock_area", "AREA")
la.shape, la.size, la.energy, la.color = "RECTANGLE", 0.8, 0.0, (1.0, 0.72, 0.44)
la.size_y = 1.5
lao = D.objects.new("v2_airlock_area", la)
air.objects.link(lao)
lao.matrix_world = M @ Matrix.Translation((0, -PD / 2 + DD - 0.2, 0.14 + DH * 0.6)) @ Euler((rad(-90), 0, 0)).to_matrix().to_4x4()   # shines OUT of the doorway
lao.visible_camera = False
# the clay SSS lets the interior light bleed through the 8 cm leaves -> exclude leaves + portholes from the interior lights
nol = D.collections.new("v2_airlock_nolight")
for o_ in air.objects:
    if o_.name.startswith(("v2_airlock_door", "v2_airlock_port")):
        nol.objects.link(o_)
for o_ in nol.collection_objects:
    o_.light_linking.link_state = "EXCLUDE"
lao.light_linking.receiver_collection = nol
inner.light_linking.receiver_collection = nol
door_center = M @ Vector((0, -PD / 2, 0.14 + DH / 2))
spill = M @ Vector((0, -PD / 2 - 1.3, 0.0))
spill.z = ground_z(spill.x, spill.y)
sc["v2_door"] = {"center": list(door_center), "spill": list(spill), "slide": list((M.to_3x3() @ Vector((1, 0, 0))).normalized()),
                 "width": DW, "leaves": {n: {"side": float(sd), "m": [v for r_ in mm for v in r_]} for n, sd, mm in leaves}}

# =========================================================== FLAG (cloth sim, Mars gravity, seamless loop)
lb_min, lb_max = world_bbox(D.objects["lifeboat"])
fx, fy = (lb_min.x + lb_max.x) / 2 + 0.4, lb_min.y - 0.6     # just in front of the pod
fgz = ground_z(fx, fy)
POLE_H, FW, FH = 2.45, 1.15, 0.72
m_pole = C.clay("v2_flag_pole", "#8E8C90", rough=0.4, sss=0.0, metallic=0.5, size=0.2, dust=0.4, dust_hex="#7A3A1E", ao_dist=0.05, haze=haze)
pole = S.tube("v2_flag_pole", [Vector((fx, fy, fgz - 0.25)), Vector((fx, fy, fgz + POLE_H))], lambda t, L: 0.028, loops, m_pole, seg=12)
knob = S.organic("v2_flag_knob", lambda bm: bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=8, radius=0.05), loops, m_pole, voxel=0.01, smooth=1)
knob.location = (fx, fy, fgz + POLE_H + 0.03)
sc.gravity = (0.0, 0.0, -3.71)
bpy.ops.mesh.primitive_grid_add(x_subdivisions=34, y_subdivisions=22, size=1.0)
flag = bpy.context.active_object
flag.name = "v2_flag"
for c in list(flag.users_collection):
    c.objects.unlink(flag)
loops.objects.link(flag)
me = flag.data
for v in me.vertices:                                  # grid in XZ: x from the pole outward, z down from the top
    u, w = v.co.x + 0.5, v.co.y + 0.5
    v.co = Vector((u * FW, 0.0, -(1 - w) * FH))
flag.location = (fx + 0.035, fy, fgz + POLE_H - 0.06)
vg = flag.vertex_groups.new(name="pin")
vg.add([v.index for v in me.vertices if v.co.x < 0.02], 1.0, "REPLACE")
cl = flag.modifiers.new("cloth", "CLOTH")
cs = cl.settings
cs.quality, cs.mass, cs.air_damping = 10, 0.07, 0.4
cs.tension_stiffness = cs.compression_stiffness = 15.0
cs.shear_stiffness, cs.bending_stiffness = 5.0, 0.3
cs.vertex_group_mass = "pin"
cl.point_cache.frame_start, cl.point_cache.frame_end = 1, 260
wdir = Vector((math.cos(rad(-15)), math.sin(rad(-15)), 0.0))    # blows right and a little toward camera: flag flies broadside
bpy.ops.object.effector_add(type="WIND", location=Vector((fx, fy, fgz + POLE_H - 0.3)) - wdir * 3.0)
wind = bpy.context.active_object
wind.rotation_mode = "QUATERNION"
wind.rotation_quaternion = wdir.to_track_quat("Z", "Y")
for c in list(wind.users_collection):
    c.objects.unlink(wind)
sc.collection.objects.link(wind)
wind.name = "v2_wind"
wind.field.strength, wind.field.noise, wind.field.flow = 300.0, 5.0, 0.0
bpy.ops.object.effector_add(type="TURBULENCE", location=(fx + 0.6, fy, fgz + POLE_H - 0.4))
turb = bpy.context.active_object
turb.name = "v2_turb"
for c in list(turb.users_collection):
    c.objects.unlink(turb)
sc.collection.objects.link(turb)
for o in (wind, turb):
    o.hide_viewport = False
    o.hide_render = False
turb.field.strength, turb.field.size = 14.0, 0.45
P = {}
for f in range(1, 241):
    sc.frame_set(f)
    if f >= 80:
        dge = bpy.context.evaluated_depsgraph_get()
        em = flag.evaluated_get(dge).to_mesh()
        P[f] = np.array([v.co[:] for v in em.vertices], np.float32)
        flag.evaluated_get(dge).to_mesh_clear()
sc.frame_set(1)
L2 = 48                                                # 24 output frames at 12 fps = every 2nd sim frame (24 fps)
best = min(range(96, 190), key=lambda s: float(np.abs(P[s] - P[s + L2]).mean() + np.abs((P[s + 2] - P[s]) - (P[s + L2 + 2] - P[s + L2])).mean() * 2))
Q = []
CF = 8
for k in range(24):
    a = P[best + 2 * k]
    if k >= 24 - CF:
        w = (k - (24 - CF) + 1) / (CF + 1)
        w = w * w * (3 - 2 * w)
        a = a * (1 - w) + P[best + 2 * k - L2] * w
    Q.append(a)
print("FLAG loop start", best, "seam", round(float(np.abs(Q[-1] - Q[0]).mean()), 4), "step", round(float(np.mean([np.abs(Q[i + 1] - Q[i]).mean() for i in range(23)])), 4))
flag.modifiers.clear()
for o in (wind, turb):
    D.objects.remove(o, do_unlink=True)
sc.gravity = (0.0, 0.0, -9.81)
for i, v in enumerate(me.vertices):
    v.co = Vector(Q[0][i])
flag.shape_key_add(name="Basis")
for k in range(24):
    sk = flag.shape_key_add(name=f"f{k:02d}")
    for i, d in enumerate(sk.data):
        d.co = Vector(Q[k][i])
    sk.value = 0.0
so_ = flag.modifiers.new("thick", "SOLIDIFY")
so_.thickness = 0.006
su_ = flag.modifiers.new("sub", "SUBSURF")
su_.levels = su_.render_levels = 1
for p in me.polygons:
    p.use_smooth = True


def flag_col(B, col):
    uv = B.node("ShaderNodeUVMap").outputs["UV"]
    u, v, _ = B.sep(uv)
    col = B.mix(B.maprange(v, 0.48, 0.52, 1.0, 0.0), col, srgb("#E6DCC6"))                # cream lower half
    d = B.vmath("LENGTH", B.vmath("MULTIPLY", B.vmath("SUBTRACT", uv, (0.24, 0.5, 0.0)), (1.6, 1.0, 0.0)))
    disc = B.maprange(d, 0.20, 0.18, 0.0, 1.0)
    col = B.mix(disc, col, srgb("#D2642A"))
    oe = B.vmath("LENGTH", B.vmath("MULTIPLY", B.vmath("SUBTRACT", uv, (0.24, 0.5, 0.0)), (0.75, 2.2, 0.0)))
    ring = B.math("MULTIPLY", B.maprange(B.math("ABSOLUTE", B.math("SUBTRACT", oe, 0.26)), 0.03, 0.0, 0.0, 1.0), B.math("SUBTRACT", 1.0, disc))
    return B.mix(ring, col, srgb("#F4EEE0"))


m_flag = C.clay("v2_flag_cloth", "#24496C", rough=0.75, sss=0.35, sss_radius=(1.0, 0.9, 0.8), sss_scale=0.01, size=0.15, thumbs=0.2, tool=0.0,
                grain=1.2, bump=0.25, sheen=0.5, haze=haze, extra_color=flag_col)
me.materials.clear()
me.materials.append(m_flag)
sc["v2_flag"] = {"base": [fx, fy, fgz], "top": [fx, fy, fgz + POLE_H]}

# =========================================================== DUST DEVIL (procedural volume, periodic in 'phase')
fwd = cam.matrix_world.to_3x3() @ Vector((0, 0, -1))
fwd.z = 0
fwd.normalize()
fr = [cam.matrix_world @ c for c in cam.data.view_frame(scene=sc)]
tr, br, bl, tl = fr
dirv = ((tl + (tr - tl) * 0.485 + (bl - tl) * 0.6) - cpos)
dirv.z = 0
dirv.normalize()
DIST, DH_, DR = 78.0, 13.0, 3.2
dp = cpos + dirv * DIST
dz = ground_z(dp.x, dp.y)
bm = bmesh.new()
bmesh.ops.create_cone(bm, cap_ends=True, segments=32, radius1=1.0, radius2=1.0, depth=1.0)
bmesh.ops.translate(bm, vec=(0, 0, 0.5), verts=bm.verts)
dev = D.objects.new("v2_dustdevil", D.meshes.new("v2_dustdevil"))
loops.objects.link(dev)
bm.to_mesh(dev.data)
bm.free()
dev.location = (dp.x, dp.y, dz - 0.3)
dev.scale = (DR, DR, DH_)
vm = D.materials.new("v2_dustdevil")
if not vm.node_tree:
    vm.use_nodes = True
nt = vm.node_tree
nt.nodes.clear()
B = NB(nt)
ph = B.node("ShaderNodeValue")
ph.name = "phase"
ph.outputs[0].default_value = 0.0
tau = B.math("MULTIPLY", ph.outputs[0], 2 * math.pi)
p = B.node("ShaderNodeTexCoord").outputs["Object"]
x, y, z = B.sep(p)
sx = B.math("SUBTRACT", x, B.math("MULTIPLY", B.math("SINE", B.math("ADD", tau, B.math("MULTIPLY", z, 3.0))), 0.10))
sy = B.math("SUBTRACT", y, B.math("MULTIPLY", B.math("COSINE", B.math("ADD", tau, B.math("MULTIPLY", z, 2.4))), 0.07))
cxyz = B.node("ShaderNodeCombineXYZ")
B.set(cxyz.inputs[0], sx); B.set(cxyz.inputs[1], sy); B.set(cxyz.inputs[2], z)
vr = B.node("ShaderNodeVectorRotate")
vr.rotation_type = "Z_AXIS"
B.set(vr.inputs["Vector"], cxyz.outputs[0])
B.set(vr.inputs["Angle"], B.math("MULTIPLY", tau, -1.0))
q = vr.outputs[0]
qx, qy, qz = B.sep(q)
rr = B.math("ADD", 0.09, B.math("MULTIPLY", B.math("POWER", B.math("MAXIMUM", z, 0.0), 1.3), 0.55))
rho = B.math("DIVIDE", B.vmath("LENGTH", B.vmath("MULTIPLY", q, (1.0, 1.0, 0.0))), rr)
core = B.maprange(rho, 1.0, 0.3, 0.0, 1.0, "SMOOTHSTEP")
th = B.math("ARCTAN2", qy, qx)
helix = B.math("ADD", 0.55, B.math("MULTIPLY", B.math("SINE", B.math("ADD", B.math("MULTIPLY", th, 3.0), B.math("MULTIPLY", z, 14.0))), 0.45))
nz = B.node("ShaderNodeTexNoise")
B.set(nz.inputs["Vector"], B.vmath("MULTIPLY", q, (3.0, 3.0, 7.0)))
nz.inputs["Scale"].default_value, nz.inputs["Detail"].default_value = 1.6, 4.0
brk = B.maprange(nz.outputs["Fac"], 0.3, 0.7, 0.35, 1.25)
fade = B.math("MULTIPLY", B.maprange(z, 0.0, 0.06, 0.0, 1.0, "SMOOTHSTEP"), B.maprange(z, 1.0, 0.72, 0.0, 1.0, "SMOOTHSTEP"))
skirt = B.math("MULTIPLY", B.maprange(z, 0.12, 0.02, 0.0, 1.0, "SMOOTHSTEP"),
               B.maprange(B.vmath("LENGTH", B.vmath("MULTIPLY", q, (1.0, 1.0, 0.0))), 0.55, 0.12, 0.0, 0.7, "SMOOTHSTEP"))
dens = B.math("MULTIPLY", B.math("ADD", B.math("MULTIPLY", B.math("MULTIPLY", core, helix), fade), B.math("MULTIPLY", skirt, nz.outputs["Fac"])), brk)
pv = B.node("ShaderNodeVolumePrincipled")
pv.inputs["Color"].default_value = (*srgb("#CFA27A"), 1)
pv.inputs["Anisotropy"].default_value = 0.45
B.set(pv.inputs["Density"], B.math("MULTIPLY", dens, 2.2))
nt.links.new(pv.outputs[0], B.node("ShaderNodeOutputMaterial").inputs["Volume"])
dev.data.materials.append(vm)
dev.visible_shadow = False
sc["v2_devil"] = {"pos": list(dp)}
print("AIRLOCK", [round(v, 2) for v in door_center], "FLAG", round(fx, 2), round(fy, 2), "DEVIL", [round(v, 1) for v in dp])
bpy.ops.wm.save_mainfile()
print("SAVED", D.filepath)
