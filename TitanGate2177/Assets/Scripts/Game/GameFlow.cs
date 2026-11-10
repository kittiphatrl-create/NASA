// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do real NASA facts used as puzzle clues make a junior astronaut feel they solved it with science?
// Date: 2026-11-10
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TitanGate.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    /// <summary>
    /// The director: title, chapter 1 (ship), chapter 2 (Mars), ending. Owns input, rooms and the
    /// shared systems (inventory, knowledge cards, hints, language). Chapter logic is in the partial files.
    /// </summary>
    public partial class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }
        /// <summary>Set before a reload so a restart goes straight into the mission.</summary>
        public static bool SkipTitle;
        public static int SeedOverride = -1;

        GameConfig _cfg;
        Localization _loc;
        GameUI _ui;
        Sfx _sfx;
        CameraRig _rig;
        PlayerActor _player;
        OrbiActor _orbi;
        WorldBuilder _world;
        Transform _worldRoot;

        readonly Inventory _inv = new Inventory();
        readonly Codex _codex = new Codex();
        readonly Dictionary<string, int> _hintLevel = new Dictionary<string, int>();
        readonly List<Transform> _dust = new List<Transform>();

        int _chapter;          // 0 title, 1 ship, 2 Mars, 3 ending
        int _room;
        string _selected;
        bool _busy;
        Hotspot _hovered;

        public GameConfig Config => _cfg;
        public Localization Loc => _loc;
        public SurvivalSim Sim => _sim;
        public int Chapter => _chapter;
        public int CurrentRoom => _room;
        public bool Busy => _busy;
        public Inventory Inv => _inv;
        public Codex Cards => _codex;

        // ------------------------------------------------------------------ setup

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            _cfg = GameConfig.FromJson(Resources.Load<TextAsset>("Data/config").text);
            _loc = Localization.FromJson(Resources.Load<TextAsset>("Data/strings").text,
                (Lang)PlayerPrefs.GetInt("lang", (int)Lang.EN));

            _sfx = new GameObject("Sfx").AddComponent<Sfx>();
            var camGo = new GameObject("Main Camera");
            _rig = camGo.AddComponent<CameraRig>();
            _rig.Init();

            _player = new GameObject("Astronaut").AddComponent<PlayerActor>();
            _player.sfx = _sfx;
            _player.speed = _cfg.presentation.walkSpeed;
            _orbi = new GameObject("ORBI").AddComponent<OrbiActor>();
            _orbi.follow = _player;

            _ui = new GameObject("UI").AddComponent<GameUI>();
            _ui.sfx = _sfx;
            _ui.Init(_loc);
            _ui.CardsClicked += OnCards;
            _ui.HintClicked += OnHint;
            _ui.LangClicked += ToggleLang;
            _ui.ItemClicked += OnItemClicked;
            _ui.SkipClicked += SkipCutscene;
            _codex.Unlocked += RememberCard;
            _inv.Changed += RefreshAll;

            foreach (var old in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (old != _rig.cam) old.gameObject.SetActive(false);
        }

        void Start()
        {
            if (ContinueFromSave && Settings.HasSave) { ContinueFromSave = false; ContinueGame(); }
            else if (SkipTitle) { SkipTitle = false; StartCoroutine(StartChapter1()); }
            else ShowMainMenu();
            BeginAutoShotIfRequested();
        }

        void ClearWorld()
        {
            if (_worldRoot != null) Destroy(_worldRoot.gameObject);
            _worldRoot = new GameObject("World").transform;
            _world = new WorldBuilder(_worldRoot, _cfg.presentation.roomSpacing);
            _dust.Clear();
            _sparks.Clear();
            _sky = null;
            _animators.Clear();
            if (_stage != null) { Destroy(_stage.gameObject); _stage = null; }
            foreach (Transform t in _rig.transform) Destroy(t.gameObject);
        }

        // ------------------------------------------------------------------ input

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null)
            {
                if (kb.f12Key.wasPressedThisFrame) Capture("manual");
                if (kb.enterKey.wasPressedThisFrame && _ui.DialogOpen) _ui.Advance();
                if (_chapter > 0 && _chapter < 3 && !_ui.ScreenOpen)
                {
                    if (kb.hKey.wasPressedThisFrame) OnHint();
                    if (kb.cKey.wasPressedThisFrame) OnCards();
                    if (kb.escapeKey.wasPressedThisFrame)
                    {
                        if (_ui.ModalOpen) _ui.CloseModal();
                        else if (_selected != null) Select(null);
                        else if (!_ui.DialogOpen) OpenPause();
                    }
                }
            }
            UpdateDust();
            TickPressure();
            TickAnimators();
            if (InCutscene)
            {
                if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) SkipCutscene();
                return;
            }
            if (_chapter < 1 || _chapter > 2 || mouse == null || _world == null) return;

            bool showAll = kb != null && kb.spaceKey.isPressed && !_ui.Blocking && !_busy;
            Vector2 mp = mouse.position.ReadValue();
            Hotspot h = null;
            bool canWorld = !_busy && !_ui.Blocking && !_ui.PointerOverUI(mp);
            RaycastHit hit = default;
            bool rayHit = false;
            if (canWorld)
            {
                var ray = _rig.cam.ScreenPointToRay(mp);
                rayHit = Physics.Raycast(ray, out hit, 400f);
                if (rayHit) h = hit.collider.GetComponent<Hotspot>();
                if (h != null && h.room != _room) h = null;
            }

            if (h != _hovered && h != null) _sfx.Play("hover", 0.7f);
            _hovered = h;
            foreach (var hs in _world.rooms[_room].hotspots)
                hs.SetHighlight(hs == h || showAll, hs == h ? 1f : 0.65f);

            if (h != null)
            {
                string name = _loc.T(h.nameKey);
                if (_selected != null) name = _loc.T("ui.use_on", NameOf(_selected), name);
                _ui.SetHover(name, _ui.WorldToPanel(h.LabelAnchor, _rig.cam));
            }
            else _ui.SetHover(null, Vector2.zero);

            if (!canWorld) return;
            if (mouse.rightButton.wasPressedThisFrame) Select(null);
            if (!mouse.leftButton.wasPressedThisFrame) return;
            if (h != null) StartCoroutine(Interact(h));
            else
            {
                // click on the floor: walk there
                var ray = _rig.cam.ScreenPointToRay(mp);
                var plane = new Plane(Vector3.forward, new Vector3(0, 0, _player.transform.position.z));
                if (plane.Raycast(ray, out float d))
                {
                    var p = ray.GetPoint(d);
                    var r = _world.rooms[_room];
                    if (p.y < 3.5f)
                    {
                        float x = Mathf.Clamp(p.x, r.centerX - WorldBuilder.RoomW / 2f + 0.6f, r.centerX + WorldBuilder.RoomW / 2f - 0.6f);
                        StartCoroutine(WalkOnly(x));
                    }
                }
            }
        }

        IEnumerator WalkOnly(float x)
        {
            _busy = true;
            yield return _player.Walk(x);
            _busy = false;
        }

        IEnumerator Interact(Hotspot h)
        {
            _busy = true;
            _ui.SetHover(null, Vector2.zero);
            foreach (var hs in _world.rooms[_room].hotspots) hs.SetHighlight(false);
            yield return _player.Walk(h.standX);
            _player.Face(h.transform.position.x + (h.leadsTo >= 0 ? (h.leadsTo > _room ? 1 : -1) : 0));
            if (h.leadsTo >= 0)
            {
                yield return GoRoom(h.leadsTo);
            }
            else if (_selected != null)
            {
                var item = _selected;
                Select(null);
                yield return UseItem(item, h);
            }
            else if (_chapter == 1) yield return Ch1Interact(h);
            else if (_chapter == 2) yield return Ch2Interact(h);
            RefreshAll();
            _busy = false;
        }

        IEnumerator UseItem(string item, Hotspot h)
        {
            if (ItemFits(item, h.id))
            {
                if (_chapter == 1) yield return Ch1Interact(h);
                else yield return Ch2Interact(h);
            }
            else yield return Say("you", "use.fail", NameOf(item), _loc.T(h.nameKey));
        }

        static bool ItemFits(string item, string spot)
        {
            switch (item)
            {
                case "item_patchkit": return spot == "s_breach";
                case "item_hull_patch": case "item_plate": case "item_sealant": return spot == "s_line";
                case "item_drillbit": return spot == "mars_drill" || spot == "titan_drill";
                case "item_heater_coil": case "item_heated_bit": return spot == "titan_drill";
                case "item_feedhorn": case "item_cable": case "item_antenna_kit": return spot == "moon_dish";
                case "item_cat_fe": case "item_cat_ni": case "item_cat_cu": return spot == "s_reactor" || spot == "s_cabinet";
            }
            return false;
        }

        void OnItemClicked(string id)
        {
            if (_busy || _ui.Blocking) return;
            if (_selected == null) { Select(id); return; }
            if (_selected == id) { Select(null); return; }
            var a = _selected;
            Select(null);
            var result = _inv.Combine(a, id, _cfg.puzzles.recipes);
            if (result != null)
            {
                _sfx.Play("good");
                _ui.Toast(_loc.T("combine.ok", NameOf(result)), GameUI.CGreen);
                _ui.Say("orbi", "combine.made." + result);
            }
            else
            {
                _sfx.Play("bad");
                _ui.Say("you", "combine.fail", null, NameOf(a), NameOf(id));
            }
        }

        void Select(string id)
        {
            _selected = id;
            RefreshInventory();
        }

        string NameOf(string item) => _loc.T("item." + item);

        // ------------------------------------------------------------------ rooms

        IEnumerator GoRoom(int target)
        {
            int from = _room;
            _sfx.Play("whoosh", 0.6f);
            var door = _world.rooms[target].hotspots.Find(x => x.leadsTo == from)
                       ?? (_world.rooms[from].kind == RoomKind.Outside ? _world.Find("s_airlock") : null);
            float arriveX = door != null ? door.standX : _world.rooms[target].centerX;
            bool toOutside = _world.rooms[target].kind == RoomKind.Outside;
            bool fromOutside = _world.rooms[from].kind == RoomKind.Outside;
            if (toOutside != fromOutside)
            {
                // airlock cycle: suit up or down
                yield return Fade(1f, 0.35f);
                _player.SetSuit(toOutside);
                _room = target;
                _rig.SnapTo(_world.rooms[target].centerX);
                _player.Place(arriveX);
                _orbi.Snap();
                _sfx.Play("whoosh");
                yield return Fade(0f, 0.35f);
            }
            else
            {
                _room = target;
                _player.Place(arriveX);
                _orbi.Snap();
                yield return _rig.SlideTo(_world.rooms[target].centerX, _cfg.presentation.cameraMoveSeconds);
            }
            // one short step clear of the door, not a walk to the middle of the room
            float inside = arriveX + (arriveX > _world.rooms[target].centerX ? -0.7f : 0.7f);
            yield return _player.Walk(inside);
            yield return OnEnterRoom(target);
        }

        IEnumerator OnEnterRoom(int room)
        {
            if (_chapter == 2) yield return Ch2EnterRoom(room);
        }

        void PlaceInRoom(int room, float xOffset)
        {
            _room = room;
            _rig.SnapTo(_world.rooms[room].centerX);
            _player.Place(_world.rooms[room].centerX + xOffset);
            _orbi.Snap();
        }

        // ------------------------------------------------------------------ shared coroutines

        IEnumerator Say(string speaker, string key, params object[] args)
        {
            bool done = false;
            _ui.Say(speaker, key, () => done = true, args);
            while (!done) yield return null;
        }

        IEnumerator WaitModal()
        {
            while (_ui.ModalOpen) yield return null;
        }

        IEnumerator Fade(float a, float s)
        {
            _ui.FadeTo(a, s);
            while (!_ui.FadeDone) yield return null;
        }

        IEnumerator Wait(float s)
        {
            float t = 0;
            while (t < s) { t += Time.deltaTime; yield return null; }
        }

        /// <summary>Unlock a knowledge card and show it (the first time only): cards are the puzzle clues.</summary>
        bool _cardTutorialShown;

        /// <summary>
        /// Unlock a knowledge card. Only the first one of a session opens by itself; after that the
        /// card goes into the book with a toast and the player reads it when they need it (C).
        /// </summary>
        IEnumerator Card(string id)
        {
            if (!_codex.Unlock(id)) yield break;
            _sfx.Play("card");
            _ui.Toast(_loc.T("ui.new_card_named", _loc.T($"card.{id}.title")), GameUI.CCyan);
            RefreshAll();
            if (_cardTutorialShown) yield break;
            _cardTutorialShown = true;
            ShowCardModal(id);
            yield return WaitModal();
        }

        /// <summary>A short status line that does not stop the game (instead of a dialogue box).</summary>
        void Note(string key, params object[] args) => _ui.Toast(_loc.T(key, args), GameUI.CCyan);

        bool _cardModalOpen;

        void ShowCardModal(string id)
        {
            _cardModalOpen = true;
            _ui.ShowModal(p =>
            {
                p.style.maxWidth = 1000;
                p.Add(_ui.L(_loc.T("ui.card_header"), 22, true, GameUI.CAmber));
                _ui.Title(p, _loc.T($"card.{id}.title"));
                p.Add(_ui.L(_loc.T($"card.{id}.body"), 28));
            }, true, () => _cardModalOpen = false);
        }

        void OnCards()
        {
            if (_busy || _ui.Blocking || _chapter < 1 || _chapter > 2) return;
            ShowCardBook();
        }

        void OnHint()
        {
            if (_busy || _ui.Blocking || _chapter < 1 || _chapter > 2) return;
            if (_chapter == 2 && _sim != null && _sim.State.Has("orbi_off")) { _ui.Say("orbi", "orbi_resting"); return; }
            string obj = _chapter == 1 ? Ch1Objective() : Ch2HintId();
            int lvl = _hintLevel.TryGetValue(obj, out var l) ? Mathf.Min(l + 1, 2) : 1;
            _hintLevel[obj] = lvl;
            _ui.Say("orbi", $"hint.{obj}.{lvl}");
        }

        void ToggleLang()
        {
            _loc.Current = _loc.Current == Lang.TH ? Lang.EN : Lang.TH;
            PlayerPrefs.SetInt("lang", (int)_loc.Current);
            _ui.RefreshText();
            if (_chapter == 0) BuildMainMenu();
            RefreshAll();
        }

        // ------------------------------------------------------------------ HUD refresh

        public void RefreshAll()
        {
            if (_ui == null) return;
            _ui.RefreshButtons(_codex.Cards.Count, _cfg.cards.Count);
            RefreshInventory();
            if (_chapter == 1) Ch1Refresh();
            else if (_chapter == 2) Ch2Refresh();
        }

        void RefreshInventory() => _ui.SetInventory(_inv.Items, _selected, NameOf);

        // ------------------------------------------------------------------ dust (Mars)

        void UpdateDust()
        {
            if (_world == null || _world.rooms.Count == 0) return;
            var r = _world.rooms[_world.rooms.Count - 1];
            foreach (var d in _dust)
            {
                if (d == null) continue;
                var p = d.position;
                if (d.name == "rain")
                {
                    p.y -= Time.deltaTime * (5f + (d.GetInstanceID() & 3));
                    p.x -= Time.deltaTime * 0.8f;
                    if (p.y < 0) { p.y = 6.5f; p.x = r.centerX - 7 + (d.GetInstanceID() % 140) / 10f; }
                }
                else
                {
                    p.x += Time.deltaTime * (0.6f + (d.GetInstanceID() & 7) * 0.12f) * (_sim != null ? 1f + _sim.State.sol * 0.4f : 1f);
                    p.y += Mathf.Sin(Time.time * 1.3f + d.GetInstanceID()) * Time.deltaTime * 0.2f;
                    if (p.x > r.centerX + 8) p.x = r.centerX - 8;
                }
                d.position = p;
            }
        }

        // ------------------------------------------------------------------ restart / capture

        public void Restart()
        {
            SkipTitle = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void RestartToTitle()
        {
            SkipTitle = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Save a screenshot to Captures/ (project root in the editor, next to the exe in a build).</summary>
        public string Capture(string name)
        {
            var dir = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, "Captures");
            System.IO.Directory.CreateDirectory(dir);
            var path = System.IO.Path.Combine(dir, $"{name}.png");
            ScreenCapture.CaptureScreenshot(path);
            return path;
        }

        static string Signed(int v) => v > 0 ? "+" + v : v.ToString();

        string DeltaText(ResourceDelta d, int ap)
        {
            var sb = new StringBuilder();
            if (ap > 0) sb.Append(_loc.T("hud.ap_cost", ap));
            void Add(int v, string key) { if (v != 0) { if (sb.Length > 0) sb.Append("  "); sb.Append(_loc.T(key)).Append(' ').Append(Signed(v)); } }
            Add(d.o2, "hud.o2"); Add(d.water, "hud.water"); Add(d.power, "hud.power"); Add(d.food, "hud.food");
            Add(d.stamina, "hud.stamina"); Add(d.fuel, "hud.fuel");
            return sb.ToString();
        }
    }
}
