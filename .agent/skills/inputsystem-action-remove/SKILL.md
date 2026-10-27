---
name: inputsystem-action-remove
description: Remove an Action (and its Bindings) from an ActionMap in a `.inputactions` asset and save it.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# InputSystem / Remove Action

Remove an `InputAction` from an `InputActionMap`. This deletes the Action and every Binding associated with it.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `mapName` — required. Name of the ActionMap containing the Action.
- `actionName` — required. Name of the Action to remove.

## Behavior

Loads the asset, removes the named Action from the map (failing if it does not exist), saves and re-imports the asset. Destructive. Runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "assetPath": "string_value",
  "mapName": "string_value",
  "actionName": "string_value"
}
'@)
unity-mcp-cli.cmd run-tool inputsystem-action-remove --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `assetPath` | `string` | Yes | 'Assets/'-rooted path to the existing '.inputactions' asset. |
| `mapName` | `string` | Yes | Name of the ActionMap containing the Action. |
| `actionName` | `string` | Yes | Name of the Action to remove. |

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
    }
  },
  "required": [
    "assetPath",
    "mapName",
    "actionName"
  ]
}
```

## Output

This tool does not return structured output.

