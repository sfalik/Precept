# How We Got Here

> **Authority boundary:** This file lives in `docs/`, the repository's legacy/current reference set. Use it for the implemented v1 surface, current product reference, or historical context. If you are designing or implementing `src/Precept.Next` / the v2 clean-room pipeline, start in [docs.next/README.md](../docs.next/README.md) instead.

## Purpose

This document is a factual map of how the repository arrived at its current direction. It is meant to save a future reader from replaying every exploratory branch, README pass, and design note just to understand what actually survived.

## Chronological Narrative

### 1. Early prototype, then a long quiet period (2016-2021)

The repository began in 2016 as a minimal `StateMachine` project, then became concrete in a small 2021 cluster around a generic `FiniteStateMachine<TState, TEvent>` API. The shape was firmly host-language-first: states and events were enum-driven, transitions were stored in a predeclared table, and authoring happened through nested C# chains such as `WhenStateIs(...).AndEventIs(...).TransitionTo(...)`.

Those experiments explored a few specific abstractions that matter in retrospect: `KeepCurrentState` for data-less self-transitions, conditional routing via `If(...).TransitionTo(...).Else ...`, optional `WithAction(...)` hooks, and a lightweight preflight check through `IsEventAccepted(...)` before `TriggerEvent(...)`. In other words, the early project was already chasing deterministic inspection and explicit transition definition, but it was doing so through fluent API layers rather than through a standalone language.

What matters from this period is not volume but lineage. Two ideas were present early and survived the later reset: transitions should be declared up front, and callers should be able to ask what an event would do before committing it. What did not survive was the authoring model. The current DSL-first product is not a direct continuation of that surface; it is a later answer to the same underlying concerns.

### 2. Reactivation and product clarification (late February 2026)

Work resumed in late February 2026 with a cleanup of the public story and developer workflow: README clarification, REPL support, VS Code setup, and sample/doc maintenance. At that point, the main public runtime shape was still a more elaborate C# builder surface: `StateMachine.CreateBuilder<TState>()`, optional `.WithData<TData>(...)` to bind immutable records to state, `.On(out var event)` to capture event tokens, `Build()` to produce a reusable template, and `CreateInstance(...)` plus staged `Inspect(...).IfDefined().IfAccepted().Fire()` chains at runtime. This phase still reads like a project trying to explain itself more clearly, rather than one that had already locked its final shape.

It did, however, establish two ideas that stayed:

- the product needed a clearer public explanation;
- tooling and authoring experience were going to matter as much as raw runtime behavior.

### 3. The decisive turn: the language redesign branch (March 2026)

The real pivot happened on `feature/language-redesign` in early March 2026. This was not a polish pass. It was a full redesign of the DSL and the machinery around it, and it changed the product's center of gravity from a fluent/builder-authored API to a DSL-authored system with a runtime built around parsing, compilation, and execution.

In a short sequence of phased commits, the branch replaced the old regex parser with a Superpower-based parser, introduced the current model types, rewrote the runtime/compiler for the new constructs, migrated the samples, rewrote the language server and TextMate grammar, and updated the design docs. The old parser was then removed entirely in Phase 10, along with archived fallback code.

This is the point where Precept stopped being "an existing state machine library with some syntax changes" and became the current product: a flat, keyword-anchored DSL with deterministic semantics, parser/tooling alignment, and design docs that explicitly treat compile-time checking and inspectability as product features.

### 4. Tooling and AI surface hardening (March 6-27, 2026)

After the language redesign landed, the work shifted from "make the language real" to "make the language operable."

That phase added or hardened:

- the MCP server surface;
- shared catalogs and drift-resistant metadata;
- stronger language-server completions and diagnostics;
- bundled delivery of the language server and MCP tooling with the VS Code extension;
- the Copilot plugin and agent/skill scaffolding;
- a tighter five-tool MCP surface aligned to the current runtime.

This phase is where the AI-first story became concrete. It stopped being implied by the DSL and became an actual delivery surface: tools an editor or agent could call, not just docs claiming that AI should like the design.

### 5. Brand, README, and surface governance (late March to early April 2026)

Once the technical core was in place, the repository entered a brand and documentation-heavy phase.

That work included:

- external brand research and positioning work;
- locking the "domain integrity engine" frame;
- locking the semantic color system and typography direction;
- hero-sample research and ranking, with Subscription Billing emerging as the current temporary README example;
- restructuring the brand spec around visual surfaces instead of abstract brand categories;
- formalizing the review gate for product surfaces: UX -> brand -> architecture -> Shane;
- README restructuring and the current inline DSL hero presentation.

This phase produced a large amount of supporting material, but its practical result is narrower than its volume: Precept now presents itself as a category-creating tool with a disciplined visual language, an AI-aware README, and a stronger rule that docs must track implementation instead of aspiration.

## What Endured Through the Journey

In retrospect, several themes proved durable enough to outlast the repository's false starts, rewrites, and reframings:

1. **Precept as a domain integrity engine.** That language eventually held because it described the implemented runtime more faithfully than the older state-machine framing.
2. **The redesigned DSL as the product's center of gravity.** The flat, keyword-anchored syntax; explicit nullability; state/data/rule unification; and deterministic inspect/fire/update model are the shape the project ultimately settled into.
3. **Tooling as part of the product, not an accessory.** The language server, VS Code extension, MCP server, and Copilot/plugin work endured because the project kept converging on the idea that authoring and operation were inseparable from the runtime itself.
4. **AI-first operation backed by actual tools.** The five MCP tools became one of the clearest signs of what survived the redesign: not just an AI-friendly story, but concrete surfaces an agent could use.
5. **Surface-first documentation and brand governance.** Over time, the repository stopped treating docs and visual language as loose packaging and started treating them as part of the product surface that needed to stay in sync.
6. **A compact, AI-readable README hero form.** Even as specific samples and hero status shifted, the enduring pattern was the same: the public face of Precept kept returning to a compact contract that could teach both human readers and agents how the product thinks.

## Last Updated

2026-04-05

Append future milestones below this marker as the repository evolves.
