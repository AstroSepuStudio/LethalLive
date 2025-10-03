using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class VortexAI : NetworkBehaviour
{
    public enum CreatureState { Wandering, Curious, Aggressive }

    [SyncVar] private CreatureState state;
    [SyncVar] private float interest;
    [SyncVar] private float patience;

    [SerializeField] NavMeshAgent agent;
    [SerializeField] LayerMask entityMask;
    [SerializeField] Animator animator;

    [Header("General")]
    [SerializeField] float wanderRadius = 10f;
    [SerializeField] float visionRange = 15f;
    [SerializeField] float personalSpace = 3f;
    [SerializeField] float seenDecayRate = 0.1f;
    [SerializeField] float sleepDuration = 5f;

    [Header("Agressive")]
    [SerializeField] float patienceThreshold = 100f;
    [SerializeField] float patienceDecay = 2f;

    [Header("Curious")]
    [SerializeField] float interestCooldown = 3f;
    [SerializeField] float interestEnterThreshold = 50f;
    [SerializeField] float interestExitThreshold = 20f;
    [SerializeField] float interestDecay = 5f;
    [SerializeField] float baseInterestGain = 10f;
    [SerializeField] float repeatedInterestFactor = 0.5f;
    [SerializeField] float forcedInterestCooldown = 1f;

    private readonly Dictionary<Transform, float> seenPlayers = new();
    private Transform targetPlayer;
    private Vector3 wanderDestination;
    private float sleepTimer;
    private bool _sleeping;
    private float lostInterestTimer = 0f;
    private float forcedInterestTimer = 0f;

    private Collider[] visionHits = new Collider[64];

    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (isServer) PickNewWanderDestination();

        _sleeping = true;
        sleepTimer = sleepDuration;

        if (isServer)
            GameTick.OnTick += OnTick;
    }

    private void OnDestroy()
    {
        if (isServer)
            GameTick.OnTick -= OnTick;
    }

    [Server]
    void OnTick()
    {
        UpdateTargetPlayer();
    }

    private void Update()
    {
        switch (state)
        {
            case CreatureState.Wandering: HandleWandering(); break;
            case CreatureState.Curious: HandleCurious(); break;
            case CreatureState.Aggressive: HandleAggressive(); break;
        }
    }

    void HandleWandering()
    {
        if (lostInterestTimer > 0f)
            lostInterestTimer -= Time.deltaTime;

        if (targetPlayer != null && lostInterestTimer <= 0)
        {
            float dist = Vector3.Distance(transform.position, targetPlayer.position);
            if (dist < personalSpace)
            {
                state = CreatureState.Curious;
                forcedInterestTimer = forcedInterestCooldown;
                return;
            }

            float multiplier = 1f;
            if (seenPlayers.TryGetValue(targetPlayer, out float seenValue))
            {
                multiplier = Mathf.Max(0.1f, seenValue);
                seenPlayers[targetPlayer] = multiplier * repeatedInterestFactor;
            }
            else seenPlayers[targetPlayer] = 1f;

            interest += Time.deltaTime * baseInterestGain * multiplier;
            if (interest >= interestEnterThreshold)
            {
                state = CreatureState.Curious;
                return;
            }
        }
        else
        {
            interest = Mathf.Max(0, interest - Time.deltaTime * interestDecay);
        }

        List<Transform> keys = new(seenPlayers.Keys);
        foreach (var p in keys)
        {
            seenPlayers[p] -= Time.deltaTime * seenDecayRate;
            if (seenPlayers[p] <= 0f) seenPlayers.Remove(p);
        }

        // --- Movement ---
        if (!agent.pathPending && agent.remainingDistance < 0.5f && !_sleeping)
        {
            sleepTimer = sleepDuration;
            _sleeping = true;
            animator.SetBool("P_Walk", false);
            animator.SetBool("A_Walk", false);
        }

        if (_sleeping)
        {
            sleepTimer -= Time.deltaTime;
            if (sleepTimer <= 0)
            {
                PickNewWanderDestination();
                _sleeping = false;
                animator.SetBool("A_Walk", false);
                animator.SetBool("P_Walk", true);
            }
        }
    }

    void HandleCurious()
    {
        if (targetPlayer == null)
        {
            Debug.Log("Lost target player");
            state = CreatureState.Wandering;
            agent.isStopped = false;
            return;
        }

        forcedInterestTimer -= Time.deltaTime;

        animator.SetBool("P_Walk", false);
        animator.SetBool("A_Walk", false);
        agent.isStopped = true;

        Vector3 lookPos = targetPlayer.position - transform.position;
        lookPos.y = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookPos), Time.deltaTime * 2f);

        float dist = Vector3.Distance(transform.position, targetPlayer.position);
        if (dist < personalSpace)
        {
            Vector3 dir = (transform.position - targetPlayer.position).normalized;
            agent.Move(2f * Time.deltaTime * dir);

            patience += Time.deltaTime * 20f;
            if (patience >= patienceThreshold) state = CreatureState.Aggressive;
        }
        else
        {
            patience = Mathf.Max(0, patience - Time.deltaTime * patienceDecay);
        }

        interest = Mathf.Max(0, interest - Time.deltaTime * interestDecay);
        if (interest <= interestExitThreshold && forcedInterestTimer <= 0)
        {
            state = CreatureState.Wandering;
            agent.isStopped = false;
            lostInterestTimer = interestCooldown;
        }
    }

    void HandleAggressive()
    {
        animator.SetBool("P_Walk", false);
        animator.SetBool("A_Walk", true);
        if (targetPlayer != null)
        {
            agent.SetDestination(targetPlayer.position);
            float dist = Vector3.Distance(transform.position, targetPlayer.position);
            if (dist < 2f)
            {
                // Attack logic here
            }
        }
        else
        {
            state = CreatureState.Wandering;
            patience = 0;
            interest = 0;
        }
    }

    void PickNewWanderDestination()
    {
        if (MapGenerator.Instance != null && MapGenerator.Instance.GeneratedDungeon)
        {
            Vector3 position = MapGenerator.Instance.GetRandomPosition();
            agent.SetDestination(position);
            return;
        }

        Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
        randomDir += transform.position;
        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, wanderRadius, 1))
        {
            wanderDestination = hit.position;
            agent.SetDestination(wanderDestination);
        }
    }

    void UpdateTargetPlayer()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, visionRange, visionHits, entityMask);
        float closestDist = Mathf.Infinity;
        Transform closest = null;

        for (int i = 0; i < count; i++)
        {
            if (Physics.Linecast(transform.position + Vector3.up, visionHits[i].transform.position + Vector3.up))
            {
                float d = Vector3.Distance(transform.position, visionHits[i].transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    closest = visionHits[i].transform;
                }
            }
        }

        targetPlayer = closest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, personalSpace);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
}
