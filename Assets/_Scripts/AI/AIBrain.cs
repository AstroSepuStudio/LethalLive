using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;

[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NavMeshAgent))]
public class AIBrain : NetworkBehaviour
{
    public enum SFXEvent { Living, Attack, CallForHelp, AlphaCall, Happy }

    [System.Serializable]
    public struct SFXGroup
    {
        public SFXEvent Event;
        public AudioSFX[] Clips;
    }

    [Header("AI Core")]
    [SerializeField] protected Animator animator;
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
    protected Dictionary<SFXEvent, AudioSFX[]> sfxMap;
    float livingSFXTimer;

    public string Prefix => $"[AIBrain ({gameObject.name})]";

    [field:SerializeField] protected AIState CurrentState { get; private set; }

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
        if (CurrentState == null) return;
        CurrentState.OnUpdateState(this);
        TickLivingSFX();
    }

    void TickLivingSFX()
    {
        if (sfxMap == null) return;

        livingSFXTimer -= Time.deltaTime;
        if (livingSFXTimer > 0f) return;

        livingSFXTimer = Random.Range(minLivingSFXInterval, maxLivingSFXInterval);
        PlaySFX(SFXEvent.Living);
    }

    #endregion

    #region SFX

    void BuildSFXMap()
    {
        sfxMap = new Dictionary<SFXEvent, AudioSFX[]>();
        foreach (var group in sfxGroups)
            sfxMap[group.Event] = group.Clips;
    }

    [Server]
    public void PlaySFX(SFXEvent sfxEvent)
    {
        if (!sfxMap.TryGetValue(sfxEvent, out var clips) || clips.Length == 0) return;
        int index = Random.Range(0, clips.Length);
        RpcPlaySFX(sfxEvent, index);
    }

    [ClientRpc]
    void RpcPlaySFX(SFXEvent sfxEvent, int clipIndex)
    {
        if (audioSrc == null) return;
        if (!sfxMap.TryGetValue(sfxEvent, out var clips) || clipIndex >= clips.Length) return;

        AudioManager.Instance.PlayOneShot(audioSrc, clips[clipIndex]);
    }

    #endregion

    protected bool HasLineOfSight(Vector3 target)
    {
        Vector3 origin = transform.position + Vector3.up;
        Vector3 dir = (target + Vector3.up) - origin;
        return !Physics.Raycast(origin, dir.normalized, dir.magnitude, losBlockingLayers, QueryTriggerInteraction.Ignore);
    }

    protected void OnAgentHurt(AttackSource source, AttackStat attack)
    {

    }

    protected void OnAgentDeath(AttackSource source, AttackStat attack)
    {

    }

    public void MoveAgent(Vector3 position) => agent.SetDestination(position);
    public void StopAgentMovement() => agent.isStopped = true;
    public void ResumeAgentMovement() => agent.isStopped = false;
    public void DisableAgent() => agent.enabled = false;
    public bool IsAgentInMovement() =>  
        !agent.isStopped && agent.hasPath && 
        agent.remainingDistance > agent.stoppingDistance && 
        agent.velocity.sqrMagnitude > 0.01f;
}
