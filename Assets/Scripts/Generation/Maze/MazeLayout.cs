
using System.Collections.Generic;
using UnityEngine;

public enum MazeOpeningState
{
    Unresolved,
    ConnectedToRoom,
    ConnectedToStart,
    Filled,
}

public enum RoomRotation
{
    R0,
    R90,
    R180,
    R270
}

/// placement of a room in our grid
public class MazeRoomPlacement
{
    public int Id;
    public MazeRoomDefinition Definition;

    public Vector2Int GridOrigin;
    public RoomRotation Rotation;

    public List<Vector2Int> OccupiesCells = new();
    public List<MazePlacedOpening> Openings = new();

    public bool IsEntrance;
    
    public int BranchId = -1;
}

public class MazePlacedOpening
{
    public int RoomId;
    public int OpeningIndex;

    public Vector2Int Cell;
    public GridDirection Direction;

    public MazeOpeningState State;
}

public class MazePotentialConnection
{
    public MazePlacedOpening OpeningA;
    public MazePlacedOpening OpeningB;
}

public class MazeConnection
{
    public MazePotentialConnection PotentialConnection;
    public GameObject ConnectorPrefab;
}

public class MazeEntrance
{
    public int RoomId;

    // opening where the entrance connects to the village
    public MazePlacedOpening Opening;
    public Vector2Int StartCell;
}

public class MazeLayout
{
    public int Seed;

    public MazeGrid Grid;

    public Dictionary<int, MazeRoomPlacement> Rooms = new(); // room id -> room placement object
    public List<MazePotentialConnection> PotentialConnections = new();
    public List<MazeConnection> Connections = new();

    public int BossRoomId = -1;

    public List<MazeEntrance> Entrances = new();
}