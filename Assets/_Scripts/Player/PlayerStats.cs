using Mirror;
using UnityEngine;

public class PlayerStats : EntityStats
{
    public PlayerData pData;

    [SerializeField] float staminaRecoveryDelay;
    [SerializeField] float staminaRecoveryRate;

    float staminaRecoveryTimer;

    [SyncVar]
    public float maxStamina = 100f;

    [SyncVar(hook = nameof(OnStaminaChanged))] 
    public float currentStamina;

    public override void OnStartServer()
    {
        currentHP = maxHP;
        currentStamina = maxStamina;
        currentKnock = 0f;
    }

    protected override void Update()
    {
        if (!isServer) return;

        staminaRecoveryTimer = Mathf.Clamp(staminaRecoveryTimer + Time.deltaTime, 0, staminaRecoveryDelay);
        knockRecoveryTimer = Mathf.Clamp(knockRecoveryTimer + Time.deltaTime, 0, knockRecoveryDelay);

        if (Mathf.Approximately(staminaRecoveryTimer, staminaRecoveryDelay))
        {
            currentStamina = Mathf.Clamp(currentStamina + Time.deltaTime * staminaRecoveryRate, 0, maxStamina);
        }

        if (Mathf.Approximately(knockRecoveryTimer, knockRecoveryDelay))
        {
            currentKnock = Mathf.Clamp(currentKnock - Time.deltaTime * knockRecoveryRate, 0, maxKnock);

            if (currentKnock <= ragdollRecoveryValue && pData.Skin_Data.Ragdoll_Manager.IsKnocked)
            {
                pData.Skin_Data.Ragdoll_Manager.DisableRagdoll();
            }
        }
    }

    [Server]
    public void ReceiveAttack(PlayerData source, AttackStat stats)
    {
        if (source.Team == pData.Team)
        {
            if (LobbyManager.Instace.LobbySettings.TeamKnock)
                ModifyKnock(source, stats);

            if (LobbyManager.Instace.LobbySettings.TeamDamage)
                ModifyHP(-stats.AttackDamage);
        }
        else
        {
            ModifyHP(-stats.AttackDamage);
            ModifyKnock(source, stats);
        }
    }

    #region HP

    [Server]

    public override void ModifyHP(float amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0f, maxHP);
        if (currentHP <= 0)
        {
            OnDeath();
        }
    }

    protected override void OnHPChanged(float oldVal, float newVal)
    {
        pData.HUDmanager.UpdateHUD();
    }

    [Server]
    protected override void OnDeath()
    {
        Debug.Log($"{gameObject.name} died.");
        // Add ragdoll, respawn, disable movement, etc.
    }

    #endregion

    #region Stamina

    [Server]
    public void ModifyStamina(float amount)
    {
        staminaRecoveryTimer = 0;
        currentStamina = Mathf.Clamp(currentStamina + amount, 0f, maxStamina);
    }

    void OnStaminaChanged(float oldVal, float newVal)
    {
        pData.HUDmanager.UpdateHUD();
    }

    #endregion

    #region Knock

    [Server]    
    public void ModifyKnock(PlayerData source, AttackStat stat)
    {
        float multiplier = Random.Range(1f, 2f);
        Vector3 dir = pData.transform.position - source.transform.position;

        float knockAmount = stat.AttackKnock * multiplier * (source.Player_Stats.strenght / 100f);
        Vector3 momentum = multiplier * stat.AttackForce * dir.normalized;

        ModifyKnock(knockAmount, momentum);
    }

    [Server]
    public override void ModifyKnock(float amount, Vector3 momentum)
    {
        knockRecoveryTimer = 0;
        currentKnock = Mathf.Clamp(currentKnock + amount, 0f, maxKnock);
        if (currentKnock >= maxKnock)
        {
            OnKnocked(momentum);
            return;
        }

        pData.Player_Movement.AddMomentum(momentum);
    }

    protected override void OnKnockChanged(float oldVal, float newVal)
    {
        pData.HUDmanager.UpdateHUD();
    }

    [Server]
    protected override void OnKnocked(Vector3 momentum)
    {
        Debug.Log($"{gameObject.name} was knocked!");
        pData.Skin_Data.Ragdoll_Manager.EnableRagdoll(momentum);
    }

    #endregion
}
