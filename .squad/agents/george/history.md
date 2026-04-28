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

### 2026-04-28 — Round 4 design (George)
- Delivered all 6 tasks from Frank's v3 § 9: slot-ordering drift tests, `_slotParsers` exhaustiveness contract, both-positions guard sample, complete `GetMeta` with `Entries` for all 11 constructs, concrete slot parser signatures (`Func<SyntaxNode?>` confirmed — no boxing, delegate covariance), and Roslyn source generator feasibility spike.
- Flagged pre-event `when` guard in TransitionRow as a language surface expansion requiring spec update + Shane approval (spec explicitly excludes it; no sample uses it).
- Confirmed `->` vs `<-` for computed fields is parser-neutral — no disambiguation benefit, keep `->` for arrow-direction consistency.
- Discovered mandatory pre-disambiguation `when` consumption: `in State when Guard write Field` is spec-legal and requires the disambiguator to consume guards BEFORE checking disambiguation tokens.
- Flagged `RuleDeclaration`'s use of `GuardClause` slot kind as a naming mismatch — the rule body is not a guard.
- Source generator recommendation: lead with test generation (round-trip + error recovery tests are cost-justified now at 11 constructs); defer AST generation to 25-30 constructs.
- Frank locked all six dispositions from Round 2: `LeadingTokenSlot` accepted, `BuildNode` stays an exhaustive switch, ActionChain fix accepted, both `when` guard positions are valid, disambiguation tokens remain explicit metadata, and the migration proceeds with a bridge property.
- Extensibility analysis now has a stable conclusion: catalog-driven parsing removes most parser-layer code for new constructs, while semantic layers remain hand-authored. Generic AST and AST-as-catalog-tree were rejected; source generation is deferred until roughly 25-30 constructs or another consumer justifies it.
- Follow-up focus for Round 4: keep the design grounded in language docs and samples, answer the calculated-field arrow question with lexer/parser evidence, and assess Roslyn source generators primarily as test-generation/sync infrastructure rather than as a reason to genericize the AST.

### 2026-04-28 — Combined design review
- Confirmed the combined compiler/runtime draft is architecturally sound but still needs explicit SyntaxTree inventory, parser/type-checker contract text, expression grammar coverage, and an explicit anti-Roslyn-bias statement.
- Locked the implementation bias to tree-walk evaluation, flat keyword-dispatched parsing, semantic-table-driven checking, and dispatch-optimized lowered runtime models.

### 2026-04-28 — Language surface simplification analysis (Round 5)
- Analyzed all 28 sample files for `when` guard usage patterns. Found pre-verb `when` is used by exactly 2 access mode lines; every other guard (45+ transition rows, rules, ensures) is post-verb or post-expression.
- Discovered spec/sample misalignment: spec grammar shows pre-`ensure` guards, but all 3 guarded ensures in samples use post-expression guards (matching rule syntax). No sample uses the pre-`ensure` position.
- Proposed moving access mode guards to post-field position (`in State write Field when Guard`). Combined with spec alignment for ensures, this eliminates the disambiguator's pre-disambiguation `when` consumption entirely — the core complexity source.
- Rejected 4 language changes that look helpful but don't help: different keywords for `from`-led constructs, punctuation separators, different arrows for actions/outcomes, mandatory guard parentheses. All solve non-problems or break design principles.
- Rejected G5 (rule body intro token) as a language change — solvable in implementation with a `RuleBody` slot kind.

## Learnings (2026-04-27 — Round 6)
- Frank R5 decisions all confirmed: F7 CS8509 switch, F8 RuleExpression no intro token, F9 pre-event guard withdrawn, F10 separate slots, validation layer tier design
- Test nit: BuildNodeHandlesEveryConstructKind may hit NullReferenceException before ArgumentOutOfRangeException — assertion needs to distinguish
- Language change (pre-verb vs post-field guard) pending Shane input — single highest-leverage parser simplification
- Design declared stable pending language decision

## Learnings (2026-04-28 — Access mode feasibility)
- Access mode guard feasibility assessment (A1 guarded read, A2 guarded omit, A3 Option B vocabulary) published to `docs/working/george-access-mode-feasibility.md` on 2026-04-28.
- Key finding: A1 (guarded read) is feasible — symmetric mechanism to guarded write, greenfield type checker, two catalog gap fixes needed regardless. A2 (guarded omit) is categorically infeasible — conditional schema membership breaks every pipeline stage and violates the structural impossibility guarantee. A3 (Option B vocabulary) is feasible with caveats — writable token picks up a three-role burden that needs explicit catalog documentation.
- One unresolved question flagged for Shane: should unguarded write + guarded read on the same (field, state) pair be a conflict or a valid refinement? Type checker design cannot proceed without this decision.
### 2026-04-28 — Access mode backlog canonicalized
- Feasibility findings are now durably recorded: guarded `read` is feasible only as a writable-baseline downgrade, guarded `omit` remains structurally invalid, and vocabulary alternatives were explored but not adopted as canonical surface.
- Follow-through from the merge: dead named-field access declarations now collapse to `RedundantAccessMode`, the parser lane stays validation-first rather than generator-first, and George's rule-7 conflict/refinement question remains unresolved.
