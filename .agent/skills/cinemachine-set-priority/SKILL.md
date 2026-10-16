---
name: cinemachine-set-priority
description: Set the `Priority.Value` of a `CinemachineCamera`. The highest-priority active camera wins control of the `CinemachineBrain`.
metadata:
  generated-by: mcp-plugin-dotnet
---

# Cinemachine / Set Priority

Set the priority of a `CinemachineCamera`. When multiple CinemachineCameras are active, the one with the highest priority becomes live (drives the rendered view through the `CinemachineBrain`).

## Inputs

- `gameObjectRef` — the GameObject hosting the `CinemachineCamera` (required).
- `priority` — the new integer priority value.

## Behavior

Assigns `CinemachineCamera.Priority` with the given value, marks the scene dirty, and repaints the Editor. The whole call runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool cinemachine-set-priority --input '{
  "gameObjectRef": "string_value",
  "priority": 0
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool cinemachine-set-priority --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool cinemachine-set-priority --input-file - <<'EOF'
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
| `priority` | `integer` | Yes | The new priority value. Higher wins when multiple cameras are active. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "type": "string"
    },
    "priority": {
      "type": "integer"
    }
  },
  "required": [
    "gameObjectRef",
    "priority"
  ]
}
```

## Output

This tool does not return structured output.

