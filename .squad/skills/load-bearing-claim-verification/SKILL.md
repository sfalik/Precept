---
name: "load-bearing-claim-verification"
description: "Review discipline for plans/designs that gate downstream work and rest their safety argument on factual claims about source code. Spot-check every load-bearing claim against source BEFORE accepting the artifact's own confidence calibration."
domain: "architecture review, plan review, fault-floor / proof-engine / registry verification"
confidence: "high"
source: "earned — compiler-readiness-plan-2026-06-11 review (Frank, 2026-06-16)"
---

# Skill: Load-Bearing Claim Verification

## The Problem

A plan that gates all downstream work (e.g., "the compiler must reach 100% on a sound floor before runtime starts") often rests its entire safety argument on a handful of factual claims about the *current* state of source code:

- "X has ZERO consumers today, so retiring Y is safe."
- "The registry currently routes Z to the generic backstop."
- "This suppression is sound because BuildEdges ignores the guard."

These claims are the load-bearing beams. If any is false, the strategy can collapse — yet they are easy to accept because they *sound* authoritative and the plan usually self-rates them HIGH confidence. **A plan's confidence calibration is not evidence; it is a claim about evidence.** Accepting it without checking is how a wrong premise becomes a locked execution contract.

## The Pattern

**Before accepting the verdict, extract every load-bearing factual claim and verify it against source. Then state explicitly what you verified vs. what you took on faith.**

### 1. Extract the load-bearing claims

Read the "Decisions captured" / "Risks & calibration" / prerequisite sections and pull out every claim of the form "the code currently does/doesn't X." Prioritize claims that *license an irreversible move* (a deletion, a retirement, a "this is safe because…").

### 2. Verify each against source — not against the plan's own appendix

Use `grep`/`view`/MCP to hit the actual file. Distinguish:

- **Literal truth** — is the claim true as worded?
- **Load-bearing truth** — is the *thing the argument needs* true, even if the wording is loose?

A claim can be load-bearing-true and literally-false (e.g., "ZERO consumers" when 2 cosmetic/display consumers exist but no *enforcement* consumer does). That gap is a finding: on a linchpin claim, imprecise wording must be tightened even when the soundness holds.

### 3. Check the arithmetic and the provenance

Coverage ledgers ("every item placed; nothing lost") have countable claims. Add them up. Count the source set (e.g., triage disposition tags). Internal inconsistencies (28 vs 32) in a completeness guarantee are blocking even when the *placement* is correct — a ledger that misstates its own denominator undercuts the guarantee it exists to provide.

### 4. Separate "verified accurate" from "took on faith" in the output

End the review with a two-list ledger. This is post-hoc auditable and it calibrates the reader: "I checked the registry, the backstop, the suppression soundness; I did NOT re-adjudicate all 236 audit rows or run the unprobed temporal-overflow probe." Honesty about what you did NOT check is part of the rigor.

## Calibration: blocking vs advisory

- **Blocking** = a load-bearing claim that is inconsistent, false-as-stated on a linchpin, or an owner-gated set that is incomplete. These can let an executor/owner act on a wrong or partial premise.
- **Advisory** = imprecision that doesn't sit on a load-bearing beam (off-taxonomy status, a numeric collision, an unqualified path).

**Do not manufacture blocking findings to seem rigorous, and do not soften real ones to seem agreeable.** If the plan's homework is real, say so — verified praise ("I checked the linchpin; it holds") is more valuable than reflexive criticism. A clean plan with one substantive blocker and two precision fixes is a *better* outcome than a padded list.

## Worked example (compiler-readiness-plan-2026-06-11)

| Claim | Verified against | Result |
|---|---|---|
| "DesugarsToRule ZERO consumers" | `grep .DesugarsToRule src/ tools/ test/` | Load-bearing-TRUE (0 enforcement consumers), literally-FALSE (2 metadata reads) → blocking precision fix |
| "generic `_ => DivisionByZero` backstop" | `ProofEngine.Diagnostics.cs:490` | Confirmed |
| "ProofStrategy has no Vacuous; Literal-mislabel live" | `ProofLedger.cs:100`, `ProofEngine.cs:1181` | Confirmed (`Literal // vacuously proved`) |
| "suppression sound — BuildEdges ignores guard" | `GraphAnalyzer.cs:379-384` | Confirmed (guard recorded as metadata only) |
| coverage 619→236→67/126/43 | `spec-coverage-audit.md` totals | Confirmed |
| "28 superseded docs" vs "32 prior docs" | triage tag count (28 SUPERSEDE + 4 ARCHIVE) | Inconsistent → blocking provenance fix |

The plan's HIGH self-confidence was justified — but that was only knowable *after* the checks. The checks are the skill.

## When This Applies

- Reviewing any plan/design that gates a phase boundary or runtime start.
- Any artifact whose safety argument is "the code currently does/doesn't X."
- Registry / proof-engine / catalog / fault-floor changes where a deletion or retirement is licensed by a current-state claim.

## When It Doesn't

- Pure design proposals with no current-state factual dependency (verify the *reasoning*, not source).
- Artifacts where the claims are self-evidently non-load-bearing (style, naming).
