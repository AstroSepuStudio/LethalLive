using Mirror;
using System.Collections;
using UnityEngine;

public class PunchManager : NetworkBehaviour
{
    [SerializeField] PlayerData pData;
    [SerializeField] AttackStat punchStats;

    private bool isPunching = false;
    WaitForSeconds punchCooldown;

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
        RpcPlayAttackAnimation();
    }

    [ClientRpc]
    void RpcPlayAttackAnimation()
    {
        if (pData.Skin_Data.CharacterAnimator != null)
        {
            pData.EmoteManager.LocalCancelEmote();
            if (pData.Player_Movement.IsCrouching)
                pData.Skin_Data.CharacterAnimator.SetTrigger("AttackCrouch");
            else
                pData.Skin_Data.CharacterAnimator.SetTrigger("Attack");

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
        if (!isLocalPlayer) return;

        CmdTryHit();
    }

    [Command]
    void CmdTryHit()
    {
        Debug.Log("Checking for punch");
        Collider[] hitPlayers = Physics.OverlapSphere(pData.Skin_Data.RightHand.position, punchStats.AttackRadius, pData.PlayerMask);
        foreach (Collider col in hitPlayers)
        {
            for (int i = 0; i < GameManager.Instance.Players.Count; i++)
            {
                // Valid target
                if (GameManager.Instance.Players[i].gameObject == pData.gameObject ||
                    GameManager.Instance.Players[i].gameObject != col.gameObject) continue;

                // Team check
                if (GameManager.Instance.Players[i].Team == pData.Team)
                {
                    if (!LobbyManager.Instace.LobbySettings.TeamKnock)
                    {
                        return;
                    }
                }

                float multiplier = Random.Range(1f, 2f);
                Vector3 dir = GameManager.Instance.Players[i].transform.position - pData.transform.position;

                float knockAmount = punchStats.AttackKnock * multiplier * (pData.Player_Stats.strenght / 100f);
                Vector3 momentum = multiplier * punchStats.AttackForce * dir.normalized;

                GameManager.Instance.Players[i].Player_Stats.ModifyKnock(knockAmount, momentum);
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
