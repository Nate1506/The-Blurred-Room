using BlurredRoom;
using UnityEngine;

// เสียงบรรยากาศข้างนอกตึก (นกในป่า ฯลฯ) — วางไว้ในซีนลานหน้าหอ (ThanYo)
// ความดังแยกตามช่วงเวลา (กลางวัน / เย็น / กลางคืน) และค่อยๆ เฟดเมื่อเวลาเปลี่ยน
public class OutdoorAmbience : MonoBehaviour
{
    [Tooltip("ไฟล์เสียงวนลูป")]
    public AudioClip clip;

    [Header("ความดังตามช่วงเวลา (0 = เงียบ)")]
    [Range(0f, 1f)] public float dayVolume = 0.4f;
    [Range(0f, 1f)] public float eveVolume = 0.25f;
    [Range(0f, 1f)] public float nightVolume = 0f;

    [Header("การเล่น")]
    [Tooltip("เฟดเข้า/ออกกี่วินาที (ตอนเข้าซีนและตอนเปลี่ยนช่วงเวลา)")]
    [Min(0f)] public float fadeSeconds = 3f;
    [Tooltip("เริ่มเล่นจากจุดสุ่มในไฟล์ (เข้าซีนแต่ละรอบเสียงไม่ซ้ำตำแหน่งเดิม)")]
    public bool randomStart = true;
    [Range(0.5f, 1.5f)] public float pitch = 1f;

    AudioSource src;

    void Start()
    {
        if (!clip) { enabled = false; return; }
        src = gameObject.AddComponent<AudioSource>();
        src.clip = clip; src.loop = true; src.spatialBlend = 0f; src.volume = 0f; src.pitch = pitch;
        src.playOnAwake = false;
        src.Play();
        if (randomStart && clip.length > 1f) src.time = Random.Range(0f, clip.length - 0.5f);
    }

    float Target()
    {
        switch (DormFX.Phase)
        {
            case DayPhase.Eve: return eveVolume;
            case DayPhase.Night: return nightVolume;
            default: return dayVolume;
        }
    }

    void Update()
    {
        if (!src) return;
        src.pitch = pitch;
        float target = Target();
        src.volume = fadeSeconds <= 0f ? target : Mathf.MoveTowards(src.volume, target, Time.deltaTime / fadeSeconds);
    }
}
