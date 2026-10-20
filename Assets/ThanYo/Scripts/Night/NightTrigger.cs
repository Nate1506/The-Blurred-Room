using BlurredRoom;
using UnityEngine;
using UnityEngine.Events;

// ทริกเกอร์อเนกประสงค์ของเดโม:
//   EnterZone = ผู้เล่นเดินเข้ากล่อง (เส้นเหลืองใน Scene view)
//   PressE    = เล็งแล้วกด E (เช่น หยิบกล้อง, เปิดเทป, ปิดไฟ)
// ทำงานได้เฉพาะตอนขั้นที่กำหนด (onlyDuringStep) แล้ว Complete ขั้นนั้น และ/หรือ ยิง Events
public class NightTrigger : MonoBehaviour, IInteractable
{
    public enum Mode { EnterZone, PressE }

    public Mode mode = Mode.PressE;

    [Header("เงื่อนไข")]
    [Tooltip("ทำงานเฉพาะตอนเดโมอยู่ที่ขั้นนี้ (ว่าง = ทำงานได้ตลอด)")]
    public string onlyDuringStep = "";
    [Tooltip("ทำงานครั้งเดียว")]
    public bool once = true;

    [Header("ผลลัพธ์")]
    [Tooltip("ทำขั้นนี้ให้เสร็จ (ว่าง = ไม่ Complete อะไร)")]
    public string completeStep = "";
    [Tooltip("ซ่อนออบเจกต์นี้หลังทำงาน (เช่น โมเดลของที่หยิบไปแล้ว)")]
    public GameObject hideAfter;
    [Tooltip("ข้อความกลางจอหลังทำงาน (ว่าง = ไม่ขึ้น)")]
    public string message = "";
    public AudioClip sound;
    [Range(0f, 1f)] public float soundVolume = 0.7f;
    public UnityEvent onTriggered = new UnityEvent();

    [Header("PressE")]
    [Tooltip("ข้อความตอนเล็ง")]
    public string prompt = "ตรวจดู";
    [Tooltip("ขนาดกล่องให้เล็งโดน (สร้าง BoxCollider ให้อัตโนมัติถ้ายังไม่มี)")]
    public Vector3 interactSize = new Vector3(0.5f, 0.5f, 0.5f);

    [Header("EnterZone")]
    public Vector3 zoneSize = new Vector3(2f, 2.4f, 2f);

    bool fired;

    public bool Armed => !(once && fired) && (string.IsNullOrEmpty(onlyDuringStep) || NightDemo.IsCurrent(onlyDuringStep));

    void Awake()
    {
        if (mode == Mode.PressE && !GetComponent<Collider>())
        {
            var b = gameObject.AddComponent<BoxCollider>();
            b.size = interactSize;
        }
        NightDemo.Respawned += OnRespawn;
    }

    void OnDestroy() => NightDemo.Respawned -= OnRespawn;

    void OnRespawn()
    {
        // ถ้าขั้นที่ผูกไว้ถูกย้อนกลับ ให้ใช้ได้อีกครั้ง
        if (!string.IsNullOrEmpty(onlyDuringStep) && !NightDemo.IsDone(onlyDuringStep))
        {
            fired = false;
            if (hideAfter) hideAfter.SetActive(true);
        }
    }

    void Update()
    {
        if (mode != Mode.EnterZone || !Armed) return;
        var p = DormFX.Player;
        if (!p) return;
        Vector3 local = transform.InverseTransformPoint(p.position + Vector3.up * 0.9f);
        Vector3 h = zoneSize * 0.5f;
        if (Mathf.Abs(local.x) <= h.x && Mathf.Abs(local.y) <= h.y && Mathf.Abs(local.z) <= h.z) Fire();
    }

    // ---------- IInteractable ----------
    public string Prompt => prompt;
    public bool CanInteract(GameObject interactor) => mode == Mode.PressE && Armed && !DormFX.UIBusy;
    public void Interact(GameObject interactor) { if (CanInteract(interactor)) Fire(); }

    [ContextMenu("ยิงเลย (ทดสอบ)")]
    public void Fire()
    {
        fired = true;
        if (sound) DormFX.Play(sound, transform.position, soundVolume);
        if (!string.IsNullOrEmpty(message)) DormFX.Msg(message, 3.5f);
        if (hideAfter) hideAfter.SetActive(false);
        onTriggered?.Invoke();
        if (!string.IsNullOrEmpty(completeStep)) NightDemo.Complete(completeStep);
    }

    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = mode == Mode.EnterZone ? new Color(1f, 0.85f, 0.2f, 0.9f) : new Color(0.3f, 1f, 0.6f, 0.9f);
        Gizmos.DrawWireCube(mode == Mode.EnterZone ? Vector3.zero : Vector3.zero, mode == Mode.EnterZone ? zoneSize : interactSize);
    }
}
