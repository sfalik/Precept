# Language Theory References — README

**Maintained by:** George (Runtime Dev)  
**Last updated:** 2026-04-05  
**Purpose:** Formal grounding for evaluating DSL feature proposals

---

## What's Here

This directory contains language-theory research relevant to potential Precept language changes. Each document covers a specific PLT topic, grounded in formal references and calibrated to Precept's current design constraints.

These are **research documents, not proposals.** They exist to give the team principled grounding when evaluating GitHub proposal issues, so proposal bodies do not need to carry all theoretical rationale inline.

---

## Document Index

| File | Topic | PLT Concept | Relevance to Language Proposals |
|------|-------|-------------|---------------------------------|
| [`expression-compactness.md`](./expression-compactness.md) | Reducing statement count without losing expressiveness | Syntactic sugar, derived forms, desugaring | **Medium** — Precept already has good shorthand inventory. Gap is error attribution through desugaring, not new sugar. |
| [`constraint-composition.md`](./constraint-composition.md) | Composing multiple constraints without boilerplate | Predicate combinators, collect-all semantics, constraint propagation | **High** — Long repeated rule conditions and conditional validation both depend on this territory. |
| [`state-machine-expressiveness.md`](./state-machine-expressiveness.md) | What PLT and statecharts offer beyond `from/on/transition` | Statecharts (Harel), hierarchical states, parallel regions, CSP | **Medium** — Hierarchical states are high cost with limited domain benefit. Multi-event `on` clause and catch-all rows are lower-cost takeaways. |
| [`multi-event-shorthand.md`](./multi-event-shorthand.md) | Formal patterns for further event-level shorthand | Symbolic automata, CSP event sets, UML multi-event arcs | **High** — Multi-event `on` remains one of the clearest future compactness candidates. |
| [`expression-evaluation.md`](./expression-evaluation.md) | Principled expression system expansion while keeping semantics decidable | Many-sorted FOL fragments, symbolic finite automata, type narrowing | **High** — String predicates, bounded built-ins, and derived-value proposals all depend on this footing. |
| [`type-system-survey.md`](./type-system-survey.md) | What data types business rule engines and expression languages need | Cross-system survey: DMN/FEEL, Cedar, Drools, NRules, BPMN, SQL | **High** — Direct evidence base for Proposal #25 (choice and date types). Confirms date and enum are universal consensus. |

---

## Reading Guide

### For evaluating a specific GitHub proposal issue

1. Identify which PLT category the issue falls into (derived form, predicate combinator, new operator sort, accessor, built-in function).
2. Read the relevant document's implementation-cost and semantic-risk sections.
3. Verify the proposal still fits Precept's design principles: flat structure, AI readability, deterministic evaluation, and configuration-like readability.

### For understanding what is already established

The existing shorthand inventory (from `expression-compactness.md`):
- Multi-state `from` → desugars to N rows
- `from any` → desugars to one row per declared state  
- Multi-name `event`, `state`, `field` declarations → desugar to N individual declarations
- These establish the **desugaring pattern** any new shorthand should follow

### Priority ranking for future investigation (observations only)

Based purely on research findings — these are observations, not proposals:

1. **Multi-event `on` clause (no-arg form)**: Highest value for lowest cost. Direct extension of existing pattern. `from any on Cancel, Withdraw -> transition Cancelled` would eliminate 2–4 rows in many samples.
2. **String predicates** (`startsWith`, `endsWith`, `length`) and bounded numeric helpers: low cost, low decidability risk, strong practical utility.
3. **Named rule reuse**: Medium cost, high impact on long `when` guards. Biggest authoring win for complex domain rules.
4. **Catch-all `on any` row**: Low-medium cost, but with semantic risks around masking `Undefined` outcomes that currently surface as analysis warnings.
5. **State machine hierarchy**: High cost, not recommended for Precept's domain model. The flat self-contained row model is a feature, not a limitation.

---

## Notes for Future Research

The following topics were identified during this research pass but are not yet covered by dedicated docs here:

- **Error recovery and resugaring** — how to preserve diagnostic quality through desugaring (especially for multi-event expansion). Relevant to any new shorthand addition.
- **Constraint propagation beyond nullable narrowing** — interval analysis on numeric comparisons, enabling the type checker to prove constraints satisfiable/unsatisfiable without runtime evaluation.
- **Named invariant composition** — the formal semantics of named rule references in different scope contexts (field scope vs. event arg scope vs. global scope).
- **Temporal assertions** — `before`/`after` event ordering constraints, analogous to TLA+ temporal operators. High semantic cost; not currently in scope.

---

## Cross-References

- Start here index: `docs/research/language/README.md`
- Expressiveness research: `docs/research/language/expressiveness/README.md`
- DSL spec: `docs/PreceptLanguageDesign.md`
- Runtime design: `docs/RuntimeApiDesign.md`  
- Rules design: `docs/RulesDesign.md`
- Parser implementation: `src/Precept/Dsl/PreceptParser.cs`
- Expression evaluator: `src/Precept/Dsl/PreceptExpressionEvaluator.cs`
