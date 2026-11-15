# Writes menu_points.json (normalized, origin top-left) for the menu camera.
# blender -b mars_keyart_menu.blend --python points.py -- <out.json>
import bpy, sys, json, math
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view

out_path = sys.argv[sys.argv.index("--") + 1]
sc = bpy.context.scene
cam = sc.camera
D = bpy.data
sc.render.resolution_x, sc.render.resolution_y = 2560, 1440
dg = bpy.context.evaluated_depsgraph_get()
W, H = sc.render.resolution_x, sc.render.resolution_y
CADET = {o.name for o in D.collections["cadet"].all_objects}


def scr(p):
    v = world_to_camera_view(sc, cam, Vector(p))
    return [round(v.x, 4), round(1.0 - v.y, 4)], v.z


def r4(v):
    return round(v, 4)


# --- camera rays through normalized top-down screen coords (handles the dutch roll)
fr = [cam.matrix_world @ c for c in cam.data.view_frame(scene=sc)]   # tr, br, bl, tl
tr, br, bl, tl = fr
cpos = cam.matrix_world.translation


def ray(u, v):
    p = tl + (tr - tl) * u + (bl - tl) * v
    return (p - cpos).normalized()


def first_hit(o, d, skip=()):
    for _ in range(16):
        hit, loc, nor, idx, ob, mt = sc.ray_cast(dg, o, d)
        if not hit:
            return None, None
        if ob.name in skip or ob.hide_render:
            o = loc + d * 0.01
            continue
        return ob, loc
    return None, None


# --- sun
sdir = Vector(sc["menu_sun_disc_dir"])
srad = sc["menu_sun_radius_rad"]
sun_xy, _ = scr(cpos + sdir * 1000.0)
perp = sdir.cross(Vector((0, 0, 1))).normalized()
edge_xy, _ = scr(cpos + (sdir * math.cos(srad) + perp * math.sin(srad)) * 1000.0)
r_h = math.hypot((edge_xy[0] - sun_xy[0]) * W, (edge_xy[1] - sun_xy[1]) * H) / H

# --- lights (windows, door lamps, beacon); drop any hidden behind scene geometry
beacons = []
names_seen = set()
for name, pos, kind in sc["menu_light_markers"]:
    p = Vector(pos)
    xy, depth = scr(p)
    d = (p - cpos)
    ob, loc = first_hit(cpos, d.normalized(), skip=CADET)
    visible = ob is not None and (loc - cpos).length >= d.length - 0.35
    if not (0 <= xy[0] <= 1 and 0 <= xy[1] <= 1) or not visible:
        print("skip light", name, xy, ob.name if ob else None)
        continue
    col = {"window": "#FF9A4A", "door": "#FFB066", "beacon": "#FF3A24"}[kind]
    beacons.append({"x": xy[0], "y": xy[1], "name": name, "kind": kind, "color": col,
                    "size": r4(0.006 if kind != "beacon" else 0.007), "dist_m": round(d.length, 1)})

# --- cadet bbox (all render-visible cadet meshes)
xs, ys = [], []
sole_pts = []
for o in D.collections["cadet"].all_objects:
    if o.type != "MESH" or o.hide_render:
        continue
    oe = o.evaluated_get(dg)
    me = oe.to_mesh()
    for vtx in me.vertices:
        w = o.matrix_world @ vtx.co
        xy, _ = scr(w)
        xs.append(xy[0]); ys.append(xy[1])
        if o.name in ("SoleL", "SoleR", "BootL", "BootR"):
            sole_pts.append(xy)
    oe.to_mesh_clear()
bbox = {"x0": r4(min(xs)), "y0": r4(min(ys)), "x1": r4(max(xs)), "y1": r4(max(ys))}
feet = {"x": r4(sum(p[0] for p in sole_pts) / len(sole_pts)), "y": r4(max(p[1] for p in sole_pts)),
        "w": r4(max(p[0] for p in sole_pts) - min(p[0] for p in sole_pts))}
head = D.objects["Head"].matrix_world.translation
head_xy, _ = scr(head + Vector((0, 0, 0.55)))

# --- horizon: top-most visible ground (in front of the mountains) per column, ignoring props
PROPS = {o.name for o in sc.objects if o.name not in ("ground", "ridge")}
horizon = []
for i in range(21):
    u = i / 20.0
    u_s = min(max(u, 0.0005), 0.9995)
    found = None
    v = 0.25
    while v < 0.95:
        ob, loc = first_hit(cpos, ray(u_s, v), skip=PROPS)
        if ob is not None and ob.name == "ground":
            lo, hi = v - 0.004, v
            for _ in range(12):
                mid = (lo + hi) / 2
                ob2, _ = first_hit(cpos, ray(u_s, mid), skip=PROPS)
                if ob2 is not None and ob2.name == "ground":
                    hi = mid
                else:
                    lo = mid
            ob3, loc3 = first_hit(cpos, ray(u_s, hi + 0.0005), skip=PROPS)
            found = (hi, (loc3 - cpos).length if loc3 else None)
            break
        v += 0.004
    if found:
        horizon.append([r4(u), r4(found[0]), round(found[1], 1) if found[1] else None])
        print("horizon", u, found)

# --- antenna tip
bc = D.objects["beacon"]
tip_xy, _ = scr(bc.matrix_world.translation + Vector((0, 0, 0.13)))
airlock = next((b for b in beacons if b["kind"] == "door"), None)

data = {
    "frame": {"width": W, "height": H, "origin": "top-left", "units": "normalized 0..1 of frame width (x) and height (y)",
              "camera": {"lens_mm": cam.data.lens, "hfov_deg": round(math.degrees(2 * math.atan(18.0 / cam.data.lens)), 2)}},
    "sun": {"x": sun_xy[0], "y": sun_xy[1], "radius": r4(r_h), "glow_radius": r4(r_h * 6.0),
            "radius_note": "disc radius as a fraction of frame height; glow_radius is the visible halo"},
    "sun_radius": r4(r_h),
    "beacons": beacons,
    "airlock": {"x": airlock["x"], "y": airlock["y"], "name": airlock["name"]} if airlock else None,
    "antenna_tip": {"x": tip_xy[0], "y": tip_xy[1]},
    "astronaut": bbox,
    "astronaut_feet": feet,
    "astronaut_head_top": {"x": head_xy[0], "y": head_xy[1]},
    "horizon": horizon,
    "horizon_note": "[x, y, distance_m]: far edge of the plain in front of the mountains; habitats and the cadet stand in front of it between x~0.45 and 1.0",
    "rover_scale_hint": {"length_at_horizon": r4(1.6 / 85.0 / (2 * 18.0 / cam.data.lens)),
                         "note": "a 1.6 m rover ~85 m out spans this fraction of frame width"},
}
with open(out_path, "w", encoding="utf-8") as f:
    json.dump(data, f, indent=1)
print("WROTE", out_path, len(beacons), "lights", len(horizon), "horizon pts")
