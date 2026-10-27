---
name: inputsystem-binding-set
description: Update a Binding's path, groups, interactions and/or processors by index on an Action in a `.inputactions` asset, then save it. Only the supplied fields are changed.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# InputSystem / Set Binding

Update fields of an existing `InputBinding` (identified by its index on an `InputAction`). Only the parameters you provide are applied; the rest are left unchanged. Use 'inputsystem-get' to find the binding index first.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `mapName` / `actionName` — required. The Action that owns the binding.
- `bindingIndex` — required. Zero-based index of the binding.
- `path` — optional new control path.
- `groups` — optional new control-scheme group(s).
- `interactions` — optional new interactions.
- `processors` — optional new processors.

## Behavior

Loads the asset, resolves the Action and binding index (failing if out of range), applies each supplied field via the `ChangeBinding` syntax (`WithPath` / `WithGroups` / `WithInteractions` / `WithProcessors`), saves and re-imports the asset. Runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "assetPath": "string_value",
  "mapName": "string_value",
  "actionName": "string_value",
  "bindingIndex": 0,
  "path": "string_value",
  "groups": "string_value",
  "interactions": "string_value",
  "processors": "string_value"
}
'@)
unity-mcp-cli.cmd run-tool inputsystem-binding-set --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `assetPath` | `string` | Yes | 'Assets/'-rooted path to the existing '.inputactions' asset. |
| `mapName` | `string` | Yes | Name of the ActionMap containing the Action. |
| `actionName` | `string` | Yes | Name of the Action that owns the binding. |
| `bindingIndex` | `integer` | Yes | Zero-based index of the binding to update. |
| `path` | `string` | No | Optional new control path. |
| `groups` | `string` | No | Optional new control-scheme group(s). |
| `interactions` | `string` | No | Optional new interactions. |
| `processors` | `string` | No | Optional new processors. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "assetPath": {
      "type": "string"
    },
    "mapName": {
      "type": "string"
    },
    "actionName": {
      "type": "string"
    },
    "bindingIndex": {
      "type": "integer"
    },
    "path": {
      "type": "string"
    },
    "groups": {
      "type": "string"
    },
    "interactions": {
      "type": "string"
    },
    "processors": {
      "type": "string"
    }
  },
  "required": [
    "assetPath",
    "mapName",
    "actionName",
    "bindingIndex"
  ]
}
```

## Output

This tool does not return structured output.

