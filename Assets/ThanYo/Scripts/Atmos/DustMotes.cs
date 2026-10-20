using UnityEngine;

// ฝุ่นลอยในอากาศ: จุดเล็กๆ ลอยช้าๆ แบบสุ่ม เห็นชัดตรงที่มีแสง (ทางเดิน ล็อบบี้ ไฟฉาย)
// สร้าง Particle System ให้เองตอนเริ่ม — ปรับทุกอย่างจากช่องด้านล่าง
public class DustMotes : MonoBehaviour
{
    [Header("พื้นที่ (กล่องรอบตัวออบเจกต์นี้)")]
    public Vector3 areaSize = new Vector3(20f, 2.4f, 2f);

    [Header("ฝุ่น")]
    [Tooltip("จำนวนฝุ่นที่ลอยอยู่พร้อมกัน")]
    [Range(10, 3000)] public int amount = 350;
    [Tooltip("ขนาดเม็ดฝุ่น (เมตร)")]
    public Vector2 size = new Vector2(0.006f, 0.02f);
    public Color color = new Color(1f, 0.92f, 0.78f);
    [Range(0f, 1f)] public float opacity = 0.35f;
    [Tooltip("เรืองแสงเล็กน้อย (บวกสี) = เห็นชัดในที่มืดกว่า")]
    public bool additive = true;
    [Tooltip("วัสดุต้นแบบ (ใส่ให้อัตโนมัติจากเมนู 11 — ทำให้ตอน build เจอ shader)")]
    public Material unlitSource;

    [Header("การลอย")]
    [Tooltip("ความแรงของการลอยไปมาแบบสุ่ม")]
    [Range(0f, 1f)] public float turbulence = 0.12f;
    [Tooltip("ลอยขึ้น(+)/ตกลง(-) ช้าๆ")]
    [Range(-0.2f, 0.2f)] public float rise = 0.01f;
    [Tooltip("อายุเม็ดฝุ่น (วินาที)")]
    public Vector2 lifetime = new Vector2(8f, 16f);

    ParticleSystem ps;

    void Start()
    {
        var go = new GameObject("Dust_PS");
        go.transform.SetParent(transform, false);
        ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true; main.playOnAwake = false; main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        var c = color; c.a = opacity;
        main.startColor = c;
        main.maxParticles = amount;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -rise;   // ค่าบวก = ลอยขึ้น

        var em = ps.emission;
        em.rateOverTime = amount / Mathf.Max(0.1f, (lifetime.x + lifetime.y) * 0.5f);

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = areaSize;

        var noise = ps.noise;
        noise.enabled = turbulence > 0f;
        noise.strength = turbulence;
        noise.frequency = 0.25f;
        noise.scrollSpeed = 0.05f;
        noise.damping = true;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = AtmosUtil.TransparentMat(DotTexture(), additive, false, unlitSource);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.minParticleSize = 0f;
        r.maxParticleSize = 0.05f;

        ps.Play();
    }

    static Texture2D dot;
    static Texture2D DotTexture()
    {
        if (dot) return dot;
        const int n = 32;
        dot = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "DustDot" };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f - 0.5f, n / 2f - 0.5f)) / (n / 2f);
                float a = Mathf.Clamp01(1f - d); a *= a;
                dot.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        dot.Apply();
        return dot;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.6f, 0.6f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, areaSize);
    }
}
