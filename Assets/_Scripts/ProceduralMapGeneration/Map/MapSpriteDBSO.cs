using UnityEngine;

[CreateAssetMenu(menuName = "LethalLive/Map Sprite Database")]
public class MapSpriteDBSO : ScriptableObject
{
    public enum PartType
    {
        Room, // 1 cell rooms
        Corner, 
        Hallway, 
        Edge
    }

    [System.Serializable]
    public struct MapPart
    {
        public Sprite partSprite;
        public PartType partTypes;
        public Direction[] partPorts;
    }

    [SerializeField] MapPart[] mapParts;
}
