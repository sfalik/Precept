# Architecture Research

External research on architectural patterns, evaluated against Precept's current implementation.

## Structure

| Location | Purpose |
|----------|---------|
| `compiler/` | 15-survey external research corpus for the clean-room compiler redesign. Covers pipeline architecture, proof systems, type systems, numeric/temporal/unit types, state graph analysis, LS integration, and runtime APIs. See [compiler/README.md](compiler/README.md) for reading order. |
| *(root files below)* | Earlier architecture research: type checker architecture surveys and synthesis for the existing implementation. |

## Files

### Type Checker Architecture (original survey)

| File | Author | Angle |
|------|--------|-------|
| [typechecker-architecture-survey-frank.md](typechecker-architecture-survey-frank.md) | Frank (Lead/Architect) | Architectural patterns in 6 production type checkers (Roslyn, TypeScript, Rust, Swift, Kotlin K2, F#) |
| [typechecker-implementation-patterns-george.md](typechecker-implementation-patterns-george.md) | George (Runtime Dev) | .NET implementation patterns at file/class/method level (Roslyn, F#, NRules, DynamicExpresso) |

### Architecture Foundation Research (2026-04-19)

| File | Author | Priority | Angle |
|------|--------|----------|-------|
| [compile-time-gate-pattern.md](compile-time-gate-pattern.md) | Frank | 🔴 High | Smart constructor / opaque validated type — formal grounding for "prevention, not detection" |
| [proof-engine-abstract-interpretation.md](proof-engine-abstract-interpretation.md) | Frank | 🔴 High | Precept's proof engine vs. abstract interpretation theory (Cousot, Miné octagon domain, ASTRÉE) |
| [atomic-working-copy-pattern.md](atomic-working-copy-pattern.md) | George | 🔴 High | Constraint-gated functional update — working-copy pattern vs. Redux, Datomic, DDD aggregates, MVCC |
| [first-match-routing-state-machine-theory.md](first-match-routing-state-machine-theory.md) | Frank | 🟡 Medium | First-match guard routing vs. Harel Statecharts, SCXML DocumentOrder, XState |
| [outcome-taxonomy-result-types.md](outcome-taxonomy-result-types.md) | George | 🟡 Medium | 8-outcome taxonomy completeness — Railway-Oriented Programming, Rust Result<T,E>, typed error models |
| [incremental-compilation-patterns.md](incremental-compilation-patterns.md) | George | 🔵 Low | salsa, Kotlin K2 FIR, Roslyn red/green tree — when Precept needs incremental compilation |
| [single-pass-type-checking.md](single-pass-type-checking.md) | Frank | 🔵 Low | Formal conditions for single-pass soundness — bidirectional typing, Dunfield-Krishnaswami survey |

## Synthesis

### Question

Is Precept's current 6-partial-class `PreceptTypeChecker` split (3,783 LOC across Main, TypeInference, Narrowing, ProofChecks, Helpers, FieldConstraints) good architecture, or should it be re-evaluated?

### Combined Verdict: KEEP AS-IS

Both research angles independently arrived at the same conclusion: the current split is sound and aligned with production precedent.

**Where we match precedent:**

- ✅ Partial-class split by responsibility — matches Roslyn (`Binder.<Concern>.cs`)
- ✅ File sizes well below precedent ceilings — max 1,260 LOC vs. Roslyn's 11,841 LOC, F#'s 2,500 LOC
- ✅ Switch-on-NodeKind dispatch — matches Roslyn (visitor pattern would add ceremony without benefit at our scale)
- ✅ Front-matter types co-located with main partial — standard .NET convention
- ✅ Horizontal layering (Narrowing → ProofChecks → TypeInference) — matches Kotlin K2 and Rust's phase model
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
