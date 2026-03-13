using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : EntityStats
{
    [Header("Player")]
    public PlayerData pData;

    [SerializeField] float staminaRecoveryDelay;
    [SerializeField] float staminaRecoveryRate;
    float staminaRecoveryTimer;

    [SyncVar] public float maxStamina = 100f;
    [SyncVar(hook = nameof(OnStaminaChanged))] public float currentStamina;

    public UnityEvent OnPlayerKnocked;

    #region Lifecycle

    public override void OnStartServer() => ResetStats();

    [Server]
    public void ResetStats()
    {
        currentHP = maxHP;
        currentStamina = maxStamina;
        currentKnock = 0f;
        dead = false;
        knocked = false;
    }

    protected override void Update()
    {
        if (!isServer || dead) return;

        TickStaminaRecovery();
        TickKnockRecovery();

        if (currentKnock <= ragdollRecoveryValue && pData.Skin_Data.Ragdoll_Manager.IsKnocked)
        {
            pData.Skin_Data.Ragdoll_Manager.DisableRagdoll();
            knocked = false;
        }
    }

    #endregion

    #region Attack

    [Server]
    public override void ReceiveAttack(AttackEvent source)
    {
        bool sameTeam = source.TeamID != -1 && source.TeamID == (int)pData.Team;

        if (sameTeam)
        {
            if (LobbySettings.Instance.TeamDamage) ApplyDamage(source);
            if (LobbySettings.Instance.TeamKnock) ApplyKnock(source);
        }
        else
        {
            PlaySFX(SFXEvent.TakeDamage);
            ApplyDamage(source);
            ApplyKnock(source);
        }
    }

    #endregion

    #region HP

    [Server]
    public override void ApplyDamage(AttackEvent source)
    {
        if (!GameManager.Instance.gameStarted || !GameManager.Instance.dayMod.dayStarted)
            return;

        base.ApplyDamage(source);
    }

    protected override void OnHPChanged(float oldVal, float newVal)
        => pData.HUDmanager.UpdateHUD();

    [Server]
    protected override void HandleDeath(AttackEvent source)
    {
        if (dead) return;
        float multiplier = Random.Range(1f, 2f);
        Vector3 momentum = source.SourceStats != null
            ? CalculateMomentum(source.Position, source.AttackStat_.AttackForce, multiplier)
            : Vector3.zero;

        dead = true;
        pData.OnPlayerDeath(source.AttackStat_, momentum, false);
    }

    [Server]
    protected void HandleDeath(AttackEvent source, bool executed)
    {
        if (dead) return;
        float multiplier = Random.Range(1f, 2f);
        Vector3 momentum = source.SourceStats != null
            ? CalculateMomentum(source.Position, source.AttackStat_.AttackForce, multiplier)
            : Vector3.zero;

        dead = true;
        pData.OnPlayerDeath(source.AttackStat_, momentum, executed);
    }

    [Server]
    public void ExecutePlayer()
    {
        currentHP = 0f;
        HandleDeath(default, true);
    }

    #endregion

    #region Stamina

    [Server]
    public void ModifyStamina(float amount)
    {
        staminaRecoveryTimer = 0f;
        currentStamina = Mathf.Clamp(currentStamina + amount, 0f, maxStamina);
    }

    void OnStaminaChanged(float oldVal, float newVal) => pData.HUDmanager.UpdateHUD();

    void TickStaminaRecovery()
    {
        staminaRecoveryTimer = Mathf.Clamp(staminaRecoveryTimer + Time.deltaTime, 0f, staminaRecoveryDelay);
        if (!Mathf.Approximately(staminaRecoveryTimer, staminaRecoveryDelay)) return;
        currentStamina = Mathf.Clamp(currentStamina + Time.deltaTime * staminaRecoveryRate, 0f, maxStamina);
    }

    #endregion

    #region Knock

    protected override void OnKnockChanged(float oldVal, float newVal)
        => pData.HUDmanager.UpdateHUD();

    [Server]
    public override void AddKnock(float amount, Vector3 momentum)
    {
        knockRecoveryTimer = 0f;
        currentKnock = Mathf.Clamp(currentKnock + amount, 0f, maxKnock);

        if (currentKnock >= maxKnock) HandleKnocked(momentum);
        else pData.Player_Movement.AddMomentum(momentum);
    }

    [Server]
    protected override void HandleKnocked(Vector3 momentum)
    {
        if (knocked) return;
        knocked = true;
        pData.PlayerInventory.DropEverything();
        pData.Skin_Data.Ragdoll_Manager.EnableRagdoll(momentum);
        OnPlayerKnocked?.Invoke();
    }

    #endregion
}