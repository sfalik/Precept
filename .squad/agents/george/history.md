## Core Context

- Owns code-level feasibility, runtime implementation detail, and architecture-to-code translation across parser, checker, analyzer, and tooling surfaces.
- Co-owns language research/design grounding with Frank and turns approved decisions into implementable structures.
- Historical summary: led implementation and feasibility passes for keyword logical operators, narrowing/proof work, computed fields, analyzer expansion, and related diagnostic/runtime follow-up.

## Learnings

- Analyzer infrastructure must follow real Roslyn operation shapes. Constructor arguments are the happy path; object initializers, spreads, and followed field initializers are the edge cases that decide helper API quality.
- The biggest implementation payoff is removing parallel copies of catalog knowledge (`OperatorTable`, widening checks, completion lists, parser/checker mapping tables).
- Precept.Next quality depends on doc/code contract alignment first. Hollow models, missing diagnostics, and stale docs block trustworthy slice work faster than raw implementation effort does.
- The architecture only stays coherent when three splits remain explicit at once: `CompilationResult` vs. `Precept`, constraints vs. faults, and authoring consumers vs. execution consumers.
- Typed action naming is fixed: `TypedAction`, `TypedInputAction`, and `TypedBindingAction`. Anything looser invites fresh naming drift.
- Parser vocabulary should derive from catalog metadata, but grammar mechanics should stay hand-written unless the catalog can express the routing contract without hiding invariants.
- Named local index constants inside exhaustive `BuildNode` switch arms are the right defense against slot-order drift; registry-like structures and exhaustive pattern matches should use different data structures for different invariants.

## Recent Updates

### 2026-04-27 — Catalog-driven parser review and estimate
- Re-estimated the parser lane: vocabulary tables are the immediate win; clean-slate full catalog-driven parsing is viable but only if the design keeps correctness guardrails visible.
- Found two real bugs in Frank's v1: ActionChain/Outcome needed peek-before-consume, and `write all` needed `LeadingTokenSlot` injection so the consumed `Write` token could also satisfy slot content.
- Flagged the catalog migration requirement: move from single `LeadingToken` to `Entries` with a `PrimaryLeadingToken` bridge so downstream consumers can migrate cleanly.

### 2026-04-28 — Frank Round 3 sync
- Frank locked all six dispositions from Round 2: `LeadingTokenSlot` accepted, `BuildNode` stays an exhaustive switch, ActionChain fix accepted, both `when` guard positions are valid, disambiguation tokens remain explicit metadata, and the migration proceeds with a bridge property.
- Extensibility analysis now has a stable conclusion: catalog-driven parsing removes most parser-layer code for new constructs, while semantic layers remain hand-authored. Generic AST and AST-as-catalog-tree were rejected; source generation is deferred until roughly 25-30 constructs or another consumer justifies it.
- Follow-up focus for Round 4: keep the design grounded in language docs and samples, answer the calculated-field arrow question with lexer/parser evidence, and assess Roslyn source generators primarily as test-generation/sync infrastructure rather than as a reason to genericize the AST.

### 2026-04-28 — Combined design review
- Confirmed the combined compiler/runtime draft is architecturally sound but still needs explicit SyntaxTree inventory, parser/type-checker contract text, expression grammar coverage, and an explicit anti-Roslyn-bias statement.
- Locked the implementation bias to tree-walk evaluation, flat keyword-dispatched parsing, semantic-table-driven checking, and dispatch-optimized lowered runtime models.
