using BlurredRoom;
using UnityEngine;

// ให้ผู้เล่นเดินทะลุ collider ของ object นี้ได้ แต่สายตา (raycast) ยังมองโดน
// ใช้กับ: พื้นที่กดประตู, ของที่หยิบได้, NPC  -> ไม่มีกำแพงล่องหน และเล็ง/กดง่ายขึ้น
public class PlayerPassThrough : MonoBehaviour
{
    void Start()
    {
        var pm = FindFirstObjectByType<PlayerMovement>();
        var cc = pm ? pm.GetComponent<CharacterController>() : null;
        if (!cc) return;
        foreach (var c in GetComponentsInChildren<Collider>(true)) Physics.IgnoreCollision(cc, c, true);
    }
}
