using System.Collections;
using System.Collections.Generic;
using BlurredRoom;
using UnityEngine;

// ไฟของทั้งตึก: ไฟดับ / ไฟฉุกเฉินสีแดง / กะพริบ / ไฟกลับมา
// normalLights = ไฟปกติ (กด "เก็บไฟอัตโนมัติ" ที่เมนูคอมโพเนนต์ เพื่อดึงไฟใต้ Interior_Lighting ชั้น 1-3 + ไฟในห้อง)
// emergencyLights = ไฟแดงฉุกเฉิน (ติดเฉพาะตอนไฟดับ)
public class BuildingPower : MonoBehaviour
{
    public static BuildingPower I { get; private set; }

    [Header("ไฟ")]
    public List<Light> normalLights = new List<Light>();
    public List<Light> emergencyLights = new List<Light>();
    [Tooltip("ออบเจกต์ที่จะเปิด/ปิดตามไฟ (เช่น หลอดไฟเรืองแสง) — ไม่ใส่ก็ได้")]
    public List<GameObject> glowObjects = new List<GameObject>();

    [Header("ไฟดับ")]
    [Tooltip("กะพริบก่อนดับกี่วินาที")]
    [Min(0f)] public float flickerBeforeBlackout = 1.6f;
    [Tooltip("ไฟฉุกเฉินติดหลังไฟดับกี่วินาที")]
    [Min(0f)] public float emergencyDelay = 1.2f;
    [Range(0f, 10f)] public float emergencyIntensity = 2.2f;
    public Color emergencyColor = new Color(1f, 0.08f, 0.04f);

    [Header("เสียง")]
    public AudioClip blackoutClip;
    public AudioClip restoreClip;
    public AudioClip buzzLoop;
    [Range(0f, 1f)] public float buzzVolume = 0.05f;

    [Header("สถานะ")]
    [SerializeField] bool powerOn = true;

    public bool PowerOn => powerOn;

    readonly Dictionary<Light, float> baseIntensity = new Dictionary<Light, float>();
    readonly HashSet<Light> manualOff = new HashSet<Light>();   // ไฟที่ผู้เล่นกดสวิตช์ปิดเอง (ไฟกลับมาก็ยังปิด)
    AudioSource buzz;
    Coroutine running;

    void Awake()
    {
        I = this;
        foreach (var l in normalLights) if (l) baseIntensity[l] = l.intensity;
        foreach (var l in emergencyLights) if (l) { l.color = emergencyColor; l.intensity = emergencyIntensity; l.enabled = false; }
        buzz = gameObject.AddComponent<AudioSource>();
        buzz.clip = buzzLoop ? buzzLoop : NightAudio.Buzz; buzz.loop = true; buzz.spatialBlend = 0f; buzz.volume = buzzVolume;
        buzz.Play();
        NightDemo.Respawned += OnRespawn;
    }

    void OnDestroy()
    {
        if (I == this) I = null;
        NightDemo.Respawned -= OnRespawn;
    }

    void OnRespawn()
    {
        manualOff.Clear();   // กลุ่มสวิตช์จะถูกสั่งใหม่โดย LightGroup ตามขั้นที่เกิด
        // กลับไปสถานะตามขั้นที่เกิดใหม่: ขั้นที่สั่งไฟดับจะสั่งใหม่เองผ่าน onBegin
        if (running != null) StopCoroutine(running);
        running = null;
    }

    [ContextMenu("ไฟดับ (ทดสอบ)")]
    public void Blackout()
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(BlackoutRoutine());
    }

    [ContextMenu("ไฟกลับมา (ทดสอบ)")]
    public void Restore()
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(RestoreRoutine());
    }

    /// <summary>ไฟดับทันที ไม่มีลูกเล่น (ใช้ตอนเกิดใหม่ในขั้นที่ไฟดับอยู่แล้ว)</summary>
    public void SetPowerInstant(bool on)
    {
        if (running != null) StopCoroutine(running);
        running = null;
        SetNormal(on ? 1f : 0f);
        SetEmergency(!on);
        powerOn = on;
        buzz.volume = on ? buzzVolume : 0f;
    }

    /// <summary>กะพริบไฟทั้งตึกชั่วครู่ (ไฟยังติดอยู่)</summary>
    public void Flicker(float seconds)
    {
        if (!powerOn) return;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(FlickerRoutine(seconds));
    }

    IEnumerator FlickerRoutine(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            SetNormal(Random.value < 0.4f ? Random.Range(0f, 0.3f) : 1f);
            yield return new WaitForSeconds(Random.Range(0.03f, 0.12f));
        }
        SetNormal(1f);
        running = null;
    }

    IEnumerator BlackoutRoutine()
    {
        for (float t = 0f; t < flickerBeforeBlackout; t += 0.06f)
        {
            SetNormal(Random.value < 0.5f ? Random.Range(0f, 0.25f) : Random.Range(0.6f, 1f));
            yield return new WaitForSeconds(Random.Range(0.04f, 0.14f));
        }
        SetNormal(0f);
        powerOn = false;
        buzz.volume = 0f;
        DormFX.Play2D(blackoutClip ? blackoutClip : NightAudio.Clunk, 0.9f, 0.7f);
        CameraDynamics.Shake(0.15f);
        yield return new WaitForSeconds(emergencyDelay);
        SetEmergency(true);
        running = null;
    }

    IEnumerator RestoreRoutine()
    {
        DormFX.Play2D(restoreClip ? restoreClip : NightAudio.Clunk, 0.9f, 1.1f);
        SetEmergency(false);
        for (float t = 0f; t < 0.8f; t += 0.08f)
        {
            SetNormal(Random.value < 0.5f ? 0f : Random.Range(0.4f, 1f));
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }
        SetNormal(1f);
        powerOn = true;
        buzz.volume = buzzVolume;
        running = null;
    }

    void SetNormal(float k)
    {
        foreach (var l in normalLights)
        {
            if (!l) continue;
            if (!baseIntensity.ContainsKey(l)) baseIntensity[l] = l.intensity;
            if (manualOff.Contains(l)) { l.enabled = false; continue; }
            l.enabled = k > 0.01f;
            l.intensity = baseIntensity[l] * k;
        }
        foreach (var g in glowObjects) if (g) g.SetActive(k > 0.5f);
    }

    /// <summary>เปิด/ปิดไฟกลุ่มหนึ่งแบบถาวร (สวิตช์) — ไฟดับ/ไฟกลับมาจะไม่ไปยุ่งกับกลุ่มนี้</summary>
    public void SetManual(IEnumerable<Light> lights, bool on)
    {
        foreach (var l in lights)
        {
            if (!l) continue;
            if (!baseIntensity.ContainsKey(l)) baseIntensity[l] = l.intensity;
            if (on) { manualOff.Remove(l); l.enabled = powerOn; l.intensity = baseIntensity[l]; }
            else { manualOff.Add(l); l.enabled = false; }
        }
    }

    void SetEmergency(bool on)
    {
        foreach (var l in emergencyLights) if (l) l.enabled = on;
    }

#if UNITY_EDITOR
    [ContextMenu("เก็บไฟอัตโนมัติ (Interior_Lighting ชั้น 1-3 + Room_Lights)")]
    void CollectLights()
    {
        normalLights.Clear();
        var il = GameObject.Find("Interior_Lighting");
        if (!il) return;
        foreach (var n in new[] { "Floor_1", "Floor_2", "Floor_3", "Room_Lights" })
        {
            var f = il.transform.Find(n);
            if (f) normalLights.AddRange(f.GetComponentsInChildren<Light>(true));
        }
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
