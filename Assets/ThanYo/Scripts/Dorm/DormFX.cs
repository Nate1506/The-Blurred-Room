using System.Collections.Generic;
using BlurredRoom;
using TMPro;
using UnityEngine;

// เครื่องมือกลางของระบบหอพัก: ฟอนต์ไทย, โหลดเบาะแส/รูป, ข้อความบนจอ, สร้างตัวละครจากกล่อง (สไตล์ PS1),
// เสียงที่สร้างจากโค้ด (เคาะประตู / สติง / ชัตเตอร์) — ไม่ต้องมีไฟล์เสียงก็เล่นได้
public static class DormFX
{
    // ---------------- assets ----------------
    static TMP_FontAsset font;
    public static TMP_FontAsset ThaiFont => font ? font : (font = Resources.Load<TMP_FontAsset>("ThaiFont"));

    /// <summary>เปิด mark/mkmk ให้สระบน + วรรณยุกต์ภาษาไทยซ้อนกันถูก (ไม่งั้น "เริ่ม" จะเหลือ "เริม")</summary>
    public static void ThaiMarks(TMP_Text t)
    {
        if (!t) return;
        if (!t.fontFeatures.Contains(UnityEngine.TextCore.OTL_FeatureTag.mark)) t.fontFeatures.Add(UnityEngine.TextCore.OTL_FeatureTag.mark);
        if (!t.fontFeatures.Contains(UnityEngine.TextCore.OTL_FeatureTag.mkmk)) t.fontFeatures.Add(UnityEngine.TextCore.OTL_FeatureTag.mkmk);
    }

    static Dictionary<string, ClueData> clues;
    public static ClueData Clue(string id)
    {
        if (clues == null)
        {
            clues = new Dictionary<string, ClueData>();
            foreach (var c in Resources.LoadAll<ClueData>("DormClues")) clues[c.Id] = c;
        }
        clues.TryGetValue(id, out var clue);
        if (!clue) Debug.LogWarning("[Dorm] Clue not found: " + id + " (run ThanYo > Blurred Room > 1. Import assets)");
        return clue;
    }

    public static Sprite Portrait(int n) => Resources.Load<Sprite>("Dorm/tenant-" + n);
    public static Sprite Creature => Resources.Load<Sprite>("Dorm/creature");
    public static Sprite Staff => Resources.Load<Sprite>("Dorm/staff");

    // ---------------- game shortcuts ----------------
    public static DayPhase Phase => TimeOfDayManager.Instance ? TimeOfDayManager.Instance.Phase : GameState.Phase;
    public static int ActionsLeft => Mathf.Max(0, DormData.ActionsPerDay - GameState.ActionsUsed);

    /// <summary>มีหน้าต่างอ่านอะไรอยู่ (เบาะแส / เอกสาร / แฟลช / จัมป์สแกร์) -> ห้ามกด E ใส่ของอื่น</summary>
    public static bool UIBusy => (GameHUD.Instance && GameHUD.Instance.ReadingClue) || DormOverlay.Busy;

    static PlayerInputSource input;
    public static PlayerInputSource Input => input ? input : (input = Object.FindFirstObjectByType<PlayerInputSource>());
    static PlayerSanity sanity;
    public static PlayerSanity Sanity => sanity ? sanity : (sanity = Object.FindFirstObjectByType<PlayerSanity>());
    static PlayerInventory inventory;
    public static PlayerInventory Inventory => inventory ? inventory : (inventory = Object.FindFirstObjectByType<PlayerInventory>());
    static Transform player;
    public static Transform Player
    {
        get
        {
            if (!player) { var pm = Object.FindFirstObjectByType<PlayerMovement>(); if (pm) player = pm.transform; }
            return player;
        }
    }
    public static Camera PlayerCamera
    {
        get { var s = Object.FindFirstObjectByType<PlayerSight>(); return s ? s.ViewCamera : Camera.main; }
    }

    // voice = เล่นเสียงพากย์จาก VoiceBank อัตโนมัติ (ดูความยาวเสียงได้ที่ Voice.LastLength)
    public static void Msg(string text, float seconds = 4f, bool voice = true) { if (GameHUD.Instance) GameHUD.Instance.ShowMessage(text, seconds, voice); }
    public static void Sub(string text, float seconds = 4f, bool voice = true) { if (GameHUD.Instance) GameHUD.Instance.ShowSubtitle(text, seconds, voice); }

    public static bool HasClue(string id) => ClueManager.Instance && ClueManager.Instance.HasClue(id);
    public static void Discover(string id)
    {
        var c = Clue(id);
        if (c && ClueManager.Instance) ClueManager.Instance.Discover(c);
    }

    /// <summary>ตั้งธงทั้งใน GameState (ข้ามซีน) และ ClueManager (ใช้เลือกฉากจบ)</summary>
    public static void SetFlag(string flag)
    {
        GameState.Set(flag);
        if (ClueManager.Instance) ClueManager.Instance.SetFlag(flag);
    }

    public static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (list[i], list[j]) = (list[j], list[i]); }
    }

    // ---------------- PS1 box people ----------------
    static readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();
    static readonly Dictionary<Color, Material> glowMats = new Dictionary<Color, Material>();

    public static Material Mat(Color c)
    {
        if (mats.TryGetValue(c, out var m) && m) return m;
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        m = new Material(sh);
        m.SetColor("_BaseColor", c); m.color = c;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        mats[c] = m;
        return m;
    }

    public static Material Glow(Color c)
    {
        if (glowMats.TryGetValue(c, out var m) && m) return m;
        var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        m = new Material(sh);
        m.SetColor("_BaseColor", c); m.color = c;
        glowMats[c] = m;
        return m;
    }

    public static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat, bool keepCollider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!keepCollider) Object.Destroy(go.GetComponent<Collider>());
        return go;
    }

    /// <summary>ผู้เช่าแบบกล่อง (สไตล์ PS1) สูง ~1.75 m, หันหน้าไป +Z ของ parent. shade &lt; 1 = มืดลง (เงา)</summary>
    public static GameObject BuildPerson(Transform parent, TenantInfo t, float shade = 1f)
    {
        var root = new GameObject("Body_" + t.id).transform;
        root.SetParent(parent, false);
        Color S(Color c) => new Color(c.r * shade, c.g * shade, c.b * shade);
        Material top = Mat(S(t.top)), bottom = Mat(S(t.bottom)), skin = Mat(S(t.skin)), hair = Mat(S(t.hair));

        Material leg = t.shorts ? skin : bottom;
        Box(root, "LegL", new Vector3(-0.09f, 0.43f, 0), new Vector3(0.14f, 0.86f, 0.16f), leg);
        Box(root, "LegR", new Vector3(0.09f, 0.43f, 0), new Vector3(0.14f, 0.86f, 0.16f), leg);
        if (t.shorts) Box(root, "Shorts", new Vector3(0, 0.8f, 0), new Vector3(0.36f, 0.18f, 0.2f), bottom);
        Box(root, "Torso", new Vector3(0, 1.17f, 0), new Vector3(0.42f, 0.6f, 0.24f), top);
        Material arm = t.shortSleeves ? skin : top;
        Box(root, "ArmL", new Vector3(-0.28f, 1.12f, 0), new Vector3(0.11f, 0.62f, 0.13f), arm);
        Box(root, "ArmR", new Vector3(0.28f, 1.12f, 0), new Vector3(0.11f, 0.62f, 0.13f), arm);
        if (t.shortSleeves)
        {
            Box(root, "SleeveL", new Vector3(-0.28f, 1.38f, 0), new Vector3(0.13f, 0.14f, 0.15f), top);
            Box(root, "SleeveR", new Vector3(0.28f, 1.38f, 0), new Vector3(0.13f, 0.14f, 0.15f), top);
        }
        Box(root, "Head", new Vector3(0, 1.62f, 0), new Vector3(0.24f, 0.27f, 0.24f), skin);
        if (!t.bald) Box(root, "Hair", new Vector3(0, 1.77f, -0.02f), new Vector3(0.26f, 0.08f, 0.27f), hair);
        var eye = Mat(S(new Color(0.05f, 0.05f, 0.05f)));
        Box(root, "EyeL", new Vector3(-0.055f, 1.64f, 0.121f), new Vector3(0.04f, 0.025f, 0.01f), eye);
        Box(root, "EyeR", new Vector3(0.055f, 1.64f, 0.121f), new Vector3(0.04f, 0.025f, 0.01f), eye);
        return root.gameObject;
    }

    /// <summary>สร้างโมเดลผู้เช่าจาก prefab (Prefabs/Tenants). shade &lt; 1 = ทาให้มืดเป็นเงา</summary>
    public static GameObject SpawnModel(GameObject prefab, Transform parent, float shade = 1f)
    {
        var go = Object.Instantiate(prefab, parent, false);
        go.name = "Body_" + prefab.name;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        if (shade < 1f)
        {
            var mpb = new MaterialPropertyBlock();
            Color dark = new Color(shade, shade, shade);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", dark); mpb.SetColor("_Color", dark);
                r.SetPropertyBlock(mpb);
            }
        }
        return go;
    }

    /// <summary>เล่นท่าพูด (trigger "Talk" ใน Animator ของโมเดล) ถ้ามี</summary>
    public static void PlayTalk(GameObject body)
    {
        if (!body) return;
        var an = body.GetComponentInChildren<Animator>();
        if (!an || !an.runtimeAnimatorController) return;
        foreach (var p in an.parameters) if (p.name == "Talk") { an.SetTrigger("Talk"); return; }
    }

    /// <summary>สิ่งผิดปกติ: ร่างผอมสูงผิดสัดส่วน (~2.5 m) ตาแดง</summary>
    public static GameObject BuildCreature(Transform parent)
    {
        var root = new GameObject("Body_Creature").transform;
        root.SetParent(parent, false);
        var dark = Mat(new Color(0.07f, 0.045f, 0.035f));
        Box(root, "LegL", new Vector3(-0.07f, 0.65f, 0), new Vector3(0.08f, 1.3f, 0.09f), dark);
        Box(root, "LegR", new Vector3(0.07f, 0.65f, 0), new Vector3(0.08f, 1.3f, 0.09f), dark);
        Box(root, "Torso", new Vector3(0, 1.75f, 0.05f), new Vector3(0.28f, 0.95f, 0.16f), dark);
        Box(root, "ArmL", new Vector3(-0.2f, 1.35f, 0.06f), new Vector3(0.06f, 1.45f, 0.07f), dark);
        Box(root, "ArmR", new Vector3(0.2f, 1.35f, 0.06f), new Vector3(0.06f, 1.45f, 0.07f), dark);
        var head = Box(root, "Head", new Vector3(0, 2.38f, 0.12f), new Vector3(0.19f, 0.34f, 0.2f), dark);
        head.transform.localRotation = Quaternion.Euler(18f, 0, 0);
        var red = Glow(new Color(1f, 0.08f, 0.05f));
        Box(root, "EyeL", new Vector3(-0.045f, 2.44f, 0.235f), new Vector3(0.035f, 0.03f, 0.01f), red);
        Box(root, "EyeR", new Vector3(0.045f, 2.44f, 0.235f), new Vector3(0.035f, 0.03f, 0.01f), red);
        return root.gameObject;
    }

    /// <summary>ยืนหน้าประตู (ฝั่งทางเดิน) หันหน้าเข้าหาประตู</summary>
    public static void PlaceAtDoor(Transform t, Door door, float closer = 0.25f)
    {
        Transform sp = door.VisitorStandPoint;
        Vector3 f = sp.forward; f.y = 0f; f.Normalize();
        t.SetPositionAndRotation(sp.position + f * closer, Quaternion.LookRotation(f));
    }

    // ---------------- procedural audio ----------------
    static AudioClip knock, sting, shutter, tap, foot, heart;
    const int Rate = 44100;

    static AudioClip Make(string name, float seconds, System.Func<float, float> f)
    {
        int n = Mathf.CeilToInt(seconds * Rate);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f);
        var clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float Noise() => Random.value * 2f - 1f;

    // เสียงทั้งหมดมาจาก SoundBank (Resources/SoundBank.asset) — ไม่มีเสียงสังเคราะห์แล้ว
    public static AudioClip KnockClip => SoundBank.I ? SoundBank.I.knock : null;
    public static AudioClip KnockHardClip => SoundBank.I ? SoundBank.I.knockHard : null;
    public static AudioClip TapClip => null;
    public static AudioClip ShutterClip => null;
    public static AudioClip FootClip => null;
    public static AudioClip HeartClip => null;
    public static AudioClip StingClip => null;

    public static void Play(AudioClip clip, Vector3 pos, float volume = 1f)
    {
        if (clip) AudioSource.PlayClipAtPoint(clip, pos, volume);
    }

    public static void Play2D(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (!clip) return;
        var go = new GameObject("OneShot2D_" + clip.name);
        var src = go.AddComponent<AudioSource>();
        src.spatialBlend = 0f; src.volume = volume; src.pitch = pitch; src.clip = clip;
        src.Play();
        Object.Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.2f);
    }

    public static void Knock(Vector3 pos, bool loud)
    {
        var clip = loud && KnockHardClip ? KnockHardClip : KnockClip;
        if (!clip) return;
        var go = new GameObject("Knock");
        go.transform.position = pos + Vector3.up * 1.2f;
        var src = go.AddComponent<AudioSource>();
        float vol = SoundBank.I ? SoundBank.I.knockVolume : 1f;
        src.clip = clip; src.spatialBlend = 0.85f; src.minDistance = 2f; src.maxDistance = 25f;
        src.volume = (loud ? 1f : 0.6f) * vol; src.pitch = Random.Range(0.94f, 1.06f);
        src.Play();
        CameraDynamics.Shake(loud ? 0.22f : 0.07f);
        Object.Destroy(go, clip.length + 0.2f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        clues = null; input = null; sanity = null; inventory = null; player = null;
        mats.Clear(); glowMats.Clear();
    }
}
