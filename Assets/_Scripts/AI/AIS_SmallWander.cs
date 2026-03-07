using UnityEngine;
using UnityEngine.Events;

public class AIS_SmallWander : AIState
{
    [SerializeField] float minDistance = 3f;
    [SerializeField] float maxDistance = 10f;

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
        brain.Animator_.SetBool("Walk", false);
    }

    public override void OnUpdateState(AIBrain brain)
    {
        bool mov = brain.IsAgentInMovement();

        brain.Animator_.SetBool("Walk", mov);
        if (mov) return;

        if (moving)
        {
            moving = false;
            OnWanderCompleted?.Invoke();
        }

        sleepTimer -= Time.deltaTime;

        if (sleepTimer <= 0)
            MoveAgent(brain);
    }

    private Vector3 GetRandomPositionInCircle(Vector3 origin, float radius, float minRadius = 0f)
    {
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(minRadius, radius);
        return origin + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }

    private void MoveAgent(AIBrain brain)
    {
        if (sleepTimer > 0) return;

        brain.MoveAgent(GetRandomPositionInCircle(transform.position, maxDistance, minDistance));
        sleepTimer = UnityEngine.Random.Range(minSleep, maxSleep);

        moving = true;
        OnWanderStart?.Invoke();
    }
}
