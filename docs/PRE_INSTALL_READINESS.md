# Battleground X — Pre-install readiness checklist

This pack prepares source, configuration, documentation and test instructions before Unity is installed.

## Completed in this pack
- GitHub repository structure and `.gitignore`
- Unity 2022.3 project version pin
- Package manifest for NGO, Transport and Unity Gaming Services
- Gameplay source scripts
- Editor menu scene/prefab generator
- Windows/Android setup guide
- Relay configuration guide
- Manual QA checklist and bug-report template
- Build/release checklist
- Project scope and known limitations

## Must happen after Unity installation
These tasks cannot be truthfully completed without running Unity and the platform toolchains:
1. Package resolution and C# compilation.
2. Running the Editor scene generator and inspecting generated scene/prefab.
3. Fixing actual compiler/runtime errors found by Unity.
4. Linking the Unity Cloud project and enabling Authentication/Relay.
5. Two-client/two-device Relay test.
6. Android APK build and physical-device test.
7. Windows EXE build and PC test.
8. Gameplay test report with real results.

## Current verified status
- ZIP archive integrity can be checked locally.
- Source files and docs are present in the archive.
- Unity compilation, live Relay, APK/EXE and gameplay have NOT been run in this environment.
