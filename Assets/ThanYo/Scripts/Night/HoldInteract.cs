using BlurredRoom;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// ของที่ต้อง "ค้าง E" ถึงจะสำเร็จ (เช่น คันโยกเบรกเกอร์) — ระหว่างค้างหันหลังให้ทางเดิน = ลุ้น
// ปล่อยมือก่อนครบ = เริ่มใหม่ (หรือค่อยๆ ลดลง ตามที่ตั้ง)
public class HoldInteract : MonoBehaviour, IInteractable
{
    [Header("เงื่อนไข")]
    [Tooltip("ใช้ได้เฉพาะตอนเดโมอยู่ขั้นนี้ (ว่าง = ตลอด)")]
    public string onlyDuringStep = "";
    public string prompt = "ค้าง [E] ดันคันโยก";
    [Tooltip("ระยะไกลสุดที่ยังนับว่ากำลังค้างอยู่")]
    [Min(0.5f)] public float maxDistance = 2.4f;

    [Header("การค้าง")]
    [Min(0.1f)] public float holdSeconds = 3f;
    [Tooltip("ปล่อยแล้วความคืบหน้าลดลงต่อวินาที (สูง = เริ่มใหม่เกือบทันที)")]
    [Min(0f)] public float decayPerSecond = 0.6f;
    [Tooltip("ระหว่างค้าง ไฟกะพริบ/เสียงเหล็กครูด")]
    public bool tensionEffects = true;

    [Header("เสียงตอนเริ่มค้างครั้งแรก")]
    [Tooltip("เล่นครั้งเดียวตอนผู้เล่นเริ่มค้างครั้งแรก (เช่น เสียงคนวิ่งขึ้นบันไดข้างนอก)")]
    public AudioClip firstHoldClip;
    [Tooltip("เสียงดังจากตรงไหน (ว่าง = ที่ตัวนี้)")]
    public Transform firstHoldClipAt;
    [Range(0f, 1f)] public float firstHoldVolume = 0.9f;

    [Header("ผลลัพธ์")]
    public string completeStep = "";
    public string message = "";
    public AudioClip doneClip;
    public UnityEvent onDone = new UnityEvent();

    [Header("กล่องให้เล็ง")]
    public Vector3 interactSize = new Vector3(0.5f, 0.6f, 0.3f);

    float progress;
    bool holding, done;
    Image bar;
    CanvasGroup group;
    float lastShake;

    bool Armed => !done && (string.IsNullOrEmpty(onlyDuringStep) || NightDemo.IsCurrent(onlyDuringStep));

    void Awake()
    {
        if (!GetComponent<Collider>()) { var b = gameObject.AddComponent<BoxCollider>(); b.size = interactSize; }
        BuildBar();
        NightDemo.Respawned += () => { if (!string.IsNullOrEmpty(onlyDuringStep) && !NightDemo.IsDone(onlyDuringStep)) { done = false; progress = 0f; playedFirstHold = false; } };
    }

    public string Prompt => prompt;
    public bool CanInteract(GameObject interactor) => Armed && !DormFX.UIBusy;
    public void Interact(GameObject interactor)
    {
        if (!Armed) return;
        holding = true;
        if (firstHoldClip && !playedFirstHold)
        {
            playedFirstHold = true;
            DormFX.Play(firstHoldClip, firstHoldClipAt ? firstHoldClipAt.position : transform.position, firstHoldVolume);
        }
    }

    bool playedFirstHold;

    void Update()
    {
        var p = DormFX.Player;
        if (holding && (!EHeld() || !p || Vector3.Distance(p.position, transform.position) > maxDistance || !Armed)) holding = false;

        if (holding)
        {
            progress += Time.deltaTime / holdSeconds;
            if (tensionEffects && Time.time - lastShake > 0.5f)
            {
                lastShake = Time.time;
                DormFX.Play(NightAudio.Clunk, transform.position, 0.15f);
                CameraDynamics.Shake(0.04f);
            }
            if (progress >= 1f) Finish();
        }
        else progress = Mathf.Max(0f, progress - decayPerSecond * Time.deltaTime);

        group.alpha = Mathf.MoveTowards(group.alpha, progress > 0.001f && !done ? 1f : 0f, Time.deltaTime * 6f);
        bar.fillAmount = progress;
    }

    void Finish()
    {
        done = true; holding = false; progress = 1f;
        DormFX.Play(doneClip ? doneClip : NightAudio.Clunk, transform.position, 1f);
        if (!string.IsNullOrEmpty(message)) DormFX.Msg(message, 3.5f);
        onDone?.Invoke();
        if (!string.IsNullOrEmpty(completeStep)) NightDemo.Complete(completeStep);
    }

    void BuildBar()
    {
        var go = new GameObject("HoldBar", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        go.transform.SetParent(transform, false);
        var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 40;
        var sc = go.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080);
        group = go.GetComponent<CanvasGroup>(); group.alpha = 0f; group.blocksRaycasts = false;
        var bg = new GameObject("BG", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        bg.transform.SetParent(go.transform, false);
        var r = bg.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.42f); r.sizeDelta = new Vector2(320, 10);
        bg.color = new Color(0, 0, 0, 0.6f);
        var spr = Resources.Load<Sprite>("UI/bar");
        bar = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        bar.transform.SetParent(bg.transform, false);
        var fr = bar.rectTransform; fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = fr.offsetMax = Vector2.zero;
        bar.sprite = spr; bar.type = Image.Type.Filled; bar.fillMethod = Image.FillMethod.Horizontal; bar.color = new Color(0.92f, 0.9f, 0.85f);
    }

#if ENABLE_INPUT_SYSTEM
    static bool EHeld() => Keyboard.current != null && Keyboard.current.eKey.isPressed;
#else
    static bool EHeld() => Input.GetKey(KeyCode.E);
#endif

    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireCube(Vector3.zero, interactSize);
    }
}
