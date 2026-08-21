using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MazeRoom))]
public class MazeRoomEditor : Editor
{
    // maze room prefabs use a fixed 30 m square local-cell grid.
    const float CellSize = 30f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Create an opening slot by choosing a prefab-local Cell X/Z and an outward direction. " + "The slot is automatically positioned and rotated on the 30 m room grid.", MessageType.Info);

        MazeRoom room = (MazeRoom)target;
        if (GUILayout.Button("Create Opening Slot...")) MazeOpeningCreationWindow.ShowFor(room);
    }

    internal static MazeRoomOpening CreateOpeningSlot(MazeRoom room, Vector2Int localCell, GridDirection direction)
    {
        GameObject marker = new GameObject(GetOpeningName(localCell, direction));
        Undo.RegisterCreatedObjectUndo(marker, "Add Maze Room Opening");
        Undo.SetTransformParent(marker.transform, room.transform, "Parent Maze Room Opening");
        marker.transform.SetLocalPositionAndRotation(GetWallCenterLocal(localCell, direction), GridDirectionUtils.ToRotation(direction));
        marker.transform.localScale = Vector3.one;

        MazeRoomOpening opening = Undo.AddComponent<MazeRoomOpening>(marker);

        GameObject closedRoot = CreateStateRoot("Closed", marker.transform);
        GameObject openRoot = CreateStateRoot("Open", marker.transform);
        openRoot.SetActive(false);

        SerializedObject serializedOpening = new SerializedObject(opening);
        serializedOpening.FindProperty("localCell").vector2IntValue = localCell;
        serializedOpening.FindProperty("direction").enumValueIndex = (int)direction;
        serializedOpening.FindProperty("closedRoot").objectReferenceValue = closedRoot;
        serializedOpening.FindProperty("openRoot").objectReferenceValue = openRoot;
        serializedOpening.ApplyModifiedProperties();

        Selection.activeGameObject = marker;
        EditorGUIUtility.PingObject(marker);
        return opening;
    }

    internal static Vector3 GetWallCenterLocal(Vector2Int localCell, GridDirection direction)
    {
        Vector3 cellCenter = new Vector3(localCell.x * CellSize, 0f, localCell.y * CellSize);
        Vector3 outward = GridDirectionUtils.ToWorldDirection(direction);
        return cellCenter + outward * (CellSize * 0.5f);
    }

    internal static string GetOpeningName(Vector2Int localCell, GridDirection direction)
    {
        return $"Opening (X {localCell.x}, Z {localCell.y}) {direction}";
    }

    static GameObject CreateStateRoot(string objectName, Transform parent)
    {
        GameObject stateRoot = new GameObject(objectName);
        Undo.RegisterCreatedObjectUndo(stateRoot, $"Create {objectName} State Root");
        Undo.SetTransformParent(stateRoot.transform, parent, $"Parent {objectName} State Root");
        stateRoot.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        stateRoot.transform.localScale = Vector3.one;
        return stateRoot;
    }
}

public class MazeOpeningCreationWindow : EditorWindow
{
    [SerializeField] MazeRoom room;
    [SerializeField] Vector2Int localCell;
    [SerializeField] GridDirection direction;

    public static void ShowFor(MazeRoom targetRoom)
    {
        MazeOpeningCreationWindow window = CreateInstance<MazeOpeningCreationWindow>();
        window.titleContent = new GUIContent("Create Maze Opening");
        window.minSize = new Vector2(370f, 330f);
        window.room = targetRoom;
        window.ShowUtility();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Create Opening Slot", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Choose any prefab-local cell and outward direction. Cells are spaced 30 m apart and the slot is placed 15 m from its cell center.", MessageType.Info);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Room Prefab", room, typeof(MazeRoom), true);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Local Cell", EditorStyles.boldLabel);
        localCell.x = EditorGUILayout.IntField(new GUIContent("Cell X", "East/west cell position within this room prefab."), localCell.x);
        localCell.y = EditorGUILayout.IntField(new GUIContent("Cell Z", "North/south cell position within this room prefab."), localCell.y);

        EditorGUILayout.Space();
        DrawDirectionPicker();

        EditorGUILayout.Space();
        // Vector3 previewPosition = MazeRoomEditor.GetWallCenterLocal(localCell, direction);
        // EditorGUILayout.LabelField("Slot local position", $"({previewPosition.x:0.###}, {previewPosition.y:0.###}, {previewPosition.z:0.###})");
        // EditorGUILayout.LabelField("Slot faces", GetDirectionLabel(direction));

        string validationError = GetValidationError();
        if (!string.IsNullOrEmpty(validationError)) EditorGUILayout.HelpBox(validationError, MessageType.Error);

        GUILayout.FlexibleSpace();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Cancel")) Close();

            using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(validationError)))
            {
                if (GUILayout.Button("Create Opening")) CreateOpening();
            }
        }
    }

    void DrawDirectionPicker()
    {
        EditorGUILayout.LabelField("Opening Direction", EditorStyles.boldLabel);
        
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            DrawDirectionButton(GridDirection.North, "North (+Z)");
            GUILayout.FlexibleSpace();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawDirectionButton(GridDirection.West, "West (-X)");
            GUILayout.FlexibleSpace();
            DrawDirectionButton(GridDirection.East, "East (+X)");
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            DrawDirectionButton(GridDirection.South, "South (-Z)");
            GUILayout.FlexibleSpace();
        }
    }

    void DrawDirectionButton(GridDirection buttonDirection, string label)
    {
        Color previousColor = GUI.backgroundColor;
        if (direction == buttonDirection) GUI.backgroundColor = new Color(0.35f, 0.8f, 1f);

        if (GUILayout.Button(label, GUILayout.Width(110f))) direction = buttonDirection;
        GUI.backgroundColor = previousColor;
    }

    string GetValidationError()
    {
        if (room == null) return "The room prefab is no longer available.";

        MazeOpeningKey key = new MazeOpeningKey(localCell, direction);
        if (room.TryGetOpening(key, out _))
        {
            return $"This prefab already has an opening at cell X={localCell.x}, Z={localCell.y}, facing {GetDirectionLabel(direction)}.";
        }

        
        Vector2Int[] cells = room.OccupiedCells;
        if (!cells.Contains(localCell)) return $"Local cell not in list of valid cells as defined by the room :(";

        return null;
    }

    void CreateOpening()
    {
        MazeRoomEditor.CreateOpeningSlot(room, localCell, direction);
        Close();
    }

    static string GetDirectionLabel(GridDirection openingDirection)
    {
        return openingDirection switch
        {
            GridDirection.North => "North (+Z)",
            GridDirection.East => "East (+X)",
            GridDirection.South => "South (-Z)",
            GridDirection.West => "West (-X)",
            _ => openingDirection.ToString()
        };
    }
}

[CustomEditor(typeof(MazeRoomOpening))]
public class MazeRoomOpeningEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty localCellProperty = serializedObject.FindProperty("localCell");
        SerializedProperty directionProperty = serializedObject.FindProperty("direction");
        SerializedProperty closedRootProperty = serializedObject.FindProperty("closedRoot");
        SerializedProperty openRootProperty = serializedObject.FindProperty("openRoot");

        Vector2Int localCell = localCellProperty.vector2IntValue;

        EditorGUILayout.LabelField("Opening Opportunity", EditorStyles.boldLabel);
        localCell.x = EditorGUILayout.IntField(new GUIContent("Cell X", "East/west cell position within this room prefab."), localCell.x);
        localCell.y = EditorGUILayout.IntField(new GUIContent("Cell Z", "North/south cell position within this room prefab."), localCell.y);
        localCellProperty.vector2IntValue = localCell;
        EditorGUILayout.PropertyField(directionProperty, new GUIContent("Direction", "North = +Z, East = +X, South = -Z, West = -X."));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Room-Owned Presentation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(closedRootProperty);
        EditorGUILayout.PropertyField(openRootProperty);

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Cell X/Z is prefab-local. The Snap button uses the room prefab's fixed 30 m grid.", MessageType.Info);

        MazeRoomOpening opening = (MazeRoomOpening)target;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview Closed")) ApplyPreview(opening, MazeOpeningState.Filled);
            if (GUILayout.Button("Preview Open")) ApplyPreview(opening, MazeOpeningState.ConnectedToRoom);
        }

        if (GUILayout.Button("Snap To 30 m Cell Wall")) SnapToCellWall(opening);
    }

    void OnSceneGUI()
    {
        MazeRoomOpening opening = (MazeRoomOpening)target;
        MazeRoom room = opening.GetComponentInParent<MazeRoom>();
        if (room == null) return;

        Vector3 position = opening.transform.position;
        Vector3 worldDirection = room.transform.TransformDirection(GridDirectionUtils.ToWorldDirection(opening.Direction));
        float handleSize = HandleUtility.GetHandleSize(position) * 0.8f;

        Handles.color = new Color(0.2f, 0.8f, 1f);
        Handles.ArrowHandleCap(
            0,
            position,
            Quaternion.LookRotation(worldDirection, room.transform.up),
            handleSize,
            EventType.Repaint);
        Handles.Label(
            position + worldDirection * handleSize,
            $"{opening.Direction}\nCell X={opening.LocalCell.x}, Z={opening.LocalCell.y}");
    }

    static void ApplyPreview(MazeRoomOpening opening, MazeOpeningState state)
    {
        if (opening.ClosedRoot != null) Undo.RecordObject(opening.ClosedRoot, "Preview Maze Opening");
        if (opening.OpenRoot != null) Undo.RecordObject(opening.OpenRoot, "Preview Maze Opening");

        opening.ApplyState(state);

        if (opening.ClosedRoot != null) EditorUtility.SetDirty(opening.ClosedRoot);
        if (opening.OpenRoot != null) EditorUtility.SetDirty(opening.OpenRoot);
    }

    static void SnapToCellWall(MazeRoomOpening opening)
    {
        MazeRoom room = opening.GetComponentInParent<MazeRoom>();

        if (room == null)
        {
            Debug.LogError($"Opening '{opening.name}' is not below a MazeRoom component.", opening);
            return;
        }

        Undo.RecordObject(opening.transform, "Snap Maze Opening To Cell Wall");
        Undo.RecordObject(opening.gameObject, "Rename Maze Opening");
        Vector3 roomLocalPosition = MazeRoomEditor.GetWallCenterLocal(opening.LocalCell, opening.Direction);
        opening.transform.SetPositionAndRotation(
            room.transform.TransformPoint(roomLocalPosition),
            room.transform.rotation * GridDirectionUtils.ToRotation(opening.Direction));
        opening.transform.localScale = Vector3.one;
        opening.gameObject.name = MazeRoomEditor.GetOpeningName(opening.LocalCell, opening.Direction);
        EditorUtility.SetDirty(opening.transform);
        EditorUtility.SetDirty(opening.gameObject);
    }
}
