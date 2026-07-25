using UnityEngine;
using UnityEngine.InputSystem;

public class DebugItemSpawner : MonoBehaviour
{
    [SerializeField] Key spawnKey = Key.G;
    [SerializeField] Key spawnByIdKey = Key.H;
    [SerializeField] string manualSpawnId;              // for testing the GetItemFromId path
    [SerializeField] float upwardSpeed = 14f;           // needs some oomph vs your boosted gravity
    [SerializeField] float horizontalSpeed = 4f;

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;   // no keyboard connected (or running headless)

        if (keyboard[spawnKey].wasPressedThisFrame)
        {
            ItemDefinition def = ItemSpawner.Instance.ItemCatalog.GetRandomItem(ItemRarityWeightDistribution.Default);
            SpawnWithArc(def);
        }

        if (keyboard[spawnByIdKey].wasPressedThisFrame && !string.IsNullOrEmpty(manualSpawnId))
        {
            // catalog lookup by id -> manual spawn, canDropRandomly irrelevant
            ItemDefinition def = ItemSpawner.Instance.ItemCatalog.GetItemFromId(manualSpawnId);
            SpawnWithArc(def);
        }
    }

    void SpawnWithArc(ItemDefinition def)
    {
        if (def == null) { Debug.LogWarning("No item definition to spawn. If the ItemCatalog has no duo items, this may trigger."); return; }

        Vector3 origin = Player.Instance.transform.position + Vector3.up * 1.5f;

        Vector2 dir = Random.insideUnitCircle.normalized;
        Vector3 velocity = new Vector3(dir.x * horizontalSpeed, upwardSpeed, dir.y * horizontalSpeed);

        ItemSpawner.Instance.Spawn(def, origin, velocity);
        Debug.Log($"Spawned '{def.DisplayName}' ({def.Rarity})");
    }
}