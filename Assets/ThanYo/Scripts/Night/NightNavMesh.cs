using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// อบ NavMesh ตอนเริ่มซีน (ไม่ต้องลงแพ็กเกจ AI Navigation) ให้ผีเดินตามทางเดิน/บันได
// กล่องสีฟ้าใน Scene view = พื้นที่ที่จะอบ — ครอบเฉพาะชั้นที่เล่นได้จะเร็วกว่า
[DefaultExecutionOrder(-200)]
public class NightNavMesh : MonoBehaviour
{
    [Tooltip("พื้นที่ที่จะอบ (พิกัดโลก)")]
    public Bounds area = new Bounds(new Vector3(11f, 4f, 5f), new Vector3(24f, 9f, 14f));
    [Tooltip("layer ที่นับเป็นพื้น/กำแพง")]
    public LayerMask geometryLayers = ~0;
    [Header("ขนาดตัวที่เดิน")]
    [Range(0.1f, 1f)] public float agentRadius = 0.28f;
    [Range(1f, 3f)] public float agentHeight = 1.9f;
    [Tooltip("ก้าวขึ้นขั้นได้สูงสุด (บันได)")]
    [Range(0.1f, 0.8f)] public float stepHeight = 0.45f;
    [Range(10f, 60f)] public float maxSlope = 50f;

    NavMeshDataInstance instance;

    void Awake() => Bake();

    void OnDestroy() { if (instance.valid) instance.Remove(); }

    [ContextMenu("อบใหม่ (ตอน Play)")]
    public void Bake()
    {
        if (instance.valid) instance.Remove();
        var sources = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(area, geometryLayers, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
        // ไม่นับตัวผู้เล่น/คนเป็นสิ่งกีดขวาง
        sources.RemoveAll(s => s.component && (s.component.GetComponentInParent<CharacterController>() || s.component.GetComponentInParent<NavMeshAgent>()));
        var settings = NavMesh.GetSettingsByID(0);
        settings.agentRadius = agentRadius; settings.agentHeight = agentHeight;
        settings.agentClimb = stepHeight; settings.agentSlope = maxSlope;
        var data = NavMeshBuilder.BuildNavMeshData(settings, sources, area, Vector3.zero, Quaternion.identity);
        if (data != null) instance = NavMesh.AddNavMeshData(data);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.6f);
        Gizmos.DrawWireCube(area.center, area.size);
    }
}
