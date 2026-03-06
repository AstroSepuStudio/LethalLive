using UnityEngine;
using UnityEngine.Events;

public class AIS_WanderAroundRoom : AIState
{
    [SerializeField] float minSleep = 3f;
    [SerializeField] float maxSleep = 10f;

    float sleepTimer = 0;

    public UnityEvent OnWanderStart;
    public UnityEvent OnWanderCompleted;

    bool moving = false;

    public override void OnEnterState(AIBrain brain)
    {
        MoveAgent(brain);
    }

    public override void OnExitState(AIBrain brain)
    {

    }

    public override void OnUpdateState(AIBrain brain)
    {
        if (brain.IsAgentInMovement()) return;

        if (moving)
        {
            moving = false;
            OnWanderCompleted?.Invoke();
        }

        sleepTimer = sleepTimer > 0 ? sleepTimer - Time.deltaTime : 0;

        if (sleepTimer < 0)
            MoveAgent(brain);
    }

    private Vector3 GetRandomRoomPosition(Vector3 origin)
    {
        var gen = DungeonGenerator.Instance;
        if (gen == null) return origin;
        
        RoomData rd = gen.GetRoomDataAtPosition(transform.position);
        if (rd == null) return origin;

        return rd.GetRandomPositionInRoom();
    }

    private void MoveAgent(AIBrain brain)
    {
        if (sleepTimer > 0) return;

        brain.MoveAgent(GetRandomRoomPosition(transform.position));
        sleepTimer = Random.Range(minSleep, maxSleep);

        moving = true;
        OnWanderStart?.Invoke();
    }
}
