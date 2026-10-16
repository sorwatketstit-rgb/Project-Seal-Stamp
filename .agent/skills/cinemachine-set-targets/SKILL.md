---
name: cinemachine-set-targets
description: Set the `Follow` (position) and/or `LookAt` (aim) targets of a `CinemachineCamera`. Pass `clearFollow` / `clearLookAt` to explicitly remove a target instead of setting one.
metadata:
  generated-by: mcp-plugin-dotnet
---

# Cinemachine / Set Targets

Set the `Follow` and/or `LookAt` targets of a `CinemachineCamera`. The Follow target drives the position-control (Body) component; the LookAt target drives the rotation-control (Aim) component.

## Inputs

- `gameObjectRef` — the GameObject hosting the `CinemachineCamera` (required).
- `followRef` — optional GameObject to set as the Follow target.
- `lookAtRef` — optional GameObject to set as the LookAt target.
- `clearFollow` — when `true`, sets Follow to null (ignored if `followRef` is provided).
- `clearLookAt` — when `true`, sets LookAt to null (ignored if `lookAtRef` is provided).

## Behavior

Only the targets you specify are changed; omitted targets are left untouched unless the matching `clear*` flag is set. Marks the scene dirty and repaints the Editor. The whole call runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool cinemachine-set-targets --input '{
  "gameObjectRef": "string_value",
  "followRef": "string_value",
  "lookAtRef": "string_value",
  "clearFollow": false,
  "clearLookAt": false
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool cinemachine-set-targets --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool cinemachine-set-targets --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `string` | Yes | Reference to the GameObject containing the CinemachineCamera component. RQL: GameObjectRef |
| `followRef` | `string` | No | Optional GameObject to set as the Follow (position) target. RQL: GameObjectRef |
| `lookAtRef` | `string` | No | Optional GameObject to set as the LookAt (aim) target. RQL: GameObjectRef |
| `clearFollow` | `boolean` | No | If true, clears the Follow target (ignored when followRef is provided). |
| `clearLookAt` | `boolean` | No | If true, clears the LookAt target (ignored when lookAtRef is provided). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "type": "string"
    },
    "followRef": {
      "type": "string"
    },
    "lookAtRef": {
      "type": "string"
    },
    "clearFollow": {
      "type": "boolean"
    },
    "clearLookAt": {
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

