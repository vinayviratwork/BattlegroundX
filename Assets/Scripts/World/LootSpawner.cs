using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-side loot spawner. Places NetworkLootPickup objects at designated spawn
/// points at match start and optionally re-spawns over time.
/// </summary>
public class LootSpawner : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private NetworkLootPickup medkitPrefab;
    [SerializeField] private NetworkLootPickup rifleAmmoPrefab;
    [SerializeField] private NetworkLootPickup shotgunAmmoPrefab;
    [SerializeField] private NetworkLootPickup sniperAmmoPrefab;

    [Header("Spawn Config")]
    [SerializeField] private Transform[] lootSpawnPoints;
    [SerializeField] private float spawnDelay    = 2f;
    [SerializeField] private float respawnChance = 0.3f;
    [SerializeField] private float respawnInterval = 60f;

    // Weight table: (prefab index, weight)
    private static readonly (int index, float weight)[] LootTable =
    {
        (0, 0.25f),  // medkit
        (1, 0.40f),  // rifle ammo
        (2, 0.20f),  // shotgun ammo
        (3, 0.15f),  // sniper ammo
    };

    private NetworkLootPickup[] _prefabs;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        _prefabs = new[] { medkitPrefab, rifleAmmoPrefab, shotgunAmmoPrefab, sniperAmmoPrefab };
        StartCoroutine(SpawnInitialLoot());
        StartCoroutine(RespawnLoop());
    }

    private IEnumerator SpawnInitialLoot()
    {
        yield return new WaitForSeconds(spawnDelay);

        foreach (var point in lootSpawnPoints)
        {
            if (point == null) continue;
            SpawnLootAt(point.position);
        }
    }

    private IEnumerator RespawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(respawnInterval);
            foreach (var point in lootSpawnPoints)
            {
                if (point == null) continue;
                if (Random.value < respawnChance)
                    SpawnLootAt(point.position);
            }
        }
    }

    private void SpawnLootAt(Vector3 position)
    {
        var prefab = SelectPrefab();
        if (prefab == null) return;

        var go = Instantiate(prefab, position + Vector3.up * 0.5f, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn(true);
    }

    private NetworkLootPickup SelectPrefab()
    {
        float total = 0f;
        foreach (var (_, w) in LootTable) total += w;
        float r = Random.value * total;
        float acc = 0f;
        foreach (var (index, weight) in LootTable)
        {
            acc += weight;
            if (r <= acc && _prefabs[index] != null)
                return _prefabs[index];
        }
        return _prefabs[1]; // fallback: rifle ammo
    }
}
