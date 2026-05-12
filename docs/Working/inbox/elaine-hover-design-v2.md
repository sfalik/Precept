# Decision: Hover Design Revision V2

**Author:** Elaine
**Date:** 2026-05-12T01:06:10.200-04:00
**Status:** Revised design complete — pending Shane sign-off

## Key Revision Decisions

1. **Compact by default.** Every rendered hover example was redesigned to fit within 8 markdown lines, with a fixed reading order: construct, meaning, status, then the highest-value facts.
2. **Audience reset.** The target reader is now a technically literate business author, not a beginner and not a general developer. Hover copy keeps terms like `type`, `nullable`, `constraint`, and `guard`, but explains their business effect.
3. **Tone reset.** Status indicators stay strong, but the prose is factual and non-salesy: statically confirmed, runtime checked, unverified.
4. **Pipeline coverage widened.** The spec now requires each construct hover to name its pipeline sources so hover can surface type-check, graph, proof, and runtime facts instead of centering only the proof engine.
5. **Construct-first implementation.** Kramer should resolve the enclosing construct before token hover so rules, ensures, transition rows, access declarations, reject rows, and qualifiers read as business contracts instead of isolated symbols.

## Durable Hover Contract

- Meaning first, syntax second.
- Use `because` text as the primary human explanation when it exists.
- Surface the pipeline that owns the fact.
- Keep proof status to one clear row with one evidence sentence at most.
- Prefer scannable state/write/reach summaries over narrative paragraphs.
