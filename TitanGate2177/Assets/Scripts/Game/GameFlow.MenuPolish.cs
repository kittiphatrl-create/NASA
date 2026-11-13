using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        float _titleBoostUntil, _titleWaveUntil;
        bool _menuLaunching;
        VisualElement _shipSprintFill;
        SpriteRenderer _titleDrone;

        VisualElement BSprintPanel()
        {
            var p = new VisualElement(); p.style.position = Position.Absolute; p.style.right = 24; p.style.top = 20; p.style.width = 220;
            p.style.backgroundColor = GameUI.CPanel; GameUI.Border(p, GameUI.CCyanD, 1, 3); p.style.paddingLeft = p.style.paddingRight = 14; p.style.paddingTop = p.style.paddingBottom = 10;
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
            var icon = new Image { sprite = Art.SprintIcon() }; icon.style.width = 28; icon.style.height = 28; row.Add(icon);
            row.Add(_ui.L("SHIFT", 18, true, GameUI.CCyan)); p.Add(row);
            var track = new VisualElement(); track.style.height = 8; track.style.backgroundColor = GameUI.CPanel2;
            _shipSprintFill = new VisualElement(); _shipSprintFill.style.height = 8; _shipSprintFill.style.backgroundColor = GameUI.CCyan; track.Add(_shipSprintFill); p.Add(track);
            return p;
        }

        void TitleShipPoke() { _titleBoostUntil = Time.unscaledTime + 1.4f; _sfx.Play("ship", .55f); }
        void TitleDronePoke() { _titleWaveUntil = Time.unscaledTime + 1.6f; _sfx.Play("drone", .45f); }

        void MenuDepart(Action action)
        {
            if (_menuLaunching) return;
            _menuLaunching = true; _sfx.Play("open", .45f); TitleShipPoke();
            _ui.Root.schedule.Execute(() => { _menuLaunching = false; action(); }).StartingIn(Settings.ReducedMotion ? 0 : 260);
        }

        void AddTitleInteractions(VisualElement root)
        {
            void Hotspot(string name, float left, float top, float width, float height, Action action)
            {
                var button = new Button(action) { name = name, tooltip = name == "kestrel-interactive" ? CrewText("Kestrel · test engines", "Kestrel · ทดสอบเครื่องยนต์") : CrewText("Say hello to ORBI", "ทักทาย ORBI") };
                button.style.position = Position.Absolute; button.style.left = Length.Percent(left); button.style.top = Length.Percent(top);
                button.style.width = Length.Percent(width); button.style.height = Length.Percent(height);
                button.style.backgroundColor = Color.clear; GameUI.Border(button, Color.clear, 0, 0);
                button.RegisterCallback<FocusInEvent>(_ => GameUI.Border(button, GameUI.CAmber, 1, 4));
                button.RegisterCallback<FocusOutEvent>(_ => GameUI.Border(button, Color.clear, 0, 0));
                root.Add(button);
            }
            Hotspot("kestrel-interactive", 54, 48, 43, 32, TitleShipPoke);
            Hotspot("orbi-interactive", 57, 37, 10, 17, TitleDronePoke);
        }

        void ThemePanel(VisualElement panel, string eyebrow)
        {
            panel.style.backgroundColor = new Color(.045f, .085f, .12f, .98f);
            GameUI.Border(panel, new Color(.42f, .63f, .63f), 2, 3);
            panel.style.borderTopColor = GameUI.CAmber; panel.style.borderTopWidth = 5;
            var label = _ui.L(eyebrow, 17, true, GameUI.CAmber); label.style.marginBottom = 8; panel.Add(label);
        }

        void StyleFlightButton(Button button, bool primary = false)
        {
            GameUI.Border(button, primary ? GameUI.CAmber : GameUI.CCyanD, primary ? 2 : 1, 3);
            button.style.borderLeftWidth = primary ? 7 : 3;
            button.style.borderBottomWidth = 4;
            button.style.borderBottomColor = new Color(.025f, .045f, .08f);
            if (primary) { button.style.backgroundColor = new Color(.78f, .45f, .23f); button.style.color = GameUI.CText; }
        }
    }
}
