using UnityEngine;

public class IA_AnimationMultiTrigger : ItemAction
{
    [SerializeField] protected AnimationTrigger[] animationTriggers;
    [SerializeField] protected bool forceAim = true;
    [SerializeField] protected bool sequenced = false;
    [SerializeField] protected SequenceDirection direction = SequenceDirection.Forward;
    
    protected int index = -1;
    protected AnimationTrigger currentTrigger;

    public override void Cancel()
    {
        TryPlaySFX(currentTrigger, ActionTiming.Cancel);
    }

    public override void OnAnimationTrigger()
    {
        TryPlaySFX(currentTrigger, ActionTiming.Trigger);
    }

    public override void Execute()
    {
        if (isServer) item.InUse = true;

        ItemActionType type = item.GetActionType(this);
        if (type == ItemActionType.None) return;

        if (forceAim) item.PData.Camera_Movement.ForcePlayerToAim();

        int index = sequenced ? GetSequencedIndex() : GetRandomIndex();
        currentTrigger = animationTriggers[index];

        if (type == ItemActionType.Primary)
            item.AnimationModule.PlayPrimary(this, item.PData, currentTrigger.Trigger);
        else if (type == ItemActionType.Secondary)
            item.AnimationModule.PlaySecondary(this, item.PData, currentTrigger.Trigger);

        TryPlaySFX(currentTrigger, ActionTiming.Start);
    }

    public override void OnAnimationFinish()
    {
        if (isServer) item.InUse = false;
        if (forceAim) item.PData.Camera_Movement.StopForcePlayerToAim();

        TryPlaySFX(currentTrigger, ActionTiming.Finish);
    }

    protected int GetRandomIndex()
    {
        index = Random.Range(0, animationTriggers.Length);
        return index;
    }

    protected int GetSequencedIndex()
    {
        index = direction == SequenceDirection.Forward ? ++index : --index;
        if (index < 0) index = animationTriggers.Length - 1;
        if (index >= animationTriggers.Length) index = 0;
        return index;
    }
}
