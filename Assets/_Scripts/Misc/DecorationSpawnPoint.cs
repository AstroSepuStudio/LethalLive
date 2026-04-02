using UnityEngine;

public class DecorationSpawnPoint : MonoBehaviour
{
    public Vector3 maxOffset = Vector3.zero;
    public ThemeDataSO.SpawnableSize maxSize;
    public float maxRotation = 360f;
    public float chance = 0f;
    public int tries = 1;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.95f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(transform.position, maxOffset * 2f);

        Gizmos.color = new Color(1f, 0.95f, 0.2f, 0.4f);
        Gizmos.DrawSphere(transform.position, 0.08f);

        if (maxRotation > 0f)
        {
            UnityEditor.Handles.color = new Color(1f, 0.8f, 0f, 0.5f);
            UnityEditor.Handles.DrawWireArc(
                transform.position,
                Vector3.up,
                Quaternion.Euler(0f, -maxRotation, 0f) * transform.forward,
                maxRotation * 2f,
                0.3f);
        }
    }
#endif
}
