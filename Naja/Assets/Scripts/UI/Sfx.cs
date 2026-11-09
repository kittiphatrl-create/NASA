// LAST 72 HOURS — tiny sound manager.
// Clips live in Assets/Resources/Audio (Kenney CC0 packs, see KENNEY_CC0_LICENSE.txt).
// Missing clips are skipped silently; the alarm is generated in code.
using System.Collections.Generic;
using UnityEngine;

namespace Last72
{
    public static class Sfx
    {
        static AudioSource oneShot, ambient, alarm;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        public static float Master = 1f;

        public static void Init(GameObject host)
        {
            if (oneShot != null) return;
            oneShot = host.AddComponent<AudioSource>();
            oneShot.playOnAwake = false;
            ambient = host.AddComponent<AudioSource>();
            ambient.loop = true; ambient.playOnAwake = false; ambient.volume = 0.22f;
            alarm = host.AddComponent<AudioSource>();
            alarm.loop = false; alarm.playOnAwake = false; alarm.volume = 0.35f;
            alarm.clip = MakeAlarm();
            if (Camera.main != null && Camera.main.GetComponent<AudioListener>() == null && Object.FindFirstObjectByType<AudioListener>() == null)
                Camera.main.gameObject.AddComponent<AudioListener>();
        }

        static AudioClip Get(string name)
        {
            AudioClip c;
            if (!cache.TryGetValue(name, out c)) { c = Resources.Load<AudioClip>("Audio/" + name); cache[name] = c; }
            return c;
        }

        public static void Play(string name, float volume = 1f, float pitch = 1f)
        {
            if (oneShot == null) return;
            var c = Get(name);
            if (c == null) return;
            if (Mathf.Abs(pitch - 1f) > 0.01f)
            {
                // separate source so pitch doesn't affect other sounds
                var src = oneShot.gameObject.AddComponent<AudioSource>();
                src.pitch = pitch; src.volume = volume * Master; src.clip = c; src.Play();
                Object.Destroy(src, c.length / Mathf.Max(0.1f, pitch) + 0.1f);
                return;
            }
            oneShot.PlayOneShot(c, volume * Master);
        }

        public static void Ambient(bool on)
        {
            if (ambient == null) return;
            if (on && !ambient.isPlaying) { ambient.clip = Get("ambient"); if (ambient.clip != null) ambient.Play(); }
            else if (!on) ambient.Stop();
        }

        public static void Alarm()
        {
            if (alarm == null || alarm.isPlaying) return;
            alarm.volume = 0.3f * Master;
            alarm.Play();
        }

        /// Two-tone klaxon, ~1.2 s, generated so no extra asset is needed.
        static AudioClip MakeAlarm()
        {
            const int rate = 22050;
            int n = (int)(rate * 1.2f);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float f = ((int)(t * 4) % 2 == 0) ? 880f : 660f;
                float env = Mathf.Clamp01(t * 20f) * Mathf.Clamp01((1.2f - t) * 8f);
                // square-ish wave, softened
                float s = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * f * t)) * 0.35f + Mathf.Sin(2 * Mathf.PI * f * t) * 0.25f;
                data[i] = s * env * 0.5f;
            }
            var clip = AudioClip.Create("alarm", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
