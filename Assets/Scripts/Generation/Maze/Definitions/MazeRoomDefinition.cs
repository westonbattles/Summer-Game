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
    [Tooltip("Readable ID (0x0_CamalCaseRoomID): EG, 1x3_Crypt, 2x2_KnightPuzzle, L_Cavern")]
    public string DefinitionId = "1x1_Room";
    public string DisplayName = "Room";
    public GameObject Prefab;
    public MazeCellType Type = MazeCellType.Normal;

    [Header("Properties")]
    [Tooltip("Local occupied cells (Shape). 2x2 would occupy (0,0), (0,1), (1,0), (1,1) for example.")]
    // instead use public MazeShapeEnum Shape = MazeShapeEnum.1x1, 2x2, etc?
    public Vector2Int[] OccupiedCells = { Vector2Int.zero }; // -> local cell offsets, benefit would be using this as local cell locations from current generation spot
    [Tooltip("Opening Opprotunities")]
    public MazeOpeningDefinition[] Openings;
    
    public float SpawnWeight = 1f;
}

//[CreateAssetMenu(menuName = "Maze Generation/Objects/Room")]
