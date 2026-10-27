---
name: cinemachine-set-lens
description: Set lens fields of a `CinemachineCamera`'s `LensSettings` — field of view (perspective) or orthographic size, near / far clip planes, and Dutch (roll) angle. Only the fields you pass are changed.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# Cinemachine / Set Lens

Set lens fields on a `CinemachineCamera`. The `LensSettings` controls how the resulting Unity Camera is configured while this virtual camera is live.

## Inputs

- `gameObjectRef` — the GameObject hosting the `CinemachineCamera` (required).
- `fieldOfView` — optional vertical FOV in degrees (perspective cameras).
- `orthographicSize` — optional orthographic half-height (orthographic cameras).
- `nearClipPlane`, `farClipPlane` — optional clip plane distances.
- `dutch` — optional Dutch (roll) angle in degrees.

## Behavior

Reads the current `LensSettings`, applies only the provided fields, writes it back, marks the scene dirty, and repaints the Editor. The whole call runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "gameObjectRef": "string_value",
  "fieldOfView": 0,
  "orthographicSize": 0,
  "nearClipPlane": 0,
  "farClipPlane": 0,
  "dutch": 0
}
'@)
unity-mcp-cli.cmd run-tool cinemachine-set-lens --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `string` | Yes | Reference to the GameObject containing the CinemachineCamera component. RQL: GameObjectRef |
| `fieldOfView` | `number` | No | Vertical field of view in degrees (perspective). |
| `orthographicSize` | `number` | No | Orthographic size (half-height) for orthographic cameras. |
| `nearClipPlane` | `number` | No | Near clip plane distance. |
| `farClipPlane` | `number` | No | Far clip plane distance. |
| `dutch` | `number` | No | Dutch (roll) angle in degrees. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "type": "string"
    },
    "fieldOfView": {
      "type": "number"
    },
    "orthographicSize": {
      "type": "number"
    },
    "nearClipPlane": {
      "type": "number"
    },
    "farClipPlane": {
      "type": "number"
    },
    "dutch": {
      "type": "number"
    }
  },
  "required": [
    "gameObjectRef"
  ]
}
```

## Output

This tool does not return structured output.

