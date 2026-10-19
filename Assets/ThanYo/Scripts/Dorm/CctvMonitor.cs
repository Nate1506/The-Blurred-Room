using BlurredRoom;
using UnityEngine;

// จอกล้องวงจรปิดบนโต๊ะห้องออฟฟิศ: กด E เปิด/ปิดหน้ากล้อง
public class CctvMonitor : MonoBehaviour, IInteractable
{
    public CctvSystem system;

    public string Prompt => system && system.IsOpen ? "ออกจากกล้อง" : "ดูกล้องวงจรปิด";

    public bool CanInteract(GameObject interactor) => system && (system.IsOpen || !DormFX.UIBusy);

    public void Interact(GameObject interactor)
    {
        if (system) system.Toggle();
    }
}
