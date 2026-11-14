// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do a full-screen grade, music that follows the danger, and small particles (dust, smoke, sparks)
//           make the ship and the Mars base feel tense and alive without hiding information?
// Date: 2026-11-15
using UnityEngine;
using Fault = TitanGate.Core.LastDock.Fault;
using Outcome = TitanGate.Core.LastDock.Outcome;

namespace TitanGate.Game
{
    /// <summary>
    /// Phase 4 polish for the top-down modes (ship = 1, LAST DOCK base = 2): the post-processing grade (LdPostFx),
    /// procedural music with three layers (calm, tension, storm) that cross-fade, and particle effects.
    /// </summary>
    public partial class GameFlow
    {
        LdPostFx _ldFx;
        float _ldDangerPulse;

        // ------------------------------------------------------------------ post-processing

        void LdPolishUpdate(float dt)
        {
            if (_bMode == 0 || _bCam == null) return;
            if (_ldFx == null || _ldFx.gameObject != _bCam.gameObject) _ldFx = _bCam.gameObject.GetComponent<LdPostFx>() ?? _bCam.gameObject.AddComponent<LdPostFx>();
            bool station = _bMode == 2 && _ld != null && _ld.Outcome == Outcome.Ongoing;
            bool danger = station && (_ld.ActiveFault != Fault.None || _ld.Health <= 1) || _bMode == 1 && _sScramble && _sLeft < 15f;
            _ldDangerPulse += dt * (danger ? 3.2f : 0);
            float pulse = danger ? 0.35f + 0.35f * Mathf.Sin(_ldDangerPulse) : 0f;
            _ldFx.danger = Mathf.Lerp(_ldFx.danger, Settings.ReducedMotion && danger ? 0.4f : pulse, dt * 6);
            _ldFx.dark = Mathf.Lerp(_ldFx.dark, station && _ld.Battery <= 0 ? 1f : station && _ld.Battery == 1 ? 0.45f : 0f, dt * 2);
            _ldFx.storm = Mathf.Lerp(_ldFx.storm, station && _ld.StormToday ? 1f : 0f, dt * 2);
            _ldFx.vignette = _bMode == 1 && _sScramble ? 1.6f : 1.1f;
            _ldFx.tint = _bMode == 2 && BIsOutside(_bPos) ? new Color(1.06f, 0.96f, 0.9f) : Color.white;
            LdMusicUpdate(dt);
            LdVfxUpdate(dt);
        }

        // ------------------------------------------------------------------ music (generated, no audio files)

        AudioSource _musCalm, _musTension, _musStorm;
        const int MusRate = 22050;

        AudioSource MakeMusic(string name, System.Func<float, float> wave, float seconds)
        {
            int n = Mathf.CeilToInt(seconds * MusRate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)MusRate), -1f, 1f) * 0.5f;
            int edge = 2048; for (int i = 0; i < edge; i++) { float k = i / (float)edge; data[i] *= k; data[n - 1 - i] *= k; }
            var clip = AudioClip.Create(name, n, 1, MusRate, false);
            clip.SetData(data, 0);
            var src = _sfx.gameObject.AddComponent<AudioSource>();
            src.clip = clip; src.loop = true; src.volume = 0; src.playOnAwake = false;
            src.Play();
            return src;
        }

        static float Sn(float t, float f) => Mathf.Sin(t * 2 * Mathf.PI * f);
        static float Hz(int semitonesFromA2) => 110f * Mathf.Pow(2f, semitonesFromA2 / 12f);

        AudioSource MusicFrom(AudioClip clip)
        {
            var src = _sfx.gameObject.AddComponent<AudioSource>();
            src.clip = clip; src.loop = true; src.volume = 0; src.playOnAwake = false; src.Play();
            return src;
        }

        /// <summary>A one-shot music cue (rescued / the end), at the music volume.</summary>
        void LdStinger(string file)
        {
            var clip = Resources.Load<AudioClip>("Audio/Music/" + file);
            if (clip == null || _sfx == null) return;
            var src = _sfx.gameObject.AddComponent<AudioSource>();
            src.clip = clip; src.volume = Settings.Master * Settings.Ambience; src.Play();
            Destroy(src, clip.length + 0.5f);
        }

        void LdMusicEnsure()
        {
            if (_musCalm != null) return;
            // licensed CC0 tracks (Dark Sci-Fi Audio Pack by SRG774, docs/AUDIO_CREDITS.md); the generated layers below are only a fallback
            var calm = Resources.Load<AudioClip>("Audio/Music/sector"); var tense = Resources.Load<AudioClip>("Audio/Music/urgent"); var storm = Resources.Load<AudioClip>("Audio/Music/airy");
            if (calm != null && tense != null && storm != null) { _musCalm = MusicFrom(calm); _musTension = MusicFrom(tense); _musStorm = MusicFrom(storm); return; }
            // calm: a slow pad over Am – F – C – G, 4 s per chord, with a soft high bell every 2 s
            int[][] chords = { new[] { 0, 3, 7, 12 }, new[] { -4, 0, 3, 8 }, new[] { 3, 7, 10, 15 }, new[] { -2, 2, 5, 10 } };
            _musCalm = MakeMusic("mus_calm", t =>
            {
                int ci = (int)(t / 4f) % 4; float local = t % 4f;
                float env = Mathf.Clamp01(local / 0.8f) * Mathf.Clamp01((4f - local) / 0.8f);
                float v = 0; foreach (var s in chords[ci]) v += Sn(t, Hz(s)) * 0.12f + Sn(t, Hz(s) * 2.005f) * 0.03f;
                float bellT = t % 2f; v += Sn(t, Hz(chords[ci][3] + 24)) * Mathf.Exp(-4f * bellT) * 0.06f;
                return v * env * (0.85f + 0.15f * Sn(t, 0.25f));
            }, 16f);
            // tension: a low pulse at 120 bpm, a ticking clock and an uneasy minor second above
            _musTension = MakeMusic("mus_tension", t =>
            {
                float beat = t % 0.5f, tick = t % 0.25f;
                float v = Sn(t, 55f) * Mathf.Exp(-6f * beat) * 0.45f + Sn(t, 1760f) * Mathf.Exp(-60f * tick) * 0.05f;
                v += (Sn(t, Hz(15)) + Sn(t, Hz(16))) * 0.04f * (0.6f + 0.4f * Sn(t, 0.125f));
                return v;
            }, 8f);
            // storm: wind-like rumble and a shimmering high tone
            var rng = new System.Random(7); float lp = 0;
            _musStorm = MakeMusic("mus_storm", t =>
            {
                lp = lp * 0.985f + (float)(rng.NextDouble() * 2 - 1) * 0.015f;
                return lp * 6f * (0.6f + 0.4f * Sn(t, 0.2f)) + Sn(t, Hz(31)) * 0.03f * (0.5f + 0.5f * Sn(t, 0.5f));
            }, 8f);
        }

        void LdMusicUpdate(float dt)
        {
            LdMusicEnsure();
            bool station = _bMode == 2 && _ld != null && _ld.Outcome == Outcome.Ongoing;
            bool tense = station && (_ld.ActiveFault != Fault.None || _ld.Health <= 1 || _ld.DarkNights > 0) || _bMode == 1 && _sScramble;
            bool storm = station && (_ld.StormToday || _ld.StormTomorrow);
            float vol = _ldEnded ? 0f : Settings.Master * Settings.Ambience * 0.9f;
            float k = dt * 0.8f;
            _musCalm.volume = Mathf.MoveTowards(_musCalm.volume, !tense && !storm ? vol * 0.7f : vol * 0.15f, k);
            _musTension.volume = Mathf.MoveTowards(_musTension.volume, tense ? vol * 0.8f : 0f, k);
            _musStorm.volume = Mathf.MoveTowards(_musStorm.volume, storm ? vol * 0.6f : 0f, k);
        }

        /// <summary>Silence the music when the top-down map goes away.</summary>
        void LdMusicStop()
        {
            foreach (var m in new[] { _musCalm, _musTension, _musStorm }) if (m != null) m.volume = 0;
        }

        // ------------------------------------------------------------------ particles

        Material _ldPartMat;
        ParticleSystem _ldDust, _ldSparks;
        readonly System.Collections.Generic.Dictionary<string, ParticleSystem> _ldSmoke = new System.Collections.Generic.Dictionary<string, ParticleSystem>();

        ParticleSystem MakeParticles(string name, Vector2 at, Vector2 area, Color color, float size, float life, float rate, Vector2 velocity, int order)
        {
            if (_ldPartMat == null)
            {
                _ldPartMat = new Material(Shader.Find("Sprites/Default"));
                _ldPartMat.mainTexture = Art.SoftLight().texture;
            }
            var go = new GameObject(name);
            go.transform.SetParent(_bRoot, false);
            go.transform.position = new Vector3(at.x, at.y, 0);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.startLifetime = life; main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = color; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 400; main.playOnAwake = true;
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(area.x, area.y, 0.01f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(velocity.x * 0.6f, velocity.x * 1.4f); vel.y = new ParticleSystem.MinMaxCurve(velocity.y * 0.6f, velocity.y * 1.4f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient(); grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(0, 1) });
            col.color = grad;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = _ldPartMat; r.sortingOrder = order;
            ps.Play();
            return ps;
        }

        void LdVfxUpdate(float dt)
        {
            if (_bRoot == null) return;
            if (_bMode == 2 && _ld != null)
            {
                // Mars dust drifting over the outside; a storm blows harder and redder
                if (_ldDust == null) _ldDust = MakeParticles("ld_dust", new Vector2(45f, 13f), new Vector2(26f, 26f), new Color(1f, 0.6f, 0.35f, 0.35f), 0.35f, 6f, 6f, new Vector2(-0.4f, 0.05f), 30004);
                var em = _ldDust.emission; em.rateOverTime = _ld.StormToday ? 40f : 6f;
                var main = _ldDust.main; main.startColor = _ld.StormToday ? new Color(1f, 0.45f, 0.2f, 0.55f) : new Color(1f, 0.6f, 0.35f, 0.3f);
                // smoke over every broken machine
                LdSmoke("ld_scrubber", !_ld.ScrubberOk); LdSmoke("ld_o2gen", !_ld.O2GenOk); LdSmoke("ld_fan", !_ld.FanOk && !_ld.BackupCircuit); LdSmoke("ld_thermal", !_ld.ThermalOk);
            }
        }

        void LdSmoke(string spot, bool on)
        {
            if (!_ldSmoke.TryGetValue(spot, out var ps) || ps == null)
            {
                if (!on) return;
                var sp = BSpotById(spot); if (sp == null) return;
                var top = sp.sr != null && sp.sr.sprite != null ? new Vector2(sp.sr.bounds.center.x, sp.sr.bounds.max.y - 0.2f) : sp.pos + Vector2.up;
                ps = MakeParticles("smoke_" + spot, top, new Vector2(0.5f, 0.2f), new Color(0.55f, 0.55f, 0.6f, 0.45f), 0.9f, 2.4f, 5f, new Vector2(0.05f, 0.6f), 30005);
                _ldSmoke[spot] = ps;
            }
            var em = ps.emission; em.enabled = on;
        }

        /// <summary>A burst of sparks where the cadet stands (a repair finished).</summary>
        void LdSparks()
        {
            if (_bRoot == null) return;
            if (_ldSparks == null)
            {
                _ldSparks = MakeParticles("ld_sparks", _bPos, new Vector2(0.4f, 0.3f), new Color(1f, 0.85f, 0.35f, 1f), 0.25f, 0.6f, 0f, new Vector2(0f, 1.6f), 30006);
                var vel = _ldSparks.velocityOverLifetime; vel.x = new ParticleSystem.MinMaxCurve(-2f, 2f); vel.y = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
            }
            _ldSparks.transform.position = new Vector3(_bPos.x, _bPos.y + 0.8f, 0);
            _ldSparks.Emit(24);
        }

        /// <summary>Forget particle systems of the map that was just torn down (EndBase destroys their objects).</summary>
        void LdPolishReset()
        {
            _ldDust = null; _ldSparks = null; _ldSmoke.Clear(); _ldFx = null;
            LdMusicStop();
        }
    }
}
