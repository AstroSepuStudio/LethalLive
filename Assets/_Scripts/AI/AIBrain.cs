using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NavMeshAgent))]
public class AIBrain : NetworkBehaviour
{
    public enum SFXEvent { Living, Attack, CallForHelp, AlphaCall, Happy, Footstep, Aggressive, Warning, Alert }
    public enum SourceType { Default, LivingSound, Footstep, Idle }

    public enum ModuleEvent
    {
        BeginSearch,
        RespondToAlphaCall,
        RespondToHelpCall,
    }

    [System.Serializable]
    public struct SFXGroup
    {
        public SFXEvent Event;
        public SoundLoudness Loudness;
        public AudioSFX[] Clips;
    }

    [System.Serializable]
    public struct AIAudioSource
    {
        public SourceType Type;
        public AudioSource Source;
    }

    [System.Serializable]
    public struct LootDrop
    {
        public ItemSO lootData;
        [Range(0f, 100f)] public float dropChance;
        [Range(1, 5)] public int minQuantity;
        [Range(1, 5)] public int maxQuantity;
    }

    [System.Serializable]
    public struct IdleAnimationTrigger
    {
        public string Trigger;
        public AudioSFX Audio_SFX;
        public float Delay;
    }

    [Header("AI Core")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected Collider collider_;
    [SerializeField] protected SkinnedMeshRenderer renderer_;
    [SerializeField] protected NavMeshAgent agent;
    [SerializeField] protected AILootDropper lootDropper;

    [SerializeField] protected AIState[] states;
    [SerializeField] protected AIModule[] modules;
    public LayerMask losBlockingLayers;

    [SerializeField] protected EntityStats entityStats;
    [SerializeField] protected AttackStat attackStat;

    [SerializeField] protected LootDrop[] lootPool;

    protected bool aggressive;

    [Header("Idle")]
    [SerializeField] IdleAnimationTrigger[] idleTriggers;
    [SerializeField] float minIdleCD = 6f;
    [SerializeField] float maxIdleCD = 18f;
    protected bool isIdle = true;
    float idleTimer = 0;

    [Header("AI Audio")]
    [SerializeField] protected AIAudioSource[] audioSources;
    [SerializeField] SFXGroup[] sfxGroups;

    [SerializeField] float minLivingSFXInterval = 7f;
    [SerializeField] float maxLivingSFXInterval = 30f;
    [SerializeField] float footstepDelay = 0.5f;
    [SerializeField] float footstepAggressiveDelay = 0.5f;
    [SerializeField] float footstepMinPitch = 0.9f;
    [SerializeField] float footstepMaxPitch = 1.1f;
    protected bool stayQuiet = false;
    protected Dictionary<SFXEvent, SFXGroup> sfxMap;
    protected Dictionary<SourceType, AudioSource> sourceMap;
    float livingSFXTimer;
    float footstepTimer;

    readonly Dictionary<System.Type, AIModule> moduleMap = new();

    public T GetModule<T>() where T : AIModule
    {
        moduleMap.TryGetValue(typeof(T), out var module);
        return module as T;
    }

    public bool TryGetModule<T>(out T result) where T : AIModule
    {
        result = GetModule<T>();
        return result != null;
    }

    protected virtual void RegisterModules()
    {
        moduleMap.Clear();
        foreach (var m in modules)
            moduleMap[m.GetType()] = m;

        foreach (var m in modules)
            m.OnModuleInit(this);
    }

    public string Prefix => $"[AIBrain ({gameObject.name})]";
    public LootDrop[] GetLootPool() => lootPool;

    //[field: SerializeField] protected AIState CurrentState { get; private set; }
    public AIState CurrentState { get; private set; }
    protected bool isDying;

    public NavMeshAgent Agent => agent;
    public Animator Animator_ => animator;
    public EntityStats EntityStats_ => entityStats;
    public AttackStat AttackStat_
    {
        get => attackStat;
        set => attackStat = value;
    }

    #region Lifecycle

    protected virtual void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        BuildSFXMap();
    }

    protected virtual void Start()
    {
        if (!isServer)
        {
            agent.enabled = false;
            return;
        }

        RegisterModules();

        if (states == null || states.Length == 0)
        {
            Debug.LogWarning($"{Prefix} Ai agent doesn't have any AI states");
            return;
        }

        SetState(states[0]);
        livingSFXTimer = Random.Range(minLivingSFXInterval, maxLivingSFXInterval);
        idleTimer = Random.Range(minIdleCD, maxIdleCD);

        GameTick.OnTick += OnTick;
    }

    protected virtual void OnDestroy()
    {
        if (isServer) GameTick.OnTick -= OnTick;
    }

    protected void SetState(AIState newState)
    {
        if (CurrentState != null) CurrentState.OnExitState(this);
        CurrentState = newState;
        CurrentState.OnEnterState(this);
    }

    protected virtual void Update()
    {
        if (isDying) return;
        if (!isServer) return;
        if (CurrentState == null) return;

        CurrentState.OnUpdateState(this);

        TickFootstep();
    }

    protected virtual void OnTick()
    {
        if (isDying) return;
        TickLivingSFX();

        foreach (var m in moduleMap.Values)
            m.OnModuleTick(this);

        if (!isIdle) return;
        if (idleTriggers == null || idleTriggers.Length == 0) return;

        idleTimer -= GameTick.TickRate;
        if (idleTimer <= 0)
        {
            idleTimer = Random.Range(minIdleCD, maxIdleCD);

            IdleAnimationTrigger iat = idleTriggers[Random.Range(0, idleTriggers.Length)];

            if (!stayQuiet && iat.Audio_SFX != null) 
                AudioManager.Instance.PlayOneShotWithDelay(TryGetAudioSource(SourceType.Idle), iat.Audio_SFX, iat.Delay, gameObject);

            animator.SetTrigger(iat.Trigger);
        }
    }

    protected virtual void TickLivingSFX()
    {
        if (sfxMap == null || stayQuiet) return;

        livingSFXTimer -= GameTick.TickRate;
        if (livingSFXTimer > 0f) return;

        livingSFXTimer = Random.Range(minLivingSFXInterval, maxLivingSFXInterval);
        PlaySFX(SourceType.LivingSound, SFXEvent.Living, 1f);
    }

    protected virtual void TickFootstep()
    {
        if (!IsAgentInMovement())
        {
            footstepTimer = 0f;
            return;
        }

        if (aggressive && footstepTimer > footstepAggressiveDelay) 
            footstepTimer = footstepAggressiveDelay;

        footstepTimer -= Time.deltaTime;
        if (footstepTimer > 0f) return;

        footstepTimer = aggressive ? footstepAggressiveDelay : footstepDelay;
        float pitch = Random.Range(footstepMinPitch, footstepMaxPitch);

        if (stayQuiet)
        {
            int loudnessLvl = (int)sfxMap[SFXEvent.Footstep].Loudness;
            loudnessLvl = Mathf.Clamp(loudnessLvl - 1, 0, 4);
            PlaySFX(SourceType.Footstep, SFXEvent.Footstep, pitch, true, (SoundLoudness)loudnessLvl);
        }
        else
            PlaySFX(SourceType.Footstep, SFXEvent.Footstep, pitch);
    }

    #endregion

    #region SFX

    void BuildSFXMap()
    {
        sfxMap = new Dictionary<SFXEvent, SFXGroup>();
        foreach (var group in sfxGroups)
            sfxMap[group.Event] = group;

        sourceMap = new Dictionary<SourceType, AudioSource>();
        foreach (var source in audioSources)
            sourceMap[source.Type] = source.Source;
    }

    AudioSource TryGetAudioSource(SourceType type)
    {
        if (!sourceMap.TryGetValue(type, out AudioSource src))
            sourceMap.TryGetValue(SourceType.Default, out src);

        return src;
    }

    [Server]
    public virtual void PlaySFX(SourceType srcType, SFXEvent sfxEvent, float pitch, 
        bool overrideLoudness = false, SoundLoudness loudnessOverride = SoundLoudness.NoSound)
    {
        if (!sfxMap.TryGetValue(sfxEvent, out var group) || group.Clips.Length == 0) return;
        int clipIndex = Random.Range(0, group.Clips.Length);

        if (overrideLoudness)
            RpcPlaySFX(srcType, sfxEvent, clipIndex, pitch, loudnessOverride, 0.6f);
        else
            RpcPlaySFX(srcType, sfxEvent, clipIndex, pitch);
    }

    [ClientRpc]
    void RpcPlaySFX(SourceType srcType, SFXEvent sfxEvent, int clipIndex, float pitch)
    {
        if (!sourceMap.TryGetValue(srcType, out var src))
            sourceMap.TryGetValue(SourceType.Default, out src);
        if (src == null) return;
        if (!sfxMap.TryGetValue(sfxEvent, out var group) || clipIndex >= group.Clips.Length) return;
        
        src.pitch = pitch;
        AudioManager.Instance.PlayOneShot(src, group.Clips[clipIndex], gameObject, group.Loudness);
    }

    [ClientRpc]
    void RpcPlaySFX(SourceType srcType, SFXEvent sfxEvent, int clipIndex, float pitch, SoundLoudness loudness, float volumeMultiplier)
    {
        if (!sourceMap.TryGetValue(srcType, out var src))
            sourceMap.TryGetValue(SourceType.Default, out src);
        if (src == null) return;
        if (!sfxMap.TryGetValue(sfxEvent, out var group) || clipIndex >= group.Clips.Length) return;

        src.pitch = pitch;
        AudioManager.Instance.PlayOneShot(src, group.Clips[clipIndex], volumeMultiplier, gameObject, loudness);
    }

    #endregion

    #region Module Events

    public virtual void OnModuleEvent(ModuleEvent evt, object context = null) { }

    #endregion

    #region Helpers

    public bool HasLineOfSight(Vector3 target)
    {
        Vector3 origin = transform.position + Vector3.up;
        Vector3 dir = (target + Vector3.up) - origin;
        return !Physics.Raycast(origin, dir.normalized, dir.magnitude, losBlockingLayers, QueryTriggerInteraction.Ignore);
    }

    public virtual void OnAgentHurt(AttackEvent source) { }

    public virtual void OnAgentDeath(AttackEvent source)
    {
        CurrentState.OnExitState(this);

        if (lootDropper == null) TryGetComponent(out lootDropper);
        if (lootDropper != null) lootDropper.OnOwnerDeath(source);

        isDying = true;
        animator.SetTrigger("Death");
        StopAgentMovement();
        DisableCollider();
        DisableAgent();
    }

    public virtual void SetAggressive(bool aggressive)
    {
        this.aggressive = aggressive;
        animator.SetBool("Aggressive", aggressive);
    }

    protected PlayerData GetPlayerNearItem(ItemBase item, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(item.transform.position, radius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            PlayerData p = hit.GetComponent<PlayerData>();
            if (p != null) return p;
        }
        return null;
    }

    public void ResetSpeed() => agent.speed = entityStats.speed;
    public void MoveAgent(Vector3 position) { if (agent.enabled) agent.SetDestination(position); }
    public void StopAgentMovement() { if (agent.enabled) agent.isStopped = true; }
    public void ResumeAgentMovement() { if (agent.enabled) agent.isStopped = false; }
    public void DisableAgent() => agent.enabled = false;
    public void DisableCollider() => collider_.enabled = false;
    public bool IsAgentInMovement() => agent.enabled &&
        !agent.isStopped && agent.hasPath &&
        agent.remainingDistance > agent.stoppingDistance &&
        agent.velocity.sqrMagnitude > 0.01f;
    public void SetIdleState(bool isIdle) => this.isIdle = isIdle;

    protected virtual void OnDrawGizmosSelected()
    {
        if (attackStat == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackStat.AttackRadius);
    }

    #endregion
}
