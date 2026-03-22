using Mirror;

public abstract class ItemAction : NetworkBehaviour
{
    protected ItemBase item;

    public ItemBase Item => item;

    public virtual void Initialize(ItemBase owner)
    {
        item = owner;
    }

    public virtual bool IsOnCooldown() => false;
    public virtual void EnterOnCooldown() { }

    public abstract void Execute();
    public abstract void Cancel();

    public virtual void OnAnimationTrigger() { }
    public virtual void OnAnimationFinish() { }
}
