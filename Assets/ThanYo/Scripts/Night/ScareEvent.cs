using System.Collections;
using BlurredRoom;
using UnityEngine;
using UnityEngine.Events;

// เหตุการณ์หลอนครั้งเดียว: ผู้เล่นเดินเข้ากล่อง (หรือสั่ง Fire จากที่อื่น) -> เกิดหลายอย่างพร้อมกันตามที่ติ๊ก
// ใช้ปูความกลัวก่อนผีออกจริง: ประตูปิดปัง, ไฟกะพริบ, เงาที่ปลายทางเดิน (เห็นเฉพาะผ่านกล้อง), เสียงกระซิบ ฯลฯ
public class ScareEvent : MonoBehaviour
{
    [Header("ทริกเกอร์")]
    [Tooltip("ผู้เล่นเดินเข้ากล่องนี้แล้วเกิด (ปิด = ต้องสั่ง Fire() จากที่อื่น)")]
    public bool triggerByZone = true;
    public Vector3 zoneSize = new Vector3(2f, 2.4f, 2f);
    [Tooltip("เกิดเฉพาะตอนเดโมอยู่ขั้นนี้ (ว่าง = ตลอด)")]
    public string onlyDuringStep = "";
    [Tooltip("ต้องยกกล้องอยู่ถึงจะเกิด")]
    public bool requireCameraRaised;
    [Tooltip("รอกี่วิหลังเข้ากล่อง")]
    [Min(0f)] public float delay = 0f;

    [Header("เสียง")]
    public AudioClip sound;
    public enum BuiltIn { None, Slam, Thud, Whisper, Clunk, Scream, Step }
    [Tooltip("ใช้เสียงที่สร้างจากโค้ดถ้ายังไม่มีไฟล์")]
    public BuiltIn builtInSound = BuiltIn.None;
    [Tooltip("เสียงดังจากตรงไหน (ว่าง = ตำแหน่งนี้)")]
    public Transform soundAt;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.3f, 2f)] public float pitch = 1f;

    [Header("ไฟ")]
    [Tooltip("ไฟทั้งตึกกะพริบกี่วิ (0 = ไม่)")]
    [Min(0f)] public float buildingFlicker = 0f;
    [Tooltip("ไฟฉายกะพริบกี่วิ (0 = ไม่)")]
    [Min(0f)] public float flashlightFlicker = 0f;

    [Header("ของที่ขยับ")]
    [Tooltip("หมุนออบเจกต์นี้ทันที (เช่น บานประตูปิดปัง)")]
    public Transform rotateObject;
    public Vector3 rotateBy = new Vector3(0, -90, 0);
    [Min(0.01f)] public float rotateTime = 0.12f;

    [Header("สิ่งที่โผล่")]
    [Tooltip("ออบเจกต์ที่โผล่ขึ้นมาชั่วคราว (เช่น เงาคนที่ปลายทางเดิน)")]
    public GameObject apparition;
    [Min(0f)] public float apparitionSeconds = 2.5f;
    [Tooltip("ซ่อนทันทีถ้าผู้เล่นยกกล้องจ้องโดน")]
    public bool vanishWhenFilmed = true;

    [Header("กล้อง / ข้อความ")]
    [Range(0f, 1f)] public float cameraShake = 0f;
    public string message = "";
    [Tooltip("ซับไตเติล (เสียงพูด)")]
    public string subtitle = "";
    public UnityEvent onFire = new UnityEvent();

    bool fired;

    void Awake()
    {
        if (apparition) apparition.SetActive(false);
        NightDemo.Respawned += () => { if (!string.IsNullOrEmpty(onlyDuringStep) && !NightDemo.IsDone(onlyDuringStep)) fired = false; };
    }

    void Update()
    {
        if (fired || !triggerByZone) return;
        if (!string.IsNullOrEmpty(onlyDuringStep) && !NightDemo.IsCurrent(onlyDuringStep)) return;
        if (requireCameraRaised && !(Camcorder.I && Camcorder.I.IsRaised)) return;
        var p = DormFX.Player;
        if (!p) return;
        Vector3 local = transform.InverseTransformPoint(p.position + Vector3.up * 0.9f);
        Vector3 h = zoneSize * 0.5f;
        if (Mathf.Abs(local.x) <= h.x && Mathf.Abs(local.y) <= h.y && Mathf.Abs(local.z) <= h.z) Fire();
    }

    [ContextMenu("เกิดเลย (ทดสอบ)")]
    public void Fire()
    {
        if (fired) return;
        fired = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        var clip = sound ? sound : BuiltInClip();
        if (clip)
        {
            Vector3 at = soundAt ? soundAt.position : transform.position;
            var go = new GameObject("Scare_SFX");
            go.transform.position = at;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip; src.volume = volume; src.pitch = pitch; src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear; src.minDistance = 2f; src.maxDistance = 25f; src.dopplerLevel = 0f;
            src.Play();
            Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.2f);
        }
        if (buildingFlicker > 0f && BuildingPower.I) BuildingPower.I.Flicker(buildingFlicker);
        if (flashlightFlicker > 0f && Flashlight.I) Flashlight.I.Flicker(flashlightFlicker);
        if (cameraShake > 0f) CameraDynamics.Shake(cameraShake);
        if (!string.IsNullOrEmpty(message)) DormFX.Msg(message, 3f);
        if (!string.IsNullOrEmpty(subtitle)) DormFX.Sub(subtitle, 3.5f);
        onFire?.Invoke();
        if (rotateObject) StartCoroutine(Rotate());
        if (apparition)
        {
            apparition.SetActive(true);
            for (float t = 0f; t < apparitionSeconds; t += Time.deltaTime)
            {
                if (vanishWhenFilmed && t > 0.4f && Camcorder.I && Camcorder.I.Sees(apparition.transform.position + Vector3.up * 1.4f))
                {
                    DormFX.Play2D(NightAudio.Click, 0.3f, 0.5f);
                    break;
                }
                yield return null;
            }
            apparition.SetActive(false);
        }
    }

    IEnumerator Rotate()
    {
        Quaternion a = rotateObject.localRotation, b = a * Quaternion.Euler(rotateBy);
        for (float t = 0f; t < rotateTime; t += Time.deltaTime)
        {
            rotateObject.localRotation = Quaternion.Slerp(a, b, t / rotateTime);
            yield return null;
        }
        rotateObject.localRotation = b;
    }

    AudioClip BuiltInClip()
    {
        switch (builtInSound)
        {
            case BuiltIn.Slam: return NightAudio.Slam;
            case BuiltIn.Thud: return NightAudio.Thud;
            case BuiltIn.Whisper: return NightAudio.Whisper;
            case BuiltIn.Clunk: return NightAudio.Clunk;
            case BuiltIn.Scream: return NightAudio.Scream;
            case BuiltIn.Step: return NightAudio.Step;
        }
        return null;
    }

    void OnDrawGizmos()
    {
        if (!triggerByZone) return;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.25f, 0.6f, 0.9f);
        Gizmos.DrawWireCube(Vector3.zero, zoneSize);
    }
}
