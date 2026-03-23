using UnityEngine;

public abstract class AIState : MonoBehaviour
{
    public abstract void OnEnterState(AIBrain brain);
    public abstract void OnUpdateState(AIBrain brain);
    public abstract void OnExitState(AIBrain brain);
}
