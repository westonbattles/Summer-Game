using UnityEngine;

// handles gameobj instantiation
public static class MazeBuilder
{
    public static Transform Build(MazeLayout layout, MazeGenerationSettings settings, Transform mazeTransform)
    {
        GameObject rootObject = new GameObject($"Generated Maze {layout.Seed}");
        Transform root = rootObject.transform;
        root.SetParent(mazeTransform, false);

        foreach (MazeRoomPlacement room in layout.Rooms.Values) BuildRoom(room, settings, root);
        foreach (MazeConnection connection in layout.Connections) BuildConnection(connection, settings, root);
        foreach (MazeEntrance entrance in layout.Entrances) BuildEntranceConnection(entrance, settings, root);
        BuildBoss(layout.Boss, settings, root);

        return root;
    }

    static void BuildRoom(MazeRoomPlacement room, MazeGenerationSettings settings, Transform root)
    {
        if (room.RoomPrefab == null) return;

        MazeRoom instanceRoom = Object.Instantiate(room.RoomPrefab, root);
        room.Instance = instanceRoom;
        GameObject instance = instanceRoom.gameObject;
        string roomName = string.IsNullOrWhiteSpace(room.RoomPrefab.DisplayName) ? room.RoomPrefab.name : room.RoomPrefab.DisplayName;
        instance.name = $"{roomName} #{room.Id} ({room.GridOrigin.x},{room.GridOrigin.y})";
        instance.transform.localPosition = settings.GetCellCenterLocal(room.PrefabOriginCell);
        instance.transform.localRotation = GridDirectionUtils.ToRotation(room.Rotation);

        foreach (MazePlacedOpening placedOpening in room.Openings)
        {
            if (!instanceRoom.TryGetOpening(placedOpening.PrefabKey, out MazeRoomOpening prefabOpening))
            {
                Debug.LogError($"Generated room '{instance.name}' has no prefab opening marker for {placedOpening.PrefabKey}.", instance);
                continue;
            }

            if (placedOpening.State == MazeOpeningState.Unresolved) Debug.LogError($"Generated room '{instance.name}' reached the builder with an unresolved opening at {placedOpening.PrefabKey}. It will remain closed.", instance);

            prefabOpening.ApplyState(placedOpening.State);
        }
    }

    static void BuildConnection(MazeConnection connection, MazeGenerationSettings settings, Transform root)
    {
        if (connection.ConnectorPrefab == null) return;

        MazePlacedOpening opening = connection.PotentialConnection.OpeningA;
        GameObject instance = Object.Instantiate(connection.ConnectorPrefab, root);
        instance.name = connection.IsCritical ? "Critical Room Connector" : "Room Connector";
        instance.transform.localPosition = GetBoundaryCenterLocal(opening, settings);
        instance.transform.localRotation = GridDirectionUtils.ToRotation(opening.Direction);
    }

    static void BuildEntranceConnection(MazeEntrance entrance, MazeGenerationSettings settings, Transform root)
    {
        if (entrance.ConnectorPrefab == null) return;

        GameObject instance = Object.Instantiate(entrance.ConnectorPrefab, root);
        instance.name = $"Village Entrance Connector {entrance.RoomId}";
        instance.transform.localPosition = GetBoundaryCenterLocal(entrance.Opening, settings);
        instance.transform.localRotation = GridDirectionUtils.ToRotation(entrance.Opening.Direction);
    }

    static void BuildBoss(MazeBossPlacement boss, MazeGenerationSettings settings, Transform root)
    {
        if (boss == null || boss.Opening == null) return;

        Vector3 outward = GridDirectionUtils.ToWorldDirection(boss.Opening.Direction);
        Vector3 boundaryCenter = GetBoundaryCenterLocal(boss.Opening, settings);

        if (boss.ConnectorPrefab != null)
        {
            GameObject connector = Object.Instantiate(boss.ConnectorPrefab, root);
            connector.name = "Boss Room Connector";
            connector.transform.localPosition = boundaryCenter;
            connector.transform.localRotation = GridDirectionUtils.ToRotation(boss.Opening.Direction);
        }

        if (boss.BossPrefab == null) return;

        GameObject bossRoom = Object.Instantiate(boss.BossPrefab, root);
        bossRoom.name = "Boss Room";
        bossRoom.transform.localPosition = boundaryCenter + outward * (settings.CellGap * 0.5f);
        bossRoom.transform.localRotation = GridDirectionUtils.ToRotation(boss.Opening.Direction);
    }

    static Vector3 GetBoundaryCenterLocal(MazePlacedOpening opening, MazeGenerationSettings settings)
    {
        float pitchX = settings.CellWidth + settings.CellGap;
        float pitchZ = settings.CellHeight + settings.CellGap;
        Vector3 cellCenter = new Vector3((opening.Cell.x + 0.5f) * pitchX, 0f, (opening.Cell.y + 0.5f) * pitchZ);
        Vector3 direction = GridDirectionUtils.ToWorldDirection(opening.Direction);
        bool eastOrWest = opening.Direction == GridDirection.East || opening.Direction == GridDirection.West;
        float halfPitch = eastOrWest ? pitchX * 0.5f : pitchZ * 0.5f;
        return cellCenter + direction * halfPitch;
    }
}
