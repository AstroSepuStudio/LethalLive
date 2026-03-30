using UnityEngine;

public class IA_AnimationTrigger : ItemAction
{
    [SerializeField] protected AnimationTrigger animationTrigger;
    [SerializeField] protected bool forceAim = true;

    public override void Cancel() 
    {
        TryPlaySFX(animationTrigger, ActionTiming.Cancel);
    }

    public override void OnAnimationTrigger()
    {
        TryPlaySFX(animationTrigger, ActionTiming.Trigger);
    }

    public override void Execute()
    {
        if (isServer) item.InUse = true;

        ItemActionType type = item.GetActionType(this);
        if (type == ItemActionType.None) return;

        if (forceAim) item.PData.Camera_Movement.ForcePlayerToAim();

        if (type == ItemActionType.Primary)
            item.AnimationModule.PlayPrimary(this, item.PData, animationTrigger.Trigger);
        else if (type == ItemActionType.Secondary)
            item.AnimationModule.PlaySecondary(this, item.PData, animationTrigger.Trigger);

        TryPlaySFX(animationTrigger, ActionTiming.Start);
    }

    public override void OnAnimationFinish()
    {
        if (isServer) item.InUse = false;
        if (forceAim) item.PData.Camera_Movement.StopForcePlayerToAim();

        TryPlaySFX(animationTrigger, ActionTiming.Finish);
    }
}
