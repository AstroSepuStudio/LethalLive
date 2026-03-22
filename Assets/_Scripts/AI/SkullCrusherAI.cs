using UnityEngine;

public class SkullCrusherAI : AIBrain, IHearingListener
{
    [Header("State Indexes")]
    [SerializeField] int[] wanderIndexes;

    [Header("Wander Cycling")]
    [SerializeField] int minWanderCycles = 1;
    [SerializeField] int maxWanderCycles = 4;
    int targetWanCycles = 0;
    int curWanCycles = 0;
    int curWanIndex = 0;
    int totalWanCycles = 0;

    protected override void Start()
    {
        base.Start();

        if (isServer)
        {
            targetWanCycles = Random.Range(minWanderCycles, maxWanderCycles);
            HearingEventBroadcaster.Instance.AddListener(this);
        }
    }

    private void OnDestroy()
    {
        if (isServer) HearingEventBroadcaster.Instance.RemoveListener(this);
    }

    public void OnSoundHeard(AudioSoundEvent soundEvent)
    {

    }

    bool IsInWanderState()
    {
        if (CurrentState == null) return false;

        if (wanderIndexes == null) return false;
        foreach (int idx in wanderIndexes)
            if (idx < states.Length && CurrentState == states[idx]) return true;
        return false;
    }

    void ResumeWander()
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
}
