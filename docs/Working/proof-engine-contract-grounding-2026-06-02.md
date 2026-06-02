# Proof-Engine Guarantee Contract — Empirical Grounding — 2026-06-02

status: Grounding — 2026-06-02

sources-consulted:
- `docs/Working/proof-engine-guarantee-contract-plan-2026-06-02.md` (Phase 2 charter; the prove-or-reject contract)
- `docs/Working/bugs.md` → BUG-017
- `src/Precept/Pipeline/ProofEngine.cs` (Pass-2 strategy dispatch; obligation discharge loop)
- `src/Precept/Pipeline/ProofEngine.Intervals.cs` (`TryIntervalContainmentProof` / `…Narrowed`, `IntervalOf`)
- `src/Precept/Pipeline/ProofEngine.Analysis.cs` (`CollectComputedFieldBoundObligations`, `CollectDefaultObligations`, `CollectNumericDefaultObligations`)
- `src/Precept/Pipeline/ProofEngine.Lengths.cs` (`TryLengthContainmentProof`, `TryCountContainmentProof`)
- `src/Precept/Pipeline/ProofEngine.Strategies.cs` (Literal / DeclarationAttribute / GuardInPath provers)
- `src/Precept/Language/Actions.cs` (`GenerateIntervalContainmentObligations` — set-action obligation generator)
- `src/Precept/Language/FaultCode.cs`, `DiagnosticCode.cs` (`[StaticallyPreventable]` mappings)
- `src/Precept/Runtime/Precept.cs`, `Evaluator.cs`, `PreceptValue.cs`, `RestoreOutcome.cs` (ingress / Restore / FromJson)
- `test/Precept.Tests/ProofEngineStringCollectionBoundTests.cs` (V1 count-obligation absence, asserted)
- Throwaway full-pipeline probe via `Compiler.Compile(src).Diagnostics` (now deleted; evidence reproduced below)

The general discharge mechanic is sound: in `ProofEngine.cs` the Pass-2 loop runs all strategies and, if none discharge, falls through to `(ProofDisposition.Unresolved, null)` (ProofEngine.cs:796), and every Unresolved obligation emits its diagnostic (ProofEngine.Diagnostics.cs). **So the contract holds for any fault class that actually *creates* the obligation.** The drifts are all "obligation never created" / "obligation created but discharge is a `null` stub" cases — the operation compiles clean because nothing was ever obligated, not because anything was proven.

## Per-fault-class behavior

| Fault class | Unprovable-case behavior | Source cite | Probe evidence | Contract verdict |
|---|---|---|---|---|
| **DivisionByZero** (runtime divisor, no nonzero/positive/guard) | **REJECT** | Division stamps `NumericProofRequirement(!=,0)` (Functions/Operations meta); no strategy discharges an unconstrained field divisor → Unresolved → diagnostic | `field D as integer default 0` + `… <- N / D` → **DivisionByZero**. `field D as integer positive` → clean (Strategy 2 discharges). `N / 0` → **DivisionByZero**. | **holds** |
| **NumericOverflow — set-action result, unbounded RHS** | **REJECT** | Obligation generated whenever target field has bounds (Actions.cs:285–304, *not* gated on a bounded RHS interval); `TryIntervalContainmentProof…` returns `false` on `resultInterval.IsUnbounded` (Intervals.cs:351,376) → Unresolved → diagnostic | `field A as integer min 0` (no max) + `set C = A * 10` (C max 100) → **NumericOverflow** | **holds** |
| **NumericOverflow — set-action result, bounded RHS exceeds** | **REJECT** | same path; bounded interval `> DeclaredMax` → `false` (Intervals.cs:380) | `A min 0 max 20`, `set C = A*10`, C max 100 → **NumericOverflow** | **holds** |
| **NumericOverflow — computed field, bounded operand exceeds** | **REJECT** | `CollectComputedFieldBoundObligations` builds the obligation when `IntervalOf` is bounded (Analysis.cs:452–473); bounded-exceeds → `false` | `A min 0 max 20`, `C min 0 max 100 <- A*10` → **NumericOverflow** | **holds** |
| **NumericOverflow — computed field, UNbounded operand** | **SKIP** | `CollectComputedFieldBoundObligations`: `var interval = IntervalOf(field.ComputedExpression …); if (interval.IsUnbounded) continue;` (Analysis.cs:452–454) — obligation **never created** | `A min 0` (no max), `C min 0 max 100 <- A*10` → **no NumericOverflow** (only the unrelated ComputedFieldWithDefault from the probe's `default`) | **BREACH** (= BUG-017) |
| **Sqrt / negative radicand** | **REJECT** | `sqrt` overload stamps `NumericProofRequirement(>=,0)` (Functions.cs:200–212); unconstrained operand undischarged → Unresolved → `SqrtOfNegative` | `field X as number` + `<- sqrt(X)` → **SqrtOfNegative**. `field X as number nonnegative` → clean (Strategy 2). | **holds** |
| **Empty-collection mutation** (`dequeue`/`pop` on unguarded collection) | **REJECT** | accessor/action stamps `NumericProofRequirement(count>0)`; no guard → Unresolved → `UnguardedCollectionMutation` (FaultCode `CollectionEmptyOnMutation`) | `dequeue Q` unguarded → **UnguardedCollectionMutation**. `when Q.count > 0` → clean (Strategy 3 GuardInPath). | **holds** |
| **Empty-collection access** (`.first`/`.last`/`.peek` on unguarded collection) | **REJECT** | `TypeAccessor` stamps `count>0`; no guard → Unresolved → `UnguardedCollectionAccess` (FaultCode `CollectionEmptyOnAccess`) | `field R as integer <- L.first` (L `list of integer`, unguarded) → **UnguardedCollectionAccess** | **holds** |
| **Count containment** (`add`/grow past `maxcount`, or below `mincount`) | **SKIP / DEFER** | No generator emits a `CountContainmentProofRequirement` for `add`/`remove` (Actions.cs only generates Interval + Length); `TryCountContainmentProof(…) => null` is a hard stub (Lengths.cs:41–42). `CountBoundViolation` (Diag 136 / Fault 15) exists but is **never emitted from a mutation** — effectively dead. | `field C as set of integer maxcount 1` + `add C Add.V` → **no CountBoundViolation** (clean re: count). Confirmed by existing assertion `ProofEngineStringCollectionBoundTests.cs:257–266` ("no obligation generator for add/remove yet"). | **BREACH** (sibling of BUG-017) |
| **maxplaces / decimal precision** | **REJECT, but Type-stage** (compile-time; not a proof obligation) | Detected in the type checker (`MaxPlacesExceeded`, Diag 67); no proof-engine obligation, no `[StaticallyPreventable]` fault round-trip | `field R as decimal default 0.123 maxplaces 2` → **MaxPlacesExceeded** | **holds** (compile-time reject; *not* the suspected runtime-`round()` model — see note) |
| **OutOfRange — numeric default violates declared bound** | **REJECT** | `CollectNumericDefaultObligations` stamps one `NumericProofRequirement` per value-bounding modifier on the default (Analysis.cs:484–524); static default evaluated against bound → Unresolved → `OutOfRange` | `field N as integer min 10 max 20 default 5` → **OutOfRange**; `default 50` → **OutOfRange** | **holds** |
| **OutOfRange — string default violates minlength/maxlength** | **REJECT** | `CollectLengthDefaultObligation` → `TryLengthContainmentProof` on the literal (Lengths.cs:21–34) | `field N as string minlength 5 default "ab"` → **LengthBoundViolation** | **holds** |
| **Length containment — set-action, *non-literal* RHS** | **DEFER (acknowledged)** | Actions.cs:309–311 only generates the length obligation when the RHS `is TypedLiteral{String}`; a non-literal string assignment to a bounded field generates **no** length obligation by deliberate design ("avoids false positives for dynamically-provided values") | (not separately probed; behavior asserted by Actions.cs comment + `ProofEngineStringCollectionBoundTests.cs:206–213`) | **drift** — same shape as BUG-017 for the length family (a non-literal that can exceed `maxlength` compiles clean) |

## Ingress enforcement

**Resolved: the runtime ingress enforces neither value-level constraints nor type/presence today — the executable model is unbuilt.** This is broader than the `evaluator.md` (type/presence) vs `business-domain-types.md` (value-level) contradiction: *both* describe an intended runtime that does not yet exist in code.

- `Precept.Create(JsonElement?)` is the only partially-live ingress and is explicitly spike-level: it matches construction rows but **skips guarded rows** and performs **no bound/qualifier/presence validation** on args — `if (row.Guard is not null) continue; // guarded rows deferred to R4` (Precept.cs:74–89). Defaults-only construction "always succeeds by compile-time guarantee" (Precept.cs:68).
- `Create(Action<IArgBuilder>)`, `InspectCreate`, and **`Fire` / `Update` do not exist as live methods** — they `throw new NotImplementedException()` (Precept.cs:96,104,108; the Fire/Update arg-gate lives only as `IArgBuilder`/`Evaluator` doc comments full of "PHASE 3 ENFORCEMENT OBLIGATION" / "to be added" markers, Evaluator.cs:59–157).
- `PreceptValue.FromJson` / `FromClr<T>` (the value-coercion entry the `IArgBuilder` doc names `TypeRuntime<T>`) are `NotImplementedException` (PreceptValue.cs:18–22).

**Implication for the contract (Part 2 "governance enforced at runtime on external input"):** governance is currently a *design commitment with no executing code*. The compile-time half of the composition (operand *carries* a declared constraint) is real and proven; the runtime half that "makes the carried constraint true on the value" is not yet implemented. This is consistent with the project being pre-release with a stub runtime — it is a build gap, not a design contradiction. When the executable model lands, the ingress must enforce **declared value-level constraints** (bounds, qualifiers, presence), per `business-domain-types.md`; the `evaluator.md` "type/presence only" framing under-describes the obligation and should be tightened in Phase 3.

## Restore

**Resolved: `Restore` does not re-validate constraints today because it is unimplemented — `Precept.Restore` and `Evaluator.Restore` both `throw new NotImplementedException()`** (Precept.cs:121–122; Evaluator.cs:154–155). The *design intent* documented in-code is that Restore **re-validates**: "Validates against the current definition: recomputes computed fields, evaluates global rules and state ensures" (Precept.cs:111–119), returning `RestoreConstraintsFailed(Violations)` when persisted data fails the current definition (RestoreOutcome.cs:14–18). So the runtime-api.md "re-validates" intent is the correct target; the `evaluator.md` "FromJson bypasses constraint validation" claim is **not** contradicted by live behavior because no live behavior exists.

**Are `Restore` and `FromJson` the same path?** No. They are distinct:
- `Restore(string? state, JsonElement fields)` is the entity-reconstitution entry that *intends* to recompute + re-validate constraints (Precept.cs:121).
- `PreceptValue.FromJson(JsonElement)` is a per-value deserialization helper on the value type (PreceptValue.cs:18) with no constraint semantics of its own — it only materializes a `PreceptValue`. Restore would *call* value-deserialization but the constraint re-validation is Restore's own pipeline step, not `FromJson`'s. The "FromJson bypasses constraint validation" framing conflates the value-level deserializer (which legitimately has no constraint duty) with the entity-level Restore (which carries the re-validation duty).

**Implication for philosophy line 35 ("no code path that bypasses the contract"):** with the design intent (Restore re-validates; `RestoreConstraintsFailed` is the out-of-contract reject), line 35 **holds** — Restore is governed re-validation, the legitimate out-of-contract boundary, not a bypass. The contingent line-35 caveat in the Phase-4 plan is **not triggered**: no implemented path bypasses constraints (there is no implemented Restore path at all). The honest Phase-3 statement is "Restore re-validates declared constraints on restored state (design); it is the out-of-contract entry, not a bypass" — and this should be implemented to match, not caveated away. Recommend re-confirming once the executable model lands.

## Contract breaches found

Two true soundness breaches (sibling shapes — "result/mutation can violate a declared bound, compiles clean because no obligation is created/dischargeable") plus one acknowledged deferral of the same family:

1. **BUG-017 (known) — computed-field NumericOverflow skips on unbounded operand.**
   Repro: `field A as integer min 0 default 0` (no `max`) + `field C as integer min 0 max 100 default 0 <- A * 10` → compiles clean; `A * 10` is unbounded and provably exceeds C's `max 100`. Root cause: `CollectComputedFieldBoundObligations` does `if (interval.IsUnbounded) continue;` (Analysis.cs:453–454) — never creates the obligation. **Fix direction: unbounded result must emit (reject), asking the author to bound the operand**, not skip.

2. **BUG-NEW (count containment) — `maxcount`/`mincount` never enforced on collection mutation.**
   Repro: `field C as set of integer maxcount 1` + `from S on Add -> add C Add.V` → compiles clean; an `add` can grow `C` past `maxcount 1`. Root cause: no obligation generator stamps `CountContainmentProofRequirement` for `add`/`remove`, and `TryCountContainmentProof` is a hard `=> null` stub (Lengths.cs:41–42). `CountBoundViolation` (Diag 136 / `[StaticallyPreventable]` Fault 15) is therefore **dead code** — declared statically-preventable but never emitted. This is the count-family analogue of BUG-017: a declared bound silently not enforced. (Confirmed-known: asserted as a V1 gap in `ProofEngineStringCollectionBoundTests.cs:257–266`, but per the contract it is a breach, not an accepted V1 limitation — a `[StaticallyPreventable]` fault with no live emission path is exactly the prevention-vs-detection failure the engine exists to prevent.)

3. **BUG-NEW (length containment, non-literal computed/assigned) — `maxlength`/`minlength` skipped on non-literal RHS.**
   Repro shape: `field Name as string maxlength 10` + `set Name = First + Last` (or a computed `<- First + Last`) → compiles clean; the concatenation can exceed `maxlength 10`. Root cause: Actions.cs:309–311 only obligates *literal* string RHS; `TryLengthContainmentProof` returns `null` for non-literals (Lengths.cs:24). Same shape as BUG-017 for the length family. (Documented as a deferred soundness hole in `phase8-value-level-obligation-ownership-2026-06-01.md`; recorded here as a breach per the contract.) Closing it needs a length/string-interval abstract domain (a larger build than BUG-017's numeric flip).

All three share one fix principle: **a declared bound whose satisfaction the engine cannot statically establish must reject (emit), never skip.** The numeric set-action path already does this correctly (it creates the obligation regardless and lets unbounded → `false` → reject); the breaches are the paths that gate obligation *creation* on provability, which inverts the prevention guarantee.

## Summary

**The prove-or-reject contract holds for the discharge mechanic and for every fault class that creates its obligation:** DivisionByZero, Sqrt/negative-radicand, empty-collection access *and* mutation, OutOfRange (numeric + string-length defaults), and — notably — **NumericOverflow on the set-action path even when the RHS is unbounded** all REJECT when safety is unprovable. `maxplaces` rejects at compile time (Type stage), so it is sound — not the suspected runtime-`round()` model.

**It drifts in three places, all the same shape (obligation never created / discharge stubbed → clean compile when a declared bound can be violated):**
- **NumericOverflow on the *computed-field* path with an unbounded operand** (BUG-017) — the asymmetry is purely that the computed-field collector skips unbounded intervals while the set-action collector does not.
- **Count containment** (`maxcount`/`mincount` on mutation) — no generator + stubbed prover + dead `CountBoundViolation`.
- **Length containment on non-literal RHS** — literal-only obligation generation.

**Ingress:** the runtime ingress (Create spike-level; Fire/Update/FromJson `NotImplementedException`) enforces **no value-level constraints today** — governance is an unbuilt design commitment, not a contradiction. Target = enforce declared value-level constraints (per business-domain-types.md); tighten evaluator.md's "type/presence" framing in Phase 3.

**Restore:** `Restore` is `NotImplementedException`; its documented intent is to **re-validate** declared constraints against restored state (`RestoreConstraintsFailed`), distinct from the constraint-free `PreceptValue.FromJson` value deserializer. Philosophy line 35 **holds** under the intended design (Restore = governed out-of-contract re-validation, not a bypass); the contingent caveat is **not triggered**. Re-confirm when the executable model implements Restore.

**Tree left clean:** the throwaway probe (`test/Precept.Tests/ZzScratchContractProbe.cs`) was deleted; no `src/Precept` modifications; no commits.
