---
name: cinemachine-set-aim
description: Add or replace the rotation-control (Aim) component of a `CinemachineCamera`, chosen by `AimType` (RotationComposer, HardLookAt, PanTilt, or None). Applies common params (screen X/Y, damping) to RotationComposer where applicable.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# Cinemachine / Set Aim

Add or replace the Aim (rotation-control) component of a `CinemachineCamera`. Any existing Aim component is removed first, then the chosen one is added — so this is destructive to the previous Aim component.

## Inputs

- `gameObjectRef` — the GameObject hosting the `CinemachineCamera` (required).
- `aimType` — `AimType` enum: `RotationComposer`, `HardLookAt`, `PanTilt`, or `None` (removes the Aim component).
- `screenX`, `screenY` — optional normalized on-screen target position for `RotationComposer` (`Composition.ScreenPosition`). Typically in the range -0.5..0.5 (0,0 = center).
- `damping` — optional uniform damping value (applies to `RotationComposer.Damping`).

## Behavior

Removes every existing `CinemachineRotationComposer`, `CinemachineHardLookAt`, and `CinemachinePanTilt` on the GameObject, then adds the requested one and applies the optional params. Marks the scene dirty and repaints. Runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "gameObjectRef": "string_value",
  "aimType": "string_value",
  "screenX": 0,
  "screenY": 0,
  "damping": 0
}
'@)
unity-mcp-cli.cmd run-tool cinemachine-set-aim --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `string` | Yes | Reference to the GameObject containing the CinemachineCamera component. RQL: GameObjectRef |
| `aimType` | `string` | Yes | Which rotation-control (Aim) component to use. |
| `screenX` | `number` | No | Normalized on-screen X target position for RotationComposer (Composition.ScreenPosition.x). |
| `screenY` | `number` | No | Normalized on-screen Y target position for RotationComposer (Composition.ScreenPosition.y). |
| `damping` | `number` | No | Uniform damping value applied where the component supports damping (RotationComposer). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "type": "string"
    },
    "aimType": {
      "type": "string",
      "enum": [
        "None",
        "RotationComposer",
        "HardLookAt",
        "PanTilt"
      ]
    },
    "screenX": {
      "type": "number"
    },
    "screenY": {
      "type": "number"
    },
    "damping": {
      "type": "number"
    }
  },
  "required": [
    "gameObjectRef",
    "aimType"
  ]
}
```

## Output

This tool does not return structured output.

