using System;
using UnityEngine;

namespace BlurredRoom
{
    /// <summary>Interact key on whatever IInteractable the player is looking at (doors, NPCs...).</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] PlayerInputSource input;
        [SerializeField] PlayerSight sight;

        public IInteractable Focus { get; private set; }

        /// <summary>For UI prompts. Null when nothing.</summary>
        public event Action<IInteractable> FocusChanged;
        public event Action<IInteractable> Interacted;

        void Awake()
        {
            if (!input) input = GetComponentInParent<PlayerInputSource>();
            if (!sight) sight = this.FindInRoot<PlayerSight>();
        }

        void Update()
        {
            IInteractable target = sight ? sight.Get<IInteractable>() : null;
            if (target != null && !target.CanInteract(gameObject)) target = null;
            if (!ReferenceEquals(target, Focus))
            {
                Focus = target;
                FocusChanged?.Invoke(Focus);
            }

            if (Focus != null && input && input.InteractPressed)
            {
                Focus.Interact(gameObject);
                Interacted?.Invoke(Focus);
            }
        }
    }
}
