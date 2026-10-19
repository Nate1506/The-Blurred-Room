using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

// ช่วงเวลาที่เลือกได้ใน Inspector
public enum TimeOfDay { Morning, Evening, Night }

// ค่าทั้งหมดของ "หนึ่งช่วงเวลา" เก็บรวมไว้ในก้อนเดียว
[System.Serializable]
public class TimeOfDayPreset
{
    [Header("Sun / Moon")]
    public Vector3 lightRotation = new Vector3(50f, 330f, 0f);
    public Color lightColor = Color.white;
    [Min(0f)] public float lightIntensity = 1f;

    [Header("Sky & Ambient")]
    public Material skybox;
    [Range(0f, 2f)] public float ambientIntensity = 1f;
    [Range(0f, 1f)] public float reflectionIntensity = 1f;

    [Header("Fog")]
    public bool fog = true;
    public Color fogColor = Color.gray;
    [Range(0f, 0.1f)] public float fogDensity = 0.01f;

    [Header("Post-processing")]
    public VolumeProfile volumeProfile;

    [Header("Lamps & window lights")]
    public bool nightLightsOn = false;
}

// ใส่ไว้บน GameObject ตัวเดียวในซีน แล้วเปลี่ยน Time Of Day ใน Inspector
// [ExecuteAlways] = ทำงานใน Editor ด้วย ไม่ต้องกด Play ก็เห็นผล
[ExecuteAlways]
public class TimeOfDayController : MonoBehaviour
{
    public TimeOfDay timeOfDay = TimeOfDay.Night;

    [Header("Scene references")]
    [Tooltip("Directional Light ตัวเดียว ใช้เป็นทั้งพระอาทิตย์และพระจันทร์")]
    public Light sunLight;
    [Tooltip("Global Volume ที่จะสลับ post-processing profile")]
    public Volume globalVolume;
    [Tooltip("โฟลเดอร์ที่เก็บไฟถนน/ไฟหน้าต่าง จะเปิดปิด Light ทุกดวงข้างใน")]
    public Transform nightLightsRoot;
    [Tooltip("ชิ้นที่เรืองแสง เช่น หัวโคมไฟ จะดับแสงเรืองตอนกลางวัน")]
    public Renderer[] nightEmissiveRenderers;
    [Tooltip("สีของชิ้นเรืองแสงตอนไฟดับ (เช่น กระจกโคมขุ่นๆ)")]
    public Color emissiveOffColor = new Color(0.55f, 0.55f, 0.5f);

    [Header("Presets")]
    public TimeOfDayPreset morning = new TimeOfDayPreset();
    public TimeOfDayPreset evening = new TimeOfDayPreset();
    public TimeOfDayPreset night = new TimeOfDayPreset();

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    // เรียกจากโค้ดอื่นตอนเล่นเกมได้ เช่น ตอนเปลี่ยนจากกลางวันเป็นกลางคืน
    public void SetTimeOfDay(TimeOfDay newTime)
    {
        timeOfDay = newTime;
        Apply();
    }

    public TimeOfDayPreset GetPreset(TimeOfDay t)
    {
        switch (t)
        {
            case TimeOfDay.Morning: return morning;
            case TimeOfDay.Evening: return evening;
            default: return night;
        }
    }

    public void Apply()
    {
        var p = GetPreset(timeOfDay);
        if (p == null) return;

        // 1) แสงหลัก (อาทิตย์/จันทร์)
        if (sunLight != null)
        {
            sunLight.transform.rotation = Quaternion.Euler(p.lightRotation);
            sunLight.color = p.lightColor;
            sunLight.intensity = p.lightIntensity;
            RenderSettings.sun = sunLight;
        }

        // 2) ท้องฟ้า + แสงรอบข้าง
        if (p.skybox != null) RenderSettings.skybox = p.skybox;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = p.ambientIntensity;
        RenderSettings.reflectionIntensity = p.reflectionIntensity;

        // 3) หมอก
        RenderSettings.fog = p.fog;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = p.fogColor;
        RenderSettings.fogDensity = p.fogDensity;

        // 4) post-processing
        if (globalVolume != null && p.volumeProfile != null)
            globalVolume.sharedProfile = p.volumeProfile;

        // 5) ไฟถนน / ไฟหน้าต่าง
        if (nightLightsRoot != null)
            foreach (var l in nightLightsRoot.GetComponentsInChildren<Light>(true))
                if (l != sunLight) l.enabled = p.nightLightsOn;

        // 6) ดับแสงเรืองของหัวโคม (ไม่แก้ไฟล์ material ใช้ property block แทน)
        if (nightEmissiveRenderers != null)
        {
            foreach (var r in nightEmissiveRenderers)
            {
                if (r == null) continue;
                if (p.nightLightsOn) r.SetPropertyBlock(null);
                else
                {
                    var block = new MaterialPropertyBlock();
                    r.GetPropertyBlock(block);
                    block.SetColor(EmissionColorId, Color.black);
                    block.SetColor(BaseColorId, emissiveOffColor);
                    r.SetPropertyBlock(block);
                }
            }
        }

        // ให้ Unity คำนวณแสงรอบข้างใหม่จากท้องฟ้าใหม่
        DynamicGI.UpdateEnvironment();
    }

    void OnEnable() => Apply();

#if UNITY_EDITOR
    // เปลี่ยนค่าใน Inspector แล้วอัปเดตทันที
    void OnValidate()
    {
        EditorApplication.delayCall += () => { if (this != null) Apply(); };
    }
#endif
}
