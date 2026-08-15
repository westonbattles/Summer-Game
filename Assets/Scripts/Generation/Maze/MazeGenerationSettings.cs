using UnityEngine;

[CreateAssetMenu(fileName = "Maze Generation/Settings")]
public class MazeGenerationSettings : ScriptableObject
{
    [Header("Grid")]
    [Min(1)] public int GridWidth = 9;
    [Min(1)] public int GridHeight = 9;
    
    [Header("Physical Room Dimensions")]
    // interior width of 1x1 room
    [Min(0.01f)] public float RoomWidth = 30f; // 30m (apply fbx transform means its 30m in blender as well)
    [Min(0.01f)] public float RoomHeight = 30f; // 30m (apply fbx transform means its 30m in blender as well)
    [Min(0.0f)] public float RoomGap = 3f; // gap between interiords of rooms
    
    [Header("Room Opening Dimensions")]
    [Min(0.01f)] public float OpeningWidth = 8f;
    [Min(0.01f)] public float OpeningHeight = 27f;
    
    // automatic cell blocking for starting area
    [Header("Starting Area")]
    public int StartAreaWidth = 5;
    public int StartAreaHeight = 5;
    
    [Header("Required Rooms")]
    //public MazeR
    
    [Header("Generation")]
    public int MaximumGenerationAttempts = 100;
}