# BATTLEGROUND X

<div align="center">

![Unity](https://img.shields.io/badge/Unity-2022.3.62f1-black?logo=unity&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20Windows-blue?logo=android)
![Networking](https://img.shields.io/badge/Networking-Unity%20NGO%20%2B%20Relay-orange)
![Players](https://img.shields.io/badge/Players-10%20Online-green)
![Phase](https://img.shields.io/badge/Phase-2%20Complete-brightgreen)
![License](https://img.shields.io/badge/License-Original%20Code-lightgrey)
![GitHub](https://img.shields.io/badge/GitHub-vinayviratwork-181717?logo=github)

**An original Unity 2022.3 online multiplayer battle-royale — Android & Windows x64**

*10 players · 8 named zones · Full match loop · Mobile + PC controls*

🔗 [github.com/vinayviratwork/BattlegroundX](https://github.com/vinayviratwork/BattlegroundX)

</div>

---

## Map — 8 Named Zones

```
╔═══════════════════════════════════════════════════════════╗
║                                                           ║
║          PORT (NW)       🪖 MILITARY BASE (N)             ║
║              ╲                    ║                       ║
║               ╲                   ║                       ║
║  🏫 SCHOOL ════╬════════════════╬════ 🏘️ POCHINKI         ║
║  (W)           ║                ║              (E)        ║
║               ╱    C E N T E R  ║                        ║
║              ╱                   ║                       ║
║          🌾 FARM (SW)      🏥 HOSPITAL (S)                ║
║                                                           ║
║          🛖 SHELTER (NE)   🏚️ RUINS (SE)                  ║
║                                                           ║
║   Map: 360×360 units  |  7 safe-zone phases              ║
╚═══════════════════════════════════════════════════════════╝
```

📖 Full zone guide → [Docs/MAP_GUIDE.md](Docs/MAP_GUIDE.md)

---

## Feature Overview

| System | Details |
|---|---|
| 🌐 **Multiplayer** | Unity NGO 1.8.1 + Transport 2.2.1 — host/join via 6-digit Relay code |
| 🔐 **Authentication** | Anonymous sign-in via Unity Authentication |
| 🏃 **Player** | Third-person: walk · run · sprint · crouch · jump · aim · camera follow |
| 🔫 **Combat** | Assault Rifle · Shotgun · Sniper — ammo · reload · recoil · weapon switch |
| 🎒 **Inventory** | Per-player ammo reserves (rifle/shotgun/sniper) + medkits |
| ❤️ **Health** | Server-authoritative damage · medkit heal · death events |
| 🔵 **Safe Zone** | 7 shrink phases with escalating damage (1→20 dmg/sec) |
| 📦 **Loot** | Weighted spawn table · proximity auto-pickup · periodic respawn |
| 🎮 **Match Flow** | Lobby → Countdown → In-Progress → Ended → Return to Lobby |
| 🏆 **Elimination** | Placement tracking · kill credits · winner detection |
| 📺 **HUD** | Health bar · ammo counter · zone indicator · kill feed · match-end stats |
| 👁️ **Spectator** | Free-cam + cycle through alive players on death |
| 📱 **Mobile** | Virtual joystick · look pad · fire/reload/jump/crouch/aim/medkit buttons |
| 🎬 **Animator** | Full state machine — idle/walk/run/sprint/crouch/jump/fire/reload/death + IK |
| 💥 **VFX** | Pooled particles — muzzle flash · hit sparks · blood · explosion · screen flash |
| 🗺️ **Minimap** | Overhead RenderTexture camera · player icons · safe-zone ring |
| ⚙️ **Settings** | Sensitivity · graphics quality · audio volume (PlayerPrefs, Escape key) |
| 📶 **Ping** | Live RTT display via ServerRpc echo — green/yellow/red |
| 🏷️ **Name Tags** | World-space TMP billboard: name + HP · hides on death/distance |
| 🔊 **Audio** | Procedural WAV clips auto-generated · replace with real clips anytime |

---

## Architecture Highlights

```
CLIENT                          UNITY RELAY (Cloud)              HOST
  │                                     │                          │
  │──── FireServerRpc(dir, weapon) ────▶│────────────────────────▶│
  │                                     │   Server validates:      │
  │                                     │   • ammo > 0             │
  │                                     │   • direction valid      │
  │                                     │   • raycast on server    │
  │◀─── FireHitFeedbackClientRpc ───────│◀────────────────────────│
  │     (hitPoint, normal, didHit)      │                          │
  │                                     │                          │
  └── VFX + Audio on all clients        │                          │
```

📖 Full architecture doc → [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md)

---

## Screenshots

> **To add screenshots:** Open Unity → `Tools → Battleground X → Generate Full Scene` → Press Play → take screenshots → add to `Docs/Screenshots/` folder

| Gameplay | Minimap | Settings |
|---|---|---|
| *(add screenshot)* | *(add screenshot)* | *(add screenshot)* |

---

## Requirements

| Tool | Version |
|---|---|
| Unity Hub | Latest |
| Unity Editor | 2022.3.62f1 LTS |
| Android Build Support | SDK + NDK + OpenJDK |
| Windows Build Support | IL2CPP |
| Unity Account | Free tier works |

---

## Setup in 6 Steps

### 1. Clone
```bash
git clone https://github.com/vinayviratwork/BattlegroundX.git
cd BattlegroundX
```

### 2. Open in Unity Hub
Open `BattlegroundX_Project/` → wait for packages to resolve (~5 min first time)

### 3. Generate Full Scene
```
Tools → Battleground X → Generate Full Scene
```
Creates the complete map with all 8 zones, networking, minimap, HUD, settings, and auto-generates the Animator Controller.

### 4. Setup Audio
```
Tools → Battleground X → Setup Audio (Procedural)
```
Auto-generates 7 WAV clips and wires them into AudioManager. Replace with real clips anytime.

### 5. Link Unity Cloud
```
Edit → Project Settings → Services → Create Project ID
```
In [Unity Dashboard](https://dashboard.unity.com) → enable **Authentication** + **Relay**

### 6. Play Test
```
Press Play → HOST → copy 6-digit code
Open second Editor window → JOIN → enter code
```

---

## Controls

<details>
<summary>PC Controls</summary>

| Key | Action |
|---|---|
| WASD | Move |
| Mouse | Look |
| Left Shift | Sprint |
| C / Left Ctrl | Crouch |
| Space | Jump |
| Left Mouse | Fire |
| Right Mouse (hold) | Aim (ADS) |
| R | Reload |
| Scroll / Q-E | Switch weapon |
| F | Pickup loot |
| Escape | Settings menu |
| Tab | Cycle spectator |

</details>

<details>
<summary>Android Controls</summary>

| Control | Action |
|---|---|
| Left joystick | Move |
| Right swipe pad | Look |
| Fire button | Fire (auto when held) |
| Reload button | Reload |
| Jump button | Jump |
| Crouch button | Crouch |
| Aim button (hold) | ADS |
| Medkit button | Heal |
| Weapon buttons | Switch weapon |

</details>

---

## Build

<details>
<summary>Android APK</summary>

1. `File → Build Settings → Android`
2. Switch Platform
3. Package: `com.yourstudio.battlegroundx`
4. Min SDK: API 23 · Target: API 33
5. Orientation: Landscape
6. Add scene → Build

</details>

<details>
<summary>Windows x64 EXE</summary>

1. `File → Build Settings → Windows, Mac, Linux`
2. Architecture: x86_64
3. Switch Platform → Add scene → Build

</details>

---

## Project Structure

```
BattlegroundX_Project/
├── Assets/
│   ├── Audio/              ← Generated WAV clips
│   ├── Animations/         ← NetworkPlayer.controller
│   ├── Editor/             ← Scene + Animator + Audio generators
│   ├── Prefabs/            ← NetworkPlayer + 4 loot prefabs
│   ├── Scenes/             ← BattlegroundX_Main.unity
│   └── Scripts/
│       ├── Audio/          ← AudioManager
│       ├── Combat/         ← NetworkHealth, NetworkWeapon
│       ├── Data/           ← GameSettings ScriptableObject
│       ├── Editor/         ← AnimatorControllerGenerator, AudioSetup
│       ├── Networking/     ← RelaySessionUI, LobbyManager
│       ├── Player/         ← Controller, Animator, Inventory, Stats, Spectator
│       ├── UI/             ← HUD, Minimap, Settings, Ping, NameTag, Mobile
│       ├── VFX/            ← VFXManager, MuzzleFlash, HitEffect, BloodEffect
│       └── World/          ← MatchManager, SafeZone, Spawner, Loot
├── Docs/
│   ├── ARCHITECTURE.md     ← Technical deep-dive
│   ├── MAP_GUIDE.md        ← Zone guide + strategy
│   ├── PROJECT_STATUS.md   ← Completion status
│   ├── DEVELOPMENT_ROADMAP.md
│   ├── TEST_PLAN.md
│   └── QA_TEST_REPORT_TEMPLATE.md
├── Packages/
│   └── manifest.json       ← NGO, Relay, Auth, URP, TMP, Input System
└── ProjectSettings/        ← Android + Windows x64 configured
```

---

## Roadmap

| Phase | Status | Goal |
|---|---|---|
| Phase 1 — Prototype | ✅ Complete | Full gameplay architecture |
| Phase 2 — Alpha | ✅ Complete | Animator, VFX, Minimap, UI polish |
| Phase 3 — Beta | 🔲 Planned | Textures, LODs, 10-player load test |
| Phase 4 — Release | 🔲 Planned | Dedicated servers, anti-cheat, store release |

📖 Full roadmap → [Docs/DEVELOPMENT_ROADMAP.md](Docs/DEVELOPMENT_ROADMAP.md)

---

## Important Notes

- Greybox map only — no commercial character models or textures included
- Owner-authoritative movement (prototype) — not production anti-cheat
- Relay peer-hosted — no dedicated server fleet
- Never push Unity credentials or signing keystores to this repo

---

## License

Original source code only. See [LICENSE](LICENSE).
No BGMI, Krafton, or any third-party game assets are included.
