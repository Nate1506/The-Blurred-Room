using System.Collections;
using BlurredRoom;
using UnityEngine;

// จุดสำรวจตอนกลางวัน: ตรวจดู / ถ่ายรูป / เก็บของ -> ใช้ 1 แอ็กชัน แล้วได้เบาะแส 1 ชิ้น
// ทำได้แค่ตอนกลางวัน และเฉพาะวันที่ >= minNight (บางเบาะแสโผล่วันหลัง)
public class DayActionPoint : MonoBehaviour, IInteractable
{
    public enum Kind { Inspect, Photo, Collect }

    public Kind kind = Kind.Inspect;
    [Tooltip("id ใน DormData.Clues")]
    public string clueId = "c_register";
    [Tooltip("ชื่อที่โชว์บนจอ เช่น 'สมุดผู้เช่า'")]
    public string label = "สมุดผู้เช่า";
    [Min(1)] public int minNight = 1;

    Renderer[] rends;
    Collider[] cols;
    bool shown = true;

    string Flag => "act_" + clueId;
    bool Done => GameState.Has(Flag);
    bool Visible => GameState.Night >= minNight && !(kind == Kind.Collect && Done);

    void Awake()
    {
        rends = GetComponentsInChildren<Renderer>(true);
        cols = GetComponentsInChildren<Collider>(true);
    }

    void Update()
    {
        bool v = Visible;
        if (v == shown) return;
        shown = v;
        foreach (var r in rends) if (r) r.enabled = v;
        foreach (var c in cols) if (c) c.enabled = v;
    }

    public string Prompt
    {
        get
        {
            string verb = kind == Kind.Photo ? "ถ่ายรูป: " : kind == Kind.Collect ? "เก็บ: " : "ตรวจดู: ";
            if (DormFX.Phase != DayPhase.Day) return verb + label + " (ทำได้ตอนกลางวัน)";
            return verb + label + " (ใช้ 1 แอ็กชัน, เหลือ " + DormFX.ActionsLeft + ")";
        }
    }

    public bool CanInteract(GameObject interactor) => shown && !Done && !DormFX.UIBusy;

    public void Interact(GameObject interactor)
    {
        if (DormFX.Phase != DayPhase.Day) { DormFX.Msg("ตอนนี้ไม่ใช่เวลาสำรวจ — ทำได้แค่ตอนกลางวัน", 3f); return; }
        if (DormFX.ActionsLeft <= 0) { DormFX.Msg("วันนี้หมดแรงแล้ว (แอ็กชันครบ 4)", 3f); return; }
        StartCoroutine(Do());
    }

    IEnumerator Do()
    {
        GameState.ActionsUsed++;
        DormFX.SetFlag(Flag);
        if (kind == Kind.Photo) yield return DormOverlay.I.CameraFlash();
        DormFX.Discover(clueId);
    }
}
