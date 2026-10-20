using System.Collections;
using System.Collections.Generic;
using System.Text;
using BlurredRoom;
using UnityEngine;

// ตัวกำกับเรื่อง The Blurred Room (ตามโน้ตใน Obsidian vault)
// 1 วัน = กลางวัน (4 แอ็กชัน: สัมภาษณ์/สำรวจ/ถ่ายรูป) -> เย็น (ต่อเบาะแสบนกระดาน + ทำพิธี) -> กลางคืน (มีคนมาเคาะประตู)
// กลางคืน: ผู้เช่าตัวจริง 2 คน + ตัวปลอม 2 ตัว / คืน
//   เปิดให้ตัวจริง = ปลอดภัย   ไม่เปิดให้ตัวจริง = ผู้เช่าหาย (กลับมาไม่ได้)
//   เปิดให้ตัวปลอม = สติ -20  ไม่เปิดให้ตัวปลอม = ปลอดภัย
// ห้องปลอดภัย: คืน 1 และ 3 = ห้องออฟฟิศ, คืน 2 = ห้องที่พิธีบอก (ต้องรีบวิ่งไปให้ทัน)
// เช้าวันที่ 4: หาย 0-3 = จบปกติ, 4-5 = Bad End, 6 = ฉากจบลับ   |   สติหมด = หลุดเข้ามิติอดีต (SystemHub)
// เก็บทุกอย่างใน GameState จึงข้ามซีน Inside <-> ThanYo ได้
public class StoryDirector : MonoBehaviour
{
    public static StoryDirector Instance { get; private set; }

    [Header("Inside scene only (leave empty in ThanYo)")]
    public Door caretakerDoor;
    public Bounds officeArea = new Bounds(new Vector3(5.2f, 1.3f, 7.9f), new Vector3(5.8f, 2.6f, 3.75f));
    public FakeVisitor fake;
    public AnomalyDirector anomalies;

    [Header("Night pacing")]
    public Vector2 secondsBetweenVisits = new Vector2(10f, 16f);
    [Tooltip("หลังเคาะครบ ผู้เล่นมีเวลาตัดสินใจกี่วินาที (ไม่เปิด = ปฏิเสธ)")]
    public float answerWindow = 15f;
    [Tooltip("คืนที่ 2: ต้องไปถึงห้องที่พิธีบอกภายในกี่วินาที (เกินแล้วโดนจัมป์สแกร์ทุก 20 วิ)")]
    public float night2RushSeconds = 75f;

    public enum FakeMode { BadKnock, MissingLineTrace, WrongSignature }

    public class VisitInfo
    {
        public TenantInfo tenant;
        public bool fake;
        public FakeMode mode;
        public KnockStyle knock;
        public bool loud;
        public string line;
        public List<string> traces = new List<string>();
        public Door door;
    }

    /// <summary>คนที่กำลังเคาะอยู่ตอนนี้ (null = ไม่มี)</summary>
    public VisitInfo Current { get; private set; }

    TimeOfDayManager time;
    Transform player;
    bool inside, busy;
    Coroutine nightRoutine;
    readonly Dictionary<string, TenantNpc> tenants = new Dictionary<string, TenantNpc>();
    readonly Dictionary<string, Door> doorsByName = new Dictionary<string, Door>();
    Door nightDoor;
    Bounds nightArea;
    bool safeReached;
    float rushDeadline;
    int visitsDone, visitsTotal;
    DoorAnswer lastAnswer;
    bool answered;

    DayPhase Phase => DormFX.Phase;
    static int Night => GameState.Night;
    bool InOffice => inside && player && officeArea.Contains(player.position + Vector3.up * 0.2f);

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        RitualCircle.Completed -= OnRitualDone;
        if (EndingManager.Instance) EndingManager.Instance.EndingReached -= OnEnding;
    }

    NightDemo nightDemo;

    void Start()
    {
        nightDemo = FindFirstObjectByType<NightDemo>();
        if (nightDemo && nightDemo.autoStart) nightDemo = null;   // เดโมที่เริ่มเองไม่ต้องรอเรา
        time = TimeOfDayManager.Instance;
        player = DormFX.Player;
        inside = caretakerDoor != null;
        foreach (var t in FindObjectsByType<TenantNpc>(FindObjectsInactive.Include, FindObjectsSortMode.None)) tenants[t.tenantId] = t;
        foreach (var d in FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None)) doorsByName[d.name] = d;
        RitualCircle.Completed += OnRitualDone;
        if (EndingManager.Instance) EndingManager.Instance.EndingReached += OnEnding;

        if (GameState.RealVisitOrder.Count == 0)
        {
            for (int i = 0; i < DormData.Tenants.Length; i++) GameState.RealVisitOrder.Add(i);
            DormFX.Shuffle(GameState.RealVisitOrder);
        }
        StartCoroutine(IntroIfNeeded());
    }

    // =====================================================================
    //  main loop: objectives + phase changes
    // =====================================================================
    void Update()
    {
        if (GameState.Ended || busy) return;
        var list = new List<(string, bool)>();
        string phaseName = Phase == DayPhase.Day ? "กลางวัน" : Phase == DayPhase.Eve ? "ตอนเย็น" : "กลางคืน";
        string header = "วันที่ " + Night + "/3 · " + phaseName + "      ผู้เช่าหาย " + GameState.Missing.Count + "/6";

        if (!inside)
        {
            list.Add(("กลับเข้าไปในตึก", false));
            Hud(header, list);
            return;
        }

        if (Phase == DayPhase.Day)
        {
            int used = GameState.ActionsUsed;
            list.Add(("ใช้แอ็กชันวันนี้ " + used + "/" + DormData.ActionsPerDay + " — สัมภาษณ์ผู้เช่า / สำรวจ / ถ่ายรูป", used >= DormData.ActionsPerDay));
            list.Add(("(ไม่บังคับ) ดูรูปพี่บิ๊กเหนือหิ้งเทียนในล็อบบี้ +สติ", GameState.Has(StaffPhoto.FlagToday)));
            Hud(header, list);
            if (used >= DormData.ActionsPerDay) StartCoroutine(ToEvening());
        }
        else if (Phase == DayPhase.Eve)
        {
            bool board = GameState.Has("board_n" + Night), rit = GameState.Has("ritual_n" + Night);
            var inv = DormFX.Inventory;
            list.Add(("ต่อเบาะแสบนกระดาน (ห้องออฟฟิศ)", board));
            list.Add(("หาธูป (บนเคาน์เตอร์ล็อบบี้)", rit || (inv && inv.Has(DormData.Incense))));
            list.Add(("หาชอล์ก (ห้องซักผ้า ชั้น 1)", rit || (inv && inv.Has(DormData.Chalk))));
            list.Add(("ทำพิธีที่ห้องเก็บของ ชั้น 3 — ค้าง [R] 3 รอบ", rit));
            if (Night != 2)
            {
                bool home = InOffice && !caretakerDoor.IsOpen;
                list.Add(("กลับห้องออฟฟิศ แล้วปิดประตูก่อนมืด", board && rit && home));
                Hud(header, list);
                if (board && rit && home) StartCoroutine(ChangePhase(DayPhase.Night, 1f));
            }
            else Hud(header, list);
        }
        else
        {
            // คืนแบบใหม่ (เดโมคืนแรก): ส่งต่อให้ NightDemo คุมทั้งหมด
            if (nightDemo)
            {
                if (!nightDemo.Started) nightDemo.StartDemo();
                return;
            }
            NightObjectives(list);
            Hud(header, list);
            if (nightRoutine == null) nightRoutine = StartCoroutine(NightRoutine());
        }
    }

    void NightObjectives(List<(string, bool)> list)
    {
        bool office = !nightDoor || nightDoor == caretakerDoor;
        string where = office ? "ห้องออฟฟิศ" : "ห้อง " + GameState.Night2Room;
        list.Add(("อยู่ใน" + where + " แล้วปิดประตู", safeReached));
        list.Add(("มีคนเคาะ: ฟังจังหวะ แล้วค้าง [Q] ส่องตาแมวก่อนตัดสินใจ", visitsTotal > 0 && visitsDone >= visitsTotal));
        if (office) list.Add(("(ไม่บังคับ) ดูกล้องวงจรปิดที่โต๊ะ หาความผิดปกติ", GameState.Has("cctv_seen_n" + Night)));
        list.Add(("รอดจนถึงเช้า (" + visitsDone + "/" + Mathf.Max(visitsTotal, 4) + ")", false));

        if (Night == 2 && !safeReached && rushDeadline > 0f)
            DormOverlay.I.SetAlert("ไปห้อง " + GameState.Night2Room + " เดี๋ยวนี้!  " + Mathf.Max(0, Mathf.CeilToInt(rushDeadline - Time.time)) + " วิ");
    }

    static void Hud(string header, List<(string, bool)> list)
    {
        if (GameHUD.Instance) GameHUD.Instance.SetObjectives(header, list);
    }

    // =====================================================================
    //  phases
    // =====================================================================
    IEnumerator IntroIfNeeded()
    {
        yield return new WaitForSeconds(1.2f);
        while (OpeningCutscene.Playing) yield return null;   // รอคัตซีนเปิดเกมจบก่อน
        if (!inside && OpeningCutscene.ShowedTitle) yield break;   // คัตซีนขึ้นชื่อหอไปแล้ว
        if (nightDemo && Phase == DayPhase.Night) yield break;
        string key = "intro_n" + Night + "_" + Phase;
        if (GameState.Has(key) || GameState.Ended) yield break;
        GameState.Set(key);
        DormFX.Msg(IntroText(), 7f);
    }

    string IntroText()
    {
        if (!inside) return "สุขนิรันดร์ แมนชั่น";
        int n = Night;
        switch (Phase)
        {
            case DayPhase.Day:
                if (n == 1) return "วันแรกในฐานะผู้ดูแล 'สุขนิรันดร์ แมนชั่น'\nทำความรู้จักผู้เช่า สำรวจตึก — วันละ 4 แอ็กชัน";
                if (n == 2) return "วันที่ 2... จำร่องรอยของผู้เช่าแต่ละคนไว้ให้ดี";
                return "วันสุดท้าย... ถ้ารอดคืนนี้ได้ ทุกอย่างจะจบ";
            case DayPhase.Eve:
                return "ตกเย็นแล้ว ต่อเบาะแสที่กระดาน แล้วไปทำพิธีที่ห้องเก็บของชั้นสาม";
            default:
                if (n == 2) return "ไปที่ห้อง " + GameState.Night2Room + " เดี๋ยวนี้!";
                if (n == 3) return "คืนสุดท้าย... อย่าเปิดประตูให้สิ่งที่ไม่ใช่คน";
                return "กฎของหอ: มีคนมาเคาะ ต้องฟังจังหวะ แล้วส่องตาแมวก่อนเปิดทุกครั้ง";
        }
    }

    IEnumerator ToEvening()
    {
        busy = true;
        yield return new WaitForSeconds(2f);
        while (DormFX.UIBusy) yield return null;
        DormFX.Msg("หมดแรงแล้ว... ตะวันเริ่มตก", 3f);
        yield return ChangePhaseRoutine(DayPhase.Eve, 1f);
        busy = false;
        StartCoroutine(IntroIfNeeded());
    }

    IEnumerator ChangePhase(DayPhase next, float fade)
    {
        busy = true;
        yield return ChangePhaseRoutine(next, fade);
        busy = false;
        StartCoroutine(IntroIfNeeded());
    }

    IEnumerator ChangePhaseRoutine(DayPhase next, float fade)
    {
        yield return new WaitForSeconds(0.6f);
        if (GameHUD.Instance) yield return GameHUD.Instance.FadeTo(1f, fade);
        if (time) time.SetPhase(next); else GameState.Phase = next;
        GameState.Phase = next;
        yield return new WaitForSeconds(0.4f);
        if (GameHUD.Instance) yield return GameHUD.Instance.FadeTo(0f, fade * 1.2f);
    }

    void OnRitualDone() => StartCoroutine(AfterRitual());

    IEnumerator AfterRitual()
    {
        busy = true;
        int n = Night;
        string text;
        if (n == 2)
        {
            GameState.Night2Room = DormData.Night2Rooms[Random.Range(0, DormData.Night2Rooms.Length)];
            Door d = RoomDoor(GameState.Night2Room);
            if (d) d.SetLocked(false);
            text = string.Format(DormData.Flashbacks[1], GameState.Night2Room);
        }
        else text = DormData.Flashbacks[n == 1 ? 0 : 2];

        yield return DormOverlay.I.Flashback(text, 5f);
        if (n == 1) DormFX.Discover("c_flash1");
        if (n == 3) DormFX.Discover("c_flash3");
        yield return null;
        while (DormFX.UIBusy) yield return null;

        if (n == 2)
        {
            // "ตัดไปตอนค่ำเลย" แล้วต้องรีบไปหาห้องที่พิธีบอก
            yield return ChangePhaseRoutine(DayPhase.Night, 0.5f);
            rushDeadline = Time.time + night2RushSeconds;
            busy = false;
            StartCoroutine(IntroIfNeeded());
            yield break;
        }
        DormFX.Msg("กลับห้องออฟฟิศ แล้วปิดประตูก่อนมืด", 4f);
        busy = false;
    }

    // =====================================================================
    //  night
    // =====================================================================
    IEnumerator NightRoutine()
    {
        int n = Night;
        nightDoor = n == 2 ? RoomDoor(GameState.Night2Room) : caretakerDoor;
        if (!nightDoor) nightDoor = caretakerDoor;
        if (nightDoor != caretakerDoor) nightDoor.SetLocked(false);
        nightArea = nightDoor == caretakerDoor ? officeArea : RoomArea(nightDoor);
        if (n == 2 && rushDeadline <= 0f) rushDeadline = Time.time + night2RushSeconds;   // เผื่อโหลดซีนกลับมากลางคืน
        safeReached = false; visitsDone = 0; visitsTotal = 0;
        if (anomalies) anomalies.BeginNight(n, nightDoor == caretakerDoor);

        // 1) ไปให้ถึงห้องปลอดภัย แล้วปิดประตู
        float nextScare = 0f;
        while (!(player && nightArea.Contains(player.position + Vector3.up * 0.2f) && !nightDoor.IsOpen))
        {
            if (GameState.Ended) yield break;
            if (n == 2 && Time.time > rushDeadline && Time.time > nextScare)
            {
                nextScare = Time.time + 20f;
                DormFX.Msg("ช้าเกินไป...", 2f);
                yield return DormOverlay.I.JumpScare(DormData.JumpScareSanity);
            }
            yield return null;
        }
        safeReached = true;
        DormOverlay.I.SetAlert("");

        if (n == 1 && !GameState.Has("rules_read"))
        {
            GameState.Set("rules_read");
            yield return new WaitForSeconds(1.5f);
            yield return DormOverlay.I.ShowDocument("กฎของหอ: ประตูตอนกลางคืน", KnockRules.RulesText);
        }

        // 2) คนมาเคาะ
        var plan = PlanVisits(n);
        visitsTotal = plan.Count;
        foreach (var v in plan)
        {
            yield return new WaitForSeconds(Random.Range(secondsBetweenVisits.x, secondsBetweenVisits.y));
            while (nightDoor.IsOpen || DormFX.UIBusy) yield return null;
            if (GameState.Ended) yield break;
            yield return RunVisit(v);
            visitsDone++;
            if (GameState.Ended) yield break;
        }

        // 3) เช้า
        yield return new WaitForSeconds(6f);
        if (anomalies) anomalies.EndNight();
        rushDeadline = 0f;
        if (n >= 3) Dawn();
        else yield return NextMorning();
        nightRoutine = null;
    }

    List<VisitInfo> PlanVisits(int n)
    {
        var plan = new List<VisitInfo>();
        var reals = new List<TenantInfo>();
        for (int k = 0; k < 2; k++)
        {
            int idx = (n - 1) * 2 + k;
            if (idx < GameState.RealVisitOrder.Count) reals.Add(DormData.Tenants[GameState.RealVisitOrder[idx]]);
        }
        foreach (var t in reals)
            plan.Add(GameState.Missing.Contains(t.id) ? MakeFake(t, RandomMode()) : MakeReal(t));   // คนที่หายแล้วมาเคาะ = ของปลอมแน่นอน

        var used = new List<TenantInfo>(reals);
        for (int k = 0; k < 2; k++)
        {
            var t = PickFakeTarget(n, used);
            used.Add(t);
            plan.Add(MakeFake(t, RandomMode()));
        }

        if (n == 1 && plan.Count == 4)
        {
            // คืนแรกสอนเล่น: ตัวจริงมาก่อน แล้วตามด้วยตัวปลอมที่เคาะผิดจังหวะชัดๆ
            var lesson = MakeFake(plan[2].tenant, FakeMode.BadKnock);
            plan = new List<VisitInfo> { plan[0], lesson, plan[1], plan[3] };
        }
        else DormFX.Shuffle(plan);
        return plan;
    }

    TenantInfo PickFakeTarget(int n, List<TenantInfo> exclude)
    {
        var missing = new List<TenantInfo>();
        var pool = new List<TenantInfo>();
        foreach (var t in DormData.Tenants)
        {
            if (exclude.Contains(t)) continue;
            if (GameState.Missing.Contains(t.id)) missing.Add(t); else pool.Add(t);
        }
        if (n >= 2 && missing.Count > 0 && Random.value < 0.5f) return missing[Random.Range(0, missing.Count)];
        if (pool.Count == 0) pool.AddRange(missing);
        if (pool.Count == 0) pool.AddRange(DormData.Tenants);
        return pool[Random.Range(0, pool.Count)];
    }

    static FakeMode RandomMode() => (FakeMode)Random.Range(0, 3);

    static VisitInfo MakeReal(TenantInfo t)
    {
        int li = Random.Range(0, t.doorLines.Length);
        var v = new VisitInfo { tenant = t, fake = false, knock = KnockRules.RandomNormal(), loud = Random.value < 0.35f, line = t.doorLines[li] };
        v.traces.AddRange(t.traces);
        v.traces.Add(t.doorTraces[li]);
        DormFX.Shuffle(v.traces);
        return v;
    }

    static VisitInfo MakeFake(TenantInfo t, FakeMode mode)
    {
        int li = Random.Range(0, t.doorLines.Length);
        var v = new VisitInfo { tenant = t, fake = true, mode = mode, loud = Random.value < 0.5f, line = t.doorLines[li] };
        v.knock = mode == FakeMode.BadKnock ? KnockRules.RandomAbnormal() : KnockRules.RandomNormal();
        v.traces.AddRange(t.traces);
        if (mode == FakeMode.WrongSignature) v.traces[0] = t.decoy;        // ลักษณะประจำตัวผิด
        if (mode != FakeMode.MissingLineTrace) v.traces.Add(t.doorTraces[li]);   // ของปลอมไม่ทิ้งร่องรอยตามคำพูด
        DormFX.Shuffle(v.traces);
        return v;
    }

    IEnumerator RunVisit(VisitInfo v)
    {
        Current = v;
        v.door = nightDoor;
        TenantNpc tn = null;
        NpcBase npc;
        if (!v.fake && tenants.TryGetValue(v.tenant.id, out tn) && tn)
        {
            tn.ShowAtDoor(nightDoor);
            npc = tn;
        }
        else
        {
            v.fake = true;
            tenants.TryGetValue(v.tenant.id, out var look);
            fake.Disguise(v.tenant, look ? look.model : null);
            fake.ShowAtDoor(nightDoor);
            npc = fake;
        }

        answered = false;
        nightDoor.Answered += OnAnswered;
        yield return KnockRules.Perform(nightDoor, npc, v.knock, v.loud, answerWindow,
            () =>
            {
                // เสียงพากย์ดังจากหน้าประตู (ของปลอมใช้เสียงเวอร์ชันปลอมถ้ามี)
                float len = Voice.Say(v.tenant.name, v.line, v.fake, nightDoor.VisitorStandPoint.position + Vector3.up * 1.5f);
                DormFX.Sub(v.tenant.name + ": " + v.line, Mathf.Max(6f, len + 0.4f), false);
                if (tn && !v.fake) tn.PlayTalk(); else fake.PlayTalk();
            });
        while (!answered && nightDoor.HasPendingVisitor && !GameState.Ended) yield return null;
        nightDoor.Answered -= OnAnswered;

        if (!answered)   // เริ่มเคาะไม่ได้ (ประตูเปิดอยู่) -> ข้าม
        {
            if (tn && !v.fake) tn.LeaveDoor(); else fake.Hide();
            Current = null;
            yield break;
        }

        string name = v.tenant.name;
        if (!v.fake)
        {
            if (lastAnswer == DoorAnswer.Opened)
            {
                DormFX.Sub(name + ": ขอบคุณนะ... คืนนี้ทางเดินมันแปลกๆ", 4f);
                yield return new WaitForSeconds(Mathf.Max(3.5f, Voice.LastLength));
                tn.LeaveDoor();
                DormFX.Msg(name + " กลับห้องไปอย่างปลอดภัย", 3f);
            }
            else
            {
                tn.LeaveDoor();
                GameState.Missing.Add(v.tenant.id);
                GameState.MissingLog.Add(v.tenant.id);
                UpdateMissingFlags();
                DormFX.Msg("เสียงฝีเท้าค่อยๆ เงียบหายไปในทางเดิน...", 4f);
            }
        }
        else
        {
            if (lastAnswer == DoorAnswer.Opened)
            {
                fake.Reveal();
                yield return DormOverlay.I.JumpScare(DormData.WrongDoorSanity);
                fake.Hide();
                DormFX.Msg("นั่นไม่ใช่" + name + "...  ปิดประตูซะ", 4f);
            }
            else
            {
                fake.Hide();
                DormFX.Msg("เสียงเคาะหยุดลง... ทางเดินกลับมาเงียบ", 3.5f);
            }
        }
        Current = null;
    }

    void OnAnswered(NpcBase npc, DoorAnswer a)
    {
        lastAnswer = a;
        answered = true;
    }

    /// <summary>ให้ DormOverlay ใช้ตอนผู้เล่นส่องตาแมว</summary>
    public bool TryPeep(Door door, out Sprite portrait, out string text)
    {
        portrait = null; text = null;
        var v = Current;
        if (v == null || v.door != door || !door.HasPendingVisitor) return false;
        portrait = DormFX.Portrait(v.tenant.portrait);
        var sb = new StringBuilder();
        sb.Append("<b>ดูเหมือน: ").Append(v.tenant.name).Append(" (ห้อง ").Append(v.tenant.room).Append(")</b>\n");
        sb.Append("พูดว่า: \"").Append(v.line).Append("\"\n");
        sb.Append("สิ่งที่เห็น: ").Append(string.Join("  ·  ", v.traces));
        text = sb.ToString();
        return true;
    }

    IEnumerator NextMorning()
    {
        busy = true;
        yield return new WaitForSeconds(2f);
        if (GameHUD.Instance) yield return GameHUD.Instance.FadeTo(1f, 1.5f);
        DormFX.Msg("รุ่งเช้า... ผ่านคืนที่ " + Night + " มาได้", 3f);
        yield return new WaitForSeconds(2.5f);
        GameState.Night++;
        GameState.ActionsUsed = 0;
        if (time) time.SetPhase(DayPhase.Day);
        GameState.Phase = DayPhase.Day;
        foreach (var t in tenants.Values) if (t) t.Refresh();
        if (GameHUD.Instance) yield return GameHUD.Instance.FadeTo(0f, 1.5f);
        busy = false;

        if (GameState.MissingLog.Count > 0)
        {
            var names = new List<string>();
            foreach (var id in GameState.MissingLog) { var t = DormData.Tenant(id); if (t != null) names.Add(t.name + " ห้อง " + t.room); }
            GameState.MissingLog.Clear();
            yield return new WaitForSeconds(1f);
            DormFX.Msg("เช้านี้... " + string.Join(", ", names) + " ไม่อยู่ในห้องแล้ว\nประตูยังล็อกจากด้านใน", 7f);
            yield return new WaitForSeconds(7.5f);
        }
        StartCoroutine(IntroIfNeeded());
    }

    void UpdateMissingFlags()
    {
        int m = GameState.Missing.Count;
        if (m >= 4) DormFX.SetFlag("missing_4");
        if (m >= 6) DormFX.SetFlag("missing_6");
    }

    void Dawn()
    {
        UpdateMissingFlags();
        DormOverlay.I.SetAlert("");
        if (EndingManager.Instance) EndingManager.Instance.TryEnd("Dawn");
        GameState.Ended = true;
    }

    void OnEnding(EndingData e, string reason)
    {
        GameState.Ended = true;
        DormOverlay.I.SetAlert("");
        if (anomalies) anomalies.EndNight();
        StartCoroutine(AppendStats(e));
    }

    IEnumerator AppendStats(EndingData e)
    {
        yield return null;   // ให้ GameHUD โชว์หน้าจบก่อน แล้วค่อยเขียนทับพร้อมสถิติ
        if (GameHUD.Instance)
            GameHUD.Instance.ShowEnding(e.Title, e.Text + "\n\n<size=24><color=#b0a090>ผู้เช่าที่หายไป " + GameState.Missing.Count +
                                                     "/6   ·   ความจริงที่ค้นพบ " + GameState.Truths.Count + "/6</color></size>");
    }

    // =====================================================================
    //  helpers
    // =====================================================================
    Door RoomDoor(string room)
    {
        if (string.IsNullOrEmpty(room)) return null;
        doorsByName.TryGetValue("Door_" + room, out var d);
        return d;
    }

    /// <summary>กล่องคร่าวๆ ของห้องหลังประตู (ห้องลึก ~3.6 m เข้าไปทางที่จุดยืนหันหน้า)</summary>
    static Bounds RoomArea(Door d)
    {
        Vector3 f = d.VisitorStandPoint.forward; f.y = 0; f.Normalize();
        Vector3 c = d.transform.position + f * 2.0f + Vector3.up * 1.3f;
        Vector3 size = Mathf.Abs(f.z) > Mathf.Abs(f.x) ? new Vector3(2.8f, 2.6f, 3.8f) : new Vector3(3.8f, 2.6f, 2.8f);
        return new Bounds(c, size);
    }
}
