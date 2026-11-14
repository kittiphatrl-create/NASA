using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        IEnumerator SMentorAdvice()
        {
            _busy = true;
            yield return Say("mentor", _sScramble ? "story.mentor.emergency" : "story.mentor.tip");
            _busy = false;
        }

        /// <summary>
        /// Opening: Earth in 2177, then a press conference at mission control where the cadet is chosen and walks up
        /// beside Commander Arun, the jump to Mars, and the two of them in the Kestrel cockpit.
        /// </summary>
        IEnumerator CutStoryIntro()
        {
            Background(Art.QuietSpace()); Stars(18);
            var earth = S(Art.EarthHD(), -2.6f, -1.2f, 8, 1.25f, -60);
            var orbit = S(Art.OrbitStation(0), 6.5f, 2.2f, 0, 1.15f, 2);
            yield return Shot(6f, "story.intro.earth", t =>
            {
                // the station glides over the lit side of Earth, its lights blinking; Earth turns very slowly
                Pos(orbit, Mathf.Lerp(6.5f, 1.2f, Ease(t)), 2.2f - t * 0.35f);
                orbit.sprite = Art.OrbitStation((int)(t * 12) % 2);
                orbit.transform.rotation = Quaternion.Euler(0, 0, -6 + t * 4);
                float k = 1.25f + t * 0.06f; earth.transform.localScale = new Vector3(k, k, 1);
            });

            // ---- press conference
            Clear(); Background(Art.PressHall());
            var podium = S(Art.Podium(), -0.6f, -1.95f, 0, 1.25f, 4);
            var official = S(Art.Crew("official", 0, -1), -0.6f, -1.7f, 0.1f, 1.3f, 3);
            var mentor = S(Art.Crew("mentor", 0, -1), 3.0f, -1.95f, 0, 1.3f, 5);
            var cadet = S(Art.Crew("cadet", 2, 0), -7.5f, -1.95f, 0, 1.3f, 6);
            var crowd = S(Art.Audience(), 0, -3.9f, -0.5f, 1.2f, 20);
            var flashes = new List<SpriteRenderer>();
            for (int i = 0; i < 6; i++) { var f = S(Art.Sparkle(), (i - 2.5f) * 2.2f, -3.4f, -0.6f, 3f, 21); f.enabled = false; flashes.Add(f); }
            void Flash(float t)
            {
                for (int i = 0; i < flashes.Count; i++) flashes[i].enabled = Mathf.Repeat(t * 7 + i * 0.37f, 1f) < 0.08f;
            }
            yield return Shot(4.5f, "story.intro.press1", t => { Flash(t); official.sprite = Art.Crew("official", 0, (t * 6) % 2 < 1 ? -1 : -2); });
            yield return Shot(5f, "story.intro.press2", t =>
            {
                Flash(t);
                float x = Mathf.Lerp(-7.5f, 1.2f, Ease(Mathf.Clamp01(t * 1.4f)));
                bool walking = t < 0.7f;
                cadet.sprite = walking ? Art.Crew("cadet", 2, (int)(t * 40) % 8) : Art.Crew("cadet", 0, -1, false, false, t > 0.8f);
                Pos(cadet, x, -1.95f);
            });
            yield return Shot(5f, "story.intro.press3", t =>
            {
                Flash(t * 0.5f);
                mentor.sprite = Art.Crew("mentor", 0, -1, false, false, t > 0.3f && t < 0.7f);
                cadet.sprite = Art.Crew("cadet", 0, -1, false, false, t > 0.5f);
            });

            // ---- the jump to Mars
            Clear(); Background(Art.QuietSpace()); Stars(14);
            var gate = S(Art.Vortex(), 4, 0, 0, .4f, 1);
            ShipWithFlame(-6, -.7f, .4f, flame: true);
            yield return Shot(5f, "story.intro.jump", t => { Pos(_ship, Mathf.Lerp(-6, 2, Ease(t)), -.7f); gate.transform.rotation = Quaternion.Euler(0, 0, -t * 80); FlickerFlame(false); });

            // ---- in the cockpit
            Clear(); Background(Art.CockpitInterior(false));
            var m2 = S(Art.Crew("mentor", 0, -1, false, false, true), -1.8f, -2.3f, 0, 1.5f, 5);
            var c2 = S(Art.Crew("cadet", 0, -1), 1.6f, -2.3f, 0, 1.5f, 6);
            yield return Shot(5.5f, "story.intro.crew", t => { Pos(m2, -1.8f, -2.3f + Mathf.Sin(t * 5) * .02f); });
            yield return Shot(5f, "story.intro.crew2", t => { m2.sprite = Art.Crew("mentor", 2, -1); c2.sprite = Art.Crew("cadet", 0, (t * 4) % 2 < 1 ? -1 : -2); });
        }

        /// <summary>The release jams: Arun stays at the console, the pod separates, and the Kestrel breaks apart behind it.</summary>
        IEnumerator CutEvacuate()
        {
            Clear(); Background(Art.CockpitInterior(true));
            var mentor = S(Art.CrewAction("mentor", "repair", 0), -1.2f, -2.3f, 0, 1.5f, 2);
            var sparks = new List<SpriteRenderer>();
            for (int i = 0; i < 8; i++) sparks.Add(S(Art.Spark(i % 2), (i - 4) * 1.4f, 1.5f, -0.2f, 2f, 9));
            _sfx.Play("alarm");
            yield return Shot(5f, "story.escape.release", t =>
            {
                mentor.sprite = Art.CrewAction("mentor", "repair", (int)(t * 20) % 4);
                for (int i = 0; i < sparks.Count; i++) { float k = Mathf.Repeat(t * 3 + i * 0.13f, 1f); Pos(sparks[i], (i - 4) * 1.4f + k * 0.6f, 2.6f - k * 3f); sparks[i].enabled = k < 0.6f; }
            });

            Clear(); Background(Art.QuietSpace()); Stars(18);
            ShipWithFlame(-1.5f, .6f, .65f, flame: false); _ship.sprite = Art.ShipExterior(false, false, true);
            var pod = S(Art.PodSide(false), -0.6f, .1f, -.3f, 1.2f, 8);
            yield return Shot(4.5f, "story.escape.signal", t =>
            {
                Pos(pod, Mathf.Lerp(-0.6f, 4.8f, Ease(t)), .1f - t * 1.6f);
                pod.transform.rotation = Quaternion.Euler(0, 0, -t * 40);
                _ship.transform.rotation = Quaternion.Euler(0, 0, -t * 6);
                _ship.color = Color.Lerp(Color.white, new Color(.6f, .55f, .6f), t);
            });

            // the Kestrel breaks apart: fireball, flash, debris; the pod tumbles away in front
            var blast = S(Art.Blast(0), -1.5f, .6f, -0.4f, 1.1f, 12);
            var debris = new List<SpriteRenderer>();
            var dirs = new List<Vector2>();
            var rng = new System.Random(5);
            for (int i = 0; i < 14; i++)
            {
                debris.Add(S(Art.Debris(), -1.5f, .6f, -0.5f, 0.45f + (float)rng.NextDouble() * 0.4f, 13));
                float a = (float)(rng.NextDouble() * Mathf.PI * 2);
                dirs.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (2f + (float)rng.NextDouble() * 4f));
            }
            bool boom = false;
            yield return Shot(5f, "story.escape.boom", t =>
            {
                if (!boom) { boom = true; _sfx.Play("thud"); _sfx.Play("alarm"); _ui.Flash(new Color(1f, 0.9f, 0.7f, 0.85f)); _rig.Shake(0.9f, 0.25f); }
                blast.sprite = Art.Blast(Mathf.Min(5, (int)(t * 9)));
                blast.color = new Color(1, 1, 1, Mathf.Clamp01(1.2f - t * 2.2f));
                _ship.enabled = t < 0.12f;
                for (int i = 0; i < debris.Count; i++)
                {
                    var d = dirs[i] * Ease(Mathf.Clamp01(t * 1.2f));
                    Pos(debris[i], -1.5f + d.x, .6f + d.y);
                    debris[i].transform.rotation = Quaternion.Euler(0, 0, t * 300 * (i % 2 == 0 ? 1 : -1));
                }
                Pos(pod, 4.8f + t * 1.5f, -1.5f - t * 0.6f);
                pod.transform.rotation = Quaternion.Euler(0, 0, -40 - t * 120);
            });
        }

        /// <summary>On Mars the pod rolls up to an old emergency base; a flexible tube reaches out and seals onto its hatch.</summary>
        IEnumerator CutDock()
        {
            Clear(); Background(Art.MarsSky());
            S(Art.MarsRidge(1), 0, -3.8f, 8, 1.3f, -50);
            var ground = S(Art.MarsGround(), 0, -5.0f, 6, 1.4f, -45);
            var baseExt = S(Art.MarsBaseExterior(), 3.0f, -2.3f, 0, 0.75f, 2);
            S(Art.SolarArray(false, false), -1.4f, -2.4f, 0.1f, 0.6f, 1);
            var pod = S(Art.PodSide(true), -8, -1.0f, -.2f, 1.1f, 3);
            var tubes = new List<SpriteRenderer>();
            for (int i = 0; i < 3; i++) { var tseg = S(Art.TubeSegment(), -0.9f - i * 0.75f, -1.8f, -0.1f, 1.0f, 4); tseg.enabled = false; tubes.Add(tseg); }
            var lights = new List<SpriteRenderer>();
            for (int i = 0; i < 3; i++) lights.Add(S(Art.Sparkle(), 0.6f + i * 1.8f, -0.9f, -0.3f, 1.5f, 6));
            yield return Shot(6.5f, "story.dock", t =>
            {
                Pos(pod, Mathf.Lerp(-8f, -4.3f, Ease(Mathf.Clamp01(t * 2f))), -1.0f + (t < 0.5f ? Mathf.Abs(Mathf.Sin(t * 20)) * 0.08f : 0));
                int shown = Mathf.Clamp(Mathf.FloorToInt((t - 0.4f) / 0.12f), 0, tubes.Count);
                for (int i = 0; i < tubes.Count; i++) tubes[i].enabled = i < shown;
                for (int i = 0; i < lights.Count; i++) lights[i].enabled = Mathf.Repeat(t * 2 + i * 0.3f, 1f) < 0.5f;
            });
        }
    }
}
