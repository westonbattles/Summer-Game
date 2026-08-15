using UnityEngine;

/*

utils for defining rotations / directions for rooms

*/

public enum RoomRotation
{
    R0,
    R90,
    R180,
    R270
}

public enum GridDirection
{
    North,
    East,
    South,
    West
}

public struct GridDirectionUtils
{
    /// just takes in grid direction and gives us the coorseponding i,j offset
    public static Vector2Int ToGridOffset(GridDirection direction)
    {
        return direction switch
        {
            GridDirection.North => Vector2Int.up,
            GridDirection.East => Vector2Int.right,
            GridDirection.South => Vector2Int.down,
            GridDirection.West => Vector2Int.left,
            _ => Vector2Int.zero
        };
    }

    public static Vector3 ToWorldDirection(GridDirection direction)
    {
        return direction switch
        {
            GridDirection.North => Vector3.forward,
            GridDirection.East => Vector3.right,
            GridDirection.South => Vector3.back,
            GridDirection.West => Vector3.left,
            _ => Vector3.zero
        };
    }

    public static GridDirection Opposite(GridDirection direction)
    {
        return direction switch
        {
            GridDirection.North => GridDirection.South,
            GridDirection.East => GridDirection.West,
            GridDirection.South => GridDirection.North,
            GridDirection.West => GridDirection.East,
            _ => direction
        };
    }

    public static Quaternion ToRotation(RoomRotation rotation)
    {
        return Quaternion.Euler(0f, (int)rotation * 90f, 0f);
    }
    
    public static Quaternion ToRotation(GridDirection direction)
    {
        return Quaternion.Euler(0f, (int)direction * 90f, 0f);
    }

    public static GridDirection RotateDirection(GridDirection direction, RoomRotation rotation)
    {
        int rotated = (int)direction + (int)rotation;
        return (GridDirection)(rotated % 4);
    }
}
