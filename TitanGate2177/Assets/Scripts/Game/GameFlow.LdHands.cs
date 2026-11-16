// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do hands-on repairs at the broken machine (a short timing game), a save the CONTINUE button really
//           resumes, and clear highlights on what can be picked up or opened make LAST DOCK feel like play
//           rather than menu clicks?
// Date: 2026-11-15
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TitanGate.Core.LastDock;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using DockRoom = TitanGate.Core.LastDock.Room;
using Res = TitanGate.Core.LastDock.Res;
using Fault = TitanGate.Core.LastDock.Fault;
using Outcome = TitanGate.Core.LastDock.Outcome;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        // ------------------------------------------------------------------ save and CONTINUE
        // The save itself lives in GameFlow.Dinner.cs (seed + kit + actions, written after each dinner). These helpers
        // only route every action through it.

        /// <summary>An older save made at the end of the scramble ("lifeboat"): start LAST DOCK with what was stowed.</summary>
        void LdContinueFromStowed(List<string> stowed)
        {
            var kit = new Dictionary<Res, int>();
            foreach (var id in stowed) { var r = LdResOfSupply(id); kit[r] = (kit.TryGetValue(r, out var n) ? n : 0) + 1; }
            _player.gameObject.SetActive(false);
            _orbi.gameObject.SetActive(false);
            StartLastDock(kit.Count > 0 ? kit : null, System.Environment.TickCount);
        }

        /// <summary>Take an action through the rules and remember it for the save (GameFlow.Dinner.cs).</summary>
        ActionResult LdRun(string a)
        {
            var r = _ld.Do(a);
            if (r.ok) LdRecord(a);
            return r;
        }

        // ------------------------------------------------------------------ hands-on repairs (a timing game)

        bool _ldHandsDone;

        /// <summary>Repairs and sending are done by hand at the machine: stop the needle in the green zone.</summary>
        static bool LdNeedsHands(string a) => a.StartsWith("repair_") || a == "send" || a == "assemble_rover";

        IEnumerator LdHandsOn(string a)
        {
            var (name, _) = LdAction(a);
            bool send = a == "send", radio = a == "repair_radio";
            int need = send ? 2 : 3, hits = 0;
            float pos = 0f, dir = 1f, speed = send ? 0.7f : 0.55f, zoneW = 0.26f;
            float zoneX = Random.Range(0.12f, 0.88f - zoneW);
            bool cancel = false, press = false;
            VisualElement zone = null, needle = null; Label status = null;
            var pips = new List<VisualElement>();
            string msg = "";
            Color msgColor = GameUI.CDim;
            _ui.ShowModal(p =>
            {
                LdHeader(p, (send ? TT("Tune the antenna", "จูนเสาอากาศ") : radio ? TT("Solder the radio board", "บัดกรีแผงวิทยุ") : TT("Fix it by hand", "ลงมือซ่อมเอง")) + " · " + name);
                p.Add(LdPara(send ? TT("Stop the needle in the green band to lock onto the rescue ship's signal.", "หยุดเข็มให้อยู่ในแถบสีเขียวเพื่อจับสัญญาณยานกู้ภัย")
                                  : TT($"Stop the needle in the green band {need} times: tighten, connect, test.", $"หยุดเข็มให้อยู่ในแถบสีเขียว {need} ครั้ง: ขัน ต่อสาย ทดสอบ"), GameUI.CText, 22));
                var bar = new VisualElement();
                bar.style.width = 760; bar.style.height = 46; bar.style.alignSelf = Align.Center; bar.style.marginTop = 12; bar.style.marginBottom = 8;
                bar.style.backgroundColor = new Color(0.08f, 0.09f, 0.13f, 1f); GameUI.Border(bar, LdGoldDim, 2, 8);
                zone = new VisualElement(); zone.style.position = Position.Absolute; zone.style.top = 0; zone.style.bottom = 0;
                zone.style.backgroundColor = new Color(0.3f, 0.85f, 0.45f, 0.8f);
                needle = new VisualElement(); needle.style.position = Position.Absolute; needle.style.top = -8; needle.style.bottom = -8; needle.style.width = 6;
                needle.style.backgroundColor = Color.white; GameUI.Border(needle, GameUI.Ink, 1, 3);
                bar.Add(zone); bar.Add(needle); p.Add(bar);
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignSelf = Align.Center; row.style.marginBottom = 6;
                pips.Clear();
                for (int i = 0; i < need; i++)
                {
                    var pip = new VisualElement(); pip.style.width = 26; pip.style.height = 26; pip.style.marginLeft = pip.style.marginRight = 6;
                    pip.style.backgroundColor = i < hits ? GameUI.CGreen : new Color(1, 1, 1, 0.12f); GameUI.Border(pip, LdGoldDim, 2, 13);
                    row.Add(pip); pips.Add(pip);
                }
                p.Add(row);
                status = LdPara(msg, msgColor, 20); status.style.alignSelf = Align.Center; p.Add(status);
                var buttons = new VisualElement(); buttons.style.flexDirection = FlexDirection.Row; buttons.style.alignSelf = Align.Center; buttons.style.marginTop = 8;
                buttons.Add(_ui.B(TT("NOW!  (E / Space)", "ตอนนี้!  (E / Space)"), () => press = true, 28, true));
                buttons.Add(_ui.B(TT("Stop (no time used)", "หยุดก่อน (ไม่เสียเวลา)"), () => cancel = true, 22));
                p.Add(buttons);
            }, false);
            _actionPose = LdPoseOf(a);
            while (!cancel && hits < need)
            {
                float dt = Time.unscaledDeltaTime;
                pos += dir * speed * dt;
                if (pos > 1f) { pos = 1f; dir = -1f; }
                if (pos < 0f) { pos = 0f; dir = 1f; }
                if (zone != null) { zone.style.left = Length.Percent(zoneX * 100f); zone.style.width = Length.Percent(zoneW * 100f); }
                if (needle != null) needle.style.left = Length.Percent(pos * 100f);
                var kb = Keyboard.current;
                if (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) press = true;
                if (kb != null && kb.escapeKey.wasPressedThisFrame) cancel = true;
                if (press)
                {
                    press = false;
                    if (pos >= zoneX - 0.01f && pos <= zoneX + zoneW + 0.01f)
                    {
                        hits++;
                        _sfx.Play(send ? "good" : "clank");
                        if (!send) LdSparks();
                        if (hits - 1 < pips.Count) pips[hits - 1].style.backgroundColor = GameUI.CGreen;
                        zoneW = Mathf.Max(0.12f, zoneW * 0.8f); speed *= 1.18f;
                        zoneX = Random.Range(0.08f, 0.92f - zoneW);
                        msg = TT("Good!", "ดีมาก!"); msgColor = GameUI.CGreen;
                    }
                    else
                    {
                        _sfx.Play("bad");
                        _ui.Flash(new Color(1f, 0.2f, 0.1f, 0.18f));
                        zoneX = Random.Range(0.08f, 0.92f - zoneW);
                        msg = TT("Missed — try again. Take your time.", "พลาด ลองใหม่ ค่อย ๆ ทำ"); msgColor = GameUI.CAmber;
                    }
                    if (status != null) { status.text = msg; status.style.color = msgColor; }
                }
                yield return null;
            }
            _actionPose = null;
            _ui.CloseModal();
            if (cancel) yield break;
            yield return new WaitForSecondsRealtime(0.15f);
            _ldHandsDone = true;
            LdDo(a);
            _ldHandsDone = false;
        }

        /// <summary>The cadet's pose for an action, so the hands match the machine.</summary>
        static string LdPoseOf(string a)
        {
            if (a.StartsWith("repair_") || a == "absorber" || a == "assemble_rover" || a == "backup_fan" || a == "seal_room" || a == "detach_storage" || a == "cannibalize") return "repair";
            if (a == "search" || a == "open_mystery") return "lift";
            return "use";
        }

        // ------------------------------------------------------------------ highlights: pick-ups, locked doors, the airlock

        readonly List<(SpriteRenderer ring, SpriteRenderer owner, BSpot spot)> _ldRings = new List<(SpriteRenderer, SpriteRenderer, BSpot)>();
        /// <summary>Machines and consoles you can use: a soft cyan outline, bright when you are close enough to press E.</summary>
        readonly List<(SpriteRenderer ring, BSpot spot, Sprite made)> _ldUseRings = new List<(SpriteRenderer, BSpot, Sprite)>();

        /// <summary>Outline every usable thing on the map (called once the map's props exist).</summary>
        void LdRingUsables()
        {
            _ldUseRings.Clear();
            foreach (var s in _bSpots)
            {
                if (s.nameKey == null || s.sr == null || s.sr.sprite == null || s.id == "deco" || s.id == "pet" || s.id.StartsWith("guest")) continue;
                if (s.id.StartsWith("sup_") || s.id == "keycard") continue;   // pick-ups have their own gold ring
                var ring = BSprite("use_ring", Art.UseRing(s.sr.sprite), s.sr.transform.position, s.sr.sortingOrder - 1);
                _ldUseRings.Add((ring, s, s.sr.sprite));
            }
        }

        void LdUseRingsUpdate(float pulse)
        {
            var near = BNearestSpot();
            for (int i = 0; i < _ldUseRings.Count; i++)
            {
                var (ring, spot, made) = _ldUseRings[i];
                if (ring == null || spot.sr == null) continue;
                bool on = spot.sr.enabled && spot.sr.gameObject.activeInHierarchy && spot.sr.sprite != null && !_ui.ScreenOpen;
                ring.enabled = on;
                if (!on) continue;
                if (spot.sr.sprite != made) { ring.sprite = Art.UseRing(spot.sr.sprite); _ldUseRings[i] = (ring, spot, spot.sr.sprite); }   // broken/fixed art swaps
                ring.transform.position = spot.sr.transform.position;
                ring.flipX = spot.sr.flipX;
                bool close = near == spot;
                ring.color = close ? new Color(1f, 0.95f, 0.6f, 0.75f + 0.25f * pulse) : new Color(0.45f, 0.95f, 1f, 0.35f + 0.25f * pulse);
            }
        }

        // ------------------------------------------------------------------ hitbox overlay (F9, or -hitboxes for QA)

        readonly List<SpriteRenderer> _bHitDbg = new List<SpriteRenderer>();
        bool _bHitOn, _bHitArgRead;
        float _bHitT;

        /// <summary>Red squares on every blocked floor tile that is not a wall: shows invisible hitboxes.</summary>
        void BHitboxOverlayUpdate()
        {
            if (!_bHitArgRead) { _bHitArgRead = true; _bHitOn = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-hitboxes") >= 0; }
            var kb = Keyboard.current;
            if (kb != null && kb.f9Key.wasPressedThisFrame) _bHitOn = !_bHitOn;
            _bHitT -= Time.unscaledDeltaTime;
            if (_bHitT > 0) return; _bHitT = 0.3f;
            int n = 0;
            if (_bHitOn && _bSolid != null && _bWall != null)
                for (int x = 0; x < _bW; x++)
                    for (int y = 0; y < _bH; y++)
                    {
                        if (!_bSolid[x, y] || _bWall[x, y] || _bKind[x, y] < 0) continue;
                        if (n >= _bHitDbg.Count || _bHitDbg[n] == null)
                        {
                            var sr = BSprite("hitbox_dbg", LdPixel(), Vector2.zero, 30500);
                            if (n >= _bHitDbg.Count) _bHitDbg.Add(sr); else _bHitDbg[n] = sr;
                        }
                        var d = _bHitDbg[n++];
                        d.gameObject.SetActive(true);
                        d.transform.position = new Vector3(x + 0.06f, y + 0.06f, 0);
                        d.transform.localScale = new Vector3(0.88f, 0.88f, 1);
                        d.color = new Color(1f, 0.1f, 0.2f, 0.42f);
                    }
            for (int i = n; i < _bHitDbg.Count; i++) if (_bHitDbg[i] != null) _bHitDbg[i].gameObject.SetActive(false);
        }
        readonly List<(SpriteRenderer bar, System.Func<Color> color)> _ldFrames = new List<(SpriteRenderer, System.Func<Color>)>();

        /// <summary>A bright pulsing ring around a thing on the floor that can be picked up.</summary>
        void LdAddPickRing(BSpot s)
        {
            if (s?.sr == null || s.sr.sprite == null) return;
            var ring = BSprite("pick_ring", Art.PickupRing(s.sr.sprite, s.id), s.sr.transform.position, s.sr.sortingOrder - 1);
            _ldRings.Add((ring, s.sr, s));
        }

        /// <summary>A pulsing frame around a door (tile rect), coloured by its state.</summary>
        void LdAddDoorFrame(RectInt r, System.Func<Color> color, float thickness = 0.14f)
        {
            void Bar(float x, float y, float w, float h)
            {
                var sr = BSprite("door_frame", LdPixel(), new Vector2(x, y), 30002);
                sr.transform.localScale = new Vector3(w, h, 1);
                _ldFrames.Add((sr, color));
            }
            Bar(r.xMin - thickness, r.yMin - thickness, r.width + 2 * thickness, thickness);
            Bar(r.xMin - thickness, r.yMax, r.width + 2 * thickness, thickness);
            Bar(r.xMin - thickness, r.yMin, thickness, r.height);
            Bar(r.xMax, r.yMin, thickness, r.height);
        }

        void LdClearHighlights() { _ldRings.Clear(); _ldFrames.Clear(); _ldUseRings.Clear(); }

        void LdHighlightsUpdate()
        {
            float pulse = Settings.ReducedMotion ? 0.8f : 0.6f + 0.4f * Mathf.Sin(_bAnim * 5f);
            LdUseRingsUpdate(pulse);
            BHitboxOverlayUpdate();
            foreach (var (ring, owner, spot) in _ldRings)
            {
                if (ring == null) continue;
                bool on = owner != null && owner.enabled && owner.gameObject.activeInHierarchy && owner.sprite != null;
                ring.enabled = on;
                if (!on) continue;
                ring.transform.position = owner.transform.position;
                bool locked = _bMode == 1 && LastDockMode && spot != null && LdShipItemLocked(spot);
                ring.color = locked ? new Color(1f, 0.6f, 0.25f, 0.45f * pulse) : new Color(1f, 1f, 1f, pulse);
            }
            foreach (var (bar, color) in _ldFrames)
            {
                if (bar == null) continue;
                var c = color(); c.a *= pulse; bar.color = c;
            }
        }

        /// <summary>LAST DOCK ship: frame the two vault doors (amber locked, green open).</summary>
        void LdFrameVaults()
        {
            const float thickness = 0.035f;
            LdAddDoorFrame(new RectInt(38, 16, 2, 1),
                () => _ldVaultOpen == "power" ? GameUI.CGreen : _ldVaultOpen == null ? LdGold : new Color(0.6f, 0.6f, 0.65f, 0.5f),
                thickness);
            LdAddDoorFrame(new RectInt(39, 6, 2, 1),
                () => _ldVaultOpen == "comms" ? GameUI.CGreen : _ldVaultOpen == null ? LdGold : new Color(0.6f, 0.6f, 0.65f, 0.5f),
                thickness);
        }

        // ------------------------------------------------------------------ sealed rooms never trap the cadet

        /// <summary>When a module's hatch shuts with the cadet inside, step them out through the door first.</summary>
        void LdStepOutOf(DockRoom room)
        {
            if (!LdRects.TryGetValue(room, out var rect) || !LdDoors.TryGetValue(room, out var doors)) return;
            var cell = new Vector2Int(Mathf.FloorToInt(_bPos.x), Mathf.FloorToInt(_bPos.y));
            bool inside = rect.Contains(cell) || doors.Any(d => d.a == cell);
            if (!inside) return;
            var outside = doors[0].b;
            _bPos = new Vector2(outside.x + 0.5f, outside.y + 0.5f);
            _bPath = null; _bGoal = null;
            _ui.Toast(TT($"You stepped out before the {LdRoomName(room)} hatch closed.", $"คุณออกมาก่อนประตู{LdRoomName(room)}ปิด"), GameUI.CAmber);
        }
    }

    public static partial class Art
    {
        static readonly Dictionary<int, (int min, int max)> _opaqueSpan = new Dictionary<int, (int, int)>();

        /// <summary>Leftmost and rightmost opaque pixel column of a sprite (relative to its rect); (0,-1) if unreadable/empty.</summary>
        public static (int min, int max) OpaqueSpanX(Sprite s)
        {
            int id = s.GetInstanceID();
            if (_opaqueSpan.TryGetValue(id, out var span)) return span;
            var tex = s.texture; var r = s.textureRect;
            int w = (int)r.width, h = (int)r.height, x0 = (int)r.x, y0 = (int)r.y, min = int.MaxValue, max = -1;
            try
            {
                var px = tex.GetPixels32();
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        if (px[(y0 + y) * tex.width + x0 + x].a > 40) { if (x < min) min = x; if (x > max) max = x; }
            }
            catch { min = 0; max = w - 1; }   // unreadable texture: use the whole rect
            span = max < 0 ? (0, -1) : (min, max);
            _opaqueSpan[id] = span;
            return span;
        }

        /// <summary>A white outline around a usable machine (tinted by the caller), thick enough to read on big Blender art.</summary>
        public static Sprite UseRing(Sprite s) => Cached("usering_" + s.GetInstanceID(), () =>
        {
            var tex = s.texture;
            var r = s.textureRect;
            int w = (int)r.width, h = (int)r.height, x0 = (int)r.x, y0 = (int)r.y;
            int t = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(w, h) / 36f), 1, 5);
            Color32[] src;
            try { src = tex.GetPixels32(); } catch { return null; }   // a texture that cannot be read: no outline
            bool Op(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && src[(y0 + y) * tex.width + x0 + x].a > 40;
            var c = new PixelCanvas(w + 2 * t, h + 2 * t);
            var white = new Color32(255, 255, 255, 255);
            for (int y = 0; y < h + 2 * t; y++)
                for (int x = 0; x < w + 2 * t; x++)
                {
                    int sx = x - t, sy = y - t;
                    if (Op(sx, sy)) continue;
                    bool near = false;
                    for (int dy = -t; dy <= t && !near; dy++)
                        for (int dx = -t; dx <= t && !near; dx++)
                            if (dx * dx + dy * dy <= t * t && Op(sx + dx, sy + dy)) near = true;
                    if (near) c.Set(x, y, white);
                }
            var pivot = new Vector2((s.pivot.x + t) / (w + 2 * t), (s.pivot.y + t) / (h + 2 * t));
            return c.ToSprite(pivot, s.pixelsPerUnit);
        });

        /// <summary>A 2-pixel ring (white inside, gold outside) around a sprite's shape: "you can pick this up".</summary>
        public static Sprite PickupRing(Sprite s, string key) => Cached("pickring_" + key + "_" + s.GetInstanceID(), () =>
        {
            var tex = s.texture;
            var r = s.textureRect;
            int w = (int)r.width, h = (int)r.height, x0 = (int)r.x, y0 = (int)r.y;
            var src = tex.GetPixels32();
            bool Op(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && src[(y0 + y) * tex.width + x0 + x].a > 0;
            var c = new PixelCanvas(w + 4, h + 4);
            var inner = new Color32(255, 255, 255, 255); var outer = Pal.Gold;
            for (int y = 0; y < h + 4; y++)
                for (int x = 0; x < w + 4; x++)
                {
                    int sx = x - 2, sy = y - 2;
                    if (Op(sx, sy)) continue;
                    bool near1 = Op(sx - 1, sy) || Op(sx + 1, sy) || Op(sx, sy - 1) || Op(sx, sy + 1);
                    if (near1) { c.Set(x, y, inner); continue; }
                    bool near2 = false;
                    for (int dy = -2; dy <= 2 && !near2; dy++)
                        for (int dx = -2; dx <= 2 && !near2; dx++)
                            if (Mathf.Abs(dx) + Mathf.Abs(dy) <= 2 && Op(sx + dx, sy + dy)) near2 = true;
                    if (near2) c.Set(x, y, outer);
                }
            var pivot = new Vector2((s.pivot.x + 2) / (w + 4), (s.pivot.y + 2) / (h + 4));
            return c.ToSprite(pivot, s.pixelsPerUnit);
        });
    }
}
