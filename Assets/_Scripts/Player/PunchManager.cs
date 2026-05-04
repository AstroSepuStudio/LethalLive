using Mirror;
using System.Collections;
using UnityEngine;

public class PunchManager : NetworkBehaviour
{
    [SerializeField] PlayerData pData;
    [SerializeField] AttackStat punchStats;
    [SerializeField] LayerMask entityLayer;

    const float DETECTION_TIME = 0.2f;
    const float FINISH_TIME = 0.35f;

    bool isPunching = false;
    int debuffIndex = -1;

    readonly WaitForSeconds waitDetect = new(DETECTION_TIME);
    readonly WaitForSeconds finishDelay = new(FINISH_TIME - DETECTION_TIME);

    public void OnPunchInput()
    {
        if (isPunching || pData._LockPlayer) return;
        CmdRequestPunch();
    }

    [Command]
    void CmdRequestPunch()
    {
        if (isPunching || pData._LockPlayer) return;
        if (!pData.InputHandler.IsDefaultController) return;
        if (debuffIndex != -1) RemoveDebuff();

        string astr = pData.Player_Movement.IsCrouching ? "AttackCrouch" : "Attack";
        pData.Skin_Data.CharacterAnimator.SetTrigger(astr);
        debuffIndex = pData.Player_Movement.AddSpeedModifier(0.5f);

        pData.Player_Movement.ServerForceAimAt(
            transform.position + pData.CameraPivot.forward * 10f);

        StartCoroutine(ServerPunchSequence());
        pData.EmoteManager.ServerClearLoopEmote();

        RpcStartPunchClient();
    }

    [Server]
    IEnumerator ServerPunchSequence()
    {
        yield return waitDetect;
        CheckForHit();

        yield return finishDelay;
        RemoveDebuff();
        pData.Player_Movement.ServerClearForcedAim();
    }

    [ClientRpc]
    void RpcStartPunchClient()
    {
        StartCoroutine(ClientPunchCooldown());
    }

    IEnumerator ClientPunchCooldown()
    {
        isPunching = true;
        yield return new WaitForSeconds(punchStats.AttackCooldown);
        isPunching = false;
    }

    [Server]
    void CheckForHit()
    {
        Collider[] hits = Physics.OverlapSphere(
            pData.Skin_Data.RightHand.position, punchStats.AttackRadius, entityLayer);

        foreach (Collider col in hits)
        {
            if (!col.TryGetComponent(out EntityStats target)) continue;
            if (target == pData.Player_Stats) continue;
            target.ReceiveAttack(AttackEvent.From(pData, target, punchStats));
        }
    }

    [Server]
    void RemoveDebuff()
    {
        if (debuffIndex == -1) return;
        pData.Player_Movement.RemoveSpeedModifier(debuffIndex);
        debuffIndex = -1;
    }

    private void OnDrawGizmosSelected()
    {
        if (pData?.Skin_Data == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pData.Skin_Data.RightHand.position, punchStats.AttackRadius);
    }
}
