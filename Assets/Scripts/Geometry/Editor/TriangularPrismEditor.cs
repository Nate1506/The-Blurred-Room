using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TriangularPrism))]
[CanEditMultipleObjects]
public class TriangularPrismEditor : Editor
{
    private SerializedProperty widthProp;
    private SerializedProperty heightProp;
    private SerializedProperty lengthProp;
    private SerializedProperty apexOffsetProp;
    private SerializedProperty presetProp;
    private SerializedProperty pivotProp;
    private SerializedProperty generateColliderProp;
    private SerializedProperty convexColliderProp;
    private SerializedProperty autoTileUVProp;
    private SerializedProperty uvScaleProp;

    private void OnEnable()
    {
        widthProp = serializedObject.FindProperty("width");
        heightProp = serializedObject.FindProperty("height");
        lengthProp = serializedObject.FindProperty("length");
        apexOffsetProp = serializedObject.FindProperty("apexOffset");
        presetProp = serializedObject.FindProperty("preset");
        pivotProp = serializedObject.FindProperty("pivot");
        generateColliderProp = serializedObject.FindProperty("generateCollider");
        convexColliderProp = serializedObject.FindProperty("convexCollider");
        autoTileUVProp = serializedObject.FindProperty("autoTileUV");
        uvScaleProp = serializedObject.FindProperty("uvScale");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        TriangularPrism prism = (TriangularPrism)target;

        EditorGUILayout.LabelField("Quick Shape Presets", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Isosceles Roof"))
        {
            presetProp.enumValueIndex = (int)TriangularPrism.ShapePreset.IsoscelesRoof;
            apexOffsetProp.floatValue = 0f;
        }
        if (GUILayout.Button("Ramp (Left)"))
        {
            presetProp.enumValueIndex = (int)TriangularPrism.ShapePreset.RightAngleRampLeft;
            apexOffsetProp.floatValue = -widthProp.floatValue * 0.5f;
        }
        if (GUILayout.Button("Ramp (Right)"))
        {
            presetProp.enumValueIndex = (int)TriangularPrism.ShapePreset.RightAngleRampRight;
            apexOffsetProp.floatValue = widthProp.floatValue * 0.5f;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.PropertyField(presetProp);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Dimensions (Adjust Freely)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(widthProp);
        EditorGUILayout.PropertyField(heightProp);
        EditorGUILayout.PropertyField(lengthProp);

        float halfW = widthProp.floatValue * 0.5f;
        EditorGUILayout.Slider(apexOffsetProp, -halfW * 1.5f, halfW * 1.5f, new GUIContent("Apex Offset (X)", "Horizontal position of the top ridge"));

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Origin & Pivot", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(pivotProp);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Physics & Collision", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(generateColliderProp);
        if (generateColliderProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(convexColliderProp);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("UV & Texturing", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(autoTileUVProp);
        EditorGUILayout.PropertyField(uvScaleProp);

        EditorGUILayout.Space(12);
        if (GUILayout.Button("Save Mesh As Asset...", GUILayout.Height(28)))
        {
            SaveMeshAsset(prism);
        }

        if (serializedObject.ApplyModifiedProperties())
        {
            foreach (var t in targets)
            {
                ((TriangularPrism)t).RebuildMesh();
            }
        }
    }

    private void OnSceneGUI()
    {
        TriangularPrism prism = (TriangularPrism)target;
        Transform tr = prism.transform;

        Vector3 pivotOffset = Vector3.zero;
        switch (prism.pivot)
        {
            case TriangularPrism.PivotPoint.BottomCenter:
                pivotOffset = Vector3.zero;
                break;
            case TriangularPrism.PivotPoint.Center:
                pivotOffset = new Vector3(0f, -prism.height * 0.5f, 0f);
                break;
            case TriangularPrism.PivotPoint.BottomCorner:
                pivotOffset = new Vector3(prism.width * 0.5f, 0f, prism.length * 0.5f);
                break;
        }

        // World positions of dimension handles
        Vector3 rightPos = tr.TransformPoint(new Vector3(prism.width * 0.5f + pivotOffset.x, pivotOffset.y, pivotOffset.z));
        Vector3 leftPos = tr.TransformPoint(new Vector3(-prism.width * 0.5f + pivotOffset.x, pivotOffset.y, pivotOffset.z));
        Vector3 topPos = tr.TransformPoint(new Vector3(prism.apexOffset + pivotOffset.x, prism.height + pivotOffset.y, pivotOffset.z));
        Vector3 frontPos = tr.TransformPoint(new Vector3(pivotOffset.x, pivotOffset.y, prism.length * 0.5f + pivotOffset.z));
        Vector3 backPos = tr.TransformPoint(new Vector3(pivotOffset.x, pivotOffset.y, -prism.length * 0.5f + pivotOffset.z));

        float handleSize = HandleUtility.GetHandleSize(rightPos) * 0.12f;

        // Width Handle (Right)
        EditorGUI.BeginChangeCheck();
        Vector3 newRightPos = Handles.Slider(rightPos, tr.right, handleSize, Handles.ConeHandleCap, 0.1f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(prism, "Change Prism Width");
            float delta = Vector3.Dot(newRightPos - rightPos, tr.right);
            prism.width = Mathf.Max(0.1f, prism.width + delta * 2f);
            prism.RebuildMesh();
        }

        // Height Handle (Top)
        EditorGUI.BeginChangeCheck();
        Vector3 newTopPos = Handles.Slider(topPos, tr.up, handleSize, Handles.ConeHandleCap, 0.1f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(prism, "Change Prism Height");
            float delta = Vector3.Dot(newTopPos - topPos, tr.up);
            prism.height = Mathf.Max(0.1f, prism.height + delta);
            prism.RebuildMesh();
        }

        // Length Handle (Front)
        EditorGUI.BeginChangeCheck();
        Vector3 newFrontPos = Handles.Slider(frontPos, tr.forward, handleSize, Handles.ConeHandleCap, 0.1f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(prism, "Change Prism Length");
            float delta = Vector3.Dot(newFrontPos - frontPos, tr.forward);
            prism.length = Mathf.Max(0.1f, prism.length + delta * 2f);
            prism.RebuildMesh();
        }

        // Apex Offset Handle (Horizontal on Top)
        EditorGUI.BeginChangeCheck();
        Vector3 newApexPos = Handles.Slider(topPos, tr.right, handleSize * 0.8f, Handles.SphereHandleCap, 0.05f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(prism, "Change Prism Apex");
            float delta = Vector3.Dot(newApexPos - topPos, tr.right);
            prism.preset = TriangularPrism.ShapePreset.Custom;
            prism.apexOffset += delta;
            prism.RebuildMesh();
        }
    }

    private void SaveMeshAsset(TriangularPrism prism)
    {
        MeshFilter mf = prism.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        string path = EditorUtility.SaveFilePanelInProject("Save Prism Mesh", "TriangularPrismMesh.asset", "asset", "Save generated mesh as an asset.");
        if (string.IsNullOrEmpty(path)) return;

        Mesh meshCopy = Instantiate(mf.sharedMesh);
        AssetDatabase.CreateAsset(meshCopy, path);
        AssetDatabase.SaveAssets();

        mf.sharedMesh = meshCopy;
        EditorUtility.DisplayDialog("Mesh Saved", "Mesh saved successfully to " + path, "OK");
    }

    [MenuItem("GameObject/3D Object/Triangular Prism", false, 0)]
    public static void CreateTriangularPrism(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("Triangular Prism");
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

        TriangularPrism prism = go.AddComponent<TriangularPrism>();

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        Material defaultMat = FindDefaultMaterial();
        if (defaultMat != null)
        {
            mr.sharedMaterial = defaultMat;
        }

        prism.RebuildMesh();

        Undo.RegisterCreatedObjectUndo(go, "Create Triangular Prism");
        Selection.activeObject = go;
    }

    private static Material FindDefaultMaterial()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        if (litShader != null)
        {
            Material mat = new Material(litShader);
            mat.name = "Default-Prism-Material";
            return mat;
        }

        return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
    }
}
