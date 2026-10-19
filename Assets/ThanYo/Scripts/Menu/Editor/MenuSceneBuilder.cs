using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// ThanYo > Blurred Room > 3. Build Main Menu scene
// สร้าง/อัปเดตฟอนต์เมนู (Georgia + Courier New จาก Windows), ตั้งรูปพื้นหลังเป็น Sprite,
// สร้างซีน Assets/ThanYo/MainMenu.unity แล้วใส่เป็นซีนแรกใน Build Settings
public static class MenuSceneBuilder
{
    const string MenuRes = "Assets/ThanYo/Resources/Menu";
    const string ScenePath = "Assets/ThanYo/MainMenu.unity";

    [MenuItem("ThanYo/Blurred Room/3. Build Main Menu scene")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(MenuRes)) AssetDatabase.CreateFolder("Assets/ThanYo/Resources", "Menu");

        // ---- background ----
        string bg = MenuRes + "/menu_bg.png";
        var ti = AssetImporter.GetAtPath(bg) as TextureImporter;
        if (ti)
        {
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.maxTextureSize = 2048; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
        }
        else Debug.LogWarning("[Menu] missing " + bg);

        // ---- fonts ----
        Font("C:/Windows/Fonts/georgia.ttf", "Assets/ThanYo/Fonts/georgia.ttf", MenuRes + "/MenuSerif.asset", "MenuSerif");
        Font("C:/Windows/Fonts/courbd.ttf", "Assets/ThanYo/Fonts/courbd.ttf", MenuRes + "/MenuType.asset", "MenuType");

        // ---- scene ----
        if (EditorSceneManager.GetActiveScene().isDirty) EditorSceneManager.SaveOpenScenes();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        new GameObject("MainMenu", typeof(MainMenu));
        EditorSceneManager.SaveScene(scene, ScenePath);

        // ---- build settings: เมนูเป็นซีนแรก ----
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        list.RemoveAll(s => s.path == ScenePath);
        list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();

        AssetDatabase.SaveAssets();
        Debug.Log("[Menu] Built " + ScenePath + " (first scene in Build Settings)");
    }

    const string SkipKey = "ThanYo.SkipMainMenu";

    [MenuItem("ThanYo/Blurred Room/Skip main menu when pressing Play")]
    static void ToggleSkip() => EditorPrefs.SetBool(SkipKey, !EditorPrefs.GetBool(SkipKey, false));

    [MenuItem("ThanYo/Blurred Room/Skip main menu when pressing Play", true)]
    static bool ToggleSkipValidate()
    {
        Menu.SetChecked("ThanYo/Blurred Room/Skip main menu when pressing Play", EditorPrefs.GetBool(SkipKey, false));
        return true;
    }

    static void Font(string system, string local, string assetPath, string name)
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath)) return;
        if (!File.Exists(local))
        {
            if (!File.Exists(system)) { Debug.LogWarning("[Menu] font not found " + system); return; }
            if (!AssetDatabase.IsValidFolder("Assets/ThanYo/Fonts")) AssetDatabase.CreateFolder("Assets/ThanYo", "Fonts");
            File.Copy(system, local);
            AssetDatabase.ImportAsset(local);
        }
        var font = AssetDatabase.LoadAssetAtPath<Font>(local);
        var fa = TMP_FontAsset.CreateFontAsset(font, 90, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        fa.name = name;
        AssetDatabase.CreateAsset(fa, assetPath);
        if (fa.material) { fa.material.name = name + " Material"; AssetDatabase.AddObjectToAsset(fa.material, fa); }
        foreach (var tex in fa.atlasTextures) if (tex) { tex.name = name + " Atlas"; AssetDatabase.AddObjectToAsset(tex, fa); }
        var thai = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ThanYo/Resources/ThaiFont.asset");
        if (thai) fa.fallbackFontAssetTable = new List<TMP_FontAsset> { thai };
        EditorUtility.SetDirty(fa);
    }
}
