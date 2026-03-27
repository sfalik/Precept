# Precept 🛡️

[![NuGet Badge](https://img.shields.io/nuget/v/Precept)](https://www.nuget.org/packages/Precept)
[![Build Status](https://img.shields.io/github/actions/workflow/status/OwnerName/Precept/build.yml?branch=main)](https://github.com/OwnerName/Precept/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![VS Code Extension](https://img.shields.io/visual-studio-marketplace/v/OwnerName.precept-vscode?label=VS%20Code)](https://marketplace.visualstudio.com/items?itemName=OwnerName.precept-vscode)

> **pre·​cept** *(noun)*: A general rule intended to regulate behavior or thought; a strict command or principle of action.

**Precept is a domain integrity engine for .NET.** It binds an entity's state, data, and business rules into a single, executable contract. By treating your business constraints as unbreakable *precepts*, the engine ensures that invalid states and illegal data mutations are fundamentally impossible.

---

## 🚀 Quick Start

1. **Install the .NET package:**
   ```bash
   dotnet add package Precept
   ```
2. **Install the VS Code extension:** Search for `Precept DSL` in the marketplace or run:
   ```bash
   code --install-extension AuthorName.precept-vscode
   ```
3. **Install the Copilot agent plugin** *(optional — requires `chat.plugins.enabled`)*: In VS Code, run `Chat: Install Plugin From Source` and provide the plugin repo URL. This adds the MCP tools, Precept Author agent, and companion skills to Copilot.

---

## 💡 The "Aha!" Moment

With Precept, you define the rules of your entity in a clean, domain-readable DSL, and then execute those exact rules deterministically in C#.

### 1. The Contract (`loan-application.precept`)

```
precept LoanApplication

# Fields with defaults and invariants
field ApplicantName as string nullable
field RequestedAmount as number default 0
field ApprovedAmount as number default 0
field CreditScore as number default 0
field AnnualIncome as number default 0
field ExistingDebt as number default 0
field DecisionNote as string nullable
field DocumentsVerified as boolean default false
invariant ApprovedAmount <= RequestedAmount because "Approved amount cannot exceed the request"

# State progression
state Draft initial
state UnderReview
state Approved
state Funded
state Declined

# Events with typed arguments and asserts
event Submit with Applicant as string, Amount as number, Score as number, Income as number, Debt as number
on Submit assert Applicant != "" because "An applicant name is required"
on Submit assert Amount > 0 because "Loan requests must be positive"

event VerifyDocuments

event Approve with Amount as number, Note as string nullable default null
on Approve assert Amount > 0 because "Approved amounts must be positive"

event Decline with Note as string

# Flat transition rows — first matching row wins
from Draft on Submit -> set ApplicantName = Submit.Applicant -> set RequestedAmount = Submit.Amount -> set CreditScore = Submit.Score -> set AnnualIncome = Submit.Income -> set ExistingDebt = Submit.Debt -> transition UnderReview
from UnderReview on VerifyDocuments -> set DocumentsVerified = true -> no transition
from UnderReview on Approve when DocumentsVerified && CreditScore >= 680 && AnnualIncome >= ExistingDebt * 2 && RequestedAmount < AnnualIncome / 2 && Approve.Amount <= RequestedAmount -> set ApprovedAmount = Approve.Amount -> set DecisionNote = Approve.Note -> transition Approved
from UnderReview on Approve -> reject "Approval requires verified documents, strong credit, and affordable debt load"
from UnderReview on Decline -> set DecisionNote = Decline.Note -> transition Declined
```

### 2. The Execution (C#)

Because guard expressions are purely evaluative, `Inspect` safely previews any action without touching your database.

```csharp
using Precept;

// Parse the DSL and compile to an engine (do this once at startup)
var definition = PreceptParser.Parse(File.ReadAllText("loan-application.precept"));
var engine = PreceptCompiler.Compile(definition);

// Restore an instance from your database
var instance = engine.CreateInstance(
    "UnderReview",
    new Dictionary<string, object?>
    {
        ["ApplicantName"] = "Jordan Lee",
        ["RequestedAmount"] = 50_000.0,
        ["CreditScore"] = 720.0,
        ["AnnualIncome"] = 140_000.0,
        ["ExistingDebt"] = 20_000.0,
        ["ApprovedAmount"] = 0.0,
        ["DocumentsVerified"] = true,
    });

// Safely inspect — no mutation
var preview = engine.Inspect(instance, "Approve", new Dictionary<string, object?>
{
    ["ApprovedAmount"] = 45_000.0
});

if (preview.Outcome == TransitionOutcome.Rejected)
{
    // Output: "Approval requires verified documents, strong credit, and affordable debt load"
    foreach (var v in preview.Violations)
        Console.WriteLine(v.Message);
}
else
{
    // Transition is valid — fire and persist
    var result = engine.Fire(instance, "Approve", new Dictionary<string, object?>
    {
        ["ApprovedAmount"] = 45_000.0
    });

    if (result.IsSuccess)
    {
        var updated = result.UpdatedInstance!;
        Console.WriteLine($"State: {updated.CurrentState}"); // Approved
        await repository.SaveAsync(updated.InstanceData);
    }
}

// Direct field editing — no event needed (fields declared with `in <State> edit`)
var editResult = engine.Update(instance, patch => patch
    .Set("Notes", "Customer called back"));

if (editResult.IsSuccess)
{
    instance = editResult.UpdatedInstance!;
    // Rules are enforced — same safety net as Fire
}
```

---

## 📚 Sample Catalog

The `samples/` directory now contains 18 fully commented workflows ordered from simple to complex. The first six are teaching-first samples, the middle six broaden feature usage, and the last six combine richer branching with more realistic review and operational rules.

| Sample | Complexity | Learning focus |
|--------|------------|----------------|
| `event-registration.precept` | Simple | Scalar fields, payment flow, direct edits |
| `library-hold-request.precept` | Simple | Queue promotion, stack history, entry and exit actions |
| `restaurant-waitlist.precept` | Simple | Queue routing, `peek`, `from any` |
| `parcel-locker-pickup.precept` | Simple | Timed guards, reminder log stack, `clear` |
| `building-access-badge-request.precept` | Simple | `set<number>`, `add`, `remove`, `contains`, `.min`, `.max` |
| `refund-request.precept` | Simple | Approval flow, nullable notes, guarded rejection |
| `maintenance-work-order.precept` | Medium | Urgency rules, from-state assertions, progress updates |
| `clinic-appointment-scheduling.precept` | Medium | Event-arg validation with `%`, scheduling edits |
| `travel-reimbursement.precept` | Medium | Arithmetic policy checks with `*` and `/` |
| `subscription-cancellation-retention.precept` | Medium | Retention offers, in-place review updates |
| `vehicle-service-appointment.precept` | Medium | Service recommendation set, to-state assertions |
| `it-helpdesk-ticket.precept` | Medium | Shared assignment queue, reopen loop |
| `apartment-rental-application.precept` | Complex | Approval screening, deposit and lease sequencing |
| `insurance-claim.precept` | Complex | Missing-document set management and review |
| `hiring-pipeline.precept` | Complex | Interview loop tracking and offer gating |
| `loan-application.precept` | Complex | Underwriting rules and affordability checks |
| `utility-outage-report.precept` | Complex | Area tracking, crew dispatch queue, incident closure |
| `warranty-repair-request.precept` | Complex | Repair step stack, `pop ... into`, return flow |

### Feature Coverage Matrix

The catalog is designed so the overall set exercises the full current language surface without forcing every feature into every file.

| Feature | Representative samples |
|---------|------------------------|
| Scalar fields, nullable fields, defaults | `event-registration.precept`, `refund-request.precept`, `loan-application.precept` |
| Global invariants | `event-registration.precept`, `travel-reimbursement.precept`, `loan-application.precept` |
| Event args with defaults and nullable defaults | `event-registration.precept`, `clinic-appointment-scheduling.precept`, `subscription-cancellation-retention.precept` |
| Event assertions | Present across all samples; see `clinic-appointment-scheduling.precept` and `travel-reimbursement.precept` |
| `when` guards and first-match branching | `refund-request.precept`, `parcel-locker-pickup.precept`, `apartment-rental-application.precept` |
| `transition`, `no transition`, and `reject` outcomes | Present across the catalog; see `event-registration.precept`, `refund-request.precept`, `loan-application.precept` |
| `in <State> assert` | `event-registration.precept`, `utility-outage-report.precept`, `warranty-repair-request.precept` |
| `to <State> assert` | `vehicle-service-appointment.precept`, `apartment-rental-application.precept` |
| `from <State> assert` | `maintenance-work-order.precept`, `vehicle-service-appointment.precept` |
| Entry and exit actions | `event-registration.precept`, `library-hold-request.precept`, `parcel-locker-pickup.precept`, `warranty-repair-request.precept` |
| Editable fields with `edit` | `event-registration.precept`, `building-access-badge-request.precept`, `insurance-claim.precept`, `utility-outage-report.precept` |
| Set collections with `add`, `remove`, `contains` | `building-access-badge-request.precept`, `insurance-claim.precept`, `vehicle-service-appointment.precept`, `hiring-pipeline.precept` |
| Queue collections with `enqueue`, `dequeue`, `peek` | `library-hold-request.precept`, `restaurant-waitlist.precept`, `it-helpdesk-ticket.precept`, `utility-outage-report.precept` |
| Stack collections with `push`, `pop`, `peek`, `clear` | `parcel-locker-pickup.precept`, `library-hold-request.precept`, `warranty-repair-request.precept` |
| Collection accessors `.count`, `.min`, `.max`, `.peek` | `building-access-badge-request.precept`, `restaurant-waitlist.precept`, `it-helpdesk-ticket.precept`, `parcel-locker-pickup.precept` |
| `from any on` rows | `restaurant-waitlist.precept`, `it-helpdesk-ticket.precept`, `utility-outage-report.precept` |
| Arithmetic operators `+`, `-`, `*`, `/`, `%` | `maintenance-work-order.precept`, `parcel-locker-pickup.precept`, `travel-reimbursement.precept`, `clinic-appointment-scheduling.precept` |
| Logical operators `&&`, `||`, `!` | `maintenance-work-order.precept`, `insurance-claim.precept`, `loan-application.precept` |

If you want to learn the language progressively, read the samples in the order listed above. If you want to see a specific feature in context, start with the matrix and jump straight to a representative file.

---

## 🛠️ World-Class Tooling

Precept isn't just a library; it's an authoring experience. The accompanying VS Code extension provides:
- **Context-Aware IntelliSense:** Completions respect DSL scope and the current grammar step, so declarations suggest the next required keywords and types, invariants and state asserts suggest data fields, event asserts suggest the active event's arguments, and guarded rows hand off to `->` once the guard is complete.
- **Hover + Go to Definition:** Hover a field, state, event, event arg, collection accessor, or DSL keyword to see its meaning and syntax form, then jump to declarations directly from references.
- **Document Outline:** The editor exposes a structured symbol tree for the precept, including fields, states, events, and event arguments.
- **Shared Type Diagnostics:** Expression typing, null-flow narrowing, collection mutation checks, and `from any` state-aware analysis now come from the core compiler, so the language server and MCP validation surface the same `PRECEPT038`–`PRECEPT047` errors. Equality requires same-family operands (`string == string` ✓, `string == number` ✗); non-nullable `== null` comparisons are rejected at compile time; `when` guards and `invariant`/`assert` positions require boolean expressions (`PRECEPT046`); and identical guards on the same `from X on Y` pair are caught as duplicates (`PRECEPT047`). Event args in transition rows must use the dotted form (`EventName.ArgName`); bare arg names are only valid in `on Event assert` positions. The MCP `precept_compile` result includes all diagnostic codes structurally.
- **Structural Diagnostics:** Real-time diagnostics now surface unreachable states, dead-end states, orphaned events, and reject-only state/event pairs that likely model unsupported events as explicit rejections.
- **Quick Fixes:** Reject-only state/event pair warnings offer a quick fix to remove the rows and restore `Undefined` semantics for unsupported events, and orphaned event warnings offer a quick fix to remove the unused event declaration and its event asserts.
- **Interactive Inspector:** Fire events and edit data against a live, mock instance in VS Code. The preview behaves like Markdown preview by default: one right-side preview follows the active `.precept` editor, and you can lock it to the current file with **Toggle Preview Locking**. Field edits use explicit **Edit** mode with **Save/Cancel**; validation runs live via inspect while typing, and field-level, event-arg, and event-level errors render inline beneath the controls they belong to. Values are committed only when **Save** is clicked.
- **Live Diagramming:** A dynamic state-transition diagram renders as you type.
- **Null-Flow Analysis:** Real-time squiggles warn you if a guard path might access an unsafe null value.

![Interactive Inspector](docs/images/inspector-preview.png)

---

## 🤖 MCP Server (Copilot Integration)

Precept includes an MCP (Model Context Protocol) server that exposes DSL parsing, validation, structural analysis, and runtime execution as tools callable by Copilot and any MCP-compatible host. This enables semantic understanding of `.precept` files beyond plain text reading.

### Tools

| Tool | Purpose |
|------|---------|
| `precept_language` | Full DSL reference — vocabulary, constructs, constraints, pipeline |
| `precept_compile` | Parse, type-check, analyze, and compile a precept definition; returns structure and diagnostics |
| `precept_inspect` | From a state+data snapshot, preview what every event would do and which fields are editable |
| `precept_fire` | Fire a single event, return the execution outcome |
| `precept_update` | Apply a direct field edit and return constraint violations |

### Agent Plugin

The MCP server, a custom **Precept Author** agent, and two companion skills (`precept-authoring`, `precept-debugging`) are distributed as a Copilot agent plugin — separate from the VS Code extension. Install the plugin from the marketplace or directly from the Git repository, and all five tools, the agent, and the skills are available in any workspace.

The VS Code extension continues to provide all editor features (language server, syntax highlighting, preview panel, commands). It does not carry MCP server binaries or Copilot customization content.

---

## 🧠 The Problem It Solves

Most complex entities start simple. But as business requirements grow, the rules governing their lifecycles scatter across your codebase:
- **State transitions** land in `switch` statements or scattered handler logic.
- **Data validation** gets pushed into ORMs, FluentValidation, or entity constructors — separate from the rules that depend on it.
- **Side effects** trigger asynchronously with no guarantee the data is ready.

Eventually, the system drifts. An entity ends up in a `Shipped` state without a `TrackingNumber`. When stakeholders ask, "Under what exact conditions can an Order be refunded?", developers have to traverse six different classes to find the answer.

Precept fixes this by treating the lifecycle of an entity as an executable contract.

## 🤖 Designed for AI

Precept is readable by humans and writable by hand — but it is *designed* to work exceptionally well with AI. Every syntax and semantics decision produces properties that AI agents can exploit:

- **Deterministic semantics.** The same precept + the same data = the same outcome, every time. An AI can reason about behavior without worrying about hidden state, timing, or side effects.
- **Structured tool APIs.** The MCP server exposes five tools (`precept_language`, `precept_compile`, `precept_inspect`, `precept_fire`, `precept_update`) that return typed JSON — not prose. An AI can validate its own work, inspect runtime behavior, and iterate without human intervention.
- **Language reference as data.** `precept_language` returns the complete DSL vocabulary, construct forms, semantic constraints, expression scopes, and fire pipeline as structured JSON. The AI doesn't need training data or examples to know what the language supports — it can query the authoritative specification at tool-call time.
- **Safe preview before commit.** `Inspect` and `precept_inspect` let an AI explore "what happens if I fire this event?" from any state without mutating anything. This makes trial-and-error exploration safe and cheap.
- **Closed validation loop.** An AI can write a precept, validate it, audit its structure, run scenarios against it, and fix issues — all through tool calls, without ever needing a terminal, a build step, or human feedback.

The overall vision: a domain expert describes what they want in natural language, the AI authors the precept, and the toolchain guarantees correctness. The human reviews a readable contract; the AI does the heavy lifting.

## 🏗️ The Pillars of Precept

### 1. The Universal Safety Net (`invariant`)
In most systems, validation is bound to *actions* (e.g., "Validate this API payload"). In Precept, rules are bound to the *data itself*.

When you declare `invariant Balance >= 0 because "Balance cannot be negative"`, that rule is absolute. Whether a complex workflow transition deducts from the balance, or a user directly edits a field via an administrative override, the engine enforces the rule upon completion. If it fails, the entire transaction rolls back.

State-scoped asserts (`in <State> assert ... because "..."`) and event-scoped asserts (`on <Event> assert ... because "..."`) let you enforce rules exactly where they apply.

### 2. Pure Inspection (`Inspect` before `Fire`)
Because Precept enforces rigorous grammar constraints—expressions evaluate, statements mutate—it is impossible for a transition guard to accidentally mutate data. This allows the `Inspect` API to safely preview any action, returning a precise outcome with specific constraint violations—all without saving a thing.

### 3. Atomic, Deterministic Mutations
A Precept transition either completely succeeds or entirely rolls back. Every evaluation is deterministic: the same definitions and the same data will *always* result in the same outcome.

### 4. Two Mutational Paths
Precept acknowledges that entirely different ceremonies apply to different types of data updates:
* **Transitions (`event`):** For lifecycle changes where routing, auditing, and complex state progression matter.
* **Direct Edits (`edit`):** For simple data mutations where event ceremony is overkill.

Both paths are safely watched by the exact same invariant engine. Direct editing isn't a hack; it is a first-class feature protected by the same ironclad invariants.

## Local Development Loop (Contributors)

This repo produces two VS Code artifacts: the **extension** (editor features) and the **agent plugin** (Copilot features). Both are developed locally from `tools/` and follow the same edit → build → reload cycle.

### Prerequisites

Enable the agent plugins preview in your user settings:

```json
{ "chat.plugins.enabled": true }
```

### Tasks

All dev tasks are in `.vscode/tasks.json`, runnable via **Tasks: Run Task** (`Ctrl+Shift+P` → `Tasks: Run Task`):

| Task | What it does |
|------|-------------|
| `build` | Builds the language server to `temp/dev-language-server/` |
| `extension: install` | Builds + installs the extension from `tools/Precept.VsCode/` |
| `extension: uninstall` | Uninstalls the local extension |
| `plugin: enable` | Registers `tools/Precept.Plugin/` in workspace `chat.pluginLocations` |
| `plugin: disable` | Unregisters the plugin from `chat.pluginLocations` |

### Extension changes

The extension provides the language server, syntax highlighting, preview panel, and commands.

**C# language-server or runtime code:**

1. Run `Build Task` / `Ctrl+Shift+B`.
2. The default `build` task compiles into `temp/dev-language-server` with `--artifacts-path`.
3. The installed extension detects the new DLL, creates a fresh shadow copy under `temp/dev-language-server/runtime`, and restarts the language client automatically.

**TypeScript extension-host or webview code:**

1. Run task: `extension: install`.
2. Run `Developer: Reload Window`.

The status bar shows `Precept LS: Dev`. Click it, or run `Precept: Show Language Server Mode`, to inspect the active launch paths.

### Plugin changes

The plugin provides the Precept Author agent, companion skills, and MCP server tools for Copilot.

**Agent or skill markdown:**

1. Edit files in `tools/Precept.Plugin/agents/` or `tools/Precept.Plugin/skills/`.
2. Run `Developer: Reload Window`.
3. The agent appears in the Chat agents picker; skills appear in the `/` slash command menu.

**MCP server C# code:**

1. Edit source in `tools/Precept.Mcp/`.
2. Run `Developer: Reload Window`, then trigger any MCP-backed action.
3. The plugin's launcher rebuilds `tools/Precept.Mcp` into `temp/dev-mcp`, creates a fresh shadow copy under `temp/dev-mcp/runtime`, and starts the new runtime on demand.

**First-time setup:**

1. Run task: `plugin: enable` (one time — stays enabled across reloads).
2. Run `Developer: Reload Window`.
3. Verify: the Precept Author agent appears in Chat, the 6 precept tools appear in the tools list.

To stop loading the plugin, run task: `plugin: disable`, then reload.

### File locking

Both the language server and MCP server use shadow-copy launchers to avoid file locking during rebuilds:

| Server | Build output | Running process locks | Rebuild safe? |
|--------|-------------|----------------------|---------------|
| Language server | `temp/dev-language-server/bin/` | `temp/dev-language-server/runtime/` | Yes |
| MCP server | `temp/dev-mcp/bin/` | `temp/dev-mcp/runtime/run-*/` | Yes |

The running process never locks the build output directory. Old runtime copies are pruned automatically on the next launch; locked directories are silently skipped.