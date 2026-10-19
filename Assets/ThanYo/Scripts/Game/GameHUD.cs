using System.Collections;
using System.Collections.Generic;
using System.Text;
using BlurredRoom;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// HUD ของเกม สร้าง UI เองตอนเริ่มซีน อ่านค่าจากระบบเนทผ่าน event อย่างเดียว:
// จุดเล็ง, ข้อความกดปุ่ม (E/F/R/Q), แถบสติ/สตามินา, รายการเป้าหมาย, ไอเทมที่ถือ,
// หน้าอ่านเบาะแส, ข้อความเล่าเรื่อง, ซับไตเติล, กรอบตาแมว, หน้าจอมืด (fade), หน้าจอฉากจบ
// ฟอนต์ไทย: Resources/ThaiFont (สร้างด้วยเมนู ThanYo > Blurred Room > 1. Import assets)
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    PlayerInteractor interactor;
    PlayerPickup pickup;
    PlayerRitual ritual;
    PlayerPeephole peephole;
    PlayerSanity sanity;
    PlayerStamina stamina;
    PlayerInventory inventory;

    TextMeshProUGUI messageText, subtitleText, itemsText, clueTitle, clueBody, endTitle, endBody;
    Image sanityFill, staminaFill, crosshair, ritualFill, fade;
    GameObject peepholeMask, cluePanel, endPanel, ritualBar;
    CanvasGroup messageGroup, subtitleGroup, barsGroup;
    Coroutine messageRoutine, subtitleRoutine, clueRoutine;
    TMP_FontAsset thaiFont;
    TextMeshProUGUI endHint;
    float endShownAt = -1f;
    PlayerInputSource input;

    // ---- game art (Resources/UI, สร้างด้วย ThanYo > Blurred Room > 4. Import UI art) ----
    Sprite paperSprite, barSprite, barBgSprite, keySprite, grimeSprite, eyeSprite, checkSprite, shadeSprite;
    TextMeshProUGUI objHeader;
    RectTransform objList;
    readonly List<(GameObject go, CanvasGroup group, Image tick, TextMeshProUGUI text)> objRows = new List<(GameObject, CanvasGroup, Image, TextMeshProUGUI)>();
    readonly List<string> objLast = new List<string>();
    RectTransform clueRect;
    CanvasGroup clueGroup;
    Image grime;
    readonly List<(GameObject root, Image frame, Image icon, TextMeshProUGUI key, TextMeshProUGUI label)> slots = new List<(GameObject, Image, Image, TextMeshProUGUI, TextMeshProUGUI)>();
    static readonly Color SanityOk = new Color(0.82f, 0.86f, 0.9f, 0.95f), SanityLow = new Color(0.78f, 0.22f, 0.16f, 0.95f);

    /// <summary>เบาะแสใหม่โชว์เป็นโน้ตเล็กๆ ด้านข้าง ไม่บังจอ/ไม่หยุดเกม (อ่านเต็มๆ ที่กระดานสืบสวน)</summary>
    public bool ReadingClue => false;

    // ข้อความภาษาอังกฤษจากโค้ดเนท/ซีน -> ไทย (โค้ดเนทไม่ได้แก้)
    static readonly Dictionary<string, string> thai = new Dictionary<string, string>
    {
        { "Open", "เปิดประตู" }, { "Open the door", "เปิดประตู" }, { "Close", "ปิดประตู" }, { "Locked", "ล็อกอยู่" },
        { "Talk", "คุย" }, { "Go outside", "ออกไปข้างนอก" }, { "Go inside", "เข้าไปในตึก" }, { "Locked (night)", "ล็อก (กลางคืน)" },
        { "A tenant's room. Locked.", "ห้องของผู้เช่า ล็อกอยู่" }, { "Vacant. Locked.", "ห้องว่าง ล็อกอยู่" },
        { "House rule: nobody goes in or out after dark.", "กฎของหอ: ห้ามเข้าออกหลังมืด" },
    };
    static string T(string s) => s != null && thai.TryGetValue(s, out var t) ? t : s;

    void Awake()
    {
        Instance = this;
        thaiFont = Resources.Load<TMP_FontAsset>("ThaiFont");
        paperSprite = Resources.Load<Sprite>("UI/paper");
        barSprite = Resources.Load<Sprite>("UI/bar");
        barBgSprite = Resources.Load<Sprite>("UI/bar_bg");
        keySprite = Resources.Load<Sprite>("UI/key");
        grimeSprite = Resources.Load<Sprite>("UI/grime");
        eyeSprite = Resources.Load<Sprite>("UI/icon_eye");
        checkSprite = Resources.Load<Sprite>("UI/check");
        shadeSprite = Resources.Load<Sprite>("UI/shade_right");
        BuildUI();
        fade.color = Color.black;   // every scene starts from black and fades in
    }

    void Start()
    {
        interactor = FindFirstObjectByType<PlayerInteractor>();
        pickup = FindFirstObjectByType<PlayerPickup>();
        ritual = FindFirstObjectByType<PlayerRitual>();
        peephole = FindFirstObjectByType<PlayerPeephole>();
        sanity = FindFirstObjectByType<PlayerSanity>();
        stamina = FindFirstObjectByType<PlayerStamina>();
        inventory = FindFirstObjectByType<PlayerInventory>();
        if (peephole) { peephole.Started += _ => SetPeep(true); peephole.Ended += _ => SetPeep(false); }
        foreach (var npc in FindObjectsByType<NeutralNpc>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            npc.LineSpoken += (n, line) => ShowSubtitle(n.DisplayName + ": " + line, 4.5f);
        if (ClueManager.Instance) ClueManager.Instance.ClueDiscovered += c => ShowClue(c.Title, c.Description);
        if (EndingManager.Instance) EndingManager.Instance.EndingReached += (e, reason) => ShowEnding(e.Title, e.Text);
        StartCoroutine(FadeTo(0f, 1.2f));
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        // หน้าจบ: กด E / F กลับเมนูหลัก
        if (endPanel.activeSelf && endShownAt >= 0f && Time.unscaledTime - endShownAt > 2.5f)
        {
            endHint.text = "[E] กลับเมนูหลัก";
            if (!input) input = FindFirstObjectByType<PlayerInputSource>();
            if (input && (input.InteractPressed || input.PickupPressed) && Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                endShownAt = -1f;
                SceneManager.LoadScene("MainMenu");
            }
        }

        foreach (var r in objRows) if (r.go.activeSelf && r.group.alpha < 1f) r.group.alpha = Mathf.MoveTowards(r.group.alpha, 1f, Time.deltaTime * 1.5f);

        bool peeping = peephole && peephole.IsViewing;
        var prompts = new List<(string key, string label, bool eye)>();
        if (!peeping)
        {
            if (interactor && interactor.Focus != null)
            {
                prompts.Add(("E", T(interactor.Focus.Prompt), false));
                if (interactor.Focus is Door d && d.HasPeephole && !d.IsOpen) prompts.Add(("Q", "ค้าง · ส่องตาแมว", true));
            }
            if (pickup && pickup.Focus != null && !(interactor && interactor.Focus is EasyPickup)) prompts.Add(("F", "เก็บ " + pickup.Focus.DisplayName, false));
            if (ritual && ritual.Focus != null && !ritual.IsPerforming) prompts.Add(("R", "ค้าง · " + ritual.Focus.Prompt, false));
        }
        SetPrompts(prompts);
        crosshair.enabled = !peeping;

        ritualBar.SetActive(ritual && ritual.IsPerforming);
        if (ritual && ritual.IsPerforming) ritualFill.fillAmount = ritual.Progress;

        if (sanity)
        {
            float n = sanity.Normalized;
            sanityFill.fillAmount = Mathf.Lerp(sanityFill.fillAmount, n, Time.deltaTime * 6f);
            sanityFill.color = Color.Lerp(SanityLow, SanityOk, Mathf.InverseLerp(0.15f, 0.6f, n));
            // ยิ่งสติน้อย ขอบจอยิ่งสกปรก/มืด (+ สั่นนิดๆ ตอนใกล้หมด)
            float target = Mathf.Lerp(0.95f, 0.2f, n) * (n < 0.25f ? 0.9f + Mathf.PerlinNoise(Time.time * 3f, 0f) * 0.2f : 1f);
            if (grime) grime.color = new Color(1, 1, 1, Mathf.Lerp(grime.color.a, target, Time.deltaTime * 2f));
        }
        if (stamina) staminaFill.fillAmount = stamina.Normalized;
        if (inventory)
        {
            var shown = new List<string>();
            foreach (var id in inventory.Items) if (!id.StartsWith("clue_")) shown.Add(id.Replace('_', ' '));
            itemsText.text = shown.Count > 0 ? "ถือ: " + string.Join(", ", shown) : "";
        }
    }

    // ---------------- API for story scripts ----------------
    public void SetObjectives(string header, IList<(string text, bool done)> list)
    {
        // หัว: "วันที่ 1/3 · กลางวัน      ผู้เช่าหาย 0/6" -> 2 บรรทัด
        int cut = header.IndexOf("      ");
        string h = cut > 0
            ? header.Substring(0, cut).Trim() + "\n<size=20><color=#b8a59a>" + header.Substring(cut).Trim() + "</color></size>"
            : header;
        if (objHeader.text != h) objHeader.text = h;

        while (objRows.Count < list.Count) objRows.Add(MakeObjectiveRow());
        while (objLast.Count < objRows.Count) objLast.Add(null);
        for (int i = 0; i < objRows.Count; i++)
        {
            var r = objRows[i];
            bool on = i < list.Count;
            if (r.go.activeSelf != on) r.go.SetActive(on);
            if (!on) { objLast[i] = null; continue; }
            string key = list[i].text + (list[i].done ? "#1" : "#0");
            if (objLast[i] == key) continue;
            bool newText = objLast[i] == null || !objLast[i].StartsWith(list[i].text);
            objLast[i] = key;
            r.text.text = list[i].text;
            r.text.color = list[i].done ? new Color(0.62f, 0.6f, 0.56f, 0.75f) : new Color(0.93f, 0.91f, 0.86f);
            r.tick.enabled = list[i].done;
            if (newText) r.group.alpha = 0f;   // เป้าหมายใหม่ค่อยๆ โผล่
        }
    }

    (GameObject, CanvasGroup, Image, TextMeshProUGUI) MakeObjectiveRow()
    {
        var go = new GameObject("Objective", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(CanvasGroup));
        go.transform.SetParent(objList, false);
        var hl = go.GetComponent<HorizontalLayoutGroup>(); hl.spacing = 12; hl.childAlignment = TextAnchor.MiddleRight;
        hl.childControlWidth = hl.childControlHeight = true; hl.childForceExpandWidth = hl.childForceExpandHeight = false;
        var t = MakeText("Text", go.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 30), 22, TextAlignmentOptions.MidlineRight);
        var le = t.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = 560;
        var boxGo = new GameObject("Box", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        boxGo.transform.SetParent(go.transform, false);
        var ble = boxGo.GetComponent<LayoutElement>(); ble.preferredWidth = ble.preferredHeight = 28;
        var box = boxGo.GetComponent<Image>(); box.raycastTarget = false; box.color = new Color(0.9f, 0.88f, 0.82f, 0.85f);
        if (keySprite) box.sprite = keySprite;
        var tick = MakeImage("Tick", boxGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(4, 4), new Vector2(34, 34), new Color(0.78f, 0.25f, 0.18f, 0.95f));
        if (checkSprite) tick.sprite = checkSprite;
        tick.enabled = false;
        return (go, go.GetComponent<CanvasGroup>(), tick, t);
    }

    public void ShowMessage(string text, float seconds = 4f)
    {
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(Fade(messageGroup, messageText, T(text), seconds));
    }

    public void ShowSubtitle(string text, float seconds = 4f)
    {
        if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
        subtitleRoutine = StartCoroutine(Fade(subtitleGroup, subtitleText, text, seconds));
    }

    public void ShowClue(string title, string body)
    {
        if (clueRoutine != null) StopCoroutine(clueRoutine);
        clueRoutine = StartCoroutine(ClueToast(title, body));
    }

    public IEnumerator FadeTo(float target, float seconds)
    {
        float start = fade.color.a;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            fade.color = new Color(0, 0, 0, Mathf.Lerp(start, target, t / seconds));
            yield return null;
        }
        fade.color = new Color(0, 0, 0, target);
    }

    public void ShowEnding(string title, string body)
    {
        endTitle.text = title; endBody.text = body;
        endPanel.SetActive(true);
        if (endShownAt < 0f) endShownAt = Time.unscaledTime;
        PlayerLook.SetCursorLocked(false);
    }

    // โน้ตเบาะแสใหม่: สไลด์เข้ามาทางขวา ค้างไว้แป๊บนึง แล้วจางหาย (ไม่หยุดเกม)
    IEnumerator ClueToast(string title, string body)
    {
        clueTitle.text = title;
        clueBody.text = body.Length > 70 ? body.Substring(0, 68) + "..." : body;
        cluePanel.SetActive(true);
        DormFX.Play2D(DormFX.ShutterClip, 0.35f, 0.6f);
        Vector2 shown = new Vector2(-40, 60), hidden = new Vector2(560, 60);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime * 3.5f)
        {
            float k = 1f - Mathf.Pow(1f - t, 3f);
            clueRect.anchoredPosition = Vector2.LerpUnclamped(hidden, shown, k);
            clueGroup.alpha = t;
            yield return null;
        }
        clueRect.anchoredPosition = shown; clueGroup.alpha = 1f;
        yield return new WaitForSeconds(5.5f);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime * 2f) { clueGroup.alpha = 1f - t; yield return null; }
        cluePanel.SetActive(false);
    }

    IEnumerator Fade(CanvasGroup g, TextMeshProUGUI t, string text, float seconds)
    {
        t.text = text;
        for (float a = g.alpha; a < 1f; a += Time.deltaTime * 4f) { g.alpha = a; yield return null; }
        g.alpha = 1f;
        yield return new WaitForSeconds(seconds);
        for (float a = 1f; a > 0f; a -= Time.deltaTime * 1.5f) { g.alpha = a; yield return null; }
        g.alpha = 0f;
    }

    void SetPeep(bool on) { peepholeMask.SetActive(on); barsGroup.alpha = on ? 0f : 1f; }

    // ---------------- UI construction ----------------
    void BuildUI()
    {
        var cgo = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler));
        cgo.transform.SetParent(transform, false);
        var canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
        var scaler = cgo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        var root = cgo.transform;

        // peephole: round hole that stays round + black side panels
        var hole = MakeImage("PeepholeMask", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180, 1180), Color.white);
        hole.sprite = MakeRadialSprite();
        var left = MakeImage("Left", hole.transform, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(3000, 0), Color.black); left.rectTransform.pivot = new Vector2(1, 0.5f);
        var right = MakeImage("Right", hole.transform, new Vector2(1, 0), new Vector2(1, 1), Vector2.zero, new Vector2(3000, 0), Color.black); right.rectTransform.pivot = new Vector2(0, 0.5f);
        peepholeMask = hole.gameObject; peepholeMask.SetActive(false);

        grime = MakeImage("Grime", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0.2f));
        if (grimeSprite) grime.sprite = grimeSprite; else grime.color = Color.clear;
        crosshair = MakeImage("Crosshair", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5, 5), new Color(1, 1, 1, 0.55f));
        BuildPromptRow(root);

        ritualBar = new GameObject("RitualBar", typeof(RectTransform)); ritualBar.transform.SetParent(root, false);
        var rbg = MakeImage("BG", ritualBar.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(340, 22), new Color(0.12f, 0.1f, 0.1f, 0.85f));
        if (barBgSprite) rbg.sprite = barBgSprite;
        ritualFill = MakeFill(rbg.transform, new Color(0.95f, 0.8f, 0.55f, 0.95f)); ritualBar.SetActive(false);

        var sg = Group("Subtitle", root); subtitleGroup = sg.GetComponent<CanvasGroup>(); subtitleGroup.alpha = 0f;
        subtitleText = MakeText("Text", sg.transform, new Vector2(0.5f, 0f), new Vector2(0, 140), new Vector2(1500, 100), 34, TextAlignmentOptions.Center);
        subtitleText.color = new Color(1f, 0.95f, 0.85f);
        var mg = Group("Message", root); messageGroup = mg.GetComponent<CanvasGroup>(); messageGroup.alpha = 0f;
        messageText = MakeText("Text", mg.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1500, 160), 36, TextAlignmentOptions.Center);
        messageText.fontStyle = FontStyles.Italic;

        var bars = Group("Bars", root); barsGroup = bars.GetComponent<CanvasGroup>();
        var sLabel = MakeText("SanityLabel", bars.transform, new Vector2(0, 1), new Vector2(60, -38), new Vector2(200, 30), 20, TextAlignmentOptions.BottomLeft);
        sLabel.rectTransform.pivot = new Vector2(0, 0); sLabel.text = "สติ"; sLabel.color = new Color(0.85f, 0.83f, 0.78f, 0.9f); sLabel.characterSpacing = 4f;
        sanityFill = MakeBar(bars.transform, "Sanity", new Vector2(60, -58), 360, 26, SanityOk);
        staminaFill = MakeBar(bars.transform, "Stamina", new Vector2(64, -86), 240, 12, new Color(0.78f, 0.82f, 0.72f, 0.7f));
        // ---- เควส: เงาจางๆ มุมขวาบน + หัวข้อ + เส้นชอล์ก + รายการพร้อมกล่องติ๊ก ----
        var shade = MakeImage("QuestShade", bars.transform, new Vector2(1, 1), new Vector2(1, 1), Vector2.zero, new Vector2(980, 380), new Color(0, 0, 0, 0.75f));
        shade.rectTransform.pivot = new Vector2(1, 1);
        if (shadeSprite) shade.sprite = shadeSprite; else shade.color = Color.clear;
        objHeader = MakeText("QuestHeader", bars.transform, new Vector2(1, 1), new Vector2(-58, -40), new Vector2(700, 80), 30, TextAlignmentOptions.TopRight);
        objHeader.rectTransform.pivot = new Vector2(1, 1); objHeader.characterSpacing = 3f; objHeader.color = new Color(0.95f, 0.93f, 0.88f);
        var line = MakeImage("QuestLine", bars.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-54, -122), new Vector2(380, 9), new Color(0.85f, 0.83f, 0.78f, 0.55f));
        line.rectTransform.pivot = new Vector2(1, 0.5f);
        if (barSprite) line.sprite = barSprite;
        var listGo = new GameObject("QuestList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        listGo.transform.SetParent(bars.transform, false);
        objList = (RectTransform)listGo.transform;
        objList.anchorMin = objList.anchorMax = new Vector2(1, 1); objList.pivot = new Vector2(1, 1); objList.anchoredPosition = new Vector2(-54, -140);
        var vl = listGo.GetComponent<VerticalLayoutGroup>(); vl.spacing = 10; vl.childAlignment = TextAnchor.UpperRight;
        vl.childControlWidth = vl.childControlHeight = true; vl.childForceExpandWidth = vl.childForceExpandHeight = false;
        listGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        listGo.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        itemsText = MakeText("Items", bars.transform, new Vector2(1, 0), new Vector2(-60, 60), new Vector2(800, 40), 22, TextAlignmentOptions.BottomRight);
        itemsText.rectTransform.pivot = new Vector2(1, 0);

        // โน้ตเบาะแสใหม่ (เล็กๆ ขวากลางจอ)
        var card = MakeImage("ClueToast", root, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 60), new Vector2(520, 190), paperSprite ? Color.white : new Color(0.86f, 0.82f, 0.72f, 0.96f));
        card.rectTransform.pivot = new Vector2(1, 0.5f);
        if (paperSprite) { card.sprite = paperSprite; card.type = Image.Type.Sliced; card.pixelsPerUnitMultiplier = 4f; }
        card.transform.localRotation = Quaternion.Euler(0, 0, 1.5f);
        clueRect = card.rectTransform;
        clueGroup = card.gameObject.AddComponent<CanvasGroup>();
        var stamp = MakeText("Stamp", card.transform, new Vector2(0, 1), new Vector2(40, -30), new Vector2(300, 34), 20, TextAlignmentOptions.TopLeft);
        stamp.rectTransform.pivot = new Vector2(0, 1);
        stamp.text = "<b>เบาะแสใหม่</b>"; stamp.color = new Color(0.62f, 0.1f, 0.08f, 0.85f); stamp.outlineWidth = 0f; stamp.characterSpacing = 4f;
        clueTitle = MakeText("Title", card.transform, new Vector2(0, 1), new Vector2(40, -60), new Vector2(440, 40), 28, TextAlignmentOptions.TopLeft);
        clueTitle.rectTransform.pivot = new Vector2(0, 1);
        clueTitle.color = new Color(0.12f, 0.08f, 0.05f); clueTitle.outlineWidth = 0f; clueTitle.fontStyle = FontStyles.Bold;
        clueBody = MakeText("Body", card.transform, new Vector2(0, 1), new Vector2(40, -100), new Vector2(440, 60), 19, TextAlignmentOptions.TopLeft);
        clueBody.rectTransform.pivot = new Vector2(0, 1);
        clueBody.color = new Color(0.2f, 0.14f, 0.08f); clueBody.outlineWidth = 0f; clueBody.textWrappingMode = TextWrappingModes.Normal;
        var hint = MakeText("Hint", card.transform, new Vector2(1, 0), new Vector2(-34, 22), new Vector2(400, 26), 16, TextAlignmentOptions.BottomRight);
        hint.rectTransform.pivot = new Vector2(1, 0);
        hint.color = new Color(0.35f, 0.25f, 0.15f, 0.9f); hint.outlineWidth = 0f; hint.text = "อ่านทั้งหมดได้ที่กระดานสืบสวน";
        cluePanel = card.gameObject; cluePanel.SetActive(false);

        // fade + ending (on top of everything)
        fade = MakeImage("Fade", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
        var end = MakeImage("EndPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.black);
        if (grimeSprite) { var eg = MakeImage("Grime", end.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.25f, 0.24f, 0.22f, 1f)); eg.sprite = grimeSprite; }
        endTitle = MakeText("Title", end.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(1400, 100), 64, TextAlignmentOptions.Center);
        endBody = MakeText("Body", end.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(1100, 360), 30, TextAlignmentOptions.Top);
        endBody.textWrappingMode = TextWrappingModes.Normal;
        endHint = MakeText("Hint", end.transform, new Vector2(0.5f, 0f), new Vector2(0, 80), new Vector2(1000, 50), 24, TextAlignmentOptions.Center);
        endHint.color = new Color(0.6f, 0.58f, 0.54f);
        endPanel = end.gameObject; endPanel.SetActive(false);
    }

    // ---------------- prompt row: [กรอบปุ่ม] ข้อความ ----------------
    void BuildPromptRow(Transform root)
    {
        var row = new GameObject("Prompts", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        row.transform.SetParent(root, false);
        var rt = (RectTransform)row.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f); rt.anchoredPosition = new Vector2(0, 205);
        var hl = row.GetComponent<HorizontalLayoutGroup>(); hl.spacing = 46; hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childControlWidth = hl.childControlHeight = true; hl.childForceExpandWidth = hl.childForceExpandHeight = false;
        var fit = row.GetComponent<ContentSizeFitter>(); fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = 0; i < 3; i++)
        {
            var slot = new GameObject("Slot" + i, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            slot.transform.SetParent(row.transform, false);
            var sh = slot.GetComponent<HorizontalLayoutGroup>(); sh.spacing = 12; sh.childAlignment = TextAnchor.MiddleLeft;
            sh.childControlWidth = sh.childControlHeight = true; sh.childForceExpandWidth = sh.childForceExpandHeight = false;

            var frameGo = new GameObject("Key", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            frameGo.transform.SetParent(slot.transform, false);
            var le = frameGo.GetComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = 46;
            var frame = frameGo.GetComponent<Image>(); frame.raycastTarget = false; frame.color = new Color(0.92f, 0.9f, 0.85f, 0.9f);
            if (keySprite) frame.sprite = keySprite;
            var key = MakeText("Letter", frameGo.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46, 46), 26, TextAlignmentOptions.Center);
            key.fontStyle = FontStyles.Bold;
            var icon = MakeImage("Icon", frameGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(54, 54), new Color(0.92f, 0.9f, 0.85f, 0.85f));
            if (eyeSprite) icon.sprite = eyeSprite;
            icon.enabled = false;

            var label = MakeText("Label", slot.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 46), 28, TextAlignmentOptions.MidlineLeft);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.gameObject.AddComponent<LayoutElement>();
            slot.SetActive(false);
            slots.Add((slot, frame, icon, key, label));
        }
    }

    void SetPrompts(List<(string key, string label, bool eye)> prompts)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            bool on = i < prompts.Count;
            if (slots[i].root.activeSelf != on) slots[i].root.SetActive(on);
            if (!on) continue;
            if (slots[i].key.text != prompts[i].key) slots[i].key.text = prompts[i].key;
            if (slots[i].label.text != prompts[i].label) slots[i].label.text = prompts[i].label;
            slots[i].icon.enabled = prompts[i].eye && eyeSprite;
        }
    }

    static GameObject Group(string name, Transform parent)
    {
        var g = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup)); g.transform.SetParent(parent, false);
        var rt = (RectTransform)g.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        return g;
    }

    Image MakeBar(Transform parent, string name, Vector2 pos, float w, float h, Color c)
    {
        var bg = MakeImage(name + "_BG", parent, new Vector2(0, 1), new Vector2(0, 1), pos, new Vector2(w, h), new Color(0.1f, 0.08f, 0.08f, 0.8f));
        bg.rectTransform.pivot = new Vector2(0, 0.5f);
        if (barBgSprite) bg.sprite = barBgSprite;
        return MakeFill(bg.transform, c);
    }

    Image MakeFill(Transform parent, Color c)
    {
        var fill = MakeImage("Fill", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, c);
        fill.sprite = barSprite ? barSprite : Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 1f;
        return fill;
    }

    Image MakeImage(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.GetComponent<Image>(); img.color = c; img.raycastTarget = false;
        return img;
    }

    TextMeshProUGUI MakeText(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (thaiFont) t.font = thaiFont;
        DormFX.ThaiMarks(t);
        t.fontSize = fontSize; t.alignment = align; t.raycastTarget = false; t.text = "";
        t.outlineWidth = 0.15f; t.outlineColor = new Color32(0, 0, 0, 200);
        return t;
    }

    static Sprite MakeRadialSprite()
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size - 0.5f, dy = (y + 0.5f) / size - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                tex.SetPixel(x, y, new Color(0, 0, 0, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.48f, 0.62f, d))));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
    }
}
