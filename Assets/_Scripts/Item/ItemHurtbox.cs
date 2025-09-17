using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class ItemHurtbox : NetworkBehaviour
{
    [SerializeField] bool startEnabled = false;
    [SerializeField] Collider hitbox;
    [SerializeField] ItemBase item;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioSFX[] hitSFX;

    List<GameObject> hitEntities = new();

    float lastHitTime;

    private void Start()
    {
        if (!startEnabled)
            enabled = false;
    }

    public void EnableHitbox()
    {
        hitEntities.Clear();
        hitbox.enabled = true;
    }

    public void DisableHitbox()
    {
        hitbox.enabled = false;
        hitEntities.Clear();
    }

    [ClientRpc]
    void RpcPlayHitSFX(int index)
    {
        AudioManager.Instance.PlayOneShot(audioSource, hitSFX[index]);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.gameObject == item.pData.gameObject) return;

        if (hitEntities.Contains(other.gameObject)) return;

        if ((Time.time - lastHitTime) > 0.5f)
            RpcPlayHitSFX(Random.Range(0, hitSFX.Length));

        if (other.TryGetComponent(out EntityStats stats))
        {
            if (stats is PlayerStats pStats)
            {
                pStats.ReceiveAttack(item.pData, item.primaryAtkStats);
            }
            else
            {
                stats.ReceiveAttack(item.pData.Player_Stats, item.primaryAtkStats);
            }

            hitEntities.Add(other.gameObject);
        }
    }
}
