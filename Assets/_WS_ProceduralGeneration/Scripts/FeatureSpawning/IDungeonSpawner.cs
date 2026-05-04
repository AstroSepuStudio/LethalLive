using System.Collections.Generic;

/// <summary>
/// Contract that <see cref="DungeonGenerator"/> uses to drive every spawn category.
/// Implement this on a MonoBehaviour and attach it as a child of the generator GameObject.
/// The generator discovers all implementors automatically — no registration needed.
/// </summary>
public interface IDungeonSpawner
{
    /// <summary>Channel this spawner is responsible for.</summary>
    SpawnChannel Channel { get; }

    /// <summary>
    /// Called once after all rooms are instantiated.
    /// The generator passes every <see cref="DungeonSpawnPoint"/> that belongs to this
    /// spawner's channel; the spawner stores them internally for use during <see cref="Spawn"/>.
    /// </summary>
    void Collect(IEnumerable<DungeonSpawnPoint> points);

    /// <summary>
    /// Execute the actual spawning logic.
    /// Only runs on the server (or always for offline generators).
    /// </summary>
    void Spawn(DungeonGenerator generator);

    /// <summary>Tear down all spawned objects and reset internal state.</summary>
    void Clear();
}
