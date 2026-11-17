using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkHealth : NetworkBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 100;

    
    public NetworkVariable<int>  CurrentHealth = new(100,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsDead = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    
    public event Action<int, int>  OnHealthChanged; 
    public event Action<ulong>     OnDied;          

    public bool IsAlive => !IsDead.Value;

    
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

    
    private void HandleDeathOnServer(ulong killerId)
    {
        
        if (killerId != ulong.MaxValue &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(killerId, out var attacker))
        {
            attacker.PlayerObject?.GetComponent<PlayerStats>()?.AddKillOnServer();
        }

        
        NotifyDeathClientRpc(killerId);

        
        NetworkMatchManager.Instance?.RegisterEliminationOnServer(OwnerClientId, killerId);
    }

    
    [ClientRpc]
    private void HitFeedbackClientRpc(int damage)
    {
        if (IsOwner)
        {
            AudioManager.Instance?.PlayHit();
            
        }
    }

    [ClientRpc]
    private void NotifyDeathClientRpc(ulong killerId)
    {
        OnDied?.Invoke(killerId);
        if (IsOwner)
        {
            
            GetComponent<SpectatorController>()?.EnterSpectate();
        }
    }

    
    private void HandleHealthChanged(int oldVal, int newVal)
    {
        OnHealthChanged?.Invoke(oldVal, newVal);
    }

    private void HandleDeathChanged(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
                if (r != null) r.enabled = IsOwner; 
        }
    }

    
    public int   MaxHealth    => maxHealth;
    public float HealthRatio  => (float)CurrentHealth.Value / maxHealth;
}