using System.Collections;
using BlurredRoom;
using UnityEngine;

// ฉากจบเดโม: รอดจนเช้า -> จอดำ -> ข้อความทิ้งท้าย -> หน้าจบ (กด E กลับเมนู)
public class NightEnding : MonoBehaviour
{
    [Tooltip("รอก่อนเริ่มฉากจบ")]
    [Min(0f)] public float delay = 3f;
    [Tooltip("ข้อความสุดท้ายก่อนจอดำ (ซับไตเติล)")]
    [TextArea] public string lastWhisper = "...ไว้เจอกันคืนพรุ่งนี้นะ";
    [Min(0.1f)] public float fadeSeconds = 2.5f;
    [Header("หน้าจบ")]
    public string title = "รอดคืนแรก";
    [TextArea(3, 8)] public string body = "06:02 น. ไฟกลับมาติดทั้งตึก\nห้อง 101 ยังล็อกจากด้านใน... ทั้งที่ลุงสมพงษ์ไม่อยู่\n\n<size=24>— จบเดโม —</size>";
    public AudioClip whisperClip;

    [ContextMenu("เล่นฉากจบ (ทดสอบ)")]
    public void Play() => StartCoroutine(Run());

    IEnumerator Run()
    {
        yield return new WaitForSeconds(delay);
        if (!string.IsNullOrEmpty(lastWhisper))
        {
            DormFX.Play2D(whisperClip ? whisperClip : NightAudio.Whisper, 0.9f, 0.85f);
            DormFX.Sub(lastWhisper, 3f);
            yield return new WaitForSeconds(2.5f);
        }
        var input = DormFX.Input;
        if (input) input.LockMovement(this);
        if (GameHUD.Instance) yield return GameHUD.Instance.FadeTo(1f, fadeSeconds);
        GameState.Ended = true;
        if (GameHUD.Instance) GameHUD.Instance.ShowEnding(title, body);
    }
}
