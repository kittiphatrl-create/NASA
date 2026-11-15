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
        AudioSource _src, _collected, _srcVar, _impact;
        bool _cineAlarm;
        float _collectedLevel = .95f;
        readonly Dictionary<string, float> _cinematicTargets = new Dictionary<string, float>();
        readonly Dictionary<string, float> _cinematicGains = new Dictionary<string, float>();
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
            _srcVar = gameObject.AddComponent<AudioSource>();
            _srcVar.playOnAwake = false;

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
            foreach (var (id, file) in new[] { ("door_open", "doorOpen_000"), ("door_close", "doorClose_000"), ("work", "impactMetal_000"),
                ("thud", "lowFrequency_explosion_000"), ("whoosh", "thrusterFire_000"), ("rumble", "spaceEngineLow_000"), ("shield", "forceField_000"),
                ("rover", "engineCircular_000"), ("console", "computerNoise_000"), ("clank", "impactMetal_002") })
            { var clip = Resources.Load<AudioClip>("Audio/KenneySciFi/" + file); if (clip != null) _clips[id] = clip; }
            // the door sound the user chose (space-ship-door-open, freesound_community): both opening and closing
            { var door = Resources.Load<AudioClip>("Audio/Doors/door"); if (door != null) { _clips["door_open"] = door; _clips["door_close"] = door; } }
            // the user found the beeping on use/pick-up annoying: real handling sounds instead (Kenney RPG Sounds, CC0)
            foreach (var (id, file) in new[] { ("card", "handleSmallLeather"), ("console", "metalClick"), ("clank", "metalLatch"), ("pickup", "cloth1"), ("drop", "dropLeather"),
                ("step0", "footstep00"), ("step1", "footstep01"), ("step2", "footstep02"), ("step3", "footstep03") })
            { var clip = Resources.Load<AudioClip>("Audio/KenneyRPG/" + file); if (clip != null) _clips[id] = clip; }

            // User-selected effects. Keep collection separate from card/UI and ordinary handling sounds.
            var collected = Resources.Load<AudioClip>("Audio/UserEffects/item_collected_game")
                ?? Resources.Load<AudioClip>("Audio/UserEffects/item_collected");
            if (collected != null) _clips["collected"] = collected;
            else _clips["collected"] = _clips.TryGetValue("pickup", out var fallbackPickup) ? fallbackPickup : _clips["card"];
            _clips["complete"] = _clips["collected"];
            var shipAlarm = Resources.Load<AudioClip>("Audio/UserEffects/ship_alarm");
            _clips["ship_alarm"] = shipAlarm != null ? shipAlarm : _clips["siren"];
            _collected = gameObject.AddComponent<AudioSource>();
            _collected.playOnAwake = false;
            _collected.loop = false;
            _collected.spatialBlend = 0f;
            _collected.priority = 32;

            // The debris strike in the cutscenes: a deep boom, a hard crunch, the hull ringing like a struck
            // tank and loose parts rattling down. Played together with the recorded explosion and metal hits.
            Make("crash", 2.6f, t =>
            {
                float atk = Mathf.Clamp01(t / 0.003f);
                float boom = Sine(t, Mathf.Lerp(64f, 26f, Mathf.Clamp01(t / 1.1f))) * Mathf.Exp(-t * 1.5f) * 0.95f;
                float crunch = Noise() * Mathf.Exp(-t * 16f) * 0.9f;
                float body = LowNoise(0.9f) * Mathf.Exp(-t * 2.4f) * 0.45f;
                float ring = (Sine(t, 187) * 0.5f + Sine(t, 311) * 0.42f + Sine(t, 467) * 0.3f + Sine(t, 733) * 0.2f + Sine(t, 1093) * 0.12f)
                             * Mathf.Exp(-t * 2.2f) * 0.4f * (1f + 0.25f * Sine(t, 5.5f));
                float rattle = (Mathf.PerlinNoise(t * 38f, 1.7f) > 0.72f ? Noise() : 0f) * Mathf.Exp(-t * 1.3f) * 0.35f;
                float x = atk * (boom + crunch + body + ring + rattle);
                return x / (1f + Mathf.Abs(x) * 0.6f) * 1.5f;   // soft saturation: loud, but no hard clipping
            });
            // A real ship's red-alert klaxon: a buzzy rising "whoop", 0.8 s on, then a short gap (loops).
            Make("klaxon", 1.15f, t =>
            {
                const float on = 0.8f;
                if (t >= on) return 0f;
                float ph = 330f * t + (620f - 330f) * t * t / (2f * on);          // a linear 330 -> 620 Hz sweep (integrated phase)
                float w = 0f;
                for (int h = 1; h <= 7; h++) w += Mathf.Sin(2 * Mathf.PI * h * ph) / h;
                float env = Mathf.Clamp01(t / 0.03f) * Mathf.Clamp01((on - t) / 0.06f);
                return w * env * 0.55f * (0.9f + 0.1f * Sine(t, 31f));
            });
            if (_clips.TryGetValue("thud", out var boomRec)) _clips["crash_boom"] = boomRec;
            var metalA = Resources.Load<AudioClip>("Audio/KenneySciFi/impactMetal_000");
            var metalB = Resources.Load<AudioClip>("Audio/KenneySciFi/impactMetal_002");
            if (metalA != null) _clips["crash_metalA"] = metalA;
            if (metalB != null) _clips["crash_metalB"] = metalB;
            _impact = gameObject.AddComponent<AudioSource>();
            _impact.playOnAwake = false; _impact.priority = 0; _impact.spatialBlend = 0f;

            // A continuous bed under the silent rendered videos, rather than a single short rumble.
            _clips["cine_engine"] = _clips["rumble"];
            _clips["cine_base"] = _clips["hum"];
            Make("cine_air", 4f, t => LowNoise(.85f) * .30f * (.85f + .15f * Mathf.Sin(t * Mathf.PI / 2f)));
            Make("cine_wind", 6f, t => LowNoise(.95f) * .44f * (.7f + .3f * Mathf.Sin(t * Mathf.PI / 3f)));
            foreach (var id in new[] { "cine_engine", "cine_air", "cine_wind", "cine_base" })
            {
                _cinematicTargets[id] = 0f;
                _cinematicGains[id] = 0f;
                Loopable(id, id == "cine_base" ? .28f : .55f, id == "cine_wind" || id == "cine_base");
            }

            Loopable("heart", 0.45f, false);
            Loopable("siren", 0.25f, false);
            Loopable("rumble", 0.5f, false);
            Loopable("hum", 0.35f, true);
            Loopable("ship_alarm", 0.35f, false);
            Loopable("klaxon", 0.42f, false);
            Settings.Changed += ApplyVolumes;
            ApplyVolumes();
        }

        void OnDestroy() => Settings.Changed -= ApplyVolumes;

        void Update()
        {
            foreach (var id in _cinematicTargets.Keys)
            {
                float gain = Mathf.MoveTowards(_cinematicGains[id], _cinematicTargets[id], Time.unscaledDeltaTime / .25f);
                _cinematicGains[id] = gain;
                if (gain <= 0f && _loops[id].isPlaying) _loops[id].Stop();
            }
            ApplyVolumes();
        }

        public void CinematicShot(string shot)
        {
            StopCinematicAudio();
            void Layer(string id, float gain = 1f) { _cinematicTargets[id] = gain; Loop(id, true); }
            if (shot == "l01_away") Layer("cine_engine");
            else if (shot == "l02_entry") { Layer("cine_engine"); Layer("cine_air"); }
            else if (shot == "l03_chute") Layer("cine_air", .65f);
            else if (shot == "l04_land") { Layer("cine_engine"); Layer("cine_air", .35f); }
            else if (shot == "l05_base") { Layer("cine_wind"); Layer("cine_base"); }
        }

        /// <summary>A heavy hit on the hull: the synthesised crash layered with the recorded explosion and metal
        /// impacts, on its own voice, with the music ducked so it lands.</summary>
        public void Impact(float vol = 1f)
        {
            if (_impact == null) return;
            if (_lastPlayed.TryGetValue("impact", out var last) && Time.unscaledTime - last < 0.25f) return;
            _lastPlayed["impact"] = Time.unscaledTime;
            if (_clips.TryGetValue("crash", out var c)) _impact.PlayOneShot(c, vol);
            if (_clips.TryGetValue("crash_boom", out c)) _impact.PlayOneShot(c, vol);
            if (_clips.TryGetValue("crash_metalA", out c)) _impact.PlayOneShot(c, vol * 0.9f);
            if (_clips.TryGetValue("crash_metalB", out c)) _impact.PlayOneShot(c, vol * 0.7f);
            GetComponent<StageMusicPlayer>()?.DuckFor(1.6f);
        }

        /// <summary>The red-alert alarm for the cutscenes: the klaxon plus the ship's alarm recording at full level.
        /// Off again returns the recording to its normal in-game level and stops the klaxon.</summary>
        public void CinematicAlarm(bool on)
        {
            _cineAlarm = on;
            Loop("klaxon", on);
            if (on) Loop("ship_alarm", true);
            ApplyVolumes();
        }

        public void StopCinematicAudio(bool immediate = false)
        {
            foreach (var id in new[] { "cine_engine", "cine_air", "cine_wind", "cine_base" })
            {
                _cinematicTargets[id] = 0f;
                if (immediate) { _cinematicGains[id] = 0f; Loop(id, false); }
            }
        }

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
            if (_collected != null) _collected.volume = Settings.Master * Settings.Effects * _collectedLevel;
            if (_srcVar != null) _srcVar.volume = _src.volume;
            if (_impact != null) _impact.volume = _src.volume;
            foreach (var kv in _loops)
            {
                float b = _loopBase[kv.Key];
                float gain = _cinematicGains.TryGetValue(kv.Key, out var cineGain) ? cineGain : 1f;
                if (_cineAlarm && kv.Key == "ship_alarm") gain *= 2.8f;   // the recording is quiet (about -30 dB): full level in the cutscene
                kv.Value.volume = Settings.Master * Mathf.Abs(b) * (b < 0 ? Settings.Ambience : Settings.Effects) * gain;
            }
        }

        /// <summary>Play a one-shot. The same sound never stacks faster than its cool-down.</summary>
        public void Play(string id, float vol = 1f)
        {
            if (id == "blip") return;   // no sound when a dialogue line appears or advances (the user asked to remove it)
            if (id == "alarm" && _loops.TryGetValue("ship_alarm", out var shipAlarm) && shipAlarm.isPlaying) return;
            bool footstep = id == "step" || id.StartsWith("step");
            bool reward = id == "collected" || id == "complete";
            if (id == "step" && _clips.ContainsKey("step0")) id = "step" + Rng.Next(4);   // real footsteps, varied
            if (_src == null || !_clips.TryGetValue(id, out var c)) return;
            float cool = reward ? .18f : id == "hover" ? .08f : footstep ? .12f : .05f;
            string family = footstep ? "step" : reward ? "reward" : id;
            if (_lastPlayed.TryGetValue(family, out var last) && Time.unscaledTime - last < cool) return;
            _lastPlayed[family] = Time.unscaledTime;
            if (reward)
            {
                // Short, boosted user recording; one reward voice avoids overlapping transients.
                _collectedLevel = .95f * vol;
                _collected.Stop();
                _collected.clip = c;
                ApplyVolumes();
                _collected.Play();
                GetComponent<StageMusicPlayer>()?.DuckFor(1.1f);
                return;
            }
            if (footstep) vol *= .75f;
            if (id == "hover") vol *= .16f; else if (id == "click") vol *= .38f; else if (id == "open" || id == "close" || id == "good" || id == "ship" || id == "drone") vol *= .55f;
            if (_pitchJitter > 0 && _srcVar != null)
            {
                // menu sounds: a small random pitch change so repeated ticks never sound identical
                _srcVar.pitch = 1f + ((float)Rng.NextDouble() * 2f - 1f) * _pitchJitter;
                _srcVar.PlayOneShot(c, vol);
                return;
            }
            _src.PlayOneShot(c, vol);
        }

        float _pitchJitter;

        /// <summary>Play a one-shot with a random pitch of ±jitter (0.03 = ±3%).</summary>
        public void PlayVaried(string id, float vol = 1f, float jitter = 0.03f)
        {
            _pitchJitter = jitter;
            try { Play(id, vol); } finally { _pitchJitter = 0; }
        }

        /// <summary>Start or stop a looping sound ("heart", "siren", "rumble", "hum").</summary>
        public void Loop(string id, bool on)
        {
            if (!_loops.TryGetValue(id, out var a) || a == null) return;   // the source is already gone while play mode shuts down
            if (on && !a.isPlaying) a.Play();
            else if (!on && a.isPlaying) a.Stop();
        }
    }
}
