using BlurredRoom;
using UnityEngine;

// เดโมเล่นถึงชั้น 3 เท่านั้น: ถ้าผู้เล่นเดินขึ้นบันไดเกินครึ่งทางไปชั้น 4 จะถูกดึงกลับลงมา
public class UpperFloorGuard : MonoBehaviour
{
    [Tooltip("สูงกว่านี้ = กำลังขึ้นชั้น 4 (ชั้น 3 อยู่ที่ y 5.6, ชั้น 4 ที่ 8.4)")]
    public float maxY = 7.2f;
    public string message = "ชั้นบนปิดซ่อม... ห้ามขึ้นไป";

    Transform player;
    CharacterController cc;
    Vector3 lastSafe;
    float nextMsg;

    void Start()
    {
        player = DormFX.Player;
        if (player) { cc = player.GetComponent<CharacterController>(); lastSafe = player.position; }
    }

    void LateUpdate()
    {
        if (!player) return;
        float y = player.position.y;
        if (y < maxY - 0.9f) { lastSafe = player.position; return; }
        if (y <= maxY) return;

        if (cc) cc.enabled = false;
        player.position = lastSafe;
        if (cc) cc.enabled = true;
        if (Time.time > nextMsg) { DormFX.Msg(message, 2.5f); nextMsg = Time.time + 3f; }
    }
}
