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
    }

    [Header("Audio")]
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] SFXGroup[] sfxGroups;

    protected Dictionary<SFXEvent, AudioSFX[]> sfxMap;

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

    public UnityEvent<AttackSource, AttackStat> OnDeath;
    public UnityEvent<AttackSource, AttackStat> OnTakeDamage;

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
        sfxMap = new Dictionary<SFXEvent, AudioSFX[]>();
        foreach (var group in sfxGroups)
            sfxMap[group.Event] = group.Clips;
    }

    [Server]
    protected void PlaySFX(SFXEvent sfxEvent)
    {
        if (!sfxMap.TryGetValue(sfxEvent, out var clips) || clips.Length == 0) return;
        int index = Random.Range(0, clips.Length);
        RpcPlaySFX(sfxEvent, index);
    }

    [ClientRpc]
    void RpcPlaySFX(SFXEvent sfxEvent, int clipIndex)
    {
        if (audioSource == null) return;
        if (!sfxMap.TryGetValue(sfxEvent, out var clips) || clipIndex >= clips.Length) return;

        if (sfxEvent == SFXEvent.Died)
            AudioManager.Instance.PlayOneShotAndDestroy(transform.position, clips[clipIndex]);
        else
            AudioManager.Instance.PlayOneShot(audioSource, clips[clipIndex]);
    }

    #endregion

    #region HP

    [Server]
    public virtual void ReceiveAttack(AttackSource source, AttackStat attack)
    {
        PlaySFX(SFXEvent.TakeDamage);
        ApplyDamage(source, attack);
        ApplyKnock(source, attack);
    }

    [Server]
    public virtual void ApplyDamage(AttackSource source, AttackStat attack)
    {
        currentHP = Mathf.Clamp(currentHP - attack.AttackDamage, 0f, maxHP);
        OnTakeDamage?.Invoke(source, attack);

        if (currentHP <= 0f)
            HandleDeath(source, attack);
    }

    protected virtual void OnHPChanged(float oldVal, float newVal) { }

    [Server]
    protected virtual void HandleDeath(AttackSource source, AttackStat attack)
    {
        OnDeath?.Invoke(source, attack);
        PlaySFX(SFXEvent.Died);
    }

    #endregion

    #region Knock

    [Server]
    public virtual void ApplyKnock(AttackSource source, AttackStat attack)
    {
        float srcStrength = source.Stats != null ? source.Stats.strength : 100f;

        float multiplier = Random.Range(1f, 2f);
        float knockAmount = attack.AttackKnock * multiplier * (srcStrength / 100f);
        Vector3 momentum = CalculateMomentum(source.Position, attack.AttackForce, multiplier);

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