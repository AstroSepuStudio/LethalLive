using UnityEngine;
using UnityEngine.Events;

public class AIS_InvestigateSound : AIState
{
    [SerializeField] float arrivalThreshold = 1.5f;

    public Vector3 TargetPosition { get; set; }
    public UnityEvent ArrivedAtLocation;

    bool arrived;

    public override void OnEnterState(AIBrain brain)
    {
        arrived = false;
        brain.MoveAgent(TargetPosition);
        brain.Animator_.SetBool("Walk", true);
        brain.SetIdleState(false);
    }

    public override void OnExitState(AIBrain brain)
    {
        brain.Animator_.SetBool("Walk", false);
        brain.SetIdleState(true);
    }

    public override void OnUpdateState(AIBrain brain)
    {
        if (!arrived)
        {
            bool moving = brain.IsAgentInMovement();
            brain.Animator_.SetBool("Walk", moving);

            float distToTarget = Vector3.Distance(brain.transform.position, TargetPosition);
            if (!moving || distToTarget <= arrivalThreshold)
            {
                arrived = true;
                brain.StopAgentMovement();
                brain.Animator_.SetBool("Walk", false);
            }
            return;
        }

        ArrivedAtLocation?.Invoke();
    }
}
