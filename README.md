# BATTLEGROUND X

**An original Unity 2022.3 online multiplayer battle-royale — Android & Windows x64**

> This is a student-project prototype. All source code and project structure are complete and ready to open in Unity. No commercial 3D assets, animations, or sound clips are included. A two-device Relay test must be completed before claiming end-to-end functionality.

---

## Features

| System | Details |
|---|---|
| **Multiplayer** | Unity Relay + NGO + Unity Transport — host/join via 6-digit code |
| **Authentication** | Anonymous sign-in via Unity Authentication |
| **Player** | Third-person: walk · run · sprint · crouch · jump · aim · camera follow |
| **Combat** | Assault Rifle · Shotgun · Sniper — ammo · reload · recoil · weapon switch |
| **Inventory** | Per-player ammo reserves (rifle/shotgun/sniper) + medkits |
| **Health** | Server-authoritative damage · medkit heal · death events |
| **Safe Zone** | 7 shrink phases with escalating damage |
| **Loot** | Weighted spawn table · proximity auto-pickup · periodic respawn |
| **Match Flow** | Lobby → Countdown → In-Progress → Ended → Return to Lobby |
| **Elimination** | Placement tracking · kill credits · winner detection |
| **HUD** | Health bar · ammo counter · zone indicator · kill feed · match-end stats |
| **Spectator** | Free-cam + cycle through alive players on death |
| **Mobile** | Virtual joystick · look pad · fire/reload/jump/crouch/aim/medkit buttons |
| **Map** | 360×360 greybox: buildings · roads · trees · rocks · bridges · cover |

---

## Requirements

- **Unity 2022.3.62f1** (or any 2022.3 LTS patch)
- **Android Build Support** module (SDK + NDK + OpenJDK) — for APK
- **Windows Build Support (IL2CPP)** — for Windows EXE
- **Unity account** + Unity Cloud project with **Authentication** and **Relay** enabled

---

## First-Time Setup

### 1. Install Unity
Download [Unity Hub](https://unity.com/unity-hub) → install **2022.3.62f1** with:
- Android Build Support (SDK/NDK/JDK)
- Windows Build Support (IL2CPP)

### 2. Open Project
Open **this folder** (`BattlegroundX_Project/`) from Unity Hub.  
Wait for the Package Manager to download all packages (~3–5 min).

### 3. Link Unity Cloud
`Edit → Project Settings → Services → Create a new project ID`

In [Unity Dashboard](https://dashboard.unity.com):
1. Open your project.
2. Enable **Authentication** → Activate anonymous sign-in.
3. Enable **Relay** → note the region.

### 4. Generate the Scene
`Tools → Battleground X → Generate Full Scene`

This creates:
- `Assets/Scenes/BattlegroundX_Main.unity`
- `Assets/Prefabs/NetworkPlayer.prefab`
- `Assets/Prefabs/Loot_*.prefab` (4 loot prefabs)

### 5. (Optional) Assign Audio Clips
Select the **AudioManager** GameObject in the scene Hierarchy.  
Drag WAV/MP3 files onto: Shot, Reload, Hit, Footstep, Zone Warning, Pickup, Death.

### 6. Play Test
1. Press **Play** in Unity Editor.
2. Click **HOST** — a 6-digit join code appears.
3. Open a second Editor instance (or standalone build).
4. Enter the join code → click **JOIN**.
5. Both instances must be on the same Unity Cloud project/environment.

---

## Controls

### PC (Windows)
| Action | Key |
|---|---|
| Move | WASD |
| Look | Mouse |
| Sprint | Left Shift (hold) |
| Crouch | C or Left Ctrl |
| Jump | Space |
| Fire | Left Mouse Button |
| Aim (ADS) | Right Mouse Button (hold) |
| Reload | R |
| Switch weapon | Mouse scroll / Q–E |
| Interact / Pickup | F (or walk into loot — auto) |
| Use medkit | (wire to key in Inspector or mobile button) |
| Spectate cycle | Tab / Right Arrow (after death) |

### Android
| Action | Control |
|---|---|
| Move | Left virtual joystick |
| Look | Right swipe pad |
| Fire | Fire button (auto-fire when held) |
| Reload | Reload button |
| Jump | Jump button |
| Crouch | Crouch button |
| Aim (ADS) | Aim button (hold) |
| Use medkit | Medkit button |
| Switch weapon | Next / Prev weapon buttons |

---

## Build Instructions

### Android APK
1. `File → Build Settings → Android`
2. Click **Switch Platform** (wait for reimport).
3. Set **Package Name**: `com.yourstudio.battlegroundx`
4. Set **Minimum API Level**: Android 6.0 (API 23)
5. Set **Target API Level**: Android 13 (API 33)
6. Orientation: **Landscape**
7. Add `Assets/Scenes/BattlegroundX_Main.unity` to **Scenes In Build**.
8. Click **Build** → save as `BattlegroundX.apk`.

### Windows x64 EXE
1. `File → Build Settings → Windows, Mac, Linux`
2. Set architecture: **x86_64**
3. Click **Switch Platform**.
4. Add the scene to **Scenes In Build**.
5. Click **Build** → choose output folder.

---

## Online Test (Two Devices / Instances)

1. Build/run on **Device A** → click **HOST** → note the join code shown on screen.
2. Run on **Device B** → enter the join code → click **JOIN**.
3. Both devices need internet and must use the **same Unity Cloud project**.
4. The host acts as the network server in this prototype (Relay peer-hosted).

---

## Project Layout

```
Assets/
  Editor/                     ← One-click scene generator
  Scripts/
    Audio/                    ← AudioManager
    Combat/                   ← NetworkHealth, NetworkWeapon
    Data/                     ← GameSettings ScriptableObject
    Networking/               ← RelaySessionUI, LobbyManager
    Player/                   ← NetworkPlayerController, PlayerInventory,
                                 PlayerStats, SpectatorController, CameraShake
    UI/                       ← NetworkHUD, MobileInputManager, FireButton
    World/                    ← NetworkMatchManager, NetworkSafeZone,
                                 SpawnManager, LootSpawner, NetworkLootPickup
Packages/
  manifest.json
ProjectSettings/
  (all .asset files)
Docs/
  DEVELOPMENT_AUDIT.md
  DEVELOPMENT_ROADMAP.md
  PROJECT_STATUS.md
  TEST_PLAN.md
  QA_TEST_REPORT_TEMPLATE.md
  RELEASE_CHECKLIST.md
  BUG_REPORT_TEMPLATE.md
  RELAY_AND_SECURITY.md
  UNITY_SETUP_AFTER_INSTALL.md
```

---

## Important Limitations

- Greybox map only — no commercial character models, textures, or animations.
- AudioManager slots are empty — assign your own WAV/MP3 clips.
- Owner-authoritative movement (prototype) — not production anti-cheat.
- Relay peer-hosted — no dedicated server fleet.
- 10-player target; architecture allows expansion.
- Never push Unity credentials, Cloud keys, or signing keystores to GitHub.

---

## GitHub Push

```bash
git init
git add .
git commit -m "feat: Battleground X v0.1.0 prototype"
git branch -M main
git remote add origin https://github.com/YOUR-USERNAME/BattlegroundX.git
git push -u origin main
```

Replace the remote URL with your own repository.

---

## License

See [LICENSE](LICENSE). Original code only — no BGMI assets, models, or IP.
