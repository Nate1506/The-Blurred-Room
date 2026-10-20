using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ชั้น 1 ฝั่งทิศใต้ (ระหว่างล็อบบี้กับห้องซักผ้า) เดิมเป็นลานโล่ง + หน้าต่างทางเดิน
// เมนูนี้เติมห้อง 3 ห้อง (105-107) ให้เหมือนชั้นอื่น: ก๊อปผนัง/ประตู/หน้าต่างจากชั้น 2 ลงมา, ปูพื้น,
// ลบหน้าต่างทางเดิน 3 บาน + หน้าต่างหลังเคาน์เตอร์ แล้วอุดรูผนัง
// เมชใหม่บันทึกเป็นไฟล์แยก (Inside_Floor1_Rooms.asset) เมชเดิมไม่ถูกแตะ — อยากย้อนกลับ: ใส่เมชเดิมกลับใน Shell
public static class Floor1RoomsBuilder
{
    // ขอบเขตลานเดิม (พิกัดโลก)
    const float X0 = 8.3f, X1 = 16.6f, ZMax = 4.1f;
    const float FloorDy = -2.8f;   // ชั้น 2 -> ชั้น 1
    const float E = 0.02f;
    const string MeshPath = "Assets/ThanYo/Inside/Meshes/Inside_Floor1_Rooms.asset";

    static readonly string[] SrcRooms = { "210", "209", "208" };
    static readonly string[] NewRooms = { "107", "106", "105" };

    [MenuItem("ThanYo/Blurred Room/9. ชั้น 1: เติมห้องฝั่งใต้ + ลบหน้าต่างเคาน์เตอร์")]
    public static void Build()
    {
        var f1 = GameObject.Find("Dorm_Interior/Floor_1");
        var f2 = GameObject.Find("Dorm_Interior/Floor_2");
        if (!f1 || !f2) { EditorUtility.DisplayDialog("Floor 1", "ต้องเปิดซีน Inside ก่อน (หา Dorm_Interior/Floor_1 ไม่เจอ)", "OK"); return; }
        var mf1 = f1.transform.Find("Shell").GetComponent<MeshFilter>();
        var mf2 = f2.transform.Find("Shell").GetComponent<MeshFilter>();
        if (mf1.sharedMesh && mf1.sharedMesh.name.EndsWith("_Rooms")) { EditorUtility.DisplayDialog("Floor 1", "ทำไปแล้ว (Shell ใช้เมช _Rooms อยู่)", "OK"); return; }

        var mesh = BuildMesh(mf1.sharedMesh, mf2.sharedMesh);
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (existing) { EditorUtility.CopySerialized(mesh, existing); mesh = existing; }
        else AssetDatabase.CreateAsset(mesh, MeshPath);
        AssetDatabase.SaveAssets();

        Undo.RecordObject(mf1, "Floor1 rooms");
        mf1.sharedMesh = mesh;
        var mc = mf1.GetComponent<MeshCollider>();
        if (mc) { Undo.RecordObject(mc, "Floor1 rooms"); mc.sharedMesh = null; mc.sharedMesh = mesh; }

        var log = new System.Text.StringBuilder("[Floor1Rooms] เมชใหม่ " + mesh.vertexCount + " vertices\n");

        // ---------- ประตู + หน้าต่าง + ป้ายห้อง (ก๊อปจากชั้น 2) ----------
        var doors1 = f1.transform.Find("Doors");
        var wins1 = f1.transform.Find("Windows");
        var rooms1 = f1.transform.Find("Rooms");
        var doorXs = new List<float>();
        for (int i = 0; i < SrcRooms.Length; i++)
        {
            var d2 = f2.transform.Find("Doors/Door_" + SrcRooms[i]);
            if (d2 && !doors1.Find("Door_" + NewRooms[i]))
            {
                var d = Object.Instantiate(d2.gameObject, doors1);
                d.name = "Door_" + NewRooms[i];
                d.transform.position = d2.position + Vector3.up * FloorDy;
                foreach (var t in d.GetComponentsInChildren<TMPro.TMP_Text>(true)) if (t.text.Contains(SrcRooms[i])) t.text = t.text.Replace(SrcRooms[i], NewRooms[i]);
                Undo.RegisterCreatedObjectUndo(d, "Floor1 rooms");
                doorXs.Add(d.transform.position.x);
                log.Append("+ " + d.name + "\n");
            }
            var w2 = f2.transform.Find("Windows/Win_" + SrcRooms[i]);
            if (w2 && !wins1.Find("Win_" + NewRooms[i]))
            {
                var w = Object.Instantiate(w2.gameObject, wins1);
                w.name = "Win_" + NewRooms[i];
                w.transform.position = w2.position + Vector3.up * FloorDy;
                Undo.RegisterCreatedObjectUndo(w, "Floor1 rooms");
                log.Append("+ " + w.name + "\n");
            }
            var r2 = f2.transform.Find("Rooms/Room_" + SrcRooms[i]);
            if (r2 && rooms1 && !rooms1.Find("Room_" + NewRooms[i]))
            {
                var r = new GameObject("Room_" + NewRooms[i]);
                r.transform.SetParent(rooms1, false);
                r.transform.position = new Vector3(r2.position.x, 1.2f, r2.position.z);
                Undo.RegisterCreatedObjectUndo(r, "Floor1 rooms");
            }
        }

        // ---------- ลบหน้าต่างทางเดิน + หน้าต่างหลังเคาน์เตอร์ ----------
        foreach (var n in new[] { "Win_Corr_1", "Win_Corr_2", "Win_Corr_3", "Win_Lobby_Side" })
        {
            var w = wins1.Find(n);
            if (w) { Undo.DestroyObjectImmediate(w.gameObject); log.Append("- " + n + "\n"); }
        }

        // ---------- โคมไฟผนังทางเดินที่ไปทับประตูใหม่: เลื่อนไปไว้ระหว่างประตู ----------
        var lights = GameObject.Find("Interior_Lighting/Floor_1");
        if (lights)
            foreach (Transform l in lights.transform)
            {
                var p = l.position;
                if (p.z < 3.6f || p.z > 4.6f || p.x < X0 || p.x > X1 || p.y > 2.8f) continue;
                foreach (float dx in doorXs)
                    if (Mathf.Abs(p.x - dx) < 0.9f)
                    {
                        Undo.RecordObject(l, "Floor1 rooms");
                        l.position = new Vector3(dx - 1.45f, p.y, p.z);
                        log.Append("~ ย้ายโคม " + l.name + " x " + p.x.ToString("F2") + " -> " + (dx - 1.45f).ToString("F2") + "\n");
                        break;
                    }
            }

        EditorSceneManager.MarkSceneDirty(f1.scene);
        Debug.Log(log.ToString());
    }

    // =====================================================================
    //  mesh
    // =====================================================================
    class Builder
    {
        public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int>[] tris = { new List<int>(), new List<int>() };

        public void Tri(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc, Vector2 ua, Vector2 ub, Vector2 uc)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            n.Add(na); n.Add(nb); n.Add(nc);
            uv.Add(ua); uv.Add(ub); uv.Add(uc);
            tris[Mathf.Min(sub, 1)].Add(i); tris[Mathf.Min(sub, 1)].Add(i + 1); tris[Mathf.Min(sub, 1)].Add(i + 2);
        }

        /// <summary>สี่เหลี่ยม 4 มุม (ตามเข็มเมื่อมองจากด้านหน้า) uv = พิกัดโลก / 1.5 เหมือนเมชเดิม</summary>
        public void Quad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            // พื้น: พิกัดโลก/1.5 | ผนัง: แนวนอน/2.8, แนวตั้ง/2.6 (สูง 1 ห้อง = 0..1) — เหมือนเมชเดิม
            Vector2 U(Vector3 p)
            {
                if (Mathf.Abs(normal.y) > 0.5f) return new Vector2(p.x, p.z) / 1.5f;
                if (Mathf.Abs(normal.x) > 0.5f) return new Vector2(p.z / 2.8f, p.y / 2.6f);
                return new Vector2(p.x / 2.8f, p.y / 2.6f);
            }
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f) { var tmp = b; b = d; d = tmp; }   // หันหน้าให้ถูกด้าน
            Tri(sub, a, b, c, normal, normal, normal, U(a), U(b), U(c));
            Tri(sub, a, c, d, normal, normal, normal, U(a), U(c), U(d));
        }
    }

    static bool In(float x, float lo, float hi) => x >= lo - E && x <= hi + E;

    static Mesh BuildMesh(Mesh m1, Mesh m2)
    {
        var b = new Builder();

        // ---------- ชั้น 1 เดิม: ตัดส่วนที่จะแทนที่ออก ----------
        {
            var v = m1.vertices; var n = m1.normals; var uv = m1.uv;
            for (int s = 0; s < m1.subMeshCount; s++)
            {
                var t = m1.GetTriangles(s);
                for (int i = 0; i < t.Length; i += 3)
                {
                    Vector3 a = v[t[i]], bb = v[t[i + 1]], c = v[t[i + 2]];
                    Vector3 nn = n[t[i]];
                    float minX = Mathf.Min(a.x, bb.x, c.x), maxX = Mathf.Max(a.x, bb.x, c.x);
                    float minZ = Mathf.Min(a.z, bb.z, c.z), maxZ = Mathf.Max(a.z, bb.z, c.z);
                    float minY = Mathf.Min(a.y, bb.y, c.y);

                    bool inYard = minX >= X0 - E && maxX <= X1 + E && minZ >= -E && maxZ <= ZMax + E && minY >= -E;
                    // เก็บผนังห้องซักผ้าด้านที่หันเข้าลาน (จะกลายเป็นผนังห้อง 105)
                    bool laundryFace = Mathf.Abs(minX - X1) < E && Mathf.Abs(maxX - X1) < E && nn.x < -0.5f;
                    if (inYard && !laundryFace) continue;

                    // รูหน้าต่างหลังเคาน์เตอร์ (ผนังล็อบบี้ x 8.1-8.3, z 1.35-2.65)
                    bool lobbyHole = minX >= 8.1f - E && maxX <= 8.3f + E && minZ >= 1.35f - E && maxZ <= 2.65f + E && minY >= -E;
                    if (lobbyHole) continue;

                    b.Tri(s, a, bb, c, n[t[i]], n[t[i + 1]], n[t[i + 2]], uv[t[i]], uv[t[i + 1]], uv[t[i + 2]]);
                }
            }
        }

        // ---------- ชั้น 2: ก๊อปผนังห้อง 208-210 ลงมา (ตัดขอบให้พอดีลาน) ----------
        {
            var v = m2.vertices; var n = m2.normals; var uv = m2.uv;
            for (int s = 0; s < m2.subMeshCount; s++)
            {
                var t = m2.GetTriangles(s);
                for (int i = 0; i < t.Length; i += 3)
                {
                    Vector3 a = v[t[i]], bb = v[t[i + 1]], c = v[t[i + 2]];
                    float minX = Mathf.Min(a.x, bb.x, c.x), maxX = Mathf.Max(a.x, bb.x, c.x);
                    float maxZ = Mathf.Max(a.z, bb.z, c.z), minZ = Mathf.Min(a.z, bb.z, c.z);
                    float maxY = Mathf.Max(a.y, bb.y, c.y);
                    if (maxY <= 2.8f + E) continue;                      // พื้นชั้น 2 (มีอยู่แล้ว = เพดานชั้น 1)
                    if (minZ < -E || maxZ > ZMax + E) continue;
                    if (maxX <= X0 + E || minX >= X1 - E) continue;      // นอกลาน

                    Vector3 ca = Clamp(a), cb = Clamp(bb), cc = Clamp(c);
                    if (Vector3.Cross(cb - ca, cc - ca).sqrMagnitude < 1e-8f) continue;
                    Vector2 ua = UvAt(a, bb, c, uv[t[i]], uv[t[i + 1]], uv[t[i + 2]], ca);
                    Vector2 ub = UvAt(a, bb, c, uv[t[i]], uv[t[i + 1]], uv[t[i + 2]], cb);
                    Vector2 uc = UvAt(a, bb, c, uv[t[i]], uv[t[i + 1]], uv[t[i + 2]], cc);
                    Vector3 dy = Vector3.up * FloorDy;
                    b.Tri(s, ca + dy, cb + dy, cc + dy, n[t[i]], n[t[i + 1]], n[t[i + 2]], ua, ub, uc);
                }
            }
        }

        // ---------- อุดรูผนังล็อบบี้ + ผนังด้านในห้อง 107 ----------
        // ด้านล็อบบี้ (หันไป -x)
        b.Quad(0, new Vector3(8.1f, 0f, 1.35f), new Vector3(8.1f, 2.8f, 1.35f), new Vector3(8.1f, 2.8f, 2.65f), new Vector3(8.1f, 0f, 2.65f), Vector3.left);
        // ด้านห้อง 107 (หันไป +x)
        b.Quad(0, new Vector3(X0, 0f, 3.95f), new Vector3(X0, 2.8f, 3.95f), new Vector3(X0, 2.8f, 0.2f), new Vector3(X0, 0f, 0.2f), Vector3.right);

        // ---------- พื้นห้อง ----------
        b.Quad(1, new Vector3(8.1f, 0f, 0.2f), new Vector3(8.1f, 0f, 3.95f), new Vector3(16.8f, 0f, 3.95f), new Vector3(16.8f, 0f, 0.2f), Vector3.up);
        b.Quad(1, new Vector3(16.8f, -0.2f, 0.2f), new Vector3(16.8f, -0.2f, 3.95f), new Vector3(8.1f, -0.2f, 3.95f), new Vector3(8.1f, -0.2f, 0.2f), Vector3.down);

        var mesh = new Mesh { name = "Inside_Floor1_Rooms" };
        mesh.indexFormat = b.v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(b.v);
        mesh.SetNormals(b.n);
        mesh.SetUVs(0, b.uv);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(b.tris[0], 0);
        mesh.SetTriangles(b.tris[1], 1);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        Unwrapping.GenerateSecondaryUVSet(mesh);
        return mesh;
    }

    static Vector3 Clamp(Vector3 p) => new Vector3(Mathf.Clamp(p.x, X0, X1), p.y, p.z);

    // uv ของจุด p บนระนาบสามเหลี่ยม abc (barycentric)
    static Vector2 UvAt(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc, Vector3 p)
    {
        Vector3 v0 = b - a, v1 = c - a, v2 = p - a;
        float d00 = Vector3.Dot(v0, v0), d01 = Vector3.Dot(v0, v1), d11 = Vector3.Dot(v1, v1);
        float d20 = Vector3.Dot(v2, v0), d21 = Vector3.Dot(v2, v1);
        float den = d00 * d11 - d01 * d01;
        if (Mathf.Abs(den) < 1e-10f) return ua;
        float w1 = (d11 * d20 - d01 * d21) / den;
        float w2 = (d00 * d21 - d01 * d20) / den;
        return ua * (1f - w1 - w2) + ub * w1 + uc * w2;
    }
}
