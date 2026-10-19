using System.Collections;
using BlurredRoom;
using UnityEngine;

// รูปพี่บิ๊ก (รูปติดผนังเหนือหิ้งเทียนในล็อบบี้ = Painting_6): ดูแล้วสติ +67, วันละ 1 ครั้ง, ไม่นับเป็นแอ็กชัน
// รูปที่โชว์บนจอดึงจาก texture ของกรอบรูปเอง (เปลี่ยนรูปในฉาก = เปลี่ยนบนจอด้วย)
public class StaffPhoto : MonoBehaviour, IInteractable
{
    public static string FlagToday => "staff_n" + GameState.Night;

    bool showing;
    Texture photo;

    void Awake()
    {
        if (!GetComponentInChildren<Collider>()) gameObject.AddComponent<BoxCollider>();   // ให้เล็งแล้วกด E ได้
        var r = GetComponentInChildren<Renderer>();
        if (r) foreach (var m in r.sharedMaterials) if (m && m.mainTexture) { photo = m.mainTexture; break; }   // วัสดุที่มีรูป (ไม่ใช่กรอบ)
    }

    public string Prompt => GameState.Has(FlagToday) ? "รูปพี่บิ๊ก (ดูสุดหล่อไปแล้ว)" : "ดูรูปพี่บิ๊ก (+สติ, วันละครั้ง)";

    public bool CanInteract(GameObject interactor) => !showing && !DormFX.UIBusy;

    public void Interact(GameObject interactor)
    {
        if (GameState.Has(FlagToday)) { DormFX.Msg("ดูสุดหล่อไปแล้ว", 2.5f); return; }
        StartCoroutine(Show());
    }

    IEnumerator Show()
    {
        showing = true;
        DormFX.SetFlag(FlagToday);
        if (DormFX.Sanity) DormFX.Sanity.Restore(DormData.StaffPhotoSanity);
        DormFX.Msg("วันนี้พี่บิ๊กหล่อจังครับ (+" + DormData.StaffPhotoSanity + " สติ)", 4f);
        yield return DormOverlay.I.ShowStaff(2.5f, photo);
        showing = false;
    }
}
