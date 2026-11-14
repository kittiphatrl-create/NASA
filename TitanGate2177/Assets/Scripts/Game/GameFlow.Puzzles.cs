// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do short hands-on panels (wires, pipes, valves, dials) make a repair feel real and a little tense?
// Date: 2026-11-13
using System;
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// Hands-on repair panels in the style of Among Us tasks, each teaching one real idea: join wires by colour,
    /// unscrew and swap a filter, scrub dust off, turn pipe pieces until water flows, stack water bags round a
    /// bunk against radiation, switch to safe mode without turning off life support, tune a radio dish to a
    /// satellite, tape air leaks and refill the pressure, or crank a radio by hand. Each reports mistakes and time.
    /// A helper standing next to you makes the panel easier. Autoshot and playtest runs solve them by themselves.
    /// </summary>
    public partial class GameFlow
    {
        const float PzW = 1100, PzH = 620;
        Action _pzSolve;
        bool _pzFinished;
        float _pzT0;
        int _pzMistakes;
        Action<int, float> _pzDone;
        VisualElement _pzRoot, _pzBoard;
        Label _pzStatus;
        public bool PuzzleOpen => _pzRoot != null && _ui.ScreenOpen;

        void PzOpen(string type, int help, JobDef d, Action<int, float> done, Action cancel)
        {
            _pzFinished = false; _pzMistakes = 0; _pzT0 = Time.realtimeSinceStartup; _pzDone = done; _pzSolve = null;
            _ui.ShowScreen(root =>
            {
                _pzRoot = root;
                root.style.alignItems = Align.Center; root.style.justifyContent = Justify.Center;
                var dim = Abs(0, 0, 1920, 1080); dim.style.backgroundColor = new Color(0.01f, 0.02f, 0.04f, 0.8f); root.Add(dim);
                var frame = new VisualElement(); frame.style.width = PzW + 60;
                frame.style.backgroundColor = new Color(0.06f, 0.07f, 0.1f, 0.99f); GameUI.Border(frame, GameUI.CAmber, 4, 18);
                frame.style.paddingLeft = frame.style.paddingRight = 30; frame.style.paddingTop = 14; frame.style.paddingBottom = 14;
                var top = new VisualElement(); top.style.flexDirection = FlexDirection.Row; top.style.justifyContent = Justify.SpaceBetween; top.style.alignItems = Align.Center;
                top.Add(_ui.L(TT(d.en, d.th), 30, true, GameUI.CAmber));
                if (help > 0) top.Add(_ui.L(TT("A friend is helping", "มีเพื่อนช่วยอยู่"), 20, true, GameUI.CGreen));
                frame.Add(top);
                _pzStatus = _ui.L(PzHelpText(type), 22, false, GameUI.CText); _pzStatus.style.marginBottom = 8; frame.Add(_pzStatus);
                var board = new VisualElement(); board.style.width = PzW; board.style.height = PzH; board.style.position = Position.Relative; board.style.overflow = Overflow.Hidden;
                board.style.backgroundColor = new Color(0.12f, 0.13f, 0.17f); GameUI.Border(board, new Color(0.3f, 0.32f, 0.38f), 3, 12);
                _pzBoard = board;
                frame.Add(board);
                var foot = new VisualElement(); foot.style.flexDirection = FlexDirection.Row; foot.style.justifyContent = Justify.SpaceBetween; foot.style.alignItems = Align.Center; foot.style.marginTop = 8;
                foot.Add(_ui.L(TT(d.factEn, d.factTh), 16, false, GameUI.CCyan));
                var back = _ui.B(TT("Step back", "ถอยออกมา"), () => { _pzRoot = null; _pzSolve = null; cancel?.Invoke(); }, 19);
                back.style.flexShrink = 0; foot.Add(back);
                frame.Add(foot);
                root.Add(frame);
                switch (type)
                {
                    case "wires": PzWires(board, help); break;
                    case "screws": PzScrews(board, help); break;
                    case "scrub": PzScrub(board, help); break;
                    case "pipes": PzPipes(board, help); break;
                    case "shield": PzShield(board, help, d); break;
                    case "switches": PzSwitches(board, help); break;
                    case "tune": PzTune(board, help); break;
                    case "leak": PzLeak(board, help); break;
                    default: PzCrank(board); break;
                }
                // screenshot and playtest runs solve the panel by themselves after a moment
                if (_autoLog != null) root.schedule.Execute(() => { if (_pzRoot == root) _pzSolve?.Invoke(); }).StartingIn(1500);
            });
        }

        string PzHelpText(string type)
        {
            switch (type)
            {
                case "wires": return TT("Drag each wire on the left to the socket of the same colour.", "ลากสายไฟทางซ้ายไปเสียบช่องสีเดียวกันทางขวา");
                case "screws": return TT("Unscrew the 4 screws, drag the dusty filter to the bin, slot in the new one, screw it back.", "คลายน็อต 4 ตัว ลากไส้กรองฝุ่นไปทิ้ง ใส่อันใหม่ แล้วขันน็อตกลับ");
                case "scrub": return TT("Hold the mouse button and rub until the dust is gone.", "กดเมาส์ค้างแล้วถูจนฝุ่นหมด");
                case "pipes": return TT("Click pipe pieces to turn them until water flows from left to right.", "คลิกหมุนท่อทีละชิ้นจนน้ำไหลจากซ้ายไปขวา");
                case "shield": return TT("Drag bags into the empty spaces round the bunk. Fill at least 6 of 8.", "ลากถุงไปวางในช่องว่างรอบที่นอน ต้องวางอย่างน้อย 6 จาก 8 ช่อง");
                case "switches": return TT("Switch OFF what we can live without. Keep life support and heating ON.", "ปิดเครื่องที่ไม่จำเป็น แต่ห้ามปิดระบบช่วยชีวิตและเครื่องทำความร้อน");
                case "tune": return TT("Drag both dials until your signal matches the satellite (the green bar fills).", "เลื่อนปุ่มทั้งสองจนคลื่นตรงกับดาวเทียม (แถบสีเขียวเต็ม)");
                case "leak": return TT("Click each hissing crack to tape it, then hold PUMP until the needle sits in the green.", "คลิกรอยร้าวที่มีเสียงฟู่เพื่อติดเทป แล้วกดค้างที่ปุ่มเติมอากาศจนเข็มอยู่ในช่องเขียว");
                default: return TT("Click fast (or press Space) to crank the dynamo and charge the radio.", "คลิกรัว ๆ (หรือกด Space) เพื่อหมุนไดนาโมชาร์จวิทยุ");
            }
        }

        void PzMistake(string whyEn, string whyTh)
        {
            _pzMistakes++;
            _sfx.Play("bad"); _sShake = 0.25f;
            if (_pzStatus != null) { _pzStatus.text = GameUI.Thai(TT(whyEn, whyTh)); _pzStatus.style.color = GameUI.CRed; }
            if (_pzBoard != null)
            {
                var f = Abs(0, 0, PzW, PzH); f.style.backgroundColor = new Color(1f, 0.2f, 0.1f, 0.3f); _pzBoard.Add(f);
                f.schedule.Execute(() => f.RemoveFromHierarchy()).StartingIn(180);
            }
        }

        void PzSay(string en, string th, Color? c = null)
        {
            if (_pzStatus == null) return;
            _pzStatus.text = GameUI.Thai(TT(en, th)); _pzStatus.style.color = c ?? GameUI.CText;
        }

        void PzComplete()
        {
            if (_pzFinished || _pzRoot == null) return;
            _pzFinished = true;
            float secs = Time.realtimeSinceStartup - _pzT0;
            _sfx.Play("good");
            PzSay("Done!", "เรียบร้อย!", GameUI.CGreen);
            var ok = _ui.L(TT("DONE", "เสร็จแล้ว"), 90, true, GameUI.CGreen);
            ok.style.position = Position.Absolute; ok.style.left = 0; ok.style.right = 0; ok.style.top = PzH / 2 - 70; ok.style.unityTextAlign = TextAnchor.MiddleCenter;
            ok.pickingMode = PickingMode.Ignore;
            _pzBoard?.Add(ok);
            var root = _pzRoot; var done = _pzDone; int m = _pzMistakes;
            root.schedule.Execute(() => { if (_pzRoot != root) return; _pzRoot = null; _pzSolve = null; done?.Invoke(m, secs); }).StartingIn(750);
        }

        static VisualElement Box(float x, float y, float w, float h, Color c, float r = 6)
        {
            var e = Abs(x, y, w, h); e.style.backgroundColor = c; Round(e, r); return e;
        }

        /// <summary>Drag an element with the pointer (captured). onDrop gets the centre in board space; false snaps it back.</summary>
        void PzDrag(VisualElement e, VisualElement board, Func<Vector2, bool> onDrop)
        {
            e.pickingMode = PickingMode.Position;
            Vector2 grab = Vector2.zero, home = Vector2.zero; bool drag = false;
            e.RegisterCallback<PointerDownEvent>(ev =>
            {
                drag = true; home = new Vector2(e.resolvedStyle.left, e.resolvedStyle.top);
                grab = board.WorldToLocal(ev.position) - home;
                e.CapturePointer(ev.pointerId); e.BringToFront(); _sfx.Play("card");
            });
            e.RegisterCallback<PointerMoveEvent>(ev =>
            {
                if (!drag) return;
                var p = board.WorldToLocal(ev.position) - grab;
                e.style.left = p.x; e.style.top = p.y;
            });
            e.RegisterCallback<PointerUpEvent>(ev =>
            {
                if (!drag) return;
                drag = false; e.ReleasePointer(ev.pointerId);
                var c = new Vector2(e.resolvedStyle.left + e.resolvedStyle.width / 2, e.resolvedStyle.top + e.resolvedStyle.height / 2);
                if (!onDrop(c)) { e.style.left = home.x; e.style.top = home.y; }
            });
        }

        // ------------------------------------------------------------------ wires

        static readonly Color[] WireCol = { new Color(0.95f, 0.25f, 0.22f), new Color(0.3f, 0.55f, 1f), new Color(1f, 0.85f, 0.2f), new Color(0.85f, 0.35f, 0.95f) };

        void PzWires(VisualElement b, int help)
        {
            var rng = new System.Random(Environment.TickCount);
            int n = 4;
            var order = new List<int> { 0, 1, 2, 3 };
            for (int i = n - 1; i > 0; i--) { int j = rng.Next(i + 1); (order[i], order[j]) = (order[j], order[i]); }
            Vector2 L(int i) => new Vector2(200, 110 + i * 130);
            Vector2 R(int color) => new Vector2(900, 110 + order.IndexOf(color) * 130);
            var linked = new bool[n];
            int dragging = -1; Vector2 dragAt = Vector2.zero;
            b.Add(Box(120, 40, 120, 540, new Color(0.18f, 0.19f, 0.24f)));
            b.Add(Box(860, 40, 120, 540, new Color(0.18f, 0.19f, 0.24f)));
            var fuse = Box(470, 20, 160, 54, new Color(0.25f, 0.27f, 0.33f), 10); b.Add(fuse);
            var fuseIc = Abs(10, 3, 48, 48); fuseIc.style.backgroundImage = new StyleBackground(Art.JobItem("fuse")); fuse.Add(fuseIc);
            var fl = _ui.L(TT("new fuse", "ฟิวส์ใหม่"), 18, true, GameUI.CAmber); fl.style.position = Position.Absolute; fl.style.left = 64; fl.style.top = 12; fuse.Add(fl);
            var canvas = Abs(0, 0, PzW, PzH);
            canvas.generateVisualContent += ctx =>
            {
                var p = ctx.painter2D;
                p.lineCap = LineCap.Round; p.lineWidth = 22;
                void Wire(Vector2 a, Vector2 z, Color c)
                {
                    p.strokeColor = new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f); p.lineWidth = 26;
                    p.BeginPath(); p.MoveTo(a); p.BezierCurveTo(a + new Vector2(180, 40), z + new Vector2(-180, 40), z); p.Stroke();
                    p.strokeColor = c; p.lineWidth = 18;
                    p.BeginPath(); p.MoveTo(a); p.BezierCurveTo(a + new Vector2(180, 40), z + new Vector2(-180, 40), z); p.Stroke();
                }
                for (int i = 0; i < n; i++) if (linked[i]) Wire(L(i) + new Vector2(36, 0), R(i) - new Vector2(36, 0), WireCol[i]);
                if (dragging >= 0) Wire(L(dragging) + new Vector2(36, 0), dragAt, WireCol[dragging]);
            };
            b.Add(canvas);
            for (int i = 0; i < n; i++)
            {
                var r = Box(R(i).x - 34, R(i).y - 30, 68, 60, new Color(0.08f, 0.08f, 0.1f), 8);
                var dot = Box(18, 14, 32, 32, WireCol[i], 16); r.Add(dot);
                b.Add(r);
            }
            void Check()
            {
                canvas.MarkDirtyRepaint();
                int c = 0; foreach (var x in linked) if (x) c++;
                if (c == n) PzComplete();
                else PzSay($"{c}/{n} " + TT("wires joined", "เส้นต่อแล้ว"), $"{c}/{n} เส้นต่อแล้ว");
            }
            for (int i = 0; i < n; i++)
            {
                int id = i;
                var node = Box(L(i).x - 36, L(i).y - 30, 72, 60, WireCol[i], 10);
                node.Add(Box(48, 18, 24, 24, new Color(0.85f, 0.8f, 0.6f), 4));
                node.pickingMode = PickingMode.Position;
                node.RegisterCallback<PointerDownEvent>(ev => { if (linked[id]) return; dragging = id; dragAt = b.WorldToLocal(ev.position); node.CapturePointer(ev.pointerId); _sfx.Play("card"); canvas.MarkDirtyRepaint(); });
                node.RegisterCallback<PointerMoveEvent>(ev => { if (dragging != id) return; dragAt = b.WorldToLocal(ev.position); canvas.MarkDirtyRepaint(); });
                node.RegisterCallback<PointerUpEvent>(ev =>
                {
                    if (dragging != id) return;
                    node.ReleasePointer(ev.pointerId); dragging = -1;
                    var at = b.WorldToLocal(ev.position);
                    for (int c = 0; c < n; c++)
                        if (Vector2.Distance(at, R(c)) < 60)
                        {
                            if (c == id) { linked[id] = true; _sfx.Play("click"); }
                            else PzMistake("Zap! Wrong colour. That cost a little power.", "ช็อต! ผิดสี เสียไฟไปนิดหน่อย");
                            break;
                        }
                    Check();
                });
                b.Add(node);
            }
            for (int i = 0; i < Mathf.Min(help, 2); i++) linked[i] = true;
            Check();
            _pzSolve = () => { for (int i = 0; i < n; i++) linked[i] = true; Check(); };
        }

        // ------------------------------------------------------------------ filter swap

        void PzScrews(VisualElement b, int help)
        {
            int phase = 0, screws = 4;
            var housing = Box(330, 70, 440, 420, new Color(0.55f, 0.58f, 0.63f), 14); b.Add(housing);
            var slot = Box(380, 120, 340, 320, new Color(0.15f, 0.16f, 0.2f), 6); b.Add(slot);
            var old = Box(390, 130, 320, 300, new Color(0.55f, 0.38f, 0.25f), 6);
            for (int i = 0; i < 8; i++) old.Add(Box(14 + i * 38, 10, 10, 280, new Color(0.42f, 0.28f, 0.18f), 2));
            for (int i = 0; i < 30; i++) old.Add(Box(UnityEngine.Random.Range(0, 300), UnityEngine.Random.Range(0, 280), 14, 10, new Color(0.7f, 0.45f, 0.28f, 0.9f), 5));
            b.Add(old);
            var bin = Box(850, 380, 200, 180, new Color(0.2f, 0.22f, 0.26f), 12);
            var bl = _ui.L(TT("dust bin", "ถังฝุ่น"), 20, true, GameUI.CDim); bl.style.position = Position.Absolute; bl.style.top = 70; bl.style.left = 0; bl.style.right = 0; bl.style.unityTextAlign = TextAnchor.MiddleCenter; bin.Add(bl);
            b.Add(bin);
            var fresh = Box(40, 300, 220, 200, new Color(0.93f, 0.95f, 0.97f), 6);
            for (int i = 0; i < 6; i++) fresh.Add(Box(14 + i * 34, 10, 8, 180, new Color(0.78f, 0.82f, 0.88f), 2));
            var fl = _ui.L(TT("new filter", "ไส้กรองใหม่"), 18, true, GameUI.CText); fl.style.position = Position.Absolute; fl.style.top = 205; fl.style.left = 0; fl.style.right = 0; fl.style.unityTextAlign = TextAnchor.MiddleCenter; fresh.Add(fl);
            b.Add(fresh);
            var screwEls = new List<VisualElement>();
            Vector2[] at = { new Vector2(345, 85), new Vector2(715, 85), new Vector2(345, 435), new Vector2(715, 435) };
            void Next()
            {
                if (phase == 0 && screws == 0) { phase = 1; PzSay("Now drag the dusty filter into the bin.", "ลากไส้กรองฝุ่นไปทิ้งในถัง"); }
                else if (phase == 3 && screws == 4) PzComplete();
            }
            for (int i = 0; i < 4; i++)
            {
                var s = Box(at[i].x, at[i].y, 40, 40, new Color(0.85f, 0.86f, 0.9f), 20);
                s.Add(Box(8, 17, 24, 6, new Color(0.3f, 0.3f, 0.35f), 2));
                s.pickingMode = PickingMode.Position;
                s.RegisterCallback<ClickEvent>(_ =>
                {
                    if (phase == 0 && s.style.opacity.value > 0.5f) { s.style.opacity = 0.15f; s.style.rotate = new Rotate(new Angle(90)); screws--; _sfx.Play("click"); Next(); }
                    else if (phase == 3 && s.style.opacity.value < 0.5f) { s.style.opacity = 1f; s.style.rotate = new Rotate(new Angle(0)); screws++; _sfx.Play("click"); Next(); }
                });
                s.style.opacity = 1f;
                screwEls.Add(s); b.Add(s);
            }
            PzDrag(old, b, c =>
            {
                if (phase != 1) return false;
                if (c.x > 830 && c.y > 330) { old.RemoveFromHierarchy(); phase = 2; _sfx.Play("good", 0.5f); PzSay("Slot the new filter into the frame.", "ใส่ไส้กรองใหม่เข้าไปในกรอบ"); return true; }
                return false;
            });
            PzDrag(fresh, b, c =>
            {
                if (phase != 2) { PzSay("Take the old filter out first.", "ต้องเอาอันเก่าออกก่อน", GameUI.CAmber); return false; }
                if (Mathf.Abs(c.x - 550) < 140 && Mathf.Abs(c.y - 280) < 140)
                {
                    fresh.style.left = 390; fresh.style.top = 130; fresh.style.width = 320; fresh.style.height = 300;
                    phase = 3; _sfx.Play("click");
                    PzSay("Screw the 4 screws back in.", "ขันน็อตทั้ง 4 ตัวกลับเข้าไป");
                    foreach (var s in screwEls) s.BringToFront();
                    if (help > 0) for (int i = 0; i < 2; i++) { screwEls[i].style.opacity = 1f; screws++; }
                    Next();
                    return true;
                }
                return false;
            });
            if (help > 0) for (int i = 0; i < 2; i++) { screwEls[i].style.opacity = 0.15f; screws--; }
            _pzSolve = PzComplete;
        }

        // ------------------------------------------------------------------ scrub

        void PzScrub(VisualElement b, int help)
        {
            const int cw = 14, ch = 7; const float cs = 52;
            float ox = (PzW - cw * cs) / 2, oy = 110;
            var under = Box(ox, oy, cw * cs, ch * cs, new Color(0.9f, 0.93f, 0.96f), 6); b.Add(under);
            for (int i = 0; i < 12; i++) under.Add(Box(14 + i * 60, 8, 10, ch * cs - 16, new Color(0.75f, 0.8f, 0.86f), 2));
            var cells = new VisualElement[cw, ch]; int left = cw * ch, total = left;
            var rng = new System.Random(Environment.TickCount);
            for (int x = 0; x < cw; x++) for (int y = 0; y < ch; y++)
            {
                var c = Abs(ox + x * cs - 4, oy + y * cs - 4, cs + 8, cs + 8);
                float t = (float)rng.NextDouble() * 0.15f;
                c.style.backgroundColor = new Color(0.55f + t, 0.36f + t * 0.5f, 0.22f, 0.97f); Round(c, 14);
                cells[x, y] = c; b.Add(c);
            }
            var drop = _ui.L(TT("water bag", "ถุงน้ำ"), 18, true, GameUI.CCyan); drop.style.position = Position.Absolute; drop.style.left = 40; drop.style.top = 40; b.Add(drop);
            bool rubbing = false;
            void Clear(int x, int y)
            {
                if (x < 0 || y < 0 || x >= cw || y >= ch || cells[x, y] == null) return;
                cells[x, y].RemoveFromHierarchy(); cells[x, y] = null; left--;
            }
            void Rub(Vector2 p)
            {
                int x = Mathf.FloorToInt((p.x - ox) / cs), y = Mathf.FloorToInt((p.y - oy) / cs);
                Clear(x, y); Clear(x + 1, y); Clear(x, y + 1);
                float k = 1 - left / (float)total;
                PzSay(TT("Clean: ", "สะอาดแล้ว ") + Mathf.RoundToInt(k * 100) + "%", "สะอาดแล้ว " + Mathf.RoundToInt(k * 100) + "%");
                if (k >= 0.9f) PzComplete();
            }
            b.pickingMode = PickingMode.Position;
            b.RegisterCallback<PointerDownEvent>(ev => { rubbing = true; b.CapturePointer(ev.pointerId); Rub(b.WorldToLocal(ev.position)); _sfx.Play("whoosh", 0.4f); });
            b.RegisterCallback<PointerMoveEvent>(ev => { if (rubbing) Rub(b.WorldToLocal(ev.position)); });
            b.RegisterCallback<PointerUpEvent>(ev => { rubbing = false; b.ReleasePointer(ev.pointerId); });
            if (help > 0) for (int x = 0; x < cw; x++) for (int y = 0; y < ch; y++) if ((x + y) % 2 == 0) Clear(x, y);
            _pzSolve = PzComplete;
        }

        // ------------------------------------------------------------------ pipes

        static int RotMask(int m, int k) { for (int i = 0; i < k; i++) m = ((m << 1) | (m >> 3)) & 15; return m; }   // N1 E2 S4 W8, clockwise

        void PzPipes(VisualElement b, int help)
        {
            const int C = 5, R = 3; const float T = 132;
            float ox = (PzW - C * T) / 2, oy = 90;
            var rng = new System.Random(Environment.TickCount);
            var need = new int[C, R];
            int row = 1;
            for (int c = 0; c < C; c++)
            {
                int to = rng.Next(R);
                int step = to > row ? 1 : -1;
                if (to == row) need[c, row] |= 8 | 2;
                else
                {
                    need[c, row] |= 8 | (step > 0 ? 4 : 1);
                    for (int r = row + step; r != to; r += step) need[c, r] |= 1 | 4;
                    need[c, to] |= (step > 0 ? 1 : 4) | 2;
                }
                row = to;
            }
            int outRow = row;
            var shape = new int[C, R]; var rot = new int[C, R]; var solved = new int[C, R];
            for (int c = 0; c < C; c++) for (int r = 0; r < R; r++)
            {
                int m = need[c, r];
                bool straight = m == 5 || m == 10;
                if (m == 0) straight = rng.NextDouble() < 0.5;
                shape[c, r] = straight ? 5 : 3;
                solved[c, r] = -1;
                if (m != 0) for (int k = 0; k < 4; k++) if (RotMask(shape[c, r], k) == m) { solved[c, r] = k; break; }
                rot[c, r] = rng.Next(4);
                if (solved[c, r] >= 0 && rot[c, r] % (straight ? 2 : 4) == solved[c, r] % (straight ? 2 : 4)) rot[c, r] = (rot[c, r] + 1) % 4;
            }
            if (help > 0) { int fixedN = 0; for (int c = 0; c < C; c++) for (int r = 0; r < R; r++) if (solved[c, r] >= 0 && fixedN++ % 2 == 0) rot[c, r] = solved[c, r]; }
            var tiles = new VisualElement[C, R];
            var inlet = Box(ox - 60, oy + 1 * T + T / 2 - 18, 64, 36, new Color(0.3f, 0.6f, 1f), 6); b.Add(inlet);
            var outlet = Box(ox + C * T - 4, oy + outRow * T + T / 2 - 18, 64, 36, new Color(0.3f, 0.32f, 0.36f), 6); b.Add(outlet);
            var inl = _ui.L(TT("in", "น้ำเข้า"), 16, true, GameUI.CCyan); inl.style.position = Position.Absolute; inl.style.left = ox - 66; inl.style.top = oy + T + T / 2 + 22; b.Add(inl);
            var outl = _ui.L(TT("tank", "ถังน้ำ"), 16, true, GameUI.CCyan); outl.style.position = Position.Absolute; outl.style.left = ox + C * T; outl.style.top = oy + outRow * T + T / 2 + 22; b.Add(outl);
            bool[,] wet = new bool[C, R];
            bool Flow()
            {
                wet = new bool[C, R];
                var q = new Queue<Vector2Int>();
                if ((RotMask(shape[0, 1], rot[0, 1]) & 8) != 0) { wet[0, 1] = true; q.Enqueue(new Vector2Int(0, 1)); }
                bool reached = false;
                while (q.Count > 0)
                {
                    var p = q.Dequeue(); int m = RotMask(shape[p.x, p.y], rot[p.x, p.y]);
                    if (p.x == C - 1 && p.y == outRow && (m & 2) != 0) reached = true;
                    void Go(int bit, int dx, int dy, int back)
                    {
                        if ((m & bit) == 0) return;
                        int nx = p.x + dx, ny = p.y + dy;
                        if (nx < 0 || ny < 0 || nx >= C || ny >= R || wet[nx, ny]) return;
                        if ((RotMask(shape[nx, ny], rot[nx, ny]) & back) == 0) return;
                        wet[nx, ny] = true; q.Enqueue(new Vector2Int(nx, ny));
                    }
                    Go(1, 0, -1, 4); Go(2, 1, 0, 8); Go(4, 0, 1, 1); Go(8, -1, 0, 2);
                }
                return reached;
            }
            void Draw()
            {
                bool done = Flow();
                outlet.style.backgroundColor = done ? new Color(0.3f, 0.6f, 1f) : new Color(0.3f, 0.32f, 0.36f);
                for (int c = 0; c < C; c++) for (int r = 0; r < R; r++)
                {
                    var t = tiles[c, r]; t.Clear();
                    int m = RotMask(shape[c, r], rot[c, r]);
                    var col = wet[c, r] ? new Color(0.35f, 0.65f, 1f) : new Color(0.55f, 0.57f, 0.62f);
                    float h = T / 2, w = 34;
                    t.Add(Box(h - w / 2, h - w / 2, w, w, col, 4));
                    if ((m & 1) != 0) t.Add(Box(h - w / 2, 0, w, h, col, 2));
                    if ((m & 4) != 0) t.Add(Box(h - w / 2, h, w, h, col, 2));
                    if ((m & 8) != 0) t.Add(Box(0, h - w / 2, h, w, col, 2));
                    if ((m & 2) != 0) t.Add(Box(h, h - w / 2, h, w, col, 2));
                }
                if (done) PzComplete();
            }
            for (int c = 0; c < C; c++) for (int r = 0; r < R; r++)
            {
                int cc = c, rr = r;
                var t = Box(ox + c * T + 3, oy + r * T + 3, T - 6, T - 6, new Color(0.17f, 0.18f, 0.23f), 8);
                t.pickingMode = PickingMode.Position;
                t.RegisterCallback<ClickEvent>(_ => { rot[cc, rr] = (rot[cc, rr] + 1) % 4; _sfx.Play("click"); Draw(); });
                tiles[c, r] = t; b.Add(t);
            }
            Draw();
            _pzSolve = () => { for (int c = 0; c < C; c++) for (int r = 0; r < R; r++) if (solved[c, r] >= 0) rot[c, r] = solved[c, r]; Draw(); };
        }

        // ------------------------------------------------------------------ radiation wall

        void PzShield(VisualElement b, int help, JobDef d)
        {
            string bagItem = d.id == "storm_last" ? "sack" : BagHas("shield_kit") ? "shield_kit" : "water_bag";
            var bunk = Box(430, 230, 240, 150, new Color(0.35f, 0.4f, 0.55f), 12); b.Add(bunk);
            var bunkL = _ui.L(TT("bunk", "ที่นอน"), 22, true, GameUI.CText); bunkL.style.position = Position.Absolute; bunkL.style.top = 56; bunkL.style.left = 0; bunkL.style.right = 0; bunkL.style.unityTextAlign = TextAnchor.MiddleCenter; bunk.Add(bunkL);
            var sun = Box(960, 30, 100, 100, new Color(1f, 0.75f, 0.2f), 50); b.Add(sun);
            var rays = _ui.L(TT("solar particles", "อนุภาคจากดวงอาทิตย์"), 17, true, GameUI.CAmber); rays.style.position = Position.Absolute; rays.style.left = 880; rays.style.top = 140; b.Add(rays);
            Vector2[] slots = { new Vector2(370, 110), new Vector2(495, 110), new Vector2(620, 110), new Vector2(370, 400), new Vector2(495, 400), new Vector2(620, 400), new Vector2(300, 255), new Vector2(690, 255) };
            var taken = new bool[slots.Length];
            int filled = 0;
            var meter = _ui.L("", 24, true, GameUI.CAmber); meter.style.position = Position.Absolute; meter.style.left = 760; meter.style.top = 520; b.Add(meter);
            var doneBtn = _ui.B(TT("That is enough", "พอแล้ว"), PzComplete, 22, true); doneBtn.style.position = Position.Absolute; doneBtn.style.left = 860; doneBtn.style.top = 560; doneBtn.style.display = DisplayStyle.None; b.Add(doneBtn);
            foreach (var s in slots) { var e = Box(s.x, s.y, 110, 74, new Color(1, 1, 1, 0.06f), 8); GameUI.Border(e, new Color(1, 1, 1, 0.3f), 2, 8); b.Add(e); }
            void Update()
            {
                meter.text = GameUI.Thai(TT("Blocked: ", "กันรังสีได้ ") + Mathf.RoundToInt(filled / 8f * 100) + "%");
                meter.style.color = filled >= 6 ? GameUI.CGreen : GameUI.CAmber;
                doneBtn.style.display = filled >= 6 && filled < 8 ? DisplayStyle.Flex : DisplayStyle.None;
                if (filled >= 8) PzComplete();
            }
            void Place(VisualElement bag, int i) { taken[i] = true; filled++; bag.style.left = slots[i].x + 5; bag.style.top = slots[i].y + 2; bag.pickingMode = PickingMode.Ignore; _sfx.Play("thud", 0.5f); Update(); }
            var bags = new List<VisualElement>();
            for (int i = 0; i < 8; i++)
            {
                var bag = Abs(40 + (i % 2) * 110, 120 + (i / 2) * 100, 100, 70);
                bag.style.backgroundImage = new StyleBackground(Art.JobItem(bagItem));
                bags.Add(bag); b.Add(bag);
                var me = bag;
                PzDrag(bag, b, c =>
                {
                    for (int s = 0; s < slots.Length; s++)
                        if (!taken[s] && Vector2.Distance(c, slots[s] + new Vector2(55, 37)) < 70) { Place(me, s); return true; }
                    return false;
                });
            }
            for (int i = 0; i < Mathf.Min(8, help * 2); i++) Place(bags[i], i);
            Update();
            _pzSolve = () => { for (int i = 0; i < 8; i++) if (!taken[i]) Place(bags[i], i); };
        }

        // ------------------------------------------------------------------ safe mode switches

        void PzSwitches(VisualElement b, int help)
        {
            var items = new (string en, string th, bool keep)[]
            {
                ("Life support", "ระบบช่วยชีวิต", true), ("Heating", "เครื่องทำความร้อน", true), ("Room lights", "ไฟส่องสว่าง", false),
                ("Rover charger", "ที่ชาร์จรถสำรวจ", false), ("Grow lamps", "ไฟปลูกพืช", false), ("Science lab", "ห้องทดลอง", false),
            };
            var on = new bool[items.Length];
            for (int i = 0; i < on.Length; i++) on[i] = true;
            var rows = new List<(VisualElement sw, Label state)>();
            void Check()
            {
                for (int i = 0; i < items.Length; i++)
                {
                    var (sw, st) = rows[i];
                    sw.style.backgroundColor = on[i] ? new Color(0.2f, 0.75f, 0.4f) : new Color(0.35f, 0.36f, 0.4f);
                    st.text = on[i] ? "ON" : "OFF";
                }
                bool ok = true;
                for (int i = 0; i < items.Length; i++) if (on[i] != items[i].keep) ok = false;
                if (ok) PzComplete();
            }
            for (int i = 0; i < items.Length; i++)
            {
                int id = i;
                float y = 30 + i * 95;
                var row = Box(180, y, 740, 80, new Color(0.17f, 0.18f, 0.23f), 10); b.Add(row);
                var name = _ui.L(TT(items[i].en, items[i].th), 26, true, GameUI.CText); name.style.position = Position.Absolute; name.style.left = 24; name.style.top = 20; row.Add(name);
                if (help > 0 && items[i].keep) { var tag = _ui.L(TT("keep on!", "ห้ามปิด!"), 18, true, GameUI.CGreen); tag.style.position = Position.Absolute; tag.style.left = 330; tag.style.top = 26; row.Add(tag); }
                var sw = Box(560, 14, 150, 52, new Color(0.2f, 0.75f, 0.4f), 26); sw.pickingMode = PickingMode.Position;
                var st = _ui.L("ON", 22, true, GameUI.CText); st.style.position = Position.Absolute; st.style.left = 0; st.style.right = 0; st.style.top = 10; st.style.unityTextAlign = TextAnchor.MiddleCenter; st.pickingMode = PickingMode.Ignore; sw.Add(st);
                sw.RegisterCallback<ClickEvent>(_ =>
                {
                    on[id] = !on[id]; _sfx.Play("click");
                    if (!on[id] && items[id].keep)
                    {
                        PzMistake("ALARM! That one keeps us alive. Switched back on.", "สัญญาณเตือน! อันนั้นช่วยชีวิตเรา เปิดกลับให้แล้ว");
                        b.schedule.Execute(() => { on[id] = true; Check(); }).StartingIn(500);
                    }
                    Check();
                });
                row.Add(sw);
                rows.Add((sw, st));
            }
            Check();
            _pzSolve = () => { for (int i = 0; i < items.Length; i++) on[i] = items[i].keep; Check(); };
        }

        // ------------------------------------------------------------------ radio tuning

        void PzTune(VisualElement b, int help)
        {
            var rng = new System.Random(Environment.TickCount);
            float fT = 0.2f + (float)rng.NextDouble() * 0.6f, aT = 0.15f + (float)rng.NextDouble() * 0.7f;
            float f = fT < 0.5f ? 0.9f : 0.1f, a = help > 0 ? aT : aT < 0.5f ? 0.92f : 0.08f;
            float hold = 0;
            float Signal() => Mathf.Clamp01(1f - (Mathf.Abs(f - fT) + Mathf.Abs(a - aT)) * 1.6f);
            var screen = Box(80, 30, 940, 250, new Color(0.03f, 0.08f, 0.06f), 10); b.Add(screen);
            var wave = Abs(0, 0, 940, 250);
            wave.generateVisualContent += ctx =>
            {
                var p = ctx.painter2D; p.lineWidth = 4;
                void Wave(float fr, float amp, Color c)
                {
                    p.strokeColor = c; p.BeginPath();
                    for (int x = 0; x <= 940; x += 6)
                    {
                        float y = 125 + Mathf.Sin(x / 940f * Mathf.PI * 2 * (2 + fr * 8)) * amp;
                        if (x == 0) p.MoveTo(new Vector2(x, y)); else p.LineTo(new Vector2(x, y));
                    }
                    p.Stroke();
                }
                Wave(fT, 80, new Color(0.3f, 0.6f, 0.4f, 0.5f));
                Wave(f, 20 + 80 * Signal(), new Color(0.4f, 1f, 0.55f));
            };
            screen.Add(wave);
            var sigTrack = Box(80, 300, 940, 30, new Color(0.1f, 0.12f, 0.15f), 15); b.Add(sigTrack);
            var sig = Box(0, 0, 10, 30, GameUI.CGreen, 15); sigTrack.Add(sig);
            var sigL = _ui.L(TT("signal", "สัญญาณ"), 18, true, GameUI.CDim); sigL.style.position = Position.Absolute; sigL.style.left = 80; sigL.style.top = 334; b.Add(sigL);
            VisualElement Slider(float y, string en, string th, Func<float> get, Action<float> set)
            {
                var lab = _ui.L(TT(en, th), 22, true, GameUI.CText); lab.style.position = Position.Absolute; lab.style.left = 80; lab.style.top = y - 4; b.Add(lab);
                var track = Box(330, y, 690, 26, new Color(0.2f, 0.22f, 0.28f), 13); track.pickingMode = PickingMode.Position; b.Add(track);
                var knob = Box(0, -12, 50, 50, GameUI.CAmber, 25); knob.pickingMode = PickingMode.Ignore; track.Add(knob);
                bool drag = false;
                void Move(Vector2 world) { var l = track.WorldToLocal(world); set(Mathf.Clamp01(l.x / 690f)); }
                track.RegisterCallback<PointerDownEvent>(ev => { drag = true; track.CapturePointer(ev.pointerId); Move(ev.position); });
                track.RegisterCallback<PointerMoveEvent>(ev => { if (drag) Move(ev.position); });
                track.RegisterCallback<PointerUpEvent>(ev => { drag = false; track.ReleasePointer(ev.pointerId); });
                knob.schedule.Execute(() => knob.style.left = get() * 690 - 25).Every(16);
                return track;
            }
            Slider(400, "Dish angle", "ทิศของจาน", () => a, v => a = v);
            Slider(490, "Frequency", "ความถี่", () => f, v => f = v);
            float last = Time.realtimeSinceStartup;
            b.schedule.Execute(() =>
            {
                float now = Time.realtimeSinceStartup, dt = now - last; last = now;
                float s = Signal();
                sig.style.width = Length.Percent(s * 100);
                sig.style.backgroundColor = s >= 0.9f ? GameUI.CGreen : s > 0.6f ? GameUI.CAmber : GameUI.CRed;
                wave.MarkDirtyRepaint();
                if (s >= 0.9f) { hold += dt; PzSay("Locked on... hold it!", "จับสัญญาณได้แล้ว... ค้างไว้!", GameUI.CGreen); if (hold > 1.2f) PzComplete(); }
                else hold = Mathf.Max(0, hold - dt);
            }).Every(30).Until(() => _pzFinished || _pzRoot == null);
            _pzSolve = () => { f = fT; a = aT; hold = 2; PzComplete(); };
        }

        // ------------------------------------------------------------------ air leak

        void PzLeak(VisualElement b, int help)
        {
            var rng = new System.Random(Environment.TickCount);
            var wall = Box(40, 30, 760, 560, new Color(0.62f, 0.64f, 0.68f), 10); b.Add(wall);
            for (int i = 1; i < 4; i++) wall.Add(Box(i * 190, 0, 4, 560, new Color(0.5f, 0.52f, 0.56f), 0));
            for (int i = 1; i < 3; i++) wall.Add(Box(0, i * 186, 760, 4, new Color(0.5f, 0.52f, 0.56f), 0));
            int open = 3;
            float pressure = 0.62f; bool pumping = false; float calm = 0;
            var gauge = Box(840, 40, 220, 400, new Color(0.1f, 0.11f, 0.14f), 12); b.Add(gauge);
            var green = Box(30, 400 - 0.75f * 360 - 20, 160, 0.15f * 360, new Color(0.2f, 0.7f, 0.35f, 0.5f), 4); gauge.Add(green);
            var red = Box(30, 400 - 1f * 360 - 20, 160, 0.1f * 360, new Color(0.85f, 0.2f, 0.2f, 0.5f), 4); gauge.Add(red);
            var needle = Box(20, 0, 180, 8, GameUI.CAmber, 4); gauge.Add(needle);
            var gl = _ui.L(TT("pressure", "ความดัน"), 18, true, GameUI.CDim); gl.style.position = Position.Absolute; gl.style.left = 900; gl.style.top = 446; b.Add(gl);
            var pump = Box(850, 490, 200, 100, new Color(0.25f, 0.35f, 0.6f), 16); pump.pickingMode = PickingMode.Position;
            var pl = _ui.L(TT("PUMP (hold)", "เติมอากาศ (กดค้าง)"), 20, true, GameUI.CText); pl.style.position = Position.Absolute; pl.style.left = 0; pl.style.right = 0; pl.style.top = 34; pl.style.unityTextAlign = TextAnchor.MiddleCenter; pl.pickingMode = PickingMode.Ignore; pump.Add(pl);
            b.Add(pump);
            pump.RegisterCallback<PointerDownEvent>(ev => { if (open > 0) { PzSay("Tape every crack first!", "ติดเทปทุกรอยก่อน!", GameUI.CAmber); return; } pumping = true; pump.CapturePointer(ev.pointerId); });
            pump.RegisterCallback<PointerUpEvent>(ev => { pumping = false; pump.ReleasePointer(ev.pointerId); });
            var cracks = new List<VisualElement>();
            void Tape(VisualElement me)
            {
                if (me.userData != null) return;
                me.userData = true; me.Clear();
                me.Add(Box(0, 20, 130, 44, new Color(0.85f, 0.8f, 0.45f), 4));
                open--; _sfx.Play("click");
                if (open > 0) PzSay(open + " cracks left", "เหลืออีก " + open + " รอย");
                else PzSay("All taped. Now hold PUMP until the needle sits in the green.", "ติดครบแล้ว กดค้างเติมอากาศให้เข็มอยู่ช่องเขียว");
            }
            for (int i = 0; i < 3; i++)
            {
                float x = 60 + i * 230 + rng.Next(80), y = 60 + rng.Next(400);
                var c = Abs(x, y, 120, 90); c.pickingMode = PickingMode.Position;
                for (int k = 0; k < 5; k++) c.Add(Box(10 + k * 20, 30 + (k % 2) * 16, 26, 6, new Color(0.15f, 0.15f, 0.18f), 2));
                var puff = Box(60, 10, 30, 30, new Color(1, 1, 1, 0.5f), 15); puff.pickingMode = PickingMode.Ignore; c.Add(puff);
                float t0 = (float)rng.NextDouble();
                puff.schedule.Execute(() => { float t = Mathf.Repeat(Time.realtimeSinceStartup + t0, 1f); puff.style.top = 10 - t * 40; puff.style.opacity = 1 - t; }).Every(30);
                var me = c;
                c.RegisterCallback<ClickEvent>(_ => Tape(me));
                cracks.Add(c); wall.Add(c);
            }
            if (help > 0) Tape(cracks[0]);
            float last = Time.realtimeSinceStartup;
            b.schedule.Execute(() =>
            {
                float now = Time.realtimeSinceStartup, dt = now - last; last = now;
                pressure += (pumping ? 0.22f : 0f) * dt - (open > 0 ? 0.015f * open : 0.025f) * dt;
                pressure = Mathf.Clamp(pressure, 0.2f, 1f);
                if (pressure > 0.92f) { pressure = 0.5f; pumping = false; PzMistake("Pop! Too much pressure, a valve vented air.", "ปุ้ง! ความดันมากไป วาล์วปล่อยอากาศทิ้ง"); }
                needle.style.top = 400 - pressure * 360 - 24;
                bool inGreen = pressure >= 0.6f && pressure <= 0.75f;
                needle.style.backgroundColor = inGreen ? GameUI.CGreen : GameUI.CAmber;
                if (open == 0 && inGreen && !pumping) { calm += dt; if (calm > 1f) PzComplete(); } else calm = 0;
            }).Every(30).Until(() => _pzFinished || _pzRoot == null);
            _pzSolve = () => { foreach (var c in cracks) if (c.userData == null) Tape(c); pressure = 0.68f; pumping = false; calm = 2; PzComplete(); };
        }

        // ------------------------------------------------------------------ crank

        void PzCrank(VisualElement b)
        {
            float charge = 0, ang = 0;
            var wheel = Box(400, 120, 300, 300, new Color(0.45f, 0.47f, 0.52f), 150); b.Add(wheel);
            var arm = Box(140, 20, 20, 130, new Color(0.25f, 0.26f, 0.3f), 6); wheel.Add(arm);
            var knob = Box(130, 0, 40, 40, GameUI.CAmber, 20); wheel.Add(knob);
            var track = Box(200, 470, 700, 40, new Color(0.1f, 0.12f, 0.15f), 20); b.Add(track);
            var fill = Box(0, 0, 1, 40, GameUI.CAmber, 20); track.Add(fill);
            void Turn() { charge += 0.06f; ang += 40; wheel.style.rotate = new Rotate(new Angle(ang)); _sfx.Play("click", 0.5f); if (charge >= 1f) PzComplete(); }
            wheel.pickingMode = PickingMode.Position;
            wheel.RegisterCallback<ClickEvent>(_ => Turn());
            float last = Time.realtimeSinceStartup;
            b.schedule.Execute(() =>
            {
                float now = Time.realtimeSinceStartup, dt = now - last; last = now;
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb.spaceKey.wasPressedThisFrame) Turn();
                charge = Mathf.Max(0, charge - 0.08f * dt);
                fill.style.width = Length.Percent(Mathf.Clamp01(charge) * 100);
            }).Every(16).Until(() => _pzFinished || _pzRoot == null);
            _pzSolve = () => { charge = 1; PzComplete(); };
        }
    }
}
