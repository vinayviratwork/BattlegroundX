using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum MatchPhase { Lobby, Countdown, InProgress, Ended }

/// <summary>
/// Authoritative match state machine: lobby → countdown → in-progress → ended.
/// Tracks alive players, elimination order, winner, and triggers return-to-lobby.
/// </summary>
public class NetworkMatchManager : NetworkBehaviour
{
    public static NetworkMatchManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float lobbyWaitTime   = 15f;
    [SerializeField] private float countdownTime   = 5f;
    [SerializeField] private float matchDuration   = 900f;
    [SerializeField] private float returnToLobbyDelay = 10f;
    [SerializeField] private int   minPlayersToStart  = 2;

    // ── Networked ─────────────────────────────────────────────────────────
    public NetworkVariable<MatchPhase> Phase = new(MatchPhase.Lobby,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> TimeRemaining = new(900f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int>   AliveCount = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<ulong> WinnerClientId = new(ulong.MaxValue,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool>  MatchEnded = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ── Private ───────────────────────────────────────────────────────────
    private readonly List<ulong> _eliminationOrder = new();
    private int _placementCounter = 0;

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        Phase.Value         = MatchPhase.Lobby;
        TimeRemaining.Value = matchDuration;
        MatchEnded.Value    = false;
        WinnerClientId.Value = ulong.MaxValue;

        NetworkManager.Singleton.OnClientConnectedCallback    += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback   += OnClientDisconnect;

        StartCoroutine(LobbyLoop());
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback  -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    // ── Lobby loop ────────────────────────────────────────────────────────
    private IEnumerator LobbyLoop()
    {
        float waited = 0f;
        while (Phase.Value == MatchPhase.Lobby)
        {
            waited += Time.deltaTime;
            int connected = NetworkManager.Singleton.ConnectedClientsList.Count;
            if (connected >= minPlayersToStart && waited >= lobbyWaitTime)
                break;
            yield return null;
        }
        StartCoroutine(CountdownLoop());
    }

    private IEnumerator CountdownLoop()
    {
        Phase.Value = MatchPhase.Countdown;
        float t = countdownTime;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            TimeRemaining.Value = t;
            yield return null;
        }
        StartMatch();
    }

    private void StartMatch()
    {
        Phase.Value         = MatchPhase.InProgress;
        TimeRemaining.Value = matchDuration;
        MatchEnded.Value    = false;
        _eliminationOrder.Clear();
        _placementCounter = NetworkManager.Singleton.ConnectedClientsList.Count;
        SpawnManager.Instance?.ResetSpawnPoints();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            SpawnManager.Instance?.TeleportToSpawn(client.ClientId);

        UpdateAliveCount();
    }

    // ── In-progress update ────────────────────────────────────────────────
    private void Update()
    {
        if (!IsServer || Phase.Value != MatchPhase.InProgress) return;

        TimeRemaining.Value = Mathf.Max(0f, TimeRemaining.Value - Time.deltaTime);
        UpdateAliveCount();

        if (AliveCount.Value <= 1 && NetworkManager.Singleton.ConnectedClientsList.Count > 1)
            EndMatch(GetLastAliveId());
        else if (TimeRemaining.Value <= 0f)
            EndMatch(GetLastAliveId());
    }

    private void UpdateAliveCount()
    {
        int count = 0;
        foreach (var c in NetworkManager.Singleton.ConnectedClientsList)
        {
            var hp = c.PlayerObject?.GetComponent<NetworkHealth>();
            if (hp != null && hp.IsAlive) count++;
        }
        AliveCount.Value = count;
    }

    private ulong GetLastAliveId()
    {
        foreach (var c in NetworkManager.Singleton.ConnectedClientsList)
        {
            var hp = c.PlayerObject?.GetComponent<NetworkHealth>();
            if (hp != null && hp.IsAlive) return c.ClientId;
        }
        return ulong.MaxValue;
    }

    // ── Elimination ───────────────────────────────────────────────────────
    public void RegisterEliminationOnServer(ulong eliminatedId, ulong killerId)
    {
        if (!IsServer) return;
        if (_eliminationOrder.Contains(eliminatedId)) return;

        _eliminationOrder.Add(eliminatedId);
        int place = _placementCounter--;

        // Assign placement stat
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(eliminatedId, out var client))
            client.PlayerObject?.GetComponent<PlayerStats>()?.SetPlacementOnServer(place);

        UpdateAliveCount();
        EliminationClientRpc(eliminatedId, killerId, place);
    }

    [ClientRpc]
    private void EliminationClientRpc(ulong eliminatedId, ulong killerId, int placement)
    {
        NetworkHUD.Instance?.ShowElimination(eliminatedId, killerId, placement);
    }

    // ── End match ─────────────────────────────────────────────────────────
    private void EndMatch(ulong winnerId)
    {
        if (MatchEnded.Value) return;

        WinnerClientId.Value = winnerId;
        MatchEnded.Value     = true;
        Phase.Value          = MatchPhase.Ended;

        // Assign winner placement
        if (winnerId != ulong.MaxValue &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(winnerId, out var winner))
            winner.PlayerObject?.GetComponent<PlayerStats>()?.SetPlacementOnServer(1);

        StartCoroutine(ReturnToLobby());
    }

    private IEnumerator ReturnToLobby()
    {
        yield return new WaitForSeconds(returnToLobbyDelay);
        Phase.Value         = MatchPhase.Lobby;
        MatchEnded.Value    = false;
        WinnerClientId.Value = ulong.MaxValue;
        TimeRemaining.Value  = matchDuration;
        _eliminationOrder.Clear();
        _placementCounter = 0;

        // Respawn all players
        foreach (var c in NetworkManager.Singleton.ConnectedClientsList)
        {
            var hp = c.PlayerObject?.GetComponent<NetworkHealth>();
            hp?.RespawnOnServer();
            SpawnManager.Instance?.TeleportToSpawn(c.ClientId);
        }

        ReturnToLobbyClientRpc();
        StartCoroutine(LobbyLoop());
    }

    [ClientRpc]
    private void ReturnToLobbyClientRpc()
    {
        NetworkHUD.Instance?.ShowLobbyScreen();
    }

    // ── Disconnect ────────────────────────────────────────────────────────
    private void OnClientConnected(ulong id)    => UpdateAliveCount();
    private void OnClientDisconnect(ulong id)   => UpdateAliveCount();
}
