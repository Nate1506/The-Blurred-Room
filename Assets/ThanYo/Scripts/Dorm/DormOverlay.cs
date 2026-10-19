using System.Collections;
using BlurredRoom;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// UI เสริมของระบบหอพัก (สร้างเองตอนเริ่มซีน): ภาพตาแมว + ร่องรอย, จอกล้องวงจรปิด, แฟลชกล้อง,
// ภาพแฟลชอดีต (สีซีเปีย), จัมป์สแกร์, หน้าเอกสาร (กระดาน/กฎ), ข้อความเตือนบนสุด, รูปพี่บิ๊ก
public class DormOverlay : MonoBehaviour
{
    static DormOverlay inst;
    public static DormOverlay I
    {
        get
        {
            if (!inst) inst = FindFirstObjectByType<DormOverlay>();
            if (!inst) inst = new GameObject("DormOverlay").AddComponent<DormOverlay>();
            return inst;
        }
    }

    /// <summary>มีหน้าจอเต็มที่บังการเล่นอยู่ (เอกสาร / แฟลช / จัมป์สแกร์)</summary>
    public static bool Busy => inst && (inst.docOpen || inst.flashbackOn || inst.scareOn);
    public float LastClosed { get; private set; }

    Image peepPortrait; GameObject peepCard; TextMeshProUGUI peepText;
    GameObject cctvRoot; RawImage cctvImage; TextMeshProUGUI cctvLabel;
    Image flash, scareImage, staffImage; GameObject scareRoot, staffRoot; RawImage staffRaw;
    CanvasGroup flashbackGroup; TextMeshProUGUI flashbackText;
    GameObject docPanel; TextMeshProUGUI docTitle, docBody; TextMeshProUGUI alertText;
    Volume pastVolume;
    bool docOpen, flashbackOn, scareOn;

    // game art (Resources/UI)
    Sprite paperSprite, cornerSprite;
    Texture2D noiseTex, scanTex;
    RawImage cctvNoise, peepNoise;
    TextMeshProUGUI cctvClock, cctvRec;
    float cctvOpenedAt;

    void Awake()
    {
        inst = this;
        paperSprite = Resources.Load<Sprite>("UI/paper");
        cornerSprite = Resources.Load<Sprite>("UI/vhs_corner");
        noiseTex = Resources.Load<Texture2D>("UI/noise");
        scanTex = Resources.Load<Texture2D>("UI/scanline");
        Build();
    }

    void Update()
    {
        // เกรนวิดีโอวิ่ง + นาฬิกากล้อง + REC กระพริบ
        var off = new Vector2(Random.value, Random.value);
        if (cctvRoot && cctvRoot.activeSelf)
        {
            if (cctvNoise) cctvNoise.uvRect = new Rect(off, new Vector2(3.5f, 2f));
            int minutes = 60 * 2 + 17 + Mathf.FloorToInt((Time.time - cctvOpenedAt) / 2f) + Mathf.FloorToInt(Time.timeSinceLevelLoad / 30f);
            int h = (minutes / 60) % 12; if (h == 0) h = 12;
            cctvClock.text = h.ToString("00") + ":" + (minutes % 60).ToString("00") + " AM";
            cctvRec.alpha = Mathf.Repeat(Time.time, 1.2f) < 0.7f ? 1f : 0.15f;
        }
        if (peepNoise && peepNoise.enabled) peepNoise.uvRect = new Rect(off, new Vector2(2.5f, 1.5f));
    }

    void Start()
    {
        var peep = FindFirstObjectByType<PlayerPeephole>();
        if (peep) { peep.Started += OnPeepStarted; peep.Ended += _ => HidePeep(); }
    }

    void OnDestroy() { if (inst == this) inst = null; }

    // ---------------- peephole ----------------
    void OnPeepStarted(Door door)
    {
        if (StoryDirector.Instance && StoryDirector.Instance.TryPeep(door, out Sprite portrait, out string text))
            ShowPeep(portrait, text);
        else
            ShowPeep(null, "ทางเดินว่างเปล่า... ไม่มีใครอยู่หน้าประตู");
    }

    public void ShowPeep(Sprite portrait, string text)
    {
        peepPortrait.sprite = portrait;
        peepPortrait.enabled = portrait;
        if (peepNoise) peepNoise.enabled = true;
        peepText.text = text;
        peepCard.SetActive(true);
    }

    public void HidePeep() { peepPortrait.enabled = false; if (peepNoise) peepNoise.enabled = false; peepCard.SetActive(false); }

    // ---------------- CCTV ----------------
    public void ShowCctv(Texture tex, string label)
    {
        if (!cctvRoot.activeSelf) cctvOpenedAt = Time.time;
        cctvImage.texture = tex; cctvLabel.text = label; cctvRoot.SetActive(true);
    }
    public void SetCctvLabel(string label) { cctvLabel.text = label; }
    public void HideCctv() { cctvRoot.SetActive(false); }

    // ---------------- alert (top centre) ----------------
    public void SetAlert(string text) { alertText.text = text ?? ""; }

    // ---------------- camera flash (ถ่ายรูป) ----------------
    public IEnumerator CameraFlash()
    {
        DormFX.Play2D(DormFX.ShutterClip, 0.8f);
        for (float t = 0f; t < 0.45f; t += Time.unscaledDeltaTime)
        {
            flash.color = new Color(1, 1, 1, 1f - t / 0.45f);
            yield return null;
        }
        flash.color = Color.clear;
    }

    // ---------------- flashback (ภาพแฟลชจากอดีต) ----------------
    public IEnumerator Flashback(string text, float seconds)
    {
        flashbackOn = true;
        var input = DormFX.Input;
        if (input) input.LockMovement(this);
        EnsurePastVolume();
        flashbackText.text = text;
        DormFX.Play2D(DormFX.StingClip, 0.35f, 0.55f);
        CameraDynamics.Shake(0.35f);
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            float k = t / 0.8f;
            flashbackGroup.alpha = k; pastVolume.weight = k;
            yield return null;
        }
        flashbackGroup.alpha = 1f; pastVolume.weight = 1f;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            flashbackGroup.alpha = Random.value < 0.06f ? 0.75f : 1f;   // ภาพกระพริบเหมือนฟิล์มเก่า
            yield return null;
        }
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            flashbackGroup.alpha = 1f - t; pastVolume.weight = 1f - t;
            yield return null;
        }
        flashbackGroup.alpha = 0f; pastVolume.weight = 0f;
        if (input) input.UnlockMovement(this);
        flashbackOn = false;
        LastClosed = Time.time;
    }

    void EnsurePastVolume()
    {
        if (pastVolume) return;
        var go = new GameObject("PastVolume");
        go.transform.SetParent(transform, false);
        pastVolume = go.AddComponent<Volume>();
        pastVolume.isGlobal = true; pastVolume.priority = 100f; pastVolume.weight = 0f;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var ca = profile.Add<ColorAdjustments>(true);
        ca.saturation.Override(-75f); ca.colorFilter.Override(new Color(1f, 0.84f, 0.62f)); ca.contrast.Override(15f);
        var grain = profile.Add<FilmGrain>(true);
        grain.type.Override(FilmGrainLookup.Medium4); grain.intensity.Override(1f);
        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.55f);
        pastVolume.profile = profile;
    }

    // ---------------- jump scare ----------------
    public IEnumerator JumpScare(float sanityLoss)
    {
        if (scareOn) yield break;
        scareOn = true;
        scareImage.sprite = DormFX.Creature;
        scareImage.color = scareImage.sprite ? Color.white : new Color(0.15f, 0, 0);
        scareRoot.SetActive(true);
        DormFX.Play2D(DormFX.StingClip, 1f);
        CameraDynamics.Shake(1f);
        if (DormFX.Sanity && sanityLoss > 0f) DormFX.Sanity.Drain(sanityLoss);
        var rt = (RectTransform)scareImage.transform;
        for (float t = 0f; t < 0.75f; t += Time.unscaledDeltaTime)
        {
            float s = 1.05f + t * 0.5f;
            rt.localScale = new Vector3(s, s, 1f);
            rt.anchoredPosition = Random.insideUnitCircle * 25f;
            yield return null;
        }
        scareRoot.SetActive(false);
        rt.localScale = Vector3.one; rt.anchoredPosition = Vector2.zero;
        scareOn = false;
        LastClosed = Time.time;
    }

    // ---------------- document (กระดาน / กฎ) — ปิดด้วย E หรือ F ----------------
    public IEnumerator ShowDocument(string title, string body)
    {
        while (docOpen) yield return null;
        docOpen = true;
        docTitle.text = title; docBody.text = body;
        docPanel.SetActive(true);
        var input = DormFX.Input;
        if (input) input.LockMovement(this);
        yield return new WaitForSecondsRealtime(0.35f);
        while (!(input && (input.InteractPressed || input.PickupPressed))) yield return null;
        docPanel.SetActive(false);
        yield return null;
        if (input) input.UnlockMovement(this);
        docOpen = false;
        LastClosed = Time.time;
    }

    // ---------------- staff photo ----------------
    public IEnumerator ShowStaff(float seconds, Texture photo = null)
    {
        staffRaw.texture = photo; staffRaw.enabled = photo;
        staffImage.enabled = !photo;
        if (!photo) staffImage.sprite = DormFX.Staff ? DormFX.Staff : MakeStaffPlaceholder();
        staffRoot.SetActive(true);
        yield return new WaitForSeconds(seconds);
        staffRoot.SetActive(false);
    }

    static Sprite placeholder;
    static Sprite MakeStaffPlaceholder()
    {
        if (placeholder) return placeholder;
        const int w = 256, h = 300;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color bg = new Color(0.95f, 0.93f, 0.85f), face = new Color(1f, 0.82f, 0.25f), ink = new Color(0.15f, 0.1f, 0.05f), tongue = new Color(0.9f, 0.3f, 0.35f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x - w / 2f, y - h / 2f - 10f);
                Color c = bg;
                if (p.magnitude < 95f) c = face;
                if (Vector2.Distance(p, new Vector2(-35, 30)) < 12f) c = ink;                                   // ตาซ้าย
                if (Mathf.Abs(p.y - 30f) < 4f && p.x > 18f && p.x < 55f) c = ink;                                // ขยิบตา
                if (p.y < -15f && p.y > -55f && Mathf.Abs(p.x) < 50f && p.magnitude > 55f && p.magnitude < 64f) c = ink; // ยิ้ม
                if (Vector2.Distance(p, new Vector2(15, -62)) < 16f && p.y < -55f) c = tongue;                  // แลบลิ้น
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return placeholder = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f);
    }

    // ---------------- UI construction ----------------
    void Build()
    {
        // ใต้ HUD (HUD มีวงตาแมวสีดำ order 50) -> รูปคนหน้าประตูจะโผล่แค่ในวงกลม
        var back = MakeCanvas("DormOverlay_Back", 49);
        peepPortrait = MakeImage("PeepPortrait", back, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(820, 820), new Color(0.75f, 0.72f, 0.68f));
        peepPortrait.preserveAspect = true; peepPortrait.enabled = false;
        if (noiseTex) { peepNoise = Raw("PeepGrain", back, noiseTex, new Color(1, 1, 1, 0.07f)); peepNoise.enabled = false; }

        var front = MakeCanvas("DormOverlay_Front", 60);

        peepCard = MakeImage("PeepCard", front, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 120), new Vector2(1100, 210), new Color(0, 0, 0, 0.72f)).gameObject;
        peepText = MakeText("Text", peepCard.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040, 190), 26, TextAlignmentOptions.TopLeft);
        peepCard.SetActive(false);

        // กล้องวงจรปิด: ภาพเต็มจอ โทนเขียวอมฟ้า + เส้นสแกน + เกรน + กรอบมุม REC / เวลา / ชื่อกล้อง
        var cctvBg = MakeImage("CCTV", front, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.black);
        cctvRoot = cctvBg.gameObject;
        var rawGo = new GameObject("Feed", typeof(RectTransform), typeof(RawImage));
        rawGo.transform.SetParent(cctvRoot.transform, false);
        var rrt = (RectTransform)rawGo.transform; rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one; rrt.offsetMin = rrt.offsetMax = Vector2.zero;
        cctvImage = rawGo.GetComponent<RawImage>(); cctvImage.color = new Color(0.62f, 0.86f, 0.84f); cctvImage.raycastTarget = false;
        MakeImage("Tint", cctvRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.05f, 0.16f, 0.17f, 0.22f));
        if (scanTex) { var sc = Raw("Scanlines", cctvRoot.transform, scanTex, new Color(1, 1, 1, 0.55f)); sc.uvRect = new Rect(0, 0, 1, 270); }
        if (noiseTex) cctvNoise = Raw("Grain", cctvRoot.transform, noiseTex, new Color(1, 1, 1, 0.11f));
        var vig = MakeImage("Vignette", cctvRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0.75f));
        var grime = Resources.Load<Sprite>("UI/grime"); if (grime) vig.sprite = grime; else vig.color = Color.clear;
        if (cornerSprite)
        {
            Vector2[] anchors = { new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0) };
            Vector2[] pos = { new Vector2(125, -115), new Vector2(-125, -115), new Vector2(-125, 115), new Vector2(125, 115) };
            float[] rot = { 0, -90, 180, 90 };
            for (int i = 0; i < 4; i++)
            {
                var c = MakeImage("Corner" + i, cctvRoot.transform, anchors[i], anchors[i], pos[i], new Vector2(110, 110), new Color(1, 1, 1, 0.9f));
                c.sprite = cornerSprite; c.transform.localRotation = Quaternion.Euler(0, 0, rot[i]);
            }
        }
        cctvRec = MakeText("Rec", cctvRoot.transform, new Vector2(0, 1), new Vector2(105, -82), new Vector2(300, 50), 34, TextAlignmentOptions.TopLeft);
        cctvRec.rectTransform.pivot = new Vector2(0, 1); cctvRec.text = "<color=#ff3030>●</color> REC"; cctvRec.color = Color.white;
        cctvClock = MakeText("Clock", cctvRoot.transform, new Vector2(1, 1), new Vector2(-105, -82), new Vector2(400, 60), 52, TextAlignmentOptions.TopRight);
        cctvClock.rectTransform.pivot = new Vector2(1, 1); cctvClock.color = new Color(0.92f, 0.95f, 0.93f); cctvClock.characterSpacing = 4f;
        cctvLabel = MakeText("Label", cctvRoot.transform, new Vector2(0, 0), new Vector2(105, 82), new Vector2(700, 60), 44, TextAlignmentOptions.BottomLeft);
        cctvLabel.rectTransform.pivot = new Vector2(0, 0); cctvLabel.color = new Color(0.92f, 0.95f, 0.93f); cctvLabel.characterSpacing = 3f;
        var hintC = MakeText("Hint", cctvRoot.transform, new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(900, 40), 22, TextAlignmentOptions.Center);
        hintC.text = "[A] [D] เปลี่ยนกล้อง      [E] ออก"; hintC.color = new Color(0.8f, 0.85f, 0.82f, 0.8f);
        cctvRoot.SetActive(false);

        var fbBg = MakeImage("Flashback", front, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.35f, 0.25f, 0.12f, 0.55f));
        flashbackGroup = fbBg.gameObject.AddComponent<CanvasGroup>(); flashbackGroup.alpha = 0f; flashbackGroup.blocksRaycasts = false;
        flashbackText = MakeText("Text", fbBg.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 600), 40, TextAlignmentOptions.Center);
        flashbackText.color = new Color(1f, 0.93f, 0.8f); flashbackText.fontStyle = FontStyles.Italic;

        var docBg = MakeImage("Document", front, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060, 900), paperSprite ? Color.white : new Color(0.86f, 0.82f, 0.72f, 0.97f));
        if (paperSprite) { docBg.sprite = paperSprite; docBg.type = Image.Type.Sliced; docBg.pixelsPerUnitMultiplier = 2f; }
        docBg.transform.localRotation = Quaternion.Euler(0, 0, 0.8f);
        docPanel = docBg.gameObject;
        docTitle = MakeText("Title", docPanel.transform, new Vector2(0.5f, 1f), new Vector2(0, -55), new Vector2(920, 70), 38, TextAlignmentOptions.Center);
        docTitle.color = new Color(0.15f, 0.1f, 0.06f); docTitle.outlineWidth = 0f; docTitle.fontStyle = FontStyles.Bold;
        docBody = MakeText("Body", docPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(900, 640), 25, TextAlignmentOptions.TopLeft);
        docBody.color = new Color(0.06f, 0.04f, 0.02f); docBody.outlineWidth = 0f; docBody.enableAutoSizing = true; docBody.fontSizeMin = 16; docBody.fontSizeMax = 25;
        var hint = MakeText("Hint", docPanel.transform, new Vector2(0.5f, 0f), new Vector2(0, 32), new Vector2(900, 40), 22, TextAlignmentOptions.Center);
        hint.color = new Color(0.3f, 0.2f, 0.1f); hint.outlineWidth = 0f; hint.text = "[E] ปิด";
        docPanel.SetActive(false);

        var staffBg = MakeImage("Staff", front, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(460, 560), paperSprite ? Color.white : new Color(0.3f, 0.2f, 0.1f));
        if (paperSprite) { staffBg.sprite = paperSprite; staffBg.type = Image.Type.Sliced; staffBg.pixelsPerUnitMultiplier = 3f; }
        staffBg.transform.localRotation = Quaternion.Euler(0, 0, -4f);
        staffRoot = staffBg.gameObject;
        staffImage = MakeImage("Photo", staffRoot.transform, Vector2.zero, Vector2.one, new Vector2(0, 20), new Vector2(-70, -110), Color.white);
        staffImage.preserveAspect = true;
        var rawGo2 = new GameObject("PhotoRaw", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        rawGo2.transform.SetParent(staffRoot.transform, false);
        rawGo2.transform.localScale = new Vector3(0.84f, 0.84f, 1f);   // เหลือขอบกระดาษรอบรูป
        staffRaw = rawGo2.GetComponent<RawImage>(); staffRaw.raycastTarget = false; staffRaw.color = new Color(0.92f, 0.9f, 0.86f);
        var arf = rawGo2.GetComponent<AspectRatioFitter>(); arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent; arf.aspectRatio = 0.78f;
        staffRaw.enabled = false;
        staffRoot.SetActive(false);

        flash = MakeImage("Flash", front, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);

        var scareBg = MakeImage("Scare", front, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.black);
        scareRoot = scareBg.gameObject;
        scareImage = MakeImage("Face", scareRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
        scareImage.preserveAspect = true;
        MakeImage("Red", scareRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.6f, 0f, 0f, 0.25f));
        scareRoot.SetActive(false);

        alertText = MakeText("Alert", front, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(1400, 70), 40, TextAlignmentOptions.Center);
        alertText.color = new Color(1f, 0.55f, 0.45f);
    }

    static RawImage Raw(string name, Transform parent, Texture tex, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var r = go.GetComponent<RawImage>(); r.texture = tex; r.color = c; r.raycastTarget = false;
        return r;
    }

    Transform MakeCanvas(string name, int order)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = order;
        var s = go.GetComponent<CanvasScaler>(); s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080); s.matchWidthOrHeight = 0.5f;
        return go.transform;
    }

    static Image MakeImage(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.GetComponent<Image>(); img.color = c; img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI MakeText(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (DormFX.ThaiFont) t.font = DormFX.ThaiFont;
        DormFX.ThaiMarks(t);
        t.fontSize = fontSize; t.alignment = align; t.raycastTarget = false; t.text = "";
        t.outlineWidth = 0.15f; t.outlineColor = new Color32(0, 0, 0, 200);
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }
}
