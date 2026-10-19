using System.Collections.Generic;
using BlurredRoom;
using UnityEngine;

// ความจำของเกมที่ต้องข้ามซีน (Inside <-> ThanYo): คืนที่เท่าไร ช่วงเวลา เบาะแส ไอเทม ธง ค่าสติ จุดเกิด
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

    public static bool Has(string flag) => Flags.Contains(flag);
    public static void Set(string flag) { if (!string.IsNullOrEmpty(flag)) Flags.Add(flag); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Night = 1; Phase = DayPhase.Day;
        Flags.Clear(); TakenPickups.Clear(); Items.Clear(); Clues.Clear();
        Sanity = -1f; SpawnId = ""; Ended = false;
    }
}
