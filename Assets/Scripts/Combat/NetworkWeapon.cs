using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

// ── Weapon definition ─────────────────────────────────────────────────────────
[Serializable]
public class WeaponDefinition
{
    public string name           = "Assault Rifle";
    public int    damage         = 25;
    public int    magazineSize   = 30;
    public float  fireRate       = 0.1f;   // seconds between shots
    public float  reloadTime     = 2.5f;
    public float  range          = 150f;
    public float  recoilAmount   = 2f;
    public bool   isAutomatic    = true;
    public LootAmmoType ammoType = LootAmmoType.Rifle;
}

// ── Networked weapon ───────────────────────────────────────────────────────────
public class NetworkWeapon : NetworkBehaviour
{
    [Header("Weapon Slots")]
    [SerializeField] private WeaponDefinition[] weapons = new WeaponDefinition[]
    {
        new WeaponDefinition
        {
            name = "Assault Rifle", damage = 25, magazineSize = 30,
            fireRate = 0.1f, reloadTime = 2.5f, range = 150f, recoilAmount = 2f,
            isAutomatic = true, ammoType = LootAmmoType.Rifle
        },
        new WeaponDefinition
        {
            name = "Shotgun", damage = 60, magazineSize = 8,
            fireRate = 0.85f, reloadTime = 3f, range = 40f, recoilAmount = 6f,
            isAutomatic = false, ammoType = LootAmmoType.Shotgun
        },
        new WeaponDefinition
        {
            name = "Sniper", damage = 90, magazineSize = 5,
            fireRate = 1.5f, reloadTime = 3.5f, range = 500f, recoilAmount = 8f,
            isAutomatic = false, ammoType = LootAmmoType.Sniper
        }
    };

    [Header("Raycast")]
    [SerializeField] private LayerMask hitMask = ~0;

    // ── Networked state ───────────────────────────────────────────────────
    public NetworkVariable<int>  CurrentWeaponIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int>  AmmoInMag          = new(30, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> IsReloading        = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // ── Private ───────────────────────────────────────────────────────────
    private float _nextFireTime;
    private bool  _triggerHeld;
    private Camera _ownerCam;
    private PlayerInventory _inventory;
    private CameraShake _cameraShake;
    private AudioManager _audio;

    // ── Init ──────────────────────────────────────────────────────────────
    public override void OnNetworkSpawn()
    {
        _ownerCam    = GetComponentInChildren<Camera>(true);
        _inventory   = GetComponent<PlayerInventory>();
        _cameraShake = GetComponentInChildren<CameraShake>(true);
        _audio       = AudioManager.Instance;

        if (IsOwner)
            AmmoInMag.Value = CurrentWeapon.magazineSize;
    }

    private WeaponDefinition CurrentWeapon =>
        weapons[Mathf.Clamp(CurrentWeaponIndex.Value, 0, weapons.Length - 1)];

    // ── Update (owner only) ───────────────────────────────────────────────
    private void Update()
    {
        if (!IsOwner || !IsSpawned) return;

#if !UNITY_ANDROID || UNITY_EDITOR
        // Automatic fire
        if (CurrentWeapon.isAutomatic && Input.GetMouseButton(0)) TryFire();
        else if (!CurrentWeapon.isAutomatic && Input.GetMouseButtonDown(0)) TryFire();

        // Reload
        if (Input.GetKeyDown(KeyCode.R)) TryReload();

        // Weapon switch via scroll wheel or Q/E
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (scroll > 0.01f) SwitchWeapon(1);
        if (scroll < -0.01f) SwitchWeapon(-1);
        if (Input.GetKeyDown(KeyCode.E)) SwitchWeapon(1);
        if (Input.GetKeyDown(KeyCode.Q)) SwitchWeapon(-1);
#endif
    }

    // ── Fire ──────────────────────────────────────────────────────────────
    public void TryFire()
    {
        if (!IsOwner || !IsSpawned) return;
        if (IsReloading.Value) return;
        if (Time.time < _nextFireTime) return;
        if (AmmoInMag.Value <= 0) { TryReload(); return; }
        if (_ownerCam == null) _ownerCam = GetComponentInChildren<Camera>(true);
        if (_ownerCam == null) return;

        _nextFireTime = Time.time + CurrentWeapon.fireRate;
        AmmoInMag.Value--;

        _cameraShake?.Shake(CurrentWeapon.recoilAmount * 0.5f);
        _audio?.PlayShot();

        FireServerRpc(_ownerCam.transform.position, _ownerCam.transform.forward);
    }

    [ServerRpc]
    private void FireServerRpc(Vector3 origin, Vector3 direction, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
        // Validate direction magnitude
        if (direction.sqrMagnitude < 0.5f || direction.sqrMagnitude > 1.5f) return;

        direction = direction.normalized;
        if (Physics.Raycast(origin, direction, out RaycastHit hit, CurrentWeapon.range, hitMask,
            QueryTriggerInteraction.Ignore))
        {
            var target = hit.collider.GetComponentInParent<NetworkHealth>();
            if (target != null && target.OwnerClientId != OwnerClientId)
            {
                target.ApplyDamageOnServer(CurrentWeapon.damage, OwnerClientId);
                FireHitFeedbackClientRpc(hit.point, hit.normal);
            }
        }
    }

    [ClientRpc]
    private void FireHitFeedbackClientRpc(Vector3 point, Vector3 normal)
    {
        // Visual hit feedback — spawn particles here if assets exist
        Debug.DrawRay(point, normal * 0.3f, Color.red, 0.5f);
    }

    // ── Reload ────────────────────────────────────────────────────────────
    public void TryReload()
    {
        if (!IsOwner || !IsSpawned || IsReloading.Value) return;
        if (AmmoInMag.Value >= CurrentWeapon.magazineSize) return;

        int reserve = _inventory != null
            ? _inventory.GetAmmo(CurrentWeapon.ammoType)
            : 999;
        if (reserve <= 0) return;

        StartCoroutine(DoReload());
    }

    private IEnumerator DoReload()
    {
        IsReloading.Value = true;
        _audio?.PlayReload();
        yield return new WaitForSeconds(CurrentWeapon.reloadTime);

        int needed = CurrentWeapon.magazineSize - AmmoInMag.Value;
        if (_inventory != null)
        {
            int taken = _inventory.ConsumeAmmo(CurrentWeapon.ammoType, needed);
            AmmoInMag.Value += taken;
        }
        else
        {
            AmmoInMag.Value = CurrentWeapon.magazineSize;
        }

        IsReloading.Value = false;
    }

    // ── Weapon switch ─────────────────────────────────────────────────────
    public void SwitchWeapon(int direction)
    {
        if (!IsOwner || !IsSpawned || IsReloading.Value) return;
        int next = (CurrentWeaponIndex.Value + direction + weapons.Length) % weapons.Length;
        CurrentWeaponIndex.Value = next;
        AmmoInMag.Value = Mathf.Min(AmmoInMag.Value, weapons[next].magazineSize);
    }

    // ── Public API ────────────────────────────────────────────────────────
    public WeaponDefinition GetCurrentWeapon() => CurrentWeapon;
    public int              WeaponCount        => weapons.Length;
    public string           CurrentWeaponName  => CurrentWeapon.name;
}
