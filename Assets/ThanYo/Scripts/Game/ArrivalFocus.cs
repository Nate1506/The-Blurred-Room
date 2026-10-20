using System.Collections;
using BlurredRoom;
using UnityEngine;
using UnityEngine.Events;

// พอผู้เล่นเข้ามาในซีนผ่านจุดเกิดที่กำหนด (เช่น เดินเข้าประตูหน้าหอ = FromOutside)
// กล้องจะค่อยๆ หันไปมองเป้าหมาย (รูปพี่บิ๊ก) ค้างไว้แป๊บนึง แล้วหันกลับมาที่เดิม
// ใช้ CameraDynamics.SetFocus จึงไม่ไปยุ่งกับระบบมองของเนท — มุมเดิมของผู้เล่นไม่เปลี่ยน
//
// วิธีใช้: วางไว้ที่ไหนก็ได้ในซีน > ลากรูปใส่ Target > ปรับเวลาใน Inspector
// ทดสอบ: ตอน Play คลิกขวาที่หัวคอมโพเนนต์ > "ทดสอบเล่นตอนนี้"
public class ArrivalFocus : MonoBehaviour
{
    [Header("เป้าหมาย")]
    [Tooltip("สิ่งที่จะให้กล้องหันไปมอง")]
    public Transform target;
    [Tooltip("เลื่อนจุดที่มองจากตำแหน่งของ Target (หน่วยเมตร, พิกัดโลก) เช่น ยกขึ้นให้ตรงหน้าในรูป")]
    public Vector3 targetOffset = Vector3.zero;

    [Header("เล่นเมื่อไหร่")]
    [Tooltip("เล่นเมื่อผู้เล่นเข้าซีนผ่านจุดเกิด id เหล่านี้ (ว่างทั้งหมด = เล่นทุกครั้งที่เข้าซีน)")]
    public string[] triggerSpawnIds = { "FromOutside" };
    [Tooltip("เล่นแค่ครั้งแรกของเกม (เริ่มเกมใหม่จากเมนูจะเล่นอีก)")]
    public bool oncePerGame = true;
    [Tooltip("ชื่อธงที่ใช้จำว่าเล่นไปแล้ว (เปลี่ยนถ้ามี ArrivalFocus หลายตัว)")]
    public string playedFlag = "intro_focus_played";

    [Header("จังหวะ (วินาที)")]
    [Tooltip("รอก่อนเริ่มหัน (จอค่อยๆ สว่างตอนเข้าซีนใช้เวลา ~1.2 วิ)")]
    [Min(0f)] public float startDelay = 0.7f;
    [Min(0.05f)] public float panInTime = 1.4f;
    [Tooltip("ค้างมองเป้าหมายนานเท่าไร")]
    [Min(0f)] public float holdTime = 1.8f;
    [Min(0.05f)] public float panOutTime = 1.1f;
    [Tooltip("ความนุ่มของการหัน (แกน X = เวลา 0..1, แกน Y = ความคืบหน้า 0..1)")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("กล้อง")]
    [Tooltip("ซูมตอนมอง (องศา FOV, ติดลบ = ซูมเข้า, 0 = ไม่ซูม)")]
    [Range(-50f, 10f)] public float zoomFov = -32f;
    [Tooltip("ล็อกการเดิน/หันของผู้เล่นระหว่างแพนกล้อง")]
    public bool lockPlayer = true;

    [Header("ข้อความ / เสียง (ไม่ใส่ก็ได้)")]
    [Tooltip("ข้อความที่ขึ้นตอนกล้องค้างที่เป้าหมาย (ว่าง = ไม่ขึ้น)")]
    [TextArea] public string message = "";
    [Min(0.5f)] public float messageSeconds = 3f;
    public AudioClip sound;
    [Range(0f, 1f)] public float soundVolume = 0.6f;

    [Header("Events")]
    public UnityEvent onStart = new UnityEvent();
    public UnityEvent onFinished = new UnityEvent();

    public bool IsPlaying { get; private set; }

    void Start()
    {
        if (!ShouldAutoPlay()) return;
        Play();
    }

    bool ShouldAutoPlay()
    {
        if (!target) return false;
        if (oncePerGame && GameState.Has(playedFlag)) return false;
        if (triggerSpawnIds == null || triggerSpawnIds.Length == 0) return true;
        foreach (var id in triggerSpawnIds) if (id == GameState.SpawnId) return true;
        return false;
    }

    /// <summary>เล่นทันที (เรียกจาก UnityEvent / สคริปต์อื่นได้)</summary>
    [ContextMenu("ทดสอบเล่นตอนนี้")]
    public void Play()
    {
        if (!Application.isPlaying || IsPlaying || !target) return;
        StartCoroutine(Run());
    }

    Vector3 Point => target.position + targetOffset;

    IEnumerator Run()
    {
        IsPlaying = true;
        GameState.Set(playedFlag);
        var input = DormFX.Input;
        if (lockPlayer && input) input.LockMovement(this);
        onStart?.Invoke();

        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        for (float t = 0f; t < panInTime; t += Time.deltaTime)
        {
            CameraDynamics.SetFocus(Point, ease.Evaluate(t / panInTime), zoomFov);
            yield return null;
        }
        CameraDynamics.SetFocus(Point, 1f, zoomFov);

        if (!string.IsNullOrEmpty(message)) DormFX.Msg(message, messageSeconds);
        if (sound) DormFX.Play2D(sound, soundVolume);

        for (float t = 0f; t < holdTime; t += Time.deltaTime)
        {
            CameraDynamics.SetFocus(Point, 1f, zoomFov);
            yield return null;
        }

        for (float t = 0f; t < panOutTime; t += Time.deltaTime)
        {
            CameraDynamics.SetFocus(Point, 1f - ease.Evaluate(t / panOutTime), zoomFov);
            yield return null;
        }
        CameraDynamics.ClearFocus();

        if (lockPlayer && input) input.UnlockMovement(this);
        IsPlaying = false;
        onFinished?.Invoke();
    }

    void OnDisable()
    {
        if (!IsPlaying) return;
        CameraDynamics.ClearFocus();
        var input = DormFX.Input;
        if (lockPlayer && input) input.UnlockMovement(this);
        IsPlaying = false;
    }

    void OnDrawGizmosSelected()
    {
        if (!target) return;
        Gizmos.color = new Color(1f, 0.75f, 0.3f);
        Gizmos.DrawWireSphere(Point, 0.15f);
        foreach (var s in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
            foreach (var id in triggerSpawnIds)
                if (s.id == id) Gizmos.DrawLine(s.transform.position + Vector3.up * 1.5f, Point);
    }
}
