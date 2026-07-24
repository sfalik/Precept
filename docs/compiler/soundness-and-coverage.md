# Soundness and Coverage

## Status

| Property | Value |
|---|---|
| Doc maturity | Design (skeleton) |
| Implementation state | Designed, not yet implemented — filled per implementation slice |
| Source | `src/Precept/Pipeline/ProofEngine.cs`, `src/Precept/Pipeline/ProofLedger.cs`, `src/Precept/Pipeline/NumericInterval.cs` (planned) |
| Upstream | Proof Engine (`proof-engine.md`), Graph Analyzer, Type Checker, Fault system |
| Downstream | Language Server (hover, `precept_proofs`), MCP proof DTOs |

> [!NOTE]
> **This is a skeleton.** Headings and one-line intents are laid down here so each implementation
> slice fills its own section. It is the canonical home for how the prove-or-reject guarantee is made
> sound: the certificate format, the witness, the deferred independent re-checker, and the coverage of
> the known fail-open holes. It cross-cuts the proof engine, graph analyzer, type checker, and fault
> correspondence.

> [!NOTE]
> **One file, two faces — deliberately not split.** This doc carries both the *mechanism* of soundness
> and the *coverage* it delivers, in one file, because they share one object: the four-leg
> certificate-admissibility criterion is simultaneously a scope-gate (§3.3) and a cert-gate (§5). Splitting
> it across two files would force most future edits to touch both. **Part A — Mechanism** and **Part B —
> Coverage & Scope** below each carry their own maturity line.

---

## Part A — Mechanism

> **Maturity — Design (skeleton); mechanism designed, not yet implemented (filled per slice).**
> Part A is *how a single verdict is produced and made sound*: the soundness model and its rationale
> (§1), the three-way verdict (§2), the witness a violating verdict carries (§4), and the certificate
> format (§5). Section numbers are stable cross-reference anchors — external docs cite them (e.g.
> `proof-engine.md` → §5b) — so a section keeps its number even where a Part groups it out of numeric order.

---

## 1. The soundness model — certificate over trust

Precept's compile-time guarantee rests on prove-or-reject: when the compiler cannot prove a computed
value stays inside its declared limit, it **rejects the definition** rather than deferring to a runtime
check. Soundness is delivered two ways, and this doc is the home for both:

- **A legible certificate on every verdict** — the engine shows its work in a form a reader (and, later,
  an independent checker) can replay. Authority rests in the certificate, not in trusting the engine's
  say-so.
- **Direct engine correctness** — because the MVP ships no live re-checker (see §5b), MVP soundness comes
  from fixing the known fail-open holes in the engine directly and from the ⊥ / single-hop rails, not
  from an independent replay catching drift.

*(One-line intent — fill per slice: state the prove-or-reject contract, the "search is free, authority
is the certificate" principle, and how the two mechanisms above compose.)*

### 1.1 Why prove-or-reject — the decision rationale

This subsection is the durable record of *why* the posture is prove-or-reject, so the choice is not
re-litigated. It is the rationale for the mechanism told end-to-end in
[`compiler-and-runtime-design.md`](../compiler-and-runtime-design.md) §1.2 — that keystone states *how*
the chain and runtime cooperate; this states *why* that shape, and not the alternatives. Sourced from the
ratified proof-engine decision ledger.

**Why prove-or-reject, and not a fault-floor-only posture.** The rejected alternative was: accept the
definition, and rely on a runtime fault-floor to refuse the bad value. Three reasons it lost:

1. **Unique value proposition.** Compile-time prevention with a re-checkable certificate is a defensible
   category, not a research toy. A fault-floor-only posture collapses Precept toward "a better validator
   that also governs" — the governance architecture survives, but the *headline* differentiator
   (structural prevention, proven before any entity exists) is relocated to runtime and weakened.
2. **It pushes authors to build better precepts.** The over-rejection worry — the corpus's many unbounded
   money fields — was answered not by softening the posture but by *strengthening the engine* and giving
   the author a legible escape (§6: state the fact as one rule, the engine applies it). The fix for
   "prove-or-reject rejects safe programs" is a stronger prover plus an author-stated rule, never
   abandoning the guarantee.
3. **A computed value has no external ingress to govern.** The governance thesis only coheres for values
   entering from *outside*; a value derived *inside* the definition has no door to check it at. A runtime
   refusal of a computed value satisfies the *discard* half of the fault definition but fails the
   *recoverable-typed-outcome* half — it is a fault wearing a governed refusal's costume, not governance.

**The load-bearing condition (do not drop it).** Prove-or-reject is honest **only as the ratified
package** — over the *strengthened* engine (the new strategies plus the §6 escape) and the certificate
criterion. Re-assert the posture over a weaker engine and it rejects the corpus's own house style. The
posture is locked; its *honesty* is contingent on the strengthened engine landing.

**Why the boundary is proof-carrying-vs-not, never band-vs-rule.** An earlier design drew the line at
"bounded band vs. authored rule" (old Decision D2). That distinction is **dead**: a bound *is* a rule. The
only line that matters is whether an operand *carries a constraint the engine can discharge* — proof-
carrying — or does not. There is no separate class of "soft" bounds that escape the obligation.

**Why there is no author-visible escape hatch.** There is no "trust me" marker that lets an author opt a
site out of the proof. Such a marker is directly corrosive to the guarantee: it reintroduces exactly the
bypassable, uncheckable path that governance-not-validation exists to eliminate. Where the engine cannot
derive a bound, the author's recourse is §6 — state the consequence as *one rule* the engine then applies
— not a bypass. (The `[StaticallyPreventable]` evaluator traps are defense-in-depth for out-of-contract
entry, not an author-facing opt-out.)

**Sources.** Proof-engine decision ledger #1 (prove-or-reject over fault-floor) and #9 (no escape hatch);
the boundary ruling that retired old D2; the prove-or-reject position paper. These Working artifacts are
the ratified reasoning this subsection promotes to canon.

---

## 2. The three-way verdict

*(Intent — fill per slice.)* The `ProofVerdict` discriminated union — `Proven` / `ProvenViolating(witness)`
/ `Unresolved(condition)` — each case carrying only its own evidence. Both non-proven cases are
`Severity.Error` and reject. Cross-reference: `proof-engine.md` §13 (prove-or-reject MVP), and the
severity model in `diagnostic-system.md`.

---

## 4. The witness (§4a)

*(Intent — fill per slice.)* A `ProvenViolating` verdict carries a **witness**: one concrete configuration
that is re-evaluated against every collected fact and actually violates. The mandatory validation gate —
an unvalidated corner demotes to `Unresolved`, never presented as a proven violation. The per-family
witness grid (value witness / configuration witness / category-mismatch N/A) and the point-binding
limits (field leaves in the MVP; arg/element leaves demote to `Unresolved` until case-by-case narrowing
lands).

---

## 5. The certificate

### 5a. Certificate format (MVP prerequisite)

*(Intent — fill per slice.)* The format the MVP ships. A certificate is a small derivation: **premises**
(each quoting an author declaration — a bound, guard, rule, or reject-row — by source span) and **steps**
(each applying one primitive inference from the spec-enumerated `CertificateSteps` catalog, carrying its
own recomputable conclusion). Built during discharge from values the folds already hold — bookkeeping,
not a second analysis pass. Every step renders to one author-readable line. This is the home for the
`CertificateSteps` catalog vocabulary and the rendering contract that feeds hover / `precept_proofs`.

### 5b. Independent re-checker (deferred)

*(Intent — fill when scheduled.)* A second, independent implementation that replays a certificate and
fails loudly if a strategy's proof and its certificate disagree. **Deferred — not in the MVP.** Its value
is builder-side drift defense, not author unblocking; it is a separate, larger build than the format. The
MVP designs the certificate format to be re-checkable *in principle* so this checker is a later addition
rather than a retrofit — there is no checker-gated mint in the MVP (a `Proven` verdict is minted by the
discharge cascade, not by passing a replay first).

---

## Part B — Coverage & Scope

> **Maturity — Commitment map, not an implementation-state claim.** Part B is *the measurable boundary*:
> which sites are and aren't proof obligations (§3), the fail-open holes MVP soundness closes directly
> (§6), and the soundness-critical rails the strategies must not breach (§7). A cell's disposition is a
> ratified design commitment, not a claim the code is live at a given HEAD — see §3's IMPORTANT note.

---

## 3. Coverage — what is and isn't a proof obligation

This section is the **measurable boundary** of the guarantee stated in the keystone narrative
([`compiler-and-runtime-design.md`](../compiler-and-runtime-design.md) §1.2). §1.2 states *that* every
fault-prone site raises an obligation and every unproven obligation rejects; this section states, cell by
cell, *which* sites do and don't — so "does Precept prove X?" has a written answer rather than a debate.

> [!IMPORTANT]
> **This is a commitment map, not an implementation-state claim.** A cell marked **in** commits that the
> obligation is stamped and either discharged or rejects; it does *not* assert the code is live today. Cells
> whose capability is designed but not yet built are tagged **in-scope, not-yet-implemented** — the
> commitment holds, the wiring is pending. Live-emission accounting (which diagnostics actually fire at a
> given HEAD) is the separate conformance track's job, not this map's. The closed axes below — the
> `FaultCode` registry (`src/Precept/Language/FaultCode.cs`) and the `ProofRequirementKind` DU
> (`src/Precept/Language/ProofRequirementKind.cs`) — are closed at those source files, which are the
> source of truth for their member lists; the per-cell dispositions here are the ratified design
> commitment.

**Disposition legend.**

- **in** — an obligation is stamped at the site and is discharged-or-rejects (prove-or-reject applies).
- **explicitly-out** — no proof obligation, *by construction*, with the stated reason (e.g. the fault is
  decided earlier by the type checker / name binder, before the proof engine runs).
- **deferred** — designed to be in, but held, with an explicit re-open trigger.

### 3.1 Coverage by fault mode (`FaultCode` axis)

The `FaultCode` registry (`src/Precept/Language/FaultCode.cs` — the source of truth for the full enrolled
member list) is closed; every member carries `[StaticallyPreventable(DiagnosticCode.X)]`, and every member
is dispositioned below. "Discharging obligation kind" names the `ProofRequirementKind`
(`src/Precept/Language/ProofRequirementKind.cs` — the source of truth for the obligation-kind set) that
discharges it, or the pre-proof stage that owns it.

| `FaultCode` | Preventing `DiagnosticCode` | Discharging obligation / owner | Disposition |
|---|---|---|---|
| `DivisionByZero` | `DivisionByZero` | `Numeric` (divisor ≠ 0) | **in** |
| `SqrtOfNegative` | `SqrtOfNegative` | `Numeric` (operand ≥ 0) | **in** |
| `TypeMismatch` | `TypeMismatch` | Type checker (well-typedness) — proof engine assumes typed input | **explicitly-out** |
| `UndeclaredField` | `UndeclaredField` | Name binder / type checker | **explicitly-out** |
| `UnexpectedNull` | `UnprovedPresenceRequirement` | `Presence` (optional set before access) | **in** — with creation-site holes (see §3.2) |
| `InvalidMemberAccess` | `InvalidMemberAccess` | Type checker (member resolution) | **explicitly-out** |
| `FunctionArityMismatch` | `FunctionArityMismatch` | Type checker (arity) | **explicitly-out** |
| `FunctionArgConstraintViolation` | `FunctionArgConstraintViolation` | *(no per-argument constraint machinery exists)* | **deferred** — owner-fork: remove-scaffolding vs. build. Re-open trigger: owner chooses "build" → routes to `/design` (new language surface). No locked spec grounds it today. |
| `CollectionEmptyOnAccess` | `UnguardedCollectionAccess` | collection non-empty / `IndexBounds` / `KeyPresence` | **in** for indexed/`.at` access; **deferred** for `lookup for K` key-presence reads (obligation never created; re-open trigger: key-presence enrollment work) |
| `CollectionEmptyOnMutation` | `UnguardedCollectionMutation` | collection non-empty (mutation) | **in** |
| `QualifierMismatch` | `QualifierMismatch` | `QualifierCompatibility` / `QualifierChain` / `AssignmentQualifier` / `DimensionalProduct` | **in** *(runtime fault-identity collapse to a single backstop is a separate runtime-only concern — see §3.2)* |
| `NumericOverflow` | `NumericOverflow` | `IntervalContainment` (computed-field bound) | **in-scope, not-yet-implemented** — the arithmetic-overflow (representable-range) lane is GATE-O, post-MVP (see §6). The declared-bound containment obligation is in; the overflow enforcement lane is deferred by build-order. |
| `OutOfRange` | `OutOfRange` | `IntervalContainment` (assignment into declared bounds) | **in** |
| `LengthBoundViolation` | `LengthBoundViolation` | `LengthContainment` | **in** |
| `CountBoundViolation` | `CountBoundViolation` | `CountContainment` | **in** |

> **Note on the `explicitly-out` rows.** `TypeMismatch`, `UndeclaredField`, `InvalidMemberAccess`, and
> `FunctionArityMismatch` are prevented, but *not by the proof engine* — the type checker / name binder
> decides them before the proof engine runs, and the proof engine assumes a well-typed `SemanticIndex`.
> They are `[StaticallyPreventable]` faults with live prevention; they are simply out of the proof
> engine's obligation surface by construction. Listing them keeps the axis exhaustive.

### 3.2 Coverage by obligation-creation site (site-kind axis)

Prove-or-reject requires the obligation to be *created* at every fault-prone site — §1.2's soundness
inversion warning is precisely about creation, not just discharge. This table dispositions the creation
sites. Sites where the obligation is created-and-walked are **in**; sites where it is designed-but-not-yet
created are **deferred (in-scope, not-yet-implemented)** with a re-open/complete trigger.

| Obligation-creation site | Obligation kind(s) | Disposition |
|---|---|---|
| Set-action RHS (assignment into a bounded/typed field) | `IntervalContainment` / `LengthContainment` / `CountContainment` | **in** |
| Computed-field `<-` expression against its own declared bounds | `IntervalContainment` (created whenever the field has bounds, incl. unbounded result → stays `Unresolved`) | **in** |
| Rule / `ensure` `.Condition` expression | per operator/function/accessor `ProofRequirements` | **in** |
| Transition / rule / ensure / access-mode / state-hook / event-row **guard** expressions | value + presence obligations in the guard | **deferred (in-scope, not-yet-implemented)** — guard positions not yet enrolled; obligation never born. Complete trigger: obligation-creation sweep. |
| Member-call **arguments** | presence / value obligations on argument sub-trees | **deferred (in-scope, not-yet-implemented)** — argument sub-tree not walked. Complete trigger: obligation-creation sweep. |
| Field **default-expression** sub-tree | value obligations (div/overflow/sqrt in a default) | **deferred (in-scope, not-yet-implemented)** — default sub-tree not walked. Complete trigger: obligation-creation sweep. |
| Event-argument **default-expression** sub-tree | value obligations | **deferred (in-scope, not-yet-implemented)** — mirrors the field-default hole; shared fix shape. |
| `lookup for K` key-presence reads | `KeyPresence` | **deferred (in-scope, not-yet-implemented)** — obligation never created (fail-open). Complete trigger: key-presence enrollment. |
| Field-reference modifier bound (`min Floor`, `max Ceiling`) | relational rule participating in single-pass narrowing | **deferred (in-scope, not-yet-implemented)** — bound parsed then discarded; enforcement subsumed by the locked relational-rules-and-bounds design (desugars to `Amount >= Floor`). |
| Constraint `.Message` interpolation holes — **value faults** (e.g. `because "bad {10/0}"`) | value obligations (div/overflow/sqrt in a message) | **in-scope, not-yet-implemented** — **owner ruling, 2026-07-23: value-fault expressions in a constraint message enroll and are caught at compile time.** A message renders exactly when its constraint fails, so a faulting expression inside it crashes the engine at the moment it was supposed to explain itself; § 0.7's no-deferral clause applies unchanged. Grounded in the want doc's own worked example, which puts `{-Balance / PlanRepayment.Months}` inside a refusal message and relies on `Months positive` to make it safe. The obligation-discharge matrix already carries this as its fault case shape, with eight `defined` cells across `reject-message-interpolation` and `constraint-rationale-interpolation`. `CollectObligations` walks a constraint's `.Condition` only and never its `.Message`, so nothing is minted today. Complete trigger: obligation-creation sweep, same fix shape as the default-expression rows above. |
| Constraint `.Message` interpolation holes — **presence** (e.g. `because "value {OptionalField}"`) | `Presence` | **in-scope, not-yet-implemented** — **owner ruling, 2026-07-24: refuse uniformly.** An interpolation hole is a *read*; reading an optional at a hole enrolls presence identically to any value position, with no split by string position — a `because`/`reject` message and a `set` RHS obey one rule. The earlier "presence-tolerant rendering" recommendation was refuted (no language-chosen sentinel is sound against the length-interval machinery, and it would revive the silent empty-string coercion deleted at `literal-system.md:145`). The stale claim that enrolling presence "would force authors to guard the field they are reporting on" is corrected: measured demand is ≤11/808 corpus holes and both originally-cited samples do not motivate; an author renders an optional via the shipped `if X is set then X else "…"` conditional (no new surface). Decision record: the locked presence-tolerant-rendering design (2026-07-24). Complete trigger: obligation-creation sweep (shared with the value-fault arm above), after its dependencies close (state-`ensure` and conditional-field presence narrowing, optional event-arg holes). |
| Unary op (numeric negation) | *(no `ProofRequirements` slot on `UnaryOperationMeta`)* | **explicitly-out** — the sole unary fault (MinValue-negation overflow) is subsumed by the enclosing containment site. Re-open trigger: if the ratified overflow model makes a unary op *independently* faultable (verify against the overflow spec), the empty slot must be re-activated, not assumed safe. |

> **Runtime-only residue (not a compile-time coverage gap).** Two known items are runtime-surfacing
> defects, not obligation-coverage holes: (1) assignment-out-of-declared-bounds currently borrows the
> `NumericOverflow` wording rather than surfacing `OutOfRange`'s own template (message precision only —
> prevention holds); (2) the runtime `FaultSiteLink` identity collapses to a single backstop code for
> `OutOfRange` and `QualifierMismatch`. Both re-open when runtime fault-surfacing / carries-proof identity
> is built and the reported code becomes observable. Compile-time prevention is fully live for both.

### 3.3 The admissibility disqualifier — leg 4 is the scope-creep gate

A proposed new proof strategy or obligation must clear the four-leg **certificate-admissibility
criterion** (stated in full in `proof-engine.md` Decision 1, §11, grounded in
`precept-language-spec.md` §0.6): it must be (1) **legible & independently re-checkable** (emits a
certificate from the spec-enumerated `CertificateSteps` catalog); (2) **performant** (stays in the
recompile budget); (3) **right-sized** (matched to Precept's tiny, few-variable, loop-free problems — hard
cap + honest fallback, never unbounded search); and (4) **earns its place** (evidenced value —
expressibility, authoring quality, or genuine fault prevention; nothing speculative).

**Leg 4 is the scope-creep disqualifier.** A proposed strategy or obligation that fails "earns its place"
is scope creep *by definition* — it buys no expressibility, no authoring-quality gain, and no fault
prevention the existing set does not already deliver. This is the principled stopping rule: it is what
bounds proof-obligation growth and makes deferrals (e.g. multi-fact combination `§1b`, see
`proof-engine.md` §13) principled rather than cowardly. The full criterion text is not duplicated here —
see Decision 1.

### 3.4 The bucket test — classifying a new failing case

When a new failing case appears (a definition that faults, or that is wrongly accepted/rejected), classify
it against this coverage map *before* treating it as a bug:

- **(a) Regression in an already-`in` cell** → a **real gap**. The obligation is committed as in-scope and
  should be created-and-discharged; fix it. (Example: an `IntervalContainment` site that stopped
  emitting.)
- **(b) An already-dispositioned `explicitly-out` or `deferred` cell** → **cite the row and close it**. Do
  *not* re-open it unless the row's cited re-open/complete trigger has actually fired. A deferred cell is a
  ratified decision, not an oversight; re-arguing it without the trigger is the re-litigation this map
  exists to stop.
- **(c) Uncataloged surface (no matching cell)** → this is **new language surface, not a bug**. Route it to
  `/design` under the Pre-Design Owner Consultation gate. A fault mode or site that no cell covers is a
  proposal, and hardening it inline would bypass the design gate.

Cross-reference: the guarantee this coverage bounds is stated in
[`compiler-and-runtime-design.md`](../compiler-and-runtime-design.md) §1.2; the rationale for the
prove-or-reject posture itself is §1.1 above.

---

## 6. Soundness-hole coverage (Slice-0 fail-open fixes)

> **Overflow enforcement is GATE-O (post-MVP), by deliberate ruling.** The present-tense claims about
> `integer`/arithmetic overflow behavior in the philosophy and spec state the *target end-state*, not the
> current build state — arithmetic-overflow enforcement is sequenced as GATE-O, after the MVP. This is a
> ratified LEAVE-AS-IS decision (the text describes the intended guarantee; the gap is build-order, not a
> text defect), recorded here so the separate conformance track does not re-report it as documentation
> drift. See §3.1 (the `NumericOverflow` row is tagged *in-scope, not-yet-implemented*).

*(Intent — fill per slice.)* With the independent re-checker deferred (§5b), MVP soundness comes from
fixing the engine directly rather than from replay catching drift. The **tactical Slice-0 fail-open fix
checklist** — the specific found-hole to-dos and the systematic `bool?`/dict-write sweep — is an
implementation to-do list whose entries close (and are removed) as the fixes land, so it lives in a
Working slice doc rather than in canon: [`../Working/soundness-hole-slice0-fixes-2026-07-14.md`](../Working/soundness-hole-slice0-fixes-2026-07-14.md).
When those fixes land, that doc is archived and this section records the closed state in canonical terms.
The canonical commitment §6 holds is the *principle*: under prove-or-reject, a discharge path that cannot
actually prove its input must render `Unresolved` and **reject** — it must never stamp a proved/clean
verdict on an unproven input.

---

## 7. Rails — ⊥ suppression and single-hop

*(Intent — fill per slice.)* The soundness-critical rails the strategies must not breach: the ⊥ (empty
interval) suppression at dict-write time, the kernel ⊥-guard inside the numeric-interval arithmetic
methods, and the single-hop / anti-transitivity rail (the engine reads a cross-field relation exactly
once and never chases a third field — spec §0.6 single-pass, depth-bounded).

---

## Cross-References

- [`../compiler-and-runtime-design.md`](../compiler-and-runtime-design.md) §1 — **the keystone guarantee
  narrative**: what Precept promises and why the guarantee is a *closed net, not a floor* — coverage
  defined as a structural sweep (fails closed, closed under language extension), not an enumerated list of
  bug categories. The net decomposes into three mechanisms (business-process soundness and calculation
  soundness, both proven; constraint governance, enforced at runtime), but that decomposition is the
  machinery, not the guarantee's definition. §1.2 (prove-or-reject: how the chain and runtime cooperate)
  is the mechanism that §1.1 (rationale) here explains and §3 (coverage) here bounds; the closure claim in
  §1 *is* this doc's §3.4 bucket test surfaced into the keystone.
- [`proof-engine.md`](proof-engine.md) — the engine these fixes and the verdict/certificate live in.
- [`diagnostic-system.md`](diagnostic-system.md) — the severity model (three-way verdict + structural
  severity families).
- [`graph-analyzer.md`](graph-analyzer.md) — process-topology diagnostics whose severity the structural
  family covers.
- [`../language/precept-language-spec.md`](../language/precept-language-spec.md) §0.6 — the proof-engine
  design contract and proof philosophy.
