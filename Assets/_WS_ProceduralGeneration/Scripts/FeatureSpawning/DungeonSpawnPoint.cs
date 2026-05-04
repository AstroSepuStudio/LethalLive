using UnityEngine;

/// <summary>
/// Place this on a GameObject inside a room prefab and assign the channel
/// that matches the spawner that should consume it.
/// </summary>
public class DungeonSpawnPoint : MonoBehaviour
{
    [Tooltip("Must match the channel on a DungeonSpawner child of the generator.")]
    [SerializeField] SpawnChannel channel;

    [Header("Placement Variation")] [Tooltip("Maximum random offset applied to the spawn position.")]
    public Vector3 maxOffset = Vector3.one * 0.25f;

    [Tooltip("Maximum random Y rotation added on top of the point's own rotation (degrees).")]
    public float maxRotation = 0f;

    [Header("Spawn Chance")] [Tooltip("Base percentage chance this point produces a spawn (0–100).")]
    [Range(0f, 100f)] public float chance = 100f;

    [Tooltip("How many independent spawn attempts to make at this point.")]
    [Min(1)] public int tries = 1;

    [Header("Surface Snapping")] [Tooltip("Enable snapping.")]
    public bool snapToSurface = false;

    [Tooltip("Snap directions.")]
    public Direction[] snapDirections = { Direction.Down };

    [Tooltip("How far to raycast when looking for a surface.")]
    [Min(0.01f)] public float snapMaxDistance = 2f;

    [Tooltip("Layers considered as valid snap surfaces.")]
    public LayerMask snapLayers = Physics.DefaultRaycastLayers;

    [Tooltip("Extra offset applied along the snap normal after hitting a surface (useful to avoid z-fighting).")]
    public float snapSurfaceOffset = 0f;

    [Tooltip("Rotate the spawned object so its -Y axis aligns with the hit surface normal.")]
    public bool alignToNormal = false;

    [Tooltip("Which snap direction's normal to align to. If null, all hit normals are blended together.")]
    public bool useSpecificNormalDirection = false;
    public Direction normalAlignDirection = Direction.Down;

    [Tooltip("Which local axis of the spawned prefab points 'away' from the surface. Y = standing upright on a floor, Z = back flush against a wall, X = side against a wall.")]
    public Direction prefabUpAxis = Direction.Up;

    public SpawnChannel Channel => channel;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Color c = channel != null ? ColorForChannel(channel) : Color.grey;

        Gizmos.color = new Color(c.r, c.g, c.b, 0.85f);
        Gizmos.DrawWireCube(transform.position, maxOffset * 2f);

        Gizmos.color = new Color(c.r, c.g, c.b, 0.4f);
        Gizmos.DrawSphere(transform.position, 0.08f);

        if (maxRotation > 0f)
        {
            UnityEditor.Handles.color = new Color(c.r, c.g, c.b, 0.5f);
            UnityEditor.Handles.DrawWireArc(
                transform.position,
                Vector3.up,
                Quaternion.Euler(0f, -maxRotation, 0f) * transform.forward,
                maxRotation * 2f,
                0.3f);
        }

        if (snapToSurface && snapDirections != null)
        {
            foreach (var dir in snapDirections)
            {
                Vector3 rayDir = (Vector3)DirectionUtils.DirectionVector(dir);
                bool hit = Physics.Raycast(transform.position, rayDir,
                    out RaycastHit hitInfo, snapMaxDistance, snapLayers);

                Gizmos.color = hit ? Color.green : new Color(1f, 0.5f, 0f, 0.8f);
                Gizmos.DrawRay(transform.position, rayDir * snapMaxDistance);

                if (hit)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(hitInfo.point, 0.06f);

                    if (alignToNormal)
                    {
                        UnityEditor.Handles.color = new Color(0f, 1f, 0.5f, 0.7f);
                        UnityEditor.Handles.ArrowHandleCap(0,
                            hitInfo.point,
                            Quaternion.LookRotation(hitInfo.normal),
                            0.25f, EventType.Repaint);
                    }
                }
            }
        }
    }

    static Color ColorForChannel(SpawnChannel ch)
    {
        int h = ch.name.GetHashCode();
        return Color.HSVToRGB(Mathf.Abs(h % 1000) / 1000f, 0.8f, 0.9f);
    }
#endif
}
