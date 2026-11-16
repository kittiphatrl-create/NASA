"""Syncs the Fish Audio voice files onto the three Discord screen recordings.

Run with a Python that has Pillow:  python production/video/sync_discord.py
Writes production/video/out/s1_discord.mp4, s2_roles.mp4, s8_outro.mp4 (1920x1080, 30 fps, AAC).

Timing notes (measured):
- Scene01.mp4: P1 alone until Frame's tile appears at 12.94 s.
- Sound1.mp3: "Bro. Bro!" starts at 6.70 s, so Sound1 is delayed to start 0.4 s after the join.
- scene02.mp4 (70.4 s) carries Sound2 + Sound3 + Sound4 (70.6 s) back to back.
- scene03.mp4 carries Sound11 (outro); a tile leaves near 24.6 s, after "We're Overtime PW!".
"""
import json, os, pathlib, shutil, subprocess
from PIL import Image, ImageDraw, ImageFont

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[1]
OUT = HERE / "out"
OVL = HERE / "overlays"
OUT.mkdir(exist_ok=True)
OVL.mkdir(exist_ok=True)
HOME = pathlib.Path(os.environ["USERPROFILE"])
CAP = HOME / "Videos" / "Captures"
SND = HOME / "Downloads"
UI = ROOT / "Assets" / "Resources" / "Audio" / "KenneyUI"
FONT_B = str(ROOT / "Assets" / "Resources" / "Fonts" / "ChakraPetch-Bold.ttf")
FONT_R = str(ROOT / "Assets" / "Resources" / "Fonts" / "ChakraPetch-Regular.ttf")


def find_ffmpeg():
    p = shutil.which("ffmpeg")
    if p:
        return p
    base = HOME / "AppData" / "Local" / "Microsoft" / "WinGet" / "Packages"
    return str(next(base.rglob("ffmpeg.exe")))


FF = find_ffmpeg()
team = json.loads((HERE / "team.json").read_text(encoding="utf-8"))
M = {m["key"]: m for m in team["members"]}

CYAN, CREAM, PANEL, ORANGE = (138, 224, 207), (255, 242, 214), (11, 22, 21, 235), (232, 154, 58)
VIDEO = "fps=30,scale=1920:1080:force_original_aspect_ratio=decrease,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,setsar=1,format=yuv420p"


def photo(m, height):
    im = Image.open(ROOT / m["photo"]).convert("RGBA")
    return im.resize((int(im.width * height / im.height), height), Image.LANCZOS)


def scrim(width_frac, strength):
    """Dark gradient from the left edge so the Discord call stays visible behind the card."""
    g = Image.new("L", (1920, 1)); px = g.load()
    for x in range(1920):
        f = max(0.0, 1.0 - x / (1920 * width_frac))
        px[x, 0] = int(255 * strength * (f ** 0.8))
    a = g.resize((1920, 1080))
    s = Image.new("RGBA", (1920, 1080), (8, 16, 15, 255)); s.putalpha(a)
    return s


def card(name, key):
    """One member: big cut-out photo, nickname, role and full name, over a soft dark scrim."""
    m = M[key]
    im = Image.new("RGBA", (1920, 1080), (8, 16, 15, 200))
    im.alpha_composite(scrim(0.7, 0.6))
    d = ImageDraw.Draw(im)
    p = photo(m, 720)
    im.alpha_composite(p, (max(40, 470 - p.width // 2), 1080 - p.height))
    x = 900
    d.rectangle((x, 430, x + 10, 640), fill=ORANGE)
    d.text((x + 40, 470), m["nick"], font=ImageFont.truetype(FONT_B, 150), fill=CREAM, anchor="ls")
    d.text((x + 44, 540), m["role"].upper(), font=ImageFont.truetype(FONT_B, 50), fill=CYAN, anchor="ls")
    d.text((x + 44, 610), m["full"], font=ImageFont.truetype(FONT_R, 40), fill=CREAM, anchor="ls")
    out = OVL / f"{name}.png"
    im.save(out)
    return out


def lineup():
    """The whole team: a dim layer with the title, plus one layer per member (animated in turn)."""
    bg = Image.new("RGBA", (1920, 1080), (8, 16, 15, 215))
    d = ImageDraw.Draw(bg)
    d.text((960, 120), f"TEAM {team['team'].upper()}", font=ImageFont.truetype(FONT_B, 84), fill=CREAM, anchor="mm")
    d.rectangle((960 - 260, 178, 960 + 260, 183), fill=ORANGE)
    layers = [(OVL / "lineup_bg.png")]
    bg.save(layers[0])
    slot = 1920 // 5
    for i, k in enumerate(["p1", "p2", "p3", "p4", "p5"]):
        m = M[k]
        im = Image.new("RGBA", (1920, 1080), (0, 0, 0, 0))
        dd = ImageDraw.Draw(im)
        p = photo(m, 560)
        if p.width > slot - 20:
            p = p.resize((slot - 20, int(p.height * (slot - 20) / p.width)), Image.LANCZOS)
        cx = slot * i + slot // 2
        im.alpha_composite(p, (cx - p.width // 2, 820 - p.height))
        dd.rectangle((cx - 150, 830, cx + 150, 834), fill=CYAN)
        dd.text((cx, 885), m["nick"], font=ImageFont.truetype(FONT_B, 56), fill=CREAM, anchor="mm")
        dd.text((cx, 940), m["role"].upper(), font=ImageFont.truetype(FONT_B, 24), fill=CYAN, anchor="mm")
        dd.text((cx, 980), m["full"], font=ImageFont.truetype(FONT_R, 24), fill=CREAM, anchor="mm")
        out = OVL / f"lineup_{k}.png"
        im.save(out)
        layers.append(out)
    return layers


def run(cmd):
    r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode:
        print(r.stderr[-3000:])
        raise SystemExit(f"ffmpeg failed: {cmd[-1]}")


def build(out, video, v_end, total, voices, sfx, overlays=(), loop=None, fade_out=False):
    """video: path; v_end: seconds of video to use; total: output length.
    voices/sfx: [(path, start_s, volume)]; overlays: [(png, t_in, t_out)];
    loop: (a, b) seconds of the video to repeat after v_end instead of freezing the last frame."""
    cmd = [FF, "-v", "error", "-y", "-i", str(video)]
    fl = []
    if loop:
        a, b = loop
        reps = int((total - v_end) / (b - a)) + 2
        parts = [f"[0:v]trim=0:{v_end},setpts=PTS-STARTPTS[v0]"]
        names = ["[v0]"]
        fl.append(f"[0:v]split={reps + 1}" + "".join(f"[s{i}]" for i in range(reps + 1)))
        parts = [f"[s0]trim=0:{v_end},setpts=PTS-STARTPTS[v0]"]
        for i in range(1, reps + 1):
            parts.append(f"[s{i}]trim={a}:{b},setpts=PTS-STARTPTS[v{i}]")
            names.append(f"[v{i}]")
        fl += parts
        fl.append("".join(names) + f"concat=n={len(names)}:v=1:a=0,trim=0:{total},setpts=PTS-STARTPTS,{VIDEO}[vb]")
    else:
        pad = max(0.0, total - v_end)
        fl.append(f"[0:v]trim=0:{v_end},setpts=PTS-STARTPTS,tpad=stop_mode=clone:stop_duration={pad:.3f},{VIDEO}[vb]")
    last = "[vb]"
    idx = 1
    for ov in overlays:
        png, t_in, t_out = ov[:3]
        dy = ov[3] if len(ov) > 3 else 140          # slide up from dy pixels below, ease-out cubic
        cmd += ["-loop", "1", "-framerate", "30", "-t", f"{total:.3f}", "-i", str(png)]
        fl.append(f"[{idx}:v]format=rgba,fade=in:st={t_in}:d=0.35:alpha=1,fade=out:st={t_out - 0.3}:d=0.3:alpha=1[o{idx}]")
        y = f"{dy}*pow(1-min(1\\,max(0\\,(t-{t_in})/0.5))\\,3)"
        fl.append(f"{last}[o{idx}]overlay=x=0:y='{y}':eval=frame:enable='between(t,{t_in},{t_out})'[vo{idx}]")
        last = f"[vo{idx}]"
        idx += 1
    if fade_out:
        fl.append(f"{last}fade=out:st={total - 1.0:.3f}:d=1.0[vf]")
        last = "[vf]"
    amix = []
    for path, start, vol in list(voices) + list(sfx):
        cmd += ["-i", str(path)]
        ms = int(start * 1000)
        fl.append(f"[{idx}:a]aresample=48000,aformat=channel_layouts=stereo,volume={vol},adelay={ms}|{ms}[a{idx}]")
        amix.append(f"[a{idx}]")
        idx += 1
    fl.append("".join(amix) + f"amix=inputs={len(amix)}:normalize=0:duration=longest,apad,atrim=0:{total:.3f}[aout]")
    cmd += ["-filter_complex", ";".join(fl), "-map", last, "-map", "[aout]", "-t", f"{total:.3f}",
            "-c:v", "libx264", "-preset", "medium", "-crf", "18", "-c:a", "aac", "-b:a", "192k", str(out)]
    run(cmd)
    print("wrote", out.name, f"{total:.2f}s")


ping, pop = UI / "good.ogg", UI / "open.ogg"

# ---------- Scene 1: Discord, P1 alone -> Frame joins ----------
JOIN = 12.94
d1 = JOIN - 6.70 + 0.4
s1_total = d1 + 32.47 + 0.6
build(OUT / "s1_discord.mp4", CAP / "Scene01.mp4", 19.39, s1_total,
      voices=[(SND / "Sound1.mp3", d1, 1.0)],
      sfx=[(ping, JOIN - 0.05, 0.6)],
      loop=(13.0, 19.39))

# ---------- Scene 2: everyone joins, the lead hands out jobs ----------
lead = 0.6
t2 = lead
t3 = t2 + 23.28
t4 = t3 + 27.19
s2_total = t4 + 20.17 + 0.4
cards = [
    (card("card_p2", "p2"), t3 + 4.2, t3 + 13.5),
    (card("card_p3", "p3"), t3 + 13.72, t3 + 21.0),
    (card("card_p4", "p4"), t3 + 21.12, t4 - 0.05),
    (card("card_p5", "p5"), t4 + 0.0, t4 + 7.8),
    (card("card_p1", "p1"), t4 + 7.92, t4 + 11.0),
]
lay = lineup()
t_all = t4 + 11.1
cards += [(lay[0], t_all, s2_total, 0)] + [(p, t_all + 0.25 + 0.15 * i, s2_total, 220) for i, p in enumerate(lay[1:])]
build(OUT / "s2_roles.mp4", CAP / "scene02.mp4", 70.4, s2_total,
      voices=[(SND / "Sound2.mp3", t2, 1.0), (SND / "Sound3.mp3", t3, 1.0), (SND / "Sound4.mp3", t4, 1.0)],
      sfx=[(ping, 0.05, 0.6), (ping, t2 + 3.12 - 0.45, 0.6), (ping, t2 + 5.98 - 0.45, 0.6)]
          + [(pop, c[1], 0.5) for c in cards[:5]] + [(pop, t_all + 0.25, 0.5)],
      overlays=cards)

# ---------- Scene 8: outro ----------
s8_total = 0.3 + 30.30 + 1.5
build(OUT / "s8_outro.mp4", CAP / "scene03.mp4", 26.45, s8_total,
      voices=[(SND / "Sound11.mp3", 0.3, 1.0)], sfx=[], fade_out=True)
