---
name: cinemachine-set-noise
description: Add or configure a `CinemachineBasicMultiChannelPerlin` (procedural camera shake / handheld noise) on a `CinemachineCamera`. Sets `AmplitudeGain` and `FrequencyGain`. The noise profile asset is optional and left untouched when not supplied.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
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

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "gameObjectRef": "string_value",
  "amplitudeGain": 0,
  "frequencyGain": 0
}
'@)
unity-mcp-cli.cmd run-tool cinemachine-set-noise --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

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

