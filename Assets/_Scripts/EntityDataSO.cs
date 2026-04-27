using UnityEngine;
using static LL_Tier;

[CreateAssetMenu(menuName = "LethalLive/Entity Data")]
public class EntityDataSO : ScriptableObject
{
    public Tier EntityTier;
    public GameObject entityPrefab;
    [Range(1, 5)] public int minSpawnCount;
    [Range(1, 5)] public int maxSpawnCount;
}
