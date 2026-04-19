using System.Collections.Generic;
using UnityEngine;

public abstract class DungeonSpawner : MonoBehaviour, IDungeonSpawner
{
    [SerializeField] SpawnChannel channel;

    readonly List<DungeonSpawnPoint> collectedPoints = new();
    protected IReadOnlyList<DungeonSpawnPoint> CollectedPoints => collectedPoints;

    public SpawnChannel Channel => channel;

    public void Collect(IEnumerable<DungeonSpawnPoint> points)
    {
        collectedPoints.Clear();
        foreach (var p in points)
            if (p != null) collectedPoints.Add(p);
        OnCollected();
    }

    public virtual void Spawn(DungeonGenerator generator)
    {
        int spawned = 0;
        foreach (var point in collectedPoints)
            for (int t = 0; t < point.tries; t++)
            {
                if (!EvaluateChance(point, generator)) continue;
                SpawnOne(point, generator);
                spawned++;
            }
        OnSpawnComplete(spawned);
    }

    public virtual void Clear()
    {
        collectedPoints.Clear();
        OnClear();
    }

    // ── Extension points ──────────────────────────────────────────────────

    protected virtual void OnCollected() { }
    protected abstract void SpawnOne(DungeonSpawnPoint point, DungeonGenerator generator);
    protected virtual void OnSpawnComplete(int spawnedCount) { }
    protected virtual void OnClear() { }

    // ── Chance / placement helpers ────────────────────────────────────────

    protected virtual bool EvaluateChance(DungeonSpawnPoint point, DungeonGenerator generator)
    {
        float roll = Random.Range(0f, 100f);
        float effective = point.chance * generator.GetDificultyMultiplier(point.transform.position);
        return roll <= effective;
    }

    /// <summary>
    /// Returns a randomised position from the point's offset settings,
    /// then optionally snaps it to the nearest surface hit along any of
    /// the point's <see cref="DungeonSpawnPoint.snapDirections"/>.
    /// </summary>
    protected static Vector3 ResolvePosition(DungeonSpawnPoint point)
    {
        Vector3 pos = RandomisedPosition(point);
        if (!point.snapToSurface || point.snapDirections == null || point.snapDirections.Length == 0)
            return pos;

        return TrySnap(pos, point, out Vector3 snapped) ? snapped : pos;
    }

    /// <summary>
    /// Returns a rotation derived from the point's maxRotation setting,
    /// then optionally aligns it to the surface normal when
    /// <see cref="DungeonSpawnPoint.alignToNormal"/> is true.
    /// </summary>
    protected static Quaternion ResolveRotation(DungeonSpawnPoint point)
    {
        if (!point.snapToSurface || !point.alignToNormal ||
            point.snapDirections == null || point.snapDirections.Length == 0)
            return RandomisedRotation(point);

        Vector3 origin = point.transform.position;
        Vector3 blendedNormal = Vector3.zero;

        foreach (var dir in point.snapDirections)
        {
            Vector3 rayDir = (Vector3)DirectionUtils.DirectionVector(dir);
            if (!Physics.Raycast(origin, rayDir, out RaycastHit hit,
                    point.snapMaxDistance, point.snapLayers)) continue;

            if (point.useSpecificNormalDirection && dir != point.normalAlignDirection) continue;

            blendedNormal += hit.normal;
        }

        if (blendedNormal == Vector3.zero) return RandomisedRotation(point);

        blendedNormal.Normalize();

        // Treat whichever local axis the user nominated as the prefab's "up"
        Vector3 prefabUp = (Vector3)DirectionUtils.DirectionVector(point.prefabUpAxis);

        // Align that axis to the surface normal, then layer the point's own
        // orientation and the random Y-spin on top
        Quaternion normalRot = Quaternion.FromToRotation(prefabUp, blendedNormal);
        float ySpin = Random.Range(-point.maxRotation, point.maxRotation);
        return normalRot * Quaternion.Euler(0f, point.transform.rotation.eulerAngles.y + ySpin, 0f);
    }

    // ── Internal ──────────────────────────────────────────────────────────

    static bool TrySnap(Vector3 origin, DungeonSpawnPoint point, out Vector3 result)
    {
        result = origin;
        bool anyHit = false;

        foreach (var dir in point.snapDirections)
        {
            Vector3 rayDir = (Vector3)DirectionUtils.DirectionVector(dir);
            if (!Physics.Raycast(origin, rayDir, out RaycastHit hit,
                    point.snapMaxDistance, point.snapLayers)) continue;

            // Project the hit point onto this direction's axis only,
            // then apply that axis displacement to the accumulated result.
            Vector3 displacement = hit.point - origin + hit.normal * point.snapSurfaceOffset;
            Vector3 axisContribution = Vector3.Scale(displacement, Abs(rayDir));
            result += axisContribution;

            anyHit = true;
        }

        return anyHit;
    }

    static Vector3 Abs(Vector3 v) => new(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    static Vector3 RandomisedPosition(DungeonSpawnPoint point)
    {
        Vector3 o = point.maxOffset;
        return point.transform.position + new Vector3(
            Random.Range(-o.x, o.x),
            Random.Range(-o.y, o.y),
            Random.Range(-o.z, o.z));
    }

    static Quaternion RandomisedRotation(DungeonSpawnPoint point) =>
        point.transform.rotation * Quaternion.Euler(
            0f, Random.Range(-point.maxRotation, point.maxRotation), 0f);
}
