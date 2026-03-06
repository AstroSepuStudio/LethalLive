using UnityEngine;

public class VortexAI : AIBrain
{
    [SerializeField] int[] wanderIndexes;

    [SerializeField] int minWanderCycles = 1;
    [SerializeField] int maxWanderCycles = 4;

    int targetWanCycles = 0;
    int curWanCycles = 0;
    int curWanIndex = 0;

    protected override void Start()
    {
        base.Start();

        targetWanCycles = Random.Range(minWanderCycles, maxWanderCycles);
    }

    public void OnWanderCompleted()
    {
        curWanCycles++;
        if (curWanCycles < targetWanCycles) return;

        curWanCycles = 0;
        targetWanCycles = Random.Range(minWanderCycles, maxWanderCycles);

        curWanIndex++;
        curWanIndex = curWanIndex >= wanderIndexes.Length ? 0 : curWanIndex;

        SetState(states[curWanIndex]);
    }
}
