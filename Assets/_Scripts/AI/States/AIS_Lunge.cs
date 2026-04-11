using UnityEngine;
using UnityEngine.Events;

public class AIS_Lunge : AIState
{
    [Header("Active Window")]
    [SerializeField] float activeDuration = 15f;

    [Header("Windup")]
    [SerializeField] float minWindup = 0.3f;
    [SerializeField] float maxWindup = 1f;
    [SerializeField] float windupTurnSpeed = 6f;

    [Header("Charge")]
    [SerializeField] float chargeSpeed = 8f;
    [SerializeField] float slideRange = 10f;
    [SerializeField] float chargeTimeToSlide = 2f;
    [SerializeField] float chargeStuckTimeout = 4f;
    [SerializeField] float chargeTurnSpeed = 4f;

    [Header("Slide")]
    [SerializeField] float slideSpeed = 12f;
    [SerializeField] float minSlideDur = 0.6f;
    [SerializeField] float maxSlideDur = 1.2f;
    [SerializeField] float rayRecalcTime = 0.2f;
    [SerializeField] float slideDeceleration = 0.4f;
    [SerializeField] float huntRecalcDelay = 0.1f;
    float huntRecalcTimer = 0;

    [Header("Recovery")]
    [SerializeField] float recoveryDuration = 0.8f;

    [Header("Player Hunt")]
    [SerializeField] float playerDetectRadius = 12f;
    [SerializeField, Range(0f, 1f)] float playerHuntChance = 0.3f;

    [Header("Random Slide")]
    [SerializeField] float randomSlideRadius = 8f;

    [Header("References")]
    [SerializeField] TriggerEventCaster triggerCaster;
    [SerializeField] Rigidbody rb;

    public Vector3 TargetPosition { get; set; }
    public Transform TargetSource { get; set; }

    public UnityEvent OnLungeFinished;

    public enum Phase { Windup, Charge, Slide, Recovery }
    public Phase CurrentPhase { get; private set; }

    enum SlideType { Slide, Walk }
    SlideType currentSlideType;

    Vector3? queuedTarget = null;
    Transform queuedSource = null;
    Vector3 lastKnownLocation;

    float chargeTimer;
    float activeTimer;
    float windupDuration;
    float windupTimer;
    float stuckTimer;
    float slideTimer;
    float slideDuration;
    float recoveryTimer;
    float rayRecTimer;
    float currentSlideSpeed;
    Vector3 slideDir;

    public void RedirectCharge(Vector3 newTarget, Transform source, AIBrain brain)
    {
        activeTimer = activeDuration;
        TargetPosition = newTarget;
        TargetSource = source;
        lastKnownLocation = newTarget;
        if (CurrentPhase == Phase.Charge)
            brain.MoveAgent(TargetPosition);
    }

    public void QueueNextTarget(Vector3 target, Transform source)
    {
        queuedTarget = target;
        queuedSource = source;
        lastKnownLocation = target;
    }

    public override void OnEnterState(AIBrain brain)
    {
        activeTimer = activeDuration;
        lastKnownLocation = TargetPosition;
        queuedTarget = null;
        queuedSource = null;

        rb.isKinematic = true;
        triggerCaster?.EnableTrigger(false);

        brain.SetIdleState(false);
        brain.SetAggressive(true);

        EnterWindup(brain, playWarning: true);
    }

    public override void OnUpdateState(AIBrain brain)
    {
        activeTimer -= Time.deltaTime;

        if (TargetSource != null)
            TargetPosition = TargetSource.position;

        switch (CurrentPhase)
        {
            case Phase.Windup: UpdateWindup(brain); break;
            case Phase.Charge: UpdateCharge(brain); break;
            case Phase.Slide: UpdateSlide(brain); break;
            case Phase.Recovery: UpdateRecovery(brain); break;
        }
    }

    public override void OnExitState(AIBrain brain)
    {
        triggerCaster?.EnableTrigger(false);

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        brain.Agent.enabled = true;
        brain.ResumeAgentMovement();
        brain.ResetSpeed();
        brain.SetAggressive(false);
        brain.SetIdleState(true);

        brain.Animator_.SetBool("Bite", false);
        brain.Animator_.SetBool("Walk", false);
        brain.Animator_.SetBool("Lunge", false);
    }

    void UpdateWindup(AIBrain brain)
    {
        FaceTarget(brain, TargetPosition, windupTurnSpeed);

        windupTimer -= Time.deltaTime;
        if (windupTimer <= 0f)
            EnterCharge(brain);
    }

    void UpdateCharge(AIBrain brain)
    {
        chargeTimer += Time.deltaTime;
        if (chargeTimer > chargeTimeToSlide)
        {
            chargeTimer = 0;
            EnterSlide(brain);
            return;
        }

        FaceTarget(brain, TargetPosition, chargeTurnSpeed);

        float dist = Vector3.Distance(brain.transform.position, TargetPosition);
        rayRecTimer -= Time.deltaTime;
        if (dist <= slideRange && rayRecTimer <= 0f)
        {
            rayRecTimer = rayRecalcTime;
            Vector3 origin = brain.transform.position + Vector3.up;
            Vector3 targetPos = TargetPosition + Vector3.up;
            Vector3 toTarget = targetPos - origin;
            if (!Physics.Raycast(origin, toTarget.normalized, toTarget.magnitude, brain.losBlockingLayers))
            {
                EnterSlide(brain);
                return;
            }
        }

        if (!brain.IsAgentInMovement())
        {
            stuckTimer -= Time.deltaTime;
            if (stuckTimer <= 0f) { EnterRecovery(brain); return; }
            brain.MoveAgent(TargetPosition);
        }
        else
        {
            stuckTimer = chargeStuckTimeout;
        }

        brain.Animator_.SetBool("Walk", brain.IsAgentInMovement());
    }

    void UpdateSlide(AIBrain brain)
    {
        slideTimer -= Time.deltaTime;

        if (currentSlideType == SlideType.Walk)
        {
            huntRecalcTimer -= Time.deltaTime;
            if (huntRecalcTimer < 0) 
            { 
                brain.MoveAgent(TargetSource.position);
                huntRecalcTimer = huntRecalcDelay;
            }

            brain.Animator_.SetBool("Walk", brain.IsAgentInMovement());
            FaceTarget(brain, TargetPosition, chargeTurnSpeed);
        }
        else
        {
            float t = 1f - Mathf.Clamp01(slideTimer / slideDuration);
            float speed = Mathf.Lerp(currentSlideSpeed, currentSlideSpeed * slideDeceleration, t);
            rb.linearVelocity = slideDir * speed;
            brain.transform.rotation = Quaternion.LookRotation(slideDir);
        }

        if (slideTimer <= 0f)
            EnterRecovery(brain);
    }

    void UpdateRecovery(AIBrain brain)
    {
        recoveryTimer -= Time.deltaTime;
        if (recoveryTimer > 0f) return;

        if (activeTimer <= 0f) { OnLungeFinished?.Invoke(); return; }

        if (queuedTarget.HasValue)
        {
            TargetPosition = queuedTarget.Value;
            TargetSource = queuedSource;
            lastKnownLocation = TargetPosition;
            queuedTarget = null;
            queuedSource = null;
        }
        else
        {
            TargetSource = null;
            TargetPosition = GetRandomNearLocation();
        }

        EnterWindup(brain, playWarning: false);
    }

    void EnterWindup(AIBrain brain, bool playWarning)
    {
        CurrentPhase = Phase.Windup;
        windupDuration = Random.Range(minWindup, maxWindup);
        windupTimer = windupDuration;

        brain.StopAgentMovement();
        brain.Animator_.SetBool("Bite", false);
        brain.Animator_.SetBool("Walk", false);
        brain.Animator_.SetBool("Lunge", false);

        if (playWarning)
            brain.PlaySFX(AIBrain.SourceType.Default, AIBrain.SFXEvent.Warning, 1f);
    }

    void EnterCharge(AIBrain brain)
    {
        CurrentPhase = Phase.Charge;
        stuckTimer = chargeStuckTimeout;
        chargeTimer = 0f;
        rayRecTimer = 0f;

        brain.Agent.enabled = true;
        brain.ResumeAgentMovement();
        brain.Agent.speed = chargeSpeed;
        brain.MoveAgent(TargetPosition);

        brain.Animator_.SetBool("Bite", false);
        brain.Animator_.SetBool("Walk", true);
        brain.Animator_.SetBool("Lunge", false);
    }

    void EnterSlide(AIBrain brain)
    {
        CurrentPhase = Phase.Slide;
        slideDuration = Random.Range(minSlideDur, maxSlideDur);
        slideTimer = slideDuration;
        currentSlideSpeed = slideSpeed;

        slideDir = (TargetPosition - brain.transform.position).normalized;
        slideDir.y = 0f;
        if (slideDir == Vector3.zero) slideDir = brain.transform.forward;

        TargetSource = null;

        triggerCaster?.EnableTrigger(true);
        brain.PlaySFX(AIBrain.SourceType.Default, AIBrain.SFXEvent.Attack, 1f);

        if (Random.value < playerHuntChance)
        {
            PlayerData player = FindNearestPlayer(brain);
            if (player != null)
            {
                TargetSource = player.transform;
                TargetPosition = player.transform.position;
                lastKnownLocation = TargetPosition;
                currentSlideType = SlideType.Walk;
            }
            else currentSlideType = SlideType.Slide;
        }
        else currentSlideType = SlideType.Slide;

        if (currentSlideType == SlideType.Walk)
        {
            brain.Agent.enabled = true;
            rb.isKinematic = true;
            brain.ResumeAgentMovement();
            brain.Agent.speed = slideSpeed;
            brain.MoveAgent(TargetPosition);

            brain.transform.rotation = Quaternion.LookRotation(slideDir);
            brain.Animator_.SetBool("Lunge", false);
            brain.Animator_.SetBool("Walk", true);
            brain.Animator_.SetBool("Bite", false);
        }
        else
        {
            brain.Agent.enabled = false;
            rb.isKinematic = false;

            brain.transform.rotation = Quaternion.LookRotation(slideDir);
            brain.Animator_.SetBool("Bite", false);
            brain.Animator_.SetBool("Lunge", true);
            brain.Animator_.SetBool("Walk", false);
        }
    }

    void EnterRecovery(AIBrain brain)
    {
        CurrentPhase = Phase.Recovery;
        recoveryTimer = recoveryDuration;

        triggerCaster?.EnableTrigger(false);

        if (currentSlideType == SlideType.Slide)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        brain.Agent.enabled = true;
        brain.StopAgentMovement();
        brain.ResetSpeed();

        brain.Animator_.SetBool("Bite", false);
        brain.Animator_.SetBool("Lunge", false);
        brain.Animator_.SetBool("Walk", false);
    }

    static void FaceTarget(AIBrain brain, Vector3 target, float speed)
    {
        Vector3 dir = target - brain.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        brain.transform.rotation = Quaternion.Slerp(
            brain.transform.rotation,
            Quaternion.LookRotation(dir.normalized),
            speed * Time.deltaTime);
    }

    PlayerData FindNearestPlayer(AIBrain brain)
    {
        Collider[] hits = Physics.OverlapSphere(brain.transform.position, playerDetectRadius);
        PlayerData closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            if (!hit.TryGetComponent<PlayerData>(out var p)) continue;
            float d = Vector3.Distance(brain.transform.position, p.transform.position);
            if (d < closestDist) { closestDist = d; closest = p; }
        }

        return closest;
    }

    Vector3 GetRandomNearLocation()
    {
        Vector2 circle = Random.insideUnitCircle.normalized
                       * Random.Range(randomSlideRadius * 0.3f, randomSlideRadius);
        return lastKnownLocation + new Vector3(circle.x, 0f, circle.y);
    }
}
