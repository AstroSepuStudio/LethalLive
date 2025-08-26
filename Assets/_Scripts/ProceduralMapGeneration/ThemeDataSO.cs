using UnityEngine;

[CreateAssetMenu(menuName = "LethalLive/ThemeData")]
public class ThemeDataSO : ScriptableObject
{
    public RoomDataSO startingRoom;
    public RoomDataSO[] spawnableRooms;
    public RoomDataSO[] deadEndRooms;
    public ItemSO[] spawnableItems;

    public int MinItems = 3;
    public int MaxItems = 12;
}
