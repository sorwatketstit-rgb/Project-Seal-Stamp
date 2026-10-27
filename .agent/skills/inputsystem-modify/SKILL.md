---
name: inputsystem-modify
description: "Generic write escape-hatch: apply a `SerializedMember` diff to the `InputActionAsset` object itself via ReflectorNet `TryModify`, for fields not covered by the dedicated tools. Use 'inputsystem-get' first to inspect the structure."
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# InputSystem / Modify Asset (generic)

Modify an `InputActionAsset` by applying a `SerializedMember` diff via ReflectorNet. This is the generic escape hatch for asset-level fields/properties that the dedicated tools do not expose.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `data` — the `SerializedMember` diff to apply. Route **fields** through the `fields` channel and **properties** through the `props` channel — ReflectorNet resolves them separately with no cross-fallback.

## Behavior

Loads the asset, applies the diff via `Reflector.TryModify`, and on success re-serializes and saves the asset back to disk. The applied logs are returned. Runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "assetPath": "string_value",
  "data": "string_value"
}
'@)
unity-mcp-cli.cmd run-tool inputsystem-modify --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `assetPath` | `string` | Yes | 'Assets/'-rooted path to the existing '.inputactions' asset. |
| `data` | `string` | Yes | The SerializedMember diff to apply. Fields go through the 'fields' channel; properties through 'props'. RQL: SerializedMember |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "assetPath": {
      "type": "string"
    },
    "data": {
      "type": "string"
    }
  },
  "required": [
    "assetPath",
    "data"
  ]
}
```

## Output

This tool does not return structured output.

