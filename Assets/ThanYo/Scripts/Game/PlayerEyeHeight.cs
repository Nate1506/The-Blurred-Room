using UnityEngine;

// ตั้งความสูงสายตาของผู้เล่น (ความสูงของ CameraPivot จากเท้า)
// ใส่ไว้บน Player — ปรับใน Inspector แล้วเห็นผลทันที ทั้งตอนแก้ฉากและตอน Play
//
// โหมด:
//   Manual    = ใส่ความสูงเอง (เมตร)
//   MatchNpc  = วัดจากโมเดล NPC (หากระดูก "Head" แล้วบวก eyeAboveHeadBone) ให้สายตาเท่ากับ NPC ตัวนั้น
[DefaultExecutionOrder(-600)]
[DisallowMultipleComponent]
public class PlayerEyeHeight : MonoBehaviour
{
    public enum Mode { Manual, MatchNpc }

    [Tooltip("CameraPivot ของผู้เล่น (ว่าง = หาลูกชื่อ CameraPivot ให้เอง)")]
    public Transform cameraPivot;

    public Mode mode = Mode.MatchNpc;

    [Header("Manual")]
    [Tooltip("ความสูงสายตาจากเท้า (เมตร) คนทั่วไป ~1.5-1.65")]
    [Range(0.8f, 2.2f)] public float eyeHeight = 1.58f;

    [Header("Match NPC")]
    [Tooltip("Prefab หรือ NPC ในฉากที่อยากให้สายตาเท่ากัน (เช่น Tenant_sompong = ลุงสมพงษ์)")]
    public GameObject npc;
    [Tooltip("ชื่อกระดูกหัวในโมเดล")]
    public string headBoneName = "Head";
    [Tooltip("ตาอยู่สูงกว่ากระดูกหัวเท่าไร (เมตร)")]
    [Range(-0.1f, 0.3f)] public float eyeAboveHeadBone = 0.12f;
    [Tooltip("ปรับเพิ่ม/ลดจากที่วัดได้ (เมตร)")]
    [Range(-0.4f, 0.4f)] public float extraOffset = 0f;

    [Header("ผลลัพธ์ (อ่านอย่างเดียว)")]
    [SerializeField] float appliedHeight;

    public float AppliedHeight => appliedHeight;

    void Awake() => Apply();

#if UNITY_EDITOR
    void OnValidate()
    {
        // เลื่อนไปทำหลัง OnValidate จบ (แก้ transform ใน OnValidate ตรงๆ Unity จะเตือน)
        UnityEditor.EditorApplication.delayCall += () => { if (this) Apply(); };
    }
#endif

    [ContextMenu("Apply now")]
    public void Apply()
    {
        if (!cameraPivot) cameraPivot = transform.Find("CameraPivot");
        if (!cameraPivot) return;
        appliedHeight = mode == Mode.MatchNpc && TryMeasureNpc(out float h) ? h + extraOffset : eyeHeight;
        var p = cameraPivot.localPosition;
        if (Mathf.Abs(p.y - appliedHeight) < 0.0001f) return;
        p.y = appliedHeight;
        cameraPivot.localPosition = p;
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(cameraPivot);
#endif
    }

    bool TryMeasureNpc(out float height)
    {
        height = 0f;
        if (!npc) return false;
        Transform head = null;
        foreach (var t in npc.GetComponentsInChildren<Transform>(true))
            if (t.name == headBoneName) { head = t; break; }
        if (!head) return false;
        // วัดเทียบกับ root ของ NPC (= เท้า) ใช้ได้ทั้ง prefab และตัวในฉาก
        height = head.position.y - npc.transform.position.y + eyeAboveHeadBone;
        return height > 0.3f;
    }
}
