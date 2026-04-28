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
- PRECEPT0007 enforces exhaustiveness on DiagnosticCode.GetMeta; a DiagnosticCode member without a catalog arm is a build error. Any time a new DiagnosticCode is added, its Diagnostics.GetMeta arm must be added in the same commit or the build breaks.
- Slot additions to a construct's sequence must include a regression test that pins the slot kind AND its position (last slot guard). Tests that only assert "guard slot exists" don't catch ordering regressions — assert `slots.Last().Kind == GuardClause` as well.

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

### 2026-04-28 — Parser complexity re-evaluation (when-move and `->` proposal)
- Confirmed: Parser.cs is a pure stub (`throw new NotImplementedException()`). All complexity analysis is design-level, not runtime code.
- The when-post-field 6→4 step reduction (v5 claim) is accurate. Proof is in the catalog: AccessMode slot sequence has no GuardClause, meaning when the parser is written the disambiguator will never stash a guard for access modes.
- `->` proposal adds no parser benefit. Arrow is already a full token (`TokenKind.Arrow`, TwoCharOperators). The `-> F ADJ` shape moves AccessModeKeyword from a free LeadingTokenSlot injection to an explicit third slot parser step. Net complexity: same to marginally worse. Frank's B3 semantic coherence objection is reinforced, not contradicted, by the parser analysis.
- `F ADJECTIVE` order (adjective after field) creates zero new lookahead — keywords and identifiers are lexically distinct token kinds.
- Surfaced a pending catalog gap: AccessMode is missing SlotGuardClause at end of slot sequence. Must be added before parser implementation for that construct begins.
- Full analysis: `docs/working/george-parser-complexity-when-analysis.md`

### 2026-04-28 — Parser complexity re-evaluation under `->` grammar and omit/access-mode split
- Re-evaluated the `when`-post-field complexity finding (v5-lang-simplify) against Shane's new `->` operator grammar and the proposed omit/access-mode semantic split.
- Finding 1: `when`-post-field finding fully holds and is strengthened. Under `in State -> Field ADJECTIVE [when Guard]`, the guard is even further from the disambiguation point. 4-step disambiguator unchanged.
- Finding 2: `->` grammar is marginally simpler than the settled verb-before-field grammar. Disambiguation token changes from a vocabulary keyword (identifier-space) to a punctuation operator — single token, no collision risk, immune to vocabulary evolution. Field/adjective boundary handled lexically by keyword classification; no lookahead ambiguity.
- Finding 3: omit/access-mode split eliminates the internal conditional-guard-slot branch from the current unified production. Two structurally flat productions (`OmitDeclaration`, `AccessModeDeclaration`) are routed by distinct tokens (`omit` keyword, `->` operator) — simpler than one production with asymmetric internal structure.
- Net verdict: proposed grammar is the simplest of the three shapes evaluated (original > settled > proposed). All improvements are structural, not just incidental.
- One AST design question flagged for Shane: separate `OmitDeclaration` + `AccessModeDeclaration` nodes vs. one unified `AccessDeclaration` node. No parser complexity impact but affects type-checker and catalog shape.
- Analysis written to `docs/working/catalog-parser-design-v5-lang-simplify.md` § Addendum and `.squad/decisions/inbox/george-parser-complexity-reeval.md`.

### 2026-04-28 — AccessMode guard slot catalog fix
- Fixed `ConstructKind.AccessMode` slot sequence in `src/Precept/Language/Constructs.cs`: added `SlotGuardClause` as the final slot. Bug was confirmed: parser generation from catalog would have silently omitted guard support for access modes.
- Fixed companion pre-existing build error: `DiagnosticCode.RedundantAccessMode` was in the enum but had no `Diagnostics.GetMeta` arm. PRECEPT0007 Roslyn analyzer was treating this as a compile error. Added the missing arm with `Severity.Warning` (hygiene issue, not structural violation).
- Also cleaned up the stale `write all` reference in the AccessMode description (language feature removed per April 28 owner directive).
- Added regression test `AccessMode_HasGuardClauseAsOptional` that pins both presence and position of the guard slot (slots.Last() assertion guards slot-order drift).
- All 1809 tests pass.


### 2026-04-28 — Access-mode vocabulary migration (B4)

- Implemented the full B4 vocabulary lock: `Write`/`Read` retired from `TokenKind`, replaced by `Modify`, `Readonly`, `Editable`.
- `ConstructKind.OmitDeclaration` added as a separate construct (no guard slot, just StateTarget + FieldTarget). `AccessMode` now uses leading token `In` (was `Write`), slot order changed to StateTarget → FieldTarget → AccessModeKeyword → GuardClause (verb `modify` consumed by disambiguator, not stored).
- `Modifiers.cs`: `ModifierKind.Write` now references `TokenKind.Editable`; `ModifierKind.Read` references `TokenKind.Readonly`.
- All 17 sample access-mode lines across 14 files migrated. Comma-separated field lists split into separate declarations.
- Key surprise: `src/Precept/Language/ConstructSlot.cs` description for `AccessModeKeyword` said `write | read | omit` — updated to `readonly | editable`.
- 1817 + 207 = 2024 tests pass after all changes.
- Cross-surface: Kramer must re-run the grammar generator; Newman has no DTO work (catalog-derived tokens surface automatically through `Tokens.All`).
- Spec reserved-keywords list still has stale `write`/`read` entries alongside new tokens — flagged as cleanup debt, not blocking.

### 2026-04-28 — Shorthand and AST directives synced
- Shane locked shared `FieldTarget` shorthand for both `modify` and `omit`, including comma-separated lists and `all`.
- `AccessModeDeclaration` and `OmitDeclaration` stay separate AST node kinds; `omit` has no guard slot.
- Frank's doc sweep updated the live parser/spec/runtime docs, and any split-field sample simplifications are superseded by the shorthand-preservation directive.
