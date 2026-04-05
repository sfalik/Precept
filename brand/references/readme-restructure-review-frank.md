# README Restructure Proposal — Architectural Review

**Reviewer:** Frank (Lead/Architect)  
**Date:** 2026-04-06  
**Proposal under review:** `brand/references/readme-restructure-proposal.md` (J. Peterman, 2026-04-05)  
**Status:** APPROVED WITH REQUIRED CHANGES

---

## Verdict

The proposal is structurally sound. Peterman correctly synthesized three independent research passes into a coherent section order that respects progressive disclosure, CTA hierarchy, and dual-audience (human + AI) requirements. The narrative architecture — prove before you teach — is the right framing for a category-creation README.

I am approving this proposal for Shane's review with **four required changes** and **three advisory notes**. The required changes must be addressed before the rewrite begins. The advisory notes are recommendations that improve the result but do not block implementation.

---

## What the Proposal Gets Right

### 1. Section Sequencing Is Correct

The proposed order — Hook → Quick Example → Getting Started → What Makes Precept Different → Learn More → License — correctly sequences the developer evaluation journey:

- **Concept** (Hook): "What is this?" answered in two sentences.
- **Proof** (Quick Example): "Can I read the DSL? How does it run?" answered by two labeled code blocks.
- **Trial** (Getting Started): "How do I use this right now?" answered by a numbered, linear sequence.
- **Differentiation** (What Makes Precept Different): "Why should I care?" answered only after the reader has seen and can try the tool.

This sequence resolves the current README's structural failure, where differentiation content (§ World-Class Tooling, § The Problem It Solves, § Designed for AI, § The Pillars of Precept) front-loads philosophy before proving basic utility. The proposal correctly places differentiation *after* the trial decision. This is the single most important structural improvement.

### 2. CTA Hierarchy Is Clean

The three-tier CTA strategy — VS Code extension (primary), NuGet package (secondary), Copilot plugin (tertiary) — eliminates the current README's three-way decision paralysis. The numbered Getting Started sequence enforces hierarchy visually without explanatory prose. The demotion of the Copilot plugin from Getting Started to the differentiation section is correct: it's a power-user capability, not a first-contact action.

### 3. Hero Split Is Architecturally Sound

Separating the DSL block (18-20 lines) from the C# execution block (5 lines) into two labeled subsections is the right structural decision. This maps directly to the two distinct artifacts the reader needs to understand: the `.precept` definition file (interpreted by the runtime) and the C# host code (compiled by the .NET build). The current README's 49-line combined block conflates these two concerns and forces the reader to mentally parse where DSL ends and C# begins.

The label convention — "The Contract" (filename.precept) and "The Execution" (C#) — is clear, descriptive, and AI-parseable.

### 4. Dual-Audience Table Is Useful

The "Serving Human and AI Readers" analysis (§ Human Reader Needs → Structural Choice, § AI Reader Needs → Structural Choice) is a strong artifact. It demonstrates that every structural decision was validated against both consumer types. The claim "there are no structural elements that serve humans but hurt AI readability, or vice versa" is accurate for this proposal.

### 5. Elaine's Constraints Are Correctly Treated as Non-Negotiable

The proposal correctly treats Elaine's 16 constraints as hard gates rather than weighted recommendations. This is the right posture. Elaine's research was the most operationally precise of the three passes, and her constraints are testable assertions, not opinions.

---

## Required Changes (Must Fix Before Rewrite)

### RC-1: C# Execution Block API Calls Are Wrong

**Section:** § Hero Treatment (Detailed) → C# Block Specification

The proposal specifies the 5-line C# block as:

> `PreceptParser.Parse(...)`, `PreceptCompiler.Compile(...)`, `engine.CreateInstance(...)`, `engine.Fire(...)`, result check

This is architecturally incorrect. `PreceptCompiler.Compile()` takes a `PreceptDefinition` and returns a `PreceptEngine`. There is no separate `engine` that you call `CreateInstance` on — `CreateInstance` *is* on the engine. The correct minimal round trip is:

```csharp
var definition = PreceptParser.Parse(dslText);
var engine = PreceptCompiler.Compile(definition);
var instance = engine.CreateInstance();
var result = engine.Fire(instance, "Submit");
// result.IsSuccess, result.UpdatedInstance
```

The proposal must specify the correct API surface. Whoever writes the hero copy will use this specification as a contract. If the spec is wrong, the hero is wrong. The actual API is documented in `docs/RuntimeApiDesign.md` and implemented in `src/Precept/Dsl/PreceptRuntime.cs`.

**Fix:** Replace the C# block specification with the correct API call chain. Include `RestoreInstance` as an alternative to `CreateInstance` (the proposal already mentions it parenthetically — good — but the primary chain must also be correct).

### RC-2: Getting Started Step 3 Title Mismatch

**Section:** § Section 3: Getting Started

The proposal's skeleton shows:

```
### 3. Add the NuGet Package
```

But the expanded example in the same section shows:

```
### 3. Integrate with Your C# Project
```

Elaine's original research (§ CTA Clarity) used yet another title: `### 3. Integrate with Your C# Project`.

Pick one. The structural proposal must be internally consistent. My recommendation: use "Add the NuGet Package" — it's concrete and action-oriented, matching the pattern of steps 1 and 2 (both start with verbs targeting a specific artifact). "Integrate with Your C# Project" is vague — integrate *how*?

**Fix:** Resolve the inconsistency. Use one title throughout the proposal.

### RC-3: "Language Server and Preview" Is Misnamed

**Section:** § Section 4: What Makes Precept Different → Subsection 4c

The proposed subsection title is "Language Server and Preview." This is an implementation label, not a user-facing capability description. No developer evaluating Precept thinks "I need a language server." They think "I need good editor support" or "I need live feedback."

The current README calls this "World-Class Tooling" — which Peterman correctly flagged as too vague and too clever. But the fix isn't to swap in a technical implementation name. The fix is a descriptive capability name.

**Fix:** Rename to "Live Editor Experience" or "Real-Time Authoring" — something that describes the *developer experience*, not the *protocol*. The body bullets already describe the experience (completions, highlighting, diagnostics, diagram). Let the heading match.

### RC-4: Differentiation Section Must Not Overstate AI Capability

**Section:** § Section 4a: AI-Native Tooling

The proposed closing line is:

> "AI agents can author, validate, and debug `.precept` files without human intervention."

This is an aspirational claim, not a verified fact. The MCP tools support validation, inspection, and execution. The Copilot plugin supports authoring assistance and debugging guidance. But "without human intervention" implies fully autonomous end-to-end authoring with zero human review. That's not what the tooling delivers today — the agent and skills assist, they don't replace.

This matters because the README is the source of truth. If an AI agent reads this claim and attempts fully autonomous `.precept` authoring based on it, the agent will produce better results if it understands the tools are *collaborative*, not autonomous.

**Fix:** Replace with a factual capability statement. Suggested: "AI agents can validate, inspect, and iterate on `.precept` files through structured tool APIs." This is precise and true. It communicates the power without overpromising.

---

## Advisory Notes (Non-Blocking Recommendations)

### AN-1: Hero Sample Selection Deserves a Structural Constraint, Not Just a Deferral

The proposal correctly defers the specific hero domain to Shane's judgment. But it should add one structural constraint that the research supports and the proposal doesn't capture explicitly:

**The hero sample must use at least one `invariant` or `assert` construct.**

The "structurally impossible invalid states" claim in the hook is the category-defining promise. If the hero doesn't demonstrate constraint enforcement, the reader sees a state machine — not a domain integrity engine. The difference between Precept and Stateless is the constraint layer. The hero must prove that.

The 18-20 line spec already mentions "1 constraint or invariant" — this note is just emphasis that this isn't optional in the sample selection.

### AN-2: "What Makes Precept Different" Heading May Need Revision

"What Makes Precept Different" implies a comparison. The proposal's own constraints (Peterman §7: no "Why Precept vs. X?" section; Steinbrenner: implicit differentiation only) argue against comparative framing. A heading like "What Precept Does" or "How Precept Works" would be more consistent with category-creation positioning — you're teaching a new concept, not defending against alternatives.

However, Elaine's progressive disclosure sequence explicitly uses the label "Why different" for this stage, so there's a tension. This is a copywriting judgment call, not an architectural one. Flagging it for awareness.

### AN-3: Badge Count Discipline

The proposal recommends three badges: NuGet version, MIT license, build status. This is correct. The current README has two (NuGet, license). Adding build status is fine.

The risk is scope creep during implementation — someone will want to add code coverage, download count, Discord, or .NET version badges. The proposal should explicitly state: **maximum three badges in the title block.** Peterman's research correctly noted that "badge walls signal maintenance anxiety, not quality." Lock the count.

---

## Items Explicitly Not In Scope for This Review

1. **Final README copy** — This proposal is structural, not copywriting. I'm reviewing the architecture of the narrative, not the prose.
2. **Hero sample domain selection** — Correctly deferred to Shane. I have no opinion on Order vs. Subscription Billing vs. LoanApplication.
3. **Color/palette application** — Correctly deferred to Elaine's palette/usage pass. No blocking dependency.
4. **Logo/brand mark SVG** — Shane's call. Not an architectural concern.

---

## Summary

The proposal's narrative architecture is sound: prove → trial → differentiate → reference. It correctly synthesizes three research passes without overreaching into copy decisions or sample selection. The four required changes are precision fixes — API accuracy, internal consistency, naming clarity, and factual claims — not structural objections. Once addressed, this proposal is ready for Shane's sign-off and the rewrite can begin.

Approved for Shane review pending the four required changes above.

— Frank
