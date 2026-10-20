using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ตัวช่วยสร้าง UI แบบกระดาษ (ทะเบียนผู้เช่า, หน้าวิธีเล่น, ป๊อปอัปสอน) — ใช้ร่วมกัน หน้าตาเดียวกันทั้งเกม
public static class PaperUI
{
    public static Canvas MakeCanvas(string name, int order)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
        var c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = order;
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 1f;
        return c;
    }

    public static Image Img(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        var r = img.rectTransform; r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;
        img.color = color; img.raycastTarget = false;
        if (sprite) { img.sprite = sprite; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 2f; }
        return img;
    }

    public static Image Fill(string name, Transform parent, Color color)
    {
        var img = Img(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, color);
        var r = img.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        return img;
    }

    public static TextMeshProUGUI Text(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align, Color color)
    {
        var t = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        t.transform.SetParent(parent, false);
        var r = t.rectTransform; r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;
        if (DormFX.ThaiFont) t.font = DormFX.ThaiFont;
        t.fontSize = fontSize; t.alignment = align; t.color = color; t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        DormFX.ThaiMarks(t);
        return t;
    }

    public static Sprite Paper => Resources.Load<Sprite>("UI/paper");
    public static Sprite Check => Resources.Load<Sprite>("UI/check");
    public static Sprite KeyCap => Resources.Load<Sprite>("UI/key");

    public static readonly Color Ink = new Color(0.16f, 0.12f, 0.08f);
    public static readonly Color InkSoft = new Color(0.33f, 0.27f, 0.2f);
    public static readonly Color RedPen = new Color(0.72f, 0.08f, 0.06f, 0.92f);
}
