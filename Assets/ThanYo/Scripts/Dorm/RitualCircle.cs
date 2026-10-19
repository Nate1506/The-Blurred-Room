using BlurredRoom;
using UnityEngine;

// วงพิธีในห้องเก็บของชั้น 3 (เควสตอนเย็น)
// ต้องมีธูป + ชอล์ก และต่อเบาะแสบนกระดานแล้ว -> ค้าง R 1 วิ x 3 รอบ -> สติ -10 -> ภาพแฟลชจากอดีต
// ใช้ระบบพิธีของเนท (PlayerRitual + IRitualTarget) ไม่ได้แก้โค้ดเนท
public class RitualCircle : MonoBehaviour, IRitualTarget, IInteractable
{
    public const int Rounds = 3;

    /// <summary>ทำพิธีครบ 3 รอบแล้ว (StoryDirector ฟังอันนี้เพื่อเล่นภาพแฟลช)</summary>
    public static event System.Action Completed;

    int rounds;
    float cooldownUntil;

    static string FlagTonight => "ritual_n" + GameState.Night;
    static bool BoardDone => GameState.Has("board_n" + GameState.Night);

    bool HasItems
    {
        get { var inv = DormFX.Inventory; return inv && inv.Has(DormData.Incense) && inv.Has(DormData.Chalk); }
    }

    // ---------- IRitualTarget (ค้าง R) ----------
    public string Prompt => "ทำพิธี (รอบ " + (rounds + 1) + "/" + Rounds + ")";
    public float RitualDuration => 1f;

    public bool CanPerformRitual(GameObject performer) =>
        DormFX.Phase == DayPhase.Eve && !GameState.Has(FlagTonight) && BoardDone && HasItems
        && Time.time >= cooldownUntil && !DormFX.UIBusy;

    public void OnRitualProgress(float normalized) { }
    public void OnRitualCancelled() { }

    public void OnRitualCompleted(GameObject performer)
    {
        rounds++;
        cooldownUntil = Time.time + 0.6f;   // ต้องปล่อยปุ่มก่อนค่อยกดรอบต่อไป
        DormFX.Play(DormFX.TapClip, transform.position, 0.8f);
        if (rounds < Rounds)
        {
            DormFX.Msg("รอบที่ " + rounds + "/" + Rounds + " — ปล่อยแล้วค้าง [R] อีกครั้ง", 2f);
            return;
        }

        rounds = 0;
        var inv = DormFX.Inventory;
        if (inv) { inv.Remove(DormData.Incense); inv.Remove(DormData.Chalk); }
        if (DormFX.Sanity) DormFX.Sanity.Drain(DormData.RitualSanity);
        DormFX.SetFlag(FlagTonight);
        Completed?.Invoke();
    }

    // ---------- IInteractable (กด E = บอกว่าขาดอะไร) ----------
    string IInteractable.Prompt => GameState.Has(FlagTonight) ? "วงพิธี (ทำแล้วคืนนี้)" : "วงพิธี — ต้องมีธูป + ชอล์ก";

    public bool CanInteract(GameObject interactor) => !DormFX.UIBusy && !CanPerformRitual(interactor);   // พร้อมทำพิธีแล้ว -> โชว์แค่ "ค้าง R"

    public void Interact(GameObject interactor)
    {
        if (GameState.Has(FlagTonight)) { DormFX.Msg("คืนนี้ทำพิธีไปแล้ว", 2.5f); return; }
        if (DormFX.Phase != DayPhase.Eve) { DormFX.Msg("พิธีต้องทำตอนเย็น", 2.5f); return; }
        if (!BoardDone) { DormFX.Msg("ต่อเบาะแสบนกระดานในห้องออฟฟิศก่อน", 3f); return; }
        if (!HasItems)
        {
            var inv = DormFX.Inventory;
            string need = "";
            if (!(inv && inv.Has(DormData.Incense))) need += " ธูป (บนเคาน์เตอร์ล็อบบี้)";
            if (!(inv && inv.Has(DormData.Chalk))) need += " ชอล์ก (ห้องซักผ้า ชั้น 1)";
            DormFX.Msg("ยังขาด:" + need, 3.5f);
            return;
        }
        DormFX.Msg("ค้าง [R] เพื่อทำพิธี 3 รอบ", 2.5f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Completed = null;
}
