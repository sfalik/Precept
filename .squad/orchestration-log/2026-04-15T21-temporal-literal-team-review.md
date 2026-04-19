# Orchestration Log — Temporal Literal Deep Design Review

**Date:** 2026-04-15T21:00:00Z
**Batch:** Temporal literal grain analysis — 5 agents, multi-dimensional evaluation
**Trigger:** Shane observed that `date(2026-01-15)` "really stands out as being different / awkward" compared to every other Precept literal. Full team deployed for deep design review.

---

## Frank (claude-opus-4.6) — Language Design Philosophy & Grain Analysis

**Task:** Temporal literal grain analysis — 7 alternatives evaluated against Precept's 13 design principles.

**Outcome:** Defined Precept's literal grain as "value-first, type-implicit" and confirmed that temporal literals necessarily break this grain because ISO 8601 date notation (`2026-01-15`) is ambiguous with integer subtraction. Systematically evaluated 7 alternative syntaxes:

| Candidate | Verdict |
|-----------|---------|
| `#2026-01-15#` (hash-delimited) | REJECTED — comment-character collision, VB baggage |
| `~d[2026-01-15]` (sigil-prefixed) | REJECTED — violates keyword-dominant design identity |
| `@2026-01-15` (at-prefixed) | REJECTED — no type signal across 7 types, violates keywords-for-domain |
| `` `2026-01-15` `` (backtick) | REJECTED — type-ambiguity, markdown rendering hazards |
| `date 2026-01-15` (keyword, no parens) | REJECTED — parser ambiguity without delimiter boundary |
| `d:2026-01-15` (thin-colon) | REJECTED — introduces `:`, no closing delimiter |
| `<date:2026-01-15>` (angle-bracket) | PREVIOUSLY REJECTED — confirmed |

**Conclusion:** `date(2026-01-15)` is the minimal form satisfying all principles. The grain extends naturally: "value-first where the value self-identifies, type-prefixed where it doesn't." Parens are the minimum viable delimiter using existing grammar. Recommends keeping Decision #18 locked and documenting the extended grain principle.

**Filed:** `.squad/decisions/inbox/frank-temporal-literal-grain.md`

---

## George (claude-sonnet-4.6) — Parser Feasibility Analysis

**Task:** Assess 8 candidate syntaxes against Superpower 3.1.0 tokenizer/parser constraints.

**Outcome:** Detailed token-level analysis of each candidate. Key findings:

| Candidate | Parser Verdict |
|-----------|---------------|
| `date(2026-01-15)` (current) | feasible-with-caveats — 8 tokens for one value; two strategies (multi-token combinator vs single-token regex) |
| `<date:2026-01-15>` | not recommended — `<` conflicts with `LessThan`, per-`<` regex attempts hurt perf |
| `#2026-01-15#` | not recommended — fatal comment-syntax collision |
| `@2026-01-15` | feasible-with-caveats — clean `@` namespace, but type inferred from shape (YAML antipattern) |
| `d'2026-01-15'` | feasible — single token, type explicit, but introduces new delimiter class |
| `'2026-01-15'` | feasible-with-caveats — type inference from content = YAML problem |

Identified that `date(...)` has two implementation strategies: Strategy A (multi-token combinator, 8 tokens, per-type content grammar) and Strategy B (single-token regex, per-type tokenizer recognizers). Strategy B is cleaner but requires 7 regex recognizers. The `time` type's colons and `timezone`'s slashes create token conflicts under Strategy A.

**Filed:** `.squad/decisions/inbox/george-temporal-literal-parser-feasibility.md`

---

## Kramer (claude-sonnet-4.6) — Tooling Impact Assessment

**Task:** Assess grammar, completions, hover, semantic tokens, error squiggles for 7 candidates.

**Outcome:** Evaluated each candidate across 6 tooling dimensions. Summary:

| # | Syntax | Grammar Effort | UX Quality | Key Risk |
|---|--------|---------------|------------|----------|
| 1 | `date(2026-01-15)` | medium | Good | Looks like function call without semantic override |
| 2 | `<date:2026-01-15>` | high | Fair | Completion ambiguity with `<` |
| 3 | `#2026-01-15#` | low | Poor | Comment confusion |
| 4 | `@2026-01-15` | low | Fair | No type keyword = reduced discoverability |
| 5 | `d'2026-01-15'` | medium | Fair | New delimiter, prefix ambiguity |
| 6 | `'2026-01-15'` | low | Poor | Zero visual distinction from strings |
| 7 | `date(...)` + semantic override | medium | **Excellent** | None significant |

**Recommendation:** Candidate 7 (enhanced Decision #18) — `date(2026-01-15)` with dedicated `typedLiteral` TextMate pattern and `preceptTemporalLiteral` semantic token override. Provides: cleanest grammar pattern, richest completions (type keyword as trigger), component-level error squiggles, guaranteed color distinctiveness. Proposed concrete TextMate pattern (begin/end with 7-keyword list).

**Filed:** `.squad/decisions/inbox/kramer-temporal-literal-tooling-impact.md`

---

## Elaine (claude-opus-4.6) — UX Analysis (10 Dimensions)

**Task:** Evaluate temporal literal candidates across readability, learnability, visual distinctiveness, cognitive experience.

**Outcome:** Scored candidates across 10 UX dimensions:

1. **First encounter** — `date(...)` wins: self-documenting, zero learning curve
2. **Writing from memory** — `date(...)` wins: builds on constructor pattern users know
3. **Reading in context** — `date(...)` wins: lowest visual noise while self-documenting
4. **Mental model consistency** — `date(...)` closest to what most users would predict
5. **Error recovery** — `date(...)` wins: richest diagnostic context (component-level errors)
6. **AI readability** — `date(...)` wins: explicitly typed at text level
7. **Visual weight** — `date(...)` has more characters but less cognitive weight
8. **Should literals stand out?** — `date(...)` is "moderately distinct" — right calibration
9. **Scaling across 8 types** — `date(...)` is the only form that scales (keyword rescues complex types like `zoneddatetime`, `duration`, `period`)
10. **Unlisted forms explored** — bare ISO, postfix `as date`, backtick, keyword-space — all rejected for specific reasons

**Conclusion:** Decision #18 is correct. The awkwardness Shane noticed stems from expectation mismatch (zero-ceremony vs keyword-wrapper), not syntax deficiency. The ceremony is proportional to the semantic weight. Investment should go into IntelliSense ghost text and syntax highlighting, not bare-literal syntax.

**Filed:** `.squad/decisions/inbox/elaine-temporal-literal-ux.md`

---

## Peterman (gpt-5.4) — Voice & Brand Analysis

**Task:** Rhythm, ceremony-to-meaning ratio, brand alignment, cross-type consistency.

**Outcome:** Read each form aloud and assessed against Precept's "authoritative with warmth" brand voice.

- **Rhythm:** `date(2026-06-30)` — functional, slightly procedural; parens create a small room around value. Best rhythm of all options. Angle-bracket form breaks prose. Hash feels like code. `@` reads well as English preposition "at" but overloaded in programming. Single-quote forms regress on Decision #18's no-quotes directive.
- **Ceremony-to-meaning ratio:** Temporal values carry more semantic freight than scalars (calendar coordinates, implicit timezone/leap-year knowledge). The `date(...)` ceremony is proportional — not excessive.
- **Brand test:** In the README hero example, `date(2026-01-15)` says "precise but humane." Angle brackets say "configuration format." `@` says "insider symbol." Quotes say "hidden type."
- **Cross-type consistency:** Only constructor form scales from `date(2026-01-15)` to `zoneddatetime(2026-01-15T14:30:00[America/New_York])`. All other forms collapse at complexity.
- **Writer's test:** Considered keyword-space form (`date 2026-06-30`) as the only plausible alternative — gains two characters, loses delimiter boundary. Not worth the grammar complication.
- **Flagged:** `zoneddatetime(...)` length may merit a naming conversation (shorter alias), but that's a type-name question, not a literal-syntax question.

**Conclusion:** Decision #18 is "narratively correct." Hiding the type to save characters betrays the brand promise "nothing is hidden."

**Filed:** `.squad/decisions/inbox/peterman-temporal-literal-voice.md`

---

## Consensus

All 5 agents independently converge on the same conclusion: **Decision #18 (`date(2026-01-15)`) is correct and should remain locked.** The alternatives are each strictly worse on at least one critical dimension. The "awkwardness" Shane noticed is real but irreducible — ISO 8601 notation collides with arithmetic syntax, requiring some disambiguation ceremony.

**Actionable enhancements identified:**
1. Dedicated TextMate `typedLiteral` pattern with semantic token override (Kramer)
2. Document the extended literal grain principle in `PreceptLanguageDesign.md` at ship time (Frank)
3. IntelliSense ghost text and format-aware completions inside temporal literal parens (Kramer, Elaine)
4. Consider shorter alias for `zoneddatetime` as a separate naming question (Peterman)

**Decision inbox:** 5 new files present. NOT merged — these are analysis inputs for Shane's review.
