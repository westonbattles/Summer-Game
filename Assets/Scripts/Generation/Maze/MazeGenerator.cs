using System;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] MazeGenerationSettings settings;

    [Header("Seed")]
    [SerializeField] bool randomizeSeed = true;
    [SerializeField] int seed = 12345;

    [Header("Runtime")]
    [SerializeField] bool generateOnStart = true;

    [Header("Debug")]
    [SerializeField] bool drawGizmos = true;

    [SerializeField, HideInInspector] Transform generatedRoot;

    public MazeLayout CurrentLayout { get; private set; }
    public int LastGeneratedSeed { get; private set; }

    void Start()
    {
        if (generateOnStart) Generate();
    }

    [ContextMenu("Generate Maze")]
    public void Generate()
    {
        if (settings == null)
        {
            Debug.LogError("MazeGenerator has no settings asset.", this);
            return;
        }

        int selectedSeed = randomizeSeed ? Environment.TickCount : seed;

        if (!MazeLayoutGenerator.TryGenerate(settings, selectedSeed, out MazeLayout layout, out string error))
        {
            Debug.LogError(error, this);
            return;
        }

        ClearGeneratedObjects();

        CurrentLayout = layout;
        LastGeneratedSeed = layout.Seed;
        generatedRoot = MazeBuilder.Build(layout, settings, transform);

        Debug.Log($"Generated maze with seed {layout.Seed}, {layout.Rooms.Count} rooms, and {layout.Connections.Count} connections.", this);
    }

    [ContextMenu("Clear Generated Maze")]
    public void ClearGeneratedObjects()
    {
        if (generatedRoot == null) return;

        if (Application.isPlaying) Destroy(generatedRoot.gameObject);
        else DestroyImmediate(generatedRoot.gameObject);

        generatedRoot = null;
        CurrentLayout = null;
    }

    public bool TryWorldToCell(Vector3 worldPosition, out Vector2Int cell)
    {
        cell = default;
        if (settings == null) return false;

        Vector3 local = transform.InverseTransformPoint(worldPosition);
        float pitchX = settings.CellWidth + settings.CellGap;
        float pitchZ = settings.CellHeight + settings.CellGap;
        int x = Mathf.FloorToInt(local.x / pitchX);
        int z = Mathf.FloorToInt(local.z / pitchZ);

        cell = new Vector2Int(x, z);
        return x >= 0 && z >= 0 && x < settings.GridWidth && z < settings.GridHeight;
    }

    public bool TryGetRoomAtWorldPosition(Vector3 worldPosition, out MazeRoomPlacement room)
    {
        room = null;
        if (CurrentLayout == null) return false;
        if (!TryWorldToCell(worldPosition, out Vector2Int cell)) return false;
        return CurrentLayout.TryGetRoomAtCell(cell, out room);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || settings == null) return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Gizmos.matrix = transform.localToWorldMatrix;
        RectInt startBounds = settings.GetStartBounds();
        float pitchX = settings.CellWidth + settings.CellGap;
        float pitchZ = settings.CellHeight + settings.CellGap;

        for (int x = 0; x < settings.GridWidth; x++)
        {
            for (int z = 0; z < settings.GridHeight; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);
                Gizmos.color = GetCellColor(cell, startBounds);
                Vector3 center = new Vector3((x + 0.5f) * pitchX, 0f, (z + 0.5f) * pitchZ);
                Gizmos.DrawWireCube(center, new Vector3(settings.CellWidth, 0.1f, settings.CellHeight));
            }
        }

        if (CurrentLayout != null)
        {
            Gizmos.color = Color.yellow;

            // draw inner cell connections for each room
            foreach (MazeRoomPlacement room in CurrentLayout.Rooms.Values)
            {
                foreach (Vector2Int cell in room.OccupiesCells)
                {
                    if (room.OccupiesCells.Contains(cell+Vector2Int.up)) Gizmos.DrawLine(settings.GetCellCenterLocal(cell), settings.GetCellCenterLocal(cell+Vector2Int.up));
                    if (room.OccupiesCells.Contains(cell+Vector2Int.right)) Gizmos.DrawLine(settings.GetCellCenterLocal(cell), settings.GetCellCenterLocal(cell+Vector2Int.right));
                }
            }
            
            // draw opening connections for each room
            foreach (MazeConnection connection in CurrentLayout.Connections)
            {
                Vector3 roomA = settings.GetCellCenterLocal(connection.PotentialConnection.OpeningA.Cell);
                Vector3 roomB = settings.GetCellCenterLocal(connection.PotentialConnection.OpeningB.Cell);
                Gizmos.DrawLine(roomA, roomB);
            }
        }

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    Color GetCellColor(Vector2Int cell, RectInt startBounds)
    {
        // outline of cells before any generation (startbounds/village cells are blue, the rest are grey)
        if (CurrentLayout == null) return startBounds.Contains(cell) ? new Color(0.2f, 0.7f, 1f, 0.5f) : new Color(1f, 1f, 1f, 0.25f);

        // 
        MazeCell mazeCell = CurrentLayout.Grid.GetCell(cell);
        if (mazeCell.State == MazeCellState.ReservedForVillage) return new Color(0.2f, 0.7f, 1f, 0.5f);
        if (mazeCell.State != MazeCellState.Occupied) return new Color(1f, 1f, 1f, 0.75f);
        if (!CurrentLayout.Rooms.TryGetValue(mazeCell.RoomId, out MazeRoomPlacement room)) return Color.gray;
        if (CurrentLayout.Boss != null && room.Id == CurrentLayout.Boss.Opening.RoomId) return new Color(1f, 0.2f, 0.2f, 0.75f);
        if (room.IsEntrance) return new Color(0.2f, 1f, 0.3f, 0.75f);

        // distinguishable hue for all different room ids 
        // Color color = Color.HSVToRGB(room.Id * 0.173f % 1f, 0.55f, 0.9f);
        Color color = new Color(1f, 1f, 1f, 0.25f);
        return color;
    }

    Vector3 GetRoomCenterLocal(MazeRoomPlacement room)
    {
        int minX = int.MaxValue;
        int minZ = int.MaxValue;
        int maxX = int.MinValue;
        int maxZ = int.MinValue;

        foreach (Vector2Int cell in room.OccupiesCells)
        {
            minX = Mathf.Min(minX, cell.x);
            minZ = Mathf.Min(minZ, cell.y);
            maxX = Mathf.Max(maxX, cell.x);
            maxZ = Mathf.Max(maxZ, cell.y);
        }

        float pitchX = settings.CellWidth + settings.CellGap;
        float pitchZ = settings.CellHeight + settings.CellGap;
        return new Vector3((minX + (maxX - minX + 1) * 0.5f) * pitchX, 0f, (minZ + (maxZ - minZ + 1) * 0.5f) * pitchZ);
    }
}
