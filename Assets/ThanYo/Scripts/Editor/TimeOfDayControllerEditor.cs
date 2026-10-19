using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// หน้าตา Inspector ของ TimeOfDayController: ปุ่มใหญ่ 3 ปุ่มอยู่บนสุด
[CustomEditor(typeof(TimeOfDayController))]
public class TimeOfDayControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var ctrl = (TimeOfDayController)target;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Time of Day", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        DrawButton(ctrl, TimeOfDay.Morning, "Day");
        DrawButton(ctrl, TimeOfDay.Evening, "Evening");
        DrawButton(ctrl, TimeOfDay.Night, "Night");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        DrawDefaultInspector();

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Re-apply current preset"))
            ApplyAndDirty(ctrl, ctrl.timeOfDay);
    }

    void DrawButton(TimeOfDayController ctrl, TimeOfDay t, string label)
    {
        var old = GUI.backgroundColor;
        if (ctrl.timeOfDay == t) GUI.backgroundColor = new Color(0.55f, 0.8f, 1f);
        if (GUILayout.Button(label, GUILayout.Height(32)))
            ApplyAndDirty(ctrl, t);
        GUI.backgroundColor = old;
    }

    void ApplyAndDirty(TimeOfDayController ctrl, TimeOfDay t)
    {
        Undo.RecordObject(ctrl, "Change Time of Day");
        if (ctrl.sunLight != null) Undo.RecordObject(ctrl.sunLight.transform, "Change Time of Day");
        if (ctrl.sunLight != null) Undo.RecordObject(ctrl.sunLight, "Change Time of Day");
        ctrl.SetTimeOfDay(t);
        EditorUtility.SetDirty(ctrl);
        EditorSceneManager.MarkSceneDirty(ctrl.gameObject.scene);
    }
}
