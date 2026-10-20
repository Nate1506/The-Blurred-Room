using UnityEngine;

// เสียงบรรยากาศวนตลอด (เสียงหึ่งต่ำๆ ของตึก) — ใส่ไฟล์จริงในช่อง clip ได้, ไม่ใส่ = ใช้เสียงที่สร้างจากโค้ด
public class AmbientLoop : MonoBehaviour
{
    public AudioClip clip;
    public enum BuiltIn { Drone, Hiss, Buzz }
    public BuiltIn fallback = BuiltIn.Drone;
    [Range(0f, 1f)] public float volume = 0.18f;
    [Tooltip("ค่อยๆ ดังขึ้นตอนเริ่ม (วินาที)")]
    [Min(0f)] public float fadeIn = 3f;

    AudioSource src;

    void Start()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.clip = clip ? clip : fallback == BuiltIn.Drone ? NightAudio.Drone : fallback == BuiltIn.Hiss ? NightAudio.Hiss : NightAudio.Buzz;
        src.loop = true; src.spatialBlend = 0f; src.volume = 0f;
        src.Play();
    }

    void Update()
    {
        if (src) src.volume = Mathf.MoveTowards(src.volume, volume, Time.deltaTime * volume / Mathf.Max(0.01f, fadeIn));
    }
}
