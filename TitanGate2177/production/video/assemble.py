"""Assembles the full LAST DOCK presentation video.

Run from anywhere with a Python that has Pillow:   python production/video/assemble.py
Inputs:  out/s1_discord.mp4, out/s2_roles.mp4, out/s8_outro.mp4 (from sync_discord.py),
         footage/play_fixed.mp4 + footage/menulive.mp4 (recorded from the real build),
         blender/{kestrel,impact,mars}/f_####.png, gfx/slide0*.png, ~/Downloads/Sound5..10.mp3
Output:  out/LAST_DOCK_presentation.mp4
Every segment is rendered on its own (segs/), joined, then captions + the full audio mix are added.
"""
import json, os, pathlib, shutil, subprocess
from PIL import Image, ImageDraw, ImageFont
import captions

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[1]
HOME = pathlib.Path(os.environ["USERPROFILE"])
SND = HOME / "Downloads"
SEGS = HERE / "segs"
OVL = HERE / "overlays"
OUT = HERE / "out"
for d in (SEGS, OVL, OUT):
    d.mkdir(exist_ok=True)
FONTS = ROOT / "Assets" / "Resources" / "Fonts"
AUD = ROOT / "Assets" / "Resources" / "Audio"
TRANSCRIPTS = pathlib.Path(os.environ.get("LD_TRANSCRIPTS", HERE / "transcripts.json"))


def tool(name):
    p = shutil.which(name)
    if p:
        return p
    return str(next((HOME / "AppData" / "Local" / "Microsoft" / "WinGet" / "Packages").rglob(name + ".exe")))


FF, FP = tool("ffmpeg"), tool("ffprobe")


def run(cmd, cwd=None):
    r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace", cwd=cwd)
    if r.returncode:
        print(" ".join(map(str, cmd))[:600])
        print(r.stderr[-2500:])
        raise SystemExit("ffmpeg failed")


def dur(p):
    r = subprocess.run([FP, "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(p)],
                       capture_output=True, text=True)
    return float(r.stdout.strip())


# ---------------------------------------------------------------- overlays (true alpha, game UI style)
CYAN, CREAM, PANEL, ORANGE = (138, 224, 207), (255, 242, 214), (11, 22, 21, 230), (232, 154, 58)
FB = str(FONTS / "ChakraPetch-Bold.ttf")
FR = str(FONTS / "ChakraPetch-Regular.ttf")


def ovl_bar(name, text):
    im = Image.new("RGBA", (1920, 1080), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    f = ImageFont.truetype(FB, 50)
    w = d.textlength(text, font=f) + 90
    x0 = (1920 - w) / 2
    d.rounded_rectangle((x0, 48, x0 + w, 138), 18, fill=PANEL, outline=CYAN, width=4)
    d.rectangle((x0 + 22, 72, x0 + 30, 114), fill=ORANGE)
    d.text((960 + 12, 93), text, font=f, fill=CREAM, anchor="mm")
    p = OVL / f"{name}.png"
    im.save(p)
    return p


def ovl_tag(name, text):
    im = Image.new("RGBA", (1920, 1080), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    f = ImageFont.truetype(FR, 26)
    w = d.textlength(text, font=f) + 44
    d.rounded_rectangle((40, 980, 40 + w, 1030), 10, fill=(11, 22, 21, 190), outline=(138, 224, 207, 160), width=2)
    d.ellipse((56, 998, 70, 1012), fill=(232, 80, 60))
    d.text((80, 1005), text, font=f, fill=CREAM, anchor="lm")
    p = OVL / f"{name}.png"
    im.save(p)
    return p


def ovl_title(name):
    im = Image.new("RGBA", (1920, 1080), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    big, sub = ImageFont.truetype(FB, 170), ImageFont.truetype(FR, 54)
    d.text((960, 470), "LAST DOCK", font=big, fill=CREAM, anchor="mm")
    d.rectangle((960 - 380, 572, 960 + 380, 578), fill=ORANGE)
    d.text((960, 640), "Junior Astronaut Mission Trainer", font=sub, fill=CYAN, anchor="mm")
    p = OVL / f"{name}.png"
    im.save(p)
    return p


def ovl_callout(name, box, label):
    """Highlight rectangle (x, y, w, h) with a small label, to point at a UI element."""
    im = Image.new("RGBA", (1920, 1080), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    x, y, w, h = box
    d.rounded_rectangle((x, y, x + w, y + h), 14, outline=ORANGE, width=6)
    f = ImageFont.truetype(FB, 34)
    tw = d.textlength(label, font=f) + 36
    ly = y - 62 if y > 80 else y + h + 12
    d.rounded_rectangle((x, ly, x + tw, ly + 50), 10, fill=PANEL, outline=ORANGE, width=3)
    d.text((x + 18, ly + 25), label, font=f, fill=CREAM, anchor="lm")
    p = OVL / f"{name}.png"
    im.save(p)
    return p


TAG = ovl_tag("tag_ingame", "IN-GAME FOOTAGE · real build")
TITLE = ovl_title("title")
BAR_4A = ovl_bar("bar_4a", "60 seconds · carry 3 · pod holds 8")
BAR_4B = ovl_bar("bar_4b", "7 days · 3 work units a day")
BAR_4C = ovl_bar("bar_4c", "More O₂ ≠ less CO₂")
BAR_STORM = ovl_bar("bar_storm", "Solar storm → shelter needs power")
BAR_CARD = ovl_bar("bar_card", "Every problem ends with a real NASA example")
BAR_DIR = ovl_bar("bar_dir", "Seeded incident director · fair, never repeats")
BAR_3D = ovl_bar("bar_3d", "Live 3D menu · drag to turn the ship")
BAR_TH = ovl_bar("bar_th", "Thai + English · shaped Thai text")
BAR_DIN = ovl_bar("bar_din", "Dinner: meal · mystery box · skip")

# ---------------------------------------------------------------- segments
FOOT = HERE / "footage" / "play_fixed.mp4"
MENU = HERE / "footage" / "menulive_fixed.mp4"
BL = HERE / "blender"
GFX = HERE / "gfx"
EV = ROOT / "production" / "qa" / "evidence"

timeline = []   # (kind, ...) in order
voices = []     # (file, global start, volume, cut)
sfx = []        # (file, global start, volume)
music = []      # (file, start, end, volume)
cap_place = []  # (name, global start, cut)


def seg(kind, src, a=0.0, b=None, dur_=None, zoom=0.0, shake=False, flash=None, glitch=False,
        fade_in=False, fade_out=False, overlays=(), tag=False, pan=None, letterbox=False):
    if pan is None:   # alternate the camera drift so consecutive shots do not move the same way
        pan = [-1, 1, 0][len(timeline) % 3]
    timeline.append(dict(kind=kind, src=src, a=a, b=b, dur=dur_ if dur_ else (b - a), zoom=zoom, shake=shake, pan=pan,
                         letterbox=letterbox or kind == "seq",
                         flash=flash, glitch=glitch, fade_in=fade_in, fade_out=fade_out,
                         overlays=list(overlays) + ([(TAG, 0, 999)] if tag else [])))


def cursor():
    return sum(s["dur"] for s in timeline)


# 1) Discord opening + roles (voices already mixed inside these files)
seg("disc", OUT / "s1_discord.mp4", 0, dur(OUT / "s1_discord.mp4"))
seg("disc", OUT / "s2_roles.mp4", 0, dur(OUT / "s2_roles.mp4"))
JOIN, D1 = 12.94, 12.94 - 6.70 + 0.4
cap_place += [("Sound1.mp3", D1, None)]
s2 = timeline[0]["dur"]
cap_place += [("Sound2.mp3", s2 + 0.6, None), ("Sound3.mp3", s2 + 0.6 + 23.28, None),
              ("Sound4.mp3", s2 + 0.6 + 23.28 + 27.19, None)]

# 2) Title: glitch out of Discord into the 3D Kestrel
t = cursor()
seg("seq", BL / "kestrel", 0, 6.0, glitch=True, zoom=0.06, pan=0, overlays=[(TITLE, 1.0, 5.6)])
sfx += [(AUD / "KenneySciFi" / "computerNoise_000.ogg", t, 0.5), (AUD / "KenneySciFi" / "forceField_000.ogg", t + 0.9, 0.6)]
music += [(AUD / "Music" / "transmission.mp3", t, t + 6.0, 0.35)]

# 3) Sound5 — the Kestrel, debris, 60-second scramble
t = cursor()
voices.append((SND / "Sound5.mp3", t + 0.3, 1.0, None)); cap_place.append(("Sound5.mp3", t + 0.3, None))
seg("clip", FOOT, 6.0, 10.5, zoom=0.05, tag=True)                       # "The year 2177", the Moon
seg("seq", BL / "impact", 0, 4.0, flash=2.2)                              # debris hits (3D)
sfx += [(AUD / "KenneySciFi" / "lowFrequency_explosion_000.ogg", t + 4.5 + 2.2, 0.9),
        (AUD / "KenneySciFi" / "impactMetal_000.ogg", t + 4.5 + 2.25, 0.6)]
seg("clip", FOOT, 45.0, 51.0, shake=True, tag=True)                       # IMPACT, red corridor "60 seconds"
seg("clip", FOOT, 54.0, 72.0, zoom=0.04, tag=True, overlays=[(BAR_4A, 1.0, 9.0)])   # scramble with timer
music += [(AUD / "Music" / "urgent.mp3", t, cursor(), 0.16)]

# 4) Sound6 — Mars, connected systems, CO2 lesson
t = cursor()
voices.append((SND / "Sound6.mp3", t, 1.0, 32.6)); cap_place.append(("Sound6.mp3", t, 32.6))
seg("seq", BL / "mars", 0, 3.0, fade_in=True)
seg("clip", FOOT, 99.0, 104.0, zoom=0.05, tag=True, overlays=[(BAR_4B, 0.2, 4.8)])
seg("clip", FOOT, 104.5, 110.5, zoom=0.06, tag=True)                      # Life Support panel
seg("clip", FOOT, 138.0, 147.0, zoom=0.05, tag=True, overlays=[(BAR_4C, 3.4, 8.8)])  # Day 3, O2 generator stopped
seg("clip", FOOT, 171.0, 176.0, zoom=0.07, tag=True)                      # NASA electrolysis card
seg("clip", FOOT, 252.0, 257.6, zoom=0.05, tag=True)                      # systems on/off
music += [(AUD / "Music" / "sector.mp3", t, None, 0.11)]                  # runs to the outro

# 5) Sound7 — dinner, rescue, "we learned it from NASA"
t = cursor()
voices.append((SND / "Sound7.mp3", t, 1.0, None)); cap_place.append(("Sound7.mp3", t, None))
seg("clip", FOOT, 124.0, 132.0, zoom=0.05, tag=True, overlays=[(BAR_DIN, 0.5, 7.5)])
seg("clip", FOOT, 179.0, 183.0, zoom=0.04, tag=True)
seg("clip", FOOT, 265.0, 274.0, zoom=0.04, tag=True)                      # rescue + LAST DOCK result
seg("still", GFX / "slide04.png", dur_=4.2, zoom=0.03, fade_in=True)

# 6) Sound8 — storm, Apollo 13, science cards
t = cursor()
voices.append((SND / "Sound8.mp3", t, 1.0, None)); cap_place.append(("Sound8.mp3", t, None))
seg("clip", FOOT, 175.0, 180.0, zoom=0.05, tag=True, overlays=[(BAR_STORM, 0.3, 4.7)])
seg("still", GFX / "slide04.png", dur_=5.0, zoom=0.03)
seg("clip", FOOT, 229.0, 235.0, zoom=0.07, tag=True, overlays=[(BAR_CARD, 0.3, 5.7)])
seg("clip", FOOT, 256.0, 261.0, zoom=0.07, tag=True)
seg("still", GFX / "slide04.png", dur_=4.2, zoom=0.03)

# 7) Sound9 — incident director, art, 3D menu, Thai text, AI disclosure
t = cursor()
voices.append((SND / "Sound9.mp3", t, 1.0, None)); cap_place.append(("Sound9.mp3", t, None))
seg("clip", FOOT, 219.0, 226.5, zoom=0.05, tag=True, overlays=[(BAR_DIR, 0.3, 7.2)])
seg("clip", FOOT, 20.0, 23.0, zoom=0.05, tag=True)                        # 3D cabin (Blender-made art)
seg("clip", FOOT, 30.0, 32.5, zoom=0.05, tag=True)                        # top-down pixel art
seg("clip", MENU, 5.0, 10.0, tag=True, overlays=[(BAR_3D, 0.3, 4.7)])
seg("still", EV / "operational-ui" / "th130-1280-life.png", dur_=5.0, zoom=0.06, overlays=[(BAR_TH, 0.3, 4.7)])
seg("still", GFX / "slide05.png", dur_=10.0, zoom=0.03, fade_in=True)

# 8) Sound10 — impact & next steps
t = cursor()
voices.append((SND / "Sound10.mp3", t, 1.0, None)); cap_place.append(("Sound10.mp3", t, None))
seg("still", GFX / "slide06.png", dur_=9.9, zoom=0.03)
seg("clip", FOOT, 84.0, 91.0, zoom=0.04, tag=True)                        # entry and landing on Mars
seg("still", GFX / "slide06.png", dur_=5.2, zoom=0.03)
SECTOR_END = cursor()

# 9) Discord outro (voices inside), then credits
t = cursor()
seg("disc", OUT / "s8_outro.mp4", 0, dur(OUT / "s8_outro.mp4"))
cap_place.append(("Sound11.mp3", t + 0.3, None))
t = cursor()
seg("still", GFX / "slide07.png", dur_=8.0, zoom=0.03, pan=0, fade_in=True, fade_out=True)
music += [(AUD / "Music" / "transmission.mp3", t - 1.0, t + 7.0, 0.3)]
TOTAL = cursor()
# one background track for the whole film (the team's choice); it ducks under speech in the mix below
BGM = HOME / "Downloads" / "leberch-no-copyright-music-594951.mp3"
music = [(BGM, 0.0, TOTAL, 0.30)]


# ---------------------------------------------------------------- render segments
def render(i, s):
    out = SEGS / f"seg_{i:02d}.mp4"
    D = s["dur"]
    n = int(round(D * 30))
    cmd = [FF, "-v", "error", "-y"]
    if s["kind"] in ("clip", "disc"):
        cmd += ["-ss", f"{s['a']:.3f}", "-t", f"{D:.3f}", "-i", str(s["src"])]
    elif s["kind"] == "seq":
        cmd += ["-framerate", "30", "-start_number", str(int(s["a"] * 30) + 1), "-i", str(s["src"] / "f_%04d.png"),
                "-t", f"{D:.3f}"]
    else:
        cmd += ["-loop", "1", "-framerate", "30", "-t", f"{D:.3f}", "-i", str(s["src"])]
    fl = ["[0:v]fps=30,scale=1920:1080:force_original_aspect_ratio=decrease,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,setsar=1"]
    if s["zoom"]:
        # slow push-in plus a sideways drift (pan -1 / 0 / 1), eased so the move starts and ends softly
        z, pn, N = s["zoom"] + 0.03 * abs(s["pan"]), s["pan"], max(1, n - 1)
        e = f"(0.5-0.5*cos(PI*on/{N}))"
        fl.append(f"scale=3840:2160,zoompan=z='1+{z}*{e}':x='(iw-iw/zoom)/2*(1+{pn}*(2*{e}-1))':y='ih/2-(ih/zoom/2)'"
                  f":d=1:s=1920x1080:fps=30")
    if s["kind"] in ("clip", "seq"):
        # light film look on game / 3D shots: a touch more contrast and colour, soft vignette, fine grain
        fl.append("eq=contrast=1.06:saturation=1.08,vignette=angle=PI/5,noise=alls=5:allf=t")
    if s["shake"]:
        fl.append("scale=2016:1134,crop=1920:1080:x='48+30*sin(t*71)*max(0\\,1-t/0.8)':y='27+22*cos(t*53)*max(0\\,1-t/0.8)'")
    if s["flash"] is not None:
        h = s["flash"]
        fl.append(f"drawbox=c=white@0.85:t=fill:enable='between(t,{h},{h + 0.07})',"
                  f"drawbox=c=white@0.4:t=fill:enable='between(t,{h + 0.07},{h + 0.17})'")
        fl.append(f"scale=2016:1134,crop=1920:1080:x='48+34*sin(t*67)*between(t,{h},{h + 0.7})*({h + 0.7}-t)'"
                  f":y='27+24*cos(t*59)*between(t,{h},{h + 0.7})*({h + 0.7}-t)'")
    if s["glitch"]:
        fl.append("rgbashift=rh=-14:bh=14:enable='lt(t,0.45)',noise=alls=45:allf=t:enable='lt(t,0.45)'")
    if s["letterbox"]:
        # cinema bars (2.39:1) that slide in over the first half second
        bar = f"min(132\\,132*t/0.5)"
        fl.append(f"drawbox=x=0:y=0:w=iw:h='{bar}':c=black:t=fill,drawbox=x=0:y='ih-{bar}':w=iw:h='{bar}':c=black:t=fill")
    if s["fade_in"]:
        fl.append("fade=t=in:st=0:d=0.35")
    if s["fade_out"]:
        fl.append(f"fade=t=out:st={D - 0.8:.3f}:d=0.8")
    chain = ",".join(fl) + "[v0]"
    graph = [chain]
    last = "[v0]"
    for k, (png, a, b) in enumerate(s["overlays"]):
        b = min(b, D)
        cmd += ["-loop", "1", "-framerate", "30", "-t", f"{D:.3f}", "-i", str(png)]
        graph.append(f"[{k + 1}:v]format=rgba,fade=t=in:st={a}:d=0.3:alpha=1,fade=t=out:st={max(a, b - 0.25):.3f}:d=0.25:alpha=1[o{k}]")
        # callout bars drop in from above, the title rises from below (ease-out cubic); the tag just fades
        dy = 0 if png == TAG else (60 if png == TITLE else -90)
        y = f"{dy}*pow(1-min(1\\,max(0\\,(t-{a})/0.5))\\,3)"
        graph.append(f"{last}[o{k}]overlay=x=0:y='{y}':eval=frame:enable='between(t,{a},{b})'[v{k + 1}]")
        last = f"[v{k + 1}]"
    cmd += ["-filter_complex", ";".join(graph), "-map", last, "-frames:v", str(n), "-an",
            "-c:v", "h264_nvenc", "-preset", "p5", "-cq", "17", "-pix_fmt", "yuv420p", "-r", "30", str(out)]
    run(cmd)
    return out


files = []
REUSE = os.environ.get("LD_REUSE") == "1"   # skip segments that already exist (faster re-runs)
for i, s in enumerate(timeline):
    f = SEGS / f"seg_{i:02d}.mp4"
    files.append(f if REUSE and f.exists() else render(i, s))
    print(f"seg {i:02d} {s['kind']:5s} {s['dur']:6.2f}s")
lst = SEGS / "list.txt"
lst.write_text("".join(f"file '{f.name}'\n" for f in files), encoding="utf-8")
video = SEGS / "video.mp4"
run([FF, "-v", "error", "-y", "-f", "concat", "-safe", "0", "-i", str(lst), "-c", "copy", str(video)])

# ---------------------------------------------------------------- audio mix (own pass: one graph for audio only)
cmd = [FF, "-v", "error", "-y"]
graph, mix, idx = [], [], 0
starts = []
for i, s in enumerate(timeline):
    st = sum(x["dur"] for x in timeline[:i])
    if s["kind"] == "disc":
        cmd += ["-i", str(s["src"])]
        graph.append(f"[{idx}:a]aresample=48000,aformat=channel_layouts=stereo,adelay={int(st * 1000)}|{int(st * 1000)}[a{idx}]")
        mix.append(f"[a{idx}]"); idx += 1
for f, at, vol, cut in voices:
    cmd += ["-i", str(f)]
    tr = f"atrim=0:{cut}," if cut else ""
    graph.append(f"[{idx}:a]{tr}aresample=48000,aformat=channel_layouts=stereo,volume={vol},adelay={int(at * 1000)}|{int(at * 1000)}[a{idx}]")
    mix.append(f"[a{idx}]"); idx += 1
graph.append("".join(mix) + f"amix=inputs={len(mix)}:normalize=0:duration=longest,asplit=2[speech][key]")
mix = ["[speech]"]
for f, at, vol in sfx:
    cmd += ["-i", str(f)]
    graph.append(f"[{idx}:a]aresample=48000,aformat=channel_layouts=stereo,volume={vol},adelay={int(at * 1000)}|{int(at * 1000)}[a{idx}]")
    mix.append(f"[a{idx}]"); idx += 1
for f, a, b, vol in music:
    L = b - a
    cmd += ["-stream_loop", str(int(L // max(1.0, dur(f))) + 1), "-i", str(f)]
    graph.append(f"[{idx}:a]atrim=0:{L:.3f},asetpts=PTS-STARTPTS,aresample=48000,aformat=channel_layouts=stereo,"
                 f"volume={vol},afade=t=in:d=1.0,afade=t=out:st={max(0, L - 1.5):.3f}:d=1.5,"
                 f"adelay={int(a * 1000)}|{int(a * 1000)}[m{idx}]")
    # music ducks about 12 dB whenever someone speaks, and comes back up in the gaps
    graph.append(f"[m{idx}][key]sidechaincompress=threshold=0.02:ratio=10:attack=30:release=450[a{idx}]")
    mix.append(f"[a{idx}]"); idx += 1
graph.append("".join(mix) + f"amix=inputs={len(mix)}:normalize=0:duration=longest,apad,atrim=0:{TOTAL:.3f},"
             "loudnorm=I=-14:TP=-1.5:LRA=11,aresample=48000[aout]")
mixwav = SEGS / "mix.wav"
cmd += ["-filter_complex", ";".join(graph), "-map", "[aout]", "-c:a", "pcm_s16le", str(mixwav)]
run(cmd)

# captions + final mux
ass = SEGS / "captions.ass"
n_caps = captions.build(TRANSCRIPTS, cap_place, ass, top={"Sound1.mp3", "Sound2.mp3", "Sound3.mp3", "Sound4.mp3", "Sound11.mp3"})
fontsdir = os.path.relpath(FONTS, SEGS).replace("\\", "/")
final = OUT / "LAST_DOCK_presentation.mp4"
run([FF, "-v", "error", "-y", "-i", "video.mp4", "-i", "mix.wav",
     "-vf", f"ass=captions.ass:fontsdir='{fontsdir}'", "-map", "0:v", "-map", "1:a",
     "-c:v", "libx264", "-preset", "medium", "-crf", "18", "-pix_fmt", "yuv420p", "-r", "30",
     "-c:a", "aac", "-b:a", "192k", "-movflags", "+faststart", "-t", f"{TOTAL:.3f}", str(final)], cwd=str(SEGS))
print(f"captions: {n_caps} lines")
print(f"wrote {final}  {TOTAL:.2f}s = {int(TOTAL // 60)}:{TOTAL % 60:05.2f}")
json.dump([{k: str(v) if isinstance(v, pathlib.Path) else v for k, v in s.items() if k != "overlays"} for s in timeline],
          open(SEGS / "timeline.json", "w"), indent=1)
