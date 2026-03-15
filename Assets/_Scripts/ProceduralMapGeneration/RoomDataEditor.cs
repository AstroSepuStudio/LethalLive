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

        if (GUILayout.Button("Refresh Spawn Positions"))
        {
            Undo.RecordObject(roomData, "Refresh Spawn Positions");
            roomData.ItemSpawnPositions = new System.Collections.Generic.List<ItemSpawnPosition>(
                roomData.GetComponentsInChildren<ItemSpawnPosition>(includeInactive: true));
            roomData.FurnitureSpawnPositions = new System.Collections.Generic.List<FurnitureSpawnPosition>(
                roomData.GetComponentsInChildren<FurnitureSpawnPosition>(includeInactive: true));
            EditorUtility.SetDirty(roomData);

            Debug.Log($"[RoomData] '{roomData.name}' — found {roomData.ItemSpawnPositions.Count} item + " +
                      $"{roomData.FurnitureSpawnPositions.Count} furniture spawn positions.");
        }

        if (GUILayout.Button("Sync Ports from RoomDataSO"))
        {
            SyncPorts(roomData);
        }
    }

    private void SyncPorts(RoomData roomData)
    {
        SerializedObject so = new SerializedObject(roomData);
        SerializedProperty portsProp = so.FindProperty("ports");

        RoomDataSO.RoomPort[] sourcePorts = roomData.Data.Ports;

        so.Update();
        portsProp.arraySize = sourcePorts.Length;

        for (int i = 0; i < sourcePorts.Length; i++)
        {
            SerializedProperty element = portsProp.GetArrayElementAtIndex(i);
            SerializedProperty localCell = element.FindPropertyRelative("localCell");
            SerializedProperty face = element.FindPropertyRelative("face");

            localCell.vector3IntValue = sourcePorts[i].localCell;
            face.enumValueIndex = (int)sourcePorts[i].face;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(roomData);

        Debug.Log($"[RoomData] '{roomData.name}' — synced {sourcePorts.Length} port(s) from '{roomData.Data.name}'.");
    }

}
#endif
