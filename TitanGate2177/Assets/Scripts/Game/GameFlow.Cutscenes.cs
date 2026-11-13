// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>
    /// In-engine pixel-art cutscenes: intro, debris impact, a landing per world, an ending per world.
    /// Each is a few short shots on a stage far from the ship; Space, Esc or the Skip button skips.
    /// </summary>
    public partial class GameFlow
    {
        const float StageX = 3000f, StageCamZ = -12f;
        Transform _stage;
        bool _skipCut;
        public bool InCutscene { get; private set; }
        readonly List<Action<float>> _animators = new List<Action<float>>();

        // ------------------------------------------------------------------ stage helpers

        SpriteRenderer S(Sprite s, float x, float y, float z, float scale = 1f, int order = 0, float rot = 0f)
        {
            var go = new GameObject("cs");
            go.transform.SetParent(_stage, false);
            go.transform.position = new Vector3(StageX + x, y, z);
            go.transform.localScale = Vector3.one * scale;
            go.transform.rotation = Quaternion.Euler(0, 0, rot);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            return sr;
        }

        void Clear()
        {
            if (_stage != null) Destroy(_stage.gameObject);
            _stage = new GameObject("Stage").transform;
        }

        /// <summary>Fill the view behind everything. Skies stretch to fit; pixel scenes keep their shape.</summary>
        void Background(Sprite s, float ignored = 0f)
        {
            var sr = S(s, 0, 0, 40, 1f, -200);
            var size = s.bounds.size;
            float w = 60f, h = 34f;   // view at z = 40 with margin for shake
            bool sky = size.x < 40f || size.y < 20f;
            if (sky) sr.transform.localScale = new Vector3(w / size.x, h / size.y, 1);
            else { float k = Mathf.Max(w / size.x, h / size.y); sr.transform.localScale = new Vector3(k, k, 1); }
        }

        static void Pos(Component c, float x, float y) { var p = c.transform.position; c.transform.position = new Vector3(StageX + x, y, p.z); }

        static float Ease(float t) => t * t * (3f - 2f * t);

        /// <summary>One shot: caption, then tick(0..1) every frame for the duration (or until skipped).</summary>
        IEnumerator Shot(float seconds, string captionKey, Action<float> tick)
        {
            if (_skipCut) yield break;
            _ui.SetCaption(captionKey != null && _loc.Has(captionKey) ? _loc.T(captionKey) : "");
            float t = 0;
            while (t < seconds && !_skipCut)
            {
                t += Time.deltaTime;
                tick?.Invoke(Mathf.Clamp01(t / seconds));
                yield return null;
            }
        }

        IEnumerator FadeCut(float a, float s) { if (!_skipCut) yield return Fade(a, s); else { _ui.FadeTo(a, 0); } }

        /// <summary>Run a cutscene by id, then put the camera, HUD and player back.</summary>
        IEnumerator PlayCutscene(string id)
        {
            InCutscene = true;
            _skipCut = false;
            bool hud = _chapter >= 1 && _chapter <= 2;
            _ui.SetHudVisible(false);
            _ui.SetIncident(null, 0);
            _player.gameObject.SetActive(false);
            _orbi.gameObject.SetActive(false);
            var keepPose = _rig.Pose;
            Clear();
            _rig.SnapPose(new Vector3(StageX, 0, StageCamZ));
            _ui.SetCinematic(true);
            _sfx.Loop("hum", false);
            yield return Fade(0f, 0.5f);

            switch (id)
            {
                case "intro": yield return CutIntro(); break;
                case "impact": yield return CutImpact(); break;
                case "land_mars": yield return CutLandMars(); break;
                case "land_moon": yield return CutLandMoon(); break;
                case "land_titan": yield return CutLandTitan(); break;
                case "win_mars": yield return CutWinMars(); break;
                case "win_moon": yield return CutWinMoon(); break;
                case "win_titan": yield return CutWinTitan(); break;
            }

            yield return Fade(1f, _skipCut ? 0.15f : 0.5f);
            _sfx.Loop("rumble", false);
            if (_stage != null) Destroy(_stage.gameObject);
            _stage = null;
            _ui.SetCinematic(false);
            _rig.SnapPose(keepPose);
            _player.gameObject.SetActive(true);
            _orbi.gameObject.SetActive(true);
            _ui.SetHudVisible(hud);
            InCutscene = false;
        }

        void SkipCutscene() { if (InCutscene) _skipCut = true; }

        // ------------------------------------------------------------------ shared pieces

        SpriteRenderer _ship, _flame;

        SpriteRenderer ShipWithFlame(float x, float y, float scale, bool shield = false, bool legs = false, bool flame = true, bool big = false)
        {
            _ship = S(Art.ShipExterior(shield, legs, false), x, y, 0, scale, 10);
            _flame = S(Art.Flame(0, big), 0, 0, 0.01f, 1f, 9);
            _flame.transform.SetParent(_ship.transform, false);
            _flame.transform.localPosition = new Vector3(-3.75f - (big ? 1.4f : 0.9f), 0.1f, 0.01f);
            _flame.enabled = flame;
            return _ship;
        }

        void FlickerFlame(bool big) { if (_flame != null && _flame.enabled) _flame.sprite = Art.Flame((int)(Time.time * 14) % 2, big); }

        List<SpriteRenderer> Stars(int n, float spread = 1f)
        {
            var list = new List<SpriteRenderer>();
            var rng = new System.Random(n);
            for (int i = 0; i < n; i++)
                list.Add(S(Art.Sparkle(), ((float)rng.NextDouble() - 0.5f) * 14 * spread, ((float)rng.NextDouble() - 0.5f) * 8, 5, 1.5f, -150));
            return list;
        }

        List<SpriteRenderer> Puffs(int n, float x, float y, int order = 20)
        {
            var list = new List<SpriteRenderer>();
            for (int i = 0; i < n; i++) list.Add(S(Art.Puff(), x, y, -0.2f, 0.01f, order));
            return list;
        }

        /// <summary>Animate puffs spreading from (x, y) along the ground.</summary>
        static void SpreadPuffs(List<SpriteRenderer> puffs, float x, float y, float t, Color tint)
        {
            for (int i = 0; i < puffs.Count; i++)
            {
                float dir = (i % 2 == 0 ? 1 : -1) * (0.4f + (i % 5) * 0.25f);
                float k = Mathf.Clamp01(t * 1.2f - i * 0.02f);
                Pos(puffs[i], x + dir * k * 3.2f, y + k * (0.3f + (i % 3) * 0.25f));
                puffs[i].transform.localScale = Vector3.one * (0.6f + k * 1.6f);
                puffs[i].color = new Color(tint.r, tint.g, tint.b, (1 - k) * 0.9f);
            }
        }

        // ------------------------------------------------------------------ intro

        IEnumerator CutIntro()
        {
            Background(Art.Nebula());
            var saturn = S(Art.Saturn(), 4.5f, 1.2f, 20, 0.7f, -100);
            var stars = Stars(24);
            ShipWithFlame(-9f, -0.4f, 0.55f);
            _sfx.Loop("rumble", true);
            yield return Shot(4.2f, "cut.intro.1", t =>
            {
                Pos(_ship, Mathf.Lerp(-9f, -1f, Ease(t)), -0.4f + Mathf.Sin(t * 6) * 0.08f);
                FlickerFlame(false);
                for (int i = 0; i < stars.Count; i++) stars[i].color = new Color(1, 1, 1, 0.4f + 0.6f * Mathf.PerlinNoise(i, Time.time * 2));
            });
            var titan = S(Art.TitanDisc(), 6.2f, -0.2f, 18, 0.5f, -99);
            var vortex = S(Art.Vortex(), 6.2f, -0.2f, 17.9f, 0.18f, -98);
            yield return Shot(4.4f, "cut.intro.2", t =>
            {
                Pos(_ship, Mathf.Lerp(-1f, 5.5f, Ease(t)), -0.4f + Mathf.Sin(t * 6) * 0.08f);
                _ship.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 0.12f, t);
                saturn.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.05f, t);
                vortex.transform.rotation = Quaternion.Euler(0, 0, -Time.time * 120);
                vortex.color = new Color(1, 1, 1, 0.5f + 0.5f * Mathf.Sin(Time.time * 4));
                FlickerFlame(false);
            });
        }

        // ------------------------------------------------------------------ impact

        IEnumerator CutImpact()
        {
            Background(Art.Nebula());
            Stars(20);
            ShipWithFlame(-0.5f, -0.3f, 0.6f);
            var rock = S(Art.Debris(), 7f, 4f, -0.1f, 1.4f, 30);
            var streak = S(Art.Streak(), 7f, 4f, -0.05f, 1f, 29);
            streak.transform.rotation = Quaternion.Euler(0, 0, 210);
            var vents = new List<SpriteRenderer>();
            bool hit = false;
            _sfx.Loop("rumble", true);
            yield return Shot(3.4f, "cut.impact.1", t =>
            {
                FlickerFlame(false);
                float k = Mathf.Clamp01(t / 0.5f);
                Pos(rock, Mathf.Lerp(7f, 0.05f, k), Mathf.Lerp(4f, -0.05f, k));
                Pos(streak, Mathf.Lerp(7f, 0.05f, k) + 1.6f, Mathf.Lerp(4f, -0.05f, k) + 0.9f);
                if (!hit && k >= 1f)
                {
                    hit = true;
                    rock.enabled = false; streak.enabled = false;
                    _ship.sprite = Art.ShipExterior(false, false, true);
                    _sfx.Play("thud"); _sfx.Play("alarm");
                    _rig.Shake(0.8f, 0.2f);
                    _ui.Flash(new Color(1f, 0.9f, 0.7f, 0.35f));
                    for (int i = 0; i < 26; i++) vents.Add(S(i % 3 == 0 ? Art.Spark(i % 2) : Art.Puff(), 0.05f, -0.05f, -0.2f, 0.5f, 31));
                }
                if (hit)
                {
                    float h = (t - 0.5f) / 0.5f;
                    for (int i = 0; i < vents.Count; i++)
                    {
                        float a = (i * 37 % 120 - 60) * Mathf.Deg2Rad + Mathf.PI / 2;
                        float d = Mathf.Repeat(h * 2.2f + i * 0.07f, 1f) * 3f;
                        Pos(vents[i], 0.05f + Mathf.Cos(a) * d * 0.5f, -0.05f + Mathf.Sin(a) * d);
                        vents[i].color = new Color(1, 1, 1, 1 - d / 3f);
                    }
                    Pos(_ship, -0.5f + Mathf.Sin(t * 40) * 0.05f * (1 - t), -0.3f - (t - 0.5f) * 0.3f);
                }
            });
            yield return Shot(2.2f, "cut.impact.2", t =>
            {
                FlickerFlame(false);
                _ship.transform.rotation = Quaternion.Euler(0, 0, -t * 6);
                for (int i = 0; i < vents.Count; i++)
                {
                    float a = (i * 37 % 120 - 60) * Mathf.Deg2Rad + Mathf.PI / 2;
                    float d = Mathf.Repeat(Time.time * 1.4f + i * 0.07f, 1f) * 3f;
                    Pos(vents[i], 0.05f + Mathf.Cos(a) * d * 0.5f, -0.35f + Mathf.Sin(a) * d);
                    vents[i].color = new Color(1, 1, 1, 1 - d / 3f);
                }
            });
        }

        // ------------------------------------------------------------------ landings

        IEnumerator EntryShot(string caption, Sprite sky, float seconds)
        {
            Clear();
            Background(sky, 30f);
            ShipWithFlame(-6f, 3.5f, 0.45f, shield: true, flame: false);
            _ship.transform.rotation = Quaternion.Euler(0, 0, -18);
            var plasma = S(Art.Plasma(0), 0, 0, -0.02f, 1f, 12);
            plasma.transform.SetParent(_ship.transform, false);
            plasma.transform.localPosition = new Vector3(0.6f, -2.2f, -0.02f);
            plasma.transform.localScale = new Vector3(2.2f, 1.6f, 1);
            var streaks = new List<SpriteRenderer>();
            for (int i = 0; i < 8; i++) { var st = S(Art.Streak(), 0, 0, -0.1f, 1.2f, 8); st.transform.rotation = Quaternion.Euler(0, 0, 162); streaks.Add(st); }
            _sfx.Play("whoosh");
            _sfx.Loop("rumble", true);
            _rig.Shake(seconds, 0.06f);
            yield return Shot(seconds, caption, t =>
            {
                Pos(_ship, Mathf.Lerp(-6f, 2.5f, t), Mathf.Lerp(3.5f, -0.8f, t));
                plasma.sprite = Art.Plasma((int)(Time.time * 16) % 2);
                for (int i = 0; i < streaks.Count; i++)
                {
                    float k = Mathf.Repeat(Time.time * 1.6f + i * 0.13f, 1f);
                    var p = _ship.transform.position;
                    streaks[i].transform.position = new Vector3(p.x - 1f - k * 6f, p.y + (i - 4) * 0.25f + k * 2f, -0.1f);
                    streaks[i].color = new Color(1, 1, 1, 1 - k);
                }
            });
        }

        IEnumerator ChuteShot(string caption, Sprite sky, Sprite ridge, float seconds, bool dropShield, bool rain = false)
        {
            Clear();
            Background(sky, 30f);
            var r = S(ridge, 0, -8f, 10, 1.4f, -50);
            ShipWithFlame(0, -0.4f, 0.4f, shield: !dropShield, flame: false);
            var chute = S(Art.Parachute(), 0, 2.1f, 0.05f, 0.7f, 11);
            SpriteRenderer plate = dropShield ? S(Art.ShipExterior(true, false, false), 0, 0, 0.1f, 0.45f, 9) : null;
            if (plate != null) plate.sprite = ShieldOnly();
            var drops = new List<SpriteRenderer>();
            if (rain) for (int i = 0; i < 40; i++) drops.Add(S(Art.Rain(0), 0, 0, -0.5f, 1.5f, 30));
            _sfx.Play("whoosh");
            yield return Shot(seconds, caption, t =>
            {
                float sway = Mathf.Sin(Time.time * 1.7f) * 6f;
                _ship.transform.rotation = Quaternion.Euler(0, 0, sway * 0.6f);
                chute.transform.rotation = Quaternion.Euler(0, 0, sway * 0.3f);
                Pos(_ship, Mathf.Sin(Time.time * 1.7f) * 0.25f, -0.4f - t * 0.3f);
                Pos(chute, Mathf.Sin(Time.time * 1.7f) * 0.32f, 1.6f - t * 0.3f);
                chute.transform.localScale = Vector3.one * Mathf.Lerp(0.15f, 0.7f, Mathf.Clamp01(t * 4));
                Pos(r, 0, Mathf.Lerp(-8f, -5.6f, t));
                if (plate != null) { Pos(plate, Mathf.Sin(Time.time * 1.7f) * 0.25f + t * 0.5f, 0.0f - t * t * 6f); plate.transform.rotation = Quaternion.Euler(0, 0, t * 70); }
                for (int i = 0; i < drops.Count; i++)
                {
                    float k = Mathf.Repeat(Time.time * 0.9f + i * 0.071f, 1f);
                    Pos(drops[i], -7f + (i * 0.37f) % 14f - k * 1.2f, 4f - k * 8f);
                    drops[i].color = new Color(0.8f, 0.9f, 1f, 0.6f);
                }
            });
        }

        static Sprite _shieldOnly;
        static Sprite ShieldOnly()
        {
            if (_shieldOnly != null) return _shieldOnly;
            var c = new PixelCanvas(96, 10);
            c.Rect(2, 2, 92, 6, PixelCanvas.Hex("#5b4636")); c.Rect(2, 2, 92, 2, PixelCanvas.Hex("#3d2e24"));
            c.Outline(Pal.Ink);
            _shieldOnly = c.ToSprite(new Vector2(0.5f, 0.5f));
            return _shieldOnly;
        }

        /// <summary>Tail-down powered descent onto the ground, with dust.</summary>
        IEnumerator RocketDownShot(string caption, Sprite sky, Sprite ground, Color dust, float seconds, bool touchdown, Sprite extra = null, Vector3 extraPos = default, float extraScale = 1f)
        {
            Clear();
            Background(sky, 30f);
            if (extra != null) S(extra, extraPos.x, extraPos.y, extraPos.z, extraScale, -60);
            var g = S(ground, 0, -5.2f, 0.5f, 1.2f, -40);
            g.transform.rotation = Quaternion.identity;
            ShipWithFlame(0, 2.0f, 0.36f, legs: true, flame: true, big: true);
            _ship.transform.rotation = Quaternion.Euler(0, 0, 90);
            var puffs = Puffs(18, 0, -2.6f);
            _sfx.Loop("rumble", true);
            yield return Shot(seconds, caption, t =>
            {
                FlickerFlame(true);
                float y = touchdown ? Mathf.Lerp(0.9f, -0.95f, Ease(t)) : Mathf.Lerp(1.8f, 0.9f, t);
                Pos(_ship, Mathf.Sin(Time.time * 3) * 0.05f, y);
                Pos(g, 0, touchdown ? -4.4f : Mathf.Lerp(-5.2f, -4.4f, t));
                if (touchdown || t > 0.4f) SpreadPuffs(puffs, 0, -2.5f, Mathf.Repeat(Time.time * 0.8f, 1f), dust);
                if (touchdown && t > 0.92f && _flame.enabled) { _flame.enabled = false; _sfx.Play("thud"); _sfx.Loop("rumble", false); _rig.Shake(0.4f, 0.12f); }
            });
        }

        IEnumerator CutLandMars()
        {
            var sky = Art.SkyGradient("marsentry", "#2a120c", "#c46a3c");
            yield return EntryShot("cut.mars.1", sky, 3.2f);
            yield return ChuteShot("cut.mars.2", Art.MarsSky(), Art.MarsRidge(0), 3.0f, false);
            yield return ChuteShot("cut.mars.3", Art.MarsSky(), Art.MarsRidge(1), 2.6f, true);
            yield return RocketDownShot("cut.mars.4", Art.MarsSky(), Art.MarsRidge(1), new Color(0.85f, 0.55f, 0.35f), 2.6f, false);
            yield return RocketDownShot("cut.mars.5", Art.MarsSky(), Art.MarsRidge(1), new Color(0.85f, 0.55f, 0.35f), 2.6f, true);
        }

        IEnumerator CutLandMoon()
        {
            Clear();
            Background(Art.Nebula());
            var moon = S(Art.MoonDisc(), -3f, -0.5f, 10, 0.3f, -60);
            var earth = S(Art.Earth(), 4f, 1.5f, 10, 1.2f, -60);
            ShipWithFlame(1f, 0.6f, 0.3f, flame: false);
            _ship.transform.rotation = Quaternion.Euler(0, 0, 180);
            yield return Shot(3.2f, "cut.moon.1", t =>
            {
                moon.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.3f, Ease(t));
                earth.transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.5f, t);
                Pos(earth, Mathf.Lerp(4f, 5.5f, t), 1.5f);
                Pos(_ship, Mathf.Lerp(1f, -0.5f, t), 0.6f);
            });
            Clear();
            Background(Art.Nebula());
            var surface = S(Art.MoonDisc(), 0, -16f, 6, 6f, -60);
            ShipWithFlame(-4f, 1f, 0.45f, flame: true, big: true);
            _ship.transform.rotation = Quaternion.Euler(0, 0, 180);
            _sfx.Loop("rumble", true);
            yield return Shot(2.8f, "cut.moon.2", t =>
            {
                FlickerFlame(true);
                Pos(_ship, Mathf.Lerp(-4f, 3f, 1 - (1 - t) * (1 - t)), 1f - t * 0.4f);
                surface.transform.rotation = Quaternion.Euler(0, 0, t * 4f);
            });
            Clear();
            Background(Art.MoonSky(), 6f);
            var ridge = S(Art.MoonRidge(0), 0, -5f, 10, 1.4f, -50);
            ShipWithFlame(0, 1.5f, 0.42f, flame: true, big: true);
            yield return Shot(2.6f, "cut.moon.3", t =>
            {
                FlickerFlame(true);
                _ship.transform.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(180, 90, Ease(t)));
                Pos(ridge, 0, Mathf.Lerp(-6f, -4.6f, t));
            });
            var e = Art.Earth();
            yield return RocketDownShot("cut.moon.4", Art.MoonSky(), Art.MoonRidge(1), new Color(0.75f, 0.76f, 0.8f), 2.6f, false, e, new Vector3(4.6f, 0.2f, 20), 0.8f);
            yield return RocketDownShot("cut.moon.5", Art.MoonSky(), Art.MoonRidge(1), new Color(0.75f, 0.76f, 0.8f), 2.6f, true, e, new Vector3(4.6f, 0.2f, 20), 0.8f);
        }

        IEnumerator CutLandTitan()
        {
            Clear();
            Background(Art.Nebula());
            var jup = S(Art.Jupiter(), 0, -0.3f, 10, 0.9f, -60);
            ShipWithFlame(-7f, 2.5f, 0.25f, flame: true);
            _sfx.Play("whoosh");
            yield return Shot(3.6f, "cut.titan.1", t =>
            {
                FlickerFlame(false);
                float a = Mathf.Lerp(160f, -20f, t) * Mathf.Deg2Rad;
                float r = 4.2f;
                Pos(_ship, Mathf.Cos(a) * r * 1.4f, Mathf.Sin(a) * r * 0.6f - 0.3f);
                _ship.transform.rotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg - 90f);
                jup.transform.rotation = Quaternion.Euler(0, 0, t * 3);
            });
            Clear();
            var bg = S(Art.Nebula(), 0, 0, 40, 1.2f, -200);
            var stars = Stars(40, 2f);
            ShipWithFlame(0, 0, 0.35f, flame: false);
            yield return Shot(3.0f, "cut.titan.2", t =>
            {
                Pos(bg, -t * 8f, 0);
                for (int i = 0; i < stars.Count; i++)
                {
                    var p = stars[i].transform.position;
                    float x = Mathf.Repeat(p.x - StageX - Time.deltaTime * 9f + 14f, 28f) - 14f;
                    stars[i].transform.position = new Vector3(StageX + x, p.y, p.z);
                    stars[i].transform.localScale = new Vector3(3f, 1f, 1f);
                }
                _ship.color = Color.Lerp(Color.white, new Color(0.6f, 0.65f, 0.8f), Mathf.PingPong(t * 2, 1));
            });
            var haze = Art.SkyGradient("titanentry", "#5a3812", "#d9a24c");
            yield return EntryShot("cut.titan.3", haze, 3.0f);
            yield return ChuteShot("cut.titan.4", Art.TitanSky(0), Art.TitanRidge(0), 3.4f, true, rain: true);
            yield return RocketDownShot("cut.titan.5", Art.TitanSky(0), Art.TitanRidge(1), new Color(0.6f, 0.45f, 0.3f), 2.6f, true, Art.Gate(0), new Vector3(4f, -1f, 12f), 0.9f);
        }

        // ------------------------------------------------------------------ endings

        IEnumerator LiftoffShot(string caption, Sprite sky, Sprite ground, Color dust, float seconds)
        {
            Clear();
            Background(sky, 30f);
            var g = S(ground, 0, -4.4f, 0.5f, 1.2f, -40);
            ShipWithFlame(0, -0.95f, 0.36f, legs: true, flame: true, big: true);
            _ship.transform.rotation = Quaternion.Euler(0, 0, 90);
            var puffs = Puffs(22, 0, -2.6f);
            _sfx.Play("thud");
            _sfx.Loop("rumble", true);
            _rig.Shake(seconds, 0.08f);
            yield return Shot(seconds, caption, t =>
            {
                FlickerFlame(true);
                float lift = t < 0.3f ? 0 : (t - 0.3f) * (t - 0.3f) * 14f;
                Pos(_ship, Mathf.Sin(Time.time * 30) * 0.03f, -0.95f + lift);
                Pos(g, 0, -4.4f - lift * 0.3f);
                SpreadPuffs(puffs, 0, -2.5f, Mathf.Repeat(Time.time * 0.7f, 1f), dust);
            });
        }

        IEnumerator CutWinMars()
        {
            yield return LiftoffShot("cut.win.mars.1", Art.MarsSky(), Art.MarsRidge(1), new Color(0.85f, 0.55f, 0.35f), 3.2f);
            Clear();
            Background(Art.Nebula());
            var mars = S(Art.MarsPlanet(), 0, -3f, 10, 2.4f, -60);
            ShipWithFlame(0, -0.5f, 0.35f, flame: true);
            _ship.transform.rotation = Quaternion.Euler(0, 0, 90);
            yield return Shot(3.0f, "cut.win.mars.2", t =>
            {
                FlickerFlame(false);
                mars.transform.localScale = Vector3.one * Mathf.Lerp(2.4f, 0.8f, t);
                Pos(mars, 0, Mathf.Lerp(-3f, -2f, t));
                Pos(_ship, 0, Mathf.Lerp(-0.5f, 2.2f, t));
                _ship.transform.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(90, 20, t));
            });
            Clear();
            Background(Art.Nebula());
            S(Art.Saturn(), 4f, 1f, 20, 0.6f, -60);
            ShipWithFlame(-6f, -0.5f, 0.45f, flame: true);
            yield return Shot(3.2f, "cut.win.mars.3", t =>
            {
                FlickerFlame(false);
                Pos(_ship, Mathf.Lerp(-6f, 3f, Ease(t)), -0.5f);
                _ship.transform.localScale = Vector3.one * Mathf.Lerp(0.45f, 0.2f, t);
            });
        }

        IEnumerator CutWinMoon()
        {
            yield return LiftoffShot("cut.win.moon.1", Art.MoonSky(), Art.MoonRidge(1), new Color(0.75f, 0.76f, 0.8f), 3.0f);
            Clear();
            Background(Art.Nebula());
            S(Art.MoonDisc(), -4f, -2f, 10, 1.6f, -60);
            ShipWithFlame(-3f, 0, 0.35f, flame: true);
            var rescue = S(Art.RescueShip(), 7f, 0.1f, 0, 0.45f, 10);
            rescue.flipX = true;
            yield return Shot(3.4f, "cut.win.moon.2", t =>
            {
                FlickerFlame(false);
                Pos(_ship, Mathf.Lerp(-3f, -1.05f, Ease(t)), 0);
                Pos(rescue, Mathf.Lerp(7f, 1.35f, Ease(t)), 0.1f);
                if (t > 0.97f) _flame.enabled = false;
            });
            Clear();
            Background(Art.Nebula());
            var earth = S(Art.Earth(), 0, 0, 10, 0.6f, -60);
            yield return Shot(3.2f, "cut.win.moon.3", t =>
            {
                earth.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 3.2f, Ease(t));
                earth.transform.rotation = Quaternion.Euler(0, 0, t * 10);
            });
        }

        IEnumerator CutWinTitan()
        {
            Clear();
            Background(Art.TitanSky(0.3f), 8f);
            S(Art.TitanRidge(1), 0, -4.8f, 10, 1.3f, -50);
            const float gateY = -1.3f;
            var gate = S(Art.Gate(0), 0, gateY, 0, 1.1f, 10);
            yield return Shot(3.4f, "cut.win.titan.1", t =>
            {
                gate.sprite = Art.Gate(Mathf.RoundToInt(Mathf.Clamp01(t * 1.1f) * 100) / 10 * 10);
                if (Mathf.Repeat(t * 12, 1f) < 0.08f) _sfx.Play("click");
            });
            float ringY = gateY + 38f / 16f * 1.1f;   // gate sprite is anchored at its base; ring centre is 38 px up
            var vortex = S(Art.Vortex(), 0, ringY, -0.1f, 0.05f, 11);
            _sfx.Play("card");
            yield return Shot(3.0f, "cut.win.titan.2", t =>
            {
                vortex.transform.rotation = Quaternion.Euler(0, 0, -Time.time * 200);
                vortex.transform.localScale = Vector3.one * Mathf.Lerp(0.05f, 0.3f, Ease(t));
            });
            ShipWithFlame(-8f, 2.2f, 0.25f, flame: true);
            yield return Shot(3.0f, "cut.win.titan.3", t =>
            {
                FlickerFlame(false);
                vortex.transform.rotation = Quaternion.Euler(0, 0, -Time.time * 260);
                Pos(_ship, Mathf.Lerp(-8f, 0f, Ease(t)), Mathf.Lerp(2.2f, ringY, Ease(t)));
                _ship.transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 0.02f, t * t);
                if (t > 0.85f) _ui.Flash(new Color(0.8f, 1f, 1f, (t - 0.85f) * 5f));
            });
        }

        // ------------------------------------------------------------------ title stage

        /// <summary>The animated key art behind the main menu.</summary>
        void BuildTitleStage()
        {
            Clear(); _rig.SnapPose(new Vector3(StageX, 0, StageCamZ));
            Background(Art.QuietSpace());
            var mars = S(Art.TitleMars(), 5.2f, 1.8f, 14, .62f, -100);
            var stars = Stars(12);
            foreach (var star in stars) if (star.transform.position.x < StageX + .5f) star.enabled = false;
            ShipWithFlame(3.1f, -1.05f, .68f, flame: true);
            _ship.transform.rotation = Quaternion.Euler(0, 0, 12);
            var ship = _ship; var flame = _flame;
            var drone = S(Art.Orbi(0), 1.4f, .2f, -.5f, .75f, 12);
            _titleDrone = drone;
            _animators.Clear();
            _animators.Add(t =>
            {
                float motion = Settings.ReducedMotion ? 0 : 1;
                float boost = Mathf.Clamp01(_titleBoostUntil - Time.unscaledTime);
                Pos(ship, 3.1f + (Mathf.Sin(t * .3f) * .15f + boost * .5f) * motion, -1.05f + Mathf.Sin(t * .8f) * .08f * motion);
                Pos(drone, 1.4f + Mathf.Sin(t * .7f) * .12f * motion, .2f + Mathf.Sin(t * 1.7f) * .09f * motion);
                drone.transform.rotation = Quaternion.Euler(0, 0, Time.unscaledTime < _titleWaveUntil && !Settings.ReducedMotion ? Mathf.Sin(t * 12) * 12 : 0);
                drone.sprite = Art.Orbi(t % 3.2f < .12f ? 2 : 0);
                if (flame != null) flame.sprite = Art.Flame((int)(t * 8) % 2, false);
                mars.transform.localScale = Vector3.one * .62f;
            });
        }

        void TickAnimators()
        {
            if (_animators.Count == 0) return;
            float t = Time.time;
            for (int i = 0; i < _animators.Count; i++) _animators[i](t);
        }
    }
}
