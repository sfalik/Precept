# parser-v2 Build Notes

**Author:** Frank
**Date:** 2026-04-28

## What was taken from parser.md

The following sections were copied forward with minimal adaptation (structure, format, and content still accurate):

- **Right-Sizing** — unchanged
- **Inputs and Outputs** — unchanged
- **Static Class + ParseSession** architecture — unchanged
- **Token Navigation** table — unchanged
- **`set` Disambiguation** — unchanged
- **`min`/`max` Disambiguation** — unchanged
- **Expression Parsing Detail** (all subsections: binding power table, null-denotation, left-denotation, conditional expressions, interpolation reassembly, action chain parsing) — unchanged
- **SourceSpan Contract** — unchanged
- **Contracts and Guarantees** / **Bounded Work Guarantee** — unchanged
- **Design Rationale** sections (preposition-first grammar, single-pass recursive descent, Pratt expression parsing, resilient by construction, flat declaration list, what stays hand-written, dispatch table: grammar structure vs. vocabulary) — lightly adapted for `Entries` terminology but structurally unchanged
- **Failure Modes and Recovery** — extended with two new error conditions but core structure unchanged
- **Deliberate Exclusions** — unchanged
- **Action Statement Nodes**, **Outcome Nodes**, **Supporting Types** — unchanged structurally; removed `IsMissing` parameter from record signatures per v8 DU style (base types no longer carry it)
- **Downstream Consumer Impact** — lightly adapted to reference `DisambiguationEntry.LeadingToken` instead of `ConstructMeta.LeadingToken`

## What was replaced from v8

- **Overview** — rewritten to describe catalog-driven dispatch (`ByLeadingToken`, `InvokeSlotParser`, `BuildNode`) as the primary architecture
- **Top-Level Dispatch Loop** — replaced hardcoded method table with catalog-driven `ByLeadingToken` lookup flow and candidate-count dispatch table
- **Preposition Disambiguation** — `In`-scoped table updated from 2 candidates to 3 (+ OmitDeclaration). Added disambiguation token explanation and guard-rejection behavior for omit
- **AST Node Hierarchy** — NEW section. Full hierarchy with FieldTargetNode DU, OmitDeclarationNode/AccessModeNode as separate constructs, complete 12-node declaration summary table
- **Grammar Reference** — NEW section. All 9 access-mode/omit forms, production rules, guard restriction with both positions documented
- **Slot Dispatch** — NEW section. `InvokeSlotParser()` mechanism with 16-row slot kind table, `BuildNode()` mechanism
- **Validation Layer** — NEW section. 4-tier enforcement pyramid (CS8509 build-time → test-time → design-time)
- **5-Layer Architecture** — NEW summary table (Layers A–E from v8 §5)
- **Dependencies — Constructs Catalog** — rewritten for `DisambiguationEntry`/`Entries` model, added derived indexes subsection
- **Diagnostics Catalog** — expanded from 4 codes to 6 (+ `OmitDoesNotSupportGuard`, `PreEventGuardNotAllowed`)

## Judgment calls

1. **Removed `IsMissing` from record signatures.** The v8 AST node specs (§3) do not carry `IsMissing` as a constructor parameter — it is a base-type concern handled by `SyntaxNode`. parser.md had `IsMissing = false` on every node. I followed v8's cleaner pattern.

2. **`ConstructKind` on `Declaration`.** parser.md had `Declaration` carrying a `ConstructKind Kind` property. v8's node specs do not show this. I followed v8 — the `ConstructKind` is known from the `BuildNode` switch arm but is not necessarily a stored property on every declaration subtype (consumers can pattern-match on the concrete type). This is a minor open question for implementation.

3. **Sync token set description.** parser.md listed `modify`, `readonly`, `editable`, `omit` as sync tokens. v8 §5 Layer E clarifies these are NOT in `LeadingTokens` — they are post-anchor disambiguation tokens. I followed v8's correction and noted that within `in`-scoped failures they serve as in-scope recovery anchors but are not part of the `Constructs.LeadingTokens` FrozenSet.

4. **Innovation section.** Expanded to cover the derived indexes and 4-tier validation as innovations (both are new in v2).

5. **Present tense throughout.** Written as if implementation is complete, per task spec.
