using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum LootAmmoType { Rifle, Pistol, Shotgun, Sniper }

/// <summary>
/// Per-player inventory: ammo counts + healing items. Server-authoritative writes.
/// </summary>
public class PlayerInventory : NetworkBehaviour
{
    [Header("Max reserves")]
    [SerializeField] private int maxRifleAmmo   = 180;
    [SerializeField] private int maxPistolAmmo  = 90;
    [SerializeField] private int maxShotgunAmmo = 56;
    [SerializeField] private int maxSniperAmmo  = 20;
    [SerializeField] private int maxMedkits     = 5;

    // ── Networked ─────────────────────────────────────────────────────────
    public NetworkVariable<int> RifleAmmo   = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> PistolAmmo  = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> ShotgunAmmo = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> SniperAmmo  = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> Medkits     = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ─────────────────────────────────────────────────────────────────────
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Start with minimal ammo for first weapon
            RifleAmmo.Value = 60;
        }
    }

    // ── Server-side mutations ─────────────────────────────────────────────
    public void AddAmmoOnServer(LootAmmoType type, int amount)
    {
        if (!IsServer || amount <= 0) return;
        switch (type)
        {
            case LootAmmoType.Rifle:   RifleAmmo.Value   = Mathf.Min(maxRifleAmmo,   RifleAmmo.Value   + amount); break;
            case LootAmmoType.Pistol:  PistolAmmo.Value  = Mathf.Min(maxPistolAmmo,  PistolAmmo.Value  + amount); break;
            case LootAmmoType.Shotgun: ShotgunAmmo.Value = Mathf.Min(maxShotgunAmmo, ShotgunAmmo.Value + amount); break;
            case LootAmmoType.Sniper:  SniperAmmo.Value  = Mathf.Min(maxSniperAmmo,  SniperAmmo.Value  + amount); break;
        }
    }

    public void AddMedkitOnServer(int amount)
    {
        if (!IsServer || amount <= 0) return;
        Medkits.Value = Mathf.Min(maxMedkits, Medkits.Value + amount);
    }

    // ── Owner-side read + consume (called from NetworkWeapon) ─────────────
    public int GetAmmo(LootAmmoType type)
    {
        return type switch
        {
            LootAmmoType.Rifle   => RifleAmmo.Value,
            LootAmmoType.Pistol  => PistolAmmo.Value,
            LootAmmoType.Shotgun => ShotgunAmmo.Value,
            LootAmmoType.Sniper  => SniperAmmo.Value,
            _                   => 0
        };
    }

    // Owner requests server to consume ammo before reload finalises
    public int ConsumeAmmo(LootAmmoType type, int amount)
    {
        if (!IsOwner) return 0;
        ConsumeAmmoServerRpc(type, amount);
        // Optimistic: return requested; server corrects if wrong
        return Mathf.Min(GetAmmo(type), amount);
    }

    [ServerRpc]
    private void ConsumeAmmoServerRpc(LootAmmoType type, int amount, ServerRpcParams p = default)
    {
        if (p.Receive.SenderClientId != OwnerClientId) return;
        int clamped = Mathf.Min(GetAmmo(type), amount);
        switch (type)
        {
            case LootAmmoType.Rifle:   RifleAmmo.Value   -= clamped; break;
            case LootAmmoType.Pistol:  PistolAmmo.Value  -= clamped; break;
            case LootAmmoType.Shotgun: ShotgunAmmo.Value -= clamped; break;
            case LootAmmoType.Sniper:  SniperAmmo.Value  -= clamped; break;
        }
    }

    // Owner requests medkit use
    public void UseMedkit()
    {
        if (!IsOwner || Medkits.Value <= 0) return;
        UseMedkitServerRpc();
    }

    [ServerRpc]
    private void UseMedkitServerRpc(ServerRpcParams p = default)
    {
        if (p.Receive.SenderClientId != OwnerClientId) return;
        if (Medkits.Value <= 0) return;
        Medkits.Value--;
        GetComponent<NetworkHealth>()?.ApplyHealOnServer(40);
    }
}
