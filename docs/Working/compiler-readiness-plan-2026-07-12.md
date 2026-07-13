---
title: "Compiler Readiness Plan v3 — MVP Proof-Engine Build Plan"
status: Draft — owner-gated
date: 2026-07-12
author: Synthesis (Claude, main loop), assembling the Stage-0 rulings
owner: Shane
supersedes: compiler-readiness-plan-2026-06-16.md (v2)
purpose: >
  The /plan-structured implementation plan for the MVP proof engine — the culminating
  Stage-0 artifact the owner reviews at the 0→1 gate. Every decision this plan rests on
  is already ruled; this page assembles them into a phased, dependency-ordered slice
  list, captures the ruling log so nothing is re-opened, and states the acceptance frame
  and Definition of Done. It supersedes the 2026-06-16 v2 plan. Docs only; no code.
---

# Compiler Readiness Plan v3 — MVP Proof-Engine Build Plan

**What this is.** The build plan for the MVP proof engine, structured per `/plan`: a
phase/slice summary, the captured ruling log, the (near-empty) open-decision list, a
dependency-ordered slice list with **Slice 0 and the next slice at heavyweight detail and
the rest as stubs**, the cross-cutting acceptance frame, the Definition of Done, and the
carried-forward disciplines. It supersedes v2 (`compiler-readiness-plan-2026-06-16.md`).

**Everything here is assembled, not decided.** All the design and owner decisions this
plan rests on are already ruled and recorded in the four companion docs. This plan does
not re-open any of them — it sequences the work that implements them.

### Companion docs (the sources — this plan points, it does not duplicate)

- **`compiler-readiness-plan-2026-07-12-pipeline-evaluation.md`** — the Stage-0a
  disposition ledger (keep / refactor / remove for every pipeline finding; the reconciled
  v2 Phase-1 cleanup; the tooling-propagation and corpus-impact surfaces; the emission (c)
  sequencing ruling).
- **`compiler-readiness-plan-2026-07-12-architecture.md`** — the Stage-0b locked
  proof-engine architecture (the `ProofVerdict` DU; the certificate format §5a; the
  `CertificateSteps` catalog; the witness + 12-family grid; the §1a/§2/§3/§6 cascade
  extensions; the four Slice-0 soundness holes; the per-slice implications table).
- **`compiler-readiness-plan-2026-07-12-structural-severity.md`** — the ruled
  structural-severity family (which diagnostics flip Warning → Error, four-leg rationale,
  0/77 corpus impact, the doc-sync and terminal-detection-regression obligations).
- **`proof-engine-decision-ledger-2026-07-12.md`** — the 10 owner rulings + the §6 MVP
  scope pull-in.

### Terms used in this plan (MVP-only — the v2 glossary is retired)

One line each; the owning companion doc is the canonical definition.

- **Prove-or-reject** — when the compiler cannot *prove* a computed value stays inside its
  declared limit, it rejects the definition; nothing is deferred to a later runtime check.
  (ledger #1)
- **`ProofVerdict` (three-way verdict)** — the discriminated union that replaces the binary
  `ProofDisposition` enum: `Proven` / `ProvenViolating(witness)` / `Unresolved(condition)`,
  each case carrying only its own evidence. (architecture §1.1)
- **Certificate** — the show-your-work note attached to *every* verdict: premises quoting
  the author's own declarations by source span, and steps applying one primitive inference
  each and carrying a recomputable conclusion. Re-checkable by hand (and by the future §5b
  checker) but with **no live re-checker in the MVP**. (architecture §1.2–§1.3)
- **`CertificateSteps` catalog** — the new spec-enumerated, closed vocabulary the certificate
  builder draws its step kinds from (catalog-before-code). (architecture Decision C1)
- **Witness** — one concrete set of values that provably breaks a rule, shown on a
  `ProvenViolating` verdict, and only after being concretely re-evaluated and confirmed to
  violate (unvalidated ⇒ demote to `Unresolved`). (architecture §1.4)
- **⊥ (bottom) suppression** — the empty interval must be dropped, never intersected or
  unioned, because `NumericInterval.Contains(⊥)` returns `true`; an unsuppressed ⊥ over-proves
  every obligation on the field. (architecture §1.6, §1.8, §1.5)
- **Single-hop / anti-transitivity** — the engine reads a cross-field relation exactly once
  and never chases a third field through it; going two-hop is §1b, deferred. (ledger #3,
  architecture §1.6)
- **Fail-open hole** — a discharge path that returns a proved/clean verdict on an input it
  has not actually proven; under prove-or-reject these must reject. Four are named for
  Slice 0. (architecture §2.1)
- **Corpus / `# EXPECT:` reconciliation** — the per-slice obligation to update the 77
  `.precept` samples and the diagnostic-sample `# EXPECT:` contracts whose strings/outcomes a
  slice changes, in the same commit. (pipeline-eval §6)

---

## 1. Phase / slice summary table

The MVP is one phase — turn prove-or-reject on without rejecting the corpus's own house
style — delivered in dependency-ordered slices. Slice 0 is the hard prerequisite; the rest
are largely independent (see the sequencing note in §7).

| Slice | Goal | Decisions required | Effort | Status |
|---|---|---|---|---|
| **0 — certificate + three-way verdict + soundness fixes** | Lay down the `ProofVerdict` DU, the certificate format, the `CertificateSteps` catalog; close the 4 fail-open holes + run the sweep; retire `DiagnosticStage` + 2 retired codes; sync tooling | **ruled** — architecture §1.1–§1.3, §1.9, §2; Decisions A/B/C; pipeline-eval §9(c) | **L** | Ready (heavyweight below) |
| **§3 money** | Prove bounded money/quantity arithmetic through × and ÷ via catalog `IntervalTransfer` wiring + one kernel ⊥-guard | **ruled** — architecture §1.5 | **S–M** | Ready (next-slice detail below) |
| **§2 case-by-case** | Conditional-arm + event-input narrowing on the corrected Slice-0 base; the ⊥-arm skip rail + dict-write ⊥ rail | **ruled** — architecture §1.8 | **S–M** | Stub |
| **§4a witness** | The witness-validation gate + the 12-family grid + witness rendering in authored units | **ruled** — architecture §1.4 | **S–M** | Stub |
| **§1a single-fact** | Multi-term linear fact + syntactic multiset matcher; memberwise staleness; single-hop preserved | **ruled** — architecture §1.7 | **L** (biggest MVP build) | Stub |
| **§6 constant-rule → bounds** | Teach `IntervalContainment` the field-vs-constant rule shape (reuse fold + ⊥ suppression + family enumeration); single-hop rail | **ruled** — ledger #6-scope, architecture §1.6 | **M** | Stub |
| **structural-severity** | Flip the ruled Warning → Error severities (process-topology + uninhabitability) + doc-sync + terminal-detection regression test | **ruled** — structural-severity doc | **S** | Stub |

All "decisions required" cells read **ruled** — this is the assembling-not-deciding plan.
Effort bands are relative (S ≈ ~1–2 focused slices; M ≈ a substantial slice; L ≈ the
largest builds — Slice 0's breaking DU reshape + 4 holes, and §1a's greenfield fact core).

---

## 2. Decisions captured — the ruling log (do not re-open)

Every ruling this plan implements, with a one-line statement and a pointer to where it is
recorded. This is the "already decided" section; a resurfacing question is answered by
finding its row and following the pointer, never by re-deriving it.

### The 10 ledger rulings (`proof-engine-decision-ledger-2026-07-12.md`)

| # | Ruling (one line) | Recorded |
|---|---|---|
| 1 | **Prove-or-reject RATIFIED** (option A) — the compiler rejects what it cannot prove fault-safe; ratified as a package with #2 and the MVP engine. | ledger #1 |
| 2 | **Certificate criterion ADOPTED** — replaces the solver-ban with a four-part admissibility test (legible & re-checkable / performant / right-sized / earns-its-place). | ledger #2 |
| 3 | **§1b DEFERRED** (option C) — not in the MVP, not scheduled; `spec:256` two-hop override **NOT authorized**. | ledger #3 |
| 4 | **Aggregates → `/design`** — admit `sum`/`min`/`max`/`average`, custom folds stay out; sequences after the MVP core; owes a Tier-2 pre-design conversation. | ledger #4 |
| 5 | **Compute-then-check DEFERRED** — parked, not closed; computed fields are not a full substitute for the event-arg-derived guard-value case. | ledger #5 |
| 6 | **spec:223 rescope ADOPTED** — "proven violations only" scoped to violation *reports*; bounded-write checks become three-way containment obligations. | ledger #6 |
| 7 | **Overflow text LEFT AS-IS** — the present-tense prevention claims correctly state the target; overflow prevention is post-MVP (GATE-O). No `philosophy.md` edit. | ledger #7 |
| 8 | **"No bugs" — tighten one clause** — keep the "No errors. No bugs." lede; tighten only "business logic cannot produce a wrong answer"; exact wording drafted for owner approval before applying. | ledger #8 |
| 9 | **Escape hatch CLOSED** — no author-visible "trust me" marker; it would corrode the prove-or-reject value proposition. | ledger #9 |
| 10 | **Dead-row severity = ERROR** — a provably-always-violating write on a reachable row is #6's `ProvenViolating` bucket and blocks. | ledger #10 |
| §6-scope | **§6 pulled into the MVP** — the philosophy-aligned multi-fact fix (author states the consequence as a rule, engine applies it). | ledger §6-scope note |

### The architecture rulings (`compiler-readiness-plan-2026-07-12-architecture.md`)

| Decision | Ruling (one line) | Recorded |
|---|---|---|
| **A — verdict representation** | **The `ProofVerdict` DU** — three cases, each carrying only its own evidence; not an enum + nullable evidence fields. | architecture §1.1, Decision A |
| **B — independent re-checker** | **RETRACTED — not an MVP decision.** The MVP ships the §5a certificate *format* only: no live re-checker, no checker-gated mint. §5b (the checker) is a later phase. | architecture §1.3, Decision B |
| **C — step vocabulary source** | **C1 — a spec-enumerated `CertificateSteps` catalog** (catalog-before-code), not a reuse of the `ProofStrategy` enum. *Source only — the catalog's membership is unruled, owing the §5a certificate-format design pass.* | architecture §1.2, Decision C |
| **F5 — dead-end disposition** | **dead-end = Error** — makes relabeling the dead-end→dead-end suppression outcome-neutral. | architecture §1.9; structural-severity doc |

### The structural-severity family (`compiler-readiness-plan-2026-07-12-structural-severity.md`)

- **Flip Warning → Error (process-topology, `philosophy.md:51` "proven impossible"):**
  `UnreachableState`, `StructuralSinkState`, `DeadEndState`,
  `RequiredStateDoesNotDominateTerminal`.
- **Flip Warning → Error (uninhabitability, "a definition no valid entity can inhabit →
  Error"):** `UnsatisfiableRule` (PRE0159), `ContradictoryRule` (PRE0155). Scoped override of
  `diagnostic-system.md:180`.
- **Stay Warning:** `UnhandledEvent`, `AlwaysRejecting` (transition rows), `StateAlwaysRejects`,
  `FieldNeverSet`, `VacuousRule`, `TautologicalGuard`, `UnsatisfiableGuard`.
- **Already Error (no change):** `UnsatisfiableInitialState`, `DefaultViolatesRule`,
  `NoInitialState`, `TerminalStateHasOutgoingEdges`, `IrreversibleStateHasBackEdge`.

### The emission-ownership sequencing ruling (`...-pipeline-evaluation.md`)

- **Emission refactor (c) — interleave.** Retire `DiagnosticStage` and remove the two
  "retired — pending removal" allow-list codes **as part of the three-way-verdict slice**
  (Slice 0). The entanglement probe found zero analyzer coupling and a three-file footprint,
  so it rides along with no separate-refactor risk. (pipeline-eval §3, §9)

---

## 3. Open decisions (near-empty — genuine remainders only)

Everything load-bearing is ruled (§2). What remains are deferrals and one already-closed
item, each with its re-open trigger. **None of these blocks the MVP.**

| Item | State | Re-open trigger |
|---|---|---|
| **§1b — multi-fact combination** | **Deferred** (ledger #3, option C); `spec:256` override not authorized | A *recurring* real definition where the derived bound genuinely resists being named as an intermediate field **and** authors hand-derive the composite rule wrong often enough to bite in practice. |
| **Aggregates (#4)** — `sum`/`min`/`max`/`average` | **Routed to `/design`**, sequenced after the MVP core | The `/design` pass may start once the MVP core lands; owes a **Tier-2 pre-design conversation** first (new language surface). |
| **Compute-then-check (#5)** | **Parked** (not closed) | Duplicated guard/set/reject-complement formulas prove a real authoring-drift pain post-MVP. One unresolved design fork to settle then (handler-local `let`-style binding vs. first-class "post-operation value of field F"). |
| **Presence / KeyPresence in-MVP configuration-witness cell** | **Deferred to a post-MVP cell** (default); the three-way verdict + certificate ship now, the diagnostic already names the unguarded subject | Owner wants the configuration-witness (path/scenario) in the MVP. It needs new non-interval machinery — the one grid cell the reuse and soundness lenses split on (low stakes). |
| **§5b — independent re-checker** | **Deferred** (Decision B) — a later phase | Builder-side drift-defense value is wanted post-MVP; it is Large (a second checking implementation), not a gate. |
| **UnsatisfiableRule / ContradictoryRule family** | **RULED (Error) — closed, not open.** Listed here only to record that this once-borderline call is settled and does not remain as a decision. | — (ruled; see §2 structural-severity) |

Later runtime/canon gates carried from the ledger's reconciliation (GATE-I fault delivery,
GATE-J spec self-contradictions, GATE-O overflow, and the untouched GATE-B/C/D/E/K/L/N) are
**post-MVP** and resolve in their own initiatives; none is an MVP-blocking decision. They are
catalogued in `proof-engine-decision-ledger-2026-07-12.md` ("Old-plan gates") — this plan
does not carry them as open MVP items.

---

## 4. The slice list (dependency order)

Slice 0 is heavyweight; **§3 money** (the next slice) carries elevated detail; the remaining
slices are stubs (the `/plan` "expand N+1 only" rule — each is expanded to Slice-0 depth by
`/lifecycle-4-execute` when it becomes the current slice).

---

### Slice 0 — certificate format + `ProofVerdict` DU + `CertificateSteps` catalog + the 4 soundness-hole fixes + emission retirement + tooling sync

**Foundational prerequisite. Every other MVP slice depends on the verdict/certificate shape
this slice lays down.** Effort: **L** (a breaking ledger-surface reshape + four failing-test-first
fixes + the systematic sweep + tooling migration).

**Grounded in:** architecture §1.1 (DU), §1.2–§1.3 (certificate format), Decision C1
(`CertificateSteps` catalog), §1.9 (the Literal two-arm fix), §2.1–§2.2 (the four holes + the
sweep); pipeline-eval §3/§9 (emission retirement) and §5 (tooling propagation).

#### Steps (with file pointers)

1. **Introduce the `ProofVerdict` DU.** Replace the binary `ProofDisposition { Proved = 1,
   Unresolved = 2 }` (`ProofLedger.cs:94–98`) and its smeared nullable siblings
   (`Strategy?`, `EmittedDiagnostic?`, `ComputedInterval?`, `ProofLedger.cs:24–32`) with a DU
   base + three sealed cases: `Proven` (certificate only), `ProvenViolating(witness)`
   (certificate + validated witness, Error per #10), `Unresolved(condition)` (certificate +
   printed missing condition). Illegal payload combinations must be structurally
   unrepresentable (no "witness on a Proven verdict"). The three-valued distinctions the
   cascade already computes feed the split (`TryLengthContainmentProof`'s `bool?`,
   `ProofEngine.cs:1093–1100`; a computed interval fully outside its band).

2. **Add the `CertificateSteps` catalog + the certificate record.** New spec-enumerated,
   closed step vocabulary (catalog-before-code — add the catalog entry and list it in the
   canonical catalog inventory, `docs/language/catalog-system.md`). The certificate record
   carries **premises** (each quoting an author declaration by source span — reuse
   `AuthoredMin/Max`, `ProofRequirement.cs:184–197`) and **steps** (each one primitive
   inference from the catalog, carrying a recomputable conclusion). Built **during discharge
   from values the folds already hold** — bookkeeping, not a second pass. **Format only — no
   live re-checker, no checker-gated mint** (Decision B): a `Proven` verdict is minted by the
   cascade, not by passing a replay.

3. **Fix the `Literal // vacuously proved` mislabel — two arms, two honest fixes**
   (architecture §1.9):
   - Unreachable-from-state arm (`ProofEngine.cs:~1178–1184`) → an honest `Proven` rendered
     "never runs — nothing to prove", with the graph reachability fact as its certificate
     premise (cites the produced `StateGraph`). Never labeled "Literal" or "vacuously proved".
   - Dead-end→dead-end arm (`ProofEngine.cs:~1186–1196`) → `Unresolved` with a plain
     certificate; only the diagnostic emission stays suppressed. Outcome-neutral now that
     `DeadEndState` = Error (structural-severity slice) already rejects the compile.

4. **Close the four fail-open holes — each failing-test-first** (architecture §2.1):
   - **Hole 1 — undecidable-magnitude default bound** (`ProofEngine.cs:1060–1066`):
     `TryNumericDefaultBoundProof`'s `null` (undecidable) currently falls through to
     `(Proved, Literal)`. Fix: `null ⇒ Unresolved` with the residual ("the default's magnitude
     could not be statically evaluated against bound `max …`").
   - **Hole 2 — arg/field name collision** (`ProofEngine.Intervals.cs:559`): the guard-branch
     narrowing loop keys on bare `gc.Field` with no arg-vs-field discriminator, so a guard on
     an event arg narrows a same-named field's interval (a live false `Proved`). Fix: an
     explicit arg/field discriminator at the narrowing site. (§2's event-input work later
     *adds* sound arg narrowing on top of this corrected base.)
   - **Hole 3 — sequential staleness ignored** (`BuildNarrowedIntervals`,
     `ProofEngine.Intervals.cs:519`): it never consults `obligation.ReassignedBefore`, unlike
     every other narrowing path — a stale guard fact still narrows the read interval (a live
     false `Proved`). Fix: drop/refuse any fact whose subject is in `ReassignedBefore`.
   - **Hole 4 — dead-end→dead-end stamped proved** (§1.9 / `Diagnostics.cs:726`): stop stamping
     false `Proved`; render the honest `Unresolved`. Outcome-neutral under dead-end = Error.

5. **Run the systematic fail-open + dict-write sweep** (architecture §2.2, F6): audit **(a)**
   every `bool?`-returning discharge path (does `null` route to reject, never to proved?) and
   **(b)** every dict-write site into the narrowed-interval dictionary (does it guard ⊥ before
   storing?). The four named holes are the found instances, not the closed set; each additional
   hole gets the same failing-test-first treatment.

6. **Retire `DiagnosticStage` + remove the two retired codes** (emission (c), pipeline-eval
   §3/§9): remove the `DiagnosticStage` enum (`Diagnostic.cs:11`) — a pure component label with
   no severity semantics, zero `Precept.Analyzers` usage, three `src/Precept` references — as
   part of this three-way-verdict slice; reshape the `Diagnostics.cs` `GetMeta` severity rows;
   remove the two "retired — pending removal" allow-list codes
   (`DiagnosticCoverageAllowLists.cs:30,37`) and their enum members + meta rows.

7. **Sync the tooling surface** (pipeline-eval §5 — MCP Tool Sync is non-negotiable): the
   verdict/certificate shape crosses the tooling boundary via `Compilation.Proof` +
   `Compilation.Diagnostics` (`Pipeline/Compilation.cs:12–13`). Update the DTO
   (`tools/Precept.Mcp/Dtos/CompileToolDtos.cs:14`, `Severity` → three-way verdict), the
   consumers (`tools/Precept.Mcp/Tools/CompileTool.cs`,
   `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs`), and the tooling docs
   (`docs/tooling/mcp.md`, `docs/tooling/language-server.md`) in the same commit.

#### Exit criteria (all full-compile-assertable)

- The `ProofVerdict` DU is live end-to-end; the `ProofDisposition` enum and its nullable
  evidence siblings are gone; illegal payload combinations do not compile.
- Every verdict carries a certificate; `precept_proofs` / hover render premises (span-cited)
  and steps (recomputable conclusions). No live re-checker exists; no verdict is checker-gated.
- Each of the four holes has a **failing-test-first** regression test asserting a
  previously-clean-but-unproven definition now rejects as `Unresolved` (Holes 1–3) / that the
  dead-end row is honest `Unresolved` (Hole 4). Verified with `Compiler.Compile(...)`, never
  the type-checker-only `Check`.
- The fail-open sweep is recorded: every `bool?` discharge path routes `null → reject`; every
  dict-write site guards ⊥. Additional holes found are fixed the same way.
- `DiagnosticStage` and the two retired codes are removed; `dotnet build` is green (the Roslyn
  emission-ownership analyzers, incl. `Precept0002FaultCodeMustHaveStaticallyPreventable`,
  still pass).
- MCP DTO/formatter + hover carry the new shape; tooling docs updated.
- Corpus green after reconciliation (samples + `# EXPECT:` contracts + affected fixtures
  migrated to the DU shape in the same slice).

#### Doc-update obligations (same commit)

- `docs/compiler/proof-engine.md` — the verdict model (three-way DU), the certificate format
  (§5a, format-only, re-checkable in principle), and the honest verdicts for the two Literal
  arms; correct any stage-6 "Full/Implemented" status overclaim (pipeline-eval §7).
- `docs/compiler/diagnostic-system.md` — the `DiagnosticStage` retirement and the three-way
  verdict; retire the two removed codes from the registry.
- `docs/language/catalog-system.md` + spec — the new `CertificateSteps` catalog entry
  (inventory is the source of truth).
- The Stage-0c foundational-doc `soundness-and-coverage.md` (linked at `README.md:37`) — must
  be authored to the **certificate model**, explicitly rewriting the deleted
  "verify-don't-trust / trusted checker" framing it inherits (do-not-resurrect; pipeline-eval
  §7).
- `docs/tooling/mcp.md`, `docs/tooling/language-server.md` — the verdict/certificate surface.

---

### §3 money — bounded money/quantity arithmetic through × and ÷ (the next slice)

**The one true "author cannot clear it by hand at all today."** Effort: **S–M**. Grounded in
architecture §1.5.

**Goal.** Prove that money/quantity/price arithmetic stays inside declared bounds by attaching
the existing `IntervalTransfer` machinery to the **whole** op-meta family via catalog wiring —
**zero engine code** — plus one kernel ⊥-guard.

**Key steps + file pointers.**
- Attach `IntervalTransfer` to the whole money/quantity/price op-meta family
  (`Operations.cs:437–559`, enumerated not tripped-over: `MoneyTimesDecimal`,
  `MoneyDivideDecimal`, `MoneyDivideMoneySameCurrency`/`CrossCurrency`,
  `MoneyDivideQuantity/Price/Period/Duration`, and the quantity siblings), reusing the delegate
  slot (`Operation.cs:108`), the shared `Multiply/Divide/Add/SubtractTransfer` helpers
  (`Operations.cs:1332–1349`), the four-corner `NumericInterval.Multiply/Divide`
  (`NumericInterval.cs:85–102`), and the generic dispatch (`Intervals.cs:102–112`).
- **Omit** any transfer whose result-space is unverified (the price-space `USD/h × h` cells,
  period/duration magnitude space) — no transfer = Unbounded = conservative reject, never
  over-prove.
- **The one kernel guard** (the single place the MVP touches the interval kernel, F4): add an
  `IsEmpty ⇒ Unbounded` (or propagate-Empty) guard **inside** the `NumericInterval` methods so
  no call site can forget, covering `Multiply/Divide/Add/Subtract/**Scale**` (the `Empty =
  [0,-1]` sentinel, `NumericInterval.cs:29`, sails past both the `IsEmpty` backstop
  `Intervals.cs:509` and the divisor-spans-zero guard `:97`, and `Scale` `:110` guards only
  `IsUnbounded`).
- Certificate: emit per-op `Arithmetic` steps (e.g. `[0..900] + [0..100] = [0..1000]`).

**Exit criteria.** Bounded money product/quotient sites prove; every ⊥-in-arithmetic vector
(`Empty × x`, `Empty ÷ x`, `Scale(Empty)`) has a failing-test-first row; omitted transfer cells
conservatively reject (no over-prove); corpus green after `# EXPECT:` reconciliation; MCP/hover
carry the arithmetic-step certificate.

**Owned docs.** `docs/language/business-domain-types.md` (money/quantity arithmetic proof
surface), `docs/compiler/proof-engine.md` (the `IntervalTransfer` money family + kernel guard),
the `Operations.cs` catalog entries.

---

### Stub — §2 case-by-case

**Goal.** Extend the guard-branch DNF union to conditional-expression arms + event-input
narrowing (adds sound arg narrowing on the Slice-0 Hole-2 base); install the ⊥-arm skip rail +
the dict-write ⊥ rail (F7, at dict-write time uniformly), and the `CaseSplit` certificate step.
**Effort:** S–M. **Owned docs:** `docs/compiler/proof-engine.md`.

### Stub — §4a witness

**Goal.** The witness-validation gate (concrete re-evaluation at mint; unvalidated ⇒
`Unresolved`; k ≤ 6 corner cap) + the 12-family grid (value / configuration / N-A per family) +
witness rendering in authored units; carry the F8 caveats (field-leaf-only point-binding, the
length-witness interval plumbing). **Effort:** S–M. **Owned docs:** `docs/compiler/proof-engine.md`,
`docs/compiler/diagnostic-system.md` (rejection message shape).

### Stub — §1a single-fact

**Goal.** The multi-term linear-term fact (coefficient/term pairs + operator + bound) + a
**syntactic** term-multiset matcher (commutative/associative normalization only, no algebra) at
the containment and flow-narrowing seams; memberwise staleness; **single-hop preserved** (never
splits `A+B<=100` into per-field bounds). **Effort:** L (the biggest MVP build). **Owned docs:**
`docs/compiler/proof-engine.md`, spec fact-shape wording (multi-term).

### Stub — §6 constant-rule → bounds

**Goal.** Teach `IntervalContainment` the field-vs-constant rule shape it already honors
relationally: a new reuse fold in `BuildNarrowedIntervals` (reuse `CollectTrustedNumericFacts`
parse + `NarrowByConstraint` apply) with **mandatory ⊥ suppression** (copy the relational fold's
empty-intersection suppression verbatim), the blocked-rule/self-unsatisfiability discipline, the
full family enumeration (`>=`/`>`/`<`/`<=`/min-max × rules-vs-ensures × arg-subject × mixed), and
the **single-hop rail** (`ExtractFieldInterval` must not learn to fold constant rules).
**Effort:** M. **Owned docs:** `docs/compiler/proof-engine.md`.

### Stub — structural-severity

**Goal.** Apply the ruled Warning → Error flips in `src/Precept/Language/Diagnostics.cs`
(process-topology: `UnreachableState:707`, `StructuralSinkState:720`, `DeadEndState:726`,
`RequiredStateDoesNotDominateTerminal:747`; uninhabitability: `ContradictoryRule:802`,
`UnsatisfiableRule:814`), leaving the ruled-Warning rows unchanged; add the terminal-detection
regression test (a declared-`terminal` state is never flagged `DeadEndState`/`StructuralSinkState`);
sync the remaining docs. **Effort:** S (0/77 corpus impact — no `.precept` edits). **Docs already
reconciled (Stage 0c, docs-before-code — verify in sync, no edit owed):** `docs/compiler/diagnostic-system.md`
(the two-severity split + the corrected "per-instance severity exists" claim) and
`docs/language/precept-language-spec.md` §0.6 item 7 (uninhabitability now "rejects the definition",
`:209`). **Owned docs (Stage-1 remaining):** `docs/compiler/graph-analyzer.md` (the OQ1 override to the
`terminal`-declares-intent reconciliation), `docs/language/precept-language-spec.md:186/189` (the
process-topology severity naming), and any severity column in `docs/compiler/README.md`. Full obligation
list: `compiler-readiness-plan-2026-07-12-structural-severity.md` §6.

---

## 5. Acceptance frame (cross-cutting — every slice inherits it)

Per-slice test matrices are written **in each slice**, not here. What every slice inherits:

- **Full-compile assertions only.** Every proof-stage assertion drives the whole pipeline —
  `Compiler.Compile(...).Diagnostics` / `CompileExpectingError` — **never** the type-checker-only
  `Check` / `CheckExpectingClean` helpers, which skip the proof engine and produce false greens
  on exactly the diagnostics these slices touch (PRE0078/0114/0141/0135, cancellation, narrowing).
- **Diagnostic samples with `# EXPECT:` contracts.** New/changed diagnostics get sample files
  under `test/integrationtests/diagnostics/*.precept` carrying `# EXPECT:` contracts, drift-guarded
  by `DiagnosticSampleDriftTests`. A slice that changes a diagnostic string or outcome updates the
  affected samples + contracts in the same commit (corpus/`# EXPECT:` reconciliation, pipeline-eval
  §6).
- **The cross-cutting invariant cells** — every proof slice's matrix must include, as explicit
  failing-test-first rows:
  - **⊥-suppression** — no path lets `Contains(⊥) ⇒ true` over-prove (the ⊥ vector for the fold
    or rail the slice touches).
  - **Single-hop / anti-transitivity** — the slice never chases a third field through a cross-field
    relation (the two-hop case stays undischarged; that is §1b, deferred).
  - **Certificate re-checkability-in-principle** — every verdict the slice mints carries premises
    that cite the author's declarations by span and steps that carry recomputable conclusions (a
    reader, or the future §5b checker, can replay it without trusting the engine).

---

## 6. Definition of Done (MVP-shippable)

The MVP ships when **all** hold:

1. **All six capabilities green via full-compile:** §5a certificate + three-way verdict (Slice 0),
   §3 money, §2 case-by-case, §4a witness, §1a single-fact, §6 constant-rule — each with its
   own passing full-compile test matrix.
2. **The three-way verdict is live end-to-end, including tooling** — `ProofVerdict` DU across the
   engine, the MCP DTO/formatter, and hover; `precept_proofs` renders the certificate.
3. **The four Slice-0 soundness holes are closed** (each with a failing-test-first regression),
   and the systematic fail-open + dict-write sweep is complete with every additional hole it
   turned up fixed the same way.
4. **The corpus compiles in budget** — all 77 samples green, and the full corpus compiles within
   the performance budget measured via `tools/Precept.Bench` (the ~1–3 ms single-file class against
   the ~50 ms debounce budget; the admissibility criterion #2 leg 2).
5. **Docs match** — every slice's doc-update obligations landed in-commit; no "Implemented"
   overclaim survives against the shipped engine; the `CertificateSteps` catalog is inventoried;
   the `soundness-and-coverage.md` foundational doc is written to the certificate model.
6. **The ruled structural-severity flips landed** with **0-regression on the corpus** (0/77 trip
   any flipped code), including the terminal-detection regression test.
7. **Out of scope stays out** — §1b (multi-fact), aggregates (#4), and compute-then-check (#5)
   are **not** built; each remains recorded in §3 with its re-open trigger.

---

## 7. Cross-cutting disciplines (carried from v2)

- **Register → disposition ledger — nothing dropped silently.** Every finding in the three
  companion docs has an explicit disposition (built in a slice / deferred with a pointer / keep).
  This plan does not silently drop any; a slice that discovers a new finding records its
  disposition rather than absorbing it.
- **Capture-pointers for deferrals.** Every deferred item (§3 open decisions) names its re-open
  trigger; a defer with no trigger is disallowed.
- **Per-slice doc-sync in the same commit.** Each slice updates its owned docs (and the tooling
  DTO/formatter + `docs/tooling/*` where a verdict/witness/certificate shape crosses the boundary,
  pipeline-eval §5) in the same commit as the code — never as a later sweep.
- **Slice-boundary review pauses.** Pause and report after each slice; do not auto-advance, even
  under auto-mode. Adversarial review of the slice diff before commit (the `/lifecycle-4-execute`
  cadence).
- **Sequencing note.** **Slice 0 is the hard prerequisite** — the verdict/certificate shape it
  lays down is a dependency of every other slice, and its DU reshape + soundness fixes must land
  before any capability slice builds on them. After Slice 0, the capability slices (§3, §2, §4a,
  §1a, §6) and the structural-severity slice are **largely independent** and can be sequenced by
  benefit-per-cost; the phases-doc lean is §3 → §2 → §4a → §1a, with §6 and structural-severity
  slotting where convenient (structural-severity has 0/77 corpus impact and can land any time after
  Slice 0's dead-end=Error dependency is available). §4a's value-witness rows degrade to
  `Unresolved` on arg/element leaves until §2 lands the arg narrowing (architecture §1.4 F8) — a
  graceful dependency, not a hard ordering.
