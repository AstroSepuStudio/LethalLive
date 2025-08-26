using Mirror;
using System.Collections;
using UnityEngine;

public class ExplosionComponent : NetworkBehaviour
{
    [SerializeField] AttackStat explosionStat;
    [SerializeField] ParticleSystem[] explosionParticles;
    [SerializeField] bool _multiTrigger = false;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioSFX[] explosionSFX;

    bool _triggered = false;
    bool _onCooldown = false;

    WaitForSeconds cooldown;

    private void Awake()
    {
        cooldown = new(explosionStat.AttackCooldown);
    }

    [Command]
    public void TriggerExplosion()
    {
        if (_triggered || _onCooldown) return;
        if (!_multiTrigger) _triggered = true;

        RPC_TriggerExplosionParticles();
        StartCoroutine(Cooldown());

        Collider[] hitPlayers = Physics.OverlapSphere(transform.position, explosionStat.AttackRadius);
        foreach (Collider col in hitPlayers)
        {
            for (int i = 0; i < GameManager.Instance.Players.Count; i++)
            {
                if (GameManager.Instance.Players[i].gameObject != col.gameObject) continue;

                Vector3 dir = GameManager.Instance.Players[i].transform.position - transform.position;
                float distance = dir.magnitude;

                float multiplier = Random.Range(0.75f, 1.25f);
                float effectiveness = Mathf.Lerp(1f, 0.75f, distance / explosionStat.AttackRadius);

                float knockAmount = explosionStat.AttackKnock * multiplier * effectiveness;
                Vector3 momentum = effectiveness * explosionStat.AttackForce * multiplier * dir.normalized;

                GameManager.Instance.Players[i].Player_Stats.ModifyKnock(knockAmount, momentum);
                GameManager.Instance.Players[i].Player_Stats.ModifyHP(-explosionStat.AttackDamage * effectiveness * multiplier);
            }
        }
    }

    [ClientRpc]
    public void RPC_TriggerExplosionParticles()
    {
        if (explosionParticles != null)
        {
            for (int i = 0; i < explosionParticles.Length; i++)
            {
                explosionParticles[i].Play();
            }
        }

        AudioManager.Instance.PlayOneShot(audioSource, explosionSFX[Random.Range(0, explosionSFX.Length)]);
    }

    IEnumerator Cooldown()
    {
        _onCooldown = true;
        yield return cooldown;
        _onCooldown = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionStat.AttackRadius);
    }
}
