---
name: cinemachine-get
description: "Generic read: serialize any Cinemachine `Component` (a type in the `Unity.Cinemachine` namespace) on a GameObject via ReflectorNet. Pair with 'cinemachine-modify' to write changes back. Read-only."
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# Cinemachine / Get Component

Serialize any Cinemachine component on a GameObject using ReflectorNet. This is the generic escape hatch for fields not covered by the dedicated tools (e.g. a specific extension's settings).

## Inputs

- `gameObjectRef` — the GameObject hosting the component (required).
- `componentRef` — optional. Resolves a specific component when the GameObject has more than one Cinemachine component; otherwise the first component whose type lives in the `Unity.Cinemachine` namespace is used.
- `deepSerialization` — when `true`, recurses through nested objects; otherwise only top-level members.

## Behavior

Finds the target Cinemachine component, serializes it via ReflectorNet, and returns the serialized member plus the resolved component type name. Read-only. Runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "gameObjectRef": "string_value",
  "componentRef": "string_value",
  "deepSerialization": false
}
'@)
unity-mcp-cli.cmd run-tool cinemachine-get --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `string` | Yes | Reference to the GameObject containing the Cinemachine component. RQL: GameObjectRef |
| `componentRef` | `string` | No | Optional reference to a specific Cinemachine component if the GameObject has multiple. RQL: ComponentRef |
| `deepSerialization` | `boolean` | No | Performs deep serialization including nested objects. Otherwise only top-level members. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "type": "string"
    },
    "componentRef": {
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

