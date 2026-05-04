using Mirror;
using UnityEngine;

public abstract class AIModule : NetworkBehaviour
{
    [SerializeField] protected bool debug = false;

    public virtual void OnModuleInit(AIBrain brain) { }

    public virtual void OnModuleTick(AIBrain brain) { }
}
