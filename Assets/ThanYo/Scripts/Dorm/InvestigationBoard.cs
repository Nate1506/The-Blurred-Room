using System.Collections;
using System.Collections.Generic;
using System.Text;
using BlurredRoom;
using UnityEngine;

// กระดานสืบสวนในห้องออฟฟิศ
// ตอนเย็น: "ต่อเบาะแส" -> ปลดความจริงที่เบาะแสครบ (ทีละชิ้น) + ตั้งธง board_nX (ต้องทำก่อนพิธี)
// เวลาอื่น: เปิดดูสรุป (ความจริง, ร่องรอยผู้เช่า, กฎเคาะ) ได้ฟรี
public class InvestigationBoard : MonoBehaviour, IInteractable
{
    bool busy;

    static string FlagTonight => "board_n" + GameState.Night;

    public string Prompt => DormFX.Phase == DayPhase.Eve && !GameState.Has(FlagTonight) ? "ต่อเบาะแสบนกระดาน" : "ดูกระดานสืบสวน";

    public bool CanInteract(GameObject interactor) => !busy && !DormFX.UIBusy && Time.time - DormOverlay.I.LastClosed > 0.4f;

    public void Interact(GameObject interactor) => StartCoroutine(Use());

    IEnumerator Use()
    {
        busy = true;
        if (DormFX.Phase == DayPhase.Eve && !GameState.Has(FlagTonight))
        {
            DormFX.SetFlag(FlagTonight);
            var found = new List<TruthInfo>();
            foreach (var t in DormData.Truths)
                if (!GameState.Truths.Contains(t.id) && Count(t) >= t.need) { GameState.Truths.Add(t.id); found.Add(t); }

            if (found.Count == 0)
                yield return DormOverlay.I.ShowDocument("กระดานสืบสวน", "เบาะแสยังต่อกันไม่ติด...\n\nพรุ่งนี้ต้องสัมภาษณ์ / สำรวจเพิ่มอีก");
            foreach (var t in found)
                yield return DormOverlay.I.ShowDocument("ความจริงที่ปลดล็อก: " + t.title, t.body);
        }
        yield return DormOverlay.I.ShowDocument("กระดานสืบสวน", Summary());
        string log = ClueLog();
        if (log.Length > 0) yield return DormOverlay.I.ShowDocument("บันทึกเบาะแส", log);
        busy = false;
    }

    static int Count(TruthInfo t)
    {
        int n = 0;
        foreach (var c in t.clues) if (DormFX.HasClue(c)) n++;
        return n;
    }

    static string ClueLog()
    {
        var sb = new StringBuilder();
        foreach (var c in DormData.Clues)
            if (DormFX.HasClue(c.id)) sb.Append("<b>").Append(c.title).Append("</b>\n").Append(c.body).Append("\n\n");
        return sb.ToString();
    }

    public static string Summary()
    {
        var sb = new StringBuilder();
        sb.Append("<b>ความจริง ").Append(GameState.Truths.Count).Append("/").Append(DormData.Truths.Length).Append("</b>\n");
        foreach (var t in DormData.Truths)
        {
            bool got = GameState.Truths.Contains(t.id);
            sb.Append(got ? "[/] " : "[  ] ").Append(got ? t.title : "???  (เบาะแส " + Count(t) + "/" + t.need + ")").Append('\n');
        }

        sb.Append("\n<b>ผู้เช่า</b>  (หายไป ").Append(GameState.Missing.Count).Append("/6)\n");
        foreach (var t in DormData.Tenants)
        {
            sb.Append("- ").Append(t.name).Append(" ห้อง ").Append(t.room).Append(": ");
            if (GameState.Missing.Contains(t.id)) sb.Append("<color=#7a2a1a>หายตัวไปแล้ว</color>");
            else if (GameState.Interviewed.Contains(t.id)) sb.Append(string.Join(", ", t.traces));
            else sb.Append("ยังไม่ได้คุย");
            sb.Append('\n');
        }

        sb.Append("\n<b>กฎเคาะ</b>: ปกติ = 3 / 3 เว้น 1 / รัวไม่เกิน 2 วิ  ·  ผิดปกติ = 3 เว้น 2+ / รัวเกิน 2 วิ  ·  ความดังไม่เกี่ยว");
        return sb.ToString();
    }
}
