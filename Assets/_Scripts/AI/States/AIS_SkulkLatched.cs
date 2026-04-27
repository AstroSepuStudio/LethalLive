using UnityEngine;

public class AIS_SkulkLatched : AIState
{
    [SerializeField] float attachOffset = 0.5f;
    [SerializeField] float minLatchDuration = 10f;
    [SerializeField] float maxLatchDuration = 15f;
    [SerializeField] int hitsToDisengage = 2;

    [SerializeField] AttackStat latchAttack;

    public PlayerData Target { get; set; }
    public bool OnCooldown => latchAttack.OnCooldown;

    SkulkParasiteAI brain;
    float latchTimer;
    int hitsReceived;

    public override void OnEnterState(AIBrain brain)
    {
        this.brain = (SkulkParasiteAI)brain;

        latchTimer = Random.Range(minLatchDuration, maxLatchDuration);
        hitsReceived = 0;

        AttackEvent attackEvent = AttackEvent.From(brain.EntityStats_, Target.Player_Stats, latchAttack);
        Target.Player_Stats.ReceiveAttack(attackEvent);

        brain.EnableCollider();
        brain.EnableColliderTrigger(true);
        brain.StopAgentMovement();
        brain.DisableAgent();
        brain.SetIdleState(false);
        brain.Animator_.SetTrigger("Latch");
    }

    public override void OnUpdateState(AIBrain brain)
    {
        if (Target == null || Target.Player_Stats.dead)
        {
            ((SkulkParasiteAI)brain).TriggerDisengage();
            return;
        }

        brain.transform.position = Target.transform.position + Vector3.up * 0.5f + brain.transform.forward * -attachOffset;

        latchTimer -= Time.deltaTime;

        if (!brain.AttackStat_.OnCooldown)
        {
            StartCoroutine(brain.AttackStat_.CountdownCooldown());
            AttackEvent atk = AttackEvent.From(brain.EntityStats_, Target.Player_Stats, brain.AttackStat_);
            Target.Player_Stats.ReceiveAttack(atk);
        }

        if (latchTimer <= 0f)
            ((SkulkParasiteAI)brain).TriggerDisengage();
    }

    public override void OnExitState(AIBrain brain)
    {
        brain.DisableCollider();
        brain.EnableColliderTrigger(false);
        brain.Agent.enabled = true;
        brain.ResumeAgentMovement();
        brain.SetIdleState(true);
        brain.Animator_.SetTrigger("Detach");
    }

    public void OnParasiteHurt()
    {
        hitsReceived++;
        if (hitsReceived >= hitsToDisengage)
            brain.TriggerDisengage();
    }
}
