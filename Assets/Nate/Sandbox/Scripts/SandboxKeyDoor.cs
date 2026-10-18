using UnityEngine;

namespace BlurredRoom.Sandbox
{
    /// <summary>Unlocks a Door as soon as the player's inventory contains the key item.</summary>
    public class SandboxKeyDoor : MonoBehaviour
    {
        [SerializeField] Door door;
        [Tooltip("Empty = the inventory on the object tagged Player.")]
        [SerializeField] PlayerInventory inventory;
        [SerializeField] string keyItemId = "room_key";

        void Start()
        {
            if (!inventory)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player) inventory = player.FindInRoot<PlayerInventory>();
            }
            if (!inventory) return;

            inventory.ItemAdded += OnItemAdded;
            if (inventory.Has(keyItemId)) Unlock();
        }

        void OnDestroy()
        {
            if (inventory) inventory.ItemAdded -= OnItemAdded;
        }

        void OnItemAdded(string itemId)
        {
            if (itemId == keyItemId) Unlock();
        }

        void Unlock()
        {
            if (!door || !door.IsLocked) return;
            door.SetLocked(false);
            Debug.Log($"[Sandbox] {door.name} unlocked with '{keyItemId}'.", door);
        }
    }
}
