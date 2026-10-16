---
name: cinemachine-camera-list
description: List every `CinemachineCamera` in the active scene with its name, priority, and whether it is currently the live (active) camera according to an active `CinemachineBrain`. Read-only.
metadata:
  generated-by: mcp-plugin-dotnet
---

# Cinemachine / List Cameras

List every `CinemachineCamera` in the active scene. For each, returns the name, the priority value, a reference, and whether the camera is currently live (driving the view through an active `CinemachineBrain`).

## Inputs

- `includeInactive` (bool, default true) — include inactive/disabled CinemachineCameras.

## Behavior

Finds all `CinemachineCamera` instances (including inactive ones), reads each `Priority.Value`, and compares against `CinemachineBrain.ActiveVirtualCamera` across all active brains to compute `isLive`. Read-only. The whole call runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool cinemachine-camera-list --input '{
  "includeInactive": false
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool cinemachine-camera-list --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool cinemachine-camera-list --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `includeInactive` | `boolean` | No | If true (default), include inactive/disabled CinemachineCameras; if false, only those on active GameObjects. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "includeInactive": {
      "type": "boolean"
    }
  }
}
```

## Output

This tool does not return structured output.

