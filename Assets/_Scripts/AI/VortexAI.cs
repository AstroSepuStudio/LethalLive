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
    [SerializeField] int dropAtHomeStateIndex = 5;
    [SerializeField] int wanderHomeStateIndex = 6;
    [SerializeField] int searchStateIndex = 7;
    [SerializeField] int[] wanderIndexes;

    [Header("Wander Cycling")]
    [SerializeField] int minWanderCycles = 1;
    [SerializeField] int maxWanderCycles = 4;
    [SerializeField] int cyclesBeforeHomeVisit = 6;
    int targetWanCycles = 0;
    int curWanCycles = 0;
    int curWanIndex = 0;
    int totalWanCycles = 0;

    [Header("Alpha")]
    [SerializeField][SyncVar(hook = nameof(UpdateScale))] int alpha = -1;
    public float Alpha => alpha;
    public VortexAI CurrentAlpha => followState.AlphaTarget;

    bool pendingFollowerDispatch = false;
    List<VortexAI> pendingFollowers = new();

    [Header("Vortex Detection")]
    [SerializeField] float vortexDetectionRadius = 8f;
    [SerializeField] float vortexDetectionInterval = 1f;
    float vortexDetectionTimer = 0f;
    readonly HashSet<VortexAI> seenVortexes = new();

    [Header("Item")]
    [SerializeField] float itemDetectionRadius = 5f;
    [SerializeField] float itemDetectionInterval = 1.5f;
    [SerializeField] float dropCooldownDuration = 3f;
    float itemDetectionTimer = 0f;
    float postDropCooldown = 0f;

    [Header("Home")]
    [SerializeField] float homeDistanceFraction = 0.5f;
    [SerializeField] float homeDistanceTolerance = 0.2f;

    public RoomData HomeRoom { get; private set; }

    RoomData alphaHomeOverride = null;
    bool hasAlphaHomeOverride = false;

    public ItemBase CarriedItem { get; private set; }

    bool isActingAsAlpha = false;
    public bool IsActingAsAlpha => isActingAsAlpha;

    AIS_StareAtVortex stareState;
    AIS_FollowAlpha followState;
    AIS_PickUpItem pickUpState;
    AIS_DropItemAtHome dropAtHomeState;
    AIS_SearchForItems searchState;

    #region Lifecycle

    public override void OnStartServer()
    {
        base.OnStartServer();
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
            if (dropAtHomeStateIndex < states.Length) dropAtHomeState = states[dropAtHomeStateIndex] as AIS_DropItemAtHome;
            if (searchStateIndex < states.Length) searchState = states[searchStateIndex] as AIS_SearchForItems;
        }

        if (isServer)
            AssignHomeRoom();
    }

    void UpdateScale(int oldValue, int newValue)
    {
        float scale = Mathf.Lerp(0.6f, 1.2f, (float)newValue / 100f);
        transform.localScale = new Vector3(scale, scale, scale);
    }

    protected override void Update()
    {
        if (!isServer) return;

        base.Update();
        TickDetection();
        TickItemDetection();
    }

    #endregion

    #region Home Assignment

    void AssignHomeRoom()
    {
        var gen = DungeonGenerator.Instance;
        if (gen == null || gen.SpawnedRooms == null) return;

        float maxDist = gen.MaxDistance;
        float prefDist = maxDist * homeDistanceFraction;
        float minBand = prefDist - maxDist * homeDistanceTolerance;
        float maxBand = prefDist + maxDist * homeDistanceTolerance;

        Vector3 startPos = gen.StartRoomPos;

        List<RoomData> candidates = new();
        List<RoomData> fallback = new();

        foreach (var kvp in gen.SpawnedRooms)
        {
            RoomData rd = kvp.Value;
            if (rd == null) continue;

            float dist = Vector3.Distance(startPos, rd.transform.position);
            if (dist >= minBand && dist <= maxBand)
                candidates.Add(rd);
            else
                fallback.Add(rd);
        }

        HomeRoom = candidates.Count > 0
            ? candidates[Random.Range(0, candidates.Count)]
            : fallback.Count > 0 ? fallback[Random.Range(0, fallback.Count)] : null;
    }

    public RoomData GetEffectiveHome() => hasAlphaHomeOverride ? alphaHomeOverride : HomeRoom;

    void SetAlphaHomeOverride(RoomData alphaHome)
    {
        alphaHomeOverride = alphaHome;
        hasAlphaHomeOverride = alphaHome != null;
    }

    void ClearAlphaHomeOverride()
    {
        alphaHomeOverride = null;
        hasAlphaHomeOverride = false;
    }

    #endregion

    #region Detection

    void TickDetection()
    {
        if (!IsInWanderState()) return;

        vortexDetectionTimer -= Time.deltaTime;
        if (vortexDetectionTimer > 0f) return;
        vortexDetectionTimer = vortexDetectionInterval;

        VortexAI encountered = FindClosestOtherVortex();
        if (encountered != null && !seenVortexes.Contains(encountered))
            TriggerStare(encountered);
    }

    void TickItemDetection()
    {
        if (CarriedItem != null || isActingAsAlpha) return;

        if (postDropCooldown > 0f)
        {
            postDropCooldown -= Time.deltaTime;
            return;
        }

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
            if (item == null) continue;
            if (!item.ItemData.pickable) continue;
            if (item.HasOwner) continue;
            if (IsItemAtEffectiveHome(item)) continue;

            float d = Vector3.Distance(transform.position, item.transform.position);
            if (d < closestDist && HasLineOfSight(item.transform.position))
            { closestDist = d; closest = item; }
        }

        return closest;
    }

    bool IsItemAtEffectiveHome(ItemBase item)
    {
        RoomData home = GetEffectiveHome();
        if (home == null) return false;

        float threshold = DungeonGenerator.Instance.CellSize;
        return Vector3.Distance(item.transform.position, home.transform.position) <= threshold;
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
        Collider[] hits = Physics.OverlapSphere(transform.position, vortexDetectionRadius);
        VortexAI closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            VortexAI other = hit.GetComponent<VortexAI>();
            if (other == null || other == this) continue;

            float d = Vector3.Distance(transform.position, other.transform.position);
            if (d < closestDist && HasLineOfSight(other.transform.position))
            { closestDist = d; closest = other; }
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
        Debug.Log($"{Prefix} Detected an item", gameObject);
    }

    public void TriggerDropAtHome() => SetState(states[dropAtHomeStateIndex]);
    void TriggerWanderHome() => SetState(states[wanderHomeStateIndex]);
    void TriggerSearch()
    {
        if (searchState == null) { TriggerDropAtHome(); return; }
        SetState(states[searchStateIndex]);
        Debug.Log($"{Prefix} Started new search", gameObject);
    }

    #endregion

    #region Item Carrying

    public void CarryItem(ItemBase item)
    {
        Debug.Log($"{Prefix} Has picked up an item!", gameObject);
        CarriedItem = item;
        item.OnPickUp();
        item.transform.SetParent(transform);
        item.transform.localPosition = Vector3.up * 1.5f;
    }

    public void DropCarriedItem()
    {
        Debug.Log($"{Prefix} Tries to drop carried item", gameObject);
        if (CarriedItem == null) return;
        Debug.Log($"{Prefix} Successfully dropped carried item", gameObject);
        CarriedItem.OnDrop(null);
        CarriedItem.transform.SetParent(null);
        CarriedItem = null;
        postDropCooldown = dropCooldownDuration;
    }

    #endregion

    #region Alpha / Follower Role

    void BecomeAlpha(VortexAI follower)
    {
        isActingAsAlpha = true;
        follower.SetAlphaHomeOverride(HomeRoom);
        pendingFollowers.Add(follower);
        pendingFollowerDispatch = true;

        if (CurrentState == states[wanderHomeStateIndex])
        {
            OnArrivedAtHome();
            return;
        }

        TriggerWanderHome();
    }

    public void BeginSearch() => TriggerSearch();

    #endregion

    #region Events

    public void OnItemPickedUp() => TriggerDropAtHome();
    public void OnItemLost() => ResumeWander();

    public void OnItemDropped()
    {
        Debug.Log($"{Prefix} On Item Dropped", gameObject);
        //DropCarriedItem();
        if (hasAlphaHomeOverride) TriggerSearch();
        else ResumeWander();
    }

    public void OnNoItemToDeliver()
    {
        if (hasAlphaHomeOverride) TriggerSearch();
        else ResumeWander();
    }

    public void OnSearchFailed() => TriggerDropAtHome();
    public void OnSearchItemFound() => TriggerDropAtHome();

    public void OnStareDecisionMade(bool shouldFollow)
    {
        if (shouldFollow && followState != null && stareState?.TargetVortex != null)
        {
            VortexAI target = stareState.TargetVortex;
            VortexAI actualAlpha = target.hasAlphaHomeOverride && target.followState?.AlphaTarget != null
            ? target.followState.AlphaTarget
            : target;

            followState.AlphaTarget = actualAlpha;
            actualAlpha.BecomeAlpha(this);
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
        totalWanCycles++;

        if (curWanCycles < targetWanCycles) return;

        curWanCycles = 0;
        targetWanCycles = Random.Range(minWanderCycles, maxWanderCycles);
        curWanIndex = (curWanIndex + 1) % wanderIndexes.Length;

        if (totalWanCycles >= cyclesBeforeHomeVisit)
        {
            if (CarriedItem != null)
            {
                TriggerDropAtHome();
                return;
            }

            if (!isActingAsAlpha && !hasAlphaHomeOverride)
            {
                totalWanCycles = 0;
                TriggerWanderHome();
                return;
            }
        }

        SetState(states[wanderIndexes[curWanIndex]]);
    }

    public void OnFollowLost()
    {
        ClearAlphaHomeOverride();
        seenVortexes.Clear();
        isActingAsAlpha = false;
        ResumeWander();
    }

    public void OnArrivedAtHome()
    {
        if (!pendingFollowerDispatch) return;
        pendingFollowerDispatch = false;

        foreach (var follower in pendingFollowers)
            follower.BeginSearch();
        pendingFollowers.Clear();
    }


    #endregion

    #region Gizmos
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(transform.position, Vector3.up, vortexDetectionRadius);

        Handles.color = Color.green;
        Handles.DrawWireDisc(transform.position, Vector3.up, itemDetectionRadius);

        RoomData effectiveHome = GetEffectiveHome();
        if (effectiveHome != null)
        {
            Vector3 homePos = effectiveHome.transform.position + Vector3.up * 0.1f;
            Color homeColor = hasAlphaHomeOverride ? new Color(0.6f, 0f, 1f) : new Color(1f, 0.5f, 0f);

            Handles.color = new Color(homeColor.r, homeColor.g, homeColor.b, 0.35f);
            Handles.DrawSolidDisc(homePos, Vector3.up, 1.5f);
            Handles.color = homeColor;
            Handles.DrawWireDisc(homePos, Vector3.up, 1.5f);
            Handles.DrawDottedLine(transform.position, homePos, 4f);

            GUIStyle homeStyle = new();
            homeStyle.alignment = TextAnchor.MiddleCenter;
            homeStyle.fontSize = 10;
            homeStyle.normal.textColor = homeColor;
            Handles.Label(
                effectiveHome.transform.position + Vector3.up * 2f,
                hasAlphaHomeOverride ? "a Home (override)" : "Home",
                homeStyle);
        }

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

        if (isActingAsAlpha)
        {
            style.normal.textColor = new Color(1f, 0.5f, 0f);
            style.fontSize = 11;
            Handles.Label(labelPos + Vector3.up * 1.2f, "ALPHA", style);
        }
    }
#endif
    #endregion
}