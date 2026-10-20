using System.Collections.Generic;
using UnityEngine;

// คลังเสียงพากย์ "ทุกบทพูด" ในเกม — อยู่ที่ Resources/VoiceBank.asset (เมนู ThanYo/Blurred Room/8. Voice Bank)
//
// วิธีใช้:
//   1) กดปุ่ม "รวบรวมบทพูดทั้งหมด" ใน Inspector -> รายการบทพูดทุกบรรทัดจะขึ้นมา แยกกลุ่มตามตัวละคร/ฉาก
//   2) ลากไฟล์เสียงใส่ช่อง "เสียง" ของบรรทัดนั้น (หรือลากหลายไฟล์วางบนหัวกลุ่ม = ใส่ให้ตามลำดับชื่อไฟล์)
//   3) จบ — ตอนเล่น ซับ/ข้อความไหนตรงกับบรรทัดในนี้ จะเล่นเสียงให้เอง และซับจะค้างจนพูดจบ
//
// ระหว่างกด Play ใน Editor บรรทัดที่ขึ้นจอแต่ยังไม่มีในคลัง จะถูกเพิ่มเข้ามาเอง (กลุ่ม "พบระหว่างเล่น")
[CreateAssetMenu(menuName = "ThanYo/Voice Bank", fileName = "VoiceBank")]
public class VoiceBank : ScriptableObject
{
    [System.Serializable]
    public class Line
    {
        [Tooltip("รหัสบรรทัด — ตั้งชื่อไฟล์เสียงตามนี้ แล้วกด 'จับคู่ไฟล์เสียงอัตโนมัติ' ได้")]
        public string id = "";
        [Tooltip("กลุ่ม (ใช้จัดหมวดใน Inspector)")]
        public string group = "";
        [Tooltip("คนพูด (ต้องตรงกับชื่อที่ขึ้นหน้าซับ เช่น 'ภูมิ' ใน 'ภูมิ: ...')")]
        public string speaker = "";
        [Tooltip("ข้อความ (ต้องตรงกับที่ขึ้นจอ)")]
        [TextArea(1, 5)] public string text = "";
        [Tooltip("ไฟล์เสียงพากย์")]
        public AudioClip clip;
        [Tooltip("เสียงตอน 'ของปลอม' พูดประโยคเดียวกัน (ว่าง = ใช้เสียงปกติ)")]
        public AudioClip fakeClip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 1.5f)] public float pitch = 1f;
        [Tooltip("รอก่อนเริ่มพูด (วินาที)")]
        [Min(0f)] public float delay = 0f;
        [Tooltip("โน้ตถึงคนพากย์ (อารมณ์ น้ำเสียง)")]
        [TextArea(1, 3)] public string note = "";
        [Tooltip("หาไม่เจอในเกมแล้วตอนรวบรวมล่าสุด (บทถูกแก้/ลบ) — เก็บไว้เผื่อไฟล์เสียงยังใช้ได้")]
        public bool missing;
    }

    [Header("ตั้งค่าทั่วไป")]
    [Tooltip("ปิด = ไม่เล่นเสียงพากย์เลย (ซับยังขึ้นปกติ)")]
    public bool playVoices = true;
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Tooltip("บรรทัดใหม่ตัดเสียงบรรทัดเก่า")]
    public bool stopPrevious = true;
    [Tooltip("ซับไตเติลค้างจนเสียงพูดจบ (ถ้าเสียงยาวกว่าเวลาซับ)")]
    public bool extendSubtitle = true;
    [Tooltip("ข้อความกลางจอ (ความคิด/บรรยาย) ก็เล่นเสียงได้ถ้าใส่ไฟล์ไว้")]
    public bool voiceMessages = true;
    [Tooltip("เสียงคนพูดหน้าประตู: 0 = ดังในหัว, 1 = ดังจากหน้าประตูเต็มที่")]
    [Range(0f, 1f)] public float doorSpatial = 0.75f;
    [Tooltip("ระยะได้ยินเสียงหน้าประตู (เมตร)")]
    [Min(2f)] public float doorMaxDistance = 18f;

    [Header("เก็บบทอัตโนมัติ (เฉพาะใน Editor)")]
    [Tooltip("กด Play แล้วเจอซับที่ยังไม่มีในคลัง -> เพิ่มเข้ามาเอง")]
    public bool recordNewSubtitles = true;
    [Tooltip("ข้อความกลางจอที่ยังไม่มีในคลัง -> เพิ่มเข้ามาเองด้วย (จะมีข้อความระบบปนมา)")]
    public bool recordNewMessages = false;
    [Tooltip("โฟลเดอร์ไฟล์เสียงสำหรับปุ่ม 'จับคู่ไฟล์เสียงอัตโนมัติ'")]
    public string autoMatchFolder = "Assets/ThanYo/Sounds/Voice";

    [Header("บทพูดทั้งหมด")]
    public List<Line> lines = new List<Line>();

    // ------------------------------------------------------------------
    static VoiceBank inst;
    public static VoiceBank I
    {
        get
        {
            if (!inst) inst = Resources.Load<VoiceBank>("VoiceBank");
            return inst;
        }
    }

    Dictionary<string, Line> byKey, byText;
    int builtCount = -1;

    public static string Norm(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("\r", "").Trim();
    static string Key(string speaker, string text) => Norm(speaker) + "|" + Norm(text);

    void OnValidate() { builtCount = -1; }
    public void MarkDirtyMap() { builtCount = -1; }

    void Build()
    {
        byKey = new Dictionary<string, Line>();
        byText = new Dictionary<string, Line>();
        foreach (var l in lines)
        {
            if (l == null || string.IsNullOrEmpty(l.text)) continue;
            var k = Key(l.speaker, l.text);
            if (!byKey.ContainsKey(k)) byKey[k] = l;
            var t = Norm(l.text);
            if (!byText.ContainsKey(t) || (!byText[t].clip && l.clip)) byText[t] = l;
        }
        builtCount = lines.Count;
    }

    /// <summary>หาบรรทัด: ตรงทั้งคนพูด+ข้อความก่อน ไม่เจอค่อยหาจากข้อความอย่างเดียว</summary>
    public Line Find(string speaker, string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        if (builtCount != lines.Count || byKey == null) Build();
        if (byKey.TryGetValue(Key(speaker, text), out var l)) return l;
        if (byText.TryGetValue(Norm(text), out l)) return l;
        return null;
    }

#if UNITY_EDITOR
    /// <summary>เพิ่มบรรทัดใหม่ (ใช้ตอน Play ใน Editor)</summary>
    public Line Record(string group, string speaker, string text)
    {
        var l = new Line { id = "auto_" + (lines.Count + 1).ToString("000"), group = group, speaker = Norm(speaker), text = Norm(text) };
        lines.Add(l);
        builtCount = -1;
        UnityEditor.EditorUtility.SetDirty(this);
        return l;
    }
#endif
}
