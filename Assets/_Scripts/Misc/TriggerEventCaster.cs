using UnityEngine;
using UnityEngine.Events;

public class TriggerEventCaster : MonoBehaviour
{
    [SerializeField] Collider TriggerBox;

    public UnityEvent<Collider> OnTriggerEnterEvent;
    public UnityEvent<Collider> OnTriggerExitEvent;

    private void OnTriggerEnter(Collider other) => OnTriggerEnterEvent?.Invoke(other);
    private void OnTriggerExit(Collider other) => OnTriggerExitEvent?.Invoke(other);

    public void EnableTrigger(bool enable) => TriggerBox.enabled = enable;
}
