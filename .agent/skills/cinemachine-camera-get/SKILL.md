---
name: cinemachine-camera-get
description: Inspect a `CinemachineCamera` — its lens, Follow / LookAt targets, priority, and the list of pipeline components (Body / Aim / Noise / extensions) currently attached. Read-only.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# Cinemachine / Get Camera

Inspect a `CinemachineCamera` component in the active scene. Returns the priority, the Follow and LookAt target names, the serialized `LensSettings`, and the list of pipeline / extension components attached to the camera GameObject.

## Inputs

- `gameObjectRef` — the GameObject hosting the `CinemachineCamera` (required).
- `deepSerialization` — when `true`, the lens is serialized recursively through nested objects; otherwise only top-level members are serialized.

## Behavior

Serializes `CinemachineCamera.Lens` via ReflectorNet and enumerates every `CinemachineComponentBase` and `CinemachineExtension` on the GameObject by type name. Read-only — nothing is mutated. The whole call runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "gameObjectRef": "string_value",
  "deepSerialization": false
}
'@)
unity-mcp-cli.cmd run-tool cinemachine-camera-get --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `string` | Yes | Reference to the GameObject containing the CinemachineCamera component. RQL: GameObjectRef |
| `deepSerialization` | `boolean` | No | Performs deep serialization of the lens including nested objects. Otherwise only top-level members. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "type": "string"
    },
    "deepSerialization": {
      "type": "boolean"
    }
  },
  "required": [
    "gameObjectRef"
  ]
}
```

## Output

This tool does not return structured output.

