---
title: "Adversarial Review — Hybrid Model Draft D (Four Dispositions)"
date: 2026-07-14
status: Draft review — 2026-07-14 (no owner ruling yet; nothing promoted)
target: docs/Working/posture-v2-support/hybrid-model-draft-D-four-dispositions-2026-07-14.md
method: >
  Six independent lenses (precept-reviewer design-doc path; citation fidelity;
  posture/Frank-ruling fidelity; routing-table exhaustiveness; worked-example
  accuracy against samples/; two-vs-four-disposition tension against
  the now-deleted root-level hybrid-model copy that the team is rewriting
  under docs/Working/posture-v2-support/), each finding adversarially
  challenged by three
  independent skeptics (majority-vote survival), followed by a completeness
  critic. 98 agents, ~6.5M tokens, ~988 tool calls.
---

# Adversarial Review — Hybrid Model Draft D (Four Dispositions)

**Target:** `docs/Working/posture-v2-support/hybrid-model-draft-D-four-dispositions-2026-07-14.md`, status "candidate for canonical promotion"

---

## 🔴 BLOCKERS

**B1. Draft D asserts a "four dispositions" count as settled, contradicting both the canonical model doc and the designated primary-authority ruling — and cites neither.**
Draft D's opening claim: "Those two guarantees are delivered by **four dispositions**. Every obligation in a real definition lands in exactly one of them." (draft-D:12)
- The now-deleted root-level hybrid-model copy — then marked **Canonical design**, with rewrite work now living under `docs/Working/posture-v2-support/` — states the opposite repeatedly and has a section titled "The banned third arm": "The decision table (§5.3) has two dispositions and three enforcement rows... There is no disposition of the form..." (historical citation from the deleted copy: lines 538, 23, 28, 207).
- `docs/Working/frank-go-forward-posture-replay-2026-07-14-v2.md`, the review's designated primary authority, gives an explicit, emphasized count that also isn't four: "two active routes, one of them two-mechanism, plus one out-of-axis disclosed park. **That is the exact count.**" (posture-replay-v2.md:90)
Draft D never mentions either document — a grep for "hybrid-model", "posture-replay", "boundary-ruling", "decision-ledger", or "thesis" across the whole file returns zero hits.

**B2. Draft D reuses the exact title pattern of the existing canonical doc it contradicts, without acknowledging or reconciling the conflict.**
The now-deleted root-level hybrid-model copy was titled "The Hybrid Model — Prove-or-Reject and Governed" and defines exactly two dispositions (historical citation from the deleted copy: line 1). Draft D's own title is "The Hybrid Model — Four Dispositions for Every Obligation" (draft-D:1-5), marked "candidate for canonical promotion." Nothing in Draft D states whether it supersedes, extends, or coexists with the sibling doc bearing the same product name.

**B3. Draft D's split contradicts the now-deleted root-level hybrid-model copy's own guardrail and worked example on the exact question Draft D is trying to answer.**
The deleted root-level copy states: "Same provenance ⇒ same disposition, however the author spelled or placed it" (historical citation from the deleted copy: line 193), and its Example F applies this directly to a constant-default violation: "`default 150` and `set ScoreA = 150` get the same disposition because they have the same provenance... both are prove-or-reject, and both are rejected as proven-violating" (historical citation from the deleted copy: lines 358-361). Draft D's §2 routing table instead pulls "all fixed at compile time" constraints out of prove-or-reject into two new dispositions ("Definition incoherence" / "Dead/flag") (draft-D:70-79) — the opposite classification for the same case, unreconciled.

**B4. Draft D lists numeric overflow as a currently-enforced fault with no disclosure caveat, contradicting v2's explicit ruling that no doc may claim it.**
Draft D §1.1: "Faults are one enumerable family... division by zero, sqrt/pow domain violations, **numeric overflow**, empty-collection access, a result outside a declared bound" (draft-D:28) — stated flatly, no qualification. v2 devotes a dedicated passage to the opposite: representational overflow is "a disclosed, defense-in-depth known-fault backstop with no live compile-time guarantee — not currently prove-or-reject, temporary, and **no doc may claim it prevented**" (posture-replay-v2.md:36, quoting the boundary ruling, reconfirmed at :275). Draft D never uses "overflow" a second time to qualify the claim.

**B5. Draft D never cites `proof-engine.md` or `soundness-and-coverage.md`, dropping the load-bearing MVP caveat that governs every "Verdict" claim it makes.**
A grep of the full file for "proof-engine.md", "soundness-and-coverage.md", "MVP", or "not... implement" returns zero hits; Draft D cites only `precept-language-spec.md`, which itself is flagged as a "Design contract... These requirements define the contract the implementation satisfies" (spec:197), not a build-status statement. `soundness-and-coverage.md:80-83` states, in bold, "**The load-bearing condition (do not drop it).** Prove-or-reject is honest **only as the ratified package** — over the *strengthened* engine... The posture is locked; its *honesty* is contingent on the strengthened engine landing." `proof-engine.md:2607-2612` confirms the three-way ProofVerdict DU underlying Draft D's §1 "Verdict" sections is "designed, not yet implemented." Draft D's own primary-authority source names presenting prove-or-reject as already-honest on today's engine as an explicit error (posture-replay-v2.md:69, :189) — Draft D reproduces that exact error throughout §1 and its §5 table with no qualification anywhere.

**B6. The flagship "Dead/flag" worked example cites a rule that does not exist in the named sample.**
Draft D §1.4 (repeated verbatim in the §7 table, draft-D:154): "A `rule ItemCount >= 0` placed over the `field ItemCount as integer default 0 nonnegative` in `samples/shopping-cart.precept` restates the field's own `nonnegative` contract, folds to always-true, and reports as `VacuousRule`." (draft-D:56) `samples/shopping-cart.precept` contains no such rule anywhere — the only rule on the field is `rule ItemCount <= 1000 because "Cart cannot exceed 1,000 total items — orders above this size require a wholesale workflow"` (samples/shopping-cart.precept:53), a real, non-vacuous constraint unrelated to the cited example. A `grep -rn "ItemCount >= 0"` across the repo turns up this hypothetical only in the non-canonical `docs/Working/precept-identity-and-guarantees-thesis-2026-07-11.md:197,302` — never in `samples/`. It reads as copied from that thesis and mis-attributed to a real sample without checking the source.

**B7. That same fabricated example also misapplies Draft D's own routing procedure to the field it names.**
The §7 table classifies ItemCount as "internal, fixed | no [external write reachable]" (draft-D:154). But `ItemCount` is explicitly declared `editable` in state Cart in the same sample (samples/shopping-cart.precept:79-81), and is written from event data at runtime: `set ItemCount = ItemCount - (...) + AddItem.Quantity` (samples/shopping-cart.precept:125, 132, 154). Draft D's own §2 row 1 ("internal origin, varies at runtime → Prove-or-reject") or row 2 ("reachable by external write → Govern") should apply — not "Dead/flag." The worked example, applied against Draft D's own stated procedure, contradicts the verdict Draft D assigns it.

**B8. The flagship "Govern" worked example also fails against source, in the opposite direction.**
Draft D §1.2 (repeated at draft-D:152): `rule RenewalCount <= 2` is classified "Govern... external write reachable? yes." In `samples/library-book-checkout.precept`, `RenewalCount` is never externally written — no `editable` window includes it (only `BookTitle`, `DueDay`, `FineAmount` are editable, lines 47-49) and it is not a construction or event argument (`event Renew(ExtraDays as integer positive)`, line 70, supplies `ExtraDays` only). It is set exclusively by internal expressions: `set RenewalCount = 0` (lines 86, 128, 138) and `set RenewalCount = RenewalCount + 1` (line 94). Under Draft D's own §2 table this should route to Prove-or-reject, not Govern.

---

## ⚠️ CONCERNS

**C1. Two of Draft D's four cited "Error" diagnostic codes are documented elsewhere as not yet shipped at that severity, and Draft D drops the existing caveat.**
Draft D §1.3: "Verdict. Error... `ContradictoryRule` (PRE0155) and `UnsatisfiableRule` (PRE0159)" (draft-D:46). `src/Precept/Language/Diagnostics.cs:802,814` currently declares both `Severity.Warning`, not Error. The canonical `docs/compiler/diagnostic-system.md:191` already documents this honestly: "where a flipped code (...ContradictoryRule, UnsatisfiableRule) still emits Severity.Warning in Diagnostics.cs, that is **tracked drift**... this section states the target severity." Draft D states the target as though it were shipped, with no caveat.

**C2. "Decidability" is named as one of the three routing questions — the exact vocabulary v2 bans as a router.**
Draft D §2: "Three questions decide which disposition an obligation lands in... 3. **Decidability**. Is the constraint's truth settled by the definition alone, or does it depend on values that vary at runtime?" (draft-D:62-68). v2's banned-framings list states: "'Route by... decidability.' ...decidability is the discharge-oracle, not the router" (posture-replay-v2.md:91) and "Decidability decides discharge-vs-reject only — never route-to-governance." Mechanically Draft D's table doesn't appear to actually route govern-vs-prove-or-reject by question 3 (reachability alone does, per draft-D:79), but presenting decidability as one of "the three questions that decide disposition" revives exactly the framing v2's ban exists to prevent.

**C3. Ordinary runtime-varying guards (the common `when`-clause case) don't cleanly land in any of the four dispositions.**
`docs/language/precept-language-spec.md:1897`: "`when` guards are routing logic, not constraints. They do not produce violations... guards select; constraints enforce." A guard whose truth genuinely varies at runtime over an externally-reachable value isn't "internal, varies at runtime" (row 1 — a guard doesn't compute/hold a value), isn't "fixed at compile time" (rows 3/4), and while it superficially fits "reachable by external write" (row 2, Govern), §1.2's own description of governance mechanics — ingress checks and sweep-discard of a failing working copy (draft-D:34-38) — does not describe guard match/no-match semantics at all. Draft D claims exhaustiveness ("every obligation... lands in exactly one of them," draft-D:12) but this common case isn't substantively covered by any row.

**C4. The origin of the four-disposition idea is an explicitly unratified working analysis, never checked against the two documents that most directly conflict with it.**
`frank-four-disposition-structural-analysis-2026-07-14.md` frontmatter states "status: Working — analysis only, no fixes yet applied," and its stated target is a different sibling draft ("hybrid-model-draft-C-fresh-structure-2026-07-14.md"), not the now-deleted root-level hybrid-model copy. Neither `hybrid-model-draft-discards-2026-07-14.md` nor `frank-self-critique-2026-07-14.md` — the two sibling QA docs specifically checked for this — raise a four-vs-two disposition-count question anywhere; the self-critique's findings are about a different axis (governance mechanism count, outcome-vs-epistemic conflation).

---

## Notes (NIT)

- **At least three live, unreconciled framings of "how many dispositions" coexisted in the repo at the time of this review**, all essentially same-day: the now-deleted root-level hybrid-model copy (two, explicit "no fourth"), `frank-go-forward-posture-replay-2026-07-14-v2.md` §4/§10 (two active routes + one disclosed out-of-axis park, "that is the exact count"), and Draft D (four). File mtimes confirm the root-level copy was refined in place earlier the same session and Draft D was authored last (per `docs/Working/posture-v2-support/hybrid-model-draft-discards-2026-07-14.md:3`), yet Draft D engages with none of the prior framings.
- Draft D introduces "dispositions" as a unifying umbrella term across fault-freedom obligations and rule/guard-satisfiability obligations that neither `compiler-and-runtime-design.md`'s three-mechanism framing nor spec §0.6's eleven proof-system responsibilities use — a plausible classification on its own terms, but new vocabulary proposed for canonical promotion without reconciliation against the three sibling framings above.
- §7's reference-table quote for `ConversionFactor` — `field ConversionFactor as decimal default 1 positive editable` (draft-D:151) — omits the `maxplaces 12` modifier present in the actual declaration (`samples/unit-of-measure-reference.precept:41`). Doesn't affect the point being illustrated but is not the verbatim quote the backtick formatting implies.

---

## Findings checked and rejected (do not act on these)

The following were raised during review and did not survive adversarial verification: absence of design-doc rigor sections (Stakes/Falsifiers/etc.); a Pre-Design Owner Consultation gate violation claim; an implicit §8-vs-spec-§0.7 mapping complaint; absence of comparable-systems engagement; a "dead/flag vs. graph-analyzer 'dead'" naming-conflation claim; a `<-`-computed-field-with-fixed-upstream routing-ambiguity claim; and a duplicate framing of the disposition-count conflict as also violating Frank's authoritative ruling (subsumed into B1 above).

---

## What this review did NOT cover

1. **Nothing was run through the live compiler.** All fragment-level claims (diagnostic codes, "folds to always-true," severities) were checked by static textual comparison against source files, not by executing Draft D's inline `.precept` fragments through `precept_compile` to confirm actual emitted codes/severities.
2. **The earlier drafts in the lineage were never opened.** `hybrid-model-draft-A-frank-subagent-2026-07-14.md`, `-A2-diversity-edits-`, and `-B-fable-coordinator-` were never read; only Draft C's associated critique docs were checked. Whether the two-vs-four tension was raised and dropped earlier in A/A2/B is unknown.
3. **Only two of Draft D's cited diagnostic codes were verified against source** (`ContradictoryRule`/PRE0155, `UnsatisfiableRule`/PRE0159). `VacuousRule`/PRE0154, `TautologicalGuard`/`UnsatisfiableGuard`, PRE0136, PRE0082, and PRE0153 were never confirmed to exist, be live, or carry the severity Draft D asserts.
4. **Several sibling QA docs in the same folder were only grep/frontmatter-checked, not fully read**: `coverage-matrix-2026-07-14.md`, `citation-audit-2026-07-14.md` (partial), `citation-dilution-check-2026-07-14.md`, `independent-check-2026-07-14.md`, `ingress-scope-verification-2026-07-14.md`, `blind-reconstruction-2026-07-14.md`. Their scoping (that they target the posture-replay doc rather than Draft D) was inferred, not confirmed by full read.
5. **No lens assessed whether Draft D's status ("candidate for canonical promotion") without owner consultation is itself a process violation** under CLAUDE.md's Pre-Design Owner Consultation gate (Tier 3 applies to proposals conflicting with a locked decision in a doc with prior canonical status) — this review reports the content contradiction (B1) but did not separately evaluate process compliance.

---

**Verdict: Remediation required.**

Eight BLOCKER findings survived adversarial verification: an unreconciled, uncited contradiction with both the now-deleted root-level hybrid-model copy and the designated primary-authority posture ruling on the central "how many dispositions" question; a title collision with the existing canonical doc at the time; a direct conflict with that doc's own provenance guardrail and worked example; an unqualified overflow-enforcement claim that the primary authority explicitly forbids any doc from making; a missing MVP/build-status caveat that the review's own grounding docs treat as load-bearing; and a fabricated sample citation that anchors both the "Dead/flag" and — via routing misapplication — implicitly the "Govern" disposition, compounded by a second flagship worked example (RenewalCount) that also fails against its cited sample. None of these are matters of style; each was independently spot-checkable against the cited sources at review time.
