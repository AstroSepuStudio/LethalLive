using UnityEngine;

/// <summary>
/// A ScriptableObject used as a typed key that connects <see cref="DungeonSpawnPoint"/>s
/// in rooms to the <see cref="DungeonSpawner"/> that consumes them.
///
/// Create one asset per category (e.g. "Furniture", "Loot", "Decoration", "Enemies")
/// via Assets → Create → DungeonGen → Spawn Channel.
/// Assign the same asset to spawn points in rooms and to the spawner component.
/// </summary>
[CreateAssetMenu(menuName = "DungeonGen/Spawn Channel", fileName = "SpawnChannel_New")]
public class SpawnChannel : ScriptableObject { }
