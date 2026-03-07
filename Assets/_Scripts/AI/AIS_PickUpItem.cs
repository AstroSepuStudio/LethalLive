using UnityEngine;
using UnityEngine.Events;

public class AIS_PickUpItem : AIState
{
    [SerializeField] float pickUpRange = 1.2f;
    [SerializeField] float validationInterval = 0.5f;

    public ItemBase TargetItem { get; set; }
    public UnityEvent OnItemPickedUp;
    public UnityEvent OnItemLost;

    float validationTimer;

    public override void OnEnterState(AIBrain brain)
    {
        if (TargetItem == null) { OnItemLost?.Invoke(); return; }

        brain.ResumeAgentMovement();
        brain.MoveAgent(TargetItem.transform.position);
        validationTimer = validationInterval;
    }

    public override void OnUpdateState(AIBrain brain)
    {
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
            brain.MoveAgent(TargetItem.transform.position);

        if (!brain.IsAgentInMovement())
        {
            float dist = TargetItem != null
                ? Vector3.Distance(brain.transform.position, TargetItem.transform.position)
                : float.MaxValue;

            if (dist <= pickUpRange)
                TryPickUp(brain);
            else
                OnItemLost?.Invoke();
        }
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
