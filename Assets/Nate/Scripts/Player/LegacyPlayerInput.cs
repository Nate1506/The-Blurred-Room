#if ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine;

namespace BlurredRoom
{
    /// <summary>Input source for the old Input Manager. Only compiles when legacy input is enabled in Player Settings.</summary>
    public class LegacyPlayerInput : PlayerInputSource
    {
        [SerializeField] KeyCode runKey = KeyCode.LeftShift;
        [SerializeField] KeyCode interactKey = KeyCode.E;
        [SerializeField] KeyCode pickupKey = KeyCode.F;
        [SerializeField] KeyCode peepholeKey = KeyCode.Q;
        [SerializeField] KeyCode ritualKey = KeyCode.R;
        [SerializeField] float mouseScale = 10f;

        public override Vector2 Move => Vector2.ClampMagnitude(
            new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
        public override Vector2 Look => new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseScale;
        public override bool RunHeld => Input.GetKey(runKey);
        public override bool InteractPressed => Input.GetKeyDown(interactKey);
        public override bool PickupPressed => Input.GetKeyDown(pickupKey);
        public override bool PeepholeHeld => Input.GetKey(peepholeKey);
        public override bool RitualHeld => Input.GetKey(ritualKey);
    }
}
#endif
