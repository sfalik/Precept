# Phase 2 — the R rows executed against `docs/runtime/`, plus the two `docs/compiler/` siblings of S15

Uncommitted working-tree edits, made 2026-07-27 against HEAD `5e853c0e` (the commit that landed the
specification corrections). Every row below is one of the `R*` rows in
[`sweep-correction-list.md`](sweep-correction-list.md), executed in the direction that row gives —
revised where the "Reconciliation against the 2026-07-26 owner rulings" section at the end of that file
revises it, and re-aimed at the corrected specification where the specification now says something more
precise than the row anticipated (see [`spec-corrections-executed.md`](spec-corrections-executed.md)).
Line numbers in the *After* column are post-edit.

Files touched: `docs/runtime/runtime-api.md`, `docs/runtime/result-types.md`, `docs/runtime/evaluator.md`,
`docs/compiler/proof-engine.md`, `docs/compiler/diagnostic-system.md`. Nothing else.

## Rows executed

| Row | Site before | Site after | Removed | Written in its place | Grep pattern now returning zero |
|---|---|---|---|---|---|
| R1 | `runtime-api.md` 148, 162, 168, 171, 174–178, 180 | 148–153, 162, 168, 173–178, 180 | The `ConstraintsFailed` case in the `Create` example; "evaluate constraints" as a construction pipeline step; `Created` produced when "all post-mutation constraints passed"; the whole `ConstraintsFailed` outcome row; "same guards, same ensures, same constraint checking" | Construction pipeline ends "→ commit", with the reason stated (the compiler proved the initial event establishes every rule). `Created` is produced when the action chain "ran to completion". `ConstraintsFailed` moves into the structurally-impossible list with its reason, matching the specification's §3A.2 construction table. `InvalidArgs` gains the ingress framing and the collect-all note the specification gives it. | `Post-mutation constraint violations`, `all post-mutation constraints passed`, `same guards, same ensures, same constraint checking` (in `docs/runtime/`) |
| R2 | `runtime-api.md` 361 | 372–374 | "…→ computed-field recomputation → constraint evaluation (collect-all) → commit or discard" | Pipeline ends "→ computed-field recomputation → commit", plus a paragraph naming the two checks that do run (argument ingress, guard evaluation) as exactly the premises the proofs rest on, and naming the only two ways a working copy is discarded (ingress failure, `reject` row). | `constraint evaluation (collect-all) → commit or discard` |
| R2 sibling | `runtime-api.md` 365 | 376 | "A mutation row executes its action chain and produces `Transitioned`, `Applied`, or `ConstraintsFailed`" | `Transitioned` or `Applied` only, with first-match-wins and "the remaining rows are not evaluated" cited to §3A.4 phase 2. | `Applied\`, or \`ConstraintsFailed` |
| R3 | `runtime-api.md` 388 | 401–403 | "…patch application to working copy → computed field recomputation → constraint evaluation (collect-all) → commit or discard" | Ingress validation of each patched value moved **before** application, then application, recomputation, commit. Wording follows the 2026-07-26 revision, not the row's original direction: the check "holds the incoming value to the constraints declared on the field it enters — nothing wider", and "an editable field is an ordinary write site, and the compiler discharged its obligations at compile time like any other." **No wider ingress check was written.** | `constraint evaluation (collect-all) → commit or discard` |
| R4 | `runtime-api.md` 646 | 663–665 | "…same guard evaluation, same action chain, same constraint checking. The only difference is disposition: inspection discards the working copy; commit promotes it on success." | Same ingress validation, same guards, same action chain, same recomputation. A second paragraph states that inspection has no constraint-evaluation phase to preview because execution has none (spec §3A.6), and names what inspection answers instead: ingress outcome, which guard wins, resulting field values, and the proof with its premises where a rule holds by proof rather than by check. | `same action chain, same constraint checking` |
| R5a | `runtime-api.md` 266 | 278 | "a stale value is re-governed by the next operation's post-mutation constraint sweep (spec §3A.4)" | "Nothing re-governs a stale value later either: there is no pass over the finished working copy that a subsequent operation could catch it in." The trusted-restore first half is kept verbatim. | `post-mutation constraint sweep` |
| R5a (scope addition) | absent from all runtime docs | `runtime-api.md` 250–260, new § *Definition Load and the Certificate Gate* | — | The load-time certificate gate the want document requires, which appeared nowhere in runtime canon: what the certificate records, that `Precept.From` refuses to return a usable `Precept` when it does not verify, that the checker walks the recorded derivation rather than re-deriving, and that the premise list is the set of runtime validations the proofs depend on. Marked **Not yet built** — no certificate is emitted and no checker exists; the record's shape and the checker's API are explicitly not settled there. | (new content — nothing removed) |
| R5b | `runtime-api.md` 889 | 909 | "A stale value surfaces at the next operation, whose post-mutation constraint sweep (spec §3A.4) governs the resulting working copy." | "It is not detected later either: no subsequent operation re-governs a value that did not enter through the contract (spec §0.7)", plus the out-of-contract statement the corrected §0.7 makes — the host has stepped outside the guarantee and nothing inside the engine will notice. | `post-mutation constraint sweep` |
| R6 | `result-types.md` 101, 114, 121, 137, 145, 146 | 100, 112, 119–123, 139, 147–152 | `EventOutcome.ConstraintsFailed` from the DU and from the variant table; the "covers ALL post-fire constraints" scope note; `Updated` described as "constraints passed"; `UpdateOutcome.ConstraintsFailed` described as "patch applied to working copy but constraints violated" at stage "Rule/ensure evaluation" | `EventOutcome` loses the variant, with the reason stated and `InvalidArgs` re-described as the collect-all ingress failure. `UpdateOutcome.ConstraintsFailed` is **kept and re-scoped** to an ingress failure at §3A.4 phase 1, refused before application — with a note that the variant name now reads wrong and should be revisited when the `Update` API is next opened. Renaming it was not done: no document settles the name. | `Post-mutation constraints violated`, `Covers ALL post-fire constraints` |
| R7a | `evaluator.md` 141, 144, 503, 513, 533–536, 606–613, 643, 704–722, 780–787, 1638 | 141, 144, 497–511, 531–533, 603, 640, 676–711, 736–757, 1609 | The `Constraint evaluation` responsibility row; "before constraint evaluation" in the recomputation row; flow-diagram step 2h and the candidate-resolution step; lifecycle steps 9 and 10; the constraint fold in both `Create`-without-initial-event pseudocode blocks; step 2h and the whole candidate list in the `Fire` pseudocode; step 4 of the `Update` pseudocode; the `ConstraintsFailed` row in the in-domain failures table | The responsibility row becomes **Ingress validation**. The flow diagram's step 2 becomes row selection then execution of the winner, with a reject row returning at dispatch; step 3 becomes **Commit**. Lifecycle steps 9 and 10 become one unconditional **Commit**, naming the two ways the array goes back to the pool. Both `Create` blocks commit directly, citing the compile-time base case. The `Fire` pseudocode returns on the first guard that passes — no candidate list, no ambiguity branch — with a prose paragraph preserving why the ambiguity branch is gone. `Update` validates ingress before applying the patch. The failures table gains one ingress row per operation. | `EvaluateFireConstraints(`, `Constraint Evaluation (§3A.4`, `Commit or discard (§3A.4`, `new EventOutcome.ConstraintsFailed` |
| R7b | `evaluator.md` 1700 | 1717–1729 | § *Constraint Evaluation Guarantees* and its "Post-mutation evaluation" row | § **Ingress Validation Guarantees**, with the post-mutation row replaced by "Checked on entry, not after" and a paragraph saying why the bucket index survives anyway (it still builds `Precept.Constraints` and `Version.ApplicableConstraints`, and still routes an incoming value to the constraints on the slot it enters). | `Post-mutation evaluation \| Constraints are evaluated` |
| R8 | `evaluator.md` 1682 (at `ed57d7fd`) | 1706, 2173 | — | **Confirmed discharged, struck, not edited.** Both sites read "`Restore` bypasses both access-mode checks and constraint evaluation — trusted hydration (spec §0.7; Decision 5)". Agrees with the want document. | `bypasses access-mode checks but enforces constraint checks` |
| S15 sibling | `proof-engine.md` 483 | 483 | "Prove-or-reject (§0.6 #1): the diagnostic emits **only** on a fold that proves `false`. An unknown/unfoldable default — a computed (`<-`) field, a non-constant default, an `UnknownSentinel` — folds to `null` and never rejects." | The three-way verdict from the corrected spec `:213`, with the base-case reasoning, plus an explicit **Implementation gap** sentence saying `ScanRulesAgainstDefaults` currently under-emits. The designed exception (a default the construction event provably overwrites) was already described in the same paragraph and is left standing. | `folds to \`null\` and never rejects` |
| S15 sibling | `diagnostic-system.md` 417 | 417 | "Like every default fold, an unknown/unfoldable default never rejects." | The three-way verdict, the designed exception, and a sentence recording that the current implementation emits only on a proven-`false` fold and that this is a known gap. | `an unknown/unfoldable default never rejects` |

## Sites corrected that no row enumerates

Each asserted the same model inside a section a row rewrote, so leaving it would have made the document
contradict itself in the same passage.

- **`runtime-api.md` 460 → 475** — `ConstraintViolation` "appears in `EventOutcome.ConstraintsFailed.Violations`", a variant that no longer exists.
- **`runtime-api.md` 878 → 895** — "the `Rejected` and `ConstraintsFailed` outcomes remain possible" for stateless construction.
- **`runtime-api.md` 883–889 → 883–890** — the stateless-construction pipeline table listed `to`/`in` ensures and global rules as evaluated at construction. They are now marked proven-at-compile-time; the argument rows are marked evaluated at ingress; promotion is marked unconditional.
- **`result-types.md` 229, 244, 349** — `HypotheticalResult` gated on "all constraints pass"; `Prospect` gated on "guard passed **and** all constraints satisfied"; the Fire-vs-Update pipeline contrast naming "constraint evaluation" as Update's second half.
- **`evaluator.md` 168, 183** — the LOC estimate and the bounded-complexity paragraph both named constraint evaluation as a component of the evaluator.
- **`evaluator.md` 335, 359** — the `EventOutcome` and `UpdateOutcome` declarations mirrored in this document.
- **`evaluator.md` CC#26 list (≈ 630)** — "Arg ensures, field constraints, global rules … all run normally" split into what runs at ingress and what is proven.
- **`evaluator.md` 850–871, 909, 914 → 845–869, 908, 917** — the inspect path derived `Prospect` from violation counts and reported `Possible` when two rows both passed. Both are now guard-only and first-match, which is what `result-types.md` § Prospect propagation already said.
- **`evaluator.md` § 7.6 (1353)** — the section was `EvaluateFireConstraints`, the post-mutation bucket walk itself. Rewritten as **Ingress Validation**, stating explicitly that `Always` / `FromState` / `ToState` are not walked and why they are still built.
- **`evaluator.md` Decision 3, 4, 5, 7 (1774, 1788, 1801, 1815)** — "one constraint evaluation implementation"; "constraint violation is a business event"; "callers who need to validate restored state should call `InspectFire`" (no longer possible — replaced with a statement that no such facility exists and that this is deliberate, citing want:141 and spec §0.7); Decision 7 re-scoped from constraint evaluation to ingress validation.
- **`evaluator.md` 2063, 2111, 2120, 2156** — the resolved-question entry on bucket evaluation order (now moot); the no-incremental and no-parallel entries; the § *Constraint Evaluation Matrix*, rewritten as § **Ingress Validation Matrix** with every row restated and a heading note that no operation evaluates a constraint against the finished working copy. Three table-of-contents entries updated to match.
- **`proof-engine.md` 1987, 2071–2075** — the PRE0115 half of the same never-reject posture, inside the document whose PRE0164 half row S15 names. Corrected in lockstep, with the pseudocode comment marked as current behaviour against a stated target.

## What was deliberately not touched, and why

Four things. Each is reported rather than guessed at.

1. **The inspection result types' `ConstraintResult` arrays.** `TransitionInspection.Constraints`,
   `UpdateInspection.Constraints` and `EventInspection.EventEnsures` were shaped around the pass that no
   longer exists. The want document says inspect should cite proof provenance (`:211`); neither it nor the
   specification says which type carries it. The three arrays and their descriptions are left as they are,
   with an explicit open note in `result-types.md` (250) and `evaluator.md` (910) and an unspecified
   `BuildConstraintResults` in the pseudocode. Inventing a provenance record here would be designing.

2. **The shape `EventOutcome.InvalidArgs` needs.** The corrected specification says ingress validation is
   collect-all — "every failing argument is reported". `InvalidArgs(string Reason)` carries one string,
   while `UpdateOutcome.ConstraintsFailed` carries a structured `ConstraintViolation` array for the same
   kind of failure on a patch. Whether `InvalidArgs` gains a `Violations` array, splits into two variants,
   or stays a string is an API-shape decision no document settles. Recorded as an open note in
   `result-types.md` (123) and left unedited.

3. **The runtime fault-backstop family — rows R9, R10, R11, R12, R13, R14, and `runtime-api.md`'s
   three-layer enforcement model.** These are a single coherent group spanning `evaluator.md` (147, 1654),
   `precept-builder.md` (115, 132, 426, 488, 607), `fault-system.md` (149–156, 248, 263),
   `runtime-api.md` (677, layer 3 of the three-layer model), and the compiler-side rows C1–C4 in
   `proof-engine.md` and `diagnostic-system.md`. Only two of those files are in this slice's scope.
   Removing the backstops from `evaluator.md` alone would leave the runtime documentation saying the
   builder plants annotations that nothing routes. The overflow ruling (walk item 15) also changes what the
   replacement text has to say, since overflow is now guarded at neither end. This wants one pass over all
   five files, not two-fifths of one.

4. **`docs/philosophy.md` (P1–P4) and `docs/language/precept-language-spec.md`** — other slices, as
   instructed.

## Contradictions with the want document found outside the list

- **`runtime-api.md` § Three-Layer Enforcement Model, layer 3 (677)** — "15 `[StaticallyPreventable]`
  fault codes exist at evaluation time as a backstop." The corrected spec §0.7 now says in as many words
  that there are no runtime fault traps. This is the fault-backstop family above; it is in a file this
  slice edited but in a section no row touches, and correcting it alone would have made the same
  half-a-family problem inside a single document. Left standing, flagged here.

- **`precept-language-spec.md:2080`** — "Fire the initial event with the caller's args through the
  standard pipeline — same guards, same mutations, same ensures, same constraint checking as any other
  event." This is the same sentence row R1 corrects at `runtime-api.md:180`, and the specification slice
  did not reach it. It is in a file this slice was told not to touch, so it is left standing. It is a
  one-line fix for whoever next opens the specification.

- **`evaluator.md` § Output Guarantees, *Fault–diagnostic correspondence* (1704)** — "If the compiler
  emits no errors, the evaluator should never fault." Under the overflow ruling this is no longer true:
  the compiler does not prove overflow absent and the runtime carries no guard, so a clean compile can
  still overflow. The corrected specification records that gap at §0.7; this line does not. Same family,
  left standing, flagged here.
