using System.Collections.Generic;
using UnityEngine;
using Mirror;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class VortexAI : AIBrain
{
    [Header("State Indexes")]
    [SerializeField] int stareStateIndex = 2;
    [SerializeField] int followStateIndex = 3;
    [SerializeField] int pickUpStateIndex = 4;
    [SerializeField] int[] wanderIndexes;

    [Header("Wander Cycling")]
    [SerializeField] int minWanderCycles = 1;
    [SerializeField] int maxWanderCycles = 4;
    int targetWanCycles = 0;
    int curWanCycles = 0;
    int curWanIndex = 0;

    [Header("Alpha")]
    [SerializeField][SyncVar] float alpha = 0f;
    public float Alpha => alpha;

    [Header("Detection")]
    [SerializeField] float detectionRadius = 8f;
    [SerializeField] float detectionInterval = 1f;
    float detectionTimer = 0f;
    readonly HashSet<VortexAI> seenVortexes = new();

    [Header("Item Detection")]
    [SerializeField] float itemDetectionRadius = 5f;
    [SerializeField] float itemDetectionInterval = 1.5f;
    float itemDetectionTimer = 0f;

    public ItemBase CarriedItem { get; private set; }

    AIS_StareAtVortex stareState;
    AIS_FollowAlpha followState;
    AIS_PickUpItem pickUpState;

    #region Lifecycle

    protected override void Awake()
    {
        base.Awake();
        alpha = Random.Range(0, 100);
    }

    protected override void Start()
    {
        base.Start();
        targetWanCycles = Random.Range(minWanderCycles, maxWanderCycles);

        if (states != null)
        {
            if (stareStateIndex < states.Length) stareState = states[stareStateIndex] as AIS_StareAtVortex;
            if (followStateIndex < states.Length) followState = states[followStateIndex] as AIS_FollowAlpha;
            if (pickUpStateIndex < states.Length) pickUpState = states[pickUpStateIndex] as AIS_PickUpItem;
        }
    }

    protected override void Update()
    {
        base.Update();
        TickDetection();
        TickItemDetection();
    }

    #endregion

    #region Detection

    void TickDetection()
    {
        if (!IsInWanderState()) return;

        detectionTimer -= Time.deltaTime;
        if (detectionTimer > 0f) return;
        detectionTimer = detectionInterval;

        VortexAI encountered = FindClosestOtherVortex();
        if (encountered != null && !seenVortexes.Contains(encountered))
            TriggerStare(encountered);
    }

    void TickItemDetection()
    {
        if (CarriedItem != null) return;

        itemDetectionTimer -= Time.deltaTime;
        if (itemDetectionTimer > 0f) return;
        itemDetectionTimer = itemDetectionInterval;

        ItemBase item = FindClosestAvailableItem();
        if (item != null)
            TriggerPickUp(item);
    }

    ItemBase FindClosestAvailableItem()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, itemDetectionRadius);
        ItemBase closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            ItemBase item = hit.GetComponent<ItemBase>();

            if (item == null) continue; // not an item
            if (!item.ItemData.pickable) continue; // not pickable
            if (item.InUse) continue; // already held by a player

            float d = Vector3.Distance(transform.position, item.transform.position);
            if (d < closestDist) { closestDist = d; closest = item; }
        }

        return closest;
    }

    #endregion

    #region State Helpers

    bool IsInWanderState()
    {
        if (CurrentState == null || wanderIndexes == null) return false;
        foreach (int idx in wanderIndexes)
            if (idx < states.Length && CurrentState == states[idx]) return true;
        return false;
    }

    void ResumeWander() => SetState(states[wanderIndexes[curWanIndex]]);

    VortexAI FindClosestOtherVortex()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        VortexAI closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            VortexAI other = hit.GetComponent<VortexAI>();
            if (other == null || other == this) continue;

            float d = Vector3.Distance(transform.position, other.transform.position);
            if (d < closestDist) { closestDist = d; closest = other; }
        }

        return closest;
    }

    void TriggerStare(VortexAI target)
    {
        if (stareState == null) return;
        seenVortexes.Add(target);
        stareState.TargetVortex = target;
        SetState(states[stareStateIndex]);
    }

    void TriggerPickUp(ItemBase item)
    {
        if (pickUpState == null) return;
        pickUpState.TargetItem = item;
        SetState(states[pickUpStateIndex]);
    }

    public void CarryItem(ItemBase item)
    {
        Debug.Log($"{Prefix} Has picked up an item!", gameObject);
        CarriedItem = item;
        item.OnPickUp();
        item.transform.SetParent(transform);
        item.transform.localPosition = Vector3.up * 1.5f;
    }

    #endregion

    #region Events

    public void OnItemPickedUp() => ResumeWander();
    public void OnItemLost() => ResumeWander();

    public void OnStareDecisionMade(bool shouldFollow)
    {
        if (shouldFollow && followState != null && stareState?.TargetVortex != null)
        {
            followState.AlphaTarget = stareState.TargetVortex;
            SetState(states[followStateIndex]);
        }
        else
        {
            ResumeWander();
        }
    }

    public void OnWanderCompleted()
    {
        curWanCycles++;
        if (curWanCycles < targetWanCycles) return;

        curWanCycles = 0;
        targetWanCycles = Random.Range(minWanderCycles, maxWanderCycles);
        curWanIndex = (curWanIndex + 1) % wanderIndexes.Length;

        SetState(states[wanderIndexes[curWanIndex]]);
    }

    public void OnFollowLost()
    {
        seenVortexes.Clear();
        ResumeWander();
    }

    #endregion

    #region Gizmos
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(transform.position, Vector3.up, detectionRadius);

        Handles.color = Color.green;
        Handles.DrawWireDisc(transform.position, Vector3.up, itemDetectionRadius);

        GUIStyle style = new();
        style.alignment = TextAnchor.MiddleCenter;

        Vector3 labelPos = transform.position + Vector3.up * 2.5f;

        style.normal.textColor = Color.cyan;
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        Handles.Label(labelPos, $"a {alpha:F0}", style);

        if (CurrentState != null)
        {
            style.normal.textColor = Color.white;
            style.fontSize = 11;
            style.fontStyle = FontStyle.Normal;
            Handles.Label(labelPos + Vector3.up * 0.4f, CurrentState.GetType().Name, style);
        }

        if (CarriedItem != null)
        {
            style.normal.textColor = Color.green;
            style.fontSize = 11;
            Handles.Label(labelPos + Vector3.up * 0.8f, $"[{CarriedItem.ItemData.itemName}]", style);
        }
    }
#endif
    #endregion
}