// LAST 72 HOURS — graybox UI built entirely from code (uGUI).
// Drop-in: auto-spawns on scene load, no scene setup needed.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Last72
{
    public class GameUI : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<GameUI>() == null)
                new GameObject("LAST72_Game").AddComponent<GameUI>();
        }

        // ---------- palette ----------
        static readonly Color CBg = new Color(0.06f, 0.07f, 0.10f);
        static readonly Color CPanel = new Color(0.11f, 0.13f, 0.17f, 0.97f);
        static readonly Color CPanel2 = new Color(0.16f, 0.19f, 0.24f);
        static readonly Color CText = new Color(0.88f, 0.92f, 0.96f);
        static readonly Color CDim = new Color(0.58f, 0.64f, 0.72f);
        static readonly Color CAccent = new Color(0.30f, 0.75f, 1f);
        static readonly Color CGood = new Color(0.35f, 0.85f, 0.45f);
        static readonly Color CWarn = new Color(1f, 0.75f, 0.25f);
        static readonly Color CBad = new Color(1f, 0.32f, 0.30f);
        static readonly Color CButton = new Color(0.22f, 0.27f, 0.34f);

        GameModel model;
        Font font;
        Transform canvas;

        // HUD refs
        Text countdown, roomTitle, invHint, selectedLabel;
        readonly Dictionary<string, Bar> bars = new Dictionary<string, Bar>();
        RectTransform roomView, alertList;
        readonly List<Button> invSlots = new List<Button>();
        Room shownRoom = (Room)(-1);
        int shownAlerts;

        // overlay refs
        GameObject modalRoot, bannerRoot;
        Text bannerText;
        float bannerUntil;
        string openHotspot;

        // walking astronaut / room life (2.5D: stage holds objects + astronaut, depth-sorted)
        RectTransform roomContent, stage, marks, actorLayer, promptRt;
        RoomShape shape = new RoomShape();
        readonly FloorPath floorPath = new FloorPath();
        readonly List<KeyValuePair<float, Transform>> sortNodes = new List<KeyValuePair<float, Transform>>();
        List<Transform> lastSortOrder;
        float doorCooldown;
        static readonly Vector2 StartPos = new Vector2(0.40f, 0.18f);
        /// Test hook: extra 2D input (x = A/D, y = W/S) added to the keyboard.
        public Vector2 DebugMove;
        Astronaut astro;
        Text promptText;
        Image fade;
        bool transitioning;
        Room lastRoom = Room.Quarters;
        Hotspot near, nearDoor;
        readonly Dictionary<string, Button> hotspotButtons = new Dictionary<string, Button>();
        readonly List<Image> blinkers = new List<Image>();
        readonly List<RectTransform> stars = new List<RectTransform>();
        Image strip; bool stripBroken;
        /// Test hook: extra horizontal input (-1..1) added to keyboard axis.
        public float DebugAxis;
        float nextStep;
        public Astronaut Astro { get { return astro; } }
        public bool Transitioning { get { return transitioning; } }
        public string NearId { get { return near != null ? near.Id : null; } }

        // layout v2 (research-based): ship map, objective tracker, toasts, cursor item, hover line
        readonly Dictionary<Room, Image> mapRooms = new Dictionary<Room, Image>();
        readonly Dictionary<Room, Image> mapDots = new Dictionary<Room, Image>();
        readonly List<Image> invIcons = new List<Image>();
        Text nextText, hoverText, cursorItemText, rationsText;
        RectTransform cursorItemRt;
        CanvasGroup toastGroup;
        float toastAt;

        // beta polish: smart cursor, double-click, guide marker, pause menu
        Hotspot hovered;
        string lastClickId; float lastClickAt = -9, lastFloorClickAt = -9;
        const float DoubleClick = 0.4f;
        RectTransform guideRt; Text guideText;
        string lastHint; float hintSince;
        const float GuideIdleSeconds = 20f;
        bool guideOn = true;
        // depth: inner rooms, close-up zoom, object markers
        readonly HashSet<string> inspected = new HashSet<string>();
        Hotspot zoomFrom; // set right before opening a panel from the room = animate the zoom-in
        /// Test hooks
        public CursorKind CursorNow { get { return ContextCursor.Current; } }
        public bool GuideVisible { get { return guideRt != null && guideRt.gameObject.activeSelf; } }
        public string GuideTarget { get; private set; }
        public string ModalName { get { return modalRoot != null ? modalRoot.name : null; } }
        public void SimulateEscape() { OnEscape(); }

        class Bar { public RectTransform Fill; public Image FillImg; public Text Label; public bool HighIsBad; public string Name; }

        // ================= setup =================
        void Awake()
        {
            model = new GameModel();
            Sfx.Init(gameObject);
            Sfx.Ambient(true);
            AudioListener.volume = PlayerPrefs.GetFloat("l72_volume", 1f);
            guideOn = PlayerPrefs.GetInt("l72_guide", 1) == 1;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Tahoma", "Segoe UI", "Segoe UI Symbol", "Arial" }, 28);
            SetupCamera();
            SetupEventSystem();
            BuildCanvas();
            BuildHUD();
            astro.Place(StartPos);
            model.Changed += Refresh;
            Refresh();
            ShowStart();
        }

        void SetupCamera()
        {
            var cam = Camera.main;
            if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = CBg; }
        }

        void SetupEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        void BuildCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            canvas = go.transform;
            Box(canvas, "BG", 0, 0, 1, 1, CBg);
        }

        // ================= ui helpers =================
        internal RectTransform Box(Transform parent, string name, float x0, float y0, float x1, float y1, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            if (color.HasValue) { var img = go.AddComponent<Image>(); img.color = color.Value; }
            return rt;
        }

        internal Text Label(Transform parent, string text, int size, TextAnchor anchor, Color color,
            float x0 = 0, float y0 = 0, float x1 = 1, float y1 = 1, bool bold = false, int pad = 8)
        {
            var rt = Box(parent, "Text", x0, y0, x1, y1);
            rt.offsetMin = new Vector2(pad, pad / 2); rt.offsetMax = new Vector2(-pad, -pad / 2);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.alignment = anchor; t.color = color; t.text = text;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        internal Button Btn(Transform parent, string text, float x0, float y0, float x1, float y1, Color bg, Action onClick, int size = 22)
        {
            var rt = Box(parent, "Btn_" + text, x0, y0, x1, y1, bg);
            var b = rt.gameObject.AddComponent<Button>();
            var cb = b.colors;
            cb.normalColor = new Color(0.85f, 0.85f, 0.85f);
            cb.highlightedColor = Color.white;
            cb.selectedColor = new Color(0.85f, 0.85f, 0.85f);
            cb.pressedColor = new Color(0.65f, 0.65f, 0.65f);
            cb.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.45f);
            b.colors = cb;
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1, 1, 1, 0.15f);
            Label(rt, text, size, TextAnchor.MiddleCenter, CText, bold: true);
            b.onClick.AddListener(() => Sfx.Play("ui_click", 0.45f));
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        /// Deactivate first so stale UI can't be clicked before end-of-frame Destroy.
        static void Kill(GameObject go) { go.SetActive(false); Destroy(go); }
        internal void KillGo(GameObject go) { Kill(go); }

        static string Hex(Color c) { return ColorUtility.ToHtmlStringRGB(c); }

        // ================= HUD =================
        void BuildHUD()
        {
            // ===== TOP BAR (FTL-style status left, big clock centre, needs right) =====
            var top = Box(canvas, "TopBar", 0, 0.9f, 1, 1, CPanel);

            // ship map: click a room to travel (free)
            var map = Box(top, "ShipMap", 0.005f, 0.08f, 0.205f, 0.92f, new Color(0, 0, 0, 0.45f));
            Label(map, "ASTRA-01", 12, TextAnchor.UpperLeft, CDim, 0, 0.7f, 1, 1, true, 6);
            Room[] order = { Room.Quarters, Room.Command, Room.LifeSupport, Room.Engineering };
            string[] shortNames = { "QTRS", "CMD", "LIFE", "ENG" };
            for (int i = 0; i < 4; i++)
            {
                Room r = order[i];
                float x0 = 0.03f + i * 0.24f;
                var b = Btn(map, shortNames[i], x0, 0.08f, x0 + 0.22f, 0.68f, CButton, () => TravelTo(r), 14);
                mapRooms[r] = b.GetComponent<Image>();
                var dot = Box(b.transform, "Dot", 0.78f, 0.66f, 0.94f, 0.92f, CGood).GetComponent<Image>();
                dot.raycastTarget = false;
                mapDots[r] = dot;
                if (i < 3) Box(map, "Link", x0 + 0.22f, 0.36f, x0 + 0.24f, 0.4f, CDim).GetComponent<Image>().raycastTarget = false;
            }

            // ship status (stacked bars)
            Label(top, "SHIP", 12, TextAnchor.UpperLeft, CDim, 0.212f, 0.78f, 0.3f, 1f, true, 4);
            AddBar(top, "o2", "OXYGEN", 0.212f, 0.54f, false);
            AddBar(top, "pwr", "POWER", 0.212f, 0.30f, false);
            AddBar(top, "prop", "PROPELLANT", 0.212f, 0.06f, false);

            // countdown (centre, biggest thing on screen)
            var cd = Box(top, "Countdown", 0.40f, 0.06f, 0.60f, 0.94f, new Color(0, 0, 0, 0.55f));
            cd.gameObject.AddComponent<Outline>().effectColor = new Color(0.3f, 0.75f, 1f, 0.35f);
            Label(cd, "MISSION COUNTDOWN", 13, TextAnchor.UpperCenter, CDim, 0, 0.7f, 1, 1);
            countdown = Label(cd, "72:00:00", 54, TextAnchor.MiddleCenter, CAccent, 0, 0, 1, 0.8f, true);

            // astronaut needs
            Label(top, "ASTRONAUT", 12, TextAnchor.UpperLeft, CDim, 0.61f, 0.78f, 0.72f, 1f, true, 4);
            AddBar(top, "hun", "HUNGER", 0.61f, 0.42f, true);
            AddBar(top, "fat", "FATIGUE", 0.61f, 0.12f, true);
            rationsText = Label(top, "", 12, TextAnchor.UpperRight, CDim, 0.66f, 0.78f, 0.79f, 1f, false, 4);

            // menus
            Btn(top, "OBJECTIVES", 0.80f, 0.12f, 0.855f, 0.88f, CButton, ShowObjectives, 14);
            Btn(top, "HANDBOOK", 0.858f, 0.12f, 0.913f, 0.88f, new Color(0.18f, 0.34f, 0.50f), ShowHandbook, 14);
            Btn(top, "LOG", 0.916f, 0.12f, 0.951f, 0.88f, CButton, ShowLog, 14);
            Btn(top, "MENU\n<size=10>ESC</size>", 0.954f, 0.12f, 0.995f, 0.88f, CButton, ShowPause, 14);

            // ===== OBJECTIVE TRACKER strip =====
            var strip2 = Box(canvas, "Tracker", 0.005f, 0.858f, 0.995f, 0.896f, new Color(0.08f, 0.10f, 0.14f, 0.95f));
            nextText = Label(strip2, "", 17, TextAnchor.MiddleLeft, CWarn, 0, 0, 0.72f, 1, true, 10);
            roomTitle = Label(strip2, "", 18, TextAnchor.MiddleRight, CText, 0.6f, 0, 1, 1, true, 10);

            // ===== ROOM VIEW (hero, full width) =====
            roomView = Box(canvas, "RoomView", 0.005f, 0.118f, 0.995f, 0.854f, CPanel2);
            roomView.gameObject.AddComponent<RectMask2D>();
            roomContent = Box(roomView, "Content", 0, 0, 1, 1);   // backdrop + floor click catcher
            stage = Box(roomView, "Stage", 0, 0, 1, 1);           // objects + astronaut, sorted by depth
            actorLayer = Box(roomView, "Actors", 0, 0, 1, 1);     // markers, prompt, guide arrow (always on top)
            marks = Box(actorLayer, "Marks", 0, 0, 1, 1);
            astro = Astronaut.Build(stage);
            promptRt = Box(actorLayer, "Prompt", 0, 0, 0, 0, new Color(0.04f, 0.06f, 0.10f, 1f));
            promptRt.pivot = new Vector2(0.5f, 0f);
            promptRt.sizeDelta = new Vector2(420, 36);
            promptRt.GetComponent<Image>().raycastTarget = false;
            promptRt.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 0.82f, 0.3f, 0.7f);
            promptText = Label(promptRt, "", 18, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f), bold: true, pad: 6);
            promptText.resizeTextForBestFit = true; promptText.resizeTextMinSize = 11; promptText.resizeTextMaxSize = 18;
            promptRt.gameObject.SetActive(false);
            // guide marker: bouncing arrow over the next objective (tutorial + idle hint)
            guideRt = Box(actorLayer, "Guide", 0, 0, 0, 0);
            guideRt.pivot = new Vector2(0.5f, 0f);
            guideRt.sizeDelta = new Vector2(80, 60);
            guideText = Label(guideRt, "▼", 46, TextAnchor.LowerCenter, CWarn, bold: true, pad: 0);
            guideText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            guideRt.gameObject.SetActive(false);
            // hover "sentence line" (what a click will do)
            hoverText = Label(roomView, "", 18, TextAnchor.MiddleCenter, new Color(0.85f, 0.95f, 1f), 0.2f, 0.0f, 0.8f, 0.055f, true);
            hoverText.gameObject.AddComponent<Shadow>();
            fade = Box(roomView, "Fade", 0, 0, 1, 1, new Color(0, 0, 0, 0)).GetComponent<Image>();
            fade.raycastTarget = false;

            // ===== TOASTS (alerts, top-right over the room, never block clicks) =====
            var toastRt = Box(canvas, "Toasts", 0.70f, 0.60f, 0.99f, 0.85f);
            toastGroup = toastRt.gameObject.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false; toastGroup.interactable = false;
            alertList = toastRt;

            // ===== INVENTORY (slim, always visible, icon slots) =====
            var inv = Box(canvas, "Inventory", 0.005f, 0.005f, 0.995f, 0.112f, CPanel);
            Label(inv, "INVENTORY", 13, TextAnchor.UpperLeft, CDim, 0, 0.7f, 0.1f, 1, true, 6);
            for (int i = 0; i < 7; i++)
            {
                int idx = i;
                float x0 = 0.075f + i * 0.078f;
                var b = Btn(inv, "", x0, 0.1f, x0 + 0.072f, 0.9f, CButton, () => OnInvSlot(idx), 14);
                var t = b.GetComponentInChildren<Text>();
                var tr = (RectTransform)t.transform; tr.anchorMin = new Vector2(0, 0); tr.anchorMax = new Vector2(1, 0.45f);
                t.alignment = TextAnchor.MiddleCenter;
                var icon = Box(b.transform, "Icon", 0.32f, 0.48f, 0.68f, 0.92f, Color.clear).GetComponent<Image>();
                icon.raycastTarget = false;
                invIcons.Add(icon);
                invSlots.Add(b);
            }
            selectedLabel = Label(inv, "", 18, TextAnchor.UpperLeft, CAccent, 0.63f, 0.5f, 1, 0.95f, true);
            invHint = Label(inv, "", 14, TextAnchor.UpperLeft, CDim, 0.63f, 0.04f, 1, 0.55f);

            // item attached to the cursor while selected
            cursorItemRt = Box(canvas, "CursorItem", 0, 0, 0, 0, new Color(0.15f, 0.55f, 0.85f, 0.95f));
            cursorItemRt.pivot = new Vector2(0f, 1f);
            cursorItemRt.sizeDelta = new Vector2(150, 30);
            cursorItemRt.GetComponent<Image>().raycastTarget = false;
            cursorItemText = Label(cursorItemRt, "", 15, TextAnchor.MiddleCenter, Color.white, bold: true, pad: 4);
            cursorItemRt.gameObject.SetActive(false);

            // --- banner (above room)
            bannerRoot = Box(canvas, "Banner", 0.18f, 0.775f, 0.69f, 0.85f, new Color(0.45f, 0.05f, 0.05f, 0.95f)).gameObject;
            bannerRoot.GetComponent<Image>().raycastTarget = false; // never block hotspot clicks
            bannerRoot.AddComponent<Outline>().effectColor = CBad;
            bannerText = Label(bannerRoot.transform, "", 22, TextAnchor.MiddleCenter, Color.white, bold: true, pad: 16);
            bannerRoot.SetActive(false);
        }

        void AddBar(Transform parent, string key, string name, float x, float y, bool highIsBad)
        {
            var root = Box(parent, "Bar_" + key, x, y, x + 0.18f, y + 0.22f, new Color(0, 0, 0, 0.55f));
            var fill = Box(root, "Fill", 0, 0, 0.5f, 1, CGood);
            var label = Label(root, name, 13, TextAnchor.MiddleLeft, Color.white, bold: true, pad: 4);
            label.gameObject.AddComponent<Shadow>();
            bars[key] = new Bar { Fill = fill, FillImg = fill.GetComponent<Image>(), Label = label, HighIsBad = highIsBad, Name = name };
        }

        void SetBar(string key, float v)
        {
            var b = bars[key];
            b.Fill.anchorMax = new Vector2(Mathf.Clamp01(v / 100f), 1);
            float danger = b.HighIsBad ? v : 100 - v;
            b.FillImg.color = danger >= 75 ? CBad : danger >= 55 ? CWarn : (b.HighIsBad ? CAccent : CGood);
            b.Label.text = b.Name + "  " + Mathf.RoundToInt(v);
        }

        // ================= refresh =================
        void Refresh()
        {
            countdown.text = GameModel.FormatTime(model.MinutesLeft);
            countdown.color = model.MinutesLeft <= 12 * 60 ? CBad : model.MinutesLeft <= 24 * 60 ? CWarn : CAccent;
            SetBar("o2", model.Oxygen); SetBar("pwr", model.Power); SetBar("prop", model.Propellant);
            SetBar("hun", model.Hunger); SetBar("fat", model.Fatigue);

            if (shownRoom != model.CurrentRoom) { shownRoom = model.CurrentRoom; }
            if (lastRoom != model.CurrentRoom)
            {
                // arrive at the door that leads back to where we came from
                var back = GameModel.Hotspots.Find(x => x.Room == model.CurrentRoom && x.DoorTo == lastRoom);
                var bsp = back != null ? Stage25D.Find(model.CurrentRoom, back.Id) : null;
                astro.Shape = Stage25D.Shape(model.CurrentRoom);
                astro.Place(bsp != null ? bsp.Arrive : new Vector2(0.5f, 0.3f));
                lastRoom = model.CurrentRoom;
            }
            BuildRoom();
            RefreshMap();
            string hint = model.NextHint();
            if (hint != lastHint) { lastHint = hint; hintSince = Time.unscaledTime; }
            nextText.text = "NEXT ▸ " + hint;
            rationsText.text = "RATIONS " + model.Rations;
            RefreshInventory();
            RefreshAlerts();

            if (model.IsOver) ShowEnding();
            else if (openHotspot != null && modalRoot != null) ShowPanel(openHotspot, null);
        }

        void RefreshInventory()
        {
            for (int i = 0; i < invSlots.Count; i++)
            {
                var b = invSlots[i];
                var t = b.GetComponentInChildren<Text>();
                var img = b.GetComponent<Image>();
                if (i < model.Inventory.Count)
                {
                    var it = model.Inventory[i];
                    t.text = GameModel.ItemName(it);
                    var art = ArtSlots.Get(ArtSlots.ItemKey(it));
                    invIcons[i].sprite = art;
                    invIcons[i].preserveAspect = art != null;
                    invIcons[i].color = art != null ? Color.white : ItemColor(it);
                    bool sel = model.Selected == it;
                    img.color = sel ? new Color(0.15f, 0.55f, 0.85f) : CButton;
                    b.interactable = true;
                }
                else { t.text = ""; invIcons[i].sprite = null; invIcons[i].color = Color.clear; img.color = new Color(0.14f, 0.16f, 0.2f); b.interactable = false; }
            }
            if (model.Selected.HasValue)
            {
                selectedLabel.text = "SELECTED: " + GameModel.ItemName(model.Selected.Value);
                invHint.text = "คลิกวัตถุในห้องเพื่อใช้ไอเทม • คลิกไอเทมอีกครั้งเพื่อยกเลิก";
            }
            else
            {
                selectedLabel.text = "";
                invHint.text = "คลิกไอเทมเพื่อเลือก แล้วคลิกวัตถุเพื่อใช้ • การสำรวจ/เดิน/อ่าน ไม่เสียเวลา";
            }
        }

        void RefreshAlerts()
        {
            if (model.Alerts.Count == shownAlerts && alertList.childCount > 0) return;
            // banner for new important alerts
            for (int i = Mathf.Max(0, Mathf.Min(shownAlerts, model.Alerts.Count)); i < model.Alerts.Count; i++)
            {
                var al = model.Alerts[i];
                if (al.Banner) ShowBanner(al);
                if (shownAlerts == 0 && i < 3) continue; // no sounds for the intro alerts
                if (al.Text.StartsWith("MICROMETEOROID")) Sfx.Play("impact", 0.9f);
                if (al.Level == AlertLevel.Critical) Sfx.Alarm();
                else if (al.Level == AlertLevel.Warning) Sfx.Play(al.Banner ? "ui_error" : "warn", 0.7f);
                else if (al.Level == AlertLevel.Good && al.Text.StartsWith("ได้รับ")) Sfx.Play("pickup", 0.8f);
                else if (al.Level == AlertLevel.Good && al.Banner) Sfx.Play("success", 0.8f);
            }
            shownAlerts = model.Alerts.Count;

            for (int i = alertList.childCount - 1; i >= 0; i--) Kill(alertList.GetChild(i).gameObject);
            int shown = 0;
            const int max = 3;
            toastAt = Time.unscaledTime;
            for (int i = model.Alerts.Count - 1; i >= 0 && shown < max; i--, shown++)
            {
                var a = model.Alerts[i];
                Color c = a.Level == AlertLevel.Critical ? CBad : a.Level == AlertLevel.Warning ? CWarn : a.Level == AlertLevel.Good ? CGood : CText;
                float y1 = 1 - shown / (float)max, y0 = 1 - (shown + 1) / (float)max;
                var row = Box(alertList, "Alert", 0.02f, y0 + 0.004f, 0.98f, y1 - 0.004f, new Color(0.03f, 0.05f, 0.08f, shown == 0 ? 0.92f : 0.8f));
                var t = Label(row, "<color=#" + Hex(CDim) + ">T-" + GameModel.FormatTime(a.MinutesLeft).Substring(0, 5) + "</color>  " + a.Text, 15, TextAnchor.MiddleLeft, c);
                t.resizeTextForBestFit = true; t.resizeTextMinSize = 11; t.resizeTextMaxSize = 16;
            }
        }

        void ShowBanner(Alert a)
        {
            bannerText.text = a.Text;
            var img = bannerRoot.GetComponent<Image>();
            img.color = a.Level == AlertLevel.Good ? new Color(0.05f, 0.35f, 0.15f, 0.95f) : a.Level == AlertLevel.Critical ? new Color(0.45f, 0.05f, 0.05f, 0.95f) : new Color(0.40f, 0.28f, 0.03f, 0.95f);
            bannerRoot.SetActive(true);
            bannerRoot.transform.SetAsLastSibling();
            if (modalRoot != null) modalRoot.transform.SetAsLastSibling();
            bannerUntil = Time.unscaledTime + 4.5f;
        }

        void Update()
        {
            if (bannerRoot.activeSelf)
            {
                float left = bannerUntil - Time.unscaledTime;
                if (left <= 0) bannerRoot.SetActive(false);
                else bannerText.color = Color.Lerp(Color.white, new Color(1, 0.85f, 0.85f), Mathf.PingPong(Time.unscaledTime * 3, 1));
            }
            if (model.MinutesLeft <= 12 * 60 && !model.IsOver)
                countdown.color = Color.Lerp(CBad, Color.white, Mathf.PingPong(Time.unscaledTime * 2, 1) * 0.5f);
            UpdateActor();
        }

        // ================= room =================
        static Color RoomColor(Room r)
        {
            switch (r)
            {
                case Room.Quarters: return new Color(0.17f, 0.18f, 0.24f);
                case Room.Command: return new Color(0.12f, 0.17f, 0.25f);
                case Room.LifeSupport: return new Color(0.11f, 0.21f, 0.20f);
                case Room.Cabin: return new Color(0.22f, 0.17f, 0.22f);
                case Room.CommsRoom: return new Color(0.09f, 0.13f, 0.20f);
                case Room.Crawlspace: return new Color(0.08f, 0.15f, 0.13f);
                case Room.PartsStorage: return new Color(0.17f, 0.13f, 0.10f);
                default: return new Color(0.22f, 0.16f, 0.13f);
            }
        }

        // ================= 2.5D room =================
        static Color Mul(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, 1f); }

        void BuildRoom()
        {
            for (int i = roomContent.childCount - 1; i >= 0; i--) Kill(roomContent.GetChild(i).gameObject);
            for (int i = stage.childCount - 1; i >= 0; i--) if (stage.GetChild(i) != astro.transform) Kill(stage.GetChild(i).gameObject);
            for (int i = marks.childCount - 1; i >= 0; i--) Kill(marks.GetChild(i).gameObject);
            hotspotButtons.Clear(); blinkers.Clear(); stars.Clear(); strip = null; near = null; nearDoor = null; hovered = null;
            sortNodes.Clear(); lastSortOrder = null;
            var room = model.CurrentRoom;
            shape = Stage25D.Shape(room);
            var spots = Stage25D.Spots(room);
            astro.Shape = shape;
            floorPath.Build(spots);
            astro.IsFree = floorPath.IsFree;
            var roomRoot = roomContent;
            // click anywhere on the floor (not on an object) = walk there; double-click = arrive instantly
            var catcher = Box(roomRoot, "ClickCatcher", 0, 0, 1, 1, new Color(0, 0, 0, 0));
            catcher.gameObject.AddComponent<FloorClick>().OnClickPoint = OnFloorClick;
            var baseCol = RoomColor(room);
            roomView.GetComponent<Image>().color = Mul(baseCol, 0.6f);
            roomTitle.text = GameModel.RoomName(room);

            float FF = shape.FloorFront, FB = shape.FloorBack, WT = shape.WallTop, L1 = shape.Left(1), R1 = shape.Right(1);
            var roomArt = ArtSlots.Get(ArtSlots.RoomKey(room));
            if (roomArt != null)
            {
                var bg = Box(roomRoot, "RoomArt", 0, 0, 1, 1, Color.white).GetComponent<Image>();
                bg.sprite = roomArt;
            }
            else
            {
                // graybox box-room in perspective: ceiling, side walls, back wall, floor with depth lines
                UIQuad.Make(roomRoot, "Ceiling", Mul(baseCol, 0.45f), new Vector2(L1, WT), new Vector2(R1, WT), new Vector2(1, 1), new Vector2(0, 1));
                UIQuad.Make(roomRoot, "WallL", Mul(baseCol, 0.85f), new Vector2(0, FF), new Vector2(L1, FB), new Vector2(L1, WT), new Vector2(0, 1));
                UIQuad.Make(roomRoot, "WallR", Mul(baseCol, 0.80f), new Vector2(R1, FB), new Vector2(1, FF), new Vector2(1, 1), new Vector2(R1, WT));
                UIQuad.Make(roomRoot, "WallBack", Mul(baseCol, 1.15f), new Vector2(L1, FB), new Vector2(R1, FB), new Vector2(R1, WT), new Vector2(L1, WT));
                UIQuad.Make(roomRoot, "Floor", Mul(baseCol, 0.55f), new Vector2(0, FF), new Vector2(1, FF), new Vector2(R1, FB), new Vector2(L1, FB));
                Box(roomRoot, "FrontLip", 0, 0, 1, FF, Mul(baseCol, 0.3f));
                var line = new Color(1, 1, 1, 0.06f);
                for (int k = 1; k < 4; k++)
                {
                    float d = k / 4f, y = shape.ScreenY(d);
                    UIQuad.Make(roomRoot, "DepthLine", line, new Vector2(shape.Left(d), y - 0.0015f), new Vector2(shape.Right(d), y - 0.0015f), new Vector2(shape.Right(d), y + 0.0015f), new Vector2(shape.Left(d), y + 0.0015f));
                }
                for (int k = 1; k < 6; k++)
                {
                    float x = k / 6f, xf = shape.ScreenX(x, 0), xb = shape.ScreenX(x, 1);
                    UIQuad.Make(roomRoot, "FloorSeam", line, new Vector2(xf - 0.001f, FF), new Vector2(xf + 0.001f, FF), new Vector2(xb + 0.0007f, FB), new Vector2(xb - 0.0007f, FB));
                }
                // edges where walls meet (reads as a 3D box)
                var edge = new Color(0, 0, 0, 0.35f);
                UIQuad.Make(roomRoot, "EdgeL", edge, new Vector2(L1 - 0.0015f, FB), new Vector2(L1 + 0.0015f, FB), new Vector2(L1 + 0.0015f, WT), new Vector2(L1 - 0.0015f, WT));
                UIQuad.Make(roomRoot, "EdgeR", edge, new Vector2(R1 - 0.0015f, FB), new Vector2(R1 + 0.0015f, FB), new Vector2(R1 + 0.0015f, WT), new Vector2(R1 - 0.0015f, WT));
                Box(roomRoot, "Skirting", L1, FB, R1, FB + 0.006f, new Color(0, 0, 0, 0.3f));
                // back wall panels
                for (int k = 1; k < 4; k++) { float x = Mathf.Lerp(L1, R1, k / 4f); Box(roomRoot, "Seam", x - 0.001f, FB, x + 0.001f, WT, new Color(0, 0, 0, 0.15f)); }
                if (shape.Inner)
                {
                    // inner rooms: pipes under the low ceiling
                    Box(roomRoot, "Pipe", L1, WT - 0.03f, R1, WT - 0.018f, new Color(0.6f, 0.65f, 0.7f, 0.28f));
                    Box(roomRoot, "Pipe2", L1, WT - 0.05f, R1, WT - 0.044f, new Color(0.6f, 0.65f, 0.7f, 0.15f));
                }
            }
            if (room == Room.Command && roomArt == null)
            {
                var win = Box(roomRoot, "Window", shape.ScreenX(0.30f, 1), WT - 0.17f, shape.ScreenX(0.70f, 1), WT - 0.03f, new Color(0.02f, 0.03f, 0.08f));
                win.gameObject.AddComponent<RectMask2D>();
                win.gameObject.AddComponent<Outline>().effectColor = new Color(0.6f, 0.7f, 0.8f, 0.4f);
                for (int s = 0; s < 18; s++)
                {
                    float sx = (s * 37 % 100) / 100f, sy = (s * 53 % 90) / 100f, sz = s % 3 == 0 ? 0.012f : 0.008f;
                    var star = Box(win, "Star", sx, sy, sx + sz, sy + sz * 4, new Color(1, 1, 1, s % 3 == 0 ? 0.9f : 0.55f));
                    stars.Add(star);
                }
            }
            if (room == Room.Engineering || room == Room.LifeSupport)
            {
                // red emergency light strip along the top of the back wall while the system is broken
                bool broken = room == Room.Engineering ? !model.PowerFixed : !model.OxygenFixed;
                strip = Box(roomRoot, "Strip", L1, WT - 0.012f, R1, WT - 0.004f, broken ? new Color(1, 0.2f, 0.2f, 0.6f) : new Color(0.3f, 1, 0.5f, 0.4f)).GetComponent<Image>();
                stripBroken = broken;
            }
            // indicator lights on the back wall (every room)
            for (int i = 0; i < 5; i++) AddBlinker(roomRoot, Mathf.Lerp(L1, R1, 0.62f + i * 0.07f), WT - 0.035f, room == Room.Engineering ? CWarn : CAccent, 0.006f, 0.01f);

            // objects, doors, props (depth-sorted on the stage)
            foreach (var sp in spots)
            {
                if (sp.Kind == SpotKind.Decor) { MakeDecor(sp); continue; }
                var h = FindHotspot(sp.Id);
                if (h != null && h.Room == room) MakeHotspot(h, sp);
            }
            SortStage(true);
            // decor must not swallow clicks meant for the floor catcher
            foreach (var img in roomContent.GetComponentsInChildren<Graphic>())
                if (img.gameObject != catcher.gameObject) img.raycastTarget = false;
            foreach (var img in stage.GetComponentsInChildren<Graphic>(true))
                if (img.GetComponent<Button>() == null) img.raycastTarget = false;
        }

        RectTransform NewNode(string name, float sortKey)
        {
            var node = Box(stage, name, 0, 0, 1, 1);
            sortNodes.Add(new KeyValuePair<float, Transform>(sortKey, node));
            return node;
        }

        /// Top face of a piece of furniture (gives graybox boxes their 3D look).
        void AddTop(Transform node, Spot sp, Color c)
        {
            float d0 = sp.D, d1 = Mathf.Min(1, sp.D + sp.Dep);
            float y0 = shape.ScreenY(d0) + (sp.Lift + sp.H) * shape.Scale(d0), y1 = shape.ScreenY(d1) + (sp.Lift + sp.H) * shape.Scale(d1);
            UIQuad.Make(node, "Top", c,
                new Vector2(shape.ScreenX(sp.X - sp.W / 2, d0), y0), new Vector2(shape.ScreenX(sp.X + sp.W / 2, d0), y0),
                new Vector2(shape.ScreenX(sp.X + sp.W / 2, d1), y1), new Vector2(shape.ScreenX(sp.X - sp.W / 2, d1), y1));
            // side faces (only the one facing the centre is visible)
            bool leftSide = sp.X > 0.5f;
            float xs = leftSide ? sp.X - sp.W / 2 : sp.X + sp.W / 2;
            float yb0 = shape.ScreenY(d0) + sp.Lift * shape.Scale(d0), yb1 = shape.ScreenY(d1) + sp.Lift * shape.Scale(d1);
            UIQuad.Make(node, "Side", Mul(c, 0.7f),
                new Vector2(shape.ScreenX(xs, d0), yb0), new Vector2(shape.ScreenX(xs, d1), yb1),
                new Vector2(shape.ScreenX(xs, d1), y1), new Vector2(shape.ScreenX(xs, d0), y0));
        }

        /// Non-interactive prop (sofa, table, crates): blocks walking, sorts with the astronaut.
        void MakeDecor(Spot sp)
        {
            var node = NewNode("Deco_" + sp.Id, Stage25D.SortDepth(sp));
            var r = Stage25D.FaceRect(shape, sp);
            var art = ArtSlots.Get(sp.Id);
            var col = Mul(RoomColor(model.CurrentRoom), 1.6f);
            if (art == null) AddTop(node, sp, Mul(col, 1.15f));
            var face = Box(node, "Face", r.xMin, r.yMin, r.xMax, r.yMax, art != null ? Color.white : col).GetComponent<Image>();
            if (art != null) { face.sprite = art; face.preserveAspect = true; }
            else
            {
                var lt = Label(face.transform, sp.Name, 13, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.45f), pad: 2);
                lt.resizeTextForBestFit = true; lt.resizeTextMinSize = 8; lt.resizeTextMaxSize = 14;
            }
        }

        void MakeHotspot(Hotspot h, Spot sp)
        {
            Color col; string tag = "";
            bool isDoor = h.DoorTo.HasValue;
            if (isDoor)
            {
                bool locked = h.DoorTo == Room.Engineering && model.EngLocked;
                bool hatch = GameModel.IsInner(h.DoorTo.Value);
                col = locked ? new Color(0.45f, 0.1f, 0.1f) : hatch ? new Color(0.14f, 0.34f, 0.36f) : sp.Kind == SpotKind.Exit ? new Color(0.16f, 0.30f, 0.36f) : new Color(0.25f, 0.30f, 0.38f);
                if (locked) tag = "\n<size=12>LOCKED</size>";
            }
            else
            {
                StatusOf(h.Id, out col, out tag);
            }
            var node = NewNode("Spot_" + h.Id, Stage25D.SortDepth(sp));
            Button b;
            if (sp.Kind == SpotKind.DoorLeft || sp.Kind == SpotKind.DoorRight || sp.Kind == SpotKind.Exit)
                b = QuadButton(node, DoorCorners(sp), col, ShortDoorLabel(h, sp) + tag, () => OnHotspot(h));
            else
            {
                if (sp.Kind == SpotKind.Floor) AddTop(node, sp, Color.Lerp(col, Color.white, 0.18f));
                var r = Stage25D.FaceRect(shape, sp);
                b = Btn(node, h.Name + tag, r.xMin, r.yMin, r.xMax, r.yMax, col, () => OnHotspot(h), 18);
                var t = b.GetComponentInChildren<Text>();
                t.resizeTextForBestFit = true; t.resizeTextMinSize = 9; t.resizeTextMaxSize = 18;
                var art = ArtSlots.Get(ArtSlots.HotspotKey(h));
                if (art != null)
                {
                    // friend's art: show the picture, keep a small name/status strip at the bottom
                    var img = b.GetComponent<Image>();
                    img.sprite = art; img.preserveAspect = true;
                    img.color = Color.Lerp(Color.white, col, 0.25f);
                    var tr = (RectTransform)t.transform;
                    tr.anchorMin = new Vector2(0, 0); tr.anchorMax = new Vector2(1, 0.3f);
                    t.alignment = TextAnchor.LowerCenter; t.resizeTextMaxSize = 15;
                    if (t.GetComponent<Outline>() == null) t.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.9f);
                }
            }
            if (!isDoor) AddMarker(h);
            hotspotButtons[h.Id] = b;
            var hr = b.gameObject.AddComponent<HoverReport>();
            hr.OnHover = on =>
            {
                if (on) hovered = h; else if (hovered == h) hovered = null;
                hoverText.text = on && CanControl() ? HoverSentence(h) : "";
            };
            if (model.Selected.HasValue && !isDoor)
                b.GetComponent<Outline>().effectColor = new Color(0.3f, 0.75f, 1f, 0.8f);
        }

        static string ShortDoorLabel(Hotspot h, Spot sp)
        {
            if (sp.Kind == SpotKind.Exit) return "▼ BACK";
            return sp.Kind == SpotKind.DoorLeft ? "◀" : "▶";
        }

        /// Door outline: on the side wall (corridor doors) or a mat on the front floor edge (inner-room exit).
        Vector2[] DoorCorners(Spot sp)
        {
            if (sp.Kind == SpotKind.Exit)
            {
                float x0 = sp.X - sp.W / 2, x1 = sp.X + sp.W / 2, d1 = 0.09f;
                return new[] { shape.Project(x0, 0), shape.Project(x1, 0), shape.Project(x1, d1), shape.Project(x0, d1) };
            }
            float da = 0.18f, db = 0.52f, h = 0.40f;
            bool left = sp.Kind == SpotKind.DoorLeft;
            float xa = left ? shape.Left(da) : shape.Right(da), xb = left ? shape.Left(db) : shape.Right(db);
            float ya = shape.ScreenY(da), yb = shape.ScreenY(db);
            var a = new Vector2(xa, ya); var b = new Vector2(xb, yb);
            var c = new Vector2(xb, yb + h * shape.Scale(db)); var d = new Vector2(xa, ya + h * shape.Scale(da));
            return left ? new[] { a, b, c, d } : new[] { b, a, d, c };
        }

        /// Button whose shape is a 4-corner polygon (clicks only inside the polygon).
        Button QuadButton(Transform parent, Vector2[] c, Color col, string text, Action onClick)
        {
            var q = UIQuad.Make(parent, "Door", col, c[0], c[1], c[2], c[3]);
            q.raycastTarget = true;
            var b = q.gameObject.AddComponent<Button>();
            b.targetGraphic = q;
            var cb = b.colors;
            cb.normalColor = new Color(0.85f, 0.85f, 0.85f); cb.highlightedColor = Color.white;
            cb.selectedColor = new Color(0.85f, 0.85f, 0.85f); cb.pressedColor = new Color(0.65f, 0.65f, 0.65f);
            b.colors = cb;
            q.gameObject.AddComponent<Outline>().effectColor = new Color(1, 1, 1, 0.15f);
            float x0 = Mathf.Min(c[0].x, c[3].x, c[1].x, c[2].x), x1 = Mathf.Max(c[0].x, c[3].x, c[1].x, c[2].x);
            float y0 = Mathf.Min(c[0].y, c[1].y, c[2].y, c[3].y), y1 = Mathf.Max(c[0].y, c[1].y, c[2].y, c[3].y);
            var lr = Box(parent, "DoorLabel", x0 - 0.02f, y0, x1 + 0.02f, y1);
            var t = Label(lr, text, 18, TextAnchor.MiddleCenter, CText, bold: true, pad: 0);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 9; t.resizeTextMaxSize = 20;
            t.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            b.onClick.AddListener(() => Sfx.Play("ui_click", 0.45f));
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        /// Draw order: far things first, so the astronaut can walk behind / in front of furniture.
        void SortStage(bool force)
        {
            var list = new List<KeyValuePair<float, Transform>>(sortNodes);
            list.Add(new KeyValuePair<float, Transform>(astro.D + 0.0001f, astro.transform));
            list.Sort((p, q) => q.Key.CompareTo(p.Key));
            var order = new List<Transform>();
            foreach (var kv in list) if (kv.Value != null) order.Add(kv.Value);
            bool same = !force && lastSortOrder != null && lastSortOrder.Count == order.Count;
            if (same) for (int i = 0; i < order.Count; i++) if (lastSortOrder[i] != order[i]) { same = false; break; }
            if (same) return;
            for (int i = 0; i < order.Count; i++) order[i].SetSiblingIndex(i);
            lastSortOrder = order;
        }

        Spot SpotOf(Hotspot h) { return Stage25D.Find(h.Room, h.Id); }

        /// Screen rect of a hotspot inside the room view (markers, guide arrow, zoom start).
        Rect ViewRect(Hotspot h)
        {
            var sp = SpotOf(h);
            if (sp == null) return new Rect(h.X, h.Y, h.W, h.H);
            var sh = Stage25D.Shape(h.Room);
            if (sp.Kind == SpotKind.DoorLeft || sp.Kind == SpotKind.DoorRight || sp.Kind == SpotKind.Exit)
            {
                var keep = shape; shape = sh;
                var c = DoorCorners(sp);
                shape = keep;
                float x0 = Mathf.Min(c[0].x, c[1].x, c[2].x, c[3].x), x1 = Mathf.Max(c[0].x, c[1].x, c[2].x, c[3].x);
                float y0 = Mathf.Min(c[0].y, c[1].y, c[2].y, c[3].y), y1 = Mathf.Max(c[0].y, c[1].y, c[2].y, c[3].y);
                return new Rect(x0, y0, x1 - x0, y1 - y0);
            }
            var r = Stage25D.FaceRect(sh, sp);
            if (sp.Kind == SpotKind.Floor) r.yMax += (sh.ScreenY(Mathf.Min(1, sp.D + sp.Dep)) - sh.ScreenY(sp.D)); // include the top face
            return r;
        }

        Vector2 StandOf(Hotspot h) { var sp = SpotOf(h); return sp != null ? sp.Stand : new Vector2(0.5f, 0.5f); }

        void StatusOf(string id, out Color col, out string tag)
        {
            Color neutral = new Color(0.32f, 0.35f, 0.40f);
            Color broken = new Color(0.50f, 0.20f, 0.18f);
            Color fixedC = new Color(0.18f, 0.42f, 0.25f);
            bool? state = StateOf(id); // null neutral, false broken, true fixed
            col = state == null ? neutral : state.Value ? fixedC : broken;
            string need = model.ItemHint(id);
            tag = need != null ? "\n<size=13><color=#FFD27F>needs " + need + "</color></size>" : "";
            if (id == "console" && model.FinalPrepped) { col = new Color(0.15f, 0.35f, 0.55f); tag = "\n<size=13>ROUTE SELECT</size>"; }
        }

        bool? StateOf(string id)
        {
            bool? state = null;
            switch (id)
            {
                case "scrubber": state = model.OxygenFixed; break;
                case "pdu": state = model.PowerFixed; break;
                case "propline": state = model.LeakSealed; break;
                case "cable": state = model.CableSpliced; break;
                case "jump": state = model.JumpStabilized; break;
                case "nav": state = model.NavReady; break;
                case "locker": state = model.LockerOpened ? (bool?)true : null; break;
                case "maint": state = model.CabinetOpened ? (bool?)true : null; break;
                case "storage": state = model.StorageOpened ? (bool?)true : null; break;
                case "comms": state = model.CommsOpened ? (bool?)true : null; break;
                case "security": state = model.EngLocked ? (bool?)false : (model.EngOverridden ? (bool?)true : null); break;
            }
            return state;
        }

        /// This War of Mine-style marker over an object: ? = not looked at yet, ! = needs work, ✓ = done.
        void AddMarker(Hotspot h)
        {
            string glyph; Color c;
            bool? st = StateOf(h.Id);
            if (!inspected.Contains(h.Id)) { glyph = "?"; c = CWarn; }
            else if (st == false || model.ItemHint(h.Id) != null) { glyph = "!"; c = CBad; }
            else if (st == true) { glyph = "✓"; c = CGood; }
            else return;
            var vr = ViewRect(h);
            var msp = SpotOf(h);
            if (msp != null && msp.Kind == SpotKind.Floor) vr = Stage25D.FaceRect(shape, msp); // sit on the furniture, not above its top face
            float cx = vr.center.x, y = vr.yMax + 0.006f;
            var m = Box(marks, "Marker", cx - 0.011f, y, cx + 0.011f, y + 0.048f, new Color(0.03f, 0.05f, 0.08f, 0.9f));
            m.gameObject.AddComponent<Outline>().effectColor = c;
            Label(m, glyph, 20, TextAnchor.MiddleCenter, c, bold: true, pad: 0);
        }

        // ================= interactions =================
        void OnInvSlot(int i)
        {
            if (i >= model.Inventory.Count) return;
            Sfx.Play("ui_select", 0.7f);
            model.SelectItem(model.Inventory[i]);
        }

        bool CanControl() { return modalRoot == null && !model.IsOver && !transitioning; }

        /// Clicking an object/door: walk there first, interact on arrival.
        void OnHotspot(Hotspot h)
        {
            if (!CanControl()) return;
            bool dbl = lastClickId == h.Id && Time.unscaledTime - lastClickAt < DoubleClick;
            lastClickId = h.Id; lastClickAt = Time.unscaledTime;
            if (near == h || nearDoor == h) { astro.Cancel(); Interact(h); return; }
            var stand = StandOf(h);
            if (dbl) { astro.Place(floorPath.NearestFree(stand)); Interact(h); return; } // double-click: skip the walk
            astro.WalkPath(floorPath.Find(astro.Pos, stand), () => { if (CanControl()) Interact(h); });
        }

        /// Click on the floor (normalised point in the room view): walk there, around furniture.
        void OnFloorClick(Vector2 screen)
        {
            if (!CanControl()) return;
            bool dbl = Time.unscaledTime - lastFloorClickAt < DoubleClick;
            lastFloorClickAt = Time.unscaledTime;
            var p = shape.Unproject(screen);
            if (dbl) astro.Place(floorPath.NearestFree(p)); else astro.WalkPath(floorPath.Find(astro.Pos, p), null);
        }

        /// Test hooks: simulate single / double clicks.
        public void ClickHotspot(string id) { var h = FindHotspot(id); if (h != null) OnHotspot(h); }
        public void ClickFloor(float x, float d) { OnFloorClick(shape.Project(x, d)); }

        void Interact(Hotspot h)
        {
            if (model.IsOver) return;
            if (h.DoorTo.HasValue)
            {
                if (h.DoorTo == Room.Engineering && model.EngLocked)
                {
                    astro.Bump();
                    Sfx.Play("bump", 0.9f);
                    model.Push(model.TryEnter(Room.Engineering), AlertLevel.Critical, true);
                    Refresh();
                    return;
                }
                StartCoroutine(DoorTransition(h.DoorTo.Value));
                return;
            }
            if (model.Selected.HasValue)
            {
                var item = model.Selected.Value;
                var r = model.UseItem(h.Id, item);
                if (r.Error != null) { model.Push(r.Error, AlertLevel.Warning, true); Refresh(); return; }
                RunAction(r.Action, h.Id);
                return;
            }
            zoomFrom = h;
            ShowPanel(h.Id, null);
        }

        void RunAction(GameAction a, string hotspotId)
        {
            if (a.Blocker != null) { model.Push(a.Blocker, AlertLevel.Warning, true); Refresh(); return; }
            if (model.CostOf(a) <= 0)
            {
                string logText = null;
                if (a.Id.StartsWith("log")) logText = GameModel.CrewLogs[int.Parse(a.Id.Substring(3))];
                if (a.FromItem) model.ClearSelection();
                model.Perform(a);
                ShowPanel(hotspotId, logText);
                return;
            }
            ShowConfirm(a, hotspotId);
        }

        System.Collections.IEnumerator DoorTransition(Room to)
        {
            transitioning = true;
            astro.Cancel();
            Sfx.Play("door_open", 0.7f);
            for (float t = 0; t < 0.15f; t += Time.unscaledDeltaTime) { fade.color = new Color(0, 0, 0, t / 0.15f); yield return null; }
            fade.color = Color.black;
            string err = model.TryEnter(to); // free: no time cost
            if (err != null) { model.Push(err, AlertLevel.Critical, true); Refresh(); }
            yield return null;
            for (float t = 0; t < 0.15f; t += Time.unscaledDeltaTime) { fade.color = new Color(0, 0, 0, 1 - t / 0.15f); yield return null; }
            fade.color = new Color(0, 0, 0, 0);
            Sfx.Play("door_close", 0.5f);
            transitioning = false;
        }

        void AddBlinker(Transform parent, float x, float y, Color c, float w = 0.008f, float hgt = 0.014f)
        {
            var img = Box(parent, "Light", x, y, x + w, y + hgt, c).GetComponent<Image>();
            img.raycastTarget = false;
            blinkers.Add(img);
        }

        Vector2 ReadAxis()
        {
            Vector2 a = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (k != null)
            {
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) a.x -= 1;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) a.x += 1;
                if (k.wKey.isPressed || k.upArrowKey.isPressed) a.y += 1;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) a.y -= 1;
            }
#else
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) a.x -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) a.x += 1;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) a.y += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) a.y -= 1;
#endif
            a += DebugMove; a.x += DebugAxis;
            return new Vector2(Mathf.Clamp(a.x, -1, 1), Mathf.Clamp(a.y, -1, 1));
        }

        bool InteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = UnityEngine.InputSystem.Keyboard.current;
            return k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space);
#endif
        }

        bool DoorPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = UnityEngine.InputSystem.Keyboard.current;
            return k != null && (k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
#endif
        }

        /// S / Down key (and test hook): go through the door in range.
        public bool EnterNearestDoor()
        {
            if (!CanControl() || nearDoor == null) return false;
            astro.Cancel();
            Interact(nearDoor);
            return true;
        }
        public string NearDoorId { get { return nearDoor != null ? nearDoor.Id : null; } }

        /// E key (and test hook): interact with the hotspot in range.
        public bool InteractNearest()
        {
            if (!CanControl() || near == null) return false;
            astro.Cancel();
            Interact(near);
            return true;
        }

        Hotspot FindNear(bool doors)
        {
            Hotspot best = null; float bestD = doors ? 0.05f : 0.07f; // reach on the floor
            foreach (var sp in Stage25D.Spots(model.CurrentRoom))
            {
                if (sp.Kind == SpotKind.Decor) continue;
                var h = FindHotspot(sp.Id);
                if (h == null || h.DoorTo.HasValue != doors) continue;
                float d = sp.DistanceTo(astro.Pos);
                if (d <= bestD) { bestD = d; best = h; }
            }
            return best;
        }

        /// Walking into a door (A at the left wall, D at the right wall, W into a hatch, S out of an inner room) uses it.
        void CheckWalkIntoDoor()
        {
            var push = astro.Push;
            if (push == Vector2.zero || Time.unscaledTime < doorCooldown) return;
            foreach (var sp in Stage25D.Spots(model.CurrentRoom))
            {
                bool hit = false;
                switch (sp.Kind)
                {
                    case SpotKind.DoorLeft: hit = push.x < 0 && astro.D >= 0.18f && astro.D <= 0.52f; break;
                    case SpotKind.DoorRight: hit = push.x > 0 && astro.D >= 0.18f && astro.D <= 0.52f; break;
                    case SpotKind.Hatch: hit = push.y > 0 && Mathf.Abs(astro.X - sp.X) <= sp.W / 2 + 0.01f; break;
                    case SpotKind.Exit: hit = push.y < 0 && Mathf.Abs(astro.X - sp.X) <= sp.W / 2 + 0.01f; break;
                }
                if (!hit) continue;
                var h = FindHotspot(sp.Id);
                if (h == null) continue;
                doorCooldown = Time.unscaledTime + 0.7f;
                Interact(h);
                return;
            }
        }

        void UpdateActor()
        {
            bool control = CanControl();
            astro.ManualMove = control ? ReadAxis() : Vector2.zero;
            if (control) CheckWalkIntoDoor();
            SortStage(false);
            if (!control && modalRoot != null) astro.Cancel();
            // stop Space from re-clicking the last selected button
            if (control && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
                EventSystem.current.SetSelectedGameObject(null);

            var n = control ? FindNear(false) : null;
            if (n != near)
            {
                if (near != null) SetHighlight(near, false);
                near = n;
                if (near != null) SetHighlight(near, true);
            }
            var d = control ? FindNear(true) : null;
            if (d != nearDoor)
            {
                if (nearDoor != null) SetHighlight(nearDoor, false);
                nearDoor = d;
                if (nearDoor != null) SetHighlight(nearDoor, true);
            }
            if ((near != null || nearDoor != null) && !astro.HasTarget)
            {
                promptRt.gameObject.SetActive(true);
                var feet = shape.Project(astro.X, astro.D);
                float headY = feet.y + (128f * Astronaut.BaseScale * shape.Scale(astro.D) + 8f) / (1080f * 0.736f);
                promptRt.anchorMin = promptRt.anchorMax = new Vector2(Mathf.Clamp(feet.x, 0.12f, 0.88f), Mathf.Min(headY, 0.9f));
                promptRt.anchoredPosition = Vector2.zero;
                promptText.text = PromptFor(near ?? nearDoor);
                if (control && InteractPressed()) { if (!InteractNearest()) EnterNearestDoor(); }
            }
            else promptRt.gameObject.SetActive(false);

            // item follows the cursor while selected
            if (model.Selected.HasValue && control)
            {
                Vector2 mp;
#if ENABLE_INPUT_SYSTEM
                var mouse = UnityEngine.InputSystem.Mouse.current;
                mp = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
#else
                mp = Input.mousePosition;
#endif
                Vector2 lp;
                var crt = (RectTransform)canvas;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(crt, mp, null, out lp))
                {
                    cursorItemRt.gameObject.SetActive(true);
                    cursorItemRt.anchorMin = cursorItemRt.anchorMax = new Vector2(0.5f, 0.5f);
                    cursorItemRt.anchoredPosition = lp + new Vector2(18, -18);
                    cursorItemText.text = GameModel.ItemName(model.Selected.Value);
                    cursorItemRt.SetAsLastSibling();
                }
            }
            else cursorItemRt.gameObject.SetActive(false);
            if (!control) hoverText.text = "";
            if (EscapePressed()) OnEscape();
            UpdateCursor(control);
            UpdateGuide(control);
            // toasts dim after a while
            toastGroup.alpha = Mathf.Lerp(1f, 0.35f, Mathf.Clamp01((Time.unscaledTime - toastAt - 6f) / 2f));

            if (astro.IsWalking && Time.unscaledTime >= nextStep) { Sfx.Play("step", 0.18f, UnityEngine.Random.Range(0.9f, 1.1f)); nextStep = Time.unscaledTime + 0.32f; }

            // ambient life
            float t = Time.unscaledTime;
            for (int i = 0; i < blinkers.Count; i++)
            {
                var img = blinkers[i]; if (img == null) continue;
                var c = img.color;
                c.a = Mathf.Sin(t * (1.3f + (i % 4) * 0.7f) + i * 1.7f) > -0.2f ? 0.95f : 0.15f;
                img.color = c;
            }
            for (int i = 0; i < stars.Count; i++)
            {
                var r = stars[i]; if (r == null) continue;
                float w = r.anchorMax.x - r.anchorMin.x;
                float x = r.anchorMin.x - Time.unscaledDeltaTime * (0.01f + (i % 3) * 0.008f);
                if (x < -w) x = 1f;
                r.anchorMin = new Vector2(x, r.anchorMin.y); r.anchorMax = new Vector2(x + w, r.anchorMax.y);
            }
            if (strip != null && stripBroken)
                strip.color = new Color(1, 0.2f, 0.2f, 0.25f + 0.6f * Mathf.PingPong(t * 1.6f, 1));
        }

        // ================= smart cursor =================
        CursorKind CursorFor(Hotspot h)
        {
            if (h.DoorTo.HasValue)
            {
                if (h.DoorTo == Room.Engineering && model.EngLocked) return CursorKind.Locked;
                if (GameModel.IsInner(h.DoorTo.Value)) return CursorKind.DoorUp;
                if (GameModel.IsInner(h.Room)) return CursorKind.DoorDown;
                var dsp = SpotOf(h);
                return dsp != null && dsp.Kind == SpotKind.DoorLeft ? CursorKind.DoorLeft : CursorKind.DoorRight;
            }
            if (model.Selected.HasValue)
                return model.UseItem(h.Id, model.Selected.Value).Error == null ? CursorKind.Use : CursorKind.CantUse;
            return CursorKind.Look;
        }

        void UpdateCursor(bool control)
        {
            var k = CursorKind.Default;
            if (control && hovered != null)
            {
                k = CursorFor(hovered);
                hoverText.text = HoverSentence(hovered); // stays correct when the selected item changes
            }
            ContextCursor.Set(k);
        }

        void OnDisable() { ContextCursor.Reset(); }

        // ================= guide marker =================
        static int RoomIndex(Room r) { return (int)r; } // enum order = corridor order

        /// Next door on the way from the current room to target (inner rooms go via their main room).
        Hotspot DoorToward(Room target)
        {
            var cur = model.CurrentRoom;
            if (GameModel.IsInner(cur)) // only way out
                return GameModel.Hotspots.Find(x => x.Room == cur && x.DoorTo.HasValue);
            if (GameModel.ParentOf(target) == cur) // inner room of this room: the hatch
                return GameModel.Hotspots.Find(x => x.Room == cur && x.DoorTo == target);
            int dir = Math.Sign(RoomIndex(GameModel.ParentOf(target)) - RoomIndex(cur));
            return GameModel.Hotspots.Find(x => x.Room == cur && x.DoorTo.HasValue && !GameModel.IsInner(x.DoorTo.Value) &&
                Math.Sign(RoomIndex(x.DoorTo.Value) - RoomIndex(cur)) == dir);
        }

        /// Arrow over the next objective: always during the tutorial (until the locker is opened),
        /// afterwards only when the player has been stuck on the same NEXT hint for a while.
        void UpdateGuide(bool control)
        {
            Hotspot mark = null;
            bool idle = Time.unscaledTime - hintSince >= GuideIdleSeconds;
            if (control && guideOn && (!model.LockerOpened || idle))
            {
                var id = model.NextTargetId();
                var h = id != null ? FindHotspot(id) : null;
                if (h != null) mark = h.Room == model.CurrentRoom ? h : DoorToward(h.Room);
                if (mark != null && (mark == near || mark == nearDoor)) mark = null; // already there: the [E] prompt says it
            }
            bool was = guideRt.gameObject.activeSelf;
            GuideTarget = mark != null ? mark.Id : null;
            guideRt.gameObject.SetActive(mark != null);
            if (mark == null) return;
            if (!was && model.LockerOpened) Sfx.Play("tick", 0.6f);
            float t = Time.unscaledTime;
            var gr = ViewRect(mark);
            float gy = gr.yMax + (mark.DoorTo.HasValue ? 0.005f : 0.06f); // objects: above the ?/! marker
            guideRt.anchorMin = guideRt.anchorMax = new Vector2(gr.center.x, Mathf.Min(gy, 0.86f));
            guideRt.anchoredPosition = new Vector2(0, Mathf.Abs(Mathf.Sin(t * 4f)) * 14f);
            guideText.color = Color.Lerp(CWarn, Color.white, Mathf.PingPong(t * 2f, 1f) * 0.6f);
            guideRt.SetAsLastSibling();
        }

        // ================= pause menu (Esc) =================
        bool EscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = UnityEngine.InputSystem.Keyboard.current;
            return k != null && k.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        /// Esc: close the open window → drop the held item → open the pause menu.
        void OnEscape()
        {
            if (modalRoot != null)
            {
                if (modalRoot.name == "Ending") return;
                CloseModal(); // panels, confirm, mini-game (= abort), log, start screen, pause
                return;
            }
            if (model.IsOver || transitioning) return;
            if (model.Selected.HasValue) { model.SelectItem(model.Selected); return; }
            ShowPause();
        }

        void ShowPause()
        {
            if (model.IsOver) return;
            var p = OpenModal(0.33f, 0.14f, 0.67f, 0.86f);
            modalRoot.name = "Pause";
            Label(p, "PAUSED", 40, TextAnchor.UpperCenter, CAccent, 0, 0.88f, 1, 0.98f, true);
            Label(p, "เวลาในเกมไม่เดินเองอยู่แล้ว — พักได้ตามสบาย", 16, TextAnchor.UpperCenter, CDim, 0, 0.82f, 1, 0.88f);
            Btn(p, "RESUME", 0.1f, 0.71f, 0.9f, 0.80f, new Color(0.15f, 0.48f, 0.28f), CloseModal, 24);

            int vol = Mathf.RoundToInt(AudioListener.volume * 100);
            Label(p, "SOUND  " + (vol == 0 ? "MUTED" : vol + "%"), 20, TextAnchor.MiddleLeft, CText, 0.06f, 0.60f, 0.45f, 0.68f, true);
            Btn(p, "−", 0.45f, 0.605f, 0.57f, 0.675f, CButton, () => SetVolume(AudioListener.volume - 0.1f), 26);
            Btn(p, "+", 0.59f, 0.605f, 0.71f, 0.675f, CButton, () => SetVolume(AudioListener.volume + 0.1f), 26);
            Btn(p, vol == 0 ? "UNMUTE" : "MUTE", 0.73f, 0.605f, 0.94f, 0.675f, CButton, () => SetVolume(AudioListener.volume > 0 ? 0 : 1), 16);

            Label(p, "HINT ARROW", 20, TextAnchor.MiddleLeft, CText, 0.06f, 0.50f, 0.6f, 0.58f, true);
            Btn(p, guideOn ? "ON" : "OFF", 0.73f, 0.505f, 0.94f, 0.575f, guideOn ? new Color(0.15f, 0.48f, 0.28f) : CButton, () =>
            {
                guideOn = !guideOn;
                PlayerPrefs.SetInt("l72_guide", guideOn ? 1 : 0);
                ShowPause();
            }, 18);

            var help = Label(p,
                "<color=#FFD27F>CONTROLS</color>\n" +
                "WASD / ลูกศร  เดินได้ทุกทิศ (W ลึกเข้าไป, S ออกมา)   •   คลิกพื้น  เดินไปตรงนั้น\n" +
                "คลิกวัตถุ  เดินไปแล้วใช้   •   <b>ดับเบิลคลิก</b>  ไปถึงทันที\n" +
                "E / Space  ใช้ของ / เข้าประตูตรงหน้า   •   เดินชนประตู  ผ่านประตูเลย\n" +
                "คลิกไอเทม แล้วคลิกวัตถุ  ใช้ไอเทม   •   Esc  ปล่อยไอเทม / เมนู",
                16, TextAnchor.UpperLeft, CText, 0.06f, 0.17f, 0.94f, 0.48f);
            help.resizeTextForBestFit = true; help.resizeTextMinSize = 11; help.resizeTextMaxSize = 16;

            Btn(p, "RESTART", 0.06f, 0.04f, 0.48f, 0.13f, new Color(0.40f, 0.28f, 0.10f), ShowRestartConfirm, 20);
            Btn(p, "QUIT", 0.52f, 0.04f, 0.94f, 0.13f, new Color(0.40f, 0.18f, 0.18f), QuitGame, 20);
        }

        void SetVolume(float v)
        {
            AudioListener.volume = Mathf.Clamp01(Mathf.Round(v * 10f) / 10f);
            PlayerPrefs.SetFloat("l72_volume", AudioListener.volume);
            Sfx.Play("ui_select", 0.8f);
            ShowPause();
        }

        void ShowRestartConfirm()
        {
            var p = OpenModal(0.33f, 0.36f, 0.67f, 0.64f);
            modalRoot.name = "RestartConfirm";
            Label(p, "เริ่มภารกิจใหม่?", 32, TextAnchor.UpperCenter, CWarn, 0, 0.62f, 1, 0.92f, true);
            Label(p, "ความคืบหน้าทั้งหมดจะหายไป", 18, TextAnchor.MiddleCenter, CDim, 0, 0.4f, 1, 0.6f);
            Btn(p, "RESTART", 0.08f, 0.08f, 0.46f, 0.32f, new Color(0.40f, 0.28f, 0.10f), Restart, 22);
            Btn(p, "BACK", 0.54f, 0.08f, 0.92f, 0.32f, CButton, ShowPause, 22);
        }

        void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        string PromptFor(Hotspot h)
        {
            if (h.DoorTo.HasValue)
            {
                if (h.DoorTo == Room.Engineering && model.EngLocked) return "[E] LOCKED — ENGINEERING";
                if (GameModel.IsInner(h.DoorTo.Value)) return "[E] Enter " + GameModel.RoomName(h.DoorTo.Value) + "  (or keep walking in)";
                if (GameModel.IsInner(h.Room)) return "[E] Back to " + GameModel.RoomName(h.DoorTo.Value) + "  (or walk out)";
                return "[E] Go to " + GameModel.RoomName(h.DoorTo.Value);
            }
            if (model.Selected.HasValue)
                return model.UseItem(h.Id, model.Selected.Value).Error == null
                    ? "[E] Use " + GameModel.ItemName(model.Selected.Value) + " → " + h.Name
                    : GameModel.ItemName(model.Selected.Value) + " ใช้ที่นี่ไม่ได้ • [Esc] ปล่อยไอเทม";
            return "[E] Inspect " + h.Name;
        }

        void SetHighlight(Hotspot h, bool on)
        {
            Button b;
            if (!hotspotButtons.TryGetValue(h.Id, out b) || b == null) return;
            var o = b.GetComponent<Outline>();
            if (on) { o.effectColor = new Color(1f, 0.82f, 0.3f, 1f); o.effectDistance = new Vector2(3, -3); }
            else
            {
                o.effectColor = model.Selected.HasValue && !h.DoorTo.HasValue ? new Color(0.3f, 0.75f, 1f, 0.8f) : new Color(1, 1, 1, 0.15f);
                o.effectDistance = new Vector2(1, -1);
            }
        }

        string HoverSentence(Hotspot h)
        {
            if (h.DoorTo.HasValue)
            {
                if (h.DoorTo == Room.Engineering && model.EngLocked) return "Engineering — LOCKED";
                if (GameModel.IsInner(h.DoorTo.Value)) return "Enter " + GameModel.RoomName(h.DoorTo.Value);
                if (GameModel.IsInner(h.Room)) return "Back to " + GameModel.RoomName(h.DoorTo.Value);
                return "Walk to " + GameModel.RoomName(h.DoorTo.Value);
            }
            if (model.Selected.HasValue)
            {
                string item = GameModel.ItemName(model.Selected.Value);
                return model.UseItem(h.Id, model.Selected.Value).Error == null
                    ? "Use " + item + " with " + h.Name
                    : "<color=#FF8A80>" + item + " ใช้กับ " + h.Name + " ไม่ได้</color>";
            }
            return "Inspect " + h.Name;
        }

        static Color ItemColor(Item i)
        {
            switch (i)
            {
                case Item.Multitool: return new Color(0.75f, 0.75f, 0.8f);
                case Item.AccessCard: return new Color(0.3f, 0.75f, 1f);
                case Item.SpareFilter: return new Color(0.4f, 0.9f, 0.6f);
                case Item.Fuse: return new Color(1f, 0.8f, 0.2f);
                case Item.RepairCable: return new Color(1f, 0.45f, 0.2f);
                case Item.SealantKit: return new Color(0.85f, 0.5f, 1f);
                default: return new Color(0.4f, 1f, 1f);
            }
        }

        void RefreshMap()
        {
            foreach (var kv in mapRooms)
            {
                Room r = kv.Key;
                bool here = r == GameModel.ParentOf(model.CurrentRoom);
                bool locked = r == Room.Engineering && model.EngLocked;
                kv.Value.color = locked ? new Color(0.45f, 0.1f, 0.1f) : here ? new Color(0.18f, 0.45f, 0.70f) : CButton;
                bool broken;
                switch (r)
                {
                    case Room.LifeSupport: broken = !model.OxygenFixed; break;
                    case Room.Engineering: broken = !model.PowerFixed || !model.LeakSealed; break;
                    case Room.Command: broken = !model.NavReady; break;
                    default: broken = false; break;
                }
                mapDots[r].color = broken ? CBad : CGood;
            }
        }

        /// Ship map click: travel (free) with the same fade as doors.
        void TravelTo(Room r)
        {
            if (!CanControl() || r == model.CurrentRoom) return;
            if (r == Room.Engineering && model.EngLocked)
            {
                astro.Bump();
                model.Push(model.TryEnter(Room.Engineering), AlertLevel.Critical, true);
                Refresh();
                return;
            }
            StartCoroutine(DoorTransition(r));
        }

        void ShowLog()
        {
            var p = OpenModal(0.15f, 0.08f, 0.85f, 0.92f);
            Label(p, "MISSION LOG", 30, TextAnchor.UpperLeft, CAccent, 0.03f, 0.91f, 0.97f, 0.99f, true);
            var sb = new System.Text.StringBuilder();
            for (int i = model.Alerts.Count - 1, n = 0; i >= 0 && n < 22; i--, n++)
            {
                var a = model.Alerts[i];
                Color c = a.Level == AlertLevel.Critical ? CBad : a.Level == AlertLevel.Warning ? CWarn : a.Level == AlertLevel.Good ? CGood : CText;
                sb.Append("<color=#" + Hex(CDim) + ">T-" + GameModel.FormatTime(a.MinutesLeft).Substring(0, 5) + "</color>  <color=#" + Hex(c) + ">" + a.Text + "</color>\n");
            }
            var t = Label(p, sb.ToString(), 18, TextAnchor.UpperLeft, CText, 0.03f, 0.11f, 0.97f, 0.9f);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 11; t.resizeTextMaxSize = 18;
            Btn(p, "CLOSE", 0.78f, 0.02f, 0.97f, 0.09f, CButton, CloseModal, 20);
        }

        // ================= modals =================
        RectTransform OpenModal(float x0, float y0, float x1, float y1, bool dim = true)
        {
            CloseModal();
            modalRoot = Box(canvas, "Modal", 0, 0, 1, 1, new Color(0, 0, 0, dim ? 0.65f : 0.0f)).gameObject;
            var p = Box(modalRoot.transform, "Panel", x0, y0, x1, y1, CPanel);
            p.gameObject.AddComponent<Outline>().effectColor = new Color(0.3f, 0.75f, 1f, 0.5f);
            return p;
        }

        void CloseModal()
        {
            bool wasPanel = openHotspot != null;
            if (modalRoot != null) Kill(modalRoot);
            modalRoot = null; openHotspot = null;
            if (wasPanel) BuildRoom(); // refresh ? / ! markers after looking at something
        }

        Hotspot FindHotspot(string id) { return GameModel.Hotspots.Find(x => x.Id == id); }

        /// Close-up view of an object (Rusty Lake / Machinarium style): device on the left, text + actions on the right.
        void ShowPanel(string id, string extraText)
        {
            var h = FindHotspot(id);
            inspected.Add(id);
            var p = OpenModal(0.10f, 0.14f, 0.90f, 0.86f);
            openHotspot = id;
            Label(p, h.Name.ToUpper(), 30, TextAnchor.UpperLeft, CAccent, 0.02f, 0.88f, 0.7f, 0.98f, true);
            Label(p, "CLOSE-UP • ดูได้ฟรี ไม่เสียเวลา", 14, TextAnchor.UpperRight, CDim, 0.6f, 0.9f, 0.98f, 0.97f);

            var view = Box(p, "CloseUp", 0.015f, 0.13f, 0.37f, 0.87f, new Color(0, 0, 0, 0.45f));
            view.gameObject.AddComponent<RectMask2D>();
            BuildCloseUp(view, h);

            const float L = 0.385f;
            var actions = model.ActionsFor(id);
            float actionsTop = 0.13f + Mathf.Min(actions.Count, 4) * 0.105f;
            string body = extraText ?? model.Describe(id);
            string need = model.ItemHint(id);
            if (need != null && extraText == null)
                body += "\n\n<color=#FFD27F>ต้องใช้ไอเทม: " + need + "</color>\n<color=#" + Hex(CDim) + ">กลับออกไป → คลิก " + need + " ใน Inventory → คลิกวัตถุนี้</color>";
            var desc = Label(p, body, 22, TextAnchor.UpperLeft, CText, L, actionsTop + 0.02f, 0.985f, 0.87f);
            desc.resizeTextForBestFit = true; desc.resizeTextMinSize = 14; desc.resizeTextMaxSize = 22;

            for (int i = 0; i < actions.Count; i++)
            {
                var a = actions[i];
                int cost = model.CostOf(a);
                string costTxt = cost > 0 ? "  [" + GameModel.FormatCost(cost) + "]" : "  [FREE]";
                // two columns if many actions
                int col = actions.Count > 4 ? i % 2 : 0;
                int row = actions.Count > 4 ? i / 2 : i;
                float x0 = actions.Count > 4 ? L + col * 0.30f : L;
                float x1 = actions.Count > 4 ? x0 + 0.29f : L + 0.40f;
                float y1 = actionsTop - row * 0.105f, y0 = y1 - 0.09f;
                var b = Btn(p, a.Title + costTxt, x0, y0, x1, y1, a.BaseMinutes > 0 ? new Color(0.20f, 0.36f, 0.52f) : CButton, () => RunAction(a, id), 19);
                b.GetComponentInChildren<Text>().resizeTextForBestFit = true;
                b.GetComponentInChildren<Text>().resizeTextMinSize = 11; b.GetComponentInChildren<Text>().resizeTextMaxSize = 19;
                if (a.Blocker != null)
                {
                    b.interactable = false;
                    b.GetComponentInChildren<Text>().color = new Color(0.5f, 0.54f, 0.6f);
                    if (actions.Count <= 4)
                    {
                        var r = Label(p, a.Blocker, 14, TextAnchor.MiddleLeft, CWarn, L + 0.41f, y0, 0.985f, y1);
                        r.resizeTextForBestFit = true; r.resizeTextMinSize = 10; r.resizeTextMaxSize = 15;
                    }
                }
            }
            // Rusty Lake-style "step back" arrow at the bottom centre
            Btn(p, "▼  BACK", 0.40f, 0.015f, 0.60f, 0.105f, CButton, CloseModal, 20);
            if (zoomFrom == h) { Sfx.Play("ui_open", 0.5f); StartCoroutine(ZoomIn(p, h)); }
            zoomFrom = null;
        }

        /// Graybox close-up of a device (replaced by Resources/Art/closeup_<id>.png when present).
        void BuildCloseUp(RectTransform view, Hotspot h)
        {
            var art = ArtSlots.Get("closeup_" + h.Id);
            if (art != null)
            {
                var img = Box(view, "Art", 0, 0, 1, 1, Color.white).GetComponent<Image>();
                img.sprite = art; img.preserveAspect = true; img.raycastTarget = false;
                return;
            }
            Color col; string tag;
            StatusOf(h.Id, out col, out tag);
            bool? st = StateOf(h.Id);
            string need = model.ItemHint(h.Id);
            Box(view, "Wall", 0, 0, 1, 1, new Color(1, 1, 1, 0.03f));
            if (st == null && need == null)
            {
                // plain object (no machine state): framed placeholder until the art team adds closeup_<id>.png
                var frame = Box(view, "Frame", 0.08f, 0.14f, 0.92f, 0.86f, new Color(col.r, col.g, col.b, 0.6f));
                frame.gameObject.AddComponent<Outline>().effectColor = new Color(1, 1, 1, 0.15f);
                Label(frame, h.Name, 30, TextAnchor.MiddleCenter, CText, 0, 0.3f, 1, 0.8f, true).resizeTextForBestFit = true;
                Label(frame, "closeup_" + h.Id + ".png", 13, TextAnchor.LowerCenter, CDim, 0, 0.04f, 1, 0.25f);
                foreach (var img in view.GetComponentsInChildren<Image>()) img.raycastTarget = false;
                return;
            }
            // machine: device body with bolts, status screen, lights
            var body = Box(view, "Device", 0.08f, 0.14f, 0.92f, 0.86f, col);
            body.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.6f);
            foreach (var c in new[] { new Vector2(0.04f, 0.04f), new Vector2(0.92f, 0.04f), new Vector2(0.04f, 0.92f), new Vector2(0.92f, 0.92f) })
                Box(body, "Bolt", c.x, c.y, c.x + 0.04f, c.y + 0.04f, new Color(0.8f, 0.82f, 0.85f, 0.6f));
            // status screen
            var screen = Box(body, "Screen", 0.1f, 0.52f, 0.9f, 0.86f, new Color(0.02f, 0.05f, 0.07f));
            string status = need != null ? "NEEDS\n" + need.ToUpper() : st == false ? "FAULT" : st == true ? "OK" : "STANDBY";
            Color sc = need != null ? CWarn : st == false ? CBad : st == true ? CGood : CAccent;
            Label(screen, status, 26, TextAnchor.MiddleCenter, sc, bold: true).resizeTextForBestFit = true;
            // indicator lights + slot for the needed item
            for (int i = 0; i < 5; i++) AddBlinker(body, 0.12f + i * 0.08f, 0.40f, st == false ? CBad : st == true ? CGood : CAccent, 0.05f, 0.06f);
            if (need != null)
            {
                var slot = Box(body, "ItemSlot", 0.52f, 0.12f, 0.88f, 0.36f, new Color(0, 0, 0, 0.5f));
                slot.gameObject.AddComponent<Outline>().effectColor = CWarn;
                Label(slot, "INSERT\n" + need, 14, TextAnchor.MiddleCenter, CWarn, bold: true).resizeTextForBestFit = true;
            }
            Label(view, h.Name, 16, TextAnchor.LowerCenter, CDim, 0, 0, 1, 0.12f, true);
            foreach (var img in view.GetComponentsInChildren<Image>()) img.raycastTarget = false;
        }

        /// Zoom the panel out of the object the player clicked (0.16 s).
        System.Collections.IEnumerator ZoomIn(RectTransform panel, Hotspot h)
        {
            Vector2 endMin = panel.anchorMin, endMax = panel.anchorMax;
            var rv = roomView;
            var hr = ViewRect(h);
            Vector2 sMin = new Vector2(Mathf.Lerp(rv.anchorMin.x, rv.anchorMax.x, hr.xMin), Mathf.Lerp(rv.anchorMin.y, rv.anchorMax.y, hr.yMin));
            Vector2 sMax = new Vector2(Mathf.Lerp(rv.anchorMin.x, rv.anchorMax.x, hr.xMax), Mathf.Lerp(rv.anchorMin.y, rv.anchorMax.y, hr.yMax));
            var go = panel.gameObject;
            for (float t = 0; t < 0.16f; t += Time.unscaledDeltaTime)
            {
                if (go == null || !go.activeInHierarchy) yield break;
                float e = 1 - (1 - t / 0.16f) * (1 - t / 0.16f); // ease-out
                panel.anchorMin = Vector2.Lerp(sMin, endMin, e);
                panel.anchorMax = Vector2.Lerp(sMax, endMax, e);
                yield return null;
            }
            if (go != null) { panel.anchorMin = endMin; panel.anchorMax = endMax; }
        }

        void ShowConfirm(GameAction a, string hotspotId)
        {
            var p = OpenModal(0.28f, 0.28f, 0.72f, 0.74f);
            int cost = model.CostOf(a);
            Label(p, a.Title, 30, TextAnchor.UpperCenter, CAccent, 0.03f, 0.82f, 0.97f, 0.97f, true);
            Label(p, "Time Cost: " + GameModel.FormatCost(cost), 34, TextAnchor.MiddleCenter, CWarn, 0.03f, 0.64f, 0.97f, 0.82f, true);
            string info = a.Summary ?? "";
            if (a.IsWork && cost > a.BaseMinutes)
                info += "\n<color=#" + Hex(CBad) + ">เวลาเพิ่มขึ้นเพราะ" + (model.Fatigue >= 80 ? " เหนื่อยมาก" : "") + (model.Hunger >= 80 ? " หิวมาก" : "") + " (ปกติ " + GameModel.FormatCost(a.BaseMinutes) + ")</color>";
            info += "\n\n<color=#" + Hex(CDim) + ">เวลาที่เหลือหลังทำ: " + GameModel.FormatTime(model.MinutesLeft - cost) + "</color>";
            Label(p, info, 20, TextAnchor.UpperCenter, CText, 0.05f, 0.2f, 0.95f, 0.64f);
            bool used = false;
            Btn(p, "CONFIRM", 0.08f, 0.04f, 0.46f, 0.18f, new Color(0.15f, 0.48f, 0.28f), () =>
            {
                if (used) return; // guard against double-click re-running the action
                used = true;
                if (HasMiniGame(a.Id)) { ShowMiniGame(a, hotspotId); return; }
                CompleteAction(a, hotspotId, false);
            }, 24);
            Btn(p, "CANCEL", 0.54f, 0.04f, 0.92f, 0.18f, new Color(0.40f, 0.18f, 0.18f), CloseModal, 24);
        }

        static readonly HashSet<string> miniIds = new HashSet<string> { "o2", "power", "cable", "seal", "calibrate", "stabilize", "override" };
        static bool HasMiniGame(string id) { return miniIds.Contains(id); }

        /// Test hook: the mini-game currently on screen (or null).
        public MiniGame ActiveMiniGame { get { return modalRoot != null ? modalRoot.GetComponentInChildren<MiniGame>() : null; } }

        void CompleteAction(GameAction a, string hotspotId, bool perfect)
        {
            CloseModal();
            if (a.FromItem) model.ClearSelection();
            if (a.BaseMinutes > 0 && a.Id != "jumpgo" && a.Id != "marsgo") Sfx.Play("work", 0.5f);
            model.Perform(a);
            if (perfect && !model.IsOver) model.ApplyPerfectBonus(a.Id);
            if (!model.IsOver && hotspotId != null && a.Id != "sleep" && a.Id != "eat" && a.Id != "jumpgo" && a.Id != "marsgo")
                ShowPanel(hotspotId, null);
        }

        void ShowMiniGame(GameAction a, string hotspotId)
        {
            var p = OpenModal(0.14f, 0.1f, 0.86f, 0.9f);
            var area = Box(p, "PlayArea", 0.03f, 0.12f, 0.97f, 0.8f, new Color(0, 0, 0, 0.35f));
            var mg = MiniGame.For(a.Id, area.gameObject);
            Label(p, mg.Title, 30, TextAnchor.UpperLeft, CAccent, 0.03f, 0.9f, 0.7f, 0.98f, true);
            Label(p, a.Title + " • " + GameModel.FormatCost(model.CostOf(a)), 15, TextAnchor.UpperRight, CDim, 0.5f, 0.92f, 0.97f, 0.98f);
            var ins = Label(p, mg.Instructions, 18, TextAnchor.MiddleLeft, CWarn, 0.03f, 0.81f, 0.97f, 0.89f);
            ins.resizeTextForBestFit = true; ins.resizeTextMinSize = 12; ins.resizeTextMaxSize = 18;
            Label(p, "ทำสำเร็จ = ซ่อมตามเวลาปกติ • พลาด = เสียเวลา 30 นาที แล้วลองใหม่ • PERFECT = โบนัสทรัพยากร", 14, TextAnchor.MiddleLeft, CDim, 0.03f, 0.02f, 0.75f, 0.1f);
            Btn(p, "ABORT", 0.8f, 0.02f, 0.97f, 0.1f, new Color(0.40f, 0.18f, 0.18f), CloseModal, 18);
            mg.Begin(this, area, (ok, perfect) => StartCoroutine(AfterMiniGame(ok, perfect, a, hotspotId)));
        }

        System.Collections.IEnumerator AfterMiniGame(bool ok, bool perfect, GameAction a, string hotspotId)
        {
            Sfx.Play(ok ? "success" : "fail", ok ? 0.9f : 0.6f);
            yield return new WaitForSecondsRealtime(0.4f);
            if (ok) { CompleteAction(a, hotspotId, perfect); yield break; }
            CloseModal();
            model.ApplyMistake(30, a.Title);
            if (model.IsOver) yield break;
            var p = OpenModal(0.3f, 0.32f, 0.7f, 0.68f);
            Label(p, "REPAIR FAILED", 34, TextAnchor.UpperCenter, CBad, 0.03f, 0.72f, 0.97f, 0.95f, true);
            Label(p, a.Title + "\nเสียเวลาไป 30 นาที — ลองใหม่ได้", 22, TextAnchor.MiddleCenter, CText, 0.05f, 0.35f, 0.95f, 0.72f);
            Btn(p, "RETRY", 0.08f, 0.06f, 0.46f, 0.28f, new Color(0.15f, 0.48f, 0.28f), () => ShowMiniGame(a, hotspotId), 24);
            Btn(p, "CANCEL", 0.54f, 0.06f, 0.92f, 0.28f, new Color(0.40f, 0.18f, 0.18f), CloseModal, 24);
        }

        void ShowObjectives()
        {
            if (model.IsOver) return;
            var p = OpenModal(0.2f, 0.12f, 0.8f, 0.88f);
            Label(p, "MISSION STATUS", 30, TextAnchor.UpperLeft, CAccent, 0.03f, 0.9f, 0.97f, 0.98f, true);
            var t = Label(p, model.ObjectivesText() + "\n\n<color=#" + Hex(CDim) + ">เลือกเส้นทางได้ที่ Main Console (Command Deck)</color>", 20, TextAnchor.UpperLeft, CText, 0.03f, 0.12f, 0.97f, 0.9f);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = 21;
            Btn(p, "CLOSE", 0.78f, 0.02f, 0.97f, 0.1f, CButton, CloseModal, 20);
        }

        int handbookTab;
        void ShowHandbook()
        {
            var p = OpenModal(0.12f, 0.08f, 0.88f, 0.92f);
            Label(p, "MISSION HANDBOOK", 30, TextAnchor.UpperLeft, CAccent, 0.02f, 0.92f, 0.6f, 0.99f, true);
            string[] tabs = { "LIFE SUPPORT", "POWER", "MARS SURVIVAL" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int idx = i;
                Btn(p, tabs[i], 0.02f + i * 0.2f, 0.84f, 0.2f + i * 0.2f, 0.91f, idx == handbookTab ? new Color(0.18f, 0.40f, 0.60f) : CButton,
                    () => { handbookTab = idx; ShowHandbook(); }, 18);
            }
            var t = Label(p, Handbook.Pages[handbookTab], 20, TextAnchor.UpperLeft, CText, 0.02f, 0.1f, 0.98f, 0.83f);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = 21;
            Label(p, "อ่านคู่มือไม่เสียเวลาในเกม", 14, TextAnchor.LowerLeft, CDim, 0.02f, 0.02f, 0.6f, 0.08f);
            Btn(p, "CLOSE", 0.8f, 0.02f, 0.98f, 0.09f, CButton, CloseModal, 20);
        }

        void ShowStart()
        {
            var p = OpenModal(0.15f, 0.08f, 0.85f, 0.92f);
            Label(p, "LAST 72 HOURS", 60, TextAnchor.UpperCenter, CAccent, 0, 0.84f, 1, 0.98f, true);
            Label(p, "SURVIVE THE JOURNEY", 22, TextAnchor.UpperCenter, CDim, 0, 0.78f, 1, 0.85f);
            var t = Label(p,
                "ปี 2126 — ยาน ASTRA-01 เดินทาง Earth → Mars → Saturn Jump Zone → New Planet\n" +
                "เศษวัตถุอวกาศพุ่งชนยาน ลูกเรืออาวุโสสองคนเสียชีวิต เหลือเพียงคุณ — นักบินฝึกหัด\n\n" +
                "<color=#FFD27F>วิธีเล่น</color>\n" +
                "• เวลา <b>ไม่เดินเอง</b> — ลดเฉพาะเมื่อคุณยืนยันทำกิจกรรม (ซ่อม / กิน / นอน)\n" +
                "• เดิน สำรวจ อ่านบันทึก เปิด Handbook = ฟรี ใช้เวลาวางแผนได้เต็มที่\n" +
                "• เดินได้ทุกทิศ: <b>WASD</b> / ลูกศร (W = เดินลึกเข้าไป, S = เดินออกมาหาจอ) หรือคลิกพื้น • <b>E</b> = สำรวจ/ใช้ของ • <b>ดับเบิลคลิก</b> = ไปถึงทันที\n" +
                "• เมาส์บอกว่าคลิกแล้วจะเกิดอะไร: แว่นขยาย = ดู, เป้าเขียว = ใช้ไอเทมได้, วงกลมขีดแดง = ใช้ไม่ได้ • <b>Esc</b> = เมนู\n" +
                "• คลิกไอเทมใน Inventory แล้วคลิกวัตถุ (หรือกด E ใกล้วัตถุ) เพื่อใช้ • เดินชนประตูเพื่อผ่าน • ประตู <b>▲</b> ผนังหลัง = ห้องด้านใน\n" +
                "• งานซ่อมมีมินิเกมสั้นๆ: ทำพลาด = เสียเวลาเพิ่ม 30 นาที, ทำ PERFECT = ได้โบนัส\n" +
                "• หิว/เหนื่อยเกิน 80 → งานช้าลง. เหนื่อยถึง 100 → สลบ 6 ชม.\n" +
                "• ท่อเชื้อเพลิงรั่วอยู่ — ยิ่งช้า ยิ่งเสียโอกาส Jump\n\n" +
                "<color=#FFD27F>เป้าหมาย</color>  ซ่อมยานให้ทัน แล้วเลือก:  <b>THE JUMP</b> (Saturn) หรือ <b>MARS FALLBACK</b> (ARES-7)",
                22, TextAnchor.UpperLeft, CText, 0.05f, 0.16f, 0.95f, 0.77f);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 14; t.resizeTextMaxSize = 22;
            Btn(p, "START MISSION", 0.35f, 0.03f, 0.65f, 0.13f, new Color(0.15f, 0.48f, 0.28f), CloseModal, 28);
            modalRoot.name = "Start";
        }

        void ShowEnding()
        {
            if (modalRoot != null && modalRoot.name == "Ending") return;
            var p = OpenModal(0.15f, 0.1f, 0.85f, 0.9f);
            modalRoot.name = "Ending";
            Sfx.Play(model.Ending == Ending.Jump ? "jump" : model.Ending == Ending.Mars ? "landing" : "fail", 1f);
            modalRoot.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);
            bool ok = model.IsSuccess;
            Label(p, model.EndingTitle(), 44, TextAnchor.UpperCenter, ok ? CGood : CBad, 0, 0.82f, 1, 0.97f, true);
            if (ok)
            {
                string stars = new string('★', model.Stars) + new string('☆', 3 - model.Stars);
                Label(p, stars, 48, TextAnchor.MiddleCenter, CWarn, 0, 0.7f, 1, 0.82f, true);
            }
            Label(p, model.EndingText(), 24, TextAnchor.UpperCenter, CText, 0.06f, 0.4f, 0.94f, 0.7f);
            string stats = string.Format("เวลาที่เหลือ {0}   •   O2 {1:0}   •   Power {2:0}   •   Propellant {3:0}\nActions {4}   •   Crew logs {5}/3",
                GameModel.FormatTime(model.MinutesLeft), model.Oxygen, model.Power, model.Propellant, model.ActionsTaken, model.LogsRead);
            Label(p, stats, 20, TextAnchor.MiddleCenter, CDim, 0.05f, 0.2f, 0.95f, 0.38f);
            Btn(p, "RESTART MISSION", 0.33f, 0.04f, 0.67f, 0.15f, new Color(0.18f, 0.40f, 0.60f), Restart, 26);
        }

        void Restart()
        {
            CloseModal();
            shownAlerts = 0;
            bannerRoot.SetActive(false);
            StopAllCoroutines();
            transitioning = false;
            fade.color = new Color(0, 0, 0, 0);
            model.Reset(); // fires Changed -> Refresh
            lastRoom = Room.Quarters;
            astro.Shape = Stage25D.Shape(Room.Quarters);
            astro.Place(StartPos);
            lastHint = null; hintSince = Time.unscaledTime;
            inspected.Clear(); BuildRoom();
            ShowStart();
        }
    }
}
