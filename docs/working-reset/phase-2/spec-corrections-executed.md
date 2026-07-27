# Phase 2 — the S rows executed against `docs/language/precept-language-spec.md`

Uncommitted working-tree edits, made 2026-07-27 against HEAD `971c51c3`. Every row below is one of the
`S*` rows in [`sweep-correction-list.md`](sweep-correction-list.md), executed in the direction that row
gives — revised where the "Reconciliation against the 2026-07-26 owner rulings" section at the end of
that file revises it. Line numbers in the *After* column are post-edit.

Nothing outside `docs/language/precept-language-spec.md` was touched. `docs/philosophy.md`,
`docs/runtime/` and `docs/compiler/` are other slices.

## Rows executed

| Row | Line before | Line after | Removed | Written in its place | Grep pattern now returning zero |
|---|---|---|---|---|---|
| S1 | 1969 | 1974 | "This post-mutation sweep is one of two enforcement points. … The sweep then re-checks every constraint against the completed working copy. Ingress governs *what enters*; the sweep governs *the result*. (Restored state is neither: it is trusted as valid at persistence time and is re-governed only by the next operation's sweep — §0.7.)" | Ingress validation stated alone: it upholds the declared contract of the slot a value enters "and nothing wider; there is no second pass over the finished working copy". Restored state is trusted and "no later operation re-governs it". **No wider ingress check was written** — see the note below. | `post-mutation sweep`, `the sweep governs` |
| S2 | 1967 | 1972 | "Constraints are evaluated against the working copy after all mutations complete. If every constraint passes, the working copy is promoted … If any constraint fails, the working copy is discarded" | Atomicity kept and made unconditional: promotion on completion, discard whole when the operation refuses part-way (`reject` row, ingress failure, access-mode failure). "Promotion is not conditional on a constraint check: every handler was proven at compile time to preserve every rule." The no-observable-partial-state clause survives verbatim in substance. | `If every constraint passes, the working copy is promoted` |
| S3 | 149 | 149 | §0.3 item 4: "Constraints are evaluated against the working copy. If all constraints pass, the working copy is promoted. If any constraint fails, the working copy is discarded." | The same replacement as S2, in the shorter form §0.3 uses. | `If all constraints pass, the working copy is promoted` |
| S4 | 162 | 162 | "evaluate a guard, execute assignments left-to-right, check rules and ensures" | "evaluate a guard, then execute assignments left-to-right" | `check rules and ensures` |
| S5 | 1354 | 1360 | "is checked against the complete working copy *after* all mutations, so declaration order is irrelevant and any field is in scope" | "constrains the complete configuration rather than any one point in a mutation sequence, so declaration order is irrelevant and any field is in scope", plus a sentence saying the constraint is a proof obligation the compiler discharges against every operation that could affect it, not a runtime check after mutations. Phrased against *the configuration the operation commits* rather than *each write*, to match walk item 9. | `checked against the complete working copy` |
| S6 | 1903 | 1909 | "Rules and ensures are evaluated exhaustively — every applicable constraint is checked, and all violations are reported." | Collect-all re-scoped to where validation actually runs — event arguments, construction inputs, and the field values in an update patch — plus an explicit "Rules and ensures are not re-evaluated after the mutations complete". | `Rules and ensures are evaluated exhaustively` |
| S7 | 1938 (and the §3A.2 taxonomy at 1917, 1926) | 1923, 1932, 1944, 1947 | Construction-outcome row "\`ConstraintsFailed\` \| Post-mutation constraint violations (collect-all semantics)"; outcome type 4 "**Constraint failure.** Mutations would violate a rule or ensure; rolled back"; the "Rejection vs constraint failure" bullet | Outcome type 4 becomes **Ingress validation failure**, refused before any mutation runs. The bullet becomes "Rejection vs ingress validation failure". `ConstraintsFailed` moves from the construction-outcome table into the structurally-impossible list with its reason. `InvalidArgs` gains the collect-all note the deleted row carried. | `Post-mutation constraint violations` |
| S8 | 1946 | 1951 | "successful restoration (data valid, constraints passed), constraint failure (persisted data violates current definition's rules/ensures) …  Restoring an entity in an invalid state is not allowed — the governance guarantee applies from the moment an entity is loaded" | Two outcomes only (success, invalid input). Restore is stated as not an ingress point, trusting data governed when it was written. The load-time verification named instead is the certificate check on the **definition** — the runtime refuses to govern under a definition whose certificate does not verify. Migration now runs "during restoration" rather than "before constraint evaluation". | `persisted data violates current definition` |
| S9 | 268 | 272 | "Because mutations execute on a working copy that is discarded if any constraint fails (§3A.4), an invalid configuration never persists" | "An invalid configuration never persists because every handler was proven at compile time to preserve every rule and ingress makes the premises those proofs rest on true of the values that arrive". A sentence was added saying ingress covers only the field's own declared contract "and nothing wider" — the S1 reconciliation's target. | `discarded if any constraint fails` |
| S10 | 272 | 276, 278 | "the next operation through the contract re-governs it (every constraint is checked against the resulting working copy, §3A.4), and the runtime fault traps backstop any out-of-contract value the evaluator would otherwise fault on. For contract data those traps are unreachable, and a fault that fires for contract data is a compiler defect" | Both clauses gone. The trusted-restore first half kept. New paragraph states the assumption the plan asked to be written into canon: "this rule already held before the operation" is *proven* for data that arrived through the contract and *assumed* for data that did not, and nothing inside the engine notices when a host steps outside. | `runtime fault traps backstop` |
| S11 | 266 | 266, 268, 270 | "cannot produce a runtime fault — no division by zero, no overflow, no empty-collection access, no result outside a declared bound. The compiler delivers it by discharging, at every fault-prone operation, an obligation that each operand *carries* a sufficient constraint …" | Three changes. (a) Declared-bound containment split out of the fault list and named as rule preservation under one constraint mechanism, "not a fault gate and should not be quoted as one". (b) The carried-constraint test replaced by proof by induction from the four premise classes, with base case and inductive step named. (c) A new paragraph states the overflow gap — see below. The "there is no deferral" clause survives verbatim. | `no result outside a declared bound`, `carries* a sufficient constraint` |
| S12 | 112 | 112 | "type mismatch, division by zero, overflow, empty collection access, constraint range impossibility"; "Runtime fault checks exist only as defensive redundancy, never as the primary enforcement mechanism." | Bound containment dropped from the fault-class list with a sentence saying why. "Runtime fault checks do not exist — the evaluator computes, and what gives confidence that it may is the proof certificate the runtime verifies once at load". Overflow exemption noted, pointing at §0.7. | `constraint range impossibility`, `Runtime fault checks exist only as defensive redundancy` |
| S13 | 110 | 110 | "Runtime fault traps exist only as defensive redundancy for paths the compiler has already proven unreachable." | "There are no runtime fault traps: the evaluator computes and does not re-test what the compiler proved. What makes that safe is the proof certificate the runtime verifies once, when it loads the definition". Overflow exemption noted, pointing at §0.7. | `Runtime fault traps exist only as defensive redundancy` |
| S14 | 258 | 258 | "(§0.7 fault-prevention: count is 'no result outside a declared bound', discharged by an author guard, never deferred to a runtime check)" | "(§0.7: a `mincount`/`maxcount` bound is rule shorthand, so keeping the collection inside it is rule preservation — discharged by an author guard, never deferred to a runtime check)". The "never deferred" clause survives verbatim. | `no result outside a declared bound` |
| S15 | 213 | 213 | "Only a provably-false fold rejects; an unknown/unfoldable default never rejects (per Proof philosophy #1–2)." | An unknown or unfoldable default now rejects too, on the three-way verdict Proof philosophy #2 and #5 already apply to bounded writes, because it leaves the base case of the induction open. The designed exception is stated: a default the construction event provably overwrites. | `never rejects` |

## How overflow was worded, and why

Walk item 15 defers the arithmetic overflow model past the first release with no compile-time guarantee,
and want:165 removes the runtime guards, so `:110`, `:112` and `:266` all listed a fault that is
currently prevented at neither end. Walk item 23 constrains the fix: the specification states the target
and the code is validated against it, not the reverse.

So the target list is unchanged in all three places — overflow still appears among the faults a clean
compile rules out — and each place carries an explicit statement that this is the one item not met
today. §0.7 carries the full statement (`:270`); Principles 10 and 11 carry a one-sentence note pointing
at it. Nothing was deleted from the goal and nothing was softened to match the current compiler.

## Sites corrected that no row enumerates

Each of these asserted the same model inside a section a row rewrote, so leaving it would have made the
specification contradict itself in the same passage.

- **§0.7 Composition (`:274`)** — restated the carried-constraint framing S11 replaces: the proof now
  rests on "a premise the author declared", not on the structural fact that a value carries a constraint.
- **§3A.4 Operation execution order, phases 8 and 9 (`:2010`)** — "Constraint evaluation" and "Commit or
  discard" were the sweep as a numbered pipeline phase, in the section S1 and S2 rewrote. They are now
  one phase, **Commit**, with the reason stated. This is the spec-side twin of row R2.
- **§3A.6 Inspection (`:2160`)** — listed constraint evaluation as a previewed phase and claimed to agree
  with §3A.4, which after the S1/S2 edits it did not. Ingress validation was added to the list and
  constraint evaluation removed. This is the spec-side twin of row R4.

## What was deliberately not touched

- **`docs/philosophy.md`** — owner-gated, another slice.
- **`docs/runtime/`, `docs/compiler/`** — another slice. S15's two siblings (`diagnostic-system.md:417`,
  `proof-engine.md:483`) still carry the never-reject posture and now disagree with `:213`.
- **The `docs/ArchitectureDesign.md` quotation at `:2012`** — it ends "constraint evaluation), and
  commits on success", but it is a verbatim quotation of a deleted document used as evidence for the
  ordering of phases 3, 4 and 6. Editing a quotation to agree with present canon would falsify the
  evidence.
