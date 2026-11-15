# LAST DOCK cutscene post: bloom, anamorphic light streaks, split-tone grade, chromatic fringe, vignette, film grain.
# Runs inside Blender (needs numpy):
#   exec(open(r"F:\NASA\TitanGate2177\Art\Blender\cinematics_post.py").read()); post_all()
# Reads Art/Blender/renders/cin/<shot>.png, writes Art/Blender/renders/cin/final/<shot>.png.
# The JPG copies the game loads (Assets/Resources/Sprites/KeyArt/<shot>.jpg.bytes) are made from those PNGs.
import bpy, numpy as np, os

RAW = r"F:\NASA\TitanGate2177\Art\Blender\renders\cin"
OUT = RAW + r"\final"

# per shot: shadow tint, highlight tint, bloom, streak, saturation, contrast, vignette, grain
LOOK = {
    'c01_cruise': dict(sh=(0.0, 0.03, 0.06), hi=(1.04, 1.0, 0.94), bloom=0.55, streak=0.7, sat=1.05, con=1.12),
    'c02_mars':   dict(sh=(0.01, 0.02, 0.05), hi=(1.06, 0.99, 0.92), bloom=0.55, streak=0.6, sat=1.1, con=1.12),
    'c03_cadet':  dict(sh=(0.0, 0.02, 0.06), hi=(1.08, 0.98, 0.88), bloom=0.6, streak=0.25, sat=1.05, con=1.1),
    'c04_debris': dict(sh=(0.03, 0.0, 0.06), hi=(1.06, 0.97, 1.0), bloom=0.6, streak=0.5, sat=1.15, con=1.15),
    'c05_impact': dict(sh=(0.02, 0.01, 0.03), hi=(1.08, 0.98, 0.86), bloom=0.9, streak=0.5, sat=1.1, con=1.15),
    'c06_alarm':  dict(sh=(0.03, 0.0, 0.01), hi=(1.06, 0.96, 0.92), bloom=0.8, streak=0.3, sat=1.08, con=1.12),
    'c07_launch': dict(sh=(0.0, 0.025, 0.05), hi=(1.08, 0.98, 0.88), bloom=0.75, streak=0.6, sat=1.1, con=1.15),
    'c08_entry':  dict(sh=(0.02, 0.01, 0.03), hi=(1.08, 0.97, 0.86), bloom=0.8, streak=0.4, sat=1.12, con=1.12),
    'c09_arrive': dict(sh=(0.03, 0.015, 0.02), hi=(1.05, 0.98, 0.9), bloom=0.22, streak=0.25, sat=1.05, con=1.18),
    'c10_rescue': dict(sh=(0.02, 0.015, 0.03), hi=(1.05, 0.99, 0.92), bloom=0.3, streak=0.45, sat=1.05, con=1.15),
    'c11_home':   dict(sh=(0.03, 0.015, 0.01), hi=(1.06, 0.99, 0.88), bloom=0.28, streak=0.25, sat=1.08, con=1.15),
    'c12_dusk':   dict(sh=(0.0, 0.015, 0.05), hi=(1.02, 1.0, 1.04), bloom=0.7, streak=0.45, sat=1.05, con=1.12),
    'c13_lone':   dict(sh=(0.0, 0.01, 0.05), hi=(1.0, 1.0, 1.06), bloom=0.75, streak=0.4, sat=1.0, con=1.1),
}

def _blur1(a, r, axis):
    a = np.moveaxis(a, axis, 0)
    p = np.concatenate([np.repeat(a[:1], r, 0), a, np.repeat(a[-1:], r, 0)], 0)
    c = np.cumsum(p, 0, dtype=np.float32)
    c = np.concatenate([np.zeros_like(c[:1]), c], 0)
    out = (c[2 * r + 1:] - c[:-2 * r - 1]) / (2 * r + 1)
    return np.moveaxis(out, 0, axis)

def blur(a, r, passes=3):
    for _ in range(passes): a = _blur1(_blur1(a, r, 0), r, 1)
    return a

def down(a, f):
    h, w = a.shape[0] // f * f, a.shape[1] // f * f
    return a[:h, :w].reshape(h // f, f, w // f, f, -1).mean((1, 3))

def up(a, f, shape):
    u = np.repeat(np.repeat(a, f, 0), f, 1)
    out = np.zeros(shape, np.float32); out[:u.shape[0], :u.shape[1]] = u[:shape[0], :shape[1]]
    return blur(out, f // 2 + 1, 1)

def load(path):
    im = bpy.data.images.load(path, check_existing=False)
    im.colorspace_settings.name = 'Non-Color'
    w, h = im.size
    a = np.empty(w * h * 4, np.float32); im.pixels.foreach_get(a)
    bpy.data.images.remove(im)
    return a.reshape(h, w, 4)[:, :, :3].copy()

def save(a, path):
    h, w, _ = a.shape
    im = bpy.data.images.new("post_out", w, h, alpha=False, float_buffer=False)
    im.colorspace_settings.name = 'Non-Color'
    rgba = np.ones((h, w, 4), np.float32); rgba[:, :, :3] = np.clip(a, 0, 1)
    im.pixels.foreach_set(rgba.ravel())
    im.filepath_raw = path; im.file_format = 'PNG'; im.save()
    bpy.data.images.remove(im)

def process(name, a, look):
    h, w, _ = a.shape
    lum = a @ np.array([0.2126, 0.7152, 0.0722], np.float32)
    # bloom: bright parts, blurred at two sizes
    th = max(0.72, float(np.percentile(lum, 97)))   # bright skies must not bloom as a whole
    br = a * (np.clip((lum - th) / max(0.05, 1.02 - th), 0, 1) ** 1.5)[:, :, None]
    s4 = down(br, 4)
    bloom = up(blur(s4, 3), 4, a.shape) * 0.6 + up(blur(s4, 14), 4, a.shape) * 0.9
    a = a + bloom * look['bloom']
    # anamorphic streak: only the hottest spots, stretched sideways, a little blue
    th2 = max(0.9, float(np.percentile(lum, 99.6)))
    hot = a * (np.clip((lum - th2) / max(0.03, 1.02 - th2), 0, 1) ** 2)[:, :, None]
    s8 = down(hot, 8)
    st = s8
    for _ in range(3): st = _blur1(st, 60, 1)
    st = _blur1(st, 1, 0)
    a = a + up(st, 8, a.shape) * np.array([0.75, 0.9, 1.25], np.float32) * look['streak'] * 3.0
    # split tone: tint shadows and highlights
    lum = a @ np.array([0.2126, 0.7152, 0.0722], np.float32)
    t = np.clip(lum, 0, 1)[:, :, None]
    a = a + np.array(look['sh'], np.float32) * (1 - t) ** 2
    a = a * (1 + (np.array(look['hi'], np.float32) - 1) * t * (1 - t) * 4)
    # saturation and an S-curve
    lum = (a @ np.array([0.2126, 0.7152, 0.0722], np.float32))[:, :, None]
    a = lum + (a - lum) * look['sat']
    a = np.clip(a, 0, 1)
    c = look['con']
    pivot = float(np.median(lum))
    a = pivot + (a - pivot) * c
    a = np.clip(a, 0, 1)
    a = a * a * (3 - 2 * a) * 0.25 + a * 0.75
    # chromatic fringe toward the corners
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    cx, cy = (w - 1) / 2, (h - 1) / 2
    dx, dy = (xx - cx) / cx, (yy - cy) / cy
    r2 = dx * dx + dy * dy
    k = 0.0025
    for ch, sgn in ((0, 1), (2, -1)):
        sx = np.clip((cx + (xx - cx) * (1 + sgn * k * r2)).round().astype(np.int32), 0, w - 1)
        sy = np.clip((cy + (yy - cy) * (1 + sgn * k * r2)).round().astype(np.int32), 0, h - 1)
        a[:, :, ch] = a[sy, sx, ch]
    # vignette and grain
    a = a * (1 - 0.38 * np.clip(r2 * 0.5, 0, 1) ** 1.3)[:, :, None]
    rng = np.random.default_rng(hash(name) & 0xffff)
    g = rng.normal(0, 1, (h, w)).astype(np.float32)
    g = (g + np.roll(g, 1, 0) + np.roll(g, 1, 1)) / 3
    a = a + g[:, :, None] * 0.012
    return np.clip(a, 0, 1)

def post(name):
    os.makedirs(OUT, exist_ok=True)
    a = load(RAW + "\\" + name + ".png")
    a = process(name, a, LOOK[name])
    save(a, OUT + "\\" + name + ".png")

def post_all(names=None):
    for n in LOOK:
        if (names is None or n in names) and os.path.exists(RAW + "\\" + n + ".png"):
            post(n)
