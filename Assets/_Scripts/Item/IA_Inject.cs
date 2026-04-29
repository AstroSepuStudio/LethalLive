using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class IA_Inject : ItemAction
{
    [SerializeField] protected AnimationTrigger animationTrigger;

    public UnityEvent OnInjectionStarted;
    public UnityEvent OnInjectionCanceled;
    public UnityEvent OnInjectionTrigger;
    public UnityEvent OnInjectionCompleted;
    public UnityEvent OnInjectionEmpty;
    public UnityEvent<float> OnOvertimeInjection;

    bool empty = false;
    bool injecting = false;
    Coroutine injectCoroutine;

    public bool IsEmpty => empty;
    public bool IsInjecting => injecting;

    public override bool IsOnCooldown() => empty;

    public override void Initialize(ItemBase owner)
    {
        base.Initialize(owner);
        empty = false;
        injecting = false;
    }

    public override void Execute()
    {
        if (empty) return;
        if (injectCoroutine != null) StopCoroutine(injectCoroutine);

        injecting = true;
        OnInjectionStarted?.Invoke();

        TryPlaySFX(animationTrigger, ActionTiming.Start);
        item.AnimationModule.PlayPrimary(this, item.PData, animationTrigger.Trigger, true);
    }

    public override void Cancel()
    {
        if (empty) return;
        if (!injecting) return;

        StopOvertimeCoroutine();
        item.AnimationModule.StopSequence(true, animationTrigger.Trigger);

        injecting = false;
        OnInjectionCanceled?.Invoke();
    }

    public override void OnAnimationTrigger()
    {
        base.OnAnimationTrigger();

        OnInjectionTrigger?.Invoke();
        TryPlaySFX(animationTrigger, ActionTiming.Trigger);
        injectCoroutine = StartCoroutine(OvertimeCoroutine());
    }

    public override void OnAnimationFinish()
    {
        StopOvertimeCoroutine();

        TryPlaySFX(animationTrigger, ActionTiming.Finish);

        injecting = false;
        empty = true;

        OnInjectionCompleted?.Invoke();
        OnInjectionEmpty?.Invoke();
    }

    IEnumerator OvertimeCoroutine()
    {
        float elapsed = 0f;
        float duration = GetAnimationFinishTime();

        if (duration <= 0f)
        {
            injectCoroutine = null;
            yield break;
        }

        while (elapsed < duration)
        {
            float delta = Time.deltaTime / duration;
            OnOvertimeInjection?.Invoke(delta);

            elapsed += Time.deltaTime;
            yield return null;
        }

        injectCoroutine = null;
    }

    void StopOvertimeCoroutine()
    {
        if (injectCoroutine == null) return;
        StopCoroutine(injectCoroutine);
        injectCoroutine = null;
    }
}
