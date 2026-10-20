using BlurredRoom;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// ไฟฉายของผู้ดูแล (ใส่บน Main Camera) — กด F เปิด/ปิด
// สั่งให้กะพริบ/ดับชั่วคราวจากเหตุการณ์ได้: Flashlight.I.Flicker(วินาที)
[DisallowMultipleComponent]
public class Flashlight : MonoBehaviour
{
    public static Flashlight I { get; private set; }

    [Header("มีไฟฉายหรือยัง")]
    public bool owned;
    [Tooltip("เปิดไฟฉายให้เลยตอนหยิบได้")]
    public bool turnOnWhenGiven = true;

    [Header("แสง")]
    public Color color = new Color(1f, 0.93f, 0.8f);
    [Tooltip("ความสว่างลำแสง (URP ใช้แสงลดตามระยะจริง ทางเดินยาวต้องสูงหน่อย)")]
    [Range(0f, 120f)] public float intensity = 35f;
    [Range(1f, 40f)] public float range = 20f;
    [Range(10f, 120f)] public float spotAngle = 64f;
    [Range(0f, 120f)] public float innerAngle = 28f;
    [Tooltip("แสงฟุ้งรอบตัว (เลียนแบบแสงสะท้อนผนัง ให้เห็นทางใกล้ๆ) 0 = ปิด")]
    [Range(0f, 5f)] public float spillIntensity = 1.4f;
    [Range(0.5f, 8f)] public float spillRange = 3.5f;
    public LightShadows shadows = LightShadows.Soft;
    [Tooltip("ก้มลำแสงลงนิดๆ (องศา) แบบคนถือไฟฉาย")]
    [Range(-10f, 30f)] public float aimDown = 6f;
    [Tooltip("ตำแหน่งไฟฉายเทียบกล้อง (ถือต่ำทางขวานิดๆ เงาจะดูมีมิติ)")]
    public Vector3 localOffset = new Vector3(0.18f, -0.15f, 0.1f);
    [Tooltip("ไฟฉายตามการหันแบบหน่วงๆ (0 = ติดกล้องเป๊ะ)")]
    [Range(0f, 30f)] public float lag = 12f;

    [Header("กะพริบเองเป็นพักๆ (ความรู้สึกไฟฉายเก่า)")]
    [Range(0f, 1f)] public float randomFlickerChance = 0.02f;
    [Tooltip("ระยะเวลาระหว่างการลุ้นกะพริบ (วินาที)")]
    public Vector2 randomFlickerEvery = new Vector2(8f, 20f);

    [Header("เสียง")]
    public AudioClip clickClip;

    public bool IsOn => owned && on;

    Light lightComp, spill;
    Transform holder;
    bool on;
    float flickerUntil, nextRandom;

    void Awake()
    {
        I = this;
        holder = new GameObject("Flashlight_Light").transform;
        holder.SetParent(transform, false);
        holder.localPosition = localOffset;
        lightComp = holder.gameObject.AddComponent<Light>();
        lightComp.type = LightType.Spot;
        lightComp.renderMode = LightRenderMode.ForcePixel;
        var sp = new GameObject("Flashlight_Spill");
        sp.transform.SetParent(holder, false);
        sp.transform.localPosition = new Vector3(0f, 0.1f, 1.2f);
        spill = sp.AddComponent<Light>();
        spill.type = LightType.Point; spill.shadows = LightShadows.None; spill.renderMode = LightRenderMode.ForcePixel;
        Apply();
        lightComp.enabled = false; spill.enabled = false;
        nextRandom = Time.time + Random.Range(randomFlickerEvery.x, randomFlickerEvery.y);
    }

    void OnDestroy() { if (I == this) I = null; }

    void OnValidate() { if (lightComp) Apply(); }

    void Apply()
    {
        lightComp.color = color; lightComp.intensity = intensity; lightComp.range = range;
        lightComp.spotAngle = spotAngle; lightComp.innerSpotAngle = Mathf.Min(innerAngle, spotAngle);
        lightComp.shadows = shadows;
        if (spill) { spill.color = color; spill.range = spillRange; spill.intensity = spillIntensity; }
        if (holder) holder.localPosition = localOffset;
    }

    public void Give()
    {
        owned = true;
        if (turnOnWhenGiven) Set(true);
    }

    public void Set(bool value)
    {
        on = value;
        DormFX.Play2D(clickClip ? clickClip : NightAudio.Click, 0.4f, 1.3f);
    }

    /// <summary>กะพริบ/ดับเป็นช่วงๆ เป็นเวลา seconds วิ</summary>
    public void Flicker(float seconds) => flickerUntil = Mathf.Max(flickerUntil, Time.time + seconds);

    void Update()
    {
        if (owned && FPressed() && !DormFX.UIBusy) Set(!on);

        if (Time.time > nextRandom)
        {
            nextRandom = Time.time + Random.Range(randomFlickerEvery.x, randomFlickerEvery.y);
            if (Random.value < randomFlickerChance * 10f) Flicker(Random.Range(0.2f, 0.7f));
        }

        bool lit = IsOn;
        if (lit && Time.time < flickerUntil) lit = Random.value > 0.45f;
        lightComp.enabled = lit;
        spill.enabled = lit && spillIntensity > 0f;
        float k = Time.time < flickerUntil ? Random.Range(0.3f, 1f) : 1f;
        if (lit) { lightComp.intensity = intensity * k; spill.intensity = spillIntensity * k; }
    }

    void LateUpdate()
    {
        // หน่วงทิศไฟฉายตามกล้องนิดๆ
        if (lag <= 0f) { holder.localRotation = Quaternion.Euler(aimDown, 0f, 0f); return; }
        Quaternion target = transform.rotation * Quaternion.Euler(aimDown, 0f, 0f);
        holder.rotation = Quaternion.Slerp(holder.rotation, target, 1f - Mathf.Exp(-lag * Time.deltaTime));
    }

#if ENABLE_INPUT_SYSTEM
    static bool FPressed() => Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#else
    static bool FPressed() => Input.GetKeyDown(KeyCode.F);
#endif
}
