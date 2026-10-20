using BlurredRoom;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// หน้า "วิธีเล่น" — กด H (หรือ Tab) ดูได้ตลอดเกม: ปุ่มกด / ผังตึก / ต้องทำอะไร / กฎเคาะประตู
// แก้ข้อความทุกส่วนได้ใน Inspector
public class HelpOverlay : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [Header("ปุ่มเปิด/ปิด")]
    public Key key = Key.H;
    public Key altKey = Key.Tab;
    [Tooltip("ข้อความเล็กมุมจอ บอกว่ากดอะไรดูวิธีเล่น (ว่าง = ไม่แสดง)")]
    public string cornerHint = "[H] วิธีเล่น";

    [Header("เนื้อหา")]
    public string title = "วิธีเล่น";
    [TextArea(6, 14)] public string controls =
        "<b>W A S D</b>  เดิน\n<b>Shift</b>  วิ่ง (เหนื่อยได้)\n<b>เมาส์</b>  มองรอบตัว\n<b>E</b>  โต้ตอบ · เคาะประตู · เปิด/ปิดประตู · คุย\n" +
        "<b>F</b>  เก็บของ\n<b>ค้าง Q</b>  ส่องตาแมว (ประตูที่มีตาแมว)\n<b>ค้าง R</b>  ทำพิธี\n<b>H / Tab</b>  เปิดหน้านี้";
    [TextArea(6, 14)] public string howTo =
        "<b>กลางวัน</b> — ทำความรู้จักผู้เช่า\n" +
        "1. ดู <b>ทะเบียนผู้เช่า</b> บนเคาน์เตอร์ล็อบบี้ ว่าห้องไหนมีคนอยู่\n" +
        "2. ไป <b>เคาะประตู</b> ห้องนั้น (กด E ที่ประตู) ผู้เช่าจะเปิดออกมาคุย\n" +
        "3. จำ <b>ลักษณะประจำตัว</b> ของแต่ละคนไว้ — กลางคืนต้องใช้\n" +
        "สัมภาษณ์ได้วันละ 4 คน (คุยซ้ำฟรี)\n\n" +
        "<b>ห้องที่ถูกขีดแดง</b> ในทะเบียน = คนหายไปอย่างปริศนา... อย่าไปเคาะ\n\n" +
        "<b>กลางคืน</b> — อยู่ในห้อง ปิดประตู มีคนมาเคาะ\nฟังจังหวะเคาะ + ส่องตาแมว ก่อนตัดสินใจเปิด";
    [TextArea(6, 14)] public string buildingMap =
        "<b>ชั้น 1</b>  ล็อบบี้ · เคาน์เตอร์ · ห้องออฟฟิศ · ห้องซักผ้า · ห้อง 101–107\n" +
        "<b>ชั้น 2</b>  ห้อง 201–212 · ห้องน้ำ · ห้องเก็บของ\n" +
        "<b>ชั้น 3</b>  ห้อง 301–312 · ห้องเก็บของ (ห้องพิธี)\n\n" +
        "บันไดอยู่ <b>หัวและท้ายทางเดิน</b> ทั้งสองฝั่ง\nเลขห้องตัวแรก = ชั้น (เช่น 203 = ชั้น 2)\nดูป้ายบนผนังตอนขึ้นบันไดแต่ละชั้น";

    Canvas canvas;
    TextMeshProUGUI corner;
    TextMeshProUGUI goal;

    void Start()
    {
        if (!string.IsNullOrEmpty(cornerHint))
        {
            var c = PaperUI.MakeCanvas("HelpHint_UI", 40);
            corner = PaperUI.Text("Hint", c.transform, new Vector2(0f, 0f), new Vector2(120, 26), new Vector2(220, 30), 18, TextAlignmentOptions.MidlineLeft, new Color(0.85f, 0.83f, 0.78f, 0.55f));
            corner.text = cornerHint;
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        bool toggle = kb[key].wasPressedThisFrame || (altKey != Key.None && kb[altKey].wasPressedThisFrame);
        if (IsOpen && kb.escapeKey.wasPressedThisFrame) toggle = true;
        if (toggle)
        {
            if (IsOpen) Close();
            else if (!DormFX.UIBusy && !GameState.Ended) Open();
        }
        if (corner) corner.enabled = !IsOpen && !OpeningCutscene.Playing;
    }

    void Open()
    {
        IsOpen = true;
        var input = DormFX.Input;
        if (input) input.LockMovement(this);
        canvas = PaperUI.MakeCanvas("Help_UI", 310);
        var root = canvas.transform;
        PaperUI.Fill("Dim", root, new Color(0, 0, 0, 0.72f));
        var paper = PaperUI.Img("Paper", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500, 900),
                                PaperUI.Paper ? Color.white : new Color(0.88f, 0.84f, 0.74f), PaperUI.Paper);
        var p = paper.transform;
        var t = PaperUI.Text("Title", p, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(1200, 60), 44, TextAlignmentOptions.Center, PaperUI.Ink);
        t.text = title; t.fontStyle = FontStyles.Bold;

        Section(p, "ปุ่มกด", controls, new Vector2(-690, -120), new Vector2(520, 420));
        Section(p, "ต้องทำอะไร", howTo, new Vector2(-120, -120), new Vector2(820, 430));
        Section(p, "ผังตึก", buildingMap, new Vector2(-120, -540), new Vector2(820, 300));

        // เป้าหมายตอนนี้ (ดึงจาก HUD)
        goal = PaperUI.Text("Goal", p, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(520, 260), 22, TextAlignmentOptions.TopLeft, PaperUI.RedPen);
        goal.rectTransform.pivot = new Vector2(0f, 1f); goal.rectTransform.anchoredPosition = new Vector2(-690, -580);
        goal.text = "<b>ตอนนี้</b>\n" + (GameHUD.Instance ? GameHUD.Instance.CurrentObjectivesText : "");

        var close = PaperUI.Text("Close", root, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(600, 40), 22, TextAlignmentOptions.Center, new Color(0.85f, 0.83f, 0.78f));
        close.text = "[H] ปิด";
    }

    static void Section(Transform p, string head, string body, Vector2 pos, Vector2 size)
    {
        var h = PaperUI.Text("Head", p, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(size.x, 44), 28, TextAlignmentOptions.TopLeft, PaperUI.Ink);
        h.text = head; h.fontStyle = FontStyles.Bold | FontStyles.Underline;
        h.rectTransform.pivot = new Vector2(0f, 1f); h.rectTransform.anchoredPosition = pos;
        var b = PaperUI.Text("Body", p, new Vector2(0.5f, 1f), pos, size, 23, TextAlignmentOptions.TopLeft, PaperUI.Ink);
        b.rectTransform.pivot = new Vector2(0f, 1f); b.rectTransform.anchoredPosition = pos + new Vector2(0, -50);
        b.text = body; b.lineSpacing = 4f;
    }

    void Close()
    {
        if (canvas) Destroy(canvas.gameObject);
        var input = DormFX.Input;
        if (input) input.UnlockMovement(this);
        IsOpen = false;
    }

    void OnDestroy() { if (IsOpen) { IsOpen = false; } }
}
