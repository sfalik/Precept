# Architecture Research

External research on architectural patterns for Precept's clean-room redesign. All files are raw research — no Precept-specific interpretations, no design conclusions, no implementation recommendations.

## Start here

- **[`../INDEX.md`](../INDEX.md)** — cross-corpus topic-to-file map. First stop for "has this been researched?".

## Structure

| Location | Purpose |
|----------|---------|
| `compiler/` | External research corpus for the clean-room compiler redesign. Covers pipeline architecture, proof systems, type systems, numeric/temporal/unit types, state graph analysis, LS integration, and runtime APIs. See [compiler/README.md](compiler/README.md) for reading order. |
| `runtime/` | Runtime evaluator architecture survey covering 10 external systems across 8 dimensions (object architecture, evaluator design, fault representation, compile-time/runtime fault correspondence, versioning, inspect/preview, result types, constraint evaluation). See [runtime/README.md](runtime/README.md). |
| `tooling/` | Tooling-architecture audits (MCP, language-server, grammar generator) — moved in Phase 10 from `research/language/`. |
- ✅ Stateless validation justifies our `static partial` choice (vs. Roslyn's instance partials, which chain through binder context)

**Where we diverge from precedent (minor, not blocking):**

- 🟡 **Helpers centralization** — Roslyn distributes helpers near consumers; we centralize 29 methods in `Helpers.cs`. Defensible because our helpers are genuinely cross-cutting stateless utilities, but worth distributing if `Helpers.cs` grows past ~600 LOC.

**Open questions (for future design review, not action items):**

1. ~~Should we formalize phases explicitly (Kotlin K2 model: RAW → Narrowing → ProofChecks → Diagnostics)? Would unlock incremental compilation and IDE responsiveness.~~ **Closed 2026-05-29 — NO (for the incremental/IDE-responsiveness motivation).** Benchmark: the full pipeline compiles the 76-sample corpus in ~44 ms; worst single file ~3 ms (warm, Release). A full recompile-on-keystroke is imperceptible, so the incremental-compilation argument for explicit phases does not hold. See [`project_compiler_fast_no_incremental`] (memory) and the benchmark in the 2026-05-29 session. (Diagnostic-phase formalization survives on *separation* grounds — see #2 — not speed.)
2. **Should diagnostics be deferred to a dedicated phase rather than collected during analysis?** **Open, now evidence-backed (2026-05-29).** `research/architecture/compiler/flow-sensitive-check-placement-survey.md` + `type-proof-stage-contract-survey.md` found 3 of 4 production compilers (Kotlin K2 `CHECKERS`, Rust MIR reporting, Roslyn) defer emission to a terminal phase. Tracked as **readiness-plan Phase 8 Slice 3** — which resolved it as catalog-declared single-stage **ownership** + a Roslyn analyzer, *not* a dedicated terminal phase. Companion slices: **Slice 1** emission inventory/classification, **Slice 2** code-mediation standardization (catalog-mediated diagnostic codes — `proof-engine.md` Decision 3), **Slice 4** counterexample/witness richness.
3. Should the Main file be split if it grows past ~2,000 LOC? Currently 1,260 — comfortable, but worth watching.

**Settled architecture direction (2026-05-29, owner-confirmed):** Precept **stays with the catalog-driven stamp/self-derive type↔proof contract** — it is well-precedented for Precept's *kind* (general DSL pipeline with proof as one stage: Rust borrowck, Roslyn nullable, K2), and Precept's catalog-metadata sourcing is a cleaner refinement. **VC-generation / intermediate-verification-language is rejected** — an IVL reintroduces the SMT non-determinism `proof-engine.md` excludes for inspectability/determinism. Grounded in `type-proof-stage-contract-survey.md` § Conclusions. Tension to monitor (not act on): Precept is architecturally the self-deriving *kind* but its prevention-as-structural-guarantee *ambition* is the dedicated-verifier kind; a Whiley deep-dive (the closest self-deriving-verifier analog) is the horizon research if that tension ever forces a revisit.

### Re-evaluation Triggers

Revisit this research if:

- Any single file exceeds 2,000 LOC
- `Helpers.cs` grows past 600 LOC
- A new analysis domain emerges that doesn't fit the current 6 seams cleanly
- Total `PreceptTypeChecker` LOC exceeds 5,000
- We adopt incremental compilation or IDE-time partial re-checking (would push us toward Kotlin K2's phase model)

### Source Coverage

8 production type checkers surveyed across both angles:

- **Roslyn** (C#) — primary precedent, covered by both researchers
- **TypeScript** (`checker.ts`) — Frank's monolithic counter-example
- **Rust** (`rustc_hir_typeck`, `rustc_infer`) — Frank's phase-based crate split
- **Swift** (`lib/Sema/`) — Frank's fine-grained file-per-concern
- **Kotlin K2** (FIR) — Frank's strongest precedent for phase model
- **F#** (`src/Compiler/Checking/`) — covered by both, closest functional analog
- **NRules** — George's visitor-pattern counter-example
- **DynamicExpresso** — George's monolithic-DSL counter-example
