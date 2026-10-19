using BlurredRoom;
using UnityEngine;

// ของสำหรับพิธี (ธูป / ชอล์ก): กด E เก็บ ฟรี ไม่ใช้แอ็กชัน
// โผล่ใหม่ทุกวัน จนกว่าคืนนั้นจะทำพิธีเสร็จ (ถ้าถืออยู่แล้วจะซ่อน)
public class RitualSupply : MonoBehaviour, IInteractable
{
    [Tooltip("DormData.Incense หรือ DormData.Chalk")]
    public string itemId = DormData.Incense;

    Renderer[] rends;
    Collider[] cols;
    bool shown = true;

    bool Available
    {
        get
        {
            var inv = DormFX.Inventory;
            return !GameState.Has("ritual_n" + GameState.Night) && !(inv && inv.Has(itemId));
        }
    }

    void Awake()
    {
        rends = GetComponentsInChildren<Renderer>(true);
        cols = GetComponentsInChildren<Collider>(true);
    }

    void Update()
    {
        bool a = Available;
        if (a == shown) return;
        shown = a;
        foreach (var r in rends) if (r) r.enabled = a;
        foreach (var c in cols) if (c) c.enabled = a;
    }

    public string Prompt => "เก็บ" + itemId + " (ใช้ทำพิธี)";
    public bool CanInteract(GameObject interactor) => shown && !DormFX.UIBusy;

    public void Interact(GameObject interactor)
    {
        var inv = DormFX.Inventory;
        if (!inv || !inv.TryAdd(itemId)) return;
        DormFX.Msg("ได้" + itemId + "มาแล้ว", 2f);
    }
}
