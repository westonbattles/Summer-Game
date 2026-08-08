using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;

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

    public Director(bool active, float baseIncome, float credits, float minPurchase)
    {
        this.active = active;
        this.baseIncome = baseIncome;
        this.income = baseIncome;
        this.credits = credits;
        this.minPurchase = minPurchase;
        this.elapsedTime = 0f;
    }
}

public class SpawnDirector : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] int spawnCap = 32;
    [SerializeField] float slowIncome = 1f;
    [SerializeField] float slowMinPurchase = 100f;
    [SerializeField] float slowFailChance = 0.1f;
    [SerializeField] float fastIncome = 10f;
    [SerializeField] float fastMinPurchase = 10f;
    [SerializeField] float fastFailChance = .6f;
    [SerializeField] private EnemySpawnGrouping spawnGrouping;

    [Header("Growth")]
    [SerializeField] private GrowthType slowGrowthType = GrowthType.Linear;
    [SerializeField] private GrowthType fastGrowthType = GrowthType.Logistic;

    [Header("Growth Tuning")]
    [SerializeField] float linearRatePerSecond = 0.1f;
    [SerializeField] float exponentialGrowthRate = 1.02f;
    [SerializeField] float logisticMaxIncome = 20f;
    [SerializeField] float logisticSteepness = 0.05f;
    [SerializeField] float logisticMidpointTime = 120f;

    public HashSet<IDamageable> spawnedEnemies {  get; private set; }

    // created in start
    Director slowDirector;
    Director fastDirector;

    Func<Director, float> slowGrowthFn;
    Func<Director, float> fastGrowthFn;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slowDirector = new Director(true, slowIncome, 0, slowMinPurchase);
        fastDirector = new Director(true, fastIncome, 0, fastMinPurchase);

        slowGrowthFn = ResolveGrowthFunction(slowGrowthType);
        fastGrowthFn = ResolveGrowthFunction(fastGrowthType);

        spawnedEnemies = new();
    }

    // Update is called once per frame
    void Update()
    {
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
                    // find suitable spawn location

                    // perform the spawn

                    // add the enemy instance to spawnedEnemies
                    // UnityEngine.Debug.Log("Spawning Enemy from Slow Director");
                }
            }
        }
    }

    void fastBehavior()
    {
        if (!fastDirector.active) return;
        Grow(ref fastDirector, fastGrowthFn);

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
                    // find suitable spawn location

                    // perform the spawn

                    // add the enemy instance to spawnedEnemies
                    // UnityEngine.Debug.Log("Spawning Enemy from Fast Director");
                }
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
}
