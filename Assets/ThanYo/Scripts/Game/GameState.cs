using System.Collections.Generic;
using BlurredRoom;
using UnityEngine;

// ความจำของเกมที่ต้องข้ามซีน (Inside <-> ThanYo): คืนที่เท่าไร ช่วงเวลา เบาะแส ไอเทม ธง ค่าสติ จุดเกิด
// + ข้อมูลระบบหอพัก: ผู้เช่าที่หาย, คนที่สัมภาษณ์แล้ว, แอ็กชันที่ใช้วันนี้, ความจริงที่ปลดแล้ว
// static = อยู่ตลอดตอนเกมเปิด เปลี่ยนซีนไม่หาย (กด Play ใหม่จะรีเซ็ต)
public static class GameState
{
    public static int Night = 1;
    public static DayPhase Phase = DayPhase.Day;
    public static readonly HashSet<string> Flags = new HashSet<string>();
    public static readonly HashSet<string> TakenPickups = new HashSet<string>();   // ไอเทม/เบาะแสที่เก็บแล้ว ไม่ให้เกิดซ้ำ
    public static readonly List<string> Items = new List<string>();
    public static readonly List<ClueData> Clues = new List<ClueData>();
    public static float Sanity = -1f;          // -1 = ยังไม่เคยบันทึก (ใช้ค่าเต็ม)
    public static string SpawnId = "";
    public static bool Ended;

    // ---- ระบบหอพัก (ดู DormData) ----
    public static readonly HashSet<string> Missing = new HashSet<string>();       // id ผู้เช่าที่หายไปแล้ว (กลับมาไม่ได้)
    public static readonly List<string> MissingLog = new List<string>();          // หายเมื่อคืน -> รายงานตอนเช้า
    public static readonly HashSet<string> Interviewed = new HashSet<string>();   // สัมภาษณ์ครั้งแรกแล้ว
    public static readonly HashSet<string> Truths = new HashSet<string>();        // ความจริงบนกระดานที่ปลดแล้ว
    public static readonly List<int> RealVisitOrder = new List<int>();            // ลำดับผู้เช่าตัวจริงที่จะมาเคาะ (สุ่มครั้งเดียว)
    public static int ActionsUsed;                                                 // แอ็กชันกลางวันที่ใช้ไปแล้ววันนี้
    public static string Night2Room = "";                                          // ห้องปลอดภัยคืนที่ 2 (พิธีบอก)

    public static bool Has(string flag) => Flags.Contains(flag);
    public static void Set(string flag) { if (!string.IsNullOrEmpty(flag)) Flags.Add(flag); }

    /// <summary>เริ่มเกมใหม่จากเมนูหลัก (ล้างทุกอย่าง)</summary>
    public static void NewGame() => Reset();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Night = 1; Phase = DayPhase.Day;
        Flags.Clear(); TakenPickups.Clear(); Items.Clear(); Clues.Clear();
        Sanity = -1f; SpawnId = ""; Ended = false;
        Missing.Clear(); MissingLog.Clear(); Interviewed.Clear(); Truths.Clear(); RealVisitOrder.Clear();
        ActionsUsed = 0; Night2Room = "";
    }
}
