using UnityEngine;
using UnityEngine.Events;

namespace BlurredRoom
{
    /// <summary>A place where the player can perform a ritual (hold key). Optional item requirement and sanity cost.</summary>
    public class RitualPoint : MonoBehaviour, IRitualTarget
    {
        [SerializeField] string ritualId = "ritual";
        [SerializeField] string prompt = "Perform ritual";
        [SerializeField] float duration = 4f;
        [Tooltip("Optional item needed in the inventory.")]
        [SerializeField] string requiredItemId = "";
        [SerializeField] bool consumeItem = true;
        [SerializeField] float sanityCost = 0f;
        [Tooltip("Flag set in ClueManager when done (used by endings). Empty = ritualId.")]
        [SerializeField] string flagOnComplete = "";
        [SerializeField] bool oneShot = true;

        [SerializeField] UnityEvent<float> onProgress;
        [SerializeField] UnityEvent onCompleted;
        [SerializeField] UnityEvent onCancelled;

        bool done;

        public string RitualId => ritualId;
        public string Prompt => prompt;
        public float RitualDuration => duration;
        public bool IsDone => done;

        public bool CanPerformRitual(GameObject performer)
        {
            if (!isActiveAndEnabled || (oneShot && done)) return false;
            if (string.IsNullOrEmpty(requiredItemId)) return true;
            PlayerInventory inv = performer.FindInRoot<PlayerInventory>();
            return inv && inv.Has(requiredItemId);
        }

        public void OnRitualProgress(float normalized) => onProgress?.Invoke(normalized);

        public void OnRitualCompleted(GameObject performer)
        {
            done = true;

            if (consumeItem && !string.IsNullOrEmpty(requiredItemId))
            {
                PlayerInventory inv = performer.FindInRoot<PlayerInventory>();
                if (inv) inv.Remove(requiredItemId);
            }

            if (sanityCost > 0f)
            {
                PlayerSanity sanity = performer.FindInRoot<PlayerSanity>();
                if (sanity) sanity.Drain(sanityCost);
            }

            if (ClueManager.Instance)
                ClueManager.Instance.SetFlag(string.IsNullOrEmpty(flagOnComplete) ? ritualId : flagOnComplete);

            onCompleted?.Invoke();
        }

        public void OnRitualCancelled() => onCancelled?.Invoke();
    }
}
