using System;
using UnityEngine;

[Serializable]
public readonly struct MazeOpeningKey : IEquatable<MazeOpeningKey>
{
    public readonly Vector2Int LocalCell;
    public readonly GridDirection Direction;

    public MazeOpeningKey(Vector2Int localCell, GridDirection direction)
    {
        LocalCell = localCell;
        Direction = direction;
    }

    public bool Equals(MazeOpeningKey other)
    {
        return LocalCell == other.LocalCell && Direction == other.Direction;
    }

    public override bool Equals(object obj)
    {
        return obj is MazeOpeningKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return LocalCell.GetHashCode() * 397 ^ (int)Direction;
        }
    }

    public override string ToString()
    {
        return $"cell X={LocalCell.x}, Z={LocalCell.y}, facing {Direction}";
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Maze Generation/Room Opening")]
public class MazeRoomOpening : MonoBehaviour
{
    [Header("Opening Opportunity")]
    [SerializeField]
    [Tooltip("local cell the opening is in. a 1x1 room always uses (0,0)")]
    Vector2Int localCell;

    [SerializeField]
    [Tooltip("direction this opening faces in the prefab")]
    GridDirection direction;

    [Header("Room-owned oprenings/closings")]
    [SerializeField]
    [Tooltip("Objects used when this opening is closed. opening meshes, colliders, and any closed-only decoration would go below this root.")]
    GameObject closedRoot;

    [SerializeField]
    [Tooltip("(OPTIONAL) objects used when this opening is open, such as a doorway trim or room-specific decor, for smoother transitions or wtv you want.")]
    GameObject openRoot;

    public Vector2Int LocalCell => localCell;
    public GridDirection Direction => direction;
    public MazeOpeningKey Key => new MazeOpeningKey(localCell, direction);
    public GameObject ClosedRoot => closedRoot;
    public GameObject OpenRoot => openRoot;

    public void ApplyState(MazeOpeningState state)
    {
        bool isOpen = IsOpenState(state);

        if (closedRoot != null) closedRoot.SetActive(!isOpen);
        if (openRoot != null) openRoot.SetActive(isOpen);
    }

    public static bool IsOpenState(MazeOpeningState state)
    {
        return state == MazeOpeningState.ConnectedToRoom || state == MazeOpeningState.ConnectedToStart || state == MazeOpeningState.ConnectedToBoss;
    }

    void Reset()
    {
        Transform closedChild = transform.Find("Closed");
        Transform openChild = transform.Find("Open");

        if (closedChild != null) closedRoot = closedChild.gameObject;
        if (openChild != null) openRoot = openChild.gameObject;
    }
}
