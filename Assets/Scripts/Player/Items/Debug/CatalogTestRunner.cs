using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class CatalogTestRunner : MonoBehaviour
{
    [SerializeField] ItemCatalog catalog;
    [SerializeField] ItemCatalog emptyCatalog;          // CAT-09: catalog asset that was never refreshed
    [SerializeField] string knownGoodId;                // CAT-01: any real item id
    [SerializeField] ItemDefinition nonDroppableItem;   // CAT-05: item with canDropRandomly unchecked
    [SerializeField] int sampleCount = 10_000;

    static readonly ItemRarityWeightDistribution Dist = ItemRarityWeightDistribution.Default;

    // test-only backdoor into the spawner's private duo chance; restored after every test
    static readonly FieldInfo DuoChanceField = typeof(ItemSpawner)
        .GetField("duoItemSpawnChance", BindingFlags.Instance | BindingFlags.NonPublic);

    static bool SpawnerReady()
    {
        if (ItemSpawner.Instance != null) return true;
        Debug.LogError("Catalog tests need play mode with an ItemSpawner in the scene (GetRandomItem reads ItemSpawner.Instance).");
        return false;
    }

    static void RunWithDuoChance(float chance, Action test)
    {
        float saved = (float)DuoChanceField.GetValue(ItemSpawner.Instance);
        DuoChanceField.SetValue(ItemSpawner.Instance, chance);
        try { test(); }
        finally { DuoChanceField.SetValue(ItemSpawner.Instance, saved); }
    }

    [ContextMenu("Run All Catalog Tests")]
    void RunAll()
    {
        if (!SpawnerReady()) return;
        TestIdLookup();       // CAT-01, CAT-02
        TestRaritySpread();   // CAT-03 (also detects CAT-08 config)
        TestItemWeights();    // CAT-04
        TestNonDroppable();   // CAT-05
        TestDuoChance();      // CAT-06, CAT-07
        TestEmptyCatalog();   // CAT-09
    }

    [ContextMenu("CAT-01/02: Id Lookup")]
    void TestIdLookup()
    {
        ItemDefinition item = catalog.GetItemFromId(knownGoodId);
        Debug.Log($"CAT-01 {(item != null ? "PASS" : "FAIL")}: '{knownGoodId}' -> '{(item != null ? item.DisplayName : "null")}'");

        try
        {
            catalog.GetItemFromId("__no_such_id__");
            Debug.LogError("CAT-02 FAIL: no exception for unknown id");
        }
        catch (ArgumentException e)
        {
            Debug.Log($"CAT-02 PASS: threw with message: {e.Message}");
        }
    }

    [ContextMenu("CAT-03: Rarity Spread")]
    void TestRaritySpread()
    {
        if (!SpawnerReady()) return;
        RunWithDuoChance(0f, () =>   // isolate the regular pool so the spread is clean
        {
            var counts = new Dictionary<ItemRarity, int>();
            foreach (ItemRarity r in Enum.GetValues(typeof(ItemRarity))) counts[r] = 0;
            int nulls = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                ItemDefinition item = catalog.GetRandomItem(Dist);
                if (item == null) { nulls++; continue; }
                counts[item.Rarity]++;
            }

            var log = new System.Text.StringBuilder($"CAT-03 over {sampleCount} rolls (nulls: {nulls}):\n");
            foreach (ItemRarity r in Enum.GetValues(typeof(ItemRarity)))
            {
                float expected = GetExpectedPercent(r);
                float actual = counts[r] * 100f / sampleCount;
                log.AppendLine($"  {r,-10} actual {actual,6:F2}%   expected {expected,6:F2}%   diff {actual - expected,5:F2}");
            }
            Debug.Log(log.ToString());
            if (nulls > 0) Debug.LogError($"CAT-03 FAIL: GetRandomItem returned null {nulls}x");
        });
    }

    [ContextMenu("CAT-04: Per-Item Weights")]
    void TestItemWeights()
    {
        if (!SpawnerReady()) return;
        RunWithDuoChance(0f, () =>
        {
            var counts = new Dictionary<ItemDefinition, int>();
            for (int i = 0; i < sampleCount; i++)
            {
                ItemDefinition item = catalog.GetRandomItem(Dist);
                if (item == null) continue;
                counts[item] = counts.GetValueOrDefault(item) + 1;
            }

            var log = new System.Text.StringBuilder("CAT-04 share within each rarity (actual vs weight-expected):\n");
            foreach (var rarityGroup in counts.Keys.GroupBy(i => i.Rarity).OrderBy(g => g.Key))
            {
                float totalWeight = rarityGroup.Sum(i => i.DropWeight);
                int totalRolls = rarityGroup.Sum(i => counts[i]);
                log.AppendLine($"  [{rarityGroup.Key}]");
                foreach (ItemDefinition item in rarityGroup.OrderByDescending(i => counts[i]))
                {
                    float expected = item.DropWeight / totalWeight * 100f;
                    float actual = counts[item] * 100f / totalRolls;
                    log.AppendLine($"    {item.DisplayName,-24} w={item.DropWeight,-4} actual {actual,6:F2}%   expected {expected,6:F2}%");
                }
            }
            Debug.Log(log.ToString());
        });
    }

    [ContextMenu("CAT-05: Non-Droppable Exclusion")]
    void TestNonDroppable()
    {
        if (nonDroppableItem == null) { Debug.LogWarning("CAT-05 SKIPPED: assign nonDroppableItem"); return; }
        if (!SpawnerReady()) return;

        // force the roll into whichever pool the item would live in if it were droppable
        RunWithDuoChance(nonDroppableItem.IsDuoItem ? 1f : 0f, () =>
        {
            int hits = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                if (catalog.GetRandomItem(Dist) == nonDroppableItem) hits++;
            }

            Debug.Log(hits == 0
                ? "CAT-05 (random) PASS: non-droppable item never rolled"
                : $"CAT-05 FAIL: non-droppable item rolled {hits}x — did you refresh the catalog after unchecking?");
        });

        ItemDefinition byId = catalog.GetItemFromId(nonDroppableItem.Id);
        Debug.Log($"CAT-05 (by id) {(byId == nonDroppableItem ? "PASS" : "FAIL")}: still reachable via GetItemFromId");
    }

    [ContextMenu("CAT-06/07: Duo Chance")]
    void TestDuoChance()
    {
        if (!SpawnerReady()) return;

        RunWithDuoChance(1f, () =>
        {
            int nonDuo = 0, nulls = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                ItemDefinition item = catalog.GetRandomItem(Dist);
                if (item == null) nulls++;
                else if (!item.IsDuoItem) nonDuo++;
            }
            Debug.Log($"CAT-06 {(nonDuo == 0 ? "PASS" : "FAIL")}: chance=1 -> {nonDuo} non-duo results ({nulls} nulls)");
        });

        RunWithDuoChance(0f, () =>
        {
            int duo = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                ItemDefinition item = catalog.GetRandomItem(Dist);
                if (item != null && item.IsDuoItem) duo++;
            }
            Debug.Log($"CAT-07 {(duo == 0 ? "PASS" : "FAIL")}: chance=0 -> {duo} duo results");
        });

        ItemDefinition forced = catalog.GetRandomDuoItem(Dist);
        Debug.Log($"CAT-07b {(forced != null && forced.IsDuoItem ? "PASS" : "FAIL")}: GetRandomDuoItem -> '{(forced != null ? forced.DisplayName : "null")}'");
    }

    [ContextMenu("CAT-09: Empty Catalog")]
    void TestEmptyCatalog()
    {
        if (emptyCatalog == null) { Debug.LogWarning("CAT-09 SKIPPED: assign an empty catalog asset"); return; }
        // safe even though it touches GetRandomItem: the definitions.Count == 0 early-out
        // returns before the spawner singleton is ever read
        ItemDefinition result = emptyCatalog.GetRandomItem(Dist);
        Debug.Log($"CAT-09 {(result == null ? "PASS" : "FAIL")}: empty catalog returned {(result == null ? "null, no throw" : result.DisplayName)}");
    }

    float GetExpectedPercent(ItemRarity rarity)
    {
        // mirrors GetRandomFrom: rarities with no droppable regular items get zeroed before normalizing
        float c = HasPool(ItemRarity.Common) ? Dist.Common : 0f;
        float u = HasPool(ItemRarity.Uncommon) ? Dist.Uncommon : 0f;
        float r = HasPool(ItemRarity.Rare) ? Dist.Rare : 0f;
        float l = HasPool(ItemRarity.Legendary) ? Dist.Legendary : 0f;
        float sum = c + u + r + l;
        float w = rarity switch { ItemRarity.Common => c, ItemRarity.Uncommon => u, ItemRarity.Rare => r, _ => l };
        return sum > 0 ? w / sum * 100f : 0f;
    }

    bool HasPool(ItemRarity rarity) =>
        catalog.AllItems.Any(i => i != null && i.CanDropRandomly && !i.IsDuoItem && i.Rarity == rarity);
}