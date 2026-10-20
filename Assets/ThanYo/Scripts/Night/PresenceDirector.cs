using System.Collections;
using System.Collections.Generic;
using BlurredRoom;
using UnityEngine;

// "มีอะไรอยู่ตรงนั้น" แบบเน้นจิตวิทยา — ไม่มีผีไล่ ไม่เห็นตัวชัดๆ
// ระหว่างช่วงที่กำหนด (เช่น ไฟดับ) จะสุ่มเหตุการณ์เล็กๆ ให้ผู้เล่นรู้สึกว่าถูกตาม:
//   - เสียงฝีเท้าเดินตามหลัง (หยุดเมื่อเราหยุด)        - เสียงหายใจข้างหลังตอนยืนนิ่งนาน
//   - ประตูห้องใกล้ๆ เปิด/ปิดเองเบาๆ หรือโดนเคาะรัว      - เสียงคนวิ่งขึ้นบันไดแล้วประตูปิดปังไกลๆ
//   - ไฟฉายกะพริบ / เสียงร้องไกลๆ                     - "เงา" แวบเดียวผ่านกล้อง (เห็นไม่ชัด หายเมื่อเล็งตรง)
// ทุกครั้งที่เกิด สติลดนิดหน่อย -> กล้องโคลง/หัวใจเต้น (CameraDynamics) = ความกดดันแทนการตาย
public class PresenceDirector : MonoBehaviour
{
    public static PresenceDirector I { get; private set; }

    [Header("ทำงานเมื่อไหร่")]
    [Tooltip("ทำงานอัตโนมัติระหว่างขั้นเหล่านี้ของ NightDemo (ว่าง = ต้องสั่ง Begin() เอง)")]
    public string[] activeDuringSteps = { "breaker" };

    [Header("จังหวะ")]
    [Tooltip("เว้นระยะระหว่างเหตุการณ์ (วินาที) สุ่มระหว่าง X-Y")]
    public Vector2 eventEvery = new Vector2(5f, 11f);
    [Tooltip("เหตุการณ์แรกเกิดหลังเริ่มกี่วินาที")]
    [Min(0f)] public float firstEventDelay = 3f;
    [Tooltip("สติที่ลดต่อเหตุการณ์")]
    [Range(0f, 20f)] public float sanityPerEvent = 4f;

    [Header("ฝีเท้าตามหลัง")]
    [Range(0f, 1f)] public float followStepsWeight = 1f;
    public AudioClip[] followStepClips;
    [Tooltip("อยู่ห่างข้างหลังกี่เมตร")]
    [Range(1f, 10f)] public float followDistance = 4f;
    [Range(0f, 1f)] public float followVolume = 0.7f;

    [Header("หายใจข้างหลัง (ตอนยืนนิ่ง)")]
    [Range(0f, 1f)] public float breathWeight = 0.7f;
    public AudioClip breathClip;
    [Tooltip("ยืนนิ่งกี่วินาทีถึงจะได้ยิน")]
    [Min(0.5f)] public float stillSeconds = 3f;
    [Range(0f, 1f)] public float breathVolume = 0.5f;

    [Header("ประตูแถวๆ นี้")]
    [Range(0f, 1f)] public float doorWeight = 0.8f;
    public AudioClip[] doorClips;
    [Tooltip("ค้นหาประตูในระยะนี้")]
    [Range(2f, 20f)] public float doorRadius = 9f;

    [Header("เสียงไกลๆ (คนวิ่ง/ร้อง/เคาะ)")]
    [Range(0f, 1f)] public float distantWeight = 0.6f;
    public AudioClip[] distantClips;
    [Range(0f, 1f)] public float distantVolume = 0.55f;

    [Header("ไฟฉายกะพริบ")]
    [Range(0f, 1f)] public float flickerWeight = 0.5f;
    public Vector2 flickerSeconds = new Vector2(0.6f, 2f);

    [Header("เงาแวบผ่านกล้อง")]
    [Range(0f, 1f)] public float glimpseWeight = 0.6f;
    [Tooltip("เงาที่จะโผล่ (ใส่ได้หลายตัว ระบบเลือกตัวที่อยู่ข้างหน้าแต่ไกลพอ)")]
    public GameObject[] glimpses;
    [Tooltip("ใกล้กว่านี้ไม่โผล่ (จะได้ไม่เห็นชัด)")]
    [Min(2f)] public float glimpseMinDistance = 6f;
    [Min(0.1f)] public float glimpseMaxSeconds = 1.6f;
    [Tooltip("ถ้าเล็งเงาเข้ากลางจอ หายทันที")]
    [Range(0f, 0.5f)] public float vanishWhenCentered = 0.12f;

    [Header("สถานะ")]
    [SerializeField] bool running;

    AudioSource follow, breath;
    Coroutine loop;
    float stillTime;
    Vector3 lastPos;

    void Awake()
    {
        I = this;
        follow = MakeSource("Presence_Steps");
        breath = MakeSource("Presence_Breath");
        foreach (var g in glimpses) if (g) g.SetActive(false);
        NightDemo.Respawned += Stop;
    }

    void OnDestroy() { if (I == this) I = null; NightDemo.Respawned -= Stop; }

    AudioSource MakeSource(string n)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, false);
        var s = go.AddComponent<AudioSource>();
        s.spatialBlend = 1f; s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 1f; s.maxDistance = 18f; s.dopplerLevel = 0f;
        return s;
    }

    void Update()
    {
        if (activeDuringSteps != null && activeDuringSteps.Length > 0)
        {
            bool should = false;
            foreach (var id in activeDuringSteps) if (NightDemo.IsCurrent(id)) should = true;
            if (NightDemo.I && NightDemo.I.IsDying) should = false;
            if (should && !running) Begin();
            else if (!should && running) Stop();
        }
        if (!running) return;

        var p = DormFX.Player;
        if (!p) return;
        float moved = (p.position - lastPos).magnitude;
        lastPos = p.position;
        stillTime = moved < 0.01f ? stillTime + Time.deltaTime : 0f;

        // ฝีเท้าตามหลังหยุดเมื่อเราหยุด (เหมือนมันรอ)
        if (follow.isPlaying && stillTime > 0.25f) follow.Stop();
        follow.transform.position = p.position - p.forward * followDistance;
        breath.transform.position = p.position - p.forward * 0.8f + Vector3.up * 1.5f;
    }

    [ContextMenu("เริ่ม (ทดสอบ)")]
    public void Begin()
    {
        if (running) return;
        running = true;
        lastPos = DormFX.Player ? DormFX.Player.position : Vector3.zero;
        loop = StartCoroutine(Loop());
    }

    [ContextMenu("หยุด")]
    public void Stop()
    {
        running = false;
        if (loop != null) StopCoroutine(loop);
        loop = null;
        StopAllCoroutines();
        if (follow) follow.Stop();
        if (breath) breath.Stop();
        foreach (var g in glimpses) if (g) g.SetActive(false);
    }

    IEnumerator Loop()
    {
        yield return new WaitForSeconds(firstEventDelay);
        while (running)
        {
            // หายใจข้างหลัง มีสิทธิ์ก่อนถ้ายืนนิ่งนาน
            if (stillTime > stillSeconds && breathClip && !breath.isPlaying && Random.value < breathWeight)
                yield return Breath();
            else
                yield return Pick();
            yield return new WaitForSeconds(Random.Range(eventEvery.x, eventEvery.y));
        }
    }

    IEnumerator Pick()
    {
        var options = new List<(float w, System.Func<IEnumerator> run)>
        {
            (followStepClips != null && followStepClips.Length > 0 ? followStepsWeight : 0f, FollowSteps),
            (doorWeight, Door),
            (distantClips != null && distantClips.Length > 0 ? distantWeight : 0f, Distant),
            (Flashlight.I ? flickerWeight : 0f, Flicker),
            (glimpses != null && glimpses.Length > 0 ? glimpseWeight : 0f, Glimpse),
        };
        float total = 0f; foreach (var o in options) total += o.w;
        if (total <= 0f) yield break;
        float r = Random.value * total;
        foreach (var o in options)
        {
            if ((r -= o.w) > 0f) continue;
            yield return o.run();
            yield break;
        }
    }

    void Drain()
    {
        var s = DormFX.Sanity;
        if (s && sanityPerEvent > 0f) s.Drain(sanityPerEvent);
    }

    // ---------- events ----------
    IEnumerator FollowSteps()
    {
        if (stillTime > 0.2f) yield break;   // ต้องเดินอยู่ถึงจะ "ตาม"
        follow.clip = followStepClips[Random.Range(0, followStepClips.Length)];
        follow.volume = followVolume; follow.pitch = Random.Range(0.9f, 1.05f);
        follow.Play();
        Drain();
        yield return new WaitForSeconds(Mathf.Min(follow.clip.length, 4f));
        follow.Stop();
    }

    IEnumerator Breath()
    {
        breath.clip = breathClip; breath.volume = breathVolume; breath.time = 0f;
        breath.Play();
        Drain();
        CameraDynamics.Shake(0.05f);
        for (float t = 0f; t < 4f && stillTime > 0.1f; t += Time.deltaTime) yield return null;   // ขยับเมื่อไหร่ เงียบทันที
        breath.Stop();
    }

    IEnumerator Door()
    {
        var p = DormFX.Player;
        Door best = null; float bestD = float.MaxValue;
        foreach (var d in FindObjectsByType<Door>(FindObjectsSortMode.None))
        {
            float dist = Vector3.Distance(d.transform.position, p.position);
            if (dist < 2.5f || dist > doorRadius || Mathf.Abs(d.transform.position.y - p.position.y) > 1.5f) continue;
            // เลือกประตูที่อยู่ข้างหลัง/ด้านข้าง มากกว่าข้างหน้า
            float behind = Vector3.Dot((d.transform.position - p.position).normalized, p.forward);
            float score = dist + behind * 4f + Random.value * 3f;
            if (score < bestD) { bestD = score; best = d; }
        }
        if (!best) yield break;
        AudioClip clip = doorClips != null && doorClips.Length > 0 ? doorClips[Random.Range(0, doorClips.Length)] : NightAudio.Slam;
        DormFX.Play(clip, best.transform.position + Vector3.up * 1.2f, 0.8f);
        Drain();
        if (Flashlight.I) Flashlight.I.Flicker(0.4f);
    }

    IEnumerator Distant()
    {
        var p = DormFX.Player;
        Vector3 dir = Quaternion.Euler(0, Random.Range(120f, 240f), 0) * p.forward;   // มาจากข้างหลังเป็นส่วนใหญ่
        Vector3 at = p.position + dir * Random.Range(8f, 14f) + Vector3.up * Random.Range(-1f, 3f);
        var go = new GameObject("Presence_Distant");
        go.transform.position = at;
        var s = go.AddComponent<AudioSource>();
        s.clip = distantClips[Random.Range(0, distantClips.Length)];
        s.spatialBlend = 1f; s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 3f; s.maxDistance = 30f; s.dopplerLevel = 0f;
        s.volume = distantVolume; s.Play();
        Destroy(go, s.clip.length + 0.2f);
        Drain();
        yield return null;
    }

    IEnumerator Flicker()
    {
        Flashlight.I.Flicker(Random.Range(flickerSeconds.x, flickerSeconds.y));
        yield return null;
    }

    IEnumerator Glimpse()
    {
        var cam = DormFX.PlayerCamera;
        if (!cam) yield break;
        // เลือกเงาที่อยู่ข้างหน้า ไกลพอ และไม่มีอะไรบัง
        GameObject pick = null;
        foreach (var g in glimpses)
        {
            if (!g) continue;
            Vector3 v = cam.WorldToViewportPoint(g.transform.position + Vector3.up * 1.4f);
            float d = Vector3.Distance(cam.transform.position, g.transform.position);
            if (v.z > 0f && d > glimpseMinDistance && v.x > 0.05f && v.x < 0.95f) { pick = g; break; }
        }
        if (!pick) yield break;
        pick.SetActive(true);
        for (float t = 0f; t < glimpseMaxSeconds; t += Time.deltaTime)
        {
            Vector3 v = cam.WorldToViewportPoint(pick.transform.position + Vector3.up * 1.4f);
            if (Mathf.Abs(v.x - 0.5f) < vanishWhenCentered && Mathf.Abs(v.y - 0.5f) < vanishWhenCentered * 2f) break;   // เล็งตรง = หาย
            yield return null;
        }
        pick.SetActive(false);
        Drain();
    }
}
