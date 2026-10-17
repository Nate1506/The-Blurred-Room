using System;
using UnityEngine;

namespace BlurredRoom
{
    /// <summary>Pickup key on whatever IPickupable the player is looking at → PlayerInventory.</summary>
    public class PlayerPickup : MonoBehaviour
    {
        [SerializeField] PlayerInputSource input;
        [SerializeField] PlayerSight sight;
        [SerializeField] PlayerInventory inventory;

        public IPickupable Focus { get; private set; }

        /// <summary>For UI prompts ("[F] Pick up key"). Null when nothing.</summary>
        public event Action<IPickupable> FocusChanged;
        public event Action<IPickupable> PickedUp;
        public event Action<IPickupable> PickupFailed;

        void Awake()
        {
            if (!input) input = GetComponentInParent<PlayerInputSource>();
            if (!sight) sight = this.FindInRoot<PlayerSight>();
            if (!inventory) inventory = this.FindInRoot<PlayerInventory>();
        }

        void Update()
        {
            IPickupable target = sight ? sight.Get<IPickupable>() : null;
            if (target != null && !target.CanPickup(gameObject)) target = null;
            if (!ReferenceEquals(target, Focus))
            {
                Focus = target;
                FocusChanged?.Invoke(Focus);
            }

            if (Focus == null || !input || !input.PickupPressed) return;

            IPickupable item = Focus;
            if (inventory && inventory.TryAdd(item.ItemId))
            {
                item.OnPickedUp(gameObject);
                PickedUp?.Invoke(item);
            }
            else
            {
                PickupFailed?.Invoke(item);
            }
        }
    }
}
