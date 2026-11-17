using Unity.Netcode;
using UnityEngine;

public class BloodEffect : NetworkBehaviour
{
    [Header("Blood settings")]
    [SerializeField] private float bloodOffsetUp   = 1.0f;  
    [SerializeField] private bool  showForOwner    = true;  

    
    private NetworkHealth _health;

    
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

    
    private void OnHealthChanged(int oldHp, int newHp)
    {
        if (newHp >= oldHp) return;      
        if (!IsServer) return;           

        Vector3 spawnPos = transform.position + Vector3.up * bloodOffsetUp;
        
        
        Vector3 normal   = (transform.position - Vector3.zero).normalized;
        SpawnBloodClientRpc(spawnPos, normal);
    }

    [ClientRpc]
    private void SpawnBloodClientRpc(Vector3 position, Vector3 normal)
    {
        
        if (IsOwner && !showForOwner) return;

        VFXManager.Instance?.PlayBloodEffect(position, normal);

        
        if (IsOwner)
            VFXManager.Instance?.FlashDamage();
    }

    
    public void TriggerBloodAtPoint(Vector3 worldPosition, Vector3 surfaceNormal)
    {
        if (!IsServer) return;
        SpawnBloodClientRpc(worldPosition, surfaceNormal);
    }
}