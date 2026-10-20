using System.Collections;
using System.Collections.Generic;
using BlurredRoom;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// กระดาษเช็คชื่อผู้เช่าบนเคาน์เตอร์ (กด E อ่าน)
//   ห้องที่มีคนอยู่ = ติ๊กถูก
//   ห้องคนหายปริศนา (+ ผู้เช่าที่หายระหว่างเกม) = ขีดเส้นแดงทับชื่อและเลขห้อง
// แก้รายชื่อห้องคนหายได้ที่ช่อง vanished ด้านล่าง (RoomKnock ของห้องนั้นต้องตั้งเป็น Vanished ด้วย — เมนู 12 ทำให้)
public class TenantRegister : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public class Row
    {
        public string room = "205";
        public string name = "";
        [Tooltip("หมายเหตุท้ายบรรทัด (ลายมือ)")]
        public string note = "";
    }

    public static bool IsOpen { get; private set; }

    [Header("หัวกระดาษ")]
    public string title = "ทะเบียนผู้เช่า — สุขนิรันดร์ แมนชั่น";
    public string subtitle = "เช็คชื่อประจำเดือน · ผู้ดูแลคนก่อน";
    [Tooltip("ข้อความลายมือท้ายกระดาษ")]
    [TextArea] public string footer = "ห้องที่ขีดแดง ไม่ต้องไปเคาะ... ไม่ว่าจะได้ยินอะไรก็ตาม";

    [Header("ห้องคนหายปริศนา (ขีดเส้นแดง)")]
    public List<Row> vanished = new List<Row>
    {
        new Row { room = "205", name = "สมศรี แก้วประเสริฐ", note = "ไม่ได้ย้ายออก" },
        new Row { room = "208", name = "วีระ ทองดี", note = "ของยังอยู่ในห้อง" },
        new Row { room = "303", name = "กิ่งแก้ว ศรีสุข", note = "?" },
        new Row { room = "304", name = "ประยูร มั่นคง", note = "ประตูล็อกจากข้างใน" },
        new Row { room = "309", name = "█████████", note = "ชื่อถูกขูดออก" },
    };

    [Header("ปุ่ม/ข้อความ")]
    public string prompt = "อ่านทะเบียนผู้เช่า";
    [Tooltip("ธงที่ตั้งเมื่ออ่านแล้ว (เป้าหมายบนจอใช้)")]
    public string readFlag = "read_register";

    Canvas canvas;
    float openedAt;

    public string Prompt => prompt;
    public bool CanInteract(GameObject interactor) => !IsOpen && !DormFX.UIBusy;
    public void Interact(GameObject interactor) { if (!IsOpen) StartCoroutine(Show()); }

    void Awake()
    {
        if (!GetComponentInChildren<Collider>()) { var b = gameObject.AddComponent<BoxCollider>(); b.size = new Vector3(0.35f, 0.05f, 0.45f); }
    }

    IEnumerator Show()
    {
        IsOpen = true;
        GameState.Set(readFlag);
        var input = DormFX.Input;
        if (input) input.LockMovement(this);
        Build();
        openedAt = Time.unscaledTime;
        yield return new WaitForSecondsRealtime(0.35f);
        while (true)
        {
            bool close = input && (input.InteractPressed || input.PickupPressed);
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) close = true;
            if (close) break;
            yield return null;
        }
        if (canvas) Destroy(canvas.gameObject);
        yield return null;
        if (input) input.UnlockMovement(this);
        IsOpen = false;
    }

    // ---------------------------------------------------------------------
    void Build()
    {
        canvas = PaperUI.MakeCanvas("Register_UI", 300);
        var root = canvas.transform;
        PaperUI.Fill("Dim", root, new Color(0, 0, 0, 0.6f));
        var paper = PaperUI.Img("Paper", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 980),
                                PaperUI.Paper ? Color.white : new Color(0.88f, 0.84f, 0.74f), PaperUI.Paper);
        paper.rectTransform.localRotation = Quaternion.Euler(0, 0, -1.2f);
        var p = paper.transform;
        var t = PaperUI.Text("Title", p, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(800, 60), 40, TextAlignmentOptions.Center, PaperUI.Ink);
        t.text = title; t.fontStyle = FontStyles.Bold;
        var st = PaperUI.Text("Sub", p, new Vector2(0.5f, 1f), new Vector2(0, -115), new Vector2(800, 36), 22, TextAlignmentOptions.Center, PaperUI.InkSoft);
        st.text = subtitle;

        // หัวตาราง
        float y = -175f;
        Col(p, "ห้อง", 0, y, 22, true); Col(p, "ชื่อผู้เช่า", 1, y, 22, true); Col(p, "หมายเหตุ", 2, y, 22, true);
        PaperUI.Img("HeadLine", p, new Vector2(0f, 1f), new Vector2(450, y - 24), new Vector2(780, 2), PaperUI.InkSoft);
        y -= 58f;

        // รวมทุกห้อง เรียงเลขห้อง
        var rows = new List<(string room, string name, string note, bool gone)>();
        foreach (var tn in DormData.Tenants)
        {
            bool gone = GameState.Missing.Contains(tn.id);
            rows.Add((tn.room, tn.name, gone ? "หายตัวไป" : "", gone));
        }
        foreach (var v in vanished) rows.Add((v.room, v.name, v.note, true));
        rows.Sort((a, b) => string.CompareOrdinal(a.room, b.room));

        foreach (var r in rows)
        {
            // ช่องติ๊ก
            var box = PaperUI.Img("Box", p, new Vector2(0f, 1f), new Vector2(70, y), new Vector2(26, 26), PaperUI.Ink);
            PaperUI.Img("BoxIn", box.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(21, 21), new Color(0.9f, 0.86f, 0.77f));
            if (!r.gone)
            {
                var tick = PaperUI.Img("Tick", box.transform, new Vector2(0.5f, 0.5f), new Vector2(3, 4), new Vector2(36, 36), PaperUI.Ink);
                if (PaperUI.Check) { tick.sprite = PaperUI.Check; tick.type = Image.Type.Simple; }
                tick.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(-8f, 8f));
            }
            Col(p, r.room, 0, y, 28, false);
            Col(p, r.name, 1, y, 28, false);
            var n = Col(p, r.note, 2, y, 20, false); n.fontStyle = FontStyles.Italic; n.color = r.gone ? PaperUI.RedPen : PaperUI.InkSoft;

            if (r.gone)
            {
                // เส้นแดงลายมือขีดทับเลขห้อง + ชื่อ
                float len = 150f + Mathf.Min(330f, r.name.Length * 15f);
                var line = PaperUI.Img("RedLine", p, new Vector2(0f, 1f), new Vector2(105 + len * 0.5f, y + Random.Range(-2f, 3f)), new Vector2(len, 3.5f), PaperUI.RedPen);
                line.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(-2.2f, 2.2f));
                var line2 = PaperUI.Img("RedLine2", p, new Vector2(0f, 1f), new Vector2(105 + len * 0.45f, y - 4f), new Vector2(len * 0.85f, 2f), new Color(PaperUI.RedPen.r, PaperUI.RedPen.g, PaperUI.RedPen.b, 0.55f));
                line2.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(-1.5f, 1.5f));
            }
            y -= 54f;
        }

        var f = PaperUI.Text("Footer", p, new Vector2(0.5f, 0f), new Vector2(0, 115), new Vector2(760, 70), 22, TextAlignmentOptions.Center, PaperUI.RedPen);
        f.text = footer; f.fontStyle = FontStyles.Italic;
        f.rectTransform.localRotation = Quaternion.Euler(0, 0, 1.5f);
        var hint = PaperUI.Text("Close", root, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(600, 40), 22, TextAlignmentOptions.Center, new Color(0.85f, 0.83f, 0.78f));
        hint.text = "[E] ปิด";
    }

    // คอลัมน์: 0 = ห้อง, 1 = ชื่อ, 2 = หมายเหตุ
    static readonly float[] colLeft = { 110f, 250f, 610f };
    static readonly float[] colWidth = { 120f, 350f, 250f };

    static TextMeshProUGUI Col(Transform p, string text, int col, float y, float size, bool head)
    {
        float w = colWidth[col];
        var t = PaperUI.Text("Cell", p, new Vector2(0f, 1f), new Vector2(colLeft[col] + w * 0.5f, y), new Vector2(w, 44), size,
                             TextAlignmentOptions.MidlineLeft, head ? PaperUI.InkSoft : PaperUI.Ink);
        t.text = text;
        if (head) t.fontStyle = FontStyles.Bold;
        return t;
    }
}
