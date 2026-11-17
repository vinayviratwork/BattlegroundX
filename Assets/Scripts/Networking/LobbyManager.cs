using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    
    private readonly Dictionary<ulong, bool> _readyMap = new();

    public NetworkVariable<int> ReadyCount = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> TotalCount = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback  += OnConnect;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnect;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback  -= OnConnect;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnect;
        }
    }

    
    public void ToggleReady()
    {
        if (!IsClient) return;
        SetReadyServerRpc(!GetLocalReady());
    }

    private bool GetLocalReady()
    {
        ulong id = NetworkManager.Singleton.LocalClientId;
        return _readyMap.TryGetValue(id, out bool r) && r;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetReadyServerRpc(bool ready, ServerRpcParams p = default)
    {
        _readyMap[p.Receive.SenderClientId] = ready;
        RefreshCounts();
    }

    
    public void ForceStart()
    {
        if (!IsHost) return;
        
        foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
            _readyMap[id] = true;
        RefreshCounts();
    }

    
    private void RefreshCounts()
    {
        int ready = 0, total = 0;
        foreach (var kv in _readyMap)
        {
            total++;
            if (kv.Value) ready++;
        }
        ReadyCount.Value = ready;
        TotalCount.Value = total;
        UpdateReadyStatusClientRpc(ready, total);
    }

    [ClientRpc]
    private void UpdateReadyStatusClientRpc(int ready, int total)
    {
        NetworkHUD.Instance?.UpdateReadyStatus(ready, total);
    }

    private void OnConnect(ulong id)
    {
        _readyMap[id] = false;
        RefreshCounts();
    }

    private void OnDisconnect(ulong id)
    {
        _readyMap.Remove(id);
        RefreshCounts();
    }
}