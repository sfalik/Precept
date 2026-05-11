# Frank-22: Fix 7 vs. PRECEPT019 — Verdict

**Date:** 2026-05-11  
**Agent:** frank-22 (claude-opus-4.6)  
**Question:** Does PRECEPT019 (Roslyn analyzer from PR #133) supersede or complement Fix 7's runtime dispatch loop in GraphAnalyzer?

## Analysis

### What PRECEPT019 Does
PRECEPT019 is a Roslyn analyzer (PR #133, on `feature/issue-132-alignment-enforcement`) that enforces method-level catalog exhaustiveness via `[HandlesCatalogExhaustively]` attribute annotations. It fires at compile time when a method annotated with the attribute doesn't handle all catalog members.

### What Fix 7 Does
Fix 7 adds an unconditional `throw new InvalidOperationException(...)` as the default case in the event modifier dispatch loop in `GraphAnalyzer.cs`. This fires at runtime if a new `GraphAnalysisKind` member is added without updating the dispatch loop.

### Why PRECEPT019 Does NOT Supersede Fix 7

1. **Scope mismatch**: PRECEPT019 enforces method-level coverage. It has zero visibility into an **inline switch inside a loop** — it can only annotate whole methods, not individual switch expressions within them.

2. **Proportionality**: `GraphAnalysisKind` is a two-member enum with a single dispatch site. The unconditional throw is minimal, self-enforcing, and proportionate.

3. **Defense in depth**: Even if PRECEPT019 were extended to cover inline switches, the runtime throw provides a second layer — it catches cases where the analyzer is bypassed (e.g., reflection, generated code).

## Decision

**Fix 7 stands as-is.** Do not remove or replace with PRECEPT019.

Revisit if `GraphAnalysisKind` grows to 4+ members across multiple dispatch sites — at that scale, PRECEPT019-style annotation becomes the better tool.
