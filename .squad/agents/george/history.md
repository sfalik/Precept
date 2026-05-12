## Core Context



- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.

- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.

- Diagnostic additions should be semantically precise; do not overload unrelated codes when the compiler contract changes shape.

- Shared-environment build discipline still matters: targeted validation beats noisy full-solution churn when the workspace may be busy.



## Learnings
### 2026-05-11T22:47:57-04:00 — RC-3 compound-unit cancellation

- QuantityTimesQuantity already advertised dimensional cancellation in the operations catalog, but assignment qualifier validation still flattened binary trees to raw leaves. That emitted false PRE0069s on qty[D] * qty[A/D] because both operands were compared directly to the target field.
- The fix was to add an operation-level CompoundUnitCancellation result-qualifier policy and derive the numerator unit/dimension before recursing into child operands.
- Interpolated compound-unit qualifiers stay symbolic ({StockingUnit}/{PurchaseUnit}), so RC-3 cancellation has to derive {Unit.dimension} symbolically instead of pretending runtime qualifier values are available during type checking.



### 2026-05-11 — Slice 11B: Temporal Price Denominator



- Pre-existing spike branch failures (26 tests in `TypeCheckerAssemblyTests` + `TypeCheckerAssignmentQualifierTests`) produce `ExpectedToken` parser errors unrelated to type checker work. Always baseline before claiming new failures.

- `TypeMeta` is a positional record — adding a new optional parameter at the end is safe because all call sites use named parameters. The computed-property-shadowing-parameter pattern (`public T[] Foo { get; } = Foo ?? []`) is the established idiom here.

- `typeRef.ResolvedKind` on a `QualifiedTypeReference` delegates to `InnerType.ResolvedKind` — no need to unbox manually; the property dispatch covers it.

- Dimension→TemporalDimension fallback is intentionally broad: any field with a `TemporalDimension` declared qualifier satisfies a `Dimension`-axis chain requirement. Cross-domain mismatches (physical vs temporal) are caught by `ExtractComparableValue` string comparison, not axis gating.

- Adding `internal static` test entry points to private ProofEngine helpers (behind `InternalsVisibleTo`) is the established pattern and keeps tests exercising real code paths without overexposing the API.



## Live Guidance



- Mutual-exclusion metadata must be symmetric; PRECEPT0011c makes one-way declarations non-buildable.

- Interpolation remains feasible, but Slice 2 is the dominant cost center and binder plus multiple LS walkers must ship with it to avoid checker/tooling drift.

- Runtime/model changes should preserve current semantic surfaces unless the task explicitly widens them.



## Historical Summary
- Earlier Track 2, parser-gap, proof-engine, and diagnostic-split detail was archived into `history-archive.md` during the 15 KB summarization pass.

- `.squad/decisions.md` remains the canonical chronology; this file now keeps only current implementation guidance and the newest merged outcomes.
- 2026-05-11 implementation-detail updates for Slice 11B, Slice 1, Slices 6/10/11/12, A2B, and early inventory fallout were compacted into `history-archive.md` during the 2026-05-12T02:12:11Z summarization pass.

## Recent Updates

### 2026-05-11T23:43:07Z — Slice 12 handoff and sample fallout recorded

- Frank completed the unblock design for Slice 12: temporal price denominators stay on `of`, duration gains implied temporal-dimension qualifiers, and Strategy 5 needs Dimension→TemporalDimension comparison support.

- Cross-agent inventory analysis says `samples/inventory-item.precept` will need follow-up on binary-chain qualifier propagation, ensure-expression coverage, and bare `unitofmeasure` semantics.

### 2026-05-12T00:01:51Z — Temporal price denominator design locked for Slice 12

- Frank validated the additive path: temporal price denomination stays on the existing `of` qualifier (`price of 'time'`, `price of 'date'`), so no new `per` keyword or separate grammar branch is needed.

- `quantity of 'time'` remains invalid; temporal semantics still route through `duration` / `period`, and `duration` should surface implied temporal-dimension metadata through `TypeMeta.ImpliedQualifiers` rather than checker hardcoding.

- Slice 12 follow-through needs comparison/proof support for Dimension→TemporalDimension fallback and must keep chain validation gated to price fields that explicitly carry the new temporal `of` qualifier.

- Shane follow-ups remain open on `quantity in 's'` hinting, `money ÷ duration -> price` inference, and `period ×/÷ integer` operator support.

### 2026-05-11T21:26:23.861-04:00 — Slice A2B compound-unit interpolation landed

- The type-grammar table in `src/Precept/Pipeline/TypeChecker.Expressions.cs` is static-array driven: each target type maps through `GetFormsForType()` to `SegmentForm(TextMatch[] TextChecks, InterpolationSlotKind[] Slots)` entries, and `TryMatchForm()` walks the parser's `2N+1` alternating segment shape to assign slot identities by hole index.

- Slot identity lives in `src/Precept/Pipeline/SemanticIndex.cs` as `InterpolationSlotKind` (the plan still calls it `SlotIdentity`). Slice A2B added `NumeratorUnit` and `DenominatorUnit` there.

- Exact text-segment matching is delegate-based, not token-based: separator checks happen through `TextMatch` functions over `TextSegment.Text`, and the slash separator is the literal `MatchSlash(string text) => text == "/"` check. Literal spaces in mixed forms still use exact inline checks like `(string s) => s == " "`.

- A2B added quantity Q5 (`'{n} {A}/{B}'`) plus a dedicated `UnitOfMeasureForms` compound-unit pattern for `'{A}/{B}'`; quantity Q6 was intentionally not added because the existing quantity whole-value lane is `H[whole-value]`, not a unit-only compound form.

### 2026-05-11T22:05:37.512-04:00 — RC-2 compound-unit interpolation patterns completed

- `TypeChecker.Expressions.cs` already had `UnitOfMeasureForms` U2 (`'{A}/{B}'`) and `PriceForms` P5 (`'0 {Currency}/{Unit}'`), so the regression was isolated to `QuantityForms` missing the numeric-prefixed compound-unit variants.

- Added quantity forms for `'0 {A}/{B}'`, `'0 {A}/each'`, and `'0 each/{B}'`, plus a `MatchNumericSpaceUnitSlash()` matcher for the fixed-numerator/denominator-hole shape.

- Added regression tests for all three quantity forms and a price guardrail test for `'0 {Currency}/{Unit}'`; targeted typed-constant tests now pass, and the full test project still only shows the pre-existing 26 spike-branch failures.

- Surprise: A2B had already landed the plain `unitofmeasure` compound form and the price grammar needed by `AverageCost`/`ListPrice`; only the quantity table lagged behind.

### 2026-05-11T22:05:37.512-04:00 — RC-1 interpolated qualifier parsing completed

- `TryParseQualifiers()` in `src/Precept/Pipeline/Parser.cs` was still hard-gated to `TokenKind.TypedConstant`; qualifier sites never entered `ParseInterpolatedTypedConstant()`, so interpolated `in '...'` / `of '...'` values fell straight into PRE0009.

- I split `ParsedQualifier` into literal vs interpolated forms in `src/Precept/Pipeline/ParsedTypeReference.cs`, then taught `TryParseQualifiers()` to accept `TypedConstantStart` and preserve the parsed `InterpolatedTypedConstantExpression` instead of dropping the site on the floor.

- `ExtractQualifiers()` in `src/Precept/Pipeline/TypeChecker.cs` now resolves interpolated qualifier forms against the expected qualifier type (`currency`, `unitofmeasure`, `dimension`) before threading placeholder qualifier metadata downstream, so field and arg declarations keep their qualifier slots instead of silently losing them.

- Follow-on surprise: turning qualifier interpolation on surfaced two tightly coupled gaps already sitting next to Slice 2 — binary-op context propagation for interpolated typed constants and the one-hole `unitofmeasure` compound forms (`'{A}/each'`, `'each/{B}'`). I patched both in `TypeChecker.Expressions.cs` so the new qualifier-path tests stayed green and the suite returned to the known 26-failure spike baseline.

- MCP compile validation in this session stayed attached to stale/disconnected server state after the first check, so I verified the sample with the rebuilt public `Precept.Compiler` API as a fallback: `samples/inventory-item.precept` no longer reports PRE0009 on the qualifier declaration lines (71-104 window / actual declarations 80-109 and arg sites 166-207).

### 2026-05-12T02:12:11Z — RC-1 and RC-2 inventory blockers closed

- RC-1 is now durably closed: qualifier positions accept interpolated typed constants, ParsedQualifier preserves literal vs interpolated forms, and inventory-style field/event-arg qualifiers no longer die at PRE0009.

- RC-2 is now durably closed: QuantityForms covers Q6/Q7/Q8 (`0 {A}/{B}`, `0 {A}/each`, `0 each/{B}`), while the already-landed price and unit-of-measure compound forms remain the supporting guardrails.

- Validation state recorded from the execution batch: core build clean, typed-constant battery at 102/102, and the broader spike branch unchanged at the known 26 pre-existing failures.

### 2026-05-11T22:34:01Z — D1/D2 AlwaysRejecting and StateAlwaysRejects diagnostics (codes 125, 126)

- Adding a span to a positional record (TypedTransitionRow) requires inserting RowSpan before Syntax (so Syntax stays last, matching the established NameSpan, Syntax positional pattern on TypedState/TypedEvent/TypedField). Only one construction site existed (TypeChecker.NormalizeTransitionRow) so the migration was surgical.

- PRECEPT0024 is why GraphAnalyzer cannot call .Syntax on any Typed* record. The pattern is always: extract span in TypeChecker at construction time, carry it as a named property, use it in GraphAnalyzer via that property. The existing CollectEdgeSpans comment documents this explicitly.

- D1 suppresses D2 by returning the flagged event name set from EmitAlwaysRejecting. Clean output parameter rather than threading mutable shared state through Analyze.

- The wildcard-override logic in EmitStateAlwaysRejects mirrors BuildEdges exactly: build xplicitStateEvents from rows where FromState is not null && StatesByName.ContainsKey && EventsByName.ContainsKey, then for each (state, event) pair prefer explicit rows if any exist, else fall back to wildcard rows.

- TransitionRowOutcome (semantic enum in SemanticIndex.cs) and OutcomeKind (catalog enum in Outcomes.cs) are parallel enums with the same three values. TransitionRowOutcome.Reject is correct inside graph analysis — no new catalog entry needed.

- The 26 pre-existing spike-branch failures (TypeCheckerAssemblyTests + TypeCheckerAssignmentQualifierTests) are invariant across this work. 9 new GraphAnalyzerTests added, all green.

