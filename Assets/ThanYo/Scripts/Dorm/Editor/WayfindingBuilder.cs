using System.Collections.Generic;
using System.Text.RegularExpressions;
using BlurredRoom;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ThanYo > Blurred Room > 12. เคาะประตู + ทะเบียนผู้เช่า + ป้ายชั้น + วิธีเล่น
// Inside : ใส่ RoomKnock ทุกประตูห้องที่ล็อกถาวร, กระดาษทะเบียนบนเคาน์เตอร์, ป้ายชั้น/เลขห้องปลายทางเดิน, ป้ายผังห้องในล็อบบี้, ป๊อปอัปสอน + หน้าวิธีเล่น
// ThanYo : ป๊อปอัปสอน + หน้าวิธีเล่น
// กดซ้ำได้ (สร้างป้าย/กระดาษใหม่, ค่าใน RoomKnock ที่ปรับไว้ไม่ถูกทับ ยกเว้นชนิดห้อง)
public static class WayfindingBuilder
{
    const string Root = "BR_Guide";

    [MenuItem("ThanYo/Blurred Room/12. เคาะประตู + ทะเบียนผู้เช่า + ป้ายชั้น + วิธีเล่น")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "Inside" && scene.name != "ThanYo") { EditorUtility.DisplayDialog("Guide", "เปิดซีน Inside หรือ ThanYo ก่อน", "OK"); return; }

        var old = GameObject.Find(Root);
        if (old) Undo.DestroyObjectImmediate(old);
        var root = new GameObject(Root);
        Undo.RegisterCreatedObjectUndo(root, "Guide");
        root.AddComponent<TutorialHints>();
        root.AddComponent<HelpOverlay>();

        var log = new System.Text.StringBuilder("[Guide] " + scene.name + "\n");
        if (scene.name == "Inside")
        {
            var reg = Register(root.transform, log);
            Knocks(reg, log);
            Signs(root.transform, log);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeObject = root;
        Debug.Log(log.ToString());
    }

    // ---------------- กระดาษทะเบียนบนเคาน์เตอร์ ----------------
    static TenantRegister Register(Transform root, System.Text.StringBuilder log)
    {
        var counter = GameObject.Find("Dorm_Interior/Floor_1/Reception_Counter");
        float top = 1.0f;
        if (counter) top = counter.GetComponent<Renderer>().bounds.max.y;
        var go = new GameObject("Tenant_Register");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(7.15f, top + 0.002f, 2.3f);
        go.transform.rotation = Quaternion.Euler(0f, 78f, 0f);

        var sheet = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sheet.name = "Sheet";
        Object.DestroyImmediate(sheet.GetComponent<Collider>());
        sheet.transform.SetParent(go.transform, false);
        sheet.transform.localScale = new Vector3(0.24f, 0.003f, 0.32f);
        sheet.GetComponent<Renderer>().sharedMaterial = PaperMat();
        // เส้นแดงเล็กๆ บนกระดาษ ให้เห็นจากไกลว่ามีอะไรเขียนอยู่
        for (int i = 0; i < 5; i++)
        {
            var l = GameObject.CreatePrimitive(PrimitiveType.Cube);
            l.name = "Line" + i;
            Object.DestroyImmediate(l.GetComponent<Collider>());
            l.transform.SetParent(go.transform, false);
            l.transform.localPosition = new Vector3(0f, 0.0025f, 0.11f - i * 0.05f);
            l.transform.localScale = new Vector3(i == 1 || i == 3 ? 0.16f : 0.12f, 0.001f, 0.004f);
            l.GetComponent<Renderer>().sharedMaterial = (i == 1 || i == 3) ? RedMat() : InkMat();
        }
        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(0.3f, 0.06f, 0.38f); col.center = new Vector3(0, 0.02f, 0);
        var reg = go.AddComponent<TenantRegister>();
        log.Append("+ ทะเบียนผู้เช่าบนเคาน์เตอร์ (y=" + top.ToString("F2") + ")\n");
        return reg;
    }

    // ---------------- RoomKnock ทุกประตูห้อง ----------------
    static void Knocks(TenantRegister reg, System.Text.StringBuilder log)
    {
        var vanished = new HashSet<string>();
        foreach (var v in reg.vanished) vanished.Add(v.room);
        var tenantByDoor = new Dictionary<Door, TenantNpc>();
        foreach (var t in Object.FindObjectsByType<TenantNpc>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.homeDoor) tenantByDoor[t.homeDoor] = t;

        int nT = 0, nV = 0, nE = 0;
        var re = new Regex(@"^Door_(\d{3})$");
        foreach (var door in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var m = re.Match(door.name);
            if (!m.Success) continue;
            string room = m.Groups[1].Value;
            var sl = door.GetComponent<StoryLock>();
            bool storyRule = sl && (!string.IsNullOrEmpty(sl.requiredItemId) || !string.IsNullOrEmpty(sl.unlockFlag));
            if (storyRule) { log.Append("  ข้าม " + door.name + " (มีเงื่อนไขเนื้อเรื่อง)\n"); continue; }
            if (!sl) sl = Undo.AddComponent<StoryLock>(door.gameObject);
            sl.lockedMessage = "";

            var rk = door.GetComponent<RoomKnock>() ?? Undo.AddComponent<RoomKnock>(door.gameObject);
            rk.room = room;
            if (tenantByDoor.TryGetValue(door, out var tn)) { rk.kind = RoomKnock.Kind.Tenant; rk.tenant = tn; nT++; }
            else if (vanished.Contains(room)) { rk.kind = RoomKnock.Kind.Vanished; rk.tenant = null; nV++; }
            else { rk.kind = RoomKnock.Kind.Empty; rk.tenant = null; nE++; }
            EditorUtility.SetDirty(rk);
        }
        log.Append("+ เคาะประตู: ผู้เช่า " + nT + " ห้อง, คนหายปริศนา " + nV + " ห้อง, ห้องว่าง " + nE + " ห้อง\n");
    }

    // ---------------- ป้าย ----------------
    static void Signs(Transform root, System.Text.StringBuilder log)
    {
        var signs = new GameObject("Signs").transform;
        signs.SetParent(root, false);
        string[] floorText =
        {
            "ชั้น 1\n<size=60%>ล็อบบี้ · ห้องออฟฟิศ · ห้องซักผ้า\nห้อง 101 – 107\nบันไดขึ้นชั้น 2 – 3 อยู่ปลายทางเดินทั้งสองฝั่ง</size>",
            "ชั้น 2\n<size=60%>ห้อง 201 – 212\nห้องน้ำ · ห้องเก็บของ\nบันไดอยู่ปลายทางเดินทั้งสองฝั่ง</size>",
            "ชั้น 3\n<size=60%>ห้อง 301 – 312\nห้องเก็บของ (ห้องพิธี)\nชั้นบนปิดซ่อม ห้ามขึ้น</size>",
        };
        for (int f = 0; f < floorText.Length; f++)
        {
            float y = 2.8f * f + 2.0f;   // สูงพอไม่ทับสวิตช์ไฟ
            Plaque(signs, "Sign_F" + (f + 1) + "_West", new Vector3(0.215f, y, 5.0f), Quaternion.LookRotation(Vector3.left), floorText[f], new Vector2(1.3f, 0.9f));
            Plaque(signs, "Sign_F" + (f + 1) + "_East", new Vector3(21.785f, y, 5.0f), Quaternion.LookRotation(Vector3.right), floorText[f], new Vector2(1.3f, 0.9f));
        }

        // ผังห้องในล็อบบี้ (ผนังหลังเคาน์เตอร์)
        float z = 3.1f;
        var painting = GameObject.Find("Painting_6");
        if (painting && Mathf.Abs(painting.transform.position.z - z) < 1f) z = painting.transform.position.z > 2f ? 0.95f : 3.1f;
        Plaque(signs, "Directory_Lobby", new Vector3(8.085f, 1.75f, z), Quaternion.LookRotation(Vector3.right),
               "ผังห้องพัก\n<size=55%>ชั้น 1 : 101 – 107\nชั้น 2 : 201 – 212\nชั้น 3 : 301 – 312\n\nเลขตัวแรก = ชั้น\nทะเบียนผู้เช่า อยู่บนเคาน์เตอร์</size>",
               new Vector2(1.0f, 1.0f));
        log.Append("+ ป้ายชั้น 6 ป้าย + ผังห้องในล็อบบี้\n");
    }

    static void Plaque(Transform parent, string name, Vector3 pos, Quaternion facing, string text, Vector2 size)
    {
        // facing = ทิศที่มองเข้าหาผนัง (คนอ่านยืนหันไปทางนี้) — ป้ายยื่นออกมาทางคนอ่าน
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, facing);

        var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "Board";
        Object.DestroyImmediate(board.GetComponent<Collider>());
        board.transform.SetParent(go.transform, false);
        board.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        board.transform.localScale = new Vector3(size.x, size.y, 0.02f);
        board.GetComponent<Renderer>().sharedMaterial = BoardMat();

        var tGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshPro));
        tGo.transform.SetParent(go.transform, false);
        tGo.transform.localPosition = new Vector3(0f, 0f, -0.025f);   // TMP อ่านได้เมื่อมองไปทาง +z ของตัวเอง
        var t = tGo.GetComponent<TextMeshPro>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ThanYo/Resources/ThaiFont.asset") ?? Resources.Load<TMP_FontAsset>("ThaiFont");
        if (font) t.font = font;
        t.rectTransform.sizeDelta = size * 0.9f;
        t.fontSize = 1.6f;
        t.enableAutoSizing = true; t.fontSizeMin = 0.4f; t.fontSizeMax = 1.6f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(0.92f, 0.86f, 0.7f);
        t.text = text;
        t.textWrappingMode = TextWrappingModes.Normal;
        DormFX.ThaiMarks(t);
    }

    // ---------------- materials ----------------
    static Material Mat(string name, Color c)
    {
        const string dir = "Assets/ThanYo/Materials/Guide";
        if (!AssetDatabase.IsValidFolder(dir)) { System.IO.Directory.CreateDirectory(dir); AssetDatabase.Refresh(); }
        string path = dir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(m);
        return m;
    }
    static Material PaperMat() => Mat("Guide_Paper", new Color(0.86f, 0.82f, 0.72f));
    static Material InkMat() => Mat("Guide_Ink", new Color(0.1f, 0.08f, 0.06f));
    static Material RedMat() => Mat("Guide_RedPen", new Color(0.6f, 0.05f, 0.04f));
    static Material BoardMat() => Mat("Guide_Board", new Color(0.12f, 0.1f, 0.08f));
}
