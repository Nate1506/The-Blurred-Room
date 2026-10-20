using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// ภาพจัมป์สแกร์เต็มจอ: เด้งขึ้นมาทันที สั่น ซูมเข้า กะพริบ แล้วตัดดำ
// เรียกจากที่ไหนก็ได้: StartCoroutine(JumpscareImage.Show(sprite, 1.1f))
// เสียง: SoundBank.jumpscare (Resources/SoundBank)
public static class JumpscareImage
{
    /// <summary>โชว์ภาพ seconds วินาที (fillScreen = ขยายเต็มจอแต่ภาพโดนตัดขอบ) (จบแล้วจอเป็นสีดำค้างไว้ ให้ระบบอื่นต่อ)</summary>
    public static IEnumerator Show(Sprite sprite, float seconds, bool fillScreen = false, float shake = 28f, float zoomTo = 1.15f)
    {
        if (!sprite) yield break;
        var go = new GameObject("Jumpscare_Overlay", typeof(Canvas), typeof(CanvasScaler));
        Object.DontDestroyOnLoad(go);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;   // เหนือ HUD ทั้งหมด
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;

        var black = new GameObject("Black", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        black.transform.SetParent(go.transform, false);
        var br = black.rectTransform; br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
        black.color = Color.black;

        var img = new GameObject("Image", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter)).GetComponent<Image>();
        img.transform.SetParent(go.transform, false);
        img.sprite = sprite; img.preserveAspect = false;
        var fit = img.GetComponent<AspectRatioFitter>();
        fit.aspectMode = fillScreen ? AspectRatioFitter.AspectMode.EnvelopeParent : AspectRatioFitter.AspectMode.FitInParent;   // Fit = เห็นทั้งภาพ ขอบข้างดำ
        fit.aspectRatio = sprite.rect.width / sprite.rect.height;
        var rt = img.rectTransform;

        var bank = SoundBank.I;
        if (bank && bank.jumpscare) DormFX.Play2D(bank.jumpscare, bank.jumpscareVolume);
        CameraDynamics.Shake(1f);
        for (float t = 0f; t < seconds; t += Mathf.Min(Time.unscaledDeltaTime, 0.05f))   // กันเฟรมกระตุก (โหลดเสียง) ทำให้ภาพหายไปก่อนเห็น
        {
            float k = t / seconds;
            float s = Mathf.Lerp(1.05f, zoomTo, k * k);
            rt.localScale = new Vector3(s, s, 1f);
            float amp = shake * (1f - k * 0.5f);
            rt.anchoredPosition = new Vector2(Random.Range(-amp, amp), Random.Range(-amp, amp));
            img.color = Random.value < 0.08f ? new Color(1f, 0.6f, 0.6f, 0.6f) : Color.white;   // กะพริบ
            yield return null;
        }
        img.enabled = false;
        yield return null;
        Object.Destroy(go, 0.05f);
    }
}
