using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Attached to NetworkPlayer (or any damageable).
/// Hooks into NetworkHealth events and fires blood VFX on all clients
/// via a ClientRpc issued from NetworkHealth's HitFeedbackClientRpc.
///
/// Add this component alongside NetworkHealth on the player prefab.
/// No prefabs required — VFXManager procedural particles handle the visuals.
/// </summary>
public class BloodEffect : NetworkBehaviour
{
    [Header("Blood settings")]
    [SerializeField] private float bloodOffsetUp   = 1.0f;  // spawn height offset
    [SerializeField] private bool  showForOwner    = true;  // show blood on own player

    // Cached
    private NetworkHealth _health;

    // ─────────────────────────────────────────────────────────────────────
    public override void OnNetworkSpawn()
    {
        _health = GetComponent<NetworkHealth>();
        if (_health != null)
            _health.OnHealthChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (_health != null)
            _health.OnHealthChanged -= OnHealthChanged;
    }

    // ── Health change → trigger blood on clients ──────────────────────────
    private void OnHealthChanged(int oldHp, int newHp)
    {
        if (newHp >= oldHp) return;      // healing, not damage
        if (!IsServer) return;           // server dispatches the RPC

        Vector3 spawnPos = transform.position + Vector3.up * bloodOffsetUp;
        // Approximate: use camera-facing normal (server doesn't know exact hit direction;
        // direction from attacker would require extra data — normal approximation is fine here)
        Vector3 normal   = (transform.position - Vector3.zero).normalized;
        SpawnBloodClientRpc(spawnPos, normal);
    }

    [ClientRpc]
    private void SpawnBloodClientRpc(Vector3 position, Vector3 normal)
    {
        // Skip on own player if disabled
        if (IsOwner && !showForOwner) return;

        VFXManager.Instance?.PlayBloodEffect(position, normal);

        // Flash the screen red if it's us taking damage
        if (IsOwner)
            VFXManager.Instance?.FlashDamage();
    }

    // ── Public API: direct call from server if position/normal are known ─
    public void TriggerBloodAtPoint(Vector3 worldPosition, Vector3 surfaceNormal)
    {
        if (!IsServer) return;
        SpawnBloodClientRpc(worldPosition, surfaceNormal);
    }
}
