using Mirror;
using UnityEngine;

public abstract class NetworkDungeonSpawner : DungeonSpawner
{
    // Subclasses can check this directly if needed
    protected bool IsServer => NetworkServer.active;

    public override void Clear()
    {
        if (!IsServer) return;
        base.Clear();
    }

    /// <summary> Returns the spawned GameObject, or null if not on server. </summary>
    protected GameObject NetworkSpawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!IsServer) return null;

        GameObject go = Instantiate(prefab, position, rotation, parent);
        NetworkServer.Spawn(go);
        return go;
    }
}
