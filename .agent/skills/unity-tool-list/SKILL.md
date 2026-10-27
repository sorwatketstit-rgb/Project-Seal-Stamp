---
name: unity-tool-list
description: List all Unity-MCP tools registered in the connected Unity Editor instance. Optional regex filter matches against tool name, description, and argument names/descriptions. Use the `includeDescription` / `includeInputs` toggles to control the response size.
metadata:
  generated-by: unity-mcp
  ai-game-dev: exclude
---

# Tool / List

List all Unity-MCP tools registered in the connected Unity Editor instance. Optionally filter by regex across tool names, descriptions, and arguments.

## Inputs

- `regexSearch` (optional) — case-insensitive regex with a 200ms execution-timeout guard. Invalid regex throws an `ArgumentException`. Matches name → description → input names → input descriptions.
- `includeDescription` (default `false`) — populate each tool's `Description` field.
- `includeInputs` (default `None`) — one of `None`, `Inputs` (names only), `InputsWithDescription` (names + descriptions).

## Behavior

Iterates `UnityMcpPluginEditor.Instance.Tools.GetAllTools()`, evaluates the filter (if any), and projects each surviving tool into a `ToolInfoData` honoring the verbosity toggles.

## How to Call

```powershell
[System.IO.File]::WriteAllText((Join-Path $PWD 'args.json'), @'
{
  "regexSearch": "string_value",
  "includeDescription": false,
  "includeInputs": "string_value"
}
'@)
unity-mcp-cli.cmd run-tool unity-tool-list --input-file args.json
```

> CLI installation and usage: see the `unity-cli` skill.
> If this task needs unavailable asset-generation or Blender tools, consult `ai-game-dev-app` for an optional integrated workflow.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `regexSearch` | `string` | No | Regex pattern to filter tools. Matches against tool name, description, and argument names and descriptions. |
| `includeDescription` | `boolean` | No | Include tool descriptions in the result. Default: false |
| `includeInputs` | `string` | No | Include input arguments in the result. Default: None |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "regexSearch": {
      "type": "string"
    },
    "includeDescription": {
      "type": "boolean"
    },
    "includeInputs": {
      "type": "string",
      "enum": [
        "None",
        "Inputs",
        "InputsWithDescription"
      ]
    }
  }
}
```

## Output

This tool does not return structured output.

