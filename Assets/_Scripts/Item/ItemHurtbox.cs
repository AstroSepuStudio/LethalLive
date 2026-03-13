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
    [SerializeField] SoundLoudness loudness;

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
        => AudioManager.Instance.PlayOneShot(audioSource, hitSFX[index], item.pData.gameObject, loudness);

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;
        if (item?.pData == null) return;
        if (other.gameObject == item.pData.gameObject) return;
        if (hitEntities.Contains(other.gameObject)) return;

        if (!other.TryGetComponent(out EntityStats target)) return;

        if ((Time.time - lastHitTime) > 0.5f)
        {
            lastHitTime = Time.time;
            RpcPlayHitSFX(Random.Range(0, hitSFX.Length));
        }

        target.ReceiveAttack(AttackEvent.From(item.pData, target, item.primaryAtkStats));
        hitEntities.Add(other.gameObject);
    }
}
