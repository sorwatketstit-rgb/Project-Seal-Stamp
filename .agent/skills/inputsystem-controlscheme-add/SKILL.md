---
name: inputsystem-controlscheme-add
description: Add a Control Scheme (with optional required / optional device requirements) to a `.inputactions` asset, then save it.
metadata:
  generated-by: mcp-plugin-dotnet
---

# InputSystem / Add Control Scheme

Add an `InputControlScheme` to an InputActionAsset. A control scheme groups bindings by a set of devices (e.g. a 'Keyboard&Mouse' scheme and a 'Gamepad' scheme), letting the same Actions support multiple input setups.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `schemeName` — required. Unique control-scheme name.
- `requiredDevices` — optional device control paths that MUST be present (e.g. `<Gamepad>`).
- `optionalDevices` — optional device control paths that MAY be present (e.g. `<Mouse>`).

## Behavior

Loads the asset, adds the control scheme via `AddControlScheme`, registers each required / optional device requirement, saves and re-imports the asset. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool inputsystem-controlscheme-add --input '{
  "assetPath": "string_value",
  "schemeName": "string_value",
  "requiredDevices": [],
  "optionalDevices": []
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool inputsystem-controlscheme-add --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool inputsystem-controlscheme-add --input-file - <<'EOF'
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
| `schemeName` | `string` | Yes | Unique name of the new control scheme. |
| `requiredDevices` | `array` | No | Optional required device control paths (e.g. '<Gamepad>'). All must be present for the scheme to be usable. |
| `optionalDevices` | `array` | No | Optional optional-device control paths (e.g. '<Mouse>'). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "assetPath": {
      "type": "string"
    },
    "schemeName": {
      "type": "string"
    },
    "requiredDevices": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "optionalDevices": {
      "type": "array",
      "items": {
        "type": "string"
      }
    }
  },
  "required": [
    "assetPath",
    "schemeName"
  ]
}
```

## Output

This tool does not return structured output.

