using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Tracks kills, damage dealt, and survival time. Server-authoritative.
/// </summary>
public class PlayerStats : NetworkBehaviour
{
    public NetworkVariable<int>   Kills        = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int>   DamageDealt  = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> SurvivalTime = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int>   Placement    = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float _startTime;
    private bool  _tracking;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _startTime = Time.time;
            _tracking  = true;
        }
    }

    private void Update()
    {
        if (!IsServer || !_tracking) return;

        var hp = GetComponent<NetworkHealth>();
        if (hp != null && hp.IsAlive)
            SurvivalTime.Value = Time.time - _startTime;
    }

    // Server-only mutations
    public void AddKillOnServer()
    {
        if (!IsServer) return;
        Kills.Value++;
    }

    public void AddDamageOnServer(int amount)
    {
        if (!IsServer || amount <= 0) return;
        DamageDealt.Value += amount;
    }

    public void SetPlacementOnServer(int place)
    {
        if (!IsServer) return;
        Placement.Value = place;
        _tracking       = false;
    }
}
