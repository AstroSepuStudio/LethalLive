using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class PunchManager : NetworkBehaviour
{
    [SerializeField] PlayerData pData;
    [SerializeField] AttackStat punchStats;
    [SerializeField] LayerMask entityLayer;

    private bool isPunching = false;
    WaitForSeconds punchCooldown;
    int debuffIndex;

    private void Start()
    {
        punchCooldown = new (punchStats.AttackCooldown);
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

        if (debuffIndex != -1)
            RemoveDebuff();

        debuffIndex = pData.Player_Movement.AddSpeedModifier(0.5f);

        LocalPlayAttackAnimation();
        RpcPlayAttackAnimation();
    }

    [ClientRpc]
    void RpcPlayAttackAnimation() => LocalPlayAttackAnimation();

    void LocalPlayAttackAnimation()
    {
        if (pData.Skin_Data.CharacterAnimator != null)
        {
            pData.EmoteManager.LocalCancelEmote();
            if (pData.Player_Movement.IsCrouching)
                pData.Skin_Data.CharacterAnimator.SetTrigger("AttackCrouch");
            else
                pData.Skin_Data.CharacterAnimator.SetTrigger("Attack");

            pData.Skin_Data.CharacterAnimator.SetLayerWeight(3, 1);
            StartCoroutine(PunchCooldown());
        }
    }

    IEnumerator PunchCooldown()
    {
        isPunching = true;
        pData.Camera_Movement.ForcePlayerToAim();

        yield return punchCooldown;

        isPunching = false;
        pData.Camera_Movement.StopForcePlayerToAim();
    }

    public void PunchDetection()
    {
        if (!isServer) return;

        CheckForHit();
    }

    public void PunchFinished()
    {
        if (!isServer) return;

        RemoveDebuff();
        pData.Skin_Data.CharacterAnimator.SetLayerWeight(3, 0);
    }

    [Server]
    void RemoveDebuff()
    {
        pData.Player_Movement.RemoveSpeedModifier(debuffIndex);
        debuffIndex = -1;
    }

    [Server]
    void CheckForHit()
    {
        Debug.Log("Checking for punch");
        Collider[] hitEntities = Physics.OverlapSphere(pData.Skin_Data.RightHand.position, punchStats.AttackRadius, entityLayer);
        foreach (Collider col in hitEntities)
        {
            if (!col.TryGetComponent(out EntityStats entity)) return;

            if (entity is PlayerStats playerStats)
            {
                playerStats.ReceiveAttack(pData, punchStats);
            }
            else
            {
                entity.ReceiveAttack(pData.Player_Stats, punchStats);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (pData.Skin_Data != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pData.Skin_Data.RightHand.position, punchStats.AttackRadius);
        }
    }
}
