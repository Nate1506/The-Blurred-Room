using UnityEngine;

namespace BlurredRoom.Tests
{
    /// <summary>
    /// Scriptable input for tests. Held values stay until changed.
    /// Pressed values (Interact / Pickup) are true during exactly one frame: the next Update pass after they are set.
    /// Runs before every other script so the press is visible to all systems in that frame.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class FakePlayerInput : PlayerInputSource
    {
        public Vector2 MoveValue;
        public Vector2 LookValue;
        public bool RunHeldValue;
        public bool PeepholeHeldValue;
        public bool RitualHeldValue;

        bool interactQueued, interactNow;
        bool pickupQueued, pickupNow;

        /// <summary>Set true to press Interact for one frame (the next frame's Update).</summary>
        public bool InteractPressedValue
        {
            get => interactNow;
            set { interactQueued = value; if (!value) interactNow = false; }
        }

        /// <summary>Set true to press Pickup for one frame (the next frame's Update).</summary>
        public bool PickupPressedValue
        {
            get => pickupNow;
            set { pickupQueued = value; if (!value) pickupNow = false; }
        }

        public override Vector2 Move => MoveValue;
        public override Vector2 Look => LookValue;
        public override bool RunHeld => RunHeldValue;
        public override bool InteractPressed => interactNow;
        public override bool PickupPressed => pickupNow;
        public override bool PeepholeHeld => PeepholeHeldValue;
        public override bool RitualHeld => RitualHeldValue;

        public void PressInteract() => InteractPressedValue = true;
        public void PressPickup() => PickupPressedValue = true;

        void Update()
        {
            interactNow = interactQueued;
            interactQueued = false;
            pickupNow = pickupQueued;
            pickupQueued = false;
        }
    }
}
