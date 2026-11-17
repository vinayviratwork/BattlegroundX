# Manual test plan

Do not mark a test passed until it has been run on the target device/build.

| ID | Test | Expected result |
|---|---|---|
| T01 | Open project in pinned Unity version | Packages resolve; no compile errors |
| T02 | Generate prototype scene | Scene and player prefab are created |
| T03 | Press Play in Editor | HUD appears; no missing references |
| T04 | Host session | Relay join code is displayed |
| T05 | Join from second client | Both player objects appear |
| T06 | Move both clients | Remote player transforms replicate |
| T07 | Fire at another player | Server decreases target health |
| T08 | Enter shrinking zone | Outside player health decreases |
| T09 | Pick up medkit | Health increases; pickup disappears |
| T10 | Eliminate all but one player | Winner state is displayed |
| T11 | Windows build | Game launches and can host/join |
| T12 | Android build | Game launches in landscape and touch controls work |

Record device model, OS, Unity version, package versions, logs and screenshots for failures.
