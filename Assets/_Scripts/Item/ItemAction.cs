using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class ItemAction : NetworkBehaviour
{
    [SerializeField] protected AudioSource audioSource;

    protected enum ActionTiming { Start, Cancel, Trigger, Finish}
    [Serializable] protected struct ActionSFX { public ActionTiming Timing; public AudioSFX SFX; public SoundLoudness Loudness; }
    [Serializable] protected struct AnimationTrigger 
    { 
        public string Trigger; 
        public ActionSFX[] SFXs;
    }

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

    protected virtual void TryPlaySFX(AnimationTrigger animationTrigger, ActionTiming timing)
    {
        if (animationTrigger.SFXs == null || animationTrigger.SFXs.Length <= 0) return;
        foreach (var sfx in animationTrigger.SFXs)
        {
            if (sfx.Timing == timing)
            {
                if (sfx.SFX == null) return;
                AudioManager.Instance.PlayOneShot(audioSource, sfx.SFX, gameObject, sfx.Loudness);
                return;
            }
        }
    }
}
