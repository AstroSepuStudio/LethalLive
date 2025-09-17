using Mirror;
using UnityEngine;

public class EntityStats : NetworkBehaviour
{
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] protected AudioSFX[] takeDamageSFX;
    [SerializeField] protected AudioSFX[] knockedSFX;
    [SerializeField] protected AudioSFX[] diedSFX;

    [SerializeField] protected float knockRecoveryDelay;
    [SerializeField] protected float knockRecoveryRate;
    [SerializeField] protected float ragdollRecoveryValue;

    protected float knockRecoveryTimer;

    [SyncVar]
    public float maxHP = 100f;

    [SyncVar(hook = nameof(OnHPChanged))]
    public float currentHP;

    [SyncVar]
    public float maxKnock = 100f;

    [SyncVar(hook = nameof(OnKnockChanged))]
    public float currentKnock;

    [SyncVar]
    public float strenght = 100f;

    [SyncVar]
    public float speed = 100f;

    public override void OnStartServer()
    {
        currentHP = maxHP;
        currentKnock = 0f;
    }

    protected virtual void Update()
    {
        if (!isServer) return;

        knockRecoveryTimer = Mathf.Clamp(knockRecoveryTimer + Time.deltaTime, 0, knockRecoveryDelay);

        if (Mathf.Approximately(knockRecoveryTimer, knockRecoveryDelay))
        {
            currentKnock = Mathf.Clamp(currentKnock - Time.deltaTime * knockRecoveryRate, 0, maxKnock);

            //if (currentKnock <= ragdollRecoveryValue && pData.Skin_Data.Ragdoll_Manager.IsKnocked)
            //{
            //    pData.Skin_Data.Ragdoll_Manager.DisableRagdoll();
            //}
        }
    }

    #region HP

    [Server]
    public virtual void ReceiveAttack(EntityStats src, AttackStat attack)
    {
        RequestPlaySFX(0);

        ModifyHP(-attack.AttackDamage);

        float multiplier = Random.Range(1f, 2f);
        Vector3 dir = transform.position - src.transform.position;

        float knockAmount = attack.AttackKnock * multiplier * (src.strenght / 100f);
        Vector3 momentum = multiplier * attack.AttackForce * dir.normalized;
        ModifyKnock(-knockAmount, momentum);
    }

    /// <summary> 0=Take Damage , 1=Knocked Out , 2=Died </summary>
    [Server]
    protected virtual void RequestPlaySFX(int index)
    {
        int sfxIndex = 0;
        if (index == 0 && takeDamageSFX.Length > 0)
            sfxIndex = Random.Range(0, takeDamageSFX.Length);
        else if (index == 1 && knockedSFX.Length > 0)
            sfxIndex = Random.Range(0, knockedSFX.Length);
        else if (index == 2 && diedSFX.Length > 0)
            sfxIndex = Random.Range(0, diedSFX.Length);

        RpcPlaySFX(index, sfxIndex);
    }

    [ClientRpc]
    protected virtual void RpcPlaySFX(int index, int sfxIndex)
    {
        if (audioSource == null) return;

        if (index == 0 && takeDamageSFX.Length > 0)
            AudioManager.Instance.PlayOneShot(audioSource, takeDamageSFX[sfxIndex]);
        else if (index == 1 && knockedSFX.Length > 0)
            AudioManager.Instance.PlayOneShot(audioSource, knockedSFX[sfxIndex]);
        else if (index == 2 && diedSFX.Length > 0)
            AudioManager.Instance.PlayOneShotAndDestroy(transform.position, diedSFX[sfxIndex]);
    }

    [Server]
    public virtual void ModifyHP(float amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0f, maxHP);
        if (currentHP <= 0)
        {
            OnDeath();
        }
    }

    protected virtual void OnHPChanged(float oldVal, float newVal)
    {
    }

    [Server]
    protected virtual void OnDeath()
    {
        RequestPlaySFX(2);

        Debug.Log($"{gameObject.name} died.");
        // Add ragdoll, respawn, disable movement, etc.
    }

    #endregion

    #region Knock

    [Server]
    public virtual void ModifyKnock(float amount, Vector3 momentum)
    {
        knockRecoveryTimer = 0;
        currentKnock = Mathf.Clamp(currentKnock + amount, 0f, maxKnock);
        if (currentKnock >= maxKnock)
        {
            OnKnocked(momentum);
            return;
        }

        //pData.Player_Movement.AddMomentum(momentum);
    }

    protected virtual void OnKnockChanged(float oldVal, float newVal)
    {

    }

    [Server]
    protected virtual void OnKnocked(Vector3 momentum)
    {
        RequestPlaySFX(1);

        Debug.Log($"{gameObject.name} was knocked!");
        //pData.Skin_Data.Ragdoll_Manager.EnableRagdoll(momentum);
    }

    #endregion
}
