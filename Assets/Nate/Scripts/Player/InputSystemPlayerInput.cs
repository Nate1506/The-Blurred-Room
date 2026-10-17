#if BR_INPUT_SYSTEM
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlurredRoom
{
    /// <summary>
    /// Input source for the new Input System. Only compiles once the Input System package is installed.
    /// Default bindings: WASD / mouse, Shift run, E interact, F pickup, Q peephole, R ritual (+ gamepad).
    /// </summary>
    public class InputSystemPlayerInput : PlayerInputSource
    {
        [SerializeField] float gamepadLookScale = 12f;

        InputAction move, look, run, interact, pickup, peephole, ritual;

        public override Vector2 Move => move.ReadValue<Vector2>();
        public override Vector2 Look => look.ReadValue<Vector2>();
        public override bool RunHeld => run.IsPressed();
        public override bool InteractPressed => interact.WasPressedThisFrame();
        public override bool PickupPressed => pickup.WasPressedThisFrame();
        public override bool PeepholeHeld => peephole.IsPressed();
        public override bool RitualHeld => ritual.IsPressed();

        void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
            string scale = gamepadLookScale.ToString(CultureInfo.InvariantCulture);
            look.AddBinding("<Gamepad>/rightStick").WithProcessor($"scaleVector2(x={scale},y={scale})");

            run = new InputAction("Run", InputActionType.Button, "<Keyboard>/leftShift");
            run.AddBinding("<Gamepad>/leftStickPress");

            interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonSouth");

            pickup = new InputAction("Pickup", InputActionType.Button, "<Keyboard>/f");
            pickup.AddBinding("<Gamepad>/buttonWest");

            peephole = new InputAction("Peephole", InputActionType.Button, "<Keyboard>/q");
            peephole.AddBinding("<Gamepad>/leftShoulder");

            ritual = new InputAction("Ritual", InputActionType.Button, "<Keyboard>/r");
            ritual.AddBinding("<Gamepad>/rightShoulder");
        }

        void OnEnable() => SetEnabled(true);
        void OnDisable() => SetEnabled(false);

        void OnDestroy()
        {
            move?.Dispose(); look?.Dispose(); run?.Dispose(); interact?.Dispose();
            pickup?.Dispose(); peephole?.Dispose(); ritual?.Dispose();
        }

        void SetEnabled(bool on)
        {
            foreach (InputAction a in new[] { move, look, run, interact, pickup, peephole, ritual })
            {
                if (a == null) continue;
                if (on) a.Enable(); else a.Disable();
            }
        }
    }
}
#endif
