## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs must derive behavior from durable catalog shape rather than enum-identity switch logic or parallel lists.
- Public API surfaces expose stable CLR/JSON interchange contracts; evaluator internals stay internal.
- Investigation docs can be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Learnings

- Three shared root causes explain most typed-literal completion bugs: quote-trigger context normalization, typed-constant boundary detection at unterminated end positions, and missing recovery branches for `NumberTyping` / `AfterPlus` slot phases.
- Invoked completion inside a typed constant cannot key solely on `TriggerCharacter == null`; clients may send an empty trigger character, and peer-expression inference must step left past the active typed-constant token.
- For domain-type bounds, qualifier semantics split by qualifier axis: exact unit match for `in 'kg'`, dimension membership for `of 'mass'`; currency remains an exact-match follow-up gap shared with `default`.
- Guard placement should come from slot metadata and parser protocol reality, not helper booleans or enum-identity switches that duplicate the catalog surface.
- Documentation drift often clusters around grammar slot order and guard position; fix the canonical docs the same pass as the source change.
- Typed literals remain on the current architectural boundary: compile-time literal validation through `TypedConstantValidation`, runtime JSON lanes through `TypeRuntime<T>`, and ISO/UCUM as embedded external datasets with Precept-owned metadata.
- Durable rationale belongs in decisions/research, not in ephemeral review comments or ad hoc implementation switches.

## Historical Summary

- Early May work locked the typed-literal boundary, the external-data posture for ISO/UCUM, and the requirement that durable rationale live in decisions/research instead of scattered implementation branches.
- Recent batches settled the when-guard parser model, grammar/spec doc-sync rules, terminal-state diagnostic gating, and the typed-literal implementation review loop.

## Recent Updates

### 2026-05-11T05:34:40Z — B4/B5 retriage corrected the prior completion-bug closure
- Kramer's apostrophe-trigger coercion was a legitimate B1 fix for true expression/default sites, but it does not repair declaration-side qualifier literals.
- B4 (`quantity in '`) and B5 (`quantity of '`) still misroute through `TryGetEnclosingField(...)`, recover the outer `Quantity` type, and surface quantity-literal items instead of the active qualifier slot.
- Durable fix direction: qualifier-site resolution must happen before expression fallback, driven by parsed qualifier metadata / qualifier-shape slots, with concrete unit/dimension assertions replacing weak non-empty tests.

### 2026-05-11 — Empty typed-constant invocation diagnosis tightened
- The lexer/span layer was already correct for empty `''`; the real regressions were client-shape variance on invoked completion and token walks that failed to skip the active typed-constant token while recovering surrounding expression context.
- Record both null and empty trigger characters as invoked completion, and make peer-operand inference walk left past the current literal token.

### 2026-05-11T01:38:51Z — Terminal-state gating and parser follow-through are now durable
- Path-to-terminal warnings only make sense once at least one terminal state is declared, and lifecycle wording should name declared terminals explicitly.
- The paired parser/type-checker closeout also landed: non-associative operators use `meta.Precedence + 1` on the RHS, and typed constants inherit peer operand type context before the D13 bailout.

### 2026-05-10T23:31:04-04:00 — Typed-literal UX review approved the architecture and wrote Kramer's plan
- Elaine's typed-literal UX was approved as the behavioral contract: type-owned routing, qualifier-aware hard filtering, compound temporal in V1, and quiet free-form text.
- The implementation plan locked the 5-slice execution order: type branching, slot detection, qualifier threading, compound temporal continuation, and integration coverage.

### 2026-05-10T19:47:35Z — Grammar doc-fix batch durably recorded
- The comprehensive `precept-grammar.md` audit, the EventHandler trailing-ensure cleanup, and the final doc-alignment pass now live in the squad ledger.
- Durable guidance: document pre-verb `when` coverage everywhere StateEnsure / StateAction / EventEnsure / AccessMode appear, keep computed-field modifiers before `<-`, and remove dead construct-metadata claims once the source deletes them.
