using UnityEngine;

// ตัวเล่นเสียงพากย์กลาง: ซับไตเติล/ข้อความทุกอันที่ขึ้นจอ (ผ่าน GameHUD) จะมาถามที่นี่ว่ามีไฟล์เสียงใน VoiceBank ไหม
// เรียกเองก็ได้: Voice.Say("ภูมิ", "ผมภูมิครับ...", fake: false, at: ตำแหน่งหน้าประตู)
public static class Voice
{
    public const string GroupSubtitle = "พบระหว่างเล่น: ซับไตเติล";
    public const string GroupMessage = "พบระหว่างเล่น: ข้อความกลางจอ";

    static AudioSource src;

    /// <summary>ความยาวเสียงของบรรทัดล่าสุดที่สั่งพูด (0 = ไม่มีไฟล์เสียง)</summary>
    public static float LastLength { get; private set; }
    public static bool IsSpeaking => src && src.isPlaying;

    static AudioSource Src()
    {
        if (src) return src;
        var go = new GameObject("Voice_Player");
        Object.DontDestroyOnLoad(go);
        src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.dopplerLevel = 0f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 1.5f;
        return src;
    }

    /// <summary>แยก "ชื่อ: ข้อความ" -> (ชื่อ, ข้อความ)</summary>
    public static void Split(string full, out string speaker, out string text)
    {
        speaker = ""; text = VoiceBank.Norm(full);
        int i = text.IndexOf(": ");
        int nl = text.IndexOf('\n');
        if (i > 0 && i <= 24 && (nl < 0 || nl > i))
        {
            speaker = text.Substring(0, i).Trim();
            text = text.Substring(i + 2).Trim();
        }
    }

    /// <summary>พูดบรรทัดที่เป็นข้อความเต็ม ("ชื่อ: ข้อความ" หรือข้อความเฉยๆ) คืนค่าความยาวเสียง</summary>
    public static float SayText(string full, string recordGroup = null, bool record = true)
    {
        Split(full, out var sp, out var tx);
        return Say(sp, tx, false, null, recordGroup, record);
    }

    /// <summary>พูด 1 บรรทัด. fake = ใช้เสียงเวอร์ชันของปลอม (ถ้ามี), at = ดังจากจุดนี้ (เช่น หน้าประตู)</summary>
    public static float Say(string speaker, string text, bool fake = false, Vector3? at = null, string recordGroup = null, bool record = true)
    {
        LastLength = 0f;
        var bank = VoiceBank.I;
        if (!bank || string.IsNullOrEmpty(text)) return 0f;
        var line = bank.Find(speaker, text);
#if UNITY_EDITOR
        if (line == null && record && Application.isPlaying && !string.IsNullOrEmpty(recordGroup))
            line = bank.Record(recordGroup, speaker, text);
#endif
        if (line == null || !bank.playVoices) return 0f;
        var clip = fake && line.fakeClip ? line.fakeClip : line.clip;
        if (!clip) return 0f;

        var s = Src();
        if (at.HasValue)
        {
            s.transform.position = at.Value;
            s.spatialBlend = bank.doorSpatial;
            s.maxDistance = bank.doorMaxDistance;
        }
        else s.spatialBlend = 0f;
        s.pitch = line.pitch;
        float vol = bank.masterVolume * line.volume;
        if (bank.stopPrevious || !s.isPlaying)
        {
            s.Stop();
            s.clip = clip; s.volume = vol;
            if (line.delay > 0f) s.PlayDelayed(line.delay); else s.Play();
        }
        else s.PlayOneShot(clip, vol);

        LastLength = clip.length / Mathf.Max(0.05f, line.pitch) + line.delay;
        return LastLength;
    }

    public static void Stop() { if (src) src.Stop(); }
}
