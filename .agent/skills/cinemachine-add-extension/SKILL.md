---
name: cinemachine-add-extension
description: Add a `CinemachineExtension`-derived component (e.g. `CinemachineDeoccluder`, `CinemachineConfiner3D`, `CinemachineFollowZoom`, `CinemachineRecomposer`) to a `CinemachineCamera` GameObject, resolved by type name. The type is validated to derive from `CinemachineExtension`.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# Cinemachine / Add Extension

Add a Cinemachine extension component to a `CinemachineCamera` GameObject. Extensions augment the camera pipeline (collision avoidance, framing, post-processing, etc.).

## Inputs

- `gameObjectRef` — the GameObject hosting the `CinemachineCamera` (required).
- `extensionTypeName` — the extension type name. A full name like `Unity.Cinemachine.CinemachineDeoccluder` is preferred; a bare class name like `CinemachineDeoccluder` is also resolved against the `Unity.Cinemachine` namespace as a fallback.

## Behavior

Resolves the type, verifies it derives from `Unity.Cinemachine.CinemachineExtension`, adds it to the GameObject, marks the scene dirty, and repaints. Throws if the type cannot be resolved or is not an extension. Runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "gameObjectRef": "string_value",
  "extensionTypeName": "string_value"
}
'@)
unity-mcp-cli.cmd run-tool cinemachine-add-extension --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `string` | Yes | Reference to the GameObject containing the CinemachineCamera component. RQL: GameObjectRef |
| `extensionTypeName` | `string` | Yes | Extension type name, e.g. 'Unity.Cinemachine.CinemachineDeoccluder' or 'CinemachineDeoccluder'. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "type": "string"
    },
    "extensionTypeName": {
      "type": "string"
    }
  },
  "required": [
    "gameObjectRef",
    "extensionTypeName"
  ]
}
```

## Output

This tool does not return structured output.

