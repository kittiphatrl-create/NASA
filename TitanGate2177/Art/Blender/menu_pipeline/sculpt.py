# Procedural "sculpting" helpers: bake modifier stacks to real meshes, displace vertices with python functions,
# sweep tubes / straps along smooth paths, organic blobs. Coordinates passed to user functions are in a reference
# frame (e.g. the cadet's Root space) so folds and seams sit in the same place on every part.
import bpy, bmesh, math
from mathutils import Vector, Matrix, noise


def bake(obj, voxel=None, coords="LOCAL", keep_smooth=True):
    """apply the modifier stack (after optionally refining the voxel remesh and pinning displace textures to LOCAL)"""
    for m in obj.modifiers:
        if m.type == "REMESH" and voxel:
            m.voxel_size = voxel
        if m.type == "DISPLACE" and coords:
            m.texture_coords = coords
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(obj.evaluated_get(dg), preserve_all_data_layers=False, depsgraph=dg)
    old = obj.data
    mats = list(old.materials)
    obj.modifiers.clear()
    obj.data = me
    me.materials.clear()
    for mt in mats:
        me.materials.append(mt)
    for p in me.polygons:
        p.use_smooth = True
    return obj


def displace(obj, ref, fn, smooth_after=0):
    """fn(p_ref: Vector, n_ref: Vector) -> offset along the normal, in ref units"""
    me = obj.data
    M = ref.matrix_world.inverted() @ obj.matrix_world
    s = M.to_scale()[0]
    N = M.to_3x3().inverted().transposed()
    me.update()
    cos = [v.co.copy() for v in me.vertices]
    nos = [v.normal.copy() for v in me.vertices]
    for i, v in enumerate(me.vertices):
        p = M @ cos[i]
        n = (N @ nos[i]).normalized()
        d = fn(p, n)
        if d:
            v.co = cos[i] + nos[i] * (d / s)
    me.update()
    if smooth_after:
        bm = bmesh.new()
        bm.from_mesh(me)
        for _ in range(smooth_after):
            bmesh.ops.smooth_vert(bm, verts=bm.verts, factor=0.5, use_axis_x=True, use_axis_y=True, use_axis_z=True)
        bm.to_mesh(me)
        bm.free()
        me.update()


def gauss(x, w):
    return math.exp(-(x / w) ** 2)


def smoothstep(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def catmull(points, n=24):
    pts = [Vector(p) for p in points]
    out = []
    P = [pts[0] + (pts[0] - pts[1])] + pts + [pts[-1] + (pts[-1] - pts[-2])]
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        for k in range(n):
            t = k / n
            t2, t3 = t * t, t * t * t
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    out.append(pts[-1])
    return out


def _frames(path, up_hint):
    frames = []
    for i, p in enumerate(path):
        t = (path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)]).normalized()
        u = up_hint(p) if callable(up_hint) else Vector(up_hint)
        b = t.cross(u).normalized()
        if b.length < 1e-6:
            b = t.orthogonal().normalized()
        nrm = b.cross(t).normalized()
        frames.append((t, nrm, b))
    return frames


def tube(name, path, radius_fn, coll, mat=None, seg=16, up_hint=(0, 0, 1)):
    """radius_fn(t in 0..1, arclength) -> radius. Closed caps."""
    bm = bmesh.new()
    L = [0.0]
    for i in range(1, len(path)):
        L.append(L[-1] + (path[i] - path[i - 1]).length)
    fr = _frames(path, up_hint)
    rings = []
    for i, p in enumerate(path):
        t, nrm, b = fr[i]
        r = radius_fn(i / (len(path) - 1), L[i])
        rings.append([bm.verts.new(p + (nrm * math.cos(a) + b * math.sin(a)) * r) for a in [2 * math.pi * k / seg for k in range(seg)]])
    for i in range(len(rings) - 1):
        for k in range(seg):
            bm.faces.new((rings[i][k], rings[i][(k + 1) % seg], rings[i + 1][(k + 1) % seg], rings[i + 1][k]))
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    bm.normal_update()
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = True
    if mat:
        me.materials.append(mat)
    o = bpy.data.objects.new(name, me)
    coll.objects.link(o)
    return o


def strap(name, path, width, thick, coll, mat=None, up_hint=(0, 0, 1)):
    """flat strap whose broad side faces up_hint(p) (e.g. away from the body)"""
    bm = bmesh.new()
    fr = _frames(path, up_hint)
    rings = []
    for i, p in enumerate(path):
        t, nrm, b = fr[i]
        # nrm ~ up_hint: thickness along nrm, width along b
        rings.append([bm.verts.new(p + b * sx * width / 2 + nrm * sy * thick / 2) for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))])
    for i in range(len(rings) - 1):
        for k in range(4):
            bm.faces.new((rings[i][k], rings[i][(k + 1) % 4], rings[i + 1][(k + 1) % 4], rings[i + 1][k]))
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    bm.normal_update()
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    if mat:
        me.materials.append(mat)
    o = bpy.data.objects.new(name, me)
    coll.objects.link(o)
    sub = o.modifiers.new("sub", "SUBSURF")
    sub.levels = sub.render_levels = 2
    for p in me.polygons:
        p.use_smooth = True
    return o


def organic(name, build_bm, coll, mat=None, voxel=0.01, smooth=4, noise_amp=0.0, noise_scale=8.0, seed=0.0):
    """build_bm(bm) creates rough shapes; result is voxel-remeshed (merges parts), smoothed and given clay wobble"""
    bm = bmesh.new()
    build_bm(bm)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    coll.objects.link(o)
    r = o.modifiers.new("remesh", "REMESH")
    r.mode, r.voxel_size = "VOXEL", voxel
    sm = o.modifiers.new("smooth", "SMOOTH")
    sm.factor, sm.iterations = 0.6, smooth
    if mat:
        me.materials.append(mat)
    bake(o, coords=None)
    if noise_amp:
        me = o.data
        for v in me.vertices:
            v.co += v.normal * noise_amp * noise.noise(v.co * noise_scale + Vector((seed, seed * 0.7, seed * 1.3)))
        me.update()
    return o


def parent_keep(obj, parent):
    mw = obj.matrix_world.copy()
    obj.parent = parent
    obj.matrix_parent_inverse = parent.matrix_world.inverted()
    obj.matrix_world = mw
