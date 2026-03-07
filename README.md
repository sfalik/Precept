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

if (preview.Outcome == PreceptOutcomeKind.Rejected)
{
    // Output: "Approval requires verified documents, strong credit, and affordable debt load"
    Console.WriteLine(string.Join(", ", preview.Reasons));
}
else
{
    // Transition is valid — fire and persist
    var result = engine.Fire(instance, "Approve", new Dictionary<string, object?>
    {
        ["ApprovedAmount"] = 45_000.0
    });

    if (result.Outcome == PreceptOutcomeKind.Accepted)
    {
        var updated = result.UpdatedInstance!;
        Console.WriteLine($"State: {updated.CurrentState}"); // Approved
        await repository.SaveAsync(updated.InstanceData);
    }
}

// Direct field editing — no event needed (fields declared with `in <State> edit`)
var editResult = engine.Update(instance, patch => patch
    .Set("Notes", "Customer called back"));

if (editResult.Outcome == PreceptUpdateOutcome.Updated)
{
    instance = editResult.UpdatedInstance!;
    // Invariants and state asserts are enforced — same safety net as Fire
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
- **Shared Type Diagnostics:** Expression typing, null-flow narrowing, collection mutation checks, and `from any` state-aware analysis now come from the core compiler, so the language server and MCP validation surface the same `PRECEPT038`-`PRECEPT043` errors. The MCP `precept_validate` result includes those diagnostic codes structurally, and returns all shared type diagnostics instead of stopping at the first one.
- **Structural Diagnostics:** Real-time diagnostics now surface unreachable states, dead-end states, orphaned events, and reject-only state/event pairs that likely model unsupported events as explicit rejections.
- **Quick Fixes:** Reject-only state/event pair warnings offer a quick fix to remove the rows and restore `NotDefined` semantics for unsupported events, and orphaned event warnings offer a quick fix to remove the unused event declaration and its event asserts.
- **Interactive Inspector:** Fire events and edit data against a live, mock instance in VS Code. The preview behaves like Markdown preview by default: one right-side preview follows the active `.precept` editor, and you can lock it to the current file with **Toggle Preview Locking**. Field edits use explicit **Edit** mode with **Save/Cancel**; validation runs live via inspect while typing, field-level errors stay attached only to the fields that caused them, and values are committed only when **Save** is clicked.
- **Live Diagramming:** A dynamic state-transition diagram renders as you type.
- **Null-Flow Analysis:** Real-time squiggles warn you if a guard path might access an unsafe null value.

![Interactive Inspector](docs/images/inspector-preview.png)

---

## 🤖 MCP Server (Copilot Integration)

Precept includes an MCP (Model Context Protocol) server that exposes DSL parsing, validation, structural analysis, and runtime execution as tools callable by Copilot and any MCP-compatible host. This enables semantic understanding of `.precept` files beyond plain text reading.

### Tools

| Tool | Purpose |
|------|---------|
| `precept_validate` | Parse and compile a `.precept` file, including shared type diagnostics such as `PRECEPT038`-`PRECEPT043`; validation returns all shared type diagnostics with structured codes |
| `precept_schema` | Return the full typed structure — states, fields, events, transitions |
| `precept_audit` | Graph analysis — reachability, dead ends, terminal states, orphaned events |
| `precept_run` | Execute a step-by-step event scenario, return outcomes |
| `precept_language` | Full DSL reference — vocabulary, constructs, constraints, pipeline |
| `precept_inspect` | From a state+data snapshot, preview what every event would do |

### Bundled with the Extension

When the VS Code extension is packaged for distribution (`npm run package:dist`), a self-contained build of the MCP server is bundled inside the VSIX for each supported platform (win-x64, linux-x64, osx-arm64). The extension registers the server via the `mcpServerDefinitionProviders` contribution point in `package.json` and the `vscode.lm.registerMcpServerDefinitionProvider()` API so that VS Code discovers and launches it automatically — no manual `mcp.json` configuration, no .NET SDK prerequisite, and no separate install step.

### Development Setup

During development in this repo, the MCP server runs from source via `.vscode/mcp.json`, bypassing the bundled binary. The workspace launcher builds and shadow-copies the server on demand:
```
node tools/Precept.VsCode/scripts/start-precept-mcp.js
```

On each launch, the launcher builds `tools/Precept.Mcp/Precept.Mcp.csproj` into `temp/dev-mcp`, copies that build into a fresh shadow runtime under `temp/dev-mcp/runtime/run-*`, and runs the copied `Precept.Mcp.dll`. This keeps the live MCP process off the default `bin/Debug` output so rebuilding the project does not collide with the running server.

---

## 🧠 The Problem It Solves

Most complex entities start simple. But as business requirements grow, the rules governing their lifecycles scatter across your codebase:
- **State transitions** land in `switch` statements or scattered handler logic.
- **Data validation** gets pushed into ORMs, FluentValidation, or entity constructors.
- **Side effects** trigger asynchronously with no guarantee the data is ready.

Eventually, the system drifts. An entity ends up in a `Shipped` state without a `TrackingNumber`. When stakeholders ask, "Under what exact conditions can an Order be refunded?", developers have to traverse six different classes to find the answer.

Precept fixes this by treating the lifecycle of an entity as an executable contract.

## 🏗️ The Pillars of Precept

### 1. The Universal Safety Net (`invariant`)
In most systems, validation is bound to *actions* (e.g., "Validate this API payload"). In Precept, constraints are bound to the *data itself*.

When you declare `invariant Balance >= 0 because "Balance cannot be negative"`, that precept is absolute. Whether a complex workflow transition deducts from the balance, or a user directly edits a field via an administrative override, the engine enforces the invariant upon completion. If it fails, the entire transaction rolls back.

State-scoped asserts (`in <State> assert ... because "..."`) and event-scoped asserts (`on <Event> assert ... because "..."`) let you enforce constraints exactly where they apply.

### 2. Pure Inspection (`Inspect` before `Fire`)
Because Precept enforces rigorous grammar constraints—expressions evaluate, statements mutate—it is impossible for a transition guard to accidentally mutate data. This allows the `Inspect` API to safely preview any action, returning a precise outcome with specific error reasons—all without saving a thing.

### 3. Atomic, Deterministic Mutations
A Precept transition either completely succeeds or entirely rolls back. Every evaluation is deterministic: the same definitions and the same data will *always* result in the same outcome.

### 4. Two Mutational Paths
Precept acknowledges that entirely different ceremonies apply to different types of data updates:
* **Transitions (`event`):** For lifecycle changes where routing, auditing, and complex state progression matter.
* **Direct Edits (`edit`):** For simple data mutations where event ceremony is overkill.

Both paths are safely watched by the exact same invariant engine. Direct editing isn't a hack; it is a first-class feature protected by the same ironclad invariants.

## VS Code Extension Local Loop (Contributors)

Use the installed extension in your normal VS Code window.

Recommended setup: keep `local.precept-vscode` installed for day-to-day work in that window. Use the packaging tasks only when you need to refresh the installed extension code after TypeScript or webview changes.

When you change C# language-server or runtime code:

1. Run `Build Task` / `Ctrl+Shift+B`.
2. The default `build` task compiles `tools/Precept.LanguageServer/Precept.LanguageServer.csproj` into `temp/dev-language-server` with `--artifacts-path`.
3. The installed extension detects the new DLL, creates a fresh shadow copy under `temp/dev-language-server/runtime`, and restarts the language client automatically.

When you change TypeScript extension-host code or webview code:

1. Run `extension: loop local install`.
2. Run `Developer: Reload Window`.

When you change MCP server code:

1. Run `Developer: Reload Window`.
2. Trigger any MCP-backed action.
3. The launcher rebuilds `tools/Precept.Mcp` into `temp/dev-mcp`, creates a fresh shadow copy under `temp/dev-mcp/runtime`, and starts the new runtime on demand.

On first activation, the installed extension bootstraps the dev language-server artifacts under `temp/dev-language-server` if they are missing.

The status bar shows `Precept LS: Dev`. Click it, or run `Precept: Show Language Server Mode`, to inspect the active launch paths in the output channel.

Use `npm run loop:local` only as the command-line equivalent of `extension: loop local install`.

Use `extension: loop local uninstall` or `npm run loop:local:uninstall` only if you want to remove the installed local VSIX from your profile.