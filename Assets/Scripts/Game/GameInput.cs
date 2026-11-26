using UnityEngine;
using DrownedBell.Core;

namespace DrownedBell.Game
{
    /// <summary>
    /// Keyboard (and the default gamepad axes) through the legacy Input Manager, which this project uses.
    /// Press edges are latched each rendered frame and handed to the next fixed simulation step, so a tap that
    /// falls between steps is never lost.
    /// </summary>
    public sealed class GameInput
    {
        bool jump, dash, attack, mouseAttack, mouseDash;

        static bool Any(params KeyCode[] keys)
        {
            foreach (KeyCode k in keys) if (Input.GetKey(k)) return true;
            return false;
        }

        static bool AnyDown(params KeyCode[] keys)
        {
            foreach (KeyCode k in keys) if (Input.GetKeyDown(k)) return true;
            return false;
        }

        public static bool JumpHeldNow => Any(KeyCode.Space, KeyCode.Z, KeyCode.JoystickButton0);

        /// <summary>Call once per rendered frame.</summary>
        public void Poll()
        {
            if (AnyDown(KeyCode.Space, KeyCode.Z, KeyCode.JoystickButton0)) jump = true;
            if (AnyDown(KeyCode.C, KeyCode.LeftShift, KeyCode.K, KeyCode.JoystickButton5)) dash = true;
            if (AnyDown(KeyCode.X, KeyCode.J, KeyCode.JoystickButton2)) attack = true;
            // Mouse: left click swings toward the cursor, right click dashes toward it.
            if (Input.GetMouseButtonDown(0)) { attack = true; mouseAttack = true; }
            if (Input.GetMouseButtonDown(1)) { dash = true; mouseDash = true; }
        }

        /// <summary>Build the input for one fixed step, consuming latched presses. `diver` is the diver's world position, for mouse aim.</summary>
        public PlayerInput Take(Camera cam, Vector2 diver, Rect display)
        {
            float x = 0f;
            if (Any(KeyCode.A, KeyCode.LeftArrow)) x -= 1f;
            if (Any(KeyCode.D, KeyCode.RightArrow)) x += 1f;
            float axis = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(axis) > 0.4f) x = Mathf.Sign(axis);
            float vertical = Input.GetAxisRaw("Vertical");
            var input = new PlayerInput
            {
                MoveX = x,
                Up = Any(KeyCode.W, KeyCode.UpArrow) || vertical > 0.5f,
                Down = Any(KeyCode.S, KeyCode.DownArrow) || vertical < -0.5f,
                JumpPressed = jump,
                JumpHeld = JumpHeldNow,
                DashPressed = dash,
                AttackPressed = attack,
            };
            if ((mouseAttack || mouseDash) && cam != null)
            {
                Vector3 cursor = Input.mousePosition;
                Vector3 m = cam.ViewportToWorldPoint(new Vector3(
                    (cursor.x - display.x) / display.width,
                    (cursor.y - (Screen.height - display.yMax)) / display.height,
                    -cam.transform.position.z));
                input.HasAim = true;
                input.AimX = m.x - diver.x;
                input.AimY = m.y - diver.y;
            }
            jump = dash = attack = mouseAttack = mouseDash = false;
            return input;
        }

        public void Clear() { jump = dash = attack = mouseAttack = mouseDash = false; }

        /// <summary>Queue a swing as if the key had been pressed (used by the screenshot tour).</summary>
        public void PressAttack() { attack = true; }

        public static bool Confirm => AnyDown(KeyCode.Return, KeyCode.KeypadEnter, KeyCode.JoystickButton7);
        /// <summary>Enter, or a left click, on a menu screen.</summary>
        public static bool ConfirmOrClick => Confirm || Input.GetMouseButtonDown(0);
        public static bool Pause => AnyDown(KeyCode.Escape, KeyCode.P, KeyCode.JoystickButton6);
        /// <summary>Running in a browser: F-keys belong to the browser there (F5 reloads, F1 opens help) and Quit does nothing.</summary>
        public static readonly bool Web = Application.platform == RuntimePlatform.WebGLPlayer;
        public static bool Debug => AnyDown(KeyCode.F1, KeyCode.I);
        public static bool Mute => AnyDown(KeyCode.M);
        public static bool Quit => !Web && AnyDown(KeyCode.Q);
        public static bool CycleShake => AnyDown(KeyCode.F5, KeyCode.Alpha1);
        public static bool ToggleFlashing => AnyDown(KeyCode.F6, KeyCode.Alpha2);
    }
}
