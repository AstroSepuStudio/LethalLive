using UnityEngine;

public class AIS_SkulkCeilingWait : AIState
{
    [SerializeField] float detectionRadius = 3f;
    [SerializeField] float dropCheckInterval = 0.4f;
    [SerializeField] float maxWaitDuration = 40f;

    public Vector3 CeilingPoint { get; set; }

    float waitTimer;
    float dropCheckTimer;

    public override void OnEnterState(AIBrain brain)
    {
        var parasite = (SkulkParasiteAI)brain;
        parasite.Burrow(false);
        parasite.EnableFin(false);

        brain.StopAgentMovement();
        brain.DisableAgent();
        brain.SetIdleState(false);

        brain.Animator_.transform.position = CeilingPoint;
        brain.Animator_.SetTrigger("CeilingAttach");

        waitTimer = maxWaitDuration;
        dropCheckTimer = 0f;
    }

    public override void OnUpdateState(AIBrain brain)
    {
        waitTimer -= Time.deltaTime;
        dropCheckTimer -= Time.deltaTime;

        if (waitTimer <= 0f)
        {
            ((SkulkParasiteAI)brain).ResumeWander();
            return;
        }

        if (dropCheckTimer > 0f) return;
        dropCheckTimer = dropCheckInterval;

        Collider[] hits = Physics.OverlapSphere(brain.transform.position, detectionRadius);
        foreach (var col in hits)
        {
            if (!col.TryGetComponent(out PlayerData player)) continue;
            if (player.Player_Stats.dead) continue;

            ((SkulkParasiteAI)brain).TriggerCharge(player);
            return;
        }
    }

    public override void OnExitState(AIBrain brain)
    {
        brain.Animator_.transform.localPosition = Vector3.zero;
        brain.Agent.enabled = true;
        brain.ResumeAgentMovement();
        brain.SetIdleState(true);
        brain.Animator_.SetTrigger("CeilingDetach");
    }
}
