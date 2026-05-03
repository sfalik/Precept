# Architecture Decision: Parser Rebuild vs. Incremental Improvement

**Author:** Frank (Lead/Architect)  
**Date:** 2026-05-02  
**Requested by:** Shane  
**Status:** DECISION — not a review, not a proposal

---

## Context

Shane's question: "Before we charge off and build the type checker, should we delete the parser and re-implement as a radical idea?"

This is a sequencing and risk question. I have three documents in front of me: my catalog-driven parser analysis (optimistic), my catalog-driven type checker analysis (also optimistic), and George's implementer reality-check (honest). George caught a factual error in my work and deflated two of my sizing claims. That calibrates my confidence appropriately.

---

## 1. The Core Tradeoff

### What the team loses by keeping the current parser as-is:

- **Dual-paradigm debt**: 3 of 4 direct constructs already use the generic `ParseConstructSlots` path. `ParseFieldDeclaration` is the outlier. This is a small irritant, not a crisis.
- **~120 lines of throw-arm ceremony**: The action-kind switches are pure noise — no behavior, just CS8524 ceremony. These will actively impede Slice 5 of the type checker because the checker must pattern-match on the current AST's per-kind `Statement` subtypes.
- **Drifting hand-maintained sets**: `StructuralBoundaryTokens` is the real ticking bomb — add a new slot kind and expression parsing silently breaks.
- **Two hardcoded precedences**: `.` = 80 and `(` = 90 in the Pratt loop. Minor technical debt, not a crisis.

### What the team loses by rebuilding it before the type checker:

- **Time**: George's honest estimate is 150-200 lines of new slot interpreter + residual special-case handling. That's 1-2 weeks of careful work against a regression surface of 2000+ tests.
- **Risk surface**: The stashed-guard pattern, split-modifier problem, and inline variant-action detection are three unsolved design gaps. Any one of them could stall a rebuild mid-flight.
- **Velocity on the priority work**: The type checker is the next milestone. It delivers user-visible value (diagnostics, better LS features). A parser rebuild delivers architectural purity.

### Is "works and is tested" enough of a reason to leave it alone?

**No — but it IS enough reason not to delete it.** "Works and is tested" earns the parser the right to be improved incrementally rather than replaced wholesale. The parser isn't wrong; it's over-specified in places where catalog metadata could do the work. That's an improvement opportunity, not a rewrite justification.

---

## 2. George's Reality-Check Impact on My Confidence

George identified three edge cases my radical proposal didn't fully address:

1. **Stashed-guard pattern**: The pre-anchor optional clause consumed before disambiguation — my slot-sequential model has no clean answer for tokens parsed out-of-order.
2. **Split-modifier problem**: Modifiers appearing both before AND after the compute expression in `ParseFieldDeclaration` — doesn't fit a flat slot sequence without new metadata design nobody has proposed yet.
3. **Inline variant-action detection**: `meta.Kind == ActionKind.Remove` mid-parse — this is per-kind routing the catalog doesn't currently express.

George also caught that `is set`/`is not set` already reads precedence from the catalog. I claimed otherwise. I was wrong.

**Impact on confidence**: My radical "Declarative Grammar Machine" proposal (§5.1 in my parser analysis) drops from "compelling vision" to "aspirational P3." The 80-line claim was wrong by 2×. The edge cases are real and unsolved. I still believe the architecture is sound as a *direction*, but George proved it's not ready as a *plan*.

The targeted improvements (P1 items) remain fully valid. George confirmed every one. The radical rewrite was the over-reach, not the diagnosis.

---

## 3. The Sequencing Question

George's key insight: **uniform action-shape `Statement` nodes MUST land before type checker Slice 5.** This is the binding constraint.

The three paths:

| Path | What it means | Risk | Timeline impact |
|------|---------------|------|-----------------|
| **A: Type checker first** | Build checker against current AST, refactor parser later | Slice 5 hits the action-kind node zoo and must either work around it or pause for a parser refactor mid-implementation | Low risk initially, high cost at Slice 5 |
| **B: Full parser rebuild** | Delete and reimplement, then build checker | 2000+ test regression surface, 3 unsolved design gaps, 1-2 weeks minimum before checker work resumes | High risk, high delay |
| **C: Targeted parser improvements** | P1 items only (action-shape consolidation, `StructuralBoundaryTokens`, `TypeParseShape`), then type checker | Solves the Slice 5 blocker, removes real debt, keeps the parser testable throughout | Low risk, 2-3 day delay |

George's ordering constraint kills Path A as stated: you can't build Slice 5 against per-kind action nodes without double-work. Path B is overkill for a constraint that Path C solves directly.

**Path C is the correct answer.**

---

## 4. The "Delete and Reimplement" Question Directly

### Is this the right move architecturally?

**No.** Not now, and probably not ever as a single atomic operation.

The parser is ~600 lines of production code (across 3 partial class files) with 2000+ passing tests. It is not broken. It has cosmetic debt (throw-arms, 2 hardcoded precedences, one hand-maintained set) and one structural gap that blocks downstream work (per-kind action nodes). The structural gap has a clean incremental fix.

### What's the risk profile?

- **Regression surface**: 2000+ tests. Every single one must pass after a rewrite.
- **Unsolved design gaps**: 3 (stashed-guard, split-modifier, variant-action triggers). A rewrite would need to solve all three simultaneously.
- **Knowledge loss**: The current parser embeds implicit knowledge about Precept's grammar edge cases. That knowledge isn't documented — it's in the code. A clean-room rewrite risks rediscovering edge cases the hard way.
- **Opportunity cost**: 1-2 weeks of rebuild time is 1-2 weeks not building the type checker.

### Is there a version that's additive/incremental rather than destructive?

**Yes, and it's the only version I'd endorse.** The path:

1. Consolidate action-shape nodes (P1, ~1 day)
2. Derive `StructuralBoundaryTokens` from slot metadata (P0, ~2 hours)
3. Add `TypeParseShape` DU to `TypeMeta`, refactor `ParseTypeRef` (P1, ~1 day)
4. Build the type checker (Slices 0-11)
5. THEN revisit the Declarative Grammar Machine as a post-checker P3 initiative, when the grammar's edge cases are better-documented by having two pipeline stages consuming the AST

Each step is independently testable, independently revertable, and leaves the parser green at every point.

### Does the current parser's dual-paradigm pattern cause actual pain during type checker implementation?

**Only at Slice 5 (action typing).** The per-kind action nodes force the checker to switch on `ActionKind` to extract fields — the same ceremony the parser has. George's insight is correct: if we consolidate to per-shape nodes before Slice 5, the checker never has to write that ceremony. The rest of the parser's structure is invisible to the type checker.

---

## 5. Recommendation

**Path C. Targeted parser improvements, then build the type checker. No delete. No rewrite.**

Specific sequencing:

### Phase 1: Parser Prerequisites (2-3 days)

1. **Derive `StructuralBoundaryTokens` from slot metadata** — immediate, P0, prevents drift
2. **Consolidate action-shape `Statement` types** — one `Statement` subtype per `ActionSyntaxShape`, carrying `ActionMeta`. Eliminates ~120 lines of throw-arms AND unblocks Slice 5
3. **Add `ActionMeta.VariantTriggerToken?`** — solves the inline kind-identity problem George identified (§3.1)
4. **Add `TypeParseShape` DU to `TypeMeta`** — benefits both parser (`ParseTypeRef` shrinks) and checker Pass 1

### Phase 2: Type Checker Implementation (primary milestone)

5. Build Slices 0-11 as specified in `docs/compiler/type-checker.md`
6. Precomputed operation/function resolution tables land in Slice 0 (P0 items)
7. `ScopeRule` on `ConstructMeta` lands before Slice 3
8. `TypeMeta.LiteralRange?` lands before Slice 4
9. `ActionMeta.TypedActionShape` lands before Slice 5

### Phase 3: Parser Evolution (post-checker, P3)

10. After all 11 checker slices are green and the grammar's edge cases are fully exercised by two consumers, *then* evaluate whether the Declarative Grammar Machine has merit
11. By that point, the split-modifier design will have been forced by real implementation pressure, not speculative metadata design

### The principle behind this call:

**Don't rebuild infrastructure to satisfy architectural aesthetics before building the thing that reveals whether the aesthetics are correct.** The type checker will exercise the AST, expose real edge cases, and tell us which parser patterns cause actual pain vs. which ones are merely inelegant. Build the consumer first, then refactor the producer with empirical evidence.

The parser is imperfect but functional. The type checker doesn't exist yet. Ship the thing that doesn't exist. Improve the thing that does exist where it directly blocks that shipping. Leave the rest for when you have evidence, not theory.

---

*This is my call as architect. I was wrong about the 80-line claim and wrong about `is set` precedence. George kept me honest. The radical vision is directionally correct but not implementation-ready. The targeted path gives us catalog-driven improvements NOW, unblocks the type checker, and preserves the option for deeper refactoring later — with evidence.*
