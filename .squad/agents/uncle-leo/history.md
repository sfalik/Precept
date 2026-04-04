## 2026-04-04T20:28:43Z — Orchestration: Elaine Palette Mapping Polish

Elaine completed beautification and unification of palette mapping visual treatments in \rand\brand-spec.html\ §2.1 (Syntax Editor) and §2.2 (State Diagram). Created \.spm-*\ CSS component system (~70 lines) to match polished §1.4 color system design. All locked semantic colors, mappings, and tokens preserved. System is general-purpose and applicable to future surface sections (Inspector, Docs, CLI).

**Decisions merged to decisions.md:** 35 inbox items (palette structure, color roles, semantic reframes, surfaces, README reviews, corrections, final verdicts)

**Status:** Complete. Ready for integration.

# Project Context

- **Owner:** shane
- **Project:** Precept — domain integrity engine for .NET. DSL that makes invalid states structurally impossible.
- **Stack:** C# / .NET 10.0, TypeScript — full stack code review
- **My domain:** Code review across all components: `src/Precept/`, `tools/`, `test/`
- **Key checklists:** Grammar Sync Checklist and Intellisense Sync Checklist (in custom instructions) — must verify on DSL surface changes
- **MCP thin-wrapper rule:** Logic in tools that belongs in `src/Precept/` is a violation
- **Docs sync rule:** Code changes must keep `README.md` and `docs/` in sync
- **Created:** 2026-04-04

## Learnings

### Code Review Knowledge Refresh (2026-04-05)

**Codebase Structure:**
- `src/Precept/Dsl/` (11 core files): Parser → tokenizer → type checker → runtime engine. Clean separation of concerns.
- `tools/Precept.LanguageServer/` (9 handlers + analyzer): LSP server wired to parser/compiler for diagnostics, completions, hover, semantic tokens, preview.
- `tools/Precept.Mcp/Tools/` (7 files): Five MCP tools wrapping core compile/fire/inspect/update APIs as thin DTOs. No domain logic leakage into tools.
- All three components follow identical null-handling and error-reporting patterns.

**Code Style & Conventions:**
1. **Records over classes** for immutable data: PreceptDefinition, PreceptField, PreceptEvent, all DTOs are sealed records. Exceptions: PreceptEngine, PreceptInstance, internal working classes.
2. **Sealed types** aggressively used — no inheritance surprises. Static inner types for working data.
3. **Ordinal comparisons** throughout: `StringComparer.Ordinal` is the standard for all identifiers, state names, event names (culture-blind, case-sensitive).
4. **Pattern matching** heavily used: `switch` expressions for model navigation and outcome evaluation. Exhaustive matching is checked.
5. **Nullable reference types** enabled (`<Nullable>enable</Nullable>` in csproj). Consistent use of `?` for optional fields.
6. **No null-forgiveness** (`null!`) except one instance in PreceptAnalyzer.cs line 35 — `TryGetDocumentText(..., out text!)` — which is safe because TryGetValue populates the out var.
7. **Dictionary.TryGetValue pattern** the standard for all lookups — no KeyNotFoundException throws.
8. **Inline DTOs** preferred over separate files when single-use (e.g., BranchDto, StateDto inline in CompileTool.cs).

**Null Handling (Consistent Pattern):**
- Input validation uses `?? Array.Empty<T>()` for model collections that might be null (line 72: `_invariants = model.Invariants ?? Array.Empty<PreceptInvariant>()`).
- Optional output uses nullable types: `IReadOnlyDictionary<string, object?>?` for event arguments.
- Defensive checks: `if (value is null)` followed by early return or fail. No null-coalescing chains or silent null-skipping.
- Collection items checked on insertion: `if (item is not null) list.Add(item)` in hydration methods.
- Rare `?.` usage (safe navigation) only on optional properties where null is expected (e.g., `value?.ToString()`).

**Error Handling Pattern (Rock-Solid):**
1. **ConstraintViolationException** carries the LanguageConstraint for diagnostic code derivation.
2. **DiagnosticCatalog** is the central registry — every error has an ID, phase, rule, message template, severity.
3. **Constraint sync comments**: Lines in parser/compiler have `// SYNC:CONSTRAINT:C7` comments linking to the constraint ID.
4. **Three entry points for errors:**
   - `Parser.Parse()` → throws InvalidOperationException wrapped as ConstraintViolationException.
   - `Parser.ParseWithDiagnostics()` → returns (model, List<ParseDiagnostic>) for LS use.
   - `Compiler.Validate()` → returns TypeCheckResult with diagnostics list.
5. **MCP tools** catch diagnostics into DTOs (DiagnosticDto) with line, column, code, severity.

**Naming Consistency (Excellent):**
- State/event/field names: Always singular or plural as declared (no renaming).
- Method names: PascalCase, verb-first for mutations (Fire, Update, Inspect), noun-first for queries (CheckCompatibility, GetDiagnostics).
- Parameter names: Camel case, descriptive (eventName not evt, instanceData not data).
- Internal prefixes: `_` for private fields, `__collection__` for internal hydrated collection keys (escaping is explicit and consistent).

**Design Doc Alignment:**
- ✅ `RuntimeApiDesign.md` accurately reflects code: PreceptParser.Parse/ParseWithDiagnostics, PreceptCompiler.Compile, PreceptEngine.CreateInstance, .Fire, .Inspect, .Update all match spec.
- ✅ `McpServerDesign.md` tool specs match CompileTool, FireTool, InspectTool, UpdateTool, LanguageTool output structures.
- ✅ `PreceptLanguageDesign.md` DSL semantics align with parser combinator implementations.
- No drift detected; docs are canonical and code honors them.

**Quality Concerns (Top 3 Red Flags):**

1. **null-forgiveness in PreceptAnalyzer.cs:35** — `out text!` assumes TryGetValue always succeeds. Safe here (TryGetValue contract) but fragile pattern. Consider explicit `_ = _documents.TryGetValue(...) ? text : ""` or assert.

2. **Incomplete regex coverage in PreceptAnalyzer completions** — Many inline regexes (NewFieldDeclRegex, NewEventWithArgsRegex, SetAssignmentExpressionRegex) are hand-written and case-insensitive. If parser syntax drifts, these regexes won't catch errors. **Action:** Document that grammar changes require regex review here. No automated check exists.

3. **Hydrate/Dehydrate dual format** — Internal format uses `__collection__<fieldName>` prefix for CollectionValue objects; public API uses clean field names with List<object>. Conversion happens in HydrateInstanceData/DehydrateData/CloneCollections (three places). Mutation sites must use all three correctly or silent corruption occurs. **Action:** Add invariant checks or consolidate to single format.

**Documentation Sync Requirement Met:**
- Code matches all design docs. No aspirational claims found in README.
- API surface stable (three-step pipeline: Parse → Compile → Engine).

**Standards to Enforce on PRs:**
- Every error path must land a DiagnosticCatalog entry with SYNC:CONSTRAINT comment.
- New DSL syntax requires grammar + completions regex + semantic token updates (three-sync rule).
- Null-forgiveness banned except in immediate safe contexts (document rationale inline).
- All collections use TryGetValue; no KeyNotFoundException throws.
- Records immutable; only engine internals mutate (and only under clone-and-commit model).

---

### Precept DSL Knowledge (2026-04-04)

**Language Summary:**
Precept is a flat, keyword-anchored state-machine DSL where every statement starts with a recognizable keyword — no indentation, no braces, no punctuation. A precept file is a sequence of declarations (fields, states, events) followed by behavioral rules (invariants, asserts, transition rows). The model is deterministic: the same state + event + data always produces the same outcome. Compile-time validation is a first-class feature — the language rejects type errors, scope mistakes, unreachable rows, and structural dead-ends before runtime. The fire pipeline executes in a strict, documented order: event asserts → row selection (when guard) → exit actions → row mutations → entry actions → validation (with full rollback on failure).

---

#### Good DSL Patterns

1. **`invariant` for data truths, `assert` for movement truths.** Use `invariant` when a condition must always hold (e.g., `Balance >= 0`). Use `in/to/from assert` for state-scoped conditions and `on Event assert` for event-arg validation. These are distinct tools for distinct purposes — do not substitute one for the other.

2. **`in` vs `to` vs `from` assert chosen correctly.** `in State assert` = must hold while residing there (checked on entry AND no-transition). `to State assert` = entry gate only (must hold on arrival, may relax later). `from State assert` = exit gate (can't leave until condition met). Wrong choice signals a design misunderstanding.

3. **Event asserts are arg-only.** `on Submit assert Amount > 0` is correct. `on Submit assert Balance > 0` is wrong — field state belongs in `when` guards, not event asserts. Event asserts answer "is this event well-formed?"; guards answer "does this event apply right now?"

4. **`reject` is for conditional denial, not structural unavailability.** Use `reject` when some paths succeed and a specific input combination is explicitly refused. If an event can *never* succeed from a state, omit the rows and let the result be `Undefined`. The "defensive reject" anti-pattern (`from Draft on Approve -> reject "Cannot approve from Draft"`) is worse than no rows at all.

5. **First-match with unguarded catch-all fallback.** When an event can succeed in some cases and fail in others, write the guarded success rows first, then a final unguarded `reject` as the catch-all. This pattern is clean and readable.
   ```
   from Submitted on Cancel when Cancel.Reason == "fraud" -> set Balance = 0 -> transition Canceled
   from Submitted on Cancel -> reject "Only fraud cancellation is allowed"
   ```

6. **Dotted arg form in transition rows, bare form in event asserts.** `EventName.ArgName` is the only valid form in `when` guards and `set` RHS. Bare `ArgName` is only valid inside `on Event assert`. This distinction is enforced by the compiler and prevents field/arg name collisions.

7. **Collection operations matched to collection kind.** `add/remove` for sets. `enqueue/dequeue` for queues. `push/pop` for stacks. `clear` for any. Using `add` on a queue is a parse error (C24). The kind communicates the access pattern — don't use `set of string` when you want queue semantics.

8. **State entry/exit actions for cross-cutting side effects.** Use `to State -> set ...` and `from State -> set ...` for mutations that should fire on every entry/exit regardless of which event caused the transition. Don't duplicate these mutations across every individual transition row.

9. **`in State edit FieldList` for runtime-editable fields.** Declare which fields users can patch directly (without an event) per state. Only declare fields where direct editing makes domain sense — not everything should be freely editable in every state.

10. **No terminal state exclusion needed for `in any`.** `in any edit` or `from any` includes terminal states. If a terminal state should not allow editing or fire a specific event, simply have no transition rows for it.

11. **Recommended file order: fields/invariants → states/asserts/actions → events/event-asserts → transition rows.** This is not enforced by the parser but is the idiomatic convention the language server reinforces via IntelliSense ordering. Deviating silently degrades the editor experience.

12. **Multi-name shorthand for shared type/default fields and no-arg events.** `field MinAmount, MaxAmount as number default 0` and `event Submit, Review, Approve` both communicate lifecycle structure in a single line. Use when fields/events share identical shapes — don't force one-per-line when the language gives you this.

---

#### Bad DSL Patterns / Smells

1. **`reject`-only transition rows where `Undefined` is correct (C51/C52 anti-pattern).** `from Draft on Approve -> reject "Cannot approve from Draft"` is the canonical bad pattern. It adds noise, deceives readers into thinking the event is "handled," and creates a C51 warning. Correct behavior: omit the row.

2. **Field references inside event asserts.** `on Submit assert Balance > 0 because "..."` is a compile-time error (C14/C16). Even if a reviewer sees it in a non-compiling snippet, it's a design smell — the author doesn't understand the arg-only scope of event asserts.

3. **`to State assert` when `in State assert` is intended.** Using `to Open assert X != null` means the constraint only runs on *arrival* — it doesn't protect against in-place mutations that violate it. If the data must hold *throughout* the state, use `in`. The `to`+`in` subsumption rule makes duplicate `to`+`in` a compile error (C45).

4. **Bare arg names in transition row guards.** `from Open on Submit when Amount > 0` is wrong — `Amount` is ambiguous between field and event arg. Must be `Submit.Amount`. Compile error (C38/C42).

5. **Cross-event arg reference.** `from Open on Submit when Cancel.Reason == "x"` is a compile error — the row is for `Submit`, not `Cancel`. Even in non-compiling samples, this reveals confusion about event scope.

6. **Unreachable rows (C25).** An unguarded row followed by any row for the same (state, event) — the second row is dead. This is a compile error, not a warning. A reviewer should catch this even by eye.

7. **`from any` overuse.** Applying `from any on Event -> ...` when the event is only meaningful in specific states creates invisible no-ops for states where the event firing makes no sense. It also obscures the precept's actual behavior. Use `from any` only when the event genuinely applies universally (e.g., emergency override, data-entry events).

8. **Orphaned events (C49).** Declaring `event Approve` with no transition rows anywhere. This is a C49 warning — the event has no behavior. Either wire it up or remove it.

9. **Unreachable states (C48).** A state with no transition row targeting it. C48 warning. Either wire a transition to it or remove it.

10. **Duplicated mutations that belong in state entry/exit actions.** If every row that transitions into `Closed` sets `ClosedAt = CurrentDay`, that should be `to Closed -> set ClosedAt = CurrentDay`. Repeating it in each row is verbose and drift-prone.

11. **`invariant` used for state-specific constraints.** `invariant Assignee != null because "..."` when Assignee is only required in certain states. This creates a constraint that's checked everywhere — even in states where Assignee being null is valid. Use `in State assert` instead.

12. **Inconsistent naming style for states and events.** States should be PascalCase nouns or noun phrases (`UnderReview`, `FlashingRed`). Events should be PascalCase verb phrases (`SubmitApplication`, `RecordFeedback`). Snake_case, camelCase, or all-caps in identifiers is inconsistent with every sample in the corpus.

13. **Non-nullable fields without explicit defaults.** Compile error C17. Non-nullable fields must declare a `default`. If a reviewer sees `field Name as string` (no `nullable`, no `default`), it won't compile.

14. **`in any edit` when terminal states should be excluded.** If the precept has terminal states (states with no outgoing transitions), declaring `in any edit Field` allows field edits on a terminal instance. This may be intentional, but the reviewer should flag it for domain verification.

15. **Over-engineered single-state precepts.** A precept with one state and `no transition` everywhere is effectively just a data container with invariants. The language supports this, but it's usually a sign the author hasn't finished the state design.

16. **Guards that restate what invariants already prove.** If `invariant Balance >= 0` exists, then `when Balance >= 0` in a guard is redundant — the invariant already guarantees this. Redundant guards clutter the model.

17. **Collection used when scalar boolean suffices.** Using a `set of string` to track a single flag when a `boolean` field captures the same semantic is over-engineering. Collections are for variable-cardinality data.

---

#### Hero Sample Quality Criteria (DSL-Specific)

A snippet worthy of the hero gallery must satisfy all of the following:

1. **Compiles clean.** Zero errors. Zero warnings. Hints are acceptable if intentional (e.g., dead-end terminal states are expected to be hints). Run through `precept_compile` — any diagnostic is disqualifying until resolved.

2. **Meaningful state machine.** At least 3 states with realistic lifecycle progression. The initial state should be clearly the starting point (e.g., `Draft`, `Active`, `Available`). Terminal states should be obvious from context (e.g., `Hired`, `Cancelled`, `Lost`).

3. **Demonstrates a real domain concept.** The hero gallery is a teaching tool. The domain must be familiar enough to be understood immediately (library checkout, traffic light, insurance claim) — not a contrived toy example.

4. **Events are domain-meaningful.** Each event should represent a real action someone or something does (`Submit`, `Approve`, `AdvanceDay`). Events that exist only to update a field (effectively a setter dressed as an event) are a smell unless the "setter" has meaningful guards or side effects.

5. **`reject` used correctly.** At least one use of `reject` as a conditional denial, not defensive unavailability. The reject reason must be human-readable and specific.

6. **Constraints used appropriately.** At least one `invariant` for a data truth, and at least one `in/to/from assert` for movement truth. Both constraint forms being present demonstrates understanding of the distinction.

7. **No dead code.** All declared events are referenced in transition rows. All declared states are reachable. No unreachable rows.

8. **Naming is idiomatic.** PascalCase for everything. State names are noun/noun-phrase. Event names are verb-phrase. Field names are descriptive but concise. No abbreviations that require domain knowledge to decode.

9. **Compact and readable.** A hero snippet should ideally fit on one screen (≤60 lines). Complex domains may go longer, but anything past 80 lines starts to lose its explanatory value. The CrosswalkSignal (~34 lines) and SubscriptionCancellationRetention (~45 lines) are good target densities.

10. **Correct event arg scope usage.** Event asserts use bare arg names. Transition row guards and mutations use dotted `EventName.ArgName` form. This is a visible correctness signal — a reader checking the snippet will notice immediately if the form is wrong.

11. **Collections are present when cardinality demands it.** If the domain involves tracking a set of items (pending documents, interview panel, waitlist parties), a collection field is appropriate and demonstrates that language feature. A hero gallery snippet should cover at least one collection operation if the domain calls for it.

12. **State entry/exit actions used where appropriate.** If a mutation applies to every transition into or out of a state, use `to/from State -> set ...` rather than repeating in every row. The presence of this feature in a hero snippet teaches the pattern.

---

#### Advisory/Diagnostic Codes to Watch in DSL Reviews

| Code | Severity | What to check |
|------|----------|---------------|
| C10 | Error | Every transition row must end with exactly one outcome |
| C13 | Error | Exactly one `initial` state across entire precept |
| C14/C15/C16 | Error | Event asserts must reference only that event's declared args |
| C17/C18/C19 | Error | Non-nullable fields need a `default`; value must match type; cannot be `null` |
| C22/C23/C24 | Error | Collection mutations must target collection fields; verb must match kind |
| C25 | Error | Unreachable row after unguarded row for same (state, event) |
| C38 | Error | Unknown identifier — bare arg in transition row, field in event assert, undeclared identifier |
| C39/C40/C41 | Error | Type mismatch in expression |
| C42 | Error | Null-flow violation — nullable assigned to non-nullable without narrowing |
| C44/C45 | Error | Duplicate or subsumed assert (`in` + `to` on same state same expression) |
| C46 | Error | Non-boolean expression in `when`/`assert`/`invariant` position |
| C47 | Error | Identical guard on duplicate (state, event) rows |
| C48 | Warning | Unreachable state — state with no incoming transitions |
| C49 | Warning | Orphaned event — declared but never in any transition row |
| C50 | Hint | Dead-end state — non-terminal state where all exits reject or no-transition |
| C51 | Warning | Reject-only (state, event) pair — use `Undefined` (no rows) instead |
| C52 | Warning | Event can never succeed from any reachable state |
| C29/C30/C31 | Error | Defaults or initial state violate invariant/assert at compile time |

**Top smell list for fast review:**
- `reject` where no rows would be correct → C51 anti-pattern
- Field name in `on Event assert` → C14/C16
- Bare arg name in transition guard → C38
- `to State assert` where `in State assert` was intended → redundant protection
- Duplicated mutations across rows that belong in `to/from State -> ...`
- Inconsistent PascalCase / naming style
- States declared but never transitioned to → C48
- Events declared but never in any `from ... on` row → C49

---

### Full Sample Corpus Review (2026-04-05)

**Scope:** All 21 `.precept` files in `samples/`. Reviewed via `precept_compile` + manual pattern analysis.

**Aggregate results:**
- 0 compile errors across all 21 files (corpus compiles 100% clean)
- 0 compiler advisories
- 22 `[SMELL]` issues across 15 files
- 20 `[MINOR]` issues across 10 files
- 1 fully clean file: `loan-application.precept` (reference model quality)

**Most common smell (13/21 files): Write-only tracking fields.** Fields that are set by events or entry actions but never referenced in any `when`, `assert`, or `invariant`. Most are intentional informational fields, but several are wiring gaps (e.g., `FraudFlag` in insurance-claim — editable but makes no difference to machine behavior).

**Second most common smell: State/scope gaps.** States with no realistic exit (e.g., `OfferExtended` with no `DeclineOffer` in hiring-pipeline; `CheckedIn` with no service-not-needed path in vehicle-service-appointment). Also: asserts that are trivially always-true because the machine already guarantees the condition (`from InProgress assert AssignedTechnician != null` in maintenance-work-order; `!LostReported` guard in library-book-checkout).

**One behavioral bug found:** `subscription-cancellation-retention.precept` — `RetentionDiscount` is not reset when a subscriber re-enters the retention flow. On a second cancellation attempt, `MakeSaveOffer` always rejects because the discount is non-zero from the first cycle.

**Notable structural smell:** `utility-outage-report.precept` — state named `VerifiedState` (suffix "State") because the field `Verified` occupies the natural name. Field-name → state-name collision is a naming design smell. Fix: rename the field to `IsVerified` or `OutageVerified`.

**Patterns to watch in future reviews:**
1. **Dead entry actions** (`to State -> set X = false` when X can never be true at that point).
2. **Cancel conflation** (`refund-request.precept` routes both Cancel and Decline to `Declined`).
3. **Edit bypass of event-enforced rules** (`in Scheduled edit ScheduledMinute` skips 15-min boundary check).
4. **Pricing/data lost after event** (`event-registration.precept` loses `PricePerSeat` because there's no field to store it).
5. **`from any` on events that only matter in one state** — creates silent no-ops in non-target states.

---

### Internal DSL Verbosity Analysis (2026-04-05)

**Scope:** All 21 `.precept` files in `samples/`. Statement counting per hero rubric v2 definition.  
**Output:** `docs/research/dsl-expressiveness/internal-verbosity-analysis.md`, `.squad/decisions/inbox/uncle-leo-verbosity-analysis.md`

**Key findings:**

- **0 of 21 samples pass the 6–8 statement gate.** Range: 29–86. Median: ~47.
- Smallest sample (`crosswalk-signal`, 29) is 3.6× the gate maximum.
- The gate is structurally unreachable by editing any existing reference sample. It requires a purpose-built hero snippet.
- The gate and the DSL Coverage floor (must show `invariant`, `reject`, `when`, `transition`, multiple states) are in **structural tension**. Satisfying both simultaneously requires language compression features that don't currently exist.

**Top 3 verbosity smells identified:**

1. **Event argument ingestion** — `set field = Event.Arg` repeated N times per intake transition. Present in all 21 samples. Worst case: 7 SET statements for one logical "record this event" operation. An `absorb Event` shorthand would collapse N sets to 1.

2. **Guard-pair header duplication** — Every `from X on Y when condition` requires a matching `from X on Y -> reject` fallthrough header. Accounts for 20–35% of all rule headers in the corpus. An inline `else reject "…"` suffix would halve this cost.

3. **Non-negative numeric invariant boilerplate** — `invariant X >= 0 because "X cannot be negative"` repeated for every numeric field. Present in 19 of 21 samples. A field-level `min 0` annotation would replace 60–70% of all `invariant` statements.

**Best hero domain candidates (emotional hook + compactness):**
- `subscription-cancellation-retention` (36 statements) — highest emotional hook, authentic retention arc, strong `because` message potential
- `restaurant-waitlist` (33 statements) — universally relatable, strongest Precept Differentiation moment in corpus (queue-peek-then-dequeue in one transition row)

### Phase 1 Research Contribution (2026-04-04)

Conducted internal DSL verbosity analysis across all 21 sample files, producing research document `docs/research/dsl-expressiveness/internal-verbosity-analysis.md` and decision inbox item.

**Critical finding:** 0 of 21 samples pass the 6–8 statement gate. Range: 29–86. Median: ~47. Smallest sample (crosswalk-signal) is 3.6× the gate maximum. The gate is structurally unreachable by editing existing samples — it requires purpose-built hero snippet or language compression features.

**Top 3 verbosity smells:**
1. **Event Argument Ingestion** — `set field = Event.Arg` repeated N times per intake. All 21 samples affected. Worst case: 7 SET statements for one logical operation. Proposal: `absorb Event` shorthand.
2. **Guard-Pair Header Duplication** — Every guarded `from X on Y when C` requires matching fallthrough. Accounts for 20–35% of rule headers. Proposal: inline `else reject "…"` suffix.
3. **Non-Negative Numeric Invariant Boilerplate** — `invariant X >= 0` repeated per numeric field. Present in 19/21 samples. Proposal: field-level `min 0` annotation.

**Strategic finding:** The 6–8 gate and DSL Coverage floor are in structural tension. Satisfying both requires language features (named guards, ternary, string.length) identified in companion research.

