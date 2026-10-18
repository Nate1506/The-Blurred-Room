using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class DeleteStaticMeshActors
{
    private const string PrefabSearchFolder = "Assets/LeartesStudios/AbondonedDistrict/Prefabs";

    [MenuItem("Tools/Delete All StaticMeshActor (Prefabs & Scene)")]
    public static void DeleteAllPrefabsAndScene()
    {
        if (!EditorUtility.DisplayDialog(
            "Delete All StaticMeshActor",
            "This will delete all GameObjects named 'StaticMeshActor*' from all PF_Building prefabs and from the active scene.\n\nAre you sure you want to proceed?",
            "Yes, Delete All",
            "Cancel"))
        {
            return;
        }

        int totalDeletedInPrefabs = DeleteFromPrefabs();
        int totalDeletedInScene = DeleteFromActiveScene();

        string summary = $"StaticMeshActor Deletion Complete!\n\n" +
                         $"• Deleted from Prefabs: {totalDeletedInPrefabs:N0}\n" +
                         $"• Deleted from Active Scene: {totalDeletedInScene:N0}";

        Debug.Log($"[DeleteStaticMeshActors] {summary}");
        EditorUtility.DisplayDialog("StaticMeshActor Cleanup", summary, "OK");
    }

    [MenuItem("Tools/Delete StaticMeshActor (Active Scene Only)")]
    public static void DeleteSceneOnly()
    {
        if (!EditorUtility.DisplayDialog(
            "Delete Scene StaticMeshActors",
            "This will delete all GameObjects named 'StaticMeshActor*' in the active scene.\n\nProceed?",
            "Yes, Delete",
            "Cancel"))
        {
            return;
        }

        int count = DeleteFromActiveScene();
        string summary = $"Deleted {count:N0} StaticMeshActor objects from the active scene.";
        Debug.Log($"[DeleteStaticMeshActors] {summary}");
        EditorUtility.DisplayDialog("Scene StaticMeshActor Cleanup", summary, "OK");
    }

    private static int DeleteFromPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabSearchFolder });
        int totalDeleted = 0;
        int processedPrefabs = 0;

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

                EditorUtility.DisplayProgressBar(
                    "Deleting StaticMeshActors from Prefabs",
                    $"Processing {fileName} ({i + 1}/{guids.Length})...",
                    (float)i / guids.Length
                );

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                    continue;

                // Collect all GameObjects matching StaticMeshActor
                Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
                List<GameObject> toDelete = new List<GameObject>();

                foreach (Transform t in allTransforms)
                {
                    if (t != null && t.gameObject != root && t.name.StartsWith("StaticMeshActor"))
                    {
                        toDelete.Add(t.gameObject);
                    }
                }

                if (toDelete.Count > 0)
                {
                    int countForPrefab = toDelete.Count;
                    // Delete collected objects (checking for null as parent destruction deletes children)
                    for (int j = toDelete.Count - 1; j >= 0; j--)
                    {
                        if (toDelete[j] != null)
                        {
                            Object.DestroyImmediate(toDelete[j]);
                        }
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    totalDeleted += countForPrefab;
                    processedPrefabs++;
                    Debug.Log($"[DeleteStaticMeshActors] Removed {countForPrefab:N0} StaticMeshActors from {fileName}");
                }

                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[DeleteStaticMeshActors] Processed {processedPrefabs} prefabs, removed a total of {totalDeleted:N0} StaticMeshActors.");
        return totalDeleted;
    }

    private static int DeleteFromActiveScene()
    {
        Transform[] allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<GameObject> toDelete = new List<GameObject>();

        foreach (Transform t in allTransforms)
        {
            if (t != null && t.name.StartsWith("StaticMeshActor"))
            {
                toDelete.Add(t.gameObject);
            }
        }

        int count = 0;
        for (int i = toDelete.Count - 1; i >= 0; i--)
        {
            if (toDelete[i] != null)
            {
                Undo.DestroyObjectImmediate(toDelete[i]);
                count++;
            }
        }

        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        return count;
    }
}
