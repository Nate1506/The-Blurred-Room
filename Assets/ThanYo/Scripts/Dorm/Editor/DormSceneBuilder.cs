using System.Collections.Generic;
using System.IO;
using BlurredRoom;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// เมนูประกอบระบบหอพักลงซีน Inside (รันซ้ำได้ — ลบของเก่าใต้ BR_Dorm แล้วสร้างใหม่)
//   ThanYo > Blurred Room > 1. Import assets   : ฟอนต์ไทย, รูปผู้เช่า/ผี จาก vault, เบาะแส (ClueData), ฉากจบ (EndingData)
//   ThanYo > Blurred Room > 2. Build in Inside : วางผู้เช่า, จุดสำรวจ, กระดาน, กล้อง, วงพิธี ฯลฯ แล้วเซฟซีน
// ของทุกชิ้นอยู่ใต้ BR_Dorm ขยับตำแหน่งในซีนได้ตามใจ (ถ้ารัน Build ใหม่ ตำแหน่งจะกลับเป็นค่าในไฟล์นี้)
public static class DormSceneBuilder
{
    const string Res = "Assets/ThanYo/Resources";
    const string MatDir = "Assets/ThanYo/Materials/Dorm";
    const string EndDir = "Assets/ThanYo/Story/Dorm";
    const string Vault = "D:/Portfolio/Obsidian/Claude/projects/blurred-room";

    // =====================================================================
    //  1. assets
    // =====================================================================
    [MenuItem("ThanYo/Blurred Room/1. Import assets (font, images, clues, endings)")]
    public static void ImportAssets()
    {
        Folder(Res); Folder(Res + "/Dorm"); Folder(Res + "/DormClues"); Folder(EndDir); Folder("Assets/ThanYo/Fonts"); Folder(MatDir);

        // ---- Thai font ----
        string ttf = "Assets/ThanYo/Fonts/LeelawUI.ttf";
        if (!File.Exists(ttf) && File.Exists("C:/Windows/Fonts/LeelawUI.ttf")) File.Copy("C:/Windows/Fonts/LeelawUI.ttf", ttf);
        if (!File.Exists(ttf) && File.Exists("C:/Windows/Fonts/tahoma.ttf")) { ttf = "Assets/ThanYo/Fonts/tahoma.ttf"; if (!File.Exists(ttf)) File.Copy("C:/Windows/Fonts/tahoma.ttf", ttf); }

        // ---- images from the Obsidian vault ----
        var images = new List<string>();
        for (int i = 1; i <= 6; i++) images.Add(CopyIn(Vault + "/tenants/img/tenant-" + i + ".png", Res + "/Dorm/tenant-" + i + ".png"));
        images.Add(CopyIn(Vault + "/refs/ref-doorway-creature.png", Res + "/Dorm/creature.png"));
        AssetDatabase.Refresh();

        foreach (var p in images)
        {
            if (p == null) continue;
            var ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (!ti) continue;
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.maxTextureSize = 1024; ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }

        string fontAssetPath = Res + "/ThaiFont.asset";
        if (File.Exists(ttf) && !AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath))
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
            var fa = TMP_FontAsset.CreateFontAsset(font, 72, 6, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            fa.name = "ThaiFont";
            AssetDatabase.CreateAsset(fa, fontAssetPath);
            if (fa.material) { fa.material.name = "ThaiFont Material"; AssetDatabase.AddObjectToAsset(fa.material, fa); }
            foreach (var tex in fa.atlasTextures) if (tex) { tex.name = "ThaiFont Atlas"; AssetDatabase.AddObjectToAsset(tex, fa); }
            var liberation = TMP_Settings.defaultFontAsset;
            if (liberation) fa.fallbackFontAssetTable = new List<TMP_FontAsset> { liberation };
            EditorUtility.SetDirty(fa);
        }

        // ---- clues ----
        foreach (var c in DormData.Clues)
        {
            string path = Res + "/DormClues/" + c.id + ".asset";
            var clue = AssetDatabase.LoadAssetAtPath<ClueData>(path);
            if (!clue) { clue = ScriptableObject.CreateInstance<ClueData>(); AssetDatabase.CreateAsset(clue, path); }
            var so = new SerializedObject(clue);
            so.FindProperty("id").stringValue = c.id;
            so.FindProperty("title").stringValue = c.title;
            so.FindProperty("description").stringValue = c.body;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- endings ----
        Ending("Ending_Dawn_Normal", "เช้าวันที่สี่", "แสงแดดส่องเข้ามาในทางเดิน ผู้เช่าส่วนใหญ่ยังอยู่ครบ\n\nแต่ตึกยังไม่ปล่อยใครไปไหน... และคุณก็เช่นกัน", "Dawn", 0);
        Ending("Ending_Dawn_Bad", "ตึกที่เงียบเกินไป", "หลายห้องว่างเปล่า ผนังดูหนาขึ้นกว่าเมื่อวาน\n\nตอนเงียบๆ คุณได้ยินเสียงเคาะเบาๆ... ดังมาจากในกำแพง", "Dawn", 1, "missing_4");
        Ending("Ending_Dawn_Secret", "ผู้ดูแลคนสุดท้าย", "ไม่เหลือใครแล้ว\n\nคุณเปิดสมุดผู้เช่าเล่มเก่า ชื่อสุดท้ายที่ยังไม่มีวันย้ายออกคือ 'นนท์'\n\nคุณไม่เคยมาสมัครงานที่นี่... คุณไม่เคยออกไปไหนเลยต่างหาก", "Dawn", 2, "missing_6");
        Ending("Ending_PastDimension", "หลุดเข้าไปในอดีต", "สติขาดผึง คุณล้มฟุบกับพื้น หน้ามืดไป\n\nเมื่อลืมตาอีกครั้ง ผนังหอพักสีใหม่เอี่ยม ปฏิทินบนผนังเขียนว่า พ.ศ. 2535", EndingReasons.SanityLost, 0);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Dorm] Import done. Font: " + (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath) ? "OK" : "missing"));
    }

    static string CopyIn(string src, string dst)
    {
        if (!File.Exists(src)) { Debug.LogWarning("[Dorm] Missing image " + src); return File.Exists(dst) ? dst : null; }
        File.Copy(src, dst, true);
        return dst;
    }

    static void Ending(string file, string title, string text, string reason, int priority, params string[] flags)
    {
        string path = EndDir + "/" + file + ".asset";
        var e = AssetDatabase.LoadAssetAtPath<EndingData>(path);
        if (!e) { e = ScriptableObject.CreateInstance<EndingData>(); AssetDatabase.CreateAsset(e, path); }
        var so = new SerializedObject(e);
        so.FindProperty("id").stringValue = file;
        so.FindProperty("title").stringValue = title;
        so.FindProperty("text").stringValue = text;
        so.FindProperty("triggerReason").stringValue = reason;
        so.FindProperty("priority").intValue = priority;
        var rf = so.FindProperty("requiredFlags");
        rf.arraySize = flags.Length;
        for (int i = 0; i < flags.Length; i++) rf.GetArrayElementAtIndex(i).stringValue = flags[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        Folder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    // =====================================================================
    //  2. scene
    // =====================================================================
    static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();
    static Transform root;

    [MenuItem("ThanYo/Blurred Room/2. Build in Inside scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "Inside") { Debug.LogError("[Dorm] Open the Inside scene first."); return; }
        Physics.SyncTransforms();
        matCache.Clear();

        var old = GameObject.Find("BR_Dorm");
        if (old) Object.DestroyImmediate(old);
        root = new GameObject("BR_Dorm").transform;

        // เรื่องเก่า (มะลิ 204 / พิธีห้อง 305) ปิดไว้ ไม่ลบ — แต่ย้ายโต๊ะห้องออฟฟิศออกมาก่อน (ยังใช้อยู่)
        foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (go.name == "Office_Desk" && go.parent && go.parent.name == "Story_Props") { go.SetParent(null, true); go.gameObject.SetActive(true); }
        SetActive("Story_Props", false);
        SetActive("Story_NPCs", false);

        var doors = new Dictionary<string, Door>();
        foreach (var d in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string key = d.name;
            if (key == "Door_Storage" || key == "Door_Bath") key += "_" + Mathf.RoundToInt(d.transform.position.y / 2.8f + 1);
            doors[key] = d;
        }

        // ---------- tenants ----------
        var tenantsRoot = Group("Tenants");
        foreach (var t in DormData.Tenants)
        {
            if (!doors.TryGetValue("Door_" + t.room, out var d)) { Debug.LogWarning("[Dorm] no door for room " + t.room); continue; }
            var go = new GameObject("Tenant_" + t.id + "_" + t.room);
            go.transform.SetParent(tenantsRoot, false);
            Transform sp = d.VisitorStandPoint;
            Vector3 f = Flat(sp.forward);
            go.transform.SetPositionAndRotation(sp.position + f * 0.35f, Quaternion.LookRotation(-f));
            var n = go.AddComponent<TenantNpc>();
            n.tenantId = t.id; n.homeDoor = d;
            n.model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThanYo/Prefabs/Tenants/Tenant_" + t.id + ".prefab");
            NpcFields(n, "tenant_" + t.id, t.name);
        }

        var fakeGo = new GameObject("FakeVisitor");
        fakeGo.transform.SetParent(root, false);
        fakeGo.transform.position = new Vector3(0, -30, 0);
        var fake = fakeGo.AddComponent<FakeVisitor>();
        NpcFields(fake, "fake", "???");

        // ---------- office (ห้องผู้ดูแล) ----------
        var office = Group("Office");
        Vector3 oc = new Vector3(5.2f, 1.45f, 7.9f);
        var sd = Object.FindFirstObjectByType<StoryDirector>();
        if (sd) oc = sd.officeArea.center + Vector3.up * 0.15f;

        Vector3 west = WallPoint(oc, Vector3.left, out Vector3 westN);
        var board = Prop(office, "InvestigationBoard", west + westN * 0.04f, Quaternion.LookRotation(westN));
        Cube(board, "Board", Vector3.zero, new Vector3(1.5f, 1.0f, 0.04f), "4a3423");
        for (int i = 0; i < 6; i++)
            Cube(board, "Paper" + i, new Vector3(-0.5f + (i % 3) * 0.5f, i < 3 ? 0.2f : -0.22f, 0.025f), new Vector3(0.32f, 0.3f, 0.01f), i == 4 ? "c9b18a" : "ddd6c4");
        Cube(board, "String", new Vector3(0, 0, 0.032f), new Vector3(1.1f, 0.01f, 0.005f), "a01818");
        board.gameObject.AddComponent<InvestigationBoard>();

        Vector3 back = WallPoint(oc + Vector3.left * 1.2f, Vector3.forward, out Vector3 backN);
        var group = Prop(office, "OldGroupPhoto", back + backN * 0.03f + Vector3.up * 0.1f, Quaternion.LookRotation(backN));
        Cube(group, "Frame", Vector3.zero, new Vector3(0.6f, 0.42f, 0.03f), "3a2a1a");
        Cube(group, "Photo", new Vector3(0, 0, 0.018f), new Vector3(0.52f, 0.34f, 0.005f), "9c8a6a");
        Action(group, DayActionPoint.Kind.Inspect, "c_groupphoto", "รูปหมู่ผู้เช่าเก่าบนผนัง", 2);

        Vector3 desk = Snap(new Vector3(7.5f, 1.6f, 9.35f));
        var monitor = Prop(office, "CCTV_Monitor", desk, Quaternion.LookRotation(Flat(oc - desk)));
        Cube(monitor, "Body", new Vector3(0, 0.2f, 0), new Vector3(0.48f, 0.34f, 0.3f), "222222");
        Cube(monitor, "Screen", new Vector3(0, 0.21f, 0.152f), new Vector3(0.4f, 0.27f, 0.005f), "1f3a24");
        var cctvMon = monitor.gameObject.AddComponent<CctvMonitor>();

        Vector3 desk2 = Snap(new Vector3(6.95f, 1.6f, 9.35f));
        var oldReg = Prop(office, "OldRegister", desk2, Quaternion.Euler(0, 15, 0));
        Cube(oldReg, "Book", new Vector3(0, 0.03f, 0), new Vector3(0.24f, 0.06f, 0.32f), "5a1f12");
        Action(oldReg, DayActionPoint.Kind.Inspect, "c_oldregister", "สมุดผู้เช่าเล่มเก่าในลิ้นชัก", 3);

        // ---------- lobby ----------
        var lobby = Group("Lobby");
        var reg = Prop(lobby, "Register", Snap(new Vector3(6.35f, 1.6f, 1.6f)), Quaternion.Euler(0, -100, 0));
        Cube(reg, "Book", new Vector3(0, 0.025f, 0), new Vector3(0.26f, 0.05f, 0.34f), "1f3550");
        Action(reg, DayActionPoint.Kind.Inspect, "c_register", "สมุดผู้เช่า", 1);

        // รูปพี่บิ๊ก = รูปติดผนังเหนือหิ้งเทียน (Painting_6)
        var painting = GameObject.Find("Painting_6");
        if (painting && !painting.GetComponent<StaffPhoto>()) painting.AddComponent<StaffPhoto>();
        if (painting && !painting.GetComponent<Collider>()) painting.AddComponent<BoxCollider>();

        var incense = Prop(lobby, "Incense", Snap(new Vector3(6.35f, 1.6f, 3.35f)), Quaternion.identity);
        Cube(incense, "Base", new Vector3(0, 0.02f, 0), new Vector3(0.1f, 0.04f, 0.1f), "6a4a2a");
        for (int i = 0; i < 3; i++) Cube(incense, "Stick" + i, new Vector3(-0.025f + i * 0.025f, 0.12f, 0), new Vector3(0.008f, 0.2f, 0.008f), "8a2a1a");
        incense.gameObject.AddComponent<RitualSupply>().itemId = DormData.Incense;

        Vector3 lobbyWall = WallPoint(new Vector3(4.5f, 1.2f, 2.2f), Vector3.left, out Vector3 lobbyN);
        var pillar = Prop(lobby, "PillarCrack", lobbyWall + lobbyN * 0.03f, Quaternion.LookRotation(lobbyN));
        Cube(pillar, "Patch", Vector3.zero, new Vector3(0.45f, 0.6f, 0.02f), "b5b0a5");
        Cube(pillar, "Cloth", new Vector3(0.05f, -0.1f, 0.015f), new Vector3(0.06f, 0.18f, 0.01f), "9a1010");
        Action(pillar, DayActionPoint.Kind.Inspect, "c_pillar", "รอยปูนใหม่บนเสาเอก", 2);

        // ---------- laundry ----------
        var laundry = Group("Laundry");
        var chalk = Prop(laundry, "Chalk", Snap(new Vector3(17.7f, 1.6f, 2.6f)), Quaternion.Euler(0, 30, 0));
        Cube(chalk, "Box", new Vector3(0, 0.03f, 0), new Vector3(0.12f, 0.06f, 0.08f), "d0c8b0");
        Cube(chalk, "Stick", new Vector3(0, 0.07f, 0), new Vector3(0.09f, 0.015f, 0.015f), "ffffff");
        chalk.gameObject.AddComponent<RitualSupply>().itemId = DormData.Chalk;

        // ---------- floor 3 ----------
        var f3 = Group("Floor3");
        var circle = Prop(f3, "RitualCircle", Snap(new Vector3(20.8f, 6.8f, 2.1f)), Quaternion.identity);
        Cube(circle, "Chalk", new Vector3(0, 0.01f, 0), new Vector3(1.2f, 0.02f, 1.2f), "e8e2d0");
        Cube(circle, "Inner", new Vector3(0, 0.012f, 0), new Vector3(0.9f, 0.02f, 0.9f), "6d675c");
        for (int i = 0; i < 4; i++)
            Cube(circle, "Candle" + i, new Vector3(i % 2 == 0 ? -0.5f : 0.5f, 0.07f, i < 2 ? -0.5f : 0.5f), new Vector3(0.05f, 0.14f, 0.05f), "f0ead0");
        circle.gameObject.AddComponent<RitualCircle>();

        var cloth = Prop(f3, "BurntCloth", Snap(new Vector3(21.4f, 7.2f, 1.0f)), Quaternion.Euler(0, 20, 0));
        Cube(cloth, "Cloth", new Vector3(0, 0.01f, 0), new Vector3(0.4f, 0.02f, 0.3f), "d8d0c0");
        Cube(cloth, "Burn", new Vector3(0.12f, 0.022f, 0), new Vector3(0.16f, 0.005f, 0.3f), "2a1a10");
        Cube(cloth, "Ink", new Vector3(-0.06f, 0.022f, 0), new Vector3(0.18f, 0.005f, 0.04f), "a01010");
        Action(cloth, DayActionPoint.Kind.Inspect, "c_storage", "หลังชั้นในห้องเก็บของ", 1);

        if (doors.TryGetValue("Door_309", out var d309))
        {
            Vector3 f = Flat(d309.VisitorStandPoint.forward);
            var sign = Prop(f3, "Room309_Sign", d309.transform.position - f * 0.08f + Vector3.up * 1.6f, Quaternion.LookRotation(-f));
            Cube(sign, "Sign", Vector3.zero, new Vector3(0.3f, 0.12f, 0.02f), "c0b080");
            Action(sign, DayActionPoint.Kind.Photo, "c_photo", "ประตูห้อง 309 (ห้องว่าง)", 1);
        }

        // ---------- CCTV ----------
        var cctvGo = new GameObject("CCTV");
        cctvGo.transform.SetParent(root, false);
        var cctv = cctvGo.AddComponent<CctvSystem>();
        var cams = new List<Camera>();
        for (int floor = 1; floor <= 3; floor++)
        {
            float y = (floor - 1) * 2.8f;
            var camGo = new GameObject("Cam_Floor" + floor);
            camGo.transform.SetParent(cctvGo.transform, false);
            camGo.transform.position = new Vector3(21.3f, y + 2.35f, 5.0f);
            camGo.transform.LookAt(new Vector3(3f, y + 0.9f, 5.0f));
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 72f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 40f; cam.enabled = false;
            cams.Add(cam);
        }
        cctv.cams = cams.ToArray();
        cctvMon.system = cctv;

        var anomGo = new GameObject("Anomalies");
        anomGo.transform.SetParent(root, false);
        var anom = anomGo.AddComponent<AnomalyDirector>();
        var win = GameObject.Find("Dorm_Interior/Floor_1/Windows/Win_Caretaker_1");
        if (win) anom.officeWindow = win.transform;
        anom.officeCenter = oc;
        cctv.anomalies = anom;

        new GameObject("DormOverlay", typeof(DormOverlay)).transform.SetParent(root, false);
        new GameObject("UpperFloorGuard", typeof(UpperFloorGuard)).transform.SetParent(root, false);

        // ---------- story director ----------
        if (sd)
        {
            sd.fake = fake;
            sd.anomalies = anom;
            if (!sd.caretakerDoor && doors.TryGetValue("Door_Caretaker", out var care)) sd.caretakerDoor = care;
            EditorUtility.SetDirty(sd);
        }
        else Debug.LogError("[Dorm] No StoryDirector in scene (object 'Story').");

        // ---------- endings ----------
        var em = Object.FindFirstObjectByType<EndingManager>();
        if (em)
        {
            var so = new SerializedObject(em);
            var arr = so.FindProperty("endings");
            string[] names = { "Ending_Dawn_Normal", "Ending_Dawn_Bad", "Ending_Dawn_Secret", "Ending_PastDimension" };
            arr.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<EndingData>(EndDir + "/" + names[i] + ".asset");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Dorm] Built BR_Dorm in Inside and saved the scene.");
    }

    // ---------------- helpers ----------------
    static Transform Group(string name)
    {
        var g = new GameObject(name).transform;
        g.SetParent(root, false);
        return g;
    }

    static Transform Prop(Transform parent, string name, Vector3 pos, Quaternion rot)
    {
        var g = new GameObject(name).transform;
        g.SetParent(parent, false);
        g.SetPositionAndRotation(pos, rot);
        return g;
    }

    static GameObject Cube(Transform parent, string name, Vector3 localPos, Vector3 size, string hex)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = Mat(hex);
        return go;
    }

    static void Action(Transform t, DayActionPoint.Kind kind, string clue, string label, int minNight)
    {
        var a = t.gameObject.AddComponent<DayActionPoint>();
        a.kind = kind; a.clueId = clue; a.label = label; a.minNight = minNight;
    }

    static Material Mat(string hex)
    {
        if (matCache.TryGetValue(hex, out var m) && m) return m;
        string path = MatDir + "/Dorm_" + hex + ".mat";
        m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m)
        {
            Folder(MatDir);
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(m, path);
        }
        matCache[hex] = m;
        return m;
    }

    static void NpcFields(NpcBase npc, string id, string display)
    {
        var so = new SerializedObject(npc);
        so.FindProperty("npcId").stringValue = id;
        so.FindProperty("displayName").stringValue = display;
        so.FindProperty("canBeVisitor").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetActive(string name, bool on)
    {
        foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (go.parent == null && go.name == name) go.gameObject.SetActive(on);
    }

    static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward; }

    /// <summary>ยิงลงพื้น/โต๊ะจากด้านบน คืนจุดที่ตกกระทบ</summary>
    static Vector3 Snap(Vector3 from)
    {
        if (Physics.Raycast(from, Vector3.down, out var hit, 3.5f, ~(1 << 2), QueryTriggerInteraction.Ignore)) return hit.point;
        return new Vector3(from.x, Mathf.Floor(from.y / 2.8f) * 2.8f, from.z);
    }

    /// <summary>ยิงไปหาผนัง คืนจุดบนผนัง + normal</summary>
    static Vector3 WallPoint(Vector3 from, Vector3 dir, out Vector3 normal)
    {
        if (Physics.Raycast(from, dir, out var hit, 8f, ~(1 << 2), QueryTriggerInteraction.Ignore)) { normal = Flat(hit.normal); return hit.point; }
        normal = -dir;
        return from + dir * 2f;
    }
}
