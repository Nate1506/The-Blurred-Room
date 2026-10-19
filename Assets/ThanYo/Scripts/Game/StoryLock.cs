using BlurredRoom;
using UnityEngine;

// ล็อกประตูของเนทไว้จนกว่าจะเข้าเงื่อนไข: มีไอเทม (เช่น กุญแจ) หรือมีธงเนื้อเรื่อง
// ว่างทั้งสองช่อง = ล็อกถาวร (ห้องที่ไม่เกี่ยวกับเรื่อง)
[RequireComponent(typeof(Door))]
public class StoryLock : MonoBehaviour
{
    public string requiredItemId = "";
    public string unlockFlag = "";
    public string lockedMessage = "";

    Door door;
    PlayerInventory inventory;

    void Start()
    {
        door = GetComponent<Door>();
        inventory = FindFirstObjectByType<PlayerInventory>();
        door.SetLocked(!Met());
        door.LockedAttempt += () => { if (!string.IsNullOrEmpty(lockedMessage) && GameHUD.Instance) GameHUD.Instance.ShowMessage(lockedMessage, 2.5f); };
    }

    bool Met()
    {
        bool hasRule = !string.IsNullOrEmpty(requiredItemId) || !string.IsNullOrEmpty(unlockFlag);
        if (!hasRule) return false;
        if (!string.IsNullOrEmpty(requiredItemId) && inventory && inventory.Has(requiredItemId)) return true;
        if (!string.IsNullOrEmpty(unlockFlag) && GameState.Has(unlockFlag)) return true;
        return false;
    }

    void Update()
    {
        if (door.IsLocked && Met()) door.SetLocked(false);
    }
}
