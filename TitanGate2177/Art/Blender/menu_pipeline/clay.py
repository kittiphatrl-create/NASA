# Cycles "premium clay" material kit (all procedural): SSS clay with grain, thumbprints, tool marks and crevice dust,
# camera-ray aerial haze matched to the menu sky, gold metal, glass, emission. Plus Cycles/OptiX/OIDN setup.
import bpy, math
from nb import NB, srgb


def cycles_setup(sc, samples=256, seed=2177, threshold=0.01, denoise=True, max_bounces=8):
    sc.render.engine = "CYCLES"
    prefs = bpy.context.preferences.addons["cycles"].preferences
    dev = "CPU"
    for t in ("OPTIX", "CUDA", "HIP", "ONEAPI"):
        try:
            prefs.compute_device_type = t
            prefs.refresh_devices()
            gpus = [d for d in prefs.devices if d.type == t]
            if gpus:
                for d in prefs.devices:
                    d.use = d.type == t
                dev = t
                break
        except Exception:
            pass
    c = sc.cycles
    c.device = "GPU" if dev != "CPU" else "CPU"
    c.samples = samples
    c.use_adaptive_sampling = True
    c.adaptive_threshold = threshold
    c.seed = seed
    c.use_animated_seed = False
    c.use_denoising = denoise
    try:
        c.denoiser = "OPENIMAGEDENOISE"
        c.denoising_input_passes = "RGB_ALBEDO_NORMAL"
        c.denoising_prefilter = "ACCURATE"
        c.denoising_quality = "HIGH"
        c.denoising_use_gpu = dev != "CPU"
    except Exception as e:
        print("denoise opt", e)
    c.max_bounces = max_bounces
    c.diffuse_bounces, c.glossy_bounces, c.transmission_bounces = 4, 4, 8
    c.volume_bounces, c.transparent_max_bounces = 0, 16
    c.caustics_reflective = c.caustics_refractive = False
    c.blur_glossy = 1.0
    c.sample_clamp_indirect = 6.0
    try:
        c.use_light_tree = True
    except Exception:
        pass
    sc.render.film_transparent = False
    sc.cycles.film_transparent_glass = True
    sc.cycles.film_transparent_roughness = 0.1
    return dev


# ---------------------------------------------------------------------------- haze (camera rays only)
def haze_group(sun_dir, far_hex, sun_hex, exp_, blue_hex=None, blue_exp=260.0, blue_k=0.0,
               start=12.0, scale=95.0, fog_max=0.8, name="HazeV2"):
    g = bpy.data.node_groups.get(name)
    if g:
        bpy.data.node_groups.remove(g)
    g = bpy.data.node_groups.new(name, "ShaderNodeTree")
    g.interface.new_socket("Shader", in_out="INPUT", socket_type="NodeSocketShader")
    s = g.interface.new_socket("Amount", in_out="INPUT", socket_type="NodeSocketFloat")
    s.default_value = 1.0
    g.interface.new_socket("Shader", in_out="OUTPUT", socket_type="NodeSocketShader")
    B = NB(g)
    gi, go = B.node("NodeGroupInput"), B.node("NodeGroupOutput")
    geo, camd, lp = B.node("ShaderNodeNewGeometry"), B.node("ShaderNodeCameraData"), B.node("ShaderNodeLightPath")
    dd = B.math("MAXIMUM", B.math("SUBTRACT", camd.outputs["View Distance"], start), 0.0)
    fd = B.math("SUBTRACT", 1.0, B.math("EXPONENT", B.math("MULTIPLY", dd, -1.0 / scale)))
    hm = B.maprange(B.sep(geo.outputs["Position"])[2], 0.0, 14.0, 1.0, 0.62)
    f = B.math("MULTIPLY", B.math("MULTIPLY", fd, hm), B.math("MULTIPLY", gi.outputs["Amount"], fog_max), clamp=True)
    f = B.math("MULTIPLY", f, lp.outputs["Is Camera Ray"])
    vdir = B.vmath("SCALE", geo.outputs["Incoming"], scale=-1.0)
    sd = B.math("MAXIMUM", B.vmath("DOT_PRODUCT", vdir, B.vec(sun_dir)), 0.0)
    col = B.mix(B.math("POWER", sd, exp_), srgb(far_hex), srgb(sun_hex))
    if blue_hex and blue_k > 0:
        col = B.mix(B.math("MULTIPLY", B.math("POWER", sd, blue_exp), blue_k), col, srgb(blue_hex))
    em = B.node("ShaderNodeEmission")
    B.set(em.inputs["Color"], col)
    ms = B.node("ShaderNodeMixShader")
    B.set(ms.inputs[0], f)
    g.links.new(gi.outputs["Shader"], ms.inputs[1])
    g.links.new(em.outputs[0], ms.inputs[2])
    g.links.new(ms.outputs[0], go.inputs["Shader"])
    return g


# ---------------------------------------------------------------------------- shared clay surface detail
def _detail(B, size, thumbs=1.0, tool=0.5, grain=1.0, coords=None):
    """returns a height socket: clay grain + thumbprint dents with faint ridges + tool-mark strokes"""
    tc = coords if coords is not None else B.node("ShaderNodeTexCoord").outputs["Object"]
    p = B.vmath("SCALE", tc, scale=1.0 / size)
    n_grain = B.node("ShaderNodeTexNoise")
    B.set(n_grain.inputs["Vector"], p)
    n_grain.inputs["Scale"].default_value, n_grain.inputs["Detail"].default_value = 140.0, 2.0
    h = B.math("MULTIPLY", B.math("SUBTRACT", n_grain.outputs["Fac"], 0.5), 0.12 * grain)
    if thumbs > 0:
        vor = B.node("ShaderNodeTexVoronoi")
        B.set(vor.inputs["Vector"], p)
        vor.inputs["Scale"].default_value = 7.0
        try:
            vor.inputs["Randomness"].default_value = 0.9
        except Exception:
            pass
        d = vor.outputs["Distance"]
        inside = B.maprange(d, 0.38, 0.12, 0.0, 1.0, "SMOOTHSTEP")           # 1 in the middle of a print
        dent = B.math("MULTIPLY", inside, -0.55 * thumbs)
        ridge = B.math("MULTIPLY", B.math("MULTIPLY", B.math("SINE", B.math("MULTIPLY", d, 95.0)), inside), 0.10 * thumbs)
        # only some cells carry a print
        sel = B.node("ShaderNodeTexWhiteNoise")
        B.set(sel.inputs["Vector"], vor.outputs["Position"])
        keep = B.maprange(sel.outputs["Value"], 0.55, 0.6, 0.0, 1.0)
        h = B.math("ADD", h, B.math("MULTIPLY", B.math("ADD", dent, ridge), keep))
    if tool > 0:
        wv = B.node("ShaderNodeTexWave")
        B.set(wv.inputs["Vector"], p)
        wv.inputs["Scale"].default_value, wv.inputs["Distortion"].default_value = 5.0, 6.0
        wv.inputs["Detail"].default_value, wv.inputs["Detail Scale"].default_value = 2.0, 1.5
        msk = B.node("ShaderNodeTexNoise")
        B.set(msk.inputs["Vector"], p)
        msk.inputs["Scale"].default_value = 1.8
        m = B.maprange(msk.outputs["Fac"], 0.52, 0.62, 0.0, 1.0)
        h = B.math("ADD", h, B.math("MULTIPLY", B.math("MULTIPLY", B.math("SUBTRACT", wv.outputs["Fac"], 0.5), m), 0.35 * tool))
    return h


def clay(name, color, rough=0.62, sss=0.22, sss_radius=(1.0, 0.42, 0.22), sss_scale=0.03, size=1.0,
         thumbs=1.0, tool=0.5, grain=1.0, bump=0.35, dust=0.0, dust_hex="#9A5A3C", ao_dist=0.12, sheen=0.25,
         haze=None, haze_amount=1.0, var=0.08, extra_height=None, extra_color=None, height_dust=None, spec=0.35, metallic=0.0):
    """color: hex or linear tuple. extra_height(B) -> socket added to the bump height.
    extra_color(B, col) -> socket colour post-process.  height_dust=(z0, z1, amount): world-z dust gradient (low = dusty)."""
    m = bpy.data.materials.get(name)
    if m:
        bpy.data.materials.remove(m)
    m = bpy.data.materials.new(name)
    if not m.node_tree:
        m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    B = NB(nt)
    base = srgb(color) if isinstance(color, str) else tuple(color)
    tc = B.node("ShaderNodeTexCoord").outputs["Object"]
    lowf = B.node("ShaderNodeTexNoise")
    B.set(lowf.inputs["Vector"], B.vmath("SCALE", tc, scale=1.0 / size))
    lowf.inputs["Scale"].default_value, lowf.inputs["Detail"].default_value = 2.5, 3.0
    vfac = B.maprange(lowf.outputs["Fac"], 0.3, 0.7, 1.0 - var, 1.0 + var)
    col = B._scale_sock(base, vfac)
    h = _detail(B, size, thumbs, tool, grain, tc)
    if extra_height is not None:
        h = B.math("ADD", h, extra_height(B))
    rough_s = B.maprange(lowf.outputs["Fac"], 0.3, 0.7, rough - 0.06, rough + 0.06)
    if dust > 0:
        ao = B.node("ShaderNodeAmbientOcclusion")
        ao.samples = 8
        ao.inputs["Distance"].default_value = ao_dist
        crev = B.maprange(ao.outputs["AO"], 0.95, 0.45, 0.0, dust, "SMOOTHSTEP")
        col = B.mix(crev, col, srgb(dust_hex))
        rough_s = B.math("ADD", rough_s, B.math("MULTIPLY", crev, 0.2))
    if height_dust:
        z0, z1, amt = height_dust
        geo = B.node("ShaderNodeNewGeometry")
        nz = B.node("ShaderNodeTexNoise")
        B.set(nz.inputs["Vector"], B.vmath("SCALE", tc, scale=1.0 / size))
        nz.inputs["Scale"].default_value = 6.0
        zz = B.math("ADD", B.sep(geo.outputs["Position"])[2], B.math("MULTIPLY", B.math("SUBTRACT", nz.outputs["Fac"], 0.5), (z1 - z0) * 0.5))
        hm = B.maprange(zz, z1, z0, 0.0, amt, "SMOOTHSTEP")
        col = B.mix(hm, col, srgb(dust_hex))
        rough_s = B.math("ADD", rough_s, B.math("MULTIPLY", hm, 0.25))
    if extra_color is not None:
        col = extra_color(B, col)
    bmp = B.node("ShaderNodeBump")
    bmp.inputs["Strength"].default_value = bump
    bmp.inputs["Distance"].default_value = 0.01 * size
    B.set(bmp.inputs["Height"], h)
    p = B.node("ShaderNodeBsdfPrincipled")
    B.set(p.inputs["Base Color"], col)
    B.set(p.inputs["Roughness"], rough_s)
    p.inputs["Metallic"].default_value = metallic
    p.inputs["Subsurface Weight"].default_value = sss
    p.inputs["Subsurface Radius"].default_value = sss_radius
    p.inputs["Subsurface Scale"].default_value = sss_scale
    p.inputs["Specular IOR Level"].default_value = spec
    p.inputs["Sheen Weight"].default_value = sheen
    p.inputs["Sheen Roughness"].default_value = 0.6
    nt.links.new(bmp.outputs["Normal"], p.inputs["Normal"])
    out_sh = p.outputs[0]
    if haze is not None:
        hg = B.node("ShaderNodeGroup")
        hg.node_tree = haze
        hg.inputs["Amount"].default_value = haze_amount
        nt.links.new(out_sh, hg.inputs["Shader"])
        out_sh = hg.outputs["Shader"]
    o = B.node("ShaderNodeOutputMaterial")
    nt.links.new(out_sh, o.inputs["Surface"])
    try:
        m.cycles.emission_sampling = "NONE"
    except Exception:
        pass
    return m


def emission(name, hexcol, strength, haze=None):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    if not m.node_tree:
        m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    B = NB(nt)
    e = B.node("ShaderNodeEmission")
    e.inputs["Color"].default_value = (*srgb(hexcol), 1) if isinstance(hexcol, str) else (*hexcol, 1)
    e.inputs["Strength"].default_value = strength
    o = B.node("ShaderNodeOutputMaterial")
    nt.links.new(e.outputs[0], o.inputs["Surface"])
    return m


def glass(name, tint="#EAF2FF", rough=0.02, ior=1.45):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    if not m.node_tree:
        m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    B = NB(nt)
    p = B.node("ShaderNodeBsdfPrincipled")
    p.inputs["Base Color"].default_value = (*srgb(tint), 1)
    p.inputs["Transmission Weight"].default_value = 1.0
    p.inputs["Roughness"].default_value = rough
    p.inputs["IOR"].default_value = ior
    o = B.node("ShaderNodeOutputMaterial")
    nt.links.new(p.outputs[0], o.inputs["Surface"])
    return m


def gold(name, size=1.0, scratches=1.0, rough=0.34, color="#D8962E", haze=None):
    """painted-clay gold: metallic, soft roughness variation, tool marks and fine scratches"""
    def extra(B):
        tc = B.node("ShaderNodeTexCoord").outputs["Object"]
        p = B.vmath("SCALE", tc, scale=1.0 / size)
        sc_ = B.node("ShaderNodeTexNoise")
        B.set(sc_.inputs["Vector"], B.vmath("MULTIPLY", p, (60.0, 1.5, 60.0)))
        sc_.inputs["Scale"].default_value, sc_.inputs["Detail"].default_value = 3.0, 1.0
        lines = B.maprange(sc_.outputs["Fac"], 0.62, 0.66, 0.0, -0.25 * scratches)
        return lines
    return clay(name, color, rough=rough, sss=0.0, metallic=0.85, size=size, thumbs=0.5, tool=0.8, grain=0.6, bump=0.3,
                dust=0.35, dust_hex="#5A3410", ao_dist=0.05 * size, sheen=0.0, spec=0.5, extra_height=extra, haze=haze)
