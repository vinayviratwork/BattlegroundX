using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class RelaySessionUI : MonoBehaviour
{
    public static RelaySessionUI Instance { get; private set; }

    [Header("Input / Output")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text       joinCodeText;
    [SerializeField] private TMP_Text       statusText;
    [SerializeField] private TMP_Text       playerListText;

    [Header("Panels")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TMP_Text   countdownText;

    [Header("Config")]
    [SerializeField, Min(1)] private int maxConnections = 15;

    private bool _servicesReady;

    
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private async void Start()
    {
        await InitializeServices();
        StartCoroutine(PlayerListRefresh());
    }

    
    private async Task InitializeServices()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            _servicesReady = true;
            SetStatus("Ready — Host a match or enter a join code.");
        }
        catch (Exception e)
        {
            SetStatus("Services init failed: " + e.Message);
            Debug.LogException(e);
        }
    }

    
    public async void HostGame()
    {
        if (!await EnsureReady()) return;
        try
        {
            SetStatus("Creating Relay allocation…");
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string code      = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(alloc, "dtls"));

            if (NetworkManager.Singleton.StartHost())
            {
                if (joinCodeText != null) joinCodeText.text = "JOIN CODE: " + code;
                SetStatus("Hosting. Share the join code above.");
            }
            else SetStatus("NetworkManager could not start host.");
        }
        catch (Exception e)
        {
            SetStatus("Host failed: " + e.Message);
            Debug.LogException(e);
        }
    }

    
    public async void JoinGame()
    {
        if (!await EnsureReady()) return;
        string code = joinCodeInput != null ? joinCodeInput.text.Trim().ToUpperInvariant() : "";
        if (string.IsNullOrWhiteSpace(code)) { SetStatus("Enter a join code."); return; }
        try
        {
            SetStatus("Joining Relay…");
            JoinAllocation alloc = await RelayService.Instance.JoinAllocationAsync(code);
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(alloc, "dtls"));

            if (NetworkManager.Singleton.StartClient()) SetStatus("Connecting to host…");
            else SetStatus("NetworkManager could not start client.");
        }
        catch (Exception e)
        {
            SetStatus("Join failed: " + e.Message);
            Debug.LogException(e);
        }
    }

    
    public void LeaveGame()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
        if (joinCodeText != null) joinCodeText.text = "";
        SetStatus("Disconnected.");
    }

    
    public void ShowCountdown(float seconds)
    {
        if (lobbyPanel != null)      lobbyPanel.SetActive(false);
        if (countdownPanel != null)  countdownPanel.SetActive(true);
        if (countdownText != null)   countdownText.text = Mathf.CeilToInt(seconds).ToString();
    }

    public void HideCountdown()
    {
        if (countdownPanel != null)  countdownPanel.SetActive(false);
    }

    
    private IEnumerator PlayerListRefresh()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            if (playerListText == null) continue;
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsListening)
            {
                playerListText.text = "";
                continue;
            }
            var sb = new System.Text.StringBuilder("Players online:\n");
            int i = 1;
            foreach (var id in nm.ConnectedClientsIds)
                sb.AppendLine($"  {i++}. Client {id}{(id == nm.LocalClientId ? " (you)" : "")}");
            playerListText.text = sb.ToString();
        }
    }

    
    private async Task<bool> EnsureReady()
    {
        if (!_servicesReady) await InitializeServices();
        if (!_servicesReady) return false;
        if (NetworkManager.Singleton == null)
        { SetStatus("Add NetworkManager + UnityTransport to the scene."); return false; }
        if (NetworkManager.Singleton.IsListening)
        { SetStatus("Already in a session. Leave first."); return false; }
        return true;
    }

    private void SetStatus(string msg)
    {
        Debug.Log("[Relay] " + msg);
        if (statusText != null) statusText.text = msg;
    }
}