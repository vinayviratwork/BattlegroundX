using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float minSpawnSeparation = 20f;

    private readonly List<int> _usedIndices = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        
        StartCoroutine(TeleportNextFrame(clientId));
    }

    private System.Collections.IEnumerator TeleportNextFrame(ulong clientId)
    {
        yield return null;
        TeleportToSpawn(clientId);
    }

    public void TeleportToSpawn(ulong clientId)
    {
        if (!IsServer) return;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
        if (client.PlayerObject == null) return;

        Vector3 pos = PickSpawnPosition(client.PlayerObject.transform.position);
        client.PlayerObject.transform.position = pos;

        var cc = client.PlayerObject.GetComponent<CharacterController>();
        if (cc != null) { cc.enabled = false; cc.enabled = true; }
    }

    private Vector3 PickSpawnPosition(Vector3 existingPos)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return Vector3.zero;

        
        List<int> candidates = new();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) continue;
            bool tooClose = false;
            foreach (int used in _usedIndices)
            {
                if (Vector3.Distance(spawnPoints[i].position, spawnPoints[used].position) < minSpawnSeparation)
                { tooClose = true; break; }
            }
            if (!tooClose) candidates.Add(i);
        }

        int chosen = candidates.Count > 0
            ? candidates[Random.Range(0, candidates.Count)]
            : Random.Range(0, spawnPoints.Length);

        _usedIndices.Add(chosen);
        return spawnPoints[chosen].position;
    }

    public void ResetSpawnPoints() => _usedIndices.Clear();
}