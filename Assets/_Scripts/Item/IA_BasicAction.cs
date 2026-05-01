using UnityEngine.Events;

public class IA_BasicAction : ItemAction
{
    public UnityEvent OnInitializeEv;
    public UnityEvent OnEnterCooldownEv;
    public UnityEvent OnCancelEv;
    public UnityEvent OnExecuteEv;
    public UnityEvent OnAnimationTriggerEv;
    public UnityEvent OnAnimationFinishEv;

    public override void Initialize(ItemBase owner)
    {
        base.Initialize(owner);
        OnInitializeEv?.Invoke();
    }

    public override void EnterOnCooldown()
    {
        OnEnterCooldownEv?.Invoke();
    }

    public override void Cancel()
    {
        OnCancelEv?.Invoke();
    }

    public override void Execute()
    {
        OnExecuteEv?.Invoke();
    }

    public override void OnAnimationTrigger()
    {
        OnAnimationTriggerEv?.Invoke();
    }

    public override void OnAnimationFinish()
    {
        OnAnimationFinishEv?.Invoke();
    }
}
