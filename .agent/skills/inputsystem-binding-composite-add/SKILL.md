---
name: inputsystem-binding-composite-add
description: Add a composite Binding (e.g. `2DVector` / WASD, `1DAxis`) with its named parts to an Action in a `.inputactions` asset, then save it.
metadata:
  generated-by: mcp-plugin-dotnet
---

# InputSystem / Add Composite Binding

Add a composite `InputBinding` to an `InputAction`. A composite synthesizes a value (e.g. a `Vector2`) from several part bindings. The classic example is a `2DVector` (WASD) composite with `up`/`down`/`left`/`right` parts; a `1DAxis` composite has `negative`/`positive` parts.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `mapName` / `actionName` — required. The Action that receives the composite.
- `composite` — composite type, e.g. `2DVector`, `1DAxis`, `Axis`, `Dpad` (default `2DVector`).
- `parts` — required. One or more `{ name, path }` entries (e.g. `up` → `<Keyboard>/w`).
- `interactions` / `processors` — optional, applied to the composite root.

## Behavior

Loads the asset, resolves the Action, adds the composite via `AddCompositeBinding` and chains each part via `.With(name, path)`, saves and re-imports the asset. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool inputsystem-binding-composite-add --input '{
  "assetPath": "string_value",
  "mapName": "string_value",
  "actionName": "string_value",
  "parts": "string_value",
  "composite": "string_value",
  "interactions": "string_value",
  "processors": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool inputsystem-binding-composite-add --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool inputsystem-binding-composite-add --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `assetPath` | `string` | Yes | 'Assets/'-rooted path to the existing '.inputactions' asset. |
| `mapName` | `string` | Yes | Name of the ActionMap containing the Action. |
| `actionName` | `string` | Yes | Name of the Action to add the composite to. |
| `parts` | `string` | Yes | Composite parts: each entry has a 'name' (e.g. 'up') and a control 'path' (e.g. '<Keyboard>/w'). RQL: [CompositePart] |
| `composite` | `string` | No | Composite type, e.g. '2DVector' (default), '1DAxis', 'Axis', 'Dpad'. |
| `interactions` | `string` | No | Optional interactions applied to the composite root. |
| `processors` | `string` | No | Optional processors applied to the composite root. |

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
    "parts": {
      "type": "string"
    },
    "composite": {
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
    "parts"
  ]
}
```

## Output

This tool does not return structured output.

