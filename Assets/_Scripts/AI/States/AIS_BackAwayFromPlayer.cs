using UnityEngine.Events;
using UnityEngine;

public class AIS_BackAwayFromPlayer : AIState
{
    [SerializeField] float backAwayDistance = 6f;
    [SerializeField] float safeDistance = 5f;
    [SerializeField] float giveUpDuration = 5f;
    [SerializeField] float recalculateInterval = 0.4f;

    float giveUpTimer;
    float recalcTimer;

    public Vector3? OverrideTarget { get; set; }

    public UnityEvent OnSafe;
    public UnityEvent OnGaveUp;

    public override void OnEnterState(AIBrain brain)
    {
        giveUpTimer = giveUpDuration;
        recalcTimer = 0f;
        brain.ResumeAgentMovement();
        brain.SetIdleState(false);
    }

    public override void OnUpdateState(AIBrain brain)
    {
        Vector3 threatPos;

        if (OverrideTarget.HasValue)
        {
            threatPos = OverrideTarget.Value;
        }
        else
        {
            if (!brain.TryGetModule<AIModule_Senses>(out var senses)) { OnSafe?.Invoke(); return; }
            PlayerData closest = senses.GetClosestSeenPlayer(brain);
            if (closest == null) { OnSafe?.Invoke(); return; }
            threatPos = closest.transform.position;
        }

        float dist = Vector3.Distance(brain.transform.position, threatPos);

        if (dist >= safeDistance)
        {
            OnSafe?.Invoke();
            return;
        }

        Vector3 lookDir = (threatPos - brain.transform.position).normalized;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
            brain.transform.rotation = Quaternion.LookRotation(lookDir);

        recalcTimer -= Time.deltaTime;
        if (recalcTimer <= 0f)
        {
            recalcTimer = recalculateInterval;
            Vector3 awayDir = (brain.transform.position - threatPos).normalized;
            brain.MoveAgent(brain.transform.position + awayDir * backAwayDistance);
        }

        giveUpTimer -= Time.deltaTime;
        if (giveUpTimer <= 0f)
            OnGaveUp?.Invoke();

        brain.Animator_.SetBool("Walk", brain.IsAgentInMovement());
    }

    public override void OnExitState(AIBrain brain)
    {
        OverrideTarget = null;
        brain.SetIdleState(true);
        brain.Animator_.SetBool("Walk", false);
    }
}
