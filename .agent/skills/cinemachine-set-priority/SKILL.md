---
name: cinemachine-set-priority
description: Set the `Priority.Value` of a `CinemachineCamera`. The highest-priority active camera wins control of the `CinemachineBrain`.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# Cinemachine / Set Priority

Set the priority of a `CinemachineCamera`. When multiple CinemachineCameras are active, the one with the highest priority becomes live (drives the rendered view through the `CinemachineBrain`).

## Inputs

- `gameObjectRef` — the GameObject hosting the `CinemachineCamera` (required).
- `priority` — the new integer priority value.

## Behavior

Assigns `CinemachineCamera.Priority` with the given value, marks the scene dirty, and repaints the Editor. The whole call runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "gameObjectRef": "string_value",
  "priority": 0
}
'@)
unity-mcp-cli.cmd run-tool cinemachine-set-priority --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `string` | Yes | Reference to the GameObject containing the CinemachineCamera component. RQL: GameObjectRef |
| `priority` | `integer` | Yes | The new priority value. Higher wins when multiple cameras are active. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "type": "string"
    },
    "priority": {
      "type": "integer"
    }
  },
  "required": [
    "gameObjectRef",
    "priority"
  ]
}
```

## Output

This tool does not return structured output.

