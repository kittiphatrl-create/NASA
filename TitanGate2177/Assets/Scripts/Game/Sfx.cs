// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>
    /// Synthesized sound: soft sine/triangle tones with gentle envelopes (no harsh square waves),
    /// plus looping danger sounds and a quiet ship ambience. Volumes follow Settings.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        AudioSource _src;
        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();
        readonly Dictionary<string, AudioSource> _loops = new Dictionary<string, AudioSource>();
        readonly Dictionary<string, float> _loopBase = new Dictionary<string, float>();
        const int Rate = 22050;
        static readonly System.Random Rng = new System.Random(1);

        void Awake()
        {
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;

            // UI: short, soft, low
            Make("click", 0.07f, t => Sine(t, 520) * Pluck(t, 0.07f) * 0.5f + Sine(t, 1040) * Pluck(t, 0.03f) * 0.15f);
            Make("hover", 0.04f, t => Sine(t, 880) * Pluck(t, 0.04f) * 0.12f);
            Make("open", 0.18f, t => Sine(t, Mathf.Lerp(440, 660, t / 0.18f)) * Soft(t, 0.18f, 0.01f) * 0.3f);
            Make("close", 0.15f, t => Sine(t, Mathf.Lerp(600, 420, t / 0.15f)) * Soft(t, 0.15f, 0.01f) * 0.25f);
            Make("good", 0.55f, t => (Tri(t, 523) * Bell(t, 0f) + Tri(t, 659) * Bell(t, 0.11f) + Tri(t, 784) * Bell(t, 0.22f)) * 0.32f);
            Make("bad", 0.35f, t => (Sine(t, 196) + 0.5f * Sine(t, 185)) * Soft(t, 0.35f, 0.01f) * 0.35f);
            Make("card", 0.9f, t => (Sine(t, 659) * Bell(t, 0f) + Sine(t, 784) * Bell(t, 0.12f) + Sine(t, 1046) * Bell(t, 0.24f) + Sine(t, 1318) * Bell(t, 0.36f) * 0.6f) * 0.3f);
            Make("blip", 0.035f, t => Sine(t, 740) * Pluck(t, 0.035f) * 0.12f);
            Make("work", 0.3f, t => Noise() * Soft(t, 0.3f, 0.05f) * 0.12f + Sine(t, 120) * Soft(t, 0.3f, 0.05f) * 0.1f);
            Make("step", 0.06f, t => Noise() * Pluck(t, 0.06f) * 0.07f);
            Make("whoosh", 0.9f, t => LowNoise(0.92f) * Mathf.Sin(t / 0.9f * Mathf.PI) * 0.45f);
            Make("thud", 0.8f, t => (Sine(t, Mathf.Lerp(90, 40, t / 0.8f)) * 0.9f + LowNoise(0.8f) * 0.5f) * Pluck(t, 0.8f));
            Make("alarm", 0.9f, t => Sine(t, (int)(t * 3) % 2 == 0 ? 660 : 495) * Soft(t, 0.9f, 0.02f) * 0.3f);
            Make("rumble", 2.0f, t => LowNoise(0.97f) * 0.6f);
            Make("heart", 0.9f, t => Sine(t, 55) * (Pulse(t, 0f) + 0.7f * Pulse(t, 0.22f)) * 1.3f);
            Make("siren", 1.6f, t => Sine(t, 520 + 160 * Mathf.Sin(t / 1.6f * Mathf.PI * 2)) * 0.22f);
            // sliding door: a pneumatic hiss with a motor whine, and a soft thunk when it shuts
            Make("door_open", 0.5f, t => LowNoise(0.55f) * Soft(t, 0.5f, 0.02f) * 0.22f + Sine(t, Mathf.Lerp(150, 240, t / 0.5f)) * Soft(t, 0.5f, 0.06f) * 0.12f);
            Make("door_close", 0.55f, t => LowNoise(0.55f) * Soft(t, 0.45f, 0.02f) * 0.2f + Sine(t, Mathf.Lerp(240, 150, t / 0.55f)) * Soft(t, 0.45f, 0.06f) * 0.1f + Sine(t, 70) * Bell(t, 0.42f) * 0.5f);
            Make("hum", 4.0f, t => (Sine(t, 55) * 0.35f + Sine(t, 110) * 0.15f + Sine(t, 165.5f) * 0.05f) * (0.85f + 0.15f * Mathf.Sin(t / 4f * Mathf.PI * 2)) + LowNoise(0.995f) * 0.08f);

            foreach (var id in new[] { "click", "hover", "open", "close", "good", "ship", "drone" }) { var clip = Resources.Load<AudioClip>("Audio/KenneyUI/" + id); if (clip != null) _clips[id] = clip; }
            // real CC0 recordings (Kenney Sci-Fi Sounds, docs/AUDIO_CREDITS.md) replace the generated ones where we have them
            foreach (var (id, file) in new[] { ("door_open", "doorOpen_000"), ("door_close", "doorClose_000"), ("blip", "computerNoise_001"), ("work", "impactMetal_000"),
                ("thud", "lowFrequency_explosion_000"), ("whoosh", "thrusterFire_000"), ("rumble", "spaceEngineLow_000"), ("shield", "forceField_000"),
                ("rover", "engineCircular_000"), ("console", "computerNoise_000"), ("clank", "impactMetal_002") })
            { var clip = Resources.Load<AudioClip>("Audio/KenneySciFi/" + file); if (clip != null) _clips[id] = clip; }

            Loopable("heart", 0.45f, false);
            Loopable("siren", 0.25f, false);
            Loopable("rumble", 0.5f, false);
            Loopable("hum", 0.35f, true);
            Settings.Changed += ApplyVolumes;
            ApplyVolumes();
        }

        void OnDestroy() => Settings.Changed -= ApplyVolumes;

        // ---- wave helpers
        static float Sine(float t, float f) => Mathf.Sin(t * 2 * Mathf.PI * f);
        static float Tri(float t, float f) { float p = t * f % 1f; return 4f * Mathf.Abs(p - 0.5f) - 1f; }
        static float Noise() => (float)(Rng.NextDouble() * 2 - 1);
        static float _lp;
        static float LowNoise(float k) { _lp = _lp * k + Noise() * (1 - k); return _lp * 6f; }
        /// <summary>Fast attack, exponential decay.</summary>
        static float Pluck(float t, float len) => Mathf.Clamp01(t / 0.004f) * Mathf.Exp(-5f * t / len);
        /// <summary>Soft attack and release.</summary>
        static float Soft(float t, float len, float attack) => Mathf.Clamp01(t / attack) * Mathf.Clamp01((len - t) / (len * 0.4f));
        static float Bell(float t, float at) { float d = t - at; return d < 0 ? 0 : Mathf.Clamp01(d / 0.005f) * Mathf.Exp(-6f * d); }
        static float Pulse(float t, float at) { float d = t - at; return d < 0 || d > 0.12f ? 0 : 1f - d / 0.12f; }

        void Make(string id, float len, System.Func<float, float> f)
        {
            _lp = 0;
            int n = Mathf.CeilToInt(len * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f) * 0.6f;
            // tiny fade at both ends: no clicks
            int edge = Mathf.Min(64, n / 4);
            for (int i = 0; i < edge; i++) { float k = i / (float)edge; data[i] *= k; data[n - 1 - i] *= k; }
            var clip = AudioClip.Create(id, n, 1, Rate, false);
            clip.SetData(data, 0);
            _clips[id] = clip;
        }

        void Loopable(string id, float vol, bool ambience)
        {
            var a = gameObject.AddComponent<AudioSource>();
            a.clip = _clips[id]; a.loop = true; a.playOnAwake = false;
            _loops[id] = a;
            _loopBase[id] = vol * (ambience ? -1 : 1);
        }

        void ApplyVolumes()
        {
            _src.volume = Settings.Master * Settings.Effects;
            foreach (var kv in _loops)
            {
                float b = _loopBase[kv.Key];
                kv.Value.volume = Settings.Master * Mathf.Abs(b) * (b < 0 ? Settings.Ambience : Settings.Effects);
            }
        }

        /// <summary>Play a one-shot. The same sound never stacks faster than its cool-down.</summary>
        public void Play(string id, float vol = 1f)
        {
            if (_src == null || !_clips.TryGetValue(id, out var c)) return;
            float cool = id == "hover" ? 0.08f : id == "step" ? 0.12f : id == "blip" ? 0.06f : 0.05f;
            if (_lastPlayed.TryGetValue(id, out var last) && Time.unscaledTime - last < cool) return;
            _lastPlayed[id] = Time.unscaledTime;
            if (id == "hover") vol *= .16f; else if (id == "click") vol *= .38f; else if (id == "open" || id == "close" || id == "good" || id == "ship" || id == "drone") vol *= .55f;
            _src.PlayOneShot(c, vol);
        }

        /// <summary>Start or stop a looping sound ("heart", "siren", "rumble", "hum").</summary>
        public void Loop(string id, bool on)
        {
            if (!_loops.TryGetValue(id, out var a)) return;
            if (on && !a.isPlaying) a.Play();
            else if (!on && a.isPlaying) a.Stop();
        }
    }
}
