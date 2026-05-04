## Core Context

- Owns UX/design across README form, semantic visual system, preview surfaces, and author-facing product language.
- Keeps public prose in plain, confident language that matches runtime truth rather than implementation jargon.
- Durable philosophy baseline: the strongest copy names the reader's pain, lands the absolute guarantee without hedging, and only then explains the structural reason the claim holds.
- Durable grammar baseline: grammar/design references should be negative-first, flat/keyword-anchored/named-slot centered, and documented with ASCII-first artifacts when examples are too wide for graph tooling.

## Learnings

- **Vocabulary at failure points matters most.** Developers read outcome/error variant names when something goes wrong — that's the highest-friction moment. Names that import vocabulary from other domains (RBAC: `AccessDenied`) or internal vocabulary (Precept rows: `RowInspection`) create maximum confusion at maximum pain. Fix those first.
- **DU base type provides operation context; variant names shouldn't repeat it.** `EventOutcome.ConstraintsFailed` is cleaner than `EventOutcome.EventConstraintsFailed` because the union name already tells you which operation produced the result. Variant names should describe *what*, not *where*.
- **Naming register consistency within a type family matters.** When success variants across parallel DUs (`EventOutcome` vs `UpdateOutcome`) use different naming registers (entity-centric `Transitioned` vs operational `FieldWriteCommitted`), it signals the types weren't reviewed side-by-side. Check all variants in a DU family against each other, not just each variant in isolation.
- **DSL keyword properties should be consistent across types.** `ConstraintDescriptor.Because` and `ConstraintViolation.BecauseClause` refer to the same concept in the same consumption context. Where a property maps to a DSL keyword, use the keyword form consistently; don't invent an alternate noun form for the same concept on a companion type.
- **Operation parameter vocabulary should propagate to outcome vocabulary.** `Update(fields)` → bad-input outcome should be `InvalidFields`, not `InvalidInput`. The asymmetry (`InvalidArgs` for Fire vs `InvalidInput` for Update) is a missed parallel; the outcome name should mirror the parameter name the caller used.


- Audience framing is usually a POV problem, not a content problem: the same guarantee can read as domain-expert guidance or as a developer commitment depending on pronouns and beneficiary framing.
- Philosophy copy weakens the moment it falls back to spec-speak (`failure classes`, semicolon mechanism lists, scare-quoted hedges) or implementation nouns the reader never encounters.
- The prevention claim lands best as structural impossibility, not as validator-style interception; the compiled contract is the ground, not a safety net beneath code.
- Business-process guarantees and expression-safety guarantees solve different anxieties; both deserve explicit language when explaining compile-time structural checking.
- Use distinct visual classes when diagrams mix grouping markers and per-construct annotations; readers need to tell structure from badges at a glance.
- For fixed-width documentation, prefer ASCII-safe symbols over ambiguous-width glyphs when alignment is part of the contract.

- **Public API result vocabulary should stay in the caller's mental model.** Use state-machine terms like `TransitionInspection` and field-shape terms like `FieldNotEditable`; avoid internal implementation words (`RowInspection`) and adjacent-domain security language (`AccessDenied`).
- **Outcome families should be reviewed as a family, not variant-by-variant.** Let the DU base carry operation context, keep failure names aligned with operation inputs (`InvalidFields`) and structural facts (`ConstraintsFailed`), and keep success names in the same short past-tense register (`Transitioned`, `Applied`, `Updated`).

## Recent Updates

### Historical summary through 2026-05-03T15:37:24Z
- Built and refined the active grammar/design reference baseline: grammar hierarchy/reference artifacts, catalog-system diagram simplification, language-server diagnostic-enrichment relocation, and the related durable presentation rules for ASCII-first documentation and author-centered wording.

### 2026-05-04T12:31:05Z — Public API naming assessment

- Conducted comprehensive naming assessment of `docs/working/runtime-api-public-surface-spec.md` against philosophy vocabulary, API consistency, and developer clarity criteria.
- Six naming issues identified: `AccessDenied` (RBAC false connotation → `FieldNotEditable`); `RowInspection` (internal vocab → `TransitionInspection`); `BecauseClause` vs `Because` inconsistency; `InvalidInput` vs `InvalidArgs` asymmetry (→ `InvalidFields`); `FieldWriteCommitted` register mismatch (→ `Updated`); plus concurrence with Frank's `ConstraintsFailed` rename and DU nesting.
- Decision record written to `.squad/decisions/inbox/elaine-api-naming-assessment.md`.

- Elaine-23 produced philosophy v5 after applying Frank/Steinbrenner/Peterman reviewer notes from the v4 review pass.
- Elaine-24 then shifted the same content to the correct audience: a developer evaluating/adopting Precept, with domain-user pain retained as the beneficiary frame rather than the direct addressee.
- Elaine-25 applied the locked v6 wording to docs/philosophy.md, replacing the Prevention, not detection and Compile-time structural checking bullets with the final prevention-first / developer-commitment text.
