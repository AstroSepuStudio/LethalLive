using Mirror;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    [SerializeField] PlayerData pData;

    [SerializeField] float staminaRecoveryDelay;
    [SerializeField] float staminaRecoveryRate;

    [SerializeField] float knockRecoveryDelay;
    [SerializeField] float knockRecoveryRate;
    [SerializeField] float ragdollRecoveryValue;

    float staminaRecoveryTimer;
    float knockRecoveryTimer;

    [SyncVar]
    public float maxHP = 100f;

    [SyncVar(hook = nameof(OnHPChanged))] 
    public float currentHP;

    [SyncVar]
    public float maxStamina = 100f;

    [SyncVar(hook = nameof(OnStaminaChanged))] 
    public float currentStamina;

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
        currentStamina = maxStamina;
        currentKnock = 0f;
    }

    private void Update()
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

    #region HP

    [Server]
    public void ModifyHP(float amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0f, maxHP);
        if (currentHP <= 0)
        {
            OnDeath();
        }
    }

    void OnHPChanged(float oldVal, float newVal)
    {
        pData.HUDmanager.UpdateHUD();
    }

    [Server]
    void OnDeath()
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
    public void ModifyKnock(float amount, Vector3 momentum)
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

    void OnKnockChanged(float oldVal, float newVal)
    {
        pData.HUDmanager.UpdateHUD();
    }

    [Server]
    void OnKnocked(Vector3 momentum)
    {
        Debug.Log($"{gameObject.name} was knocked!");
        pData.Skin_Data.Ragdoll_Manager.EnableRagdoll(momentum);
    }

    #endregion
}
