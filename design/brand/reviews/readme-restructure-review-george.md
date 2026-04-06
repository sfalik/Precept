# README Restructure Proposal — Runtime & DSL Accuracy Review

**Reviewer:** George (Runtime Dev)  
**Date:** 2026-04-06  
**Proposal under review:** `brand/references/readme-restructure-proposal.md` (J. Peterman, 2026-04-05)  
**Frank's review:** `brand/references/readme-restructure-review-frank.md` (reviewed first)  
**Status:** CONDITIONALLY APPROVED — 3 required changes

---

## Verdict

The narrative architecture is sound and I don't dispute Frank's structural approval. He caught the right structural issues. But RC-1 from his review contains a factual error about the API surface — one I have to correct before it propagates into the README copy. And the proposal has two additional gaps that are my domain: a missing prerequisite and an overclaiming risk in the hook.

Three required changes. One endorses and corrects Frank's RC-1. Two are new.

---

## What the Proposal Gets Right (From a Runtime Standpoint)

### The C# API chain is directionally correct

`PreceptParser.Parse(...)` → `PreceptCompiler.Compile(...)` → `engine.CreateInstance(...)` → `engine.Fire(...)` is the correct round trip. Class names are real, method names are real, return types chain correctly. This is good — bad hero copy with a fabricated API surface is worse than no hero copy at all.

### "Full inspectability — preview any action without mutation" is accurate

`Inspect` simulates the full 6-stage pipeline (exit actions → row mutations → entry actions → constraint validation) against a working copy and commits nothing. This claim is a precise description of what the engine does.

### "Compile-time checking" bullets are accurate

Unreachable states, dead-end states, type mismatches, and null-safety violations are all real, implemented diagnostics (C26–C54). No overclaiming there.

### "Live Diagramming" is confirmed implemented

`drawDiagram()` is a fully implemented SVG renderer in `tools/Precept.VsCode/webview/inspector-preview.html`. This is not a mockup or a placeholder — the function is ~700 lines of working code. The claim is accurate.

### "Italic signals constraint pressure" is accurate

Confirmed in `tools/Precept.VsCode/package.json`: scopes ending in `.constrained.precept` get `fontStyle: italic`. States, events, and fields under active constraints render in italic. This is a real, working feature.

### 5 MCP tools, 2 skills are confirmed

`precept_compile`, `precept_fire`, `precept_inspect`, `precept_update`, `precept_language` — confirmed from source. Skills: `precept-authoring` and `precept-debugging` — confirmed in `tools/Precept.Plugin/skills/`. Counts are accurate.

---

## Required Changes

### G1: Frank's RC-1 Is Partially Wrong — `RestoreInstance` Is Not a Real API

**Section:** § Hero Treatment → C# Block Specification  
**Type:** Fabricated API name — must be removed  

Frank's RC-1 correctly flags the C# API chain, but his fix contains an error. He writes:

> "Include `RestoreInstance` as an alternative to `CreateInstance` (the proposal already mentions it parenthetically — good)"

`RestoreInstance` does not exist. I verified against `src/Precept/Dsl/PreceptRuntime.cs`. There is no method by that name on `PreceptEngine` or anywhere in the public API.

The proposal's parenthetical "(or `RestoreInstance`)" will cause real harm: developers who follow a README that names a method that doesn't exist will search for it, find nothing, and assume the documentation is stale or wrong. That's a credibility hit we cannot afford for a category-creation README.

**What actually exists:** `CreateInstance` has two overloads:
1. `engine.CreateInstance()` — starts at `InitialState` with empty data (new entity)
2. `engine.CreateInstance(string state, IDictionary<string, object?> data)` — with specific state and data (database restore)

The second overload is how you restore an entity from the database. It's still `CreateInstance`.

**Fix:** Remove `RestoreInstance` from the C# block specification entirely. The spec must read:

```csharp
var definition = PreceptParser.Parse(dslText);
var engine = PreceptCompiler.Compile(definition);
var instance = engine.CreateInstance(savedState, savedData); // or CreateInstance() for new
var result = engine.Fire(instance, "Submit");
// result.IsSuccess, result.UpdatedInstance
```

If the hero shows a "restore from DB" scenario, the comment clarifies which overload applies. There is no separate `RestoreInstance`.

---

### G2: Prerequisites Are Missing from Getting Started

**Section:** § Section 3: Getting Started  
**Type:** Documentation gap — will cause silent failures  

The VS Code language server is a .NET 10 process. A developer who installs the extension without .NET installed sees no diagnostics, no completions, no hover — no errors, just nothing. Silent failure is the worst possible first experience with a tool that promises real-time feedback.

The current README has: "Prerequisites: .NET 10 SDK (required for both the language server and MCP tools)." The proposal's Getting Started drops this entirely — it goes straight to the `code --install-extension` command.

This isn't a cosmetic omission. It's a correctness gap that will cause real confusion for developers who reach step 1, install the extension, open a `.precept` file, and see no feedback.

**Fix:** Add a Prerequisites note before or within Getting Started Step 1:

> **Prerequisite:** The language server requires the [.NET 10 SDK](https://dotnet.microsoft.com/download). Install it before step 1 if you don't have it already.

This satisfies the "minimum path to first working file" requirement from Steinbrenner research: a developer who follows the steps in order must arrive at a working state. Without .NET, they don't.

---

### G3: "Replacing Three Separate Libraries" Overclaims Interoperability

**Section:** § Section 1: Hook  
**Type:** Overclaiming risk — sets incorrect adoption expectations  

The proposed hook sentence reads:

> "Precept unifies state machines, validation, and business rules into a single DSL — **replacing three separate libraries with one contract**."

The word "replacing" implies Precept can be adopted as a drop-in substitute for existing libraries (Stateless, FluentValidation, NRules, etc.). It cannot. Precept requires adopting a new execution model — entities are governed entirely by the DSL contract, not by calling validators or state machine APIs. It doesn't interop with existing library patterns; it supersedes them with its own model.

A developer reading "replacing" will ask: "Can I replace FluentValidation with Precept in my existing app?" The honest answer is: "Only if you rearchitect your entity lifecycle around the Precept contract." That's a significant commitment, not a library swap.

This matters for the README because it's the first sentence developers evaluate. Overclaiming here creates churn: developers who try to "replace" their existing stack hit friction immediately, blame Precept, and leave — when the right framing would have set correct expectations.

**Fix:** Change to: "Precept unifies state machines, validation, and business rules into a single DSL — eliminating the fragmentation that comes from managing them separately." This conveys the same category-creation argument without implying a drop-in substitution path.

---

## Endorsements of Frank's Other Required Changes

**RC-2 (Step 3 title inconsistency):** Correct. "Add the NuGet Package" is the right title. Consistent with steps 1 and 2, concrete, action-oriented. No runtime concern here but agree it needs to be resolved before the rewrite.

**RC-3 ("Language Server and Preview" is misnamed):** Correct. "Language server" is an implementation term. Developers don't evaluate tools by protocol name. Frank's suggested alternative "Live Editor Experience" describes the developer-facing capability. I'd add: the subsection should be named in a way that makes the state diagram discoverable — it's a feature developers don't expect until they see it.

**RC-4 (AI claims overclaiming):** Correct, and my G3 above is complementary. Frank's fix for the Section 4a closing line is good: "AI agents can validate, inspect, and iterate on `.precept` files through structured tool APIs." This is accurate without overpromising.

---

## Advisory Notes

### Inspect only simulates Transition outcomes without args

A subtle accuracy note for whoever writes the hero: `Inspect(instance, event)` without event arguments still simulates the outcome for `Transition`-targeted rows, but event asserts are skipped (by design — args aren't available). The README hero shouldn't describe `Inspect` as "fully simulating" if the hero shows an event with required arguments and no args are passed. If the hero's C# block shows `engine.Inspect(instance, "Submit")` without args, the result is a partial inspection (guards evaluate without arg data). This is fine for the hero if the DSL block doesn't have event asserts — just be aware that the "full simulation" framing requires args to be complete.

### TransitionOutcome enum has 6 values, not 4

The inspector panel UI shows 4 outcome states (enabled, noTransition, blocked, undefined). The runtime `TransitionOutcome` enum has 6 values: `Transition`, `NoTransition`, `Rejected`, `ConstraintFailure`, `Undefined`, `Unmatched`. The UI collapses `Rejected` + `ConstraintFailure` into one "blocked" visual, and filters `Unmatched` entirely (maps to NotApplicable in the UI — see my earlier surfaces review). This is not a proposal issue, but copy that describes the inspector outcomes should use UI terminology (enabled/blocked/etc.), not enum names.

---

## Summary

Frank's structural approval stands. My three required changes are precision fixes: one corrects a factual error in Frank's own RC-1 (remove `RestoreInstance`), one fills a documentation gap that will cause first-run failures (add .NET SDK prerequisite), and one reduces an adoption expectation mismatch (soften "replacing" to "eliminating fragmentation"). None are structural objections. All are low-effort fixes. Once addressed, this proposal is ready for Shane's sign-off.

— George
