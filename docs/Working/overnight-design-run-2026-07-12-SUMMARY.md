# Overnight Design Run — Morning Review Index (2026-07-12)

Status: Review index — nothing here is locked, committed, or applied to canonical docs.

## What ran overnight

Two stages:

1. **Slice-0 re-review** — an adversarial pass over the settled certificate vocabulary doc (`docs/Working/certificate-steps-membership-2026-07-12.md`). Verdict: **lockable after fixes**. Six findings, all confirmed against source (one high-severity completeness hole, two medium, three low). Each finding carries a specified fix; the fixes are enumerated in the doc's review record — the doc is not lockable until they land. See the ranked list below for the one finding whose fix involves an owner choice.
2. **Six slice designs**, each taken through design + adversarial review with all findings verified at source and fixed in place. All six landed **design-complete with holes found and fixed**. No canonical docs were touched; all proposed canonical corrections are recorded as proposals awaiting your sign-off.

**Review path:** the Slice-0 doc plus six new slice-design docs, all in `docs/Working/`:

- `docs/Working/certificate-steps-membership-2026-07-12.md` (Slice 0, re-reviewed)
- `docs/Working/slice-design-money-2026-07-12.md`
- `docs/Working/slice-design-constant-rule-2026-07-12.md`
- `docs/Working/slice-design-case-by-case-2026-07-12.md`
- `docs/Working/slice-design-witness-2026-07-12.md`
- `docs/Working/slice-design-single-fact-2026-07-12.md`
- `docs/Working/slice-design-structural-severity-2026-07-12.md`

## Per-slice status

| Slice | Doc | Status | Review outcome (one line) |
|---|---|---|---|
| Slice 0 — certificate vocabulary | `docs/Working/certificate-steps-membership-2026-07-12.md` | lockable-after-fixes | 6 confirmed findings: type-implied modifiers are an uncovered evidence source (high); 3 modifier/dimension sub-arms mis-mapped to DirectMatch; fact records lack the source spans premises require; plus 3 coverage-table corrections. Fixes specified, must land before lock. |
| §3 money — bounded ×/÷ arithmetic | `docs/Working/slice-design-money-2026-07-12.md` | holes-found-and-fixed | 3 findings verified and fixed: five unary Negate cells added to the disposition table (parked under OQ1), PRE0078 range claim restated honestly, dead unreachable proof helper slated for deletion so the soundness argument has no dormant counterexample. |
| §6 constant-rule → bounds | `docs/Working/slice-design-constant-rule-2026-07-12.md` | holes-found-and-fixed | 7 findings verified, 6 fixed in-doc: hooks now decline ALL facts (staleness tracking can't cross the row-to-hook boundary), family table completed to all five constraint kinds, fold order pinned, sign/interval divergence priced into OQ1. |
| §2 case-by-case | `docs/Working/slice-design-case-by-case-2026-07-12.md` | holes-found-and-fixed | 6 findings verified and fixed, including a **live false-Proven hole re-confirmed by probe** (lossy negation of compound reject guards — "Hole 5"); else-negation now requires lossless extraction; a settled-text overclaim replaced by a two-reading owner question. |
| §4a witness | `docs/Working/slice-design-witness-2026-07-12.md` | holes-found-and-fixed | 6 findings verified and fixed: corner-fallback and certificate-linkage gaps re-surfaced as owner-gated proposed deltas instead of silent reliance; witness construction tightened (joint intersection, point-result requirement); acceptance criteria corrected to real triggers. |
| §1a single-fact multi-term proof | `docs/Working/slice-design-single-fact-2026-07-12.md` | holes-found-and-fixed | 8 findings verified and fixed: both certificate seams now need an explicit owner disposition (OQ1 widened), moved-left-form rule consolidated with a worked trace and in-ceiling argument, circularity and magnitude-space arguments verified at file:line, live-compiled sample added. |
| Structural-severity flip | `docs/Working/slice-design-structural-severity-2026-07-12.md` | holes-found-and-fixed | 6 findings verified and confirmed: full empirical run-under-flip enumeration (37 test breakages total, repair pattern included), a direction-reversed sequencing sentence found in the active plan (de-garble proposed), snippet corpora pulled into scope with a new compile gate. |

## Ranked open questions (most owner-important first)

New-language-surface parks and spec conflicts head the list, per the run's contract: **new surface was parked, not settled** — every item below waits on you.

### 1. §1a — Certificate step shape for the multi-term match (NEW PUBLIC SURFACE — the headline park)

- **Question:** Both seams of the new multi-term proof path need a certificate disposition: the containment seam's match+intersection inference AND the flow seam's multiset discharge (the settled RelationImplication recompute rule is fixed at two-field sides, so an unwidened emission would fail its own recompute rule). Also: does a payload widening of an existing kind count against the certificate doc's growth budget, or only a new kind?
- **Area:** `docs/Working/certificate-steps-membership-2026-07-12.md` (Decision 4 membership = 21, Decision 6 growth accounting, Falsifier 2); architecture §1.1 universal-certificate rule.
- **Why you:** Certificate step kinds are public, spec-owned vocabulary; the settled Slice-0 doc assigns exactly this ruling to the owner. Until ruled, no Proven verdict mints through the new path at either seam.
- **Options:** (a) widen both existing kinds' payloads (no new member; two public recompute-rule changes); (b) one new step kind serving both seams (membership 21 → 22, the single budgeted growth); (c) mixed — one new kind plus one widening.

### 2. §1a — spec:256 lock reading (POTENTIAL SPEC CONFLICT / Tier-3 trigger)

- **Question:** The locked relational-reasoning paragraph describes only the single-pair fact shape. Is widening it to "linear multi-term single fact" (depth bound untouched) a routine wording widening — or is the lock shape-restricting, making this a Tier-3 override conversation before any of this slice proceeds?
- **Area:** `docs/language/precept-language-spec.md:256` (quoted verbatim in the slice doc) and :207 (item 2 capability mandate).
- **Why you:** A locked spec paragraph cannot be overridden inside a design pass. The design proposes the wording-widening read but did not apply it.
- **Options:** confirm the wording-widening read (edit lands at the canonical-correction step); or read the lock as shape-restricting → full Tier-3 conversation first.

### 3. §1a — Recognition breadth: Extensions A and B (SPEC CONFLICT: internal tension in architecture §1.7)

- **Question:** The ruled matcher ceiling says "commutative/associative normalization only — no algebraic rearrangement," but the same section's synthesis note invites a move-all-terms-left pass. Which governs? Separately: literal-coefficient terms (Extension B) widen proof power beyond the ruled example. The design also reads the flow seam's INTERNAL moved-left form for two-sided facts as inside the ceiling (no constant crosses the comparison) — flagged for your confirmation; if you read the ceiling otherwise, the whole-site match reopens.
- **Area:** `compiler-readiness-plan-2026-07-12-architecture.md` §1.7 (both sentences quoted in the slice doc).
- **Why you:** The canonical doc contains a locked ceiling and a synthesis note in tension; only you can say which governs.
- **Options:** rule Extension A in / out; rule Extension B in / out; defer both pending corpus evidence. And separately: confirm or reject the in-ceiling reading of the internal moved-left form.

### 4. §2 case-by-case — proof-engine.md Strategy 8 soundness claim refuted by live probe (SPEC CONFLICT + live fail-open)

- **Question:** `proof-engine.md:1707` claims Strategy 8 is "sound under all numeric domains" and that multi-leaf reject conjunctions are safely forfeited — refuted by a live probe: a compound reject guard with an unextractable conjunct negates only the extracted leaf and compiles a reachable violation as Proved (Hole 5, false-Proven, confirmed). Recorded as unintended drift; correction proposed, not applied. Companion routing question: who owns the failing-test regressions for Hole 5 and F7 — Slice 0's sweep or §2?
- **Area:** `docs/compiler/proof-engine.md:1707`; architecture §2.1/§2.3 vs plan slice board :88/:341.
- **Why you:** A canonical soundness claim is wrong (doc correction is proposal-gated), and a regression test must not be double-owed or fall between slices.
- **Options:** approve the doc correction as proposed or adjust it; route the regressions to Slice 0 (with §2 refactoring onto the uniform helper) or to §2 failing-test-first.

### 5. §2 case-by-case — evidence form for conditional-expression arm assumptions (SPEC CONFLICT with settled Slice-0 text)

- **Question:** The settled certificate Decision 7 says an arm assumption is "either a cited premise's condition or its negation" — but a conditional-expression arm assumption under §2 is site-derived, neither. Every option, including the lightest, revises this settled sentence.
- **Area:** `certificate-steps-membership-2026-07-12.md` Decision 1 (eleven premise kinds), site-context rule :158, Decision 7 :462.
- **Why you:** Settled public output vocabulary — any revision of settled text is owner-gated by definition.
- **Options:** (a) site-context payload, no premise (plus the one-sentence Decision 7 revision); (b) widen the GuardCondition premise kind's meaning; (c) new site-condition premise kind (grows the settled eleven).

### 6. §4a witness — two proposed extensions of settled/locked text (SPEC CONFLICTS, surfaced not applied)

- **Question (two parts):** (i) The design mints ProvenViolating for qualifier/dimension families, but the settled verdict-linkage text licenses ProvenViolating only from BandContainment/Comparison, and the replay note doesn't cover the new re-checks — accept the proposed textual amendments (zero vocabulary growth), or decline and let those families fall back to Unresolved on the violating arm? (ii) The locked §1.4 witness construction names a singular corner procedure; the design proposes a second candidate corner (previously mispresented as a "reading") — confirm the two-candidate extension or drop to the single corner?
- **Area:** `certificate-steps-membership-2026-07-12.md` :246 and :421; `compiler-readiness-plan-2026-07-12-architecture.md` :54 and :73.
- **Why you:** Both touch settled/locked text; both are now explicit owner-gated proposed deltas with stated fallbacks.
- **Options:** per part — accept amendment / decline (Unresolved fallback); confirm two-candidate set / decline (single directional corner).

### 7. Structural-severity — the active plan's sequencing sentence is backwards (SPEC CONFLICT in active canon) + doc-correction sign-off bundle

- **Question:** The plan says this slice "can land any time after Slice 0's dead-end=Error dependency" — but dead-end=Error IS this slice's deliverable, and the architecture's Hole-4 relabel presumes it already in force. This slice must land BEFORE or WITH that relabel, or dead-end rows silently flip compile outcomes. A de-garble is proposed, alongside the slice's other canonical corrections (graph-analyzer.md OQ1 override prose, spec :186/:189 severity naming, diagnostic-system.md :191 caveat retirement).
- **Area:** `compiler-readiness-plan-2026-07-12.md:556-557` vs `…-architecture.md:155`; plus graph-analyzer.md, precept-language-spec.md, diagnostic-system.md.
- **Why you:** The plan is active canon and graph-analyzer.md carries a previously-resolved decision being overridden; the autonomous contract is propose-only.
- **Options:** apply corrections as proposed; adjust the prose; or revisit the underlying severity flip with a new ruling.

### 8. Slice 0 — type-implied modifiers hole: widen or park

- **Question:** Type-implied modifiers (e.g. `exchangerate` implies Positive; four collection types imply Notempty) are a live evidence source with no legal premise or step — a divisor of type exchangerate discharges nonzero with zero authored evidence, falsifying the doc's "no premise outside the eleven" claim. The fix is either to widen the CatalogFact recompute rule with a third arm citing the Types catalog, or to park it for your ruling.
- **Area:** `certificate-steps-membership-2026-07-12.md:207-209, :300`; `src/Precept/Language/Types.cs:674`, `ProofEngine.Composition.cs:674`.
- **Why you:** It touches the settled step vocabulary's recompute rules; the review offered widen-vs-park explicitly.
- **Options:** widen CatalogFact (plus coverage-table rows and a liveness test cell); or park as an open question.

### 9. §6 constant-rule — hook contexts and runtime-ordering canon

- **Question:** Rules and ensures on hook contexts decline ALL facts in this design, because the staleness tracking can't cross the row-to-hook boundary and canon states no hook-vs-row-effect ordering guarantee. Whether hook reads see committed pre-state is runtime-ordering semantics only you can put into canon; it also decides whether the shipped sign path's hook behavior is a live fail-open.
- **Area:** `docs/compiler/proof-engine.md` (no hook-ordering statement); spec §0.6 item 7 (reassignment sequencing only).
- **Why you:** A semantics-in-canon question, not an implementation choice.
- **Options:** (a) confirm exclusion as the end-state rule and route the sign path's hook behavior to the §2.2 sweep; (b) establish the ordering guarantee in canon and re-admit hooks; (c) split by hook direction if distinguishable; (d) extend the staleness carrier across the boundary (a new ledger mechanism — itself a park, see below).

### 10. §2 case-by-case — reading of the locked ⊥-arm sentence (one-word confirmation)

- **Question:** The locked phrase "a ⊥-arm conservatively contributes its un-narrowed base interval" is ambiguous between two sound readings. The design chose (A) — evaluate under the outer environment, assumption not applied (tightest unconditionally-sound) — and asks for confirmation rather than claiming it's the only reading.
- **Area:** `compiler-readiness-plan-2026-07-12-architecture.md:106`.
- **Why you:** Locked-text interpretation; the design must not foreclose your intended reading by fiat.
- **Options:** (A) outer environment (chosen); (B) bare declared intervals (also sound, strictly wider, no benefit).

### 11. §3 money — wire-set boundary

- **Question:** Do the six +/− cells, the five business unary Negate cells, and the three ExchangeRate cells belong in this slice, a later pass, or a split? The ratified architecture text supports three readings (cited line range, enumerated list, and "whole op-meta family" prose each span different cell sets).
- **Area:** architecture §1.5 vs plan §3 stub title ("× and ÷") — no locked decision settles the boundary.
- **Why you:** Ruled scope is ambiguous; widening or holding it changes effort/test budget, and the autonomous pass may not settle scope the ruling left ambiguous. All parked cells are precision-only meanwhile (unwired ⇒ conservative reject).
- **Options:** (a) include all (Duration/Period negation gated on space verification); (b) hold strictly to ×/÷, rest rides a later pass; (c) include +/− and the three same-space Negate cells, defer Duration/Period negation and ExchangeRate.

### 12. §4a witness — Presence/KeyPresence witness cell: confirm deferred or pull in-MVP

- **Question:** The one grid cell the reuse and soundness lenses split on. The design implements the deferred default (mint only Proven/Unresolved; existing diagnostics keep naming the unguarded subject).
- **Area:** architecture §1.4 Presence/KeyPresence note + readiness-plan open-decision row (:163) — the architecture defers "unless the owner wants it in-MVP."
- **Why you:** The architecture explicitly reserves this to you.
- **Options:** (i) confirm deferred; (ii) pull in-MVP via a follow-up design pass (new non-interval machinery + a third witness payload type).

### 13. §4a witness — Modifier family's witness-grid cell (13th obligation kind missing from the owner-ruled 12-family grid)

- **Question:** The code has 13 obligation kinds; the ruled grid has 12 families. How should the Modifier family's cell read? Until ruled, built behavior is the conservative common denominator (Proven/Unresolved only, no witness).
- **Area:** architecture §1.4 grid + pipeline-eval §4 vs `ProofRequirementKind.cs:6-54`.
- **Why you:** The grid is owner-ruled; adding a row is your call.
- **Options:** (i) NotApplicable mirroring the qualifier row; (ii) deferred configuration-style cell grouped with Presence; (iii) explicit permanent no-witness row.

### 14. §6 constant-rule — qualified-rule residual posture

- **Question:** First magnitude-sensitive consumption of trusted facts: how to handle the residual where a qualified rule's own qualifier obligation is unresolved (a ledger-dishonest Proven inside an already-rejecting compile), including a sign-fold/interval-fold divergence where two certificates can disagree about the same declaration in one compile.
- **Area:** `docs/compiler/proof-engine.md:1522-1528`; architecture §1.6 tradeoff leg — neither addresses magnitude-sensitive consumption.
- **Why you:** Sets the posture for every future magnitude consumer of trusted facts.
- **Options:** (a) semantics-only; document the failing-compile-only residual and the divergence; (b) unit-space gate at the fold (order-independent, statically checkable); (c) sequence rule-condition obligations first and thread the blocked set (strongest; rides Slice 0's reshape).

### 15. Remaining parks and scope calls (lower stakes; safe under any answer)

- **§6 constant-rule:** staleness-carrier extension across the row-to-hook boundary (new mechanism — adopt / decline / subsume under ordering canon); state-boundary ensure kinds as fact sources (widen after ordering canon / leave declined with rewrite hint); fact-parse widening for compound conditions and length/count subjects (widen later / decline with hints); ensure-side self-unsatisfiability diagnostic — no PRE0159 analogue exists for ensures (new scan / accept as covered / fold into §2.2 sweep).
- **§2 case-by-case:** infeasibility step kind for true ⊥-branch exclusion (logged, not proposed — keep conservative contribution or add via the owner-gated growth path); TypedArg.ImpliedModifiers population (authored-only asymmetry is the default under silence; symmetric population is a small change).
- **§4a witness:** F8 plumbing — demote arg-leaf/element/length sites for MVP (wired default; sites convert automatically when §2 lands) vs build §2-shaped plumbing now (exceeds the slice delegation).
- **§3 money:** pre-existing kernel crash — decimal corner/endpoint overflow throws instead of diagnosing (`NumericInterval.cs:90/:100`), confirmed live; robustness not soundness, but it lives in methods this slice touches. Route: (a) fold a saturate-to-Unbounded guard into this slice's kernel pass; (b) route to the Slice-0 sweep; (c) standalone bugs.md entry.
- **Structural-severity:** PRE0159 message renders the raw internal expression record — becomes a blocking rejection message once the code is an Error (fix here / bugs.md / fold into Phase 8 message-richness); `samples/Test.precept` fails the clean-compile gate with two pre-existing PRE0078 Errors — you committed it deliberately as a snapshot, so its disposition (delete / repair / move out of samples/) is yours.

## How to review

Per the plan's verification steps:

- `git status` should show **only `docs/Working/` changes** (plus this summary). No `src/`, no canonical `docs/` edits — all canonical corrections above are proposals, not applied.
- **Nothing was Locked.** All six slice docs sit at design-complete/Externally-Grounded with open questions; the Slice-0 doc is lockable only after its six fixes land.
- **Nothing was committed.**
- **All new language/public surface was parked, not settled** — the §1a certificate step kind (item 1 above) is the headline; every park carries neutral options and waits on you.

Suggested order: work the ranked list top-down — items 1–7 unblock or gate the most downstream work; items 8–14 are per-slice postures; item 15 can be batch-dispositioned.
