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

    public override void OnEnterState(AIBrain brain)
    {
        VortexAI vortex = brain as VortexAI;
        if (vortex == null) return;

        if (vortex.CarriedItem == null)
        {
            destinationSet = false;
            return;
        }

        RoomData home = vortex.GetEffectiveHome();
        if (home == null)
        {
            destinationSet = false;
            return;
        }

        brain.Animator_.SetBool("Walk", true);

        destinationSet = true;
        stuckTimer = stuckTimeout;
        graceTimer = movementGracePeriod;
        recalcTimer = recalculateInterval;
        targetPosition = home.transform.position;

        brain.MoveAgent(targetPosition);
    }

    public override void OnUpdateState(AIBrain brain)
    {
        if (!destinationSet)
        {
            OnNoItemToDeliver?.Invoke();
            return;
        }

        if (graceTimer > 0f)
        {
            graceTimer -= Time.deltaTime;
            return;
        }

        recalcTimer -= Time.deltaTime;
        if (recalcTimer <= 0f)
        {
            recalcTimer = recalculateInterval;
            brain.MoveAgent(targetPosition);
        }

        if (brain.IsAgentInMovement())
        {
            stuckTimer = stuckTimeout;
            return;
        }

        stuckTimer -= Time.deltaTime;
        if (stuckTimer <= 0f)
        {
            OnItemDropped?.Invoke();
            return;
        }

        float dist = Vector3.Distance(brain.transform.position, targetPosition);
        if (dist <= dropRange)
            DropItem(brain as VortexAI);
    }

    public override void OnExitState(AIBrain brain)
    {
        brain.Animator_.SetBool("Walk", false);
        destinationSet = false;
    }

    void DropItem(VortexAI vortex)
    {
        if (vortex == null) return;
        vortex.DropCarriedItem();
        OnItemDropped?.Invoke();
        graceTimer = 0f;
    }
}
