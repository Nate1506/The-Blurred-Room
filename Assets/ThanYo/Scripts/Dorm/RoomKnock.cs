using System.Collections;
using System.Collections.Generic;
using BlurredRoom;
using UnityEngine;

// เคาะประตูห้อง (ใส่บนประตูห้องที่ล็อกอยู่) — กด E ที่ประตู = เคาะ
//   Tenant  : ห้องที่มีคนอยู่ -> ผู้เช่าเปิดประตู เดินออกมาคุย (เฉพาะกลางวัน)
//   Vanished: ห้องคนหายปริศนา -> ประตูเปิดเอง... ไม่มีใคร ไฟกะพริบ เสียงหลอน สติลด แล้วประตูค่อยๆ ปิด
//   Empty   : ห้องว่าง -> เงียบ ไม่มีใครตอบ
// ผู้เช่าที่หายตัวไประหว่างเกม -> ห้องนั้นกลายเป็นแบบ Vanished เอง
[RequireComponent(typeof(Door))]
public class RoomKnock : MonoBehaviour
{
    public enum Kind { Tenant, Vanished, Empty }

    [Header("ห้องนี้")]
    public string room = "101";
    public Kind kind = Kind.Empty;
    [Tooltip("ผู้เช่า (ถ้าเป็นห้องที่มีคนอยู่)")]
    public TenantNpc tenant;

    [Header("การเคาะ")]
    [Range(1, 6)] public int knockCount = 3;
    [Range(0.1f, 1f)] public float knockInterval = 0.38f;
    [Tooltip("รอกี่วินาทีหลังเคาะ ก่อนมีคนตอบ/เกิดเรื่อง")]
    public Vector2 answerDelay = new Vector2(1.2f, 2.2f);
    [Range(0f, 1f)] public float knockVolume = 0.9f;
    [Tooltip("ผู้เช่าออกมาเฉพาะกลางวัน")]
    public bool tenantsOnlyByDay = true;

    [Header("ห้องคนหายปริศนา")]
    [Tooltip("เสียงหลอนตอนประตูเปิด (ว่าง = เสียงลางใน SoundBank)")]
    public AudioClip eerieClip;
    [Range(0f, 1f)] public float eerieVolume = 0.85f;
    [Tooltip("สติลดเท่าไร")]
    [Min(0f)] public float sanityLoss = 6f;
    [Tooltip("ไฟที่อยู่ใกล้ประตูในระยะนี้จะกะพริบ (เมตร)")]
    [Min(1f)] public float flickerRadius = 7f;
    [Min(0.2f)] public float flickerSeconds = 1.6f;
    [Tooltip("ประตูเปิดค้างกี่วินาทีก่อนค่อยๆ ปิดเอง")]
    [Min(0.5f)] public float holdOpenSeconds = 3.5f;
    [Tooltip("เกิดเหตุได้วันละครั้งต่อห้อง (ครั้งต่อไป = เงียบ)")]
    public bool oncePerDay = true;

    [Header("ข้อความ")]
    public string noAnswerText = "...ไม่มีใครตอบ";
    public string eveningText = "...ไม่มีใครตอบ (ตกเย็นแล้ว ผู้เช่าไม่ออกมาแล้ว)";
    public string vanishedAfterText = "ห้องว่างเปล่า... ราวกับไม่เคยมีใครอยู่";
    public string noActionsText = "วันนี้หมดแรงสัมภาษณ์แล้ว (แอ็กชันครบ 4) — ลองใหม่พรุ่งนี้";

    Door door;
    bool busy;

    public static bool AnyBusy { get; private set; }

    void Awake()
    {
        door = GetComponent<Door>();
        door.LockedAttempt += OnLockedAttempt;
        var sl = GetComponent<StoryLock>();
        if (sl) sl.lockedMessage = "";   // ไม่ขึ้น "Locked" ภาษาอังกฤษ เราจัดการเอง
    }

    void OnDestroy() { if (door) door.LockedAttempt -= OnLockedAttempt; }

    bool TenantGone => tenant && tenant.IsGone;
    Kind Effective => kind == Kind.Tenant && TenantGone ? Kind.Vanished : kind;
    string DayFlag => "knock_" + room + "_n" + GameState.Night;

    /// <summary>ข้อความปุ่มที่ HUD ใช้แทน "Open"</summary>
    public string Prompt
    {
        get
        {
            if (Effective == Kind.Tenant && tenant && tenant.Info != null)
                return GameState.Interviewed.Contains(tenant.tenantId) ? "เคาะประตูห้อง " + room + " (คุยกับ" + tenant.Info.name + ")"
                                                                        : "เคาะประตูห้อง " + room + " (สัมภาษณ์" + tenant.Info.name + ")";
            return "เคาะประตูห้อง " + room;
        }
    }

    void OnLockedAttempt()
    {
        if (busy || AnyBusy || (tenant && tenant.IsOut)) return;
        StartCoroutine(Knock());
    }

    IEnumerator Knock()
    {
        busy = true; AnyBusy = true;
        var kindNow = Effective;

        // หมดแอ็กชัน = ไม่ต้องเคาะสัมภาษณ์คนใหม่
        bool day = DormFX.Phase == DayPhase.Day;
        if (kindNow == Kind.Tenant && day && tenant && !GameState.Interviewed.Contains(tenant.tenantId) && DormFX.ActionsLeft <= 0)
        {
            DormFX.Msg(noActionsText, 3f);
            busy = false; AnyBusy = false;
            yield break;
        }

        Vector3 at = door.transform.position + Vector3.up * 1.2f;
        var clip = DormFX.KnockClip;
        for (int i = 0; i < knockCount; i++)
        {
            if (clip) DormFX.Play(clip, at, knockVolume);
            yield return new WaitForSeconds(knockInterval);
        }
        GameState.Set("knocked_any");
        yield return new WaitForSeconds(Random.Range(answerDelay.x, Mathf.Max(answerDelay.x, answerDelay.y)));

        switch (kindNow)
        {
            case Kind.Tenant:
                if (tenantsOnlyByDay && !day) DormFX.Msg(eveningText, 3f);
                else if (tenant) { AnyBusy = false; yield return tenant.AnswerKnock(door); }
                break;
            case Kind.Vanished:
                if (oncePerDay && GameState.Has(DayFlag)) DormFX.Msg(noAnswerText, 2.5f);
                else { GameState.Set(DayFlag); yield return Vanished(); }
                break;
            default:
                DormFX.Msg(noAnswerText, 2.5f);
                break;
        }
        busy = false; AnyBusy = false;
    }

    // ประตูเปิดเอง... ไม่มีใคร
    IEnumerator Vanished()
    {
        door.SetLocked(false);
        door.TryOpen();
        yield return new WaitForSeconds(0.5f);

        var clip = eerieClip ? eerieClip : (SoundBank.I ? SoundBank.I.omen : null);
        if (clip) DormFX.Play(clip, door.transform.position + Vector3.up * 1.5f, eerieVolume);
        CameraDynamics.Shake(0.35f);
        if (DormFX.Sanity && sanityLoss > 0f) DormFX.Sanity.Drain(sanityLoss);
        yield return FlickerNearby();
        DormFX.Msg(vanishedAfterText, 3f);

        yield return new WaitForSeconds(holdOpenSeconds);
        if (door.IsOpen) door.Close();
        yield return new WaitForSeconds(0.8f);
        door.SetLocked(true);
    }

    IEnumerator FlickerNearby()
    {
        var lights = new List<(Light l, float i)>();
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.enabled && l.type != LightType.Directional && Vector3.Distance(l.transform.position, door.transform.position) < flickerRadius)
                lights.Add((l, l.intensity));
        for (float t = 0f; t < flickerSeconds; )
        {
            bool off = Random.value < 0.55f;
            foreach (var (l, i) in lights) if (l) l.intensity = off ? i * Random.Range(0f, 0.15f) : i;
            float w = Random.Range(0.04f, 0.14f);
            yield return new WaitForSeconds(w);
            t += w;
        }
        foreach (var (l, i) in lights) if (l) l.intensity = i;
    }
}
