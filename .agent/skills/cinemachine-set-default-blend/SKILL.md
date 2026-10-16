---
name: cinemachine-set-default-blend
description: Set the `DefaultBlend` of a `CinemachineBrain` — the blend style (Cut, EaseInOut, Linear, etc.) and duration used when transitioning between CinemachineCameras. Targets the referenced Camera's brain, or `Camera.main`'s brain.
metadata:
  generated-by: mcp-plugin-dotnet
---

# Cinemachine / Set Default Blend

Set the default blend on a `CinemachineBrain`. The default blend is used whenever the brain transitions from one live CinemachineCamera to another and no custom blend overrides it.

## Inputs

- `cameraRef` — optional GameObject hosting the Camera + `CinemachineBrain`. When omitted, `Camera.main` is used.
- `style` — `CinemachineBlendDefinition.Styles` enum (`Cut`, `EaseInOut`, `EaseIn`, `EaseOut`, `HardIn`, `HardOut`, `Linear`, `Custom`).
- `time` — blend duration in seconds.

## Behavior

Resolves the brain (throws if the Camera has none), assigns a new `CinemachineBlendDefinition(style, time)` to `DefaultBlend`, marks the scene dirty, and repaints. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool cinemachine-set-default-blend --input '{
  "style": "string_value",
  "time": 0,
  "cameraRef": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool cinemachine-set-default-blend --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool cinemachine-set-default-blend --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `style` | `string` | Yes | Blend style used when transitioning between CinemachineCameras. |
| `time` | `number` | No | Blend duration in seconds. |
| `cameraRef` | `string` | No | Optional reference to the GameObject hosting the Camera + CinemachineBrain. If omitted, Camera.main is used. RQL: GameObjectRef |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "style": {
      "type": "string",
      "enum": [
        "Cut",
        "EaseInOut",
        "EaseIn",
        "EaseOut",
        "HardIn",
        "HardOut",
        "Linear",
        "Custom"
      ]
    },
    "time": {
      "type": "number"
    },
    "cameraRef": {
      "type": "string"
    }
  },
  "required": [
    "style"
  ]
}
```

## Output

This tool does not return structured output.

