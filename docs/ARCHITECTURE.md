# BATTLEGROUND X — ARCHITECTURE DOCUMENT

> Technical deep-dive for engineers reviewing the codebase.

---

## 1. Technology Stack

| Layer | Technology | Version |
|---|---|---|
| Engine | Unity | 2022.3.62f1 LTS |
| Networking | Unity Netcode for GameObjects (NGO) | 1.8.1 |
| Transport | Unity Transport Package (UTP) | 2.2.1 |
| Relay | Unity Gaming Services — Relay | 1.0.5 |
| Auth | Unity Authentication (anonymous) | 3.3.3 |
| Rendering | Universal Render Pipeline (URP) | 14.0.11 |
| UI | TextMeshPro + Unity UI | 3.0.6 |
| Input | Unity Input System | 1.7.0 |
| Camera | Cinemachine | 2.9.7 |
| Platform | Android (API 23+) + Windows x64 | — |

---

## 2. High-Level Architecture

```
┌─────────────────────────────────────────────────────┐
│                   CLIENT (Player)                   │
│                                                     │
│  Input Layer        Logic Layer      Network Layer  │
│  ┌──────────┐      ┌──────────┐     ┌───────────┐  │
│  │ Keyboard │─────▶│ Player   │────▶│ ServerRpc │  │
│  │ Mobile   │      │ Controller│     │ ClientRpc │  │
│  │ Joystick │      └──────────┘     └───────────┘  │
│  └──────────┘            │                │        │
│                   ┌──────▼──────┐         │        │
│                   │  Animator   │         │        │
│                   │  VFX        │         │        │
│                   │  Audio      │         │        │
│                   └─────────────┘         │        │
└───────────────────────────────────────────┼────────┘
                                            │ UTP/DTLS
┌───────────────────────────────────────────▼────────┐
│              UNITY RELAY (Cloud)                   │
│         Routes packets between clients             │
│         No direct IP exposure                      │
└───────────────────────────────────────────┬────────┘
                                            │
┌───────────────────────────────────────────▼────────┐
│                   HOST CLIENT                      │
│                                                    │
│  NetworkManager (singleton)                        │
│  ├── NetworkMatchManager  (match state)            │
│  ├── NetworkSafeZone      (zone shrink)            │
│  ├── SpawnManager         (player spawning)        │
│  ├── LootSpawner          (loot lifecycle)         │
│  └── LobbyManager        (ready system)            │
└────────────────────────────────────────────────────┘
```

---

## 3. Networking Model

### Authority Model
```
Owner-Authoritative Movement (Prototype)
  └── Player moves locally → OwnerNetworkTransform syncs position to all clients
  └── Trade-off: low latency feel vs. server-authority (production upgrade in Phase 4)

Server-Authoritative Combat
  └── FireServerRpc(direction, weaponIndex)
        └── Server validates: distance, direction magnitude, ammo count
        └── Server applies damage via NetworkHealth
        └── FireHitFeedbackClientRpc → all clients show VFX
```

### NetworkVariable Usage
```csharp
// All replicated state uses NetworkVariable<T>
NetworkVariable<int>   CurrentHealth    (Server write, Everyone read)
NetworkVariable<int>   CurrentWeaponIndex
NetworkVariable<bool>  IsReloading
NetworkVariable<bool>  IsCrouching
NetworkVariable<bool>  IsAiming
NetworkVariable<bool>  IsRunning
NetworkVariable<bool>  IsDead
NetworkVariable<MatchPhase> CurrentPhase
NetworkVariable<float> SafeZoneRadius
```

### RPC Flow — Fire Example
```
[Client — Owner]
  FireButton pressed
  → NetworkWeapon.TryFire()
  → FireServerRpc(direction, weaponIndex, clientTick)

[Server]
  → Validates sender owns the weapon object
  → Validates ammo > 0
  → Raycast on server physics
  → If hit: NetworkHealth.TakeDamage(amount, attackerId)
  → FireHitFeedbackClientRpc(hitPoint, hitNormal, didHit)

[All Clients]
  → VFXManager.PlayHitEffect(hitPoint, hitNormal)
  → If didHit: VFXManager.PlayBloodEffect()
  → AudioManager.PlayShot()
```

---

## 4. Match State Machine

```
                    ┌──────────┐
         ┌─────────▶│  LOBBY   │◀──────────────┐
         │          └────┬─────┘               │
         │               │ All players ready    │
         │               ▼                     │
         │          ┌──────────┐               │
         │          │COUNTDOWN │ (5 seconds)    │
         │          └────┬─────┘               │
         │               │ Timer ends           │
         │               ▼                     │
         │          ┌──────────┐               │
         │          │IN_PROGRESS│              │
         │          └────┬─────┘               │
         │               │ 1 player remains     │
         │               │ OR timer = 0         │
         │               ▼                     │
         │          ┌──────────┐               │
         └──────────│  ENDED   │───────────────┘
    (after delay)   └──────────┘  (auto-return)
```

**Implementation:** `NetworkMatchManager.cs`
- State stored in `NetworkVariable<MatchPhase>`
- Only server transitions state
- All clients react via `OnValueChanged` callback

---

## 5. Safe Zone System

```
Phase 1: radius=160  damage=1/s   (full map)
Phase 2: radius=120  damage=2/s
Phase 3: radius=80   damage=3/s
Phase 4: radius=55   damage=5/s
Phase 5: radius=35   damage=8/s
Phase 6: radius=20   damage=12/s
Phase 7: radius=8    damage=20/s  (endgame)
```

- Zone centre stored as `NetworkVariable<Vector3>` — synced to all clients
- Damage applied server-side every 0.5s to players outside radius
- Visual ring scales in real-time on all clients
- Minimap ring tracks `NetworkSafeZone.CurrentRadius` live

---

## 6. VFX Architecture — Object Pooling

```
VFXManager (singleton, scene lifetime)
├── Queue<ParticleSystem> _muzzlePool    (8 slots)
├── Queue<ParticleSystem> _hitPool       (16 slots)
├── Queue<ParticleSystem> _bloodPool     (16 slots)
└── Queue<ParticleSystem> _explosionPool (4 slots)

Play() → Dequeue → Position → Play → StartCoroutine(ReturnAfterDelay)
       → ReturnAfterDelay → Stop → SetActive(false) → Enqueue
```

**Why pooling:** Instantiate/Destroy every bullet hit causes GC spikes.
Pool eliminates allocations during gameplay — critical for 60fps on Android.

---

## 7. Minimap System

```
MinimapController
├── Orthographic Camera (height=120, size=90 units)
│   └── Renders only "UI" layer
│   └── RenderTexture (256×256) → HUD RawImage
├── Follows local player (LateUpdate)
└── Optional: rotates with player yaw

MinimapIcon (per NetworkPlayer)
├── World-space Quad above player head
├── Layer = "UI" → visible only to minimap camera
├── Green = local owner, Red = enemy
└── Direction arrow quad (child)

MinimapSafeZoneRing
└── Cylinder scaled to NetworkSafeZone.CurrentRadius each LateUpdate
```

---

## 8. Animator State Machine

```
Parameters:
  Speed (float)     0=idle, 1=walk, 2=run, 3=sprint
  IsCrouching (bool)
  IsAiming    (bool)
  IsGrounded  (bool)
  IsDead      (bool)
  WeaponIndex (int)  0=AR, 1=Shotgun, 2=Sniper
  Fire        (trigger)
  Reload      (trigger)
  Jump        (trigger)
  TakeDamage  (trigger)

Data source: PlayerAnimatorController reads NetworkVariables
  → Works for local player AND all remote players
  → No extra RPCs needed — piggybacks existing replicated state
```

---

## 9. Mobile Input Architecture

```
MobileInputManager
├── Left virtual joystick  → SetMobileMove(Vector2)
├── Right look pad         → SetMobileLookDelta(Vector2)
├── Fire button (hold)     → FireButton auto-fire loop
├── Reload / Jump / Crouch → direct method calls
├── Aim button (hold)      → SetMobileAim(bool)
└── Medkit / WeaponSwitch  → inventory calls

NetworkPlayerController
└── Update() reads:
    ├── IsOwner + !isMobile  → keyboard/mouse
    └── IsOwner + isMobile   → MobileInputManager values
```

---

## 10. Key Design Decisions

### Why Unity Relay instead of direct P2P?
Direct P2P requires players to expose their IP. Relay routes traffic through Unity's cloud infrastructure — no IP exposure, works behind NAT/firewalls, same join-code flow as BGMI.

### Why Owner-Authoritative movement (not Server-Authoritative)?
Server-auth movement requires client-side prediction + reconciliation — 400+ lines of additional code. For a prototype, owner-auth gives responsive feel with acceptable trade-off. Phase 4 roadmap targets dedicated servers with full server-auth movement.

### Why NGO over Mirror/Photon?
NGO is Unity's first-party networking solution. Direct integration with Relay, Authentication, and GameServerHosting — no third-party dependency. API is stable and matches Unity 2022.3 LTS lifecycle.

### Why procedural scene generation?
Avoids Unity scene file merge conflicts in Git (`.unity` files are binary). The `BattlegroundXSceneGenerator` produces the entire scene from code — reproducible, reviewable, and version-controllable.

### Why object pooling for VFX?
Mobile GPU/CPU budget is strict. Instantiate() during gameplay causes frame spikes. Pool pre-allocates at scene start and reuses objects — zero GC allocation during combat.

---

## 11. File Count Summary

| Category | Files |
|---|---|
| Gameplay C# Scripts | 22 |
| Phase 2 C# Scripts | 13 |
| Editor Tools | 3 |
| ProjectSettings | 13 |
| Documentation | 10 |
| CI/CD | 1 |
| **Total** | **62** |

---

## 12. Known Limitations and Phase 4 Upgrades

| Current (Prototype) | Phase 4 Target |
|---|---|
| Owner-auth movement | Server-auth + client prediction |
| Relay peer-hosted | Dedicated server fleet (Multiplay) |
| 10-player max | 100-player with delta compression |
| Greybox map | Textured environment with LODs |
| Anonymous auth only | Account system + leaderboard |
| No anti-cheat | Server-side movement validation |
