using System.Collections;
using System.Collections.Generic;
using BlurredRoom;
using UnityEngine;

// กฎเคาะประตู (จาก vault):
//   เคาะ 3                       = ปกติ
//   เคาะ 3 -> เว้น 2 วิ -> อีก 1     = ปกติ (รวม 4)
//   เคาะ 3 -> เว้น 2 วิ -> อีก 2     = ผิดปกติ (รวม 5)
//   เคาะ 3 -> เว้น 2 วิ -> อีก 3+    = ผิดปกติ (รวม 6+)
//   เคาะรัวไม่เกิน 2 วิ              = ปกติ
//   เคาะรัวเกิน 2 วิ                 = ผิดปกติ
//   ความดัง                         = ไม่มีผล (ตัวหลอกให้ตกใจเฉยๆ)
public enum KnockStyle { Three, ThreePlusOne, ThreePlusTwo, ThreePlusMany, RapidShort, RapidLong }

public static class KnockRules
{
    public const string RulesText =
        "ฟังจังหวะเคาะให้ดี ก่อนจะเปิดประตูทุกครั้ง\n\n" +
        "ปกติ:\n" +
        "- เคาะ 3 ครั้ง\n" +
        "- เคาะ 3 ครั้ง เว้น 2 วิ แล้วเคาะอีก 1 ครั้ง\n" +
        "- เคาะรัวๆ ไม่เกิน 2 วินาที\n\n" +
        "ผิดปกติ:\n" +
        "- เคาะ 3 ครั้ง เว้น 2 วิ แล้วเคาะอีก 2 ครั้งขึ้นไป\n" +
        "- เคาะรัวๆ นานเกิน 2 วินาที\n\n" +
        "ความดังไม่เกี่ยว อย่าตกใจ\n\n" +
        "ตาแมว [ค้าง Q]: คนจริงทิ้งร่องรอย สิ่งปลอมไม่ทิ้ง\n" +
        "ร่องรอยต้องตรงกับคำพูด และตรงกับลักษณะประจำตัวของคนนั้น\n\n" +
        "เปิดให้สิ่งผิดปกติ = สติ -20\n" +
        "ไม่เปิดให้คนจริง = ผู้เช่าคนนั้นจะหายไป";

    public static bool IsNormal(KnockStyle s) => s == KnockStyle.Three || s == KnockStyle.ThreePlusOne || s == KnockStyle.RapidShort;

    static readonly KnockStyle[] normal = { KnockStyle.Three, KnockStyle.ThreePlusOne, KnockStyle.RapidShort };
    static readonly KnockStyle[] abnormal = { KnockStyle.ThreePlusTwo, KnockStyle.ThreePlusMany, KnockStyle.RapidLong };
    public static KnockStyle RandomNormal() => normal[Random.Range(0, normal.Length)];
    public static KnockStyle RandomAbnormal() => abnormal[Random.Range(0, abnormal.Length)];

    public struct Seg { public int count; public float interval, pauseAfter; }

    /// <summary>แตกจังหวะเป็นช่วงๆ: ช่วงละ (จำนวนครั้ง, ระยะห่าง, เว้นหลังจบช่วง)</summary>
    public static List<Seg> Segments(KnockStyle s)
    {
        var l = new List<Seg>();
        void Add(int c, float i, float p) => l.Add(new Seg { count = c, interval = i, pauseAfter = p });
        switch (s)
        {
            case KnockStyle.Three: Add(3, 0.55f, 0f); break;
            case KnockStyle.ThreePlusOne: Add(3, 0.55f, 2f); Add(1, 0.5f, 0f); break;
            case KnockStyle.ThreePlusTwo: Add(3, 0.55f, 2f); Add(2, 0.5f, 0f); break;
            case KnockStyle.ThreePlusMany: Add(3, 0.55f, 2f); Add(Random.Range(3, 6), 0.45f, 0f); break;
            case KnockStyle.RapidShort: Add(Mathf.RoundToInt(Random.Range(1.0f, 1.7f) / 0.11f) + 1, 0.11f, 0f); break;
            case KnockStyle.RapidLong: Add(Mathf.RoundToInt(Random.Range(3.0f, 4.2f) / 0.11f) + 1, 0.11f, 0f); break;
        }
        return l;
    }

    /// <summary>
    /// ให้ npc เคาะประตู door ตามจังหวะ s ผ่านระบบ Door ของเนท (BeginVisit / KnockAgain)
    /// จบเมื่อเคาะครบ (ผู้เล่นยังมีเวลาตัดสินใจอีก answerWindow วิ) หรือผู้เล่นเปิดประตูกลางคัน
    /// </summary>
    public static IEnumerator Perform(Door door, NpcBase npc, KnockStyle s, bool loud, float answerWindow, System.Action onFirstKnock)
    {
        var segs = Segments(s);
        void OnKnock(int i, int total) => DormFX.Knock(door.transform.position, loud);
        door.Knocked += OnKnock;

        for (int k = 0; k < segs.Count; k++)
        {
            Seg sg = segs[k];
            bool last = k == segs.Count - 1;
            var pattern = new KnockPattern { knocks = sg.count, interval = sg.interval, answerWindow = last ? answerWindow : 9999f };

            if (k == 0)
            {
                if (!door.BeginVisit(npc, pattern)) break;   // ประตูเปิดอยู่ / มีคนอื่นเคาะอยู่
                onFirstKnock?.Invoke();
            }
            else
            {
                if (!door.HasPendingVisitor) break;
                door.KnockAgain(pattern);
            }

            float dur = (sg.count - 1) * sg.interval + 0.1f + (last ? 0f : sg.pauseAfter);
            for (float t = 0f; t < dur && door.HasPendingVisitor; t += Time.deltaTime) yield return null;
            if (!door.HasPendingVisitor) break;               // ผู้เล่นเปิดประตูแล้ว
        }

        door.Knocked -= OnKnock;
    }
}
