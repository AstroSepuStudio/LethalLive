using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class VortexAI : AIBrain
{
    [SerializeField] Transform pickUpPos;
    [SerializeField] ParticleSystem deathParticles;
    [SerializeField] AudioSource alphaCallSrc;

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

    [Header("Detection")]
    [SerializeField] float detectionRadius = 10f;
    [SerializeField] float detectionInterval = 1f;
    [SerializeField] float playerTooCloseDistance = 3f;
    [SerializeField] float playerNearItemDistance = 3f;
    float detectionTimer = 0f;

    readonly HashSet<VortexAI> seenVortexes = new();

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

    AIModule_Alpha AlphaModule => GetModule<AIModule_Alpha>();
    AIModule_Home HomeModule => GetModule<AIModule_Home>();
    AIModule_Patience PatienceModule => GetModule<AIModule_Patience>();
    AIModule_ItemCarrier CarrierModule => GetModule<AIModule_ItemCarrier>();
    AIModule_Senses SensesModule => GetModule<AIModule_Senses>();

    public float Alpha => AlphaModule?.AlphaValue ?? 0f;
    public bool IsActingAsAlpha => AlphaModule?.IsActingAsAlpha ?? false;
    public AIBrain CurrentAlpha => followState?.AlphaTarget;
    public ItemBase CarriedItem => CarrierModule?.CarriedItem;
    public RoomData HomeRoom => HomeModule?.HomeRoom;
    public float Patience => PatienceModule?.Patience ?? float.MaxValue;

    public override void PlaySFX(SFXEvent sfxEvent, float pitch)
    {
        pitch *= AlphaModule?.GetPitch() ?? 1f;
        base.PlaySFX(sfxEvent, pitch);
    }

    public void PlayAlphaCall(float pitch)
    {
        if (!sfxMap.TryGetValue(SFXEvent.AlphaCall, out var group) || group.Clips.Length == 0) return;
        int index = Random.Range(0, group.Clips.Length);
        RpcPlayAlphaCall(index, pitch);
    }

    [ClientRpc]
    void RpcPlayAlphaCall(int clipIndex, float pitch)
    {
        if (alphaCallSrc == null) return;
        if (!sfxMap.TryGetValue(SFXEvent.AlphaCall, out var group) || group.Clips.Length == 0) return;
        audioSrc.pitch = pitch;
        AudioManager.Instance.PlayOneShot(alphaCallSrc, group.Clips[clipIndex], gameObject, group.Loudness);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        lootDropper.OnLootSpawned = (item) =>
        {
            float am = AlphaModule?.GetMultiplier() ?? 1f;
            item.MultiplyValue(am);
            item.SetScale(Vector3.one * Mathf.Lerp(0.6f, 1.2f, Alpha / 100f));
        };
    }

    protected override void Start()
    {
        base.Start();

        CacheStates();

        if (isServer)
            targetWanCycles = Random.Range(minWanderCycles, maxWanderCycles);
    }

    void CacheStates()
    {
        if (states == null) return;

        stareState = SafeGetState<AIS_StareAtVortex>(stareStateIndex);
        followState = SafeGetState<AIS_FollowAlpha>(followStateIndex);
        pickUpState = SafeGetState<AIS_PickUpItem>(pickUpStateIndex);
        dropAtHomeState = SafeGetState<AIS_DropItemAtHome>(dropAtHomeStateIndex);
        searchState = SafeGetState<AIS_SearchForItems>(searchStateIndex);
        followPlayerState = SafeGetState<AIS_FollowPlayer>(followPlayerStateIndex);
        attackFurnitureState = SafeGetState<AIS_AttackFurniture>(attackFurnitureStateIndex);
        backAwayState = SafeGetState<AIS_BackAwayFromPlayer>(backAwayStateIndex);
        stareAtPlayerNearItemState = SafeGetState<AIS_StareAtPlayerNearItem>(stareAtPlayerNearItemStateIndex);
        attackPlayerState = SafeGetState<AIS_AttackPlayer>(attackPlayerStateIndex);
    }

    T SafeGetState<T>(int index) where T : AIState
    {
        if (index >= 0 && index < states.Length)
            return states[index] as T;
        return null;
    }

    protected override void Update()
    {
        if (isDying) return;
        if (!isServer) return;

        base.Update();

        detectionTimer -= Time.deltaTime;
        if (detectionTimer > 0f) return;
        detectionTimer = detectionInterval;

        CheckHomeItems();
        RunDetection();
    }

    public override void OnModuleEvent(ModuleEvent evt, object context = null)
    {
        switch (evt)
        {
            case ModuleEvent.BeginSearch:
                TriggerSearch();
                break;

            case ModuleEvent.RespondToAlphaCall:
                if (context is VortexAI alphaSource) RespondToAlphaCall(alphaSource);
                break;

            case ModuleEvent.RespondToHelpCall:
                if (context is VortexAI helpCaller) RespondToHelpCall(helpCaller);
                break;
        }
    }

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

        var carrier = CarrierModule;
        var home = HomeModule;

        foreach (var hit in hits)
        {
            string tag = hit.tag;

            if (tag == "Vortex")
            {
                if (!IsInWanderState()) continue;
                var other = hit.GetComponent<VortexAI>();
                if (other == null || other == this) continue;
                float d = Vector3.Distance(transform.position, other.transform.position);
                if (d < closestVortexDist && HasLineOfSight(other.transform.position))
                { closestVortexDist = d; closestVortex = other; }
            }
            else if (tag == "Item")
            {
                if (carrier != null && carrier.IsOnDropCooldown) continue;
                var item = hit.GetComponent<ItemBase>();
                if (item == null || !item.ItemData.pickable || item.HasOwner) continue;
                if (home != null && home.IsItemAtHome(item)) continue;
                float d = Vector3.Distance(transform.position, item.transform.position);
                if (d < closestItemDist && HasLineOfSight(item.transform.position))
                { closestItemDist = d; closestItem = item; }
            }
            else if (tag == "Player")
            {
                if (!hit.TryGetComponent<PlayerData>(out var player)) continue;
                if (!HasLineOfSight(player.transform.position)) continue;
                float d = Vector3.Distance(transform.position, player.transform.position);
                if (d <= playerTooCloseDistance) tooClosePlayer = player;
                if (d < closestPlayerDist) { closestPlayerDist = d; closestPlayer = player; }
            }
        }

        // Priority: back away > vortex stare > item pickup > player follow
        if (tooClosePlayer != null) { TriggerBackAway(); return; }

        if (closestVortex != null && !seenVortexes.Contains(closestVortex))
        { TriggerStare(closestVortex); return; }

        if (closestItem != null) { TriggerPickUp(closestItem); return; }

        if (closestPlayer != null && IsInWanderState())
        {
            var senses = SensesModule;
            if (senses != null && !senses.HasSeenPlayer(closestPlayer))
            {
                senses.RegisterSeenPlayer(closestPlayer);
                TriggerFollowPlayer();
            }
        }
    }

    void CheckHomeItems()
    {
        var home = HomeModule;
        if (home == null) return;

        RoomData effectiveHome = home.GetEffectiveHome();
        if (effectiveHome == null) return;

        float distFromHome = Vector3.Distance(transform.position, effectiveHome.transform.position);
        if (distFromHome > detectionRadius) return;

        home.ScanAndCheckStolenItems(this);
    }

    bool IsInWanderState()
    {
        if (CurrentState == null) return false;
        if (wanderHomeStateIndex < states.Length && CurrentState == states[wanderHomeStateIndex])
            return true;
        if (wanderIndexes == null) return false;
        foreach (int idx in wanderIndexes)
            if (idx < states.Length && CurrentState == states[idx]) return true;
        return false;
    }

    bool IsInAttackState()
    {
        if (attackFurnitureState != null && CurrentState == states[attackFurnitureStateIndex]) return true;
        if (attackPlayerState != null && CurrentState == states[attackPlayerStateIndex]) return true;
        return false;
    }

    void TriggerStare(VortexAI target)
    {
        if (stareState == null) return;
        seenVortexes.Add(target);
        stareState.TargetBrain = target;
        SetState(states[stareStateIndex]);
    }

    void TriggerPickUp(ItemBase item)
    {
        if (pickUpState == null) return;
        PlayerData blocker = GetPlayerNearItem(item, playerNearItemDistance);
        if (blocker != null) { TriggerStareAtPlayerNearItem(item, blocker); return; }
        pickUpState.TargetItem = item;
        SetState(states[pickUpStateIndex]);
    }

    void TriggerFollowPlayer()
    {
        if (followPlayerState == null) return;
        if (SensesModule == null) return;
        followPlayerState.WatchedPlayers = SensesModule.WatchedPlayers;
        SetState(states[followPlayerStateIndex]);
    }

    public void TriggerDropAtHome() => SetState(states[dropAtHomeStateIndex]);
    void TriggerWanderHome() => SetState(states[wanderHomeStateIndex]);
    public void ResumeWander() => SetState(states[wanderIndexes[curWanIndex]]);

    void TriggerSearch()
    {
        if (searchState == null) { TriggerDropAtHome(); return; }
        SetState(states[searchStateIndex]);
    }

    void TriggerBackAway()
    {
        if (backAwayState == null) { ResumeWander(); return; }
        if (PatienceModule == null) return;
        PatienceModule.DrainOnBackAway();
        if (PatienceModule == null || !PatienceModule.IsExhausted)
            SetState(states[backAwayStateIndex]);
    }

    void TriggerStareAtPlayerNearItem(ItemBase item, PlayerData blocker)
    {
        if (stareAtPlayerNearItemState == null) { ResumeWander(); return; }
        stareAtPlayerNearItemState.WatchedItem = item;
        stareAtPlayerNearItemState.BlockingPlayer = blocker;
        SetState(states[stareAtPlayerNearItemStateIndex]);
    }

    public void TriggerAttackPlayer()
    {
        var patience = PatienceModule;
        if (patience != null) patience.Restore(0f);

        if (attackPlayerState == null) { ResumeWander(); return; }
        if (SensesModule == null) return;

        PlayerData target = SensesModule.GetClosestSeenPlayer(this);
        if (target == null) { ResumeWander(); return; }

        if (CarrierModule != null)
        {
            ItemBase droppedItem = CarriedItem;
            CarrierModule.DropCarriedItem();
            attackPlayerState.ItemToRecoverAfter = droppedItem;
        }
        
        attackPlayerState.Target = target;
        SetState(states[attackPlayerStateIndex]);
    }

    void BecomeAlpha(VortexAI follower)
    {
        AlphaModule?.BecomeLeaderOf(follower, this);

        if (CurrentState == states[wanderHomeStateIndex])
        {
            OnArrivedAtHome();
            return;
        }

        TriggerWanderHome();
    }

    public void RespondToAlphaCall(VortexAI alpha)
    {
        if (IsInAttackState()) return;
        CarrierModule?.DropCarriedItem();

        PlayerData alphaTarget = alpha.attackPlayerState?.Target ?? alpha.SensesModule?.GetClosestSeenPlayer(alpha);

        if (alphaTarget != null) SensesModule?.RegisterSeenPlayer(alphaTarget);

        if (alphaTarget != null &&
            Vector3.Distance(transform.position, alphaTarget.transform.position) <= detectionRadius)
        {
            if (attackPlayerState == null) return;
            PatienceModule?.Restore(0f);
            attackPlayerState.Target = alphaTarget;
            attackPlayerState.ItemToRecoverAfter = null;
            SetState(states[attackPlayerStateIndex]);
            return;
        }

        TriggerWanderHome();
    }

    public void RespondToHelpCall(VortexAI caller)
    {
        if (IsInAttackState()) return;
        CarrierModule?.DropCarriedItem();
        PlaySFX(SFXEvent.CallForHelp, 1f);

        PlayerData target = caller.attackPlayerState?.Target ?? caller.SensesModule?.GetClosestSeenPlayer(caller);
        if (target == null) return;

        SensesModule?.RegisterSeenPlayer(target);

        if (Vector3.Distance(transform.position, target.transform.position) <= detectionRadius)
        {
            if (attackPlayerState == null) return;
            PatienceModule?.Restore(0f);
            attackPlayerState.Target = target;
            attackPlayerState.ItemToRecoverAfter = null;
            SetState(states[attackPlayerStateIndex]);
        }
    }

    public override void SetAggressive(bool aggressive)
    {
        base.SetAggressive(aggressive);
        renderer_.SetBlendShapeWeight(0, aggressive ? 0 : 100);
    }

    public void OnItemPickedUp() => TriggerDropAtHome();
    public void OnItemLost() => ResumeWander();

    public void OnItemDropped()
    {
        if (IsActingAsAlpha) { TriggerWanderHome(); return; }
        if (HomeModule?.HasOverride == true) TriggerSearch();
        else ResumeWander();
    }

    public void OnNoItemToDeliver()
    {
        if (IsActingAsAlpha) { TriggerWanderHome(); return; }
        if (HomeModule?.HasOverride == true) TriggerSearch();
        else ResumeWander();
    }

    public void OnSearchFailed() => TriggerDropAtHome();
    public void OnSearchItemFound() => TriggerDropAtHome();

    public void OnStareDecisionMade(bool shouldFollow)
    {
        if (shouldFollow && followState != null && stareState?.TargetBrain != null)
        {
            AIBrain targetBrain = stareState.TargetBrain;
            VortexAI target = targetBrain as VortexAI;

            var targetHome = target.HomeModule;
            var targetFollow = target.followState;
            VortexAI actualAlpha = (targetHome != null && targetHome.HasOverride && targetFollow?.AlphaTarget is VortexAI va)
                ? va
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
            if (CarriedItem != null) { TriggerDropAtHome(); return; }

            if (!IsActingAsAlpha && HomeModule?.HasOverride != true)
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
        HomeModule?.ClearOverride();
        seenVortexes.Clear();

        if (followState?.AlphaTarget is VortexAI alpha)
            alpha.AlphaModule?.RemoveFromPack(this);

        ResumeWander();
    }

    public void OnArrivedAtHome()
    {
        HomeModule?.ScanHomeItems();
        AlphaModule?.DispatchPendingFollowers();
    }

    public void OnCuriosityExpired()
    {
        SensesModule?.ClearWatched();
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
        PatienceModule?.Restore(0.3f);

        ItemBase itemToRecover = attackPlayerState?.ItemToRecoverAfter;
        if (itemToRecover != null && itemToRecover.ItemData.pickable && !itemToRecover.HasOwner)
        {
            attackPlayerState.ItemToRecoverAfter = null;
            TriggerPickUp(itemToRecover);
            return;
        }

        ResumeWander();
    }

    public void DrainPatienceOnItemStolen() => PatienceModule?.DrainOnItemStolen();
    public void DrainPatienceOnBackAway() => PatienceModule?.DrainOnBackAway();

    public override void OnAgentHurt(AttackEvent source)
    {
        if (IsActingAsAlpha && AlphaModule != null) OnAlphaHurt();
        OnVortexHurt();
    }

    void OnAlphaHurt()
    {
        PlaySFX(SFXEvent.AlphaCall, 1f);
        TriggerAttackPlayer();
        AlphaModule?.AlertPack(this);
    }

    void OnVortexHurt()
    {
        PlaySFX(SFXEvent.CallForHelp, 1f);
        TriggerAttackPlayer();

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Vortex")) continue;
            var other = hit.GetComponent<VortexAI>();
            if (other == null || other == this || other.IsInAttackState()) continue;
            other.RespondToHelpCall(this);
        }
    }

    public override void OnAgentDeath(AttackEvent source)
    {
        base.OnAgentDeath(source);

        CarrierModule?.DropCarriedItem();
        isDying = true;
        StopAgentMovement();
        DisableCollider();
        DisableAgent();
        animator.SetTrigger("Death");

        StartCoroutine(DespawnVortex());
        RPC_PlayDeathParticles();
    }

    IEnumerator DespawnVortex()
    {
        yield return new WaitForSeconds(8f);
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc] void RPC_PlayDeathParticles() => StartCoroutine(PlayDeathParticles());

    IEnumerator PlayDeathParticles()
    {
        deathParticles.Play();
        yield return new WaitForSeconds(2f);
        deathParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Handles.color = Color.yellow;
        Handles.DrawWireDisc(transform.position, Vector3.up, detectionRadius);

        RoomData effectiveHome = HomeModule?.GetEffectiveHome();
        if (effectiveHome != null)
        {
            Vector3 homePos = effectiveHome.transform.position + Vector3.up * 0.1f;
            bool hasOverride = HomeModule?.HasOverride ?? false;
            Color homeColor = hasOverride ? new Color(0.6f, 0f, 1f) : new Color(1f, 0.5f, 0f);

            Handles.color = new Color(homeColor.r, homeColor.g, homeColor.b, 0.35f);
            Handles.DrawSolidDisc(homePos, Vector3.up, 1.5f);
            Handles.color = homeColor;
            Handles.DrawWireDisc(homePos, Vector3.up, 1.5f);
            Handles.DrawDottedLine(transform.position, homePos, 4f);

            GUIStyle homeStyle = new() { alignment = TextAnchor.MiddleCenter, fontSize = 10 };
            homeStyle.normal.textColor = homeColor;
            Handles.Label(effectiveHome.transform.position + Vector3.up * 2f,
                hasOverride ? "Home (override)" : "Home", homeStyle);
        }

        GUIStyle style = new() { alignment = TextAnchor.MiddleCenter };
        Vector3 labelPos = transform.position + Vector3.up;

        style.normal.textColor = Color.cyan;
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        Handles.Label(labelPos, $"a {Alpha:F0}", style);

        if (CurrentState != null)
        {
            style.normal.textColor = Color.white;
            style.fontSize = 11;
            style.fontStyle = FontStyle.Normal;
            Handles.Label(labelPos, CurrentState.GetType().Name, style);
        }

        if (CarriedItem != null)
        {
            style.normal.textColor = Color.green;
            style.fontSize = 11;
            Handles.Label(labelPos + Vector3.up * 0.2f, $"[{CarriedItem.ItemData.itemName}]", style);
        }

        if (IsActingAsAlpha)
        {
            style.normal.textColor = new Color(1f, 0.5f, 0f);
            style.fontSize = 11;
            Handles.Label(labelPos + Vector3.up * 0.4f, "ALPHA", style);
        }

        var pm = PatienceModule;
        if (pm != null)
        {
            style.normal.textColor = Color.Lerp(Color.red, Color.green, pm.Patience / pm.MaxPatience);
            style.fontSize = 10;
            Handles.Label(labelPos + Vector3.up * 0.6f,
                $"{pm.CurrentPersonality} [{pm.Patience:F0}/{pm.MaxPatience:F0}]", style);
        }
    }
#endif
}
