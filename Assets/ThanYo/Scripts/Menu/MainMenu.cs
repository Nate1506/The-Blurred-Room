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

// หน้าเมนูหลัก (สไตล์ RE Requiem): ภาพทางเดินชั้น 2 เป็นพื้นหลัง, โลโก้ + แถบพู่กัน, รายการเมนูทางซ้าย, คำอธิบายทางขวา
// สร้าง UI เองทั้งหมดตอนเริ่ม (เหมือน GameHUD) — รูป/ฟอนต์อยู่ใน Resources/Menu และ Resources/ThaiFont
// คีย์บอร์ด: W/S หรือ ลูกศร เลือก, Enter/E/Space ยืนยัน, Esc/Backspace ย้อนกลับ, A/D ปรับค่าในหน้าตั้งค่า   เมาส์: ชี้ + คลิก
public class MainMenu : MonoBehaviour
{
    [Tooltip("ซีนที่เริ่มเล่น")]
    public string gameScene = "Inside";

    class Item
    {
        public string label, desc;
        public System.Action onConfirm;
        public System.Func<string> value;       // หน้าตั้งค่า: ข้อความค่าปัจจุบัน
        public System.Action<int> onAdjust;     // หน้าตั้งค่า: -1 / +1
        public TextMeshProUGUI text;
        public RectTransform rect;
    }

    enum Page { Main, Settings, HowTo }

    TMP_FontAsset thai, serif, type;
    RectTransform listRoot, bgRect, highlight;
    TextMeshProUGUI header, desc, hint;
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
            new Item { label = "เริ่มเกม", desc = "เริ่มกะแรกในฐานะผู้ดูแล 'สุขนิรันดร์ แมนชั่น'\nเอาตัวรอดให้ครบ 3 คืน", onConfirm = () => StartCoroutine(StartGame()) },
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

    void Back()
    {
        if (page == Page.HowTo) { howToPanel.SetActive(false); page = Page.Main; return; }
        if (page == Page.Settings) ShowMain();
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

        if (page == Page.HowTo)
        {
            if (Confirm() || BackPressed() || Clicked()) Back();
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
            var t = Text("Item_" + i, listRoot, new Vector2(0, 1), new Vector2(60, -i * 54f), new Vector2(520, 50), 30, TextAlignmentOptions.MidlineLeft, thai);
            t.rectTransform.pivot = new Vector2(0, 1);
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
            string v = it.value != null ? "      <  " + it.value() + "  >" : "";
            it.text.text = it.label + v;
            it.text.color = i == index ? On : Off;
        }
        var sel = items[index].rect;
        highlight.SetParent(listRoot, false);
        highlight.anchoredPosition = new Vector2(30, sel.anchoredPosition.y - 25f);
        highlight.SetAsFirstSibling();
        desc.rectTransform.anchoredPosition = new Vector2(desc.rectTransform.anchoredPosition.x, listRoot.anchoredPosition.y + sel.anchoredPosition.y - 4f);
        desc.text = items[index].desc;
    }

    static void Tick() => DormFX.Play2D(DormFX.TapClip, 0.25f, 0.55f);

    // =====================================================================
    //  atmosphere
    // =====================================================================
    float bgT;
    void AnimateBackground()
    {
        bgT += Time.deltaTime;
        float s = 1.04f + Mathf.Sin(bgT * 0.05f) * 0.03f;                 // ซูมเข้าออกช้าๆ
        bgRect.localScale = new Vector3(s, s, 1f);
        bgRect.anchoredPosition = new Vector2(Mathf.Sin(bgT * 0.07f) * 12f, Mathf.Cos(bgT * 0.05f) * 6f);
        float f = Random.value < 0.012f ? Random.Range(0.25f, 0.55f) : Mathf.MoveTowards(flicker.color.a, 0f, Time.deltaTime * 2.5f);
        flicker.color = new Color(0, 0, 0, f);                             // ไฟกระพริบเป็นพักๆ
    }

    IEnumerator DistantKnock()
    {
        int n = Random.value < 0.5f ? 3 : 5;                                // บางทีก็เคาะ 5 ครั้ง...
        for (int i = 0; i < n; i++) { DormFX.Play2D(DormFX.KnockClip, 0.18f, 0.85f); yield return new WaitForSeconds(i == 2 ? 2f : 0.55f); }
    }

    void StartAmbience()
    {
        var src = gameObject.AddComponent<AudioSource>();
        const int rate = 44100, len = rate * 4;
        var data = new float[len];
        float a = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)rate;
            a = Mathf.Lerp(a, Random.value * 2f - 1f, 0.02f);              // ลมเบาๆ (low-pass noise)
            data[i] = a * 0.35f + Mathf.Sin(2f * Mathf.PI * 50f * t) * 0.05f + Mathf.Sin(2f * Mathf.PI * 100f * t) * 0.02f; // ไฟฟลูออเรสเซนต์ฮัม
        }
        var clip = AudioClip.Create("menu_drone", len, 1, rate, false);
        clip.SetData(data, 0);
        src.clip = clip; src.loop = true; src.volume = 0.35f; src.spatialBlend = 0f;
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

        // background: ทางเดินชั้น 2
        var bgSprite = Resources.Load<Sprite>("Menu/menu_bg");
        var bg = Img("Background", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, bgSprite ? Color.white : new Color(0.08f, 0.08f, 0.08f));
        bg.sprite = bgSprite;
        bgRect = bg.rectTransform;
        if (bgSprite)
        {
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = bgSprite.rect.width / bgSprite.rect.height;
        }
        flicker = Img("Flicker", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);

        // เงาดำฝั่งซ้ายให้อ่านเมนูง่าย
        var shade = Img("LeftShade", root, Vector2.zero, new Vector2(0.7f, 1f), Vector2.zero, Vector2.zero, Color.white);
        shade.sprite = GradientSprite();

        // เส้นขูดแนวตั้งทางซ้าย
        var line = Img("ScratchLine", root, new Vector2(0, 0), new Vector2(0, 1), new Vector2(68, 0), new Vector2(6, -120), new Color(0.85f, 0.84f, 0.8f, 0.8f));
        line.sprite = ScratchSprite();

        // โลโก้
        var title = Text("Title", root, new Vector2(0, 1), new Vector2(140, -190), new Vector2(900, 80), 58, TextAlignmentOptions.BottomLeft, serif);
        title.rectTransform.pivot = new Vector2(0, 0);
        title.text = "THE BLURRED"; title.characterSpacing = 18f; title.color = new Color(0.93f, 0.92f, 0.88f);

        var brush = Img("Brush", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(105, -205), new Vector2(720, 120), new Color(0.92f, 0.91f, 0.87f));
        brush.rectTransform.pivot = new Vector2(0, 1);
        brush.sprite = BrushSprite(1024, 170, 7);
        var room = Text("Room", brush.transform, new Vector2(0, 0.5f), new Vector2(70, 2), new Vector2(620, 120), 96, TextAlignmentOptions.MidlineLeft, type);
        room.rectTransform.pivot = new Vector2(0, 0.5f);
        room.text = "r o o m"; room.color = new Color(0.06f, 0.06f, 0.06f); room.fontStyle = FontStyles.Bold; room.outlineWidth = 0f;

        var sub = Text("Subtitle", root, new Vector2(0, 1), new Vector2(145, -345), new Vector2(700, 40), 24, TextAlignmentOptions.TopLeft, thai);
        sub.rectTransform.pivot = new Vector2(0, 1);
        sub.text = "สุขนิรันดร์ แมนชั่น · ฝั่งธนฯ"; sub.color = new Color(0.62f, 0.6f, 0.56f); sub.characterSpacing = 4f;

        // หัวข้อ + รายการ
        header = Text("Header", root, new Vector2(0, 1), new Vector2(145, -455), new Vector2(500, 44), 28, TextAlignmentOptions.MidlineLeft, thai);
        header.rectTransform.pivot = new Vector2(0, 1);
        header.color = new Color(0.82f, 0.8f, 0.76f); header.characterSpacing = 6f;

        var listGo = new GameObject("List", typeof(RectTransform));
        listGo.transform.SetParent(root, false);
        listRoot = (RectTransform)listGo.transform;
        listRoot.anchorMin = listRoot.anchorMax = new Vector2(0, 1); listRoot.pivot = new Vector2(0, 1);
        listRoot.anchoredPosition = new Vector2(115, -515); listRoot.sizeDelta = new Vector2(600, 400);

        var hl = Img("Highlight", listRoot, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(470, 44), new Color(0.02f, 0.02f, 0.02f, 0.92f));
        hl.rectTransform.pivot = new Vector2(0, 0.5f);
        hl.sprite = BrushSprite(512, 64, 3);
        highlight = hl.rectTransform;

        desc = Text("Description", root, new Vector2(0, 1), new Vector2(720, -515), new Vector2(760, 120), 26, TextAlignmentOptions.TopLeft, thai);
        desc.rectTransform.pivot = new Vector2(0, 1);
        desc.color = new Color(0.88f, 0.87f, 0.83f);

        hint = Text("Hints", root, new Vector2(0, 0), new Vector2(145, 60), new Vector2(1200, 40), 24, TextAlignmentOptions.BottomLeft, thai);
        hint.rectTransform.pivot = new Vector2(0, 0);
        hint.text = "[Enter] ยืนยัน      [Esc] ย้อนกลับ      [W/S] เลือก";
        hint.color = new Color(0.75f, 0.74f, 0.7f);

        // วิธีเล่น
var paperSprite = Resources.Load<Sprite>("UI/paper");
        var paper = Img("HowTo", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120, 0), new Vector2(1140, 940), paperSprite ? Color.white : new Color(0.86f, 0.82f, 0.72f, 0.97f));
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
