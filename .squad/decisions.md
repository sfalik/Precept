# Squad Decisions

---

## ACTIVE DECISIONS — Current Sprint

---

### 2026-04-12T01:53:16Z: Issue #65 — Event action hooks for stateless precepts — PROPOSAL FILED
**By:** Frank (research + design alignment), George (runtime impact), Steinbrenner (PM), Coordinator (filing)
**Status:** Proposal filed — branch `research/event-hooks-proposal`, Issue #65 open

Shane surfaced a parse error attempting `on Advance -> set Count = Count + 1` in a stateless precept. The team investigated, confirmed a principled design gap, and filed a formal proposal.

**Gap verdict (Frank):**
- The `on <Event> -> <ActionChain>` form does not exist in the grammar. Parse fails with "Expected: 'on \<Event\> assert '"." This is a deliberate exclusion — Principle 7 prohibits shared event-level context in stateful precepts (invisible across rows). However, stateless precepts have no transition rows, so Principle 7 has no application surface. The stateless case is a **principled gap**, not a principled exclusion.
- Workarounds exist (duplication across rows; `to any ->` state hook) but are semantically non-equivalent.
- Full research: `research/language/expressiveness/event-hooks.md`

**External precedent (Frank — 5 systems surveyed):**

| System | Flat event hook? | Closest analog |
|---|---|---|
| XState v5 | Not at flat level | Hierarchical parent `on:` only |
| SCXML | No (parentstate wildcard only) | §3.13: exit → **transition content** → entry |
| Akka Classic FSM | No | `whenUnhandled` = fallback only |
| Spring SM | Via listener | Post-transition, observational only |
| Redux | Yes — pure event bus | Pre-reducer middleware |
| **Precept (proposed)** | **Issue A — stateless only** | After asserts, before invariants |

No surveyed system has a flat stateless event hook with typed args and post-mutation constraint enforcement. This is Precept-native territory.

**Split verdict:**
- **Issue A (stateless):** CONFIRMED VIABLE. Zero Principle 7 tension. Locked execution order (after asserts, before invariants). C49 revision required in same PR.
- **Issue B (stateful):** NOT BLOCKED but 3 open questions must be locked first: (1) execution order position — 4 options with different semantics, Frank recommends Option 3 (after row mutations, before exit actions) per SCXML §3.13; (2) outcome-scoping (fires on Unmatched?); (3) explicit Principle 7 exception rationale. **Labeled `deferred`.**

**C49 revision decision (Steinbrenner + Frank):**

| Case | C49 behavior |
|---|---|
| Event declared, no asserts, no hooks | C49 Warning: "Event 'X' has no effect — it will return `Undefined`" |
| Event declared, has asserts, no hooks | C49 Warning (revised, lower severity): "Event 'X' validates arguments but has no action effect" |
| Event declared, has at least one hook | **C49 suppressed** |
C49 revision ships in the same PR as the runtime and grammar changes (language-surface-sync requirement).

**Runtime impact (George):**
- **Parser:** Small. `EventActionDecl.Try()` before `EventAssertDecl.Try()`. New `EventActionResult` record. Reuses `ActionChain` unchanged.
- **Model:** Extra-small. New `PreceptEventAction` record.
- **Type checker:** Medium. C49 suppression + scope validation.
- **Engine:** Medium. Hard-abort semantics for hook constraint violations recommended.

**PM decisions (Steinbrenner):**
- Issue A advances on current roadmap wave. Issue B deferred, not abandoned.
- C49 revision is in-scope for Issue A, not a follow-up.
- Stateless event hooks do not change stateless precept's position as first-class entity model — they complete it.

---

### 2026-04-10: PR quality policy — detailed implementation plans + live checkbox updates
**By:** Shane (owner directive, via Coordinator)
**Status:** Standing policy — enforced from 2026-04-10 forward

Every implementation PR must have a detailed implementation plan with granular checkboxes. Checkboxes must be kept current after every push to the branch.

**Policy:**
1. **PR creation:** Coordinator reads `.squad/skills/pr-implementation-plan/SKILL.md` before writing any PR body. Required sections: What this does, Design decisions locked, Implementation checklist (by component), Critical review focus.
2. **Checklist granularity:** Each checkbox names a file, behavior, or specific case. "Type checker: handles nullable" is too vague. "C56: fires for nullable field in invariant scope (separate symbol injection path from guard scope)" is correct.
3. **Checkbox maintenance:** Scribe updates PR checkbox state after every agent work batch. Scribe reads `.squad/skills/pr-implementation-plan/SKILL.md` before performing checkbox updates.
4. **CatalogDriftTests footgun:** Always use `-> no transition` for compile-phase constraint triggers in guards.

**Artifacts created:** `.squad/skills/pr-implementation-plan/SKILL.md`. Scribe charter updated.

---

### 2026-04-10: Issue #10 — Three-level dotted form (`A.B.C`) included in PR #56 scope
**By:** Shane (via Coordinator)
**Status:** Implemented — George's changes landed in `squad/10-string-length-accessor`

Decision to include `A.B.C` three-level dotted identifier support (e.g. `Submit.Name.length`) in PR #56 rather than a separate PR.

**Frank's invasiveness assessment:** Low-Medium (~54 lines, 7 files). Test scaffolding already existed in `StringAccessorTests.cs`.

**Implementation sites (George):**
- `PreceptModel.cs` — `string? SubMember = null` on `PreceptIdentifierExpression`
- `PreceptParser.cs` — `DottedIdentifier` extended with optional second dot chain; `ReconstituteExpr` updated
- `PreceptExpressionEvaluator.cs` — three-level guard at top of `EvaluateIdentifier`
- `PreceptTypeChecker.cs` — 4 sites: `TryInferKind` key, C56 nullable-arg extension (critical: base key is `$"{Name}.{Member}"` not `Name`), `BuildSymbolKinds` arg loop, `BuildEventAssertSymbols` loop
- `PreceptAnalyzer.cs` — 3-level prefix detection in LS completions

**Critical gotcha (Frank's review, George's implementation):** C56 base-kind lookup must use `$"{identifier.Name}.{identifier.Member}"` for the three-level case, not `identifier.Name` (which resolves to the event name and silently skips C56 for nullable args).

---

### 2026-04-10: Frank — Expanded modifier design space — decisions requiring owner input
**By:** Frank (Lead/Architect)
**Status:** Owner decision required — 5 open questions

Second pass at `research/language/expressiveness/structural-lifecycle-modifiers.md` identified 4 open design questions requiring Shane's direction before any modifier implementation:

1. **Modifier role scope:** Option C recommended — modifiers must be either compile-time verifiable OR tooling-actionable. `sensitive`/`audit` as pure annotation need a different mechanism.
2. **Event modifier taxonomy correction:** Event structural properties (source scope, outcome shape, target scope) are fully compile-time provable. Event behavioral properties (firing history) are not. Domain taxonomy for #23 should reflect this subcategorization.
3. **`sealed after <State>` grammar form:** New modifier form with state reference — requires architectural sign-off before implementation. Multi-state sealing concern.
4. **Modifier stacking semantics:** Are incompatible combinations compile-time validated or accepted with runtime checks? Ordering convention?
5. **Implementation sequence confirmed:** `terminal` → event modifiers (`entry`/`advancing`/`settling`/`isolated`) → `writeonce` → `sealed after <State>` → `guarded` on states.

No philosophy gap. Modifier space extends graph-topology annotation, not entity category or guarantee model.

---

### 2026-04-11: Wave 3 — MCP vocab decisions (integer/decimal/choice)
**By:** Newman (MCP/AI Dev)
**Status:** Applied — branch `feature/wave3-integer-decimal-choice`

**Decision 1:** `LanguageTool` requires zero code changes for `integer`/`decimal`/`choice`/`maxplaces`/`ordered` — all catalog-driven via `[TokenCategory]` attributes. Canonical sync check: token has `[TokenSymbol]` + `[TokenCategory]` → all downstream layers pick up automatically.

**Decision 2:** `round()` is registered in `ConstructCatalog` via `.Register(new ConstructInfo(...))` on the `RoundAtom` parser combinator, not hardcoded in `LanguageTool.cs`. Surfaces in `precept_language.constructs`.

**Decision 3:** `FieldDto.IsOrdered` is `bool?` — only populated when `true`. Absent from JSON when false (reduces noise for non-choice fields).

**Decision 4:** `McpServerDesign.md` updated with 3 new reference tables: scalar type reference (integer/decimal/choice), full constraint keyword reference, built-in function reference for `round()`.

---

### 2026-04-11: Wave 3 — Integer type constraint desugaring bug (soup-nazi)
**By:** Soup Nazi (Tester)
**Status:** Bug filed — fix required before integer constraints are considered complete

**Critical bug:** `nonnegative`, `positive`, `min N`, `max N` constraints on `integer` fields are accepted by the parser but NEVER desugared to runtime invariants. `BuildScalarConstraintExpr` in `PreceptParser.cs` has branches for `Number` and `String` only — `Integer` falls through to `return (string.Empty, null, string.Empty)`. Constraints are silently ignored at runtime.

**Fix required (George):** Add `Integer` branch to `BuildScalarConstraintExpr`. Use `PreceptLiteralExpression(0L)` for nonneg/positive (long literal). Cast `mn.Value`/`mx.Value` to `long` for min/max.

**Test file:** `PreceptIntegerTypeTests.cs` — 31 tests pass, 3 skipped pending fix.

---

### 2026-04-10: Issue #13 — Grammar + completions tooling decisions
**By:** Kramer (Tooling Dev)
**Status:** Applied — grammar and completions changes committed

**Grammar:** New `constraintKeywords` repository entry matching all 9 constraint keywords (`nonnegative`, `positive`, `notempty`, `min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`). Scope: `keyword.other.precept`. Inserted before `identifierReference` catch-all. Added to `fieldScalarDeclaration` capture 9 and `eventWithArgsDeclaration` capture 8. No conflict with `collectionMemberAccess` `.min`/`.max` (dot-gated).

**Completions:** Pattern-match `beforeCursor` for field/event-arg declaration context. Type-split by scalar type (number/string/boolean/collection) to return appropriate keyword set. Static items: `NumberConstraintItems`, `StringConstraintItems`, `CollectionConstraintItems`.

---

### 2026-04-11: Wave 3 — Integer/decimal/choice grammar + completions decisions
**By:** Kramer (Tooling Dev)
**Status:** Applied — branch `feature/wave3-integer-decimal-choice`, commit `73092a9`

1. **Integer constraint tier reuses `NumberConstraintItems`** — same vocabulary as `number`. No separate array.
2. **`choice(...)` surfaces as snippet** in `TypeItems` (`insertText = choice("${1:A}", "${2:B}")`), not bare `TypeParameter`. Bare `choice` is invalid DSL.
3. **`fieldScalarDeclaration` pattern updated** for `integer`/`decimal` only, not `choice(...)` (argument form incompatible with word-boundary alternation).
4. **`round(expr, N)` in `ExpressionOperatorItems`** (all expression contexts) — type-filtered injection requires guard type inference, out of scope; snippet in all positions is acceptable and correct.

---

### 2026-04-11T00:00:00Z: Issue #14 — `when <guard>` on declarations — FINAL DESIGN APPROVAL, all 4 forms, implementation scope locked
**By:** George (runtime), Kramer (tooling), Newman (MCP), Soup Nazi (testability), Frank (design/architecture)
**Status:** APPROVED — all 4 forms cleared for implementation in a single wave.

Design review of Issue #14 (`when <guard>` conditional invariants, state asserts, event asserts, conditional edit eligibility) completed in two passes. Initial review (Frank `frank-issue14-design-review.md`) cleared Forms 1–3 and deferred Form 4 as a scoping question. Second pass (`frank-issue14-form4-design.md`, `george-issue14-form4-simplicity.md`) confirmed the additive approach resolves the structural obstacle. Final approval round (`frank-issue14-final-approval.md`, `george-issue14-implementation-scope.md`, `kramer-issue14-final-tooling.md`, `newman-issue14-final-dtos.md`, `soup-nazi-issue14-final-tests.md`) — all 5 reviewers filed, all approved, all 4 forms same wave.

**Implementation scope: ~163 lines across 5 files, 19 change sites.**

**Frank — APPROVED all 4 forms (final verdict):**
- All 4 forms semantically unified: `when <guard>` means "conditional on this boolean condition" — constraint-skip semantics (Forms 1–3) and permission-grant semantics (Form 4) follow naturally from declaration kind, not syntax. Teaching model holds uniformly.
- Fail-closed edit guards are non-negotiable: guard evaluation error → field not granted. Permission-grant systems must default to deny on uncertainty. Must be explicitly contracted in the issue body with named tests `Update_GuardThrows_FieldNotGranted` and `Inspect_GuardNull_FieldNotGranted`.
- `in any when <guard>` pre-expansion is correct: expand to one `PreceptEditBlock` per declared state at construction, each carrying the same `WhenGuard`. Runtime evaluates `block.State == currentState && evaluateGuard(block.WhenGuard, instanceData)`. No sentinel handling at evaluation sites.
- All six locked language decisions from the initial review upheld. Form 4 guard is field-scoped only — arg references rejected with C69.

**George — implementation scope confirmed:**
- **File 1: `PreceptModel.cs`** — +8 lines. Add optional `WhenGuard?`/`WhenText?` tail parameters to `PreceptInvariant`, `StateAssertion`, `EventAssertion`, `PreceptEditBlock`. Pattern identical to existing `PreceptTransitionRow`. Zero call-site breakage (named defaults). Must land first.
- **File 2: `DiagnosticCatalog.cs`** — +8 lines. Register C69 (cross-scope guard reference — better message than C38). Pure additive.
- **File 3: `PreceptParser.cs`** — +16 lines. 4 injection points via existing `OptionalWhenGuardParser` (1 line each). `EditDecl` guard injected between `StateTarget` and `Edit` token. Extend `EditResult` and `StateAssertResult` private records (+2 fields each). Update `AssembleModel` cases for both (+2 lines each). `EditDecl.Try()` before `StateAssertDecl.Try()` ordering verified correct.
- **File 4: `PreceptTypeChecker.cs`** — ~5 distinct changes. B1 narrowing fix: add `WhenGuard is null` filter in `BuildStateAssertNarrowings` (1-line critical prerequisite, must land first as its own commit). Guard scope validation for all 4 forms (C69 emission). C29/C30 guard-against-defaults pre-check. Form 4 guarded-edit evaluation pass (second pass after static `_editableFieldsByState` fast path — additive, untouches existing unconditional path).
- **File 5: `PreceptAnalyzer.cs` (language server)** — 13 changes (~33–40 lines). See Kramer.
- **Confirmed non-changes:** tokenizer, expression evaluator, `_editableFieldsByState` fast path, `IsSynthetic` contamination path, statement union ordering — all verified unchanged.

**Kramer — zero grammar changes, 13 completions changes (~33–40 lines in `PreceptAnalyzer.cs`):**
- Grammar: `when` already in `controlKeywords` without positional anchor. `in State when guard edit` parse is handled by existing individual keyword patterns. **Zero grammar file changes for any of the 4 forms.**
- New static `WhenItem` added alongside `BecauseItem`.
- Forms 1–3: 2 new branches + 1 modification per declaration block (A1/A2/A-modified, C1/C2/C-modified, E1/E2/E-modified). All `when`-bearing branches inserted before base branches (strictly more specific — safe ordering).
- Form 4: 4 new branches for the `in <State> when <guard> edit` sequence — after `in State` → offer `[assert, edit, when, ->]`; after `in State when` → offer field names + `not`; after `in State when <guard>` → offer `edit`; after `in State when <guard> edit` → offer field names.
- Branch ordering critical throughout — new branches precede their less-specific base branches.

**Newman — 4 new DTO arrays in `precept_compile` output (B2 — parallel prerequisite):**
- New: `invariants` (exclude `IsSynthetic`), `stateAsserts`, `eventAsserts`, `editBlocks` — each a top-level array with `expression`, `when: string | null`, `reason`, `line` shape. `stateAsserts` adds `anchor` and `state`. `editBlocks` adds `state` and `fields`.
- `StateDto.rules: string[]` preserved alongside new arrays — not replaced.
- `when` fields ship as `null` until George's model record changes land (Commit B). DTOs are structurally ready; wire-up is a 1-line change per array once model records have `WhenText`.
- `precept_inspect` structured constraint trace: `constraintTrace` top-level key per-state/per-event showing which guards evaluated true/false and which assertions fired. Prerequisite for form-by-form tracing in agent workflows.

**Soup Nazi — ~154 tests (6 test files):**
- `NewSyntaxParserTests.cs`: +5 (→ 36 total)
- `PreceptTypeCheckerTests.cs`: +7 (→ 33 total)
- `PreceptWorkflowTests.cs` / `NewSyntaxRuntimeTests.cs` / `PreceptRulesTests.cs`: Forms 1–3 runtime, unchanged (38 tests — regression gate)
- `GuardedEditTests.cs`: 22 new tests (Form 4 core). Key: fail-closed (`Update_EditGuard_EvaluationError_FieldNotEditable`), `in any` expansion (3 tests), union semantics (3 tests), state-filter-before-guard (1 test), data-driven guard flip (1 test).
- `Precept.LanguageServer.Tests/`: +3 completions (→ 10)
- `Precept.Mcp.Tests/CompileToolTests.cs`: +2 (→ 5); `InspectToolTests.cs`: +2 (→ 5). Both block on Newman B2.
- **EC-3 is the ordering gate:** `Check_Invariant_WhenGuardFalse_AtDefaultData_NoPrecompileViolation` must pass before runtime tests run.

**Prerequisites and sequencing:**
1. **B1 narrowing fix** — 1-line commit to `BuildStateAssertNarrowings` (`WhenGuard is null` filter). Must land first, standalone commit.
2. **Newman DTO additions** — `precept_compile` structured arrays (B2). Parallel with B1. Must land before MCP tests.
3. Model records → parser → type checker → runtime (Form 4 additive pass) → completions, in dependency order.

**Key spec requirements locked:**
- Fail-closed edit guards (Frank F2 — explicit issue-body contract required)
- `in any when <guard>` pre-expansion at construction (Frank F3 / George OQ-2)
- C69 diagnostic for cross-scope guard references (George / Kramer)
- EC-3 pre-check ordering gate (Soup Nazi)
- `StateDto.rules` preserved alongside new DTO arrays (Newman)

**Elaine coordination point:** Dynamic editability UX — guarded edit blocks produce per-instance editability rather than a static per-state set. Elaine's preview inspector and any UI surface showing editable fields must handle guard-conditional editability. Coordinate before Elaine's inspector work begins.

---

### 2026-04-10T21:00:00Z: Issue #10 — String `.length` accessor — fully implemented
**By:** Frank (design analysis), George (runtime + evaluator), Kramer (grammar + completions), Soup Nazi (tests), Coordinator (integration)
**Status:** Implemented — branch `squad/10-string-length-accessor`, 800 tests passing

String `.length` accessor fully ships as Issue #10. All design decisions locked.

**Design decisions (from Frank's analysis, approved by Shane):**

- **Q1: Unicode semantics → UTF-16 code units.** `.length` returns `string.Length` (UTF-16 code units), matching .NET. `"💀".length == 2`. Ties Precept string semantics to the host platform for predictability and O(1) performance. Documented in `docs/PreceptLanguageDesign.md`.
- **Q2: New diagnostic code C56.** `.length` on a nullable string without null narrowing emits **C56** — not C42. Separates "accessor on nullable type without null guard" from C42 ("nullable assigned to non-nullable target"). Null narrowing via `!= null and` or `== null or` removes C56.

**Runtime + evaluator (George):** `.length` evaluator added alongside `.count` pattern. Type checker builds `{field}.length` as `StaticValueKind.Number` in all 4 scope types. C56 registered and emitted in the type-checking pass.

**Grammar + completions (Kramer):**
- `length` added to the `collectionMemberAccess` alternation in grammar alongside `count|min|max|peek`. Comment updated to cover both collection and string accessors.
- String-field branch added inside existing `collectionMemberPrefixMatch` block in `PreceptAnalyzer.cs`. Uses `info.FieldTypeKinds & StaticValueKind.String`. `BuildStringMemberItems(fieldName, isNullable)` returns `.length` as `CompletionItemKind.Property`, detail `"number"`. Nullable strings inject null-guard reminder.
- Build: 0 warnings, 0 errors. 87 language server tests pass.

**Tests (Soup Nazi — `StringAccessorTests.cs`):** 25 tests: parser (2), type checker valid (2), type checker type errors (3), C56 nullable (1), C56 narrowing (2), runtime UTF-16 contract (4), null guard compound (4), invariant context (2), event assert context (2), guard routing (2), regression .count (1). UTF-16 emoji test and three-level dotted form (`Submit.Name.length`) are first-class coverage.

---

### 2026-04-10: Policy — Coordinator must enforce draft PR gate before implementation work
**By:** Shane (owner directive)
**Status:** Captured, applied to `.github/agents/squad.agent.md`

Coordinator is accountable for ensuring a draft PR exists before any implementation work is routed to agents. Missed for issue #31 — branch was created and exploratory work started with no PR opened. Coordinator must verify branch + PR existence at start of any "work on issue N" request and open the draft PR itself if missing.

**Basis:** CONTRIBUTING.md §3 ("open a draft PR immediately"). Recovery pattern: if `mcp_github_create_pull_request` fails due to no commits ahead of base, push empty chore commit first, then retry. See issue #31 recovery (2026-04-10) as canonical example.

**Remediation:** `.github/agents/squad.agent.md` updated with explicit "Implementation Gate — Draft PR Required" section.

---

### 2026-04-10: Issue #13 — Constraint consistency analysis v2 — No redesign needed
**By:** Frank (Lead/Architect)
**Status:** Analysis complete — no redesign needed. Supersedes v1 (rejected for insufficient research grounding).

Shane's question: "How does this contrast with how we apply constraints to events and states? I don't like that it is not consistent."

**Verdict:** The asymmetry is justified scope segregation, not inconsistency. The constraint surface has two tiers:
1. **Type-shape tier** (field-local, closed vocabulary): keyword constraints on fields and event args
2. **Business-rule tier** (cross-field, open expression language): predicate invariants and asserts on states, events, and global scope

**Research grounding (7-category, 30+ system precedent survey):** Zero systems use a single mechanism for both type-shape and cross-entity constraints. States have no type shapes to constrain — state asserts are always bespoke cross-field business rules with no closed keyword vocabulary. `nonnegative`/`min`/`max` address 84% of invariants (46/55 in the corpus), all type-shape bounds. Field constraint suffixes desugar to invariants — one mechanism with two authoring surfaces, connected by the Pombrio & Krishnamurthi resugaring framework with preserved diagnostic fidelity.

**Three unification paths evaluated and rejected:** all-predicate (surrenders most measurable verbosity reduction), all-keyword (states lack type shapes), unified where-clause (still dual mechanism, higher verbosity).

**Action item (non-blocking):** #13 proposal should include an explicit scope-segregation rationale section. See `constraint-composition-domain.md`, `constraint-composition.md`, `internal-verbosity-analysis.md`, `expression-language-audit.md`.

---

### 2026-04-10: Frank — Philosophy refresh assessment — 32 research files surveyed
**By:** Frank (Lead/Architect)
**Status:** Filed — awaiting owner direction on refresh order and standalone governance-vs-validation document

Assessment at `docs/research/language/philosophy-refresh-assessment.md`. 32 language research files reviewed against rewritten `docs/philosophy.md`.

**Key findings:** (1) 0/32 files use "governed integrity" — the philosophy's unifying principle. (2) 14/32 do not consider data-only/stateless entities. (3) 2 files need significant refresh: `xstate.md` (outdated product description), `fluent-validation.md` (missing governance-vs-validation distinction — most important positioning claim for the most commercially important comparison). (4) 4 new research gaps: stateless constraint patterns, stateless Inspect/Fire semantics, governance-vs-validation evidence document, MDM/industry-standard positioning depth.

**Owner decision needed:** confirm refresh order (recommended start: `fluent-validation.md`, `xstate.md`, `data-only-precepts-research.md`), batch vs. per-file, and whether `governance-vs-validation.md` is standalone or folded into existing file.

---

### 2026-04-10: Frank — Structural lifecycle modifiers — New research domain #23
**By:** Frank (Lead/Architect)
**Status:** Filed — P4 horizon, research-only

Structural lifecycle modifiers (`terminal`, `required`, `transient`, etc.) constitute new domain #23, distinct from constraint composition (#8, #13, #14), static reasoning expansion, and entity modeling (#17, #22). These constrain *graph topology*, not field values. Closest relative: state machine expressiveness, but annotate existing graph features rather than adding new ones.

Document at `research/language/expressiveness/structural-lifecycle-modifiers.md`. Added to `domain-map.md` as domain #23 and to the horizon list in `README.md`. Priority P4 — research complete; `terminal` is Tier 1 ready for proposal when demand arises. No code changes.

---

### 2026-04-10: Research roadmap decisions — Milestones, critical path, type system split
**By:** Frank (Architect), Steinbrenner (PM), Peterman (Brand/DevRel) — approved by Shane
**Status:** Accepted

**Three milestones for language expansion roadmap:**
- **M1 "Governed Integrity"**: #31 + #22 + #13 — category-proving milestone
- **M2 "Full Entity Surface"**: #8 + #14 + #29 + #25 + #11 — production-credible milestone
- **M3 "Expression Power"**: #26 + #27 + #16 + #9/#10/#15 + #17 — self-sufficiency milestone

**Critical path:** #31 → #22 → #13 → (#8+#14) → (#29+#25) → #11 → #16 → (#10,#15,#9) → (#26+#27) → #17

**Type system split:** integer (#29) + choice (#25) ship in M2; date (#26) + decimal (#27) defer to M3 (benefit from #16 functions shipping first). **Absorb (#11) promoted** from P3 to late M2 (#1 verbosity pattern, 132 instances, gated on research pass). **Brand sequencing constraint (Peterman):** #22 (data-only) must ship before any type expansion — shipping types on workflow-only precepts reinforces "state machine tool" perception. After M1: update README hero to side-by-side (lifecycle + data-only precept). VS Code extension description to lead with governance identity: "Govern entity integrity — fields, constraints, lifecycle rules — in a single .precept file." **Missing proposal:** file new proposal for inline guard rejection (`else reject "reason"`) — #2 verbosity pattern (20-35% of rule headers), no proposal exists. **Governance-vs-validation positioning:** the failure-mode taxonomy (bypass, timing gap, scattered rules, silent mutation) is the primary competitive weapon; deploy in README, marketplace, and developer communications.

---

### 2026-04-09: Frank — PR #48 final sign-off — APPROVED
**By:** Frank (Lead/Architect)
**Status:** Merged — HEAD 6468617

All 756 tests pass (614 Precept.Tests, 55 Precept.Mcp.Tests, 87 Precept.LanguageServer.Tests). All four blocking items from the CHANGES REQUESTED review confirmed resolved: (1) `docs/PreceptLanguageDesign.md` — stateless section, C12/C13/C55/C49 fully documented, root `edit` grammar, `ExpandEditFieldNames()`. (2) `docs/RuntimeApiDesign.md` — `IsStateless`, nullable `InitialState`, both `CreateInstance` overloads. (3) `docs/McpServerDesign.md` — `isStateless` in CompileResult DTO, nullable `currentState` for Inspect/Fire/Update. (4) Placeholder samples (`customer-profile.precept`, `fee-schedule.precept`, `payment-method.precept`) present. Non-blocking recommendations applied at commit 530725d. **APPROVED. PR #48 merge-ready.**

---

### 2026-04-10T12:00:00Z: Issue #31 merged — keyword logical operators (and/or/not replace &&/||/!)
**By:** George (Runtime Dev), Kramer (Tooling Dev), Newman (MCP/AI Dev), Soup Nazi (Tester), Frank (Lead/Architect), Coordinator
**Status:** Merged — PR #50, main SHA `305ec03`

`&&`/`||`/`!` have been removed from the Precept DSL and replaced with keyword forms `and`/`or`/`not` across all 8 slices. All 775 tests passing. Issue #31 closed.

**Runtime (George — Slices 1-4 + Samples):**
- `[TokenSymbol]` attributes on `PreceptToken.And`/`Or`/`Not` changed to `"and"`/`"or"`/`"not"`. Old operator entries removed from tokenizer steps 4-5. Keyword loop (step 7, `requireDelimiters: true`) handles them automatically — `android` cannot match `And`.
- All operator string comparisons updated in: parser (AST strings), type-checker main switch, `ApplyNarrowing()` (~line 889, a critical second update site distinct from the main switch), and expression evaluator.
- 17 of 24 sample files updated; 7 unchanged (no logical operators used).
- Commit: `83497aa`.

**Grammar + Language Server (Kramer — Slice 5):**
- `and`/`or`/`not` added to `actionKeywords` alternation in grammar (same group as `contains` — expression-position, operator-category tokens).
- Old `keyword.operator.logical.precept` block (`&&|\|\||!`) removed from grammar entirely; `!=` untouched.
- `ExpressionOperatorItems` in `PreceptAnalyzer.cs` updated: `&&`/`||`/`!` `Operator` items replaced with `and`/`or`/`not` `Keyword` items. Global keyword discovery via `BuildKeywordItems()` auto-picks up `And`/`Or`/`Not` from enum — no explicit additions needed.
- Semantic tokens handler unchanged — catalog-driven via `[TokenCategory(Operator)]`.
- Commit: `8f3bdab`.

**MCP Operator Inventory (Newman — Slice 6):**
- Zero code changes to MCP tools. `LanguageTool.cs` is fully catalog-driven via `PreceptTokenMeta.GetSymbol(token)`. George's `[TokenSymbol]` attribute update automatically propagates to `precept_language` output.
- `docs/McpServerDesign.md` already used `and` in expression examples — no doc changes needed.
- New test: `LogicalOperatorsAreKeywordForms` — asserts `and`/`or`/`not` present in operator inventory, `&&`/`||` absent. Canonical dual-assertion pattern for operator renames.

**Tests (Soup Nazi — Slice 7):**
- 10 existing test files updated (symbol substitutions: `&&`→`and`, `||`→`or`, `!`→`not`).
- New `PreceptKeywordLogicalOperatorTests.cs` (15 tests): basic parsing, precedence (`not > and > or`), null narrowing through `not (Field == null)`, `!=` unaffected, old symbols produce `InvalidOperationException`, compound expressions, invariant context.

**Docs (Frank — Slice 8):**
- `docs/PreceptLanguageDesign.md` fully synchronized: migration table heading updated ("Implemented"), "Until implementation…" paragraph removed, `and`/`or`/`not` added to reserved keywords list, nullability Pattern 1/2 code examples updated, "pending migration" annotations removed from expressions section, event asserts and Minimal Example updated.
- `docs/research/language/references/cel-comparison.md` created: full Precept vs. CEL language-level comparison.

**PR Review (Frank):**
- APPROVED. All 8 slices verified. Architecture sound. Coverage complete.
- Key architectural confirmation: `BuildKeywordDictionary()` uses `symbol.All(char.IsLetter)` — operator-to-keyword migration for alphabetic symbols requires only `[TokenSymbol]` attribute change; no tokenizer code changes. Correct pattern for future migrations.
- Non-blocking: `LogicalOperatorsAreKeywordForms` does not assert `!` absent. Acceptable — `!` removed from tokenizer entirely, no path to operator inventory.

**Coordinator final action:**
- Added `NotContain("!")` assertion to `LogicalOperatorsAreKeywordForms`. Merged as 775th test.

**Open item (deferred):**
- Grammar group classification: `and`/`or`/`not` in `actionKeywords` is correct per current convention but a semantic visual system session should formally address whether expression-position operator-category tokens warrant a distinct scope token from structural action keywords. Deferred by Shane.

---

### 2026-04-08T23:50:00Z: Soup Nazi exploratory MCP regression — methodology validated, 5 authoring corrections captured
**By:** Soup Nazi (Tester)
**Status:** Applied

Exploratory MCP regression rounds 1+2 executed against the data-only precepts implementation (feature/issue-22-data-only-precepts). All probes synthesized from scratch using `precept_language` as vocabulary reference.

**Round 1 (18 compile probes):** 15/18 passed as authored. Three were test-plan syntax errors:
1. Multi-line action chains not supported — full transition row must be on one line.
2. `when` guard must precede the first `->`: correct form is `from S on E when Guard -> outcome`.
3. `dequeue`/`pop` require `into <field>` target; bare form is invalid.

Additional corrections: Probe 16 used wrong diagnostic code (PRECEPT008, not C13/PRECEPT013). Probe 17 had wrong expectation (zero-row terminal state produces no C50 diagnostic — C50 fires only when a state has rows that still can't reach another state). All corrected probes passed.

**Round 2 (7 outcome kinds):** All confirmed across three synthesized shapes (Approval flow, FeatureGate, RangeGuard): Transition, NoTransition, Rejected, ConstraintFailure, UneditableField, Update, Undefined.

**Verdict:** PASS (engine). Five test-plan authoring corrections promoted to Soup Nazi charter `## MCP Regression Testing` skill section.

**Report:** `.squad/decisions/inbox/soup-nazi-mcp-regression-exploratory.md` (merged).

---

### 2026-04-08T22:30:00Z: Soup Nazi MCP regression pass for PR #48 — PASS
**By:** Soup Nazi (Tester)
**Status:** Applied

Full 4-round MCP regression against PR #48 (data-only precepts).

- **Round 1 (24 sample compiles):** 24/24 valid, 0 error diagnostics. Three new stateless samples (`customer-profile`, `fee-schedule`, `payment-method`) all return `isStateless: true`.
- **Round 2 (stateful E2E — maintenance-work-order):** Draft→Open transition fired correctly; UneditableField and Update outcomes both confirmed.
- **Round 3 (stateless E2E — customer-profile, fee-schedule):** `edit all` expands to all fields; selective edit blocks locked fields with `UneditableField "(stateless)"`; invariant ConstraintFailure triggered correctly; Fire on stateless precept → Undefined with "stateless" message.
- **Round 4 (diagnostic edge cases):** C12, C55, C49, and parse-failure all behave exactly per spec.

**Verdict:** PASS. PR #48 clear to merge (pending docs gate — see Frank's review below).

**Report:** `.squad/decisions/inbox/soup-nazi-mcp-regression.md` (merged).

---

### 2026-04-08T22:00:00Z: Frank PR #48 review — CHANGES REQUESTED (docs sync missing)
**By:** Frank (Lead/Architect)
**Status:** Blocking — awaiting Slice 8 (docs + samples)

PR #48 (feature/issue-22-data-only-precepts, Slices 1–7) reviewed. Architecture is sound; all 12 design Q&A decisions faithfully executed. One blocking gap: docs sync requirement unmet.

**Blocking items (Slice 8):**
1. `docs/PreceptLanguageDesign.md` — not updated. Missing: stateless precept form, root `edit` grammar rule, C12 redefinition, C13 conditionalization, C55 new constraint, `in State edit all` validity for stateful.
2. `docs/RuntimeApiDesign.md` — not updated. Missing: `IsStateless` property, nullable `InitialState`/`CurrentState`, `CreateInstance(data)` stateless behavior, `CreateInstance(state, data)` throws `ArgumentException` on stateless.
3. `docs/McpServerDesign.md` — not updated. Missing: `IsStateless` in CompileResult DTO, nullable `currentState` for Inspect/Fire/Update, stateless null-passing notes.
4. Sample files — Decision 11 placeholder samples (`customer-profile.precept`, `fee-schedule.precept`, `payment-method.precept`) not present in Slices 1–7.

**Non-blocking notes:** Minor nullable lie on `model.InitialState!` (safe in practice); `TryValidateScalarValue out string error` signature nit; stateful `in State edit all` not in design docs (intentional expansion without coverage).

**Report:** `.squad/decisions/inbox/frank-pr48-review.md` (merged).

---

### 2026-04-08: Uncle Leo security survey — 4 attack surfaces identified, recommendations pending
**By:** Uncle Leo (Security Champion)
**Status:** Pending action — recommendations require owner review

Initial security survey of MCP server (5 tools), language server (LSP/stdio), core DSL runtime, and public C# API surface.

**Attack surfaces ranked:**
| Surface | Risk | Summary |
|---|---|---|
| Unbounded `text` input | High | No size limits before tokenization — DoS via oversized DSL text |
| MCP output echoes DSL content | High | `ExpressionText`, `Reason`, `BranchDto.Guard`, `DiagnosticDto.Message` echo raw user input — indirect prompt injection vector |
| Unvalidated `data`/`fields`/`args` dicts | Medium | Unknown keys accepted silently — no allowlist against declared field/arg names |
| No auth on MCP tools | Medium | stdio-only today; assumption not explicitly documented |
| LS document size (keystroke path) | Medium | Same parser on every change, no size limits |
| JSON object handling (`ToNative` fallback) | Low | Objects/arrays stringify silently — confusing, not exploitable |

**Clean:** No RCE gadget paths found. No file I/O, process spawning, or unsafe deserialization.

**Top recommended actions (ordered):** (1) Input size limits on `text` parameter. (2) Sanitize MCP output fields that echo DSL content. (3) Add invocation logging to MCP server. (4) Audit `PreceptAnalyzer.cs` regexes for ReDoS.

**External frameworks referenced:** OWASP Input Validation Cheat Sheet, OWASP LLM01:2025 Prompt Injection, OWASP MCP Security Cheat Sheet, OWASP Deserialization Cheat Sheet, Microsoft .NET Security.

**Report:** `.squad/decisions/inbox/uncle-leo-security-survey.md` (merged).

---

### 2026-04-08: Issue #22 semantic rules rewrite — warning model and binary taxonomy
**By:** Shane (owner decision), with Frank (research/analysis) and George (runtime evidence)
**Status:** Decided

Issue #22's "States, events, and transitions forbidden in stateless precepts" semantic rule was decomposed and rewritten:

1. **"States forbidden" dropped** — tautological. The absence of states IS what makes a precept stateless; it's definitional, not prohibitive.
2. **"Transitions forbidden" dropped** — structurally impossible. C54 already rejects transition rows referencing undeclared states.
3. **Events-without-states = warning, not error.** Events declared but without any routing surface (states) can never fire. Severity set to warning for consistency with C49 (orphaned event).
4. **C50 upgraded from hint to warning.** A non-terminal state where all outgoing rows dead-end (reject or no-transition) is "probably wrong," not just "FYI." Hint was too lenient. Consistency with C49 and the events-without-states warning.
5. **Binary taxonomy confirmed:** data tier (fields/invariants/editability) or behavioral tier (fields/invariants/states/events/transitions). No middle tier. The "middle tier" (data + events, no states) is a category error.

**Research base:** `.squad/decisions/inbox/frank-stateless-event-boundary.md` (deep analysis), `.squad/decisions/inbox/frank-warning-model-research.md` (diagnostic infrastructure audit + external precedent), `.squad/decisions/inbox/frank-issue22-semantic-rule-rewrite.md` (decomposition recommendation).

---

### 2026-04-08: No temporary local files for GitHub issue editing
**By:** Shane (owner directive)
**Status:** Standing policy

When composing or editing GitHub issue bodies, use the GitHub API directly (MCP tools or `gh` CLI). Do not create temporary local markdown files as intermediate drafts — they create confusion, clutter the workspace, and drift from the actual issue content. Compose the content in memory and post it in one step.

This applies to all agents. The decisions inbox (`.squad/decisions/inbox/`) is for team decisions, not for staging issue text.

---

### 2026-04-08T18:53:22Z: Broadened target model for computed-field rule violations
**By:** Scribe, recording review consensus from Shane
**Status:** Captured

Shane chose the broadened target model for field-based rule violations involving computed fields.

**Decision:**
- Target sets for a violated rule should include all entity data the rule semantically depends on, whether the dependency is explicit or implicit.
- When a violated rule depends on a computed field, the target set should include the computed field's transitive underlying inputs, not just the surface computed field reference.
- Runtime and MCP wording for inspect and fire should align to this dependency-aware target model.

**Recorded drafting state:**
- Frank drafted replacement Issue #17 rationale allowing computed-field constraints.
- George drafted replacement runtime contract text for dependency-aware targets.
- Newman drafted matching MCP and output text for inspect and fire under the broadened target model.

**Why:**
- The chosen target model follows the violated rule's real semantic dependencies, including transitive inputs beneath computed fields.

### 2026-04-08T13:37:07Z: Issue #17 wording clarification for computed fields vs. constraints
**By:** Scribe, recording review consensus from Shane
**Status:** Captured

Issue #17 does not contain a semantic contradiction between "field-level constraints do not apply to computed fields" and "computed fields remain readable in guards, invariants, and state asserts."

**Decision:**
- Treat the problem as wording ambiguity, not as a semantic conflict.
- Use "field-level constraints" only for declaration-level constraints attached directly to a field.
- Keep guards, invariants, and state asserts described as boolean rule expressions that may read computed fields.
- Make the wording explicit that computed fields cannot carry field-level constraint declarations, but remain readable anywhere rule expressions evaluate instance state.

**Why:**
- The current proposal wording overloads "constraints" for two different concepts: declaration-level field constraints and boolean rule expressions.
- Computed fields remain part of readable instance state, so rule expressions can depend on them without implying that declaration-level field constraints attach to computed fields.

### 2026-04-08T13:29:23Z: Language research corpus complete + Squad closeout
**By:** Scribe, recording the completed corpus state from Frank, George, and Steinbrenner
**Status:** Applied

The language research corpus on `squad/language-research-corpus` is complete. Batch 1 landed in `54a77da`, Batch 2 landed in `48860ae`, and the final corpus plus index sweep landed in `3cc5343`.

**Closeout decisions:**
- Treat `3cc5343` as the finishing corpus checkpoint for this branch.
- Keep `docs/research/language/domain-map.md` as the canonical domain-first map and the language indexes as the discovery surface.
- Preserve the standing user constraints through closeout: no proposal-body edits, `computed-fields.md` as the quality bar, horizon domains retained, and commit-after-batch discipline.
- Merge the remaining `.squad/decisions/inbox/` backlog into this ledger, skipping superseded duplicates and clearing the inbox.

**Recorded state:**
- Batch 1 landed in `54a77da`
- Batch 2 landed in `48860ae`
- Final corpus + indexes landed in `3cc5343`
- Merged 61 inbox entries; skipped 2 superseded and 0 already-recorded duplicates
- Remaining local changes in this pass are `.squad/` bookkeeping only

---
### 2026-04-08T07:16:23Z: Language Research Corpus Structure & Batch Discipline
**By:** Frank (Lead/Architect), Steinbrenner (PM), with George's Batch 1/2 type-system work recorded
**Status:** Applied

The language-research corpus stays organized by **domain**, not by individual proposal, and the canonical map remains `docs/research/language/domain-map.md`.

**Decision:**
- Keep `docs/research/language/domain-map.md` as the single durable map of the language research corpus.
- Treat alternate map filenames as disposable experiments, not parallel sources of truth.
- Preserve the standing corpus guardrails during research curation:
  - no proposal-body edits
  - `docs/research/language/expressiveness/computed-fields.md` remains the quality bar
  - include horizon domains even when no proposal exists yet
  - close each completed batch with its own commit
- Keep the three-batch execution plan from `docs/research/language/domain-research-batches.md`, with **Batch 1 regrouped** to include constraint composition alongside the rest of the validator/rule/declaration lane.

**Recorded state:**
- Batch 1 landed in `54a77da`
- Batch 2 landed in `48860ae`
- Batch 3 research and the final README/index sweep are still outstanding

---

### 2026-05-18T00:25:00Z: README DSL Hero Image Width Contract
**By:** Elaine (UX), Kramer (Tooling), with Frank's sizing analysis preserved
**Status:** Applied

The README DSL hero remains an image-based branded treatment, but it must now be sized against GitHub's actual repo-view image ceiling instead of the wider article frame.

**Decision:**
- Keep the README DSL hero as an image for now
- Regenerate/capture it at **1660px** source width from an **830px** viewport at **2×** device scale
- Treat **830px** as the effective GitHub repo README image display cap for this asset
- Tune the rendered code text for about **13px** apparent size at display
- Spend any extra composition room on whitespace rather than on additional contract width
- Preserve `design/brand/capture-hero-dsl.mjs` as the repeatable regeneration path

**Tradeoffs and retained learning:**
- Native README text/fenced code remains the only fully robust way to keep DSL text scaling in lockstep with surrounding prose across viewport and zoom changes.
- GitHub page-geometry research still matters: the repo shell tops out around **1280px** and the README/article frame around **1012px**, but the displayed README image for this treatment clamps earlier at about **830px**.
- Do not rely on custom CSS, sanitizer-sensitive HTML, or viewport-specific image swapping as a stable README contract.

---

### 2026-04-07T23:30:44Z: README Contract Image & DSL Copyable Block Cleanup
**By:** J. Peterman (Brand/DevRel)
**Status:** Decided

README.md Quick Example section refactored to remove explanatory hedge, copyable DSL block, and replace markdown image syntax with fixed-width HTML img tag for consistent GitHub rendering.

**What Changed:**
1. Removed explanatory sentence about DSL rendering fallback
2. Removed copyable DSL code block from Quick Example
3. Replaced markdown image syntax with `<img src="design/brand/readme-hero-dsl.png" alt="..." width="600" />`

**Rationale:** Simplify visual hierarchy, trust the professionally-rendered contract image, and direct DSL learners to `samples/` and language reference rather than README copy-paste scaffolding.

**Brand Rationale:** The Quick Example teaches the pattern, not the DSL. Remove defensive scaffolding; the rendered contract is the hero artifact.

---

### 2026-04-07T23:20:00Z: PR #34 Merge — Squad Upgrade + README Image Fix
**By:** Frank
**Status:** Merged

Merged PR #34 (chore: upgrade Squad configuration and fix README image links) to `main` using merge commit strategy.

**Context:**
- Branch `chore/upgrade-squad-latest` carried Squad configuration updates plus README.md image path fixes
- Image paths corrected: `brand/readme-hero.svg` → `design/brand/readme-hero.svg`
- README fixes directly related to stated Squad upgrade task scope
- No branch protection blockers

**Actions:**
1. Committed README.md image fix with Co-authored-by trailer
2. Pushed branch with upstream tracking
3. Created PR via `gh pr create` with explicit base/head branches
4. Merged with merge-commit strategy
5. Deleted remote branch post-merge

**Outcome:**
- PR #34 merged successfully
- Main now carries both Squad updates and corrected README image references
- Remote branch cleaned up
- No unrelated code changes; scope remained surgical

---

### 2026-04-07T23:16:55Z: User directive — Branch retention for future work
**By:** shane (via Copilot)
**Status:** Captured

Keep the `chore/upgrade-squad-latest` branch open for other work — user request captured for team memory.

---

### 2026-04-05T16-16-48Z: User directive — philosophy/readability bar
**By:** shane (via Copilot)
**Status:** Captured

Keep the language-review bar anchored to project philosophy and audience fit:
- evaluate proposals against Precept's core goals, not isolated cleverness
- keep non-programmer readability explicit in naming decisions
- prefer wording and feature framing that read closer to configuration or scripting than to a general-purpose programming language

**Why:** User request — captured for team memory.

---

### Decision: Expression roadmap framing for #8-#10 (2026-04-05)
**Filed by:** Frank, George, and Steinbrenner
**Status:** Research captured — awaiting Shane sign-off

The current expressiveness roadmap is grounded in the runtime audit and proposal research:
- docs/research/dsl-expressiveness/expression-language-audit.md records the concrete gaps
- docs/research/dsl-expressiveness/expression-feature-proposals.md organizes the candidate features into rollout waves
- dsl-expressiveness is the tracking label for the capability-gap proposals #8, #9, and #10

**Current first-wave framing:** named rule declarations (#8), ternary expressions in set mutations (#9), and string .length (#10).

**Gate:** No implementation begins until Shane approves the specific proposal/wave.

---

### Decision: Preview concepts deep analysis baseline (2026-04-05)
**Filed by:** Elaine
**Status:** Proposed — awaiting Shane review

The preview-concept deep pass is now the baseline research record for preview UX work.

**Key outcomes:**
- Timeline + Notebook remain the primary recommendation
- Decision Matrix, Storyboard, and Execution Trace are the strongest secondary concepts
- Rule Pressure Map and Kanban remain mode-level ideas, not the default product shape
- Concept 12 (Execution Trace / Pipeline Debugger) was added because it uniquely visualizes Precept's fire pipeline

**Next constraints:** stress-test the concepts against more complex samples, expose collection/edit/outcome detail, and keep future mockups grounded in real runtime semantics.

---

### Decision: Proposal #8 finalized as named rule declarations (2026-04-05)
**Filed by:** Scribe synthesis from Frank, George, J. Peterman, and Steinbrenner
**Status:** Locked for proposal framing — implementation still requires Shane sign-off

Proposal #8 is now framed as **named rule declarations**, not guards or predicates.

**Winning syntax:** rule <Name> when <BoolExpr>

**Allowed reuse:**
- when
- invariant
- in / to / from <State> assert

**Explicit exclusions (v1):**
- on <Event> assert
- set right-hand-side / computed-value aliasing
- rule-to-rule composition

**Why rule won:** it names a business concept without academic jargon, preserves Precept's English-ish/configuration-like readability, and fits the product's "one file, all rules" identity better than guard or predicate.

**Repo sync:** issue #8 now tracks **Proposal: Named rule declarations**; the associated philosophy/readability notes and charter guidance were synchronized for Frank, George, Steinbrenner, and J. Peterman.

---

### 2026-04-05T15:15:31Z: User directive — dsl-compactness label
**By:** shane (via Copilot)
**Status:** Captured

Use the dsl-compactness label as the categorization tag for the current language-improvement proposal issues.

**Why:** User request — captured for team memory.

---

### Decision: Use dsl-compactness for language compactness proposals (2026-04-05)
**Filed by:** Steinbrenner
**Status:** Applied

Use the GitHub label dsl-compactness as the categorization tag for the current language improvement proposal issues focused on making the DSL more compact.

**Applied to:**
- #8 — Proposal: Named guard declarations
- #9 — Proposal: Ternary expressions in set mutations
- #10 — Proposal: String length accessor
- #11 — Proposal: Event argument absorb shorthand
- #12 — Proposal: Inline guarded fallback (else reject)
- #13 — Proposal: Field-level range/basic constraints

**Why:** These six issues form one coherent roadmap slice around reducing ceremony and improving expression density in the language, so a shared label gives PM, architecture, and implementation a stable theme tag across multiple rollout waves.

---


### Decision: Gold Brand Mark Exception

**Date:** 2026-04-04
**By:** Elaine (UX Designer)
**Status:** Implemented — pending Shane sign-off

#### Decision
Gold (`#FBBF24`) is permitted as a single sparse accent in the **combined brand mark only** — the third icon (tablet + state machine). It does not appear in the standalone diagram icon or the standalone tablet icon.

#### Placement
The Gold stroke represents the `because "…"` line — the human-readable rule message baked into the running system. In the SVG, it is the short horizontal line at `y=33` inside the tablet/code area: shorter than the body lines above it (stopping at x=34 vs x=40), stroke-width 1, opacity 0.65. Deliberately dim and singular so it reads as an accent, not a second brand color.

#### Why
Gold already carries this meaning in syntax highlighting: every `because` and `reject` string is Gold in the editor. The combined mark unifies the state machine and the written rule — it is the only icon that shows both halves. Adding one Gold line there extends an existing meaning rather than inventing a new one. It's a philosophical nod, not a decorative choice.

#### Constraints
- **One mark only.** The diagram icon and the tablet icon remain unchanged.
- **One line only.** Gold must not appear on structural elements (rect outlines, arrowheads, circles).
- **Not a new UI color.** This exception does not permit Gold in badges, borders, button states, status chips, or any other UI surface.
- **Not a new accent lane.** Gold remains syntax-primary. This is a narrow named exception, not a policy relaxation.
- Amber (`#FCD34D`) continues to own warning/caution semantics. The visual distance between Gold and Amber is maintained; Gold in the mark is dimmer (`opacity: 0.65`) and lives in a non-signal context (brand icon, not status UI), so no semantic collision occurs.

#### Files Changed
- `brand/brand-spec.html` — SVG updated, color key updated, prose updated in §1.3, §1.4 intro, §1.4.1, and the Rules · Gold surface section
- `.squad/skills/color-roles/SKILL.md` — Rule 2 and the Gold row updated to reflect this exception


### Design Gate: Peterman Brand Compliance Review (2026-04-04)
**Filed by:** Coordinator (via Shane)
**Status:** LOCKED

The design gate for technical surfaces now requires Peterman's brand compliance review as a formal step between Elaine's UX spec and Frank's architecture review.

**Gate sequence:**
1. Elaine — UX design spec
2. Peterman — brand compliance review
3. Frank — architectural fit
4. Shane — final sign-off

**Applies to:** VS Code extension, preview webview, state diagram, inspector panel, and any future product surface.

**Why:** Brand should be applied consistently to all surfaces. Peterman's involvement ensures brand decisions made in `brand/brand-spec.html` are honored in technical implementation, not just noted in documentation.

---

### Decision: README Syntax Highlighting for Precept DSL (2026-04-05)
**Filed by:** Kramer (Tooling Dev)
**Status:** No Change Required

README already uses the correct approach: ` ```precept ` fence for DSL code samples.

**Research findings:**
- GitHub Linguist does NOT recognize `precept` as a language identifier
- Unknown language fences render as plain monospace text without syntax highlighting
- Industry practice (Terraform, etc.) uses custom language names before Linguist support exists

**Options considered:**
- **Option A: Keep ` ```precept `** (SELECTED) — Truthful, future-proof, standard practice. Auto-highlights if/when Precept joins Linguist.
- **Option B: Use similar language tag** (e.g., `yaml`, `text`) — Rejected: misleading, provides false/inappropriate highlighting.
- **Option C: No language tag** (empty ` ``` `) — Rejected: same rendering as Option A but loses documentation value.

**Rationale:** No real improvement exists—GitHub cannot highlight unknown languages. Mislabeling would mislead readers. Current approach is already optimal and future-compatible.

**Future path:** To enable syntax highlighting, submit a Linguist PR with the TextMate grammar from `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`. All existing fences will automatically gain highlighting once merged.

---

### Decision: How We Got Here API Evolution Clarification (2026-04-05)
**Filed by:** J. Peterman
**Status:** COMPLETE

`docs/HowWeGotHere.md` now states the product's authoring progression explicitly inside the chronology:

1. early fluent-interface experiments,
2. later public builder-pattern API,
3. current DSL-centric direction.

**Why:**
- Prevents the repo history from reading like a direct jump from an older state-machine library to the current DSL.
- Clarifies that the major shift was a change in authoring model, not just an implementation refresh.

**Scope:** Documentation correction only. No runtime or API behavior changed.

---

### Brand-Spec Restructure: Surface-First Organization (2026-04-04)
**Filed by:** J. Peterman
**Status:** COMPLETE

`brand/brand-spec.html` restructured from 10-section (color-category-first) to 3-section (visual-surfaces-first) structure.

**New structure:**
- **Section 1: Brand Identity** — positioning, narrative, voice, wordmark, brand mark, color system, typography
- **Section 2: Visual Surfaces** — Syntax Editor (locked), State Diagram (locked), Inspector Panel (draft), Docs Site (draft), CLI / Terminal (draft)
- **Section 3: Research & Explorations** — living section with research links and exploration index

**Deferred surfaces** (marked as DRAFT awaiting contribution):
- Inspector Panel — pending Elaine's design review
- Docs Site — scope clarification needed
- CLI / Terminal — color audit pending

**Research foundation:** `brand/references/brand-spec-structure-research.md` (validated surface-first pattern across 13 systems: VS Code, Vercel, GitHub, IBM, Material, etc.)

---

### Visual Surfaces Draft: Five UX Specifications (2026-04-04)
**Filed by:** Elaine
**Status:** DRAFT FOR REVIEW
**File:** `brand/visual-surfaces-draft.html`

Five surfaces drafted with UX descriptions covering purpose, visual concerns, color application, typography, accessibility, and AI-first notes:
1. **Syntax Editor** — `.precept` file authoring in VS Code
2. **State Diagram** — visual graph in preview webview for logic verification
3. **Inspector Panel** — live instance state and field editing (functional, brand drift found)
4. **Docs Site** — future public documentation surface (scope clarification needed)
5. **CLI / Terminal** — command-line output (color audit pending)

**Key principles locked:**
- Semantic unity: all surfaces use the same visual language
- Color + shape/symbol redundancy for accessibility
- Verdict colors (green/yellow/red) runtime-only
- AI-first design: every surface works for humans AND AI agents

**Critical issues flagged:**
- Inspector panel uses custom palette (#6D7F9B, #8573A8, #1FFF7A, #FF2A57) instead of brand system (violet, cyan, emerald, rose)
- Inspector uses Segoe UI instead of Cascadia Cove monospace
- CLI color system audit needed (unknown if existing tools apply colors)
- Light theme support clarification needed

**Next steps:** Shane review → Elaine/Peterman integration into brand-spec → Kramer inspector color fix

---

### Inspector Panel Design Review: Brand Color Drift Found (2026-04-04)
**Filed by:** Elaine
**Status:** PENDING FIX
**File:** `brand/inspector-panel-review.md`

Kramer's inspector panel is functionally complete but diverges from brand system:

**What works:** ✅ List-based layout, constraint violation workflow, edit mode UX, accessibility redundancy

**Brand mismatches (CSS-only fixes):**
| Element | Current | Brand Target |
|---------|---------|--------------|
| State colors | `#6D7F9B` | Violet `#A898F5` |
| Event colors | `#8573A8` | Cyan `#30B8E8` |
| Success | `#1FFF7A` | Emerald `#34D399` |
| Error | `#FF2A57` | Rose `#F87171` |
| Font | Segoe UI | Cascadia Cove monospace |

**Owner:** Kramer (implementation)
**Gate:** Elaine review → Peterman brand compliance → Frank → Shane sign-off

---

### Charter Updates: Peterman + Elaine Design Governance (2026-04-04)
**Filed by:** Coordinator
**Status:** LOCKED

Two charter amendments formalize design gate participation:

**Peterman (Brand/DevRel) — Design Review Participation (Brand Compliance)**
- Participates in design reviews for any technical surface where brand is applied
- Flags brand violations, approves final surface designs before implementation
- Approval gate: brand identity correctly expressed, color palette applied per locked rules, typography follows conventions, voice consistent
- Surfaces: syntax highlighting, diagrams, inspector panels, documentation sites, CLI output

**Elaine (UX/Design) — Brand Compliance in Technical Surface Design**
- Leads design work on technical surfaces: Inspector Panel, Docs Site, CLI output, future surfaces
- All designs pass through gate: (1) Peterman reviews brand compliance → (2) Frank reviews architecture → (3) Shane signs off
- Peterman is the brand gate; her approval is prerequisite for architecture review
- Surface contracts defined in `brand/brand-spec.html §2`; designs must conform before review

---

### Color System Audit: Open Decisions (2026-04-04)
**Filed by:** J. Peterman
**Status:** FINDINGS FOR REVIEW

Four open brand decisions pending Shane approval:

**Decision #4: Color Card Treatment**
- **Current state:** `brand/explorations/color-exploration.html` has palette-card format but was never updated after Indigo was chosen
- **Options:**
  - (a) Add SUPERSEDED banner to color-exploration.html (like semantic-color-exploration already has)
  - (b) Create new "brand color card" HTML showing locked Indigo + Gold pair with same format
  - (c) Both
- **Recommendation:** Archive color-exploration as reference; no blocking impact

**Decision #5: Outcome Color Scope**
- **Current framing:** "Runtime outcomes in inspector and diagrams"
- **Shane's description:** Broader — diagnostics, inspector, UI states, any success/warning/error surface
- **Question:** Does outcome color layer apply everywhere except syntax highlighting, or is it scope-limited?

**Other open decisions documented in inbox files; recommend Shane review before locking charter updates and brand-spec final section numbering.**


### Diagram Color Mapping — Section Placement and Scope (2026-04-04)
**Filed by:** Frank (Lead/Architect)
**Status:** RECOMMENDATION — awaiting Shane sign-off

Add a **"Diagram color mapping"** h3 subsection to **§2.2 State Diagram** in brand-spec.html. This is the authoritative reference for every color decision in the state diagram surface.

**Placement:** Within §2.2, between the "No lifecycle tints" callout and the shape tiles. Reader flow: Purpose → Color intro → **Color mapping table** → Shape tiles → SVG example → Lifecycle tables.

**Scope boundaries:**
| Concern | Section | Owns |
|---------|---------|------|
| "Violet means states" | §1.4 | Semantic family identity |
| "State names in syntax: #A898F5, italic when constrained" | §2.1 | Syntax-level application |
| "State names in diagram: #A898F5, italic when constrained" | **§2.2** | Diagram-level application |
| "Blocked transitions: #FB7185 dashed" | **§2.2** | Diagram-specific element |
| "Active state: #4338CA fill overlay" | **§2.2** | Diagram-specific interactive state |
| "Error rose used in diagrams and inspector" | §1.4.1 | Cross-cutting usage note |

**Minimum mapping (4 categories):**
1. **Static elements** — canvas bg, node borders (per lifecycle role), node fill, state names, transition edges + arrowheads, event labels, guard annotations, legend
2. **Interactive elements** — active state highlight, enabled/blocked/warning transition edges + labels
3. **Semantic signals** — constrained state/event italic, orphaned node opacity, dead-end shape
4. **Exclusions** — data fields (inspector), rule messages (syntax-only), comments (editorial)

**Hex discrepancies to fix:**
| Element | Current | Correct (palette card source of truth) |
|---------|---------|---------------------------------------|
| Blocked legend SVG | #f43f5e | #FB7185 |
| brand-decisions.md Blocked | #F87171 | #FB7185 |
| brand-decisions.md Warning | #FDE047 | #FCD34D |

**Full analysis:** rand/references/brand-spec-diagram-color-mapping-review-frank.md

---

### Diagram Color Mapping Section (2026-04-04)
**Filed by:** Elaine (UX/Design)
**Status:** PROPOSED — needs Shane sign-off

Add a dedicated diagram color mapping subsection to §2.2 State Diagram in rand/brand-spec.html.

**What:**
Two new <h3> blocks within the existing §2.2 card:
1. **"Diagram color mapping"** — a complete element-to-color reference table covering every visible diagram component (canvas, node borders, node fills, state name text, event label text, transition arrows, arrow markers, guard annotations, legend text).
2. **"Runtime verdict overlay"** — how diagram colors change when paired with an active inspector instance. Covers: current state highlighting, enabled/blocked/warning edge coloring, muted non-current-state edges, transition glow effects, hover interaction colors.

**Why:**
- Scattered specification — diagram colors mentioned inline in §2.2 prose, partially in §1.4.1, and in brand-decisions.md; never collected into one reference
- Implementation drift — webview uses #1FFF7A / #FF2A57 / #6D7F9B; locked system specifies #34D399 / #F87171 / TBD
- Runtime overlay unspecified — verdict-colored edges exist in implementation but have no brand-spec backing
- Current state indicator undefined — not specified anywhere

**Open sub-decisions for Shane:**
1. **Current state indicator style:** Fill tint (#1e1b4b at low opacity) vs. border glow vs. badge dot. Elaine recommends fill tint.
2. **Muted edge color:** #71717A (text-muted, in system) vs. #52525b (zinc-600, off-system). Elaine recommends #71717A.
3. **Guard annotation text color:** Slate #B0BEC5 (data family). Elaine recommends this.

**Full analysis:** rand/references/brand-spec-diagram-color-mapping-review-elaine.md

---
---

---

# Team Knowledge Refresh — 2026-04-04 Findings
*Filed by Scribe, 2026-04-04T06:08:06Z. Consolidated from 6 domain reviews.*

---

## CRITICAL SYNC RULE: Grammar-Completions Drift
**Priority:** HIGH — NON-NEGOTIABLE
**Owner:** Kramer (Tooling), Frank (Architecture)
**From:** kramer-tooling-review.md

The DSL parser and VS Code tooling are loosely coupled via regex patterns in `PreceptAnalyzer.cs` and `syntaxes/precept.tmLanguage.json`. No automated drift detection exists.

**Finding:** When the parser syntax changes, both files MUST be updated in the same PR or the tooling drifts silently. Example: `NewFieldDeclRegex`, `NewCollectionFieldRegex`, `NewEventWithArgsRegex` are hand-written and not derived from the parser grammar.

**Action:**
1. Add a documented checklist comment to `PreceptAnalyzer.cs` header:
   ```csharp
   // ⚠️ GRAMMAR SYNC REQUIRED: If DSL syntax changes, update these regexes:
   //   - field syntax: NewFieldDeclRegex, NewCollectionFieldRegex
   //   - event syntax: NewEventWithArgsRegex
   //   - transition syntax: SetAssignmentExpressionRegex, CollectionMutationExpressionRegex
   // Test by running samples/*.precept through the parser and verifying regex matches.
   ```
2. Establish review rule: Kramer must review tooling whenever parser syntax lands.

---

## Medium-Priority Architectural Concerns
**From:** frank-arch-review.md

### 1. Thin-Wrapper Violation Risk in MCP Tools
**Risk:** Some MCP tools independently versioned. Over time, tools accumulate business logic (validation, transformation) that should live in `src/Precept/`.

**Recommendation:** Establish "tool hygiene rule": if tool method exceeds ~50 lines of non-serialization code, logic belongs in core. Audit all 5 tools quarterly against this rule. **Action:** Audit before GA.

### 2. Expression Evaluator Isolation
**Risk:** `PreceptExpressionEvaluator` tested end-to-end through runtime/inspect tests, but has no dedicated unit suite. Null handling, operator precedence, arithmetic overflow tested indirectly.

**Recommendation:** Add `ExpressionEvaluatorTests.cs` with 20–30 test cases covering:
- All operator combinations with null operands
- Operator precedence (e.g., `1 + 2 * 3 == 7`, not `9`)
- Division by zero handling
- Numeric overflow/underflow
- String/boolean operator mismatches

**Action:** Add when expression-related bugs surface or before 1.0 GA.

### 3. Naming Density in Violation Model
**Risk:** `ConstraintViolation`, `ConstraintSource`, `ConstraintTarget`, `ConstraintSourceKind`, `ConstraintTargetKind`, `AssertAnchor`, `StateTarget`, `FieldTarget`, `EventTarget`, `EventArgTarget`, `DefinitionTarget` — 11 types, 4 enums, lots of discriminated unions. Maintainers and AI reading the code can get confused.

**Recommendation:** Add **Violation Model Guide** (`docs/ViolationModelGuide.md`) explaining type hierarchy, when each type is used, with worked examples. Keep under 2 pages (visual diagrams preferred).

**Action:** Create before public distribution.

### 4. Edit Mode Protocol Complexity
**Risk:** `PreceptPreviewProtocol` carries typed field data, edit metadata, and bidirectional graphs collapsed to index arrays. Well-designed but brittle; future changes could require careful migration.

**Recommendation:**
- Document protocol version in `PreceptPreviewProtocol.cs` file header: `// Protocol version: 2 (2026-04-06, adds EditableFields)`
- Establish stability pledge: "Breaking changes require major version bump and migration guide."
- Add protocol changelog as comments in the file.

**Action:** Document and lock before Marketplace submission.

### 5. Graph Analysis Completeness vs. Data Constraint Detection
**Risk:** `PreceptAnalysis.cs` is deliberately incomplete. Does not detect impossible entry conditions, provably impossible guards, or deadlock states.

**Rationale:** Out of scope for MVP; inspector catches runtime impossibilities.

**Recommendation:** Document explicitly in `docs/PreceptLanguageDesign.md` § Compile-time checks: "The checker detects reachability, orphaned events, and reject-only pairs. It does not detect impossible data constraints (e.g., `X > 0 && X < 0`) — these are left to runtime inspection."

**Action:** Already handled by documentation. Confirm C48 warning is being emitted.

### 6. No Cross-Precept Composition
**Finding:** Precepts are isolated. No import, inheritance, reference, or composition across preceptsinstances.

**Recommendation:** Document as a deliberate scoping decision (not a bug) in `docs/RoadmapFuture.md` or README. If composition becomes a requirement, design a separate feature.

**Action:** Confirm with shane that this is intentional; document as known limitation if needed.

---

## Runtime Edge Case Review
**From:** george-runtime-review.md

Eight edge cases reviewed; 7 working-as-designed. **One medium-risk finding:**

### Dotted Name Resolution in Constraints ⚠️ MEDIUM RISK
**Location:** `ConstraintViolation.cs` lines 116–124 (`ExpressionSubjects.Walk`)

**Scenario:** Invariant references field with dotted property:
```precept
invariant Items.count > 0
```

**Issue:** Walk behavior identifies `Items.count` as `("Items", "count")` — treats it as EventArg reference instead of field property. Violation targets would be `EventArgTarget("Items", "count")` instead of `FieldTarget("Items")`.

**Impact:** Affects violation attribution in UI/API, not engine correctness.

**Mitigation:** Type checker validates at compile time — dotted refs in non-event-assert scopes are flagged if prefix isn't an event name.

**Recommendation:** Add defensive check in constraint extraction; if Walk produces arg-targets for field expressions, log warning.

---

## Language Server & Extension Gaps
**From:** kramer-tooling-review.md

### Syntax Highlighting Implementation (Phases 0-7)
**Status:** Design docs exist; implementation not started.

**Current state:**
- Phase 0 (Grammar refactor) — not started
- Phase 1-2 (Custom semantic tokens) — not started
- Phase 3-7 (Color binding + modifiers) — not started

**Impact:** 8-shade palette defined but not implemented; users see generic theme colors.

**Recommendation:** Lane assignment: **George** (Phases 0-1), **Kramer** (Phases 2-7). Multi-week project, not urgent, but blocks "design locked" claims in marketing.

### Completions Type-Awareness Gaps
**Finding:** Completions lack type-aware filtering in three scenarios:
1. Set assignment expressions — suggests all fields, not just same-type values
2. Collection mutations — suggests all expressions, not just inner-type values
3. Dequeue/pop "into" targets — partially implemented; doesn't exclude captured fields

**Status:** Nice-to-have validations; parser already catches errors. Queue as Phase 2 enhancement. Low priority.

### Semantic Token Modifiers Not Emitted
**Finding:** `preceptConstrained` modifier registered but never emitted. Design calls for italic text on constrained fields/states/events.

**Status:** Phase 7 of implementation plan. Queue after colors bound (Phase 5).

### Hover & Definition Limitations
1. **Built-in collection members** — hovering over `Floors.count` shows no tooltip
2. **Precept name** — top-level machine name not clickable for "go to definition"

**Status:** Design limitations. Built-in members should have dedicated hover tooltip in Phase 2. Top-level name requires workspace scanning.

### Document Intelligence Fallback Parsing
**Finding:** `PreceptDocumentIntellisense.cs` uses regex to extract declarations when main parser returns null (incomplete/invalid syntax). Patterns separate from canonical parser patterns.

**Recommendation:** Already documented as "fallback-path code." No change needed, but make clear that regex drift is a known risk. Mitigate via PR review + testing.

---

## Rule Analyzer Diagnostic Gaps
**From:** soup-nazi-rule-analyzer-gaps.md
**Priority:** MEDIUM (UX/DX, not correctness)

### Problem
PreceptAnalyzerRuleWarningTests.cs has only **1 test**, covering 1 warning scenario. **Seven critical diagnostic cases are untested:**

1. **From-state asserts never checked** — no incoming transitions to state
2. **Field rule scope violations** — references field other than its own
3. **Event rule scope violations** — references instance data (only args visible)
4. **Top-level rule forward reference** — references field before declaration
5. **Null expression failure in rule** — may fail if nullable field is null
6. **Rule violated by field defaults** — default value violates invariant
7. **Initial state rule violated by defaults** — boot failure scenario

### Why This Matters
These diagnostics are **compile-time checks already** (parser + compiler validate them). The analyzer should expose them via Diagnostic objects for real-time IDE highlighting. Without them:
- User edits rule, hits save → no red squiggles
- User publishes → compile fails at deploy time
- Or: code compiles but rule is silently never checked

### Action
Write 7 additional test methods in `test/Precept.LanguageServer.Tests/PreceptAnalyzerRuleWarningTests.cs`:

```csharp
[Fact] public void Diagnostics_FromStateAssertWithoutExitingTransitions_ProducesWarning() { ... }
[Fact] public void Diagnostics_FieldRuleReferencesAnotherField_ProducesError() { ... }
[Fact] public void Diagnostics_EventRuleReferencesInstanceData_ProducesError() { ... }
[Fact] public void Diagnostics_RuleForwardReferencesField_ProducesError() { ... }
[Fact] public void Diagnostics_NullableFieldInNonNullExpression_ProducesWarning() { ... }
[Fact] public void Diagnostics_FieldDefaultViolatesRule_ProducesError() { ... }
[Fact] public void Diagnostics_InitialStateRuleViolatedByDefaults_ProducesError() { ... }
```

---

## Code Quality Concerns
**From:** uncle-leo-code-review.md

### 1. Unsafe Null-Forgiveness Pattern (LOW SEVERITY)
**Location:** `tools/Precept.LanguageServer/PreceptAnalyzer.cs:35`

```csharp
public bool TryGetDocumentText(DocumentUri uri, out string text)
    => _documents.TryGetValue(uri, out text!);
```

**Issue:** `text!` bypasses type system. While technically safe, it's a code smell that breaks nullable flow analysis. If logic changes, suppressed compiler warnings would catch the bug.

**Fix (ranked):**
1. **Explicit assignment** (Option 1) — most explicit, zero surprise
2. **Assertion** with comment explaining unreachability
3. **Accept pattern** — document inline if performance-critical

**Recommendation:** Option 1 (explicit assignment).

### 2. Hydrate/Dehydrate Dual-Format Complexity (MEDIUM SEVERITY)
**Location:** `src/Precept/Dsl/PreceptRuntime.cs:162–253`

**Issue:** Instance data lives in two formats:
- **Public:** Field names → values (no prefix). Collections are `List<object>`.
- **Internal:** `__collection__<fieldName>` → `CollectionValue` objects.

Three methods (`Hydrate`, `Dehydrate`, `CloneCollections`) invoked at three mutation sites (Fire, Inspect, Update). If any site forgets one step, **silent data corruption** occurs.

**Fix (ranked by effort):**
1. **Highest confidence (Medium effort):** Extract `DataMutation` record encapsulating triple: `(Clean, Internal, Collections)`. Pass through mutation methods instead of juggling three variables.
2. **Good practice (Low effort, high ROI):** Add invariant checks before returning:
   ```csharp
   foreach (var kvp in resultData) {
       if (kvp.Key.StartsWith("__collection__")) {
           throw new InvalidOperationException("Dehydrate forgot to strip collection prefix");
       }
   }
   ```
3. **Documentation (Immediate):** Add comment explaining three-step protocol.

**Recommendation:** Implement option 2 immediately (catches mistakes in testing). Schedule option 1 for next refactor.

---

### 2026-04-04T05:55: User directive — voice & hero tone update
**By:** shane (via Copilot)
**What:** Brand voice updated to allow occasional jokes. The hero sample may use a fun/pop-culture domain. Back to the Future (TimeMachine) is explicitly approved as a hero candidate. Jokes in `because` reason messages are appropriate for the hero snippet.
**Why:** User updated brand-decisions.md and confirmed this direction directly.
**Supersedes:** Any prior "Serious. No jokes." guidance from Steinbrenner's hero spec.

---

---

# Brand Research — Team Observations
*Filed by J. Peterman, 2026-04-05. For team awareness.*

---

## 1. Reference files are causing a navigation problem

The `brand/references/` files were written *before* brand decisions were locked. They contain "here are four options" framing for things that have already been resolved. A future contributor reading `color-systems.md` or `brand-positioning.md` will encounter open-question language for closed questions.

**Recommendation:** Add a status header to each reference file pointing to `brand-decisions.md` as the locked resolution. Example:

```
> STATUS: Research archive. Decisions resolved in brand/brand-decisions.md.
> Do not treat options in this file as open.
```

This is a one-line edit per file. Low cost, prevents re-litigation.

---

## 2. The AI-native frame is undersold

The secondary positioning — "the contract AI can reason about" — is treated almost as a footnote in current materials. But the actual implementation (five MCP tools, deterministic engine, structured APIs) is genuinely first-class and differentiated. No other tool in the state machine / domain integrity space has this story.

**Recommendation:** The AI-native frame deserves a dedicated paragraph in the README, not just a parenthetical in positioning docs. Not as the opening — the primary frame is correct — but as a named capability section. Something like:

> **AI-native by design.** Precept ships a complete MCP server. An AI can create, inspect, fire, and validate a `.precept` definition without human feedback. The deterministic engine guarantees the AI's changes produce the same outcomes a human would verify manually.

Worth discussing as a team: is this the right moment to elevate this, or does it wait for explicit AI-workflow documentation?

---

## 3. Hero snippet priority

The hero snippet is the most consequential unresolved brand decision — more so than the icon. It will appear in every screenshot, every VS Code listing, every blog post. The icon appears once in NuGet search. The hero snippet is seen by every developer who evaluates the product.

Active spec: `.squad/decisions/inbox/steinbrenner-hero-example-spec.md`

This should be treated as a blocking item for any launch-facing work.

---

## 4. Wordmark rationale should be public

"Small caps is the typographic convention for defined terms, legal codes, and axioms — exactly what a precept is." This is a strong line that builds brand credibility with developers who notice intentionality. It currently lives only in internal brand files.

**Recommendation:** Surface this reasoning in the README or a brief "About the design" section. Not a manifesto — two sentences. Developers who care about craft will notice it. Developers who don't will skip it.

---

---

# Decision: TimeMachine Hero Concept (Candidate I)

**Proposed by:** J. Peterman
**Date:** 2026-04-04
**Status:** Proposed — awaiting team review

---

## What changed

Candidate I in `brand/explorations/visual-language-exploration.html` has been upgraded from a minimal 2-state BTTF toy to a full-featured 18-line hero example.

## The improved snippet

```
precept TimeMachine

field Speed as number default 0
field FluxLevel as number default 0
invariant Speed >= 0 because "Even DeLoreans cannot drive in reverse through time"

state Parked initial, Accelerating, TimeTraveling

event FloorIt with TargetMph as number, Gigawatts as number
on FloorIt assert TargetMph > 0 because "The car has to be moving, Doc"
on FloorIt assert Gigawatts > 0 because "The flux capacitor cannot run on vibes"

event Arrive

from Parked on FloorIt -> set Speed = FloorIt.TargetMph -> set FluxLevel = FloorIt.Gigawatts -> transition Accelerating
from Accelerating on FloorIt when FloorIt.TargetMph >= 88 && FloorIt.Gigawatts >= 1.21 -> set Speed = FloorIt.TargetMph -> set FluxLevel = FloorIt.Gigawatts -> transition TimeTraveling
from Accelerating on FloorIt -> reject "Roads? Where we're going, we still need 88 mph and 1.21 gigawatts."
from TimeTraveling on Arrive -> set Speed = 0 -> set FluxLevel = 0 -> transition Parked
```

**Compiles clean — zero diagnostics.**

## Why this concept

### Part 1 — What makes a great hero

A hero example must do three things simultaneously: teach the DSL surface, demonstrate the brand voice, and make the reader smile. The `because` messages are the brand's one permitted moment of wit — they're the human voice inside the machine, and that voice earns trust from developers.

The original TimeMachine (candidate I) failed on all three counts: no invariant (the most important constraint mechanism, invisible), only 2 states (no state machine shape), no `when` guard (key conditional logic, missing), no `reject` (the enforcement story untold), and flat `because` messages ("1.21 gigawatts required" — informational but not memorable).

Tone: the brand voice is authoritative and matter-of-fact with warmth. Serious but not humorless. The `because` messages are the exception — they may carry personality because they are authored by the developer, not the framework.

### Part 2 — Two concepts considered

**Option A: Improved TimeMachine (winner)**
- Domain: Back to the Future DeLorean time machine
- Why it's funny: Everyone knows the 88mph/1.21 gigawatt conditions. The DSL asserts map perfectly to the film's exact physics contract. The reject message subverts the film's most famous line.
- DSL features: invariant, 3 states, event with 2 args, dual event asserts, when guard on event args, reject, dotted access, clean 3-state cycle
- Why it wins: universal cultural legibility, physics constraints map naturally to precept constraints, the `because` messages are immediately memorable

**Option B: EspressoMachine (runner-up)**
- Domain: Specialty espresso shot workflow (Cold → Preheating → Ready → Pulling)
- Why it's funny: treating coffee with engineering seriousness; `because` messages like "Anything under 7 grams is technically a beverage, not an espresso" and "The boiler is not at temperature. This is espresso, not hot brown water."
- DSL features: 4 states, invariant on BoilerTemp, event with domain args (DoseGrams, GrindSize), when guard on boiler temperature, reject on cold-pull attempt
- Why it doesn't win: requires more domain explanation; TimeMachine's conditions are already universally known, zero cognitive overhead

### Part 3 — Feature checklist

| Feature | Present |
|---------|---------|
| field(s) with defaults | ✅ Speed, FluxLevel default 0 |
| invariant with `because` | ✅ Speed >= 0 because "Even DeLoreans..." |
| 3+ states | ✅ Parked, Accelerating, TimeTraveling |
| event with args | ✅ FloorIt with TargetMph, Gigawatts |
| event assert with `because` | ✅ Two on FloorIt asserts |
| `when` guard | ✅ when FloorIt.TargetMph >= 88 && FloorIt.Gigawatts >= 1.21 |
| `reject` | ✅ from Accelerating on FloorIt -> reject "Roads?..." |
| set with dotted access | ✅ FloorIt.TargetMph, FloorIt.Gigawatts |
| clean transitions | ✅ Parked → Accelerating → TimeTraveling → Parked |

### The `because` messages — brand voice rationale

- `"Even DeLoreans cannot drive in reverse through time"` — dry, matter-of-fact, earns a smile
- `"The car has to be moving, Doc"` — addresses the reader directly; the "Doc" makes it feel authored, not generated
- `"The flux capacitor cannot run on vibes"` — this is the best line in the snippet; "vibes" is the exact wrong energy for a precision instrument
- `"Roads? Where we're going, we still need 88 mph and 1.21 gigawatts."` — perfect subversion of the film's most famous line; makes the constraint feel inevitable

## Decision requested

Should candidate I be promoted as the third shortlisted hero candidate alongside B′ (ParkingMeter) and H′ (TrafficLight)? If promoted, the shortlist note should be updated to reflect the three candidates and their distinct tradeoffs.

---

### 2026-04-04: User directive — model upgrade policy
**By:** Shane (via Copilot)
**What:** Always use latest 4.6 Claude models (claude-opus-4.6 / claude-sonnet-4.6). Never use haiku. Uncle Leo uses gpt-5.4 for large context window code reviews. Steinbrenner (PM) upgraded from haiku to claude-sonnet-4.6.
**Why:** User request — captured for team memory

---

---

# Hero Domain Verdict — Subscription

**Decision:** The Precept hero snippet domain is **Subscription**.

**Requested by:** shane
**PM:** Steinbrenner
**Status:** Final — execute against `steinbrenner-hero-sample-brief.md`

---

## The Winning Domain

**Subscription** (`Trial → Active → Suspended → Cancelled`)

## Three Reasons

1. **Maximum recognizability.** The subscription lifecycle is universally legible to any backend .NET developer in under three seconds — no industry context required. Every engineer has built or integrated a billing/subscription system. The projection is immediate and frictionless.

2. **Natural three-construct proof.** The domain generates the three hero constructs without forcing: `invariant MonthlyPrice >= 0` (obvious business fact), `reject "Cancelled subscriptions cannot be reactivated"` (obviously correct blocked path), `when PlanName == null` or similar guard (conditional engine reasoning). No contrived rules needed.

3. **Line budget fits cleanly.** State names are short (Trial, Active, Suspended, Cancelled). Field names are short (MonthlyPrice, PlanName). Events are short (Activate, Suspend, Cancel). The hero fits 15 lines with room for structural blank lines — no cramming.

## Ruled Out

- **TimeMachine:** Scored 1/5. Fantasy domain. No invariant, no when guard, no reject. Pop culture because messages violate brand voice. Disqualified.
- **Loan:** Canonical 35-line sample already exists in `samples/loan-application.precept`. A 15-line version would be a worse imitation of existing work.
- **Shipment:** Too many bootstrap fields (weight, carrier, address) to generate natural rules within the line budget.
- **ServiceTicket:** Strong (5/5) but narrower projection target than Subscription — requires knowing your team uses a ticketing system.

---

---

# Hero Example Spec — TimeMachine Replacement

**Status:** Spec — ready for J. Peterman to execute
**Requested by:** shane
**PM:** Steinbrenner

---

## 1. What the Hero Must Demonstrate (Non-Negotiables)

The hero has ONE job: make a .NET developer read it and think "this is a real business rule engine, and I understand it immediately." Six DSL features earn that reaction — every one of them must appear:

| Feature | Why it's non-negotiable |
|---------|------------------------|
| `invariant … because` | THE headline claim: "invalid states structurally impossible" — if this is missing, the hero doesn't prove the product |
| `when` guard | Shows the engine makes conditional decisions, not just routes transitions |
| `reject … because` | Shows blocked paths — the contract refuses bad requests, it doesn't just log them |
| Event `with` args + `assert … because` | Shows input validation at the event boundary, not in user code |
| `set … = Event.Arg` (dotted access) | Shows the transition body is an atomic, auditable pipeline |
| Named states that mean something | States must read like a real lifecycle (not Parked/TimeTraveling) |

### Also show (structural density signals)
- `from` / `on` transition syntax — the core dispatch pattern
- `no transition` — in-place update without state change
- Comma source list (`from A, B on Event`) if possible without cost to readability

---

## 2. Why the Current TimeMachine Is Weak

### Feature gaps (15 lines, but only ~7 DSL features)
- **No `invariant`** — the product's marquee claim is entirely absent
- **No `when` guard** — no conditional branching, no domain logic
- **No `reject`** — no demonstration that invalid states are impossible; nothing is ever refused
- **No `no transition`** — no in-place update pattern

### Domain problems
- **Fantasy domain** — a DeLorean is not a workflow a .NET developer will map to their codebase. The abstract distance is too high.
- **Pop culture asserts** — `TargetSpeed == 88` and `Gigawatts >= 1.21` are jokes, not business rules. They show the syntax but communicate nothing about why the engine matters.
- **Brand voice violation** — brand says "Serious. No jokes." The TimeMachine is a joke. It also wastes the gold `because` messages on punchlines, which is the only warm hue in the palette.

### Mechanical problems
- The `Accelerate` transition is crammed onto one line: `-> set Speed … -> set FluxLevel … -> transition` — hard to scan; defeats the top-to-bottom readability argument
- `Arrive` just zeros everything — a reset with no rule. No guard, no assertion, no business meaning.

---

## 3. Line-Count Target

**15 lines, hard cap.** Reasoning:
- LoanApplication (the full product demo) is 35 lines. A hero is not a tutorial.
- Both shortlisted candidates (B′ ParkingMeter, H′ TrafficLight) hit 15 clean.
- 15 lines ≈ a code block that fits in a README without a scroll affordance on most screens.
- Below 13 lines: too sparse to show enough features. Above 17: starts to look like documentation, not a hero shot.

Structural budget at 15 lines (±1):
```
1  precept Name

---

## Hero Domain Selection: Subscription Billing

**Date:** 2026-05-01
**Author:** J. Peterman
**Status:** Decided

### Decision

The rank-1 domain chosen as the new hero in `brand/explorations/visual-language-exploration.html` is **Subscription Billing**.

### Rationale

Subscription Billing scored 29/30 in the hero deliberation (tied with SaaS Trial and Coffee Order), winning the tiebreak on Precept Differentiation (5/5). It was selected as rank #1 for:

- **Universal recognition**: The SaaS trial → active → cancelled lifecycle is immediately understood by any developer, regardless of stack or industry.
- **Quintessential structural impossibility**: `reject "Cancelled subscriptions cannot be reactivated"` is the clearest possible expression of the product thesis — "invalid states are structurally impossible."
- **Multi-line hero format**: The `from Trial on Activate when PlanName == null` block reads like a product spec, teaching five DSL concepts in four lines.
- **Full DSL coverage** (5/5): invariant, when guard, reject, dotted set, transition, no transition, 3 states, typed event, event assert — all present.

### HTML Changes Made

1. **Hero card**: Replaced loan-application.precept with subscription.precept (rank #1, score 29/30).
2. **Rubric section**: New section inserted after hero, showing the 6-criterion scoring table with max scores and descriptions.
3. **30-Candidate Gallery**: All 30 deliberation candidates displayed in rank order, each with badge (gold/silver/bronze), score breakdown, DSL snippet (inline-styled), and reasoning sentence.
4. **Final Ranking Table**: Compact table showing all 30 candidates with per-criterion scores and notes.

### Candidates Tied at 29/30

| Rank | Domain | Why ranked below |
|------|--------|-----------------|
| 1 | Subscription Billing | Winner — strongest differentiation |
| 2 | SaaS Trial | Slightly lower narrative immediacy |
| 3 | Coffee Order | Lower Diff score (4 vs 5) |
2  (blank)
3  field … default
4  invariant … because
5  (blank)
6  state … initial, …
7  (blank)
8  event … with … as …
9  on … assert … because
10 (blank)
11 from A, B on Event
12   -> set … = Event.Arg
13   -> transition C
14 from C on Event when …  -> reject "…"
15 from C on Event          -> no transition
```

That structure shows every non-negotiable in 15 lines with one blank separator budget.

---

## 4. What "Cute/Funny" Actually Buys

Nothing. Less than nothing.

The brand voice is "authoritative with warmth." Warmth means plain-language `because` messages that sound like a real product owner wrote them — e.g., "Approved loans must have verified documents." Not punchlines.

The TimeMachine's humor signals: "this is a toy demonstration." The product needs to signal: "this is a production runtime." The developer reading the README needs to see themselves using this in their actual codebase. A DeLorean blocks that transfer.

**Tone rule for J. Peterman:** the `because` messages are the only copy that breathes. Write them in the voice of a domain expert who cares about correctness, not a comedian. One wry edge is acceptable if the domain earns it (ParkingMeter's "Negative time is a mathematical luxury we cannot afford" lands because it's true and precise, not because it's a pop culture reference). Zero references to movies, TV, or internet culture.

---

## 5. Winning Example Checklist

J. Peterman executes against this list. All items must be checked.

**Domain**
- [ ] Real-world domain — something a .NET developer recognizes as a workflow they might own
- [ ] 2–4 states with meaningful names (a lifecycle, not arbitrary labels)
- [ ] Domain makes the `reject` condition self-evidently correct (reader should think "of course that's rejected")

**Features**
- [ ] `invariant … because` present — data integrity, not just event validation
- [ ] `when` guard on at least one transition
- [ ] `reject "…"` on at least one blocked path
- [ ] `on Event assert … because` — event-level input validation with a message
- [ ] `set Field = Event.Arg` — dotted access in transition body
- [ ] `no transition` — in-place update (or comma sources if budget allows both)
- [ ] `from … on … -> … -> transition` — multi-step transition body (must be multi-line, not crammed)

**Craft**
- [ ] 15 lines ±1
- [ ] `because` messages are domain-appropriate, not jokes or pop culture references
- [ ] All 4 semantic color families are represented: indigo structure, violet states, cyan events, slate data, gold messages
- [ ] The snippet compiles clean against `precept_compile`
- [ ] Multi-step transition bodies are line-broken (one `->` per line)

**Brand**
- [ ] No jokes; no movie/TV references
- [ ] No hedging ("kind of", "basically") — state facts
- [ ] Domain maps directly to what a .NET backend developer builds

---

## Candidate Domain Suggestions (for J. Peterman)

These are starting points, not mandates:

- **Subscription** — `Trial → Active → Suspended → Cancelled`: natural lifecycle, `reject` on reactivating a cancelled subscription, `invariant` on billing amount
- **ServiceTicket** — `Open → InProgress → Resolved → Closed`: multi-state, `reject` on resolving without a resolution note, `when` guard on SLA breach
- **ShipmentOrder** — `Pending → Dispatched → Delivered`: `invariant` on weight, `reject` on dispatching with no carrier, `when` based on weight threshold
- **MediaUpload** — `Queued → Processing → Published → Archived`: `reject` on publishing zero-byte file, `when` on file size, `no transition` on metadata update

Avoid: anything that requires 3+ fields to make the domain legible (kills the line budget). Avoid: domains that require long state names.

---

---

# Steinbrenner — Lang Spec Review Decisions

**Date:** 2026-04-04
**Author:** Steinbrenner (PM)
**Source:** language-spec-brief.md deep-dive

---

## Decision 1 — Syntax Highlighting is a Release Gate

**Decision:** The 8-shade semantic palette (SyntaxHighlightingImplementationPlan.md) must ship before v1 release. It is not a backlog item.

**Rationale:** The entire "color encodes compiler-known meaning" value proposition is undelivered. The extension currently renders `.precept` files with whatever the active theme provides. The brand spec, design doc, and 8-phase implementation plan are all locked and waiting. Phase 0 (TokenCategory.Grammar refactor) is a standalone 1-day change. Phases 1-6 are mechanical. Phases 7-8 are explicitly deferred and do not need to ship in v1.

**Owner needed:** George (or any engineer). Assign Phase 0 immediately.

---

## Decision 2 — RulesDesign.md Must Be Archived or Rewritten

**Decision:** RulesDesign.md must be updated before any new contributor or AI agent reads it.

**Rationale:** The doc says "Status: Implemented" and describes a `rule` keyword with indented syntax and type-prefix field declarations (`number Balance = 0`) that do not exist in the current language. No sample file uses `rule`. The current language uses `invariant` and `assert`. This is a documentation liability that actively misleads anyone trying to understand or author the language.

**Action:** Either (a) archive to `docs/archive/` and note the supersession, or (b) rewrite to accurately describe how invariants and asserts map to what `rule` was designed to do.

---

## Decision 3 — CLI-or-Kill: Decision Required

**Decision:** A decision must be made on whether to implement or permanently defer the CLI.

**Rationale:** The CLI design (CliDesign.md) has been pending since the old CLI host was removed. The MCP server now covers the same workflows. The @ToDo.md explicitly asks whether the CLI is still needed. Every roadmap review re-encounters this unresolved question. The design doc also uses stale `Dsl*` naming (pre Phase 0-3 renames) — it needs an audit pass before it could be implemented.

**Options:**
- **Kill:** Archive CliDesign.md, remove CLI from "Later" items. Rely on MCP for all machine interactions.
- **Implement:** Assign to an engineer, complete naming audit, implement in a dedicated milestone.
- **Defer with a hard date:** Set a specific milestone where this decision is revisited.

**Recommendation:** Kill. The MCP surface is live, the 5-tool surface covers inspect/fire/update, and the AI-first workflow is better served by MCP than a terminal REPL. Sample integration tests can be written directly against the C# API or MCP tools.

---

## Decision 4 — Contradiction/Deadlock Detection: Downgrade in Spec or Implement

**Decision:** The spec must be honest. Checks #4 and #5 in PreceptLanguageDesign.md are labeled as compile-time **errors** but are not implemented. This is a false promise.

**Options:**
- **Implement:** Requires interval/set analysis on expression ASTs. Non-trivial work. Estimate: 1-2 sprints.
- **Downgrade in spec:** Change their severity from "Error" to "Future Work" or "Planned" in the design doc. Makes the spec honest without implementation commitment.

**Recommendation:** Downgrade in spec now; implement later if interval analysis is ever prioritized.

---

## Decision 5 — Preview Protocol: Structured Violations Roadmap

**Decision:** Structured violations in the preview protocol should be scheduled for the release after syntax highlighting ships.

**Rationale:** The runtime returns full `ConstraintViolation` objects. The webview still receives flat strings. Field-level inline highlighting in the inspector panel is blocked by this gap. Not urgent, but it's a visible UX deficit in the flagship developer surface.

**Action:** Schedule as the next "Later" item to graduate to an active milestone after syntax highlighting and plugin distribution are done.

---

---

# Spec Review Findings — Inbox

**Date:** 2026-03-27
**From:** Steinbrenner (PM)
**Artifacts:** `.squad/agents/steinbrenner/spec-brief.md` (27KB comprehensive inventory)

---

## Summary for the Team

I've read the entire language spec, design docs, and implementation plan. Here's what you need to know:

### What We Have

**Language:** Feature-complete. Flat keyword-anchored syntax, four assert kinds, state actions, first-match transitions, editable fields, 50+ compile-time checks, graph analysis. All working.

**Tooling:** Complete. Parser (Superpower), compiler (type-checker + graph analysis), runtime (Fire + Inspect + Update), language server, VS Code extension, preview webview, Copilot plugin (agent + 2 skills), MCP server (5 tools).

**Quality:** High. Type safety locked in (Phases D–H). Constraint violations now structured (Phase CV). Graph analysis warnings (Phase I). 666 tests passing.

**Samples:** 20 `.precept` files demonstrating major features.

### What's NOT Done (But Planned)

| Item | Status | Why Deferred |
|---|---|---|
| CLI | Design exists; code not written | Lower priority; MCP already covers programmatic access |
| Same-preposition contradiction detection | Designed | Requires sophisticated interval/set analysis |
| Cross-preposition deadlock detection | Designed | Same analysis as above |
| Fluent interface for runtime | Nice-to-have | Ergonomic improvement; API works as-is |

**Blocking item:** None. Language is production-ready.

### For the Hero Example

**Pick a concrete domain** (loan application, work ticket, shipment — not fantasy). Demonstrate:
1. Fields + invariants (constraints)
2. States + events (workflow)
3. Transition with guard (routing)
4. One or two asserts showing "invalid states impossible"

**10–15 lines max.** The language can do it. Just need a domain that lets viewers see themselves in it.

---

## Key Design Wins

1. **Prepositions (in/to/from/on) carry consistent meaning everywhere.** Reduces keyword bloat; makes syntax learnable.

2. **First-match transitions instead of exclusive clauses.** Simpler semantics, common pattern (guarded special case + unguarded fallback).

3. **Four assert kinds instead of one generic "rule."** Each temporal scope (entry, exit, residing, event-args) deserves its own keyword. No confusion about when checks run.

4. **Type checking at compile time.** Workflow bugs caught before preview, not discovered on fire. This is a product feature, not implementation detail.

5. **Constraint violations with Source + Targets.** Enables precise error attribution (inline for field targets, banner for scope). No more string-guessing in the UI.

---

## Three Decisions the Team Should Make

### 1. Hero Example Domain & Timeline

**Decision needed:** Which workflow should we use to introduce Precept to the world? (Loan application? Service ticket? Shipment tracking?) And when?

The language is ready. Hero examples are ready. Timing is now a business/marketing decision, not a technical one.

### 2. CLI Implementation Priority

**Decision needed:** The design exists. Do we implement it now, or defer to post-launch?

Trade-off: CLI gives standalone access (good for automation, testing, CI/CD). But MCP already covers programmatic use and the extension covers interactive use. CLI is nice-to-have, not blocking.

### 3. Same/Cross-Preposition Analysis (Contradiction + Deadlock Detection)

**Decision needed:** Implement the interval/set analysis to catch "state is provably unsatisfiable" at compile time, or leave that discovery to inspect/fire?

Trade-off: Catching contradictions at compile time is powerful and differentiating. But it's non-trivial algorithm work and the runtime already detects these via simulation. Both are valid choices.

---

## What the Spec Brief Contains

- **Language Surface** — full inventory of keywords, constructs, types, expressions
- **Implementation Status** — what's done vs. planned with rationale
- **Key Design Decisions** — the "why" behind syntax and semantics choices
- **API Surface** — complete public C# API (Parse, Compile, Engine, Fire, Inspect, Update)
- **Constraint System** — end-to-end: four kinds, compile checks, fire pipeline order
- **Open Design Questions** — things still being figured out
- **For Hero Work** — which features are stable/showable vs. avoid
- **PM Assessment** — three strategic priorities for the product

**Location:** `.squad/agents/steinbrenner/spec-brief.md` (27KB; comprehensive; reference-grade)

---

## Recommendations

1. **Land the hero example in the next sprint.** Language is done. Choose a domain, write the precept, put it in the README, use it for all marketing. This is the single best way to communicate what Precept does.

2. **Keep type safety + constraint violations on the radar.** These are what differentiate Precept from other state machine DSLs. Document them. Show them off.

3. **Mark CLI as "next phase" not "blocking."** MCP + extension cover the current use cases. Revisit after launch if demand justifies.

4. **Table the interval/set analysis work.** Designed but non-trivial. Ship without it, add it later if teams report "I want this detected at compile time" feedback.

---

---

# Phase 1 Research Findings — Hero Research Sprint (2026-04-04T13:39:08Z)

*Filed by Scribe; consolidated from 4 agents (J. Peterman, Steinbrenner, Uncle Leo, George)*

---

## Finding 1: Named Guard Declarations (Priority: HIGH)

**From:** Steinbrenner (DSL Expressiveness Research)

**Decision Inbox Merged**

This section consolidates inbox items from Phase 1 research agents:
- steinbrenner-dsl-research.md
- uncle-leo-verbosity-analysis.md
- george-language-research.md
- j-peterman-hero-creative-brief.md

### 2026-04-04T19:45:58Z: User directive
**By:** Shane (via Copilot)
**What:** Reframe `brand/brand-spec.html` so §1.4 presents only the locked 5 semantic families plus 3 outcome colors; remove `brand-light` and `brand-muted` from the semantic color story entirely unless a specific surface truly needs a local tonal variant. §2.1 may use shading variants within a family as needed, but the general color story should remain 5+3.
**Why:** User request — captured for team memory

---

### 2026-04-04T19:55:35Z: User directive
**By:** Shane (via Copilot)
**What:** In the generic color spec, do not call green/yellow/red "outcome colors." They are only outcome colors in §2.2 when tied to transition/runtime outcomes. In the general spec they need a different name.
**Why:** User request — captured for team memory

---

### 2026-04-04T20:10:30Z: User directive
**By:** Shane (via Copilot)
**What:** Stop using Haiku for agent spawns going forward.
**Why:** User request — captured for team memory

---

### 2026-04-04: User directive — docs terminology + ownership
**By:** Shane (via Copilot)
**What:**
- Do NOT call it "docs site". It is just "docs".
- Docs are internal team artifacts owned by the team.
- README is the only public-facing exception right now.
- In the future, when public docs exist, Peterman owns them.
- Future public docs site = planned, not current scope.
**Why:** User request — captured for team memory

---

### 2026-04-04T14-21-55: File organization directive
**By:** shane (via Copilot)
**What:** Research files should be stored according to domain ownership: Peterman's research (brand/copy) → rand/references/; Steinbrenner's research (PM/product/docs) → docs/references/. Each agent's research artifacts live under their domain root, not a shared location.
**Why:** User request — captured for team memory

---

---

# Decision: Color Usage Roles for 8+3 System

**Date:** 2026-07-12
**Filed by:** Elaine (UX Designer)
**Status:** LOCKED
**Affects:** brand-spec.html §1.4, §1.4.1; README revamp; all product surfaces

## Summary

Defined concrete color usage roles for every color in the locked 8+3 system. Updated brand-spec.html §1.4 palette card to correctly display all 8+3 colors. Added §1.4.1 Color Usage as the definitive reference for how each color maps to product surfaces and README context.

## What Changed

### §1.4 Palette Card — Fixed

**Problem:** The palette card showed an indigo gradient with 8 shades from color-exploration.html, including off-system colors (`#a5b4fc`, `#1e1b4b`, `#312e81`, `#3730a3`). It was an "Indigo overview" card, not the 8+3 brand system.

**Fix:** Replaced with a full-width palette card showing all 8 core colors + 3 semantic colors + gold accent note, organized as:
- Brand Family (indigo trio): `#6366F1` brand, `#818CF8` brand-light, `#C7D2FE` brand-muted
- Text Family: `#E5E5E5` text, `#A1A1AA` text-secondary, `#71717A` text-muted
- Structural: `#27272A` border, `#09090B` bg
- Semantic: `#34D399` success, `#FB7185` error, `#FCD34D` warning
- Note: `#F59E0B` gold (syntax accent only)

### Verdict Colors — Fixed

**Problem:** Verdicts section used wrong hex values inherited from earlier exploration:
- Error: `#F87171` → corrected to `#FB7185`
- Warning: `#FDE047` → corrected to `#FCD34D`

**Why:** The locked semantic colors are emerald `#34D399`, rose `#FB7185`, amber `#FCD34D`. The old values (`#F87171` = Tailwind red-400, `#FDE047` = Tailwind yellow-300) were never part of the locked system.

### §1.4.1 Color Usage — New Section

Added three subsections:

1. **Color Roles table** — All 12 colors with role name and specific usage examples across product surfaces.

2. **README & Markdown Application table** — How brand color maps to GitHub Markdown constraints (wordmark SVG, shields.io badge parameters, hero code block, emoji alignment).

3. **Color Usage Q&A** — Answers five common questions:
   - Secondary highlight → brand-light `#818CF8`
   - Success green in README → CI badges, feature checkmarks
   - Warning amber in README → beta/preview callouts
   - Error rose in README → No (product UI only)
   - Gold outside syntax → No (syntax-only, never UI)

4. **README Color Contract callout** — Defines the three channels for brand identity in plain Markdown: SVG wordmark, shields.io badge row, and DSL keyword rhythm in hero code block.

## Key Decisions

| Decision | Rationale |
|----------|-----------|
| Error rose `#FB7185` never appears in README | Marketing copy should never communicate failure. Error states are product UI feedback, not public messaging. |
| Gold `#F59E0B` is syntax-only, not UI | Gold exists to distinguish human-readable rule messages from machine code. Using it in badges or UI would dilute that semantic precision. |
| Brand identity in GitHub Markdown comes through SVG + badges, not CSS | GitHub strips custom styles. Instead of fighting the platform, the brand speaks through assets (wordmark) and parameters (shields.io colors) that survive rendering. |
| Warning amber is valid in README for beta/preview | Unlike error (which implies failure), warning signals "attention needed" — appropriate for preview features and cautionary notes. |
| Secondary highlight is always brand-light, not a new color | The 8+3 system is closed. Brand-light `#818CF8` serves the "accent" role that might otherwise tempt someone to introduce a new color. |

## Downstream Impact

This decision record is the direct input for:
1. **README revamp** — Peterman can use the README Application table and Q&A without follow-up questions about color.
2. **Brand-spec §1.4 LOCKED status** — The palette card + usage guidance are now definitive enough to lock.
3. **Badge catalog** — Kramer's badge work should use the shields.io color values documented here.

## Off-System Colors Removed from §1.4

| Hex | What It Was | Why Removed |
|-----|-------------|-------------|
| `#a5b4fc` | Indigo gradient swatch (Tailwind indigo-300) | Not in locked 8+3. Shane explicitly called out as off-system. |
| `#1e1b4b` | Deep indigo gradient swatch | Part of indigo exploration ramp, not the brand system. Still used in callout styling as page chrome. |
| `#312e81` | Dark indigo gradient swatch | Same as above. |
| `#3730a3` | Medium-dark indigo gradient swatch | Same as above. |
| `#F87171` | Verdict "Blocked" color (Tailwind red-400) | Wrong. Locked error color is `#FB7185`. |
| `#FDE047` | Verdict "Warning" color (Tailwind yellow-300) | Wrong. Locked warning color is `#FCD34D`. |

---

---

# Palette Mapping Visual Unification

**Filed by:** Elaine
**Date:** 2026-04-04
**Status:** COMPLETE
**Scope:** `brand/brand-spec.html` — Sections 2.1 and 2.2

## Summary

Unified the palette mapping visual treatment in §2.1 (Syntax Editor) and §2.2 (State Diagram) to match the polished §1.4 (Color System) design language. Created a reusable `.spm-*` CSS component system for surface palette mappings.

## What Changed

### New CSS Component System (`.spm-*`)

Added ~70 lines of scoped CSS creating a "Surface Palette Mapping" design system:

| Component | Purpose |
|-----------|---------|
| `.spm-surface` | Container card with dark background and indigo border |
| `.spm-header` | Color-tinted section headers with gradient backgrounds |
| `.spm-row` / `.spm-grid` | Grid-based layout for swatches and info |
| `.spm-swatch` | Gradient swatches with subtle shadows (56×38px) |
| `.spm-title` / `.spm-hex` / `.spm-weight` / `.spm-tokens` | Consistent info typography |
| `.spm-table-section` / `.spm-table` | Polished table treatment for diagram mappings |
| `.spm-shapes` / `.spm-shape-tile` | Unified shape legend tiles |

### §2.1 Syntax Editor

- Consolidated 7 separate `.card` elements into one `.spm-surface` container
- Each color family (Structure, States, Events, Data, Rules) gets a tinted header
- Grouped as: Core Semantic Tokens → Support Tokens → Reserved Verdict Colors
- Gradient swatches matching §1.4 visual treatment

### §2.2 State Diagram

- Shape tiles now use `.spm-shape-tile` — unified 4-column grid
- Static elements table wrapped in `.spm-table-section` with colored header dot
- Runtime verdict overlay table uses same refined treatment
- Mini-swatches (20×20px) inline with color names in tables

## Why

§2.1 and §2.2 contained the same semantic information as §1.4, but presented with inconsistent visual treatments. The brand spec is itself a brand artifact — visual coherence matters. The unified treatment:

1. Makes the document scannable — color-coded headers provide instant orientation
2. Reinforces the 5+3 semantic model — grouping makes structure obvious
3. Demonstrates brand quality — polished documentation signals polished product
4. Enables reuse — future surface sections (Inspector, Docs, CLI) can use `.spm-*` components

## Semantic Content Preserved

- All locked color values unchanged (5 semantic families + 3 signal colors)
- All element-to-color mappings unchanged
- All hex codes, weight/style notes, and token lists intact
- Runtime verdict semantics remain scoped to §2.2 only
- No new semantic colors introduced

## Applicability

The `.spm-*` system is intentionally general. When §2.3 (Inspector), §2.4 (Docs), or §2.5 (CLI) need detailed element-to-color mappings, the same components apply.

---

---

# README UX Requirements (Elaine)

**Date:** 2026-04-04
**Source:** UX/IA review of Peterman and Steinbrenner's README research
**Status:** Inbox — awaiting incorporation into README restructure proposal

---

## Non-Negotiable Requirements

The final README restructure proposal **must** satisfy all of these constraints:

### 1. Mobile-First "Above the Fold"

**Requirement:** Logo, hook, and primary CTA must be visible in a **550px vertical viewport** without scrolling.

**Rationale:** GitHub mobile web traffic is significant. Developers on phones decide "Is this relevant?" in the first screenful. If they have to scroll to see the value prop, they bounce.

**Validation:** Test rendered README on:
- 400px width (phone portrait)
- 600px width (phone landscape / small tablet)
- 800px width (tablet / narrow desktop)

**Current violation:** Current README's logo + hook + hero code = ~1200px vertical. Hero doesn't appear above fold on mobile.

---

### 2. Single Primary CTA

**Requirement:** The Getting Started section must present **one clear next action**, with secondary actions labeled as "next steps."

**Rationale:** Multiple equally-weighted CTAs = decision paralysis. Developers presented with three options (package, extension, plugin) choose none.

**Implementation:** Numbered sequence:
1. Install VS Code extension (primary CTA)
2. Create first file (follow hero)
3. Add NuGet package (integration step)

Secondary CTA (Copilot plugin) goes in a separate "Advanced" section.

**Current violation:** Three CTAs with equal weight in Quick Start section.

---

### 3. Semantic Heading Hierarchy

**Requirement:**
- H1 for title (project name)
- H2 for major sections (Getting Started, What Makes Precept Different)
- H3 for subsections (1. Install Extension, 2. Create File)
- **No heading level skips** (H2 → H4 is invalid)
- **Headings must be descriptive, not clever**

**Rationale:**
- Screen reader users navigate by heading level
- AI agents parse heading structure to build document outline
- Skipping levels breaks both use cases

**Current violations:**
- Emoji in H2 headings (`🚀 Quick Start`) — screen reader announces "rocket emoji Quick Start"
- Clever headings (`💡 The "Aha!" Moment`) instead of descriptive (`Quick Example`)

**Fix:** Move emoji to end of heading or remove. Replace clever headings with descriptive labels.

---

### 4. Progressive Disclosure

**Requirement:** Section order must follow the evaluation journey:
1. **What is this?** (Hook — 1 sentence)
2. **Can I read this?** (Hero DSL — 18 lines)
3. **Can I use this?** (Quickstart — 5 lines C#)
4. **What makes this different?** (AI tooling, features)
5. **Where do I learn more?** (Docs, samples)

**Rationale:** Each section deepens commitment. Never front-load complexity before proving basic usage works.

**Current violations:**
- Sample catalog appears before quickstart (reference material before onboarding)
- Tooling features described before C# usage is shown
- Philosophical "Pillars" content interrupts onboarding flow

**Fix:** Defer differentiation (AI tools, MCP server) until after Getting Started. Move sample catalog to "Learn More" section at bottom.

---

### 5. Scannable Formatting

**Requirements:**
- Prose paragraphs: **max 2-3 sentences**
- Feature lists: **bullets, not prose**
- Visual separators: **horizontal rules (`---`) between major sections**
- Callout boxes: **blockquotes (`>`) for important asides**

**Rationale:** F-pattern scanning research shows developers scan headings (left edge) and sweep horizontally for interesting content. Wall-of-text paragraphs are skipped.

**Current violations:**
- "The Problem It Solves" section: 150-word prose paragraph
- "World-Class Tooling" section: 6 features in paragraph form
- No horizontal rules between sections

**Fix:** Break prose into chunks, convert feature descriptions to bulleted lists, add visual separators.

---

### 6. Viewport Resilience

**Requirements:**
- Hero code block: **no horizontal scrolling at 600px width**
- Tables: **≤3 columns** (wider tables moved to external docs)
- Images: **responsive scaling** (width="100%" or similar)

**Rationale:** GitHub renders READMEs in a narrow column on desktop and mobile. Long code lines and wide tables break the layout.

**Current violations:**
- Hero code block: 49 lines with some long lines (requires horizontal scroll at narrow widths)
- Sample catalog table: 21 rows × 3 columns (collapses to single-column stack on mobile, unreadable)

**Fix:** Reduce hero to 18-20 lines, move catalog table to external docs page.

---

### 7. Screen Reader Compatibility

**Requirements:**
- Emoji: **after heading text or removed** (not at start)
- Badge alt text: **includes version/status** (not just "NuGet Badge")
- Code blocks: **preceded by descriptive labels** ("Example: Defining an order workflow")
- Heading hierarchy: **valid for navigation** (no H2 → H4 skips)

**Rationale:** Screen reader users navigate by heading level and rely on alt text for image context.

**Current violations:**
- All H2 headings start with emoji (noisy for screen readers)
- Badge alt text: generic ("NuGet Badge" instead of "NuGet version 1.0.0")
- Some code blocks lack descriptive labels

**Fix:** Move emoji to heading end, update badge alt text, add labels before code blocks.

---

### 8. AI Parseability

**Requirements:**

#### Code Blocks
- **All code blocks tagged with language:**
  - ` ```precept ` for DSL samples
  - ` ```csharp ` for C# runtime code
  - ` ```bash ` for shell commands
- **Preceded by descriptive labels:**
  - "**The Contract** (`time-machine.precept`):" before DSL
  - "**The Execution** (C#):" before runtime sample

#### Links
- **Descriptive link text** (no "click here")
  - ✅ "[Language Reference](link)"
  - ❌ "[here](link)"
- **Absolute URLs** for external links (AI agents don't always resolve relative paths)

#### Images
- **Descriptive alt text:**
  - ✅ `![State diagram showing Parked → Accelerating → TimeTraveling transitions](path)`
  - ❌ `![diagram](path)`
- **Text description in surrounding prose** (for AI agents that can't parse images)

#### Structure
- **Semantic HTML via Markdown:**
  - H1 → H2 → H3 (no level skips)
  - Horizontal rules (`---`) between major sections
- **Feature lists use bullets** (not prose)

**Rationale:** When a developer asks Claude "What is Precept?", Claude reads `README.md`. If the structure isn't AI-parseable, Claude gives shallow or hallucinated answers.

**Current violations:**
- Some code blocks lack language tags
- Image alt text: generic ("Interactive Inspector")
- Links use "here" pattern in some places

**Fix:** Tag all code blocks, update image alt text, use descriptive link text.

---

## Recommended Section Hierarchy

The proposal should use this structure as a starting point:

```

---

# Precept
[Logo + Badges]

> **Definition:** A general rule intended to regulate behavior or thought.

**Hook:** Precept is a domain integrity engine for .NET...

---

## Quick Example

**The Contract** (`time-machine.precept`):
[18-20 line DSL hero]

**The Execution** (C#):
[5-line runtime usage]

---

## Getting Started

### 1. Install the VS Code Extension
[Instructions + marketplace link]

### 2. Create Your First Precept File
[Follow hero example]

### 3. Integrate with Your C# Project
[`dotnet add package` + quickstart guide link]

---

## What Makes Precept Different

### AI-Native Tooling
[MCP + Copilot + LSP description]

### Unified Domain Integrity
[Replaces state machines + validation + rules]

### World-Class Developer Experience
[Bulleted list of features]

---

## Learn More

- [Documentation](link)
- [Language Reference](link)
- [Sample Catalog](link)
- [Contributing](link)

---

## License
[MIT badge + copyright]
```

**Key decisions:**
- Hero before installation (proves readability first)
- Getting Started as numbered sequence (single primary CTA)
- Differentiation after quickstart (progressive disclosure)
- Sample catalog in "Learn More" (reference, not onboarding)

---

## AI Parseability Checklist

The proposal author must verify:

- [ ] All code blocks tagged with language (` ```precept `, ` ```csharp `, ` ```bash `)
- [ ] Code blocks preceded by descriptive labels
- [ ] Descriptive link text (no "click here")
- [ ] Image alt text includes content description
- [ ] Heading hierarchy valid (H1 → H2 → H3, no skips)
- [ ] Emoji after heading text or removed
- [ ] Badge alt text includes version/status
- [ ] Feature lists use bullets, not prose
- [ ] Tables ≤3 columns (wider tables linked externally)
- [ ] Horizontal rules between major sections

**Validation:** An AI agent should be able to:
1. Answer "What is Precept?" from hook + definition
2. Extract hero code with language tag preserved
3. Navigate to installation using heading hierarchy
4. Identify primary CTA from Getting Started sequence
5. Understand AI tooling from structured list
6. Find links to docs without parsing complex prose

If any task fails, the structure needs revision.

---

## Viewport Testing Plan

Before approving the proposal, test at:

- **400px width** (phone portrait): Logo + hook + CTA visible above fold?
- **600px width** (phone landscape): Hero code readable without horizontal scroll?
- **800px width** (tablet): Full Getting Started section visible?

Current README fails all three tests.

---

## Next Steps

1. **Proposal author** uses these requirements to draft new README structure
2. **Shane + Peterman** review for brand compliance
3. **Steinbrenner** reviews for adoption journey mapping
4. **Elaine** validates against UX requirements checklist
5. **Implementation** once all four approve

These are **constraints**, not suggestions. The proposal can refine the execution, but it cannot violate these requirements.

---

---

# Elaine — Reviewer Corrections Applied to Brand Spec

**Date:** 2026-04-04
**Status:** All corrections applied, ready for integration

---

## Summary

Three reviewers (George, Peterman, Frank) provided corrections to the 5-surface UX spec incorporated into `brand/brand-spec.html`. All corrections have been applied successfully.

---

## Corrections Applied

### From George (Technical)

1. **Diagnostic code range (line 632)**
   - **Issue:** Cited `PRECEPT001–PRECEPT047` instead of actual range `PRECEPT001–PRECEPT054` (54 constraints, not 47).
   - **Fix:** Updated example diagnostic code from `PRECEPT047` → `PRECEPT054`.
   - **Location:** `brand/brand-spec.html` §2.1 Diagnostics section.

2. **Inspector yellow NotApplicable state (line 788)**
   - **Issue:** Described "Warning (#FDE047) for unmatched guards" as one of the inspector's verdict states. The inspector does NOT show a yellow NotApplicable warning state — that outcome is filtered out entirely.
   - **Fix:** Removed reference to yellow warning for unmatched guards. Updated constraint violation description to "Blocked `#F87171` for rejected events or invariant violations."
   - **Location:** `brand/brand-spec.html` §2.3 Inspector Panel, Color application list.

3. **CLI surface describes non-existent tooling (§2.5)**
   - **Issue:** CLI surface spec described a `precept` CLI tool that does not exist. PRECEPT diagnostic codes are surfaced in VS Code Problems panel, not terminal output.
   - **Fix:** Changed section status from "LOCKED" to "ASPIRATIONAL". Added prominent callout at section start: "⚠️ CLI surface is planned; not yet implemented." Updated all prose to future tense and clarified this is a design contract for a future tool.
   - **Location:** `brand/brand-spec.html` §2.5 CLI / Terminal.

4. **Read-only fields mischaracterized (line 786)**
   - **Issue:** "Read-only" described as "fields that cannot be modified directly (computed, state-derived)." The DSL has no computed or state-derived field types. Read-only means editability-per-state.
   - **Fix:** Updated to "fields not declared editable in the current state via `in State edit Field`".
   - **Location:** `brand/brand-spec.html` §2.3 Inspector Panel, Color application list.

### From Peterman (Brand)

5. **State Diagram — off-system colors (lines 658, 666, 674, 739–750)**
   - **Issue:** State lifecycle roles (initial/intermediate/terminal) used three off-system hex values: `#A5B4FC`, `#94A3B8`, `#C4B5FD`. Event subtypes used off-system cyan shades: `#38BDF8`, `#7DD3FC`, `#0EA5E9`. This directly contradicted the "no lifecycle tints" principle stated in the same section.
   - **Fix:** Removed ALL off-system hex values. Replaced with locked system colors:
     - **States:** All use `#6366F1` (brand indigo) for borders, `#A898F5` (violet) for state names. Shape (circle, rounded rect, double-border) encodes lifecycle role.
     - **Events:** All use `#30B8E8` (locked cyan). Edge styling (solid, dashed, self-loop) differentiates event subtypes.
     - Added explicit callout: "No lifecycle tints: States are not tinted by role. Shape carries that signal."
   - **Location:** `brand/brand-spec.html` §2.2 State Diagram, visual samples and lifecycle tables.

### From Frank (Architectural)

6. **CLI surface — aspirational (same as George #3)**
   - **Issue:** Same as George's correction #3.
   - **Fix:** Applied (see George #3 above).

7. **Docs surface terminology**
   - **Issue:** Flagged potential confusion between "docs site" (public website) vs. internal `docs/` artifacts.
   - **Verification:** Reviewed §2.4 — already correctly described as internal team artifacts, not a public site. No changes needed.
   - **Location:** `brand/brand-spec.html` §2.4 Docs Surface.

8. **State Diagram — InitialState gap (§2.2)**
   - **Issue:** `PreceptPreviewSnapshot` does not currently expose `InitialState`, so the state diagram renderer cannot identify the initial state from the snapshot alone.
   - **Fix:** Added TODO callout in §2.2: "Protocol gap — TODO: `PreceptPreviewSnapshot` does not currently expose `InitialState`. Initial state highlighting in diagrams depends on adding this field to the protocol."
   - **Location:** `brand/brand-spec.html` §2.2 State Diagram, after state lifecycle roles table.

---

## Sections Affected

All corrections applied to:
- **§2.1 Syntax Editor** — Diagnostic code range updated
- **§2.2 State Diagram** — Off-system colors removed, locked colors enforced, InitialState gap documented
- **§2.3 Inspector Panel** — Yellow NotApplicable state removed, read-only field semantics corrected
- **§2.4 Docs Surface** — Verified correct (no changes needed)
- **§2.5 CLI / Terminal** — Marked as aspirational, added non-existent tool callout

---

## Remaining Open Items

**None.** All reviewer feedback addressed.

---

## Next Steps

1. Shane reviews corrected brand-spec.html §2.1–2.5
2. If approved, sections are formally locked
3. Implementation can proceed on editor (§2.1), diagram (§2.2), and inspector (§2.3)
4. CLI surface (§2.5) remains aspirational until tool is scoped

---

**Signed:** Elaine
**Date:** 2026-04-04

---

---

# Visual Surfaces UX Specifications — Incorporated into Brand Spec

**Date:** 2026-04-04
**Author:** Elaine (UX Designer)
**Status:** Locked
**Affects:** `brand/brand-spec.html` §2.3, §2.4, §2.5; `brand/explorations/visual-language-exploration.html`

## Decision

The 5-surface visual UX spec draft (previously in `brand/visual-surfaces-draft.html`) has been incorporated into `brand/brand-spec.html` as three fully specified, locked sections:

- **§2.3 Inspector Panel** — Runtime verdict surface with complete color, typography, accessibility, and AI-first specifications
- **§2.4 Docs** — Internal documentation artifacts (clarified scope: team-facing, not a public docs site)
- **§2.5 CLI / Terminal** — Command-line output with verdict color usage, symbol redundancy, and terminal compatibility constraints

All three sections are marked **LOCKED** and serve as implementation contracts for current and future work.

## Open Questions Resolved

1. **Inspector panel status:** Confirmed as fully implemented. Spec describes current behavior with brand color alignment recommendations.
2. **"Docs site" scope:** Clarified as internal team documentation artifacts (`docs/` folder), not a public-facing website. If Peterman designs a public docs site in the future, that's a separate surface.
3. **Light theme:** Not planned. Marked as backlog item across all surfaces. Current system is dark-mode-only.
4. **Accessibility audit:** Formal color-blind simulation and screen reader testing is a backlog item. Contrast ratios are documented; redundancy principles (color + shape/symbol) are locked.
5. **CLI color audit:** Current CLI tools need an audit pass to ensure compliance with locked spec. Marked as backlog item.

## Color Compliance Fixes

All instances of `#475569` (Tailwind slate-600, NOT in the locked 8+3 color system) were replaced with correct system colors in brand mark SVGs:

| Element | Old Color | New Color | System Role |
|---------|-----------|-----------|-------------|
| Document outline/border (combined icon) | `#475569` | `#27272A` | border |
| Document content lines (combined icon) | `#475569` | `#27272A` | border |
| Document content lines (tablet icon) | `#475569` | `#71717A` | text-muted |
| Inactive/destination state circle | `#475569` | `#27272A` | border |
| Color key label | "Slate #475569" | "Border #27272A" | — |
| Badge backgrounds (visual-language-exploration.html) | `#475569` | `#27272A` | border |

**Files affected:**
- `brand/brand-spec.html` §1.3 (brand marks), §1.4 (indigo overview card)
- `brand/explorations/visual-language-exploration.html` (brand marks, badges)

## Rationale

### Why these color replacements?

`#475569` is Tailwind's slate-600 — a pre-built utility shade that was never part of Precept's locked 8+3 system. The locked system includes:

**8 authoring shades:**
- Indigo structure: `#4338CA` (semantic), `#6366F1` (grammar)
- Violet states: `#A898F5`
- Cyan events: `#30B8E8`
- Slate data: `#B0BEC5` (names), `#9AA8B5` (types), `#84929F` (values)
- Gold messages: `#FBBF24`

**3 runtime verdict colors:**
- Enabled: `#34D399` (emerald)
- Blocked: `#F87171` (coral)
- Warning: `#FDE047` (yellow)

**Plus structural/UI neutrals:**
- Background: `#09090B` (bg)
- Text: `#E5E5E5` (text), `#A1A1AA` (text-secondary), `#71717A` (text-muted)
- Border: `#27272A` (border)

The brand marks were using a color outside this system. The replacements bring the marks into compliance:

- **`#27272A` (border)** is the correct choice for structural elements like document outlines, inactive state circles, and borders. It's the locked neutral for "this is a container, not content."
- **`#71717A` (text-muted)** is appropriate for secondary visual elements like content lines in the tablet icon — they're not primary structure, but they're not invisible either.

### Why lock the surface specs now?

1. **Implementation exists.** The syntax editor, state diagram, and inspector panel are all implemented. The specs describe current behavior and establish the contract for future changes.
2. **No guesswork left.** Every color hex, every typography rule, every accessibility requirement is now explicit. Kramer doesn't have to infer intent; Peterman doesn't have to reverse-engineer decisions.
3. **Brand compliance gate.** With locked specs, any UI artifact can be reviewed against its corresponding section. If a surface exists, it has a locked spec. If it doesn't have a spec, it shouldn't be implemented yet.
4. **AI-first by design.** Each spec includes an "AI-first note" explaining how the design serves both human and AI consumers. This is fundamental to Precept's positioning — the surfaces aren't just for developers, they're for AI agents authoring, inspecting, and reasoning about precepts.

## Impact

- **For Kramer:** Implementation contract. Every surface has explicit color/typography/accessibility rules.
- **For Peterman:** Brand compliance reference. Surface-specific application of brand decisions.
- **For Shane:** Decision record. Future changes require spec updates, not just code changes.
- **For AI agents:** Structured design knowledge. Each surface spec includes AI consumption notes.

## Follow-Up Work

1. **CLI color audit** — Verify current CLI tools (dotnet build, language server diagnostics, precept CLI) align with §2.5 spec
2. **Accessibility audit** — Formal color-blind simulation and screen reader testing on all surfaces
3. **Light theme exploration** — If/when light theme support is planned, color recalibration (especially verdict colors) will be required
4. **Inspector panel brand alignment** — Review Kramer's implementation against §2.3 spec; recommend CSS-only color remapping if drift exists

## Related Files

- `brand/brand-spec.html` — Canonical source of truth, §2.3–2.5 now locked
- `brand/visual-surfaces-draft.html` — Draft file now superseded; can be archived or deleted
- `.squad/agents/elaine/history.md` — Session 3 entry documents this work

---

**Locked by:** Shane (via Elaine)
**Locked on:** 2026-04-04

---

---

# Decision: brand-spec §1.4 Palette Structure Refactor

**Filed by:** Frank (Lead/Architect)
**Date:** 2026-04-07
**Type:** Information Architecture
**Affects:** brand-spec.html §1.4, §1.4.1, §2.1
**Full review:** `brand/references/brand-spec-palette-structure-review-frank.md`

---

## Problem

§1.4 Color System contains two distinct palettes at different abstraction levels:

1. **Brand palette card** ("Precept Color System · 8 + 3") — foundational UI tokens: brand, text, structural, semantic. General-purpose.
2. **Syntax family cards** (Structure · Indigo through Constraint Signaling) — editor-specific token-to-color mappings with typography rules and keyword lists.

The section intro says "8 authoring-time shades" but the brand palette card has a different set of 8 (UI tokens). The actual 8 syntax shades are in the family cards. This "two different eights" collision confuses readers. The constraint signaling table is duplicated between §1.4 and §2.1.

## Recommendation

**Move syntax family cards from §1.4 to §2.1.** Replace them in §1.4 with a brief Semantic Family Reference table (one row per family, conceptual identity only — no hex/typography/keyword detail). Update the §1.4 intro to describe brand tokens + semantic families, not syntax shades.

**Result:**
- §1.4 = brand token definitions + semantic family identities (what colors *mean*)
- §1.4.1 = cross-cutting usage roles matrix (unchanged, minor trim of duplicate rationale)
- §2.1 = complete syntax color reference (what colors *do* in the editor)

No content is lost. No locked values change. The restructure separates identity from implementation, matching the rest of the document's §1 (identity) → §2 (surfaces) architecture.

## Owner

J. Peterman (brand-spec maintainer) with Elaine review.

## Status

PENDING — awaiting Shane sign-off.

---

---

# Decision: README Restructure Proposal — Architectural Approval

**Filed by:** Frank (Lead/Architect)
**Date:** 2026-04-06
**Status:** APPROVED WITH REQUIRED CHANGES — Pending Shane sign-off
**Proposal:** `brand/references/readme-restructure-proposal.md`
**Full review:** `brand/references/readme-restructure-review-frank.md`

---

## Decision

The README restructure proposal (Peterman, 2026-04-05) is architecturally approved with four required changes. The narrative architecture — prove → trial → differentiate → reference — is correct for a category-creation README. No implementation begins until the required changes are addressed and Shane signs off.

## Required Changes Before Rewrite

1. **RC-1: Fix C# API call chain** — The hero C# block spec lists the wrong API sequence. Must use `PreceptParser.Parse → PreceptCompiler.Compile → engine.CreateInstance → engine.Fire` per `docs/RuntimeApiDesign.md`.
2. **RC-2: Getting Started step 3 title inconsistency** — Two different titles used in the same proposal. Pick one.
3. **RC-3: Rename "Language Server and Preview"** — Implementation label, not user-facing. Use a capability-describing heading (e.g., "Live Editor Experience").
4. **RC-4: Remove "without human intervention" claim** — Overstates current AI tooling capability. Replace with factual statement about structured tool APIs.

## Gate Enforcement

Per the design gate protocol: no README rewrite implementation starts until Shane explicitly approves the revised proposal. Frank's architectural approval alone is not sufficient.

## Impact

- **No code changes.** This is a documentation restructure.
- **Downstream:** Once the rewrite ships, the README becomes the new source-of-truth narrative for the project. All brand, tooling, and feature claims must be verifiable from the codebase.
- **MCP/AI surface:** The restructured README improves AI agent comprehension via semantic headings, language-tagged code blocks, and structured bullet lists. This aligns with the AI-first design principle.

---

## Frank — Architecture Review: Elaine's 5-Surface UX Spec
**Date:** 2026-04-04
**Status:** Needs revision before implementation

---

### Surface-by-surface notes

#### Surface 1: Syntax Editor

Architecturally sound. The spec accurately describes what the language server delivers: semantic tokens with `preceptConstrained` modifier for constrained states/events/fields, 8-shade authoring palette, hover, diagnostics. The `preceptConstrained` modifier is implemented and emitted in `PreceptSemanticTokensHandler.cs` — the italic rendering Elaine describes is real, not aspirational.

One precision gap: "inline constraint indicators" is ambiguous. The language server produces diagnostic underlines and semantic token modifiers, not a separate inline indicator layer. If the spec means those two mechanisms, it's correct. If it implies a third display channel, that doesn't exist. Clarify in the final spec.

The AI-first note (syntax highlighting as machine-readable metadata) is architecturally correct and reflects how MCP tools consume semantic token data.

**Verdict: Approved with notation.**

---

#### Surface 2: State Diagram

**Protocol gap — blocker.**

`PreceptPreviewSnapshot` (the protocol record the webview receives) exposes `States` as `IReadOnlyList<string>` — a flat list with no role metadata. `PreceptEngine.InitialState` exists but is NOT included in the snapshot sent to the webview. The spec calls for initial states to be distinguished by shape and color. The diagram renderer cannot determine which state is initial from the snapshot alone. It would have to guess from the state name or run a secondary query — neither is acceptable.

What works: `PreceptPreviewTransition` includes `From`, `To`, `Event`, `GuardExpression`, and `Kind` — everything the spec needs for edge labeling, guard display, and event sub-shading is there. Terminal states can be derived by finding states with no outgoing transitions in the `Transitions` list.

Fix required before implementation: add `InitialState` to `PreceptPreviewSnapshot`. This is a one-field protocol addition, not a runtime change.

**Verdict: Needs one protocol fix before implementation can start.**

---

#### Surface 3: Inspector Panel

Mostly aligned. The protocol is implemented and the data model covers the core of Elaine's spec:
- Field names, types, values, current state → covered by `PreceptPreviewSnapshot.Data` + `PreceptPreviewEditableField`
- Constraint violations on fields → `PreceptPreviewEditableField.Violation`
- Event argument contracts → `PreceptPreviewEventArg` (name, type, nullable, default)
- Enabled/blocked event status → `PreceptPreviewEventStatus.Outcome`

Two architectural gaps worth flagging:

**Gap 1 — Field change delta.** The spec says "before and after state, with visual indication of which fields changed." The protocol delivers a complete replacement snapshot — no delta, no `PreviousData` field. To show which fields changed, the webview must cache the pre-fire snapshot and diff it client-side. This works but it's client logic, not protocol-supported. It's a warning, not a blocker, because client-side diffing is reasonable here.

**Gap 2 — Violation structure loss.** `PreceptPreviewEditableField.Violation` is a `string?` — a single flattened message. The underlying `ConstraintViolation` type carries structured targets (which fields/args/events are implicated). That structure is lost at the protocol boundary. For simple coloring, a string is enough. If the UI ever needs to cross-highlight multiple implicated fields from a single constraint, the protocol can't support it without a breaking change. Flag for future consideration.

**Gap 3 — Read-only field rendering.** The spec describes "read-only field indicators" as a separate visual affordance. In the current model, editability is implied by presence in `EditableFields` — non-editable fields appear only in `Data`. The webview must infer read-only status by set subtraction. This is workable but should be explicit in the webview implementation spec.

**Verdict: Approved with warnings. Note gaps before implementation.**

---

#### Surface 4: Docs Surface

**Terminology error — must be corrected.**

Elaine has designed a public-facing documentation *website* — "responsive layout," "mobile (stacked layout)," "docs site" — complete with a future delivery timeline. That is not what this surface is. In this project, "docs" means `docs/*.md` — internal design and architecture documents. There is no documentation website planned, no docs pipeline, no docs tooling.

Elaine herself flags this with "scope clarification needed," so she knows something is off. The answer is: this is not a product surface. There is no docs site architecture to review. The spec section should be retitled "Internal Docs" or struck from the visual surfaces document entirely. If Shane ever greenlights a public docs site, that gets its own design gate from scratch.

The AI-first note in this section (structured HTML, code blocks in `<pre>`, heading hierarchy for agent parsing) is well-reasoned for a docs site that *did* exist. File it away for when that surface is actually in scope.

**Verdict: Not a current product surface. Retitle or remove from this spec.**

---

#### Surface 5: CLI / Terminal

**Scope error — no such surface exists.**

The spec describes a "precept CLI" with `--json` output flags, custom colored diagnostic output, and build logging with brand verdict colors. None of this exists. There is no standalone `precept` CLI tool. The runtime is a library. The language server is an in-process VS Code extension host — it sends diagnostics to VS Code's Problems panel, not to a terminal stream. `dotnet build` output uses MSBuild formatting, not custom brand colors.

The fictional `--json` flag in the AI-first note is a concrete error — it implies an interface that doesn't exist and could mislead implementation agents.

What *is* real and terminal-visible: `dotnet build` output (standard MSBuild, not brand-colored), `dotnet test` output (standard xUnit), and MCP tool responses (JSON over stdio, invisible to human terminal users).

If a `precept` CLI tool is ever designed, that requires a full architectural spec and a new runtime interface — it cannot be assumed from the current engine API.

**Verdict: Reject this surface as currently specified. Either (a) remove it, or (b) retitle it "Diagnostic Output / VS Code Problems Panel" and redesign around what actually exists.**

---

### Blockers

1. **`PreceptPreviewSnapshot` missing `InitialState`** — State diagram cannot distinguish the initial state without this field. Protocol change required before any diagram rendering implementation begins. This is a one-field addition to `PreceptPreviewProtocol.cs` and `PreceptPreviewHandler.cs`.

2. **CLI surface describes a non-existent product surface** — The "precept CLI" tool does not exist. Any agent implementing against this spec would be building dead code or making false assumptions about the runtime API. Must be resolved (remove or replace) before implementation.

3. **Docs surface is misclassified** — Describing an aspirational public docs site as a current visual surface conflates product aspiration with design contract. Must be corrected before this spec is integrated into `brand-spec.html`.

---

### Warnings

1. **Inspector: no field change delta in protocol** — "Visual indication of which fields changed" requires client-side diffing. Acceptable, but the webview implementation spec needs to make this explicit so Kramer doesn't expect a protocol delta.

2. **Inspector: `Violation` is string, not structured** — `PreceptPreviewEditableField.Violation` stringifies what the engine produces as a structured `ConstraintViolation`. This loses multi-target attribution. Fine for now, but any future "cross-highlight implicated fields" feature requires a protocol change.

3. **Inspector: read-only field rendering requires set subtraction** — Non-editable fields live in `Data`, editable fields live in `EditableFields`. The webview computes read-only status by exclusion. Not a protocol gap, but the UI implementation must understand this model explicitly.

4. **Editor: "inline constraint indicators" is ambiguous** — Could mean diagnostics (squiggles), semantic token modifiers (color/italic), or a third thing. Clarify before implementation so Kramer builds the right affordance.

5. **Docs AI-first note is orphaned but valuable** — The structured content notes (heading hierarchy, `<pre>` blocks, definition lists for AI parsing) are worth keeping somewhere. When a docs site is ever scoped, this belongs in that spec.

---

### Overall verdict

The inspector panel and editor surface specs are solid and grounded in the real runtime; they need minor clarifications, not redesigns. The state diagram spec has a concrete protocol gap (missing `InitialState` in the snapshot) that must be fixed before implementation starts. The CLI and docs surfaces are both misaligned with what the product actually ships — one is fictional, one is mislabeled — and must be corrected before this spec is merged into `brand-spec.html`. Fix the blockers, then implementation can proceed on editor, inspector, and diagram.

---

## George — InitialState Protocol Fix
**Date:** 2026-04-04
**Status:** Complete

---

### Problem

Frank's architecture review of Elaine's 5-surface UX spec identified a **blocking protocol gap** preventing state diagram implementation:

> `PreceptPreviewSnapshot` (the protocol record the webview receives) exposes `States` as `IReadOnlyList<string>` — a flat list with no role metadata. `PreceptEngine.InitialState` exists but is NOT included in the snapshot sent to the webview. The spec calls for initial states to be distinguished by shape and color. The diagram renderer cannot determine which state is initial from the snapshot alone. It would have to guess from the state name or run a secondary query — neither is acceptable.

(Source: `.squad/decisions/inbox/frank-surfaces-review.md` § Surface 2)

### Solution

Added `InitialState` property to `PreceptPreviewSnapshot` and populated it from the existing `PreceptEngine.InitialState` field.

**Files changed:**
1. `tools\Precept.LanguageServer\PreceptPreviewProtocol.cs` (line 33) — added `string InitialState` positional parameter after `CurrentState`
2. `tools\Precept.LanguageServer\PreceptPreviewHandler.cs` (line 296) — populated `session.Engine.InitialState` in `BuildSnapshot()`

**Build/test status:**
- Language server build: ✓ succeeded (6.6s)
- Language server tests: ✓ 84/84 passed (1.6s)
- No regressions

### Downstream Impact

**Immediate consumers:**
- **Inspector webview** (`tools\Precept.VsCode\webview\inspector-preview.html`): Already reads `snapshot.CurrentState` at line 1387. `InitialState` now available but not yet consumed. No changes needed to existing UI — field is additive.

**Blocked work now unblocked:**
- **Kramer (Language Server / Webview Dev)**: State diagram renderer can now be implemented per Elaine's visual spec. `InitialState` provides the metadata needed to distinguish the initial state with a double-circle border and contrasting color as the spec requires.

**Not affected:**
- **Newman's MCP tools**: MCP tools in `tools\Precept.Mcp\Tools\` do not reference `PreviewSnapshot` — they use core engine types directly. No changes needed.

### Design Gate

This change was approved as part of Frank's architectural review and Shane's approval of the fix plan. It is a **one-field additive protocol change** with no impact on:
- DSL syntax or semantics
- Parser, tokenizer, type checker
- Runtime execution engine
- Constraint evaluation

The underlying `PreceptEngine.InitialState` property already existed and was already populated during compilation from `model.InitialState.Name`. This fix exposes existing data to consumers — no new logic, no new semantics.

Per charter, this qualifies as a non-design-gated fix because it's a small protocol addition explicitly called out in an approved architectural review.

### Notes

- Protocol is positional record type — downstream consumers using named construction will not break (C# record semantics).
- JSON serialization is automatic (OmniSharp LSP infrastructure) — `InitialState` will appear in webview responses immediately.
- No documentation update needed — preview protocol types in `PreceptPreviewProtocol.cs` serve as implementation documentation. (Design docs like `EditableFieldsDesign.md` and `RulesDesign.md` reference the protocol in context but don't document the full structure.)

---

---

# Runtime Review: README Restructure Proposal — Three Required Changes

**Filed by:** George (Runtime Dev)
**Date:** 2026-04-06
**Type:** Required changes — gates on rewrite begin
**Full review:** `brand/references/readme-restructure-review-george.md`
**Proposal:** `brand/references/readme-restructure-proposal.md`
**Frank's review:** `brand/references/readme-restructure-review-frank.md`

---

## Status

CONDITIONALLY APPROVED — same verdict as Frank's review, plus three required changes from the runtime domain.

---

## Decision Summary

Frank's RC-1 fix direction is correct but contains a factual error that must be resolved before the copy brief is issued. Two additional required changes are new findings from runtime and documentation review.

---

## G1: Remove `RestoreInstance` — It Doesn't Exist

**Corrects:** Frank's RC-1 addendum
**Severity:** Must fix before copy brief is issued

Frank's RC-1 fix reads: "Include `RestoreInstance` as an alternative to `CreateInstance`." This is wrong. `RestoreInstance` is not a real method. It does not exist in `PreceptEngine` or anywhere in the public API.

Verified against `src/Precept/Dsl/PreceptRuntime.cs`: the only relevant methods are two overloads of `CreateInstance`:
- `engine.CreateInstance()` — new entity at InitialState
- `engine.CreateInstance(string state, IDictionary<string, object?> data)` — restore from database

The correct restore pattern is `engine.CreateInstance(savedState, savedData)`. There is no `RestoreInstance`.

**Required action:** Remove `(or RestoreInstance)` from the proposal's C# Block Specification. Update Frank's RC-1 fix guidance to reflect that the second overload of `CreateInstance` is the restore pattern. Any copy brief that propagates `RestoreInstance` will produce a README with a nonexistent API call.

---

## G2: Add .NET SDK Prerequisite to Getting Started

**New finding**
**Severity:** Must fix before rewrite — first-run correctness gap

The VS Code language server is a .NET 10 process. A developer who installs the VS Code extension without .NET installed gets no language features — no diagnostics, no completions, no hover. No error message. Silent failure.

The proposal's Getting Started section drops the Prerequisites section that exists in the current README ("Prerequisites: .NET 10 SDK — required for both the language server and MCP tools").

Steinbrenner's research requires a working first-run path. A developer who follows Getting Started Step 1 without .NET installed cannot get to a working state. The prerequisite must be restored.

**Required action:** Add a .NET 10 SDK prerequisite before or within Getting Started Step 1. One sentence is sufficient: "The language server requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) — install it first if you don't have it."

---

## G3: Soften "Replacing Three Separate Libraries" in the Hook

**New finding**
**Severity:** Required before rewrite — adoption expectation risk

The proposed hook reads: "Precept unifies state machines, validation, and business rules into a single DSL — **replacing three separate libraries with one contract**."

"Replacing" implies a drop-in substitution path. Precept requires adopting a new execution model — entities are governed entirely by the DSL contract. Developers who read "replacing" will attempt to swap out Stateless/FluentValidation/NRules piecemeal and find it doesn't work that way. Churn follows.

The brand voice requirement is "declarative, no hedging, no overclaiming." "Replacing" overclaims substitutability.

**Required action:** Change "replacing three separate libraries with one contract" to "eliminating the fragmentation that comes from managing them separately" or similar. Retains category-creation force without implying a drop-in path.

---

## Endorsements

Frank's RC-2 (title inconsistency in Step 3), RC-3 (rename Section 4c heading), and RC-4 (soften AI autonomous authoring claim) are all correct. No changes from me on those.

Frank's RC-4 fix suggestion — "AI agents can validate, inspect, and iterate on `.precept` files through structured tool APIs" — is accurate and should be adopted.

---

*George*

---

## George — Technical Review: Elaine's 5-Surface UX Spec
**Date:** 2026-04-04
**Status:** Accurate with notes

---

### Surface-by-surface notes

#### Surface 1: Syntax Editor
Mostly accurate. The 8-shade palette description matches the locked system. Bold/italic typography signal (bold = structure semantic, italic = constrained actors) is correctly described and is what the semantic tokens handler implements.

**One factual error:** She cites the diagnostic code range as **PRECEPT001–PRECEPT047**. The actual range is **PRECEPT001–PRECEPT054** (C1–C54 — 54 constraints, not 47). This appears in her `history.md` surfaces inventory section (under the VS Code Extension heading) and should be corrected.

Everything else under this surface — semantic tokens, hover, completions, go-to-definition, italic constraint signaling — is consistent with what the language server and runtime actually provide.

---

#### Surface 2: State Diagram
The lifecycle model (initial / intermediate / terminal) is technically accurate as a conceptual description. **Important nuance:** "terminal" is not a first-class DSL keyword. Terminal states are inferred at analysis time by the analysis pass (`PreceptAnalysis.cs`) — states with no outgoing transitions to other states. The DSL grammar only has `state <Name> initial` for the initial marker; "terminal" is a computed property. Elaine's diagram description won't mislead a designer, but the implementation team should know "terminal" isn't declared — it's detected.

The event sub-categories (transition / conditional / stationary) used in the diagram sub-shading are accurate:
- Transition = always moves state
- Conditional = guarded row, uncertain
- Stationary = no state change (AcceptedInPlace)

Color and shape redundancy claims are accurate to the locked brand decisions.

---

#### Surface 3: Inspector Panel
**Answer to open question:** YES, the inspector is fully implemented. `tools/Precept.VsCode/webview/inspector-preview.html` is a complete, working webview. Elaine's brand drift finding is correct (Segoe UI, custom colors — see her `inspector-panel-review.md`).

**What the inspector actually shows:**
- State diagram (SVG) with current state highlighted
- Event list for current state with fire forms and event argument inputs
- Field data list with values
- Inline field edit mode (not a modal — fields are editable directly in the data list with constraint violation messages inline)
- Nullable argument toggle (null button per arg, already implemented)

**Technical inaccuracy — event outcomes:** Elaine describes "Warning (#FDE047) for unmatched guards" as one of the inspector's three verdict states. This is wrong for the inspector surface specifically. The inspector event bar has **four** states:
1. `enabled` → green (#1FFF7A) — event will fire and change state
2. `noTransition` → green/dimmed (#1FFF7A at 72% opacity) — event fires in-place, no state change (AcceptedInPlace outcome)
3. `blocked` → red (#FF2A57) — rejected
4. `undefined` → red/dimmed (#FF2A57 at 72% opacity) — no row exists for this event from current state

The `notApplicable` outcome (all guards fail) is **filtered out** of the event bar entirely — it does not render at all, let alone as yellow. There is currently no yellow/warning state in the inspector event list. If the brand spec requires a yellow "NotApplicable" state in the inspector, that's a new implementation requirement — not current behavior.

**"Before and after" framing:** Her spec says the inspector shows "before and after state, with visual indication of which fields changed." This is slightly aspirational. The inspector shows the current state (after firing), a toast confirmation message, and updated field values — but there's no explicit side-by-side before/after comparison or field diff highlighting. Fields update in place.

**"Computed, state-derived" read-only fields:** The DSL doesn't have computed or state-derived field types. All fields are declared scalar or collection fields. What controls editability is `in State edit Field` — fields not declared editable in the current state are locked in the inspector. The "read-only" concept is real, but the framing as "computed/state-derived" is inaccurate — it's editability-per-state, not field type.

---

#### Surface 4: Docs Site
**Naming issue:** This surface is called "Docs Site" throughout, implying a public documentation website. We don't have one. The `docs/` folder is internal design documents and architecture records — not a public-facing site. Whether a docs site is planned is a Shane-level question, but the current naming treats an aspirational future artifact as a live surface. If this goes into `brand-spec.html`, it should be marked clearly as a future/planned surface, not a present one.

The visual guidelines Elaine wrote for this surface (syntax highlighting consistency in embedded code samples, indigo navigation accents, responsive layout) are sound if/when a site is built — no technical objections to the design principles.

---

#### Surface 5: CLI / Terminal
**Significant issue:** There is no standalone `precept` command-line tool. Elaine writes: "Developers see this surface when running `dotnet build`, invoking the language server, or checking `precept` command-line tools." That last phrase implies a dedicated `precept` CLI exists — it doesn't. The diagnostic toolchain is:

- `dotnet build` → standard MSBuild output (no PRECEPT-coded messages in the build output)
- Language server → surfaces PRECEPT diagnostics in VS Code's Problems panel (not the terminal)
- MCP server (`precept_compile`, etc.) → JSON API, not terminal output

PRECEPT diagnostic codes (PRECEPT001–PRECEPT054) are currently only surfaced in the VS Code Problems panel via the language server. They don't appear in `dotnet build` output. The CLI surface as Elaine describes it — with structured error formatting, `✗ ERROR: PRECEPT042` lines, and verdict colors — describes a tool that doesn't exist yet.

The color principles she defines (verdict colors + symbol redundancy, no structural indigo in terminals, default terminal color for file paths) are all technically sound as a design contract for a future CLI tool. But the spec should be honest that this surface is aspirational, not current.

---

### Corrections needed

1. **Diagnostic code range:** `PRECEPT001–PRECEPT047` → `PRECEPT001–PRECEPT054`. 54 constraints registered (C1–C54).

2. **Inspector — event outcome states:** The spec says three verdict states: enabled / blocked / warning (for unmatched guards). In the inspector, `notApplicable` is filtered out of the UI, not shown as yellow warning. The actual four states are: `enabled` (green), `noTransition` (green dimmed), `blocked` (red), `undefined` (red dimmed). Yellow/warning is not currently used in the event list. If yellow for NotApplicable is a brand requirement, it needs to be added as a new implementation ticket for Kramer.

3. **Inspector — "before and after" / field diff:** No explicit before/after comparison view exists. The inspector fires and shows the resulting state with updated values. "Visual indication of which fields changed" is aspirational.

4. **Inspector — "computed, state-derived" fields:** Wrong framing. Read-only fields in the inspector are fields not declared editable in the current state via `in State edit Field`. There are no computed or state-derived field types in the DSL.

5. **CLI — no `precept` command-line tool exists.** The surface as described is a future design artifact, not current implementation.

6. **Docs Site — naming:** "Docs Site" implies a public website. `docs/` is internal team artifacts. This surface should be explicitly labeled as future/planned if it stays in the spec.

---

### Open questions for Elaine

1. **Inspector yellow/warning:** Do you want to add a yellow "NotApplicable" state to the inspector event list? Right now that outcome is hidden. This would require an implementation change (Kramer's domain) — want to add it as a requirement?

2. **Docs Site scope:** Shane was already asked about this. Once he answers — is this a future public site, or internal docs only? — the surface section should be updated or labeled accordingly.

3. **CLI surface intent:** Is this a design contract for a future CLI tool, or is it meant to describe existing behavior? If future, flag it as aspirational. If someone is planning to build a `precept` CLI, that needs to be on the architecture roadmap.

4. **State diagram — current inspector vs. standalone surface:** The state diagram renders inside the inspector webview (same panel). Are you intending these as one surface or two? The current implementation is a unified preview panel with both diagram and inspector side-by-side. If they become separate surfaces, that's an architectural change.

---

### Overall verdict

The brand and UX design principles in Elaine's spec are sound — the color system, typography, accessibility thinking, and AI-first framing are all grounded correctly. The technical inaccuracies are concentrated in the inspector's event outcome model (no yellow NotApplicable state), the diagnostic code range (47 vs 54), and the CLI surface (which describes a tool that doesn't exist yet). None of these are blocking for brand spec integration, but the CLI section especially needs a disclaimer so it doesn't get mistaken for a description of current behavior.

---

---

# Brand-Spec Color Information Architecture — Implementation Complete

**Author:** J. Peterman
**Date:** 2026-04-07
**Status:** Implemented — pending Shane visual review
**Scope:** `brand/brand-spec.html` — §1.4, §1.4.1, §2.1, §2.2

---

## What Was Done

Implemented the approved color information architecture restructure. All changes are information architecture — no locked color values were changed.

### §1.4 Color System — Identity Palette Only

- **Rewrote intro paragraph** to clarify the two-layer system (brand palette here + syntax highlighting palette in §2.1).
- **Removed** all 7 per-category syntax token cards (Structure, States, Events, Data, Rules, Comments, Verdicts) — moved to §2.1.
- **Removed** the constraint signaling table — consolidated into §2.1 (where a functionally identical table already existed).
- **Kept** the 8+3 brand palette card, both callouts (Locked design, Semantic color as brand), and the hue map.
- **Updated** the hue map footer text to add a forward reference to §2.1.
- **Added** a new "Semantic family reference" table — 6 rows (Structure, States, Events, Data, Rules, Verdicts), identity-level only (hue assignment, meaning, surfaces). Forward references to §2.1 and §2.2 for implementation detail.

### §1.4.1 Cross-Surface Color Application — Renamed and Narrowed

- **Renamed** from "Color Usage" to "Cross-Surface Color Application."
- **Updated intro** to add forward references to §2.1 and §2.2.
- **Fixed** the `brand-light` row: removed the incorrect claim that brand-light is used for "state names in diagrams and syntax." State names use Violet `#A898F5`. Brand-light `#818CF8` is an accent color. (This was an existing discrepancy flagged by both Frank and Elaine.)
- Kept: Color Roles table, README & Markdown Application table, Color Usage Q&A, README color contract callout.

### §2.1 Syntax Editor — Self-Contained Syntax Color Reference

- **Updated Color application text** — now references §1.4 as the source of semantic family identities, with forward reference language rather than generic palette pointer.
- **Split the main card** into two: (1) Purpose + Color application; (2) Live example + Constraint signaling + Diagnostics.
- **Moved in** all 7 syntax token cards from §1.4, placed between the two cards.
- **Moved in** the hue map callout from §1.4 (now in §2.1 for syntax implementers).
- **Renamed** "Constraint-aware highlighting" → "Constraint signaling" for consistency with Frank's terminology.
- **Updated** the Constraint signaling intro text (minor rewrite for clarity).
- **Added** "Live example" h3 heading before the syntax block.
- **Updated** the Rules card to note explicitly that gold is syntax-only (deduplication: consolidating the gold restriction to the 8+3 card note and the §2.1 Rules card).

### §2.2 State Diagram — Diagram Color Mapping Added

- **Updated Color application paragraph** to reference §1.4 semantic families and distinguish the diagram surface from §2.1 (shape/edge styling vs. typography).
- **Fixed SVG legend** blocked line color: `#f43f5e` → `#FB7185` (aligning to brand palette source of truth, per Frank's hex discrepancy finding).
- **Added "Diagram color mapping" h3 subsection** with:
  - Static elements table (12 rows): canvas, borders by node type, fills, state name text, transition edges, event labels, guard annotations, legend text.
  - Runtime verdict overlay section with verdict overlay table (7 rows): current state node, enabled/blocked/warning edges, muted edges, enabled/blocked labels.
  - Open decision callout for current state indicator (Option A vs Option B — pending Shane sign-off).
  - Semantic signals callout (compile-time static analysis treatments: constrained italic, orphaned opacity, self-loop edges).

---

## Open Decisions Documented (Not Resolved)

Per the instruction to document open decisions rather than invent values:

**Current state indicator visual treatment** — documented as an open decision in §2.2 with two proposals:
- Option A (Elaine): `#1e1b4b` fill tint at 20–30% opacity
- Option B (Frank): `#4338CA` fill tint at 10–15% opacity

Both use a fill tint. The specific shade needs Shane sign-off before implementation.

---

## Hex Discrepancy Note

The SVG legend blocked line was fixed from `#f43f5e` to `#FB7185` (brand palette source of truth). The §2.3 Inspector Panel still uses `#F87171` for "Constraint violation indicators" — this pre-existing discrepancy is outside the scope of this restructure but should be addressed when §2.3 is next updated.

---

## What Was NOT Changed

- No locked color values changed
- No brand decisions altered
- §2.3, §2.4, §2.5 untouched (their §1.4 cross-references still resolve correctly)
- All existing content preserved — only reorganized

---

## Decision Required

**Shane visual review:** The restructure is functionally complete. Shane's eye is the final gate before this is considered locked.

Key things to review:
1. §1.4 — does the semantic family reference table give enough orientation without the full token cards?
2. §2.1 — does the layout (purpose card → 7 token cards → hue map → live example card) read well?
3. §2.2 — does the diagram color mapping section work as a reference? Open decision on current state indicator.

---

---

# Decision: Brand-Spec § 1.4 Palette Structure Reorganization
**Filed by:** J. Peterman
**Date:** 2026-04-06
**Status:** RECOMMENDATION — pending Shane sign-off
**Reference:** `brand/references/brand-spec-palette-structure-review-peterman.md`

---

## Summary

Section 1.4 of `brand-spec.html` currently contains two distinct, unrelated color systems with no structural separation. Section 1.4.1 then re-lists the same brand palette a second time, creating redundancy. A constraint signaling table appears verbatim in both § 1.4 and § 2.1.

Three issues:
1. **Two palettes in one section** — brand palette (`pc-palette` card) and syntax-highlighting palette (per-category token cards) are stacked together in § 1.4 with no seam
2. **Conflicting "8+3" nomenclature** — the intro paragraph and the palette card both say "8+3" but count different colors (syntax tokens vs. brand/UI colors)
3. **§ 1.4.1 re-lists the same colors** — the Color Roles table duplicates the palette card's role names; the constraint signaling table appears verbatim in both § 1.4 and § 2.1

---

## Recommended Action

**Move:** Per-category syntax cards (Structure, States, Events, Data, Rules, Comments, Verdicts) and constraint signaling table from § 1.4 → § 2.1 (Syntax Editor), where they belong conceptually.

**Trim:** The "Color Roles" table in § 1.4.1 — eliminate the color identity columns already in the `pc-palette` card above. Keep only the "Specific Uses" column, or fold usage notes into the palette card itself.

**Rename:** § 1.4.1 "Color Usage" → "Cross-Surface Color Application" (more precise).

**Clarify:** § 1.4 intro paragraph — rewrite to explicitly acknowledge the two-layer system (brand palette here, syntax palette in § 2.1).

**No color values change. No locked decisions change.** This is reorganization only.

---

## Who Needs to Review

- **Shane** — final sign-off before edits
- **George / Kramer** — FYI that syntax token palette docs will move to § 2.1 (closer to their implementation reference)

---

## Files Affected (when approved)

- `brand/brand-spec.html` — structural reorganization of § 1.4, § 1.4.1, § 2.1

---

---

# J. Peterman — Final Brand-Spec Cleanup Pass

**Date:** 2026-04-04
**Scope:** `brand/brand-spec.html` final palette consistency cleanup

## Decision

Treat the §1.4 palette card as the color source of truth and normalize all remaining downstream references to it.

## Applied in this pass

- Legacy verdict refs normalized to locked values:
  - Blocked/Error → `#FB7185`
  - Warning → `#FCD34D`
- Related palette drift tied to the same feedback thread also normalized:
  - Background → `#0c0c0f`
  - Gold syntax accent → `#FBBF24`
- Later-surface prose now uses the locked verdict language consistently (`emerald / rose / amber` where naming matters)

## Not changed

- No redesign
- No new color decisions
- No resolution invented for the §2.2 current-state indicator

## Remaining blocker

One item still requires Shane sign-off before the brand spec can be considered fully closed: the current-state indicator treatment in `brand/brand-spec.html` §2.2.

---

---

# Decision: README Restructure Proposal Filed

**Filed by:** J. Peterman
**Date:** 2026-04-05
**Status:** PROPOSED — awaiting Shane review
**Artifact:** `brand/references/readme-restructure-proposal.md`

---

## Summary

A README restructure proposal has been filed synthesizing three research passes:

- **Peterman** — brand/copy conventions from 13 comparable library READMEs
- **Steinbrenner** — developer evaluation journey and adoption patterns
- **Elaine** — UX/IA review with explicit hard constraints

## Key Structural Decisions

1. **Section order locked:** Title → Hook → Quick Example → Getting Started → What Makes Precept Different → Learn More → License
2. **Hero treatment:** 18-20 line DSL block (≤60 chars/line) + separate 5-line C# execution block; both with language tags; business domain only
3. **CTA hierarchy:** Primary = VS Code extension; Secondary = NuGet; Tertiary = Copilot plugin (deferred to differentiation section — removes 3-way decision paralysis from current README)
4. **Sample catalog removed** from main README; linked externally
5. **Time Machine sample** moved to sample catalog; hero uses real business logic domain

## Hard Constraints (Elaine — 16 total)

All 16 constraints from Elaine's UX/IA review are documented as non-negotiables in the proposal. They include: mobile-first viewport, single primary CTA, semantic heading hierarchy, progressive disclosure order, viewport resilience, screen reader compatibility, and AI parseability requirements.

## Open Items Requiring Shane Decision

- **Hero sample domain:** Order vs. Subscription Billing vs. LoanApplication (structural spec is locked; domain is Shane's call)
- **Brand mark form** in title block (SVG at 48px for mobile-first)
- **Docs site links** — all "link" placeholders require real URLs before README ships
- **Palette usage roles** — Elaine's palette/usage pass still in flight; no color decisions in README rewrite should anticipate that pass

## What This Does NOT Change

- No edits to `README.md` have been made
- Brand decisions remain locked (`brand/brand-decisions.md`)
- Positioning language is locked: "domain integrity engine for .NET"

---

---

# Decision: README Badge Cleanup + Sample Catalog Count

**Author:** Kramer
**Date:** 2026-04-05
**Requested by:** Shane

---

## Decisions Made

### 1. Remove Build Status badge
**Decision:** Removed the Build Status badge entirely.
**Rationale:** No `build.yml` (or any CI build workflow) exists in `.github/workflows/`. The badge used placeholder owner `OwnerName` and would have permanently shown "unknown" status. No CI build pipeline to link to — the badge was misleading noise.

### 2. Remove VS Code Extension marketplace badge
**Decision:** Removed the VS Code Marketplace badge entirely.
**Rationale:** `tools/Precept.VsCode/package.json` has `"publisher": "local"` — a local development placeholder. The extension has not been published to the VS Code Marketplace. The badge would permanently fail to resolve against the marketplace API.

### 3. Fix `AuthorName.precept-vscode` placeholder in Quick Start
**Decision:** Updated to `sfalik.precept-vscode`.
**Rationale:** GitHub remote is `https://github.com/sfalik/Precept.git`, so the owner/publisher is `sfalik`. This is consistent but still provisional until the extension is actually published with a confirmed publisher ID.

### 4. Update sample count 20 → 21 and add `crosswalk-signal.precept`
**Decision:** Updated README sample catalog to include `crosswalk-signal.precept` and corrected count to 21.
**Rationale:** `crosswalk-signal.precept` existed in `samples/` but was absent from the README sample catalog table and all feature coverage matrix rows. The count claim ("20 workflows") was factually wrong.

---

## NuGet Badge — No Change Needed
The NuGet badge (`https://img.shields.io/nuget/v/Precept` → `https://www.nuget.org/packages/Precept`) is correctly structured. The package name `Precept` matches the project name in `src/Precept/Precept.csproj` (no explicit `<PackageId>`, defaults to project name).

---

## No Numeric Catalog/Constructs Count Claim Found
Searched README for claims about number of DSL constructs, catalog items, or language features. No such claim exists.

---

---

# Decision: Restore "Two Surfaces + 3 Brand Marks" to brand-spec.html § 1.3

**Date:** 2026-04-05
**Owner:** J. Peterman (Brand/DevRel)
**Status:** COMPLETED
**File:** `brand/brand-spec.html`

---

## Problem Statement

The brand-spec lost content from explorations: the "two surfaces" (DSL Code + State Diagram side-by-side) and the brand mark size variants (64px, 32px, 16px) were not integrated into § 1.3 (Wordmark & Brand Mark). Shane requested restoration.

## Solution

### Part 1: Brand Mark Size Variants

Expanded the "Brand mark form" card to display three size variants in a horizontal flex layout:

- **Full (64px)** — NuGet, GitHub, VS Code extension icon
  - Existing SVG maintained at full scale (80px display, 64px viewBox)
  - Use case: "NuGet, GitHub, VS Code"

- **Badge (32px)** — sidebar, compact contexts
  - Same SVG scaled to 40px display (64px viewBox)
  - Use case: "Sidebar, compact"

- **Micro (16px)** — favicon, status bar
  - Simplified SVG (32px viewBox) showing only indigo circle + emerald arrow
  - Destination circle removed for legibility at micro scale
  - Use case: "Favicon, status bar"

All three shown with label and use-case note beneath. Color key simplified to four swatches (indigo, emerald, slate, ground) without verbose role descriptions.

### Part 2: "Brand in System" Card

Added a new card immediately after the brand mark form card, showing the two locked DSL surfaces side-by-side:

**Title:** "Brand in system: the two primary surfaces"
**Subtitle:** *"DSL code and state diagram. One palette. Every precept file becomes a brand moment."*

**Layout:** Two-column grid (`grid-template-columns: 1fr 1fr`)

- **Left column:** Surface 1: DSL Code
  - Syntax-colored Precept example (LoanApplication)
  - Uses locked palette: keywords #4338CA, states #A898F5, events #30B8E8, operators #6366F1

- **Right column:** Surface 2: State Diagram
  - SVG state machine diagram (Draft → Submit → UnderReview)
  - Same palette; transition arrow #34D399 emerald, ground #1e1b4b indigo

**Footer text:** Explains the locked palette and how DSL semantics appear in two visual forms.

## Design Rationale

1. **Size variants clarify product placement:**
   - No ambiguity about icon sizing across NuGet badges, GitHub repos, VS Code extensions, and system UI
   - Micro variant's simplified form (no destination circle) is a practical concession to legibility at 16px scale

2. **Two surfaces card reinforces visual language principle:**
   - The same DSL semantics appear in two visual forms
   - One locked palette across both surfaces (keywords, states, events, operators, transitions)
   - Placement after brand mark (not in § 2: Visual Surfaces) keeps § 1 as a complete identity system

3. **Grid layout matches brand-spec style:**
   - Inline CSS grid (not `.side-by-side` class)
   - Consistent with existing card styling: dark background #0c0c0f, border #1e1b4b
   - Each surface panel has its own visual container

## Content Source

The two-surfaces card adapted from `brand/explorations/visual-language-exploration.html` § 3 (lines 2132–2174), originally showing the "combined visual identity system" with code and diagram surfaces locked together.

## Files Changed

- `brand/brand-spec.html` — § 1.3 expanded with size variants and two-surfaces card

## Decision

**Approved:** Both the size variants expansion and the two-surfaces card restore the intended brand-spec narrative: the brand mark is a locked form with three deployment contexts (Full, Badge, Micro), and it lives within a larger visual language where DSL code and diagrams speak the same semantic palette.

---

**Next Steps:** (None — this completes Shane's request.)

---

---

# Decision: Replace README Hero with TimeMachine

**Date:** 2026-07-11
**Author:** J. Peterman (Brand/DevRel)
**Requested by:** Shane
**Status:** Implemented

---

## What Changed

The hero code block in `README.md` (the `.precept` example under "The Aha! Moment") has been replaced.

**Before:** `LoanApplication` — 40+ lines, five fields, multiple states, compound guards on income ratios and credit scores, commented sections. Technically complete. Tonally: a mortgage application.

**After:** `TimeMachine` — 15 lines. One field (`Speed`), one invariant, three states (`Parked → Accelerating ↔ TimeTraveling`), two events, dotted arg access, an 88 mph && 1.21 gigawatts `when` guard, a `reject` with a line that earns its place, and a clean arrival loop back to `Parked`. No comments. No language tag on the fence.

---

## Rationale

The hero example is not documentation. It is an argument. It must answer — in seconds, without narration — *why Precept exists* and *what it feels like to use it*.

`LoanApplication` answered the first question competently. It did not answer the second.

`TimeMachine` answers both. The invariant is self-evident. The guard condition is famous. The reject message — *"Roads? Where we're going, we still need 88 mph and 1.21 gigawatts."* — demonstrates that constraint messages can carry personality without losing precision. The state loop is satisfying. The whole thing reads in under thirty seconds and leaves a developer thinking about what they would build.

This is the correct register for a hero example.

---

## Authoring Choices

- **No comments** — the code block reads cleanly without annotation. The structure is legible.
- **No language tag** on the fence — GitHub renders plain monospace, which is correct. A syntax tag would apply GitHub's generic highlighter, which does not know Precept.
- **15 lines exactly** — within the 10–15 line constraint. Nothing trimmed from the canonical source.

---

## Files Affected

- `README.md` — hero code block and section heading updated

---

---

# Decision: Indigo Overview Card Format in § 1.4

**Date:** 2026-05-01
**Requested by:** Shane
**Agent:** J. Peterman
**Status:** Implemented

## Decision

The indigo color system overview card in `brand/brand-spec.html` § 1.4 now uses the `palette-card` format from `brand/explorations/color-exploration.html`.

## What Changed

The old card used a bespoke `.card` layout with:
- A 48px swatch bar bleeding into the card via negative margins
- An 8-shade ramp below it (also negative-margin bleeding)
- A prose title (`h3`) and description paragraph
- A full color role table (8 rows × 4 columns: swatch, hex, role, usage)
- A two-column grid (NuGet badge left / syntax snippet right)
- A separate state diagram block with its own background and border

The new card mirrors the `palette-card` format exactly:
- `pc-swatch-bar` — 64px tall, solid #6366F1, full bleed at the top
- `pc-swatch-gradient` — 32px flex row, 8 shade divs (#1e1b4b → #c7d2fe)
- `h2` title: "Indigo · 239°"
- `pc-hex`: `#6366F1   rgb(99, 102, 241)` in monospace
- Four `pc-context-block` sections, each with an uppercase `h3` header:
  1. **NuGet Badge** — v1.0.0 (indigo #4338ca), .NET 8.0 (slate), license MIT (slate)
  2. **Icon Mock (64px / 32px)** — combined tablet + state machine SVG at both sizes
  3. **Syntax Highlighting** — keywords #4338ca bold, states #818cf8, event #30b8e8, operators #6366f1
  4. **State Diagram Accent** — Draft → Submit → Review, indigo stroke, emerald arrow

## Why

The exploration card format (width: 320px, dark #141414 background, context-block sections) is the canonical visual audit format for the Precept palette. Keeping the brand-spec's overview card in a bespoke format meant maintaining two divergent representations of the same information. The exploration format is more compact, better organized for at-a-glance review, and visually consistent with the exploration artifacts that informed the brand decision.

## Implementation Notes

- Added a `pc-` CSS class prefix family to brand-spec.html's `<style>` block. The existing `.palette-card` class in brand-spec serves the palette swatch grid (a different context) and could not be repurposed.
- The `pc-context-block h3` override resets brand-spec.html's global `h3` styles (uppercase + border-bottom) to match the exploration card's style.
- All surrounding § 1.4 content preserved: heading, both callouts, and the structural/state/event/data/rules/comments/verdicts cards below.

---

---

# Decision: Indigo Color System Overview Card

**Author:** J. Peterman (Brand/DevRel)
**Date:** 2026-04-05
**Section:** brand/brand-spec.html § 1.4 Color System
**Status:** Implemented

---

## What was decided

A self-contained **"Indigo Color System — Overview"** card was added at the top of section 1.4, before the per-role "Structure · Indigo" card. It provides a single-card reference for the complete 8-shade indigo ramp.

## Structure

| Element | Detail |
|---------|--------|
| Swatch bar | 48px solid `#6366F1` bleed strip |
| Gradient ramp | 8 equal segments: `#1e1b4b` → `#312e81` → `#3730a3` → `#4338ca` → `#6366f1` → `#818cf8` → `#a5b4fc` → `#c7d2fe` |
| Title | "Indigo · 239°" in `#6366F1` |
| Role table | Swatch · hex · role · usage; 8 rows |
| Badge | NuGet `#6366f1` |
| Snippet | 5-line LoanApplication DSL |
| Diagram | Draft → Submit → UnderReview, 280×80 SVG |
| Bottom note | Rationale for indigo selection |

## Shade roles

| Hex | Role | Usage |
|-----|------|-------|
| `#1e1b4b` | Ground | Dark indigo surface; diagram and editor backgrounds |
| `#312e81` | Deep | Border emphasis; icon background layer |
| `#3730a3` | Rich | Reserved; deep structural accent |
| `#4338ca` | Semantic | DSL control keywords (bold): precept, state, event, from, on… |
| `#6366f1` | Grammar | DSL grammar connectives (normal): as, ->, =, operators |
| `#818cf8` | Brand-Light | Secondary brand; code block borders; diagram stroke |
| `#a5b4fc` | Brand-Muted | Diagnostic code prefix (PRECEPT001); subtle highlights |
| `#c7d2fe` | Pale | Light muted; barely-there tint for callout backgrounds |

## Rationale

The existing section 1.4 jumped straight to per-role breakdown without ever showing the family as a whole. Designers and engineers reading the spec had no single reference for the complete ramp or the logic of the progression. The overview card fills that gap — it is a map before the street-level directions.

The in-context examples (badge, syntax, diagram) demonstrate the palette in the three surfaces where it actually appears, not in abstraction. Every shade appears at least once.

## Implementation notes

- No new CSS classes — all styling is inline, per brief
- Negative top/side margins on swatch bar and ramp achieve full-bleed effect inside `.card` padding box
- Arrow marker ID: `indigo-arrow-overview` (scoped to avoid `<defs>` collisions)
- Card inserted before `<!-- Structure -->` comment, line 383 in original file

## Closing line

*"Depth and authority. Selected over teal, amber, and steel. The only brand color that reads as both technical precision and earned trust."*

---

---

# Decision: Voice & Tone — Wit Integration into Brand Spec

**Date:** 2026-XX-XX
**Owner:** J. Peterman, Brand & DevRel
**Status:** Implemented
**Files:** `brand/brand-spec.html` (§1.2)

## Problem

Section 1.2 Voice & Tone stated: *"Serious. No jokes."* This was wrong. It didn't capture Precept's actual voice—one that contains dry wit, precision humor, the kind of humor that lands because it's understated and earned.

Shane's feedback: "Doesn't represent the wit element we want to incorporate."

## Decision

Revised section 1.2 to acknowledge and celebrate dry wit as a core voice element. Wit that flows from confidence and precision, not performance. Like Stripe docs. Like the best GitHub changelogs.

### What Changed

**1. Table Row (Serious ↔ Funny)**
- **Old:** "Serious. No jokes."
- **New:** "Dry wit welcome. Never forced. Precision finds the humor in the truth."

**2. Prose Paragraph (Brand Description)**
- **Old:** "The voice states facts. It doesn't hedge. It doesn't oversell. But it occasionally explains *when* something matters."
- **New:** "The voice states facts. It doesn't hedge. It doesn't oversell. It finds the wit in precision. When something matters, it says why — and the clarity itself can be the humor."

**3. Do Examples (Wit in Action)**
Added two examples showcasing precision humor without mockery:
- *"If you've been writing the same validation in four services, Precept has questions."*
- *"Turns out business rules don't change just because you moved them to a different service."*

**4. Status**
- Changed header status chip from `LOCKED` to `REVISED` (blue background, light text).

## Reasoning

Precept's wit is **earned from specificity**:
- The tool knows exactly what it does.
- The alternatives are slightly absurd.
- The humor doesn't mock the user—it states the truth in a way that makes the truth funnier than any joke.

This wit is **not performance**:
- It doesn't wink at the camera.
- It doesn't try too hard.
- It's confident enough to be understated.

Compare: "Say goodbye to bugs forever!" (performative) vs. "If you've been writing the same validation in four services, Precept has questions." (precision humor). The second works because it's true. The truth is the joke.

## Impact

- **Voice brand now reflects reality** — Precept's communications will include this wit, examples will model it, and new writers will understand it's intentional and encouraged.
- **Alignment with design systems** — Stripe, GitHub, and similar high-confidence brands all use wit this way. We're in good company.
- **Developer experience** — Developers recognize and appreciate this tone. It signals confidence and taste.

## Next Steps

- Monitor copy across docs, changelogs, and communication for consistency with this updated voice.
- Use the two new Do examples as models for future content.
- Consider highlighting this distinction in internal brand guidelines or contributor docs.

---

---

# Decision: README Restructure Proposal — Editorial Review Complete

**Date:** 2026-04-06
**Author:** Uncle Leo (Copy/Editorial)
**Verdict:** Approve with required changes
**Input artifact:** `brand/references/readme-restructure-proposal.md`
**Full review:** `brand/references/readme-restructure-review-uncle-leo.md`

---

## Required Changes Before Rewrite

**RC-1 — Section map / hero label conflict**
The Recommended Section Order block uses H3 markers (`### The Contract`, `### The Execution`) but the Hero Treatment section specifies bold inline labels, not H3 headings. The rewrite will get conflicting signals. The section map must be corrected to match the Hero Treatment spec (bold inline labels, not subheadings).

**RC-2 — Getting Started context reminder is missing, not implicit**
The proposal claims Elaine's required one-sentence context reminder is "implicit" in step 1's benefit copy. It is not. The context reminder (re-anchoring non-linear readers to what Precept is) must be written explicitly before or within step 1. This was a hard requirement in Elaine's research.

---

## Wording Concerns (addressable during rewrite)

- WC-1: "Badge walls signal maintenance anxiety" — editorializing; suggest "add visual noise without adding signal at the awareness stage"
- WC-2: AI tooling "lead with / don't bury" argument conflates two separate points; suggest splitting
- WC-3: Closing tagline "One file. Every rule. Prove it in 30 seconds." is ambiguous — proposal flourish or proposed README copy? Label it.

---

## What Is Approved

Overall structure, section order, CTA hierarchy (Primary/Secondary/Tertiary), dual-audience table format, constraint table, hero spec (18-20 lines DSL + 5-line C# block), and the closing summary framing ("proves before it teaches") are all approved without changes.

---

## Status

Blocked on RC-1 and RC-2. Frank's four required changes are separate and not duplicated here. George's technical accuracy review is ongoing.

---

---

# README Revamp — Scope & Priority Recommendation

**From:** Steinbrenner (PM)
**Date:** 2026-04-04
**Context:** Parallel research to Peterman's brand/copy work. This is the product/adoption strategy for the README revamp.
**Research Document:** `brand/references/readme-research-steinbrenner.md`

---

## Executive Summary

The current Precept README is structured like API documentation, not a product landing page. Based on analysis of 9 category-defining tools (xstate, Polly, Temporal, Terraform, Bun, Deno, etc.), the README must:

1. **Define the category** ("domain integrity") before describing the tool
2. **Lead with tooling** (MCP, VS Code, AI-native) as the primary differentiator
3. **Provide a quickstart** that gets from "I saw this" to "I have it running" in <3 minutes
4. **Teach the mental model** before showing syntax

This recommendation outlines the minimum viable README revamp scope from a product adoption perspective.

---

## Critical Gaps in Current README

### 1. **No Problem Statement**
**Current:** README opens with "Precept is a DSL for defining domain integrity constraints..."
**Issue:** Developers don't know what "domain integrity" is or why they need it.
**Fix:** Lead with the problem: "Your validation is scattered across controllers, your state machine is split from your business rules, and your constraints are duplicated in code and tests. Precept unifies all three into one executable contract."

### 2. **No Quickstart Path**
**Current:** README has installation instructions but no "first working example" flow.
**Issue:** Developers can't evaluate time-to-value without seeing the quickstart friction.
**Fix:** 3-step quickstart: Install extension → Create Order.precept → Get real-time diagnostics. Or: `dotnet add package Precept` → 10-line example → 3-line C# usage.

### 3. **No Category Education**
**Current:** README assumes you know what "domain integrity" means.
**Issue:** We're establishing a new category, not implementing a known pattern.
**Fix:** Add "What is Domain Integrity?" section that explains the unified state+data+rules model.

### 4. **Tooling Story Buried**
**Current:** VS Code extension, MCP server, language server mentioned in passing.
**Issue:** This is Precept's strongest differentiator vs. xstate/FluentValidation — and it's hidden.
**Fix:** Lead with tooling: "Precept is a domain integrity DSL with AI-native tooling. Write .precept files, get real-time diagnostics in VS Code, and let Claude reason about your domain model through MCP."

### 5. **No Developer Journey Mapping**
**Current:** Sections appear in implementation order (what we built), not evaluation order (what developers need to decide).
**Issue:** Developers evaluate tools through a 4-stage journey (Awareness → Evaluation → Trial → Adoption). Section order must match this journey.
**Fix:** Restructure to optimal adoption order (see Section Order proposal below).

---

## Minimum Viable Revamp Scope

### Phase 1: Above-the-Fold (Must-Have)

**Goal:** Hook the developer in 5 seconds, convince them to scroll in 30 seconds.

**Changes:**
1. Add **problem statement** (1-2 sentences): "Precept unifies your entity's state, data, and business rules into a single executable contract."
2. Add **differentiation tagline** (1 sentence): "Write .precept files, get real-time diagnostics in VS Code, and let AI agents reason about your domain model."
3. Move **badges** (build, license, version) immediately below logo
4. Add **quickstart** (3 steps max): Install → Example → Run

**Effort:** 2 hours (copy) + 1 hour (structure)

---

### Phase 2: Educational Section (Should-Have)

**Goal:** Teach "domain integrity" as a concept so developers understand the category.

**Changes:**
1. Add **"What is Domain Integrity?"** section
   - Explain the problem: scattered validation, split state machines, fragmented rules
   - Show how Precept unifies all three
   - Position as "single source of truth for domain behavior"
2. Add **simple before/after** example (optional but strong):
   - Before: C# with separate FluentValidation rules, Stateless state machine, scattered business logic
   - After: 15-line .precept file

**Effort:** 3 hours (research + copy)

---

### Phase 3: Feature Positioning (Should-Have)

**Goal:** Communicate capabilities without becoming a feature list.

**Changes:**
1. Add **"Key Features"** section with 3-5 bullets:
   - Unified state machines + validation + business rules
   - Real-time diagnostics in VS Code
   - AI-native tooling (MCP server, Claude integration)
   - Type-safe DSL with compile-time checking
   - Zero runtime dependencies
2. Keep bullets **benefit-focused**, not feature-focused:
   - Wrong: "Supports invariant, assert, and reject keywords"
   - Right: "Invalid states become structurally impossible"

**Effort:** 2 hours (copy)

---

### Phase 4: Section Reordering (Must-Have)

**Goal:** Match section order to developer evaluation journey.

**Current order:** Installation → Building → Testing → Documentation → Contributing
**Optimal order:** Problem → Differentiation → Quickstart → "What is DI?" → Features → Docs → Community

**Changes:**
1. Move **Installation** and **Quickstart** to top (after problem statement)
2. Move **"What is Domain Integrity?"** before feature deep-dive
3. Move **Building/Testing** to Contributing section (developer-focused, not user-focused)
4. Keep **Documentation** link prominent (adoption phase)

**Effort:** 1 hour (restructure)

---

### Phase 5: Comparison Strategy (Nice-to-Have)

**Goal:** Handle "why not xstate/FluentValidation/Stateless?" without being defensive.

**Recommendation:** Use **implicit differentiation** — never name competitors, but position Precept as solving the problems they create:

- "Unlike separate state machine and validation libraries, Precept unifies state, data, and rules in one file."
- "Precept isn't a state machine library or a validation library — it's both, plus business rules enforcement."

**Optional:** If comparison table is needed, place it **below** quickstart with title "How Precept Compares" and focus on architectural differences (unified vs. split), not feature counts.

**Effort:** 2 hours (copy) — **Only if Shane requests explicit comparison**

---

### Phase 6: Visual Assets (Nice-to-Have)

**Goal:** Show, don't tell.

**Recommendations:**
1. **Screenshot:** VS Code extension with hover/diagnostics on a .precept file
2. **GIF/Video:** Live preview pane showing state machine visualization
3. **Diagram:** "Before Precept" (scattered validation/state/rules) vs. "After Precept" (unified .precept file)

**Effort:** 3-5 hours (design + screenshot + hosting)

---

## Section Order Proposal

Based on developer evaluation journey research:

```markdown

---

# Precept

[Logo]

**One-sentence hook:** A domain integrity engine for .NET that unifies state, data, and business rules.

**Problem statement:** Your validation is scattered across controllers, your state machine is split from your business rules, and your constraints are duplicated in code and tests. Precept binds all three into a single executable contract.

**Badges:** [Build] [Version] [License]

## Quick Start

1. Install VS Code extension: Search "Precept" in Extensions
2. Create `Order.precept` with 10-line example
3. Start typing — you'll get completions and diagnostics

Or use NuGet: `dotnet add package Precept`

[Link to Getting Started docs]

## What is Domain Integrity?

[Educational section explaining the unified state+data+rules model]

## Key Features

- Invalid states become structurally impossible
- Real-time diagnostics in VS Code
- AI-native tooling (MCP server, Claude integration)
- Type-safe DSL with compile-time checking
- Zero runtime dependencies

[Link to full feature docs]

## Documentation

- [Language Guide](docs/language-guide.md)
- [Runtime API](docs/runtime-api.md)
- [MCP Tools](docs/mcp-tools.md)
- [Samples](samples/)

## Community & Contributing

[Discord/GitHub/Contributing links]
```

---

## Social Proof Strategy

**Challenge:** Precept is new — can't compete on download counts or stars yet.

**Alternatives:**
1. **Test count:** "666 tests across 3 projects" (shows maturity)
2. **Sample count:** "20+ canonical domain models included" (shows real usage)
3. **Feature badges:** "Featured in Claude Marketplace" (once available)
4. **Build status:** CI passing badge (shows active maintenance)
5. **VS Code rating:** Extension rating (once published)

**Recommendation:** Use build status + test count + sample count now. Add download badges once >1k installs.

---

## Comparison to Studied Tools

| Tool       | Category Strategy        | Quickstart Position | Tooling Story         |
|------------|--------------------------|---------------------|-----------------------|
| xstate     | Category definition      | Section 2           | Stately Studio (lead) |
| Polly      | Category definition      | Section 3           | Buried                |
| Temporal   | Category definition      | Section 2 + video   | CLI (prominent)       |
| Terraform  | Category definition      | External docs       | Buried                |
| FastEndpoints | Direct positioning    | External docs       | Buried                |
| Bun        | Direct positioning       | Section 2           | Built-in (implied)    |
| Deno       | Problem-correction       | Section 2           | Built-in (implied)    |
| **Precept** | **Category definition** | **Section 1 (new)** | **Lead differentiator** |

**Insight:** No comparable tool leads with tooling as primary differentiator. This is Precept's positioning opportunity.

---

## Risks & Mitigations

### Risk 1: "Domain integrity" is too abstract
**Mitigation:** Lead with concrete problem (scattered validation/state/rules) before introducing the term. Use "What is Domain Integrity?" section to define it.

### Risk 2: Developers don't recognize they have this problem
**Mitigation:** Show before/after example — make the pain visible. "You're already writing validation, state machines, and business rules. Precept just puts them in one place."

### Risk 3: Quickstart feels too complex
**Mitigation:** Offer two paths: VS Code extension (zero code) OR NuGet package (3 lines of C#). Let developer choose their entry point.

### Risk 4: Tooling story overshadows DSL quality
**Mitigation:** Position tooling as *enabler*, not *replacement* for good DSL design. "The DSL is concise. The tooling makes it fast."

---

## Success Metrics

README revamp is successful if:

1. **Time-to-quickstart** < 60 seconds from opening README
2. **Mental model clarity** — developer understands "domain integrity" without reading docs
3. **Differentiation signal** — developer sees how Precept differs from xstate/FluentValidation within first scroll
4. **CTA completion** — developer clicks through to docs or installs extension

---

## Next Steps

1. **Shane review** — approve/reject scope recommendations
2. **Peterman handoff** — share research for brand/copy integration
3. **Content creation** — write problem statement, quickstart, "What is DI?" section
4. **Structure implementation** — reorder sections, add badges, create quickstart flow
5. **Visual assets** (if approved) — screenshot extension, create before/after diagram

---

## Appendix: Research Sources

- xstate: https://github.com/statelyai/xstate
- Polly: https://github.com/App-vNext/Polly
- Temporal: https://github.com/temporalio/temporal
- Terraform: https://github.com/hashicorp/terraform
- FastEndpoints: https://github.com/FastEndpoints/FastEndpoints
- Bun: https://github.com/oven-sh/bun
- Deno: https://github.com/denoland/deno
- TypeScript: https://github.com/microsoft/TypeScript
- Axios: https://github.com/axios/axios

Full analysis: `brand/references/readme-research-steinbrenner.md`

---

## Model Policy: Use Latest Available Versions

**Filed by:** User (via Copilot)
**Date:** 2026-04-04T20:30:36Z
**Status:** ACTIVE

Always use the latest version of an available model rather than older pinned model versions. Global `defaultModel` constraint removed from `.squad/config.json` to enable automatic routing. Agent-specific overrides (Frank, Uncle Leo) remain intact.

---

## Agent Model Override: Elaine → claude-sonnet-4.6

**Filed by:** User (via Copilot)
**Date:** 2026-04-04T20:38:09Z
**Status:** ACTIVE

For design and polish work, pin Elaine to Claude Sonnet 4.6 (latest available Sonnet). Applied to `.squad/config.json` via `agentModelOverrides.elaine`.

**Rationale:** Design and UI work benefits from Sonnet's balanced speed/reasoning profile over Opus. User directive captured as team memory.

---

## Decision: Mapping Table Visual Unification

**Author:** Elaine
**Date:** 2026-04-05
**Status:** Proposed
**Category:** UX Design

Convert all three mapping tables in `brand/brand-spec.html` (§2.1 Reserved Verdict Colors, §2.2 Static Elements Compile-Time, §2.2 Runtime Verdict Overlay) to use identical `.sf-palette` component structure from §1.4:
- Card container with rounded corners, dark background, gradient header
- Title + subtitle describing purpose
- Grouped sections with semantic labels
- Row structure with 56px gradient swatches and info grid

Visual consistency builds trust in the specification. The row-with-swatch pattern is more scannable than tabular data for color reference. All three tables now share exact visual DNA with §1.4.

---

## Model Policy: Opus Escalation Acceptable When Needed

**Filed by:** User (via Copilot)
**Date:** 2026-04-04T20:38:55Z
**Status:** ACTIVE

Claude Sonnet 4.6 remains the default model for design and polish work. However, aggressive escalation to Claude Opus 4.6 is acceptable when Sonnet's context or reasoning capability proves insufficient for complex design decisions.

**Applied to:** Elaine agent (baseline Sonnet 4.6) + team-wide escalation policy.

**Rationale:** User directive clarifying nuanced model guidance — Sonnet handles most design polish, but Opus available for premium reasoning tasks.

---

### 2026-04-04T21:23:19Z: User directive
**By:** shane (via Copilot)
**What:** Gold is the hero color and should have a prominent role in hero samples and syntax coloring so rule-oriented content stands out.
**Why:** User request — captured for team memory

---

### 2026-04-04T21:31:26Z: User directive
**By:** shane (via Copilot)
**What:** Relax the Gold syntax-only rule so Gold can be used sparingly as a hero color to highlight what is truly valuable and important, where appropriate. Exact wording still needs refinement.
**Why:** User request — captured for team memory

---

### 2026-04-04T21:40:08Z: User directive
**By:** shane (via Copilot)
**What:** Use the word "judicious" to describe Gold usage; Gold should be present in the tablet-only mark as well, and Emerald should own the transition arrow in both brand-mark contexts.
**Why:** User request — captured for team memory

---

### 2026-04-04T21:55:44Z: User directive
**By:** shane (via Copilot)
**What:** README restructure should retain some of the 'why this approach' in the README, keep contributor-focused developer loop content at the bottom, and work back in the phrase about treating business constraints as unbreakable precepts while keeping the message tight.
**Why:** User request — captured for team memory

---

### 2026-04-04T22:30:43Z: User directive
**By:** shane (via Copilot)
**What:** For README pass 1, use the already-settled temporary sample, tweak the tagline, and use the current brand mark for now; all three can be revisited later.
**Why:** User request — captured for team memory

---

### 2026-04-04T22:49:59Z: User directive
**By:** shane (via Copilot)
**What:** Stop using Haiku. Existing no-Haiku preference must be enforced consistently for squad routing and agent launches.
**Why:** User request — captured for team memory

---

---

# Brand Mark Icons: Spec Alignment

**Author:** Elaine (UX Designer)
**Date:** 2026-04-04
**Scope:** §1.3 Brand Mark — three forms + lockup combined icon

## Decision

Brand mark icons now follow the same semantic color and shape vocabulary as §2.2 State Diagram.

### Color changes
- **Transition arrows** use Grammar Indigo `#6366F1`, not Emerald `#34D399`. Emerald is reserved for verdict overlay per §1.4/§2.2.
- **Node borders** use Semantic Indigo `#4338CA` (initial: 2.5px, intermediate: 1.5px) per the §2.2 diagram color mapping.
- **Combined mark code page border** uses `#6366f1` (matching standalone tablet icon) instead of `#27272a` (was nearly invisible against `#1e1b4b` ground).

### Shape changes
- **Destination state node** changed from circle (Initial shape) to rounded rectangle (Intermediate shape) — circles are reserved for Initial nodes per §2.2 lifecycle roles.

### Color key
Updated to reflect actual icon palette: Semantic `#4338CA`, Grammar `#6366F1`, Accent `#818CF8`, Ground `#1E1B4B`.

## Rationale

Icons are abstractions, but they still speak the spec's locked visual language. Using Emerald for transitions and circles for non-initial states contradicted §2.2's explicit rules. The combined mark's code page border at `#27272a` created an undesirable fade-out effect that undermined the icon's legibility.

## Pattern established

**Brand mark icons inherit their parent surface's semantic rules.** If a brand mark depicts a state diagram, it uses §2.2 diagram colors and shapes. If it depicts a code page, it uses §2.1 syntax colors. The icons don't get to invent their own palette — they are small-scale renderings of the locked system.

---

---

# Decision: Gold Brand Mark Exception

**Date:** 2026-04-04
**By:** Elaine (UX Designer)
**Status:** Implemented — pending Shane sign-off

## Decision

Gold (`#FBBF24`) is permitted as a single sparse accent in the **combined brand mark only** — the third icon (tablet + state machine). It does not appear in the standalone diagram icon or the standalone tablet icon.

## Placement

The Gold stroke represents the `because "…"` line — the human-readable rule message baked into the running system. In the SVG, it is the short horizontal line at `y=33` inside the tablet/code area: shorter than the body lines above it (stopping at x=34 vs x=40), stroke-width 1, opacity 0.65. Deliberately dim and singular so it reads as an accent, not a second brand color.

## Why

Gold already carries this meaning in syntax highlighting: every `because` and `reject` string is Gold in the editor. The combined mark unifies the state machine and the written rule — it is the only icon that shows both halves. Adding one Gold line there extends an existing meaning rather than inventing a new one. It's a philosophical nod, not a decorative choice.

## Constraints

- **One mark only.** The diagram icon and the tablet icon remain unchanged.
- **One line only.** Gold must not appear on structural elements (rect outlines, arrowheads, circles).
- **Not a new UI color.** This exception does not permit Gold in badges, borders, button states, status chips, or any other UI surface.
- **Not a new accent lane.** Gold remains syntax-primary. This is a narrow named exception, not a policy relaxation.
- Amber (`#FCD34D`) continues to own warning/caution semantics. The visual distance between Gold and Amber is maintained; Gold in the mark is dimmer (`opacity: 0.65`) and lives in a non-signal context (brand icon, not status UI), so no semantic collision occurs.

## Files Changed

- `brand/brand-spec.html` — SVG updated, color key updated, prose updated in §1.3, §1.4 intro, §1.4.1, and the Rules · Gold surface section
- `.squad/skills/color-roles/SKILL.md` — Rule 2 and the Gold row updated to reflect this exception

---

---

# Decision: Brand Mark Color Revision — Gold Judicious + Emerald Arrows

**Date:** 2026-04-05
**Author:** Elaine (UX Designer)
**Requested by:** Shane

## What Changed

### 1. Transition Arrow → Emerald (#34D399)
All three brand mark SVGs that carry a transition arrow now use Emerald for the arrow line and arrowhead. Previously both were Indigo (`#6366F1`). Emerald is already the "allowed flow / enabled transition" color across the product — the mark should say the same thing.

**Affected SVGs:**
- State + transition mark (standalone): arrow line + arrowhead
- Combined mark (tablet + state machine): arrow line + arrowhead
- Wordmark lockup icon (combined form, header context): arrow line + arrowhead

### 2. Gold — Less Muted, Now in Tablet Mark Too
**Combined mark:** Gold `because` line raised from `stroke-width="1" opacity="0.65"` to `stroke-width="1.5" opacity="0.9"`. It was too easy to overlook; the remembered rule should register.

**Tablet-only mark:** The shortest bottom line (the `because`-text position) is now Gold `#FBBF24` at `opacity="0.85"`, up from a muted zinc line at `opacity="0.4"`. This gives the tablet mark a family-consistent Gold signal without adding structural noise.

**Design rationale:** The word "judicious" was Shane's framing — Gold earns its place by being sparse and meaningful. Giving it presence in the tablet mark improves family consistency. The restraint is still intact: it's one line, at the bottom, in a role that encodes human language.

### 3. Language: "Judicious" replaces "Narrow exception"
All copy and skill rules that said "narrow exception" or "combined mark only" for Gold now say "judicious exception" or "tablet and combined marks." The tone shift from "barely allowed" to "deliberately placed" aligns with how the mark system actually works.

## Color Key Updated
The brand mark color key now shows Emerald as a fifth entry (Transition #34D399) alongside the existing four, and Gold's annotation reads "(judicious — tablet & combined)".

## Files Changed
- `brand/brand-spec.html` — three SVG marks, wordmark lockup, color key, §1.4 Gold description, §1.4 usage rules table, §2.1 Gold copy
- `.squad/skills/color-roles/SKILL.md` — Emerald row, Gold Syntax Accent row, Rule #2

## Guardrails Maintained
- Emerald is not a second structural lane — it only appears on the transition arrow (one element per mark)
- Gold does not appear on the state-only mark (no `because` line there; nothing to represent)
- State shapes and Indigo/Violet structure unchanged

---

---

# Decision: Elaine's Direct Contribution Role in README Rewrite

**Author:** Elaine (UX Designer)
**Date:** 2026-04-07
**Status:** Guidance — not yet implemented
**Context:** Peterman holds primary ownership of the README rewrite. This document defines where Elaine contributes directly vs. where she reviews.

---

## Decision

Elaine co-authors six specific areas. Peterman authors everything else. No section gets final sign-off without Elaine's explicit approval on the areas listed below.

---

## Elaine's Direct Contribution Areas

### 1. Title Block — Above-the-Fold Composition

Elaine owns the layout spec, not the copy:
- Logo at **48px** (not 64px SVG full) to clear the 550px mobile viewport with room for one badge row, one blockquote, and the two-sentence hook
- **Badge count: 3 maximum** — NuGet version, MIT license, build status, in that order
- Badge alt text template: each must include the actual value (e.g. `NuGet version 1.2.3`, `Build: passing`)
- The complete title block must be tested at **400px, 550px, and 800px** viewport widths before finalizing

Peterman writes the copy. Elaine validates the layout passes the mobile test.

---

### 2. Hero Format Template — Quick Example Section

Elaine owns the **form**, not the code:
- Two labeled blocks under `## Quick Example`: `**The Contract** (filename.precept):` and `**The Execution** (C#):`
- DSL block: 18-20 lines, **≤60 characters per line** — this is a hard layout constraint, not a style preference
- C# block: 5 lines maximum, no comments, no imports
- Both blocks tagged with language identifiers (` ```precept `, ` ```csharp `)

The 60-character line constraint shapes what Peterman can write in the DSL hero. He needs Elaine's template before authoring the hero code. **This is a dependency, not a review.**

---

### 3. Getting Started — CTA Structure

Elaine directly authors the structural template for Getting Started:
- Numbered 1/2/3 sequence is Elaine's format (not Peterman's preference)
- Step 1 must install VS Code extension — not NuGet, not Copilot plugin
- Step 3 must carry the "when you're ready" qualifier to signal it is a progression, not a simultaneous decision
- Copilot plugin is **removed from Getting Started entirely** — it surfaces only in Section 4 (AI-Native Tooling)

The words inside each step are Peterman's. The order, the hierarchy signal, and the single-primary-CTA enforcement are Elaine's.

---

### 4. All Section Headings

Elaine directly approves every H2 and H3 heading before the rewrite ships:
- No emoji at heading start (screen reader and AI parseability requirement)
- Headings are descriptive, not clever — "Live Editor Experience" not "World-Class Tooling"
- H1 → H2 → H3 with no level skips — verified by Elaine after the full draft exists

This is a two-pass process: Peterman drafts headings; Elaine audits and edits them. Peterman does not unilaterally finalize headings.

---

### 5. Visual Separators and Prose Scannability

Elaine owns the formatting rules applied throughout:
- `---` horizontal rule between every major H2 section — Elaine places these, not Peterman
- Prose paragraphs: **maximum 2-3 sentences** — Elaine edits any paragraph that exceeds this
- Feature lists: **bullets, not prose** — Elaine converts any 3+ item run into a bulleted list
- Blockquote callouts (`> **Note:** ...`) for important asides — Elaine decides which asides qualify

Peterman writes the content. Elaine applies and enforces the scannability format throughout the full document.

---

### 6. Contributing Section — Formatting Pass

Elaine owns the formatting of the Contributing section:
- All build commands in properly tagged ` ```bash ` blocks
- Section placed **after Learn More, before License** — Elaine enforces this position
- No contributor content bleeds into the primary user flow above it
- Section opens with a single-sentence scope statement ("Precept is built with .NET 10.0 and TypeScript. The VS Code extension, language server, MCP server, and runtime are all in this repository.")

---

## What Peterman Owns Exclusively

- Hook copy (brand voice: the definition blockquote, positioning sentence, clarifier sentence)
- Section 4 differentiation copy (AI-Native Tooling, Unified Domain Integrity, Live Editor Experience — body content)
- Learn More link list (link text and ordering)
- License section
- Overall section order (already locked in the restructure proposal)
- Hero domain selection (Order Fulfillment vs. Subscription Billing — deferred to Shane)

---

## Collaboration Protocol

- Peterman drafts full sections → Elaine does a formatting pass → no copy changes, only structural/formatting edits
- Any conflict between Elaine's formatting pass and Peterman's intended copy hierarchy gets escalated to Shane
- Heading audit happens on the full draft, not section by section
- The 60-char line constraint in the hero is a **hard block** — Peterman cannot finalize hero code without Elaine's line-length validation

---

---

# Decision: Elaine's Gate Role in README Rewrite Flow

**Date:** 2026-04-07
**By:** Elaine (UX Designer)
**Status:** Recommendation — pending Shane acknowledgment

---

## Decision

Elaine enters the README rewrite at **two specific gates**, not throughout the writing process.

### Gate 1 — Pre-Draft Hero Domain Confirmation (now)

Before Peterman writes a line, the hero domain must be locked. Elaine's role: confirm the selected domain satisfies the "universally relatable" requirement (no SaaS jargon, immediately recognizable by any .NET developer). This is a 5-minute validation, not a design pass. Per the `proposal-gate-analysis` skill: hero domain is always a Gate-Before-Start item. Changing it after the draft undoes the draft.

### Gate 2 — Post-Draft UX Compliance Audit

After Peterman delivers the first draft, Elaine runs a formal pass against the 16 hard constraints already embedded in the proposal. The audit is pass/fail per constraint — not a copy edit, not a rewrite. Peterman resolves any failures. Shane signs off after both Peterman and Elaine clear.

---

## Elaine Owns (Structural UX Layer)

| # | What | Why |
|---|------|-----|
| 1 | Above-the-fold audit (550px viewport) | Constraint #1 |
| 2 | Viewport resilience (no horizontal scroll at 400px) | Constraint #2 |
| 3 | CTA hierarchy enforcement | Constraint #3 |
| 4 | Heading hierarchy audit (H1→H2→H3, no skips) | Constraints #4, #5 |
| 5 | Scannability mechanics (prose length, bullets, separators) | Constraints #6, #7 |
| 6 | Hero line length audit (≤60 chars/line) | Constraint #8 |
| 7 | Progressive disclosure order | Constraint #10 |
| 8 | AI/human readability compliance (code block tags, link text, alt text) | Constraints #11–#14 |

---

## Peterman Owns (Brand/Copy Layer)

- Copy quality, brand voice, tone
- Positioning language (exact form of category claim)
- Hero sample content (DSL lines, within Elaine's line-length constraints)
- Section content decisions
- Badge selection and ordering
- Link anchor text (Elaine ensures form, Peterman ensures content)

---

## The Clean Split

**Peterman writes. Elaine audits structure.** These are separate passes, not concurrent collaboration. Peterman does not need Elaine present while writing. Elaine does not write copy. If there is a heading skip, Elaine flags it — Peterman corrects it. If the CTA hierarchy is correct but the positioning language is weak, Peterman owns that.

---

## Why This Matters

Elaine's constraints were treated as hard non-negotiables in the proposal precisely because they are prerequisites, not suggestions. But constraints on paper are not enforcement. The only way they hold is if the constraint-holder reviews the final artifact. A proposal that satisfies all 16 constraints structurally, delivered as a README that satisfies none of them, is a failure. Elaine's post-draft gate is what closes that loop.

---

## Gate Sequence (Full)

1. Shane — hero domain lock (Gate-Before-Start)
2. Peterman — writes draft
3. Elaine — UX compliance audit (post-draft)
4. Peterman — resolves Elaine's flagged issues
5. Frank — architectural accuracy check (contributor section, commands)
6. Shane — final sign-off

---

---

# Decision: README Form/Shape Pass

**Author:** Elaine (UX)
**Date:** 2026-04-07
**Status:** Applied

## What Changed

Form/shape improvements to README.md for improved scannability and viewport efficiency:

1. **Title block** — Removed emoji from H1, shortened badge labels ("NuGet version" → "NuGet"), tightened definition blockquote (removed invisible Unicode spacing)

2. **Quick Example** — Shortened C# variable names (`definition` → `def`, `engine` → `eng`, `instance` → `inst`) for visual compactness; kept vertical layout (side-by-side tables don't render code blocks reliably on GitHub)

3. **Getting Started** — Moved prerequisite to a single bold line (was a blockquote callout), cut redundant intro sentence, tightened step prose to single sentences where possible

4. **What Makes Precept Different** — Collapsed three H3 subsections into bold inline headers with em-dash lead-ins; preserved the bullet list for Unified Domain Integrity; cut granular bullet details from AI-Native and Live Editor (now single-sentence summaries)

5. **Learn More** — Changed from bullet list to table format for better scanability and alignment

6. **Contributing** — Collapsed language server and VS Code extension build details into a simpler two-command code block; trimmed quick-reference table to essential rows

## Rationale

The previous draft had good content but felt dense. Each section competed for attention. These changes improve heading rhythm, reduce vertical scroll, and make the README feel "shaped" rather than "stacked."

## What I Did Not Change

- Core messaging and word choices (Peterman's domain)
- Section order
- The brand mark (kept current state per task brief)
- Hero domain (Order) — marked as temporary but acceptable

---

---

# Decision: Shape-First README Pass — Elaine's Position

**Filed by:** Elaine
**Date:** 2026-04-08
**Status:** Recommendation — Pending Shane sign-off
**Context:** Shane asked whether Elaine should produce a form/shape skeleton before Peterman writes the README copy.

---

## Recommendation

**Targeted scaffold — not a full skeleton pass.**

The restructure proposal already IS the structural spec at high fidelity. A full skeleton pass reproduces that work in a different format. The targeted intervention that adds genuine value is a **hero code block scaffold**: an annotated Markdown stub showing Peterman exactly what viewport-safe DSL looks like in 18-20 lines at ≤60 chars per line — with constraint callouts embedded as HTML comments.

---

## What Elaine Would Produce in Shape-First Mode

A `README-scaffold.md` file (not README.md itself) containing:

- All headings in place at correct H1/H2/H3 levels
- Badge row slot with alt-text annotation
- Blockquote definition slot
- Two-sentence hook slot with word-count annotation
- Hero DSL block as a commented stub:
  ```
  <!-- ≤20 lines | ≤60 chars per line | viewport constraint #8 -->
  <!-- Required: precept decl, 2-3 fields, 3 states (initial marked), 2 events, 1 guard, 1 invariant/assert -->
  ```
- 5-line C# execution block slot
- All section headings and visual separators in place
- Constraint callout comments above each constrained content slot

**What it is not:** copy. No body prose. No positioning language. Peterman owns all of that.

---

## Does This Reduce or Increase Churn?

**Reduces churn marginally for the full file; meaningfully for the hero block.**

The proposal is specific enough that section-level violations are unlikely. The real risk is the hero — Peterman writes a sample, we find at constraint audit that line 7 is 78 characters, and that means a content rewrite after copywriting is done. An annotated hero slot catches that class of issue before pen touches paper.

For everything outside the hero, the scaffold is a convenience, not a necessity.

---

## Peterman's Counter-Position

Peterman filed a recommendation (`.squad/decisions/inbox/j-peterman-shape-first-readme.md`) that the skeleton pass is redundant and adds an extra round trip. His argument is sound for the full file. The disagreement is narrow: Elaine's position is that the hero block specifically benefits from a pre-draft structural artifact. Peterman's position is that this is a "single targeted question, not a skeleton pass."

**They're describing the same intervention at different scopes.** Shane can resolve this by scope-limiting: no full skeleton, but a specific hero consultation before Peterman writes that section.

---

## What Must Be Locked Before Copywriting Starts

| Item | Status | Action Required |
|------|--------|----------------|
| Section order | Locked in proposal | None |
| 16 hard constraints | Locked | None |
| Badge count (3 max) | Specified, not signed off | Shane confirms |
| **Hero domain** (Order Fulfillment vs. Subscription Billing) | **Unresolved — deferred to Shane** | **Shane decides** |
| Time Machine hero — retire it | Proposed, not explicitly signed off | **Shane signs off** |
| Above-the-fold test baseline | Specified at 550px | Confirm before final |

**Hero domain selection is the hard gate.** Everything else can proceed without Elaine's scaffold. The hero cannot.

---

## Correct Gate Sequence If Shane Approves Scaffold

1. **Shane** — selects hero domain; signs off on Time Machine retirement
2. **Elaine** — produces `README-scaffold.md` (scope: hero stub + heading skeleton only; ~30 minutes)
3. **Peterman** — writes README copy into the scaffold
4. **Elaine** — constraint audit: 16-row pass/fail table
5. **Peterman** — resolves any failures
6. **Shane** — final sign-off

---

## Correct Gate Sequence If Shane Skips the Scaffold

1. **Shane** — selects hero domain; signs off on Time Machine retirement
2. **Elaine** — single hero consultation: what domain fits cleanest in ≤60 chars/line?
3. **Peterman** — writes README against the proposal directly
4. **Elaine** — constraint audit: 16-row pass/fail table
5. **Peterman** — resolves any failures
6. **Shane** — final sign-off

Both paths are valid. The scaffold path prevents one class of hero-related rework. The no-scaffold path is one pass shorter. The hero domain decision is required in both cases.

---

---

# Decision: Gold — Judicious Use, Tablet Mark Inclusion, Emerald Transition Arrows

**Date:** 2026-04-07
**By:** J. Peterman (Brand/DevRel)
**Status:** Implemented — Shane sign-off requested

## Summary

Three related changes to the brand color system, directed by Shane:

1. **Gold policy language** — "narrow exception" replaced with "judicious exception" everywhere Gold's limited use is described. The word governs the policy; it is not decorative.

2. **Tablet mark carries Gold** — The standalone tablet icon now includes a single Gold `because` line (same signal as the combined mark). The family of marks reads consistently: where a tablet form appears, the remembered rule is present. The combined mark's Gold opacity was also boosted from 0.65 → 0.9 (was visually muted).

3. **Transition arrows are Emerald** — The §2.2 state diagram SVG (marker, edge lines, legend) and the §2.2 diagram color mapping swatches now use Emerald `#34D399`. The "State + transition" brand mark icon was already Emerald; the combined mark icon has also been updated. Emerald owns transition directionality across the system — "allowed flow," not "grammar structure."

## Rationale

Gold's prior scope ("combined brand mark only") was a Elaine-era constraint based on a philosophy of maximum restraint. Shane's direction expands the scope minimally: the tablet form is the vessel for written rules across all three marks, so Gold belongs there wherever the tablet appears. The combined mark is primary, but the standalone tablet is the same conceptual object — it would be peculiar for it to be silent about the rule it represents.

Emerald on transition arrows is semantically correct: Emerald means "allowed, do, go." A transition arrow is the diagram's way of saying the system may move. Indigo on arrows was a structural reading (grammar connective tissue); Emerald is a semantic reading (the allowed path forward). The latter is more honest.

## Gold Policy As It Stands

- **Primary:** `because` / `reject` string content in syntax highlighting. Always.
- **Judicious exception:** A single `because` line in the tablet mark and combined mark. One line. No other uses.
- **Never:** badges, borders, button states, status chips, general UI emphasis, signal overlay.

## Files Changed

- `brand/brand-spec.html` — tablet SVG, combined mark, §2.2 SVG diagram, §2.2 color mapping, §1.4 palette card, §1.4.1 cross-surface table, §2.1 Rules · Gold description, brand paragraph, color key
- `.squad/skills/color-roles/SKILL.md` — (already updated in prior session; confirmed current)

---

---

# Decision: Canonical hero snippet now lives in brand-spec §2.6

**Date:** 2026-04-08
**By:** J. Peterman (Brand/DevRel)
**Status:** Complete — Ready for review & merge to decisions.md

---

## Decision

Reworked `brand/brand-spec.html` §2.6 into the canonical reusable hero-snippet artifact for the current README example. The section now mirrors the live README sample verbatim, keeps its **TEMPORARY** status explicit, and provides cross-surface reuse rules for README, VS Code Marketplace, NuGet, Claude Marketplace, and AI prompt contexts.

## Why this was necessary

The previous §2.6 had already established the right intent, but it had drifted from reality in four important ways:

1. The DSL rendering no longer matched the live README exactly (`>= 0` had drifted to `= 0`).
2. The C# execution block omitted the closing result comment that exists in README.
3. The guidance treated color rendering as canonical, even though README/NuGet/AI contexts depend on plain text first.
4. It referenced a nonexistent `samples/Order.precept` file as though the temporary hero already had a raw sample file.

For a reusable brand artifact, copy fidelity matters more than decorative rendering. The source of truth must match the live public surface exactly.

## What §2.6 now contains

- **Status framing:** approved for reuse now, still explicitly temporary
- **Exact canonical text:** verbatim DSL block (20 lines, 60-character max) and verbatim C# execution block
- **Cross-surface usage table:** README, VS Code Marketplace, NuGet, Claude Marketplace, AI prompt context
- **Portable constraints:** preserve exact wording, preserve blank lines, keep plaintext legibility primary, update README + brand spec together
- **Refresh triggers:** usability review, language/API drift, or deliberate promotion of a dedicated hero sample file
- **Source references:** README as live surface, §2.6 as reuse reference, explicit note that there is no standalone `samples/` file yet

## Team-relevant decision

Until the usability review says otherwise, the current README Order example is the temporary canonical hero sample across brand surfaces. Treat the exact text plus its reuse rules as locked; treat the domain itself as provisional.

## Impact

- **README:** No content change required; the live README already contains the approved temporary hero text.
- **Marketplace copy:** Teams can now copy from §2.6 without inventing surface-specific variations.
- **AI-facing docs/prompts:** Plaintext reuse is now explicitly blessed, which matches how agents actually consume the sample.

## Next steps

1. Merge this into `decisions.md` after review.
2. Use §2.6 as the source when updating README-adjacent marketplace copy.
3. Revisit only if usability testing or language/API changes force a new hero.

---

---

# Decision: README Rewrite Execution Path

**Author:** J. Peterman
**Date:** 2026-07-14
**Status:** Awaiting Shane sign-off on one gating decision

---

## Situation

The restructure proposal has been through three review passes (Frank, George, Uncle Leo — all approved with required changes). The revised proposal (2026-04-07) already incorporates every required change from all three reviewers. The proposal is ready to execute. No further review pass is needed.

## Gating Decision for Shane

**The only decision that blocks drafting §2 (Quick Example / hero) is: what is the hero domain?**

Three options the proposal has already pre-analyzed:
- **Order Fulfillment** — universally relatable; Elaine's preference
- **Subscription Billing** — 18 DSL statements; Peterman's original recommendation
- **Loan Application** — already exists in samples; complex but proven

The rest of the README (§0 title block, §1 hook, §3 getting started, §4 differentiation, §5 learn more, §6 contributing, §7 license) can be fully drafted before this decision is made. Hero domain only gates §2.

**Recommended path:** Shane names the hero domain, then the agent drafts §2 in the same pass as everything else and delivers a complete README ready for a single review pass.

## What Must Be Preserved

- Dictionary definition blockquote — `> **pre·​cept** *(noun)*: A general rule intended to regulate behavior or thought.` — confirmed in proposal §Hook
- C# API examples (trimmed: current is 45+ lines, target is 5 lines flat)
- Dev loop contributor table (the `What you changed / What to do / Window reload?` table in Current README § Local Development Loop) — good content, fits directly into the new Contributing section
- .NET 10 SDK prerequisite — exists in current README; must appear as a callout before Getting Started step 1
- License badge + MIT statement
- The `CreateInstance` overload pattern (current README shows it correctly; George confirmed both overloads are real)

## What Gets Cut

| Current section | Fate |
|---|---|
| `📚 Sample Catalog` (21-row table + feature matrix) | Remove from README; link to external docs in Learn More |
| `🛠️ World-Class Tooling` (150-word prose block) | Cut prose; distill to 4 bullets under §4c Live Editor Experience |
| `🧠 The Problem It Solves` | Absorbed into §1 Hook second sentence; the diagnosis lives there, not as a standalone section |
| `🏗️ The Pillars of Precept` (4 subsections) | Content folded into §4b Unified Domain Integrity bullets; no standalone section |
| `🤖 Designed for AI` (5 prose bullets) | Absorbed into §4a AI-Native Tooling; prose format replaced by bullets |
| `🤖 MCP Server` section (current standalone) | Folded into §4a AI-Native Tooling |
| Emoji in all heading prefixes (🚀 💡 📚 🛠️ 🤖 🧠 🏗️) | All removed — Elaine constraint #5: no emoji at heading start |
| Installation table + Copilot plugin steps in Getting Started | Plugin steps moved to §4a; Installation simplified to NuGet in Getting Started step 3 |
| `💡 The "Aha!" Moment` heading | Replaced by `## Quick Example` — descriptive, not clever |

## Draft Sequencing (Minimal Churn Path)

**Round 1 — No dependencies, draft immediately:**
1. §0 Title Block + Badges (logo placeholder, 3 badges, badge alt text)
2. §1 Hook (definition blockquote + 2-sentence positioning)
3. §3 Getting Started (template is fully spec'd in proposal)
4. §4 What Makes Precept Different — all three subsections (copy spec is detailed)
5. §5 Learn More (link list, external URLs TBD)
6. §6 Contributing (dev loop table + build commands, adapted from current README)
7. §7 License

**Round 2 — After Shane names the hero domain:**
1. §2 Quick Example — draft the 18-20 line DSL hero + 5-line C# execution block

**Round 3 — Shane single review pass:**
- Full README in proposed structure
- Check viewport compliance (≤60 char lines, above-the-fold test at 550px)
- Confirm hero domain/sample reads as real business logic, not toy demo
- Confirm badge values (NuGet version, build status URL)
- Confirm external doc links (placeholder during draft, real URLs before merge)

**Commit after Shane approves Round 3.**

## Required Changes from Reviews — Status Check

All required changes from Frank, George, and Uncle Leo are incorporated into the revised proposal (2026-04-07). No pre-rewrite patch pass needed.

| RC | Status |
|---|---|
| Frank RC-1 (C# API chain) | Fixed by George G1: CreateInstance overloads documented correctly, RestoreInstance removed |
| Frank RC-2 (Step 3 title) | Fixed: "Add the NuGet Package" throughout |
| Frank RC-3 (Section 4c name) | Fixed: "Live Editor Experience" |
| Frank RC-4 (AI claims) | Fixed: "AI agents can validate, inspect, and iterate..." |
| George G1 (RestoreInstance) | Fixed |
| George G2 (Prerequisites) | Fixed: .NET 10 SDK prerequisite in Getting Started template |
| George G3 ("replacing") | Fixed: "eliminating the fragmentation" |
| Uncle Leo RC-1 (H3 vs bold labels) | Fixed: Section map now shows `[bold lead-in: The Contract / The Execution]` |
| Uncle Leo RC-2 (context reminder) | Fixed: Explicit one-sentence context reminder in Getting Started template |

---

---

# Decision: README Restructure Proposal — Shane Feedback Revision

**Author:** J. Peterman
**Date:** 2026-04-07
**Source:** Shane's feedback pass on `brand/references/readme-restructure-proposal.md`
**Status:** Inbox — awaiting Shane sign-off

---

## Decisions Established by This Revision

### 1. "By treating your business constraints as unbreakable precepts" — Retained and Integrated

**Decision:** The phrase is not cut. It is folded as the mechanism half of the positioning sentence.

**Approved form:**
> **Precept is a domain integrity engine for .NET.** By treating your business constraints as unbreakable precepts, it binds state, data, and rules into a single executable contract where invalid states are structurally impossible.

**Rationale:** The phrase earns the brand name — it connects the dictionary definition to the mechanism in one beat. As a standalone third sentence it was redundant; as a participial lead-in to the positioning sentence it carries weight without bloating.

**Constraint:** The mechanism phrase must always appear as a participial fold into the positioning sentence. It is not a standalone tagline. It is not a separate sentence.

---

### 2. "Why This Approach" Content Stays Partially in the README

**Decision:** Not all philosophical/rationale content is deferred to docs. The core reasoning belongs in the README.

**What stays in README:**
- 1–2 sentences in `§ Unified Domain Integrity` explaining *why* co-location prevents disagreement between separately-managed rules
- The "prevention, not detection" framing

**What defers to docs:**
- Full pipeline mechanics (6-stage fire, working copy behavior)
- Construct catalog with full syntax reference
- Extended design rationale and comparison to alternative approaches

**Rule:** If a developer needs it to understand why Precept exists, it stays. If they need it to use Precept correctly, it goes to docs.

---

### 3. Contributing / Developer Loop — Retained at Bottom

**Decision:** The developer build loop (language server → extension auto-detect → edit → diagnostics) is explicitly retained in the README as a `## Contributing` section, positioned after `## Learn More` and before `## License`.

**This is not user onboarding.** The section is scoped to contributors. Users of Precept do not need to build the language server. Contributors do.

**Content minimum:** `dotnet build`, language server build command, `npm run compile` / `npm run loop:local`, `dotnet test`, link to CONTRIBUTING.md.

---

### 4. Three-CTA Problem — Definition Locked

**Decision:** "Three-CTA problem" is now a defined term in this project's README vocabulary. It means: three equal-weight next steps presented simultaneously at the same decision point, with no hierarchy. The problem is not the count — three CTAs remain in the restructured README. The problem is the simultaneous presentation.

**Canonical description (for use in any related document):**
> The current README's Getting Started presents VS Code extension, NuGet package, and Copilot plugin as three simultaneously equal next steps with no hierarchy. The problem is not having three CTAs — it is having three primary CTAs at the same decision point.

---

*Written to inbox by J. Peterman, 2026-04-07.*

---

### README Pass 1 — Structural Decisions Made During Rewrite

**Date:** 2026-04-08
**By:** J. Peterman
**Status:** COMPLETE — Shane awareness

---

#### Decision 1: Hero Domain — Order (Temporary)

**What:** Used a simplified Order domain (`order.precept`) as the pass-1 hero. Four states (New, Paid, Shipped, Delivered), one invariant, two event args, one guard, one reject, 20 lines, all lines ≤60 characters.

**Why:** Neither Time Machine nor LoanApplication fit the 60-char line constraint without either stripping personality (Time Machine) or structural complexity (LoanApplication). The Order domain is the cleanest compliant fit in accessible context. Shane's instruction "already-settled temporary sample" confirmed this is explicitly a placeholder.

**Status:** Temporary. The Phase 3 hero creative brief process (8 domain ideas, Duel at Dawn / Heist Safe ranked highest) remains open and should replace this in a future pass.

---

#### Decision 2: Hook Tagline — One Sentence

**What:** Reduced the proposal's two-sentence hook to one: "It binds state machines, validation, and business rules into a single executable contract — eliminating the fragmentation that comes from managing them separately."

**Why:** Shane's pass-1 tightening instruction + George's G3 (remove "replacing") + Uncle Leo's WC redundancy note. The "unbreakable precepts" phrase was cut for pass-1 compression; it can be reintroduced in pass 2 as a participial phrase if the team wants it back.

**Open:** Whether "unbreakable precepts" reconnects the brand name to the mechanism in the hook is a pass-2 copy decision. Not blocking.

---

#### Decision 3: Copilot Plugin CTA in AI-Native Tooling

**What:** Added one-sentence Copilot plugin install call to action in "AI-Native Tooling": "Install the plugin via `Chat: Install Plugin From Source` or enable it from the plugin marketplace."

**Why:** The plugin CTA needed to appear somewhere in the README (it was removed from Getting Started per the proposal). "AI-Native Tooling" is the correct adoption-stage location per the CTA strategy.

**Status:** Placeholder instruction. The exact install path may need updating when the plugin is marketplace-published.

---

#### Decision 4: Learn More links use relative doc paths

**What:** `Learn More` links point to `docs/PreceptLanguageDesign.md`, `samples/`, `docs/McpServerDesign.md`. `CONTRIBUTING.md` is referenced but does not exist yet.

**Why:** No published docs site URLs exist. Relative paths to existing repo docs are the best available option.

**Open:** Replace with absolute docs site URLs when the site ships. Create `CONTRIBUTING.md` to resolve the dangling reference.

---

---
date: 2026-04-08T00:00:00Z
author: j-peterman
status: ready_for_shane
phase: readme_copy_refinement
---

---

# README Copy Polish Pass — Decision Summary

**Decision:** Complete focused copy polish on README.md post-Elaine's structural design pass. Preserve all structural improvements. Tighten prose only for clarity, cadence, and brand voice.

## What Changed

| Line | Original | Tightened | Rationale |
|------|----------|-----------|-----------|
| 8 | "It binds state machines, validation, and business rules into a single executable contract — eliminating the fragmentation that comes from managing them separately." | "By treating business constraints as unbreakable precepts, it binds state machines, validation, and business rules into a single executable contract where invalid states are structurally impossible." | Integrates "unbreakable precepts" mechanism phrase (from design review) into main positioning sentence. Connects brand name to core idea. Removes vague "fragmentation" → uses concrete "structurally impossible" outcome. |
| 79 | "MCP server (5 tools), GitHub Copilot plugin, and a language server that gives AI agents structured access…" | "MCP server with 5 core tools, GitHub Copilot plugin, and language server give AI agents structured access…" | Removes parenthetical restatement. Fixes parallel structure ("give" → plural subject). Preserves all three components and tool count. |
| 81 | "Precept co-locates them: constraints live next to the state they govern." | "Precept unifies them into one definition." | Same meaning, 9 fewer words. "Unify" is more direct than "co-locate/constraints live next to." Clearer antecedent. |
| 84 | "One file, complete rules…" | "One file, all rules…" | "All rules" matches "together" grammatically. Slightly more precise. |
| 85 | "…without mutation" | "…without executing it" | Avoids misreading "mutation" as data structure mutation. Actual meaning: avoiding execution. More precise verb. |
| 88 | "Context-aware completions, semantic highlighting, inline diagnostics, and a live state diagram preview." | "Completions, semantic highlighting, inline diagnostics, and a live state diagram preview in VS Code." | Removes redundant "context-aware" (implicit in completions). Adds "in VS Code" (concrete location, serves mobile navigation and AI reader). |

## Design Decisions Preserved

- ✅ Elaine's structural hierarchy (Title → Hook → Quick Example → Getting Started → Differentiation → Learn More → License)
- ✅ Hero code sample (Order fulfillment business domain, 18-20 lines DSL, 5-line C# execution)
- ✅ CTA hierarchy (VS Code extension as primary, NuGet as secondary)
- ✅ All technical claims, links, and prerequisite language
- ✅ Brand positioning: "domain integrity engine" + mechanism phrase

## Key Phrase Decision

The phrase "treating business constraints as unbreakable precepts" was flagged in earlier drafts as potentially redundant. On review, it does distinct work: it connects brand name to mechanism in one moment, making the name feel earned rather than invented. Rather than cutting, integrated it as a participial phrase in the main positioning sentence.

## Files Modified

- `README.md` — 5 text edits, all prose tightening (no structural changes)
- `.squad/agents/j-peterman/history.md` — appended learning entry

## Status

Ready for Shane sign-off. README is near-signoff state: structure locked by Elaine, prose tightened for clarity and cadence, all approved messaging preserved.

## Validation

- ✅ Approved message intact ("domain integrity engine" + "unbreakable precepts" mechanism)
- ✅ Key phrase about business constraints preserved and integrated naturally
- ✅ Copy factual, aligned with prior reviewer corrections
- ✅ Brand voice consistent (authoritative, matter-of-fact, no hedging)
- ✅ No structural changes introduced
- ✅ No new hero domain introduced (Order sample retained)

---

---

# Decision: README Proposal Review Gap Pass — All Required Changes Applied

**Author:** J. Peterman
**Date:** 2026-04-07
**Status:** Applied — ready for Shane sign-off
**File modified:** `brand/references/readme-restructure-proposal.md`

---

## Summary

Seven required changes from the Frank/George/Uncle Leo review round were documented in the proposal's trim summary but had not been applied to the proposal body. All seven are now corrected.

---

## Changes Applied

| Reviewer | Item | Old | New |
|----------|------|-----|-----|
| Frank RC-4 | AI capability claim | "AI agents can author, validate, and debug `.precept` files without human intervention." | "AI agents can validate, inspect, and iterate on `.precept` files through structured tool APIs." |
| George G1 | Fabricated API name | `engine.CreateInstance(...)` (or `RestoreInstance`) | `engine.CreateInstance(savedState, savedData)` (or `engine.CreateInstance()` for new entity) |
| George G2 | Missing prerequisite | Getting Started steps with no .NET prereq | Explicit `.NET 10 SDK` prerequisite blockquote before step 1 |
| Uncle Leo RC-2 | Missing context reminder | "Here it's implicit in step 1's description..." (not actually present) | Explicit one-sentence context reminder as opening line of Getting Started template |
| Uncle Leo WC-1 | Badge-wall phrasing | "badge walls signal maintenance anxiety, not quality" | "additional badges add visual noise without adding signal at the awareness stage" |
| George G3 | Overclaiming hook | "It replaces three separate libraries..." | "It eliminates the fragmentation that comes from managing state, validation, and business rules separately." |
| Uncle Leo WC-3 | Unlabeled tagline | "One file. Every rule. Prove it in 30 seconds." (ambiguous: proposal copy or README copy?) | *(Proposed README tagline — confirm or substitute during rewrite):* One file. Every rule. Prove it in 30 seconds. |

---

## Decision for Shane

The proposal now accurately reflects the review feedback. No structural changes were required — all corrections were precision edits to content and copy specifications within the existing structure.

The proposal is ready for Shane's sign-off. The rewrite can begin.

---

## What Was Not Changed

- Frank RC-1/RC-2/RC-3 — already addressed in the prior Shane feedback pass
- Uncle Leo RC-1 (H3 vs bold lead-ins in section map) — already addressed in the prior pass
- Frank/George advisory notes (AN-1, AN-2, AN-3, Inspect args note, TransitionOutcome enum note) — non-blocking; addressed by the rewriter
- Uncle Leo WC-2 (Section 4a rationale sentence structure) — wording concern; non-blocking; addressable during rewrite

---

---

# Decision: README Restructure Trim Summary Added to Proposal

**Filed by:** J. Peterman
**Date:** 2026-04-06
**Status:** Proposed — pending Shane review

---

## Decision

A "What the Restructure Trims, Compresses, Defers, or Removes" section has been appended to `brand/references/readme-restructure-proposal.md`. This section is a permanent part of the proposal document — not a separate artifact.

## Rationale

Restructure proposals communicate gains well. They communicate costs poorly. Before a rewrite begins, Shane should be able to evaluate the restructure against what it removes, not just what it adds. The trim summary makes that cost explicit and scannable.

## What the Summary Covers

The trim summary is organized into four categories:

1. **Removed entirely** — content that does not survive in any form (third hook sentence, full C# boilerplate, badge walls)
2. **Compressed** — content that stays but shrinks (49-line hero → 18-20 lines + 5-line C#; 150-word "World-Class Tooling" → 4 bullets; three front-loaded differentiation sections → one post-trial section with bullets)
3. **Repositioned** — same content, lower prominence or different section (AI tooling CTA, Time Machine hero, differentiation-before-installation → differentiation-after-installation)
4. **Deferred to external docs** — content moved from README to linked references (sample catalog table, full quickstart walkthrough, language reference, MCP docs, philosophy)

A fifth category — **Net-New Content** — captures additions the restructure requires (prerequisite note, context reminder, language tags). This ensures Shane evaluates the full delta, not just what's cut.

## Files Changed

- `brand/references/readme-restructure-proposal.md` — trim summary section appended

## Team Impact

The trim summary is a reference artifact for whoever executes the rewrite. It functions as a checklist: if any item in the "Removed Entirely" list appears in the rewritten README, something went wrong. If any item in "Deferred to External Docs" appears as embedded content (not a link), that is also a regression.

---

---

# Decision: README Collaboration Order — Shape-First vs. Draft-First

**Filed by:** J. Peterman
**Date:** 2026-04-08
**Status:** Recommendation — Pending Shane sign-off
**Context:** Shane asked whether Elaine should produce a form/shape skeleton before Peterman writes the README copy.

---

## Recommendation

**Skip the skeleton pass. Peterman drafts first. Elaine audits after.**

The restructure proposal (`brand/references/readme-restructure-proposal.md`) already IS the skeleton. It defines:
- Section order and heading levels
- 16 hard constraints with explicit source attribution
- Per-section content guidance with placeholder examples
- Viewport requirements, CTA hierarchy, progressive disclosure order

An Elaine skeleton pass would translate that proposal into a blank document structure — a mechanical step that reproduces work already done.

---

## The Shape Is Already Fixed

The proposal documents form at the same precision Elaine would produce in a skeleton:

| Form element | Already in proposal |
|---|---|
| Section order | Yes — explicit recommended order with rationale |
| H1/H2/H3 hierarchy | Yes — Constraint #4, per-section heading levels specified |
| Mobile-first above-the-fold | Yes — Constraint #1, 550px requirement, explicit test instruction |
| CTA structure | Yes — Constraint #3, primary vs. secondary subordination |
| Code block format | Yes — Constraints #8/#9, 60-char limit, 5-line C# block |
| Visual separators | Yes — Constraint #7, `---` between major sections |

---

## Why Skeleton-First Adds an Extra Pass Without Adding Value

Collaboration order if Elaine goes first:
1. Peterman proposal (done)
2. Elaine skeleton (translates proposal to blank doc)
3. Peterman draft (fills skeleton with copy)
4. Elaine constraint audit (checks 16 hard constraints)

Total: 4 passes.

Collaboration order if Peterman goes first:
1. Peterman proposal (done)
2. Peterman draft (writes against the proposal directly)
3. Elaine constraint audit (checks 16 hard constraints)

Total: 3 passes. Same quality gate. One less round trip.

---

## The Risk of Skeleton-First

The skeleton pass creates a new artifact that Peterman must interpret. Any ambiguity in placeholder structure — heading text, container depth, code block labels — becomes a negotiation surface that didn't need to exist. The proposal already has this precision. Two sources of structural truth is one too many.

---

## The One Exception

**Hero code block.** The 60-character line constraint (#8) shapes what DSL sample Peterman can write — short field names, abbreviated rule copy. Before Peterman finalizes the hero DSL, Elaine should confirm which sample domain (Subscription Billing, Order Fulfillment, Loan Application) fits cleanest in a narrow viewport. This is a single targeted question, not a skeleton pass.

---

## Correct Gate Sequence for README Rewrite

1. **Peterman** — drafts full README against the restructure proposal
2. **Elaine** — constraint audit pass: 16-row pass/fail table, no copy edits
3. **Peterman** — resolves any constraint failures
4. **Frank** — optional architecture review (contributing section accuracy)
5. **Shane** — final sign-off

This matches the existing `constraint-holder-review-gate` skill pattern documented in `.squad/skills/`.

---

## What This Decision Is Not

This is not a ruling against Elaine's involvement. Elaine's structural requirements already govern this rewrite. The question is sequencing. Her form authority is exercised through the post-draft constraint audit, not through a pre-draft skeleton — because the skeleton already exists in the proposal.

---

---

# Decision: Signal Color Family Names

**Date:** 2026-04-07
**Agent:** j-peterman
**Status:** Applied — no new decision required

## Summary

The three semantic signal colors in the Precept color system have proper family names. They were already present in the spec in isolated sections but had not been applied consistently to the signal color definitions.

| Plain name | Family name | Hex |
|---|---|---|
| Green | Emerald | `#34D399` |
| Yellow | Amber | `#FCD34D` |
| Red | Rose | `#FB7185` |

## What was done

Updated `brand/brand-spec.html` to use the family names (Emerald, Amber, Rose) consistently everywhere the signal colors are named — swatch labels, intro paragraphs, cross-surface application table, syntax editor notes, state diagram intro, and README surface table.

## Implication for team

All future references to these colors — in docs, copy, surface specs, and team communications — should use **Emerald**, **Amber**, and **Rose**. Plain "green/yellow/red" is now retired from brand vocabulary.

---

---

# Decision: spm-* layout blocks must not add horizontal padding when parent spm-group already provides it

**Date:** 2026-04-04
**Author:** J. Peterman
**Status:** Resolved

## Context

The `spm-surface` component system (used in §2.1 and §2.2) has a three-level container hierarchy:

```
spm-surface
  spm-group        ← owns horizontal indent: padding: 16px 24px
    spm-header     ← section sub-header (e.g. "Structure · Indigo")
    spm-grid       ← swatch row container
      spm-row      ← individual token rows
        spm-swatch ← the colored block
```

The `sf-group` system (used in §1.4 and Reserved Verdict Colors) has a simpler hierarchy:

```
sf-group           ← owns horizontal indent: padding: 20px 24px
  sf-row           ← padding: 10px 0 (no horizontal)
    sf-swatch      ← the colored block
```

## Problem

Both `spm-header` (CSS class, `padding: 18px 24px`) and `spm-grid` (inline `style="padding: 16px 24px;"`) were adding their own 24px horizontal padding *in addition to* the 24px from the parent `spm-group`. This caused swatch content to render at 48px from the surface edge instead of 24px — a 24px misalignment vs. sf-swatch in the same card.

## Decision

**Rule:** In the `spm-*` component system, horizontal padding is the exclusive responsibility of the parent container (`spm-group`). Child layout blocks (`spm-header`, `spm-grid`) must use `padding: Npx 0` — vertical spacing only.

## Change Made

- `.spm-header { padding: 18px 0 }` (was `18px 24px`)
- All 6 `spm-grid` inline styles: `style="padding: 16px 0;"` (was `16px 24px`)

## Applies To

Any future `spm-*` sections (§2.3 Inspector, §2.4 Docs Site, §2.5 CLI/Terminal) must follow this rule when adding new `spm-header` or `spm-grid` blocks inside `spm-group`.

---

---

# Decision: README Ship Plan — Shortest Safe Path to Published README

**Author:** Steinbrenner (PM)
**Date:** 2026-05-01
**Status:** Recommendation — awaiting Shane sign-off
**Input artifacts:** readme-restructure-proposal.md (revised 2026-04-07), reviews from Frank, George, Uncle Leo, and j-peterman-readme-review-gap-pass.md

---

## Current State

The proposal is ready. All 7 required changes from the Frank/George/Uncle Leo review round have been applied to the proposal body (verified via `j-peterman-readme-review-gap-pass.md`). The reviewed issues that remain advisory/non-blocking are explicitly tracked and addressable by the rewriter without Shane involvement.

**The single remaining blocker is Shane's sign-off.**

---

## Gate 1 — Shane Decisions (Required Before Rewrite Begins)

These are the only four things that require Shane before the rewriter can start. None of them require a doc; they can be resolved in a single conversation.

| # | Decision | Options | Default if Shane passes |
|---|----------|---------|------------------------|
| G1 | **Approve the proposal** — structural, CTA hierarchy, section order | Approve / Approve with changes / Reject | Blocked |
| G2 | **Hero domain** — which sample domain goes in Quick Example | Subscription (team recommendation), TimeMachine (Shane prior approval), other | Blocked — no default |
| G3 | **Tagline** — confirm or substitute the proposed closing tagline | "One file. Every rule. Prove it in 30 seconds." or substitute | Rewriter picks during draft |
| G4 | **Logo/SVG** — is a brand mark SVG ready, or should title block use text wordmark only? | SVG at 48px / text wordmark placeholder | Text wordmark — rewriter proceeds, logo swaps in later without structural change |

G1 is the formal gate. G2 is the highest-churn risk — if the hero domain is not locked before the draft, the rewriter will make a call and may have to redo it. G3 and G4 can slip into the draft without blocking.

---

## In-Flight Decisions (Rewriter Resolves Without Shane)

These items are non-blocking per the reviews but must not be dropped silently:

| Item | Decision owner | Guidance |
|------|---------------|---------|
| Section 4 heading: "What Makes Precept Different" vs "What Precept Does" | J. Peterman | Leo's AN-2 flagged this as a copywriting call. Either is acceptable. "What Precept Does" is more category-creation-consistent; "What Makes Precept Different" is what developers scan for. |
| Section 4a rationale sentence structure (AI tooling "lead with / don't bury" conflation) | J. Peterman | Leo WC-2: split into two sentences per Leo's suggested rewrite. No Shane input needed. |
| `engine.Inspect` args footnote | George | If hero uses an event with required args, add a comment clarifying partial inspection behavior. George advises. |
| Context reminder wording (Getting Started opening line) | J. Peterman + Uncle Leo | The structure is locked ("Precept is a domain integrity engine for .NET. Install..."). The exact prose is Peterman's call; Leo reviews. |
| Badge alt text values (version/status) | J. Peterman | Pull current NuGet version and build status values at draft time. Constraint #14. |

---

## Explicitly Out of Scope — First Rewrite Pass

These items must not block or delay the first README rewrite. If they come up during drafting, defer them.

| Out of scope | Why |
|---|---|
| Color/palette application within the README document | Elaine's palette/usage pass for the README surface has not landed. Use shields.io defaults for badges; no custom README styling in this pass. |
| New DSL language features | Named guards, ternary in `set`, `string.length` — all in the research proposal pipeline, none implemented. The README must describe what exists. |
| CLI section | CLI design exists, implementation deferred. Do not add CLI content to the README until implemented. |
| Comparison table ("Precept vs. Stateless vs. FluentValidation") | Implicit differentiation strategy is locked. No comparison table. |
| Full pipeline mechanics | 6-stage fire, working copy behavior, TransitionOutcome enum values → deferred to docs. The README links out. |
| Sample catalog table | Per Elaine constraint #15: removed from main README, linked externally. |
| New screenshots or state diagram images | If no current screenshot is available, use a descriptive alt text placeholder. Do not delay the draft for new assets. |

---

## Execution Path After Gate 1 Clears

This is a one-shot draft → parallel review → ship sequence. No iteration loops.

```
1. J. Peterman writes README draft (one pass, targeting all 16 Elaine constraints)
2. Frank + George co-review in parallel (Frank: structure/narrative; George: API accuracy + runtime claims)
3. Uncle Leo editorial pass (copy, clarity, redundancy)
4. Scribe validates constraint checklist (16 Elaine constraints, explicit check)
5. Shane final read — no surprises expected if gates cleared upfront
6. Ship
```

**Estimated elapsed time after Gate 1 clears:** 1–2 sessions (draft + review). The proposal is highly specified — the rewriter does not need to invent structure, section order, CTA hierarchy, or C# API calls. Those are all locked.

**Churn risk:** Hero domain is the highest risk factor. A domain selected without conviction tends to get revised. Lock it in Gate 1.

---

## Appendix: What "Approved" Means for the Proposal

The proposal (revised 2026-04-07) includes:
- Recommended section order (finalized)
- All 16 Elaine hard constraints (listed)
- Section-by-section rationale with copy templates
- Hero treatment (DSL block spec + C# block spec with correct API surface)
- CTA hierarchy (primary/secondary/tertiary)
- Trim summary (what is removed, compressed, repositioned, deferred)
- Dual-audience (human/AI) structural validation table

A rewriter working from this proposal needs one thing: the hero sample. Everything else is already specified.

---

### 2026-04-04T23:00:50Z: User directive
**By:** shane (via Copilot)
**What:** Use the top-rated hero snippet from the visual-language exploration as the temporary live hero until the final hero question is settled later.
**Why:** User request — captured for team memory


---

---

# Decision Inbox Merge — 2026-04-05T02:00:30Z

**Merged by:** Scribe
**Source:** .squad/decisions/inbox/
---

### 2026-04-04T23:22:00Z: User directive
**By:** shane (via Copilot)
**What:** For the README hero snippet, visual impact matters more than copyability; it is a hero artifact, not a practical code sample.
**Why:** User request — captured for team memory


---

### 2026-04-04T23:28:11Z: User directive
**By:** shane (via Copilot)
**What:** Do not show a raw Precept code block in the README hero; use the SVG route for the hero treatment instead.
**Why:** User request — captured for team memory


---

### 2026-04-05T01:10:02Z: User directive
**By:** shane (via Copilot)
**What:** Also include Frank as the architect on the SVG hero effort.
**Why:** User request — captured for team memory


---

### 2026-04-05T01:13:13Z: User directive
**By:** shane (via Copilot)
**What:** Frank should lead on architecture for the SVG hero effort.
**Why:** User request — captured for team memory


---

### 2026-04-05T01:15:32Z: User directive
**By:** shane (via Copilot)
**What:** Do not use Haiku. Use non-Haiku models for squad work.
**Why:** User request — captured for team memory


---

### 2026-04-05T01:25:49Z: User directive
**By:** shane (via Copilot)
**What:** Always use the latest version of models; do not route work to older model versions like gpt-4.1 by default.
**Why:** User request — captured for team memory


---

---

# Design Spec: README Hero SVG — "The Contract That Says No"

**Filed by:** Elaine (UX Designer)
**Date:** 2026-07-17
**Status:** DRAFT — awaiting Shane review
**Issue:** #4 — Replace README hero code block with branded SVG visual
**Asset:** `brand/readme-hero.svg`

---

## Concept

The hero SVG stages the Subscription Billing lifecycle as a visual narrative. The reader's eye follows a happy-path state flow left to right — Trial → Active → Cancelled — then drops below Cancelled to find the punchline: a rejected reactivation attempt, stopped by a Rose X mark, with the Gold rejection message as the final line.

**One sentence:** The hero shows a system that works perfectly — including the moment it refuses.

The comedy (and the product thesis) lives in the structural irony: a precise, orderly flow ends in a blunt refusal that the system treats as unremarkable. The reader sees that Precept doesn't check whether reactivation is allowed — it structurally can't express it.

---

## Composition

Four horizontal zones on a dark canvas:

| Zone | Content | Purpose |
|------|---------|---------|
| **Title** (top) | `precept Subscription` in syntax-like coloring | Establishes this is a precept definition |
| **Flow** (middle) | Trial → Active → Cancelled with event labels | The happy-path lifecycle |
| **Rejection** (below Cancelled) | Dashed Rose line → circled X → Gold message | The punchline — structural impossibility |
| **Tagline** (bottom) | "Invalid states are structurally impossible." | Product thesis, quiet and factual |

The Activate event appears three times — Trial→Active transition, Active self-loop (price update), and the rejected Cancelled path. One event, three states, three outcomes. That IS the product thesis in one image.

### State Node Shapes (per §2.2)

- **Trial** — Circle with thick Indigo border (#4338CA, 2.5px), small filled dot (#6366F1) for initial state indicator
- **Active** — Rounded rect, Indigo border (#4338CA, 1.5px)
- **Cancelled** — Double-border rect, Indigo (#6366F1, inner 2px + outer 1px at 0.3 opacity)

### Edges

- **Happy-path transitions** — Emerald (#34D399) solid lines with arrow markers
- **Self-loop on Active** — Emerald arc above node (price update without state change)
- **Rejected path** — Rose (#FB7185) dashed line descending from Cancelled to a circled X

### Text

- **State names** — Violet (#A898F5), monospace
- **Event labels** — Cyan (#30B8E8), monospace
- **Rejection event label** — Cyan at reduced opacity (0.6) — dimmer to signal it's the failed attempt
- **Rejection message** — Gold (#FBBF24) at 0.85 opacity — the human-readable rule text
- **Tagline** — Muted (#52525b) — present but not competing

---

## Color Mapping

Every color traces to brand-spec §1.4 semantic families:

| Element | Color | Hex | Semantic lane |
|---------|-------|-----|---------------|
| Canvas | Near-black | #0c0c0f | Background (brand standard) |
| Canvas border | Deep Indigo | #1e1b4b | Ground tone (brand mark family) |
| State node borders | Semantic Indigo | #4338CA | Structure |
| Terminal outer glow | Indigo | #6366F1 | Grammar-level structure |
| State name text | Violet | #A898F5 | State identity |
| Event label text | Cyan | #30B8E8 | Transition verbs |
| Flow arrows | Emerald | #34D399 | Allowed/success signal |
| Rejected path | Rose | #FB7185 | Blocked/error signal |
| Rejection message | Gold | #FBBF24 | Human-readable rule text |
| Title keyword | Indigo Accent | #818cf8 | Syntax keyword |
| Tagline | Muted | #52525b | Support text |

---

## Typography

Font stack: `'Cascadia Code', 'Fira Code', 'JetBrains Mono', 'Consolas', monospace`

GitHub renders SVGs server-side with limited font availability. The design degrades gracefully to any monospace font — colors and shapes carry the brand, not exact typeface rendering. If font fidelity becomes critical, key text can be converted to paths in a future polish pass.

---

## GitHub SVG Constraints

- Static SVG, no `<script>`, no `<foreignObject>`, no external resources
- Inline attributes only (no `<style>` block for maximum compatibility)
- All text as `<text>` elements (not paths — keeps the file editable and small)
- `viewBox` with fixed dimensions for consistent aspect ratio
- Tested target: renders correctly as `<img>` tag in GitHub Markdown

---

## README Structure Change

**Current:**
```

---

# Precept
badges + definition + tagline
---
## Quick Example  ← hero area (raw code blocks)
```

**Proposed:**
```

---

# Precept
badges + definition + tagline
---
![Precept — Subscription lifecycle](brand/readme-hero.svg)
---
## Quick Example  ← raw code blocks preserved below hero
```

The SVG becomes the visual-first hero treatment. The raw Precept + C# code blocks remain in "Quick Example" for teaching. The "Temporary hero sample" note is removed since the SVG is now the intentional hero, not a placeholder.

---

## Open Questions for Shane

### 1. Rose in the README

Brand-spec §1.4.1 says: "Do not use Rose in README marketing surfaces." The hero SVG IS in the README, but it's depicting a product diagram — the blocked path is the same visual language as §2.2 State Diagram, not a marketing decoration.

**My recommendation:** Rose is appropriate here. The hero is a product diagram embedded in a marketing context, not marketing styled with error colors. The rejection is the punchline — removing Rose would gut the visual narrative.

**If Shane disagrees:** The rejection could use a muted treatment (dimmed Indigo + text-only message) instead of Rose. It loses punch but stays within strict README color rules.

### 2. Tagline text

Current draft: "Invalid states are structurally impossible." — the product thesis verbatim.

**Alternative:** No tagline (let the image speak). Or a softer line like "A domain integrity engine for .NET" (repeats the existing subtitle).

**My recommendation:** Keep the thesis. It's the one sentence that makes the image click. Muted color (#52525b) keeps it subordinate.

### 3. Supporting copy in README Markdown

The issue suggests "one or two lines reinforcing the product thesis" between the image and Quick Example. Do we need this, or does the existing tagline in the README header + the image's embedded tagline cover it?

**My recommendation:** No additional copy between the image and Quick Example. The README header already has the definition and tagline. Adding more text between hero and code breaks the visual rhythm.

---

## Alternatives Considered

### A. Split-panel layout (code + diagram side by side)
Rejected. Two focal points compete for attention. Doesn't work on narrow viewports. The hero needs ONE story, not two panels.

### B. Styled code block rendering (syntax-colored SVG of the DSL text)
Rejected. This is just a fancier code block — it doesn't change the hero from "code listing" to "visual showcase." It also creates a maintenance burden (SVG must match DSL text exactly).

### C. Abstract brand mark blown up to hero scale
Rejected. The brand mark works at icon scale because it's symbolic. At hero scale, an abstract mark feels empty — there's no content, no story, no comprehension anchor.

### D. Full state diagram with all DSL features annotated
Rejected. Too dense. A hero is one idea, one glance, one feeling. The teaching happens in the code block below.

---

## What Shane Is Approving

Not pixels. Not final SVG polish. This is a concept lock:

1. **Message:** The hero shows governed state flow plus a blocked invalid path — the product thesis made visual
2. **Hierarchy:** SVG hero → raw code example → Getting Started (visual first, teaching second)
3. **Narrative device:** One event (Activate), three states, three outcomes — the third is structurally impossible
4. **Visual language:** Brand-spec §2.2 diagram vocabulary applied to a README hero context
5. **Concept:** Subscription Billing remains the temporary live domain

Once these are settled, the SVG can be refined without reopening narrative strategy.

---

## Next Steps After Approval

1. Peterman reviews for brand compliance
2. Refinement pass: tighten spacing, test font rendering on GitHub, verify mobile viewport behavior
3. Frank reviews for architectural fit (should be trivial — it's a static asset)
4. README wiring: insert SVG, remove "temporary" note, verify Quick Example still works
5. Brand-spec §2.6 update to reflect the new hero treatment
6. Shane final sign-off on the implemented README


---

---

# SVG Hero Proposal — Design Spec (Issue #4)

**Filed by:** Elaine (UX Designer)
**Date:** 2026-07-17
**Status:** PROPOSAL — awaiting Shane review
**Relates to:** GitHub issue #4

---

## 1. Recommended Concept: "The Contract That Says No"

A single dark-surface SVG panel (~800×360px) that stages the Subscription lifecycle as a **visual micro-narrative** — the happy path flows elegantly left-to-right, then the attempted reactivation from Cancelled gets definitively bounced. The rejection moment IS the punchline and IS the product thesis.

### Why this works as a hero

- **Two-second comprehension.** A developer sees three nodes, two green arrows, one red dashed arrow that doesn't connect. They understand "this system prevents bad transitions" before reading a single word.
- **The product thesis is the visual focal point.** "Invalid states are structurally impossible" isn't a tagline underneath — it's the thing your eye lands on. The Rose dashed arrow terminating at a ✕ is the most visually distinct element in the composition.
- **Cute without being corny.** The humor is structural, not illustrated. There's no mascot, no cartoon, no winking emoji. The comedy is the _contrast_ — three states flowing beautifully, and then one hilariously definitive "nah." The system has a voice (via the Gold rejection callout), and that voice is dry and matter-of-fact. Developer humor lives in the gap between elegant machinery and blunt refusal.
- **Brand-native.** Every color, shape, and label uses the locked brand vocabulary exactly as specified in `brand-spec.html` §1.3 and §2.2. No new visual concepts. The hero IS the brand.
- **GitHub-safe.** Static SVG, no scripts, no animation, no external fonts required (fallback to system monospace). Renders identically on light and dark GitHub themes against the self-contained dark canvas.

---

## 2. Composition / Layout

### Overall structure

```
┌─────────────────────────────────────────────────────────┐
│  (dark canvas #0c0c0f, 1px #1e1b4b border, 8px radius) │
│                                                          │
│  ┌─ top-left: context label ─────────────────────────┐  │
│  │  "precept Subscription"  (brand-light #818CF8)    │  │
│  └───────────────────────────────────────────────────┘  │
│                                                          │
│  ┌─ center stage: state flow ────────────────────────┐  │
│  │                                                    │  │
│  │  (Trial)  ──Activate──▶  [Active]  ──Cancel──▶ ║Cancelled║ │
│  │   ○ initial    emerald    □ intermediate  emerald  ╬ terminal │
│  │                                                    │  │
│  └───────────────────────────────────────────────────┘  │
│                                                          │
│  ┌─ focal moment: the rejection ─────────────────────┐  │
│  │                                                    │  │
│  │  ║Cancelled║ ╌╌╌ Activate ╌╌╌✕                    │  │
│  │    rose dashed arrow, terminates at ✕              │  │
│  │                                                    │  │
│  │    ┌ speech callout (gold accent) ──────────┐     │  │
│  │    │ "Cancelled subscriptions cannot         │     │  │
│  │    │  be reactivated"                        │     │  │
│  │    └─────────────────────────────────────────┘     │  │
│  └───────────────────────────────────────────────────┘  │
│                                                          │
│  ┌─ bottom: tagline ─────────────────────────────────┐  │
│  │  Invalid states are structurally impossible.       │  │
│  │  (text-secondary #A1A1AA, small, centered)         │  │
│  └───────────────────────────────────────────────────┘  │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### Spatial budget

| Zone | Vertical % | Purpose |
|------|-----------|---------|
| Context label | ~10% | Establishes this is a precept, not generic diagram |
| State flow row | ~35% | The happy path — three nodes, two Emerald arrows |
| Rejection moment | ~40% | The punchline — Rose dashed arrow + Gold callout |
| Tagline | ~15% | Reinforces thesis without competing with diagram |

### Key layout principles

- **Left-to-right narrative.** Trial sits at left, Cancelled at right. The happy path reads naturally. The rejected arrow curves back rightward, making the "going backward" attempt visible.
- **Rejection gets the most space.** The callout/bubble is the largest text element. It earns this space because it IS the product message.
- **No symmetry.** The happy path is tidy; the rejection moment is deliberately more expressive. This asymmetry creates visual interest and hierarchy.
- **Vertical breathing room.** No element should feel cramped. The dark canvas is a feature, not wasted space.

---

## 3. Visual Language

### State nodes (from §2.2 shape vocabulary)

| State | Shape | Border | Label color |
|-------|-------|--------|-------------|
| Trial | Circle, r≈26 | #4338CA 2.5px (initial) | #A898F5 (Violet) |
| Active | Rounded rect, rx=6 | #4338CA 1.5px (intermediate) | #A898F5 |
| Cancelled | Double-border rect | #6366F1 inner 2px + outer 1px @30% opacity | #A898F5 |

### Arrows and edges

| Path | Style | Color | Label color |
|------|-------|-------|-------------|
| Trial → Active | Solid, 1.5px, filled arrowhead | #34D399 (Emerald) | #30B8E8 (Cyan) "Activate" |
| Active → Cancelled | Solid, 1.5px, filled arrowhead | #34D399 (Emerald) | #30B8E8 (Cyan) "Cancel" |
| Cancelled → ✕ (rejected) | Dashed 3,2 pattern, 1.5px, no arrowhead — terminates at ✕ | #FB7185 (Rose) | #30B8E8 (Cyan) "Activate" (dimmed) |

### The rejection callout

- **Shape:** Rounded rectangle with a small triangular notch pointing toward the ✕ termination point. Like a speech bubble, but geometric and restrained.
- **Border:** #FB7185 (Rose) at 40% opacity, 1px — present but not loud.
- **Background:** #0c0c0f (same as canvas) or very subtle Rose tint at ~5% opacity.
- **Text:** #FBBF24 (Gold), the locked `because`/`reject` accent color. This is its correct semantic use — a human-readable rule message.
- **Content:** `"Cancelled subscriptions cannot be reactivated"`
- **Font:** Cascadia Cove monospace (with system mono fallback), ~11px, normal weight. Italic optional for the quoted string.

### Labels and tone

The hero uses **exactly two text tones**:
1. **Diagram labels** — state names in Violet, event names in Cyan, following §2.2 exactly.
2. **Narrative text** — the context label ("precept Subscription") in brand-light, the rejection message in Gold, the tagline in text-secondary.

No body copy. No explanatory paragraphs inside the SVG. The diagram tells the story; the callout delivers the punchline; the tagline names the principle.

### How "cute/funny" enters without becoming corny

The humor is **structural irony**, not decoration:

1. **Contrast comedy.** The happy path is elegant — clean Emerald arrows flowing left to right. The rejected attempt is visually messy — dashed Rose line, a ✕, a speech bubble. The system's _perfection_ in refusing is funny because it contrasts with the messy attempt.
2. **The system has a voice.** The Gold callout reads like the system is speaking directly: "Cancelled subscriptions cannot be reactivated." It's not a tooltip or annotation — it's the contract talking. Dry, definitive, slightly smug. Developer humor.
3. **The ✕ is the punchline.** A small Rose ✕ where the arrow terminates. Not a big red X, not a stop sign. Just a small, precise mark that says "this path does not exist." The understated delivery IS the joke.
4. **No mascots, no emojis, no winks.** The warmth comes from the rejection message being in Gold — the warmest color in the palette. The system is firm but not hostile. "I'm not angry, I'm just right."

---

## 4. Relationship to Supporting Copy and README Structure

### Hero → Copy bridge

The SVG hero replaces the current `## Quick Example` hero block. Below the hero image, the README should have:

```markdown
> **precept** *(noun)*: A general rule intended to regulate behavior or thought.

**Precept is a domain integrity engine for .NET.** By treating business constraints
as unbreakable precepts, it binds state machines, validation, and business rules
into a single executable contract where invalid states are structurally impossible.
```

The tagline inside the SVG ("Invalid states are structurally impossible") echoes directly into this copy. The reader sees the principle visually, then reads it in prose. Reinforcement without repetition.

### Hero → Practical example relationship

The raw Precept code block and C# execution example move to a **later section** (e.g., `## Quick Example` or `## How It Works`), not adjacent to the hero. The structural separation is:

| Section | Role | Contains raw code? |
|---------|------|--------------------|
| Hero (top) | Visual recognition, emotional hook | No — SVG only |
| Product copy | Thesis reinforcement | No |
| Quick Example (later) | Practical teaching | Yes — full DSL + C# |

The hero **earns the scroll** to the example. The example **proves the promise** the hero made visually. They're partners, not redundant.

### Section ordering (proposed)

1. Badges
2. SVG Hero image
3. One-liner definition + tagline paragraph
4. `---`
5. Quick Example (relocated DSL + C# blocks)
6. Getting Started
7. What Makes Precept Different
8. Learn More
9. Contributing
10. License

---

## 5. Alternatives Considered

### Alt A: "The Rule Tablet" — enlarging the brand mark to hero scale

Concept: Blow up the combined brand mark (tablet + state machine icon from §1.3) to a wide hero format. Left half shows stylized code lines (not readable DSL, just branded line fragments suggesting structure). Right half shows the state diagram emerging from the tablet, as if the rules generate the machine.

**Why it loses:**
- Too abstract. A developer sees colored lines and shapes but can't immediately answer "what does this product do?" The concept requires interpretation — the hero should require none.
- The brand mark works at icon scale because it's symbolic. At hero scale, the same abstraction feels empty. You need content at hero scale.
- The cute/funny dimension has nowhere to land. Abstract shapes aren't funny.

### Alt B: "Side-by-Side Panels" — what you write vs. what happens

Concept: Split the hero into two panels. Left panel: a stylized rendering of the DSL (not raw code, but visually distinguished fragments — field declarations, state list, a transition line). Right panel: the state diagram. Visual connection between them (dotted lines from code → diagram elements, or a shared background gradient).

**Why it loses:**
- Still feels like documentation, not a hero. Side-by-side explanation is a teaching layout, not a showcase layout.
- The left panel is uncomfortably close to "raw code block" even if it's styled. The issue explicitly says no raw Precept block in the hero.
- Splits attention. A hero needs one focal point, not two competing panels.
- The cute/funny moment doesn't have a natural home — you'd have to force it into a label.

### Alt C: "Three-Panel Story" — comic-inspired triptych

Concept: Three panels reading left to right like a comic strip. Panel 1: "The Contract" — a small tablet icon with "precept Subscription" label. Panel 2: "The Flow" — Trial→Active→Cancelled happy path. Panel 3: "The Refusal" — the Cancelled→Active rejection with callout.

**Why it's close but still loses:**
- The triptych framing adds visual structure that competes with the content. Borders between panels, panel numbering, visual gutters — it's a lot of chrome for a simple story.
- The three-panel layout implies sequence/time, which over-formalizes what should feel like a single glance.
- At ~800px width, each panel gets ~250px — tight for readable diagram content.
- The recommended concept tells the same story without the panel overhead. The spatial zones (flow row → rejection row) create the narrative sequence without explicit framing.

### Why "The Contract That Says No" wins

It has the highest signal-to-chrome ratio. Three nodes, two arrows, one blocked arrow, one callout. Every element earns its space. The story reads in one glance. The punchline (the rejection) gets the most visual weight. And the cute/funny dimension emerges naturally from the composition itself — the contrast between elegant flow and definitive refusal — without any added illustration or decoration.

---

## Implementation Notes (for handoff to Frank/Kramer)

- **Canvas:** `<svg viewBox="0 0 800 360">` with `<rect>` fill #0c0c0f, 1px #1e1b4b stroke, rx=8
- **Fonts:** Embed subset of Cascadia Cove if GitHub allows, or specify `font-family="Cascadia Code,Cascadia Mono,Consolas,monospace"` — GitHub SVG renders system fonts
- **GitHub SVG constraints:** No `<foreignObject>`, no `<script>`, no external stylesheets, no `<use>` referencing external files. All styles must be inline or in `<defs><style>`.
- **Dark/light mode:** The self-contained dark canvas means the SVG looks identical regardless of GitHub's page theme. This is intentional — the brand surface IS dark.
- **File location:** `brand/hero.svg` (source of truth) with a copy or symlink wherever README references it.
- **README wiring:** `<p align="center"><img src="brand/hero.svg" alt="Precept — a domain integrity engine for .NET. State diagram showing Trial → Active → Cancelled subscription lifecycle with a rejected reactivation attempt." width="800"></p>`

---

## Open Questions for Shane

1. **Tagline inside SVG vs. outside?** Proposal includes "Invalid states are structurally impossible." inside the SVG. Alternative: leave it out of the SVG and let the README copy carry it. Inside the SVG makes the image self-contained; outside keeps the SVG purely diagrammatic.
2. **Rejection message wording.** The current DSL uses `"Cancelled subscriptions cannot be reactivated"`. This is good — clear, direct, slightly formal. If we want more warmth for the hero moment, options include keeping it exactly as-is (recommended — the formality IS the humor) or adjusting slightly.
3. **"precept Subscription" label.** Should the top-left context label be present? It hints at the DSL without showing raw code. Alternative: omit it and let the diagram stand alone.


---

### SVG Hero Architecture Proposal — Issue #4

**Date:** 2026-04-08
**By:** Frank (Lead/Architect)
**Status:** PROPOSAL — awaiting Shane review + Elaine/Peterman design pass
**Issue:** [#4 — Replace README hero code block with branded SVG visual](https://github.com/sfalik/Precept/issues/4)

---

## 1. Recommended Asset/Workflow Architecture

### Asset path and format

| Item | Decision |
|------|----------|
| **Format** | Static inline SVG, no `<image>`, `<foreignObject>`, `<script>`, or external references |
| **Repo path** | `brand/readme-hero.svg` — single source-of-truth file, version-controlled alongside brand-spec |
| **README integration** | `<picture>` with `<source media="(prefers-color-scheme: dark)">` + `<img>` fallback referencing the same SVG via relative path |
| **Viewport** | Fixed `width`/`height` attributes (recommended 800×280) plus `viewBox` — GitHub strips `width`/`height` expressed as percentages |
| **Content** | Subscription Billing lifecycle flow: `Trial → Active → Cancelled`, with reject moment, using brand-spec §1.3 visual language (state nodes, transition arrows, rule text line) |

### Why a standalone `.svg` file (not inline in README)

- GitHub sanitizes inline SVG in markdown, stripping most attributes and all `<style>` blocks.
- A referenced `.svg` file is rendered through GitHub's SVG proxy (`camo`), which preserves the full SVG spec minus scripts and external resources.
- A standalone file allows Elaine and Peterman to iterate on the design without touching README markdown.
- `git diff` on a single `.svg` file gives clean changesets vs. diffing an embedded SVG inside a markdown file.

### README wiring pattern

```markdown
<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="brand/readme-hero.svg">
    <img alt="Precept — Subscription Billing lifecycle: Trial → Active → Cancelled" src="brand/readme-hero.svg" width="800">
  </picture>
</p>
```

A single dark-mode SVG is sufficient initially (the brand surface is dark-ground). If a light variant is needed later, add a second `<source>` and `brand/readme-hero-light.svg`. This is additive — no restructuring needed.

---

## 2. GitHub Rendering & Maintenance Constraints

### GitHub SVG sandbox rules (non-negotiable)

| Constraint | Impact |
|------------|--------|
| **No `<script>`** | All content must be static geometry + text |
| **No `<foreignObject>`** | No embedded HTML, no `<div>`, no markdown-in-SVG |
| **No external resources** | No `<image href="...">`, no `xlink:href` to remote assets, no `@import` fonts |
| **No `<style>` blocks in referenced SVGs** | GitHub's camo proxy strips `<style>`. All styling must be **inline attributes** (`fill`, `stroke`, `font-family`, etc.) |
| **No CSS classes** | Stripped by sanitizer; use inline style or direct SVG attributes |
| **`font-family` fallback** | Custom fonts (Cascadia Cove) will NOT render — GitHub serves SVGs as `<img>`, so system fonts only. Use `font-family="monospace"` and accept platform-default monospace |
| **Fixed dimensions** | Set explicit `width` and `height` in px on the root `<svg>` element; percentage-based sizing is stripped |
| **Max file size** | Keep under 100KB for fast rendering and reasonable diff size |

### Font reality check

The brand-spec locks Cascadia Cove as the brand font. **GitHub cannot render it** in SVGs served via camo proxy (no font loading). Two viable options:

1. **Use `font-family="monospace"`** — accepts platform default. Pragmatic, zero maintenance.
2. **Convert text to `<path>` outlines** — pixel-perfect but non-editable, increases file size, complicates maintenance.

**Recommendation:** Use `font-family="monospace"` for all text elements. Reserve `<path>` conversion only if Peterman's brand review determines that text rendering variance across platforms is unacceptable. This is a **Shane decision point** — see §5.

### Maintenance model

- The SVG lives in `brand/` alongside brand-spec. Brand-aware contributors (Elaine, Peterman) own its visual correctness.
- README changes that affect the hero area (copy above/below the SVG) do not require touching the SVG file.
- SVG color values must match the locked 8+3 palette. Any palette drift is caught during Peterman's brand compliance gate.
- If the hero domain changes away from Subscription Billing (a separate future decision), the SVG is replaced wholesale — not patched.

---

## 3. Source-of-Truth & Handoff Plan

### Design → Implementation handoff sequence

```
1. Frank         → Architecture proposal (this document)        ← YOU ARE HERE
2. Shane         → Approves architecture + resolves open decisions (§5)
3. Elaine        → SVG design spec: layout, composition, visual hierarchy
                   Deliverable: annotated mockup OR draft SVG in brand/
4. Peterman      → Brand compliance review of Elaine's design
                   Checks: palette compliance, shape vocabulary (§1.3 brand marks),
                   signal color rules, typography constraints
5. Frank         → Architecture review of final SVG
                   Checks: GitHub constraint compliance (§2 above),
                   file size, no external deps, inline-only styling
6. Kramer        → Implementation: produce final brand/readme-hero.svg,
                   wire into README, relocate raw code block below hero
7. Elaine        → Post-draft visual audit
8. Peterman      → Post-draft brand compliance audit
9. Shane         → Final sign-off
```

### Source-of-truth files

| Artifact | Path | Owner |
|----------|------|-------|
| Hero SVG asset | `brand/readme-hero.svg` | Elaine (design), Peterman (brand gate) |
| README wiring | `README.md` lines 1–20 (approx) | Kramer (implementation) |
| Brand palette reference | `brand/brand-spec.html` §1.4 | Peterman |
| Shape vocabulary reference | `brand/brand-spec.html` §1.3 | Peterman |
| Architecture constraints | This proposal | Frank |

### Documentation sync (per project rules)

When the SVG is merged, the following must update in the same PR:
- `README.md` — hero section restructured (raw block removed, SVG wired in, code example relocated)
- `brand/brand-spec.html` — if a new surface section is warranted for "README hero," add it or note it in §2
- Any `.squad/decisions/` entries reflecting the locked choices

---

## 4. Rejected Alternatives

| Alternative | Why rejected |
|-------------|-------------|
| **Inline SVG in README markdown** | GitHub's markdown sanitizer strips `<style>`, most attributes, and many elements. The SVG would render broken or severely degraded. A referenced file goes through the camo proxy which preserves far more of the SVG spec. |
| **PNG/JPEG hero image** | Loses scalability, crisp rendering on retina, and version-control friendliness. Binary diffs are opaque. SVG is the correct format for geometric/diagrammatic content with text. |
| **Animated SVG (CSS keyframes / SMIL)** | GitHub strips `<style>` blocks and SMIL is deprecated in some browsers. Animation is also explicitly out of scope per issue #4. |
| **`<foreignObject>` with styled HTML** | Stripped by GitHub sanitizer. Non-starter. |
| **Raster screenshot of a styled code block** | Combines the worst of both: non-scalable, non-diffable, requires regeneration on any change. Also violates the "no raw Precept block in hero" directive since it would visually be a code block. |
| **SVG with embedded web fonts (`@font-face`)** | GitHub's camo proxy does not load external fonts. The font would silently fall back to default, making the embedded font declaration dead weight. |
| **Text-to-path for all SVG text** | Technically GitHub-safe but creates a maintenance nightmare: every text change requires re-outlining. Only justified if platform font variance is deemed unacceptable (Shane decision — §5). |
| **Dark + light SVG pair from day one** | Premature — the brand surface is dark-ground, GitHub dark mode is the primary context, and the `<picture>` pattern allows adding a light variant later without restructuring. |

---

## 5. Open Decisions for Shane

These are **Gate-Before-Start** items. Elaine's design work cannot begin until these are resolved.

### Decision A: Font rendering strategy

**Question:** Accept platform-default monospace in the hero SVG, or require path-outlined text for pixel-perfect brand font rendering?

| Option | Tradeoff |
|--------|----------|
| **A1: `font-family="monospace"` (recommended)** | Text is editable, file stays small, maintenance is trivial. Visual will vary slightly across OS (Consolas on Windows, SF Mono on macOS, DejaVu Sans Mono on Linux). All are monospace — the structural feel is preserved. |
| **A2: Path outlines** | Pixel-perfect Cascadia Cove rendering. But every text edit requires font tooling to re-outline. File size increases. `git diff` on path data is meaningless. |

**Default if not decided:** A1 (platform monospace).

### Decision B: SVG composition scope

**Question:** Should the hero SVG contain only the visual diagram (state flow + reject moment), or also include text elements like the product name / tagline?

| Option | Tradeoff |
|--------|----------|
| **B1: Diagram only — text stays in README markdown (recommended)** | Clean separation: SVG owns the visual, markdown owns the copy. Text is searchable, editable, translatable. SVG stays focused and small. |
| **B2: Diagram + wordmark + tagline inside SVG** | Self-contained visual unit — looks exactly the same everywhere. But text is not searchable, not easily editable, and couples copy changes to SVG file changes. |

**Default if not decided:** B1 (diagram only in SVG, text in markdown).

### Decision C: Hero concept lock

**Question:** Confirm Subscription Billing as the hero concept for the SVG pass, understanding that changing it later means replacing the SVG wholesale.

This is already stated in issue #4 ("current live concept can remain Subscription Billing"), but per the `proposal-gate-analysis` skill, hero domain is always a Gate-Before-Start item. Explicit confirmation avoids mid-flight domain churn.

**Default if not decided:** Subscription Billing (per issue #4).

---

## Risks

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Font rendering variance across platforms looks unprofessional | Medium | Peterman reviews on macOS + Windows + Linux screenshots before merge. If unacceptable, escalate to Decision A (path outlines). |
| SVG file grows too complex / large for clean diffs | Low | Set 100KB ceiling. Kramer flags if approaching. Simplify geometry before adding detail. |
| GitHub changes camo proxy sanitization rules | Very low | The constraints listed in §2 have been stable for years. If they change, SVG is still the most resilient static format. |
| Design iteration takes multiple rounds | Medium | Expected. The handoff sequence (§3) has clear gates. Each round is scoped: Elaine owns visual, Peterman owns brand, Frank owns GitHub constraints. |
| Subscription Billing domain changes before SVG ships | Low | Issue #4 explicitly locks it. If it changes, the SVG is replaced — the architecture (file path, wiring, `<picture>` pattern) is domain-agnostic. |


---

---

# J. Peterman — Brand/DevRel proposal pass for issue #4

## Proposed section for amendment

### Brand / DevRel recommendation

**1. One-glance message**

The hero should communicate this instantly: **Precept is the contract that governs the lifecycle.** The visual should show a clean state flow for Subscription Billing, then make the blocked reactivation path unmistakable so GitHub readers understand, in one glance, that Precept does not merely describe rules — it structurally prevents invalid transitions.

**2. Supporting copy**

Recommended supporting copy beneath the SVG:

> Define the contract once. Precept binds state, data, and business rules into one executable surface where invalid states are structurally blocked.

If a second line is wanted:

> The README hero should sell the category first; the raw example can teach the syntax just below.

**3. Brand constraints the SVG must honor**

- **SVG-first, GitHub-safe, static.** No scripts, no fragile CSS dependence, no animation.
- **Dark-surface friendly.** It should feel native on GitHub without fighting GitHub's chrome.
- **Semantic color discipline.** Indigo for structure, Violet for states, Cyan for events, Bright Slate for data, Gold only as a sparse rule/message accent.
- **No Rose-led marketing surface.** Blocked status can be communicated by closure, crossed flow, or wording; the README hero should not feel error-branded.
- **Use the Precept visual language, not generic SaaS diagramming.** The piece should feel like a branded contract surface, not a flowchart template.
- **Tasteful restraint.** One focal story: `Trial → Active → Cancelled`, with the non-reactivation rule made memorable. No faux-dashboard clutter, no ornamental gradients that add heat but not meaning.
- **Typography should stay in-family.** If labels or wordmark treatment appear inside the SVG, they should align with the Cascadia Cove / editor-native brand posture.

**4. What should move lower in the README**

Once the hero becomes visual-first, the current raw DSL block and paired C# execution snippet should move down into the practical teaching section as the real **Quick Example**. The temporary status note should attach to that example, not sit in the hero area. In other words: top of README becomes product thesis; lower section becomes hands-on proof.

**5. Approval framing**

Shane is not really being asked to approve an SVG drawing. He is being asked to approve five things:

1. that the README's first screen becomes **visual-first**
2. that **Subscription Billing** remains the temporary live concept
3. that the hero's job is to communicate **executable contract + governed state flow + blocked invalid path**
4. that the raw Precept/C# sample moves lower as proof instead of serving as the hero itself
5. that brand discipline means **stronger than utilitarian**, but still truthful, restrained, and GitHub-safe

That is the real decision gate. Once those are approved, SVG execution becomes craft, not strategy.

---

---

# Decision Inbox Merge — 2026-04-05T02:32:19Z

**Merged by:** Scribe
**Source:** .squad/decisions/inbox/

---

---

# README inline DSL for preview surfaces

- **Date:** 2026-04-05
- **Owner:** J. Peterman
- **Decision:** The README hero's DSL now renders inline in `README.md` with README-safe HTML (`<pre><code>` and inline `span` styles) instead of asking readers to open `brand/readme-hero-dsl.html`.
- **Why:** VS Code markdown preview needs the contract visible on the public surface itself. The standalone HTML and raw `.precept` artifacts remain as companion references, not substitutes for the README display.
- **Implication:** README-safe inline HTML is the preferred pattern when a branded code sample must remain visible inside markdown preview without inventing new product claims or replacing the source artifacts.

---

---

# README link removal

- Removed the contract artifact links from `README.md` and kept the inline DSL block as the single quick-example reading path.
- Left the standalone files in place, but stopped advertising them in the README so the hero section reads cleanly without sending readers away.


---

---

# Decision Inbox Merge — 2026-04-05T02:54:36Z

**Merged by:** Scribe
**Source:** .squad/decisions/inbox/
**Summary:** Team safe recommendation = freeze-and-curate cutover. Frank's force-promote recommendation remains logged as a dissenting architectural view and is currently blocked by reviewer rejection.
**Merged:** 4
**Skipped as duplicates:** 0

---

---

# Decision: Trunk Consolidation Strategy — Force-Promote Feature to Main

**Date:** 2026-04-09
**Owner:** Frank (Lead/Architect)
**Requested by:** Shane
**Status:** Awaiting Shane sign-off
**Supersedes:** `steinbrenner-consolidation-plan.md` (recommending rejection)

---

## Situation Analysis

### Branch Topology

| Branch | Commits | Root | Unique content vs `feature/language-redesign` |
|--------|---------|------|-----------------------------------------------|
| `main` | 2 | `a2b867f` (GitHub auto-init) | **Zero.** Placeholder README, superseded. |
| `feature/language-redesign` | 276 | `550b708` (real project init) | **This IS the project.** Runtime, tooling, 666 tests, 20+ samples, docs, brand. |
| `origin/master` | ancestor of feature | `550b708` | Zero. Fully contained. |
| `diagram-layout-option-b` | ancestor of feature | `550b708` | Zero. Fully contained. |
| `copilot/worktree-*` | ancestor of feature | `550b708` | Zero. Fully contained. |
| `origin/diagram-layout-option-a` | 1 unique commit | `550b708` | 1 abandoned exploration commit. |
| `origin/diagram-layout-option-c` | 1 unique commit | `550b708` | 1 abandoned exploration commit. |
| All other `origin/*` branches | ancestors of feature | `550b708` | Zero. All fully contained. |

### The Core Problem

`main` and `feature/language-redesign` have **no merge base** — they descend from different root commits. `main` was created via GitHub's "Initialize repository" UI (`a2b867f`), while all real development started from a separate local init (`550b708`). The two histories never intersected.

`main` contains exactly 2 commits: an auto-generated initial commit and a "concept readme" with placeholder badges and aspirational content. Every byte of that content has been superseded by the feature branch.

### Current State

- Working tree is **clean** (previously uncommitted doc changes were committed as `9436065`).
- 17 commits on `feature/language-redesign` are not yet pushed to origin.
- One sibling worktree exists at `Precept.worktrees/copilot-worktree-*` on branch `copilot/worktree-2026-03-28T05-06-33`. Its branch is fully contained in the feature branch.
- No open PRs on the repository.

---

## Decision

**Force-promote `feature/language-redesign` to `main`.** Do not merge, rebase, cherry-pick, or curate.

### Rationale

The "unrelated histories" framing makes this sound dangerous. It is not. Here is what is actually happening:

1. `main` is a **dead placeholder** — 2 commits, zero unique content, zero test coverage, zero runtime code.
2. `feature/language-redesign` **is the project** — 276 commits of coherent development history including the real initial commit, all implementation, all tests, all tooling.
3. There is nothing to merge. There is nothing to transplant. There is nothing to curate. The placeholder must be replaced with the reality.

A `--allow-unrelated-histories` merge would create a nonsensical merge commit joining an empty placeholder to 276 commits of real work. That is worse than force-push — it leaves a lie in the history.

Steinbrenner's "curated re-landing" plan proposes creating a fresh branch off `main`, transplanting content in buckets, and committing curated chunks. This is an **outrageous** amount of work to preserve two placeholder commits that have zero value. It also introduces transplant risk (missed files, broken references, test failures from incomplete copies) for absolutely no gain.

### What We Lose

`main`'s 2-commit history (`a2b867f Initial commit` → `31a9f9e concept readme`). This is a GitHub auto-init plus a concept README. There is nothing here worth preserving. If sentimentality demands it, tag it first.

### What We Gain

- `main` becomes the single authoritative line with the project's real history.
- Zero risk of transplant errors.
- No multi-day curated landing operation.
- Clean `git log main` showing the actual development story from `550b708 Initial commit` forward.

---

## Execution Sequence

### Pre-flight (before any push)

1. **Build passes.** `dotnet build` on `feature/language-redesign` must succeed.
2. **All 666+ tests pass.** `dotnet test` must be green.
3. **Push feature branch.** Push the 17 unpushed commits to `origin/feature/language-redesign` so they are backed up remotely before any destructive operation.

### Execute

4. **Tag the old main for provenance** (optional but cheap):
   ```
   git tag archive/old-main main
   git push origin archive/old-main
   ```

5. **Force-update main locally:**
   ```
   git checkout main
   git reset --hard feature/language-redesign
   ```

6. **Force-push main to origin:**
   ```
   git push origin main --force-with-lease
   ```

7. **Delete the feature branch** (it IS main now):
   ```
   git branch -d feature/language-redesign
   git push origin --delete feature/language-redesign
   ```

### Cleanup

8. **Remove sibling worktree:**
   ```
   git worktree remove <path-to-copilot-worktree> --force
   git branch -D copilot/worktree-2026-03-28T05-06-33
   ```

9. **Archive and prune stale remote branches.** Tag any branch with unique commits first:
   ```
   git tag archive/diagram-option-a origin/diagram-layout-option-a
   git tag archive/diagram-option-c origin/diagram-layout-option-c
   git push origin archive/diagram-option-a archive/diagram-option-c
   ```
   Then delete all stale remote branches: `origin/master`, `origin/diagram-layout-option-*`, `origin/shane/*`, `origin/upgrade-to-NET10`, `origin/copilot/*`.

10. **Set default branch on GitHub** to `main` if not already (it is — `origin/HEAD` → `origin/main`).

11. **Verify final state:** `main` is the sole authoritative branch. `git log main` shows 276 commits from the real root. All tests pass. No dangling worktrees or ghost branches.

---

## What Must Happen Before Any Push to Main

This is non-negotiable:

1. ✅ Working tree clean (confirmed — `9436065` committed the last dirty files)
2. ⬜ `dotnet build` passes on `feature/language-redesign`
3. ⬜ `dotnet test` passes — all 666+ tests green
4. ⬜ 17 unpushed commits pushed to `origin/feature/language-redesign` as backup
5. ⬜ Shane explicitly approves this strategy

---

## Side Branch and Worktree Treatment

| Asset | Action | Reason |
|-------|--------|--------|
| `diagram-layout-option-b` (local) | Tag as `archive/diagram-option-b`, then delete | Fully contained in feature; historical reference only |
| `copilot/worktree-*` (local + worktree) | Remove worktree, delete branch | Fully contained; worktree is stale |
| `origin/diagram-layout-option-a` | Tag as `archive/diagram-option-a`, then delete | 1 unique commit — abandoned exploration |
| `origin/diagram-layout-option-c` | Tag as `archive/diagram-option-c`, then delete | 1 unique commit — abandoned exploration |
| `origin/master` | Delete | Fully contained in feature; legacy default branch name |
| `origin/shane/*` (3 branches) | Delete | All fully contained in feature |
| `origin/upgrade-to-NET10` | Delete | Fully contained in feature |

---

## Rejection of Steinbrenner's "Curated Re-Landing" Plan

Steinbrenner proposed transplanting content from `feature/language-redesign` to a fresh branch off `main` in curated buckets. I am rejecting this approach for the following reasons:

1. **It preserves nothing of value.** `main`'s 2-commit history is a placeholder. Preserving it as the root of the curated branch adds no information.
2. **It creates transplant risk.** Manually copying a 276-commit project across branch boundaries is error-prone. Files get missed, paths break, test configurations diverge.
3. **It destroys real history.** The curated commits would replace 276 real commits with a synthetic reconstruction. The actual development story — who wrote what, when, and why — would be lost.
4. **It costs days of work for zero architectural benefit.** The only beneficiary is a 2-commit placeholder that should never have existed.

The only scenario where curated re-landing makes sense is when the feature branch contains work that should NOT go to trunk. I audited the full tree — there is no such content. The feature branch *is* the project.

---

## Definition of Done

1. `main` points to the current tip of `feature/language-redesign` (commit `9436065` or later).
2. `origin/main` is force-updated to match.
3. `feature/language-redesign` branch is deleted (local and remote).
4. All stale branches are archived (tagged) or deleted.
5. Sibling worktree is removed.
6. Build and tests pass on the new `main`.
7. Shane can point to `main` as the sole authoritative line for all ongoing work.

---

---

# Decision: Safe Return to Trunk from `feature/language-redesign`

**Date:** 2026-04-05
**Owner:** Steinbrenner
**Requested by:** Shane

## Situation

- `main` and `feature/language-redesign` have no merge base. This is an unrelated-history problem, not a normal long-lived feature branch.
- `main` is still the minimal trunk (`Initial commit` + `concept readme`).
- `feature/language-redesign` is the live superset branch and already contains the work from:
  - `diagram-layout-option-b`
  - `copilot/worktree-2026-03-28T05-06-33`
- The working tree is dirty before landing starts:
  - `docs/PreceptLanguageDesign.md`
  - `docs/RuntimeApiDesign.md`

## Decision

**Return to trunk by curated re-landing from a fresh branch created off `main`. Do not preserve the current branch history as the trunk history.**

This branch line is too mixed for direct promotion:

- unrelated history versus trunk
- exploratory README / brand / squad / language-doc work layered together
- uncommitted docs on top of the branch tip

The safe path is to treat `feature/language-redesign` as a source tree, not as merge-ready history.

## Recommended Landing Sequence

1. **Freeze the source branch.** No new work lands on `feature/language-redesign` until the trunk-return sequence is complete.
2. **Quarantine current uncommitted docs.** Stash `docs/PreceptLanguageDesign.md` and `docs/RuntimeApiDesign.md` together as a named WIP stash. Do not let those edits ride along invisibly into trunk work.
3. **Inventory what actually deserves trunk.** Split the source tree into landing buckets before copying anything:
   - product/runtime/tooling code and tests
   - core docs that describe shipped behavior
   - README / brand / squad process material
4. **Create a fresh integration branch from `main`.** This is the only branch that should target trunk.
5. **Transplant bucket 1 first: product/runtime/tooling.** Copy the implementation tree from `feature/language-redesign` into the integration branch and commit it in curated chunks that produce a coherent product baseline.
6. **Validate baseline.** Build and run the existing test suite on the integration branch before any README/brand/process payload is added.
7. **Transplant bucket 2 second: essential docs only.** Land docs that are required to describe the implementation now on trunk. Keep this separate from process/brand material.
8. **Re-evaluate the stashed local docs.** Only reapply them if they still describe implemented behavior after the product baseline is on trunk. If they are speculative or partially aligned, split or discard them.
9. **Transplant bucket 3 last and selectively: README / brand / squad.** Land only the pieces that support the current strategy and are intended to live on trunk. Do not bulk-copy every orchestration artifact by default.
10. **Review trunk shape against strategy.** Confirm the resulting trunk contains the intended product baseline plus the current README/hero direction, with no leftover exploratory baggage.
11. **Open one reviewed PR to trunk.** The PR should present curated commits, not the old branch graph.
12. **After merge, clean up retired branches/worktrees.** Do cleanup only after trunk is green and the new trunk branch is authoritative.

## What to Commit, Stash, Discard, or Split Before Trunk Work Begins

### Stash immediately

- `docs/PreceptLanguageDesign.md`
- `docs/RuntimeApiDesign.md`

Reason: these are local, unreviewed, and currently mixed into a planning problem. They must be evaluated after the integration branch exists.

### Split during landing

- **Implementation vs. docs** must be separate curated commits.
- **README / brand / squad** must not be combined with runtime/tooling commits.
- Any commit that mixes product behavior with process-history files should be broken apart.

### Commit into trunk only if still justified

- Product/runtime/tooling files that represent the real current strategy.
- Documentation that matches implemented behavior.
- README/brand assets that support the chosen hero and positioning and are intended to ship with trunk.

### Discard or leave behind unless specifically needed

- Pure orchestration exhaust, duplicated session logs, and temporary planning artifacts that do not add lasting trunk value.
- Any local doc text from the stash that describes behavior not yet implemented or no longer strategically chosen.

## Side Branch and Worktree Treatment

### `diagram-layout-option-b`

- Treat as **historical reference only** during trunk return.
- It is already an ancestor of `feature/language-redesign`; nothing from it needs to be merged separately.
- Keep it until trunk landing is complete, then retire it if no one still needs branch-local breadcrumbs.

### `copilot/worktree-2026-03-28T05-06-33`

- Treat the sibling worktree branch the same way: **freeze and preserve until trunk landing completes**, but do not merge it independently.
- It is also already contained in `feature/language-redesign`.
- After trunk is green, remove the worktree and delete the branch if no active task still depends on it.

## History Strategy

**Land as curated commits. Do not preserve the existing branch history on trunk.**

Why:

- There is no merge base with `main`.
- The branch history mixes product work, brand exploration, squad process setup, and follow-on cleanups.
- A curated sequence gives Shane a reviewable story: product baseline first, docs second, strategy-facing assets last.

If historical provenance matters, preserve the old branch by tag or by leaving the branch in remote temporarily. Trunk should still receive the curated series.

## Definition of Done

We are back on trunk and finalized on the current strategy when all of the following are true:

1. `main` contains the selected implementation baseline from `feature/language-redesign` through a reviewed PR from a fresh integration branch.
2. The trunk history is a curated commit series, not an unrelated-history merge.
3. The test/build baseline passes on the integration branch and again after merge.
4. Every local pre-landing edit was resolved deliberately: committed as scoped work, left in a clearly named follow-up branch, or discarded.
5. No side branch or sibling worktree remains as a hidden source of truth; either trunk supersedes them or they are explicitly retained as archived references.
6. README / brand / docs on trunk reflect the current strategy actually chosen to ship now, not exploratory alternates.
7. Shane can point to `main` as the sole authoritative line for ongoing work.

---

---

# Uncle Leo - Consolidation Safety Review

**Date:** 2026-04-05
**Reviewer:** Uncle Leo
**Subject:** return-to-main risk review for `feature/language-redesign`

## What I reviewed

- `main` at `31a9f9ed534c0290ac1340830b210b091ca37a35`
- `feature/language-redesign` at `9436065c678aff1d93538129bf72b1e4d9d244eb`
- branch topology, worktree state, recent history shape, `README.md`, `docs/PreceptLanguageDesign.md`, `.squad/decisions.md`
- repository health checks: IDE diagnostics clean; `dotnet build` succeeded; `dotnet test --no-build` succeeded (703/703)

## Observed facts that drive the review

1. There is **no merge base** between `HEAD` and `main`.
2. `main` is effectively a 2-commit concept branch; `feature/language-redesign` is a 275-commit product branch.
3. `main` vs current `HEAD` is not a normal PR-sized change. It is roughly **600 files / 106k insertions** and introduces the actual repo structure (`src`, `tools`, `test`, `samples`, `.github`, `.copilot`, `.squad`, brand assets, etc.).
4. The candidate branch **moved during review**: the snapshot started at `f302417...` and later advanced to `9436065...`. In a shared worktree, that means any trunk decision against the branch name alone is unsafe.
5. Local `feature/language-redesign` is **17 commits ahead of `origin/feature/language-redesign`**.

## 1. Biggest technical and process risks

### Technical risks

- **Topology risk:** unrelated histories mean a normal merge does not preserve a meaningful review trail from `main`.
- **Blast-radius risk:** landing this branch touches product code, tests, tooling, docs, automation, squad/process files, and public brand assets in one shot.
- **Contract drift risk:** `README.md`, `docs/PreceptLanguageDesign.md`, runtime API docs, language server behavior, and MCP-facing outputs are all trunk-visible contracts. A bad landing here breaks both humans and AI consumers.
- **Automation risk:** `.github`, `.copilot`, and `.squad` are not passive content. Landing them changes workflow behavior, agent behavior, and repo operations.

### Process risks

- **Unstable approval surface:** the branch advanced while being reviewed.
- **Mixed intent history:** code, docs, brand work, automation, and squad state are interleaved. That is terrible landing hygiene for trunk.
- **Reviewability failure:** treating this as a merge instead of a repository cutover will hide the real decision: which parts of this branch are actually authorized to become trunk.

## 2. What makes a direct merge or push unsafe

### Reject: **Direct merge**
**Why rejected:** no merge base, unrelated histories, and the resulting merge commit would pretend this was an incremental integration when it is actually a full replacement/import.

### Reject: **Direct push / force-push `main` to the floating branch**
**Why rejected:** the candidate moved during review, is ahead of origin by 17 local commits, and contains mixed product + process + documentation history. Replacing trunk from a mutable branch name is not a controlled landing.

### Reject: **Blind squash of current HEAD onto `main`**
**Why rejected:** it hides provenance for a 600-file import and prevents targeted rollback/review of code vs docs vs automation.

### Reject: **Cherry-pick only the newest docs/README commits**
**Why rejected:** those commits assume the rest of the product branch exists. Moving docs/public claims without the underlying product surface is guaranteed drift.

## 3. Artifacts and branch states that need explicit review before trunk is touched

1. **Frozen candidate SHA** - trunk work must target an exact commit, not `feature/language-redesign` as a moving ref.
2. **Product tree import** - `src\`, `tools\`, `test\`, `samples\` need explicit sign-off as the real software payload.
3. **Public contract docs** - `README.md`, `docs\PreceptLanguageDesign.md`, `docs\RuntimeApiDesign.md`, and MCP/language-server-facing docs must be checked against implementation.
4. **Operational surfaces** - `.github\`, `.copilot\`, `.squad\`, `.gitattributes`, `.gitignore`, and any workflow/config additions need explicit authorization, not incidental landing.
5. **Brand/public collateral** - `brand\` and related README hero assets should be reviewed as publication changes, not hidden inside a code cutover.
6. **Branch/worktree state** - confirm no sibling worktree or local-only state is about to be invalidated by history surgery, and confirm the reviewed SHA still matches what is being promoted.

## 4. Preserve history as-is, or curate it?

**Verdict:** preserve the current branch history for archaeology, but **do not land trunk with this history as-is**.

What should happen instead:

- keep `feature/language-redesign` (and optionally tag the reviewed SHA) as the archival record
- create a **curated integration branch** from `main`
- transplant the approved tree onto that branch in deliberate, reviewable commits (for example: product/code, docs/contracts, automation/process)
- only then update `main`

That keeps the evidence without making trunk absorb every exploratory, orchestration, and checkpoint-era commit.

## 5. Reviewer verdict on strategy patterns

### Approve: **Freeze-and-curate cutover**
Approved pattern:
1. freeze the exact SHA
2. create a fresh integration branch from `main`
3. import only the explicitly approved tree/content
4. review by artifact class (product, contracts, automation, brand/process)
5. rerun build/tests on the curated branch
6. update `main` from that curated branch

### Reject: **Merge-as-if-normal PR**
Rejected because the histories are unrelated and the scope is repository replacement, not incremental change.

### Reject: **Force-repoint `main` to feature head**
Rejected because the branch is mutable, locally ahead of origin, and not separated from process/automation payload.

### Reject: **Ship current history intact to trunk**
Rejected because the trunk history would inherit exploratory/squad/checkpoint noise and destroy review clarity.

## Bottom line

This branch can be the source of truth for a landing, but **not by direct merge and not by floating-ref force push**. Freeze the SHA, curate the landing, and treat trunk touch as a repository cutover with explicit artifact review gates.

---

---

# Recommendation: Treat trunk consolidation as curation, not merge mechanics

**Date:** 2026-04-05
**By:** J. Peterman
**Status:** Recommendation for review

## Summary

`feature/language-redesign` carries the actual product direction, while `main` remains a separate concept-readme lineage with no merge base to the current branch.

That means trunk consolidation should be treated as a curation decision, not a routine merge operation.

## Recommendation

Before finalizing onto trunk, explicitly decide:

1. whether trunk will be rebased around `feature/language-redesign` as the new root, or
2. whether a selected subset of this branch will be re-landed onto `main`.

## Why

- The current branch contains the real language redesign, runtime/tooling work, MCP/AI surface, and the current public narrative.
- `main` does not represent that work.
- A normal merge frame would imply continuity that the repository history does not have.

## Practical effect

The team should create a keep/defer/archive list before trunk consolidation, then land the chosen line deliberately with source-of-truth docs updated in the same pass.

---

# UX Decision: In-Diagram Transitions Exploration

**Filed by:** Elaine (UX Designer)
**Date:** 2026-04-07
**Status:** Exploration — pending Shane review
**Artifact:** `tools/Precept.VsCode/mockups/preview-inspector-in-diagram-transitions-mockup.html`
**Comparison baseline:** `tools/Precept.VsCode/mockups/preview-inspector-redesign-mockup.html`

---

## What This Explores

Moving the primary transition affordances (event buttons, inline args, fire action, outcomes, reject reasons) out of the bottom event dock and into edge-anchored panels on the diagram surface itself. The bottom dock is removed entirely; vertical space is reclaimed for the diagram.

---

## What Changed From The Dock Model

| Aspect | Dock model (redesign mockup) | In-diagram model (this exploration) |
|--------|------------------------------|-------------------------------------|
| Event interaction surface | Bottom dock panel with flat list of event rows | Floating panels anchored to their SVG edges |
| Vertical layout | Header + diagram/data + dock (three-part) | Header + diagram/data (two-part) |
| Spatial context | Events listed by name; user must map to edges mentally | Events live where their edges are; spatial context is immediate |
| Keyboard flow | Tab through flat list in dock | Tab through edge-anchored panels in diagram z-order |
| Scalability at 5+ events | Dock scrolls vertically; stays usable | Panels start overlapping; requires collapse/expand management |
| Screen reader story | Clean HTML list semantics | `role="region"` panels over SVG; harder to linearize |

---

## Tradeoffs

### Gains

1. **Spatial coherence.** Each event is visually connected to the edge it represents — source state, destination state, and transition direction are immediately visible without cross-referencing between the dock and the diagram.
2. **Vertical real estate.** The dock typically consumes 25-30% of panel height. Removing it gives the diagram room to breathe, especially in short viewports.
3. **Fewer cognitive zones.** The user scans one surface (diagram + panels) instead of two (diagram, then dock below).
4. **Debugging context.** When tracing "why did this transition fire?" the args, outcome, and edge are all in one place.

### Losses

1. **Keyboard navigation regression.** The dock's flat list is inherently keyboard-friendly — a simple `aria-role="list"` with tab stops. Edge-anchored panels require spatial focus management that is harder to implement and test accessibly.
2. **Overlap at scale.** With 5+ events from one state (common in real precepts like hiring pipelines or insurance claims), floating panels will overlap each other. The dock's vertical scroll handles this gracefully.
3. **Arg entry ergonomics.** In the dock, arg fields are always visible and inline. In the diagram model, expanded panels compete for space with the diagram itself, and expanding one may obscure adjacent edges or state nodes.
4. **Visual noise.** Always-expanded panels create a "dashboard" feeling rather than a focused diagram. Collapse-to-reveal adds interaction cost.

### Open Questions

- **Auto-expand vs click-to-reveal?** If panels auto-expand for all current-state events, dense diagrams become cluttered. If they require click, the user can't scan all events at a glance the way the dock allows.
- **Panel positioning strategy.** For self-loops, panels can anchor above the node. For horizontal edges, they can anchor below. But complex topologies (multiple edges between the same pair, backward edges) create positioning conflicts.
- **How does this interact with edit mode?** The dock cleanly separates event execution from field editing. In the diagram model, the data lane is the only non-diagram surface — does edit mode feel orphaned?

---

## Recommendation

**This is worth exploring further but is not ready to replace the dock model.**

The spatial-context gain is real and meaningful for debugging. But the keyboard/accessibility regression and the scalability concern at 5+ events are significant. A hybrid approach may be worth considering:

- Keep the dock as the primary keyboard-accessible event list
- Add edge-hover or edge-click highlighting that scrolls the dock to the relevant event
- Or: use diagram edge labels as a *secondary* fire affordance (click edge → fires event) while keeping the dock as the canonical interaction surface

I'd want Shane to look at both mockups side by side and judge whether the spatial gain outweighs the interaction density loss for real debugging work.

---

## Review Needed

- **Shane:** Does this direction feel worth pursuing, or does the dock model serve debugging better?
- **Peterman:** Brand compliance is identical (same palette, same font, same semantic colors). No brand review needed unless the layout direction changes.
- **Frank:** If this direction moves forward, the SVG overlay / `foreignObject` positioning strategy would need architectural review.

---

# Inspector review refresh decision

**Date:** 2026-04-05
**Author:** Elaine

## Decision

For PRD and redesign work, treat the inspector as a **combined preview surface**, not a standalone side panel. The baseline UX is a three-part shell:

1. header shell
2. diagram canvas with in-canvas data lane
3. bottom event dock

## Why

- That is the shape implemented in `tools/Precept.VsCode/webview/inspector-preview.html`.
- It matches the archived interaction contract in `docs/archive/InteractiveInspectorMockup.md` closely enough to count as the lived UX baseline.
- The old `brand/inspector-panel-review.md` had become misleading because it audited mostly the data list and under-described the rest of the surface.

## Consequences for the redesign

- Do **not** write the PRD as if the task is only "reskin the inspector list."
- Preserve the diagram/data/event-dock relationship unless Shane signs off on a structural change.
- Evaluate accessibility improvements against the overlay model first; do not assume a table-first replacement without evidence.
- Define AI-first requirements at the preview host contract level, not only as DOM helper functions.

---

# UX Decision: Preview/Inspector Redesign Mockup v1

**Author:** Elaine (UX Designer)
**Date:** 2026-04-07
**Issue:** #7 — Create UX mockups for preview/inspector redesign
**Artifact:** `tools/Precept.VsCode/mockups/preview-inspector-redesign-mockup.html`

---

## What this decides

The first concrete mockup for the preview/inspector redesign establishes the following UX patterns. These are design proposals — not locked until Shane signs off — but they should guide Peterman's brand review and Frank's architecture review.

### 1. Current-state label in header chrome

The current state now appears as an explicit **violet pill badge** in the header bar, alongside the file name and follow/lock mode. This satisfies PRD § 6.2: the current state is unambiguous even when the diagram is dense, and it's trivially extractable by AI agents.

**Rationale:** The diagram already shows current state visually (violet stroke + fill). But for quick scanning, screenshots, and AI extraction, a textual label in the chrome is faster. The violet pill treatment matches the state's semantic color and reads as a status indicator, not navigation.

### 2. Field type as secondary metadata

Each field row now shows type information (`string · nullable`, `number · default 0`) as a second line below the field name, in `--slate-type: #9AA8B5` at 10px. This satisfies PRD § 6.5.

**Rationale:** Field type is useful for debugging and AI comprehension but should not compete with the field name and value for visual weight. A muted secondary line gives the information without cluttering the scan path.

### 3. Constraint message treatment: Gold, not Red

Constraint explanation text (the `because` messages from invariants and assertions) uses **Gold `#FBBF24`** at 11px, per brand-spec § 2.3. This distinguishes constraint explanations from blocked/error signals (Rose).

**Rationale:** The current implementation shares the same red for "this event is blocked" and "here's why the rule exists." Those are different cognitive tasks — one is a status signal, the other is an explanation. Gold separates them.

### 4. State-rules badge

An `⚡ 1 rule` badge appears in the data lane header when invariants are active. This replaces the prior orange badge with a gold-bordered badge that matches the constraint-message color family.

### 5. Event outcome visibility

Each event row now shows the outcome inline: `→ transition Cancelled` or `→ no transition · stays Active`. This makes the destination explicit without requiring diagram reading.

### 6. Title change: "Precept Preview" not "State Diagram"

The header title is now "Precept Preview" — this surface is not just a diagram, it's the integrated preview/inspector. The title should reflect the full product surface.

### 7. Section labels for data lane and event dock

Both zones have small uppercase labels ("INSTANCE DATA", "EVENTS · Active") that clarify the panel structure. The event dock label includes the current state name so the scope is explicit.

### 8. Scenario switcher (mockup-only)

The mockup includes a tab switcher to show both the Active state (two enabled events with args) and the Cancelled state (one blocked, one undefined event with reasons). This is a **mockup-only** affordance — the real implementation switches states through event execution, not tabs.

---

## What this does NOT decide

- Edit mode layout (Save/Cancel flow, draft validation banners) — future mockup pass
- Diagram visual restyling beyond structural color alignment — future mockup pass
- AI-first contract format — needs Frank's architecture review
- Responsive breakpoint behavior — needs testing in real webview

---

## Review needed

- **Peterman:** Brand compliance review against § 1.4 + § 2.3. Especially: violet pill in header, gold constraint messages, emerald/rose event buttons.
- **Frank:** Architecture fit for the current-state label pattern and event outcome display.
- **Shane:** Design gate sign-off before Kramer implements anything.

---

# Preview/Inspector Panel Audit

**Author:** Kramer (Tooling Dev)
**Date:** 2026-04-05
**For:** PRD authoring — Shane / coordinator

---

## 1. Current-State Implementation Summary

The inspector/preview panel is a **fully functional, production-quality interactive surface** implemented as a VS Code webview in `tools/Precept.VsCode/webview/inspector-preview.html` (3,464 lines), driven by `tools/Precept.VsCode/src/extension.ts`.

### What it does today:

**Layout:**
- Header: "State Diagram" title + source file name + preview mode indicator (Following/Locked) + Edit/Save/Cancel/Reset buttons
- Main body: SVG state diagram (left, in a scrollable container) + in-canvas data lane (right, fixed-width column)
- Bottom event dock: vertical event list for current-state events

**State diagram:**
- SVG drawn with smooth rounded-corner polyline paths (computed via layout payload from extension host)
- Animated transition: runner dot travels at constant speed along the accepted path, with source collapse / destination arrival handoff
- Edge/node emphasis model: hover highlights matching transitions, non-hovered transitions mute
- Destination node colors reflect evaluated outcome (green/red)
- Toast overlay in diagram on fire result (transient pill chip)

**Event dock:**
- Parallelogram-skewed event buttons (design evolution from mockup's round pills)
- Microstatus glyphs: ✔ (enabled), ✖ (blocked), ∅ (undefined/disabled)
- Inline arg inputs beside each event button (text, number, boolean toggle, nullable toggle)
- Inline reason text for blocked/undefined events, dimmed when not selected, full-bright on hover/focus
- Row-anchored result chip on fire (transient, green/red)
- Keyboard nav: ArrowUp/Down cycles events, Enter fires selected event

**Data panel (in-canvas right lane):**
- `<ul>` list of field name / value pairs
- Rule violation banner (orange, shown when active rule violations exist on the current state)
- State-rules indicator badge (shows count of rule definitions scoped to the current state, with tooltip)
- Draft validation banner (shown during edit mode when form-level errors exist)
- Field-level rule icons (⚠ with tooltip for fields with rule definitions)
- Edit mode: Enter/Save/Cancel workflow, per-field inline inputs (text/number/boolean/null toggle)
- Live draft validation via `inspectUpdate` round-trip on debounced input change
- Data toasts: `before → after` inline animation on successful fire
- Null toggle for nullable fields

**Extension host integration (extension.ts):**
- Single webview panel (`preceptPreview`), singleton lifecycle
- Follow-active-editor mode + preview lock toggle
- postMessage protocol: `previewRequest/previewResponse` (snapshot, fire, reset, inspect, inspectUpdate, update, replay actions)
- Layout computed server-side, delivered in snapshot payload

---

## 2. Biggest Gaps vs. Mockup + Brand Review

### Gap 1: Color System — Still Not Migrated (HIGH)

The `inspector-panel-review.md` identified this and it remains 100% unaddressed:

| Element | Current | Brand Target | Status |
|---|---|---|---|
| State label (`.status`) | `--state: #6D7F9B` | Violet `#A898F5` | ❌ Not done |
| Event names | `--event: #8573A8` | Cyan `#30B8E8` | ❌ Not done |
| Enabled indicator | `--ok: #1FFF7A` (neon green) | `#34D399` (emerald) | ❌ Not done |
| Blocked indicator | `--err: #FF2A57` | `#F87171` (rose) | ❌ Not done |
| Constraint violation messages | `--err` red | Gold `#FBBF24` | ❌ Not done |
| Field names | inherits white | Slate `#B0BEC5` | ❌ Not done |
| Field values (read-only) | `--muted: #59657A` | Slate `#84929F` | ❌ Not done |

**Brand spec reference:** `brand/visual-surfaces-draft.html § Inspector Panel`

### Gap 2: Typography — Still Segoe UI (MEDIUM)

Body font is still `"Segoe UI", Arial, sans-serif`. The brand spec calls for `"Cascadia Cove", "Cascadia Code", "Consolas", monospace` for field names/values (identifiers from `.precept` source). Review's Priority 2.

### Gap 3: Header State Display — Removed (MEDIUM, NEW)

The mockup showed `Current: Red` in the header as a `.status` labeled element. The current implementation removed this — the current state is only visible as the filled/highlighted node inside the SVG diagram. This is not a direct regression (the implementation is richer overall), but the PRD should make a deliberate call: is the current state surfaced in a text header label, in the diagram only, or both?

### Gap 4: Field Types Not Displayed (LOW)

The brand review's Priority 3 — field type info (e.g. `string`, `number`) is not shown next to field names. Not a blocker but useful for debugging type mismatches.

### Gap 5: Inspector Review is Partially Stale (NEW)

The `inspector-panel-review.md` was written at an earlier implementation snapshot. It **misses entirely**:
- Rule violations banner (orange)
- State-rules indicator badge
- Draft validation banner
- Edit mode (Edit/Save/Cancel with live draft inspection)
- Null toggle for nullable fields
- Field-level rule icons

The color mismatch section of the review is still accurate. The "what was found" structural description is significantly incomplete. The PRD should use the implementation file itself as source of truth, not the review doc.

### Gap 6: No JSON Export / `getInspectorState()` (LOW, AI-FIRST)

The review recommended exposing a `getInspectorState()` function for AI consumption (structured `{ currentState, fields, violations }` JSON). Not implemented. The current surface is parseable via DOM but not programmatically exposed.

### Gap 7: `SaveInstance` / `ReplayInstance` Commands (LOW, FUTURE)

The archived design spec's command contract included `SaveInstance(path?)` and `ReplayInstance`. The extension has no save-instance flow. The `replay` action appears in the TypeScript `PreviewAction` type but its full behavior is unconfirmed. These were explicitly marked "future build" in the archived spec.

---

## 3. Source-of-Truth File Paths for PRD Author

| Purpose | File |
|---|---|
| Full webview implementation | `tools/Precept.VsCode/webview/inspector-preview.html` |
| Extension host (commands, panel lifecycle, message protocol) | `tools/Precept.VsCode/src/extension.ts` |
| Mockup (original UX contract — still useful for interaction spec) | `tools/Precept.VsCode/mockups/interactive-inspector-mockup.html` |
| Archived behavior spec | `docs/archive/InteractiveInspectorMockup.md` |
| Brand review (color/typography gaps; partial feature coverage) | `brand/inspector-panel-review.md` |
| Brand color/visual system reference | `brand/visual-surfaces-draft.html` |

---

## 4. Recommendation on `inspector-panel-review.md`

**Refresh it.** The color/typography gap analysis is still accurate and should be kept. The structural "what was found" section needs a full update to reflect the current feature set (edit mode, rule violation banners, state-rules indicator, field icons, null toggles). The AI-first JSON export recommendation should be elevated to a clearer requirement. The review doc is valuable input but must not be used as a complete feature inventory — it predates the current feature set by a significant margin.

---

## Decision Needed

Before PRD authoring proceeds, the following open questions should be resolved:

1. **Color migration priority** — Is the brand color migration (all 7 gaps in Gap 1) a PRD requirement for the redesign, or a separate polish pass?
2. **Current state in header** — Should the current state label be restored to the header, or is diagram-only sufficient?
3. **JSON export** — Is `getInspectorState()` a PRD requirement (AI-first contract) or nice-to-have?
4. **SaveInstance scope** — Is instance save/load in scope for this PRD or explicitly deferred?

---

# Preview panel redesign board blocked on GitHub project scopes

- Requested outcome: create a GitHub project board for the preview panel redesign in the `sfalik` owner context for `sfalik/Precept`.
- What I verified: the active `gh` auth can access the repo but only carries `gist`, `read:org`, `repo`, and `workflow` scopes.
- Blocking fact: GitHub Projects v2 listing fails without `read:project`, and creation requires `project`, so the board cannot be created or verified visible from this session as-is.
- Fallback check: the legacy repo-project REST endpoint for `sfalik/Precept` is not available here (`404`), so there is no classic-project escape hatch.
- PM decision: treat the preview-panel board as blocked pending auth refresh, not as deferred product scope.
- Unblock: refresh `gh` auth with `project,read:project`, then create `Preview Panel Redesign`, add a short description/readme, link `sfalik/Precept`, and confirm it appears in the owner's project list.

---

# UX Decision: Preview Reimagined — Phase 2 (Five More Directions)

**Author:** Elaine (UX Designer)
**Date:** 2026-04-07
**Status:** EXPLORATION — awaiting Shane review
**Artifacts:** `tools/Precept.VsCode/mockups/preview-reimagined-index.html` (updated index with all ten)

---

## Context

Shane loved the first five reimagined preview concepts and asked for five more to push diversity further — bringing the total to ten. These five explore interaction models the first batch didn't cover: comparison, governance, spatial canvas, scenario testing, and dense monitoring.

---

## The Five New Concepts

### 06 — Dual-Pane Diff
**File:** `preview-reimagined-06-dual-pane-diff.html`
**Metaphor:** Compare any two states side by side.

Pick two states (or two history moments) and see what differs: events, data, rules. Like a code diff but for state-machine snapshots.

**Strengths:**
- Immediately answers "what changes between A and B?" with visual precision
- Developers already think in diffs — this is a natural mental model
- Useful for both debugging (compare before/after a transition) and design review
- Could be a feature within any primary shape, not just standalone

**Weaknesses:**
- Only shows two points at a time — less useful for understanding the whole machine
- With only 3 states and 2 fields, the Subscription sample underplays its value
- More of a utility than a primary product shape
- Selector UX needs careful thought for precepts with 10+ states

---

### 07 — Rule Pressure Map
**File:** `preview-reimagined-07-rule-pressure-map.html`
**Metaphor:** Constraints as the organizing principle.

Every rule/invariant/assertion is a tile with health status, pressure indicators, driving fields, and "what would violate this?" scenarios.

**Strengths:**
- Only view that inverts the usual state-first or event-first model
- Uniquely answers "is my machine safe?" before asking "where am I?"
- Pressure bars and violation scenarios give proactive governance
- Scales beautifully with complex precepts that have many business rules

**Weaknesses:**
- Not useful for simple precepts with 1-2 rules (feels sparse)
- Doesn't show state transitions or event outcomes directly
- Novel concept — users may not immediately understand the pressure metaphor
- Requires static analysis to detect "at risk" vs "passing" automatically

---

### 08 — Graph Canvas
**File:** `preview-reimagined-08-graph-canvas.html`
**Metaphor:** The diagram IS the interface.

A full-bleed, zoomable, pannable 2D canvas with interactive state nodes and event edge labels. Direct manipulation: click to enter, click to fire, drag to rearrange.

**Strengths:**
- Spatial understanding is immediate — you see the whole topology
- Direct manipulation (click node to enter, click edge to fire) is deeply intuitive
- Zoom/pan handles any size of precept, from 3 states to 30
- Data overlay keeps field values accessible without cluttering the spatial view

**Weaknesses:**
- Auto-layout for complex precepts is a hard technical problem
- Data and rules are secondary — float as overlays rather than being primary citizens
- VS Code panel real estate is limited for a full canvas experience
- Requires significant rendering infrastructure (SVG/Canvas library)

---

### 09 — Storyboard / Scenario Builder
**File:** `preview-reimagined-09-storyboard-scenarios.html`
**Metaphor:** Build and replay named event sequences.

A vertical storyboard where each step is a card showing event, args, outcome, and data snapshot. Save/name/replay scenarios. Coverage bar shows what paths you haven't tested yet.

**Strengths:**
- Only concept that treats the preview as a test harness
- Coverage tracking ("2 of 4 transitions covered") is immediately useful for QA
- Scenario library enables saving, sharing, and comparing paths
- Natural complement to Timeline (01): Timeline shows what happened, Storyboard plans what to test
- Saved scenarios could seed automated test suites

**Weaknesses:**
- Building scenarios step-by-step is slower than live fire-and-inspect
- Scenario library needs persistence infrastructure
- The storyboard view is vertical — long scenarios scroll extensively
- Less useful for exploratory "just playing around" usage

---

### 10 — Dashboard / Control Room
**File:** `preview-reimagined-10-dashboard-control-room.html`
**Metaphor:** All signals at once.

Multiple independently useful widgets: state summary, event heatmap, field value sparklines, constraint health, and an activity feed. An instrument panel for complex precepts.

**Strengths:**
- Maximum information density — power users can see everything without drilling
- Each widget is independently useful and could be mixed into other shapes
- Heatmap is a compact version of the Decision Matrix (03) with less overhead
- Sparklines show field value history — a visual dimension no other concept offers
- Activity feed doubles as a history log

**Weaknesses:**
- Dense UIs can overwhelm new users — steep learning curve
- Six widgets in a VS Code panel is ambitious for screen real estate
- Subscription sample (3 states, 2 fields) is too small to fully demonstrate the value
- Widget layout may need to be configurable — one size won't fit all precepts

---

## Updated Recommendation

The original top picks hold: **01 (Timeline)** and **05 (Notebook)** remain the strongest primary shapes for their combination of debugger power and comprehensive coverage.

Phase 2 adds one compelling new primary contender:

**09 (Storyboard / Scenarios)** is the standout. It's the only concept that frames the preview as a testing and verification surface — not just observation. A **Timeline + Storyboard hybrid** (history for debugging, named scenarios for verification, coverage tracking for completeness) would be uniquely powerful and differentiated from any other VS Code extension.

**07 (Rule Pressure Map)** introduces a genuinely novel lens worth pursuing as a secondary mode. For precepts with complex business rules, seeing constraints as the organizing principle rather than states is the fastest path to "is my machine correct?"

**10 (Dashboard)** is the power-user play — strong for complex precepts, potentially overwhelming for simple ones. Worth prototyping as a "command center" mode.

**06 (Dual-Pane Diff)** and **08 (Graph Canvas)** are strong utility concepts that could be features within whatever primary shape is chosen, rather than standalone product shapes.

---

## Design Space Summary (All 10)

| # | Concept | Primary Lens | Unique Strength |
|---|---------|-------------|-----------------|
| 01 | Timeline Debugger | Time/history | "How did I get here?" |
| 02 | Conversational REPL | Text/commands | AI-native, greppable |
| 03 | Decision Matrix | Completeness | Full truth table at a glance |
| 04 | Focus / Spotlight | Present moment | Maximum clarity, minimal UI |
| 05 | Notebook / Report | Document/sections | Complete coverage, shareable |
| 06 | Dual-Pane Diff | Comparison | Visual diff between any two states |
| 07 | Rule Pressure Map | Governance/rules | Constraints-first, proactive safety |
| 08 | Graph Canvas | Spatial/topology | The diagram IS the interface |
| 09 | Storyboard / Scenarios | Testing/workflows | Build, save, replay, coverage |
| 10 | Dashboard / Control Room | Multi-signal | Everything at once, data-dense |

---

## Review Needed

- **Shane:** Across all ten — which resonated? Phase 2 standouts?
- **Peterman:** Brand compliance check on 06–10. All use the locked palette, but new interaction patterns (diff markers, pressure bars, canvas, sparklines) should be reviewed.
- **Frank:** Architecture implications — 09 needs scenario persistence, 07 needs static analysis for pressure scoring, 08 needs canvas rendering. Which are cheapest to prototype?

---

## Files Created / Updated

| File | Description |
|------|-------------|
| `tools/Precept.VsCode/mockups/preview-reimagined-06-dual-pane-diff.html` | Concept 06 |
| `tools/Precept.VsCode/mockups/preview-reimagined-07-rule-pressure-map.html` | Concept 07 |
| `tools/Precept.VsCode/mockups/preview-reimagined-08-graph-canvas.html` | Concept 08 |
| `tools/Precept.VsCode/mockups/preview-reimagined-09-storyboard-scenarios.html` | Concept 09 |
| `tools/Precept.VsCode/mockups/preview-reimagined-10-dashboard-control-room.html` | Concept 10 |
| `tools/Precept.VsCode/mockups/preview-reimagined-index.html` | Updated to include all ten + revised recommendation |

All first-five mockups are preserved. Shared CSS unchanged.

---

# UX Decision: Preview Reimagined — Five Alternative Directions

**Author:** Elaine (UX Designer)
**Date:** 2026-04-07
**Status:** EXPLORATION — awaiting Shane review
**Artifacts:** `tools/Precept.VsCode/mockups/preview-reimagined-index.html` (index linking all five)

---

## Context

Shane asked me to think outside the box and reimagine what the preview could look like. The current preview surface (header + diagram + data lane + event dock) is functionally strong but architecturally fixed. This exploration asks: what if the core interaction model were fundamentally different?

All five concepts serve the same core jobs:
1. Understand the current state and what transitions are available
2. Inspect current data and active rule pressure
3. Try events and immediately understand outcomes
4. Support real debugging work inside VS Code

---

## The Five Concepts

### 01 — Timeline Debugger
**File:** `preview-reimagined-01-timeline-debugger.html`
**Metaphor:** Time is the primary axis.

A horizontal timeline of fired events dominates the top. Click any point to see state + data at that moment. Below: a split view of data diffs (what changed) and available next actions.

**Strengths:**
- Uniquely answers "how did I get here?" — no other concept does this
- Data diffs at each step are immediately useful for debugging
- Scrubbing through history is natural for complex multi-step workflows
- Timeline is a visual debugger pattern developers already know

**Weaknesses:**
- History requires runtime tracking infrastructure the extension doesn't have yet
- Timeline gets unwieldy for 20+ step sessions
- Diagram is absent — spatial mental model is lost
- Initial view (no history yet) is sparse

---

### 02 — Conversational REPL
**File:** `preview-reimagined-02-conversational-repl.html`
**Metaphor:** Type events, read results.

A scrolling command log replaces the diagram. State + data live in a compact sidebar. You type event commands and the system responds with structured outcome blocks.

**Strengths:**
- AI agents would consume this format natively — structured input/output
- Text-first means everything is greppable, copiable, shareable
- Builds on terminal/REPL familiarity developers already have
- Conversation log is a natural audit trail

**Weaknesses:**
- No spatial overview — you lose the diagram's "where am I in the machine?" view
- Scrolling log gets long; hard to see current state at a glance after 10+ actions
- Typing event names + args is slower than clicking
- Novel for VS Code panels — users expect visual content, not a terminal

---

### 03 — Decision Matrix
**File:** `preview-reimagined-03-decision-matrix.html`
**Metaphor:** Every outcome in one table.

State × Event truth table. Rows = states, columns = events, cells = outcomes. Click a cell to inspect detail in a side panel.

**Strengths:**
- Only view that shows the FULL contract at once — completeness at a glance
- Immediately reveals undefined transitions, blocked paths, dead ends
- Great for design review ("is this machine correct?")
- Table structure is inherently accessible and AI-parseable

**Weaknesses:**
- Scales poorly for precepts with many states/events (10×10+ gets cramped)
- Doesn't show "current state" as strongly — it's just a row highlight
- Static feel — less useful for live debugging, more for contract review
- Data and fields are secondary; not a data-first view

---

### 04 — Focus / Spotlight
**File:** `preview-reimagined-04-focus-spotlight.html`
**Metaphor:** One thing at a time, large and clear.

Current state name is massive, center-screen. Available transitions radiate outward as interactive cards. Data orbits beneath. Minimal chrome.

**Strengths:**
- Absolute clarity about current state — no ambiguity
- Cards for each path provide rich context without clutter
- Context-adaptive: the view can shift based on last action
- Beautiful, zen-like — great for demos, presentations, first impressions

**Weaknesses:**
- Scales poorly: 5+ events would overflow the horizontal cards
- No diagram, no history — only shows the present moment
- Feels light on information density for power users
- Data section is too compact for precepts with 10+ fields

---

### 05 — Notebook / Report
**File:** `preview-reimagined-05-notebook-report.html`
**Metaphor:** A live, scrollable document.

Vertical card-based sections: contract overview, current state, data, events, rules, mini diagram. Progressive disclosure via expand/collapse.

**Strengths:**
- Complete coverage of every aspect in one scrollable view
- Progressive disclosure handles complexity well (collapse what you don't need)
- Readable, shareable, potentially printable for review sessions
- Card structure maps naturally to AI agent consumption (section by section)
- Accommodates precepts of any size — just adds more cards

**Weaknesses:**
- Vertical scrolling means the diagram is "below the fold"
- Less immediate than the current surface for quick fire/inspect loops
- Can feel long and document-heavy for simple precepts
- Card-based layout is common but not distinctive

---

## Recommendation

**Deeper iteration:** Concepts **01 (Timeline Debugger)** and **05 (Notebook / Report)**.

The Timeline gives debugger-grade "how did I get here?" power that no current design offers. The Notebook gives complete, readable coverage that works for understanding, sharing, and AI consumption. A hybrid — notebook structure with an embedded timeline and inline event execution — could be the strongest future direction.

**Strong secondary:** Concept **03 (Decision Matrix)** as a mode/tab alongside whatever primary shape is chosen. The "show me everything" view is uniquely valuable for contract review and catches problems the other views miss.

---

## Review Needed

- **Shane:** Which concepts resonate? Which feel right for the product?
- **Peterman:** Brand compliance check on all five — all use the locked palette, but card layouts, typography hierarchy, and visual density vary.
- **Frank:** Architecture implications — especially 01 (needs history tracking) and 02 (needs text input parsing). Which are cheapest to prototype?

---

## Files Created

| File | Description |
|------|-------------|
| `tools/Precept.VsCode/mockups/preview-reimagined-index.html` | Index linking all five concepts |
| `tools/Precept.VsCode/mockups/preview-reimagined-01-timeline-debugger.html` | Concept 01 |
| `tools/Precept.VsCode/mockups/preview-reimagined-02-conversational-repl.html` | Concept 02 |
| `tools/Precept.VsCode/mockups/preview-reimagined-03-decision-matrix.html` | Concept 03 |
| `tools/Precept.VsCode/mockups/preview-reimagined-04-focus-spotlight.html` | Concept 04 |
| `tools/Precept.VsCode/mockups/preview-reimagined-05-notebook-report.html` | Concept 05 |
| `tools/Precept.VsCode/mockups/preview-reimagined-shared.css` | Shared brand-aligned styles |

All existing mockups are preserved.

---

# Frank — language proposal review

Date: 2026-04-05

## Decision summary

Reviewed issues `#8` through `#13` and added architectural comments directly on GitHub.

## Recommended sequencing

1. **First wave**
   - `#10` String length accessor
   - `#8` Named guard declarations
2. **Second wave**
   - `#9` Ternary expressions in `set` mutations
   - `#11` Event argument absorb shorthand
3. **Last wave / explicit architectural review required**
   - `#12` Inline guarded fallback (`else reject`)
   - `#13` Field-level range/basic constraints

## Architectural conclusions

- `#10` is the safest proposal: it extends the existing expression/member-access model and should be treated as the string analogue of collection `.count`.
- `#8` is a good early declaration-form addition if scoped as reusable boolean symbols, not macros and not new control flow.
- `#9` is acceptable if it remains strictly about value selection in `set` RHS positions; it must not become a disguised outcome-branching feature.
- `#11` should be an explicit action keyword that desugars to ordered `set` operations. No hidden header inference; ambiguous mappings must fail closed.
- `#12` must stay syntax sugar for an existing guarded-row-plus-reject-row pair. Do not let it widen into general inline branching.
- `#13` is the highest-risk proposal because it pressures the DSL's keyword-anchored grammar. If no syntax preserves that discipline cleanly, the correct answer is to reject the feature.

## Guardrails for later design work

- Preserve **keyword-anchored flat statements** as a first-class design constraint.
- Preserve **top-to-bottom first-match routing**; do not trade concision for a muddier control-flow model.
- Favor **desugaring to existing semantic forms** where possible so runtime, MCP, diagnostics, and tooling stay aligned.
- Keep proposals narrowly scoped. None of these should be allowed to smuggle in regex validation, hierarchical states, or generalized inline branching.

---

# Steinbrenner — Language Proposal Intake

**Date:** 2026-04-05
**Requested by:** Shane

## Framing

Created GitHub Project v2 **Precept Language Improvements** and loaded the first six proposal issues there so the language roadmap has a single queue:

- Project: https://github.com/users/sfalik/projects/2

## Proposal set

This six-issue bundle preserves the strongest remembered set from the DSL expressiveness research plus the hero-sample condensation pass:

1. Direct expressiveness proposals already ranked in research:
   - #8 — Proposal: Named guard declarations
   - #9 — Proposal: Ternary expressions in set mutations
   - #10 — Proposal: String length accessor
2. Hero-condensation / verbosity reducers that repeatedly surfaced in corpus review:
   - #11 — Proposal: Event argument absorb shorthand
   - #12 — Proposal: Inline guarded fallback (`else reject`)
   - #13 — Proposal: Field-level range/basic constraints

## Caveat to carry forward

Issue #13 is intentionally included even though it is the weakest of the six from a design-fit standpoint. The research explicitly flags field-inline constraints as being in tension with Precept's keyword-anchored statement model, so that issue should be treated as a proposal to evaluate carefully, not as a presumed roadmap commitment.

## Sequencing note

If we want a fast first pass on language value vs. implementation cost, the clean review order is:

1. #8 Named guards
2. #9 Ternary in `set`
3. #10 String `.length`
4. #11 Event absorb shorthand
5. #12 Inline `else reject`
6. #13 Field-level basic constraints (caveated)

---

---

# Decision Inbox Merge — 2026-04-05T15:10:06Z

Merged from `.squad/decisions/inbox/`.

- Appended: 3
- Skipped as duplicates: 0
- Files: steinbrenner-expand-language-proposals.md, frank-expand-language-proposals.md, elaine-kanban-preview-concept.md

---

# Steinbrenner — expand language proposal issue bodies

**Date:** 2026-04-05
**Requested by:** Shane

## Decision

Standardize the main bodies of language proposal issues `#8`, `#9`, and `#10` around one comparison-friendly format:

1. Problem / motivation
2. Proposed feature
3. Precept today / pain point example
4. Proposed Precept syntax example
5. Reference example(s) from research
6. Benefits
7. Open questions / risks

## Why

The original issue bodies were good stubs, but they were too thin for architecture review and sequencing. These proposals are easier to evaluate when each issue shows:

- what authors struggle with today,
- what the proposed syntax would look like,
- how similar pressure is handled in a known reference system,
- and what risks must stay in scope.

## Guardrail

All proposed DSL snippets in issue bodies must be labeled as hypothetical / not implemented today so the roadmap does not read like shipped behavior.

---

# Decision Record: Language Proposal Issues #11, #12, #13 — Body Expansion

**Author:** Frank (Lead/Architect)
**Date:** 2026-04-05
**Status:** Documented — no implementation authorized

---

## What Was Done

Expanded the body of GitHub issues #11, #12, and #13 from acceptance-criteria placeholders into full proposal writeups. Each issue now contains:

- A "pain today" section with 2–3 concrete Precept examples drawn from real sample files
- Proposed syntax with before/after comparison
- Reference-language code examples (xstate, Polly, Zod, FluentValidation)
- Architectural cautions specific to each proposal's risk profile
- A scoping recommendation with explicit wave placement

---

## Wave Placement Reaffirmed

| Issue | Proposal | Wave | Risk |
|-------|----------|------|------|
| #11 | `absorb Event` shorthand | Last | Medium — name-matching semantics, maintenance hazard |
| #12 | `else reject` inline fallback | Second-to-last | Medium-high — first-match semantics pressure, multi-guard interaction undefined |
| #13 | Field-level range constraints | Last | High — keyword-anchor principle conflict (Shape A) or new keyword sprawl (Shape B) |

---

## Key Architectural Positions

### #11 — `absorb`
- `absorb` must be event-scoped (`absorb Submit`, not bare `absorb`). Bare absorb is unsafe in multi-event precepts.
- Explicit `set` always wins over `absorb` for the same field. This is the precedence rule.
- Language server must warn when `absorb EventName` maps zero fields. Silent zero-match is a maintenance hazard.

### #12 — `else reject`
- Scope is deliberately narrow: `else reject` only; not `else transition`, not `else set`.
- Only one `else reject` permitted per event+state pair. Multi-guard scenarios must use standalone fallback rows.
- The "what fires if both guards fail in a multi-else-reject scenario" question must be resolved in a design document before implementation. The safest answer: error at compile time.

### #13 — Field-level constraints
- Shape A (`field Amount as number default 0 min 0`) violates the keyword-anchor principle. The research README already rejected this shape. Adopting it requires an explicit decision to accept the violation.
- Shape B (`constrain Amount min 0`) preserves the principle but creates two constraint pathways. Clear documentation of when to use `constrain` vs `invariant` is required.
- Neither shape is implementation-ready. This is a recorded gap. Shane must choose a shape before any code is written.

---

## No Implementation Authorized

These issues are in research/proposal state. No implementation agent should begin work on any of these features until:
1. A formal design document exists for the chosen approach
2. Shane has explicitly signed off

This record is informational — it documents the architectural positions taken in the issue bodies.

---

# UX Decision: Kanban Board Preview Concept (Concept 11)

**Author:** Elaine (UX Designer)
**Date:** 2026-04-07
**Status:** Proposed — awaiting Shane review
**Scope:** Preview reimagined exploration, Phase 3

## Context

Shane asked for a kanban board concept to add to the preview exploration set (Concepts 01–10). The design space already covered debugger-first (Timeline), document-first (Notebook), table-first (Decision Matrix), test-first (Storyboard), governance-first (Rule Pressure Map), dashboard-first (Control Room), spatial-first (Graph Canvas), and others.

## What Makes Kanban Genuinely Distinct

The kanban metaphor's organizing principle is **flow through lifecycle stages**. No other concept in the set uses this lens:

- **States are columns**, arranged left to right in lifecycle order
- **The entity is a card** that physically lives in its current state's column
- **Transitions are connectors between columns**, with guard conditions and event names visible on the arrows
- **Ghost cards** show where the entity was previously, giving history context without a separate timeline
- **Terminal states** are columns with no outgoing connectors — visually obvious
- **Self-loops** (events that don't change state) appear as a loop indicator inside the current column

This is different from:
- **Timeline (01):** Time-first, horizontal history. Kanban is position-first, spatial.
- **Decision Matrix (03):** Complete enumeration in a table. Kanban shows only the flow paths.
- **Graph Canvas (08):** Free-form spatial diagram. Kanban is structured left-to-right columns.
- **Storyboard (09):** Linear step sequence. Kanban shows all states simultaneously.
- **Dashboard (10):** Multi-widget overview. Kanban is a single cohesive metaphor.

## Key UX Rationale

1. **"Where am I?" is answered instantly.** The entity card's column position tells you the current state without reading any text. This is the fastest answer to the most common question.

2. **AI-parseable by design.** An AI agent can express the preview state as "entity at column Active, transitions available: Activate (self-loop), Cancel (→ Cancelled)." The spatial-positional model maps cleanly to text descriptions.

3. **Guard conditions on connectors, not buried in event detail.** The constraint surface is visible in the transition arrows themselves — you see what guards each lane change without drilling into an event card.

4. **Ghost cards give history without a separate timeline.** Previous positions are visible as dashed outlines in earlier columns, preserving the "how did I get here?" context without a dedicated history axis.

5. **Terminal states are visually obvious.** An empty column with no outgoing connectors reads as a dead end. No label needed — the structure communicates it.

6. **Self-loops stay local.** Events that don't change state (like Activate from Active) appear as a dashed loop inside the column, not as a transition connector. This keeps the flow arrows clean.

## Limitations and Honest Tradeoffs

- **Scales poorly past ~6 states.** Columns get narrow or require horizontal scrolling. For complex precepts with many states, this becomes a kanban board with too many columns — the spatial advantage diminishes.
- **Non-linear lifecycles are awkward.** If a precept has many bidirectional transitions (A↔B↔C), the left-to-right flow metaphor breaks down. The concept works best for roughly linear or funnel-shaped lifecycles.
- **Data density is lower than Dashboard or Decision Matrix.** The entity card shows fields and rules, but there's only one card on the board at a time. You trade density for clarity.
- **Event arguments aren't surfaced directly.** Unlike the Notebook or Timeline concepts that show inline arg inputs, the kanban connector arrows are too compact for arg entry. You'd need a click-to-expand pattern for events with arguments.

## Recommendation

Concept 11 is a solid addition to the exploration set. It's the most intuitive concept for simple-to-moderate lifecycles (3–5 states, roughly linear flow) — which describes the majority of real-world precepts.

**Best used as:** A primary view for simpler precepts, or a "lifecycle overview" mode alongside Timeline (for debugging) and Storyboard (for testing) in a multi-mode preview.

**Not a replacement for:** Timeline (debugging), Storyboard (testing), or Dashboard (monitoring complex machines). Those solve different jobs.

## Files

- Mockup: `tools/Precept.VsCode/mockups/preview-reimagined-11-kanban-board.html`
- Index (updated): `tools/Precept.VsCode/mockups/preview-reimagined-index.html`

---

# PR #35 Merge: README Cleanup and Squad Decision Recording

**Date:** 2026-04-07T23:40:00Z
**Actor:** Frank (Lead/Architect)
**Outcome:** MERGED to main

## Summary

Merged PR #35 containing README Quick Example refactoring and Squad orchestration recording. The branch `chore/upgrade-squad-latest` carried 2 commits that finalized the README contract cleanup decision (removing explanatory hedge and copyable DSL block, fixing image sizing) and recorded the orchestration outcome.

## What Merged

- **Commit 1759b33:** Scribe post-task recording for PR #34 merge
  - Updated `.squad/agents/frank/history.md` with PR #34 learnings
  - Recorded decisions in `.squad/decisions.md` (Squad config + README image fix outcome, user directive to keep branch open)
  - Cleaned up merged inbox entries

- **Commit 3798d92:** .squad merge J. Peterman README contract cleanup decision
  - Updated `.squad/agents/j-peterman/history.md` with team update
  - Merged `j-peterman-readme-contract.md` decision into `.squad/decisions.md`
  - Added README Quick Example refactoring decision record

## PR Details

- **PR:** #35 "chore: finalize README cleanup and record Squad decision"
- **Base:** main
- **Head:** sfalik:chore/upgrade-squad-latest
- **Status:** Merged via merge commit
- **Changes:** 81 additions, 3 files changed (README.md, .squad/decisions.md, .squad/agents/j-peterman/history.md)
- **Checks:** None (no blocking checks on this PR)

## Key Decisions

1. **Clean working tree before push:** Separated uncommitted Squad sync artifacts from committed README cleanup work. Reset working tree to HEAD, removed untracked workflow files, then pushed the 2 clean commits.

2. **Explicit PR creation:** Used `gh pr create --base main --head sfalik:chore/upgrade-squad-latest` to ensure correct target branch despite branch name potentially matching multiple remotes.

3. **Branch retention:** Per user directive, branch was NOT deleted post-merge. It remains available locally and remotely for future work in this upgrade cycle.

## Scope Verification

✓ Zero scope creep — only the README cleanup commits from the branch were included
✓ No unrelated code changes
✓ Co-authored-by trailer included in original commits

## Next Steps

Branch `chore/upgrade-squad-latest` is available for additional Squad upgrade work. Main now carries the finalized README Quick Example refactoring and complete orchestration record.


## MERGED INBOX BACKLOG — 2026-04-08T13:29:23Z


---

### 2026-04-05T17:17:10Z: User directive
**By:** shane (via Copilot)
**What:** Treat `deferred` as a general issue state, not a proposal-only status.
**Why:** User request — captured for team memory

---

### 2026-04-05T23:29:46Z: User directive
**By:** shane (via Copilot)
**What:** For preview UX mockups, Elaine should build her own mockups directly instead of handing them to Kramer first. The UX direction should stay less radical unless explicitly requested, and revisions should preserve useful prior structure such as tile-based layouts when that better serves review.
**Why:** User request — captured for team memory

---

### 2026-04-06T00:59:52-04:00: User directive
**By:** Shane Falik (via Copilot)
**What:** Mockup/UI refinement work should be routed through Elaine rather than handled inline by the coordinator.
**Why:** User expects UX-facing mockup changes to stay with Elaine's design ownership.

---

### 2026-04-06T03:08:24.1183672-04:00: User directive
**By:** shane (via Copilot)
**What:** Keep the small dot inside the new rounded card shape for the brand mark.
**Why:** User request — captured for team memory

---

### 2026-04-06T08:05:19.9729098-04:00: User directive
**By:** shane (via Copilot)
**What:** In the brand mark, the two state shapes should be the same size and square shaped.
**Why:** User request — captured for team memory

---

### 2026-04-06T08:09:46.6698857-04:00: User directive
**By:** shane (via Copilot)
**What:** Center the dot everywhere it is used. When combined with text, shift the dot left so it precedes the text, keep the dot vertically centered, and never wrap; widen the box as needed so the pattern reads like "dot + label" on one line.
**Why:** User request — captured for team memory

---

### 2026-04-06T11:15:40.0468360-04:00: User directive
**By:** shane (via Copilot)
**What:** Delete both live design HTML documents and restart them from scratch. Elaine creates and maintains both `design/brand/brand-spec.html` and `design/system/foundations/semantic-visual-system.html` as the visual and creative owner, with explicit emphasis on beauty, consistency, and a strong shared visual system. Peterman remains brand owner, provides brand and semantic-system input based on his research, and collaborates so the brand spec leverages the design system to showcase the brand beautifully.
**Why:** User wants a cleaner ownership model where visual execution is centralized under Elaine while brand authority still informs the work through Peterman collaboration.

---

### 2026-04-08T06:22:34Z: User directive
**By:** shane (via Copilot)
**What:** Build a durable language research corpus organized by domain rather than issue number; use `computed-fields.md` as the quality bar; do not edit proposal bodies in `temp/issue-body-rewrites/`; create and expand research docs only; include domains with no current proposal if they are clearly on the horizon; maximize parallel agent utilization; commit research files after each batch completes; update the language research indexes so every open proposal points to its grounding and every research doc lists the proposals it informs.
**Why:** User request — captured for team memory

---

### 2026-04-05T18:05:14Z: User directive
**By:** shane (via Copilot)
**What:** Keep mockup / UX work in its own PR separate from the workflow closeout work.
**Why:** User request — captured for team memory

---

### 2026-04-05T18:21:47Z: User directive
**By:** shane (via Copilot)
**What:** Bundle all remaining work on the current branch into a PR, push it, and leave the branch ready to delete so new tasks can start from a clean place.
**Why:** User request — captured for team memory

---

# Decision: Interactive Journey Prototype (Concept 17)

**Filed by:** Elaine (UX Designer)
**Date:** 2026-05-02
**Status:** Proposed — awaiting Shane review

## Decision

Created **Concept 17 — Interactive Journey** as the first clickable prototype in the mockup series. This is a playable synthesis of Concepts 14 (Contract Explorer), 15 (Journey), and 16 (Navigator) that lets Shane click through the full InsuranceClaim lifecycle in the browser.

## What It Does

- **Fire events**: Click "Fire ▶" on any available event to advance the simulation. Events accept typed arguments (text inputs, number inputs, boolean dropdowns).
- **State transitions**: The precept transitions between Draft → Submitted → UnderReview → Approved/Denied → Paid, exactly matching the `insurance-claim.precept` definition.
- **History accumulation**: Each fired event appends to the Past trail with step number, event name, arguments, and data deltas (field before→after values).
- **Data provenance**: Current state's data grid shows "← set at #N" annotations connecting each field's value to the step that set it. Recently-changed fields glow green.
- **Guard enforcement**: Approve is guarded by `(!PoliceReportRequired || MissingDocuments.count == 0) && Amount ≤ ClaimAmount`. Invalid fires show toast rejection messages.
- **Edit-in-place**: FraudFlag is toggleable in UnderReview state via inline toggle, matching the `in UnderReview edit FraudFlag` declaration.
- **Undo/Reset**: Undo reverses one step with full data restoration. Reset returns to Draft initial state.
- **Topology rail**: Left rail shows all 6 states with visited/current/unvisited visual layers and step badges.
- **Breadcrumb trail**: Header tracks the unique-state path through the machine.
- **Terminal states**: Denied and Paid show completion summaries with journey statistics.

## Interaction Patterns Established

1. **Fire-and-observe**: The core loop is "fill args → click Fire ▶ → watch Past grow + Present update + Future recalculate." This should be the primary interaction pattern for any production implementation.
2. **Toast feedback**: Transitions show green toasts ("▶ Submit → Submitted"), self-loops show muted toasts ("↻ stayed in UnderReview"), rejections show red toasts ("✕ guard message"). Quick, non-modal feedback.
3. **Temporal scroll**: Past section grows upward from the origin, Present is the hero in the middle, Future shows what's next. The scroll naturally centers on Present after each step.
4. **Data provenance as micro-annotation**: "← set at #N" is a low-cost annotation that connects current field values to their causal step without requiring a separate history view. This pattern should carry into any production implementation.
5. **Undo as time travel**: Single-step undo with full data restoration. Not scrubbing — just "oops, let me try a different path."

## Files

- `tools/Precept.VsCode/mockups/preview-reimagined-17-interactive-journey.html` — the interactive prototype
- `tools/Precept.VsCode/mockups/preview-reimagined-index.html` — updated to 17 concepts with interactive prototype at the top

## Suggested Play-Through

1. **Submit**(Claimant: "Jane Doe", Amount: 5000) → Submitted
2. **AssignAdjuster**(Name: "Bob Smith") → UnderReview
3. **RequestDocument**(Name: "Police Report") → stays in UnderReview
4. Try **Approve**(Amount: 4500) — rejected! MissingDocuments not empty
5. **ReceiveDocument**(Name: "Police Report") → stays, MissingDocuments clears
6. **Approve**(Amount: 4500, Note: "Approved after review") → Approved
7. **PayClaim** → Paid (terminal, journey complete)

Alternative path at step 6: **Deny**(Note: "Insufficient evidence") → Denied

## Relationship to Existing Concepts

- **14 (Contract Explorer)** remains the leading direction for static preview design — it has pipeline trace depth and what-if that this prototype doesn't replicate.
- **15 (Journey)** provided the temporal layout pattern (Past/Present/Future).
- **16 (Navigator)** provided the topology rail with visited/current/unvisited layers.
- **17 combines** these into the first prototype that can be *experienced*, not just *viewed*.

## Next Steps

- Shane plays through the prototype and provides feedback on the interaction feel.
- If the fire-and-observe pattern feels right, it validates the Journey-style layout for the production implementation.
- The topology rail interaction (clicking rail nodes to jump to states) could be a future enhancement.

---

# Re-Review: Computed Fields (#17) — Teachability & UX Clarity

**Reviewer:** Elaine (UX Designer)
**Date:** 2026-04-08
**Trigger:** Shane requested re-review after comprehensive 24-system research document was completed
**Inputs reviewed:**
- `temp/issue-body-rewrites/17.md` — proposal body
- `docs/research/language/expressiveness/computed-fields.md` — research doc (24 systems)
- `samples/travel-reimbursement.precept`, `samples/loan-application.precept`, `samples/insurance-claim.precept`, `samples/event-registration.precept` — existing DSL reading patterns
- George's prior feasibility review findings (session context)

---

## Verdict: APPROVE WITH REQUIRED CHANGES

The core design — bare `->` for derivation, read-only semantics, field-local scope — is teachable. The 24-system research strongly validates the syntax choice. The concept maps cleanly onto mental models users already have (spreadsheet formulas, database generated columns, C# expression-bodied properties). The required changes below are about making the proposal *self-teaching* rather than requiring readers to have read background material.

---

## Required Changes (Learnability Improvements)

### 1. Add a "How to Read This" callout with the `->` dual-use explanation

The `->` token appears in two contexts: action chains (`-> set X = Y -> transition Z`) and field derivation (`field Total as number -> A + B`). Both share Principle 11 ("results in"), but a first-time reader hitting `->` in a field declaration after seeing it in transition rows will momentarily stall. The proposal needs a 3-line callout:

> **Reading guide:** `->` always means "results in." In a transition row, `-> set X = Y` means "this action results in X becoming Y." In a field declaration, `-> A + B` means "this field results in A + B." A derived field is a formula that always results in a value — which is why it's read-only.

This is the single most important learnability addition. Without it, users must infer the connection themselves.

### 2. Show a realistic before/after using the travel-reimbursement sample

The motivation section shows the problem (manual synchronization), and the examples section shows computed field syntax — but they're disconnected. Add a single side-by-side before/after block using `RequestedTotal` from `travel-reimbursement.precept`:

**Before:** `field RequestedTotal as number default 0` plus `-> set RequestedTotal = Submit.Lodging + Submit.Meals + (Submit.Miles * Submit.Rate)` repeated in every relevant transition row.

**After:** `field RequestedTotal as number -> LodgingTotal + MealsTotal + MileageTotal` — declared once, always correct.

This is more persuasive than abstract examples because a reader can look at the existing sample file and see the pain firsthand.

### 3. Show computed fields interleaved with regular fields (realistic reading context)

The proposal's examples section shows computed fields in isolation. But in a real `.precept` file, they'll be interleaved:

```precept
field LodgingTotal as number default 0
field MealsTotal as number default 0
field MileageTotal as number default 0
field RequestedTotal as number -> LodgingTotal + MealsTotal + MileageTotal
field ApprovedTotal as number default 0
```

A new reader scanning this block needs to immediately distinguish "this one is different." The `default X` → `-> expression` visual shift is clear enough *with syntax highlighting* (where `->` and the expression would be colored differently). But in plain text — documentation, code review diffs, chat messages — the `->` is small and easy to miss compared to the longer `default` keyword. The proposal should acknowledge this and confirm that:
- Language server hover will show "Computed: LodgingTotal + MealsTotal + MileageTotal" (already in acceptance criteria — good)
- Syntax highlighting will visually distinguish derived fields (confirm with grammar sync)

### 4. Specify teachable error messages for all invalid combinations

The proposal lists 5+ invalid combinations as compile errors but doesn't show what a user would *see*. Error messages are the primary learning surface for constraint discovery. Add example diagnostics:

| Invalid code | Error message |
|---|---|
| `field X as number default 0 -> Y + Z` | "A field cannot have both a default value and a derivation. Use `default` for user-set fields or `->` for computed fields, not both." |
| `field X as number nullable -> Y + Z` | "Computed fields always produce a value and cannot be nullable. Remove `nullable` or remove the derivation." |
| `field X as number -> Y + Z` with `nonnegative` | "Field-level constraints apply to user-entered data. Computed fields are read-only derivations — use an `invariant` to validate the result instead." |
| `-> set TotalCost = 50` (set targeting computed) | "TotalCost is a computed field and cannot be assigned. Its value is always derived from: Quantity * UnitPrice." |
| `edit TotalCost` block | "TotalCost is a computed field and cannot appear in edit declarations. Computed fields are read-only." |
| `field A -> B` / `field B -> A` | "Circular dependency detected: A → B → A. Computed fields cannot reference each other in a cycle." |

These don't need to be locked to exact wording, but the *shape* of the message matters. Notice: the best messages explain *why* something is wrong and *what to do instead*. The constraint-exclusion message is especially important — it redirects to invariants, which is the correct alternative.

### 5. Surface the unique positioning claim in the proposal body

The research doc's strongest finding — "No system in the survey combines field-level derivation with lifecycle-aware constraint enforcement" — is buried in the cross-category pattern section. This should appear in the proposal's Motivation section, because it answers the implicit reader question: "Why would Precept add computed fields when databases/spreadsheets already have them?"

One sentence is enough: "Existing systems that support derivation (databases, spreadsheets, enterprise platforms) lack lifecycle-aware constraint enforcement. Precept would be the first to combine declared derivations with state-guarded invariants, ensuring computed values are always fresh when guards and assertions evaluate."

### 6. Clarify the null-safety learning path for users

George's review identified that the null-safety type checking rule is unspecified. From a teachability perspective, this matters because a user's *first* computed field is likely to reference a nullable field (common in the sample set — `EmployeeName`, `AdjusterName`, `DecisionNote` are all nullable). The error they see must be immediately understandable:

> "Computed field expression references nullable field 'EmployeeName'. Computed fields must always produce a value — use only non-nullable fields or collection accessors that guarantee a result."

The proposal should add this to the error message table and note that broader nullable-input support (e.g., null-coalescing operators) is explicitly deferred to a future proposal.

### 7. Add a one-line "mental model" sentence to the summary

The proposal's summary says "fields whose values are calculated from other fields and re-evaluated automatically." This is accurate but mechanical. Add one sentence that gives the reader an instant mental model:

> "Think of computed fields as spreadsheet formulas: you declare the formula once, and the value is always current."

Spreadsheets are the most universally understood precedent from the research survey, and the analogy is exact.

---

## Non-Blocking Observations

- **Research strengthens the `->` choice.** The 24-system survey shows every derivation system converges on "declared once, read-only, auto-recomputed." The bare `->` is compact and consistent with Precept's existing vocabulary. No competing system uses a cleaner syntax for the same concept. The research fully justifies not adding a `computed` keyword.

- **DSL reading flow is good.** Looking at `travel-reimbursement.precept` and `loan-application.precept`, computed fields would reduce the longest transition rows (the Submit rows with 6-7 chained `set` actions) without changing the reading order of the file. Field declarations stay at the top; transitions stay at the bottom. The file's narrative structure is preserved.

- **AI agent audience is well-served.** A computed field is easier for an AI agent to parse and reason about than a duplicated formula across multiple transition rows. The compile-time diagnostics (cycle detection, null-safety, exclusion rules) give agents clear, actionable error signals. The inspect/fire MCP tools should show computed values — proposal already covers this.

---

## Summary

The proposal's core design is sound and the research comprehensively validates the syntax choice. The required changes are all about making the proposal a better *teaching document* — the kind of spec a new user could read without needing supplementary material. The most impactful additions are: the "how to read `->` " callout (#1), the before/after example (#2), and the error message table (#4). Together, these three changes transform the proposal from an implementer's specification into a user-facing design document.

---

### 2026-04-05: Dockable preview workbench pattern
**By:** Shane (via Elaine)
**What:** Treat Data, Topology, and Timeline as persistent panel identities that can move between docks without changing what each panel means. Docking, collapse, ordering, and resizing belong to the workbench chrome, while axis changes belong only to Topology and Timeline.
**Why:** The preview needs to support different reading modes without forcing a single privileged arrangement or making users relearn the simulation itself.

---

# UX Decision: Interactive Guidance System for Journey Prototype

**Author:** Elaine · UX Designer
**Date:** 2026-05-02
**Scope:** Concept 17 (Interactive Journey) mockup
**Status:** Proposed — needs Shane sign-off

## Context

Concept 17 is the first fully clickable prototype in the mockup series. Shane requested that clickable controls be visually obvious and that the experience include lightweight guidance for first-time users.

The prototype already had functional interactivity (fire events, undo, reset, toggle edits) but the controls blended into the layout — a first-time visitor couldn't immediately tell what was clickable or where to start.

## Decision

Added three layers of interactive guidance, each independently dismissible:

### 1. Visual Emphasis on Interactive Controls

- **Fire ▶ buttons** get a soft pulse-ring animation (2.4s cycle, ~45% opacity peak). The pulse is CSS-only, lightweight, and draws the eye without being distracting.
- **Suggested-next event cards** get a cyan glow border + faster pulse on their Fire button + a "▶ suggested next" badge. This traces the golden path without preventing exploration.
- **Input fields** get a subtle indigo border lift and focus ring (box-shadow on `:focus`).
- **Edit toggles** show a ✎ cursor hint on hover.
- **Undo/Reset buttons** get a press-scale (96%) for tactile feedback.

### 2. Coach Mark Tour (5 Steps)

A spotlight-and-card overlay that walks through the five core interactive surfaces:

1. **Journey Layout** — Past/Present/Future temporal flow
2. **Fire an Event** — arg fields + Fire ▶ button
3. **Topology Rail** — visited-state tracking on the left
4. **Undo & Reset** — header controls
5. **Blue Hints** — the suggested-next badge system

Accessible from: welcome banner ("🎓 Take the tour") and status bar ("🎓 Tour"). Dismissible at any point via "Skip tour." Step indicators show progress as dots.

### 3. Contextual Step Hints

After each event fires, a lightweight "Suggested next: …" hint appears above the journey content, pointing to the next event in the golden path. Dismissible with ✕ and stays dismissed for the session.

The golden path: Submit → AssignAdjuster → RequestDocument → ReceiveDocument → Approve → PayClaim.

### 4. Welcome Banner

On first load (before any events are fired), a welcome banner explains the interaction model and shows the suggested journey as a visual breadcrumb trail. Two CTAs: "Got it — let me explore" (dismiss) and "🎓 Take the tour" (start coach marks).

## Design Principles Applied

- **Progressive disclosure:** Welcome banner → step hints → explore freely. Each layer peels away as the user gains confidence.
- **Non-blocking:** Every guidance element is dismissible. The prototype is fully functional with all guidance hidden.
- **Visual hierarchy:** Pulse animations use low opacity (never above 50%) and cool colors (cyan for suggestion, indigo for focus). They draw attention without competing with the content.
- **Prototype-appropriate:** No localStorage persistence, no analytics, no complex state machines for the guidance itself. Session-scoped dismissal only.

## Patterns Established

1. **Pulse-ring for interactive controls** — CSS `::after` pseudo-element with border animation. Reusable on any clickable element in future surfaces.
2. **Suggested-next badge** — cyan chip with "▶ suggested next" label. Could be generalized to any guided flow in the extension.
3. **Coach mark overlay** — spotlight (box-shadow trick) + positioned card. Pattern could be extracted to shared CSS for other mockups.
4. **Golden path hinting** — data-driven suggested journey that maps state → next event. Decoupled from the simulation logic.

## What This Doesn't Do

- No user tracking or persistence — guidance resets on page reload (appropriate for a prototype).
- No forced linearity — the golden path is a suggestion, not a rail. Users can fire any event at any time.
- No complex animation library — everything is CSS keyframes + simple JS DOM manipulation.
- No accessibility degradation — coach marks are keyboard-dismissible, all text is screen-reader visible, no content is hidden behind hover-only interactions.

## Files Modified

- `tools/Precept.VsCode/mockups/preview-reimagined-17-interactive-journey.html` — CSS additions + JS guidance system
- `tools/Precept.VsCode/mockups/preview-reimagined-index.html` — updated Concept 17 description to mention guided tour

---

# Decision: History + Inspection Hybrid Concepts (15–16)

**Filed by:** Elaine (UX Designer)
**Date:** 2026-05-02
**Status:** Proposed — awaiting Shane review

## Context

Shane observed that Timeline (01) is strongest for production UI, but there's value in a hybrid that combines:
- **History** — where we came from (states visited, events fired, data deltas)
- **Inspection** — where we can go next (available events, outcomes, args)
- A **present-focused developer workflow** rather than a production-monitoring workflow

This is adjacent to Concept 13 (Flow Cards) and Concept 14 (Contract Explorer) but adds a temporal dimension none of the existing concepts have.

## Two Approaches Explored

### Concept 15: Journey — Temporal Narrative

Organizes by **time**: Past → Present → Future as a single vertical scroll.

- **Past section**: compact trail of steps taken. Each step shows event fired, state entered, and key data deltas (before→after). One row per step — works with even 1–3 steps.
- **Present section**: hero card with full data snapshot. "Recently changed" markers on data fields connect current values to the step that set them (e.g., "← set at #1"). Full rules, violations, editable fields.
- **Future section**: available events as action cards grouped by outcome type (transitions first, self-loops next, blocked last). Closes with "also reachable" topology hints.

**Strength**: reads as a narrative grounding the developer in their simulation. Answers "how did I get here?" without requiring deep history. The outcome grouping in the Future section makes "what should I do next?" immediately scannable.

**Limitation**: doesn't show the full topology — you only see visited states, the current state, and available next events. The "also reachable" section is a lightweight nod to topology but doesn't give the complete picture.

### Concept 16: Navigator — Flow Cards with Memory

Organizes by **topology** (like Flow Cards 13) but **threads history through it**.

- **Visited cards**: warm styling, step numbers, mini data-delta strips showing what changed when we passed through.
- **Current card**: full spotlight with "← set at #N" provenance annotations on changed fields.
- **Unvisited cards**: dimmed but present, showing event hints of what's ahead.
- **Traced edges**: transition edges that were actually taken are visually distinguished from available-but-not-taken edges. Step numbers on traced edges.
- **Rail**: visited nodes filled with step badges, traced path highlighted.

**Strength**: preserves the whole-machine context from Flow Cards while making the developer's path visible. The three-layer visual hierarchy (visited/current/unvisited) immediately answers "where have I been and what's left?"

**Limitation**: more complex than Journey — additional visual states to learn. The unvisited future cards show hints but no data (since we don't know what data will look like when we arrive).

## Relationship to Contract Explorer (14)

These are not replacements for Concept 14. They explore a dimension 14 doesn't have — history awareness. The strongest synthesis may be **Contract Explorer with Navigator's history layer**: the same feature-complete preview, but with visited/current/unvisited visual states and data provenance annotations.

## Files Created

- `tools/Precept.VsCode/mockups/preview-reimagined-15-journey.html` — Concept 15 mockup
- `tools/Precept.VsCode/mockups/preview-reimagined-16-navigator.html` — Concept 16 mockup
- `tools/Precept.VsCode/mockups/preview-reimagined-index.html` — Updated with Phase 7, new concept cards, and revised recommendation

## Recommendation

Review 14, 15, and 16 side by side. The choice axis is:
- **14 (Contract Explorer)**: feature-complete contract exploration, no history awareness
- **15 (Journey)**: temporal narrative, lightweight, present-focused, no full topology
- **16 (Navigator)**: topology + history hybrid, full machine visible, richest context

A synthesis of 14 + 16 may be the strongest final form — Contract Explorer's feature completeness with Navigator's history layer.

---

# Decision: Preview Concept Recombination — Flow Cards

**Author:** Elaine (UX Designer)
**Date:** 2026-05-02
**Status:** Proposed — awaiting Shane review

## Context

Shane observed that Timeline/history is a stronger fit for production UI than for the dev preview panel, since manual simulation rarely builds deep event history. He asked whether the large-card approach from Concept 04 (Focus/Spotlight) could be expanded to show the entire flow.

## Decision

Created **Concept 13: Flow Cards** — a recombination of the best ideas from the concept set:

- **From Concept 04 (Focus/Spotlight):** The large-card aesthetic — generous spacing, big state names, minimal chrome, radial path cards. The current state is THE visual anchor.
- **From Concept 05 (Notebook):** Vertical scrolling, progressive disclosure, card-based layout that works at any width.
- **From Concept 11 (Kanban):** The whole-flow metaphor — seeing the full lifecycle from initial to terminal, not just the current state.
- **New:** Left-rail mini-diagram for spatial context. Transition edges between cards carrying event names and guard summaries. Branch sections for alternate paths.

## Key Changes

1. **Timeline (01) reclassified** from Tier 1 (primary) to a production-UI pattern. Still the best history-debugging concept, but history isn't deeply useful in dev preview.
2. **Flow Cards (13) is now the top recommendation** for dev preview — it answers "what does this machine look like, where am I, and what can I do?" in a single scrollable view.
3. **Notebook (05) remains strong** as a secondary "full report" mode.
4. **First complex-sample mockup:** Uses InsuranceClaim (6 states, 7 events, collections) instead of Subscription (3 states, 2 events), validating the layout at real-world scale.

## Artifacts

- `tools/Precept.VsCode/mockups/preview-reimagined-13-flow-cards.html` — the new mockup
- `tools/Precept.VsCode/mockups/preview-reimagined-index.html` — updated with Concept 13 and revised recommendation
- `tools/Precept.VsCode/mockups/preview-concepts-deep-analysis.md` — addendum with Concept 13 analysis and revised tier ranking

## Tradeoffs

- Flow Cards is state-centric; Notebook is facet-centric. For precepts where the developer wants a "report" view (all data, all rules, all events separately), Notebook is still better. Both should exist as switchable modes.
- Non-current cards are compact by default. For a 7-state precept, the developer may need to expand cards to understand states they haven't visited yet. The expand interaction isn't yet prototyped.
- The branch section for alternate paths (UnderReview → Denied) is a simple labeled divider. More complex branching patterns (3-way splits, cycles) need further design.

---

# Decision: Contract Explorer (Concept 14) as Leading Preview Direction

**Filed by:** Elaine
**Date:** 2026-05-02
**Status:** Proposed — awaiting Shane review

## Decision

Concept 14 — **Contract Explorer** — is the leading direction for the Precept preview panel. It refines Flow Cards (Concept 13) into a shipping-candidate shape by integrating:

1. **Inline Execution Trace** (from Concept 12) — expand any event to see the 6-stage fire pipeline with expression evaluation, guard matching, and mutation chains. The "microscope" for debugging.
2. **Structured violation detail** — source-kind badges (transition-rejection, invariant, etc.), expression text, because-reason, and clickable target-field links.
3. **Edit-in-place** — editable fields show inline toggles/inputs directly in the data grid.
4. **What-if bar** — hypothetically change a field and see all event outcomes re-evaluate instantly, without committing the change.
5. **All 6 outcome kinds** with distinct visual treatments: Transition, NoTransition, Rejected, ConstraintFailure, Undefined, Unmatched.
6. **Mode tabs** (Flow / Matrix / Notebook) — acknowledges that secondary views serve different jobs.

## Why This Over Alternatives

- **vs. Flow Cards (13):** Same layout, but now addresses the cross-cutting gaps identified in the deep analysis — pipeline visibility, all outcome kinds, violations, and edit mode.
- **vs. Notebook (05):** State-centric organization is more natural for the primary dev question ("I'm in state X, what can I do?") vs. facet-centric ("show me all events"). Notebook remains a strong secondary tab.
- **vs. Timeline (01):** Timeline is stronger for production UI; manual simulation in dev preview rarely builds deep history. Contract Explorer shows the whole contract now.

## What's New in the Mockup

- `preview-reimagined-14-contract-explorer.html` — full mockup using InsuranceClaim (6 states, 7 events, 8 fields, 1 set collection, 3 invariants, 2 state asserts).
- `preview-reimagined-index.html` — reorganized into three tiers: Leading Direction → Strong Secondary Modes → Exploration Archive.
- Concept 12 (Execution Trace) is now marked "✓ Integrated" in the index since its pipeline debugger ships inline within Concept 14.

## Product Shape Summary

The preview panel is a **state-centric card scroll with inline dev tools**:

- **Header:** precept name, file path, mode tabs (Flow / Matrix / Notebook), Edit Data, Reset
- **What-if bar:** hypothetical field changes with instant re-evaluation
- **Left rail:** compact mini-diagram for spatial context
- **Card flow:** one card per state, connected by transition edges with event labels
- **Current state card (spotlight):** large name, data grid (with edit-in-place), event dock (with pipeline trace expansion, fire buttons, arg inputs, outcome badges, violation detail), rule chips
- **Other state cards:** compact with expand affordance
- **Status bar:** invariant count, structural stats, breadcrumb trail

## Open Questions

1. **Pipeline trace data model:** The trace currently shows a static mockup of expression evaluation. The real implementation needs the fire API to return stage-by-stage detail. Does this require a new `precept_fire_trace` API, or can the existing fire result be enriched?
2. **What-if performance:** Hypothetical field changes re-evaluate all events via inspect. With 7+ events and complex guards, this should be fast (it's stateless evaluation), but needs profiling.
3. **Responsive at sidebar width:** The mockup collapses the rail and reduces card size at 500px. Is this enough, or does sidebar placement need a fundamentally different layout?

## Next Steps

- Shane reviews Concept 14 mockup and decides whether this is the shape to build toward.
- If approved, Kramer scopes the webview implementation and identifies the runtime API changes needed for pipeline trace data.
- Notebook (05) and Matrix (03) need refined mockups using InsuranceClaim data to validate them as secondary tabs.

---

### 2026-04-05: MaintenanceWorkOrder layout probes
**By:** Shane (via Elaine)
**What:** Seed the new mockups at the Scheduled state with real MaintenanceWorkOrder tension already present: urgent is true, parts are not approved, and the first meaningful question is whether the user can read why StartWork is blocked before firing it.
**Why:** The richer mid-flow seed exposes state diagram context, guard friction, and field provenance immediately, so each layout can differentiate itself without spending the first interaction on empty-state setup.

---

### Decision: Right-rail animated state diagram for Interactive Journey (Concept 17)

**Filed by:** Elaine
**Status:** Implemented — awaiting Shane review

**What changed:**
The Interactive Journey prototype now includes a right-hand SVG state diagram that tracks the user's simulation path through the InsuranceClaim lifecycle. The diagram is purely topological — nodes, edges, arrowheads, self-loop indicators, and tiny event-name labels. No data, no guards, no rules. It's a spatial minimap, not a duplicate of the journey scroll.

**Key design decisions:**
1. **Non-duplicative scope** — the diagram shows only state topology + traversal state. All detail (data fields, guards, mutations, provenance) lives in the journey scroll.
2. **Animation model** — newly-traversed edges flash green → violet (900ms). Self-loops flash green. Current node has a pulsing glow ring (2.4s cycle). Animations are CSS keyframes triggered by DOM re-creation on each render.
3. **Fork layout** — UnderReview branches diagonally to Approved (left) and Denied (right), with Paid below Approved. Terminal states use dashed strokes. Initial state has a small violet dot above.
4. **Progressive visibility** — edge labels and arrowheads are dim by default and brighten when their edge has been traced. Visited nodes brighten progressively.
5. **Responsive collapse** — diagram hidden at ≤700px, left rail hidden at ≤500px. Prototype degrades gracefully to 2-column then 1-column.
6. **Coach tour integration** — new step 4 ("The State Diagram") added to the 6-step tour, using a new `position: 'left'` coach card placement.

**Why this approach:**
The diagram answers "where am I in the machine?" at a glance, which the vertical journey scroll cannot — the scroll is temporal, not spatial. Keeping it simple (no data, no guards) avoids the information-density problem that derailed Graph Canvas (Concept 08). The 200px right rail fits comfortably alongside the 56px left rail and fluid journey content.

**Files changed:**
- `tools/Precept.VsCode/mockups/preview-reimagined-17-interactive-journey.html` — state diagram CSS, HTML container, JS render function, coach step, responsive breakpoint
- `tools/Precept.VsCode/mockups/preview-reimagined-index.html` — concept 17 card description and tags updated

---

### 2026-04-08: Computed Fields (#17) — Re-Review After Research Expansion

**By:** Frank (Lead/Architect & Language Designer)
**Requested by:** Shane
**Verdict:** REQUIRED CHANGES — Research grounds the feature; proposal needs targeted updates before implementation.

---

## Review Summary

The expanded research document (`docs/research/language/expressiveness/computed-fields.md`) is a strong piece of work — 24 systems across all 7 philosophy positioning categories, with honest precedent mapping and explicit contract identification. It adequately grounds the **feature concept** and strengthens most of the proposal's locked design decisions. The proposal itself has not been updated since the initial review round, and the gaps identified by George and Kramer in the first pass are still present.

The research does not change my verdict from the first review (REQUIRED CHANGES), but it narrows the remaining work to precise, enumerable updates. No new blocking risks surfaced. The feature remains correctly positioned.

---

## Research Assessment

### Does the 24-system precedent survey adequately ground the proposal?

**Yes, for the feature concept. No, for the syntax choice.**

The survey demonstrates overwhelming cross-category convergence on the core derivation contract: read-only, declared once, automatically recomputed, distinct from defaults. This grounds every locked design decision except #1 (the bare `->` syntax). The research surveys what derivation *means* in 24 systems; it does not survey what derivation *looks like* syntactically. That said, the syntax choice is adequately grounded by Principle 11 (`->` means "results in") — external syntax precedent was never the right anchor for that decision. The research is correctly scoped.

### Cross-category pattern analysis

The research's strongest finding: "No system in the survey combines field-level derivation with lifecycle-aware constraint enforcement." This is the positioning claim that matters. Computed fields would not duplicate a capability from any adjacent category — they fill a gap every category leaves open. That strengthens Precept's positioning, not just the proposal's.

### Dead ends

All 5 dead ends in the research (event-arg coupling, lazy evaluation, writable computed fields, silent defaults, cross-precept derivation) are already reflected in the proposal's locked decisions or explicit exclusions. No new dead ends surfaced. The "silent default fallbacks" risk connects directly to the unresolved nullability contract (see below).

---

## Semantic Contract Coverage

The research identifies 6 semantic contracts that must be explicit before implementation. Here is the current proposal's coverage:

| # | Contract | Coverage | Notes |
|---|---|---|---|
| 1 | **Scope Boundary** | **COVERED** | Proposal locked decision #6: fields + collection accessors only, no event args, no cross-precept refs. Acceptance criteria include "Event argument reference produces compile error." |
| 2 | **Recomputation Timing** | **PARTIALLY COVERED** | Proposal specifies Fire timing ("after mutations, before invariant checks") but is silent on Update pipeline and Inspect preview. Research correctly identifies this as the most important contract. The first review (George's findings #3, #5) flagged the same gap. Still unaddressed. |
| 3 | **Nullability and Accessor Safety** | **NOT COVERED** | Proposal says "Cannot be `nullable`" but that's about the computed field itself, not its inputs. What if the derivation expression references a nullable field? What do `.peek`/`.min`/`.max` return on empty collections? The research frames this as an open decision. George's findings #1 and #2 flagged it. Still unaddressed. This is the single largest type-safety gap. |
| 4 | **Dependency Ordering and Cycles** | **COVERED** | Topological sort at compile time, cycle diagnostics with path. Acceptance criteria include both. MySQL ordering precedent supports the approach. |
| 5 | **Writeability and External Input** | **PARTIALLY COVERED** | Proposal covers `edit` and `set` restrictions (locked decisions #5). But the API surface is unspecified: what happens if a caller passes a computed field value in `CreateInstance`, `Update`, hydration, or MCP payloads? Ignored? Rejected? George's finding #4 flagged this. Still unaddressed. |
| 6 | **Tooling and Serialization Surface** | **PARTIALLY COVERED** | Proposal mentions hover and MCP serialization at headline level. No concrete specs: what does hover show (formula? value? dependencies?)? What shape does the MCP compile output take? How do Inspect and Update outputs surface computed values? Kramer's findings #1, #2, #5 flagged this. Still unaddressed. |

**Score: 2 COVERED, 3 PARTIALLY COVERED, 1 NOT COVERED.**

---

## DSL Philosophy Filter (7 Questions)

| # | Question | Pass? | Rationale |
|---|---|---|---|
| 1 | **Domain integrity** | **YES** | Computed fields eliminate manual synchronization — the formula drift and single-source-of-truth loss documented in the research are real integrity risks. The `travel-reimbursement.precept` sample demonstrates the pain: `RequestedTotal` is manually recomputed in every row that changes its inputs. |
| 2 | **Determinism** | **YES** | Pure field-to-field derivation is deterministic by construction. The research correctly rejects lazy/cached evaluation and event-arg coupling as anti-deterministic. Proposal excludes both. |
| 3 | **Keyword clarity** | **PASS WITH CAVEAT** | The bare `->` reuses an existing symbol with a new semantic role (derivation vs. action). Principle 11 covers it: `->` means "results in." But today `->` always follows a context (state/event/guard) and introduces an *action*. In `field X as number -> Expr`, it follows a type and introduces a *definition*. This is context-disambiguated at the parser level (after `as Type` = derivation; after `when Expr` = action), but it creates a new visual pattern that authors must learn. The research does not address this directly. I note it as a learning-curve cost, not a blocking concern — Principle 11 is broad enough to absorb both uses. |
| 4 | **Truth boundaries** | **YES** | Computed fields are data truth (like invariants), computed from persistent entity state. They don't blur the data/movement distinction. |
| 5 | **Locality** | **YES** | Maximum locality — the formula lives on the field declaration itself. Stronger locality than the current workaround (formulas scattered across transition rows). |
| 6 | **Compile-time soundness** | **CONDITIONAL** | Cycle detection, mutual exclusion rules, and type checking are all compile-time. But the null-safety gap (Contract #3) means the type checker cannot currently prove a computed expression is total if it references nullable inputs. Until the proposal specifies the conservative rejection rule, compile-time soundness is incomplete. |
| 7 | **Alias creep / AI legibility** | **YES** | The `->` extension is natural for AI parsing — field declarations are already keyword-anchored, and the arrow is a recognizable derivation marker. The research's point about formula-bearing fields being universally understood as declared derivations supports AI readability. |

**Philosophy verdict: Passes 5/7 cleanly, passes 2/7 conditionally. No failures.**

---

## Required Proposal Updates

The following updates are needed before the proposal is implementation-ready. Numbered for tracking.

### Must-fix (blocks implementation)

1. **Specify recomputation timing for all three pipelines.** The proposal must state explicitly:
   - **Fire:** recompute after ALL mutations (exit actions → row actions → entry actions), before invariant/assert evaluation. One recomputation pass after Stage 5 (entry actions), not after Stage 4 (row mutations). George's finding #5 was correct — entry actions can mutate dependency fields.
   - **Update:** recompute after field edits are applied, before invariant/assert evaluation.
   - **Inspect:** preview output must reflect post-recomputation values. Add acceptance criteria for Inspect output.

2. **Specify the nullable input rule.** The proposal must add: "The type checker conservatively rejects computed field expressions that could produce null. Expressions may reference only non-nullable scalar fields and collection accessors that are guaranteed to return a value (`.count`). Accessors that are undefined on empty collections (`.min`, `.max`, `.peek`) are not permitted in computed field expressions." This is the conservative starting point. A future null-coalescing operator (if added) can relax this constraint.

3. **Specify the external input contract.** Add: "Computed field values supplied by external callers — in `CreateInstance` data, `Update` patches, hydration payloads, or MCP tool arguments — are rejected with a diagnostic. The runtime does not silently ignore them." Align with Terraform's `Computed` attribute precedent (reject, don't ignore).

### Should-fix (blocks quality implementation)

4. **Add Update pipeline acceptance criteria.** Currently all AC items test Fire-path behavior. Add: "Update(instance, patch) recomputes dependent computed fields before constraint evaluation" and "Inspect output includes fresh computed values."

5. **Specify hover content.** "Hover on a computed field shows: the derivation expression, the result type, and the list of dependency fields." This gives the LS team a concrete spec.

6. **Specify MCP serialization shape.** `precept_compile` output should include `isComputed: true` and `expression: "<source text>"` on computed field entries. `precept_inspect` and `precept_fire` outputs should include computed field values in the instance data snapshot, marked with their source expression.

7. **Add acceptance criterion for event-arg compile error.** The semantic rule says "no event args in computed expressions" but the AC list doesn't include: "Event argument reference in computed field expression produces compile error." Add it.

### Nice-to-have (strengthens proposal)

8. **Link the research document.** The "Research and rationale links" section should reference `docs/research/language/expressiveness/computed-fields.md` for the 24-system precedent survey.

9. **Add a note on the `->` dual-use visual cost.** Under locked decision #1, acknowledge that the arrow now has two syntactic roles (action introducer in transitions, derivation operator in fields) and that context disambiguation at the parser level resolves any ambiguity. This is an honest design note, not a design concern.

---

## Research Sufficiency

**The research is sufficient to ground implementation.** No additional external research is needed. The 24-system survey covers all 7 philosophy categories, the cross-category gap analysis is correctly stated, and the 6 semantic contracts are the right set. What remains is proposal editing, not more research.

---

## Locked Decision Strength After Research

| # | Decision | Research Effect |
|---|---|---|
| 1 | Bare `->` syntax | Neutral (research grounds feature, not syntax; Principle 11 anchors syntax) |
| 2 | Mutually exclusive with `default` | **Strengthened** (PostgreSQL, every database in the survey separates generated from default) |
| 3 | Cannot be `nullable` | Unchanged (research raises the input-nullability question the proposal still hasn't answered) |
| 4 | Constraints excluded | **Strengthened** (enterprise platforms don't apply field-level validation to formula fields) |
| 5 | Edit and set restrictions | **Strongly strengthened** (universal read-only contract across all systems that support derivation) |
| 6 | Event arg scope excluded | **Strengthened** (research correctly frames as scope boundary; dead-end analysis confirms) |
| 7 | Topological sort | **Strengthened** (MySQL explicit ordering precedent) |
| 8 | Recomputation timing | Strengthened but reveals gaps (Update and Inspect pipelines unspecified) |

---

# Decision: Constraint Composition Is One Research Domain

**By:** Frank (Language Designer)
**Date:** 2026-05-18
**Status:** Decided

## Decision

Treat named rules (#8), field-level constraints (#13), and conditional (guarded) declarations (#14) as **one research domain** — "Constraint Composition" — rather than three separate issue-first efforts. Research documents are organized by domain, not by proposal number.

## Rationale

1. All three proposals stem from the same root gap: Precept's constraint surface has no composition layer between individual boolean expressions and the flat statement model.
2. They share scope rules, grammar positions (suffix zones), and desugaring targets (invariants/asserts). Designing them independently risks inconsistent answers to "where does this constraint live?"
3. The domain-research-batches plan already grouped them as one effort under "Constraint Composition." This decision formalizes that grouping in the research corpus.

## Structural Changes

- **New domain doc:** `docs/research/language/expressiveness/constraint-composition-domain.md` covers all three features as one domain pass — background, precedent survey (all philosophy categories + databases, languages, platforms, end-user tools), philosophy fit, locality boundaries, semantic contracts, dead ends, and proposal implications.
- **Expanded reference doc:** `docs/research/language/references/constraint-composition.md` upgraded from C-grade to full theory/reference coverage — formal foundations (Boolean lattice, Specification pattern, scope theory, desugaring semantics), expanded system examples (Alloy, Zod, FluentValidation, Drools, OCL, FHIR), implementation cost matrix, and resolved design decisions.
- **No proposal edits.** Issue bodies (#8, #13, #14) remain as-is per task scope.

## Sequencing Implication

The research confirms: #13 and #14 together in Wave 2, #8 in Wave 4 after expression surface (#9, #31) settles. This aligns with the existing batch plan.

---

### 2026-04-06: Decimal scale constraint semantics
**By:** Frank (Lead/Architect)
**What:** `scale <N>` constraints on `decimal` fields **reject** values with too many decimal places — they never silently round. Scale is checked only at the field assignment boundary, not on intermediate subexpressions. This matches Precept's constraint philosophy (reject invalid states, don't fix them) and aligns with FEEL/Cedar (rule-engine peers) rather than SQL (storage peer). A future `round()` function is deferred until real-world usage demonstrates the need.

### 2026-04-06: decimal / decimal → decimal (corrected)
**By:** Frank (Lead/Architect)
**What:** The coercion rule for `decimal / decimal` was corrected from `number` (IEEE 754) to `decimal`. .NET's `System.Decimal` division operator returns `decimal`, not `double`. The proposal's coercion table now matches actual .NET behavior. All 11 coercion rules verified against .NET System.Decimal.

---

# Decision: Drop `unless` keyword from Precept

**Date:** 2026-04-06
**Author:** Frank (Lead/Architect & Language Designer)
**Status:** Accepted
**Scope:** Language surface — #14 (conditional invariants and edit eligibility)

## Decision

Precept will **not** add an `unless` keyword. Negative guards use `when not`, which becomes available after #31 (`not` keyword) lands.

## Rationale

1. **Precedent:** 7-to-3 against across 10 comparable systems. Only FluentValidation, Alloy, and Ruby offer `unless`-style constructs; FluentValidation's `Unless()` is rarely used in practice.
2. **Compound condition breakdown:** `unless A and B` triggers De Morgan confusion — does the negation distribute? `when not (A and B)` is unambiguous.
3. **One canonical form:** Precept's design principle of one way to say each thing. Two negation forms (`unless X` vs `when not X`) would violate this.
4. **Zero new grammar:** `when not` composes from existing keywords — no parser changes, no grammar additions, no new token type beyond what #31 already delivers.

## What changed

- **Issue #14** — rewritten to remove all `unless` references; examples use `when not`; "Add `unless` keyword" section removed; acceptance criteria updated; new "Why not `unless`?" section added.
- **`docs/research/language/expressiveness/conditional-logic-strategy.md`** — Resolution section updated; `unless` dropped from approved syntax; rationale added.
- **`docs/research/language/README.md`** — Issue map entry for #14 updated to reflect scope.

## References

- `docs/research/language/expressiveness/conditional-logic-strategy.md` — full precedent survey and audit
- GitHub Issue #14 — canonical proposal
- GitHub Issue #31 — `not` keyword dependency

---

# Decision: Expression expansion domain research completed

**Filed by:** Frank (Lead/Architect)
**Date:** 2026-04-08
**Status:** Research complete — awaiting Shane review

## What was produced

Completed the Batch 2 Expression Expansion research effort as a single domain-first pass covering all five open proposals (#9 conditional expressions, #10 string `.length`, #15 string `.contains()`, #16 built-in function library, #31 logical keywords).

### Deliverables

1. **`docs/research/language/expressiveness/expression-expansion-domain.md`** — primary domain research at `computed-fields.md` quality bar. Covers: background/problem grounded in samples, cross-category precedent survey (25+ systems across 9 categories), philosophy fit per design principle, vocabulary boundary taxonomy (operators vs accessors vs methods vs functions), null safety policy, string surface specification, semantic contracts, 10+ dead ends and rejected directions, and proposal sequencing implications.

2. **`docs/research/language/references/expression-evaluation.md`** (expanded) — formal/theory companion now covers: vocabulary taxonomy with formal properties, conditional expression type rules and null narrowing, function-call semantics in constrained DSLs (static dispatch, compile-time constraints, totality), string predicate theory from symbolic automata, and expanded null safety framework.

## Key structural recommendations

### Vocabulary boundary taxonomy

The expression surface introduces four distinct vocabulary categories, each with different formal properties and extension costs:

- **Operators** — binary/unary infix tokens at defined precedence (`and`/`or`/`not`, existing `+`/`==`/`contains`)
- **Accessors** — parameterless dotted reads (`.length`, `.count`) — no new AST node
- **Methods** — dotted calls with arguments (`.contains(sub)`) — new grammar form
- **Functions** — prefix calls (`abs(x)`, `round(x)`) — new AST node required

This taxonomy is durable: every future expression-surface decision (accessor? method? function? operator?) should be classified against it.

### Sequencing recommendation

1. #31 (logical keywords) — pure token swap, zero semantic risk
2. #10 (string `.length`) — single accessor, mirrors `.count`
3. #9 (conditional expressions) — new AST node but well-understood
4. #15 (string `.contains()`) — new method-call form, narrow scope
5. #16 (built-in functions) — largest parser investment, but infrastructure for all future functions

### Explicit exclusions locked

- Regex: decidability risk, no blocking demand
- User-defined functions: permanently excluded (breaks flat-identifier model)
- Collection aggregates: deferred (no sample demand)
- Varargs: deferred (nesting covers multi-argument `min`/`max`)
- Method chaining: excluded (function composition via nesting instead)

## Gate

No implementation begins until Shane approves the specific proposal/wave. This research provides the evidence base for that decision.

---

---
author: frank
date: 2026-04-05
status: proposed
domain: proposal-workflow
requires-sign-off: shane
supersedes: frank-squad-project-compatibility.md
---

# Decision: Remove proposal status labels only after a board-first contract exists

## Verdict

**Unsafe for now.**

Removing `needs-decision`, `decided`, and `deferred` today would take away proposal-state semantics before Squad has a replacement that is equally queryable, inspectable, and enforceable.

It becomes **safe with changes** if proposal status moves to a single explicit project field and Squad's proposal workflow is updated to read that field instead of labels.

## What I verified

Current proposal workflow is split across three places:

- GitHub labels include `proposal`, `language`, `needs-decision`, `decided`, and `deferred`
- Project **Precept Language Improvements** contains the proposal issues
- That project currently has only the built-in `Status` field with `Todo`, `In Progress`, and `Done`

Current open proposal issues (`#8`-`#17`) are labeled `needs-decision`; closed proposal `#18` is labeled `decided` and sits in project `Done`.

The repo automation workflows enforce `go:`, `release:`, `type:`, and `priority:` namespaces. They do **not** currently enforce proposal-status labels. That means this is mainly a **workflow-contract migration**, not a GitHub Actions migration.

## What must remain true for Squad compatibility

If proposal status labels go away, these invariants must still hold:

1. **Proposal identity stays issue-native.**
   - `proposal` remains required.
   - Domain/slice labels remain taxonomy only.
   - `squad:{member}` remains routing only.

2. **Proposal lifecycle has one machine-readable source of truth.**
   - No split between labels saying one thing and the board saying another.
   - Queue state and decision state must not be inferred from ad hoc comments or tribal knowledge.

3. **Active proposals are queryable without manual reading.**
   - Squad must be able to answer "what still needs a decision?" deterministically.

4. **Terminal outcomes stay distinguishable.**
   - `Accepted`, `Rejected`, `Deferred`, and `Implemented` cannot collapse into one generic `Done`.

5. **A proposal cannot be "off-board but still valid."**
   - If the board becomes the lifecycle source, every proposal issue must be on that board.

6. **Close-out discipline remains mandatory.**
   - Closing a proposal still requires a decision comment with rationale and links to any `.squad/decisions/` artifact.

## Required guardrails

### 1. Add a dedicated project field

Do **not** overload the built-in `Status` field. Add a separate single-select field such as `Proposal Status`.

Minimum values:

- `Needs Decision`
- `In Review`
- `Accepted`
- `Rejected`
- `Deferred`
- `Implemented`

`Todo / In Progress / Done` is execution flow. It is not proposal semantics.

### 2. Make the project field authoritative

If labels are removed, `Proposal Status` becomes the canonical lifecycle field for proposals. Do not keep a shadow status system.

### 3. Keep issue-level auditability

Each terminal move still needs a closing comment with a structured first line, e.g.:

- `Decision: Accepted`
- `Decision: Rejected`
- `Decision: Deferred`

The project field carries current status; the comment carries rationale and preserves issue-page inspectability.

### 4. Enforce project membership

Every `proposal` issue must be added to **Precept Language Improvements**. Orphan proposal issues are invalid under a label-free model.

### 5. Keep routing orthogonal

`squad:*` labels stay. Proposal status migration must not touch Squad ownership/routing labels.

## Migration prerequisites

Before removing any proposal-status labels:

1. **Create and backfill `Proposal Status`** on the project for all existing proposal issues.
2. **Decide visibility policy**:
   - either make the project public,
   - or explicitly accept that proposal status is an authenticated/internal surface.
3. **Update Squad instructions and skills** that currently describe label-based proposal status:
   - `.squad/templates/skills/architectural-proposals/SKILL.md`
   - `.copilot/skills/architectural-proposals/SKILL.md`
   - `docs/research/language/README.md`
   - any coordinator guidance that tells agents to query proposal status by labels
4. **Define the query contract** Squad will use:
   - proposal discovery via issue labels (`proposal`, domain, `squad:*`)
   - proposal lifecycle via `gh project` field reads
5. **Add an enforcement check** so a `proposal` issue cannot be closed or deferred without:
   - a populated `Proposal Status`
   - a decision comment

## Recommended migration shape

### Phase 1 — prepare the board

- Add `Proposal Status`
- Backfill all current proposal issues
- Keep existing labels temporarily during migration

### Phase 2 — switch Squad's proposal contract

- Update skills/docs/query recipes to use:
  - issue labels for taxonomy/routing
  - project field for lifecycle state

### Phase 3 — remove status labels

Only after Phases 1 and 2 are complete:

- remove `needs-decision`
- remove `decided`
- remove `deferred`

## Architecture judgment

The safe end state is **not** "labels but less of them." The safe end state is:

- **issues** hold proposal identity and rationale
- **project field** holds proposal lifecycle
- **routing labels** hold Squad ownership

That is a clean separation of concerns.

But we are **not there yet**. Right now the board cannot represent proposal outcomes cleanly, and the written Squad workflow still assumes label-based proposal status. So the correct call is:

**unsafe now; proceed only as a staged migration with explicit guardrails.**

---

### 2026-04-06: Decimal precision constraint named `maxplaces`
**By:** Frank (Lead/Architect)
**What:** The constraint keyword for limiting decimal places on `decimal` fields is `maxplaces`, not `scale`. This follows Issue #13's `max{noun}` pattern (`maxlength`, `maxcount`, `maxplaces`). Alternatives rejected: `scale` (SQL jargon, breaks pattern), `places` (bare noun, breaks pattern), `maxdecimals` (verbose, redundant with type name), `precision` (wrong semantics — means total digits in SQL).
**Why:** Same reasoning that chose `choice` over `enum`: Precept prefers plain-English configuration language over programming/database jargon. The `max{noun}` pattern is self-describing and discoverable via LS auto-complete.

---

# Decision: Issue #17 Computed Fields Proposal Revamp

**Filed by:** Frank (Lead/Architect & Language Designer)
**Date:** 2026-04-08
**Status:** Complete
**Category:** `dsl-expressiveness`

---

## Summary

Revamped the computed fields proposal (`temp/issue-body-rewrites/17.md`) to incorporate ALL findings from the research document (`docs/research/language/expressiveness/computed-fields.md`) and ALL 7 team re-review decisions.

## Gate Decisions Applied (Steinbrenner defaults)

| Gate | Decision | Rationale |
|------|----------|-----------|
| Null-safety rule | Conservative rejection — type checker rejects any computed expression referencing a nullable field | Only sound option without null-coalescing operator |
| Collection accessor safety | `.count` only — `.peek`/`.min`/`.max` excluded | Research dead-end "silent default fallbacks" rules out option (c); guard complexity rules out option (b) |
| External input contract | Reject with error — Terraform model | Precept philosophy favors explicit errors over silent behavior |
| Recomputation timing (Fire) | Single pass after ALL mutations (exit + row + entry actions), before validation | George's analysis: entry actions can mutate dependency fields, so recompute must happen after Stage 5 |

## What Changed

### New sections added
- **Tooling surface** — hover content spec, MCP compile/inspect/fire/update specs, completions spec
- **Teachable error messages** — 9 scenarios with realistic messages that explain why + what to do instead
- **Before/after example** — travel-reimbursement `RequestedTotal` pain point → computed field solution
- **Resolved decisions table** — maps all 6 research semantic contracts to locked decisions
- **`->` reading guide callout** — explains arrow dual-use for first-time readers

### Sections expanded
- **Summary** — added spreadsheet mental model sentence
- **Motivation** — added cross-category positioning claim from research
- **Semantic rules** — added nullable input rejection, collection accessor safety, external input rejection, pipeline-specific recomputation timing, event argument reference rule
- **Locked decisions** — expanded #8 to all pipelines; added #9 (nullable), #10 (external input), #11 (accessor restriction); added `->` dual-use note to #1; added precedent citations from research
- **Explicit exclusions** — added silent default fallbacks, writable computed fields, null-coalescing, unsafe accessors; merged fixed-point with cycle detection
- **Acceptance criteria** — expanded from 19 flat items to 37 categorized items
- **Implementation scope** — expanded from 10 to 16 items
- **Wave** — changed from "Wave 4: Compactness + Composition" to "Composition"
- **Research links** — added research document link

### Sections replaced
- **Open Questions: "None"** → **Resolved decisions** table showing all 6 research contracts as locked

## Review Coverage

All 7 re-reviews were addressed:

| Reviewer | Items | Addressed |
|----------|-------|-----------|
| Frank | 3 must-fix, 4 should-fix, 2 nice-to-have | All 9 |
| George | 8 edits | All 8 |
| Kramer | 6 missing items | All 6 |
| Elaine | 7 improvements | All 7 |
| Soup Nazi | 7 must-add ACs, 7 should-add ACs | All 14 |
| Uncle Leo | 5 blocking, 8 advisory | All 13 |
| Steinbrenner | 4 gate decisions, 11 edits | All 15 |

## Next Steps

Proposal is ready for Shane's sign-off. No further research needed. All semantic contracts resolved. All acceptance criteria testable.

---

# Decision: Rationale pass on computed fields proposal (#17)

**Author:** Frank (Lead/Architect & Language Designer)
**Date:** 2026-04-08
**Status:** Complete — awaiting Shane review

## What changed

Added retroactive "Why" rationale to every locked design decision and key semantic rule in `temp/issue-body-rewrites/17.md`. The proposal previously stated WHAT was decided but not WHY.

## Specific changes

### All 11 locked decisions now include:
- Alternatives considered and rejected (with reasons)
- Precedent from the 24-system research survey
- Tradeoff accepted
- Philosophy principle served

### Shane's 3 design points incorporated:

1. **Locked decision #9 (nullable input rejection):** Documented that Precept is stricter than all 24 surveyed systems. Justified because those systems have null-handling operators (COALESCE, BLANKVALUE) that Precept lacks. Practical impact near zero — all numeric fields in 21 samples are non-nullable with defaults.

2. **Locked decision #4 (constraints excluded):** Clarified that invariants already reference computed fields, covering output validation. Field-level constraints validate external input; computed fields have no external supply path. Determinism not threatened either way — the choice is about mechanism clarity, not correctness.

3. **Locked decision #8 + semantic rules (Inspect recomputation):** Reworded to clarify Inspect operates on a clone. Recomputation is simulated on a working copy, consistent with how `set` mutations work during Inspect today. Skipping recomputation would make Inspect disagree with Fire on constraint evaluation.

### Additional rationale added to:
- Semantic rules section (nullable input rejection, collection accessor safety, field-level constraints, event argument exclusion, external input rejection)
- Exclusions section (event argument references, recursive dependencies, lazy evaluation, cross-precept references)

## Principle

Per CONTRIBUTING.md: "A proposal that states WHAT without WHY is incomplete." The research document had all the evidence; the gap was in surfacing the key "because" statement at each decision point in the proposal itself.

---

# Decision: Mandatory Per-Decision Rationale in Proposals

**Author:** Frank (Lead/Architect & Language Designer)
**Date:** 2026-04-08
**Trigger:** Shane feedback — design proposals cover the "how" but not enough of the "why."

## Decision

All proposal and design-process documentation now requires explicit per-decision rationale. Every locked design decision must include: why this choice, what alternatives were rejected, what precedent grounds it, and what tradeoff is accepted. A proposal that states WHAT without WHY is incomplete.

## What Changed

### 1. `CONTRIBUTING.md` — Proposal lifecycle

- Added a rationale checklist after the proposal contents list: each locked decision must carry why, alternatives rejected, precedent, and tradeoff accepted.
- Added a completeness gate: proposals without rationale are sent back before advancing to Ready.
- Updated the "Where Things Live" table to clarify that rationale lives in BOTH the issue body (per-decision "why") and `docs/research/` (full evidence base).
- Added a quality bar note pointing to `docs/research/language/expressiveness/computed-fields.md` and Issue #17 as the reference standard for rationale depth.

### 2. `.github/copilot-instructions.md` — AI agent instructions

- Added a "Per-Decision Rationale Requirement" subsection under "Proposal Philosophy Capture" with the four-point checklist (rationale, alternatives, precedent, tradeoff).
- Added an incompleteness flag: agents must flag proposals that state WHAT without WHY.

### 3. Frank's charter (`.squad/agents/frank/charter.md`)

- Added rationale enforcement to the Design Gate section: I now explicitly require the four-point rationale checklist on every locked design decision and send back proposals that lack it.

## Rationale

Design decisions made without captured rationale are effectively lost after the session ends. When the team revisits a decision later, they can see WHAT was decided but not WHY — making it impossible to evaluate whether the reasoning still holds. Baking rationale into the process at every gate (proposal authoring, AI agent review, architectural sign-off) ensures the "why" is never optional.

---

### 2026-04-06: round() promoted to in-scope for decimal proposal
**By:** Frank (Lead/Architect)
**What:** `round(decimal, N)` is the first expression function in Precept and is IN SCOPE for Issue #27 (not deferred). Uses banker's rounding (`MidpointRounding.ToEven`). Only accepts `decimal` expressions; second arg must be a non-negative integer literal. Valid in `set`, `when`, and `invariant` positions.
**Why:** The reject-on-assignment semantics of `maxplaces` create a catch-22 without explicit rounding — mixed-precision arithmetic (e.g., `UnitPrice maxplaces 4 × Quantity → LineTotal maxplaces 2`) has no clean solution. Every surveyed rule engine provides rounding; omitting it would be the only gap. Scope is intentionally narrow: only `round()` in v1; `abs()`, `floor()`, `ceil()` defer to future evidence.
**Implications:** Parser must handle `FunctionCall` AST node. `round` is a built-in function name, not a keyword. The parser matches `round` specifically — no general-purpose function-call mechanism. No user-defined functions.

---

# Frank Decision — Separate Closeout Lanes

Date: 2026-04-05
Requested by: Shane

## Decision

Do **not** sweep the current worktree into one closeout. Split it into three lanes:

1. **Trunk-closeout now:** only the governance/workflow normalization files that collectively move Squad from the old `untriaged` / `go:*` model to the new `Backlog` + project-status + `blocked` / `deferred` model.
2. **UX/mockup lane:** preview/inspector PRD, audit, and mockup exploration stay on their own path.
3. **Hold back as unrelated/noisy:** research-tree reorganization, agent-history churn, and stray artifacts.

## Why

These edits are not one change. They are three unrelated narratives sharing a dirty worktree:

- **Workflow normalization** changes live automation and the docs/templates that operators will read.
- **Preview redesign** is product/UX exploration with mockups and a PRD.
- **Research-tree churn** is documentation topology work and currently overlaps awkwardly with other guidance.

Bundling them would make review sloppy and rollback miserable.

## Safe lane contents

### Lane 1 — trunk-closeout now

Operational workflow files:

- `.github/workflows/sync-squad-labels.yml`
- `.github/workflows/squad-triage.yml`
- `.github/workflows/squad-heartbeat.yml`
- `.github/agents/squad.agent.md`
- `.squad/templates/issue-lifecycle.md`
- `.squad/routing.md`
- `.squad/templates/routing.md`
- `.squad/templates/ralph-triage.js`
- `.squad/templates/squad.agent.md`
- `.squad/templates/workflows/sync-squad-labels.yml`
- `.squad/templates/workflows/squad-triage.yml`
- `.squad/templates/workflows/squad-heartbeat.yml`
- `.squad/templates/workflows/squad-label-enforce.yml`

### Lane 2 — UX/mockup lane

- `brand/inspector-panel-review.md`
- `docs/PreviewInspectorRedesignPrd.md`
- `tools/Precept.VsCode/mockups/preview-reimagined-index.html`
- `tools/Precept.VsCode/mockups/preview-reimagined-17-interactive-journey.html`
- the rest of the untracked `tools/Precept.VsCode/mockups/preview-*` exploration files and shared CSS

### Lane 3 — do not sweep into closeout

- `.copilot/skills/architectural-proposals/SKILL.md`
- `.squad/templates/skills/architectural-proposals/SKILL.md`
- `.squad/agents/frank/charter.md`
- `.squad/agents/george/charter.md`
- `.squad/agents/j-peterman/charter.md`
- `.squad/agents/newman/charter.md`
- `.squad/agents/steinbrenner/charter.md`
- `.squad/agents/elaine/history.md`
- `.squad/agents/frank/history.md`
- `.squad/agents/steinbrenner/history.md`
- `.squad/skills/issue-workflow-normalization/SKILL.md`
- `.squad/skills/unified-issue-workflow/SKILL.md`
- `docs/research/README.md`
- everything under new `docs/research/language/`
- the deleted `docs/research/dsl-expressiveness/*` and `docs/research/language-references/*` set
- `package-lock.json`

## Recommended landing order

1. **Land Lane 1 core automation first** — label sync, triage, heartbeat, and label-enforcement template changes.
2. **Then land Lane 1 guidance/docs** — agent instructions, lifecycle template, routing docs, and mirrored Squad templates.
3. **Validate the workflow behavior on GitHub** before touching anything UX-related.
4. **After that, review Lane 2 as a standalone UX/design package** with product sign-off.
5. **Do not include Lane 3** in closeout. Revisit it as separate documentation/process cleanup only if someone explicitly wants that scope.

## Architectural note

There is a live inconsistency already hiding in the noisy lane: proposal-storage policy changes are being mixed with a research-tree relocation, while some guidance still points at the old research paths. That is exactly why this material stays out of closeout until it gets its own deliberate review.

---

# Frank Decision — Standardized GitHub Issue Workflow

## Decision

Adopt one issue workflow for the whole repo. Stop using proposal-specific status labels as lifecycle state.

Separate the model into four distinct layers:

1. **Routing labels** — who should handle it
2. **Taxonomy labels** — what kind of work it is
3. **Project Status** — where it is in the workflow
4. **Issue open/closed** — whether the issue is still live

That split is the whole game. Anything else is duplication.

## Required routing labels

- `squad` — team inbox; triage required
- `squad:{member}` — exactly one active owner label when routed to a specific member

These are the only routing labels Squad truly needs.

## Useful taxonomy labels

Keep type/domain labels because they help filtering and reporting:

- Type: `proposal`, `bug`, `feature`, `chore`, `ux`, `docs`, `research`
- Domain/slice labels as needed: `language`, `runtime`, `mcp`, `plugin`, `tooling`, `extension`, etc.

Taxonomy labels must never carry lifecycle state.

## Project status model

Use one compact `Status` field in GitHub Projects:

- `Inbox` — new, not yet triaged
- `Ready` — triaged, approved to proceed, waiting pickup
- `In Progress` — active execution, investigation, or drafting
- `In Review` — awaiting review, decision, sign-off, or merge
- `Blocked` — cannot proceed due to an external dependency
- `Done` — optional short-lived terminal board state before archive/removal

## Open/closed semantics

- **Open** means the issue still has a live next action
- **Closed** means the issue’s purpose is complete or intentionally ended

Close issues when they are:

- implemented
- rejected
- deferred / not now
- duplicate
- superseded

Closure reason belongs in the final comment, not in a status label.

## Proposal handling

Proposals follow the exact same workflow:

- `proposal` + relevant domain labels
- routed through `squad` / `squad:frank` as needed
- `In Review` while awaiting architectural or Shane sign-off
- closed once the decision is recorded

If implementation follows, open linked implementation issue(s). Do not keep the proposal issue open as a fake execution tracker.

## Migration direction

1. Freeze creation of `needs-decision`, `decided`, and `deferred`.
2. Create or standardize a single project `Status` field with the states above.
3. Remap open issues from labels to project status based on the real next action.
4. Backfill proposal closing comments before removing old status labels from closed items.
5. Update templates, skills, and Ralph/Squad guidance so automation reads project status instead of proposal-status labels.

## Risks

1. **Decision ambiguity on closed proposals** — mitigated by a required closing comment template: `Decision`, `Why`, `Next step`.
2. **One issue trying to be both proposal and implementation tracker** — mitigated by splitting execution into follow-on issues after approval.
3. **Teams sneaking workflow back into labels** — mitigated by a hard rule: labels classify, project status tracks progress.

---

# Decision: Transition Shorthand Research Recommendation

**Filed by:** Frank (Lead/Architect & Language Designer)
**Date:** 2026-05-18
**Status:** Research complete — recommendation for sequencing

## Context

Completed the Batch 3 Transition Shorthand domain research as `docs/research/language/expressiveness/transition-shorthand.md`. This covers multi-event `on` clauses, catch-all event routing, and row-count reduction across the 21-sample corpus.

## Structural Recommendation

**Multi-event `on` should be the next shorthand added after the current proposal queue clears.** The no-arg subset (events with no argument references in guards or mutations) is the right Phase 1: low parser cost, high consistency with existing multi-state `from` expansion, and formally justified by CSP event-set abstraction.

**Catch-all routing (`from any on any` or `from State on any`) should remain rejected.** The diagnostic cost — silencing `Undefined` outcomes and C49/C51/C52 warnings — exceeds the compactness benefit. No sample in the corpus uses defensive reject-only rows for unrouted pairs, and the `Undefined` signal is architecturally valuable.

**Guard-pair `else reject` is a higher-impact compactness mechanism than multi-event `on`.** It should be tracked as an independent design domain. The verbosity analysis shows ~39 reject rows in 196 total rows (~20%), with 20–35% being pure fallthrough pairs. An inline `else reject "..."` suffix on guarded rows would halve the header cost for all conditional transitions.

## Sequencing

1. Guard-pair `else reject` (separate proposal — highest impact)
2. Multi-event `on`, no-arg subset (next shorthand after current queue)
3. Multi-event `on`, shared-arg subset (requires arg-compatibility checker)
4. Scoped catch-all (only if diagnostic masking is solved)

No proposal should be opened for any of these until Batches 1 and 2 (type system, expression, entity modeling) are resolved.

---

### Decision: Type system expansion proposal filed (2026-04-05)
**Filed by:** Frank
**Status:** Proposed — awaiting Shane sign-off

Filed a comprehensive GitHub issue proposal for expanding the Precept type system beyond `string`, `number`, `boolean`, and collections. Proposal tracked under `dsl-expressiveness`.

**Recommended additions (v1):**
- `choice` type — constrained value sets (`field Severity as choice("Low", "Medium", "High")`). Replaces the pattern of `number` + paired `invariant >= / <=` bounds, and `set of string` used as enumerated document/service lists. High confidence — strong corpus evidence, clean parser integration, no philosophy violations.
- `date` type — calendar dates with day-level granularity, no time-of-day or timezone. Replaces the `number` day-counter pattern used in 4+ samples. Medium confidence — needs careful operator design to stay inspectable and deterministic.

**Explicitly excluded (with rationale):**
- `datetime` / `timestamp` — timezone semantics violate deterministic inspectability. Precept is a domain integrity DSL, not a scheduling runtime. If wall-clock time matters, inject it as a `date` event arg.
- `enum` keyword — rejected in favor of `choice` for readability; `enum` carries programming-language connotation.
- Structured / record types — violate Precept's flat-field design invariant.
- Duration arithmetic beyond day-level — keeps the evaluator simple and avoids floating-point time math.

**Dependency notes:**
- `choice` is independent of all other proposals (#8–#17) and could ship first.
- `date` interacts with the numeric function proposal (#16) for potential date-arithmetic helpers, but can be scoped independently with basic `+` / `-` day arithmetic.

**Gate:** No implementation begins until Shane approves the specific proposal.

---

# Decision: Type system survey — external evidence for Proposal #25

**Filed by:** Frank
**Date:** 2026-04-05
**Status:** Research complete — evidence supports Proposal #25 as drafted

## Context

Shane asked for real-world evidence beyond our own sample files before proceeding with Proposal #25 (type system expansion — choice and date types). The concern: our samples were built to demo DSL features, not stress-test the type system.

## Research performed

Surveyed 6 systems via web documentation:
- **DMN/FEEL** — the OMG standard for business rule expression languages
- **Cedar** — AWS authorization policy language (minimal-by-design comparator)
- **Drools DRL** — dominant open-source Java business rule engine
- **NRules** — primary .NET business rule engine
- **BPMN** — business process modeling standard
- **SQL DDL** — universal data type baseline

Full research at `docs/research/language/references/type-system-survey.md`.

## Key findings

1. **`date` is universal consensus** — all 6 systems include a date type. Date-only at day granularity is viable (Cedar's `datetime("2024-10-15")` proves it).
2. **`enum`/`choice` is table stakes for enterprise rules** — Drools, NRules, SQL all have it. FEEL and Cedar lack it and users work around the absence.
3. **Proposal #25's rejections are validated** — `datetime` (timezone non-determinism per SQL cautionary tale), structured types (breaks flat model), overengineered duration (day-count suffices) are all confirmed as correct architectural calls.
4. **Constructor function pattern is standard** — FEEL and Cedar both use `type("literal")` constructors for temporal types.
5. **Decimal/money is growth-phase, not v1** — all systems have it but Precept's `number` already handles decimals.

## Decision

The external evidence **strongly supports** Proposal #25 as drafted. `choice` and `date` are the two highest-value type additions by cross-system consensus. No changes to the proposal framing are indicated by the research.

**Next step:** Shane sign-off on Proposal #25 to authorize implementation planning.

---

# Decision: Type System Semantic Contracts

**Filed by:** Frank (Lead/Architect & Language Designer)
**Date:** 2026-06-12
**Status:** Research captured — awaiting Shane sign-off
**Grounding:** `docs/research/language/references/type-system-survey.md`

## Recommendation

The type-system survey identifies 7 semantic contracts that should be locked before any of #25, #26, #27, or #29 moves to implementation:

1. **No silent coercion.** Widening (`integer → decimal`, `integer → number`) is implicit because it's value-preserving. Narrowing always requires an explicit function. `decimal ↔ number` mixing is a type error.

2. **Unambiguous literal parsing.** `42` → `integer`, `3.14` → `number`, `date("...")` → `date`. No context-dependent inference.

3. **Static operator validity.** Every operator combination resolves at compile time. No runtime type dispatch.

4. **Choice is a closed type.** Not decorated string. Cross-field choice-type incompatibility even with identical value sets.

5. **Date is naive calendar day.** No timezone, no time-of-day. Deterministic by construction.

6. **Decimal means exact arithmetic.** `maxplaces` is a constraint (reject violations), not a coercion (silently round).

7. **Integer is semantically distinct from decimal.** Different division behavior, different representation, different coercion paths.

## Why This Matters Now

All four proposals already cite this survey document. If these contracts are not reviewed before implementation starts, each proposal will independently reinvent type-boundary rules, producing inconsistencies between `choice`, `date`, `decimal`, and `integer` that would have to be reconciled after the fact.

## Non-goals Confirmed

The survey explicitly locks out: `datetime`/timezone, `money` scalar, parameterized types, records/maps, `any`/dynamic, host-type leakage, implicit boolean conversion from numerics, and string ordering.

## Action Requested

Shane to review and approve the 7 contracts as the shared semantic foundation for Wave 3 implementation.

---

# Frank — workflow migration surfaces

## Decision

Precept should standardize all issue-process guidance and automation on one lifecycle:

`Backlog -> Ready -> In Progress -> In Review -> Done`

Use `blocked` and `deferred` only as exception labels. Keep `squad` and `squad:{member}` as the routing surface. Keep taxonomy labels separate from status. Remove proposal-specific status-label guidance (`needs-decision`, `decided`) and the older triage-verdict/status-label layer (`go:*`, `squad:untriaged`, `next-up`) anywhere it is being used as workflow state.

## Migration surfaces

### Active repo surfaces
- `.github/workflows/sync-squad-labels.yml`
- `.github/workflows/squad-triage.yml`
- `.github/agents/squad.agent.md`

### Repo governance / skill surfaces
- `.squad/templates/issue-lifecycle.md`
- `.squad/skills/unified-issue-workflow/SKILL.md`
- `.squad/skills/issue-workflow-normalization/SKILL.md`

### Template / future-project source copies
- `.squad/templates/workflows/sync-squad-labels.yml`
- `.squad/templates/workflows/squad-triage.yml`
- `.squad/templates/workflows/squad-label-enforce.yml`
- `.squad/templates/squad.agent.md`
- `.squad/templates/skills/architectural-proposals/SKILL.md`

## Guidance

1. Stop simulating board status with labels.
2. If a workflow only assigns routing, let it assign routing only.
3. If future automation needs to move project status, add dedicated Projects-v2 automation rather than reintroducing status labels.
4. Keep proposal issues on the same workflow as bugs/features/docs; proposal-ness is taxonomy, not lifecycle.

---

# George — Computed Fields Re-Review (Issue #17)

**Date:** 2026-04-08
**Requested by:** Shane
**Input:** Proposal (`temp/issue-body-rewrites/17.md`), research doc (24-system precedent survey), language design, runtime engine code
**Prior review:** Session findings identified 3 gaps — recomputation timing, nullable handling, dependency ordering

---

## Verdict: `feasible-with-caveats`

The research strengthens the case rather than weakening it. The 24-system precedent survey shows extremely consistent convergence on the exact contract Precept would implement: read-only, declared once, automatically recomputed, direct assignment rejected. No system in the survey combines field-level derivation with lifecycle-aware constraint enforcement — that gap is precisely what Precept occupies. Implementation risk is unchanged from the first review; the caveats are specification precision, not design flaws.

---

## Per-Contract Assessment

### Contract 1: Scope Boundary
**Research recommendation:** Fields + safe collection accessors only; no event args, no cross-precept.
**Proposal says:** "Can only reference fields and collection accessors. Event arguments not in scope."
**Implementation difficulty:** Low
**Proposal precision:** Sufficient

The proposal matches the research recommendation exactly. The expression evaluator already has context-scoping infrastructure — rejecting event arg references at type-check time is a straightforward addition. No proposal edits needed for this contract.

### Contract 2: Recomputation Timing
**Research recommendation:** Enumerate Fire, Update, AND Inspect pipelines separately.
**Proposal says:** "After field mutations, before invariant checks" (single generic statement).
**Implementation difficulty:** Low
**Proposal precision:** Insufficient — needs pipeline-specific language

Having read the engine code, the actual insertion points are clear:

| Pipeline | Insert recomputation after | Before |
|---|---|---|
| **Fire (transition)** | `CommitCollections` (after exit actions + row mutations + entry actions) | Stage 6 validation |
| **Fire (no-transition)** | `CommitCollections` (after row mutations) | Invariants + in-state asserts |
| **Update** | `CommitCollections` (after patch application) | Stage 4 rules evaluation |
| **Inspect** | Same position as Fire simulation | Constraint check |

All four paths follow the same pattern: **after CommitCollections, before any constraint evaluation**. This is important because collection accessors (`.count`, etc.) need committed collection state to return correct values. The proposal's "after field mutations" is directionally correct but doesn't account for the CommitCollections boundary — and it omits Update and Inspect entirely.

### Contract 3: Nullability and Accessor Safety
**Research recommendation:** Decide how nullable inputs and empty-collection accessors interact with "always produce a value."
**Proposal says:** "Cannot be `nullable`" (forbids `nullable` keyword on the field itself).
**Implementation difficulty:** Medium
**Proposal precision:** Insufficient — conflates field nullability with expression nullability

The proposal's non-nullable rule is correct but incomplete. Two sub-problems:

**3a. Nullable field references in expressions.** If `field OptionalName as string nullable` exists, what happens with `field NameLen as number -> OptionalName.length` (hypothetical) or even just `field Echo as string -> OptionalName`? The DSL has no null-coalescing operator. The type checker must conservatively reject computed expressions that reference nullable fields. This is a type-system rule, not just a syntax rule.

**3b. Collection accessor safety.** `.count` always returns a number (safe). `.peek`, `.min`, `.max` on empty collections are undefined. The proposal says collection accessors work in computed expressions but doesn't specify empty-collection behavior. Until these accessors have guaranteed non-null semantics (or until the language gains null-coalescing), computed field expressions using them on potentially-empty collections must either be rejected or the accessors must be defined to return a typed default.

Current practical impact: `.count` is the only collection accessor that's unambiguously safe. A computed field like `field RejectionCount as number -> AllRejections.count` works. A computed field like `field LastEntry as string -> Queue.peek` does not — and the proposal doesn't say that.

### Contract 4: Dependency Ordering and Cycles
**Research recommendation:** Compile-time dependency graph, topological sort, cycle diagnostics with readable path.
**Proposal says:** "Topological sort at compile time; circular references produce compile error with cycle path."
**Implementation difficulty:** Medium
**Proposal precision:** Sufficient

This is new infrastructure (dependency graph construction, Kahn's algorithm or DFS-based topo sort, cycle path extraction, DiagnosticCatalog expansion) but it's well-understood CS. MySQL's precedent — generated columns may reference earlier generated columns in declared order — is exactly the model. No ambiguity in what to implement. The medium difficulty rating is for code volume, not conceptual risk.

### Contract 5: Writeability and External Input
**Research recommendation:** Explicitly state behavior across all mutation surfaces — `edit`, `set`, CreateInstance, Update, hydration, MCP payloads.
**Proposal says:** "Cannot appear in `edit` blocks. Cannot be target of `set`."
**Implementation difficulty:** Low
**Proposal precision:** Insufficient — silent on API-boundary behavior

The proposal covers DSL-surface restrictions (edit blocks, set actions) but not API-surface behavior. What happens when a caller passes a computed field value in:
- `CreateInstance(instanceData: { "TotalCost": 99 })`?
- `Update(patch => patch.Set("TotalCost", 99))`?
- MCP `precept_fire` with a data payload including computed fields?

Terraform's precedent is clear: **reject, don't ignore.** Silent ignoring creates debug confusion. The proposal should say: "Caller-provided values for computed fields are rejected at the API boundary with a descriptive error. They are not silently dropped."

### Contract 6: Tooling and Serialization Surface
**Research recommendation:** Specify what tools expose — formula, result, or both.
**Proposal says:** "Hover shows computed expression" and "MCP tools serialize computed fields correctly."
**Implementation difficulty:** Low
**Proposal precision:** Borderline — implementation scope lists items but doesn't spec output shape

The implementation scope mentions hover, MCP serialization, and completions. These are followable but could be more precise. Specifically:
- `precept_compile` should include the derivation expression in the field model.
- `precept_inspect` / `precept_fire` output should show the current computed value alongside other field data (not hidden or separate).
- Hover should show `computed: Quantity * UnitPrice` (formula) and ideally the resolved type.

This is not a blocker — it's standard LS/MCP work — but the acceptance criteria should include "computed field values visible in inspect/fire output."

---

## Does the Research Change Implementation Cost or Risk?

**Cost:** Unchanged. The research validated the same contract my first review identified. No new runtime mechanisms are needed beyond what the proposal already describes. The implementation breaks down to:
- Parser: low (one new optional combinator in field declaration)
- Type checker: medium (null-safety validation + cycle detection = new infrastructure)
- Runtime: low (one recomputation call after CommitCollections in each pipeline path)
- LS/MCP: low (standard additions)

**Risk:** Slightly reduced. The 24-system survey provides extremely strong precedent convergence. Every system that supports derivation converges on the same read-only, auto-recompute, reject-writes contract. This is not a novel design — it's a well-understood pattern being applied to Precept's specific lifecycle model. The risk is in specification precision (addressed below), not in the concept.

---

## Specific Proposal Edits Needed

1. **Replace the single recomputation timing sentence** with pipeline-specific language: "Computed fields are recomputed in all pipelines after CommitCollections and before constraint evaluation: Fire (after exit actions + row mutations + entry actions are committed), Update (after patch operations are committed), and Inspect (mirrors Fire simulation). Guards and invariants always see fresh computed values."

2. **Add a nullable-input type-checking rule** under Semantic Rules: "Computed field expressions that reference nullable fields produce a compile error. The type checker conservatively rejects any expression path that could produce null. A future null-coalescing operator may relax this restriction."

3. **Add an acceptance criterion for nullable field reference rejection:** "Expression referencing a nullable field produces compile error."

4. **Specify empty-collection accessor behavior** under Semantic Rules: "Only collection accessors with guaranteed non-null return types may appear in computed expressions. `.count` is always safe. `.peek`, `.min`, and `.max` on potentially-empty collections are rejected unless the language gains a null-safe accessor or null-coalescing operator."

5. **Add acceptance criterion for empty-collection accessor rejection:** "Expression using `.peek`/`.min`/`.max` on a collection in a computed field produces compile error (no guaranteed non-null return)."

6. **Add API-boundary rejection rule** under Semantic Rules: "Caller-provided values for computed fields in CreateInstance, Update, or MCP payloads are rejected with a descriptive error, not silently dropped."

7. **Add acceptance criteria for API-boundary rejection:** "CreateInstance with computed field value in instanceData produces error. Update patch targeting computed field produces error. MCP tool payload with computed field value produces error."

8. **Add acceptance criterion for inspect/fire output visibility:** "Computed field values appear in `precept_inspect` and `precept_fire` output alongside other field data."

---

## Summary

The research doc is excellent — it covers the right systems, draws the right conclusions, and identifies the right contracts. The proposal's core design (read-only, field-local, auto-recompute, `->` syntax) is validated by overwhelming precedent. The remaining caveats are all specification-precision issues, not design flaws. Once the 8 edits above are applied, I can implement this with confidence. The largest single piece of new infrastructure is the dependency graph + topological sort in the type checker, which is well-understood and bounded.

---

# Static Reasoning Expansion — George

**Date:** 2026-05-30
**Author:** George (Runtime Dev)

## Summary

Executed the Batch 3 Static Reasoning Expansion research pass. Created a comprehensive durable research doc covering contradiction detection, deadlock detection, satisfiability analysis, and range propagation as a coherent horizon domain.

## Deliverable

`docs/research/language/references/static-reasoning-expansion.md` — covers background anchored in C4/C5 implementation status, per-philosophy-category precedent survey (formal specification languages, validators, decision/policy engines, enterprise platforms, state machines, type systems, databases, orchestrators), philosophy fit, AST reducibility contracts, dead ends, summary table, and activation criteria.

## Key findings

### 1. C4/C5 are the right first target and the right scope boundary

Same-preposition contradiction (C4) and cross-preposition deadlock (C5) are already in the design spec and already spec'd correctly: per-field interval analysis on the expression AST, cross-field expressions assumed satisfiable, no false positives. The research confirms this is the correct fragment. DMN table overlap analysis (Camunda) and Cedar shadow analysis are the closest commercial precedents — both operate on per-input interval analysis, and both stop at the same cross-field boundary Precept already drew.

### 2. The interval reduction pass is the key infrastructure item

C4, C5, and any future global invariant satisfiability check all require the same per-field interval reducer over the expression AST. The AST reducibility table (in the research doc) is the core engineering contract — which expression forms reduce to intervals and which get assumed satisfiable (returned as ⊤). That reducer is the shared foundation; everything else in this lane is a caller.

### 3. Global invariant satisfiability is the natural C4/C5 companion

C4/C5 check per-state assertion sets. The obvious companion check is per-definition: if the full set of `invariant` declarations produces an empty per-field domain, no valid entity instance can ever be created. Same algorithm as C4 (interval intersection), different scope (all invariants instead of one state's asserts). Same severity (Error). This should be designed and implemented in the same wave as C4/C5 — not a separate proposal.

### 4. Range propagation through guards is explicitly not recommended for near-term

TypeScript and Kotlin both declined numeric range propagation after active compiler investment. The value for Precept is low because the inspector already catches data-dependent violations by simulation. This lane is deferred, not rejected permanently.

### 5. Full SMT and cross-field LP analysis are permanently rejected for the first wave

Not because they're technically intractable — they're decidable. But the diagnostic quality (author-opaque infeasibility witnesses), the native binary dependency (Z3), and the performance risk on always-on compile paths all cut against the product's identity. Cedar validates that SMT is commercially viable for authorization policy sets; it also shows why Precept's scalar entity fragment doesn't need it.

## Implementation sequencing recommendation

1. Implement C4/C5 after Batch 1 and Batch 2 language lanes have settled (type system, expression expansion).
2. Design global invariant satisfiability in the same proposal as C4/C5 — same algorithm, same implementation wave.
3. Do not design a range-propagation-through-guards proposal until C4/C5 are shipped and the interval reduction pass performance is known.
4. Do not open a cross-field satisfiability proposal at all without new evidence from author pain reports.

## No design gate issues

All four items above are research findings, not design decisions requiring immediate sign-off. The activation criteria in the research doc define when a proposal should be opened. No current proposal is ready.

---

# George — Type System Semantics Recommendations

**Date:** 2026-05-18
**Requested by:** Shane
**Status:** Recommendations for team review — not yet accepted

---

## Context

After completing the theory/semantics expansion of `docs/research/language/references/type-system-survey.md`, the following durable semantic recommendations emerge from the cross-system evidence. These apply to the four current proposals (#25, #26, #27, #29) and to any future type-system work.

---

## Recommendations

### R1: Maintain the three-way numeric split permanently

`integer`, `decimal`, and `number` serve three distinct semantic domains (counting, exact measurement, approximation). The survey confirms that every major database and enterprise platform maintains all three. The MONEY anti-pattern in PostgreSQL is the strongest evidence that conflating these domains produces systems that cannot be trusted. This split is not a v1 compromise — it is the correct permanent model.

**No cross-domain implicit coercion.** `decimal + number` must remain a type error now and in all future waves. Widening paths: `integer → decimal` and `integer → number` only.

### R2: `.count` accessor return type should be `integer`, not `number`

`.count` returns a count of elements — a whole number by definition. The current `number` return type is a backward-compatibility compromise for a language that didn't have `integer`. When `integer` ships (#29), `.count` should be refined to return `integer`. This is not breaking: `integer` widens to `number`, so `collection.count > 0` (where `0` is now inferred as `integer`) remains valid. The refinement should be part of the #29 acceptance criteria.

### R3: `date - date → integer` (not a duration type)

The `date - date` operation should return `integer` (day count), not a `duration` type, in v1. This is consistent with C#'s `DateOnly` (`DayNumber` difference), aligns with the `integer` proposal's acceptance criteria, and avoids the FEEL two-duration-type complexity until there is domain evidence that duration types are needed. When duration is eventually added, `date - date → duration` can replace `date - date → integer` — but only after a dedicated `duration` type design is complete.

### R4: Integer literals infer as `integer`; decimal-point literals infer as `number`

`5` should be inferred as `integer`; `5.0` as `number`. This is the universal convention across C#, Java, Python, and Cedar. It affects how mixed-type expressions resolve. For example, `date + 5` (where `5` is `integer`) is a valid date arithmetic expression; `date + 5.0` (where `5.0` is `number`) should be a type error. This literal inference rule should be included in the #29 parser specification.

### R5: Banker's rounding is the only rounding mode for `round()`

`round(decimal, N)` uses `MidpointRounding.ToEven` (banker's rounding). No other rounding mode in v1. If future proposals require different modes (half-up for display formatting, for example), they should be explicit parameters: `round(amount, 2, "halfUp")`. The default being banker's rounding is correct — it is statistically neutral and matches C# and Python defaults.

### R6: `set of choice(...)` is typed at the element level, not via invariant

`add CollectionField "NotAMember"` should be a compile-time error, not a runtime constraint violation, for a `set of choice(...)` field. The element type of the collection is the choice type; membership is enforced by the type checker on `add` operations. This is functionally equivalent to `Set<DocumentType>` in C# — wrong element type is a compile error. The acceptance criteria for #25 should explicitly include this compile-time enforcement for collection `add` operations.

### R7: `maxplaces` fires at assignment only; intermediate arithmetic is unconstrained

The `maxplaces N` constraint applies when a value is assigned to a field — not during intermediate subexpressions. `UnitPrice * Quantity` may produce a result with more than `maxplaces` decimal places; the constraint fires when the result is assigned to a `LineTotal as decimal maxplaces 2` field. This matches SQL `DECIMAL(p,s)` behavior (stored-value precision is enforced; intermediate computation precision is broader). The `round()` function is the author's explicit tool to bring a computed value within the `maxplaces` constraint before assignment.

---

## What These Recommendations Do NOT Change

- The core type vocabulary for #25–#29 (choice, date, decimal, integer) — already locked in proposal bodies
- The `decimal + number → type error` rule — already locked in #27
- The `ordered` opt-in for choice comparisons — already locked in #25
- The constructor form for date literals — already locked in #26
- The `maxplaces` rejection-not-rounding semantics — already locked in #27

These recommendations clarify implementation details and add future-proofing language to the acceptance criteria. They are refinements, not new design directions.

---

# Type System Research Complete — George

**Date:** 2026-05-14
**Author:** George (Runtime Dev)

## Summary

Executed the Batch 1 type-system expansion research pass. Created two durable research docs covering `choice` (#25), `date` (#26), `decimal` (#27), and `integer` (#29) as a coherent domain.

## Deliverables

- `docs/research/language/expressiveness/type-system-domain-survey.md` — domain-level survey: sample corpus analysis (21 files), 10-domain 100-field count, cross-category precedent table (databases, languages, enterprise platforms, end-user tools, rule engines), philosophy fit, semantic contracts, dead ends, proposal implications.
- `docs/research/language/references/type-system-survey.md` — formal grounding and per-system deep-dives: PostgreSQL, SQL Server, MySQL, C#, TypeScript, Kotlin, F#, Rust, Python, Salesforce, Dynamics 365, ServiceNow, Excel, Google Sheets, Notion, FEEL, Cedar, Drools/NRules, Pydantic, Zod. Non-goals section and cross-system pattern summary table.

## Key findings for the team

1. **`money` type is permanently ruled out.** PostgreSQL, SQL Server, and Salesforce all tried it; all have community guidance against it. Precept's `decimal` + `choice(...)` currency-code pattern is the correct model across all surveyed systems.

2. **Dataverse named the same three numeric concepts.** Dataverse calls them Whole Number, Decimal Number, and Floating Point Number. Precept calls them `integer`, `decimal`, and `number`. The concepts are identical. Enterprise platforms have already validated this three-way split for entity modeling.

3. **`date` without timezone is the unanimous v1 call.** Every database system has a day-only type (`DATE`, `DateOnly`, `glide_date`). Every system that includes timezone produces well-documented footguns. The v1 deferral of `time` and `duration` is confirmed by FEEL's two-duration-type split — that complexity is not for v1.

4. **`choice` is universally recognized.** MySQL `ENUM`, Salesforce Picklist, Dataverse Choice, ServiceNow choice, Notion Select, Zod `z.enum(...)` — the concept exists at every level of the stack. The `ordered` opt-in is a Precept improvement over MySQL's implicit declaration-order ordering.

5. **Coercion hierarchy is the highest-risk semantic contract.** `decimal + number → type error` must hold unconditionally. Any implicit mixing of exact and approximate arithmetic defeats the precision guarantee. This is the one rule with no exceptions.

6. **Implementation sequencing:** `integer` before `decimal` (the mixed arithmetic rules depend on both being present), then `choice` and `date` independently.

## No design gate issues

All four proposals have existing issue bodies with locked design decisions. The research confirms those decisions. No new design concerns found that would require Frank's review before implementation begins.

## Kramer notification

The grammar and completions implications are in the domain survey (Proposal Implications section). Kramer will need:
- New type keywords: `choice`, `date`, `decimal`, `integer`
- New constraint keywords: `ordered`, `maxplaces`
- `choice(...)` syntax form for the grammar
- Date accessor patterns: `.year`, `.month`, `.day`, `.dayOfWeek`
- `round(...)` function call syntax

These are documented in the proposal bodies for #25, #26, #27, #29. The research docs provide context for why these are the correct surface choices.

---

# Kramer: Computed Fields Re-Review (Issue #17) — Tooling Feasibility

**Date:** 2026-04-08
**Requested by:** Shane
**Input:** `temp/issue-body-rewrites/17.md`, `docs/research/language/expressiveness/computed-fields.md` (§6), current grammar and analyzer

---

## Verdict: Medium-Effort

The syntax is a clean extension of the existing `fieldScalarDeclaration` grammar rule, completions cost is moderate (one new context position plus suppression rules), and hover/semantic tokens need minor additions. The main effort is in getting the new expression context right in completions and making hover show both formula and value.

---

## Tooling Breakdown

### 1. TextMate Grammar (`precept.tmLanguage.json`) — Low Effort

**Current state:** `fieldScalarDeclaration` matches `field Name as Type (nullable) (default Value)` with the tail captured as group 9 and recursed through `declarationKeywords`, `booleanNull`, `numbers`, `strings`.

**Required change:** Extend the `fieldScalarDeclaration` regex to recognize `->` after the type keyword as an alternative to `nullable`/`default`. Two approaches:

- **(A) Extend group 9 inner patterns** to include `#arrowOperator`, `#operators`, `#identifierReference`, `#numbers`, `#booleanNull`, `#collectionMemberAccess`. This is the minimal-diff option — the tail after `as Type` already captures everything after the type; we'd just add expression-related patterns to the inner list.
- **(B) Add a dedicated `fieldComputedDeclaration` pattern** before `fieldScalarDeclaration` that matches `field Name as Type -> Expr`. This gives better semantic specificity (separate scope name like `meta.field-declaration.computed.precept`) but is slightly more work.

**Recommendation:** Option B. It gives us a distinct scope name that semantic tokens and themes can target, and it keeps the regex cleaner. The `->` in field declarations is structurally different from `->` in action chains.

The `arrowOperator` pattern already matches `->` globally. No new keyword entry needed. The arrow in a field declaration will get `punctuation.separator.arrow.precept` scope from the existing rule, which is correct.

**Risk:** None. No ambiguity — the arrow is syntactically distinguishable from action chains because it appears after `as Type` in a field declaration line.

### 2. Completions (`PreceptAnalyzer.cs`) — Medium Effort

**Current state:** After `field Name as Type `, the analyzer offers `nullable` and `default`. After `field Name as Type nullable `, it offers `default`. No expression-context completions exist for field declarations.

**Required changes:**

1. **After `field Name as Type `** — add `->` to the suggestion list alongside `nullable` and `default`. This is a one-line addition to the existing regex branch.

2. **After `field Name as Type -> `** — new regex branch that triggers **expression completions** using `BuildDataExpressionCompletions(dataFields, collectionKinds)`. This matches the invariant expression context (field names + collection accessors, no event args). This is a ~5-line addition.

3. **Suppression: `-> ` must NOT suggest `nullable`, `default`, or constraint keywords.** The existing field-declaration completions must not fire once `->` has been typed. The new regex for `-> ` should be checked *before* existing field-tail completions.

4. **`edit` block suppression:** Computed fields must be excluded from `edit` block suggestions. This requires reading the parsed model to identify computed fields and filtering them out of `BuildItems(dataFields, ...)` in the edit context. Medium complexity — requires threading `computedFields` through the intellisense info.

5. **`set` target suppression:** Similarly, computed fields should be filtered out of `set` target completions. Same mechanism as #4.

**Risk (medium):** Items 4 and 5 require the intellisense infrastructure to track which fields are computed. Today `PreceptDocumentIntellisense.Analyze()` does not distinguish computed from regular fields. The `PreceptDocumentInfo` record will need a new `ComputedFields` set, populated by detecting `->` in the field declaration line. This isn't hard, but it touches several methods.

### 3. Semantic Tokens (`PreceptSemanticTokensHandler.cs`) — Low Effort

**Current state:** Tokens are catalog-driven via `[TokenCategory]` attributes on `PreceptToken` enum values. Field names get `preceptFieldName` semantic type.

**Required change:** No new semantic token type needed. Computed fields are still fields — they should get the `preceptFieldName` token. However, a **semantic modifier** like `readonly` (or the existing `preceptConstrained`) could be applied to distinguish computed fields visually. This would require:

- The semantic tokens handler to detect when a field name token belongs to a computed field
- Emitting the modifier alongside the base type

**Recommendation:** Use the existing `preceptConstrained` modifier or introduce a new `preceptComputed` modifier. Low cost either way — it's adding one modifier lookup in the field-context branch.

### 4. Hover (`PreceptDocumentIntellisense.cs`) — Medium Effort

**Current state:** `BuildFieldMarkdown()` shows `field Name as Type (default Value)`. For computed fields, it should show the derivation formula.

**Required changes:**

1. **`BuildFieldMarkdown` must detect computed fields** and render the formula:
   ```
   ```precept
   field TotalCost as number -> Quantity * UnitPrice
   ```
   Computed from: `Quantity * UnitPrice`
   ```

2. **The `PreceptField` model** currently has no property for the derivation expression. The parser must populate something (e.g., `PreceptExpression? DerivedExpression` or `string? DerivedExpressionText`) on `PreceptField`. The hover handler needs to render it.

3. **At runtime (inspect/update):** The hover should ideally show the **current computed value** alongside the formula when previewing. This is a stretch goal — it requires the preview webview to feed computed values back into hover context.

**Risk:** Hover quality depends on the `PreceptField` model carrying the expression text. If the parser only stores a parsed AST, the hover handler will need to reconstruct readable text from the AST or store the raw expression string.

### 5. MCP Tool Surface — Low Effort (but proposal must specify)

Computed fields must appear in:
- **`precept_compile` output:** Show the field with its derivation expression. Low effort — serialize the new field property.
- **`precept_inspect` / `precept_fire` output:** Show the current computed value in data snapshots. Low effort — the runtime already produces field values.
- **`precept_update` output:** Computed fields should be excluded from updateable fields. The MCP layer should reject or ignore them. Low effort.

---

## What the Proposal Currently Specifies vs. What's Missing

### Currently specified (adequate for tooling):
- ✅ Syntax: `field <Name> as <Type> -> <Expression>`
- ✅ Cannot be target of `set` (compile error)
- ✅ Cannot appear in `edit` blocks (compile error)
- ✅ Mutually exclusive with `default` and `nullable`
- ✅ "Hover shows computed expression" (acceptance criteria)
- ✅ "MCP tools serialize computed fields correctly" (acceptance criteria)

### Missing from proposal (needed for tooling clarity):

1. **Compile output contract:** The proposal says "MCP tools serialize computed fields correctly" but doesn't specify *what* that means. Should `precept_compile` output show the derivation expression text, a parsed AST, or just a `isComputed: true` flag? Tooling needs the expression text for hover, completions documentation, and MCP output.

2. **Inspect/update output contract:** §6 of the research asks whether inspect/update show computed values. The proposal's acceptance criteria don't cover this. Should `precept_inspect` previews show the computed value in the data snapshot? Should `precept_update` reject computed field names in the `fields` input or silently skip them?

3. **Hover spec: formula vs. value vs. both.** The proposal says "hover shows computed expression" but doesn't say whether hover also shows the *current value* (which depends on instance state). For a static DSL file without runtime context, showing the formula is sufficient. But the preview panel has runtime context — should hover there show `→ 150.00 (from Quantity * UnitPrice)`?

4. **Completions filtering in `edit` and `set` contexts.** The proposal specifies compile errors but doesn't mention that completions should proactively *exclude* computed fields from `edit` and `set` suggestion lists. This is a UX quality gap — users shouldn't see suggestions the compiler will reject.

5. **Semantic token modifier.** Should computed fields be visually distinguishable from regular fields in the editor? The proposal doesn't specify. A subtle visual distinction (e.g., italic via a semantic modifier) would aid readability.

6. **Expression text preservation.** The proposal doesn't specify whether the model stores the raw expression text or only a parsed AST. Tooling (hover, MCP compile output, diagnostics) benefits greatly from having the original expression text available, not just a reconstructed form.

---

## Risk Flags

- **Arrow ambiguity: NONE.** The `->` in `field X as number -> Expr` is unambiguous in the grammar because it appears after `as Type` in a field declaration line, not after a `from/on` header or action verb. TextMate line-level matching handles this cleanly.

- **Completion conflict: LOW.** The new `->` completion after `as Type` is additive. The only ordering risk is ensuring the computed-expression completions regex fires *before* the existing `nullable`/`default` branch once `->` is present.

- **Hover gap: MEDIUM.** Without the raw expression text on `PreceptField`, hover is limited to `(computed)` with no formula. This is the one place where a parser-level decision (store expression text or not) directly affects tooling quality. Flag this as a parser requirement.

- **Edit/set filtering: LOW-MEDIUM.** Requires threading a `ComputedFields` set through `PreceptDocumentInfo`. Not hard, but easy to forget. Should be an explicit acceptance criterion.

---

## Summary

| Surface | Effort | Notes |
|---------|--------|-------|
| TextMate grammar | Low | New `fieldComputedDeclaration` pattern, ~20 lines |
| Completions | Medium | New `->` suggestion + expression context + edit/set filtering |
| Semantic tokens | Low | Optional modifier, ~5 lines |
| Hover | Medium | Formula display depends on expression text in model |
| MCP tools | Low | Serialize new field property, filter update input |
| **Overall** | **Medium** | |

---

# Decision: Retire Legacy Proposal Labels via Sync Workflow

**Agent:** Kramer
**Date:** 2026-04-05
**Requested by:** Shane (workflow closeout)

## Decision

Added `needs-decision` and `decided` to the `RETIRED_LABELS` array in both the active workflow (`.github/workflows/sync-squad-labels.yml`) and the template copy (`.squad/templates/workflows/sync-squad-labels.yml`).

On next workflow run, those labels will be deleted from the repo. The 404-ignore guard is already in place, so the deletion is safe if either label doesn't exist yet.

## Rationale

The unified issue workflow (approved) replaces proposal-specific status labels with board state. `needs-decision` and `decided` were the last proposal-state labels not yet covered by the retirement list. The workflow was already cleaning up `go:*` and `squad:untriaged`; this finishes that pass.

## Impact

- No change to the five-stage workflow model
- No new labels added
- Only addition: two label names to an existing deletion list
- Template kept in sync so new repos provisioned from the template start clean

---

# Decision: Proposal Governance Finalized

**Author:** Newman
**Date:** 2026-04-05
**Status:** Applied

## Decision

Proposal storage policy is now uniformly enforced across all team charters and both copies of the `architectural-proposals` skill.

**Canonical rule:**
- **GitHub issues** are the proposal surface. All structured asks for Shane's sign-off go as issues.
- **`docs/` markdown** is for research, rationale, and implementation design support — the artifacts that explain *why* a decision was made and *how* to implement it.
- **`docs/proposals/`** is not a recognized location. It should not be created or used.

## What Changed

### Charters Updated

All five charters now include an explicit **Proposal Storage Policy** section:
- `frank/charter.md` — added policy to "How I Work" area
- `george/charter.md` — added policy to "How I Work" area
- `steinbrenner/charter.md` — updated "Research-to-proposal pipeline" to name GitHub issues explicitly
- `j-peterman/charter.md` — added policy to "How I Work" area
- `newman/charter.md` — added policy to "How I Work" area

### Skill Files Updated (both copies)

- `.squad/templates/skills/architectural-proposals/SKILL.md`
- `.copilot/skills/architectural-proposals/SKILL.md`

Changes in both:
- Frontmatter `tools`: replaced `create` (docs/proposals/) entry with `github` (create issue) entry
- Section heading: `Proposal Structure (docs/proposals/)` → `Proposal Structure (GitHub Issue)`
- Context paragraph: added explicit policy statement — proposals are GitHub issues, not markdown files
- Examples: removed `docs/proposals/squad-interactive-shell.md` reference; noted proposal lives in the issue

## Rationale

A prior patch to propagate this policy failed partway through. The inconsistency created ambiguity: agents reading the skill files could reasonably conclude that `docs/proposals/` was the intended workflow, even if individual charters didn't endorse it. With all surfaces now aligned, there is a single, unambiguous answer regardless of which file an agent reads first.

The policy distinction is real and load-bearing: GitHub issues are discoverable, reviewable, linkable, and trackable. Markdown files in `docs/proposals/` are not proposals — they are documentation. Conflating the two creates governance gaps (proposals without issues, issues without rationale docs). Keeping them separate preserves both.

---

---
author: newman
date: 2026-05-14
status: proposed
domain: proposal-workflow
requires-sign-off: shane
supersedes: newman-project-workflow-recommendation.md
---

# Decision: Label-Free Proposal Status — Squad Compatibility Analysis

## What Shane Asked

Remove status labels (`needs-decision`, `decided`, `deferred`) from proposal issues, in a way that still works with Squad.

---

## What Squad Currently Assumes About Labels

The key assumptions baked into the current Squad system:

| Assumption | Where It Lives |
|---|---|
| Proposal decision state is readable by querying `label:needs-decision` / `label:decided` / `label:deferred` | `squad.agent.md` coordinator instructions, `architectural-proposals` SKILL.md, `docs/research/language/README.md` |
| Open proposals are surfaced by `gh issue list --label "squad:{member}"` alongside any label-based filters | `squad.agent.md` — Ralph's work-check cycle |
| `state:closed label:decided` distinguishes accepted/rejected from deferred | SKILL.md closing workflow |
| `label:deferred` is the only way to identify intentionally parked proposals | Implied throughout — no project-field alternative exists today |

The MCP GitHub tools (`list_issues`, `search_issues`) operate on issue API fields: state, labels, assignees, title, body. **Project Status fields live in the Projects v2 GraphQL layer.** They are not exposed by any current MCP GitHub server tool. An agent cannot query `Proposal Status = Deferred` without a GraphQL query it does not have.

---

## The Three Status Labels, Analyzed Individually

### `needs-decision`

**Verdict: Redundant. Safe to drop.**

`state:open label:proposal` is an exact substitute. Every open proposal issue is, by definition, awaiting a decision. Adding `needs-decision` on top adds noise without adding information. Any AI agent query that currently reads `label:needs-decision` can be mechanically replaced with `state:open label:proposal`.

### `decided`

**Verdict: Redundant. Safe to drop.**

A closed proposal issue with a decision comment in its thread IS decided. The label restates what the close event and comment already express. Any agent query reading `label:decided state:closed` becomes `state:closed label:proposal` (excluding `deferred`). The only caveat: a closed proposal with no `deferred` label must be assumed decided, not merely abandoned — the close-out discipline (required decision comment before closing) is the enforcement mechanism here.

### `deferred`

**Verdict: Carries unique semantic. NOT safe to drop — yet.**

A deferred proposal is intentionally parked and potentially revisitable. Its GitHub state is `closed`. Without the `deferred` label, it is indistinguishable from a decided-and-resolved issue. No issue-API-visible field encodes this distinction today.

The only alternative that works for AI agent discoverability is a `Proposal Status` single-select field on the project board — but that field does not yet exist, the project is currently private, and no MCP GitHub tool can query project fields. Until all three of those conditions are resolved, removing `deferred` silently drops a semantic that agents currently rely on.

---

## Is Label-Free Viable Today?

**Partially.** A two-thirds migration is viable now:

- ✅ Drop `needs-decision` immediately
- ✅ Drop `decided` immediately
- ❌ Cannot drop `deferred` until a queryable replacement exists

**Full label-free is not viable today** due to three hard blockers:

1. **No `Proposal Status` field on the project** — the `Precept Language Improvements` board has only the generic `Todo / In Progress / Done` Status field. It cannot represent `Deferred` as a distinct outcome.
2. **The project is private** — even if the field existed, human repo viewers (and agents operating without project access) could not see it.
3. **No MCP project field query** — the GitHub MCP server does not expose Projects v2 GraphQL fields. AI agents cannot query `Proposal Status = Deferred` via any available MCP tool.

---

## What Full Label-Free Requires (Minimum Changes)

To reach a state where all three status labels can be removed without breaking Squad:

| # | Change | Owner | Blocker? |
|---|---|---|---|
| 1 | Add `Proposal Status` single-select field to **Precept Language Improvements** project | Shane | No — do immediately |
| 2 | Add values: `Needs Decision`, `In Review`, `Accepted`, `Rejected`, `Deferred` | Shane | No — do immediately |
| 3 | Make **Precept Language Improvements** project public | Shane | This unblocks human discoverability |
| 4 | GitHub MCP server exposes Projects v2 field queries | External (GitHub) | This unblocks AI discoverability — not in our control |
| 5 | Update Squad queries in `squad.agent.md` from label-based to project-field-based (or hybrid) | Newman | Depends on 4 |
| 6 | Update SKILL.md files to remove status-label requirements | Newman | Depends on 4 |
| 7 | Update `docs/research/language/README.md` proposal tracking section | Newman | Depends on 4 |

**The external blocker (item 4) is the constraint.** Steps 1–3 can be done now and will improve the human workflow and project board. But AI agent discoverability — the part Squad depends on — remains label-driven until the GitHub MCP server exposes project fields.

---

## Recommended Architecture for Precept

### Phase 1 — Do Now (no risk)

Drop `needs-decision` and `decided`. These carry no information that `state:open label:proposal` and `state:closed label:proposal` don't already express. Update the following surfaces in the same pass:

**Updated label set (proposal workflow):**

| Label | Keep? | Reason |
|---|---|---|
| `proposal` | ✅ Keep | Type marker. Essential for all filtering. |
| `language`, `runtime`, `mcp`, etc. | ✅ Keep | Domain taxonomy. |
| `dsl-compactness`, `dsl-expressiveness` | ✅ Keep | Slice taxonomy. |
| `squad:frank`, `squad:george`, etc. | ✅ Keep | Owner routing. Non-negotiable. |
| `needs-decision` | ❌ Drop | Redundant with `state:open label:proposal`. |
| `decided` | ❌ Drop | Redundant with closed state + decision comment. |
| `deferred` | ✅ Keep (for now) | Unique semantic — not yet expressible elsewhere. |

**Updated query equivalents for Squad:**

| Intent | Old query | New query |
|---|---|---|
| All active proposals | `label:needs-decision` | `state:open label:proposal` |
| All decided proposals | `label:decided state:closed` | `state:closed label:proposal` (excludes deferred) |
| All deferred proposals | `label:deferred` | `label:deferred` (unchanged) |

**Files to update:**
- `.copilot/skills/architectural-proposals/SKILL.md` — remove `needs-decision`/`decided` from required labels
- `.squad/templates/skills/architectural-proposals/SKILL.md` — same
- `docs/research/language/README.md` — update proposal tracking workflow section
- `squad.agent.md` instructions for Ralph's work-check cycle (if label-specific queries are hardcoded)

### Phase 2 — Prepare the project board (do now, pays off later)

Add a `Proposal Status` single-select field to **Precept Language Improvements** with values: `Needs Decision`, `In Review`, `Accepted`, `Rejected`, `Deferred`. Mirror the `deferred` label by setting the project field to `Deferred` when an issue gets the label. This makes the board semantically useful for humans even before AI tooling can query it.

Optionally make the project public. Public projects allow human contributors to see proposal state without repo access.

### Phase 3 — Drop `deferred` (when external blocker lifts)

When the GitHub MCP server exposes Projects v2 field queries (or when an alternative project-field query path exists), drop the `deferred` label. At that point:
- AI agents query `Proposal Status = Deferred` on the project board
- The project field is the sole source of truth for deferred state
- The label namespace is fully clean: `proposal`, domain labels, slice labels, squad routing only

---

## AI/Discoverability Impact Summary

Phase 1 loses zero discoverability. Phase 2 adds human visibility at no AI cost. Phase 3 completes the label-free model but requires an external dependency. The safe move is Phase 1 + 2 now, Phase 3 when the tooling catches up.

---

## Why Not Skip `deferred` Now

Two proposals in this queue (`deferred` is not yet applied to any, but the mechanism exists) represent the "intentionally parked" category. If we drop `deferred` today:
- A closed proposal with a "parking" comment looks identical to a decided-and-closed proposal
- An agent asked "what's been deferred?" has no queryable answer
- Re-opening a deferred proposal has no prior state marker to restore

The cost of keeping `deferred` is one label. The cost of dropping it prematurely is invisible state for the AI layer. Keep it.

---

## Open Questions for Shane

1. **Make the project public?** Needed for Phase 2 to benefit humans. No impact on AI until Phase 3.
2. **Add the `Proposal Status` field now?** I recommend yes — sets up the migration path and makes the board more useful immediately.
3. **Apply Phase 1 now (drop `needs-decision` and `decided`)?** I recommend yes — zero risk, cleaner label taxonomy today.

---

---
author: newman
date: 2026-05-14
status: proposed
domain: proposal-workflow
---

# Decision: Proposal Tagging and Workflow

## Context

The `architectural-proposals` skill described creating proposals as markdown files under `docs/proposals/`. In practice, the repo never used that directory. Instead, proposal work happened entirely in GitHub issues — which have labels, project placement, workflow state (open/closed, project column/status), and direct linkability. The `docs/research/` tree grew as the evidence base, with issue bodies linking to it.

The mismatch created ambiguity: agents following the skill would create markdown files that no one would find or triage, bypassing the actual workflow infrastructure.

## Decision

**GitHub issues are the canonical home for proposals. Markdown files in `docs/proposals/` are not a valid proposal artifact.**

Rationale:
- Issues carry workflow state (open, closed, in-progress) without extra tooling.
- Labels (`proposal`, `language`, `design`, `needs-decision`, etc.) make proposals filterable and discoverable.
- Project board placement gives proposals a lane (e.g., Backlog → In Review → Decided).
- Issue bodies support the full required section structure — they aren't structurally limited.
- Research evidence lives in `docs/research/` and is linked from the issue body; it isn't duplicated in the proposal itself.

## Workflow (adopted pattern)

### When opening a proposal issue

1. **Title format:** `[Proposal] <short imperative description>` — e.g., `[Proposal] Named rule declarations`
2. **Labels (required):**
   - `proposal` — marks it as a proposal, not a bug or task
   - Domain label: `language`, `runtime`, `mcp`, `plugin`, `tooling`, or `docs` — one required
   - Status label: `needs-decision` (open), `decided` (closed with resolution), or `deferred` (explicitly parked)
3. **Project placement:** Add to the relevant project board column (typically `Backlog` or `In Review`)
4. **Body structure:** Use the full required section format (see `architectural-proposals` skill) as the issue body
5. **Research link:** Under a `## Research corpus consulted` section, link the relevant `docs/research/` files

### Closing a proposal issue

- Close with a comment stating the decision reached and the rationale (1–3 sentences)
- Apply `decided` or `deferred` label before closing
- If the decision lands in `.squad/decisions.md` or as a `.squad/decisions/inbox/` record, link it in the closing comment

### What stays in docs

- `docs/research/` — evidence, audits, formal references that *support* proposals. Not proposals themselves.
- `docs/PreceptLanguageDesign.md` and other spec files — updated *after* a proposal is decided, not during proposal phase.

## What This Replaces

- The `create` tool step in `architectural-proposals` SKILL.md that said "Create proposal in docs/proposals/" — removed.
- Any mental model that proposals are private markdown files. They are tracked, labeled, linked GitHub issues.

## Impact

- `architectural-proposals` SKILL.md updated to reflect issue-first workflow.
- `docs/research/language/README.md` already aligned — no change needed.
- No `docs/proposals/` directory to clean up (it was never created).

---

# Soup Nazi — Computed Fields (#17) Re-Review: Testability Assessment

**Date:** 2026-04-08
**Requested by:** Shane
**Input artifacts:** `temp/issue-body-rewrites/17.md` (proposal), `docs/research/language/expressiveness/computed-fields.md` (research)
**Prior reviews referenced:** George (feasibility, Findings 1–3), Kramer (tooling, F1–F5)

---

## Verdict: MANAGEABLE

The test surface is large (~70 cases) but structurally well-organized. Existing test patterns in all three suites cover analogous feature categories (edit blocks, constraints, type checking, MCP tools). The **blocking issue** is not test complexity — it's **7 acceptance criteria gaps** where the proposal leaves behavior undefined, making tests unspecifiable until the proposal is amended.

---

## Estimated Test Case Breakdown

| Suite | Category | Count | Notes |
|-------|----------|-------|-------|
| **Precept.Tests** | Parser | 8 | Arrow syntax, mutual exclusions, precedence |
| **Precept.Tests** | Type Checker | 20 | Null-safety, cycles, dependency graphs, mutual exclusions, scope validation |
| **Precept.Tests** | Runtime | 22 | Recomputation timing (Fire + Update + entry/exit actions), guards, invariants, collection accessors, CreateInstance |
| **Precept.LanguageServer.Tests** | Completions + Hover + Tokens | 10 | Completion context after `->`, hover spec, semantic tokens, diagnostics |
| **Precept.Mcp.Tests** | All 5 tools | 10 | Compile output shape, fire/inspect/update computed values, update rejection |
| | **Total** | **~70** | |

---

## Acceptance Criteria Coverage Analysis

### Research §1 (Scope Boundary) — COVERED
- "Event argument reference produces compile error" ✓
- "Can only reference fields and collection accessors" ✓
- Cross-precept exclusion documented in out-of-scope ✓

### Research §2 (Recomputation Timing) — MAJOR GAPS

The proposal says "Re-evaluated after every mutation, before invariant checks." The research demands explicit contracts for **three distinct pipelines**. Current AC only covers Fire implicitly.

**Missing AC items:**
1. **Update pipeline recomputation.** `PreceptEngine.Update()` applies edits to fields that computed fields depend on. Must recompute before rules evaluation. Zero coverage in current AC.
2. **Inspect pipeline preview.** `PreceptEngine.Inspect()` must show post-recomputation values in preview output. Zero coverage in current AC.
3. **Entry/exit action timing.** Fire pipeline Stage 5 (entry actions) can mutate dependency fields AFTER Stage 4 (row mutations). If recomputation happens only once after Stage 4, entry action mutations produce stale computed values. The research says recompute after "all mutations in the chosen path, including exit actions, row actions, and entry actions." Proposal is ambiguous — "after every mutation" could mean one pass or per-stage. Must be explicit.

**Test scenarios blocked:** ~8 runtime tests + ~4 MCP tests cannot be written until these are specified.

### Research §3 (Nullability) — SIGNIFICANT GAPS

The proposal says "Cannot be nullable" and has AC for `nullable + ->` mutual exclusion. But:

**Missing AC items:**
4. **Nullable field references in computed expressions.** `field OptName as string nullable` + `field Bad as string -> OptName` — is this a compile error? The research and George's Finding 1 both flag this. Current type checker rejects nullable-to-non-nullable assignment (C42) but only in `set` context. Must add: "Computed expression referencing nullable field produces compile error."
5. **Empty collection accessor safety.** `.count` is always safe (returns 0). `.peek`/`.min`/`.max` on empty collections are undefined. Must state whether computed expressions using these are compile errors, runtime rejections, or produce default values. Research §3 explicitly requires this decision.

**Test scenarios blocked:** ~4 type checker tests + ~3 runtime tests cannot be written until these are specified.

### Research §4 (Dependency Ordering) — MOSTLY COVERED

AC says "Topological sort correct for linear chains and DAGs" and "Circular dependency produces compile error with cycle path." This is testable.

**Recommended additions (not blocking, but improve coverage):**
- Computed field depending on another computed field (multi-hop chain)
- Diamond dependency: A→{B,C}, B→D, C→D
- Self-referencing field: A→A (degenerate cycle)
- Cycle path error message format verification (e.g., "A → B → C → A")

### Research §5 (Writeability) — SIGNIFICANT GAPS

AC covers `edit` and `set` restrictions. But:

**Missing AC items:**
6. **CreateInstance behavior.** If caller passes `{"TotalCost": 42}` where `TotalCost` is computed, does `CreateInstance` ignore it, reject it, or overwrite it? Terraform precedent says reject. Proposal is silent. This directly affects `PreceptEngine.CreateInstance()` and MCP `precept_fire` (which calls CreateInstance implicitly).
7. **Hydration/serialization contract.** Are computed fields included in `InstanceData` after Fire/Update? If yes, they're serialized and visible to callers. If no, callers can't read computed values. Research §5 and Kramer's F5 both flag this.

**Test scenarios blocked:** ~4 runtime tests + ~3 MCP tests cannot be written.

### Research §6 (Tooling and Serialization) — PARTIALLY COVERED

AC says "Hover shows computed expression" and "MCP tools serialize computed fields correctly." But:

**Missing specification (not blocking tests, but blocking LS implementation):**
- Hover content: expression text only? Type annotation? Dependency list?
- Compile output: `PreceptField` gains `IsComputed` + `DerivedExpression`? New DTO?
- Inspect/Fire output: computed field values appear in `InstanceData`? Separate section?

Kramer's F1–F5 cover these tooling gaps thoroughly. Deferring to his findings for LS/MCP spec.

---

## Dead Ends vs. Proposal Coverage

| Dead End (Research §) | Proposal Coverage | Test Need |
|------------------------|-------------------|-----------|
| Event-argument-derived fields | AC: "Event argument reference produces compile error" ✓ | ✓ Covered |
| Lazy/cached evaluation | Implicit in "recomputation timing" design decision | No test needed — it's a design choice, not a runtime behavior |
| Writable computed fields | AC: `edit` + `set` restrictions ✓ | Partially covered — CreateInstance/Update/hydration gaps remain |
| Silent default fallbacks | **NOT ADDRESSED** | Need AC for what happens when expression would produce null |
| Cross-precept derivations | Explicit exclusion in out-of-scope ✓ | No test needed |

---

## Acceptance Criteria That MUST Be Added

Based on research findings cross-referenced with existing AC:

### Must-Add (Blocks Test Specification)

| # | Acceptance Criterion | Source |
|---|---------------------|--------|
| A1 | Update path: computed fields recomputed after direct field edits, before rules evaluation | Research §2, George F3 |
| A2 | Inspect preview: computed field values reflect post-recomputation state for both fire and update previews | Research §2, Kramer F1 |
| A3 | Entry/exit action mutations: computed fields recompute after ALL mutation phases (exit actions + row actions + entry actions), not just row mutations | Research §2, George F5 |
| A4 | Computed expression referencing nullable field produces compile error | Research §3, George F1 |
| A5 | Computed expression using `.peek`/`.min`/`.max` on potentially empty collection: behavior specified (compile error, runtime error, or safe default) | Research §3, George F2 |
| A6 | `CreateInstance` with computed field value in data dict: behavior specified (ignore silently, reject with error, or overwrite) | Research §5 |
| A7 | Computed field values appear in `InstanceData` after Fire/Update (serialization contract) | Research §5, §6, Kramer F5 |

### Should-Add (Improves Coverage, Not Blocking)

| # | Acceptance Criterion | Source |
|---|---------------------|--------|
| B1 | Computed field depending on another computed field evaluates in correct dependency order | Research §4 |
| B2 | Diamond dependency (A depends on B and C, both depend on D) resolves correctly | Research §4 |
| B3 | Self-referencing computed field (A → A) produces cycle error | Research §4 |
| B4 | Cycle path error message includes full cycle (e.g., "A → B → C → A") | Research §4 (already partially in AC) |
| B5 | Completions after `->` in field declaration offer field names, exclude event arguments | Kramer F1 |
| B6 | MCP `precept_compile` output includes computed field metadata (expression, dependencies) | Kramer F5 |
| B7 | MCP `precept_update` with computed field in patch returns rejection | Research §5 |

---

## Edge Cases Surfaced by Research Not in Proposal

1. **Computed field depends on field mutated only by entry action** — recomputation timing matters at a granularity the proposal doesn't specify.
2. **Computed field referencing another computed field that references a nullable field** — transitive null safety. Conservative rejection cascades.
3. **Computed field that produces a value violating an invariant at CreateInstance time** — e.g., `field Total as number -> X + Y` with `invariant Total > 0` when X=0, Y=0. Is this a constraint failure at creation?
4. **All computed fields have no inputs yet (all dependencies at default values)** — what's the initial computed value? Evaluated once at creation? Or only on first mutation?
5. **Computed field referencing a collection that's modified via `add`/`remove` in a transition row** — collection commit timing vs. recomputation timing.
6. **Multiple computed fields with overlapping dependencies but different types** — type checker must trace type through dependency chain.
7. **MCP `precept_fire`/`precept_update` returning computed field values in `data` dict** — if the value changes, does the output reflect pre- or post-recomputation state?
8. **Computed field with parenthesized sub-expressions** — parser must handle `field X as number -> (A + B) * C`.

---

## Risk Assessment

| Risk | Level | Mitigation |
|------|-------|-----------|
| 7 AC gaps block ~19 test scenarios | **HIGH** | Amend proposal before implementation |
| Dependency graph + topological sort is new infrastructure | **MEDIUM** | Well-understood algorithm; test patterns exist in PreceptTypeCheckerTests |
| Three-pipeline recomputation (Fire/Update/Inspect) | **MEDIUM** | Each pipeline already has test coverage for existing features; computed field recomputation follows same insertion points |
| Parser arrow ambiguity (field `->` vs. transition `->`) | **LOW** | Context-disambiguated; parser tests will catch regressions |
| Regression risk to existing tests | **LOW** | Computed fields are additive; no existing syntax changes |

**Overall regression risk: LOW.** Computed fields are purely additive — no existing syntax or behavior changes. The 666 existing tests should pass unchanged.

---

## Recommendation

**Amend the proposal** with the 7 must-add ACs (A1–A7) before implementation begins. Once amended, the test plan is straightforward: ~70 tests across well-understood categories using established patterns from `PreceptEditTests`, `PreceptTypeCheckerTests`, `NewSyntaxRuntimeTests`, `UpdateToolTests`, and `InspectToolTests`.

The research document is excellent — it surfaced contracts that would have become test-time ambiguities or post-release bugs. Every semantic contract from the research maps to a testable behavior. The dead ends are well-documented and mostly already covered by existing AC.

**No code soup for you until those 7 ACs are in the proposal.**

---

# Decision: Computed Fields (#17) — PM Gate Re-Review (2026-04-08)

**Filed by:** Steinbrenner (PM)
**Status:** Proposed — awaiting Shane review
**Category:** `dsl-expressiveness`
**Input artifacts:** `temp/issue-body-rewrites/17.md`, `docs/research/language/expressiveness/computed-fields.md`, `docs/@ToDo.md`

---

## Verdict: NEEDS REVISIONS FIRST

The research is strong — 24 systems across all 7 philosophy positioning categories, with a clear cross-category finding that no existing system combines field-level derivation with lifecycle-aware constraint enforcement. The proposal's core design (read-only, field-local, topologically ordered, auto-recomputed) is sound and well-grounded. But three semantic contracts are unresolved, six acceptance criteria are missing, and the wave label is stale. These are bounded tightening edits, not a rethink.

---

## Philosophy Filter

| Dimension | Assessment |
|-----------|-----------|
| **User need** | **Strong.** Manual synchronization pain is real and visible in 12+ of 21 samples. Formula drift, single-source-of-truth loss, and review friction are recurring problems. Not speculative. |
| **External precedent** | **Exceptionally strong.** 24 systems surveyed. Every system that supports derivation converges on the same contract: read-only, declared once, auto-recomputed. PostgreSQL, SQL Server, MySQL, Salesforce, Dynamics 365, ServiceNow, Pydantic, spreadsheets — all align. |
| **Philosophy fit** | **Good under narrow contract.** Computed fields strengthen prevention (auto-recompute eliminates drift), one-file completeness (formula lives at field declaration), determinism (pure expressions, topological order), and inspectability (formula + value visible). The research correctly identifies the narrow contract boundary. |
| **Non-goals** | **No violations.** Proposal explicitly excludes event-arg scope, cross-precept refs, lazy eval, writable computed fields, silent default fallbacks. All of these would violate philosophy. |

**Philosophy Filter: PASS.**

---

## Wave Placement Recommendation

**Proposed by issue:** Wave 4: Compactness + Composition
**Recommended:** Reclassify as **Composition** — not compactness.

The research's cross-category finding reframes this: computed fields are not about writing fewer characters (compactness). They are about composing derivation with lifecycle-aware constraint enforcement — a combination no surveyed system provides. That places this squarely in **composition** territory.

Practically, the roadmap (Groups 1-4c) is fully complete. All infrastructure is in place: DiagnosticCatalog, structured constraint violations, MCP 5-tool surface, graph analysis. Computed fields would be the first new language feature after foundational work. There is no blocking dependency. The proposal should drop the wave label and instead state its position as "first post-foundation feature" or adopt whatever sequencing model replaces waves.

---

## Gate-Before-Start Decisions (Shane must decide)

These are the semantic contracts that cannot be deferred to implementation. Each has an explicit default-if-skipped.

### 1. Null-safety rule for computed expressions
**Options:**
- **(a) Conservative rejection** — type checker rejects any computed expression that references a nullable field. Computed fields can only depend on non-nullable fields and safe collection accessors (`.count`). This blocks some practical use cases but is sound.
- **(b) Permissive with future null-coalescing** — allow nullable refs but defer to a future `??` operator for null handling. Unsound until that operator ships.

**Default if skipped:** (a) Conservative rejection. It's the only option that preserves "computed fields always produce a value" without new language surface.

### 2. Collection accessor behavior on empty collections
**Options:**
- **(a) Exclude `.peek`/`.min`/`.max` from computed expressions** — only `.count` (always safe) is allowed. Simplest, most restrictive.
- **(b) Allow but require non-empty guard** — type checker requires a `when Collection.count > 0` guard or similar before allowing unsafe accessors. Complex; may not fit computed field context.
- **(c) Define default values for empty accessors** — `.peek` returns type default, `.min`/`.max` return 0/empty string. Violates "no silent default fallbacks" dead end.

**Default if skipped:** (a) Exclude unsafe accessors. The research's "Dead Ends" section explicitly rejects silent defaults, so (c) is out. (b) adds guard complexity inappropriate for first pass. (a) is clean and expandable later.

### 3. External input contract (CreateInstance/Update/hydration/MCP)
**Options:**
- **(a) Reject** — providing a computed field value in CreateInstance, Update, or MCP payloads is an error. Follows Terraform precedent.
- **(b) Ignore** — silently drop computed field values from external input. Convenient but hides caller mistakes.

**Default if skipped:** (a) Reject. The research cites Terraform's model; Precept's philosophy favors explicit errors over silent behavior.

### 4. Recomputation timing in Fire pipeline
**Options:**
- **(a) Single pass after all mutations** — recompute once after exit actions + row mutations + entry actions (after Stage 5), before validation (Stage 6).
- **(b) Two passes** — recompute after row mutations (Stage 4) and again after entry actions (Stage 5). More correct but more expensive.

**Default if skipped:** (a) Single pass after all mutations, before validation. Prior review (George) flagged that entry actions can mutate dependency fields, so the recompute must happen *after* entry actions, not between row mutations and entry actions.

---

## In-Flight Decisions (Executor resolves)

These do not need Shane's attention. The implementing agent resolves them during the PR:

1. **DiagnosticCatalog code allocation** — new codes for nullable+derived, default+derived, constraints+derived, cycle detection, event-arg-in-derived, set-target-derived, edit-target-derived. Follow existing catalog pattern.
2. **Hover content format** — show expression text + resolved type. Dependencies optional for first pass.
3. **MCP compile output DTO shape** — extend `PreceptField` with `IsComputed` flag and `Expression` property, or introduce a dedicated shape. Follow existing DTO conventions.
4. **Completion trigger patterns** — add `->` as completion option after type keyword in field declarations. Build a `BuildComputedFieldExpressionCompletions` that excludes event args.
5. **Grammar pattern ordering** — `->` in field context vs. action context. Context-disambiguated by surrounding patterns.
6. **Test organization** — follow existing xUnit/FluentAssertions conventions.
7. **Sample selection** — which 1-2 samples to add or retrofit.

---

## Out of Scope for First Pass

1. Null-coalescing operator (`??`) — future feature; computed fields launch with conservative null-safety
2. Unsafe collection accessors (`.peek`/`.min`/`.max`) in computed expressions — deferred pending null-safety story
3. Cross-precept field references
4. Conditional derivation (`if` expressions in computed fields) — only if `if` expressions already exist
5. Computed fields referencing event arguments
6. Recursive or fixed-point evaluation

---

## Specific Proposal Edits Required Before Shane Review

### Must-fix (blocks sign-off)

1. **Replace "Open questions: None"** with the 4 Gate-Before-Start decisions above, each with options and Shane's chosen resolution once decided.

2. **Add Update pipeline recomputation** to "Recomputation timing" semantic rule. Current text says "After field mutations, before invariant checks" — must explicitly state this applies to both Fire and Update pipelines.

3. **Add Inspect API integration** to semantic rules and acceptance criteria: "Inspect output includes fresh computed values reflecting hypothetical state for both event-inspect and update-inspect."

4. **Add null-safety rule** to semantic rules: specify that computed expressions may only reference non-nullable fields and safe collection accessors (per Gate decision #1).

5. **Add collection accessor safety** to semantic rules: specify which accessors are allowed in computed expressions (per Gate decision #2).

6. **Add external input contract** to semantic rules: specify reject behavior for computed field values in CreateInstance/Update/hydration/MCP (per Gate decision #3).

7. **Add 6 missing acceptance criteria:**
   - Expression referencing nullable field produces compile error
   - Update path: computed fields recomputed before rules evaluation
   - Inspect output includes fresh computed values
   - CreateInstance/Update with computed field value produces error
   - Unsafe collection accessors (`.peek`/`.min`/`.max`) in computed expressions produce compile error
   - Entry action mutations reflected in computed field values before validation

### Should-fix (improves quality)

8. **Update wave label** — drop "Wave 4: Compactness + Composition" and replace with composition framing or "first post-foundation feature."

9. **Add hover spec** — state what hover shows: expression text, resolved type, and (optionally) dependency list.

10. **Add MCP serialization shape** — state whether `precept_compile` output includes `IsComputed` flag and `Expression` on field DTOs.

11. **Add recomputation timing to Fire pipeline** — clarify single pass happens after entry actions (Stage 5), not after row mutations (Stage 4).

---

## Research Adequacy

The research document is **implementation-ready** for the core design. It covers:
- 24 systems across all 7 philosophy positioning categories (complete)
- Cross-category gap analysis with clear finding (strong)
- 6 semantic contracts identified with options (thorough)
- 5 dead ends documented with reasoning (thorough)
- Philosophy fit analysis against all core commitments (good)

**No additional research needed.** The research identifies the right questions; the proposal just needs to answer them.

---

## Summary

The computed fields feature is well-motivated, thoroughly researched, and philosophically sound. The proposal needs a tightening pass: resolve 4 semantic contracts (null-safety, accessor safety, external input, recomputation timing), add 6 missing acceptance criteria, and update the wave label. Once those edits land, this is ready for Shane's sign-off.

---

# Entity-Modeling Surface — Positioning Decision

**Author:** Steinbrenner (PM)
**Date:** 2026-04-08
**Status:** Research complete — ready for team review
**Related:** Proposal #17 (computed / derived fields), Proposal #22 (data-only precepts)

## Decision needed

Confirm the category line for Precept's entity-modeling surface:

- Precept should stay **one language for governed entities** across two explicit axes:
  1. **lifecycle-heavy ↔ lifecycle-light**, and
  2. **stored facts ↔ derived facts**.

## Key findings

1. **Computed fields and data-only precepts are the same category question.** One asks whether derived facts belong inside the entity contract; the other asks whether lifecycle-light entities belong inside the same contract language. The answer to both is yes when the governing unit is the entity.

2. **Adjacent systems split along only one axis at a time.** Databases, spreadsheets, and enterprise platforms support stored-vs-derived values well, but they do not provide Precept's lifecycle-aware integrity contract. Validators support lifecycle-light definitions, but not structural prevention. State machines support lifecycle, but not entity data governance. Precept's category is the join.

3. **The product boundary stays explicit.** Derived values must remain declared, read-only, entity-local, and visibly recomputed. Stateless definitions must remain governed, not downgraded into optional validation passes.

4. **Non-goals are now clearer.** Hidden recomputation, workflow bypass, cross-entity magic, and orchestrator-style process logic all dilute the category instead of extending it.

## Evidence

Primary synthesis: `docs/research/language/expressiveness/entity-modeling-surface.md`
Supporting research: `docs/research/language/expressiveness/computed-fields.md`, `docs/research/language/expressiveness/data-only-precepts-research.md`

## Recommendation

Treat #17 and #22 as one **entity-modeling surface** lane in planning and review. The durable PM framing should be:

> Precept governs business entities whether their integrity depends on lifecycle state, derived values, both, or neither — but always through explicit, inspectable, one-file contracts.

---

# Steinbrenner — finish language cleanup

## Decision

The language-research tree now uses a hard split:

- `docs/research/` stores research, audits, references, and rationale only.
- GitHub issues are the canonical home for proposal bodies, status, and acceptance discussion.
- `docs/research/language/expressiveness/expression-feature-proposals.md` is retired and should not be recreated.

## Why

The repo had entered an ambiguous state where research markdown still looked canonical even after proposal issues #8-#18 existed. That ambiguity would slow review, create drift between markdown and GitHub, and make it unclear where proposal status lives.

## Required follow-through

1. Keep research docs evidence-oriented.
2. Link research files to the relevant GitHub issue instead of embedding proposal bundles.
3. Add new proposal framing in GitHub first, then back-link to research in `docs/research/language/`.
4. Treat `expression-language-audit.md` and the library comparisons as supporting evidence, not shadow proposal specs.

---

# Steinbrenner — index sweep for language research

## Decision

Lock the language-research README structure to a **domain-first navigation model**:

- `docs/research/language/README.md` is the master entry point and serves **both** as the domain index and the open-issue map.
- `docs/research/language/expressiveness/README.md` is organized around **domain packets first**, then comparative studies, then cross-cutting audits.
- `docs/research/language/references/README.md` is organized around **theory companions by domain**, including horizon domains with no active proposal.

## Why

The corpus has reached the point where proposal-by-proposal navigation would hide the actual structure of the work. The durable asset is the domain packet, not the current issue body. Future proposal authors need one obvious answer to "where does this idea live?" before they start drafting or re-drafting issues.

## Guardrails preserved

1. Research docs remain organized by domain, not by proposal body.
2. Proposal bodies stay untouched in GitHub issues.
3. Horizon domains stay visible even when no proposal exists yet.
4. Every open proposal should be discoverable from the indexes through its research grounding, not through copied proposal prose.

---

---
author: steinbrenner
date: 2026-04-05
status: recommended
domain: proposal-workflow
---

# Decision: Project-first status for the language proposal queue

## Decision

For Precept's language proposal queue, use the GitHub Project as the primary workflow-status system, not issue labels.

- Keep labels for durable classification: `proposal`, `language`, owner labels, and slice labels such as `dsl-expressiveness` / `dsl-compactness`.
- Add a dedicated single-select project field named `Proposal Status` on **Precept Language Improvements**.
- Use the built-in project `Status` field only for execution flow (`Todo`, `In Progress`, `Done`), not proposal-decision meaning.
- Because the current project is private, do **not** drop public status signals immediately. Keep the existing status labels as a temporary bridge until the board is public or another public status surface exists.

## Recommended field values

`Proposal Status` should carry the proposal lifecycle:

- `Needs decision`
- `In review`
- `Accepted`
- `Deferred`
- `Rejected`
- `Implemented`

## Why

Precept's current queue is double-tracking the same information in two weak forms:

- open proposal issues are labeled `needs-decision`
- those same items sit in project `Status = Todo`

That gives us duplication without better reporting. The project is not yet carrying the proposal states that actually matter.

Current GitHub guidance points the other way:

- Projects are designed to be the customizable planning surface, with custom fields, views, automation, and insights.
- GitHub explicitly recommends a single source of truth and shows single-select fields as the right place for workflow metadata.
- Labels are repository-scoped and better suited to classification than to mutually-exclusive workflow state.

GitHub's own public roadmap is the clearest precedent: it uses a project board for roadmap placement while labels carry durable metadata such as release phase, feature area, and SKU. That is a split by **dimension**, not a label-only workflow.

## Tradeoffs

### Discoverability

- **Labels win** in the repository issue list and search UI.
- **Projects win** once someone is working inside the queue.

Because Precept's language project is currently private, labels still provide better public discoverability today. That is the only strong argument for keeping status labels in the short term.

### Filtering and scaling

- A single-select project field scales cleanly to `In review`, `Deferred`, and `Rejected`.
- Labels become noisier as the state model grows and are easier to misapply in combinations that make no sense.

### Automation

- Projects support built-in workflows, custom-field automations, and insights.
- Labels can drive Actions, but they are a weaker fit for structured lifecycle movement.

### Multi-project membership

- Project fields are per-project, which is exactly what we want if one proposal appears in both a language queue and a release-planning board.
- Labels are global to the repo, so one label must pretend to mean the same thing everywhere.

### Auditability

The durable audit trail should be:

1. issue body
2. closing or decision comment
3. open/closed state
4. project field history / charts

Do not rely on label history alone as the record of why a proposal was accepted, deferred, or rejected.

## Recommendation for Precept

**Short term:** keep the current labels because the board is private and we should not hide queue state from repo-only viewers.

**Preferred end state:** once the project is public, or once we are comfortable making the project the internal source of truth, retire `needs-decision` / `decided` / `deferred` as routine status labels and move that meaning into `Proposal Status`.

That leaves labels for taxonomy and keeps workflow state where GitHub Projects is strongest.

## Follow-through if approved

1. Make **Precept Language Improvements** public, or explicitly accept that project-first status is internal-only.
2. Add the `Proposal Status` single-select field.
3. Create filtered views for `Needs decision`, `In review`, and terminal outcomes.
4. Keep a required decision comment when closing or deferring a proposal.
5. Remove proposal-status labels after migration, or keep only a minimal public fallback if the board stays private.

---

---
author: steinbrenner
date: 2026-04-05
status: recommended
domain: proposal-workflow
---

# Decision: Remove proposal status labels via project-first workflow

## Decision

Precept should retire proposal-status labels (`needs-decision`, `decided`, `deferred`) and move proposal state into **Precept Language Improvements** as project metadata.

Use this split:

- **Labels stay for taxonomy only:** `proposal`, domain (`language`, later `runtime` / `mcp` / `tooling`), owner labels (`squad:*`), slice labels (`dsl-expressiveness`, `dsl-compactness`), and release labels when implementation work is actually scheduled.
- **Project fields carry workflow state:** add a single-select field named `Proposal State`.
- **Issue open/closed state carries activeness:** open issues are active proposals; closed issues are decided, deferred, or rejected proposals with a decision comment.

## Current state observed

- The repo currently has three proposal-status labels: `needs-decision`, `decided`, `deferred`.
- All 11 proposal issues are already in **Precept Language Improvements**.
- The project is currently **private** and only has the built-in `Status` field (`Todo`, `In Progress`, `Done`).
- In practice, the queue is duplicating state today: open proposals are `needs-decision` **and** `Status = Todo`.

That duplication is the part to remove.

## Recommended project model

### 1. Add `Proposal State` (single select)

Recommended values:

- `Needs decision` — triaged, waiting for PM / architecture call
- `In review` — actively being discussed or revised
- `Accepted` — decision made; implementation should move to normal execution issues
- `Deferred` — parked intentionally, with a revisit trigger or date
- `Rejected` — decision made not to pursue

Do **not** add `Implemented` here. Implementation is a delivery workflow, not a proposal workflow.

### 2. Keep built-in `Status`, but demote it

Use built-in `Status` only as a coarse progress signal:

- `Todo` — `Needs decision`
- `In Progress` — `In review`
- `Done` — `Accepted`, `Deferred`, or `Rejected`

That preserves GitHub's built-in automation value without forcing board columns to pretend all "done" outcomes mean the same thing.

### 3. Add one date field for parked work

Add `Revisit On` (date).

Only deferred proposals need it. This gives PM a clean "what should come back up?" filter without inventing another label.

## Reviewer day-to-day workflow

### Opening a proposal

1. Create the GitHub issue with the normal proposal structure.
2. Apply only durable labels:
   - `proposal`
   - one domain label (`language`)
   - owner label (`squad:frank` today)
   - optional slice label (`dsl-expressiveness` or `dsl-compactness`)
3. Add it to **Precept Language Improvements**.
4. Set `Proposal State = Needs decision`.

### Active review

- PM or architecture moves the item to `In review` when it becomes an active conversation.
- Review happens in issue comments, not in project notes.
- If the proposal needs iteration, it stays open and stays in `In review`.

### Decision handling

For every terminal decision, require a short comment at the end of the issue:

- first line: `Decision: Accepted`, `Decision: Deferred`, or `Decision: Rejected`
- 1-3 sentence rationale
- link to any follow-up issue, decision record, or research note

Then:

- **Accepted** → set `Proposal State = Accepted`, set project `Status = Done`, close the issue, and open/link normal execution issue(s)
- **Deferred** → set `Proposal State = Deferred`, fill `Revisit On` if known, set project `Status = Done`, close the issue with a clear revisit rationale
- **Rejected** → set `Proposal State = Rejected`, set project `Status = Done`, close the issue with the rejection rationale

If a deferred proposal comes back, reopen the issue and move `Proposal State` back to `Needs decision` or `In review`.

## Views and filtering

### Project views

Create these saved views on **Precept Language Improvements**:

1. **Active proposals** — filter `Proposal State:Needs decision,In review`
2. **Review now** — filter `Proposal State:In review`
3. **Deferred queue** — filter `Proposal State:Deferred`, show `Revisit On`
4. **Decisions log** — filter `Proposal State:Accepted,Rejected,Deferred`

Use a board grouped by `Proposal State` for human scanning. Use a table view for sorting/filtering.

### Repo issue filtering after label removal

The repo issue list remains usable:

- active proposals: `is:issue is:open label:proposal`
- active language proposals: `is:issue is:open label:proposal label:language`
- expressiveness slice: add `label:dsl-expressiveness`
- compactness slice: add `label:dsl-compactness`
- resolved proposals: `is:issue is:closed label:proposal`

The repo list no longer answers "deferred vs rejected" by label alone; that distinction lives in the project and in the decision comment. That is an acceptable trade once the project is the canonical workflow surface.

## Why this still works with squad

- Squad agents can still discover proposal issues by `label:proposal` plus domain/slice labels.
- Owner labels remain intact, so team routing does not change.
- The project becomes the only place that answers workflow questions like "what needs review now?" or "what was deferred for revisit?"
- Closing comments become the durable human-readable audit trail, which is better than relying on historical label changes.

## Key tradeoff

The only serious downside is discoverability while the project remains private.

If the board stays private forever, removing status labels means repo-only viewers lose a public status signal. That is survivable for the squad, but it is worse for casual repository browsing.

## Migration path

### Phase 1 — prepare, do not remove labels yet

1. Add `Proposal State`
2. Add `Revisit On`
3. Create the four saved views
4. Backfill all existing proposal issues from labels into `Proposal State`
5. Update squad instructions/templates to say project field is the source of truth

### Phase 2 — dual run for one review cycle

For one review cycle:

- update both the project field and the old status label
- confirm reviewers are actually using project views
- confirm deferred/rejected decisions remain clear from closing comments

### Phase 3 — remove status labels

Once the project workflow is proven:

1. Stop applying `needs-decision`, `decided`, `deferred`
2. Remove those labels from existing proposal issues
3. Update repo docs/templates that still instruct people to use status labels

### Optional gate before Phase 3

If public repo discoverability matters, make **Precept Language Improvements** public before removing the labels. If not, accept project-first status as an internal squad workflow and proceed anyway.

## Recommendation

Adopt the phased migration, not an all-at-once cutover.

The project already contains the full proposal queue, so Precept is structurally ready for label-free proposal state. The missing piece is one explicit `Proposal State` field plus a short period where humans and squad agents prove they will actually use the project as the canonical review surface.

---

---
author: steinbrenner
date: 2026-04-05
status: applied
domain: proposal-workflow
---

# Decision: Language Proposal Workflow Cleanup

## Decision

Language proposals belong in the GitHub Project v2 board **Precept Language Improvements**.

Use this label stack for proposal-stage language work:

- `proposal`
- `language`
- exactly one of `needs-decision`, `decided`, or `deferred`
- the normal owner label (currently `squad:frank`)
- thematic slice labels such as `dsl-expressiveness` and `dsl-compactness`

## Applied cleanup

- Added proposal labels `proposal`, `language`, `needs-decision`, `decided`, and `deferred`
- Put issues `#14`-`#18` onto **Precept Language Improvements**, joining the earlier queue items
- Marked open language proposals as `needs-decision`
- Closed `#18` as a decided rejection, labeled it `decided`, and moved its project item to `Done`
- Tightened thematic tagging so ceremony-reduction proposals carry `dsl-compactness` where that is the real slice

## Operating rule

Use GitHub issues for proposal bodies and decision state. Use `docs/research/` for evidence only.

The project board supplies queue movement (`Todo`, `In Progress`, `Done`). The issue labels supply proposal state (`needs-decision`, `decided`, `deferred`) because the current project field set does not expose decision-specific columns.

---

---
author: steinbrenner
date: 2026-04-05
status: recommended
domain: issue-workflow
requires-sign-off: shane
---

# Decision: Standardize on one GitHub issue workflow for all work types

## Decision

Precept should stop treating proposals as a special workflow class and standardize on one issue lifecycle for proposals, bugs, features, chores, UX work, docs, and research:

`Inbox` -> `Ready` -> `In Progress` -> `In Review` -> `Done`

Use the project board as the main workflow surface. Use labels for durable taxonomy only:

- exactly one **issue type** label (`proposal`, `bug`, `feature`, `chore`, `ux`, `docs`, `research`)
- one **primary domain** label (`language`, `runtime`, `tooling`, `mcp`, `plugin`, `docs`, `ux`, `roadmap`, etc.)
- one **owner/routing** label (`squad:{member}`)
- optional **slice** labels (`dsl-expressiveness`, `dsl-compactness`, release themes)

Keep only two optional cross-cutting exception labels:

- `blocked` — open work that cannot move until an external dependency clears
- `deferred` — intentionally parked work; close the issue with a defer rationale and reopen when revived

Retire proposal-only status labels as routine workflow labels.

## Why this is the right model

The current proposal queue duplicates meaning:

- issue labels say `needs-decision`
- the project says `Todo`

That split does not scale to bugs, chores, docs, or research. A single lifecycle works better because every issue ultimately moves through the same questions:

1. Has it been triaged?
2. Is it ready to be worked?
3. Is someone actively doing it?
4. Is it waiting on review or sign-off?
5. Is it complete?

That is the stable workflow axis. Issue type and domain are separate axes and should stay labels.

## Shared status definitions

### Inbox

New issue. Not yet triaged. Missing at least one of: type, domain, owner, priority, or next action.

### Ready

Triaged, scoped enough to act on, unblocked, and has a clear owner plus next action.

Examples:
- proposal: decision question framed, research linked, owner known
- bug: repro and expected behavior written
- feature: acceptance outcome and dependency call made
- research: question, deliverable, and consumer identified

### In Progress

Someone is actively working it now. Only move here when there is current ownership and live execution, not merely intent.

### In Review

Execution is complete for this pass and the issue is waiting on an external verdict:
- PR review
- PM/architect decision
- UX review
- human sign-off

### Done

The issue reached its terminal outcome:
- code/docs merged and verified
- decision recorded
- research delivered and accepted
- issue closed as completed

## Triage and prioritization rules

At triage, do five things in one pass:

1. assign exactly one issue type label
2. assign one primary domain label
3. assign the `squad:{member}` owner label
4. set priority based on user value x urgency x dependency order x implementation cost
5. choose one path: `Ready`, `Deferred`, or close as duplicate/declined

Priority guidance:

- **P0** — release blocker, broken core path, or hard external deadline
- **P1** — high user value or key dependency for upcoming work
- **P2** — worthwhile but not on current critical path
- **P3** — nice-to-have, cleanup, or backlog exploration

Dependency order breaks ties. Foundational work ships before polish.

## Label guidance

### Issue type labels

Use exactly one. They answer: *what kind of work is this?*

- `proposal`
- `bug`
- `feature`
- `chore`
- `ux`
- `docs`
- `research`

If an item starts as research and ends by asking for a decision, keep the original issue typed as `research` and open a follow-on `proposal` or execution issue if needed. Do not overload one issue with multiple types.

### Domain labels

Use one primary domain label to answer: *where does this belong and who should look first?*

Examples:
- `language`
- `runtime`
- `tooling`
- `mcp`
- `plugin`
- `docs`
- `ux`
- `roadmap`

Add a secondary slice label only when it supports durable filtering across many issues. Do not stack domain labels casually.

## Why `blocked` and `deferred` should remain

These two labels carry cross-cutting semantics that ordinary status does not.

### `blocked`

Keep it.

It tells the team: this issue should be moving, but cannot. It needs:
- a blocker comment
- the owner of the unblock
- the trigger for resuming

### `deferred`

Keep it.

It tells the team: this issue is intentionally parked, not finished and not rejected. The clean rule is:

- close the issue with `deferred`
- comment with why it was cut and what would bring it back
- reopen the same issue if it returns to scope

That gives a searchable parking lot without abusing normal workflow status.

## Migration steps

1. **Freeze the taxonomy.** Lock the issue type list, domain label list, owner labels, and exception labels (`blocked`, `deferred`).
2. **Backfill the board.** Put all active issues on one project and map them to `Inbox`, `Ready`, `In Progress`, `In Review`, or `Done`.
3. **Retire proposal-only status labels.** Convert `needs-decision` items to `Ready` or `In Review`; convert `decided` items to `Done`; keep `deferred` only where the issue is truly parked.
4. **Update triage practice.** Require one-pass triage: type, domain, owner, priority, status, and next action before an issue leaves `Inbox`.
5. **Run one review cycle, then enforce.** Audit for drift after one cycle and then treat multiple type labels, missing owners, and unlabeled domains as process bugs.

## Failure modes to avoid

1. **Using labels as a second status system.** Once workflow lives on the board, do not recreate `ready`, `review`, or proposal-only state labels.
2. **Applying multiple issue types.** `proposal + research + feature` on one issue makes routing and reporting useless.
3. **Using `blocked` or `deferred` as a graveyard.** Every blocked/deferred issue needs a reason and a re-entry condition, or it is just hidden backlog.

## Recommendation

Adopt the single lifecycle and treat proposals as one issue type within it, not as a separate operating system.

That is the cleanest way to make the repo understandable day-to-day, scalable across work types, and less dependent on label folklore.

---

# Decision: Static Reasoning Expansion stays interval-first and horizon-scoped

**Filed by:** Steinbrenner (PM)
**Status:** Proposed — awaiting Shane + Frank review
**Category:** `language-research`

## Context

Static reasoning expansion is now documented as a horizon-domain research lane covering contradiction detection, deadlock detection, satisfiability analysis, and range propagation beyond current null narrowing.

The key scope question is whether this lane should open as a broad solver-backed proposal soon, or stay narrowed to a smaller proof fragment until the surrounding language surface settles.

## Decision

Keep static reasoning expansion as **horizon research**, not active proposal work, and frame the likely first implementation shape as:

- null-fact propagation plus
- interval reasoning for numbers plus
- finite-set reasoning for booleans / choice-like domains,

with **sound-but-incomplete** diagnostics and **solver-backed satisfiability explicitly deferred**.

## Rationale

1. **This matches Precept's trust model.** Error diagnostics should only appear when emptiness or impossibility is proven in a fragment authors can understand from the message.
2. **It builds on what Precept already has.** Today's null narrowing is the seed; interval/set facts are a natural extension.
3. **It avoids outrunning the language surface.** Type-system and field-constraint work should settle before Precept promises deeper value-domain reasoning.
4. **It keeps compile-path cost bounded.** A small abstract domain is much more plausible for always-on language-server and MCP compile use than a solver-first pipeline.

## Implication

Do not open proposal work for this lane until the fragment, diagnostic contract, and performance budget are explicit and the type/constraint lanes have stabilized enough to define what values the analyzer is actually reasoning about.

---

# Decision: Type System Domain Sequencing (2026-04-08)

**Filed by:** Steinbrenner (PM)
**Status:** Proposed
**Category:** `dsl-expressiveness`

## Context

Batch 1 research now treats choice, date, decimal, and integer as one shared type-system domain. The research packet is unified, but implementation ordering still matters because this lane has the widest parser/runtime/tooling blast radius in the language roadmap.

## Decision

Keep one shared research packet, but preserve a staged implementation order:

1. **choice first**
2. **date second**
3. **decimal third**
4. **integer fourth**

## Rationale

- **choice** closes the cleanest expressiveness gap: bounded vocabularies are currently typo-prone strings, and enterprise-platform precedent is unanimous.
- **date** is the next strongest fit because current samples simulate calendar logic with numeric day counters; a day-only date type improves readability without importing timezone complexity.
- **decimal** and **integer** are valid, but both are more sensitive to constraint and coercion design. They should land only once explicit mixed-type and assignment contracts are settled.
- Bundling the research avoids duplicate precedent work. Staging implementation limits blast radius.

## Guardrails

- Do not reopen datetime/timestamp, records/maps, dynamic typing, host-language leakage, or silent coercion as part of this lane.
- Treat `money` as `decimal + currency choice`, not as a standalone scalar in this wave.

---

# Type System Domain Survey — Research Findings

**Author:** Steinbrenner (PM)
**Date:** 2026-04-05
**Status:** Research complete — ready for team review
**Related:** Proposal #25 (type system expansion — choice and date types)

## Decision needed

Confirm that proposal #25's scope (choice + date) is correct based on the domain evidence, and decide sequencing relative to Wave 1 expression proposals (#8, #10).

## Key findings

1. **Choice (enum) is universal.** 41/100 modeled fields across 10 business domains are choice-typed. All three entity-definition platforms (ServiceNow, Salesforce, Dataverse) have it as a first-class type. The `string` workaround provides zero compile-time safety — guard typos like `"Approvd"` compile and run silently.

2. **Date is universal.** 30/100 modeled fields across 10 domains. 4/5 surveyed platforms support dedicated date types (ServiceNow, Salesforce, Dataverse, Camunda 7). Without a date type, Precept cannot express temporal business logic meaningfully.

3. **Integer and currency are deferrable.** No domain field is *blocked* by Precept's `number` type. The gap is precision metadata, not modeling capability. 4/5 platforms distinguish integer from float, but `number` is a tolerable workaround.

4. **Proposal #25 scope is exact.** The evidence validates choice + date as the two types that close real gaps. No scope expansion needed.

5. **Precept is an entity-definition system, not a workflow orchestrator.** This aligns it with ServiceNow/Salesforce/Dataverse (all have choice + date) rather than Camunda 8/Temporal (delegate typing to host language).

## Evidence

Full research document: `docs/research/language/expressiveness/type-system-domain-survey.md`

## Recommendation

Ship choice first (highest typo-safety impact), then date. Maintain the previously scoped sequencing position of Wave 2/2.5, after #10 (string .length) and #8 (named rules) land.

---

# Decision: Type System Expansion — PM Scoping (2026-04-05)

**Filed by:** Steinbrenner (PM)
**Status:** Proposed — awaiting Shane + Frank review
**Category:** `dsl-expressiveness`

## Context

Shane requested a new proposal for expanding the Precept type system beyond `string`, `number`, `boolean`, and collections (`set<T>`, `queue<T>`, `stack<T>`). This decision records the PM scoping section that Frank should incorporate into or append to the technical language design proposal.

## Key PM Positions

1. **Enum/value-set type is the highest-value addition.** It addresses a genuine expressiveness gap (authors cannot declare bounded value domains), has strong external precedent across all comparable systems, and fits Precept's philosophy of explicit, compile-time-verifiable constraints. Recommend as the primary type-system expansion candidate.

2. **Integer subtype has value but is not urgent.** The `number`-as-integer workaround plus `% 1 == 0` or invariant constraints is tolerable. Defer unless enum work reveals a natural implementation path.

3. **Date/duration is the most ambiguous.** Four samples use day-counter numbers. Whether Precept should own temporal semantics or treat time as a host-injected concern is an open philosophical question. Recommend parking this behind enum and revisiting after Wave 2 completion.

4. **Record/struct is a non-goal for v1.** The only sample evidence is trafficlight's string concatenation. This conflicts with Precept's flat-field design and adds structural complexity that breaks keyword-anchored flat statements.

5. **Field-level range constraints (proposal #13) partially overlaps with constrained-number use cases.** The type-system proposal should not duplicate #13's territory. If enum lands first, #13's remaining value shrinks to numeric ranges only.

6. **Sequencing: this proposal belongs in Wave 2 or Wave 2.5.** It depends on Wave 1 expression foundations (#10 string .length, #8 named rules) being stable. It unlocks cleaner field-level constraints (#13) and the `in [literal, ...]` membership feature (audit item L8).

## Downstream Impact

- **Blast radius:** parser (new type keywords), type checker (new `StaticValueKind` flags), evaluator (new value handling), grammar (new type tokens), language server completions (new type suggestions), MCP tools (new type serialization in DTOs), runtime API (new field value types).
- **Samples affected:** 14 of 21 samples would benefit from at least one new type (enum: 6+, integer: 8+, date: 6).

## Linked Artifacts

- PM scoping section: delivered inline in the task response
- Research evidence: `docs/research/language/expressiveness/expression-language-audit.md` (L7 constrained types, L8 set membership)
- Existing proposals: #13 field-level constraints (overlap zone)
- External precedent: Cedar (datetime, decimal, duration extensions), Zod/Valibot (enum, literal unions), xstate (TypeScript context typing), FluentValidation (built-in validators for ranges/enums), SQL DDL (INTEGER, DATE, ENUM, CHECK constraints)

---

---
author: steinbrenner
date: 2026-04-05
status: recommended
domain: issue-workflow
requires-sign-off: shane
---

# Decision: workflow migration surfaces for the shared issue flow

## Decision

Standardize repo and template operations on one project workflow:

`Backlog -> Ready -> In Progress -> In Review -> Done`

Policy rules:

- `blocked` and `deferred` are optional exception labels only, not routine workflow states
- do not use proposal-specific status labels
- proposals follow the same flow as bugs, features, docs, chores, research, and UX work
- issue `open` / `closed` answers whether work is still live; project status answers where open work sits in the flow

## Migration map

### 1. Core workflow policy docs

- `.squad/skills/unified-issue-workflow/SKILL.md`
  - Replace `Inbox` with `Backlog`
  - Make `blocked` / `deferred` explicitly exception labels, not workflow stages
  - Add the open-vs-project-status rule directly to the core pattern section
- `.squad/skills/issue-workflow-normalization/SKILL.md`
  - Replace the current compact model (`Inbox`, `Blocked`, optional `Done`) with `Backlog -> Ready -> In Progress -> In Review -> Done`
  - Remove any suggestion that `Blocked` is a board column
  - Keep `blocked` / `deferred` as exception overlays only

### 2. Proposal-specific operational guidance

- `.copilot/skills/architectural-proposals/SKILL.md`
- `.squad/templates/skills/architectural-proposals/SKILL.md`
  - Remove required status labels `needs-decision` / `decided`
  - Change project placement from `Backlog or In Review` shorthand to the shared flow
  - Say proposals open in the same board flow as other work, and close with a decision comment rather than a proposal-status label
  - Reserve `deferred` only for intentionally parked work
- `docs/research/language/README.md`
  - Rewrite the proposal tracking workflow section so proposals use board status, not proposal-status labels
  - Clarify that issue closure records the terminal decision; it is not the same thing as project status

### 3. Repo routing and lifecycle templates

- `.squad/routing.md`
- `.squad/templates/routing.md`
  - Replace "inbox / untriaged" wording with `Backlog`
  - Clarify that `squad:{member}` means owner/routing, not automatic `In Progress`
- `.squad/templates/issue-lifecycle.md`
  - Rewrite the GitHub / ADO / Planner normalization tables away from `untriaged`, `assigned`, `needsReview`, `readyToMerge`, `changesRequested`, and `ciFailure`
  - Remove obsolete label guidance like `squad:untriaged`, `go:needs-research`, and `next-up`
  - Add a clean definition of board status vs issue open/closed vs exception labels

### 4. Label automation

- `.github/workflows/sync-squad-labels.yml`
- `.squad/templates/workflows/sync-squad-labels.yml`
  - Stop syncing `go:yes`, `go:no`, and `go:needs-research`
  - Ensure `blocked` and `deferred` exist as exception labels
  - Keep synced labels focused on routing, taxonomy, priority, and durable slices
- `.squad/templates/workflows/squad-label-enforce.yml`
  - Remove `go:` exclusivity and the `go:yes` / `go:no` release automation
  - If this workflow remains, it should enforce only durable label namespaces and exception-label rules

### 5. Triage and work-monitor automation

- `.github/workflows/squad-triage.yml`
- `.squad/templates/workflows/squad-triage.yml`
  - Stop auto-applying `go:needs-research`
  - Triage should assign owner/routing and comment on next action, while board status reflects `Backlog` or `Ready`
- `.github/workflows/squad-issue-assign.yml`
  - Change assignment copy so owner assignment does not imply the issue has entered `In Progress`
- `.github/workflows/squad-heartbeat.yml`
- `.squad/templates/workflows/squad-heartbeat.yml`
- `.squad/templates/ralph-triage.js`
  - Replace `untriaged` language with `Backlog`
  - Reframe Ralph's monitoring around `Backlog`, `Ready`, `In Progress`, `In Review`, and `Done`
  - Treat `blocked` / `deferred` as overlays, not primary state buckets

### 6. Agent operating instructions

- `.github/agents/squad.agent.md`
- `.squad/templates/squad.agent.md`
  - Replace Ralph board categories like `Untriaged`, `Ready = approved PR`, and other legacy state terms with the new workflow
  - Clarify that review-ready code and decision-ready proposals both sit in `In Review`
  - Make open/closed semantics explicit so closed issues are terminal outcomes, not active board states

## Supersession note

This decision supersedes older `Inbox` wording in:

- `.squad/agents/steinbrenner/history.md`
- `.squad/decisions/inbox/steinbrenner-standard-issue-workflow.md`

Those files are useful history, but they should not be treated as the current operating model once the migration lands.

---

# Uncle Leo — Computed Fields (#17) Quality Gate Re-Review

**Date:** 2026-04-08
**Reviewer:** Uncle Leo (Code Reviewer)
**Requested by:** Shane
**Artifacts reviewed:**
- `temp/issue-body-rewrites/17.md` (proposal body)
- `docs/research/language/expressiveness/computed-fields.md` (research document)
- `docs/PreceptLanguageDesign.md` (language design principles)
- Prior session findings (George's feasibility review, Kramer's tooling review)

**Verdict: REQUIRED CHANGES**

The proposal and research are substantively aligned on core design — syntax choice (`->`), locked decision rationale, dead-end rejections, and philosophical grounding are all clean. But the research explicitly identifies 6 semantic contracts the proposal "must state directly" and warns that "leaving them implicit would shift product design into implementation guesswork." The proposal fails to make 3 of those 6 explicit, and its "Open Questions: None" claim directly contradicts the research's own conclusion section.

---

## Findings

### Finding 1 — Update Pipeline Recomputation Missing (BLOCKING)

**Research says:** Semantic Contract #2 requires recomputation timing be explicit for **Fire**, **Update**, and **Inspect** — called "the most important semantic contract."
**Proposal says:** "Recomputation timing: After field mutations, before invariant checks" and Locked Decision #8 says "After all `set` actions in mutation phase."
**Discrepancy:** The proposal addresses only the Fire pipeline. Update pipeline (direct field edits via `edit` declarations) is completely absent. If a computed field depends on an editable field and the user edits that field, the computed value would go stale before invariant checks without explicit recomputation in the Update path.
**What should change:** Add explicit statement: "Computed fields are recomputed in both Fire (after all mutations in the chosen path) and Update (after field edits are applied) pipelines, before any constraint evaluation." Add acceptance criterion: "Update path: computed fields recomputed before rules evaluation."

### Finding 2 — Inspect Pipeline Recomputation Missing (BLOCKING)

**Research says:** Contract #2 states "preview output should reflect post-recomputation values, or the preview becomes misleading."
**Proposal says:** Nothing — Inspect is never mentioned.
**Discrepancy:** Precept's product promise includes inspectability. If `precept_inspect` shows stale computed values in its preview output, it undermines the entire inspect-before-commit model.
**What should change:** Add to Semantic Rules: "Inspect previews reflect post-recomputation values." Add acceptance criterion: "Inspect output includes fresh computed values reflecting hypothetical mutations."

### Finding 3 — Nullable Input Handling Unspecified (BLOCKING)

**Research says:** Contract #3 requires the proposal to "choose and document whether expressions that reference nullable fields are rejected conservatively." Explicitly frames this as an open proposal decision.
**Proposal says:** "Cannot be `nullable`" (Locked Decision #3) — but this only forbids the `nullable` keyword on the computed field itself.
**Discrepancy:** The proposal is silent on what happens when a computed expression *references* a nullable field. Example: `field OptionalName as string nullable` / `field NameLen as number -> OptionalName.length` — is the expression conservatively rejected? The current DSL has no null-coalescing operator, so the type checker must reject, but the proposal doesn't say so.
**What should change:** Add explicit rule: "Type checker conservatively rejects computed expressions that could produce null (i.e., that reference nullable fields or unsafe collection accessors)." Add acceptance criterion: "Expression referencing nullable field produces compile error."

### Finding 4 — Empty Collection Accessor Safety Undefined (BLOCKING)

**Research says:** Contract #3 asks "what happens when `.peek`, `.min`, or `.max` touch an empty collection" and "whether those accessor cases are compile-time errors, runtime rejections, or default-bearing semantics."
**Proposal says:** "Collection accessors work in computed expressions" (acceptance criterion) — no safety specification.
**Discrepancy:** `.count` is always safe (returns 0), but `.peek`, `.min`, `.max` are undefined on empty collections. If they can return null, they violate the "computed fields always produce a value" guarantee. The proposal assumes safety without specifying it.
**What should change:** Specify which collection accessors are safe for computed fields. At minimum: "`.count` is always safe. Computed expressions using `.peek`/`.min`/`.max` are deferred until accessor null-safety semantics are specified (or conservatively rejected)."

### Finding 5 — "Open Questions: None" Contradicts Research (BLOCKING)

**Research says:** The "Proposal Implications" section explicitly identifies 4 areas the proposal must clarify: (1) Fire/Update/Inspect timing, (2) nullable inputs + empty accessors, (3) external-input and serialization contract, (4) cycle diagnostics + inspect output + update recomputation + tooling visibility.
**Proposal says:** "Open questions: None."
**Discrepancy:** Three of the four research-identified clarification areas are not resolved in the proposal (Findings 1–4 above). Claiming "None" is factually incorrect and sends a false "ready for implementation" signal.
**What should change:** Populate "Open questions" with the unresolved items from Findings 1–4 and 6. Alternatively, resolve them as locked decisions — but they cannot remain unacknowledged.

### Finding 6 — External Input Behavior Unspecified (ADVISORY)

**Research says:** Contract #5 asks: "may callers provide [computed fields] in `CreateInstance`, `Update`, hydration, or MCP payloads? If callers do provide them, is that ignored or rejected?" Cites Terraform's reject-don't-ignore precedent.
**Proposal says:** Covers `edit` and `set` restrictions but is silent on external API callers.
**Discrepancy:** The API surface matters. If `CreateInstance(data)` is called with a computed field value, the runtime needs a defined response. Silently ignoring it would hide caller mistakes; rejecting it enforces the read-only contract.
**What should change:** Add locked decision or semantic rule: "External API calls (CreateInstance, Update, hydration, MCP payloads) that provide values for computed fields produce [reject / ignore — decide]."

### Finding 7 — Dead End "Silent Default Fallbacks" Missing from Exclusions (ADVISORY)

**Research identifies 5 dead ends.** Proposal lists 4 exclusions. "Silent Default Fallbacks" — the research's warning against quietly substituting `0`/`""`/`false` for null or empty cases — is missing from the exclusions section.
**What should change:** Add exclusion: "Silent default fallback values for null or empty inputs (use explicit declarations or future null-coalescing operators instead)."

### Finding 8 — Dead End "Writable Computed Fields" Not in Exclusions (ADVISORY)

**Research dead end #3:** Writable computed fields defeat single-source-of-truth.
**Proposal coverage:** Covered in Locked Decision #5 (edit/set restrictions) but missing from "Explicit exclusions."
**What should change:** Add to exclusions for cross-reference clarity with the research document, or note that Locked Decision #5 covers this.

### Finding 9 — Exclusion "Recursive Fixed-Point Evaluation" Has No Research Backing (ADVISORY)

**Proposal exclusion:** "Recursive field dependencies requiring fixed-point evaluation."
**Research:** This term doesn't appear. The research covers dependency cycles (Contract #4, well-addressed in Locked Decision #7) but "fixed-point evaluation" is a different computational concept.
**What should change:** Either ground this exclusion in the research or merge it into the cycle detection locked decision. Currently it reads as an orphan exclusion.

### Finding 10 — Research Document Not Referenced (ADVISORY)

**Proposal's "Research and rationale links"** references `PreceptLanguageDesign.md`, sample files, and principle alternatives — but does NOT reference `docs/research/language/expressiveness/computed-fields.md`, the primary research document created specifically for this proposal.
**What should change:** Add link: `docs/research/language/expressiveness/computed-fields.md` — comprehensive precedent survey, semantic contracts, and dead-end analysis.

### Finding 11 — MCP Serialization Shape Undefined (ADVISORY)

**Research Contract #6:** "whether compile output shows the derivation expression, whether inspect/update output shows the current computed value."
**Proposal:** Implementation scope says "Serialize computed fields in precept_compile output" — no shape defined.
**What should change:** Specify minimum DTO expectations: does `PreceptField` gain `IsComputed` and `Expression` properties? Does inspect output include post-recomputation values?

### Finding 12 — Hover Specification Too Vague (ADVISORY)

**Research Contract #6:** "whether hover/completions surface the formula, the result, or both."
**Proposal:** "Hover shows derived expression."
**What should change:** Clarify: does hover show the formula text, the last computed value, the dependency list, or a combination?

### Finding 13 — Entry Action Timing Ambiguity (ADVISORY)

**Proposal Locked Decision #8:** "After all `set` actions in mutation phase."
**Research Contract #2:** "after all mutations in the chosen path, including exit actions, row actions, and entry actions."
**Discrepancy:** The proposal's phrasing ("mutation phase") could be read as only row mutations, excluding entry actions which also execute `set` statements. If entry actions mutate a dependency field, one recomputation pass after row mutations would produce stale values.
**What should change:** Clarify: recomputation occurs after ALL mutation sources in the Fire pipeline (exit + row + entry actions), not just after row actions.

---

## Scoring Summary

| # | Finding | Severity | Category |
|---|---------|----------|----------|
| 1 | Update pipeline recomputation missing | **Blocking** | Semantic Contract #2 gap |
| 2 | Inspect pipeline recomputation missing | **Blocking** | Semantic Contract #2 gap |
| 3 | Nullable input handling unspecified | **Blocking** | Semantic Contract #3 gap |
| 4 | Empty collection accessor safety undefined | **Blocking** | Semantic Contract #3 gap |
| 5 | "Open Questions: None" contradicts research | **Blocking** | Internal consistency |
| 6 | External input behavior unspecified | Advisory | Semantic Contract #5 gap |
| 7 | Silent default fallbacks missing from exclusions | Advisory | Dead-end coverage gap |
| 8 | Writable computed fields not in exclusions | Advisory | Dead-end coverage gap |
| 9 | Fixed-point exclusion has no research backing | Advisory | Orphan exclusion |
| 10 | Research document not referenced | Advisory | Documentation sync |
| 11 | MCP serialization shape undefined | Advisory | Semantic Contract #6 gap |
| 12 | Hover specification too vague | Advisory | Semantic Contract #6 gap |
| 13 | Entry action timing ambiguity | Advisory | Semantic Contract #2 precision |

**5 blocking findings, 8 advisory findings.**

All 5 blocking findings trace directly to semantic contracts the research document explicitly flagged as "must be stated directly." These are not new concerns — they are the research's own conclusions that the proposal hasn't absorbed yet.

---

## Recommendation

The proposal is close. The core design is sound, the syntax is well-grounded, and the locked decisions are solid. What's missing is the second half of the research's output — the semantic contracts and open questions section. A single editing pass addressing Findings 1–5 would resolve all blocking issues. The advisory findings can be addressed in the same pass or during implementation.