---
name: cinemachine-brain-ensure
description: Ensure a `CinemachineBrain` exists on a `Camera`. Targets the referenced Camera GameObject, or `Camera.main` when none is given. Returns the brain + camera info. A CinemachineBrain is required for any CinemachineCamera to actually drive the rendering Camera.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# Cinemachine / Ensure Brain

Ensure a `CinemachineBrain` component exists on a `Camera`. A CinemachineBrain is the bridge between Cinemachine virtual cameras and a real Unity `Camera`; without it, `CinemachineCamera` components have no effect on the rendered view.

## Inputs

- `cameraRef` — optional GameObject hosting a `Camera`. When omitted, `Camera.main` (a Camera tagged `MainCamera`) is used.

## Behavior

Resolves the Camera, adds a `CinemachineBrain` if one is missing (idempotent — an existing brain is reused), marks the scene dirty, repaints the Editor, and returns the brain instanceId plus the camera name. The whole call runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "cameraRef": "string_value"
}
'@)
unity-mcp-cli.cmd run-tool cinemachine-brain-ensure --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `cameraRef` | `string` | No | Optional reference to the GameObject hosting the Camera. If omitted, Camera.main is used. RQL: GameObjectRef |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "cameraRef": {
      "type": "string"
    }
  }
}
```

## Output

This tool does not return structured output.

