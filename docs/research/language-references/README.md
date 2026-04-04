# Language Theory References — README

**Maintained by:** George (Runtime Dev)  
**Last updated:** 2026-04-04  
**Purpose:** Formal grounding for evaluating DSL feature proposals in Phase 2

---

## What's Here

This directory contains language theory research documents relevant to potential Precept expression expansions. Each document covers a specific PLT topic, grounded in formal references and calibrated to Precept's current design constraints.

These are **research documents, not proposals.** They exist to give the team principled grounding when evaluating feature proposals — so we don't invent justifications on the spot and instead can reason from established theory.

---

## Document Index

| File | Topic | PLT Concept | Relevance to Hero Sample Expressiveness |
|------|-------|-------------|----------------------------------------|
| [`expression-compactness.md`](./expression-compactness.md) | Reducing statement count without losing expressiveness | Syntactic sugar, derived forms, desugaring | **Medium** — Precept already has good shorthand inventory. Gap is error attribution through desugaring, not new sugar. |
| [`constraint-composition.md`](./constraint-composition.md) | Composing multiple constraints without boilerplate | Predicate combinators, collect-all semantics, constraint propagation | **High** — The single largest gap. Long `when` guards with repeated sub-conditions are the most common verbosity complaint in samples. Named reusable predicates would reduce this. |
| [`state-machine-expressiveness.md`](./state-machine-expressiveness.md) | What PLT and statecharts offer beyond `from/on/transition` | Statecharts (Harel), hierarchical states, parallel regions, CSP | **Medium** — Hierarchical states are high cost with limited domain benefit. Multi-event `on` clause and catch-all rows are low cost and high value. |
| [`multi-event-shorthand.md`](./multi-event-shorthand.md) | Formal patterns for further event-level shorthand | Symbolic automata, CSP event sets, UML multi-event arcs | **High** — Multi-event in the `on` clause is the most concrete near-term addition. Clean formal justification, consistent with existing multi-state shorthand pattern. |
| [`expression-evaluation.md`](./expression-evaluation.md) | Principled expression system expansion while keeping semantics decidable | Many-sorted FOL fragments, symbolic finite automata, type narrowing | **High** — String predicates (`startsWith`, `endsWith`, `length`) are low cost, low risk, and would eliminate the `!= ""` / `length > 0` workaround pattern seen in every sample. |

---

## Reading Guide

### For evaluating a specific feature proposal

1. Check which PLT category the proposal falls into (derived form? predicate combinator? new operator sort?)
2. Read the relevant document's "Implementation Cost" and "Semantic Risks" sections
3. Verify the proposal doesn't violate Precept's design principles (flat structure, AI readability, deterministic evaluation)

### For understanding what's already decided

The existing shorthand inventory (from `expression-compactness.md`):
- Multi-state `from` → desugars to N rows
- `from any` → desugars to one row per declared state  
- Multi-name `event`, `state`, `field` declarations → desugar to N individual declarations
- These establish the **desugaring pattern** any new shorthand should follow

### Priority ranking for Phase 2 proposals (observations only)

Based purely on research findings — these are observations, not proposals:

1. **Multi-event `on` clause (no-arg form)**: Highest value for lowest cost. Direct extension of existing pattern. `from any on Cancel, Withdraw → transition Cancelled` would eliminate 2–4 rows in many samples.

2. **String predicates** (`startsWith`, `endsWith`, `length`): Low cost, no decidability risk, eliminates the `!= ""` workaround that appears in every sample with string args.

3. **Named predicate reuse**: Medium cost, high impact on long `when` guards. Requires new AST node and scope resolution. Biggest authoring win for complex domain rules.

4. **Catch-all `on any` row**: Low-medium cost, but has semantic risks around masking `Undefined` outcomes that currently surface as analysis warnings.

5. **State machine hierarchy**: High cost, not recommended for Precept's domain model. The flat self-contained row model is a feature, not a limitation.

---

## Notes for Future Research

The following topics were identified during this research pass but not documented here. They may warrant future documents:

- **Error recovery and resugaring** — how to preserve diagnostic quality through desugaring (especially for multi-event expansion). Relevant to any new shorthand addition.
- **Constraint propagation beyond nullable narrowing** — interval analysis on numeric comparisons, enabling the type checker to prove constraints satisfiable/unsatisfiable without runtime evaluation.
- **Named invariant composition** — the formal semantics of named predicate references in different scope contexts (field scope vs. event arg scope vs. global scope).
- **Temporal assertions** — `before`/`after` event ordering constraints, analogous to TLA+ temporal operators. High semantic cost; not currently in scope.

---

## Cross-References

- DSL spec: `docs/PreceptLanguageDesign.md`
- Runtime design: `docs/RuntimeApiDesign.md`  
- Rules design: `docs/RulesDesign.md`
- Parser implementation: `src/Precept/Dsl/PreceptParser.cs`
- Expression evaluator: `src/Precept/Dsl/PreceptExpressionEvaluator.cs`
