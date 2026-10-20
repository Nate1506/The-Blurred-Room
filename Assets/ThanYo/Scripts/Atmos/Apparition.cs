using UnityEngine;

// เงา/ร่างที่โผล่ให้เห็นแวบๆ (เช่น ศพแขวนคอในหมอก, เงาดำสุดทางเดิน)
// - โผล่เฉพาะตอนจุดนั้น "อยู่นอกสายตา" (ไม่เห็นตอนมันโผล่ = เหมือนมันยืนอยู่ตรงนั้นมาตลอด)
// - หายเมื่อเดินเข้าใกล้ หรือจ้องนานเกิน -> ไฟกะพริบ/ภาพกะพริบ แล้วหายไป
// - ตั้งให้โผล่ในคัตซีนเปิดเกมได้ด้วย
public class Apparition : MonoBehaviour
{
    [Header("ภาพ")]
    public Texture2D texture;
    [Tooltip("ความสูงของร่าง (เมตร) ความกว้างคิดตามสัดส่วนภาพ")]
    [Min(0.2f)] public float height = 2.3f;
    [Tooltip("สีคูณภาพ (มืดลง = กลืนกับเงา)")]
    public Color tint = new Color(0.55f, 0.55f, 0.6f);
    [Range(0f, 1f)] public float opacity = 0.9f;
    [Tooltip("ลอยเหนือพื้นกี่เมตร (ศพแขวน = เท้าลอย)")]
    public float lift = 0.4f;
    [Tooltip("วัสดุต้นแบบ (ใส่ให้อัตโนมัติจากเมนู 11 — ทำให้ตอน build เจอ shader)")]
    public Material unlitSource;

    [Header("แกว่ง (แบบแขวนเชือก)")]
    [Range(0f, 15f)] public float swayDegrees = 3f;
    [Min(0.5f)] public float swayPeriod = 5f;

    [Header("ในคัตซีนเปิดเกม")]
    public bool showInOpeningCutscene = true;
    [Tooltip("จุดที่ยืนตอนคัตซีน")]
    public Transform cutsceneSpot;

    [Header("ระหว่างเล่น")]
    [Tooltip("จุดที่อาจโผล่ (สุ่ม)")]
    public Transform[] spots;
    [Tooltip("สุ่มโผล่ทุกๆ กี่วินาที")]
    [Min(1f)] public float checkEvery = 25f;
    [Tooltip("โอกาสโผล่ในแต่ละรอบ")]
    [Range(0f, 1f)] public float chance = 0.35f;
    [Tooltip("ต้องอยู่ห่างผู้เล่นอย่างน้อย (เมตร)")]
    public float minDistance = 12f;
    [Tooltip("เข้าใกล้กว่านี้ = หาย")]
    public float vanishDistance = 8f;
    [Tooltip("จ้องตรงๆ นานกว่านี้ = หาย (วินาที)")]
    public float stareSeconds = 1.5f;
    [Tooltip("อยู่ให้เห็นนานสุด (วินาที) แล้วหายเอง")]
    public float maxShowSeconds = 40f;
    [Tooltip("เสียงตอนหาย (ว่างได้)")]
    public AudioClip vanishSound;
    [Range(0f, 1f)] public float vanishVolume = 0.5f;

    Transform body;
    Renderer rend;
    MaterialPropertyBlock mpb;   // สร้างใน Start (สร้างตอนประกาศไม่ได้)
    bool visible, vanishing, cutsceneMode;
    float timer, stare, shownAt, vanishT;

    void Start()
    {
        mpb = new MaterialPropertyBlock();
        if (!texture) { enabled = false; return; }
        var go = new GameObject("Apparition_Body", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(transform, false);
        go.GetComponent<MeshFilter>().sharedMesh = AtmosUtil.BottomQuad();
        rend = go.GetComponent<MeshRenderer>();
        rend.sharedMaterial = AtmosUtil.TransparentMat(texture, false, false, unlitSource);
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        body = go.transform;
        float w = height * texture.width / Mathf.Max(1f, texture.height);
        body.localScale = new Vector3(w, height, 1f);
        Show(false);
        timer = checkEvery * Random.Range(0.3f, 0.8f);
    }

    void Show(bool on)
    {
        visible = on; vanishing = false; stare = 0f;
        rend.enabled = on;
        if (on) shownAt = Time.time;
    }

    void PlaceAt(Transform spot)
    {
        transform.position = spot.position;
    }

    void LateUpdate()
    {
        var cam = AtmosUtil.ActiveCamera();
        if (!cam) return;

        // ---------- คัตซีน ----------
        if (showInOpeningCutscene && OpeningCutscene.Playing && cutsceneSpot)
        {
            if (!cutsceneMode) { cutsceneMode = true; PlaceAt(cutsceneSpot); Show(true); }
            Draw(cam, 1f);
            return;
        }
        if (cutsceneMode) { cutsceneMode = false; Show(false); timer = 0f; }   // คัตซีนจบ = หายไปแล้ว

        // ---------- ระหว่างเล่น ----------
        if (!visible)
        {
            timer += Time.deltaTime;
            if (timer >= checkEvery)
            {
                timer = 0f;
                if (spots != null && spots.Length > 0 && Random.value < chance) TryAppear(cam);
            }
            return;
        }

        if (vanishing)
        {
            vanishT += Time.deltaTime;
            float flick = (Mathf.Sin(vanishT * 70f) > 0f) ? 1f : 0.15f;
            Draw(cam, flick * (1f - vanishT / 0.35f));
            if (vanishT > 0.35f) Show(false);
            return;
        }

        Vector3 toMe = body.position + Vector3.up * height * 0.5f - cam.transform.position;
        float dist = toMe.magnitude;
        bool looking = Vector3.Angle(cam.transform.forward, toMe) < 7f;
        stare = looking ? stare + Time.deltaTime : Mathf.Max(0f, stare - Time.deltaTime * 0.5f);
        if (dist < vanishDistance || stare > stareSeconds || Time.time - shownAt > maxShowSeconds) Vanish();
        Draw(cam, 1f);
    }

    void TryAppear(Camera cam)
    {
        for (int tries = 0; tries < spots.Length; tries++)
        {
            var s = spots[Random.Range(0, spots.Length)];
            if (!s) continue;
            Vector3 p = s.position + Vector3.up * height * 0.5f;
            if (Vector3.Distance(p, cam.transform.position) < minDistance) continue;
            Vector3 vp = cam.WorldToViewportPoint(p);
            bool inView = vp.z > 0f && vp.x > -0.1f && vp.x < 1.1f && vp.y > -0.1f && vp.y < 1.1f;
            if (inView) continue;   // โผล่เฉพาะตอนไม่ได้มอง
            PlaceAt(s);
            Show(true);
            return;
        }
    }

    void Vanish()
    {
        vanishing = true; vanishT = 0f;
        if (vanishSound) AudioSource.PlayClipAtPoint(vanishSound, body.position, vanishVolume);
    }

    void Draw(Camera cam, float alphaMul)
    {
        body.localPosition = Vector3.up * lift;
        AtmosUtil.FaceYaw(body, cam.transform.position);
        // แกว่งจากจุดบน (เชือก): หมุนรอบแกนหน้า แล้วเลื่อนให้ขอบบนอยู่ที่เดิม
        float ang = swayDegrees * Mathf.Sin(Time.time * Mathf.PI * 2f / swayPeriod);
        Vector3 top = transform.position + Vector3.up * (lift + height);
        body.rotation = Quaternion.AngleAxis(ang, body.forward) * body.rotation;
        body.position = top - body.up * height;
        var c = tint; c.a = opacity * Mathf.Clamp01(alphaMul);
        AtmosUtil.SetColor(rend, mpb, c);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.7f);
        if (cutsceneSpot) Gizmos.DrawWireCube(cutsceneSpot.position + Vector3.up * (lift + height * 0.5f), new Vector3(0.6f, height, 0.6f));
        if (spots != null)
            foreach (var s in spots) if (s) Gizmos.DrawWireCube(s.position + Vector3.up * (lift + height * 0.5f), new Vector3(0.5f, height, 0.5f));
    }
}
