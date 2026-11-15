// Mechanical sounds for the docking-ring menu, synthesised once at start-up (no new audio files):
// a ratchet tick per item the ring passes, a servo whir that follows the ring's speed, a latch
// "clunk" when an item docks, and a warm two-note chime when an item is chosen.
using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Game.Menu
{
    public static class MenuAudio
    {
        const int Rate = 44100;
        static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
        static AudioSource[] _oneShots;
        static AudioSource _servo;
        static int _next;
        static readonly System.Random Rng = new System.Random(11);
        static float _lastTick;

        static float Vol => Settings.Master * Settings.Effects;

        public static void Init(GameObject host)
        {
            if (_servo != null) return;   // already set up (sources live on the host; a destroyed host makes this null again)
            _oneShots = new AudioSource[4];
            for (int i = 0; i < _oneShots.Length; i++) { _oneShots[i] = host.AddComponent<AudioSource>(); _oneShots[i].playOnAwake = false; }
            if (Clips.Count == 0) Build();
            _servo = host.AddComponent<AudioSource>();
            _servo.clip = Clips["servo"]; _servo.loop = true; _servo.playOnAwake = false; _servo.volume = 0;
        }

        static float N() => (float)(Rng.NextDouble() * 2 - 1);

        static AudioClip Make(string id, float len, System.Func<float, float> f, bool loop = false)
        {
            int n = Mathf.CeilToInt(len * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f);
            if (!loop) { int e = Mathf.Min(48, n / 8); for (int i = 0; i < e; i++) d[n - 1 - i] *= i / (float)e; }
            var c = AudioClip.Create("menu_" + id, n, 1, Rate, false); c.SetData(d, 0);
            Clips[id] = c;
            return c;
        }

        static void Build()
        {
            const float Tau = Mathf.PI * 2f;
            // ratchet: a tiny noise burst plus two short metallic rings
            Make("tick", 0.06f, t => N() * Mathf.Exp(-t / 0.0035f) * 0.45f
                + Mathf.Sin(Tau * 2650 * t) * Mathf.Exp(-t / 0.011f) * 0.35f + Mathf.Sin(Tau * 1320 * t) * Mathf.Exp(-t / 0.02f) * 0.22f);
            // lock: a falling low thump, a click, and an inharmonic metal ping that rings out
            float lp = 0;
            Make("lock", 0.6f, t =>
            {
                float f = Mathf.Lerp(120, 52, Mathf.Clamp01(t / 0.12f));
                float thump = Mathf.Sin(Tau * f * t) * Mathf.Exp(-t / 0.07f) * 0.75f;
                lp += (N() - lp) * 0.35f;
                float click = lp * Mathf.Exp(-t / 0.008f) * 0.7f;
                float ping = (Mathf.Sin(Tau * 1418 * t) + 0.6f * Mathf.Sin(Tau * 2291 * t) + 0.35f * Mathf.Sin(Tau * 3707 * t)) * Mathf.Exp(-t / 0.2f) * 0.12f;
                return thump + click + ping;
            });
            // confirm: two warm notes (E5 then B5) with a soft attack and a long tail
            Make("chime", 1.1f, t =>
            {
                float a = Mathf.Clamp01(t / 0.01f) * Mathf.Exp(-t / 0.35f);
                float b = t < 0.09f ? 0 : Mathf.Clamp01((t - 0.09f) / 0.01f) * Mathf.Exp(-(t - 0.09f) / 0.45f);
                float e5 = Mathf.Sin(Tau * 659.3f * t) + 0.25f * Mathf.Sin(Tau * 1318.5f * t);
                float b5 = Mathf.Sin(Tau * 987.8f * t) + 0.2f * Mathf.Sin(Tau * 1975.5f * t);
                return (e5 * a * 0.32f + b5 * b * 0.3f);
            });
            // servo: band-limited noise plus a hum; loops seamlessly (1 s, whole-number cycles)
            float s1 = 0, s2 = 0;
            Make("servo", 1f, t =>
            {
                s1 += (N() - s1) * 0.12f; s2 += (s1 - s2) * 0.12f;
                return (s1 - s2) * 1.6f + Mathf.Sin(Tau * 180 * t) * 0.22f + Mathf.Sin(Tau * 360 * t) * 0.08f;
            }, true);
        }

        public static void Play(string id, float vol = 1f, float pitchJitter = 0.03f, float pitch = 1f)
        {
            if (_oneShots == null || !Clips.TryGetValue(id, out var c)) return;
            var src = _oneShots[_next = (_next + 1) % _oneShots.Length];
            if (src == null) return;
            src.pitch = pitch * (1f + (float)(Rng.NextDouble() * 2 - 1) * pitchJitter);
            src.PlayOneShot(c, vol * Vol);
        }

        /// <summary>One ratchet click per item the ring passes (rate-limited so fast spins stay clean).</summary>
        public static void Tick(float speed)
        {
            if (Time.unscaledTime - _lastTick < 0.035f) return;
            _lastTick = Time.unscaledTime;
            Play("tick", 0.55f, 0.05f, 1f + Mathf.Clamp(speed * 0.04f, 0, 0.25f));
        }

        /// <summary>Servo whir: louder and higher while the ring turns fast, silent when still.</summary>
        public static void Servo(float speed, float dt)
        {
            if (_servo == null) return;
            float target = Mathf.Clamp01(speed / 6f) * 0.22f * Vol;
            _servo.volume = Mathf.MoveTowards(_servo.volume, target, dt * 1.6f);
            _servo.pitch = 0.75f + Mathf.Clamp01(speed / 8f) * 0.6f;
            if (_servo.volume > 0.001f && !_servo.isPlaying) _servo.Play();
            else if (_servo.volume <= 0.001f && _servo.isPlaying) _servo.Stop();
        }

        public static void StopAll() { if (_servo != null) { _servo.volume = 0; _servo.Stop(); } }
    }
}
