---
name: inputsystem-actionmap-add
description: Add a new ActionMap to an existing `.inputactions` InputActionAsset and save it.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# InputSystem / Add ActionMap

Add a new `InputActionMap` to an InputActionAsset. An ActionMap groups related Actions (e.g. a 'Player' map and a 'UI' map).

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `mapName` — required. Name of the new ActionMap. Must be unique within the asset.

## Behavior

Loads the asset, adds the ActionMap (failing if a map of that name already exists), saves the asset back to disk, and re-imports it. Runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "assetPath": "string_value",
  "mapName": "string_value"
}
'@)
unity-mcp-cli.cmd run-tool inputsystem-actionmap-add --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `assetPath` | `string` | Yes | 'Assets/'-rooted path to the existing '.inputactions' asset. |
| `mapName` | `string` | Yes | Name of the new ActionMap (unique within the asset). |

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

