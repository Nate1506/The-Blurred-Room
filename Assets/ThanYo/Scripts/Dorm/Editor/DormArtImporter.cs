using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// ThanYo > Blurred Room > 4. Import UI art + realistic grade
//  - ตั้งค่า import ของรูปใน Resources/UI (กระดาษ 9-slice, แถบชอล์ก, กรอบปุ่ม, VHS, เกรน)
//  - สร้าง Volume "BR_Grade_Volume" ในซีนที่เปิดอยู่: โทนเย็นอมฟ้า ซีด เกรนฟิล์ม ขอบมืด (แนวภาพสมจริง)
//    priority สูงกว่า Inside_Volume จึงทับเฉพาะค่าที่ตั้ง (ค่าเช้า/เย็น/คืนของ TimeOfDay ยังทำงาน)
public static class DormArtImporter
{
    const string UI = "Assets/ThanYo/Resources/UI/";
    const string GradePath = "Assets/ThanYo/Inside/BR_Grade.asset";

    [MenuItem("ThanYo/Blurred Room/4. Import UI art + warm hotel grade")]
    public static void Run()
    {
        Sprite("paper", 140); Sprite("paper_dark", 140);
        Sprite("bar", 0); Sprite("bar_bg", 0); Sprite("key", 0); Sprite("icon_eye", 0); Sprite("vhs_corner", 0); Sprite("check", 0); Sprite("shade_right", 0);
        Sprite("grime", 0, 2048);
        Tiled("noise", FilterMode.Bilinear); Tiled("scanline", FilterMode.Point);
        AssetDatabase.SaveAssets();
        BuildGrade();
        Debug.Log("[Dorm] UI art imported + grade volume ready");
    }

    static void Sprite(string name, int border, int maxSize = 1024)
    {
        var ti = AssetImporter.GetAtPath(UI + name + ".png") as TextureImporter;
        if (!ti) { Debug.LogWarning("[Dorm] missing UI/" + name); return; }
        ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
        ti.mipmapEnabled = false; ti.alphaIsTransparency = true; ti.maxTextureSize = maxSize;
        ti.spriteBorder = new Vector4(border, border, border, border);
        var s = new TextureImporterSettings(); ti.ReadTextureSettings(s); s.spriteMeshType = SpriteMeshType.FullRect; ti.SetTextureSettings(s);
        ti.SaveAndReimport();
    }

    static void Tiled(string name, FilterMode filter)
    {
        var ti = AssetImporter.GetAtPath(UI + name + ".png") as TextureImporter;
        if (!ti) { Debug.LogWarning("[Dorm] missing UI/" + name); return; }
        ti.textureType = TextureImporterType.Default; ti.wrapMode = TextureWrapMode.Repeat; ti.filterMode = filter;
        ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.alphaIsTransparency = true;
        ti.SaveAndReimport();
    }

    static void BuildGrade()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GradePath);
        if (!profile)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, GradePath);
        }
        T Get<T>() where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out var c)) { c = profile.Add<T>(true); AssetDatabase.AddObjectToAsset(c, profile); }
            c.active = true;
            return c;
        }

        // โทน "โรงแรมเก่าตอนดึก": อุ่นอมส้ม มืด คอนทราสต์สูง แสงจากโคมเป็นวงๆ เงาลึก
        var ca = Get<ColorAdjustments>();
        ca.postExposure.Override(-0.25f); ca.saturation.Override(-20f); ca.contrast.Override(24f); ca.colorFilter.Override(new Color(1f, 0.95f, 0.88f));
        var wb = Get<WhiteBalance>();
        wb.temperature.Override(8f); wb.tint.Override(2f);
        var smh = Get<ShadowsMidtonesHighlights>();
        smh.shadows.Override(new Vector4(1.0f, 0.94f, 0.9f, -0.12f));     // เงาอุ่นและลึก
        smh.midtones.Override(new Vector4(1.0f, 0.97f, 0.92f, -0.04f));
        smh.highlights.Override(new Vector4(1.04f, 0.99f, 0.9f, 0f));
        var bloom = Get<Bloom>();
        bloom.threshold.Override(0.85f); bloom.intensity.Override(0.6f); bloom.scatter.Override(0.75f); bloom.tint.Override(new Color(1f, 0.9f, 0.75f));
        var grain = Get<FilmGrain>();
        grain.type.Override(FilmGrainLookup.Medium3); grain.intensity.Override(0.04f); grain.response.Override(0.6f);
        var vig = Get<Vignette>();
        vig.intensity.Override(0.42f); vig.smoothness.Override(0.5f);
        var chroma = Get<ChromaticAberration>();
        chroma.intensity.Override(0.08f);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "Inside") { Debug.Log("[Dorm] Grade profile saved. Open Inside and run again to add the volume."); return; }
        var go = GameObject.Find("BR_Grade_Volume");
        if (!go) go = new GameObject("BR_Grade_Volume", typeof(Volume));
        var v = go.GetComponent<Volume>();
        v.isGlobal = true; v.priority = 10f; v.weight = 1f; v.sharedProfile = profile;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
