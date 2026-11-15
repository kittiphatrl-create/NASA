# Gold "LAST DOCK logo" metal + simple mesh builders shared by the docking-ring style pieces (window frame etc.).
import bpy, bmesh, math
from nb import NB, srgb

TAU = 2 * math.pi
LOGO_FRONT = [(0.0, "#6E2C0E"), (0.16, "#A8501C"), (0.42, "#F0A030"), (0.70, "#F9D58E"), (1.0, "#FFF4E0")]
LOGO_BEVEL = [(0.0, "#3C1606"), (0.5, "#C87A26"), (1.0, "#FFF8EC")]


def ramp_node(B, fac, stops):
    n = B.node("ShaderNodeValToRGB")
    B.set(n.inputs[0], fac)
    cr = n.color_ramp
    cr.interpolation = "EASE"
    cr.elements[0].position, cr.elements[0].color = stops[0][0], (*srgb(stops[0][1]), 1)
    cr.elements[1].position, cr.elements[1].color = stops[-1][0], (*srgb(stops[-1][1]), 1)
    for p, h in stops[1:-1]:
        e = cr.elements.new(p)
        e.color = (*srgb(h), 1)
    return n.outputs["Color"]


def metal(name, lo, hi, t_edge, t_center, center=(0.0, 0.0, 0.0), axis_mat=None, gain=1.0, light_mix=0.0):
    """Logo metal, radial about `center` in the plane given by axis_mat (world->local 3x3 as nested tuples; local z = ring axis).
    gain scales the result (to sit in a dim interior); light_mix blends in the scene's diffuse light (0 = pure logo look)."""
    m = bpy.data.materials.new(name)
    if not m.node_tree:
        m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    B = NB(nt)
    geo = B.node("ShaderNodeNewGeometry")
    P = B.vmath("SUBTRACT", geo.outputs["Position"], center)
    N = geo.outputs["Normal"]
    if axis_mat is not None:
        rows = [B.vec(r) for r in axis_mat]
        P = B.vec((0, 0, 0)) if False else _xform(B, P, rows)
        N = _xform(B, N, rows)
    nx, ny, nz = B.sep(N)
    pxy = B.vmath("MULTIPLY", P, (1.0, 1.0, 0.0))
    c = B.vmath("LENGTH", pxy)
    t = B.maprange(c, lo, hi, 0.0, 1.0)
    v = B.math("SUBTRACT", 1.0, B.math("ABSOLUTE", B.math("SUBTRACT", B.math("MULTIPLY", t, 2.0), 1.0)))
    vs = B.maprange(v, 0.0, 1.0, 0.0, 1.0, "SMOOTHSTEP")
    ft = B.math("ADD", t_edge, B.math("MULTIPLY", vs, t_center - t_edge))
    front_col = ramp_node(B, ft, LOGO_FRONT)
    nr = B.vmath("DOT_PRODUCT", B.vmath("NORMALIZE", pxy), N)
    front = B.maprange(nz, 0.82, 0.97, 0.0, 1.0, "SMOOTHSTEP")
    bu = B.math("ADD", B.math("ADD", 0.25, B.math("MULTIPLY", nz, 0.55)), B.math("MULTIPLY", nr, 0.25), clamp=True)
    bev = ramp_node(B, bu, LOGO_BEVEL)
    bev_col = B.mix(0.55, B.mix(0.35, bev, front_col, "MULTIPLY"), bev)
    col = B.mix(front, bev_col, front_col)
    if light_mix > 0:
        dif = B.node("ShaderNodeBsdfDiffuse")
        s2r = B.node("ShaderNodeShaderToRGB")
        nt.links.new(dif.outputs[0], s2r.inputs[0])
        lit = B.maprange(s2r.outputs["Color"], 0.0, 0.6, 1.0 - light_mix, 1.0 + light_mix * 0.6)
        col = B._scale_sock(col, lit)
    col = B._scale_sock(col, gain)
    em = B.node("ShaderNodeEmission")
    B.set(em.inputs["Color"], col)
    out = B.node("ShaderNodeOutputMaterial")
    nt.links.new(em.outputs[0], out.inputs["Surface"])
    return m


def _xform(B, vec, rows):
    x = B.vmath("DOT_PRODUCT", vec, rows[0])
    y = B.vmath("DOT_PRODUCT", vec, rows[1])
    z = B.vmath("DOT_PRODUCT", vec, rows[2])
    n = B.node("ShaderNodeCombineXYZ")
    B.set(n.inputs[0], x); B.set(n.inputs[1], y); B.set(n.inputs[2], z)
    return n.outputs[0]


def mesh_obj(coll, name, bm, mat, smooth=False):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = smooth
    if mat:
        me.materials.append(mat)
    o = bpy.data.objects.new(name, me)
    coll.objects.link(o)
    return o


def fillet(cx, cz, rad, a0, a1, n=8):
    return [(cx + rad * math.cos(a0 + (a1 - a0) * i / n), cz + rad * math.sin(a0 + (a1 - a0) * i / n)) for i in range(n + 1)]


def lathe(coll, name, prof, mat, segs=360):
    bm = bmesh.new()
    vs = [bm.verts.new((r, 0.0, z)) for r, z in prof]
    es = [bm.edges.new((vs[i], vs[i + 1])) for i in range(len(vs) - 1)]
    bmesh.ops.spin(bm, geom=vs + es, cent=(0, 0, 0), axis=(0, 0, 1), angle=TAU, steps=segs, use_merge=True)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
    bm.normal_update()
    top = max(bm.faces, key=lambda f: f.calc_center_median().z)
    if top.normal.z < 0:
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
    return mesh_obj(coll, name, bm, mat, smooth=True)


def box(coll, name, size, loc, rot, mat, bevel=0.0, segs=4, parent=None):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=size, verts=bm.verts)
    o = mesh_obj(coll, name, bm, mat)
    if parent:
        o.parent = parent
    o.location = loc
    o.rotation_euler = rot
    if bevel > 0:
        md = o.modifiers.new("bev", "BEVEL")
        md.width, md.segments, md.limit_method = bevel, segs, "NONE"
    return o


def bolt(coll, name, loc, r, mat, parent=None, flat=0.45):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=20, v_segments=10, radius=r)
    bmesh.ops.scale(bm, vec=(1, 1, flat), verts=bm.verts)
    o = mesh_obj(coll, name, bm, mat, smooth=True)
    if parent:
        o.parent = parent
    o.location = loc
    return o
