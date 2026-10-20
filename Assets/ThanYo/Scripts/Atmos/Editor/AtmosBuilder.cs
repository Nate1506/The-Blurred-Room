using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ThanYo > Blurred Room > 11. บรรยากาศ: ฝุ่น ควัน เงา
// ใส่ระบบบรรยากาศลงซีนที่เปิดอยู่ (ThanYo = หมอกเรี่ยพื้น + ฝุ่นใต้ไฟถนน,
// Inside = ฝุ่นทุกทางเดิน + หมอกจางๆ ชั้น 1/3) — กดซ้ำ = สร้างใหม่ทั้งชุด
public static class AtmosBuilder
{
    const string Tex = "Assets/ThanYo/Textures/Atmos/";
    const string Root = "BR_Atmos";

    [MenuItem("ThanYo/Blurred Room/11. บรรยากาศ: ฝุ่น ควัน เงา")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "ThanYo" && scene.name != "Inside") { EditorUtility.DisplayDialog("Atmos", "เปิดซีน ThanYo หรือ Inside ก่อน", "OK"); return; }
        FixImports();
        var old = GameObject.Find(Root);
        if (old) Undo.DestroyObjectImmediate(old);
        var root = new GameObject(Root);
        Undo.RegisterCreatedObjectUndo(root, "Atmos");

        var smokeA = AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + "fog_smoke_a.png");
        var smokeB = AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + "fog_smoke_b.png");
        var smokes = new[] { smokeA, smokeB };
        unlitMat = SourceMat("Atmos_Unlit", "Universal Render Pipeline/Particles/Unlit");
        litMat = SourceMat("Atmos_SimpleLit", "Universal Render Pipeline/Particles/Simple Lit");

        if (scene.name == "ThanYo") Outside(root.transform, smokes);
        else Inside(root.transform, smokes);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeObject = root;
        Debug.Log("[Atmos] ใส่บรรยากาศใน " + scene.name + " แล้ว (" + Root + ")");
    }

    // ---------------- ลานหน้าหอ ----------------
    static void Outside(Transform root, Texture2D[] smokes)
    {
        var low = Fog(root, "GroundFog_Yard", new Vector3(-24f, 0f, 22f), new Vector3(50f, 0.4f, 46f), smokes, 70);
        low.widthRange = new Vector2(6f, 12f); low.heightRange = new Vector2(0.9f, 2f); low.opacity = 0.2f; low.brightness = 0.8f;

        var far = Fog(root, "Fog_ForestWall", new Vector3(-24f, 0f, 22f), new Vector3(85f, 0.4f, 75f), smokes, 30);
        far.widthRange = new Vector2(14f, 24f); far.heightRange = new Vector2(2.5f, 5f); far.opacity = 0.15f; far.brightness = 0.85f;
        far.wind = new Vector3(0.15f, 0f, 0.05f);

        var lampDust = Dust(root, "Dust_StreetLamp", new Vector3(-16.7f, 2.4f, 21.6f), new Vector3(6f, 4f, 6f), 160);
        lampDust.opacity = 0.45f; lampDust.size = new Vector2(0.01f, 0.03f);
    }

    // ---------------- ในตึก ----------------
    static void Inside(Transform root, Texture2D[] smokes)
    {
        for (int f = 1; f <= 6; f++)
        {
            float y = 2.8f * (f - 1);
            var d = Dust(root, "Dust_Corridor_F" + f, new Vector3(11f, y + 1.3f, 5f), new Vector3(21.4f, 2.4f, 1.8f), f <= 3 ? 280 : 160);
            d.opacity = 0.3f;
        }
        var lobby = Dust(root, "Dust_Lobby", new Vector3(5.2f, 1.3f, 2f), new Vector3(5.6f, 2.4f, 3.6f), 200);
        lobby.opacity = 0.35f;

        foreach (var (name, y, alpha) in new[] { ("Mist_Corridor_F1", 0f, 0.12f), ("Mist_Corridor_F3", 5.6f, 0.18f) })
        {
            var m = Fog(root, name, new Vector3(11f, y, 5f), new Vector3(21f, 0.05f, 1.4f), smokes, 12);
            m.widthRange = new Vector2(2f, 4f); m.heightRange = new Vector2(0.45f, 0.9f); m.sink = 0.15f;
            m.matchSceneFog = false; m.tint = new Color(0.45f, 0.44f, 0.42f); m.brightness = 1f;
            m.opacity = alpha; m.fadeNear = 2.5f; m.fadeGone = 0.6f;
            m.wind = new Vector3(0.06f, 0f, 0f);
        }
    }

    // ---------------- helpers ----------------
    static Material unlitMat, litMat;

    static Material SourceMat(string name, string shader)
    {
        const string dir = "Assets/ThanYo/Materials/Atmos";
        if (!AssetDatabase.IsValidFolder(dir)) { System.IO.Directory.CreateDirectory(dir); AssetDatabase.Refresh(); }
        string path = dir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, path); }
        return m;
    }

    static FogCards Fog(Transform root, string name, Vector3 pos, Vector3 size, Texture2D[] tex, int count)
    {
        var f = new GameObject(name).AddComponent<FogCards>();
        f.transform.SetParent(root, false);
        f.transform.position = pos;
        f.areaSize = size; f.textures = tex; f.count = count;
        f.litSource = litMat; f.unlitSource = unlitMat;
        return f;
    }

    static DustMotes Dust(Transform root, string name, Vector3 pos, Vector3 size, int amount)
    {
        var d = new GameObject(name).AddComponent<DustMotes>();
        d.transform.SetParent(root, false);
        d.transform.position = pos;
        d.areaSize = size; d.amount = amount; d.unlitSource = unlitMat;
        return d;
    }

    static Transform Spot(Transform parent, string name, Vector3 pos)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.position = pos;
        return t;
    }

    static void FixImports()
    {
        foreach (var n in new[] { "fog_smoke_a.png", "fog_smoke_b.png" })
        {
            var ti = AssetImporter.GetAtPath(Tex + n) as TextureImporter;
            if (!ti) continue;
            if (ti.alphaIsTransparency && ti.wrapMode == TextureWrapMode.Clamp) continue;
            ti.textureType = TextureImporterType.Default;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.mipmapEnabled = true;
            ti.SaveAndReimport();
        }
    }
}
