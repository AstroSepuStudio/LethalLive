#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoomData))]
public class RoomDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RoomData roomData = (RoomData)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Refresh Room Lights"))
        {
            Undo.RecordObject(roomData, "Refresh Room Lights");
            roomData.roomLights = roomData.GetComponentsInChildren<LED_Light>(includeInactive: true);
            EditorUtility.SetDirty(roomData);
        }

        if (GUILayout.Button("Refresh Room Renderers"))
        {
            Undo.RecordObject(roomData, "Refresh Room Renderers");
            roomData.roomRenderers = roomData.GetComponentsInChildren<Renderer>(includeInactive: true);
            EditorUtility.SetDirty(roomData);
        }
    }
}
#endif