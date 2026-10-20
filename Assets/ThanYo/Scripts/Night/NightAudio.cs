using UnityEngine;

// เสียงสำรองที่สร้างจากโค้ด (ใช้เมื่อยังไม่ได้ใส่ไฟล์เสียงจริงในช่อง AudioClip ของแต่ละคอมโพเนนต์)
// พอมีไฟล์เสียงจริง ลากใส่ช่องใน Inspector ได้เลย ตัวนี้จะไม่ถูกใช้
public static class NightAudio
{
    /// <summary>ปิดเสียงสังเคราะห์ทั้งหมด (ใช้เสียงจริงจาก SoundBank / ช่องใน Inspector แทน)</summary>
    public static bool Enabled = false;

    const int Rate = 44100;
    static AudioClip hiss, click, clunk, slam, breath, scream, drone, whisper, thud, buzz, tapeOn, step;

    static AudioClip Make(string name, float seconds, System.Func<float, float> f, bool loop = false)
    {
        int n = Mathf.CeilToInt(seconds * Rate);
        var d = new float[n];
        for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f);
        if (loop)   // crossfade ปลายให้วนเนียน
        {
            int x = Mathf.Min(n / 8, Rate / 4);
            for (int i = 0; i < x; i++) { float k = i / (float)x; d[i] = d[i] * k + d[n - x + i] * (1f - k); }
            System.Array.Resize(ref d, n - x);
            n -= x;
        }
        var c = AudioClip.Create(name, n, 1, Rate, false);
        c.SetData(d, 0);
        return c;
    }

    static float N() => Random.value * 2f - 1f;
    static float Env(float t, float a, float d) => t < a ? t / a : Mathf.Exp(-(t - a) / d);

    /// <summary>ซ่าเทป (วนได้)</summary>
    public static AudioClip Hiss => !Enabled ? null : hiss ? hiss : (hiss = MakeHiss());
    static AudioClip MakeHiss() { float lp = 0f; return Make("vhs_hiss", 3f, t => { lp += (N() - lp) * 0.35f; return lp * 0.5f + Mathf.Sin(t * 2f * Mathf.PI * 15734f) * 0.01f; }, true); }

    public static AudioClip Click => !Enabled ? null : click ? click : (click = Make("cam_click", 0.12f, t => N() * Env(t, 0.001f, 0.012f) * 0.8f + Mathf.Sin(t * 2f * Mathf.PI * 1800f) * Env(t, 0.001f, 0.02f) * 0.4f));

    /// <summary>คันโยกเบรกเกอร์ / ไฟตัด</summary>
    public static AudioClip Clunk => !Enabled ? null : clunk ? clunk : (clunk = Make("clunk", 0.6f, t => (Mathf.Sin(t * 2f * Mathf.PI * 70f) * 0.9f + N() * 0.5f) * Env(t, 0.002f, 0.08f)));

    /// <summary>ประตูกระแทกปิด</summary>
    public static AudioClip Slam => !Enabled ? null : slam ? slam : (slam = Make("slam", 1.4f, t =>
        (Mathf.Sin(t * 2f * Mathf.PI * 55f) * 1.0f + Mathf.Sin(t * 2f * Mathf.PI * 120f) * 0.4f + N() * 0.6f) * Env(t, 0.003f, 0.12f)
        + N() * 0.12f * Env(t, 0.05f, 0.5f)));

    /// <summary>ของหล่นตึง</summary>
    public static AudioClip Thud => !Enabled ? null : thud ? thud : (thud = Make("thud", 0.8f, t => (Mathf.Sin(t * 2f * Mathf.PI * 48f) + N() * 0.3f) * Env(t, 0.004f, 0.09f)));

    /// <summary>ลมหายใจผี (วนได้)</summary>
    public static AudioClip Breath => !Enabled ? null : breath ? breath : (breath = MakeBreath());
    static AudioClip MakeBreath()
    {
        float lp = 0f;
        return Make("ghost_breath", 4f, t =>
        {
            lp += (N() - lp) * 0.08f;
            float cycle = Mathf.Repeat(t, 2f) / 2f;                     // หายใจเข้า-ออก 2 วิ
            float amp = Mathf.Pow(Mathf.Sin(cycle * Mathf.PI), 2f);
            float growl = Mathf.Sin(t * 2f * Mathf.PI * (62f + Mathf.Sin(t * 5f) * 6f)) * 0.25f;
            return (lp * 1.6f + growl) * amp * 0.7f;
        }, true);
    }

    /// <summary>กรีดร้องตอนจับได้</summary>
    public static AudioClip Scream => !Enabled ? null : scream ? scream : (scream = Make("scream", 1.6f, t =>
    {
        float f = 520f + Mathf.Sin(t * 37f) * 90f + t * 160f;
        float s = Mathf.Sin(t * 2f * Mathf.PI * f) + Mathf.Sin(t * 2f * Mathf.PI * f * 1.49f) * 0.6f + Mathf.Sin(t * 2f * Mathf.PI * f * 0.51f) * 0.5f;
        return (s * 0.45f + N() * 0.45f) * Env(t, 0.02f, 0.7f);
    }));

    /// <summary>เสียงต่ำหึ่งๆ สร้างบรรยากาศ (วนได้)</summary>
    public static AudioClip Drone => !Enabled ? null : drone ? drone : (drone = Make("drone", 8f, t =>
        Mathf.Sin(t * 2f * Mathf.PI * 41f) * 0.35f + Mathf.Sin(t * 2f * Mathf.PI * 43.5f) * 0.3f + Mathf.Sin(t * 2f * Mathf.PI * 82.5f) * 0.1f * (0.5f + 0.5f * Mathf.Sin(t * 0.8f)), true));

    /// <summary>กระซิบ (นอยส์ผ่านฟิลเตอร์เป็นพยางค์)</summary>
    public static AudioClip Whisper => !Enabled ? null : whisper ? whisper : (whisper = MakeWhisper());
    static AudioClip MakeWhisper()
    {
        float bp = 0f, lp = 0f;
        return Make("whisper", 2.2f, t =>
        {
            float n = N(); lp += (n - lp) * 0.5f; bp += (lp - bp) * 0.2f;
            float syl = Mathf.Max(0f, Mathf.Sin(t * 2f * Mathf.PI * 3.3f)) * Mathf.Max(0f, Mathf.Sin(t * 1.4f));
            return (lp - bp) * syl * 1.4f * Env(t, 0.2f, 1.2f);
        });
    }

    /// <summary>หลอดไฟหึ่ง (วนได้)</summary>
    public static AudioClip Buzz => !Enabled ? null : buzz ? buzz : (buzz = Make("buzz", 2f, t => (Mathf.Sin(t * 2f * Mathf.PI * 100f) * 0.4f + Mathf.Sign(Mathf.Sin(t * 2f * Mathf.PI * 50f)) * 0.08f) * 0.5f, true));

    /// <summary>กดเล่นเทป</summary>
    public static AudioClip TapeOn => !Enabled ? null : tapeOn ? tapeOn : (tapeOn = Make("tape_on", 0.5f, t => N() * Env(t, 0.001f, 0.03f) + Mathf.Sin(t * 2f * Mathf.PI * 90f) * Env(t, 0.05f, 0.15f) * 0.5f));

    /// <summary>ฝีเท้าหนักๆ (ผี)</summary>
    public static AudioClip Step => !Enabled ? null : step ? step : (step = Make("heavy_step", 0.35f, t => (Mathf.Sin(t * 2f * Mathf.PI * 65f) * 0.8f + N() * 0.35f) * Env(t, 0.003f, 0.06f)));
}
