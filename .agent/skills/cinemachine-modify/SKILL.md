---
name: cinemachine-modify
description: "Generic write: apply a `SerializedMember` diff to any Cinemachine `Component` on a GameObject via ReflectorNet `TryModify`. Use 'cinemachine-get' first to inspect the structure so the diff is targeted."
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# Cinemachine / Modify Component

Modify any Cinemachine component on a GameObject by applying a `SerializedMember` diff via ReflectorNet. This is the generic escape hatch for fields not covered by the dedicated tools.

## Inputs

- `gameObjectRef` — the GameObject hosting the component (required).
- `componentRef` — optional. Resolves a specific component when the GameObject has more than one Cinemachine component; otherwise the first component in the `Unity.Cinemachine` namespace is used.
- `data` — the `SerializedMember` diff to apply. Include only the properties you want to change.

## Behavior

Finds the target Cinemachine component, applies the diff via `Reflector.TryModify`, and on success marks the component + scene dirty and repaints. The applied logs are returned. Runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "gameObjectRef": "string_value",
  "data": "string_value",
  "componentRef": "string_value"
}
'@)
unity-mcp-cli.cmd run-tool cinemachine-modify --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `string` | Yes | Reference to the GameObject containing the Cinemachine component. RQL: GameObjectRef |
| `data` | `string` | Yes | The SerializedMember diff to apply. Only include properties you want to change. RQL: SerializedMember |
| `componentRef` | `string` | No | Optional reference to a specific Cinemachine component if the GameObject has multiple. RQL: ComponentRef |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "type": "string"
    },
    "data": {
      "type": "string"
    },
    "componentRef": {
      "type": "string"
    }
  },
  "required": [
    "gameObjectRef",
    "data"
  ]
}
```

## Output

This tool does not return structured output.

