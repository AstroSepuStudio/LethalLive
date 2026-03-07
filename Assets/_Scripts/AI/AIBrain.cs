using Mirror;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NavMeshAgent))]
public class AIBrain : NetworkBehaviour
{
    [SerializeField] protected Animator animator;
    [SerializeField] protected NavMeshAgent agent;

    [SerializeField] protected AIState[] states;

    protected string Prefix => $"[AIBrain ({gameObject.name})]";

    [field:SerializeField] protected AIState CurrentState { get; private set; }

    public NavMeshAgent Agent => agent;

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

    public void MoveAgent(Vector3 position) => agent.SetDestination(position);
    public void StopAgentMovement() => agent.isStopped = true;
    public void ResumeAgentMovement() => agent.isStopped = false;
    public void DisableAgent() => agent.enabled = false;
    public bool IsAgentInMovement() =>  
        !agent.isStopped && agent.hasPath && 
        agent.remainingDistance > agent.stoppingDistance && 
        agent.velocity.sqrMagnitude > 0.01f;
}
