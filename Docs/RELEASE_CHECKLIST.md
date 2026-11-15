# Release checklist

## Source control
- [ ] Unity-generated `Library/`, `Temp/`, `Obj/`, `Builds/` are ignored.
- [ ] `Packages/manifest.json` and `Packages/packages-lock.json` committed.
- [ ] No passwords, service secrets or personal access tokens committed.
- [ ] README reflects tested features only.
- [ ] Git tag created after a verified milestone.

## Windows
- [ ] Windows x86_64 build succeeds.
- [ ] Test host/join, movement, shooting, HUD, safe zone and match end.
- [ ] Share the complete build directory or ZIP.

## Android
- [ ] ARM64 APK builds.
- [ ] Installs and launches on target device.
- [ ] Touch input works and screen stays landscape.
- [ ] Thermal/performance and network tests completed.
- [ ] Two-device Relay test passes.

## Submission
- [ ] Screenshots and screen recording captured from real build.
- [ ] QA report completed with actual results.
- [ ] Project report and presentation match implemented functionality.
- [ ] Known limitations disclosed.
