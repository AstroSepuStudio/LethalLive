using UnityEngine;

public class SkullCrusherAI : AIBrain, IHearingListener
{
    [Header("State Indexes")]
    [SerializeField] int wanderStateIndex = 0;
    [SerializeField] int investigateStateIndex = 1;
    [SerializeField] int lungeStateIndex = 2;

    [Header("Patience")]
    [SerializeField] int maxPatience = 2;
    [SerializeField] float patienceRestoreTime = 30f;
    int patience = 0;
    float patienceTimer = 0;

    readonly System.Collections.Generic.Dictionary<SoundLoudness, float> lungeThreshold = new()
    {
        { SoundLoudness.Quiet,   2.5f  },
        { SoundLoudness.Average, 7.5f },
        { SoundLoudness.Loud,    15f },
        { SoundLoudness.Global,  100f }
    };

    readonly System.Collections.Generic.Dictionary<SoundLoudness, float> invScalateThreshold = new()
    {
        { SoundLoudness.Quiet,   3.5f  },
        { SoundLoudness.Average, 11f },
        { SoundLoudness.Loud,    24f },
        { SoundLoudness.Global,  50f }
    };

    AIS_InvestigateSound investigateState;
    AIS_Lunge lungeState;

    protected override void Start()
    {
        base.Start();

        if (!isServer) return;

        SetStates();
        HearingEventBroadcaster.Instance.AddListener(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (isServer) HearingEventBroadcaster.Instance.RemoveListener(this);
    }

    protected override void OnTick()
    {
        base.OnTick();

        if (patienceTimer > 0) patienceTimer -= GameTick.TickRate;
        if (patienceTimer <= 0) patience = 0;
    }

    void SetStates()
    {
        if (states == null) return;

        if (investigateStateIndex < states.Length)
            investigateState = states[investigateStateIndex] as AIS_InvestigateSound;

        if (lungeStateIndex < states.Length)
            lungeState = states[lungeStateIndex] as AIS_Lunge;
    }

    public void OnSoundHeard(AudioSoundEvent soundEvent)
    {
        if (!isServer) return;
        float dist = Vector3.Distance(transform.position, soundEvent.position);
        if (soundEvent.source == gameObject) return;
        if (soundEvent.source.CompareTag("SkullCrusher")) return;

        lungeThreshold.TryGetValue(soundEvent.category, out float threshold);
        Transform source = soundEvent.source != null ? soundEvent.source.transform : null;

        if (dist <= threshold || IsInLungeState())
        {
            if (patience >= maxPatience)
                HandleLungeSound(soundEvent.position, source);
            patience++;
            patienceTimer = patienceRestoreTime;
        }
        else
        {
            invScalateThreshold.TryGetValue(soundEvent.category, out threshold);
            HandleInvestigateSound(soundEvent.position, source, dist <= threshold);
        }
    }

    void HandleLungeSound(Vector3 position, Transform source)
    {
        if (!IsInLungeState())
        {
            TriggerLunge(position, source);
            return;
        }

        switch (lungeState.CurrentPhase)
        {
            case AIS_Lunge.Phase.Windup:
            case AIS_Lunge.Phase.Charge:
                lungeState.RedirectCharge(position, source, this);
                break;

            case AIS_Lunge.Phase.Slide:
            case AIS_Lunge.Phase.Recovery:
                lungeState.QueueNextTarget(position, source);
                break;
        }
    }

    void HandleInvestigateSound(Vector3 position, Transform source, bool escalateRange)
    {
        if (IsInInvestigateState())
            investigateState.RedirectAttention(position, source, this, escalateRange);
        else
            TriggerInvestigate(position);
    }

    void TriggerInvestigate(Vector3 position)
    {
        if (investigateState == null) return;
        if (IsInInvestigateState()) return;

        investigateState.TargetPosition = position;
        SetState(states[investigateStateIndex]);
    }

    void TriggerLunge(Vector3 position, Transform source)
    {
        if (lungeState == null) return;

        lungeState.TargetPosition = position;
        lungeState.TargetSource = source;

        SetState(states[lungeStateIndex]);
    }

    bool IsInInvestigateState() => investigateState != null && CurrentState == states[investigateStateIndex];
    bool IsInLungeState() => lungeState != null && CurrentState == states[lungeStateIndex];

    public void ResumeWander() => SetState(states[wanderStateIndex]);

    public void OnInvestigationCompleted() => ResumeWander();

    public void OnInvestigationEscalated(Transform source)
    {
        if (investigateState == null) return;
        TriggerLunge(investigateState.TargetPosition, source);
    }

    public void OnLungeFinished() => ResumeWander();

    public void OnColliderDetected(Collider collider)
    {
        if (collider == null || collider.gameObject == gameObject) return;
        if (collider.gameObject.CompareTag("SkullCrusher")) return;
        if (!collider.TryGetComponent(out EntityStats target)) return;

        animator.SetBool("Bite", true);

        AttackEvent attack = AttackEvent.From(entityStats, target, attackStat);
        target.ReceiveAttack(attack);
        Debug.Log("Attacking");
    }

    public override void OnAgentHurt(AttackEvent source)
    {
        if (!IsInLungeState())
            TriggerLunge(source.Position, source.SourceStats.transform);
    }

    public override void OnAgentDeath(AttackEvent source)
    {
        base.OnAgentDeath(source);
        isDying = true;
    }
}
