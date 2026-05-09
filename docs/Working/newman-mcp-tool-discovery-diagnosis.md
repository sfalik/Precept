# MCP Tool Discovery & Stdout Log Pollution — Diagnosis

**Date:** 2026-05-09  
**Author:** Newman  
**Status:** Diagnosis complete. Awaiting Shane's authorization to implement fixes.

---

## Issue 1: Only 3 Tools Discovered (Expected 5)

### Observed Symptom

VS Code MCP logs report: `Discovered 3 tools`

The expected 5 production tools are:
`precept_language`, `precept_compile`, `precept_inspect`, `precept_fire`, `precept_update`

### File Evidence

`tools/Precept.Mcp/Tools/` contains exactly **3 files**:

| File | Tool Name | `[McpServerToolType]` | `[McpServerTool]` |
|------|-----------|----------------------|-------------------|
| `LanguageTool.cs` | `precept_language` | ✅ | ✅ |
| `CompileTool.cs` | `precept_compile` | ✅ | ✅ |
| `PingTool.cs` | `precept_ping` | ✅ | ✅ |

**Missing tool files (do not exist):**

| Expected Tool | File | Status |
|---------------|------|--------|
| `precept_inspect` | `InspectTool.cs` | ❌ Not created |
| `precept_fire` | `FireTool.cs` | ❌ Not created |
| `precept_update` | `UpdateTool.cs` | ❌ Not created |

### Root Cause

`Program.cs` uses `WithToolsFromAssembly()`, which auto-discovers all classes decorated with `[McpServerToolType]`. The discovery mechanism is correct and complete — it simply has nothing to find for the three unimplemented tools. **The three tools were never implemented.** No conditional registration, no build artifact issue, no suppression — they are absent from the codebase.

This is corroborated by the test suite: `test/Precept.Mcp.Tests/` contains only `CompileToolTests.cs` and `LanguageToolTests.cs`. There are no test files for `InspectTool`, `FireTool`, or `UpdateTool`, consistent with those tools never having been written.

The note in `history.md` ("The 5 non-ping MCP tools have zero implementation") was recorded when all 5 were missing. Since then, `precept_language` and `precept_compile` shipped. The remaining three were never picked up.

VS Code's "3 tools discovered" = all 3 that exist (`precept_ping`, `precept_language`, `precept_compile`). Nothing is broken in discovery or registration — the tools simply don't exist yet.

### Proposed Fix

Implement the three missing tool classes:

1. **`tools/Precept.Mcp/Tools/InspectTool.cs`**  
   Decorated with `[McpServerToolType]` / `[McpServerTool(Name = "precept_inspect")]`.  
   Wraps the runtime's read-only inspection API — given a precept text, current state, and field data, returns what each event would do without firing it. See `docs/McpServerDesign.md § precept_inspect` for the specified response shape.

2. **`tools/Precept.Mcp/Tools/FireTool.cs`**  
   Decorated with `[McpServerToolType]` / `[McpServerTool(Name = "precept_fire")]`.  
   Wraps the single-event fire execution path — given precept text, current state, event name, and optional data/args, executes one event and returns the outcome. See `docs/McpServerDesign.md § precept_fire`.

3. **`tools/Precept.Mcp/Tools/UpdateTool.cs`**  
   Decorated with `[McpServerToolType]` / `[McpServerTool(Name = "precept_update")]`.  
   Wraps the direct field-update path — given precept text, current state, field data, and a field map, applies edits and validates constraints. See `docs/McpServerDesign.md § precept_update`.

Each tool will need corresponding DTOs in `tools/Precept.Mcp/Dtos/`. Implementation is non-trivial (requires wiring to the runtime API) and requires design review.

**Design approval required:** Yes — these are new tool surfaces.

---

## Issue 2: Log Lines Leaking to stdout ("Failed to parse message")

### Observed Symptom

VS Code MCP logs show:
```
[warning] Failed to parse message: "info: ModelContextProtocol.Server.McpServer[1867955179]\r\n"
[warning] Failed to parse message: "      Server (Precept.Mcp 1.0.0.0), Client (Visual Studio Code 1.119.0)"
```

These are Microsoft.Extensions.Logging console output lines landing on stdout, which VS Code then fails to parse as JSON-RPC.

### Root Cause

`Program.cs` calls `Host.CreateApplicationBuilder(args)`. The `CreateApplicationBuilder` default registers the console logging provider, which writes to **stdout** by default. The MCP stdio transport exclusively reserves stdout as the JSON-RPC channel — any non-JSON bytes on stdout corrupt the protocol stream.

The log lines originate inside the MCP SDK itself (`McpServer` emitting the `Server (...), Client (...)` connection banner at `Information` level). They are legitimate informational logs; the problem is their routing to the wrong stream.

### File Evidence

`tools/Precept.Mcp/Program.cs` (entire file):
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
```

No explicit logging configuration is present. The default `CreateApplicationBuilder` wires up the console logger with a default minimum level of `Information`, writing to stdout.

### Proposed Fix

Two options, either is trivially correct:

**Option A — Redirect console logger to stderr (preferred, preserves diagnostic visibility):**

```csharp
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
```

This routes all console log output to stderr, leaving stdout clean for JSON-RPC. Operators who watch stderr (e.g., VS Code's MCP debug panel) still see the logs.

**Option B — Suppress to Warning threshold (simpler, loses info logs):**

```csharp
builder.Logging.SetMinimumLevel(LogLevel.Warning);
```

This prevents the `Information`-level connection banner from being emitted at all. Warning and above would still appear (on stdout, which is still wrong). A complete fix for Option B would also require clearing default providers or using Option A's stderr redirect.

**Recommendation:** Option A. It preserves diagnostic visibility without polluting the protocol channel. Option B is incomplete — suppressing level doesn't fix the stdout routing for warning/error logs that may appear later.

**Design approval required:** No — this is a trivial bug fix. Option A is a one-line addition to `Program.cs` and carries no design risk. It is consistent with every MCP stdio server implementation. Authorized to implement immediately upon Shane's say-so.

---

## Summary Table

| Issue | Root Cause | Trivial Fix? | Requires Design? |
|-------|-----------|--------------|-----------------|
| 3 of 5 tools missing | `InspectTool`, `FireTool`, `UpdateTool` never implemented | No | Yes |
| stdout log pollution | `CreateApplicationBuilder` default console logger writes to stdout; MCP stdio needs stdout clean | Yes (one line) | No |
