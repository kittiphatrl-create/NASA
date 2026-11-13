"""Encode the opt-in Unity QA capture with its actual frame times and listener mix."""
import argparse
import array
import csv
import json
from pathlib import Path
import subprocess

p = argparse.ArgumentParser()
p.add_argument("frames", type=Path)
p.add_argument("output", type=Path)
p.add_argument("--ffmpeg", default="ffmpeg")
a = p.parse_args()
rows = [(int(i), float(t)) for i, t in csv.reader((a.frames / "times.csv").open())]
assert rows and all(rows[i + 1][1] > rows[i][1] for i in range(len(rows) - 1))
requested = len(rows)
# Unity can coalesce two screenshot requests in one rendered frame (the named QA
# shot and the video recorder). Hold the preceding real frame for that interval.
run_started = (a.frames / f"frame_{rows[0][0]:05d}.png").stat().st_mtime - .5
def current_frame(index):
    frame = a.frames / f"frame_{index:05d}.png"
    return frame.exists() and frame.stat().st_mtime >= run_started
rows = [(i, t) for i, t in rows if current_frame(i)]
assert rows, "No rendered capture frames were saved"
entries = []
for n, (index, timestamp) in enumerate(rows):
    frame = (a.frames / f"frame_{index:05d}.png").resolve()
    assert frame.exists(), frame
    duration = rows[n + 1][1] - timestamp if n + 1 < len(rows) else 1 / 12
    entries += [f"file '{frame.as_posix()}'", f"duration {duration:.5f}"]
entries += [f"file '{frame.as_posix()}'"]
concat = a.frames / "walkthrough.ffconcat"
concat.write_text("ffconcat version 1.0\n" + "\n".join(entries) + "\n", encoding="utf-8")
duration = rows[-1][1] - rows[0][1] + 1 / 12
cmd = [a.ffmpeg, "-hide_banner", "-loglevel", "warning", "-y", "-f", "concat", "-safe", "0", "-i", str(concat)]
audio = a.frames / "audio.f32"
metrics = {"frames": len(rows), "coalescedCaptureRequests": requested - len(rows),
           "durationSeconds": round(duration, 3), "actualListenerAudio": False}
if audio.exists() and audio.stat().st_size:
    meta = json.loads(Path(str(audio) + ".json").read_text())
    samples = array.array("f")
    with audio.open("rb") as src:
        samples.fromfile(src, audio.stat().st_size // 4)
    peak = max(abs(v) for v in samples)
    rms = (sum(v * v for v in samples) / len(samples)) ** .5
    metrics.update(actualListenerAudio=peak > 0, audioPeak=peak, audioRms=rms,
                   audioSeconds=len(samples) / meta["sampleRate"] / meta["channels"])
    if peak > 0:
        cmd += ["-f", "f32le", "-ar", str(meta["sampleRate"]), "-ac", str(meta["channels"]), "-i", str(audio), "-map", "0:v:0", "-map", "1:a:0", "-c:a", "aac", "-b:a", "160k"]
a.output.parent.mkdir(parents=True, exist_ok=True)
cmd += ["-c:v", "libx264", "-preset", "fast", "-crf", "20", "-pix_fmt", "yuv420p", "-r", "30", "-fps_mode", "cfr", "-t", f"{duration:.5f}", "-movflags", "+faststart", str(a.output)]
subprocess.run(cmd, check=True)
metrics["videoBytes"] = a.output.stat().st_size
a.output.with_suffix(".json").write_text(json.dumps(metrics, indent=2), encoding="utf-8")
print(json.dumps(metrics))
