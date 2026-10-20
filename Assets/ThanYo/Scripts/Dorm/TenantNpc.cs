using System.Collections;
using BlurredRoom;
using UnityEngine;

// ผู้เช่า 1 คน
// กลางวัน: อยู่ในห้อง (มองไม่เห็น) -> ผู้เล่นเคาะประตู (RoomKnock) -> เปิดประตู เดินออกมาคุย -> คุยจบ เดินกลับเข้าห้อง ปิดประตู
//          ครั้งแรก = สัมภาษณ์ (ใช้ 1 แอ็กชัน + ได้เบาะแส), ครั้งต่อไป = คุยเล่น (ฟรี)
// กลางคืน: StoryDirector พามาเคาะประตูผ่าน ShowAtDoor
// หายตัวไปแล้ว (GameState.Missing) = ไม่โผล่อีกเลย
public class TenantNpc : NpcBase, IInteractable
{
    public string tenantId = "phum";
    [Tooltip("ประตูห้องของผู้เช่าคนนี้")]
    public Door homeDoor;
    [Tooltip("โมเดล + อนิเมชัน (Assets/ThanYo/Prefabs/Tenants/Tenant_xxx). ว่าง = ใช้ตัวกล่องแทน")]
    public GameObject model;

    [Header("เดินออกมาจากห้อง (ตอนถูกเคาะ)")]
    [Tooltip("ความเร็วเดิน (เมตร/วินาที)")]
    [Min(0.2f)] public float walkSpeed = 1.1f;
    [Tooltip("เริ่มเดินจากในห้อง ลึกจากประตูกี่เมตร")]
    [Min(0.3f)] public float insideDepth = 1.4f;
    [Tooltip("ยืนหน้าห้อง ห่างจากจุดยืนหน้าประตูกี่เมตร")]
    public float standOffset = 0.35f;
    [Tooltip("คุยเสร็จแล้วรอกี่วินาทีก่อนเดินกลับเข้าห้อง")]
    [Min(0f)] public float lingerAfterTalk = 1.2f;

    GameObject body;
    CapsuleCollider col;
    Animator anim;
    bool talking, atNightDoor, outside;
    float lastTalkEnd = -10f;

    public TenantInfo Info => DormData.Tenant(tenantId);
    public override NpcKind Kind => NpcKind.Neutral;
    /// <summary>กำลังออกมานอกห้อง (เดินออก/คุย/เดินกลับ)</summary>
    public bool IsOut => outside;
    public bool IsGone => GameState.Missing.Contains(tenantId);

    protected override void Awake()
    {
        base.Awake();
        col = gameObject.GetComponent<CapsuleCollider>();
        if (!col) col = gameObject.AddComponent<CapsuleCollider>();
        col.radius = 0.3f; col.height = 1.8f; col.center = new Vector3(0, 0.9f, 0);
        if (model) body = DormFX.SpawnModel(model, transform);
        else if (Info != null) body = DormFX.BuildPerson(transform, Info);
        if (body) anim = body.GetComponentInChildren<Animator>();
    }

    protected override void Start()
    {
        base.Start();
        if (TimeOfDayManager.Instance) TimeOfDayManager.Instance.PhaseChanged += OnPhase;
        Refresh();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (TimeOfDayManager.Instance) TimeOfDayManager.Instance.PhaseChanged -= OnPhase;
    }

    void OnPhase(DayPhase p) => Refresh();

    /// <summary>ปกติอยู่ในห้อง (ซ่อน) — โผล่เฉพาะตอนถูกเคาะ หรือตอนมาเคาะประตูกลางคืน</summary>
    public void Refresh()
    {
        if (atNightDoor || outside) return;
        ShowBody(false);
    }

    public void ShowBody(bool on)
    {
        if (body) body.SetActive(on);
        if (col) col.enabled = on;
    }

    // ---------- night visit ----------
    public void ShowAtDoor(Door door)
    {
        atNightDoor = true;
        DormFX.PlaceAtDoor(transform, door);
        ShowBody(true);
    }

    public void PlayTalk() => DormFX.PlayTalk(body);

    public void LeaveDoor()
    {
        atNightDoor = false;
        ShowBody(false);
        Refresh();
    }

    public override void OnDoorAnswered(Door door, DoorAnswer answer) { }   // StoryDirector จัดการผลลัพธ์เอง

    // ---------- ถูกเคาะประตู: เดินออกมาคุย ----------
    /// <summary>ตอบรับการเคาะ (เรียกจาก RoomKnock) — เปิดประตู เดินออกมา คุย แล้วเดินกลับ</summary>
    public IEnumerator AnswerKnock(Door door)
    {
        if (!door || outside || atNightDoor || IsGone) yield break;
        outside = true;

        Vector3 sp = door.VisitorStandPoint.position;
        Vector3 outward = sp - door.transform.position; outward.y = 0f;
        if (outward.sqrMagnitude < 0.001f) outward = door.VisitorStandPoint.forward;
        outward.Normalize();
        Vector3 inside = door.transform.position - outward * insideDepth; inside.y = sp.y;
        Vector3 standAt = sp + outward * standOffset;

        // เปิดประตูจากด้านใน
        door.SetLocked(false);
        door.TryOpen();
        yield return new WaitForSeconds(0.35f);

        transform.position = inside;
        ShowBody(true);
        yield return WalkTo(standAt);
        FacePlayer();

        // คุย (ครั้งแรก = สัมภาษณ์)
        yield return Talk();
        yield return new WaitForSeconds(lingerAfterTalk);

        // เดินกลับเข้าห้อง แล้วปิดประตู
        yield return WalkTo(inside);
        ShowBody(false);
        if (door.IsOpen) door.Close();
        yield return new WaitForSeconds(0.6f);
        door.SetLocked(true);
        outside = false;
    }

    IEnumerator WalkTo(Vector3 target)
    {
        SetWalk(true);
        Vector3 start = transform.position;
        Vector3 dir = target - start; dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);
        float dur = dir.magnitude / walkSpeed;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(start, target, t / dur);
            yield return null;
        }
        transform.position = target;
        SetWalk(false);
    }

    void SetWalk(bool on)
    {
        if (!anim || !anim.runtimeAnimatorController) return;
        foreach (var p in anim.parameters) if (p.name == "Walk" && p.type == AnimatorControllerParameterType.Bool) { anim.SetBool("Walk", on); return; }
    }

    void FacePlayer()
    {
        if (!DormFX.Player) return;
        Vector3 d = DormFX.Player.position - transform.position; d.y = 0;
        if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
    }

    // ---------- interview ----------
    public string Prompt
    {
        get
        {
            var i = Info;
            if (GameState.Interviewed.Contains(tenantId)) return "คุยกับ" + i.name;
            return "สัมภาษณ์" + i.name + " ห้อง " + i.room + " (ใช้ 1 แอ็กชัน, เหลือ " + DormFX.ActionsLeft + ")";
        }
    }

    public bool CanInteract(GameObject interactor) =>
        body && body.activeSelf && !atNightDoor && !outside && !talking && !DormFX.UIBusy && Time.time - lastTalkEnd > 0.6f;

    public void Interact(GameObject interactor) => StartCoroutine(Talk());

    IEnumerator Talk()
    {
        if (talking) yield break;
        talking = true;
        var i = Info;
        FacePlayer();

        if (!GameState.Interviewed.Contains(tenantId))
        {
            if (DormFX.ActionsLeft <= 0)
            {
                DormFX.Msg("วันนี้หมดแรงสัมภาษณ์แล้ว (แอ็กชันครบ 4)", 3f);
            }
            else
            {
                GameState.ActionsUsed++;
                GameState.Interviewed.Add(tenantId);
                foreach (var line in i.interview)
                {
                    PlayTalk();
                    DormFX.Sub(i.name + ": " + line, 3.2f);
                    yield return new WaitForSeconds(Mathf.Max(3.4f, Voice.LastLength + 0.3f));   // มีเสียงพากย์ = รอพูดจบ
                }
                yield return new WaitForSeconds(0.5f);
                DormFX.Discover(i.clueId);
            }
        }
        else
        {
            PlayTalk();
            DormFX.Sub(i.name + ": " + i.chat, 3.5f);
            yield return new WaitForSeconds(Mathf.Max(2.5f, Voice.LastLength + 0.3f));
        }

        talking = false;
        lastTalkEnd = Time.time;
    }
}
