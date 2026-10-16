---
name: inputsystem-modify
description: "Generic write escape-hatch: apply a `SerializedMember` diff to the `InputActionAsset` object itself via ReflectorNet `TryModify`, for fields not covered by the dedicated tools. Use 'inputsystem-get' first to inspect the structure."
metadata:
  generated-by: mcp-plugin-dotnet
---

# InputSystem / Modify Asset (generic)

Modify an `InputActionAsset` by applying a `SerializedMember` diff via ReflectorNet. This is the generic escape hatch for asset-level fields/properties that the dedicated tools do not expose.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `data` — the `SerializedMember` diff to apply. Route **fields** through the `fields` channel and **properties** through the `props` channel — ReflectorNet resolves them separately with no cross-fallback.

## Behavior

Loads the asset, applies the diff via `Reflector.TryModify`, and on success re-serializes and saves the asset back to disk. The applied logs are returned. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool inputsystem-modify --input '{
  "assetPath": "string_value",
  "data": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool inputsystem-modify --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool inputsystem-modify --input-file - <<'EOF'
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

