using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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

    
    private static readonly (int index, float weight)[] LootTable =
    {
        (0, 0.25f),  
        (1, 0.40f),  
        (2, 0.20f),  
        (3, 0.15f),  
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
        return _prefabs[1]; 
    }
}