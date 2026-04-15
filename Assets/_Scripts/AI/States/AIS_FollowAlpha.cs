using UnityEngine;
using UnityEngine.Events;

public class AIS_FollowAlpha : AIState
{
    [SerializeField] float followStopDistance = 2f;
    [SerializeField] float recalculateInterval = 0.5f;
    [SerializeField] float separationRadius = 2.5f;
    [SerializeField] float separationPushForce = 3f;
    [SerializeField] float searchDispatchTimeout = 45f;

    public AIBrain AlphaTarget { get; set; }

    Vector3 leaderOffset;
    bool offsetAssigned;

    float recalcTimer;
    float dispatchTimer;

    public UnityEvent OnFollowLost;

    public override void OnEnterState(AIBrain brain)
    {
        brain.ResumeAgentMovement();
        recalcTimer = 0f;
        dispatchTimer = searchDispatchTimeout;
        brain.Agent.stoppingDistance = followStopDistance;
        brain.SetIdleState(false);

        if (!offsetAssigned)
        {
            offsetAssigned = true;
            leaderOffset = PickOffset();
        }
    }

    public override void OnUpdateState(AIBrain brain)
    {
        if (AlphaTarget == null) { OnFollowLost?.Invoke(); return; }

        dispatchTimer -= Time.deltaTime;
        if (dispatchTimer <= 0f)
        {
            brain.OnModuleEvent(AIBrain.ModuleEvent.BeginSearch);
            return;
        }

        recalcTimer -= Time.deltaTime;
        if (recalcTimer <= 0f)
        {
            recalcTimer = recalculateInterval;

            Vector3 desiredPos = AlphaTarget.transform.position + leaderOffset;
            float distToSlot = Vector3.Distance(brain.transform.position, desiredPos);
            float distToLeader = Vector3.Distance(brain.transform.position, AlphaTarget.transform.position);

            if (distToLeader < separationRadius * 0.5f)
            {
                Vector3 pushDir = (brain.transform.position - AlphaTarget.transform.position).normalized;
                if (pushDir == Vector3.zero) pushDir = leaderOffset.normalized;
                brain.MoveAgent(brain.transform.position + pushDir * separationPushForce);
            }
            else
            {
                brain.MoveAgent(desiredPos);
            }
        }

        brain.Animator_.SetBool("Walk", brain.IsAgentInMovement());
    }

    public override void OnExitState(AIBrain brain)
    {
        brain.Agent.stoppingDistance = 0f;
        brain.Animator_.SetBool("Walk", false);
        brain.SetIdleState(true);
    }

    Vector3 PickOffset()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = separationRadius;
        return new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
    }
}
