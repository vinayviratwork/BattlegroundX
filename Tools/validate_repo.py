from pathlib import Path
import json, sys

root = Path(__file__).resolve().parents[1]
errors = []

manifest = root / "Packages/manifest.json"
try:
    data = json.loads(manifest.read_text(encoding="utf-8"))
    if "dependencies" not in data:
        errors.append("Packages/manifest.json missing dependencies")
except Exception as exc:
    errors.append(f"Invalid package manifest: {exc}")

required = [
    "README.md",
    ".gitignore",
    "ProjectSettings/ProjectVersion.txt",
    "Assets/Editor/BattlegroundXSceneGenerator.cs",
    "Assets/Scripts/Networking/RelaySessionUI.cs",
    "Assets/Scripts/Player/NetworkPlayerController.cs",
    "Assets/Scripts/Combat/NetworkHealth.cs",
    "Assets/Scripts/Combat/NetworkWeapon.cs",
    "Assets/Scripts/World/NetworkSafeZone.cs",
    "Assets/Scripts/World/NetworkLootPickup.cs",
    "Assets/Scripts/World/NetworkMatchManager.cs",
]
for item in required:
    if not (root / item).exists():
        errors.append(f"Missing required file: {item}")

if errors:
    print("Repository validation FAILED:")
    print("\\n".join(errors))
    sys.exit(1)

print("Repository structure and JSON manifest validation PASSED.")
print("Note: this does NOT compile C# or validate Unity APIs. Run Unity Editor to do that.")
