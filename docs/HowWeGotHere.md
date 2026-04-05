# How We Got Here

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

### 6. Where branch history left us

The unusual part is not just what was built. It is where it lives.

The current working line is `feature/language-redesign`, while `main` remains a separate two-commit concept-readme line. There is no merge base between `HEAD` and `main` or `origin/main`. In other words: the current strategy did not grow out of trunk in the ordinary way. It accumulated on a separate line that now carries nearly all of the real product work.

## What Is Actually Current / Worth Preserving

These elements appear to have survived exploration and describe the real direction of the repository:

1. **Precept as a domain integrity engine.** This framing is now repeated across README, brand decisions, and design docs, and it matches the implemented runtime model.
2. **The redesigned DSL.** The flat, keyword-anchored syntax; explicit nullability; state/data/rule unification; and deterministic inspect/fire/update model are the center of gravity now.
3. **Tooling as part of the product, not an accessory.** The language server, VS Code extension, MCP server, and Copilot/plugin work are not side quests. They are part of how Precept now explains itself.
4. **AI-first operation backed by actual tools.** The five MCP tools and related plugin work are one of the clearest through-lines from the redesign into the current strategy.
5. **Surface-first documentation and brand governance.** The repo has moved past generic brand notes. It now has explicit rules for how syntax, diagrams, inspector surfaces, and public docs should stay in sync.
6. **A temporary but concrete README hero path.** The current README uses an inline contract plus companion hero artifacts. What seems preserved is the form: a compact, AI-readable contract in the public surface. What remains provisional is the final hero status and long-term sample choice.

## What Remains Unresolved Before Trunk Consolidation

Several things should be settled before this line is treated as trunk-ready.

### 1. Consolidation strategy is unresolved

Because there is no merge base with `main`, this is not a routine merge. Someone needs to decide whether trunk becomes:

- a new root taken from `feature/language-redesign`, or
- a curated re-landing of selected work onto `main`.

Without that decision, "merge to trunk" is more slogan than plan.

### 2. The working tree is not fully settled

There are uncommitted edits in `docs\PreceptLanguageDesign.md`, and there is also a sibling worktree branch (`copilot/worktree-2026-03-28T05-06-33`). Before consolidation, the team should decide which line owns which remaining edits and what, if anything, still needs to be harvested from the sibling worktree.

### 3. Some public-surface decisions are still explicitly temporary or pending

The current README still marks the hero sample as temporary. The surrounding brand work is substantial, but the public surface itself has not yet claimed finality. That is acceptable during exploration; it should be made explicit before trunking whether the temporary hero ships as-is, graduates, or is replaced.

### 4. Surface implementation and surface guidance are not fully closed

The decision log and brand materials still point to pending or draft work around the inspector surface, CLI color behavior, and broader visual-surface integration. Those do not all have to block trunk, but they should be triaged deliberately rather than carried forward as ambient "later."

### 5. Exploration material needs curation

The branch contains a great deal of valuable research, proposals, deliberation records, and design-review material. Not all of it belongs on the critical path of trunk. Before consolidation, the team should separate:

- implementation-facing source-of-truth docs;
- active supporting references worth keeping nearby;
- exploration artifacts that can remain archived without driving future decisions.

## Recommendation

1. **Treat trunk consolidation as a curation exercise, not a merge exercise.** Start with an explicit keep/drop/defer list.
2. **Preserve the implemented center:** the redesigned DSL, runtime/tooling stack, AI tooling surface, and the current source-of-truth docs that describe them.
3. **Resolve the branch-topology question before polishing copy.** The repo first needs to know what trunk is.
4. **Keep the README honest.** If the hero remains temporary at consolidation time, say so. If it is promoted, remove the temporary framing everywhere in one pass.
5. **Archive aggressively, but not blindly.** The exploration history is useful. It just should not be mistaken for the final operating manual.
