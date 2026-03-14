using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NavMeshAgent))]
public class AIBrain : NetworkBehaviour
{
    public enum SFXEvent { Living, Attack, CallForHelp, AlphaCall, Happy, Footstep, Aggressive, Warning }

    [System.Serializable]
    public struct SFXGroup
    {
        public SFXEvent Event;
        public SoundLoudness Loudness;
        public AudioSFX[] Clips;
    }

    [Header("AI Core")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected Collider collider_;
    [SerializeField] protected SkinnedMeshRenderer renderer_;
    [SerializeField] protected NavMeshAgent agent;

    [SerializeField] protected AIState[] states;
    [SerializeField] protected LayerMask losBlockingLayers;

    [SerializeField] protected EntityStats entityStats;
    [SerializeField] protected AttackStat attackStat;

    [Header("AI Audio")]
    [SerializeField] protected AudioSource audioSrc;
    [SerializeField] SFXGroup[] sfxGroups;

    [SerializeField] float minLivingSFXInterval = 7f;
    [SerializeField] float maxLivingSFXInterval = 30f;
    [SerializeField] float footstepDelay = 0.5f;
    [SerializeField] float footstepMinPitch = 0.9f;
    [SerializeField] float footstepMaxPitch = 1.1f;

    protected Dictionary<SFXEvent, SFXGroup> sfxMap;
    float livingSFXTimer;
    float footstepTimer;

    public string Prefix => $"[AIBrain ({gameObject.name})]";

    [field:SerializeField] protected AIState CurrentState { get; private set; }
    protected bool isDying;

    public NavMeshAgent Agent => agent;
    public Animator Animator_ => animator;
    public EntityStats EntityStats_ => entityStats;
    public AttackStat AttackStat_ => attackStat;

    #region Lifecycle

    protected virtual void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        BuildSFXMap();
    }

    protected virtual void Start()
    {
        if (states == null || states.Length == 0)
        {
            Debug.LogWarning($"{Prefix} Ai agent doesn't have any AI states");
            return;
        }

        SetState(states[0]);
        livingSFXTimer = Random.Range(minLivingSFXInterval, maxLivingSFXInterval);
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
        if (CurrentState == null) return;
        CurrentState.OnUpdateState(this);
        TickLivingSFX();
        TickFootstep();
    }

    protected virtual void TickLivingSFX()
    {
        if (sfxMap == null) return;

        livingSFXTimer -= Time.deltaTime;
        if (livingSFXTimer > 0f) return;

        livingSFXTimer = Random.Range(minLivingSFXInterval, maxLivingSFXInterval);
        PlaySFX(SFXEvent.Living, 1f);
    }

    protected virtual void TickFootstep()
    {
        if (!IsAgentInMovement())
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= Time.deltaTime;
        if (footstepTimer > 0f) return;

        footstepTimer = footstepDelay;
        float pitch = Random.Range(footstepMinPitch, footstepMaxPitch);
        PlaySFX(SFXEvent.Footstep, pitch);
    }

    #endregion

    #region SFX

    void BuildSFXMap()
    {
        sfxMap = new Dictionary<SFXEvent, SFXGroup>();
        foreach (var group in sfxGroups)
            sfxMap[group.Event] = group;
    }

    [Server]
    public virtual void PlaySFX(SFXEvent sfxEvent, float pitch)
    {
        if (!sfxMap.TryGetValue(sfxEvent, out var group) || group.Clips.Length == 0) return;
        int index = Random.Range(0, group.Clips.Length);
        RpcPlaySFX(sfxEvent, index, pitch);
    }

    [ClientRpc]
    void RpcPlaySFX(SFXEvent sfxEvent, int clipIndex, float pitch)
    {
        if (audioSrc == null) return;
        if (!sfxMap.TryGetValue(sfxEvent, out var group) || clipIndex >= group.Clips.Length) return;

        audioSrc.pitch = pitch;
        AudioManager.Instance.PlayOneShot(audioSrc, group.Clips[clipIndex], gameObject, group.Loudness);
    }

    #endregion

    public bool HasLineOfSight(Vector3 target)
    {
        Vector3 origin = transform.position + Vector3.up;
        Vector3 dir = (target + Vector3.up) - origin;
        return !Physics.Raycast(origin, dir.normalized, dir.magnitude, losBlockingLayers, QueryTriggerInteraction.Ignore);
    }

    public virtual void OnAgentHurt(AttackEvent source)
    {

    }

    public virtual void OnAgentDeath(AttackEvent source)
    {

    }

    public void MoveAgent(Vector3 position) => agent.SetDestination(position);
    public void StopAgentMovement() => agent.isStopped = true;
    public void ResumeAgentMovement() => agent.isStopped = false;
    public void DisableAgent() => agent.enabled = false;
    public void DisableCollider() => collider_.enabled = false;
    public bool IsAgentInMovement() =>  
        !agent.isStopped && agent.hasPath && 
        agent.remainingDistance > agent.stoppingDistance && 
        agent.velocity.sqrMagnitude > 0.01f;

    protected virtual void OnDrawGizmosSelected()
    {
        if (attackStat == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackStat.AttackRadius);
    }
}
