---
name: inputsystem-save
description: Re-serialize a `.inputactions` InputActionAsset to disk and re-import it. Useful to force-persist an asset after external edits.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# InputSystem / Save Asset

Persist an `InputActionAsset` to its `.inputactions` file. The mutation tools save automatically; this tool is an explicit save for cases where the on-disk JSON should be regenerated (e.g. after edits made outside these tools).

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.

## Behavior

Loads the asset, re-serializes it to JSON via `ToJson()`, writes the file, and re-imports it into the AssetDatabase. Runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "assetPath": "string_value"
}
'@)
unity-mcp-cli.cmd run-tool inputsystem-save --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `assetPath` | `string` | Yes | 'Assets/'-rooted path to the existing '.inputactions' asset. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "assetPath": {
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

