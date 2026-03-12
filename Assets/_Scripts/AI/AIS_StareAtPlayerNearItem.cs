using UnityEngine;
using UnityEngine.Events;

public class AIS_StareAtPlayerNearItem : AIState
{
    [SerializeField] float waitDuration = 8f;
    [SerializeField] float checkInterval = 0.5f;
    [SerializeField] float playerClearDistance = 4f;
    [SerializeField] float patienceDecayRate = 4f;
    [SerializeField] float minWarningInterval = 2f;
    [SerializeField] float maxWarningInterval = 4f;

    float waitTimer;
    float checkTimer;
    float warningTimer;

    public ItemBase WatchedItem { get; set; }
    public PlayerData BlockingPlayer { get; set; }

    public UnityEvent OnPlayerLeft;
    public UnityEvent OnGaveUp;
    public UnityEvent OnItemStolen;

    public override void OnEnterState(AIBrain brain)
    {
        waitTimer = waitDuration;
        checkTimer = 0f;
        warningTimer = Random.Range(minWarningInterval, maxWarningInterval);
    }

    public override void OnUpdateState(AIBrain brain)
    {
        if (BlockingPlayer != null)
        {
            Vector3 dir = (BlockingPlayer.transform.position - brain.transform.position).normalized;
            dir.y = 0f;
            if (dir != Vector3.zero)
                brain.transform.rotation = Quaternion.LookRotation(dir);

            warningTimer -= Time.deltaTime;
            if (warningTimer <= 0f)
            {
                warningTimer = Random.Range(minWarningInterval, maxWarningInterval);
                brain.Animator_.SetTrigger("Attack");
                brain.PlaySFX(AIBrain.SFXEvent.Warning, 1);
            }

            VortexAI vortex = brain as VortexAI;
            if (vortex != null)
            {
                vortex.DrainPatience(patienceDecayRate * Time.deltaTime);
                if (vortex.Patience <= 0f) return;
            }
        }

        waitTimer -= Time.deltaTime;
        if (waitTimer <= 0f) { OnGaveUp?.Invoke(); return; }

        checkTimer -= Time.deltaTime;
        if (checkTimer > 0f) return;
        checkTimer = checkInterval;

        if (WatchedItem == null || !WatchedItem.ItemData.pickable)
        {
            OnGaveUp?.Invoke();
            return;
        }

        if (WatchedItem.HasOwner)
        {
            OnItemStolen?.Invoke();
            return;
        }

        if (BlockingPlayer == null) { OnPlayerLeft?.Invoke(); return; }

        float dist = Vector3.Distance(BlockingPlayer.transform.position, WatchedItem.transform.position);
        if (dist >= playerClearDistance)
            OnPlayerLeft?.Invoke();
    }

    public override void OnExitState(AIBrain brain) { }
}
