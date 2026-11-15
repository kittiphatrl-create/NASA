# Procedural surface detail for the LAST DOCK cutscenes (no image files): metal panels with seams, grime and
# per-plate colour, Mars and Moon ground with pebbles and wind ripples, layered and cracked rock, solar cells,
# wood grain and woven fabric. pixel_cine.py calls texture_pass() after the assets are built; it rewrites the
# node trees of the existing materials in place, so every object keeps its material.
import bpy

def _nt(m):
    m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    return nt

def _n(nt, t, **kw):
    n = nt.nodes.new(t)
    for k, v in kw.items():
        if k in n.inputs: _set(nt, n.inputs[k], v)
        else: setattr(n, k, v)
    return n

def _set(nt, inp, v):
    if hasattr(v, 'is_output'): nt.links.new(v, inp)
    elif isinstance(v, (tuple, list)) and len(v) == 3 and inp.type == 'RGBA': inp.default_value = (*v, 1)
    else: inp.default_value = v

def _mix(nt, fac, a, b, blend='MIX'):
    n = nt.nodes.new('ShaderNodeMix'); n.data_type = 'RGBA'; n.blend_type = blend; n.clamp_result = True
    _set(nt, n.inputs[0], fac); _set(nt, n.inputs[6], a); _set(nt, n.inputs[7], b)
    return n.outputs[2]

def _math(nt, op, a, b=0.0, clamp=False):
    n = nt.nodes.new('ShaderNodeMath'); n.operation = op; n.use_clamp = clamp
    _set(nt, n.inputs[0], a); _set(nt, n.inputs[1], b)
    return n.outputs[0]

def _range(nt, v, f0, f1, t0=0.0, t1=1.0):
    n = nt.nodes.new('ShaderNodeMapRange'); n.clamp = True
    _set(nt, n.inputs['Value'], v)
    n.inputs['From Min'].default_value = f0; n.inputs['From Max'].default_value = f1
    n.inputs['To Min'].default_value = t0; n.inputs['To Max'].default_value = t1
    return n.outputs['Result']

def _coords(nt, stretch=None):
    tc = nt.nodes.new('ShaderNodeTexCoord')
    v = tc.outputs['Object']
    if stretch:
        mp = nt.nodes.new('ShaderNodeMapping'); mp.inputs['Scale'].default_value = stretch
        nt.links.new(v, mp.inputs['Vector']); v = mp.outputs['Vector']
    return v

def _noise(nt, v, scale, detail=6.0, rough=0.6):
    return _n(nt, 'ShaderNodeTexNoise', Vector=v, Scale=scale, Detail=detail, Roughness=rough)

def _vor(nt, v, scale, feature='F1', dist='EUCLIDEAN', rnd=1.0):
    n = nt.nodes.new('ShaderNodeTexVoronoi'); n.feature = feature; n.distance = dist
    _set(nt, n.inputs['Vector'], v); n.inputs['Scale'].default_value = scale; n.inputs['Randomness'].default_value = rnd
    return n

def _finish(nt, col, rough, metal=0.0, height=None, bump=0.4, dist=0.05, coat=0.0, emit=None, emit_str=None):
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    bs = nt.nodes.new('ShaderNodeBsdfPrincipled')
    _set(nt, bs.inputs['Base Color'], col); _set(nt, bs.inputs['Roughness'], rough)
    bs.inputs['Metallic'].default_value = metal; bs.inputs['Coat Weight'].default_value = coat
    if height is not None:
        b = _n(nt, 'ShaderNodeBump', Strength=bump, Distance=dist, Height=height)
        nt.links.new(b.outputs['Normal'], bs.inputs['Normal'])
    if emit is not None:
        _set(nt, bs.inputs['Emission Color'], emit); _set(nt, bs.inputs['Emission Strength'], emit_str)
    nt.links.new(bs.outputs['BSDF'], out.inputs['Surface'])
    return bs

# ------------------------------------------------------------------ recipes

def panels(m, base, dark, scale=2.0, seam=0.025, grime=0.5, rough=0.45, metal=0.3, tint=0.25, bump=0.5,
           dust=None, dust_z=(0.0, 3.0), coat=0.0, lights=None, plate=(1.0, 1.0, 1.6)):
    """Hull plating: rectangular plates with dark seams, each plate a slightly different shade, streaky grime."""
    nt = _nt(m)
    v = _coords(nt)
    pv = _coords(nt, plate)   # stretched so plates are long rectangles on a regular grid
    edge = _vor(nt, pv, scale, 'DISTANCE_TO_EDGE', 'CHEBYCHEV', 0.0).outputs['Distance']
    cell = _vor(nt, pv, scale, 'F1', 'CHEBYCHEV', 0.0)
    seam_m = _range(nt, edge, 0.0, seam, 1.0, 0.0)
    col = _mix(nt, tint, base, cell.outputs['Color'], 'SOFT_LIGHT')
    streak = _noise(nt, _coords(nt, (1.0, 1.0, 0.25)), scale * 1.6, 8.0, 0.65)
    g = _range(nt, streak.outputs['Fac'], 0.42, 0.78, 0.0, grime)
    col = _mix(nt, g, col, dark)
    col = _mix(nt, _math(nt, 'MULTIPLY', seam_m, 0.85), col, dark)
    if dust:
        sep = nt.nodes.new('ShaderNodeSeparateXYZ'); nt.links.new(v, sep.inputs['Vector'])
        dn = _noise(nt, v, scale * 0.8, 4.0)
        dz = _range(nt, sep.outputs['Z'], dust_z[0], dust_z[1], 1.0, 0.0)
        dm = _math(nt, 'MULTIPLY', dz, _range(nt, dn.outputs['Fac'], 0.3, 0.7, 0.5, 1.0))
        col = _mix(nt, dm, col, dust)
    r = _range(nt, g, 0.0, max(grime, 0.01), rough, min(1.0, rough + 0.35))
    fine = _noise(nt, v, scale * 30, 4.0)
    h = _math(nt, 'ADD', _math(nt, 'MULTIPLY', seam_m, -1.0), _math(nt, 'MULTIPLY', fine.outputs['Fac'], 0.08))
    emit = emit_s = None
    if lights:   # a few plates carry tiny status lights
        pick = _range(nt, _n(nt, 'ShaderNodeSeparateColor', Color=cell.outputs['Color']).outputs[0], 0.965, 0.975, 0.0, 1.0)
        dot = _range(nt, _vor(nt, v, scale * 9, 'F1').outputs['Distance'], 0.08, 0.16, 1.0, 0.0)
        emit_s = _math(nt, 'MULTIPLY', _math(nt, 'MULTIPLY', pick, dot), 6.0)
        emit = lights
    _finish(nt, col, r, metal, h, bump, 0.03, coat, emit, emit_s)

def solar(m):
    """Solar array: deep blue cells in a pale grid, glassy."""
    nt = _nt(m)
    v = _coords(nt)
    edge = _vor(nt, v, 3.5, 'DISTANCE_TO_EDGE', 'CHEBYCHEV', 0.0).outputs['Distance']
    grid = _range(nt, edge, 0.0, 0.04, 1.0, 0.0)
    shade = _noise(nt, v, 1.5, 3.0)
    cellc = _mix(nt, _range(nt, shade.outputs['Fac'], 0.3, 0.7), (0.02, 0.05, 0.16), (0.05, 0.11, 0.3))
    col = _mix(nt, grid, cellc, (0.7, 0.72, 0.75))
    _finish(nt, col, _range(nt, grid, 0.0, 1.0, 0.12, 0.5), 0.4, _math(nt, 'MULTIPLY', grid, -1.0), 0.3, 0.02, 0.6)

def ground(m, c1, c2, c3, pebble=(0.25, 0.12, 0.06), scale=1.0, rough=0.95):
    """Regolith: broad colour drifts, finer mottling, scattered pebbles, faint wind ripples."""
    nt = _nt(m)
    v = _coords(nt)
    n1 = _noise(nt, v, 0.04 * scale, 4.0, 0.55)
    n2 = _noise(nt, v, 0.5 * scale, 8.0, 0.65)
    col = _mix(nt, _range(nt, n1.outputs['Fac'], 0.35, 0.65), c1, c2)
    col = _mix(nt, _range(nt, n2.outputs['Fac'], 0.45, 0.7, 0.0, 0.55), col, c3)
    peb = _vor(nt, v, 2.2 * scale, 'F1')
    pm = _range(nt, peb.outputs['Distance'], 0.08, 0.2, 1.0, 0.0)
    pm = _math(nt, 'MULTIPLY', pm, _range(nt, _n(nt, 'ShaderNodeSeparateColor', Color=peb.outputs['Color']).outputs[0], 0.55, 0.6))
    col = _mix(nt, pm, col, pebble)
    wv = _n(nt, 'ShaderNodeTexWave', Vector=v, Scale=0.9 * scale, Distortion=6.0, Detail=3.0); wv.bands_direction = 'Y'
    h = _math(nt, 'ADD', _math(nt, 'MULTIPLY', n2.outputs['Fac'], 0.6), _math(nt, 'MULTIPLY', pm, 0.7))
    h = _math(nt, 'ADD', h, _math(nt, 'MULTIPLY', wv.outputs['Fac'], 0.25))
    _finish(nt, col, rough, 0.0, h, 0.5, 0.15)

def rock(m, c1, c2, crack=(0.12, 0.05, 0.03), scale=1.0):
    """Layered rock: colour bands, mottling, dark cracks, rough relief."""
    nt = _nt(m)
    v = _coords(nt)
    wv = _n(nt, 'ShaderNodeTexWave', Vector=v, Scale=0.9 * scale, Distortion=7.0, Detail=5.0); wv.bands_direction = 'Z'
    n = _noise(nt, v, 1.5 * scale, 10.0, 0.62)
    col = _mix(nt, _range(nt, n.outputs['Fac'], 0.35, 0.7), c1, c2)
    col = _mix(nt, _range(nt, wv.outputs['Fac'], 0.3, 1.0, 0.0, 0.45), col, c2, 'MULTIPLY')
    cr = _range(nt, _vor(nt, v, 1.4 * scale, 'DISTANCE_TO_EDGE').outputs['Distance'], 0.0, 0.03, 1.0, 0.0)
    col = _mix(nt, _math(nt, 'MULTIPLY', cr, 0.8), col, crack)
    h = _math(nt, 'ADD', n.outputs['Fac'], _math(nt, 'MULTIPLY', cr, -0.6))
    h = _math(nt, 'ADD', h, _math(nt, 'MULTIPLY', wv.outputs['Fac'], 0.3))
    _finish(nt, col, 0.9, 0.0, h, 0.7, 0.2)

def wood(m, c1=(0.45, 0.27, 0.14), c2=(0.27, 0.15, 0.07)):
    nt = _nt(m)
    v = _coords(nt, (1.0, 6.0, 1.0))
    wv = _n(nt, 'ShaderNodeTexWave', Vector=v, Scale=3.0, Distortion=8.0, Detail=4.0); wv.wave_type = 'RINGS'
    col = _mix(nt, _range(nt, wv.outputs['Fac'], 0.2, 0.9), c1, c2)
    _finish(nt, col, 0.55, 0.0, wv.outputs['Fac'], 0.15, 0.01)

def fabric(m, c1, c2):
    """Woven cloth: fine checker weave and soft colour drift."""
    nt = _nt(m)
    v = _coords(nt)
    ck = _n(nt, 'ShaderNodeTexChecker', Vector=v, Scale=70.0)
    n = _noise(nt, v, 3.0, 4.0)
    col = _mix(nt, _range(nt, n.outputs['Fac'], 0.35, 0.7), c1, c2)
    col = _mix(nt, _math(nt, 'MULTIPLY', ck.outputs['Fac'], 0.12), col, (0.0, 0.0, 0.0))
    _finish(nt, col, 0.85, 0.0, ck.outputs['Fac'], 0.12, 0.005)

def texture_pass(M):
    """Rewrite the shared cutscene materials with the recipes above. M is the material dict from cinematics_build."""
    mats = bpy.data.materials
    def get(name): return M.get(name) or mats.get(name)
    todo = [
        ('hull', lambda m: panels(m, (0.68, 0.66, 0.62), (0.22, 0.21, 0.2), 2.4, 0.03, 0.55, 0.42, 0.35, lights=(0.3, 0.9, 1.0))),
        ('hullDark', lambda m: panels(m, (0.2, 0.21, 0.23), (0.07, 0.07, 0.08), 3.0, 0.03, 0.4, 0.5, 0.5)),
        ('panel', solar),
        ('orange', lambda m: panels(m, (0.9, 0.42, 0.08), (0.4, 0.15, 0.03), 2.0, 0.03, 0.3, 0.4, 0.1)),
        ('wall', lambda m: panels(m, (0.3, 0.33, 0.38), (0.1, 0.11, 0.13), 1.6, 0.03, 0.6, 0.5, 0.45, lights=(0.3, 0.85, 1.0))),
        ('crate', lambda m: panels(m, (0.36, 0.38, 0.32), (0.14, 0.15, 0.12), 3.0, 0.04, 0.5, 0.6, 0.2)),
        ('base', lambda m: panels(m, (0.86, 0.84, 0.79), (0.42, 0.38, 0.33), 0.55, 0.02, 0.35, 0.5, 0.15,
                                  dust=(0.62, 0.34, 0.18), dust_z=(-0.5, 3.5), lights=(1.0, 0.7, 0.35))),
        ('ground', lambda m: ground(m, (0.58, 0.3, 0.15), (0.44, 0.21, 0.1), (0.7, 0.4, 0.22))),
        ('moonGround', lambda m: ground(m, (0.5, 0.49, 0.47), (0.36, 0.36, 0.35), (0.62, 0.61, 0.59), (0.2, 0.2, 0.2))),
        ('rock', lambda m: rock(m, (0.55, 0.28, 0.14), (0.32, 0.14, 0.07))),
        ('rockDark', lambda m: rock(m, (0.3, 0.15, 0.08), (0.14, 0.07, 0.04), (0.05, 0.03, 0.02), 1.5)),
        ('cabWall', lambda m: panels(m, (0.24, 0.27, 0.36), (0.1, 0.11, 0.16), 2.2, 0.03, 0.4, 0.55, 0.2)),
        ('cabTrim', lambda m: panels(m, (0.5, 0.42, 0.33), (0.25, 0.2, 0.15), 3.0, 0.03, 0.3, 0.5, 0.3)),
        ('wood', wood),
        ('blanket', lambda m: fabric(m, (0.16, 0.42, 0.22), (0.09, 0.28, 0.14))),
        ('sheet', lambda m: fabric(m, (0.84, 0.86, 0.88), (0.7, 0.72, 0.76))),
        ('sofaMat', lambda m: fabric(m, (0.26, 0.37, 0.58), (0.17, 0.25, 0.42))),
        ('podShell', lambda m: panels(m, (1.0, 0.5, 0.1), (0.62, 0.22, 0.03), 0.9, 0.008, 0.1, 0.28, 0.0, tint=0.06, bump=0.15, coat=0.5)),
        ('rescueShell', lambda m: panels(m, (0.88, 0.91, 0.96), (0.35, 0.45, 0.62), 0.9, 0.008, 0.12, 0.28, 0.1, tint=0.06, bump=0.15, coat=0.5)),
    ]
    done = []
    for name, fn in todo:
        m = get(name)
        if m is not None:
            fn(m); done.append(name)
    return done
