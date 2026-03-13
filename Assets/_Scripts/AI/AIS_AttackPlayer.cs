using UnityEngine.Events;
using UnityEngine;

public class AIS_AttackPlayer : AIState
{
    [SerializeField] float movementGracePeriod = 0.2f;
    [SerializeField] float stuckTimeout = 4f;
    [SerializeField] float recalculateInterval = 0.3f;
    [SerializeField] float giveUpDistance = 10f;
    [SerializeField] float calmDownDuration = 10f;
    [SerializeField] float attackHitDelay = 0.2f;
    [SerializeField] float dodgeDistance = 1.5f;
    [SerializeField] float losCheckInterval = 0.5f;
    [SerializeField] float losGiveUpDuration = 3f;

    public PlayerData Target { get; set; }
    public ItemBase ItemToRecoverAfter { get; set; }

    float graceTimer;
    float stuckTimer;
    float recalcTimer;
    float calmDownTimer;
    float losCheckTimer;
    float losLostTimer;
    bool hasLOS;
    bool calmingDown;

    bool attackPending;
    float attackTimer;
    Vector3 targetPosAtSwing;

    public UnityEvent OnTargetLost;
    public UnityEvent OnCalmedDown;

    public override void OnEnterState(AIBrain brain)
    {
        if (Target == null) { OnTargetLost?.Invoke(); return; }
        brain.ResumeAgentMovement();
        brain.Agent.stoppingDistance = brain.AttackStat_.AttackRadius * 0.8f;
        brain.Animator_.SetBool("Aggresive", true);
        brain.PlaySFX(AIBrain.SFXEvent.Aggressive, 1);
        graceTimer = movementGracePeriod;
        stuckTimer = stuckTimeout;
        recalcTimer = 0f;
        attackTimer = 0f;
        losCheckTimer = 0f;
        losLostTimer = 0f;
        hasLOS = true;
        calmingDown = false;
        attackPending = false;
    }

    public override void OnUpdateState(AIBrain brain)
    {
        if (calmingDown)
        {
            calmDownTimer -= Time.deltaTime;
            if (calmDownTimer <= 0f) OnCalmedDown?.Invoke();
            return;
        }

        if (Target == null || Target.Player_Stats.dead || Target.Player_Stats.knocked)
        {
            EnterCalmDown(brain);
            return;
        }

        float dist = Vector3.Distance(brain.transform.position, Target.transform.position);

        if (dist > giveUpDistance)
        {
            EnterCalmDown(brain);
            return;
        }

        if (graceTimer > 0f) { graceTimer -= Time.deltaTime; return; }

        losCheckTimer -= Time.deltaTime;
        if (losCheckTimer <= 0f)
        {
            losCheckTimer = losCheckInterval;
            hasLOS = brain.HasLineOfSight(Target.transform.position);
        }

        if (!hasLOS)
        {
            losLostTimer += Time.deltaTime;
            if (losLostTimer >= losGiveUpDuration)
            {
                EnterCalmDown(brain);
                return;
            }
        }
        else
        {
            losLostTimer = 0f;
        }
        
        Vector3 dir = (Target.transform.position - brain.transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            brain.transform.rotation = Quaternion.LookRotation(dir);

        bool moving = brain.IsAgentInMovement();

        if (dist > brain.AttackStat_.AttackRadius)
        {
            recalcTimer -= Time.deltaTime;
            if (recalcTimer <= 0f)
            {
                recalcTimer = recalculateInterval;
                brain.MoveAgent(Target.transform.position);
            }

            if (!moving)
            {
                stuckTimer -= Time.deltaTime;
                if (stuckTimer <= 0f)
                {
                    EnterCalmDown(brain);
                    return;
                }
            }
        }

        if (moving)
        {
            stuckTimer = stuckTimeout;
            brain.Animator_.SetBool("Walk", true);
        }

        if (dist <= brain.AttackStat_.AttackRadius)
        {
            brain.StopAgentMovement();
            stuckTimer = stuckTimeout;
            brain.Animator_.SetBool("Walk", false);

            if (!brain.AttackStat_.OnCooldown && !attackPending)
            {
                brain.Animator_.SetTrigger("Attack");
                brain.PlaySFX(AIBrain.SFXEvent.Attack, 1);
                brain.StartCoroutine(brain.AttackStat_.CountdownCooldown());
                targetPosAtSwing = Target.transform.position;
                attackPending = true;
                attackTimer = attackHitDelay;
            }
        }
        else
        {
            brain.ResumeAgentMovement();
        }

        if (attackPending)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackPending = false;
                if (Target != null)
                {
                    float movedDist = Vector3.Distance(Target.transform.position, targetPosAtSwing);
                    if (movedDist <= dodgeDistance)
                    {
                        AttackEvent source = AttackEvent.From(brain.EntityStats_, Target.Player_Stats, brain.AttackStat_);
                        Target.Player_Stats.ReceiveAttack(source);
                    }
                }
            }
        }
    }

    public override void OnExitState(AIBrain brain)
    {
        brain.ResumeAgentMovement();
        brain.Agent.stoppingDistance = 0;
        brain.Animator_.SetBool("Aggresive", false);
        brain.Animator_.SetBool("Walk", false);
        attackPending = false;
        calmingDown = false;
    }

    void EnterCalmDown(AIBrain brain)
    {
        brain.Animator_.SetBool("Walk", false);
        brain.Animator_.SetBool("Aggresive", false);
        brain.StopAgentMovement();
        calmingDown = true;
        calmDownTimer = calmDownDuration;
    }
}
