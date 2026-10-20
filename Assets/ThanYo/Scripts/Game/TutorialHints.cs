using System.Collections.Generic;
using BlurredRoom;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ป๊อปอัปสอนเล่น — ขึ้นครั้งเดียวต่อเกม เมื่อเจอสถานการณ์นั้นครั้งแรก
// เพิ่ม/แก้/ลบได้ในรายการ hints (เลือกเงื่อนไข + ข้อความ)
public class TutorialHints : MonoBehaviour
{
    public enum Trigger
    {
        SceneStart,          // เข้าซีนนี้ครั้งแรก
        DayStart,            // เป็นกลางวัน
        LookAtRegister,      // มองกระดาษทะเบียนผู้เช่า
        LookAtKnockDoor,     // มองประตูห้องที่เคาะได้
        LookAtTenant,        // มองผู้เช่าที่ออกมาคุย
        AfterFirstKnock,     // เคาะประตูไปแล้วครั้งแรก
        LookAtPeepholeDoor,  // มองประตูที่มีตาแมว (กลางคืน)
        EveStart,            // ตกเย็น
        NightStart,          // กลางคืน
        Running,             // กดวิ่งครั้งแรก
        Flag,                // มีธงเนื้อเรื่องตามช่อง flag
    }

    [System.Serializable]
    public class Hint
    {
        public string id = "hint";
        public Trigger when = Trigger.SceneStart;
        [Tooltip("ใช้กับ Trigger = Flag")]
        public string flag = "";
        [Tooltip("ปุ่มที่เกี่ยว (ขึ้นเป็นกรอบปุ่ม) ว่าง = ไม่มี")]
        public string key = "";
        [TextArea(2, 4)] public string text = "";
        [Min(1f)] public float seconds = 6f;
        [Tooltip("รอกี่วินาทีหลังเงื่อนไขเป็นจริง")]
        [Min(0f)] public float delay = 0.3f;
    }

    [Tooltip("ปิดทั้งหมด")]
    public bool enabledHints = true;
    public List<Hint> hints = new List<Hint>
    {
        new Hint { id = "move", when = Trigger.SceneStart, key = "WASD", text = "เดิน  ·  <b>Shift</b> วิ่ง  ·  <b>เมาส์</b> มอง  ·  <b>H</b> ดูวิธีเล่นได้ตลอด", seconds = 7f, delay = 2.5f },
        new Hint { id = "register", when = Trigger.DayStart, key = "", text = "เริ่มจาก <b>ทะเบียนผู้เช่า</b> บนเคาน์เตอร์ล็อบบี้ — ดูว่าห้องไหนมีคนอยู่", seconds = 7f, delay = 10f },
        new Hint { id = "register_look", when = Trigger.LookAtRegister, key = "E", text = "อ่านทะเบียนผู้เช่า", seconds = 4f },
        new Hint { id = "knock", when = Trigger.LookAtKnockDoor, key = "E", text = "เคาะประตู — ถ้ามีคนอยู่ เขาจะเปิดออกมาคุย\nเลขห้องตัวแรก = ชั้น (203 = ชั้น 2)", seconds = 6f },
        new Hint { id = "tenant", when = Trigger.LookAtTenant, key = "", text = "ฟังให้ดี แล้วจำ <b>ลักษณะประจำตัว</b> ของเขาไว้ — กลางคืนต้องใช้แยกคนจริงกับของปลอม", seconds = 6f },
        new Hint { id = "run", when = Trigger.Running, key = "Shift", text = "วิ่งนานๆ จะหอบ — ปล่อยแล้วค่อยๆ หายเหนื่อย", seconds = 4f, delay = 3f },
        new Hint { id = "eve", when = Trigger.EveStart, key = "", text = "ตกเย็นแล้ว ผู้เช่าไม่เปิดประตูแล้ว — ทำตามเป้าหมายมุมขวาบน", seconds = 6f, delay = 1.5f },
        new Hint { id = "peep", when = Trigger.LookAtPeepholeDoor, key = "Q", text = "ค้างไว้เพื่อส่องตาแมว — ดูก่อนตัดสินใจเปิดทุกครั้ง", seconds = 6f },
    };

    readonly HashSet<string> shownThisSession = new HashSet<string>();
    Canvas canvas;
    CanvasGroup group;
    TextMeshProUGUI keyText, bodyText;
    GameObject keyBox;
    Hint showing;
    float showUntil, pendingAt = -1f;
    Hint pending;
    PlayerInteractor interactor;

    static string Flag(Hint h) => "tut_" + h.id;

    void Start()
    {
        interactor = FindFirstObjectByType<PlayerInteractor>();
        canvas = PaperUI.MakeCanvas("Tutorial_UI", 60);
        var card = PaperUI.Img("Card", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0, 190), new Vector2(980, 96), new Color(0.06f, 0.05f, 0.05f, 0.82f));
        group = card.gameObject.AddComponent<CanvasGroup>(); group.alpha = 0f;
        keyBox = PaperUI.Img("Key", card.transform, new Vector2(0f, 0.5f), new Vector2(70, 0), new Vector2(96, 54), new Color(0.9f, 0.88f, 0.82f), PaperUI.KeyCap).gameObject;
        keyText = PaperUI.Text("KeyText", keyBox.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96, 54), 24, TextAlignmentOptions.Center, new Color(0.1f, 0.08f, 0.06f));
        keyText.fontStyle = FontStyles.Bold;
        bodyText = PaperUI.Text("Body", card.transform, new Vector2(0f, 0.5f), new Vector2(560, 0), new Vector2(800, 90), 25, TextAlignmentOptions.MidlineLeft, new Color(0.93f, 0.91f, 0.86f));
    }

    void Update()
    {
        if (!enabledHints || canvas == null) return;
        bool hidden = OpeningCutscene.Playing || DormFX.UIBusy || GameState.Ended;
        group.alpha = Mathf.MoveTowards(group.alpha, showing != null && Time.time < showUntil && !hidden ? 1f : 0f, Time.deltaTime * 3f);
        if (showing != null && Time.time >= showUntil && group.alpha <= 0f) showing = null;

        if (hidden) return;
        if (pending != null)
        {
            if (Time.time >= pendingAt && showing == null) { Show(pending); pending = null; }
            return;
        }
        if (showing != null) return;
        foreach (var h in hints)
        {
            if (h == null || GameState.Has(Flag(h)) || shownThisSession.Contains(h.id)) continue;
            if (!Met(h)) continue;
            pending = h; pendingAt = Time.time + h.delay;
            shownThisSession.Add(h.id);
            GameState.Set(Flag(h));
            break;
        }
    }

    bool Met(Hint h)
    {
        var focus = interactor ? interactor.Focus : null;
        switch (h.when)
        {
            case Trigger.SceneStart: return true;
            case Trigger.DayStart: return DormFX.Phase == DayPhase.Day;
            case Trigger.EveStart: return DormFX.Phase == DayPhase.Eve;
            case Trigger.NightStart: return DormFX.Phase == DayPhase.Night;
            case Trigger.LookAtRegister: return focus is TenantRegister;
            case Trigger.LookAtKnockDoor: return focus is Door d && d.IsLocked && !d.IsOpen && d.GetComponent<RoomKnock>();
            case Trigger.LookAtTenant: return focus is TenantNpc;
            case Trigger.AfterFirstKnock: return GameState.Has("knocked_any");
            case Trigger.LookAtPeepholeDoor: return focus is Door pd && pd.HasPeephole && !pd.IsOpen && DormFX.Phase == DayPhase.Night;
            case Trigger.Running: return DormFX.Input && DormFX.Input.RunHeld && DormFX.Input.Move.sqrMagnitude > 0.1f;
            case Trigger.Flag: return !string.IsNullOrEmpty(h.flag) && GameState.Has(h.flag);
        }
        return false;
    }

    void Show(Hint h)
    {
        showing = h;
        showUntil = Time.time + h.seconds;
        keyBox.SetActive(!string.IsNullOrEmpty(h.key));
        keyText.text = h.key;
        var r = keyBox.GetComponent<RectTransform>();
        r.sizeDelta = new Vector2(Mathf.Max(54f, 24f + h.key.Length * 18f), 54f);
        r.anchoredPosition = new Vector2(30f + r.sizeDelta.x * 0.5f, 0f);
        keyText.rectTransform.sizeDelta = r.sizeDelta;
        float left = string.IsNullOrEmpty(h.key) ? 40f : 60f + r.sizeDelta.x;
        bodyText.rectTransform.anchoredPosition = new Vector2(left + 400f, 0f);
        bodyText.text = h.text;
    }

    [ContextMenu("ล้างประวัติ (ให้ขึ้นใหม่ทั้งหมด)")]
    void ResetAll()
    {
        shownThisSession.Clear();
        foreach (var h in hints) GameState.Flags.Remove(Flag(h));
    }
}
