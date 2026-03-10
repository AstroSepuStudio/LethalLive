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
    [SyncVar] public bool dead = false;

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
    }

    protected override void Update()
    {
        if (!isServer || dead) return;

        TickStaminaRecovery();
        TickKnockRecovery();

        if (currentKnock <= ragdollRecoveryValue && pData.Skin_Data.Ragdoll_Manager.IsKnocked)
            pData.Skin_Data.Ragdoll_Manager.DisableRagdoll();
    }

    #endregion

    #region Attack

    [Server]
    public override void ReceiveAttack(AttackSource source, AttackStat attack)
    {
        bool sameTeam = source.TeamID != -1 && source.TeamID == (int)pData.Team;

        if (sameTeam)
        {
            if (LobbySettings.Instance.TeamDamage) ApplyDamage(source, attack);
            if (LobbySettings.Instance.TeamKnock) ApplyKnock(source, attack);
        }
        else
        {
            PlaySFX(SFXEvent.TakeDamage);
            ApplyDamage(source, attack);
            ApplyKnock(source, attack);
        }
    }

    #endregion

    #region HP

    [Server]
    public override void ApplyDamage(AttackSource source, AttackStat attack)
    {
        if (!GameManager.Instance.gameStarted || !GameManager.Instance.dayMod.dayStarted)
            return;

        base.ApplyDamage(source, attack);
    }

    protected override void OnHPChanged(float oldVal, float newVal)
        => pData.HUDmanager.UpdateHUD();

    [Server]
    protected override void HandleDeath(AttackSource source, AttackStat attack)
    {
        float multiplier = Random.Range(1f, 2f);
        Vector3 momentum = source.Stats != null
            ? CalculateMomentum(source.Position, attack.AttackForce, multiplier)
            : Vector3.zero;

        dead = true;
        pData.OnPlayerDeath(attack, momentum);
    }

    [Server]
    public void ExecutePlayer()
    {
        currentHP = 0f;
        HandleDeath(default, null);
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
        Debug.Log($"[{pData.PlayerName}] Player knocked");
        pData.Skin_Data.Ragdoll_Manager.EnableRagdoll(momentum);
        OnPlayerKnocked?.Invoke();
    }

    #endregion
}