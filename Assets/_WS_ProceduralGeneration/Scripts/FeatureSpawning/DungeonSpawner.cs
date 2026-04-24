using Mirror;
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
        float roll = (float)(generator.RNG.NextDouble() * 100f);

        float effective = point.chance * generator.GetDificultyMultiplier(point.transform.position);
        return roll <= effective;
    }

    protected static Vector3 ResolvePosition(DungeonSpawnPoint point, System.Random rng)
    {
        Vector3 pos = RandomisedPosition(point, rng);
        if (!point.snapToSurface || point.snapDirections == null || point.snapDirections.Length == 0)
            return pos;
        return TrySnap(pos, point, out Vector3 snapped) ? snapped : pos;
    }

    protected static Quaternion ResolveRotation(DungeonSpawnPoint point, System.Random rng)
    {
        if (!point.snapToSurface || !point.alignToNormal ||
            point.snapDirections == null || point.snapDirections.Length == 0)
            return RandomisedRotation(point, rng);

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

        if (blendedNormal == Vector3.zero) return RandomisedRotation(point, rng);

        blendedNormal.Normalize();
        Vector3 prefabUp = (Vector3)DirectionUtils.DirectionVector(point.prefabUpAxis);
        Quaternion normalRot = Quaternion.FromToRotation(prefabUp, blendedNormal);
        float ySpin = (float)(rng.NextDouble() * 2f - 1f) * point.maxRotation;
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

            Vector3 displacement = hit.point - origin + hit.normal * point.snapSurfaceOffset;
            Vector3 axisContribution = Vector3.Scale(displacement, Abs(rayDir));
            result += axisContribution;

            anyHit = true;
        }

        return anyHit;
    }

    static Vector3 Abs(Vector3 v) => new(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    static Vector3 RandomisedPosition(DungeonSpawnPoint point, System.Random rng)
    {
        Vector3 o = point.maxOffset;
        return point.transform.position + new Vector3(
            (float)(rng.NextDouble() * 2f - 1f) * o.x,
            (float)(rng.NextDouble() * 2f - 1f) * o.y,
            (float)(rng.NextDouble() * 2f - 1f) * o.z);
    }

    static Quaternion RandomisedRotation(DungeonSpawnPoint point, System.Random rng) =>
        point.transform.rotation * Quaternion.Euler(
            0f, (float)(rng.NextDouble() * 2f - 1f) * point.maxRotation, 0f);

    protected void DestroyChildren(Transform parent, bool hasNetID, bool isServer, bool ignoreFirst = false)
    {
        if (parent == null) return;
        if (hasNetID && !isServer) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            if (i == 0 && ignoreFirst) continue;
            var child = parent.GetChild(i).gameObject;
            if (hasNetID) NetworkServer.Destroy(child);
            else Destroy(child);
        }
    }
}
