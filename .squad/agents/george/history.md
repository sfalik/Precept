## Core Context

- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.
- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.
- Shared-environment discipline still applies: validate surgically, stage exact paths only, and preserve durable regression anchors when changing semantics.

## Live Guidance

- Interval and proof work must use explicit authored bounds; `nonnegative` is proof-only and does not populate `DeclaredMin`.
- `quantity` remains a reserved DSL keyword in tests and samples; use names like `qty` instead.
- Guard narrowing only handles field-to-constant comparisons today; arg-ref and broader interpolated reasoning still need explicit implementation.
- `TypedInterpolatedTypedConstant` is still not an exhaustive semantic surface: static qualifier-bearing text and whole-value qualifier identity are missing until new slices capture them.
- Do not treat `PreceptValue` as decimal-only when planning quantity runtime work; quantity unit identity can ride the existing reference lane.

## Historical Summary

- 2026-05-12 and 2026-05-13 closed George's major proof/checker work: interval-proof slices 1-4 validated green, D93/D94 constructor guarantees landed, the Tokens/Types static-init crash was fixed, and the comma-list `StateTarget` program closed with re-review approval.
- Same-qualifier proof repairs and hover/proof-surface closeout remain part of George's durable baseline, but detailed batch chronology now lives in `.squad/decisions.md` and `history-archive.md`.

## Recent Updates

### 2026-05-14T17:10:32.283-04:00 — Exhaustive interpolated typed-constant audit widened the normalization track

- Audited `TypedInterpolatedTypedConstant` coverage against quantity-normalization Slices 19-21 and confirmed those slices fix the motivating bug but are **not** exhaustive.
- Confirmed five gap categories: bounded interpolated `set` false positives, qualifier-proof false positives, silent qualifier-mismatch acceptance in assignment/defaults, missing interpolated field-default proofing, and completely unwired event-arg defaults.
- Durable severity callout: three false-positive classes and one silent-wrong acceptance are already real; only fully dynamic qualifier text (`'{n} {u}'`-style forms) should remain conservative/unproved by design.
- Recommended the next planning set as slices 22-26: capture static interpolated qualifier metadata, route it through qualifier consumers, extend interval extraction beyond quantity, add field-default proof coverage, and decide event-arg default resolution.

### 2026-05-14T22:00:00Z — PRE0027 diagnosis: Test.precept revert recommended

- Frank investigated suspected PRE0027 (`DuplicateArgName`) errors. Result: **none exist anywhere** in the repository.
- The only error in `samples/Test.precept` is **PRE0078** (pre-existing), present before George's edit. George's change (`'6 [lb_av]'` → `'{test2} [lb_av]'`) changed proof shape but not error category.
- **Recommendation from Frank:** revert `samples/Test.precept` via `git checkout samples/Test.precept`. If interpolated-quantity test coverage is needed for normalization work, create a new sample file with satisfiable bounds.
- `test/Precept.Analyzers.Tests/AnalyzerTestHelper.cs` addition (`AnalyzeWithFilePathsAsync<TAnalyzer>()`) is clean, legitimate C# test infrastructure.

### 2026-05-14T22:00:00Z — Frank's conditions resolution + Slice 15b confirmed: event-arg bound normalization approved

- Frank resolved all six §5.5.6 conditions — the implementation gate for Slices 14–21 is cleared (pending Shane's sign-off).
- Key outcome for George: **Slice 15b** adds `NormalizedDeclaredMin/Max` to `TypedEventArg` (Option a). This is now a design-locked requirement, architecturally parallel to `TypedField`.
- Slices 22–26 have full §5.6 detail entries; George can reference those for implementation planning.



- Frank independently approved the normalization design with conditions and identified the same high-risk areas George's audit hit from the code side: `IntervalOf` scoping, normalized-field bound reads, normalized `StaticMagnitude`, and missing event-arg bound parity.
- Treat the combined George + Frank result as the current architectural baseline before any implementation slices are started.

### 2026-05-15T00:08:25Z — Slice P1 landed typed-constant hole presence-proof traversal

- `ProofEngine.WalkExpression` now traverses `TypedInterpolatedTypedConstant` slot expressions and can emit presence obligations for optional arg refs only in that hole context.
- Focused presence tests passed, and `samples/Test.precept` now reports PRE0116 on line 14 instead of compiling cleanly through the gap.
- Landed as commit `ae19510f`, aligned with Frank's architectural decision that this is a separate presence-proof repair rather than part of quantity-normalization Slices 14–21.

## Learnings

- Typed interpolated typed-constant holes were bypassing presence-proof generation entirely; the fix is to recurse `TypedInterpolatedTypedConstant.Slots` through `WalkExpression` so optional field reads inside holes emit PRE0116 unless a guard proves presence. Verified with new proof-engine tests and with `samples/Test.precept`, which now reports `UnprovedPresenceRequirement` on line 14.
- Quantity-normalization review: the compile-time core is implementable, but the design still has implementation traps Shane should gate on — duplicate Slice 22 numbering, runtime Slice 22 depending on nonexistent `TypeRuntimeMeta`/`TypeRuntime<T>` surfaces, display drift once computed intervals become normalized, and `TryGetStaticNumericValue` becoming unsound if dynamic-unit interpolated constants fall back to raw `StaticMagnitude`. Also: the actual arg semantic type is `TypedArg`, not `TypedEventArg`, so slice specs must target the real code surface.

### 2026-05-15T02:37:53Z — Function-call qualifier enforcement gap added to the counting-unit fix track

- Frank's comprehensive cross-counting-unit audit found the remaining critical checker hole is not another binary-op branch; it is function-call resolution.
- `TypeChecker.Expressions.Callables.cs` resolves `min`/`max`/`clamp`/`abs` overloads without ever enforcing `FunctionOverload.Match`, so `QualifierMatch.Same` metadata is currently dead for function calls.
- Implementation direction is locked in the design doc: add `ValidateFunctionQualifierCompatibility` immediately after `SelectOverload`, reuse PRE0137 for explicit cross-counting-unit mismatches, and leave `in` membership as a separate deferred follow-up.

### 2026-05-14T22:57:25.658-04:00 — §5.7 slice review found stale paths and the wrong membership surface

- Blocked the §5.7 execution plan as written: the repo does **not** have `src/Precept/Catalog/...`, `DiagnosticCatalog.cs`, `FunctionsCatalog.cs`, or `TypeChecker.TryGetStaticScalingFactor()` today. The real current surfaces are `src/Precept/Language/Ucum/UcumAtomCatalog.cs`, `src/Precept/Language/Diagnostics.cs`, and `src/Precept/Language/Functions.cs`; any affine helper still needs to be introduced.
- Confirmed Gap C's real seam is still `TypeChecker.Expressions.Callables.cs` `SelectOverload`, but the qualifier check must guard both `TypedFunctionCall` return paths there (direct winner and context-retry winner).
- Confirmed Gap D is **not** an `in` / `not in` path in the current DSL. Membership is `contains`, and the checker route is `ResolveBinaryOp` → `TryResolveCatalogBinaryWithoutOperation` → `CreateSyntheticBinaryOp`. `OperatorTypingTests.cs` is the current regression anchor for that surface.
- Verified PRE0137 is available: `DiagnosticCode.CountBoundViolation = 136` is the current high watermark, so 137 is the next free ordinal.
- Found missed regression surfaces for slices 30–33: `test/Precept.Tests/ProofEngineTests.cs` PartB Slice7/9 still assume old proof-only behavior, and `test/Precept.Tests/TypeChecker/OperatorTypingTests.cs` already covers `contains` typing.

### 2026-05-14T23:11:17.096-04:00 — §5.7 re-review approved after Frank’s corrections

- Re-reviewed the revised §5.7 slice plan and cleared the original blockers: stale catalog/diagnostic/function references were corrected to the real `src/Precept/Language/...` surfaces, and the membership slice now targets `contains` through `ResolveBinaryOp` → `TryResolveCatalogBinaryWithoutOperation` → `CreateSyntheticBinaryOp`.
- Spot-checks against source confirmed `ValidateQualifierCompatibility`, `ResolveFunctionCall`, `SelectOverload`, `CreatePendingAtom`, `StripFunctionWrapper`, and the current `TypedInterpolatedTypedConstant` semantic node; PRE0137 remains free because `DiagnosticCode.CountBoundViolation = 136` is still the high watermark.

### 2026-05-15T03:13:42Z — George approved the revised §5.7 slice plan

- George’s re-review cleared the earlier §5.7 blockers: the slice list now points at the real `src/Precept/Language/...` seams, covers both successful `SelectOverload` returns, and moves the membership work to Precept’s actual `contains` operator path.
- Scribe merged George’s approval note into `.squad/decisions/decisions.md`, cleared the inbox file, and recorded the approval as the current architectural baseline for slices 30–43.
