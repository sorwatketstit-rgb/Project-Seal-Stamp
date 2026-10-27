---
name: inputsystem-asset-create
description: Create a new `.inputactions` InputActionAsset at an 'Assets/'-rooted path, optionally seeding an initial ActionMap. Returns the created asset path.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# InputSystem / Create InputActionAsset

Create a new Unity Input System `InputActionAsset` (a `.inputactions` file). This is the container for ActionMaps, Actions, Bindings and Control Schemes.

## Inputs

- `assetPath` — required. An 'Assets/'-rooted path ending in `.inputactions` (e.g. `Assets/Input/Player.inputactions`). Intermediate folders must already exist (use 'assets-create-folder').
- `initialActionMap` — optional. When provided, an empty ActionMap with this name is added to the new asset.

## Behavior

Creates an empty `InputActionAsset`, optionally adds the initial ActionMap, serializes it to JSON, writes the file, and imports it into the AssetDatabase. Fails if an asset already exists at the path. Runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "assetPath": "string_value",
  "initialActionMap": "string_value"
}
'@)
unity-mcp-cli.cmd run-tool inputsystem-asset-create --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `assetPath` | `string` | Yes | 'Assets/'-rooted path ending in '.inputactions' for the new asset. |
| `initialActionMap` | `string` | No | Optional name for an initial ActionMap to add to the new asset. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "assetPath": {
      "type": "string"
    },
    "initialActionMap": {
      "type": "string"
    }
  },
  "required": [
    "assetPath"
  ]
}
```

## Output

This tool does not return structured output.

