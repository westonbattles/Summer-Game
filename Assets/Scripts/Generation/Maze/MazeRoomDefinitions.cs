using UnityEngine;
using System;

public enum MazeCellType
{
    Normal,     // normal cell
    Entrance,   // one of the four entrance/starting cells
    Start,      // starting area cell
    Boss        // boss room cell
}

// wall openings per room (local cells stores the room, direction stores the wall that has the opening
[Serializable]
public struct MazeOpeningDefinition
{
    public Vector2Int LocalCell;
    public GridDirection Direction;
}

[CreateAssetMenu(fileName = "Maze Room", menuName = "Maze Generation/Objects/Room")]
public class MazeRoomDefinition : ScriptableObject
{
    public string DisplayName;
    public GameObject Prefab;

    public MazeCellType Type = MazeCellType.Normal;

    [Header("Properties")]
    [Tooltip("Occupied cells before room rotations")]
    public Vector2Int[] OccupiedCells = { Vector2Int.zero };

    //[Tooltip("Allowed rotations: 0,1,2,3")]
    //public int[] AllowedRotations = { 0 };  -: not sure if we want every room to just garuntee all 4 rotations, that probably makes more sense lowkey.
    
    [Header("Opening Slots")]
    public MazeOpeningDefinition[] OpeningSlots;
    
}

//[CreateAssetMenu(menuName = "Maze Generation/Objects/Room")]