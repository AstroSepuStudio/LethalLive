using UnityEngine;
using UnityEngine.Events;

public class AIS_BackAwayFromPlayer : AIState
{
    [SerializeField] float backAwayDistance = 6f;
    [SerializeField] float safeDistance = 5f;
    [SerializeField] float giveUpDuration = 5f;
    [SerializeField] float recalculateInterval = 0.4f;

    float giveUpTimer;
    float recalcTimer;

    public UnityEvent OnSafe;
    public UnityEvent OnGaveUp;

    public override void OnEnterState(AIBrain brain)
    {
        giveUpTimer = giveUpDuration;
        recalcTimer = 0f;
        brain.ResumeAgentMovement();
    }

    public override void OnUpdateState(AIBrain brain)
    {
        VortexAI vortex = brain as VortexAI;
        if (vortex == null) { OnSafe?.Invoke(); return; }

        PlayerData closest = vortex.GetClosestSeenPlayer();

        if (closest == null) { OnSafe?.Invoke(); return; }

        float dist = Vector3.Distance(brain.transform.position, closest.transform.position);

        if (dist >= safeDistance)
        {
            OnSafe?.Invoke();
            return;
        }

        Vector3 lookDir = (closest.transform.position - brain.transform.position).normalized;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
            brain.transform.rotation = Quaternion.LookRotation(lookDir);

        recalcTimer -= Time.deltaTime;
        if (recalcTimer <= 0f)
        {
            recalcTimer = recalculateInterval;
            Vector3 awayDir = (brain.transform.position - closest.transform.position).normalized;
            brain.MoveAgent(brain.transform.position + awayDir * backAwayDistance);
        }

        giveUpTimer -= Time.deltaTime;
        if (giveUpTimer <= 0f)
            OnGaveUp?.Invoke();

        brain.Animator_.SetBool("Walk", brain.IsAgentInMovement());
    }

    public override void OnExitState(AIBrain brain)
    {
        brain.Animator_.SetBool("Walk", false);
    }
}
