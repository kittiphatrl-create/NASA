using System.Collections.Generic;
using UnityEngine;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        static readonly int LightBoundsProperty = Shader.PropertyToID("_LightBounds");

        void BBoundRoomLight(SpriteRenderer light, Vector2 source, bool includeWallFace = false)
        {
            var cell = new Vector2Int(Mathf.FloorToInt(source.x), Mathf.FloorToInt(source.y));
            foreach (var (room, _) in _bRooms)
            {
                bool inRoom = room.Contains(cell);
                bool atNorthWall = includeWallFace && cell.y == room.yMax && cell.x >= room.xMin && cell.x < room.xMax;
                if (!inRoom && !atNorthWall) continue;
                var properties = new MaterialPropertyBlock();
                properties.SetVector(LightBoundsProperty, new Vector4(room.xMin - .08f, room.yMin - .08f,
                    room.xMax + .08f, room.yMax + (includeWallFace ? .65f : .08f)));
                light.SetPropertyBlock(properties);
                return;
            }
        }

        float BStationLampPower => _bMode != 2 || _ld == null ? 1f :
            _ld.Battery <= 0 ? .08f : _ld.Battery == 1 ? .48f : 1f;

        float? _sPowerFaultAt;
        bool SPowerFault => _sPowerFaultAt.HasValue;
        float SPowerFaultAge => _bAnim - _sPowerFaultAt.GetValueOrDefault();
        static readonly Color EmergencyRed = new Color(1f, .085f, .045f);
        readonly List<(SpriteRenderer lens, SpriteRenderer spill, Color lensColor, Color spillColor)> _sHullLights =
            new List<(SpriteRenderer, SpriteRenderer, Color, Color)>();
        float SFailedLight(float phase, float period)
        {
            if (Settings.ReducedMotion) return .72f;
            float age = SPowerFaultAge;
            float returnTime = .30f + phase * .65f;
            if (age < returnTime) return Mathf.Lerp(.16f, .035f, Mathf.Clamp01(age / returnTime));
            float t = Mathf.Repeat((age - returnTime) / period + phase, 1f);
            float main = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.03f, .12f, t))
                * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.25f, .41f, t)));
            float retry = .32f * Mathf.Max(0f, 1f - Mathf.Abs(t - .56f) / .095f);
            return Mathf.Max(.09f, Mathf.Max(main, retry));
        }

        void BuildKestrelExteriorLights()
        {
            _sHullLights.Clear();
            _sPowerFaultAt = null;
            var nose = new Color(.72f, .88f, 1f);
            var amber = new Color(1f, .62f, .25f);
            AddHullLight(new Vector2(42.93f, 7.24f), Vector2.right, nose, 1f);
            AddHullLight(new Vector2(42.93f, 15.76f), Vector2.right, nose, 1f);
            float upperEdge = LastDockMode ? 27.93f : 20.93f;
            AddHullLight(new Vector2(15.5f, upperEdge), Vector2.up, new Color(1f, .23f, .19f), .42f);
            AddHullLight(new Vector2(29.5f, 2.07f), Vector2.down, new Color(.27f, 1f, .53f), .42f);
            AddHullLight(new Vector2(27.75f, 2.07f), Vector2.down, amber, .38f);
            AddHullLight(new Vector2(33.75f, 2.07f), Vector2.down, amber, .38f);
            AddHullLight(new Vector2(1.07f, 7.5f), Vector2.left, amber, .32f);
            AddHullLight(new Vector2(1.07f, 14.5f), Vector2.left, amber, .32f);
        }

        void AddHullLight(Vector2 position, Vector2 outwards, Color tint, float intensity)
        {
            var rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(outwards.y, outwards.x) * Mathf.Rad2Deg);
            var spill = BSprite("exterior-light-spill", Art.SoftLight(), position + outwards * .65f, -30001);
            spill.transform.localRotation = rotation;
            spill.transform.localScale = new Vector3(3.6f, 1.8f, 1f) * Mathf.Lerp(.56f, 1f, intensity);
            spill.color = new Color(tint.r, tint.g, tint.b, intensity * .45f);
            if (_bGlowMaterial != null) spill.sharedMaterial = _bGlowMaterial;

            var fitting = BSprite("hull-light-housing", Art.KestrelHullLight(false), position, 15000);
            fitting.transform.localRotation = rotation;
            var lens = BSprite("hull-light-lens", Art.KestrelHullLight(true), position, 15001);
            lens.transform.localRotation = rotation;
            var color = Color.Lerp(tint * .75f, tint, intensity);
            color.a = 1f;
            lens.color = color;
            _sHullLights.Add((lens, spill, lens.color, spill.color));
        }

        void SUpdateHullLights()
        {
            for (int i = 0; i < _sHullLights.Count; i++)
            {
                var (lens, spill, lensColor, spillColor) = _sHullLights[i];
                if (lens == null || spill == null) continue;
                if (!SPowerFault)
                {
                    lens.color = lensColor;
                    spill.color = spillColor;
                    continue;
                }
                float pulse = SFailedLight(Mathf.Repeat(i * .37f, 1f), 1.6f + (i % 3) * .27f);
                var red = Color.Lerp(new Color(.13f, .027f, .024f), EmergencyRed, pulse);
                lens.color = red;
                spill.color = new Color(EmergencyRed.r, EmergencyRed.g, EmergencyRed.b, spillColor.a * pulse);
            }
        }

        sealed class SkySpeck
        {
            public SpriteRenderer sprite;
            public Vector2 origin;
            public float phase, rate;
            public bool ice;
        }

        readonly List<SkySpeck> _kestrelSky = new List<SkySpeck>();

        void BuildKestrelSky()
        {
            _kestrelSky.Clear();
            var random = new System.Random(7241);
            float Between(float min, float max) => min + (max - min) * (float)random.NextDouble();
            for (int i = 0; i < 176; i++)
            {
                bool ice = i >= 64;
                // Give the observation windows a clear view of the drift near the hull.
                bool byWindows = i % 3 == 0;
                var origin = new Vector2(byWindows ? Between(41.8f, 51f) : Between(-6f, _bW + 9f),
                    Between(-6f, _bH + 6f));
                var sprite = BSprite(ice ? "ice-drift" : "twinkling-star", Art.KestrelSkySpeck(ice), origin, -30001);
                sprite.transform.localScale = Vector3.one * (ice ? Between(.45f, 1.05f) : Between(.55f, 1.25f));
                if (ice) sprite.transform.localRotation = Quaternion.Euler(0f, 0f, Between(-25f, 25f));
                _kestrelSky.Add(new SkySpeck
                {
                    sprite = sprite, origin = origin, ice = ice, phase = Between(0f, 2f * Mathf.PI),
                    rate = ice ? Between(.19f, .45f) : Between(.75f, 1.6f)
                });
            }
            UpdateKestrelSky();
        }

        void UpdateKestrelSky()
        {
            bool still = Settings.ReducedMotion;
            float time = still ? 0f : _bAnim;
            float height = _bH + 12f;
            foreach (var speck in _kestrelSky)
            {
                if (speck.sprite == null) continue;
                float wave = .5f + .5f * Mathf.Sin(time * speck.rate + speck.phase);
                if (speck.ice)
                {
                    float y = Mathf.Repeat(speck.origin.y + 6f - time * speck.rate, height);
                    float sway = Mathf.Sin(time * .44f + speck.phase) - Mathf.Sin(speck.phase);
                    speck.sprite.transform.position = new Vector3(speck.origin.x + sway * .35f, y - 6f, 0f);
                    float edge = Mathf.Clamp01(Mathf.Min(y, height - y));
                    speck.sprite.color = new Color(.74f, .87f, 1f, (.28f + wave * .44f) * edge);
                }
                else
                {
                    speck.sprite.color = new Color(.82f, .92f, 1f, .23f + wave * wave * .70f);
                }
            }
        }

        sealed class DetailLight
        {
            public BSpot spot;
            public SpriteRenderer glow, indicator;
            public Color tint;
            public float phase;
        }
        readonly List<DetailLight> _detailLights = new List<DetailLight>();
        Transform _detailRoot;
        int _detailCount = -1;

        bool DetailPowered(string id)
        {
            if (_bMode != 2 || _ld == null) return true;
            if (_ld.Battery <= 0) return false;
            switch (id)
            {
                case "ld_scrubber": return _ld.ScrubberOk;
                case "ld_o2gen": return _ld.O2GenOk;
                case "ld_fan": return _ld.FanOk || _ld.BackupCircuit;
                case "ld_thermal": return _ld.ThermalOk;
                default: return true;
            }
        }

        void WorldDetailsUpdate()
        {
            if (_bRoot == null) return;
            if (_detailRoot != _bRoot.transform || _detailCount != _bSpots.Count)
            {
                foreach (var old in _detailLights)
                {
                    if (old.glow != null) Destroy(old.glow.gameObject);
                    if (old.indicator != null) Destroy(old.indicator.gameObject);
                }
                _detailLights.Clear();
                _detailRoot = _bRoot.transform;
                _detailCount = _bSpots.Count;
                foreach (var s in _bSpots)
                {
                    if (s.sr == null || s.sr.sprite == null) continue;
                    string id = s.id.ToLowerInvariant();
                    bool screen = id.Contains("radio") || id.Contains("panel") || id.Contains("board") || id.Contains("seats");
                    bool machine = id.Contains("scrubber") || id.Contains("o2gen") || id.Contains("fan") || id.Contains("thermal") || id.Contains("battery") || id == "recycler" || id == "air";
                    bool warm = id.Contains("sofa") || id.Contains("table") || id.Contains("food");
                    if (!screen && !machine && !warm) continue;
                    var tint = warm ? new Color(1f,.64f,.32f) : new Color(.25f,.75f,1f);
                    var glow = BSprite("detail-light", Art.SoftLight(), s.sr.bounds.center, s.sr.sortingOrder + 1);
                    if (_bGlowMaterial != null) glow.sharedMaterial = _bGlowMaterial;
                    glow.transform.localScale = new Vector3(2.2f,1.4f,1);
                    SpriteRenderer indicator = null;
                    if (screen || machine)
                    {
                        indicator = BSprite("detail-status", Art.SoftLight(), s.sr.bounds.center, s.sr.sortingOrder + 2);
                        indicator.transform.localScale = Vector3.one * .13f;
                    }
                    _detailLights.Add(new DetailLight { spot=s, glow=glow, indicator=indicator, tint=tint, phase=_detailLights.Count * 1.73f });
                }
            }
            foreach (var d in _detailLights)
            {
                bool visible = d.spot.sr != null && d.spot.sr.gameObject.activeInHierarchy;
                d.glow.enabled = visible;
                if (d.indicator != null) d.indicator.enabled = visible;
                if (!visible) continue;
                bool power = DetailPowered(d.spot.id);
                float wave = Settings.ReducedMotion ? 0f : Mathf.Sin(_bAnim * .8f + d.phase);
                var c = d.tint; c.a = power ? .10f + wave * .012f : .015f;
                d.glow.color = c;
                d.glow.transform.position = d.spot.sr.bounds.center + Vector3.down * .15f;
                if (d.indicator != null)
                {
                    d.indicator.color = power ? new Color(.35f,1f,.8f,.65f + wave*.12f) : new Color(1f,.32f,.12f,.45f);
                    d.indicator.transform.position = d.spot.sr.bounds.center + Vector3.up * .15f;
                }
            }
        }
    }
}
