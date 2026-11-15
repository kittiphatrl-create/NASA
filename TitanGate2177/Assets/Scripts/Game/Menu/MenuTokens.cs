// Design tokens for the LAST DOCK main menu. Every colour, size, gap and timing the menu
// components use comes from here, so the Figma spec and the game stay in step.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game.Menu
{
    public enum MenuFont { Display, Menu, Ui, UiBold }

    public static class MenuTokens
    {
        // ---- colour
        public static readonly Color BgDeep = Hex(0x0E0A0C);
        public static readonly Color Panel = Hex(0x14121A, 0.85f);
        public static readonly Color PanelHover = Hex(0x1E1A24, 0.92f);
        public static readonly Color Accent = Hex(0xF0A030);
        public static readonly Color AccentHot = Hex(0xFFB347);
        public static readonly Color TextPrimary = Hex(0xFFF4E0);
        public static readonly Color TextSecondary = Hex(0xC9B8A0);
        public static readonly Color Danger = Hex(0xE5533D);
        public static readonly Color CoolShadow = Hex(0x2B3A5C);
        public static readonly Color InkOnAccent = Hex(0x1A0E06);
        public static readonly Color InkOnAccentSoft = Hex(0x4A2A0E);
        public static readonly Color Border = new Color(Accent.r, Accent.g, Accent.b, 0.4f);
        public static readonly Color HighlightFill = new Color(Accent.r, Accent.g, Accent.b, 0.14f);
        public static readonly Color TextShadow = new Color(0, 0, 0, 0.75f);

        // ---- type (px at 1080p; scaled by the player's text-size setting)
        public const float TitleSize = 160, ButtonSize = 34, SubSize = 20, TaglineSize = 28, LocationSize = 20, SecondarySize = 24, FooterSize = 16, ChipSize = 16;
        public const float LocationTracking = 4, ButtonTracking = 2.5f, DisplayTracking = 4;

        // ---- spacing (8 px grid)
        public const float S1 = 8, S2 = 16, S3 = 24, S4 = 32, S6 = 48, S8 = 64;
        public const float Radius = 8, BorderWidth = 1.5f, FocusRing = 2;
        public const float ButtonWidth = 640, ButtonHeight = 84, IconDisc = 48, Icon = 24, ItemHeight = 48, ItemWidth = 308;
        /// <summary>Safe-area margin as a fraction of the screen.</summary>
        public const float Safe = 0.05f;
        /// <summary>Where the menu column is centred, as a fraction of screen height.</summary>
        public const float ColumnCentre = 0.55f;

        // ---- motion (seconds)
        public const float Fast = 0.12f, Base = 0.22f, Slow = 0.45f, Stagger = 0.08f, TooltipDelay = 0.3f;
        public const float FocusScale = 1.02f, PressScale = 0.98f;
        /// <summary>UI Toolkit transitions cannot take a custom curve; EaseOutCubic is the closest built-in to the token curve.</summary>
        public const EasingMode TransitionEase = EasingMode.EaseOutCubic;

        /// <summary>cubic-bezier(0.22, 1, 0.36, 1) for script-driven animation.</summary>
        public static float Ease(float x)
        {
            x = Mathf.Clamp01(x);
            const float x1 = 0.22f, y1 = 1f, x2 = 0.36f, y2 = 1f;
            float t = x;
            for (int i = 0; i < 6; i++)   // Newton: solve bx(t) = x
            {
                float bx = Bez(t, x1, x2) - x, d = BezD(t, x1, x2);
                if (Mathf.Abs(bx) < 1e-4f || Mathf.Abs(d) < 1e-5f) break;
                t = Mathf.Clamp01(t - bx / d);
            }
            return Bez(t, y1, y2);
        }
        static float Bez(float t, float a, float b) => 3 * (1 - t) * (1 - t) * t * a + 3 * (1 - t) * t * t * b + t * t * t;
        static float BezD(float t, float a, float b) => 3 * (1 - t) * (1 - t) * a + 6 * (1 - t) * t * (b - a) + 3 * t * t * (1 - b);

        public static float Scaled(float px) => Mathf.Round(px * Mathf.Clamp(Settings.TextScale, 1f, 1.5f));

        public static Color Hex(int rgb, float a = 1f) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);
        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        // ---- fonts
        static readonly Dictionary<string, Font> Fonts = new Dictionary<string, Font>();

        static Font Load(string name)
        {
            if (Fonts.TryGetValue(name, out var f) && f != null) return f;
            f = Resources.Load<Font>("Fonts/" + name);
            if (f == null) Debug.LogWarning("Menu font missing: Fonts/" + name);
            Fonts[name] = f;
            return f;
        }

        public static bool HasThai(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s) if (c >= '฀' && c <= '๿') return true;
            return false;
        }

        /// <summary>Latin display text uses Cinzel; Thai uses Taviraj (a looped serif) for menu labels and Sarabun for UI text.</summary>
        public static void Apply(TextElement t, MenuFont kind, float size, Color color, bool? thai = null)
        {
            bool th = thai ?? HasThai(t.text);
            Font f = kind switch
            {
                MenuFont.Display => Load(th ? "Taviraj-SemiBold" : "CinzelDecorative-Bold"),
                MenuFont.Menu => Load(th ? "Taviraj-SemiBold" : "Cinzel-SemiBold"),
                MenuFont.UiBold => Load("Sarabun-SemiBold"),
                _ => Load("Sarabun-Regular"),
            };
            if (f != null) t.style.unityFontDefinition = FontDefinition.FromFont(f);
            t.style.unityFontStyleAndWeight = FontStyle.Normal;
            t.style.unityTextGenerator = TextGeneratorType.Advanced;   // correct Thai mark stacking
            t.style.fontSize = Scaled(size);
            t.style.color = color;
            t.style.whiteSpace = WhiteSpace.Normal;
            t.style.letterSpacing = th ? 0 : kind == MenuFont.Display ? DisplayTracking : kind == MenuFont.Menu ? ButtonTracking : 0.3f;
            // Thai stacks vowels and tone marks above and below the line: give it room.
            t.style.paddingTop = th ? Mathf.Round(size * 0.12f) : 0;
            t.style.paddingBottom = th ? Mathf.Round(size * 0.08f) : 0;
            t.style.marginTop = t.style.marginBottom = 0;
            t.style.paddingLeft = t.style.paddingRight = 0;
        }

        public static Label Text(string text, MenuFont kind, float size, Color color, bool? thai = null)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            Apply(l, kind, size, color, thai);
            return l;
        }

        public static void Shadow(TextElement t, float blur = 6, float alpha = 0.75f)
            => t.style.textShadow = new TextShadow { offset = new Vector2(0, 2), blurRadius = blur, color = new Color(0, 0, 0, alpha) };

        public static void Round(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r;
        }

        public static void Stroke(VisualElement e, Color c, float w)
        {
            e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = w;
            e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = c;
        }

        public static VisualElement Fill(VisualElement e = null)
        {
            e ??= new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute; e.style.left = 0; e.style.right = 0; e.style.top = 0; e.style.bottom = 0;
            return e;
        }

        /// <summary>Smooth transitions for the given properties at a token duration.</summary>
        public static void Transition(VisualElement e, float seconds, params string[] props)
        {
            var names = new List<StylePropertyName>(); var durs = new List<TimeValue>(); var eases = new List<EasingFunction>();
            foreach (var p in props) { names.Add(p); durs.Add(new TimeValue(seconds, TimeUnit.Second)); eases.Add(TransitionEase); }
            e.style.transitionProperty = names; e.style.transitionDuration = durs; e.style.transitionTimingFunction = eases;
        }
    }
}
