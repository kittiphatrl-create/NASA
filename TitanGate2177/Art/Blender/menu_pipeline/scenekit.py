# Image IO / layer helpers for the layered menu scenes (numpy, top-down row order, straight alpha, sRGB-encoded).
import bpy, os, json, math, time
import numpy as np


def load(path):
    im = bpy.data.images.load(path, check_existing=False)
    w, h = im.size
    px = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, im.channels if im.channels in (3, 4) else 4)
    bpy.data.images.remove(im)
    if px.shape[2] == 3:
        px = np.concatenate([px, np.ones(px.shape[:2] + (1,), np.float32)], 2)
    return px[::-1].copy()


def save(px, path, fmt="PNG", quality=90):
    h, w = px.shape[:2]
    if px.shape[2] == 3:
        px = np.concatenate([px, np.ones((h, w, 1), np.float32)], 2)
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    im.pixels[:] = np.clip(px[::-1], 0, 1).ravel()
    if fmt == "JPEG":
        im.file_format = "JPEG"
        im.save(filepath=path, quality=quality)
    else:
        im.filepath_raw = path
        im.file_format = "PNG"
        im.save()
    bpy.data.images.remove(im)


def alpha_bbox(px, thr=1.0 / 255, margin=4):
    ys, xs = np.nonzero(px[..., 3] > thr)
    h, w = px.shape[:2]
    if len(xs) == 0:
        return None
    return (max(int(xs.min()) - margin, 0), max(int(ys.min()) - margin, 0), min(int(xs.max()) + 1 + margin, w), min(int(ys.max()) + 1 + margin, h))


def over(dst, src, x0, y0):
    """straight-alpha 'over' of src onto dst (both top-down RGBA) at pixel (x0, y0); clips to dst."""
    h, w = src.shape[:2]
    H, W = dst.shape[:2]
    sx0, sy0 = max(0, -x0), max(0, -y0)
    dx0, dy0 = max(0, x0), max(0, y0)
    dx1, dy1 = min(W, x0 + w), min(H, y0 + h)
    if dx1 <= dx0 or dy1 <= dy0:
        return dst
    s = src[sy0:sy0 + dy1 - dy0, sx0:sx0 + dx1 - dx0]
    d = dst[dy0:dy1, dx0:dx1]
    sa = s[..., 3:4]
    da = d[..., 3:4]
    oa = sa + da * (1 - sa)
    oc = (s[..., :3] * sa + d[..., :3] * da * (1 - sa)) / np.maximum(oa, 1e-6)
    dst[dy0:dy1, dx0:dx1] = np.concatenate([oc, oa], 2)
    return dst


def resize(px, w, h):
    """bilinear resize on premultiplied colour"""
    H, W = px.shape[:2]
    pm = px.copy()
    pm[..., :3] *= pm[..., 3:4]
    ys = (np.arange(h) + 0.5) * H / h - 0.5
    xs = (np.arange(w) + 0.5) * W / w - 0.5
    y0 = np.clip(np.floor(ys).astype(int), 0, H - 1); y1 = np.clip(y0 + 1, 0, H - 1); fy = np.clip(ys - y0, 0, 1)[:, None, None]
    x0 = np.clip(np.floor(xs).astype(int), 0, W - 1); x1 = np.clip(x0 + 1, 0, W - 1); fx = np.clip(xs - x0, 0, 1)[None, :, None]
    a = pm[y0][:, x0] * (1 - fx) + pm[y0][:, x1] * fx
    b = pm[y1][:, x0] * (1 - fx) + pm[y1][:, x1] * fx
    o = a * (1 - fy) + b * fy
    o[..., :3] /= np.maximum(o[..., 3:4], 1e-6)
    return o


def gblur(a, sigma):
    r = max(1, int(sigma * 3))
    k = np.exp(-0.5 * (np.arange(-r, r + 1) / sigma) ** 2)
    k /= k.sum()
    p = np.pad(a, r, mode="edge")
    p = np.apply_along_axis(lambda m: np.convolve(m, k, mode="valid"), 0, p)
    return np.apply_along_axis(lambda m: np.convolve(m, k, mode="valid"), 1, p)


def to_srgb(c):
    c = np.asarray(c, np.float32)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(np.maximum(c, 0), 1 / 2.4) - 0.055)


def render(scene, path, w=None, h=None, border=None):
    """border = integer pixel rect (x0, y0, x1, y1) top-down at the given resolution"""
    r = scene.render
    if w:
        r.resolution_x, r.resolution_y = w, h
    r.resolution_percentage = 100
    if border:
        W, H = r.resolution_x, r.resolution_y
        x0, y0, x1, y1 = border
        r.use_border, r.use_crop_to_border = True, True
        r.border_min_x, r.border_max_x = (x0 + 0.25) / W, (x1 + 0.25) / W
        r.border_min_y, r.border_max_y = (H - y1 + 0.25) / H, (H - y0 + 0.25) / H
    else:
        r.use_border = False
    st = r.image_settings
    st.file_format, st.color_mode, st.color_depth, st.compression = "PNG", "RGBA", "8", 90
    r.filepath = path
    t0 = time.time()
    bpy.ops.render.render(write_still=True, scene=scene.name)
    r.use_border = False
    return time.time() - t0


def shadow_layer(base, base_sh):
    """premultiplied-free shadow layer (RGBA, straight) that turns `base` into `base_sh` when composited over it"""
    ok = base[..., 3] > 0.5
    ratio = np.where(base[..., :3] > 1.0 / 255, base_sh[..., :3] / np.maximum(base[..., :3], 1e-6), 1.0)
    a = np.clip(1.0 - ratio.min(axis=2), 0.0, 1.0) * ok
    a = np.where(a < 0.02, 0.0, a)
    S = np.clip(base_sh[..., :3] - base[..., :3] * (1.0 - a[..., None]), 0.0, 1.0)
    col = S / np.maximum(a[..., None], 1e-6)
    return np.concatenate([np.where(a[..., None] > 0, col, 0.0), a[..., None]], 2)


def norm_rect(px_rect, W, H):
    x0, y0, x1, y1 = px_rect
    return [round(x0 / W, 5), round(y0 / H, 5), round(x1 / W, 5), round(y1 / H, 5)]
