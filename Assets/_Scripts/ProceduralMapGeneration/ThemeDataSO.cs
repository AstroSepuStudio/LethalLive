using System;
using UnityEngine;

[CreateAssetMenu(menuName = "LethalLive/ThemeData")]
public class ThemeDataSO : ScriptableObject
{
    [Serializable]
    public struct EntitySpawn
    {
        public GameObject entityPrefab;
        public float spawnWeight;
        public int minSpawnCount;
        public int maxSpawnCount;
    }

    public RoomDataSO startingRoom;
    public RoomDataSO[] spawnableRooms;
    public RoomDataSO[] deadEndRooms;
    public ItemSO[] spawnableItems;
    public GameObject[] furniturePrefabs;
    public EntitySpawn[] entitySpawns;

    public int MinItems = 3;
    public int MaxItems = 12;
}
