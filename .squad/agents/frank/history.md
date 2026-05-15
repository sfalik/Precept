## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; pipeline, runtime, tooling, and docs should derive from durable metadata rather than enum-identity switches or parallel lists.
- Proof, qualifier, field-state, and normalization design work must stay grounded in shipped surfaces and verified implementation seams.

## Live Guidance

- Quantity normalization now has two durable lanes: compile-time normalization for declarations and literals, runtime normalization for ingress values (`TypeRuntimeMeta.ReadJson` / `TypeRuntime<Quantity>.FromClr`). Both call the same `TypedConstantNormalizer` logic.
- `TypedField` is the normalization handshake between analysis and execution: authored bounds stay available for display, normalized bounds feed proof/comparison surfaces, and the Builder remains the conversion boundary into `PreceptValue`.
- `IntervalOf` scaling is expression-form scoped, not universal: scale static typed constants and interpolated magnitude + static-unit forms; do **not** scale field refs, arg refs, or interpolated whole-value holes.
- `GetFieldBounds` and trusted-fact extraction must read normalized quantity data, and event-arg bound normalization still needs explicit parity design.
- Compiler/runtime duplication questions should be framed through the three-layer enforcement model: compile-time diagnostics, ingress validation, and defense-in-depth runtime faults.
- Comparison and equality operator checking must mirror assignment strictness for explicit counting units: shared `count` dimension is not enough when static unit codes differ.

## Durable Learnings

- Any claim that work happens "only at compile time" must be stress-tested against Fire/Update/Restore ingress paths.
- Storage conventions for business-domain values are architectural decisions; they shape evaluator invariants and cannot be deferred casually.
- ProofEngine intervals and evaluator opcodes share source data, not a common intermediate representation.
- `PreceptValue` bytes 8-23 are a three-way union lane (`decimal`, `long`, or reference region); quantity unit identity is not blocked by the 32-byte layout.
- Prefer catalog-mediated dispatch and metadata-backed mappings over per-code hardcoded routing in both compiler and runtime consumers.
- Dynamic-unit interpolated forms MUST produce `Unbounded` / not-proved — never fall back to raw `StaticMagnitude` against normalized bounds. This is the false-proof prevention invariant.
- When a design says "universal post-step," verify it actually means expression-type-dispatched post-step — the dispatch table is the contract, not the word "universal."
- Slices that depend on unimplemented runtime infrastructure (stubs) should be explicitly numbered out of the implementation sequence and marked not implementation-ready to prevent ordering confusion.
- Counting-unit comparisons need unit-code identity, not just dimension-family compatibility; only physically convertible UCUM units should pass same-dimension cross-unit checks.
- Catalog `QualifierMatch` declarations are only as strong as their enforcement wiring: the Functions catalog declared `QualifierMatch.Same` on min/max/clamp/abs overloads from inception, but `SelectOverload` never read it. Audit principle: every catalog constraint must trace to an enforcement point.
- Function call resolution and binary operator resolution are architecturally parallel but historically asymmetric: operators have `ValidateQualifierCompatibility`, functions have nothing. Any future qualifier-sensitive surface must wire enforcement at the same time the catalog entry is declared.

## Historical Summary

- 2026-05-12 through 2026-05-15 concentrated Frank's work around hover contract reviews, field-state guarantees, constructor diagnostics, interval-proof design, quantity normalization, diagnostic-enforcement architecture, and the counting-unit comparison gap.
- The durable enforcement baseline is: PRE0078 stays in ProofEngine Strategy 7, PRE0079 is the TypeChecker literal-bounds wire, PRE0019 is retired unless real presence-obligation generation is added, and PRE0094 is already emitted in the checker.
- Older batch-by-batch detail now lives in `.squad/decisions.md` and `history-archive.md`; this live file keeps only the guidance and latest outcomes other agents need immediately.

## Recent Updates

### 2026-05-15T02:57:25Z — George blocked §5.7 implementation slices

- George reviewed `docs/working/quantity-normalization-design.md` §5.7 and marked the current slice plan BLOCKED for revision.
- Slice 33 must target `contains` on the synthetic membership path (`ResolveBinaryOp` → `TryResolveCatalogBinaryWithoutOperation` → `CreateSyntheticBinaryOp`), not `in` / `not in`.
- Slice 32 must cover both successful returns inside `SelectOverload`, and slices 35–36 need "introduce" wording because the currently named normalizer/helper seams do not exist yet.
- Current canonical file seams called out by George: `src/Precept/Language/Ucum/UcumAtomCatalog.cs`, `src/Precept/Language/Diagnostics.cs`, and `src/Precept/Language/Functions.cs`.
- PRE0137 remains the correct next diagnostic ordinal; regression anchors should call out `test/Precept.Tests/ProofEngineTests.cs` and `test/Precept.Tests/TypeChecker/OperatorTypingTests.cs`.
- George also noted that `dotnet test test/Precept.Tests/Precept.Tests.csproj --no-restore` is already red by 7 baseline failures.
### 2026-05-15T02:32:44Z — Affine unit conversion design for temperature units

- Designed `docs/working/quantity-normalization-design.md` §6.8 to support affine temperature normalization for `Cel`, `[degF]`, and `[degRe]` with `base = (value + offset) × scale`.
- Root cause is in UCUM parsing: `StripFunctionWrapper` keeps multiplicative factors but erases the function-name offset encoding, so Celsius currently collapses to Kelvin semantics unless the offset is carried separately.
- Locked the implementation shape around `AffineOffset` metadata, affine proof/interval normalization, and a 24-test matrix; logarithmic units remain explicitly excluded.
- Scribe merged the decision into `.squad/decisions.md` and cleared `.squad/decisions/inbox/frank-affine-conversion-design.md`.

### 2026-05-16 — Comprehensive cross-counting-unit operation analysis: function gap found

- Exhaustive analysis of all 16 operation categories for cross-counting-unit interaction. Prior §6.7 was correct for binary operators but missed function calls entirely.
- **Critical finding (Gap C):** The Functions catalog declares `QualifierMatch.Same` on min/max/clamp/abs quantity+money overloads, but `SelectOverload` in `TypeChecker.Expressions.Callables.cs` never reads the `Match` property. Qualifier enforcement is completely absent for function calls. `max(qty_each, qty_box)` resolves without error.
- Locked: PRE0137 covers both operators and functions (single diagnostic code, adapted message). Fix is `ValidateFunctionQualifierCompatibility` after overload selection, reading existing catalog metadata.
- Lower-priority Gap D identified: `in` membership operator uses `CreateSyntheticBinaryOp` which skips `ValidateQualifierCompatibility`. Deferred to follow-up.
- Architectural principle locked: every `QualifierMatch` constraint declared in any catalog entry must have a corresponding enforcement point.
- Design doc updated: §6.7.9–6.7.11 added to `docs/working/quantity-normalization-design.md`.

### 2026-05-15T02:26:33Z — Cross-counting-unit comparison gap: full solution designed

- Traced the exact root cause in `ValidateQualifierCompatibility`: PRE0070/PRE0071 only apply to `OperatorFamily.Arithmetic`, and the same-dimension fallback treats all counting units as identical `count` quantities.
- Designed the two-tier fix: extend PRE0070/PRE0071 to comparison operators, then add PRE0137 `CrossCountingUnitOperation` when both operands are static count-dimension quantities with different unit codes.
- Locked the architectural boundary: SI units with the same dimension but different codes stay valid because UCUM normalization converts them; the stricter rule is only for business counting units with no universal factor.
- `docs/working/quantity-normalization-design.md` §6.7 now carries the implementation-ready plan, and Scribe merged the result into `.squad/decisions.md`.

### 2026-05-15T01:52:56Z — Counting-unit wording fix exposed a proof gap

- Corrected the counting-unit research note: `count` / `DimensionVector.None` is a shared dimension-family alias for business units such as `each` and `box`; it is not a conversion rule.
- Locked the language distinction between dimensional compatibility and value convertibility so future docs do not imply `1 box = 1 each`.
- Surfaced the deeper architectural issue: binary-op qualifier proof currently falls back through the shared `count` dimension, so explicit-unit comparisons can prove even when no conversion law exists.

### 2026-05-15T01:37:41Z — External normalization research merged

- Frank validated the quantity-normalization design against F#, Rust/uom, JSR-385, FHIR/UCUM, Modelica, and decimal interval-arithmetic practice; the architecture stayed sound, with only medium-priority documentation follow-ups.
- Business units (`each`, `box`, package-family count units) already normalize correctly by construction through factor-1 UCUM atoms and shared count-dimension metadata; no runtime storage change is needed.
- Scribe merged the supporting research records into `.squad/decisions.md`, deleted the inbox notes, and logged the batch for durable recovery.

### 2026-05-14T22:26 — Affine unit conversion design for temperature units
- Designed §6.8 extension to `docs/working/quantity-normalization-design.md` covering affine (scale + offset) conversions for temperature units (°C, °F, °Ré).
- **Key finding:** `UcumAtomCatalog.GetDefinitionExpression` strips UCUM `<function>` wrappers via `StripFunctionWrapper`, capturing scale but discarding the offset encoded in function names (`Cel`, `degF`, `degRe`). Celsius currently normalizes as identity (scale=1, no offset) — indistinguishable from Kelvin.
- **Approach:** Catalog extension — `UcumAtom` gains `decimal? AffineOffset`, `UcumParsedUnit` propagates for single-atom units only. Conversion: `base = (value + offset) × scale`. Linear units have `offset = null` → no regression.
- **Logarithmic units (dB, pH) excluded:** interval arithmetic incompatibility, domain mismatch, reference-level ambiguity.
- **Orthogonal to frank-12:** PRE0137 targets counting units (`DimensionVector.None`); temperature has `DimensionVector.Temperature`.
- Decision record: `.squad/decisions/inbox/frank-affine-conversion-design.md`.

## Learnings

- 2026-05-15T07:59:53.548-04:00 — Corrected `docs/language/business-domain-types.md` to state that business counting units share `DimensionVector.None` / the count dimension while still requiring explicit business conversion factors; static cross-unit count operations belong to PRE0137, not PRE0071. Updated `docs/working/quantity-normalization-design.md` progress tables to reflect shipped wave-1 work (Slices 14, 22, 30, 32, 34, 38–43 done; Slice 31 partial with binary operators still pending wave 2). Follow-up audit found no further PRE0137 wording contradictions beyond the corrected `business-domain-types.md` claim.
- 2026-05-14T23:36:31.558-04:00 — Documentation Slices 38–42 are now carried in the design/spec surfaces: temperature is explicitly in scope via affine `(scale, offset)` normalization; the no-epsilon guarantee is now stated as an exact `UcumExactFactor` + `decimal` contract; and business counting units are documented as shared `DimensionVector.None` / factor-one representations that still require PRE0137 unit-code enforcement.
- 2026-05-14T22:48:46.544-04:00 — Added formal implementation slices 30–43 to `docs/working/quantity-normalization-design.md`, covering the four qualifier gaps, the four-slice affine lane, five pre-implementation documentation slices, and the standalone `TypedInterpolatedTypedConstant` → `InterpolatedTypedConstant` rename.
- 2026-05-14T23:06:08.162-04:00 — George's §5.7 blockers required hard correction of the actual code seams: the diagnostic surfaces are `src/Precept/Language/DiagnosticCode.cs` and `src/Precept/Language/Diagnostics.cs`, the functions catalog is `src/Precept/Language/Functions.cs`, and the membership seam is `src/Precept/Pipeline/TypeChecker.Expressions.cs` via `ResolveBinaryOp` → `TryResolveCatalogBinaryWithoutOperation` → `CreateSyntheticBinaryOp` for `contains` (not `in` / `not in`). There is no existing `TypeChecker.TryGetStaticScalingFactor()` helper in the current codebase; affine helper wording must use introduce/new-helper language instead.
- 2026-05-14T23:17:29.653-04:00 — As of 2026-05-14, George's full codebase audit confirmed ALL slices 14–27 are NOT_STARTED. Not one line of normalization code has been written. `docs/working/quantity-normalization-design.md` now carries a master progress tracker at §5.0 covering all slices 14–43, plus Status columns added to the §5.1, §5.3, §5.6, and §5.7 summary tables.

### 2026-05-15T03:13:42Z — Frank’s §5.7 revisions cleared the review gate

- Frank’s revised §5.7 plan is now the durable baseline: stale catalog/diagnostic/function references were corrected to the real `src/Precept/Language/...` surfaces, Slice 32 names both successful `SelectOverload` return paths, and Slice 33 now targets `contains` through the synthetic membership path.
- George’s re-review approved the revised slice list with no remaining stale path or method references, and PRE0137 remains the next free diagnostic ordinal after `CountBoundViolation = 136`.

### 2026-05-15T03:17:29Z — Scribe recorded doc-tracker update

- Scribe merged Frank's doc-tracker note into `.squad/decisions/decisions.md` and cleared `.squad/decisions/inbox/frank-doc-tracker-update.md`.
- Durable baseline: `docs/Working/quantity-normalization-design.md` now carries §5.0 plus Status columns in §5.1/§5.3 and summary/status tables for §5.6/§5.7, all grounded in George's NOT_STARTED audit for slices 14–27.
- Scribe wrote the orchestration/session logs for the shared slice-audit + doc-tracker batch so the design tracker and codebase audit stay linked.

### 2026-05-15T03:43:11Z — Documentation annotation wave closed with one counting-unit follow-up

- Slices 38–42 doc annotations are committed and remain the live spec baseline for the normalization track.
- One durable follow-up stays open: `docs/language/business-domain-types.md:373` still says business counting units are opaque with no shared dimension, which now contradicts the shared `DimensionVector.None` + factor-one representation model.
- Keep PRE0137 as the enforcing rule for explicit unit-code identity inside that count family; the remaining work is wording correction, not architectural reconsideration.
