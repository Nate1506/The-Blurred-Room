using UnityEngine;

namespace BlurredRoom.Tests
{
    public class TestPickupable : MonoBehaviour, IPickupable
    {
        public string itemId = "test_item";
        public bool canPickup = true;
        public int pickedUpCount;
        public GameObject lastPicker;

        public string ItemId => itemId;
        public string DisplayName => "Test item";
        public bool CanPickup(GameObject picker) => canPickup;

        public void OnPickedUp(GameObject picker)
        {
            pickedUpCount++;
            lastPicker = picker;
        }
    }
}
