// LAST 72 HOURS — short repair mini-games (5–20 s each).
// Success = the repair happens with its normal time cost.
// Fail    = GameUI charges a 30-minute mistake and offers a retry.
// Perfect = small resource bonus.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Last72
{
    public abstract class MiniGame : MonoBehaviour
    {
        protected GameUI ui;
        protected RectTransform area;
        protected int mistakes;
        bool finished;
        Action<bool, bool> done;
        public abstract string Title { get; }
        public abstract string Instructions { get; }

        public void Begin(GameUI owner, RectTransform playArea, Action<bool, bool> onDone)
        {
            ui = owner; area = playArea; done = onDone;
            Build();
        }

        protected abstract void Build();

        /// Also used by tests to force a result.
        public void Finish(bool success, bool perfect)
        {
            if (finished) return;
            finished = true;
            if (done != null) done(success, success && perfect);
        }

        protected bool Finished { get { return finished; } }

        protected static bool ActionKeyDown()
        {
#if ENABLE_INPUT_SYSTEM
            var k = UnityEngine.InputSystem.Keyboard.current;
            return k != null && (k.spaceKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E);
#endif
        }

        protected Image Rect(Transform p, float x0, float y0, float x1, float y1, Color c)
        {
            var img = ui.Box(p, "r", x0, y0, x1, y1, c).GetComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        public static MiniGame For(string actionId, GameObject host)
        {
            switch (actionId)
            {
                case "o2": return host.AddComponent<TimingGame>();
                case "power": var w = host.AddComponent<WireGame>(); w.Count = 4; return w;
                case "cable": var c = host.AddComponent<WireGame>(); c.Count = 3; return c;
                case "seal": return host.AddComponent<LeakGame>();
                case "calibrate": return host.AddComponent<WaveGame>();
                case "stabilize": return host.AddComponent<SimonGame>();
                case "override": return host.AddComponent<KeypadGame>();
            }
            return null;
        }
    }

    // ------------------------------------------------------------------
    // Skill check: stop the sweeping needle inside the green zone, twice.
    public class TimingGame : MiniGame
    {
        public override string Title { get { return "SEAT THE FILTER"; } }
        public override string Instructions { get { return "กด STOP (หรือ Space) ให้เข็มหยุดในโซนสีเขียว 2 ครั้ง • โซนเหลืองตรงกลาง = PERFECT"; } }
        RectTransform needle, zone, perfectZone;
        Text status;
        float pos, dir = 1, speed = 0.7f, zoneC, zoneW = 0.16f;
        int hits, perfectHits;
        const int Need = 2;

        protected override void Build()
        {
            Rect(area, 0.05f, 0.45f, 0.95f, 0.62f, new Color(0, 0, 0, 0.6f));
            zone = (RectTransform)Rect(area, 0, 0.45f, 0, 0.62f, new Color(0.25f, 0.8f, 0.4f, 0.8f)).transform;
            perfectZone = (RectTransform)Rect(area, 0, 0.45f, 0, 0.62f, new Color(1f, 0.85f, 0.3f, 0.95f)).transform;
            needle = (RectTransform)Rect(area, 0, 0.40f, 0, 0.67f, Color.white).transform;
            status = ui.Label(area, "", 22, TextAnchor.MiddleCenter, Color.white, 0, 0.72f, 1, 0.85f, true);
            ui.Btn(area, "STOP", 0.35f, 0.08f, 0.65f, 0.3f, new Color(0.15f, 0.48f, 0.28f), Stop, 30);
            NewZone();
        }

        void NewZone()
        {
            zoneC = UnityEngine.Random.Range(0.2f, 0.8f);
            SetX(zone, zoneC - zoneW / 2, zoneC + zoneW / 2);
            SetX(perfectZone, zoneC - 0.02f, zoneC + 0.02f);
            status.text = "HIT " + hits + " / " + Need;
        }

        static void SetX(RectTransform r, float a, float b)
        {
            r.anchorMin = new Vector2(0.05f + a * 0.9f, r.anchorMin.y);
            r.anchorMax = new Vector2(0.05f + b * 0.9f, r.anchorMax.y);
        }

        void Update()
        {
            if (Finished) return;
            pos += dir * speed * Time.unscaledDeltaTime;
            if (pos > 1) { pos = 1; dir = -1; }
            if (pos < 0) { pos = 0; dir = 1; }
            SetX(needle, pos - 0.004f, pos + 0.004f);
            if (ActionKeyDown()) Stop();
        }

        void Stop()
        {
            if (Finished) return;
            float d = Mathf.Abs(pos - zoneC);
            if (d <= zoneW / 2)
            {
                hits++;
                if (d <= 0.02f) { perfectHits++; Sfx.Play("success", 0.6f, 1.3f); } else Sfx.Play("tick", 1f);
                if (hits >= Need) { Finish(true, perfectHits == Need); return; }
                speed *= 1.3f; zoneW *= 0.85f;
                NewZone();
            }
            else
            {
                mistakes++;
                Sfx.Play("glitch", 0.8f);
                status.text = "MISS! (" + mistakes + "/2)";
                if (mistakes >= 2) Finish(false, false);
            }
        }
    }

    // ------------------------------------------------------------------
    // Wiring: connect each left terminal to the same colour on the right.
    public class WireGame : MiniGame
    {
        public int Count = 4;
        public override string Title { get { return "REWIRE THE PANEL"; } }
        public override string Instructions { get { return "คลิกขั้วซ้าย แล้วคลิกขั้วขวาที่สีเดียวกัน • ต่อผิด 3 ครั้ง = ไฟช็อต"; } }
        static readonly Color[] Cols = { new Color(1f, 0.3f, 0.3f), new Color(0.3f, 0.6f, 1f), new Color(1f, 0.85f, 0.2f), new Color(0.85f, 0.4f, 1f) };
        readonly List<Button> left = new List<Button>(), right = new List<Button>();
        int[] rightColor;
        int picked = -1, connected;
        readonly bool[] doneL = new bool[4], doneR = new bool[4];
        Text status;

        protected override void Build()
        {
            rightColor = new int[Count];
            for (int i = 0; i < Count; i++) rightColor[i] = i;
            for (int i = Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); int t = rightColor[i]; rightColor[i] = rightColor[j]; rightColor[j] = t; }
            float h = 0.7f / Count;
            for (int i = 0; i < Count; i++)
            {
                int idx = i;
                float y1 = 0.85f - i * h, y0 = y1 - h * 0.7f;
                var l = ui.Btn(area, "", 0.08f, y0, 0.2f, y1, Cols[i], () => PickLeft(idx), 14);
                var r = ui.Btn(area, "", 0.8f, y0, 0.92f, y1, Cols[rightColor[i]], () => PickRight(idx), 14);
                left.Add(l); right.Add(r);
            }
            status = ui.Label(area, "", 20, TextAnchor.MiddleCenter, Color.white, 0.25f, 0.0f, 0.75f, 0.12f, true);
            status.text = "0 / " + Count;
        }

        void PickLeft(int i)
        {
            if (Finished || doneL[i]) return;
            picked = i;
            Sfx.Play("tick", 0.8f);
            for (int k = 0; k < Count; k++) left[k].GetComponent<Outline>().effectColor = k == i ? Color.white : new Color(1, 1, 1, 0.15f);
            left[i].GetComponent<Outline>().effectDistance = new Vector2(4, -4);
        }

        void PickRight(int i)
        {
            if (Finished || picked < 0 || doneR[i]) return;
            if (rightColor[i] == picked)
            {
                doneL[picked] = true; doneR[i] = true;
                DrawWire(left[picked].GetComponent<RectTransform>(), right[i].GetComponent<RectTransform>(), Cols[picked]);
                connected++;
                Sfx.Play("ui_confirm", 0.7f);
                status.text = connected + " / " + Count;
                picked = -1;
                if (connected >= Count) Finish(true, mistakes == 0);
            }
            else
            {
                mistakes++;
                Sfx.Play("glitch", 0.9f);
                status.text = "SPARK! ผิด " + mistakes + "/3";
                if (mistakes >= 3) Finish(false, false);
            }
        }

        void DrawWire(RectTransform a, RectTransform b, Color c)
        {
            Canvas.ForceUpdateCanvases();
            Vector2 pa = area.InverseTransformPoint(a.TransformPoint(new Vector3(a.rect.xMax, a.rect.center.y)));
            Vector2 pb = area.InverseTransformPoint(b.TransformPoint(new Vector3(b.rect.xMin, b.rect.center.y)));
            var go = new GameObject("Wire", typeof(RectTransform));
            go.transform.SetParent(area, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            Vector2 d = pb - pa;
            rt.sizeDelta = new Vector2(d.magnitude, 8);
            rt.anchoredPosition = pa - area.rect.center;
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var img = go.AddComponent<Image>(); img.color = c; img.raycastTarget = false;
        }
    }

    // ------------------------------------------------------------------
    // Leak patching: click each leak before it sprays out.
    public class LeakGame : MiniGame
    {
        public override string Title { get { return "PATCH THE LEAKS"; } }
        public override string Instructions { get { return "คลิกจุดรั่ว (LEAK) ให้ทันก่อนมันหายไป • พลาด 3 จุด = ซีลไม่อยู่"; } }
        const int Total = 7;
        int spawned, patched, missed;
        float nextAt, life = 1.5f;
        Button current; float currentUntil;
        Text status;

        protected override void Build()
        {
            Rect(area, 0.02f, 0.18f, 0.98f, 0.92f, new Color(0.25f, 0.18f, 0.14f, 0.9f));
            Rect(area, 0.02f, 0.5f, 0.98f, 0.6f, new Color(0.55f, 0.55f, 0.6f, 1f)); // the pipe
            status = ui.Label(area, "", 20, TextAnchor.MiddleCenter, Color.white, 0.2f, 0.02f, 0.8f, 0.15f, true);
            nextAt = Time.unscaledTime + 0.6f;
            UpdateStatus();
        }

        void UpdateStatus() { status.text = "PATCHED " + patched + " / " + Total + "    MISSED " + missed + " / 3"; }

        void Update()
        {
            if (Finished) return;
            if (current != null && Time.unscaledTime > currentUntil)
            {
                ui.KillGo(current.gameObject); current = null;
                missed++; Sfx.Play("glitch", 0.7f); UpdateStatus();
                if (missed >= 3) { Finish(false, false); return; }
            }
            if (current == null && Time.unscaledTime >= nextAt)
            {
                if (spawned >= Total) { Finish(true, missed == 0); return; }
                spawned++;
                float x = UnityEngine.Random.Range(0.06f, 0.82f), y = UnityEngine.Random.Range(0.22f, 0.74f);
                current = ui.Btn(area, "LEAK", x, y, x + 0.12f, y + 0.14f, new Color(0.9f, 0.45f, 0.15f), Patch, 18);
                currentUntil = Time.unscaledTime + life;
                Sfx.Play("seal", 0.35f, 1.4f);
            }
        }

        void Patch()
        {
            if (current == null || Finished) return;
            ui.KillGo(current.gameObject); current = null;
            patched++; Sfx.Play("tick", 1f); UpdateStatus();
            life = Mathf.Max(0.9f, life - 0.08f);
            nextAt = Time.unscaledTime + 0.25f;
        }
    }

    // ------------------------------------------------------------------
    // Navigation: tune frequency + amplitude to overlay the target signal.
    public class WaveGame : MiniGame
    {
        public override string Title { get { return "ALIGN NAV SIGNAL"; } }
        public override string Instructions { get { return "ปรับ FREQ / AMP ให้คลื่นสีฟ้าทับคลื่นเป้าหมาย (สีเทา) แล้วกด LOCK • LOCK ผิด 3 ครั้ง = ล้มเหลว"; } }
        int f = 1, a = 1, tf, ta;
        const int Cols = 28;
        readonly List<RectTransform> cur = new List<RectTransform>(), tgt = new List<RectTransform>();
        Text readout;

        protected override void Build()
        {
            tf = UnityEngine.Random.Range(2, 6); ta = UnityEngine.Random.Range(2, 6);
            Rect(area, 0.05f, 0.3f, 0.95f, 0.92f, new Color(0, 0.04f, 0.08f, 0.95f));
            for (int i = 0; i < Cols; i++)
            {
                float x = 0.06f + i * (0.88f / Cols);
                tgt.Add((RectTransform)Rect(area, x, 0.6f, x + 0.012f, 0.62f, new Color(0.7f, 0.7f, 0.75f, 0.9f)).transform);
                cur.Add((RectTransform)Rect(area, x + 0.012f, 0.6f, x + 0.022f, 0.62f, new Color(0.3f, 0.8f, 1f, 1f)).transform);
            }
            ui.Btn(area, "FREQ ◀", 0.05f, 0.14f, 0.2f, 0.26f, new Color(0.22f, 0.27f, 0.34f), () => Adj(ref f, -1), 18);
            ui.Btn(area, "FREQ ▶", 0.21f, 0.14f, 0.36f, 0.26f, new Color(0.22f, 0.27f, 0.34f), () => Adj(ref f, 1), 18);
            ui.Btn(area, "AMP ◀", 0.40f, 0.14f, 0.55f, 0.26f, new Color(0.22f, 0.27f, 0.34f), () => Adj(ref a, -1), 18);
            ui.Btn(area, "AMP ▶", 0.56f, 0.14f, 0.71f, 0.26f, new Color(0.22f, 0.27f, 0.34f), () => Adj(ref a, 1), 18);
            ui.Btn(area, "LOCK", 0.76f, 0.14f, 0.95f, 0.26f, new Color(0.15f, 0.48f, 0.28f), Lock, 22);
            readout = ui.Label(area, "", 18, TextAnchor.MiddleCenter, Color.white, 0.05f, 0.0f, 0.95f, 0.12f, true);
            Draw(tgt, tf, ta); Draw(cur, f, a); UpdateReadout();
        }

        void Adj(ref int v, int d) { v = Mathf.Clamp(v + d, 1, 5); Sfx.Play("tick", 0.8f, 0.8f + v * 0.1f); Draw(cur, f, a); UpdateReadout(); }

        void UpdateReadout() { readout.text = "FREQ " + f + "   AMP " + a + "      ผิด " + mistakes + "/3"; }

        static void Draw(List<RectTransform> cols, int freq, int amp)
        {
            for (int i = 0; i < cols.Count; i++)
            {
                float v = Mathf.Sin(i / (float)cols.Count * Mathf.PI * 2f * freq * 0.5f) * amp * 0.055f;
                var r = cols[i];
                float y0 = 0.61f + Mathf.Min(0, v), y1 = 0.61f + Mathf.Max(0, v) + 0.012f;
                r.anchorMin = new Vector2(r.anchorMin.x, y0); r.anchorMax = new Vector2(r.anchorMax.x, y1);
            }
        }

        void Lock()
        {
            if (Finished) return;
            if (f == tf && a == ta) { Finish(true, mistakes == 0); return; }
            mistakes++; Sfx.Play("ui_error", 0.8f); UpdateReadout();
            if (mistakes >= 3) Finish(false, false);
        }
    }

    // ------------------------------------------------------------------
    // Jump drive: memorise the flash sequence and repeat it.
    public class SimonGame : MiniGame
    {
        public override string Title { get { return "STABILIZE JUMP CORE"; } }
        public override string Instructions { get { return "จำลำดับไฟที่กะพริบ แล้วกดตามให้ถูก (5 ขั้น) • ผิด 2 ครั้ง = แกนไม่เสถียร"; } }
        static readonly Color[] Cols = { new Color(1f, 0.35f, 0.3f), new Color(0.3f, 0.85f, 0.45f), new Color(0.3f, 0.6f, 1f), new Color(1f, 0.85f, 0.25f) };
        readonly List<Button> pads = new List<Button>();
        readonly List<int> seq = new List<int>();
        int input; bool showing; Text status;

        protected override void Build()
        {
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                float x0 = 0.1f + (i % 2) * 0.42f, y0 = 0.5f - (i / 2) * 0.32f;
                var b = ui.Btn(area, "", x0, y0, x0 + 0.38f, y0 + 0.28f, Dim(Cols[i]), () => Press(idx), 14);
                pads.Add(b);
            }
            status = ui.Label(area, "", 20, TextAnchor.MiddleCenter, Color.white, 0, 0.82f, 1, 0.95f, true);
            for (int i = 0; i < 5; i++) seq.Add(UnityEngine.Random.Range(0, 4));
            StartCoroutine(Show());
        }

        static Color Dim(Color c) { return new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f, 1); }

        public int[] Sequence { get { return seq.ToArray(); } } // test hook
        public bool Showing { get { return showing; } }

        System.Collections.IEnumerator Show()
        {
            showing = true; input = 0;
            status.text = "WATCH...";
            yield return new WaitForSecondsRealtime(0.6f);
            foreach (int i in seq)
            {
                pads[i].GetComponent<Image>().color = Cols[i];
                Sfx.Play("tick", 1f, 0.8f + i * 0.2f);
                yield return new WaitForSecondsRealtime(0.45f);
                pads[i].GetComponent<Image>().color = Dim(Cols[i]);
                yield return new WaitForSecondsRealtime(0.15f);
            }
            showing = false;
            status.text = "REPEAT  0 / " + seq.Count;
        }

        public void Press(int i)
        {
            if (Finished || showing) return;
            StartCoroutine(Flash(i));
            if (seq[input] == i)
            {
                input++;
                Sfx.Play("tick", 1f, 0.8f + i * 0.2f);
                status.text = "REPEAT  " + input + " / " + seq.Count;
                if (input >= seq.Count) Finish(true, mistakes == 0);
            }
            else
            {
                mistakes++;
                Sfx.Play("glitch", 0.9f);
                if (mistakes >= 2) { Finish(false, false); return; }
                StartCoroutine(Show());
            }
        }

        System.Collections.IEnumerator Flash(int i)
        {
            pads[i].GetComponent<Image>().color = Cols[i];
            yield return new WaitForSecondsRealtime(0.15f);
            if (pads[i] != null) pads[i].GetComponent<Image>().color = Dim(Cols[i]);
        }
    }

    // ------------------------------------------------------------------
    // Security override: the 4-digit code is in the engineer's voice log.
    public class KeypadGame : MiniGame
    {
        public const string Code = "4719";
        public override string Title { get { return "SECURITY OVERRIDE"; } }
        public override string Instructions { get { return "ใส่รหัส Override 4 หลัก • คำใบ้: บันทึกเสียงของวิศวกร Mali (Voice Recorder, Crew Quarters)"; } }
        string entry = "";
        Text display;

        protected override void Build()
        {
            var screen = Rect(area, 0.3f, 0.78f, 0.7f, 0.95f, new Color(0, 0.05f, 0.03f, 1));
            display = ui.Label(screen.transform, "____", 40, TextAnchor.MiddleCenter, new Color(0.3f, 1f, 0.5f), bold: true);
            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "CLR", "0", "ENTER" };
            for (int i = 0; i < 12; i++)
            {
                string k = keys[i];
                float x0 = 0.3f + (i % 3) * 0.135f, y1 = 0.74f - (i / 3) * 0.18f;
                ui.Btn(area, k, x0, y1 - 0.16f, x0 + 0.125f, y1, k == "ENTER" ? new Color(0.15f, 0.48f, 0.28f) : new Color(0.22f, 0.27f, 0.34f), () => Key(k), k.Length > 1 ? 16 : 26);
            }
        }

        public void Key(string k)
        {
            if (Finished) return;
            if (k == "CLR") entry = "";
            else if (k == "ENTER")
            {
                if (entry == Code) { Sfx.Play("ui_confirm"); Finish(true, true); return; }
                Sfx.Play("ui_error"); mistakes++; entry = "";
                display.text = "DENIED"; return;
            }
            else if (entry.Length < 4) { entry += k; Sfx.Play("tick", 0.8f); }
            display.text = entry.PadRight(4, '_');
        }
    }
}
