using UnityEngine;
using UnityEngine.Events;

public class AIS_PickUpItem : AIState
{
    [SerializeField] float recalculateInterval = 1f;
    [SerializeField] float pickUpRange = 1.2f;
    [SerializeField] float validationInterval = 0.5f;
    [SerializeField] float movementGracePeriod = 0.2f;
    [SerializeField] float stuckTimeout = 3f;
    [SerializeField] int pickupAttempts = 5;

    public ItemBase TargetItem { get; set; }
    public FurnitureEntity BlockingFurniture => blockingFurniture;

    public UnityEvent OnItemPickedUp;
    public UnityEvent OnItemLost;
    public UnityEvent OnFurnitureBlocking;

    int attemptsRemaining;
    float stuckTimer;
    float graceTimer;
    float recalcTimer;
    float validationTimer;
    FurnitureEntity blockingFurniture;

    public override void OnEnterState(AIBrain brain)
    {
        if (TargetItem == null) { OnItemLost?.Invoke(); return; }

        brain.ResumeAgentMovement();
        brain.MoveAgent(TargetItem.transform.position);
        validationTimer = validationInterval;
        graceTimer = movementGracePeriod;
        stuckTimer = stuckTimeout;
        attemptsRemaining = pickupAttempts;
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

        if (dist <= GetEffectivePickUpRange(brain))
        {
            TryPickUp(brain);
        }
        else if (attemptsRemaining <= 0)
        {
            blockingFurniture = FindBlockingFurniture();
            if (blockingFurniture != null)
                OnFurnitureBlocking?.Invoke();
            else
                OnItemLost?.Invoke();
        }

        attemptsRemaining --;
    }

    public override void OnExitState(AIBrain brain) { }

    bool IsItemAvailable() =>
        TargetItem != null &&
        TargetItem.ItemData.pickable &&
        !TargetItem.HasOwner;

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

    FurnitureEntity FindBlockingFurniture()
    {
        if (TargetItem == null) return null;

        Collider[] hits = Physics.OverlapSphere(
            TargetItem.transform.position, 1f);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<FurnitureEntity>(out var f)) 
                return f;
        }
        return null;
    }

    float GetEffectivePickUpRange(AIBrain brain)
    {
        VortexAI vortex = brain as VortexAI;
        if (vortex == null) return pickUpRange;
        return pickUpRange * vortex.transform.localScale.x;
    }
}
