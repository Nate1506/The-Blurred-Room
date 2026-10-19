using BlurredRoom;
using UnityEngine;

// วางไว้ 1 ตัวต่อซีน ทำงานก่อนสคริปต์อื่น:
// 1) คืนค่าจาก GameState (ช่วงเวลา ไอเทม เบาะแส ธง ค่าสติ) 2) ย้ายผู้เล่นไปจุดเกิด 3) ลบของที่เคยเก็บไปแล้ว
// 4) คอยจดทุกการเปลี่ยนแปลงกลับเข้า GameState เพื่อพาไปซีนถัดไป
[DefaultExecutionOrder(-500)]
public class SceneBootstrap : MonoBehaviour
{
    TimeOfDayManager time;
    PlayerInventory inventory;
    PlayerSanity sanity;
    PlayerPickup pickup;
    ClueManager clues;

    void Start()
    {
        time = TimeOfDayManager.Instance;
        inventory = FindFirstObjectByType<PlayerInventory>();
        sanity = FindFirstObjectByType<PlayerSanity>();
        pickup = FindFirstObjectByType<PlayerPickup>();
        clues = ClueManager.Instance;

        // ---- restore ----
        if (time) { time.AutoAdvance = false; time.SetPhase(GameState.Phase); }
        if (inventory) foreach (var id in GameState.Items.ToArray()) inventory.TryAdd(id);
        if (clues)
        {
            foreach (var c in GameState.Clues) clues.Discover(c);
            foreach (var f in GameState.Flags) clues.SetFlag(f);
        }
        if (sanity && GameState.Sanity >= 0f) sanity.Drain(sanity.Max - GameState.Sanity);

        foreach (var p in FindObjectsByType<PickupItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (GameState.TakenPickups.Contains(p.ItemId)) Destroy(p.gameObject);

        SpawnPlayer();

        // ---- record from now on ----
        if (time) time.PhaseChanged += ph => GameState.Phase = ph;
        if (inventory)
        {
            inventory.ItemAdded += id => GameState.Items.Add(id);
            inventory.ItemRemoved += id => GameState.Items.Remove(id);
        }
        if (pickup) pickup.PickedUp += item => GameState.TakenPickups.Add(item.ItemId);
        if (clues)
        {
            clues.ClueDiscovered += c => { if (!GameState.Clues.Contains(c)) GameState.Clues.Add(c); };
            clues.FlagChanged += (f, on) => { if (on) GameState.Flags.Add(f); else GameState.Flags.Remove(f); };
        }
        if (sanity) sanity.Changed += (cur, max) => GameState.Sanity = cur;
    }

    void SpawnPlayer()
    {
        var player = FindFirstObjectByType<PlayerMovement>();
        if (!player) return;
        SpawnPoint target = null;
        foreach (var s in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
        {
            if (!string.IsNullOrEmpty(GameState.SpawnId) && s.id == GameState.SpawnId) { target = s; break; }
            if (s.isDefault && target == null) target = s;
        }
        if (!target) return;
        var cc = player.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        player.transform.SetPositionAndRotation(target.transform.position, Quaternion.Euler(0f, target.transform.eulerAngles.y, 0f));
        if (cc) cc.enabled = true;
    }
}
