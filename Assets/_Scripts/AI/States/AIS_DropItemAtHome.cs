using UnityEngine;
using UnityEngine.Events;

public class AIS_DropItemAtHome : AIState
{
    [SerializeField] float recalculateInterval = 0.5f;
    [SerializeField] float dropRange = 1.5f;
    [SerializeField] float movementGracePeriod = 1f;
    [SerializeField] float stuckTimeout = 3f;

    float stuckTimer;
    float graceTimer;
    float recalcTimer;
    bool destinationSet;
    Vector3 targetPosition;

    public UnityEvent OnItemDropped;
    public UnityEvent OnNoItemToDeliver;
    public UnityEvent OnArrivedAtHome;

    public override void OnEnterState(AIBrain brain)
    {
        var carrier = brain.GetModule<AIModule_ItemCarrier>();
        var home = brain.GetModule<AIModule_Home>();

        if (carrier == null || !carrier.HasItem)
        {
            destinationSet = false;
            return;
        }

        if (home == null || home.GetEffectiveHome() == null)
        {
            destinationSet = false;
            return;
        }

        brain.Animator_.SetBool("Walk", true);
        destinationSet = true;
        stuckTimer = stuckTimeout;
        graceTimer = movementGracePeriod;
        recalcTimer = recalculateInterval;
        targetPosition = home.GetEffectiveHome().transform.position;
        brain.MoveAgent(targetPosition);
        brain.SetIdleState(false);
    }

    public override void OnUpdateState(AIBrain brain)
    {
        if (!destinationSet)
        {
            OnNoItemToDeliver?.Invoke();
            return;
        }

        if (graceTimer > 0f) { graceTimer -= Time.deltaTime; return; }

        recalcTimer -= Time.deltaTime;
        if (recalcTimer <= 0f)
        {
            recalcTimer = recalculateInterval;
            brain.MoveAgent(targetPosition);
        }

        bool mov = brain.IsAgentInMovement();
        brain.Animator_.SetBool("Walk", mov);

        if (mov) { stuckTimer = stuckTimeout; return; }

        stuckTimer -= Time.deltaTime;
        if (stuckTimer <= 0f) { OnItemDropped?.Invoke(); return; }

        float dist = Vector3.Distance(brain.transform.position, targetPosition);
        if (dist <= dropRange)
        {
            brain.GetModule<AIModule_ItemCarrier>()?.DropCarriedItem();
            OnArrivedAtHome?.Invoke();
            OnItemDropped?.Invoke();
            graceTimer = 0f;
        }
    }

    public override void OnExitState(AIBrain brain)
    {
        brain.Animator_.SetBool("Walk", false);
        destinationSet = false;
        brain.SetIdleState(true);
    }
}
