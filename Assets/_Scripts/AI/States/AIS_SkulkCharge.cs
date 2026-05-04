using UnityEngine;

public class AIS_SkulkCharge : AIState
{
    [SerializeField] Rigidbody rb;
    [SerializeField] TriggerEventCaster tec;

    [SerializeField] float windupDuration = 0.8f;
    [SerializeField] float chargeForce = 7f;
    [SerializeField] float chargeDuration = 2f;
    [SerializeField] float stunDuration = 2.5f;
    [SerializeField] LayerMask hitLayers;

    public PlayerData Target { get; set; }

    enum Phase { Windup, Charging, Stunned }
    Phase phase;
    float timer;
    Vector3 chargeDir;
    float chargeTimer;
    bool hit;

    public override void OnEnterState(AIBrain brain)
    {
        phase = Phase.Windup;
        timer = windupDuration;
        hit = false;
        chargeTimer = 0f;

        brain.StopAgentMovement();
        brain.DisableAgent();
        brain.SetIdleState(false);
        brain.Animator_.SetTrigger("Windup");

        if (Target != null)
        {
            Vector3 dir = Target.transform.position - brain.transform.position;
            dir.y = 0f;
            chargeDir = dir.normalized;
        }
    }

    public override void OnUpdateState(AIBrain brain)
    {
        switch (phase)
        {
            case Phase.Windup:
                timer -= Time.deltaTime;

                if (Target != null)
                {
                    Vector3 dir = Target.transform.position - brain.transform.position;
                    dir.y = 0f;
                    if (dir != Vector3.zero)
                        brain.transform.rotation = Quaternion.LookRotation(dir);
                    chargeDir = dir.normalized;
                }

                if (timer <= 0f)
                {
                    phase = Phase.Charging;
                    var parasite = (SkulkParasiteAI)brain;
                    parasite.Burrow(false);
                    brain.Animator_.SetTrigger("Charge");

                    tec.EnableTrigger(true);
                    brain.DisableAgent();
                    brain.EnableCollider();
                    rb.isKinematic = false;
                    rb.AddForce((chargeDir + Vector3.up * 0.3f) * chargeForce, ForceMode.Impulse);

                    brain.PlaySFX(AIBrain.SourceType.Default, AIBrain.SFXEvent.Attack, 1);
                }
                break;

            case Phase.Charging:
                chargeTimer += Time.deltaTime;

                if (chargeTimer >= chargeDuration)
                    EnterStun(brain);
                break;

            case Phase.Stunned:
                timer -= Time.deltaTime;

                if (timer <= 0f)
                {
                    tec.EnableTrigger(false);
                    rb.isKinematic = true;
                    brain.EnableAgent();
                    brain.DisableCollider();
                    ((SkulkParasiteAI)brain).ResumeWander();
                }
                break;
        }
    }

    public override void OnExitState(AIBrain brain)
    {
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        tec.EnableTrigger(false);
        brain.Agent.enabled = true;
        brain.ResumeAgentMovement();
        brain.SetIdleState(true);
        brain.DisableCollider();
    }

    public void OnTriggerDetected(Collider other, AIBrain brain)
    {
        if (hit) return;
        if (!other.TryGetComponent(out PlayerData player)) return;
        if (player == null || player.Player_Stats.dead) return;

        ((SkulkParasiteAI)brain).TriggerLatched(player);
        hit = true;
    }

    public void EnterStun(AIBrain brain)
    {
        phase = Phase.Stunned;
        timer = stunDuration;
        brain.Animator_.SetTrigger("Stunned");
    }
}
