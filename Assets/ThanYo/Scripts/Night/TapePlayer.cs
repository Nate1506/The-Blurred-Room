using System.Collections;
using BlurredRoom;
using UnityEngine;
using UnityEngine.Events;

// เทปที่พี่บิ๊กอัดทิ้งไว้ (กด E ที่เครื่องเล่น) — เล่นเป็นซับไตเติลทีละบรรทัด แล้วจบด้วยกระดาษกฎสั้นๆ
// แก้บทพูด/เวลาได้ใน Inspector, ใส่ไฟล์เสียงพูดจริงทีหลังได้ทีละบรรทัด
public class TapePlayer : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public class Line
    {
        public string speaker = "พี่บิ๊ก";
        [TextArea] public string text = "";
        [Min(0.5f)] public float seconds = 3.5f;
        [Tooltip("เสียงพูดจริงของบรรทัดนี้ (ไม่ใส่ = ใช้ไฟล์ที่ลากไว้ใน Voice Bank แทน)")]
        public AudioClip voice;
    }

    [Header("เงื่อนไข")]
    public string onlyDuringStep = "";
    public string prompt = "เปิดเทป";

    [Header("บทพูด")]
    public Line[] lines;

    [Header("กฎที่ขึ้นตอนจบ (ว่าง = ไม่ขึ้น)")]
    public string rulesTitle = "กฎของหอ";
    [TextArea(4, 12)] public string rulesBody = "";

    [Header("ระหว่างฟัง")]
    [Tooltip("ล็อกไม่ให้เดินระหว่างฟัง")]
    public bool lockPlayer = false;
    public AudioClip hissLoop;
    [Range(0f, 1f)] public float hissVolume = 0.25f;

    [Header("ผลลัพธ์")]
    public string completeStep = "";
    public UnityEvent onFinished = new UnityEvent();

    bool playing, done;
    AudioSource src;

    void Awake()
    {
        if (!GetComponent<Collider>()) { var b = gameObject.AddComponent<BoxCollider>(); b.size = new Vector3(0.4f, 0.25f, 0.3f); }
        src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 0.6f; src.minDistance = 1.5f; src.maxDistance = 10f;
        NightDemo.Respawned += () => { if (!string.IsNullOrEmpty(onlyDuringStep) && !NightDemo.IsDone(onlyDuringStep)) done = false; };
    }

    public string Prompt => prompt;
    public bool CanInteract(GameObject interactor) => !playing && !done && (string.IsNullOrEmpty(onlyDuringStep) || NightDemo.IsCurrent(onlyDuringStep)) && !DormFX.UIBusy;
    public void Interact(GameObject interactor) { if (CanInteract(interactor)) StartCoroutine(Play()); }

    [ContextMenu("เล่น (ทดสอบ)")]
    public void PlayNow() { if (!playing) StartCoroutine(Play()); }

    IEnumerator Play()
    {
        playing = true;
        var input = DormFX.Input;
        if (lockPlayer && input) input.LockMovement(this);
        DormFX.Play(NightAudio.TapeOn, transform.position, 0.8f);
        src.clip = hissLoop ? hissLoop : NightAudio.Hiss; src.loop = true; src.volume = hissVolume; src.Play();
        yield return new WaitForSeconds(0.6f);

        if (lines != null)
            foreach (var l in lines)
            {
                float len = 0f;
                if (l.voice) { src.PlayOneShot(l.voice, 1f); len = l.voice.length; }
                else len = Voice.Say(l.speaker, l.text, false, transform.position);   // ดังจากเครื่องเล่นเทป
                float sec = Mathf.Max(l.seconds, len + 0.3f);
                DormFX.Sub((string.IsNullOrEmpty(l.speaker) ? "" : l.speaker + ": ") + l.text, sec, false);
                yield return new WaitForSeconds(sec + 0.2f);
            }

        src.Stop();
        DormFX.Play(NightAudio.TapeOn, transform.position, 0.6f);
        if (lockPlayer && input) input.UnlockMovement(this);

        if (!string.IsNullOrEmpty(rulesBody) && DormOverlay.I)
            yield return DormOverlay.I.ShowDocument(rulesTitle, rulesBody);

        playing = false; done = true;
        onFinished?.Invoke();
        if (!string.IsNullOrEmpty(completeStep)) NightDemo.Complete(completeStep);
    }
}
