"""Builds an .ass subtitle file from the Whisper word timings of the voice files.

placements: [(voice file name, start time in the final cut, optional cut-off in file seconds)]
Lines are short (<= 7 words or 2.8 s), styled like the game UI (Chakra Petch, cream on dark teal box).
"""
import json, re

FIX = [
    (r"\bmass\b", "Mars"), (r"Overtime,? P\.? ?W\.?", "Overtimepw"), (r"\bTam\b", "Tan"),
    (r"\bSkibbity\.?", ""), (r"\bblender\b", "Blender"), (r"\bLast dock\b", "LAST DOCK"),
    (r"\bCO2\b", "CO₂"), (r"\bvoiceover\b", "voice-over"),
]

HEADER = """[Script Info]
ScriptType: v4.00+
PlayResX: 1920
PlayResY: 1080
WrapStyle: 2

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Cap,Chakra Petch,50,&H00D6F2FF,&H00CFE08A,&H1F2F3017,&H00000000,-1,0,0,0,100,100,0.5,0,3,14,0,2,200,200,150,1
Style: CapTop,Chakra Petch,46,&H00D6F2FF,&H00CFE08A,&H1F2F3017,&H00000000,-1,0,0,0,100,100,0.5,0,3,12,0,8,200,200,22,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
"""


def ts(t):
    t = max(0.0, t)
    h, r = divmod(t, 3600)
    m, s = divmod(r, 60)
    return f"{int(h)}:{int(m):02d}:{s:05.2f}"


def clean(text):
    for a, b in FIX:
        text = re.sub(a, b, text)
    return re.sub(r"\s+", " ", text).strip()


def build(transcripts_path, placements, out_path, top=()):
    data = json.load(open(transcripts_path, encoding="utf-8"))
    lines = []
    for name, start, cut in placements:
        words = [w for s in data[name]["segments"] for w in s["words"] if cut is None or w[0] < cut]
        chunk = []
        for i, (a, b, w) in enumerate(words):
            chunk.append((a, b, w))
            end_sentence = w.strip().endswith((".", "!", "?"))
            nxt_gap = words[i + 1][0] - b if i + 1 < len(words) else 9
            if len(chunk) >= 7 or chunk[-1][1] - chunk[0][0] > 2.8 or end_sentence or nxt_gap > 0.6:
                text = clean("".join(x[2] for x in chunk))
                if text:
                    lines.append((start + chunk[0][0], start + chunk[-1][1] + 0.15, text, "CapTop" if name in top else "Cap"))
                chunk = []
    # never overlap: trim each line to the next one's start
    lines.sort()
    out = []
    for i, (a, b, t, st) in enumerate(lines):
        if i + 1 < len(lines):
            b = min(b, lines[i + 1][0] - 0.02)
        out.append(f"Dialogue: 0,{ts(a)},{ts(b)},{st},,0,0,0,,{t}")
    open(out_path, "w", encoding="utf-8").write(HEADER + "\n".join(out) + "\n")
    return len(out)
