using System.Collections;
using System.Collections.Generic;
using System.Text;
using BlurredRoom;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD ของเกม สร้าง UI เองตอนเริ่มซีน อ่านค่าจากระบบเนทผ่าน event อย่างเดียว:
// จุดเล็ง, ข้อความกดปุ่ม (E/F/R/Q), แถบสติ/สตามินา, รายการเป้าหมาย, ไอเทมที่ถือ,
// หน้าอ่านเบาะแส, ข้อความเล่าเรื่อง, ซับไตเติล, กรอบตาแมว, หน้าจอมืด (fade), หน้าจอฉากจบ
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

    TextMeshProUGUI promptText, messageText, subtitleText, objectiveText, itemsText, clueTitle, clueBody, endTitle, endBody;
    Image sanityFill, staminaFill, crosshair, ritualFill, fade;
    GameObject peepholeMask, cluePanel, endPanel, ritualBar;
    CanvasGroup messageGroup, subtitleGroup, barsGroup;
    Coroutine messageRoutine, subtitleRoutine, clueRoutine;

    void Awake()
    {
        Instance = this;
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
        bool peeping = peephole && peephole.IsViewing;
        var sb = new StringBuilder();
        if (!peeping)
        {
            if (interactor && interactor.Focus != null)
            {
                sb.Append("[E] ").Append(interactor.Focus.Prompt);
                if (interactor.Focus is Door d && d.HasPeephole && !d.IsOpen) sb.Append("      Hold [Q] Peephole");
            }
            if (pickup && pickup.Focus != null && !(interactor && interactor.Focus is EasyPickup)) { if (sb.Length > 0) sb.Append("      "); sb.Append("[F] Take ").Append(pickup.Focus.DisplayName); }
            if (ritual && ritual.Focus != null && !ritual.IsPerforming) { if (sb.Length > 0) sb.Append("      "); sb.Append("Hold [R] ").Append(ritual.Focus.Prompt); }
        }
        promptText.text = sb.ToString();
        crosshair.enabled = !peeping;

        ritualBar.SetActive(ritual && ritual.IsPerforming);
        if (ritual && ritual.IsPerforming) ritualFill.fillAmount = ritual.Progress;

        if (sanity) sanityFill.fillAmount = Mathf.Lerp(sanityFill.fillAmount, sanity.Normalized, Time.deltaTime * 6f);
        if (stamina) staminaFill.fillAmount = stamina.Normalized;
        if (inventory)
        {
            var shown = new List<string>();
            foreach (var id in inventory.Items) if (!id.StartsWith("clue_")) shown.Add(id.Replace('_', ' '));
            itemsText.text = shown.Count > 0 ? "Carrying: " + string.Join(", ", shown) : "";
        }
    }

    // ---------------- API for story scripts ----------------
    public void SetObjectives(string header, IList<(string text, bool done)> list)
    {
        var sb = new StringBuilder();
        sb.Append("<size=26><b>").Append(header).Append("</b></size>\n");
        foreach (var o in list)
            sb.Append(o.done ? "<color=#8a8a8a><s>" : "").Append(o.done ? "[x] " : "[ ] ").Append(o.text).Append(o.done ? "</s></color>" : "").Append('\n');
        objectiveText.text = sb.ToString();
    }

    public void ShowMessage(string text, float seconds = 4f)
    {
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(Fade(messageGroup, messageText, text, seconds));
    }

    public void ShowSubtitle(string text, float seconds = 4f)
    {
        if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
        subtitleRoutine = StartCoroutine(Fade(subtitleGroup, subtitleText, text, seconds));
    }

    public void ShowClue(string title, string body)
    {
        if (clueRoutine != null) StopCoroutine(clueRoutine);
        clueRoutine = StartCoroutine(ClueRoutine(title, body));
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
        PlayerLook.SetCursorLocked(false);
    }

    // เปิดหน้าอ่านค้างไว้ หยุดการเดิน/มอง จนกว่าผู้เล่นกด F เพื่อไปต่อ
    IEnumerator ClueRoutine(string title, string body)
    {
        clueTitle.text = title; clueBody.text = body;
        cluePanel.SetActive(true);
        var input = FindFirstObjectByType<PlayerInputSource>();
        if (input) input.LockMovement(this);
        yield return new WaitForSecondsRealtime(0.4f);              // ignore the F press that picked the clue up
        while (!(input && (input.PickupPressed || input.InteractPressed))) yield return null;
        cluePanel.SetActive(false);
        yield return null;                                          // don't let the same press pick up something else
        if (input) input.UnlockMovement(this);
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

        crosshair = MakeImage("Crosshair", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5, 5), new Color(1, 1, 1, 0.55f));
        promptText = MakeText("Prompt", root, new Vector2(0.5f, 0f), new Vector2(0, 230), new Vector2(1600, 60), 30, TextAlignmentOptions.Center);

        ritualBar = new GameObject("RitualBar", typeof(RectTransform)); ritualBar.transform.SetParent(root, false);
        var rbg = MakeImage("BG", ritualBar.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(300, 8), new Color(0, 0, 0, 0.5f));
        ritualFill = MakeFill(rbg.transform, new Color(1f, 0.85f, 0.6f, 0.9f)); ritualBar.SetActive(false);

        var sg = Group("Subtitle", root); subtitleGroup = sg.GetComponent<CanvasGroup>(); subtitleGroup.alpha = 0f;
        subtitleText = MakeText("Text", sg.transform, new Vector2(0.5f, 0f), new Vector2(0, 140), new Vector2(1500, 100), 34, TextAlignmentOptions.Center);
        subtitleText.color = new Color(1f, 0.95f, 0.85f);
        var mg = Group("Message", root); messageGroup = mg.GetComponent<CanvasGroup>(); messageGroup.alpha = 0f;
        messageText = MakeText("Text", mg.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1500, 120), 38, TextAlignmentOptions.Center);
        messageText.fontStyle = FontStyles.Italic;

        var bars = Group("Bars", root); barsGroup = bars.GetComponent<CanvasGroup>();
        sanityFill = MakeBar(bars.transform, "Sanity", new Vector2(60, -60), 320, 8, new Color(0.75f, 0.85f, 1f, 0.85f));
        staminaFill = MakeBar(bars.transform, "Stamina", new Vector2(60, -82), 220, 4, new Color(1f, 1f, 1f, 0.5f));
        objectiveText = MakeText("Objectives", bars.transform, new Vector2(1, 1), new Vector2(-60, -50), new Vector2(620, 400), 24, TextAlignmentOptions.TopRight);
        objectiveText.rectTransform.pivot = new Vector2(1, 1);
        itemsText = MakeText("Items", bars.transform, new Vector2(1, 0), new Vector2(-60, 60), new Vector2(800, 40), 22, TextAlignmentOptions.BottomRight);
        itemsText.rectTransform.pivot = new Vector2(1, 0);

        // clue reader (paper-coloured card)
        var card = MakeImage("CluePanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(760, 420), new Color(0.86f, 0.82f, 0.72f, 0.96f));
        clueTitle = MakeText("Title", card.transform, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(680, 60), 34, TextAlignmentOptions.Center);
        clueTitle.color = new Color(0.15f, 0.1f, 0.06f); clueTitle.outlineWidth = 0f; clueTitle.fontStyle = FontStyles.Bold;
        clueBody = MakeText("Body", card.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(660, 300), 26, TextAlignmentOptions.TopLeft);
        clueBody.color = new Color(0.18f, 0.13f, 0.08f); clueBody.outlineWidth = 0f; clueBody.enableWordWrapping = true;
        var hint = MakeText("Continue", card.transform, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(680, 40), 22, TextAlignmentOptions.Center);
        hint.color = new Color(0.25f, 0.18f, 0.1f); hint.outlineWidth = 0f; hint.text = "[E] Continue";
        cluePanel = card.gameObject; cluePanel.SetActive(false);

        // fade + ending (on top of everything)
        fade = MakeImage("Fade", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
        var end = MakeImage("EndPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.black);
        endTitle = MakeText("Title", end.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(1400, 100), 64, TextAlignmentOptions.Center);
        endBody = MakeText("Body", end.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(1100, 260), 30, TextAlignmentOptions.Top);
        endBody.enableWordWrapping = true;
        endPanel = end.gameObject; endPanel.SetActive(false);
    }

    static GameObject Group(string name, Transform parent)
    {
        var g = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup)); g.transform.SetParent(parent, false);
        var rt = (RectTransform)g.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        return g;
    }

    Image MakeBar(Transform parent, string name, Vector2 pos, float w, float h, Color c)
    {
        var bg = MakeImage(name + "_BG", parent, new Vector2(0, 1), new Vector2(0, 1), pos, new Vector2(w, h), new Color(0, 0, 0, 0.45f));
        bg.rectTransform.pivot = new Vector2(0, 0.5f);
        return MakeFill(bg.transform, c);
    }

    Image MakeFill(Transform parent, Color c)
    {
        var fill = MakeImage("Fill", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, c);
        fill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
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

