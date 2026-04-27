using Mirror;
using UnityEngine;

public class SkulkParasiteAI : AIBrain, IHearingListener
{
    [Header("Skulk Parasite")]
    [SerializeField] int burrowKeyIndex = 0;
    [SerializeField] int hideFinKeyIndex = 1;
    [SerializeField] float emissionSpeed = 2f;
    [SerializeField] float huntCooldown = 10f;
    [SerializeField, SyncVar(hook = nameof(OnBurrowValueChanged))] float burrowValue = 0;
    [SerializeField, SyncVar(hook = nameof(OnFinValueChanged))] float finValue = 0;

    [Header("State Indexes")]
    [SerializeField] int wanderStateIndex = 0;
    [SerializeField] int huntStateIndex = 1;
    [SerializeField] int chargeStateIndex = 2;
    [SerializeField] int latchedStateIndex = 3;
    [SerializeField] int disengageStateIndex = 4;
    [SerializeField] int ceilingWaitStateIndex = 5;

    [Header("Ceiling Attach")]
    [SerializeField] float ceilingAttachChance = 0.25f;
    [SerializeField] float ceilingRayDistance = 3f;
    [SerializeField] LayerMask ceilingLayer;

    AIS_SkulkHunt huntState;
    AIS_SkulkCharge chargeState;
    AIS_SkulkLatched latchedState;
    AIS_SkulkDisengage disengageState;
    AIS_SkulkCeilingWait ceilingWaitState;

    PlayerData lastHeardPlayer;

    MaterialPropertyBlock mpb;
    float emissionTimer;
    float huntTimer;

    readonly System.Collections.Generic.Dictionary<SoundLoudness, float> hearingOverride = new()
    {
        { SoundLoudness.Quiet, 1f },
        { SoundLoudness.Moderate, 3f },
        { SoundLoudness.Average, 5f },
        { SoundLoudness.Loud, 8f },
        { SoundLoudness.Global, 32f }
    };

    protected override void Start()
    {
        base.Start();
        mpb = new MaterialPropertyBlock();

        if (!isServer) return;

        Burrow(true);
        EnableFin(false);

        CacheStates();
        HearingEventBroadcaster.Instance.AddListener(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (isServer) HearingEventBroadcaster.Instance.RemoveListener(this);
    }

    void CacheStates()
    {
        huntState = SafeGetState<AIS_SkulkHunt>(huntStateIndex);
        chargeState = SafeGetState<AIS_SkulkCharge>(chargeStateIndex);
        latchedState = SafeGetState<AIS_SkulkLatched>(latchedStateIndex);
        disengageState = SafeGetState<AIS_SkulkDisengage>(disengageStateIndex);
        ceilingWaitState = SafeGetState<AIS_SkulkCeilingWait>(ceilingWaitStateIndex);
    }

    public void OnSoundHeard(AudioSoundEvent soundEvent)
    {
        if (!isServer || isDying) return;

        float distance = Vector3.Distance(soundEvent.position, transform.position);
        if (distance > hearingOverride[soundEvent.category]) return;

        if (disengageState != null & CurrentState == disengageState) return;
        if (latchedState != null && CurrentState == latchedState) return;
        if (ceilingWaitState != null && CurrentState == ceilingWaitState) return;
        if (soundEvent.source == gameObject) return;
        if (!soundEvent.source.CompareTag("Player")) return;

        if (!soundEvent.source.TryGetComponent(out PlayerData player)) return;

        lastHeardPlayer = player;

        if (CurrentState == states[wanderStateIndex] ||
            CurrentState == states[ceilingWaitStateIndex])
            TriggerHunt(player);
    }

    public void Burrow(bool burrow) => burrowValue = burrow? 100 : 0;
    public void EnableFin(bool enable) => finValue = enable ? 0 : 100;

    private void OnBurrowValueChanged(float _, float newValue) => renderer_.SetBlendShapeWeight(burrowKeyIndex, newValue);
    private void OnFinValueChanged(float _, float newValue) => renderer_.SetBlendShapeWeight(hideFinKeyIndex, newValue);

    protected override void Update()
    {
        base.Update();
        AnimateEmission();

        huntTimer = Mathf.Max(huntTimer - Time.deltaTime, 0);
    }

    void AnimateEmission()
    {
        if (renderer_ == null) return;

        emissionTimer += Time.deltaTime * emissionSpeed;

        float t = Mathf.PingPong(emissionTimer, 1f);

        t = Mathf.SmoothStep(0f, 1f, t);

        Color emissionColor = Color.white * t * 5f;

        renderer_.GetPropertyBlock(mpb);
        mpb.SetColor("_EmissionColor", emissionColor);
        renderer_.SetPropertyBlock(mpb);
    }

    public void ResumeWander()
    {
        Burrow(true);
        EnableFin(false);

        if (TryAttachToCeiling())
            return;

        SetState(states[wanderStateIndex]);
    }

    public void TriggerHunt(PlayerData target)
    {
        if (huntState == null || huntTimer > 0) return;
        huntTimer = huntCooldown;

        huntState.Target = target;
        SetState(states[huntStateIndex]);
    }

    public void TriggerCharge(PlayerData target)
    {
        if (chargeState == null) return;
        if (latchedState != null && latchedState.OnCooldown) return;

        chargeState.Target = target;
        SetState(states[chargeStateIndex]);
    }

    public void TriggerLatched(PlayerData target)
    {
        if (latchedState == null) return;
        latchedState.Target = target;
        SetState(states[latchedStateIndex]);
    }

    public void TriggerDisengage()
    {
        SetState(states[disengageStateIndex]);
    }

    public void TryAttackToCeiling() => TryAttachToCeiling();

    bool TryAttachToCeiling()
    {
        float roll = (float)new System.Random().NextDouble();
        if (roll > ceilingAttachChance) return false;

        if (!Physics.Raycast(transform.position, Vector3.up,
                out RaycastHit hit, ceilingRayDistance, ceilingLayer)) return false;

        if (ceilingWaitState == null) return false;
        ceilingWaitState.CeilingPoint = hit.point;
        SetState(states[ceilingWaitStateIndex]);
        return true;
    }

    public override void OnAgentHurt(AttackEvent source)
    {
        base.OnAgentHurt(source);

        if (latchedState != null && CurrentState == latchedState)
            latchedState.OnParasiteHurt();
    }

    public override void OnAgentDeath(AttackEvent source)
    {
        base.OnAgentDeath(source);

        if (latchedState != null && CurrentState == latchedState)
            TriggerDisengage();
    }

    public void OnTriggerDetected(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        chargeState.OnTriggerDetected(other, this);
    }

    private void OnCollisionEnter(Collision collision)
    {
        chargeState.EnterStun(this);
    }
}
