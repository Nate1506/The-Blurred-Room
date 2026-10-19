using System.Collections;
using System.Collections.Generic;
using BlurredRoom;
using UnityEngine;

// ตัวกำกับเรื่อง 3 คืน: แต่ละคืนมี กลางวัน -> เย็น -> กลางคืน และเป้าหมายบนจอทุกช่วง
// เก็บความคืบหน้าใน GameState จึงข้ามซีน Inside <-> ThanYo ได้
// เหตุการณ์กลางคืน (คนมาเคาะ, พิธี) เกิดเฉพาะในซีน Inside (กลางคืนออกนอกตึกไม่ได้อยู่แล้ว)
public class StoryDirector : MonoBehaviour
{
    [Header("Inside scene only (leave empty in ThanYo)")]
    public Door caretakerDoor;
    public Door room305Door;
    public NeutralNpc tenant;
    public NeutralNpc mimic;
    [Tooltip("Ritual steps in room 305, in order: thread, incense, rice")]
    public RitualPoint[] ritualSteps = new RitualPoint[0];
    public Bounds officeArea = new Bounds(new Vector3(5.2f, 1.3f, 7.9f), new Vector3(5.8f, 2.6f, 3.75f));

    [Header("Night 3")]
    public float night3TimeLimit = 300f;
    public float night3KnockEvery = 50f;

    TimeOfDayManager time;
    PlayerSanity sanity;
    Transform player;
    bool inside, busy;
    Coroutine nightRoutine;
    DoorAnswer lastAnswer;
    bool answered;
    readonly List<Light> flicker = new List<Light>();

    // ---------- shortcuts ----------
    static bool Has(string f) => GameState.Has(f);
    static void Set(string f) => GameState.Set(f);
    bool Clue(string id) { foreach (var c in GameState.Clues) if (c && c.Id == id) return true; return false; }
    static bool Item(string id) => GameState.Items.Contains(id);
    bool InOffice => inside && player && officeArea.Contains(player.position);
    DayPhase Phase => time ? time.Phase : GameState.Phase;

    void Start()
    {
        time = TimeOfDayManager.Instance;
        sanity = FindFirstObjectByType<PlayerSanity>();
        var pm = FindFirstObjectByType<PlayerMovement>(); if (pm) player = pm.transform;
        inside = caretakerDoor != null;
        if (EndingManager.Instance) EndingManager.Instance.EndingReached += (e, r) => GameState.Ended = true;
        if (inside)
        {
            caretakerDoor.Answered += OnAnswered;
            if (room305Door) room305Door.Answered += OnAnswered;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Point && Vector3.Distance(l.transform.position, caretakerDoor.transform.position) < 6f) flicker.Add(l);
            for (int i = 0; i < ritualSteps.Length; i++) if (ritualSteps[i]) ritualSteps[i].gameObject.SetActive(false);
        }
        StartCoroutine(IntroIfNeeded());
    }

    void OnAnswered(NpcBase npc, DoorAnswer a) { lastAnswer = a; answered = true; }

    IEnumerator IntroIfNeeded()
    {
        yield return new WaitForSeconds(1.2f);
        string key = "intro_n" + GameState.Night + "_" + Phase;
        if (Has(key)) yield break;
        Set(key);
        Msg(IntroText(), 6f);
    }

    string IntroText()
    {
        int n = GameState.Night;
        switch (Phase)
        {
            case DayPhase.Day:
                if (n == 1) return "Day 1. You are the new caretaker of this building.";
                if (n == 2) return "Day 2. Mali from room 204 never came down this morning.";
                return "Day 3. Two more tenants are gone. It has to end tonight.";
            case DayPhase.Eve: return "Evening. Be back in your room before dark.";
            default: return n == 3 ? "Night 3. Break the ritual before it takes the building." : "Night " + n + ". House rule: never open the door after dark.";
        }
    }

    // ---------- main loop: objectives + phase changes ----------
    void Update()
    {
        if (GameState.Ended || busy) return;
        var list = new List<(string, bool)>();
        int n = GameState.Night;
        string header = "Night " + n + "  -  " + (Phase == DayPhase.Day ? "Day" : Phase == DayPhase.Eve ? "Evening" : "Night");

        if (Phase == DayPhase.Day)
        {
            if (n == 1) list.Add(("Read the tenant register (lobby counter)", Clue("register")));
            if (n == 2) { list.Add(("Check room 204 (2nd floor)", Clue("photo_305"))); list.Add(("Find the old caretaker's logbook (B1 door, lobby)", Clue("logbook"))); }
            if (n == 3) { list.Add(("Find the sacred thread (2nd floor storage)", Item("thread"))); list.Add(("Find incense (3rd floor WC)", Item("incense"))); list.Add(("Find rice (laundry, ground floor)", Item("rice"))); }
            if (AllDone(list)) StartCoroutine(ChangePhase(DayPhase.Eve));
        }
        else if (Phase == DayPhase.Eve)
        {
            bool needKey = n == 2;
            if (needKey) list.Add(("Take the key to room 305 (desk in your room)", Item("key_305")));
            list.Add(("Get back to your room (OFFICE) before dark", InOffice && (!needKey || Item("key_305"))));
            if (!inside) list.Add(("(the building entrance is behind you)", false));
            if (AllDone(list)) StartCoroutine(ChangePhase(DayPhase.Night));
        }
        else // night
        {
            if (n == 1) { list.Add(("Stay in your room", InOffice)); list.Add(("Someone may knock. Look first. [Q]", Has("n1_done"))); }
            if (n == 2) { list.Add(("Check room 305 (3rd floor)", Clue("ritual_order"))); list.Add(("Get back to your room", Has("n2_back"))); }
            if (n == 3)
            {
                list.Add(("Room 305: tie the sacred thread [R]", Has("r_thread")));
                list.Add(("Room 305: light the incense [R]", Has("r_incense")));
                list.Add(("Room 305: scatter the rice [R]", Has("r_rice")));
            }
            if (inside && nightRoutine == null) nightRoutine = StartCoroutine(n == 1 ? Night1() : n == 2 ? Night2() : Night3());
        }
        if (GameHUD.Instance) GameHUD.Instance.SetObjectives(header, list);
    }

    static bool AllDone(List<(string, bool)> l) { foreach (var o in l) if (!o.Item2) return false; return l.Count > 0; }

    IEnumerator ChangePhase(DayPhase next)
    {
        busy = true;
        yield return new WaitForSeconds(1f);
        if (GameHUD.Instance) yield return GameHUD.Instance.FadeTo(1f, 1f);
        if (time) time.SetPhase(next); else GameState.Phase = next;
        yield return new WaitForSeconds(0.5f);
        if (GameHUD.Instance) yield return GameHUD.Instance.FadeTo(0f, 1.2f);
        busy = false;
        StartCoroutine(IntroIfNeeded());
    }

    IEnumerator NextMorning()
    {
        busy = true;
        yield return new WaitForSeconds(3f);
        if (GameHUD.Instance) yield return GameHUD.Instance.FadeTo(1f, 1.5f);
        Msg("Morning came. Night " + GameState.Night + " survived.", 4f);
        yield return new WaitForSeconds(3f);
        GameState.Night++;
        nightRoutine = null;
        if (time) time.SetPhase(DayPhase.Day); else GameState.Phase = DayPhase.Day;
        if (GameState.Night == 2) Set("day2");                 // unlocks room 204
        if (GameHUD.Instance) yield return GameHUD.Instance.FadeTo(0f, 1.5f);
        busy = false;
        StartCoroutine(IntroIfNeeded());
    }

    // ---------- nights ----------
    IEnumerator Night1()
    {
        while (!InOffice) yield return null;
        yield return new WaitForSeconds(10f);
        yield return Visit(caretakerDoor, tenant, true, 0.9f, new KnockPattern { knocks = 3, interval = 0.7f, answerWindow = 15f },
                           "Mali (204): It's me... from 204. I locked myself out. Please.");
        if (lastAnswer == DoorAnswer.Opened) { yield return new WaitForSeconds(4f); tenant.SetActiveState(false); Msg("She thanks you and hurries upstairs.", 4f); }
        else Msg("The footsteps fade down the hall.", 4f);

        yield return new WaitForSeconds(16f);
        StartCoroutine(Flicker(9f));
        yield return Visit(caretakerDoor, mimic, false, 0.9f, new KnockPattern { knocks = 4, interval = 0.45f, answerWindow = 15f },
                           "Mali (204): It's me... from 204. I locked myself out. Please.");
        yield return MimicConsequence(45f, 6f, "That wasn't her.", "It kept knocking long after it should have stopped.");
        Set("n1_done");
        StartCoroutine(NextMorning());
    }

    IEnumerator Night2()
    {
        while (!Clue("ritual_order")) yield return null;               // player went to 305
        Msg("Go back to your room. Now.", 4f);
        while (!InOffice) yield return null;
        Set("n2_back");
        yield return new WaitForSeconds(8f);
        StartCoroutine(Flicker(10f));
        // Mali is missing, so whatever uses her voice is not her. It stands far too close to the door.
        yield return Visit(caretakerDoor, mimic, true, 0.35f, new KnockPattern { knocks = 5, interval = 0.5f, answerWindow = 18f },
                           "Mali (204): Please... it's so cold up there. Let me in.");
        yield return MimicConsequence(50f, 8f, "Mali has been missing since this morning.", "It whispered your name through the door until dawn.");
        StartCoroutine(NextMorning());
    }

    IEnumerator Night3()
    {
        float start = Time.time;
        float nextKnock = Time.time + 25f;
        if (ritualSteps.Length > 0 && ritualSteps[0]) ritualSteps[0].gameObject.SetActive(true);
        while (true)
        {
            // ritual order: enable the next step only after the previous one
            for (int i = 0; i < ritualSteps.Length; i++)
                if (ritualSteps[i]) ritualSteps[i].gameObject.SetActive(i == 0 ? !Has("r_thread") : i == 1 ? Has("r_thread") && !Has("r_incense") : Has("r_incense") && !Has("r_rice"));

            if (Has("r_thread") && Has("r_incense") && Has("r_rice"))
            {
                Set("ritual_complete");
                if (ClueManager.Instance) ClueManager.Instance.SetFlag("ritual_complete");
                yield return new WaitForSeconds(1.5f);
                End(EndingReasons.Solved);
                yield break;
            }
            if (Time.time - start > night3TimeLimit) { End(EndingReasons.TimeUp); yield break; }

            if (Time.time > nextKnock && room305Door && !room305Door.IsOpen && player && Vector3.Distance(player.position, room305Door.transform.position) < 8f)
            {
                StartCoroutine(Flicker(6f));
                yield return Visit(room305Door, mimic, false, 0.9f, new KnockPattern { knocks = 4, interval = 0.45f, answerWindow = 12f },
                                   "Mali (204): You found it... open the door.");
                yield return MimicConsequence(60f, 4f, "It was waiting for you.", "The knocking stops. For now.");
                nextKnock = Time.time + night3KnockEvery;
            }
            yield return null;
        }
    }

    // ---------- helpers ----------
    IEnumerator Visit(Door door, NeutralNpc npc, bool facingDoor, float distance, KnockPattern pattern, string line)
    {
        Transform p = door.VisitorStandPoint;
        Vector3 toDoor = p.forward;
        Vector3 pos = p.position + toDoor * (0.9f - distance);   // stand point is 0.9 m from the door
        npc.Appear(pos, facingDoor ? Quaternion.LookRotation(toDoor) : Quaternion.LookRotation(-toDoor));
        answered = false;
        void OnKnock(int i, int total) { if (i == 1) { door.Knocked -= OnKnock; Sub(line, 5f); } }
        door.Knocked += OnKnock;
        if (!door.BeginVisit(npc, pattern)) { door.Knocked -= OnKnock; npc.SetActiveState(false); yield break; }
        while (!answered) yield return null;
    }

    IEnumerator MimicConsequence(float openedLoss, float ignoredLoss, string openedMsg, string ignoredMsg)
    {
        if (lastAnswer == DoorAnswer.Opened)
        {
            if (sanity) sanity.Drain(openedLoss);
            SetFlickerLights(false);
            yield return new WaitForSeconds(0.6f);
            mimic.SetActiveState(false);
            yield return new WaitForSeconds(1.2f);
            SetFlickerLights(true);
            Msg(openedMsg, 5f);
        }
        else
        {
            if (sanity) sanity.Drain(ignoredLoss);
            Msg(ignoredMsg, 5f);
        }
    }

    IEnumerator Flicker(float seconds)
    {
        var lights = new List<Light>(flicker);
        if (room305Door && GameState.Night == 3)
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Point && Vector3.Distance(l.transform.position, room305Door.transform.position) < 6f) lights.Add(l);
        var baseI = new Dictionary<Light, float>();
        foreach (var l in lights) baseI[l] = l.intensity;
        float end = Time.time + seconds;
        while (Time.time < end)
        {
            foreach (var l in lights) if (l) l.intensity = baseI[l] * (Random.value < 0.25f ? Random.Range(0f, 0.3f) : 1f);
            yield return new WaitForSeconds(Random.Range(0.04f, 0.18f));
        }
        foreach (var l in lights) if (l) l.intensity = baseI[l];
    }

    void SetFlickerLights(bool on) { foreach (var l in flicker) if (l) l.enabled = on; }

    void End(string reason)
    {
        GameState.Ended = true;
        if (EndingManager.Instance) EndingManager.Instance.TryEnd(reason);
    }

    void Msg(string t, float s) { if (GameHUD.Instance) GameHUD.Instance.ShowMessage(t, s); }
    void Sub(string t, float s) { if (GameHUD.Instance) GameHUD.Instance.ShowSubtitle(t, s); }
}
