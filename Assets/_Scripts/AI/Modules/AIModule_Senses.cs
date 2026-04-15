using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AIModule_Senses : AIModule
{
    [SerializeField] float detectionRadius = 10f;
    [SerializeField] float playerTooCloseDistance = 3f;
    [SerializeField] float scanInterval = 0.5f;

    [Header("Events")]
    public UnityEvent<PlayerData> OnPlayerSpotted;
    public UnityEvent<PlayerData> OnPlayerTooClose;

    public float DetectionRadius => detectionRadius;
    public float PlayerTooCloseDistance => playerTooCloseDistance;

    readonly HashSet<PlayerData> seenPlayers = new();
    readonly List<PlayerData> watchedPlayers = new();
    readonly HashSet<PlayerData> tooClosePlayers = new();

    public IReadOnlyCollection<PlayerData> SeenPlayers => seenPlayers;
    public List<PlayerData> WatchedPlayers => watchedPlayers;

    float scanTimer = 0f;

    public override void OnModuleTick(AIBrain brain)
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f) return;
        scanTimer = scanInterval;

        ScanForPlayers(brain);
        ScanForTooClosePlayers(brain);
    }

    public void RegisterSeenPlayer(PlayerData player)
    {
        if (player == null) return;

        bool isNew = seenPlayers.Add(player);

        if (!watchedPlayers.Contains(player))
            watchedPlayers.Add(player);

        if (isNew)
            OnPlayerSpotted?.Invoke(player);
    }

    public void UnwatchPlayer(PlayerData player) => watchedPlayers.Remove(player);
    public void ClearWatched() => watchedPlayers.Clear();
    public bool HasSeenPlayer(PlayerData player) => seenPlayers.Contains(player);

    public void ClearAll()
    {
        seenPlayers.Clear();
        watchedPlayers.Clear();
        tooClosePlayers.Clear();
    }

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

        return closest ?? ScanForClosestPlayer(brain);
    }

    public PlayerData ScanForClosestPlayer(AIBrain brain)
    {
        var players = GameManager.Instance.playMod.Players;
        if (players == null) return null;

        PlayerData closest = null;
        float closestDist = float.MaxValue;

        foreach (var p in players)
        {
            if (!IsValidTarget(p, brain)) continue;
            float d = Vector3.Distance(brain.transform.position, p.transform.position);
            if (d < closestDist) { closestDist = d; closest = p; }
        }

        if (closest != null) RegisterSeenPlayer(closest);
        return closest;
    }

    public PlayerData GetTooClosePlayer(AIBrain brain)
    {
        var players = GameManager.Instance.playMod.Players;
        if (players == null) return null;

        foreach (var p in players)
        {
            if (p == null) continue;
            float d = Vector3.Distance(brain.transform.position, p.transform.position);
            if (d <= playerTooCloseDistance && brain.HasLineOfSight(p.transform.position))
                return p;
        }

        return null;
    }

    void ScanForPlayers(AIBrain brain)
    {
        var players = GameManager.Instance.playMod.Players;
        if (players == null) return;

        foreach (var p in players)
        {
            if (!IsValidTarget(p, brain)) continue;
            RegisterSeenPlayer(p);
        }

        seenPlayers.RemoveWhere(p => p == null);
        watchedPlayers.RemoveAll(p => p == null);
    }

    void ScanForTooClosePlayers(AIBrain brain)
    {
        var players = GameManager.Instance.playMod.Players;
        if (players == null) return;

        foreach (var p in players)
        {
            if (p == null) continue;

            float d = Vector3.Distance(brain.transform.position, p.transform.position);
            bool isTooClose = d <= playerTooCloseDistance && brain.HasLineOfSight(p.transform.position);

            if (isTooClose)
            {
                if (tooClosePlayers.Add(p))
                    OnPlayerTooClose?.Invoke(p);
            }
            else
            {
                tooClosePlayers.Remove(p);
            }
        }

        tooClosePlayers.RemoveWhere(p => p == null);
    }

    bool IsValidTarget(PlayerData p, AIBrain brain)
    {
        if (p == null) return false;
        if (p.Player_Stats.dead || p.Player_Stats.knocked) return false;

        float d = Vector3.Distance(brain.transform.position, p.transform.position);
        if (d > detectionRadius) return false;

        return brain.HasLineOfSight(p.transform.position);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!debug) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, playerTooCloseDistance);
    }
#endif
}
