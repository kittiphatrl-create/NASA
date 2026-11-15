# Quick Cycles preview of a v2 scene: blender -b <v2.blend> --python preview_v2.py -- out=<png> [res=1280] [samples=64] [border=x0,x1,y0,y1] [full=1]
import bpy, sys, os, time
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import clay as C
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
o = dict(a.split("=", 1) for a in argv if "=" in a)
sc = bpy.data.scenes[o.get("scene", "Scene")]
C.cycles_setup(sc, samples=int(o.get("samples", 64)))
for ob in sc.objects:
    if ob.type in ("MESH", "CURVE"):
        ob.visible_camera, ob.is_holdout = True, False
w = int(o.get("res", 1280))
sc.render.resolution_x, sc.render.resolution_y = w, w * 9 // 16
sc.render.resolution_percentage = 100
if "border" in o:
    x0, x1, y0, y1 = (float(v) for v in o["border"].split(","))
    sc.render.use_border = sc.render.use_crop_to_border = True
    sc.render.border_min_x, sc.render.border_max_x, sc.render.border_min_y, sc.render.border_max_y = x0, x1, 1 - y1, 1 - y0
sc.render.image_settings.file_format = "PNG"
sc.render.image_settings.color_mode = "RGB"
sc.render.filepath = o["out"]
t = time.time()
bpy.ops.render.render(write_still=True, scene=sc.name)
print("PREVIEW", round(time.time() - t, 1), "s")
