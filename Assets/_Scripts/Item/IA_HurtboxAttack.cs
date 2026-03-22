using UnityEngine;

public class IA_HurtboxAttack : ItemAction
{
    [Header("Hurtbox Attack Settings")]
    [SerializeField] ItemHurtbox hurtbox;
    [SerializeField] string animationTrigger = "Atk_Default";
    [SerializeField] bool forceAim = true;
    [SerializeField] AttackStat attackStat;

    public AttackStat AttackStat => attackStat;

    public override void Initialize(ItemBase owner)
    {
        base.Initialize(owner);

        hurtbox.Initialize(this);
    }

    public override bool IsOnCooldown() => attackStat.OnCooldown;

    public override void EnterOnCooldown() => StartCoroutine(attackStat.CountdownCooldown());

    public override void Execute()
    {
        if (isServer) item.InUse = true;

        ItemActionType type = item.GetActionType(this);
        if (type == ItemActionType.None) return;

        if (forceAim)
            item.PData.Camera_Movement.ForcePlayerToAim();

        if (type == ItemActionType.Primary)
            item.AnimationModule.PlayPrimary(this, item.PData, animationTrigger);
        else if (type == ItemActionType.Secondary)
            item.AnimationModule.PlaySecondary(this, item.PData, animationTrigger);
    }

    public override void Cancel()
    {

    }

    public override void OnAnimationTrigger()
    {
        hurtbox.EnableHitbox();
    }

    public override void OnAnimationFinish()
    {
        if (isServer) item.InUse = false;
        hurtbox.DisableHitbox();

        if (forceAim)
            item.PData.Camera_Movement.StopForcePlayerToAim();
    }
}
