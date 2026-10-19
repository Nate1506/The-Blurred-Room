using BlurredRoom;
using UnityEngine;

// "สิ่งผิดปกติ" ที่ปลอมตัวเป็นผู้เช่ามาเคาะประตู: ผ่านตาแมวดูเหมือนคนนั้นทุกอย่าง (ยกเว้นร่องรอย)
// ถ้าผู้เล่นเปิดประตู -> Reveal() เผยร่างจริง (ผอมสูง ตาแดง)
public class FakeVisitor : NpcBase
{
    GameObject disguise, creature;

    public override NpcKind Kind => NpcKind.Neutral;

    protected override void Awake()
    {
        base.Awake();
        creature = DormFX.BuildCreature(transform);
        creature.SetActive(false);
    }

    public void Disguise(TenantInfo t, GameObject model = null)
    {
        if (disguise) Destroy(disguise);
        disguise = model ? DormFX.SpawnModel(model, transform) : DormFX.BuildPerson(transform, t);
        disguise.SetActive(false);
    }

    public void ShowAtDoor(Door door)
    {
        DormFX.PlaceAtDoor(transform, door);
        if (disguise) disguise.SetActive(true);
        creature.SetActive(false);
    }

    public void PlayTalk() => DormFX.PlayTalk(disguise);

    public void Reveal()
    {
        if (disguise) disguise.SetActive(false);
        creature.SetActive(true);
    }

    public void Hide()
    {
        if (disguise) disguise.SetActive(false);
        creature.SetActive(false);
    }

    public override void OnDoorAnswered(Door door, DoorAnswer answer) { }   // StoryDirector จัดการเอง
}
