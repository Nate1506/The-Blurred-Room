using BlurredRoom;
using UnityEngine;

// ให้ของที่หยิบได้ (PickupItem ของเนท) กดด้วย E ได้ด้วย ปุ่มเดียวกับประตู
// ทำงานผ่านระบบเดิมของเนท: ใส่กระเป๋า + เรียก OnPickedUp (ซึ่งเปิดเบาะแสเอง)
[RequireComponent(typeof(PickupItem))]
public class EasyPickup : MonoBehaviour, IInteractable
{
    PickupItem item;
    PlayerInventory inventory;

    void Awake() { item = GetComponent<PickupItem>(); }
    void Start() { inventory = FindFirstObjectByType<PlayerInventory>(); }

    public string Prompt => "Take " + item.DisplayName;
    public bool CanInteract(GameObject interactor) => item && item.CanPickup(interactor);

    public void Interact(GameObject interactor)
    {
        if (!inventory || !inventory.TryAdd(item.ItemId)) return;
        GameState.TakenPickups.Add(item.ItemId);
        item.OnPickedUp(interactor);
    }
}
