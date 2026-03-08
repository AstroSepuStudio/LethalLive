using UnityEngine;
using UnityEngine.Events;

public class AIS_WanderAroundHome : AIState
{
    [SerializeField] float minSleep = 3f;
    [SerializeField] float maxSleep = 8f;
    [SerializeField] int maxWandersBeforeLeaving = 2;
    int wanderCount;

    float sleepTimer;
    bool moving;

    public UnityEvent OnWanderStart;
    public UnityEvent OnWanderCompleted;
    public UnityEvent OnHomeVisitComplete;

    public override void OnEnterState(AIBrain brain)
    {
        wanderCount = 0;
        moving = false;
        sleepTimer = 0f;
        MoveToRandomHomePosition(brain);
    }

    public override void OnUpdateState(AIBrain brain)
    {
        bool mov = brain.IsAgentInMovement();

        brain.Animator_.SetBool("Walk", mov);
        if (mov) return;

        if (moving)
        {
            moving = false;
            wanderCount++;
            OnWanderCompleted?.Invoke();

            VortexAI vortex = brain as VortexAI;
            bool isAlpha = vortex != null && vortex.IsActingAsAlpha;

            if (!isAlpha && wanderCount >= maxWandersBeforeLeaving)
                OnHomeVisitComplete?.Invoke();
        }

        sleepTimer -= Time.deltaTime;
        if (sleepTimer <= 0f)
        {
            VortexAI vortex = brain as VortexAI;
            if (vortex.CarriedItem != null)
            {
                vortex.TriggerDropAtHome();
                return;
            }

            MoveToRandomHomePosition(brain);
        }
    }

    public override void OnExitState(AIBrain brain)
    {
        brain.Animator_.SetBool("Walk", false);
    }

    void MoveToRandomHomePosition(AIBrain brain)
    {
        VortexAI vortex = brain as VortexAI;
        RoomData home = vortex?.HomeRoom;
        if (home == null) return;

        brain.MoveAgent(home.GetRandomPositionInRoom());
        brain.Animator_.SetBool("Walk", true);
        sleepTimer = Random.Range(minSleep, maxSleep);
        moving = true;
        OnWanderStart?.Invoke();
    }
}
