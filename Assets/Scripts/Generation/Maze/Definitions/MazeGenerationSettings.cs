using UnityEngine;

[CreateAssetMenu(menuName = "Maze Generation/Settings", fileName = "Maze Generation Settings")]
public class MazeGenerationSettings : ScriptableObject
{
    [Header("Grid")]
    [Min(1)] public int GridWidth = 9;
    [Min(1)] public int GridHeight = 9;
    
    [Header("Physical Room Dimensions")]
    // exterior width of 1x1 room
    [Min(0.01f)] public float CellWidth = 30f; // 30m (apply fbx transform means its 30m in blender as well)
    [Min(0.01f)] public float CellHeight = 30f; // 30m (apply fbx transform means its 30m in blender as well)
    [Min(0.0f)] public float CellGap = 3f; // gap between cells
    
    [Header("Room Opening Dimensions")]
    [Min(0.01f)] public float OpeningWidth = 8f;
    [Min(0.01f)] public float OpeningHeight = 27f;
    
    // automatic cell blocking for starting area
    [Header("Starting Area")]
    public int StartAreaWidth = 5;
    public int StartAreaHeight = 5;
    
    [Header("Room/Connector Catalogs")]
    public MazeRoomDefinition[] Rooms;
    [Tooltip("Connector prefabs for connecting rooms")]
    public GameObject[] Connectors;
    [Tooltip("Essentially half of a connector prefab that fills the opening instead of connecting it")]
    public GameObject[] OpeningFillers;
    
    [Header("Generation")]
    public int MaximumGenerationAttempts = 100;

    /// gets the center coordinates of the cell, adjusted with room size and gap size ( [0][0] centers at 0.5 * (CellWidth + CellGap) )
    public Vector3 GetCellCenterLocal(Vector2Int cell)
    {
        return new Vector3((cell.x + 0.5f) * (CellWidth + CellGap), 0f, (cell.y + 0.5f) * (CellWidth + CellGap));
    }
}