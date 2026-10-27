---
name: inputsystem-get
description: Read the full structure of a `.inputactions` InputActionAsset — its ActionMaps, Actions (type / expectedControlType), Bindings (path / groups / interactions / processors / index) and Control Schemes. Read-only.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# InputSystem / Get Asset Structure

Inspect an `InputActionAsset` and return its structure so you can discover map names, action names, and binding indices to drive the other tools.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.

## Behavior

Loads the asset and walks its ActionMaps → Actions → Bindings plus its Control Schemes, returning a compact structured summary (including each binding's zero-based index, whether it is a composite, and its part metadata). Read-only. Runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "assetPath": "string_value"
}
'@)
unity-mcp-cli.cmd run-tool inputsystem-get --input-file args.json
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

