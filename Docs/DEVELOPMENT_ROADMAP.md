# BATTLEGROUND X — DEVELOPMENT ROADMAP

**Version:** 0.2.0 (Phase 2 Complete)
**Updated:** 2026-11-15

---

## Phase 1 — Prototype ✅ COMPLETE

> Core architecture — all gameplay systems written at source level.

| Task | Status |
|---|---|
| Unity 2022.3 project structure | ✅ |
| NGO + Transport + Relay networking | ✅ |
| Anonymous Unity Authentication | ✅ |
| Third-person player controller (walk/run/sprint/crouch/jump/aim) | ✅ |
| 3-weapon system with ammo, reload, recoil | ✅ |
| Server-authoritative damage & health | ✅ |
| Player inventory (ammo + medkits) | ✅ |
| 7-phase shrinking safe zone | ✅ |
| Loot spawner with weighted table + respawn | ✅ |
| Match state machine (lobby → countdown → match → ended) | ✅ |
| Elimination tracking + winner detection | ✅ |
| Return-to-lobby flow | ✅ |
| Kill feed + match-end screen + stats | ✅ |
| Spectator camera | ✅ |
| Mobile virtual joystick + look pad + action buttons | ✅ |
| Editor one-click scene generator | ✅ |
| Greybox map (360×360) with buildings/roads/trees/cover | ✅ |
| ProjectSettings for Android + Windows x64 | ✅ |
| GitHub Actions CI skeleton | ✅ |

---

## Phase 2 — Alpha ✅ COMPLETE (source-level)

> Playability layer — animation, VFX, minimap, UI polish.

| Task | Status |
|---|---|
| Animator Controller with all states and parameters | ✅ AnimatorControllerGenerator.cs — auto-generates .controller asset |
| PlayerAnimatorController driven from NetworkVariables | ✅ Reads IsRunning, IsCrouching, IsAiming, IsDead; IK support |
| VFXManager — pooled particle effects | ✅ Muzzle flash, hit sparks, blood, explosion; procedural fallback |
| MuzzleFlash — light pulse + shell eject | ✅ Attached to MuzzlePoint child on player prefab |
| HitEffect — sparks + decal pool | ✅ Pooled bullet-hole quads with fade-out |
| BloodEffect — server→ClientRpc splatter | ✅ Screen flash for local owner |
| MinimapController — overhead camera + RenderTexture | ✅ Follows local player, optional player-facing rotation |
| MinimapIcon — per-player colour-coded dot | ✅ Direction arrow; green=owner, red=enemy |
| MinimapSafeZoneRing — live zone boundary | ✅ Scales with NetworkSafeZone.CurrentRadius |
| SettingsMenu — sensitivity / quality / volume | ✅ PlayerPrefs persisted; Escape to toggle; settings open button on HUD |
| PingDisplay — RTT via ServerRpc echo | ✅ Rolling 5-sample average; colour-coded green/yellow/red |
| NameTag — world-space TMP billboard | ✅ Shows name + HP; hides on death and beyond max distance |
| BattlegroundXSceneGenerator Phase 2 wiring | ✅ All new components added to prefab and scene automatically |

### Remaining manual steps for Phase 2 to run:
| Step | Status |
|---|---|
| Assign AnimationClip motions to each Animator state | ⚠️ Manual — no clips included |
| Assign AudioClips to AudioManager slots | ⚠️ Manual — no clips included |
| Unity Editor compile test | ⚠️ Requires Unity install |
| Two-device Relay test + join code flow | ⚠️ Requires running builds |
| Fill QA_TEST_REPORT_TEMPLATE.md | ⚠️ Requires test run |
| Android APK build test | ⚠️ Requires Unity + Android SDK |
| Windows x64 build test | ⚠️ Requires Unity |

---

## Phase 3 — Beta

> Polished gameplay loop, 10 real players tested.

| Task | Priority |
|---|---|
| Replace greybox with higher-quality models/terrain | 🟡 Medium |
| Texture and material pass on all surfaces | 🟡 Medium |
| LOD groups on buildings and trees | 🟡 Medium |
| Occlusion culling bake | 🟡 Medium |
| Post-processing stack (bloom, color grading, vignette) | 🟡 Medium |
| 10-player load test via Relay | 🔴 High |
| Performance profiling (CPU + GPU + memory) | 🔴 High |
| Android GPU instancing + draw call batching | 🟡 Medium |
| Weapons: add pistol + melee | 🟢 Low |
| Parachute spawn drop | 🟢 Low |
| Ping/latency display (server-side RTT) | ✅ Done in Phase 2 |
| Settings menu (sensitivity, graphics quality) | ✅ Done in Phase 2 |
| In-game name tags | ✅ Done in Phase 2 |

---

## Phase 4 — Release Candidate

> Store-ready build with no known critical bugs.

| Task | Priority |
|---|---|
| Dedicated server migration (Game Server Hosting / Multiplay) | 🔴 High |
| Anti-cheat server-side movement validation | 🔴 High |
| Matchmaking / lobby invites | 🔴 High |
| Account persistence + leaderboard | 🟡 Medium |
| Ranking system | 🟢 Low |
| Squad/duo mode | 🟢 Low |
| Bot AI for fill players | 🟢 Low |
| Analytics integration | 🟢 Low |
| Play Store APK signing + release track | 🔴 High |
| Windows Steam / sideload release | 🟡 Medium |
| GDPR/privacy policy + age gate | 🔴 High |
| Final QA pass + release checklist sign-off | 🔴 High |

---

## Architecture Scalability Notes

- NetworkMatchManager is a singleton NetworkBehaviour — replace with a fleet-managed session service when moving to dedicated servers.
- PlayerInventory uses NetworkVariables; at 100-player scale, consider delta-compressed custom messages.
- SpawnManager's anti-overlap check is O(n²) — acceptable for 10 players, needs optimisation for 100.
- AudioManager is a local MonoBehaviour singleton — extend with a spatial audio system for 3D positional SFX.
- VFXManager uses a simple Queue pool — at high player counts (>20), increase pool sizes or switch to a job-based particle system.
- MinimapController uses one overhead camera per scene — for large maps, reduce ortho size or use a chunked minimap texture.
