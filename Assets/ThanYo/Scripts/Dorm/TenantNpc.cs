using System.Collections;
using BlurredRoom;
using UnityEngine;

// ผู้เช่า 1 คน: กลางวันยืนหน้าห้องตัวเอง ให้สัมภาษณ์ (ครั้งแรกใช้ 1 แอ็กชัน + ได้เบาะแส + รู้ร่องรอยประจำตัว)
// กลางคืนหายเข้าห้อง แล้ว StoryDirector จะพามาเคาะประตูผ่าน ShowAtDoor
// ถ้าหายตัวไปแล้ว (GameState.Missing) จะไม่โผล่อีกเลย
public class TenantNpc : NpcBase, IInteractable
{
    public string tenantId = "phum";
    [Tooltip("ประตูห้องของผู้เช่าคนนี้ (กลางวันจะยืนอยู่หน้าประตูนี้)")]
    public Door homeDoor;
    [Tooltip("โมเดล + อนิเมชัน (Assets/ThanYo/Prefabs/Tenants/Tenant_xxx). ว่าง = ใช้ตัวกล่องแทน")]
    public GameObject model;

    GameObject body;
    CapsuleCollider col;
    bool talking, atNightDoor;
    float lastTalkEnd = -10f;

    public TenantInfo Info => DormData.Tenant(tenantId);
    public override NpcKind Kind => NpcKind.Neutral;

    protected override void Awake()
    {
        base.Awake();
        col = gameObject.GetComponent<CapsuleCollider>();
        if (!col) col = gameObject.AddComponent<CapsuleCollider>();
        col.radius = 0.3f; col.height = 1.8f; col.center = new Vector3(0, 0.9f, 0);
        if (model) body = DormFX.SpawnModel(model, transform);
        else if (Info != null) body = DormFX.BuildPerson(transform, Info);
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

    /// <summary>กลางวัน + ยังไม่หาย = ยืนหน้าห้อง, นอกนั้นซ่อน</summary>
    public void Refresh()
    {
        if (atNightDoor) return;
        bool show = DormFX.Phase == DayPhase.Day && !GameState.Missing.Contains(tenantId);
        if (show && homeDoor)
        {
            Transform sp = homeDoor.VisitorStandPoint;
            Vector3 f = sp.forward; f.y = 0; f.Normalize();
            transform.SetPositionAndRotation(sp.position + f * 0.35f, Quaternion.LookRotation(-f));
        }
        ShowBody(show);
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
        body && body.activeSelf && !atNightDoor && !talking && !DormFX.UIBusy && Time.time - lastTalkEnd > 0.6f;

    public void Interact(GameObject interactor) => StartCoroutine(Talk());

    IEnumerator Talk()
    {
        talking = true;
        var i = Info;
        if (DormFX.Player)
        {
            Vector3 d = DormFX.Player.position - transform.position; d.y = 0;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
        }

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
            yield return new WaitForSeconds(Mathf.Max(1f, Voice.LastLength));
        }

        talking = false;
        lastTalkEnd = Time.time;
    }
}
