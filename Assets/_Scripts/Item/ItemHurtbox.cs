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
        if (!startEnabled) enabled = false;
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
        => AudioManager.Instance.PlayOneShot(audioSource, hitSFX[index]);

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;
        if (item?.pData == null) return;
        if (other.gameObject == item.pData.gameObject) return;
        if (hitEntities.Contains(other.gameObject)) return;

        if (!other.TryGetComponent(out EntityStats stats)) return;

        if ((Time.time - lastHitTime) > 0.5f)
        {
            lastHitTime = Time.time;
            RpcPlayHitSFX(Random.Range(0, hitSFX.Length));
        }

        stats.ReceiveAttack(AttackSource.From(item.pData), item.primaryAtkStats);
        hitEntities.Add(other.gameObject);
    }
}
