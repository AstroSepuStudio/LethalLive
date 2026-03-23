using Mirror;

public abstract class AIModule : NetworkBehaviour
{
    public virtual void OnModuleInit(AIBrain brain) { }

    public virtual void OnModuleTick(AIBrain brain) { }
}
