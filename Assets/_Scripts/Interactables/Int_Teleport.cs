using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class Int_Teleport : InteractableObject
{
    [SerializeField] Transform targetPosition;
    [SerializeField] float minDistance = 1f;
    [SerializeField] float maxDistance = 4f;
    [SerializeField] AudioSFX musicSFX;

    public readonly UnityEvent<PlayerData> OnPlayerTeleports;

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
    
    public void Teleport(PlayerData sourceData)
    {
        if (requireGameStarted && !GameManager.Instance.dayStarted) return;

        Vector3 desiredPosition = Vector3.zero;
        bool foundValidPos = false;
        int tries = 0;

        while (!foundValidPos && tries < 5)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minDistance, maxDistance);
            Vector3 offset = new Vector3(randomCircle.x, 0f, randomCircle.y) * distance;

            desiredPosition = targetPosition.position + offset;
            Vector3 sourcePosition = sourceData.Character_Controller.transform.position + Vector3.up * 1.5f;
            Vector3 rayDir = (desiredPosition - sourcePosition).normalized;

            if (Physics.Raycast(sourcePosition, rayDir, out RaycastHit hit, Vector3.Distance(sourcePosition, desiredPosition)))
            {
                if (hit.distance >= minDistance + 0.5f)
                {
                    desiredPosition = sourcePosition + rayDir * (hit.distance - 0.5f);

                    foundValidPos = true;
                    break;
                }
                tries++;
            }

            foundValidPos = true;
        }

        if (!foundValidPos) return;

        sourceData.Character_Controller.enabled = false;
        sourceData.Character_Controller.transform.position = desiredPosition;
        sourceData.Character_Controller.enabled = true;

        canvas.DisableCanvas();
        
        GameManager.Instance.OnEnterDungeon(sourceData);
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
