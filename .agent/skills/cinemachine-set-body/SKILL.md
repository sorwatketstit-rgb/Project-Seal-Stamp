---
name: cinemachine-set-body
description: Add or replace the position-control (Body) component of a `CinemachineCamera`, chosen by `BodyType` (Follow, OrbitalFollow, ThirdPersonFollow, PositionComposer, HardLockToTarget, or None). Applies common params (followOffset, damping, cameraDistance) where applicable.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# Cinemachine / Set Body

Add or replace the Body (position-control) component of a `CinemachineCamera`. Any existing Body component is removed first, then the chosen one is added — so this is destructive to the previous Body component.

## Inputs

- `gameObjectRef` — the GameObject hosting the `CinemachineCamera` (required).
- `bodyType` — `BodyType` enum: `Follow`, `OrbitalFollow`, `ThirdPersonFollow`, `PositionComposer`, `HardLockToTarget`, or `None` (removes the Body component).
- `followOffset` — optional `Vector3` offset from the Follow target (applies to `Follow`).
- `damping` — optional uniform damping value (applied where the component supports damping).
- `cameraDistance` — optional distance from the target (applies to `ThirdPersonFollow` / `PositionComposer` / `OrbitalFollow` radius).

## Behavior

Removes every existing `CinemachineFollow`, `CinemachineOrbitalFollow`, `CinemachineThirdPersonFollow`, `CinemachinePositionComposer`, and `CinemachineHardLockToTarget` on the GameObject, then adds the requested one and applies the optional params. Marks the scene dirty and repaints. Runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "gameObjectRef": "string_value",
  "bodyType": "string_value",
  "followOffset": "string_value",
  "damping": 0,
  "cameraDistance": 0
}
'@)
unity-mcp-cli.cmd run-tool cinemachine-set-body --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `string` | Yes | Reference to the GameObject containing the CinemachineCamera component. RQL: GameObjectRef |
| `bodyType` | `string` | Yes | Which position-control (Body) component to use. |
| `followOffset` | `string` | No | Offset from the Follow target (applies to Follow). RQL: Vector3 |
| `damping` | `number` | No | Uniform damping value applied where the component supports damping. |
| `cameraDistance` | `number` | No | Distance from the target (ThirdPersonFollow/PositionComposer/OrbitalFollow radius). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "type": "string"
    },
    "bodyType": {
      "type": "string",
      "enum": [
        "None",
        "Follow",
        "OrbitalFollow",
        "ThirdPersonFollow",
        "PositionComposer",
        "HardLockToTarget"
      ]
    },
    "followOffset": {
      "type": "string"
    },
    "damping": {
      "type": "number"
    },
    "cameraDistance": {
      "type": "number"
    }
  },
  "required": [
    "gameObjectRef",
    "bodyType"
  ]
}
```

## Output

This tool does not return structured output.

