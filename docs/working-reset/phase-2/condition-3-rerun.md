# Condition 3, re-run

**What this is.** Phase 2's third finish-line condition says the superseded wording no longer appears
in the canonical tree, and tests it by running each register row's recorded grep pattern over
`docs/language`, `docs/compiler`, `docs/runtime` and `docs/philosophy.md` — plus `README.md`,
`docs/compiler-and-runtime-design.md` and `docs/tooling` for the rows marked **WIDE**. It was last
established at an older tree. Since then `c9193ec8` removed the runtime fault-backstop model across
seventeen canonical documents and `3fb04266` followed it, so patterns written before that commit
could fail on sentences that commit rewrote while the corrections behind them stand. This is the
re-run that tells those two apart.

| Field | Value |
|---|---|
| Holds as of | `3a561dade42f58978a504a73087c419b7b9e2342` (`3a561dad`), branch `spike/Precept-V2-Radical-reset` |
| Run on | 2026-07-28 |
| Tree state | Clean at the start of the run; the only edits made are to this file, the register's patterns, and `search-procedure.md`. **No canonical document was touched.** |
| Verdict | **Condition 3 holds at this commit.** No row blocks it. |
| Genuine regressions found | **None.** See § 1 — this is a measured result, not an unexamined one. |
| Patterns rewritten | 6 — `R9`, `N26`, `C1`, `C2`, `N29`, `C6` |

---

## 1. Regressions

**None found, and the search for them was the point of the run.** Every pattern that did not behave
as its row expected was opened, the canonical text read at HEAD and at the row's executing commit,
and the cause identified. In every case the correction had landed and the pattern was what had gone
wrong. Nothing was undone; nothing failed to land.

The distinction a reader needs: this is *none found after looking at all of them*, not *not looked
for*. Nine rows were investigated by reading — six because a deletion pattern returned more than
zero, three because a pattern's direction could not be read off the count alone. Their canonical
text is quoted in § 4.

**What this run cannot tell you** is whether any correction was done *wrong*. Condition 3 is
self-refereed: the person who writes a correction writes the pattern that proves the old wording
gone. It catches a forgotten file. Reading the surviving text around each investigated hit is a
partial defence and no more.

---

## 2. What was changed in the search procedure's variables

The task this run was given expected `$CANON_WIDE` in `search-procedure.md` to be missing
`docs/tooling`. **It is not.** At `3a561dad` the file reads:

```bash
CANON="docs/language docs/compiler docs/runtime docs/philosophy.md"
CANON_WIDE="docs/language docs/compiler docs/runtime docs/philosophy.md README.md docs/compiler-and-runtime-design.md docs/tooling"
```

`$CANON` is the plan's own four-path set and is correct as the plan's set. `$CANON_WIDE` carries
`docs/tooling`, and so does the file-resolver in that document's § 7. The fifth pass added the folder
to both. **Neither variable needed changing, and neither was changed.**

**The defect is real; it is in the other copy of the definition.**
`contradiction-register.md` § How to read a row spelled **WIDE** out longhand as `$CANON` plus
`README.md docs/compiler-and-runtime-design.md`, with no `docs/tooling` — and that is the copy a
re-runner working row by row reads, because it sits next to the rows. Rows `N103` and `N104` correct
`docs/tooling/language-server.md` and `docs/tooling/mcp.md`. Run under that sentence as it stood,
both patterns return zero because the folder is not in the file set: zero for the wrong reason,
recorded as a pass.

Two changes, both outside the canonical tree:

- **`contradiction-register.md` § How to read a row** — `docs/tooling` added to the longhand **WIDE**
  set, with a note saying which copy drifted and why it mattered.
- **`search-procedure.md` § 1** — a paragraph recording that both variables were checked at this
  commit and are correct, that the register's copy was the one that had drifted, and that it has been
  corrected there.

`N103` and `N104` were then re-run with the folder in the set, against `$CANON_WIDE` as their rows
already specify. Both return **0**, and the folder was confirmed to be genuinely in the searched set
by a positive control — a term present only in `docs/tooling/` returns hits under `$CANON_WIDE`. So
their zeros are honest.

---

## 3. Every row's pattern at `3a561dad`

Direction column: **0** means the row is a deletion row and its pattern must return zero. **>0** means
an addition or confirm row, whose pattern must return more than zero. **survives** means a re-scope
or read-by-reading row where the count alone decides nothing and § 4 carries the reading. **n/a**
means the row records no runnable pattern.

### 3.1 The post-mutation constraint sweep (§ 1)

| Row | Pattern (abbreviated) | Direction | Count | Verdict |
|---|---|---|---|---|
| S1 | `the sweep governs` | 0 | 0 | pass |
| S2 | `If any constraint fails, the working copy is discarded` | 0 | 0 | pass |
| S3 | `If all constraints pass, the working copy is promoted` | 0 | 0 | pass |
| S4 | `check rules and ensures` | 0 | 0 | pass |
| S5 | `checked against the complete working copy *after* all mutations` | 0 | 0 | pass |
| S6 | `Rules and ensures are evaluated exhaustively` | 0 | 0 | pass |
| S7 | `ConstraintsFailed` filtered to the post-mutation reading | 0 | 0 | pass (re-scope) |
| S9 | `discarded if any constraint fails` | 0 | 0 | pass |
| N1 | `Every applicable rule and ensure is evaluated against the working copy` | 0 | 0 | pass |
| N2 | `before any constraint is evaluated` | 0 | 0 | pass |
| N3 | `Collect-all vs first-match` | survives | 2 | pass — § 4.7 |
| N83 | `rests on the structural fact that the value carries a declared constraint` | 0 | 0 | pass |
| N84 | located by reading | n/a | — | — |
| N85 | `constraint evaluation` in §3A.6, by reading | survives | 2 in file | pass — § 4.8 |
| N86 | `same guards, same ensures, same constraint checking` | 0 | 0 | pass |
| R1 | `constraint evaluation found violations` | 0 | 0 | pass |
| N4 | `all post-mutation constraints passed` | 0 | 0 | pass |
| N5 | `same guards, same ensures, same constraint checking` | 0 | 0 | pass |
| R2 | `constraint evaluation (collect-all) → commit or discard` | 0 | 0 | pass |
| R3 | `patch application to working copy → computed field recomputation → constraint evaluation` | 0 | 0 | pass |
| R4 | `same action chain, same constraint checking` | 0 | 0 | pass |
| R6 | `Post-mutation constraints violated` | 0 | 0 | pass |
| R7a | `Run constraint plans against the working copy` | 0 | 0 | pass |
| R7b | `AFTER action execution and computed field recomputation` | 0 | 0 | pass |
| N6 | `Collect all violations; if any → row fails` | 0 | 0 | pass |
| N7 | ``evaluate each constraint's `ExecutionPlan` against slot array`` | 0 | 0 | pass |
| N8 | `against the post-mutation working copy` | 0 | 0 | pass, with a bookkeeping note — § 4.6 |
| N9 | `Collect-all applies at ingress only` | >0 | 1 | pass (re-scope) |
| N10 | `ConstraintPlanIndex` | reduced, not zero | 19 | open row, waiting on Shane — § 4.9 |
| N88 | `plan execution and constraint evaluation` | 0 | 0 | pass |
| N89 | `evaluate constraints from prebuilt buckets` | 0 | 0 | pass |
| N90 | `walks execution plans and evaluates constraints` | 0 | 0 | pass |
| N91 | § 1.0 family test | see below | 35 | pass — § 3.9 |
| N92 | § 1.0 family test | see below | 35 | pass — § 3.9 |
| N11 | `discarded whole if any constraint fails` (WIDE) | 0 | 0 | pass |
| N12 | `so no invalid configuration commits` (WIDE) | 0 | 0 | pass |
| N13 | `on a working copy discarded on failure` (WIDE) | 0 | 0 | pass |
| N14 | `Constraint plans evaluated` (WIDE) | 0 | 0 | pass |
| N15 | three `*ConstraintsFailed` names, repeated `-e` (WIDE) | 0 | 0 | pass |
| N16 | `the next operation's post-mutation sweep will` (WIDE) | 0 | 0 | pass |
| N17 | `re-governed by the next operation's post-mutation sweep` (WIDE) | 0 | 0 | pass |
| N87 | `on a working copy discarded whole if it fails` (WIDE) | 0 | 0 | pass |
| N102 | the marked disputed scope in `compiler-and-runtime-design.md` | >0 | 1 | pass (addition) |
| N93 | `Available; whether the anchor shape survives is open` | >0 | 1 | pass (addition) |
| N97 | `constraint re-?check` | 0 | 0 | pass |
| N98 | covered by `N97` | 0 | 0 | pass |

### 3.2 Restore is trusted (§ 2)

| Row | Pattern (abbreviated) | Direction | Count | Verdict |
|---|---|---|---|---|
| S8 | `Restoring an entity in an invalid state is not allowed` | 0 | 0 | pass |
| S10 | `the next operation through the contract re-governs it` | 0 | 0 | pass |
| R5a | `re-governed by the next operation's post-mutation constraint sweep` | 0 | 0 | pass |
| R5b | `whose post-mutation constraint sweep` | 0 | 0 | pass |
| R8 | `bypasses both access-mode checks and` | >0 | 3 | pass (confirm) |
| N18 | `Persisted data is never trusted` (WIDE) | 0 | 0 | pass |
| N19 | `RestoreConstraintsFailed` | 0 | 0 | pass |
| N20 | `RestoreConstraintsFailed` (WIDE) | 0 | 0 | pass |
| N21 | `Future migration logic runs before constraint evaluation` | 0 | 0 | pass |

### 3.3 Runtime fault traps and defence in depth (§ 3)

| Row | Pattern (abbreviated) | Direction | Count | Verdict |
|---|---|---|---|---|
| R9 | `FaultSiteAnnotation` | 0 | **2** | **pattern rot — rewritten**, 0 after — § 4.1 |
| N22 | `Fault Backstop Pass` | 0 | 0 | pass |
| N23 | `FaultBackstops` | 0 | 0 | pass |
| R10 | `Fault backstop routing` | 0 | 0 | pass |
| N24 | `CC#6` | 0 | 0 | pass |
| N25 | `Impossible-Path Failures (Defense-in-Depth)` | 0 | 0 | pass |
| R14 | `Impossible-Path Failures` | 0 | 0 | pass |
| N26 | `FaultSiteDescriptor` | 0 | **5** | **pattern rot — rewritten**, 0 after — § 4.2 |
| R11 | `Fail(FaultCode.DivisionByZero)` | 0 | 0 | pass |
| R12 | `bypassed compile-time checking` | 0 | 0 | pass |
| N27 | `Defense-in-depth, not a primary error path` | 0 | 0 | pass |
| R13 | `LengthBoundViolation,` in `fault-system.md` — a *retention* check | 1 | 1 | pass |
| N28 | `fault codes exist at evaluation time as a backstop` | 0 | 0 | pass |
| N29 | `errors the type checker should have prevented` | survives | **0** | **pattern rot — rewritten**, 5 after — § 4.3 |
| C1 | `FaultSiteLink` | 0 | **3** | **pattern rot — rewritten**, 0 after — § 4.1 |
| C2 | `belt-and-suspenders` | 0 | **1** | **never usable — rewritten**, 0 after — § 4.4 |
| C4 | `the runtime count-fault trap` | 0 | 0 | pass |
| N30 | `defense-in-depth` in `proof-engine.md` | 0 | 0 | pass |
| N31 | `defensive redundancy` | 0 | 0 | pass |
| N32 | `Runtime fault — integer overflow` | 0 | 0 | pass |
| N33 | `defensive redundancy for paths the compiler has already proven unreachable` | 0 | 0 | pass |
| N34 | `evaluator traps are defense-in-depth` | 0 | 0 | pass |
| N35 | `conservative backstop for proof-only obligation families` | 0 | 0 | pass |
| N36 | `defense-in-depth` in `compiler-and-runtime-design.md` | 0 | 0 | pass |

### 3.4 Declared-bound containment (§ 4)

| Row | Pattern (abbreviated) | Direction | Count | Verdict |
|---|---|---|---|---|
| S11 | `no result outside a declared bound` | 0 | 0 | pass |
| S12 | `constraint range impossibility` | 0 | 0 | pass |
| S13 | `Runtime fault traps exist only as defensive redundancy` | 0 | 0 | pass |
| S14 | `count is "no result outside a declared bound"` | 0 | 0 | pass |
| SC-1 | `carries a constraint the engine can discharge` | 0 | 0 | pass |
| N37 | `division, overflow, non-negative, out-of-range, bounded write` | 0 | 0 | pass |
| N38 | `declared-bound value-safety codes` | 0 | 0 | pass |
| N104 | `declared-bound value-safety codes` (WIDE, `docs/tooling`) | 0 | 0 | pass — folder confirmed in the set, § 2 |
| N39 | `bound containment are *illustrations*` (WIDE) | 0 | 0 | pass |
| N40 | the containment sentence not read as a fault gate | 0 | 0 | pass (re-scope) |

### 3.5 Overflow deferral (§ 5)

| Row | Pattern (abbreviated) | Direction | Count | Verdict |
|---|---|---|---|---|
| N41 | the fault list with no deferral disclosure on it | 0 | 0 | pass (re-scope) |
| N42 | `Overflow is where this does not hold today` | >0 | 1 | pass (re-scope) |
| N42 | `no division by zero, no overflow` outside `philosophy.md` | exactly 1 | 1 | pass |
| N43 | overflow stated as an error with no disclosure near it | 0 | 0 | pass (re-scope) |
| N101 | `checked overflow` / `overflow is checked` | 0 | 0 | pass |
| N103 | `The runtime check is a fallback, not a guarantee` (WIDE, `docs/tooling`) | 0 | 0 | pass — folder confirmed in the set, § 2 |
| N44 | confirm by reading | n/a | — | confirmed — § 4.10 |
| P5, P6 | excluded by owner ruling, not rows | n/a | — | — |

### 3.6 Structural defects (§ 6)

| Row | Pattern (abbreviated) | Direction | Count | Verdict |
|---|---|---|---|---|
| C7 | `Warning, not error` | 0 | 0 | pass |
| N45 | `DeadEndState` named at Warning with code 108 | 0 | 0 | pass |
| N46 | `Diagnostic(Warning, Graph, "UnreachableState"` | 0 | 0 | pass |
| C6 | `UnsatisfiableGuard` in `diagnostic-system.md` | survives | **3** | **never usable — rewritten**, 1 after — § 4.5 |
| N47 | confirm by reading | n/a | — | confirmed — § 4.10 |
| N48 | ``stay `Warning` per §0.6 items 7/8`` | 0 | 0 | pass |
| N49 | confirm by reading | n/a | — | confirmed — § 4.10 |
| N50 | confirm by reading | n/a | — | confirmed — § 4.10 |
| N94 | `provably dead declaration` | 0 | 0 | pass |

### 3.7 Induction, solvers, provenance, certificates, defaults (§§ 7–11)

| Row | Pattern (abbreviated) | Direction | Count | Verdict |
|---|---|---|---|---|
| N51 | `No Inductive Proof` | 0 | 0 | pass |
| N52 | `inductive step` / `proof by induction` / `by induction` / `base case` | >0 | 12 | pass (addition) |
| N53 | `premise class` | >0 | 2 | pass (addition) |
| N54 | `every write site of every field` | >0 | 2 | pass (addition) |
| N55 | `proven for data that arrived … assumed for data that did not` | >0 | 1 | pass (addition) — § 4.11 |
| N56 | `SMT integration` | 0 | 0 | pass |
| N57 | any bare strategy numeral in the two documents | 3 legitimate | 3 | pass — § 4.12 |
| N58 | `four strategies only` (WIDE) | 0 | 0 | pass |
| N99 | `Strategy 6` in `proof-engine.md`, only Dimensional Product | 3 | 3 | pass — § 4.12 |
| N100 | `the same strategies differently from 6 onward` | >0 | 1 | pass (disclosure) |
| N59 | `Non-linear rules are proven from a premise that states the condition directly` | >0 | 1 | pass (addition) |
| N60 | `is decided by provenance` (WIDE) | 0 | 0 | pass |
| N61 | `split only by where the value comes from` (WIDE) | 0 | 0 | pass |
| N62, N63, N64 | confirm by reading | n/a | — | confirmed — § 4.10 |
| N65 | `carries a sufficient constraint` | 0 | 0 | pass |
| C5 | `no live re-checker` | 0 | 0 | pass |
| N66 | routed to phases 4 and 5 | n/a | — | still open, § 5a of `soundness-and-coverage.md` still tagged *(Intent — fill per slice.)* at `:143` |
| N67 | `CertificateSteps` | >0 | 7 | pass (confirm) |
| N68 | `certificat` in `docs/runtime` | >0 | 20 | pass (addition) |
| N69 | `derived from the proof` | >0 | 1 | pass (addition) |
| N70 | `proven from these premises` | — | 0 | row not executed by design; open behind `N79` |
| N71 | `author-visible` in `diagnostic-system.md` | >0 | 1 | pass (addition) |
| S15 | `never rejects` | 0 | 0 | pass |
| C8 | `an unknown/unfoldable default never rejects` | 0 | 0 | pass |
| N72 | ``folds to `null` and never rejects`` | 0 | 0 | pass |
| N73 | `suppresses the rule-vs-default fold entirely` | 0 | 0 | pass |

### 3.8 Canon contradicting canon (§ 12), and the deferred rows (§ 13)

| Row | Pattern (abbreviated) | Direction | Count | Verdict |
|---|---|---|---|---|
| N74 | the `(none)` row of the temporal classification table | 0 | 0 | pass |
| C3 | `§3A.4 swept prior state` | 0 | 0 | pass |
| N75 | ``.count ≥ 1` on collections` | 0 | 0 | pass |
| N76, N77, N78 | cross-references to `N18`, `N56`, `S8` | n/a | — | carried by their targets, all pass |
| N95 | `precept-language-spec.md:1992` | 0 | 0 | pass |
| N79, N80, N81, N82, N96 | routed to the owner; no pattern | n/a | — | still open |
| P1–P4 | philosophy rows; no pattern column | n/a | — | executed in `20021deb` / `5e77865a` |

### 3.9 The § 1.0 family test

Cited by `S6`, `R7a`, `N7`, `N14`, `N89`, `N90`, `N91` and `N92` as a second test alongside their own
patterns. Run over `$CANON_WIDE` at `3a561dad` it returns **35** lines. The register records **33**
at `3dbad40b`.

All 35 were read. Every one falls into a group the register's § 1.0 triage already names — denial of
the removed model (`runtime-api.md:153`, `:178`, `:252`, `:277`; `result-types.md:119`;
`precept-language-spec.md:1927`, `:1965`; `evaluator.md:603`, `:717`, `:786`, `:1682`, `:1752`,
`:2037`; `precept-builder.md:798`; `proof-engine.md:2172`, `:2183`, `:2595`;
`compiler-and-runtime-design.md:125`, `:129`), ingress correctly scoped (`evaluator.md:141`,
`:2131`), the `ConstraintResult` arrays that ride on `N79` (`result-types.md:175`, `:212`, `:250`;
`runtime-api.md:416`; `evaluator.md:387`, `:411`), compile-time attribution rather than a runtime
pass (`descriptor-types.md:109`, `:176`, `:181`; `runtime/README.md:54`), a verbatim quotation from a
deleted document (`precept-language-spec.md:2035`), and unrelated vocabulary
(`collection-types.md:1505`, `compiler-and-runtime-design.md:766`, `philosophy.md:42`).

**Nothing asserts the removed model.** The count moved from 33 to 35 because `c9193ec8` wrote *more*
denials of it, not fewer — `proof-engine.md:2172` and `:2183` are the corrected form of the rows
`N97` and `N98` deleted. `runtime/README.md:54` is new to the enumeration and belongs to the
compile-time attribution group; it is added here so a third run can reproduce this triage.

The § 3 `A2` backstop sweep was run as a cross-check over the same file set. Its fourteen hits are
the code-residue disclosures of § 4.1 and § 4.2, `type-checker.md:1133` (§ 4.4), `mcp.md:110`
(defence in depth against bugs in the MCP tool wrappers — a tooling concern, not the removed runtime
model), `compiler-and-runtime-design.md:714` (a deployment safety net for contract-digest diffing),
and four denials. No backstop of the removed kind stands.

---

## 4. Every row that did not behave as its row expected

### 4.1 `R9` and `C1` — pattern rot. The identifiers survive only where canon records that the code still declares them

`R9`'s `FaultSiteAnnotation` returns **2**; `C1`'s `FaultSiteLink` returns **3**. Both are bare
identifiers, and both went from a large number to a small one across `c9193ec8` — 25 → 2 and 34 → 3.

The register's own § 3 preamble says what the survivors are, in the tree these patterns were re-run
against:

> What remains is in the code: `FaultSiteDescriptor`, `FaultSiteLink`, `FaultSiteAnnotation` and the
> `ProofLedger.FaultSiteLinks` field are still declared, the documents now say so, and the removal is
> phase 11's.

The three surviving lines are exactly those statements. `precept-builder.md:965`:

> `src/Precept/Pipeline/ProofLedger.cs` — `ConstraintInfluenceEntry` — builder input. The file also
> declares `FaultSiteLink` and `FaultSiteAnnotation`, and the `ProofLedger` record still carries a
> `FaultSiteLinks` field; no builder pass reads any of the three, and their removal has not been made

`proof-engine.md:272` says the same at length and adds the reason the field's contents commit to
nothing: "an obligation the engine cannot discharge refuses the definition, so a link to what would
have failed at evaluation time has nothing to point at." `proof-engine.md:8` is the
implementation-state row that points at it.

So the correction landed and then deliberately wrote the identifier back, in a disclosure about
un-removed code. A pattern that is the bare identifier cannot distinguish the two uses. **Rewritten**
to exclude any line naming the source file the residue lives in — `ProofLedger.cs` for `R9`,
`ProofLedger` for `C1`. Both return **0** at HEAD and **25** at `c9193ec8^`.

### 4.2 `N26` — the same, for `FaultSiteDescriptor`

Returns **5**: `descriptor-types.md:8` and `:206`, `precept-builder.md:957`, `evaluator.md:2025` and
`:2161`. Each names `src/Precept/Runtime/Descriptors.cs` and says the record is declared there,
described by no document, and on its way out. `descriptor-types.md:206` carries the reasoning:

> That record is the runtime residue of an obligation the compiler could not discharge, and there is
> no such residue: an obligation that cannot be discharged refuses the definition, so nothing is left
> to plant. It is still declared in code and its removal has not been made.

**Rewritten** to exclude any line naming `Descriptors.cs`. **22** at `c9193ec8^`, **0** at HEAD.

### 4.3 `N29` — pattern rot in the opposite direction: a re-scope row whose text was rewritten

`N29`'s resolution is not deletion. It is "keep the `Faulted` variant, re-scope its population," and
its pattern quoted the pre-correction sentence with the note "expect the sentence to survive in
re-scoped form." It returns **0**.

The variant survives. What did not survive is the wording. At `c9193ec8^`,
`result-types.md:72` read "Faults — errors the type checker should have prevented, such as
referencing an unknown field — surface as the single `EventOutcome.Faulted(Fault)` variant." At HEAD
it reads:

> A breach of one of the engine's own invariants — a defect in the compiler, the builder or the
> evaluator — surfaces as the single `EventOutcome.Faulted(Fault)` variant rather than as a thrown
> exception.

That is precisely the re-scoping the row asked for: away from statically-preventable fault sites,
onto engine-invariant breaches. The row is correct and its pattern measured the wrong thing.

**Rewritten** as a re-scope pattern that asserts the aligned claim is present:
`breach of one of the engine's own invariants` in `result-types.md` must return more than zero —
**0** at `c9193ec8^`, **5** at HEAD.

### 4.4 `C2` — the pattern was never usable at the scope it was recorded at

`belt-and-suspenders` over `$CANON` returns **1**, and it returned **2** at `c9193ec8^`. The second
hit was never this row's:

> `docs/compiler/type-checker.md:1133` — Forward-reference prohibition belt-and-suspenders validation

That line has stood since `004eede1`, long before phase 2 opened, and has nothing to do with fault
backstops. `C2`'s own site is in `proof-engine.md`, and the sentence it deletes — "backstops are
defense-in-depth … They exist for belt-and-suspenders safety" at `:2457` — is gone. The pattern was
scoped across the whole canonical set while the row corrects one document, so it could never reach
zero however well the correction went. This is the defect the register already corrected for `N36`.

**Rewritten** scoped to the file: `belt-and-suspenders` in `docs/compiler/proof-engine.md` — **1** at
`c9193ec8^`, **0** at HEAD.

### 4.5 `C6` — a bare identifier on a re-scope row tests nothing in either direction

`UnsatisfiableGuard` in `diagnostic-system.md` returns **3**. The row's resolution moves the code from
the reporting family to the blocking family, so the identifier was always going to stay; a zero would
have meant the code had been deleted, which nobody asked for.

The move landed. `diagnostic-system.md:189` now sits under "Two sub-families **block**
(`Severity.Error`)":

> *A declared path that can never be taken*: `UnsatisfiableGuard` (PRE0082) — a guard provably false
> under the declared constraints. The row it guards can never execute, so a path the author wrote
> into the lifecycle silently never happens … The test is not whether the entity is still inhabitable
> but whether something the author declared can never occur; a guard comparing a nought-to-one ratio
> against `75` instead of `0.75` is the shape this catches.

The report paragraph at `:191` no longer names it and keeps `VacuousRule` and `TautologicalGuard`,
which is the split `C6` specified. The other two hits are the tracked-drift list at `:193` and the
code table at `:324`.

**Rewritten** to assert the block-family placement: the heading phrase together with the identifier
must return more than zero — **0** at `d32d8e26^`, **1** at HEAD.

### 4.6 `N8` — the pattern went to zero on a row the register records as not executed

`against the post-mutation working copy` returns **0**, and the commit column says "Not executed."
Read together those look like a contradiction. They are not.

`N8` has two halves. The first — that the removed model must stop being the framing of the open
question — was carried out by `c3a9be21`, which is where the phrase disappeared (`git log -S`
confirms it). `result-types.md:215` now reads:

> OQ-4 used to ask whether it should move inside `TransitionInspection`, evaluated per row against
> that row's post-mutation state; that option describes a pipeline the language no longer has. … What
> the array should *carry* now that execution has no constraint-evaluation phase is the open question
> recorded below.

The second half — what inspection reports instead — is genuinely open and is `N79`'s. The commit cell
is describing that half. **No rewrite.** The pattern is a deletion pattern and behaved correctly; the
cell is conservative about a row that is partly done. Worth knowing when reading the register, not
worth changing.

### 4.7 `N3` — a survives row, correctly

`Collect-all vs first-match` returns **2**: the heading at `precept-language-spec.md:1923` and a
cross-reference to it at `:2027`. The row says in its own grep cell to expect the heading to survive
and to verify the re-scoping by reading. The section's first category now reads "Validation runs at
ingress, on the values an operation brings in from outside — event arguments, construction inputs,
and the field v[alue]…" (`:1927`), which is the re-scoping `S6` and `N3` asked for.

### 4.8 `N85` — verified by reading

`constraint evaluation` appears twice in the specification, at `:1965` and `:2035`. Neither is the
§3A.6 inspection preview this row corrected. `:1965` denies the outcome can arise ("there is no
post-mutation constraint evaluation to fail"); `:2035` reproduces the deleted `ArchitectureDesign.md`
verbatim as history, which the register's § 1.0 triage already accounts for and which cannot be
edited without falsifying the quotation.

### 4.9 `N10` — an open row, and its count moved

`ConstraintPlanIndex` returns **19**, against **17** at `c9193ec8^`. The row's own instruction is to
expect a reduced but non-zero count and to record the target in the executing commit; it has no
executing commit, because it is routed to the owner as `N80` — whether the index survives in reduced
form or is replaced by a certificate-derived premise list.

The rise of two is not a regression. `c9193ec8` rewrote `precept-builder.md` § 12 and
`proof-engine.md`'s index sections to say what the index is *not*, and naming a structure in order to
narrow it costs occurrences. Nothing new asserts per-operation constraint evaluation — the § 1.0
family test over the same files is clean (§ 3.9). But **`N10` remains the one row whose pattern
cannot be turned into a test until `N80` is decided**, and this run does not change that.

### 4.10 The confirm-by-reading rows

Seven rows record no runnable pattern and are verified by reading. All were re-read at `3a561dad` and
all hold:

- `N44` — `soundness-and-coverage.md:311`–`:313` still states that the present-tense overflow claims
  in the philosophy and the specification are the target end state and that enforcement is sequenced
  after the MVP.
- `N47` — `precept-language-spec.md:209` still says an uninhabitable definition means the compiler
  "rejects the definition."
- `N49` — `diagnostic-system.md:193` still names the block-versus-report split as the ruled severity
  model and the `Severity.Warning` codes in `Diagnostics.cs` as tracked drift.
- `N50` — `graph-analyzer.md:558` still emits `FieldNeverSet` at Warning, which is what the row
  predicted.
- `N62`, `N63` — the composition seam survives, and "a computed value has no external ingress to
  govern" survives scoped to computed values at `soundness-and-coverage.md:88` and
  `compiler-and-runtime-design.md:175`, not as a general division of labour.
- `N64` — superseded in place by `N83`, which records the reword.

### 4.11 `N55` — the rewrite from the fifth pass held

`N55`'s pattern was rewritten twice, most recently on the fifth pass, to drop the clause that kept
being reworded and keep only "*proven* for data that arrived … *assumed* for data that did not." It
returns **1** at `3a561dad`, surviving `c9193ec8` untouched. This is the working defence the plan
names, measured: the two earlier spellings of this pattern would both have failed here.

### 4.12 `N57` and `N99` — line numbers moved, patterns held

`N57` returns **3**, matching the three legitimate survivors its cell enumerates, though two have
moved: the estimated-lines-of-code row is now `:143`, the disclosure note `:150`, and the third is
`:2561` ("all five requirement kinds"), which the cell recorded at `:2648`. `N99` returns **3**, all
Dimensional Product, now at `:459`, `:1428` and `:1435` rather than `:505`, `:1474` and `:1481`.
Both patterns are written against words rather than positions and survived the movement; the line
numbers in their cells are stale and are left alone, because correcting a line number in a cell whose
pattern already finds the line adds a thing to rot.

---

## 5. Does condition 3 hold?

**Yes, as of `3a561dade42f58978a504a73087c419b7b9e2342`.**

Every deletion row's pattern returns zero. Every addition and confirm row's pattern returns more than
zero. Every re-scope row's positive test passes and its removed reading is absent. The three rows
routed to the owner (`N10`/`N80`, `N70`/`N79`, `N100`) carry no deletion pattern to fail, and the
register says so on each of them rather than claiming a pass they have not earned.

**No row blocks it.** Six patterns had stopped testing what they were written to test, and they were
rewritten rather than reported as failures, because in each case the canonical text at HEAD carries
the correction the row asked for. Every rewrite was checked against the tree its correction landed on
top of, so none of the six is a pattern that returns zero in both trees.

**And it holds as of a commit, not permanently.** The next commit to any canonical document
invalidates this line. Six of the patterns in this register have now had to be rewritten because a
later pass reworded a sentence an earlier pattern quoted, and this run is the third time that has
happened. The defence that keeps working is the one the plan states: quote the least changeable words
that carry the claim. The defence that does not work is running the patterns only for the rows a pass
touched.
