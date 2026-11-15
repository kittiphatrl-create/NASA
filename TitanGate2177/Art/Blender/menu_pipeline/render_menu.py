# Renders menu layers from mars_keyart_menu.blend.
# blender -b mars_keyart_menu.blend --python render_menu.py -- <pass> [preview] [out=<dir>]
#   passes: full, bg, bg_shadow, cadet, cadet_look
import bpy, sys, os, time, math

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
passes = [a for a in argv if "=" not in a and a != "preview"]
preview = "preview" in argv
opts = dict(a.split("=", 1) for a in argv if "=" in a)
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))   # project root
OUT = opts.get("out", os.path.join(ROOT, r"Art\Blender\renders\menu"))
os.makedirs(OUT, exist_ok=True)
sc = bpy.context.scene
D = bpy.data
vl = sc.view_layers[0]
cadet_objs = [o for o in D.collections["cadet"].all_objects]
OTHER_COLS = ["terrain", "base", "lifeboat", "rover"]

if preview:
    sc.render.resolution_x, sc.render.resolution_y = 1280, 720
    sc.eevee.taa_render_samples = int(opts.get("samples", 24))
else:
    sc.render.resolution_x, sc.render.resolution_y = 2560, 1440
    sc.eevee.taa_render_samples = int(opts.get("samples", 96))
sc.render.resolution_percentage = 100
if "border" in opts:   # x0,x1,y0,y1 in top-down normalized coords
    x0, x1, y0, y1 = (float(v) for v in opts["border"].split(","))
    sc.render.use_border = True
    sc.render.use_crop_to_border = True
    sc.render.border_min_x, sc.render.border_max_x = x0, x1
    sc.render.border_min_y, sc.render.border_max_y = 1 - y1, 1 - y0
if "exec" in opts:
    exec(open(opts["exec"]).read())


def find_lc(lc, name):
    if lc.name == name:
        return lc
    for c in lc.children:
        r = find_lc(c, name)
        if r:
            return r


def setup(p):
    head = D.objects["Head"]
    body = D.objects["Body"]
    look = p in ("cadet_look", "full_look")
    # look pose: 15 deg total toward the base (right) = 4 deg upper body + 11 deg neck
    body.rotation_euler[2] = math.radians(float(opts.get("body", -4.0))) if look else 0.0
    head.rotation_euler[2] = math.radians(float(opts.get("look", -11.0))) if look else 0.0
    sc.render.film_transparent = p in ("cadet", "cadet_look")
    if p == "full_look":
        p = "full"
    def hold(lc, v):
        lc.holdout = v
        for ch in lc.children:
            hold(ch, v)
    for n in OTHER_COLS:
        hold(find_lc(vl.layer_collection, n), p in ("cadet", "cadet_look"))
    for o in cadet_objs:
        if p == "bg":
            o.visible_camera = False
            o.visible_shadow = False
        elif p == "bg_shadow":
            o.visible_camera = False
            o.visible_shadow = True
        else:
            o.visible_camera = True
            o.visible_shadow = True
    s = sc.render.image_settings
    s.file_format = "PNG"
    s.color_mode = "RGBA" if sc.render.film_transparent else "RGB"
    s.color_depth = "8"
    s.compression = 90


times = {}
for p in passes:
    setup(p)
    sc.render.filepath = os.path.join(OUT, ("prev_" if preview else "") + opts.get("prefix", "") + p + ".png")
    t0 = time.time()
    bpy.ops.render.render(write_still=True)
    times[p] = round(time.time() - t0, 1)
    print("RENDERED", p, sc.render.filepath, times[p], "s")
print("TIMES", times)
