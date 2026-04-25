using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class AIModule_Alpha : AIModule
{
    [SyncVar(hook = nameof(OnAlphaChanged))]
    int alphaValue = -1;

    public float AlphaValue => alphaValue;
    public bool IsActingAsAlpha => isActingAsAlpha;
    public UnityEvent<float> OnAlphaValueChanged;

    bool isActingAsAlpha;
    AIBrain brain;

    public override void OnModuleInit(AIBrain brain)
    {
        this.brain = brain;
        if (!brain.isServer) return;

        alphaValue = Random.Range(0, 100);

        var patience = brain.GetModule<AIModule_Patience>();
        if (patience != null) patience.AssignPersonalityFromValue(alphaValue);

        float am = GetMultiplier();
        brain.EntityStats_.OverrideMaxHP(brain.EntityStats_.maxHP * am, true);

        brain.AttackStat_ = new(
            Mathf.Clamp(brain.AttackStat_.AttackRadius * am, 1.1f, 2f),
            brain.AttackStat_.AttackKnock,
            brain.AttackStat_.AttackForce * am,
            brain.AttackStat_.AttackDamage * am,
            brain.AttackStat_.AttackCooldown);
    }

    public float GetMultiplier() => Mathf.Lerp(0.5f, 1.5f, alphaValue / 100f);

    public float GetPitch() => 2f - GetMultiplier();

    void OnAlphaChanged(int _, int newVal)
    {
        if (brain == null) { Debug.Log("[AlphaModule] Brain is null"); return; }

        OnAlphaValueChanged?.Invoke(newVal);
        float scale = Mathf.Lerp(0.6f, 1.2f, newVal / 100f);
        brain.transform.localScale = Vector3.one * scale;
    }

    public void SetActingAsAlpha(bool value) => isActingAsAlpha = value;
}
