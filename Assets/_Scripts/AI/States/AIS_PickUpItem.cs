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
        brain.SetIdleState(false);
    }

    public override void OnUpdateState(AIBrain brain)
    {
        if (graceTimer > 0f) { graceTimer -= Time.deltaTime; return; }

        validationTimer -= Time.deltaTime;
        if (validationTimer <= 0f)
        {
            validationTimer = validationInterval;
            if (!IsItemAvailable()) { OnItemLost?.Invoke(); return; }
        }

        if (TargetItem != null)
        {
            recalcTimer -= Time.deltaTime;
            if (recalcTimer <= 0f)
            {
                recalcTimer = recalculateInterval;
                brain.MoveAgent(TargetItem.transform.position);
            }
        }

        bool mov = brain.IsAgentInMovement();
        brain.Animator_.SetBool("Walk", mov);
        if (mov) { stuckTimer = stuckTimeout; return; }

        stuckTimer -= Time.deltaTime;
        if (stuckTimer <= 0f) { OnItemLost?.Invoke(); return; }

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

        attemptsRemaining--;
    }

    public override void OnExitState(AIBrain brain) 
    { 
        brain.SetIdleState(true); 
    }

    bool IsItemAvailable() =>
        TargetItem != null &&
        TargetItem.ItemData.pickable &&
        !TargetItem.HasOwner;

    void TryPickUp(AIBrain brain)
    {
        if (!IsItemAvailable()) { OnItemLost?.Invoke(); return; }

        var carrier = brain.GetModule<AIModule_ItemCarrier>();
        if (carrier == null) { OnItemLost?.Invoke(); return; }

        carrier.CarryItem(TargetItem, brain);
        OnItemPickedUp?.Invoke();
    }

    FurnitureEntity FindBlockingFurniture()
    {
        if (TargetItem == null) return null;
        Collider[] hits = Physics.OverlapSphere(TargetItem.transform.position, 1f);
        foreach (var hit in hits)
            if (hit.TryGetComponent<FurnitureEntity>(out var f)) return f;
        return null;
    }

    float GetEffectivePickUpRange(AIBrain brain)
    {
        return pickUpRange * brain.transform.localScale.x;
    }
}
