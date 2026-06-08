---
status: Active
purpose: Cross-layer anti-pattern catalog — patterns reviewers should flag and authors should consult before writing code or designs that "look fine but violate a principle elsewhere"
---

# Anti-Patterns

A cross-layer catalog of patterns that violate Precept's non-negotiable principles. Each entry: **the pattern**, **why it's wrong** (principle violated), and **the correct alternative**. Reviewers flag these; authors consult before writing code or designs.

This doc is the **single index** of anti-patterns. Per-layer specifics (catalog patterns A-H, MCP-tool anti-patterns, LS feature routing) live in the canonical docs linked from each entry — this doc points to them, never duplicates them.

## Conventions

Entries are grouped by layer. Each entry has the same shape:

- **Pattern** — name the failure mode
- **Why it's wrong** — the principle violated (cite the canonical doc)
- **Correct alternative** — what to do instead

The catalog-driven checklist's "Red flags" section ([`docs/contributing/catalog-driven-checklist.md § Red flags`](catalog-driven-checklist.md)) covers the specific code smells reviewers grep for. This doc covers the architectural principle behind each smell.

---

## Catalog system

### CS-1. Switching on `*Kind` enum identity to dispatch per-member behavior

**Pattern.** `kind switch { FooKind.Bar => ..., FooKind.Baz => ... }` in a pipeline stage, where each arm exists "because the language says so."

**Why it's wrong.** Per-member behavior is domain knowledge about the language. It belongs in catalog metadata so the catalog system can enforce completeness. The smell is Pattern B in [`catalog-system.md § Architectural Violation Patterns`](../language/catalog-system.md).

**Correct alternative.** Add the behavior to the catalog's `*Meta` record; the consumer becomes a generic walker. Note: switching on a **DU subtype** is correct — the subtype IS the metadata shape. Switching on a catalog member's enum identity is the violation.

### CS-2. Maintaining parallel keyword / token lists

**Pattern.** `new[] { TokenKind.X, TokenKind.Y }` or `FrozenSet<TokenKind>` in a consumer, listing specific members the consumer "cares about."

**Why it's wrong.** Parallel knowledge maintained outside the catalog drifts independently. The catalog system was built to make this class of drift mechanically impossible. Pattern A in [`catalog-system.md § Architectural Violation Patterns`](../language/catalog-system.md).

**Correct alternative.** Derive the set from catalog metadata: `Constructs.ByLeadingToken`, `DisambiguationEntry.DisambiguationTokens`, `Modifiers`, `Actions`, `Types`, `Operators`, or `TokenMeta.IsMessagePosition` already encode what you need. If the catalog can't express it, the catalog field is the deliverable — not a workaround.

### CS-3. Hardcoded language fact in C# code

**Pattern.** An operator-implication table, type-default table, collection-suffix table, or operator-folding table lives in C# code.

**Why it's wrong.** Language facts are domain knowledge. The next reader has to discover the table by grep rather than by reading the catalog. Pattern D in [`catalog-system.md § Architectural Violation Patterns`](../language/catalog-system.md).

**Correct alternative.** Promote to catalog metadata (`OperatorMeta.InverseComparison`, `TypeMeta.AbstractDefaultValue`, `TypeMeta.CollectionSyntax`, etc.).

### CS-4. Sentinel-encoded semantics

**Pattern.** A wildcard or broadcast target is carried as `null`, `"any"`, or a magic string instead of a typed metadata field. Consumers re-derive its meaning by literal comparison.

**Why it's wrong.** The semantic is invisible at the type level; every consumer must encode the convention separately. Pattern E in [`catalog-system.md § Architectural Violation Patterns`](../language/catalog-system.md).

**Correct alternative.** Add a typed metadata field (`TokenMeta.IsStateWildcard`, `FieldTargetKind`, broadcast policy on transition rows). The sentinel disappears.

### CS-5. Out-of-band classification axis

**Pattern.** A consumer maintains a side-channel taxonomy — `if mod == X || mod == Y` checks for "initial-ish" modifiers; paired-bound modifier checks — rather than reading a metadata flag.

**Why it's wrong.** The grouping is domain knowledge about modifier semantics; encoding it outside the catalog creates drift. Pattern H in [`catalog-system.md § Architectural Violation Patterns`](../language/catalog-system.md).

**Correct alternative.** Add the semantic role flag to the catalog (`Modifiers.BoundCounterpart`, `MarksInitial`, `AffectsPresence`, `AffectsWritability`).

### CS-6. "Small enum doesn't need a catalog"

**Pattern.** Argument that an enum with only 3 members doesn't warrant cataloging.

**Why it's wrong.** Size is irrelevant. `AccessMode` has 3 values but each has distinct behavioral semantics. The question is "do consumers hardcode per-member knowledge that should be metadata?" — and if any do, it gets cataloged. See [`catalog-system.md § Completeness Principle`](../language/catalog-system.md).

**Correct alternative.** Catalog any enum whose members carry behavioral semantics consumers need.

### CS-7. Flat record with inapplicable nullable fields

**Pattern.** A catalog's `*Meta` record has fields that are meaningless for some members; consumers must check for null or default values per member.

**Why it's wrong.** The shape doesn't match the data; type system can't help. See [`catalog-system.md § Pattern Definition § Meta record shape`](../language/catalog-system.md).

**Correct alternative.** Use a discriminated union — abstract base + sealed subtypes — where each subtype carries exactly the fields its consumers need.

### CS-8. Bypassed metadata shape

**Pattern.** A specialized path parses or validates a construct using a stripped-down grammar that ignores the catalog's declared shape (e.g., event-argument modifiers skipping `ValueModifierMeta.HasValue`).

**Why it's wrong.** The specialized path will drift from the catalog's shape over time. Pattern C in [`catalog-system.md § Architectural Violation Patterns`](../language/catalog-system.md).

**Correct alternative.** Reuse the shared metadata-driven path. Specializations of grammar are catalog gaps; encode them on the meta record.

---

## Parser

### PR-1. Hardcoded operator precedence

**Pattern.** A binding-power integer in `ParseExpression`, `GetLedBindingPower`, or a lookahead helper.

**Why it's wrong.** Operator precedence is catalog metadata. Hardcoded precedence means every operator addition or precedence change is a parser edit. See [`compiler/parser.md § Right-Sizing`](../compiler/parser.md).

**Correct alternative.** Read precedence from `Operators.GetMeta()`. The Pratt-parser engine consumes catalog metadata; precedence change is a catalog edit.

### PR-2. Manual disambiguation lookahead

**Pattern.** Constructs share a leading token; the parser hardcodes a peek sequence to disambiguate them.

**Why it's wrong.** The disambiguation logic belongs in `DisambiguationEntry` metadata on the construct catalog. Parser becomes a generic disambiguator.

**Correct alternative.** Encode the disambiguation tokens / conditions in `Constructs.ByLeadingToken` + `DisambiguationEntry.DisambiguationTokens`; let the parser dispatch from metadata.

---

## Type checker

### TC-1. Per-construct check methods

**Pattern.** `CheckFieldDeclaration()`, `CheckTransitionRow()`, `CheckRuleDeclaration()` — one method per `ConstructKind`.

**Why it's wrong.** Construct-by-construct check methods accumulate parallel logic; the catalog already declares slot shapes and validation rules. See [`compiler-and-runtime-design.md § 6 — Right-sized type checking`](../compiler-and-runtime-design.md).

**Correct alternative.** Generic resolution passes that read construct metadata from catalogs. Only construct-specific structural validation that genuinely differs by kind warrants a per-kind method.

### TC-2. Mirroring the parse tree

**Pattern.** `SemanticIndex` mirrors `ConstructManifest`'s shape — types bolted onto syntax nodes, nested traversal required.

**Why it's wrong.** Anti-mirroring rules — the SemanticIndex is a flat semantic inventory shaped for consumers, not a tree mirror of the parser. See [`compiler/type-checker.md § 7.1 § Anti-mirroring rules`](../compiler/type-checker.md).

**Correct alternative.** Build a flat inventory by semantic role (Fields, States, Events, TypedTransitionRows, TypedRules). Carry back-pointers to `ParsedConstruct` for diagnostic source spans; downstream stages consume the semantic columns, never the back-pointers.

### TC-3. Walking back-pointers in downstream stages

**Pattern.** GraphAnalyzer, ProofEngine, PreceptBuilder, or Evaluator reads `TypedField.Syntax`, `TypedState.Syntax`, etc. to extract semantic data.

**Why it's wrong.** Anti-mirroring rule 3 — back-pointers are TypeChecker-internal. Downstream stages must consume semantic data from typed records, not syntax structure. See [`compiler/type-checker.md § 7.1 § Anti-mirroring rules`](../compiler/type-checker.md).

**Correct alternative.** If the downstream stage needs information not present on the typed record, the inventory is underspecified — add the field. Don't walk back to syntax.

### TC-4. Flat action shape with optional nullable fields

**Pattern.** `TypedAction` carries `InputExpression?` (nullable for actions that don't have one) and `Binding?` (nullable for actions that don't have one).

**Why it's wrong.** A flat shape with optional fields requires every consumer to handle the null cases; the type system can't help. See [`compiler/type-checker.md § 7.1 § Typed action family`](../compiler/type-checker.md).

**Correct alternative.** Three-shape DU: `TypedAction<Field>`, `TypedAction<State>`, `TypedAction<None>` (or equivalent). Each subtype carries exactly its target shape; consumers pattern-match.

---

## Proof engine

### PE-1. Proof discharge with local knowledge

**Pattern.** Proof discharge, diagnostic mapping, or guard reasoning encodes operator/accessor/requirement semantics in handwritten switches inside the proof engine.

**Why it's wrong.** Proof semantics is per-operator / per-accessor metadata — it belongs in the operator and accessor catalogs. Pattern F in [`catalog-system.md § Architectural Violation Patterns`](../language/catalog-system.md).

**Correct alternative.** Add proof metadata to the relevant catalog (`ProofRequirementMeta.FailureDiagnostic`, `OperatorMeta.SatisfactionCovers`, accessor-level `LogicalRole`). The discharge engine becomes generic.

### PE-2. Hardcoded fault-code prevention mapping

**Pattern.** "FaultCode X is prevented by DiagnosticCode Y" — encoded in a switch in proof-engine code.

**Why it's wrong.** The prevention chain is a structural invariant of the catalog system. Encoded in code, it drifts; encoded as `[StaticallyPreventable(DiagnosticCode.X)]` attributes on `FaultCode` enum members, the Roslyn analyzers verify it at build time. See [`compiler-and-runtime-design.md § 8 § Proof/fault chain`](../compiler-and-runtime-design.md).

**Correct alternative.** Declare prevention via `[StaticallyPreventable]` attribute. PRECEPT0001 / PRECEPT0002 analyzers enforce the linkage at compile time.

---

## Runtime

### RT-1. Evaluator reasoning about semantics at runtime

**Pattern.** The evaluator looks up types, resolves operators, dispatches on enum identity, or re-runs analysis during a Fire / Update / Create / Restore operation.

**Why it's wrong.** All semantic questions are resolved at build time (PreceptBuilder). The evaluator is a plan executor against prebuilt execution plans. Runtime semantic reasoning means the build step didn't do its job. See [`compiler-and-runtime-design.md § 10 — Precept Builder`](../compiler-and-runtime-design.md).

**Correct alternative.** If the evaluator needs information, it must be on the descriptor or execution plan that flows from PreceptBuilder. Add the field upstream; the evaluator stays generic.

### RT-2. 1:1 SemanticIndex → runtime type mapping

**Pattern.** Runtime types are renamed copies of SemanticIndex types — `RuntimeField` mirrors `TypedField` field-for-field.

**Why it's wrong.** The runtime model is restructured for execution: constraints by activation anchor, actions by transition row, expressions as flat opcodes. A 1:1 mapping means the dispatch optimizations weren't done. See [`runtime/precept-builder.md § 2 § Restructuring, not renaming`](../runtime/precept-builder.md).

**Correct alternative.** PreceptBuilder is a restructuring transformation. Group constraints by activation anchor; lower expressions to flat opcodes; build dispatch indexes. The runtime type is a dispatch-optimized index, not a renamed analysis model.

### RT-3. Trusting persisted data on Restore

**Pattern.** `Restore` accepts persisted data and treats it as already-valid; skips constraint evaluation.

**Why it's wrong.** Persisted data may include stale computed-field values or definition-evolution mismatches. Trusting it lets invariant violations through. See [`runtime/runtime-api.md § Restore`](../runtime/runtime-api.md).

**Correct alternative.** `Restore` recomputes computed fields BEFORE constraint evaluation, then validates against the current definition. The constraint evaluation runs; only access-mode checks (an active-edit concern) are bypassed.

---

## Language server

### LS-1. Walking ConstructManifest to answer semantic questions

**Pattern.** Hover / go-to-definition / completions / semantic tokens reads the parse tree (`ConstructManifest`) directly to resolve semantic identity.

**Why it's wrong.** `ConstructManifest` is syntactic. Semantic identity lives in `SemanticIndex`. Walking the parse tree means the SemanticIndex is underspecified — but the LS shouldn't paper over that; the inventory should be fixed. See [`tooling/language-server.md § LSP Feature Routing`](../tooling/language-server.md) and [`compiler-and-runtime-design.md § 15`](../compiler-and-runtime-design.md).

**Correct alternative.** Read `SemanticIndex` + back-pointers. If the answer isn't there, add the semantic field to the typed record (TC-3 same root cause as the downstream-stage version).

### LS-2. Consuming Compilation after Precept exists

**Pattern.** Preview / inspect features query `Compilation` (the analysis snapshot) even though a valid `Precept` runtime model is available.

**Why it's wrong.** The runtime snapshot is the source of truth for inspection — that's the structural guarantee. Reading `Compilation` after `Precept` exists means the LS is bypassing the runtime model. See [`tooling/language-server.md § LSP Feature Routing`](../tooling/language-server.md).

**Correct alternative.** Use `Precept` + inspection runtime for preview / inspect. Use `Compilation` only for authoring-time features (diagnostics, completions, hover during edit).

### LS-3. Duplicating catalog knowledge in completion / hover code

**Pattern.** Completions hardcode "after `field`, suggest types and modifiers"; hover hardcodes per-keyword description text.

**Why it's wrong.** The catalog already declares `TokenMeta.ValidAfter` (positional grammar) and `TokenMeta.Description` (hover text). Duplicating in LS code creates drift. See [`tooling/language-server.md`](../tooling/language-server.md).

**Correct alternative.** Query the catalog; format the response. Adding a keyword's description in the catalog automatically updates hover.

---

## MCP

### MCP-1. Tool method exceeding ~30 lines of non-serialization code

**Pattern.** A tool in `tools/Precept.Mcp/Tools/` contains domain logic — not just thin serialization.

**Why it's wrong.** MCP tools are thin wrappers. Domain logic in MCP code means it's not in `src/Precept/` where it belongs (testable, reusable, single source of truth). See [`tooling/mcp.md § Architectural principles`](../tooling/mcp.md) and `CLAUDE.md § MCP Tool Sync`.

**Correct alternative.** Move the logic to `src/Precept/`. The MCP tool deserializes the request, calls a core API, serializes the response.

### MCP-2. Parallel vocabulary maintained in MCP code

**Pattern.** A list of keywords / types / operators / diagnostic codes lives in `tools/Precept.Mcp/Tools/` separate from the catalogs.

**Why it's wrong.** Same anti-pattern as CS-2 at the MCP layer. See [`tooling/mcp.md`](../tooling/mcp.md).

**Correct alternative.** MCP catalog-reference tools (`precept_syntax`, `precept_types`, `precept_operations`, `precept_domains`, `precept_proofs`, `precept_patterns`, `precept_diagnostic`) iterate the catalogs at request time. No parallel list.

---

## Grammar generator

### GG-1. Hand-editing `precept.tmLanguage.json`

**Pattern.** Adding patterns or scope rules directly to `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`.

**Why it's wrong.** The grammar is a generated artifact. Hand edits drift from the catalog the moment a keyword changes. See [`compiler-and-runtime-design.md § 13`](../compiler-and-runtime-design.md) and [`compiler/grammar-generator.md`](../compiler/grammar-generator.md).

**Correct alternative.** Add the language element to the appropriate catalog. The grammar generator picks it up. Hand-edited grammar JSON is a build-time regeneration target, not a source.

---

## Documentation

### DOC-1. Parallel knowledge in prose

**Pattern.** A doc carries a count, list, or inventory of catalog members ("the thirteen catalogs", "fourteen catalogs cover the language surface", a list of all operators with their precedence) outside the canonical catalog source.

**Why it's wrong.** Same catalog-discipline failure as in code. Three docs counting independently gave three different counts in the Phase-1 audit. See [`catalog-system.md § Catalog Inventory`](../language/catalog-system.md).

**Correct alternative.** The catalogs in `src/Precept/Language/*.cs` and the MCP catalog-reference tools are the canonical inventory. Docs reference catalogs by name, not by count; reviewers verify via tools, not prose.

### DOC-2. "Implemented" status on a doc whose code disagrees

**Pattern.** A doc carries `Status: Implemented` but the code throws `NotImplementedException` or behaves differently from the doc's claim.

**Why it's wrong.** Doc-sync rule from CLAUDE.md. An "Implemented" claim that's wrong wastes every reader's time and erodes trust in every other "Implemented" claim. See [`CLAUDE.md § Documentation Sync`](../../CLAUDE.md).

**Correct alternative.** When code changes, update the doc in the same pass. When code reality diverges from a doc claim, fix the doc — don't defer.

### DOC-3. Aspirational claim in `README.md`

**Pattern.** README advertises an API surface, a tool count, or a feature that doesn't yet exist as described.

**Why it's wrong.** README is the public face; an agent told "read the README" produces code against the wrong surface. See `CLAUDE.md § Source of Truth`.

**Correct alternative.** README tracks real implementation. Future-only content is clearly marked ("designed surface; ships in vNext") or omitted.

### DOC-4. Hand-edited generated artifact

**Pattern.** Editing a doc that's marked or known as generated — `precept.tmLanguage.json` (covered above in GG-1), MCP-tool output schemas, anything stamped "auto-generated."

**Why it's wrong.** Same drift surface as GG-1 at the doc layer.

**Correct alternative.** Edit the source. Regenerate.

---

## Tests

### TS-1. Hardcoded member lists in tests

**Pattern.** `new[] { ActionKind.Add, ActionKind.Remove }` in a test, representing a semantic grouping ("collection-mutation actions").

**Why it's wrong.** The grouping is domain knowledge — it should be a catalog field (`ApplicableTo`, `Category`, etc.). The test should query the catalog. When a new action is added that semantically belongs to the group, the catalog flag carries the membership; the hardcoded test list silently misses it. See [`contributing/catalog-driven-checklist.md § Per-member behavior`](catalog-driven-checklist.md).

**Correct alternative.** Add a catalog flag; query it in the test. The test stays correct as new members are added.

### TS-2. `default:` arm to silence exhaustive switch

**Pattern.** Adding `default: ...` or `_: ...` to a switch over a catalog enum to suppress CS8509.

**Why it's wrong.** CS8509 is the catalog system's enforcement mechanism. Suppressing it silently swallows new members. See [`catalog-system.md § Roslyn Enforcement Layer`](../language/catalog-system.md).

**Correct alternative.** Handle every member explicitly. If the handling is "do nothing," say so per-member with an explicit empty arm — the discipline is per-member acknowledgment, not catch-all suppression.

---

## Process / design

### PROC-1. Designing language surface inline in chat

**Pattern.** An agent (or human) proposes a specific syntax — "let's make this `field X maxplaces iso`" — in direct conversation, and the suggestion hardens into "the" design without going through `/design`.

**Why it's wrong.** A casual chat suggestion lacks the four-leg rationale, Philosophy Alignment matrix, Language Design Grounding (broader field, not just Precept-internal), Authoring Audience check, and Semantic Rules that the design skill enforces. The first thing written down tends to harden into the answer. See `CLAUDE.md § Language Surface Design`.

**Correct alternative.** When syntax options come up in chat, discuss tradeoffs briefly but route to `/design`. A suggestion made in chat is brainstorming; it must not harden into a decision without the skill.

### PROC-2. Implementation-plan markdown file alongside the PR

**Pattern.** A separate `implementation-plan.md` file in the branch or PR alongside actual code changes.

**Why it's wrong.** The PR body IS the implementation plan. A separate file duplicates and drifts. See `CONTRIBUTING.md` and [`.claude/skills/execute`](../../.claude/skills/execute/SKILL.md).

**Correct alternative.** Implementation Plan lives in the PR body. Slice list lives there. Check off slices as they land.

### PROC-3. Compile-time-preventable becoming runtime check

**Pattern.** A constraint that could be statically proved (division by zero on a literal, type coercion on a known type, range check on a known interval) is enforced at runtime instead.

**Why it's wrong.** "Prevention, not detection" — the structural guarantee. Pushing enforcement to runtime when compile-time enforcement was achievable weakens the central commitment. See [`philosophy.md`](../philosophy.md) and [`compiler/proof-engine.md`](../compiler/proof-engine.md).

**Correct alternative.** Encode as a proof obligation; the proof engine discharges it at compile time. Only obligations the proof engine genuinely cannot discharge become runtime fault-site backstops (defense-in-depth, not primary error path).

### PROC-4. Treating philosophy.md as immutable but downstream docs as inconsistent with it

**Pattern.** Philosophy.md commits to "honesty about approximation" and "domain expert primary author"; the type docs / runtime docs / language-server docs don't operationalize either commitment.

**Why it's wrong.** The philosophy that doesn't propagate to operational docs is aspirational, not load-bearing. An agent making decisions against the operational docs alone will drift from the philosophy. See Phase 3 of the doc-corpus improvement plan and [`philosophy.md`](../philosophy.md).

**Correct alternative.** Operationalize each commitment in the docs that could threaten it. Approximation Stance in type docs; Authoring Audience in the spec; reviewer enforces both as BLOCKERs.

---

## When the catalog can't express it

Do not work around it. Escalate.

The catalog system is designed to be extended. If a pipeline stage needs metadata that no catalog field currently carries, the answer is to add that field to the catalog — not to hardcode it in the consumer.

Before proposing an inline workaround, answer:

1. Which catalog should carry this knowledge?
2. What field shape would it have?
3. Which consumer(s) would read it?
4. Why does the catalog system structurally prevent expressing this — not just "it's inconvenient today"?

If you cannot answer (4), the catalog system can express it. Add the field. If you can answer (4) and have evidence of a genuine structural limitation, bring it to the owner with a concrete proposal. Do not ship the workaround while the discussion is open.

See [`contributing/catalog-driven-checklist.md § When You Think the Catalog Can't Express It`](catalog-driven-checklist.md).

---

## References

- [`language/catalog-system.md`](../language/catalog-system.md) — Architectural Violation Patterns A-H; catalog discipline foundations
- [`contributing/catalog-driven-checklist.md`](catalog-driven-checklist.md) — Reviewer red flags by code-smell category
- [`compiler/type-checker.md § 7.1 § Anti-mirroring rules`](../compiler/type-checker.md) — Numbered anti-mirroring rules for SemanticIndex
- [`compiler/parser.md § Right-Sizing`](../compiler/parser.md) — Right-sized parser patterns and what NOT to do
- [`compiler-and-runtime-design.md § 13`](../compiler-and-runtime-design.md) — Hand-edited grammar anti-pattern
- [`CLAUDE.md`](../../CLAUDE.md) — Catalog system, doc sync, language surface design rules
- [`philosophy.md`](../philosophy.md) — Prevention-not-detection commitment grounding PROC-3
