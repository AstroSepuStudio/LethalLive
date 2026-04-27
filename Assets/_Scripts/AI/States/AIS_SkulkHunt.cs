using UnityEngine;

public class AIS_SkulkHunt : AIState
{
    [SerializeField] float recalculateInterval = 0.3f;
    [SerializeField] float chargeDistance = 4f;
    [SerializeField] float loseTargetDistance = 25f;

    public PlayerData Target { get; set; }

    float recalcTimer;

    public override void OnEnterState(AIBrain brain)
    {
        var parasite = (SkulkParasiteAI)brain;
        parasite.EnableFin(true);
        brain.ResumeAgentMovement();
        brain.SetIdleState(false);
        recalcTimer = 0f;

        brain.PlaySFX(AIBrain.SourceType.Default, AIBrain.SFXEvent.Alert, 1);
    }

    public override void OnUpdateState(AIBrain brain)
    {
        if (Target == null || Target.Player_Stats.dead)
        {
            ((SkulkParasiteAI)brain).ResumeWander();
            return;
        }

        float dist = Vector3.Distance(brain.transform.position, Target.transform.position);

        if (dist > loseTargetDistance)
        {
            ((SkulkParasiteAI)brain).ResumeWander();
            return;
        }

        if (dist <= chargeDistance)
        {
            ((SkulkParasiteAI)brain).TriggerCharge(Target);
            return;
        }

        recalcTimer -= Time.deltaTime;
        if (recalcTimer <= 0f)
        {
            recalcTimer = recalculateInterval;
            brain.MoveAgent(Target.transform.position);
        }

        brain.Animator_.SetBool("Walk", brain.IsAgentInMovement());
    }

    public override void OnExitState(AIBrain brain)
    {
        brain.Animator_.SetBool("Walk", false);
        brain.SetIdleState(true);
    }
}
