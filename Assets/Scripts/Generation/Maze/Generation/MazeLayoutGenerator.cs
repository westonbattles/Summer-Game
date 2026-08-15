using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public static class MazeLayoutGenerator
{
    class RoomVariant
    {
        public MazeRoomDefinition Definition;
        public RoomRotation Rotation;

        public Vector2Int[] Cells;
        public VariantOpening[] Openings;

        public Vector2Int Bounds;
    }
    
    struct VariantOpening
    {
        public int DefinitionOpeningIndex;
        public Vector2Int Cell;
        public GridDirection Direction;
    }
    
    class PlacementCandidate
    {
        public RoomVariant Variant;
        public Vector2Int Origin;

        public float Weight => Variant.Definition.SpawnWeight;
    }
    
    struct EntranceRequirement
    {
        public Vector2Int RoomCell;
        public Vector2Int StartCell;
        public GridDirection DirectionIntoStart;
        public int BranchId;
    }
    
    struct OpeningKey : IEquatable<OpeningKey>
    {
        public Vector2Int Cell;
        public GridDirection Direction;

        public OpeningKey(Vector2Int cell, GridDirection direction)
        {
            Cell = cell;
            Direction = direction;
        }

        public bool Equals(OpeningKey other)
        {
            return Cell == other.Cell && Direction == other.Direction;
        }

        public override bool Equals(object obj)
        {
            return obj is OpeningKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Cell.GetHashCode() * 397 ^ (int)Direction;
            }
        }
    }

    public static bool TryGenerate(MazeGenerationSettings settings, int baseSeed, out MazeLayout layout, out string error)
    {
        layout = null;
        if (!ValidateSetting(settings, out error)) return false;

        Dictionary<MazeRoomDefinition, List<RoomVariant>> variants = BuildVariantCatalog(settings.Rooms);
        string failure = "No generation attempt ran";
        for (int attempt = 0; attempt < settings.MaximumGenerationAttempts; attempt++)
        {
            // unchcked just allows overflow which is fine for hashes
            int attemptSeed = unchecked(baseSeed + attempt * 7919); // 1000th prime number
            Random random = new Random(attemptSeed);

            if (TryGenerateAttempt(settings, variants, attemptSeed, random, out layout, out failure))
            {
                error = null;
                return true; // succesfull generation
            }
        }
        error = $"Maze generation failed after {settings.MaximumGenerationAttempts} attempts. Last failure: {failure}";
        return false;
    }

    static bool TryGenerateAttempt(
        MazeGenerationSettings settings,
        Dictionary<MazeRoomDefinition, List<RoomVariant>> variants,
        int seed,
        Random random,
        out MazeLayout layout,
        out string failure)
    {
        layout = new MazeLayout
        {
            Seed = seed,
            Grid = new MazeGrid(settings.GridWidth, settings.GridHeight)
        };

        ReserveStartingArea(layout.Grid, settings);

        List<EntranceRequirement> entranceRequirements = GetEntranceRequirements(settings);
        HashSet<Vector2Int> entranceSlots = new();

        foreach (EntranceRequirement requirement in entranceRequirements)
        {
            entranceSlots.Add(requirement.RoomCell);
        }
        
        if (!TryPlaceBoss)

    }
}