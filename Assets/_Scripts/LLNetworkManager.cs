using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class LLNetworkManager : NetworkManager
{
    public Transform[] spawnPoints;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Transform spawn = spawnPoints.Length > 0
            ? spawnPoints[conn.connectionId % spawnPoints.Length]
            : null;

        Vector3 spawnPos = spawn ? spawn.position : new Vector3(0, 5, 0); // fallback Y=5
        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        NetworkServer.AddPlayerForConnection(conn, player);
    }
}

