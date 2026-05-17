# SyntaxReference Construction Pattern Entries

**Author:** George  
**Date:** 2026-05-17  
**Requested by:** Shane  
**Status:** Decision Record

---

## Decision

`src/Precept/Language/SyntaxReference.cs` should expose the two first-class construction idioms directly in `SyntaxReference.CommonPatterns` as named `CommonPattern` entries:

1. `Constructor Pattern (Existential Fields)`
2. `Free-Construction Pattern (Governed Draft)`

---

## Rationale

`precept_patterns` reads the hardcoded `CommonPattern` list, and `CommonPattern` only has three fields: `Name`, `Description`, and `DslSnippet`.

That means construction-choice guidance must be carried inside the description text and example snippet rather than a dedicated `WhenToUse` property. The patterns need to contrast each other explicitly so MCP consumers can answer both questions:

- how each idiom is authored
- when to choose one over the other

---

## Durable Guidance

- Pattern A should show `event Create(...) initial` plus `on Create` construction rows for entities whose required fields are existential at birth.
- Pattern B should show the no-initial-event path: an initial draft state, `editable` progressive enrichment, and a later activation transition that enforces readiness.
- Pattern B must be described as governed from birth, not as an ungoverned loophole.
- Future `CommonPattern` additions that need selection guidance should keep that guidance in `Description` unless the record shape is intentionally expanded.
