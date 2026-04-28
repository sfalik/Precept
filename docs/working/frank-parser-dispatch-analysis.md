# Parser Dispatch Table — Not a Catalog Violation

**By:** Frank
**Date:** 2026-04-27
**Status:** Analysis complete, no action required

## Question

Does the parser's top-level dispatch table (token → parse production) violate the metadata-driven architecture by hardcoding per-token method calls instead of deriving dispatch from catalog metadata?

## Decision

**No.** The dispatch table is legitimate hand-written grammar mechanics, explicitly carved out by the catalog-system design doc (§ Pipeline Stage Impact, Parser row): "Grammar productions stay hand-written."

## Key Evidence

1. **`LeadingToken` already exists.** Every `ConstructMeta` carries a `TokenKind LeadingToken` field. The catalog already contributes the token→construct mapping.

2. **The mapping is 1:N, not 1:1.** Four leading tokens map to multiple ConstructKinds — `In`→2, `To`→2, `From`→3, `On`→2. A single catalog lookup cannot select the production; the parser must look ahead past the state/event target to the following verb to disambiguate. This is grammar structure, not domain knowledge.

3. **The catalog-system doc explicitly distinguishes two parser concerns:**
   - **Vocabulary tables** (~40–50% of language knowledge) → catalog-derived frozen dictionaries. This IS the metadata-driven part.
   - **Grammar productions** (recursive descent structure) → hand-written. This is where the dispatch table lives.

4. **`Parser.cs` is a stub today.** The dispatch table in the spec (§2.2) is design intent. When implemented, the parser will use catalog-derived vocabulary tables (operator precedence, type keywords, modifier/action recognition sets) but hand-written production selection.

## What Would Not Buy Us Anything

Putting a `ParseHandler` delegate or production-selection logic in the ConstructCatalog would:
- Not eliminate disambiguation — the 1:N LeadingToken cases still need lookahead logic
- Couple the catalog to the parser's internal method signatures
- Move code without removing complexity
- Violate the catalog's role (declarative metadata, not imperative behavior)

## Where the Real Catalog-Driven Work Is

The spec and catalog-system doc already identify the high-value parser work: migrating vocabulary tables to catalog-derived frozen dictionaries. When the parser is implemented, it should derive operator precedence from `Operators.All`, type keyword sets from `Types.All`, modifier recognition from `Modifiers.All`, and action keywords from `Actions.All`. That's the metadata-driven boundary for the parser.
