using UnityEngine;
using UnityEngine.Events;

public class AIS_StareAtVortex : AIState
{
    [SerializeField] float minStareDuration = 1.5f;
    [SerializeField] float maxStareDuration = 3.5f;

    [SerializeField] float turnSpeed = 5f;

    public VortexAI TargetVortex { get; set; }
    public UnityEvent<bool> OnStareDecisionMade;

    float stareTimer;

    public override void OnEnterState(AIBrain brain)
    {
        brain.StopAgentMovement();
        stareTimer = Random.Range(minStareDuration, maxStareDuration);
        brain.Animator_.SetBool("Walk", false);
    }

    public override void OnUpdateState(AIBrain brain)
    {
        if (TargetVortex == null)
        {
            OnStareDecisionMade?.Invoke(false);
            return;
        }

        Vector3 dir = (TargetVortex.transform.position - brain.transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            brain.transform.rotation = Quaternion.Slerp(
                brain.transform.rotation, lookRot, turnSpeed * Time.deltaTime);
        }

        stareTimer -= Time.deltaTime;
        if (stareTimer <= 0f)
        {
            VortexAI self = brain as VortexAI;
            bool shouldFollow = self != null && TargetVortex.Alpha > self.Alpha;
            OnStareDecisionMade?.Invoke(shouldFollow);
        }
    }

    public override void OnExitState(AIBrain brain)
    {
        brain.ResumeAgentMovement();
    }
}
