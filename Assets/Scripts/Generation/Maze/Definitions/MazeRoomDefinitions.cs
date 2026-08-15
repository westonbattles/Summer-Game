using UnityEngine;
using System;

public enum MazeCellType
{
    Normal,     // normal cell
    Boss        // boss room cell
}

// wall openings in a cell (cell and the nsew direction of the wall in that cell that has the opening)
[Serializable]
public struct MazeOpeningDefinition
{
    public Vector2Int Cell;
    public GridDirection Direction;
}

[CreateAssetMenu(fileName = "Maze Room", menuName = "Maze Generation/Objects/Room")]
public class MazeRoomDefinition : ScriptableObject
{
    public string DisplayName;
    public GameObject Prefab;
    public MazeCellType Type = MazeCellType.Normal;

    [Header("Properties")]
    //[Tooltip("Occupied cells / Shape")]
    // not sure which direction to go down yet for this.
    //public Vector2Int[] OccupiedCells = { Vector2Int.zero }; -> local cell offsets, benefit would be using this as local cell locations from current generation spot
    //public MazeShapeEnum Shape = MazeShapeEnum.1x1, 2x2, etc
    [Tooltip("Opening Opprotunities")]
    public MazeOpeningDefinition[] Openings;
    
    public float SpawnWeight = 1f;
}

//[CreateAssetMenu(menuName = "Maze Generation/Objects/Room")]
