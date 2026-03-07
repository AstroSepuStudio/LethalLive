using UnityEngine;
using UnityEngine.Events;

public class AIS_FollowAlpha : AIState
{
    [SerializeField] float followStopDistance = 2f;
    [SerializeField] float recalculateInterval = 0.5f;
    [SerializeField] float separationDistance = 1.5f;
    [SerializeField] float separationPushDistance = 3f;
    [SerializeField] float searchDispatchTimeout = 45f;
    float dispatchTimer;

    public VortexAI AlphaTarget { get; set; }
    public UnityEvent OnFollowLost;

    float recalcTimer;

    public override void OnEnterState(AIBrain brain)
    {
        brain.ResumeAgentMovement();
        recalcTimer = 0f;
        dispatchTimer = searchDispatchTimeout;

        brain.Agent.stoppingDistance = followStopDistance;
    }

    public override void OnUpdateState(AIBrain brain)
    {
        if (AlphaTarget == null)
        {
            OnFollowLost?.Invoke();
            return;
        }

        dispatchTimer -= Time.deltaTime;
        if (dispatchTimer <= 0f)
        {
            VortexAI vortex = brain as VortexAI;
            vortex?.BeginSearch();
            return;
        }

        float dist = Vector3.Distance(brain.transform.position, AlphaTarget.transform.position);

        recalcTimer -= Time.deltaTime;
        if (recalcTimer <= 0f)
        {
            recalcTimer = recalculateInterval;

            if (dist < separationDistance)
            {
                Vector3 pushDir = (brain.transform.position - AlphaTarget.transform.position).normalized;
                brain.MoveAgent(brain.transform.position + pushDir * separationPushDistance);
            }
            else
            {
                brain.MoveAgent(AlphaTarget.transform.position);
            }
        }

        brain.Animator_.SetBool("Walk", brain.IsAgentInMovement());
    }

    public override void OnExitState(AIBrain brain)
    {
        brain.Agent.stoppingDistance = 0f;
        brain.Animator_.SetBool("Walk", false);
    }
}
