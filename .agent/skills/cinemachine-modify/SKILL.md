---
name: cinemachine-modify
description: "Generic write: apply a `SerializedMember` diff to any Cinemachine `Component` on a GameObject via ReflectorNet `TryModify`. Use 'cinemachine-get' first to inspect the structure so the diff is targeted."
metadata:
  generated-by: mcp-plugin-dotnet
---

# Cinemachine / Modify Component

Modify any Cinemachine component on a GameObject by applying a `SerializedMember` diff via ReflectorNet. This is the generic escape hatch for fields not covered by the dedicated tools.

## Inputs

- `gameObjectRef` — the GameObject hosting the component (required).
- `componentRef` — optional. Resolves a specific component when the GameObject has more than one Cinemachine component; otherwise the first component in the `Unity.Cinemachine` namespace is used.
- `data` — the `SerializedMember` diff to apply. Include only the properties you want to change.

## Behavior

Finds the target Cinemachine component, applies the diff via `Reflector.TryModify`, and on success marks the component + scene dirty and repaints. The applied logs are returned. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool cinemachine-modify --input '{
  "gameObjectRef": "string_value",
  "data": "string_value",
  "componentRef": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool cinemachine-modify --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool cinemachine-modify --input-file - <<'EOF'
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
| `data` | `string` | Yes | The SerializedMember diff to apply. Only include properties you want to change. RQL: SerializedMember |
| `componentRef` | `string` | No | Optional reference to a specific Cinemachine component if the GameObject has multiple. RQL: ComponentRef |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "type": "string"
    },
    "data": {
      "type": "string"
    },
    "componentRef": {
      "type": "string"
    }
  },
  "required": [
    "gameObjectRef",
    "data"
  ]
}
```

## Output

This tool does not return structured output.

