---
name: cinemachine-camera-list
description: List every `CinemachineCamera` in the active scene with its name, priority, and whether it is currently the live (active) camera according to an active `CinemachineBrain`. Read-only.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# Cinemachine / List Cameras

List every `CinemachineCamera` in the active scene. For each, returns the name, the priority value, a reference, and whether the camera is currently live (driving the view through an active `CinemachineBrain`).

## Inputs

- `includeInactive` (bool, default true) — include inactive/disabled CinemachineCameras.

## Behavior

Finds all `CinemachineCamera` instances (including inactive ones), reads each `Priority.Value`, and compares against `CinemachineBrain.ActiveVirtualCamera` across all active brains to compute `isLive`. Read-only. The whole call runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "includeInactive": false
}
'@)
unity-mcp-cli.cmd run-tool cinemachine-camera-list --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `includeInactive` | `boolean` | No | If true (default), include inactive/disabled CinemachineCameras; if false, only those on active GameObjects. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "includeInactive": {
      "type": "boolean"
    }
  }
}
```

## Output

This tool does not return structured output.

