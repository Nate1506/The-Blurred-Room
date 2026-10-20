using System.Collections.Generic;
using UnityEngine;

// หมอก/ควันเรี่ยพื้น: แผ่นภาพควันหลายแผ่นลอยอยู่ในกล่อง ค่อยๆ ลอยตามลม หายใจเข้า-ออก (จางลง-เข้มขึ้น)
// หันเข้ากล้องเสมอ และจางหายเมื่อกล้องเข้าใกล้ (ไม่เห็นขอบแผ่นตัดหน้ากล้อง)
// วางที่ไหนก็ได้ -> ปรับขนาดกล่อง (ดูกรอบสีฟ้าใน Scene) แล้วปรับค่าด้านล่าง
public class FogCards : MonoBehaviour
{
    [Header("ภาพควัน (สุ่มใช้)")]
    public Texture2D[] textures;

    [Header("พื้นที่ (กล่องรอบตัวออบเจกต์นี้)")]
    public Vector3 areaSize = new Vector3(30f, 1f, 30f);
    [Tooltip("จำนวนแผ่นควัน")]
    [Range(1, 200)] public int count = 30;

    [Header("ขนาดแผ่น (เมตร)")]
    public Vector2 widthRange = new Vector2(6f, 12f);
    public Vector2 heightRange = new Vector2(1.5f, 3.5f);
    [Tooltip("จมลงพื้นกี่ส่วนของความสูง (ให้ขอบล่างกลืนกับพื้น)")]
    [Range(0f, 0.6f)] public float sink = 0.25f;

    [Header("สี")]
    [Tooltip("รับแสงจากไฟในฉาก (มืดในที่มืด สว่างใต้ไฟ/ไฟหน้ารถ) — ปิด = สว่างเท่ากันทั้งหมด")]
    public bool lit = true;
    [Tooltip("วัสดุต้นแบบ (ใส่ให้อัตโนมัติจากเมนู 11 — ทำให้ตอน build เจอ shader)")]
    public Material litSource, unlitSource;
    [Tooltip("ใช้สีหมอกของฉาก (RenderSettings.fogColor) ให้กลืนกับบรรยากาศ")]
    public bool matchSceneFog = true;
    public Color tint = new Color(0.75f, 0.78f, 0.82f);
    [Range(0f, 3f)] public float brightness = 1.15f;
    [Range(0f, 1f)] public float opacity = 0.35f;

    [Header("การเคลื่อนไหว")]
    [Tooltip("ทิศลม + ความเร็ว (เมตร/วินาที)")]
    public Vector3 wind = new Vector3(0.25f, 0f, 0.08f);
    [Tooltip("จางลง-เข้มขึ้นเป็นจังหวะ (วินาทีต่อรอบ)")]
    public Vector2 breathePeriod = new Vector2(8f, 16f);
    [Range(0f, 1f)] public float breatheAmount = 0.45f;

    [Header("ใกล้กล้อง")]
    [Tooltip("เริ่มจางเมื่อกล้องเข้าใกล้กว่านี้ (เมตร)")]
    public float fadeNear = 6f;
    [Tooltip("หายหมดเมื่อใกล้กว่านี้")]
    public float fadeGone = 1.5f;

    class Card { public Transform t; public Renderer r; public Vector3 pos; public float phase, period, w, h; }
    readonly List<Card> cards = new List<Card>();
    MaterialPropertyBlock mpb;   // สร้างใน Start (สร้างตอนประกาศไม่ได้)
    Material[] mats;

    void Start()
    {
        mpb = new MaterialPropertyBlock();
        if (textures == null || textures.Length == 0) { enabled = false; return; }
        mats = new Material[textures.Length];
        for (int i = 0; i < textures.Length; i++) mats[i] = AtmosUtil.TransparentMat(textures[i], false, lit, lit ? litSource : unlitSource);
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Fog_" + i, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = AtmosUtil.BottomQuad();
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mats[Random.Range(0, mats.Length)];
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var c = new Card
            {
                t = go.transform, r = r,
                phase = Random.value * 10f,
                period = Random.Range(breathePeriod.x, Mathf.Max(breathePeriod.x, breathePeriod.y)),
                w = Random.Range(widthRange.x, widthRange.y),
                h = Random.Range(heightRange.x, heightRange.y),
            };
            c.pos = new Vector3(Random.Range(-0.5f, 0.5f) * areaSize.x, Random.Range(-0.5f, 0.5f) * areaSize.y, Random.Range(-0.5f, 0.5f) * areaSize.z);
            if (Random.value < 0.5f) c.w = -c.w;   // กลับด้านภาพ ไม่ให้ซ้ำตา
            cards.Add(c);
        }
    }

    void LateUpdate()
    {
        var cam = AtmosUtil.ActiveCamera();
        if (!cam) return;
        Vector3 cp = cam.transform.position;
        Color baseCol = (matchSceneFog && RenderSettings.fog ? RenderSettings.fogColor : tint) * brightness;
        Vector3 half = areaSize * 0.5f;
        Vector3 drift = transform.InverseTransformVector(wind) * Time.deltaTime;

        foreach (var c in cards)
        {
            // ลอยตามลม วนกลับเข้ากล่อง
            Vector3 p = c.pos + drift;
            if (p.x > half.x) p.x -= areaSize.x; else if (p.x < -half.x) p.x += areaSize.x;
            if (p.z > half.z) p.z -= areaSize.z; else if (p.z < -half.z) p.z += areaSize.z;
            c.pos = p;

            c.t.localScale = new Vector3(c.w, c.h, 1f);
            c.t.localPosition = p;
            Vector3 wp = c.t.position; wp.y -= c.h * sink;   // จมลงพื้นนิดนึง
            c.t.position = wp;
            AtmosUtil.FaceYaw(c.t, cp);

            float d = Vector3.Distance(new Vector3(cp.x, wp.y, cp.z), wp);
            float near = Mathf.InverseLerp(fadeGone, Mathf.Max(fadeGone + 0.01f, fadeNear), d);
            // ขอบกล่องจางลง ไม่โผล่/หายกระโดด
            float edge = Mathf.Clamp01((half.x - Mathf.Abs(p.x)) / Mathf.Max(0.01f, half.x * 0.25f)) *
                         Mathf.Clamp01((half.z - Mathf.Abs(p.z)) / Mathf.Max(0.01f, half.z * 0.25f));
            float breathe = 1f - breatheAmount * (0.5f + 0.5f * Mathf.Sin((Time.time + c.phase * c.period) * Mathf.PI * 2f / c.period));
            var col = baseCol; col.a = opacity * near * edge * breathe;
            AtmosUtil.SetColor(c.r, mpb, col);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, areaSize);
    }
}
