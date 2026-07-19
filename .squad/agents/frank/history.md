## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for Precept.
- Catalog metadata stays the language truth; tooling and runtime derive from it instead of hardcoded parallel rules.
- Work is only complete when docs, diagnostics, samples, and downstream tooling match shipped behavior.

## Live Guidance

- Stress-test any “compile-time only” claim against ingress, Fire/Update, and runtime-governance paths before hardening language or docs around it.
- When the grammar can make an invalid form impossible, prefer structural exclusion over later semantic prohibition.
- Exact vs approximate behavior must stay visible in the type system and public surface.

## Historical Summary

- 2026-05 through 2026-06: concentrated work on constructor semantics, quantity normalization, compiler-readiness review, and the arg-constraint consult. Durable detail lives in `.squad/decisions.md`; this file keeps only posture and high-value learnings.
- 2026-07-06: guarantee-model ruling work converged on Hybrid as Precept's governing boundary; the durable lesson is to route by value origin, never by proof difficulty.

## Recent Updates

### 2026-07-14T22:35:00Z — Posture-v2 strengthening closeout recorded

- Frank first produced `docs/Working/posture-v2-support/frank-self-critique-2026-07-14.md`, surfacing 7 findings (H1-H4, M1-M3) against his own v1 posture replay.
- He then authored `docs/Working/frank-go-forward-posture-replay-2026-07-14-v2.md`, a materially strengthened rewrite with 13 sections, 17 tracked changes (C1-C17), and a changelog appendix, informed by five independent inputs: citation audit, devil's-advocate memo, coverage matrix, blind reconstruction, and self-critique.
- George's independent follow-up check judged v2 ready for Shane's sign-off, with one explicit owner-gated open item: M3, where the separate canonical-capture recommendation still carries the overruled “proof-carrying vs. not” framing and needs Shane's correction before Step 1 runs.

### 2026-07-19T03:56:26Z — `what-i-want-2026-07-16` advisory review logged

- Produced `docs/Working/frank-review-what-i-want-2026-07-16-opinion.md` for Shane.
- Verdict: **REWORK-then-GO**.
- Closed the two open forensic forks and most adversarial/collation findings, but flagged the missing scope line / stopping rule, the live canon contradiction around deleting the post-mutation sweep, an overflow honesty gap, and two philosophy escalations.

## Learnings

- A strengthening review needs independent pressure from multiple angles (citation integrity, coverage completeness, adversarial gaps, blind reconstruction, and self-critique), not just a single audit memo.
- Owner-sensitive framing should be called out explicitly when a support doc still preserves language that the main posture has already overruled.
- Keep big histories summarized aggressively; the durable narrative belongs here, while batch chronology belongs in the decision ledger and orchestration logs.
