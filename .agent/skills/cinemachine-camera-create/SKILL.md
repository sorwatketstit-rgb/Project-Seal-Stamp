---
name: cinemachine-camera-create
description: Create a new GameObject with a `CinemachineCamera` (Cinemachine 3.x virtual camera) in the active scene. Optionally set name, position, rotation, priority, and Follow / LookAt targets. Returns the new GameObject reference and instanceId.
metadata:
  generated-by: mcp-plugin-dotnet
---

# Cinemachine / Create Camera

Create a new GameObject hosting a `CinemachineCamera` component in the active scene. The `CinemachineCamera` is the Cinemachine 3.x virtual camera that a `CinemachineBrain` blends between to drive the real Unity Camera.

## Inputs

- `name` — optional GameObject name (default `CinemachineCamera`).
- `position` — optional world `Vector3` position (default zero).
- `rotation` — optional euler-degrees `Vector3` rotation (default zero).
- `priority` — optional priority value; higher wins when multiple cameras compete (default 0).
- `followRef` — optional GameObject the camera should Follow (position target).
- `lookAtRef` — optional GameObject the camera should LookAt (aim target).

## Behavior

Creates the GameObject, adds a `CinemachineCamera`, sets transform / `Priority.Value` / `Follow` / `LookAt`, marks the scene dirty, repaints the Editor, and returns the new GameObject reference and instanceId. The whole call runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool cinemachine-camera-create --input '{
  "name": "string_value",
  "position": "string_value",
  "rotation": "string_value",
  "priority": 0,
  "followRef": "string_value",
  "lookAtRef": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool cinemachine-camera-create --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool cinemachine-camera-create --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `name` | `string` | No | Name of the new CinemachineCamera GameObject. |
| `position` | `string` | No | World-space position of the camera. RQL: Vector3 |
| `rotation` | `string` | No | World-space rotation of the camera in euler angles (degrees). RQL: Vector3 |
| `priority` | `integer` | No | Priority value. Higher priority wins when several CinemachineCameras are active. |
| `followRef` | `string` | No | Optional GameObject the camera should Follow (drives position). RQL: GameObjectRef |
| `lookAtRef` | `string` | No | Optional GameObject the camera should LookAt (drives aim). RQL: GameObjectRef |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "name": {
      "type": "string"
    },
    "position": {
      "type": "string"
    },
    "rotation": {
      "type": "string"
    },
    "priority": {
      "type": "integer"
    },
    "followRef": {
      "type": "string"
    },
    "lookAtRef": {
      "type": "string"
    }
  }
}
```

## Output

This tool does not return structured output.

