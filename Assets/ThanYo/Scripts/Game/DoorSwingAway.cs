using BlurredRoom;
using UnityEngine;

// ทำให้ประตูของเนทเปิดหนีจากฝั่งที่ผู้เล่นยืน (ไม่ให้บานหมุนเข้ามาบังหน้า)
// ไม่แก้โค้ดเนท: ฟัง event Opened แล้วตั้งทิศมุมเปิดก่อนที่ประตูจะเริ่มหมุนในเฟรมถัดไป
[RequireComponent(typeof(Door))]
public class DoorSwingAway : MonoBehaviour
{
    [Tooltip("Degrees the leaf opens")]
    public float angle = 100f;

    Door door;
    Transform hinge;
    Transform player;
    System.Reflection.FieldInfo openAngleField;

    void Awake()
    {
        door = GetComponent<Door>();
        openAngleField = typeof(Door).GetField("openAngle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var hingeField = typeof(Door).GetField("hinge", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        hinge = hingeField != null ? hingeField.GetValue(door) as Transform : null;
        door.Opened += OnOpened;
    }

    void Start()
    {
        var pm = FindFirstObjectByType<PlayerMovement>();
        if (pm) player = pm.transform;
    }

    void OnDestroy() { if (door) door.Opened -= OnOpened; }

    void OnOpened()
    {
        if (!hinge || !player || openAngleField == null) return;
        // the leaf's closed direction from the hinge (along the wall) and the wall normal
        Transform leaf = hinge.childCount > 0 ? hinge.GetChild(0) : null;
        if (!leaf) return;
        Vector3 along = leaf.GetComponent<Renderer>() ? leaf.GetComponent<Renderer>().bounds.center - hinge.position : hinge.right;
        along.y = 0f;
        // which side is the player on, relative to the closed door plane
        Vector3 normal = Vector3.Cross(Vector3.up, along).normalized;
        float playerSide = Mathf.Sign(Vector3.Dot(player.position - hinge.position, normal));
        // a positive Y rotation turns 'along' toward +normal when cross(up, along) = normal... test both signs and pick the one moving away
        Vector3 testPos = Quaternion.Euler(0f, angle, 0f) * along;
        float sideOfPositive = Mathf.Sign(Vector3.Dot(testPos, normal));
        float signed = sideOfPositive == playerSide ? -angle : angle;
        // only change it while the door is still (nearly) closed, so we don't flip mid-swing
        openAngleField.SetValue(door, signed);
    }
}
