using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEditor.SceneManagement;

public static class EnableBuildingShadows
{
    private const string PrefabFolder = "Assets/LeartesStudios/AbondonedDistrict/Prefabs";

    [MenuItem("Tools/Enable Building Shadows (Standard)")]
    public static void EnableShadowsStandard()
    {
        ProcessAllBuildings(ShadowCastingMode.On);
    }

    [MenuItem("Tools/Enable Building Shadows (Two-Sided)")]
    public static void EnableShadowsTwoSided()
    {
        ProcessAllBuildings(ShadowCastingMode.TwoSided);
    }

    private static void ProcessAllBuildings(ShadowCastingMode shadowMode)
    {
        int modifiedPrefabs = 0;
        int totalPrefabs = 0;
        int modifiedPrefabRenderers = 0;
        int totalPrefabRenderers = 0;

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

            if (!fileName.StartsWith("PF_Building"))
                continue;

            totalPrefabs++;

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = scope.prefabContentsRoot;
                MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
                bool modified = false;

                foreach (MeshRenderer r in renderers)
                {
                    totalPrefabRenderers++;
                    if (r.shadowCastingMode != shadowMode || !r.receiveShadows)
                    {
                        r.shadowCastingMode = shadowMode;
                        r.receiveShadows = true;
                        modifiedPrefabRenderers++;
                        modified = true;
                    }
                }

                if (modified)
                {
                    modifiedPrefabs++;
                }
            }
        }

        AssetDatabase.SaveAssets();

        // Also process any building instances in the currently open scene
        int sceneRenderersModified = 0;
        MeshRenderer[] sceneRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool sceneModified = false;

        foreach (MeshRenderer r in sceneRenderers)
        {
            Transform current = r.transform;
            bool isBuilding = false;
            while (current != null)
            {
                if (current.name.StartsWith("PF_Building"))
                {
                    isBuilding = true;
                    break;
                }
                current = current.parent;
            }

            if (isBuilding)
            {
                if (r.shadowCastingMode != shadowMode || !r.receiveShadows)
                {
                    Undo.RecordObject(r, "Enable Building Shadows");
                    r.shadowCastingMode = shadowMode;
                    r.receiveShadows = true;
                    sceneRenderersModified++;
                    sceneModified = true;
                }
            }
        }

        if (sceneModified)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        string message = $"Building Shadows Enabled ({shadowMode})!\n\n" +
                         $"• Prefabs checked: {totalPrefabs}\n" +
                         $"• Prefabs updated: {modifiedPrefabs}\n" +
                         $"• Prefab Renderers verified: {totalPrefabRenderers} (updated: {modifiedPrefabRenderers})\n" +
                         $"• Scene Renderers updated: {sceneRenderersModified}";

        Debug.Log("[EnableBuildingShadows] " + message);
        EditorUtility.DisplayDialog("Building Shadows", message, "OK");
    }
}
