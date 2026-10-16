---
name: cinemachine-get
description: "Generic read: serialize any Cinemachine `Component` (a type in the `Unity.Cinemachine` namespace) on a GameObject via ReflectorNet. Pair with 'cinemachine-modify' to write changes back. Read-only."
metadata:
  generated-by: mcp-plugin-dotnet
---

# Cinemachine / Get Component

Serialize any Cinemachine component on a GameObject using ReflectorNet. This is the generic escape hatch for fields not covered by the dedicated tools (e.g. a specific extension's settings).

## Inputs

- `gameObjectRef` — the GameObject hosting the component (required).
- `componentRef` — optional. Resolves a specific component when the GameObject has more than one Cinemachine component; otherwise the first component whose type lives in the `Unity.Cinemachine` namespace is used.
- `deepSerialization` — when `true`, recurses through nested objects; otherwise only top-level members.

## Behavior

Finds the target Cinemachine component, serializes it via ReflectorNet, and returns the serialized member plus the resolved component type name. Read-only. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool cinemachine-get --input '{
  "gameObjectRef": "string_value",
  "componentRef": "string_value",
  "deepSerialization": false
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool cinemachine-get --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool cinemachine-get --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `string` | Yes | Reference to the GameObject containing the Cinemachine component. RQL: GameObjectRef |
| `componentRef` | `string` | No | Optional reference to a specific Cinemachine component if the GameObject has multiple. RQL: ComponentRef |
| `deepSerialization` | `boolean` | No | Performs deep serialization including nested objects. Otherwise only top-level members. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "type": "string"
    },
    "componentRef": {
      "type": "string"
    },
    "deepSerialization": {
      "type": "boolean"
    }
  },
  "required": [
    "gameObjectRef"
  ]
}
```

## Output

This tool does not return structured output.

