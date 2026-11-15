# Cadet idle loop (32 frames, 8 fps, seamless): two breaths, one weight shift, one small head turn toward the right and back.
# Frame 0 is exactly the rest pose used by the stills.
import bpy, math

N_FRAMES = 32
_REST = {}
RIG = ["Body", "Head", "ShoulderR", "ShoulderL", "Torso"]


def _store():
    if _REST:
        return
    for n in RIG:
        o = bpy.data.objects[n]
        _REST[n] = (o.location.copy(), o.rotation_euler.copy(), o.scale.copy())


def curves(f, n=N_FRAMES):
    t = (f % n) / n
    breath = 0.5 - 0.5 * math.cos(2 * math.pi * 2 * t)          # 0..1, two breaths per loop
    shift = math.sin(2 * math.pi * t)                              # one weight shift left/right
    turn = math.sin(math.pi * (t - 0.35) / 0.5) ** 2 if 0.35 <= t <= 0.85 else 0.0   # glance right and back
    return breath, shift, turn


def pose(f, n=N_FRAMES):
    """Set the cadet rig for frame f (local offsets on top of the rest pose)."""
    _store()
    b, w, h = curves(f, n)
    rad = math.radians
    for nme, (loc, rot, scl) in _REST.items():
        o = bpy.data.objects[nme]
        o.location, o.rotation_euler, o.scale = loc.copy(), rot.copy(), scl.copy()
    body = bpy.data.objects["Body"]
    body.location.z += 0.0065 * b
    body.location.x += 0.012 * w
    body.rotation_euler.y += rad(1.3) * w
    body.rotation_euler.z += rad(-4.0) * h
    head = bpy.data.objects["Head"]
    head.location.z += 0.003 * b
    head.rotation_euler.x += rad(-0.8) * b
    head.rotation_euler.y += rad(-0.9) * w
    head.rotation_euler.z += rad(-11.0) * h
    for side, sgn in (("ShoulderR", 1), ("ShoulderL", -1)):
        s = bpy.data.objects[side]
        s.location.z += 0.0045 * b
        s.rotation_euler.y += rad(1.2) * sgn * b
    torso = bpy.data.objects["Torso"]
    torso.scale.x *= 1.0 + 0.014 * b
    torso.scale.y *= 1.0 + 0.018 * b
    bpy.context.view_layer.update()


def rest():
    pose(0)


def keyframe_all(scene, n=N_FRAMES):
    """Bake the loop as keyframes (frames 1..n+1) so the .blend carries the animation too."""
    for f in range(n + 1):
        pose(f % n, n)
        for nme in RIG:
            o = bpy.data.objects[nme]
            for path in ("location", "rotation_euler", "scale"):
                o.keyframe_insert(data_path=path, frame=f + 1)
    scene.frame_start, scene.frame_end = 1, n
    scene.render.fps = 8
    rest()
