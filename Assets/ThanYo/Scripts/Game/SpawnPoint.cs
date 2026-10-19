using UnityEngine;

// จุดเกิดของผู้เล่นในซีน ประตูข้ามซีนจะบอก id ของจุดที่ต้องไปเกิด
public class SpawnPoint : MonoBehaviour
{
    public string id = "Default";
    [Tooltip("ใช้จุดนี้ถ้าไม่มีใครบอก id มา (เช่น กด Play ในซีนนี้ตรงๆ)")]
    public bool isDefault;

    void OnDrawGizmos()
    {
        Gizmos.color = isDefault ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, 0.3f);
        Gizmos.DrawRay(transform.position + Vector3.up * 0.9f, transform.forward * 0.8f);
    }
}
