using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-authoritative health with death events, hit feedback, and stat integration.
/// </summary>
public class NetworkHealth : NetworkBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 100;

    // ── Networked ─────────────────────────────────────────────────────────
    public NetworkVariable<int>  CurrentHealth = new(100,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsDead = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ── Events (local) ───────────────────────────────────────────────────
    public event Action<int, int>  OnHealthChanged; // (old, new)
    public event Action<ulong>     OnDied;          // (killerClientId)

    public bool IsAlive => !IsDead.Value;

    // ─────────────────────────────────────────────────────────────────────
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentHealth.Value = maxHealth;
            IsDead.Value        = false;
        }
        CurrentHealth.OnValueChanged += HandleHealthChanged;
        IsDead.OnValueChanged        += HandleDeathChanged;
    }

    public override void OnNetworkDespawn()
    {
        CurrentHealth.OnValueChanged -= HandleHealthChanged;
        IsDead.OnValueChanged        -= HandleDeathChanged;
    }

    // ── Server calls ──────────────────────────────────────────────────────
    public void ApplyDamageOnServer(int amount, ulong attackerClientId = ulong.MaxValue)
    {
        if (!IsServer || amount <= 0 || !IsAlive) return;
        int oldHp = CurrentHealth.Value;
        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - amount);

        if (CurrentHealth.Value == 0 && !IsDead.Value)
        {
            IsDead.Value = true;
            HandleDeathOnServer(attackerClientId);
        }

        HitFeedbackClientRpc(amount);
    }

    public void ApplyHealOnServer(int amount)
    {
        if (!IsServer || amount <= 0 || !IsAlive) return;
        CurrentHealth.Value = Mathf.Min(maxHealth, CurrentHealth.Value + amount);
    }

    public void RespawnOnServer()
    {
        if (!IsServer) return;
        CurrentHealth.Value = maxHealth;
        IsDead.Value        = false;
    }

    // ── Death flow ────────────────────────────────────────────────────────
    private void HandleDeathOnServer(ulong killerId)
    {
        // Credit kill to attacker
        if (killerId != ulong.MaxValue &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(killerId, out var attacker))
        {
            attacker.PlayerObject?.GetComponent<PlayerStats>()?.AddKillOnServer();
        }

        // Notify attacker and victim
        NotifyDeathClientRpc(killerId);

        // Let MatchManager handle elimination
        NetworkMatchManager.Instance?.RegisterEliminationOnServer(OwnerClientId, killerId);
    }

    // ── ClientRpc ─────────────────────────────────────────────────────────
    [ClientRpc]
    private void HitFeedbackClientRpc(int damage)
    {
        if (IsOwner)
        {
            AudioManager.Instance?.PlayHit();
            // Screen flash could be triggered here
        }
    }

    [ClientRpc]
    private void NotifyDeathClientRpc(ulong killerId)
    {
        OnDied?.Invoke(killerId);
        if (IsOwner)
        {
            // Trigger spectator mode on local client
            GetComponent<SpectatorController>()?.EnterSpectate();
        }
    }

    // ── Local change callbacks ────────────────────────────────────────────
    private void HandleHealthChanged(int oldVal, int newVal)
    {
        OnHealthChanged?.Invoke(oldVal, newVal);
    }

    private void HandleDeathChanged(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            // Disable physics/rendering for dead player
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
                if (r != null) r.enabled = IsOwner; // keep visible for spectator
        }
    }

    // ── Getters ───────────────────────────────────────────────────────────
    public int   MaxHealth    => maxHealth;
    public float HealthRatio  => (float)CurrentHealth.Value / maxHealth;
}
