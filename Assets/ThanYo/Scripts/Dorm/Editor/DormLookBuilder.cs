using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ThanYo > Blurred Room > 5. Apply hotel look (Inside)
//  - พรมทางเดินลายโรงแรม (Resources/UI/carpet.png) ปูชั้น 1-3  -> object "BR_Look"
//  - ไฟทางเดินชั้น 1-3: สีส้มอุ่น ~2700K, แสงเป็นวงๆ (สว่างใกล้โคม มืดระหว่างโคม)
//  - ไฟทางเดินชั้น 4-6 ปิด (เดโมขึ้นไม่ได้อยู่แล้ว ลดภาระเงา)
// รันซ้ำได้ (ลบ BR_Look แล้วสร้างใหม่; ค่าไฟตั้งทับ)
public static class DormLookBuilder
{
    const string MatPath = "Assets/ThanYo/Materials/Dorm/Carpet_Runner.mat";
    static readonly Color Warm = new Color(1f, 0.78f, 0.55f);

    [MenuItem("ThanYo/Blurred Room/5. Apply hotel look (Inside)")]
    public static void Apply()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "Inside") { Debug.LogError("[Look] Open the Inside scene first."); return; }
        Physics.SyncTransforms();

        // ---- carpet material ----
        var texPath = "Assets/ThanYo/Resources/UI/carpet.png";
        var ti = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (ti) { ti.textureType = TextureImporterType.Default; ti.wrapMode = TextureWrapMode.Repeat; ti.anisoLevel = 8; ti.maxTextureSize = 1024; ti.SaveAndReimport(); }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (!mat)
        {
            if (!AssetDatabase.IsValidFolder("Assets/ThanYo/Materials/Dorm")) AssetDatabase.CreateFolder("Assets/ThanYo/Materials", "Dorm");
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, MatPath);
        }
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", new Color(0.85f, 0.8f, 0.78f));
        mat.SetFloat("_Smoothness", 0.04f);
        EditorUtility.SetDirty(mat);

        // ---- runners ----
        var old = GameObject.Find("BR_Look");
        if (old) Object.DestroyImmediate(old);
        var root = new GameObject("BR_Look").transform;
        for (int floor = 1; floor <= 3; floor++)
        {
            float y = (floor - 1) * 2.8f;
            Vector3 mid = new Vector3(11f, y + 1.2f, 5.0f);
            float floorY = Physics.Raycast(mid, Vector3.down, out var hit, 2f, ~(1 << 2), QueryTriggerInteraction.Ignore) ? hit.point.y : y;
            float xMin = Physics.Raycast(mid, Vector3.left, out var hl, 25f, ~(1 << 2), QueryTriggerInteraction.Ignore) ? hl.point.x : 0.6f;
            float xMax = Physics.Raycast(mid, Vector3.right, out var hr, 25f, ~(1 << 2), QueryTriggerInteraction.Ignore) ? hr.point.x : 21.6f;
            xMin += 0.25f; xMax -= 0.25f;

            var go = new GameObject("Carpet_Floor" + floor, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(0, floorY + 0.006f, 0);
            float len = xMax - xMin, z0 = 4.3f, z1 = 5.7f;
            var mesh = new Mesh { name = "CarpetRunner_" + floor };
            mesh.vertices = new[] { new Vector3(xMin, 0, z0), new Vector3(xMax, 0, z0), new Vector3(xMin, 0, z1), new Vector3(xMax, 0, z1) };
            float tiles = len / 1.6f;   // ลาย 1 ชุดทุก 1.6 ม.
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(tiles, 0), new Vector2(0, 1), new Vector2(tiles, 1) };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ---- corridor lights ----
        var il = GameObject.Find("Interior_Lighting");
        int warm = 0, off = 0;
        if (il)
        {
            for (int floor = 1; floor <= 6; floor++)
            {
                var f = il.transform.Find("Floor_" + floor);
                if (!f) continue;
                if (floor >= 4) { f.gameObject.SetActive(false); off++; continue; }
                foreach (var l in f.GetComponentsInChildren<Light>(true))
                {
                    l.useColorTemperature = false;
                    l.color = Warm;
                    l.intensity = 4.2f;
                    l.range = 4.0f;
                    l.shadows = LightShadows.Soft;
                    l.shadowStrength = 0.85f;
                    EditorUtility.SetDirty(l);
                    warm++;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Look] carpet x3, warm lights " + warm + ", floors 4-6 lights off (" + off + " groups)");
    }
}
