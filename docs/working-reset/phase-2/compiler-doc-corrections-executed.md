# Phase 2 — the corrections executed against `docs/compiler/`

Uncommitted working-tree edits, made 2026-07-27 against HEAD `c3a9be21` (the commit that landed the
runtime and evaluator corrections). Rows come from
[`contradiction-register.md`](contradiction-register.md), executed in the direction each row gives, and
re-aimed at the corrected specification where §0.6 and §0.7 now say something more precise than the row
anticipated. Line numbers in the *After* column are post-edit.

Files touched: `docs/compiler/proof-engine.md`, `docs/compiler/graph-analyzer.md`,
`docs/compiler/diagnostic-system.md`, `docs/compiler/soundness-and-coverage.md`,
`docs/compiler/README.md` (one phrase). Nothing else. `docs/compiler-and-runtime-design.md`,
`docs/runtime/`, `docs/language/`, `docs/philosophy.md`, `docs/Working/bugs.md` and
`docs/working-reset/phase-5/` were left alone, as instructed.

**Register line numbers had drifted.** `c3a9be21` moved `proof-engine.md` sites below `:483` by about
three lines. The register's `:2687`, `:2580` and `:2537` resolve at `:2690`, `:2583` and `:2540` at this
HEAD. Every site below was located by its text, not by its line number.

## Rows executed

| Row | Site before | Site after | Removed | Written in its place | Grep pattern now returning zero |
|---|---|---|---|---|---|
| N51 | `proof-engine.md` 2690 (§ 14, heading "No Inductive Proof") + TOC 83 | 2692–2698, TOC 83 | "### No Inductive Proof — The proof engine does not handle loop invariants, recursive definitions, or **inductive properties**. Precept does not have loops or recursion — the DSL is finite and event-driven." | Heading becomes **No Loop Invariants, No Recursive Definitions**, keeping the two true exclusions and the reason for them. Then a paragraph stating that proof over the reachable configurations *is* an induction and is the model this engine serves — base case, inductive step, the four premise classes, cited to spec §0.7 — and that the absence of loops and recursion is what makes the induction finite rather than what rules it out. Then a paragraph naming two gaps against that target so the model is not read as a description of the build: the pre-state premise is available only where a declared rule contributes an unconditional fact, and the write-site enumeration the induction quantifies over is asserted by spec §3A.4 rather than kept closed by the build. | `No Inductive Proof`, `inductive properties` |
| N56 | `graph-analyzer.md` 790 | 792 | "Determining whether a guard can be satisfied requires the proof engine's SMT integration, not graph-level analysis." | The boundary claim survives (it is not graph-level analysis); the mechanism claim is replaced. Guard satisfiability is decided by the proof engine's satisfiability scan — `UnsatisfiableGuard`, `TautologicalGuard`, spec §0.6 items 9 and 10 — from the same bounded strategy set as every other obligation. Adds that there is no solver to wait for (opaque proof artifacts are excluded by spec §0.8 item 4) and that the diagnostic fires only where the guard is provably unsatisfiable (Proof philosophy #2). | `SMT integration` |
| C3 | `proof-engine.md` 1733 | 1735 | "(the field already satisfies its band entering the chain — **§3A.4 swept prior state**)" | The same seeding fact, re-grounded on the induction: a `mincount`/`maxcount` bound is rule shorthand, and every rule holding in the pre-state is a premise the induction supplies (spec §0.7); "nothing re-checked the band at the end of the previous operation". The proof mechanics are untouched. | `§3A.4 swept prior state` |
| C4 | `proof-engine.md` 1731 | 1733 | "the runtime count-fault trap is a defense-in-depth backstop, unreachable for contract data" | "nothing catches an over-count later: the runtime evaluates no constraint against the finished working copy and carries no fault trap at evaluation time (spec §0.7 *The boundary of the guarantee*). Either this obligation discharges here or the definition is refused." The "no deferral" clause survives verbatim. | `the runtime count-fault trap` |
| C7 | `graph-analyzer.md` 457 | 457–459 | "**Severity:** Warning, not error. Dead-end states may be intentional … The author can suppress the warning by adding the `terminal` modifier." | Severity Error, with the reason. The `terminal` modifier is kept as the author's *declaration* that an endpoint is intended — a state carrying it is not a dead end and never reaches the check — explicitly not as warning suppression. A second paragraph records that `Diagnostics.cs` still emits Warning and points at `diagnostic-system.md` § severity for the drift record. | `Warning, not error` |
| C7 sibling | `graph-analyzer.md` 452 | 452 | "Emit Diagnostic(DeadEndState, Warning)" | "Emit Diagnostic(DeadEndState, Error)" | — |
| C7 sibling | `graph-analyzer.md` 777–779 (resolved-question note) | 779 | "Warning severity, not error: dead-end states may be intentional permanent-hold designs; the author can suppress by adding the `terminal` modifier" | Error, pointing at § 6.2; the note records what it originally said and why that reasoning does not survive — an intentional endpoint is declared, and there is no case left where an undeclared trap should merely be reported. | — |
| N45 | `graph-analyzer.md` 271 | 271 | "emits `DeadEndState` (Warning, code 108)" | "(Error, code 108 — the code still emits `Warning`; see § 6.2)" | `` `DeadEndState` (Warning, code 108) `` |
| N45 sibling | `proof-engine.md` 2149 | 2152 | The same "(Warning, code 108)" phrase in the `DeadEndStateFact` consumption table | "(code 108, Error)" | (same pattern) |
| N46 | `diagnostic-system.md` 518 | 521–522 | "→ Diagnostic(**Warning**, Graph, "UnreachableState", …)" in the worked emission example | Error, with a one-line comment saying this is the ruled severity and `Diagnostics.cs` has not caught up. | `Diagnostic(Warning, Graph, "UnreachableState"` |
| C6 | `diagnostic-system.md` 189 | 190, 193 | `UnsatisfiableGuard` listed among the redundancy *reports* | Moved into the blocking family as its own sub-family — *a declared path that can never be taken* — with want:155's reasoning written out: the test is not whether the entity stays inhabitable but whether something the author declared can never occur, and a row whose guard is provably false makes the path silently never happen and may leave its target state unreachable. `VacuousRule` and `TautologicalGuard` stay reports, and the report sentence now says why (an always-true construct leaves no declared path unreachable). | `UnsatisfiableGuard` no longer appears in the report list (verify by reading `:193`) |
| N49 | `diagnostic-system.md` 191 | 194 | — | **Confirmed and kept** as the ruled severity model, with `UnsatisfiableGuard` added to the list of codes whose `Diagnostics.cs` severity is tracked drift. | n/a |
| N48 | `proof-engine.md` 2537 | 2542 | "(the contradictory/vacuous/unsatisfiable-rule family stay `Warning` per §0.6 items 7/8 "reports")" | The parenthetical is replaced by a sentence saying the invariant does not depend on those severities, and stating them correctly: contradictory and unsatisfiable rules reject because §0.6 item 7 says an uninhabitable rule set makes the compiler reject the definition, while a vacuous rule stays a report under item 8. Records that the old sentence cited both items for one disposition item 7 does not give. | `` stay `Warning` per §0.6 items 7/8 `` |
| N37 | `diagnostic-system.md` 180, 182 | 180, 182 | "**Fault-prevention obligations block** … A fault-prone operation (division, overflow, non-negative, **out-of-range, bounded write**) carries a *containment obligation*" | Split into two kinds carrying the same obligation: a fault-prone operation (division by zero, square root of a negative, empty-collection access) and a **bounded write** (out-of-range, string length, collection count). The second is named as rule preservation, not a fault — a declared bound is rule shorthand (spec §0.7) — with the note that the identical disposition is why they appear together and that the classification must not be quoted the other way round. The section heading axis becomes "containment obligations and structural soundness". | `division, overflow, non-negative, out-of-range, bounded write` |
| N38 | `diagnostic-system.md` 163 | 163 | "The declared-bound **value-safety** codes are `Proof`" | "The declared-bound **containment** codes are `Proof`. These are rule-preservation obligations, not fault gates … and `NumericOverflow` names a computed value leaving a declared bound, not arithmetic overflow of the machine type." The `Proof` classification and the rest of the paragraph are untouched. | `declared-bound value-safety codes` |
| N71 (ADDITION) | absent | `diagnostic-system.md` 184 | — | A new paragraph, **The three verdicts are author-visible**: a refusal reports the search, so the author must be able to tell a proven-violating refusal (this program breaks, here is the case) from an unresolved one (the compiler did not get there, the program may well be fine, here is what would close it). States that both reject so nothing compiles under a weaker promise, that the distinction is about *why* a definition was refused and never about how strongly an accepted rule is held (want:173, spec §0.7), and that a message reporting an unresolved case in the words of a proven violation is a defect against this contract. Message templates are left to phase 4. | `grep -n "author-visible" docs/compiler/diagnostic-system.md` returns **1**, as the row requires |
| N57 | `proof-engine.md` 106, 121, 142–155, 2396, 2398, 2413, 2580, 2588, 2608, 2626, 2644, 2646, 2681, 2696 | same, plus a new note at 151 | Every bare count of the strategy set — "six strategies only", "Strategy count \| 6", "Why six strategies", "one of six strategies succeeded", "Five strategies only", "Adding a sixth strategy", "Five-strategy coverage validation", "the five-strategy boundary", "The six strategies are local and shallow", "The five-strategy set is sufficient" | Each numeral is replaced by a pointer to § Proof Strategies, which is the enumeration. A note at § 4 records the disagreement openly: this document said "six" in some sections and "five" in others, § Proof Strategies enumerates eleven numbered strategies plus two requirement-dispatched paths that reuse another strategy's reporting tag, and `compiler-and-runtime-design.md` says four. **No number was chosen** — picking one is a decision about whether the two tag-sharing paths count as strategies, which no document answers. | `Five strategies only`, `six strategies`, `five-strategy` |
| N73 | `proof-engine.md` 483 (suppression clause) | 485 | "This is a whole-precept suppression — the residual coarseness is a rule over a field the construction event does not assign …; that case … errs in the safe under-emit direction. Per-field precision … is a future refinement, deliberately not built here for parity with the ensure path." | The suppression is named as **wider than the exception the language allows**: spec §0.6 item 11 designs the exception per field — a default the construction event *provably overwrites*. A default the construction event does not assign is still the initial value, so suppressing the fold for it leaves the base case of the induction open for that field, which is a prevention hole rather than a conservative under-emit. Per-field precision is stated as the target and the current early-return as an implementation gap, not a settled design. | `suppresses the rule-vs-default fold entirely` |
| C5 | `soundness-and-coverage.md` 48, 134, 137, 288; `proof-engine.md` 2614; `soundness-and-coverage.md` 16; `compiler/README.md` 37 | 48–56, 146–163, 314; `proof-engine.md` 2617; 16; README 37 | "because the MVP ships **no live re-checker** (see §5b), MVP soundness comes from fixing the known fail-open holes in the engine directly"; "### 5b. Independent re-checker (**deferred**) … **Deferred — not in the MVP** … there is no checker-gated mint in the MVP"; "With the independent re-checker deferred (§5b), MVP soundness comes from fixing the engine directly"; the proof-engine bullet "Certificate format only — no live re-checker in the MVP"; the two summary lines calling it "the deferred independent re-checker" | The checker is in scope, at both call sites: a compiler stage auditing the engine at authoring time, and a load-time gate in the runtime that refuses to govern under a definition whose certificate does not verify. Written as want:201–206 states it — the checker stays small because the search happened at compile time and the replay only confirms recorded steps. **Not built** is stated plainly: no certificate is emitted, no checker exists, a `Proven` verdict is minted by the discharge cascade, and until it lands soundness rests on the engine being right. The direct-engine-correctness bullet is kept and re-framed as complementary rather than as a substitute, with the honest limit — a replay checks the derivations the engine recorded, so an obligation the engine never created is a hole no replay can see. | `no live re-checker` |
| SC-1 | `soundness-and-coverage.md` 87 | 91–100 | "The only line that matters is whether an operand *carries a constraint the engine can discharge* — proof-carrying — or does not." | The heading becomes "Why there is no band-vs-rule boundary". A bound is a rule and there is one constraint mechanism underneath — a modifier is a compact spelling, not a separate system — and every rule is discharged the same way: proven to hold of the initial configuration and preserved by every handler, from the four premise classes (spec §0.7). What decides a site is whether those premises close the proof, not which syntax the constraint was written in. The dead-D2 point and the no-soft-bounds sentence survive. | `carries a constraint the engine can discharge` |
| N50 | `graph-analyzer.md` 556 | — | — | **Confirmed by reading, not edited.** `FieldNeverSet` stays a report: a field that only ever holds its default is a weaker specification, not a path that silently never happens. It is correctly outside the `UnsatisfiableGuard` reclassification. | n/a |
| N63 | `soundness-and-coverage.md` 75 | — | — | **Confirmed by reading, not edited.** "A computed value has no external ingress to govern" stands as an argument about computed values — one of three reasons prove-or-reject beat a fault-floor posture — and is not stated there as a general division of labour by provenance. | n/a |
| N44 | `soundness-and-coverage.md` 282–286 | — | — | **Confirmed by reading, not edited**, exactly as the register requires. It already states the overflow gap correctly. | n/a |

## Sites corrected that no row enumerates

Each asserted the removed model inside a section a row rewrote, so leaving it would have made the
document contradict itself in the same passage.

- **`proof-engine.md` § 14, "No Constraint Evaluation" (2673 → 2675)** — "the evaluator enforces
  dynamically … *Evaluator:* 'Is this constraint satisfied right now given these values?'" This is the
  post-mutation sweep restated as a boundary claim, in the section immediately above the one N51
  rewrites. Retitled **No Evaluation of Rules Against Values**, stating that nothing re-checks a rule
  against the configuration an operation commits (spec §0.7, `evaluator.md` § Ingress Validation) and
  that the evaluator's two questions are the incoming value against the slot's declared constraints, and
  the guard.
- **`proof-engine.md` § 3, Out of Scope, "Constraint enforcement" (135)** — "Runtime constraint checking
  is the evaluator's responsibility." Rewritten to ingress: holding an incoming value to the constraints
  declared on the slot it enters. The proof-engine half of the row is unchanged.
- **`proof-engine.md` table of contents (79–83)** — the entry "No SMT Solver" pointed at a heading
  renamed in an earlier commit ("No Opaque Solver …"), so the anchor was dead. Corrected in the same pass
  as the two entries this slice renamed.

## What was deliberately not touched, and why

Four things, each reported rather than guessed at.

1. **The whole runtime fault-backstop family — register rows C1, C2, N30, N34, N35, and the
   `[StaticallyPreventable]` parenthetical at `soundness-and-coverage.md:106`.** The previous slice left
   this family for one pass over all five files that carry it, because removing the backstops from one
   document leaves the others describing a builder that plants annotations nothing routes. Doing the
   compiler half alone reproduces exactly that problem in mirror image: `proof-engine.md` would stop
   producing `FaultSiteLink` while `precept-builder.md` still consumes it and `evaluator.md` still routes
   it. Three of the five files are outside this slice's scope. **The compiler-side inventory for whoever
   makes that pass**: `proof-engine.md` 107, 123, 258, 315, 321, 333–363, 469, 477, 1925–1940, 2205,
   2213–2224, 2246, 2288, 2298, 2342, 2402–2405, 2449, 2666–2671; `diagnostic-system.md` 548–554 and
   § FaultCode → DiagnosticCode Chain; `soundness-and-coverage.md` 106, and the coverage-matrix cells at
   the `QualifierMismatch` and runtime-residue notes.
2. **`proof-engine.md` § 13, "all five requirement kinds" against § 16, "`ProofRequirementKind` enum
   (thirteen members)".** The document disagrees with itself about how many requirement kinds exist.
   Settling it means reading the catalog, which this phase may not cite as evidence. Left unedited,
   reported here.
3. **The `ConstraintPlanIndex` (N10 / N80).** `grep -rn ConstraintPlanIndex docs/compiler` returns
   **nothing**. Every one of the register's roughly thirteen sites is in `docs/runtime/evaluator.md` and
   `docs/runtime/precept-builder.md`. There was no compiler-side work to do, and the runtime slice has
   already re-scoped the index to ingress routing.
4. **`compiler-and-runtime-design.md` (N58's "four strategies only", and the whole WIDE set), the
   specification, `docs/philosophy.md`, `docs/runtime/`.** Other slices, as instructed.

## Contradictions with the want document found outside the register

- **`graph-analyzer.md` § 2, Out of Scope, "Guard analysis" (96)** — "Guard expressions are opaque to the
  graph analyzer. The proof engine handles guard satisfiability." Not a contradiction; recorded because a
  reader correcting N56 will meet it and it is already right.
- **`proof-engine.md` § 4 metric table, "Estimated LOC 400–600"** — a size claim for a stage whose
  prove-or-reject layer is designed and unbuilt, and which the induction and the certificate both add to.
  It is not a model claim and no row covers it, but it will be wrong. Left standing.
- **`proof-engine.md` § 13, "Certificate format only"** — corrected under C5, but the bullet still lists
  the certificate work as part of a "prove-or-reject MVP" scope list that the induction (N51/N52) now
  exceeds. Whether the induction is inside that MVP scope list is a planning question, not a document
  contradiction, so nothing was written about it.
