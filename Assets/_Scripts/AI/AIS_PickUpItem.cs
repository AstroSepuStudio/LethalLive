using UnityEngine;
using UnityEngine.Events;

public class AIS_PickUpItem : AIState
{
    [SerializeField] float recalculateInterval = 1f;
    [SerializeField] float pickUpRange = 1.2f;
    [SerializeField] float validationInterval = 0.5f;
    [SerializeField] float movementGracePeriod = 0.2f;
    [SerializeField] float stuckTimeout = 3f;

    public ItemBase TargetItem { get; set; }
    public UnityEvent OnItemPickedUp;
    public UnityEvent OnItemLost;

    float stuckTimer;
    float graceTimer;
    float recalcTimer;
    float validationTimer;

    public override void OnEnterState(AIBrain brain)
    {
        if (TargetItem == null) { OnItemLost?.Invoke(); return; }

        brain.ResumeAgentMovement();
        brain.MoveAgent(TargetItem.transform.position);
        validationTimer = validationInterval;
        graceTimer = movementGracePeriod;
        stuckTimer = stuckTimeout;
    }

    public override void OnUpdateState(AIBrain brain)
    {
        if (graceTimer > 0f) { graceTimer -= Time.deltaTime; return; }

        validationTimer -= Time.deltaTime;
        if (validationTimer <= 0f)
        {
            validationTimer = validationInterval;
            if (!IsItemAvailable())
            {
                OnItemLost?.Invoke();
                return;
            }
        }

        if (TargetItem != null)
        {
            if (recalcTimer <= 0f)
            {
                recalcTimer = recalculateInterval;
                if (TargetItem != null)
                    brain.MoveAgent(TargetItem.transform.position);
            }
            recalcTimer -= Time.deltaTime;
        }

        if (brain.IsAgentInMovement())
        {
            stuckTimer = stuckTimeout;
            return;
        }

        stuckTimer -= Time.deltaTime;
        if (stuckTimer <= 0f)
        {
            OnItemLost?.Invoke();
            return;
        }

        float dist = TargetItem != null
                ? Vector3.Distance(brain.transform.position, TargetItem.transform.position)
                : float.MaxValue;

        if (dist <= pickUpRange)
            TryPickUp(brain);
    }

    public override void OnExitState(AIBrain brain) { }

    bool IsItemAvailable() =>
        TargetItem != null &&
        TargetItem.ItemData.pickable &&
        !TargetItem.InUse;

    void TryPickUp(AIBrain brain)
    {
        if (!IsItemAvailable())
        {
            OnItemLost?.Invoke();
            return;
        }

        VortexAI vortex = brain as VortexAI;
        if (vortex == null) return;

        vortex.CarryItem(TargetItem);
        OnItemPickedUp?.Invoke();
    }
}
