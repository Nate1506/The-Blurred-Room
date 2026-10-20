using UnityEngine;

// คลังเสียงกลางของเกม (ไฟล์: Assets/ThanYo/Resources/SoundBank.asset)
// ทุกระบบดึงเสียงจากที่นี่ -> อยากเปลี่ยนเสียงไหน ลากไฟล์ใหม่ใส่ช่องนั้นช่องเดียว ทั้งเกมเปลี่ยนตาม
// ช่องไหนว่าง = ไม่มีเสียง (ไม่มีเสียงสังเคราะห์สำรองแล้ว)
[CreateAssetMenu(menuName = "ThanYo/Sound Bank", fileName = "SoundBank")]
public class SoundBank : ScriptableObject
{
    static SoundBank inst;
    public static SoundBank I => inst ? inst : (inst = Resources.Load<SoundBank>("SoundBank"));

    [Header("ประตู")]
    [Tooltip("เคาะประตู 1 ครั้ง (ตัดมาจาก fast-knocking)")]
    public AudioClip knock;
    [Tooltip("เคาะประตูแรงๆ 1 ครั้ง (ตัดมาจาก door-slam)")]
    public AudioClip knockHard;
    public AudioClip doorOpen;
    public AudioClip doorClose;
    [Range(0f, 1f)] public float knockVolume = 0.9f;
    [Range(0f, 1f)] public float doorVolume = 0.8f;

    [Header("ผู้เล่น")]
    [Tooltip("เสียงเดิน (วนตอนเดิน)")]
    public AudioClip walkLoop;
    [Tooltip("เสียงขึ้นลงบันได (วนตอนอยู่บนบันได)")]
    public AudioClip stairsLoop;
    [Tooltip("หอบตอนสเตมิน่าหมด")]
    public AudioClip staminaBreath;
    [Range(0f, 1f)] public float walkVolume = 0.45f;
    [Range(0f, 1f)] public float stairsVolume = 0.55f;
    [Range(0f, 1f)] public float breathVolume = 0.7f;

    [Header("บรรยากาศ / เหตุการณ์")]
    [Tooltip("เสียงครางไกลๆ เป็นลางระหว่างทำเควส")]
    public AudioClip omen;
    [Tooltip("เสียงคนวิ่งขึ้นบันไดแล้วปิดประตู (ตอนอยู่ห้องซักผ้า)")]
    public AudioClip runUpStairs;
    public AudioClip scream;
    [Tooltip("เสียงตอนจัมป์สแกร์ (ภาพเด้งเต็มจอ)")]
    public AudioClip jumpscare;
    [Range(0f, 1f)] public float jumpscareVolume = 1f;

    [Header("เมนู")]
    public AudioClip menuMusic;
    [Range(0f, 1f)] public float menuMusicVolume = 0.4f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() => inst = null;
}
