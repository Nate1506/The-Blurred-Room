using System.Collections;
using System.Collections.Generic;
using BlurredRoom;
using UnityEngine;

// ต่อเสียงจริง (SoundBank) เข้ากับระบบของเนทโดยไม่แก้โค้ดเนท:
//   - ประตูทุกบาน: เปิด / ปิด
//   - สเตมิน่าหมด: หอบ (ค่อยๆ เงียบตอนหายเหนื่อย)
//   - ลาง: เสียงครางไกลๆ สุ่มเป็นระยะระหว่างทำเควส
// วางไว้ 1 ตัวต่อซีน
public class AudioHooks : MonoBehaviour
{
    [Header("ประตู")]
    public bool doorSounds = true;

    [Header("หอบตอนสเตมิน่าหมด")]
    public bool staminaBreath = true;
    [Tooltip("เวลาค่อยๆ เงียบหลังหายเหนื่อย (วินาที)")]
    [Min(0.1f)] public float breathFadeOut = 2.5f;

    [Header("ลาง (เสียงครางไกลๆ)")]
    public bool omen = true;
    [Tooltip("เกิดทุกๆ X-Y วินาที")]
    public Vector2 omenEvery = new Vector2(45f, 90f);
    [Tooltip("ครั้งแรกหลังเข้าซีนกี่วินาที")]
    [Min(0f)] public float omenFirstDelay = 30f;
    [Tooltip("ระยะห่างจากผู้เล่น (เมตร)")]
    public Vector2 omenDistance = new Vector2(10f, 16f);
    [Range(0f, 1f)] public float omenVolume = 0.35f;
    [Tooltip("เกิดเฉพาะตอนมีเควส (เป้าหมายบนจอ) อยู่")]
    public bool omenOnlyDuringQuests = true;

    AudioSource breath;
    PlayerStamina stamina;
    bool exhausted;

    IEnumerator Start()
    {
        var bank = SoundBank.I;
        if (doorSounds && bank)
            foreach (var d in FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var door = d;
                door.Opened += () => PlayAt(bank.doorOpen, door.transform.position, bank.doorVolume);
                door.Closed += () => PlayAt(bank.doorClose, door.transform.position, bank.doorVolume);
            }

        stamina = FindFirstObjectByType<PlayerStamina>();
        if (staminaBreath && stamina && bank && bank.staminaBreath)
        {
            breath = gameObject.AddComponent<AudioSource>();
            breath.clip = bank.staminaBreath; breath.loop = true; breath.spatialBlend = 0f; breath.volume = 0f;
            stamina.Exhausted += () => exhausted = true;
            stamina.Recovered += () => exhausted = false;
        }

        if (omen && bank && bank.omen) StartCoroutine(Omens(bank));
        yield break;
    }

    void Update()
    {
        if (!breath) return;
        var bank = SoundBank.I;
        float target = exhausted ? bank.breathVolume : 0f;
        breath.volume = Mathf.MoveTowards(breath.volume, target, Time.deltaTime * (exhausted ? 3f : bank.breathVolume / breathFadeOut));
        if (breath.volume > 0.001f && !breath.isPlaying) { breath.time = 0f; breath.Play(); }
        else if (breath.volume <= 0.001f && breath.isPlaying) breath.Stop();
    }

    IEnumerator Omens(SoundBank bank)
    {
        yield return new WaitForSeconds(omenFirstDelay);
        while (true)
        {
            var p = DormFX.Player;
            bool quest = !omenOnlyDuringQuests || HasQuest();
            if (p && quest && !GameState.Ended)
            {
                Vector3 dir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
                Vector3 at = p.position + dir * Random.Range(omenDistance.x, omenDistance.y) + Vector3.up * Random.Range(0f, 3f);
                var go = new GameObject("Omen");
                go.transform.position = at;
                var s = go.AddComponent<AudioSource>();
                s.clip = bank.omen; s.spatialBlend = 1f; s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 4f; s.maxDistance = 30f;
                s.volume = omenVolume; s.pitch = Random.Range(0.85f, 1.05f); s.dopplerLevel = 0f;
                s.Play();
                Destroy(go, bank.omen.length / s.pitch + 0.2f);
            }
            yield return new WaitForSeconds(Random.Range(omenEvery.x, omenEvery.y));
        }
    }

    static bool HasQuest()
    {
        if (NightDemo.I && NightDemo.I.Current != null) return true;
        return StoryDirector.Instance && StoryDirector.Instance.isActiveAndEnabled;
    }

    static void PlayAt(AudioClip clip, Vector3 pos, float vol)
    {
        if (!clip) return;
        var go = new GameObject("DoorSFX");
        go.transform.position = pos + Vector3.up * 1.1f;
        var s = go.AddComponent<AudioSource>();
        s.clip = clip; s.spatialBlend = 1f; s.minDistance = 1.5f; s.maxDistance = 20f; s.volume = vol; s.pitch = Random.Range(0.94f, 1.05f);
        s.Play();
        Object.Destroy(go, clip.length + 0.2f);
    }
}
