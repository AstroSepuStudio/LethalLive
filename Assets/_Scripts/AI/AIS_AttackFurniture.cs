using UnityEngine;
using UnityEngine.Events;

public class AIS_AttackFurniture : AIState
{
    [SerializeField] float movementGracePeriod = 0.2f;
    [SerializeField] float stuckTimeout = 3f;
    [SerializeField] float attackHitDelay = 0.2f;
    [SerializeField] float dodgeDistance = 0.5f;

    public FurnitureEntity Target { get; set; }
    public ItemBase ItemToPickUpAfter { get; set; }

    float graceTimer;
    float stuckTimer;
    bool attackPending;
    float attackTimer;
    Vector3 targetPosAtSwing;

    public UnityEvent OnFurnitureDestroyed;
    public UnityEvent OnFurnitureLost;

    public override void OnEnterState(AIBrain brain)
    {
        if (Target == null) { OnFurnitureLost?.Invoke(); return; }
        Debug.Log($"{brain.Prefix} is trying to attack furniture!", gameObject);
        brain.Agent.stoppingDistance = brain.AttackStat_.AttackRadius * 0.5f;
        brain.ResumeAgentMovement();
        brain.MoveAgent(Target.transform.position);
        graceTimer = movementGracePeriod;
        stuckTimer = stuckTimeout;
        attackPending = false;
    }

    public override void OnUpdateState(AIBrain brain)
    {
        if (Target == null) { OnFurnitureDestroyed?.Invoke(); return; }
        if (graceTimer > 0f) { graceTimer -= Time.deltaTime; return; }

        float dist = Vector3.Distance(brain.transform.position, Target.transform.position);

        if (dist > brain.AttackStat_.AttackRadius)
        {
            if (brain.IsAgentInMovement())
            {
                stuckTimer = stuckTimeout;
            }
            else
            {
                brain.MoveAgent(Target.transform.position);
                stuckTimer -= Time.deltaTime;
                if (stuckTimer <= 0f) { OnFurnitureLost?.Invoke(); return; }
            }
            return;
        }

        stuckTimer = stuckTimeout;
        brain.StopAgentMovement();

        Vector3 dir = (Target.transform.position - brain.transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            brain.transform.rotation = Quaternion.LookRotation(dir);

        if (!brain.AttackStat_.OnCooldown && !attackPending)
        {
            brain.PlaySFX(AIBrain.SFXEvent.Attack, 1);
            brain.Animator_.SetTrigger("Attack");
            brain.StartCoroutine(brain.AttackStat_.CountdownCooldown());
            targetPosAtSwing = Target.transform.position;
            attackPending = true;
            attackTimer = attackHitDelay;
        }

        if (attackPending)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackPending = false;
                if (Target != null && !Target.IsDying)
                {
                    float movedDist = Vector3.Distance(Target.transform.position, targetPosAtSwing);
                    if (movedDist <= dodgeDistance)
                    {
                        AttackSource source = AttackSource.From(brain.EntityStats_);
                        Target.ApplyDamage(source, brain.AttackStat_);
                    }

                    if (Target == null || Target.IsDying)
                        OnFurnitureDestroyed?.Invoke();
                }
            }
        }
    }

    public override void OnExitState(AIBrain brain)
    {
        brain.Agent.stoppingDistance = 0;
        brain.Animator_.SetBool("Walk", false);
        attackPending = false;
    }
}
