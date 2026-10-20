using System.Collections;
using System.Collections.Generic;
using BlurredRoom;
using UnityEngine;
using UnityEngine.Events;

// ช่วง "มีคนมาเคาะ" ของเดโม: ผู้เล่นอยู่ในห้องปลอดภัย ปิดประตู แล้วมีคนมาเคาะทีละราย (ตั้งใน Inspector)
// ส่องตาแมว (ค้าง Q) = เห็นภาพจริงหน้าประตู -> ตัดสินใจเปิด/ไม่เปิด เทียบกับกฎในเทป
// เปิดให้ของปลอม = มันพุ่งเข้าใส่ -> ตาย -> เริ่มช่วงนี้ใหม่
public class NightVisits : MonoBehaviour
{
    public static NightVisits I { get; private set; }

    [System.Serializable]
    public class Visit
    {
        [Tooltip("ชื่อเรียกในทีมพัฒนา (ไม่ขึ้นในเกม)")]
        public string label = "visit";
        [Tooltip("id ผู้เช่าที่มา/ที่ถูกปลอมตัว (phum, ton, sompong, boonmee, mew, fon)")]
        public string tenantId = "boonmee";
        [Tooltip("ของปลอม")]
        public bool isFake;
        [Tooltip("ส่องตาแมวแล้วไม่เห็นใคร (มีแต่เสียงเคาะ)")]
        public bool showNobody;
        public KnockStyle knock = KnockStyle.Three;
        public bool loud;
        [Tooltip("รอก่อนเคาะ (วินาที)")]
        [Min(0f)] public float delayBefore = 8f;
        [Tooltip("พูดหลังเคาะครั้งแรก (ว่าง = เงียบ)")]
        [TextArea] public string line = "";
        [Tooltip("ข้อความหลังเปิดให้คนจริง")]
        public string openedRealText = "";
        [Tooltip("ข้อความหลังไม่เปิดให้คนจริง")]
        public string ignoredRealText = "";
        [Tooltip("ข้อความหลังไม่เปิดให้ของปลอม")]
        public string ignoredFakeText = "";
        [Tooltip("ข้อความตอนตายเพราะเปิดให้ของปลอม")]
        public string deathText = "นั่นไม่ใช่คน...";

        [Header("แบบสคริปต์ (เล่นเสียงตามลำดับ ไม่มีให้ตัดสินใจ)")]
        [Tooltip("ติ๊ก = ใช้ลำดับ beats ข้างล่างแทนการเคาะตามกฎ")]
        public bool scripted;
        [Tooltip("ล็อกประตู เปิดไม่ได้ตลอดช่วงนี้")]
        public bool lockDoor = true;
        [Tooltip("ให้ตัวละครยืนหน้าประตู (เห็นผ่านตาแมว)")]
        public bool standAtDoor = true;
        public List<Beat> beats = new List<Beat>();
        [Tooltip("ข้อความหลังจบลำดับ")]
        public string afterScriptText = "";

        [Header("ถ้าผู้เล่นเปิดประตู (แบบสคริปต์ที่ไม่ล็อก / หรือของปลอมที่ใส่ภาพจัมป์สแกร์)")]
        [Tooltip("เสียงที่ดังพร้อมจัมป์สแกร์")]
        public AudioClip openedVoice;
        [Range(0f, 1f)] public float openedVoiceVolume = 1f;
        [Tooltip("ภาพจัมป์สแกร์เต็มจอ (ว่าง = ให้ตัวละครพุ่งเข้าหน้ากล้องแทน)")]
        public Sprite jumpscareImage;
        [Tooltip("ขยายภาพเต็มจอ (ภาพจะโดนตัดบน-ล่าง) / ไม่ติ๊ก = เห็นทั้งภาพ ขอบข้างดำ")]
        public bool jumpscareFillScreen;
        [Tooltip("จัมป์สแกร์กี่วินาที ก่อนตัดดำ")]
        [Min(0.2f)] public float jumpscareSeconds = 1.1f;
        [Tooltip("ไปฉากจบพิเศษ (ไม่ติ๊ก = ตายแล้วเริ่มใหม่ที่ checkpoint)")]
        public bool openedEndsGame = true;
        public string openedEndingTitle = "";
        [TextArea(3, 8)] public string openedEndingBody = "";
    }

    public enum BeatKnock { None, Normal, SoftSlow, Hard }

    [System.Serializable]
    public class Beat
    {
        public string label = "beat";
        [Tooltip("รอกี่วินาทีก่อนเริ่มท่อนนี้")]
        [Min(0f)] public float delay = 0f;
        [Tooltip("นับเวลารอหลังเสียงท่อนก่อนหน้าเล่นจบ (ไม่ติ๊ก = นับจากตอนเริ่มท่อนก่อนหน้า)")]
        public bool afterPreviousEnds = true;
        [Tooltip("เสียงพูด/เสียงประกอบของท่อนนี้ (ดังจากหน้าประตู)")]
        public AudioClip voice;
        [Range(0f, 1f)] public float voiceVolume = 1f;
        [Tooltip("ซับไตเติล (ว่าง = ไม่ขึ้น)")]
        public string subtitle = "";
        [Header("เคาะประตู")]
        public BeatKnock knock = BeatKnock.None;
        [Range(0, 20)] public int knockCount = 3;
        [Tooltip("ระยะห่างแต่ละครั้ง (วินาที)")]
        [Range(0.05f, 3f)] public float knockInterval = 0.55f;
        [Tooltip("เริ่มเคาะหลังเริ่มเสียงกี่วินาที")]
        [Min(0f)] public float knockOffset = 0f;
        [Tooltip("เสียงเคาะเพิ่ม (เช่น ไฟล์เคาะรัว) เล่นพร้อมกัน")]
        public AudioClip extraKnockClip;
    }

    [Header("ห้องปลอดภัย")]
    public Door door;
    [Tooltip("กล่องห้องปลอดภัย (พิกัดโลก) ผู้เล่นต้องอยู่ข้างในและปิดประตูก่อนเริ่ม")]
    public Bounds safeArea = new Bounds(new Vector3(5.2f, 1.3f, 7.9f), new Vector3(5.8f, 2.6f, 3.75f));

    [Header("ตัวละคร")]
    public FakeVisitor fake;
    [Tooltip("ผีตัวไล่ (ถ้าใส่ เปิดผิดแล้วผีตัวนี้จะพุ่งเข้ามาแทนร่างกล่อง)")]
    public GhostChaser ghost;

    [Header("ขั้นในเดโม")]
    [Tooltip("ขั้นที่ 'เข้าห้องปลอดภัยแล้วปิดประตู'")]
    public string enterSafeStep = "safe_room";
    [Tooltip("ขั้นที่ 'มีคนมาเคาะ' (จบเมื่อครบทุกราย)")]
    public string visitsStep = "visits";

    [Header("รายการคนมาเคาะ (เรียงตามลำดับ)")]
    public List<Visit> visits = new List<Visit>();
    [Tooltip("เวลาตัดสินใจหลังเคาะครบ (ไม่เปิด = ปฏิเสธ)")]
    [Min(2f)] public float answerWindow = 12f;
    [Tooltip("ตอนส่องตาแมว ซ่อนการ์ดข้อมูลแบบเก่า เห็นแค่ภาพจริง")]
    public bool realPeepholeOnly = true;

    public enum RevealMode { Unseen, ShowCreature, Ghost }

    [Header("เมื่อเปิดให้ของปลอม")]
    [Tooltip("Unseen = เปิดประตูแล้วไม่มีใคร... ไฟฉายดับ หายใจรดหน้า แล้วจอดับ (ไม่เห็นตัว)\nShowCreature = เห็นร่างจริงพุ่งเข้ามา\nGhost = ใช้ผีตัวไล่ (ต้องใส่ช่อง ghost)")]
    public RevealMode revealMode = RevealMode.Unseen;
    [Min(0.2f)] public float revealScareSeconds = 1.2f;
    [Tooltip("ช่วงเงียบหลังเปิดประตูก่อนเกิดเรื่อง (Unseen)")]
    [Min(0f)] public float unseenSilence = 1.4f;
    public AudioClip revealScream;
    [Tooltip("เสียงหายใจรดหน้า (Unseen)")]
    public AudioClip closeBreath;

    public UnityEvent onAllVisitsDone = new UnityEvent();

    [Header("สถานะ")]
    [SerializeField] int current = -1;

    readonly Dictionary<string, TenantNpc> tenants = new Dictionary<string, TenantNpc>();
    Coroutine routine;
    bool answered;
    DoorAnswer lastAnswer;

    public bool RealPeephole => realPeepholeOnly && routine != null;

    void Awake()
    {
        I = this;
        NightDemo.Respawned += OnRespawn;
    }

    void OnDestroy()
    {
        if (I == this) I = null;
        NightDemo.Respawned -= OnRespawn;
    }

    void Start()
    {
        foreach (var t in FindObjectsByType<TenantNpc>(FindObjectsInactive.Include, FindObjectsSortMode.None)) tenants[t.tenantId] = t;
    }

    void Update()
    {
        // ขั้น "เข้าห้องปลอดภัยแล้วปิดประตู"
        if (NightDemo.IsCurrent(enterSafeStep) && InSafeRoom()) NightDemo.Complete(enterSafeStep);
    }

    public bool InSafeRoom()
    {
        var p = DormFX.Player;
        return p && door && safeArea.Contains(p.position + Vector3.up * 0.2f) && !door.IsOpen;
    }

    /// <summary>เริ่มช่วงเคาะประตู (ต่อจาก onBegin ของขั้น visits)</summary>
    [ContextMenu("เริ่ม (ทดสอบ)")]
    public void Begin()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run());
    }

    void OnRespawn()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        if (door) { door.Answered -= OnAnswered; door.SetLocked(false); if (door.IsOpen) door.Close(); }
        if (scriptVoice) Destroy(scriptVoice.gameObject);
        if (fake) fake.Hide();
        foreach (var t in tenants.Values) if (t) t.LeaveDoor();
        CameraDynamics.ClearFocus();
    }

    IEnumerator Run()
    {
        for (current = 0; current < visits.Count; current++)
        {
            var v = visits[current];
            yield return new WaitForSeconds(v.delayBefore);
            while (door.IsOpen || DormFX.UIBusy) yield return null;
            yield return RunVisit(v);
            if (GameState.Ended || (NightDemo.I && NightDemo.I.IsDying)) yield break;
        }
        routine = null;
        onAllVisitsDone?.Invoke();
        NightDemo.Complete(visitsStep);
    }

    IEnumerator RunVisit(Visit v)
    {
        if (v.scripted) { yield return RunScripted(v); yield break; }
        var info = DormData.Tenant(v.tenantId);
        tenants.TryGetValue(v.tenantId, out var tn);
        NpcBase npc;
        if (!v.isFake && tn)
        {
            tn.ShowAtDoor(door);
            npc = tn;
        }
        else
        {
            fake.Disguise(info, tn ? tn.model : null);
            fake.ShowAtDoor(door);
            if (v.showNobody) fake.Hide();
            npc = fake;
        }

        answered = false;
        door.Answered += OnAnswered;
        yield return KnockRules.Perform(door, npc, v.knock, v.loud, answerWindow, () =>
        {
            if (!string.IsNullOrEmpty(v.line))
            {
                string who = info != null ? info.name : "";
                float len = Voice.Say(who, v.line, v.isFake, door.VisitorStandPoint.position + Vector3.up * 1.5f);
                DormFX.Sub(v.line, Mathf.Max(5f, len + 0.4f), false);
                if (!v.isFake && tn) tn.PlayTalk(); else if (!v.showNobody) fake.PlayTalk();
            }
        });
        while (!answered && door.HasPendingVisitor) yield return null;
        door.Answered -= OnAnswered;

        bool opened = answered && lastAnswer == DoorAnswer.Opened;
        if (!v.isFake)
        {
            if (opened)
            {
                if (!string.IsNullOrEmpty(v.openedRealText)) DormFX.Sub(v.openedRealText, 4f);
                yield return new WaitForSeconds(Mathf.Max(3.5f, Voice.LastLength));
            }
            else if (!string.IsNullOrEmpty(v.ignoredRealText)) DormFX.Msg(v.ignoredRealText, 4f);
            if (tn) tn.LeaveDoor();
        }
        else
        {
            if (opened)
            {
                yield return Reveal(v);
                yield break;
            }
            fake.Hide();
            if (!string.IsNullOrEmpty(v.ignoredFakeText)) DormFX.Msg(v.ignoredFakeText, 4f);
        }
    }

    // ---------- แบบสคริปต์ ----------
    IEnumerator RunScripted(Visit v)
    {
        tenants.TryGetValue(v.tenantId, out var tn);
        if (v.standAtDoor)
        {
            if (tn) tn.ShowAtDoor(door);
            else { fake.Disguise(DormData.Tenant(v.tenantId), null); fake.ShowAtDoor(door); }
        }
        if (v.lockDoor) { if (door.IsOpen) door.Close(); door.SetLocked(true); }

        Vector3 at = door.VisitorStandPoint.position + Vector3.up * 1.5f;
        var src = new GameObject("Visit_Voice").AddComponent<AudioSource>();
        src.transform.position = at;
        src.spatialBlend = 0.85f; src.rolloffMode = AudioRolloffMode.Linear; src.minDistance = 1.5f; src.maxDistance = 15f; src.dopplerLevel = 0f;
        scriptVoice = src;

        bool canOpen = !v.lockDoor;
        float prevStart = Time.time, prevEnd = Time.time;
        foreach (var b in v.beats)
        {
            float startAt = (b.afterPreviousEnds ? prevEnd : prevStart) + b.delay;
            while (Time.time < startAt)
            {
                if (canOpen && door.IsOpen) { yield return ScriptedOpened(v, tn, src); yield break; }
                yield return null;
            }
            prevStart = Time.time;
            float len = 0f;
            if (b.voice)
            {
                src.PlayOneShot(b.voice, b.voiceVolume);
                len = b.voice.length;
                if (tn && v.standAtDoor) tn.PlayTalk();
            }
            if (!string.IsNullOrEmpty(b.subtitle)) DormFX.Sub(b.subtitle, Mathf.Max(2.5f, len), !b.voice);   // มีเสียงในท่อนแล้ว = ไม่ต้องเล่นซ้ำจาก VoiceBank
            if (b.knock != BeatKnock.None) knockRoutines.Add(StartCoroutine(Knocks(b)));
            float knockLen = b.knock != BeatKnock.None ? b.knockOffset + b.knockCount * b.knockInterval : 0f;
            prevEnd = prevStart + Mathf.Max(len, knockLen);
        }
        while (Time.time < prevEnd)
        {
            if (canOpen && door.IsOpen) { yield return ScriptedOpened(v, tn, src); yield break; }
            yield return null;
        }

        Destroy(src.gameObject, 0.5f);
        scriptVoice = null;
        if (v.lockDoor) door.SetLocked(false);
        if (tn) tn.LeaveDoor(); else fake.Hide();
        if (!string.IsNullOrEmpty(v.afterScriptText)) DormFX.Msg(v.afterScriptText, 4f);
    }

    AudioSource scriptVoice;

    /// <summary>เปิดประตูให้ (เช่น ฝน): เสียง + พุ่งเข้าหน้ากล้องพร้อมกัน -> ตัดดำ -> ฉากจบพิเศษ</summary>
    // เปิดประตูให้ของปลอม (รายที่ใส่ภาพจัมป์สแกร์ไว้): ภาพเด้งเต็มจอ -> จอดำ -> ฉากจบ หรือ ตายแล้วเริ่มใหม่
    IEnumerator ImageReveal(Visit v)
    {
        var input = DormFX.Input;
        if (input) input.LockMovement(this);
        fake.Hide();
        if (v.openedVoice) DormFX.Play2D(v.openedVoice, v.openedVoiceVolume);
        if (GameHUD.Instance) StartCoroutine(GameHUD.Instance.FadeTo(1f, 0.05f));   // ข้างหลังภาพเป็นดำ
        yield return JumpscareImage.Show(v.jumpscareImage, v.jumpscareSeconds, v.jumpscareFillScreen);
        CameraDynamics.ClearFocus();
        if (v.openedEndsGame)
        {
            routine = null;
            GameState.Ended = true;
            yield return new WaitForSeconds(1.5f);
            if (GameHUD.Instance) GameHUD.Instance.ShowEnding(v.openedEndingTitle, v.openedEndingBody);
        }
        else
        {
            if (input) input.UnlockMovement(this);
            NightDemo.Kill(v.deathText);
        }
    }

    IEnumerator ScriptedOpened(Visit v, TenantNpc tn, AudioSource src)
    {
        StopCoroutineKnocks();
        src.Stop();
        var input = DormFX.Input;
        if (input) input.LockMovement(this);
        if (v.openedVoice) DormFX.Play2D(v.openedVoice, v.openedVoiceVolume);
        CameraDynamics.Shake(1f);

        if (v.jumpscareImage)
        {
            if (tn) tn.LeaveDoor(); else fake.Hide();
            if (GameHUD.Instance) StartCoroutine(GameHUD.Instance.FadeTo(1f, 0.05f));   // ข้างหลังภาพเป็นดำ
            yield return JumpscareImage.Show(v.jumpscareImage, v.jumpscareSeconds, v.jumpscareFillScreen);
        }
        else
        {
        Transform body = tn ? tn.transform : fake.transform;
        Transform head = null;
        foreach (var t in body.GetComponentsInChildren<Transform>()) if (t.name == "Head") { head = t; break; }
        var cam = DormFX.PlayerCamera;
        for (float t = 0f; t < v.jumpscareSeconds; t += Time.deltaTime)
        {
            if (cam)
            {
                Vector3 front = cam.transform.position + cam.transform.forward * 0.45f;
                Vector3 target = new Vector3(front.x, body.position.y, front.z);
                body.position = Vector3.Lerp(body.position, target, Time.deltaTime * 14f);
                Vector3 look = cam.transform.position - body.position; look.y = 0f;
                if (look.sqrMagnitude > 0.001f) body.rotation = Quaternion.LookRotation(look);
                CameraDynamics.SetFocus(head ? head.position : body.position + Vector3.up * 1.55f, 1f, -22f);
            }
            yield return null;
        }
        }
        if (GameHUD.Instance) yield return GameHUD.Instance.FadeTo(1f, 0.08f);
        CameraDynamics.ClearFocus();
        Destroy(src.gameObject);
        scriptVoice = null;
        if (tn) tn.LeaveDoor(); else fake.Hide();

        if (v.openedEndsGame)
        {
            routine = null;
            GameState.Ended = true;
            yield return new WaitForSeconds(1.5f);
            if (GameHUD.Instance) GameHUD.Instance.ShowEnding(v.openedEndingTitle, v.openedEndingBody);
        }
        else
        {
            if (input) input.UnlockMovement(this);
            NightDemo.Kill(v.deathText);
        }
    }

    readonly List<Coroutine> knockRoutines = new List<Coroutine>();
    void StopCoroutineKnocks() { foreach (var c in knockRoutines) if (c != null) StopCoroutine(c); knockRoutines.Clear(); }

    IEnumerator Knocks(Beat b)
    {
        if (b.knockOffset > 0f) yield return new WaitForSeconds(b.knockOffset);
        Vector3 pos = door.transform.position + Vector3.up * 1.2f;
        if (b.extraKnockClip) DormFX.Play(b.extraKnockClip, pos, b.knock == BeatKnock.Hard ? 1f : 0.6f);
        for (int i = 0; i < b.knockCount; i++)
        {
            switch (b.knock)
            {
                case BeatKnock.SoftSlow: DormFX.Play(DormFX.KnockClip, pos, 0.3f); break;
                case BeatKnock.Normal: DormFX.Play(DormFX.KnockClip, pos, 0.75f); break;
                case BeatKnock.Hard:
                    DormFX.Play(DormFX.KnockHardClip ? DormFX.KnockHardClip : DormFX.KnockClip, pos, 1f);
                    CameraDynamics.Shake(0.18f);
                    break;
            }
            yield return new WaitForSeconds(b.knockInterval);
        }
    }

    IEnumerator Reveal(Visit v)
    {
        if (v.jumpscareImage) { yield return ImageReveal(v); yield break; }   // รายนี้มีภาพจัมป์สแกร์ของตัวเอง
        if (revealMode == RevealMode.Unseen) { yield return Unseen(v); yield break; }
        if (revealMode == RevealMode.Ghost && ghost)
        {
            Vector3 at = fake.transform.position;
            fake.Hide();
            ghost.CatchNow(at, v.deathText);
            yield break;
        }
        fake.Reveal();
        var input = DormFX.Input;
        if (input) input.LockMovement(this);
        DormFX.Play2D(revealScream ? revealScream : NightAudio.Scream, 1f);
        CameraDynamics.Shake(1f);
        for (float t = 0f; t < revealScareSeconds; t += Time.deltaTime)
        {
            CameraDynamics.SetFocus(fake.transform.position + Vector3.up * 1.7f, 1f, -20f);
            var p = DormFX.Player;
            if (p)
            {
                Vector3 front = p.position + p.forward * 0.6f;
                fake.transform.position = Vector3.Lerp(fake.transform.position, new Vector3(front.x, fake.transform.position.y, front.z), Time.deltaTime * 8f);
            }
            yield return null;
        }
        if (input) input.UnlockMovement(this);
        NightDemo.Kill(v.deathText);
    }

    IEnumerator Unseen(Visit v)
    {
        fake.Hide();   // เปิดประตู... ไม่มีใครอยู่ตรงนั้นแล้ว
        var input = DormFX.Input;
        if (input) input.LockMovement(this);
        CameraDynamics.SetFocus(door.VisitorStandPoint.position + Vector3.up * 1.5f, 0.6f, -6f);
        yield return new WaitForSeconds(unseenSilence);
        if (Flashlight.I) Flashlight.I.Flicker(1.5f);
        if (closeBreath) DormFX.Play2D(closeBreath, 0.9f);
        yield return new WaitForSeconds(0.9f);
        DormFX.Play2D(revealScream ? revealScream : NightAudio.Scream, 1f);
        if (SoundBank.I) DormFX.Play2D(SoundBank.I.jumpscare, SoundBank.I.jumpscareVolume);
        CameraDynamics.Shake(1f);
        if (GameHUD.Instance) yield return GameHUD.Instance.FadeTo(1f, 0.12f);   // ตัดดำทันที ไม่เห็นอะไรเลย
        CameraDynamics.ClearFocus();
        if (input) input.UnlockMovement(this);
        NightDemo.Kill(v.deathText);
    }

    void OnAnswered(NpcBase npc, DoorAnswer a) { lastAnswer = a; answered = true; }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.5f);
        Gizmos.DrawWireCube(safeArea.center, safeArea.size);
    }
}
