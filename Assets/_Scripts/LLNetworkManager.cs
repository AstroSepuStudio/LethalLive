using Mirror;
using Steamworks;
using UnityEngine;

public class LLNetworkManager : NetworkManager
{
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Transform[] spawnPoints = GameManager.Instance.playMod.spawnPoints;

        Transform spawn = spawnPoints.Length > 0
            ? spawnPoints[conn.connectionId % spawnPoints.Length]
            : null;

        Vector3 spawnPos = spawn ? spawn.position : GameManager.Instance.transform.position;
        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        NetworkServer.AddPlayerForConnection(conn, player);
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        BuildConsole.Instance.SendConsoleMessage("Lost connection to host.");

        // Leave Steam lobby if still in one
        if (LobbyManager.Instance.CurrentLobbyID.m_SteamID != 0)
        {
            SteamMatchmaking.LeaveLobby(LobbyManager.Instance.CurrentLobbyID);
            LobbyManager.Instance.ClearLobby();
        }

        LobbyManager.Instance.OnLobbyLeaveEvent?.Invoke();
    }
}

