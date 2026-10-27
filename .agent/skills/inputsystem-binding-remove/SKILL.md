---
name: inputsystem-binding-remove
description: Remove a Binding by its index from an Action in a `.inputactions` asset, then save it.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# InputSystem / Remove Binding

Remove an `InputBinding` from an `InputAction` by its index within that Action's binding list. Use 'inputsystem-get' to discover binding indices first.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `mapName` / `actionName` — required. The Action that owns the binding.
- `bindingIndex` — required. Zero-based index of the binding within the Action.

## Behavior

Loads the asset, resolves the Action, erases the binding at the given index (failing if out of range), saves and re-imports the asset. Note: erasing a composite root also affects its parts. Destructive. Runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "assetPath": "string_value",
  "mapName": "string_value",
  "actionName": "string_value",
  "bindingIndex": 0
}
'@)
unity-mcp-cli.cmd run-tool inputsystem-binding-remove --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `assetPath` | `string` | Yes | 'Assets/'-rooted path to the existing '.inputactions' asset. |
| `mapName` | `string` | Yes | Name of the ActionMap containing the Action. |
| `actionName` | `string` | Yes | Name of the Action that owns the binding. |
| `bindingIndex` | `integer` | Yes | Zero-based index of the binding within the Action's binding list. |

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

