---
status: Active (index — auto-maintained)
purpose: Decision-history index for archived design work. Read here first before greping the archive for prior rationale on a topic.
---

# Working/Archive/ — Decision History Index

Archived design docs from `docs/Working/`. Each was either **promoted** (durable rationale lifted to canonical docs; the original retained as historical record) or **completed** without promotion (the work landed and the doc is no longer load-bearing).

**Do not edit these files.** They are historical record. If a decision needs revisiting, write a new design doc in `docs/Working/` and cite the relevant archive doc.

**Maintenance:** the `/lifecycle-5-promote` skill appends an entry here when archiving a doc. Manual archive moves should also update this index.

## How to use this index

- **Looking for prior rationale on a topic?** Find the topic group below; each entry's one-line summary + canonical-promotion target tells you whether the rationale lives in a canonical doc now (read that instead) or in the archive (read the archived doc).
- **Investigating a past decision?** Use the Author/Date columns to triangulate; many of these have detailed four-leg rationale that informed canonical docs.
- **Cross-referencing a sample or test?** The archive may contain the design that introduced the pattern; promoted-to pointer surfaces the current canonical location.

---

## By topic

### Catalog system / architectural enforcement

| Doc | Status | Promoted to / outcome |
|---|---|---|
| [catalog-compliance-audit.md](catalog-compliance-audit.md) | Promoted 2026-05-24 | `catalog-system.md § Architectural Violation Patterns` (Pattern A-H taxonomy) |
| [diagnostic-enforcement.md](diagnostic-enforcement.md) | Promoted 2026-05-24 | `catalog-system.md § Roslyn Enforcement Layer` |
| [diagnostic-enforcement-implementation-notes.md](diagnostic-enforcement-implementation-notes.md) | Promoted 2026-05-24 | Companion to diagnostic-enforcement.md |
| [frank-catalog-obligation-audit.md](frank-catalog-obligation-audit.md) | Promoted 2026-05-24 | `compiler/proof-engine.md § Obligation Generation Contract` |

### Language surface — constructs & semantics

| Doc | Status | Promoted to / outcome |
|---|---|---|
| [constructor-semantics.md](constructor-semantics.md) | Promoted | `precept-language-spec.md § 3A.5 Entity Construction` + `compiler/parser.md` |
| [comma-list-syntax-spike.md](comma-list-syntax-spike.md) | Spike analysis | State-only scope; implementation followed |
| [frank-constructor-terminal-design.md](frank-constructor-terminal-design.md) | Analysis | Owner-decision feed into constructor-semantics |
| [frank-initial-design-critique.md](frank-initial-design-critique.md) | Critique | Initial-event/state semantics feed |
| [frank-initial-event-semantics.md](frank-initial-event-semantics.md) | Semantics doc | Initial-event reasoning record |
| [hover-design.md](hover-design.md) | Design doc | Hover content design |
| [interval-hover-design.md](interval-hover-design.md) | Design doc | Interval-aware hover surface |

### Language surface — field & state guarantees

| Doc | Status | Promoted to / outcome |
|---|---|---|
| [field-state-guarantees.md](field-state-guarantees.md) | Second revision (superseded by v2/v3) | Analysis record |
| [field-state-guarantees-v2.md](field-state-guarantees-v2.md) | Design finalized | Superseded by v3 |
| [field-state-guarantees-v3.md](field-state-guarantees-v3.md) | Pending sign-off (archived) | Final design iteration |

### Language surface — frank reviews / audits

| Doc | Status | Promoted to / outcome |
|---|---|---|
| [frank-bounds-qualifier-audit.md](frank-bounds-qualifier-audit.md) | Promoted 2026-05-24 | `business-domain-types.md § Bounds qualification rules`; PRE0133/PRE0134 shipped |
| [frank-grammar-comprehensive-review-2026-05-10.md](frank-grammar-comprehensive-review-2026-05-10.md) | Review | Grammar doc audit |
| [frank-grammar-spec-audit-2026-05-10.md](frank-grammar-spec-audit-2026-05-10.md) | Integrity audit | Grammar / spec coherence |
| [frank-money-modifiers.md](frank-money-modifiers.md) | Decision (implemented by Kramer) | Numeric range modifiers on money/quantity |
| [frank-price-qualifier-enforcement-gap.md](frank-price-qualifier-enforcement-gap.md) | Bug analysis | Checker-gap analysis |
| [frank-price-qualifier-full-analysis.md](frank-price-qualifier-full-analysis.md) | Analysis (governing) | Price qualifier enforcement scope |
| [frank-price-qualifier-shape-analysis.md](frank-price-qualifier-shape-analysis.md) | Proposal | Price qualifier shape |
| [frank-qualifier-deferred-scoping.md](frank-qualifier-deferred-scoping.md) | Scoping analysis | Qualifier deferral decisions |
| [frank-v2-consistency-review.md](frank-v2-consistency-review.md) | Review | V2 consistency check |
| [frank-when-guard-audit.md](frank-when-guard-audit.md) | Audit (initial) | When-guard surface review |
| [frank-when-guard-audit-2.md](frank-when-guard-audit-2.md) | Audit (follow-up) | When-guard iteration |
| [frank-when-guard-audit-3.md](frank-when-guard-audit-3.md) | Audit (follow-up) | When-guard iteration |
| [frank-when-guard-audit-4-final.md](frank-when-guard-audit-4-final.md) | Audit (final) | When-guard surface lock |

### Compiler / pipeline

| Doc | Status | Promoted to / outcome |
|---|---|---|
| [constraint-refs-proof-plan.md](constraint-refs-proof-plan.md) | Implementation plan (approved) | ConstraintRefs population + SemanticSubjects removal |
| [count-bound-discharge-semantics-2026-06-03.md](count-bound-discharge-semantics-2026-06-03.md) | Promoted 2026-06-04 | `compiler/proof-engine.md § Strategy 10 / § Sequential proof flow / § Obligation Generation Contract` + `precept-language-spec.md §0.6 item 7` + `compiler/diagnostic-system.md` (PRE0136) + `collection-types.md § Constraint Catalog` (BUG-018 count-bound: Reading-A prove-or-reject, guard carrier, dedup floor) |
| [interval-proof-engine-design.md](interval-proof-engine-design.md) | Design + implementation plan | Interval proof engine + Slice 7 obligation generation |
| [overflow-prevention-design-analysis.md](overflow-prevention-design-analysis.md) | Analysis | Numeric overflow prevention |
| [pipeline-audit-fix-plan.md](pipeline-audit-fix-plan.md) | Fix plan | Pipeline-audit-driven fixes |
| [proof-engine-qualifier-audit.md](proof-engine-qualifier-audit.md) | Audit | Proof engine qualifier coverage |
| [proof-engine-remediation-review.md](proof-engine-remediation-review.md) | Remediation review | Proof engine work product |
| [proof-gaps-issues.md](proof-gaps-issues.md) | Issues list | Proof obligation gaps |
| [quantity-normalization-design.md](quantity-normalization-design.md) | Design | Quantity normalization |
| [typed-constants-and-proof-coverage-plan.md](typed-constants-and-proof-coverage-plan.md) | Coverage plan | Typed constants + proof coverage |

### Compiler / proof engine — relational reasoning (Slice 2c-i)

| Doc | Status | Promoted to / outcome |
|---|---|---|
| [relational-rules-and-bounds-design-2026-06-02.md](relational-rules-and-bounds-design-2026-06-02.md) | Promoted 2026-06-04 | `compiler/proof-engine.md § Strategy 4 / § Design Rationale Decision 6` + `precept-language-spec.md §0.6` (relation-as-fact, single-pass/no-fixpoint) |
| [relational-narrowing-core-design-2026-06-03.md](relational-narrowing-core-design-2026-06-03.md) | Promoted 2026-06-04 | `compiler/proof-engine.md § Strategy 4 / § Design Rationale Decision 6 / § Satisfiability` + `precept-language-spec.md §0.6` (FieldToFieldConstraint reuse, guarded-rule drop, depth-1 Intersect, satisfiability isolation) |
| [relational-subject-resolution-discharge-2026-06-04.md](relational-subject-resolution-discharge-2026-06-04.md) | Promoted 2026-06-04 | `compiler/proof-engine.md § Strategy 4` (subject resolution; the §1343/§1356 division-coverage correction) |
| [relational-contradiction-reject-2026-06-04.md](relational-contradiction-reject-2026-06-04.md) | Promoted 2026-06-04 | `compiler/proof-engine.md § Strategy 4 contradiction guard` + `compiler/diagnostic-system.md § Severity` (empty-intersection contradiction; severity sourced from dependent op's Error) |
| [relational-contradiction-discharge-guard-2026-06-04.md](relational-contradiction-discharge-guard-2026-06-04.md) | Historical — superseded | by `relational-contradiction-reject-2026-06-04.md` (settled its three open questions) |
| [relational-fact-representation-2026-06-03.md](relational-fact-representation-2026-06-03.md) | Historical — superseded | by `relational-narrowing-core-design-2026-06-03.md` (scoped too narrowly) |
| [relational-narrowing-core-INDEPENDENT-2026-06-03.md](relational-narrowing-core-INDEPENDENT-2026-06-03.md) | Historical — superseded | by `relational-narrowing-core-design-2026-06-03.md` (merged: blind + review-informed) |

### Tooling — language server, MCP, completions

| Doc | Status | Promoted to / outcome |
|---|---|---|
| [completions-bugs.md](completions-bugs.md) | Promoted | `tooling/language-server.md § 7.3` (qualifier-site resolution) + `compiler/type-checker.md § Typed Records` |
| [elaine-typed-literal-autocomplete-ux.md](elaine-typed-literal-autocomplete-ux.md) | Promoted 2026-05-24 | `tooling/language-server.md § 7.3` (typed-literal completion surface) |
| [kramer-typed-literal-impl-plan.md](kramer-typed-literal-impl-plan.md) | Implementation plan | Typed-literal completion implementation |
| [language-server-implementation-plan.md](language-server-implementation-plan.md) | Implementation plan | LS slices and Slice 10 architectural decision (SemanticTokenTypes catalog) |
| [mcp-dto-free-design.md](mcp-dto-free-design.md) | Design | MCP DTO-free / markdown-first design |
| [syntax-coloring-fix-design.md](syntax-coloring-fix-design.md) | Fix design | Syntax coloring corrections |
| [temporal-businessunit-completions-proposal.md](temporal-businessunit-completions-proposal.md) | Proposal | Temporal / business-unit completion surface |

### Diagnostic UX

| Doc | Status | Promoted to / outcome |
|---|---|---|
| [diagnostic-name-message-review.md](diagnostic-name-message-review.md) | UX review (Elaine) | Diagnostic name/message audit |

### Cross-cutting plans / toolchain

| Doc | Status | Promoted to / outcome |
|---|---|---|
| [precept-toolchain-bugs.md](precept-toolchain-bugs.md) | Bug ledger (archived) | Toolchain bugs at archive time |
| [precept-toolchain-plan.md](precept-toolchain-plan.md) | Plan | Toolchain slices 1-13 |

---

## Maintenance protocol

When archiving a doc:

1. Move the doc from `docs/Working/` to `docs/Working/Archive/` (or create it directly here if completed without promotion).
2. The doc must carry one of:
   - `> **Promoted to:** <canonical-doc-link>` header (preferred — rationale moved to canonical)
   - `> **Status:** Completed — <one-line outcome>` (work landed without canonical-doc promotion)
   - `> **Status:** Historical — <one-line context>` (preserved for archaeology only)
3. Append a row to this index in the relevant topic group. Format: `| [filename.md](filename.md) | <Status> | <Promoted-to / outcome> |`
4. If the topic group doesn't exist, add it alphabetically among existing groups.

The `/lifecycle-5-promote` skill enforces steps 2 + 3 — it refuses to complete an archival without the header and the index update.
