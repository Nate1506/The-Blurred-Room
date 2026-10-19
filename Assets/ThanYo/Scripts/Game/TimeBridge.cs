using BlurredRoom;
using UnityEngine;

// ตัวเชื่อม: เวลาของเนท (ตรรกะเกม) -> ภาพของเรา (แสง ฟ้า หมอก)
// ไม่แก้โค้ดเนท แค่ฟัง event PhaseChanged แล้วสั่ง TimeOfDayController
public class TimeBridge : MonoBehaviour
{
    public TimeOfDayController visuals;

    TimeOfDayManager time;

    void Start()
    {
        time = TimeOfDayManager.Instance;
        if (!visuals) visuals = FindFirstObjectByType<TimeOfDayController>();
        if (!time || !visuals) { enabled = false; return; }
        time.PhaseChanged += Apply;
        Apply(time.Phase);
    }

    void OnDestroy()
    {
        if (time) time.PhaseChanged -= Apply;
    }

    void Apply(DayPhase phase)
    {
        switch (phase)
        {
            case DayPhase.Day: visuals.SetTimeOfDay(TimeOfDay.Morning); break;
            case DayPhase.Eve: visuals.SetTimeOfDay(TimeOfDay.Evening); break;
            default: visuals.SetTimeOfDay(TimeOfDay.Night); break;
        }
    }
}
