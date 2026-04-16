---
description: "Use when changing the Precept DSL surface, parser, runtime behavior, syntax grammar, language server completions, MCP DTOs/tools, or sample .precept files. Covers same-PR sync for docs, grammar, completions, MCP docs, tests, and samples."
name: "Language Surface Sync"
applyTo: "src/Precept/Dsl/**,tools/Precept.LanguageServer/**,tools/Precept.VsCode/syntaxes/**,tools/Precept.Mcp/Tools/**,samples/**/*.precept"
---
# Language Surface Sync

Follow `CONTRIBUTING.md` and keep language-surface changes synchronized in the same PR.

Every language-surface change has impact across three categories: **Runtime**, **Tooling**, and **MCP**. Evaluate all three in the same change — not as follow-up work.

## Impact Categories

### 1. Runtime Impact

*What the parser, type checker, evaluator, and engine must handle.*

| Change type | What must work |
|---|---|
| New type keyword | Parses in field declarations, event args, and collection inner types |
| New constraint keyword | Accepted in constraint zone; type-checked against valid target types |
| New operator | Evaluates correctly in guards, rules, and all expression contexts |
| New diagnostic | Emits on the correct condition with correct code, severity, and message |
| New expression form | Evaluates in every expression context: guard, rule, set RHS, ensure |

**Docs to update:** `docs/PreceptLanguageDesign.md`, `docs/RuntimeApiDesign.md`, `docs/ConstraintViolationDesign.md`.

### 2. Tooling Impact

*What syntax highlighting, completions, hover, and semantic tokens must handle.*

| Change type | Syntax highlighting | Completions | Hover |
|---|---|---|---|
| New type keyword | Highlighted as type in all positions: standalone keyword, after `as` in scalar fields, after `of` in collections | Offered after `as` in field declarations; offered after `of` in collection fields | Shows type info when hovering on a field of this type |
| New constraint keyword | Highlighted as constraint keyword | Offered in constraint zone after the correct types only | Shown in field hover when applied |
| New collection inner type | Highlighted as type after `of` in collection declarations | Offered after `of` in collection field context | Shows inner type in collection field hover |
| Choice member values | String literals highlighted within `choice(...)` | Offered as completions in `set`/`add`/`=` positions targeting a choice field | Listed on hover for choice fields |
| New field modifier | Highlighted in the correct syntactic position | Offered in the correct position after type | Reflected in hover output |

**Files to update:** `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`, `tools/Precept.LanguageServer/` (completions, hover, semantic tokens).

### 3. MCP Impact

*What the MCP tools must expose to AI consumers.*

| Change type | `precept_language` | `precept_compile` | Fire / Inspect / Update |
|---|---|---|---|
| New type keyword | Listed in type keywords (automatic if `[TokenCategory(Type)]` attribute is set) | Type name appears in field and event-arg DTOs | Values stored and returned in correct serialized form |
| New constraint keyword | Listed in constraint keywords (automatic if attribute is set) | Constraint appears in field constraints | Constraint enforced at runtime; violation = ConstraintFailure |
| New field property (e.g., choiceValues, isOrdered) | N/A | New DTO property carries the metadata | Property reflected in instance data and inspection output |
| New diagnostic | Listed in constraints (automatic from `DiagnosticCatalog`) | Diagnostic appears in compilation output | N/A (compile-time only) |

**Files to update:** `tools/Precept.Mcp/Tools/` (DTOs, serialization), `docs/McpServerDesign.md`.

## Process Obligations

### At Design Time (proposals)

Every proposal that changes the language surface must include an **Impact** section covering all three categories. Frank leads the impact analysis; implementing devs (George, Kramer, Newman) participate in the design review to flag gaps Frank may miss. A proposal without an impact section is incomplete — it does not advance to implementation.

### At Implementation Time

The implementing agent verifies all three categories work before marking a slice done. A feature that parses and type-checks but lacks syntax highlighting or MCP serialization is **incomplete**.

### At Review Time

Frank verifies each impact category has been addressed or explicitly marked N/A. Soup Nazi verifies drift tests cover every new type, constraint, and operator. A PR that addresses runtime but neglects tooling or MCP is blocked.

## General Rules

- If you touch `.precept` samples, read at least one existing sample first and validate the changed files.
- If no sync updates are needed for a category, say so explicitly in the PR summary.
- Do not leave aspirational docs. Specs in `docs/` must describe what exists after the implementation lands.
- `README.md` must track real implementation — update on every meaningful change.