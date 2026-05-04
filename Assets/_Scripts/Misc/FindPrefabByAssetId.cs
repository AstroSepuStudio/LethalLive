#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class FindPrefabByAssetId : EditorWindow
{
    uint targetId;

    [MenuItem("Tools/Find Prefab By AssetId")]
    static void Open() => GetWindow<FindPrefabByAssetId>("Find Prefab By AssetId");

    void OnGUI()
    {
        targetId = (uint)EditorGUILayout.LongField("Asset ID", targetId);

        if (GUILayout.Button("Search"))
        {
            bool found = false;
            string[] guids = AssetDatabase.FindAssets("t:GameObject");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                if (go.TryGetComponent(out Mirror.NetworkIdentity ni) && ni.assetId == targetId)
                {
                    Debug.Log($"Found: {path}", go);
                    Selection.activeObject = go;
                    EditorGUIUtility.PingObject(go);
                    found = true;
                    break;
                }
            }

            if (!found)
                Debug.LogWarning($"No prefab found with assetId {targetId}");
        }
    }
}
#endif