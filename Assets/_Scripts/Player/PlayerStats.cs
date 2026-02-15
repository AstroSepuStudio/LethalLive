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

    [SyncVar]
    public bool dead = false;

    public override void OnStartServer()
    {
        ResetStats();
    }

    [Server]
    public void ResetStats()
    {
        currentHP = maxHP;
        currentStamina = maxStamina;
        currentKnock = 0f;
        dead = false;
    }

    protected override void Update()
    {
        if (!isServer) return;
    
        if (dead) return;

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
            if (LobbyManager.Instance.LobbySettings.TeamKnock)
                ModifyKnock(source, stats);

            if (LobbyManager.Instance.LobbySettings.TeamDamage)
                ModifyHP(source.Player_Stats, stats);
        }
        else
        {
            ModifyHP(source.Player_Stats, stats);
            ModifyKnock(source, stats);
        }
    }

    #region HP

    [Server]
    public void ExecutePlayer()
    {
        currentHP = 0;
        OnDeath(null, null);
    }

    [Server]
    public override void ModifyHP(EntityStats source, AttackStat stat)
    {
        if (!GameManager.Instance.gameStarted ||
            !GameManager.Instance.dayMod.dayStarted)
            return;

        currentHP = Mathf.Clamp(currentHP - stat.AttackDamage, 0f, maxHP);
        if (currentHP <= 0)
        {
            OnDeath(source, stat);
        }
    }

    protected override void OnHPChanged(float oldVal, float newVal)
    {
        pData.HUDmanager.UpdateHUD();
    }

    [Server]
    protected override void OnDeath(EntityStats source, AttackStat stat)
    {
        Vector3 momentum = Vector3.zero;

        if (source != null && stat != null)
        {
            float multiplier = Random.Range(1f, 2f);
            Vector3 dir = pData.transform.position - source.transform.position;
            momentum = multiplier * stat.AttackForce * dir.normalized;
        }

        dead = true;
        pData.OnPlayerDeath(stat, momentum);
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
        float strenght = 100;
        if (source != null)
            strenght = source.Player_Stats.strenght;

        float multiplier = Random.Range(1f, 2f);
        Vector3 dir = pData.transform.position - source.transform.position;

        float knockAmount = stat.AttackKnock * multiplier * (strenght / 100f);
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
