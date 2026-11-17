# Contributing to Battleground X

Thanks for your interest. This document explains the project structure and how to work with the codebase.

---

## Project Structure

```
Assets/Scripts/
├── Audio/          AudioManager.cs
├── Combat/         NetworkHealth.cs · NetworkWeapon.cs
├── Data/           GameSettings.cs
├── Editor/         AnimatorControllerGenerator.cs · AudioSetup.cs
├── Networking/     RelaySessionUI.cs · LobbyManager.cs
├── Player/         NetworkPlayerController.cs · PlayerAnimatorController.cs
│                   PlayerInventory.cs · PlayerStats.cs
│                   SpectatorController.cs · CameraShake.cs
├── UI/             NetworkHUD.cs · MinimapController.cs · MinimapIcon.cs
│                   MinimapSafeZoneRing.cs · SettingsMenu.cs
│                   PingDisplay.cs · NameTag.cs
│                   MobileInputManager.cs · FireButton.cs · MobileJoystickBridge.cs
├── VFX/            VFXManager.cs · MuzzleFlash.cs · HitEffect.cs · BloodEffect.cs
└── World/          NetworkMatchManager.cs · NetworkSafeZone.cs
                    SpawnManager.cs · LootSpawner.cs · NetworkLootPickup.cs
```

---

## Coding Conventions

| Rule | Detail |
|---|---|
| No comments in source | All logic named clearly — self-documenting code |
| NetworkBehaviour base | All networked objects inherit NetworkBehaviour |
| IsServer guard | Server-only logic wrapped in `if (!IsServer) return;` |
| IsOwner guard | Owner-only input wrapped in `if (!IsOwner) return;` |
| NetworkVariable<T> | All replicated state via NetworkVariable |
| ServerRpc suffix | All server-bound RPCs end in `ServerRpc` |
| ClientRpc suffix | All client-bound RPCs end in `ClientRpc` |
| Singleton pattern | `public static T Instance` with Awake guard |

---

## Adding a New Feature

### New NetworkBehaviour script
```csharp
using Unity.Netcode;
using UnityEngine;

public class MyNewSystem : NetworkBehaviour
{
    private NetworkVariable<int> _myValue =
        new(0, NetworkVariableReadPermission.Everyone,
               NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        // server init here
    }

    [ServerRpc(RequireOwnership = true)]
    private void DoSomethingServerRpc(int value, ServerRpcParams rpc = default)
    {
        _myValue.Value = value;
        DoSomethingClientRpc(value);
    }

    [ClientRpc]
    private void DoSomethingClientRpc(int value)
    {
        // runs on all clients
    }
}
```

### Add to scene generator
In `BattlegroundXSceneGenerator.cs` → `BuildMatchObjects()`:
```csharp
var newSystemGo = new GameObject("MyNewSystem");
newSystemGo.AddComponent<NetworkObject>();
newSystemGo.AddComponent<MyNewSystem>();
```

---

## Adding to the Player Prefab

Add your component in `BattlegroundXSceneGenerator.CreatePlayerPrefab()`:
```csharp
go.AddComponent<MyPlayerComponent>();
```
Then re-run `Tools → Battleground X → Generate Full Scene`.

---

## Audio

Add new sounds in `AudioManager.cs`:
```csharp
[SerializeField] private AudioClip myNewClip;
public void PlayMySound() => Play(myNewClip, sfxVolume);
```
Then call `AudioManager.Instance.PlayMySound()` from anywhere.

---

## VFX

Use VFXManager for all particle effects:
```csharp
VFXManager.Instance.PlayHitEffect(position, normal);
VFXManager.Instance.PlayBloodEffect(position, normal);
VFXManager.Instance.PlayMuzzleFlash(position, rotation);
VFXManager.Instance.PlayExplosion(position);
VFXManager.Instance.FlashDamage();
VFXManager.Instance.FlashHeal();
```

---

## Git Workflow

```bash
git checkout -b feature/your-feature-name
# make changes
git add -A
git commit -m "feat: description of what you added"
git push origin feature/your-feature-name
# open pull request to main
```

### Commit message format
```
feat:     new feature
fix:      bug fix
refactor: code change without feature/fix
docs:     documentation only
style:    formatting only
```

---

## Running the Game

1. Open `BattlegroundX_Project/` in Unity 2022.3.62f1
2. `Tools → Battleground X → Generate Full Scene`
3. `Tools → Battleground X → Setup Audio (Procedural)`
4. Link Unity Cloud project: `Edit → Project Settings → Services`
5. Press Play → HOST → share join code → second instance → JOIN

---

## Known Limitations

- No unit tests (Unity Test Framework not configured — Phase 3 target)
- No dedicated server — Relay peer-hosted only
- Owner-authoritative movement — not anti-cheat ready
- No animation clips included — assign `.fbx` anims in Animator window
