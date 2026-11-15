# LAST DOCK cutscenes, pixel-art style: real 3D scenes (lit, hazy, depth of field) rendered small (480x270) and
# animated, then cut to a fixed palette per shot so they read as hand-made pixel art (Art/Blender/pixel_cine_post.py).
# Run inside Blender:
#   exec(open(r"F:\NASA\TitanGate2177\Art\Blender\pixel_cine.py").read()); build(); render()
# It reuses the clay asset kit from cinematics_build.py and the game's own cadet from cadet.blend.
import bpy, bmesh, math, random, sys, types, os
from mathutils import Vector, Euler, Matrix, Quaternion

ROOT = r"F:\NASA\TitanGate2177"
BL = ROOT + r"\Art\Blender"
FRAMES = BL + r"\renders\pix"
W, H, FPS = 960, 540, 12   # animation is keyed at 12 fps and rendered at 24 (frame_map) for smooth motion
VIDEO = BL + r"\renders\cine"

cb = types.ModuleType('cb'); sys.modules['cb'] = cb
exec(open(BL + r"\cinematics_build.py", encoding='utf-8').read(), cb.__dict__)
A, M = cb.A, cb.M
node, L = cb.node, cb.L

# ------------------------------------------------------------------ the cadet (the game's own model)

CADET = {}

def load_cadet():
    with bpy.data.libraries.load(BL + r"\cadet.blend", link=False) as (src, dst):
        dst.objects = [n for n in src.objects if n not in ('SpriteCam', 'Key', 'Rim')]
    dg = None
    for o in dst.objects:
        cb.LIB.objects.link(o)
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    for o in dst.objects:   # freeze the clay modifiers so posing is cheap
        if o.type == 'MESH' and o.modifiers:
            ev = o.evaluated_get(dg); me = bpy.data.meshes.new_from_object(ev)
            o.modifiers.clear(); o.data = me
    for o in dst.objects:
        CADET[o.name.split('.')[0]] = o

def cadet(sc, name, loc=(0, 0, 0), rot_z=0.0, scale=1.0, crate=False):
    """A posable copy of the cadet in scene sc. Returns {part name: object}."""
    parts = {}
    def rec(o, parent):
        base = o.name.split('.')[0]
        c = o.copy(); c.name = name + "_" + base; sc.collection.objects.link(c)
        c.parent = parent
        if parent is not None: c.matrix_parent_inverse = o.matrix_parent_inverse.copy()
        parts[base] = c
        for ch in o.children: rec(ch, c)
    rec(CADET['Root'], None)
    r = parts['Root']; r.location = loc; r.rotation_euler = (0, 0, rot_z); r.scale = (scale,) * 3
    for n in ('Crate', 'CrateLabel'):
        if n in parts: parts[n].hide_render = not crate; parts[n].hide_viewport = not crate
    return parts

def key(o, f, loc=None, rot=None, scale=None):
    if loc is not None: o.location = loc; o.keyframe_insert('location', frame=f)
    if rot is not None: o.rotation_euler = rot; o.keyframe_insert('rotation_euler', frame=f)
    if scale is not None: o.scale = scale; o.keyframe_insert('scale', frame=f)

def rad(*d): return tuple(math.radians(x) for x in d)

def pose(c, f, hipL=0, hipR=0, armL=(0, 0, -4), armR=(0, 0, 4), body=(0, 0, 0), head=(0, 0, 0), bodyz=0.58):
    key(c['HipL'], f, rot=rad(hipL, 0, 0)); key(c['HipR'], f, rot=rad(hipR, 0, 0))
    key(c['ShoulderL'], f, rot=rad(*armL)); key(c['ShoulderR'], f, rot=rad(*armR))
    key(c['Body'], f, loc=(0, 0, bodyz), rot=rad(*body)); key(c['Head'], f, rot=rad(*head))

def blink(c, f):
    for e in ('EyeL', 'EyeR'):
        key(c[e], f - 1, scale=(1, 1, 1)); key(c[e], f, scale=(1, 1, 0.15)); key(c[e], f + 1, scale=(1, 1, 1))

def walk(c, f0, f1, stride=32, bob=0.025, arms=26, run=False, carry=False):
    """Walk (or run) cycle on the spot from frame f0 to f1; move the Root separately."""
    for f in range(f0, f1 + 1):
        ph = (f - f0) * math.pi / (3 if run else 4); s = math.sin(ph)
        z = 0.58 + bob * abs(math.cos(ph)) - bob * 0.5
        if carry: aL, aR = (-50, 0, 18), (-50, 0, -18)
        elif run: aL, aR = (-arms * s - 10, 0, -6), (arms * s - 10, 0, 6)
        else: aL, aR = (-arms * s, 0, -4), (arms * s, 0, 4)
        pose(c, f, hipL=stride * s, hipR=-stride * s, armL=aL, armR=aR, body=(-12 if run else 0, 0, 0), bodyz=z)

def linear(o):
    if o.animation_data and o.animation_data.action:
        try:
            for fc in o.animation_data.action.fcurves:
                for k in fc.keyframe_points: k.interpolation = 'LINEAR'
        except AttributeError:
            pass

# ------------------------------------------------------------------ simple set pieces (shaped, soft-edged blocks)

def box(sc, name, loc, size, mat, rot=(0, 0, 0), bevel=0.04):
    me = bpy.data.meshes.new(name); bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector(size), verts=bm.verts)
    if bevel: bmesh.ops.bevel(bm, geom=list(bm.edges), offset=bevel, segments=2, affect='EDGES')
    bm.to_mesh(me); bm.free(); me.materials.append(mat)
    o = bpy.data.objects.new(name, me); sc.collection.objects.link(o); o.location = loc; o.rotation_euler = rot
    return o

def plane(sc, name, loc, size, mat, rot=(0, 0, 0)):
    me = bpy.data.meshes.new(name); bm = bmesh.new(); bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=0.5)
    bmesh.ops.scale(bm, vec=Vector((size[0], size[1], 1)), verts=bm.verts); bm.to_mesh(me); bm.free(); me.materials.append(mat)
    o = bpy.data.objects.new(name, me); sc.collection.objects.link(o); o.location = loc; o.rotation_euler = rot
    return o

def poster_mat(name, c1, c2, scale=4.0, glow=0.6):
    """A printed poster: bold bands and shapes in two colours, slightly lit."""
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree; nt.nodes.clear()
    out = node(nt, 'ShaderNodeOutputMaterial', (700, 0)); bs = node(nt, 'ShaderNodeBsdfPrincipled', (400, 0))
    tc = node(nt, 'ShaderNodeTexCoord', (-700, 0))
    vo = node(nt, 'ShaderNodeTexVoronoi', (-450, 0), Scale=scale); L(nt, tc.outputs['UV'], vo.inputs['Vector'])
    rp = node(nt, 'ShaderNodeValToRGB', (-200, 0)); e = rp.color_ramp.elements
    e[0].color = (*c1, 1); e[1].color = (*c2, 1); rp.color_ramp.interpolation = 'CONSTANT'; e[1].position = 0.45
    L(nt, vo.outputs['Distance'], rp.inputs['Fac'])
    L(nt, rp.outputs['Color'], bs.inputs['Base Color']); L(nt, rp.outputs['Color'], bs.inputs['Emission Color'])
    bs.inputs['Emission Strength'].default_value = glow; bs.inputs['Roughness'].default_value = 0.7
    L(nt, bs.outputs['BSDF'], out.inputs['Surface']); return m

# ------------------------------------------------------------------ scene setup

def glow_pass(sc):
    """Compositor: a soft bloom around bright lights."""
    try:
        tree = bpy.data.node_groups.new(sc.name + '_comp', 'CompositorNodeTree')
        sc.compositing_node_group = tree
        tree.interface.new_socket('Image', in_out='OUTPUT', socket_type='NodeSocketColor')
        rl = tree.nodes.new('CompositorNodeRLayers'); rl.scene = sc
        gl = tree.nodes.new('CompositorNodeGlare')
        for k, v in (('glare_type', 'BLOOM'), ('quality', 'HIGH')):
            try: setattr(gl, k, v)
            except Exception: pass
        for k, v in (('Type', 'BLOOM'), ('Type', 'Bloom'), ('Quality', 'HIGH'), ('Quality', 'High')):
            try: gl.inputs[k].default_value = v
            except Exception: pass
        for k, v in (('Threshold', 0.8), ('Strength', 0.6), ('Size', 0.7)):
            if k in gl.inputs: gl.inputs[k].default_value = v
        out = tree.nodes.new('NodeGroupOutput')
        tree.links.new(rl.outputs['Image'], gl.inputs['Image']); tree.links.new(gl.outputs['Image'], out.inputs[0])
    except Exception as e:
        print('glow pass skipped:', e)

def pshot(name, world, frames):
    sc = bpy.data.scenes.new(name); sc.world = world
    sc.render.engine = 'BLENDER_EEVEE'
    e = sc.eevee
    for k, v in (('taa_render_samples', 32), ('use_shadows', True), ('use_volumetric_shadows', True), ('volumetric_tile_size', '4'),
                 ('use_raytracing', True), ('shadow_ray_count', 2), ('volumetric_samples', 32),
                 ('volumetric_start', 0.2), ('volumetric_end', 1200.0), ('volumetric_sample_distribution', 0.9)):
        try: setattr(e, k, v)
        except Exception: pass
    sc.render.resolution_x, sc.render.resolution_y, sc.render.resolution_percentage = W, H, 100
    sc.render.fps = FPS * 2; sc.frame_start = 1; sc.frame_end = frames * 2
    sc.render.frame_map_old = 100; sc.render.frame_map_new = 200
    try: sc.render.filter_size = 0.7
    except Exception: pass
    try: sc.render.image_settings.media_type = 'VIDEO'
    except Exception: pass
    sc.render.image_settings.file_format = 'FFMPEG'
    ff = sc.render.ffmpeg; ff.format = 'MPEG4'; ff.codec = 'H264'; ff.constant_rate_factor = 'HIGH'; ff.gopsize = 12; ff.audio_codec = 'NONE'
    glow_pass(sc)
    sc.view_settings.exposure = 0.6
    try: sc.view_settings.view_transform = 'AgX'
    except TypeError: pass
    for lk in ('AgX - Punchy', 'Punchy'):
        try: sc.view_settings.look = lk; break
        except TypeError: continue
    # one-pixel ink outlines, like a pixel artist's line
    sc.render.use_freestyle = False;   # ink lines (Freestyle) cost up to 40 s a frame on one CPU core; set True for outlines
    sc.render.line_thickness_mode = 'ABSOLUTE'; sc.render.line_thickness = 1.4
    vl = sc.view_layers[0]; fs = vl.freestyle_settings
    ls = fs.linesets[0] if len(fs.linesets) else fs.linesets.new('ink')
    ls.select_by_visibility = True; ls.select_by_edge_types = True
    ls.select_silhouette = True; ls.select_border = True; ls.select_crease = False
    ls.linestyle.color = (0.04, 0.03, 0.06); ls.linestyle.thickness = 1.0; ls.linestyle.alpha = 0.85
    ls.select_by_collection = True; ls.collection = noink_col(sc); ls.collection_negation = 'EXCLUSIVE'
    return sc

SHOTS = {}
def shot(f): SHOTS[f.__name__] = f; return f

NOINK_KEYS = {'terrain', 'mars', 'marsAtm', 'earthP', 'earthAtm', 'jupiter', 'jupSpot', 'moonP', 'sun', 'earth', 'smokeball', 'fireball',
              'flame', 'flameBlue', 'plasma', 'beam', 'trail', 'streak', 'helmet', 'shield'}
def noink_col(sc):
    """Each shot keeps its line-free objects in its own child collection; the ink line set skips that collection."""
    n = 'NoInk_' + sc.name
    col = bpy.data.collections.get(n)
    if col is None:
        col = bpy.data.collections.new(n); sc.collection.children.link(col)
    return col
def noink(sc, o):
    col = noink_col(sc)
    if o.name not in col.objects: col.objects.link(o)
    if o.name in sc.collection.objects: sc.collection.objects.unlink(o)
    return o
_put0, _haze0 = cb.put, cb.haze_box
def _put(sc, key, *a, **k):
    o = _put0(sc, key, *a, **k)
    return noink(sc, o) if key in NOINK_KEYS else o
def _haze(sc, *a, **k): return noink(sc, _haze0(sc, *a, **k))
cb.put = _put; cb.haze_box = _haze

# ------------------------------------------------------------------ shots

@shot
def p05_cabin():
    """You: the youngest cadet, in a tiny bunk room on the Kestrel, reading the first-day orders on a tablet."""
    N = 48
    sc = pshot('p05_cabin', cb.world_space('pw05', band=(0.3, 0.6, 0.7), mw=0.6, stars=40), N)
    wall = cb.clay_mat('cabWall', (0.22, 0.25, 0.33), (0.16, 0.18, 0.25), scale=0.6, bump=0.15)
    trim = cb.clay_mat('cabTrim', (0.5, 0.42, 0.33), (0.38, 0.3, 0.22), scale=1.0)
    blanket = cb.clay_mat('blanket', (0.16, 0.42, 0.22), (0.1, 0.3, 0.15), scale=3.0, bump=0.4)
    sheet = cb.clay_mat('sheet', (0.82, 0.84, 0.86), (0.7, 0.72, 0.75), scale=3.0)
    wood = cb.clay_mat('wood', (0.42, 0.26, 0.14), (0.3, 0.18, 0.1), scale=2.0)
    # room: back wall with a porthole, side wall, floor, low ceiling
    back = box(sc, 'cabBack', (0, 1.6, 1.4), (6, 0.2, 3.2), wall, bevel=0)
    hole = box(sc, 'cabHole', (1.3, 1.6, 1.7), (1.1, 1.0, 1.1), wall, bevel=0); hole.hide_render = True; hole.hide_viewport = True
    me = bpy.data.meshes.new('cyl'); bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=32, radius1=0.55, radius2=0.55, depth=1.0); bm.to_mesh(me); bm.free()
    cyl = bpy.data.objects.new('cabHoleCyl', me); sc.collection.objects.link(cyl); cyl.location = (1.3, 1.6, 1.75); cyl.rotation_euler = (math.pi / 2, 0, 0)
    cyl.hide_render = True; cyl.hide_viewport = True
    bo = back.modifiers.new('hole', 'BOOLEAN'); bo.object = cyl; bo.operation = 'DIFFERENCE'
    ring = cb.put(sc, 'porthole', (1.3, 1.5, 1.75), (0, 0, math.pi / 2), 0.27)
    box(sc, 'cabSide', (-2.9, 0, 1.4), (0.2, 4, 3.2), wall, bevel=0)
    box(sc, 'cabFloor', (0, 0, -0.05), (6, 4, 0.1), trim, bevel=0)
    box(sc, 'cabCeil', (0, 0, 2.95), (6, 4, 0.1), wall, bevel=0)
    for x in (-2.0, 0.2, 2.4):
        box(sc, 'rib%.1f' % x, (x, 1.45, 1.4), (0.18, 0.12, 3.0), trim)
    # bunk
    box(sc, 'bunkFrame', (-0.6, 0.75, 0.3), (2.6, 1.3, 0.35), wood)
    box(sc, 'mattress', (-0.6, 0.75, 0.55), (2.5, 1.2, 0.2), sheet, bevel=0.08)
    bl = cb.clay('blanketShape', [cb.egg((0, 0, 0), (1.9, 1.3, 0.18)), cb.egg((0.4, 0.2, 0.06), (0.8, 0.6, 0.12)), cb.egg((-0.5, -0.3, 0.05), (0.7, 0.7, 0.1))],
                 res=0.04, disp=0.03, dscale=0.2, smooth=2)
    blo = bpy.data.objects.new('blanket', bl); bl.materials.append(blanket); sc.collection.objects.link(blo); blo.location = (0.1, 0.65, 0.68)
    box(sc, 'pillow', (-1.6, 1.1, 0.72), (0.6, 0.4, 0.18), sheet, rot=(0, 0, 0.2), bevel=0.08)
    box(sc, 'shelf', (-0.8, 1.42, 2.25), (1.6, 0.25, 0.06), wood)
    for i, (x, h, c) in enumerate([(-1.4, 0.28, (0.7, 0.3, 0.2)), (-1.2, 0.22, (0.2, 0.4, 0.7)), (-1.05, 0.3, (0.8, 0.7, 0.3)), (-0.3, 0.18, (0.5, 0.5, 0.55))]):
        box(sc, 'book%d' % i, (x, 1.42, 2.28 + h / 2), (0.12, 0.2, h), cb.clay_mat('bk%d' % i, c, tuple(v * 0.7 for v in c), scale=3), bevel=0.01)
    box(sc, 'posterA', (-1.9, 1.48, 1.45), (0.9, 0.02, 1.2), poster_mat('pA', (0.08, 0.1, 0.25), (0.9, 0.5, 0.2), 3.0), bevel=0)
    box(sc, 'posterB', (-0.6, 1.48, 1.5), (0.8, 0.02, 1.05), poster_mat('pB', (0.15, 0.05, 0.12), (0.3, 0.75, 0.6), 5.0), bevel=0)
    box(sc, 'patch', (0.55, 1.48, 1.2), (0.4, 0.02, 0.4), poster_mat('pC', (0.8, 0.2, 0.1), (0.95, 0.85, 0.6), 2.0, 0.9), bevel=0)
    # bedside clock with red digits, desk in the foreground
    box(sc, 'clock', (0.95, 1.2, 0.95), (0.3, 0.18, 0.16), cb.M['hullDark'])
    box(sc, 'digits', (0.95, 1.1, 0.96), (0.2, 0.01, 0.07), cb.emit_mat('digitsE', (1.0, 0.1, 0.05), 6), bevel=0)
    box(sc, 'desk', (2.3, -0.6, 0.85), (1.2, 2.2, 0.08), wood)
    box(sc, 'deskLeg', (2.3, -0.6, 0.42), (1.0, 0.1, 0.8), wood)
    # outside: Mars and stars through the porthole
    cb.put(sc, 'mars', (-48, 300, 28), (0.2, 0.5, 0), 55); cb.put(sc, 'marsAtm', (-48, 300, 28), (0, 0, 0), 55.4)
    # the cadet sitting on the bunk, legs along the bed, tablet in both hands
    c = cadet(sc, 'cad', (-0.75, 0.75, 0.2), rot_z=math.radians(28))
    tab = box(sc, 'tablet', (0, -0.4, 0.2), (0.28, 0.02, 0.2), cb.M['hullDark'], rot=(math.radians(-50), 0, 0), bevel=0.01)
    scr = box(sc, 'screen', (0, -0.012, 0), (0.24, 0.005, 0.16), cb.emit_mat('scrE', (0.45, 0.85, 1.0), 7), bevel=0)
    scr.parent = tab; tab.parent = c['Body']
    for f in (1, N + 1):
        pose(c, f, hipL=-86, hipR=-80, armL=(-62, 0, 24), armR=(-62, 0, -24), head=(14, 0, 0))
    pose(c, 26, hipL=-86, hipR=-80, armL=(-62, 0, 24), armR=(-66, 0, -22), head=(18, 0, -4))
    blink(c, 16); blink(c, 40)
    for o in c.values(): linear(o)
    # light: tablet glow on the face, cold window light, a dim warm lamp, the clock
    gl = cb.lamp(sc, (0, 0, 0), (0.45, 0.8, 1.0), 25, 0.1, name='tabGlow'); gl.parent = tab; gl.location = (0, -0.25, 0.1)
    for f, p in ((1, 9), (12, 11), (20, 8), (33, 10), (N, 9)):
        gl.data.energy = p; gl.data.keyframe_insert('energy', frame=f)
    cb.lamp(sc, (1.3, 2.4, 1.9), (0.5, 0.65, 1.0), 900, 0.6, 'SPOT', target=(-0.2, 0.4, 0.6), spot=1.1, name='moon')
    cb.lamp(sc, (-1.5, -2.0, 2.4), (0.3, 0.4, 0.8), 60, 2.0, 'AREA', target=(-0.6, 0.7, 1.0), name='fill')
    cb.lamp(sc, (2.4, -0.2, 1.6), (1.0, 0.62, 0.32), 60, 0.3, name='lamp')
    cb.lamp(sc, (0.95, 1.0, 1.0), (1.0, 0.1, 0.05), 1.5, 0.05, name='clockGlow')
    cb.haze_box(sc, (0, 0.4, 1.4), (5.6, 3.0, 2.8), (0.6, 0.7, 0.9), 0.08, 0.5)
    cm = cb.cam(sc, (2.2, -3.3, 1.25), (-0.25, 0.75, 1.05), lens=32, dof=3.6, fstop=2.2)
    key(cm, 1, loc=(2.2, -3.3, 1.25)); key(cm, N, loc=(2.05, -3.05, 1.22)); linear(cm)

# ------------------------------------------------------------------ more set pieces

def remat(o, mat):
    """Give this object its own material, leaving the shared mesh alone."""
    for s in o.material_slots:
        s.link = 'OBJECT'; s.material = mat

def suit_up(sc, c):
    """Helmet bubble and backpack for the cadet when outside."""
    face = c['Face']
    bb = [face.matrix_local @ Vector(v) for v in face.bound_box]
    ctr = sum(bb, Vector()) / 8
    if 'helmet' not in A:
        me = cb.clay('helmetM', [cb.ball((0, 0, 0), 1.0)], res=0.03, disp=0, smooth=0)
        A['helmet'] = cb.join('Helmet', [(me, cb.glow_mat('glass', (0.7, 0.85, 1.0), 1.2, 3.0))])
        me = cb.clay('packM', [cb.egg((0, 0, 0), (0.42, 0.22, 0.5)), cb.limb((-0.14, 0.05, -0.2), (0.14, 0.05, -0.2), 0.1)], res=0.02, disp=0.006, dscale=0.06)
        A['pack'] = cb.join('Pack', [(me, cb.M['pack'])])
    h = cb.put(sc, 'helmet', (0, 0, 0), (0, 0, 0), 0.82, name=c['Root'].name + '_helmet'); h.parent = c['Head']; h.location = ctr
    p = cb.put(sc, 'pack', (0, 0, 0), (0, 0, 0), 1.0, name=c['Root'].name + '_pack'); p.parent = c['Body']; p.location = (0, 0.26, 0.28)
    return h

def make_extras():
    # Earth: oceans, land, clouds, city lights on the night side
    m = bpy.data.materials.new('earthSurf'); m.use_nodes = True; nt = m.node_tree; nt.nodes.clear()
    out = node(nt, 'ShaderNodeOutputMaterial', (1000, 0)); bs = node(nt, 'ShaderNodeBsdfPrincipled', (700, 0)); bs.inputs['Roughness'].default_value = 0.6
    tc = node(nt, 'ShaderNodeTexCoord', (-1000, 0))
    land = node(nt, 'ShaderNodeTexNoise', (-750, 200), Scale=2.2, Detail=10.0, Roughness=0.6); L(nt, tc.outputs['Object'], land.inputs['Vector'])
    lm = node(nt, 'ShaderNodeMapRange', (-500, 200)); lm.inputs['From Min'].default_value = 0.5; lm.inputs['From Max'].default_value = 0.53; L(nt, land.outputs['Fac'], lm.inputs['Value'])
    col = node(nt, 'ShaderNodeMix', (-250, 200)); col.data_type = 'RGBA'; L(nt, lm.outputs['Result'], col.inputs[0])
    col.inputs[6].default_value = (0.03, 0.12, 0.35, 1); col.inputs[7].default_value = (0.22, 0.32, 0.12, 1)
    cl = node(nt, 'ShaderNodeTexNoise', (-750, -100), Scale=4.0, Detail=8.0, Roughness=0.7); L(nt, tc.outputs['Object'], cl.inputs['Vector'])
    cm = node(nt, 'ShaderNodeMapRange', (-500, -100)); cm.inputs['From Min'].default_value = 0.52; cm.inputs['From Max'].default_value = 0.62; L(nt, cl.outputs['Fac'], cm.inputs['Value'])
    c2 = node(nt, 'ShaderNodeMix', (0, 100)); c2.data_type = 'RGBA'; L(nt, cm.outputs['Result'], c2.inputs[0]); L(nt, col.outputs[2], c2.inputs[6]); c2.inputs[7].default_value = (0.9, 0.92, 0.95, 1)
    L(nt, c2.outputs[2], bs.inputs['Base Color'])
    # city lights: land, no cloud, facing away from the sun (sun along +X in object space)
    geo = node(nt, 'ShaderNodeNewGeometry', (-750, -400))
    dt = node(nt, 'ShaderNodeVectorMath', (-500, -400)); dt.operation = 'DOT_PRODUCT'; dt.inputs[1].default_value = (1, 0, 0); L(nt, geo.outputs['Normal'], dt.inputs[0])
    nm = node(nt, 'ShaderNodeMapRange', (-300, -400)); nm.inputs['From Min'].default_value = 0.05; nm.inputs['From Max'].default_value = -0.25; L(nt, dt.outputs['Value'], nm.inputs['Value'])
    ct = node(nt, 'ShaderNodeTexVoronoi', (-750, -650), Scale=260.0); L(nt, tc.outputs['Object'], ct.inputs['Vector'])
    cr = node(nt, 'ShaderNodeMapRange', (-500, -650)); cr.inputs['From Min'].default_value = 0.3; cr.inputs['From Max'].default_value = 0.05; L(nt, ct.outputs['Distance'], cr.inputs['Value'])
    m1 = node(nt, 'ShaderNodeMath', (-100, -500)); m1.operation = 'MULTIPLY'; L(nt, nm.outputs['Result'], m1.inputs[0]); L(nt, cr.outputs['Result'], m1.inputs[1])
    m2 = node(nt, 'ShaderNodeMath', (100, -500)); m2.operation = 'MULTIPLY'; L(nt, m1.outputs['Value'], m2.inputs[0]); L(nt, lm.outputs['Result'], m2.inputs[1])
    m3 = node(nt, 'ShaderNodeMath', (300, -500)); m3.operation = 'MULTIPLY'; m3.inputs[1].default_value = 6.0; L(nt, m2.outputs['Value'], m3.inputs[0])
    bs.inputs['Emission Color'].default_value = (1.0, 0.7, 0.35, 1); L(nt, m3.outputs['Value'], bs.inputs['Emission Strength'])
    L(nt, bs.outputs['BSDF'], out.inputs['Surface'])
    me = cb.clay('earthP', [cb.ball((0, 0, 0), 2.0)], res=0.02, disp=0, smooth=0)
    A['earthP'] = cb.join('EarthPlanet', [(me, m)])
    me = cb.clay('earthAtm', [cb.ball((0, 0, 0), 2.0)], res=0.02, disp=0, smooth=0)
    A['earthAtm'] = cb.join('EarthAtmos', [(me, cb.glow_mat('eatm', (0.35, 0.6, 1.0), 4.0, 4.0))])
    # Jupiter: cream and rust bands, a red spot
    m = bpy.data.materials.new('jupSurf'); m.use_nodes = True; nt = m.node_tree; nt.nodes.clear()
    out = node(nt, 'ShaderNodeOutputMaterial', (800, 0)); bs = node(nt, 'ShaderNodeBsdfPrincipled', (500, 0)); bs.inputs['Roughness'].default_value = 0.8
    tc = node(nt, 'ShaderNodeTexCoord', (-900, 0))
    wv = node(nt, 'ShaderNodeTexWave', (-650, 0), Scale=2.2, Distortion=4.0, Detail=6.0); wv.bands_direction = 'Z'; L(nt, tc.outputs['Object'], wv.inputs['Vector'])
    rp = node(nt, 'ShaderNodeValToRGB', (-350, 0)); e = rp.color_ramp.elements
    e[0].color = (0.55, 0.32, 0.18, 1); e[1].color = (0.92, 0.86, 0.74, 1); e.new(0.5).color = (0.78, 0.6, 0.42, 1)
    L(nt, wv.outputs['Fac'], rp.inputs['Fac']); L(nt, rp.outputs['Color'], bs.inputs['Base Color'])
    L(nt, bs.outputs['BSDF'], out.inputs['Surface'])
    me = cb.clay('jupP', [cb.ball((0, 0, 0), 2.0)], res=0.02, disp=0, smooth=0)
    A['jupiter'] = cb.join('Jupiter', [(me, m)])
    me = cb.clay('jupSpot', [cb.egg((0, 0, 0), (0.25, 0.4, 0.16))], res=0.02, disp=0, smooth=1)
    A['jupSpot'] = cb.join('JupSpot', [(me, cb.clay_mat('spot', (0.7, 0.28, 0.15), (0.6, 0.22, 0.12), scale=3))])
    # Moon: grey sphere for the sky, grey ground material
    M['moonGround'] = cb.clay_mat('moonGround', (0.48, 0.47, 0.46), (0.3, 0.3, 0.3), rough=0.95, scale=0.1, bump=0.1)
    me = cb.clay('moonP', [cb.ball((0, 0, 0), 2.0)], res=0.02, disp=0, smooth=0)
    A['moonP'] = cb.join('MoonPlanet', [(me, cb.clay_mat('moonS', (0.6, 0.6, 0.58), (0.35, 0.35, 0.35), scale=2.0, bump=0.2))])
    # rover
    body = cb.clay('roverB', [cb.egg((0, 0, 0.6), (2.4, 1.4, 0.7)), cb.egg((0.6, 0, 1.05), (1.0, 1.0, 0.5)), cb.limb((-0.8, 0.3, 1.0), (-0.8, 0.3, 1.8), 0.08),
                              cb.egg((-0.8, 0.3, 1.9), (0.1, 0.6, 0.4))], res=0.03, disp=0.01, dscale=0.2)
    wheels = cb.clay('roverW', [cb.egg((x, y, 0.3), (0.55, 0.3, 0.55)) for x in (-0.8, 0.8) for y in (-0.75, 0.75)], res=0.025, disp=0.01, dscale=0.2)
    win = cb.clay('roverL', [cb.egg((1.08, 0, 1.08), (0.1, 0.7, 0.25))], res=0.02, disp=0, smooth=0)
    A['rover'] = cb.join('Rover', [(body, cb.M['hull']), (wheels, cb.M['hullDark']), (win, cb.M['win'])])
    # parachute: a soft striped dome and its lines
    dome = cb.clay('chuteD', [cb.egg((0, 0, 0), (6, 6, 2.6)), ('ELLIPSOID', (0, 0, -0.9), 1.0, (5.4 / 1.15, 5.4 / 1.15, 1.6 / 1.15), None, 2.0, True)], res=0.06, disp=0.04, dscale=0.6, smooth=2)
    m = bpy.data.materials.new('chuteM'); m.use_nodes = True; nt = m.node_tree; nt.nodes.clear()
    out = node(nt, 'ShaderNodeOutputMaterial', (600, 0)); bs = node(nt, 'ShaderNodeBsdfPrincipled', (300, 0)); bs.inputs['Roughness'].default_value = 0.7
    tc = node(nt, 'ShaderNodeTexCoord', (-700, 0)); sp = node(nt, 'ShaderNodeSeparateXYZ', (-500, 0)); L(nt, tc.outputs['Object'], sp.inputs['Vector'])
    at = node(nt, 'ShaderNodeMath', (-300, 0)); at.operation = 'ARCTAN2'; L(nt, sp.outputs['Y'], at.inputs[0]); L(nt, sp.outputs['X'], at.inputs[1])
    st = node(nt, 'ShaderNodeMath', (-100, 0)); st.operation = 'SINE'; L(nt, at.outputs['Value'], st.inputs[0])
    sc8 = node(nt, 'ShaderNodeMath', (-200, -150)); sc8.operation = 'MULTIPLY'; sc8.inputs[1].default_value = 8.0; L(nt, at.outputs['Value'], sc8.inputs[0]); L(nt, sc8.outputs['Value'], st.inputs[0])
    rp = node(nt, 'ShaderNodeValToRGB', (100, 0)); rp.color_ramp.interpolation = 'CONSTANT'; e = rp.color_ramp.elements
    e[0].color = (0.95, 0.4, 0.08, 1); e[1].position = 0.5; e[1].color = (0.92, 0.9, 0.86, 1)
    mr = node(nt, 'ShaderNodeMapRange', (0, -100)); mr.inputs['From Min'].default_value = -1; L(nt, st.outputs['Value'], mr.inputs['Value']); L(nt, mr.outputs['Result'], rp.inputs['Fac'])
    L(nt, rp.outputs['Color'], bs.inputs['Base Color']); L(nt, bs.outputs['BSDF'], out.inputs['Surface'])
    lines = cb.clay('chuteL', [cb.limb((2.6 * math.cos(a), 2.6 * math.sin(a), -0.4), (0, 0, -9.0), 0.05) for a in [k * math.tau / 8 for k in range(8)]], res=0.03, disp=0, smooth=0)
    A['chute'] = cb.join('Chute', [(dome, m), (lines, cb.M['hullDark'])])
    make_pod()
    sofa = cb.clay('sofaM', [cb.egg((0, 0, 0.32), (2.0, 0.9, 0.34)), cb.egg((0, 0.36, 0.72), (2.0, 0.3, 0.8)), cb.egg((-1.0, 0, 0.5), (0.3, 0.9, 0.62)),
                             cb.egg((1.0, 0, 0.5), (0.3, 0.9, 0.62)), cb.egg((0, 0, 0.1), (1.9, 0.8, 0.22))], res=0.03, disp=0.01, dscale=0.2)
    A['sofa'] = cb.join('Sofa', [(sofa, cb.clay_mat('sofaMat', (0.25, 0.36, 0.56), (0.18, 0.26, 0.42), scale=3, bump=0.05))])
    # the rescue ship: the same pod family, but white and blue so it never reads as our lifeboat
    m = bpy.data.materials.new('rescueShell'); m.use_nodes = True; nt = m.node_tree
    bs = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
    bs.inputs['Base Color'].default_value = (0.86, 0.9, 0.95, 1); bs.inputs['Roughness'].default_value = 0.3; bs.inputs['Coat Weight'].default_value = 0.4
    M['rescueShell'] = m
    # heat shield (glowing hot under the lifeboat)
    me = cb.clay('shield', [cb.egg((0, 0, 0), (0.4, 2.3, 2.3)), cb.dent((-0.25, 0, 0), 2.0)], res=0.03, disp=0.01, dscale=0.2)
    A['shield'] = cb.join('Shield', [(me, cb.emit_mat('hotShield', (1.0, 0.45, 0.12), 8))])


def pod_mat():
    """Glossy toy-orange shell: light on top, deeper orange underneath."""
    m = bpy.data.materials.new('podShell'); m.use_nodes = True; nt = m.node_tree; nt.nodes.clear()
    out = node(nt, 'ShaderNodeOutputMaterial', (800, 0)); bs = node(nt, 'ShaderNodeBsdfPrincipled', (500, 0))
    bs.inputs['Roughness'].default_value = 0.32; bs.inputs['Coat Weight'].default_value = 0.35
    tc = node(nt, 'ShaderNodeTexCoord', (-700, 0)); sp = node(nt, 'ShaderNodeSeparateXYZ', (-500, 0)); L(nt, tc.outputs['Object'], sp.inputs['Vector'])
    mr = node(nt, 'ShaderNodeMapRange', (-300, 0)); mr.inputs['From Min'].default_value = -0.35; mr.inputs['From Max'].default_value = 0.05
    L(nt, sp.outputs['X'], mr.inputs['Value'])
    mx = node(nt, 'ShaderNodeMix', (0, 0)); mx.data_type = 'RGBA'; L(nt, mr.outputs['Result'], mx.inputs[0])
    mx.inputs[6].default_value = (0.78, 0.26, 0.04, 1); mx.inputs[7].default_value = (1.0, 0.52, 0.1, 1)
    L(nt, mx.outputs[2], bs.inputs['Base Color'])
    L(nt, bs.outputs['BSDF'], out.inputs['Surface'])
    return m

def make_pod():
    """The lifeboat as in the game: a round orange pod, white light strip, two blue windows, a grey hatch, a thruster below.
    Built upright, then turned so its top points along +X (the shots treat +X as the nose)."""
    shell = cb.clay('podShell', [cb.egg((0, 0, 0.25), (3.4, 2.6, 2.7)), cb.egg((0, 0, -0.45), (3.3, 2.5, 1.3)), cb.dent((0, 0, -2.75), 2.9)],
                    res=0.03, disp=0.006, dscale=0.3, smooth=3)
    strip = cb.clay('podStrip', [cb.egg((0, 0, -0.12), (3.47, 2.67, 0.14))], res=0.02, disp=0, smooth=1)
    wins = cb.clay('podWin', [cb.ball((x, -1.02, 0.42), 0.5) for x in (-0.95, 0.95)], res=0.015, disp=0, smooth=1)
    rims = cb.clay('podRim', [cb.ball((x, -0.98, 0.42), 0.62) for x in (-0.95, 0.95)], res=0.015, disp=0, smooth=1)
    door = cb.clay('podDoor', [('CUBE', (0, -1.2, -0.38), 0.18, (0.36 / 1.15 * 1.4, 0.06, 0.55 / 1.15 * 1.4), None, 2.0)], res=0.015, disp=0, smooth=1)
    frame = cb.clay('podFrame', [('CUBE', (0, -1.13, -0.38), 0.22, (0.42 / 1.15 * 1.4, 0.07, 0.62 / 1.15 * 1.4), None, 2.0)], res=0.015, disp=0, smooth=1)
    step = cb.clay('podStep', [cb.egg((0, -1.22, -1.12), (0.75, 0.32, 0.1))], res=0.015, disp=0, smooth=1)
    noz = cb.clay('podNoz', [cb.ball((0, 0, -1.3), 0.95), cb.dent((0, 0, -1.75), 0.75)], res=0.02, disp=0, smooth=1)
    glow = cb.clay('podGlow', [cb.ball((0, 0, -1.48), 0.5)], res=0.015, disp=0, smooth=0)
    winM = cb.emit_mat('podWinE', (0.1, 0.5, 0.85), 1.2)
    stripM = cb.emit_mat('podStripE', (1.0, 0.97, 0.9), 4.0)
    doorM = cb.clay_mat('podDoorM', (0.5, 0.52, 0.62), (0.4, 0.42, 0.5), rough=0.4, scale=3, bump=0.02)
    darkM = cb.clay_mat('podDarkM', (0.12, 0.12, 0.16), (0.08, 0.08, 0.1), rough=0.5, scale=3, bump=0.02)
    stepM = cb.emit_mat('podStepE', (1.0, 0.65, 0.15), 2.0)
    ob = cb.join('LifePod', [(shell, pod_mat()), (strip, stripM), (rims, darkM), (wins, winM), (frame, darkM), (door, doorM), (step, stepM),
                             (noz, darkM), (glow, cb.M['engine'])])
    ob.data.transform(Matrix.Rotation(math.radians(90), 4, 'Y'))
    A['lifeboat'] = ob

def moon_set(sc):
    t = cb.put(sc, 'terrain', (0, 0, 0)); remat(t, M['moonGround'])
    b = cb.put(sc, 'base', (0, 0, -0.4), (0, 0, 0.35)); return t, b

def flicker(lamp_obj, frames, base, amount, seed=1, every=2):
    rr = random.Random(seed)
    for f in range(1, frames + 1, every):
        lamp_obj.data.energy = base * (1 + rr.uniform(-amount, amount)); lamp_obj.data.keyframe_insert('energy', frame=f)

def shake(cm, f0, f1, amp, seed=3):
    rr = random.Random(seed); base = cm.location.copy()
    for f in range(f0, f1 + 1):
        k = amp * (1 - (f - f0) / max(1, f1 - f0))
        key(cm, f, loc=base + Vector((rr.uniform(-k, k), rr.uniform(-k, k) * 0.3, rr.uniform(-k, k))))
    key(cm, f1 + 1, loc=base)

def move(o, keys, linear_=True):
    """keys: [(frame, loc, rot)]"""
    for f, loc, rot in keys: key(o, f, loc=loc, rot=rot)
    if linear_: linear(o)

# ------------------------------------------------------------------ prologue: the world in 2177

@shot
def p01_earth():
    """2177: Earth at night, cities glowing, the sun rising over the edge; a station glides past."""
    N = 48
    sc = pshot('p01_earth', cb.world_space('pw01', band=(0.2, 0.5, 0.8), mw=0.5, stars=50), N)
    cm = cb.cam(sc, (0, 0, 0), (0, 1, 0.08), lens=32)
    ep = cb.at(cm, 0.0, -2.6, 120)
    e = cb.put(sc, 'earthP', tuple(ep), (0, 0, 0), 90); cb.put(sc, 'earthAtm', tuple(ep), (0, 0, 0), 90.9)
    key(e, 1, rot=(0, 0, math.radians(-30))); key(e, N, rot=(0, 0, math.radians(-26))); linear(e)
    sp = cb.at(cm, 0.55, -0.32, 3000)
    cb.put(sc, 'sun', tuple(sp), (0, 0, 0), 26)
    cb.sun(sc, sp, (1.0, 0.95, 0.88), 5.0, 0.01)
    st = cb.put(sc, 'ship', (0, 0, 0), (0, 0, 0), 0.22, name='p01_station')
    move(st, [(1, tuple(cb.at(cm, -0.9, 0.35, 30)), (0, 0.1, 0.2)), (N, tuple(cb.at(cm, -0.3, 0.3, 30)), (0, 0.1, 0.2))])
    bl = cb.lamp(sc, (0, 0, 0), (1.0, 0.15, 0.1), 0, 0.05, name='blink'); bl.parent = st; bl.location = (3, 0, 1.4)
    for f in range(1, N + 1, 6):
        bl.data.energy = 30 if (f // 6) % 2 == 0 else 0; bl.data.keyframe_insert('energy', frame=f)

@shot
def p02_moon():
    """A town on the Moon: domes, a rover kicking up grey dust, Earth hanging in the black sky."""
    N = 48
    sc = pshot('p02_moon', cb.world_space('pw02', band=(0.6, 0.3, 0.7), mw=0.4, stars=50), N)
    moon_set(sc)
    cb.sun(sc, Vector((-0.8, 0.3, 0.35)), (1.0, 0.97, 0.92), 6.0, 0.005)
    cm = cb.cam(sc, (-34, -44, 4.5), (0, 0, 6), lens=30)
    cb.put(sc, 'earthP', tuple(cb.at(cm, 0.5, 0.55, 900)), (0, 0, 2.0), 24); cb.put(sc, 'earthAtm', tuple(cb.at(cm, 0.5, 0.55, 900)), (0, 0, 0), 24.3)
    rv = cb.put(sc, 'rover', (0, 0, 0), (0, 0, 0), 1.0)
    p0, p1 = Vector((-26, -26, 0)), Vector((-12, -24, 0))
    keys = []
    for f in (1, N):
        p = p0.lerp(p1, (f - 1) / (N - 1)); keys.append((f, (p.x, p.y, cb.ground_h(p.x, p.y) - 0.05), (0, 0, math.radians(8))))
    move(rv, keys)
    for i in range(5):
        d = cb.put(sc, 'smokeball', (0, 0, 0), (0, 0, 0), 0.1, name='p02_dust%d' % i); remat(d, cb.smoke_mat('moonDust%d' % i, (0.5, 0.5, 0.5), 0.6))
        f0 = 1 + i * 9
        p = p0.lerp(p1, (f0 - 1) / (N - 1))
        key(d, f0, loc=(p.x - 1.2, p.y, cb.ground_h(p.x, p.y) + 0.3), scale=(0.15, 0.15, 0.1))
        key(d, min(N, f0 + 24), loc=(p.x - 2.5, p.y + 0.3, cb.ground_h(p.x, p.y) + 1.0), scale=(0.9, 0.9, 0.5))

@shot
def p03_mars():
    """A base on Mars at dusk; a lander settles down on a pillar of fire."""
    N = 48
    sdir = Vector((0.6, 1.0, 0.12))
    sc = pshot('p03_mars', cb.world_sky('pw03', (1.0, 0.6, 0.32), (0.4, 0.24, 0.18), sdir, (1.0, 0.8, 0.55), 2.5, 6.0, 1.0), N)
    cb.mars_set(sc)
    cb.sun(sc, sdir, (1.0, 0.75, 0.48), 4.0, 0.02)
    cb.haze_box(sc, (0, 40, 30), (420, 360, 90), (1.0, 0.7, 0.45), 0.0025, 0.7)
    cm = cb.cam(sc, (-40, -52, 7), (5, 10, 8), lens=30)
    ld = cb.put(sc, 'lifeboat', (0, 0, 0), (0, 0, 0), 2.2, name='p03_lander')
    fl = cb.put(sc, 'flame', (0, 0, 0), (0, 0, 0), (1.0, 1.0, 1.0), name='p03_flame')
    for f, z in ((1, 34), (N, 12)):
        key(ld, f, loc=(28, 10, z), rot=(0, math.radians(-90), 0))
        key(fl, f, loc=(28, 10, z - 5.3), rot=(0, math.radians(90), 0), scale=(1.4, 0.8, 0.8))
    linear(ld); linear(fl)
    lp = cb.lamp(sc, (28, 10, 20), (1.0, 0.6, 0.3), 4000, 1.0, name='flameLight')
    for f, z in ((1, 28), (N, 6)): key(lp, f, loc=(28, 10, z))

@shot
def p04_jupiter():
    """And ships that reach as far as Jupiter."""
    N = 48
    sc = pshot('p04_jupiter', cb.world_space('pw04', band=(0.3, 0.8, 0.5), mw=0.5, stars=50), N)
    j = cb.put(sc, 'jupiter', (230, 600, -40), (0.15, 0, 0), 170)
    key(j, 1, rot=(0.15, 0, 0)); key(j, N, rot=(0.15, 0, math.radians(3))); linear(j)
    cb.sun(sc, Vector((-1, -0.3, 0.3)), (1.0, 0.95, 0.88), 5.0, 0.01)
    cm = cb.cam(sc, (0, 0, 0), (0.2, 1, -0.05), lens=30)
    sh = cb.put(sc, 'ship', (0, 0, 0), (0, 0, 0), 0.6, name='p04_ship')
    move(sh, [(1, tuple(cb.at(cm, -1.3, -0.25, 40)), (0, 0.05, math.radians(10))), (N, tuple(cb.at(cm, 0.15, -0.1, 40)), (0, 0.05, math.radians(10)))])
    for i, (sy, sz) in enumerate([(0.95, 0.95), (-0.95, 0.95), (0.95, -0.95), (-0.95, -0.95)]):
        f = cb.put(sc, 'flameBlue', (0, 0, 0), (0, 0, 0), name='p04_fl%d' % i); f.parent = sh; f.location = (-10.4, sy, sz); f.scale = (0.6, 0.5, 0.5)

@shot
def p06_walk():
    """First day on the Kestrel: the cadet carries a crate down the corridor toward the camera."""
    N = 48
    sc = pshot('p06_walk', cb.world_space('pw06', mw=0.0, stars=0.0), N)
    cb.put(sc, 'corridor', (0, 0, 0))
    c = cadet(sc, 'cad', (0, 0, 0), rot_z=math.radians(-90), crate=True)
    walk(c, 1, N, stride=28, carry=True)
    move(c['Root'], [(1, (8.5, 0.2, -0.1), (0, 0, math.radians(-90))), (N, (3.6, 0.2, -0.1), (0, 0, math.radians(-90)))])
    blink(c, 30)
    for i, x in enumerate([2, 7.2, 12.4, 17.6]):
        cb.lamp(sc, (x + 1.3, 0, 3.0), (1.0, 0.86, 0.68), 160, 0.4, name='ceil%d' % i)
    cb.lamp(sc, (21, 0, 3.0), (0.4, 0.7, 1.0), 120, 0.5, name='far')
    cb.haze_box(sc, (10, 0, 1.5), (24, 4.6, 3.6), (0.85, 0.8, 0.75), 0.03, 0.4)
    cm = cb.cam(sc, (1.2, -1.2, 1.35), (10, 0.4, 1.1), lens=28, dof=7.5, fstop=2.8)
    key(cm, 1, loc=(1.2, -1.2, 1.35)); key(cm, N, loc=(1.5, -1.1, 1.3)); linear(cm)

# ------------------------------------------------------------------ the impact

@shot
def i01_debris():
    """Rocks streak past the Kestrel; one grazes the top of the hull, a burst of fire and sparks, the ship lurches."""
    N = 48
    sc = pshot('i01_debris', cb.world_space('pi01', band=(0.9, 0.3, 0.4), core=(1.0, 0.45, 0.7), haze=(0.4, 0.2, 0.8), mw=1.0, stars=50), N)
    cm = cb.cam(sc, (0, 0, 0), (0, 1, 0), lens=32)
    sh = cb.put(sc, 'ship', tuple(cb.at(cm, 0.0, -0.08, 30)))
    cb.face(sh, Vector((-1, 0.35, 0.05))); cb.roll(sh, 0.35)
    r0 = sh.rotation_euler.copy()
    cb.attach_engines(sc, sh)
    cb.sun(sc, cb.mat_of(cm).to_3x3() @ Vector((-0.5, 0.7, 0.6)), (1.0, 0.86, 0.72), 2.6, 0.02)
    cb.lamp(sc, tuple(sh.location + Vector((10, -8, 6))), (0.5, 0.35, 1.0), 6e3, 10, 'AREA', target=tuple(sh.location), name='fill')
    cb.lamp(sc, tuple(sh.location + Vector((-6, 14, 10))), (0.9, 0.8, 1.0), 5e3, 8, 'AREA', target=tuple(sh.location), name='rim')
    # near misses: rocks rush from deep behind toward the camera, passing well above or below the ship
    rr = random.Random(21)
    for i in range(14):
        side = rr.choice((-1, 1))
        sy0, sy1 = side * rr.uniform(0.7, 1.1), side * rr.uniform(1.4, 2.2)
        sx0 = rr.uniform(-1.3, 1.3); sx1 = sx0 * 1.4 + rr.uniform(-0.3, 0.3)
        f0 = rr.randint(1, 30); f1 = min(N, f0 + rr.randint(12, 22))
        p0 = cb.at(cm, sx0, sy0, rr.uniform(120, 170)); p1 = cb.at(cm, sx1, sy1, rr.uniform(16, 26))
        o = cb.put(sc, 'debris%d' % rr.randint(0, 5), (0, 0, 0), (0, 0, 0), rr.uniform(1.0, 2.4), name='i01_d%d' % i)
        a0 = Vector((rr.uniform(0, 6), rr.uniform(0, 6), rr.uniform(0, 6)))
        a1 = a0 + Vector((rr.uniform(-4, 4), rr.uniform(-4, 4), rr.uniform(-4, 4)))
        key(o, 1, loc=tuple(p0), rot=tuple(a0)); key(o, f0, loc=tuple(p0), rot=tuple(a0)); key(o, f1, loc=tuple(p1), rot=tuple(a1))
        linear(o)
    # the graze: a rock skims the top of the habitat ring at frame 24 and glances off
    M0 = cb.mat_of(sh)
    up = (M0.to_3x3() @ Vector((0, 0, 1))).normalized()
    C = M0 @ Vector((0.6, 0, 1.85))
    hitter = cb.put(sc, 'debris3', (0, 0, 0), (0, 0, 0), 2.4, name='i01_hitter')
    pa, pc_ = C + Vector((-34, 40, 20)), C + up * 2.0
    pb = C + up * 4 + Vector((10, -22, 9))
    key(hitter, 1, loc=tuple(pa), rot=(0, 0, 0)); key(hitter, 24, loc=tuple(pc_), rot=(1.5, 0.8, 0.4)); key(hitter, N, loc=tuple(pb), rot=(5.5, 3.0, 2.2))
    linear(hitter)
    # the ship lurches from the blow
    key(sh, 1, rot=tuple(r0)); key(sh, 23, rot=tuple(r0))
    key(sh, 27, rot=(r0.x + 0.07, r0.y - 0.05, r0.z + 0.06)); key(sh, N, rot=(r0.x + 0.1, r0.y - 0.07, r0.z + 0.09))
    # fire, smoke, flash, sparks and chips at the impact point
    fb = cb.put(sc, 'fireball', tuple(C), (0, 0, 0), 0.01, name='i01_fire')
    for f, sz in ((23, 0.01), (25, 2.0), (30, 3.0), (N, 2.2)): key(fb, f, scale=(sz, sz, sz))
    sm = cb.put(sc, 'smokeball', tuple(C + up * 1.5), (0, 0, 0), 0.01, name='i01_smoke')
    for f, sz, dz in ((25, 0.01, 0), (36, 2.6, 2.5), (N, 4.0, 5)): key(sm, f, loc=tuple(C + up * (1.2 + dz)), scale=(sz, sz, sz * 0.8))
    fl = cb.lamp(sc, tuple(C + up * 1.2), (1.0, 0.55, 0.2), 0, 1.0, name='blast')
    for f, e in ((1, 0), (23, 0), (24, 2.5e5), (27, 4e4), (32, 1.6e4), (N, 1.2e4)): fl.data.energy = e; fl.data.keyframe_insert('energy', frame=f)
    for f in range(33, N + 1, 2):
        fl.data.energy = 1.2e4 * (0.7 + 0.6 * random.Random(f).random()); fl.data.keyframe_insert('energy', frame=f)
    for i in range(26):
        d = (up * rr.uniform(0.3, 1.0) + Vector((rr.uniform(-1, 1), rr.uniform(-1, 0.5), rr.uniform(-0.3, 1)))).normalized()
        sp = cb.put(sc, 'streak', (0, 0, 0), (0, 0, 0), 0.01, name='i01_s%d' % i)
        cb.face(sp, d); key(sp, 1, loc=tuple(C), rot=tuple(sp.rotation_euler), scale=(0.01, 0.01, 0.01))
        key(sp, 24, loc=tuple(C), scale=(0.01, 0.01, 0.01))
        key(sp, 26, loc=tuple(C + d * rr.uniform(1.5, 3)), scale=(rr.uniform(1.0, 2.2), 1.6, 1.6))
        key(sp, 34, loc=tuple(C + d * rr.uniform(6, 10)), scale=(0.01, 0.01, 0.01))
    for i in range(10):
        d = (up + Vector((rr.uniform(-1, 1), rr.uniform(-1, 1), rr.uniform(-0.2, 0.8)))).normalized()
        ch = cb.put(sc, 'debris%d' % rr.randint(0, 5), (0, 0, 0), (0, 0, 0), 0.01, name='i01_chip%d' % i)
        key(ch, 1, loc=tuple(C), scale=(0.01, 0.01, 0.01)); key(ch, 24, loc=tuple(C), scale=(0.01, 0.01, 0.01))
        key(ch, 25, loc=tuple(C + d * 0.8), scale=(0.25, 0.25, 0.25), rot=(0, 0, 0))
        key(ch, N, loc=tuple(C + d * rr.uniform(8, 14)), scale=(0.2, 0.2, 0.2), rot=(rr.uniform(3, 9), rr.uniform(3, 9), 0))
    # red warning light on the hull, blinking after the hit
    bl = cb.lamp(sc, tuple(M0 @ Vector((3.0, 0, 1.6))), (1.0, 0.1, 0.05), 0, 0.2, name='warn')
    for f in range(1, N + 1, 3):
        bl.data.energy = (2500 if (f // 3) % 2 == 0 else 0) if f > 26 else 0; bl.data.keyframe_insert('energy', frame=f)
    key(cm, 1, loc=(0, 0, 0)); shake(cm, 24, 36, 0.5, 9)
    key(cm, N, loc=(0, -2.0, 0.3))

@shot
def i02_hit():
    """IMPACT: the cadet is resting on the lounge sofa (as in the game); the ship jolts, a white flash,
    the lights die to red, and the cadet is thrown onto the floor."""
    N = 30
    sc = pshot('i02_hit', cb.world_space('pi02', mw=0.0, stars=0.0), N)
    cb.put(sc, 'corridor', (0, 0, 0))
    cb.put(sc, 'sofa', (7.4, 1.45, -0.1))
    c = cadet(sc, 'cad', (7.4, 1.35, -0.12), rot_z=0.0)
    for f in (1, 8):
        pose(c, f, hipL=-82, hipR=-78, armL=(-20, 0, -14), armR=(-20, 0, 14), head=(6, 0, 0))
    blink(c, 4)
    pose(c, 11, hipL=-50, hipR=-40, armL=(-120, 0, -30), armR=(-110, 0, 30), body=(28, 0, 0), head=(-18, 0, 0))
    pose(c, 16, hipL=10, hipR=-20, armL=(-80, 0, -40), armR=(-70, 0, 40), body=(55, 0, 0), head=(-10, 0, 0))
    pose(c, N, hipL=20, hipR=-10, armL=(-70, 0, -40), armR=(-60, 0, 40), body=(60, 0, 0), head=(-24, 0, 6))
    key(c['Root'], 8, loc=(7.4, 1.35, -0.12)); key(c['Root'], 13, loc=(7.4, 0.6, -0.2)); key(c['Root'], 17, loc=(7.4, 0.05, -0.42)); key(c['Root'], N, loc=(7.4, 0.0, -0.44))
    lamps = []
    for i, x in enumerate([2, 7.2, 12.4, 17.6]):
        w = cb.lamp(sc, (x + 1.3, 0, 3.0), (1.0, 0.86, 0.68), 160, 0.4, name='ceil%d' % i)
        r = cb.lamp(sc, (x, -1.6 if i % 2 else 1.6, 3.0), (1.0, 0.06, 0.03), 0, 0.15, name='alarm%d' % i)
        for f, e1, e2 in ((1, 160, 0), (8, 160, 0), (9, 2500, 0), (10, 0, 0), (14, 30, 900), (18, 0, 300), (22, 0, 900), (26, 0, 300), (N, 0, 900)):
            w.data.energy = e1; w.data.keyframe_insert('energy', frame=f); r.data.energy = e2; r.data.keyframe_insert('energy', frame=f)
    sp = cb.lamp(sc, (9.5, -1.9, 2.6), (1.0, 0.6, 0.2), 0, 0.05, name='spark')
    flicker(sp, N, 900, 0.9, 5, 1); sp.data.energy = 0; sp.data.keyframe_insert('energy', frame=8)
    for i in range(10):
        s = cb.put(sc, 'streak', (0, 0, 0), (0, 0, 0), 0.0, name='i02_s%d' % i)
        d = Vector((random.Random(i).uniform(-1, 1), random.Random(i + 9).uniform(0.2, 1), random.Random(i + 3).uniform(-1, 0.3))).normalized()
        key(s, 9, loc=(9.5, -1.9, 2.6), scale=(0.01, 0.01, 0.01)); cb.face(s, d); s.keyframe_insert('rotation_euler', frame=9)
        key(s, 12, loc=tuple(Vector((9.5, -1.9, 2.6)) + d * 1.2), scale=(0.35, 1, 1)); key(s, 16, loc=tuple(Vector((9.5, -1.9, 2.6)) + d * 2.0), scale=(0.01, 0.01, 0.01))
    cb.haze_box(sc, (10, 0, 1.5), (24, 4.6, 3.6), (0.75, 0.7, 0.7), 0.05, 0.4)
    cm = cb.cam(sc, (3.7, 0.2, 1.6), (7.5, 0.9, 0.5), lens=28, dof=4.0, fstop=2.8)
    key(cm, 1, loc=(3.7, 0.2, 1.6)); shake(cm, 9, 20, 0.18)

@shot
def i03_run():
    """The alarm wails: the cadet runs for the lifeboat bay, red lights sweeping, sparks falling."""
    N = 36
    sc = pshot('i03_run', cb.world_space('pi03', mw=0.0, stars=0.0), N)
    cb.put(sc, 'corridor', (0, 0, 0))
    c = cadet(sc, 'cad', (0, 0, 0), rot_z=math.radians(90))
    walk(c, 1, N, stride=50, bob=0.06, arms=48, run=True)
    move(c['Root'], [(1, (3.5, 0.2, -0.1), (0, 0, math.radians(90))), (N, (13.5, 0.0, -0.1), (0, 0, math.radians(90)))])
    for i, x in enumerate([2, 7.2, 12.4, 17.6]):
        r = cb.lamp(sc, (x, -1.6 if i % 2 else 1.6, 3.0), (1.0, 0.06, 0.03), 0, 0.15, name='alarm%d' % i)
        for f in range(1, N + 1, 2):
            r.data.energy = 300 + 900 * (0.5 + 0.5 * math.sin(f * 0.9 + i)); r.data.keyframe_insert('energy', frame=f)
    cb.lamp(sc, (21, 0, 3.0), (1.0, 0.5, 0.15), 500, 0.5, name='evac')
    sp = cb.lamp(sc, (14.5, -1.8, 2.3), (1.0, 0.6, 0.2), 0, 0.05, name='spark'); flicker(sp, N, 500, 0.95, 7, 1)
    cb.put(sc, 'smokeball', (15.5, 1.0, 2.6), (0, 0, 0), (2.0, 1.2, 0.8))
    cb.haze_box(sc, (10, 0, 1.5), (24, 4.6, 3.6), (0.75, 0.7, 0.7), 0.06, 0.4)
    cm = cb.cam(sc, (0.4, -0.9, 1.3), (12, 0.3, 1.2), lens=24)
    key(cm, 1, loc=(0.4, -0.9, 1.3)); key(cm, N, loc=(2.4, -0.7, 1.3)); linear(cm)
    shake(cm, 1, 6, 0.04)

# ------------------------------------------------------------------ escape and landing

@shot
def l01_away():
    """The lifeboat breaks free; behind it the Kestrel burns; Mars waits below."""
    N = 48
    sc = pshot('l01_away', cb.world_space('pl01', band=(0.5, 0.3, 0.8), mw=0.4, stars=50), N)
    cm = cb.cam(sc, (0, 0, 0), (0, 1, 0), lens=30)
    mp = cb.at(cm, -0.55, -1.55, 700)
    cb.put(sc, 'mars', tuple(mp), (0.1, 0.3, 0), 330); cb.put(sc, 'marsAtm', tuple(mp), (0, 0, 0), 331.4)
    cb.sun(sc, cb.mat_of(cm).to_3x3() @ Vector((-0.7, 0.6, 0.5)), (1.0, 0.93, 0.85), 5.0, 0.01)
    sh = cb.put(sc, 'ship', tuple(cb.at(cm, 0.42, 0.25, 55)))
    cb.face(sh, Vector((1, 0.6, 0.25))); cb.roll(sh, 0.4)
    for i, lp in enumerate([(1.4, -1.7, 0.6), (-2.6, 1.6, 1.0), (3.5, 0.5, 1.5)]):
        f = cb.attach(sc, sh, 'fireball', lp, (i, 0, 0), 1.3, name='l01_f%d' % i)
        fl = cb.lamp(sc, tuple(f.location), (1.0, 0.45, 0.12), 2e4, 1.0, name='fire%d' % i); flicker(fl, N, 2e4, 0.4, i)
    lb = cb.put(sc, 'lifeboat', (0, 0, 0))
    p0, p1 = cb.at(cm, 0.15, 0.1, 18), cb.at(cm, -0.35, -0.12, 9)
    move(lb, [(1, tuple(p0), (0, 0, 0)), (N, tuple(p1), (0, 0, 0))])
    d = (p1 - p0).normalized(); rot = d.to_track_quat('X', 'Z').to_euler()
    for f in (1, N): key(lb, f, rot=rot)
    fl = cb.put(sc, 'flame', (0, 0, 0), (0, 0, 0), name='l01_flame'); fl.parent = lb; fl.location = (-3.0, 0, 0); fl.scale = (1.0, 0.7, 0.7)
    for f in range(1, N + 1, 2):
        fl.scale = (1.0 + 0.25 * random.Random(f).random(), 0.7, 0.7); fl.keyframe_insert('scale', frame=f)
    tl = cb.lamp(sc, (0, 0, 0), (1.0, 0.6, 0.3), 2500, 0.5, name='thrust'); tl.parent = lb; tl.location = (-2.2, 0, 0)
    cb.lamp(sc, (-6, -3, 3), (0.35, 0.5, 0.9), 300, 6, 'AREA', target=tuple(p1), name='fill')

@shot
def l02_entry():
    """Entry: the heat shield glows at 1,300 °C, plasma streams past, everything shakes."""
    N = 36
    sc = pshot('l02_entry', cb.world_space('pl02', band=(0.2, 0.3, 0.9), mw=0.3, stars=50), N)
    cm = cb.cam(sc, (0, 0, 0), (0, 1, -0.3), lens=30, roll=0.1)
    R0 = 3000.0; mp = Vector((0, 700, -160 - R0))
    cb.put(sc, 'mars', tuple(mp), (0.4, 0.1, 0), R0); cb.put(sc, 'marsAtm', tuple(mp), (0, 0, 0), R0 * 1.004)
    cb.sun(sc, cb.at(cm, 1.6, 0.35, 8000), (1.0, 0.8, 0.62), 1.3, 0.01)
    lb = cb.put(sc, 'lifeboat', tuple(cb.at(cm, 0.05, 0.05, 11)))
    cb.face(lb, Vector((-1.0, 0.1, 0.6)))   # the pod falls bottom first, heat shield leading
    sh = cb.attach(sc, lb, 'shield', (-1.5, 0, 0), (0, 0, math.pi), 1.45)
    pl = cb.attach(sc, lb, 'plasma', (-0.4, 0, 0), (0, 0, math.pi), 1.15)
    for f in range(1, N + 1, 2):
        s = 1.0 + 0.15 * random.Random(f).random(); pl.scale = (s, s * 0.95, s * 0.95); pl.keyframe_insert('scale', frame=f)
    g = cb.lamp(sc, tuple(cb.mat_of(lb) @ Vector((-2.6, 0, 0))), (1.0, 0.45, 0.18), 9000, 1.0, name='glow'); flicker(g, N, 9000, 0.35, 4, 1)
    shake(cm, 1, N, 0.08, 6)

@shot
def l03_chute():
    """The parachute bangs open; the lifeboat swings beneath it over the red plains."""
    N = 48
    sdir = Vector((0.4, 1.0, 0.25))
    sc = pshot('l03_chute', cb.world_sky('pl03', (1.0, 0.62, 0.34), (0.38, 0.24, 0.2), sdir, (1.0, 0.82, 0.58), 2.5, 6.0, 1.0), N)
    cb.mars_set(sc)
    cb.sun(sc, sdir, (1.0, 0.76, 0.5), 4.0, 0.02)
    cb.haze_box(sc, (0, 40, 30), (420, 360, 120), (1.0, 0.7, 0.45), 0.0022, 0.7)
    piv = bpy.data.objects.new('swing', None); sc.collection.objects.link(piv); piv.location = (-20, -30, 95)
    ch = cb.put(sc, 'chute', (0, 0, 0)); ch.parent = piv; ch.location = (0, 0, 0)
    lb = cb.put(sc, 'lifeboat', (0, 0, 0), (0, math.radians(-90), 0), 1.0); lb.parent = piv; lb.location = (0, 0, -10.5)
    for f, a, s in ((1, 0, 0.6), (6, 4, 1.15), (14, -6, 0.95), (24, 5, 1.0), (34, -4, 1.0), (N, 3, 1.0)):
        key(piv, f, rot=(math.radians(a), math.radians(a * 0.5), 0))
        key(ch, f, scale=(s, s, s * 1.05))
    key(piv, 1, loc=(-20, -30, 98)); key(piv, N, loc=(-20, -30, 92))
    cm = cb.cam(sc, (-30, -50, 84), (-20, -30, 91), lens=30)
    key(cm, 1, loc=(-30, -50, 84)); key(cm, N, loc=(-29, -48, 81)); linear(cm)

@shot
def l04_land():
    """Rockets roar, dust boils up, and the lifeboat settles onto Mars."""
    N = 48
    sdir = Vector((0.3, 1.0, 0.15))
    sc = pshot('l04_land', cb.world_sky('pl04', (1.0, 0.62, 0.34), (0.42, 0.26, 0.19), sdir, (1.0, 0.82, 0.58), 3.0, 6.0, 1.0), N)
    cb.mars_set(sc)
    cb.sun(sc, sdir, (1.0, 0.74, 0.47), 4.0, 0.02)
    cb.haze_box(sc, (0, 40, 30), (420, 360, 90), (1.0, 0.72, 0.48), 0.002, 0.75)
    gx, gy = -30.0, -40.0; gz = cb.ground_h(gx, gy)
    lb = cb.put(sc, 'lifeboat', (0, 0, 0), (0, math.radians(-90), 0), 1.0)
    fl = cb.put(sc, 'flame', (0, 0, 0), (0, math.radians(90), 0), (1.0, 0.7, 0.7), name='l04_flame')
    for f, z, fs in ((1, gz + 14, 1.2), (30, gz + 3.0, 1.0), (36, gz + 1.35, 0.6), (40, gz + 1.35, 0.01), (N, gz + 1.35, 0.01)):
        key(lb, f, loc=(gx, gy, z)); key(fl, f, loc=(gx, gy, z - 1.4 - 1.75 * fs), scale=(fs, 0.7 * max(fs, 0.01), 0.7 * max(fs, 0.01)))
    lp = cb.lamp(sc, (gx, gy, gz + 6), (1.0, 0.6, 0.3), 3000, 1.0, name='flameLight')
    for f, e in ((1, 3000), (36, 2500), (40, 0), (N, 0)): lp.data.energy = e; lp.data.keyframe_insert('energy', frame=f)
    for i in range(8):
        a = i / 8 * math.tau
        d = cb.put(sc, 'smokeball', (0, 0, 0), (0, 0, 0), 0.1, name='l04_dust%d' % i); remat(d, cb.smoke_mat('marsDust%d' % i, (0.55, 0.3, 0.16), 0.8))
        key(d, 18, loc=(gx, gy, gz + 0.3), scale=(0.1, 0.1, 0.1))
        key(d, N, loc=(gx + math.cos(a) * 7, gy + math.sin(a) * 7, gz + 1.0), scale=(2.4, 2.4, 1.2))
    cm = cb.cam(sc, (gx - 8, gy - 13, gz + 3.0), (gx, gy, gz + 4.5), lens=30)
    shake(cm, 20, 40, 0.15, 8)

@shot
def l05_base():
    """Out of the lifeboat: the cadet walks toward an old, empty base under a low dusty sun."""
    N = 48
    sdir = Vector((0.15, 1.0, 0.1))
    sc = pshot('l05_base', cb.world_sky('pl05', (1.0, 0.64, 0.34), (0.42, 0.26, 0.19), sdir, (1.0, 0.82, 0.58), 3.0, 6.0, 1.1), N)
    cb.mars_set(sc)
    cb.sun(sc, sdir, (1.0, 0.74, 0.47), 4.5, 0.02)
    cb.haze_box(sc, (0, 60, 40), (440, 360, 110), (1.0, 0.72, 0.48), 0.0028, 0.75)
    cb.scatter_rocks(sc, (-10, -20), 70, 90, 0.4, 2.2, 3, avoid=((0, 0), 30))
    c = cadet(sc, 'cad', (0, 0, 0), rot_z=math.radians(160)); suit_up(sc, c)
    walk(c, 1, N, stride=30)
    p0, p1 = Vector((-26.0, -50.0)), Vector((-24.6, -46.2))
    move(c['Root'], [(1, (p0.x, p0.y, cb.ground_h(p0.x, p0.y)), (0, 0, math.radians(160))), (N, (p1.x, p1.y, cb.ground_h(p1.x, p1.y)), (0, 0, math.radians(160)))])
    cm = cb.cam(sc, (-29.4, -56.5, cb.ground_h(-28, -55) + 1.5), (-6, 20, 4), lens=30, dof=6.0, fstop=4)
    key(cm, 1, loc=(-29.4, -56.5, cb.ground_h(-28, -55) + 1.5)); key(cm, N, loc=(-28.6, -54.0, cb.ground_h(-28, -55) + 1.6)); linear(cm)

# ------------------------------------------------------------------ endings

@shot
def w01_rescue():
    """The rescue ship comes down on beams of light; the cadet jumps and waves."""
    N = 48
    sdir = Vector((-0.6, 1.0, 0.14))
    sc = pshot('w01_rescue', cb.world_sky('pw01b', (1.0, 0.56, 0.28), (0.28, 0.15, 0.14), sdir, (1.0, 0.76, 0.46), 3.0, 5.0, 1.0), N)
    cb.mars_set(sc)
    cb.sun(sc, sdir, (1.0, 0.7, 0.42), 4.0, 0.02)
    cb.haze_box(sc, (0, 40, 40), (440, 360, 110), (1.0, 0.68, 0.45), 0.003, 0.6)
    cm = cb.cam(sc, (-8, -42, cb.ground_h(-8, -42) + 1.1), (4, 20, 20), lens=24, dof=3.6, fstop=4)
    p = cb.at(cm, 0.4, -0.55, 3.6)
    c = cadet(sc, 'cad', (p.x, p.y, cb.ground_h(p.x, p.y)), rot_z=math.radians(180 + 20)); suit_up(sc, c)
    for f in range(1, N + 1, 3):
        s = math.sin(f * 0.7)
        pose(c, f, armR=(-150 + 25 * s, 0, 10 + 20 * s), armL=(-10, 0, -4), head=(-20, 0, 0), bodyz=0.58 + 0.05 * max(0, math.sin(f * 0.35)))
    sp0, sp1 = cb.at(cm, -0.3, 0.75, 90), cb.at(cm, -0.08, 0.4, 60)
    S = 4.0
    ship = cb.put(sc, 'lifeboat', (0, 0, 0), (0, 0, 0), S, name='w01_ship'); ship.material_slots[0].link = 'OBJECT'; ship.material_slots[0].material = M['rescueShell']
    fl = cb.put(sc, 'flameBlue', (0, 0, 0), (0, 0, 0), (1.4, 1.0, 1.0), name='w01_fl')
    beams = [cb.put(sc, 'beam', (0, 0, 0), (0, 0, 0), (1.3, 1.3, 1.3), name='w01_beam%d' % i) for i in range(2)]
    lt = cb.lamp(sc, (0, 0, 0), (1.0, 0.95, 0.85), 3e5, 1.0, 'SPOT', spot=0.6, name='land')
    for f, p, tilt in ((1, sp0, 8), (N, sp1, 3)):
        key(ship, f, loc=tuple(p), rot=(math.radians(tilt), math.radians(-90), 0))
        key(fl, f, loc=tuple(p - Vector((0, 0, 1.4 * S + 1.7 * 1.4))), rot=(0, math.radians(90), 0))
        for i, b in enumerate(beams): key(b, f, loc=tuple(p + Vector(((-1.6 if i == 0 else 1.6), 0, -1.2 * S - 5.8))))
        key(lt, f, loc=tuple(p - Vector((0, 0, 1.2 * S))), rot=(0, 0, 0))
    for o in [ship, fl, lt] + beams: linear(o)

@shot
def w02_home():
    """Going home: the ship climbs away on a column of fire; Mars drops below."""
    N = 48
    sdir = Vector((0.12, 1.0, 0.12))
    sc = pshot('w02_home', cb.world_sky('pw02b', (1.0, 0.7, 0.4), (0.52, 0.33, 0.22), sdir, (1.0, 0.86, 0.62), 3.5, 4.0, 1.2), N)
    cb.mars_set(sc)
    cb.sun(sc, sdir, (1.0, 0.78, 0.5), 4.5, 0.02)
    cb.haze_box(sc, (0, 30, 35), (440, 380, 100), (1.0, 0.74, 0.52), 0.0012, 0.75)
    ship = cb.put(sc, 'lifeboat', (0, 0, 0), (0, math.radians(-90), 0), 3.0, name='w02_ship'); ship.material_slots[0].link = 'OBJECT'; ship.material_slots[0].material = M['rescueShell']
    fl = cb.put(sc, 'flame', (0, 0, 0), (0, math.radians(90), 0), (2.2, 1.3, 1.3), name='w02_fl')
    for f, z in ((1, 8), (N, 52)):
        key(ship, f, loc=(-10, 20, z)); key(fl, f, loc=(-10, 20, z - 8.2))
    for o in (ship, fl):
        for fc in (o.animation_data.action.fcurves if hasattr(o.animation_data.action, 'fcurves') else []):
            for k in fc.keyframe_points: k.interpolation = 'QUAD'; k.easing = 'EASE_IN'
    lp = cb.lamp(sc, (-10, 20, 0), (1.0, 0.6, 0.3), 2e4, 2.0, name='fireLight')
    for f, z in ((1, 2), (N, 56)): key(lp, f, loc=(-10, 20, z))
    for i in range(6):
        a = i / 6 * math.tau; gz = cb.ground_h(-10, 20)
        d = cb.put(sc, 'smokeball', (0, 0, 0), (0, 0, 0), 0.1, name='w02_dust%d' % i); remat(d, cb.smoke_mat('homeDust%d' % i, (0.6, 0.35, 0.2), 0.8))
        key(d, 1, loc=(-10, 20, gz + 0.5), scale=(1, 1, 0.6)); key(d, N, loc=(-10 + math.cos(a) * 14, 20 + math.sin(a) * 14, gz + 3), scale=(5, 5, 2.5))
    cm = cb.cam(sc, (-34, -50, 5), (-10, 20, 12), lens=30)
    key(cm, 1, rot=cm.rotation_euler.copy()); r2 = cm.rotation_euler.copy(); r2.x += math.radians(9); key(cm, N, rot=r2)

@shot
def x01_dusk():
    """Night falls: the base lights flicker and go out one by one; only the red beacon blinks."""
    N = 48
    sdir = Vector((0.7, 1.0, 0.015))
    sc = pshot('x01_dusk', cb.world_sky('px01', (0.18, 0.2, 0.28), (0.03, 0.035, 0.06), sdir, (0.35, 0.6, 1.0), 2.5, 10.0, 0.9, stars=30.0), N)
    cb.mars_set(sc)
    cb.sun(sc, sdir, (0.5, 0.65, 1.0), 0.5, 0.05)
    cb.haze_box(sc, (0, 0, 15), (300, 300, 40), (0.6, 0.65, 0.8), 0.008, 0.5)
    for i, (p, off) in enumerate((((8, -16, 3), 14), ((2, -9, 3), 26), ((18, -4, 2.5), 36))):
        lt = cb.lamp(sc, p, (1.0, 0.6, 0.3), 3000, 1.0, name='win%d' % i)
        for f, e in ((1, 3000), (off, 3000), (off + 1, 600), (off + 2, 2800), (off + 3, 300), (off + 5, 0), (N, 0)):
            lt.data.energy = e; lt.data.keyframe_insert('energy', frame=f)
    bc = cb.lamp(sc, (-4.4, -4.6, 17.6), (1.0, 0.08, 0.04), 0, 0.3, name='beacon')
    for f in range(1, N + 1, 4):
        bc.data.energy = 3000 if (f // 4) % 3 == 0 else 0; bc.data.keyframe_insert('energy', frame=f)
    cb.scatter_rocks(sc, (-10, -40), 50, 60, 0.4, 2.0, 11, avoid=((0, 0), 30))
    cm = cb.cam(sc, (-46, -60, 4.5), (8, 20, 9), lens=34)
    key(cm, 1, loc=(-46, -60, 4.5)); key(cm, N, loc=(-43, -56, 4.4)); linear(cm)

@shot
def x02_stars():
    """Alone under the stars, the cadet looks up; Earth is a small blue dot. Try again."""
    N = 48
    sdir = Vector((0.2, 1.0, -0.2))
    sc = pshot('x02_stars', cb.world_sky('px02', (0.07, 0.09, 0.16), (0.004, 0.006, 0.016), sdir, (0.2, 0.3, 0.6), 0.6, 5.0, 1.0, stars=90.0), N)
    cb.mars_set(sc)
    cl = cb.put(sc, 'cliff', (-30, -110, cb.ground_h(-30, -110) + 2), (0, 0, math.pi + 0.1), 1.0)
    x, y = -30.0, -102.0; z = cb.surface_z(cl, x, y) or cb.ground_h(-30, -110) + 10
    top = Vector((x, y, z))
    ang = math.atan2(8 - y, 0 - x)
    c = cadet(sc, 'cad', (x, y, z - 0.5), rot_z=ang + math.pi / 2); suit_up(sc, c)
    for f in (1, N + 1):
        pose(c, f, hipL=-80, hipR=-76, armL=(-40, 0, 20), armR=(-40, 0, -20), head=(-14, 0, 0))
    pose(c, 24, hipL=-80, hipR=-76, armL=(-40, 0, 20), armR=(-40, 0, -20), head=(-26, 0, 6))
    cp = Vector(cb.behind(top, (0, 8, 0), 5.5, 2.6, 1.7)); gz = cb.surface_z(cl, cp.x, cp.y)
    if gz is not None: cp.z = max(cp.z, gz + 0.9)
    cm = cb.cam(sc, tuple(cp), (0, 8, 7), lens=26, dof=6.2, fstop=4)
    cb.put(sc, 'earth', tuple(cb.at(cm, 0.3, 0.55, 3000)), (0, 0, 0), 4.0)
    cb.sun(sc, Vector((0.6, 0.5, 0.7)), (0.45, 0.6, 1.0), 0.3, 0.04, name='moonlight')
    cb.lamp(sc, (0, -14, 4), (1.0, 0.6, 0.3), 3000, 1.0, name='baseglow')
    cb.lamp(sc, tuple(top + Vector((-1.5, 4.0, 2.5))), (0.45, 0.65, 1.0), 120, 1.5, 'AREA', target=tuple(top + Vector((0, 0, 0.8))), name='rim')
    key(cm, 1, loc=tuple(cp)); key(cm, N, loc=tuple(cp + Vector((0.3, 0.4, 0.1)))); linear(cm)


# ------------------------------------------------------------------ build and render

def build(names=None):
    cb.build_lib(); load_cadet(); make_extras()
    # pixel art wants calm, flat surfaces: soften the clay dents that would turn into speckles
    for m in bpy.data.materials:
        if m.node_tree:
            for n in m.node_tree.nodes:
                if n.type == 'BUMP': n.inputs['Strength'].default_value *= 0.2
    for n, f in SHOTS.items():
        if names is None or n in names: f()
    tx = {}
    exec(open(BL + r"\cine_textures.py", encoding='utf-8').read(), tx)
    print('textured:', tx['texture_pass'](M))
    bpy.ops.wm.save_as_mainfile(filepath=BL + r"\pixel_cine.blend")

def render(names=None, still=None):
    """Render each shot to Art/Blender/renders/cine/<shot>.mp4 (or one PNG still at frame `still`)."""
    os.makedirs(VIDEO, exist_ok=True)
    for n in SHOTS:
        if names is not None and n not in names: continue
        sc = bpy.data.scenes[n]
        fmt = sc.render.image_settings
        if still is not None:
            sc.frame_current = still
            try: fmt.media_type = 'IMAGE'
            except Exception: pass
            fmt.file_format = 'PNG'; sc.render.filepath = VIDEO + "\\" + n + "_still.png"
            bpy.ops.render.render(write_still=True, scene=n)
            try: fmt.media_type = 'VIDEO'
            except Exception: pass
            fmt.file_format = 'FFMPEG'
        else:
            sc.render.filepath = VIDEO + "\\" + n + ".mp4"; sc.render.use_file_extension = False
            bpy.ops.render.render(animation=True, scene=n)
