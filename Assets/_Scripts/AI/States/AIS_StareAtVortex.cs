using UnityEngine;
using UnityEngine.Events;

public class AIS_StareAtVortex : AIState
{
    [SerializeField] float minStareDuration = 1.5f;
    [SerializeField] float maxStareDuration = 3.5f;
    [SerializeField] float turnSpeed = 5f;

    public AIBrain TargetBrain { get; set; }

    public UnityEvent<bool> OnStareDecisionMade;

    float stareTimer;

    public override void OnEnterState(AIBrain brain)
    {
        brain.StopAgentMovement();
        stareTimer = Random.Range(minStareDuration, maxStareDuration);
        brain.Animator_.SetBool("Walk", false);
        brain.SetIdleState(true);
    }

    public override void OnUpdateState(AIBrain brain)
    {
        if (TargetBrain == null) { OnStareDecisionMade?.Invoke(false); return; }

        Vector3 dir = (TargetBrain.transform.position - brain.transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
        {
            brain.transform.rotation = Quaternion.Slerp(
                brain.transform.rotation,
                Quaternion.LookRotation(dir),
                turnSpeed * Time.deltaTime);
        }

        stareTimer -= Time.deltaTime;
        if (stareTimer > 0f) return;

        var selfAlpha = brain.GetModule<AIModule_Alpha>();
        var targetAlpha = TargetBrain.GetModule<AIModule_Alpha>();

        if (selfAlpha == null || targetAlpha == null)
        {
            OnStareDecisionMade?.Invoke(false);
            return;
        }

        float selfValue = selfAlpha.IsActingAsAlpha
            ? selfAlpha.AlphaValue
            : brain.GetModule<AIModule_Alpha>()?.AlphaValue ?? 0f;

        var followState = brain.GetComponentInChildren<AIS_FollowAlpha>();
        if (followState?.AlphaTarget != null)
            selfValue = followState.AlphaTarget.GetModule<AIModule_Alpha>()?.AlphaValue ?? selfValue;

        bool shouldFollow = targetAlpha.AlphaValue > selfValue;
        OnStareDecisionMade?.Invoke(shouldFollow);
    }

    public override void OnExitState(AIBrain brain)
    {
        brain.ResumeAgentMovement();
    }
}
