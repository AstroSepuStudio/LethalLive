using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;

public class AIS_FollowPlayer : AIState
{
    [SerializeField] float followStopDistance = 3f;
    [SerializeField] float recalculateInterval = 0.5f;
    [SerializeField] float separationDistance = 2f;
    [SerializeField] float separationPushDistance = 3f;
    [SerializeField] float minCuriosityDuration = 10f;
    [SerializeField] float maxCuriosityDuration = 60f;

    public List<PlayerData> WatchedPlayers { get; set; }

    float CuriosityDuration;
    float curiosityTimer;
    float recalcTimer;

    public UnityEvent OnCuriosityExpired;

    public override void OnEnterState(AIBrain brain)
    {
        CuriosityDuration = Random.Range(minCuriosityDuration, maxCuriosityDuration);
        curiosityTimer = CuriosityDuration;
        recalcTimer = 0f;
        brain.ResumeAgentMovement();
        brain.Agent.stoppingDistance = followStopDistance;
        Debug.Log($"{brain.Prefix} seems to be intereset in a player!");
    }

    public override void OnUpdateState(AIBrain brain)
    {
        VortexAI vortex = brain as VortexAI;
        if (vortex != null && vortex.CarriedItem != null)
        {
            OnCuriosityExpired?.Invoke();
            return;
        }

        if (WatchedPlayers == null || WatchedPlayers.Count == 0)
        {
            OnCuriosityExpired?.Invoke();
            return;
        }

        curiosityTimer -= Time.deltaTime;
        if (curiosityTimer <= 0)
        {
            OnCuriosityExpired?.Invoke();
            return;
        }

        PlayerData target = GetClosestPlayer(brain.transform.position);
        if (target == null) { OnCuriosityExpired?.Invoke(); return; }

        recalcTimer -= Time.deltaTime;
        if (recalcTimer <= 0f)
        {
            recalcTimer = recalculateInterval;
            float dist = Vector3.Distance(brain.transform.position, target.transform.position);

            if (dist < separationDistance)
            {
                Vector3 pushDir = (brain.transform.position - target.transform.position).normalized;
                brain.MoveAgent(brain.transform.position + pushDir * separationPushDistance);
            }
            else
            {
                brain.MoveAgent(target.transform.position);
            }
        }

        brain.Animator_.SetBool("Walk", brain.IsAgentInMovement());
    }

    public override void OnExitState(AIBrain brain)
    {
        brain.Agent.stoppingDistance = 0f;
        brain.Animator_.SetBool("Walk", false);
    }

    PlayerData GetClosestPlayer(Vector3 origin)
    {
        PlayerData closest = null;
        float closestDist = float.MaxValue;
        foreach (var p in WatchedPlayers)
        {
            if (p == null) continue;
            float d = Vector3.Distance(origin, p.transform.position);
            if (d < closestDist) { closestDist = d; closest = p; }
        }
        return closest;
    }
}
