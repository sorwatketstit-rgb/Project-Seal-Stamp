---
name: cinemachine-set-noise
description: Add or configure a `CinemachineBasicMultiChannelPerlin` (procedural camera shake / handheld noise) on a `CinemachineCamera`. Sets `AmplitudeGain` and `FrequencyGain`. The noise profile asset is optional and left untouched when not supplied.
metadata:
  generated-by: mcp-plugin-dotnet
---

# Cinemachine / Set Noise

Add or configure a `CinemachineBasicMultiChannelPerlin` noise component on a `CinemachineCamera`. This drives procedural Perlin-noise shake (handheld camera feel). The actual noise shape comes from a `NoiseSettings` profile asset; without one, the amplitude/frequency gains have no visible effect, so this tool is null-safe and does not require a profile.

## Inputs

- `gameObjectRef` — the GameObject hosting the `CinemachineCamera` (required).
- `amplitudeGain` — optional gain applied to the profile amplitudes (1 = normal).
- `frequencyGain` — optional gain applied to the profile frequencies (1 = normal).

## Behavior

Reuses an existing `CinemachineBasicMultiChannelPerlin` on the GameObject or adds one, then applies the provided gains. The noise profile asset is never modified here (leave it null-safe). Marks the scene dirty and repaints. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool cinemachine-set-noise --input '{
  "gameObjectRef": "string_value",
  "amplitudeGain": 0,
  "frequencyGain": 0
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool cinemachine-set-noise --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool cinemachine-set-noise --input-file - <<'EOF'
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
| `amplitudeGain` | `number` | No | Gain applied to the noise profile amplitudes (1 = normal). |
| `frequencyGain` | `number` | No | Gain applied to the noise profile frequencies (1 = normal). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "type": "string"
    },
    "amplitudeGain": {
      "type": "number"
    },
    "frequencyGain": {
      "type": "number"
    }
  },
  "required": [
    "gameObjectRef"
  ]
}
```

## Output

This tool does not return structured output.

