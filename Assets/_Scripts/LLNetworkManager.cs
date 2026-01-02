using Mirror;
using UnityEngine;

public class LLNetworkManager : NetworkManager
{
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Transform[] spawnPoints = GameManager.Instance.spawnPoints;

        Transform spawn = spawnPoints.Length > 0
            ? spawnPoints[conn.connectionId % spawnPoints.Length]
            : null;

        Vector3 spawnPos = spawn ? spawn.position : GameManager.Instance.transform.position;
        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        NetworkServer.AddPlayerForConnection(conn, player);
    }
}

