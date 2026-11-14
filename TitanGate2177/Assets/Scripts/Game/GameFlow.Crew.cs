using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        sealed class CrewRoutine
        {
            public readonly List<Vector2> path = new List<Vector2>();
            public Vector2 facing = Vector2.down;
            public float wait = 2;
            public int stop;
        }
        readonly Dictionary<string, CrewRoutine> _crewRoutine = new Dictionary<string, CrewRoutine>();
        SpriteRenderer _crewBubble;
        Label _crewPrompt;
        float _crewWaveUntil;

        string CrewText(string en, string th) => _loc.Current == Lang.TH ? th : en;

        string BTimeHint(string id)
        {
            float hours = id == "solar" || id == "drill" ? 3 : id.StartsWith("plot") ? 2 : 0;
            return hours == 0 ? "" : " · " + hours + CrewText(" h", " ชม.");
        }

        bool BTimedTask(Stock delta, float hours)
        {
            _life.CardProgress(out int done, out int total);
            float reserve = total - done;
            if (_life.State.schedule.Hour + hours > LateHour + 0.01f)
            {
                _ui.Toast(CrewText("Too late for that today. It would run past bedtime.", "วันนี้ดึกเกินไปแล้ว ทำไม่ทันก่อนเข้านอน"), GameUI.CAmber);
                return false;
            }
            if (!_life.State.schedule.CanSpend(hours, reserve))
            {
                _ui.Toast(CrewText("Not enough work time. Keep time for today's events; rest at the pod when ready.", "เวลาทำงานไม่พอ ต้องเผื่อเวลาให้เหตุการณ์วันนี้ พร้อมแล้วกลับไปพักที่ยาน"), GameUI.CAmber);
                return false;
            }
            bool ok = _life.ApplyTask(delta, hours, reserve);
            if (ok && RestoreMode && _restore != null) { RestoreSync(); _restore.Tire(hours); }
            if (ok) _ui.Toast(CrewText("Work completed · ", "ทำงานเสร็จ · ") + hours + CrewText(" h elapsed", " ชั่วโมงผ่านไป"), GameUI.CCyan);
            if (!ok) _ui.Toast(CrewText("Not enough supplies for this job.", "ทรัพยากรไม่พอสำหรับงานนี้"), GameUI.CRed);
            return ok;
        }

        string BClockText()
        {
            float hour = _life.State.schedule.Hour;
            hour = Mathf.Repeat(hour, 24f);
            string phase = hour < 12 ? CrewText("MORNING", "เช้า") : hour < 17 ? CrewText("AFTERNOON", "บ่าย") : hour < 20 ? CrewText("DUSK", "เย็น") : CrewText("NIGHT", "กลางคืน");
            return $"{(int)hour:00}:{(int)((hour % 1) * 60):00}  ·  {phase}  ·  " + (RestoreMode && _restore != null ? _restore.HoursLeft : _life.State.schedule.Remaining).ToString("0.#") + CrewText(" h left", " ชม. เหลือ");
        }

        void CrewReset()
        {
            _crewRoutine.Clear(); _crewWaveUntil = 0; _crewBubble = null;
            _crewPrompt?.RemoveFromHierarchy(); _crewPrompt = null;
        }

        // Routines are presentation only. Actual daily help remains authoritative in LifeboatSim.
        void CrewUpdate(float dt)
        {
            if (_bMode != 0 || _life == null) return;
            BSpot nearest = null; float nearestD = 1.8f;
            for (int i = 0; i < _bGuests.Count && i < _life.State.guests.Count; i++)
            {
                var sr = _bGuests[i]; if (!sr.gameObject.activeSelf) continue;
                string id = _life.State.guests[i];
                if (!_crewRoutine.TryGetValue(id, out var r)) { r = new CrewRoutine { wait = 1 + i }; _crewRoutine[id] = r; }
                Vector2 p = sr.transform.position, delta = Vector2.zero;
                bool moving = false;
                if (!_bGather && !_ui.Blocking)
                {
                    r.wait -= dt;
                    if (_crewCall.TryGetValue(id, out var callTo))
                    {
                        // called over to a job: walk there and stay to help
                        if (r.path.Count == 0)
                        {
                            if (Vector2.Distance(p, callTo) > 0.35f)
                            {
                                var route = BFindPath(p, callTo);
                                if (route != null && route.Count > 0) r.path.AddRange(route); else { p = callTo; _crewHere.Add(id); }
                            }
                            else { _crewHere.Add(id); r.facing = Vector2.up; }
                        }
                        r.wait = 2;
                    }
                    else if (r.path.Count == 0 && r.wait <= 0)
                    {
                        // Walk to reachable work stations through the same collision grid as the player.
                        string job = id == "aya" || id == "ree" ? "plot0" : id == "tik" || id == "jules" ? "air" : "shelf_food";
                        var station = BSpotById(r.stop++ % 2 == 0 ? job : "board");
                        if (station != null)
                        {
                            var route = BFindPath(p, station.stand);
                            if (route != null) r.path.AddRange(route);
                        }
                        r.wait = 7 + i * 2;
                    }
                    if (r.path.Count > 0)
                    {
                        delta = r.path[0] - p;
                        p = Vector2.MoveTowards(p, r.path[0], dt * (_crewCall.ContainsKey(id) ? 2.6f : 1.4f));
                        if (Vector2.Distance(p, r.path[0]) < .03f) r.path.RemoveAt(0);
                        if (delta.sqrMagnitude > .001f) r.facing = delta.normalized;
                        moving = delta.sqrMagnitude > .001f;
                    }
                }
                int dir = Mathf.Abs(r.facing.y) > Mathf.Abs(r.facing.x) ? (r.facing.y > 0 ? 1 : 0) : 2;
                int frame = moving ? (int)(_bAnim * 8) % 8 : (_bAnim % 4 > 3.8f ? -2 : -1);
                sr.sprite = _bGather ? Art.CrewAction(id, "sit") : !moving && _crewHere.Contains(id) ? Art.CrewAction(id, "repair", (int)(_bAnim * 5) % 4) : !moving && r.wait < 5 && (id == "aya" || id == "jules") ? Art.CrewAction(id, id == "aya" ? "water" : "repair", (int)(_bAnim * 5) % 4) : Art.Crew(id, dir, frame, false, false, Time.time < _crewWaveUntil);
                sr.flipX = dir == 2 && r.facing.x < 0; sr.transform.localScale = Vector3.one;
                sr.transform.position = p; sr.sortingOrder = SortY(p.y);
                var spot = BSpotById("guest" + i); if (spot == null) continue;
                spot.stand = p; spot.pos = p;
                float d = Vector2.Distance(_bPos, p);
                if (d < nearestD) { nearest = spot; nearestD = d; }
            }
            if (_crewBubble == null) _crewBubble = BSprite("crew-talk", Art.TalkBubble(), Vector2.zero, 31000);
            bool show = nearest != null && !_ui.Blocking && !_bGather;
            _crewBubble.gameObject.SetActive(show);
            if (_crewPrompt == null)
            {
                _crewPrompt = _ui.L("", 23, true, GameUI.CText);
                _crewPrompt.style.position = Position.Absolute; _crewPrompt.style.bottom = 32;
                _crewPrompt.style.alignSelf = Align.Center; _crewPrompt.style.backgroundColor = GameUI.CPanel;
                _crewPrompt.style.paddingLeft = _crewPrompt.style.paddingRight = 20;
                GameUI.Border(_crewPrompt, GameUI.CAmber, 2, 12); _ui.Root.Add(_crewPrompt);
            }
            _crewPrompt.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show)
            {
                _crewBubble.transform.position = nearest.sr.transform.position + new Vector3(0, 2.15f, 0);
                _crewPrompt.text = GameUI.Thai("[E] " + CrewText("Talk to ", "คุยกับ ") + nearest.nameKey);
            }
        }
    }
}
