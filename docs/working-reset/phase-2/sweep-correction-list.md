# The canon-correction list — the post-mutation sweep and what has to come out of the canonical documents

| Field | Value |
|---|---|
| What this is | The worklist of every place in the canonical documents that asserts the post-mutation constraint sweep, the runtime fault backstop, restore-time constraint validation, or the fault-gate classification of declared-bound containment — each with the exact sentence, the want-document section it contradicts, and the direction the correction has to go. No replacement wording is decided here. |
| Where it came from | `docs/Working/want-analysis-2026-07-19/DECISION-PACKET.md` § (d), which compresses `docs/Working/want-analysis-2026-07-19/canon-corrections-and-supersession.md` §§ 2 and 4. The 25 individually-verified rows are `verification.md` canon-1 … canon-25, all CONFIRMED. |
| Which phase consumes it | **Phase 2** — settle every place canon and the want document disagree. This is phase 2's starting point. Not one row has been executed. |
| Naming | `compiler-readiness-plan.md` § Phase 1 lists this output as `docs/working-reset/canon-corrections.md`. It is this file. Pick one name and make the plan match before phase 2 starts, so nobody goes looking for a second list. |
| Copied and re-verified | 2026-07-26 against HEAD `3f69b316`, branch `spike/Precept-V2-Radical-reset`. The original verification ran at `ed57d7fd`; the canonical documents have changed since, so every line number below was re-checked and the drift is recorded per row. |
| What produced a *traced* verdict | Nothing here. This is agent-authored evidence about **what canon says**, which is a documentary fact anyone can recheck — it is not a record of anything the owner ruled. The direction column reflects the want document (`docs/Working/what-i-want-2026-07-16.md`, bylined "Author: Shane"), so each row's *target* traces; the row's existence and its wording do not. |

## How to read the re-verify column

- **holds** — file, line and verbatim text all resolve at HEAD exactly as recorded.
- **moved** — the text resolves verbatim, at a different line. The new line is given.
- **reworded** — the sentence at that place still asserts the same model but is no longer byte-identical to the quote. Read before editing.
- **discharged** — the text is gone at HEAD and the place now says something compatible with the want. The row needs no edit; it needs confirming and striking.

Nothing below was silently fixed. Where a row failed to re-verify it is marked, and the original citation is left in place next to the new one.

---

## 1. The 25 verified rows

Adversarially confirmed at `ed57d7fd`, re-verified at `3f69b316`. Row IDs are the canon report's (`S*`, `P*`, `R*`); the `canon-N` column is the verification entry, so the count of 25 is auditable rather than asserted. `R5` and `R7` are each one row in the canon report covering two sites; verification checked each site separately, which is where 23 report rows become 25 verified rows.

### 1.1 `docs/language/precept-language-spec.md`

| # | canon-N | Line 07-19 | Line at HEAD | Re-verify | Contradicting text (verbatim) | Want section contradicted | Direction of correction | Sev |
|---|---|---|---|---|---|---|---|---|
| S1 | canon-1 | 1969 | 1969 | holds | "This post-mutation sweep is one of two enforcement points. … The sweep then re-checks every constraint against the completed working copy. Ingress governs *what enters*; the sweep governs *the result*. (Restored state is neither: it is trusted as valid at persistence time and is re-governed only by the next operation's sweep — §0.7.)" | § Certificates ¶1 (no post-mutation re-evaluation); § The runtime's role (runtime runs declared premises and nothing beyond them) | The whole paragraph goes. §3A.4 returns to being about atomic all-or-nothing write mechanics only. Restored state is trusted and is never re-governed by any later sweep. Relational-rule enforcement on edits moves to the editable-write ingress point (field's modifier-rules plus every rule mentioning the field), per the want's ingress paragraph. | high |
| S2 | canon-2 | 1967 | 1967 | holds | "Constraints are evaluated against the working copy after all mutations complete. If every constraint passes, the working copy is promoted to become the entity's committed state. If any constraint fails, the working copy is discarded…" | § Certificates ¶1; § The runtime's role | Keep the atomicity guarantee (multi-action writes are all-or-nothing; no observable partial state). Remove constraint evaluation as the commit condition: commit follows premise-validated execution unconditionally, because every handler was proven to preserve every rule at compile time. There is no post-mutation pass/fail decision. | high |
| S3 | canon-3 | 149 | 149 | holds | "Mutations are atomic. All mutations execute on a working copy. Constraints are evaluated against the working copy. If all constraints pass, the working copy is promoted. If any constraint fails, the working copy is discarded." | Same as S2 | Same as S2 — this is the same model restated; correct in lockstep with §3A.4. | high |
| S4 | canon-4 | 162 | 162 | holds | "A transition row is a flat sequence: evaluate a guard, execute assignments left-to-right, check rules and ensures." | § The runtime's role | Drop "check rules and ensures" as a runtime execution step. The row's runtime sequence is: validate args at ingress, evaluate the guard, execute assignments, commit. | medium |
| S5 | canon-5 | 1354 | 1354 | holds | "A *rule condition* — and a *constraint modifier*, which is rule shorthand (§2.4) — is checked against the complete working copy *after* all mutations, so declaration order is irrelevant and any field is in scope." | § Certificates ¶1 | The scoping conclusion (any field in scope, declaration order irrelevant) survives — but its justification changes: a rule constrains the whole configuration as a proof obligation over every write site, not because a runtime check runs after mutations. | medium |
| S6 | canon-6 | 1903 | 1903 | holds | "**Validation surfaces,** which are collect-all. Rules and ensures are evaluated exhaustively — every applicable constraint is checked, and all violations are reported." | § The runtime's role; § Certificates ¶1 | The runtime's validation points are ingress (event args, editable-field writes) and guards. Rules and ensures are not evaluated at runtime; collect-all semantics survive only where validation actually runs — event args **and** editable-field writes. | high |
| S7 | canon-7 | 1938 | 1938 | holds | "`ConstraintsFailed` \| Post-mutation constraint violations (collect-all semantics)." | § Certificates ¶1 | The `ConstraintsFailed` outcome presupposes post-mutation constraint evaluation; under the want it cannot arise. Remove or re-scope the outcome taxonomy (construction §3A.2 table; same change ripples to the runtime docs, rows R1/R8). | high |
| S8 | canon-8 | 1946 | 1946 | holds | "Entity restoration from persisted data produces a distinct outcome space: successful restoration (data valid, constraints passed), constraint failure (persisted data violates current definition's rules/ensures), or invalid input… Restoring an entity in an invalid state is not allowed — the governance guarantee applies from the moment an entity is loaded, not just when it is mutated." | § The compiler's promise, ingress paragraph ("Restore/rehydration is *not* ingress — data coming back from persistence is trusted and unchecked") | The constraint-failure arm of the restoration outcome space goes; restore trusts persisted data. (This paragraph also contradicts the spec's own §0.7:272 trusted-restore sentence — an intra-canon contradiction.) The load-time verification that *does* exist under the want is the certificate check on the **definition**, not constraint evaluation on the **data**. | high |
| S9 | canon-9 | 268 | 268 | holds | "Because mutations execute on a working copy that is discarded if any constraint fails (§3A.4), an invalid configuration never persists — this is prevention, not detection." | § Certificates ¶1 | §0.7's governance paragraph is otherwise the closest canon text to the want (ingress-scoped). This one clause ties governance to the sweep and goes: an invalid configuration never persists because every handler is proven to preserve every rule, not because a sweep discards violations. | medium |
| S10 | canon-10 | 272 | 272 | holds | "…the next operation through the contract re-governs it (every constraint is checked against the resulting working copy, §3A.4), and the runtime fault traps backstop any out-of-contract value the evaluator would otherwise fault on." | § The compiler's promise (restore is trusted and unchecked); § The runtime's role (no fault checks at evaluation time) | Both clauses go: there is no next-operation sweep to re-govern out-of-contract data (it is trusted, full stop), and there are no runtime fault traps. The trusted-restore *first half* of this paragraph survives. | high |
| S11 | canon-11 | 266 | 266 | holds | "…cannot produce a runtime fault — no division by zero, no overflow, no empty-collection access, no result outside a declared bound. The compiler delivers it by discharging, at every fault-prone operation, an obligation that each operand *carries* a sufficient constraint…" | § The compiler's promise ("Modifiers are sugar for rules … one constraint mechanism underneath"); § Worked example (`PlanRepayment` — faults get the identical premise-and-certificate treatment, as a distinct family) | Fault-gate residue site 1 of 3. Separate the true fault list (division, overflow, empty access) from declared-bound containment, which is rule preservation under the single constraint mechanism — the prove-or-reject disposition is identical, so nothing weakens, but the classification stops being quotable as a fault gate. Restate the discharge model as proof by induction from the four premise classes (modifiers, arg constraints, guards, pre-state rules) rather than a per-operation carried-constraint test. The sentence's "there is no deferral" clause is aligned and survives. | medium |
| S12 | canon-12 | 112 | 112 | holds | "Every fault class that the evaluator can produce — type mismatch, division by zero, overflow, empty collection access, constraint range impossibility — is linked to a compiler diagnostic that prevents it. Runtime fault checks exist only as defensive redundancy, never as the primary enforcement mechanism." | Same as S11; § The runtime's role | Fault-gate residue site 2 of 3: drop "constraint range impossibility" from the fault-class enumeration (it is rule preservation). Separately: "runtime fault checks exist only as defensive redundancy" becomes "do not exist" — what gives confidence under the want is the certificate verified at load, not redundant evaluation-time checks. | medium |
| S13 | canon-13 | 110 | 110 | holds | "Runtime fault traps exist only as defensive redundancy for paths the compiler has already proven unreachable." | § The runtime's role | Same as the second half of S12 — Principle 10's trap clause goes; the rest of P10 (prove safety or reject) is aligned. | medium |
| S15 | canon-14 | 213 | 213 | holds | "Only a provably-false fold rejects; an unknown/unfoldable default never rejects (per Proof philosophy #1–2)." | § The compiler's promise, base case ("The default configuration satisfies every rule"); § What must not compile (the `default 20000.0` row); § No deferral | The flag posture on defaults contradicts the base case: a default that actually constitutes part of the initial configuration and cannot be proven to satisfy the rules leaves the base case open and must reject, with the unresolved-blocks treatment §0.6 #2/#5 already applies to bounded writes. (Defaults a construction event provably overwrites are placeholders and are a designed exception.) Siblings: `docs/compiler/diagnostic-system.md:417`, `docs/compiler/proof-engine.md:483`. | medium |

**S3 footnote, carried from the packet.** Line 149 sits in §0.3 (Governance, Not Validation), not §0.2 as the canon report's direction column says — quote, line, contradiction and lockstep target all verified; only the section label was wrong.

**S15 sibling drift.** `proof-engine.md:483` still asserts the same never-reject posture at HEAD but no longer word-for-word: it now reads "An unknown/unfoldable default — a computed (`<-`) field, a non-constant default, an `UnknownSentinel` — folds to `null` and never rejects." Same claim, different sentence. `diagnostic-system.md:417` still carries the original wording.

### 1.2 `docs/philosophy.md` — every row here is owner-gated

CLAUDE.md: no philosophy edits without explicit owner approval. The want document itself names P1 for owner-approved rewording (`what-i-want-2026-07-16.md:203`).

| # | canon-N | Line 07-19 | Line at HEAD | Re-verify | Contradicting text (verbatim) | Want section contradicted | Direction of correction | Sev |
|---|---|---|---|---|---|---|---|---|
| P1 | canon-15 | 19 | 19 | holds | "**Fire**: execute a transition. The engine validates input arguments, selects a matching transition row via guards, executes mutations, evaluates all applicable constraints against the resulting configuration, and commits only if every constraint holds." | § Certificates, philosophy flag — the want statement names this sentence as needing "a deliberate, owner-approved rewording — the constraint evaluation moves to proof-plus-premise-checks, verified by certificate." | Exactly as the want flags: Fire's described mechanism becomes arg validation at ingress, guard evaluation, mutation, unconditional commit — the constraints hold because they were proven preserved, verified by certificate. | high |
| P2 | canon-16 | 51 | 51 | holds | "Whether the rule is about what the entity *is* or what the operation *brings in*, it evaluates atomically — the change and the check are the same act." | § Certificates ¶1; § The runtime's role | "The change and the check are the same act" asserts per-operation runtime rule evaluation as the enforcement mechanism. Under the want, enforcement is compile-time proof plus ingress/guard premise checks; there is no per-operation rule-evaluation act. The guarantee claim in the surrounding sentences ("no operation can produce a result that violates a declared rule") is aligned and survives. | medium |

### 1.3 `docs/runtime/`

| # | canon-N | Site 07-19 | Site at HEAD | Re-verify | Contradicting text (verbatim) | Want section contradicted | Direction of correction | Sev |
|---|---|---|---|---|---|---|---|---|
| R1 | canon-17 | runtime-api.md:171 | 171 | holds | "`ConstraintsFailed` \| Post-mutation constraint violations (collect-all semantics). \| A mutation row matched and executed, but constraint evaluation found violations." | § Certificates ¶1 | Same as S7 — the outcome presupposes the sweep. Adjacent rows carry the same model: `:168` ("A mutation row matched and all post-mutation constraints passed"), `:180` ("same guards, same ensures, same constraint checking"). Both still at 168 and 180 at HEAD. | high |
| R2 | canon-18 | runtime-api.md:361 | 361 | **reworded** | Verified fragment holds verbatim: "constraint evaluation (collect-all) → commit or discard". The surrounding sentence has been rewritten since `ed57d7fd` — it now reads "Fire runs the event pipeline in § 3A.4 order (`precept-language-spec.md`, § 3A.4, *Operation execution order*): ingress governance (arg validation) → dispatch (row matching, first-match with guard evaluation) → exit actions → the state change and `omit` reset → the row's action chain on the working copy (mutation rows) or rejection (reject rows) → entry actions → computed-field recomputation → constraint evaluation (collect-all) → commit or discard." The pipeline gained stages; the contradicting tail is unchanged. | § The runtime's role; § Certificates ¶1 | The "constraint evaluation (collect-all) → commit or discard" stage goes; the pipeline ends "…recomputation → commit." Arg validation at ingress and guard evaluation are exactly the premises the want keeps. | high |
| R3 | canon-19 | runtime-api.md:386 | **388** | moved | "Update runs the field-write pipeline: access mode check → type validation → patch application to working copy → computed field recomputation → constraint evaluation (collect-all) → commit or discard." | § The compiler's promise, ingress paragraph | Update's validation moves to the ingress point: the incoming value is checked against the field's modifier-rules and every rule that mentions the field, *before* application — not whole-entity constraint evaluation after patch application. | high |
| R4 | canon-20 | runtime-api.md:646 | **648** | moved | "`InspectFire` runs the same pipeline as `Fire`— same guard evaluation, same action chain, same constraint checking." | § Certificates ¶1; § Certificates (inspect cites provenance) | "Same constraint checking" inherits the sweep. Inspect's prediction changes with the model: it previews arg/ingress validation, guard outcomes, and cites proof provenance ("no check needed here — proven from these premises"), rather than predicting a post-mutation constraint verdict. | medium |
| R5a | canon-21 | runtime-api.md:266 | 266 | holds | "…a stale value is re-governed by the next operation's post-mutation constraint sweep (spec §3A.4)." | § The compiler's promise, ingress paragraph | The trusted-restore first half of this paragraph is aligned and survives; the sweep clause goes — nothing re-governs restored data. **Scope addition carried on this row:** the runtime load-time certificate gate the want requires ("the runtime refuses to govern under a definition whose certificate does not verify") appears **nowhere** in runtime canon — verified 2026-07-19 as zero certificate mentions across all seven runtime docs. Adding it is in this correction's scope. | high |
| R5b | canon-22 | runtime-api.md:889 | **891** | moved | "A stale value surfaces at the next operation, whose post-mutation constraint sweep (spec §3A.4) governs the resulting working copy." | § The compiler's promise, ingress paragraph | Same as R5a — the sweep clause goes. | high |
| R6 | canon-23 | result-types.md:114 | 114 | **reworded** | Verified fragment holds verbatim: "`ConstraintsFailed` \| Post-mutation constraints violated (rules, state ensures, event ensures)". The row's third column has changed since `ed57d7fd` — it read "Stage 9-10 \| Yes" and now reads "Constraint evaluation → discard (§3A.4) \| Yes". | § Certificates ¶1 | Same as R1/S7 — the outcome-type taxonomy loses its post-mutation-violation arm. | high |
| R7a | canon-24 | evaluator.md:502–505 | **533–536** | moved | "**Evaluate:** Run constraint plans against the working copy" (:533) / "**Commit or discard:** If constraints pass, donate the working copy … if constraints fail, return the array" (:534) / "This ensures that constraint evaluation sees the post-mutation state…" (:536). The lifecycle step numbers changed with the move: what was steps 6–7 is now steps 9–10, and both now carry "(§3A.4 — …)" labels. | § The runtime's role; § Certificates ¶1 | The Fire lifecycle loses the evaluate/discard model; commit is unconditional after premise-validated execution. Same family, all re-verified at HEAD: `:141` (responsibilities table, "Constraint evaluation … collect violations"), `:144` ("After mutations and before constraint evaluation"), `:1638` (was `:1585`, "Rows matched, but constraint(s) failed post-mutation"), `:1842` (was `:1789`, collect-all decision) — correct as one family. | high |
| R7b | canon-25 | evaluator.md:1700 | **1753** | moved | "Constraints are evaluated against the working copy AFTER action execution and computed field recomputation" — the Description cell of the "Post-mutation evaluation" row in § Constraint Evaluation Guarantees. | § Certificates ¶1 | The constraint-evaluation guarantees table restates the sweep; corrects with the Fire-lifecycle family. | high |

### 1.4 The additional confirmed row from the clarity axis

Not in the canon report's table; confirmed within the clarity overall-verdict check.

| # | Site 07-19 | Site at HEAD | Re-verify | What it asserts | Direction |
|---|---|---|---|---|---|
| SC-1 | `docs/compiler/soundness-and-coverage.md` §1.1 | **:87** | holds | "…only line that matters is whether an operand *carries a constraint the engine can discharge* — proof-…" — the proof-carrying framing, **committed** at `e13b94cd` with a commit message flagging it as a known defect. | A sixth committed canon edit, superseded by the want's one-mechanism model. Note §1.1's heading has since changed to "Why prove-or-reject — the decision rationale" (at `:55`); the sentence itself is unchanged and still live at `:87`. |

---

## 2. Unverified rows — leads, not findings

In the canon report; **not individually cross-examined**. Treat each as requiring a read before edit. Line numbers below are re-verified at HEAD; the content is not.

### 2.1 Ledger overflow (14 rows)

| # | Site 07-19 | Site at HEAD | Re-verify | Note |
|---|---|---|---|---|
| R8 | evaluator.md:1682 | **1735** | **discharged** | The sentence at `ed57d7fd` read "`Restore` bypasses access-mode checks but enforces constraint checks" — an intra-canon contradiction independent of the want. At HEAD it reads "`Restore` bypasses both access-mode checks and constraint evaluation — trusted hydration (spec §0.7; Decision 5 below)." The contradiction is gone and the text now agrees with the want. Confirm and strike; do not edit. |
| R10 | evaluator.md:147 | 147 | holds | "**Fault backstop routing** \| At impossible-path sites: check `opcode.FaultSite` … non-null → `Faults.Create(annotation, context)`" — same family as R9; evaluation-time fault routing goes. |
| R9 | precept-builder.md:426, :607 | 426, 607 | holds | `:426` stamps `FaultSiteAnnotation` on opcodes from unresolved proof obligations; `:607` "Fault backstops are defense-in-depth… When reached, they fire the `FaultCode`'s runtime behavior". Family also at `:115`, `:132`, `:488` — all re-verified at those lines. |
| R11 | fault-system.md:247, :263 | 248, 263 | moved | The divide-by-zero end-to-end example: "1. Evaluator: `a / b` where `b == 0` → `Fail(FaultCode.DivisionByZero)`" — the `Fail(...)` line is at 248 at HEAD (the example block starts at 244). |
| R12 | fault-system.md:263 | 263 | holds | "…must be handled if data arrives from external sources that bypassed compile-time checking." |
| R13 | fault-system.md:149–156 | 149–156 | holds | `[StaticallyPreventable(DiagnosticCode.OutOfRange)]` at 149, `LengthBoundViolation,` at 153, `CountBoundViolation,` at 156. |
| C1 | proof-engine.md:346 | 346 | holds | "The Precept Builder consumes these to plant `FaultSiteAnnotation` backstops — defense-in-depth runtime checks for operations that could not be proven safe." Family also at `:107`, `:123`, `:342`, `:2210`, `:2243`, `:2435`, `:2667` — all re-verified at those lines. |
| C2 | proof-engine.md:2446 | 2446 | holds | "…backstops are defense-in-depth — they should never fire if proof is correct. They exist for belt-and-suspenders safety…" |
| C3 | proof-engine.md:1733 | 1733 | holds | "…the field already satisfies its band entering the chain — §3A.4 swept prior state…" A compile-time proof premise grounded on the runtime sweep. Re-ground on the inductive hypothesis; the proof mechanics survive. |
| C5 | proof-engine.md:2612 | 2612 | holds | "The MVP builds **no live re-checker** and **no checker-gated mint**…" Siblings at soundness-and-coverage.md `:134` (§5b heading), `:137` ("Deferred — not in the MVP"), `:48`, and **`:288`** (was `:287`). |
| C6 | diagnostic-system.md:189 | 189 | holds | The advisory-family classification of `UnsatisfiableGuard`. Pending Q6. |
| C7 | graph-analyzer.md:457 | 457 | holds | "**Severity:** Warning, not error. Dead-end states may be intentional…" Siblings `:271` and `:452` re-verified; `proof-engine.md:2147` re-verified. `diagnostic-system.md:186`/`:191` already state Error as the ruled target and mark the Warning as tracked drift. |
| C5-sib | soundness-and-coverage.md:137 | 137 | holds | See C5. |

### 2.2 Never in the ledger (6 rows)

| # | Site 07-19 | Site at HEAD | Re-verify | Note |
|---|---|---|---|---|
| S14 | spec:258 | 258 | holds | "(§0.7 fault-prevention: count is 'no result outside a declared bound', discharged by an author guard, never deferred to a runtime check)" — propagation site of the fault-gate residue. Re-anchor count containment to rule preservation when S11 is corrected; the "never deferred" clause survives. |
| P3 | philosophy.md:20 | 20 | holds | "**Update**: edit a field directly. The same constraint enforcement applies." Rides on P1. Owner-gated. |
| P4 | philosophy.md:33 | 33 | holds | "…but in either case, the engine enforces it structurally on every operation where it applies." Wording-level; "checked" implies runtime evaluation. Owner-gated, same pass as P1/P2. |
| C4 | proof-engine.md:1731 | 1731 | holds | "There is no deferral to a runtime check (§0.7 prove-or-reject); the runtime count-fault trap is a defense-in-depth backstop, unreachable for contract data." First clause survives; the trap clause goes with the backstop family. |
| C8 | diagnostic-system.md:417 | 417 | holds | "Like every default fold, an unknown/unfoldable default never rejects." Sibling of S15; flips with it. |
| R14 | evaluator.md:1601 | **1654** | moved | "\| Division by zero \| `DivisionByZero` \| Guard `when Divisor != 0` or `nonzero` modifier \|" under "Impossible-Path Failures (Defense-in-Depth)" (heading now at `:1648`). Goes with the backstop family. |

---

## 3. Sweep-coverage gaps

From the canon report's own flags — leg 2 of its closure condition. Both re-verified as still open at HEAD.

- `docs/compiler-and-runtime-design.md` was **not read** by the sweep; spec §0.7:274 delegates the discharge/governance description to it. UNKNOWN whether it carries the sweep/backstop model.
- The `docs/language/*.md` type docs beyond the spec were outside the assigned sweep list.

## 4. Supersession map — copied, unverified

Carried from the packet verbatim, and it stays unverified here:

> The canon report §4 classifies all 62 git-tracked top-level `docs/Working/` docs as superseded (≈28), live-input (≈30), or orthogonal (4), as the worklist for a later owner-approved archive pass. **These 62 classifications were NOT individually verified** — the map is a labeled draft, not confirmed evidence. Two entries interact with this packet's confirmed findings and deserve a check before archiving: `proof-engine-decision-ledger-2026-07-12.md` (live-input, but its ruling-#1 fold-in line :24 is superseded — confirmed) and `compiler-readiness-plan-2026-07-12.md` + architecture companion (live-input pending the want-reconciliation).

Phase 1 slice 1.6 supersedes the archive timing this map assumed: the old folder stays until phase 8 confirms rather than reverts, then moves in bulk.

## 5. `README.md` — checked, no corrections

The canon report found none. The README's guarantee claims (line 8 "every operation enforces the complete logic"; line 104 "no window where an invalid configuration can exist") are mechanism-neutral, and its boundary statement (line 117) points at spec §0.7 — it inherits whatever §0.7 says and needs no independent correction once §0.7 is corrected.

---

## What did not re-verify

Four rows are not byte-identical to what was recorded on 2026-07-19. None was fixed in place.

1. **R8** (`evaluator.md`, restore) — **discharged**. The contradiction it names is gone; the sentence now says restore bypasses constraint evaluation. Phase 2 confirms and strikes it rather than editing.
2. **R2** (`runtime-api.md:361`) — **reworded**. The Fire pipeline sentence was rewritten and gained stages. The contradicting tail is unchanged and still at 361.
3. **R6** (`result-types.md:114`) — **reworded**. The row's third column changed from a stage reference to a mechanism description. The contradicting cell is unchanged.
4. **S15's `proof-engine.md:483` sibling** — **reworded**. Same never-reject posture, different sentence.

Six sites moved without changing: `runtime-api.md` 386→388, 646→648, 889→891; `evaluator.md` 502–505→533–536, 1585→1638, 1601→1654, 1700→1753, 1789→1842; `soundness-and-coverage.md` 287→288; `fault-system.md` 247→248.
