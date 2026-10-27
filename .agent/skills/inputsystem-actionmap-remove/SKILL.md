---
name: inputsystem-actionmap-remove
description: Remove an ActionMap (and all its Actions and Bindings) from a `.inputactions` asset and save it.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# InputSystem / Remove ActionMap

Remove an `InputActionMap` from an InputActionAsset. This deletes the map along with every Action and Binding it contains.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `mapName` — required. Name of the ActionMap to remove.

## Behavior

Loads the asset, removes the named ActionMap (failing if it does not exist), saves and re-imports the asset. Destructive. Runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "assetPath": "string_value",
  "mapName": "string_value"
}
'@)
unity-mcp-cli.cmd run-tool inputsystem-actionmap-remove --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `assetPath` | `string` | Yes | 'Assets/'-rooted path to the existing '.inputactions' asset. |
| `mapName` | `string` | Yes | Name of the ActionMap to remove. |

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
    }
  },
  "required": [
    "assetPath",
    "mapName"
  ]
}
```

## Output

This tool does not return structured output.

