
using System.Collections.Generic;
using UnityEngine;

public enum MazeOpeningState
{
    Unresolved,
    ConnectedToRoom,
    ConnectedToStart,
    ConnectedToBoss,
    Filled,
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

    public MazeOpeningState State = MazeOpeningState.Unresolved;

    public GameObject FillerPrefab;
}

public class MazePotentialConnection
{
    public int Id;
    
    public MazePlacedOpening OpeningA;
    public MazePlacedOpening OpeningB;

    public bool TouchesRoom(int roomId)
    {
        return OpeningA.RoomId == roomId || OpeningB.RoomId == roomId;
    }

    public int GetOtherRoomId(int roomId)
    {
        return OpeningA.RoomId == roomId ? OpeningB.RoomId : OpeningA.RoomId;
    }
}

public class MazeConnection
{
    public MazePotentialConnection PotentialConnection;
    public GameObject ConnectorPrefab;
    public bool IsCritical;
}

public class MazeEntrance
{
    public int RoomId;

    // opening where the entrance connects to the village
    public MazePlacedOpening Opening;
    public Vector2Int StartCell;
    public GameObject ConnectorPrefab;
}

public class MazeBossPlacement
{
    public MazePlacedOpening Opening;
    public GameObject BossPrefab;
    public GameObject ConnectorPrefab;
}

public class MazeLayout
{
    public int Seed;

    public MazeGrid Grid;

    public Dictionary<int, MazeRoomPlacement> Rooms = new(); // room id -> room placement object
    public List<MazePotentialConnection> PotentialConnections = new();
    public List<MazeConnection> Connections = new();

    public MazeBossPlacement Boss;

    public List<MazeEntrance> Entrances = new();
    public Dictionary<int, List<int>> Adjacency = new();

    /// builds adjaceny matrix using connections list
    public void RebuildAdjacency()
    {
        Adjacency.Clear();

        foreach (int roomId in Rooms.Keys) Adjacency[roomId] = new List<int>();
        
        foreach (MazeConnection connection in Connections)
        {
            MazePotentialConnection potential = connection.PotentialConnection;

            int roomA = potential.OpeningA.RoomId;
            int roomB = potential.OpeningB.RoomId;

            if (!Adjacency[roomA].Contains(roomB)) Adjacency[roomA].Add(roomB);
            if (!Adjacency[roomB].Contains(roomA)) Adjacency[roomB].Add(roomA);
        }
    }

    public bool TryGetRoomAtCell(Vector2Int cell, out MazeRoomPlacement room)
    {
        room = null;
        if (!Grid.TryGetRoomId(cell, out int roomId)) return false;
        return Rooms.TryGetValue(roomId, out room);
    }
}
