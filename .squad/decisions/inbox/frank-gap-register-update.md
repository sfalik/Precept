# Gap Register Update

**Author:** Frank (Lead/Architect)  
**Date:** 2026-05-03  
**Requested by:** Shane  

---

## Decision

Applied the approved cross-cutting audit recommendations across the working registers.

### What changed

1. Promoted the mis-triaged catalog-gap items into `docs/working/cross-cutting-decisions.md` as entries **#21–#24**:
   - Event `optional` modifier
   - `SemanticIndex.EnsuresByState`
   - `EventOutcome.mutations`
   - Unmatched guard trace enrichment
2. Added the two uncaptured cross-cutting decisions from the audit as entries **#25–#26**:
   - Execution dispatch delegate design
   - Stateless precepts `CreateInitialVersion` semantics
3. Added new catalog gaps **#41–#43** to `docs/working/catalog-gap-register.md`:
   - `TokenMeta.SemanticTokenModifiers`
   - `TypeAccessor` DU hierarchy
   - `ActionMeta` missing properties aggregate
4. Reclassified catalog-gap items **#14, #19, #32, #38** to explicit "Captured in cross-cutting-decisions.md #N" statuses.
5. Reclassified catalog-gap items **#10, #26, #28, #29** to explicit "Moved to cross-cutting-decisions.md #N" statuses.

### Umbrella-decision judgment

I considered the audit recommendation to add an umbrella decision for evaluator-output richness for tooling consumers. I did **not** add a separate umbrella entry because decisions **#22–#24** already form a tight, concrete cluster with direct implementation value; a parent record would add navigation indirection without introducing an additional design choice to resolve.

### Files updated

- `docs/working/cross-cutting-decisions.md`
- `docs/working/catalog-gap-register.md`
- `.squad/agents/frank/history.md`

### Outcome

The working registers now reflect the approved audit recommendations and provide traceable links between the catalog-gap and cross-cutting registers for the newly reclassified items.
