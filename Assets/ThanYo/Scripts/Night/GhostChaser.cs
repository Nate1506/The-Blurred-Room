using System.Collections;
using System.Collections.Generic;
using BlurredRoom;
using UnityEngine;
using UnityEngine.AI;

// ผีที่ไล่ผู้เล่น
//   - ร่าง: เอาโมเดลผู้เช่ามาดัด (ดำ ผอมสูง คอยาว หัวเอียง ตาแดง เดินกระตุก) — ปรับทุกอย่างใน Inspector
//   - อยู่บน layer Spectral = ตาเปล่ามองไม่เห็น ต้องยกกล้องวิดีโอ (คลิกขวา) ถึงเห็น — แต่ได้ยินเสียงหายใจ/ฝีเท้าตลอด
//   - โดนกล้องจับภาพ = ช้าลงมาก (ใช้หนีได้ แต่แบตกล้องจำกัด)
//   - เดินหาตามจุด patrol, เห็น/ได้ยินผู้เล่น = ไล่, เข้าใกล้พอ = จับ (จัมป์สแกร์ -> ตาย -> เกิดที่ checkpoint)
//   - เดินด้วย NavMesh ที่อบตอนเริ่มเกม (NightNavMesh) ไม่ต้องลงแพ็กเกจเพิ่ม
public class GhostChaser : MonoBehaviour
{
    public enum State { Hidden, Patrol, Chase, Catching }

    [Header("ร่าง")]
    [Tooltip("โมเดลที่เอามาดัดเป็นผี (Prefabs/Tenants/Tenant_xxx)")]
    public GameObject modelPrefab;
    [Tooltip("สเกลร่าง (Y มากกว่า = ผอมสูง)")]
    public Vector3 bodyScale = new Vector3(0.88f, 1.2f, 0.88f);
    [Tooltip("สีผิว (ซีดๆ จะเห็นชัดในความมืดผ่านกล้อง)")]
    public Color skinColor = new Color(0.62f, 0.6f, 0.57f);
    [Range(0f, 1f)] public float skinSmoothness = 0.35f;
    [Tooltip("เรืองแสงจางๆ ให้พอเห็นเค้าโครงในที่มืดสนิท")]
    public Color skinGlow = new Color(0.05f, 0.05f, 0.055f);
    public Color eyeColor = new Color(1f, 0.05f, 0.02f);
    [Range(0f, 20f)] public float eyeGlow = 6f;
    [Tooltip("ตาอยู่สูงกว่ากระดูกหัว / ยื่นไปข้างหน้ากี่เมตร (ปรับถ้าตาไม่อยู่บนหน้า)")]
    [Range(-0.2f, 0.3f)] public float eyeHeight = 0.08f;
    [Range(-0.2f, 0.3f)] public float eyeForward = 0.1f;
    [Tooltip("ยืดคอ (เมตร)")]
    [Range(0f, 0.4f)] public float neckStretch = 0.16f;
    [Tooltip("หัวเอียง (องศา)")]
    [Range(-90f, 90f)] public float headTilt = 38f;
    [Tooltip("หัวกระตุกแรงแค่ไหน (องศา)")]
    [Range(0f, 40f)] public float headTwitch = 14f;
    [Tooltip("ยืดแขน (คูณความยาวกระดูกแขน)")]
    [Range(1f, 1.8f)] public float armStretch = 1.35f;

    [Header("มองเห็นได้ไหม")]
    [Tooltip("เห็นเฉพาะผ่านกล้องวิดีโอ")]
    public bool onlyVisibleThroughCamera = true;
    public string spectralLayer = "Spectral";

    [Header("การเคลื่อนที่")]
    [Range(0.2f, 4f)] public float patrolSpeed = 1.1f;
    [Tooltip("ผู้เล่นเดิน 2.2 วิ่ง 4.5 — ค่านี้อยู่ระหว่างกลาง = ต้องวิ่งหนี")]
    [Range(0.5f, 6f)] public float chaseSpeed = 3.3f;
    [Tooltip("โดนกล้องจับภาพแล้วเหลือความเร็วกี่เท่า")]
    [Range(0f, 1f)] public float filmedSpeedMultiplier = 0.2f;
    [Tooltip("เดินกระตุก (หยุดสั้นๆ แบบสุ่ม)")]
    [Range(0f, 1f)] public float stutter = 0.35f;
    public Transform[] patrolPoints;

    [Header("การรับรู้")]
    [Min(1f)] public float sightRange = 11f;
    [Range(10f, 180f)] public float sightAngle = 120f;
    [Tooltip("ได้ยินผู้เล่นวิ่งในระยะนี้ (ผ่านกำแพงด้วย)")]
    [Min(0f)] public float hearRunRange = 9f;
    [Tooltip("ได้ยินผู้เล่นเดินในระยะนี้")]
    [Min(0f)] public float hearWalkRange = 3.5f;
    [Tooltip("ไล่ต่อกี่วินาทีหลังคลาดสายตา")]
    [Min(0f)] public float memorySeconds = 5f;
    [Min(0.3f)] public float catchDistance = 1.05f;

    [Header("จับได้")]
    [TextArea] public string deathText = "มันจับเราได้...";
    [Min(0.2f)] public float catchScareSeconds = 1.1f;

    [Header("เสียง")]
    public AudioClip breathLoop;
    public AudioClip stepClip;
    public AudioClip screamClip;
    public AudioClip chaseStinger;
    [Range(0f, 1f)] public float breathVolume = 0.9f;
    [Tooltip("ได้ยินเสียงไกลสุด (เมตร)")]
    [Min(2f)] public float hearingDistance = 16f;

    [Header("เริ่มต้น")]
    [Tooltip("โผล่ตั้งแต่เริ่มซีน (ปกติให้ขั้นในเดโมสั่ง Appear)")]
    public bool startActive;

    [Header("สถานะ (อ่านอย่างเดียว)")]
    [SerializeField] State state = State.Hidden;
    [SerializeField] bool filmed;

    public State CurrentState => state;
    public bool IsFilmed => filmed;

    NavMeshAgent agent;
    GameObject body;
    Animator anim;
    Transform head, neck, lArm, rArm;
    Vector3 neckBase;
    Quaternion headBase;
    AudioSource breath, sfx;
    Vector3 startPos; Quaternion startRot;
    int patrolIndex;
    float lastSeen = -99f, stutterUntil, nextStep, nextStutter;
    Vector3 lastKnown;
    readonly List<Renderer> renderers = new List<Renderer>();

    void Awake()
    {
        startPos = transform.position; startRot = transform.rotation;
        BuildBody();
        agent = GetComponent<NavMeshAgent>();
        if (!agent) agent = gameObject.AddComponent<NavMeshAgent>();
        agent.radius = 0.3f; agent.height = 2f; agent.acceleration = 30f; agent.angularSpeed = 540f; agent.stoppingDistance = 0.2f;
        agent.autoBraking = false; agent.enabled = false;

        breath = gameObject.AddComponent<AudioSource>();
        breath.clip = breathLoop ? breathLoop : NightAudio.Breath; breath.loop = true;
        breath.spatialBlend = 1f; breath.rolloffMode = AudioRolloffMode.Linear; breath.minDistance = 1.5f; breath.maxDistance = hearingDistance;
        breath.volume = breathVolume; breath.dopplerLevel = 0f;
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.spatialBlend = 1f; sfx.rolloffMode = AudioRolloffMode.Linear; sfx.minDistance = 1.5f; sfx.maxDistance = hearingDistance; sfx.dopplerLevel = 0f;

        NightDemo.Respawned += OnRespawn;
        SetVisible(false);
    }

    IEnumerator Start()
    {
        yield return null;   // รอ NightNavMesh อบเสร็จ
        if (startActive) Appear();
    }

    void OnDestroy() => NightDemo.Respawned -= OnRespawn;

    void OnRespawn() => Vanish();

    // =====================================================================
    //  public API (ต่อจาก NightDemo Step / UnityEvent ได้)
    // =====================================================================
    /// <summary>โผล่ที่ตำแหน่งเริ่มต้น แล้วเริ่มเดินหา</summary>
    [ContextMenu("โผล่ (ทดสอบ)")]
    public void Appear() => AppearAt(null);

    public void AppearAt(Transform at)
    {
        Vector3 p = at ? at.position : startPos;
        Quaternion r = at ? at.rotation : startRot;
        if (NavMesh.SamplePosition(p, out var hit, 2f, NavMesh.AllAreas)) p = hit.position;
        transform.SetPositionAndRotation(p, r);
        agent.enabled = true;
        agent.Warp(p);
        agent.isStopped = false;
        SetLayer(LayerMask.NameToLayer(spectralLayer));
        SetVisible(true);
        breath.Play();
        state = State.Patrol;
        lastSeen = -99f;
        GoNextPatrol();
    }

    [ContextMenu("หายไป (ทดสอบ)")]
    public void Vanish()
    {
        StopAllCoroutines();
        state = State.Hidden;
        if (agent.enabled) agent.enabled = false;
        SetVisible(false);
        breath.Stop();
        filmed = false;
    }

    /// <summary>โผล่ที่จุด patrol ที่ไกลจากผู้เล่นที่สุด (แต่ไม่เกิน maxDistance) — ให้มีเวลาตั้งตัว</summary>
    [ContextMenu("โผล่ไกลจากผู้เล่น (ทดสอบ)")]
    public void AppearFarFromPlayer()
    {
        var p = DormFX.Player;
        Transform best = null; float bestD = -1f;
        if (p && patrolPoints != null)
            foreach (var t in patrolPoints)
            {
                if (!t) continue;
                float d = Vector3.Distance(t.position, p.position);
                if (Mathf.Abs(t.position.y - p.position.y) > 1.5f) d -= 100f;   // ชั้นเดียวกันก่อน
                if (d > bestD) { bestD = d; best = t; }
            }
        AppearAt(best);
    }

    /// <summary>โผล่ตรงหน้าแล้วจับทันที (ใช้ตอนเปิดประตูให้ของปลอม)</summary>
    public void CatchNow(Vector3 at, string reason = null)
    {
        Vanish();
        transform.position = at;
        var p = DormFX.Player;
        if (p) { Vector3 l = p.position - at; l.y = 0; if (l.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(l); }
        SetVisible(true);
        breath.Play();
        if (!string.IsNullOrEmpty(reason)) deathOverride = reason;
        StartCoroutine(Catch());
    }

    string deathOverride;

    /// <summary>บังคับให้ไล่ผู้เล่นทันที (เช่น ตอนเปิดประตูผิด)</summary>
    public void ForceChase()
    {
        if (state == State.Hidden) Appear();
        lastSeen = Time.time; state = State.Chase;
        if (chaseStinger) DormFX.Play2D(chaseStinger, 0.7f);
    }

    // =====================================================================
    void Update()
    {
        if (state == State.Hidden || state == State.Catching) return;
        var player = DormFX.Player;
        if (!player || !agent.isOnNavMesh) return;
        if (NightDemo.I && NightDemo.I.IsDying) return;

        Vector3 eye = transform.position + Vector3.up * 1.7f;
        Vector3 pTarget = player.position + Vector3.up * 1.2f;
        float dist = Vector3.Distance(transform.position, player.position);

        filmed = Camcorder.I && Camcorder.I.Sees(eye - Vector3.up * 0.4f);

        if (CanSense(player, eye, pTarget, dist))
        {
            if (state != State.Chase && chaseStinger) DormFX.Play2D(chaseStinger, 0.6f);
            lastSeen = Time.time; lastKnown = player.position; state = State.Chase;
        }

        if (state == State.Chase)
        {
            if (Time.time - lastSeen > memorySeconds) { state = State.Patrol; GoNextPatrol(); }
            else agent.SetDestination(Time.time - lastSeen < 0.5f ? player.position : lastKnown);
        }
        else if (!agent.pathPending && agent.remainingDistance < 0.5f) GoNextPatrol();

        // ความเร็ว + เดินกระตุก
        float speed = state == State.Chase ? chaseSpeed : patrolSpeed;
        if (filmed) speed *= filmedSpeedMultiplier;
        if (Time.time > nextStutter)
        {
            nextStutter = Time.time + Random.Range(0.6f, 2.2f);
            if (Random.value < stutter) stutterUntil = Time.time + Random.Range(0.08f, 0.35f);
        }
        if (Time.time < stutterUntil) speed *= 0.05f;
        agent.speed = speed;
        if (anim) anim.speed = Mathf.Lerp(0.6f, 1.6f, agent.velocity.magnitude / Mathf.Max(0.1f, chaseSpeed)) * (Time.time < stutterUntil ? 0f : 1f);
        if (anim) anim.SetBool("Walk", agent.velocity.sqrMagnitude > 0.05f);

        // ฝีเท้า
        if (agent.velocity.sqrMagnitude > 0.1f && Time.time > nextStep)
        {
            nextStep = Time.time + Mathf.Lerp(0.75f, 0.33f, agent.velocity.magnitude / Mathf.Max(0.1f, chaseSpeed));
            sfx.pitch = Random.Range(0.75f, 0.95f);
            sfx.PlayOneShot(stepClip ? stepClip : NightAudio.Step, 0.9f);
        }

        if (dist < catchDistance && Mathf.Abs(player.position.y - transform.position.y) < 1.2f) StartCoroutine(Catch());
    }

    bool CanSense(Transform player, Vector3 eye, Vector3 target, float dist)
    {
        var move = player.GetComponent<PlayerMovement>();
        bool running = move && move.IsRunning;
        bool moving = move && move.IsMoving;
        if (running && dist < hearRunRange) return true;
        if (moving && dist < hearWalkRange) return true;
        if (dist > sightRange) return false;
        Vector3 to = target - eye;
        if (Vector3.Angle(transform.forward, to) > sightAngle * 0.5f) return false;
        int mask = ~((1 << gameObject.layer) | (1 << 2));
        return !Physics.Raycast(eye, to.normalized, to.magnitude - 0.4f, mask, QueryTriggerInteraction.Ignore);
    }

    void GoNextPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            // ไม่มีจุด patrol -> เดินสุ่มรอบตัว
            Vector3 rnd = transform.position + Random.insideUnitSphere * 6f;
            if (NavMesh.SamplePosition(rnd, out var h, 3f, NavMesh.AllAreas)) agent.SetDestination(h.position);
            return;
        }
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        if (patrolPoints[patrolIndex]) agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    IEnumerator Catch()
    {
        state = State.Catching;
        if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
        var input = DormFX.Input;
        if (input) input.LockMovement(this);
        // บังคับให้เห็นตัวแม้ไม่ได้ยกกล้อง + หันกล้องไปที่หน้า
        SetLayer(0);
        if (head) CameraDynamics.SetFocus(head.position, 1f, -18f);
        DormFX.Play2D(screamClip ? screamClip : NightAudio.Scream, 1f);
        CameraDynamics.Shake(1f);
        for (float t = 0f; t < catchScareSeconds; t += Time.deltaTime)
        {
            if (head) CameraDynamics.SetFocus(head.position, 1f, -18f);
            var p = DormFX.Player;
            if (p)   // พุ่งเข้าหน้า
            {
                Vector3 front = p.position + p.forward * 0.55f;
                transform.position = Vector3.Lerp(transform.position, new Vector3(front.x, transform.position.y, front.z), Time.deltaTime * 10f);
                Vector3 look = p.position - transform.position; look.y = 0;
                if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(look);
            }
            yield return null;
        }
        if (input) input.UnlockMovement(this);
        NightDemo.Kill(string.IsNullOrEmpty(deathOverride) ? deathText : deathOverride);
        deathOverride = null;
        yield return new WaitForSeconds(0.2f);
        CameraDynamics.ClearFocus();
    }

    // =====================================================================
    //  body
    // =====================================================================
    void BuildBody()
    {
        if (!modelPrefab) { body = DormFX.BuildCreature(transform); }
        else
        {
            body = Instantiate(modelPrefab, transform);
            body.name = "GhostBody";
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            foreach (var c in body.GetComponentsInChildren<Collider>(true)) Destroy(c);
        }
        body.transform.localScale = Vector3.Scale(body.transform.localScale, bodyScale);
        anim = body.GetComponentInChildren<Animator>();

        var skin = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        skin.SetColor("_BaseColor", skinColor);
        skin.SetFloat("_Smoothness", skinSmoothness);
        skin.EnableKeyword("_EMISSION");
        skin.SetColor("_EmissionColor", skinGlow);
        renderers.AddRange(body.GetComponentsInChildren<Renderer>(true));
        foreach (var r in renderers)
        {
            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = skin;
            r.sharedMaterials = mats;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        foreach (var t in body.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLower();
            if (!head && n == "head") head = t;
            else if (!neck && n.Contains("neck")) neck = t;
            else if (!lArm && (n.Contains("leftforearm") || n.Contains("l_forearm") || n == "forearm.l")) lArm = t;
            else if (!rArm && (n.Contains("rightforearm") || n.Contains("r_forearm") || n == "forearm.r")) rArm = t;
        }
        if (head) { MakeEye(head, -0.035f); MakeEye(head, 0.035f); }
        if (neck) neckBase = neck.localPosition;
        if (head) headBase = head.localRotation;
        SetLayer(LayerMask.NameToLayer(spectralLayer));
    }

    void MakeEye(Transform h, float x)
    {
        var e = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(e.GetComponent<Collider>());
        e.name = "Eye";
        // วางในพิกัดโลกตามทิศของตัวผี (กระดูกหัวแต่ละโมเดลหันแกนไม่เหมือนกัน) แล้วค่อยผูกกับหัว
        float k = bodyScale.y;
        e.transform.position = h.position + transform.up * (eyeHeight * k) + transform.forward * (eyeForward * k) + transform.right * x;
        e.transform.localScale = Vector3.one * 0.022f;
        e.transform.SetParent(h, true);
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", eyeColor * eyeGlow);
        e.GetComponent<Renderer>().sharedMaterial = m;
        renderers.Add(e.GetComponent<Renderer>());
    }

    void LateUpdate()
    {
        if (state == State.Hidden) return;
        // ดัดร่างทับอนิเมชันทุกเฟรม
        // คำนวณจากท่าพักทุกเฟรม (ไม่สะสม) — กันคอยืด/หัวหมุนไม่หยุดตอนอนิเมชันไม่ได้เขียนทับ
        if (neck)
        {
            Vector3 up = neck.parent ? neck.parent.InverseTransformDirection(transform.up) : Vector3.up;
            float scale = neck.parent ? Mathf.Max(0.0001f, neck.parent.lossyScale.y) : 1f;
            neck.localPosition = neckBase + up * (neckStretch / scale);
        }
        if (head)
        {
            float tw = (Mathf.PerlinNoise(Time.time * 3f, 1.3f) - 0.5f) * 2f * headTwitch;
            if (Random.value < 0.01f) tw += Random.Range(-1f, 1f) * headTwitch * 2f;
            head.localRotation = headBase;
            head.rotation = Quaternion.AngleAxis(headTilt + tw, transform.forward) * head.rotation;
        }
        if (lArm) lArm.localScale = Vector3.one * armStretch;
        if (rArm) rArm.localScale = Vector3.one * armStretch;
    }

    void SetVisible(bool on)
    {
        if (body) body.SetActive(on);
    }

    void SetLayer(int layer)
    {
        if (layer < 0) layer = 0;
        if (!onlyVisibleThroughCamera && layer != 0) layer = 0;
        foreach (var t in GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, catchDistance);
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, hearRunRange);
        if (patrolPoints != null)
            for (int i = 0; i < patrolPoints.Length; i++)
                if (patrolPoints[i] && patrolPoints[(i + 1) % patrolPoints.Length])
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[(i + 1) % patrolPoints.Length].position);
    }
}
