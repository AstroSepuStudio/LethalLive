using Mirror;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class EntityStats : NetworkBehaviour
{
    public enum SFXEvent { TakeDamage, Knocked, Died, StrongHit, PartialBreak }

    [System.Serializable]
    public struct SFXGroup
    {
        public SFXEvent Event;
        public AudioSFX[] Clips;
        public SoundLoudness Loudness;
    }

    [Header("Audio")]
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] SFXGroup[] sfxGroups;

    protected Dictionary<SFXEvent, SFXGroup> sfxMap;

    [Header("Knock Recovery")]
    [SerializeField] protected float knockRecoveryDelay;
    [SerializeField] protected float knockRecoveryRate;
    [SerializeField] protected float ragdollRecoveryValue;
    protected float knockRecoveryTimer;

    [Header("Stats")]
    [SyncVar] public float maxHP = 100f;
    [SyncVar] public float maxKnock = 100f;
    [SyncVar] public float strength = 100f;
    [SyncVar] public float speed = 100f;

    [SyncVar(hook = nameof(OnHPChanged))]
    public float currentHP;

    [SyncVar(hook = nameof(OnKnockChanged))]
    public float currentKnock;

    [SyncVar] public bool dead;
    [SyncVar] public bool knocked;

    public UnityEvent<AttackEvent> OnDeath;
    public UnityEvent<AttackEvent> OnTakeDamage;

    #region Lifecylce

    protected virtual void Awake()
    {
        BuildSFXMap();
    }

    public override void OnStartServer()
    {
        currentHP = maxHP;
        currentKnock = 0f;
    }

    protected virtual void Update()
    {
        if (!isServer) return;
        TickKnockRecovery();
    }

    #endregion

    #region SFX

    void BuildSFXMap()
    {
        sfxMap = new Dictionary<SFXEvent, SFXGroup>();
        foreach (var group in sfxGroups)
            sfxMap[group.Event] = group;
    }

    [Server]
    protected void PlaySFX(SFXEvent sfxEvent)
    {
        if (!sfxMap.TryGetValue(sfxEvent, out var group) || group.Clips.Length == 0) return;
        int index = Random.Range(0, group.Clips.Length);
        RpcPlaySFX(sfxEvent, index);
    }

    [ClientRpc]
    void RpcPlaySFX(SFXEvent sfxEvent, int clipIndex)
    {
        if (audioSource == null) return;
        if (!sfxMap.TryGetValue(sfxEvent, out var group) || clipIndex >= group.Clips.Length) return;

        if (sfxEvent == SFXEvent.Died)
            AudioManager.Instance.PlayOneShotAndDestroy(transform.position, group.Clips[clipIndex], gameObject, group.Loudness);
        else
            AudioManager.Instance.PlayOneShot(audioSource, group.Clips[clipIndex], gameObject, group.Loudness);
    }

    #endregion

    #region HP

    [Server]
    public virtual void OverrideMaxHP(float maxHP, bool restore)
    {
        this.maxHP = maxHP;
        if (restore)
            currentHP = maxHP;
    }

    [Server]
    public virtual void ReceiveAttack(AttackEvent source)
    {
        PlaySFX(SFXEvent.TakeDamage);
        ApplyDamage(source);
        ApplyKnock(source);
    }

    [Server]
    public virtual void ApplyDamage(AttackEvent source)
    {
        currentHP = Mathf.Clamp(currentHP - source.AttackStat_.AttackDamage, 0f, maxHP);
        OnTakeDamage?.Invoke(source);

        if (currentHP <= 0f)
            HandleDeath(source);
    }

    protected virtual void OnHPChanged(float oldVal, float newVal) { }

    [Server]
    protected virtual void HandleDeath(AttackEvent source)
    {
        dead = true;
        OnDeath?.Invoke(source);
        PlaySFX(SFXEvent.Died);
    }

    #endregion

    #region Knock

    [Server]
    public virtual void ApplyKnock(AttackEvent source)
    {
        float srcStrength = source.SourceStats != null ? source.SourceStats.strength : 100f;

        float multiplier = Random.Range(1f, 2f);
        float knockAmount = source.AttackStat_.AttackKnock * multiplier * (srcStrength / 100f);
        Vector3 momentum = CalculateMomentum(source.Position, source.AttackStat_.AttackForce, multiplier);

        AddKnock(knockAmount, momentum);
    }

    protected Vector3 CalculateMomentum(Vector3 sourcePos, float force, float multiplier)
        => multiplier * force * (transform.position - sourcePos).normalized;

    [Server]
    public virtual void AddKnock(float amount, Vector3 momentum)
    {
        knockRecoveryTimer = 0f;
        currentKnock = Mathf.Clamp(currentKnock + amount, 0f, maxKnock);

        if (currentKnock >= maxKnock)
            HandleKnocked(momentum);
    }

    protected virtual void OnKnockChanged(float oldVal, float newVal) { }

    [Server]
    protected virtual void HandleKnocked(Vector3 momentum)
    {
        knocked = true;
        PlaySFX(SFXEvent.Knocked);
    }

    [Server]
    protected void TickKnockRecovery()
    {
        knockRecoveryTimer = Mathf.Clamp(knockRecoveryTimer + Time.deltaTime, 0f, knockRecoveryDelay);

        if (!Mathf.Approximately(knockRecoveryTimer, knockRecoveryDelay)) return;

        currentKnock = Mathf.Clamp(currentKnock - Time.deltaTime * knockRecoveryRate, 0f, maxKnock);
    }

    #endregion
}