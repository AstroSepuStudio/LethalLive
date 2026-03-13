using Mirror;
using System.Collections;
using UnityEngine;

public class PunchManager : NetworkBehaviour
{
    [SerializeField] PlayerData pData;
    [SerializeField] AttackStat punchStats;
    [SerializeField] LayerMask entityLayer;

    bool isPunching = false;
    int debuffIndex = -1;
    WaitForSeconds punchCooldown;

    private void Start()
    {
        punchCooldown = new(punchStats.AttackCooldown);
    }

    public void OnPunchInput()
    {
        if (isPunching || pData._LockPlayer) return;
        CmdRequestPunch();
    }

    [Command]
    void CmdRequestPunch()
    {
        if (isPunching || pData._LockPlayer) return;

        if (debuffIndex != -1) RemoveDebuff();
        debuffIndex = pData.Player_Movement.AddSpeedModifier(0.5f);

        RpcPlayAttackAnimation();
    }

    [ClientRpc]
    void RpcPlayAttackAnimation() => LocalPlayAttackAnimation();

    void LocalPlayAttackAnimation()
    {
        if (pData.Skin_Data.CharacterAnimator == null) return;

        pData.EmoteManager.LocalCancelEmote();
        string trigger = pData.Player_Movement.IsCrouching ? "AttackCrouch" : "Attack";
        pData.Skin_Data.CharacterAnimator.SetTrigger(trigger);
        pData.Skin_Data.CharacterAnimator.SetLayerWeight(2, 1);
        StartCoroutine(PunchCooldown());
    }

    IEnumerator PunchCooldown()
    {
        isPunching = true;
        pData.Camera_Movement.ForcePlayerToAim();
        yield return punchCooldown;
        isPunching = false;
        pData.Camera_Movement.StopForcePlayerToAim();
    }

    [Server]
    public void PunchDetection() => CheckForHit();

    [Server]
    public void PunchFinished()
    {
        RemoveDebuff();
        RpcSetAnimatorLayerWeight(2, 0);
    }

    [ClientRpc]
    void RpcSetAnimatorLayerWeight(int layer, float weight)
        => pData.Skin_Data.CharacterAnimator.SetLayerWeight(layer, weight);

    [Server]
    void RemoveDebuff()
    {
        pData.Player_Movement.RemoveSpeedModifier(debuffIndex);
        debuffIndex = -1;
    }

    [Server]
    void CheckForHit()
    {
        Collider[] hits = Physics.OverlapSphere(pData.Skin_Data.RightHand.position, punchStats.AttackRadius, entityLayer);
        foreach (Collider col in hits)
        {
            if (!col.TryGetComponent(out EntityStats target)) continue;

            target.ReceiveAttack(AttackEvent.From(pData, target, punchStats));
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (pData?.Skin_Data == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pData.Skin_Data.RightHand.position, punchStats.AttackRadius);
    }
}
