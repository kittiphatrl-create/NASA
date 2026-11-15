# LAST DOCK cutscene stills: build every shot in Blender as hand-shaped clay (metaballs, then noise and smoothing,
# so nothing reads as a hard geometric primitive), light it like a film still, render with Cycles.
# Run inside Blender:
#   exec(open(r"F:\NASA\TitanGate2177\Art\Blender\cinematics_build.py").read()); build_all(); render_all()
# Then run Art/Blender/cinematics_post.py (bloom, streaks, grade, grain); it writes Assets/Resources/Sprites/KeyArt/*.jpg.bytes.
import bpy, bmesh, math, random
from mathutils import Vector, Euler, Quaternion, Matrix, noise

ROOT = r"F:\NASA\TitanGate2177"
RAW = ROOT + r"\Art\Blender\renders\cin"
R = random.Random(2177)
LIB = None

# ------------------------------------------------------------------ setup

def reset():
    global LIB
    bpy.ops.wm.read_homefile(use_empty=True)
    lib = bpy.context.scene; lib.name = "Lib"
    LIB = bpy.data.collections.new("LibAssets"); lib.collection.children.link(LIB)
    return lib

def node(nt, t, loc=(0, 0), **inp):
    n = nt.nodes.new(t); n.location = loc
    for k, v in inp.items(): n.inputs[k].default_value = v
    return n

def L(nt, a, b): nt.links.new(a, b)

# ------------------------------------------------------------------ materials

def clay_mat(name, c1, c2, rough=0.58, sss=0.06, bump=0.25, scale=5.0, metal=0.0, coat=0.0, strata=0.0):
    """Clay: soft two-tone colour, darker in cavities, thumb-pressed dents in the surface."""
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = node(nt, 'ShaderNodeOutputMaterial', (900, 0))
    bs = node(nt, 'ShaderNodeBsdfPrincipled', (600, 0))
    bs.inputs['Roughness'].default_value = rough; bs.inputs['Metallic'].default_value = metal
    bs.inputs['Subsurface Weight'].default_value = sss; bs.inputs['Coat Weight'].default_value = coat
    tc = node(nt, 'ShaderNodeTexCoord', (-1000, 0))
    nz = node(nt, 'ShaderNodeTexNoise', (-750, 200), Scale=scale * 0.6, Detail=6.0, Roughness=0.62)
    L(nt, tc.outputs['Object'], nz.inputs['Vector'])
    ramp = node(nt, 'ShaderNodeValToRGB', (-500, 200))
    e0, e1 = ramp.color_ramp.elements
    e0.position, e1.position = 0.3, 0.72; e0.color = (*c1, 1); e1.color = (*c2, 1)
    L(nt, nz.outputs['Fac'], ramp.inputs['Fac'])
    col = ramp.outputs['Color']
    if strata > 0:   # layered rock bands
        wv = node(nt, 'ShaderNodeTexWave', (-750, 450), Scale=scale * 0.25, Distortion=6.0, Detail=4.0)
        wv.bands_direction = 'Z'
        L(nt, tc.outputs['Object'], wv.inputs['Vector'])
        hs0 = node(nt, 'ShaderNodeMapRange', (-500, 450)); hs0.inputs['To Min'].default_value = 1 - strata; hs0.inputs['To Max'].default_value = 1.0
        L(nt, wv.outputs['Fac'], hs0.inputs['Value'])
        hsa = node(nt, 'ShaderNodeHueSaturation', (-250, 300))
        L(nt, col, hsa.inputs['Color']); L(nt, hs0.outputs['Result'], hsa.inputs['Value']); col = hsa.outputs['Color']
    geo = node(nt, 'ShaderNodeNewGeometry', (-750, -150))
    pr = node(nt, 'ShaderNodeMapRange', (-500, -150)); pr.inputs['From Min'].default_value = 0.47; pr.inputs['From Max'].default_value = 0.53
    pr.inputs['To Min'].default_value = 0.55; pr.inputs['To Max'].default_value = 1.15
    L(nt, geo.outputs['Pointiness'], pr.inputs['Value'])
    hs = node(nt, 'ShaderNodeHueSaturation', (0, 100))
    L(nt, col, hs.inputs['Color']); L(nt, pr.outputs['Result'], hs.inputs['Value'])
    L(nt, hs.outputs['Color'], bs.inputs['Base Color'])
    vor = node(nt, 'ShaderNodeTexVoronoi', (-750, -400)); vor.feature = 'SMOOTH_F1'; vor.inputs['Scale'].default_value = scale * 2.5
    L(nt, tc.outputs['Object'], vor.inputs['Vector'])
    nz2 = node(nt, 'ShaderNodeTexNoise', (-750, -650), Scale=scale * 8, Detail=3.0)
    L(nt, tc.outputs['Object'], nz2.inputs['Vector'])
    add = node(nt, 'ShaderNodeMath', (-450, -500)); add.operation = 'ADD'
    L(nt, vor.outputs['Distance'], add.inputs[0]); L(nt, nz2.outputs['Fac'], add.inputs[1])
    bp = node(nt, 'ShaderNodeBump', (300, -350), Strength=bump, Distance=0.1)
    L(nt, add.outputs['Value'], bp.inputs['Height']); L(nt, bp.outputs['Normal'], bs.inputs['Normal'])
    L(nt, bs.outputs['BSDF'], out.inputs['Surface'])
    return m

def emit_mat(name, color, strength):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = node(nt, 'ShaderNodeOutputMaterial', (300, 0))
    em = node(nt, 'ShaderNodeEmission', (0, 0), Color=(*color, 1), Strength=strength)
    L(nt, em.outputs['Emission'], out.inputs['Surface']); return m

def glow_mat(name, color, strength, power=2.0):
    """Glow shell: bright at the silhouette edge, see-through in the middle (atmosphere limb)."""
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = node(nt, 'ShaderNodeOutputMaterial', (600, 0))
    lw = node(nt, 'ShaderNodeLayerWeight', (-400, 0), Blend=0.5)
    pw = node(nt, 'ShaderNodeMath', (-200, 0)); pw.operation = 'POWER'; pw.inputs[1].default_value = power
    L(nt, lw.outputs['Facing'], pw.inputs[0])
    tr = node(nt, 'ShaderNodeBsdfTransparent', (100, 150))
    em = node(nt, 'ShaderNodeEmission', (100, -100), Color=(*color, 1), Strength=strength)
    mx = node(nt, 'ShaderNodeMixShader', (350, 0))
    L(nt, pw.outputs['Value'], mx.inputs[0]); L(nt, tr.outputs['BSDF'], mx.inputs[1]); L(nt, em.outputs['Emission'], mx.inputs[2])
    L(nt, mx.outputs['Shader'], out.inputs['Surface'])
    return m

def vol_glow(name, color, strength, falloff=1.6, flicker=0.5):
    """Glowing gas (flame, plasma, light beam): brightest in the middle of the shape, fading to nothing at its edge."""
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = node(nt, 'ShaderNodeOutputMaterial', (800, 0))
    vp = node(nt, 'ShaderNodeVolumePrincipled', (500, 0)); vp.inputs['Density'].default_value = 0.0
    vp.inputs['Emission Color'].default_value = (*color, 1)
    tc = node(nt, 'ShaderNodeTexCoord', (-900, 0))
    ma = node(nt, 'ShaderNodeVectorMath', (-700, 0)); ma.operation = 'MULTIPLY_ADD'
    ma.inputs[1].default_value = (2, 2, 2); ma.inputs[2].default_value = (-1, -1, -1)
    L(nt, tc.outputs['Generated'], ma.inputs[0])
    gr = node(nt, 'ShaderNodeTexGradient', (-500, 0)); gr.gradient_type = 'SPHERICAL'; L(nt, ma.outputs['Vector'], gr.inputs['Vector'])
    pw = node(nt, 'ShaderNodeMath', (-300, 0)); pw.operation = 'POWER'; pw.inputs[1].default_value = falloff; L(nt, gr.outputs['Fac'], pw.inputs[0])
    nz = node(nt, 'ShaderNodeTexNoise', (-500, -250), Scale=4.0, Detail=4.0); L(nt, tc.outputs['Generated'], nz.inputs['Vector'])
    fl = node(nt, 'ShaderNodeMapRange', (-300, -250)); fl.inputs['To Min'].default_value = 1 - flicker; fl.inputs['To Max'].default_value = 1 + flicker
    L(nt, nz.outputs['Fac'], fl.inputs['Value'])
    m1 = node(nt, 'ShaderNodeMath', (-100, 0)); m1.operation = 'MULTIPLY'; L(nt, pw.outputs['Value'], m1.inputs[0]); L(nt, fl.outputs['Result'], m1.inputs[1])
    m2 = node(nt, 'ShaderNodeMath', (100, 0)); m2.operation = 'MULTIPLY'; m2.inputs[1].default_value = strength; L(nt, m1.outputs['Value'], m2.inputs[0])
    L(nt, m2.outputs['Value'], vp.inputs['Emission Strength'])
    L(nt, vp.outputs['Volume'], out.inputs['Volume'])
    return m

def fire_mat(name, density=6.0, hot=2600, cool=1100, scale=3.0, emit=8.0):
    """Volume fireball: noise density, blackbody colour from cool edges to a hot core."""
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = node(nt, 'ShaderNodeOutputMaterial', (700, 0))
    vp = node(nt, 'ShaderNodeVolumePrincipled', (400, 0))
    tc = node(nt, 'ShaderNodeTexCoord', (-900, 0))
    nz = node(nt, 'ShaderNodeTexNoise', (-650, 100), Scale=scale, Detail=8.0, Roughness=0.6)
    L(nt, tc.outputs['Object'], nz.inputs['Vector'])
    gr = node(nt, 'ShaderNodeTexGradient', (-650, -200)); gr.gradient_type = 'SPHERICAL'
    L(nt, tc.outputs['Object'], gr.inputs['Vector'])
    mul = node(nt, 'ShaderNodeMath', (-400, 0)); mul.operation = 'MULTIPLY'
    L(nt, nz.outputs['Fac'], mul.inputs[0]); L(nt, gr.outputs['Fac'], mul.inputs[1])
    mr = node(nt, 'ShaderNodeMapRange', (-200, 0)); mr.inputs['From Min'].default_value = 0.08; mr.inputs['From Max'].default_value = 0.5
    L(nt, mul.outputs['Value'], mr.inputs['Value'])
    dn = node(nt, 'ShaderNodeMath', (50, 150)); dn.operation = 'MULTIPLY'; dn.inputs[1].default_value = density
    L(nt, mr.outputs['Result'], dn.inputs[0]); L(nt, dn.outputs['Value'], vp.inputs['Density'])
    tp = node(nt, 'ShaderNodeMapRange', (50, -150)); tp.inputs['To Min'].default_value = cool; tp.inputs['To Max'].default_value = hot
    L(nt, mr.outputs['Result'], tp.inputs['Value']); L(nt, tp.outputs['Result'], vp.inputs['Temperature'])
    vp.inputs['Blackbody Intensity'].default_value = emit
    vp.inputs['Color'].default_value = (0.6, 0.3, 0.1, 1)
    L(nt, vp.outputs['Volume'], out.inputs['Volume'])
    return m

def haze_mat(name, color, density, aniso=0.55):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = node(nt, 'ShaderNodeOutputMaterial', (500, 0))
    vp = node(nt, 'ShaderNodeVolumePrincipled', (200, 0))
    vp.inputs['Color'].default_value = (*color, 1); vp.inputs['Density'].default_value = density; vp.inputs['Anisotropy'].default_value = aniso
    L(nt, vp.outputs['Volume'], out.inputs['Volume']); return m

def smoke_mat(name, color=(0.04, 0.035, 0.035), density=1.5, scale=2.0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = node(nt, 'ShaderNodeOutputMaterial', (700, 0))
    vp = node(nt, 'ShaderNodeVolumePrincipled', (400, 0)); vp.inputs['Color'].default_value = (*color, 1)
    tc = node(nt, 'ShaderNodeTexCoord', (-900, 0))
    nz = node(nt, 'ShaderNodeTexNoise', (-650, 100), Scale=scale, Detail=6.0, Roughness=0.55)
    L(nt, tc.outputs['Object'], nz.inputs['Vector'])
    gr = node(nt, 'ShaderNodeTexGradient', (-650, -200)); gr.gradient_type = 'SPHERICAL'
    L(nt, tc.outputs['Object'], gr.inputs['Vector'])
    mul = node(nt, 'ShaderNodeMath', (-400, 0)); mul.operation = 'MULTIPLY'
    L(nt, nz.outputs['Fac'], mul.inputs[0]); L(nt, gr.outputs['Fac'], mul.inputs[1])
    mr = node(nt, 'ShaderNodeMapRange', (-200, 0)); mr.inputs['From Min'].default_value = 0.1; mr.inputs['From Max'].default_value = 0.4
    mr.inputs['To Max'].default_value = density
    L(nt, mul.outputs['Value'], mr.inputs['Value']); L(nt, mr.outputs['Result'], vp.inputs['Density'])
    L(nt, vp.outputs['Volume'], out.inputs['Volume']); return m

# ------------------------------------------------------------------ clay shapes

def _bake(ob):
    """Evaluate modifiers (or the metaball surface) into a plain mesh datablock."""
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    ev = ob.evaluated_get(dg)
    tmp = ev.to_mesh()
    bm = bmesh.new(); bm.from_mesh(tmp); ev.to_mesh_clear()
    me = bpy.data.meshes.new(ob.name + "_m"); bm.to_mesh(me); bm.free()
    return me

# metaball helpers in real sizes (a radius-1 ball is 1.15 wide at threshold 0.6)
def ball(co, d, stiff=2.0): return ('BALL', co, d / 1.15, None, None, stiff)
def egg(co, dims, rot=None, stiff=2.0): return ('ELLIPSOID', co, 1.0, tuple(x / 1.15 for x in dims), rot, stiff)
def limb(p1, p2, d, stiff=2.0):
    p1, p2 = Vector(p1), Vector(p2); v = p2 - p1
    q = Vector((1, 0, 0)).rotation_difference(v.normalized()) if v.length > 1e-6 else Quaternion()
    return ('CAPSULE', tuple((p1 + p2) / 2), d / 1.15, (max(0.001, v.length / 2), 1, 1), q, stiff)
def dent(co, d): return ('BALL', co, d / 1.15, None, None, 2.0, True)

_fam = [0]
def clay(name, elems, res=0.05, disp=0.03, dscale=0.35, lumps=0.0, smooth=3, thresh=0.6):
    """Shape a clay piece from soft blobs (they melt into each other like pressed clay), then roughen and smooth it."""
    _fam[0] += 1
    fam = "mb%03d%s" % (_fam[0], name.replace('.', '_'))
    mb = bpy.data.metaballs.new(fam); mb.resolution = res; mb.render_resolution = res; mb.threshold = thresh
    ob = bpy.data.objects.new(fam, mb); LIB.objects.link(ob)
    for e in elems:
        el = mb.elements.new(type=e[0]); el.co = e[1]; el.radius = e[2]
        if e[3]: el.size_x, el.size_y, el.size_z = e[3]
        if e[4] is not None:
            el.rotation = e[4] if isinstance(e[4], Quaternion) else Euler(e[4]).to_quaternion()
        el.stiffness = e[5]
        if len(e) > 6 and e[6]: el.use_negative = True
    me = _bake(ob)
    bpy.data.objects.remove(ob); bpy.data.metaballs.remove(mb)
    return roughen(name, me, disp, dscale, lumps, smooth)

def roughen(name, me, disp, dscale, lumps=0.0, smooth=3):
    ob = bpy.data.objects.new(name + "_tmp", me); LIB.objects.link(ob)
    if disp:
        t = bpy.data.textures.new(name + "_t", 'CLOUDS'); t.noise_scale = dscale; t.noise_depth = 2
        d = ob.modifiers.new('dp', 'DISPLACE'); d.texture = t; d.strength = disp; d.texture_coords = 'GLOBAL'
    if lumps:
        t2 = bpy.data.textures.new(name + "_l", 'CLOUDS'); t2.noise_scale = dscale * 4; t2.noise_depth = 1
        d2 = ob.modifiers.new('lp', 'DISPLACE'); d2.texture = t2; d2.strength = lumps; d2.texture_coords = 'GLOBAL'
    if smooth:
        s = ob.modifiers.new('sm', 'SMOOTH'); s.factor = 0.5; s.iterations = smooth
    out = _bake(ob); bpy.data.objects.remove(ob); bpy.data.meshes.remove(me)
    return out

def join(name, parts, coll=None):
    """parts: [(mesh, material)] -> one object with a material slot per material."""
    bm = bmesh.new(); mats = []
    for me, mat in parts:
        if mat not in mats: mats.append(mat)
        idx = mats.index(mat); n0 = len(bm.faces)
        bm.from_mesh(me); bm.faces.ensure_lookup_table()
        for i in range(n0, len(bm.faces)): bm.faces[i].material_index = idx
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for m in mats: me.materials.append(m)
    try: me.shade_smooth()
    except Exception:
        for p in me.polygons: p.use_smooth = True
    ob = bpy.data.objects.new(name, me); (coll or LIB).objects.link(ob)
    return ob

# ------------------------------------------------------------------ assets

M = {}
A = {}

def make_materials():
    M['hull'] = clay_mat('hull', (0.62, 0.6, 0.56), (0.42, 0.4, 0.37), scale=1.2, bump=0.3)
    M['hullDark'] = clay_mat('hullDark', (0.2, 0.2, 0.22), (0.12, 0.12, 0.13), scale=1.5)
    M['panel'] = clay_mat('panel', (0.05, 0.09, 0.2), (0.1, 0.16, 0.32), rough=0.35, metal=0.3, coat=0.4, scale=2.0)
    M['orange'] = clay_mat('orange', (0.85, 0.36, 0.08), (0.6, 0.22, 0.05), scale=2.0)
    M['suit'] = clay_mat('suit', (0.86, 0.84, 0.79), (0.66, 0.64, 0.6), scale=6.0, bump=0.2)
    M['pack'] = clay_mat('pack', (0.85, 0.55, 0.12), (0.55, 0.32, 0.06), scale=6.0)
    M['visor'] = clay_mat('visor', (0.9, 0.6, 0.25), (0.7, 0.42, 0.15), rough=0.08, metal=1.0, bump=0.05, scale=4)
    M['rock'] = clay_mat('rock', (0.5, 0.24, 0.12), (0.3, 0.13, 0.07), rough=0.85, scale=0.25, bump=0.5, strata=0.35)
    M['rockDark'] = clay_mat('rockDark', (0.28, 0.13, 0.07), (0.16, 0.07, 0.04), scale=0.3, bump=0.5, strata=0.3)
    M['ground'] = clay_mat('ground', (0.52, 0.26, 0.13), (0.36, 0.17, 0.08), rough=0.92, scale=0.08, bump=0.4)
    M['base'] = clay_mat('base', (0.78, 0.74, 0.66), (0.55, 0.5, 0.44), scale=0.6, bump=0.3)
    M['wall'] = clay_mat('wall', (0.16, 0.18, 0.21), (0.09, 0.1, 0.12), scale=0.8, bump=0.35)
    M['crate'] = clay_mat('crate', (0.22, 0.24, 0.26), (0.14, 0.15, 0.16), scale=1.5)
    M['win'] = emit_mat('win', (1.0, 0.62, 0.3), 14)
    M['winCool'] = emit_mat('winCool', (0.55, 0.8, 1.0), 10)
    M['engine'] = emit_mat('engine', (0.35, 0.65, 1.0), 40)
    M['red'] = emit_mat('red', (1.0, 0.08, 0.04), 40)
    M['evac'] = emit_mat('evac', (1.0, 0.55, 0.15), 18)
    M['strip'] = emit_mat('strip', (0.75, 0.88, 1.0), 6)
    M['spark'] = emit_mat('spark', (1.0, 0.6, 0.2), 60)
    M['flame'] = vol_glow('flame', (1.0, 0.5, 0.2), 25, 1.8)
    M['flameBlue'] = vol_glow('flameBlue', (0.35, 0.65, 1.0), 30, 1.8)
    M['plasma'] = vol_glow('plasma', (1.0, 0.28, 0.06), 8, 1.4, 0.7)
    M['beam'] = vol_glow('beam', (1.0, 0.9, 0.75), 2.5, 1.0, 0.2)
    M['fire'] = fire_mat('fire', density=0.9, hot=4200, cool=1300, scale=2.2, emit=6)
    M['smoke'] = smoke_mat('smoke')

def make_ship():
    hull = clay('kes_hull', [
        limb((-5.5, 0, 0), (5, 0, 0), 2.4),
        egg((6.2, 0, 0.15), (3.6, 2.2, 1.9)),          # cockpit nose
        egg((-6.6, 0, 0), (2.8, 3.1, 3.1)),            # engine block
        egg((-2.6, 0, 0), (1.1, 3.6, 3.6)), egg((-0.6, 0, 0), (1.1, 3.6, 3.6)),
        egg((1.4, 0, 0), (1.1, 3.4, 3.4)),             # habitat rings
        egg((3.6, 0, 1.3), (2.2, 1.0, 0.9)),           # spine hump
        limb((-8.2, 0.95, 0.95), (-6.5, 0.95, 0.95), 1.0), limb((-8.2, -0.95, 0.95), (-6.5, -0.95, 0.95), 1.0),
        limb((-8.2, 0.95, -0.95), (-6.5, 0.95, -0.95), 1.0), limb((-8.2, -0.95, -0.95), (-6.5, -0.95, -0.95), 1.0),
        limb((0.6, 0, 1.6), (0.9, 0, 2.6), 0.25), egg((1.0, 0, 2.8), (0.35, 1.6, 1.4), (0, -0.35, 0)),   # dish
        limb((-1.0, 1.4, 0), (-1.0, 3.2, 0), 0.35), limb((-1.0, -1.4, 0), (-1.0, -3.2, 0), 0.35),          # wing struts
        dent((-8.7, 0.95, 0.95), 0.6), dent((-8.7, -0.95, 0.95), 0.6), dent((-8.7, 0.95, -0.95), 0.6), dent((-8.7, -0.95, -0.95), 0.6),
    ], res=0.06, disp=0.05, dscale=0.5, lumps=0.06)
    wings = clay('kes_wings', [egg((-1.0, 6.4, 0), (2.2, 6.6, 0.18)), egg((-1.0, -6.4, 0), (2.2, 6.6, 0.18)),
                               egg((-1.0, 9.3, 0), (1.6, 1.4, 0.14)), egg((-1.0, -9.3, 0), (1.6, 1.4, 0.14))],
                 res=0.05, disp=0.03, dscale=0.6, smooth=2)
    wins = []
    for i in range(9):
        x = -4.2 + i * 1.05
        wins += [ball((x, 0.0, 1.17), 0.2), ball((x, 0.85, 0.85), 0.18), ball((x, -0.85, 0.85), 0.18)]
    wins += [egg((7.2, 0, 0.75), (1.0, 1.3, 0.35), (0, 0.5, 0))]
    win = clay('kes_win', wins, res=0.03, disp=0, smooth=0)
    eng = clay('kes_eng', [ball((-8.75, sy, sz), 0.62) for sy in (0.95, -0.95) for sz in (0.95, -0.95)], res=0.03, disp=0, smooth=0)
    A['ship'] = join('Kestrel', [(hull, M['hull']), (wings, M['panel']), (win, M['win']), (eng, M['engine'])])

def make_lifeboat():
    body = clay('lb_body', [
        egg((0, 0, 0), (3.2, 1.7, 1.5)), ball((1.3, 0, 0.1), 1.3),
        egg((-1.5, 0, 0.75), (1.2, 0.18, 1.0), (0, 0.4, 0)), egg((-1.4, 0.75, -0.45), (1.1, 0.9, 0.16), (-0.6, 0, 0)),
        egg((-1.4, -0.75, -0.45), (1.1, 0.9, 0.16), (0.6, 0, 0)), egg((-1.75, 0, 0), (0.8, 1.2, 1.2)),
        dent((-2.2, 0, 0), 0.8),
    ], res=0.035, disp=0.025, dscale=0.25, lumps=0.03)
    band = clay('lb_band', [egg((0.0, 0, 0), (2.6, 1.78, 0.35))], res=0.03, disp=0.01, dscale=0.2)
    win = clay('lb_win', [egg((1.55, 0, 0.42), (0.7, 0.9, 0.3), (0, 0.3, 0))], res=0.025, disp=0, smooth=0)
    noz = clay('lb_noz', [ball((-2.05, 0, 0), 0.62)], res=0.025, disp=0, smooth=0)
    A['lifeboat'] = join('Lifeboat', [(body, M['hull']), (band, M['orange']), (win, M['win']), (noz, M['engine'])])

def astronaut(name, pose='stand'):
    """Cadet in a clay spacesuit, facing -Y. Poses: stand, wave, sit, run."""
    hip, chest, head = (0, 0, 0.95), (0, 0, 1.25), (0, -0.02, 1.62)
    if pose == 'sit':
        hip, chest, head = (0, 0, 0.55), (0, 0.02, 0.88), (0, 0.0, 1.25)
    body = [egg(chest, (0.5, 0.36, 0.6)), egg(hip, (0.42, 0.3, 0.3)), egg((chest[0], chest[1], chest[2] + 0.22), (0.62, 0.36, 0.22)),
            egg((0, 0, chest[2] + 0.33), (0.3, 0.3, 0.12))]
    sh = Vector(chest) + Vector((0, 0, 0.2))
    if pose == 'stand':
        arms = [((-0.27, 0, 0), (-0.36, -0.02, -0.28), (-0.38, -0.08, -0.55)), ((0.27, 0, 0), (0.36, -0.02, -0.28), (0.38, -0.08, -0.55))]
        legs = [((-0.11, 0, 0), (-0.14, -0.02, -0.45), (-0.15, 0.02, -0.88)), ((0.11, 0, 0), (0.14, 0.03, -0.45), (0.16, 0.06, -0.88))]
    elif pose == 'wave':
        arms = [((-0.27, 0, 0), (-0.36, -0.02, -0.28), (-0.38, -0.08, -0.55)), ((0.27, 0, 0), (0.52, 0.0, 0.2), (0.6, -0.05, 0.55))]
        legs = [((-0.11, 0, 0), (-0.17, -0.02, -0.45), (-0.2, 0.02, -0.88)), ((0.11, 0, 0), (0.15, 0.03, -0.45), (0.18, 0.06, -0.88))]
    elif pose == 'run':
        arms = [((-0.27, 0, 0), (-0.34, 0.2, -0.22), (-0.36, 0.32, -0.45)), ((0.27, 0, 0), (0.34, -0.2, -0.2), (0.34, -0.38, 0.0))]
        legs = [((-0.11, 0, 0), (-0.13, -0.3, -0.35), (-0.14, -0.2, -0.82)), ((0.11, 0, 0), (0.13, 0.25, -0.42), (0.14, 0.5, -0.75))]
    else:   # sit: thighs forward, shins down, hands on knees
        arms = [((-0.27, 0, 0), (-0.32, -0.15, -0.25), (-0.22, -0.4, -0.32)), ((0.27, 0, 0), (0.32, -0.15, -0.25), (0.22, -0.4, -0.32))]
        legs = [((-0.12, 0, 0), (-0.14, -0.45, 0.0), (-0.15, -0.5, -0.5)), ((0.12, 0, 0), (0.14, -0.45, 0.0), (0.15, -0.5, -0.5))]
    for a in arms:
        s, e, h = sh + Vector(a[0]), sh + Vector(a[1]), sh + Vector(a[2])
        body += [ball(tuple(s), 0.22), limb(s, e, 0.16), limb(e, h, 0.14), ball(tuple(h), 0.15)]
    feet = []
    for lg in legs:
        s, k, f = Vector(hip) + Vector(lg[0]), Vector(hip) + Vector(lg[1]), Vector(hip) + Vector(lg[2])
        body += [limb(s, k, 0.2), limb(k, f, 0.17)]
        feet.append(f)
    for f in feet: body.append(egg(tuple(f + Vector((0, -0.07, -0.05))), (0.16, 0.28, 0.13)))
    suit = clay(name + '_suit', body, res=0.02, disp=0.012, dscale=0.08, smooth=2)
    helm = clay(name + '_helm', [ball(head, 0.4), egg((head[0], head[1] + 0.02, head[2] - 0.16), (0.36, 0.34, 0.12))], res=0.018, disp=0.006, dscale=0.06, smooth=2)
    visor = clay(name + '_visor', [egg((head[0], head[1] - 0.12, head[2] + 0.01), (0.3, 0.18, 0.24))], res=0.015, disp=0, smooth=1)
    pack = clay(name + '_pack', [egg((chest[0], chest[1] + 0.27, chest[2] + 0.02), (0.46, 0.26, 0.62)),
                                 limb((chest[0] - 0.18, chest[1] + 0.32, chest[2] - 0.32), (chest[0] + 0.18, chest[1] + 0.32, chest[2] - 0.32), 0.12)],
                res=0.02, disp=0.012, dscale=0.08, smooth=2)
    lamp = clay(name + '_lamp', [ball((head[0] + 0.17, head[1] - 0.08, head[2] + 0.12), 0.07)], res=0.012, disp=0, smooth=0)
    return join(name, [(suit, M['suit']), (helm, M['suit']), (visor, M['visor']), (pack, M['pack']), (lamp, M['winCool'])])

def make_rocks():
    for i in range(6):
        el = [ball((0, 0, 0), 1.6)]
        for k in range(R.randint(2, 4)):
            el.append(ball((R.uniform(-0.7, 0.7), R.uniform(-0.7, 0.7), R.uniform(-0.3, 0.5)), R.uniform(0.8, 1.3)))
        me = clay('rk%d' % i, el, res=0.07, disp=0.35, dscale=0.45, lumps=0.25, smooth=2)
        A['rock%d' % i] = join('Rock%d' % i, [(me, M['rock'])])
    for i in range(6):   # darker chunks for space
        el = [egg((0, 0, 0), (R.uniform(1, 1.8), R.uniform(0.8, 1.4), R.uniform(0.7, 1.2)))]
        for k in range(3): el.append(ball((R.uniform(-0.5, 0.5), R.uniform(-0.4, 0.4), R.uniform(-0.3, 0.3)), R.uniform(0.6, 1.0)))
        me = clay('db%d' % i, el, res=0.05, disp=0.3, dscale=0.35, lumps=0.2, smooth=1)
        A['debris%d' % i] = join('Debris%d' % i, [(me, M['rockDark'])])

def make_spires():
    """Tall tilted rock fins, like broken slabs leaning out of the plain."""
    for i in range(4):
        el = []
        h = R.uniform(28, 46); tilt = R.uniform(0.45, 0.8)
        for k in range(7):
            t = k / 6.0
            el.append(egg((t * h * math.sin(tilt) * 0.9, 0, t * h * math.cos(tilt)), (h * 0.28 * (1.1 - t * 0.7), 3.8 * (1.1 - t * 0.5), h * 0.22), (0, tilt, 0)))
        el.append(egg((h * 0.55 * math.sin(tilt), 0.6, h * 0.62 * math.cos(tilt)), (4, 2.5, 3), (0.3, tilt + 0.4, 0)))
        me = clay('sp%d' % i, el, res=0.55, disp=2.2, dscale=3.5, lumps=1.6, smooth=2)
        A['spire%d' % i] = join('Spire%d' % i, [(me, M['rock'])])
    cliff = [egg((0, 0, -2), (30, 22, 14))]
    for k in range(10):
        cliff.append(egg((R.uniform(-12, 12), R.uniform(-9, 9), R.uniform(0, 5)), (R.uniform(6, 12), R.uniform(5, 10), R.uniform(4, 8))))
    cliff.append(egg((0, -10, 6.5), (8, 5, 3)))   # the ledge the cadet sits on
    me = clay('cliff', cliff, res=0.45, disp=1.4, dscale=2.5, lumps=1.0, smooth=3)
    A['cliff'] = join('Cliff', [(me, M['rock'])])

def make_base():
    shell = clay('base_shell', [
        egg((0, 0, 0), (16, 16, 11)), egg((19, 3, 0), (10, 10, 7)), egg((-15, 8, 0), (9, 9, 6)),
        limb((0, 0, 1.2), (19, 3, 1.2), 3.2), limb((0, 0, 1.2), (-15, 8, 1.2), 3.0),
        egg((8, -10, 0.5), (6, 4, 4)), limb((5, -5, 1.0), (8, -10, 1.0), 2.4),
        limb((-4, -4, 5), (-4.4, -4.6, 15), 0.45), egg((-4.4, -4.8, 15.6), (0.5, 3.2, 2.6), (0.5, 0, 0.3)),
    ], res=0.18, disp=0.12, dscale=1.2, lumps=0.18, smooth=3)
    wins = []
    for k in range(8):
        a = -0.6 + k * 0.2
        wins.append(egg((7.6 * math.sin(a), -7.6 * math.cos(a), 2.8), (0.9, 0.5, 0.55), (0, 0, a)))
    for k in range(4): wins.append(egg((16 + k * 2, -1.5, 1.6), (0.7, 0.4, 0.5)))
    wins.append(egg((8, -12.1, 1.0), (1.6, 0.4, 1.8)))    # door light
    win = clay('base_win', wins, res=0.06, disp=0, smooth=0)
    panels = []
    for r in range(2):
        for c in range(5):
            panels.append(egg((-22 + c * 3.6, 16 + r * 3.6, 1.3), (3.0, 2.6, 0.2), (-0.5, 0, 0)))
    pan = clay('base_pan', panels, res=0.12, disp=0.05, dscale=0.8, smooth=2)
    legs = clay('base_legs', [limb((-22 + c * 3.6, 16 + r * 3.6, 0), (-22 + c * 3.6, 16 + r * 3.6, 1.2), 0.25) for r in range(2) for c in range(5)], res=0.08, disp=0.02, smooth=1)
    beacon = clay('base_beacon', [ball((-4.4, -4.6, 17.4), 0.6)], res=0.05, disp=0, smooth=0)
    A['base'] = join('Base', [(shell, M['base']), (win, M['win']), (pan, M['panel']), (legs, M['hullDark']), (beacon, M['red'])])

TN, TSIZE = 360, 420.0
def make_terrain():
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=TN, y_segments=TN, size=TSIZE / 2)
    for v in bm.verts:
        v.co.z = terrain_h(v.co.x, v.co.y)
    me = bpy.data.meshes.new('terrain_m'); bm.to_mesh(me); bm.free()
    try: me.shade_smooth()
    except Exception: pass
    me.materials.append(M['ground'])
    A['terrain'] = bpy.data.objects.new('Terrain', me); LIB.objects.link(A['terrain'])

def terrain_h(x, y):
    p = Vector((x / 90.0, y / 90.0, 0.3))
    h = noise.hetero_terrain(p, 0.9, 2.0, 5, 0.6) * 9.0 - 6
    h += noise.noise(Vector((x / 9.0, y / 9.0, 2.0))) * 0.7
    h += math.sin(x * 0.12 + noise.noise(Vector((x / 30, y / 30, 5))) * 3) * 0.6
    d = math.hypot(x, y)
    flat = min(1.0, max(0.0, (d - 30) / 40.0))
    h = h * flat + (1 - flat) * (-0.6)
    rim = max(0.0, d - 150) / 60.0
    return h + rim * 9 + rim * noise.noise(Vector((x / 40, y / 40, 9.0))) * 8

def ground_h(x, y): return terrain_h(x, y)

def make_corridor():
    """Ship corridor along +X: clay ribs, wall pads, pipes, crates, a lit EVAC door at the far end."""
    ribs, pads, pipes, lights = [], [], [], []
    for i in range(9):
        x = i * 2.6
        pts = [(x, -2.0, -0.2), (x, -2.3, 1.2), (x, -1.8, 2.6), (x, -0.7, 3.3), (x, 0.7, 3.3), (x, 1.8, 2.6), (x, 2.3, 1.2), (x, 2.0, -0.2)]
        for a, b in zip(pts, pts[1:]): ribs.append(limb(a, b, 0.42))
        if i < 8:
            pads += [egg((x + 1.3, -2.25, 1.2), (2.1, 0.3, 2.4)), egg((x + 1.3, 2.25, 1.2), (2.1, 0.3, 2.4)),
                     egg((x + 1.3, 0, 3.4), (2.1, 1.6, 0.3))]
            lights.append(egg((x + 1.3, 0, 3.22), (1.4, 0.18, 0.06)))
    ribs.append(egg((10.4, 0, -0.35), (24, 4.4, 0.5)))   # floor
    for side in (-1, 1):
        pipes += [limb((-1, side * 1.95, 2.3), (21, side * 1.95, 2.3), 0.22), limb((-1, side * 1.85, 2.0), (21, side * 1.85, 2.0), 0.14)]
    end = [egg((21.6, 0, 1.4), (0.6, 5, 4.4)), dent((21.2, 0, 1.2), 2.6)]
    shell = clay('cor_shell', ribs + end, res=0.06, disp=0.04, dscale=0.4, lumps=0.05, smooth=2)
    pad = clay('cor_pad', pads, res=0.06, disp=0.03, dscale=0.4, smooth=2)
    pip = clay('cor_pipe', pipes, res=0.04, disp=0.01, dscale=0.3, smooth=1)
    lit = clay('cor_lit', lights, res=0.03, disp=0, smooth=0)
    crates = clay('cor_crate', [('CUBE', (5.0, -1.4, 0.15), 0.3, (0.35, 0.35, 0.3), None, 2.0), ('CUBE', (5.0, -1.4, 0.95), 0.25, (0.3, 0.3, 0.25), (0, 0, 0.3), 2.0),
                                ('CUBE', (13.5, 1.4, 0.2), 0.3, (0.4, 0.3, 0.35), None, 2.0)], res=0.04, disp=0.02, dscale=0.3, smooth=2)
    evac = clay('cor_evac', [egg((21.0, 0, 3.0), (0.2, 1.4, 0.45))], res=0.03, disp=0, smooth=0)
    door = clay('cor_door', [egg((21.15, 0, 1.25), (0.3, 2.2, 2.6))], res=0.04, disp=0.02, smooth=1)
    A['corridor'] = join('Corridor', [(shell, M['wall']), (pad, M['wall']), (pip, M['hullDark']), (lit, M['strip']), (crates, M['crate']),
                                      (evac, M['evac']), (door, M['hullDark'])])
    ring = []
    for k in range(28):
        a = k / 28 * math.tau
        ring.append(ball((0, 2.2 * math.cos(a), 2.2 * math.sin(a)), 0.7))
    me = clay('porthole', ring, res=0.05, disp=0.03, dscale=0.3, smooth=2)
    A['porthole'] = join('Porthole', [(me, M['wall'])])
    wall = []
    for k in range(10):
        a = k / 10 * math.tau
        wall.append(egg((0.15, 4.3 * math.cos(a), 4.3 * math.sin(a)), (0.45, 3.4, 3.4), (a, 0, 0)))
    me = clay('portwall', wall, res=0.08, disp=0.05, dscale=0.5, smooth=2)
    A['portwall'] = join('PortWall', [(me, M['wall'])])
    me = clay('slab', [egg((0, 0, 0), (10, 8, 0.4)), egg((-2, 2, 0.1), (5, 3, 0.3))], res=0.08, disp=0.04, dscale=0.5, smooth=2)
    A['slab'] = join('Slab', [(me, M['wall'])])

def make_planet():
    me = clay('mars_p', [ball((0, 0, 0), 2.0)], res=0.02, disp=0, smooth=0)
    m = bpy.data.materials.new('marsSurf'); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = node(nt, 'ShaderNodeOutputMaterial', (900, 0)); bs = node(nt, 'ShaderNodeBsdfPrincipled', (600, 0)); bs.inputs['Roughness'].default_value = 0.9
    tc = node(nt, 'ShaderNodeTexCoord', (-900, 0))
    nz = node(nt, 'ShaderNodeTexNoise', (-650, 150), Scale=4.5, Detail=14.0, Roughness=0.66)
    L(nt, tc.outputs['Object'], nz.inputs['Vector'])
    rp = node(nt, 'ShaderNodeValToRGB', (-350, 150))
    e = rp.color_ramp.elements
    e[0].position = 0.42; e[0].color = (0.09, 0.035, 0.02, 1); e[1].position = 0.58; e[1].color = (0.8, 0.36, 0.13, 1)
    el = rp.color_ramp.elements.new(0.5); el.color = (0.45, 0.17, 0.07, 1)
    L(nt, nz.outputs['Fac'], rp.inputs['Fac'])
    sep = node(nt, 'ShaderNodeSeparateXYZ', (-650, -150)); L(nt, tc.outputs['Object'], sep.inputs['Vector'])
    cap = node(nt, 'ShaderNodeMapRange', (-350, -150)); cap.inputs['From Min'].default_value = 0.88; cap.inputs['From Max'].default_value = 0.93
    L(nt, sep.outputs['Z'], cap.inputs['Value'])
    mix = node(nt, 'ShaderNodeMix', (0, 100)); mix.data_type = 'RGBA'
    L(nt, cap.outputs['Result'], mix.inputs[0]); L(nt, rp.outputs['Color'], mix.inputs[6]); mix.inputs[7].default_value = (0.9, 0.85, 0.8, 1)
    L(nt, mix.outputs[2], bs.inputs['Base Color'])
    nz2 = node(nt, 'ShaderNodeTexNoise', (-650, -400), Scale=40.0, Detail=8.0)
    L(nt, tc.outputs['Object'], nz2.inputs['Vector'])
    bp = node(nt, 'ShaderNodeBump', (300, -300), Strength=0.4, Distance=0.02)
    L(nt, nz2.outputs['Fac'], bp.inputs['Height']); L(nt, bp.outputs['Normal'], bs.inputs['Normal'])
    L(nt, bs.outputs['BSDF'], out.inputs['Surface'])
    atm = clay('mars_atm', [ball((0, 0, 0), 2.0)], res=0.02, disp=0, smooth=0)
    A['mars'] = join('MarsPlanet', [(me, m)])
    A['marsAtm'] = join('MarsAtmos', [(atm, glow_mat('atm', (1.0, 0.42, 0.18), 3.0, 6.0))])
    sm = clay('sunball', [ball((0, 0, 0), 2.0)], res=0.1, disp=0, smooth=0)
    A['sun'] = join('SunBall', [(sm, emit_mat('sunE', (1.0, 0.95, 0.88), 400))])
    em = clay('earthball', [ball((0, 0, 0), 2.0)], res=0.1, disp=0, smooth=0)
    A['earth'] = join('EarthBall', [(em, emit_mat('earthE', (0.45, 0.7, 1.0), 200))])

def make_fx():
    def blobfx(name, mat, el, res=0.05):
        me = clay(name, el, res=res, disp=0.0, smooth=1)
        A[name] = join(name, [(me, mat)])
    fb = [ball((0, 0, 0), 2.0)] + [ball((R.uniform(-0.8, 0.8), R.uniform(-0.8, 0.8), R.uniform(-0.8, 0.8)), R.uniform(1.0, 1.6)) for _ in range(5)]
    blobfx('fireball', M['fire'], fb, 0.06)
    blobfx('smokeball', M['smoke'], [ball((0, 0, 0), 2.0)] + [ball((R.uniform(-1, 1), R.uniform(-1, 1), R.uniform(-0.6, 0.6)), 1.4) for _ in range(5)], 0.08)
    blobfx('flame', M['flame'], [egg((0, 0, 0), (3.5, 0.9, 0.9))], 0.03)
    blobfx('flameBlue', M['flameBlue'], [egg((0, 0, 0), (3.5, 0.9, 0.9))], 0.03)
    blobfx('plasma', M['plasma'], [egg((0.6, 0, 0), (3.6, 2.6, 2.4)), egg((-5.5, 0, 0), (12, 1.6, 1.4))], 0.06)
    blobfx('streak', M['spark'], [egg((0, 0, 0), (1.0, 0.03, 0.03))], 0.008)
    blobfx('trail', M['beam'], [egg((0, 0, 0), (40, 0.5, 0.5))], 0.08)
    blobfx('beam', M['beam'], [egg((0, 0, 0), (0.6, 0.6, 9))], 0.05)

def build_lib():
    reset(); make_materials(); make_ship(); make_lifeboat(); make_rocks(); make_spires(); make_base(); make_terrain()
    make_corridor(); make_planet(); make_fx()
    A['cadet_stand'] = astronaut('CadetStand', 'stand'); A['cadet_wave'] = astronaut('CadetWave', 'wave')
    A['cadet_sit'] = astronaut('CadetSit', 'sit'); A['cadet_run'] = astronaut('CadetRun', 'run')
    LIB.hide_render = True

# ------------------------------------------------------------------ worlds

def world_space(name, band=(0.3, 0.2, 0.9), core=(1.0, 0.75, 0.55), haze=(0.35, 0.45, 0.6), mw=0.5, stars=6.0):
    """Deep space: sharp stars plus a dusty galaxy band with dark lanes."""
    w = bpy.data.worlds.new(name); w.use_nodes = True
    nt = w.node_tree; nt.nodes.clear()
    out = node(nt, 'ShaderNodeOutputWorld', (1200, 0))
    tc = node(nt, 'ShaderNodeTexCoord', (-1200, 0)); d = tc.outputs['Generated']
    vor = node(nt, 'ShaderNodeTexVoronoi', (-900, 300), Scale=700.0); L(nt, d, vor.inputs['Vector'])
    st = node(nt, 'ShaderNodeMapRange', (-650, 300)); st.inputs['From Min'].default_value = 0.0; st.inputs['From Max'].default_value = 0.14
    st.inputs['To Min'].default_value = 1.0; st.inputs['To Max'].default_value = 0.0
    L(nt, vor.outputs['Distance'], st.inputs['Value'])
    sp = node(nt, 'ShaderNodeMath', (-450, 300)); sp.operation = 'POWER'; sp.inputs[1].default_value = 14; L(nt, st.outputs['Result'], sp.inputs[0])
    sm = node(nt, 'ShaderNodeMath', (-250, 300)); sm.operation = 'MULTIPLY'; sm.inputs[1].default_value = stars; L(nt, sp.outputs['Value'], sm.inputs[0])
    dot = node(nt, 'ShaderNodeVectorMath', (-900, -100)); dot.operation = 'DOT_PRODUCT'
    dot.inputs[1].default_value = Vector(band).normalized(); L(nt, d, dot.inputs[0])
    ab = node(nt, 'ShaderNodeMath', (-700, -100)); ab.operation = 'ABSOLUTE'; L(nt, dot.outputs['Value'], ab.inputs[0])
    bd = node(nt, 'ShaderNodeMapRange', (-500, -100)); bd.inputs['From Min'].default_value = 0.0; bd.inputs['From Max'].default_value = 0.42
    bd.inputs['To Min'].default_value = 1.0; bd.inputs['To Max'].default_value = 0.0; L(nt, ab.outputs['Value'], bd.inputs['Value'])
    bp = node(nt, 'ShaderNodeMath', (-300, -100)); bp.operation = 'POWER'; bp.inputs[1].default_value = 2.2; L(nt, bd.outputs['Result'], bp.inputs[0])
    cl = node(nt, 'ShaderNodeTexNoise', (-900, -400), Scale=5.5, Detail=16.0, Roughness=0.72); L(nt, d, cl.inputs['Vector'])
    cr = node(nt, 'ShaderNodeMapRange', (-650, -400)); cr.inputs['From Min'].default_value = 0.42; cr.inputs['From Max'].default_value = 0.75; L(nt, cl.outputs['Fac'], cr.inputs['Value'])
    ln = node(nt, 'ShaderNodeTexNoise', (-900, -650), Scale=11.0, Detail=12.0, Roughness=0.7); L(nt, d, ln.inputs['Vector'])
    lr = node(nt, 'ShaderNodeMapRange', (-650, -650)); lr.inputs['From Min'].default_value = 0.5; lr.inputs['From Max'].default_value = 0.62
    lr.inputs['To Min'].default_value = 1.0; lr.inputs['To Max'].default_value = 0.15; L(nt, ln.outputs['Fac'], lr.inputs['Value'])
    g1 = node(nt, 'ShaderNodeMath', (-100, -250)); g1.operation = 'MULTIPLY'; L(nt, bp.outputs['Value'], g1.inputs[0]); L(nt, cr.outputs['Result'], g1.inputs[1])
    g2 = node(nt, 'ShaderNodeMath', (100, -250)); g2.operation = 'MULTIPLY'; L(nt, g1.outputs['Value'], g2.inputs[0]); L(nt, lr.outputs['Result'], g2.inputs[1])
    gc = node(nt, 'ShaderNodeMix', (300, -250)); gc.data_type = 'RGBA'
    L(nt, cr.outputs['Result'], gc.inputs[0]); gc.inputs[6].default_value = (*haze, 1); gc.inputs[7].default_value = (*core, 1)
    gs = node(nt, 'ShaderNodeEmission', (550, -250)); L(nt, gc.outputs[2], gs.inputs['Color'])
    gm = node(nt, 'ShaderNodeMath', (350, -450)); gm.operation = 'MULTIPLY'; gm.inputs[1].default_value = mw; L(nt, g2.outputs['Value'], gm.inputs[0])
    L(nt, gm.outputs['Value'], gs.inputs['Strength'])
    se = node(nt, 'ShaderNodeEmission', (550, 300)); L(nt, vor.outputs['Color'], se.inputs['Color']); L(nt, sm.outputs['Value'], se.inputs['Strength'])
    ad = node(nt, 'ShaderNodeAddShader', (850, 0)); L(nt, se.outputs['Emission'], ad.inputs[0]); L(nt, gs.outputs['Emission'], ad.inputs[1])
    L(nt, ad.outputs['Shader'], out.inputs['Surface'])
    return w

def world_sky(name, horizon, zenith, sun_dir, sun_col, sun_glow=2.0, glow_pow=20.0, strength=1.0, stars=0.0):
    """Mars sky: gradient from a dusty horizon to the zenith, a wide glow around the sun."""
    w = bpy.data.worlds.new(name); w.use_nodes = True
    nt = w.node_tree; nt.nodes.clear()
    out = node(nt, 'ShaderNodeOutputWorld', (1100, 0))
    tc = node(nt, 'ShaderNodeTexCoord', (-1100, 0)); d = tc.outputs['Generated']
    sep = node(nt, 'ShaderNodeSeparateXYZ', (-850, 200)); L(nt, d, sep.inputs['Vector'])
    zr = node(nt, 'ShaderNodeMapRange', (-650, 200)); zr.inputs['From Min'].default_value = -0.05; zr.inputs['From Max'].default_value = 0.6
    L(nt, sep.outputs['Z'], zr.inputs['Value'])
    zp = node(nt, 'ShaderNodeMath', (-450, 200)); zp.operation = 'POWER'; zp.inputs[1].default_value = 0.6; L(nt, zr.outputs['Result'], zp.inputs[0])
    mx = node(nt, 'ShaderNodeMix', (-200, 200)); mx.data_type = 'RGBA'
    L(nt, zp.outputs['Value'], mx.inputs[0]); mx.inputs[6].default_value = (*horizon, 1); mx.inputs[7].default_value = (*zenith, 1)
    dt = node(nt, 'ShaderNodeVectorMath', (-850, -200)); dt.operation = 'DOT_PRODUCT'; dt.inputs[1].default_value = Vector(sun_dir).normalized()
    L(nt, d, dt.inputs[0])
    cl = node(nt, 'ShaderNodeMath', (-650, -200)); cl.operation = 'MAXIMUM'; cl.inputs[1].default_value = 0.0; L(nt, dt.outputs['Value'], cl.inputs[0])
    pw = node(nt, 'ShaderNodeMath', (-450, -200)); pw.operation = 'POWER'; pw.inputs[1].default_value = glow_pow; L(nt, cl.outputs['Value'], pw.inputs[0])
    pw2 = node(nt, 'ShaderNodeMath', (-450, -400)); pw2.operation = 'POWER'; pw2.inputs[1].default_value = 900.0; L(nt, cl.outputs['Value'], pw2.inputs[0])
    m2 = node(nt, 'ShaderNodeMath', (-250, -500)); m2.operation = 'MULTIPLY'; m2.inputs[1].default_value = 60.0; L(nt, pw2.outputs['Value'], m2.inputs[0])
    sm = node(nt, 'ShaderNodeMath', (-250, -300)); sm.operation = 'MULTIPLY_ADD'; sm.inputs[1].default_value = sun_glow
    L(nt, pw.outputs['Value'], sm.inputs[0]); L(nt, m2.outputs['Value'], sm.inputs[2])
    sv = node(nt, 'ShaderNodeVectorMath', (-50, -300)); sv.operation = 'SCALE'; sv.inputs[0].default_value = sun_col
    L(nt, sm.outputs['Value'], sv.inputs['Scale'])
    sc = node(nt, 'ShaderNodeMix', (100, -100)); sc.data_type = 'RGBA'; sc.blend_type = 'ADD'
    sc.inputs[0].default_value = 1.0
    L(nt, mx.outputs[2], sc.inputs[6]); L(nt, sv.outputs['Vector'], sc.inputs[7])
    bg = node(nt, 'ShaderNodeBackground', (500, 0), Strength=strength); L(nt, sc.outputs[2], bg.inputs['Color'])
    if stars > 0:
        vor = node(nt, 'ShaderNodeTexVoronoi', (-850, 500), Scale=600.0); L(nt, d, vor.inputs['Vector'])
        st = node(nt, 'ShaderNodeMapRange', (-650, 500)); st.inputs['From Max'].default_value = 0.14; st.inputs['To Min'].default_value = 1.0; st.inputs['To Max'].default_value = 0.0
        L(nt, vor.outputs['Distance'], st.inputs['Value'])
        sp = node(nt, 'ShaderNodeMath', (-450, 500)); sp.operation = 'POWER'; sp.inputs[1].default_value = 14; L(nt, st.outputs['Result'], sp.inputs[0])
        sz = node(nt, 'ShaderNodeMath', (-250, 500)); sz.operation = 'MULTIPLY'; L(nt, sp.outputs['Value'], sz.inputs[0]); L(nt, zr.outputs['Result'], sz.inputs[1])
        se = node(nt, 'ShaderNodeEmission', (500, 300), Color=(1, 1, 1, 1)); m3 = node(nt, 'ShaderNodeMath', (250, 500)); m3.operation = 'MULTIPLY'; m3.inputs[1].default_value = stars
        L(nt, sz.outputs['Value'], m3.inputs[0]); L(nt, m3.outputs['Value'], se.inputs['Strength'])
        ad = node(nt, 'ShaderNodeAddShader', (800, 100)); L(nt, bg.outputs['Background'], ad.inputs[0]); L(nt, se.outputs['Emission'], ad.inputs[1])
        L(nt, ad.outputs['Shader'], out.inputs['Surface'])
    else:
        L(nt, bg.outputs['Background'], out.inputs['Surface'])
    return w

# ------------------------------------------------------------------ shot helpers

def new_shot(name, world):
    sc = bpy.data.scenes.new(name); sc.world = world
    sc.render.engine = 'CYCLES'
    try: sc.cycles.device = 'GPU'
    except Exception: pass
    sc.cycles.samples = 160; sc.cycles.use_adaptive_sampling = True; sc.cycles.adaptive_threshold = 0.02
    sc.cycles.use_denoising = True
    sc.cycles.max_bounces = 6; sc.cycles.volume_bounces = 1; sc.cycles.transparent_max_bounces = 16
    sc.cycles.volume_step_rate = 2.0; sc.cycles.volume_preview_step_rate = 4.0
    sc.cycles.caustics_reflective = False; sc.cycles.caustics_refractive = False
    sc.cycles.sample_clamp_indirect = 8.0
    sc.render.resolution_x, sc.render.resolution_y = 1920, 1080
    sc.render.image_settings.file_format = 'PNG'
    try: sc.view_settings.view_transform = 'AgX'
    except TypeError: pass
    for lk in ('AgX - Punchy', 'Punchy', 'AgX - High Contrast', 'High Contrast'):
        try: sc.view_settings.look = lk; break
        except TypeError: continue
    return sc

def put(sc, key, loc, rot=(0, 0, 0), scale=1.0, name=None):
    src = A[key]; o = src.copy(); o.name = name or (sc.name + "_" + key)
    o.location = loc; o.rotation_euler = rot; o.scale = (scale, scale, scale) if isinstance(scale, (int, float)) else scale
    sc.collection.objects.link(o); return o

def cam(sc, loc, target, lens=35, dof=None, fstop=2.8, shift=(0, 0), roll=0.0):
    cd = bpy.data.cameras.new(sc.name + "_cam"); cd.lens = lens; cd.clip_end = 20000; cd.clip_start = 0.05
    cd.shift_x, cd.shift_y = shift
    if dof:
        cd.dof.use_dof = True; cd.dof.focus_distance = dof; cd.dof.aperture_fstop = fstop
    co = bpy.data.objects.new(sc.name + "_cam", cd); sc.collection.objects.link(co)
    co.location = loc
    d = Vector(target) - Vector(loc)
    co.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    co.rotation_euler.rotate_axis('Z', roll)
    sc.camera = co; return co

def mat_of(o): return Matrix.LocRotScale(o.location, o.rotation_euler.to_quaternion(), o.scale)

def at(c, sx, sy, depth):
    """World point at screen position (sx, sy in -1..1 of the frame) and a depth in front of camera c."""
    hw = depth * 18.0 / c.data.lens; hh = hw * 9 / 16
    return mat_of(c) @ Vector((sx * hw, sy * hh, -depth))

def face(o, direction):
    """Point the object's +X (nose) along direction, keeping +Z roughly up."""
    o.rotation_euler = Vector(direction).normalized().to_track_quat('X', 'Z').to_euler()

def roll(o, angle):
    o.rotation_euler.rotate_axis('X', angle)

_bvh = {}
def surface_z(o, x, y, top=800.0):
    """Height of the top of object o's surface at (x, y), or None."""
    from mathutils.bvhtree import BVHTree
    me = o.data
    if me.name not in _bvh:
        _bvh[me.name] = BVHTree.FromPolygons([v.co.copy() for v in me.vertices], [tuple(p.vertices) for p in me.polygons])
    mw = mat_of(o); mi = mw.inverted()
    org = mi @ Vector((x, y, top)); dv = (mi.to_3x3() @ Vector((0, 0, -1))).normalized()
    hit = _bvh[me.name].ray_cast(org, dv)
    return None if hit[0] is None else (mw @ hit[0]).z

def attach(sc, parent, key, local, lrot=(0, 0, 0), scale=1.0, name=None):
    o = put(sc, key, (0, 0, 0), name=name)
    s = (scale,) * 3 if isinstance(scale, (int, float)) else scale
    o.matrix_world = mat_of(parent) @ Matrix.LocRotScale(Vector(local), Euler(lrot).to_quaternion(), Vector(s))
    return o

def attach_engines(sc, ship):
    for i, (sy, sz) in enumerate([(0.95, 0.95), (-0.95, 0.95), (0.95, -0.95), (-0.95, -0.95)]):
        attach(sc, ship, 'flameBlue', (-10.4, sy, sz), (0, 0, 0), (0.6, 0.5, 0.5), name='%s_fl%d' % (sc.name, i))
    lamp(sc, tuple(mat_of(ship) @ Vector((-10.5, 0, 0))), (0.4, 0.7, 1.0), 2500 * ship.scale[0] ** 2, 1.0, name='eng')

def sun(sc, direction_to_sun, color, strength, angle=0.02, name='sun'):
    ld = bpy.data.lights.new(sc.name + name, 'SUN'); ld.color = color; ld.energy = strength; ld.angle = angle
    o = bpy.data.objects.new(sc.name + name, ld); sc.collection.objects.link(o)
    o.rotation_euler = Vector(direction_to_sun).normalized().to_track_quat('Z', 'Y').to_euler(); return o

def lamp(sc, loc, color, power, radius=0.2, kind='POINT', target=None, spot=0.8, name='lamp'):
    ld = bpy.data.lights.new(sc.name + name, kind); ld.color = color; ld.energy = power
    try: ld.shadow_soft_size = radius
    except Exception: pass
    if kind == 'SPOT': ld.spot_size = spot; ld.spot_blend = 0.6
    if kind == 'AREA': ld.size = radius
    o = bpy.data.objects.new(sc.name + name, ld); sc.collection.objects.link(o); o.location = loc
    if target is not None:
        o.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    return o

def haze_box(sc, center, size, color, density, aniso=0.5):
    me = bpy.data.meshes.new(sc.name + "_haze"); bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0); bm.to_mesh(me); bm.free()
    me.materials.append(haze_mat(sc.name + "_hazeM", color, density, aniso))
    o = bpy.data.objects.new(sc.name + "_haze", me); sc.collection.objects.link(o)
    o.location = center; o.scale = size; o.visible_shadow = False
    return o

def scatter_rocks(sc, center, radius, count, smin, smax, seed=1, avoid=None):
    rr = random.Random(seed)
    for i in range(count):
        a = rr.uniform(0, math.tau); r = radius * math.sqrt(rr.random())
        p = Vector(center) + Vector((math.cos(a) * r, math.sin(a) * r))
        if avoid and (p - Vector(avoid[0])).length < avoid[1]: continue
        s = rr.uniform(smin, smax)
        put(sc, 'rock%d' % rr.randint(0, 5), (p.x, p.y, ground_h(p.x, p.y) - s * 0.3), (rr.uniform(0, 3), rr.uniform(0, 3), rr.uniform(0, 6)),
            (s, s * rr.uniform(0.8, 1.2), s * rr.uniform(0.5, 0.9)), name="%s_rk%d" % (sc.name, i))

def mars_set(sc, spires=True, base=True):
    put(sc, 'terrain', (0, 0, 0))
    if base: put(sc, 'base', (0, 0, -0.4), (0, 0, 0.35))
    if spires:
        put(sc, 'spire0', (-60, 95, -4), (0, 0, 2.2), 1.0)
        put(sc, 'spire1', (-25, 120, -4), (0, 0, 2.6), 1.2)
        put(sc, 'spire2', (40, 105, -4), (0, 0, 1.9), 0.9)
        put(sc, 'spire3', (80, 140, -4), (0, 0, 2.4), 1.3)
        put(sc, 'spire1', (-110, 150, -4), (0, 0, 2.0), 1.5, name=sc.name + '_sp5')
        put(sc, 'spire2', (130, 60, -4), (0, 0, 3.6), 1.1, name=sc.name + '_sp6')

# ------------------------------------------------------------------ the shots

SHOTS = {}
def shot(f): SHOTS[f.__name__] = f; return f

@shot
def c01_cruise():
    sc = new_shot('c01_cruise', world_space('w01', band=(0.25, 0.4, 0.88), mw=0.7, stars=40))
    c = cam(sc, (0, 0, 0), (0, 1, 0.05), lens=35)
    sunp = at(c, -0.24, 0.3, 3000)
    put(sc, 'sun', tuple(sunp), (0, 0, 0), 55)
    sun(sc, sunp, (1.0, 0.95, 0.88), 6.0, 0.01)
    ship = put(sc, 'ship', tuple(at(c, 0.3, -0.16, 46)))
    face(ship, at(c, -1.6, 0.35, 70) - ship.location); roll(ship, 0.9)
    attach_engines(sc, ship)
    p = at(c, -0.32, -1.3, 520)
    put(sc, 'mars', tuple(p), (0.3, 0.2, 0.5), 150); put(sc, 'marsAtm', tuple(p), (0, 0, 0), 150.6)
    lamp(sc, (8, -6, -6), (0.3, 0.45, 0.85), 5e3, 12, 'AREA', target=tuple(ship.location), name='fill')
    lamp(sc, tuple(ship.location + Vector((-25, -10, 22))), (1.0, 0.85, 0.7), 2.5e4, 10, 'AREA', target=tuple(ship.location), name='key')

@shot
def c02_mars():
    sc = new_shot('c02_mars', world_space('w02', band=(0.8, 0.1, 0.6), mw=0.5, stars=40))
    c = cam(sc, (0, 0, 0), (0, 1, -0.22), lens=28)
    p = at(c, 0.1, -1.6, 300)
    put(sc, 'mars', tuple(p), (0.2, 0.4, 0.3), 165); put(sc, 'marsAtm', tuple(p), (0, 0, 0), 165.7)
    sunp = at(c, 0.9, 0.12, 4000)
    put(sc, 'sun', tuple(sunp), (0, 0, 0), 30)
    sun(sc, sunp, (1.0, 0.92, 0.82), 7.0, 0.01)
    ship = put(sc, 'ship', tuple(at(c, -0.38, 0.32, 26)), (0, 0, 0), 0.45)
    face(ship, p - ship.location + Vector((0, 0, 120))); roll(ship, 0.6)
    attach_engines(sc, ship)

@shot
def c03_cadet():
    """The cadet at a porthole, a dark cabin, Mars light pouring in through the haze."""
    sc = new_shot('c03_cadet', world_space('w03', band=(0.2, 0.9, 0.3), mw=0.4, stars=40))
    put(sc, 'portwall', (0.15, 0, 2.2))
    put(sc, 'porthole', (0.0, 0, 2.2))
    put(sc, 'slab', (-3, 0, -0.2))
    cad = put(sc, 'cadet_stand', (-1.1, -0.35, 0.0), (0, 0, math.pi / 2 + 0.25))
    c = cam(sc, (-5.6, -2.3, 1.35), (0.0, 0.25, 2.0), lens=32, dof=4.7, fstop=2.8)
    mp = at(c, 0.25, 0.32, 600)
    put(sc, 'mars', tuple(mp), (0.4, 0.2, 0), 120); put(sc, 'marsAtm', tuple(mp), (0, 0, 0), 120.5)
    sunp = Vector((1.0, -0.3, 0.32))
    sun(sc, sunp, (1.0, 0.62, 0.36), 7.0, 0.02)
    lamp(sc, (-3.5, 2.5, 3.6), (0.25, 0.45, 1.0), 120, 2.0, 'AREA', target=(-1.1, -0.35, 1.0), name='fill')
    lamp(sc, (-0.6, -0.9, 1.7), (1.0, 0.55, 0.3), 6, 0.3, name='kick')
    haze_box(sc, (-3, 0, 2), (7.5, 9, 5), (0.75, 0.8, 0.9), 0.08, 0.65)

@shot
def c04_debris():
    sc = new_shot('c04_debris', world_space('w04', band=(0.9, 0.3, 0.4), core=(1.0, 0.45, 0.7), haze=(0.4, 0.2, 0.8), mw=3.0, stars=40))
    c = cam(sc, (0, 0, 0), (0, 1, 0), lens=24, dof=120, fstop=11)
    sunp = at(c, 0.5, 0.3, 3000)
    put(sc, 'sun', tuple(sunp), (0, 0, 0), 25)
    sun(sc, sunp, (1.0, 0.72, 0.5), 6.0, 0.02)
    ship = put(sc, 'ship', tuple(at(c, 0.05, -0.1, 120)))
    face(ship, Vector((-1, 0.35, 0.05)))
    attach_engines(sc, ship)
    lamp(sc, (-20, -10, 10), (0.5, 0.3, 1.0), 4e4, 20, 'AREA', target=tuple(ship.location), name='fill')
    rr = random.Random(4)
    for i in range(110):
        dpt = 6 + rr.random() ** 1.6 * 200
        p = at(c, rr.uniform(-1.3, 1.3), rr.uniform(-1.3, 1.3), dpt)
        put(sc, 'debris%d' % rr.randint(0, 5), tuple(p), (rr.uniform(0, 6), rr.uniform(0, 6), rr.uniform(0, 6)), dpt * rr.uniform(0.012, 0.04), name='c04_d%d' % i)
    move = Vector((0.6, -1.0, 0.15)).normalized()
    for i in range(50):
        dpt = 15 + rr.random() * 180
        p = at(c, rr.uniform(-1.2, 1.2), rr.uniform(-1.2, 1.2), dpt)
        o = put(sc, 'streak', tuple(p), (0, 0, 0), (dpt * rr.uniform(0.15, 0.4), dpt * 0.02, dpt * 0.02), name='c04_s%d' % i)
        face(o, move)

@shot
def c05_impact():
    sc = new_shot('c05_impact', world_space('w05', band=(0.3, 0.2, 0.9), mw=0.3, stars=40))
    put(sc, 'ship', (0, 0, 0), (0, 0, 0))
    hit = Vector((0.6, -1.75, 0.9))
    put(sc, 'debris2', tuple(hit + Vector((-0.2, -0.4, 0.3))), (0.4, 1.2, 0.3), 0.9)
    put(sc, 'fireball', tuple(hit + Vector((0, -0.4, 0))), (0, 0, 0), (1.5, 1.3, 1.2))
    put(sc, 'smokeball', tuple(hit + Vector((1.2, -1.6, 1.2))), (0.3, 0.2, 0), (2.4, 2.0, 1.8))
    lamp(sc, tuple(hit + Vector((0, -1.0, 0.3))), (1.0, 0.5, 0.15), 3e4, 1.5, name='blast')
    rr = random.Random(9)
    for i in range(40):
        d = Vector((rr.uniform(-1, 1), rr.uniform(-1, -0.1), rr.uniform(-0.3, 1))).normalized()
        o = put(sc, 'streak', tuple(hit + d * rr.uniform(0.8, 4.5)), (0, 0, 0), (rr.uniform(0.2, 0.7), 1, 1), name='c05_s%d' % i)
        face(o, d)
    for i in range(16):
        d = Vector((rr.uniform(-1, 1), rr.uniform(-1, -0.2), rr.uniform(-0.2, 1))).normalized()
        put(sc, 'debris%d' % rr.randint(0, 5), tuple(hit + d * rr.uniform(2, 7)), (rr.random() * 6, rr.random() * 6, 0), rr.uniform(0.1, 0.35), name='c05_c%d' % i)
    sun(sc, Vector((-0.6, 0.6, 0.5)), (0.6, 0.75, 1.0), 2.0, 0.02)
    cam(sc, (-5.0, -10.5, 0.2), tuple(hit + Vector((0.4, 0.6, 0.3))), lens=30, roll=0.15)

@shot
def c06_alarm():
    sc = new_shot('c06_alarm', world_space('w06', mw=0.0, stars=0.0))
    put(sc, 'corridor', (0, 0, 0))
    put(sc, 'cadet_run', (9.0, 0.3, 0.05), (0, 0, math.pi / 2 + 0.15))
    for i, x in enumerate([2, 7.2, 12.4, 17.6]):
        lamp(sc, (x, -1.6 if i % 2 else 1.6, 3.0), (1.0, 0.06, 0.03), 900, 0.15, name='alarm%d' % i)
    lamp(sc, (21, 0, 3.0), (1.0, 0.5, 0.15), 400, 0.5, name='evac')
    lamp(sc, (-3, 0, 1.6), (0.2, 0.35, 0.8), 120, 1.5, 'AREA', target=(8, 0, 1.2), name='back')
    lamp(sc, (14.5, -1.8, 0.6), (1.0, 0.5, 0.15), 600, 0.1, name='sparkL')
    haze_box(sc, (10, 0, 1.5), (24, 4.6, 3.6), (0.75, 0.7, 0.7), 0.055, 0.4)
    put(sc, 'smokeball', (15.5, 1.0, 2.6), (0, 0, 0), (2.0, 1.2, 0.8))
    rr = random.Random(5)
    for i in range(30):
        d = Vector((rr.uniform(-1, 1), rr.uniform(0.0, 1), rr.uniform(-1, 0.4))).normalized()
        o = put(sc, 'streak', tuple(Vector((14.5, -1.9, 2.3)) + d * rr.uniform(0.1, 1.4)), (0, 0, 0), (rr.uniform(0.15, 0.4), 1, 1), name='c06_s%d' % i)
        face(o, d)
    cam(sc, (0.3, -0.9, 1.25), (12, 0.35, 1.3), lens=24, dof=8.8, fstop=2.8)

@shot
def c07_launch():
    """The lifeboat tears away, the Kestrel burning behind it, Mars waiting below."""
    sc = new_shot('c07_launch', world_space('w07', band=(0.5, 0.3, 0.8), mw=0.5, stars=40))
    c = cam(sc, (0, 0, 0), (0, 1, 0), lens=30, dof=10, fstop=5.6)
    mp = at(c, -0.55, -1.55, 700)
    put(sc, 'mars', tuple(mp), (0.1, 0.3, 0), 330); put(sc, 'marsAtm', tuple(mp), (0, 0, 0), 331.4)
    sun(sc, mat_of(c).to_3x3() @ Vector((-0.7, 0.6, 0.5)), (1.0, 0.93, 0.85), 5.0, 0.01)
    ship = put(sc, 'ship', tuple(at(c, 0.42, 0.25, 55)))
    face(ship, Vector((1, 0.6, 0.25))); roll(ship, 0.4)
    for i, lp in enumerate([(1.4, -1.7, 0.6), (-2.6, 1.6, 1.0), (3.5, 0.5, 1.5), (-0.6, -1.8, -0.8)]):
        f = attach(sc, ship, 'fireball', lp, (i, 0, 0), 1.3, name='c07_f%d' % i)
        attach(sc, ship, 'smokeball', (lp[0] - 1.5, lp[1] * 1.5, lp[2] + 1.0), (i, 0, 0), 2.6, name='c07_sm%d' % i)
        lamp(sc, tuple(f.location), (1.0, 0.45, 0.12), 2.5e4, 1.0, name='fire%d' % i)
    lb = put(sc, 'lifeboat', tuple(at(c, -0.22, -0.02, 10)))
    face(lb, Vector((-1.0, -0.25, -0.2))); roll(lb, -0.2)
    attach(sc, lb, 'flame', (-4.1, 0, 0), (0, 0, 0), (1.2, 0.75, 0.75))
    lamp(sc, tuple(mat_of(lb) @ Vector((-3.0, 0, 0))), (1.0, 0.6, 0.3), 3000, 0.5, name='thrust')
    lamp(sc, (-6, -3, 3), (0.35, 0.5, 0.9), 400, 6, 'AREA', target=tuple(lb.location), name='fill')

@shot
def c08_entry():
    """Entry: a streak of fire over the curve of Mars."""
    sc = new_shot('c08_entry', world_space('w08', band=(0.2, 0.3, 0.9), mw=0.6, stars=40))
    c = cam(sc, (0, 0, 0), (0, 1, -0.3), lens=30, roll=0.1)
    R0 = 3000.0
    mp = Vector((0, 700, -160 - R0))
    put(sc, 'mars', tuple(mp), (0.4, 0.1, 0), R0); put(sc, 'marsAtm', tuple(mp), (0, 0, 0), R0 * 1.004)
    sunp = at(c, 1.6, 0.35, 8000)
    sun(sc, sunp, (1.0, 0.8, 0.62), 1.3, 0.01)
    lb = put(sc, 'lifeboat', tuple(at(c, 0.08, 0.05, 13)))
    face(lb, Vector((1.0, -0.1, -0.6)))
    attach(sc, lb, 'plasma', (0.2, 0, 0), (0, 0, 0), 1.0)
    lamp(sc, tuple(mat_of(lb) @ Vector((2.6, 0, 0))), (1.0, 0.45, 0.18), 9000, 1.0, name='glow')

@shot
def c09_arrive():
    """Like a frontier painting: the cadet on a rock, the old base below, rock fins and a low sun in the dust."""
    sdir = Vector((0.15, 1.0, 0.1))
    sc = new_shot('c09_arrive', world_sky('w09', (1.0, 0.64, 0.34), (0.42, 0.26, 0.19), sdir, (1.0, 0.82, 0.58), 3.0, 6.0, 1.1))
    mars_set(sc)
    perch = put(sc, 'rock2', (-30, -52, ground_h(-30, -52) + 0.5), (0.1, 0.05, 0.3), (5.0, 4.0, 2.6), name='c09_perch')
    put(sc, 'rock4', (-25.5, -50, ground_h(-25.5, -50)), (0.4, 0.2, 1.0), (3, 3, 2.0), name='c09_perch2')
    put(sc, 'rock0', (-35, -50, ground_h(-35, -50)), (0.4, 0.2, 2.0), (3.5, 3, 2.2), name='c09_perch3')
    z = surface_z(perch, -29.6, -51.2) or ground_h(-30, -52) + 2.5
    put(sc, 'cadet_stand', (-29.6, -51.2, z - 0.05), (0, 0, math.pi + 0.3), 1.0)
    scatter_rocks(sc, (-10, -20), 70, 90, 0.4, 2.2, 3, avoid=((0, 0), 30))
    sun(sc, sdir, (1.0, 0.74, 0.47), 4.5, 0.02)
    haze_box(sc, (0, 60, 40), (440, 360, 110), (1.0, 0.72, 0.48), 0.0028, 0.75)
    cam(sc, (-32.6, -56.5, z + 1.3), (-8, 20, z + 2), lens=30, dof=5.6, fstop=4)

@shot
def c10_rescue():
    """The rescue ship drops through the dust on beams of light; the cadet waves."""
    sdir = Vector((-0.6, 1.0, 0.14))
    sc = new_shot('c10_rescue', world_sky('w10', (1.0, 0.56, 0.28), (0.28, 0.15, 0.14), sdir, (1.0, 0.76, 0.46), 3.0, 5.0, 1.0))
    mars_set(sc)
    c = cam(sc, (-8, -42, ground_h(-8, -42) + 1.1), (4, 20, 20), lens=24, dof=3.6, fstop=4)
    p = at(c, 0.42, -0.55, 3.6)
    put(sc, 'cadet_wave', (p.x, p.y, ground_h(p.x, p.y)), (0, 0, math.pi - 0.35))
    sp = at(c, -0.12, 0.35, 70)
    ship = put(sc, 'lifeboat', tuple(sp), (0, 0, 0), 4.0, name='c10_rescueShip')
    face(ship, Vector((-1, 0.2, -0.25)))
    attach(sc, ship, 'flameBlue', (-3.9, 0, 0), (0, 0, 0), (1.2, 0.8, 0.8))
    for i, x in enumerate((-1.0, 1.0)):
        b = attach(sc, ship, 'beam', (x, 0, -5.5), (0, 0, 0), (1.3, 1.3, 1.2), name='c10_beam%d' % i)
        lamp(sc, tuple(mat_of(ship) @ Vector((x, 0, -1.0))), (1.0, 0.95, 0.85), 3e5, 1.0, 'SPOT', target=(sp.x, sp.y, 0), spot=0.45, name='land%d' % i)
    tr = put(sc, 'trail', tuple(sp + Vector((40, 10, 18))), (0, 0, 0), (2.0, 1.0, 1.0))
    face(tr, Vector((1, 0.25, 0.45)))
    scatter_rocks(sc, (-10, -20), 50, 60, 0.4, 1.8, 7, avoid=((0, 0), 30))
    sun(sc, sdir, (1.0, 0.7, 0.42), 4.0, 0.02)
    haze_box(sc, (0, 40, 40), (440, 360, 110), (1.0, 0.68, 0.45), 0.0028, 0.6)

def _cliff_cadet(sc, pose='cadet_sit'):
    cl = put(sc, 'cliff', (-30, -110, ground_h(-30, -110) + 2), (0, 0, math.pi + 0.1), 1.0)
    x, y = -30.0, -102.0
    z = surface_z(cl, x, y) or ground_h(-30, -110) + 10
    look = Vector((0, 8)) - Vector((x, y)); ang = math.atan2(look.y, look.x)
    cad = put(sc, pose, (x, y, z - 0.08), (0, 0, ang + math.pi / 2))
    return cl, Vector((x, y, z))

def behind(top, target, back=4.0, side=1.3, up=1.4):
    """Camera spot behind and to the right of a figure at top, looking toward target."""
    d = (Vector(target) - top); d.z = 0; d.normalize()
    right = Vector((d.y, -d.x, 0))
    return tuple(top - d * back + right * side + Vector((0, 0, up)))

@shot
def c11_home():
    """Home: the cadet on a ridge above the valley, golden light, the rescue ship rising."""
    sdir = Vector((0.12, 1.0, 0.12))
    sc = new_shot('c11_home', world_sky('w11', (1.0, 0.7, 0.4), (0.52, 0.33, 0.22), sdir, (1.0, 0.86, 0.62), 3.5, 4.0, 1.2))
    mars_set(sc)
    cl, top = _cliff_cadet(sc)
    c = cam(sc, behind(top, (0, 8, 0), 4.2, 1.5, 1.5), (0, 8, -2), lens=30, dof=4.6, fstop=4)
    sp = at(c, 0.35, 0.42, 160)
    ship = put(sc, 'lifeboat', tuple(sp), (0, 0, 0), 3.0, name='c11_ship')
    face(ship, Vector((0.4, 0.3, 1.0)))
    attach(sc, ship, 'flame', (-4.0, 0, 0), (0, 0, 0), (2.0, 1.0, 1.0))
    tr = attach(sc, ship, 'trail', (-30, 0, 0), (0, 0, 0), (1.4, 1.6, 1.6))
    sun(sc, sdir, (1.0, 0.78, 0.5), 4.5, 0.02)
    haze_box(sc, (0, 30, 35), (440, 380, 100), (1.0, 0.74, 0.52), 0.0016, 0.75)

@shot
def c12_dusk():
    sdir = Vector((0.7, 1.0, 0.015))
    # Mars sunsets are blue near the sun: fine dust scatters blue light forward
    sc = new_shot('c12_dusk', world_sky('w12', (0.18, 0.2, 0.28), (0.03, 0.035, 0.06), sdir, (0.35, 0.6, 1.0), 2.5, 10.0, 0.9, stars=30.0))
    mars_set(sc)
    sun(sc, sdir, (0.5, 0.65, 1.0), 0.5, 0.05)
    lamp(sc, (8, -16, 3), (1.0, 0.6, 0.3), 5000, 1.0, name='door')
    lamp(sc, (-4.4, -4.6, 17.6), (1.0, 0.08, 0.04), 3000, 0.3, name='beacon')
    haze_box(sc, (0, 0, 15), (300, 300, 40), (0.6, 0.65, 0.8), 0.008, 0.5)
    scatter_rocks(sc, (-10, -40), 50, 60, 0.4, 2.0, 11, avoid=((0, 0), 30))
    cam(sc, (-46, -60, 4.5), (8, 20, 9), lens=34)

@shot
def c13_lone():
    """Alone at night: the cadet under the stars, Earth a bright blue dot, the base lights below."""
    sdir = Vector((0.2, 1.0, -0.2))
    sc = new_shot('c13_lone', world_sky('w13', (0.07, 0.09, 0.16), (0.004, 0.006, 0.016), sdir, (0.2, 0.3, 0.6), 0.6, 5.0, 1.0, stars=90.0))
    mars_set(sc)
    cl, top = _cliff_cadet(sc)
    cp = Vector(behind(top, (0, 8, 0), 3.4, 1.5, 0.7))
    gz = surface_z(cl, cp.x, cp.y)
    if gz is not None: cp.z = max(cp.z, gz + 0.9)
    c = cam(sc, tuple(cp), (0, 8, 16), lens=24, dof=3.9, fstop=4)
    put(sc, 'earth', tuple(at(c, 0.3, 0.55, 3000)), (0, 0, 0), 4.0)
    sun(sc, Vector((0.6, 0.5, 0.7)), (0.45, 0.6, 1.0), 0.3, 0.04, name='moon')
    lamp(sc, tuple(top + Vector((0.3, 0.2, 1.4))), (0.6, 0.85, 1.0), 30, 0.05, 'SPOT', target=tuple(top + Vector((2, 12, -2))), spot=0.6, name='headlamp')
    lamp(sc, (0, -14, 4), (1.0, 0.6, 0.3), 3000, 1.0, name='baseglow')
    lamp(sc, tuple(top + Vector((-1.5, 4.0, 2.5))), (0.45, 0.65, 1.0), 120, 1.5, 'AREA', target=tuple(top + Vector((0, 0, 0.8))), name='rim')
    haze_box(sc, (0, -20, 15), (300, 260, 40), (0.6, 0.7, 0.9), 0.004, 0.6)

# ------------------------------------------------------------------ build and render

def build_all(names=None):
    build_lib()
    for n, f in SHOTS.items():
        if names is None or n in names: f()
    bpy.ops.wm.save_as_mainfile(filepath=ROOT + r"\Art\Blender\cinematics.blend")

def setup_gpu():
    p = bpy.context.preferences.addons['cycles'].preferences
    for t in ('HIP', 'OPTIX', 'CUDA', 'METAL', 'ONEAPI'):
        try: p.compute_device_type = t; break
        except TypeError: continue
    p.refresh_devices()
    for d in p.devices: d.use = d.type != 'CPU'

def render_shot(name, scale=100, samples=None, path=None):
    setup_gpu()
    sc = bpy.data.scenes[name]
    sc.render.resolution_percentage = scale
    if samples: sc.cycles.samples = samples
    sc.render.filepath = path or (RAW + "\\" + name + ".png")
    bpy.ops.render.render(write_still=True, scene=name)
    return sc.render.filepath

def render_all(scale=100, samples=None, names=None, skip_done=False):
    import os
    for n in SHOTS:
        if skip_done and os.path.exists(RAW + "\\" + n + ".png"): continue
        if n in bpy.data.scenes and (names is None or n in names): render_shot(n, scale, samples)
