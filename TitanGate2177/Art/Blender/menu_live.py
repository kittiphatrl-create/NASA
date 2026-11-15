# LAST DOCK main menu, live 3D version: exports what Unity needs to draw the menu scene in real time,
# so the player can turn and zoom the ship.
#   - the Kestrel (decimated), with baked albedo, emission and tangent-space normal maps
#   - three asteroid meshes with baked albedo
#   - a UV sphere plus baked Mars and Phobos surface maps
#   - the sky as an equirectangular panorama (galaxy band, warm core and a nebula on the left; the stars are
#     drawn sharp in the Unity shader, and Mars and Phobos are real spheres there)
#   - menu_live.json: camera, lights, planet placement, engine and light positions, asteroid layout
# It builds the same scene as menu_space.py (the looping video), so the default view matches the video.
# Run:  blender -b --factory-startup --python Art/Blender/menu_live.py
# Output: Art/Blender/renders/menu/live/*  (copy_live.sh copies them into Assets/Resources/MenuLive/)
# Unity coordinates are Blender (x, z, y); triangle winding is reversed for the handedness change.
import bpy, bmesh, math, os, struct, json, random, subprocess
from mathutils import Vector, Matrix

BL = r"F:\NASA\TitanGate2177\Art\Blender"
OUT = BL + r"\renders\menu\live"
os.makedirs(OUT, exist_ok=True)
g = {'__name__': 'menu_space_mod'}
exec(open(BL + r"\menu_space.py", encoding='utf-8').read(), g)
cb, A, M = g['cb'], g['A'], g['M']

def U(v): return [round(v[0], 5), round(v[2], 5), round(v[1], 5)]
def Uq(q):   # Blender quaternion -> Unity (x, y, z, w); the y/z swap is a reflection
    return [round(-q.x, 6), round(-q.z, 6), round(-q.y, 6), round(q.w, 6)]

# ------------------------------------------------------------------ the scene (same as the video)
sc = g['setup']()
sc.frame_set(1)
cm = sc.camera; tgt = bpy.data.objects['menu_target']
cam_loc = cm.location.copy()                       # keyed location at frame 1 (its rotation comes from a Track To)
cam_rel = cam_loc - tgt.location
dist = cam_rel.length
yaw = math.degrees(math.atan2(cam_rel.y, cam_rel.x)); pitch = math.degrees(math.asin(cam_rel.z / dist))
hfov = math.degrees(2 * math.atan(cm.data.sensor_width / 2 / cm.data.lens))
fwd = (tgt.location - cam_loc).normalized(); right = fwd.cross(Vector((0, 0, 1))).normalized(); up = right.cross(fwd)
mars = bpy.data.objects['menu_space_mars']; phob = bpy.data.objects.get('menu_space_moonP')
sun = next(o for o in sc.objects if o.type == 'LIGHT' and o.data.type == 'SUN')
sun_dir = (sun.rotation_euler.to_matrix() @ Vector((0, 0, 1))).normalized()   # toward the sun (rotation, not matrix_world: that scene is not evaluated)
fill = next(o for o in sc.objects if o.type == 'LIGHT' and o.name.endswith('fill'))
bounce = next(o for o in sc.objects if o.type == 'LIGHT' and o.name.endswith('marsBounce'))

# ------------------------------------------------------------------ mesh writer
def write_mesh(ob, path, matrix=None):
    """Binary: 'MLM1', nverts, nindices, then positions, normals, uvs (floats) and int indices."""
    dg = bpy.context.evaluated_depsgraph_get()
    ev = ob.evaluated_get(dg); me = ev.to_mesh()
    bm = bmesh.new(); bm.from_mesh(me); bmesh.ops.triangulate(bm, faces=bm.faces[:]); bm.to_mesh(me); bm.free()
    mx = matrix or Matrix.Identity(4); nx = mx.to_3x3().inverted().transposed()
    try: cn = me.corner_normals
    except AttributeError: me.calc_normals_split(); cn = None
    uvl = me.uv_layers.active.data if me.uv_layers.active else None
    key2i, P, N, T, I = {}, [], [], [], []
    for poly in me.polygons:
        tri = []
        for li in poly.loop_indices:
            vi = me.loops[li].vertex_index
            n = cn[li].vector if cn is not None else me.loops[li].normal
            uv = uvl[li].uv if uvl else (0, 0)
            k = (vi, round(n.x, 3), round(n.y, 3), round(n.z, 3), round(uv[0], 5), round(uv[1], 5))
            if k not in key2i:
                key2i[k] = len(P)
                P.append(U(mx @ me.vertices[vi].co)); N.append(U((nx @ n).normalized())); T.append([uv[0], uv[1]])
            tri.append(key2i[k])
        I += [tri[0], tri[2], tri[1]]
    with open(path, 'wb') as f:
        f.write(b'MLM1'); f.write(struct.pack('<ii', len(P), len(I)))
        for arr in (P, N, T):
            for v in arr: f.write(struct.pack('<%df' % len(v), *v))
        f.write(struct.pack('<%di' % len(I), *I))
    ev.to_mesh_clear()
    print('MESH', os.path.basename(path), len(P), 'verts', len(I) // 3, 'tris')

# ------------------------------------------------------------------ baking
bk = bpy.context.scene            # the start-up scene does the baking (operators work on the context scene)
# (the asset library lives in this scene too; a bake only touches the selected object, so it stays)
bk.render.engine = 'CYCLES'; bk.cycles.samples = 8
try: bk.cycles.device = 'GPU'
except Exception: pass
bk.view_settings.view_transform = 'Standard'
bk.render.bake.margin = 12

def into_bake(src, name, decimate=None):
    o = bpy.data.objects.new(name, src.data.copy()); bk.collection.objects.link(o)
    bpy.context.view_layer.objects.active = o
    for x in bk.objects: x.select_set(x == o)
    if decimate:
        md = o.modifiers.new('dec', 'DECIMATE'); md.ratio = decimate
        bpy.ops.object.modifier_apply(modifier='dec')
    return o

def unwrap(o, margin=0.003):
    bpy.context.view_layer.objects.active = o
    for x in bk.objects: x.select_set(x == o)
    for l in list(o.data.uv_layers): o.data.uv_layers.remove(l)
    o.data.uv_layers.new(name='UV')
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=margin)
    bpy.ops.object.mode_set(mode='OBJECT')

def bake(o, kind, w, h, path):
    bpy.context.view_layer.objects.active = o
    for x in bk.objects: x.select_set(x == o)
    img = bpy.data.images.new(os.path.basename(path), w, h, alpha=False)
    if kind == 'normal': img.colorspace_settings.name = 'Non-Color'
    for m in o.data.materials:
        nt = m.node_tree
        n = nt.nodes.get('BAKE_T') or nt.nodes.new('ShaderNodeTexImage'); n.name = 'BAKE_T'
        n.image = img; nt.nodes.active = n
    if kind == 'albedo': bpy.ops.object.bake(type='DIFFUSE', pass_filter={'COLOR'}, use_clear=True)
    elif kind == 'emit': bpy.ops.object.bake(type='EMIT', use_clear=True)
    elif kind == 'normal': bpy.ops.object.bake(type='NORMAL', normal_space='TANGENT', use_clear=True)
    png = path + '.png'
    img.filepath_raw = png; img.file_format = 'PNG'; img.save()
    # store as JPEG (smaller); the menu decodes it with Texture2D.LoadImage
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', png, '-q:v', '2', path + '.jpg'], check=True)
    os.remove(png)
    print('BAKED', os.path.basename(path), kind)

# the ship: the scene copy carries the textured materials
ship_src = bpy.data.objects['menu_space_ship']
ship = into_bake(ship_src, 'live_ship', decimate=0.36)
ship.matrix_world = Matrix.Identity(4)
unwrap(ship, 0.002)
bake(ship, 'albedo', 2048, 2048, OUT + r"\ship_albedo")
bake(ship, 'emit', 1024, 1024, OUT + r"\ship_emit")
bake(ship, 'normal', 2048, 2048, OUT + r"\ship_normal")
write_mesh(ship, OUT + r"\ship.mlm")

# asteroids
for i, k in enumerate(('debris1', 'debris3', 'debris5')):
    r = into_bake(A[k], 'live_rock%d' % i, decimate=0.6)
    unwrap(r, 0.01)
    bake(r, 'albedo', 512, 512, OUT + r"\rock%d_albedo" % i)
    write_mesh(r, OUT + r"\rock%d.mlm" % i)

# a UV sphere for Mars and Phobos (radius 1: the same object space as the planet meshes, so the noise matches)
bpy.ops.mesh.primitive_uv_sphere_add(segments=128, ring_count=64, radius=1.0)
sph = bpy.context.active_object
for c in sph.users_collection: c.objects.unlink(sph)
bk.collection.objects.link(sph)
for poly in sph.data.polygons: poly.use_smooth = True
sph.data.materials.append(bpy.data.materials['marsSurf'])
bake(sph, 'albedo', 2048, 1024, OUT + r"\mars_albedo")
sph.data.materials[0] = bpy.data.materials['moonS']
bake(sph, 'albedo', 512, 256, OUT + r"\phobos_albedo")
write_mesh(sph, OUT + r"\sphere.mlm")

# ------------------------------------------------------------------ sky panorama
def nebula(world, ndir):
    """A soft teal-and-violet nebula round direction ndir, added to the galaxy; the galaxy's own stars are removed."""
    nt = world.node_tree; N_ = lambda t: nt.nodes.new(t)
    ad0 = next(n for n in nt.nodes if n.type == 'ADD_SHADER')
    for l in list(ad0.inputs[1].links): nt.links.remove(l)
    d = N_('ShaderNodeTexCoord').outputs['Generated']
    dn = N_('ShaderNodeVectorMath'); dn.operation = 'NORMALIZE'; nt.links.new(d, dn.inputs[0])
    dot = N_('ShaderNodeVectorMath'); dot.operation = 'DOT_PRODUCT'; dot.inputs[1].default_value = ndir; nt.links.new(dn.outputs[0], dot.inputs[0])
    mr = N_('ShaderNodeMapRange'); mr.inputs['From Min'].default_value = 0.62; mr.inputs['From Max'].default_value = 1.0
    nt.links.new(dot.outputs['Value'], mr.inputs['Value'])
    pw = N_('ShaderNodeMath'); pw.operation = 'POWER'; pw.inputs[1].default_value = 1.6; nt.links.new(mr.outputs['Result'], pw.inputs[0])
    n1 = N_('ShaderNodeTexNoise'); n1.inputs['Scale'].default_value = 3.2; n1.inputs['Detail'].default_value = 14; n1.inputs['Roughness'].default_value = 0.68
    nt.links.new(dn.outputs[0], n1.inputs['Vector'])
    n2 = N_('ShaderNodeTexNoise'); n2.inputs['Scale'].default_value = 9.0; n2.inputs['Detail'].default_value = 10; n2.inputs['Roughness'].default_value = 0.7
    nt.links.new(dn.outputs[0], n2.inputs['Vector'])
    c1 = N_('ShaderNodeMapRange'); c1.inputs['From Min'].default_value = 0.45; c1.inputs['From Max'].default_value = 0.78; nt.links.new(n1.outputs['Fac'], c1.inputs['Value'])
    c2 = N_('ShaderNodeMapRange'); c2.inputs['From Min'].default_value = 0.35; c2.inputs['From Max'].default_value = 0.7
    c2.inputs['To Min'].default_value = 0.35; nt.links.new(n2.outputs['Fac'], c2.inputs['Value'])
    m1 = N_('ShaderNodeMath'); m1.operation = 'MULTIPLY'; nt.links.new(pw.outputs[0], m1.inputs[0]); nt.links.new(c1.outputs['Result'], m1.inputs[1])
    m2 = N_('ShaderNodeMath'); m2.operation = 'MULTIPLY'; nt.links.new(m1.outputs[0], m2.inputs[0]); nt.links.new(c2.outputs['Result'], m2.inputs[1])
    m3 = N_('ShaderNodeMath'); m3.operation = 'MULTIPLY'; m3.inputs[1].default_value = 0.55; nt.links.new(m2.outputs[0], m3.inputs[0])
    mx = N_('ShaderNodeMix'); mx.data_type = 'RGBA'; nt.links.new(n2.outputs['Fac'], mx.inputs[0])
    mx.inputs[6].default_value = (0.1, 0.55, 0.7, 1); mx.inputs[7].default_value = (0.62, 0.25, 0.85, 1)
    em = N_('ShaderNodeEmission'); nt.links.new(mx.outputs[2], em.inputs['Color']); nt.links.new(m3.outputs[0], em.inputs['Strength'])
    nt.links.new(em.outputs[0], ad0.inputs[1])

neb_dir = (fwd - right * 1.05 - up * 0.2).normalized()
pano = bpy.data.scenes.new('pano'); pano.world = sc.world
nebula(pano.world, neb_dir)
pano.render.engine = 'CYCLES'; pano.cycles.samples = 24
try: pano.cycles.use_denoising = True
except Exception: pass
pano.render.resolution_x, pano.render.resolution_y, pano.render.resolution_percentage = 4096, 2048, 100
pano.view_settings.view_transform = sc.view_settings.view_transform
pano.view_settings.look = sc.view_settings.look
pano.view_settings.exposure = sc.view_settings.exposure
pcd = bpy.data.cameras.new('pano_cam'); pcd.type = 'PANO'
try: pcd.panorama_type = 'EQUIRECTANGULAR'
except Exception: pcd.cycles.panorama_type = 'EQUIRECTANGULAR'
pc = bpy.data.objects.new('pano_cam', pcd); pano.collection.objects.link(pc); pano.camera = pc
pc.rotation_euler = (math.pi / 2, 0, -math.pi / 2)        # +X at u = 0.5, +Y at u = 0.25, +Z at the top
pano.render.image_settings.file_format = 'JPEG'; pano.render.image_settings.quality = 92
pano.render.filepath = OUT + r"\sky.jpg"
bpy.ops.render.render(write_still=True, scene='pano')
print('SKY done')

# ------------------------------------------------------------------ layout for Unity
rr = random.Random(5)
cam_m = Matrix.LocRotScale(cam_loc, fwd.to_track_quat('-Z', 'Y'), None)
def at(sx, sy, depth):
    hw = depth * 18.0 / cm.data.lens; hh = hw * 9 / 16
    return cam_m @ Vector((sx * hw, sy * hh, -depth))
rocks = []
for i, (sx, sy, depth, s) in enumerate(((-1.15, 0.55, 30, 1.6), (-0.95, -0.35, 24, 1.1), (-0.7, 0.85, 60, 3.2), (-1.3, -0.8, 40, 2.4),
                                         (-0.55, -0.9, 90, 4.0), (-1.05, 0.1, 130, 6.5), (-0.45, 0.4, 170, 7.0), (-1.4, 0.35, 70, 3.0),
                                         (0.95, 0.95, 140, 5.0), (0.35, -0.95, 75, 2.6))):
    p = at(sx, sy, depth)
    ax = Vector((rr.uniform(-1, 1), rr.uniform(-1, 1), rr.uniform(-1, 1))).normalized()
    rocks.append({'mesh': i % 3, 'pos': U(p), 'scale': s, 'axis': U(ax), 'spin': round(rr.uniform(4, 14), 2),
                  'drift': U(Vector((rr.uniform(-1, 1), rr.uniform(-1, 1), rr.uniform(-0.4, 0.4))) * rr.uniform(0.15, 0.45))})
data = {
    'target': U(tgt.location), 'yaw': round(yaw, 3), 'pitch': round(pitch, 3), 'dist': round(dist, 3),
    'hfov': round(hfov, 3), 'shift': [cm.data.shift_x, cm.data.shift_y],
    'sunDir': U(sun_dir), 'sunColor': list(sun.data.color), 'sunPower': 2.4,
    'fillDir': U((fill.location - Vector((0, 0, 0))).normalized()), 'fillColor': list(fill.data.color),
    'bounceDir': U((bounce.location - Vector((0, 0, 0))).normalized()), 'bounceColor': list(bounce.data.color),
    'marsPos': U(mars.location), 'marsRadius': mars.scale[0], 'marsRot': Uq(mars.rotation_euler.to_quaternion()),
    'phobosPos': U(phob.location) if phob else [0, 0, 0], 'phobosRadius': phob.scale[0] if phob else 0,
    'band': U(fwd.cross((up * 0.62 + right * 0.78).normalized()).normalized()),
    'engines': [c for sy in (0.95, -0.95) for sz in (0.95, -0.95) for c in U(Vector((-8.95, sy, sz)))],   # flat x,y,z list (JsonUtility has no nested arrays)
    'engineAxis': U(Vector((-1, 0, 0))),
    'navRed': U(Vector((-1.0, 9.9, 0.1))), 'navGreen': U(Vector((-1.0, -9.9, 0.1))), 'strobe': U(Vector((3.6, 0, 1.9))),
    'rocks': rocks,
}
with open(OUT + r"\menu_live.json", 'w') as f: json.dump(data, f, indent=1)
print('LIVEDONE', json.dumps({k: data[k] for k in ('yaw', 'pitch', 'dist', 'hfov', 'marsRadius')}))
