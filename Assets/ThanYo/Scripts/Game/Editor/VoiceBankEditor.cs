using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// หน้า Inspector ของ Voice Bank: รวบรวมบทพูดทุกบรรทัดในเกม แยกกลุ่ม + ช่องลากไฟล์เสียง + ปุ่มลองฟัง
[CustomEditor(typeof(VoiceBank))]
public class VoiceBankEditor : Editor
{
    const string AssetPath = "Assets/ThanYo/Resources/VoiceBank.asset";

    static readonly Dictionary<string, bool> groupOpen = new Dictionary<string, bool>();
    static readonly HashSet<int> lineOpen = new HashSet<int>();
    static string search = "";
    static bool onlyNoClip;

    [MenuItem("ThanYo/Blurred Room/8. Voice Bank (เสียงพากย์ทุกบทพูด)")]
    public static void OpenBank()
    {
        var bank = AssetDatabase.LoadAssetAtPath<VoiceBank>(AssetPath);
        if (!bank)
        {
            System.IO.Directory.CreateDirectory("Assets/ThanYo/Resources");
            bank = CreateInstance<VoiceBank>();
            AssetDatabase.CreateAsset(bank, AssetPath);
            AssetDatabase.SaveAssets();
        }
        if (bank.lines.Count == 0 && !EditorApplication.isPlaying) Collect(bank);
        Selection.activeObject = bank;
        EditorGUIUtility.PingObject(bank);
    }

    // =====================================================================
    //  Inspector
    // =====================================================================
    public override void OnInspectorGUI()
    {
        var bank = (VoiceBank)target;
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "1) กด 'รวบรวมบทพูดทั้งหมด' (ทำซ้ำได้ทุกครั้งที่แก้บท — ไฟล์เสียงที่ลากไว้ไม่หาย)\n" +
            "2) ลากไฟล์เสียงใส่ช่อง 'เสียง' ของแต่ละบรรทัด หรือ ลากหลายไฟล์วางบนหัวกลุ่ม = ใส่ให้ตามลำดับชื่อไฟล์\n" +
            "3) ตั้งชื่อไฟล์ตาม 'รหัส' (เช่น phum_interview_1.wav) แล้วกด 'จับคู่อัตโนมัติ' ก็ได้ (+ _fake = เสียงของปลอม)\n" +
            "ตอนเล่น ซับไหนตรงกับบรรทัดในนี้จะพูดเอง และซับค้างจนพูดจบ", MessageType.Info);

        DrawPropertiesExcluding(serializedObject, "m_Script", "lines");
        EditorGUILayout.Space(6);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                if (GUILayout.Button("รวบรวมบทพูดทั้งหมด", GUILayout.Height(28))) { serializedObject.ApplyModifiedProperties(); Collect(bank); GUIUtility.ExitGUI(); }
            if (GUILayout.Button("จับคู่ไฟล์เสียงอัตโนมัติ", GUILayout.Height(28))) { serializedObject.ApplyModifiedProperties(); AutoMatch(bank); GUIUtility.ExitGUI(); }
            if (GUILayout.Button("■ หยุดเสียง", GUILayout.Width(90), GUILayout.Height(28))) StopPreview();
        }

        int total = bank.lines.Count, withClip = bank.lines.Count(l => l.clip);
        var r = EditorGUILayout.GetControlRect(false, 18);
        EditorGUI.ProgressBar(r, total == 0 ? 0f : (float)withClip / total, "มีเสียงแล้ว " + withClip + " / " + total + " บรรทัด");

        using (new EditorGUILayout.HorizontalScope())
        {
            search = EditorGUILayout.TextField("ค้นหา", search);
            onlyNoClip = GUILayout.Toggle(onlyNoClip, "เฉพาะที่ยังไม่มีเสียง", "Button", GUILayout.Width(140));
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("กางทุกกลุ่ม")) foreach (var g in bank.lines.Select(l => l.group).Distinct()) groupOpen[g] = true;
            if (GUILayout.Button("พับทุกกลุ่ม")) groupOpen.Clear();
        }
        EditorGUILayout.Space(4);

        var linesProp = serializedObject.FindProperty("lines");
        var groups = new List<string>();
        var byGroup = new Dictionary<string, List<int>>();
        for (int i = 0; i < bank.lines.Count; i++)
        {
            var l = bank.lines[i];
            string g = string.IsNullOrEmpty(l.group) ? "(ไม่มีกลุ่ม)" : l.group;
            if (!byGroup.TryGetValue(g, out var list)) { list = new List<int>(); byGroup[g] = list; groups.Add(g); }
            list.Add(i);
        }

        int removeAt = -1;
        foreach (var g in groups)
        {
            var idx = byGroup[g];
            var shown = idx.Where(i => Visible(bank.lines[i])).ToList();
            if (shown.Count == 0) continue;
            int have = idx.Count(i => bank.lines[i].clip);
            groupOpen.TryGetValue(g, out bool open);
            if (!string.IsNullOrEmpty(search)) open = true;

            var hr = EditorGUILayout.GetControlRect(false, 22);
            EditorGUI.DrawRect(hr, have == idx.Count ? new Color(0.2f, 0.35f, 0.2f, 0.5f) : new Color(0f, 0f, 0f, 0.25f));
            bool nowOpen = EditorGUI.Foldout(new Rect(hr.x + 4, hr.y + 2, hr.width - 8, hr.height), open,
                g + "   (" + have + "/" + idx.Count + ")   ← ลากไฟล์เสียงหลายไฟล์มาวางตรงนี้ได้", true, EditorStyles.foldoutHeader);
            if (string.IsNullOrEmpty(search)) groupOpen[g] = nowOpen;
            HandleGroupDrop(hr, bank, idx);
            if (!nowOpen) continue;

            EditorGUI.indentLevel++;
            foreach (int i in shown)
                if (DrawLine(bank, linesProp.GetArrayElementAtIndex(i), i, g)) removeAt = i;
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        if (removeAt >= 0) { linesProp.DeleteArrayElementAtIndex(removeAt); lineOpen.Clear(); }

        EditorGUILayout.Space(6);
        if (GUILayout.Button("+ เพิ่มบรรทัดเอง"))
        {
            linesProp.arraySize++;
            var p = linesProp.GetArrayElementAtIndex(linesProp.arraySize - 1);
            p.FindPropertyRelative("id").stringValue = "custom_" + linesProp.arraySize;
            p.FindPropertyRelative("group").stringValue = "เพิ่มเอง";
            p.FindPropertyRelative("speaker").stringValue = "";
            p.FindPropertyRelative("text").stringValue = "";
            p.FindPropertyRelative("clip").objectReferenceValue = null;
            p.FindPropertyRelative("fakeClip").objectReferenceValue = null;
            p.FindPropertyRelative("missing").boolValue = false;
            groupOpen["เพิ่มเอง"] = true;
            lineOpen.Add(linesProp.arraySize - 1);
        }

        if (serializedObject.ApplyModifiedProperties()) bank.MarkDirtyMap();
    }

    static bool Visible(VoiceBank.Line l)
    {
        if (onlyNoClip && l.clip) return false;
        if (string.IsNullOrEmpty(search)) return true;
        string s = search.Trim();
        return (l.text ?? "").Contains(s) || (l.speaker ?? "").Contains(s) || (l.id ?? "").Contains(s);
    }

    /// <returns>true = กดลบ</returns>
    bool DrawLine(VoiceBank bank, SerializedProperty p, int index, string group)
    {
        var l = bank.lines[index];
        bool del = false;
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var st = new GUIStyle(EditorStyles.boldLabel) { richText = true };
                string who = string.IsNullOrEmpty(l.speaker) ? "<color=#999>(ไม่มีชื่อคนพูด)</color>" : l.speaker;
                GUILayout.Label(who + (l.missing ? "   <color=#e0a030>⚠ ไม่พบในเกมแล้ว</color>" : ""), st);
                GUILayout.FlexibleSpace();
                GUILayout.Label(l.id, EditorStyles.miniLabel);
            }
            EditorGUILayout.LabelField(l.text, EditorStyles.wordWrappedLabel);
            if (!string.IsNullOrEmpty(l.note)) EditorGUILayout.LabelField("โน้ต: " + l.note, EditorStyles.wordWrappedMiniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(p.FindPropertyRelative("clip"), new GUIContent("เสียง"));
                using (new EditorGUI.DisabledScope(!l.clip))
                    if (GUILayout.Button("▶", GUILayout.Width(26))) PlayPreview(l.clip);
            }
            bool isDoor = group.Contains("หน้าประตู") || l.fakeClip;
            if (isDoor)
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(p.FindPropertyRelative("fakeClip"), new GUIContent("เสียงของปลอม", "ตอนสิ่งผิดปกติเลียนเสียงคนนี้ (ว่าง = ใช้เสียงปกติ)"));
                    using (new EditorGUI.DisabledScope(!l.fakeClip))
                        if (GUILayout.Button("▶", GUILayout.Width(26))) PlayPreview(l.fakeClip);
                }

            bool open = lineOpen.Contains(index);
            bool nowOpen = EditorGUILayout.Foldout(open, "ตั้งค่าเพิ่ม", true);
            if (nowOpen != open) { if (nowOpen) lineOpen.Add(index); else lineOpen.Remove(index); }
            if (nowOpen)
            {
                EditorGUILayout.PropertyField(p.FindPropertyRelative("volume"), new GUIContent("ความดัง"));
                EditorGUILayout.PropertyField(p.FindPropertyRelative("pitch"), new GUIContent("ระดับเสียง"));
                EditorGUILayout.PropertyField(p.FindPropertyRelative("delay"), new GUIContent("รอก่อนพูด"));
                EditorGUILayout.PropertyField(p.FindPropertyRelative("note"), new GUIContent("โน้ตคนพากย์"));
                EditorGUILayout.HelpBox("แก้ 2 ช่องล่างต้องตรงกับที่ขึ้นจอเป๊ะ ไม่งั้นจะหาไม่เจอ", MessageType.None);
                EditorGUILayout.PropertyField(p.FindPropertyRelative("speaker"), new GUIContent("คนพูด"));
                EditorGUILayout.PropertyField(p.FindPropertyRelative("text"), new GUIContent("ข้อความ"));
                EditorGUILayout.PropertyField(p.FindPropertyRelative("id"), new GUIContent("รหัส"));
                EditorGUILayout.PropertyField(p.FindPropertyRelative("group"), new GUIContent("กลุ่ม"));
                if (GUILayout.Button("ลบบรรทัดนี้", GUILayout.Width(110))) del = true;
            }
        }
        return del;
    }

    // ลากหลายไฟล์มาวางบนหัวกลุ่ม -> ใส่ให้บรรทัดที่ยังว่าง เรียงตามชื่อไฟล์
    void HandleGroupDrop(Rect area, VoiceBank bank, List<int> idx)
    {
        var e = Event.current;
        if (!area.Contains(e.mousePosition)) return;
        if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;
        var clips = DragAndDrop.objectReferences.OfType<AudioClip>().OrderBy(c => c.name, System.StringComparer.OrdinalIgnoreCase).ToList();
        if (clips.Count == 0) return;
        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        if (e.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            Undo.RecordObject(bank, "ใส่เสียงทั้งกลุ่ม");
            int c = 0;
            foreach (int i in idx)
            {
                if (c >= clips.Count) break;
                if (bank.lines[i].clip && clips.Count < idx.Count) continue;   // ไฟล์น้อยกว่าบรรทัด = เติมเฉพาะช่องว่าง
                bank.lines[i].clip = clips[c++];
            }
            EditorUtility.SetDirty(bank);
            bank.MarkDirtyMap();
            serializedObject.Update();
        }
        e.Use();
    }

    // =====================================================================
    //  จับคู่อัตโนมัติจากชื่อไฟล์
    // =====================================================================
    static void AutoMatch(VoiceBank bank)
    {
        string folder = string.IsNullOrEmpty(bank.autoMatchFolder) ? "Assets" : bank.autoMatchFolder.TrimEnd('/');
        if (!AssetDatabase.IsValidFolder(folder)) { EditorUtility.DisplayDialog("Voice Bank", "ไม่พบโฟลเดอร์ " + folder, "OK"); return; }
        var map = new Dictionary<string, AudioClip>();
        foreach (var g in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g));
            if (c && !map.ContainsKey(c.name.ToLowerInvariant())) map[c.name.ToLowerInvariant()] = c;
        }
        Undo.RecordObject(bank, "จับคู่เสียงอัตโนมัติ");
        int n = 0;
        foreach (var l in bank.lines)
        {
            if (string.IsNullOrEmpty(l.id)) continue;
            string k = l.id.ToLowerInvariant();
            if (!l.clip && map.TryGetValue(k, out var c)) { l.clip = c; n++; }
            if (!l.fakeClip && map.TryGetValue(k + "_fake", out var f)) { l.fakeClip = f; n++; }
        }
        EditorUtility.SetDirty(bank);
        bank.MarkDirtyMap();
        EditorUtility.DisplayDialog("Voice Bank", "จับคู่ได้ " + n + " ไฟล์ (ค้นใน " + folder + ")", "OK");
    }

    // =====================================================================
    //  รวบรวมบทพูดทั้งหมด
    // =====================================================================
    struct Found { public string id, group, speaker, text, note; }

    public static void Collect(VoiceBank bank)
    {
        var found = new List<Found>();
        void Add(string id, string group, string speaker, string text, string note = "")
        {
            text = VoiceBank.Norm(text);
            if (string.IsNullOrEmpty(text)) return;
            if (found.Any(f => f.speaker == (speaker ?? "") && f.text == text)) return;
            found.Add(new Found { id = id, group = group, speaker = speaker ?? "", text = text, note = note });
        }

        // ---------- ผู้เช่า (DormData) ----------
        foreach (var t in DormData.Tenants)
        {
            string g = "ผู้เช่า: " + t.name + " (ห้อง " + t.room + ")";
            for (int i = 0; i < t.interview.Length; i++) Add(t.id + "_interview_" + (i + 1), g, t.name, t.interview[i], "สัมภาษณ์ครั้งแรก (กลางวัน) ประโยคที่ " + (i + 1));
            Add(t.id + "_chat", g, t.name, t.chat, "คุยครั้งต่อๆ ไป (กลางวัน)");
            Add(t.id + "_thanks", g, t.name, "ขอบคุณนะ... คืนนี้ทางเดินมันแปลกๆ", "หลังเปิดประตูให้ (คนจริง) ตอนกลางคืน");
        }
        foreach (var t in DormData.Tenants)
        {
            string g = "หน้าประตูตอนกลางคืน: " + t.name;
            for (int i = 0; i < t.doorLines.Length; i++)
                Add(t.id + "_door_" + (i + 1), g, t.name, t.doorLines[i], "พูดผ่านประตู — 'เสียงของปลอม' = ตอนสิ่งผิดปกติเลียนเสียง (เพี้ยนนิดๆ)");
        }

        // ---------- ผู้เล่น ----------
        Add("player_staff_1", "ผู้เล่น (นนท์)", "ผู้เล่น", "วันนี้พี่บิ๊กหล่อจังครับ", "ดูรูปพี่บิ๊กครั้งแรกของวัน");
        Add("player_staff_2", "ผู้เล่น (นนท์)", "ผู้เล่น", "ดูสุดหล่อไปแล้ว", "ดูรูปพี่บิ๊กซ้ำ");

        // ---------- ของในฉาก (เปิดทุกซีนใน Build Settings มาอ่าน) ----------
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var paths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
        foreach (var path in paths)
        {
            var sc = SceneManager.GetSceneByPath(path);
            bool opened = false;
            if (!sc.isLoaded)
            {
                try { sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); opened = true; }
                catch { continue; }
            }
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
            foreach (var root in sc.GetRootGameObjects())
                ScanObject(root, sceneName, Add);
            if (opened) EditorSceneManager.CloseScene(sc, true);
        }

        Merge(bank, found);
    }

    static void ScanObject(GameObject root, string scene, System.Action<string, string, string, string, string> add)
    {
        foreach (var tape in root.GetComponentsInChildren<TapePlayer>(true))
            if (tape.lines != null)
                for (int i = 0; i < tape.lines.Length; i++)
                    add("tape_" + (i + 1), "เทปบันทึกเสียง (" + tape.name + ")", tape.lines[i].speaker, tape.lines[i].text,
                        "เล่นจากเครื่องเล่นเทป" + (tape.lines[i].voice ? " — บรรทัดนี้มีเสียงใส่ใน TapePlayer แล้ว" : ""));

        foreach (var nv in root.GetComponentsInChildren<NightVisits>(true))
            foreach (var v in nv.visits)
            {
                var info = DormData.Tenant(v.tenantId);
                string who = info != null ? info.name : "";
                string g = "คืนเดโม: คนมาเคาะ (" + scene + ")";
                add("visit_" + v.label + "_line", g, who, v.line, (v.isFake ? "ของปลอม" : "คนจริง") + " พูดหลังเคาะ");
                add("visit_" + v.label + "_opened", g, "", v.openedRealText, "หลังเปิดประตูให้คนจริง (" + who + ")");
                for (int b = 0; b < v.beats.Count; b++)
                    if (!v.beats[b].voice)
                        add("visit_" + v.label + "_beat" + (b + 1), g, "", v.beats[b].subtitle, "ซับของท่อน " + v.beats[b].label);
            }

        foreach (var se in root.GetComponentsInChildren<ScareEvent>(true))
            add("scare_" + se.name, "เหตุการณ์หลอน (" + scene + ")", "", se.subtitle, "ซับตอนเกิดเหตุการณ์ " + se.name);

        foreach (var ne in root.GetComponentsInChildren<NightEnding>(true))
            add("ending_whisper", "ฉากจบ", "", ne.lastWhisper, "เสียงกระซิบก่อนจอดำ");

        // ข้อความกลางจอ (ความคิด/บรรยาย) จากของในฉาก
        string mg = "ข้อความกลางจอ (" + scene + ")";
        foreach (var t in root.GetComponentsInChildren<NightTrigger>(true))
            add("msg_" + t.name, mg, "", t.message, "ขึ้นตอนทริกเกอร์ " + t.name);
        foreach (var d in root.GetComponentsInChildren<NightDemo>(true))
            foreach (var s in d.steps)
                add("step_" + s.id, mg, "", s.messageOnBegin, "ขึ้นตอนเริ่มขั้น " + s.id);
        foreach (var se in root.GetComponentsInChildren<ScareEvent>(true))
            add("scaremsg_" + se.name, mg, "", se.message, "ข้อความตอนเกิดเหตุการณ์ " + se.name);
        foreach (var a in root.GetComponentsInChildren<ArrivalFocus>(true))
            add("arrival_" + a.name, mg, "", a.message, "ตอนกล้องแพนหาสิ่งของ");

        // NPC ของระบบหลัก (BlurredRoom.NeutralNpc) — อ่านทุกช่องข้อความที่ชื่อมีคำว่า line/dialog
        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (!mb || mb.GetType().Name != "NeutralNpc") continue;
            string who = mb.name;
            var dn = mb.GetType().GetProperty("DisplayName");
            if (dn != null) { try { who = dn.GetValue(mb) as string ?? who; } catch { } }
            ScanStrings(new SerializedObject(mb), "npc_" + mb.name, "NPC: " + who + " (" + scene + ")", who, add, 0);
        }
    }

    static void ScanStrings(SerializedObject so, string idBase, string group, string who, System.Action<string, string, string, string, string> add, int depth)
    {
        var it = so.GetIterator();
        int n = 0;
        while (it.Next(true))
        {
            string path = it.propertyPath.ToLowerInvariant();
            if (it.propertyType == SerializedPropertyType.String && (path.Contains("line") || path.Contains("dialog") || path.Contains("speech")))
                add(idBase + "_" + (++n), group, who, it.stringValue, "");
            else if (depth == 0 && it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue is ScriptableObject data)
                ScanStrings(new SerializedObject(data), idBase + "_" + data.name, group, who, add, 1);
        }
    }

    static void Merge(VoiceBank bank, List<Found> found)
    {
        Undo.RecordObject(bank, "รวบรวมบทพูด");
        var old = new List<VoiceBank.Line>(bank.lines);
        var used = new HashSet<VoiceBank.Line>();
        var result = new List<VoiceBank.Line>();
        int added = 0;
        foreach (var f in found)
        {
            var m = old.FirstOrDefault(l => !used.Contains(l) && !string.IsNullOrEmpty(l.id) && l.id == f.id)
                 ?? old.FirstOrDefault(l => !used.Contains(l) && VoiceBank.Norm(l.speaker) == f.speaker && VoiceBank.Norm(l.text) == f.text)
                 ?? old.FirstOrDefault(l => !used.Contains(l) && (l.id ?? "").StartsWith("auto_") && VoiceBank.Norm(l.text) == f.text);
            if (m == null) { m = new VoiceBank.Line(); added++; }
            used.Add(m);
            m.id = f.id; m.group = f.group; m.speaker = f.speaker; m.text = f.text; m.missing = false;
            if (string.IsNullOrEmpty(m.note)) m.note = f.note;
            result.Add(m);
        }
        // ของเดิมที่หาไม่เจอแล้ว: เก็บไว้ท้ายรายการ (บรรทัดที่เก็บอัตโนมัติตอนเล่น / เพิ่มเอง ไม่นับว่าหาย)
        foreach (var l in old)
        {
            if (used.Contains(l)) continue;
            string lid = l.id ?? ""; bool auto = lid.StartsWith("auto_") || lid.StartsWith("custom_");
            l.missing = !auto;
            result.Add(l);
        }
        bank.lines = result;
        bank.MarkDirtyMap();
        EditorUtility.SetDirty(bank);
        AssetDatabase.SaveAssets();
        Debug.Log("[VoiceBank] รวบรวมบทพูดแล้ว " + found.Count + " บรรทัด (ใหม่ " + added + ", ทั้งหมดในคลัง " + bank.lines.Count + ")");
    }

    // =====================================================================
    //  ลองฟังใน Editor (AudioUtil เป็น internal API)
    // =====================================================================
    static void PlayPreview(AudioClip clip)
    {
        StopPreview();
        var t = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        var m = t?.GetMethod("PlayPreviewClip", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null,
                             new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
        if (m != null) m.Invoke(null, new object[] { clip, 0, false });
    }

    static void StopPreview()
    {
        var t = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        t?.GetMethod("StopAllPreviewClips", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)?.Invoke(null, null);
    }
}
