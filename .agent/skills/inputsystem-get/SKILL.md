---
name: inputsystem-get
description: Read the full structure of a `.inputactions` InputActionAsset — its ActionMaps, Actions (type / expectedControlType), Bindings (path / groups / interactions / processors / index) and Control Schemes. Read-only.
metadata:
  generated-by: mcp-plugin-dotnet
---

# InputSystem / Get Asset Structure

Inspect an `InputActionAsset` and return its structure so you can discover map names, action names, and binding indices to drive the other tools.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.

## Behavior

Loads the asset and walks its ActionMaps → Actions → Bindings plus its Control Schemes, returning a compact structured summary (including each binding's zero-based index, whether it is a composite, and its part metadata). Read-only. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool inputsystem-get --input '{
  "assetPath": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool inputsystem-get --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool inputsystem-get --input-file - <<'EOF'
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

