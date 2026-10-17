using System;
using UnityEngine;

namespace BlurredRoom
{
    /// <summary>Hold the Peephole key while looking at a Door that has a peephole to look outside.</summary>
    public class PlayerPeephole : MonoBehaviour
    {
        [SerializeField] PlayerInputSource input;
        [SerializeField] PlayerSight sight;
        [SerializeField] bool lockMovementWhileViewing = true;

        public Door Current { get; private set; }
        public bool IsViewing => Current != null;

        /// <summary>Door being looked through. Read door.Visitor to know who is outside.</summary>
        public event Action<Door> Started;
        public event Action<Door> Ended;

        void Awake()
        {
            if (!input) input = GetComponentInParent<PlayerInputSource>();
            if (!sight) sight = this.FindInRoot<PlayerSight>();
        }

        void OnDisable()
        {
            if (Current) End();
        }

        void Update()
        {
            bool held = input && input.PeepholeHeld;

            if (Current)
            {
                if (!held || Current.IsOpen) End();
                return;
            }

            if (!held || !sight) return;
            Door door = sight.Get<Door>();
            if (door && door.HasPeephole && !door.IsOpen) Begin(door);
        }

        void Begin(Door door)
        {
            Current = door;
            if (lockMovementWhileViewing && input) input.LockMovement(this);
            door.SetPeepholeInUse(true);
            Started?.Invoke(door);
        }

        void End()
        {
            Door door = Current;
            Current = null;
            if (input) input.UnlockMovement(this);
            if (door) door.SetPeepholeInUse(false);
            Ended?.Invoke(door);
        }
    }
}
