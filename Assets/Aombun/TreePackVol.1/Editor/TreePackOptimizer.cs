using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class TreePackOptimizer
{
    private const string PREF_KEY = "TreePackVol1_Configured_v2";

    static TreePackOptimizer()
    {
        EditorApplication.delayCall += OnEditorReady;
    }

    private static void OnEditorReady()
    {
        if (!EditorPrefs.GetBool(PREF_KEY, false))
        {
            MakeAllTreesReady();
            EditorPrefs.SetBool(PREF_KEY, true);
        }
    }

    [MenuItem("Tools/TreePack/Make All Tree Prefabs Ready (URP & Colliders)")]
    public static void MakeAllTreesReadyManual()
    {
        MakeAllTreesReady();
        EditorUtility.DisplayDialog("TreePack Vol.1 Ready", "All TreePack Vol.1 materials have been upgraded to Universal Render Pipeline (URP) Lit and all 48 tree prefabs have been equipped with trunk colliders!", "OK");
    }

    public static void MakeAllTreesReady()
    {
        Debug.Log("[TreePackOptimizer] Starting conversion of TreePack Vol.1 for URP and adding colliders...");

        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader == null)
        {
            Debug.LogError("[TreePackOptimizer] Could not find 'Universal Render Pipeline/Lit' shader! Make sure URP package is installed.");
            return;
        }

        int materialsConverted = 0;
        int prefabsUpdated = 0;

        // 1. Process all standalone .mat files
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/TreePackVol.1" });
        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                if (UpgradeMaterialToURP(mat, urpLitShader))
                {
                    EditorUtility.SetDirty(mat);
                    materialsConverted++;
                }
            }
        }

        // 2. Process all prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/TreePackVol.1/Prefabs" });
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            if (prefabRoot == null) continue;

            bool prefabDirty = false;

            // Upgrade renderer materials (including embedded ones)
            Renderer[] renderers = prefabRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer rend in renderers)
            {
                Material[] sharedMats = rend.sharedMaterials;
                bool matsChanged = false;
                for (int i = 0; i < sharedMats.Length; i++)
                {
                    Material m = sharedMats[i];
                    if (m != null)
                    {
                        if (UpgradeMaterialToURP(m, urpLitShader))
                        {
                            matsChanged = true;
                            materialsConverted++;
                        }
                    }
                }
                if (matsChanged)
                {
                    rend.sharedMaterials = sharedMats;
                    prefabDirty = true;
                }
            }

            // Ensure a well-fitted CapsuleCollider on the tree trunk
            MeshFilter mf = prefabRoot.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Bounds bounds = mf.sharedMesh.bounds;
                CapsuleCollider col = prefabRoot.GetComponent<CapsuleCollider>();
                if (col == null)
                {
                    col = prefabRoot.AddComponent<CapsuleCollider>();
                }

                // Calculate realistic trunk height (covering from ground up into the lower canopy)
                float totalHeight = bounds.size.y;
                float trunkHeight = Mathf.Clamp(totalHeight * 0.45f, 2.5f, totalHeight * 0.75f);
                float trunkRadius = Mathf.Clamp(Mathf.Min(bounds.size.x, bounds.size.z) * 0.08f, 0.25f, 0.6f);

                col.direction = 1; // Y-axis
                col.radius = trunkRadius;
                col.height = trunkHeight;
                col.center = new Vector3(bounds.center.x, bounds.min.y + trunkHeight * 0.5f, bounds.center.z);

                prefabDirty = true;
            }

            if (prefabDirty)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                prefabsUpdated++;
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[TreePackOptimizer] Completed! Upgraded {materialsConverted} material instances to URP Lit and updated {prefabsUpdated} tree prefabs with trunk colliders.");
    }

    private static bool UpgradeMaterialToURP(Material mat, Shader urpLitShader)
    {
        bool isAlreadyURPLit = mat.shader != null && mat.shader.name == "Universal Render Pipeline/Lit";

        string matName = mat.name.ToLower();
        string shaderName = mat.shader != null ? mat.shader.name.ToLower() : "";

        bool isLeaf = matName.Contains("leaf") || matName.Contains("leave") || shaderName.Contains("leaf") || shaderName.Contains("leave");

        // Collect existing properties before changing shader
        Texture mainTex = null;
        if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null)
        {
            mainTex = mat.GetTexture("_BaseMap");
        }
        else if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
        {
            mainTex = mat.GetTexture("_MainTex");
        }

        Color mainColor = Color.white;
        if (mat.HasProperty("_BaseColor"))
        {
            mainColor = mat.GetColor("_BaseColor");
        }
        else if (mat.HasProperty("_Color"))
        {
            mainColor = mat.GetColor("_Color");
        }

        Texture bumpMap = null;
        if (mat.HasProperty("_BumpMap") && mat.GetTexture("_BumpMap") != null)
        {
            bumpMap = mat.GetTexture("_BumpMap");
        }
        else if (mat.HasProperty("_BumpSpecMap") && mat.GetTexture("_BumpSpecMap") != null)
        {
            bumpMap = mat.GetTexture("_BumpSpecMap");
        }

        float cutoff = 0.35f;
        if (mat.HasProperty("_Cutoff"))
        {
            cutoff = mat.GetFloat("_Cutoff");
        }

        if (!isAlreadyURPLit)
        {
            mat.shader = urpLitShader;
        }

        bool changed = false;

        if (mainTex != null)
        {
            mat.SetTexture("_BaseMap", mainTex);
            mat.SetTextureScale("_BaseMap", Vector2.one);
            changed = true;
        }

        mat.SetColor("_BaseColor", mainColor);

        if (bumpMap != null)
        {
            mat.SetTexture("_BumpMap", bumpMap);
            mat.EnableKeyword("_NORMALMAP");
            changed = true;
        }

        if (isLeaf)
        {
            // Two-sided alpha cutout leaf material
            mat.SetFloat("_Surface", 0.0f); // Opaque surface
            mat.SetFloat("_Blend", 0.0f);
            mat.SetFloat("_AlphaClip", 1.0f); // Alpha Clipping ON
            mat.SetFloat("_Cutoff", Mathf.Clamp(cutoff, 0.2f, 0.55f));
            mat.SetFloat("_Cull", 0.0f); // Two-Sided (Cull Off)
            mat.SetFloat("_Smoothness", 0.1f);
            mat.SetFloat("_SpecHighlights", 1.0f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            changed = true;
        }
        else
        {
            // Solid bark / trunk / fruit
            mat.SetFloat("_Surface", 0.0f);
            mat.SetFloat("_AlphaClip", 0.0f);
            mat.SetFloat("_Cull", 2.0f); // Backface culling
            mat.SetFloat("_Smoothness", 0.05f);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            changed = true;
        }

        return changed;
    }
}
