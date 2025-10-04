using Mirror;
using UnityEngine;

public class Int_Teleport : InteractableObject
{
    [SerializeField] Transform targetPosition;
    [SerializeField] float minDistance = 1f;
    [SerializeField] float maxDistance = 4f;
    [SerializeField] AudioSFX musicSFX;

    [SyncVar]
    [SerializeField] bool requireGameStarted;

    [Header("Debug")]
    [SerializeField] bool _displayRing;

    public void SetTeleportPos(Vector3 pos)
    {
        targetPosition.position = pos;
    }

    public void SetParent(Transform parent)
    {
        targetPosition.parent = parent;
        targetPosition.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }
    // [Server]
    public override void OnInteract(PlayerData sourceData)
    {
        if (requireGameStarted && !GameManager.Instance.dayStarted) return;

        sourceData.Character_Controller.enabled = false;

        Vector2 randomCircle = Random.insideUnitCircle.normalized;
        float distance = Random.Range(minDistance, maxDistance);
        Vector3 offset = new Vector3(randomCircle.x, 0f, randomCircle.y) * distance;
        sourceData.Character_Controller.transform.position = targetPosition.position + offset;

        sourceData.Character_Controller.enabled = true;

        DisableCanvas();

        AudioManager.Instance.PlayMusic(musicSFX);

        OnInteractEvent?.Invoke(sourceData);
    }

    private void OnDrawGizmos()
    {
        if (targetPosition == null || !_displayRing) return;

        Gizmos.color = Color.green;
        DrawCircle(targetPosition.position, maxDistance);

        Gizmos.color = Color.red;
        DrawCircle(targetPosition.position, minDistance);
    }

    private void DrawCircle(Vector3 center, float radius, int segments = 64)
    {
        Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}
