using BlurredRoom;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// กล้องวิดีโอ VHS ของผู้ดูแล (ใส่บน Main Camera ของผู้เล่น)
//   - ค้างคลิกขวา = ยกกล้องขึ้นถ่าย: เห็นภาพผ่านจอกล้อง (REC, เวลา, แบต, ภาพแตก)
//   - "ผี" อยู่บน layer Spectral -> ตาเปล่ามองไม่เห็น เห็นเฉพาะผ่านกล้อง
//   - ผีที่โดนกล้องจับภาพจะช้าลง (GhostChaser ถาม Camcorder.Sees)
//   - แบตลดตอนถ่าย ฟื้นช้าๆ ตอนลดกล้อง (ปรับได้)
//   - ภาพ VHS จางๆ ตลอดเวลา แม้ไม่ได้ยกกล้อง (ความรู้สึกเหมือนเทปที่มีคนเจอ)
[DisallowMultipleComponent]
public class Camcorder : MonoBehaviour
{
    public static Camcorder I { get; private set; }

    [Header("มีกล้องหรือยัง")]
    [Tooltip("ติ๊ก = มีตั้งแต่เริ่ม (ในเดโมจะได้ตอนหยิบที่ออฟฟิศ -> Give())")]
    public bool owned;

    [Header("การยกกล้อง")]
    [Tooltip("เวลายกขึ้น/ลดลง (วินาที)")]
    [Min(0.01f)] public float raiseTime = 0.25f;
    [Tooltip("FOV ตอนยกกล้อง (ภาพซูมนิดๆ)")]
    [Range(30f, 90f)] public float raisedFov = 62f;

    [Header("แบตเตอรี่")]
    [Tooltip("ถ่ายต่อเนื่องได้กี่วินาทีจนแบตหมด")]
    [Min(1f)] public float batterySeconds = 60f;
    [Tooltip("แบตฟื้นต่อวินาทีตอนไม่ได้ถ่าย (หน่วยเป็นวินาทีของแบต)")]
    [Min(0f)] public float rechargePerSecond = 1.5f;
    [Range(0f, 1f)] public float battery01 = 1f;

    [Header("ผี (layer ที่เห็นเฉพาะผ่านกล้อง)")]
    public string spectralLayer = "Spectral";
    [Tooltip("ระยะไกลสุดที่กล้องจับภาพผีได้")]
    [Min(1f)] public float captureRange = 14f;
    [Tooltip("ขอบจอที่ไม่นับว่าอยู่ในภาพ (0..0.4)")]
    [Range(0f, 0.4f)] public float frameMargin = 0.12f;

    [Header("ภาพ VHS")]
    [Tooltip("ความเข้มของเส้น/นอยส์ตอนไม่ได้ยกกล้อง")]
    [Range(0f, 0.3f)] public float idleVhs = 0.02f;
    [Tooltip("ความเข้มตอนยกกล้อง")]
    [Range(0f, 1f)] public float raisedVhs = 0.08f;
    [Tooltip("เลนส์โค้ง (ติดลบ = นูนแบบกล้องเก่า)")]
    [Range(-0.6f, 0f)] public float lensDistortion = -0.28f;
    [Range(0f, 1f)] public float chromatic = 0.3f;
    [Tooltip("เกรนฟิล์มตอนยกกล้อง (0 = เนียน)")]
    [Range(0f, 1f)] public float raisedGrain = 0.05f;

    [Header("โหมดกลางคืน (Night Vision สีเขียว)")]
    [Tooltip("ยกกล้องแล้วภาพเป็นโทนเขียวแบบกล้องอินฟราเรด เห็นในที่มืดได้")]
    public bool nightVision = true;
    public Color nightVisionTint = new Color(0.45f, 1f, 0.62f);
    [Tooltip("ความสว่างที่เพิ่มขึ้นตอนเปิด Night Vision (stop)")]
    [Range(0f, 4f)] public float nightVisionBoost = 1.8f;
    [Tooltip("ไฟอินฟราเรดที่ติดกล้อง (ส่องให้เห็นใกล้ๆ ในที่มืดสนิท) 0 = ปิด")]
    [Range(0f, 40f)] public float irLightIntensity = 14f;
    [Range(1f, 25f)] public float irLightRange = 12f;
    [Tooltip("วันเวลาบนจอกล้อง")]
    public string dateText = "OCT. 31 1997";
    [Tooltip("เวลาเริ่มบนจอ (นาทีหลังเที่ยงคืน, ติดลบ = ก่อนเที่ยงคืน) เช่น -75 = 22:45")]
    public int startClockMinutes = -75;
    [Tooltip("นาฬิกาบนจอเดินเร็วกว่าจริงกี่เท่า")]
    [Min(1f)] public float clockSpeed = 6f;

    [Header("ทดสอบ")]
    [Tooltip("ติ๊กตอน Play = ยกกล้องค้างไว้ (ไว้ดูภาพ/ทดสอบผีโดยไม่ต้องกดเมาส์)")]
    public bool debugHoldRaise;

    [Header("เสียง")]
    public AudioClip raiseClip;
    public AudioClip hissLoop;
    [Range(0f, 1f)] public float hissVolume = 0.12f;

    public bool IsRaised => owned && raise01 > 0.6f && battery01 > 0f;
    public float Raise01 => raise01;

    Camera cam;
    int spectralMask;
    float raise01;
    bool wantRaise;
    // UI
    Canvas canvas;
    CanvasGroup raisedGroup;
    RawImage scan, noise;
    TextMeshProUGUI rec, clock, date, batt;
    Image recDot;
    Volume volume;
    Light irLight;
    LensDistortion lens;
    ChromaticAberration chroma;
    FilmGrain grain;
    ColorAdjustments color;
    ChannelMixer mixer;
    AudioSource hiss;

    void Awake()
    {
        I = this;
        cam = GetComponent<Camera>();
        int l = LayerMask.NameToLayer(spectralLayer);
        spectralMask = l >= 0 ? 1 << l : 0;
        if (cam && spectralMask != 0) cam.cullingMask &= ~spectralMask;
        BuildUI();
        BuildVolume();
        var ir = new GameObject("Camcorder_IR");
        ir.transform.SetParent(transform, false);
        irLight = ir.AddComponent<Light>();
        irLight.type = LightType.Spot; irLight.spotAngle = 80f; irLight.innerSpotAngle = 40f; irLight.shadows = LightShadows.None;
        irLight.color = Color.white; irLight.enabled = false;
        ApplyLook();
        hiss = gameObject.AddComponent<AudioSource>();
        hiss.loop = true; hiss.spatialBlend = 0f; hiss.volume = 0f;
        hiss.clip = hissLoop;
        hiss.Play();
    }

    void OnDestroy() { if (I == this) I = null; }

    public void Give()
    {
        owned = true;
        battery01 = 1f;
    }

    public void AddBattery(float seconds) => battery01 = Mathf.Clamp01(battery01 + seconds / batterySeconds);

    // =====================================================================
    void Update()
    {
        float dt = Time.deltaTime;
        var input = DormFX.Input;
        bool blocked = !owned || DormFX.UIBusy || (input && input.MovementLocked) || (NightDemo.I && NightDemo.I.IsDying);
        wantRaise = !blocked && (RmbHeld() || debugHoldRaise) && battery01 > 0f;

        float before = raise01;
        raise01 = Mathf.MoveTowards(raise01, wantRaise ? 1f : 0f, dt / raiseTime);
        if (before < 0.05f && raise01 >= 0.05f) DormFX.Play2D(raiseClip, 0.5f);

        if (IsRaised) battery01 = Mathf.Max(0f, battery01 - dt / batterySeconds);
        else battery01 = Mathf.Min(1f, battery01 + rechargePerSecond * dt / batterySeconds);

        // เห็นผีเฉพาะตอนยกกล้อง
        if (cam && spectralMask != 0)
        {
            if (raise01 > 0.5f) cam.cullingMask |= spectralMask; else cam.cullingMask &= ~spectralMask;
        }

        UpdateUI(dt);
    }

    void LateUpdate()
    {
        // FOV: ทำงานร่วมกับ CameraDynamics (มันตั้ง fov ใน LateUpdate) -> เราผสมทับหลังจากนั้น
        if (cam && raise01 > 0f) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, raisedFov, Smooth(raise01));
    }

    static float Smooth(float x) => x * x * (3f - 2f * x);

    /// <summary>กล้องกำลังถ่ายจุดนี้อยู่ไหม (อยู่ในเฟรม + ไม่มีอะไรบัง)</summary>
    public bool Sees(Vector3 worldPoint)
    {
        if (!IsRaised || !cam) return false;
        Vector3 v = cam.WorldToViewportPoint(worldPoint);
        if (v.z <= 0f || v.z > captureRange) return false;
        if (v.x < frameMargin || v.x > 1f - frameMargin || v.y < frameMargin * 0.5f || v.y > 1f - frameMargin * 0.5f) return false;
        Vector3 from = cam.transform.position;
        Vector3 dir = worldPoint - from;
        int mask = ~(spectralMask | (1 << 2));
        return !Physics.Raycast(from, dir.normalized, dir.magnitude - 0.3f, mask, QueryTriggerInteraction.Ignore);
    }

#if ENABLE_INPUT_SYSTEM
    static bool RmbHeld() => Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
    static bool RmbHeld() => Input.GetMouseButton(1);
#endif

    // =====================================================================
    //  UI
    // =====================================================================
    void BuildUI()
    {
        var go = new GameObject("Camcorder_UI", typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;   // ใต้ HUD หลัก (HUD ข้อความยังอ่านได้)
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
        var root = go.transform;

        scan = Raw("Scanlines", root, Resources.Load<Texture2D>("UI/scanline"));
        noise = Raw("Noise", root, Resources.Load<Texture2D>("UI/noise"));

        var rg = new GameObject("Raised", typeof(RectTransform), typeof(CanvasGroup));
        rg.transform.SetParent(root, false);
        Stretch((RectTransform)rg.transform);
        raisedGroup = rg.GetComponent<CanvasGroup>();
        raisedGroup.alpha = 0f; raisedGroup.blocksRaycasts = false;

        // มุมกรอบ
        var corner = Resources.Load<Sprite>("UI/vhs_corner");
        Corner(rg.transform, corner, new Vector2(0, 1), new Vector2(70, -70), 0f);
        Corner(rg.transform, corner, new Vector2(1, 1), new Vector2(-70, -70), -90f);
        Corner(rg.transform, corner, new Vector2(1, 0), new Vector2(-70, 70), 180f);
        Corner(rg.transform, corner, new Vector2(0, 0), new Vector2(70, 70), 90f);

        recDot = new GameObject("RecDot", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        recDot.transform.SetParent(rg.transform, false);
        var rr = recDot.rectTransform; rr.anchorMin = rr.anchorMax = new Vector2(0, 1); rr.anchoredPosition = new Vector2(120, -120); rr.sizeDelta = new Vector2(26, 26);
        recDot.color = new Color(0.9f, 0.1f, 0.08f);
        rec = Label(rg.transform, "REC", new Vector2(0, 1), new Vector2(150, -120), TextAlignmentOptions.MidlineLeft, 44);
        clock = Label(rg.transform, "", new Vector2(0, 0), new Vector2(120, 150), TextAlignmentOptions.MidlineLeft, 40);
        date = Label(rg.transform, dateText, new Vector2(0, 0), new Vector2(120, 105), TextAlignmentOptions.MidlineLeft, 34);
        batt = Label(rg.transform, "", new Vector2(1, 1), new Vector2(-120, -120), TextAlignmentOptions.MidlineRight, 40);
        var play = Label(rg.transform, "SP", new Vector2(1, 0), new Vector2(-120, 105), TextAlignmentOptions.MidlineRight, 34);
        play.alpha = 0.8f;
    }

    void UpdateUI(float dt)
    {
        float r = Smooth(raise01);
        float lowBatt = battery01 < 0.2f ? (Mathf.Repeat(Time.time * 3f, 1f) < 0.5f ? 1f : 0.3f) : 1f;
        raisedGroup.alpha = r;
        float vhs = Mathf.Lerp(idleVhs, raisedVhs, r);
        if (battery01 <= 0.02f && wantRaise) vhs = 1f;
        // เส้น/นอยส์เป็นสีดำจางๆ (ทำให้ภาพหม่น ไม่ใช่จุดขาวยิบ)
        scan.color = new Color(0, 0, 0, vhs * 0.6f);
        noise.color = new Color(0, 0, 0, vhs * (0.3f + Random.value * 0.2f));
        float o = Random.value;
        noise.uvRect = new Rect(o, Random.value, 2.2f, 1.4f);
        scan.uvRect = new Rect(0, Time.time * 0.15f, 1, Screen.height / 4f);

        if (r > 0.01f)
        {
            recDot.enabled = Mathf.Repeat(Time.time, 1.2f) < 0.7f;
            int minutes = startClockMinutes + Mathf.FloorToInt(Time.timeSinceLevelLoad * clockSpeed / 60f);
            minutes = ((minutes % 1440) + 1440) % 1440;
            int h = minutes / 60, m = minutes % 60;
            string ampm = h < 12 ? "AM" : "PM";
            int h12 = h % 12; if (h12 == 0) h12 = 12;
            clock.text = ampm + " " + h12 + ":" + m.ToString("00");
            int bars = Mathf.CeilToInt(battery01 * 4f);
            batt.text = "BATT [" + new string('|', bars * 2) + new string('.', 8 - bars * 2) + "]";
            batt.alpha = lowBatt;
            date.text = dateText;
        }

        if (volume) volume.weight = r;
        if (irLight) irLight.enabled = nightVision && irLightIntensity > 0f && r > 0.5f;
        if (hiss) hiss.volume = Mathf.Lerp(hissVolume * 0.25f, hissVolume, r);
    }

    void BuildVolume()
    {
        var vgo = new GameObject("Camcorder_Volume");
        vgo.transform.SetParent(transform, false);
        volume = vgo.AddComponent<Volume>();
        volume.isGlobal = true; volume.priority = 50f; volume.weight = 0f;
        var p = ScriptableObject.CreateInstance<VolumeProfile>();
        lens = p.Add<LensDistortion>(true); lens.intensity.Override(lensDistortion); lens.scale.Override(1.02f);
        chroma = p.Add<ChromaticAberration>(true); chroma.intensity.Override(chromatic);
        grain = p.Add<FilmGrain>(true); grain.type.Override(FilmGrainLookup.Medium4); grain.intensity.Override(raisedGrain);
        color = p.Add<ColorAdjustments>(true);
        mixer = p.Add<ChannelMixer>(true);
        var vig = p.Add<Vignette>(true); vig.intensity.Override(0.45f); vig.smoothness.Override(0.6f);
        ApplyLook();
        volume.sharedProfile = p;
    }

    void ApplyLook()
    {
        if (!color) return;
        if (mixer) mixer.active = nightVision;
        if (nightVision)
        {
            // ภาพขาวดำ (ความสว่างตามสายตาคน) แล้วย้อมด้วยสี tint -> เขียวแบบกล้องอินฟราเรด
            Vector3 lum = new Vector3(30f, 59f, 11f);
            Vector3 r = lum * nightVisionTint.r, g = lum * nightVisionTint.g, b = lum * nightVisionTint.b;
            mixer.redOutRedIn.Override(r.x); mixer.redOutGreenIn.Override(r.y); mixer.redOutBlueIn.Override(r.z);
            mixer.greenOutRedIn.Override(g.x); mixer.greenOutGreenIn.Override(g.y); mixer.greenOutBlueIn.Override(g.z);
            mixer.blueOutRedIn.Override(b.x); mixer.blueOutGreenIn.Override(b.y); mixer.blueOutBlueIn.Override(b.z);
            color.saturation.Override(0f);
            color.colorFilter.Override(Color.white);
            color.postExposure.Override(nightVisionBoost);
            color.contrast.Override(18f);
        }
        else
        {
            color.saturation.Override(-35f);
            color.colorFilter.Override(Color.white);
            color.postExposure.Override(0.35f);
            color.contrast.Override(12f);
        }
        if (grain) grain.intensity.Override(raisedGrain);
        if (irLight) { irLight.intensity = irLightIntensity; irLight.range = irLightRange; }
    }

    void OnValidate()
    {
        ApplyLook();
        if (lens) lens.intensity.Override(lensDistortion);
        if (chroma) chroma.intensity.Override(chromatic);
    }

    // ---------- UI helpers ----------
    static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }

    static RawImage Raw(string name, Transform parent, Texture tex)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        img.transform.SetParent(parent, false);
        Stretch(img.rectTransform);
        img.texture = tex; img.raycastTarget = false; img.color = new Color(1, 1, 1, 0);
        return img;
    }

    static void Corner(Transform parent, Sprite s, Vector2 anchor, Vector2 pos, float rot)
    {
        var img = new GameObject("Corner", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        var rt = img.rectTransform; rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(90, 90);
        rt.localRotation = Quaternion.Euler(0, 0, rot);
        img.sprite = s; img.color = new Color(1, 1, 1, 0.85f); img.raycastTarget = false;
        if (!s) img.enabled = false;
    }

    static TextMeshProUGUI Label(Transform parent, string text, Vector2 anchor, Vector2 pos, TextAlignmentOptions align, float size)
    {
        var t = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        t.transform.SetParent(parent, false);
        var rt = t.rectTransform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(anchor.x, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(600, 60);
        var font = Resources.Load<TMP_FontAsset>("Menu/MenuType");
        if (font) t.font = font;
        t.text = text; t.fontSize = size; t.alignment = align; t.color = new Color(0.95f, 0.95f, 0.92f);
        t.outlineWidth = 0.15f; t.outlineColor = new Color32(0, 0, 0, 160); t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }
}
