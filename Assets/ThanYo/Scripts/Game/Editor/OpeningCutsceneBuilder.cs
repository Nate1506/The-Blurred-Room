using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ThanYo > Blurred Room > 10. คัตซีนเปิดเกม (แท็กซี่)
// สร้าง Cutscene_Opening ในซีน ThanYo: จุดกล้อง + ทางรถ (เริ่ม -> จอด) + ใส่เสียง — กดซ้ำได้ (ไม่ทับจุดที่ปรับไว้แล้ว)
public static class OpeningCutsceneBuilder
{
    const string Snd = "Assets/ThanYo/Sounds/";

    [MenuItem("ThanYo/Blurred Room/10. คัตซีนเปิดเกม (แท็กซี่)")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "ThanYo") { EditorUtility.DisplayDialog("Cutscene", "เปิดซีน ThanYo ก่อน", "OK"); return; }

        var root = GameObject.Find("Cutscene_Opening");
        bool fresh = !root;
        if (fresh) { root = new GameObject("Cutscene_Opening"); Undo.RegisterCreatedObjectUndo(root, "Cutscene"); }
        var cut = root.GetComponent<OpeningCutscene>() ?? Undo.AddComponent<OpeningCutscene>(root);

        if (!cut.taxi) { var t = GameObject.Find("taxi"); if (t) cut.taxi = t.transform; }
        if (!cut.arriveClip) cut.arriveClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Snd + "freesound_community-cararriveandstop-6191.mp3");
        if (!cut.doorClip) cut.doorClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Snd + "musicnation782-car-door-shut-297266.mp3");

        cut.cameraPoint = Child(root, "Camera_Point", cut.cameraPoint, new Vector3(-24.8f, 3.2f, 9.45f), Quaternion.Euler(23f, 30.6f, 0f));

        // ทางรถ: วิ่งตามแนวหน้ารถ มาจอดกลางเฟรม
        Vector3 park = new Vector3(-20f, 0f, 19f), start = new Vector3(2f, 0f, 9.8f);
        if (cut.taxi)
        {
            var mf = cut.taxi.GetComponentInChildren<MeshFilter>();
            Vector3 front = mf ? -mf.transform.up : cut.taxi.forward;   // โมเดลแท็กซี่นี้ หน้ารถ = -Y ของเมช
            front.y = 0f; front.Normalize();
            Vector3 p0 = cut.taxi.position;
            float s = (park.x - p0.x) / Mathf.Abs(front.x) * Mathf.Sign(front.x) ;
            park = new Vector3(p0.x + front.x * s, p0.y, p0.z + front.z * s);
            start = park - front * 24f;
        }
        cut.parkPoint = Child(root, "Taxi_Park", cut.parkPoint, park, Quaternion.identity);
        cut.startPoint = Child(root, "Taxi_Start", cut.startPoint, start, Quaternion.identity);

        EditorUtility.SetDirty(cut);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeObject = root;
        Debug.Log("[Cutscene] พร้อมแล้ว — ปรับมุมกล้องที่ Camera_Point / ทางรถที่ Taxi_Start, Taxi_Park");
    }

    static Transform Child(GameObject root, string name, Transform current, Vector3 pos, Quaternion rot)
    {
        if (current) return current;
        var t = root.transform.Find(name);
        if (t) return t;
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Cutscene");
        go.transform.SetParent(root.transform, false);
        go.transform.SetPositionAndRotation(pos, rot);
        return go.transform;
    }
}
