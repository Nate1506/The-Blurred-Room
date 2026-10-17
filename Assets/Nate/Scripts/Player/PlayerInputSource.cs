using System.Collections.Generic;
using UnityEngine;

namespace BlurredRoom
{
    /// <summary>
    /// Every player system reads input from here, never from a device directly.
    /// Swap the concrete source (Input System, legacy, AI, cutscene...) without touching the systems.
    /// </summary>
    public abstract class PlayerInputSource : MonoBehaviour
    {
        public abstract Vector2 Move { get; }
        /// <summary>Look delta this frame (mouse-delta scale).</summary>
        public abstract Vector2 Look { get; }
        public abstract bool RunHeld { get; }
        public abstract bool InteractPressed { get; }
        public abstract bool PickupPressed { get; }
        public abstract bool PeepholeHeld { get; }
        public abstract bool RitualHeld { get; }

        readonly HashSet<object> movementLocks = new HashSet<object>();

        /// <summary>True while any system has locked walking/looking (peephole, ritual, ending, UI...).</summary>
        public bool MovementLocked => movementLocks.Count > 0;

        public void LockMovement(object owner) { if (owner != null) movementLocks.Add(owner); }
        public void UnlockMovement(object owner) { if (owner != null) movementLocks.Remove(owner); }
    }
}
