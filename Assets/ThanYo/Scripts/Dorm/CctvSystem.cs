using BlurredRoom;
using UnityEngine;

// กล้องวงจรปิด 3 ตัว (ทางเดินชั้น 1-3) ดูจากจอบนโต๊ะในห้องออฟฟิศ
// เปิด/ปิด: E ที่จอ   เปลี่ยนกล้อง: A / D
// ถ้าจ้องกล้องที่มีความผิดปกติ ~1.2 วิ = บันทึกได้ (ครั้งแรกได้เบาะแส "กล้องวงจรปิด")
public class CctvSystem : MonoBehaviour
{
    public Camera[] cams = new Camera[0];
    public string[] labels = { "CAM01  ชั้น 1", "CAM02  ชั้น 2", "CAM03  ชั้น 3" };
    public AnomalyDirector anomalies;
    public Vector2Int resolution = new Vector2Int(320, 180);   // ความละเอียดต่ำ = ฟีลกล้องเก่า / PS1

    RenderTexture rt;
    int index;
    float lastX, watch;
    public bool IsOpen { get; private set; }

    void Start()
    {
        rt = new RenderTexture(resolution.x, resolution.y, 16) { filterMode = FilterMode.Point, name = "CCTV_RT" };
        foreach (var c in cams) if (c) { c.targetTexture = rt; c.enabled = false; }
    }

    void OnDestroy() { if (rt) rt.Release(); }

    public void Toggle()
    {
        if (IsOpen) Close(); else Open();
    }

    void Open()
    {
        if (cams.Length == 0) return;
        IsOpen = true;
        var input = DormFX.Input;
        if (input) input.LockMovement(this);
        Show(index);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        if (cams[index]) cams[index].enabled = false;
        var input = DormFX.Input;
        if (input) input.UnlockMovement(this);
        DormOverlay.I.HideCctv();
    }

    void Show(int i)
    {
        if (cams[index]) cams[index].enabled = false;
        index = (i + cams.Length) % cams.Length;
        if (cams[index]) cams[index].enabled = true;
        watch = 0f;
        DormOverlay.I.ShowCctv(rt, Label());
    }

    string Label()
    {
        string l = index < labels.Length ? labels[index] : "CAM " + (index + 1);
        return l;
    }

    void Update()
    {
        if (!IsOpen) return;
        if (GameState.Ended) { Close(); return; }

        var input = DormFX.Input;
        float x = input ? input.Move.x : 0f;
        if (x > 0.5f && lastX <= 0.5f) Show(index + 1);
        else if (x < -0.5f && lastX >= -0.5f) Show(index - 1);
        lastX = x;

        var a = anomalies ? anomalies.OnFloor(index + 1) : null;
        if (a != null && !a.observed)
        {
            watch += Time.deltaTime;
            if (watch > 1.2f)
            {
                a.observed = true;
                DormFX.SetFlag("cctv_seen_n" + GameState.Night);
                DormFX.Msg("บันทึกภาพไว้แล้ว: " + a.desc, 5f);
                if (!DormFX.HasClue("c_cctv")) { Close(); DormFX.Discover("c_cctv"); }
            }
        }
        else watch = 0f;
    }
}
