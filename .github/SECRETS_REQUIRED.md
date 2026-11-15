# GitHub Secrets Required for CI

Add these in your GitHub repo → **Settings → Secrets and variables → Actions → New repository secret**.

| Secret Name | Value |
|---|---|
| `UNITY_LICENSE` | Your Unity personal/pro license XML (from `unity-activate` step or manual export) |
| `UNITY_EMAIL` | The email address of your Unity account |
| `UNITY_PASSWORD` | Your Unity account password |

## How to Get UNITY_LICENSE

1. Install [GameCI](https://game.ci/docs/github/activation) and follow the manual activation workflow.
2. The workflow outputs a `.alf` file → upload to [license.unity3d.com](https://license.unity3d.com/manual) → download `.ulf`.
3. Paste the contents of the `.ulf` file as the `UNITY_LICENSE` secret value.

## Important
- **Never** commit secrets to git.
- **Never** commit `.ulf`, `.alf`, keystore files, or Unity Cloud credentials.
- The CI workflow uses [game-ci/unity-builder](https://github.com/game-ci/unity-builder) — a free open-source action.
