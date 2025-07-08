using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] List<PlayerData> players = new ();
    public IReadOnlyList<PlayerData> Players => players;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [Server]
    public void RegisterPlayer(PlayerData player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log($"Player registered: {player.netIdentity.connectionToClient.connectionId}");
        }
    }

    [Server]
    public void UnregisterPlayer(PlayerData player)
    {
        if (players.Contains(player))
        {
            players.Remove(player);
            Debug.Log($"Player unregistered: {player.netIdentity.connectionToClient.connectionId}");
        }
    }

}
