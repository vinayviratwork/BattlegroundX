# BATTLEGROUND X

**Original Unity 2022.3 online multiplayer battle-royale — Android & Windows x64**

> 10-player prototype | Phase 2 complete | Source-ready for Unity Editor

🔗 **GitHub:** [github.com/vinayviratwork/BattlegroundX](https://github.com/vinayviratwork/BattlegroundX)

---

## What's Inside

| System | Status |
|---|---|
| Multiplayer (NGO + Relay) | ✅ Host/join via 6-digit code |
| Authentication | ✅ Anonymous Unity Auth |
| Player controller | ✅ Walk · Run · Sprint · Crouch · Jump · Aim |
| Combat | ✅ AR · Shotgun · Sniper — ammo · reload · recoil |
| Inventory | ✅ Per-player ammo + medkits |
| Health | ✅ Server-authoritative damage + healing |
| Safe Zone | ✅ 7 shrink phases, escalating damage |
| Loot | ✅ Weighted spawn + proximity auto-pickup + respawn |
| Match Flow | ✅ Lobby → Countdown → Match → End → Lobby |
| Kill Feed | ✅ Kill credits + placement + winner detection |
| Spectator | ✅ Free-cam + cycle alive players |
| Mobile Input | ✅ Virtual joystick + look pad + all action buttons |
| **Animator System** | ✅ Full state machine — idle/walk/run/sprint/crouch/jump/fire/reload/death |
| **VFX** | ✅ Muzzle flash · Hit sparks · Blood · Screen flash · Bullet decals |
| **Minimap** | ✅ Overhead camera + RenderTexture + player icons + zone ring |
| **Settings Menu** | ✅ Sensitivity · Graphics quality · Volume (Escape key) |
| **Ping Display** | ✅ Live RTT — green/yellow/red |
| **Name Tags** | ✅ World-space floating name + HP |
| **Named Map Zones** | ✅ Military Base · School · Pochinki · Hospital · Port · Ruins · Farm · Shelter |
| **Audio** | ✅ Procedural WAV clips auto-generated and wired |

---

## Map

8 named zones on a 360×360 map:

```
           MILITARY BASE
    PORT      SCHOOL | POCHINKI    SHELTER
              -------+-------
    FARM     HOSPITAL | RUINS
```

Full zone guide: [Docs/MAP_GUIDE.md](Docs/MAP_GUIDE.md)

---

## Requirements

- **Unity 2022.3.62f1** (any 2022.3 LTS patch)
- Android Build Support (SDK + NDK + OpenJDK) — for APK
- Windows Build Support (IL2CPP) — for EXE
- Unity account + project with **Authentication** and **Relay** enabled

---

## Setup — 6 Steps

### 1. Clone
```bash
git clone https://github.com/vinayviratwork/BattlegroundX.git
```

### 2. Open in Unity
Open `BattlegroundX_Project/` from Unity Hub. Wait for packages to resolve (~5 min).

### 3. Generate Scene
```
Tools → Battleground X → Generate Full Scene
```
This creates the full map with all 8 named zones, all network objects, minimap, HUD, settings, and runs the Animator Controller generator automatically.

### 4. Setup Audio
```
Tools → Battleground X → Setup Audio (Procedural)
```
Generates and wires 7 procedural WAV clips into AudioManager. Replace with real clips any time by dragging WAV/MP3 files onto the AudioManager Inspector slots.

### 5. Link Unity Cloud
```
Edit → Project Settings → Services → Create Project ID
```
In [Unity Dashboard](https://dashboard.unity.com): enable **Authentication** + **Relay**.

### 6. Play Test
Press **Play** → click **HOST** → note the 6-digit join code.
Open a second Editor instance → enter code → **JOIN**.

---

## Controls

| PC | Action |
|---|---|
| WASD | Move |
| Mouse | Look |
| Left Shift | Sprint |
| C | Crouch |
| Space | Jump |
| LMB | Fire |
| RMB | Aim (ADS) |
| R | Reload |
| Scroll / Q-E | Switch weapon |
| F | Pickup loot |
| Escape | Settings menu |

**Android:** Virtual joystick (left) + look pad (right) + on-screen buttons.

---

## Build

### Android APK
`File → Build Settings → Android → Switch Platform → Build`
- Package: `com.yourstudio.battlegroundx`
- Min SDK: Android 6.0 (API 23)
- Orientation: Landscape

### Windows EXE
`File → Build Settings → Windows x86_64 → Switch Platform → Build`

---

## Project Structure

```
Assets/
  Audio/                    ← Generated WAV clips
  Animations/               ← NetworkPlayer.controller (auto-generated)
  Editor/                   ← Scene generator + Animator generator + Audio setup
  Prefabs/                  ← NetworkPlayer + 4 loot prefabs
  Scenes/                   ← BattlegroundX_Main.unity
  Scripts/
    Audio/                  ← AudioManager
    Combat/                 ← NetworkHealth, NetworkWeapon
    Data/                   ← GameSettings ScriptableObject
    Editor/                 ← AnimatorControllerGenerator, AudioSetup
    Networking/             ← RelaySessionUI, LobbyManager
    Player/                 ← NetworkPlayerController, PlayerAnimatorController,
                               PlayerInventory, PlayerStats, SpectatorController, CameraShake
    UI/                     ← NetworkHUD, MinimapController, MinimapIcon,
                               MinimapSafeZoneRing, SettingsMenu, PingDisplay, NameTag,
                               MobileInputManager, FireButton, MobileJoystickBridge
    VFX/                    ← VFXManager, MuzzleFlash, HitEffect, BloodEffect
    World/                  ← NetworkMatchManager, NetworkSafeZone, SpawnManager,
                               LootSpawner, NetworkLootPickup
Docs/
  MAP_GUIDE.md              ← Zone guide + strategy + loot table
  PROJECT_STATUS.md         ← Full completion status
  DEVELOPMENT_ROADMAP.md    ← Phase 1 ✅ Phase 2 ✅ Phase 3 → Phase 4 →
  TEST_PLAN.md
  QA_TEST_REPORT_TEMPLATE.md
  RELEASE_CHECKLIST.md
```

---

## What Still Needs Manual Work

| Task | Why |
|---|---|
| Assign animation motion clips | No .fbx animations included — assign in Animator window |
| Replace procedural audio (optional) | Drop real WAV/MP3 onto AudioManager Inspector slots |
| Link Unity Cloud project | Requires your own Unity account |
| Run two-device Relay test | Requires running builds |
| Build APK / EXE | Requires Unity Editor |

---

## Limitations

- Greybox map only — no commercial character models or textures
- Owner-authoritative movement (prototype) — not production anti-cheat
- Relay peer-hosted — no dedicated server fleet
- 10-player target — architecture extensible to more
- Never push Unity credentials or signing keystores to this repo

---

## License

Original source code only. See [LICENSE](LICENSE).
No BGMI, Krafton, or Rockstar Games assets, models, audio, or IP are included.
