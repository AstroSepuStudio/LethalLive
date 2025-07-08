using Mirror;
using System.Collections;
using UnityEngine;

public class PunchManager : NetworkBehaviour
{
    [SerializeField] PlayerData pData;
    [SerializeField] float attackCooldown = 1.0f;
    [SerializeField] float punchRadius = 1f;

    private bool isPunching = false;
    WaitForSeconds cooldown;

    private void Start()
    {
        cooldown = new (attackCooldown);
    }

    public void OnPunchInput()
    {
        if (!isLocalPlayer) return;
        if (isPunching) return;

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
        if (pData.CharacterAnimator != null)
        {
            pData.EmoteManager.LocalCancelEmote();
            if (pData.Player_Movement.IsCrouching)
                pData.CharacterAnimator.SetTrigger("AttackCrouch");
            else
                pData.CharacterAnimator.SetTrigger("Attack");

            StartCoroutine(PunchCooldown());
        }
    }

    IEnumerator PunchCooldown()
    {
        isPunching = true;
        pData.Camera_Movement.ForcePlayerToAim();

        yield return cooldown;

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
        Collider[] hitPlayers = Physics.OverlapSphere(pData.RightHand.position, punchRadius, pData.PlayerMask);

        foreach (Collider col in hitPlayers)
        {
            if (col.TryGetComponent(out PlayerStats targetStats) && col.gameObject != gameObject)
            {
                targetStats.ModifyKnock(Random.Range(10, 20) * (pData.Player_Stats.strenght / 100f));
                //Debug.Log($"[SERVER] {pData.PlayerName} hit {col.name}");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (pData.RightHand != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pData.RightHand.position, punchRadius);
        }
    }
}
