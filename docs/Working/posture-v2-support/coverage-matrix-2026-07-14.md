---
title: Coverage / Completeness / Hedge Matrix — Frank posture replay v2 input
date: 2026-07-14
author: Soup Nazi (testing coverage pass)
status: Working
---

# Coverage Matrix — posture replay vs source record

Primary artifact reviewed: `docs/Working/frank-go-forward-posture-replay-2026-07-14.md`.

Status key: `present-correct` / `hedged` / `dropped` / `contradicted`.

## Full matrix

| Source item (brief + citation) | Status | Note |
|---|---|---|
| Decision Ledger #1 — ratify prove-or-reject as a package (items 2–3 ride with it) | `present-correct` | Replay §3 and §6 track the ratified answer: derived-value obligations reject when unresolved; §6 and the package condition are both named. |
| Decision Ledger #2 — replace spec:225 tool ban with the certificate criterion (+ admissibility extension) | `present-correct` | Replay §7 explicitly calls the certificate criterion a genuinely new deposit; no softening. |
| Decision Ledger #3 — defer §1b, leave OPEN, no spec:256 override | `present-correct` | Replay §3/§6 say §1b is deferred, not rejected, and §6 is the honest workaround. |
| Decision Ledger #4 — route simple aggregates (`sum`/`min`/`max`/`average`) to `/design`; keep arbitrary folds out | `dropped` | No §4/#4 aggregate-language-surface call appears in the replay. |
| Decision Ledger #5 — compute-then-check remains deferred; computed fields are not a full substitute | `dropped` | Replay never mentions the post-MVP ergonomic gap or the computed-fields carve-out. |
| Decision Ledger #6 — rescope spec:223; bounded-write checks use the three-way verdict (`proven` / `proven-violating` / `unresolved` + printed WP) | `dropped` | Replay keeps reject-vs-govern routing but omits the spec:223 rescope and the three-way verdict surface. |
| Decision Ledger #7 — leave overflow canon text as-is; no interim rider; treat the gap as build-order, not text defect | `hedged` | Replay discloses the carve-out in posture-doc prose, which is softer/more rider-like than the source ruling's explicit 'leave the text as-is' stance. |
| Decision Ledger #8 — tighten only the `philosophy.md:49` 'wrong answer' clause | `dropped` | Replay does not restate the owner call on the single-clause philosophy edit. |
| Decision Ledger #9 — close the escape hatch / trust-marker question | `present-correct` | Replay §6 says 'no escape hatch' for unresolved derived obligations and ties that to the ratified ruling. |
| Decision Ledger #10 — dead-row severity = Error | `dropped` | Replay never says the dead-row fork is closed on the Error side. |
| Decision Ledger old gate D1 — overflow park survives | `present-correct` | Replay §2 foregrounds the owner-parked overflow carve-out. |
| Decision Ledger old gate D2 — band-split superseded by #1 | `present-correct` | Replay §6 bans 'govern, don't prove' for derived values and spelling-based routing, which is the D2 premise being killed. |
| Decision Ledger old gate D3/D4 — settled/applied, no new ruling needed | `dropped` | No mention. |
| Decision Ledger old gate GATE-F — folded into overflow/band sentence handling | `dropped` | No mention of the sentence-enumeration gate. |
| Decision Ledger old gate GATE-G — declared `min` bounds discharge proofs under the prove-or-reject rebuild | `dropped` | Replay never restates this folded gate explicitly. |
| Decision Ledger old gate GATE-H — three-category taxonomy folded into #1 | `dropped` | Replay uses prove/govern language but never restates the taxonomy decision. |
| Decision Ledger old gate GATE-I — fault delivery still open | `dropped` | No mention. |
| Decision Ledger old gate GATE-J — two spec self-contradictions still open | `dropped` | No mention. |
| Decision Ledger old gates B/C/D/E/K/L/N — still open, unaffected, non-blocking | `dropped` | No mention. |
| Decision Ledger old gate M — PRE0141 ownership deferred to Phase 5 start | `dropped` | No mention. |
| Decision Ledger old gate O — ∞/NaN and integer-conversion overflow remain open inside the parked lane | `dropped` | Replay mentions overflow generally but not the still-open sub-lane issues. |
| Decision Ledger Stage 0b — Decision A: `ProofVerdict` DU (`Proven` / `ProvenViolating` / `Unresolved`) | `dropped` | Replay omits the verdict-model choice entirely. |
| Decision Ledger Stage 0b — Decision B: independent re-checker retracted from MVP | `dropped` | Replay never states that MVP includes certificate format only, not a live re-checker. |
| Decision Ledger Stage 0b — Decision C: certificate step vocabulary comes from a spec catalog, not `ProofStrategy` | `dropped` | Replay never mentions the catalog-source ruling. |
| Decision Ledger Stage 0b — six structural-severity flips Warning→Error re-affirmed | `dropped` | Replay never carries forward the structural-severity ruling. |
| Decision Ledger Stage 0b — F5 dead-end→dead-end disposition = Error | `dropped` | Replay omits the dead-end severity disposition. |
| Decision Ledger Stage 0b — Emission (c): interleave; retire `DiagnosticStage` in Slice 0 | `dropped` | Replay never mentions the interleaving/emission ruling. |
| Boundary Ruling — Hybrid model, total over the fault surface, with overflow parked | `present-correct` | Replay §2–§5 preserve the model and the parked-overflow carve-out. |
| Boundary Ruling — Obligation-Role Rule / provenance route; decidability is discharge-oracle, not router | `present-correct` | Replay §5 restates this almost verbatim. |
| Boundary Q5 — proven-violating derived assignments are Error | `dropped` | Replay never restates the Q5 severity call. |
| Boundary Q6 — default-vs-set-action consistency; same disposition regardless of spelling | `hedged` | Replay strongly preserves spelling-invariance, but it never explicitly states the default/set-action consistency pair. |
| Boundary Q7 — merely-unprovable live fault obligations reject and name the carrier | `present-correct` | Replay §3/§6 say unresolved derived obligations reject; no runtime amnesty. |
| Boundary Q8 — carries-proof split dissolves; all declared constraints are governed + premises | `hedged` | Replay reaches the same practical outcome via provenance routing, but it drops the source doc's explicit carries-proof framing. |
| Boundary Q9 — ship per-obligation PROVEN/GOVERNED surface; no DEFERRED state | `dropped` | Replay never mentions the disposition surface. |
| Boundary Q11 — taxonomy (fault floor / incoherence / flag layer / parked overflow lane / governance disposition) | `dropped` | Replay omits the taxonomy entirely. |
| Boundary R1 — read 'no deferral' as fault-scoped; governance is not deferral | `present-correct` | Replay §4–§6 makes this distinction central. |
| Boundary R2 — optional precision clause on §268's derivation-line boundary | `present-correct` | Replay §4–§5 operationalize the same derivation-line precision. |
| Boundary R3 — keep spec §0.6:258 unchanged | `dropped` | No mention. |
| Boundary R4 — trust construct re-opened as a future `/design` question, not decided here | `contradicted` | Replay follows the later owner ruling that closed the escape hatch; it no longer leaves this open. |
| Boundary R5 — no edits to `philosophy.md:49/55/57` | `dropped` | Replay does not restate the no-edit recommendation. |
| Boundary R6 — surface overflow-text tension to the owner | `dropped` | Replay discloses overflow but omits the owner-surface/document-reconciliation instruction. |
| Boundary worked row 1 — foldable default outside bound => compile-time Error | `dropped` | Replay does not cover default-fold coherence cases. |
| Boundary worked row 2 — unfoldable default => compile clean; construction sweep governs | `dropped` | Replay does not cover the unfoldable-default edge case. |
| Boundary worked row 3 — computed literal into declared bound => prove-or-reject Error | `present-correct` | Replay §5 says derived-value containment is prove-or-reject regardless of spelling. |
| Boundary worked row 4 — raw external value into its own bound => ingress-governed | `present-correct` | Replay §4 covers this directly. |
| Boundary worked row 5 — stricter downstream bound on raw input still governs at ingress | `present-correct` | Replay §4's ingress-governance description fits this case. |
| Boundary worked row 6 — derived `v * 2` into bounded field rejects unless proven safe | `present-correct` | Replay §4/§5 make derived containment reject when unresolved. |
| Boundary worked row 7 — governed argument + derived sum split (`a` governed, `Current + a` proven) | `present-correct` | Replay §4 distinguishes ingress governance from derived-value proof exactly this way. |
| Boundary worked row 8 — `nonzero` ingress carrier discharges division via composition | `present-correct` | Replay §4 calls out the same composition seam. |
| Boundary worked row 9 — division without a carrier rejects | `present-correct` | Replay §3/§5 preserve this. |
| Boundary worked row 10 — standalone nonlinear relational invariant is governed by the post-mutation sweep, not proven | `present-correct` | Replay §3–§4 make this one of the central corrections. |
| Boundary worked row 11 — linear invariant is governed, but a derived assignment using the same relation is proven | `present-correct` | Replay §3 explicitly cites worked row 11 and preserves the distinction. |
| Boundary worked row 12 — dynamic bound declaration legal/governed; dependent containment still prove-or-rejects | `hedged` | Replay keeps the general provenance rule but never explicitly carries forward the dynamic-bound example/BUG-020 shape. |
| Boundary worked row 13 — relational rule folded against constant defaults => compile-time Error | `dropped` | No default-fold relational example appears. |
| Boundary worked row 14 — derived cardinality vs `maxcount` is prove-or-reject Error | `dropped` | Replay's named live fault set omits the count-bound family entirely. |
| Boundary worked row 15 — empty dequeue/access rejects | `present-correct` | Replay still names empty/index access inside the live fault surface. |
| Boundary worked row 16 — same rule shape, different decidability; routing unchanged, discharge differs | `present-correct` | Replay §5 keeps the 'decidability only decides discharge-vs-reject' rule. |
| Boundary worked row 17 — flag modifiers (`optional`, `editable`, `ordered`) sit outside the value-constraint boundary | `dropped` | Replay never preserves this out-of-scope edge of the model. |
| Identity Thesis Rec A — govern+flag non-proof-carrying bands; keep prove-or-reject only for proof-carrying constraints | `contradicted` | Replay §6 explicitly rejects the thesis's counter-position and returns derived non-proof-carrying containment to reject. |
| Identity Thesis Rec B — temper `philosophy.md:49` 'No bugs / wrong answer' | `dropped` | Replay does not restate the thesis's philosophy-copy recommendation. |
| Identity Thesis Rec C — add a design-by-contract comparator row to philosophy | `dropped` | No mention. |
| Identity Thesis Rec D — reconcile overflow sentences with a narrowing/rider while D1 is parked | `dropped` | Replay discloses overflow substantively but does not carry forward the thesis's canon-edit recommendation. |
| Identity Thesis Rec E — resolve §0.7 discharge-tier vacuity | `dropped` | No mention. |
| Identity Thesis Rec F — adopt the three-category taxonomy + flag-soundness in canon text | `dropped` | Replay omits the taxonomy/flag-soundness recommendation. |
| Identity Thesis Rec G — route the per-obligation disposition surface to `/design` with inline-rendering / soundness criteria | `dropped` | Replay never mentions the design bar for the surface. |
| Identity Thesis Rec H — make the dead-row severity fork an explicit owner choice | `dropped` | Replay does not restate the dead-row fork at all. |
| Identity Thesis Rec I — schedule the definition-evolution deploy-lane decision before first external users | `dropped` | No mention. |
| Critique C1-F1 — state the Event-B trade honestly: Event-B proves invariant preservation; Precept enforces it | `dropped` | Replay never engages the Event-B comparison. |
| Critique C1-F2 — narrow the SPARK uniqueness claim to the ingress-discharge qualifier | `dropped` | Replay never revisits the SPARK/uniqueness language. |
| Critique C1-F3 — recast the 'empty cell' as cost-to-replicate, not occupancy | `dropped` | No positioning/comparator treatment appears. |
| Critique C1-F4 — call the proof claim a bet, not a moat | `dropped` | Replay does not preserve the thesis's downgraded 'bet' language. |
| Critique C1-F5 — don't over-attribute language austerity to proof alone | `dropped` | No mention. |
| Critique C1-F6 — hedge the Eiffel/Code Contracts evidence as two-sided | `dropped` | No mention. |
| Critique C2-F1 — query-only disposition surfaces are insufficient; inline/ambient display is required | `dropped` | Replay says nothing about the false-security design constraint on the surface. |
| Critique C2-F2 — do not bundle PROVEN / FLAGGED / GOVERNED into one flowing guarantee sentence | `dropped` | Replay no longer uses the thesis's marketing sentence, but it does not preserve the explicit anti-bundling lesson either. |
| Critique C2-F3 — if bands are reclassified, walk the `min 5` case through the author-visible disposition | `dropped` | Replay abandons the reclassification but does not record the legibility lesson as a standing caution. |
| Critique C2-F4 — call out the inspector-only / out-of-fragment residual risk or add a per-expression marker | `dropped` | Replay never mentions the residual-risk marker question. |
| Critique C2-F5 — disclosure first; disposition tags must not outrun their own soundness | `dropped` | Replay omits the self-referential soundness caution on the surface. |
| Critique C3-F1 — the substrate supports governing non-proof-carrying bands rather than rejecting them | `contradicted` | Replay sides with the later owner ruling and rejects this finding's end-state conclusion. |
| Critique C3-F2 — interim vs end-state split (Error interim, govern end-state) should be stated plainly | `contradicted` | Replay adopts the no-interim/end-state posture and does not preserve the temporal split. |
| Critique C3-F3 — separate merely-unprovable vs proven-always-violating; justify the default/set-action asymmetry | `dropped` | Replay does not preserve this split or the rationale request. |
| Critique C3-F4 — surface the number-model blocker under the overflow story | `hedged` | Replay names overflow as owner-parked but not the number-model prerequisite/blocker that the critique wanted made explicit. |
| Critique C3-F5 — `philosophy.md:53` overclaim must be carried into the recommendation set | `dropped` | Replay omits the explicit philosophy-text reconciliation ask. |
| Critique C3-F6 — retain the fair split between fault-deferral rejection and band-relocation debate | `dropped` | Replay keeps the outcome but not the critique's explicit fairness lesson. |
| Critique C3-F7 — don't borrow authority from text you also want to amend | `dropped` | Replay does not preserve this methodological caution. |
| Critique C3-F8 — convergence is corroboration, not ratification | `dropped` | Replay leans on ratified outcomes rather than preserving this meta-lesson. |
| Critique C3-F9 — no GOVERNED claim without real enforcement behind it | `hedged` | Replay asserts real enforcement, but it drops the critique's explicit 'empirical precondition / no governed claim without the sweep' caution. |
| Forcing §4.1 — replace spec:225's tool ban with the certificate-defined legibility rule | `present-correct` | Replay §7 keeps the certificate criterion as new, load-bearing material. |
| Forcing §4.2 — rescope spec:223 and distinguish `proven-violating` from `unresolved` with printed WP | `dropped` | Replay never mentions the spec:223 repair or the split between witness vs weakest-precondition rejections. |
| Forcing §4.3 — keep derived-value containment as a re-proved obligation, strengthen WP/error output, and make the posture conditional on the engine package | `hedged` | Replay keeps the package-condition and re-proved posture, but it omits the strengthened WP/assume-after-establish specifics. |
| Forcing §4.4 — admit simple aggregates; keep general folds out | `dropped` | No mention. |
| Forcing §4.5 — replace spec:256's depth bound with full linear entailment | `contradicted` | Replay follows the ledger's deferred §1b / no-override call rather than this source doc's replacement proposal. |
| Forcing §4.6 — commit to a compute-then-check row construct as a named ergonomic dependency | `dropped` | Replay does not preserve the dependency call. |
| Forcing §4.7 — single-operation timestamp / verification-not-search / flag-and-record guidance | `dropped` | No mention. |
| Retrospective §1.1 lesson 1 — 2c-ii was a soundness wall + missing capability (BUG-027), not 'under-designed' | `dropped` | Replay §7 says the arc was hard-won, but it never preserves the specific 2c-ii diagnosis. |
| Retrospective §1.1 lesson 2 — 'just enforce the bound' hid genuinely new machinery (length/count relational work) | `dropped` | No mention. |
| Retrospective §2 lesson 1 — the certificate criterion was the biggest single new deposit | `present-correct` | Replay §7 repeats this almost verbatim. |
| Retrospective §2 lesson 2 — §6 is the principled escape valve / honest fix for over-rejection | `present-correct` | Replay §3 keeps §6 in the MVP and frames it as the author-states-the-fact escape valve. |
| Retrospective §2 lesson 3 — §1b is never an expressibility gain; that is why deferral is principled | `present-correct` | Replay §3 and §7 preserve this. |
| Retrospective §2 lesson 4 — the destination is prove-or-reject plus a stronger engine package, not June's engine | `present-correct` | Replay §3/§7 explicitly draw this contrast. |
| Retrospective §3/P0 — structural-severity sequencing must be fixed before Slice 0 starts | `dropped` | Replay omits the sequencing hazard. |
| Retrospective §3/P0 — OQ1 (certificate step kind) must be ruled before `Proven` mints depend on it | `dropped` | Replay omits the OQ1 gating lesson. |
| Retrospective §3 — §1a's real recurrence risk is matcher near-miss creep; OQ2 must stay parked | `dropped` | Replay omits the near-miss creep lesson. |
| Retrospective §4 — when a slice hits a wall, do the engineering diagnosis first; do not re-open product identity as the reflex | `dropped` | Replay names the spiral, but not this corrective process rule. |
| Retrospective §5 condition 1 — do not start Slice 0 dirty; fix the two P0s first | `dropped` | Not preserved in §6 or elsewhere. |
| Retrospective §5 condition 2 — treat Slice 0 like 2c-ii should have been treated: full input-space + adversarial soundness + no adjacent scope | `dropped` | No mention. |
| Retrospective §5 condition 3 — hold the parking lines; 'just one more form' is the spiral restarting | `dropped` | No mention. |

## Prioritized hedged / dropped findings

### Critical
- Decision Ledger #6 — spec:223 rescope / three-way verdict is dropped.
- Boundary Q9 — the per-obligation PROVEN/GOVERNED surface is dropped.
- Boundary/Q11 + worked row 14 — count/length/cardinality fault-family coverage is dropped from the replay's live-fault story.
- Decision Ledger #10 + Stage 0b F5 — dead-row / dead-end severity rulings are dropped.
- Retrospective §4 + §5(1–3) — the anti-spiral process lessons (diagnose the wall first; do not start Slice 0 dirty; hold parking lines; refuse adjacent scope) are absent from §6.

### Significant
- Decision Ledger #4 / Forcing §4.4 — aggregates-in / folds-out is dropped.
- Decision Ledger #5 / Forcing §4.6 — compute-then-check remains a documented ergonomic dependency, but the replay drops it.
- Decision Ledger #7 — the owner ruled 'leave overflow text as-is'; the replay reintroduces a softer rider-like disclosure tone.
- Boundary Q5/Q6/Q8 + worked rows 1/2/12/13/17 — default-fold, dynamic-bound, and edge-of-boundary cases are underrepresented or absent.
- Critique C2-F1/F2/F4/F5 + Thesis Rec G — false-security / surface-legibility lessons are dropped wholesale.

### Context / completeness
- Decision Ledger old gates D3/D4/GATE-F/GATE-G/GATE-H/GATE-I/GATE-J/GATE-B/C/D/E/K/L/N/GATE-M/GATE-O — all dropped from the replay.
- Stage 0b decisions A/B/C + Emission (c) — all dropped.
- Identity Thesis recs B/C/D/E/F/H/I — dropped; Rec A is contradicted.
- Critique C1-F1..F6 — uniqueness/positioning cautions are all dropped.
- Critique C3-F3/F5/F6/F7/F8/F9 — methodology / overflow / ratification cautions are dropped or hedged.

### Complete hedged / dropped inventory (every finding)

#### High
- `dropped` — Decision Ledger #4 — route simple aggregates (`sum`/`min`/`max`/`average`) to `/design`; keep arbitrary folds out
- `dropped` — Decision Ledger #5 — compute-then-check remains deferred; computed fields are not a full substitute
- `dropped` — Decision Ledger #6 — rescope spec:223; bounded-write checks use the three-way verdict (`proven` / `proven-violating` / `unresolved` + printed WP)
- `hedged` — Decision Ledger #7 — leave overflow canon text as-is; no interim rider; treat the gap as build-order, not text defect
- `dropped` — Decision Ledger #10 — dead-row severity = Error
- `dropped` — Decision Ledger Stage 0b — six structural-severity flips Warning→Error re-affirmed
- `dropped` — Decision Ledger Stage 0b — F5 dead-end→dead-end disposition = Error
- `dropped` — Boundary Q5 — proven-violating derived assignments are Error
- `hedged` — Boundary Q6 — default-vs-set-action consistency; same disposition regardless of spelling
- `hedged` — Boundary Q8 — carries-proof split dissolves; all declared constraints are governed + premises
- `dropped` — Boundary Q9 — ship per-obligation PROVEN/GOVERNED surface; no DEFERRED state
- `dropped` — Boundary Q11 — taxonomy (fault floor / incoherence / flag layer / parked overflow lane / governance disposition)
- `hedged` — Boundary worked row 12 — dynamic bound declaration legal/governed; dependent containment still prove-or-rejects
- `dropped` — Boundary worked row 14 — derived cardinality vs `maxcount` is prove-or-reject Error
- `dropped` — Identity Thesis Rec G — route the per-obligation disposition surface to `/design` with inline-rendering / soundness criteria
- `dropped` — Critique C2-F1 — query-only disposition surfaces are insufficient; inline/ambient display is required
- `dropped` — Critique C2-F2 — do not bundle PROVEN / FLAGGED / GOVERNED into one flowing guarantee sentence
- `dropped` — Critique C2-F3 — if bands are reclassified, walk the `min 5` case through the author-visible disposition
- `dropped` — Critique C2-F4 — call out the inspector-only / out-of-fragment residual risk or add a per-expression marker
- `dropped` — Critique C2-F5 — disclosure first; disposition tags must not outrun their own soundness
- `hedged` — Critique C3-F4 — surface the number-model blocker under the overflow story
- `hedged` — Critique C3-F9 — no GOVERNED claim without real enforcement behind it
- `dropped` — Forcing §4.2 — rescope spec:223 and distinguish `proven-violating` from `unresolved` with printed WP
- `hedged` — Forcing §4.3 — keep derived-value containment as a re-proved obligation, strengthen WP/error output, and make the posture conditional on the engine package
- `dropped` — Forcing §4.4 — admit simple aggregates; keep general folds out
- `dropped` — Forcing §4.6 — commit to a compute-then-check row construct as a named ergonomic dependency
- `dropped` — Retrospective §3/P0 — structural-severity sequencing must be fixed before Slice 0 starts
- `dropped` — Retrospective §3/P0 — OQ1 (certificate step kind) must be ruled before `Proven` mints depend on it
- `dropped` — Retrospective §3 — §1a's real recurrence risk is matcher near-miss creep; OQ2 must stay parked
- `dropped` — Retrospective §4 — when a slice hits a wall, do the engineering diagnosis first; do not re-open product identity as the reflex
- `dropped` — Retrospective §5 condition 1 — do not start Slice 0 dirty; fix the two P0s first
- `dropped` — Retrospective §5 condition 2 — treat Slice 0 like 2c-ii should have been treated: full input-space + adversarial soundness + no adjacent scope
- `dropped` — Retrospective §5 condition 3 — hold the parking lines; 'just one more form' is the spiral restarting

#### Medium
- `dropped` — Decision Ledger #8 — tighten only the `philosophy.md:49` 'wrong answer' clause
- `dropped` — Decision Ledger old gate D3/D4 — settled/applied, no new ruling needed
- `dropped` — Decision Ledger old gate GATE-F — folded into overflow/band sentence handling
- `dropped` — Decision Ledger old gate GATE-G — declared `min` bounds discharge proofs under the prove-or-reject rebuild
- `dropped` — Decision Ledger old gate GATE-H — three-category taxonomy folded into #1
- `dropped` — Decision Ledger old gate GATE-I — fault delivery still open
- `dropped` — Decision Ledger old gate GATE-J — two spec self-contradictions still open
- `dropped` — Decision Ledger old gates B/C/D/E/K/L/N — still open, unaffected, non-blocking
- `dropped` — Decision Ledger old gate M — PRE0141 ownership deferred to Phase 5 start
- `dropped` — Decision Ledger old gate O — ∞/NaN and integer-conversion overflow remain open inside the parked lane
- `dropped` — Decision Ledger Stage 0b — Decision A: `ProofVerdict` DU (`Proven` / `ProvenViolating` / `Unresolved`)
- `dropped` — Decision Ledger Stage 0b — Decision B: independent re-checker retracted from MVP
- `dropped` — Decision Ledger Stage 0b — Decision C: certificate step vocabulary comes from a spec catalog, not `ProofStrategy`
- `dropped` — Decision Ledger Stage 0b — Emission (c): interleave; retire `DiagnosticStage` in Slice 0
- `dropped` — Boundary R3 — keep spec §0.6:258 unchanged
- `dropped` — Boundary R5 — no edits to `philosophy.md:49/55/57`
- `dropped` — Boundary R6 — surface overflow-text tension to the owner
- `dropped` — Boundary worked row 1 — foldable default outside bound => compile-time Error
- `dropped` — Boundary worked row 2 — unfoldable default => compile clean; construction sweep governs
- `dropped` — Boundary worked row 13 — relational rule folded against constant defaults => compile-time Error
- `dropped` — Boundary worked row 17 — flag modifiers (`optional`, `editable`, `ordered`) sit outside the value-constraint boundary
- `dropped` — Identity Thesis Rec B — temper `philosophy.md:49` 'No bugs / wrong answer'
- `dropped` — Identity Thesis Rec C — add a design-by-contract comparator row to philosophy
- `dropped` — Identity Thesis Rec D — reconcile overflow sentences with a narrowing/rider while D1 is parked
- `dropped` — Identity Thesis Rec E — resolve §0.7 discharge-tier vacuity
- `dropped` — Identity Thesis Rec F — adopt the three-category taxonomy + flag-soundness in canon text
- `dropped` — Identity Thesis Rec H — make the dead-row severity fork an explicit owner choice
- `dropped` — Identity Thesis Rec I — schedule the definition-evolution deploy-lane decision before first external users
- `dropped` — Critique C3-F3 — separate merely-unprovable vs proven-always-violating; justify the default/set-action asymmetry
- `dropped` — Critique C3-F5 — `philosophy.md:53` overclaim must be carried into the recommendation set
- `dropped` — Critique C3-F6 — retain the fair split between fault-deferral rejection and band-relocation debate
- `dropped` — Critique C3-F7 — don't borrow authority from text you also want to amend
- `dropped` — Critique C3-F8 — convergence is corroboration, not ratification
- `dropped` — Forcing §4.7 — single-operation timestamp / verification-not-search / flag-and-record guidance
- `dropped` — Retrospective §1.1 lesson 1 — 2c-ii was a soundness wall + missing capability (BUG-027), not 'under-designed'
- `dropped` — Retrospective §1.1 lesson 2 — 'just enforce the bound' hid genuinely new machinery (length/count relational work)

#### Low
- `dropped` — Critique C1-F1 — state the Event-B trade honestly: Event-B proves invariant preservation; Precept enforces it
- `dropped` — Critique C1-F2 — narrow the SPARK uniqueness claim to the ingress-discharge qualifier
- `dropped` — Critique C1-F3 — recast the 'empty cell' as cost-to-replicate, not occupancy
- `dropped` — Critique C1-F4 — call the proof claim a bet, not a moat
- `dropped` — Critique C1-F5 — don't over-attribute language austerity to proof alone
- `dropped` — Critique C1-F6 — hedge the Eiffel/Code Contracts evidence as two-sided

## Proposed additions for §6 'What must NEVER be said again'

Every retrospective mistake/lesson not already named in §6 is a gap. Proposed additions:
- **Add:** 'Prove-or-reject is already honest on June/today's engine.'  
  **Why:** Retrospective §2 + Decision Ledger #1: the posture is honest only as the ratified package, not as the old engine with the slogan re-asserted.
- **Add:** 'This hard slice means Precept's identity is wrong / we should reopen the philosophy first.'  
  **Why:** Retrospective §4: the first move at a wall is disciplined engineering diagnosis, not identity re-litigation.
- **Add:** 'Slice 0 can start dirty; we can fix structural-severity sequencing / OQ1 later.'  
  **Why:** Retrospective §3 P0 + §5(1): the two P0s are preconditions, not cleanup.
- **Add:** 'Just handle one more matcher form / one more §1b-ish case while we're here.'  
  **Why:** Retrospective §3 near-miss creep + §5(3): that is the spiral restarting.
- **Add:** 'Slice 0 is just plumbing; it can absorb adjacent scope.'  
  **Why:** Retrospective §3 + §5(2): Slice 0 is the highest-risk reshape and must be scope-fenced.
- **Add:** '2c-ii was just under-designed / mostly reuse that got unlucky.'  
  **Why:** Retrospective §1.1: the stall was a soundness wall + missing capability + genuinely new machinery, and forgetting that lesson invites repetition.

## Plain-text summary

Total inventoried: 107. Present-correct: 27. Hedged: 7. Dropped: 68. Contradicted: 5. Most urgent fix: restore the missing spec:223 / three-way-verdict + per-obligation-surface story (Decision Ledger #6 + Boundary Q9), because v2 can still sound decisive while silently losing the honesty machinery that makes the posture legible.
