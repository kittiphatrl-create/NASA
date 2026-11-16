# Renders a short animated camera move from an existing scene, without saving the .blend.
# blender -b <file.blend> --python blender_shot.py -- <mode> <out_prefix> <frames>
# mode "orbit": circle the camera around the world origin (Kestrel in story_space.blend), debris drifts past.
# mode "push": slow push-in along the camera's view with a slight sideways slide (Mars keyart scenes).
import bpy, sys, math
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
mode, out, frames = argv[0], argv[1], int(argv[2])

s = bpy.context.scene
s.render.resolution_x, s.render.resolution_y, s.render.resolution_percentage = 1920, 1080, 100
s.render.fps = 30
s.frame_start, s.frame_end = 1, frames
s.render.image_settings.file_format = "PNG"
s.render.filepath = out
cam = s.camera

def ease(t):
    return t * t * (3 - 2 * t)

if mode == "orbit":
    start = cam.location.copy()
    r0 = math.hypot(start.x, start.y)
    a0 = math.atan2(start.y, start.x)
    debris = [o for o in bpy.data.objects if o.name.startswith("debris")]
    base = {o.name: o.location.copy() for o in debris}
    for f in range(1, frames + 1):
        t = ease((f - 1) / max(1, frames - 1))
        a = a0 + math.radians(-28) * t
        r = r0 * (1.0 - 0.18 * t)
        cam.location = Vector((r * math.cos(a), r * math.sin(a), start.z + 1.2 * t))
        d = Vector((0, 0, 0)) - cam.location
        cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
        cam.keyframe_insert("location", frame=f)
        cam.keyframe_insert("rotation_euler", frame=f)
        for o in debris:
            o.location = base[o.name] + Vector((6.0, -1.5, 0.6)) * ((f - 1) / frames)
            o.rotation_euler.x += 0.03
            o.rotation_euler.z += 0.02
            o.keyframe_insert("location", frame=f)
            o.keyframe_insert("rotation_euler", frame=f)
elif mode == "impact":
    # the biggest debris rock flies into the hull at 55 % of the shot; the camera shakes after the hit
    import random
    random.seed(7)
    debris = sorted([o for o in bpy.data.objects if o.name.startswith("debris")],
                    key=lambda o: -max(o.dimensions))
    # a new, clearly visible rock (the scene's own debris is tiny at this distance)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.85)
    rock = bpy.context.active_object
    rock.scale = (1.0, 0.75, 0.6)
    for v in rock.data.vertices:
        v.co *= 0.8 + 0.4 * random.random()
    mat = bpy.data.materials.new("rock")
    mat.diffuse_color = (0.32, 0.29, 0.27, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.32, 0.29, 0.27, 1)
        bsdf.inputs["Roughness"].default_value = 0.9
    rock.data.materials.append(mat)
    target = Vector((-1.0, 1.35, 0.5))
    start_pos = target + Vector((7.0, 3.0, 5.5))
    hit = int(frames * 0.55)
    cam_start = cam.location.copy()
    cam_end = cam_start + (Vector((0, 0, 0)) - cam_start) * 0.25
    for f in range(1, frames + 1):
        t = (f - 1) / max(1, frames - 1)
        p = min(1.0, (f - 1) / max(1, hit - 1))
        rock.location = start_pos.lerp(target, p) if f <= hit else target + Vector((0.4, 2.5, 0.3)) * ((f - hit) / frames)
        rock.rotation_euler.x += 0.08
        rock.rotation_euler.y += 0.05
        rock.keyframe_insert("location", frame=f)
        rock.keyframe_insert("rotation_euler", frame=f)
        base = cam_start.lerp(cam_end, ease(t))
        shake = Vector((0, 0, 0))
        if f > hit:
            k = max(0.0, 1.0 - (f - hit) / 30.0) * 0.45
            shake = Vector((random.uniform(-k, k), random.uniform(-k, k), random.uniform(-k, k)))
        cam.location = base + shake
        cam.rotation_euler = (Vector((0, 0, 0)) - base).to_track_quat("-Z", "Y").to_euler()
        cam.keyframe_insert("location", frame=f)
        cam.keyframe_insert("rotation_euler", frame=f)
        for o in debris:
            o.rotation_euler.z += 0.02
            o.keyframe_insert("rotation_euler", frame=f)
else:
    start = cam.location.copy()
    fwd = cam.matrix_world.to_quaternion() @ Vector((0, 0, -1))
    side = cam.matrix_world.to_quaternion() @ Vector((1, 0, 0))
    dist = 0.10 * start.length if start.length > 0 else 2.0
    for f in range(1, frames + 1):
        t = ease((f - 1) / max(1, frames - 1))
        cam.location = start + fwd * dist * t + side * 0.04 * dist * t
        cam.keyframe_insert("location", frame=f)

bpy.ops.render.render(animation=True)
