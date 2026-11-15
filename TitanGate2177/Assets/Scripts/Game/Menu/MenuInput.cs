// Reads mouse, keyboard, gamepad and touch for the menu, tracks the last-used device
// (prompts and focus rings follow it) and turns held directions into repeating steps.
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TitanGate.Game.Menu
{
    public enum MenuDevice { Mouse, Keyboard, Gamepad, Touch }

    public struct MenuInputFrame
    {
        public int dx, dy;            // -1 / 0 / +1 steps this frame (dy +1 = down)
        public bool submit, back, any;
        public Vector2 look;          // -0.5..0.5, for parallax
    }

    public class MenuInput
    {
        public MenuDevice Device { get; private set; } = MenuDevice.Mouse;
        float _lastMouseMove = -1f;

        /// <summary>True while the physical mouse is moving (not when menu items slide under a still cursor).</summary>
        public bool MouseMovedRecently
        {
            get
            {
                var m = Mouse.current; if (m == null) return false;
                if (m.delta.ReadValue().sqrMagnitude > 0.5f) return true;
                return Time.unscaledTime - _lastMouseMove < 0.12f;
            }
        }
        public event Action<MenuDevice> DeviceChanged;

        const float RepeatDelay = 0.38f, RepeatRate = 0.11f, StickOn = 0.55f, StickOff = 0.35f;
        Vector2Int _held; float _heldT, _nextRepeat;
        Vector2 _lastMouse = new Vector2(-1, -1);

        void Use(MenuDevice d)
        {
            if (d == Device) return;
            Device = d;
            DeviceChanged?.Invoke(d);
        }

        public MenuInputFrame Poll(float dt)
        {
            var f = new MenuInputFrame();
            var kb = Keyboard.current; var gp = Gamepad.current; var mouse = Mouse.current; var touch = Touchscreen.current;
            Vector2Int dir = Vector2Int.zero; bool pressedDirThisFrame = false;

            if (kb != null)
            {
                if (kb.anyKey.wasPressedThisFrame) { Use(MenuDevice.Keyboard); f.any = true; }
                if (kb.upArrowKey.isPressed || kb.wKey.isPressed) dir.y = -1;
                else if (kb.downArrowKey.isPressed || kb.sKey.isPressed) dir.y = 1;
                if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) dir.x = -1;
                else if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) dir.x = 1;
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) f.submit = true;
                if (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame) f.back = true;
            }
            if (gp != null)
            {
                var stick = gp.leftStick.ReadValue();
                bool button = gp.buttonSouth.wasPressedThisFrame || gp.buttonEast.wasPressedThisFrame || gp.buttonNorth.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame
                    || gp.startButton.wasPressedThisFrame || gp.selectButton.wasPressedThisFrame || gp.dpad.up.wasPressedThisFrame || gp.dpad.down.wasPressedThisFrame
                    || gp.dpad.left.wasPressedThisFrame || gp.dpad.right.wasPressedThisFrame;
                if (button || stick.magnitude > StickOn || gp.rightStick.ReadValue().magnitude > StickOn) { Use(MenuDevice.Gamepad); f.any |= button; }
                float on = _held == Vector2Int.zero ? StickOn : StickOff;
                if (gp.dpad.up.isPressed || stick.y > on) dir.y = -1; else if (gp.dpad.down.isPressed || stick.y < -on) dir.y = 1;
                if (gp.dpad.left.isPressed || stick.x < -on) dir.x = -1; else if (gp.dpad.right.isPressed || stick.x > on) dir.x = 1;
                if (gp.buttonSouth.wasPressedThisFrame) f.submit = true;
                if (gp.buttonEast.wasPressedThisFrame) f.back = true;
                var rs = gp.rightStick.ReadValue();
                if (Device == MenuDevice.Gamepad) f.look = rs * 0.5f;
            }
            if (mouse != null)
            {
                var mp = mouse.position.ReadValue();
                bool click = mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame;
                if (_lastMouse.x >= 0 && (mp - _lastMouse).sqrMagnitude > 1f) _lastMouseMove = Time.unscaledTime;
                if (click || (_lastMouse.x >= 0 && (mp - _lastMouse).sqrMagnitude > 9f)) Use(MenuDevice.Mouse);
                f.any |= click;
                _lastMouse = mp;
                if (Device == MenuDevice.Mouse && Screen.width > 0)
                    f.look = new Vector2(mp.x / Screen.width - 0.5f, mp.y / Screen.height - 0.5f);
            }
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) { Use(MenuDevice.Touch); f.any = true; }

            // directions: a step on press, then repeats while held
            if (dir != _held) { _held = dir; _heldT = 0; _nextRepeat = RepeatDelay; pressedDirThisFrame = dir != Vector2Int.zero; }
            else if (dir != Vector2Int.zero)
            {
                _heldT += dt;
                if (_heldT >= _nextRepeat) { _nextRepeat += RepeatRate; pressedDirThisFrame = true; }
            }
            if (pressedDirThisFrame) { f.dx = dir.x; f.dy = dir.y; }
            return f;
        }

        /// <summary>Focus rings show for keyboard and gamepad only (focus-visible), never for mouse or touch.</summary>
        public bool FocusVisible => Device == MenuDevice.Keyboard || Device == MenuDevice.Gamepad;
    }
}
