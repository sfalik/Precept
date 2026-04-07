# Precept

[![NuGet](https://img.shields.io/nuget/v/Precept)](https://www.nuget.org/packages/Precept)
[![MIT License](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

> **pre·cept** *(noun)*: A general rule intended to regulate behavior or thought.

**Precept is a domain integrity engine for .NET.** By treating business constraints as unbreakable precepts, it binds state machines, validation, and business rules into a single executable contract where invalid states are structurally impossible.

---

## Quick Example

> Temporary hero sample — using the current top-rated Subscription Billing candidate while the final hero decision stays open.

![Precept Subscription hero](design/brand/readme-hero.svg)

**The Contract**

GitHub cannot render the styled DSL treatment faithfully, so the README uses the rendered contract and keeps the source copyable below.

![Rendered Precept Subscription contract](design/brand/readme-hero-dsl.png)

<details>
<summary>Copyable DSL</summary>

```text
precept Subscription

field PlanName as string nullable
field MonthlyPrice as number default 0

invariant MonthlyPrice >= 0 because "Monthly price cannot be negative"

state Trial initial, Active, Cancelled

event Activate with Plan as string, Price as number
on Activate assert Price > 0 because "Plan price must be positive"

event Cancel

from Trial on Activate when PlanName == null
  -> set PlanName = Activate.Plan
  -> set MonthlyPrice = Activate.Price
  -> transition Active
from Active on Activate -> set MonthlyPrice = Activate.Price -> no transition
from Active on Cancel -> transition Cancelled
from Cancelled on Activate -> reject "Cancelled subscriptions cannot be reactivated"
```
</details>

**The Execution**

```csharp
var def = PreceptParser.Parse(dslText);
var eng = PreceptCompiler.Compile(def);
var inst = eng.CreateInstance(state, data);
var result = eng.Fire(inst, "Activate", args);
// result.IsSuccess, result.UpdatedInstance
```

---

## Getting Started

**Prerequisite:** [.NET 10 SDK](https://dotnet.microsoft.com/download)

### 1. Install the VS Code Extension

```bash
code --install-extension sfalik.precept-vscode
```

Syntax highlighting and live diagnostics are active immediately.

### 2. Create Your First Precept File

Create `Subscription.precept` and type along with the temporary example above. The language server provides completions, hover docs, and error detection in real time.

### 3. Add the NuGet Package

```bash
dotnet add package Precept
```

See the [Quickstart Guide](docs/RuntimeApiDesign.md) for a complete runtime integration walkthrough.

---

## What Makes Precept Different

**AI-Native Tooling** — MCP server with 5 core tools, GitHub Copilot plugin, and language server give AI agents structured access to validate, inspect, and iterate on `.precept` files.

**Unified Domain Integrity** — State machines, validators, and rules engines often disagree when split across libraries. Precept unifies them into one definition.

- Prevention, not detection — invalid states are structurally impossible
- One file, all rules — guards, constraints, invariants, and transitions together
- Full inspectability — preview any action's outcome without executing it
- Compile-time checking — unreachable states and type errors caught before runtime

**Live Editor Experience** — Completions, semantic highlighting, inline diagnostics, and a live state diagram preview in VS Code.

---

## Learn More

| Resource | Description |
|----------|-------------|
| [Language Reference](docs/PreceptLanguageDesign.md) | Full DSL syntax and construct reference |
| [Sample Catalog](samples/) | 20+ domain models in `.precept` |
| [Quickstart Guide](docs/RuntimeApiDesign.md) | Step-by-step integration walkthrough |
| [MCP Server Docs](docs/McpServerDesign.md) | Tool reference for AI agent integration |

---

## Contributing

Precept is built with .NET 10.0 and TypeScript.

```bash
dotnet build            # Build everything
dotnet test             # Run all tests
```

First-time local setup in a new clone:

1. Run task `build`.
2. Run task `extension: install`, then reload the window.

If you previously used an older local plugin-registration flow, remove any stale `chat.pluginLocations` entry that points at `tools/Precept.Plugin/`. The current local model uses workspace-native `.github/agents/`, `.github/skills/`, and `.vscode/mcp.json` instead.

| What you changed | Command | Reload? |
|------------------|---------|---------|
| C# runtime or language server | `Ctrl+Shift+B` | No |
| TypeScript, webview, or syntax | Task: `extension: install` | Yes |
| Agent or skill markdown in `.github/agents/` or `.github/skills/` | Reload Window | Yes |

See [ArtifactOperatingModelDesign.md](docs/ArtifactOperatingModelDesign.md) for the local-vs-distribution operating model, worktree rules, the workspace `.vscode/mcp.json` `servers` schema, and the plugin payload sync boundary.

---

## License

MIT — see [LICENSE](LICENSE) for details.
