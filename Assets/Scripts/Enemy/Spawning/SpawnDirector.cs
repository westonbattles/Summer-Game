using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using NaughtyAttributes;

public enum GrowthType
{
    Flat,       // income doesn't change
    Linear,     // income increases by a fixed amount per second
    Exponential,// income compounds over time
    Logistic    // income ramps up then stops growing
}
struct Director
{
    public bool active;
    public float baseIncome;
    public float income;
    public float credits;
    public float elapsedTime;
    public float nextSpawnAttemptTime;
    public EnemySpawnCard selectedCard;
    public int spawnsInSeries;

    public Director(bool active, float baseIncome, float credits)
    {
        this.active = active;
        this.baseIncome = baseIncome;
        this.income = baseIncome;
        this.credits = credits;
        this.elapsedTime = 0f;
        nextSpawnAttemptTime = 0f;
        selectedCard = null;
        spawnsInSeries = 0;
    }
}

public class SpawnDirector : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] int spawnCap = 32;
    [SerializeField] float slowIncome = 1f;
    [SerializeField] float fastIncome = 1f;
    [SerializeField] private EnemySpawnGrouping spawnGrouping;

    [Header("Spawn Timing")]
    [SerializeField] Vector2 slowSeriesBreak = new Vector2(22.5f, 30f);
    [SerializeField] Vector2 fastSeriesBreak = new Vector2(4.5f, 9f);
    [SerializeField] Vector2 withinSeriesInterval = new Vector2(0.1f, 1f);
    [SerializeField, Min(1)] int maxSpawnsPerSeries = 6;

    [Header("Spawn Placement")]
    [SerializeField, Min(0f)] float minSpawnDistance = 12f;
    [SerializeField, Min(0f)] float maxSpawnDistance = 30f;
    [SerializeField, Min(0f)] float navMeshSampleRadius = 2f; // how far a random candidate may be projected onto the navmesh
    [SerializeField, Min(1)] int maxSpawnAttempts = 12;
    [SerializeField, Min(0f)] float spawnCapsuleGroundOffset = .05f;
    [SerializeField] LayerMask spawnBlockingLayers;

    [Header("Growth")]
    [SerializeField] private GrowthType slowGrowthType = GrowthType.Linear;
    [SerializeField] private GrowthType fastGrowthType = GrowthType.Linear;

    [Header("Growth Tuning")]
    [ShowIf(nameof(UsesLinear))]
    [SerializeField] float linearRatePerSecond = 0.0005f;
    [ShowIf(nameof(UsesExponential))]
    [SerializeField] float exponentialGrowthRate = 1.02f;
    [ShowIf(nameof(UsesLogistic))]
    [SerializeField] float logisticMaxIncome = 20f;
    [ShowIf(nameof(UsesLogistic))]
    [SerializeField] float logisticSteepness = 0.05f;
    [ShowIf(nameof(UsesLogistic))]
    [SerializeField] float logisticMidpointTime = 120f;

    bool UsesLinear => slowGrowthType == GrowthType.Linear || fastGrowthType == GrowthType.Linear;
    bool UsesExponential => slowGrowthType == GrowthType.Exponential || fastGrowthType == GrowthType.Exponential;
    bool UsesLogistic => slowGrowthType == GrowthType.Logistic || fastGrowthType == GrowthType.Logistic;

    public HashSet<Enemy> spawnedEnemies {  get; private set; }

    // created in start
    Director slowDirector;
    Director fastDirector;

    Func<Director, float> slowGrowthFn;
    Func<Director, float> fastGrowthFn;
    
    MazeGenerator mazeGenerator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (spawnBlockingLayers.value == 0) spawnBlockingLayers = ~LayerMask.GetMask("Ground");

        slowDirector = new Director(true, slowIncome, 0f);
        fastDirector = new Director(true, fastIncome, 0f);

        slowGrowthFn = ResolveGrowthFunction(slowGrowthType);
        fastGrowthFn = ResolveGrowthFunction(fastGrowthType);

        spawnedEnemies = new();
        mazeGenerator = FindFirstObjectByType<MazeGenerator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (mazeGenerator == null || mazeGenerator.CurrentLayout == null || Player.Instance == null) return;

        // director only works when not in village
        if (mazeGenerator.TryWorldToCell(Player.Instance.transform.position, out Vector2Int cell) && mazeGenerator.CurrentLayout.Grid.GetCell(cell).State == MazeCellState.ReservedForVillage)
        {
            return;
        }
        
        slowBehavior();
        fastBehavior();
    }

    void slowBehavior()
    {
        TickDirector(ref slowDirector, slowGrowthFn, slowSeriesBreak);
    }

    void fastBehavior()
    {
        TickDirector(ref fastDirector, fastGrowthFn, fastSeriesBreak);
    }

    void TickDirector(ref Director director, Func<Director, float> growthFn, Vector2 seriesBreak)
    {
        if (!director.active) return;

        // Keep earning during breaks. This clock pauses with income while in the village.
        Grow(ref director, growthFn);
        if (director.elapsedTime < director.nextSpawnAttemptTime) return;

        AttemptSpawn(ref director, seriesBreak);
    }

    bool AttemptSpawn(ref Director director, Vector2 seriesBreak)
    {
        if (spawnedEnemies.Count >= spawnCap || director.spawnsInSeries >= maxSpawnsPerSeries)
        {
            EndSeries(ref director, seriesBreak);
            return false;
        }

        if (director.selectedCard == null) director.selectedCard = SelectSeriesCard();

        EnemySpawnCard card = director.selectedCard;
        if (card == null || card.Prefab == null || card.creditCost <= 0 ||
            director.credits < card.creditCost || IsTooCheap(card, director.credits))
        {
            EndSeries(ref director, seriesBreak);
            return false;
        }

        if (!TrySpawn(card))
        {
            EndSeries(ref director, seriesBreak);
            return false;
        }

        director.credits -= card.creditCost;
        director.spawnsInSeries++;
        director.nextSpawnAttemptTime = director.elapsedTime +
            UnityEngine.Random.Range(withinSeriesInterval.x, withinSeriesInterval.y);
        return true;
    }

    void EndSeries(ref Director director, Vector2 seriesBreak)
    {
        // Keep savings, but reroll the enemy type after a longer randomized break.
        director.selectedCard = null;
        director.spawnsInSeries = 0;
        director.nextSpawnAttemptTime = director.elapsedTime +
            UnityEngine.Random.Range(seriesBreak.x, seriesBreak.y);
    }

    EnemySpawnCard SelectSeriesCard()
    {
        if (spawnGrouping == null) return null;

        EnemySpawnCard selected = null;
        float totalWeight = 0f;

        foreach (var entry in spawnGrouping.Entries)
        {
            if (entry == null || entry.Weight <= 0f) continue;

            EnemySpawnCard card = entry.Card;
            if (card == null || card.Prefab == null || card.creditCost <= 0) continue;

            // Include unaffordable cards: failing to afford a choice creates a saving period.
            totalWeight += entry.Weight;
            if (selected == null || UnityEngine.Random.value < entry.Weight / totalWeight) selected = card;
        }

        return selected;
    }

    bool IsTooCheap(EnemySpawnCard card, float credits)
    {
        if (spawnGrouping == null || credits <= (float)card.creditCost * maxSpawnsPerSeries) return false;

        // Prefer larger purchases when savings are high, while always allowing the most expensive card.
        // Elite variants can extend this check with their effective costs later.
        foreach (var entry in spawnGrouping.Entries)
        {
            if (entry == null || entry.Weight <= 0f) continue;

            EnemySpawnCard other = entry.Card;
            if (other != null && other.Prefab != null && other.creditCost > card.creditCost) return true;
        }

        return false;
    }

    bool TrySpawn(EnemySpawnCard spawnCard)
    {
        if (EnemySpawner.Instance == null || !TryGetSpawnPosition(spawnCard, out Vector3 spawnPosition)) return false;

        Enemy enemy = EnemySpawner.Instance.Spawn(spawnCard, spawnPosition);
        if (enemy == null) return false;

        if (spawnedEnemies.Add(enemy))
        {
            enemy.OnDied += RemoveSpawnedEnemy;
        }

        return true;
    }

    bool TryGetSpawnPosition(EnemySpawnCard spawnCard, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        if (mazeGenerator == null || mazeGenerator.CurrentLayout == null ||
            Player.Instance == null || spawnCard == null || spawnCard.Prefab == null) return false;
        if (!spawnCard.Prefab.TryGetComponent(out NavMeshAgent agent)) return false;

        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };

        Vector3 playerPosition = Player.Instance.transform.position;

        float minDistance = Mathf.Max(0f, minSpawnDistance);
        float maxDistance = Mathf.Max(minDistance, maxSpawnDistance);
        float minDistanceSquared = minDistance * minDistance;
        float maxDistanceSquared = maxDistance * maxDistance;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            float distance = Mathf.Sqrt(UnityEngine.Random.Range(minDistanceSquared, maxDistanceSquared));
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

            Vector3 candidate = playerPosition + new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance);

            // dont spawn enemies in village
            if (mazeGenerator.TryWorldToCell(candidate, out Vector2Int cell) && mazeGenerator.CurrentLayout.Grid.GetCell(cell).State == MazeCellState.ReservedForVillage) continue;
            
            // only spawn enemy if theres a navmesh position navMeshSampleRadius away from our candidate
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit spawnHit, navMeshSampleRadius, filter)) continue;

            float projectedDistanceSquared = (spawnHit.position - playerPosition).sqrMagnitude;
            if (projectedDistanceSquared < minDistanceSquared || projectedDistanceSquared > maxDistanceSquared) continue;

            // ensure navmesh enemy actually has room to spawn (so we dont try to spawn it in like a micro gap or something)
            if (!HasSpawnRoom(agent, spawnHit.position)) continue;
            
            spawnPosition = spawnHit.position;
            return true;
        }

        return false;
    }

    bool HasSpawnRoom(NavMeshAgent agent, Vector3 spawnPosition)
    {
        float radius = agent.radius;
        float height = Mathf.Max(agent.height, radius * 2f);
        float halfLineLength = height * .5f - radius;
        Vector3 center = spawnPosition + Vector3.up * (agent.baseOffset + spawnCapsuleGroundOffset);
        Vector3 bottom = center - Vector3.up * halfLineLength;
        Vector3 top = center + Vector3.up * halfLineLength;

        return !Physics.CheckCapsule(
            bottom,
            top,
            radius,
            spawnBlockingLayers,
            QueryTriggerInteraction.Ignore);
    }

    void RemoveSpawnedEnemy(Enemy enemy)
    {
        enemy.OnDied -= RemoveSpawnedEnemy;
        spawnedEnemies.Remove(enemy);
    }

    void OnDestroy()
    {
        if (spawnedEnemies == null) return;

        foreach (Enemy enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                enemy.OnDied -= RemoveSpawnedEnemy;
            }
        }
    }

    void Grow(ref Director director, Func<Director, float> growthFn)
    {
        director.elapsedTime += Time.deltaTime;
        director.income = growthFn(director);
        director.credits += director.income * Time.deltaTime;
    }

    Func<Director, float> ResolveGrowthFunction(GrowthType type)
    {
        switch (type)
        {
            case GrowthType.Linear: 
                return LinearGrowth;
            case GrowthType.Exponential:
                return ExponentialGrowth;
            case GrowthType.Logistic:
                return LogisticGrowth;
            default:
                return FlatGrowth;
        }
    }

    // Growth Formulas
    // All take the director as an argument and return
    // the income value for this frame. This is modular and has room for new functions
    // so feel free to add more here if you think it could be good to test. NOTE: make sure
    // to wire them into ResolveGrowthFunction and the enum if you do

    float FlatGrowth(Director d) => d.baseIncome;

    float LinearGrowth(Director d) => d.baseIncome + linearRatePerSecond * d.elapsedTime;

    float ExponentialGrowth(Director d) => d.baseIncome * Mathf.Pow(exponentialGrowthRate, d.elapsedTime);

    float LogisticGrowth(Director d)
    {
        // starts near baseIncome, ramps hardest at logisticMidpointTime, and flattens toward logisticMaxIncome
        float sigmoid = 1f / (1f + Mathf.Exp(-logisticSteepness * (d.elapsedTime - logisticMidpointTime))); // I'm a sigmoid male. super rare. you haven't heard of it I bet
        return d.baseIncome + (logisticMaxIncome - d.baseIncome) * sigmoid;
    }

#if UNITY_EDITOR
    #region DEBUGGING
    [Header("Debug Settings")]
#pragma warning disable CS0414
    [SerializeField] private bool showDebugButtons = false;
#pragma warning restore CS0414

    [ShowNativeProperty] float SlowCredits => slowDirector.credits;
    [ShowNativeProperty] float SlowIncome => slowDirector.income;
    [ShowNativeProperty] float FastCredits => fastDirector.credits;
    [ShowNativeProperty] float FastIncome => fastDirector.income;
    [ShowNativeProperty] int SpawnedCount => spawnedEnemies?.Count ?? 0;
    [ShowNativeProperty] float SlowElapsed => slowDirector.elapsedTime;
    [ShowNativeProperty] float FastElapsed => fastDirector.elapsedTime;
    [ShowNativeProperty] int SlowSeriesSpawns => slowDirector.spawnsInSeries;
    [ShowNativeProperty] int FastSeriesSpawns => fastDirector.spawnsInSeries;

    [ShowIf(nameof(showDebugButtons))]
    [Button("Give Slow Director 500 Credits", EButtonEnableMode.Playmode)]
    private void TestGrantSlowCredits()
    {
        slowDirector.credits += 500f;
    }

    [ShowIf(nameof(showDebugButtons))]
    [Button("Give Fast Director 500 Credits", EButtonEnableMode.Playmode)]
    private void TestGrantFastCredits()
    {
        fastDirector.credits += 500f;
    }

    [ShowIf(nameof(showDebugButtons))]
    [Button("Attempt Slow Director Spawn", EButtonEnableMode.Playmode)]
    private void TestForceSlowSpawn()
    {
        bool spawned = AttemptSpawn(ref slowDirector, slowSeriesBreak);
        UnityEngine.Debug.Log($"[SpawnDirector - Debug]: Slow spawn succeeded: {spawned}, credits left {slowDirector.credits}.");
    }

    [ShowIf(nameof(showDebugButtons))]
    [Button("Attempt Fast Director Spawn", EButtonEnableMode.Playmode)]
    private void TestForceFastSpawn()
    {
        bool spawned = AttemptSpawn(ref fastDirector, fastSeriesBreak);
        UnityEngine.Debug.Log($"[SpawnDirector - Debug]: Fast spawn succeeded: {spawned}, credits left {fastDirector.credits}.");
    }

    [ShowIf(nameof(showDebugButtons))]
    [Button("Reset Directors", EButtonEnableMode.Playmode)]
    private void TestResetDirectors()
    {
        slowDirector = new Director(true, slowIncome, 0f);
        fastDirector = new Director(true, fastIncome, 0f);
        UnityEngine.Debug.Log("[SpawnDirector - Debug]: Directors reset.");
    }

    [ShowIf(nameof(showDebugButtons))]
    [Button("Toggle Slow Active", EButtonEnableMode.Playmode)]
    private void TestToggleSlow()
    {
        slowDirector.active = !slowDirector.active;
    }

    [ShowIf(nameof(showDebugButtons))]
    [Button("Toggle Fast Active", EButtonEnableMode.Playmode)]
    private void TestToggleFast()
    {
        fastDirector.active = !fastDirector.active;
    }
    #endregion
#endif
}
