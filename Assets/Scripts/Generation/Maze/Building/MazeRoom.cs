using System.Collections.Generic;
using UnityEngine;

public enum MazeCellType
{
    Normal,
    Boss
}

// Lives on every room prefab root and contains everything the maze needs to place it.
[DisallowMultipleComponent]
[AddComponentMenu("Maze Generation/Room")]
public class MazeRoom : MonoBehaviour
{
    [Header("Room Catalog")]
    public string DisplayName = "Room";
    public MazeCellType Type = MazeCellType.Normal;

    [Tooltip("Local occupied cells relative to the prefab origin cell (0,0). The prefab root must be centered on cell (0,0).")]
    public Vector2Int[] OccupiedCells = { Vector2Int.zero };

    [Min(0f)] public float SpawnWeight = 1f;

    public MazeRoomOpening[] GetOpenings()
    {
        return GetComponentsInChildren<MazeRoomOpening>(true);
    }

    public bool TryGetOpening(MazeOpeningKey key, out MazeRoomOpening opening)
    {
        foreach (MazeRoomOpening candidate in GetOpenings())
        {
            if (candidate.Key.Equals(key))
            {
                opening = candidate;
                return true;
            }
        }

        opening = null;
        return false;
    }

    public bool TryValidate(out string error)
    {
        MazeRoomOpening[] authoredOpenings = GetOpenings();
        Dictionary<MazeOpeningKey, MazeRoomOpening> openingsByKey = new();

        if (authoredOpenings.Length == 0) return ValidationFailed($"Room prefab '{name}' has no MazeRoomOpening components. Add an opening slot for every possible doorway.", out error);

        foreach (MazeRoomOpening opening in authoredOpenings)
        {
            if (!opening.gameObject.activeSelf) return ValidationFailed($"Room prefab '{name}' has an inactive opening marker at {opening.Key}. Keep the marker active and toggle only its Open and Closed children.", out error);
            if (opening.ClosedRoot == null) return ValidationFailed($"Room prefab '{name}' has no Closed Root assigned for its opening at {opening.Key}.", out error);
            if (opening.ClosedRoot == opening.gameObject) return ValidationFailed($"Room prefab '{name}' uses the opening marker itself as the Closed Root at {opening.Key}. Use an always-active marker with a separate Closed child so an open doorway can still enable its Open Root.", out error);
            if (!opening.ClosedRoot.transform.IsChildOf(opening.transform)) return ValidationFailed($"Room prefab '{name}' has a Closed Root outside its opening marker at {opening.Key}. Make the Closed object a child of that MazeRoomOpening.", out error);
            if (opening.OpenRoot != null && opening.OpenRoot == opening.ClosedRoot) return ValidationFailed($"Room prefab '{name}' uses the same object for both Closed Root and Open Root at {opening.Key}.", out error);
            if (opening.OpenRoot != null && !opening.OpenRoot.transform.IsChildOf(opening.transform)) return ValidationFailed($"Room prefab '{name}' has an Open Root outside its opening marker at {opening.Key}. Make the Open object a child of that MazeRoomOpening.", out error);
            if (opening.OpenRoot != null && opening.OpenRoot.transform.IsChildOf(opening.ClosedRoot.transform)) return ValidationFailed($"Room prefab '{name}' has its Open Root below its Closed Root at {opening.Key}. Make Open and Closed siblings so disabling Closed does not also hide Open.", out error);
            if (!openingsByKey.TryAdd(opening.Key, opening)) return ValidationFailed($"Room prefab '{name}' has more than one opening marker at {opening.Key}.", out error);
        }

        error = null;
        return true;
    }

    static bool ValidationFailed(string message, out string error)
    {
        error = message;
        return false;
    }
}
