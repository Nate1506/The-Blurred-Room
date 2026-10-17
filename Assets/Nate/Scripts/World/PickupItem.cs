using UnityEngine;
using UnityEngine.Events;

namespace BlurredRoom
{
    /// <summary>An item in the world the player can pick up. Can also reveal a clue when picked.</summary>
    public class PickupItem : MonoBehaviour, IPickupable
    {
        [SerializeField] string itemId = "item";
        [SerializeField] string displayName = "Item";
        [Tooltip("Optional: picking this up also discovers this clue.")]
        [SerializeField] ClueData clue;
        [SerializeField] bool destroyOnPickup = true;
        [SerializeField] UnityEvent onPickedUp;

        public string ItemId => itemId;
        public string DisplayName => displayName;

        public bool CanPickup(GameObject picker) => isActiveAndEnabled;

        public void OnPickedUp(GameObject picker)
        {
            if (clue && ClueManager.Instance) ClueManager.Instance.Discover(clue);
            onPickedUp?.Invoke();
            if (destroyOnPickup) Destroy(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
