# Catalog-Driven Parser: Frank's Cross-Review of George's Estimate

**By:** Frank
**Date:** 2026-04-27
**Status:** Cross-review response — updated position after reading George's implementation estimate
**References:** `frank-catalog-parser-scope.md` (my scope), `george-catalog-parser-estimate.md` (George's estimate)

---

## 1. Does George's Estimate Change My Architectural Recommendation?

**Layers A, B, E — No change. George confirms these are the right calls.**

George sized Layer A at ~2 days with near-zero risk and called it "the highest ROI of all 5 layers." That matches my scope exactly. His inventory work is precise — 80 vocabulary entries across 4 tables, clean frozen dictionary derivations, two documented edge cases (`SetType` parser-synthesis exception, `Initial` dual-modifier resolution). Nothing here surprised me. The fact that `Operators.ByToken` already exists and the parser is still a stub means we're building catalog-driven from day one, not refactoring — which is exactly the cost advantage I expected.

Layer B and E: George bundles both into Layer A's timeline. I agree. The `ByLeadingToken` index is ~1 hour. Sync-point derivation is ~3 hours. Both are trivially correct and belong in the same pass.

**Layer D — George found costs I underestimated. My position changes.**

This is where George's estimate earns its keep. I'll address this fully in §2 below.

**Layer C — George reframed it. My position partially changes.**

Addressed in §3 below.

---

## 2. Layer D: Updated Position

### What George Found That I Didn't Adequately Weight

My scope called Layer D "the single biggest structural win" and advocated it with constraints (generic slot iteration + per-construct AST factories + self-contained slot parsers). George priced it at **4–5 weeks / ~154 hours** and flagged five specific risk vectors:

1. **`ActionChain` is a loop, not a slot.** The `->` separator is also the loop terminator when followed by `transition`/`no`/`reject`. I acknowledged slot boundary contracts in my scope but didn't price the complexity of encoding loop-stopping-conditions as catalog metadata. George is right: this requires either a `LoopSlot` DU subtype or special-case handling in the iterator. Neither is trivial.

2. **`Outcome` has 3 forms.** `-> transition <state>`, `-> no transition`, `-> reject <msg>`. George correctly identifies this as a sub-grammar. I glossed over this in my slot parser pseudocode. At the point where slot metadata needs to express sub-production alternatives, we're building a grammar DSL inside the catalog — which violates my own principle that catalog metadata should express *what* the language is, not *how* the parser navigates structure.

3. **`BuildDeclaration` factory fragility.** I specified per-construct AST factories as the type-safety preservation layer. George's concern about runtime type coercion and poor error messages on mismatch is valid. The factory is a runtime-checked mapping between a heterogeneous slot array and named record fields — exactly the kind of thing that fails silently until it doesn't.

4. **Scale mismatch.** 11 constructs, intentionally closed grammar. George's framing is blunt and correct: "A closed DSL with exactly 11 constructs doesn't need a generic production framework." My scope argued the win was structural — add construct #12 from a catalog entry. George's counter: construct #12 is a design-level decision requiring design review, language spec change, and full pipeline impact. The "one catalog entry and done" promise is aspirational, not operational.

5. **Regression surface.** Converting 11 proven parse methods to a generic framework means any bug in the slot-dispatch machinery affects all 11 constructs simultaneously. I didn't adequately price this risk.

### My Updated Position on Layer D

**I walk back Layer D.** George's estimate is honest and the costs are real.

Here's where I went wrong: I was seduced by the architectural elegance of "the catalog drives the parser's production sequence." That IS the textbook metadata-driven architecture win. But Precept is not a textbook. We have 11 constructs — a number that changes at the pace of language evolution, not at the pace of feature development. The abstraction cost (154 hours, 5 risk vectors, full regression rewrite) buys automation for a change frequency that's measured in years.

The more honest assessment: **`ConstructMeta.Slots` already IS the slot-driven parser — for tooling consumers.** LS completions iterate slots to suggest what comes next. MCP vocabulary exposes slot shapes. Grammar generation uses slot sequences. All of that value is captured by the existing `Slots` field. Wiring the parser runtime to iterate those same slots buys code-organization elegance at the cost of debuggability, type safety, and 4–5 weeks of high-risk work.

**New position: Layer D is architecturally appealing but operationally unjustified at Precept's grammar scale. The `Slots` metadata serves its consumers. The parser doesn't need to be one of them.**

I acknowledge this is a reversal. George's estimate made me confront the gap between "biggest structural win" and "biggest structural win *at this scale with these costs.*" The principle holds in the abstract. It doesn't hold for 11 constructs and 154 hours.

---

## 3. Layer C: Updated Position on `DisambiguationToken`

### My Original Position

I rejected Layer C entirely. My reasoning: the `when` guard re-dispatch breaks the flat-metadata model, the 4 disambiguation methods are small and stable (~60 lines total), and encoding disambiguation as catalog metadata invents a control-flow DSL inside the catalog.

### George's Narrower Framing

George didn't propose what I rejected. His recommendation is specifically:

> Add `DisambiguationToken: TokenKind[]?` to `ConstructMeta` for catalog completeness and tooling value. Document the `When`-intermediate exception. Parser methods reference it as metadata (comment/documentation) but continue to own their own parsing sequences.

This is a different proposal than "make the parser disambiguation table-driven." George is saying: the disambiguation token IS language structure, the catalog should describe it, and consumers (LS, MCP) should have access to it. The parser doesn't have to be restructured around it.

### Why This Changes My Position

My rejection was aimed at parser restructuring — replacing 4 clean methods with a table-driven generic dispatcher. George explicitly agrees that's not worth doing. His proposal is narrower: **add the metadata for consumers, not for the parser.**

Under the metadata-driven architecture principle, the question is: "Is the disambiguation token part of a complete description of the language?" Yes. When `from <state>` is followed by `on`, that's a `TransitionRow`. When followed by `ensure`, that's a `StateEnsure`. When followed by `->`, that's a `StateAction`. This IS language structure. The catalog should describe it.

George's specific observations strengthen the case:
- MCP can answer "what constructs can follow `from <state>`?" — the index answers that.
- LS can drive context-aware completions after scoped anchors.
- The `When`-intermediate case is documented as an exception, not modeled away.
- The `AccessMode` multi-token disambiguation requires `TokenKind[]?` (array), not `TokenKind?` (single) — George correctly identified this.

### Updated Position on Layer C

**I accept `DisambiguationToken: TokenKind[]?` on `ConstructMeta` for catalog completeness and consumer value.** This is a catalog metadata addition, not a parser restructuring. The field describes language structure that belongs in the catalog. The parser references it for documentation but doesn't restructure around it.

George's cost estimate: ~2 days for catalog + tooling, no parser restructuring. That's reasonable.

**What I still reject:** Restructuring the 4 parser disambiguation methods to be table-driven. George agrees with this rejection. We're aligned.

---

## 4. Updated Recommendation: What to Ship First

Given George's numbers, here's what I would advocate if Shane asked me to pick a slice:

### Ship First: Layer A + B (index) + E — "Vocabulary Catalog Derivation"

**Effort:** ~2.5 days (George's estimate: 2 days for A, bundled B index + E)
**Risk:** Near-zero
**What it delivers:**
- 80 vocabulary entries (operators, types, modifiers, actions) stay in catalogs, never duplicated in the parser
- Sync points auto-derived from `Constructs.All.Select(LeadingToken)` — definitionally correct, zero drift
- `Constructs.ByLeadingToken` index exists for LS/MCP consumers
- Parser built catalog-driven from day one — no future refactoring needed

This is the clean win. It captures the highest-value catalog derivation (vocabulary tables) at the lowest cost with the cleanest risk profile. The parser writes its vocabulary lookups exactly once, correctly, from catalog metadata.

### Ship Second: Layer C (catalog field only) — "Disambiguation Metadata"

**Effort:** ~2 days (George's estimate for catalog + tooling, no parser restructuring)
**Risk:** Low–medium (the `When` exception and `AccessMode` multi-token shape require careful documentation)
**What it delivers:**
- `DisambiguationToken: TokenKind[]?` on `ConstructMeta` — complete language structure in catalog
- `ByLeadingAndDisambiguationToken` derived index for LS context-aware completions and MCP
- Documented `When`-intermediate exception
- Parser methods reference the metadata but don't restructure

### Do Not Ship: Layer D

**Effort:** 4–5 weeks
**Risk:** High
**What it delivers:** Architectural elegance for 11 constructs that change at language-evolution pace
**Why not:** The `Slots` field already serves tooling consumers. The parser doesn't need to be slot-driven at this scale. George's estimate made the cost/benefit case definitive.

---

## 5. Summary of Position Changes

| Layer | My Original Position | George's Estimate Impact | My Updated Position |
|-------|---------------------|-------------------------|-------------------|
| **A** | Pursue | Confirmed: 2 days, zero risk | **Pursue — unchanged** |
| **B** | Pursue (dispatch + index) | Marginal for parser; Yes for index | **Pursue index only — aligned with George** |
| **C** | Reject entirely | Accept as catalog field for consumers, not parser restructuring | **Accept narrower framing — catalog field + tooling, no parser restructuring** |
| **D** | Pursue with constraints (biggest structural win) | XL / 4-5 weeks, high regression risk, near-zero correctness improvement | **Walk back — not justified at this scale** |
| **E** | Pursue | Trivial, bundle with A | **Pursue — unchanged** |

---

## 6. What I Learned

George's estimate taught me something I should have caught in my scope: **the relationship between abstraction value and grammar scale is not linear.** A generic production framework for 200 constructs is a necessity. A generic production framework for 11 constructs is over-engineering — regardless of how clean the architecture looks on paper.

The catalog-driven principle still holds absolutely for vocabulary (Layer A) — 80 entries across 4 tables, high change frequency (new types and modifiers are the most common language changes), zero-cost derivation. That's where the principle pays for itself.

It holds partially for structure (Layer B dispatch, Layer C disambiguation metadata, Layer E sync points) — the catalog describes it, consumers read it, but the parser doesn't have to be mechanically driven by it.

It does NOT hold for grammar productions at this scale (Layer D) — the abstraction cost exceeds the drift-prevention value when the construct count is small and change frequency is low.

The architectural principle is: **catalog-drive vocabulary always; catalog-describe structure for consumers; hand-write grammar mechanics.** That's the right boundary for a closed 11-construct DSL.
