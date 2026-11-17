using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkHUD : MonoBehaviour
{
    public static NetworkHUD Instance { get; private set; }

    
    [Header("Health")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    
    [Header("Ammo")]
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private TMP_Text reloadingText;

    
    [Header("Match Info")]
    [SerializeField] private TMP_Text playersText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text phaseText;

    
    [Header("Zone")]
    [SerializeField] private TMP_Text  zoneText;
    [SerializeField] private Image     zoneWarningImage;
    [SerializeField] private Color     safeColor   = Color.green;
    [SerializeField] private Color     dangerColor = Color.red;

    
    [Header("Kill Feed")]
    [SerializeField] private Transform killFeedContainer;
    [SerializeField] private TMP_Text  killFeedEntryPrefab;
    [SerializeField] private int       maxKillFeedEntries = 5;
    [SerializeField] private float     killFeedDuration   = 4f;
    private readonly Queue<TMP_Text>   _killFeedQueue = new();

    
    [Header("Match End")]
    [SerializeField] private GameObject matchEndPanel;
    [SerializeField] private TMP_Text   matchResultText;
    [SerializeField] private TMP_Text   statsText;
    [SerializeField] private Button     returnToLobbyButton;

    
    [Header("Lobby")]
    [SerializeField] private GameObject lobbyStatusPanel;
    [SerializeField] private TMP_Text   readyCountText;
    [SerializeField] private Button     readyButton;

    
    [Header("Minimap")]
    [SerializeField] private RawImage minimapImage;

    
    private NetworkHealth      _localHealth;
    private NetworkWeapon      _localWeapon;
    private PlayerInventory    _localInventory;
    private NetworkSafeZone    _zone;

    
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (matchEndPanel   != null) matchEndPanel.SetActive(false);
        if (lobbyStatusPanel!= null) lobbyStatusPanel.SetActive(true);
        if (reloadingText   != null) reloadingText.gameObject.SetActive(false);

        if (returnToLobbyButton != null)
            returnToLobbyButton.onClick.AddListener(OnReturnToLobbyPressed);
        if (readyButton != null)
            readyButton.onClick.AddListener(() => LobbyManager.Instance?.ToggleReady());
    }

    private void Update()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsClient) return;

        FindLocalRefs();
        RefreshHealth();
        RefreshAmmo();
        RefreshMatchInfo();
        RefreshZone();
    }

    
    private void FindLocalRefs()
    {
        if (_localHealth != null) return;
        foreach (var hp in FindObjectsOfType<NetworkHealth>())
        {
            if (!hp.IsOwner) continue;
            _localHealth    = hp;
            _localWeapon    = hp.GetComponent<NetworkWeapon>();
            _localInventory = hp.GetComponent<PlayerInventory>();
            _zone           = FindObjectOfType<NetworkSafeZone>();
            break;
        }
    }

    
    private void RefreshHealth()
    {
        if (_localHealth == null) return;
        int hp = _localHealth.CurrentHealth.Value;
        if (healthText   != null) healthText.text = $"HP: {hp}";
        if (healthSlider != null) healthSlider.value = _localHealth.HealthRatio;
    }

    
    private void RefreshAmmo()
    {
        if (_localWeapon == null) return;
        int mag     = _localWeapon.AmmoInMag.Value;
        bool reload = _localWeapon.IsReloading.Value;
        int reserve = _localInventory != null
            ? _localInventory.GetAmmo(_localWeapon.GetCurrentWeapon().ammoType) : 0;

        if (ammoText      != null) ammoText.text      = $"{mag} / {reserve}";
        if (weaponNameText!= null) weaponNameText.text = _localWeapon.CurrentWeaponName;
        if (reloadingText != null) reloadingText.gameObject.SetActive(reload);
    }

    
    private void RefreshMatchInfo()
    {
        var match = NetworkMatchManager.Instance;
        var nm    = NetworkManager.Singleton;
        if (nm    != null && playersText != null)
            playersText.text = $"⬟ {(match != null ? match.AliveCount.Value : nm.ConnectedClientsIds.Count)}";
        if (match != null)
        {
            float t = match.TimeRemaining.Value;
            if (timerText != null) timerText.text = $"{Mathf.FloorToInt(t/60):00}:{Mathf.FloorToInt(t%60):00}";
            if (phaseText != null) phaseText.text = match.Phase.Value.ToString();

            if (match.MatchEnded.Value) ShowMatchEnd(match.WinnerClientId.Value);
        }
    }

    
    private void RefreshZone()
    {
        if (_zone == null || _localHealth == null) return;
        bool inside = _zone.IsInsideZone(_localHealth.transform.position);
        if (zoneText         != null) zoneText.text           = inside ? "SAFE" : "DANGER — MOVE!";
        if (zoneWarningImage != null) zoneWarningImage.color  = inside ? safeColor : dangerColor;
    }

    
    public void ShowElimination(ulong eliminatedId, ulong killerId, int placement)
    {
        string msg = killerId == ulong.MaxValue
            ? $"Client {eliminatedId} eliminated (#{placement})"
            : $"Client {killerId} eliminated Client {eliminatedId} (#{placement})";

        AddKillFeedEntry(msg);
    }

    private void AddKillFeedEntry(string msg)
    {
        if (killFeedContainer == null || killFeedEntryPrefab == null) return;

        if (_killFeedQueue.Count >= maxKillFeedEntries)
        {
            var old = _killFeedQueue.Dequeue();
            if (old != null) Destroy(old.gameObject);
        }

        var entry = Instantiate(killFeedEntryPrefab, killFeedContainer);
        entry.text = msg;
        _killFeedQueue.Enqueue(entry);
        StartCoroutine(RemoveKillFeedEntry(entry, killFeedDuration));
    }

    private IEnumerator RemoveKillFeedEntry(TMP_Text entry, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (entry != null)
        {
            _killFeedQueue.TryDequeue(out _);
            Destroy(entry.gameObject);
        }
    }

    
    private void ShowMatchEnd(ulong winnerId)
    {
        if (matchEndPanel == null || matchEndPanel.activeSelf) return;
        matchEndPanel.SetActive(true);

        bool isWinner = winnerId == NetworkManager.Singleton?.LocalClientId;
        if (matchResultText != null)
            matchResultText.text = isWinner ? "🏆 WINNER WINNER!" :
                                   winnerId == ulong.MaxValue ? "MATCH OVER" :
                                   $"Winner: Client {winnerId}";

        if (statsText != null && _localHealth != null)
        {
            var stats = _localHealth.GetComponent<PlayerStats>();
            if (stats != null)
                statsText.text = $"Kills: {stats.Kills.Value}\n" +
                                 $"Damage: {stats.DamageDealt.Value}\n" +
                                 $"Survived: {Mathf.FloorToInt(stats.SurvivalTime.Value)}s\n" +
                                 $"Placement: #{stats.Placement.Value}";
        }
    }

    private void OnReturnToLobbyPressed()
    {
        if (matchEndPanel != null) matchEndPanel.SetActive(false);
    }

    public void ShowLobbyScreen()
    {
        if (matchEndPanel    != null) matchEndPanel.SetActive(false);
        if (lobbyStatusPanel != null) lobbyStatusPanel.SetActive(true);
    }

    public void UpdateReadyStatus(int ready, int total)
    {
        if (readyCountText != null) readyCountText.text = $"Ready: {ready}/{total}";
    }
}