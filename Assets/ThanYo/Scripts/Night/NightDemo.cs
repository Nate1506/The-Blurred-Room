using System.Collections;
using System.Collections.Generic;
using BlurredRoom;
using UnityEngine;
using UnityEngine.Events;

// ตัวกำกับ "เดโมคืนที่ 1" — เล่นเป็นฉากๆ ทีละขั้น (Step) มีเป้าหมายบรรทัดเดียวบนจอเสมอ
//
// วิธีทำงาน:
//   - Steps เรียงลำดับใน Inspector: แต่ละขั้นมี id, ข้อความเป้าหมาย, Events ตอนเริ่ม/จบ
//   - ของในฉาก (NightTrigger, BreakerSwitch, NightVisits ฯลฯ) เรียก NightDemo.Complete("id") เมื่อผู้เล่นทำขั้นนั้นเสร็จ
//   - ขั้นที่ติ๊ก Checkpoint = จุดเกิดใหม่ตอนตาย (ผีจับ / เปิดประตูผิด)
//   - ขั้นที่ทำเสร็จแล้วจำไว้ใน GameState (ข้ามซีนได้, เริ่มเกมใหม่จากเมนูจะล้าง)
//
// ใช้ได้ทั้งซีน ThanYo (ลานหน้าหอ) และ Inside — ซีนละ 1 ตัว
[DefaultExecutionOrder(-50)]
public class NightDemo : MonoBehaviour
{
    public static NightDemo I { get; private set; }
    /// <summary>มีเดโมทำงานอยู่ในซีนนี้ไหม (ระบบเก่าใช้เช็คว่าต้องหลบไหม)</summary>
    public static bool Active => I && I.isActiveAndEnabled;
    /// <summary>ผู้เล่นตายแล้วเกิดใหม่ (ผี/ระบบต่างๆ ฟังเพื่อรีเซ็ตตัวเอง)</summary>
    public static event System.Action Respawned;

    [System.Serializable]
    public class Step
    {
        [Tooltip("ชื่อขั้น (ใช้อ้างอิงจาก trigger) ห้ามซ้ำ")]
        public string id = "step";
        [Tooltip("เป้าหมายที่ขึ้นบนจอ (สั้นๆ บรรทัดเดียว)")]
        public string objective = "";
        [Tooltip("ตายแล้วกลับมาเริ่มที่ขั้นนี้")]
        public bool checkpoint;
        [Tooltip("จุดเกิดตอนตาย (ใช้เมื่อติ๊ก Checkpoint) ว่าง = จุดที่ผู้เล่นยืนตอนเริ่มขั้น")]
        public Transform respawnPoint;
        [Tooltip("รอกี่วินาทีหลังจบขั้นนี้ ก่อนขึ้นขั้นต่อไป")]
        [Min(0f)] public float delayAfter = 0.5f;
        [Tooltip("ข้อความที่ขึ้นกลางจอตอนเริ่มขั้น (ว่าง = ไม่ขึ้น)")]
        [TextArea] public string messageOnBegin = "";
        public UnityEvent onBegin = new UnityEvent();
        public UnityEvent onComplete = new UnityEvent();
        [Tooltip("เรียกเมื่อเกมเริ่มโดยข้ามขั้นนี้ไปแล้ว (เช่น ปุ่มลัดดูเดโม) — ใช้คืนสภาพ เช่น ให้กล้อง/ไฟฉาย, ปิดไฟ")]
        public UnityEvent onRestore = new UnityEvent();
    }

    [Header("เริ่มเมื่อไหร่")]
    [Tooltip("เริ่มขั้นแรกทันทีที่เข้าซีน (ปิด = รอให้ระบบอื่นเรียก StartDemo() เช่น StoryDirector ตอนเข้ากลางคืน)")]
    public bool autoStart = true;

    [Header("หัวข้อบนจอ")]
    public string header = "คืนที่ 1";

    [Header("เวลาในเกม")]
    [Tooltip("ตั้งช่วงเวลาตอนเข้าซีน")]
    public bool setPhaseOnStart = true;
    public DayPhase startPhase = DayPhase.Eve;

    [Header("ปิดระบบเก่า (วันละ 4 แอ็กชัน / กระดาน / พิธี ฯลฯ)")]
    [Tooltip("ออบเจกต์ที่จะถูกปิดตอนเริ่ม (ระบบเดิมที่ไม่ใช้ในเดโม)")]
    public GameObject[] disableOnStart;

    [Header("ขั้นตอน (เรียงตามลำดับ)")]
    public List<Step> steps = new List<Step>();

    [Header("ตาย / เริ่มใหม่")]
    [TextArea] public string deathText = "...";
    [Min(0.1f)] public float deathFadeOut = 1.2f;
    [Min(0f)] public float deathHoldBlack = 1.5f;
    [Min(0.1f)] public float deathFadeIn = 1.5f;

    [Header("สถานะ (อ่านอย่างเดียว)")]
    [SerializeField] int currentIndex = -1;
    [SerializeField] string currentId = "";
    [SerializeField] int checkpointIndex = 0;

    Vector3 checkpointPos;
    Quaternion checkpointRot;
    bool dying, advancing;

    public Step Current => currentIndex >= 0 && currentIndex < steps.Count ? steps[currentIndex] : null;
    public bool IsDying => dying;

    static string Flag(string id) => "nd_" + id;

    void Awake()
    {
        I = this;
        if (disableOnStart != null) foreach (var g in disableOnStart) if (g) g.SetActive(false);
    }

    void OnDestroy() { if (I == this) I = null; }

    IEnumerator Start()
    {
        if (setPhaseOnStart)
        {
            GameState.Phase = startPhase;
            if (TimeOfDayManager.Instance) TimeOfDayManager.Instance.SetPhase(startPhase);
        }
        yield return null;   // ให้ SceneBootstrap ย้ายผู้เล่นไปจุดเกิดก่อน
        if (autoStart) StartDemo();
    }

    public bool Started { get; private set; }

    /// <summary>เริ่มเดโม (ขั้นแรกที่ยังไม่เสร็จ)</summary>
    public void StartDemo()
    {
        if (Started) return;
        Started = true;
        int first = 0;
        while (first < steps.Count && GameState.Has(Flag(steps[first].id))) { steps[first].onRestore?.Invoke(); first++; }
        if (first < steps.Count) Begin(first);
        else Hud("");
    }

    // =====================================================================
    //  public API
    // =====================================================================
    public static bool IsCurrent(string id) => I && I.Current != null && I.Current.id == id;
    public static bool IsDone(string id) => GameState.Has(Flag(id));

    /// <summary>บอกว่าขั้น id ทำเสร็จแล้ว (ถ้าไม่ใช่ขั้นปัจจุบันจะไม่สนใจ)</summary>
    public static void Complete(string id)
    {
        if (!I || I.dying || I.advancing) return;
        if (!IsCurrent(id)) return;
        I.StartCoroutine(I.Advance());
    }

    /// <summary>ใช้กับ UnityEvent (ปุ่ม/ทริกเกอร์ที่ต่อจาก Inspector)</summary>
    public void CompleteStep(string id) => Complete(id);

    /// <summary>ผู้เล่นตาย -> จอดำ -> เกิดใหม่ที่ checkpoint ล่าสุด</summary>
    public static void Kill(string reason = null)
    {
        if (!I || I.dying) return;
        I.StartCoroutine(I.DeathRoutine(reason));
    }

    // =====================================================================
    //  flow
    // =====================================================================
    void Begin(int index)
    {
        currentIndex = index;
        var s = steps[index];
        currentId = s.id;
        if (s.checkpoint)
        {
            checkpointIndex = index;
            var p = DormFX.Player;
            if (s.respawnPoint) { checkpointPos = s.respawnPoint.position; checkpointRot = s.respawnPoint.rotation; }
            else if (p) { checkpointPos = p.position; checkpointRot = p.rotation; }
        }
        Hud(s.objective);
        if (!string.IsNullOrEmpty(s.messageOnBegin)) DormFX.Msg(s.messageOnBegin, 4.5f);
        s.onBegin?.Invoke();
    }

    IEnumerator Advance()
    {
        advancing = true;
        var s = Current;
        GameState.Set(Flag(s.id));
        Hud(s.objective, true);
        s.onComplete?.Invoke();
        if (s.delayAfter > 0f) yield return new WaitForSeconds(s.delayAfter);
        advancing = false;
        int next = currentIndex + 1;
        if (next < steps.Count) Begin(next);
        else { currentIndex = steps.Count; currentId = ""; Hud(""); }
    }

    IEnumerator DeathRoutine(string reason)
    {
        dying = true;
        var input = DormFX.Input;
        if (input) input.LockMovement(this);
        var hud = GameHUD.Instance;
        if (hud) yield return hud.FadeTo(1f, deathFadeOut);
        string text = string.IsNullOrEmpty(reason) ? deathText : reason;
        if (!string.IsNullOrEmpty(text)) DormFX.Msg(text, deathHoldBlack + 1f);
        yield return new WaitForSeconds(deathHoldBlack);

        // ย้อนสถานะทุกขั้นตั้งแต่ checkpoint
        for (int i = checkpointIndex; i < steps.Count; i++) GameState.Flags.Remove(Flag(steps[i].id));
        CameraDynamics.ClearFocus();
        var p = DormFX.Player;
        if (p && checkpointPos != Vector3.zero)
        {
            var cc = p.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            p.SetPositionAndRotation(checkpointPos, Quaternion.Euler(0f, checkpointRot.eulerAngles.y, 0f));
            if (cc) cc.enabled = true;
        }
        var san = DormFX.Sanity;
        if (san) san.Restore(san.Max);
        Respawned?.Invoke();

        StopCoroutinesExceptThis();
        advancing = false;
        Begin(checkpointIndex);
        if (input) input.UnlockMovement(this);
        if (hud) yield return hud.FadeTo(0f, deathFadeIn);
        dying = false;
    }

    void StopCoroutinesExceptThis() { /* ขั้นตอนต่างๆ รีเซ็ตตัวเองผ่าน Respawned */ }

    void Hud(string objective, bool done = false)
    {
        if (!GameHUD.Instance) return;
        var list = new List<(string, bool)>();
        if (!string.IsNullOrEmpty(objective)) list.Add((objective, done));
        GameHUD.Instance.SetObjectives(header, list);
    }

    // ---------- เครื่องมือทดสอบใน Inspector ----------
    [ContextMenu("ข้ามขั้นปัจจุบัน (ทดสอบ)")]
    void DebugSkip() { if (Current != null) Complete(Current.id); }

    [ContextMenu("ตาย (ทดสอบ checkpoint)")]
    void DebugKill() => Kill();
}
