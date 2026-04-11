using System.Collections.Generic;
using UnityEngine;

public class SkullCrusherAI : AIBrain, IHearingListener
{
    [Header("State Indexes")]
    [SerializeField] int[] wanderIndexes;
    [SerializeField] int investigateStateIndex = 1;
    [SerializeField] int lungeStateIndex = 2;

    [Header("Wander Cycling")]
    [SerializeField] int minWanderCycles = 1;
    [SerializeField] int maxWanderCycles = 4;
    [SerializeField] int minWCAfterInvest = 3;
    [SerializeField] int maxWCAfterInvest = 6;
    int targetWanCycles = 0;
    int curWanCycles = 0;
    int curWanIndex = 0;
    int totalWanCycles = 0;

    readonly Dictionary<SoundLoudness, float> lungeThreshold = new()
    {
        { SoundLoudness.Quiet,   2.5f  },
        { SoundLoudness.Average, 7.5f },
        { SoundLoudness.Loud,    15f },
        { SoundLoudness.Global,  100f }
    };

    AIS_InvestigateSound investigateState;
    AIS_Lunge lungeState;

    protected override void Start()
    {
        base.Start();

        if (!isServer) return;

        targetWanCycles = Random.Range(minWanderCycles, maxWanderCycles);
        SetStates();
        HearingEventBroadcaster.Instance.AddListener(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (isServer) HearingEventBroadcaster.Instance.RemoveListener(this);
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
            HandleLungeSound(soundEvent.position, source);
        else
            HandleInvestigateSound(soundEvent.position, source);
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

    void HandleInvestigateSound(Vector3 position, Transform source)
    {
        if (IsInInvestigateState())
            investigateState.RedirectAttention(position, source, this);
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

    bool IsInWanderState()
    {
        if (CurrentState == null) return false;

        if (wanderIndexes == null) return false;
        foreach (int idx in wanderIndexes)
            if (idx < states.Length && CurrentState == states[idx]) return true;
        return false;
    }

    public void ResumeWander()
    {
        if (wanderIndexes == null || wanderIndexes.Length == 0) return;
        SetState(states[wanderIndexes[curWanIndex]]);
    }

    public void OnWanderCompleted()
    {
        curWanCycles++;
        totalWanCycles++;

        if (curWanCycles < targetWanCycles) return;

        curWanCycles = 0;
        targetWanCycles = Random.Range(minWanderCycles, maxWanderCycles);
        curWanIndex = (curWanIndex + 1) % wanderIndexes.Length;

        SetState(states[wanderIndexes[curWanIndex]]);
    }

    public void OnInvestigationCompleted()
    {
        curWanIndex = 1;
        curWanCycles = 0;
        targetWanCycles = Random.Range(minWCAfterInvest, maxWCAfterInvest);
        ResumeWander();
    }

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
}
