using System.Collections.Generic;
using UnityEngine;

public class AIModule_Senses : AIModule
{
    [SerializeField] float detectionRadius = 10f;
    [SerializeField] float playerTooCloseDistance = 3f;

    public float DetectionRadius => detectionRadius;
    public float PlayerTooCloseDistance => playerTooCloseDistance;

    readonly HashSet<PlayerData> seenPlayers = new();
    readonly List<PlayerData> watchedPlayers = new();

    public IReadOnlyCollection<PlayerData> SeenPlayers => seenPlayers;
    public List<PlayerData> WatchedPlayers => watchedPlayers;

    public void RegisterSeenPlayer(PlayerData player)
    {
        if (player == null) return;
        seenPlayers.Add(player);
        if (!watchedPlayers.Contains(player))
            watchedPlayers.Add(player);
    }

    public void UnwatchPlayer(PlayerData player)
    {
        watchedPlayers.Remove(player);
    }

    public void ClearWatched() => watchedPlayers.Clear();

    public void ClearAll()
    {
        seenPlayers.Clear();
        watchedPlayers.Clear();
    }

    public bool HasSeenPlayer(PlayerData player) => seenPlayers.Contains(player);

    public PlayerData GetClosestSeenPlayer(AIBrain brain)
    {
        PlayerData closest = null;
        float closestDist = float.MaxValue;

        foreach (var p in seenPlayers)
        {
            if (p == null) continue;
            float d = Vector3.Distance(brain.transform.position, p.transform.position);
            if (d < closestDist) { closestDist = d; closest = p; }
        }

        if (closest == null) return ScanForClosestPlayer(brain);

        return closest;
    }

    public PlayerData ScanForClosestPlayer(AIBrain brain)
    {
        Collider[] hits = Physics.OverlapSphere(brain.transform.position, detectionRadius);
        PlayerData closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            if (!hit.TryGetComponent<PlayerData>(out var p)) continue;
            if (!brain.HasLineOfSight(p.transform.position)) continue;
            float d = Vector3.Distance(brain.transform.position, p.transform.position);
            if (d < closestDist) { closestDist = d; closest = p; }
        }

        if (closest != null) RegisterSeenPlayer(closest);
        return closest;
    }

    public PlayerData GetTooClosePlayer(AIBrain brain)
    {
        Collider[] hits = Physics.OverlapSphere(brain.transform.position, playerTooCloseDistance);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            if (hit.TryGetComponent<PlayerData>(out var p) && brain.HasLineOfSight(p.transform.position))
                return p;
        }
        return null;
    }
}
