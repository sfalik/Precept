---
name: "reject-path-construct-split"
description: "When a DSL row can either mutate/succeed or reject, express the fork as separate grammar constructs rather than optional slots plus diagnostics."
domain: "language-design"
confidence: "high"
source: "earned — 2026-05-15 constructor/reject audit across Constructs.cs, Tokens.cs, spec, and samples"
---

## Context

This skill applies when a language surface combines work slots (`ActionChain`, success outcomes) with a reject/refusal lane. It captures the Precept rule that reject mutual exclusion belongs in grammar, not in the type checker.

## Rule

- If a construct can do work or reject, split it into two constructs: mutation/success variant and reject variant.
- Mutation variant omits the reject slot.
- Reject variant omits work slots and success outcomes.
- Parser commitment happens at the earliest distinguishing token after the shared prefix.
- Semantic-model shape mirrors the grammar split with DU subtypes.
- Do not add a mutual-exclusion diagnostic for a hybrid the grammar can make unwritable.

## Why

Precept expresses impossibility by omitting impossible slots. A diagnostic for `mutate then reject` is downstream cleanup of a grammar mistake. The catalog should describe only valid shapes.

## Precept example

- `TransitionRowMutation`: `from State on Event [when Guard] -> actions -> transition|no transition`
- `TransitionRowReject`: `from State on Event [when Guard] -> reject "reason"`
- `EventHandlerMutation`: `on Event [when Guard] -> actions`
- `EventHandlerReject`: `on Event [when Guard] -> reject "reason"`

## Smells

- Single construct with both `ActionChain` and `RejectClause`
- Flat semantic records with `Actions`, `Outcome`, and `RejectReason` nullable together
- Type-checker-only `MutualExclusion` diagnostics for a shape the parser could exclude
- Spec grammar that allows `-> set ... -> reject ...` while samples never author it

## Implementation checklist

1. Audit every construct for reject acceptance.
2. Split construct kinds and slot kinds.
3. Route the parser on the first distinguishing token after the shared prefix.
4. Split the semantic model into DU subtypes.
5. Update spec, completions, hover, and generated grammar to show the split.
