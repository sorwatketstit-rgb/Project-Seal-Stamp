---
name: cinemachine-set-body
description: Add or replace the position-control (Body) component of a `CinemachineCamera`, chosen by `BodyType` (Follow, OrbitalFollow, ThirdPersonFollow, PositionComposer, HardLockToTarget, or None). Applies common params (followOffset, damping, cameraDistance) where applicable.
metadata:
  generated-by: mcp-plugin-dotnet
---

# Cinemachine / Set Body

Add or replace the Body (position-control) component of a `CinemachineCamera`. Any existing Body component is removed first, then the chosen one is added — so this is destructive to the previous Body component.

## Inputs

- `gameObjectRef` — the GameObject hosting the `CinemachineCamera` (required).
- `bodyType` — `BodyType` enum: `Follow`, `OrbitalFollow`, `ThirdPersonFollow`, `PositionComposer`, `HardLockToTarget`, or `None` (removes the Body component).
- `followOffset` — optional `Vector3` offset from the Follow target (applies to `Follow`).
- `damping` — optional uniform damping value (applied where the component supports damping).
- `cameraDistance` — optional distance from the target (applies to `ThirdPersonFollow` / `PositionComposer` / `OrbitalFollow` radius).

## Behavior

Removes every existing `CinemachineFollow`, `CinemachineOrbitalFollow`, `CinemachineThirdPersonFollow`, `CinemachinePositionComposer`, and `CinemachineHardLockToTarget` on the GameObject, then adds the requested one and applies the optional params. Marks the scene dirty and repaints. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool cinemachine-set-body --input '{
  "gameObjectRef": "string_value",
  "bodyType": "string_value",
  "followOffset": "string_value",
  "damping": 0,
  "cameraDistance": 0
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool cinemachine-set-body --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool cinemachine-set-body --input-file - <<'EOF'
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
| `bodyType` | `string` | Yes | Which position-control (Body) component to use. |
| `followOffset` | `string` | No | Offset from the Follow target (applies to Follow). RQL: Vector3 |
| `damping` | `number` | No | Uniform damping value applied where the component supports damping. |
| `cameraDistance` | `number` | No | Distance from the target (ThirdPersonFollow/PositionComposer/OrbitalFollow radius). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "type": "string"
    },
    "bodyType": {
      "type": "string",
      "enum": [
        "None",
        "Follow",
        "OrbitalFollow",
        "ThirdPersonFollow",
        "PositionComposer",
        "HardLockToTarget"
      ]
    },
    "followOffset": {
      "type": "string"
    },
    "damping": {
      "type": "number"
    },
    "cameraDistance": {
      "type": "number"
    }
  },
  "required": [
    "gameObjectRef",
    "bodyType"
  ]
}
```

## Output

This tool does not return structured output.

