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

    [Serializable]
    public struct EerySFX
    {
        public AudioSFX clip;
        public float weight;
    }

    public string levelName;

    [Header("Room Generation")]
    public RoomDataSO startingRoom;
    public RoomDataSO[] spawnableRooms;
    public RoomDataSO[] deadEndRooms;

    [Header("Features Generation")]
    public ItemSO[] spawnableItems;
    public GameObject[] furniturePrefabs;
    public EntitySpawn[] entitySpawns;

    [Header("Ambient")]
    public AudioSFX loopingMusic;
    public EerySFX[] eerySFX;

    public int MinItems = 3;
    public int MaxItems = 12;
}
