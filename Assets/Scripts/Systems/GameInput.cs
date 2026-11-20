using UnityEngine;
using UnityEngine.InputSystem;

namespace LastLantern
{
    /// <summary>
    /// Input Actions игры (новая Input System), собранные в коде:
    /// движение — WASD, стрелки, левый стик и крестовина геймпада; пауза — Esc и Start.
    /// </summary>
    public static class GameInput
    {
        static InputAction move;
        static InputAction pause;

        public static InputAction Move { get { Init(); return move; } }
        public static InputAction Pause { get { Init(); return pause; } }

        static void Init()
        {
            if (move != null) return;

            move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            move.AddBinding("<Gamepad>/dpad");
            move.Enable();

            pause = new InputAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");
            pause.Enable();
        }

        // Сбрасываем статику при входе в Play Mode (на случай выключенной перезагрузки домена)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            move?.Dispose();
            pause?.Dispose();
            move = null;
            pause = null;
        }
    }
}
