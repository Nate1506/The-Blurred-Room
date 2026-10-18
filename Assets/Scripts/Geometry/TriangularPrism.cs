using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[SelectionBase]
[AddComponentMenu("Rendering/Triangular Prism")]
public class TriangularPrism : MonoBehaviour
{
    public enum ShapePreset
    {
        Custom,
        IsoscelesRoof,
        RightAngleRampLeft,
        RightAngleRampRight
    }

    public enum PivotPoint
    {
        BottomCenter,
        Center,
        BottomCorner
    }

    [Header("Prism Dimensions")]
    [Tooltip("Total base width along the X axis.")]
    [Min(0.01f)]
    public float width = 2f;

    [Tooltip("Total height along the Y axis.")]
    [Min(0.01f)]
    public float height = 1.5f;

    [Tooltip("Total length / depth along the Z axis.")]
    [Min(0.01f)]
    public float length = 3f;

    [Tooltip("Horizontal offset of the top ridge / apex along X. 0 = centered (isosceles).")]
    public float apexOffset = 0f;

    [Header("Preset & Alignment")]
    [Tooltip("Quick presets for common shapes like roofs and ramps.")]
    public ShapePreset preset = ShapePreset.IsoscelesRoof;

    [Tooltip("Where the local origin (pivot) of the prism is located.")]
    public PivotPoint pivot = PivotPoint.BottomCenter;

    [Header("Collision")]
    [Tooltip("Automatically generate and update a MeshCollider matching this prism.")]
    public bool generateCollider = true;

    [Tooltip("Mark the MeshCollider as Convex so it can interact with rigidbodies.")]
    public bool convexCollider = true;

    [Header("UV Mapping")]
    [Tooltip("Automatically scale texture coordinates with dimensions to prevent texture stretching.")]
    public bool autoTileUV = true;

    [Tooltip("Multiplier for texture tiling along (U, V).")]
    public Vector2 uvScale = Vector2.one;

    private MeshFilter meshFilter;
    private Mesh generatedMesh;

    private void Awake()
    {
        EnsureMeshReady();
    }

    private void OnEnable()
    {
        EnsureMeshReady();
    }

    private void Start()
    {
        EnsureMeshReady();
    }

    private void OnValidate()
    {
        // Enforce positive dimensions
        width = Mathf.Max(0.01f, width);
        height = Mathf.Max(0.01f, height);
        length = Mathf.Max(0.01f, length);

        ApplyPreset();
        RebuildMesh();
    }

    public void ApplyPreset()
    {
        switch (preset)
        {
            case ShapePreset.IsoscelesRoof:
                apexOffset = 0f;
                break;
            case ShapePreset.RightAngleRampLeft:
                apexOffset = -width * 0.5f;
                break;
            case ShapePreset.RightAngleRampRight:
                apexOffset = width * 0.5f;
                break;
            case ShapePreset.Custom:
                break;
        }
    }

    public void EnsureMeshReady()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        RebuildMesh();
    }

    public void RebuildMesh()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        if (generatedMesh == null)
        {
            generatedMesh = new Mesh();
            generatedMesh.name = "TriangularPrism_Procedural";
        }
        else
        {
            generatedMesh.Clear();
        }

        BuildPrismGeometry(generatedMesh);

        meshFilter.sharedMesh = generatedMesh;

        UpdateCollider();
    }

    private void UpdateCollider()
    {
        MeshCollider col = GetComponent<MeshCollider>();
        if (generateCollider)
        {
            if (col == null)
            {
                col = gameObject.AddComponent<MeshCollider>();
            }
            col.sharedMesh = null; // force refresh
            col.sharedMesh = generatedMesh;
            col.convex = convexCollider;
            col.enabled = true;
        }
        else
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }
    }

    private void BuildPrismGeometry(Mesh mesh)
    {
        // Calculate pivot offset
        Vector3 pivotOffset = Vector3.zero;
        switch (pivot)
        {
            case PivotPoint.BottomCenter:
                pivotOffset = new Vector3(0f, 0f, 0f);
                break;
            case PivotPoint.Center:
                pivotOffset = new Vector3(0f, -height * 0.5f, 0f);
                break;
            case PivotPoint.BottomCorner:
                pivotOffset = new Vector3(width * 0.5f, 0f, length * 0.5f);
                break;
        }

        float halfW = width * 0.5f;
        float halfL = length * 0.5f;

        // Base vertices in local space before pivot offset
        float bl_x = -halfW + pivotOffset.x;
        float br_x = halfW + pivotOffset.x;
        float top_x = apexOffset + pivotOffset.x;

        float bottom_y = 0f + pivotOffset.y;
        float top_y = height + pivotOffset.y;

        float front_z = halfL + pivotOffset.z;
        float back_z = -halfL + pivotOffset.z;

        // 3D key positions
        Vector3 v_front_bl = new Vector3(bl_x, bottom_y, front_z);
        Vector3 v_front_br = new Vector3(br_x, bottom_y, front_z);
        Vector3 v_front_top = new Vector3(top_x, top_y, front_z);

        Vector3 v_back_bl = new Vector3(bl_x, bottom_y, back_z);
        Vector3 v_back_br = new Vector3(br_x, bottom_y, back_z);
        Vector3 v_back_top = new Vector3(top_x, top_y, back_z);

        List<Vector3> vertices = new List<Vector3>(18);
        List<int> triangles = new List<int>(24);
        List<Vector2> uvs = new List<Vector2>(18);

        Vector2 uvScaleFactor = autoTileUV ? uvScale : Vector2.one;

        // -------------------------------------------------------------
        // 1. FRONT TRIANGLE (+Z)
        // -------------------------------------------------------------
        int startIdx = vertices.Count;
        vertices.Add(v_front_bl);
        vertices.Add(v_front_top);
        vertices.Add(v_front_br);

        float frontU_top = (top_x - bl_x) / width;
        uvs.Add(Vector2.Scale(new Vector2(0f, 0f), uvScaleFactor));
        uvs.Add(Vector2.Scale(new Vector2(frontU_top * (autoTileUV ? width : 1f), autoTileUV ? height : 1f), uvScaleFactor));
        uvs.Add(Vector2.Scale(new Vector2(autoTileUV ? width : 1f, 0f), uvScaleFactor));

        triangles.Add(startIdx + 0);
        triangles.Add(startIdx + 1);
        triangles.Add(startIdx + 2);

        // -------------------------------------------------------------
        // 2. BACK TRIANGLE (-Z)
        // -------------------------------------------------------------
        startIdx = vertices.Count;
        vertices.Add(v_back_bl);
        vertices.Add(v_back_br);
        vertices.Add(v_back_top);

        float backU_top = (top_x - bl_x) / width;
        uvs.Add(Vector2.Scale(new Vector2(autoTileUV ? width : 1f, 0f), uvScaleFactor));
        uvs.Add(Vector2.Scale(new Vector2(0f, 0f), uvScaleFactor));
        uvs.Add(Vector2.Scale(new Vector2((1f - backU_top) * (autoTileUV ? width : 1f), autoTileUV ? height : 1f), uvScaleFactor));

        triangles.Add(startIdx + 0);
        triangles.Add(startIdx + 1);
        triangles.Add(startIdx + 2);

        // -------------------------------------------------------------
        // 3. BOTTOM QUAD (-Y)
        // -------------------------------------------------------------
        startIdx = vertices.Count;
        vertices.Add(v_front_bl); // BL
        vertices.Add(v_front_br); // BR
        vertices.Add(v_back_br);  // TR
        vertices.Add(v_back_bl);  // TL

        float bW = autoTileUV ? width : 1f;
        float bL = autoTileUV ? length : 1f;
        uvs.Add(Vector2.Scale(new Vector2(0f, 0f), uvScaleFactor));
        uvs.Add(Vector2.Scale(new Vector2(bW, 0f), uvScaleFactor));
        uvs.Add(Vector2.Scale(new Vector2(bW, bL), uvScaleFactor));
        uvs.Add(Vector2.Scale(new Vector2(0f, bL), uvScaleFactor));

        // Normal points down (-Y), so winding is BL, TL, TR and BL, TR, BR
        triangles.Add(startIdx + 0);
        triangles.Add(startIdx + 3);
        triangles.Add(startIdx + 2);

        triangles.Add(startIdx + 0);
        triangles.Add(startIdx + 2);
        triangles.Add(startIdx + 1);

        // -------------------------------------------------------------
        // 4. LEFT SLOPE QUAD (BL -> Top)
        // -------------------------------------------------------------
        startIdx = vertices.Count;
        vertices.Add(v_back_bl);
        vertices.Add(v_front_bl);
        vertices.Add(v_front_top);
        vertices.Add(v_back_top);

        float slopeLeft = Vector2.Distance(new Vector2(bl_x, bottom_y), new Vector2(top_x, top_y));
        float sLW = autoTileUV ? slopeLeft : 1f;
        uvs.Add(Vector2.Scale(new Vector2(0f, 0f), uvScaleFactor));
        uvs.Add(Vector2.Scale(new Vector2(bL, 0f), uvScaleFactor));
        uvs.Add(Vector2.Scale(new Vector2(bL, sLW), uvScaleFactor));
        uvs.Add(Vector2.Scale(new Vector2(0f, sLW), uvScaleFactor));

        triangles.Add(startIdx + 0);
        triangles.Add(startIdx + 1);
        triangles.Add(startIdx + 2);

        triangles.Add(startIdx + 0);
        triangles.Add(startIdx + 2);
        triangles.Add(startIdx + 3);

        // -------------------------------------------------------------
        // 5. RIGHT SLOPE QUAD (Top -> BR)
        // -------------------------------------------------------------
        startIdx = vertices.Count;
        vertices.Add(v_front_top);
        vertices.Add(v_front_br);
        vertices.Add(v_back_br);
        vertices.Add(v_back_top);

        float slopeRight = Vector2.Distance(new Vector2(top_x, top_y), new Vector2(br_x, bottom_y));
        float sRW = autoTileUV ? slopeRight : 1f;
        uvs.Add(Vector2.Scale(new Vector2(0f, sRW), uvScaleFactor));
        uvs.Add(Vector2.Scale(new Vector2(0f, 0f), uvScaleFactor));
        uvs.Add(Vector2.Scale(new Vector2(bL, 0f), uvScaleFactor));
        uvs.Add(Vector2.Scale(new Vector2(bL, sRW), uvScaleFactor));

        triangles.Add(startIdx + 0);
        triangles.Add(startIdx + 1);
        triangles.Add(startIdx + 2);

        triangles.Add(startIdx + 0);
        triangles.Add(startIdx + 2);
        triangles.Add(startIdx + 3);

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        if (generatedMesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(generatedMesh);
            }
            else
            {
                DestroyImmediate(generatedMesh);
            }
        }
    }
}
