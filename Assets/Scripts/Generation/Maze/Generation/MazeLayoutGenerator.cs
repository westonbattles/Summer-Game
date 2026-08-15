using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public static class MazeLayoutGenerator
{
    const float CriticalPathRandomness = 2f;

    class RoomVariant
    {
        public MazeRoomDefinition Definition;
        public RoomRotation Rotation;
        public Vector2Int[] Cells;
        public VariantOpening[] Openings;
        public Vector2Int Bounds;
        public Vector2Int AnchorCell;
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

    struct BossRequirement
    {
        public Vector2Int RoomCell;
        public GridDirection DirectionOutOfMaze;
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

    struct CellEdge : IEquatable<CellEdge>
    {
        public Vector2Int CellA;
        public Vector2Int CellB;

        public CellEdge(Vector2Int cellA, Vector2Int cellB)
        {
            bool aComesFirst = cellA.x < cellB.x || cellA.x == cellB.x && cellA.y <= cellB.y;
            CellA = aComesFirst ? cellA : cellB;
            CellB = aComesFirst ? cellB : cellA;
        }

        public bool Equals(CellEdge other)
        {
            return CellA == other.CellA && CellB == other.CellB;
        }

        public override bool Equals(object obj)
        {
            return obj is CellEdge other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return CellA.GetHashCode() * 397 ^ CellB.GetHashCode();
            }
        }
    }

    public static bool TryGenerate(MazeGenerationSettings settings, int baseSeed, out MazeLayout layout, out string error)
    {
        layout = null;
        if (!ValidateSettings(settings, out error)) return false;

        Dictionary<MazeRoomDefinition, List<RoomVariant>> variants = BuildVariantCatalog(settings.Rooms);
        string failure = "No generation attempt ran.";

        for (int attempt = 0; attempt < settings.MaximumGenerationAttempts; attempt++)
        {
            int attemptSeed = unchecked(baseSeed + attempt * 7919);
            Random random = new Random(attemptSeed);

            if (!TryGenerateAttempt(settings, variants, attemptSeed, random, out layout, out failure)) continue;

            error = null;
            return true;
        }

        error = $"Maze generation failed after {settings.MaximumGenerationAttempts} attempts. Last failure: {failure}";
        return false;
    }

    static bool TryGenerateAttempt(MazeGenerationSettings settings, Dictionary<MazeRoomDefinition, List<RoomVariant>> variants, int seed, Random random, out MazeLayout layout, out string failure)
    {
        layout = new MazeLayout
        {
            Seed = seed,
            Grid = new MazeGrid(settings.GridWidth, settings.GridHeight)
        };

        ReserveStartingArea(layout.Grid, settings);

        List<EntranceRequirement> entranceRequirements = GetEntranceRequirements(settings);
        HashSet<Vector2Int> entranceSlots = new();
        foreach (EntranceRequirement requirement in entranceRequirements) entranceSlots.Add(requirement.RoomCell);

        if (!TryPickBossRequirement(layout.Grid, entranceSlots, random, out BossRequirement bossRequirement))
        {
            failure = "No perimeter cell was available for the boss entrance.";
            return false;
        }

        if (!TryBuildCriticalPaths(layout.Grid, bossRequirement, entranceRequirements, random, out HashSet<Vector2Int> criticalCells, out HashSet<CellEdge> requiredEdges, out failure)) return false;
        if (!TryPlaceCriticalRooms(layout, variants, criticalCells, requiredEdges, entranceSlots, random))
        {
            failure = "The critical paths could not be covered by compatible rooms.";
            return false;
        }

        if (!TryCreateBoss(layout, settings, bossRequirement, random, out failure)) return false;
        if (!TryCreateEntrances(layout, settings, entranceRequirements, random, out failure)) return false;

        if (!TryFillRemainingCells(layout, variants, random))
        {
            failure = "A free cell could not be covered by any available normal room.";
            return false;
        }

        if (!TryBuildPotentialConnections(layout, out failure)) return false;
        if (!TryBuildConnectionGraph(layout, settings, requiredEdges, random, out failure)) return false;

        ResolveUnusedOpenings(layout, settings, random);
        layout.RebuildAdjacency();

        failure = null;
        return true;
    }

    static bool ValidateSettings(MazeGenerationSettings settings, out string error)
    {
        if (settings == null)
        {
            error = "Maze generation settings are missing.";
            return false;
        }

        if (settings.StartAreaWidth <= 0 || settings.StartAreaHeight <= 0)
        {
            error = "The village dimensions must be positive.";
            return false;
        }

        if (settings.GridWidth - settings.StartAreaWidth < 2 || settings.GridHeight - settings.StartAreaHeight < 2)
        {
            error = "The grid needs at least one maze cell around every side of the village.";
            return false;
        }

        if (settings.Rooms == null || settings.Rooms.Length == 0)
        {
            error = "The room catalog is empty.";
            return false;
        }

        bool hasNormalRoom = false;
        bool hasUniversalOneCellRoom = false;

        foreach (MazeRoomDefinition definition in settings.Rooms)
        {
            if (definition == null) continue;

            if (definition.OccupiedCells == null || definition.OccupiedCells.Length == 0)
            {
                error = $"Room '{definition.name}' has no occupied cells.";
                return false;
            }

            HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>(definition.OccupiedCells);

            if (occupiedCells.Count != definition.OccupiedCells.Length)
            {
                error = $"Room '{definition.name}' has duplicate occupied cells.";
                return false;
            }

            if (!occupiedCells.Contains(Vector2Int.zero))
            {
                error = $"Room '{definition.name}' must occupy cell (0,0), which anchors its prefab origin.";
                return false;
            }

            if (!AreOccupiedCellsConnected(occupiedCells))
            {
                error = $"Room '{definition.name}' has occupied cells that do not touch each other.";
                return false;
            }

            if (definition.Openings != null)
            {
                foreach (MazeOpeningDefinition opening in definition.Openings)
                {
                    if (!occupiedCells.Contains(opening.Cell))
                    {
                        error = $"An opening in '{definition.name}' belongs to a cell outside its shape.";
                        return false;
                    }

                    Vector2Int neighbor = opening.Cell + GridDirectionUtils.ToGridOffset(opening.Direction);
                    if (!occupiedCells.Contains(neighbor)) continue;

                    error = $"An opening in '{definition.name}' points into another cell belonging to that same room.";
                    return false;
                }
            }

            if (definition.Type == MazeCellType.Normal)
            {
                hasNormalRoom = true;

                if (occupiedCells.Count == 1)
                {
                    Vector2Int roomCell = definition.OccupiedCells[0];
                    HashSet<GridDirection> openingDirections = new();

                    if (definition.Openings != null)
                    {
                        foreach (MazeOpeningDefinition opening in definition.Openings)
                        {
                            if (opening.Cell == roomCell) openingDirections.Add(opening.Direction);
                        }
                    }

                    if (openingDirections.Contains(GridDirection.North) && openingDirections.Contains(GridDirection.East) && openingDirections.Contains(GridDirection.South) && openingDirections.Contains(GridDirection.West)) hasUniversalOneCellRoom = true;
                }
            }

        }

        if (!hasNormalRoom)
        {
            error = "The catalog has no normal rooms.";
            return false;
        }

        if (!HasPrefab(settings.BossRoomPrefabs))
        {
            error = "The boss room prefab catalog is empty.";
            return false;
        }

        if (!hasUniversalOneCellRoom)
        {
            error = "The catalog needs a normal 1x1 fallback room with openings on all four sides.";
            return false;
        }

        error = null;
        return true;
    }

    static bool AreOccupiedCellsConnected(HashSet<Vector2Int> occupiedCells)
    {
        HashSet<Vector2Int> reachedCells = new();
        Queue<Vector2Int> cellsToCheck = new();

        foreach (Vector2Int cell in occupiedCells)
        {
            reachedCells.Add(cell);
            cellsToCheck.Enqueue(cell);
            break;
        }

        while (cellsToCheck.Count > 0)
        {
            Vector2Int cell = cellsToCheck.Dequeue();

            for (int direction = 0; direction < 4; direction++)
            {
                Vector2Int neighbor = cell + GridDirectionUtils.ToGridOffset((GridDirection)direction);
                if (!occupiedCells.Contains(neighbor) || !reachedCells.Add(neighbor)) continue;
                cellsToCheck.Enqueue(neighbor);
            }
        }

        return reachedCells.Count == occupiedCells.Count;
    }

    static Dictionary<MazeRoomDefinition, List<RoomVariant>> BuildVariantCatalog(MazeRoomDefinition[] definitions)
    {
        Dictionary<MazeRoomDefinition, List<RoomVariant>> catalog = new();

        foreach (MazeRoomDefinition definition in definitions)
        {
            if (definition == null) continue;

            List<RoomVariant> variants = new();
            variants.Add(BuildVariant(definition, RoomRotation.R0));
            variants.Add(BuildVariant(definition, RoomRotation.R90));
            variants.Add(BuildVariant(definition, RoomRotation.R180));
            variants.Add(BuildVariant(definition, RoomRotation.R270));
            catalog.Add(definition, variants);
        }

        return catalog;
    }

    static RoomVariant BuildVariant(MazeRoomDefinition definition, RoomRotation rotation)
    {
        Vector2Int[] rawCells = new Vector2Int[definition.OccupiedCells.Length];
        Vector2Int minimum = new Vector2Int(int.MaxValue, int.MaxValue);

        for (int i = 0; i < rawCells.Length; i++)
        {
            rawCells[i] = RotateCell(definition.OccupiedCells[i], rotation);
            minimum.x = Mathf.Min(minimum.x, rawCells[i].x);
            minimum.y = Mathf.Min(minimum.y, rawCells[i].y);
        }

        Vector2Int[] normalizedCells = new Vector2Int[rawCells.Length];
        Vector2Int maximum = Vector2Int.zero;

        for (int i = 0; i < rawCells.Length; i++)
        {
            normalizedCells[i] = rawCells[i] - minimum;
            maximum.x = Mathf.Max(maximum.x, normalizedCells[i].x);
            maximum.y = Mathf.Max(maximum.y, normalizedCells[i].y);
        }

        MazeOpeningDefinition[] definitionOpenings = definition.Openings ?? Array.Empty<MazeOpeningDefinition>();
        VariantOpening[] openings = new VariantOpening[definitionOpenings.Length];

        for (int i = 0; i < definitionOpenings.Length; i++)
        {
            openings[i] = new VariantOpening
            {
                DefinitionOpeningIndex = i,
                Cell = RotateCell(definitionOpenings[i].Cell, rotation) - minimum,
                Direction = GridDirectionUtils.RotateDirection(definitionOpenings[i].Direction, rotation)
            };
        }

        return new RoomVariant
        {
            Definition = definition,
            Rotation = rotation,
            Cells = normalizedCells,
            Openings = openings,
            Bounds = maximum + Vector2Int.one,
            AnchorCell = -minimum
        };
    }

    static Vector2Int RotateCell(Vector2Int cell, RoomRotation rotation)
    {
        return rotation switch
        {
            RoomRotation.R0 => cell,
            RoomRotation.R90 => new Vector2Int(cell.y, -cell.x),
            RoomRotation.R180 => new Vector2Int(-cell.x, -cell.y),
            RoomRotation.R270 => new Vector2Int(-cell.y, cell.x),
            _ => cell
        };
    }

    static void ReserveStartingArea(MazeGrid grid, MazeGenerationSettings settings)
    {
        RectInt bounds = settings.GetStartBounds();

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int z = bounds.yMin; z < bounds.yMax; z++) grid.ReserveVillageCell(new Vector2Int(x, z));
        }
    }

    static List<EntranceRequirement> GetEntranceRequirements(MazeGenerationSettings settings)
    {
        RectInt start = settings.GetStartBounds();
        int centerX = start.xMin + start.width / 2;
        int centerZ = start.yMin + start.height / 2;

        return new List<EntranceRequirement>
        {
            new EntranceRequirement
            {
                RoomCell = new Vector2Int(centerX, start.yMax),
                StartCell = new Vector2Int(centerX, start.yMax - 1),
                DirectionIntoStart = GridDirection.South,
                BranchId = 0
            },
            new EntranceRequirement
            {
                RoomCell = new Vector2Int(start.xMax, centerZ),
                StartCell = new Vector2Int(start.xMax - 1, centerZ),
                DirectionIntoStart = GridDirection.West,
                BranchId = 1
            },
            new EntranceRequirement
            {
                RoomCell = new Vector2Int(centerX, start.yMin - 1),
                StartCell = new Vector2Int(centerX, start.yMin),
                DirectionIntoStart = GridDirection.North,
                BranchId = 2
            },
            new EntranceRequirement
            {
                RoomCell = new Vector2Int(start.xMin - 1, centerZ),
                StartCell = new Vector2Int(start.xMin, centerZ),
                DirectionIntoStart = GridDirection.East,
                BranchId = 3
            }
        };
    }

    static bool TryPickBossRequirement(MazeGrid grid, HashSet<Vector2Int> entranceSlots, Random random, out BossRequirement bossRequirement)
    {
        List<BossRequirement> requirements = new();

        for (int x = 0; x < grid.Width; x++)
        {
            AddBossRequirement(requirements, entranceSlots, new Vector2Int(x, grid.Height - 1), GridDirection.North);
            AddBossRequirement(requirements, entranceSlots, new Vector2Int(x, 0), GridDirection.South);
        }

        for (int z = 0; z < grid.Height; z++)
        {
            AddBossRequirement(requirements, entranceSlots, new Vector2Int(grid.Width - 1, z), GridDirection.East);
            AddBossRequirement(requirements, entranceSlots, new Vector2Int(0, z), GridDirection.West);
        }

        bossRequirement = default;
        if (requirements.Count == 0) return false;
        bossRequirement = requirements[random.Next(requirements.Count)];
        return true;
    }

    static void AddBossRequirement(List<BossRequirement> requirements, HashSet<Vector2Int> entranceSlots, Vector2Int roomCell, GridDirection direction)
    {
        if (entranceSlots.Contains(roomCell)) return;
        requirements.Add(new BossRequirement { RoomCell = roomCell, DirectionOutOfMaze = direction });
    }

    static bool TryCreateBoss(MazeLayout layout, MazeGenerationSettings settings, BossRequirement requirement, Random random, out string error)
    {
        if (!layout.TryGetRoomAtCell(requirement.RoomCell, out MazeRoomPlacement room))
        {
            error = $"No room covers boss entrance cell {requirement.RoomCell}.";
            return false;
        }

        MazePlacedOpening opening = room.Openings.Find(candidate => candidate.Cell == requirement.RoomCell && candidate.Direction == requirement.DirectionOutOfMaze);
        if (opening == null)
        {
            error = $"Boss approach room {room.Id} has no opening facing out of the maze.";
            return false;
        }

        opening.State = MazeOpeningState.ConnectedToBoss;
        layout.Boss = new MazeBossPlacement
        {
            Opening = opening,
            BossPrefab = PickRandomPrefab(settings.BossRoomPrefabs, random),
            ConnectorPrefab = PickRandomPrefab(settings.Connectors, random)
        };

        error = null;
        return true;
    }

    static bool TryBuildCriticalPaths(MazeGrid grid, BossRequirement bossRequirement, List<EntranceRequirement> entranceRequirements, Random random, out HashSet<Vector2Int> criticalCells, out HashSet<CellEdge> requiredEdges, out string error)
    {
        criticalCells = new HashSet<Vector2Int>();
        requiredEdges = new HashSet<CellEdge>();

        if (!grid.IsFree(bossRequirement.RoomCell))
        {
            error = "The selected boss entrance cell is not free.";
            return false;
        }

        Vector2Int outsideCell = bossRequirement.RoomCell + GridDirectionUtils.ToGridOffset(bossRequirement.DirectionOutOfMaze);
        criticalCells.Add(bossRequirement.RoomCell);
        requiredEdges.Add(new CellEdge(bossRequirement.RoomCell, outsideCell));

        Dictionary<CellEdge, float> edgeCosts = BuildCellEdgeCosts(grid, random);
        List<EntranceRequirement> shuffledEntrances = new List<EntranceRequirement>(entranceRequirements);
        Shuffle(shuffledEntrances, random);

        foreach (EntranceRequirement requirement in shuffledEntrances)
        {
            requiredEdges.Add(new CellEdge(requirement.RoomCell, requirement.StartCell));

            if (!TryFindCellPathToNetwork(grid, requirement.RoomCell, criticalCells, edgeCosts, out List<Vector2Int> path))
            {
                error = $"No cell path could connect entrance {requirement.BranchId} to the boss network.";
                return false;
            }

            foreach (Vector2Int cell in path) criticalCells.Add(cell);
            for (int i = 1; i < path.Count; i++) requiredEdges.Add(new CellEdge(path[i - 1], path[i]));
        }

        error = null;
        return true;
    }

    static Dictionary<CellEdge, float> BuildCellEdgeCosts(MazeGrid grid, Random random)
    {
        Dictionary<CellEdge, float> edgeCosts = new();

        for (int x = 0; x < grid.Width; x++)
        {
            for (int z = 0; z < grid.Height; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);
                if (!grid.IsFree(cell)) continue;

                for (int direction = 0; direction < 4; direction++)
                {
                    Vector2Int neighbor = cell + GridDirectionUtils.ToGridOffset((GridDirection)direction);
                    if (!grid.IsFree(neighbor)) continue;

                    CellEdge edge = new CellEdge(cell, neighbor);
                    if (!edgeCosts.TryAdd(edge, 1f + (float)random.NextDouble() * CriticalPathRandomness)) continue;
                }
            }
        }

        return edgeCosts;
    }

    static bool TryFindCellPathToNetwork(MazeGrid grid, Vector2Int source, HashSet<Vector2Int> targetCells, Dictionary<CellEdge, float> edgeCosts, out List<Vector2Int> path)
    {
        path = new List<Vector2Int>();
        if (!grid.IsFree(source)) return false;

        Dictionary<Vector2Int, float> distances = new();
        Dictionary<Vector2Int, Vector2Int> previousCells = new();
        HashSet<Vector2Int> unvisitedCells = new();

        for (int x = 0; x < grid.Width; x++)
        {
            for (int z = 0; z < grid.Height; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);
                if (!grid.IsFree(cell)) continue;
                distances[cell] = float.PositiveInfinity;
                unvisitedCells.Add(cell);
            }
        }

        distances[source] = 0f;
        Vector2Int reachedTarget = default;
        bool reachedNetwork = false;

        while (unvisitedCells.Count > 0)
        {
            Vector2Int currentCell = default;
            float currentDistance = float.PositiveInfinity;
            bool foundCell = false;

            foreach (Vector2Int cell in unvisitedCells)
            {
                if (distances[cell] >= currentDistance) continue;
                currentCell = cell;
                currentDistance = distances[cell];
                foundCell = true;
            }

            if (!foundCell || float.IsPositiveInfinity(currentDistance)) break;
            unvisitedCells.Remove(currentCell);

            if (targetCells.Contains(currentCell))
            {
                reachedTarget = currentCell;
                reachedNetwork = true;
                break;
            }

            for (int direction = 0; direction < 4; direction++)
            {
                Vector2Int neighbor = currentCell + GridDirectionUtils.ToGridOffset((GridDirection)direction);
                if (!unvisitedCells.Contains(neighbor)) continue;

                CellEdge edge = new CellEdge(currentCell, neighbor);
                float nextDistance = currentDistance + edgeCosts[edge];
                if (nextDistance >= distances[neighbor]) continue;

                distances[neighbor] = nextDistance;
                previousCells[neighbor] = currentCell;
            }
        }

        if (!reachedNetwork) return false;

        Vector2Int current = reachedTarget;
        path.Add(current);

        while (current != source)
        {
            if (!previousCells.TryGetValue(current, out current)) return false;
            path.Add(current);
        }

        path.Reverse();
        return true;
    }

    static bool TryPlaceCriticalRooms(MazeLayout layout, Dictionary<MazeRoomDefinition, List<RoomVariant>> variants, HashSet<Vector2Int> criticalCells, HashSet<CellEdge> requiredEdges, HashSet<Vector2Int> entranceSlots, Random random)
    {
        while (true)
        {
            List<PlacementCandidate> bestCandidates = null;

            foreach (Vector2Int criticalCell in criticalCells)
            {
                if (!layout.Grid.IsFree(criticalCell)) continue;

                List<PlacementCandidate> candidates = GetCriticalCandidatesCoveringCell(layout.Grid, variants, criticalCell, requiredEdges, entranceSlots);
                if (candidates.Count == 0) return false;
                if (bestCandidates == null || candidates.Count < bestCandidates.Count) bestCandidates = candidates;
            }

            if (bestCandidates == null) return true;
            PlaceRoom(layout, PickWeighted(bestCandidates, random));
        }
    }

    static List<PlacementCandidate> GetCriticalCandidatesCoveringCell(MazeGrid grid, Dictionary<MazeRoomDefinition, List<RoomVariant>> variants, Vector2Int requiredCell, HashSet<CellEdge> requiredEdges, HashSet<Vector2Int> entranceSlots)
    {
        List<PlacementCandidate> candidates = new();

        foreach (KeyValuePair<MazeRoomDefinition, List<RoomVariant>> pair in variants)
        {
            if (pair.Key.Type != MazeCellType.Normal) continue;

            foreach (RoomVariant variant in pair.Value)
            {
                foreach (Vector2Int localCell in variant.Cells)
                {
                    PlacementCandidate candidate = new PlacementCandidate { Variant = variant, Origin = requiredCell - localCell };
                    if (!CanPlace(grid, candidate)) continue;

                    int occupiedEntranceSlots = 0;
                    foreach (Vector2Int entranceSlot in entranceSlots)
                    {
                        if (CandidateOccupies(candidate, entranceSlot)) occupiedEntranceSlots++;
                    }

                    if (occupiedEntranceSlots > 1) continue;
                    if (!CandidateSupportsRequiredEdges(candidate, requiredEdges)) continue;
                    candidates.Add(candidate);
                }
            }
        }

        return candidates;
    }

    static bool CandidateSupportsRequiredEdges(PlacementCandidate candidate, HashSet<CellEdge> requiredEdges)
    {
        foreach (CellEdge edge in requiredEdges)
        {
            bool containsA = CandidateOccupies(candidate, edge.CellA);
            bool containsB = CandidateOccupies(candidate, edge.CellB);
            if (containsA == containsB) continue;

            Vector2Int insideCell = containsA ? edge.CellA : edge.CellB;
            Vector2Int outsideCell = containsA ? edge.CellB : edge.CellA;
            if (!TryGetDirection(insideCell, outsideCell, out GridDirection direction)) return false;
            if (!VariantHasOpeningAt(candidate, insideCell, direction)) return false;
        }

        return true;
    }

    static bool TryGetDirection(Vector2Int from, Vector2Int to, out GridDirection direction)
    {
        Vector2Int offset = to - from;

        for (int directionIndex = 0; directionIndex < 4; directionIndex++)
        {
            direction = (GridDirection)directionIndex;
            if (GridDirectionUtils.ToGridOffset(direction) == offset) return true;
        }

        direction = default;
        return false;
    }

    static bool TryCreateEntrances(MazeLayout layout, MazeGenerationSettings settings, List<EntranceRequirement> entranceRequirements, Random random, out string error)
    {
        foreach (EntranceRequirement requirement in entranceRequirements)
        {
            if (!layout.TryGetRoomAtCell(requirement.RoomCell, out MazeRoomPlacement room))
            {
                error = $"No room covers entrance cell {requirement.RoomCell}.";
                return false;
            }

            if (room.IsEntrance)
            {
                error = $"Room {room.Id} covers more than one village entrance.";
                return false;
            }

            MazePlacedOpening requiredOpening = room.Openings.Find(opening => opening.Cell == requirement.RoomCell && opening.Direction == requirement.DirectionIntoStart);
            if (requiredOpening == null)
            {
                error = $"Entrance room {room.Id} has no opening into the village.";
                return false;
            }

            room.IsEntrance = true;
            room.BranchId = requirement.BranchId;
            requiredOpening.State = MazeOpeningState.ConnectedToStart;

            layout.Entrances.Add(new MazeEntrance
            {
                RoomId = room.Id,
                Opening = requiredOpening,
                StartCell = requirement.StartCell,
                ConnectorPrefab = PickRandomPrefab(settings.Connectors, random)
            });
        }

        error = null;
        return true;
    }

    static bool TryFillRemainingCells(MazeLayout layout, Dictionary<MazeRoomDefinition, List<RoomVariant>> variants, Random random)
    {
        while (true)
        {
            List<PlacementCandidate> bestCandidates = null;

            for (int x = 0; x < layout.Grid.Width; x++)
            {
                for (int z = 0; z < layout.Grid.Height; z++)
                {
                    Vector2Int cell = new Vector2Int(x, z);
                    if (!layout.Grid.IsFree(cell)) continue;

                    List<PlacementCandidate> candidates = GetNormalCandidatesCoveringCell(layout.Grid, variants, cell, null, null);
                    if (candidates.Count == 0) return false;
                    if (bestCandidates == null || candidates.Count < bestCandidates.Count) bestCandidates = candidates;
                }
            }

            if (bestCandidates == null) return true;
            PlaceRoom(layout, PickWeighted(bestCandidates, random));
        }
    }

    static List<PlacementCandidate> GetNormalCandidatesCoveringCell(MazeGrid grid, Dictionary<MazeRoomDefinition, List<RoomVariant>> variants, Vector2Int requiredCell, GridDirection? requiredOpeningDirection, HashSet<Vector2Int> entranceSlots)
    {
        List<PlacementCandidate> candidates = new();

        foreach (KeyValuePair<MazeRoomDefinition, List<RoomVariant>> pair in variants)
        {
            if (pair.Key.Type != MazeCellType.Normal) continue;

            foreach (RoomVariant variant in pair.Value)
            {
                foreach (Vector2Int localCell in variant.Cells)
                {
                    PlacementCandidate candidate = new PlacementCandidate { Variant = variant, Origin = requiredCell - localCell };
                    if (!CanPlace(grid, candidate)) continue;

                    if (entranceSlots != null)
                    {
                        bool occupiesOtherEntrance = false;

                        foreach (Vector2Int entranceSlot in entranceSlots)
                        {
                            if (entranceSlot == requiredCell) continue;
                            if (!CandidateOccupies(candidate, entranceSlot)) continue;
                            occupiesOtherEntrance = true;
                            break;
                        }

                        if (occupiesOtherEntrance) continue;
                    }

                    if (requiredOpeningDirection.HasValue && !VariantHasOpeningAt(candidate, requiredCell, requiredOpeningDirection.Value)) continue;
                    candidates.Add(candidate);
                }
            }
        }

        return candidates;
    }

    static bool CanPlace(MazeGrid grid, PlacementCandidate candidate)
    {
        foreach (Vector2Int localCell in candidate.Variant.Cells)
        {
            if (!grid.IsFree(candidate.Origin + localCell)) return false;
        }

        return true;
    }

    static bool CandidateOccupies(PlacementCandidate candidate, Vector2Int cell)
    {
        foreach (Vector2Int localCell in candidate.Variant.Cells)
        {
            if (candidate.Origin + localCell == cell) return true;
        }

        return false;
    }

    static bool VariantHasOpeningAt(PlacementCandidate candidate, Vector2Int cell, GridDirection direction)
    {
        foreach (VariantOpening opening in candidate.Variant.Openings)
        {
            if (candidate.Origin + opening.Cell == cell && opening.Direction == direction) return true;
        }

        return false;
    }

    static MazeRoomPlacement PlaceRoom(MazeLayout layout, PlacementCandidate candidate)
    {
        int roomId = layout.Rooms.Count;
        MazeRoomPlacement room = new MazeRoomPlacement
        {
            Id = roomId,
            Definition = candidate.Variant.Definition,
            GridOrigin = candidate.Origin,
            PrefabOriginCell = candidate.Origin + candidate.Variant.AnchorCell,
            Rotation = candidate.Variant.Rotation
        };

        foreach (Vector2Int localCell in candidate.Variant.Cells)
        {
            Vector2Int cell = candidate.Origin + localCell;
            room.OccupiesCells.Add(cell);
            layout.Grid.Occupy(cell, roomId);
        }

        foreach (VariantOpening opening in candidate.Variant.Openings)
        {
            room.Openings.Add(new MazePlacedOpening
            {
                RoomId = roomId,
                OpeningIndex = opening.DefinitionOpeningIndex,
                Cell = candidate.Origin + opening.Cell,
                Direction = opening.Direction
            });
        }

        layout.Rooms.Add(roomId, room);
        return room;
    }

    static bool TryBuildPotentialConnections(MazeLayout layout, out string error)
    {
        Dictionary<OpeningKey, MazePlacedOpening> openings = new();

        foreach (MazeRoomPlacement room in layout.Rooms.Values)
        {
            foreach (MazePlacedOpening opening in room.Openings)
            {
                if (opening.State != MazeOpeningState.Unresolved) continue;

                OpeningKey key = new OpeningKey(opening.Cell, opening.Direction);
                if (openings.TryAdd(key, opening)) continue;

                error = $"Duplicate opening at {opening.Cell} facing {opening.Direction}.";
                return false;
            }
        }

        int connectionId = 0;

        foreach (MazePlacedOpening opening in openings.Values)
        {
            Vector2Int neighborCell = opening.Cell + GridDirectionUtils.ToGridOffset(opening.Direction);
            OpeningKey oppositeKey = new OpeningKey(neighborCell, GridDirectionUtils.Opposite(opening.Direction));
            if (!openings.TryGetValue(oppositeKey, out MazePlacedOpening opposite)) continue;
            if (opening.RoomId == opposite.RoomId || opening.RoomId > opposite.RoomId) continue;

            layout.PotentialConnections.Add(new MazePotentialConnection
            {
                Id = connectionId++,
                OpeningA = opening,
                OpeningB = opposite
            });
        }

        error = null;
        return true;
    }

    static bool TryBuildConnectionGraph(MazeLayout layout, MazeGenerationSettings settings, HashSet<CellEdge> requiredEdges, Random random, out string error)
    {
        Dictionary<CellEdge, MazePotentialConnection> connectionsByCellEdge = new();

        foreach (MazePotentialConnection connection in layout.PotentialConnections)
        {
            CellEdge edge = new CellEdge(connection.OpeningA.Cell, connection.OpeningB.Cell);
            connectionsByCellEdge[edge] = connection;
        }

        HashSet<int> selectedEdgeIds = new();

        foreach (CellEdge edge in requiredEdges)
        {
            if (!layout.Grid.TryGetRoomId(edge.CellA, out int roomA) || !layout.Grid.TryGetRoomId(edge.CellB, out int roomB)) continue;
            if (roomA == roomB) continue;

            if (!connectionsByCellEdge.TryGetValue(edge, out MazePotentialConnection connection))
            {
                error = $"Required connection between {edge.CellA} and {edge.CellB} has no matching room openings.";
                return false;
            }

            AddConnection(layout, settings, random, connection, selectedEdgeIds, true);
        }

        if (layout.Boss == null || layout.Boss.Opening == null)
        {
            error = "The external boss entrance was not created.";
            return false;
        }

        List<MazePotentialConnection> allowedEdges = layout.PotentialConnections;
        HashSet<int> connectedRooms = GetReachableRooms(layout.Boss.Opening.RoomId, allowedEdges, selectedEdgeIds);

        foreach (MazeEntrance entrance in layout.Entrances)
        {
            if (!connectedRooms.Contains(entrance.RoomId))
            {
                error = $"Critical entrance room {entrance.RoomId} is not connected to the boss.";
                return false;
            }
        }

        while (connectedRooms.Count < layout.Rooms.Count)
        {
            List<MazePotentialConnection> frontier = new();

            foreach (MazePotentialConnection edge in allowedEdges)
            {
                if (selectedEdgeIds.Contains(edge.Id)) continue;

                bool aConnected = connectedRooms.Contains(edge.OpeningA.RoomId);
                bool bConnected = connectedRooms.Contains(edge.OpeningB.RoomId);
                if (aConnected != bConnected) frontier.Add(edge);
            }

            if (frontier.Count == 0)
            {
                error = "Some rooms could not be connected to the guaranteed network.";
                return false;
            }

            MazePotentialConnection selected = frontier[random.Next(frontier.Count)];
            AddConnection(layout, settings, random, selected, selectedEdgeIds, false);
            connectedRooms.Add(selected.OpeningA.RoomId);
            connectedRooms.Add(selected.OpeningB.RoomId);
        }

        AddDetourShortcuts(layout, settings, random, allowedEdges, selectedEdgeIds);

        foreach (MazePotentialConnection edge in allowedEdges)
        {
            if (selectedEdgeIds.Contains(edge.Id) || random.NextDouble() > settings.ExtraConnectionChance) continue;
            AddConnection(layout, settings, random, edge, selectedEdgeIds, false);
        }

        error = null;
        return true;
    }

    static void AddDetourShortcuts(MazeLayout layout, MazeGenerationSettings settings, Random random, List<MazePotentialConnection> allowedEdges, HashSet<int> selectedEdgeIds)
    {
        int maximumDetour = Mathf.Max(2, settings.MaximumConnectionDetour);

        while (true)
        {
            int longestDetour = maximumDetour;
            List<MazePotentialConnection> bestShortcuts = new();

            foreach (MazePotentialConnection edge in allowedEdges)
            {
                if (selectedEdgeIds.Contains(edge.Id)) continue;

                int detour = GetConnectionDistance(edge.OpeningA.RoomId, edge.OpeningB.RoomId, allowedEdges, selectedEdgeIds);
                if (detour <= maximumDetour || detour < longestDetour) continue;

                if (detour > longestDetour)
                {
                    longestDetour = detour;
                    bestShortcuts.Clear();
                }

                bestShortcuts.Add(edge);
            }

            if (bestShortcuts.Count == 0) return;
            AddConnection(layout, settings, random, bestShortcuts[random.Next(bestShortcuts.Count)], selectedEdgeIds, false);
        }
    }

    static int GetConnectionDistance(int startRoomId, int targetRoomId, List<MazePotentialConnection> edges, HashSet<int> selectedEdgeIds)
    {
        HashSet<int> reachedRooms = new HashSet<int> { startRoomId };
        Queue<(int RoomId, int Distance)> roomsToCheck = new Queue<(int, int)>();
        roomsToCheck.Enqueue((startRoomId, 0));

        while (roomsToCheck.Count > 0)
        {
            (int roomId, int distance) = roomsToCheck.Dequeue();

            foreach (MazePotentialConnection edge in edges)
            {
                if (!selectedEdgeIds.Contains(edge.Id) || !edge.TouchesRoom(roomId)) continue;

                int nextRoomId = edge.GetOtherRoomId(roomId);
                if (nextRoomId == targetRoomId) return distance + 1;
                if (!reachedRooms.Add(nextRoomId)) continue;
                roomsToCheck.Enqueue((nextRoomId, distance + 1));
            }
        }

        return int.MaxValue;
    }

    static HashSet<int> GetReachableRooms(int startRoomId, List<MazePotentialConnection> edges, HashSet<int> selectedEdgeIds)
    {
        HashSet<int> reachedRooms = new HashSet<int> { startRoomId };
        Queue<int> roomsToCheck = new Queue<int>();
        roomsToCheck.Enqueue(startRoomId);

        while (roomsToCheck.Count > 0)
        {
            int roomId = roomsToCheck.Dequeue();

            foreach (MazePotentialConnection edge in edges)
            {
                if (!selectedEdgeIds.Contains(edge.Id) || !edge.TouchesRoom(roomId)) continue;

                int nextRoomId = edge.GetOtherRoomId(roomId);
                if (!reachedRooms.Add(nextRoomId)) continue;
                roomsToCheck.Enqueue(nextRoomId);
            }
        }

        return reachedRooms;
    }

    static void AddConnection(MazeLayout layout, MazeGenerationSettings settings, Random random, MazePotentialConnection potential, HashSet<int> selectedEdgeIds, bool isCritical)
    {
        if (!selectedEdgeIds.Add(potential.Id)) return;

        potential.OpeningA.State = MazeOpeningState.ConnectedToRoom;
        potential.OpeningB.State = MazeOpeningState.ConnectedToRoom;

        layout.Connections.Add(new MazeConnection
        {
            PotentialConnection = potential,
            ConnectorPrefab = PickRandomPrefab(settings.Connectors, random),
            IsCritical = isCritical
        });
    }

    static void ResolveUnusedOpenings(MazeLayout layout, MazeGenerationSettings settings, Random random)
    {
        foreach (MazeRoomPlacement room in layout.Rooms.Values)
        {
            foreach (MazePlacedOpening opening in room.Openings)
            {
                if (opening.State != MazeOpeningState.Unresolved) continue;
                opening.State = MazeOpeningState.Filled;
                opening.FillerPrefab = PickRandomPrefab(settings.OpeningFillers, random);
            }
        }
    }

    static PlacementCandidate PickWeighted(List<PlacementCandidate> candidates, Random random)
    {
        float totalWeight = 0f;
        foreach (PlacementCandidate candidate in candidates) totalWeight += Mathf.Max(0f, candidate.Weight);
        if (totalWeight <= 0f) return candidates[random.Next(candidates.Count)];

        double roll = random.NextDouble() * totalWeight;

        foreach (PlacementCandidate candidate in candidates)
        {
            roll -= Mathf.Max(0f, candidate.Weight);
            if (roll <= 0d) return candidate;
        }

        return candidates[candidates.Count - 1];
    }

    static GameObject PickRandomPrefab(GameObject[] prefabs, Random random)
    {
        if (!HasPrefab(prefabs)) return null;

        int startIndex = random.Next(prefabs.Length);
        for (int offset = 0; offset < prefabs.Length; offset++)
        {
            GameObject prefab = prefabs[(startIndex + offset) % prefabs.Length];
            if (prefab != null) return prefab;
        }

        return null;
    }

    static bool HasPrefab(GameObject[] prefabs)
    {
        if (prefabs == null) return false;
        foreach (GameObject prefab in prefabs) if (prefab != null) return true;
        return false;
    }

    static void Shuffle<T>(IList<T> list, Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int otherIndex = random.Next(i + 1);
            (list[i], list[otherIndex]) = (list[otherIndex], list[i]);
        }
    }
}
