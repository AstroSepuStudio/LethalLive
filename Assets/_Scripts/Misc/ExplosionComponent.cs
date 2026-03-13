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

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionStat.AttackRadius);
        foreach (Collider col in hits)
        {
            if (!col.TryGetComponent(out EntityStats stats)) continue;

            Vector3 dir = stats.transform.position - transform.position;
            float distance = dir.magnitude;
            float multiplier = Random.Range(0.75f, 1.25f);
            float effectiveness = Mathf.Lerp(1f, 0.75f, distance / explosionStat.AttackRadius);

            AttackStat modifiedAttack = new(explosionStat, explosionStat.AttackDamage * effectiveness * multiplier);
            float knockAmount = explosionStat.AttackKnock * multiplier * effectiveness;
            Vector3 momentum = effectiveness * explosionStat.AttackForce * multiplier * dir.normalized;

            stats.ApplyDamage(AttackSource.None, modifiedAttack);
            stats.AddKnock(knockAmount, momentum);
        }
    }

    [ClientRpc]
    public void RPC_TriggerExplosionParticles()
    {
        if (explosionParticles != null)
            foreach (var p in explosionParticles) p.Play();

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
