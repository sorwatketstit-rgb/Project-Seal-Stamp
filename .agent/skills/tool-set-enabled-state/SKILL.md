---
name: tool-set-enabled-state
description: Enable or disable MCP tools by name in batch. Persists the change via `UnityMcpPluginEditor.Instance.Save()` only when at least one tool actually flipped. Returns per-input success flags plus optional operation logs.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# Tool / Set Enabled State

Enable or disable MCP tools by name. Allows controlling which tools are available for the AI agent.

## Inputs

- `tools` — array of `ToolToggleInput { Name, Enabled }`. Non-empty.
- `includeLogs` (default `false`) — when true, returns per-step operation logs alongside the success map.

## Behavior

Each entry is resolved against the tool manager's exact-name and case-insensitive lookups. Already-correct state short-circuits as success without writing. The plugin's config is saved once at the end iff at least one tool actually changed state.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "tools": "string_value",
  "includeLogs": false
}
'@)
unity-mcp-cli.cmd run-tool tool-set-enabled-state --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `tools` | `string` | Yes | Array of tools with their desired enabled state. RQL: [ToolToggleInput] |
| `includeLogs` | `boolean` | No | Include operation logs in the result. Default: false |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "tools": {
      "type": "string"
    },
    "includeLogs": {
      "type": "boolean"
    }
  },
  "required": [
    "tools"
  ]
}
```

## Output

This tool does not return structured output.

