# Premium-clay cadet: sculpted suit folds/seams, hair locks, gloves, chunky boots, life-support backpack with straps,
# ribbed hoses and a generic mission patch; optional glass bubble helmet with a sculpted neck ring (Mars).
# All coordinates are in the cadet's Root space (Root is scaled 1.5 in the world, front = -Y, back = +Y).
import bpy, bmesh, math
from mathutils import Vector, Matrix, Euler, noise
import sculpt as S
import clay as C
from nb import NB, srgb

D = bpy.data


def _root_obj(name, bm_builder, coll, mat, parent, voxel=0.008, smooth=4, amp=0.002, seed=0.0):
    root = D.objects["Root"]
    o = S.organic(name, bm_builder, coll, mat, voxel=voxel, smooth=smooth, noise_amp=amp, noise_scale=9.0, seed=seed)
    o.matrix_world = root.matrix_world
    S.parent_keep(o, D.objects[parent])
    return o


def _box(bm, c, size, bevel_r=0.0):
    r = bmesh.ops.create_cube(bm, size=1.0)
    vs = r["verts"]
    bmesh.ops.scale(bm, vec=size, verts=vs)
    bmesh.ops.translate(bm, vec=c, verts=vs)
    return vs


def _capsule(bm, a, b, r, seg=16):
    a, b = Vector(a), Vector(b)
    d = b - a
    L = d.length
    res = bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=seg // 2, radius=r)
    vs = res["verts"]
    q = Vector((0, 0, 1)).rotation_difference(d.normalized())
    for v in vs:
        z = v.co.z
        v.co.z = z + (L / 2 if z > 0 else -L / 2)
        v.co = q @ v.co + (a + b) / 2
    return vs


def _ellipsoid(bm, c, radii, seg=24):
    res = bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=seg // 2, radius=1.0)
    for v in res["verts"]:
        v.co = Vector((v.co.x * radii[0], v.co.y * radii[1], v.co.z * radii[2])) + Vector(c)
    return res["verts"]


def materials(haze, mars):
    dusty = (0.10, 0.42, 0.85) if mars else None    # z0, z1 (root-local, scaled to world below), amount
    seam = lambda B: _suit_seams(B)
    M = {}
    M["suit"] = C.clay("v2_suit", (0.70, 0.30, 0.095), rough=0.66, sss=0.25, sss_radius=(1.0, 0.35, 0.18), sss_scale=0.02,
                       size=0.6, thumbs=0.7, tool=0.4, bump=0.4, dust=0.25, dust_hex="#7A3A1C", ao_dist=0.06, haze=haze,
                       haze_amount=0.15, extra_height=seam, extra_color=(lambda B, col: _dust_obj(B, col, 0.0, 0.42, 0.75)) if mars else None)
    M["suitD"] = C.clay("v2_suitD", (0.45, 0.15, 0.05), rough=0.6, sss=0.2, size=0.6, haze=haze, haze_amount=0.15)
    M["cream"] = C.clay("v2_cream", "#E6D9C0", rough=0.6, sss=0.25, sss_radius=(1.0, 0.8, 0.6), size=0.5, haze=haze, haze_amount=0.15,
                        extra_color=(lambda B, col: _dust_obj(B, col, 0.0, 0.25, 0.8)) if mars else None)
    M["boot"] = C.clay("v2_boot", "#2E3650", rough=0.7, sss=0.08, size=0.5, dust=0.4, dust_hex="#8A4A2A", ao_dist=0.05, haze=haze, haze_amount=0.15,
                       extra_color=(lambda B, col: _dust_obj(B, col, 0.0, 0.22, 0.85)) if mars else None)
    M["belt"] = C.clay("v2_belt", "#262C40", rough=0.6, sss=0.08, size=0.5, haze=haze, haze_amount=0.15)
    M["skin"] = C.clay("v2_skin", (0.80, 0.42, 0.26), rough=0.55, sss=0.45, sss_radius=(1.0, 0.35, 0.2), sss_scale=0.03, size=0.4,
                       thumbs=0.5, tool=0.2, bump=0.25, haze=haze, haze_amount=0.15)
    M["glove"] = C.clay("v2_glove", "#D9D4CA", rough=0.62, sss=0.2, sss_radius=(1.0, 0.8, 0.6), size=0.4, dust=0.35, dust_hex="#9A6A50",
                        ao_dist=0.04, haze=haze, haze_amount=0.15)
    M["hair"] = C.clay("v2_hair", (0.24, 0.115, 0.050), rough=0.40, sss=0.18, sss_radius=(1.0, 0.5, 0.3), size=0.5, thumbs=0.25, tool=0.15,
                       grain=0.5, bump=0.25, sheen=0.3, spec=0.55, haze=haze, haze_amount=0.15, var=0.05)
    M["ink"] = C.clay("v2_ink", "#1A1A26", rough=0.3, sss=0.0, size=0.3)
    M["pack"] = C.clay("v2_pack", "#CBC6BC", rough=0.6, sss=0.18, sss_radius=(1.0, 0.8, 0.6), size=0.5, thumbs=0.8, tool=0.6,
                       dust=0.55, dust_hex="#7A5A48" if not mars else "#9A5A3A", ao_dist=0.06, haze=haze, haze_amount=0.15,
                       height_dust=None)
    M["strap"] = C.clay("v2_strap", "#2A3044", rough=0.75, sss=0.05, size=0.3, thumbs=0.2, tool=0.0, haze=haze, haze_amount=0.15,
                        extra_height=_strap_stitch)
    M["hose"] = C.clay("v2_hose", "#454A58", rough=0.55, sss=0.05, size=0.3, thumbs=0.2, tool=0.0, dust=0.4, dust_hex="#2A2A30",
                       ao_dist=0.03, haze=haze, haze_amount=0.15)
    M["ring"] = C.clay("v2_ring", "#D4D0C8", rough=0.45, sss=0.12, size=0.4, thumbs=0.6, tool=0.8, dust=0.5, dust_hex="#8A5A40",
                       ao_dist=0.04, haze=haze, haze_amount=0.15, spec=0.5)
    M["bolt"] = C.clay("v2_bolt", "#8C8E96", rough=0.4, sss=0.0, metallic=0.6, size=0.2, haze=haze, haze_amount=0.15)
    M["patch"] = _patch_mat(haze)
    M["glass"] = _helmet_mat()
    return M


def _suit_seams(B):
    """back centre seam + shoulder yoke + outer leg/arm seams with stitching (object space == Root space)"""
    tc = B.node("ShaderNodeTexCoord").outputs["Object"]
    x, y, z = B.sep(tc)
    ax = B.math("ABSOLUTE", x)
    back = B.maprange(y, 0.02, 0.08, 0.0, 1.0)
    g_back = B.math("MULTIPLY", B.maprange(ax, 0.006, 0.0, 0.0, 1.0, "SMOOTHSTEP"), back)
    g_yoke = B.math("MULTIPLY", B.maprange(B.math("ABSOLUTE", B.math("SUBTRACT", z, 0.985)), 0.006, 0.0, 0.0, 1.0, "SMOOTHSTEP"), back)
    g_leg = B.math("MULTIPLY", B.maprange(B.math("ABSOLUTE", B.math("SUBTRACT", ax, 0.232)), 0.006, 0.0, 0.0, 1.0, "SMOOTHSTEP"),
                   B.maprange(z, 0.62, 0.58, 0.0, 1.0))
    groove = B.math("MAXIMUM", B.math("MAXIMUM", g_back, g_yoke), g_leg)
    # stitching: short dashes 1.3 cm either side of the back seam, along z
    dash = B.maprange(B.math("SINE", B.math("MULTIPLY", z, 260.0)), 0.2, 0.6, 0.0, 1.0)
    side = B.maprange(B.math("ABSOLUTE", B.math("SUBTRACT", ax, 0.014)), 0.003, 0.0, 0.0, 1.0, "SMOOTHSTEP")
    stitch = B.math("MULTIPLY", B.math("MULTIPLY", dash, side), back)
    return B.math("ADD", B.math("MULTIPLY", groove, -0.9), B.math("MULTIPLY", stitch, 0.35))


def _dust_obj(B, col, z0, z1, amt):
    tc = B.node("ShaderNodeTexCoord").outputs["Object"]
    z = B.sep(tc)[2]
    nz = B.node("ShaderNodeTexNoise")
    B.set(nz.inputs["Vector"], tc)
    nz.inputs["Scale"].default_value = 14.0
    zz = B.math("ADD", z, B.math("MULTIPLY", B.math("SUBTRACT", nz.outputs["Fac"], 0.5), 0.12))
    m = B.maprange(zz, z1, z0, 0.0, amt, "SMOOTHSTEP")
    return B.mix(m, col, srgb("#A8633C"))


def _hair_strands(B):
    tc = B.node("ShaderNodeTexCoord").outputs["Object"]
    n = B.node("ShaderNodeTexNoise")
    B.set(n.inputs["Vector"], B.vmath("MULTIPLY", tc, (55.0, 55.0, 7.0)))
    n.inputs["Scale"].default_value, n.inputs["Detail"].default_value = 3.0, 4.0
    return B.math("MULTIPLY", B.math("SUBTRACT", n.outputs["Fac"], 0.5), 0.9)


def _strap_stitch(B):
    tc = B.node("ShaderNodeTexCoord").outputs["Object"]
    w = B.node("ShaderNodeTexWave")
    B.set(w.inputs["Vector"], tc)
    w.inputs["Scale"].default_value = 60.0
    return B.math("MULTIPLY", B.math("SUBTRACT", w.outputs["Fac"], 0.5), 0.15)


SUN_DIR = Vector((0.4, 0.9, 0.1))


def _helmet_mat():
    """clear glass bubble: low-IOR transmission (keeps the hair visible), thin fresnel rim highlight,
    soft blue sheen of the Martian sunset aureole on the sun side"""
    m = D.materials.get("v2_helmet_glass") or D.materials.new("v2_helmet_glass")
    if not m.node_tree:
        m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    B = NB(nt)
    g = B.node("ShaderNodeBsdfPrincipled")
    g.inputs["Base Color"].default_value = (*srgb("#F4F8FF"), 1)
    g.inputs["Transmission Weight"].default_value = 1.0
    g.inputs["Roughness"].default_value = 0.0
    g.inputs["IOR"].default_value = 1.18
    geo = B.node("ShaderNodeNewGeometry")
    lw = B.node("ShaderNodeLayerWeight")
    lw.inputs["Blend"].default_value = 0.5
    rim = B.maprange(lw.outputs["Facing"], 0.78, 0.97, 0.0, 1.0, "SMOOTHSTEP")
    sund = Vector((SUN_DIR.x, SUN_DIR.y, 0.0)).normalized()
    side = B.maprange(B.vmath("DOT_PRODUCT", geo.outputs["Normal"], B.vec(sund)), -0.2, 0.9, 0.0, 1.0, "SMOOTHSTEP")
    em = B.node("ShaderNodeEmission")
    col = B.mix(side, srgb("#E8F0FF"), srgb("#8FB8F0"))
    B.set(em.inputs["Color"], col)
    sheen = B.math("MULTIPLY", B.math("MULTIPLY", side, B.maprange(lw.outputs["Facing"], 0.3, 0.9, 0.0, 1.0)), 0.18)
    B.set(em.inputs["Strength"], B.math("ADD", B.math("MULTIPLY", rim, 0.55), sheen))
    ad = B.node("ShaderNodeAddShader")
    nt.links.new(g.outputs[0], ad.inputs[0])
    nt.links.new(em.outputs[0], ad.inputs[1])
    nt.links.new(ad.outputs[0], B.node("ShaderNodeOutputMaterial").inputs["Surface"])
    return m


def _interp(prof, z):
    """prof: [(z, f)] sorted top -> bottom; cosine interpolation"""
    if z >= prof[0][0]:
        return prof[0][1]
    for (z0, f0), (z1, f1) in zip(prof, prof[1:]):
        if z1 <= z <= z0:
            t = (z0 - z) / (z0 - z1)
            t = 0.5 - 0.5 * math.cos(math.pi * t)
            return f0 + (f1 - f0) * t
    return prof[-1][1]


def _rebuild_limb(obj, cx, cy, rx, ry, prof, root, voxel=0.0068):
    """replace a boxy limb with an organic tapered form (elliptical sections following prof), voxel-remeshed and smoothed"""
    z_top, z_bot = prof[0][0], prof[-1][0]

    def bmb(bm):
        seg, n = 40, 46
        rings = []
        for i in range(n + 1):
            z = z_top - (z_top - z_bot) * i / n
            f = _interp(prof, z)
            rings.append([bm.verts.new((cx + rx * f * math.cos(a), cy + ry * f * math.sin(a), z)) for a in [2 * math.pi * k / seg for k in range(seg)]])
        for i in range(n):
            for k in range(seg):
                bm.faces.new((rings[i][k], rings[i][(k + 1) % seg], rings[i + 1][(k + 1) % seg], rings[i + 1][k]))
        for ring, zc, sgn in ((rings[0], z_top, 1), (rings[-1], z_bot, -1)):
            f = _interp(prof, zc)
            prev = ring
            for j in range(1, 6):
                a = j / 6 * math.pi / 2
                cur = [bm.verts.new((cx + rx * f * math.cos(a) * math.cos(t), cy + ry * f * math.cos(a) * math.sin(t),
                                     zc + sgn * min(rx, ry) * f * 0.8 * math.sin(a))) for t in [2 * math.pi * k / seg for k in range(seg)]]
                for k in range(seg):
                    q = (prev[k], prev[(k + 1) % seg], cur[(k + 1) % seg], cur[k])
                    bm.faces.new(q if sgn > 0 else tuple(reversed(q)))
                prev = cur
            bm.faces.new(prev if sgn < 0 else list(reversed(prev)))
    tmp = S.organic(obj.name + "_v2", bmb, D.collections["cadet"], None, voxel=voxel, smooth=5)
    me = tmp.data
    Mx = (root.matrix_world.inverted() @ obj.matrix_world).inverted()
    me.transform(Mx)
    mats = list(obj.data.materials)
    obj.data = me
    me.materials.clear()
    for mt in mats:
        me.materials.append(mt)
    for p in me.polygons:
        p.use_smooth = True
    D.objects.remove(tmp, do_unlink=True)


def _patch_mat(haze):
    """generic mission patch: gold embroidered border, navy field, a small rust planet with a white orbit and a star"""
    m = D.materials.get("v2_patch") or D.materials.new("v2_patch")
    if not m.node_tree:
        m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    B = NB(nt)
    tc = B.node("ShaderNodeTexCoord").outputs["Object"]
    x, y, z = B.sep(B.vmath("SCALE", tc, scale=1.0 / 0.05))
    r = B.vmath("LENGTH", B.vmath("MULTIPLY", tc, (20.0, 20.0, 0.0)))
    border = B.maprange(r, 0.80, 0.86, 0.0, 1.0)
    planet_d = B.vmath("LENGTH", B.vmath("SUBTRACT", B.vmath("MULTIPLY", tc, (20.0, 20.0, 0.0)), (0.15, -0.12, 0.0)))
    planet = B.maprange(planet_d, 0.36, 0.33, 0.0, 1.0)
    shade = B.maprange(B.math("ADD", x, y), -0.3, 0.6, 0.55, 1.0)
    orbit_e = B.vmath("LENGTH", B.vmath("MULTIPLY", B.vmath("SUBTRACT", B.vmath("MULTIPLY", tc, (20.0, 20.0, 0.0)), (0.12, -0.10, 0.0)), (0.55, 1.4, 0.0)))
    orbit = B.math("MULTIPLY", B.maprange(B.math("ABSOLUTE", B.math("SUBTRACT", orbit_e, 0.36)), 0.035, 0.0, 0.0, 1.0), B.math("SUBTRACT", 1.0, planet))
    star_d = B.vmath("LENGTH", B.vmath("SUBTRACT", B.vmath("MULTIPLY", tc, (20.0, 20.0, 0.0)), (-0.38, 0.36, 0.0)))
    star = B.maprange(star_d, 0.09, 0.05, 0.0, 1.0)
    col = B.rgb(srgb("#1F2C55"))
    col = B.mix(planet, col, B._scale_sock(srgb("#B4502A"), shade))
    col = B.mix(orbit, col, srgb("#EDE6D6"))
    col = B.mix(star, col, srgb("#F2E8C8"))
    col = B.mix(border, col, srgb("#D9A040"))
    st = B.node("ShaderNodeTexWave")
    B.set(st.inputs["Vector"], B.vmath("SCALE", tc, scale=40.0))
    st.inputs["Scale"].default_value, st.inputs["Distortion"].default_value = 6.0, 2.0
    bmp = B.node("ShaderNodeBump")
    bmp.inputs["Strength"].default_value = 0.35
    B.set(bmp.inputs["Height"], B.math("ADD", st.outputs["Fac"], B.math("MULTIPLY", border, 0.6)))
    p = B.node("ShaderNodeBsdfPrincipled")
    B.set(p.inputs["Base Color"], col)
    p.inputs["Roughness"].default_value = 0.8
    p.inputs["Sheen Weight"].default_value = 0.6
    nt.links.new(bmp.outputs["Normal"], p.inputs["Normal"])
    out = p.outputs[0]
    if haze is not None:
        hg = B.node("ShaderNodeGroup")
        hg.node_tree = haze
        hg.inputs["Amount"].default_value = 0.15
        nt.links.new(out, hg.inputs["Shader"])
        out = hg.outputs["Shader"]
    nt.links.new(out, B.node("ShaderNodeOutputMaterial").inputs["Surface"])
    return m


def upgrade(helmet=False, mars=False, haze=None, sun_dir=None):
    global SUN_DIR
    if sun_dir is not None:
        SUN_DIR = Vector(sun_dir)
    root = D.objects["Root"]
    hair_o = D.objects["Hair"]
    for m_ in list(hair_o.modifiers):                 # the noisy clay lumps made the hair read as wrinkles
        if m_.type == "DISPLACE":
            hair_o.modifiers.remove(m_)
    coll = D.collections["cadet"]
    M = materials(haze, mars)
    mapping = {"suit": M["suit"], "suitD": M["suitD"], "cream": M["cream"], "boot": M["boot"], "belt": M["belt"],
               "skin": M["skin"], "hair": M["hair"], "ink": M["ink"], "patch": M["suit"]}
    # ---------------------------------------------------------------- bake + sculpt the existing parts
    VOX = {"Torso": 0.008, "LegL": 0.0075, "LegR": 0.0075, "ArmL": 0.0075, "ArmR": 0.0075, "BootL": 0.008, "BootR": 0.008,
           "Hair": 0.007, "Face": 0.009, "Collar": 0.008, "CuffL": 0.007, "CuffR": 0.007, "HandL": 0.006, "HandR": 0.006,
           "Belt": 0.008, "SoleL": 0.006, "SoleR": 0.006, "Zip": 0.006, "Patch": 0.006}
    for o in list(coll.all_objects):
        if o.type != "MESH" or o.hide_render:
            continue
        if o.name in VOX:
            S.bake(o, voxel=VOX[o.name])
        old = o.data.materials[0].name if o.data.materials else None
        o.data.materials.clear()
        if o.name.startswith("Hand"):
            o.data.materials.append(M["glove"])
        elif old in mapping:
            o.data.materials.append(mapping[old])
        else:
            o.data.materials.append(M["suit"])

    ARM = [(1.025, 0.80), (0.995, 0.97), (0.95, 1.04), (0.90, 1.02), (0.855, 0.90), (0.83, 0.88), (0.79, 0.95), (0.74, 0.93),
           (0.70, 0.84), (0.668, 0.80)]
    LEG = [(0.625, 0.90), (0.58, 1.02), (0.52, 1.06), (0.46, 1.02), (0.405, 0.90), (0.375, 0.88), (0.33, 0.95), (0.28, 0.98),
           (0.235, 0.90), (0.19, 0.80), (0.15, 0.78)]
    for side, sx in (("R", 1), ("L", -1)):
        _rebuild_limb(D.objects["Arm" + side], 0.345 * sx, 0.0, 0.092, 0.084, ARM, root)
        _rebuild_limb(D.objects["Leg" + side], 0.131 * sx, 0.0, 0.104, 0.102, LEG, root)

    def fabric(p, n, creases, axis_c, amp=0.0085, w=0.009):
        d = 0.003 * noise.noise(p * 6.0)
        th = math.atan2(p.y - axis_c[1], p.x - axis_c[0])
        for i, zc in enumerate(creases):
            zz = zc + 0.03 * math.sin(th + i * 2.1) + 0.008 * noise.noise(p * 9 + Vector((i, 0, 0)))
            fade = 0.35 + 0.65 * max(0.0, math.sin(th * 1.0 + i * 1.7 + 0.6))   # creases die out around the limb
            d += (-amp * S.gauss(p.z - zz, w) + 0.0035 * S.gauss(p.z - zz - 0.02, 0.012)) * fade
        return d

    for side, sx in (("R", 1), ("L", -1)):
        S.displace(D.objects["Arm" + side], root, lambda p, n, sx=sx: fabric(p, n, (0.825, 0.865), (0.345 * sx, 0.0), amp=0.007))
        S.displace(D.objects["Leg" + side], root, lambda p, n, sx=sx: fabric(p, n, (0.37, 0.41, 0.215), (0.131 * sx, 0.0), amp=0.008))

    def torso_fn(p, n):
        d = fabric(p, n, (0.705,), (0.0, 0.0), amp=0.009)
        if p.y > 0.04:                                     # back centre seam + yoke seam grooves
            d += -0.004 * S.gauss(p.x, 0.006) - 0.0035 * S.gauss(p.z - 0.985, 0.006)
        return d
    S.displace(D.objects["Torso"], root, torso_fn)

    # hair: sculpted locks flowing from the crown (elongated voronoi cells), grooves between locks
    hc = Vector((0.0, 0.02, 1.42))

    def hair_fn(p, n):
        # combed, sculpted locks: ridges radiating from the crown, phase wobble so they curve, deeper toward the fringe
        v = p - hc
        if v.length < 1e-6:
            return 0.0
        phi = math.acos(max(-1.0, min(1.0, v.z / v.length)))
        th = math.atan2(v.x, v.y)
        # ~11 chunky locks with a gentle swirl: rounded lobes, clean grooves, tips swelling at the fringe
        # irregular lock widths (warped azimuth), per-lock depth variation and a swirl from the crown
        thw = th + 0.22 * math.sin(th * 3.0 + 0.7) + 0.12 * math.sin(th * 5.0 + 2.1)
        swirl = 0.45 * phi + 0.22 * noise.noise(Vector((math.sin(th) * 1.2, math.cos(th) * 1.2, phi * 0.8)))
        k = thw * 9.0 + swirl * 2.0
        lobe = 0.5 - 0.5 * math.cos(k)
        lock_id = math.floor(k / (2 * math.pi))
        depth = 0.75 + 0.45 * (0.5 + 0.5 * math.sin(lock_id * 2.39 + 1.0))
        shape = lobe ** 0.6 - 0.6
        crown = S.smoothstep(0.12, 0.6, phi)
        tip = S.smoothstep(1.05, 1.5, phi)
        return 0.0125 * shape * depth * crown * (0.75 + 0.6 * tip)
    S.displace(D.objects["Hair"], root, hair_fn, smooth_after=4)

    # gloves: finger grooves on the lower half, slight knuckle ridge
    for side, sx in (("R", 1), ("L", -1)):
        hcen = Vector((0.357 * sx, 0.0, 0.55))

        def glove_fn(p, n, hcen=hcen):
            low = S.smoothstep(0.565, 0.515, p.z)
            d = 0.0
            for yi in (-0.032, 0.0, 0.032):
                d += -0.0075 * S.gauss(p.y - hcen.y - yi, 0.0065) * low
            d += 0.004 * S.gauss(p.z - 0.575, 0.01)
            return d
        S.displace(D.objects["Hand" + side], root, glove_fn)

    # boots: chunky tread lugs on the soles, ankle crease on the boot
    for side in ("R", "L"):
        def sole_fn(p, n):
            lug = 1.0 if math.sin(p.y * 2 * math.pi / 0.045) > 0.0 else 0.0
            low = S.smoothstep(0.034, 0.0, p.z)
            return 0.010 * lug * low * max(0.0, 1.0 - abs(n.z))
        so = D.objects["Sole" + side]
        so.scale.z *= 1.6
        S.displace(so, root, sole_fn)
        S.displace(D.objects["Boot" + side], root, lambda p, n: -0.006 * S.gauss(p.z - 0.15, 0.007) + 0.003 * noise.noise(p * 10))

    for sx in (1, -1):                                  # rolled fabric cuffs above the boots
        path = [Vector((0.131 * sx + 0.093 * math.cos(a), 0.092 * math.sin(a), 0.192 + 0.004 * math.sin(3 * a))) for a in [2 * math.pi * k / 48 for k in range(49)]]
        ac = S.tube(f"cad_anklecuff{sx}", path, lambda t, L: 0.022, coll, M["cream"], seg=12)
        ac.matrix_world = root.matrix_world
        S.parent_keep(ac, D.objects["HipR" if sx > 0 else "HipL"])

    # ---------------------------------------------------------------- new parts
    def pack_bm(bm):
        _box(bm, (0.0, 0.27, 0.865), (0.42, 0.18, 0.42))
        for sx in (1, -1):
            _capsule(bm, (0.225 * sx, 0.28, 0.70), (0.225 * sx, 0.28, 0.99), 0.043)
        _box(bm, (0.0, 0.275, 1.075), (0.36, 0.15, 0.035))          # lid
    pack = _root_obj("cad_backpack", pack_bm, coll, M["pack"], "Body", voxel=0.0075, smooth=6, amp=0.0018, seed=3.0)

    def pack_fn(p, n):
        d = 0.0
        if n.y > 0.5:                                   # back face: vents + access panel
            for zi in (0.935, 0.962, 0.989, 1.016):
                d += -0.010 * S.gauss(p.z - zi, 0.0048) * S.smoothstep(0.13, 0.11, abs(p.x))
            d += 0.004 * S.smoothstep(0.012, 0.0, max(abs(p.x - 0.06) - 0.07, abs(p.z - 0.76) - 0.06))
        d += -0.004 * S.gauss(p.z - 1.052, 0.004)       # lid seam
        return d
    S.displace(pack, root, pack_fn)

    def curve_points(pts):
        return S.catmull(pts, 14)

    for sx in (1, -1):
        sp = curve_points([(0.135 * sx, 0.20, 1.03), (0.16 * sx, 0.13, 1.135), (0.165 * sx, 0.0, 1.16), (0.162 * sx, -0.13, 1.10), (0.155 * sx, -0.19, 0.95)])
        st = S.strap(f"cad_strap{sx}", sp, 0.068, 0.016, coll, M["strap"], up_hint=lambda p: (Vector((p.x * 0.3, p.y, p.z - 0.85))).normalized())
        st.matrix_world = root.matrix_world
        S.parent_keep(st, D.objects["Body"])
        wp = curve_points([(0.19 * sx, 0.20, 0.70), (0.25 * sx, 0.12, 0.665), (0.272 * sx, 0.0, 0.63)])
        ws = S.strap(f"cad_waist{sx}", wp, 0.05, 0.014, coll, M["strap"], up_hint=lambda p: Vector((p.x, p.y, 0.0)).normalized())
        ws.matrix_world = root.matrix_world
        S.parent_keep(ws, D.objects["Body"])
        end = (0.245 * sx, 0.06, 1.13) if helmet else (0.185 * sx, 0.03, 1.10)
        hp = curve_points([(0.205 * sx, 0.30, 1.0), (0.27 * sx, 0.25, 1.10), (0.255 * sx, 0.14, 1.165), end])
        hs = S.tube(f"cad_hose{sx}", hp, lambda t, L: 0.021 * (1.0 + 0.16 * max(0.0, math.sin(L * 2 * math.pi / 0.022))), coll, M["hose"], seg=14)
        hs.matrix_world = root.matrix_world
        S.parent_keep(hs, D.objects["Body"])

    # mission patch on the backpack (generic emblem)
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=40, radius1=0.05, radius2=0.048, depth=0.008)
    me = D.meshes.new("cad_patch")
    bm.to_mesh(me)
    bm.free()
    pt = D.objects.new("cad_patch", me)
    coll.objects.link(pt)
    me.materials.append(M["patch"])
    for p in me.polygons:
        p.use_smooth = True
    pt.matrix_world = root.matrix_world @ Matrix.Translation((-0.105, 0.362, 0.82)) @ Euler((math.radians(-90), 0, 0)).to_matrix().to_4x4()
    S.parent_keep(pt, D.objects["Body"])

    # ears (help the head read from behind and make the glance visible)
    for sx in (1, -1):
        e = _root_obj(f"cad_ear{sx}", lambda bm, sx=sx: _ellipsoid(bm, (0.352 * sx, 0.02, 1.405), (0.035, 0.07, 0.085)), coll, M["skin"], "Head",
                      voxel=0.006, smooth=3, amp=0.001, seed=7 + sx)

    if helmet:
        c = Vector((0.0, 0.02, 1.46))

        def ring_bm(bm):
            res = bmesh.ops.create_circle(bm, cap_ends=False, segments=48, radius=0.335)
            for k in range(6):
                pass
        # neck ring: torus swept as a tube around the neck
        path = [Vector((0.335 * math.cos(a), 0.02 + 0.335 * math.sin(a), 1.108)) for a in [2 * math.pi * k / 64 for k in range(65)]]
        rg = S.tube("cad_neckring", path, lambda t, L: 0.048, coll, M["ring"], seg=18)
        rg.scale = (1, 1, 1)
        me = rg.data
        for v in me.vertices:                     # flatten + sculpt the ring profile, add hand-made wobble
            v.co.z = 1.108 + (v.co.z - 1.108) * 0.62 + 0.003 * noise.noise(v.co * 11)
        me.update()
        rg.matrix_world = root.matrix_world
        S.parent_keep(rg, D.objects["Body"])
        for k in range(8):
            a = 2 * math.pi * (k + 0.5) / 8
            bm = bmesh.new()
            bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=6, radius=0.014)
            for v in bm.verts:
                v.co.z *= 0.55
            me = D.meshes.new(f"cad_ringbolt{k}")
            bm.to_mesh(me)
            bm.free()
            b = D.objects.new(f"cad_ringbolt{k}", me)
            coll.objects.link(b)
            me.materials.append(M["bolt"])
            for p in me.polygons:
                p.use_smooth = True
            b.matrix_world = root.matrix_world @ Matrix.Translation((0.335 * math.cos(a), 0.02 + 0.335 * math.sin(a), 1.136))
            S.parent_keep(b, D.objects["Body"])
        # glass bubble: sphere shell open at the neck ring
        bm = bmesh.new()
        bmesh.ops.create_uvsphere(bm, u_segments=96, v_segments=48, radius=0.505)
        for v in bm.verts:
            v.co += c
        bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.calc_center_median().z < 1.125], context="FACES")
        me = D.meshes.new("cad_helmet")
        bm.to_mesh(me)
        bm.free()
        hb = D.objects.new("cad_helmet", me)
        coll.objects.link(hb)
        me.materials.append(M["glass"])
        for p in me.polygons:
            p.use_smooth = True
        so = hb.modifiers.new("shell", "SOLIDIFY")
        so.thickness, so.offset = 0.007, -1.0
        hb.matrix_world = root.matrix_world
        S.parent_keep(hb, D.objects["Body"])
        hb.visible_shadow = False                # thin clear glass: no shadow blob on the face/hair
    return M
