using Mirror;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Gives an AI a numeric alpha rank that scales its stats and drives pack leadership.
/// Requires AIModule_Home and AIModule_Patience to also be present.
/// </summary>
public class AIModule_Alpha : AIModule
{
    [SyncVar(hook = nameof(OnAlphaChanged))]
    int alphaValue = -1;

    public float AlphaValue => alphaValue;
    public bool IsActingAsAlpha => isActingAsAlpha;

    bool isActingAsAlpha;
    bool pendingFollowerDispatch;

    readonly List<AIBrain> pendingFollowers = new();
    readonly List<AIBrain> packMembers = new();

    public override void OnModuleInit(AIBrain brain)
    {
        if (!brain.isServer) return;

        alphaValue = Random.Range(0, 100);

        var patience = brain.GetModule<AIModule_Patience>();
        patience?.AssignPersonalityFromValue(alphaValue);

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

    public void BecomeLeaderOf(AIBrain follower, AIBrain selfBrain)
    {
        isActingAsAlpha = true;

        var myHome = selfBrain.GetModule<AIModule_Home>();
        var followerHome = follower.GetModule<AIModule_Home>();
        followerHome?.SetOverride(myHome?.HomeRoom);

        pendingFollowers.Add(follower);
        packMembers.Add(follower);
        pendingFollowerDispatch = true;
    }

    public void RemoveFromPack(AIBrain follower) => packMembers.Remove(follower);

    public void DispatchPendingFollowers()
    {
        if (!pendingFollowerDispatch) return;
        pendingFollowerDispatch = false;

        foreach (var follower in pendingFollowers)
            follower.GetModule<AIModule_Alpha>()?.OnDispatchedByAlpha(follower);

        pendingFollowers.Clear();
    }

    public void OnDispatchedByAlpha(AIBrain brain)
    {
        brain.OnModuleEvent(AIBrain.ModuleEvent.BeginSearch);
    }

    public void AlertPack(AIBrain selfBrain, float rangeMultiplier = 2f)
    {
        foreach (var member in packMembers)
        {
            if (member == null) continue;
            float dist = Vector3.Distance(selfBrain.transform.position, member.transform.position);
            float radius = (selfBrain.GetModule<AIModule_Senses>()?.DetectionRadius ?? 10f) * rangeMultiplier;
            if (dist > radius) continue;
            member.OnModuleEvent(AIBrain.ModuleEvent.RespondToAlphaCall, selfBrain);
        }
    }

    void OnAlphaChanged(int _, int newVal)
    {
        float scale = Mathf.Lerp(0.6f, 1.2f, newVal / 100f);
        transform.localScale = Vector3.one * scale;
    }
}
