"""Builds every still graphic for the LAST DOCK video as 1920x1080 PNGs.

HTML templates are rendered with headless Edge/Chrome. Edit team.json, then run:
    python production/video/graphics.py
Output: production/video/gfx/*.png
"""
import json, os, subprocess, html, pathlib, base64

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[1]
OUT = HERE / "gfx"
TMP = HERE / "gfx_html"
OUT.mkdir(exist_ok=True)
TMP.mkdir(exist_ok=True)
FONTS = ROOT / "Assets" / "Resources" / "Fonts"
BROWSERS = [
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Google\Chrome\Application\chrome.exe",
]
BROWSER = next(b for b in BROWSERS if os.path.exists(b))
team = json.loads((HERE / "team.json").read_text(encoding="utf-8"))
M = team["members"]

PANEL, PANEL2, CYAN, CREAM, DIM, ORANGE = "#17302F", "#203F3C", "#8AE0CF", "#FFF2D6", "#C4C4BF", "#E89A3A"
AV_COLORS = ["#E89A3A", "#5865F2", "#3BA55C", "#EB459E", "#FAA61A"]

CSS = f"""
@font-face {{ font-family: Chakra; src: url('{(FONTS / "ChakraPetch-Regular.ttf").as_uri()}'); }}
@font-face {{ font-family: Chakra; font-weight: 700; src: url('{(FONTS / "ChakraPetch-Bold.ttf").as_uri()}'); }}
* {{ margin: 0; padding: 0; box-sizing: border-box; }}
html, body {{ width: 1920px; height: 1080px; overflow: hidden; font-family: Chakra, 'Segoe UI', sans-serif; }}
"""


def esc(s):
    return html.escape(str(s))


def img_uri(p):
    p = pathlib.Path(p)
    if not p.is_absolute():
        p = ROOT / p
    mime = "image/png" if p.suffix.lower() == ".png" else "image/jpeg"
    return f"data:{mime};base64," + base64.b64encode(p.read_bytes()).decode()


def avatar(m, i, size):
    if m.get("photo"):
        inner = f"<img src='{img_uri(m['photo'])}' style='width:100%;height:100%;object-fit:cover'>"
    else:
        inner = f"<span style='font-size:{int(size*0.42)}px;font-weight:700;color:#fff'>{esc(m['nick'][:2].upper())}</span>"
    return (f"<div style='width:{size}px;height:{size}px;border-radius:50%;overflow:hidden;background:{AV_COLORS[i % 5]};"
            f"display:flex;align-items:center;justify-content:center'>{inner}</div>")


def render(name, body, bg="#000"):
    page = TMP / f"{name}.html"
    page.write_text(f"<!doctype html><html><head><meta charset='utf-8'><style>{CSS} body{{background:{bg}}}</style></head>"
                    f"<body>{body}</body></html>", encoding="utf-8")
    out = OUT / f"{name}.png"
    subprocess.run([BROWSER, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=1",
                    "--window-size=1920,1080", f"--screenshot={out}", "--virtual-time-budget=2000",
                    page.as_uri()], check=True, capture_output=True)
    print("wrote", out.name)


# ---------- Discord mock: one image per join state (1..5 people in the call) ----------
def discord(n, speaking=-1):
    tiles = ""
    for i, m in enumerate(M[:n]):
        ring = f"box-shadow:0 0 0 6px #3BA55C;" if i == speaking else ""
        tiles += (f"<div style='background:#2B2D31;border-radius:14px;display:flex;flex-direction:column;align-items:center;"
                  f"justify-content:center;gap:22px'><div style='border-radius:50%;{ring}'>{avatar(m, i, 170)}</div>"
                  f"<div style='color:#F2F3F5;font-size:34px;font-weight:700'>{esc(m['nick'])}</div></div>")
    cols = 1 if n == 1 else 2 if n <= 4 else 3
    side = "".join(
        f"<div style='display:flex;align-items:center;gap:12px;padding:8px 0 8px 34px;color:{'#F2F3F5' if i < n else '#80848E'}'>"
        f"{avatar(m, i, 34) if i < n else ''}<span style='font-size:22px'>{esc(m['nick']) if i < n else ''}</span></div>"
        for i, m in enumerate(M))
    body = f"""
<div style='display:flex;height:1080px;background:#313338'>
  <div style='width:90px;background:#1E1F22;display:flex;flex-direction:column;align-items:center;padding-top:20px;gap:14px'>
    <div style='width:60px;height:60px;border-radius:18px;background:{CYAN};color:{PANEL};font-weight:700;font-size:26px;display:flex;align-items:center;justify-content:center'>OT</div>
  </div>
  <div style='width:330px;background:#2B2D31;color:#DBDEE1;padding-top:24px'>
    <div style='font-size:26px;font-weight:700;padding:0 24px 22px;border-bottom:1px solid #1E1F22'>{esc(team['team'])}</div>
    <div style='font-size:18px;color:#80848E;padding:24px 24px 8px;letter-spacing:1px'>VOICE CHANNELS</div>
    <div style='font-size:24px;padding:6px 24px;color:#F2F3F5'>🔊 space-apps-2026</div>
    {side}
  </div>
  <div style='flex:1;display:flex;flex-direction:column;background:#000'>
    <div style='height:70px;display:flex;align-items:center;padding:0 30px;color:#F2F3F5;font-size:26px;font-weight:700;background:#1E1F22'>🔊 space-apps-2026</div>
    <div style='flex:1;display:grid;grid-template-columns:repeat({cols},1fr);gap:16px;padding:24px'>{tiles}</div>
    <div style='height:96px;display:flex;align-items:center;justify-content:center;gap:22px;background:#1E1F22'>
      <div style='width:64px;height:64px;border-radius:50%;background:#2B2D31'></div>
      <div style='width:64px;height:64px;border-radius:50%;background:#2B2D31'></div>
      <div style='width:96px;height:64px;border-radius:32px;background:#DA373C'></div>
    </div>
  </div>
</div>"""
    return body


BG = f"background:radial-gradient(circle at 50% 40%, {PANEL2}, #0B1615 75%)"


# ---------- Slides ----------
def slide(title, bullets, kicker="", image=None):
    lis = "".join(f"<li style='margin:0 0 26px 0'>{b}</li>" for b in bullets)
    pic = (f"<div style='flex:0 0 760px;height:430px;border-radius:20px;overflow:hidden;border:3px solid {CYAN}'>"
           f"<img src='{img_uri(image)}' style='width:100%;height:100%;object-fit:cover'></div>") if image else ""
    return f"""
<div style='width:1920px;height:1080px;{BG};padding:110px 130px;color:{CREAM}'>
  <div style='color:{ORANGE};font-size:34px;letter-spacing:4px;margin-bottom:18px'>{kicker}</div>
  <div style='font-size:96px;font-weight:700;margin-bottom:60px'>{title}</div>
  <div style='display:flex;gap:70px;align-items:flex-start'>
    <ul style='list-style:none;font-size:46px;line-height:1.25;flex:1'>{lis}</ul>{pic}
  </div>
  <div style='position:absolute;left:130px;bottom:60px;color:{DIM};font-size:26px'>LAST DOCK · Team {esc(team['team'])} · NASA Space Apps Challenge 2026 — Bangkok</div>
</div>"""


ev = "production/qa/evidence"
render("slide01", f"""
<div style='width:1920px;height:1080px;position:relative;background:#000'>
  <img src='{img_uri("docs/key-art.png")}' style='position:absolute;inset:0;width:100%;height:100%;object-fit:cover;opacity:.55'>
  <div style='position:absolute;left:140px;top:330px;color:{CREAM}'>
    <div style='font-size:170px;font-weight:700;letter-spacing:18px'>LAST DOCK</div>
    <div style='width:760px;height:6px;background:{ORANGE};margin:20px 0 30px'></div>
    <div style='font-size:56px'>Junior Astronaut Mission Trainer</div>
    <div style='font-size:36px;color:{CYAN};margin-top:26px'>Team {esc(team['team'])} · NASA Space Apps Challenge 2026 — Bangkok</div>
  </div>
</div>""")
render("slide02", slide("Our question", [
    "How can a child understand life in space —",
    f"<span style='color:{CYAN}'>without reading a textbook first?</span>",
    "Life support is invisible: air, water, heat, power.",
    "So we made them break — and let players choose what to fix."], "THE IDEA"))
render("slide03", slide("Who it's for", [
    "Kids aged 7–12", "Thai + English", "Learn by choosing, not memorising",
    "Short science cards after every problem"], "PLAYERS", f"{ev}/operational-ui/th130-1280-life.png"))
render("slide04", slide("Real NASA science", [
    f"CO₂ removal · O₂ from water · water recovery <span style='color:{CYAN}'>→ NASA ECLSS</span>",
    f"Heat <span style='color:{CYAN}'>→ ISS Active Thermal Control</span>",
    f"Power <span style='color:{CYAN}'>→ ISS solar arrays &amp; batteries</span>",
    f"Solar storm <span style='color:{CYAN}'>→ NASA: Mars lights up in a solar storm</span>",
    f"Improvised CO₂ repair <span style='color:{CYAN}'>→ Apollo 13</span>",
    f"<span style='color:{DIM};font-size:36px'>Game rules: the 7-day rescue, numbers and chances</span>"], "SCIENCE"))
render("slide05", slide("How we built it", [
    "Unity + C#", "Survival rules in a pure C# simulation",
    "Automated tests + bot players", "Fair, seeded incident director",
    "Pixel art in code + Blender renders · live 3D menu", "Thai text shaping (HarfBuzz)"], "TECH"))
render("slide06", slide("Impact &amp; next steps", [
    "Leave with questions, not just answers",
    "Why does air need to move?", "Why do batteries matter when the sun shines?",
    f"<span style='color:{CYAN}'>Next: playtest with kids &amp; teachers</span>"], "WHY IT MATTERS"))
names = "".join(f"<div style='display:flex;align-items:center;gap:22px;margin-bottom:18px'>{avatar(m, i, 86)}<div><div style='font-size:36px'>{i + 1}. {esc(m['full'])} ({esc(m['nick'])})</div><div style='font-size:28px;color:{CYAN}'>{esc(m['role'])}</div></div></div>" for i, m in enumerate(M))
render("slide07", f"""
<div style='width:1920px;height:1080px;{BG};padding:90px 130px;color:{CREAM};font-size:34px;line-height:1.5'>
  <div style='font-size:80px;font-weight:700;margin-bottom:30px'>Credits</div>
  <div style='display:flex;gap:90px'>
    <div style='flex:1'><div style='color:{ORANGE};letter-spacing:3px;margin-bottom:10px'>TEAM {esc(team['team'].upper())}</div>{names}</div>
    <div style='flex:1;font-size:28px'>
      <div style='color:{ORANGE};letter-spacing:3px;margin-bottom:10px;font-size:34px'>RESOURCES</div>
      NASA — ECLSS; ISS Active Thermal Control; ISS Electrical Power;<br>Water Recovery on ISS; Mars solar storm; Apollo 13<br>
      Unity · Blender · Kenney sounds (CC0) · SRG774 music (CC0)<br>Background music: leberch (no-copyright music)<br>Google Fonts: Chakra Petch, Sarabun (OFL)<br>
      <span style='color:{CYAN}'>AI use: coding, debugging and documents assisted by AI tools;<br>voice-over generated with Fish Audio.</span>
    </div>
  </div>
</div>""")

# ---------- Bars used over game footage (full-frame PNG with transparent-looking dark strip; keyed in ffmpeg) ----------
def bar(name, text):
    render(name, f"<div style='width:1920px;height:1080px;position:relative'>"
                 f"<div style='position:absolute;left:0;right:0;top:40px;display:flex;justify-content:center'>"
                 f"<div style='background:rgba(11,22,21,.85);border:3px solid {CYAN};border-radius:18px;padding:16px 40px;"
                 f"color:{CREAM};font-size:52px;font-weight:700'>{text}</div></div></div>", bg="#00FF00")


bar("bar_4a", "60 seconds · carry 3 · pod holds 8")
bar("bar_4b", "7 days · 3 work units a day")
bar("bar_4c", "More O₂ ≠ less CO₂")
bar("bar_4d", "Storm · Dinner · Rescue")
print("done")
