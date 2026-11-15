# LAST DOCK main menu backdrop: the Kestrel in orbit, a galaxy behind, Mars large on the right.
# Rendered as a seamless 16 s loop (1920x1080, 24 fps) that the menu plays behind its UI.
# The camera slowly swings round the ship and zooms in on it, then eases back out; every motion is
# periodic over the loop, so the last frame flows into the first.
# Run:  blender -b --python Art/Blender/menu_space.py -- [still FRAME | video]
# Output: renders/menu/menu_space.mp4 and menu_space.jpg (the first frame, shown until the video starts).
import bpy, math, sys, os
from mathutils import Vector

BL = r"F:\NASA\TitanGate2177\Art\Blender"
OUT = BL + r"\renders\menu"
exec(open(BL + r"\pixel_cine.py", encoding='utf-8').read())   # asset kit, cadet loader, helpers (does not build or render)

LOOP = 384          # output frames: 16 s at 24 fps
TAU = math.tau
AZ0 = 215          # camera azimuth: behind and to the left of the ship, so its nose points away toward Mars

def wave(f, cycles=1.0, phase=0.0):
    return math.sin(TAU * (cycles * (f - 1) / LOOP) + phase)

def galaxy(name, band_n, core_dir):
    """A Milky-Way style sky: fine star field, a narrow dusty band with a warm core, dark dust lanes,
    pink nebula patches, and more stars inside the band."""
    w = bpy.data.worlds.new(name); w.use_nodes = True
    nt = w.node_tree; nt.nodes.clear(); N_ = lambda t, **k: cb.node(nt, t, (0, 0), **k)
    def math_(op, a, b=None, clamp=False):
        m = N_('ShaderNodeMath'); m.operation = op; m.use_clamp = clamp
        for i, x in enumerate((a, b)):
            if x is None: continue
            if isinstance(x, (int, float)): m.inputs[i].default_value = x
            else: L(nt, x, m.inputs[i])
        return m.outputs[0]
    def mrange(x, a, b, c=0.0, d=1.0):
        m = N_('ShaderNodeMapRange'); L(nt, x, m.inputs['Value'])
        m.inputs['From Min'].default_value, m.inputs['From Max'].default_value = a, b
        m.inputs['To Min'].default_value, m.inputs['To Max'].default_value = c, d
        return m.outputs['Result']
    def noise(sc_, det, rough, vec):
        n = N_('ShaderNodeTexNoise'); n.inputs['Scale'].default_value = sc_; n.inputs['Detail'].default_value = det
        n.inputs['Roughness'].default_value = rough; L(nt, vec, n.inputs['Vector']); return n
    def mix(fac, a, b):
        m = N_('ShaderNodeMix'); m.data_type = 'RGBA'
        if isinstance(fac, (int, float)): m.inputs[0].default_value = fac
        else: L(nt, fac, m.inputs[0])
        for slot, c in ((6, a), (7, b)):
            if isinstance(c, tuple): m.inputs[slot].default_value = (*c, 1)
            else: L(nt, c, m.inputs[slot])
        return m.outputs[2]
    out = N_('ShaderNodeOutputWorld')
    d = N_('ShaderNodeTexCoord').outputs['Generated']
    dn = N_('ShaderNodeVectorMath'); dn.operation = 'NORMALIZE'; L(nt, d, dn.inputs[0]); d = dn.outputs['Vector']
    dot = N_('ShaderNodeVectorMath'); dot.operation = 'DOT_PRODUCT'; dot.inputs[1].default_value = Vector(band_n).normalized(); L(nt, d, dot.inputs[0])
    # warp the band edge with noise so it is never a clean stripe
    wob = noise(2.2, 6, 0.6, d)
    bdist = math_('ADD', math_('ABSOLUTE', dot.outputs['Value']), math_('MULTIPLY', math_('SUBTRACT', wob.outputs['Fac'], 0.5), 0.12))
    band = math_('POWER', mrange(bdist, 0.0, 0.17, 1.0, 0.0), 2.2)            # 1 on the band's spine, 0 away from it
    cdot = N_('ShaderNodeVectorMath'); cdot.operation = 'DOT_PRODUCT'; cdot.inputs[1].default_value = Vector(core_dir).normalized(); L(nt, d, cdot.inputs[0])
    core = math_('POWER', mrange(cdot.outputs['Value'], 0.93, 1.0, 0.0, 1.0), 2.5)   # bright bulge round the core
    clouds = noise(4.0, 14, 0.7, d)
    fine = noise(16.0, 10, 0.65, d)
    glow = math_('MULTIPLY', math_('MULTIPLY', band, mrange(clouds.outputs['Fac'], 0.35, 0.75, 0.25, 1.0)), mrange(fine.outputs['Fac'], 0.3, 0.7, 0.6, 1.15))
    glow = math_('ADD', glow, math_('MULTIPLY', core, math_('MULTIPLY', band, 1.4)))
    # dark dust lanes along the spine
    lanes = noise(9.0, 12, 0.72, d)
    lane = mrange(lanes.outputs['Fac'], 0.48, 0.58, 0.08, 1.0)
    lane = math_('ADD', lane, mrange(band, 0.35, 0.95, 1.0, 0.0), clamp=True)
    glow = math_('MULTIPLY', glow, lane)
    pinkp = noise(6.5, 8, 0.6, d)
    col = mix(mrange(clouds.outputs['Fac'], 0.4, 0.7), (0.22, 0.26, 0.85), (0.62, 0.42, 0.95))
    col = mix(math_('MULTIPLY', mrange(pinkp.outputs['Fac'], 0.6, 0.72), band), col, (1.0, 0.35, 0.55))
    col = mix(math_('MINIMUM', math_('MULTIPLY', core, 1.6), 1.0), col, (1.0, 0.78, 0.55))
    gem = N_('ShaderNodeEmission'); L(nt, col, gem.inputs['Color']); L(nt, math_('MULTIPLY', glow, 0.42), gem.inputs['Strength'])
    # stars: a dense faint field plus fewer bright ones; denser inside the band
    def stars(scale, cut, power, gain):
        v = N_('ShaderNodeTexVoronoi'); v.inputs['Scale'].default_value = scale; L(nt, d, v.inputs['Vector'])
        p = math_('POWER', mrange(v.outputs['Distance'], 0.0, cut, 1.0, 0.0), power)
        return math_('MULTIPLY', p, gain), v
    s1, v1 = stars(1100, 0.12, 16, 40)
    s2, v2 = stars(260, 0.07, 12, 140)
    sdens = math_('ADD', 0.35, math_('MULTIPLY', band, 2.2))
    stot = math_('MULTIPLY', math_('ADD', s1, s2), sdens)
    scol = mix(v2.outputs['Color'], (0.75, 0.82, 1.0), (1.0, 0.86, 0.7))
    sem = N_('ShaderNodeEmission'); L(nt, scol, sem.inputs['Color']); L(nt, stot, sem.inputs['Strength'])
    ad = N_('ShaderNodeAddShader'); L(nt, gem.outputs['Emission'], ad.inputs[0]); L(nt, sem.outputs['Emission'], ad.inputs[1])
    L(nt, ad.outputs['Shader'], out.inputs['Surface'])
    return w

def menu_space():
    sc = pshot('menu_space', None, LOOP // 2)
    sc.render.resolution_x, sc.render.resolution_y = 1920, 1080
    sc.render.fps = 24; sc.frame_start = 1; sc.frame_end = LOOP
    sc.render.frame_map_old = 100; sc.render.frame_map_new = 100
    sc.render.ffmpeg.constant_rate_factor = 'MEDIUM'; sc.render.ffmpeg.gopsize = 24
    sc.view_settings.exposure = 0.45

    # the ship at the origin, nose +X, rocking very gently
    sh = cb.put(sc, 'ship', (0, 0, 0))
    cb.attach_engines(sc, sh)
    flames = [o for o in sc.collection.all_objects if '_fl' in o.name]
    eng = next(o for o in sc.collection.all_objects if o.name.endswith('eng'))
    for o in flames + [eng]:
        mw = o.matrix_world.copy(); o.parent = sh; o.matrix_parent_inverse = sh.matrix_world.inverted(); o.matrix_world = mw
    # navigation lights: red on the port wing tip, green on starboard, a white strobe on the spine
    nav = []
    for nm, loc, col in (('navR', (-1.0, 9.9, 0.1), (1.0, 0.08, 0.05)), ('navG', (-1.0, -9.9, 0.1), (0.1, 1.0, 0.25)), ('strobe', (3.6, 0, 1.9), (1.0, 0.95, 0.9))):
        m = cb.emit_mat('m_' + nm, col, 0.0)
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.16, location=loc); o = bpy.context.active_object
        for c in o.users_collection: c.objects.unlink(o)
        sc.collection.objects.link(o); o.name = 'menu_' + nm; o.data.materials.append(m); o.parent = sh
        lp = cb.lamp(sc, loc, col, 0, 0.1, name=nm); lp.parent = sh
        nav.append((m, lp, nm))
    em_of = lambda m: next(n for n in m.node_tree.nodes if n.type == 'EMISSION')
    for f in range(1, LOOP + 1):
        for m, lp, nm in nav:
            if nm == 'strobe': on = (f - 1) % 96 in (0, 1, 2, 7, 8, 9)       # double flash every 4 s
            else: on = ((f - 1) % 48) < 20                                    # slow blink, 2 s
            em_of(m).inputs['Strength'].default_value = (60 if nm == 'strobe' else 30) if on else 0.5
            em_of(m).inputs['Strength'].keyframe_insert('default_value', frame=f)
            lp.data.energy = (900 if nm == 'strobe' else 300) if on else 0
            lp.data.keyframe_insert('energy', frame=f)
        sh.rotation_euler = (math.radians(3.0) * wave(f, 1, 0.4), math.radians(1.2) * wave(f, 2), math.radians(1.5) * wave(f, 1, 1.3))
        sh.location = (0, 0, 0.35 * wave(f, 2, 0.7))
        sh.keyframe_insert('rotation_euler', frame=f); sh.keyframe_insert('location', frame=f)
        for i, o in enumerate(flames):   # engine plumes flicker
            k = 1.0 + 0.12 * math.sin(f * 1.7 + i * 2.1) + 0.06 * math.sin(f * 4.3 + i)
            o.scale = (0.6 * k, 0.5, 0.5); o.keyframe_insert('scale', frame=f)

    # camera: rear-quarter view, engines toward us; it swings about 8 degrees round the ship and zooms in mid-loop
    tgt = bpy.data.objects.new('menu_target', None); sc.collection.objects.link(tgt); tgt.location = (-0.5, 0, 0.3)
    cd = bpy.data.cameras.new('menu_cam'); cd.clip_end = 20000; cd.clip_start = 0.1
    cd.shift_x = -0.13; cd.shift_y = 0.02
    cm = bpy.data.objects.new('menu_cam', cd); sc.collection.objects.link(cm); sc.camera = cm
    tc = cm.constraints.new('TRACK_TO'); tc.target = tgt; tc.track_axis = 'TRACK_NEGATIVE_Z'; tc.up_axis = 'UP_Y'
    for f in range(1, LOOP + 1):
        z = 0.5 - 0.5 * math.cos(TAU * (f - 1) / LOOP)                 # 0 -> 1 -> 0 over the loop: the zoom
        z = z * z * (3 - 2 * z)
        az = math.radians(AZ0 + 8 * wave(f, 1, 0.0))
        el = math.radians(16 + 3 * wave(f, 1, 1.1))
        d = 48 - 13 * z
        cm.location = (d * math.cos(el) * math.cos(az), d * math.cos(el) * math.sin(az), d * math.sin(el))
        cm.keyframe_insert('location', frame=f)
        cd.lens = 30 + 9 * z; cd.keyframe_insert('lens', frame=f)
    for o in [cm, sh] + flames: linear(o)

    # Mars, large on the right, far behind the ship; the sun from the upper left lights its limb and the ship
    sc.frame_set(1)
    fwd = (tgt.location - cm.location).normalized(); right = fwd.cross(Vector((0, 0, 1))).normalized(); up = right.cross(fwd)
    mp = cm.location + fwd * 1500 + right * 760 - up * 120
    cb.put(sc, 'mars', tuple(mp), (0.3, 0.2, 1.2), 420); cb.put(sc, 'marsAtm', tuple(mp), (0, 0, 0), 422)
    # softer surface contrast than the cutscene Mars: rust plains with darker basalt regions, not cloud blotches
    mm = bpy.data.materials.get('marsSurf')
    if mm:
        rp = next((n for n in mm.node_tree.nodes if n.type == 'VALTORGB'), None)
        if rp:
            es = sorted(rp.color_ramp.elements, key=lambda e: e.position)
            for e, (pos, c) in zip(es, ((0.36, (0.26, 0.1, 0.045)), (0.5, (0.55, 0.23, 0.09)), (0.66, (0.82, 0.43, 0.2)))):
                e.position = pos; e.color = (*c, 1)
    # the galaxy: its band crosses the frame diagonally behind the ship, the warm core high in the middle
    band_n = fwd.cross((up * 0.62 + right * 0.78).normalized()).normalized()
    core_dir = (fwd + right * 0.08 + up * 0.32).normalized()
    sc.world = galaxy('pmenu', band_n, core_dir)
    sun_dir = (-right * 0.8 + up * 0.5 + fwd * 0.45).normalized()
    cb.sun(sc, sun_dir, (1.0, 0.93, 0.84), 4.2, 0.01)
    # a cool fill from the galaxy side and a warm bounce from Mars, so the hull never goes flat black
    cb.lamp(sc, tuple(cm.location * 0.6 + Vector((0, 0, 18)) - right * 20), (0.5, 0.5, 1.0), 6e3, 14, 'AREA', target=(0, 0, 0), name='fill')
    cb.lamp(sc, tuple((mp - Vector((0, 0, 0))).normalized() * 40), (1.0, 0.45, 0.2), 1.5e4, 16, 'AREA', target=(0, 0, 0), name='marsBounce')
    # Phobos, a tiny dark moon just off the limb
    try:
        ph = cb.put(sc, 'moonP', tuple(cm.location + fwd * 1400 + right * 300 + up * 300), (0, 0, 0), 7); ph.visible_shadow = False
    except KeyError:
        pass
    return sc

def setup():
    cb.build_lib(); load_cadet(); make_extras()
    for m in bpy.data.materials:
        if m.node_tree:
            for n in m.node_tree.nodes:
                if n.type == 'BUMP': n.inputs['Strength'].default_value *= 0.2
    sc = menu_space()
    tx = {}
    exec(open(BL + r"\cine_textures.py", encoding='utf-8').read(), tx)
    tx['texture_pass'](M)
    return sc

if __name__ == '__main__':
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    os.makedirs(OUT, exist_ok=True)
    sc = setup()
    if argv and argv[0] == 'still':
        frames = [int(x) for x in argv[1:]] or [1]
        for f in frames:
            sc.frame_start = sc.frame_end = f
            try: sc.render.image_settings.media_type = 'IMAGE'
            except Exception: pass
            sc.render.image_settings.file_format = 'JPEG'; sc.render.image_settings.quality = 92
            sc.render.filepath = OUT + "\\_work\\space_####"; sc.render.use_file_extension = True
            bpy.ops.render.render(animation=True, scene=sc.name)
    else:
        sc.render.filepath = OUT + "\\menu_space.mp4"; sc.render.use_file_extension = False
        bpy.ops.render.render(animation=True, scene=sc.name)
        sc.frame_start = sc.frame_end = 1
        try: sc.render.image_settings.media_type = 'IMAGE'
        except Exception: pass
        sc.render.image_settings.file_format = 'JPEG'; sc.render.image_settings.quality = 90
        sc.render.filepath = OUT + "\\menu_space_####"; sc.render.use_file_extension = True
        bpy.ops.render.render(animation=True, scene=sc.name)
    print('MENUDONE')
