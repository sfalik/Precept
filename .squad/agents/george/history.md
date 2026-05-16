## Core Context

- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.
- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.
- Shared-environment discipline still applies: validate surgically, stage exact paths only, and preserve durable regression anchors when changing semantics.

## Live Guidance

- Interval and proof work must use explicit authored bounds; `nonnegative` is proof-only and does not populate `DeclaredMin`.
- `quantity` remains a reserved DSL keyword in tests and samples; use names like `qty` instead.
- Guard narrowing only handles field-to-constant comparisons today; arg-ref and broader interpolated reasoning still need explicit implementation.
- `TypedInterpolatedTypedConstant` still needs careful treatment whenever qualifiers or static text are split across dynamic slots.
- Do not treat `PreceptValue` as decimal-only when planning quantity runtime work; quantity unit identity can ride the existing reference lane.

## Historical Summary

- 2026-05-12 and 2026-05-13 closed George's major proof/checker work: interval-proof slices 1–4 validated green, D93/D94 constructor guarantees landed, the Tokens/Types static-init crash was fixed, and the comma-list `StateTarget` program closed with re-review approval.
- 2026-05-15 concentrated on qualifier enforcement, construction-guarantee follow-ups, and targeted completion/proof seams. Detailed slice-by-slice chronology now lives in `.squad/decisions.md` and `history-archive.md`; this file keeps only the guidance and latest durable outcomes.

## Recent Updates

### 2026-05-15T23:59:59Z — Deferred test-9 closeout reported green

- George closed the remaining quantity-bound red test after Frank's spec, recorded implementation commit `d68eb6bc`, and reported `5699/5699` passing across the validation run.
- Durable seam: typed-constant validator codes must survive emission when the qualifier text is concrete, and quantity assignment qualifier checks remain regression-sensitive whenever typed constants are special-cased.


### 2026-05-15T23:26:25Z — Pairwise qualifier repair lane reduced the branch from 10 failing tests to 1

- Commit `a03fcf4e` implemented Frank's three-root-cause fix spec for the remaining pre-existing failure cluster.
- Shipped fixes: added the missing `time` dimension alias, narrowed compound-operation pairwise qualifier suppression so real cancellation/elevation paths skip eager PRE0070/PRE0071 checks without hiding definite non-cancelling errors, limited same-qualifier proof deferral to non-field-expression contexts, and taught the proof path to treat zero-magnitude interpolated typed constants with dynamic unit slots as a usable numeric zero for positivity clearing.
- Validation moved `test/Precept.Tests` from 10 failures to 1 remaining intentional red test: `QuantityBound_CrossDimensionAssignment_IsBlockedByDimensionCheck`.

### 2026-05-15T23:14:11Z — Deferred qualifier follow-up lane is durably closed

- George's follow-up commits closed the PRE0141 review notes: `TypedFunctionCall.ResultQualifiers`, implied-qualifier parity in `ResolveDirectQualifierAxis`, shared `QualifierUnitHelpers`, and the slot-hole `Unknown` fallback now form the canonical shipped seam.
- Frank kept the architecture APPROVED after review; Soup Nazi's N3/N4 regression coverage locked the follow-up behavior into direct tests.

### 2026-05-15T18:51:51Z — Construction guarantee follow-up closed

- Stateful construction validation must account for every initial state covered by a wildcard `from any` row, not just explicit source-state rows.
- Ordered undefined-read checking now covers `SecondaryExpression`, cross-field reads, and omit→present materialization paths that recurse through computed-field dependencies.
- The resulting shipped diagnostics are `PRE0142`, `PRE0143`, and `PRE0144`, each guarding a distinct "read before the language established a value" seam.

## Learnings

- When a qualifier-preserving expression can know some axes but not all, store the partial resolved set instead of collapsing to all-or-nothing.
- Warning follow-ups should first verify whether the intended invariant is already present in shipped code and only missing regression locks.
- `IntervalContainmentProofRequirement.DeclaredMin/Max` remain normalized proof-math bounds even though their names read like authored values; display surfaces need parallel authored fields.
- For compound quantity operations, eager pairwise mismatch diagnostics must respect whether the operation's catalog semantics resolve qualifiers structurally or defer them to proof requirements.
- Typed-constant validator codes can safely promote to `DiagnosticCode` only when the declared qualifiers are concrete; interpolated qualifier text must keep the generic catalog fallback to avoid false `DimensionCategoryMismatch` emissions.
