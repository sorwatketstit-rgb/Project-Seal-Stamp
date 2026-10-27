---
name: inputsystem-binding-add
description: Add a simple (non-composite) Binding to an Action in a `.inputactions` asset, with an optional control path, groups, interactions and processors, then save it.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# InputSystem / Add Binding

Add an `InputBinding` to an existing `InputAction`. A binding maps a physical control path (e.g. `<Keyboard>/space`) to the Action. For composite bindings (2DVector / 1DAxis) use 'inputsystem-binding-composite-add' instead.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `mapName` — required. ActionMap containing the Action.
- `actionName` — required. Action to add the binding to.
- `path` — control path (e.g. `<Keyboard>/space`, `<Gamepad>/leftStick`).
- `groups` — optional control-scheme group(s) (semicolon-separated, e.g. `Keyboard&Mouse`).
- `interactions` — optional interactions (e.g. `hold`, `press,tap`).
- `processors` — optional processors (e.g. `invert`, `scale(factor=2)`).

## Behavior

Loads the asset, resolves the Action, appends a binding with the supplied metadata, saves and re-imports the asset, and returns the new binding's index. Runs on the Unity main thread.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "assetPath": "string_value",
  "mapName": "string_value",
  "actionName": "string_value",
  "path": "string_value",
  "groups": "string_value",
  "interactions": "string_value",
  "processors": "string_value"
}
'@)
unity-mcp-cli.cmd run-tool inputsystem-binding-add --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `assetPath` | `string` | Yes | 'Assets/'-rooted path to the existing '.inputactions' asset. |
| `mapName` | `string` | Yes | Name of the ActionMap containing the Action. |
| `actionName` | `string` | Yes | Name of the Action to add the binding to. |
| `path` | `string` | Yes | Control path for the binding (e.g. '<Keyboard>/space'). |
| `groups` | `string` | No | Optional control-scheme group(s), semicolon-separated. |
| `interactions` | `string` | No | Optional interactions (e.g. 'hold', 'press,tap'). |
| `processors` | `string` | No | Optional processors (e.g. 'invert'). |

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
    "path"
  ]
}
```

## Output

This tool does not return structured output.

