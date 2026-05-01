## Core Context

- Owns code-level feasibility, runtime implementation detail, and architecture-to-code translation across parser, checker, analyzer, and tooling surfaces.
- Co-owns language research/design grounding with Frank and converts approved language decisions into implementable parser/catalog structures.
- Historical summary: closed catalog extensibility hardening, PRECEPT0018 analyzer enforcement, and parser whitespace-insensitivity implementation while keeping catalog truth primary.

## Learnings

- Eliminate hardcoded parallel copies of catalog knowledge; derive parser/checker behavior from metadata whenever the behavior is part of the language contract.
- Exhaustiveness guarantees need explicit compiler enforcement (`#pragma warning disable CS8524` + no wildcard arms) plus pinned regression tests.
- Use `None = 0` only for real structural sentinels; otherwise make named enum members 1-based so zero-initialization fails loudly.
- Shared catalog signature changes require a full constructor-call audit before compiling.
- Qualifier parsing needs two gates: confirm the type even supports qualifiers, then use catalog-derived lookahead for ambiguous prepositions.
- Whitespace-insensitivity belongs at the trivia-stripping boundary, not in ad hoc parser escape hatches.
- Multi-qualifier scalar types should be modeled as immutable collections, not stacked nullable singletons.
- Parser-facing type properties that reflect durable language truth belong in catalog traits (`TypeTrait.ChoiceElement`), not hand-maintained token lists.
- A slice is only complete when docs, diagnostics, tests, and samples all still agree on the contract.

## Recent Updates

### 2026-05-01 — WSI parser slices 2–5 recorded
- Deleted the dead `SkipTrivia()` path and removed `NewLine` from `StructuralBoundaryTokens`, making parser structure explicitly trivia-free.
- Rewrote `ParseTypeRef` for multi-qualifier scalar types, with `AmbiguousQualifierPrepositions` derived from catalog metadata rather than hardcoded token tables.
- Added `TypeTrait.ChoiceElement` and derived `ChoiceElementTypeKeywords` from catalog truth.
- Validation recorded at 2310 passing tests.

### 2026-04-29 — PRECEPT0018 correctness gate closed
- Follow-up commit `e7a643d` added the 5 required regression anchors from Frank's blocked review without changing analyzer behavior.
- Durable closeout rule: when a reviewer names missing spec IDs, backfill by spec ID rather than by matching only the requested count.

### 2026-04-28 — Catalog extensibility and enum-safety hardening
- Landed catalog-driven parser routing/action-shape metadata, exhaustive CS8509 switch enforcement, and access-mode keyword derivation.
- Landed PRECEPT0018 analyzer enforcement so semantically meaningful zero-valued enum members are either explicit sentinels, `[Flags]`, or annotated exemptions.
