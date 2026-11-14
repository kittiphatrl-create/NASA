// PROTOTYPE - NOT FOR PRODUCTION
// Question: Is the survival week more fun when the cadet walks around a top-down base and does each job?
// Date: 2026-11-12
using System.Collections;
using System.Collections.Generic;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// Walkable survival week. The same LifeboatSim rules and day cards, but each card waits at a place in a
    /// top-down Mars base (a knock at the airlock, a storm alarm at the power panel, the beacon outside). The cadet
    /// walks there and opens it. Between events there are hands-on jobs that trade one stock for another.
    /// </summary>
    public partial class GameFlow
    {
        /// <summary>Verification: play the week as the old read-and-choose diary.</summary>
        public static bool ForceDiary;
        bool WalkBase => LC.walkBase && !ForceDiary;

        class BSpot
        {
            public string id, nameKey;
            public Vector2 pos, stand;
            public SpriteRenderer sr;
            public RectInt block;
            public bool solid, on = true;
        }

        const int MW = 31, MH = 22;
        /// <summary>Map size and wall rows. Base: rooms 6 tall + 7 rows outside. Ship: rooms 5 tall, space around.</summary>
        int _bW = MW, _bH = MH, _bTop = 14, _bMid = 7;
        /// <summary>0 = Mars base week, 1 = ship (morning quests, then the 60-second scramble).</summary>
        int _bMode;
        const float BSpeed = 4.6f, BReach = 0.95f, BCamSize = 6.4f;

        Transform _bRoot;
        Camera _bCam;
        bool _bOpen;
        bool[,] _bWall, _bSolid;
        readonly List<BSpot> _bSpots = new List<BSpot>();
        SpriteRenderer _bPlayer, _bPet, _bMarker, _bArrow, _bDark, _bDust, _bDoor, _bKnocker, _bHeart;
        float _bPetWalkT; int _bPetDir;
        readonly List<SpriteRenderer> _bGuests = new List<SpriteRenderer>();
        readonly List<(SpriteRenderer sr, float strength)> _bLamps = new List<(SpriteRenderer, float)>();
        Vector2 _bPos, _bPetPos;
        List<Vector2> _bPath;
        BSpot _bGoal;
        bool _bRight = true, _bMoving;
        float _bAnim, _bHeartT;
        readonly List<Vector2> _bTrail = new List<Vector2>();

        // the event that is waiting, and its clock
        string _bEventKey;
        float _bTimer = -1;
        bool _bTimerAlarm;

        // hands-on jobs
        readonly HashSet<string> _bDone = new HashSet<string>();
        int _bDay = -1, _bSeeds;
        readonly int[] _bPlot = new int[3], _bPlotReady = new int[3];
        bool _bPanelsDusty;
        (string date, string cls, string region) _bFlare;

        VisualElement _bHud;
        VisualElement _bHudGauges, _bHudPortrait;
        Label _bHudRoom, _bHudDay, _bHudStock, _bHudNext, _bHudTimer;
        VisualElement _bHudBar;
        float _bHudT;

        /// <summary>Real solar flares from NASA's DONKI space weather database (archived values, not live).</summary>
        static readonly (string date, string cls, string region)[] RealFlares =
        {
            ("2024-10-03", "X9.0", "AR 3842"),
            ("2024-05-14", "X8.7", "AR 3664"),
            ("2017-09-10", "X8.2", "AR 2673"),
            ("2024-10-01", "X7.1", "AR 3842"),
            ("2024-02-22", "X6.3", "AR 3590"),
            ("2024-05-10", "X5.8", "AR 3664"),
        };

        // ------------------------------------------------------------------ start / stop

        void StartBase()
        {
            EmReset();
            JobReset(); _dayNotes.Clear();
            _bOpen = false; _bEventKey = null; _bTimer = -1; _bDay = -1; _bKnockPending = false; _bSuitT = -1; _bDoorOpen = false;
            _bTimerCard = null; _bAnnounced.Clear(); _bTaskMarks.Clear(); _bTaskSpots.Clear();
            _bFriend.Clear();
            _bDone.Clear(); _bMax.Clear();
            for (int i = 0; i < 3; i++) { _bPlot[i] = 0; _bPlotReady[i] = 0; }
            _bPanelsDusty = false;
            _bFlare = RealFlares[Random.Range(0, RealFlares.Length)];
            BBegin(0, new Vector2(5.5f, 9.6f));
            _ui.FadeTo(1f, 0f);
            _ui.FadeTo(0f, 0.8f);
        }

        /// <summary>Build a top-down map with its own orthographic camera. mode 0 = Mars base, 1 = ship.</summary>
        void BBegin(int mode, Vector2 start)
        {
            CrewReset();
            ClearWorld();
            _player.gameObject.SetActive(false);
            _orbi.gameObject.SetActive(false);
            _ui.SetHudVisible(false);
            _ui.SetBaseButton(false);
            _ui.SetIncident(null, 0);
            _ui.CloseModal();
            _ui.HideScreen();
            _sfx.Loop("hum", true);

            _bMode = mode; ResetActions(); _sprint.Reset(); _bGaitPhase = 0;

            _bRoot = new GameObject(mode == 0 ? "MarsBase" : "ShipTopDown").transform;
            var camGo = new GameObject("BaseCam");
            camGo.transform.SetParent(_bRoot, false);
            _bCam = camGo.AddComponent<Camera>();
            _bCam.orthographic = true;
            _bCam.orthographicSize = BCamSize;
            _bCam.clearFlags = CameraClearFlags.SolidColor;
            _bCam.backgroundColor = mode == 0 ? new Color(0.05f, 0.04f, 0.06f) : new Color(0.01f, 0.01f, 0.03f);
            _bCam.nearClipPlane = 0.1f; _bCam.farClipPlane = 50f;
            _bCam.depth = 5;
            _rig.cam.enabled = false;

            _bTrail.Clear(); _bPath = null; _bGoal = null;
            _bPos = start;
            _bPetPos = _bPos + new Vector2(-1, 0);
            BBuildMap();
            BBuildHud();
            BSnapCamera();
        }

        /// <summary>Play one of the side-view cutscenes in the middle of a top-down chapter.</summary>
        IEnumerator BCutscene(string id)
        {
            if (_bCam != null) _bCam.enabled = false;
            if (_bHud != null) _bHud.style.display = DisplayStyle.None;
            if (_bHudPortrait != null) _bHudPortrait.style.display = DisplayStyle.None;
            if (_petPrompt != null) _petPrompt.style.display = DisplayStyle.None;
            if (_crewPrompt != null) _crewPrompt.style.display = DisplayStyle.None;
            _rig.cam.enabled = true;
            yield return PlayCutscene(id);
            _player.gameObject.SetActive(false);
            _orbi.gameObject.SetActive(false);
            _ui.SetHudVisible(false);
            if (_bCam != null) { _bCam.enabled = true; _rig.cam.enabled = false; }
        }

        /// <summary>Called by ClearWorld: drop the base and give the camera back to the side-view rig.</summary>
        void EndBase()
        {
            ResetActions();
            CrewReset();
            if (_bGlowMaterial != null) { Destroy(_bGlowMaterial); _bGlowMaterial = null; }
            if (_bRoot != null) Destroy(_bRoot.gameObject);
            _bRoot = null; _bCam = null;
            _bSpots.Clear(); _bGuests.Clear(); _bLamps.Clear(); _bShadows.Clear(); _sFx.Clear(); _bFaceWalls.Clear();
            if (_bHud != null) { _bHud.RemoveFromHierarchy(); _bHud = null; }
            if (_bHudPortrait != null) { _bHudPortrait.RemoveFromHierarchy(); _bHudPortrait = null; }
            PaceEndBase();
            if (_rig != null && _rig.cam != null) _rig.cam.enabled = true;
        }

        // ------------------------------------------------------------------ map

        static int SortY(float y) => Mathf.RoundToInt(-y * 20f);

        SpriteRenderer BSprite(string name, Sprite s, Vector2 p, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_bRoot, false);
            go.transform.position = new Vector3(p.x, p.y, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            return sr;
        }

        void BBuildMap()
        {
            BLayout();
            _bWall = new bool[_bW, _bH];
            _bSolid = new bool[_bW, _bH];
            for (int x = 0; x < _bW; x++)
                for (int y = 0; y < _bH; y++)
                    _bWall[x, y] = LMakesWall(x, y);

            var rng = new System.Random(_bMode * 31 + 5);
            for (int x = 0; x < _bW; x++)
                for (int y = 0; y < _bH; y++)
                {
                    int k = _bKind[x, y];
                    _bSolid[x, y] = _bWall[x, y] || k < 0;
                    var p = new Vector2(x, y);
                    if (_bWall[x, y])
                    {
                        bool face = y > 0 && _bKind[x, y - 1] >= 0;
                        bool hull = LIsHull(x, y);
                        var ws = BSprite("wall", Art.BaseWall(face, hull), p, SortY(y));
                        if (face) _bFaceWalls.Add((ws, x, y));
                        // little details on wall faces: vents, pipes, warning stripes
                        if (face && rng.NextDouble() < 0.22) _bFaceWalls.Add((BSprite("walldeco", Art.WallDetail(rng.Next(4), hull), p, SortY(y) + 1), x, y));
                    }
                    else if (k >= 0)
                    {
                        BSprite("floor", Art.BaseFloor(k, (x * 7 + y * 13) % 9), p, -30000);
                        // a doorway cut through a horizontal wall gets a lintel at wall height
                        // (no lintel sprite: the stacked bars read as clutter)
                    }
                }

            if (_bMode == 0)
            {
                // Mars ground everywhere around the base
                var g = BSprite("ground", Art.BaseFloor(2, 0), new Vector2(-14, -12), -30001);
                g.drawMode = SpriteDrawMode.Tiled; g.size = new Vector2(_bW + 28, _bH + 24);
                for (int i = 0; i < 60; i++)
                {
                    var rp = new Vector2((float)rng.NextDouble() * (_bW + 16) - 8, (float)rng.NextDouble() * (_bH + 12) - 6);
                    int cx = Mathf.FloorToInt(rp.x), cy = Mathf.FloorToInt(rp.y);
                    bool inMap = cx >= 0 && cy >= 0 && cx < _bW && cy < _bH;
                    if (inMap && (_bWall[cx, cy] || _bKind[cx, cy] >= 0 && _bKind[cx, cy] != KOutside)) continue;
                    if (inMap && _bKind[cx, cy] == KOutside && rng.NextDouble() < 0.7) continue;
                    BSprite("rock", Art.Rock(rng.Next(3)), rp, SortY(rp.y));
                }
                BBuildProps();
            }
            else
            {
                var sp = BSprite("space", Art.SpaceTile(), new Vector2(-16, -12), -30002);
                sp.drawMode = SpriteDrawMode.Tiled; sp.size = new Vector2(_bW + 32, _bH + 24);
                BBuildShipProps();
            }

            _bShadows.Clear();
            _bPlayer = BSprite("cadet", Art.Astronaut(false, Pose.Idle0), _bPos, 0);
            _bPet = BSprite("pet", Art.Pet("comet"), _bPos, 0);
            _bPet.gameObject.SetActive(false);
            _bMarker = BSprite("marker", Art.Marker(false), Vector2.zero, 31000);
            _bArrow = BSprite("arrow", Art.PointerArrow(), Vector2.zero, 31001);
            _bHeart = BSprite("heart", Art.Heart(), Vector2.zero, 31002);
            _bHeart.gameObject.SetActive(false);
            _bKnocker = BSprite("knocker", Art.Visitor("tik"), _bHatch + new Vector2(1f, 1.55f), SortY(_bHatch.y + 1.55f));
            _bKnocker.gameObject.SetActive(false);
            foreach (var who in new[] { _bPlayer, _bPet, _bKnocker }) BShadowFor(who, 1f);

            // light: a dark veil over everything, lamps on top of it, a dust haze, and a vignette on the camera
            _bDark = BSprite("dark", WorldBuilder.WhitePixel(), new Vector2(_bW / 2f, _bH / 2f), 30000);
            _bDark.transform.localScale = new Vector3(100 * 16, 80 * 16, 1);
            _bDark.color = new Color(0.02f, 0.03f, 0.1f, 0f);
            _bDust = BSprite("dust", WorldBuilder.WhitePixel(), new Vector2(_bW / 2f, _bH / 2f), 29990);
            _bDust.transform.localScale = new Vector3(100 * 16, 80 * 16, 1);
            _bDust.color = new Color(0.75f, 0.45f, 0.25f, 0f);
            var vig = new GameObject("vignette").AddComponent<SpriteRenderer>();
            vig.transform.SetParent(_bCam.transform, false);
            vig.transform.localPosition = new Vector3(0, 0, 5);
            vig.sprite = Art.Vignette();
            vig.sortingOrder = 30500;
            float vh = BCamSize * 2.05f;
            vig.transform.localScale = new Vector3(vh * 16f / 9f * 1.05f / vig.sprite.bounds.size.x, vh / vig.sprite.bounds.size.y, 1);
            _bVignette = vig;
        }

        /// <summary>Wall faces and lintels; they fade when the cadet walks behind them.</summary>
        readonly List<(SpriteRenderer sr, int x, int y)> _bFaceWalls = new List<(SpriteRenderer, int, int)>();

        void BFadeWalls(float dt)
        {
            foreach (var (sr, x, y) in _bFaceWalls)
            {
                if (sr == null) continue;
                bool behind = _bPos.y > y + 0.3f && _bPos.y < y + 2.3f && Mathf.Abs(_bPos.x - (x + 0.5f)) < 1.8f;
                var c = sr.color;
                float target = behind ? 0.42f : 1f;
                if (Mathf.Abs(c.a - target) > 0.01f) { c.a = Mathf.MoveTowards(c.a, target, dt * 4f); sr.color = c; }
            }
        }

        readonly List<(SpriteRenderer shadow, SpriteRenderer owner)> _bShadows = new List<(SpriteRenderer, SpriteRenderer)>();
        SpriteRenderer _bVignette;

        void BShadowFor(SpriteRenderer owner, float width)
        {
            var sh = BSprite("shadow", Art.Shadow(), owner.transform.position, -29000);
            sh.transform.localScale = new Vector3(width, 1, 1);
            _bShadows.Add((sh, owner));
        }

        void BUpdateShadows()
        {
            foreach (var (sh, owner) in _bShadows)
            {
                bool on = owner != null && owner.gameObject.activeInHierarchy && owner.sprite != null;
                sh.gameObject.SetActive(on);
                if (!on) continue;
                sh.transform.position = owner.transform.position + new Vector3(0, 0.02f, 0);
                sh.transform.localScale = new Vector3(Mathf.Max(0.8f, owner.sprite.bounds.size.x * 0.9f), 1, 1);
            }
        }

        Material _bGlowMaterial;
        void BLamp(Vector2 p, Color c, float size, float strength)
        {
            var l = BSprite("lamp", Art.SoftLight(), p, 30001);
            if (_bGlowMaterial == null) { var shader = Resources.Load<Shader>("UI/HabitatGlow"); if (shader != null) _bGlowMaterial = new Material(shader); }
            if (_bGlowMaterial != null) l.sharedMaterial = _bGlowMaterial;
            l.color = new Color(c.r, c.g, c.b, 0);
            l.transform.localScale = Vector3.one * size;
            _bLamps.Add((l, strength));
        }

        /// <summary>A prop. Wall props stand on the face of a wall (y = the wall row); floor props block their footprint.</summary>
        BSpot BProp(string id, string nameKey, Sprite s, float x, float y, Vector2 stand, int blockW = 0, int blockH = 0, int order = int.MinValue)
        {
            var sp = new BSpot { id = id, nameKey = nameKey, pos = new Vector2(x, y), stand = stand };
            sp.sr = BSprite(id, s, sp.pos, order == int.MinValue ? SortY(y) : order);
            if (blockW > 0)
            {
                sp.solid = true;
                if (s != null) BShadowFor(sp.sr, blockW);
                sp.block = new RectInt(Mathf.FloorToInt(x - blockW / 2f + 0.01f), Mathf.FloorToInt(y), blockW, Mathf.Max(1, blockH));
                for (int bx = sp.block.xMin; bx < sp.block.xMax; bx++)
                    for (int by = sp.block.yMin; by < sp.block.yMax; by++)
                        if (bx >= 0 && by >= 0 && bx < _bW && by < _bH) _bSolid[bx, by] = true;
            }
            _bSpots.Add(sp);
            return sp;
        }

        void BDeco(Sprite s, float x, float y, int blockW = 0) => BProp("deco", null, s, x, y, Vector2.zero, blockW);

        BSpot BSpotById(string id) => _bSpots.Find(s => s.id == id);

        // ------------------------------------------------------------------ the week's events

        /// <summary>Where in the base a day card waits.</summary>
        static string BCardSpot(LifeCard c)
        {
            if (c == null) return "seats";
            if (!string.IsNullOrEmpty(c.visitor)) return "hatch";
            switch (c.id)
            {
                case "filter": return "air";
                case "crate": case "far_crate": return "crate";
                case "locker": return "locker";
                case "recycler": return "recycler";
                case "storm_warning": case "storm_last": return "shelter";
                case "storm_power": return "panel";
                case "beacon": case "beacon_last": return "beacon";
                case "coords": case "coords_last": return "radio";
                case "log": return "seats";
                default: return "board";
            }
        }

        static bool BUrgent(LifeCard c) => c != null && (c.id == "storm_last" || c.id == "storm_power");

        /// <summary>A visitor knocks from this hour even while jobs are still open (they wait at the door).</summary>
        const float VisitorHour = 11f;

        LifeCard _bTimerCard;                                         // the card a running knock/alarm timer belongs to
        readonly HashSet<string> _bAnnounced = new HashSet<string>();  // cards already announced today (toast, knock)
        readonly List<SpriteRenderer> _bTaskMarks = new List<SpriteRenderer>();
        readonly HashSet<string> _bTaskSpots = new HashSet<string>();  // spots with a "!" this frame

        /// <summary>
        /// The card the base treats as "current" (toasts, knock, the bot). The player is free to walk to any other
        /// open job and do it first. A visitor at the door or a running alarm stays current; a storm alarm comes
        /// first; the visitor knocks once the morning is over or nothing else is left.
        /// </summary>
        LifeCard LbFocus()
        {
            var open = _life.OpenCards();
            if (open.Count == 0) return null;
            if (_bTimerCard != null && open.Contains(_bTimerCard)) return _bTimerCard;
            var urgent = open.Find(BUrgent);
            if (urgent != null) return urgent;
            var visitor = open.Find(c => !string.IsNullOrEmpty(c.visitor));
            var job = open.Find(c => string.IsNullOrEmpty(c.visitor));
            if (visitor != null && (job == null || _life.State.schedule.Hour >= VisitorHour)) return visitor;
            return job ?? visitor;
        }

        /// <summary>A knock or an alarm interrupts whatever job is current (the job itself stays open).</summary>
        void BRefocus()
        {
            if (_life == null || _lbView != LbView.Card || _bOpen || _ui.Blocking || _busy || _bKnockPending || ActionLocked) return;
            var f = LbFocus();
            if (f == null || f == _lbCard) return;
            if (string.IsNullOrEmpty(f.visitor) && !BUrgent(f)) return;
            _lbCard = f;
            BOnEvent();
        }

        /// <summary>The visitor waiting at the door right now, if any.</summary>
        LifeCard BKnockCard() =>
            _bTimerCard != null && !string.IsNullOrEmpty(_bTimerCard.visitor) && (_life == null || !_life.State.answered.Contains(_bTimerCard.id)) ? _bTimerCard
            : _lbCard != null && !string.IsNullOrEmpty(_lbCard.visitor) ? _lbCard : null;

        bool BHasTask(string id) => id == BActiveSpot || _bTaskSpots.Contains(id);

        Vector3 BMarkTop(BSpot spot, bool knocker) =>
            spot.id == "hatch" ? (knocker ? new Vector3(_bHatch.x + 1, _bKnocker.bounds.max.y + 0.15f, 0) : new Vector3(_bHatch.x + 1, _bHatch.y + 1.9f, 0))
            : spot.sr != null && spot.sr.sprite != null ? new Vector3(spot.sr.bounds.center.x, spot.sr.bounds.max.y + 0.1f, 0) : (Vector3)(spot.pos + Vector2.up);

        string BActiveSpot => _bMode == 1 ? (_bRoot == null ? null : _sTarget) : _bRoot == null || _bOpen || _bKnockPending || _life == null || _life.State.outcome != LifeOutcome.Ongoing ? null
            : _lbView == LbView.Card ? BCardSpot(_lbCard) : _lbView == LbView.Ration ? "seats" : null;

        /// <summary>LbShow calls this instead of opening the diary when the next card should wait in the base.</summary>
        bool BInterceptShow()
        {
            if (_bRoot == null || _bOpen || _life == null) return false;
            if (_lbView != LbView.Card && _lbView != LbView.Ration) return false;
            _ui.HideScreen();
            BOnEvent();
            return true;
        }

        void BOnEvent()
        {
            // a new day (or a new card) gets everyone up from the pod seats, even on a day with no events at all
            if (_lbView == LbView.Card || _life.State.day != _bDay) BEndGather();
            bool newDay = _life.State.day != _bDay;
            if (newDay) BNewDay();
            string key = _lbView == LbView.Ration ? "ration" + _life.State.day : _lbCard.id;
            if (!_bAnnounced.Add(key)) return;
            _bEventKey = key;
            if (newDay && _lbView == LbView.Card)
            {
                int jobs = _life.OpenCards().FindAll(c => string.IsNullOrEmpty(c.visitor)).Count;
                if (jobs > 1) _ui.Toast(TT(jobs + " jobs today, marked with ! . Do them in any order you like.", "วันนี้มีงาน " + jobs + " อย่าง (ดูเครื่องหมาย !) ทำอันไหนก่อนก็ได้"), GameUI.CCyan);
            }
            _bTimer = -1; _bTimerAlarm = false;
            string place = _loc.T("base.place." + BActiveSpot);
            if (_lbView == LbView.Ration)
            {
                _ui.Toast(_loc.T("base.toast.evening"), GameUI.CCyan);
                return;
            }
            if (!string.IsNullOrEmpty(_lbCard.visitor))
            {
                if (BIsOutside(_bPos)) { _bKnockPending = true; return; }   // nobody knocks while you are outside
                if (_bDoorOpen && !BInDoorway()) BSetDoor(false);
                _bTimer = LC.visitorWaitSeconds; _bTimerCard = _lbCard;
                StartCoroutine(BKnock());
                _sShake = 0.15f;
                _ui.Toast(_loc.T("base.toast.knock"), GameUI.CAmber);
            }
            else if (BUrgent(_lbCard))
            {
                _bTimer = LC.alarmSeconds; _bTimerAlarm = true; _bTimerCard = _lbCard;
                _sfx.Play("alarm");
                _sShake = 0.6f;
                _ui.Toast(_loc.T("base.toast.alarm", _loc.T($"life.card.{_lbCard.id}.title"), Mathf.RoundToInt(LC.alarmSeconds)), GameUI.CRed);
            }
            else
            {
                _sfx.Play("blip");
                _ui.Toast(_loc.T("base.toast.new", _loc.T($"life.card.{_lbCard.id}.title"), place), GameUI.CCyan);
            }
            if (_lbCard.id == "storm_warning") _ui.Toast(_loc.T("base.toast.flare", _bFlare.date, _bFlare.cls), GameUI.CAmber);
        }

        // ------------------------------------------------------------------ airlock door and suit air

        const float SuitSeconds = 75f;
        bool _bDoorOpen, _bKnockPending, _bSuitWarned;
        float _bDoorT, _bSuitT = -1;

        bool BInDoorway() => _bPos.y > _bHatch.y - 0.7f && _bPos.y < _bHatch.y + 1.7f && _bPos.x > _bHatch.x - 0.5f && _bPos.x < _bHatch.x + 2.5f;

        /// <summary>The outer airlock door blocks the way unless it is opened; it closes itself after you pass.</summary>
        void BSetDoor(bool open)
        {
            if (_bSolid == null) return;
            _bDoorOpen = open;
            for (int i = 0; i < 2; i++)
            {
                int x = (int)_bHatch.x + i, y = (int)_bHatch.y;
                if (x >= 0 && x < _bW && y >= 0 && y < _bH) _bSolid[x, y] = !open;
            }
            if (open) _bDoorT = 3.5f;
        }

        void BUseDoor()
        {
            if (_lbView == LbView.Card && BKnockCard() != null && !_bKnockPending)
            {
                _sfx.Play("bad");
                _ui.Toast(_loc.T("base.door.locked"), GameUI.CRed);
                return;
            }
            if (_bDoorOpen) { if (!BInDoorway()) { BSetDoor(false); _sfx.Play("close"); } return; }
            BSetDoor(true);
            _sfx.Play("open");
            _ui.Toast(_loc.T(BIsOutside(_bPos) ? "base.door.in" : "base.door.out"), GameUI.CCyan);
        }

        void BDoorAndSuit(float dt)
        {
            if (_bDoorOpen && !BInDoorway()) { _bDoorT -= dt; if (_bDoorT <= 0) { BSetDoor(false); _sfx.Play("close"); } }
            bool outside = BIsOutside(_bPos);
            if (outside)
            {
                if (_bSuitT < 0) { _bSuitT = SuitSeconds; _bSuitWarned = false; }
                _bSuitT -= dt;
                if (!_bSuitWarned && _bSuitT < 20f) { _bSuitWarned = true; _sfx.Play("alarm"); _ui.Toast(_loc.T("base.suit.low"), GameUI.CRed); }
                if (_bSuitT <= 0)
                {
                    // out of suit air: ORBI talks you back through the airlock, and the refill costs air from the base
                    _life.ApplyTask(new Stock { o2 = -Mathf.Min(1f, _life.State.stock.o2) });
                    _bPath = null; _bGoal = null;
                    _bPos = new Vector2(_bHatch.x + 1, _bHatch.y - 1.3f);
                    _bPetPos = _bPos + Vector2.left; _bTrail.Clear();
                    BSetDoor(false);
                    _sShake = 0.3f;
                    _ui.Toast(_loc.T("base.suit.out"), GameUI.CRed);
                    _bSuitT = -1;
                }
            }
            else
            {
                _bSuitT = -1;
                if (_bKnockPending)
                {
                    _bKnockPending = false;
                    _bEventKey = null;
                    if (_lbCard != null) _bAnnounced.Remove(_lbCard.id);
                    BOnEvent();
                }
            }
        }

        IEnumerator BKnock()
        {
            for (int i = 0; i < 3; i++) { _sfx.Play("thud"); yield return new WaitForSeconds(0.28f); }
        }

        void BNewDay()
        {
            bool first = _bDay < 0; _sprint.Reset();
            BClockNewDay();
            if (!first) BRespawnSalvage();
            _bDay = _life.State.day;
            _bDone.Clear();
            if (first && !_bSeedsRestored) _bSeeds = _life.State.items.Contains("seed_pack") ? LC.seedsWithPack : LC.seedsBase;
            else
            {
                // overnight: plants grow if the grow lamps had power
                bool lamps = _life.State.stock.power >= 1;
                bool any = false;
                for (int i = 0; i < 3; i++)
                    if (_bPlot[i] > 0 && _bPlot[i] < 3) { any = true; if (lamps) _bPlot[i] = Mathf.Min(3, _bPlot[i] + (_life.State.guests.Contains("kira") || _life.State.guests.Contains("aya") || _life.State.guests.Contains("bizz") ? 2 : 1) + (_life.State.flags.Contains("grow_lamps") ? 1 : 0)); }
                if (any && !lamps) _ui.Toast(_loc.T("base.plot.nolamp"), GameUI.CAmber);
            }
            _bPanelsDusty = _bDay >= 2;
            BRefreshProps();
        }

        /// <summary>Open the waiting card (the cadet reached its place).</summary>
        void BOpenEvent()
        {
            if (_bMode == 0 && _lbView == LbView.Ration && !_bGather) { StartCoroutine(BGather()); return; }
            if (_bMode == 0 && _lbView == LbView.Card && CardJob != null) { JobShow(CardJob); return; }   // a repair job: fetch parts, solve the panel
            _bOpen = true;
            if (_bTimerCard == null || _bTimerCard == _lbCard) _bTimer = -1;
            _bPath = null; _bGoal = null;
            _sfx.Play("open");
            LbShow();
        }

        bool _bGather;

        /// <summary>
        /// Evening: everyone gathers in the lifeboat pod and sits round the console (you, the pet, guests), the
        /// camera moves in, then the status check opens: who eats and drinks tonight.
        /// </summary>
        IEnumerator BGather()
        {
            _life.State.schedule.TrySpend(Mathf.Max(0, 20 - _life.State.schedule.Hour));
            _bOpen = true;
            _bTimer = -1;
            _bPath = null; _bGoal = null;
            yield return Fade(1f, 0.35f);
            _bGather = true;
            _bTrail.Clear();
            _sfx.Play("open");
            BSnapCamera();
            _bCam.orthographicSize = 4.6f;
            BSnapCamera();
            yield return Fade(0f, 0.45f);
            _ui.Toast(_loc.T("base.toast.gather"), GameUI.CCyan);
            yield return new WaitForSeconds(2.4f);
            LbShow();
        }

        void BEndGather()
        {
            if (!_bGather) return;
            _bGather = false;
            _bPos = new Vector2(5.5f, 9.6f);
            _bPetPos = new Vector2(4.4f, 9.6f);
            _bTrail.Clear();
            for (int i = 0; i < _bGuests.Count; i++) _bGuests[i].transform.position = new Vector3(31.4f + i * 1.3f, 11.2f + (i % 2) * 0.9f, 0);
            _bCam.orthographicSize = BCamSize;
            BSnapCamera();
        }

        /// <summary>Time ran out: the visitor walks away, or the storm decides. The last option is taken.</summary>
        void BTimeout()
        {
            _bTimer = -1;
            if (_lbView != LbView.Card) return;
            if (_bTimerCard != null && _life.OpenCards().Contains(_bTimerCard)) _lbCard = _bTimerCard;
            if (_lbCard == null) return;
            LifeOption pick = null;
            for (int i = _lbCard.options.Count - 1; i >= 0 && pick == null; i--)
                if (_life.CanChoose(_lbCard.options[i], out _)) pick = _lbCard.options[i];
            if (pick == null) return;
            _ui.Toast(_loc.T("base.toast.late", _loc.T($"life.card.{_lbCard.id}.title")), GameUI.CRed);
            _bOpen = true;
            LbChoose(pick);
        }

        // ------------------------------------------------------------------ hands-on jobs

        void BUse(BSpot s)
        {
            if (ActionLocked || _busy || _bMode == 1 && _sSitting) return;
            if (s.id == "pet") { PetMenu(); return; }
            if (_bMode == 0 && (s.id == "solar" || s.id == "drill" || s.id.StartsWith("plot")))
            { StartCoroutine(Act(TaskPose(s.id), .9f, () => BUseResolved(s))); return; }
            BUseResolved(s);
        }

        void BUseResolved(BSpot s)
        {
            if (_bMode == 1) { SUse(s); return; }
            if (_bMode == 0 && _lbView == LbView.Card && !_bOpen && _life != null && s.id != BActiveSpot)
            {
                // free order: walk up to any open job and do it now; the visitor's door only once they knocked
                var other = _life.OpenCards().Find(c => BCardSpot(c) == s.id && (string.IsNullOrEmpty(c.visitor) || c == BKnockCard()));
                if (other != null) { _lbCard = other; _bAnnounced.Add(other.id); _bEventKey = other.id; BOpenEvent(); return; }
            }
            if (s.id == BActiveSpot) { BOpenEvent(); return; }
            if (_bMode == 0 && EmTryFix(s)) return;
            if (_bMode == 0 && s.id == "toolbox") { JobShowToolbox(); return; }
            if (_bMode == 0 && BUseOutside(s)) return;
            if (_bMode == 0 && (s.id == "hatch" || s.id == "hatch_out")) { BUseDoor(); return; }
            var st = _life?.State;
            if (st == null) return;
            string Once(string key) => _bDone.Contains(key) ? _loc.T("base.task.done_today") : null;
            switch (s.id)
            {
                case "solar":
                {
                    if (!_bPanelsDusty) { _ui.Toast(_loc.T("base.solar.clean"), GameUI.CCyan); return; }
                    if (!BTimedTask(LC.taskSolar, 3)) return;
                    _bPanelsDusty = false; BGood("base.solar.ok"); break;
                }
                case "drill":
                {
                    var done = Once("drill"); if (done != null) { _ui.Toast(done, GameUI.CCyan); return; }
                    if (!BTimedTask(LC.taskDrill, 3)) return;
                    _bDone.Add("drill"); BGood("base.drill.ok"); break;
                }
                case "plot0": case "plot1": case "plot2":
                {
                    int i = s.id[4] - '0';
                    if (_bPlot[i] == 3)
                    {
                        if (!BTimedTask(LC.taskHarvest, 1)) return;
                        _bPlot[i] = 0;
                        BGood("base.plot.harvest", "");
                    }
                    else if (_bPlot[i] > 0) _ui.Toast(_loc.T("base.plot.growing", _bPlotReady[i]), GameUI.CCyan);
                    else if (_bSeeds <= 0) BBad("base.plot.noseed");
                    else if (!BTimedTask(LC.taskPlant, 2)) return;
                    else
                    {
                        _bSeeds--; _bPlot[i] = 1; _bPlotReady[i] = st.day + LC.growNights;
                        BGood("base.plot.plant", LC.growNights, _bSeeds);
                    }
                    break;
                }
                case "pet":
                    _bHeartT = 1.2f; _sfx.Play("good", 0.6f);
                    if (_bDone.Add("pet_comfort")) st.morale = Mathf.Min(100, st.morale + 5);
                    _ui.Toast(_loc.T("base.pet", LbName(LifeboatSim.Pet)), GameUI.CGreen);
                    break;
                case "board": ProjectsShow(); break;   // pick the base projects you want (GameFlow.Pace.cs)
                case "air": MoxieShow(); break;        // run the oxygen maker any time of day
                case "shelf_o2": _ui.Toast(_loc.T("base.shelf", _loc.T("life.res.o2"), LbNum(st.stock.o2)), GameUI.CCyan); break;
                case "shelf_water":
                    _ui.Toast(_loc.T("base.shelf", _loc.T("life.res.water"), LbNum(st.stock.water)), GameUI.CCyan);
                    if (!BagHas("water_bag") && BagTake("water_bag")) _ui.Toast(TT("You fill a water bag and carry it (Tab to see your bag).", "เติมน้ำใส่ถุงแล้วถือไว้ (กด Tab ดูกระเป๋า)"), GameUI.CCyan);
                    break;
                case "shelf_food": _ui.Toast(_loc.T("base.shelf", _loc.T("life.res.food"), LbNum(st.stock.food)), GameUI.CCyan); break;
                case "battery": _ui.Toast(_loc.T("base.battery", LbNum(st.stock.power)), GameUI.CCyan); break;
                default:
                    if (s.id.StartsWith("guest")) CrewTalk(s);
                    else _ui.Toast(_loc.T("base.nothing", s.nameKey != null ? _loc.T(s.nameKey) : ""), GameUI.CDim);
                    break;
            }
            BRefreshProps();
        }

        void BGood(string key, params object[] args) { _sfx.Play("good"); _ui.Toast(_loc.T(key, args), GameUI.CGreen); }
        void BBad(string key, params object[] args) { _sfx.Play("bad"); _ui.Toast(_loc.T(key, args), GameUI.CRed); }

        /// <summary>Props show the state: supplies on shelves, battery bars, plots, dusty panels, the shelter, guests.</summary>
        void BRefreshProps()
        {
            if (_bRoot == null || _life == null || _bMode != 0) return;
            var st = _life.State;
            int R(float v) => Mathf.Clamp(Mathf.FloorToInt(v + 0.001f), 0, 10);
            BSpotById("shelf_o2").sr.sprite = Art.SupplyShelf("o2", R(st.stock.o2));
            BSpotById("shelf_water").sr.sprite = Art.SupplyShelf("water", R(st.stock.water));
            BSpotById("shelf_food").sr.sprite = Art.SupplyShelf("food", R(st.stock.food));
            BSpotById("battery").sr.sprite = Art.BatteryBank(Mathf.Clamp(R(st.stock.power), 0, 6));
            for (int i = 0; i < 3; i++) BSpotById("plot" + i).sr.sprite = Art.Plot(_bPlot[i], false);
            BSpotById("solar").sr.sprite = Art.SolarArray(false, _bPanelsDusty);
            BSpotById("shelter").sr.sprite = Art.WaterWall(st.flags.Contains("shelter_ready"));
            BSpotById("beacon").sr.sprite = Art.Dish(st.flags.Contains(LC.beaconFlag));
            BSpotById("locker").sr.sprite = Art.Locker(st.flags.Contains("locker_locked") ? false : st.day >= 3);
            var crate = BSpotById("crate");
            bool crateOut = _lbView == LbView.Card && _life.OpenCards().Exists(c => c.id == "crate" || c.id == "far_crate");
            crate.sr.enabled = crateOut;

            // the pet and the guests who live here now
            if (st.pet != null)
            {
                _bPet.sprite = Art.Pet(st.pet);
                _bPet.gameObject.SetActive(true);
                if (BSpotById("pet") == null) _bSpots.Add(new BSpot { id = "pet", sr = _bPet });
                BSpotById("pet").nameKey = LbName(LifeboatSim.Pet);
            }
            for (int i = 0; i < Mathf.Max(st.guests.Count, _bGuests.Count); i++)
            {
                if (i >= _bGuests.Count)
                {
                    var g = BSprite("guest" + i, null, new Vector2(31.4f + i * 1.3f, 11.2f + (i % 2) * 0.9f), 0);
                    _bGuests.Add(g);
                    BShadowFor(g, 1f);
                    _bSpots.Add(new BSpot { id = "guest" + i, sr = g });
                }
                bool on = i < st.guests.Count;
                _bGuests[i].gameObject.SetActive(on);
                if (on)
                {
                    _bGuests[i].sprite = Art.Visitor(st.guests[i]);
                    _bGuests[i].sortingOrder = SortY(_bGuests[i].transform.position.y);
                    _bSpots.Find(s => s.id == "guest" + i).nameKey = LbName(st.guests[i]);
                }
            }
        }

        // ------------------------------------------------------------------ per frame

        void BUpdate()
        {
            if (_bRoot == null) return;
            float dt = Time.deltaTime;
            var kb = Keyboard.current;
            var mouse = Mouse.current;

            if (_bTimer > 0 && !_ui.Blocking)
            {
                _bTimer -= dt;
                if (_bTimer <= 0) BTimeout();
            }
            if (_bMode == 0 && !_ui.Blocking) BDoorAndSuit(dt);
            if (_bMode == 0) { EmUpdate(dt); BClock(dt); JobUpdate(dt); RadUpdate(dt); BRefocus(); PaceUpdate(kb); }

            if (!_ui.Blocking && kb != null && kb.escapeKey.wasPressedThisFrame) { _ui.SetHover(null, Vector2.zero); OpenPause(); return; }
            Vector2 move = Vector2.zero;
            if (!_ui.Blocking && kb != null && !_bGather && !ActionLocked && !_busy && !(_bMode == 1 && _sSitting))
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1;
                if (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame && !_ui.DialogOpen) BUseNearest();
                if (_bMode == 0 && kb.gKey.wasPressedThisFrame) BShowGuide();
            }

            // mouse: hover label, click to walk or to use
            BSpot hover = null;
            if (!_ui.Blocking && mouse != null && !_bGather && !ActionLocked && !_busy && !(_bMode == 1 && _sSitting))
            {
                Vector2 mp = mouse.position.ReadValue();
                if (!_ui.PointerOverUI(mp))
                {
                    Vector2 w = _bCam.ScreenToWorldPoint(new Vector3(mp.x, mp.y, 10));
                    hover = BSpotAt(w);
                    if (mouse.leftButton.wasPressedThisFrame)
                    {
                        if (hover != null) BGoTo(hover.stand, hover);
                        else BGoTo(w, null);
                    }
                }
            }
            if (hover != null)
            {
                string name = hover.nameKey == null ? "" : _loc.Has(hover.nameKey) ? _loc.T(hover.nameKey) : hover.nameKey;
                if (BHasTask(hover.id)) name = "! " + name;
                name += BTimeHint(hover.id);
                var top = hover.sr != null && hover.sr.sprite != null ? new Vector3(hover.sr.bounds.center.x, hover.sr.bounds.max.y + 0.2f, 0) : (Vector3)(hover.pos + Vector2.up);
                _ui.SetHover(name, _ui.WorldToPanel(top, _bCam));
            }
            else _ui.SetHover(null, Vector2.zero);

            // walking
            Vector2 beforeMove = _bPos;
            _bMoving = false;
            _bRun = _bMode == 1 && _sScramble || kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
            _bRun &= _sprint.CanSprint;
            float speed = BSpeed * (_bRun ? 1.5f : 1f);
            if (move != Vector2.zero) { _bPath = null; _bGoal = null; _bLastMove = move.normalized; BStep(move.normalized * speed * dt); _bMoving = true; }
            else if (!_ui.Blocking && !ActionLocked && !_busy && _bPath != null && _bPath.Count > 0)
            {
                var target = _bPath[0];
                var d = target - _bPos;
                float step = speed * dt;
                if (d.sqrMagnitude > 0.0001f) _bLastMove = d.normalized;
                if (d.magnitude <= step) { _bPos = target; _bPath.RemoveAt(0); }
                else { _bPos += d.normalized * step; if (Mathf.Abs(d.x) > 0.05f) _bRight = d.x > 0; }
                _bMoving = true;
                if (_bPath.Count == 0)
                {
                    _bPath = null;
                    if (_bGoal != null) { var g = _bGoal; _bGoal = null; BFace(g); BUse(g); }
                }
            }

            float travelled = Vector2.Distance(beforeMove, _bPos);
            _bMoving = travelled > .0001f;
            _sprint.Tick(dt, _bRun && _bMoving && !_ui.Blocking);
            if (_bMoving) _bGaitPhase += travelled / (_bRun ? 3.2f : 2.7f) * 8;
            if (_bMoving) { _bStepT -= dt; if (_bStepT <= 0) { _sfx.Play("step", 0.3f); _bStepT = 0.34f; } }
            BAnimate(dt, hover);
            ActionPresentation();
            CrewUpdate(dt);
            BUpdateShadows();
            BFadeWalls(dt);
            if (_bMode == 1) { SUpdate(dt); SLight(dt); } else BLight(dt);
            BCamera(dt);
            BHud(dt);
        }

        void BStep(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) > 0.001f) _bRight = delta.x > 0;
            var nx = new Vector2(_bPos.x + delta.x, _bPos.y);
            if (BFree(nx)) _bPos = nx;
            var ny = new Vector2(_bPos.x, _bPos.y + delta.y);
            if (BFree(ny)) _bPos = ny;
        }

        /// <summary>The cadet's feet fit here: a small box around the feet touches no wall or prop.</summary>
        bool BFree(Vector2 p)
        {
            const float rx = 0.32f, ry = 0.18f;
            return BFreeCell(p.x - rx, p.y - ry) && BFreeCell(p.x + rx, p.y - ry) && BFreeCell(p.x - rx, p.y + ry) && BFreeCell(p.x + rx, p.y + ry);
        }

        bool BFreeCell(float x, float y)
        {
            int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
            if (cx < 0 || cy < 0 || cx >= _bW || cy >= _bH) return false;
            return !_bSolid[cx, cy];
        }

        BSpot BSpotAt(Vector2 w)
        {
            BSpot best = null;
            float bestY = float.MaxValue;
            foreach (var s in _bSpots)
            {
                if (s.nameKey == null || s.sr == null || !s.sr.enabled || !s.sr.gameObject.activeInHierarchy || s.sr.sprite == null)
                {
                    if (s.id == "hatch" && _bDoor != null && _bDoor.bounds.Contains(new Vector3(w.x, w.y, _bDoor.transform.position.z))) return s;
                    if (s.id == "lifeboat" && _sHatchSr != null && _sHatchSr.bounds.Contains(new Vector3(w.x, w.y, _sHatchSr.transform.position.z))) return s;
                    continue;
                }
                var b = s.sr.bounds;
                if (w.x < b.min.x || w.x > b.max.x || w.y < b.min.y || w.y > b.max.y) continue;
                if (s.sr.transform.position.y < bestY) { best = s; bestY = s.sr.transform.position.y; }
            }
            return best;
        }

        void BUseNearest()
        {
            var best = BNearestSpot();
            if (best != null) { BFace(best); BUse(best); }
        }

        BSpot BNearestSpot()
        {
            BSpot best = null;
            float bd = 1.7f;
            foreach (var s in _bSpots)
            {
                if (s.nameKey == null || s.sr != null && (!s.sr.enabled || !s.sr.gameObject.activeInHierarchy)) continue;
                var stand = s.id == "pet" || s.id.StartsWith("guest") ? (Vector2)s.sr.transform.position : s.stand;
                float d = Vector2.Distance(stand, _bPos);
                if (BHasTask(s.id)) d -= 0.5f;
                if (d < bd) { bd = d; best = s; }
            }
            return best;
        }

        void BFace(BSpot s)
        {
            var p = s.sr != null ? (Vector2)s.sr.transform.position : s.pos;
            if (Mathf.Abs(p.x - _bPos.x) > 0.2f) _bRight = p.x > _bPos.x;
        }

        void BGoTo(Vector2 target, BSpot use)
        {
            if (use != null && (use.id == "pet" || use.id.StartsWith("guest")))
                target = (Vector2)use.sr.transform.position + new Vector2(_bPos.x < use.sr.transform.position.x ? -0.9f : 0.9f, 0);
            if (use != null && Vector2.Distance(target, _bPos) < BReach) { BFace(use); BUse(use); return; }
            var path = BFindPath(_bPos, target);
            if (path == null) { if (use != null && Vector2.Distance(use.stand, _bPos) < 2.2f) { BFace(use); BUse(use); } return; }
            _bPath = path;
            _bGoal = use;
        }

        /// <summary>Grid A* over tile centres (8 directions, no corner cutting), ending at the exact target.</summary>
        List<Vector2> BFindPath(Vector2 from, Vector2 to)
        {
            var start = new Vector2Int(Mathf.FloorToInt(from.x), Mathf.FloorToInt(from.y));
            var goal = new Vector2Int(Mathf.FloorToInt(to.x), Mathf.FloorToInt(to.y));
            if (!BIn(goal) || _bSolid[goal.x, goal.y])
            {
                // nearest open cell to the target
                float bd = float.MaxValue; bool found = false;
                for (int dx = -2; dx <= 2; dx++)
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        var c = new Vector2Int(goal.x + dx, goal.y + dy);
                        if (!BIn(c) || _bSolid[c.x, c.y]) continue;
                        float d = Vector2.Distance(new Vector2(c.x + 0.5f, c.y + 0.5f), to);
                        if (d < bd) { bd = d; goal = c; found = true; }
                    }
                if (!found) return null;
                to = new Vector2(goal.x + 0.5f, goal.y + 0.5f);
            }
            var open = new List<Vector2Int> { start };
            var came = new Dictionary<Vector2Int, Vector2Int>();
            var g = new Dictionary<Vector2Int, float> { [start] = 0 };
            int guard = 0;
            while (open.Count > 0 && guard++ < 4000)
            {
                int bi = 0; float bf = float.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    float f = g[open[i]] + Vector2Int.Distance(open[i], goal);
                    if (f < bf) { bf = f; bi = i; }
                }
                var cur = open[bi];
                open.RemoveAt(bi);
                if (cur == goal)
                {
                    var path = new List<Vector2> { to };
                    while (came.ContainsKey(cur)) { cur = came[cur]; if (cur != start) path.Insert(0, new Vector2(cur.x + 0.5f, cur.y + 0.5f)); }
                    return path;
                }
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var n = new Vector2Int(cur.x + dx, cur.y + dy);
                        if (!BIn(n) || _bSolid[n.x, n.y]) continue;
                        if (dx != 0 && dy != 0 && (_bSolid[cur.x + dx, cur.y] || _bSolid[cur.x, cur.y + dy])) continue;
                        float ng = g[cur] + (dx != 0 && dy != 0 ? 1.414f : 1f);
                        if (g.TryGetValue(n, out var old) && old <= ng) continue;
                        g[n] = ng; came[n] = cur;
                        if (!open.Contains(n)) open.Add(n);
                    }
            }
            return null;
        }

        bool BIn(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < _bW && c.y < _bH;

        void BAnimate(float dt, BSpot hover)
        {
            _bAnim += dt;
            var pose = _bMoving ? (Pose)((int)Pose.Walk0 + (int)(_bAnim * 9) % 4) : ((int)(_bAnim * 1.6f) % 2 == 0 ? Pose.Idle0 : Pose.Idle1);
            bool outside = BIsOutside(_bPos);
            // four directions: back when walking away, front when coming down, side otherwise
            if (_bMoving && _bLastMove != Vector2.zero)
                _bFacing = Mathf.Abs(_bLastMove.y) > Mathf.Abs(_bLastMove.x) * 0.9f ? (_bLastMove.y > 0 ? 1 : 0) : 2;
            int frame = _bMoving ? (int)_bGaitPhase % 8 : ((_bAnim % 3.2f) > 3.05f ? -2 : -1);
            _bPlayer.sprite = Art.Cadet(_bGather ? 0 : _bFacing, frame, outside, _bRun && _bMoving);
            _bPlayer.flipX = _bFacing == 2 && !_bRight;
            _bPlayer.transform.position = new Vector3(_bPos.x, _bPos.y - 0.15f, 0);
            _bPlayer.sortingOrder = SortY(_bPos.y);

            if (_bGather)
            {
                // everyone sits round the pod console: left seats face right, right seats face left
                _bPos = PodSeats[0] + new Vector2(0.15f, -0.2f);
                _bRight = true;
                _bMoving = false;
                // the pet cannot sit on a seat: it stands on the floor beside the cadet
                _bPetPos = PodSeats[0] + new Vector2(1.1f, -0.55f);
                _bPet.flipX = false;
                for (int i = 0; i < _bGuests.Count; i++)
                    if (i + 1 < PodSeats.Length) { _bGuests[i].transform.position = PodSeats[i + 1] + new Vector2(0.1f, -0.2f); _bGuests[i].sortingOrder = SortY(PodSeats[i + 1].y - 0.2f); _bGuests[i].flipX = i != 1; }
                    else _bGuests[i].transform.position = new Vector3(5.5f + (i - 1) * 0.9f, 9.5f, 0);
                _bPlayer.transform.position = new Vector3(_bPos.x, _bPos.y - 0.15f, 0);
                _bPlayer.sortingOrder = SortY(_bPos.y);
            }
            // the pet follows the cadet's trail
            if (_bMoving && (_bTrail.Count == 0 || Vector2.Distance(_bTrail[_bTrail.Count - 1], _bPos) > 0.25f)) _bTrail.Add(_bPos);
            while (_bTrail.Count > 6) _bTrail.RemoveAt(0);
            if (_bPet.gameObject.activeSelf)
            {
                var petWas = _bPetPos;
                if (_bTrail.Count >= 5 && !_bGather && !_petStay) _bPetPos = Vector2.MoveTowards(_bPetPos, _bTrail[0], BSpeed * 0.95f * dt);
                // animate and turn only while the pet really walks (it used to step in place and flip with the
                // cadet's direction, which looked like constant flicker); a short grace keeps the gait smooth
                var petStep = _bPetPos - petWas;
                if (!_petStay && petStep.sqrMagnitude > 1e-6f)
                {
                    _bPetWalkT = 0.15f;
                    if (Mathf.Abs(petStep.x) >= Mathf.Abs(petStep.y) * 0.7f) { _bPetDir = 2; _bPet.flipX = petStep.x < 0; }
                    else _bPetDir = petStep.y > 0 ? 1 : 0;
                }
                else _bPetWalkT -= dt;
                bool petWalk = !_petStay && _bPetWalkT > 0;
                float bob = petWalk ? Mathf.Abs(Mathf.Sin(_bAnim * 14)) * 0.06f : 0;
                _bPet.sprite = Art.PetFrame(PetId, petWalk ? (int)(_bAnim * 8) % 8 : -1, _bPetDir);
                _bPet.transform.position = new Vector3(_bPetPos.x, _bPetPos.y - 0.15f + bob, 0);
                _bPet.sortingOrder = SortY(_bPetPos.y);
            }
            if (_bHeartT > 0)
            {
                _bHeartT -= dt;
                _bHeart.gameObject.SetActive(true);
                _bHeart.transform.position = new Vector3(_bPetPos.x, _bPetPos.y + 1.0f + (1.2f - _bHeartT) * 0.6f, 0);
            }
            else _bHeart.gameObject.SetActive(false);

            for (int i = 0; i < _bGuests.Count; i++)
                if (_bGuests[i].gameObject.activeSelf)
                    _bGuests[i].transform.localScale = new Vector3(1, 1 + Mathf.Sin(_bAnim * 2 + i) * 0.02f, 1);

            // the door looks open while the cadet stands in it
            bool inDoor = _bPos.y > _bHatch.y - 0.4f && _bPos.y < _bHatch.y + 1.4f && _bPos.x > _bHatch.x - 0.4f && _bPos.x < _bHatch.x + 2.4f;
            string active = BActiveSpot;
            bool knocking = active == "hatch";
            if (_bDoor != null)
            {
                _bDoor.sprite = Art.PeepDoor(knocking && ((int)(_bAnim * 3) % 2 == 0));
                _bDoor.color = new Color(1, 1, 1, _bDoorOpen ? 0.22f : 1f);
            }

            // the visitor waits outside the door
            var knock = _bMode == 0 ? BKnockCard() : null;
            bool showKnocker = knock != null && !_bKnockPending && (_lbView == LbView.Card || _lbView == LbView.Result && _bOpen)
                && _life != null && !_life.State.guests.Contains(knock.visitor) && _life.State.outcome == LifeOutcome.Ongoing;
            _bKnocker.gameObject.SetActive(showKnocker);
            if (showKnocker) _bKnocker.sprite = Art.Visitor(knock.visitor);

            // a "!" over every place with something to do; no guide arrow: the player chooses where to go first
            _bMarker.gameObject.SetActive(false);
            _bArrow.gameObject.SetActive(false);
            _bTaskSpots.Clear();
            var marks = new List<(Vector3 top, bool urgent)>();
            void Mark(string id, bool urg)
            {
                var sp = BSpotById(id);
                if (sp == null || !_bTaskSpots.Add(id)) return;
                if (sp.sr != null && (!sp.sr.enabled || !sp.sr.gameObject.activeInHierarchy) && id != "hatch") return;
                marks.Add((BMarkTop(sp, showKnocker), urg));
            }
            if (_bMode == 1) { if (active != null) Mark(active, false); }
            else if (_life != null && !_bOpen && !_bGather && _life.State.outcome == LifeOutcome.Ongoing)
            {
                if (_lbView == LbView.Card)
                    foreach (var c in _life.OpenCards())
                    {
                        if (!string.IsNullOrEmpty(c.visitor) && (c != knock || _bKnockPending)) continue;
                        bool urg = BUrgent(c) || c == _bTimerCard && _bTimer > 0 && (_bTimerAlarm || _bTimer < 15f);
                        Mark(BCardSpot(c), urg);
                    }
                if (_lbView == LbView.Ration && _life.State.schedule.Hour >= DinnerHour - 1) Mark("seats", false);
                if (_bPanelsDusty) Mark("solar", false);
                for (int i = 0; i < 3; i++) if (_bPlot[i] == 3) Mark("plot" + i, false);
            }
            if (_bTaskMarks.Count > 0 && _bTaskMarks[0] == null) _bTaskMarks.Clear();
            while (_bTaskMarks.Count < marks.Count) _bTaskMarks.Add(BSprite("task-mark", Art.Marker(false), Vector2.zero, 31000));
            for (int i = 0; i < _bTaskMarks.Count; i++)
            {
                bool on = i < marks.Count;
                _bTaskMarks[i].gameObject.SetActive(on);
                if (!on) continue;
                _bTaskMarks[i].sprite = Art.Marker(marks[i].urgent);
                _bTaskMarks[i].transform.position = marks[i].top + Vector3.up * Mathf.Abs(Mathf.Sin(_bAnim * 4 + i * 0.9f)) * 0.25f;
            }
        }

        void BLight(float dt)
        {
            // time of day follows the day's progress: morning → evening → night
            float t = 0.15f;
            if (_life != null)
            {
                t = _life.State.schedule.Progress;
                if (_lbView == LbView.Ration) t = 0.85f;
                if (_lbView == LbView.Night) t = 1f;
            }
            // dusk comes late and stays readable: the base is never too dark to find things
            float dark = t < 0.72f ? Mathf.Lerp(0.08f, 0.0f, t / 0.72f) : Mathf.Lerp(0.02f, 0.46f, (t - 0.72f) / 0.28f);
            var col = new Color(0.03f, 0.04f, 0.14f, dark);
            if (_bTimerAlarm && _bTimer > 0)
            {
                float pulse = 0.12f + Mathf.Abs(Mathf.Sin(_bAnim * (Settings.ReducedMotion ? 0 : 3))) * 0.18f;
                col = Color.Lerp(col, new Color(0.9f, 0.05f, 0.08f, pulse + dark * 0.5f), 0.8f);
            }
            _bDark.color = Color.Lerp(_bDark.color, col, dt * 3f);
            foreach (var (sr, strength) in _bLamps)
            {
                var c = sr.color;
                c.a = Mathf.Lerp(c.a, Mathf.Clamp01(0.24f + dark * 0.8f) * strength, dt * 3f);
                sr.color = c;
            }
            bool storm = _life != null && (_lbCard != null && _lbCard.id.StartsWith("storm") || _life.State.day == 4 && _lbView == LbView.Ration);
            var dc = _bDust.color; dc.a = Mathf.Lerp(dc.a, storm ? (BIsOutside(_bPos) ? 0.4f : 0.18f) : 0f, dt * 1.5f); _bDust.color = dc;
        }

        void BCamera(float dt)
        {
            var target = BCameraTarget();
            var p = _bCam.transform.position;
            var n = Vector3.Lerp(p - _bShakeOff, target, 1 - Mathf.Exp(-dt * 6f));
            _sShake = Mathf.Max(0, _sShake - dt);
            _bShakeOff = _sShake > 0 && !Settings.ReducedMotion ? (Vector3)(Random.insideUnitCircle * _sShake * 0.35f) : Vector3.zero;
            _bCam.transform.position = n + _bShakeOff;
        }

        Vector3 BCameraTarget()
        {
            var t = _bGather ? new Vector2(5.5f, 12.2f) : new Vector2(_bPos.x, _bPos.y + 0.6f);
            if (_ui.ScreenOpen) t.x += _bCam.orthographicSize * _bCam.aspect * (_bGather ? 0.65f : 0.55f);   // the diary panel sits on the right
            float hw = _bCam.orthographicSize * _bCam.aspect, hh = _bCam.orthographicSize;
            t.x = Mathf.Clamp(t.x, -3 + hw, _bW + 3 - hw + (_ui.ScreenOpen ? hw : 0));
            t.y = Mathf.Clamp(t.y, -1 + hh, _bH + 1 - hh);
            return new Vector3(t.x, t.y, -10);
        }

        Vector3 _bShakeOff;
        Vector2 _bLastMove = Vector2.down;
        int _bFacing;
        bool _bRun;
        readonly SprintMeter _sprint = new SprintMeter();
        float _bGaitPhase;
        float _bStepT;

        void BSnapCamera() { if (_bCam != null) _bCam.transform.position = BCameraTarget(); }

        // ------------------------------------------------------------------ HUD

        void BBuildHud()
        {
            if (_bHud != null) _bHud.RemoveFromHierarchy();
            _bHud = new VisualElement { pickingMode = PickingMode.Ignore };
            _bHud.style.position = Position.Absolute;
            _bHud.style.left = 24; _bHud.style.top = 20;
            _bHud.style.width = 566;
            _bHud.style.backgroundColor = GameUI.CPanel;
            GameUI.Border(_bHud, GameUI.CCyanD, 2, 12);
            _bHud.style.paddingLeft = _bHud.style.paddingRight = 18; _bHud.style.paddingTop = 10; _bHud.style.paddingBottom = 12;
            _bHudDay = _ui.L("", 24, true, GameUI.CAmber);
            _bHudStock = _ui.L("", 26, true);
            _bHudNext = _ui.L("", 21, false, GameUI.CText);
            _bHudTimer = _ui.L("", 23, true, GameUI.CRed);
            _bHudBar = new VisualElement { pickingMode = PickingMode.Ignore };
            _bHudBar.style.height = 8; _bHudBar.style.backgroundColor = GameUI.CRed; _bHudBar.style.marginTop = 4;
            var help = _ui.L(_loc.T("base.hud.controls"), 16, false, GameUI.CDim);
            help.style.marginTop = 6;
            _bHudRoom = _ui.L("", 22, true, GameUI.CCyan);
            _bHud.Add(_bHudDay); _bHud.Add(_bHudRoom); _bHud.Add(_bHudStock); _bHud.Add(_bHudNext); _bHud.Add(_bHudTimer); _bHud.Add(_bHudBar); _bHud.Add(help);
            _bHudGauges = new VisualElement { pickingMode = PickingMode.Ignore };
            _bHudGauges.style.marginTop = 4; _bHudGauges.style.marginBottom = 4;
            _bHud.Insert(2, _bHudGauges);
            _ui.Root.Insert(Mathf.Min(1, _ui.Root.childCount), _bHud);
            if (_bHudPortrait != null) _bHudPortrait.RemoveFromHierarchy();
            _bHudPortrait = new VisualElement { pickingMode = PickingMode.Ignore };
            _bHudPortrait.style.position = Position.Absolute; _bHudPortrait.style.left = 0; _bHudPortrait.style.right = 0; _bHudPortrait.style.top = 0; _bHudPortrait.style.bottom = 0;
            _ui.Root.Insert(Mathf.Min(2, _ui.Root.childCount), _bHudPortrait);
            if (_bMode == 1) _bHudPortrait.Add(BSprintPanel());
            PaceBuildHud();
            _bHudT = 0;
        }

        void BHud(float dt)
        {
            if (_bHud == null) return;
            var kbd = UnityEngine.InputSystem.Keyboard.current;
            if (kbd != null && kbd.tabKey.wasPressedThisFrame && _bMode == 0 && _life != null && !InCutscene)
            {
                if (_bagOpen && _ui.ScreenOpen) { _bagOpen = false; _ui.HideScreen(); }
                else if (!_ui.Blocking && !_bGather) JobShowBag();
                _bHudT = 0;
            }
            if (_bagOpen && !_ui.ScreenOpen) _bagOpen = false;
            bool baseHud = _bMode == 0 && _life != null && !_ui.ScreenOpen;
            _bHudPortrait.style.display = baseHud ? DisplayStyle.Flex : DisplayStyle.None;
            _bHudGauges.style.display = _bMode == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _bHudStock.style.display = DisplayStyle.Flex;
            string room = BRoomAt(_bPos);
            _bHudRoom.text = room == null ? "" : GameUI.Thai("▸ " + _loc.T(room));
            _bHudRoom.style.display = room == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (_bMode == 1) { SHud(); _bHudPortrait.style.display = !_ui.ScreenOpen ? DisplayStyle.Flex : DisplayStyle.None; if (_shipSprintFill != null) { _shipSprintFill.style.width = Length.Percent(_sprint.Value); _shipSprintFill.style.backgroundColor = _sprint.Exhausted ? GameUI.CAmber : GameUI.CCyan; } return; }
            bool show = _life != null && !_ui.ScreenOpen;
            _bHud.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;
            _bHudT -= dt;
            if (_bTimer > 0)
            {
                _bHudTimer.style.display = DisplayStyle.Flex; _bHudBar.style.display = DisplayStyle.Flex;
                float total = _bTimerAlarm ? LC.alarmSeconds : LC.visitorWaitSeconds;
                _bHudBar.style.width = Length.Percent(Mathf.Clamp01(_bTimer / total) * 100f);
                _bHudTimer.text = GameUI.Thai(_loc.T(_bTimerAlarm ? "base.hud.alarm" : "base.hud.wait", Mathf.CeilToInt(_bTimer)));
            }
            else if (_bMode == 0 && _bSuitT > 0)
            {
                _bHudTimer.style.display = DisplayStyle.Flex; _bHudBar.style.display = DisplayStyle.Flex;
                _bHudBar.style.width = Length.Percent(Mathf.Clamp01(_bSuitT / SuitSeconds) * 100f);
                _bHudTimer.text = GameUI.Thai(_loc.T("base.hud.suit", Mathf.CeilToInt(_bSuitT)));
            }
            else { _bHudTimer.style.display = DisplayStyle.None; _bHudBar.style.display = DisplayStyle.None; }
            if (_bHudT > 0) return;
            _bHudT = 0.25f;
            BRefreshProps();
            var st = _life.State;
            _bHudDay.text = GameUI.Thai(_loc.T("base.hud.day", st.day, LC.daysTotal));
            _bHudStock.style.display = DisplayStyle.Flex;
            _bHudStock.text = GameUI.Thai(BClockText());
            _bHudStock.style.fontSize = 23;
            _bHudGauges.Clear();
            _bHudGauges.Add(BGaugeRow(1f));
            _bHudPortrait.Clear();
            _bHudPortrait.Add(BPortraitPanel());
            if (!_bGather) _bHudPortrait.Add(JobBagStrip());
            string a = BActiveSpot;
            if (_lbView == LbView.Card && !_bOpen)
            {
                var todo = new List<string>();
                var knockNow = BKnockCard();
                foreach (var c in _life.OpenCards())
                {
                    if (!string.IsNullOrEmpty(c.visitor) && (c != knockNow || _bKnockPending)) continue;
                    todo.Add("! " + _loc.T($"life.card.{c.id}.title") + " · " + _loc.T("base.place." + BCardSpot(c)));
                }
                _bHudNext.text = GameUI.Thai(todo.Count == 0 ? "" : TT("To do (any order): ", "งานที่รอ (ทำอันไหนก่อนก็ได้): ") + string.Join("   ", todo));
            }
            else
            {
                string what = a == null ? "" : _lbView == LbView.Ration ? _loc.T("base.event.ration") : _loc.T($"life.card.{_lbCard.id}.title");
                _bHudNext.text = GameUI.Thai(a == null ? "" : _loc.T("base.hud.next", what, _loc.T("base.place." + a)));
            }
        }

        // ------------------------------------------------------------------ peephole

        /// <summary>Round fish-eye view through the airlock peephole: the visitor outside, Mars behind.</summary>
        VisualElement BPeephole(string visitor)
        {
            const int size = 170;
            var ring = new VisualElement();
            ring.style.width = size; ring.style.height = size; ring.style.flexShrink = 0;
            ring.style.borderTopLeftRadius = ring.style.borderTopRightRadius = ring.style.borderBottomLeftRadius = ring.style.borderBottomRightRadius = size / 2f;
            ring.style.overflow = Overflow.Hidden;
            ring.style.backgroundColor = new Color(0.84f, 0.56f, 0.38f);
            GameUI.Border(ring, new Color(0.08f, 0.09f, 0.14f), 12, size / 2f);
            var ground = new VisualElement();
            ground.style.position = Position.Absolute; ground.style.left = 0; ground.style.right = 0; ground.style.bottom = 0; ground.style.height = size * 0.34f;
            ground.style.backgroundColor = new Color(0.62f, 0.3f, 0.17f);
            ring.Add(ground);
            var sprite = Art.Visitor(visitor);
            float scale = Mathf.Min(120f / sprite.rect.height, 100f / sprite.rect.width);
            var pic = new VisualElement();
            pic.style.position = Position.Absolute;
            pic.style.width = sprite.rect.width * scale; pic.style.height = sprite.rect.height * scale;
            pic.style.left = (size - 24 - sprite.rect.width * scale) / 2f + 2; pic.style.bottom = size * 0.12f;
            pic.style.backgroundImage = new StyleBackground(sprite);
            ring.Add(pic);
            var glint = new VisualElement();
            glint.style.position = Position.Absolute; glint.style.left = 34; glint.style.top = 30; glint.style.width = 34; glint.style.height = 18;
            glint.style.backgroundColor = new Color(1, 1, 1, 0.18f);
            glint.style.borderTopLeftRadius = glint.style.borderTopRightRadius = glint.style.borderBottomLeftRadius = glint.style.borderBottomRightRadius = 10;
            ring.Add(glint);
            return ring;
        }
    }
}
