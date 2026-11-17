using Unity.Netcode;
using UnityEngine;

public enum LootType  { Medkit, AmmoRifle, AmmoShotgun, AmmoSniper }

public class NetworkLootPickup : NetworkBehaviour
{
    [SerializeField] private LootType lootType     = LootType.AmmoRifle;
    [SerializeField] private int      amount       = 30;
    [SerializeField] private float    pickupRadius = 3f;

    
    [SerializeField] private float hoverAmplitude = 0.25f;
    [SerializeField] private float hoverSpeed     = 1.5f;
    [SerializeField] private float rotateSpeed    = 60f;

    private NetworkVariable<bool> _collected = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Vector3 _startPos;

    public override void OnNetworkSpawn()
    {
        _startPos = transform.position;
        _collected.OnValueChanged += (_, now) => { if (now) gameObject.SetActive(false); };
    }

    private void Update()
    {
        if (_collected.Value) return;
        
        float y = _startPos.y + Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    
    public void TryPickup()
    {
        if (!IsSpawned || _collected.Value) return;
        RequestPickupServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPickupServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer || _collected.Value) return;

        ulong senderId = rpcParams.Receive.SenderClientId;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(senderId, out var client)) return;
        if (client.PlayerObject == null) return;

        float dist = Vector3.Distance(client.PlayerObject.transform.position, transform.position);
        if (dist > pickupRadius) return;

        var inv    = client.PlayerObject.GetComponent<PlayerInventory>();
        var health = client.PlayerObject.GetComponent<NetworkHealth>();

        switch (lootType)
        {
            case LootType.Medkit:      health?.ApplyHealOnServer(amount);                           break;
            case LootType.AmmoRifle:   inv?.AddAmmoOnServer(LootAmmoType.Rifle,   amount);           break;
            case LootType.AmmoShotgun: inv?.AddAmmoOnServer(LootAmmoType.Shotgun, amount);           break;
            case LootType.AmmoSniper:  inv?.AddAmmoOnServer(LootAmmoType.Sniper,  amount);           break;
        }

        _collected.Value = true;
        GetComponent<NetworkObject>()?.Despawn();
    }

    
    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;
        var player = other.GetComponentInParent<NetworkPlayerController>();
        if (player != null && player.IsOwner) TryPickup();
    }
}