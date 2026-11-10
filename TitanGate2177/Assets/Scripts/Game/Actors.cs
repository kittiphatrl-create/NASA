// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System.Collections;
using UnityEngine;

namespace TitanGate.Game
{
    /// <summary>The junior astronaut: walks to a spot and plays a visible action for everything they do.</summary>
    public class PlayerActor : MonoBehaviour
    {
        enum Mode { Idle, Walk, Act, Lie }

        SpriteRenderer _sr;
        Mode _mode;
        Pose _a = Pose.Work0, _b = Pose.Work1;
        float _t;
        bool _suit;
        public Sfx sfx;
        public float speed = 3.4f;
        public bool FacingRight { get; private set; } = true;

        public float X => transform.position.x;

        void Awake()
        {
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = WorldBuilder.OrderPlayer;
            _sr.sprite = Art.Astronaut(false, Pose.Idle0);
        }

        public void SetSuit(bool suit) { _suit = suit; Refresh(); }
        public bool Suit => _suit;

        public void Place(float x)
        {
            transform.position = new Vector3(x, 0, 0.1f);
            _mode = Mode.Idle;
            transform.rotation = Quaternion.identity;
            Refresh();
        }

        public void Face(float x)
        {
            if (Mathf.Abs(x - X) < 0.05f) return;
            FacingRight = x > X;
            _sr.flipX = !FacingRight;
        }

        public IEnumerator Walk(float x)
        {
            Face(x);
            _mode = Mode.Walk;
            float stepT = 0;
            while (Mathf.Abs(X - x) > 0.02f)
            {
                var p = transform.position;
                p.x = Mathf.MoveTowards(p.x, x, speed * Time.deltaTime);
                transform.position = p;
                stepT += Time.deltaTime;
                if (stepT > 0.25f) { stepT = 0; sfx?.Play("step", 0.6f); }
                yield return null;
            }
            _mode = Mode.Idle;
            Refresh();
        }

        /// <summary>Loop two poses for the given time (repairing, eating, drilling...).</summary>
        public IEnumerator Act(Pose a, Pose b, float seconds, string sound = "work")
        {
            _a = a; _b = b; _mode = Mode.Act; _t = 0;
            float s = 0, snd = 0;
            while (s < seconds)
            {
                s += Time.deltaTime;
                snd += Time.deltaTime;
                if (snd > 0.55f && sound != null) { snd = 0; sfx?.Play(sound, 0.5f); }
                yield return null;
            }
            _mode = Mode.Idle;
            Refresh();
        }

        public void Lie(bool on)
        {
            _mode = on ? Mode.Lie : Mode.Idle;
            transform.rotation = on ? Quaternion.Euler(0, 0, FacingRight ? 90 : -90) : Quaternion.identity;
            Refresh();
        }

        void Update()
        {
            _t += Time.deltaTime;
            Refresh();
        }

        void Refresh()
        {
            if (_sr == null) return;
            Pose p;
            switch (_mode)
            {
                case Mode.Walk: p = (Pose)((int)Pose.Walk0 + (int)(_t * 8) % 4); break;
                case Mode.Act: p = ((int)(_t * 5) % 2 == 0) ? _a : _b; break;
                case Mode.Lie: p = Pose.Idle0; break;
                default: p = ((int)(_t * 1.6f) % 2 == 0) ? Pose.Idle0 : Pose.Idle1; break;
            }
            _sr.sprite = Art.Astronaut(_suit, p);
        }
    }

    /// <summary>ORBI, the mission AI drone. Floats beside the astronaut.</summary>
    public class OrbiActor : MonoBehaviour
    {
        SpriteRenderer _sr;
        public PlayerActor follow;
        float _t;
        public bool talking;

        void Awake()
        {
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = WorldBuilder.OrderPlayer + 2;
            _sr.sprite = Art.Orbi(0);
        }

        void Update()
        {
            _t += Time.deltaTime;
            if (follow == null) return;
            float side = follow.FacingRight ? -1f : 1f;
            var target = new Vector3(follow.X + side * 0.9f, 2.3f + Mathf.Sin(_t * 2.2f) * 0.12f + (talking ? Mathf.Abs(Mathf.Sin(_t * 12)) * 0.08f : 0), -0.2f);
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-6f * Time.deltaTime));
            int frame = (_t % 3.2f) < 0.12f ? 2 : ((int)(_t * 8) % 2);
            _sr.sprite = Art.Orbi(frame);
            _sr.flipX = !follow.FacingRight;
        }

        public void Snap()
        {
            if (follow != null) transform.position = new Vector3(follow.X - 0.9f, 2.3f, -0.2f);
        }
    }

    /// <summary>Fixed perspective camera, one room at a time, with a slide between rooms and a shake.</summary>
    public class CameraRig : MonoBehaviour
    {
        public Camera cam;
        Vector3 _base;
        float _shake, _shakeAmp;
        public const float Height = 3.05f, Dist = -13f;

        public void Init()
        {
            cam = gameObject.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = false;
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 300f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Pal.Space;
            gameObject.AddComponent<AudioListener>();
            _base = new Vector3(0, Height, Dist);
            transform.position = _base;
        }

        public void SnapTo(float x) { _base = new Vector3(x, Height, Dist); transform.position = _base; }

        /// <summary>Put the camera anywhere (cutscenes, title). SnapTo returns it to the room view.</summary>
        public void SnapPose(Vector3 p) { _base = p; transform.position = p; }

        public Vector3 Pose => _base;

        public IEnumerator SlideTo(float x, float seconds)
        {
            var from = _base;
            var to = new Vector3(x, Height, Dist);
            float t = 0;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0, 1, t / seconds);
                _base = Vector3.Lerp(from, to, k);
                yield return null;
            }
            _base = to;
        }

        public void Shake(float seconds, float amp) { _shake = seconds; _shakeAmp = amp; }

        void LateUpdate()
        {
            var p = _base;
            if (_shake > 0)
            {
                _shake -= Time.deltaTime;
                p += new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) * _shakeAmp;
            }
            transform.position = p;
        }
    }
}
