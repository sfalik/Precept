# Architecture Research

External research on architectural patterns for Precept's clean-room redesign. All files are raw research — no Precept-specific interpretations, no design conclusions, no implementation recommendations.

## Structure

| Location | Purpose |
|----------|---------|
| `compiler/` | 15-survey external research corpus for the clean-room compiler redesign. Covers pipeline architecture, proof systems, type systems, numeric/temporal/unit types, state graph analysis, LS integration, and runtime APIs. See [compiler/README.md](compiler/README.md) for reading order. |
| `runtime/` | Runtime evaluator architecture survey covering 10 external systems across 8 dimensions (object architecture, evaluator design, fault representation, compile-time/runtime fault correspondence, versioning, inspect/preview, result types, constraint evaluation). See [runtime/README.md](runtime/README.md). |
- ✅ Stateless validation justifies our `static partial` choice (vs. Roslyn's instance partials, which chain through binder context)

**Where we diverge from precedent (minor, not blocking):**

- 🟡 **Helpers centralization** — Roslyn distributes helpers near consumers; we centralize 29 methods in `Helpers.cs`. Defensible because our helpers are genuinely cross-cutting stateless utilities, but worth distributing if `Helpers.cs` grows past ~600 LOC.

**Open questions (for future design review, not action items):**

1. Should we formalize phases explicitly (Kotlin K2 model: RAW → Narrowing → ProofChecks → Diagnostics)? Would unlock incremental compilation and IDE responsiveness, but adds orchestration complexity.
2. Should diagnostics be deferred to a dedicated phase rather than collected during analysis? Cleaner separation, but bookkeeping cost.
3. Should the Main file be split if it grows past ~2,000 LOC? Currently 1,260 — comfortable, but worth watching.

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
