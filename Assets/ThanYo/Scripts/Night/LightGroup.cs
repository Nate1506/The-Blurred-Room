using System.Collections.Generic;
using UnityEngine;

// กลุ่มไฟที่เปิด/ปิดด้วยสวิตช์ (เช่น ไฟทางเดินชั้น 2) — ต่อจาก NightTrigger.onTriggered -> TurnOff()
// ถ้าผู้เล่นตายแล้วเกิดใหม่ก่อนขั้นที่ปิดไฟ ไฟกลุ่มนี้จะกลับมาติดเอง
public class LightGroup : MonoBehaviour
{
    public List<Light> lights = new List<Light>();
    [Tooltip("ขั้นที่ปิดไฟกลุ่มนี้ (ใช้ตอนเกิดใหม่: ถ้าขั้นนี้ทำไปแล้ว ไฟจะยังดับ)")]
    public string offAfterStep = "";
    public AudioClip switchClip;

    void Start() => NightDemo.Respawned += Sync;
    void OnDestroy() => NightDemo.Respawned -= Sync;

    void Sync()
    {
        if (!BuildingPower.I || string.IsNullOrEmpty(offAfterStep)) return;
        BuildingPower.I.SetManual(lights, !NightDemo.IsDone(offAfterStep));
    }

    [ContextMenu("ปิด (ทดสอบ)")]
    public void TurnOff()
    {
        if (BuildingPower.I) BuildingPower.I.SetManual(lights, false);
        else foreach (var l in lights) if (l) l.enabled = false;
        BlurredRoomPlay(switchClip ? switchClip : NightAudio.Click);
    }

    [ContextMenu("เปิด (ทดสอบ)")]
    public void TurnOn()
    {
        if (BuildingPower.I) BuildingPower.I.SetManual(lights, true);
        else foreach (var l in lights) if (l) l.enabled = true;
        BlurredRoomPlay(switchClip ? switchClip : NightAudio.Click);
    }

    void BlurredRoomPlay(AudioClip c) => DormFX.Play(c, transform.position, 0.8f);
}
