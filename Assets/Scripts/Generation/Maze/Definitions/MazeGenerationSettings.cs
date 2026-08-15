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
    [Tooltip("Boss room prefabs. The prefab origin should be its entrance socket, with local forward pointing into the boss room.")]
    public GameObject[] BossRoomPrefabs;
    [Tooltip("Connector prefabs for connecting rooms")]
    public GameObject[] Connectors;
    [Tooltip("Essentially half of a connector prefab that fills the opening instead of connecting it")]
    public GameObject[] OpeningFillers;
    
    [Header("Generation")]
    public int MaximumGenerationAttempts = 100;
    [Tooltip("Adjacent rooms with matching openings are connected when their existing route would be longer than this many room connections.")]
    [Min(2)] public int MaximumConnectionDetour = 6;
    [Tooltip("Chance for each remaining optional room connection to be opened after guaranteed shortcuts are added.")]
    [Range(0f, 1f)] public float ExtraConnectionChance = 0.1f;

    
    /*
     if we had a grid like
     
     X X X X X
     X 0 0 0 X
     X 0 0 0 X
     X 0 0 0 X
     X X X X X
     
     start bounds wouuld be a rectint such that
     xmin = 1 (top left i index)
     ymin = 1 (top left j index)
     width = 3
     height = 3
     
     in cases for non even cetnering like a 1x1 start in a 4x4 maze:
     
     X X X X
     X 0 X X
     X X X X
     X X X X
     
     we would calc minx as (maze width - start width)/2 which is (4-1)/2 -> 3/2 -> 1 (int floor)
     so essentially uncenterable starting areas get offset up and left 
     
     */
    public RectInt GetStartBounds()
    {
        int minX = (GridWidth - StartAreaWidth) / 2;
        int minZ = (GridHeight - StartAreaHeight) / 2;

        return new RectInt(
            minX,
            minZ,
            StartAreaWidth,
            StartAreaHeight);
    }
    
    /// gets the center coordinates of the cell, adjusted with room size and gap size ( [0][0] centers at 0.5 * (CellWidth + CellGap) )
    public Vector3 GetCellCenterLocal(Vector2Int cell)
    {
        return new Vector3((cell.x + 0.5f) * (CellWidth + CellGap), 0f, (cell.y + 0.5f) * (CellWidth + CellGap));
    }

    /// gets world dimensions of a given room bounds (not counting outer cell gaps! but including inner ones ofc)
    public Vector2 GetRoomWorldSize(Vector2Int footprintBounds)
    {
        float width = footprintBounds.x * CellWidth + Mathf.Max(0, footprintBounds.x - 1) * CellGap;
        float height = footprintBounds.y * CellHeight + Mathf.Max(0, footprintBounds.y - 1) * CellGap;
        return new Vector2(width, height);
    }
    
    /// gets location of center in world coordinates, but localized with 0,0,0 being the (bottom-left?) corner of the maze
    public Vector3 GetStartAreaCenterLocal()
    {
        RectInt bounds = GetStartBounds();

        return new Vector3(
            (bounds.xMin + bounds.width * 0.5f) * (CellWidth + CellGap),
            0f,
            (bounds.yMin + bounds.height * 0.5f) * (CellHeight + CellGap));
    }
    public Vector2 GetStartAreaWorldSize()
    {
        return GetRoomWorldSize(new Vector2Int(StartAreaWidth, StartAreaHeight));
    }
}
