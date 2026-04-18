## Core Context

- Owns PM framing, roadmap sequencing, proposal structure, and reviewer-facing positioning across the Squad workflow.
- Keeps proposal work tied to philosophy fit, durable taxonomy, and clear implementation/review sequencing.
- Historical summary (pre-2026-04-13): shaped language-research batching, proposal/body standards, expressiveness vs compactness tagging, named-rule positioning, and PM analysis for event hooks and related roadmap decisions.

## Learnings

- Prevention-first languages cannot casually overload enforcement-shaped keywords for passive constructs.
- Proposal issues land best with one durable structure and clearly labeled hypothetical syntax.
- Roadmap labels and workflow metadata are most useful when taxonomy, ownership, and exceptions stay separate.
- Principle 8 PM stance (2026-04-17): in a prevention-first product, compiler uncertainty cannot be silently treated as safety. `Compiles clean` must mean no unresolved proof gaps on guarantee-bearing surfaces; if proof runs out, surface it explicitly or require a loud escape hatch rather than assuming satisfiable.
- Strict prevention-first products can train authors to adapt to a bounded checker, but only when the proof boundary is legible, diagnostics teach the accepted reformulations, and any escape hatch is loud enough that "verified" still means something.
- Real entity-governance arithmetic is dominated by thresholds, ranges, ratios, bracket tables, bounded counts, and capped totals; the PM bar should optimize the proof engine for those patterns and treat actuarial, optimization, and tax-engine computation as upstream values for Precept to govern, not re-compute.
- Proof-engine roadmap work should be judged as a product surface, not just a compiler milestone: diagnostics, hover, MCP, docs, and examples need the same truth model and wording if users are going to trust the guarantee.
- For guarantee-bearing features, performance is part of scope completeness. A bounded proof engine still needs explicit compile/hover latency acceptance bars before it is release-ready.
- In a prevention-first DSL, only examples beyond the canonical trust-building sample are optional. Strategic capabilities need at least one durable exemplar in samples/docs at launch.

## Recent Updates

### 2026-04-18 — Milestone restructuring recommendation
- Recommended closing M1 (Governed Integrity) and M2 (Full Entity Surface) — both effectively complete. Deferred #11 does not hold M2 open.
- Recommended retiring M3 (Expression Power) — shipped scope complete, 4 open issues redistributed to new milestones.
- Proposed 4 new milestones: M4 Proven Safe (#106, #111, #115), M5 Real-World Types (#107, #26, #95, #15, #61), M6 Expressive Governance (#112, #65, #58, #80), M7 Language Integrity (#93, #92, #86, #117).
- Ordering matches Shane's stated priorities: proof → types → expressiveness → integrity.
- Key insight: #112 (stateless events) is the most independent high-value issue — it's the pressure-relief valve if proof or type work stalls.
- Product story framing: each milestone boundary delivers a complete, pitchable capability narrative.

### 2026-04-18 — Language project prioritization assessment
- Assessed all 30 items on the Language Improvements project board, focusing on the 7 newly-added issues.
- Shane's proposed sequence (#111 → #107/#26 → #95) is sound but gated on PR #108 closing.
- #111 and #106 are tightly coupled — #111 literally consumes the interval infrastructure PR #108 builds. No overlap possible; strict dependency.
- #107 (temporal) subsumes #26 (date-only). If #107 is too large, #26 is the extractable MVP.
- #95 (currency/UOM) depends on the postfix literal grammar #107 introduces — must follow.
- #112 (stateless events) is the most independent of the top proposals and could be pulled forward if proof work stalls.
- Board assignments: #111 Ready, #112 Ready, #117 Backlog, #107 Backlog, #95 Backlog, #92 Backlog, #61 Backlog.
- Key risk: Phase 2 review blockers on PR #108 (7 reviewers, all filed CHANGES_NEEDED except Frank's conditional approval) are the critical path constraint.

### 2026-04-12 — Event hooks PM analysis
- Recommended a two-proposal split: advance stateless event hooks first and defer stateful hooks until execution-order and scope questions are resolved.

### 2026-04-11 — Named rule keyword analysis
- Recommended avoiding `rule` for passive named predicates because it silently implies enforcement in a prevention-first product.
