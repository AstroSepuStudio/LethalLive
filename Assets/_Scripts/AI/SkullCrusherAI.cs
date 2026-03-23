using UnityEngine;

public class SkullCrusherAI : AIBrain, IHearingListener
{
    [Header("State Indexes")]
    [SerializeField] int[] wanderIndexes;
    [SerializeField] int investigateStateIndex = 1;

    [Header("Wander Cycling")]
    [SerializeField] int minWanderCycles = 1;
    [SerializeField] int maxWanderCycles = 4;
    [SerializeField] int minWCAfterInvest = 3;
    [SerializeField] int maxWCAfterInvest = 6;
    int targetWanCycles = 0;
    int curWanCycles = 0;
    int curWanIndex = 0;
    int totalWanCycles = 0;

    AIS_InvestigateSound investigateState;

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
        {
            investigateState = states[investigateStateIndex] as AIS_InvestigateSound;
        }
    }

    public void OnSoundHeard(AudioSoundEvent soundEvent)
    {
        if (!isServer) return;

        float dist = Vector3.Distance(transform.position, soundEvent.position);
        if (dist > soundEvent.GetRadius()) return;

        if (soundEvent.source == gameObject) return;

        TriggerInvestigate(soundEvent.position);
    }

    void TriggerInvestigate(Vector3 position)
    {
        if (investigateState == null) return;
        if (IsInInvestigateState()) return;

        investigateState.TargetPosition = position;
        SetState(states[investigateStateIndex]);
    }

    bool IsInInvestigateState() => investigateState != null && CurrentState == states[investigateStateIndex];

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

    public void InvestigateSound_ArrivedAtLocation()
    {
        curWanIndex = 1;
        curWanCycles = 0;
        targetWanCycles = Random.Range(minWCAfterInvest, maxWCAfterInvest);
        ResumeWander();
    }
}
