using UnityEngine;

public enum Direction { North, South, East, West, Up, Down }

public static class DirectionUtils
{
    public static Vector3Int DirectionVector(Direction d) => d switch
    {
        Direction.North => new Vector3Int(0, 0, 1),
        Direction.South => new Vector3Int(0, 0, -1),
        Direction.East => new Vector3Int(1, 0, 0),
        Direction.West => new Vector3Int(-1, 0, 0),
        Direction.Up => new Vector3Int(0, 1, 0),
        Direction.Down => new Vector3Int(0, -1, 0),
        _ => Vector3Int.zero
    };


    public static Direction OppositeDirection(Direction d) => d switch
    {
        Direction.North => Direction.South,
        Direction.South => Direction.North,
        Direction.East => Direction.West,
        Direction.West => Direction.East,
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        _ => Direction.North
    };
}
