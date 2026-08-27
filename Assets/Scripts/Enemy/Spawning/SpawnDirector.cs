using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
    public float minPurchase;
    public float elapsedTime;
    public float nextSpawnAttemptTime;

    public Director(bool active, float baseIncome, float credits, float minPurchase)
    {
        this.active = active;
        this.baseIncome = baseIncome;
        this.income = baseIncome;
        this.credits = credits;
        this.minPurchase = minPurchase;
        this.elapsedTime = 0f;
        nextSpawnAttemptTime = 0f;
    }
}

public class SpawnDirector : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] int spawnCap = 32;
    [SerializeField] float slowIncome = 1f;
    [SerializeField] float slowMinPurchase = 100f;
    [SerializeField, Range(0f, 1f)] float slowFailChance = 0.1f;
    [SerializeField] float fastIncome = 10f;
    [SerializeField] float fastMinPurchase = 10f;
    [SerializeField, Range(0f, 1f)] float fastFailChance = .6f;
    [SerializeField] private EnemySpawnGrouping spawnGrouping;

    [Header("Spawn Placement")]
    [SerializeField, Min(0f)] float minSpawnDistance = 12f;
    [SerializeField, Min(0f)] float maxSpawnDistance = 30f;
    [SerializeField, Min(0f)] float navMeshSampleRadius = 2f; // how far a random candidate may be projected onto the navmesh
    [SerializeField, Min(0f)] float spawnAttemptInterval = .25f;
    [SerializeField, Min(1)] int maxSpawnAttempts = 12;
    [SerializeField, Min(0f)] float spawnCapsuleGroundOffset = .05f;
    [SerializeField] LayerMask spawnBlockingLayers;

    [Header("Growth")]
    [SerializeField] private GrowthType slowGrowthType = GrowthType.Linear;
    [SerializeField] private GrowthType fastGrowthType = GrowthType.Logistic;

    [Header("Growth Tuning")]
    [ShowIf(nameof(UsesLinear))]
    [SerializeField] float linearRatePerSecond = 0.1f;  
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

        slowDirector = new Director(true, slowIncome, 0, slowMinPurchase);
        fastDirector = new Director(true, fastIncome, 0, fastMinPurchase);

        slowGrowthFn = ResolveGrowthFunction(slowGrowthType);
        fastGrowthFn = ResolveGrowthFunction(fastGrowthType);

        spawnedEnemies = new();
        mazeGenerator = FindFirstObjectByType<MazeGenerator>();
    }

    // Update is called once per frame
    void Update()
    {
        // director only works when not in village
        if (mazeGenerator.TryWorldToCell(Player.Instance.transform.position, out Vector2Int cell) && mazeGenerator.CurrentLayout.Grid.GetCell(cell).State == MazeCellState.ReservedForVillage)
        {
            return;
        }
        
        slowBehavior();
        fastBehavior();
    }

    List<EnemySpawnCard> SelectSlowPurchase(ref Director director, EnemySpawnGrouping grouping, int maxSpawns)
    {
        List<EnemySpawnCard> purchased = new();

        while (purchased.Count < maxSpawns)
        {
            EnemySpawnCard best = null;
            int bestCost = -1;

            foreach (var entry in grouping.Entries)
            {
                int cost = entry.Card.creditCost;
                if (cost <= director.credits && cost > bestCost)
                {
                    best = entry.Card;
                    bestCost = cost;
                }
            }

            if (best == null) break; // nothing left to buy

            director.credits -= bestCost;
            purchased.Add(best);
        }

        return purchased;
    }

    List<EnemySpawnCard> SelectFastPurchase(ref Director director, EnemySpawnGrouping grouping, int maxSpawns)
    {
        // Ideally we would have these behave differently. Right now it's a stub that calls the same selector
        return SelectSlowPurchase(ref director, grouping, maxSpawns);
    }

    void slowBehavior()
    {
        if (!slowDirector.active) return;
        Grow(ref slowDirector, slowGrowthFn);
        if (Time.time < slowDirector.nextSpawnAttemptTime) return;
        slowDirector.nextSpawnAttemptTime = Time.time + spawnAttemptInterval;

        if (slowDirector.credits >= slowDirector.minPurchase && UnityEngine.Random.value >= slowFailChance) // enough credits and spawn chance rolls
        {
            if (spawnedEnemies.Count < spawnCap)
            {
                int remainingSpawns = spawnCap - spawnedEnemies.Count;
                // Select purchase. For now the function selects the most expensive available options and as many as it can afford
                // purchase is an array of all the things to spawn. The spawning code should iterate through each one to find the location
                // and perform the spawn. If a spawn isn't possible the credits should be refunded
                List<EnemySpawnCard> purchase = SelectSlowPurchase(ref slowDirector, spawnGrouping, remainingSpawns);

                foreach(var spawn in purchase)
                {
                    TrySpawn(ref slowDirector, spawn);
                }
            }
        }
    }

    void fastBehavior()
    {
        if (!fastDirector.active) return;
        Grow(ref fastDirector, fastGrowthFn);
        if (Time.time < fastDirector.nextSpawnAttemptTime) return;
        fastDirector.nextSpawnAttemptTime = Time.time + spawnAttemptInterval;

        if (fastDirector.credits >= fastDirector.minPurchase && UnityEngine.Random.value >= fastFailChance) // enough credits and spawn chance rolls
        {
            if (spawnedEnemies.Count < spawnCap)
            {
                int remainingSpawns = spawnCap - spawnedEnemies.Count;
                // Select purchase. For now the function selects the most expensive available options and as many as it can afford
                // purchase is an array of all the things to spawn. The spawning code should iterate through each one to find the location
                // and perform the spawn. If a spawn isn't possible the credits should be refunded
                List<EnemySpawnCard> purchase = SelectFastPurchase(ref fastDirector, spawnGrouping, remainingSpawns);

                foreach (var spawn in purchase)
                {
                    TrySpawn(ref fastDirector, spawn);
                }
            }
        }
    }

    void TrySpawn(ref Director director, EnemySpawnCard spawnCard)
    {
        if (!TryGetSpawnPosition(spawnCard, out Vector3 spawnPosition) || EnemySpawner.Instance == null)
        {
            director.credits += spawnCard.creditCost;
            return;
        }

        Enemy enemy = EnemySpawner.Instance.Spawn(spawnCard, spawnPosition);

        if (enemy == null)
        {
            director.credits += spawnCard.creditCost;
            return;
        }

        if (spawnedEnemies.Add(enemy))
        {
            enemy.OnDied += RemoveSpawnedEnemy;
        }
    }

    bool TryGetSpawnPosition(EnemySpawnCard spawnCard, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        if (Player.Instance == null || spawnCard == null || spawnCard.Prefab == null) return false;
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

    void Refund(ref Director director, EnemySpawnCard card)
    {
        director.credits += card.creditCost;
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
    [Button("Force Slow Director Purchase", EButtonEnableMode.Playmode)]
    private void TestForceSlowPurchase()
    {
        var purchase = SelectSlowPurchase(ref slowDirector, spawnGrouping, spawnCap - spawnedEnemies.Count);
        UnityEngine.Debug.Log($"[SpawnDirector - Debug]: Purchased {purchase.Count} cards, credits left {slowDirector.credits}.");
    }

    [ShowIf(nameof(showDebugButtons))]
    [Button("Force Fast Director Purchase", EButtonEnableMode.Playmode)]
    private void TestForceFastPurchase()
    {
        var purchase = SelectFastPurchase(ref fastDirector, spawnGrouping, spawnCap - spawnedEnemies.Count);
        UnityEngine.Debug.Log($"[SpawnDirector - Debug]: Purchased {purchase.Count} cards, credits left {fastDirector.credits}.");
    }

    [ShowIf(nameof(showDebugButtons))]
    [Button("Reset Directors", EButtonEnableMode.Playmode)]
    private void TestResetDirectors()
    {
        slowDirector = new Director(true, slowIncome, 0, slowMinPurchase);
        fastDirector = new Director(true, fastIncome, 0, fastMinPurchase);
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
