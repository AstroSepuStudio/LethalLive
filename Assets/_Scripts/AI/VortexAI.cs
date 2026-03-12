using System.Collections.Generic;
using UnityEngine;
using Mirror;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class VortexAI : AIBrain
{
    [Header("State Indexes")]
    [SerializeField] int[] wanderIndexes;
    [SerializeField] int stareStateIndex = 2;
    [SerializeField] int followStateIndex = 3;
    [SerializeField] int pickUpStateIndex = 4;
    [SerializeField] int dropAtHomeStateIndex = 5;
    [SerializeField] int wanderHomeStateIndex = 6;
    [SerializeField] int searchStateIndex = 7;
    [SerializeField] int followPlayerStateIndex = 8;
    [SerializeField] int attackFurnitureStateIndex = 9;
    [SerializeField] int backAwayStateIndex = 10;
    [SerializeField] int stareAtPlayerNearItemStateIndex = 11;
    [SerializeField] int attackPlayerStateIndex = 12;

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
    bool isActingAsAlpha = false;
    bool pendingFollowerDispatch = false;
    List<VortexAI> pendingFollowers = new();

    public float Alpha => alpha;
    public bool IsActingAsAlpha => isActingAsAlpha;
    public VortexAI CurrentAlpha => followState.AlphaTarget;

    [Header("Detection")]
    [SerializeField] float detectionRadius = 10f;
    [SerializeField] float detectionInterval = 1f;
    float detectionTimer = 0f;

    readonly HashSet<VortexAI> seenVortexes = new();
    readonly HashSet<PlayerData> seenPlayers = new();
    readonly List<PlayerData> watchedPlayers = new();

    [Header("Item")]
    [SerializeField] float dropCooldownDuration = 3f;
    [SerializeField] float playerTooCloseDistance = 3f;
    [SerializeField] float playerNearItemDistance = 3f;
    float postDropCooldown = 0f;

    public ItemBase CarriedItem { get; private set; }

    [Header("Home")]
    [SerializeField] float homeDistanceFraction = 0.5f;
    [SerializeField] float homeDistanceTolerance = 0.2f;
    bool hasAlphaHomeOverride = false;
    RoomData alphaHomeOverride = null;

    public RoomData HomeRoom { get; private set; }

    [Header("Patience")]
    [SerializeField] float patienceDecayDistance = 4;
    [SerializeField] float patienceDecay = 2f;
    [SerializeField] float patienceDecayOnBackAway = 5f;
    [SerializeField] float patienceDecayOnItemStolen = 25f;
    float patience;
    float maxPatience;

    public enum Personality { Passive, Cautious, Neutral, Irritated, Hostile }
    public Personality CurrentPersonality { get; private set; }
    public float Patience => patience;

    AIS_StareAtVortex stareState;
    AIS_FollowAlpha followState;
    AIS_PickUpItem pickUpState;
    AIS_DropItemAtHome dropAtHomeState;
    AIS_SearchForItems searchState;
    AIS_FollowPlayer followPlayerState;
    AIS_AttackFurniture attackFurnitureState;
    AIS_BackAwayFromPlayer backAwayState;
    AIS_StareAtPlayerNearItem stareAtPlayerNearItemState;
    AIS_AttackPlayer attackPlayerState;

    public float GetAlphaPitch() => (2f - GetAlphaMultiplier());

    public override void PlaySFX(SFXEvent sfxEvent, float pitch)
    {
        pitch *= GetAlphaPitch();
        base.PlaySFX(sfxEvent, pitch);
    }

    #region Lifecycle

    public override void OnStartServer()
    {
        base.OnStartServer();

        alpha = Random.Range(0, 100);
        AssignPersonality();

        float am = GetAlphaMultiplier();
        entityStats.OverrideMaxHP(entityStats.maxHP * am, true);

        attackStat = new(
            Mathf.Clamp(attackStat.AttackRadius * am, 1.1f, 2f),
            attackStat.AttackKnock,
            attackStat.AttackForce * am,
            attackStat.AttackDamage * am,
            attackStat.AttackCooldown);
    }

    protected override void Start()
    {
        base.Start();

        SetStates();

        if (isServer)
            AssignHomeRoom();
    }

    void SetStates()
    {
        targetWanCycles = Random.Range(minWanderCycles, maxWanderCycles);

        if (states != null)
        {
            if (stareStateIndex < states.Length)
                stareState = states[stareStateIndex] as AIS_StareAtVortex;

            if (followStateIndex < states.Length)
                followState = states[followStateIndex] as AIS_FollowAlpha;

            if (pickUpStateIndex < states.Length)
                pickUpState = states[pickUpStateIndex] as AIS_PickUpItem;

            if (dropAtHomeStateIndex < states.Length)
                dropAtHomeState = states[dropAtHomeStateIndex] as AIS_DropItemAtHome;

            if (searchStateIndex < states.Length)
                searchState = states[searchStateIndex] as AIS_SearchForItems;

            if (attackFurnitureStateIndex < states.Length)
                attackFurnitureState = states[attackFurnitureStateIndex] as AIS_AttackFurniture;

            if (followPlayerStateIndex < states.Length)
                followPlayerState = states[followPlayerStateIndex] as AIS_FollowPlayer;

            if (backAwayStateIndex < states.Length)
                backAwayState = states[backAwayStateIndex] as AIS_BackAwayFromPlayer;

            if (stareAtPlayerNearItemStateIndex < states.Length)
                stareAtPlayerNearItemState = states[stareAtPlayerNearItemStateIndex] as AIS_StareAtPlayerNearItem;

            if (attackPlayerStateIndex < states.Length)
                attackPlayerState = states[attackPlayerStateIndex] as AIS_AttackPlayer;
        }
    }

    void UpdateScale(int oldValue, int newValue)
    {
        float scale = Mathf.Lerp(0.6f, 1.2f, (float)newValue / 100f);
        transform.localScale = new Vector3(scale, scale, scale);
    }

    void AssignPersonality()
    {
        if (alpha < 15) CurrentPersonality = Personality.Passive;
        else if (alpha < 35) CurrentPersonality = Personality.Cautious;
        else if (alpha < 60) CurrentPersonality = Personality.Neutral;
        else if (alpha < 80) CurrentPersonality = Personality.Irritated;
        else CurrentPersonality = Personality.Hostile;

        maxPatience = CurrentPersonality switch
        {
            Personality.Passive => Random.Range(120f, 180f),
            Personality.Cautious => Random.Range(70f, 120f),
            Personality.Neutral => Random.Range(35f, 70f),
            Personality.Irritated => Random.Range(12f, 35f),
            Personality.Hostile => Random.Range(2f, 12f),
            _ => 60f
        };

        patience = maxPatience;
    }


    protected override void Update()
    {
        if (!isServer) return;
        base.Update();
        TickPatience();

        if (postDropCooldown > 0f) postDropCooldown -= Time.deltaTime;

        detectionTimer -= Time.deltaTime;
        if (detectionTimer > 0f) return;
        detectionTimer = detectionInterval;

        RunDetection();
    }

    void TickPatience()
    {
        if (patience <= 0f) return;
        if (followPlayerState != null && CurrentState == followPlayerState) return;

        PlayerData closest = GetClosestSeenPlayer();
        if (closest == null)
        {
            closest = GetClosestPlayer();
            if (closest == null) return;
        }

        float dist = Vector3.Distance(transform.position, closest.transform.position);
        float proximity = 1f - Mathf.Clamp01(dist / patienceDecayDistance);
        patience -= patienceDecay * proximity * Time.deltaTime;

        if (patience <= 0f) TriggerAttackPlayer();
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

    void RunDetection()
    {
        if (CarriedItem != null) return;
        if (IsInAttackState()) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);

        VortexAI closestVortex = null;
        ItemBase closestItem = null;
        PlayerData closestPlayer = null;
        PlayerData tooClosePlayer = null;

        float closestVortexDist = float.MaxValue;
        float closestItemDist = float.MaxValue;
        float closestPlayerDist = float.MaxValue;

        foreach (var hit in hits)
        {
            string tag = hit.tag;

            if (tag == "Vortex")
            {
                if (!IsInWanderState()) continue;
                VortexAI other = hit.GetComponent<VortexAI>();
                if (other == null || other == this) continue;
                float d = Vector3.Distance(transform.position, other.transform.position);
                if (d < closestVortexDist && HasLineOfSight(other.transform.position))
                { closestVortexDist = d; closestVortex = other; }
            }
            else if (tag == "Item")
            {
                if (postDropCooldown > 0f) continue;
                ItemBase item = hit.GetComponent<ItemBase>();
                if (item == null || !item.ItemData.pickable || item.HasOwner) continue;
                if (IsItemAtEffectiveHome(item)) continue;
                float d = Vector3.Distance(transform.position, item.transform.position);
                if (d < closestItemDist && HasLineOfSight(item.transform.position))
                { closestItemDist = d; closestItem = item; }
            }
            else if (tag == "Player")
            {
                PlayerData player = hit.GetComponent<PlayerData>();
                if (player == null) continue;
                if (!HasLineOfSight(player.transform.position)) continue;
                float d = Vector3.Distance(transform.position, player.transform.position);
                if (d <= playerTooCloseDistance)
                    tooClosePlayer = player;
                if (d < closestPlayerDist)
                { closestPlayerDist = d; closestPlayer = player; }
            }
        }

        // Priority order: back away > vortex stare > item pickup > player follow
        if (tooClosePlayer != null)
        {
            TriggerBackAway();
            return;
        }

        if (closestVortex != null && !seenVortexes.Contains(closestVortex))
        {
            TriggerStare(closestVortex);
            return;
        }

        if (closestItem != null)
        {
            TriggerPickUp(closestItem);
            return;
        }

        if (closestPlayer != null && IsInWanderState())
        {
            if (!seenPlayers.Contains(closestPlayer))
            {
                seenPlayers.Add(closestPlayer);
                watchedPlayers.Add(closestPlayer);
                TriggerFollowPlayer();
            }
        }
    }

    bool IsItemAtEffectiveHome(ItemBase item)
    {
        RoomData home = GetEffectiveHome();
        if (home == null) return false;

        float threshold = DungeonGenerator.Instance.CellSize;
        return Vector3.Distance(item.transform.position, home.transform.position) <= threshold;
    }

    bool IsInAttackState()
    {
        if (attackFurnitureState != null && CurrentState == states[attackFurnitureStateIndex]) return true;
        if (attackPlayerState != null && CurrentState == states[attackPlayerStateIndex]) return true;
        return false;
    }

    #endregion

    #region State Helpers

    bool IsInWanderState()
    {
        if (CurrentState == null) return false;
        if (states[wanderHomeStateIndex] != null && CurrentState == states[wanderHomeStateIndex])
            return true;

        if (wanderIndexes == null) return false;
        foreach (int idx in wanderIndexes)
            if (idx < states.Length && CurrentState == states[idx]) return true;
        return false;
    }

    PlayerData GetClosestPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, patienceDecayDistance);
        PlayerData closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            if (!hit.TryGetComponent<PlayerData>(out var p)) continue;
            if (!HasLineOfSight(p.transform.position)) continue;
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < closestDist) { closestDist = d; closest = p; }
        }

        if (closest != null) 
        { 
            seenPlayers.Add(closest); 
            watchedPlayers.Add(closest);
        }
        return closest;
    }

    public PlayerData GetClosestSeenPlayer()
    {
        PlayerData closest = null;
        float closestDist = float.MaxValue;
        
        foreach (var p in seenPlayers)
        {
            if (p == null) continue;
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < closestDist) { closestDist = d; closest = p; }
        }
        return closest;
    }

    PlayerData GetPlayerNearItem(ItemBase item)
    {
        Collider[] hits = Physics.OverlapSphere(item.transform.position, playerNearItemDistance);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            PlayerData p = hit.GetComponent<PlayerData>();
            if (p != null) return p;
        }
        return null;
    }

    public void DrainPatience(float amount)
    {
        patience -= amount;
        if (patience <= 0f) TriggerAttackPlayer();
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

        PlayerData blocker = GetPlayerNearItem(item);
        if (blocker != null)
        {
            TriggerStareAtPlayerNearItem(item, blocker);
            return;
        }

        pickUpState.TargetItem = item;
        SetState(states[pickUpStateIndex]);
    }

    void TriggerFollowPlayer()
    {
        if (followPlayerState == null) return;
        followPlayerState.WatchedPlayers = watchedPlayers;
        SetState(states[followPlayerStateIndex]);
    }

    public void TriggerDropAtHome() => SetState(states[dropAtHomeStateIndex]);
    void TriggerWanderHome() => SetState(states[wanderHomeStateIndex]);
    void ResumeWander() => SetState(states[wanderIndexes[curWanIndex]]);

    void TriggerSearch()
    {
        if (searchState == null) { TriggerDropAtHome(); return; }
        SetState(states[searchStateIndex]);
    }

    void TriggerBackAway()
    {
        if (backAwayState == null) { ResumeWander(); return; }
        DrainPatienceOnBackAway();
        if (patience > 0f)
            SetState(states[backAwayStateIndex]);
    }

    void TriggerStareAtPlayerNearItem(ItemBase item, PlayerData blocker)
    {
        if (stareAtPlayerNearItemState == null) { ResumeWander(); return; }
        stareAtPlayerNearItemState.WatchedItem = item;
        stareAtPlayerNearItemState.BlockingPlayer = blocker;
        SetState(states[stareAtPlayerNearItemStateIndex]);
    }

    void TriggerAttackPlayer()
    {
        patience = 0f;
        if (attackPlayerState == null) { ResumeWander(); return; }

        PlayerData target = GetClosestSeenPlayer();
        if (target == null) { ResumeWander(); return; }

        ItemBase droppedItem = CarriedItem;
        if (CarriedItem != null) DropCarriedItem();

        attackPlayerState.Target = target;
        attackPlayerState.ItemToRecoverAfter = droppedItem;
        SetState(states[attackPlayerStateIndex]);
    }

    #endregion

    #region Item Carrying

    public void CarryItem(ItemBase item)
    {
        PlaySFX(SFXEvent.Happy, 1);

        if (stareAtPlayerNearItemState != null) stareAtPlayerNearItemState.WatchedItem = null;
        CarriedItem = item;
        item.OnPickUp();
        item.transform.SetParent(transform);
        item.transform.localPosition = Vector3.up * 1.5f;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            if (hit.TryGetComponent<PlayerData>(out var player)) 
                watchedPlayers.Remove(player);
        }
    }

    public void DropCarriedItem()
    {
        if (CarriedItem == null) return;
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

    float GetAlphaMultiplier()
    {
        return Mathf.Lerp(0.5f, 1.5f, (float)alpha / 100f);
    }

    #endregion

    #region Events

    public void OnItemPickedUp() => TriggerDropAtHome();
    public void OnItemLost() => ResumeWander();

    public void OnItemDropped()
    {
        if (isActingAsAlpha) { TriggerWanderHome(); return; }
        if (hasAlphaHomeOverride) TriggerSearch();
        else ResumeWander();
    }

    public void OnNoItemToDeliver()
    {
        if (isActingAsAlpha) { TriggerWanderHome(); return; }
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

    public void OnCuriosityExpired()
    {
        watchedPlayers.Clear();
        ResumeWander();
    }

    public void OnFurnitureBlocking()
    {
        if (attackFurnitureState == null) { ResumeWander(); return; }
        attackFurnitureState.Target = pickUpState.BlockingFurniture;
        attackFurnitureState.ItemToPickUpAfter = pickUpState.TargetItem;
        SetState(states[attackFurnitureStateIndex]);
    }

    public void OnFurnitureDestroyed()
    {
        if (attackFurnitureState?.ItemToPickUpAfter != null)
            TriggerPickUp(attackFurnitureState.ItemToPickUpAfter);
        else
            ResumeWander();
    }

    public void OnFurnitureLost() => ResumeWander();

    public void OnBackAwaySafe() => ResumeWander();
    public void OnBackAwayGaveUp() => ResumeWander();

    public void OnPlayerLeftItem()
    {
        if (stareAtPlayerNearItemState?.WatchedItem != null)
            TriggerPickUp(stareAtPlayerNearItemState.WatchedItem);
        else
            ResumeWander();
    }
    public void OnItemStareGaveUp() => ResumeWander();

    public void OnAttackPlayerLost() => ResumeWander();

    public void OnAttackPlayerCalmedDown()
    {
        patience = maxPatience * 0.3f;

        ItemBase itemToRecover = attackPlayerState != null ? attackPlayerState.ItemToRecoverAfter : null;
        if (itemToRecover != null && itemToRecover.ItemData.pickable && !itemToRecover.HasOwner)
        {
            attackPlayerState.ItemToRecoverAfter = null;
            TriggerPickUp(itemToRecover);
            return;
        }

        ResumeWander();
    }

    public void DrainPatienceOnItemStolen() => DrainPatience(patienceDecayOnItemStolen);
    public void DrainPatienceOnBackAway() => DrainPatience(patienceDecayOnBackAway);

    #endregion

    #region Gizmos
#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Handles.color = Color.yellow;
        Handles.DrawWireDisc(transform.position, Vector3.up, detectionRadius);

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

        style.normal.textColor = Color.Lerp(Color.red, Color.green, patience / maxPatience);
        style.fontSize = 10;
        Handles.Label(labelPos + Vector3.up * 1.6f,
            $"{CurrentPersonality} [{patience:F0}/{maxPatience:F0}]", style);
    }
#endif
    #endregion
}