using UnityEngine;
using UnityEngine.Events;

public class AIS_AttackFurniture : AIState
{
    [SerializeField] float movementGracePeriod = 0.2f;
    [SerializeField] float stuckTimeout = 3f;

    public FurnitureEntity Target { get; set; }
    public ItemBase ItemToPickUpAfter { get; set; }

    float graceTimer;
    float stuckTimer;

    public UnityEvent OnFurnitureDestroyed;
    public UnityEvent OnFurnitureLost;

    public override void OnEnterState(AIBrain brain)
    {
        if (Target == null) { OnFurnitureLost?.Invoke(); return; }
        Debug.Log($"{brain.Prefix} is trying to attack furniture!");
        brain.ResumeAgentMovement();
        brain.MoveAgent(Target.transform.position);
        graceTimer = movementGracePeriod;
        stuckTimer = stuckTimeout;
    }

    public override void OnUpdateState(AIBrain brain)
    {
        if (Target == null) { OnFurnitureDestroyed?.Invoke(); return; }

        if (graceTimer > 0f) { graceTimer -= Time.deltaTime; return; }

        if (brain.IsAgentInMovement())
        {
            stuckTimer = stuckTimeout;
            return;
        }

        stuckTimer -= Time.deltaTime;
        if (stuckTimer <= 0f) { OnFurnitureLost?.Invoke(); return; }

        float dist = Vector3.Distance(brain.transform.position, Target.transform.position);
        if (dist > brain.AttackStat_.AttackRadius)
        {
            brain.MoveAgent(Target.transform.position);
            return;
        }

        Vector3 dir = (Target.transform.position - brain.transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            brain.transform.rotation = Quaternion.LookRotation(dir);

        if (!brain.AttackStat_.OnCooldown)
        {
            Debug.Log($"{brain.Prefix} is attacking furniture!");
            brain.PlaySFX(AIBrain.SFXEvent.Attack);

            AttackSource source = AttackSource.From(brain.EntityStats_);
            Target.ApplyDamage(source, brain.AttackStat_);
            brain.StartCoroutine(brain.AttackStat_.CountdownCooldown());
            brain.Animator_.SetTrigger("Attack");

            if (Target == null || Target.IsDying)
                OnFurnitureDestroyed?.Invoke();
        }
    }

    public override void OnExitState(AIBrain brain)
    {
        brain.Animator_.SetBool("Walk", false);
    }
}
