using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class ItemHurtbox : NetworkBehaviour
{
    [SerializeField] bool startEnabled = false;
    [SerializeField] Collider hitbox;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioSFX[] hitSFX;
    [SerializeField] SoundLoudness loudness;

    IA_HurtboxAttack itemAction;

    readonly List<GameObject> hitEntities = new();
    float lastHitTime;

    private void Start()
    {
        if (!startEnabled) enabled = false;
    }

    public void Initialize(IA_HurtboxAttack itemAction)
    {
        this.itemAction = itemAction;
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
        if (isServer) return;
        AudioManager.Instance.PlayOneShot(audioSource, hitSFX[index], itemAction.Item.PData.gameObject, loudness);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;
        if (itemAction.Item == null || itemAction.Item.PData == null) return;
        if (other.gameObject == itemAction.Item.PData.gameObject) return;
        if (hitEntities.Contains(other.gameObject)) return;

        if ((Time.time - lastHitTime) > 0.5f)
        {
            lastHitTime = Time.time;

            int index = Random.Range(0, hitSFX.Length);
            AudioManager.Instance.PlayOneShot(audioSource, hitSFX[index], itemAction.Item.PData.gameObject, loudness);
            RpcPlayHitSFX(index);
        }

        if (!other.TryGetComponent(out EntityStats target)) return;

        target.ReceiveAttack(AttackEvent.From(itemAction.Item.PData, target, itemAction.AttackStat));
        hitEntities.Add(other.gameObject);
    }
}
