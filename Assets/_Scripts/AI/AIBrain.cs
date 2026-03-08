using Mirror;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NavMeshAgent))]
public class AIBrain : NetworkBehaviour
{
    [Header("AI Core")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected NavMeshAgent agent;

    [SerializeField] protected AIState[] states;
    [SerializeField] protected LayerMask losBlockingLayers;

    [SerializeField] protected EntityStats entityStats;
    [SerializeField] protected AttackStat attackStat;
    public string Prefix => $"[AIBrain ({gameObject.name})]";

    [field:SerializeField] protected AIState CurrentState { get; private set; }

    public NavMeshAgent Agent => agent;
    public Animator Animator_ => animator;
    public EntityStats EntityStats_ => entityStats;
    public AttackStat AttackStat_ => attackStat;

    protected virtual void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
    }

    protected virtual void Start()
    {
        if (states == null || states.Length == 0)
        {
            Debug.LogWarning($"{Prefix} Ai agent doesn't have any AI states");
            return;
        }

        SetState(states[0]);
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
    }

    protected bool HasLineOfSight(Vector3 target)
    {
        Vector3 origin = transform.position + Vector3.up;
        Vector3 dir = (target + Vector3.up) - origin;
        return !Physics.Raycast(origin, dir.normalized, dir.magnitude, losBlockingLayers, QueryTriggerInteraction.Ignore);
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
