using System.Collections;
using System.Collections.Generic;
using BlurredRoom;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// หน้าเมนูหลัก: ทางเดินชั้น 2 (เรนเดอร์จากฉากจริง) เบลอเหมือนกล้องหลุดโฟกัส — "The Blurred Room"
// ทุกๆ ช่วง กล้องจะ "ดึงโฟกัส" ภาพชัดขึ้นแวบนึง... บางครั้งมีใครยืนอยู่สุดทางเดิน แล้วไฟกะพริบ มันก็หายไป
// เลย์เอาต์กลางจอ (โลโก้ / รายการ / แถบล่าง), ฝุ่นลอย, ขอบมืด, ไฟกะพริบ — ปรับทุกอย่างใน Inspector หัวข้อ "บรรยากาศเมนู"
// สร้าง UI เองทั้งหมดตอนเริ่ม (เหมือน GameHUD) — รูป/ฟอนต์อยู่ใน Resources/Menu และ Resources/ThaiFont
// คีย์บอร์ด: W/S หรือ ลูกศร เลือก, Enter/E/Space ยืนยัน, Esc/Backspace ย้อนกลับ, A/D ปรับค่าในหน้าตั้งค่า   เมาส์: ชี้ + คลิก
public class MainMenu : MonoBehaviour
{
    [Header("เริ่มเกม")]
    [Tooltip("ซีนที่โหลดตอนกด 'เริ่มเกม' (ThanYo = ลานหน้าหอ)")]
    public string gameScene = "ThanYo";
    [Tooltip("id ของ SpawnPoint ในซีนนั้นที่ให้ผู้เล่นไปเกิด (ว่าง = ใช้จุดที่ติ๊ก isDefault)")]
    public string startSpawnId = "Start";

    [Header("ปุ่มลัดดูเดโม (มุมซ้ายล่าง)")]
    public bool showDemoJump = true;
    public string demoJumpLabel = ">> ข้ามไปฉากเคาะประตู (ดูเดโม)";
    [Tooltip("ซีนของฉากเคาะประตู")]
    public string demoJumpScene = "Inside";
    [Tooltip("จุดเกิด (SpawnPoint id) ในห้องผู้ดูแล")]
    public string demoJumpSpawnId = "DemoKnock";
    [Tooltip("ขั้นของ NightDemo ที่ถือว่าทำไปแล้ว (ข้ามมาเริ่มที่ขั้นถัดไป)")]
    public string[] demoJumpDoneSteps = { "go_office", "tape", "gear", "switch_f2", "switch_f3", "back_down", "breaker", "safe_room" };

    [Header("บรรยากาศเมนู")]
    [Tooltip("ดึงโฟกัส (ภาพชัดขึ้น) ทุกๆ X-Y วินาที")]
    public Vector2 focusPullEvery = new Vector2(14f, 24f);
    [Tooltip("ค้างภาพชัดกี่วินาที")]
    [Min(0.2f)] public float focusHold = 1.8f;
    [Tooltip("โอกาสที่ตอนภาพชัดจะมีคนยืนอยู่สุดทางเดิน")]
    [Range(0f, 1f)] public float figureChance = 0.55f;
    [Tooltip("ความชัดสูงสุดตอนดึงโฟกัส (1 = ชัดเต็ม)")]
    [Range(0f, 1f)] public float focusAmount = 0.85f;
    [Tooltip("ภาพเลื่อน/ซูมช้าๆ แรงแค่ไหน")]
    [Range(0f, 3f)] public float drift = 1f;
    [Tooltip("จำนวนฝุ่นที่ลอยอยู่")]
    [Range(0, 150)] public int dustCount = 55;
    [Tooltip("ไฟกะพริบเองเป็นพักๆ (โอกาสต่อเฟรม)")]
    [Range(0f, 0.05f)] public float flickerChance = 0.008f;
    [Tooltip("ความมืดด้านหลังโลโก้และรายการ (ช่วยให้อ่านง่าย)")]
    [Range(0f, 1f)] public float centerShadeAlpha = 0.45f;
    [Tooltip("ข้อความมุมซ้ายล่าง")]
    public string buildLabel = "DEMO BUILD · v0.1";

    class Item
    {
        public string label, desc;
        public System.Action onConfirm;
        public System.Func<string> value;       // หน้าตั้งค่า: ข้อความค่าปัจจุบัน
        public System.Action<int> onAdjust;     // หน้าตั้งค่า: -1 / +1
        public TextMeshProUGUI text;
        public RectTransform rect;
    }

    enum Page { Main, Settings, HowTo, Warning }

    [Header("หน้าคำเตือน (ขึ้นก่อนเข้าเกม)")]
    public bool showWarning = true;
    [Tooltip("ขึ้นแค่ครั้งแรกที่กดเริ่ม (ต่อการเปิดเกม 1 ครั้ง)")]
    public bool warnOncePerSession = false;
    public string warningTitle = "คำเตือน";
    [TextArea(6, 16)] public string warningBody =
        "เกมนี้มี <b>ฉากตกใจ (Jumpscare)</b> และ<b>เสียงดังกะทันหัน</b>\n\n" +
        "- มีภาพกะพริบ ภาพสั่น และแสงวูบวาบ — ผู้ที่ไวต่อแสงกะพริบหรือมีประวัติโรคลมชักควรหลีกเลี่ยง\n" +
        "- มีเนื้อหาสยองขวัญ ความมืด และบรรยากาศกดดัน อาจไม่เหมาะกับผู้ที่ตกใจง่ายหรือมีโรคหัวใจ\n" +
        "- แนะนำให้ใส่หูฟัง และปรับเสียงให้พอดีก่อนเริ่ม\n" +
        "- ถ้ารู้สึกไม่สบาย เวียนหัว หรือเครียด ให้พักได้ตลอด\n\n" +
        "เรื่องราวและตัวละครทั้งหมดเป็นเรื่องแต่ง";
    public string warningHint = "[Enter] เข้าใจแล้ว เข้าเกม        [Esc] ย้อนกลับ";

    GameObject warningPanel;
    CanvasGroup warningContent;
    [Tooltip("เวลาให้ข้อความคำเตือนค่อยๆ จางหายก่อนเข้าเกม (วินาที)")]
    [Min(0f)] public float warningFadeOut = 1.2f;
    System.Action warningContinue;
    static bool warnedThisSession;

    TMP_FontAsset thai, serif, type;
    RectTransform listRoot, bgRect, highlight;
    Image bgSharp, bgFigure;
    RawImage noiseRaw;
    TextMeshProUGUI titleGhost;
    readonly List<RectTransform> dust = new List<RectTransform>();
    readonly List<Vector2> dustVel = new List<Vector2>();
    TextMeshProUGUI header, desc, hint, demoJump;
    Image fade, flicker;
    GameObject howToPanel;
    readonly List<Item> items = new List<Item>();
    int index;
    Page page;
    bool busy;
    float nextKnock;

    static readonly Color Off = new Color(0.55f, 0.55f, 0.52f), On = new Color(0.95f, 0.94f, 0.9f);

    // ---- กด Play จากซีนเกม (Inside / ThanYo) ใน Editor -> เด้งมาเมนูก่อน ----
    static bool visited;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetVisited() => visited = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RedirectToMenu()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorPrefs.GetBool("ThanYo.SkipMainMenu", false)) return;
#endif
        string s = SceneManager.GetActiveScene().name;
        if (!visited && (s == "Inside" || s == "ThanYo") && Application.CanStreamedLevelBeLoaded("MainMenu"))
            SceneManager.LoadScene("MainMenu");
    }

    void Awake()
    {
        visited = true;
        thai = Resources.Load<TMP_FontAsset>("ThaiFont");
        serif = Resources.Load<TMP_FontAsset>("Menu/MenuSerif") ?? thai;
        type = Resources.Load<TMP_FontAsset>("Menu/MenuType") ?? serif;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
        Build();
        ShowMain();
        StartCoroutine(Fade(1f, 0f, 1.6f));
        StartAmbience();
        nextKnock = Time.time + Random.Range(8f, 14f);
    }

    // =====================================================================
    //  pages
    // =====================================================================
    void ShowMain()
    {
        page = Page.Main;
        header.text = "เมนูหลัก";
        SetItems(new List<Item>
        {
            new Item { label = "เริ่มเกม", desc = "เริ่มกะแรกในฐานะผู้ดูแล 'สุขนิรันดร์ แมนชั่น'\nเอาตัวรอดให้ครบ 3 คืน", onConfirm = () => WarnThen(() => StartCoroutine(StartGame())) },
            new Item { label = "วิธีเล่น", desc = "ปุ่มควบคุม และกฎของหอที่ต้องจำให้ขึ้นใจ", onConfirm = ShowHowTo },
            new Item { label = "ตั้งค่า", desc = "ปรับความไวเมาส์ มุมมอง และเสียง", onConfirm = ShowSettings },
            new Item { label = "ออกจากเกม", desc = "ปิดไฟ ล็อกประตู แล้วกลับบ้าน...\nถ้ายังกลับได้", onConfirm = Quit },
        });
    }

    void ShowSettings()
    {
        page = Page.Settings;
        header.text = "ตั้งค่า";
        SetItems(new List<Item>
        {
            new Item { label = "ความไวเมาส์", desc = "กด A / D หรือ ลูกศรซ้าย-ขวา เพื่อปรับ",
                       value = () => GameSettings.LookSensitivity.ToString("0.0"), onAdjust = d => GameSettings.LookSensitivity += d * 0.1f },
            new Item { label = "มุมมอง (FOV)", desc = "มุมกว้าง = เห็นรอบตัวมากขึ้น\nมุมแคบ = อึดอัดกว่า",
                       value = () => Mathf.RoundToInt(GameSettings.FieldOfView).ToString(), onAdjust = d => GameSettings.FieldOfView += d * 5f },
            new Item { label = "เสียง", desc = "ความดังของเสียงทั้งหมด\n(แนะนำให้ใส่หูฟัง)",
                       value = () => Mathf.RoundToInt(GameSettings.MasterVolume * 100f) + "%", onAdjust = d => GameSettings.MasterVolume += d * 0.1f },
            new Item { label = "กลับค่าเริ่มต้น", desc = "", onConfirm = () => { GameSettings.ResetToDefaults(); Refresh(); } },
            new Item { label = "กลับ", desc = "", onConfirm = ShowMain },
        });
    }

    void ShowHowTo()
    {
        page = Page.HowTo;
        howToPanel.SetActive(true);
    }

    // ข้อความจางหาย (พื้นดำค้าง) -> เข้าเกม (ซีนเกมจะค่อยๆ สว่างจากดำเอง)
    IEnumerator LeaveWarning()
    {
        busy = true;
        for (float t = 0f; t < warningFadeOut; t += Time.unscaledDeltaTime)
        {
            warningContent.alpha = 1f - t / warningFadeOut;
            yield return null;
        }
        warningContent.alpha = 0f;
        fade.color = Color.black;   // จอหลักดำค้างไว้ด้วย (ไม่กระพริบกลับมาเห็นเมนู)
        busy = false;
        page = Page.Main;
        var go = warningContinue; warningContinue = null;
        go?.Invoke();
    }

    /// <summary>ขึ้นหน้าคำเตือนก่อน แล้วค่อยทำ next (ถ้าปิดคำเตือนไว้ ทำเลย)</summary>
    void WarnThen(System.Action next)
    {
        if (!showWarning || !warningPanel || (warnOncePerSession && warnedThisSession)) { next(); return; }
        warningContinue = next;
        page = Page.Warning;
        warningContent.alpha = 1f;
        warningPanel.SetActive(true);
    }

    void Back()
    {
        if (page == Page.HowTo) { howToPanel.SetActive(false); page = Page.Main; return; }
        if (page == Page.Settings) ShowMain();
    }

    IEnumerator JumpToKnock()
    {
        busy = true;
        yield return Fade(0f, 1f, 1f);
        GameState.NewGame();
        GameState.Phase = DayPhase.Night;
        foreach (var id in demoJumpDoneSteps) GameState.Set("nd_" + id);
        GameState.SpawnId = demoJumpSpawnId;
        SceneManager.LoadScene(demoJumpScene);
    }

    IEnumerator StartGame()
    {
        busy = true;
        DormFX.Play2D(DormFX.KnockClip, 0.9f);
        yield return new WaitForSeconds(0.55f);
        DormFX.Play2D(DormFX.KnockClip, 0.9f);
        yield return new WaitForSeconds(0.55f);
        DormFX.Play2D(DormFX.KnockClip, 0.9f);
        yield return Fade(0f, 1f, 1.4f);
        GameState.NewGame();
        GameState.SpawnId = startSpawnId;
        SceneManager.LoadScene(gameScene);
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // =====================================================================
    //  input
    // =====================================================================
    void Update()
    {
        AnimateBackground();
        if (Time.time > nextKnock)
        {
            nextKnock = Time.time + Random.Range(18f, 30f);
            StartCoroutine(DistantKnock());
        }
        if (busy) return;
        if (demoJump)
        {
            bool over = RectTransformUtility.RectangleContainsScreenPoint(demoJump.rectTransform, MousePos());
            demoJump.color = over ? On : new Color(0.6f, 0.58f, 0.55f, 0.8f);
            if (over && Clicked() && page == Page.Main) { Tick(); WarnThen(() => StartCoroutine(JumpToKnock())); return; }
        }

        if (page == Page.HowTo)
        {
            if (Confirm() || BackPressed() || Clicked()) Back();
            return;
        }
        if (page == Page.Warning)
        {
            if (BackPressed()) { warningPanel.SetActive(false); page = Page.Main; return; }
            if (Confirm() || Clicked())
            {
                Tick();
                warnedThisSession = true;
                StartCoroutine(LeaveWarning());
            }
            return;
        }

        if (Down()) Select(index + 1);
        if (Up()) Select(index - 1);
        var it = items[index];
        if (it.onAdjust != null)
        {
            if (Right()) { it.onAdjust(+1); Refresh(); }
            if (Left()) { it.onAdjust(-1); Refresh(); }
        }

        // mouse hover
        Vector2 mouse = MousePos();
        for (int i = 0; i < items.Count; i++)
            if (RectTransformUtility.RectangleContainsScreenPoint(items[i].rect, mouse) && i != index && MouseMoved()) Select(i);

        bool clickOnItem = Clicked() && RectTransformUtility.RectangleContainsScreenPoint(items[index].rect, mouse);
        if ((Confirm() || clickOnItem) && it.onConfirm != null) { Tick(); it.onConfirm(); }
        else if (clickOnItem && it.onAdjust != null) { it.onAdjust(+1); Refresh(); }
        if (BackPressed()) Back();
    }

#if ENABLE_INPUT_SYSTEM
    static Keyboard K => Keyboard.current;
    static bool Pressed(params Key[] keys) { if (K == null) return false; foreach (var k in keys) if (K[k].wasPressedThisFrame) return true; return false; }
    static bool Down() => Pressed(Key.S, Key.DownArrow);
    static bool Up() => Pressed(Key.W, Key.UpArrow);
    static bool Left() => Pressed(Key.A, Key.LeftArrow);
    static bool Right() => Pressed(Key.D, Key.RightArrow);
    static bool Confirm() => Pressed(Key.Enter, Key.NumpadEnter, Key.Space, Key.E);
    static bool BackPressed() => Pressed(Key.Escape, Key.Backspace);
    static bool Clicked() => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    static Vector2 MousePos() => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    static bool MouseMoved() => Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.5f;
#else
    static bool Down() => Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
    static bool Up() => Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
    static bool Left() => Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
    static bool Right() => Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
    static bool Confirm() => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E);
    static bool BackPressed() => Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace);
    static bool Clicked() => Input.GetMouseButtonDown(0);
    static Vector2 MousePos() => Input.mousePosition;
    static bool MouseMoved() => Mathf.Abs(Input.GetAxisRaw("Mouse X")) + Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 0.01f;
#endif

    // =====================================================================
    //  list
    // =====================================================================
    void SetItems(List<Item> list)
    {
        foreach (var it in items) if (it.text) Destroy(it.text.gameObject);
        items.Clear();
        items.AddRange(list);
        for (int i = 0; i < items.Count; i++)
        {
            var t = Text("Item_" + i, listRoot, new Vector2(0.5f, 1), new Vector2(0, -i * 58f), new Vector2(800, 52), 34, TextAlignmentOptions.Center, thai);
            t.rectTransform.pivot = new Vector2(0.5f, 1);
            t.characterSpacing = 4f;
            items[i].text = t;
            items[i].rect = t.rectTransform;
        }
        index = 0;
        Select(0, false);
    }

    void Select(int i, bool sound = true)
    {
        index = (i + items.Count) % items.Count;
        if (sound) Tick();
        Refresh();
    }

    void Refresh()
    {
        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            string v = it.value != null ? "    <  " + it.value() + "  >" : "";
            bool on = i == index;
            it.text.text = on ? "<color=#8a2018>—</color>   " + it.label + v + "   <color=#8a2018>—</color>" : it.label + v;
            it.text.color = on ? On : Off;
            it.text.fontSize = on ? 36 : 32;
        }
        var sel = items[index].rect;
        highlight.SetParent(listRoot, false);
        highlight.anchoredPosition = new Vector2(0, sel.anchoredPosition.y - 26f);
        highlight.sizeDelta = new Vector2(Mathf.Max(360f, items[index].text.preferredWidth + 140f), 54f);
        highlight.SetAsFirstSibling();
        desc.rectTransform.anchoredPosition = new Vector2(0, listRoot.anchoredPosition.y - items.Count * 58f - 64f);
        desc.text = items[index].desc;
    }

    static void Tick() => DormFX.Play2D(DormFX.TapClip, 0.25f, 0.55f);

    // =====================================================================
    //  atmosphere
    // =====================================================================
    float bgT, nextPull = 6f, focus, figure, ghostT;
    bool pulling;

    void AnimateBackground()
    {
        float dt = Time.deltaTime;
        bgT += dt;
        // ภาพเลื่อน/ซูมช้าๆ เหมือนกล้องมือถือ
        float s = 1.06f + Mathf.Sin(bgT * 0.05f) * 0.025f * drift;
        bgRect.localScale = new Vector3(s, s, 1f);
        bgRect.anchoredPosition = new Vector2(Mathf.Sin(bgT * 0.07f) * 14f * drift, Mathf.Cos(bgT * 0.05f) * 7f * drift);

        // ดึงโฟกัส
        if (!pulling && bgT > nextPull && bgSharp.enabled) StartCoroutine(FocusPull());
        bgSharp.color = new Color(1, 1, 1, focus * focusAmount);
        bgFigure.color = new Color(1, 1, 1, figure * focusAmount);

        // ไฟกะพริบเองเป็นพักๆ
        if (!pulling)
        {
            float f = Random.value < flickerChance ? Random.Range(0.25f, 0.6f) : Mathf.MoveTowards(flicker.color.a, 0f, dt * 2.5f);
            flicker.color = new Color(0, 0, 0, f);
        }

        // ฝุ่น
        for (int i = 0; i < dust.Count; i++)
        {
            var p = dust[i].anchoredPosition + (dustVel[i] + new Vector2(Mathf.Sin(bgT * 0.4f + i) * 4f, 0f)) * dt;
            if (p.y > 1100f) p = new Vector2(Random.Range(0f, 1920f), -20f);
            if (p.x > 1940f) p.x = -20f; else if (p.x < -20f) p.x = 1940f;
            dust[i].anchoredPosition = p;
        }

        // นอยส์ฟิล์ม + เงาแดงของโลโก้ (ภาพซ้อนเบลอๆ เป็นพักๆ)
        if (noiseRaw) noiseRaw.uvRect = new Rect(Random.value, Random.value, 2.4f, 1.4f);
        ghostT = Random.value < 0.006f ? 0.35f : Mathf.MoveTowards(ghostT, 0f, dt * 1.2f);
        if (titleGhost)
        {
            titleGhost.color = new Color(0.75f, 0.1f, 0.08f, ghostT);
            titleGhost.rectTransform.anchoredPosition = new Vector2(0, 330) + (ghostT > 0f ? new Vector2(Random.Range(-6f, 6f), Random.Range(-2f, 2f)) : Vector2.zero);
        }
    }

    // ภาพค่อยๆ ชัดขึ้น... บางครั้งมีคนยืนอยู่สุดทางเดิน -> ไฟกะพริบ -> คนหายไป -> ภาพเบลอกลับ
    IEnumerator FocusPull()
    {
        pulling = true;
        bool withFigure = bgFigure.enabled && Random.value < figureChance;
        for (float t = 0f; t < 1.6f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 1.6f);
            focus = k; if (withFigure) figure = k;
            yield return null;
        }
        yield return new WaitForSeconds(focusHold);
        if (withFigure)
        {
            for (int i = 0; i < 4; i++)   // ไฟกะพริบ... แล้วมันก็ไม่อยู่ตรงนั้นแล้ว
            {
                flicker.color = new Color(0, 0, 0, Random.Range(0.6f, 0.92f));
                if (i == 2) figure = 0f;
                yield return new WaitForSeconds(Random.Range(0.05f, 0.12f));
                flicker.color = new Color(0, 0, 0, 0.1f);
                yield return new WaitForSeconds(Random.Range(0.04f, 0.1f));
            }
            flicker.color = Color.clear;
            yield return new WaitForSeconds(0.8f);
        }
        for (float t = 0f; t < 2f; t += Time.deltaTime)
        {
            focus = 1f - Mathf.SmoothStep(0f, 1f, t / 2f);
            yield return null;
        }
        focus = 0f; figure = 0f;
        nextPull = bgT + Random.Range(focusPullEvery.x, focusPullEvery.y);
        pulling = false;
    }

    IEnumerator DistantKnock()
    {
        int n = Random.value < 0.5f ? 3 : 5;                                // บางทีก็เคาะ 5 ครั้ง...
        for (int i = 0; i < n; i++) { DormFX.Play2D(DormFX.KnockClip, 0.18f, 0.85f); yield return new WaitForSeconds(i == 2 ? 2f : 0.55f); }
    }

    void StartAmbience()
    {
        var bank = SoundBank.I;
        if (!bank || !bank.menuMusic) return;   // เพลงเมนูตั้งที่ Resources/SoundBank
        var src = gameObject.AddComponent<AudioSource>();
        src.clip = bank.menuMusic; src.loop = true; src.volume = bank.menuMusicVolume; src.spatialBlend = 0f;
        src.Play();
    }

    IEnumerator Fade(float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            fade.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t / seconds));
            yield return null;
        }
        fade.color = new Color(0, 0, 0, to);
    }

    // =====================================================================
    //  UI construction
    // =====================================================================
    void Build()
    {
        var cgo = new GameObject("Menu_Canvas", typeof(Canvas), typeof(CanvasScaler));
        cgo.transform.SetParent(transform, false);
        var canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = cgo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        var root = cgo.transform;

        // ---------- พื้นหลัง: เบลอ / ชัด / ชัด+เงาคน (ซ้อนกัน) ----------
        var bgGo = new GameObject("Background", typeof(RectTransform), typeof(AspectRatioFitter));
        bgGo.transform.SetParent(root, false);
        bgRect = (RectTransform)bgGo.transform;
        bgRect.anchorMin = bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        var blurSprite = Resources.Load<Sprite>("Menu/menu_bg_blur") ?? Resources.Load<Sprite>("Menu/menu_bg");
        var fitBg = bgGo.GetComponent<AspectRatioFitter>();
        fitBg.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitBg.aspectRatio = blurSprite ? blurSprite.rect.width / blurSprite.rect.height : 16f / 9f;
        var blurImg = Img("Blur", bgRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, blurSprite ? Color.white : new Color(0.06f, 0.05f, 0.05f));
        blurImg.sprite = blurSprite;
        bgSharp = Img("Sharp", bgRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0));
        bgSharp.sprite = Resources.Load<Sprite>("Menu/menu_bg_sharp");
        bgFigure = Img("Figure", bgRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0));
        bgFigure.sprite = Resources.Load<Sprite>("Menu/menu_bg_figure");
        if (!bgSharp.sprite) bgSharp.enabled = false;
        if (!bgFigure.sprite) bgFigure.enabled = false;

        flicker = Img("Flicker", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);

        // ฝุ่นลอยในแสง
        var dustSprite = DotSprite();
        var dustRoot = new GameObject("Dust", typeof(RectTransform)).transform as RectTransform;
        dustRoot.SetParent(root, false);
        dustRoot.anchorMin = Vector2.zero; dustRoot.anchorMax = Vector2.one; dustRoot.offsetMin = dustRoot.offsetMax = Vector2.zero;
        for (int i = 0; i < dustCount; i++)
        {
            float sz = Random.Range(2f, 7f) * (Random.value < 0.1f ? 2.2f : 1f);
            var d = Img("d", dustRoot, Vector2.zero, Vector2.zero, new Vector2(Random.Range(0f, 1920f), Random.Range(0f, 1080f)), new Vector2(sz, sz),
                        new Color(1f, 0.9f, 0.75f, Random.Range(0.04f, 0.22f)));
            d.sprite = dustSprite;
            dust.Add(d.rectTransform);
            dustVel.Add(new Vector2(Random.Range(-6f, 10f), Random.Range(2f, 12f)));
        }

        // ขอบมืด + ม่านด้านล่างให้อ่านง่าย + นอยส์ฟิล์มจางๆ
        var vig = Img("Vignette", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
        vig.sprite = Resources.Load<Sprite>("Menu/menu_vignette");
        if (!vig.sprite) vig.color = new Color(0, 0, 0, 0.35f);
        var bottomShade = Img("BottomShade", root, Vector2.zero, new Vector2(1f, 0.55f), Vector2.zero, Vector2.zero, Color.white);
        bottomShade.sprite = VerticalGradientSprite();
        var noiseTex = Resources.Load<Texture2D>("UI/noise");
        if (noiseTex)
        {
            noiseRaw = new GameObject("FilmNoise", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            noiseRaw.transform.SetParent(root, false);
            var nr = noiseRaw.rectTransform; nr.anchorMin = Vector2.zero; nr.anchorMax = Vector2.one; nr.offsetMin = nr.offsetMax = Vector2.zero;
            noiseRaw.texture = noiseTex; noiseRaw.color = new Color(0, 0, 0, 0.12f); noiseRaw.raycastTarget = false;
        }

        // เงามืดนุ่มๆ ด้านหลังโลโก้/รายการ ให้อ่านง่ายบนทางเดินที่สว่าง
        var centerShade = Img("CenterShade", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1500, 1100), new Color(0, 0, 0, centerShadeAlpha));
        centerShade.sprite = dustSprite;

        // ---------- โลโก้ (กลางจอ) ----------
        titleGhost = Text("TitleGhost", root, new Vector2(0.5f, 0.5f), new Vector2(0, 330), new Vector2(1200, 90), 70, TextAlignmentOptions.Center, serif);
        titleGhost.text = "THE BLURRED"; titleGhost.characterSpacing = 26f; titleGhost.color = new Color(0.75f, 0.1f, 0.08f, 0f); titleGhost.outlineWidth = 0f;
        var title = Text("Title", root, new Vector2(0.5f, 0.5f), new Vector2(0, 330), new Vector2(1200, 90), 70, TextAlignmentOptions.Center, serif);
        title.text = "THE BLURRED"; title.characterSpacing = 26f; title.color = new Color(0.93f, 0.91f, 0.86f);

        var brush = Img("Brush", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 225), new Vector2(760, 128), new Color(0.92f, 0.9f, 0.85f));
        brush.sprite = BrushSprite(1024, 170, 7);
        var room = Text("Room", brush.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(700, 120), 100, TextAlignmentOptions.Center, type);
        room.text = "r o o m"; room.color = new Color(0.05f, 0.05f, 0.05f); room.fontStyle = FontStyles.Bold; room.outlineWidth = 0f;

        var sub = Text("Subtitle", root, new Vector2(0.5f, 0.5f), new Vector2(0, 135), new Vector2(900, 40), 24, TextAlignmentOptions.Center, thai);
        sub.text = "สุขนิรันดร์ แมนชั่น  ·  ฝั่งธนฯ  ·  กะดึก"; sub.color = new Color(0.66f, 0.63f, 0.58f); sub.characterSpacing = 6f;

        // ---------- รายการ (กลางจอ) ----------
        header = Text("Header", root, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(600, 40), 22, TextAlignmentOptions.Center, thai);
        header.color = new Color(0.62f, 0.6f, 0.56f); header.characterSpacing = 10f;

        var listGo = new GameObject("List", typeof(RectTransform));
        listGo.transform.SetParent(root, false);
        listRoot = (RectTransform)listGo.transform;
        listRoot.anchorMin = listRoot.anchorMax = new Vector2(0.5f, 0.5f); listRoot.pivot = new Vector2(0.5f, 1f);
        listRoot.anchoredPosition = new Vector2(0, 0); listRoot.sizeDelta = new Vector2(900, 400);

        var hl = Img("Highlight", listRoot, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(420, 52), new Color(0.02f, 0.02f, 0.02f, 0.75f));
        hl.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        hl.sprite = BrushSprite(512, 64, 3);
        highlight = hl.rectTransform;

        desc = Text("Description", root, new Vector2(0.5f, 0.5f), new Vector2(0, -300), new Vector2(1100, 90), 22, TextAlignmentOptions.Top, thai);
        desc.color = new Color(0.72f, 0.7f, 0.66f); desc.fontStyle = FontStyles.Italic;

        // ---------- แถบล่าง ----------
        var bar = Img("BottomBar", root, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 46), new Color(0.02f, 0.02f, 0.02f, 0.78f));
        bar.rectTransform.pivot = new Vector2(0.5f, 0f);
        var barLine = Img("BarLine", root, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 46), new Vector2(0, 1), new Color(1f, 1f, 1f, 0.08f));
        barLine.rectTransform.pivot = new Vector2(0.5f, 0f);
        var build = Text("Build", bar.transform, new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(300, 40), 18, TextAlignmentOptions.MidlineLeft, type);
        build.rectTransform.pivot = new Vector2(0, 0.5f); build.text = buildLabel; build.color = new Color(0.5f, 0.48f, 0.45f); build.outlineWidth = 0f;

        hint = Text("Hints", bar.transform, new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(900, 40), 20, TextAlignmentOptions.MidlineRight, thai);
        hint.rectTransform.pivot = new Vector2(1, 0.5f);
        hint.text = "[Enter] ยืนยัน      [Esc] ย้อนกลับ      [W/S] เลือก";
        hint.outlineWidth = 0f;

        if (showDemoJump)
        {
            demoJump = Text("DemoJump", bar.transform, new Vector2(0, 0.5f), new Vector2(300, 0), new Vector2(520, 40), 20, TextAlignmentOptions.MidlineLeft, thai);
            demoJump.rectTransform.pivot = new Vector2(0, 0.5f);
            demoJump.text = demoJumpLabel;
            demoJump.color = new Color(0.6f, 0.58f, 0.55f, 0.8f);
            demoJump.outlineWidth = 0f;
        }
        hint.color = new Color(0.62f, 0.6f, 0.56f);

        // วิธีเล่น
var paperSprite = Resources.Load<Sprite>("UI/paper");
        var paper = Img("HowTo", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1140, 900), paperSprite ? Color.white : new Color(0.86f, 0.82f, 0.72f, 0.97f));
        if (paperSprite) { paper.sprite = paperSprite; paper.type = Image.Type.Sliced; paper.pixelsPerUnitMultiplier = 2f; }
        paper.transform.localRotation = Quaternion.Euler(0, 0, -0.8f);
        howToPanel = paper.gameObject;
        var ht = Text("Title", paper.transform, new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(1000, 70), 40, TextAlignmentOptions.Center, thai);
        ht.text = "<b>วิธีเล่น</b>"; ht.color = new Color(0.12f, 0.08f, 0.04f); ht.outlineWidth = 0f;
        var hb = Text("Body", paper.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -25), new Vector2(980, 720), 24, TextAlignmentOptions.TopLeft, thai);
        hb.color = new Color(0.08f, 0.05f, 0.02f); hb.outlineWidth = 0f; hb.enableAutoSizing = true; hb.fontSizeMin = 15; hb.fontSizeMax = 24;
        hb.text =
            "<b>ปุ่ม</b>   WASD เดิน · Shift วิ่ง · E โต้ตอบ · ค้าง Q ส่องตาแมว · ค้าง R ทำพิธี\n\n" +
            "<b>1 วัน</b>\n" +
            "- กลางวัน: สัมภาษณ์ผู้เช่า / สำรวจ / ถ่ายรูป ได้ 4 แอ็กชัน — จำร่องรอยประจำตัวของแต่ละคนไว้\n" +
            "- ตอนเย็น: ต่อเบาะแสบนกระดานในห้องออฟฟิศ แล้วเอาธูป + ชอล์กไปทำพิธีที่ห้องเก็บของชั้น 3\n" +
            "- กลางคืน: อยู่ในห้องปลอดภัย มีคนมาเคาะ... ตัดสินใจว่าจะเปิดหรือไม่\n\n" +
            "<b>กฎของหอ</b>\n" + KnockRules.RulesText + "\n\n" +
            "<b>สติ</b> เริ่ม 100 · ดูรูปพี่บิ๊กเหนือหิ้งเทียนในล็อบบี้ +67 (วันละครั้ง) · สติหมด = หลุดเข้าไปในอดีต";
        var hh = Text("Hint", paper.transform, new Vector2(0.5f, 0), new Vector2(0, 32), new Vector2(1000, 40), 22, TextAlignmentOptions.Center, thai);
        hh.text = "[Esc] ปิด"; hh.color = new Color(0.3f, 0.2f, 0.1f); hh.outlineWidth = 0f;
        howToPanel.SetActive(false);

        // หน้าคำเตือน: แคนวาสแยก (อยู่บนสุด) พื้นดำทึบ
        var wgo = new GameObject("Warning_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        wgo.transform.SetParent(transform, false);
        var wc = wgo.GetComponent<Canvas>(); wc.renderMode = RenderMode.ScreenSpaceOverlay; wc.sortingOrder = 100;
        var wsc = wgo.GetComponent<CanvasScaler>();
        wsc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; wsc.referenceResolution = new Vector2(1920, 1080); wsc.matchWidthOrHeight = 0.5f;
        warningPanel = wgo;
        var wroot = wgo.transform;
        Img("Black", wroot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.black);
        var content = new GameObject("Content", typeof(RectTransform), typeof(CanvasGroup));
        content.transform.SetParent(wroot, false);
        var crt = (RectTransform)content.transform; crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one; crt.offsetMin = crt.offsetMax = Vector2.zero;
        warningContent = content.GetComponent<CanvasGroup>();
        Img("RedBar", content.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(560, 6), new Color(0.6f, 0.06f, 0.04f));
        var wt = Text("Title", content.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 310), new Vector2(1200, 90), 64, TextAlignmentOptions.Center, thai);
        wt.text = "<b>" + warningTitle + "</b>"; wt.color = new Color(0.85f, 0.12f, 0.08f); wt.characterSpacing = 8f;
        var wb = Text("Body", content.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1250, 480), 30, TextAlignmentOptions.Top, thai);
        wb.text = warningBody; wb.color = new Color(0.9f, 0.88f, 0.84f); wb.lineSpacing = 10f;
        wb.enableAutoSizing = true; wb.fontSizeMin = 18; wb.fontSizeMax = 30;
        var wh = Text("Hint", content.transform, new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(1200, 40), 24, TextAlignmentOptions.Center, thai);
        wh.text = warningHint; wh.color = new Color(0.65f, 0.63f, 0.6f);
        warningPanel.SetActive(false);

        fade = Img("Fade", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.black);
    }

    static Image Img(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.GetComponent<Image>(); img.color = c; img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI Text(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align, TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (font) t.font = font;
        DormFX.ThaiMarks(t);
        t.fontSize = fontSize; t.alignment = align; t.raycastTarget = false; t.text = "";
        t.outlineWidth = 0.12f; t.outlineColor = new Color32(0, 0, 0, 180);
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    // ---------- procedural textures ----------
    /// <summary>แถบพู่กันขอบขรุขระ มีเส้นแห้งๆ (เหมือนโลโก้ requiem)</summary>
    static Sprite BrushSprite(int w, int h, int seed)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var rng = new System.Random(seed);
        float[] dry = new float[h];
        for (int y = 0; y < h; y++) dry[y] = (float)rng.NextDouble();
        var px = new Color32[w * h];
        for (int x = 0; x < w; x++)
        {
            float u = x / (float)w;
            float top = h * (0.12f + 0.08f * Mathf.PerlinNoise(u * 9f, seed)) + (float)rng.NextDouble() * 2.5f;
            float bot = h * (0.88f - 0.08f * Mathf.PerlinNoise(u * 8f, seed + 3)) - (float)rng.NextDouble() * 2.5f;
            for (int y = 0; y < h; y++)
            {
                float v = y / (float)h;
                float startX = w * (0.02f + 0.05f * Mathf.PerlinNoise(v * 6f, seed + 7));
                float endX = w * (0.93f + 0.06f * Mathf.PerlinNoise(v * 5f, seed + 9));
                bool inside = y > top && y < bot && x > startX && x < endX;
                float a = inside ? 1f : 0f;
                if (inside)
                {
                    float tail = Mathf.InverseLerp(endX - w * 0.22f, endX, x);     // ปลายพู่กันแห้ง
                    if (dry[y] < 0.12f + tail * 0.6f && Mathf.PerlinNoise(x * 0.05f, y * 0.9f) < 0.55f + tail * 0.3f) a = 0f;
                    if (rng.NextDouble() < 0.015f) a = 0f;
                }
                px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0, 0.5f));
    }

    static Sprite DotSprite()
    {
        const int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f - 0.5f, n / 2f - 0.5f)) / (n / 2f);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), Vector2.one * 0.5f);
    }

    /// <summary>ดำด้านล่าง จางขึ้นไปด้านบน</summary>
    static Sprite VerticalGradientSprite()
    {
        var tex = new Texture2D(1, 256, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 256; y++) tex.SetPixel(0, y, new Color(0, 0, 0, Mathf.Lerp(0.78f, 0f, Mathf.SmoothStep(0f, 1f, y / 255f))));
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 256), Vector2.one * 0.5f);
    }

    static Sprite GradientSprite()
    {
        var tex = new Texture2D(256, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int x = 0; x < 256; x++)
        {
            float u = x / 255f;
            tex.SetPixel(x, 0, new Color(0, 0, 0, Mathf.Lerp(0.88f, 0f, Mathf.SmoothStep(0f, 1f, u))));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 256, 1), Vector2.one * 0.5f);
    }

    static Sprite ScratchSprite()
    {
        const int w = 8, h = 512;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var rng = new System.Random(3);
        for (int y = 0; y < h; y++)
        {
            float on = Mathf.PerlinNoise(y * 0.08f, 0.5f) > 0.3f ? 1f : 0.2f;
            int c = 3 + (int)(Mathf.PerlinNoise(y * 0.02f, 2f) * 3f) - 1;
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Abs(x - c) <= 1 ? on * (0.6f + (float)rng.NextDouble() * 0.4f) : 0f));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f);
    }
}
