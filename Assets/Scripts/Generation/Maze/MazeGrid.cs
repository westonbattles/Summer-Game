using UnityEngine;

public enum MazeCellState
{
    Available,
    ReservedForVillage,
    Occupied,
}

public class MazeCell
{
    public MazeCellState State = MazeCellState.Available;
    public int RoomId = -1;
}

public class MazeGrid
{
    public int Width { get; }
    public int Height { get; }

    readonly MazeCell[,] cells; // https://stackoverflow.com/questions/12567329/multidimensional-array-vs

    public MazeGrid(int width, int height)
    {
        Width = width;
        Height = height;

        cells = new MazeCell[width, height];

        // init the cells
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                cells[i, j] = new MazeCell(); 
            }
        }
    }

    public bool Contains(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
    }
    
    public MazeCell GetCell(Vector2Int cell)
    {
        return cells[cell.x, cell.y];
    }
    
    // if a cell in our grid hasn't been assigned yet (meaning its open for holding a room cell)
    public bool IsFree(Vector2Int cell)
    {
        return Contains(cell) && GetCell(cell).State == MazeCellState.Available;
    }
    
    /// reserve village cells meaning generation won't bother it
    public void ReserveVillageCell(Vector2Int cell)
    {
        MazeCell mazeCell = GetCell(cell);
        mazeCell.State = MazeCellState.ReservedForVillage;
        mazeCell.RoomId = -1;
    }

    public void Occupy(Vector2Int cell, int roomId)
    {
        MazeCell mazeCell = GetCell(cell);
        mazeCell.State = MazeCellState.Occupied;
        mazeCell.RoomId = roomId;
    }

    public bool TryGetRoomId(Vector2Int cell, out int roomId)
    {
        roomId = -1;
        if (!Contains(cell)) return false;

        MazeCell mazeCell = GetCell(cell);
        if (mazeCell.State != MazeCellState.Occupied) return false;

        roomId = mazeCell.RoomId;
        return true;
    }
}