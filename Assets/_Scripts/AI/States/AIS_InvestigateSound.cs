using UnityEngine;
using UnityEngine.Events;

public class AIS_InvestigateSound : AIState
{
    [Header("Arrival")]
    [SerializeField] float arrivalThreshold = 1.5f;

    [Header("Roam After Arrival")]
    [SerializeField] float roamDuration = 10f;
    [SerializeField] float roamRadius = 5f;
    [SerializeField] float minRoamSleep = 1.5f;
    [SerializeField] float maxRoamSleep = 3.5f;

    [Header("Alert / Escalation")]
    [SerializeField] float alertCooldown = 2f;
    [SerializeField] int alertsBeforeLunge = 3;

    public Vector3 TargetPosition { get; set; }

    public UnityEvent CompletedInvestigation;
    public UnityEvent<Transform> EscalatedToLunge;

    enum Phase { Moving, Roaming }
    Phase phase;

    bool roamMoving;
    bool roamDestinationSet;
    float roamTimer;
    float roamSleepTimer;
    Vector3 roamOrigin;
    Vector3 roamDestination;

    float alertCooldownTimer;
    int alertCount;

    public override void OnEnterState(AIBrain brain)
    {
        alertCooldownTimer = 0f;
        alertCount = 0;

        PlayAlert(brain);
        BeginMovingTo(TargetPosition, brain);
    }

    public override void OnExitState(AIBrain brain)
    {
        brain.ResumeAgentMovement();
        brain.Animator_.SetBool("Walk", false);
        brain.SetIdleState(true);
    }

    public override void OnUpdateState(AIBrain brain)
    {
        alertCooldownTimer -= Time.deltaTime;

        switch (phase)
        {
            case Phase.Moving: UpdateMoving(brain); break;
            case Phase.Roaming: UpdateRoaming(brain); break;
        }
    }

    public void RedirectAttention(Vector3 position, Transform source, AIBrain brain, bool scalateRange)
    {
        PlayAlert(brain);

        if (alertCount >= alertsBeforeLunge && scalateRange)
        {
            EscalatedToLunge?.Invoke(source);
            return;
        }

        BeginMovingTo(position, brain);
    }

    void PlayAlert(AIBrain brain)
    {
        if (alertCooldownTimer > 0f) return;

        brain.PlaySFX(AIBrain.SourceType.Default, AIBrain.SFXEvent.Alert, 1f);
        alertCooldownTimer = alertCooldown;
        alertCount++;
    }

    void BeginMovingTo(Vector3 position, AIBrain brain)
    {
        TargetPosition = position;
        phase = Phase.Moving;
        roamMoving = false;
        roamDestinationSet = false;

        brain.ResumeAgentMovement();
        brain.MoveAgent(TargetPosition);
        brain.Animator_.SetBool("Walk", true);
        brain.SetIdleState(false);
    }

    void UpdateMoving(AIBrain brain)
    {
        float dist = Vector3.Distance(brain.transform.position, TargetPosition);
        if (dist > arrivalThreshold)
        {
            brain.Animator_.SetBool("Walk", brain.IsAgentInMovement());
            return;
        }

        roamOrigin = brain.transform.position;
        roamTimer = roamDuration;
        roamSleepTimer = 0f;
        roamMoving = false;
        roamDestinationSet = false;
        phase = Phase.Roaming;

        brain.Animator_.SetBool("Walk", false);
    }

    void UpdateRoaming(AIBrain brain)
    {
        roamTimer -= Time.deltaTime;
        if (roamTimer <= 0f)
        {
            brain.StopAgentMovement();
            brain.Animator_.SetBool("Walk", false);
            CompletedInvestigation?.Invoke();
            return;
        }

        if (roamMoving && roamDestinationSet)
        {
            float distToDest = Vector3.Distance(brain.transform.position, roamDestination);
            if (!brain.IsAgentInMovement() && distToDest <= arrivalThreshold)
                roamMoving = false;
        }

        brain.Animator_.SetBool("Walk", brain.IsAgentInMovement());

        if (!roamMoving)
        {
            roamSleepTimer -= Time.deltaTime;
            if (roamSleepTimer <= 0f)
            {
                Vector2 circle = Random.insideUnitCircle * roamRadius;
                roamDestination = roamOrigin + new Vector3(circle.x, 0f, circle.y);
                roamDestinationSet = true;

                brain.ResumeAgentMovement();
                brain.MoveAgent(roamDestination);
                roamSleepTimer = Random.Range(minRoamSleep, maxRoamSleep);
                roamMoving = true;
            }
        }
    }
}
