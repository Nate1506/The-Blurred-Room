using UnityEngine;

namespace BlurredRoom
{
    /// <summary>Anything the player can use with the Interact key (doors, NPCs, switches...).</summary>
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
    }

    /// <summary>Anything the player can pick up into the inventory.</summary>
    public interface IPickupable
    {
        string ItemId { get; }
        string DisplayName { get; }
        bool CanPickup(GameObject picker);
        void OnPickedUp(GameObject picker);
    }

    /// <summary>Anything the player can perform a (hold-to-complete) ritual on.</summary>
    public interface IRitualTarget
    {
        string Prompt { get; }
        float RitualDuration { get; }
        bool CanPerformRitual(GameObject performer);
        void OnRitualProgress(float normalized);
        void OnRitualCompleted(GameObject performer);
        void OnRitualCancelled();
    }
}
