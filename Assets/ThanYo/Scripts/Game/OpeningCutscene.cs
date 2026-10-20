using System.Collections;
using System.Collections.Generic;
using BlurredRoom;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// คัตซีนเปิดเกม (ซีน ThanYo): เฟดจากดำ -> แท็กซี่แล่นเข้ามาจอดกลางเฟรมพร้อมเสียงรถ -> เสียงจบ = ตัดดำ
// -> เสียงปิดประตูรถ -> ค่อยๆ เฟดเข้าเกม (ผู้เล่นยืนหน้าหอ)
// ฟีลหนัง: แถบดำบน-ล่าง, กล้องดอลลี่ช้าๆ + สั่นแบบถือมือ, ตัวหนังสือชื่อสถานที่
//
// ปรับมุมกล้อง: ย้าย/หมุนลูก "Camera_Point" หรือคลิกขวาที่คอมโพเนนต์ -> "ตั้งกล้องตามมุม Scene View"
// ปรับทางรถ: ย้าย "Taxi_Start" (จุดเริ่ม นอกจอ) กับ "Taxi_Park" (จุดจอด)
public class OpeningCutscene : MonoBehaviour
{
    /// <summary>กำลังเล่นคัตซีนอยู่ (ระบบอื่นรอได้ เช่น ข้อความต้อนรับ)</summary>
    public static bool Playing { get; private set; }
    /// <summary>คัตซีนขึ้นชื่อสถานที่ไปแล้ว (ข้อความต้อนรับไม่ต้องขึ้นซ้ำ)</summary>
    public static bool ShowedTitle { get; private set; }

    public enum AfterCutscene { Stay, Hide }

    [Header("เล่นเมื่อไหร่")]
    [Tooltip("เล่นเมื่อเข้าซีนผ่านจุดเกิดเหล่านี้ (Start = กดเริ่มเกมจากเมนู)")]
    public string[] triggerSpawnIds = { "Start" };
    [Tooltip("เล่นครั้งเดียวต่อเกม")]
    public bool playOnce = true;
    public string doneFlag = "cut_opening";
    [Tooltip("ทดสอบ: กด Play ตรงๆ ในซีนนี้ (ไม่ผ่านเมนู) ก็เล่นด้วย")]
    public bool playWhenNoSpawnId = false;

    [Header("กล้อง")]
    [Tooltip("ตำแหน่ง+มุมกล้อง")]
    public Transform cameraPoint;
    [Range(15f, 90f)] public float fieldOfView = 60f;
    [Tooltip("ตัดของที่อยู่ใกล้กล้องเกินระยะนี้ออก (กันพุ่มไม้บังหน้ากล้อง)")]
    [Range(0.05f, 5f)] public float nearClip = 1.5f;
    [Tooltip("กล้องค่อยๆ เคลื่อนเข้าไปข้างหน้ากี่เมตร ตลอดคัตซีน")]
    [Range(0f, 5f)] public float dollyForward = 0.6f;
    [Tooltip("สั่นแบบถือกล้องด้วยมือ (0 = นิ่งสนิท)")]
    [Range(0f, 3f)] public float handheld = 0.6f;
    [Tooltip("กล้องหันตามรถนิดๆ (0 = ไม่ตาม, 1 = ล็อกที่รถ)")]
    [Range(0f, 1f)] public float followTaxi = 0.2f;

    [Header("แท็กซี่")]
    public Transform taxi;
    [Tooltip("จุดเริ่ม (นอกจอ)")]
    public Transform startPoint;
    [Tooltip("จุดจอด (กลางเฟรม)")]
    public Transform parkPoint;
    [Tooltip("รถเริ่มวิ่งตอนวินาทีที่เท่าไรของเสียงรถ")]
    [Min(0f)] public float moveStartAt = 8.5f;
    [Tooltip("รถจอดสนิทตอนวินาทีที่เท่าไรของเสียงรถ (ให้ตรงจังหวะเบรกในเสียง)")]
    [Min(0.1f)] public float arriveAt = 14.2f;
    [Tooltip("ความแรงของการเบรก (1 = ความเร็วคงที่, 3 = มาเร็วแล้วเบรกนุ่มๆ)")]
    [Range(1f, 5f)] public float braking = 2.6f;
    [Tooltip("หน้ารถยุบลงตอนเบรก (องศา)")]
    [Range(0f, 6f)] public float brakeDip = 1.8f;
    [Tooltip("หลังจบคัตซีน: Stay = จอดอยู่ตรงนั้น, Hide = รถออกไปแล้ว")]
    public AfterCutscene afterCutscene = AfterCutscene.Hide;

    [Header("ไฟหน้ารถ")]
    public bool headlights = true;
    [Range(0f, 50f)] public float headlightIntensity = 12f;
    public Color headlightColor = new Color(1f, 0.93f, 0.78f);
    [Tooltip("ทิศหน้ารถเทียบกับทิศที่วิ่ง (ปกติ 0)")]
    public float headlightYawOffset = 0f;

    [Header("เสียง")]
    public AudioClip arriveClip;
    [Range(0f, 1f)] public float arriveVolume = 1f;
    public AudioClip doorClip;
    [Range(0f, 1f)] public float doorVolume = 1f;
    [Tooltip("ตัดดำก่อนเสียงรถจบกี่วินาที (0 = ตัดตอนเสียงจบพอดี)")]
    [Min(0f)] public float cutBeforeEnd = 0f;
    [Tooltip("จอดำแล้วรอกี่วินาทีก่อนเสียงปิดประตู")]
    [Min(0f)] public float doorDelay = 0.45f;

    [Header("จังหวะภาพ")]
    [Tooltip("เฟดจากดำตอนเริ่ม")]
    [Min(0f)] public float fadeIn = 2.5f;
    [Tooltip("ค้างจอดำหลังเสียงปิดประตู")]
    [Min(0f)] public float blackHold = 1.6f;
    [Tooltip("เฟดเข้าเกม")]
    [Min(0f)] public float fadeToGame = 2.2f;

    [Header("ฟีลหนัง")]
    [Tooltip("ความสูงแถบดำบน/ล่าง (สัดส่วนของจอ, 0.12 ≈ จอซีเนม่า 2.39:1)")]
    [Range(0f, 0.25f)] public float letterbox = 0.12f;
    [Tooltip("ชื่อสถานที่ (ว่าง = ไม่ขึ้น)")]
    public string captionTitle = "สุขนิรันดร์ แมนชั่น";
    public string captionSub = "ฝั่งธนบุรี";
    [Tooltip("ขึ้นตัวหนังสือตอนวินาทีที่")]
    [Min(0f)] public float captionAt = 3f;
    [Min(0.5f)] public float captionHold = 4.5f;

    [Header("ข้ามคัตซีน")]
    public bool allowSkip = true;
    public string skipHint = "[Space] ข้าม";

    public UnityEvent onFinished = new UnityEvent();

    // ------------------------------------------------------------------
    Camera cutCam, playerCam;
    AudioSource src;
    Canvas overlay;
    Image black;
    RectTransform barTop, barBottom;
    TextMeshProUGUI title, sub, hint;
    readonly List<Behaviour> hiddenCanvases = new List<Behaviour>();
    readonly List<Renderer> hiddenRenderers = new List<Renderer>();
    readonly List<Light> lights = new List<Light>();
    Vector3 taxiBasePos;
    Quaternion taxiBaseRot;
    bool skip;

    bool ShouldPlay()
    {
        if (playOnce && GameState.Has(doneFlag)) return false;
        if (string.IsNullOrEmpty(GameState.SpawnId)) return playWhenNoSpawnId;
        foreach (var id in triggerSpawnIds) if (id == GameState.SpawnId) return true;
        return false;
    }

    void Awake()
    {
        if (taxi) { taxiBasePos = taxi.position; taxiBaseRot = taxi.rotation; }
        if (!ShouldPlay())
        {
            // ไม่เล่น: ให้รถอยู่ในสภาพ "หลังคัตซีน"
            if (taxi)
            {
                if (afterCutscene == AfterCutscene.Hide && GameState.Has(doneFlag)) taxi.gameObject.SetActive(false);
                else if (parkPoint && GameState.Has(doneFlag)) taxi.position = parkPoint.position;
            }
            return;
        }
        Playing = true;
        BuildOverlay();
    }

    void OnDestroy() { if (overlay) Playing = false; }

    IEnumerator Start()
    {
        if (!Playing) yield break;
        yield return null;   // ให้ระบบจุดเกิดย้ายผู้เล่นก่อน
        yield return Run();
    }

    [ContextMenu("เล่นคัตซีน (ทดสอบ ตอน Play)")]
    void PlayNow()
    {
        if (!Application.isPlaying || Playing) return;
        Playing = true;
        BuildOverlay();
        StartCoroutine(Run());
    }

    // =====================================================================
    IEnumerator Run()
    {
        var input = DormFX.Input;
        if (input) input.LockMovement(this);
        BeginCamera();
        HideWorldUi(true);
        HidePlayer(true);
        if (headlights && taxi) MakeHeadlights();

        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false; src.spatialBlend = 0f;
        float clipLen = arriveClip ? arriveClip.length : arriveAt + 1f;
        float cutAt = Mathf.Max(0.5f, clipLen - cutBeforeEnd);
        if (arriveClip) { src.clip = arriveClip; src.volume = arriveVolume; src.Play(); }

        float t = 0f;
        while (t < cutAt && !skip)
        {
            t += Time.deltaTime;
            if (allowSkip && t > 1f && SkipPressed()) skip = true;

            // ภาพ: เฟดจากดำ + แถบดำ + ตัวหนังสือ
            black.color = new Color(0, 0, 0, fadeIn > 0f ? 1f - Mathf.Clamp01(t / fadeIn) : 0f);
            SetBars(1f);
            Caption(t);
            if (hint) hint.alpha = allowSkip ? Mathf.Clamp01((t - 1.5f) / 1f) * 0.35f : 0f;

            UpdateTaxi(t);
            UpdateCamera(t / Mathf.Max(0.1f, cutAt), t);
            yield return null;
        }

        // ---------- ตัดดำ (เสียงรถจบ) -> ปิดประตูรถ ----------
        if (src) src.Stop();
        black.color = Color.black;
        if (title) title.alpha = 0f;
        if (sub) sub.alpha = 0f;
        if (hint) hint.alpha = 0f;
        yield return new WaitForSeconds(doorDelay);
        if (doorClip) src.PlayOneShot(doorClip, doorVolume);
        yield return new WaitForSeconds(blackHold);

        // ---------- คืนการควบคุม ----------
        if (taxi)
        {
            foreach (var l in lights) if (l) Destroy(l.gameObject);
            if (afterCutscene == AfterCutscene.Hide) taxi.gameObject.SetActive(false);
            else if (parkPoint) taxi.SetPositionAndRotation(parkPoint.position, taxiBaseRot);
        }
        EndCamera();
        HidePlayer(false);
        HideWorldUi(false);
        if (input) input.UnlockMovement(this);
        GameState.Set(doneFlag);
        ShowedTitle = !string.IsNullOrEmpty(captionTitle);
        Playing = false;

        for (float k = 0f; k < fadeToGame; k += Time.deltaTime)
        {
            float a = 1f - k / fadeToGame;
            black.color = new Color(0, 0, 0, a);
            SetBars(Mathf.SmoothStep(0f, 1f, a));
            yield return null;
        }
        Destroy(overlay.gameObject);
        overlay = null;
        onFinished?.Invoke();
    }

    static bool SkipPressed()
    {
        var k = Keyboard.current;
        return k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame);
    }

    // =====================================================================
    //  taxi
    // =====================================================================
    void UpdateTaxi(float t)
    {
        if (!taxi || !startPoint || !parkPoint) return;
        float u = Mathf.Clamp01((t - moveStartAt) / Mathf.Max(0.1f, arriveAt - moveStartAt));
        float e = 1f - Mathf.Pow(1f - u, braking);      // มาเร็ว แล้วค่อยๆ เบรก
        Vector3 a = startPoint.position, b = parkPoint.position;
        Vector3 pos = Vector3.Lerp(a, b, e);
        pos.y = Mathf.Lerp(a.y, b.y, e);

        // หน้ารถยุบตอนเบรก แล้วเด้งกลับหลังจอด
        Vector3 dir = b - a; dir.y = 0f;
        Quaternion rot = taxiBaseRot;
        if (dir.sqrMagnitude > 0.001f && brakeDip > 0f)
        {
            float dip = 0f;
            if (u > 0.55f && u < 1f) dip = Mathf.Sin((u - 0.55f) / 0.45f * Mathf.PI * 0.5f);
            float after = t - arriveAt;
            if (u >= 1f) dip = after < 0.7f ? Mathf.Cos(after / 0.7f * Mathf.PI) * Mathf.Exp(-after * 3f) : 0f;
            Vector3 side = Vector3.Cross(Vector3.up, dir.normalized);
            rot = Quaternion.AngleAxis(dip * brakeDip, side) * taxiBaseRot;
        }
        taxi.SetPositionAndRotation(pos, rot);
    }

    void MakeHeadlights()
    {
        Vector3 dir = (parkPoint && startPoint) ? parkPoint.position - startPoint.position : taxi.forward;
        dir.y = 0f; dir = Quaternion.Euler(0f, headlightYawOffset, 0f) * dir.normalized;
        var rs = taxi.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        Bounds bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
        float halfLen = Mathf.Max(bb.extents.x, bb.extents.z);
        Vector3 side = Vector3.Cross(Vector3.up, dir);
        Vector3 front = new Vector3(bb.center.x, bb.min.y + bb.size.y * 0.45f, bb.center.z) + dir * halfLen * 0.92f;
        foreach (float s in new[] { -1f, 1f })
        {
            var go = new GameObject("Headlight");
            go.transform.SetParent(taxi, true);
            go.transform.SetPositionAndRotation(front + side * s * halfLen * 0.28f, Quaternion.LookRotation(dir + Vector3.down * 0.12f));
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot; l.color = headlightColor; l.intensity = headlightIntensity;
            l.range = 18f; l.spotAngle = 55f; l.innerSpotAngle = 25f; l.shadows = LightShadows.None;
            lights.Add(l);
        }
    }

    // =====================================================================
    //  camera
    // =====================================================================
    void BeginCamera()
    {
        var player = GameObject.Find("Player");
        playerCam = player ? player.GetComponentInChildren<Camera>() : Camera.main;
        var go = new GameObject("Cutscene_Camera");
        cutCam = go.AddComponent<Camera>();
        if (playerCam)
        {
            cutCam.CopyFrom(playerCam);
            var pd = playerCam.GetUniversalAdditionalCameraData();
            var cd = cutCam.GetUniversalAdditionalCameraData();
            cd.renderPostProcessing = pd.renderPostProcessing;
            cd.antialiasing = pd.antialiasing;
            cd.volumeLayerMask = pd.volumeLayerMask;
            cd.renderShadows = pd.renderShadows;
            playerCam.enabled = false;
        }
        cutCam.fieldOfView = fieldOfView;
        cutCam.nearClipPlane = nearClip;
        cutCam.depth = 50;
        UpdateCamera(0f, 0f);
    }

    void UpdateCamera(float k, float t)
    {
        if (!cutCam) return;
        Transform p = cameraPoint ? cameraPoint : transform;
        Vector3 pos = p.position + p.forward * dollyForward * Mathf.SmoothStep(0f, 1f, k);
        Quaternion rot = p.rotation;
        if (taxi && followTaxi > 0f)
        {
            Vector3 look = taxi.position + Vector3.up * 0.6f - pos;
            if (look.sqrMagnitude > 0.01f) rot = Quaternion.Slerp(rot, Quaternion.LookRotation(look), followTaxi);
        }
        if (handheld > 0f)
        {
            float px = (Mathf.PerlinNoise(t * 0.35f, 1.3f) - 0.5f) * 1.6f * handheld;
            float py = (Mathf.PerlinNoise(4.1f, t * 0.3f) - 0.5f) * 2.2f * handheld;
            float pz = (Mathf.PerlinNoise(t * 0.25f, 7.7f) - 0.5f) * 0.8f * handheld;
            rot = rot * Quaternion.Euler(px, py, pz);
        }
        cutCam.transform.SetPositionAndRotation(pos, rot);
    }

    void EndCamera()
    {
        if (playerCam) playerCam.enabled = true;
        if (cutCam) Destroy(cutCam.gameObject);
    }

    // =====================================================================
    //  hide HUD / player
    // =====================================================================
    void HideWorldUi(bool hide)
    {
        if (hide)
        {
            hiddenCanvases.Clear();
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c != overlay && c.isRootCanvas && c.enabled && c.renderMode != RenderMode.WorldSpace) { c.enabled = false; hiddenCanvases.Add(c); }
        }
        else
        {
            foreach (var c in hiddenCanvases) if (c) c.enabled = true;
            hiddenCanvases.Clear();
        }
    }

    void HidePlayer(bool hide)
    {
        if (hide)
        {
            hiddenRenderers.Clear();
            var player = GameObject.Find("Player");
            if (!player) return;
            foreach (var r in player.GetComponentsInChildren<Renderer>())
                if (r.enabled) { r.enabled = false; hiddenRenderers.Add(r); }
        }
        else
        {
            foreach (var r in hiddenRenderers) if (r) r.enabled = true;
            hiddenRenderers.Clear();
        }
    }

    // =====================================================================
    //  overlay (black, letterbox, caption)
    // =====================================================================
    void BuildOverlay()
    {
        var go = new GameObject("Cutscene_Overlay", typeof(Canvas), typeof(CanvasScaler));
        overlay = go.GetComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder = 450;
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 1f;

        barTop = Bar("BarTop", new Vector2(0, 1), go.transform);
        barBottom = Bar("BarBottom", new Vector2(0, 0), go.transform);

        var font = DormFX.ThaiFont;
        title = Label("Title", go.transform, new Vector2(110, 230), 46, font);
        title.text = captionTitle; title.characterSpacing = 8f;
        sub = Label("Sub", go.transform, new Vector2(112, 180), 24, font);
        sub.text = captionSub; sub.characterSpacing = 14f; sub.color = new Color(0.75f, 0.72f, 0.66f);
        hint = Label("SkipHint", go.transform, new Vector2(0, 0), 20, font);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = new Vector2(1, 0);
        hint.rectTransform.anchoredPosition = new Vector2(-40, 30);
        hint.alignment = TextAlignmentOptions.BottomRight;
        hint.text = skipHint;

        black = new GameObject("Black", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        black.transform.SetParent(go.transform, false);
        var br = black.rectTransform; br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
        black.color = Color.black; black.raycastTarget = false;
        title.alpha = sub.alpha = hint.alpha = 0f;
        SetBars(1f);
    }

    RectTransform Bar(string name, Vector2 anchor, Transform parent)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        img.color = Color.black; img.raycastTarget = false;
        var r = img.rectTransform;
        r.anchorMin = new Vector2(0, anchor.y); r.anchorMax = new Vector2(1, anchor.y); r.pivot = new Vector2(0.5f, anchor.y);
        r.anchoredPosition = Vector2.zero;
        return r;
    }

    static TextMeshProUGUI Label(string name, Transform parent, Vector2 pos, float size, TMP_FontAsset font)
    {
        var t = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        t.transform.SetParent(parent, false);
        var r = t.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 0);
        r.anchoredPosition = pos; r.sizeDelta = new Vector2(1200, 70);
        if (font) t.font = font;
        t.fontSize = size; t.color = new Color(0.93f, 0.91f, 0.86f);
        t.alignment = TextAlignmentOptions.BottomLeft; t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        DormFX.ThaiMarks(t);
        return t;
    }

    void SetBars(float k)
    {
        float h = 1080f * letterbox * k;
        if (barTop) barTop.sizeDelta = new Vector2(0, h);
        if (barBottom) barBottom.sizeDelta = new Vector2(0, h);
    }

    void Caption(float t)
    {
        if (!title || string.IsNullOrEmpty(captionTitle)) return;
        float x = t - captionAt;
        float a = x < 0f ? 0f : x < 1.2f ? x / 1.2f : x < 1.2f + captionHold ? 1f : Mathf.Max(0f, 1f - (x - 1.2f - captionHold) / 1.5f);
        title.alpha = a;
        sub.alpha = Mathf.Clamp01(a * 1.2f - 0.2f);
        title.rectTransform.anchoredPosition = new Vector2(110 + Mathf.Max(0f, x) * 2f, 230);   // ลอยช้าๆ
    }

    // =====================================================================
    //  editor helpers
    // =====================================================================
#if UNITY_EDITOR
    [ContextMenu("ตั้งกล้องตามมุม Scene View")]
    void CameraFromSceneView()
    {
        var sv = UnityEditor.SceneView.lastActiveSceneView;
        if (!sv || !cameraPoint) return;
        UnityEditor.Undo.RecordObject(cameraPoint, "Cutscene camera");
        cameraPoint.SetPositionAndRotation(sv.camera.transform.position, sv.camera.transform.rotation);
    }

    [ContextMenu("ดูมุมกล้องคัตซีนใน Scene View")]
    void SceneViewToCamera()
    {
        var sv = UnityEditor.SceneView.lastActiveSceneView;
        if (!sv || !cameraPoint) return;
        sv.AlignViewToObject(cameraPoint);
    }

    void OnDrawGizmos()
    {
        if (startPoint && parkPoint)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f);
            Gizmos.DrawLine(startPoint.position, parkPoint.position);
            Gizmos.DrawWireSphere(parkPoint.position, 0.4f);
            Gizmos.DrawWireCube(startPoint.position, Vector3.one * 0.5f);
        }
        if (cameraPoint)
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f);
            Gizmos.matrix = Matrix4x4.TRS(cameraPoint.position, cameraPoint.rotation, Vector3.one);
            Gizmos.DrawFrustum(Vector3.zero, fieldOfView, 4f, 0.3f, 16f / 9f);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
#endif
}
