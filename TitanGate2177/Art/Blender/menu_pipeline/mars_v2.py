# MARS menu scene v2 (premium clay, Cycles). Builds Art/Blender/mars_keyart_sunset_v2.blend from mars_keyart_sunset.blend
# (first save is a Save As; the sunset v1 file is not modified).
# blender -b Art/Blender/mars_keyart_sunset.blend --python Art/Blender/menu_pipeline/mars_v2.py
import bpy, bmesh, sys, os, math, random
from mathutils import Vector, Matrix, Euler, noise
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from nb import NB, srgb
import clay as C
import sculpt as S
import cadet_v2
import idle

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
SRC = os.path.join(ROOT, "Art", "Blender", "mars_keyart_sunset.blend")
DST = os.path.join(ROOT, "Art", "Blender", "mars_keyart_sunset_v2.blend")
assert os.path.normcase(os.path.abspath(bpy.data.filepath)) == os.path.normcase(SRC), bpy.data.filepath
bpy.ops.wm.save_as_mainfile(filepath=DST)
D = bpy.data
sc = bpy.context.scene
rad = math.radians
for _n in idle.RIG:
    D.objects[_n].animation_data_clear()
sc.frame_set(1)
dev = C.cycles_setup(sc, samples=64)
print("DEVICE", dev)

SUN = Vector(sc["menu_sun_disc_dir"])
haze = C.haze_group(SUN, "#5C4440", "#B88A60", 9.0, blue_hex="#8FB4E6", blue_exp=260.0, blue_k=0.45, start=12.0, scale=70.0, fog_max=0.90)

# ------------------------------------------------------------------ environment materials
def base_of(m):
    if not m or not m.node_tree:
        return None
    for n in m.node_tree.nodes:
        if n.type == "MIX" and n.blend_type == "MULTIPLY" and not n.inputs[6].is_linked:
            c = n.inputs[6].default_value
            return (c[0], c[1], c[2])
    return None


def ripples(B):
    tc = B.node("ShaderNodeTexCoord").outputs["Object"]
    p = B.vmath("MULTIPLY", tc, (1.0, 1.0, 0.0))
    wv = B.node("ShaderNodeTexWave")
    B.set(wv.inputs["Vector"], B.vmath("MULTIPLY", p, (0.55, 1.0, 1.0)))
    wv.inputs["Scale"].default_value, wv.inputs["Distortion"].default_value = 2.6, 7.0
    wv.inputs["Detail"].default_value, wv.inputs["Detail Scale"].default_value = 3.0, 0.8
    wv.bands_direction = "X"
    big = B.node("ShaderNodeTexNoise")
    B.set(big.inputs["Vector"], p)
    big.inputs["Scale"].default_value = 0.09
    fade = B.maprange(big.outputs["Fac"], 0.35, 0.6, 0.25, 1.0)
    return B.math("MULTIPLY", B.math("SUBTRACT", wv.outputs["Fac"], 0.5), B.math("MULTIPLY", fade, 2.2))


def sand_color(B, col):
    tc = B.node("ShaderNodeTexCoord").outputs["Object"]
    n = B.node("ShaderNodeTexNoise")
    B.set(n.inputs["Vector"], tc)
    n.inputs["Scale"].default_value, n.inputs["Detail"].default_value = 0.06, 5.0
    return B.mix(B.maprange(n.outputs["Fac"], 0.42, 0.62, 0.0, 0.45), col, srgb("#5A2010"))


m_sand = C.clay("v2_sand", (0.42, 0.10, 0.030), rough=0.9, sss=0.04, size=4.0, thumbs=0.0, tool=0.0, grain=0.6, bump=0.22,
                sheen=0.0, haze=haze, extra_height=ripples, extra_color=sand_color, spec=0.15)


def strata(B):
    tc = B.node("ShaderNodeTexCoord").outputs["Object"]
    wv = B.node("ShaderNodeTexWave")
    B.set(wv.inputs["Vector"], tc)
    wv.bands_direction = "Z"
    wv.inputs["Scale"].default_value, wv.inputs["Distortion"].default_value, wv.inputs["Detail"].default_value = 1.4, 4.0, 4.0
    return B.math("MULTIPLY", B.math("SUBTRACT", wv.outputs["Fac"], 0.5), 0.8)


m_ridge = C.clay("v2_ridge", (0.27, 0.085, 0.045), rough=0.9, sss=0.0, size=30.0, thumbs=0.4, tool=0.6, bump=0.25, haze=haze,
                 haze_amount=1.25, extra_height=lambda B: B.math("MULTIPLY", strata(B), 0.4))
m_rock = C.clay("v2_rock", (0.15, 0.045, 0.026), rough=0.82, sss=0.06, size=1.2, thumbs=0.6, tool=0.4, bump=0.45, dust=0.55,
                dust_hex="#8A3A1A", ao_dist=0.2, haze=haze, extra_height=lambda B: B.math("MULTIPLY", strata(B), 0.6))


def pebble_color(B, col):
    oi = B.node("ShaderNodeObjectInfo")
    return B.mix(B.maprange(oi.outputs["Random"], 0.0, 1.0, 0.0, 0.85), col, srgb("#6A3826"))


m_pebble = C.clay("v2_pebble", (0.11, 0.035, 0.022), rough=0.8, sss=0.05, size=0.15, thumbs=0.5, tool=0.2, bump=0.4,
                  haze=haze, extra_color=pebble_color)
m_rockfg = C.clay("v2_rock_fg", (0.022, 0.010, 0.008), rough=0.9, sss=0.0, size=0.8, thumbs=0.8, tool=0.5, bump=0.5, sheen=0.0, spec=0.15, haze=None)


def hab_detail(B):
    """panel seams (rings + longitudinal), rivet rows beside the ring seams (object space: hab axis = local Z)"""
    tc = B.node("ShaderNodeTexCoord").outputs["Object"]
    x, y, z = B.sep(tc)
    ring = B.math("ABSOLUTE", B.math("SUBTRACT", B.math("FRACT", B.math("MULTIPLY", z, 1.0 / 1.25)), 0.5))
    ring_g = B.maprange(ring, 0.5, 0.488, 1.0, 0.0, "SMOOTHSTEP")
    ang = B.math("ARCTAN2", y, x)
    lon = B.math("ABSOLUTE", B.math("SUBTRACT", B.math("FRACT", B.math("MULTIPLY", ang, 8 / (2 * math.pi))), 0.5))
    lon_g = B.maprange(lon, 0.5, 0.488, 1.0, 0.0, "SMOOTHSTEP")
    seam = B.math("MAXIMUM", ring_g, lon_g)
    # rivets: dots every ~7.5 cm beside each ring seam
    riv_a = B.math("ABSOLUTE", B.math("SUBTRACT", B.math("FRACT", B.math("MULTIPLY", ang, 64 / (2 * math.pi))), 0.5))
    riv_z = B.math("ABSOLUTE", B.math("SUBTRACT", ring, 0.468))
    rivet = B.math("MULTIPLY", B.maprange(riv_a, 0.16, 0.05, 0.0, 1.0, "SMOOTHSTEP"), B.maprange(riv_z, 0.012, 0.0, 0.0, 1.0, "SMOOTHSTEP"))
    return B.math("ADD", B.math("MULTIPLY", seam, -1.0), B.math("MULTIPLY", rivet, 0.7))


m_module = C.clay("v2_module", (0.60, 0.545, 0.44), rough=0.55, sss=0.15, sss_radius=(1.0, 0.85, 0.7), size=1.0, thumbs=0.6, tool=0.6,
                  bump=0.5, dust=0.45, dust_hex="#7A3A1E", ao_dist=0.35, haze=haze, extra_height=hab_detail, height_dust=(0.0, 1.9, 0.75))


def scorch(B, col):
    geo = B.node("ShaderNodeNewGeometry")
    tc = B.node("ShaderNodeTexCoord").outputs["Object"]
    n = B.node("ShaderNodeTexNoise")
    B.set(n.inputs["Vector"], B.vmath("MULTIPLY", tc, (6.0, 6.0, 0.7)))
    n.inputs["Scale"].default_value, n.inputs["Detail"].default_value = 2.0, 6.0
    streak = B.maprange(n.outputs["Fac"], 0.48, 0.7, 0.0, 1.0)
    low = B.maprange(B.sep(geo.outputs["Position"])[2], 1.6, -0.2, 0.15, 1.0)
    m = B.math("MULTIPLY", streak, low)
    col = B.mix(m, col, srgb("#2A160E"))
    return B.mix(B.math("MULTIPLY", B.maprange(n.outputs["Fac"], 0.3, 0.45, 1.0, 0.0), 0.25), col, srgb("#E0B080"))


m_lifeboat = C.clay("v2_lifeboat", (0.70, 0.20, 0.045), rough=0.6, sss=0.18, size=1.2, thumbs=0.8, tool=0.6, bump=0.5, dust=0.5,
                    dust_hex="#6A2A12", ao_dist=0.3, haze=haze, extra_color=scorch, height_dust=(-0.6, 0.9, 0.6))
m_port = C.clay("v2_porthole", (0.02, 0.03, 0.05), rough=0.08, sss=0.0, spec=0.9, size=0.3, thumbs=0.0, tool=0.0, haze=haze)
m_metal = C.clay("v2_metal", (0.28, 0.30, 0.35), rough=0.45, sss=0.0, metallic=0.4, size=0.5, dust=0.5, dust_hex="#7A3A1E", ao_dist=0.15, haze=haze)
m_mast = C.clay("v2_mast", (0.06, 0.055, 0.06), rough=0.55, sss=0.0, size=0.5, dust=0.4, dust_hex="#6A3A22", ao_dist=0.1, haze=haze)
m_cable = C.clay("v2_cable", (0.04, 0.035, 0.04), rough=0.6, sss=0.0, size=0.2, thumbs=0.2, haze=haze)


def solar_mat():
    m = D.materials.new("v2_solar")
    m.use_nodes = True if not m.node_tree else m.use_nodes
    nt = m.node_tree
    nt.nodes.clear()
    B = NB(nt)
    tc = B.node("ShaderNodeTexCoord").outputs["Generated"]
    gx, gy, gz = B.sep(tc)

    def line(v, n, w):
        d = B.math("ABSOLUTE", B.math("SUBTRACT", B.math("FRACT", B.math("MULTIPLY", v, n)), 0.5))
        return B.maprange(d, 0.5 - w * 1.6, 0.5 - w, 0.0, 1.0, "SMOOTHSTEP")
    grid = B.math("MAXIMUM", line(gx, 6.0, 0.03), line(gy, 4.0, 0.045))
    dn = B.node("ShaderNodeTexNoise")
    B.set(dn.inputs["Vector"], B.node("ShaderNodeTexCoord").outputs["Object"])
    dn.inputs["Scale"].default_value, dn.inputs["Detail"].default_value = 3.5, 6.0
    dust = B.maprange(dn.outputs["Fac"], 0.42, 0.7, 0.15, 0.75)
    cell = B.node("ShaderNodeBsdfPrincipled")
    cell.inputs["Base Color"].default_value = (*srgb("#0A1426"), 1)
    cell.inputs["Roughness"].default_value = 0.12
    cell.inputs["Specular IOR Level"].default_value = 0.8
    frame = B.node("ShaderNodeBsdfPrincipled")
    frame.inputs["Base Color"].default_value = (*srgb("#9A9CA4"), 1)
    frame.inputs["Roughness"].default_value = 0.45
    frame.inputs["Metallic"].default_value = 0.5
    dustb = B.node("ShaderNodeBsdfPrincipled")
    dustb.inputs["Base Color"].default_value = (*srgb("#A8684A"), 1)
    dustb.inputs["Roughness"].default_value = 0.9
    m1 = B.node("ShaderNodeMixShader")
    B.set(m1.inputs[0], grid)
    nt.links.new(cell.outputs[0], m1.inputs[1])
    nt.links.new(frame.outputs[0], m1.inputs[2])
    m2 = B.node("ShaderNodeMixShader")
    B.set(m2.inputs[0], dust)
    nt.links.new(m1.outputs[0], m2.inputs[1])
    nt.links.new(dustb.outputs[0], m2.inputs[2])
    hg = B.node("ShaderNodeGroup")
    hg.node_tree = haze
    nt.links.new(m2.outputs[0], hg.inputs["Shader"])
    nt.links.new(hg.outputs[0], B.node("ShaderNodeOutputMaterial").inputs["Surface"])
    return m


def dome_mat():
    m = D.materials.new("v2_dome")
    m.use_nodes = True if not m.node_tree else m.use_nodes
    nt = m.node_tree
    nt.nodes.clear()
    B = NB(nt)
    tc = B.node("ShaderNodeTexCoord").outputs["Generated"]
    ox, oy, oz = B.sep(tc)

    def line(v, n, w):
        d = B.math("ABSOLUTE", B.math("SUBTRACT", B.math("FRACT", B.math("MULTIPLY", v, n)), 0.5))
        return B.maprange(d, 0.5 - w * 1.6, 0.5 - w, 0.0, 1.0, "SMOOTHSTEP")
    ang = B.math("ARCTAN2", B.math("SUBTRACT", oy, 0.5), B.math("SUBTRACT", ox, 0.5))
    ribs = B.math("MAXIMUM", line(B.math("DIVIDE", ang, 2 * math.pi), 14.0, 0.035), line(oz, 9.0, 0.045))
    g = B.node("ShaderNodeBsdfPrincipled")
    g.inputs["Base Color"].default_value = (*srgb("#C8D8C0"), 1)
    g.inputs["Transmission Weight"].default_value = 0.85
    g.inputs["Roughness"].default_value = 0.18
    glow = B.maprange(oz, 0.62, 0.95, 1.0, 0.0, "SMOOTHSTEP")
    B.set(g.inputs["Emission Color"], srgb("#FF9A48"))
    B.set(g.inputs["Emission Strength"], B.math("MULTIPLY", glow, 1.2))
    rib = B.node("ShaderNodeBsdfPrincipled")
    rib.inputs["Base Color"].default_value = (*srgb("#CFC3AE"), 1)
    rib.inputs["Roughness"].default_value = 0.55
    ms = B.node("ShaderNodeMixShader")
    B.set(ms.inputs[0], ribs)
    nt.links.new(g.outputs[0], ms.inputs[1])
    nt.links.new(rib.outputs[0], ms.inputs[2])
    hg = B.node("ShaderNodeGroup")
    hg.node_tree = haze
    nt.links.new(ms.outputs[0], hg.inputs["Shader"])
    nt.links.new(hg.outputs[0], B.node("ShaderNodeOutputMaterial").inputs["Surface"])
    return m


SPECIAL = {"sand": m_sand, "sandD": m_ridge, "rock": m_rock, "rock_fg": m_rockfg, "module": m_module, "moduleD": m_module,
           "lifeboat": m_lifeboat, "glass": m_port, "metal": m_metal, "mast_dark": m_mast, "solar_menu": solar_mat(), "dome_glass": dome_mat()}
CADET = {o.name for o in D.collections["cadet"].all_objects}
generic = {}
for o in sc.objects:
    if o.type not in ("MESH", "CURVE") or o.name in CADET:
        continue
    for i, slot in enumerate(o.material_slots):
        m = slot.material
        if m is None:
            continue
        if m.name in SPECIAL:
            slot.material = SPECIAL[m.name]
            continue
        has_toon = m.node_tree and any(n.type == "SHADERTORGB" for n in m.node_tree.nodes)
        if not has_toon:
            continue                      # emission lamps / windows / beacons stay as they are
        if m.name not in generic:
            generic[m.name] = C.clay("v2_" + m.name, base_of(m) or (0.3, 0.3, 0.3), rough=0.6, sss=0.12, size=0.6, haze=haze, dust=0.3, ao_dist=0.1)
        slot.material = generic[m.name]
print("generic converted", list(generic))

# ------------------------------------------------------------------ cadet (premium clay + bubble helmet + dust)
cadet_v2.upgrade(helmet=True, mars=True, haze=haze, sun_dir=SUN)
# soft warm fill from the camera side, light-linked to the cadet only (stop-motion style key-fill on a backlit puppet)
_body = D.objects["Body"].matrix_world.translation
_to_cam = (sc.camera.matrix_world.translation - _body)
_to_cam.z = 0
_to_cam.normalize()
_left = Vector((-_to_cam.y, _to_cam.x, 0.0))
fl = D.lights.new("v2_cadet_fill", "AREA")
fl.shape, fl.size, fl.energy, fl.color = "DISK", 1.8, 125.0, (1.0, 0.86, 0.72)
flo = D.objects.new("v2_cadet_fill", fl)
sc.collection.objects.link(flo)
flo.location = _body + _to_cam * 4.0 + _left * 1.2 + Vector((0, 0, 1.6))
flo.rotation_mode = "QUATERNION"
flo.rotation_quaternion = (_body + Vector((0, 0, 0.6)) - flo.location).to_track_quat("-Z", "Y")
flo.visible_camera = False
flo.visible_glossy = False          # no disc reflection in the helmet glass
flo.light_linking.receiver_collection = D.collections["cadet"]

# ------------------------------------------------------------------ sculpted pebble scatter in front of the camera
pebbles = D.collections.get("v2_pebble_protos") or D.collections.new("v2_pebble_protos")
for i in range(7):
    random.seed(i)
    def bmb(bm, i=i):
        res = bmesh.ops.create_uvsphere(bm, u_segments=32, v_segments=16, radius=0.04)
        sx, sy, sz = 1.0 + 0.6 * random.random(), 0.8 + 0.4 * random.random(), 0.45 + 0.3 * random.random()
        for v in res["verts"]:
            v.co = Vector((v.co.x * sx, v.co.y * sy, v.co.z * sz))
            v.co += v.co.normalized() * 0.010 * noise.noise(v.co * 16 + Vector((i * 2.3, i, 0)))
    o = S.organic(f"pebble{i}", bmb, pebbles, m_pebble, voxel=0.0035, smooth=4, noise_amp=0.0015, noise_scale=40.0, seed=i * 3.1)
cam = sc.camera
ground = D.objects["ground"]
patch = ground.copy()
patch.data = ground.data.copy()
patch.name = "v2_pebble_patch"
sc.collection.objects.link(patch)
cpos = cam.matrix_world.translation
fwd = (cam.matrix_world.to_3x3() @ Vector((0, 0, -1)))
fwd.z = 0
fwd.normalize()
bm = bmesh.new()
bm.from_mesh(patch.data)
mw = patch.matrix_world
kill = []
for f in bm.faces:
    c = mw @ f.calc_center_median()
    d = c - cpos
    d.z = 0
    dist = d.length
    if dist < 1.0 or dist > 32.0:
        kill.append(f)
        continue
    if abs(math.degrees(fwd.angle(d.normalized(), 0.0))) > 26:
        kill.append(f)
bmesh.ops.delete(bm, geom=kill, context="FACES")
bm.to_mesh(patch.data)
bm.free()
patch.data.materials.clear()
gn = D.node_groups.new("v2_scatter", "GeometryNodeTree")
gn.interface.new_socket("Geometry", in_out="INPUT", socket_type="NodeSocketGeometry")
gn.interface.new_socket("Geometry", in_out="OUTPUT", socket_type="NodeSocketGeometry")
N = gn.nodes
gi, go = N.new("NodeGroupInput"), N.new("NodeGroupOutput")
dist = N.new("GeometryNodeDistributePointsOnFaces")
dist.inputs["Density"].default_value = 2.6
dist.inputs["Seed"].default_value = 11
ci = N.new("GeometryNodeCollectionInfo")
ci.inputs["Collection"].default_value = pebbles
ci.inputs["Separate Children"].default_value = True
ci.inputs["Reset Children"].default_value = True
iop = N.new("GeometryNodeInstanceOnPoints")
iop.inputs["Pick Instance"].default_value = True
rr = N.new("FunctionNodeRandomValue")
rr.data_type = "FLOAT_VECTOR"
next(i for i in rr.inputs if i.name == "Max" and i.type == "VECTOR").default_value = (6.283, 6.283, 6.283)
rs = N.new("FunctionNodeRandomValue")
rs.data_type = "FLOAT"
_fl = [i for i in rs.inputs if i.type == "VALUE" and i.name in ("Min", "Max")]
_fl[0].default_value, _fl[1].default_value = 0.0, 1.0
rs.inputs["Seed"].default_value = 5
pw = N.new("ShaderNodeMath")
pw.operation = "POWER"
pw.inputs[1].default_value = 3.0
sc_m = N.new("ShaderNodeMath")
sc_m.operation = "MULTIPLY_ADD"
sc_m.inputs[1].default_value, sc_m.inputs[2].default_value = 1.6, 0.3
L = gn.links
L.new(gi.outputs[0], dist.inputs["Mesh"])
L.new(dist.outputs["Points"], iop.inputs["Points"])
L.new(ci.outputs[0], iop.inputs["Instance"])
L.new(next(o for o in rr.outputs if o.type == "VECTOR"), iop.inputs["Rotation"])
L.new(next(o for o in rs.outputs if o.type == "VALUE"), pw.inputs[0])
L.new(pw.outputs[0], sc_m.inputs[0])
L.new(sc_m.outputs[0], iop.inputs["Scale"])
L.new(iop.outputs[0], go.inputs[0])
md = patch.modifiers.new("scatter", "NODES")
md.node_group = gn

# ------------------------------------------------------------------ lifeboat: draped parachute (cloth sim) + suspension lines
lb = D.objects["lifeboat"]
dg = bpy.context.evaluated_depsgraph_get()
lbm = lb.evaluated_get(dg).to_mesh()
lvs = [lb.matrix_world @ v.co for v in lbm.vertices]
lb_min = Vector([min(v[i] for v in lvs) for i in range(3)])
lb_max = Vector([max(v[i] for v in lvs) for i in range(3)])
lb.evaluated_get(dg).to_mesh_clear()
for o in (lb, ground):
    if not any(m.type == "COLLISION" for m in o.modifiers):
        o.modifiers.new("collide", "COLLISION")
        o.collision.thickness_outer = 0.02
bpy.ops.mesh.primitive_grid_add(x_subdivisions=44, y_subdivisions=34, size=1.0)
chute = bpy.context.active_object
chute.name = "v2_parachute"
chute.scale = (3.6, 2.8, 1.0)
chute.location = (lb_max.x - 0.2, (lb_min.y + lb_max.y) / 2 + 0.5, lb_max.z + 0.6)
chute.rotation_euler = (rad(8), rad(-14), rad(25))
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
cl = chute.modifiers.new("cloth", "CLOTH")
cl.settings.quality = 6
cl.settings.mass = 0.2
cl.settings.tension_stiffness = cl.settings.compression_stiffness = 8.0
cl.settings.bending_stiffness = 0.05
cl.collision_settings.distance_min = 0.012
cl.collision_settings.use_self_collision = False
cl.point_cache.frame_start, cl.point_cache.frame_end = 1, 70
for f in range(1, 71):
    sc.frame_set(f)
dg = bpy.context.evaluated_depsgraph_get()
me = D.meshes.new_from_object(chute.evaluated_get(dg), depsgraph=dg)
chute.modifiers.clear()
chute.data = me
sc.frame_set(1)
for o in (lb, ground):
    for m in list(o.modifiers):
        if m.type == "COLLISION":
            o.modifiers.remove(m)
sol = chute.modifiers.new("thick", "SOLIDIFY")
sol.thickness = 0.012
sub = chute.modifiers.new("sub", "SUBSURF")
sub.levels = sub.render_levels = 1
for p in chute.data.polygons:
    p.use_smooth = True


def gores(B, col):
    tc = B.node("ShaderNodeTexCoord").outputs["Generated"]
    gx = B.sep(tc)[0]
    band = B.maprange(B.math("SINE", B.math("MULTIPLY", gx, 2 * math.pi * 6)), -0.1, 0.1, 0.0, 1.0)
    return B.mix(band, col, srgb("#C8562A"))


m_chute = C.clay("v2_chute", "#E4D8C2", rough=0.7, sss=0.2, size=0.6, thumbs=0.3, tool=0.2, bump=0.3, dust=0.4, dust_hex="#9A5A3A",
                 ao_dist=0.2, haze=haze, extra_color=gores, height_dust=(-0.5, 0.6, 0.7))
chute.data.materials.clear()
chute.data.materials.append(m_chute)
for c in list(chute.users_collection):
    c.objects.unlink(chute)
D.collections["lifeboat"].objects.link(chute)
for k in range(5):                                          # suspension lines from the canopy edge to the pod top
    a = Vector((lb_max.x - 0.4, lb_min.y + 0.5 + 0.45 * k, lb_max.z - 0.1))
    vs = [chute.matrix_world @ v.co for v in chute.data.vertices]
    b = min(vs, key=lambda v: (v - a).length + 2.0 * abs(v.z - lb_max.z))
    path = S.catmull([a, (a + b) / 2 + Vector((0, 0, 0.25)), b], 10)
    S.tube(f"v2_chute_line{k}", path, lambda t, L: 0.008, D.collections["lifeboat"], m_cable, seg=6)

# ------------------------------------------------------------------ antenna cabling
mast = D.objects["Cylinder.004"]
mt = mast.matrix_world.translation
top = D.objects["beacon"].matrix_world.translation
pts = []
for i in range(14):
    t = i / 13
    a = t * 6 * math.pi
    pts.append(Vector((mt.x + 0.17 * math.cos(a), mt.y + 0.17 * math.sin(a), top.z - 0.15 - t * (top.z - 0.2))))
hc = D.objects["hab_c_cap"].matrix_world.translation
gz = 0.05
pts += [Vector((mt.x - 0.8, mt.y - 0.6, gz + 0.05)), Vector((hc.x + 0.6, hc.y - 1.6, gz + 0.06)), Vector((hc.x, hc.y - 1.2, 0.6))]
S.tube("v2_antenna_cable", S.catmull(pts, 8), lambda t, L: 0.03, D.collections["base"], m_cable, seg=8)

# ------------------------------------------------------------------ lights: low sun (key), cool blue aureole rim, sky fill
sun = D.objects["Sun"]
sun.data.energy = 2.6
sun.data.angle = rad(1.6)
sun.data.color = (1.0, 0.82, 0.64)
VIEW_AZ = math.degrees(math.atan2(fwd.y, fwd.x))
rim_dir = Vector((math.cos(rad(VIEW_AZ - 55)) * math.cos(rad(16)), math.sin(rad(VIEW_AZ - 55)) * math.cos(rad(16)), math.sin(rad(16))))
ld = D.lights.new("v2_rim_blue", "SUN")
ld.energy, ld.color, ld.angle = 3.4, (0.55, 0.72, 1.0), rad(8)
rl = D.objects.new("v2_rim_blue", ld)
sc.collection.objects.link(rl)
rl.rotation_mode = "QUATERNION"
rl.rotation_quaternion = (-rim_dir).to_track_quat("-Z", "Y")
w = sc.world
for n in w.node_tree.nodes:
    if n.type == "BACKGROUND" and not n.inputs["Color"].is_linked:      # non-camera branch: dusky sky light
        n.inputs["Color"].default_value = (*srgb("#4A3A44"), 1)
        n.inputs["Strength"].default_value = 0.9
for o in sc.objects:                                         # Eevee-only helpers off
    if o.type == "LIGHT" and o.name.endswith("_spill"):
        o.data.energy = 18.0
sc.view_settings.exposure = -0.2
cam.data.dof.aperture_fstop = 2.4
ridge = D.objects["ridge"]
if not any(m.type == "SUBSURF" for m in ridge.modifiers):
    sm = ridge.modifiers.new("v2_sub", "SUBSURF")
    sm.levels = sm.render_levels = 2
    er = ridge.modifiers.new("v2_erode", "DISPLACE")
    tx = D.textures.new("v2_erosion", "CLOUDS")
    tx.noise_scale, tx.noise_depth = 2.5, 4
    er.texture, er.strength, er.texture_coords = tx, 0.9, "LOCAL"
for p in ridge.data.polygons:
    p.use_smooth = True
ch = D.objects["v2_parachute"]
from bpy_extras.object_utils import world_to_camera_view
vs = [ch.matrix_world @ v.co for v in ch.data.vertices]
pr = [world_to_camera_view(sc, cam, v) for v in vs]
print("CHUTE screen x", round(min(p.x for p in pr), 3), round(max(p.x for p in pr), 3), "y", round(1 - max(p.y for p in pr), 3), round(1 - min(p.y for p in pr), 3), "zmin", round(min(v.z for v in vs), 2))
bpy.ops.wm.save_mainfile()
print("SAVED", D.filepath)
