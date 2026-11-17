# Unity setup after installation — exact sequence

## A. Install editor and modules
Install Unity 2022.3.62f1 from Unity Hub if available. Select:
- Android Build Support
- Android SDK & NDK Tools
- OpenJDK
- Windows Build Support (IL2CPP)
- Visual Studio Community (recommended)

## B. Open project
1. Extract repository to a short path, e.g. `D:\\Projects\\BattlegroundX`.
2. Unity Hub → Add → Add project from disk.
3. Open the project and wait for package resolution.
4. If TMP asks to import Essential Resources, accept.
5. Open Window → General → Console.
6. Resolve all red compiler errors before proceeding.

## C. Generate scene
1. Tools → Battleground X → Generate Prototype Scene.
2. Open `Assets/Scenes/BattlegroundX_Main.unity`.
3. Save the scene.
4. Add scene to File → Build Settings → Scenes In Build.
5. Confirm NetworkManager's Player Prefab is `Assets/Prefabs/NetworkPlayer.prefab`.
6. Confirm `NetworkMatchManager` and `NetworkSafeZone` scene objects have NetworkObject components.

## D. Verify package compatibility
Packages are pinned in `Packages/manifest.json`. If Unity reports package version incompatibility:
- Open Window → Package Manager.
- Check NGO, Unity Transport, Services Core, Authentication, Relay and TMP versions.
- Use versions compatible with the selected Unity editor.
- Do not randomly upgrade every package.
- Save manifest and `packages-lock.json` after successful resolution.

## E. Relay
1. Create/link Unity Cloud project.
2. Enable Authentication and Relay.
3. Confirm the project uses the same Cloud project/environment on both test clients.
4. Run host on client A, copy join code.
5. Join from client B.
6. Record logs and test movement, hit damage, safe-zone damage and disconnect/reconnect.

## F. Android build
1. File → Build Settings → Android → Switch Platform.
2. Player Settings:
   - Package Identifier: `com.yourstudio.battlegroundx`
   - Product Name: `Battleground X`
   - Default Orientation: Landscape
   - Scripting Backend: IL2CPP for release
   - Target Architectures: ARM64
3. Add gameplay scene to Scenes In Build.
4. Build an APK for sideload testing.
5. Install on two real Android devices and test Relay.
6. Do not publish until privacy, age rating, store policy and performance checks are complete.

## G. Windows build
1. File → Build Settings → Windows, x86_64 → Switch Platform.
2. Add gameplay scene.
3. Build to `Builds/Windows/BattlegroundX`.
4. Test on a second Windows account/device if possible.
5. Zip the whole build folder, not just the EXE, when sharing.
