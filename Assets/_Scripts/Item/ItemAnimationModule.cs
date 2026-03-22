using System.Collections;
using UnityEngine;

public class ItemAnimationModule : MonoBehaviour
{
    [SerializeField] float primaryTriggerTime;
    [SerializeField] float primaryFinishTime;
    [SerializeField] float secondaryTriggerTime;
    [SerializeField] float secondaryFinishTime;

    Coroutine activeSequence;

    WaitForSeconds primaryTrigger;
    WaitForSeconds primaryFinish;
    WaitForSeconds secondaryTrigger;
    WaitForSeconds secondaryFinish;

    public void Initialize()
    {
        primaryTrigger = new(primaryTriggerTime);
        primaryFinish = new(primaryFinishTime);
        secondaryTrigger = new(secondaryTriggerTime);
        secondaryFinish = new(secondaryFinishTime);
    }

    public void PlayPrimary(ItemAction action, PlayerData pData, string animTrigger)
        => PlaySequence(action, pData, animTrigger, primaryTrigger, primaryFinish);

    public void PlaySecondary(ItemAction action, PlayerData pData, string animTrigger)
        => PlaySequence(action, pData, animTrigger, secondaryTrigger, secondaryFinish);

    public void StopSequence()
    {
        if (activeSequence != null)
            StopCoroutine(activeSequence);
        activeSequence = null;
    }

    void PlaySequence(ItemAction action, PlayerData pData, string animTrigger,
                      WaitForSeconds triggerDelay, WaitForSeconds finishDelay)
    {
        StopSequence();
        activeSequence = StartCoroutine(Sequence(action, pData, animTrigger, triggerDelay, finishDelay));
    }

    IEnumerator Sequence(ItemAction action, PlayerData pData, string animTrigger,
                         WaitForSeconds triggerDelay, WaitForSeconds finishDelay)
    {
        pData.Skin_Data.CharacterAnimator.SetTrigger(animTrigger);

        yield return triggerDelay;
        action.OnAnimationTrigger();

        yield return finishDelay;
        action.OnAnimationFinish();

        activeSequence = null;
    }
}
